using EcoLilly.Data;
using Microsoft.EntityFrameworkCore;
using EcoLilly.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Session (existing)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register CashfreeService with typed HttpClient
builder.Services.AddHttpClient<CashfreeService>();
// Note: CashfreeService should have a constructor accepting IConfiguration and HttpClient.

var app = builder.Build();

// QuestPDF License
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();