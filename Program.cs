using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TriviaApp.Models;

public static class ProxySettings
{
    public static bool UseProxy = false;
    public static string ProxyAddress = "";
    public static int ProxyPort = 0;
    public static string ProxyUsername = "";
    public static string ProxyPassword = "";
}
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        var connectionString = builder.Configuration.GetConnectionString("DbContextConnection") ?? throw new
            InvalidOperationException("Connection string 'DbContextConnection' not found.");

        ProxySettings.UseProxy = builder.Configuration.GetValue<bool>("UseProxy");
        if (ProxySettings.UseProxy)
        {
            ProxySettings.ProxyAddress = builder.Configuration.GetValue<string>("ProxyAddress");
            ProxySettings.ProxyPort = builder.Configuration.GetValue<int>("ProxyPort");
            ProxySettings.ProxyUsername = builder.Configuration.GetValue<string>("ProxyUsername");
            ProxySettings.ProxyPassword = builder.Configuration.GetValue<string>("ProxyPassword");
        }

        builder.Services.AddDbContext<TriviaContext>(options => options
            .UseLazyLoadingProxies()
            .UseSqlite(connectionString));
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => {
            options.SlidingExpiration = true;
            options.LoginPath = new("/Account/Login");
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        WebApplication app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.MapStaticAssets();
        app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();
        TriviaApp.TriviaAPI.Client client = new();
        app.Run();
    }
}
