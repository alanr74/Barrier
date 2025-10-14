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
