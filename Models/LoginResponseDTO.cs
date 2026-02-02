using Booking.web.Controllers;

namespace Booking.web.Models
{
    public class LoginResponseDTO
    {
        public string Token { get; set; }
        //public UserInfo User { get; set; }
        public UserViewModel User { get; set; }
    }
}
