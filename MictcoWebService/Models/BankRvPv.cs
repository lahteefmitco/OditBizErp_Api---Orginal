using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class BankRvPv
    {
        public string type { get; set; }
        public int bankId { get; set; }
        public string cheque_dd_no { get; set; }
        public string status { get; set; }
        public string chequeDate { get; set; }
        public double amount { get; set; } = 0;
        public double bankCharge { get; set; } = 0;
    }
}
