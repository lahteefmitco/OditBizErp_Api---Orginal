using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class BankPaymentReceipt
    {
        public double bankCharge { get; set; } = 0;
        public int customer { get; set; }
        public string date { get; set; }
        public string remark { get; set; }
        public double chequeAmount { get; set; } = 0;
        public string amount { get; set; }
        public string location { get; set; }
        public string discount { get; set; }
        public string voucherType { get; set; }
        public string statement { get; set; }
        public int entryNo { get; set; }

        public int approved {get;set;}
        public BankRvPv[] bankRvPv { get; set; }
        public AccBillwisePur[] billwisePur { get; set; }
    }
}
