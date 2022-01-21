using System.Collections.Generic;
using System.Threading.Tasks;
using TicketService.Domain.Entities;

namespace TicketService.Application.Interfaces
{
    /// <summary>
    ///     Blueprint of CalandarGeneratorService
    /// </summary>
    public interface ICalendarGeneratorService
    {
        /// <summary>
        ///     Default function
        /// </summary>
        /// <returns></returns>
        Task CreateIcs(ICollection<AnalyticsResult> analyticsResults);
    }
}