namespace TicketService.Application.Options
{
    public class ApplicationOptions
    {
        public const string Version = "1.3.0";

        public const string Author = "Bram Dekker";

        public const string LogFolder = "log";

        public const string CalendarFolder = "calendars";

        public bool GlobalCalendar { get; set; }

        public bool TxTLog { get; set; }

        public bool ConsoleLog { get; set; }

        public string RefreshInterval { get; set; }

        public bool SaveToDatabase { get; set; }

        public bool UsingPreStart { get; set; }

        public SqlOptions Sql { get; set; }

        public SftpOptions Sftp { get; set; }
        public ServerOptions Server { get; set; }

        public PathsOptions Paths { get; set; }
    }
}