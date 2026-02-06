using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

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
                        // Agora o compilador já reconhece o .Id porque usamos UserViewModel
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

                    TempData["Message"] = "Welcome back!";
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Email ou password incorretos.");
            return View(model);
        }

        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");


            var response = await client.PostAsJsonAsync("api/users/register", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Registration failed. Try a different email.");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        public void Email2FA(string toEmail)
        {
            string code = new Random().Next(100000, 999999).ToString();

            TempData["VerificationCode"] = code;
            TempData["Email"] = toEmail;

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("mystaystaff@gmail.com");
            mail.To.Add(toEmail);
            mail.Subject = "MyStay Verification Code (2FA)";
            mail.Body = $"Automated Email from MyStay,\nYour verification code is: {code}";

            //ENVIO REAL
            SmtpClient smtp;
            string domain = toEmail.Split('@')[1].ToLower();
            try
            {
                if (domain.Contains("gmail.com"))
                {
                    smtp = new SmtpClient("smtp.gmail.com", 587)
                    {
                        Credentials = new NetworkCredential("mystaystaff@gmail.com", "swqp mdpr bysr jxtz"),
                        EnableSsl = true
                    };
                    smtp.Send(mail);
                }
                else if (domain.Contains("outlook.com") || domain.Contains("hotmail.com") || domain.Contains("live.com"))
                {
                    smtp = new SmtpClient("smtp.office365.com", 587)
                    {
                        Credentials = new NetworkCredential("mystaystaff@gmail.com", "swqp mdpr bysr jxtz"),
                        EnableSsl = true
                    };
                    smtp.Send(mail);
                }
                else if (domain.Contains("yahoo.com"))
                {
                    smtp = new SmtpClient("smtp.mail.yahoo.com", 587)
                    {
                        Credentials = new NetworkCredential("mystaystaff@gmail.com", "swqp mdpr bysr jxtz"),
                        EnableSsl = true
                    };
                    smtp.Send(mail);
                }
                else
                {
                    ModelState.AddModelError("", "Error in sending the email.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to send email: " + ex.Message);
            }
        }
        public bool Verify2FA(VerifyCodeModel model)
        {
            string correctCode = TempData["VerificationCode"]?.ToString();

            if (correctCode == null)
            {
                ModelState.AddModelError("", "Verification expired. Request a new code.");
                return false;
            }

            if (model.Code != correctCode)
            {
                ModelState.AddModelError("", "Invalid Code");
                return false;
            }
            return true;
        }
        [HttpGet]
        public IActionResult VerifyCode(string Email)
        {
            Email2FA(Email);
            return View(new VerifyCodeModel());
        }

        [HttpPost]
        public IActionResult VerifyCode(VerifyCodeModel model)
        {
            if (Verify2FA(model) == false)
                return View(model);
            else
                return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult ResendCode(string Email)
        {
            Email2FA(Email);
            return View(new VerifyCodeModel());
        }


        [HttpGet]
        public IActionResult Profile()
        {
            var user = new UserUpdateDto
            {
                Id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                Name = User.Identity.Name ?? "Unknown",
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                Role = User.FindFirst(ClaimTypes.Role)?.Value,
                CompanyType = User.FindFirst("CompanyType")?.Value ?? "None"
            };
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ProfileEdit(UserUpdateDto profile, string ?newpass, string ?confirmpass)
        {
            var user = new UserUpdateDto
            {
                Id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                Name = User.Identity.Name ?? "Unknown",
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown",
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown",
                CompanyType = User.FindFirst("CompanyType")?.Value // Pode ser null para clientes
            };

            if(profile.Role == "Company" && profile.CompanyType == "None" || 
               profile.Role == "Customer" && profile.CompanyType == "Housing")
            {
                ModelState.AddModelError("", "Company type is required for company users or can not be a customer with companytype housing.");
                return View("Profile", user);
            }

            if (newpass != confirmpass)
            {
                ModelState.AddModelError("", "New password and confirmation do not match.");
                return View("Profile", user);
            }
            else { profile.Password = newpass; } //api da hash

            profile.Id = user.Id;

            var token = User.FindFirst("JWToken")?.Value;
            var client = _clientFactory.CreateClient("Booking.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.PutAsJsonAsync("api/users/update", profile);

            if(newpass != null)
            {
                return RedirectToAction("Logout");
            }
            else
            {
                return View("Profile", profile);
            }
        }
    }
}