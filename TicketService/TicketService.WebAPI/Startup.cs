using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using TicketService.Application;
using TicketService.Application.Interfaces;
using TicketService.Application.Options;
using TicketService.Application.Services;

namespace TicketService.WebAPI
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddScoped<IDatabaseService, DatabaseService>();
            services.AddScoped<ITicketService, Application.Services.TicketService>();
            services.AddScoped<ITicketAnalyzer, TicketAnalyzer>();
            services.AddScoped<ICalendarGeneratorService, CalendarGeneratorService>();
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IStartTicketPlanningTool, StartTicketPlanningTool>();
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.Configure<ApplicationOptions>(configuration.GetSection("TicketPlanning"));
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "TicketService.WebAPI", Version = "v1"});
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketService.WebAPI v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}