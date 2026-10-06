using System;

namespace MictcoWebService.Models
{
    public class StockReportRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int LocationId { get; set; }
        public int ItemId { get; set; }
        public int SupplierId { get; set; }

        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }

        public int Group1Id { get; set; }
        public int Group2Id { get; set; }
        public int Group3Id { get; set; }

        public int BrandId { get; set; }

        public string Barcode { get; set; } = "";

        public string IntBarcode { get; set; } = "";

        public bool TaxItems { get; set; }

        public bool NonTaxItems { get; set; }

        public bool FinishedGoods { get; set; }

        public bool RawMaterials { get; set; }

        public bool ShowAll { get; set; }

        public bool MultiUnit { get; set; }
    }
}
