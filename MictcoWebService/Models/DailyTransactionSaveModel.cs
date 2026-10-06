namespace MictcoWebService.Models
{
    public class DailyTransactionSaveModel
    {
        public decimal itemId { get; set; }
        public decimal locationId { get; set; }
        public decimal userId { get; set; }
        public float openingQty { get; set; }
    }
}
