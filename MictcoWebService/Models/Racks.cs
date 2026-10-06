namespace MictcoWebService.Models
{
    public class Racks
    {
       public decimal? RckpIrId { get; set; }          // [numeric](6, 0) -> decimal?
        public decimal? RckpRackId { get; set; }        // [numeric](6, 0) -> decimal?
        public double? RckpQtyIn { get; set; }          // [float] -> double?
        public double? RckpQtyOut { get; set; }         // [float] -> double?
        public decimal? RckpLocId { get; set; }         // [numeric](6, 0) -> decimal?
        public string RckpRemarks { get; set; }
    }
}
