using System.Collections.Generic;

namespace EcoLilly.Models
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalRevenue { get; set; }

        // For admin pages
        public List<Order> RecentOrders { get; set; } = new();
        public List<string> OrdersByDayLabels { get; set; } = new();
        public List<decimal> OrdersByDayTotals { get; set; } = new();

        public string AdminName { get; set; } = "Admin";

        // For account/dashboard pages
        public User? User { get; set; }
        public int CartCount { get; set; }
        public int WishlistCount { get; set; }
    }
}