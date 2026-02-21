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

        public async Task<IActionResult> List()
        {
            var client = CreateClientWithAuth();
            var cities = await client.GetFromJsonAsync<List<CityViewModel>>("api/Cities")
                         ?? new List<CityViewModel>();

            return View(cities);
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

            var client = CreateClientWithAuth();
            var response = await client.PostAsJsonAsync("api/Cities", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Cidade " + model.Citynamept + " adicionada com sucesso!";
                return RedirectToAction("List");
            }

            ModelState.AddModelError("", "Erro ao guardar a cidade. Status: " + response.StatusCode);
            await PopulateCountriesDropdown();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateClientWithAuth();
            var response = await client.GetAsync("api/Cities/" + id);

            if (!response.IsSuccessStatusCode) return NotFound();

            var city = await response.Content.ReadFromJsonAsync<CityViewModel>();

            await PopulateCountriesDropdown();

            return View(city);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCountriesDropdown();
                return View(model);
            }

            var client = CreateClientWithAuth();
            var response = await client.PutAsJsonAsync("api/Cities/" + model.Id, model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Cidade " + model.Citynamept + " atualizada com sucesso!";
                return RedirectToAction("List");
            }

            TempData["Error"] = "Erro ao atualizar a cidade.";
            await PopulateCountriesDropdown();
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClientWithAuth();
            var response = await client.DeleteAsync("api/Cities/" + id);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "A cidade com o ID " + id + " foi desativada com sucesso.";
            }
            else
            {
                TempData["Error"] = "Não foi possível ocultar a cidade.";
            }

            return RedirectToAction("List");
        }


        private HttpClient CreateClientWithAuth()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
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
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Erro ao carregar países: " + ex.Message);
                ViewBag.Countries = new SelectList(new List<SelectListItem>());
            }
        }
    }
}