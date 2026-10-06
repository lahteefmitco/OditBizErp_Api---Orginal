using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class SearchShop
    {
        public string routeId { get; set; }
        public string search { get; set; }
        public string latitude { get; set; }

        public string longitude { get; set; }
    }
}
