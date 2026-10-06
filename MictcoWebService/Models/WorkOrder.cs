using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class WorkOrder
    {
        public string date { get; set; }
        public string orderId { get; set; }
        public string mob { get; set; }
        public string companyName { get; set; }
        public string modelName { get; set; }
        public int braCompanyId { get; set; }
        public string color { get; set; }
        public string value { get; set; }
        public int userId { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public int assignTo { get;set;}
        
    }
}
