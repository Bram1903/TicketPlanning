using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketService.Application;
using TicketService.Application.Interfaces;
using TicketService.Application.Options;
using TicketService.Application.Services;

namespace TicketService.ConsoleApplication
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("./appsettings.json", false)
                .Build();

            // Build the host
            using var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                    services.AddScoped<IDatabaseService, DatabaseService>()
                        .AddScoped<ITicketService, Application.Services.TicketService>()
                        .AddScoped<ITicketAnalyzer, TicketAnalyzer>()
                        .AddScoped<ICalendarGeneratorService, CalendarGeneratorService>()
                        .AddScoped<IFileUploadService, FileUploadService>()
                        .AddScoped<IStartTicketPlanningTool, StartTicketPlanningTool>()
                        .Configure<ApplicationOptions>(configuration.GetSection("TicketPlanning"))
                ).Build();


            // Create a new scope for the DI services
            using var scope = host.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            // Get the required service from DI
            var planningTool = serviceProvider.GetService<IStartTicketPlanningTool>();
            if (planningTool != null) Task.FromResult(planningTool.Start());

            // Run the application
            host.Run();
        }
    }
}