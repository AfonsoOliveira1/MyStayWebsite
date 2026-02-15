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

        // get
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
            return View(new List<RenterViewModel>());
        }

        // get
        public IActionResult Create() => View();

        // post
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
                    TempData["Message"] = "Novo Renter criado com sucesso!";
                    return RedirectToAction(nameof(List));
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ModelState.AddModelError("", "Sessão expirada. Por favor, faça login novamente.");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Erro API: {error}");
                    ModelState.AddModelError("", "Erro ao criar Renter na API.");
                }
            }
            return View(model);
        }

        //get
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

        //post
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
                    TempData["Message"] = "Empresa atualizada com sucesso!";
                    return RedirectToAction(nameof(List));
                }
                ModelState.AddModelError("", "Erro ao atualizar na API.");
            }
            return View(model);
        }

        // post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var client = GetAuthenticatedClient();
            var response = await client.DeleteAsync($"api/Renters/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Renter removido com sucesso!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = !string.IsNullOrEmpty(error) ? error : "Erro ao remover o Renter.";
            }

            return RedirectToAction(nameof(List));
        }

        [Authorize(Roles = "RENTER,ADMIN")]
        public async Task<IActionResult> MyHousings()
        {
            var client = _clientFactory.CreateClient("Booking.API");
  
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Housings/my-housings");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var myHousings = JsonSerializer.Deserialize<List<HousingViewModel>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(myHousings);
            }

            return View(new List<HousingViewModel>());
        }
    }
}