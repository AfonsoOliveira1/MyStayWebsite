using System;

namespace Booking.web.Models
{
    public class HousingBookingViewModel
    {
        public int Id { get; set; }
        public int HousingId { get; set; }
        public int CustomerId { get; set; }

        public string HousingName { get; set; } = "";    
        public decimal PricePerNight { get; set; }    

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }

        public decimal TotalPrice                    
        {
            get
            {
                int nights = (CheckOutDate - CheckInDate).Days;
                return nights > 0 ? nights * PricePerNight : 0;
            }
        }

        public string Status { get; set; } = "Confirmed";
        public bool IsActive { get; set; } = true;
    }
}
