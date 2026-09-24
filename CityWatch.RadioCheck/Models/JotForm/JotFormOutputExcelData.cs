
using System.Collections.Generic;

namespace CityWatch.RadioCheck.Models.JotForm
{
    public class JotFormOutputExcelData
    {
        public List<string> Headers { get; set; }
        public List<JotFormField> HeaderList { get; set; }
        public List<Dictionary<string, object>> Rows { get; set; }
    }
}
