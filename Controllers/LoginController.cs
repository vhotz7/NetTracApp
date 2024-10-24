using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

public class LoginController : Controller
{
    // GET: /Login/Index
    [HttpGet]
    public IActionResult Index()
    {
        // Render the Login view from /Views/Home/Login.cshtml
        return View("~/Views/Home/Login.cshtml");
    }

    // POST: /Login/Login
    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        // Simulate login logic; replace with your authentication logic
        if (password == "pw")
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };

            if (username == "t2")
            {
                claims.Add(new Claim(ClaimTypes.Role, "Tier2"));
                await SignInUser(claims);
                return RedirectToAction("Tier2Dashboard", "Tier2");
            }
            else if (username == "t3")
            {
                claims.Add(new Claim(ClaimTypes.Role, "Tier3"));
                await SignInUser(claims);
                return RedirectToAction("Tier3Dashboard", "Tier3");
            }
        }

        // If login fails, show the error message without redirecting
        ViewBag.Message = "Invalid username or password.";
        return View("~/Views/Home/Login.cshtml");
    }

    // Helper method to sign in the user
    private async Task SignInUser(List<Claim> claims)
    {
        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, // Keeps the user signed in
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) // Cookie expiration
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }

    // POST: /Login/Logout
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Login");
    }

    // Optional: GET version of Logout for use with anchor tags
    [HttpGet]
    public async Task<IActionResult> LogoutViaLink()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Login");
    }
}
