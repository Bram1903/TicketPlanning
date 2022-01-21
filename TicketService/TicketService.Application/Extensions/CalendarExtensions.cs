using System;
using TicketService.Domain.Entities;

namespace TicketService.Application.Extensions
{
    public static class CalendarExtensions
    {
        public static void AddAnalyticsResult(this Calendar calendar, AnalyticsResult analyticsResult)
        {
            calendar.Body.AppendLine("BEGIN:VEVENT");
            if (analyticsResult.Ticket != null)
            {
                calendar.Body.AppendLine("DTSTART;TZID=Europe/Amsterdam:" +
                                         analyticsResult.Ticket.PlnGeplandeStartDatumTijd
                                             .ToString("yyyyMMddTHHmm00"));
                calendar.Body.AppendLine("DTEND;TZID=Europe/Amsterdam:" +
                                         analyticsResult.Ticket.PlnGeplandeEindeDatumTijd
                                             .ToString("yyyyMMddTHHmm00"));
                calendar.Body.AppendLine("DTSTAMP:" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssK"));
                calendar.Body.AppendLine("UID:" + analyticsResult.Ticket.Id);
                if (analyticsResult.Ticket.AssignedTo != null)
                    if (calendar.Common)
                    {
                        if (analyticsResult.Ticket.Product != null)
                            if (analyticsResult.Ticket.Module != null)
                                calendar.Body.AppendLine("SUMMARY:" + $"Ticket ID: {analyticsResult.Ticket.Id} " +
                                                         $"(Product: {analyticsResult.Ticket.Product.ProductNaam} " +
                                                         $" Module: {analyticsResult.Ticket.Module.ModuleNaam} " +
                                                         $" Activiteit: {analyticsResult.Ticket.Activity} " +
                                                         $" Status: {analyticsResult.Ticket.Status} " +
                                                         $" Prioriteit: {analyticsResult.Ticket.Priority})" +
                                                         $" Naam: {analyticsResult.Ticket.AssignedTo.VolledigeNaam}");
                    }
                    else if (analyticsResult.Ticket.Product != null)
                    {
                        if (analyticsResult.Ticket.Module != null)
                            calendar.Body.AppendLine("SUMMARY:" + $"Ticket ID: {analyticsResult.Ticket.Id} " +
                                                     $"(Product: {analyticsResult.Ticket.Product.ProductNaam} " +
                                                     $" Module: {analyticsResult.Ticket.Module.ModuleNaam} " +
                                                     $" Activiteit: {analyticsResult.Ticket.Activity} " +
                                                     $" Status: {analyticsResult.Ticket.Status} " +
                                                     $" Prioriteit: {analyticsResult.Ticket.Priority})");
                    }

                calendar.Body.AppendLine("LOCATION:" + "Kantoor");
                calendar.Body.AppendLine("DESCRIPTION:" + analyticsResult.Ticket.OmschrijvingPlain);
                calendar.Body.AppendLine("X-ALT-DESC;FMTTYPE=text/html:<HTML>");
                calendar.Body.AppendLine("    " + analyticsResult.Ticket.OmschrijvingHtml);
                calendar.Body.AppendLine("    </HTML>");
            }

            calendar.Body.AppendLine("PRIORITY:5");
            calendar.Body.AppendLine("END:VEVENT");
        }
    }
}