
namespace Booking.web.Models
{
    public class VerifyCodeDTO
    {
        public string Email { get; set; }
        public string? Code { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}
