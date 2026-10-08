using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace MictcoWebService.Models
{
    public class ServiceComplaintModel
    {
        public int scr_id { get; set; }
        public string scr_customer_name { get; set; }
        public string scr_mobile_no { get; set; }
        public string scr_email { get; set; }
        public string scr_contact_no { get; set; }
        public int scr_category_id { get; set; }
        public string scr_customer_address { get; set; }
        public string scr_findings { get; set; }
        public bool src_isLedger { get; set; }
        public decimal src_ledger_id { get; set; }
        public int user_id { get; set; }
        public int scr_rout_id { get; set; }
        public DateTime scr_token_date { get; set; }


    }
    public class ServiceTicketModel
    {
        public string CustomerName { get; set; }          // si_cust_name

        public DateTime? Date { get; set; }                 //si_date
        public int? BrandId { get; set; }                // si_company
        public int? ColorId { get; set; }                // si_color
        public string? ModelNo { get; set; }                  // si_model
        public string? IMEI { get; set; }                  // si_imei
        public string? BatteryNo { get; set; }             // si_batteryno
        public DateTime? ExpectedDate { get; set; }        // si_expected_date

        //public DateTime DeliveryDate { get; set; }     // si_deliverydate
        public string? EstimateCost { get; set; }                 // si_remarks
        public decimal? AdvanceAmount { get; set; }         // si_cash_recieved

        public int? CashPaidAccount { get; set; }          // si_cash_paid_acc
        public decimal? CashAmount { get; set; }         // si_cash_recieved
        public int? CardAccount { get; set; }          // si_cash_paid_acc
        public decimal? CardAmount { get; set; }         // si_cash_recieved

        public int? CategoryId { get; set; }          // si_category_id
        public string? CouponNo { get; set; }                  // si_coupon_no
        public List<string>? list_itemscollected { get; set; } = new();
        public string? LendItemsJson { get; set; }
        public List<LendRowModel>? LendItems { get; set; }
        public List<IFormFile>? Images { get; set; }
    }

    //public class ServiceTicketModel
    //{
    //    public int? complaint_id { get; set; }
    //    public string? si_cust_name { get; set; }
    //    public string? Brand { get; set; }
    //    public string? si_model { get; set; }
    //    public string? si_imei { get; set; }
    //    public string? BatteryNo { get; set; }
    //    public string? si_deliverydate { get; set; }
    //    public string? si_expected_date { get; set; }
    //    public string? EstimateCost { get; set; }
    //    public int? CashAccId { get; set; }
    //    public decimal? AdvanceAmount { get; set; }
    //    public string? StatementType { get; set; }

    //    public List<string> list_itemscollected { get; set; } = new();

    //    // 🔑 JSON string from form-data
    //    public string LendItemsJson { get; set; }

    //    // Deserialized list
    //    public List<LendRowModel> LendItems { get; set; } = new();

    //    public List<IFormFile> Images { get; set; }
    //}

    public class LendRowModel
    {
        public string? li_remarks { get; set; }
        public int? li_ir_id { get; set; }
    }

    public class ServiceDeliveryModel
    {
        public int complaint_id { get; set; }
        public int ticket_id { get; set; }
        public int delivery_entryno { get; set; }

        public string si_cust_name { get; set; }
        public int si_assign_to { get; set; }
        public DateTime date { get; set; }
        public string Brand { get; set; }
        public string si_model { get; set; }
        public string si_imei { get; set; }
        public string BatteryNo { get; set; }
        public DateTime? si_deliverydate { get; set; }
        public DateTime? si_expected_date { get; set; }
        public string EstimateCost { get; set; }
        public int? CashPaidAccount { get; set; }          // si_cash_paid_acc
        public decimal? CashAmount { get; set; }         // si_cash_recieved
        public int? CardAccount { get; set; }          // si_cash_paid_acc
        public decimal? CardAmount { get; set; }         // si_cash_recieved
        public decimal AdvanceAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Total { get; set; }
        public decimal Balance { get; set; }
        public decimal OtherCharge { get; set; }
        public decimal OtherDisc { get; set; }
        public string StatementType { get; set; }
        public int si_color { get; set; }
        public string si_other_remarks { get; set; }
        public bool isTax { get; set; }
        public List<string> list_itemscollected { get; set; } = new List<string>();
        public List<ItemRowModel> Items { get; set; } = new List<ItemRowModel>();

        public List<LendRowModel> LendItems { get; set; } = new List<LendRowModel>();
    }
    public class ItemRowModel
    {
        public int? ir_id { get; set; }
        public int? qty { get; set; }
        public decimal? gross { get; set; }
        public decimal? net { get; set; }
        public decimal? total { get; set; }
        public decimal? discount { get; set; }
        public decimal? gst { get; set; }
        public decimal? saleRate { get; set; }
        public long uniquecode { get; set; }
    }
    public class QCModel
    {
        public int qc_id { get; set; }
        public string qc_name { get; set; }
        public int qc_category_id { get; set; }
        public bool qc_is_active { get; set; }
        public int created_by { get; set; }
        public int updated_by { get; set; }
    }
    public class ItemsCollectedModel
    {
        public int iic_id { get; set; }
        public string iic_name { get; set; }
        public int? iic_status { get; set; }
        public string iic_remarks { get; set; }
    }
    public class SpareRequestModel
    {
        //public int sr_id { get; set; }
        public int sr_ticket_no { get; set; }
        public int sr_item_id { get; set; }
        public decimal sr_qty { get; set; }
        public string sr_remarks { get; set; }
        public bool is_technician_fault { get; set; }
        public string fault_remark { get; set; }


        //public decimal sr_srate { get; set; }
        //public decimal sr_discount { get; set; }
        //public decimal sr_gross { get; set; }
        //public decimal sr_net { get; set; }
        //public decimal sr_gst { get; set; }
        //public decimal sr_total { get; set; }
        //public decimal sr_uniquecode { get; set; }
        //public int created_by { get; set; }
    }
    public class SpareApproveModel
    {
        //public int sr_id { get; set; }
        public int sr_ticket_no { get; set; }
        public int sr_item_id { get; set; }
        public decimal sr_qty { get; set; }
        public string sr_remarks { get; set; }
        public decimal sr_srate { get; set; }
        public decimal sr_discount { get; set; }
        public decimal sr_gross { get; set; }
        public decimal sr_net { get; set; }
        public decimal sr_gst { get; set; }
        public decimal sr_total { get; set; }
        public decimal sr_uniquecode { get; set; }

        public bool is_technician_fault { get; set; }
        public string fault_remark { get; set; }
    }
    public class TicketStatusModel
    {
        public int ticket_id { get; set; }
        public string status { get; set; }  
        public string remarks { get; set; }
        public int technician_id { get; set; }
    }
    public class GeneralRemarksModel
    {
        public int ticket_id { get; set; }
        public string generalRemarks { get; set; }
    }
    public class QCApproveModel
    {
        public int si_entryno { get; set; }
        public string qc_status { get; set; }   // Approved / Rejected
        public string qc_findings { get; set; }
        public int qc_technician_id { get; set; }
        public int user_id { get; set; }
        public List<QCChecklistItem> qc_list { get; set; }
    }
    public class QCChecklistItem
    {
        public int qc_id { get; set; }
        public bool qc_checked { get; set; }
    }
    public class BrandModel
    {
        public string name { get; set; }
        public string? remarks { get; set; }
        public int model_company_id { get; set; }
    }
    public class SearchModel
    {
        public int? entryno { get; set; }
        public string? mobilno { get; set; }
        public string? custname { get; set; }
        public string? imei { get; set; }
        public string? slno { get; set; }
        public string? route { get; set; }
        public string? brandName { get; set; }
        public string? modelName { get; set; }
        public string? complaint { get; set; }
        public string? technicianName { get; set; }
        public DateTime? fromDate { get; set; }
        public DateTime? toDate { get; set; }
        public string? si_finish { get; set; }


    }
    public class AccSubHeadModel
    {
        public int as_id { get; set; }
        public string as_name { get; set; }
        public string as_mob { get; set; }
        public string as_mail { get; set; }
        public string as_add1 { get; set; }
        public string as_add2 { get; set; }
        public int as_rout_id { get; set; }
    }
    public class BillwiseReceiptModel
    {
        public int vbri_cash_acc { get; set; }
        public decimal vbri_total { get; set; }
        public string vbri_remarks { get; set; }
        public int vbri_verified { get; set; }
        public int vbri_salesman { get; set; }
        public int vbri_location_id { get; set; }
        public int vbri_user_id { get; set; }

        // Child items list representing individual items (like the items seen in Outstanding Report result.jpg)
        public List<ReceiptDetailModel> Details { get; set; } = new List<ReceiptDetailModel>();
    }

    public class ReceiptDetailModel
    {
        public string vbrp_billno { get; set; }
        public string vbrp_form { get; set; }
        public decimal vbrp_bill_amount { get; set; }
        public decimal vbrp_amount { get; set; } // The actual collection amount paid
    }

    public class ApproveBillwiseReceiptModel
    {
        public int vbri_id { get; set; }
    }
    public class BulkApproveBillwiseReceiptModel
    {
        public List<int> vbri_ids { get; set; }
    }
    public class PerformanceReportModel
    {
        public int technician_id { get; set; }
        public int as_id { get; set; }
        public int rout_id { get; set; }
        public int route_id { get; set; }
        public int loc_id { get; set; }
        public int location_id { get; set; }
        public string status { get; set; }
        public DateTime fromDate { get; set; }
        public DateTime toDate { get; set; }
    }
    public class TechnicianLossReportModel
    {
        public int technician_id { get; set; }
        public int location_id { get; set; }

        public DateTime from_date { get; set; }
        public DateTime to_date { get; set; }
    }
    public class TicketFiltrationModel
    {
        public string status { get; set; }
        public DateTime? fromDate { get; set; }
        public DateTime? toDate { get; set; }
        public int? ticketNo { get; set; }
        public int? customerId { get; set; }
        public int? TechnicianId { get; set; }
        public int? routeId { get; set; }

    }

}
