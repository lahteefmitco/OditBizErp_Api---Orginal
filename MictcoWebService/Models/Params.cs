using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class Params
    { 
        public string Value { get; set; }
        public string date { get; set; }
        public int searchLength { get; set; } = 0;
        public bool advanceSearch { get; set; } = false;
    }
}
