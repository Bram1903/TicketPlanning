#nullable enable
namespace TicketService.Domain.Entities
{
    public class AnalyticsResult
    {
        /// <summary>
        ///     To be able to know which ticket we are talking about.
        /// </summary>
        public Ticket? Ticket { get; init; }
    }
}