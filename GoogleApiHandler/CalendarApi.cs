using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GoogleApiHandler.Defines;

namespace tomxyz.googleapi
{
    /// <summary>
    /// Calendar api handler
    /// </summary>
    public class CalendarApi
    {
        const string Application = "Calendar Updated";
        protected UserCredential? Credentials { get; set; }
        protected CalendarService? Service { get; set; }
        protected string[] Scopes = new string[] { "https://www.googleapis.com/auth/calendar.readonly", "https://www.googleapis.com/auth/calendar.events" }; // user basic profile

        /// <summary>
        /// Authenticate with OAuth 2.0 Client ID json downloaded from google console
        /// </summary>
        /// <param name="credentialFilePath">json file path</param>
        public async Task AutenticateAsync(string credentialFilePath)
        {
            // Load client secrets.
            using var stream = new FileStream(credentialFilePath, FileMode.Open, FileAccess.Read);
            string tokenPath = "token.json";

            Credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(tokenPath, true));

            Service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Credentials,
                ApplicationName = Application
            });
        }

        /// <summary>
        /// List all calendars
        /// </summary>
        /// <returns>calendards</returns>
        public async Task<IEnumerable<Calendar>> ListCalendarsAsync()
        {
            if (Service == null)
                throw new Exception("Google account is not authenticated");

            var result = await Service.CalendarList.List().ExecuteAsync();
            return result.Items.Select(x => new Calendar(x.Summary, x.Id));
        }

        /// <summary>
        /// Get all events from the calendar
        /// </summary>
        /// <param name="calendarId">calendar id</param>
        /// <param name="yearsToProcess">count of years to process</param>
        /// <returns>events</returns>
        public async Task<IEnumerable<Event>> GetAllEventsAsync(string calendarId, int yearsToProcess)
        {
            if (Service == null)
                throw new Exception("Google account is not authenticated");

            var items = new List<Event>();

            var now = DateTime.Now;
            var request = Service.Events.List(calendarId);
            request.TimeMin = new DateTime(now.Year, 1, 1);
            request.TimeMax = new DateTime(now.Year + yearsToProcess, 12, 31);
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            Google.Apis.Calendar.v3.Data.Events result;
            do
            {
                result = await request.ExecuteAsync();
                if (result != null)
                {
                    items.AddRange(result.Items.Select(x => new Event(x.Id, x.Summary, x.Start.Date)));
                    request.PageToken = result.NextPageToken;
                }
            } while (result != null && result.NextPageToken != null);

            return items;
        }

        /// <summary>
        /// Update event summary (description)
        /// </summary>
        /// <param name="calendarId">calendar id</param>
        /// <param name="eventId">event id</param>
        /// <param name="summary">new summary</param>
        /// <exception cref="Exception"></exception>
        public async Task UpdateEventSummaryAsync(string calendarId, string eventId, string summary)
        {
            if (Service == null)
                throw new Exception("Google account is not authenticated");

            var e = Service.Events.Get(calendarId, eventId).Execute();
            e.Summary = summary;

            var updater = Service.Events.Update(e, calendarId, eventId);
            await updater.ExecuteAsync();
        }
    }
}