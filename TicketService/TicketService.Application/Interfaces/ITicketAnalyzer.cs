using System.Collections.Generic;
using System.Threading.Tasks;
using TicketService.Domain.Entities;

namespace TicketService.Application.Interfaces
{
    /// <summary>
    ///     Blueprint for the TicketAnalyzer
    /// </summary>
    public interface ITicketAnalyzer
    {
        /// <summary>
        ///     Ticket Filter
        /// </summary>
        /// <returns></returns>
        Task<ICollection<AnalyticsResult>> AnalyzeAll();
    }
}