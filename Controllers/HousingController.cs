using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace Booking.Web.Controllers
{
    public class HousingController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public HousingController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> List(decimal? minPrice, decimal? maxPrice, int? cityId)
        {
            var client = _clientFactory.CreateClient("Booking.API");

            var housings = await client.GetFromJsonAsync<List<HousingViewModel>>("api/Housings")
                           ?? new List<HousingViewModel>();

            var response = await client.GetAsync("api/Cities");
            IEnumerable<CityViewModel> cities = new List<CityViewModel>();

            if (response.IsSuccessStatusCode)
            {
                cities = await response.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>()
                         ?? new List<CityViewModel>();
            }

           
            ViewBag.Cities = new SelectList(cities, "Id", "Citynamept", cityId);

            //   filtros 
            if (minPrice.HasValue)
                housings = housings.Where(h => h.PricePerNight >= minPrice).ToList();

            if (maxPrice.HasValue)
                housings = housings.Where(h => h.PricePerNight <= maxPrice).ToList();

            if (cityId.HasValue)
                housings = housings.Where(h => h.CityId == cityId).ToList();

            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_HousingCards", housings);

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

            
            ViewBag.Cities = new SelectList(cities, "Id", "Citynamept");

            return View("Create");
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
            var cities = citiesResponse.IsSuccessStatusCode
                ? await citiesResponse.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>()
                : new List<CityViewModel>();

            ViewBag.Cities = new SelectList(cities, "Id", "Citynamept");

            return View(model);
        }

        [HttpGet]
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
        public async Task<IActionResult> ConfirmReservation(HousingBookingViewModel model)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Garantimos que os nomes das propriedades são idênticos ao DTO da API (case-sensitive às vezes importa)
            var bookingData = new
            {
                Housingid = model.HousingId,
                Customerid = model.CustomerId,
                Checkindate = model.CheckInDate,
                Checkoutdate = model.CheckOutDate
            };

            var response = await client.PostAsJsonAsync("api/HousingBookings", bookingData);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Reserva guardada na base de dados!";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // Se falhou, vamos ler o porquê
                var errorBody = await response.Content.ReadAsStringAsync();

                // Isto vai ajudar-te a ver no "Output" do Visual Studio qual o erro real
                System.Diagnostics.Debug.WriteLine($"Erro da API: {errorBody}");

                ModelState.AddModelError("", "A API rejeitou a reserva. Verifique se as datas estão disponíveis.");
                return View("Reserve", model);
            }
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> PendingApprovals()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Housings/pending");
            var housings = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<HousingViewModel>>()
                : new List<HousingViewModel>();

            return View(housings);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Approve(int id, decimal commissionRate)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var approvalData = new { CommissionRate = commissionRate };

         
            string url = "api/Housings/" + id + "/approve";

            var response = await client.PostAsJsonAsync(url, approvalData);

            if (response.IsSuccessStatusCode)
            { 
                TempData["Success"] = "Alojamento aprovado com sucesso! Taxa: " + commissionRate + "%";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "Erro ao aprovar: " + error;
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
                TempData["Success"] = "Alojamento rejeitado.";
            else
                TempData["Error"] = "Erro ao comunicar com a API para rejeição.";

            return RedirectToAction("PendingApprovals");
        }

        [Authorize(Roles = "RENTER")]
        public async Task<IActionResult> MyHousings()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Housings/MyHousings");
            var housings = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<HousingViewModel>>()
                : new List<HousingViewModel>();

            return View(housings);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;

            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var housing = await client.GetFromJsonAsync<HousingViewModel>("api/Housings/" + id);
            if (housing == null) return NotFound();

            var citiesResponse = await client.GetAsync("api/Cities");
            var cities = await citiesResponse.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>()
                          ?? new List<CityViewModel>();

            ViewBag.Cities = new SelectList(cities, "Id", "Citynamept", housing.CityId);

            return View(housing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HousingViewModel model)
        {
            //  limpa validacoes dos campos que n estao no formulario
            ModelState.Remove("HousingRatingViewModel");
            ModelState.Remove("CityName");
            ModelState.Remove("ApprovalStatus");

            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var updateDto = new
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                PricePerNight = model.PricePerNight,
                CityId = model.CityId,
                ImageUrl = model.ImageUrl,
                CompanyId = model.CompanyId,
                IsAvailable = model.IsAvailable
            };

           
            var response = await client.PutAsJsonAsync("api/Housings/" + model.Id, updateDto);


            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Alojamento atualizado com sucesso!";
                return RedirectToAction("MyHousings", "Housing");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            TempData["Error"] = "A API não autorizou a gravação: " + errorContent;

            var citiesResponse = await client.GetAsync("api/Cities");
            var cities = await citiesResponse.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>()
                         ?? new List<CityViewModel>();
            ViewBag.Cities = new SelectList(cities, "Id", "Citynamept", model.CityId);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EnviarRating(int housingId, int score, string comment)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = await HttpContext.GetTokenAsync("access_token") ?? User.FindFirst("JWToken")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var ratingData = new
            {
                housingid = housingId,
                customerid = int.Parse(userId),
                score = score,
                comment = comment
            };

            var response = await client.PostAsJsonAsync("api/HousingRatings", ratingData);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Avaliação guardada!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "Erro na API: " + error;
            }

            return RedirectToAction("MyBookings", "Bookings");
        }
    }


}