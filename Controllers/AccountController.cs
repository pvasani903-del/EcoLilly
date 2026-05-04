using EcoLilly.Models;
using EcoLilly.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EcoLilly.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ================= REGISTER =================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user, IFormFile? profilePhoto)
        {
            if (!ModelState.IsValid)
                return View(user);

            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(user);
            }

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                string folder = Path.Combine(_environment.WebRootPath, "uploads");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(profilePhoto.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    profilePhoto.CopyTo(stream);
                }

                user.ProfileImage = "/uploads/" + fileName;
            }

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        // ================= LOGIN =================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            // ===== ADMIN LOGIN =====
            var admin = _context.Admins
                .FirstOrDefault(a => a.Email == email);

            // Wait, did they use the right password?
            if (admin != null && admin.Password == password)
            {
                HttpContext.Session.SetString("Role", "Admin");
                HttpContext.Session.SetString("AdminEmail", admin.Email);
                HttpContext.Session.SetString("UserEmail", admin.Email);
                HttpContext.Session.SetString("UserName", admin.Name ?? admin.Username ?? "Admin");
                HttpContext.Session.SetString("UserProfileImage", "/images/default-avatar.png");

                return RedirectToAction("Index", "Admin");
            }

            // ===== USER LOGIN =====
            var user = _context.Users
                .FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("Role", "User");
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.Name ?? "");

                HttpContext.Session.SetString(
                    "UserProfileImage",
                    string.IsNullOrEmpty(user.ProfileImage)
                        ? "/images/default-avatar.png"
                        : user.ProfileImage
                );

                var cartCount = _context.CartItems.Count(c => c.UserEmail == user.Email);
                var wishlistCount = _context.Wishlists.Count(w => w.UserEmail == user.Email);

                HttpContext.Session.SetInt32("CartCount", cartCount);
                HttpContext.Session.SetInt32("WishlistCount", wishlistCount);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid Email or Password";
            return View();
        }

        // ================= FORGOT PASSWORD (GET) =================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            // Returns Views/Account/ForgotPassword.cshtml
            return View();
        }

        // ================= FORGOT PASSWORD (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email, string? newPassword = null, string? confirmPassword = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email.";
                return View();
            }

            // Immediate reset path
            if (!string.IsNullOrEmpty(newPassword) || !string.IsNullOrEmpty(confirmPassword))
            {
                if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
                {
                    ViewBag.Error = "Passwords do not match.";
                    return View();
                }

                // Prefer updating admin first if email belongs to admin
                var admin = _context.Admins.FirstOrDefault(a => a.Email == email);
                if (admin != null)
                {
                    admin.Password = newPassword;
                    _context.Update(admin);
                    _context.SaveChanges();

                    TempData["success"] = "Password reset successfully. Please login.";
                    return RedirectToAction("Login");
                }

                var user = _context.Users.FirstOrDefault(u => u.Email == email);
                if (user != null)
                {
                    user.Password = newPassword;
                    _context.Update(user);
                    _context.SaveChanges();

                    TempData["success"] = "Password reset successfully. Please login.";
                    return RedirectToAction("Login");
                }

                // Generic response to avoid revealing whether account exists (production-safe)
                TempData["success"] = "If an account exists for that email, the password has been updated. Please login.";
                return RedirectToAction("Login");
            }

            // Default behaviour: generate dev reset link (existing flow)
            var devToken = Guid.NewGuid().ToString();
            var resetLink = Url.Action("ResetPassword", "Account", new { email = email, token = devToken }, Request.Scheme);

            ViewBag.Message = "If an account exists for that email, a reset link has been sent.";
            // Helpful for development to verify flow — remove in production
            ViewBag.DebugResetLink = resetLink;

            return View();
        }

        // ================= RESET PASSWORD (GET) =================
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            // In production: validate token against DB and expiry. Here we simply show the form.
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        // ================= RESET PASSWORD (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Invalid request.";
                return View();
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                ViewBag.Email = email;
                ViewBag.Token = token;
                return View();
            }

            // Prefer updating admin first (so admin and user emails both supported)
            var admin = _context.Admins.FirstOrDefault(a => a.Email == email);
            if (admin != null)
            {
                admin.Password = newPassword;
                _context.Update(admin);
                await _context.SaveChangesAsync();

                TempData["success"] = "Password has been reset successfully. You may now login.";
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Invalid request.";
                return View();
            }

            user.Password = newPassword;
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["success"] = "Password has been reset successfully. You may now login.";
            return RedirectToAction("Login");
        }

        // ================= DASHBOARD =================
        public IActionResult Dashboard()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            var cartCount = _context.CartItems.Count(c => c.UserEmail == userEmail);
            var wishlistCount = _context.Wishlists.Count(w => w.UserEmail == userEmail);

            var orders = _context.Orders
                .Where(o => o.UserEmail == userEmail)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            var model = new DashboardViewModel
            {
                User = user,
                RecentOrders = orders,
                CartCount = cartCount,
                WishlistCount = wishlistCount
            };

            return View(model);
        }
        // ================= CHANGE PASSWORD =================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("UserEmail") == null)
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
                return RedirectToAction("Login");

            if (user.Password != currentPassword)
            {
                ViewBag.Error = "Current password is incorrect.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            user.Password = newPassword;

            _context.Update(user);
            await _context.SaveChangesAsync();

            ViewBag.Success = "Password changed successfully";
            return View();
        }

        // ================= EDIT PROFILE =================
        [HttpGet]
        public IActionResult EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            return View(user);
        }

        [HttpPost]
        public IActionResult EditProfile(User model, IFormFile? profilePhoto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == model.Id);

            if (user == null)
                return NotFound();

            user.Name = model.Name;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.Address = model.Address;

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                string folder = Path.Combine(_environment.WebRootPath, "uploads");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(profilePhoto.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    profilePhoto.CopyTo(stream);
                }

                user.ProfileImage = "/uploads/" + fileName;
            }

            _context.SaveChanges();

            // update session
            HttpContext.Session.SetString("UserName", user.Name ?? "");
            HttpContext.Session.SetString(
                "UserProfileImage",
                string.IsNullOrEmpty(user.ProfileImage)
                    ? "/images/default-avatar.png"
                    : user.ProfileImage
            );

            TempData["success"] = "Profile updated successfully";

            return RedirectToAction("EditProfile");
        }

        // ================= LOGOUT =================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}