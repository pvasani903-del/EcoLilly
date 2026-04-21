using EcoLilly.Helpers;
using EcoLilly.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

public class CheckoutController : Controller
{
    public IActionResult Index()
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");

        if (cart == null || cart.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        return View(cart);
    }

    [HttpPost]
    public IActionResult ProcessPayment(string paymentMethod)
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");

        if (cart == null || cart.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        if (paymentMethod == "Online")
        {
            // Fake success (for now)
            TempData["Success"] = "Online Payment Successful!";
        }
        else
        {
            TempData["Success"] = "Order Placed Successfully!";
        }

        HttpContext.Session.Remove("Cart");

        return RedirectToAction("Success");
    }

    public IActionResult Success()
    {
        return View();
    }
}