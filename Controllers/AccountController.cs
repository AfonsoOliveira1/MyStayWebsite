using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Booking.web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AccountController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model, bool force = false)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");

            var response = await client.PostAsJsonAsync("api/users/login?force=" + force, model);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();

                //sessao ja ativa noutro pc
                if (loginResponse?.Status == "AlreadyLoggedIn")
                {
                    ViewBag.ShowForceLoginPrompt = true;
                    return View(model);
                }

                if (loginResponse?.User != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, loginResponse.User.Id.ToString()),
                        new Claim(ClaimTypes.Name, loginResponse.User.Name ?? loginResponse.User.Email),
                        new Claim(ClaimTypes.Email, loginResponse.User.Email),
                        new Claim(ClaimTypes.Role, loginResponse.User.Role),
                        new Claim("JWToken", loginResponse.Token),
                        new Claim("SessionId", loginResponse.SessionId) 
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity)
                    );

                    TempData["Message"] = "Bem-vindo de volta!";
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Email ou password incorretos.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var client = _clientFactory.CreateClient("Booking.API");

            var airlineResponse = await client.GetAsync("api/Airlines");
            var airlines = airlineResponse.IsSuccessStatusCode
                ? await airlineResponse.Content.ReadFromJsonAsync<List<AirlineViewModel>>()
                : new List<AirlineViewModel>();

            var renterResponse = await client.GetAsync("api/Renters");
            var renters = renterResponse.IsSuccessStatusCode
                ? await renterResponse.Content.ReadFromJsonAsync<List<RenterViewModel>>()
                : new List<RenterViewModel>();

            ViewBag.Airlines = airlines;
            ViewBag.Renters = renters;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");

            var userDto = new UserCreateDto
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Passwordhash,
                Role = model.Role,
                CompanyType = model.Role,
                CompanyId = model.SelectedCompanyId
            };

            var response = await client.PostAsJsonAsync("api/users/register", userDto);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Registo efetuado! Por favor faça login.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Falha no registo. Verifique se o email já está em uso.");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult ConfirmEmailCode(RegisterModel model)
        {
            if (string.IsNullOrEmpty(model.Email)) return RedirectToAction("Register");

            TempData["RegisterModel"] = JsonSerializer.Serialize(model);
            TempData["Email"] = model.Email;
            TempData.Keep("RegisterModel");
            TempData.Keep("Email");

            return View(new VerifyCodeDTO { Email = model.Email });
        }

        [HttpPost]
        public async Task<IActionResult> ResendCodeConfirm(string Email)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/Email/send-code", new VerifyCodeDTO
            {
                Email = Email,
                Subject = "2FA Confirm your Email - MyStay",
                Body = "Thanks for joining MyStay,\nYour verification code is"
            });

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiMessage>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                ModelState.AddModelError("", apiResponse?.Message ?? "Falha ao enviar código.");
            }
            return View("ConfirmEmailCode", new VerifyCodeDTO { Email = Email });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmEmailCode(VerifyCodeDTO model)
        {
            if (!ModelState.IsValid)
            {
                TempData.Keep("RegisterModel");
                TempData.Keep("Email");
                return View(model);
            }

            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/Email/verify-code", model);

            if (!response.IsSuccessStatusCode)
            {
                TempData.Keep("RegisterModel");
                TempData.Keep("Email");
                ModelState.AddModelError("", "Código incorreto.");
                return View(model);
            }

            var registerModelJson = TempData["RegisterModel"] as string;
            if (string.IsNullOrEmpty(registerModelJson)) return RedirectToAction("Register");

            RegisterModel regModel = JsonSerializer.Deserialize<RegisterModel>(registerModelJson);

            var userDto = new
            {
                Name = regModel.Name,
                Email = regModel.Email,
                Password = regModel.Passwordhash,
                Role = regModel.Role,
                CompanyId = regModel.SelectedCompanyId
            };

            var registerResponse = await client.PostAsJsonAsync("api/users/register", userDto);

            if (registerResponse.IsSuccessStatusCode)
            {
                TempData.Remove("RegisterModel");
                TempData["Message"] = "Registo efetuado com sucesso!";
                return RedirectToAction("Login");
            }

            var errorDetail = await registerResponse.Content.ReadAsStringAsync();
            ModelState.AddModelError("", "Erro ao registar: " + errorDetail);
            TempData.Keep("RegisterModel");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> VerifyCode() => View();

        [HttpPost]
        public async Task<IActionResult> VerifyCode(VerifyCodeDTO model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/Email/verify-code", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Código inválido.");
                return View(model);
            }

            var identity = (ClaimsIdentity)User.Identity;

            var existingConfirmed = identity.FindFirst("EmailConfirmed");
            if (existingConfirmed != null) identity.RemoveClaim(existingConfirmed);

            var existingDate = identity.FindFirst("EmailConfirmedAt");
            if (existingDate != null) identity.RemoveClaim(existingDate);

            identity.AddClaim(new Claim("EmailConfirmed", "true"));
            identity.AddClaim(new Claim("EmailConfirmedAt", DateTime.UtcNow.ToString("o")));

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr == null) return RedirectToAction("Login");

            var profile = new UserUpdateDto
            {
                Id = int.Parse(userIdStr),
                Name = User.FindFirst(ClaimTypes.Name)?.Value,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                Role = User.FindFirst(ClaimTypes.Role)?.Value,
            };

            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> ProfileEdit(UserUpdateDto profile, string? newpass, string? confirmpass)
        {
            if (!string.IsNullOrEmpty(newpass))
            {
                if (newpass != confirmpass)
                {
                    ModelState.AddModelError("", "A nova password e a confirmação não coincidem.");
                    return View("Profile", profile);
                }
                profile.Password = newpass;
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            profile.Id = int.Parse(userIdStr);
            var token = User.FindFirst("JWToken")?.Value;

            var client = _clientFactory.CreateClient("Booking.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync("api/users/update", profile);

            if (response.IsSuccessStatusCode)
            {
                var identity = (ClaimsIdentity)User.Identity;

                //atualizar os claims no Cookie
                void UpdateClaim(string type, string value)
                {
                    var c = identity.FindFirst(type);
                    if (c != null) identity.RemoveClaim(c);
                    identity.AddClaim(new Claim(type, value));
                }

                UpdateClaim(ClaimTypes.Name, profile.Name);
                UpdateClaim(ClaimTypes.Email, profile.Email);
                UpdateClaim(ClaimTypes.Role, profile.Role);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                TempData["Message"] = "Perfil atualizado com sucesso!";

                if (!string.IsNullOrEmpty(newpass)) return RedirectToAction("Logout");

                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("", "Erro ao atualizar o perfil na API.");
            return View("Profile", profile);
        }
    }
}