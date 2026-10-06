using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MictcoWebService.Models
{
    public class ReportModelContext
    {
        public static string searializeDt(Object dt)
        {
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(dt);
            return JSONString; 
        }
    }
}
