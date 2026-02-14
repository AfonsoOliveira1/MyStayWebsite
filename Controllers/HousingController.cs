using Booking.web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            
            var housings = await client.GetFromJsonAsync<List<HousingViewModel>>("api/Housings")
                           ?? new List<HousingViewModel>();
            return View(housings);
        }


        [Authorize(Roles = "RENTER,ADMIN")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.GetAsync("api/Cities");

            IEnumerable<CityViewModel> cities = new List<CityViewModel>();

            if (response.IsSuccessStatusCode)
            {
                cities = await response.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>()
                         ?? new List<CityViewModel>();
            }

            
            ViewBag.Cities = new SelectList(cities, "Id", "Name");

            return View();
        }
        [Authorize(Roles = "RENTER,ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HousingCreateDto model)
        {
            var client = _clientFactory.CreateClient("Booking.API");

            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("api/Housings", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Alojamento enviado para aprovação!";
                return RedirectToAction("List");
            }

         
            var citiesResponse = await client.GetAsync("api/Cities");
            if (citiesResponse.IsSuccessStatusCode)
            {
                var cities = await citiesResponse.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>();

               
                ViewBag.Cities = new SelectList(cities, "Id", "Name");
            }
            else
            {
                ViewBag.Cities = new SelectList(new List<CityViewModel>(), "Id", "Name");
            }

            return View(model);
        }


        public async Task<IActionResult> Reserve(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient("Booking.API");

            
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            
            var housing = await client.GetFromJsonAsync<HousingViewModel>("api/Housings/" + id);
            if (housing == null) return NotFound();

            var model = new HousingBookingViewModel
            {
                HousingId = housing.Id,
                CustomerId = int.Parse(userIdStr),
                HousingName = housing.Name,
                PricePerNight = housing.PricePerNight,
                CheckInDate = DateTime.Now,
                CheckOutDate = DateTime.Now.AddDays(1)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(HousingBookingViewModel booking)
        {
            if (!ModelState.IsValid) return View(booking);

            var client = _clientFactory.CreateClient("Booking.API");

            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            
            var response = await client.PostAsJsonAsync("api/HousingBookings", booking);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Reserva efetuada com sucesso!";
                return RedirectToAction("Index", "Bookings");
            }

            ModelState.AddModelError("", "Não foi possível reservar o alojamento " + booking.HousingName + ".");
            return View(booking);
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> PendingApprovals()
        {
            var client = _clientFactory.CreateClient("Booking.API");

            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Housings/pending");
            if (response.IsSuccessStatusCode)
            {
                var housings = await response.Content.ReadFromJsonAsync<List<HousingViewModel>>();
                return View(housings);
            }

            return View(new List<HousingViewModel>());
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Approve(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync("api/Housings/" + id + "/approve", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Alojamento aprovado com sucesso!";
            }
            else
            {
                TempData["Error"] = "Erro ao comunicar com a API para aprovação.";
            }

            return RedirectToAction("PendingApprovals");
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Reject(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync("api/Housings/" + id + "/reject", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Alojamento rejeitado.";
            }
            else
            {
                TempData["Error"] = "Erro ao comunicar com a API para rejeição.";
            }

            return RedirectToAction("PendingApprovals");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDescription(int id, string description)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PutAsJsonAsync($"api/Housing/{id}/description", description);

            if (response.IsSuccessStatusCode) return Ok();
            return BadRequest();
        }
    }
}