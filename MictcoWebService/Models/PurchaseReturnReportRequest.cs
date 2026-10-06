namespace MictcoWebService.Models
{
    public class PurchaseReturnReportRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public int DateType { get; set; }

        public int LocationId { get; set; }
        public int ItemId { get; set; }
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }

        public string Barcode { get; set; }
        public string IntBarcode { get; set; }

        public int SupplierId { get; set; }
        public int SalesmanId { get; set; }
        public int UserId { get; set; }

        public bool Summary { get; set; }
        public bool Detailed { get; set; }

        // All / Included / Excluded
        public string GstType { get; set; }
    }
}
