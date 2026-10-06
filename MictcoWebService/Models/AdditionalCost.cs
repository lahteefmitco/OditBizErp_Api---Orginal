namespace MictcoWebService.Models
{
    public class AdditionalCost
    {
        public string DrAccount { get; set; }
        public string CrAccount { get; set; }
        public decimal? PpcDrId { get; set; }
        public decimal? PpcCrId { get; set; }
        public decimal? PpcAmount { get; set; }
        public string PpcRemarks { get; set; }
    }
}
