namespace MictcoWebService.Models
{
    public class OpeningStockReport
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }

        // 1 = By Entry Date
        // 2 = By Invoice Date
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

        // GST Reports
        // GST / NON GST / empty = All
        public int GstType { get; set; } = 1;

        public bool Summary { get; set; } = true;
        public bool Detailed { get; set; } = false;

        public bool ShowAll { get; set; } = true;
    }
}
