using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Booking.web; // Certifique-se que o namespace do SharedResource está correto

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração de Localização
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options => {
        // Força o uso da classe SharedResource para traduções de validação/modelos
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    })
    .AddNewtonsoftJson();

// 2. Configuração de Autenticação 
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

// 3. Configuração do HttpClient
builder.Services.AddHttpClient("Booking.API", client =>
{
    client.BaseAddress = new Uri("https://localhost:7117/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// 4. Configuração detalhada de idiomas
var supportedCultures = new[]
{
    new CultureInfo("pt-PT"),
    new CultureInfo("en-US"),
    new CultureInfo("en") // Adicionado 'en' genérico para compatibilidade
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-PT"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// Permite mudar o idioma via QueryString: ?culture=en-US
localizationOptions.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());

// 5. Pipeline de Execução
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles(); // Serve arquivos da wwwroot
app.UseRouting();

// O UseRequestLocalization deve vir DEPOIS de UseRouting e ANTES de UseAuthorization
app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();