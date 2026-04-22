using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Resend;
using Scalar.AspNetCore;
using Stripe;
using YpsiMarketXPrint.API.Data;
using YpsiMarketXPrint.API.Models;
using YpsiMarketXPrint.API.Services;

namespace YpsiMarketXPrint.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Database
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 45)),
                    mySqlOptions =>
                        mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 10,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null
                        )
                )
            );

            // Stripe
            StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

            // Allow requests from the React frontend during development
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllowFrontend",
                    policy =>
                    {
                        policy
                            .WithOrigins("http://localhost:5173")
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                );
            });

            // JWT Authentication
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            builder
                .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                        ),
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // Email service
            builder.Services.AddHttpClient<IResend, ResendClient>();
            builder.Services.Configure<ResendClientOptions>(options =>
            {
                options.ApiToken = builder.Configuration["Resend:ApiKey"]!;
            });
            builder.Services.AddSingleton<EmailService>();

            // Rate limiting — keyed by IP address (X-Forwarded-For respected if configured)
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                static string KeyBy(HttpContext ctx) =>
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Tight: login + password reset — brute-force / enumeration targets
                options.AddPolicy("auth-strict", ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(KeyBy(ctx), _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        }));

                // Medium: registration, email-triggering, checkout
                options.AddPolicy("auth-standard", ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(KeyBy(ctx), _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        }));

                // Loose global default for everything else
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(KeyBy(ctx), _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 120,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        }));
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("AllowFrontend");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Retry migration on startup in case MySQL isn't ready yet
            var retries = 0;
            while (retries < 10)
            {
                try
                {
                    using (var scope = app.Services.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                        db.Database.Migrate();

                        if (!db.Users.Any(u => u.UserType == UserType.Admin))
                        {
                            db.Users.Add(
                                new User
                                {
                                    FirstName = "Admin",
                                    LastName = "User",
                                    Email = config["Seed:AdminEmail"]!,
                                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                                        config["Seed:AdminPassword"]!
                                    ),
                                    UserType = UserType.Admin,
                                }
                            );
                            db.SaveChanges();
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    retries++;
                    Console.WriteLine(
                        $"Migration attempt {retries} failed: {ex.Message}. Retrying in 5s..."
                    );
                    Thread.Sleep(5000);
                }
            }

            app.Run();
        }
    }
}
