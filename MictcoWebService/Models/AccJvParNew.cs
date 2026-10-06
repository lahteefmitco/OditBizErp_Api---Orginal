using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class AccJvParNew
    {
        public string draccount{ get; set; }
        public string craccount { get; set; }
        public double amount { get; set; }

        public string remarks { get; set; }

        public int dr_id { get; set; }

        public int cr_id { get; set; }



    }
}
