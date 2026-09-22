using System.Threading.RateLimiting;
using Scalar.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using AccountFlow.Backend.Validators;
using AccountFlow.Backend.Services;
using AccountFlow.Backend.Options;
using AccountFlow.Backend.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Fail fast on invalid JWT settings instead of discovering them on the first login.
var jwtKey = builder.Configuration["AppSettings:Token"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.StartsWith("REPLACE_WITH", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "AppSettings:Token is not configured. Set a long random secret via user-secrets or appsettings.Development.json.");
}
if (System.Text.Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "AppSettings:Token must be at least 32 bytes when encoded as UTF-8.");
}

var jwtIssuer = builder.Configuration["AppSettings:Issuer"];
if (string.IsNullOrWhiteSpace(jwtIssuer))
    throw new InvalidOperationException("AppSettings:Issuer must not be empty.");

var jwtAudience = builder.Configuration["AppSettings:Audience"];
if (string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("AppSettings:Audience must not be empty.");

// Add services to the container.
var corsPolicyName = "AllowFrontend";
var frontendOrigin = (builder.Configuration["AppSettings:FrontendUrl"] ?? "http://localhost:5173")
    .TrimEnd('/');
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicyName,
        policy =>
        {
            // Bearer tokens are sent in the Authorization header, so AllowCredentials (cookies) isn't needed.
            policy.WithOrigins(frontendOrigin)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registers every AbstractValidator in this assembly with a single call.
builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddScoped<SessionVersionValidator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.Configure<GmailOptions>(builder.Configuration.GetSection(GmailOptions.GmailOptionsKey));
builder.Services.AddOptions<AuthOptions>()
    .BindConfiguration(AuthOptions.SectionKey)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Consistent ProblemDetails responses plus a catch-all handler for unhandled exceptions.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Throttle the auth endpoints to slow down brute-force / credential-stuffing attempts.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuerSigningKey = true,
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var validator = context.HttpContext.RequestServices.GetRequiredService<SessionVersionValidator>();
            if (context.Principal is null ||
                !await validator.IsCurrentAsync(context.Principal, context.HttpContext.RequestAborted))
            {
                context.Fail("Session has been revoked.");
            }
        }
    };
});

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// app.UseHttpsRedirection();
app.UseCors(corsPolicyName);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
