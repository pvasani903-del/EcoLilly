using System.ComponentModel.DataAnnotations;

namespace EcoLilly.Models
{
    public class Contact
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please fill this field")]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters long")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please fill this field")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]

        public string Email { get; set; }

        [Required(ErrorMessage = "Please fill this field")]
        [MinLength(5, ErrorMessage = "Subject must be at least 5 characters long")]

        public string Subject { get; set; }

        [Required(ErrorMessage = "Please fill this field")]
        [MinLength(10, ErrorMessage = "Message must be at least 10 characters long")]

        public string Message { get; set; }
    }
}