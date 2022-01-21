using System;
using Microsoft.Extensions.Options;
using TicketService.Application.Options;
using TicketService.Domain.Entities;
using TicketService.Domain.Enumerations;

namespace TicketService.Application.Extensions
{
    internal static class Log
    {
        public static void AddConsoleLog(string message, ConsoleColor foreground, ConsoleColor background,
            bool timeStamps, IOptions<ApplicationOptions> options)
        {
            if (!options.Value.ConsoleLog) return;
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            var timeStamp = DateTime.Now.ToString("dd/MM/yy HH:mm:ss");
            if (timeStamps)
            {
                if (message.StartsWith("\n"))
                    Console.WriteLine($"\n[{timeStamp}] " + message.TrimStart('\n'));
                else
                    Console.WriteLine($"[{timeStamp}] " + message);
            }
            else
            {
                Console.WriteLine(message);
            }

            Console.ResetColor();
        }

        public static void AddConsoleLog(string message, ConsoleColor color, bool timeStamps,
            IOptions<ApplicationOptions> options)
        {
            if (!options.Value.ConsoleLog) return;
            Console.ForegroundColor = color;
            var timeStamp = DateTime.Now.ToString("dd/MM/yy HH:mm:ss");
            if (timeStamps)
            {
                if (message.StartsWith("\n"))
                    Console.WriteLine($"\n[{timeStamp}] " + message.TrimStart('\n'));
                else
                    Console.WriteLine($"[{timeStamp}] " + message);
            }
            else
            {
                Console.WriteLine(message);
            }

            Console.ResetColor();
        }

        public static void AddConsoleLog(string message, bool timeStamps, IOptions<ApplicationOptions> options)
        {
            if (!options.Value.ConsoleLog) return;
            var timeStamp = DateTime.Now.ToString("dd/MM/yy HH:mm:ss");
            if (timeStamps)
            {
                if (message.StartsWith("\n"))
                    Console.WriteLine($"\n[{timeStamp}] " + message.TrimStart('\n'));
                else
                    Console.WriteLine($"[{timeStamp}] " + message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        public static void AddConsoleLog(string message, ConsoleColor color, IOptions<ApplicationOptions> options)
        {
            if (!options.Value.ConsoleLog) return;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void AddConsoleLog(string message, IOptions<ApplicationOptions> options)
        {
            if (!options.Value.ConsoleLog) return;
            Console.WriteLine(message);
        }

        public static void LogAnalyticsResult(IOptions<ApplicationOptions> options, AnalyticsResult analyticsResult,
            bool newUser)
        {
            if (!options.Value.ConsoleLog) return;
            if (newUser)
                if (analyticsResult.Ticket?.AssignedTo != null)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"\n{analyticsResult.Ticket.AssignedTo.VolledigeNaam}");
                    Console.ResetColor();
                }

            // Logs the calculated ticket information 
            if (analyticsResult.Ticket == null) return;
            Console.WriteLine($"Ticked ID: {analyticsResult.Ticket.Id}");
            Console.WriteLine($"Type: {analyticsResult.Ticket.Type}");
            Console.WriteLine($"Ticked Status: {analyticsResult.Ticket.Status}");
            Console.WriteLine($"Prioriteit: {analyticsResult.Ticket.Priority}");
            Console.WriteLine($"Activiteit: {analyticsResult.Ticket.Activity}");
            if (analyticsResult.Ticket.Product != null)
                Console.WriteLine($"Productlijn: {analyticsResult.Ticket.Product.ProductNaam}");
            if (analyticsResult.Ticket.Module != null)
                Console.WriteLine($"Module: {analyticsResult.Ticket.Module.ModuleNaam}");
            Console.WriteLine($"Geplande uren: {analyticsResult.Ticket.PlnGeplandeUren}");
            Console.WriteLine(
                $"Begin tijd: {analyticsResult.Ticket.PlnGeplandeStartDatumTijd} Eind tijd: {analyticsResult.Ticket.PlnGeplandeEindeDatumTijd}");
            Console.WriteLine($"PreStart: {analyticsResult.Ticket.PreStart}");
            Console.WriteLine($"PreStartPreviousRun: {analyticsResult.Ticket.PreStartPreviousRun}");
            if (analyticsResult.Ticket.PlnGeplandeRestUren > 0)
                Console.WriteLine($"Geplande resteerde programmeer uren: {analyticsResult.Ticket.PlnGeplandeRestUren}");
            else
                switch (analyticsResult.Ticket.Activity)
                {
                    //TEST CODE
                    case TicketActivity.Analyse:
                        Console.WriteLine("");
                        Console.WriteLine($"Prognose Start: {analyticsResult.Ticket.PlnPrognoseAnalStart}");
                        Console.WriteLine($"Prognose eind: {analyticsResult.Ticket.PlnPrognoseAnalEind}");
                        if (analyticsResult.Ticket.CorrectionPrognoseEndDateTime)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Prognose eind datum/time aangepast");
                            Console.WriteLine(
                                $"Prognose eind Aangepast: {analyticsResult.Ticket.PlnPrognoseAnalEindGecor}");
                            Console.ResetColor();
                        }

                        break;
                    case TicketActivity.Programmeren:
                        Console.WriteLine("");
                        Console.WriteLine($"Prognose start: {analyticsResult.Ticket.PlnPrognoseProgStart}");
                        Console.WriteLine($"Prognose eind: {analyticsResult.Ticket.PlnPrognoseProgEind}");
                        if (analyticsResult.Ticket.CorrectionPrognoseEndDateTime)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Prognose eind datum/time aangepast");
                            Console.WriteLine(
                                $"Prognose eind Aangepast: {analyticsResult.Ticket.PlnPrognoseProgEindGecor}");
                            Console.ResetColor();
                        }

                        break;
                    case TicketActivity.Testen:
                        Console.WriteLine("");
                        Console.WriteLine($"Prognose start: {analyticsResult.Ticket.PlnPrognoseTestStart}");
                        Console.WriteLine($"Prognose eind: {analyticsResult.Ticket.PlnPrognoseTestEind}");
                        if (analyticsResult.Ticket.CorrectionPrognoseEndDateTime)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Prognose eind datum/time aangepast");
                            Console.WriteLine(
                                $"Prognose eind Aangepast: {analyticsResult.Ticket.PlnPrognoseTestEindGecor}");
                            Console.ResetColor();
                        }

                        break;
                }


            Console.WriteLine(Environment.NewLine);
        }
    }
}