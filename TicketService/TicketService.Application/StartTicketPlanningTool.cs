using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TicketService.Application.Extensions;
using TicketService.Application.Interfaces;
using TicketService.Application.Options;

namespace TicketService.Application
{
    public class StartTicketPlanningTool : IStartTicketPlanningTool
    {
        private readonly IOptions<ApplicationOptions> options;
        private readonly ICalendarGeneratorService service;
        private readonly ITicketAnalyzer ticketAnalyzer;

        public StartTicketPlanningTool(ICalendarGeneratorService service,
            ITicketAnalyzer ticketAnalyzer,
            IOptions<ApplicationOptions> options)
        {
            this.service = service;
            this.ticketAnalyzer = ticketAnalyzer;
            this.options = options;
        }

        public async Task Start()
        {
            // Clears the console from unnecessary Microsoft information
            Console.Clear();

            const string title = @"
  _______ _      _        _     _____  _                   _             
 |__   __(_)    | |      | |   |  __ \| |                 (_)            
    | |   _  ___| | _____| |_  | |__) | | __ _ _ __  _ __  _ _ __   __ _ 
    | |  | |/ __| |/ / _ \ __| |  ___/| |/ _` | '_ \| '_ \| | '_ \ / _` |
    | |  | | (__|   <  __/ |_  | |    | | (_| | | | | | | | | | | | (_| |
    |_|  |_|\___|_|\_\___|\__| |_|    |_|\__,_|_| |_|_| |_|_|_| |_|\__, |
                                                                    __/ |
                                                                   |___/ 
";
            // Checks if the CONSOLE_LOG = true, otherwise their won't be a console output.
            Log.AddConsoleLog(title, options);
            var analyticsResults = await ticketAnalyzer.AnalyzeAll();
            await service.CreateIcs(analyticsResults);
        }
    }
}