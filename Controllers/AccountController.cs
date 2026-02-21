using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Protocol;
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
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/users/login", model);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();

                if (loginResponse != null && loginResponse.User != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, loginResponse.User.Id.ToString()),
                        new Claim(ClaimTypes.Name, loginResponse.User.Name ?? loginResponse.User.Email),
                        new Claim(ClaimTypes.Email, loginResponse.User.Email),
                        new Claim(ClaimTypes.Role, loginResponse.User.Role),    
                        new Claim("JWToken", loginResponse.Token)
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
            // se formulário foi bem preenchido
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
            if (string.IsNullOrEmpty(model.Email))
            {
                return RedirectToAction("Register");
            }

            // guardar dados
            TempData["RegisterModel"] = JsonSerializer.Serialize(model);
            TempData["Email"] = model.Email;

            // manter dados 
            TempData.Keep("RegisterModel");
            TempData.Keep("Email");

            // passar o email 
            return View(new VerifyCodeDTO { Email = model.Email });
        }

        [HttpPost]
        public async Task<IActionResult> ResendCodeConfirm(string Email)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/Email/send-code", new VerifyCodeDTO { Email = Email, Subject = "2FA Confirm your Email - MyStay", Body = "Thanks for joining MyStay,\nThis code will expire in 5 minutes. Do not share it with anyone.\nYour MyStay verification code is" });

            if (!response.IsSuccessStatusCode)
            {
                //le o conteúdo da resposta de erro
                var content = await response.Content.ReadAsStringAsync();
                string message;

                var apiResponse = JsonSerializer
                    .Deserialize<ApiMessage>(content);
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

            // Verificar code
            var response = await client.PostAsJsonAsync("api/Email/verify-code", model);

            if (!response.IsSuccessStatusCode)
            {
                TempData.Keep("RegisterModel");
                TempData.Keep("Email");
                ModelState.AddModelError("", "Código incorreto.");
                return View(model);
            }

            // recuperar os dados do registo
            var registerModelJson = TempData["RegisterModel"] as string;
            if (string.IsNullOrEmpty(registerModelJson))
            {
                return RedirectToAction("Register");
            }

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
            else
            { 
                var errorDetail = await registerResponse.Content.ReadAsStringAsync();
                ModelState.AddModelError("", "Erro da API:" +"{errorDetail}");

                TempData.Keep("RegisterModel");
                TempData.Keep("Email");
                return View(model);
            }
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
                ModelState.AddModelError("", "Code failed.");
                return View("VerifyCode", new VerifyCodeDTO { Email = model.Email });
            }
            
            var identity = (ClaimsIdentity)User.Identity;

            var confirmed = User.Claims.FirstOrDefault(c => c.Type == "EmailConfirmed")?.Value;
            var confirmedAt = User.Claims.FirstOrDefault(c => c.Type == "EmailConfirmedAt")?.Value;

            if(confirmed == "true" && confirmedAt != null)
            {
                identity.RemoveClaim(identity.FindFirst("EmailConfirmed"));
                identity.RemoveClaim(identity.FindFirst("EmailConfirmedAt"));
            }

            identity.AddClaim(new Claim("EmailConfirmed", "true"));
            identity.AddClaim(new Claim("EmailConfirmedAt", DateTime.UtcNow.ToString("o")));

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ResendCode(string Email)
        {
            VerifyCodeDTO model = new VerifyCodeDTO { Email = Email, Subject = "2FA Verify your Email - MyStay", Body = "Welcome back to MyStay,\nThis code will expire in 5 minutes. Do not share it with anyone.\nYour MyStay verification code is" };
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/Email/send-code", model);

                if (!response.IsSuccessStatusCode)
            {
                //le o conteúdo da resposta de erro
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiMessage>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                ModelState.AddModelError("", apiResponse?.Message ?? "Falha ao enviar código.");
            }
            return View("VerifyCode", model);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var confirmed = User.Claims.FirstOrDefault(c => c.Type == "EmailConfirmed")?.Value;
            var confirmedAt = User.Claims.FirstOrDefault(c => c.Type == "EmailConfirmedAt")?.Value;

            if (confirmed != "true" || confirmedAt == null)
                return RedirectToAction("VerifyCode");

          
            if (DateTime.TryParse(confirmedAt, out var time))
            {
                if (DateTime.UtcNow - time > TimeSpan.FromMinutes(5))
                    return RedirectToAction("VerifyCode");
            }
            else
                return RedirectToAction("VerifyCode");
            

            var user = new UserUpdateDto
            {
                Id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"),
                Name = User.Identity?.Name ?? "Utilizador",
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer",
                RenterId = 0,
                AirlineId = 0
            };

            return View(user);
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

            // obter o id do user
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            profile.Id = int.Parse(userIdStr);

            // tokn JWT
            var token = User.FindFirst("JWToken")?.Value;
            var client = _clientFactory.CreateClient("Booking.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync("api/users/update", profile);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Perfil atualizado com sucesso!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("", "Erro ao atualizar o perfil na API.");
            return View("Profile", profile);
        }
    }
}