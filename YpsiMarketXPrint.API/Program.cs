using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using YpsiMarketXPrint.API.Data;
using YpsiMarketXPrint.API.Models;

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

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("AllowFrontend");
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

                        if (!db.Users.Any(u => u.UserType == "admin"))
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
                                    UserType = "admin",
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
