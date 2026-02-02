using System.ComponentModel.DataAnnotations;

namespace Booking.web.Models
{
    public class VerifyCodeModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "O código deve ter 6 dígitos.")]
        public string Code { get; set; }
    }

}
