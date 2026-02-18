namespace Booking.web.Models
{
    public class FlightBookingCreateDto
    {

        public int Flightid { get; set; }
        public List<string> SeatIds { get; set; }
        public int? Passengerid { get; set; }
    }
}
