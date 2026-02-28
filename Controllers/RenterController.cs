using Booking.web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Booking.web.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class RenterController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public RenterController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        private HttpClient GetAuthenticatedClient()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirstValue("JWToken");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        public async Task<IActionResult> List()
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync("api/Renters");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var renters = JsonSerializer.Deserialize<List<RenterViewModel>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(renters);
            }

            TempData["ErrorMessage"] = "Erro ao carregar a lista de empresas.";
            return View(new List<RenterViewModel>());
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RenterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var client = GetAuthenticatedClient();
                var response = await client.PostAsJsonAsync("api/Renters", model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Nova empresa criada com sucesso!";
                    return RedirectToAction(nameof(List));
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ModelState.AddModelError("", "Sessão expirada. Por favor, faça login novamente.");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", "Erro ao criar empresa na API: " + error);
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync($"api/Renters/{id}");

            if (!response.IsSuccessStatusCode) return NotFound();

            var content = await response.Content.ReadAsStringAsync();
            var renter = JsonSerializer.Deserialize<RenterViewModel>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(renter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RenterViewModel model)
        {
            if (id != model.IdRenter) return BadRequest();

            if (ModelState.IsValid)
            {
                var client = GetAuthenticatedClient();
                var response = await client.PutAsJsonAsync($"api/Renters/{id}", model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Empresa atualizada com sucesso!";
                    return RedirectToAction(nameof(List));
                }

                ModelState.AddModelError("", "Erro ao atualizar empresa na API.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var client = GetAuthenticatedClient();
            var response = await client.DeleteAsync($"api/Renters/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Empresa removida com sucesso!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = "Erro ao remover a empresa: " + error;
            }

            return RedirectToAction(nameof(List));
        }

        [Authorize(Roles = "RENTER,ADMIN")]
        public async Task<IActionResult> MyHousings()
        {
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync("api/Housings/my-housings");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var myHousings = JsonSerializer.Deserialize<List<HousingViewModel>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(myHousings);
            }

            TempData["ErrorMessage"] = "Erro ao carregar os seus alojamentos.";
            return View(new List<HousingViewModel>());
        }
    }
}