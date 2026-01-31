using System.ComponentModel.DataAnnotations;

namespace Booking.web.Models
{
    public class RegisterModel
    {
        
            [Required]
            public string Name { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Passwordhash { get; set; }

            [Required]
            public string Role { get; set; } // Customer ou Company
        
    }
}
