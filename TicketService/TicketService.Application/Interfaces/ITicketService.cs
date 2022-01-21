using System.Collections.Generic;
using System.Threading.Tasks;
using TicketService.Domain.Entities;

namespace TicketService.Application.Interfaces
{
    /// <summary>
    ///     Blueprint of ticket service implantations
    /// </summary>
    public interface ITicketService
    {
        /// <summary>
        ///     Retrieves a single ticket
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ICollection<Ticket>> Get(int id);

        /// <summary>
        ///     Get all the tickets out of the database
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        Task<ICollection<Ticket>> GetAll(int amount = 10, int page = 10);

        Task UpdatePlanning(IEnumerable<AnalyticsResult> analyticsResults);
    }
}