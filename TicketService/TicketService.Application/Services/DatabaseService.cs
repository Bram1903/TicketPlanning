using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Options;
using TicketService.Application.Extensions;
using TicketService.Application.Interfaces;
using TicketService.Application.Options;

namespace TicketService.Application.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IOptions<ApplicationOptions> options;

        // Fetching the options / logger through dependency injection.
        public DatabaseService(IOptions<ApplicationOptions> options)
        {
            this.options = options;
        }


        // Creates a connection function, that can be used by other functions.
        public IDbConnection CreateConnection()
        {
            // Sets up easy variables to make the connection string more readable.
            var host = options.Value.Sql.Host;
            var database = options.Value.Sql.Database;
            var user = options.Value.Sql.User;
            var password = options.Value.Sql.Password;

            // Building the connetion string by parsing the variables in the hardcoded part.
            var conn = new SqlConnection($"Server={host};Database={database};User Id={user};Password={password};");

            // Basic logging information for in the console, as someone otherwise could get confused, of what the application is doing.
            Log.AddConsoleLog("\nTrying to establish a connection with the database.\n", options);

            // Simple try staement, to prevent the application from crashing upon failing to establish a connection.
            try
            {
                // Trying to open the connection.
                conn.Open();
            }
            catch (Exception ex)
            {
                // Parsing the error message into a string, so the user knows what to change.
                // Closing the application to prevent it to keep going.
                Log.AddConsoleLog(
                    $"Something went wrong while trying to establish a connection with the database. Check your connection credentials in the settings file. \n Error: {ex}",
                    ConsoleColor.Red, options);
                Environment.Exit(1);
            }

            // Connection has succesfully been established and giving back the connection.
            Log.AddConsoleLog("Succesfully established a connection with the databse.\n", options);
            return conn;
        }
    }
}