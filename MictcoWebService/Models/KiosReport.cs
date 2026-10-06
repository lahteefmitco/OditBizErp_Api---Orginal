using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class KiosReport
    {
        public bool isBarcode { get; set; }
        public bool isItemCode { get; set; }
        public string value { get; set; }
        public int customerId { get; set; }
    }
}
