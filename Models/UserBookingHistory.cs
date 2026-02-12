namespace Booking.web.Models
{
    public class UserBookingHistory
    {
        public IEnumerable<FlightBookingReadDto> Flights { get; set; }
        public IEnumerable<HousingBookingReadDto> Housings { get; set; }

    }
}
