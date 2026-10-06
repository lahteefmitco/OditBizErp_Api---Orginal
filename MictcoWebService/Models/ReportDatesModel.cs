using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class ReportDatesModel
    {
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public int? location_id { get; set; }
        public string statement { get; set; }
    }
}
