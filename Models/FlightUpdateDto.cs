namespace Booking.web.Models
{
    public class FlightUpdateDto
    {
        public int Id { get; set; }
        public int? OriginId { get; set; }
        public int? DestinationId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public string ApprovalStatus { get; set; }
    }
}
