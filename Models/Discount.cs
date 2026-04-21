using System;
using System.ComponentModel.DataAnnotations;

namespace EcoLilly.Models
{
    public class Discount
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; }

        public int Percentage { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}