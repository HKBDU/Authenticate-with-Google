using System.Security.Claims;
using AuthGG.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthGG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("google/login")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        var frontendUrl = configuration["Cors:FrontendUrl"] ?? "http://localhost:5173";
        var properties = new AuthenticationProperties
        {
            RedirectUri = $"{frontendUrl.TrimEnd('/')}/auth/callback"
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public IActionResult Me() => Ok(new
    {
        subject = User.FindFirstValue(ClaimTypes.NameIdentifier),
        email = User.FindFirstValue(ClaimTypes.Email),
        name = User.FindFirstValue(ClaimTypes.Name),
        picture = User.FindFirstValue("picture")
    });

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
