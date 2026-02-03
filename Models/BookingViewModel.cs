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
        public DateTime? BookingDate { get; set; }           
        public DateTime? CheckInDate { get; set; }           
        public DateTime? CheckOutDate { get; set; }         
        public DateTime? FlightDepartureDate { get; set; }   
        public DateTime? FlightArrivalDate { get; set; }    
        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = "Confirmed";

        // se esta ativo ou n
        public bool IsActive { get; set; } = true;
    }
}
