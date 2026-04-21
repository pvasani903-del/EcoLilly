using System.Collections.Generic;

namespace EcoLilly.Models
{
    public class CheckoutViewModel
    {
        public List<CheckoutItem> Items { get; set; }

        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        public string PaymentMethod { get; set; }

        public string OfferCode { get; set; }
        public string OfferNote { get; set; }

        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }

    public class CheckoutItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}