using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Controllers;
using MyBGList.Models;
using MyBGList.Swagger;
using System.Text.Json;

namespace MyBGList
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.

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
            });
            builder.Services.AddCors();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

                options.UseSqlServer(connectionString);
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
            
            app.UseHttpsRedirection();
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
            
            app.MapControllers();

            app.Run();
        }
    }
}
