using CommandLine;
using System.Text;
using System.Text.RegularExpressions;

namespace tomxyz.googleapi
{
    class CalendarAgeUpdater
    {
        /// <summary>
        /// Print usage help
        /// </summary>
        private static void PrintUsage()
        {
            Console.WriteLine($"Consider that you have event 'John Smith - birthday (2002)' in your calendar and want to add proper age for year 2022.");
            Console.WriteLine($"You can run:\r\nCalendarAgeUpdater.exe -r \"(.*) - birthday \\((.*)\\)\" -g 2 -p \"$1+ $Y+ ($2)\"");
            Console.WriteLine($"Event is updated to: 'John Smith 20 (2022)'");
        }

        /// <summary>
        /// Print current configuration
        /// </summary>
        private static void PrintConfiguration(string regex, int group, string targetPattern, int eventsYear)
        {
            Console.WriteLine($"Configuration: \r\nRegex to find: {regex}, \r\nGroup with age: {group}, \r\nTarget pattern: {targetPattern}, Year to list: {eventsYear}");
            Console.WriteLine();
        }

        /// <summary>
        /// Get new name of the event
        /// </summary>
        /// <param name="regexMatch">match for the event's summeary and configured regex</param>
        /// <param name="oneEvent">event from calendar</param>
        /// <param name="targetPattern">pattern for target name</param>
        /// <param name="yearGroup">group with tour from regexMatch</param>
        /// <returns>new summary/description or empty string if some error occures</returns>
        private static string CalculateNewEventSummary(Match regexMatch, GoogleApiHandler.Defines.Event oneEvent, string targetPattern, int yearGroup)
        {
            try
            {
                var year = int.Parse(regexMatch.Groups[yearGroup].Value);
                var age = oneEvent.Date.Year - year;
                string summary = string.Empty;

                var tokens = targetPattern.Split("+", StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in tokens)
                {
                    var matchGroup = Regex.Match(token, "(.*)\\$([0-9]+)(.*)");
                    var matchYear = Regex.Match(token, "(.*)\\$Y(.*)");
                    if (matchGroup.Success)
                    {
                        var group = int.Parse(matchGroup.Groups[2].Value);
                        summary += $"{matchGroup.Groups[1].Value}{regexMatch.Groups[group].Value}{matchGroup.Groups[3].Value}";
                    }
                    else if (matchYear.Success)
                    {// year
                        summary += $"{matchYear.Groups[1].Value}{age}{matchYear.Groups[2].Value}";
                    }
                    else
                    {
                        summary += token;
                    }
                }

                return summary;
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public static async Task StartProcessingAsync(string regexFind, int yearGroup, int? yearsToProcess, string targetPattern)
        {
            var rFind = new Regex(regexFind, RegexOptions.IgnoreCase);
            if (!yearsToProcess.HasValue)
                yearsToProcess = 1;

            // set utf-8 encoding for console output
            Console.OutputEncoding = Encoding.UTF8;

            var calendarApi = new CalendarApi();
            Console.WriteLine("Sign in to your Google account and consent permissions for the application");

            // authenticate
            await calendarApi.AutenticateAsync("auth.json");

            // list calendars
            int i = 0;
            var calendars = (await calendarApi.ListCalendarsAsync()).ToDictionary(x => ++i);

            Console.WriteLine("Listed calendars (select target): \r\n");
            foreach (var (n, c) in calendars)
                Console.WriteLine($"{n}: {c.Name}");

            Console.WriteLine();
            var read = Console.ReadLine();
            if (string.IsNullOrEmpty(read))
            {
                Console.WriteLine("No calendar selected. Exiting.");
            }

            var index = int.Parse(read);
            var calendar = calendars[index];
            Console.WriteLine($"Selected calendar: {index} - {calendar.Name}");
            Console.WriteLine();

            var events = await calendarApi.GetAllEventsAsync(calendar.Id, yearsToProcess.Value);
            var filtered = events
                .Select(x => (rFind.Match(x.Description), x))
                .Where(x => x.Item1.Success)
                .Select(x => (x.Item2, CalculateNewEventSummary(x.Item1, x.Item2, targetPattern, yearGroup)));

            while (true)
            {
                Console.WriteLine($"Press: \r\n\ta\tlist all events\r\n\tf\tlist filtered events\r\n\tr\tpairs of results\r\n\ts\tstart renaming\r\n\tother\texit");
                read = Console.ReadLine();

                if (read == "a")
                {
                    foreach (var e in events)
                        Console.WriteLine($"{e.Date}: {e.Description}");
                }
                else if (read == "f")
                {
                    foreach (var pair in filtered)
                        Console.WriteLine($"{pair.Item1.Date}: {pair.Item1.Description}");
                }
                else if (read == "r")
                {
                    foreach (var pair in filtered)
                        Console.WriteLine($"{pair.Item1.Date}: {pair.Item1.Description} -> {pair.Item2}");
                }
                else if (read == "s")
                {
                    foreach (var pair in filtered)
                    {
                        if (!string.IsNullOrEmpty(pair.Item2))
                            await calendarApi.UpdateEventSummaryAsync(calendar.Id, pair.Item1.Id, pair.Item2);
                    }

                    Console.WriteLine($"Events updated. New events in calendar: ");
                    events = await calendarApi.GetAllEventsAsync(calendar.Id, yearsToProcess.Value);
                    foreach (var e in events)
                        Console.WriteLine($"{e.Date}: {e.Description}");

                    break;
                }
                else
                    break;
            }
        }

        public static int Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {

                       StartProcessingAsync(o.Regex, o.Group, o.Years, o.Patttern)
                       .GetAwaiter().GetResult();
                   })
                   .WithNotParsed<Options>(async =>
                   {
                       PrintUsage();
                   });


                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: \r\n{e}");
                return 1;
            }
        }
    }
}
