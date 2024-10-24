using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTracApp.Data;
using NetTracApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Get the connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configure database context with MySQL and retry logic
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)),
    mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

// Add exception filter for database-related pages (development only)
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity with role-based authentication and authorization
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true; // Require email confirmation to sign in
})
.AddRoles<IdentityRole>() // Enable role management
.AddEntityFrameworkStores<ApplicationDbContext>(); // Use ApplicationDbContext with Identity

// Configure cookie authentication to avoid unnecessary logouts
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index"; // Redirect to login if not authenticated
        options.AccessDeniedPath = "/Login/AccessDenied"; // Optional: Access Denied page
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20); // Adjust as needed
        options.SlidingExpiration = false; // Disable sliding expiration to require login again
        options.Cookie.HttpOnly = true; // Prevent client-side access to cookies
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Use secure cookies
        options.Cookie.SameSite = SameSiteMode.Strict; // Prevent CSRF attacks
    });

builder.Services.AddAuthorization(); // Add authorization services

// Register controllers, views, and Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Add Razor Pages support

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
    app.UseMigrationsEndPoint(); // Enable migrations endpoint for development
}
else
{
    app.UseExceptionHandler("/Home/Error"); // Use error handler in production
    app.UseHsts(); // Enable HSTS to enforce HTTPS
}

// Enable middleware for HTTPS redirection and static files
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); // Enable routing middleware

app.UseAuthentication(); // Enable authentication middleware
app.UseAuthorization(); // Enable authorization middleware

// Configure the default route pattern for controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

// Map Razor Pages (if needed)
app.MapRazorPages();

// Run the application
app.Run();
