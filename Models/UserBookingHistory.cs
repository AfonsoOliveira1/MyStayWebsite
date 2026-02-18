namespace Booking.web.Models
{
    public class UserBookingHistory
    {
        public List<FlightBookingViewModel> Flights { get; set; } = new List<FlightBookingViewModel>();
        public List<HousingBookingViewModel> Housings { get; set; } = new List<HousingBookingViewModel>();

    }
    public class FlightBookingViewModel1
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
