using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class AccBillwisePur
    {
		public long voucherEntryno { get; set; }
		public string billDate { get; set; }
		public string voucherType { get; set; }
		public double billAmount { get; set; }
		public double billBalance { get; set; }
		public double amount { get; set; }
		public string chequeNo { get; set; }
		public double discount { get; set; }
		public double tds { get; set; }

		public string locEntryno { get; set; }

		public string supInvno { get; set; }

		public bool chkTick { get; set; }
	}
}
