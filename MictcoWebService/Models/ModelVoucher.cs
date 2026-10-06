using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class ModelVoucher
    {
        public double amount { get; set; }
        public double discount { get; set; }
        public string remark { get; set; }

        public int partyId { get; set; }

        public string date { get; set; }

        public int cashacId { get; set; }

        public int location { get; set; }

        public long entryNo { get; set; }
        public double ob { get; set; }
        public string voucherType { get; set; }
    }
}
