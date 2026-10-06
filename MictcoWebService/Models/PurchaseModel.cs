using System;
using System.Data;

namespace MictcoWebService.Models
{
    public class PurchaseModel
    {
        public int PiEntryNo { get; set; }//
        public string PiDate { get; set; }//
        public string PiInvDate { get; set; }//
        public int? PiSupId { get; set; }//
        public string PiSupInvNo { get; set; }//
        //public string PiType { get; set; }
        public decimal? PiGrossValue { get; set; }//
        public decimal? PiDisc { get; set; }//
        public decimal? PiNet { get; set; }//
        public decimal? PiTax { get; set; }//
        public decimal? PiTotal { get; set; }//
        public decimal? PiOtherCharge { get; set; }//
        public float? PiOtherDiscPer { get; set; }//
        public decimal? PiOtherDisc { get; set; }//
        public decimal? PiGrandTotal { get; set; }//
        public string PiTaxType { get; set; }//
        public int PiPurAccId { get; set; }
        public string PiNarration { get; set; }
        public decimal? PiRoundoff { get; set; }
        public decimal? PiCashPaid { get; set; }
        public decimal? PiBalance { get; set; }
        public int PiUserId { get; set; }
        //public int? PiTransferStatus { get; set; }
        public int? PiPono { get; set; }
        public int? PiLocationId { get; set; }
        //public string PiLocEntryNo { get; set; }
        //public int? PiLocEntryNoOnly { get; set; }
        //public string PiTaxInvNo { get; set; }
        //public long? PiTaxInvNoOnly { get; set; }
        public string PiMrnBookNo { get; set; }
        public int? PiApprove { get; set; }
        public string? PiApproveDate { get; set; }
        public int? PiNextEntryNo { get; set; }
        public int? PiUnregistered { get; set; }
        public float? PiFreightPer { get; set; }
        public decimal? PiFreightCharges { get; set; }
        public decimal? PiFreightTax { get; set; }
        public float? PiHandPer { get; set; }
        public decimal? PiHandCharges { get; set; }
        public decimal? PiHandTax { get; set; }
        public float? PiInsurancePer { get; set; }
        public decimal? PiInsuranceCharges { get; set; }
        public decimal? PiInsuranceTax { get; set; }
        public int? PiInterstate { get; set; }
        public decimal? PiCurrencyValue { get; set; }
        public string PiGstin { get; set; }
       // public int? PiRefAdvanceInpId { get; set; }
        public string PiLrno { get; set; }
        public string? PiLrDate { get; set; }
        public decimal? PiLendAdd { get; set; }
        public decimal? PiLendLess { get; set; }
        public decimal? PiSumLocalExp { get; set; }
        public decimal? PiSumSgst { get; set; }
        public decimal? PiSumCgst { get; set; }
        public decimal? PiSumIgst { get; set; }
        public int? PiSalesmanId { get; set; }
        public decimal? PiExpenseAmount { get; set; }
        public float? PiTcsPer { get; set; }
        public decimal? PiTcsAmount { get; set; }
        public int? PiHoId { get; set; }
        public int? PiBranchTransfer { get; set; }
        public string PiBranchName { get; set; }
        public float? PiTdsPer { get; set; }
        public decimal? PiTdsAmount { get; set; }
        public decimal? PiAdditionalCost { get; set; }
        public decimal? PiAdditionalCostAdd { get; set; }
        public decimal? PiAdditionalCostLess { get; set; }
        public int? PiPendingStatus { get; set; }
        public int? PiCounter { get; set; }
        public decimal? PiSumCess { get; set; }
        public decimal? PiSumAdCess { get; set; }
        public string PiSalesman { get; set; }
        public string PiSupName { get; set; }
        //public long? PiDupEntryNo { get; set; }
        public string PoiApprovedStatus { get; set; }
        public int? PoiApprovedDays { get; set; }
        public string PoiDestination { get; set; }
        public string PoiDestinationAdd1 { get; set; }
        public string PoiDestinationAdd2 { get; set; }
        public string PoiDestinationAdd3 { get; set; }
        public string PoiContactPerson { get; set; }
        public int? OsiCounter { get; set; }
        public string PoiPopupDate { get; set; }
        public PurchaseProducts[] items { get; set; }
        //public Racks[] rack { get; set; }
        //public AdditionalCost[] additionalCosts { get; set; }
        public int Type { get; set; }

    }
}










