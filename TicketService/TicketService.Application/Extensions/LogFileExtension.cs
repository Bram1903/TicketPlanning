using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using TicketService.Application.Options;
using TicketService.Domain.Entities;
using TicketService.Domain.Enumerations;

namespace TicketService.Application.Extensions
{
    public static class LogFileExtensions
    {
        public static void Add(this LogFile fileLog, AnalyticsResult analyticsResult, bool newUsers)
        {
            if (analyticsResult.Ticket == null) return;
            if (newUsers && analyticsResult.Ticket.AssignedTo != null)
                fileLog.Body.AppendLine($"\n-------- {analyticsResult.Ticket.AssignedTo.VolledigeNaam} ---------\n");
            fileLog.Body.AppendLine($"Ticked ID: {analyticsResult.Ticket.Id}");
            fileLog.Body.AppendLine($"Type: {analyticsResult.Ticket.Type}");
            fileLog.Body.AppendLine($"Ticked Status: {analyticsResult.Ticket.Status}");
            fileLog.Body.AppendLine($"Prioriteit: {analyticsResult.Ticket.Priority}");
            fileLog.Body.AppendLine($"Activiteit: {analyticsResult.Ticket.Activity}");
            if (analyticsResult.Ticket.Product != null)
                fileLog.Body.AppendLine($"Productlijn: {analyticsResult.Ticket.Product.ProductNaam}");
            if (analyticsResult.Ticket.Module != null)
                fileLog.Body.AppendLine($"Module: {analyticsResult.Ticket.Module.ModuleNaam}");
            fileLog.Body.AppendLine($"Geplande uren: {analyticsResult.Ticket.PlnGeplandeUren}");
            fileLog.Body.AppendLine(
                $"Begin tijd: {analyticsResult.Ticket.PlnGeplandeStartDatumTijd} Eind tijd: {analyticsResult.Ticket.PlnGeplandeEindeDatumTijd}");
            fileLog.Body.AppendLine($"PreStart: {analyticsResult.Ticket.PreStart}");
            fileLog.Body.AppendLine($"PreStartPreviousRun: {analyticsResult.Ticket.PreStartPreviousRun}");
            if (analyticsResult.Ticket.PlnGeplandeRestUren > 0)
                fileLog.Body.AppendLine(
                    $"Geplande resteerde programmeer uren: {analyticsResult.Ticket.PlnGeplandeRestUren}");
            else
                switch (analyticsResult.Ticket.Activity)
                {
                    //TEST CODE
                    case TicketActivity.Analyse:
                        fileLog.Body.AppendLine("");
                        fileLog.Body.AppendLine($"Prognose Start: {analyticsResult.Ticket.PlnPrognoseAnalStart}");
                        fileLog.Body.AppendLine($"Prognose eind: {analyticsResult.Ticket.PlnPrognoseAnalEind}");
                        if (analyticsResult.Ticket.CorrectionPrognoseEndDateTime)
                        {
                            fileLog.Body.AppendLine("Prognose eind datum/time aangepast");
                            fileLog.Body.AppendLine(
                                $"Prognose eind Aangepast: {analyticsResult.Ticket.PlnPrognoseAnalEindGecor}");
                        }

                        break;
                    case TicketActivity.Programmeren:
                        fileLog.Body.AppendLine("");
                        fileLog.Body.AppendLine($"Prognose start: {analyticsResult.Ticket.PlnPrognoseProgStart}");
                        fileLog.Body.AppendLine($"Prognose eind: {analyticsResult.Ticket.PlnPrognoseProgEind}");
                        if (analyticsResult.Ticket.CorrectionPrognoseEndDateTime)
                        {
                            fileLog.Body.AppendLine("Prognose eind datum/time aangepast");
                            fileLog.Body.AppendLine(
                                $"Prognose eind Aangepast: {analyticsResult.Ticket.PlnPrognoseProgEindGecor}");
                        }

                        break;
                    case TicketActivity.Testen:
                        fileLog.Body.AppendLine("");
                        fileLog.Body.AppendLine($"Prognose start: {analyticsResult.Ticket.PlnPrognoseTestStart}");
                        fileLog.Body.AppendLine($"Prognose eind: {analyticsResult.Ticket.PlnPrognoseTestEind}");
                        if (analyticsResult.Ticket.CorrectionPrognoseEndDateTime)
                        {
                            fileLog.Body.AppendLine("Prognose eind datum/time aangepast");
                            fileLog.Body.AppendLine(
                                $"Prognose eind Aangepast: {analyticsResult.Ticket.PlnPrognoseTestEindGecor}");
                        }

                        break;
                }

            fileLog.Body.AppendLine("");
        }

        public static void Start(this LogFile fileLog, IOptions<ApplicationOptions> options)
        {
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

            fileLog.Body.AppendLine(title);
            fileLog.Body.AppendLine("");
        }


        public static void End(this LogFile fileLog, int totalPlannedTickets, int totalPlannedUsers,
            List<string> locations, IOptions<ApplicationOptions> options)
        {
            fileLog.Body.AppendLine("");
            fileLog.Body.AppendLine("");
            fileLog.Body.AppendLine("");
            fileLog.Body.AppendLine($"Einde datum/tijd: {DateTime.Now}");
            fileLog.Body.AppendLine($"Totaal aantal geplande tickets: {totalPlannedTickets}");
            fileLog.Body.AppendLine($"Totaal aantal unique geplande users: {totalPlannedUsers}");
            if (!options.Value.Sftp.Upload) return;
            fileLog.Body.AppendLine("");
            fileLog.Body.AppendLine("Calandar website locations:");
            foreach (var location in locations) fileLog.Body.AppendLine(location);
        }
    }
}