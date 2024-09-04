using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class AudioRecordingLog
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string BlobUrl { get; set; }
        public DateTime UploadedDate { get; set; }
    }

}
