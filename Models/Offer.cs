using System.ComponentModel.DataAnnotations;

namespace EcoLilly.Models
{
    // Offer entity: Flat (value) or Percentage (value percent)
    public class Offer
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        // "Flat" or "Percentage"
        [Required]
        public string DiscountType { get; set; } = "Flat";

        // For Flat -> rupee amount; For Percentage -> percent value (e.g., 10)
        public decimal Value { get; set; }

        public bool IsActive { get; set; } = true;
    }
}