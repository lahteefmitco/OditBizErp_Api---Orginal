using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class ShopCheckin
    {

        public int shopId { get; set; }
        public string date { get; set; }
        public string time { get; set; }

        public string latitude { get; set; }

        public string longitude { get; set; }

        public int checkinId { get; set; }

        public int routeCheckInId { get; set; }
    }
}
