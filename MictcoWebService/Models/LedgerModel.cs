using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class LedgerModel
    {
        public int asId { get; set; }

        public int ledgerType { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
    }
}
