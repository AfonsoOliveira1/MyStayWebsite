using Booking.web.Models;
namespace Booking.web.Models
{
    public class HomeViewModel
    {
        public List<FlightViewModel> TopFlights { get; set; } = new();
        public List<HousingViewModel> TopStays { get; set; } = new();
    }
}
