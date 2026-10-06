namespace MictcoWebService.Models
{
    public class DamageReport
    {
        public string FromDate { get; set; }

        public string ToDate { get; set; }

        public int LocationId { get; set; }

        public int ItemId { get; set; }

        public string Barcode { get; set; }

        public bool Summary { get; set; }

        public bool Detailed { get; set; }
    }
}
