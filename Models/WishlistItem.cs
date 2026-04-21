namespace EcoLilly.Models
{
    public class WishlistItem
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string Name { get; set; }

        public string Image { get; set; }

        public string Category { get; set; }

        public string Description { get; set; }

        public string EcoFeatures { get; set; }

        public decimal Price { get; set; }
    }
}