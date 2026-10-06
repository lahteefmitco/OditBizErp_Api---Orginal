using System;

namespace MictcoWebService.Models
{
    public class InvoicewisePaymentAndReceiptModel
    {
        public int entryNo { get; set; }
        public string voucherName { get; set; }
        public DateTime date { get; set; }
        public int accountId { get; set; }
        public string accountName { get; set; }
        public int drCrAccId { get; set; }
        public string drCrAccName { get; set; }
        public decimal amount { get; set; }
        public decimal sumDisc { get; set; }
        public decimal sumTds { get; set; }
        public string reference { get; set; }
        public decimal netAmount { get; set; }
        public decimal sgstp { get; set; }
        public decimal cgstp { get; set; }
        public decimal igstp { get; set; }
        public float sgst { get; set; }
        public float cgst { get; set; }
        public float igst { get; set; }
        public decimal grandTotal { get; set; }
        public string remarks { get; set; }
        public int salasManId { get; set; }
        public int approved { get; set; }
        public int gstStatus { get; set; }

        public int transferStatus { get; set; }
        public int schemeStatus { get; set; }
        public string irn { get; set; }
        public string qrLink { get; set; }
        public string jsonInsert { get; set; }
        public int cancelStatus { get; set; }
        public int costId { get; set; }
        public decimal roundOff { get; set; }
        public string statement { get; set; }
        public AccBillwisePur[] billWisePur { get; set; }
    }
}
