#nullable enable
using System;

namespace TicketService.Domain.Entities
{
    public class User
    {
        /// <summary>
        ///     The unique identifier of a user.
        /// </summary>
        public Guid Oid { get; init; }

        /// <summary>
        ///     The full name of the user.
        /// </summary>
        public string? VolledigeNaam { get; init; }
    }
}