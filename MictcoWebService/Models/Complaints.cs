using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class Complaints
    {
        public int complaintId { get; set; }
        public int fixTypeId { get; set; }
        public double amount { get; set; } 
        public string remarks { get; set; }
        public double li_in { get; set; } = 0.00;
        public double li_out { get; set; } = 0.00;
        public string li_date { get; set; }
        public int li_as_id { get; set; }
    }
}
