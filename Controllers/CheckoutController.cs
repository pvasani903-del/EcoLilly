using EcoLilly.Data;
using EcoLilly.Models;
using EcoLilly.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EcoLilly.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly CashfreeService _cashfreeService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(ApplicationDbContext context, IConfiguration config, CashfreeService cashfreeService, ILogger<CheckoutController> logger)
        {
            _context = context;
            _config = config;
            _cashfreeService = cashfreeService;
            _logger = logger;
        }

        // AJAX endpoint used by frontend to request token and order data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string name, string phone, string address, string paymentMethod, string? couponCode)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return Json(new { success = false, message = "You must be signed in." });

            var cartItems = _context.CartItems
                .Where(c => c.UserEmail == userEmail)
                .ToList();

            if (!cartItems.Any())
                return Json(new { success = false, message = "Cart is empty." });

            // Load products
            _context.Entry(cartItems.First()).Reference(c => c.Product).Load();
            var cart = _context.CartItems
                .Where(c => c.UserEmail == userEmail)
                .Select(c => new { c.ProductId, c.Quantity })
                .ToList();

            decimal total = cart.Sum(i => _context.Products.Find(i.ProductId)!.Price * i.Quantity);

            // compute discount (same rules as you used)
            decimal discountAmount = 0m;
            decimal finalAmount = total;
            if (!string.IsNullOrEmpty(couponCode))
            {
                var codeTrim = couponCode.Trim().ToLowerInvariant();
                var disc = _context.Discounts
                    .Where(d => d.IsActive && d.ExpiryDate >= DateTime.Now)
                    .AsEnumerable()
                    .FirstOrDefault(d => string.Equals(d.Code?.Trim(), codeTrim, StringComparison.OrdinalIgnoreCase));

                if (disc != null)
                {
                    int allowedMaxPercent = 0;
                    if (total > 1500m) allowedMaxPercent = 50;
                    else if (total > 1000m) allowedMaxPercent = 20;
                    else if (total > 500m) allowedMaxPercent = 10;

                    if (disc.Percentage <= allowedMaxPercent)
                    {
                        discountAmount = Math.Round(total * disc.Percentage / 100m, 2);
                        finalAmount = Math.Round(total - discountAmount, 2);
                    }
                }
            }

            // create core order (status Pending) before redirect
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
            // add order items
            foreach (var ci in cartItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    Price = ci.Product.Price,
                    Order = order
                });
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            HttpContext.Session.SetInt32("CartCount", 0);

            // If Online, create token and return JSON for client to post to Cashfree
            if (string.Equals(paymentMethod, "Online", StringComparison.OrdinalIgnoreCase))
            {
                var returnUrl = Url.Action("PaymentCallback", "Checkout", null, Request.Scheme); // must match callback route
                var amountForPayment = order.FinalAmount > 0 ? order.FinalAmount : order.TotalAmount;

                _logger.LogInformation("Creating cftoken for Order {OrderId} Amount {Amount}", order.Id, amountForPayment);

                var cftoken = await _cashfreeService.CreateCftokenAsync(order.Id.ToString(), amountForPayment, order.CustomerName, order.Phone, order.UserEmail, returnUrl);
                if (string.IsNullOrEmpty(cftoken))
                {
                    _logger.LogWarning("cftoken creation failed for order {OrderId}", order.Id);
                    return Json(new { success = false, message = "Unable to initiate online payment. Try COD or contact support." });
                }

                var checkoutUrl = _cashfreeService.GetCheckoutPostUrl();
                var appId = _config["Cashfree:AppId"] ?? string.Empty;

                return Json(new
                {
                    success = true,
                    token = cftoken,
                    orderId = order.Id.ToString(),
                    orderAmount = amountForPayment.ToString("0.00"),
                    checkoutUrl,
                    returnUrl,
                    appId
                });
            }

            // COD: client will redirect to Success via normal flow
            return Json(new { success = true, cod = true, redirect = Url.Action("Success", "Cart") ?? "/Cart/Success" });
        }

        // Payment callback (Cashfree redirects here)
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> PaymentCallback()
        {
            // read both form and query keys (Cashfree may POST or GET)
            var payload = Request.HasFormContentType ? Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString()) : Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

            payload = payload.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);

            string TryGet(string key)
            {
                payload.TryGetValue(key, out var v);
                return v ?? string.Empty;
            }

            var orderId = TryGet("orderId");
            var txStatus = TryGet("txStatus") ?? TryGet("status");
            var referenceId = TryGet("referenceId") ?? TryGet("reference_id");
            var txMsg = TryGet("txMsg") ?? TryGet("tx_msg") ?? string.Empty;

            if (string.IsNullOrEmpty(orderId))
            {
                TempData["Error"] = "Payment callback missing orderId.";
                return RedirectToAction("Checkout", "Cart");
            }

            var order = _context.Orders.FirstOrDefault(o => o.Id.ToString() == orderId);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Checkout", "Cart");
            }

            var status = (txStatus ?? string.Empty).Trim().ToUpperInvariant();
            if (status == "SUCCESS" || status == "TXN_SUCCESS" || status == "PAID")
                order.PaymentStatus = "Success";
            else if (status == "PENDING" || status == "TXN_PENDING")
                order.PaymentStatus = "Pending";
            else
                order.PaymentStatus = "Failed";

            if (!string.IsNullOrEmpty(referenceId))
                order.TransactionId = referenceId;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            if (order.PaymentStatus == "Success")
                return RedirectToAction("Success", "Cart");

            TempData["Error"] = "Payment did not complete: " + txMsg;
            return RedirectToAction("Checkout", "Cart");
        }
    }
}