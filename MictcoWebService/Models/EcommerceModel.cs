using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace MictcoWebService.Models
{
    public class BannerModel
    {
        public int ib_id { get; set; }
        public string ib_type { get; set; }
    }
    public class ImageModel
    {
        public int imageId { get; set; }
        public string referenceType { get; set; }
        public int referenceId { get; set; }
        public string imageData { get; set; }  
    }
    public class ImageUploadRequest
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

    public class BulkImageFormRequest
    {
        public int ReferenceId { get; set; }
        public string? RedirectUrl { get; set; }
        public IFormFile Image { get; set; }
    }
    public class SectionMasterModel
    {
        public int sm_id { get; set; }
        public string sm_name { get; set; }
        public int sm_order { get; set; }
    }

    public class SectionItemModel
    {
        public int si_id { get; set; }
        public int si_section_id { get; set; }
        public int si_item_id { get; set; }
    }
    public class WishlistModel
    {
        public int iw_id { get; set; }
        public int iw_user_id { get; set; }
        public int iw_product_id { get; set; }
    }
    public class WishlistToggleModel
    {
        public int productId { get; set; }    
        public int isWishlist { get; set; }    
    }
    public class CartToggleModel
    {
        public int customerId { get; set; }
        public int salesmanId { get; set; }
        public int itemId { get; set; }
        public double qty { get; set; }
        public decimal price { get; set; }
        public string remarks { get; set; }
        public string attachment { get; set; }
    }
    public class CartToggleModel1
    {
        public int? customerId { get; set; }
        public int salesmanId { get; set; }
        public int itemId { get; set; }
        public double qty { get; set; }
        public decimal price { get; set; }
        public string remarks { get; set; }
        //public string attachment { get; set; }
    }
    public class PoCartToggleModel
    {
        public int? customerId { get; set; }
        public int? itemId { get; set; }
        public string? itemName { get; set; }
        public double qty { get; set; }
        public string remarks { get; set; }
        //public string attachment { get; set; }
        public string tag { get; set; }
    }

    public class CheckoutRequest
    {
        public int customerId { get; set; }
        public int salesmanId { get; set; }
        public DateTime date { get; set; }
        public decimal total { get; set; }
        public string status { get; set; }
        public List<CartItem> cartItems { get; set; }
    }
    public class CheckoutRequest1
    {
        public int customerId { get; set; }
        public int salesmanId { get; set; }
        public DateTime date { get; set; }
        public decimal total { get; set; }
        public string status { get; set; }
        public List<int> cartIds { get; set; }   // 👈 changed to list
    }

    public class CartItem
    {
        public int itemId { get; set; } // sp_ir_id
        public decimal qty { get; set; } // sp_qty
        public decimal price { get; set; } // sp_rate
        public string status { get; set; }
        public string remarks { get; set; }
        public string attachment { get; set; }

    }
    public class CheckoutResponse
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
        public List<CartItemresponse> cartItems { get; set; }
    }
    public class CheckoutResponseList
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
        public List<CartItemresponse> cartItems { get; set; }
    }
    public class OrderStatusResponse
    {
        public string status { get; set; }
        public DateTime date { get; set; }
    }
    public class CheckoutResponseByOrderId
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
        public List<OrderStatusResponse> statusList { get; set; } = new();
        // List of cart items (initialized in code)
        public List<CartItemresponseByOrderId> cartItems { get; set; }
    }

    public class CartItemresponse
    {
        public int itemId { get; set; } // sp_ir_id
        public string itemName { get; set; } // sp_ir_id
        public decimal qty { get; set; } // sp_qty
        public decimal price { get; set; } // sp_rate
        public string status { get; set; }
        public string remarks { get; set; }
        public string attachment { get; set; }


    }
    public class CartItemresponseByOrderId
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
    public class CreatePoFromCartRequest
    {
        public List<int> CartIds { get; set; }
    }
    public class UpdatePoParRequest
    {
        public decimal epop_id { get; set; } 

        public float? order_qty { get; set; }
        public decimal? order_price { get; set; }

        public float? received_qty { get; set; }

        public float? damaged_qty { get; set; }

        public decimal? purchase_price { get; set; }
        public int? supplier_id { get; set; }

        public string? note { get; set; }
        public int? userId { get; set; }

        public string? type { get; set; }

    }
    public class OrderDashboardRequest
    {
        public DateTime? FromDate { get; set; }   // optional
        public DateTime? ToDate { get; set; }     // optional
        public string? Status { get; set; }     // optional

    }
    public class UpdateOrderItemStatusRequest
    {
        public string Status { get; set; } = "";
        public List<long> OrderParIds { get; set; } = new();
    }
    public class ReturnRequest
    {
        public int customerId { get; set; }
        public int salesmanId { get; set; }
        public DateTime date { get; set; }
        public decimal total { get; set; }
        public string status { get; set; }
        public string description { get; set; }

        public List<ReturnItem> ReturnItems { get; set; }
    }


    public class ReturnItem
    {
        public int itemId { get; set; } // sp_ir_id
        public decimal qty { get; set; } // sp_qty
        public decimal price { get; set; } // sp_rate
        public string status { get; set; }
        public string remarks { get; set; }
        public string attachment { get; set; }

    }
    public class SalesReturnRequestDto
    {
        public int OrderId { get; set; }        // si_id
        public string Reason { get; set; }
        public string Attachment { get; set; }  // image/video path
        public List<SalesReturnItemDto> Items { get; set; }
    }

    public class SalesReturnItemDto
    {
        public int ItemId { get; set; }          // sp_ir_id
        public decimal ReturnQty { get; set; }
        public decimal TotalPrice { get; set; }

    }
    public class EcartCustomerModel
    {
        public string MobileNo { get; set; }          // sp_ir_id
        public string Name { get; set; }
        public string PinCode { get; set; }
        public string Add1 { get; set; }
        public string Add2 { get; set; }
        public string Add3 { get; set; }
    }
    public class EcartAddressModel
    {
        public string FullName { get; set; }
        public string MobileNo { get; set; }

        public string HouseName { get; set; }
        public string Area { get; set; }
        public string PinCode { get; set; }

        public string AddressType { get; set; } // Home / Work
        public bool IsDefault { get; set; }
    }
}
