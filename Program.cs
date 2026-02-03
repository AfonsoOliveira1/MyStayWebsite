using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
/////////////////////////////////////////////////////////////////////////////////////////
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization()
    .AddNewtonsoftJson();
/////////////////////////////////////////////////////////////////////////////////////////
//builder.Services.AddControllersWithViews().AddNewtonsoftJson(); duplicado


// Isto cria a identtidade do useer
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });


builder.Services.AddHttpClient("Booking.API", client =>
{
    client.BaseAddress = new Uri("https://localhost:7117/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();
///////////////////////////////////////////////////////////////////
var supportedCultures = new[]
{
    new CultureInfo("pt-PT"),
    new CultureInfo("en-US")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-PT"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};
///////////////////////////////////////////////////////////////////
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseRouting();
app.UseRequestLocalization(localizationOptions);
//app.UseAuthentication(); está em comentario pois está duplicado "pq que n apagar?" pode dar errado sem tlv
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();