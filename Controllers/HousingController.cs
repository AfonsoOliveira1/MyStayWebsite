using Booking.web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims; // ADICIONA ISTO

namespace Booking.Web.Controllers
{
    public class HousingController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public HousingController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IActionResult> List()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            // CORREÇÃO: api/housings (plural)
            var housings = await client.GetFromJsonAsync<List<HousingViewModel>>("api/housings");
            return View(housings);
        }

        public async Task<IActionResult> Reserve(int id)
        {
            // VAI BUSCAR O ID REAL DO USER LOGADO
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient("Booking.API");
            // CORREÇÃO: api/housings (plural)
            var housing = await client.GetFromJsonAsync<HousingViewModel>($"api/housings/{id}");

            if (housing == null) return NotFound();

            var model = new HousingBookingViewModel
            {
                HousingId = housing.Id,
                CustomerId = int.Parse(userIdStr), 
                HousingName = housing.Name,
                PricePerNight = housing.PricePerNight
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(HousingBookingViewModel booking)
        {
            if (!ModelState.IsValid) return View(booking);

            var client = _clientFactory.CreateClient("Booking.API");
            // CORREÇÃO: Verifica se na API o endpoint de reserva também é plural ou singular
            var response = await client.PostAsJsonAsync("api/housingbooking", booking);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(List));

            ModelState.AddModelError("", "Não foi possível reservar o alojamento.");
            return View(booking);
        }
    }
}