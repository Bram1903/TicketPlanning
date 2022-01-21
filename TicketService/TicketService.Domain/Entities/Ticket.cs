#nullable enable
using System;
using TicketService.Domain.Enumerations;

namespace TicketService.Domain.Entities
{
    public class Ticket
    {
        /// <summary>
        ///     The unique identifier for the ticket
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        ///     Omschrijving
        /// </summary>
        public string? Omschrijving { get; set; }

        public string? OmschrijvingHtml { get; set; }

        public string? OmschrijvingPlain { get; set; }

        /// <summary>
        ///     Ticket type
        /// </summary>
        public TicketType Type { get; init; }

        /// <summary>
        ///     Module
        /// </summary>
        public Module? Module { get; init; }

        /// <summary>
        ///     Product naam
        /// </summary>
        public ProductLine? Product { get; init; }

        /// <summary>
        ///     Priority level
        /// </summary>
        public TicketPriority Priority { get; init; }

        /// <summary>
        ///     Assigned to specific developer.
        /// </summary>
        public User? AssignedTo { get; init; }

        /// <summary>
        ///     Current status
        /// </summary>
        public TicketStatus Status { get; init; }

        public int? PlnComplexiteit { get; init; }
        public int PlnSchattingAnalUren { get; init; }
        public int PlnSchattingProgUren { get; init; }
        public int PlnSchattingTestUren { get; init; }
        public int PlnGeplandeProgUren { get; set; }
        public int PlnGeplandeAnalUren { get; set; }
        public int PlnGeplandeTestUren { get; set; }

        public DateTime PlnGeplandeStartDatumTijd { get; set; }

        public DateTime PlnGeplandeEindeDatumTijd { get; set; }

        public int PlnGeplandeRestUren { get; set; }

        public int PlnGeplandeUren { get; set; }

        public TicketActivity Activity { get; set; }

        public DateTime PlnPrognoseAnalStart { get; set; }

        public DateTime PlnPrognoseAnalEind { get; set; }

        public DateTime PlnPrognoseProgStart { get; set; }

        public DateTime PlnPrognoseProgEind { get; set; }

        public DateTime PlnPrognoseTestStart { get; set; }

        public DateTime PlnPrognoseTestEind { get; set; }

        public DateTime PlnPrognoseAnalEindGecor { get; set; }

        public DateTime PlnPrognoseProgEindGecor { get; set; }

        public DateTime PlnPrognoseTestEindGecor { get; set; }
        public bool PreStart { get; set; }
        public bool PreStartPreviousRun { get; set; }
        public bool CorrectionPrognoseEndDateTime { get; set; }
    }
}