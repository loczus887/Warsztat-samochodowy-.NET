using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Mappers;
using WorkshopManager.Models;
using WorkshopManager.Services.Interfaces;
using WorkshopManager.Services;
using WorkshopManager.BackgroundServices;
using DinkToPdf;
using DinkToPdf.Contracts;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Starting application");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // U¿ycie NLog jako providera logowania
    builder.Host.UseNLog();

    // Add services to the container.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    // Konfiguracja Identity (BEZ .AddDefaultUI())
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    // Rejestracja WSZYSTKICH serwisów
    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<IVehicleService, VehicleService>();
    builder.Services.AddScoped<IServiceOrderService, ServiceOrderService>();
    builder.Services.AddScoped<IPartService, PartService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();

    // Rejestracja maperów Mapperly
    builder.Services.AddScoped<CustomerMapper>();
    builder.Services.AddScoped<VehicleMapper>();
    builder.Services.AddScoped<ServiceOrderMapper>();
    builder.Services.AddScoped<ServiceTaskMapper>();
    builder.Services.AddScoped<PartMapper>();
    builder.Services.AddScoped<UsedPartMapper>();
    builder.Services.AddScoped<CommentMapper>();

    // DinkToPdf
    builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

    // Background Service
    builder.Services.AddHostedService<DailyReportBackgroundService>();

    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                await DbInitializer.Initialize(context, userManager, roleManager);
            }
            catch (Exception ex)
            {
                var scopedLogger = services.GetRequiredService<ILogger<Program>>();
                scopedLogger.LogError(ex, "An error occurred while seeding the database.");
            }
        }
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapRazorPages();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Error starting the application");
    throw;
}
finally
{
    LogManager.Shutdown();
}