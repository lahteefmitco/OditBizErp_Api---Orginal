using System;
namespace MictcoWebService.Models
{
    public class SalesReporGsrtR2Model
    {
        public int strid { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public string StatementType { get; set; } = "";
    }
}
