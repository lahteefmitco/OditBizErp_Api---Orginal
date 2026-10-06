using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class UserModel
    {
        public string userName { get; set; }
        public string password { get; set; }
        public string location { get; set; }
        public string macId { get; set; }
        public string area { get; set; } = "";
        public string routId { get; set; } = "";

    }
}
