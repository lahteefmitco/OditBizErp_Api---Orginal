using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace MictcoWebService.Models
{
    public class EcartModel
    {
        public class BannerModelEcart
        {
            public int ib_id { get; set; }
            public string ib_type { get; set; }
        }
        public class ImageModelEcart
        {
            public int imageId { get; set; }
            public string referenceType { get; set; }
            public int referenceId { get; set; }
            public string imageData { get; set; }
        }
        public class ImageUploadRequestEcart
        {
            public int? ImageId { get; set; }
            public string ReferenceType { get; set; }
            public int? ReferenceId { get; set; }
        }



        //public class BannerImageItem
        //{
        //    public string ImagePath { get; set; }     // ex: /images/banner1.jpg
        //    public string RedirectUrl { get; set; }   // optional
        //}
        //public class ImageGroupRequest
        //{
        //    public int ReferenceId { get; set; }
        //    public List<ImageItemRequest> Images { get; set; }
        //}

        //public class ImageItemRequest
        //{
        //    public string Image { get; set; }
        //    public string RedirectUrl { get; set; }
        //}

        public class BulkImageFormRequestEcart
        {
            public int ReferenceId { get; set; }
            public string? RedirectUrl { get; set; }
            public IFormFile Image { get; set; }
        }
        public class SectionMasterModelEcart
        {
            public int sm_id { get; set; }
            public string sm_name { get; set; }
            public int sm_order { get; set; }
        }

        public class SectionItemModelEcart
        {
            public int si_id { get; set; }
            public int si_section_id { get; set; }
            public int si_item_id { get; set; }
        }
        public class WishlistModelEcart
        {
            public int iw_id { get; set; }
            public int iw_user_id { get; set; }
            public int iw_product_id { get; set; }
        }
        public class WishlistToggleModelEcart
        {
            public int productId { get; set; }
            public int isWishlist { get; set; }
        }
        public class CartToggleModelEcart
        {
            public int customerId { get; set; }
            public int salesmanId { get; set; }
            public int itemId { get; set; }
            public double qty { get; set; }
            public decimal price { get; set; }
            public string remarks { get; set; }
            public string attachment { get; set; }
        }
        public class CartToggleModel1Ecart
        {
            public int? customerId { get; set; }
            public int salesmanId { get; set; }
            public int itemId { get; set; }
            public double qty { get; set; }
            public decimal price { get; set; }
            public string remarks { get; set; }
            //public string attachment { get; set; }
        }
        public class PoCartToggleModelEcart
        {
            public int? customerId { get; set; }
            public int? itemId { get; set; }
            public string? itemName { get; set; }
            public double qty { get; set; }
            public string remarks { get; set; }
            //public string attachment { get; set; }
        }

        public class CheckoutRequestEcart
        {
            public int customerId { get; set; }
            public int salesmanId { get; set; }
            public DateTime date { get; set; }
            public decimal total { get; set; }
            public string status { get; set; }
            public int addressId { get; set; }
            public List<CartItem> cartItems { get; set; }
        }
        public class CheckoutRequest1Ecart
        {
            public int customerId { get; set; }
            public int salesmanId { get; set; }
            public DateTime date { get; set; }
            public decimal total { get; set; }
            public string status { get; set; }
            public List<int> cartIds { get; set; }   // 👈 changed to list
        }

        public class CartItemEcart
        {
            public int itemId { get; set; } // sp_ir_id
            public decimal qty { get; set; } // sp_qty
            public decimal price { get; set; } // sp_rate
            public string status { get; set; }
            public string remarks { get; set; }
            public string attachment { get; set; }

        }
        public class CheckoutResponseEcart
        {
            public int orderId { get; set; } // <-- add this
            public int orderNo { get; set; } // <-- add this
            public int customerId { get; set; }
            public string customerName { get; set; } // ✅ add this
            public int salesmanId { get; set; }
            public string salesmanName { get; set; } // ✅ add this
            public DateTime date { get; set; }
            public decimal total { get; set; }
            public string status { get; set; }
            //public int entryNo { get; set; } // <-- add this
            public List<CartItemresponseEcart> cartItems { get; set; }
        }
        public class CheckoutResponseListEcart
        {
            public int orderId { get; set; } // <-- add this
            public int orderNo { get; set; } // <-- add this
            public int customerId { get; set; }
            public string customerName { get; set; } // ✅ add this
            public int salesmanId { get; set; }
            public string salesmanName { get; set; } // ✅ add this
            public DateTime date { get; set; }
            public decimal total { get; set; }
            public string status { get; set; }
            public List<CartItemresponseEcart> cartItems { get; set; }
        }
        public class OrderStatusResponseEcart
        {
            public string status { get; set; }
            public DateTime date { get; set; }
        }
        public class CheckoutResponseByOrderIdEcart
        {
            public int orderNo { get; set; }
            public decimal totalAmount { get; set; }
            public int customerId { get; set; }
            public string customerName { get; set; }
            public int? salesmanId { get; set; }
            public string salesmanName { get; set; }
            public int? routeId { get; set; }
            public string routeName { get; set; }
            public int? areaId { get; set; }
            public string areaName { get; set; }
            public DateTime date { get; set; }
            public decimal totalQty { get; set; }
            public string status { get; set; }
            public bool isReturn { get; set; }
            public EcartAddressModelEcart address { get; set; }

            public List<OrderStatusResponseEcart> statusList { get; set; } = new();
            // List of cart items (initialized in code)
            public List<CartItemresponseByOrderIdEcart> cartItems { get; set; }
        }

        public class CartItemresponseEcart
        {
            public int itemId { get; set; } // sp_ir_id
            public string itemName { get; set; } // sp_ir_id
            public decimal qty { get; set; } // sp_qty
            public decimal price { get; set; } // sp_rate
            public string status { get; set; }
            public string remarks { get; set; }
            public string attachment { get; set; }


        }
        public class CartItemresponseByOrderIdEcart
        {
            public int itemId { get; set; } // sp_ir_id
            public string itemName { get; set; } // sp_ir_id
            public decimal qty { get; set; } // sp_qty
            public decimal price { get; set; } // sp_rate
            public string status { get; set; }
            public string remarks { get; set; }
            public string attachment { get; set; }
            public List<string> images { get; set; } = new();

        }
        public class CreatePoFromCartRequestEcart
        {
            public List<int> CartIds { get; set; }
        }
        public class OrderDashboardRequestEcart
        {
            public DateTime? FromDate { get; set; }   // optional
            public DateTime? ToDate { get; set; }     // optional
            public string? Status { get; set; }     // optional

        }
        public class UpdateOrderItemStatusRequestEcart
        {
            public string Status { get; set; } = "";
            public List<long> OrderParIds { get; set; } = new();
        }
        public class ReturnRequestEcart
        {
            public int customerId { get; set; }
            public int salesmanId { get; set; }
            public DateTime date { get; set; }
            public decimal total { get; set; }
            public string status { get; set; }
            public string description { get; set; }

            public List<ReturnItemEcart> ReturnItems { get; set; }
        }


        public class ReturnItemEcart
        {
            public int itemId { get; set; } // sp_ir_id
            public decimal qty { get; set; } // sp_qty
            public decimal price { get; set; } // sp_rate
            public string status { get; set; }
            public string remarks { get; set; }
            public string attachment { get; set; }

        }
        public class SalesReturnRequestDtoEcart
        {
            public int OrderId { get; set; }        // si_id
            public string Reason { get; set; }
            public string Attachment { get; set; }  // image/video path
            public List<SalesReturnItemDto> Items { get; set; }
        }

        public class SalesReturnItemDtoEcart
        {
            public int ItemId { get; set; }          // sp_ir_id
            public decimal ReturnQty { get; set; }
        }
        public class EcartCustomerModelEcart
        {
            public string MobileNo { get; set; }          // sp_ir_id
            public string Name { get; set; }
            public string PinCode { get; set; }
            public string Add1 { get; set; }
            public string Add2 { get; set; }
            public string Add3 { get; set; }
        }
        public class EcartAddressModelEcart
        {
            public string FullName { get; set; }
            public string MobileNo { get; set; }

            public string HouseName { get; set; }
            public string Area { get; set; }
            public string PinCode { get; set; }

            public string AddressType { get; set; } // Home / Work
            public bool IsDefault { get; set; }
        }
        public class EcartIssueReportModel
        {
            public int? OrderId { get; set; }
            public List<string>? Category { get; set; }
            public string Description { get; set; }
        }
        public class EcartFeedbackModel
        {
            public int Rating { get; set; }
            public List<string>? LikedOptions { get; set; }
            public string? Comment { get; set; }
        }
    }
}
