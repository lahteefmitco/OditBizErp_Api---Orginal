using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class StockReportModel
    {
        public int location { get; set; }

        public int category { get; set; }

        public int brand { get; set; }

        public string itemCode { get; set; }

        public int itemName { get; set; }

        public bool showall { get; set; }

        public string brandName { get; set; }

        public string intBarcode { get; set; }

        public string barcode { get; set; }
        public int? mfr { get; set; }
        public int? subCategory { get; set; }


    }
}
