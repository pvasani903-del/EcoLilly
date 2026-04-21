using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcoLilly.Models
{
    public class Product
    {
        public Product()
        {
            Reviews = new List<Review>();
        }

        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public decimal Price { get; set; }

        public string? Description { get; set; }

        public string? Image { get; set; }

        public string? Category { get; set; }

        public string? EcoFeatures { get; set; }
        // Stored as comma separated string

        public bool InStock { get; set; }

        public List<Review> Reviews { get; set; }
    }
}