using EcoLilly.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLilly.Models // Added the missing namespace!
{
    public class Wishlist
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string UserEmail { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}