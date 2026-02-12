namespace Booking.web.Models
{
    public class UserBookingHistory
    {
        public IEnumerable<FlightBookingViewModel> Flights { get; set; }
        public IEnumerable<HousingBookingViewModel> Housings { get; set; }

    }
}
