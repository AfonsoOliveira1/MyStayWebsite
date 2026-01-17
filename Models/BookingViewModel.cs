namespace Booking.web.Models
{
    public class BookingViewModel
    {
        public int Id { get; set; }

        // Flight ou Housing
        public string Type { get; set; } = "";

        // voo ou alojamento
        public string ItemName { get; set; } = "";

        // Datas 
        public DateTime? BookingDate { get; set; }           // Data da reserva
        public DateTime? CheckInDate { get; set; }           // Apenas para alojamento
        public DateTime? CheckOutDate { get; set; }          // Apenas para alojamento
        public DateTime? FlightDepartureDate { get; set; }   // Apenas para voo
        public DateTime? FlightArrivalDate { get; set; }     // Apenas para voo

        
        public string Status { get; set; } = "Confirmed";

        // se está ativo ou n
        public bool IsActive { get; set; } = true;
    }
}
