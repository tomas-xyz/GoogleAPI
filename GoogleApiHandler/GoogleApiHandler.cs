using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GoogleApiHandler.Defines;

namespace GoogleApiHandler
{
    public class CalendarApi
    {
        const string Application = "Calendar Updated";
        protected UserCredential? Credentials { get; set; }
        protected CalendarService Service { get; set; }
        protected string[] Scopes = new string[] { "https://www.googleapis.com/auth/calendar.readonly", "https://www.googleapis.com/auth/calendar.events" }; // user basic profile

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

        public IEnumerable<Calendar> ListCalendars()
        {

            var calendars = Service.CalendarList.List().Execute().Items;
            return calendars.Select(x => new Calendar(x.Summary, x.Id));
        }

        public IEnumerable<Event> GetAllEvents(string calendarId, int year)
        {
            var request = Service.Events.List(calendarId);
            request.TimeMin = new DateTime(year, 1, 1);
            request.TimeMax = new DateTime(year, 12, 31);
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            return request.Execute().Items.Select(x => new Event(x.Id, x.Summary, x.Start.Date));
        }

        public void UpdateEventSummary(string calendarId, string eventId, string summary)
        {
            var e = Service.Events.Get(calendarId, eventId).Execute();
            e.Summary = summary;

            var updater = Service.Events.Update(e, calendarId, eventId);
            updater.Execute();
        }

    }
}