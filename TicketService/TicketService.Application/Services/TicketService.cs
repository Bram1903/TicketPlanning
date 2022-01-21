using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TicketService.Application.Extensions;
using TicketService.Application.Interfaces;
using TicketService.Application.Options;
using TicketService.Domain.Entities;
using TicketService.Domain.Enumerations;

namespace TicketService.Application.Services
{
    public class TicketService : ITicketService
    {
        private readonly IConfiguration config;
        private readonly IDatabaseService databaseService;
        private readonly IOptions<ApplicationOptions> options;

        // Requests the database function file into this file
        public TicketService(IDatabaseService databaseService,
            //ILogger<TicketService> logger,
            IOptions<ApplicationOptions> options,
            IConfiguration config)
        {
            this.databaseService = databaseService;
            this.options = options;
            this.config = config;
        }

        // Fetches a single ticket, for debugging purposes by filtering on the ID
        public async Task<ICollection<Ticket>> Get(int id)
        {
            var singleTicket = new List<Ticket>();

            try
            {
                using var connection = databaseService.CreateConnection();
                SqlCommand sqlCommand = new(
                    "SELECT DevTicket.Id, DevTicket.Omschrijving, DevTicket.Type, DevTicket.Module, DevTicket.Priority, DevTicket.AangemaaktOp, DevTicket.AssignedTo, DevTicket.Status, DevTicket.PlnComplexiteit, DevTicket.PlnSchattingAnalUren, PlnSchattingProgUren, PlnSchattingTestUren, PlnGeplandeProgUren, PlnGeplandeAnalUren, PlnGeplandeTestUren, PlnGeplandeVersie, PlnPrognoseAnalStart, PlnPrognoseAnalEind, PlnPrognoseProgStart, PlnPrognoseProgEind, PlnPrognoseTestStart, PlnPrognoseTestEind, PlnPrognoseAnalEindGecor, PlnPrognoseProgEindGecor, PlnPrognoseTestEindGecor, PreStart, DevModule.Omschrijving as module_Omschrijving, DevModule.Id as module_Oid, DevTicketUser.VolledigeNaam as user_VolledigeNaam, DevTicketUser.Oid as user_Oid, DevProductLine.Omschrijving as productline_Omschrijving, DevProductLine.Id as productline_Oid " +
                    "FROM dbo.DevTicket " +
                    "INNER JOIN DevModule ON DevTicket.Module = DevModule.Id " +
                    "INNER JOIN DevTicketUser ON DevTicket.AssignedTo = DevTicketUser.Oid " +
                    "INNER JOIN DevProductLine ON DevTicket.ProductLine = DevProductLine.Id  " +
                    "WHERE DevTicket.Id = @id",
                    (SqlConnection) connection);
                sqlCommand.Parameters.AddWithValue("@id", id);

                // Putting the connection timeout on 5 minutes.
                sqlCommand.CommandTimeout = 300;

                var reader = await sqlCommand.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Enum.TryParse<TicketType>((reader["Type"] as int? ?? default).ToString(), out var newType);
                    Enum.TryParse<TicketPriority>((reader["Priority"] as int? ?? default).ToString(),
                        out var newPriority);
                    Enum.TryParse<TicketStatus>((reader["Status"] as int? ?? default).ToString(), out var newStatus);

                    // Create the entity ticket
                    Ticket ticket = new()
                    {
                        // Add the properties from the database results to the ticket entity
                        AssignedTo = new User
                        {
                            Oid = reader["user_Oid"] as Guid? ?? default,
                            VolledigeNaam = reader["user_VolledigeNaam"] as string
                        },
                        Id = reader["id"] as int? ?? default,
                        Type = newType,
                        Omschrijving = reader["Omschrijving"] as string,
                        Module = new Module
                        {
                            ModuleNaam = reader["module_Omschrijving"] as string
                        },
                        Product = new ProductLine
                        {
                            ProductNaam = reader["productline_Omschrijving"] as string
                        },
                        Priority = newPriority,
                        Status = newStatus,
                        PlnComplexiteit = reader["PlnComplexiteit"] as int?,
                        PlnSchattingAnalUren = reader["PlnSchattingAnalUren"] as int? ?? default,
                        PlnSchattingProgUren = reader["PlnSchattingProgUren"] as int? ?? default,
                        PlnSchattingTestUren = reader["PlnSchattingTestUren"] as int? ?? default,
                        PlnGeplandeProgUren = reader["PlnGeplandeProgUren"] as int? ?? default,
                        PlnGeplandeAnalUren = reader["PlnGeplandeAnalUren"] as int? ?? default,
                        PlnGeplandeTestUren = reader["PlnGeplandeTestUren"] as int? ?? default,
                        PlnPrognoseAnalStart = reader["PlnPrognoseAnalStart"] as DateTime? ?? default,
                        PlnPrognoseAnalEind = reader["PlnPrognoseAnalEind"] as DateTime? ?? default,
                        PlnPrognoseProgStart = reader["PlnPrognoseProgStart"] as DateTime? ?? default,
                        PlnPrognoseProgEind = reader["PlnPrognoseProgEind"] as DateTime? ?? default,
                        PlnPrognoseTestStart = reader["PlnPrognoseTestStart"] as DateTime? ?? default,
                        PlnPrognoseTestEind = reader["PlnPrognoseTestEind"] as DateTime? ?? default,
                        PlnPrognoseAnalEindGecor = reader["PlnPrognoseAnalEindGecor"] as DateTime? ?? default,
                        PlnPrognoseProgEindGecor = reader["PlnPrognoseProgEindGecor"] as DateTime? ?? default,
                        PlnPrognoseTestEindGecor = reader["PlnPrognoseTestEindGecor"] as DateTime? ?? default,
                        PreStart = reader["PreStart"] as bool? ?? default,
                        PreStartPreviousRun = reader["PreStart"] as bool? ?? default
                    };
                    // Add the ticket to the list
                    singleTicket.Add(ticket);
                }

                //Closes the connection, to prevent connections floating around.
                connection.Close();
            }
            // If something when wrong whilel fetching the data, log the following information.
            catch (Exception error)
            {
                //LogFile logFile = new();
                //LogFile.AddLogError(error.ToString(), "DataBaseService Error");
                //LogFile.Save();
                Console.Clear();
                Log.AddConsoleLog($"{error}\n\n", ConsoleColor.Red, options);
                Environment.Exit(1);
            }

            // Return the list
            return singleTicket;
        }

        // Fetches all the requested data, rather than only a single one.
        public async Task<ICollection<Ticket>> GetAll(int amount = 10, int page = 1)
        {
            var tickets = new List<Ticket>();

            try
            {
                using var connection = databaseService.CreateConnection();
                SqlCommand sqlCommand = new(
                    $"SELECT TOP {amount} DevTicket.AssignedTo, DevTicket.Id, DevTicket.Omschrijving, DevTicket.Type, DevTicket.Module, DevTicket.Priority, DevTicket.AangemaaktOp, DevTicket.Status, PlnComplexiteit, PlnSchattingAnalUren, PlnSchattingProgUren, PlnSchattingTestUren, PlnGeplandeProgUren, PlnGeplandeAnalUren, PlnGeplandeTestUren, PlnGeplandeVersie, PlnPrognoseAnalStart, PlnPrognoseAnalEind, PlnPrognoseProgStart, PlnPrognoseProgEind, PlnPrognoseTestStart, PlnPrognoseTestEind, PlnPrognoseAnalEindGecor, PlnPrognoseProgEindGecor, PlnPrognoseTestEindGecor, PreStart, DevModule.Omschrijving as module_Omschrijving, DevModule.Id as module_Oid, DevTicketUser.VolledigeNaam as user_VolledigeNaam, DevTicketUser.Oid as user_Oid, DevProductLine.Omschrijving as productline_Omschrijving, DevProductLine.Id as productline_Oid " +
                    "FROM dbo.DevTicket INNER JOIN DevModule ON DevTicket.Module = DevModule.Id " +
                    "INNER JOIN DevTicketUser ON DevTicket.AssignedTo = DevTicketUser.Oid " +
                    "INNER JOIN DevProductLine ON DevTicket.ProductLine = DevProductLine.Id " +
                    "ORDER BY [AssignedTo] desc, [PreStart] desc, [PlnGeplandeVersie] ASC, [Module]ASC , [Type] ASC , [Priority] ASC, [AangemaaktOp] desc",
                    (SqlConnection) connection);

                // Putting the connection timeout on 5 minutes.
                sqlCommand.CommandTimeout = 300;

                var reader = await sqlCommand.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Enum.TryParse<TicketType>((reader["Type"] as int? ?? default).ToString(), out var newType);
                    Enum.TryParse<TicketPriority>((reader["Priority"] as int? ?? default).ToString(),
                        out var newPriority);
                    Enum.TryParse<TicketStatus>((reader["Status"] as int? ?? default).ToString(), out var newStatus);

                    // Create entity ticket
                    Ticket ticket = new()
                    {
                        // Add the properties from the database results to the ticket entity
                        AssignedTo = new User
                        {
                            Oid = reader["user_Oid"] as Guid? ?? default,
                            VolledigeNaam = reader["user_VolledigeNaam"] as string
                        },
                        Id = reader["id"] as int? ?? default,
                        Type = newType,
                        Omschrijving = reader["Omschrijving"] as string,
                        Module = new Module
                        {
                            ModuleNaam = reader["module_Omschrijving"] as string
                        },
                        Product = new ProductLine
                        {
                            ProductNaam = reader["productline_Omschrijving"] as string
                        },
                        Priority = newPriority,
                        Status = newStatus,
                        PlnComplexiteit = reader["PlnComplexiteit"] as int?,
                        PlnSchattingAnalUren = reader["PlnSchattingAnalUren"] as int? ?? default,
                        PlnSchattingProgUren = reader["PlnSchattingProgUren"] as int? ?? default,
                        PlnSchattingTestUren = reader["PlnSchattingTestUren"] as int? ?? default,
                        PlnGeplandeProgUren = reader["PlnGeplandeProgUren"] as int? ?? default,
                        PlnGeplandeAnalUren = reader["PlnGeplandeAnalUren"] as int? ?? default,
                        PlnGeplandeTestUren = reader["PlnGeplandeTestUren"] as int? ?? default,
                        PlnPrognoseAnalStart = reader["PlnPrognoseAnalStart"] as DateTime? ?? default,
                        PlnPrognoseAnalEind = reader["PlnPrognoseAnalEind"] as DateTime? ?? default,
                        PlnPrognoseProgStart = reader["PlnPrognoseProgStart"] as DateTime? ?? default,
                        PlnPrognoseProgEind = reader["PlnPrognoseProgEind"] as DateTime? ?? default,
                        PlnPrognoseTestStart = reader["PlnPrognoseTestStart"] as DateTime? ?? default,
                        PlnPrognoseTestEind = reader["PlnPrognoseTestEind"] as DateTime? ?? default,
                        PlnPrognoseAnalEindGecor = reader["PlnPrognoseAnalEindGecor"] as DateTime? ?? default,
                        PlnPrognoseProgEindGecor = reader["PlnPrognoseProgEindGecor"] as DateTime? ?? default,
                        PlnPrognoseTestEindGecor = reader["PlnPrognoseTestEindGecor"] as DateTime? ?? default,
                        PreStart = reader["PreStart"] as bool? ?? default,
                        PreStartPreviousRun = reader["PreStart"] as bool? ?? default
                    };
                    // Add the ticket to the list
                    tickets.Add(ticket);
                }

                //Closes the connection, to prevent connections floating around
                connection.Close();
            }
            // If something goes wrong, returns the following message
            catch (Exception error)
            {
                //LogFile logFile = new();
                //LogFile.AddLogError(error.ToString(), "DataBaseService Error");
                //LogFile.Save();
                //Environment.Exit(1);
                Console.Clear();
                Log.AddConsoleLog($"{error}\n\n", ConsoleColor.Red, options);
                Environment.Exit(1);
            }

            // Return the list
            return tickets;
        }

        public async Task UpdatePlanning(IEnumerable<AnalyticsResult> analyticsResults)
        {
            try
            {
                // Requesting a connection from the database service.
                using var connection = databaseService.CreateConnection();

                // Looping through all the calculated ticekts, and push their updated values to the database.
                foreach (var analyticsResult in analyticsResults)
                {
                    if (analyticsResult.Ticket == null) continue;

                    // Afhankelijk van activteitsoort start en eind datum terugschrijven
                    switch (analyticsResult.Ticket.Activity)
                    {
                        case TicketActivity.Analyse:
                        {
                            SqlCommand sqlCommand = new(
                                "UPDATE dbo.DevTicket " +
                                "SET PlnPrognoseAnalStart = @PlnPrognoseAnalStart," +
                                "PlnPrognoseAnalEind = @PlnPrognoseAnalEind," +
                                "PlnPrognoseAnalEindGecor = @PlnPrognoseAnalEindGecor," +
                                "PreStart = @PreStart " +
                                "WHERE Id = @id",
                                (SqlConnection) connection);

                            sqlCommand.Parameters.AddWithValue("PlnPrognoseAnalStart",
                                analyticsResult.Ticket.PlnPrognoseAnalStart);
                            sqlCommand.Parameters.AddWithValue("PlnPrognoseAnalEind",
                                analyticsResult.Ticket.PlnPrognoseAnalEind);
                            sqlCommand.Parameters.AddWithValue("PlnPrognoseAnalEindGecor",
                                analyticsResult.Ticket.PlnPrognoseAnalEindGecor);
                            sqlCommand.Parameters.AddWithValue("Prestart ",
                                analyticsResult.Ticket.PreStart);

                            sqlCommand.Parameters.AddWithValue("@id", analyticsResult.Ticket.Id);

                            // Putting the connection timeout on 5 minutes.
                            sqlCommand.CommandTimeout = 300;

                            // Executing the update script asynchronously.
                            await sqlCommand.ExecuteNonQueryAsync();

                            break;
                        }
                        case TicketActivity.Programmeren:
                        {
                            SqlCommand sqlCommand = new(
                                "UPDATE dbo.DevTicket " +
                                "SET PlnPrognoseProgStart = @PlnPrognoseProgStart," +
                                "PlnPrognoseProgEind = @PlnPrognoseProgEind," +
                                "PlnPrognoseProgEindGecor = @PlnPrognoseProgEindGecor," +
                                "PreStart = @PreStart " +
                                "WHERE Id = @id",
                                (SqlConnection) connection);

                            sqlCommand.Parameters.AddWithValue("PlnPrognoseProgStart",
                                analyticsResult.Ticket.PlnPrognoseProgStart);
                            sqlCommand.Parameters.AddWithValue("PlnPrognoseProgEind",
                                analyticsResult.Ticket.PlnPrognoseProgEind);
                            sqlCommand.Parameters.AddWithValue("PlnPrognoseProgEindGecor",
                                analyticsResult.Ticket.PlnPrognoseProgEindGecor);
                            sqlCommand.Parameters.AddWithValue("Prestart ",
                                analyticsResult.Ticket.PreStart);

                            sqlCommand.Parameters.AddWithValue("@id", analyticsResult.Ticket.Id);

                            // Putting the connection timeout on 5 minutes.
                            sqlCommand.CommandTimeout = 300;

                            // Executing the update script asynchronously.
                            await sqlCommand.ExecuteNonQueryAsync();

                            break;
                        }
                        case TicketActivity.Testen:
                        {
                            SqlCommand sqlCommand = new(
                                "UPDATE dbo.DevTicket " +
                                "SET PlnPrognoseTestStart = @PlnPrognoseTestStart," +
                                "PlnPrognoseTestEind = @PlnPrognoseTestEind," +
                                "PlnPrognoseTestEindGecor = @PlnPrognoseTestEindGecor," +
                                "PreStart = @PreStart " +
                                "WHERE Id = @id",
                                (SqlConnection) connection);

                            sqlCommand.Parameters.AddWithValue("PlnPrognoseTestStart",
                                analyticsResult.Ticket.PlnPrognoseTestStart);
                            sqlCommand.Parameters.AddWithValue("PlnPrognoseTestEind",
                                analyticsResult.Ticket.PlnPrognoseTestEind);
                            sqlCommand.Parameters.AddWithValue("PlnPrognoseTestEindGecor",
                                analyticsResult.Ticket.PlnPrognoseTestEindGecor);
                            sqlCommand.Parameters.AddWithValue("Prestart ",
                                analyticsResult.Ticket.PreStart);

                            sqlCommand.Parameters.AddWithValue("@id", analyticsResult.Ticket.Id);

                            // Putting the connection timeout on 5 minutes.
                            sqlCommand.CommandTimeout = 300;

                            // Executing the update script asynchronously.
                            await sqlCommand.ExecuteNonQueryAsync();
                            break;
                        }
                        case TicketActivity.Unkown:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Log.AddConsoleLog($"Updated ticket: {analyticsResult.Ticket.Id}", options);
                }

                // Closing the connection, after all the filtered tickets have been updated.
                connection.Close();
            }
            catch (Exception ex)
            {
                // Logging the error, and even though there is an error the application won't close, as it won't break the application.
                Log.AddConsoleLog(
                    $"Something went wrong while updating the new calculated values to the database.\n Error: {ex}",
                    ConsoleColor.Red, options);
            }
        }
    }
}