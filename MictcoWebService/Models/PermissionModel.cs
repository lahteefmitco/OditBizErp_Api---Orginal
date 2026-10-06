using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class PermissionModel
    {
        public int userId { get; set; }
        public MenuPermissions[] permission{get;set;}
    }
}
