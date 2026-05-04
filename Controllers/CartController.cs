using EcoLilly.Data;
using EcoLilly.Models;
using EcoLilly.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EcoLilly.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly CashfreeService _cashfreeService;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, IConfiguration config, CashfreeService cashfreeService, ILogger<CartController> logger)
        {
            _context = context;
            _config = config;
            _cashfreeService = cashfreeService;
            _logger = logger;
        }

        // ================= CART PAGE =================
        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Account");

            var cartItems = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserEmail == userEmail)
                .ToList();

            return View(cartItems);
        }

        // ================= ADD TO CART =================
        public IActionResult AddToCart(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Account");

            var item = _context.CartItems
                .FirstOrDefault(c => c.ProductId == id && c.UserEmail == userEmail);

            if (item != null)
                item.Quantity++;
            else
                _context.CartItems.Add(new CartItem
                {
                    ProductId = id,
                    Quantity = 1,
                    UserEmail = userEmail
                });

            _context.SaveChanges();

            HttpContext.Session.SetInt32("CartCount",
                _context.CartItems.Count(c => c.UserEmail == userEmail));

            return RedirectToAction("Index");
        }

        public IActionResult Increase(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var cart = _context.CartItems
                .FirstOrDefault(c => c.Id == id && c.UserEmail == userEmail);

            if (cart != null)
            {
                cart.Quantity++;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public IActionResult Decrease(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var cart = _context.CartItems
                .FirstOrDefault(c => c.Id == id && c.UserEmail == userEmail);

            if (cart != null && cart.Quantity > 1)
            {
                cart.Quantity--;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var cart = _context.CartItems
                .FirstOrDefault(c => c.Id == id && c.UserEmail == userEmail);

            if (cart != null)
            {
                _context.CartItems.Remove(cart);
                _context.SaveChanges();
            }

            HttpContext.Session.SetInt32("CartCount",
                _context.CartItems.Count(c => c.UserEmail == userEmail));

            return RedirectToAction("Index");
        }

        // ================= CHECKOUT =================
        public IActionResult Checkout()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Account");

            var cartItems = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserEmail == userEmail)
                .ToList();

            var subtotal = cartItems.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);
            ViewBag.Total = subtotal;

            // determine max allowed discount percent based on subtotal
            int allowedMaxPercent = 0;
            if (subtotal > 1500m) allowedMaxPercent = 50;
            else if (subtotal > 1000m) allowedMaxPercent = 20;
            else if (subtotal > 500m) allowedMaxPercent = 10;

            // load only active, unexpired discounts that do not exceed allowedPercent
            var discounts = _context.Discounts
                .Where(d => d.IsActive && d.ExpiryDate >= DateTime.Now && d.Percentage <= allowedMaxPercent)
                .OrderByDescending(d => d.Percentage)
                .ToList();

            ViewBag.Discounts = discounts;

            return View(cartItems);
        }

        // ================= PLACE ORDER =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string name, string phone, string address, string paymentMethod, string? couponCode)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var cartItems = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserEmail == userEmail)
                .ToList();
            if (!cartItems.Any()) return RedirectToAction("Index");

            decimal total = cartItems.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);

            // Compute discount (server-side) — same rules as checkout UI
            decimal discountAmount = 0m;
            decimal finalAmount = total;

            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var codeTrim = couponCode.Trim();

                // determine allowed percent based on subtotal
                int allowedMaxPercent = 0;
                if (total > 1500m) allowedMaxPercent = 50;
                else if (total > 1000m) allowedMaxPercent = 20;
                else if (total > 500m) allowedMaxPercent = 10;

                var disc = _context.Discounts
                    .Where(d => d.IsActive && d.ExpiryDate >= DateTime.Now)
                    .AsEnumerable()
                    .FirstOrDefault(d => string.Equals(d.Code?.Trim(), codeTrim, StringComparison.OrdinalIgnoreCase));

                if (disc != null && disc.Percentage <= allowedMaxPercent)
                {
                    discountAmount = Math.Round(total * disc.Percentage / 100m, 2);
                    finalAmount = Math.Round(total - discountAmount, 2);
                    if (finalAmount < 0) finalAmount = 0m;
                }
            }

            var order = new Order
            {
                CustomerName = name,
                Address = address,
                Phone = phone,
                UserEmail = userEmail,
                TotalAmount = total,
                DiscountAmount = discountAmount,
                FinalAmount = finalAmount,
                OrderDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                PaymentStatus = "Pending",
                TransactionId = Guid.NewGuid().ToString()
            };

            _context.Orders.Add(order);

            foreach (var item in cartItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product?.Price ?? 0,
                    Order = order
                });
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            HttpContext.Session.SetInt32("CartCount", 0);

            // Online payment flow (unchanged) — uses order.FinalAmount for payment amount
            if (string.Equals(paymentMethod, "Online", StringComparison.OrdinalIgnoreCase))
            {
                var configuredReturn = _config["Cashfree:ReturnUrl"];
                string returnUrl;

                if (!string.IsNullOrWhiteSpace(configuredReturn))
                {
                    // If configuredReturn is an absolute URL, ensure it matches the current origin
                    if (Uri.TryCreate(configuredReturn, UriKind.Absolute, out var cfgUri))
                    {
                        var currentOrigin = $"{Request.Scheme}://{Request.Host.Host}";
                        if (Request.Host.Port.HasValue)
                            currentOrigin += $":{Request.Host.Port.Value}";

                        var cfgOrigin = $"{cfgUri.Scheme}://{cfgUri.Host}";
                        if (!cfgUri.IsDefaultPort)
                            cfgOrigin += $":{cfgUri.Port}";

                        if (!string.Equals(currentOrigin, cfgOrigin, StringComparison.OrdinalIgnoreCase))
                        {
                            // configured return URL points to a different origin/port — avoid using it (prevents popup pointing to wrong localhost port)
                            _logger.LogWarning("Configured Cashfree:ReturnUrl '{Configured}' does not match current origin '{Origin}'. Using dynamic callback URL instead.", configuredReturn, currentOrigin);
                            returnUrl = Url.Action("CashfreeCallback", "Cart", null, Request.Scheme) ?? configuredReturn;
                        }
                        else
                        {
                            returnUrl = configuredReturn;
                        }
                    }
                    else
                    {
                        // configuredReturn is not absolute — treat it as a relative path and build absolute URL on current origin
                        if (configuredReturn.StartsWith("/"))
                            returnUrl = $"{Request.Scheme}://{Request.Host}{configuredReturn}";
                        else
                            returnUrl = configuredReturn;
                    }
                }
                else
                {
                    // No configured return — use dynamic URL for current host/port
                returnUrl = Url.Action("CashfreeCallback", "Cart", null, Request.Scheme) ?? "";
                }

                var amountForPayment = order.FinalAmount > 0 ? order.FinalAmount : order.TotalAmount;

                var cftoken = await _cashfreeService.CreateCftokenAsync(order.Id.ToString(), amountForPayment, order.CustomerName, order.Phone, order.UserEmail, returnUrl);

                var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

                if (string.IsNullOrEmpty(cftoken))
                {
                    var errMsg = "Unable to initiate online payment right now. Please try again or choose Cash On Delivery.";
                    _logger.LogError("PlaceOrder: CreateCftokenAsync returned null for Order {OrderId}", order.Id);

                    if (isAjax)
                        return Json(new { success = false, message = errMsg });

                    TempData["Error"] = errMsg;
                    return RedirectToAction("Checkout");
                }

                var checkoutUrl = _cashfreeService.GetCheckoutPostUrl();
                var appId = _config["Cashfree:AppId"] ?? "";

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        token = cftoken,
                        orderId = order.Id.ToString(),
                        orderAmount = amountForPayment.ToString("0.00"),
                        checkoutUrl,
                        returnUrl,
                        appId,
                        customerName = order.CustomerName,
                        customerPhone = order.Phone,
                        customerEmail = order.UserEmail
                    });
                }

                var vm = new
                {
                    Token = cftoken,
                    CheckoutUrl = checkoutUrl,
                    OrderId = order.Id.ToString(),
                    OrderAmount = amountForPayment.ToString("0.00"),
                    ReturnUrl = returnUrl,
                    AppId = appId,
                    CustomerName = order.CustomerName,
                    CustomerPhone = order.Phone,
                    CustomerEmail = order.UserEmail
                };

                return View("CashfreeRedirect", vm);
            }

            return RedirectToAction("Success", new { orderId = order.Id });
        }

        // ================= SUCCESS =================
        public async Task<IActionResult> Success(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return View(order);
        }

        // ================= PDF INVOICE =================
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);

                    page.Content().Column(col =>
                    {
                        col.Item().Text("INVOICE").FontSize(20).Bold().AlignCenter();

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeColumn().Column(left =>
                            {
                                left.Item().Text($"Order #{order.Id}");
                                left.Item().Text($"Customer: {order.CustomerName}");
                            });

                            // ✅ FIXED ALIGN RIGHT
                            row.ConstantColumn(200).AlignRight().Column(right =>
                            {
                                right.Item().Text($"Total: ₹{order.FinalAmount}");
                                right.Item().Text($"Status: {order.PaymentStatus}");
                            });
                        });

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(80);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Product").Bold();
                                h.Cell().Text("Qty").Bold();
                                h.Cell().Text("Price").Bold();
                            });

                            foreach (var item in order.OrderItems)
                            {
                                table.Cell().Text(item.Product?.Name ?? "");
                                table.Cell().Text(item.Quantity.ToString());
                                table.Cell().Text($"₹{item.Price}");
                            }
                        });
                    });
                });
            });

            var pdf = document.GeneratePdf();
            return File(pdf, "application/pdf", $"invoice_{order.Id}.pdf");
        }

        // ================= CASHFREE CALLBACK =================
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> CashfreeCallback()
        {
            // Read payload (form, query or JSON body)
            var payload = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (Request.HasFormContentType)
                {
                    foreach (var key in Request.Form.Keys)
                        payload[key] = Request.Form[key];
                }

                foreach (var key in Request.Query.Keys)
                {
                    if (!payload.ContainsKey(key))
                        payload[key] = Request.Query[key];
                }

                if (!payload.Any() && Request.ContentLength > 0)
                {
                    Request.Body.Position = 0;
                    using var reader = new System.IO.StreamReader(Request.Body);
                    var raw = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        try
                        {
                            var doc = System.Text.Json.JsonDocument.Parse(raw);
                            foreach (var prop in doc.RootElement.EnumerateObject())
                                payload[prop.Name] = prop.Value.GetString() ?? string.Empty;
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            _logger.LogWarning("CashfreeCallback: non-json body received.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CashfreeCallback: error reading payload.");
            }

            _logger.LogInformation("CashfreeCallback payload: {@Payload}", payload);

            string TryGet(params string[] keys)
            {
                foreach (var k in keys)
                    if (payload.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                        return v;
                return string.Empty;
            }

            var orderIdStr = TryGet("orderId", "order_id", "orderid", "orderID");
            var txStatus = TryGet("txStatus", "tx_status", "txstatus", "status", "payment_status");
            var referenceId = TryGet("referenceId", "reference_id", "reference");
            var txMsg = TryGet("txMsg", "tx_msg", "message");

            if (string.IsNullOrEmpty(orderIdStr))
            {
                _logger.LogWarning("CashfreeCallback: missing orderId, payload: {@Payload}", payload);
                return View("CashfreeReturn", new { orderId = 0, success = false, message = "Payment callback missing order identifier." });
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id.ToString() == orderIdStr);

            if (order == null)
            {
                _logger.LogWarning("CashfreeCallback: order not found for id {OrderId}", orderIdStr);
                return View("CashfreeReturn", new { orderId = 0, success = false, message = "Order not found." });
            }

            var statusNormalized = (txStatus ?? string.Empty).Trim().ToUpperInvariant();

            // If Cashfree returned no explicit txStatus, treat as user returning — keep Pending and show Success UX.
            if (string.IsNullOrEmpty(statusNormalized))
            {
                order.PaymentStatus = "Pending";
                if (!string.IsNullOrEmpty(referenceId)) order.TransactionId = referenceId;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                // Notify opener to show success/pending
                return View("CashfreeReturn", new { orderId = order.Id, success = true, message = "Payment pending. We will update final status shortly." });
            }

            // Map statuses
            if (statusNormalized == "SUCCESS" || statusNormalized == "TXN_SUCCESS" || statusNormalized == "PAID")
            {
                order.PaymentStatus = "Paid";
                if (!string.IsNullOrEmpty(referenceId)) order.TransactionId = referenceId;
            }
            else if (statusNormalized == "PENDING" || statusNormalized == "TXN_PENDING")
            {
                order.PaymentStatus = "Pending";
                if (!string.IsNullOrEmpty(referenceId)) order.TransactionId = referenceId;
            }
            else
            {
                order.PaymentStatus = "Failed";
                if (!string.IsNullOrEmpty(referenceId)) order.TransactionId = referenceId;
            }

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            // Clear user's cart count in session (UX)
            try { HttpContext.Session.SetInt32("CartCount", 0); } catch { /* ignore if session not available */ }

            var success = statusNormalized == "SUCCESS" || statusNormalized == "TXN_SUCCESS" || statusNormalized == "PAID";
            var message = success ? "Payment successful. Thank you!" : ("Payment failed or cancelled: " + (txMsg ?? ""));

            // Return small page that will notify the opener (parent) to show Success and close the popup.
            return View("CashfreeReturn", new { orderId = order.Id, success, message });
        }
    }
}