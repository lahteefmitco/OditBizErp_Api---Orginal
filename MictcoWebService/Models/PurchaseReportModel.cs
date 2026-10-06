using System;

namespace MictcoWebService.Models
{
    public class PurchaseReportModel
    {
        public int SupplierId { get; set; }
        public string FromDate { get; set; }

        public string ToDate { get; set; }
    }
}
