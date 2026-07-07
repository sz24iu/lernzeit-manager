using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Noter.Application.HashingUnits;
using Noter.Domain.Entities.ConfigEntities;
using Noter.Domain.Entities.DbEntities;
using Noter.Domain.Entities.Dtos.MilestoneDto;
using Noter.Domain.Entities.Dtos.StudyGoalDto;
using Noter.Domain.Entities.Dtos.UserDto;
using Noter.Domain.Entities.Enums;
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

        var jwtSettings = builder.Configuration.GetSection("JwtConfig");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]!);

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ValidateIssuer = false,
            ValidateAudience = false,

            RequireExpirationTime = true,
            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
        };

        builder.Services.AddSingleton(tokenValidationParameters);

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
                Description = "Login JWT Token"
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

        builder.Services
            .AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.TokenValidationParameters = tokenValidationParameters;
            });

        builder.Services.AddScoped<IStudyGoalRepository, StudyGoalRepository>();
        builder.Services.AddScoped<IMilestoneRepository, MilestoneRepository>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IStudySessionPlanRepository, StudySessionPlanRepository>();
        builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
        builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));



        var app = builder.Build();

        await ApplyDatabaseMigrationsAsync(app);
        await SeedDemoDataAsync(app);

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

    private static async Task SeedDemoDataAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<NoterDbContext>();
        var user = await dbContext.Users
            .OrderBy(x => x.Email)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            logger.LogInformation("Skipping demo data seed because no users exist yet.");
            return;
        }

        var hasGoals = await dbContext.StudyGoals.AnyAsync();

        if (hasGoals)
        {
            logger.LogInformation("Skipping demo data seed because study goals already exist.");
            return;
        }

        var goal1 = new StudyGoal(new CreateStudyGoalDto
        {
            Title = "Klausurvorbereitung Statistik I",
            Description = "Wichtige Formeln wiederholen und Altklausuren loesen.",
            Type = GoalType.Exam,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            UserId = user.Id
        });

        var goal2 = new StudyGoal(new CreateStudyGoalDto
        {
            Title = "Hausarbeit Geschichte des 19. Jahrhunderts",
            Description = "Quellenrecherche, Gliederung und erste Rohfassung erstellen.",
            Type = GoalType.Assignment,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(40),
            UserId = user.Id
        });

        var goal3 = new StudyGoal(new CreateStudyGoalDto
        {
            Title = "Lernplan Anatomie I",
            Description = "Woechentliche Lernziele fuer Knochen, Muskeln und Organsysteme.",
            Type = GoalType.Module,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(70),
            UserId = user.Id
        });

        var goal4 = new StudyGoal(new CreateStudyGoalDto
        {
            Title = "Projektarbeit Psychologie: Lernmotivation",
            Description = "Literatur auswerten und ein kleines Befragungskonzept entwickeln.",
            Type = GoalType.Project,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(60),
            UserId = user.Id
        });

        dbContext.StudyGoals.AddRange(goal1, goal2, goal3, goal4);
        await dbContext.SaveChangesAsync();

        var milestone1 = new Milestone(new CreateMilestoneDto
        {
            StudyGoalId = goal1.Id,
            Title = "Zusammenfassung der Vorlesungen Kapitel 1-4"
        });

        var milestone2 = new Milestone(new CreateMilestoneDto
        {
            StudyGoalId = goal1.Id,
            Title = "Zwei Altklausuren unter Zeitdruck bearbeiten"
        });

        var milestone3 = new Milestone(new CreateMilestoneDto
        {
            StudyGoalId = goal2.Id,
            Title = "Mindestens 10 wissenschaftliche Quellen sammeln"
        });

        var milestone4 = new Milestone(new CreateMilestoneDto
        {
            StudyGoalId = goal2.Id,
            Title = "Gliederung und Einleitung fertigstellen"
        });

        var milestone5 = new Milestone(new CreateMilestoneDto
        {
            StudyGoalId = goal3.Id,
            Title = "Lernkarten fuer Bewegungsapparat erstellen"
        });

        var milestone6 = new Milestone(new CreateMilestoneDto
        {
            StudyGoalId = goal3.Id,
            Title = "Testatvorbereitung fuer Anatomie-Labor"
        });

        var milestone7 = new Milestone(new CreateMilestoneDto
        {
            StudyGoalId = goal4.Id,
            Title = "Forschungsfrage und Methodik definieren"
        });

        var milestone8 = new Milestone(new CreateMilestoneDto
        {
            StudyGoalId = goal4.Id,
            Title = "Erste Auswertung der Umfrageergebnisse"
        });

        // Mix von Stati fuer realistischere Testdaten im Frontend.
        milestone2.Status = GoalStatus.InProgress;
        milestone3.Status = GoalStatus.Completed;
        milestone5.Status = GoalStatus.InProgress;
        milestone7.Status = GoalStatus.Completed;

        dbContext.Milestones.AddRange(
            milestone1,
            milestone2,
            milestone3,
            milestone4,
            milestone5,
            milestone6,
            milestone7,
            milestone8);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Demo data seeded successfully.");
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
            }

            if (pendingMigrations.Count > 0)
            {
                logger.LogInformation("Applying {Count} pending EF Core migration(s).", pendingMigrations.Count);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("EF Core migrations applied successfully.");
            }

            await EnsureMilestoneSchemaCompatibilityAsync(dbContext, logger);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to apply EF Core migrations during startup.");
            throw;
        }
    }

    private static async Task EnsureMilestoneSchemaCompatibilityAsync(NoterDbContext dbContext, ILogger logger)
    {
        const string ensureMilestoneColumnsSql = @"
DO $$
BEGIN
    IF to_regclass('""Milestones""') IS NULL THEN
        RETURN;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'Milestones'
          AND column_name = 'StudyGoalId') THEN
        ALTER TABLE ""Milestones"" ADD COLUMN ""StudyGoalId"" uuid;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'Milestones'
          AND column_name = 'Status') THEN
        ALTER TABLE ""Milestones"" ADD COLUMN ""Status"" integer NOT NULL DEFAULT 0;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'Milestones'
          AND column_name = 'DueDateTime') THEN
        ALTER TABLE ""Milestones"" ADD COLUMN ""DueDateTime"" timestamp with time zone NOT NULL DEFAULT now();

        IF EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = current_schema()
              AND table_name = 'Milestones'
              AND column_name = 'DueDate') THEN
            UPDATE ""Milestones"" SET ""DueDateTime"" = ""DueDate"";
        END IF;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'Milestones'
          AND column_name = 'StartDateTime') THEN
        ALTER TABLE ""Milestones"" ADD COLUMN ""StartDateTime"" timestamp with time zone NOT NULL DEFAULT now();
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'Milestones'
          AND column_name = 'EndDateTime') THEN
        ALTER TABLE ""Milestones"" ADD COLUMN ""EndDateTime"" timestamp with time zone NOT NULL DEFAULT now();
    END IF;

    UPDATE ""Milestones""
    SET ""StartDateTime"" = COALESCE(""StartDateTime"", ""DueDateTime"", now()),
        ""EndDateTime"" = COALESCE(""EndDateTime"", ""DueDateTime"", now()),
        ""DueDateTime"" = COALESCE(""DueDateTime"", ""EndDateTime"", now());
END
$$;";

        await dbContext.Database.ExecuteSqlRawAsync(ensureMilestoneColumnsSql);
        logger.LogInformation("Milestone schema compatibility check completed.");
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