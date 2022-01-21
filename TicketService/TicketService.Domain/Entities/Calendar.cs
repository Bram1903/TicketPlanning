#nullable enable
using System.Text;

namespace TicketService.Domain.Entities
{
    public class Calendar
    {
        private const string End = "END:VCALENDAR";

        // Setting up a new string builder
        private readonly StringBuilder _start = new();

        // Base class for the calandar generator, if the parameters have been given.
        public Calendar(string name, bool common = false, string refreshInterval = "16M")
        {
            Name = name;
            GenerateCalendarStart(refreshInterval);
            AddTimeZoneInfo();

            Body.Append("NAME:" + name + "\r\nX-WR-CALNAME:" + name + "'s agenda\r\n");
            Common = common;
        }

        public Calendar()
        {
        }

        public string Name { get; } = null!;

        public bool Common { get; }

        // Setting up a new string builder
        public StringBuilder Body { get; } = new();

        /// <summary>
        ///     Creates the first few lines of calendar metadata
        /// </summary>
        /// <returns>StringBuilder you can use to add the rest of the calendar data to</returns>
        private void GenerateCalendarStart(string refreshInterval = "16M")
        {
            _start.AppendLine("BEGIN:VCALENDAR");
            _start.AppendLine("VERSION:2.0");
            _start.AppendLine("PRODID:-//KRAAN//KRAAN kalender 1.0//NL");
            _start.AppendLine("CALSCALE:GREGORIAN");
            _start.AppendLine("METHOD:PUBLISH");
            _start.AppendLine("REFRESH-INTERVAL;VALUE=DURATION:PT" + refreshInterval + "\r\nX-PUBLISHED-TTL:PT" +
                              refreshInterval);
        }

        private void AddTimeZoneInfo()
        {
            _start.AppendLine("BEGIN:VTIMEZONE");
            _start.AppendLine("TZID:Europe/Amsterdam");
            _start.AppendLine("X-LIC-LOCATION:Europe/Amsterdam");
            _start.AppendLine("BEGIN:DAYLIGHT");
            _start.AppendLine("TZOFFSETFROM:+0100");
            _start.AppendLine("TZOFFSETTO:+0200");
            _start.AppendLine("TZNAME:CEST");
            _start.AppendLine("DTSTART:19700329T020000");
            _start.AppendLine("RRULE:FREQ=YEARLY;BYMONTH=3; BYDAY=-1SU");
            _start.AppendLine("END:DAYLIGHT");
            _start.AppendLine("BEGIN:STANDARD");
            _start.AppendLine("TZOFFSETFROM:+0200");
            _start.AppendLine("TZOFFSETTO:+0100");
            _start.AppendLine("TZNAME:CET");
            _start.AppendLine("DTSTART:19701025T030000");
            _start.AppendLine("RRULE:FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU");
            _start.AppendLine("END:STANDARD");
            _start.AppendLine("END:VTIMEZONE");
        }

        // Returns a string representation of the calendar
        public new string ToString()
        {
            return _start + "\n" + Body + "\n" + End;
        }
    }
}