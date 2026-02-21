using System;
using System.Text.Json.Serialization;
namespace Booking.web.Models
{
    public class HousingRatingViewModel
    {
        public int Id { get; set; }
        public int HousingId { get; set; }
        public int CustomerId { get; set; }

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; } = "";
        public DateTime RatingDate { get; set; } = DateTime.UtcNow;
    }
}
