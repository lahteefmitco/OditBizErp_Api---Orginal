using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class SaleModel
    {
        public double balance { get; set; }
        public int cashId { get; set; }
        public double cashReceived { get; set; }
        public double cgst { get; set; }

        public int customerId { get; set; }

        //public int customerName { get; set; }

        public string date { get; set; }

        public double freightCharge { get; set; }

        public double gross { get; set; }

        public double net { get; set; }

        public double profit { get; set; } 

        public string remark { get; set; }

        public int salesManId { get; set; }

        public int sistrId { get; set; }

        public double taxAmount { get; set; }

        public double total { get; set; }

        public double totalDiscount { get; set; }

        public double totalDiscountPer { get; set; }

        public SalesProducts[] items { get; set; }

        public int entryNo { get; set; }

        public double sgst { get; set; }

        public string statement { get; set; }

        public int commisionAccId { get; set; }

        public String custName { get; set; }

        public int lc_id { get; set; } = 0;
        public string oldBalance { get; set; }
        public double netBalance { get; set; }
        public string salesRateType { get; set; }
        public string aliasName { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string grandTotal { get; set; }
        public string lendAdd { get; set; }
        public double otherCharge { get; set; } = 0;
        public string si_other_works { get; set; } = ""; // chair in restuarent
        public int si_kot_status { get; set; } = 0;
        public string si_destination { get; set; } = "";
        public int si_commision_acc_id { get; set; } = 0;
        public int si_bankacc { get; set; } = 0;
        public double si_card_amount { get; set; } = 0;
        public string si_batteryno { get; set; } = "";
        public string si_sim { get; set; } = "";
        public string si_einvoice_ksa { get; set; } = "";

        public int si_company { get; set; }
        public int si_model { get; set; }
        public int si_color { get; set; }
        public int si_service_type_id { get; set; }
        public string si_imei { get; set; }
        //public string[] itemcollected { get; set; } 
        public string si_signature { get; set; }
        public string si_expected_date { get; set; }
        public int si_work_priority { get; set; }
        public int si_rc_id { get; set; }
        public string si_due_date { get; set; }
        public int si_iws_id { get; set; }
        public string si_delivered { get; set; }
        public int si_finish { get; set; }
        public int si_location_id { get; set; }
        public Complaints[] complaint { get; set; }
        public string si_finished_date { get; set; }
        public string currentDate { get; set; }
        public int assignedTo { get; set; }
        public string itemcollected { get; set; }
        public string billingName { get; set; }
        public string billingAddress { get; set; }
        public string billingLocation { get; set; }
        public string billingPincode { get; set; }
        public string billingScode { get; set; }
        public string shippingName { get; set; }
        public string shippingGstin { get; set; }
        public string shippingAddress { get; set; }
        public string shippingLocation { get; set; }
        public string shippingPincode { get; set; }
        public string shippingScode { get; set; }
        public List<HoldingItem> HoldingItems { get; set; }

    }
    public class HoldingItem
    {
        public int IrId { get; set; }         
        public decimal HoldQty { get; set; }
        public string ToDate { get; set; }
        public int LocationId { get; set; }
    }
}
