using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TicketService.Application.Extensions;
using TicketService.Application.Interfaces;
using TicketService.Application.Options;
using TicketService.Domain.Entities;
using TicketService.Domain.Enumerations;

namespace TicketService.Application.Services
{
    public class TicketAnalyzer : ITicketAnalyzer
    {
        private readonly IOptions<ApplicationOptions> options;
        private readonly ITicketService ticketService;

        public TicketAnalyzer(ITicketService ticketService, IOptions<ApplicationOptions> options)
        {
            this.ticketService = ticketService;
            this.options = options;
        }

        // Analyzes all of the tickets, to filter on specific statusses
        public async Task<ICollection<AnalyticsResult>> AnalyzeAll()
        {
            try
            {
                // Retrieve tickets
                var tickets = await ticketService.GetAll(10000000);

                // Apply changes for each ticket
                var todoStatusses = new List<TicketStatus>
                {
                    TicketStatus.Planned, TicketStatus.AcceptanceTest, TicketStatus.Analyse, TicketStatus.Development,
                    TicketStatus.DevelopmentTest, TicketStatus.Foto, TicketStatus.OfferedForTest,
                    TicketStatus.SystemTest, TicketStatus.AnalystTest
                };
                return (from ticket in tickets
                    where todoStatusses.Contains(ticket.Status)
                    select new AnalyticsResult {Ticket = ticket}).ToList();
            }
            catch (Exception e)
            {
                Log.AddConsoleLog(e.ToString(), options);
                throw;
            }
        }
    }
}