using Booking.web.Controllers;

namespace Booking.web.Models
{
    public class LoginResponseDTO
    {
        public string Token { get; set; }
        public UserViewModel User { get; set; }
        public string Message { get; set; }
        public string SessionId { get; set; }// novo id de sessao
        public string Status { get; set; }
    }
}
