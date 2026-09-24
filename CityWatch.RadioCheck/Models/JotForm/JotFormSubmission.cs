using System.Collections.Generic;

namespace CityWatch.RadioCheck.Models.JotForm
{
    public class JotFormSubmission
    {
        public string id { get; set; }
        public Dictionary<string, JotFormAnswer> answers { get; set; }
    }
}
