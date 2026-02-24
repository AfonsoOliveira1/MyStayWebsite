using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Booking.web;
using Booking.web.Models; 
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options => {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    })
    .AddNewtonsoftJson();

//validacao de sessao
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                //dados no Cookie atual
                var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var cookieSessionId = context.Principal?.FindFirst("SessionId")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(cookieSessionId))
                {
                    return;
                }

                var clientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client = clientFactory.CreateClient("Booking.API");

               
                var response = await client.GetAsync("api/Users/check-session/" + userId);

                if (response.IsSuccessStatusCode)
                {
                    // ler o SessionId
                    var dbSessionId = (await response.Content.ReadAsStringAsync()).Trim('"');

                    // se o ID da bd mudou, este pc e expulso
                    if (cookieSessionId != dbSessionId)
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
            }
        };
    });

builder.Services.AddHttpClient("Booking.API", client =>
{
    client.BaseAddress = new Uri("https://localhost:7117/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();


var supportedCultures = new[]
{
    new CultureInfo("pt-PT"),
    new CultureInfo("en-US"),
    new CultureInfo("en")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-PT"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

localizationOptions.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// aplica as traduções
app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();