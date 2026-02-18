namespace Booking.web.Models
{
    public class FlightBookingViewModel
    {
        public int Id { get; set; }
        public int FlightId { get; set; }
        public int SeatId { get; set; }
        public int PassengerId { get; set; }
        public string Status { get; set; } = "Confirmed";

        public string OriginCityName { get; set; }
        public string DestinationCityName { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public string SeatNumber { get; set; }
    }
}
