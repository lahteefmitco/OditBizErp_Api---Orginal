namespace MictcoWebService.Models
{
    public class StockTransferReport
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }

        public int TransferFromId { get; set; }

        public int TransferToId { get; set; }

        public bool Detailed { get; set; }

        public bool ShowAll { get; set; } = true;
    }
}
