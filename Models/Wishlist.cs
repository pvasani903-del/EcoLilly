using EcoLilly.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Wishlist
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string UserEmail { get; set; }   // THIS COLUMN MUST EXIST IN DB

    [ForeignKey("ProductId")]
    public Product Product { get; set; }
}