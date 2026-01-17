using Booking.web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Booking.Web.Controllers
{
    public class BookingsController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly int _dummyUserId = 1;

        public BookingsController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // GET: Bookings/Index
        public async Task<IActionResult> Index()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var bookings = await client.GetFromJsonAsync<List<BookingViewModel>>($"api/bookings/user/{_dummyUserId}");
            return View(bookings);
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
