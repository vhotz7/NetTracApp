using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTracApp.Data;
using NetTracApp.Services;

var builder = WebApplication.CreateBuilder(args);

// get the connection string from the configuration file
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// add and configure the database context with MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)),
    mysqlOptions =>
    {
        // enable retry on failure for database connections
        mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5, // maximum number of retry attempts
            maxRetryDelay: TimeSpan.FromSeconds(10), // delay between retries
            errorNumbersToAdd: null); // no specific error numbers to add for retry
    }));

// add exception filter for database-related pages
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// configure identity services for user authentication and authorization
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// add support for controllers and views
builder.Services.AddControllersWithViews();

// register the custom CsvService for dependency injection
builder.Services.AddScoped<CsvService>();

// configure logging to output to console and debug window
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

var app = builder.Build();

// configure middleware for different environments
if (app.Environment.IsDevelopment())
{
    // use developer-friendly database error pages in development
    app.UseMigrationsEndPoint();
}
else
{
    // use a generic error page in production
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // use HTTP Strict Transport Security
}

app.UseHttpsRedirection(); // redirect HTTP requests to HTTPS
app.UseStaticFiles(); // serve static files

app.UseRouting(); // configure routing

app.UseAuthentication(); // enable authentication middleware
app.UseAuthorization(); // enable authorization middleware

// configure default route pattern for controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

// Map Razor Pages (if needed)
app.MapRazorPages();

app.Run();