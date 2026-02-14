using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Booking.web.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AirlineController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AirlineController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AirlineViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");

            var token = await HttpContext.GetTokenAsync("access_token")
                        ?? User.FindFirst("JWToken")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

           
            var response = await client.PostAsJsonAsync("api/Airlines", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "A companhia " + model.AirlineName + " foi criada com sucesso!";
                return RedirectToAction("Create", "Flight");
            }

            var erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", "Erro ao guardar na API: " + erro);

            return View(model);
        }
        public async Task<IActionResult> List()
        {
            var client = _clientFactory.CreateClient("Booking.API");

            
            var token = await HttpContext.GetTokenAsync("access_token") ?? User.FindFirst("JWToken")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

          
            var response = await client.GetAsync("api/Airlines");

            if (response.IsSuccessStatusCode)
            {
                var airlines = await response.Content.ReadFromJsonAsync<List<AirlineViewModel>>();
                return View(airlines);
            }

            return View(new List<AirlineViewModel>());
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");

            var token = await HttpContext.GetTokenAsync("access_token") ?? User.FindFirst("JWToken")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.DeleteAsync("api/Airlines/" + id);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Airline deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Error deleting airline.";
            }

            return RedirectToAction("List");
        }

      //get
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
           
            var response = await client.GetAsync("api/Airlines/" + id);

            if (response.IsSuccessStatusCode)
            {
                var model = await response.Content.ReadFromJsonAsync<AirlineViewModel>();
                return View(model);
            }

            return RedirectToAction("List");
        }

        //post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AirlineViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");

            var response = await client.PutAsJsonAsync("api/Airlines/" + model.IdAirline, model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Airline " + model.AirlineName + " updated successfully!";
                return RedirectToAction("List");
            }

            ModelState.AddModelError("", "Error updating airline. Please try again.");
            return View(model);
        }
    }
}
