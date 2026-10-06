namespace MictcoWebService.Models
{
    public class PurchaseReportSales
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }

  
        public int DateType { get; set; } = 1;

        public int LocationId { get; set; }
        public int ItemId { get; set; }
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }

        public string Barcode { get; set; }
        public string IntBarcode { get; set; }

        public int SupplierId { get; set; }
        public int SalesmanId { get; set; }
        public int UserId { get; set; }

        public bool Summary { get; set; } = true;
        public bool Detailed { get; set; } = true;

        public bool TaxItem { get; set; }
        public bool NonTaxItem { get; set; }

        public bool ShowAll { get; set; } = true;

        // GST report selection
        //public string GstReport { get; set; }
        public int GstType { get; set; }
    }
}
