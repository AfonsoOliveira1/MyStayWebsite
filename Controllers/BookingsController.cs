using Booking.web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Booking.Web.Controllers
{
    public class BookingsController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public BookingsController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // GET: Bookings/Index
        public async Task<IActionResult> Index()
        {
            int userId = int.Parse(User.FindFirst("id").Value);

            var client = _clientFactory.CreateClient("Booking.API");

            // --- CALL FLIGHT API ---
            var responseFlight = await client.GetAsync($"api/FlightBookingsController/passenger/{userId}");

            IEnumerable<FlightBookingViewModel> flightBookings = new List<FlightBookingViewModel>();

            if (responseFlight.IsSuccessStatusCode)
            {
                flightBookings = await responseFlight.Content
                    .ReadFromJsonAsync<IEnumerable<FlightBookingViewModel>>();
            }

            // --- CALL HOUSING API ---
            var responseHouse = await client.GetAsync($"api/HousingBookingController/user/{userId}");

            IEnumerable<HousingBookingViewModel> housingBookings = new List<HousingBookingViewModel>();

            if (responseHouse.IsSuccessStatusCode)
            {
                housingBookings = await responseHouse.Content
                    .ReadFromJsonAsync<IEnumerable<HousingBookingViewModel>>();
            }

            // --- BUILD UNIFIED HISTORY ---
            var history = new UserBookingHistory { Flights = flightBookings, Housings = housingBookings};

            
            return View(history);
        }

        // POST: Bookings/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PutAsync($"api/bookings/cancel/{bookingId}", null);

            return RedirectToAction(nameof(Index));
        }
    }
}