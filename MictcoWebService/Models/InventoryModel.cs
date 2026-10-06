using System;
namespace MictcoWebService.Models
{
    public class InventoryModel
    {
        public string uniquecode { get; set; } = "0";
        public string intBarcode { get; set; } = "0";
        public string narration { get; set; } = "0";
        public string size { get; set; } = "0";
        public int ir_id { get; set; } = 0;
        public string itemcode { get; set; } = "";
        public string StatementType { get; set; } = "";
    }
}
