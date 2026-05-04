namespace EcoLilly.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }   // ✅ safe

        public int ProductId { get; set; }

        // ✅ FIX: allow null (VERY IMPORTANT)
        public Product? Product { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }
    }
}