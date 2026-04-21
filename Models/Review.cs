using EcoLilly.Models;
using System;

namespace EcoLilly.Models
{
    public class Review
    {
        public int Id { get; set; }

        public string? User { get; set; }

        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime Date { get; set; }

        public int ProductId { get; set; }

        public Product? Product { get; set; }
    }
}