var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews().AddNewtonsoftJson(); // Para JSON


builder.Services.AddHttpClient("Booking.API", client => 
{
    client.BaseAddress = new Uri("https://localhost:7117/"); 
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
