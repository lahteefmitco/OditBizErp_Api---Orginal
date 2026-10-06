using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class ShopUpdateModel
    {
        public int shop_id { get; set; }

        public string shop_name { get; set; }
        public string address { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string locationName { get; set; }
        public string mobile { get; set; }
        public int active { get; set; }
        public int pinCode { get; set; }
        public decimal creditLimit { get; set; }
        public int creditDays { get; set; }
        public decimal obCr { get; set; }
        public decimal obDr { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public int tcsStatus { get; set; }
        public int tcsStatusSales { get; set; }
        public int tdsStatus { get; set; }
        public string gstin { get; set; }
        public string state { get; set; }
        public string stateCode { get; set; }

        public int groupId { get; set; }

        public int routeId { get; set; }
        public int location { get; set; }
        public int salesmanId { get; set; } = -1;
        public int as_area_id { get; set; } = -1;
    }
}
