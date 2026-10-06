using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class StartRouteRide
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string date { get; set; }
        public string time { get; set; }
        public string meterReading { get; set; }
        //public IFormFile meterPhoto { get; set; }
        public int routeId { get; set; }

        public int rowId { get; set; }

        public string status { get; set; }

        public string remark { get; set; }

    }
}
