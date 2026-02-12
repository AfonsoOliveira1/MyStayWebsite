using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Booking.Web.Controllers
{
    [Authorize(Roles = "ADMIN,AIRLINE")]
    public class FlightController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public FlightController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        private async Task LoadFlightViewBags()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = await HttpContext.GetTokenAsync("access_token") ?? User.FindFirst("JWToken")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                var cities = await client.GetFromJsonAsync<List<CityViewModel>>("api/Cities") ?? new List<CityViewModel>();
                var cityList = new SelectList(cities, "Id", "Name");
                ViewBag.OriginId = cityList;
                ViewBag.DestinationId = cityList;
            }
            catch
            {
                ViewBag.OriginId = new SelectList(new List<SelectListItem>());
                ViewBag.DestinationId = new SelectList(new List<SelectListItem>());
            }

            var airlines = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "TAP Air Portugal" },
                new SelectListItem { Value = "2", Text = "Ryanair" },
                new SelectListItem { Value = "3", Text = "Emirates" }
            };
            ViewBag.AirlineId = new SelectList(airlines, "Value", "Text");
        }

        [AllowAnonymous]
        public async Task<IActionResult> List()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var flights = await client.GetFromJsonAsync<List<FlightViewModel>>("api/Flights") ?? new List<FlightViewModel>();
            return View(flights);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadFlightViewBags();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlightViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadFlightViewBags();
                return View(model);
            }

            var client = _clientFactory.CreateClient("Booking.API");
            model.ArrivalTime = model.DepartureTime.AddHours(2);

            var token = await HttpContext.GetTokenAsync("access_token") ?? User.FindFirst("JWToken")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.PostAsJsonAsync("api/Flights", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Voo registado! Aguarda aprovação de um administrador.";
                return RedirectToAction("List");
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", "Erro na API: " + errorMsg);

            await LoadFlightViewBags();
            return View(model);
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> PendingApprovals()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = await HttpContext.GetTokenAsync("access_token") ?? User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Flights/pending");

            if (response.IsSuccessStatusCode)
            {
                var flights = await response.Content.ReadFromJsonAsync<List<FlightViewModel>>();
                return View(flights);
            }

            return View(new List<FlightViewModel>());
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Approve(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = await HttpContext.GetTokenAsync("access_token") ?? User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            
            var response = await client.PostAsync("api/Flights/" + id + "/approve", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Voo " + id + " aprovado com sucesso!";
            }
            else
            {
                TempData["Error"] = "Erro ao aprovar o voo " + id + ".";
            }

            return RedirectToAction("PendingApprovals");
        }
    }
}