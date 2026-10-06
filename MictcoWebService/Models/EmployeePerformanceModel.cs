using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class EmployeePerformanceModel
    {
        public int hep_id { get; set; }
        public string hep_orderno { get; set; }
        public int hep_emp_acc_id { get; set; }
        public string hep_start_date { get; set; }
        public string hep_end_date { get; set; }
        public string hep_remarks { get; set; }
        public string hep_status { get; set; }
        public string emp_name { get; set; }
    }
}
