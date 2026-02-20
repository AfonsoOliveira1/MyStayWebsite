using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Booking.Web.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class CityController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public CityController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateCountriesDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCountriesDropdown();
                return View(model);
            }

            var client = _clientFactory.CreateClient("Booking.API");

           
            var token = User.FindFirst("JWToken")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            
            var response = await client.PostAsJsonAsync("api/Cities", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Cidade " + model.Citynamept + " adicionada com sucesso!";
                return RedirectToAction("List"); // de cidades
            }

            ModelState.AddModelError("", "Erro ao guardar a cidade na API. Status: " + response.StatusCode);
            await PopulateCountriesDropdown();
            return View(model);
        }

        public async Task<IActionResult> List()
        {
            var client = _clientFactory.CreateClient("Booking.API");

            var token = User.FindFirst("JWToken")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            
            var cities = await client.GetFromJsonAsync<List<CityViewModel>>("api/Cities")
                         ?? new List<CityViewModel>();

            return View(cities);
        }

        private async Task PopulateCountriesDropdown()
        {
            var client = _clientFactory.CreateClient("Booking.API");

            try
            {
                var countries = await client.GetFromJsonAsync<List<CountryViewModel>>("api/Countries")
                                ?? new List<CountryViewModel>();

                ViewBag.Countries = new SelectList(countries, "Id", "Name");
            }
            catch
            {
                ViewBag.Countries = new SelectList(Enumerable.Empty<SelectListItem>());
                ModelState.AddModelError("", "Não foi possível carregar a lista de países da API.");
            }
        }
    }
}