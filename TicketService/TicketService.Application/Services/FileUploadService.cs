using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using TicketService.Application.Extensions;
using TicketService.Application.Interfaces;
using TicketService.Application.Options;
using TicketService.Domain.Entities;

namespace TicketService.Application.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IOptions<ApplicationOptions> options;

        public FileUploadService(IOptions<ApplicationOptions> options)
        {
            this.options = options;
        }

        public async Task FileUploadSftp(LogFile fileLog)
        {
            // Fetching the settings, and parse them into a local variable.
            var host = options.Value.Sftp.Host;
            var port = options.Value.Sftp.Port; // Default SFTP port is 22
            var username = options.Value.Sftp.User;
            var password = options.Value.Sftp.Password;
            var remotedir = options.Value.Sftp.RemoteDirectory;

            // The path to all the calculated tickets.
            var uploadFiles = Directory.GetFiles(options.Value.Paths.AbsoluteCalendarLocation);

            // Setting up the connection info needed for a SFTP connection.
            using var client = new SftpClient(host, port, username, password);

            fileLog.Body.AppendLine("\n--- SFTP ---");
            fileLog.Body.AppendLine($"Trying to establish a connection with {host}");
            Log.AddConsoleLog("\n--- SFTP ---", options);
            Log.AddConsoleLog($"Trying to establish a connection with {host}", options);

            // Trying to establish a connection to the SFTP server with the given credentials.
            try
            {
                // Setting up the connection.
                client.Connect();

                // Logging information to both the logfile, and console logger.
                fileLog.Body.AppendLine("SFTP Connection established.");
                Log.AddConsoleLog("SFTP Connection established.", options);
            }
            catch (Exception error)
            {
                const string message =
                    @"Something went wrong while setting up a connection with the SFTP server. You might want to check the following things.
                                   
    - Is the SFTP server up?
    - Are the connection credentials correct?

Error:

";
                Log.AddConsoleLog(message, options);
                Log.AddConsoleLog(error.ToString(), options);
                fileLog.Body.AppendLine(message);
                fileLog.Body.AppendLine(error.ToString());
            }

            // If the client has successfully established a connetion to the SFTP server.
            if (client.IsConnected)
            {
                // Logging information.
                fileLog.Body.AppendLine($"\nSFTP Files removing from {remotedir}");
                Log.AddConsoleLog($"\nSFTP Files removing from {remotedir}", options);

                // Remove old .ics files, on the SFTP server
                foreach (var file in client.ListDirectory(remotedir))
                {
                    // Checks if the file is a calandar file.
                    if (!file.FullName.Contains(".ics")) continue;

                    // Trying to remove the file.
                    try
                    {
                        // Removing the file.
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        Log.AddConsoleLog($"An error occured while trying to remove {file.FullName}.", ConsoleColor.Red,
                            options);
                        fileLog.Body.AppendLine($"An error occured while trying to remove {file.FullName}.");
                    }

                    // If everything went fine, log the information.
                    fileLog.Body.AppendLine($"SFTP File Removed: {Path.GetFileName(file.FullName)}");
                    Log.AddConsoleLog($"SFTP File Removed: {Path.GetFileName(file.FullName)}", options);
                }

                fileLog.Body.AppendLine($"\nSFTP Files uploading to {remotedir}");
                Log.AddConsoleLog($"\nSFTP Files uploading to {remotedir}", options);

                // For each file in the calandar directory
                foreach (var file in uploadFiles)
                {
                    // Uploads each .ics file to the SFTP server
                    if (!file.Contains(".ics")) continue;

                    // Settig up the fileStream
                    await using var fileStream = new FileStream(file, FileMode.Open);
                    client.BufferSize = 4 * 1024; // bypass Payload error large files
                    try
                    {
                        // Trying to upload the file to the server location.
                        client.UploadFile(fileStream, remotedir + Path.GetFileName(file));
                    }
                    catch (Exception error)
                    {
                        Log.AddConsoleLog(
                            $"I do not have permission to write in {remotedir}. See the following error:\n {error}",
                            ConsoleColor.Red, options);
                        fileLog.Body.AppendLine(error.ToString());
                    }

                    // Logging information.
                    fileLog.Body.AppendLine($"SFTP File Uploaded to: {remotedir + Path.GetFileName(file)}");
                    Log.AddConsoleLog($"SFTP File Uploaded to: {remotedir + Path.GetFileName(file)}", options);
                }

                // Close the connection to the SFTP server.
                client.Disconnect();

                // Logging information.
                fileLog.Body.AppendLine("\nSFTP connection closed");
                Log.AddConsoleLog("\nSFTP connection closed", options);
            }
        }

        public async Task FileUploadServer(LogFile fileLog)
        {
            if (options.Value.Server.Upload)
            {
                // path for file you want to upload
                var uploadFiles = Directory.GetFiles(options.Value.Paths.AbsoluteCalendarLocation);

                fileLog.Body.AppendLine("\n--- Connect to Server ---");
                Log.AddConsoleLog("\n--- Connect to Server ---", options);

                fileLog.Body.AppendLine($"\nServer Files removing from {options.Value.Server.Url}");
                Log.AddConsoleLog($"\nServer Files removing from {options.Value.Server.Url}", options);

                if (!Directory.Exists(options.Value.Server.Url))
                {
                    Log.AddConsoleLog($"\nThe directory {options.Value.Server.Url} doesn't exist.", ConsoleColor.Red,
                        options);
                    Log.AddConsoleLog($"Please check / create the filepath: {options.Value.Server.Url}",
                        ConsoleColor.Red, options);
                    Log.AddConsoleLog(
                        "Application stopt with write data to File-Server and continu running the application\n",
                        ConsoleColor.Red, options);
                    fileLog.Body.AppendLine($"\nThe directory {options.Value.Server.Url} doesn't exist.");
                    fileLog.Body.AppendLine($"Please check / create the filepath: {options.Value.Server.Url}");
                    fileLog.Body.AppendLine(
                        "Application stopt with write data to File-Server and continu running the application\n");
                    return;
                }

                foreach (var file in Directory.GetFiles(options.Value.Server.Url))
                {
                    // Checks if the file contains .ics, so it doesn't remove other files by accident 
                    if (file.Contains(".ics"))
                        File.Delete(file);
                    fileLog.Body.AppendLine($"Server File Removed: {Path.GetFileName(file)}");
                    Log.AddConsoleLog($"Server File Removed: {Path.GetFileName(file)}", options);
                }

                fileLog.Body.AppendLine($"\nServer Files uploading to {options.Value.Server.Url}");
                Log.AddConsoleLog($"\nServer Files uploading to {options.Value.Server.Url}", options);

                // Uploads each .ics file to SFTP server
                foreach (var file in uploadFiles)
                {
                    if (!file.Contains(".ics")) continue;
                    try
                    {
                        File.Copy(file, options.Value.Server.Url + Path.GetFileName(file), true);
                    }
                    catch (Exception error)
                    {
                        Log.AddConsoleLog(
                            $"I do not have permission to write in {options.Value.Server.Url}. See the following error:\n {error}",
                            ConsoleColor.Red, options);
                        fileLog.Body.AppendLine(error.ToString());
                    }

                    fileLog.Body.AppendLine(
                        $"Server File Uploaded to: {options.Value.Server.Url + Path.GetFileName(file)}");
                    Log.AddConsoleLog($"Server File Uploaded to: {options.Value.Server.Url + Path.GetFileName(file)}",
                        options);
                }
            }
        }
    }
}