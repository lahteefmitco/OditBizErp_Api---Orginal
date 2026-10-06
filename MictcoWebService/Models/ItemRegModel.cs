using Microsoft.AspNetCore.Http.Features;
using System;

namespace MictcoWebService.Models
{
    public class ItemRegModel
    {
        //public decimal EntryNo { get; set; } // ir_id
        public string ItemCode { get; set; } // ir_code
        public string ItemName { get; set; } // ir_name
        public string AliasName { get; set; } // ir_alias_name
        public string HsnCode { get; set; } // ir_hsn_code
        public float? TaxPer { get; set; } // ir_taxper
        public float? Cgst { get; set; } // ir_cgst
        public float? Sgst { get; set; } // ir_sgst
        public float? Igst { get; set; } // ir_igst
        public int? AllowNegative { get; set; } // ir_allow_negative
        public decimal? CategoryId { get; set; } // ir_category_id
        public decimal? SubCategoryId { get; set; } // ir_sub_category_id
        public string Brand { get; set; } // ir_brand
        public int? BatchStatus { get; set; } // ir_batch_status
        public decimal? MinUnitId { get; set; } // ir_min_unit_id
        //public decimal? BulkUnitId { get; set; } // ir_bulk_unit_id    
        public decimal? Mrp { get; set; } // ir_mrp
        public decimal? Retail { get; set; } // ir_retail
        public decimal? Wholesale { get; set; } // ir_wholesale
        public decimal? SpRetail { get; set; } // ir_spretail
        public decimal? Branch { get; set; } // ir_branch
        public byte[] ImageData { get; set; }  
        public double? MinQty { get; set; } // ir_rlevel
        //public decimal? IrMfrId { get; set; } // ir_mfr_id
        //public string IrIntBarcode { get; set; } // ir_int_barcode
        //public decimal? IrPRate { get; set; } // ir_prate
        //public decimal? IrRealPRate { get; set; } // ir_realprate
        //public decimal? IrLocationId { get; set; } // ir_location_id
        //public DateTime? IrExpDate { get; set; } // ir_exp_date
        //public string IrColor { get; set; } // ir_color
        //public string IrSize { get; set; } // ir_size
        //public int? IrLendStatus { get; set; } // ir_lend_status
        //public int? IrRawMaterial { get; set; } // ir_rawmaterial
        //public double IrMaxQty { get; set; } // ir_maxqty
        public int? Active { get; set; } // ir_active
        public float? Cess { get; set; } // ir_cess
        public float? AdCess { get; set; } // ir_ad_cess
        //public int? IrH1 { get; set; } // ir_h1
        //public int? IrControlled { get; set; } // ir_controlled
        //public int? IrNrx { get; set; } // ir_nrx
        //public int? IrBanned { get; set; } // ir_banned
        //public float? IrKfc { get; set; } // ir_kfc
        //public string IrImage { get; set; } // ir_image
        //public int? IrWeighingStatus { get; set; } // ir_weighing_status
        //public decimal? IrLoadingCharge { get; set; } // ir_loading_charge
        //public decimal? IrLendRate { get; set; } // ir_lend_rate
        //public int? IrTransferStatus { get; set; } // ir_transfer_status
        //public double IrBulkQty { get; set; } // ir_bulk_qty
        //public decimal? IrR1 { get; set; } // ir_r1
        //public decimal? IrR2 { get; set; } // ir_r2
        //public decimal? IrR3 { get; set; } // ir_r3
        //public decimal? IrR4 { get; set; } // ir_r4
        //public string IrPrinter { get; set; } // ir_printer
        public decimal? Group1 { get; set; } // ir_group1
        public decimal? Group2 { get; set; } // ir_group2
        public decimal? Group3 { get; set; } // ir_group3
        //public string IrNarration { get; set; } // ir_narration
        //public float? IrMrpPer { get; set; } // ir_mrp_per
        //public float? IrRetailPer { get; set; } // ir_retail_per
        //public float? IrWholesalePer { get; set; } // ir_wholesale_per
        //public float? IrSpRetailPer { get; set; } // ir_spretail_per
        //public float? IrBranchPer { get; set; } // ir_branch_per
        //public double IrNos { get; set; } // ir_nos
        //public int? IrServiceItem { get; set; } // ir_service_item
        //public int? IrReorderDays { get; set; } // ir_reorder_days
        //public int? IrWeighingId { get; set; } // ir_weighing_id
        //public int? IrFastPosItem { get; set; } // ir_fast_pos_item
        //public decimal? IrNetWgt { get; set; } // ir_netwgt
        //public decimal? IrSectionId { get; set; } // ir_section_id
        //public int? IrSync { get; set; } // ir_sync
        //public int? IrTaxMethod { get; set; } // ir_tax_method
        //public decimal? IrParentIrId { get; set; } // ir_parent_ir_id
        //public int? IrVarintsStatus { get; set; } // ir_varints_status
        //public int? IrNegativeProfit { get; set; } // ir_negative_profit
        public decimal? MinRate { get; set; } // ir_min_rate
        public decimal? MaxRate { get; set; } // ir_min_rate

        //public int? IrUserId { get; set; } // ir_ad_cess_amt
        //public int? IrAdCessAmt { get; set; } // ir_ad_cess_amt
        //public int? IrHsnStatus { get; set; } // ir_hsn_status
        //public string IrRackName { get; set; } // ir_rack_name
        //public int? IrOfferStatus { get; set; } // ir_offer_status
        //public float? IrReducePer { get; set; } // ir_reduce_per
        //public int? IrLendValidation { get; set; } // ir_lend_validation
        public MultiUnitModel[] items { get; set; }
        //public ItemRegBarcode[] itemregbarcode { get; set; }
        //public ItemRegExtras[] extras { get; set; }
    }
}
