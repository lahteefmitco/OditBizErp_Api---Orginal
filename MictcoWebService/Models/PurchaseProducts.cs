using System;

namespace MictcoWebService.Models
{
    public class PurchaseProducts
    {
        public int IrId { get; set; }
        public float Qty { get; set; }
        public decimal PRate { get; set; }
        public decimal Gross { get; set; }
        public float DiscP { get; set; }
        public decimal Disc { get; set; }
        public decimal Net { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public float ProfitP { get; set; }
        public decimal MRP { get; set; }
        public float RetailPer { get; set; }
        public decimal Retail { get; set; }
        public float WholesalePer { get; set; }
        public decimal WholeSale { get; set; }
        public float SpRetailPer { get; set; }
        public decimal SpRetail { get; set; }
        public float BranchPer { get; set; }
        public decimal Branch { get; set; }
        public decimal RealPRate { get; set; }
        public int Location { get; set; }
        public string? ExpDate { get; set; }
        public int Sticker { get; set; }
        public string Color { get; set; } = "";
        public string Size { get; set; }
        public string Brand { get; set; }
        public string IntBarcode { get; set; }
        public decimal UniqueCode { get; set; }
        public float QtyMultiUnit { get; set; }
        public int UnitMulti { get; set; }
        public decimal PRateMultiUnit { get; set; }
        public decimal Cost { get; set; }
        public string Narration { get; set; }
        public decimal Cess { get; set; }
        public decimal AdCess { get; set; }
        public float TaxPer { get; set; }
        public string HSN { get; set; }
        public decimal LocalExp { get; set; }
        public decimal BulkRate { get; set; }
        public decimal LendAmount { get; set; }
        public decimal SGST { get; set; }
        public decimal CGST { get; set; }
        public decimal IGST { get; set; }
        public float SGSTP { get; set; }
        public float CGSTP { get; set; }
        public float IGSTP { get; set; }
        public decimal R1 { get; set; }
        public decimal R2 { get; set; }
        public decimal R3 { get; set; }
        public decimal R4 { get; set; }
        public string ExpDate1 { get; set; }
        public int SlNo { get; set; }
        public string ItemNarration { get; set; }
        public float FQty { get; set; }
    }
}
