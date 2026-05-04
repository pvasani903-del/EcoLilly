using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLilly.Models
{
    public class Admin
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        // Use Email for login
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}