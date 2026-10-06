using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class SpLoyality
    {
        public string statement { get; set; }
        public Loyalty loyalty { get; set; }
    }
}
