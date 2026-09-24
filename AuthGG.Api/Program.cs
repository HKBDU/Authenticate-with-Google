using AuthGG.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<UserStore>();

var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                     ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var googleClientId = builder.Configuration["Google:ClientId"]
                    ?? throw new InvalidOperationException("Google:ClientId chưa được cấu hình.");
var googleClientSecret = builder.Configuration["Google:ClientSecret"]
                        ?? throw new InvalidOperationException("Google:ClientSecret chưa được cấu hình.");
var frontendUrl = builder.Configuration["Cors:FrontendUrl"] ?? "http://localhost:5173";

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "authgg.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/api/auth/google/callback/";
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = false;
        options.Events.OnCreatingTicket = context =>
        {
            var identity = (ClaimsIdentity)context.Principal!.Identity!;
            var subject = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? identity.FindFirst("sub")?.Value;
            var email = identity.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Google không trả về thông tin tài khoản hợp lệ.");

            var name = identity.FindFirst(ClaimTypes.Name)?.Value ?? email;
            var picture = identity.FindFirst("picture")?.Value;
            context.HttpContext.RequestServices.GetRequiredService<UserStore>()
                .Upsert(subject, email, name, picture);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
