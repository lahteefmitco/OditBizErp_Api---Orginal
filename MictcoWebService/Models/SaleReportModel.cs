using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class SaleReportModel
    {
        public int location { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public int customer { get; set; }
        public int salesMan { get; set; }
        public int category { get; set; }
        public int brand { get; set; }
        public int district { get; set; }
        public int area { get; set; }
        public int[] stypes { get; set; }
        public string reprtType { get; set; }
        public bool reportAccReg { get; set; }=false;
        public int routeId { get; set; } = 0;
        public int product { get; set; }
        public int subCategory{ get; set; }
        public bool dayWise { get; set; } = false;
        public int status { get; set; } = 0;
        public bool itemWise { get; set; } = false;

    }
    public class TargetQtyRequest
    {
        public decimal TargetQty { get; set; }
        public string YearMonth { get; set; }   // Format: yyyy-MM
        public int LocationId { get; set; }
    }
}
