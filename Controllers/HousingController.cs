using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Booking.Web.Controllers
{
    [Route("Housing")]
    public class HousingController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public HousingController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        private async Task<HttpClient> GetAuthorizedClient()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = await HttpContext.GetTokenAsync("access_token")
                        ?? User.FindFirst("JWToken")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        [HttpGet("List")]
        public async Task<IActionResult> List(decimal? minPrice, decimal? maxPrice, int? cityId)
        {
            var client = _clientFactory.CreateClient("Booking.API");

            try
            {
                var housings = await client.GetFromJsonAsync<List<HousingViewModel>>("api/Housings")
                               ?? new List<HousingViewModel>();

                var response = await client.GetAsync("api/Cities");
                IEnumerable<CityViewModel> cities = new List<CityViewModel>();
                if (response.IsSuccessStatusCode)
                {
                    cities = await response.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>()
                             ?? new List<CityViewModel>();
                }

                // viewbags 
                var culture = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
                string cityNameField = culture.StartsWith("pt") ? "Citynamept" : "Citynameen";
                ViewBag.Cities = new SelectList(cities, "Id", cityNameField, cityId);

                // filtros
                if (minPrice.HasValue)
                    housings = housings.Where(h => h.PricePerNight >= minPrice).ToList();
                if (maxPrice.HasValue)
                    housings = housings.Where(h => h.PricePerNight <= maxPrice).ToList();
                if (cityId.HasValue)
                    housings = housings.Where(h => h.CityId == cityId).ToList();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return PartialView("_HousingCards", housings);

                return View("~/Views/Housing/List.cshtml", housings);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Erro ao carregar os alojamentos.";
                return View("~/Views/Housing/List.cshtml", new List<HousingViewModel>());
            }
        }

        [Authorize(Roles = "RENTER,ADMIN")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateCitiesDropdown();
            return View("Create");
        }

        [Authorize(Roles = "RENTER,ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HousingCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCitiesDropdown();
                return View(model);
            }

            var client = await GetAuthorizedClient();
            var response = await client.PostAsJsonAsync("api/Housings", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Alojamento enviado para aprovação!";
                return RedirectToAction("List");
            }

            TempData["ErrorMessage"] = "Erro ao criar alojamento.";
            await PopulateCitiesDropdown();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Reserve(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            var client = await GetAuthorizedClient();

            var housing = await client.GetFromJsonAsync<HousingViewModel>("api/Housings/" + id);
            if (housing == null) return NotFound();

            var model = new HousingBookingViewModel
            {
                HousingId = housing.Id,
                CustomerId = int.Parse(userIdStr),
                HousingName = housing.Name,
                PricePerNight = housing.PricePerNight,
                CheckInDate = DateTime.Now,
                CheckOutDate = DateTime.Now.AddDays(1),
                Description = housing.Description,
                Ratings = housing.Ratings?.Select(r => new HousingRatingViewModel
                {
                    Score = r.Score,
                    Comment = r.Comment,
                    CustomerName = r.CustomerName
                }).ToList() ?? new List<HousingRatingViewModel>()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetBookedDates(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.GetAsync("api/HousingBookings/occupied-dates/" + id);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return Content(data, "application/json");
            }
            return BadRequest();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReservation(HousingBookingViewModel model)
        {
            var client = await GetAuthorizedClient();

            //validar datas
            var checkResponse = await client.GetAsync("api/HousingBookings/occupied-dates/" + model.HousingId);
            if (checkResponse.IsSuccessStatusCode)
            {
                var occupied = await checkResponse.Content.ReadFromJsonAsync<List<OccupiedRangeDto>>();
                var checkIn = DateOnly.FromDateTime(model.CheckInDate);
                var checkOut = DateOnly.FromDateTime(model.CheckOutDate);

                bool isOverlapping = occupied?.Any(o =>
                    (checkIn >= o.from && checkIn < o.to) ||
                    (checkOut > o.from && checkOut <= o.to) ||
                    (checkIn <= o.from && checkOut >= o.to)
                ) ?? false;

                if (isOverlapping)
                {
                    ModelState.AddModelError("", "As datas selecionadas já não estão disponíveis. Por favor, escolha outro período.");
                    return View("Reserve", model);
                }
            }

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
                TempData["SuccessMessage"] = "Reserva guardada na base de dados!";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = "A API rejeitou a reserva: " + errorBody;
                return View("Reserve", model);
            }
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> PendingApprovals()
        {
            var client = await GetAuthorizedClient();
            var response = await client.GetAsync("api/Housings/pending");

            var housings = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<HousingViewModel>>()
                : new List<HousingViewModel>();

            return View(housings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Approve(int id, decimal commissionRate, string description)
        {
            var client = await GetAuthorizedClient();
            var approvalData = new { CommissionRate = commissionRate, Description = description };
            var response = await client.PostAsJsonAsync("api/Housings/" + id + "/approve", approvalData);

            if (response.IsSuccessStatusCode)
                TempData["SuccessMessage"] = "Alojamento aprovado!";
            else
                TempData["ErrorMessage"] = "Erro ao aprovar alojamento: " + response.StatusCode;

            return RedirectToAction("PendingApprovals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Reject(int id)
        {
            var client = await GetAuthorizedClient();
            var response = await client.PostAsync("api/Housings/" + id + "/reject", null);

            if (response.IsSuccessStatusCode)
                TempData["SuccessMessage"] = "Alojamento rejeitado!";
            else
                TempData["ErrorMessage"] = "Erro ao rejeitar alojamento na API.";

            return RedirectToAction("PendingApprovals");
        }

        [Authorize(Roles = "RENTER")]
        public async Task<IActionResult> MyHousings()
        {
            var client = await GetAuthorizedClient();
            var response = await client.GetAsync("api/Housings/MyHousings");

            var housings = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<HousingViewModel>>()
                : new List<HousingViewModel>();

            return View(housings);
        }

        [HttpGet]
        [Authorize(Roles = "RENTER")]
        public async Task<IActionResult> Cancel(int id)
        {
            var client = await GetAuthorizedClient();
            var unlock = await client.PostAsync($"api/Housings/unlock/{id}", null);

            if (unlock.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                TempData["ErrorMessage"] = "Erro ao desbloquear.";
                return RedirectToAction("MyHousings");
            }
            return RedirectToAction("MyHousings");
        }

        [HttpGet]
        [Authorize(Roles = "RENTER")]
        public async Task<IActionResult> Edit(int id)
        {
            var client = await GetAuthorizedClient();
            var lockhouse = await client.PostAsync($"api/Housings/lock/{id}", null);

            if (lockhouse.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                TempData["ErrorMessage"] = "Outro utilizador está a editar este alojamento.";
                return RedirectToAction("MyHousings");
            }

            var housing = await client.GetFromJsonAsync<HousingViewModel>("api/Housings/" + id);
            if (housing == null) return NotFound();

            await PopulateCitiesDropdown(housing.CityId);

            return View(housing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RENTER")]
        public async Task<IActionResult> Edit(HousingViewModel model)
        {
            ModelState.Remove("HousingRatingViewModel");
            ModelState.Remove("CityName");
            ModelState.Remove("ApprovalStatus");

            if (model.NewPrice.HasValue && model.NewPrice < 0)
            {
                ModelState.AddModelError("", "Preço inválido");
            }

            if (!ModelState.IsValid)
            {
                await PopulateCitiesDropdown(model.CityId);
                return View(model);
            }

            var client = await GetAuthorizedClient();
            var updateDto = new
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                PricePerNight = model.PricePerNight,
                NewPrice = model.NewPrice,
                CityId = model.CityId,
                ImageUrl = model.ImageUrl,
                CompanyId = model.CompanyId,
                IsAvailable = model.IsAvailable
            };

            var response = await client.PutAsJsonAsync("api/Housings/" + model.Id, updateDto);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Alojamento atualizado com sucesso!";
                await client.PostAsync($"api/Housings/unlock/{model.Id}", null);
                return RedirectToAction("MyHousings");
            }

            TempData["ErrorMessage"] = "Erro ao atualizar alojamento.";
            await PopulateCitiesDropdown(model.CityId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarRating(int housingId, int score, string comment)
        {
            if (housingId <= 0) return RedirectToAction("MyBookings", "Bookings");

            var client = await GetAuthorizedClient();
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            // ve se pode avaliar
            var responseReservas = await client.GetAsync("api/HousingBookings/my-bookings/" + userIdClaim);
            if (responseReservas.IsSuccessStatusCode)
            {
                var reservas = await responseReservas.Content.ReadFromJsonAsync<List<HousingBookingViewModel>>();
                DateOnly hoje = DateOnly.FromDateTime(DateTime.Now);
                var podeAvaliar = reservas?.Any(r => r.HousingId == housingId && DateOnly.FromDateTime(r.CheckInDate) <= hoje) ?? false;

                if (!podeAvaliar)
                {
                    TempData["ErrorMessage"] = "Só pode avaliar alojamentos onde já esteve.";
                    return RedirectToAction("MyBookings", "Bookings");
                }
            }

            var ratingData = new { housingid = housingId, customerid = int.Parse(userIdClaim), score = score, comment = comment };
            await client.PostAsJsonAsync("api/HousingRatings", ratingData);
            TempData["SuccessMessage"] = "Avaliação enviada!";

            return RedirectToAction("MyBookings", "Bookings");
        }

        [HttpGet]
        public IActionResult UpdatePrice(int id)
        {
            return View(new HousingPriceUpdateViewModel { Id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ApplyCommission(int id, decimal newRate)
        {
            if (newRate < 0 || newRate > 100)
            {
                TempData["ErrorMessage"] = "Percentagem inválida.";
                return RedirectToAction("List");
            }

            var client = await GetAuthorizedClient();
            var url = "api/Housings/" + id + "/commission";
            var dto = new { BookingCommissionRate = newRate };
            var response = await client.PutAsJsonAsync(url, dto);

            if (!response.IsSuccessStatusCode)
                TempData["ErrorMessage"] = "Erro ao atualizar comissão.";
            else
                TempData["SuccessMessage"] = "Comissão atualizada com sucesso!";

            return RedirectToAction("List");
        }

        [Authorize(Roles = "RENTER")]
        [HttpGet("Earnings")]
        public async Task<IActionResult> Earnings()
        {
            var client = await GetAuthorizedClient();
            var response = await client.GetAsync("api/Housings/FinanceSummary");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<RenterFinanceViewModel>();
                return View("~/Views/Renter/Earnings.cshtml", data);
            }

            return View("~/Views/Renter/Earnings.cshtml", new RenterFinanceViewModel { Bookings = new List<HousingBookingHistoryViewModel>() });
        }

        private async Task PopulateCitiesDropdown(int? selectedId = null)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.GetAsync("api/Cities");
            IEnumerable<CityViewModel> cities = new List<CityViewModel>();
            if (response.IsSuccessStatusCode)
            {
                cities = await response.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>()
                         ?? new List<CityViewModel>();
            }
            ViewBag.Cities = new SelectList(cities, "Id", "Citynamept", selectedId);
        }
    }
}