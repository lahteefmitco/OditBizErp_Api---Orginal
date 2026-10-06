using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class ProductController : ControllerBase
    {

        [HttpGet("product-details/{id}")]
        public string productDetailsById(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

            bool LOCATIONWISEPRODUCT = false;
            if (usqlre.get_android_settings("LOCATION WISE PRODUCT"))
            {
                LOCATIONWISEPRODUCT = true;
            }

            string sql = @"SELECT inv_item_reg.ir_id,ir_name,ir_cgst,ir_sgst,ISNULL(cast(qty as nvarchar(100)), '0') qty,uniquecode,ISNULL(cast(prate as nvarchar(100)), '0') prate,ISNULL(cast(mrp as nvarchar(100)), '0') mrp,
                        ISNULL(cast(wholesale as nvarchar(100)), '0') wholesale,ISNULL(cast(spretail as nvarchar(100)), '0') spretail,
                        ISNULL(cast(branch as nvarchar(100)), '0') branch,ISNULL(cast(retail as nvarchar(100)), '0') retail,
                        ISNULL(cast(realprate as nvarchar(100)), '0') realprate,ISNULL(cast(R1 as nvarchar(100)), '0') R1,
                        ISNULL(cast(R2 as nvarchar(100)), '0') R2,ISNULL(cast(R3 as nvarchar(100)), '0') R3,
                        ISNULL(cast(R4 as nvarchar(100)), '0') R4,ir_kfc,ir_hsn_code,ir_batch_status,
                        ISNULL(cast(cost as nvarchar(100)), '0') cost,ir_allow_negative,inv_item_reg.ir_taxper,
                        TaxType,ISNULL(cast(realprate as nvarchar(100)), '0') as  realprate,ir_lend_rate as lendRate,
                        gl_name as Location,narration,as_name as SupplierName,int_barcode
                        from View_Stock inner join inv_item_reg on
                        inv_item_reg.ir_id = View_Stock.ir_id inner join gnl_location on View_Stock.location_id=gl_id 
                        left join acc_subhead on as_id= Sup WHERE inv_item_reg.ir_id = " + id + "";
            if(LOCATIONWISEPRODUCT && usqlre.user_role!="ADMIN")
               sql += "and location_id = '" + usqlre.locationId + "'";
            DataTable dt = usqlre.dbReaderFill(sql);
            hash.Add("product", dt);
            sql = "SELECT m.im_id as value,m.im_unit_id,m.im_conversion,u.u_name as label,ISNULL(cast(m.im_rate as nvarchar(100)),'0') im_rate,ISNULL(cast(m.im_loading_charge as nvarchar(100)),'0') im_loading_charge,ISNULL(cast(m.im_retail as nvarchar(100)),'0') im_retail,ISNULL(cast(m.im_wsale as nvarchar(100)),'0') im_wsale,ISNULL(cast(m.im_spretail as nvarchar(100)),'0') im_spretail,ISNULL(cast(m.im_branch as nvarchar(100)),'0') im_branch from inv_multi_unit m inner join inv_unit u on m.im_unit_id=u.u_id where im_ir_id='" + id + "' order by m.im_conversion ASC";
            dt = usqlre.dbReaderFill(sql);
            hash.Add("multi", dt);

            sql = "select top (4) sp_rate ,si_entryno ,si_date  from inv_sales_par inner join inv_sales_inf on si_entryno=sp_entryno where sp_ir_id = '" + id + "' order by sp_entryno desc";
            dt = usqlre.dbReaderFill(sql);
            hash.Add("lastRate", dt);
            usqlre.close();
            return ReportModelContext.searializeDt(hash);
        }
        //new product details 
        [HttpGet("product-details-oditbiz/{id}/{cId}/{strId}")]
        public string productDetailsByIdCustomerAndStrId(int id,int cId,int strId)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            bool ENABLESUPNAMEINPRODUCTDETAILS = false;
            if (usqlre.get_android_settings("ENABLE SUPNAME IN PRODUCT DETAILS"))
            {
                ENABLESUPNAMEINPRODUCTDETAILS = true;
            }
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";
            DataTable dt_settings= usqlre.dbReaderFill("select gs_status from gnl_settings where gs_value='DISABLECUSTOMERWISEITEMRETURN'");
            if(dt_settings.Rows.Count>0)
            {
                if(cId>0 && Convert.ToInt32(dt_settings.Rows[0]["gs_status"].ToString())==0 && (strId==7 || strId==9))
                {
                    sql = @"SELECT ISNULL(cast(sp_qty as nvarchar(100)), '0') qty,
                            ir_id as ir_id,ir_name,ir_cgst,ir_sgst,
                            ISNULL(cast(sp_rate as nvarchar(100)), '0')SRate,
                            ISNULL(cast(b_prate as nvarchar(100)), '0') prate,
                            ISNULL(cast(b_mrp as nvarchar(100)), '0')mrp,
                            ISNULL(cast(b_wholesale as nvarchar(100)), '0') wholesale,
                            ISNULL(cast(b_spretail as nvarchar(100)), '0') spretail,
                            ISNULL(cast(b_branch as nvarchar(100)), '0') branch,
                            ISNULL(cast(b_retail as nvarchar(100)), '0') retail,
                            ISNULL(cast(b_realprate as nvarchar(100)), '0') realprate,
                            ISNULL(cast(b_r1 as nvarchar(100)), '0') R1,
                            ISNULL(cast(b_r2 as nvarchar(100)), '0') R2,
                            ISNULL(cast(b_r3 as nvarchar(100)), '0')R3,
                            ISNULL(cast(b_r4 as nvarchar(100)), '0') R4,
                            ir_kfc,ir_hsn_code,ir_batch_status,
                            ISNULL(cast(b_cost as nvarchar(100)), '0') cost,
                            b_uniquecode uniquecode,
                            ir_allow_negative,inv_item_reg.ir_taxper,b_tax_type as TaxType,
                            gl_name as Location,ir_lend_rate as lendRate,ir_min_unit_id,b_narration as narration,
                            ir_min_rate As minRate,as_name as SupplierName,ir_int_barcode as int_barcode";

                    if (usqlre.get_android_settings("ENABLE MAX AND MIN RATE IN SALE"))
                    {
                        sql += ", ir_max_rate as maxRate";
                    }
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", ir_discper as discPer";
                    }

                    sql += @" from inv_sales_inf 
                            inner join inv_sales_par on si_str_id=sp_str_id and si_entryno=sp_entryno
                            inner join inv_barcode on sp_uniquecode=b_uniquecode
                            inner join inv_item_reg on ir_id=b_ir_id 
                            inner join gnl_location on b_location_id=gl_id
                            left join acc_subhead on as_id= b_as_id
                            where (sp_str_id=1 or sp_str_id=2 or sp_str_id=4 or sp_str_id=6)
                            and sp_ir_id=" + id + " and si_acc_id=" + cId + "";
                  
                }
                else
                {
                    sql = @"SELECT inv_item_reg.ir_id,ir_name,ir_cgst,
                        ir_sgst,ISNULL(cast(qty as nvarchar(100)), '0') qty,
                        uniquecode,ISNULL(cast(prate as nvarchar(100)), '0') prate,
                        ISNULL(cast(mrp as nvarchar(100)), '0') mrp,
                        ISNULL(cast(wholesale as nvarchar(100)), '0') wholesale,
                        ISNULL(cast(spretail as nvarchar(100)), '0') spretail,
                        ISNULL(cast(branch as nvarchar(100)), '0') branch,
                        ISNULL(cast(retail as nvarchar(100)), '0') retail,
                        ISNULL(cast(realprate as nvarchar(100)), '0') realprate,
                        ISNULL(cast(R1 as nvarchar(100)), '0') R1,
                        ISNULL(cast(R2 as nvarchar(100)), '0') R2,
                        ISNULL(cast(R3 as nvarchar(100)), '0') R3,
                        ISNULL(cast(R4 as nvarchar(100)), '0') R4,
                        ir_kfc,ir_hsn_code,ir_batch_status,
                        ISNULL(cast(cost as nvarchar(100)), '0') cost,
                        ir_allow_negative,inv_item_reg.ir_taxper,
                        TaxType,ISNULL(cast(realprate as nvarchar(100)), '0') as  realprate,
                        ir_lend_rate as lendRate,ir_min_unit_id,
                        gl_name as Location,narration,
                        ir_min_rate As minRate,as_name as SupplierName,ir_int_barcode as int_barcode";

                    if (usqlre.get_android_settings("ENABLE MAX AND MIN RATE IN SALE"))
                    {
                        sql += ", ir_max_rate as maxRate";
                    }
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", ir_discper as discPer";
                    }
                    sql += @" from View_Stock inner join inv_item_reg on
                        inv_item_reg.ir_id = View_Stock.ir_id inner join
                        gnl_location on View_Stock.location_id=gl_id
                        left join acc_subhead on as_id= Sup
                        WHERE inv_item_reg.ir_id = " + id + " and location_id = '" + usqlre.locationId + "'";
                }

            }
           
            
            DataTable dt = usqlre.dbReaderFill(sql);
            DataTable dt_lastrate = new DataTable();
            int lastratecnt = 0;
            hash.Add("product", dt);
            sql = "SELECT m.im_id as value,m.im_unit_id,m.im_conversion," +
                "u.u_name as label,ISNULL(cast(m.im_rate as nvarchar(100)),'0') im_rate," +
                "ISNULL(cast(m.im_loading_charge as nvarchar(100)),'0') im_loading_charge," +
                "ISNULL(cast(m.im_retail as nvarchar(100)),'0') im_retail," +
                "ISNULL(cast(m.im_wsale as nvarchar(100)),'0') im_wsale," +
                "ISNULL(cast(m.im_spretail as nvarchar(100)),'0') im_spretail," +
                "ISNULL(cast(m.im_branch as nvarchar(100)),'0') im_branch " +
                "from inv_multi_unit m inner join inv_unit u on m.im_unit_id=u.u_id " +
                "where im_ir_id='" + id + "' order by m.im_conversion ASC";

            dt = usqlre.dbReaderFill(sql);
            hash.Add("multi", dt);
            DataTable dt_lastcnt = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='SHOWLASTRATEINSALE'");
            if (dt_lastcnt.Rows.Count > 0)
                lastratecnt = Convert.ToInt32(dt_lastcnt.Rows[0][0].ToString());
           if(lastratecnt>0)
           {
                sql = "select top ("+ lastratecnt + ") sp_rate,si_entryno,si_date from inv_sales_par inner join inv_sales_inf on si_entryno=sp_entryno where sp_ir_id = '" + id + "' and si_acc_id='" + cId + "' order by sp_entryno desc";
           }
           else
           {
                sql = "select top (4) sp_rate,si_entryno,si_date from inv_sales_par inner join inv_sales_inf on si_entryno=sp_entryno where sp_ir_id = '" + id + "' and si_acc_id='" + cId + "' order by sp_entryno desc";
           }
            
            dt_lastrate = usqlre.dbReaderFill(sql);
            hash.Add("lastRate", dt_lastrate);
            if (ENABLESUPNAMEINPRODUCTDETAILS)
            {
                DataTable supName = new DataTable();
                sql = @"select isnull(as_name,'') as supplierName from inv_purchase_inf left join inv_parchase_par on pi_entryno=pp_entryno left join acc_subhead on as_id = pi_sup_id WHERE pp_ir_id = '" + id + "'  and pi_location_id = '" + usqlre.locationId + "'";
                supName = usqlre.dbReaderFill(sql);
                hash.Add("supName", supName);
            }           
            usqlre.close();
            return ReportModelContext.searializeDt(hash);
        }
        [HttpGet("product-details-oditbiz/{id}/{cId}")]
        public string Odz_productDetailsById(int id,int cId)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";
            DataTable dt_settings= usqlre.dbReaderFill("select gs_status from gnl_settings where gs_value='DISABLECUSTOMERWISEITEMRETURN'");
            if(dt_settings.Rows.Count>0)
            {
                if(cId>0 && Convert.ToInt32(dt_settings.Rows[0]["gs_status"].ToString())==0)
                {
                    sql = @"SELECT ISNULL(cast(sp_qty as nvarchar(100)), '0') qty,
                            ir_id as ir_id,ir_name,ir_cgst,ir_sgst,
                            ISNULL(cast(sp_rate as nvarchar(100)), '0')SRate,
                            ISNULL(cast(b_prate as nvarchar(100)), '0') prate,
                            ISNULL(cast(b_mrp as nvarchar(100)), '0')mrp,
                            ISNULL(cast(b_wholesale as nvarchar(100)), '0') wholesale,
                            ISNULL(cast(b_spretail as nvarchar(100)), '0') spretail,
                            ISNULL(cast(b_branch as nvarchar(100)), '0') branch,
                            ISNULL(cast(b_retail as nvarchar(100)), '0') retail,
                            ISNULL(cast(b_realprate as nvarchar(100)), '0') realprate,
                            ISNULL(cast(b_r1 as nvarchar(100)), '0') R1,
                            ISNULL(cast(b_r2 as nvarchar(100)), '0') R2,
                            ISNULL(cast(b_r3 as nvarchar(100)), '0')R3,
                            ISNULL(cast(b_r4 as nvarchar(100)), '0') R4,
                            ir_kfc,ir_hsn_code,ir_batch_status,
                            ISNULL(cast(b_cost as nvarchar(100)), '0') cost,
                            b_uniquecode uniquecode,
                            ir_allow_negative,inv_item_reg.ir_taxper,b_tax_type as TaxType,
                            gl_name as Location,ir_lend_rate as lendRate,ir_min_unit_id,b_narration as narration,as_name as SupplierName
                            from inv_sales_inf 
                            inner join inv_sales_par on si_str_id=sp_str_id and si_entryno=sp_entryno
                            inner join inv_barcode on sp_uniquecode=b_uniquecode
                            inner join inv_item_reg on ir_id=b_ir_id 
                            inner join gnl_location on b_location_id=gl_id
                            left join acc_subhead on as_id= b_as_id
                            where (sp_str_id=1 or sp_str_id=2 or sp_str_id=4 or sp_str_id=6)
                            and sp_ir_id=" + id + " and si_acc_id=" + cId + "";
                  
                }
                else
                {
                    sql = @"SELECT inv_item_reg.ir_id,ir_name,ir_cgst,
                        ir_sgst,ISNULL(cast(qty as nvarchar(100)), '0') qty,
                        uniquecode,ISNULL(cast(prate as nvarchar(100)), '0') prate,
                        ISNULL(cast(mrp as nvarchar(100)), '0') mrp,
                        ISNULL(cast(wholesale as nvarchar(100)), '0') wholesale,
                        ISNULL(cast(spretail as nvarchar(100)), '0') spretail,
                        ISNULL(cast(branch as nvarchar(100)), '0') branch,
                        ISNULL(cast(retail as nvarchar(100)), '0') retail,
                        ISNULL(cast(realprate as nvarchar(100)), '0') realprate,
                        ISNULL(cast(R1 as nvarchar(100)), '0') R1,
                        ISNULL(cast(R2 as nvarchar(100)), '0') R2,
                        ISNULL(cast(R3 as nvarchar(100)), '0') R3,
                        ISNULL(cast(R4 as nvarchar(100)), '0') R4,
                        ir_kfc,ir_hsn_code,ir_batch_status,
                        ISNULL(cast(cost as nvarchar(100)), '0') cost,
                        ir_allow_negative,inv_item_reg.ir_taxper,
                        TaxType,ISNULL(cast(realprate as nvarchar(100)), '0') as  realprate,
                        ir_lend_rate as lendRate,ir_min_unit_id,
                        gl_name as Location,narration,as_name as SupplierName
                        from View_Stock inner join inv_item_reg on
                        inv_item_reg.ir_id = View_Stock.ir_id inner join
                        gnl_location on View_Stock.location_id=gl_id
                        left join acc_subhead on as_id= Sup
                        WHERE inv_item_reg.ir_id = " + id + " and location_id = '" + usqlre.locationId + "'";
                }

            }
           
            
            DataTable dt = usqlre.dbReaderFill(sql);
            hash.Add("product", dt);
            sql = "SELECT m.im_id as value,m.im_unit_id,m.im_conversion," +
                "u.u_name as label,ISNULL(cast(m.im_rate as nvarchar(100)),'0') im_rate," +
                "ISNULL(cast(m.im_loading_charge as nvarchar(100)),'0') im_loading_charge," +
                "ISNULL(cast(m.im_retail as nvarchar(100)),'0') im_retail," +
                "ISNULL(cast(m.im_wsale as nvarchar(100)),'0') im_wsale," +
                "ISNULL(cast(m.im_spretail as nvarchar(100)),'0') im_spretail," +
                "ISNULL(cast(m.im_branch as nvarchar(100)),'0') im_branch " +
                "from inv_multi_unit m inner join inv_unit u on m.im_unit_id=u.u_id " +
                "where im_ir_id='" + id + "' order by m.im_conversion ASC";

            dt = usqlre.dbReaderFill(sql);
            hash.Add("multi", dt);
            usqlre.close();
            return ReportModelContext.searializeDt(hash);
        }

        [HttpGet("product-details/{id}/{cId}")]
        public string productDetailsByIdCustomer(int id, int cId)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            bool LOCATIONWISEPRODUCT = false;
            if (usqlre.get_android_settings("LOCATION WISE PRODUCT"))
            {
                LOCATIONWISEPRODUCT = true;
            }
            string sql = @"SELECT inv_item_reg.ir_id,ir_name,ir_cgst,ir_sgst,ISNULL(cast(qty as nvarchar(100)), '0') qty,uniquecode,ISNULL(cast(prate as nvarchar(100)), '0') prate,ISNULL(cast(mrp as nvarchar(100)), '0') mrp,
                        ISNULL(cast(wholesale as nvarchar(100)), '0') wholesale,ISNULL(cast(spretail as nvarchar(100)), '0') spretail,
                        ISNULL(cast(branch as nvarchar(100)), '0') branch,ISNULL(cast(retail as nvarchar(100)), '0') retail,
                        ISNULL(cast(realprate as nvarchar(100)), '0') realprate,ISNULL(cast(R1 as nvarchar(100)), '0') R1,
                        ISNULL(cast(R2 as nvarchar(100)), '0') R2,ISNULL(cast(R3 as nvarchar(100)), '0') R3,
                        ISNULL(cast(R4 as nvarchar(100)), '0') R4,ir_kfc,ir_hsn_code,ir_batch_status,
                        ISNULL(cast(cost as nvarchar(100)), '0') cost,ir_allow_negative,inv_item_reg.ir_taxper,
                        TaxType,ISNULL(cast(realprate as nvarchar(100)), '0') as  realprate,ir_lend_rate as lendRate,
                        gl_name as Location,narration,as_name as SupplierName,int_barcode
                        from View_Stock inner join inv_item_reg on
                        inv_item_reg.ir_id = View_Stock.ir_id inner join gnl_location on View_Stock.location_id=gl_id left join acc_subhead on as_id= Sup WHERE inv_item_reg.ir_id = " + id + "";
            if (LOCATIONWISEPRODUCT || usqlre.user_role != "ADMIN")
                sql += "and location_id = '" + usqlre.locationId + "'";
            DataTable dt = usqlre.dbReaderFill(sql);
            hash.Add("product", dt);
            sql = "SELECT m.im_id as value,m.im_unit_id,m.im_conversion,u.u_name as label,ISNULL(cast(m.im_rate as nvarchar(100)),'0') im_rate,ISNULL(cast(m.im_loading_charge as nvarchar(100)),'0') im_loading_charge,ISNULL(cast(m.im_retail as nvarchar(100)),'0') im_retail,ISNULL(cast(m.im_wsale as nvarchar(100)),'0') im_wsale,ISNULL(cast(m.im_spretail as nvarchar(100)),'0') im_spretail,ISNULL(cast(m.im_branch as nvarchar(100)),'0') im_branch from inv_multi_unit m inner join inv_unit u on m.im_unit_id=u.u_id where im_ir_id='" + id + "' order by m.im_conversion ASC";
            dt = usqlre.dbReaderFill(sql);
            hash.Add("multi", dt);
            sql = "select top (4) sp_rate,si_entryno,si_date  from inv_sales_par inner join inv_sales_inf on si_entryno=sp_entryno where sp_ir_id = '" + id + "' and si_acc_id='" + cId + "' order by sp_entryno desc";
            dt = usqlre.dbReaderFill(sql);
            hash.Add("lastRate", dt);
            usqlre.close();
            return ReportModelContext.searializeDt(hash);
        }
        [HttpGet("product-details-location/{id}/{location}")]
        public string productDetailsByIdLocation(int id, int location)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            bool ENABLESUPNAMEINPRODUCTDETAILS = false;
            if (usqlre.get_android_settings("ENABLE SUPNAME IN PRODUCT DETAILS"))
            {
                ENABLESUPNAMEINPRODUCTDETAILS = true;
            }
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = @"SELECT inv_item_reg.ir_id,ir_name,ir_cgst,ir_sgst,ISNULL(cast(qty as nvarchar(100)), '0') qty,uniquecode,ISNULL(cast(prate as nvarchar(100)), '0') prate,ISNULL(cast(mrp as nvarchar(100)), '0') mrp,
                        ISNULL(cast(wholesale as nvarchar(100)), '0') wholesale,ISNULL(cast(spretail as nvarchar(100)), '0') spretail,
                        ISNULL(cast(branch as nvarchar(100)), '0') branch,ISNULL(cast(retail as nvarchar(100)), '0') retail,
                        ISNULL(cast(realprate as nvarchar(100)), '0') realprate,ISNULL(cast(R1 as nvarchar(100)), '0') R1,
                        ISNULL(cast(R2 as nvarchar(100)), '0') R2,ISNULL(cast(R3 as nvarchar(100)), '0') R3,
                        ISNULL(cast(R4 as nvarchar(100)), '0') R4,ir_kfc,ir_hsn_code,ir_batch_status,
                        ISNULL(cast(cost as nvarchar(100)), '0') cost,ir_allow_negative,inv_item_reg.ir_taxper,
                        TaxType,ISNULL(cast(realprate as nvarchar(100)), '0') as  realprate,ir_lend_rate as lendRate,
                        gl_name as Location,narration,ir_min_rate As minRate,as_name as SupplierName,int_barcode";

                        if (usqlre.get_android_settings("ENABLE MAX AND MIN RATE IN SALE"))
                        {
                            sql += ", ir_max_rate as maxRate";
                        }
                        if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                        {
                            sql += ", ir_discper as discPer";
                        }
            sql += @" from View_Stock inner join inv_item_reg on
                        inv_item_reg.ir_id = View_Stock.ir_id inner join gnl_location on View_Stock.location_id=gl_id 
                        left join acc_subhead on as_id= Sup
                        WHERE inv_item_reg.ir_id = " + id + "";
            sql += "and location_id = '" + location + "'";
            DataTable dt = usqlre.dbReaderFill(sql);
            hash.Add("product", dt);
            sql = "SELECT m.im_id as value,m.im_unit_id,m.im_conversion,u.u_name as label,ISNULL(cast(m.im_rate as nvarchar(100)),'0') im_rate,ISNULL(cast(m.im_loading_charge as nvarchar(100)),'0') im_loading_charge,ISNULL(cast(m.im_retail as nvarchar(100)),'0') im_retail,ISNULL(cast(m.im_wsale as nvarchar(100)),'0') im_wsale,ISNULL(cast(m.im_spretail as nvarchar(100)),'0') im_spretail,ISNULL(cast(m.im_branch as nvarchar(100)),'0') im_branch from inv_multi_unit m inner join inv_unit u on m.im_unit_id=u.u_id where im_ir_id='" + id + "' order by m.im_conversion ASC";
            dt = usqlre.dbReaderFill(sql);
            hash.Add("multi", dt);
            sql = "select top (4) sp_rate ,si_entryno ,si_date  from inv_sales_par inner join inv_sales_inf on si_entryno=sp_entryno where sp_ir_id = '" + id + "' and si_location_id= '" + location + "' order by sp_entryno desc";
            dt = usqlre.dbReaderFill(sql);
            hash.Add("lastRate", dt);
            if (ENABLESUPNAMEINPRODUCTDETAILS)
            {
                DataTable supName = new DataTable();
                sql = @"select isnull(as_name,'') as supplierName from inv_purchase_inf left join inv_parchase_par on pi_entryno=pp_entryno left join acc_subhead on as_id = pi_sup_id WHERE pp_ir_id = '" + id + "'  and pi_location_id = '" + location + "'";
                supName = usqlre.dbReaderFill(sql);
                hash.Add("supName", supName);
            }
            usqlre.close();
            return ReportModelContext.searializeDt(hash);
        }


        [HttpPost("product-details")]
        public async Task<IActionResult> productDetails([FromBody] KiosReport model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            String value = model.value.Replace("\\", "");
            value = value.Replace("\\/", "");
            bool LOCATIONWISEPRODUCT = false;
            if (usqlre.get_android_settings("LOCATION WISE PRODUCT"))
            {
                LOCATIONWISEPRODUCT = true;
            }
            string sql = "";
            if (model.isBarcode)
            {
                sql = @"SELECT inv_item_reg.ir_id,ir_name,ir_cgst,ir_sgst,ISNULL(cast(qty as nvarchar(100)), '0') qty,uniquecode,int_barcode,
                    ISNULL(cast(prate as nvarchar(100)), '0') prate,ISNULL(cast(mrp as nvarchar(100)), '0') mrp,
                    ISNULL(cast(wholesale as nvarchar(100)), '0') wholesale,ISNULL(cast(spretail as nvarchar(100)), '0') spretail,
                    ISNULL(cast(branch as nvarchar(100)), '0') branch,ISNULL(cast(retail as nvarchar(100)), '0') retail,
                    ISNULL(cast(realprate as nvarchar(100)), '0') realprate,ir_lend_rate as lendRate,
                    ISNULL(cast(R1 as nvarchar(100)), '0') R1,ISNULL(cast(R2 as nvarchar(100)), '0') R2,
                    ISNULL(cast(R3 as nvarchar(100)), '0') R3,ISNULL(cast(R4 as nvarchar(100)), '0') R4,
                    ir_kfc,ir_hsn_code,ir_batch_status,ISNULL(cast(cost as nvarchar(100)), '0') cost,
                    ir_allow_negative,inv_item_reg.ir_taxper,TaxType,ISNULL(cast(realprate as nvarchar(100)), '0') as  realprate,
                    gl_name as Location,narration,as_name as SupplierName
                    from View_Stock inner join inv_item_reg on inv_item_reg.ir_id = View_Stock.ir_id
                    inner join gnl_location on View_Stock.location_id=gl_id left join acc_subhead on as_id= Sup WHERE ";
                sql += " (narration = '" + value + "' OR cast(uniquecode as nvarchar(100))='" + value + "' OR size='" + value + "' OR int_barcode = '" + value + "')";
                if (LOCATIONWISEPRODUCT || usqlre.user_role != "ADMIN")
                    sql += " and View_Stock.location_id = '" + usqlre.locationId + "'";
            }
            else if (model.isItemCode)
            {
                sql = @"SELECT inv_item_reg.ir_id,ir_name,ir_cgst,ir_sgst,ISNULL(cast(qty as nvarchar(100)), '0') qty,uniquecode,int_barcode,
                    ISNULL(cast(prate as nvarchar(100)), '0') prate,ISNULL(cast(mrp as nvarchar(100)), '0') mrp,
                    ISNULL(cast(wholesale as nvarchar(100)), '0') wholesale,ISNULL(cast(spretail as nvarchar(100)), '0') spretail,
                    ISNULL(cast(branch as nvarchar(100)), '0') branch,ISNULL(cast(retail as nvarchar(100)), '0') retail,
                    ISNULL(cast(realprate as nvarchar(100)), '0') realprate,ir_lend_rate as lendRate,
                    ISNULL(cast(R1 as nvarchar(100)), '0') R1,ISNULL(cast(R2 as nvarchar(100)), '0') R2,
                    ISNULL(cast(R3 as nvarchar(100)), '0') R3,ISNULL(cast(R4 as nvarchar(100)), '0') R4,
                    ir_kfc,ir_hsn_code,ir_batch_status,ISNULL(cast(cost as nvarchar(100)), '0') cost,
                    ir_allow_negative,inv_item_reg.ir_taxper,TaxType,ISNULL(cast(realprate as nvarchar(100)), '0') as  realprate,
                    gl_name as Location,narration,as_name as SupplierName
                    from View_Stock inner join inv_item_reg on inv_item_reg.ir_id = View_Stock.ir_id
                    inner join gnl_location on View_Stock.location_id=gl_id left join acc_subhead on as_id= Sup WHERE ";
                sql += " (inv_item_reg.ir_code = '" + value + "')";
                if (LOCATIONWISEPRODUCT || usqlre.user_role != "ADMIN")
                    sql += " and View_Stock.location_id = '" + usqlre.locationId + "'";
            }
            else
            {
                sql = @"SELECT inv_item_reg.ir_id,ir_name,ir_cgst,ir_sgst,ISNULL(cast(qty as nvarchar(100)), '0') qty,
                    uniquecode,int_barcode,ISNULL(cast(prate as nvarchar(100)), '0') prate,ISNULL(cast(mrp as nvarchar(100)), '0') mrp,
                    ISNULL(cast(wholesale as nvarchar(100)), '0') wholesale,ISNULL(cast(spretail as nvarchar(100)), '0') spretail,
                    ISNULL(cast(branch as nvarchar(100)), '0') branch,ISNULL(cast(retail as nvarchar(100)), '0') retail,
                    ISNULL(cast(realprate as nvarchar(100)), '0') realprate,ir_lend_rate as lendRate,ISNULL(cast(R1 as nvarchar(100)), '0') R1,
                    ISNULL(cast(R2 as nvarchar(100)), '0') R2,ISNULL(cast(R3 as nvarchar(100)), '0') R3,
                    ISNULL(cast(R4 as nvarchar(100)), '0') R4,ir_kfc,ir_hsn_code,ir_batch_status,ISNULL(cast(cost as nvarchar(100)), '0') cost,
                    ir_allow_negative,inv_item_reg.ir_taxper,TaxType,ISNULL(cast(realprate as nvarchar(100)), '0') as  realprate,
                    gl_name as Location,narration,as_name as SupplierName
                    from View_Stock inner join inv_item_reg on inv_item_reg.ir_id = View_Stock.ir_id
                    inner join gnl_location on View_Stock.location_id=gl_id left join acc_subhead on as_id= Sup WHERE ";
                sql += " inv_item_reg.ir_id = '" + value + "'";
                if (LOCATIONWISEPRODUCT || usqlre.user_role != "ADMIN")
                    sql += "and View_Stock.location_id = '" + usqlre.locationId+"'";
            }
            
            DataTable dt = usqlre.dbReaderFill(sql);
            
            string ir_id = "0";
            if (dt != null)
            {
                if (dt.Rows.Count > 0)
                {
                    ir_id = dt.Rows[0]["ir_id"].ToString();
                }
            }
            hash.Add("product", dt);
            if (ir_id != "0")
            {
                sql = "SELECT m.im_id as value ,m.im_unit_id,m.im_conversion,u.u_name as label,ISNULL(cast(m.im_rate as nvarchar(100)),'0') im_rate,ISNULL(cast(m.im_loading_charge as nvarchar(100)),'0') im_loading_charge,ISNULL(cast(m.im_retail as nvarchar(100)),'0') im_retail,ISNULL(cast(m.im_wsale as nvarchar(100)),'0') im_wsale,ISNULL(cast(m.im_spretail as nvarchar(100)),'0') im_spretail,ISNULL(cast(m.im_branch as nvarchar(100)),'0') im_branch from inv_multi_unit m inner join inv_unit u on m.im_unit_id=u.u_id where im_ir_id='" + ir_id + "' order by m.im_conversion ASC";
                dt = usqlre.dbReaderFill(sql);
                hash.Add("multi", dt);
            }
            sql = "select top (2) sp_rate,si_entryno,si_date from inv_sales_par inner join inv_sales_inf on si_entryno=sp_entryno where sp_ir_id = '" + ir_id + "' and si_acc_id='" + model.customerId + "' order by sp_entryno desc";
            dt = usqlre.dbReaderFill(sql);
            hash.Add("lastRate", dt);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(hash));
        }

        [HttpPost("search-product-code")]
        public async Task<IActionResult> searchProductCode([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "";
            if (model.Value == "")
                sql = "SELECT top 20 ir_code as label,ir_code as value from inv_item_reg WHERE ir_active=1";
            else
                sql = "SELECT ir_code as label,ir_code as value from inv_item_reg where ir_code like '%" + model.Value + "%' and ir_active=1";

            DataTable products_codes = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(products_codes));
        }

        [HttpPost("search-product-name")]
        public async Task<IActionResult> searchProductName([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "";
            if (model.Value == "")
                sql = "SELECT top 20 ir_name as label,ir_id as value from  inv_item_reg and ir_active=1";
            else
                sql = "SELECT ir_name as label,ir_id as value from inv_item_reg where ir_name like '%" + model.Value + "%' and ir_active=1";

            DataTable products = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(products));
        }


        [HttpGet("location-stock-products/{location}")]
        public async Task<IActionResult> locationStockProducts(int location)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "";
            sql = "select max(V.ir_id)as value,max(I.ir_name) as label from View_stock V left join inv_item_reg i on V.ir_id=i.ir_id where location_id=" + location + "  and qty>0 group by V.ir_id";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));

        }
        [HttpPost("search-lend-item")]
        public async Task<IActionResult> searchLendItem([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "";
            if (model.Value == "")
                sql = "SELECT top 20 ir_name as label,ir_id as value from  inv_item_reg where ir_lend_status=1";
            else
                sql = "SELECT ir_name as label,ir_id as value from inv_item_reg where  ir_lend_status=1 and  ir_name like '%" + model.Value + "%'";

            DataTable products = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(products));
        }
    }
}
