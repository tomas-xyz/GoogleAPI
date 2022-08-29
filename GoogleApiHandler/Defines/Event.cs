using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleApiHandler.Defines
{
    public class Event
    {
        public Event(string id, string description, string date)
        {
            Id = id;
            Description = description;
            UpdatedDescription = string.Empty;
            Date = DateTime.Parse(date);
        }

        public string Id { get; set; }
        public string Description { get; set; }
        public string UpdatedDescription { get; set; }
        public DateTime Date { get; set; }

    }
}
