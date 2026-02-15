using System.ComponentModel.DataAnnotations;

namespace Booking.web.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "O nome é obrigatório")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Passwordhash { get; set; }

        [Required]
        public string Role { get; set; } // "CUSTOMER" / "COMPANY"

        public string? CompanyType { get; set; } // "AIRLINE" / "RENTER"
        public int? SelectedCompanyId { get; set; } // ID da airline / Renter 

    }
}
