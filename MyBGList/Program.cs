using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Controllers;
using MyBGList.Models;
using MyBGList.Swagger;

namespace MyBGList
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.

            builder.Services.AddControllers();
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
                app.UseExceptionHandler("/error");
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

            // Shown, when any exception occure
            app.MapGet("/error", () => { return "This page occure when error happens in the mean time."; });

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
