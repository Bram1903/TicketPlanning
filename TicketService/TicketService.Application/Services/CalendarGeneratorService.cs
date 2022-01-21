using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TicketService.Application.Extensions;
using TicketService.Application.Interfaces;
using TicketService.Application.Options;
using TicketService.Domain.Entities;
using TicketService.Domain.Enumerations;

namespace TicketService.Application.Services
{
    public class CalendarGeneratorService : ICalendarGeneratorService
    {
        private readonly IFileUploadService fileUploadService;
        private readonly IOptions<ApplicationOptions> options;
        private readonly ITicketService ticketService;

        public CalendarGeneratorService(
            ITicketService ticketService,
            IFileUploadService fileUploadService,
            IOptions<ApplicationOptions> options)
        {
            this.ticketService = ticketService;
            this.fileUploadService = fileUploadService;
            this.options = options;
        }

        public async Task CreateIcs(ICollection<AnalyticsResult> analyticsResults)
        {
            // Creating some basic variables needed for the calculation.
            var assignedToChecker = default(Guid);
            var previousTicketEndTime = DateTime.Now;
            var previousTicketUser = "";
            var calenderUserFileOpen = false;
            var totalPlannedTickets = 0;
            var totalPlannedUsers = 0;

            // creating the logging file.
            var fileLog = new LogFile();

            // If the setting txt log is set to true, save it.
            if (options.Value.TxTLog) fileLog.Start(options);

            // Removes old .ics files
            try
            {
                // Gets the directory path from the options.Value file
                var filePaths = Directory.GetFiles(options.Value.Paths.AbsoluteCalendarLocation);
                foreach (var file in filePaths)
                    // Checks if the file contains .ics, so it doesn't remove other files by accident 
                    if (file.Contains(".ics"))
                        File.Delete(file);
            }
            catch (Exception error)
            {
                Log.AddConsoleLog(
                    $"Something went wrong while deleting the file in {options.Value.Paths.AbsoluteCalendarLocation}. See the following error:\n {error}",
                    ConsoleColor.Red, options);
                fileLog.Body.AppendLine(error.ToString());
            }

            // List filled with Guid's used for stats.
            List<Guid> done = new();
            if (done == null) throw new ArgumentNullException(nameof(done));

            // List filled with the ticket locations on the SFTP location, and the server location.
            List<string> locations = new();

            // Creating the global calandar.
            var commonCalendar = new Calendar("Alle afspraken", true, options.Value.RefreshInterval);

            // Calandar for each user.
            var calendar = new Calendar();

            // Information logging.
            fileLog.Body.Append("\nCreate Tickets...\n");
            Log.AddConsoleLog("\n--- Create Tickets ---\n", options);


            // Loops through all the results
            foreach (var analyticsResult in analyticsResults)
            {
                // Create Ticket Description (HTML and PLainText)
                await TicketDescription(analyticsResult);

                // Creating 
                var newUser = analyticsResult.Ticket is {AssignedTo: { }} &&
                              assignedToChecker != analyticsResult.Ticket.AssignedTo.Oid;


                if (newUser)
                {
                    totalPlannedUsers++;
                    if (options.Value.Sftp.Upload)
                        if (analyticsResult.Ticket.AssignedTo.VolledigeNaam != null)
                        {
                            var calandarlocation = options.Value.Sftp.WebsiteUrl +
                                                   // Replacing the space between the name.
                                                   analyticsResult.Ticket.AssignedTo.VolledigeNaam.Replace(" ", "");
                            // Saving the location of the calculated calandar on the website to a list.
                            locations.Add(
                                $"{analyticsResult.Ticket.AssignedTo.VolledigeNaam}'s (Web) calandar can be found at: {calandarlocation}.ics");
                        }

                    // Uploading the calandar to a server location, but only if the setting is on "True".
                    if (options.Value.Server.Upload)
                        if (analyticsResult.Ticket.AssignedTo.VolledigeNaam != null)
                        {
                            var calandarlocation = options.Value.Server.Url +
                                                   analyticsResult.Ticket.AssignedTo.VolledigeNaam.Replace(" ", "");
                            locations.Add(
                                $"{analyticsResult.Ticket.AssignedTo.VolledigeNaam}'s (Server) calandar can be found at: {calandarlocation}.ics");
                        }

                    // Adding an empty line to the location list, for reading effectiveness.
                    locations.Add("");

                    // Creates the base of a new calandar. 
                    if (analyticsResult.Ticket.AssignedTo.VolledigeNaam != null)
                        calendar = new Calendar(analyticsResult.Ticket.AssignedTo.VolledigeNaam, false,
                            options.Value.RefreshInterval);
                    calenderUserFileOpen = true;
                }

                // Requests the CalculateTicketHours function, and gives it the current analytisResult
                await CalculateTicketHours(analyticsResult);

                // Requests the TicketActivity function, and gives it the current analyticsResult
                await TicketActivityCalculator(analyticsResult);

                // Yes Yes, I know. It is a do. 
                do
                {
                    // +1 the total planned tickets integer.
                    totalPlannedTickets++;

                    // Requests the NextPlannedTicketDateTime, and gives it the following parameters
                    await NextPlannedTicketDateTime(analyticsResult, newUser, previousTicketEndTime);

                    // Sets the value of a variable for calculation purposes. 
                    if (analyticsResult.Ticket != null)
                        previousTicketEndTime = analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;


                    // Custom logging
                    if (options.Value.ConsoleLog)
                        Log.LogAnalyticsResult(options, analyticsResult, newUser);

                    // Fills the Calandar for each person with calculated the calculated ticket events.
                    calendar.AddAnalyticsResult(analyticsResult);

                    // Fills the global Calandar
                    if (options.Value.GlobalCalendar)
                        commonCalendar.AddAnalyticsResult(analyticsResult);

                    // Adding the calculated analyticsResult to the logging file.
                    fileLog.Add(analyticsResult, newUser);

                    newUser = false;
                } while (analyticsResult.Ticket is {PlnGeplandeRestUren: > 0});

                // Setting some variables
                if (analyticsResult.Ticket?.AssignedTo == null) continue;
                done.Add(analyticsResult.Ticket.AssignedTo.Oid);
                assignedToChecker = analyticsResult.Ticket.AssignedTo.Oid;
                previousTicketEndTime = analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;
                previousTicketUser = analyticsResult.Ticket.AssignedTo.VolledigeNaam;


                // Save the Calandar for each user, when the last ticket has been processed.
                if (calenderUserFileOpen)
                    await SaveCalendar(calendar);
            }

            // Save the global calandar, but only when the setting is set to true.
            if (options.Value.GlobalCalendar)
                await SaveCalendar(commonCalendar);

            // Uploading the calandars to the SFTP location, but only when the setting is set to true.
            if (options.Value.Sftp.Upload)
                await fileUploadService.FileUploadSftp(fileLog);

            // Uploading the calandars to the Server location, but only when the settign is set to true.
            if (options.Value.Server.Upload)
                await fileUploadService.FileUploadServer(fileLog);

            // If the setting txt log is set to true, save it.
            if (options.Value.TxTLog)
            {
                fileLog.End(totalPlannedTickets, totalPlannedUsers, locations, options);
                await SaveLogFile(fileLog);
            }

            // Calls a method within the ticketservice that pushes and updates the new calculated values to the database.
            if (options.Value.SaveToDatabase)
                await ticketService.UpdatePlanning(analyticsResults);

            // When everything is processed gives back some statics, but only if logging is enabled in the options.Value
            if (options.Value.ConsoleLog)
            {
                Log.AddConsoleLog($"\nTotaal aantal geplande tickets: {totalPlannedTickets}", options);
                Log.AddConsoleLog($"Totaal aantal unique geplande users: {totalPlannedUsers}", options);
                Log.AddConsoleLog($"\nApplication Version : {ApplicationOptions.Version}", options);
                Log.AddConsoleLog($"Written by          : {ApplicationOptions.Author}", options);
            }

            // Waiting 1 minute, before closing the application.
            Thread.Sleep(60000);

            // Exit the application.
            Environment.Exit(1);
        }

        /// <summary>
        ///     Calculate ticket timings.
        /// </summary>
        /// <param name="analyticsResult"></param>
        /// <returns></returns>
        private async Task CalculateTicketHours(AnalyticsResult analyticsResult)
        {
            if (analyticsResult.Ticket != null)
            {
                // Setting entity values
                analyticsResult.Ticket.PlnGeplandeAnalUren = analyticsResult.Ticket.PlnSchattingAnalUren;
                analyticsResult.Ticket.PlnGeplandeProgUren = analyticsResult.Ticket.PlnSchattingProgUren;
                analyticsResult.Ticket.PlnGeplandeTestUren = analyticsResult.Ticket.PlnSchattingTestUren;

                // Excecutes this if statement, but only when all the hours combined equals to 0.
                if (analyticsResult.Ticket.PlnGeplandeAnalUren + analyticsResult.Ticket.PlnGeplandeProgUren +
                    analyticsResult.Ticket.PlnGeplandeTestUren == 0)
                    switch (analyticsResult.Ticket.PlnComplexiteit)
                    {
                        case 0:
                        {
                            analyticsResult.Ticket.PlnGeplandeAnalUren = 1;
                            analyticsResult.Ticket.PlnGeplandeProgUren = 2;
                            analyticsResult.Ticket.PlnGeplandeTestUren = 1;
                            break;
                        }
                        case 1:
                        {
                            analyticsResult.Ticket.PlnGeplandeAnalUren = 2;
                            analyticsResult.Ticket.PlnGeplandeProgUren = 4;
                            analyticsResult.Ticket.PlnGeplandeTestUren = 2;
                            break;
                        }
                        case 2:
                        {
                            analyticsResult.Ticket.PlnGeplandeAnalUren = 10;
                            analyticsResult.Ticket.PlnGeplandeProgUren = 20;
                            analyticsResult.Ticket.PlnGeplandeTestUren = 10;
                            break;
                        }
                        case 3:
                        {
                            analyticsResult.Ticket.PlnGeplandeAnalUren = 25;
                            analyticsResult.Ticket.PlnGeplandeProgUren = 50;
                            analyticsResult.Ticket.PlnGeplandeTestUren = 25;
                            break;
                        }
                        default:
                        {
                            analyticsResult.Ticket.PlnGeplandeAnalUren = 3;
                            analyticsResult.Ticket.PlnGeplandeProgUren = 6;
                            analyticsResult.Ticket.PlnGeplandeTestUren = 3;
                            break;
                        }
                    }
            }
        }

        private async Task NextPlannedTicketDateTime(AnalyticsResult analyticsResult, bool newUser,
            DateTime previousTicketEndTime)
        {
            // Parsing the current time into a variable.
            var currentdateTime = DateTime.Now;

            if (!options.Value.UsingPreStart)
            {
                analyticsResult.Ticket.PreStart = false;
                analyticsResult.Ticket.PreStartPreviousRun = false;
            }


            // If the ticket calculation is not for the same person, as the one before then execute this statement
            // to be able to calculate when to plan the next ticket.
            if (newUser)
            {
                if (new TimeSpan(9, 00, 0) >= currentdateTime.TimeOfDay)
                {
                    if (analyticsResult.Ticket != null)
                        analyticsResult.Ticket.PlnGeplandeStartDatumTijd = await NextBusinessDay(
                            new DateTime(currentdateTime.Year, currentdateTime.Month, currentdateTime.Day, 9, 0, 00)
                                .AddDays(-1));
                }
                else
                {
                    if (analyticsResult.Ticket != null)
                        analyticsResult.Ticket.PlnGeplandeStartDatumTijd = await NextBusinessDay(
                            new DateTime(currentdateTime.Year, currentdateTime.Month, currentdateTime.Day, 9, 0, 00));
                }
            }

            // Else if it's the same person as the one before execute this statement.
            else
            {
                if (previousTicketEndTime.Hour >= 17)
                {
                    if (analyticsResult.Ticket != null)
                        analyticsResult.Ticket.PlnGeplandeStartDatumTijd = await NextBusinessDay(
                            new DateTime(previousTicketEndTime.Year, previousTicketEndTime.Month,
                                previousTicketEndTime.Day, 9, 0, 00));
                }
                else
                {
                    if (analyticsResult.Ticket != null)
                        analyticsResult.Ticket.PlnGeplandeStartDatumTijd = previousTicketEndTime;
                }
            }

            //Calculation PreStart
            if (analyticsResult.Ticket.PlnGeplandeRestUren == 0 && options.Value.UsingPreStart)
            {
                if (new TimeSpan(9, 00, 0) >= currentdateTime.TimeOfDay)
                {
                    if (analyticsResult.Ticket != null)
                        if (analyticsResult.Ticket.PlnGeplandeStartDatumTijd <= await NextBusinessDay(
                                new DateTime(currentdateTime.Year, currentdateTime.Month, currentdateTime.Day, 17, 0,
                                    00).AddDays(-1)))
                            analyticsResult.Ticket.PreStart = true;
                }
                else
                {
                    if (analyticsResult.Ticket != null)
                        if (analyticsResult.Ticket.PlnGeplandeStartDatumTijd <= await NextBusinessDay(
                                new DateTime(currentdateTime.Year, currentdateTime.Month, currentdateTime.Day, 17, 0,
                                    00)))
                            analyticsResult.Ticket.PreStart = true;
                }
            }

            //Calculation PreStartPreviousRun. Set to False if PlnGeplandeStartDatumTijd is lower the PlnPrognoseXXXXStart
            if (analyticsResult.Ticket.PlnGeplandeRestUren == 0 && options.Value.UsingPreStart &&
                analyticsResult.Ticket.PreStartPreviousRun)
                switch (analyticsResult.Ticket.Activity)
                {
                    case TicketActivity.Analyse:
                    {
                        if (analyticsResult.Ticket.PlnPrognoseAnalStart >=
                            analyticsResult.Ticket.PlnGeplandeStartDatumTijd)
                            analyticsResult.Ticket.PreStartPreviousRun = false;
                        break;
                    }
                    case TicketActivity.Programmeren:
                    {
                        if (analyticsResult.Ticket.PlnPrognoseProgStart >=
                            analyticsResult.Ticket.PlnGeplandeStartDatumTijd)
                            analyticsResult.Ticket.PreStartPreviousRun = false;
                        break;
                    }
                    case TicketActivity.Testen:
                    {
                        if (analyticsResult.Ticket.PlnPrognoseTestStart >=
                            analyticsResult.Ticket.PlnGeplandeStartDatumTijd)
                            analyticsResult.Ticket.PreStartPreviousRun = false;
                        break;
                    }
                    case TicketActivity.Unkown:
                        break;
                }

            // If the PlnGeplandeRestUren == 0 (start of main ticket) then set PlnPrognoseXXXXStart
            if (analyticsResult.Ticket.PlnGeplandeRestUren == 0)
                switch (analyticsResult.Ticket.Activity)
                {
                    case TicketActivity.Analyse:
                    {
                        analyticsResult.Ticket.PlnGeplandeRestUren = analyticsResult.Ticket.PlnGeplandeAnalUren;
                        analyticsResult.Ticket.PlnGeplandeUren = analyticsResult.Ticket.PlnGeplandeAnalUren;
                        // If prestart = false then set PlnPrognoseAnalStart to PlnGeplandeStartDatumTijd
                        if (analyticsResult.Ticket.PreStart == false ||
                            analyticsResult.Ticket.PreStartPreviousRun == false)
                            analyticsResult.Ticket.PlnPrognoseAnalStart =
                                analyticsResult.Ticket.PlnGeplandeStartDatumTijd;
                        // If prestart = true and no current Prognose Start date then set date to PlnGeplandeStartDatumTijd
                        else if (analyticsResult.Ticket.PlnPrognoseAnalStart == new DateTime(0001, 01, 01, 00, 00, 00))
                            analyticsResult.Ticket.PlnPrognoseAnalStart =
                                analyticsResult.Ticket.PlnGeplandeStartDatumTijd;
                        break;
                    }
                    case TicketActivity.Programmeren:
                    {
                        analyticsResult.Ticket.PlnGeplandeRestUren = analyticsResult.Ticket.PlnGeplandeProgUren;
                        analyticsResult.Ticket.PlnGeplandeUren = analyticsResult.Ticket.PlnGeplandeProgUren;
                        // If prestart = false then set PlnPrognoseProgStart to PlnGeplandeStartDatumTijd
                        if (analyticsResult.Ticket.PreStart == false ||
                            analyticsResult.Ticket.PreStartPreviousRun == false)
                            analyticsResult.Ticket.PlnPrognoseProgStart =
                                analyticsResult.Ticket.PlnGeplandeStartDatumTijd;
                        // If prestart = true and no current Prognose Start date then set date to PlnGeplandeStartDatumTijd
                        else if (analyticsResult.Ticket.PlnPrognoseProgStart == new DateTime(0001, 01, 01, 00, 00, 00))
                            analyticsResult.Ticket.PlnPrognoseProgStart =
                                analyticsResult.Ticket.PlnGeplandeStartDatumTijd;
                        break;
                    }
                    case TicketActivity.Testen:
                    {
                        analyticsResult.Ticket.PlnGeplandeRestUren = analyticsResult.Ticket.PlnGeplandeTestUren;
                        analyticsResult.Ticket.PlnGeplandeUren = analyticsResult.Ticket.PlnGeplandeTestUren;
                        // If prestart = false then set PlnPrognoseTestStart to PlnGeplandeStartDatumTijd
                        if (analyticsResult.Ticket.PreStart == false ||
                            analyticsResult.Ticket.PreStartPreviousRun == false)
                            analyticsResult.Ticket.PlnPrognoseTestStart =
                                analyticsResult.Ticket.PlnGeplandeStartDatumTijd;
                        // If prestart = true and no current Prognose Start date then set date to PlnGeplandeStartDatumTijd
                        else if (analyticsResult.Ticket.PlnPrognoseTestStart == new DateTime(0001, 01, 01, 00, 00, 00))
                            analyticsResult.Ticket.PlnPrognoseTestStart =
                                analyticsResult.Ticket.PlnGeplandeStartDatumTijd;
                        break;
                    }
                    case TicketActivity.Unkown:
                        break;
                    default:
                    {
                        analyticsResult.Ticket.PlnGeplandeRestUren = 0;
                        break;
                    }
                }


            // Calculation Planning End time
            var resterendeUrenDag = 17 - analyticsResult.Ticket.PlnGeplandeStartDatumTijd.Hour;

            if (analyticsResult.Ticket.PlnGeplandeRestUren <= resterendeUrenDag)
            {
                // End of the ticket.
                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd =
                    analyticsResult.Ticket.PlnGeplandeStartDatumTijd.AddHours(analyticsResult.Ticket
                        .PlnGeplandeRestUren);
                analyticsResult.Ticket.PlnGeplandeRestUren = 0;

                // Getting the ticket start/end time.
                switch (analyticsResult.Ticket.Activity)
                {
                    case TicketActivity.Analyse:
                    {
                        // Set End Time to 13:00 if starttime is >= 11:00
                        if (analyticsResult.Ticket.PlnGeplandeStartDatumTijd.Hour <= 11 &&
                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Hour >= 13)
                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd = new DateTime(
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Year,
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Month,
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Day, 13, 0, 00);

                        // Set new PlnPrognoseAnalEindGecor date
                        analyticsResult.Ticket.PlnPrognoseAnalEindGecor =
                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;

                        // If prestart = false then set PlnPrognoseAnalEind to PlnGeplandeEindDatumTijd
                        if (analyticsResult.Ticket.PreStart == false ||
                            analyticsResult.Ticket.PreStartPreviousRun == false)
                        {
                            analyticsResult.Ticket.PlnPrognoseAnalEind =
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;
                            analyticsResult.Ticket.PlnPrognoseAnalEindGecor = new DateTime(2000, 01, 01, 00, 00, 00);
                        }
                        // If prestart = true and no current Prognose Eind date then set date to PlnGeplandeEindDatumTijd
                        else

                            // Set CorrectionPrognoseEndDateTime = True
                        {
                            analyticsResult.Ticket.CorrectionPrognoseEndDateTime = true;
                        }

                        if (analyticsResult.Ticket.PlnPrognoseAnalEind == new DateTime(0001, 01, 01, 00, 00, 00))
                            analyticsResult.Ticket.PlnPrognoseAnalEind =
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;
                        break;
                    }
                    case TicketActivity.Programmeren:
                    {
                        // Set End Time to 13:00 if starttime is >= 11:00
                        if (analyticsResult.Ticket.PlnGeplandeStartDatumTijd.Hour <= 11 &&
                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Hour >= 13)
                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd = new DateTime(
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Year,
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Month,
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Day, 13, 0, 00);


                        // Set new PlnPrognoseProgEindGecor date
                        analyticsResult.Ticket.PlnPrognoseProgEindGecor =
                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;

                        // If prestart = false then set PlnPrognoseProgEind to PlnGeplandeEindDatumTijd
                        if (analyticsResult.Ticket.PreStart == false ||
                            analyticsResult.Ticket.PreStartPreviousRun == false)
                        {
                            analyticsResult.Ticket.PlnPrognoseProgEind =
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;
                            analyticsResult.Ticket.PlnPrognoseProgEindGecor = new DateTime(2000, 01, 01, 00, 00, 00);
                        }

                        // If prestart = true and no current Prog Eind date then set date to PlnGeplandeEindDatumTijd
                        else

                            // Set CorrectionPrognoseEndDateTime = True
                        {
                            analyticsResult.Ticket.CorrectionPrognoseEndDateTime = true;
                        }

                        if (analyticsResult.Ticket.PlnPrognoseProgEind == new DateTime(0001, 01, 01, 00, 00, 00))
                            analyticsResult.Ticket.PlnPrognoseProgEind =
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;
                        break;
                    }
                    case TicketActivity.Testen:
                    {
                        // Set End Time to 13:00 if starttime is >= 11:00
                        if (analyticsResult.Ticket.PlnGeplandeStartDatumTijd.Hour <= 11 &&
                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Hour >= 13)
                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd = new DateTime(
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Year,
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Month,
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Day, 13, 0, 00);

                        // Set new PlnPrognoseTestEindGecor date
                        analyticsResult.Ticket.PlnPrognoseTestEindGecor =
                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;

                        // If prestart = false then set PlnPrognoseTestEind to PlnGeplandeEindDatumTijd
                        if (analyticsResult.Ticket.PreStart == false ||
                            analyticsResult.Ticket.PreStartPreviousRun == false)
                        {
                            analyticsResult.Ticket.PlnPrognoseTestEind =
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;
                            analyticsResult.Ticket.PlnPrognoseTestEindGecor = new DateTime(2000, 01, 01, 00, 00, 00);
                        }
                        // If prestart = true and no current Test Eind date then set date to PlnGeplandeEindDatumTijd
                        else

                            // Set CorrectionPrognoseEndDateTime = True
                        {
                            analyticsResult.Ticket.CorrectionPrognoseEndDateTime = true;
                        }

                        if (analyticsResult.Ticket.PlnPrognoseTestEind == new DateTime(0001, 01, 01, 00, 00, 00))
                            analyticsResult.Ticket.PlnPrognoseTestEind =
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;
                        break;
                    }
                    case TicketActivity.Unkown:
                        break;
                }
            }
            else
            {
                //Set Planned end time and go for a new day!
                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd =
                    analyticsResult.Ticket.PlnGeplandeStartDatumTijd.AddHours(resterendeUrenDag);
                analyticsResult.Ticket.PlnGeplandeRestUren =
                    analyticsResult.Ticket.PlnGeplandeRestUren - resterendeUrenDag;

                // Getting the ticket start/end time.

                if (analyticsResult.Ticket.PreStart && analyticsResult.Ticket.PreStartPreviousRun)
                    switch (analyticsResult.Ticket.Activity)
                    {
                        case TicketActivity.Analyse:
                        {
                            if (analyticsResult.Ticket.PlnPrognoseAnalEind <=
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd)
                            {
                                // Set CorrectionPrognoseEndDateTime = True & PlnGeplandeEindeDatumTijd to 13:00
                                if (analyticsResult.Ticket.PlnPrognoseAnalEind !=
                                    analyticsResult.Ticket.PlnGeplandeEindeDatumTijd)
                                {
                                    // Set End Time to 13:00 if starttime is >= 11:00
                                    if (analyticsResult.Ticket.PlnGeplandeStartDatumTijd.Hour <= 11)
                                        analyticsResult.Ticket.PlnGeplandeEindeDatumTijd = new DateTime(
                                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Year,
                                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Month,
                                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Day, 13, 0, 00);
                                    analyticsResult.Ticket.CorrectionPrognoseEndDateTime = true;
                                }

                                analyticsResult.Ticket.PlnGeplandeRestUren = 0;
                                analyticsResult.Ticket.PlnPrognoseAnalEindGecor =
                                    analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;
                            }

                            break;
                        }
                        case TicketActivity.Programmeren:
                        {
                            if (analyticsResult.Ticket.PlnPrognoseProgEind <=
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd)
                            {
                                analyticsResult.Ticket.PlnGeplandeRestUren = 0;
                                analyticsResult.Ticket.PlnPrognoseProgEindGecor =
                                    analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;

                                // Set CorrectionPrognoseEndDateTime = True & PlnGeplandeEindeDatumTijd to 13:00
                                if (analyticsResult.Ticket.PlnPrognoseProgEind !=
                                    analyticsResult.Ticket.PlnGeplandeEindeDatumTijd)
                                {
                                    // Set End Time to 13:00 if starttime is >= 11:00
                                    if (analyticsResult.Ticket.PlnGeplandeStartDatumTijd.Hour <= 11)
                                        analyticsResult.Ticket.PlnGeplandeEindeDatumTijd = new DateTime(
                                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Year,
                                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Month,
                                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Day, 13, 0, 00);
                                    analyticsResult.Ticket.CorrectionPrognoseEndDateTime = true;
                                }

                                analyticsResult.Ticket.PlnGeplandeRestUren = 0;
                                analyticsResult.Ticket.PlnPrognoseProgEindGecor =
                                    analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;
                            }

                            break;
                        }
                        case TicketActivity.Testen:
                        {
                            if (analyticsResult.Ticket.PlnPrognoseTestEind <=
                                analyticsResult.Ticket.PlnGeplandeEindeDatumTijd)
                            {
                                // Set CorrectionPrognoseEndDateTime = True & PlnGeplandeEindeDatumTijd to 13:00
                                if (analyticsResult.Ticket.PlnPrognoseTestEind !=
                                    analyticsResult.Ticket.PlnGeplandeEindeDatumTijd)
                                {
                                    // Set End Time to 13:00 if starttime is >= 11:00
                                    if (analyticsResult.Ticket.PlnGeplandeStartDatumTijd.Hour <= 11)
                                        analyticsResult.Ticket.PlnGeplandeEindeDatumTijd = new DateTime(
                                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Year,
                                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Month,
                                            analyticsResult.Ticket.PlnGeplandeEindeDatumTijd.Day, 13, 0, 00);
                                    analyticsResult.Ticket.CorrectionPrognoseEndDateTime = true;
                                }

                                analyticsResult.Ticket.PlnGeplandeRestUren = 0;
                                analyticsResult.Ticket.PlnPrognoseTestEindGecor =
                                    analyticsResult.Ticket.PlnGeplandeEindeDatumTijd;
                            }

                            break;
                        }
                        case TicketActivity.Unkown:
                            break;
                    }
            }
        }

        // A function to prevent tickets being planned in the weekends.
        private async Task<DateTime> NextBusinessDay(DateTime currentDay)
        {
            //The next business day should always be at least the next day
            var nextBusinessDay = currentDay.AddDays(1);

            // If the next day equals to Saturday, or Sunday depending of the day, 1 or 2 days gets added
            if (nextBusinessDay.DayOfWeek != DayOfWeek.Saturday && nextBusinessDay.DayOfWeek != DayOfWeek.Sunday)
                return nextBusinessDay;
            if (nextBusinessDay.DayOfWeek == DayOfWeek.Saturday) nextBusinessDay = nextBusinessDay.AddDays(2);
            if (nextBusinessDay.DayOfWeek == DayOfWeek.Sunday) nextBusinessDay = nextBusinessDay.AddDays(1);

            // Maybe a function to calculate people that are on vacation?

            // Returns the new calculated NextBusinessDay
            return nextBusinessDay;
        }

        private async Task TicketActivityCalculator(AnalyticsResult analyticsResult)
        {
            if (analyticsResult.Ticket != null)
                analyticsResult.Ticket.Activity = analyticsResult.Ticket.Status switch
                {
                    TicketStatus.Planned => analyticsResult.Ticket.PlnGeplandeAnalUren > 0
                        ? TicketActivity.Analyse
                        : TicketActivity.Programmeren,
                    TicketStatus.Foto => TicketActivity.Analyse,
                    TicketStatus.AnalystTest => TicketActivity.Analyse,
                    TicketStatus.Analyse => TicketActivity.Analyse,
                    TicketStatus.Development => TicketActivity.Programmeren,
                    TicketStatus.DevelopmentTest => TicketActivity.Programmeren,
                    TicketStatus.OfferedForTest => TicketActivity.Testen,
                    TicketStatus.AcceptanceTest => TicketActivity.Testen,
                    TicketStatus.SystemTest => TicketActivity.Testen,
                    _ => TicketActivity.Unkown
                };
        }

        private async Task TicketDescription(AnalyticsResult analyticsResult)
        {
            if (analyticsResult.Ticket is {Omschrijving: { }})
            {
                //Create description HTMLText
                analyticsResult.Ticket.OmschrijvingHtml =
                    analyticsResult.Ticket.Omschrijving.Replace("\n", "\n   ").Replace("\r", "");
                var charCount = 0;
                var lines = analyticsResult.Ticket.OmschrijvingHtml
                    .Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries)
                    .GroupBy(w => (charCount += w.Length + 1) / 64)
                    .Select(g => string.Join(" ", g));
                analyticsResult.Ticket.OmschrijvingHtml = string.Join("\n   ", lines.ToArray());
                charCount = 0;

                //Create description PlainText
                analyticsResult.Ticket.OmschrijvingPlain = Regex
                    .Replace(analyticsResult.Ticket.Omschrijving, "<[^>]*>", "").Replace("&nbsp;", "")
                    .Replace("\n\n", "\n").Replace("\r", "");
                analyticsResult.Ticket.OmschrijvingPlain = analyticsResult.Ticket.OmschrijvingPlain.TrimStart('\n');
                analyticsResult.Ticket.OmschrijvingPlain = analyticsResult.Ticket.OmschrijvingPlain.TrimStart('\t');
                analyticsResult.Ticket.OmschrijvingPlain = analyticsResult.Ticket.OmschrijvingPlain.TrimStart('\n');

                analyticsResult.Ticket.Omschrijving = analyticsResult.Ticket.OmschrijvingPlain;

                analyticsResult.Ticket.OmschrijvingPlain =
                    analyticsResult.Ticket.OmschrijvingPlain.Replace("\n", @"\n");
                lines = analyticsResult.Ticket.OmschrijvingPlain
                    .Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries)
                    .GroupBy(w => (charCount += w.Length + 1) / 64)
                    .Select(g => string.Join(" ", g));
                analyticsResult.Ticket.OmschrijvingPlain = string.Join("\n   ", lines.ToArray());
            }
        }

        private async Task SaveCalendar(Calendar calendar)
        {
            var fileName = calendar.Name.Replace(" ", "");
            var file = options.Value.Paths.AbsoluteCalendarLocation == ""
                ? new FileInfo(AppDomain.CurrentDomain.BaseDirectory + options.Value.Paths.AbsoluteCalendarLocation +
                               "/" + fileName + ".ics")
                : new FileInfo(options.Value.Paths.AbsoluteCalendarLocation + "/" + fileName + ".ics");
            try
            {
                file.Directory?.Create();
            }
            catch (Exception ex)
            {
                Log.AddConsoleLog(
                    $"An error occured while trying to create the file directory {file.Directory}.\nError: {ex}",
                    ConsoleColor.Red, options);
            }

            try
            {
                await File.WriteAllTextAsync(file.FullName, calendar.ToString());
            }
            catch (Exception ex)
            {
                Log.AddConsoleLog($"Something went wrong while saving the files to {file.Directory}.\n Error: {ex}",
                    ConsoleColor.Red, options);
                throw;
            }
        }

        private async Task SaveLogFile(LogFile logFile)
        {
            const string fileName = "LogFile";
            var file = options.Value.Paths.AbsoluteLogLocation == ""
                ? new FileInfo(AppDomain.CurrentDomain.BaseDirectory + options.Value.Paths.AbsoluteLogLocation +
                               "/" + fileName + ".txt")
                : new FileInfo(options.Value.Paths.AbsoluteLogLocation + "/" + fileName + ".txt");
            try
            {
                file.Directory?.Create();
            }
            catch (Exception ex)
            {
                Log.AddConsoleLog(
                    $"An error occured while trying to create the file directory {file.Directory}.\nError: {ex}",
                    ConsoleColor.Red, options);
            }

            try
            {
                await File.WriteAllTextAsync(file.FullName, logFile.ToString());
            }
            catch (Exception ex)
            {
                Log.AddConsoleLog($"Something went wrong while saving the files to {file.Directory}.\n Error: {ex}",
                    ConsoleColor.Red, options);
                throw;
            }
        }
    }
}