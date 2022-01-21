#nullable enable
using System.Text;

namespace TicketService.Domain.Entities
{
    public class LogFile
    {
        // Setting up a new string builder
        private readonly StringBuilder _start = new();

        // Base class for the calandar generator, if the parameters have been given.
        public LogFile()
        {
            Body.AppendLine("Ticket Planning");
        }

        // Setting up a new string builder
        public StringBuilder Body { get; } = new();

        // Returns a string representation of the calendar
        public new string ToString()
        {
            return _start + "\n" + Body;
        }
    }
}