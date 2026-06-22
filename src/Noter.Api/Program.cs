using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Noter.Domain.Repositories;
using Noter.Inrastructure.Persistence.DbContexts;
using Noter.Inrastructure.Repositories;
using Npgsql;
using System.Text;
using System.Text.Json.Serialization;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Controllers
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

        // Database
        var azureConnectionString = builder.Configuration["AZURE_POSTGRESQL_CONNECTIONSTRING"]
            ?? builder.Configuration.GetConnectionString("AZURE_POSTGRESQL_CONNECTIONSTRING");

        if (string.IsNullOrWhiteSpace(azureConnectionString))
        {
            throw new InvalidOperationException(
                "Missing Azure PostgreSQL connection string. Set AZURE_POSTGRESQL_CONNECTIONSTRING in App Service configuration.");
        }

        var normalizedConnectionString = NormalizePostgresConnectionString(azureConnectionString);

        builder.Services.AddDbContext<NoterDbContext>(options =>
        {
            options.UseNpgsql(normalizedConnectionString);
        });

        // Repositories
        //builder.Services.AddScoped<ITaskRepository, TaskRepository>();

        // JWT
        //var key = Encoding.ASCII.GetBytes(
        //    builder.Configuration["JwtConfig:Secret"]!);

        //var tokenValidationParameters = new TokenValidationParameters
        //{
        //    ValidateIssuerSigningKey = true,
        //    IssuerSigningKey = new SymmetricSecurityKey(key),
        //    ValidateIssuer = false,
        //    ValidateAudience = false,
        //    RequireExpirationTime = false,
        //    ValidateLifetime = true
        //};

        //builder.Services.AddSingleton(tokenValidationParameters);

        //builder.Services
        //    .AddAuthentication(options =>
        //    {
        //        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        //        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        //    })
        //    .AddJwtBearer(options =>
        //    {
        //        options.TokenValidationParameters = tokenValidationParameters;
        //    });

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowVue", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });


        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Noter API",
                Version = "v1"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "������� JWT �����: Bearer {token}"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });


        builder.Services.AddScoped<IStudyGoalRepository, StudyGoalRepository>();
        builder.Services.AddScoped<IMilestoneRepository, MilestoneRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IStudySessionPlanRepository, StudySessionPlanRepository>();


        var app = builder.Build();

    await ApplyDatabaseMigrationsAsync(app);

        // Middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }


        app.UseCors("AllowVue");

        app.UseHttpsRedirection();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapFallbackToFile("index.html");

        app.Run();
    }

    private static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<NoterDbContext>();

        try
        {
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

            if (pendingMigrations.Count == 0)
            {
                logger.LogInformation("No pending EF Core migrations.");
                return;
            }

            logger.LogInformation("Applying {Count} pending EF Core migration(s).", pendingMigrations.Count);
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("EF Core migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to apply EF Core migrations during startup.");
            throw;
        }
    }

    private static string NormalizePostgresConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }
}