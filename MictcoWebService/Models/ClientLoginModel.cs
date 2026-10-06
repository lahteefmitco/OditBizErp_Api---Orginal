using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class ClientLoginModel
    {
        //[Required(ErrorMessage = "User Name is required")]
        public string ClientId { get; set; }

        //[Required(ErrorMessage = "Password is required")]
        public string Secret { get; set; }

    }
}
