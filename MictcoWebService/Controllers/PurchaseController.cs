using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System.Data;
using System.Threading.Tasks;
using System;
using System.Security.Policy;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;
using System.Data.Common;
using Microsoft.CodeAnalysis;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class PurchaseController : Controller
    {
        private readonly string shop;

        [HttpPost("save-purchase")]
        public async Task<IActionResult> SavePurchase([FromBody] PurchaseModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string Message = "";
            try
            {
                if (usqlre.FinancialDateCheck(DateTime.Parse(model.PiDate)) == false)
                {
                    return Ok(new { status = false, entryNo = 0, pi_loc_entryno = "0" });
                }
                int entryNo = 0;
                switch (model.Type)
                {
                    case 1: // Purchase Entry
                        entryNo = await Task.Run(() => usqlre.savePurchase("P_Insert", model));
                        break;
                    case 2: // Purchase Order
                        entryNo = await Task.Run(() => usqlre.savePurchase("PO_Insert", model));
                        break;
                    case 3: // Purchase Return
                        entryNo = await Task.Run(() => usqlre.savePurchase("PR_Insert", model));
                        break;
                    default:
                        Message = "Invalid purchase type";
                        return BadRequest(new { status = false, msg = Message });
                }
                return Ok(new { status = entryNo > 0, entryNo = entryNo, msg = entryNo > 0 ? "Saved Successfully" : "Error in saving data" });

            }
            catch (Exception ex)
            {
                Message = "Error in processing request: " + ex.Message;
            }

            return BadRequest(new { status = false, msg = Message });
        }
        [HttpPost("update-purchase")]
        public async Task<IActionResult> UpdatePurchase([FromBody] PurchaseModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string Message = "";
            try
            {
                if (usqlre.FinancialDateCheck(DateTime.Parse(model.PiDate)) == false)
                {
                    return Ok(new { status = false, entryNo = 0, pi_loc_entryno = "0" });
                }
                string sql1 = "SELECT COUNT(1) FROM inv_purchase_inf WHERE pi_entryno = " + model.PiEntryNo + "";
                DataTable entryno = usqlre.dbReaderFill(sql1);
                if ( entryno.Rows.Count > 0 && Convert.ToInt32(entryno.Rows[0][0].ToString()) == 0)
                {
                    return Ok(new { status = false, msg = "Purchase does not exists" });
                }
                int entryNo = 0;
                switch (model.Type)
                {
                    case 1: // Purchase Entry
                        entryNo = await Task.Run(() => usqlre.savePurchase("P_Update", model));
                        break;
                    case 2: // Purchase Order
                        entryNo = await Task.Run(() => usqlre.savePurchase("PO_Update", model));
                        break;
                    case 3: // Purchase Return
                        entryNo = await Task.Run(() => usqlre.savePurchase("PR_Update", model));
                        break;
                    default:
                        Message = "Invalid purchase type";
                        return BadRequest(new { status = false, msg = Message });
                }
                return Ok(new { status = entryNo > 0, entryNo = model.PiEntryNo, msg = entryNo > 0 ? "Updated Successfully" : "Error in editing data" });

            }
            catch (Exception ex)
            {
                Message = "Error in processing request: " + ex.Message;
            }

            return BadRequest(new { status = false, msg = Message });
        }
        [HttpPost("search-purchase")]
        public async Task<IActionResult> searchPurchase([FromBody] SearchPurchaseModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            var hash = new Dictionary<string, object>();

            if (model.EntryNo == 0)
            {
                string sql = "SELECT MAX(pi_entryno) AS max_entry_no,MIN(pi_entryno) AS min_entry_no," +
                             "LTRIM(RTRIM(dbo.generate_entry_no(COALESCE((SELECT TOP 1 pi_loc_entryno_only " +
                             "FROM inv_purchase_inf WHERE pi_location_id = "+model.locationId+" " +
                             "ORDER BY pi_entryno DESC), 0) + " + model.locationId + ", 1, 1))) AS location_entryno " +
                             "FROM inv_purchase_inf;";
                DataTable entrno = usqlre.dbReaderFill(sql);
                hash.Add("EntryNo", entrno);

                string sql1 = $"EXEC Sp_acc_reg @StatementType = 'sup', @as_location_id = "+model.locationId+";";
                DataTable dt = usqlre.dbReaderFill(sql1);
                DataTable suppliers = new DataTable();
                suppliers.Columns.Add("value", typeof(int)); 
                suppliers.Columns.Add("label", typeof(string));
                suppliers.Columns.Add("gst", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    DataRow newRow = suppliers.NewRow();
                    newRow["value"] = row["as_id"];
                    newRow["label"] = row["as_name"];
                    newRow["gst"] = row["as_tin"];
                    suppliers.Rows.Add(newRow);
                }

                hash.Add("Suppliers", suppliers);

                string sql2 = "select as_id, as_name from acc_subhead inner join acc_parent on ap_id = as_ap_id " +
                              "where ap_name = 'EMPLOYEES' and as_active = 1";
                DataTable salesman = usqlre.dbReaderFill(sql2);
                hash.Add("Salesman", salesman);

                string sql3 = "SELECT ir_name as label, ir_id as value from inv_item_reg left join inv_unit " +
                              "on ir_min_unit_id = u_id where ir_active = 1";
                DataTable product = usqlre.dbReaderFill(sql3);
                hash.Add("Products", product);
            }
            else
            {
                string sql = "SELECT MAX(pi_entryno) AS max_entry_no, MIN(pi_entryno) AS min_entry_no, " +
                             "LTRIM(RTRIM(dbo.generate_entry_no(COALESCE((SELECT TOP 1 pi_loc_entryno_only " +
                             "FROM inv_purchase_inf WHERE pi_location_id = " + usqlre.locationId + " " +
                             "ORDER BY pi_entryno DESC), 0) + " + usqlre.locationId + ", 1, 1))) AS location_entryno " +
                             "FROM inv_purchase_inf;";
                DataTable entrno = usqlre.dbReaderFill(sql);
                hash.Add("EntryNo", entrno);

                string sql1 = $"EXEC Sp_acc_reg @StatementType = 'sup', @as_location_id = " + model.locationId + ";";
                DataTable dt = usqlre.dbReaderFill(sql1);
                DataTable suppliers = new DataTable();
                suppliers.Columns.Add("value", typeof(int));
                suppliers.Columns.Add("label", typeof(string));
                suppliers.Columns.Add("gst", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    DataRow newRow = suppliers.NewRow();
                    newRow["value"] = row["as_id"];
                    newRow["label"] = row["as_name"];
                    newRow["gst"] = row["as_tin"];
                    suppliers.Rows.Add(newRow);
                }

                hash.Add("Suppliers", suppliers);

                string sql2 = "select as_id, as_name from acc_subhead inner join acc_parent on ap_id = as_ap_id " +
                              "where ap_name = 'EMPLOYEES' and as_active = 1";
                DataTable salesman = usqlre.dbReaderFill(sql2);
                hash.Add("Salesman", salesman);

                string sql3 = "SELECT ir_name as label, ir_id as value from inv_item_reg left join inv_unit " +
                              "on ir_min_unit_id = u_id where ir_active = 1";
                DataTable product = usqlre.dbReaderFill(sql3);
                hash.Add("Products", product);
                string sql4 = @$"EXEC [dbo].[Sp_Purchase] 
                        @StatementType = 'P_Search',
                        @pi_location_id = {model.locationId}, 
                        @pi_entryno = {model.EntryNo}";
                DataSet ds = usqlre.dbreadDataset(sql4);

                var productDetails = new Dictionary<string, DataTable>
                {
                    { "Main", ds.Tables[0] },
                    { "Product", ds.Tables[1]}
                };

                hash.Add("ProductDetails", productDetails);
            }

            usqlre.close();
            string jsonResult = ReportModelContext.searializeDt(hash);
            return Content(jsonResult, "application/json");
        }

        [HttpGet("last-purchases")]
        public IActionResult lastPurchases(int locationId)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            var hash = new Dictionary<string, DataTable>();
            string sql = "SELECT  top 10 pi_entryno,pi_date,pi_sup_name,pi_sup_id,as_tin as SupplierGst,pi_grand_total,pi_roundoff FROM inv_purchase_inf left JOIN acc_subhead ON inv_purchase_inf.pi_sup_id = acc_subhead.as_id order by pi_entryno desc";
            DataTable main = usqlre.dbReaderFill(sql);
            hash.Add("Main", main);
            usqlre.close();
            string jsonResult = ReportModelContext.searializeDt(hash);
            return Content(jsonResult, "application/json");
        }
        [HttpPost("purchase-report")]
        public IActionResult purchaseReport([FromBody] PurchaseReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            var hash = new Dictionary<string, DataTable>();
            string sql = "select pi_entryno as EntryNo, pi_date as [Date], as_name, pi_type, pi_gross_value as Gross, " +
                "pi_disc as Disc, pi_net as Net, pi_tax as Gst, pi_total as Total, " +
                "pi_other_charge as OtherCharge, pi_other_disc as OtherDiscount, " +
                "pi_grand_total as GrandTotal, pi_tax_type as Type, pi_narration as Remarks, " +
                "pi_roundoff as RoundOff " +
                "from inv_purchase_inf " +
                "inner join acc_subhead on pi_sup_id = as_id " +
                "where pi_sup_id = " + model.SupplierId;

            if (!string.IsNullOrEmpty(model.FromDate))
            {
                sql += " AND pi_date >= '" + model.FromDate + "'";
            }

            if (!string.IsNullOrEmpty(model.ToDate))
            {
                sql += " AND pi_date <= '" + model.ToDate + "'";
            }
            DataTable summery = usqlre.dbReaderFill(sql);
            hash.Add("Customsummery", summery);
            string sql1 = "select pp_entryno as EntryNo,pi_date as PDate, sup.as_name as Supplier,ir_name as Itemname,c_name as Category, pp_prate as PRate, pp_qty as Qty, pp_gross_value as Gross, pp_disc as Disc, pp_net_amount as Net, pp_tax as Gst, ir_cess as Cess,ir_ad_cess as AdCess,pp_total as Total, pp_mrp as Mrp, pp_retail as Retail, pp_wholesale as WSale, pp_spretail as Spretail, pp_branch as Branch, pp_realprate as RPrate, pp_exp_date as [Exp], pp_color as Color, pp_size as Size, pp_brand as Brand, pp_int_barcode as IntBarcode, pp_uniquecode as Barcode from inv_parchase_par inner join inv_item_reg on pp_ir_id=ir_id inner join inv_purchase_inf on pi_entryno=pp_entryno inner join acc_subhead as sup on pi_sup_id=sup.as_id left join inv_mfr on m_id=ir_mfr_id left join inv_category on c_id=ir_category_id left join inv_unit on u_id=ir_min_unit_id left join inv_subcategory on ir_sub_category_id=sc_id left join gnl_users on gu_user_id=pi_user_id left join gnl_location on gl_id=pi_location_id left join inv_barcode on b_uniquecode=pp_uniquecode left join inv_group1 on g1_id=ir_group1 left join inv_group2 on g2_id=ir_group2 left join acc_area on area_id = as_area_id " +
                "where pi_sup_id = " + model.SupplierId;

            if (!string.IsNullOrEmpty(model.FromDate))
            {
                sql1 += " AND pi_date >= '" + model.FromDate + "'";
            }

            if (!string.IsNullOrEmpty(model.ToDate))
            {
                sql1 += " AND pi_date <= '" + model.ToDate + "'";
            }
            DataTable customdetails = usqlre.dbReaderFill(sql1);
            hash.Add("Customdetails", customdetails);
            usqlre.close();
            string jsonResult = ReportModelContext.searializeDt(hash);
            return Content(jsonResult, "application/json");
        }


        [HttpGet("check-supplier-invoice")]
        public IActionResult CheckSupplierInvoice(string supplierInvoiceNo, int supplierId, int entryNo)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            try
            {
                string sql = "select count(*),pi_entryno from inv_purchase_inf where pi_sup_invno='" + supplierInvoiceNo + "' and pi_sup_id=" + supplierId + " and pi_entryno != " + entryNo + " group by pi_entryno";
                DataTable SupplyInvNo = usqlre.dbReaderFill(sql);
                if (SupplyInvNo.Rows.Count > 0 && Convert.ToInt32(SupplyInvNo.Rows[0][0].ToString()) > 0)
                {
                    string outmsg = SupplyInvNo.Rows[0][1].ToString();
                    return Ok(new { Message = $"Already Done In {outmsg}", Success = false });
                }
                return Ok(new { Message = "No duplicates found", Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred", Details = ex.Message });
            }
        }
        [HttpGet("check-supplier-old-balance")]
        public IActionResult CheckSupplierOb(int supplierId)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            try
            {
                string sql = $"EXEC Sp_acc_reg @StatementType = 'ledbalance', @as_id = {supplierId};";
                DataTable supplier = usqlre.dbReaderFill(sql);
                if (supplier.Rows.Count > 0)
                {   
                    string balance = supplier.Rows[0][0].ToString();
                    return Ok(new { OB = balance });
                }
                else
                {
                    return NotFound(new { Message = "Supplier not found or no balance available" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred", Details = ex.Message });
            }
        }
        [HttpGet("item-list/{Id}")]
        public IActionResult allSalesType(int Id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            var hash = new Dictionary<string, DataTable>();
            string sql = @"SELECT distinct i.ir_id AS itemId, ir_code as itemCode, ir_name as Name, ir_cgst as Cgst,
            ir_sgst as Sgst,ir_igst as Igst, ir_hsn_code as_HsnCode, ir_taxper as irTaxper,ir_cess as irCess ,
            ir_ad_cess as irAdCess,u_id,u_name, (SELECT 
			CASE 
				WHEN (SELECT TOP 1 pp_prate FROM inv_parchase_par where i.ir_id= pp_ir_id ORDER BY pp_prate DESC) > 0 
				THEN (SELECT TOP 1 pp_prate FROM inv_parchase_par where i.ir_id= pp_ir_id ORDER BY pp_prate DESC)
				WHEN (SELECT TOP 1 osp_prate FROM inv_openingstock_par where i.ir_id= osp_ir_id ORDER BY osp_prate DESC) > 0 
				THEN (SELECT TOP 1 osp_prate FROM inv_openingstock_par where i.ir_id= osp_ir_id ORDER BY osp_prate DESC)
				ELSE 0 
			END )AS pRate,mrp as Mrp,
            retail as Retail, spretail as SpRetail, wholesale as WholeSale, branch as Branch
            from inv_item_reg i
			left join View_Stock v on v.ir_id =i.ir_id
            left join inv_unit on ir_min_unit_id = u_id
            where ir_active = 1 AND i.ir_id=" + Id + "";
            DataTable allItemList = usqlre.dbReaderFill(sql);
            hash.Add("allItemList", allItemList);
            string sql1 = "select im_id as Value, im_unit_id as UnitId, im_conversion ,u_name as label,ISNULL(cast(im_rate as nvarchar(100)),'0') im_rate,ISNULL(cast(im_loading_charge as nvarchar(100)),'0') im_loading_charge,ISNULL(cast(im_retail as nvarchar(100)),'0') im_retail,ISNULL(cast(im_wsale as nvarchar(100)),'0') im_wsale,ISNULL(cast(im_spretail as nvarchar(100)),'0') im_spretail,ISNULL(cast(im_branch as nvarchar(100)),'0') im_branch,im_ir_id as Conversion  from inv_multi_unit inner join inv_item_reg on ir_id = im_ir_id inner join inv_unit u on im_unit_id=u_id where ir_active = 1 and im_ir_id=" + Id + "";
            DataTable multi = usqlre.dbReaderFill(sql1);
            hash.Add("Multi", multi);
            usqlre.close();
            string jsonResult = ReportModelContext.searializeDt(hash);
            return Content(jsonResult, "application/json");
        }


        [HttpPost("item-registration")]
        public async Task<IActionResult> itemReg([FromBody] ItemRegModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT COUNT(1) FROM inv_item_reg WHERE ir_name = '"+model.ItemName+ "'";
            DataTable itemName = usqlre.dbReaderFill(sql);
            if (itemName.Rows.Count > 0 && Convert.ToInt32(itemName.Rows[0][0].ToString()) > 0)
            {                 
                return Ok(new { status = false, msg = "Item already exists" });
            }
            if (model.items != null && model.items.Length > 0)
            {
                if (model.MinUnitId == null || model.items[0].ImUnitId == null ||
                    model.MinUnitId != model.items[0].ImUnitId)
                {
                    return Ok(new
                    {
                        status = false,
                        msg = "First item unit must be same as min unit"
                    });
                }
            }
            int entryNo = usqlre.itemReg("Insert", model);
            return Ok(new { status = entryNo > 0, msg = entryNo > 0 ? "Saved Successfully" : "Error in saving data" });
        }
        [HttpPost("category-regitration")]
        public async Task<IActionResult> categoryReg([FromBody] CategoryModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                string sql = "DECLARE @return_value int EXEC @return_value =  [dbo].[Sp_inv_category] @c_name='" + model.Name + "',@c_remarks='" + model.Remarks + "',@c_group3_id= - 1,@StatementType = N'Insert' SELECT 'return' = @return_value ";
                DataTable dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dt.Rows[0]["return"].ToString()) == 1)
                        {
                            return Ok(new { status = true});
                        }
                    }
                    return Ok(new { status = false });
                }
                else
                    return Ok(new { status = false });
            }
            catch
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }

        }
        [HttpPost("subcategory-regitration")]
        public async Task<IActionResult> subcategoryReg([FromBody] CategoryModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                string sql = "DECLARE @return_value int EXEC @return_value =  [dbo].[Sp_inv_subcategory] @sc_name='" + model.Name + "',@sc_remarks='" + model.Remarks + "',@StatementType = N'Insert' SELECT 'return' = @return_value ";
                DataTable dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dt.Rows[0]["return"].ToString()) == 1)
                        {
                            return Ok(new { status = true });
                        }
                    }
                    return Ok(new { status = false });
                }
                else
                    return Ok(new { status = false });
            }
            catch
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }

        }
        [HttpPost("brand-regitration")]
        public async Task<IActionResult> brandReg([FromBody] CategoryModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                string sql = "DECLARE @return_value int EXEC @return_value =  [dbo].[Sp_inv_brand] @bra_name='" + model.Name + "',@bra_remarks='" + model.Remarks + "',@bra_transfersame= 0,@bra_comp_id= 0,@StatementType = N'Insert' SELECT 'return' = @return_value ";
                DataTable dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dt.Rows[0]["return"].ToString()) == 1)
                        {
                            return Ok(new { status = true });
                        }
                    }
                    return Ok(new { status = false });
                }
                else
                    return Ok(new { status = false });
            }
            catch
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }

        }
        [HttpPost("unit-regitration")]
        public async Task<IActionResult> unitReg([FromBody] CategoryModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                string sql = "DECLARE @return_value int EXEC @return_value =  [dbo].[Sp_inv_unit] @u_name='" + model.Name + "',@u_remarks='" + model.Remarks + "',@u_focus_qty= 0,@StatementType = N'Insert' SELECT 'return' = @return_value ";
                DataTable dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dt.Rows[0]["return"].ToString()) == 1)
                        {
                            return Ok(new { status = true });
                        }
                    }
                    return Ok(new { status = false });
                }
                else
                    return Ok(new { status = false });
            }
            catch
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }

        }
        [HttpPost("group1-regitration")]
        public async Task<IActionResult> group1Reg([FromBody] CategoryModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                string sql = "DECLARE @return_value int EXEC @return_value =  [dbo].[Sp_inv_group1] @g1_name='" + model.Name + "',@g1_remarks='" + model.Remarks + "',@StatementType = N'Insert' SELECT 'return' = @return_value ";
                DataTable dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dt.Rows[0]["return"].ToString()) == 1)
                        {
                            return Ok(new { status = true });
                        }
                    }
                    return Ok(new { status = false });
                }
                else
                    return Ok(new { status = false });
            }
            catch
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }

        }
        [HttpPost("group2-regitration")]
        public async Task<IActionResult> group2Reg([FromBody] CategoryModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                string sql = "DECLARE @return_value int EXEC @return_value =  [dbo].[Sp_inv_group2] @g2_name='" + model.Name + "',@g2_remarks='" + model.Remarks + "',@StatementType = N'Insert' SELECT 'return' = @return_value ";
                DataTable dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dt.Rows[0]["return"].ToString()) == 1)
                        {
                            return Ok(new { status = true });
                        }
                    }
                    return Ok(new { status = false });
                }
                else
                    return Ok(new { status = false });
            }
            catch
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }

        }
        [HttpPost("group3-regitration")]
        public async Task<IActionResult> group3Reg([FromBody] CategoryModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                string sql = "DECLARE @return_value int EXEC @return_value =  [dbo].[Sp_inv_group3] @g3_name='" + model.Name + "',@g3_remarks='" + model.Remarks + "',@StatementType = N'Insert' SELECT 'return' = @return_value ";
                DataTable dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dt.Rows[0]["return"].ToString()) == 1)
                        {
                            return Ok(new { status = true });
                        }
                    }
                    return Ok(new { status = false });
                }
                else
                    return Ok(new { status = false });
            }
            catch
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }

        }
        [HttpPost("area-regitration")]
        public async Task<IActionResult> areaReg([FromBody] CategoryModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                string sql = "DECLARE @return_value int EXEC @return_value =  [dbo].[Sp_area] @area_name='" + model.Name + "',@area_remarks='" + model.Remarks + "',@StatementType = N'Insert' SELECT 'return' = @return_value";
                DataTable dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dt.Rows[0]["return"].ToString()) == 1)
                        {
                            return Ok(new { status = true });
                        }
                    }
                    return Ok(new { status = false });
                }
                else
                    return Ok(new { status = false });
            }
            catch
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }

        }
        [HttpPost("route-regitration")]
        public async Task<IActionResult> routeReg([FromBody] CategoryModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                string sql = "DECLARE @return_value int " +
                     "EXEC @return_value = [dbo].[Sp_inv_route] " +
                     "@r_name = N'" + model.Name + "', " +
                     "@r_remarks = N'" + model.Remarks + "', " +
                     "@r_starting_place = N'" + model.StartingPlace + "', " +
                     "@r_ending_place = N'" + model.EndingPlace + "', " +
                     "@StatementType = N'Insert' " +
                     "SELECT 'return' = @return_value";
                DataTable dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dt.Rows[0]["return"].ToString()) == 1)
                        {
                            return Ok(new { status = true });
                        }
                    }
                    return Ok(new { status = false });
                }
                else
                    return Ok(new { status = false });
            }
            catch
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }

        }
        [HttpGet("item-list")]
        public IActionResult allSalesType()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = @"select i.ir_id as ItemId,ir_name as ItemName,ir_active as Active,ir_taxper as TaxPer
            from inv_item_reg i 
            order by i.ir_id";
            DataTable allItemList = usqlre.dbReaderFill(sql);
            usqlre.close();
            string jsonResult = ReportModelContext.searializeDt(allItemList);

            return Content(jsonResult, "application/json");
        }
        [HttpGet("getAllList")]
        public IActionResult getAllList()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            var hash = new Dictionary<string, DataTable>();
            string sql = "select c_id,c_name from inv_category";
            DataTable category = usqlre.dbReaderFill(sql);
            hash.Add("categories", category);
            string sql1 = "select sc_id,sc_name from inv_subcategory";
            DataTable subcategory = usqlre.dbReaderFill(sql1);
            hash.Add("subcategories", subcategory);
            string sql2 = "select bra_id,bra_name from inv_brand";
            DataTable brand = usqlre.dbReaderFill(sql2);
            hash.Add("brands", brand);
            string sql3 = "select u_id,u_name from inv_unit";
            DataTable unit = usqlre.dbReaderFill(sql3);
            hash.Add("units", unit);
            string sql4 = "SELECT tp_taxper FROM inv_taxper";
            DataTable taxper = usqlre.dbReaderFill(sql4);
            hash.Add("taxper", taxper);
            string sql5 = "select g1_id,g1_name from inv_group1";
            DataTable group1 = usqlre.dbReaderFill(sql5);
            hash.Add("group1", group1);
            string sql6 = "select g2_id,g2_name from inv_group2";
            DataTable group2 = usqlre.dbReaderFill(sql6);
            hash.Add("group2", group2);
            string sql7 = "select g3_id,g3_name from inv_group3";
            DataTable group3 = usqlre.dbReaderFill(sql7);
            hash.Add("group3", group3);
            usqlre.close();
            string jsonResult = ReportModelContext.searializeDt(hash);
            return Content(jsonResult, "application/json");
        }
        [HttpGet("item-next-entryno")]
        public IActionResult EntryNo()
        {
            string nextEntryNo = "1"; // Initialize with default value
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql = "SELECT TOP 1 (ir_id + 1) AS next_entryno FROM inv_item_reg ORDER BY ir_id DESC;";
                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt != null && dt.Rows.Count > 0)
                {
                    nextEntryNo = dt.Rows[0]["next_entryno"].ToString();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }

            return Ok(new { result = nextEntryNo });
        }
        [HttpPost("update-image")]
        public IActionResult UpdateCompanyLogo(IFormFile imageFile, string ItemCode)  // Add 'id' parameter to receive the ID
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("Invalid image file");
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    imageFile.CopyTo(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();

                    UserSqlServer usqlre = new UserSqlServer(this);
                    bool logoUpdated = usqlre.UpdateImage(imageBytes, ItemCode); 

                    if (logoUpdated)
                    {
                        return Ok(ReportModelContext.searializeDt(new { status = true }));
                    }
                    else
                    {
                        return NotFound(new { status = false, Message = "No company found or logo not updated" });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //[HttpGet("reseed")]
        //public IActionResult reseed()
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);
        //    string sql = "DECLARE @return INT;EXEC @return = Sp_Purchase @StatementType = 'P_Reseed'; SELECT @return AS Reseed;";
        //    //string sql = "DECLARE @return_value int EXEC @return_value =  Sp_Purchase @StatementType = N'P_Reseed' SELECT 'return' =@return_value";
        //    DataTable reseed = usqlre.dbReaderFill(sql);
        //    usqlre.close();
        //    string jsonResult = ReportModelContext.searializeDt(reseed);

        //    return Content(jsonResult, "application/json");
        //}
        //[HttpGet("supplier")]
        //public IActionResult supplier(int location)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);

        //    string sql = $"EXEC Sp_acc_reg @StatementType = 'sup', @as_location_id = {location};";
        //    DataTable supplier = usqlre.dbReaderFill(sql);
        //    usqlre.close();
        //    DataTable filteredSupplier = supplier.DefaultView.ToTable(false, "as_id", "as_name");
        //    string jsonResult = ReportModelContext.searializeDt(filteredSupplier);

        //    // Return the result as JSON
        //    return Content(jsonResult, "application/json");
        //}
    }
}
