using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyBGList.Constants;
using MyBGList.Controllers;
using MyBGList.Models;
using MyBGList.Swagger;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Text.Json;

namespace MyBGList;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Add services to the container.
        builder.Logging
            .ClearProviders()
            .AddSimpleConsole()
            .AddDebug()
            .AddApplicationInsights(
                telemetry => telemetry.ConnectionString = builder.Configuration["Azure:ApplicationInsights:ConnectionString"],
                loggerOptions => { });
        builder.Host.UseSerilog((ctx, lc) =>
        {
            lc.ReadFrom.Configuration(ctx.Configuration);
            lc.WriteTo.MSSqlServer(
                connectionString: ctx.Configuration.GetConnectionString("DefaultConnection"),
                sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
                {
                    TableName = "LogEvents",
                    AutoCreateSqlTable = true
                },
                columnOptions: new ColumnOptions()
                {
                    AdditionalColumns = new SqlColumn[]
                    {
                        new SqlColumn
                        {
                            ColumnName = "SourceContext",
                            PropertyName = "SourceContext",
                            DataType = System.Data.SqlDbType.NVarChar
                        }
                    }
                });
        },
        writeToProviders: true);
        builder.Services.AddControllers(options =>
        {
            options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => $"The value '{x}' is invalid.");
            options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(x => $"The field {x} must be a number.");
            options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((x, y) => $"The value '{y}' is not valid for {x}.");
            options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => "A value is required.");
        });
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.ParameterFilter<SortOrderFilter>();
            options.ParameterFilter<SortColumnFilter>();

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
        builder.Services.AddCors();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            options.UseSqlServer(connectionString);
        });
        builder.Services.AddIdentity<ApiUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 12;
        }).AddEntityFrameworkStores<ApplicationDbContext>();
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme =
            options.DefaultChallengeScheme =
            options.DefaultForbidScheme =
            options.DefaultScheme =
            options.DefaultSignInScheme =
            options.DefaultSignOutScheme =
                JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["JWT:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["JWT:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    builder.Configuration["JWT:SigningKey"]))
            };
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Configuration.GetValue<bool>("UseSwagger"))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (app.Configuration.GetValue<bool>("UseDeveloperExceptionPage"))
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler(action =>
            {
                action.Run(async context =>
                {
                    if (context.Request.Method == HttpMethods.Get)
                    {
                        context.Response.Redirect("/error");
                        return;
                    }

                    var exceptionHandler = context.Features.Get<IExceptionHandlerPathFeature>();

                    // logging, sending notification, ...

                    var details = new ProblemDetails();
                    details.Detail = exceptionHandler?.Error.Message;
                    details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
                    details.Extensions["customKey"] = "To jest overloaded ExceptionHandler";
                    details.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                    details.Status = StatusCodes.Status500InternalServerError;

                    await context.Response.WriteAsync(JsonSerializer.Serialize(details));

                });
            });
        }

        app.UseResponseCaching();

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        // Test COD
        app.MapGet("/cod/test", () =>
        {
            return Results.Text("<script>" +
                "window.alert('Your client supports JavaScript!" +
                "\\r\\n\\r\\n" +
                $"Server time (UTC): {DateTime.UtcNow.ToString("o")}" +
                "\\r\\n" +
                "Client time (UTC): ' + new Date().toISOString());" +
                "</script>" +
                "<noscript>Your client does not support JavaScript</noscript>",
                "text/html");
        });

        // Shown, when any exception occure (handles only GET methods)
        app.MapGet("/error", (HttpContext context) =>
        {
            var exceptionHandler = context.Features.Get<IExceptionHandlerPathFeature>();

            // logging, sending notification, ...
            app.Logger.LogError(3333, "Exception occured {Exception}", exceptionHandler?.Error.Message);

            var details = new ProblemDetails();
            details.Detail = exceptionHandler?.Error.Message;
            details.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
            details.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
            details.Status = StatusCodes.Status500InternalServerError;
            return Results.Problem(details);  // return details; - seems to work the same
        });

        app.MapPost("/error/test2", () =>
        {
            throw new Exception("This is POST!");
        });

        // Simulate exceptions
        app.MapGet("/error/test", () =>
        {
            throw new Exception("Tusom!");
        });

        // authorized test
        app.MapGet("/auth/test/1",
            [Authorize]
        [ResponseCache(NoStore = true)] () =>
        {
            return Results.Ok("You are authorized!");
        });

        app.MapGet("/auth/test/2",
            [Authorize(Roles = RoleNames.Moderator)]
            [ResponseCache(NoStore = true)] () =>
        {
            return Results.Ok("You are authorized!");
        });

        app.MapGet("/auth/test/3",
            [Authorize(Roles = RoleNames.Administrator)]
            [ResponseCache(NoStore = true)] () =>
        {
            return Results.Ok("You are authorized!");
        });

        app.MapControllers();

        app.Run();
    }
}
