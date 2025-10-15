using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using System.Reflection;
using Ava.Repositories;
using Ava.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Ava
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddHttpClient();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(o =>
            {
                o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
            });

            // Load and register configuration
            var appConfig = builder.Configuration.Get<AppConfig>() ?? new AppConfig();
            builder.Services.AddSingleton(appConfig);

            // Configure web host URLs for API access from any IP
            var apiPort = builder.Configuration.GetValue<int>("ApiPort", 8086);
            builder.Logging.AddConsole();
            builder.WebHost.UseKestrel(options =>
            {
                options.ListenAnyIP(apiPort); // HTTP only
            });
            Console.WriteLine($"Starting web host on port {apiPort}...");

            // Add logging
            builder.Logging.SetMinimumLevel(LogLevel.Information);

            // Add your services
            builder.Services.AddSingleton<Config>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddSingleton<ILoggingService, LoggingService>();
            builder.Services.AddSingleton<DuplicateSuppressorService>(sp =>
                new DuplicateSuppressorService(sp.GetRequiredService<AppConfig>().DuplicateSuppressionWindowSeconds));
            builder.Services.AddSingleton<IBarrierService, BarrierService>();
            builder.Services.AddSingleton<INumberPlateService, NumberPlateService>();
            builder.Services.AddSingleton<ISchedulingService, SchedulingService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Barrier API v1");
                c.RoutePrefix = "swagger"; // Accessible at http://localhost:PORT/swagger
            });

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            // Start the web host in a background thread (but wait for it to start)
            try
            {
                File.AppendAllText("api-host.log", $"[{DateTime.Now}] Starting web host on port {apiPort}...\n");
                Console.WriteLine($"Starting web host on port {apiPort}...");
                app.RunAsync();
                File.AppendAllText("api-host.log", $"[{DateTime.Now}] Web host started successfully.\n");
                Console.WriteLine("Web host started");
            }
            catch (Exception ex)
            {
                File.AppendAllText("api-host.log", $"[{DateTime.Now}] Web host failed to start: {ex.Message}\n{ex.StackTrace}\n");
                Console.WriteLine("Web host start failed with exception.");
                throw; // Re-throw to ensure app doesn't proceed
            }

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
