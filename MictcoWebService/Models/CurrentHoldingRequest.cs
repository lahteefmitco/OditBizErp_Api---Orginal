using System.Collections.Generic;

namespace MictcoWebService.Models
{
    public class CurrentHoldingRequest
    {
        public int CustId { get; set; }
        public List<int> ItemIds { get; set; }
    }
}
