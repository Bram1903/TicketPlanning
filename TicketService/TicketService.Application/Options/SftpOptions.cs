namespace TicketService.Application.Options
{
    public class SftpOptions
    {
        public string WebsiteUrl { get; set; }
        public string Host { get; set; }

        public string User { get; set; }

        public string Password { get; set; }

        public string RemoteDirectory { get; set; }

        public bool Upload { get; set; }

        public int Port { get; set; }
    }
}