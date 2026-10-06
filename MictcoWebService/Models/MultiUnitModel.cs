namespace MictcoWebService.Models
{
    public class MultiUnitModel
    {
        //public decimal ImId { get; set; } 
        public decimal? ImIrId { get; set; } 
        public decimal? ImUnitId { get; set; }
        public float? ImConversion { get; set; } 
        public decimal? ImMinUnitId { get; set; } 
        public string ImIntBarcode { get; set; }
        public decimal? ImRate { get; set; }
        public decimal? ImLoadingCharge { get; set; }
        public decimal? ImRetail { get; set; }
        public decimal? ImWSale { get; set; }
        public decimal? ImSpRetail { get; set; }
        public decimal? ImBranch { get; set; }
        public int? ImGatePass { get; set; }
    }
}
