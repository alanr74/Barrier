using Avalonia;
using Avalonia.ReactiveUI;
using System;
using Ava.Repositories;
using Ava.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;

namespace Ava
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add your services
            builder.Services.AddSingleton<Config>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddSingleton<ILoggingService, LoggingService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            // Start the web host in a background thread
            var webHostTask = app.RunAsync();

            // Start the Avalonia UI
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            // The web host will be stopped by App.OnExiting
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();
    }
}
