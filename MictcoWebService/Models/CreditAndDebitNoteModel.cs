using System;

namespace MictcoWebService.Models
{
    public class CreditAndDebitNoteModel
    {
        public int EntryNo { get; set; }
        public string VoucherName { get; set; }
        public DateTime Date { get; set; }
        public int AccountId { get; set; }
        public int DrCrAccId { get; set; }
        public decimal Amount { get; set; }
        public int UserId { get; set; }
        public int LocationId { get; set; }
        public string Referense { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Sgstp { get; set; }
        public decimal Cgstp { get; set; }
        public decimal Igstp { get; set; }
        public float Sgst { get; set; }
        public float Cgst { get; set; }
        public float Igst { get; set; }
        public decimal GrandTotal { get; set; }
        //public string Remarks { get; set; }

        //public decimal SumDisc { get; set; }
        //public decimal SumTds { get; set; }


        //public int SalasmanId { get; set; }
        //public int approved { get; set; }
        //public int GstStatus { get; set; }

        //public int TransferStatus { get; set; }
        //public int SchemeStatus { get; set; }
        //public string Irn { get; set; }
        //public string QrLink { get; set; }
        //public string JsonInsert { get; set; }
        //public int CancelStatus { get; set; }
        //public int CostId { get; set; }
        //public decimal RoundOff { get; set; }
        //public AccBillwisePur[] billwisePur { get; set; }

    }
}
