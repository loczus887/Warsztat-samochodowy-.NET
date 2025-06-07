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

// === DODAJ TEN DEBUG NA POCZĄTKU ===
var currentDirectory = Directory.GetCurrentDirectory();
var logsPath = Path.Combine(currentDirectory, "logs");

Console.WriteLine($"=== NLOG DEBUG ===");
Console.WriteLine($"Current Directory: {currentDirectory}");
Console.WriteLine($"Logs Path: {logsPath}");

// Sprawdź czy folder logs istnieje i utwórz
try
{
    if (!Directory.Exists(logsPath))
    {
        Directory.CreateDirectory(logsPath);
        Console.WriteLine($"Utworzono folder: {logsPath}");
    }
    else
    {
        Console.WriteLine($"Folder już istnieje: {logsPath}");
    }

    // Test zapisu pliku
    var testFile = Path.Combine(logsPath, "test.txt");
    await File.WriteAllTextAsync(testFile, $"Test zapisu: {DateTime.Now}");
    Console.WriteLine($"✅ Test zapisu udany: {testFile}");

    // Sprawdź czy plik istnieje
    if (File.Exists(testFile))
    {
        Console.WriteLine($"✅ Plik testowy istnieje, usuwam...");
        File.Delete(testFile);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Błąd uprawnień: {ex.Message}");
}

// Test NLog
logger.Info("=== TEST LOGU - START APLIKACJI ===");
logger.Error("=== TEST LOGU BŁĘDU ===");
Console.WriteLine("=== Sprawdź czy powyższe logi pojawiły się w pliku ===");
Console.WriteLine("=== END NLOG DEBUG ===");

logger.Debug("Starting application");
// === KONIEC DEBUGU ===

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseNLog();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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

    // Konfiguracja EmailSettings
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

    // Rejestracja serwisów
    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<IVehicleService, VehicleService>();
    builder.Services.AddScoped<IServiceOrderService, ServiceOrderService>();
    builder.Services.AddScoped<IPartService, PartService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IFileUploadService, FileUploadService>();
    builder.Services.AddScoped<IEmailService, EmailService>(); // NOWY SERWIS

    // Mappers
    builder.Services.AddScoped<CustomerMapper>();
    builder.Services.AddScoped<VehicleMapper>();
    builder.Services.AddScoped<ServiceOrderMapper>();
    builder.Services.AddScoped<ServiceTaskMapper>();
    builder.Services.AddScoped<PartMapper>();
    builder.Services.AddScoped<UsedPartMapper>();
    builder.Services.AddScoped<CommentMapper>();

    // PDF Generator
    builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

    // Background Services
    builder.Services.AddHostedService<DailyReportBackgroundService>();

    // API Documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "Workshop Manager API",
            Version = "v1",
            Description = "API dla systemu zarządzania warsztatem samochodowym"
        });
    });

    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Workshop Manager API v1");
            c.RoutePrefix = "swagger";
        });

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