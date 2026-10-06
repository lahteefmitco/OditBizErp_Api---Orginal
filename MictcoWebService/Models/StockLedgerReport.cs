namespace MictcoWebService.Models
{
    public class StockLedgerReport
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }

        public int LocationId { get; set; }

        public int ItemId { get; set; }

        public int SupplierId { get; set; }

        public int CategoryId { get; set; }

        public int SubCategoryId { get; set; }

        public int Group1Id { get; set; }

        public int Group2Id { get; set; }

        public int Group3Id { get; set; }

        public bool StockLocation { get; set; }

        public bool Detailed { get; set; }

        public bool InOut { get; set; }

        public bool ShowAll { get; set; } = true;
    }
}
