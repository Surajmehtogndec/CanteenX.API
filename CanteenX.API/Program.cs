using CanteenX.API.Data;
using CanteenX.API.Helpers;
using CanteenX.API.Model;
using CanteenX.API.Services;
using CanteenX.API.Services.HomeService;
using CanteenX.API.Services.AdminServices;
using CanteenX.API.Services.Auth;
using CanteenX.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CanteenX.API.Configurations;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CanteenX.API.Services.PaymentsServices;
using CanteenX.API.Services.PaymentsServices.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// database connection
builder.Services.AddDbContext<CanteenxdbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),

        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )


    )

    );

// RazorPay Interigation
builder.Services.Configure<RazorpaySettings>(
    builder.Configuration.GetSection("Razorpay"));

// =====================================================
// CORS CONFIGURATION
// =====================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMVCFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:7180") // Yahan apne MVC project ka actual URL daalein
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Cookies (JWT/Refresh Token) ke liye zaroori hai
    });
});


// =====================================================
// ASP.NET CORE IDENTITY
// =====================================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password Policy
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // User Policy
        options.User.RequireUniqueEmail = true;

        // Account Lockout
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<CanteenxdbContext>()
    .AddDefaultTokenProviders();

// token life time configuration

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(15);
});


// =====================================================
// JWT CONFIGURATION
// =====================================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT Key is missing from appsettings.json.");
}


builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ValidateIssuer = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidateAudience = true,

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };
    })
    .AddGoogle(options =>
    {

        options.ClientId =
                builder.Configuration[
                    "Authentication:Google:ClientId"]!;

        options.ClientSecret =
       builder.Configuration[
           "Authentication:Google:ClientSecret"]!;

        options.CallbackPath =
       "/api/auth/google-callback";

    });


// =====================================================
// APPLICATION SERVICES
// =====================================================
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IAdminServices, AdminServices>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPaymentsServices, PaymentsServices>();
// =====================================================
// CONTROLLERS + SWAGGER
// =====================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//http factory
builder.Services.AddHttpClient();
var app = builder.Build();

// =====================================================
// SEED IDENTITY ROLES
// =====================================================

using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

    await RoleSeeder.SeedRolesAsync(roleManager);
}
// =====================================================
// HTTP REQUEST PIPELINE
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowMVCFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
