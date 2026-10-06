using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class CrmEnquiry
    {
        public int id { get; set; } = 0;
        public int salesmanId { get; set; } = 0;
        public string date { get; set; } = "";
        public string time { get; set; } = "";
        public string customerName { get; set; } = "";
        public string business { get; set; } = "";
        public string place { get; set; } = "";
        public string mobile { get; set; } = "";
        public string email { get; set; } = "";
        public string remark { get; set; } = "";
        public int categoryId { get; set; } = 0;
        public int typeId { get; set; } = 0;
        public string cdate { get; set; } = "";
        public string type { get; set; } = "";
        public string nextDate { get; set; } = "";
        public int sourceId { get; set; } = 0;
        public string productCatogery { get; set; } = "";
        
        public int periodId { get; set; } = 0;
        public int status { get; set; } = 0;
        public string lastDate { get; set; } = "";
        public string fromDate { get; set; } = "";
        public string toDate { get; set; } = "";
        public string reference { get; set; } = "";
        public string search { get; set; } = "";
        public string reportType { get; set; } = "";
        public string statementType { get; set; } = "";
    }
}
