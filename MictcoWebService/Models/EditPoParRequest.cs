using System;

namespace MictcoWebService.Models
{
    public class EditPoParRequest
    {
        public decimal? epop_id { get; set; }

        public decimal? epop_epoi_id { get; set; }
        public decimal? epop_cust_id { get; set; }
        public int? epop_ir_id { get; set; }
        public string? epop_item_name { get; set; }

        public float? epop_requested_qty { get; set; }
        public int? epop_requested_by { get; set; }
        public DateTime? epop_requested_date { get; set; }

        public int? epop_enquiry_by { get; set; }
        public DateTime? epop_enquiry_date { get; set; }
        public float? epop_enquiry_qty { get; set; }

        public float? epop_order_qty { get; set; }
        public int? epop_order_by { get; set; }
        public DateTime? epop_order_date { get; set; }
        public decimal? epop_order_price { get; set; }

        public float? epop_received_qty { get; set; }
        public int? epop_received_by { get; set; }
        public DateTime? epop_received_date { get; set; }

        public float? epop_damaged_qty { get; set; }
        public decimal? epop_purchase_price { get; set; }
        public int? epop_supplier_id { get; set; }

        public string? epop_tag { get; set; }
        public string? epop_status { get; set; }
        public string? epop_note { get; set; }
        //public string? epop_attachment { get; set; }
        public bool? remove_attachment { get; set; }
    }
}
