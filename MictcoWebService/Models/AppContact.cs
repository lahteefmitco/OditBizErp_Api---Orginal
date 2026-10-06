using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class AppContact
    {
        public int userid { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string phone { get; set; }
        public string place { get; set; }
        public int status { get; set; }

    }
}
