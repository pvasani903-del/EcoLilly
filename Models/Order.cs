using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcoLilly.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        public string PaymentMethod { get; set; } = "COD";

        public string PaymentStatus { get; set; } = "Pending";

        public string? TransactionId { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        public List<OrderItem> OrderItems { get; set; } = new();
    }
}