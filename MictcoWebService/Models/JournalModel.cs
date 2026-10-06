using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class JournalModel
    {
        public string statement { get; set; }
        public long ji_entryno { get; set; }
        public string ji_date { get; set; }
        public double ji_amount { get; set; }

        public int ji_user { get; set; }

        public int ji_location_id { get; set; }

        public AccJvParNew[] jvparnew {get;set;}
        public AccBillwisePur[] billwisePur { get; set; }

        public long entryno { get; set; }

    }
}
