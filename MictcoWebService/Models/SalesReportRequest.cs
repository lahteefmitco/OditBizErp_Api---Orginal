namespace MictcoWebService.Models
{
    public class SalesReportRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }

        // Main filters
        public int FormId { get; set; }
        public int LocationId { get; set; }
        public int ItemId { get; set; }
        public int SupplierId { get; set; }
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }

        public string Barcode { get; set; }
        public string IntBarcode { get; set; }

        public int CustomerId { get; set; }
        public int SalesmanId { get; set; }
        public int UserId { get; set; }

        // Report options
        public bool Summary { get; set; }
        public bool Detailed { get; set; }

        public bool IncludeReturn { get; set; }
        public bool NonTaxItems { get; set; }
        public bool TaxItems { get; set; }

        // GST report
        public bool GstReport { get; set; }
    }
}
