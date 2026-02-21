using Booking.web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Booking.web.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AdminController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Revenue()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Users/dashboard/finance");

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var summary = await response.Content.ReadFromJsonAsync<RevenueSummaryViewModel>(options);
                return View(summary);
            }
            return View(new RevenueSummaryViewModel());
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> FinancialDashboard()
        {
            return await GetFinancialData();
        }
        private async Task<IActionResult> GetFinancialData()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Users/dashboard/finance");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true 
                };

                var summary = JsonSerializer.Deserialize<RevenueSummaryViewModel>(jsonString, options);

                return View("Revenue", summary);
            }

            return View("Revenue", new RevenueSummaryViewModel());
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> EditCommission(int id)
        {
            return View(id);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> UpdateCommission(int id, decimal newRate)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = "api/Housings/" + id + "/update-commission";

            var response = await client.PatchAsJsonAsync(url, newRate);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("List", "Housing");
            }

            return BadRequest("Erro ao comunicar com a API");
        }
    }
}