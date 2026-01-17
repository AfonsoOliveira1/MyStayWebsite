using Booking.web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Booking.Web.Controllers
{
    public class HousingController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly int _dummyCustomerId = 1; // Substituir pelo user logado

        public HousingController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // GET: Housing/List
        public async Task<IActionResult> List()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var housings = await client.GetFromJsonAsync<List<HousingViewModel>>("api/housing");
            return View(housings);
        }

        // GET: Housing/Reserve/5
        public async Task<IActionResult> Reserve(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var housing = await client.GetFromJsonAsync<HousingViewModel>($"api/housing/{id}");

            if (housing == null) return NotFound();

            var model = new HousingBookingViewModel
            {
                HousingId = housing.Id,
                CustomerId = _dummyCustomerId,
                HousingName = housing.Name,
                PricePerNight = housing.PricePerNight
            };

            return View(model);
        }

        // POST: Housing/Reserve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(HousingBookingViewModel booking)
        {
            if (!ModelState.IsValid) return View(booking);

            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/housingbooking", booking);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(List));

            ModelState.AddModelError("", "Não foi possível reservar o alojamento.");
            return View(booking);
        }
    }
}
