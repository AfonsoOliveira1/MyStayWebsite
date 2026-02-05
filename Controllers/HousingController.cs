using Booking.web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

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
            var housings = await client.GetFromJsonAsync<List<HousingViewModel>>("api/housings");
            return View(housings);
        }

        public async Task<IActionResult> Reserve(int id)
        {
            // Agora o NameIdentifier já não virá nulo
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient("Booking.API");

            // BUSCA O TOKEN DO COOKIE E ENVIA PARA A API
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var housing = await client.GetFromJsonAsync<HousingViewModel>($"api/housings/{id}");
            if (housing == null) return NotFound();

            var model = new HousingBookingViewModel
            {
                HousingId = housing.Id,
                CustomerId = int.Parse(userIdStr),
                HousingName = housing.Name,
                PricePerNight = housing.PricePerNight,
                CheckInDate = DateTime.Now, // Data de hoje
                CheckOutDate = DateTime.Now.AddDays(1) // Amanhã
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(HousingBookingViewModel booking)
        {
            if (!ModelState.IsValid) return View(booking);

            var client = _clientFactory.CreateClient("Booking.API");

            // ENVIA O TOKEN TAMBÉM NO POST
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("api/housingbooking", booking);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(List));

            ModelState.AddModelError("", "Não foi possível reservar o alojamento.");
            return View(booking);
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> PendingApprovals()
        {
            var client = _clientFactory.CreateClient("Booking.API");

            var response = await client.GetAsync("api/Housings/pending");
            if (response.IsSuccessStatusCode)
            {
                var housings = await response.Content.ReadFromJsonAsync<List<HousingViewModel>>();
                return View(housings);
            }

            return View(new List<HousingViewModel>());
        }
    }
}