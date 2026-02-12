namespace Booking.web.Models
{
    public class FlightBookingReadDto
    {
        public int Id { get; set; }
        public string Seatnumber { get; set; }
        public string PassengerName { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set; }
        public decimal Price { get; set; }
               public string AirlineName { get; set; } 
        public string OriginCityName { get; set; } 
        public string DestinationCityName { get; set; } 
    }
}
