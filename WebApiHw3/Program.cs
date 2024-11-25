using Asp.Versioning;
using Asp.Versioning.Conventions;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace WebApiHw3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // OpenAPI for API (controllers + actions scope)
            builder.Services.AddControllers();

            // OpenAPI For Minimal API
            builder.Services.AddEndpointsApiExplorer();

            // initialize API versionings
            builder.Services.AddApiVersioning(o =>
            {
                o.AssumeDefaultVersionWhenUnspecified = true; // assume version 1 if not specified by client
                o.DefaultApiVersion = new ApiVersion(1, 0); // default version
                o.ReportApiVersions = true; // return supported versions in response
                o.ApiVersionReader = new UrlSegmentApiVersionReader(); // read version from URL
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });


            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(
                    "v1",
                    new OpenApiInfo { Title = "MyBGList", Version = "v1.0" });
                options.SwaggerDoc(
                    "v2",
                    new OpenApiInfo { Title = "MyBGList", Version = "v2.0" });
                options.SwaggerDoc(
                    "v3",
                    new OpenApiInfo { Title = "MyBGList", Version = "v3.0" });
            });

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });

                options.AddPolicy("AnyOrigin_GetOnly", policy =>
                {
                    policy.AllowAnyOrigin().WithMethods("GET").AllowAnyHeader();
                });

                options.AddPolicy("SpecificOrigin", policy =>
                {
                    policy.WithOrigins("https://example.com").AllowAnyMethod().AllowAnyHeader();
                });
            });

            builder.Services.AddResponseCaching();

            var app = builder.Build();

            var apiVersionSet = app.NewApiVersionSet()
                .HasDeprecatedApiVersion(new ApiVersion(1, 0))
                .HasApiVersion(new ApiVersion(2, 0))
                .HasApiVersion(new ApiVersion(3, 0))
                .ReportApiVersions()
                .Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint(
                       $"/swagger/v1/swagger.json",
                       $"MyBGList v1");
                    options.SwaggerEndpoint(
                        $"/swagger/v2/swagger.json",
                        $"MyBGList v2");
                    options.SwaggerEndpoint(
                        $"/swagger/v3/swagger.json",
                        $"MyBGList v3");
                });
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseResponseCaching();
            app.UseAuthorization();

            app.MapGet("cod/javascript/test", () =>
            {
                return Results.Text(
                    "window.alert('Your client supports JS!" +
                    "\\r\\n\\r\\n" +
                    $"Server time (UTC): {DateTime.UtcNow.ToString("O")}" +
                    "\\r\\n" +
                    "Client time (UTC): ' + new Date().toISOString());",
                    "text/javascript");
            }).RequireCors("SpecificOrigin");

            app.MapGet("api/v{version:ApiVersion}/temperature", () =>
            {
                Random random = new Random();
                return Results.Ok($"Temperature: {random.Next(1, 101)}");
            })
                .WithApiVersionSet(apiVersionSet)
                .MapToApiVersion(new ApiVersion(2, 0));

            app.MapGet("cod/v{version:ApiVersion}/html/test", () =>
                {
                    return Results.Text("<script>" +
                        "window.alert('Your client supports JS!" +
                        "\\r\\n\\r\\n" +
                        $"Server time (UTC): {DateTime.UtcNow.ToString("O")}" +
                        "\\r\\n" +
                        "Client time (UTC): ' + new Date().toISOString());" +
                        "</script>" +
                        "<noscript>Your browser doesn't support JS</noscript>",
                        "text/html");
                }
            )
                .WithApiVersionSet(apiVersionSet)
                .MapToApiVersions(new List<ApiVersion> {
                    new ApiVersion(1, 0),
                    new ApiVersion(2, 0),
                    new ApiVersion(3, 0) 
                });

            app.MapControllers();

            app.Run();
        }
    }
}
