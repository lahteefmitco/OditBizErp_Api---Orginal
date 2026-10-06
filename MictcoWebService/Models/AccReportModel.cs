using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class AccReportModel
    {
        public int asId { get; set; }
        public int salesManId { get; set; } = 0;
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public string todayDate { get; set; }
        public int routeId { get; set; }
        public bool ledgerExcludePending { get; set; } = false;
        public string StatementType { get; set; } = "";
        public int groupSales { get; set; }
        public int strId { get; set; }
        public string type { get; set; }
        public int page { get; set; } = 0;
        public int pageSize { get; set; } = 0;
        public int? agingdays { get; set; } = 0;

    }
}
