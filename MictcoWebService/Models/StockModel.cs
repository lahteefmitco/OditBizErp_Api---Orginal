using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class StockModel
    {
        public int fromId { get; set; }
        public int toId { get; set; }
        public string date { get; set; }
        public string statement { get; set; }
        public string remark { get; set; }
        public SalesProducts[] items { get; set; }


    }
}
