using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System;

namespace MictcoWebService.Models
{
    public class SearchPurchaseModel
    {
        public int? EntryNo { get; set; }
        public int? locationId { get; set; }
    }
}
