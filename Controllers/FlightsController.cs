using Booking.web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Booking.Web.Controllers
{
    public class FlightController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly int _dummyPassengerId = 1;

        public FlightController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // GET: Flight/List
        public async Task<IActionResult> List()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var flights = await client.GetFromJsonAsync<List<FlightViewModel>>("api/flights");
            return View(flights);
        }

        // GET: Flight/Reserve/5
        public async Task<IActionResult> Reserve(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var flight = await client.GetFromJsonAsync<FlightViewModel>($"api/flights/{id}");

            if (flight == null) return NotFound();

            return View(flight);
        }

        // POST: Flight/Reserve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int Id, int SeatId)
        {
            var booking = new FlightBookingViewModel
            {
                FlightId = Id,
                SeatId = SeatId,
                PassengerId = _dummyPassengerId
            };

            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/flightbooking", booking);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(List));

            TempData["Error"] = "Não foi possível reservar este lugar.";
            return RedirectToAction(nameof(Reserve), new { id = Id });
        }
    }
}
