using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class HrModel
    {
        public DateTime date { get; set; }
        public int hhoe_entryno { get; set; }
    }

    public class LeaveApplyModel
    {
        public string hla_leave_entrydate { get; set; }
        public int hla_leave_entryno { get; set; }
        public int hla_leave_empid { get; set; }
        public int hla_hlc_id { get; set; }
        public string hla_leave_from_date { get; set; }
        public string hla_leave_to_date { get; set; }
        public int hla_leave_half_full_day { get; set; }
        public string hla_leave_reason { get; set; }
        public string hla_leave_status { get; set; }
        public int hla_leave_approver_empid { get; set; }   
        public string statement { get; set; }      
        public decimal hla_leave_days { get; set; }
        public int hla_leave_userid { get; set; }
        public string name { get; set; }
        

    }

}
