using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Booking.web.Controllers
{
    [Authorize(Roles = "AIRLINE,ADMIN")]
    [Route("Airline")]
    public class AirlineController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AirlineController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // metodo aux para config o cliente com o token
        private HttpClient GetAuthorizedClient()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AirlineViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = GetAuthorizedClient();

            var response = await client.PostAsJsonAsync("api/Airlines", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "A companhia " + model.AirlineName + " foi criada com sucesso!";
                return RedirectToAction("Create", "Flight");
            }

            var erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", "Erro ao guardar na API: " + erro);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var client = GetAuthorizedClient();
            var response = await client.GetAsync("api/Airlines");

            if (response.IsSuccessStatusCode)
            {
                var airlines = await response.Content.ReadFromJsonAsync<List<AirlineViewModel>>();
                return View(airlines);
            }

            TempData["ErrorMessage"] = "Erro ao carregar a lista de companhias.";
            return View(new List<AirlineViewModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Delete(int id)
        {
            var client = GetAuthorizedClient();
            var response = await client.DeleteAsync("api/Airlines/" + id);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Airline deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting airline.";
            }

            return RedirectToAction("List");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = GetAuthorizedClient();
            var response = await client.GetAsync("api/Airlines/" + id);

            if (response.IsSuccessStatusCode)
            {
                var model = await response.Content.ReadFromJsonAsync<AirlineViewModel>();
                return View(model);
            }

            TempData["ErrorMessage"] = "Airline not found.";
            return RedirectToAction("List");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AirlineViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = GetAuthorizedClient();
            var response = await client.PutAsJsonAsync("api/Airlines/" + model.IdAirline, model);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Airline " + model.AirlineName + " updated successfully!";
                return RedirectToAction("List");
            }

            ModelState.AddModelError("", "Error updating airline. Please try again.");
            return View(model);
        }

        [Authorize(Roles = "AIRLINE")]
        [HttpGet("Earnings")]
        public async Task<IActionResult> Earnings()
        {
            var client = GetAuthorizedClient();

            var culture = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var response = await client.GetAsync("api/Airlines/FinanceSummary?lang=" + culture);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<AirlineFinanceViewModel>();
                return View("~/Views/Airline/Earnings.cshtml", data);
            }

            TempData["ErrorMessage"] = "Erro ao carregar resumo financeiro.";
            return View("~/Views/Airline/Earnings.cshtml", new AirlineFinanceViewModel { Bookings = new List<FlightBookingHistoryViewModel>() });
        }
    }
}