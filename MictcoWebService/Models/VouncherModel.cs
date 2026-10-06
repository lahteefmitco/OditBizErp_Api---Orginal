using System;

namespace MictcoWebService.Models
{
    public class VouncherModel
    {
        public int entryNo { get; set; }
        public string date { get; set; }
        public double sumDiscount { get; set; }
        public double sumAmount { get; set; }
        public double sumTotal { get; set; }
        public int cashacId { get; set; }
        public int location { get; set; }
        public string Statement { get; set; }
        public VouncherParModel[] items { get; set; }
    }
    public class VouncherParModel
    {
        public double amount { get; set; }
        public double discount { get; set; }
        public double total { get; set; }
        public string remark { get; set; }
        public int partyId { get; set; }
    }
}
