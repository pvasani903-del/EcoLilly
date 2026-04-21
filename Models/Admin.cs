using System.ComponentModel.DataAnnotations;

namespace EcoLilly.Models
{
    public class Admin
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        // Use Email for login
        [Required, EmailAddress] 
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string Name { get; set; }
    }
}