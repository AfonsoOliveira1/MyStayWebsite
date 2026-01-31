using System;
using System.Collections.Generic;


namespace Booking.web.Models
{
    public class FlightViewModel
    {
        public int Id { get; set; }
        public int OriginId { get; set; }
        public string? OriginCity { get; set; } // Para mostrar o nome na lista
        public int DestinationId { get; set; }
        public string? DestinationCity { get; set; } // Para mostrar o nome na lista
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public int AirlineId { get; set; }
        public string? AirlineName { get; set; }
        public string ApprovalStatus { get; set; } = "PENDING";
    }
}
