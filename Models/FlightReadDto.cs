namespace Booking.web.Models
{
    public class FlightReadDto
    {
        public int Id { get; set; }

        public int? OriginId { get; set; }
        public string? OriginCity { get; set; }

        public int? DestinationId { get; set; }
        public string? DestinationCity { get; set; }

        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }

        public decimal Price { get; set; }

        public int AirlineId { get; set; }
        public string? AirlineName { get; set; }

        public string ApprovalStatus { get; set; }
    }
}