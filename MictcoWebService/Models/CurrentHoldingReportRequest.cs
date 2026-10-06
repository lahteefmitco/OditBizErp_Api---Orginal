using System.Collections.Generic;

namespace MictcoWebService.Models
{
    public class CurrentHoldingReportRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }

        public int CustomerId { get; set; }
        public int ItemId { get; set; }

        public string StatementType { get; set; }

        public List<int> RouteIds { get; set; }
        public List<int> SalesmanIds { get; set; }
        public List<int> LocationIds { get; set; }
    }
}
