using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTracApp.Data;
using NetTracApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Get the connection string from the configuration file
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add and configure the database context with MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)),
    mysqlOptions =>
    {
        // Enable retry on failure for database connections
        mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

// Add exception filter for database-related pages
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure identity services with role-based authentication and authorization
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddRoles<IdentityRole>() // Add role support
.AddEntityFrameworkStores<ApplicationDbContext>();

// Configure the authentication cookie to avoid unnecessary logouts
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/Index"; // Redirect to login if unauthenticated
    options.AccessDeniedPath = "/Login/AccessDenied"; // Redirect on access denied
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Extend cookie lifetime
    options.SlidingExpiration = true; // Renew cookie with each request
});

// Add support for controllers and views
builder.Services.AddControllersWithViews();

// Register CsvService for dependency injection
builder.Services.AddScoped<CsvService>();

// Configure logging to output to console and debug window
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

var app = builder.Build();

// Configure middleware for different environments
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Ensure authentication middleware is enabled
app.UseAuthorization(); // Ensure authorization middleware is enabled

// Configure default route pattern for controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Change default route to Home/Index

// Map Razor Pages (if needed)
app.MapRazorPages();

app.Run();
