namespace Booking.web.Models
{
    public class HousingRatingReadDto
    {
        public int Id { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; }
        public DateTime Ratingdate { get; set; }
        public string CustomerName { get; set; } 
    }
}