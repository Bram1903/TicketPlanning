using System.Data;

namespace TicketService.Application.Interfaces
{
    /// <summary>
    ///     Blueprint for the DatabaseService
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        ///     Function to setup a database connection
        /// </summary>
        /// <returns></returns>
        IDbConnection CreateConnection();
    }
}