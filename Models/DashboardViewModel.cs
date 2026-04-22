using System;
using System.Collections.Generic;

namespace EcoLilly.Models
{
    public class DashboardViewModel
    {
        // ================= BASIC COUNTS =================
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalRevenue { get; set; }

        // ================= ADMIN INFO =================
        public string AdminName { get; set; } = "Admin";

        // ================= RECENT DATA =================
        public List<Order> RecentOrders { get; set; } = new();

        // ================= CHART DATA (7 DAYS) =================
        public List<string> OrdersByDayLabels { get; set; } = new();
        public List<decimal> OrdersByDayTotals { get; set; } = new();

        // ================= CART & WISHLIST =================
        public int CartCount { get; set; }
        public int WishlistCount { get; set; }

        // ================= OPTIONAL USER INFO =================
        public User? User { get; set; }
    }
}