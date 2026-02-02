using System;
namespace Booking.web.Models
{
    public class HousingRatingViewModel
    {
        public int Id { get; set; }
        public int HousingId { get; set; }
        public int CustomerId { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; } = "";
        public DateTime RatingDate { get; set; } = DateTime.UtcNow;
    }
}
