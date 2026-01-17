using System;
using System.Collections.Generic;


namespace Booking.web.Models
{
    public class FlightViewModel
    {
        public int Id { get; set; }
        public string Origin { get; set; } = "";
        public string Destination { get; set; } = "";
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Price { get; set; }

        // Lista de lugares do voo
        public List<SeatViewModel> Seats { get; set; } = new();
    }
}
