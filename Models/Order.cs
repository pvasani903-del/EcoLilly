using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcoLilly.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Phone { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        // Payment
        public string PaymentMethod { get; set; } = "COD";

        public string PaymentStatus { get; set; } = "Pending";

        public string? TransactionId { get; set; }

        public string UserEmail { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}