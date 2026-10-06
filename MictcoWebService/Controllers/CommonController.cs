using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Newtonsoft.Json;

namespace MictcoWebService.Controllers
{

    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class CommonController : ControllerBase
    {

        [HttpGet("import-all")]
        public IActionResult importAll()
        {

            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            UserSqlServer usqlre = new UserSqlServer(this);
            string userid = usqlre.userId;

            // Ensure all project android settings exist (insert with status 0 if missing)
            usqlre.ensure_all_android_settings();

            bool SALESMANWISECUSTOMER = false;
            bool SHOWSALESMANCODE = false;
            bool SHOWSALESMANWITHCODE = false;
            bool LOCATIONWISECUSTOMER = false;
            //bool AREAWISECUSTOMER = false;
            //bool ROUTWISECUSTOMER = false;
            bool AREAORROUTE = false;
            string[] areaList = usqlre.areaId.Split(",");
            string areaCondtn = "";
            string areaonly = " ";
            string[] routList = usqlre.routeId.Split(",");
            string routCondtn = "";
            string routonly = " ";
            string areaRoutCondition = "";
            string areaRoutOnly = "";


            if (usqlre.get_android_settings("SALESMANWISECUSTOMER"))
            {
                SALESMANWISECUSTOMER = true;
            }
            if (usqlre.get_android_settings("SHOW SALESMAN CODE"))
            {
                SHOWSALESMANCODE = true;
            }
            if (usqlre.get_android_settings("SHOW SALESMAN WITH CODE"))
            {
                SHOWSALESMANWITHCODE = true;
            }
            if (usqlre.get_android_settings("LOCATION WISE CUSTOMER"))
            {
                LOCATIONWISECUSTOMER = true;
            }
            if (usqlre.get_android_settings("AREAWISE LOGIN"))
            {
                AREAORROUTE = true;
                if (areaList.Length > 0)
                {
                    for (int i = 0; i < areaList.Length; i++)
                    {
                        int areaId;
                        if (int.TryParse(areaList[i], out areaId) && areaId > 0)
                        {
                            if (i == 0)
                            {
                                areaCondtn = " and as_area_id= " + areaId + "";
                                areaonly = " where as_area_id= " + areaId + "";
                            }
                            else
                            {
                                areaCondtn += " or as_area_id= " + areaId + "";
                                areaonly += " or as_area_id= " + areaId + "";
                            }
                        }
                    }
                }
                areaRoutCondition = areaCondtn;
                areaRoutOnly = areaonly;
            }
            if (usqlre.get_android_settings("ROUTEWISE LOGIN"))
            {
                //ROUTWISECUSTOMER = true;
                AREAORROUTE = true;
                if (routList.Length > 0)
                {
                    for (int i = 0; i < routList.Length; i++)
                    {
                        if (Convert.ToInt32(routList[i]) > 0)
                        {
                            if (i == 0)
                            {
                                routCondtn = " and as_rout_id= " + routList[i] + "";
                                routonly = " where as_rout_id= " + routList[i] + "";
                            }
                            else
                            {
                                routCondtn = routCondtn + " or as_rout_id= " + routList[i] + "";
                                routonly = routonly + " or as_rout_id= " + routList[i] + "";
                            }
                        }

                    }
                }
                areaRoutCondition = routCondtn;
                areaRoutOnly = routonly;
            }



            string sql = "";
            if (SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value, as_active as active from acc_subhead group by as_id,as_active order by label asc";

                }
                else
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value, as_active as active from acc_subhead where as_salesman_id = '" + usqlre.gu_acc_id + "' and as_location_id ='" + usqlre.locationId + "'" + areaRoutCondition + "  group by as_id,as_active order by label asc";
                }
            }
            else if (SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && !AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value, as_active as active from acc_subhead group by as_id,as_active order by label asc";

                }
                else
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead where as_salesman_id = '" + usqlre.gu_acc_id + "' and as_location_id ='" + usqlre.locationId + "' group by as_id,as_active order by label asc";
                }
            }
            else if (SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead group by as_id,as_active order by label asc";

                }
                else
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead where as_salesman_id = '" + usqlre.gu_acc_id + "' "+ areaRoutCondition + "  group by as_id,as_active order by label asc";
                }
            }
            if (SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && !AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead group by as_id,as_active order by label asc";

                }
                else
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead where as_salesman_id = '" + usqlre.gu_acc_id + "'  group by as_id,as_active order by label asc";
                }
            }
            else if (!SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead group by as_id,as_active order by label asc";

                }
                else
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead where as_location_id ='" + usqlre.locationId + "'"+ areaRoutCondition + "  group by as_id,as_active  order by label asc";
                }
            }
            else if (!SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead group by as_id ,as_active order by label asc";

                }
                else
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead" + areaRoutOnly+ "  group by as_id,as_active order by label asc";
                }
            }
            else if (!SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && !AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead group by as_id,as_active order by label asc";

                }
                else
                {
                    sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead where as_location_id ='" + usqlre.locationId + "'   group by as_id,as_active order by label asc";
                }
            }
            else
            {
                sql = "SELECT max(as_name) as label,max(as_id) as value,  as_active as active from acc_subhead group by as_id,as_active order by label asc";
            }

            DataTable ledger = usqlre.dbReaderFill(sql);
            hash.Add("ledger", ledger);

            sql = "SELECT ap_name as label,ap_id as value from acc_parent";
            DataTable group = usqlre.dbReaderFill(sql);
            hash.Add("group", group);

            sql = "SELECT gl_name as label,gl_id as value,gl_short_1, gl_short_2, gl_short_3, " +
                "gl_short_4, gl_short_5, gl_short_6, gl_short_7, gl_short_8, gl_short_9 from gnl_location";
            DataTable location = usqlre.dbReaderFill(sql);
            hash.Add("location", location);

            sql = "SELECT c_name as label,c_id as value from inv_category where c_name !=''";
            DataTable category = usqlre.dbReaderFill(sql);
            hash.Add("category", category);

            sql = "SELECT bra_name as label,bra_id as value,bra_company_id from inv_brand";
            DataTable brand = usqlre.dbReaderFill(sql);
            hash.Add("brand", brand);


            sql = "select ir_code as label,ir_id as value from inv_item_reg where ir_active=1";
            DataTable itemCode = usqlre.dbReaderFill(sql);
            hash.Add("itemCode", itemCode);

            bool productNameOther = false;
            bool productAliasColumn = false;
            bool DISABLE_LOCATION_BASED_STOCK_IN_PRODUCT_LIST = false;
            if (usqlre.get_android_settings("PRODUCT NAME ALTERNATIVE"))
            {
                productNameOther = true;
            }
            if (usqlre.get_android_settings("DISABLE LOCATION BASED STOCK_IN PRODUCT LIST"))
            {
                DISABLE_LOCATION_BASED_STOCK_IN_PRODUCT_LIST = true;
            }
            if(!DISABLE_LOCATION_BASED_STOCK_IN_PRODUCT_LIST)
            {
                if (productNameOther)
                {
                    if (usqlre.get_android_settings("PRODUCT NAME ALIASCOLUMN"))
                    {
                        productAliasColumn = true;
                    }
                    if (productAliasColumn)
                    {
                        sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_alias_name as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  and  v.location_id='" + usqlre.locationId +@"'
                    GROUP BY i.ir_name,i.ir_alias_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch;
                    ";
                    }
                    else
                    {
                        sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_hsn_code as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  and  v.location_id='" + usqlre.locationId +@"'
                    GROUP BY i.ir_name,i.ir_hsn_code,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch;
                    ";

                    }
                }
                else
                {
                    sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  and  v.location_id='" + usqlre.locationId +@"'
                    GROUP BY i.ir_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch;
                    ";

                }
            }
            else
            {
                if (productNameOther)
                {
                    if (usqlre.get_android_settings("PRODUCT NAME ALIASCOLUMN"))
                    {
                        productAliasColumn = true;
                    }
                    if (productAliasColumn)
                    {
                        sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_alias_name as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  
                    GROUP BY i.ir_name,i.ir_alias_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch;
                    ";
                    }
                    else
                    {
                        sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_hsn_code as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  
                    GROUP BY i.ir_name,i.ir_hsn_code,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch;
                    ";

                    }
                }
                else
                {
                    sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  
                    GROUP BY i.ir_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch;
                    ";

                }
            }
            

            DataTable itemName = usqlre.dbReaderFill(sql);
            hash.Add("itemName", itemName);

            sql = "SELECT sc_name as label,sc_id as value from inv_subcategory";
            DataTable subcategory = usqlre.dbReaderFill(sql);
            hash.Add("subcategory", subcategory);

            //if (SALESMANWISECUSTOMER)
            //{
            //    if (usqlre.user_role == "ADMIN")
            //    {
            //        sql = "SELECT as_name as label,as_id as value,rt_name as as_rate_type from acc_subhead left join inv_rate_type on as_rate_type = rt_name_display  where as_ap_id=4";
            //    }
            //    else
            //    { 
            //        sql = "SELECT as_name as label,as_id as value,rt_name as as_rate_type from acc_subhead left join inv_rate_type on as_rate_type = rt_name_display  where as_ap_id=4 and as_salesman_id = '"+usqlre.gu_acc_id + "'";
            //    }
            //}
            //else
            //{
            //    sql = "SELECT as_name as label,as_id as value,rt_name as as_rate_type from acc_subhead left join inv_rate_type on as_rate_type = rt_name_display  where as_ap_id=4";
            //}



            if (SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type,as_tin as tin, as_active as active,as_account_code as accountCode,as_mob as mobile ,as_disc_per as discPer,as_credit_limit as creditLimit ,as_latitude as latitude, as_longitude as longitude ";

                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += "  from acc_subhead  where as_ap_id=4 or as_ap_id=1 order by label asc";

                }
                else
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type ,as_tin, as_active as active,as_account_code as accountCode,as_mob as mobile ,as_disc_per as discPer,as_credit_limit as creditLimit ,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += "   from acc_subhead  where as_ap_id=4 and as_salesman_id = '" + usqlre.gu_acc_id + "'and as_location_id ='" + usqlre.locationId + "'"+ areaRoutCondition + " or as_ap_id=1 order by label asc";
                }
            }
            else if (SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && !AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type,as_tin as tin, as_active as active,as_account_code as accountCode,as_mob as mobile ,as_disc_per as discPer,as_credit_limit as creditLimit,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += "   from acc_subhead  where as_ap_id=4 or as_ap_id=1 order by label asc";
                }
                else
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type ,as_tin,as_active as active,as_account_code as accountCode,as_mob as mobile ,as_disc_per as discPer,as_credit_limit as creditLimit ,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += "   from acc_subhead  where as_ap_id=4 and as_salesman_id = '" + usqlre.gu_acc_id + "' or as_ap_id=1 order by label asc";
                }
            }
            else if (SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && !AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type,as_tin as tin,as_active as active,as_account_code as accountCode,as_mob as mobile,as_disc_per as discPer,as_credit_limit as creditLimit,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += " from acc_subhead  where as_ap_id=4 or as_ap_id=1 order by label asc";
                }
                else
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type ,as_tin,as_active as active,as_account_code as accountCode,as_mob as mobile,as_disc_per as discPer,as_credit_limit as creditLimit ,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += "  from acc_subhead  where as_ap_id=4 and as_salesman_id = '" + usqlre.gu_acc_id + "'and as_location_id ='" + usqlre.locationId + "' or as_ap_id=1 order by label asc";
                }
            }
            else if (SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type,as_tin as tin, as_active as active,as_account_code as accountCode,as_mob as mobile  ,as_disc_per as discPer,as_credit_limit as creditLimit,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += "  from acc_subhead  where as_ap_id=4 or as_ap_id=1 order by label asc";
                }
                else
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type ,as_tin, as_active as active,as_account_code as accountCode ,as_mob as mobile ,as_disc_per as discPer,as_credit_limit as creditLimit,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += " from acc_subhead  where as_ap_id=4 and as_salesman_id = '" + usqlre.gu_acc_id + "'"+ areaRoutCondition + " or as_ap_id=1 order by label asc";
                }
            }
            else if (!SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type,as_tin as tin, as_active as active,as_account_code as accountCode ,as_mob as mobile ,as_disc_per as discPer,as_credit_limit as creditLimit ,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += " from acc_subhead  where as_ap_id=4 or as_ap_id=1 order by label asc";
                }
                else
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type ,as_tin, as_active as active,as_account_code as accountCode,as_mob as mobile,as_disc_per as discPer,as_credit_limit as creditLimit ,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += "  from acc_subhead  where as_ap_id=4 and as_location_id ='" + usqlre.locationId + "'"+ areaRoutCondition + " or as_ap_id=1 order by label asc";
                }
            }
            else if (!SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && !AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type,as_tin as tin , as_active as active,as_account_code as accountCode,as_mob as mobile,as_disc_per as discPer,as_credit_limit as creditLimit ,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += "   from acc_subhead  where as_ap_id=4 or as_ap_id=1 order by label asc";
                }
                else
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type ,as_tin, as_active as active,as_account_code as accountCode ,as_mob as mobile ,as_disc_per as discPer,as_credit_limit as creditLimit ,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += " from acc_subhead  where as_ap_id=4 and as_location_id ='" + usqlre.locationId + "' or as_ap_id=1 order by label asc";
                }
            }
            else if (!SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && AREAORROUTE)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type,as_tin as tin, as_active as active,as_account_code as accountCode,as_mob as mobile,as_disc_per as discPer,as_credit_limit as creditLimit ,as_latitude as latitude, as_longitude as longitude";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += " from acc_subhead  where as_ap_id=4 or as_ap_id=1 order by label asc";
                }
                else
                {
                    sql = "SELECT as_name as label,as_id as value,as_rate_type ,as_tin, as_active as active,as_account_code as accountCode,as_mob as mobile ,as_disc_per as discPer,as_credit_limit as creditLimit ,as_latitude as latitude, as_longitude as longitude ";
                    if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                    {
                        sql += ", as_discount_applicable as discApplicable";
                    }
                    sql += "   from acc_subhead  where as_ap_id=4 " + areaRoutCondition + " or as_ap_id=1 order by label asc";
                }
            }
            else
            {
                sql = "SELECT as_name as label,as_id as value, as_rate_type,as_tin as tin, as_active as active,as_account_code as accountCode,as_mob as mobile,as_disc_per as discPer,as_credit_limit as creditLimit ,as_latitude as latitude, as_longitude as longitude ";
                if (usqlre.get_android_settings("ENABLE CUSTOMERWISE DISCPER"))
                {
                    sql += ", as_discount_applicable as discApplicable";
                }
                sql += "  from acc_subhead where as_ap_id=4 or as_ap_id=1 order by label asc";
            }

            DataTable customer = usqlre.dbReaderFill(sql);
            hash.Add("customer", customer);

            if (SHOWSALESMANCODE)
            {
                sql = "SELECT as_account_code as label,as_id as value, as_active as active from acc_subhead where as_ap_id=14";
            }
            else if (SHOWSALESMANWITHCODE)
            {
                sql = "SELECT CONCAT(as_account_code,'-',as_name) as label,as_id as value, as_active as active from acc_subhead where as_ap_id=14";
            }
            else
            {
                sql = "SELECT as_name as label,as_id as value, as_active as active from acc_subhead where as_ap_id=14";
            }
            DataTable salesman = usqlre.dbReaderFill(sql);
            hash.Add("salesman", salesman);

            sql = "SELECT area_name as label,area_id as value from acc_area";
            DataTable area = usqlre.dbReaderFill(sql);
            hash.Add("area", area);

            sql = "SELECT dct_name as label,dct_id as value from inv_district";
            DataTable district = usqlre.dbReaderFill(sql);
            hash.Add("district", district);

            sql = "SELECT str_name as label,str_id as value from inv_sales_type_reg";
            DataTable salesType = usqlre.dbReaderFill(sql);
            hash.Add("salesType", salesType);

            sql = "SELECT as_name as label,as_id as value from acc_subhead where as_ap_id='1' or as_ap_id='2'";
            DataTable cash = usqlre.dbReaderFill(sql);
            hash.Add("cash", cash);

            sql = "SELECT * from gnl_company";
            DataTable company = usqlre.dbReaderFill(sql);
            hash.Add("company", company);

            sql = "SELECT * from inv_sales_type_reg";
            DataTable allSalesType = usqlre.dbReaderFill(sql);
            hash.Add("allSalesType", allSalesType);

            sql = "SELECT rt_id as value, rt_name_display as label, rt_name FROM inv_rate_type ORDER BY rt_order";
            DataTable allRateType = usqlre.dbReaderFill(sql);
            hash.Add("allRateType", allRateType);

            sql = "select * from android_settings";
            DataTable android = usqlre.dbReaderFill(sql);
            hash.Add("android", android);


            sql = "select r_id as value,r_name as label from inv_rout_reg";
            DataTable allRoutes = usqlre.dbReaderFill(sql);
            hash.Add("allRoutes", allRoutes);

            sql = "select am_as_name as label,ap_m_id as value from android_permission_menu inner join android_menu on ap_m_id = am_id  where am_other=1 and android_permission_menu.ap_gu_user_id=" + usqlre.userId + " and android_permission_menu.ap_other_status=1";
            DataTable permissionsdt = usqlre.dbReaderFill(sql);
            hash.Add("permissions", permissionsdt);

            sql = "select gu_user_id as value,gu_name as label, ur_id as roleId,ur_name as roleName from gnl_users left join gnl_user_roles on ur_id= gu_ur_id";
            DataTable gnl_usersdt = usqlre.dbReaderFill(sql);
            hash.Add("gnl_users", gnl_usersdt);

            sql = "SELECT as_name as label,as_id as value from acc_subhead where as_ap_id='2'";
            DataTable bank = usqlre.dbReaderFill(sql);
            hash.Add("bank", bank);


            sql = @"EXEC [dbo].[Sp_erp_mobile] @StatementType = 'import_all'";
            DataSet ds = usqlre.dbreadDataset(sql);


            hash.Add("company_", ds.Tables[0]);
            hash.Add("receivingcondition", ds.Tables[1]);
            hash.Add("servicetype", ds.Tables[2]);
            hash.Add("complaints", ds.Tables[3]);
            hash.Add("fixtype", ds.Tables[4]);
            hash.Add("workorderstatus", ds.Tables[5]);
            hash.Add("color", ds.Tables[6]);
            hash.Add("collecteditems", ds.Tables[7]);
            hash.Add("deliverystatus", ds.Tables[8]);
            hash.Add("formControls", ds.Tables[9]);
            hash.Add("lenditems", ds.Tables[10]);

            usqlre.close();
            return Ok(ReportModelContext.searializeDt(hash));
        }

        [HttpGet("testapi")]
        public IActionResult testapi(string apiname)
        {

            var messag = new { message = "Ok" };
            return Ok(messag);
        }
        

        [HttpPost("search-brand")]
        public async Task<IActionResult> searchBrand([FromBody] Params model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                String sql = "";
                if (model.Value == "")
                    sql = "SELECT top 20 bra_name as label,bra_id as value from inv_brand";
                else
                    sql = "SELECT bra_name as label,bra_id as value from inv_brand where bra_name like '%" + model.Value + "%'";

                DataTable products_codes = usqlre.dbReaderFill(sql);
                return Ok(ReportModelContext.searializeDt(products_codes));
            }
            catch (Exception)
            {
                return Ok(new { status = "false"});
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }
           
        }

        [HttpPost("search-category")]
        public async Task<IActionResult> searchCategory([FromBody] Params model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                String sql = "";
                if (model.Value == "")
                    sql = "SELECT top 20 c_name as label,c_id as value from inv_category where c_name !=''";
                else
                    sql = "SELECT c_name as label,c_id as value from inv_category where c_name like '%" + model.Value + "%'";

                DataTable products_codes = usqlre.dbReaderFill(sql);
                return Ok(ReportModelContext.searializeDt(products_codes));
            }
            catch (Exception e)
            {
                return Ok(new { status = false});
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }
            
        }

        [HttpPost("search-salesman")]
        public async Task<IActionResult> searchSalesman([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "";
            if (model.Value == "")
                sql = "SELECT top 20 as_name as label,as_id as value from acc_subhead where as_ap_id=14";
            else
                sql = "SELECT as_name as label,as_id as value from acc_subhead where as_name like '%" + model.Value + "%' and as_ap_id=14";

            DataTable salesman = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(salesman));
        }

        [HttpPost("search-customer")]
        public async Task<IActionResult> searchCustomer([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "";

            bool LOCATIONWISECUSTOMER = false;
            bool SALESMANWISECUSTOMER = false;
           // bool AREAWISECUSTOMER = false;
            bool AREAORROUTE = false;

            bool CUSTOMERSEARCHVISITINDICATOR = false;
            DataTable salesman = new DataTable();
            string[] areaList = usqlre.areaId.Split(",");
            string areaCondtn = "";
            string[] routList = usqlre.routeId.Split(",");
            string routCondtn = "";
            string areaRoutCondition = "";


            if (usqlre.get_android_settings("AREAWISE LOGIN"))
            {
                //AREAWISECUSTOMER = true;
                AREAORROUTE = true;
                if (areaList.Length > 0)
                {
                    for (int i = 0; i < areaList.Length; i++)
                    {
                        if (Convert.ToInt32(areaList[i]) > 0)
                        {
                            if (i == 0)
                            {
                                areaCondtn = "and as_area_id= " + areaList[i] + "";
                            }
                            else
                            {
                                areaCondtn = areaCondtn + " or as_area_id= " + areaList[i] + "";
                            }
                        }

                    }
                }
                areaRoutCondition = areaCondtn;
               
            }
            else if (usqlre.get_android_settings("ROUTEWISE LOGIN"))
            {
                //ROUTWISECUSTOMER = true;
                AREAORROUTE = true;
                if (routList.Length > 0)
                {
                    for (int i = 0; i < routList.Length; i++)
                    {
                        if (Convert.ToInt32(routList[i]) > 0)
                        {
                            if (i == 0)
                            {
                                routCondtn = "and as_rout_id= " + routList[i] + "";
                            }
                            else
                            {
                                routCondtn = routCondtn + " or as_rout_id= " + routList[i] + "";
                            }
                        }

                    }
                }
                areaRoutCondition = routCondtn;
              
            }

            int length = model.searchLength > 0 ? model.searchLength : 20;

            if (usqlre.get_android_settings("SALESMANWISECUSTOMER"))
            {
                SALESMANWISECUSTOMER = true;
            }
            if (usqlre.get_android_settings("LOCATION WISE CUSTOMER"))
            {
                LOCATIONWISECUSTOMER = true;
            }
            if (usqlre.get_android_settings("CUSTOMER SEARCH VISIT INDICATOR"))
            {
                CUSTOMERSEARCHVISITINDICATOR = true;
            }



            if(!model.advanceSearch)
            {
                if (SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_ap_id = 4 or as_ap_id=1 and as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where (as_ap_id=4 or as_ap_id=1) and as_active=1 and as_salesman_id = '" + usqlre.gu_acc_id + "'and as_location_id ='" + usqlre.locationId + "'" + areaRoutCondition + " ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 and as_salesman_id = '" + usqlre.gu_acc_id + "'and as_location_id ='" + usqlre.locationId + "'" + areaRoutCondition + " or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                }
                else if (SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && !AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_ap_id = 4 or as_ap_id=1 and as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where (as_ap_id=4 or as_ap_id=1) and as_active=1 and as_salesman_id = '" + usqlre.gu_acc_id + "' ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 and as_salesman_id = '" + usqlre.gu_acc_id + "' or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                }
                else if (SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && !AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_ap_id = 4 or as_ap_id=1 and as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where (as_ap_id = 4 or as_ap_id=1) and as_active=1 and as_salesman_id = '" + usqlre.gu_acc_id + "'and as_location_id ='" + usqlre.locationId + "' ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 and as_salesman_id = '" + usqlre.gu_acc_id + "'and as_location_id ='" + usqlre.locationId + "' or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                }
                else if (SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_ap_id = 4 or as_ap_id=1 and as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where (as_ap_id = 4 or as_ap_id=1) and as_active=1 and as_salesman_id = '" + usqlre.gu_acc_id + "'" + areaRoutCondition + " ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 and as_salesman_id = '" + usqlre.gu_acc_id + "'" + areaRoutCondition + " or as_ap_id=1 OR as_mob like '" + model.Value + "%'  ORDER BY as_name";
                    }
                }
                else if (!SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_ap_id = 4 or as_ap_id=1 and as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where (as_ap_id = 4 or as_ap_id=1) and as_active=1 and as_location_id ='" + usqlre.locationId + "'" + areaRoutCondition + " ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 and as_location_id ='" + usqlre.locationId + "'" + areaRoutCondition + " or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                }
                else if (!SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && !AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_ap_id = 4 or as_ap_id=1 ORDER BY as_name and as_active=1";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where (as_ap_id = 4 or as_ap_id=1) and as_active=1 and as_location_id ='" + usqlre.locationId + "' ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 and as_location_id ='" + usqlre.locationId + "' or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                }
                else if (!SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_ap_id = 4 or as_ap_id=1 and as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and (as_ap_id=4 or as_ap_id=1) OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where (as_ap_id = 4 or as_ap_id=1)  and as_active=1 " + areaRoutCondition + " ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 " + areaRoutCondition + " or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                    }
                }
                else
                {
                    if (model.Value == "")
                        sql = "SELECT top " + length + " as_name as label,as_id as value, as_rate_type,as_tin as tin from acc_subhead where (as_ap_id=4 or as_ap_id=1) and as_active=1 ORDER BY as_name";
                    else
                        sql = "SELECT top " + length + " as_name as label,as_id as value, as_rate_type,as_tin as tin from acc_subhead where as_name like '" + model.Value + "%' and as_active=1 and as_ap_id=4 or as_ap_id=1 OR as_mob like '" + model.Value + "%' ORDER BY as_name";
                }
            }
            else
            {
                if (model.Value == "")
                    sql = "SELECT top " + length + " as_name as label,as_id as value, as_rate_type,as_tin as tin from acc_subhead where (as_ap_id=4 or as_ap_id=1) and as_active=1 ORDER BY as_name";
                else
                    sql = "SELECT top " + length + " as_name as label,as_id as value, as_rate_type,as_tin as tin from acc_subhead where as_name like '" + model.Value + "%' and as_active=1 and (as_ap_id=4 or as_ap_id=1) OR as_mob like '" + model.Value + "%' ORDER BY as_name";
            }

            salesman = usqlre.dbReaderFill(sql);

            if (CUSTOMERSEARCHVISITINDICATOR)
            {
                if(model.date != null)
                {
                    salesman.Columns.Add("isvisited");
                    for (int i = 0; i < salesman.Rows.Count; i++)
                    {
                        int cnt = 0;
                        string a = model.date.Trim();
                        string sql1 = "select count(sc_id) as count from shop_check_in where sc_date='" + a + "' and sc_shop_id=" + salesman.Rows[i]["value"] + "";
                        DataTable dtcnt = usqlre.dbReaderFill(sql1);
                        if (dtcnt.Rows.Count > 0)
                        {
                            if (Convert.ToInt32(dtcnt.Rows[0]["count"]) >= 1)
                                salesman.Rows[i]["isvisited"] = "1";
                            else
                                salesman.Rows[i]["isvisited"] = "0";
                        }
                    }
                }
                
               
               
            }

            usqlre.close();
            return Ok(ReportModelContext.searializeDt(salesman));
        }


        [HttpPost("search-ledger")]
        public async Task<IActionResult> searchLedger([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "";

            bool LOCATIONWISECUSTOMER = false;
            bool SALESMANWISECUSTOMER = false;
            // bool AREAWISECUSTOMER = false;
            bool AREAORROUTE = false;

            string[] areaList = usqlre.areaId.Split(",");
            string areaCondtn = "";
            string[] routList = usqlre.routeId.Split(",");
            string routCondtn = "";
            string areaRoutCondition = "";
           
            int length = model.searchLength > 0 ? model.searchLength : 20;
            if (usqlre.get_android_settings("SALESMANWISECUSTOMER"))
            {
                SALESMANWISECUSTOMER = true;
            }
            if (usqlre.get_android_settings("LOCATION WISE CUSTOMER"))
            {
                LOCATIONWISECUSTOMER = true;
            }
            if (usqlre.get_android_settings("AREAWISE LOGIN"))
            {
                // AREAWISECUSTOMER = true;
                AREAORROUTE = true;
                if (areaList.Length > 0)
                {

                    for (int i = 0; i < areaList.Length; i++)
                    {
                        if (i == 0)
                        {
                            areaCondtn = " and as_area_id= " + areaList[i] + "";
                        }
                        else
                        {
                            areaCondtn = areaCondtn + " or as_area_id= " + areaList[i] + "";
                        }
                    }
                }
            }
            else if (usqlre.get_android_settings("ROUTEWISE LOGIN"))
            {
                //ROUTWISECUSTOMER = true;
                AREAORROUTE = true;
                if (routList.Length > 0)
                {
                    for (int i = 0; i < routList.Length; i++)
                    {
                        if (Convert.ToInt32(routList[i]) > 0)
                        {
                            if (i == 0)
                            {
                                routCondtn = "and as_rout_id= " + routList[i] + "";
                            }
                            else
                            {
                                routCondtn = routCondtn + " or as_rout_id= " + routList[i] + "";
                            }
                        }

                    }
                }
                areaRoutCondition = routCondtn;

            }
            if (!model.advanceSearch)
            {
                if (SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '%" + model.Value + "%' and as_active=1 ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_salesman_id = '" + usqlre.gu_acc_id + "'and as_location_id ='" + usqlre.locationId + "'" + areaRoutCondition + " and as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '%" + model.Value + "%' and as_salesman_id = '" + usqlre.gu_acc_id + "'and as_location_id ='" + usqlre.locationId + "'" + areaRoutCondition + " and as_active=1 ORDER BY as_name";
                    }
                }
                else if (SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && !AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '%" + model.Value + "%' and as_active=1 ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_salesman_id = '" + usqlre.gu_acc_id + "' and as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '%" + model.Value + "%' and as_salesman_id = '" + usqlre.gu_acc_id + "' and as_active=1 ORDER BY as_name";
                    }
                }
                else if (SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && !AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '%" + model.Value + "%' ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_salesman_id = '" + usqlre.gu_acc_id + "'and as_location_id ='" + usqlre.locationId + "' ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_name like '%" + model.Value + "%' and as_salesman_id = '" + usqlre.gu_acc_id + "'and as_location_id ='" + usqlre.locationId + "' ORDER BY as_name";
                    }
                }
                else if (SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '%" + model.Value + "%' ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where  as_salesman_id = '" + usqlre.gu_acc_id + "'" + areaRoutCondition + " ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_name like '%" + model.Value + "%' and as_salesman_id = '" + usqlre.gu_acc_id + "'" + areaRoutCondition + " ORDER BY as_name";
                    }
                }
                else if (!SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '%" + model.Value + "%' and as_active=1 ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_location_id ='" + usqlre.locationId + "'" + areaRoutCondition + " and as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '%" + model.Value + "%' and as_location_id ='" + usqlre.locationId + "'" + areaRoutCondition + " and as_active=1 ORDER BY as_name";
                    }
                }
                else if (!SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER && !AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '%" + model.Value + "%' and as_active=1 ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_location_id ='" + usqlre.locationId + "' and as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '%" + model.Value + "%' and as_location_id ='" + usqlre.locationId + "' and as_active=1 ORDER BY as_name";
                    }
                }
                else if (!SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER && AREAORROUTE)
                {
                    if (usqlre.user_role == "ADMIN")
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_active=1 ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '" + model.Value + "%' and as_active=1 ORDER BY as_name";
                    }
                    else
                    {
                        if (model.Value == "")
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead where as_active=1" + areaRoutCondition + " ORDER BY as_name";
                        else
                            sql = "SELECT top " + length + " as_name as label,as_id as value,as_rate_type,as_tin as tin from acc_subhead  where as_name like '%" + model.Value + "%'" + areaRoutCondition + " and as_active=1 ORDER BY as_name";
                    }
                }
                else
                {
                    if (model.Value == "")
                        sql = "SELECT top " + length + " as_name as label,as_id as value, as_rate_type,as_tin as tin from acc_subhead where as_active=1 ORDER BY as_name";
                    else
                        sql = "SELECT top " + length + " as_name as label,as_id as value, as_rate_type,as_tin as tin from acc_subhead where as_name like '%" + model.Value + "%' and as_active=1 ORDER BY as_name";
                }
            }
            else
            {
                if (model.Value == "")
                    sql = "SELECT top " + length + " as_name as label,as_id as value, as_rate_type,as_tin as tin from acc_subhead where as_active=1 ORDER BY as_name";
                else
                    sql = "SELECT top " + length + " as_name as label,as_id as value, as_rate_type,as_tin as tin from acc_subhead where as_name like '%" + model.Value + "%' and as_active=1 ORDER BY as_name";
            }

            DataTable salesman = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(salesman));
        }

        [HttpPost("search-ledger-name")]
        public async Task<IActionResult> searchLedgerName([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "";

            bool SALESMANWISECUSTOMER = false;
            if (usqlre.get_android_settings("SALESMANWISECUSTOMER"))
            {
                SALESMANWISECUSTOMER = true;
            }

            if (SALESMANWISECUSTOMER)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    if (model.Value == "")
                    {
                        sql = "SELECT top 20 max(as_name) as label,as_id as value from acc_subhead left join acc_account_transactions on as_id=at_as_id group by as_id";
                    }
                    else
                    {
                        sql = "SELECT max(as_name) as label,as_id as value from acc_subhead left join acc_account_transactions on as_id=at_as_id where as_name like '%" + model.Value + "%' group by as_id";
                    }

                }
                else {
                    if (model.Value == "")
                    {
                        sql = "SELECT top 20 max(as_name) as label,as_id as value from acc_subhead left join acc_account_transactions on as_id=at_as_id where as_salesman_id = '" + usqlre.gu_acc_id + "' group by as_id";
                    }
                    else
                    {
                        sql = "SELECT max(as_name) as label,as_id as value from acc_subhead left join acc_account_transactions on as_id=at_as_id where as_name like '%" + model.Value + "%' and as_salesman_id = '"+usqlre.gu_acc_id + "' group by as_id";
                    }

                }
            }
            else
            {
                if (model.Value == "")
                {
                    sql = "SELECT top 20 max(as_name) as label,as_id as value from acc_subhead left join acc_account_transactions on as_id=at_as_id group by as_id";
                }
                else
                {
                    sql = "SELECT max(as_name) as label,as_id as value from acc_subhead left join acc_account_transactions on as_id=at_as_id where as_name like '%" + model.Value + "%' group by as_id";
                }
            }
            
            DataTable ledgers = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(ledgers));
        }

        [HttpPost("search-group-name")]
        public async Task<IActionResult> searchGroupName([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "";
            if (model.Value == "")
            {
                sql = "SELECT top 20 ap_name as label,cast(ap_id as int) as value from acc_parent";
            }
            else
            {
                sql = "SELECT ap_name as label,cast(ap_id as int) as value from acc_parent where ap_name like '%" + model.Value + "%'";
            }
            DataTable ledgers = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(ledgers));
        }

        [HttpPost("search-routes")]
        public async Task<IActionResult> searchRoutes([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "";
            if (model.Value == "")
            {
                sql = "SELECT top 20 r_name as label,cast(r_id as int) as value from inv_rout_reg";
            }
            else
            {
                sql = "SELECT r_name as label,cast(r_id as int) as value from inv_rout_reg where r_name like '%" + model.Value + "%'";
            }
            DataTable ledgers = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(ledgers));
        }
        //[HttpGet("search-shop-name/{id}")]
        //public string getShopDetails(int id)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);
        //    string sql = "select as_name,as_ap_id,as_add1,as_mob,as_tin,ISNULL(as_latitude,'') AS as_latitude,ISNULL(as_longitude,'') AS as_longitude from acc_subhead where as_id = "+id+"";
        //    DataTable dt = usqlre.dbReaderFill(sql);
        //    usqlre.close();
        //    return ReportModelContext.searializeDt(dt);
        //}


        [HttpPost("search-shop-current-root")]
        public async Task<IActionResult> searchShopCurrentRoot([FromBody] ShopRoute model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                bool SALESMANWISECUSTOMER = false;
                if (usqlre.get_android_settings("SALESMANWISECUSTOMER"))
                {
                    SALESMANWISECUSTOMER = true;
                }

                String sql = "";
                if (SALESMANWISECUSTOMER)
                    sql = "select isnull(a.as_latitude,'') as as_latitude,isnull(a.as_longitude,'') as as_longitude,a.as_name,a.as_id,a.as_add1,a.as_mob,a.as_tin,a.as_rout_id from (SELECT max(as_latitude) as as_latitude,max(as_longitude) as as_longitude,max(as_name) as as_name,max(as_id) as as_id,max(as_add1) as as_add1,max(as_mob) as as_mob,max(as_tin) as as_tin,max(as_rout_id) as as_rout_id from acc_subhead left join acc_account_transactions on as_id=at_as_id where as_rout_id = " + model.routeId + " and as_salesman_id = '" + usqlre.gu_acc_id + "' group by as_id) a where as_name like '%" + model.search + "%'";
                else
                    sql = "select isnull(a.as_latitude,'') as as_latitude,isnull(a.as_longitude, '') as as_longitude,a.as_name,a.as_id,a.as_add1,a.as_mob,a.as_tin,a.as_rout_id from(SELECT max(as_latitude) as as_latitude,max(as_longitude) as as_longitude,max(as_name) as as_name,max(as_id) as as_id,max(as_add1) as as_add1,max(as_mob) as as_mob,max(as_tin) as as_tin,max(as_rout_id) as as_rout_id from acc_subhead left join acc_account_transactions on as_id=at_as_id where as_rout_id = " + model.routeId + " group by as_id) a where as_name like '%" + model.search + "%'";


                DataTable shoproots = usqlre.dbReaderFill(sql);
                return Ok(ReportModelContext.searializeDt(shoproots));
            }
            catch (Exception e)
            {
                return Ok(new { status  =false});
            }
            finally 
            {
                if (usqlre != null)
                    usqlre.close();
            }
        }

        [HttpGet("user-routes")]
        public async Task<IActionResult> UserBasedAssignedRoutes()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string user_id = usqlre.userId;
            //String sql = "select max(r_name) as r_name,max(r_starting_place) as r_starting_place,max(r_ending_place) as r_ending_place,max(r_id) as r_id,max(r_approx_distance) as r_approx_distance,max(as_id) as as_id,max(as_name) as as_name from inv_rout_reg inner join acc_subhead on r_id = as_rout_id inner join inv_route_allocation on r_id = ira_route_id  where ira_user_id = " + user_id + " and as_rout_id > 0 group by r_id";
            //sql = "select TOP 10 as_id,as_name from acc_subhead";
            string sql = "";
            sql = "select r_id,r_name,r_starting_place,r_ending_place,r_approx_distance from inv_rout_reg";
            DataTable routesdt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(routesdt));
        }

        [HttpPost("get-shop-current-latlng")]
        public async Task<IActionResult> getShopCurrentLatlng([FromBody] ShopLocation model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                String sql = "";
                //sql = "select max(a.as_id) as as_id,max(as_name) as as_name,max(as_add1) as as_add1,max(as_mob) as as_mob,max(as_tin) as as_tin,max(as_latitude) as as_latitude,max(as_longitude) as as_longitude,sum(at_Dr)-sum(at_Cr) as Balance from (SELECT as_id,as_name,as_add1,as_mob,as_tin,as_latitude,as_longitude, ( 3959 * acos( cos( radians(" + model.latitude + ") ) * cos( radians( as_latitude ) ) * " +
                 // " cos(radians(as_longitude) - radians(" + model.longitude + ")) + sin(radians(" + model.latitude + ")) * " +
                  // " sin(radians(as_latitude)) ) ) AS distance,'true' as status FROM acc_subhead where as_rout_id = '" + model.routeId + "') a where  distance < 0.0093205679 ";

                string latitude = "0";
                if (model.latitude != null)
                {
                    if (model.latitude.Length > 0)
                        latitude = model.latitude;
                }

                if (latitude.Length > 12)
                    latitude = latitude.Substring(0, 12);

                string longitude = "0";

                if (model.longitude != null)
                {
                    if (model.longitude.Length > 0)
                    {
                        longitude = model.longitude;
                    }
                }

                if (longitude.Length > 12)
                    longitude = longitude.Substring(0, 12);

                bool SALESMANWISECUSTOMER = false;
                bool NONROUTECUSTOMER = false;
                if (usqlre.get_android_settings("SALESMANWISECUSTOMER"))
                {
                    SALESMANWISECUSTOMER = true;
                }
                if (usqlre.get_android_settings("NON ROUTE CUSTOMER"))
                {
                    NONROUTECUSTOMER = true;
                }

                if (SALESMANWISECUSTOMER && NONROUTECUSTOMER)
                    sql = "select max(a.as_id) as as_id,max(as_name) as as_name,max(as_add1) as as_add1,max(as_mob) as as_mob,max(as_tin) as as_tin,max(as_latitude) as as_latitude,max(as_longitude) as as_longitude,isnull(sum(at_Dr)-sum(at_Cr),0) as Balance,max(status) as status from (SELECT as_id,as_name,as_add1,as_mob,as_tin,isnull(as_latitude,0) as as_latitude,isnull(as_longitude,0) as as_longitude, ( 3959 * acos( cos( radians(" + latitude + ") ) * cos( radians( as_latitude ) ) *  cos(radians(as_longitude) - radians(" + longitude + ")) + sin(radians(" + latitude + ")) *  sin(radians(as_latitude)) ) ) AS distance,'true' as status FROM acc_subhead where  as_salesman_id = '" + usqlre.gu_acc_id + "') a left join acc_account_transactions b on a.as_id=b.at_as_id    where  a.distance < 0.0186411 group by as_id";
                else if(!SALESMANWISECUSTOMER && NONROUTECUSTOMER)
                    sql = "select max(a.as_id) as as_id,max(as_name) as as_name,max(as_add1) as as_add1,max(as_mob) as as_mob,max(as_tin) as as_tin,max(as_latitude) as as_latitude,max(as_longitude) as as_longitude,isnull(sum(at_Dr)-sum(at_Cr),0) as Balance,max(status) as status from (SELECT as_id,as_name,as_add1,as_mob,as_tin,isnull(as_latitude,0) as as_latitude,isnull(as_longitude,0) as as_longitude, ( 3959 * acos( cos( radians(" + latitude + ") ) * cos( radians( as_latitude ) ) *  cos(radians(as_longitude) - radians(" + longitude + ")) + sin(radians(" + latitude + ")) *  sin(radians(as_latitude)) ) ) AS distance,'true' as status FROM acc_subhead) a left join acc_account_transactions b on a.as_id=b.at_as_id    where  a.distance < 0.0186411 group by as_id";
                else if (SALESMANWISECUSTOMER && !NONROUTECUSTOMER)
                    sql = "select max(a.as_id) as as_id,max(as_name) as as_name,max(as_add1) as as_add1,max(as_mob) as as_mob,max(as_tin) as as_tin,max(as_latitude) as as_latitude,max(as_longitude) as as_longitude,isnull(sum(at_Dr)-sum(at_Cr),0) as Balance,max(status) as status from (SELECT as_id,as_name,as_add1,as_mob,as_tin,isnull(as_latitude,0) as as_latitude,isnull(as_longitude,0) as as_longitude, ( 3959 * acos( cos( radians(" + latitude + ") ) * cos( radians( as_latitude ) ) *  cos(radians(as_longitude) - radians(" + longitude + ")) + sin(radians(" + latitude + ")) *  sin(radians(as_latitude)) ) ) AS distance,'true' as status FROM acc_subhead where as_rout_id = '" + model.routeId + "' and as_salesman_id = '" + usqlre.gu_acc_id + "') a left join acc_account_transactions b on a.as_id=b.at_as_id    where  a.distance < 0.0186411 group by as_id";
                else
                    sql = "select max(a.as_id) as as_id,max(as_name) as as_name,max(as_add1) as as_add1,max(as_mob) as as_mob,max(as_tin) as as_tin,max(as_latitude) as as_latitude,max(as_longitude) as as_longitude,isnull(sum(at_Dr)-sum(at_Cr),0) as Balance,max(status) as status from (SELECT as_id,as_name,as_add1,as_mob,as_tin,isnull(as_latitude,0) as as_latitude,isnull(as_longitude,0) as as_longitude, ( 3959 * acos( cos( radians(" + latitude + ") ) * cos( radians( as_latitude ) ) *  cos(radians(as_longitude) - radians(" + longitude + ")) + sin(radians(" + latitude + ")) *  sin(radians(as_latitude)) ) ) AS distance,'true' as status FROM acc_subhead where as_rout_id = '" + model.routeId + "') a left join acc_account_transactions b on a.as_id=b.at_as_id    where  a.distance < 0.0186411 group by as_id";
                DataTable shoproots = usqlre.dbReaderFill(sql);
                return Ok(ReportModelContext.searializeDt(shoproots));
            }
            catch (Exception e)
            {
                return null;
            }
            finally {
                if (usqlre != null)
                    usqlre.close();
            }   
        }
        //[HttpPost("search-shop-current-root")]
        //public async Task<IActionResult> searchShopCurrentroot([FromBody] SearchShop model)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);
        //    String sql = "";
        //    sql = "select max(a.as_id) as as_id,max(as_name) as as_name,max(as_add1) as as_add1,max(as_mob) as as_mob,max(as_tin) as as_tin,max(as_latitude) as as_latitude,max(as_longitude) as as_longitude,sum(at_Dr)-sum(at_Cr) as Balance from (SELECT as_id,as_name,as_add1,as_mob,as_tin,as_latitude,as_longitude, ( 3959 * acos( cos( radians(" + model.latitude + ") ) * cos( radians( as_latitude ) ) * " +
        //          " cos(radians(as_longitude) - radians(" + model.longitude + ")) + sin(radians(" + model.latitude + ")) * " +
        //           " sin(radians(as_latitude)) ) ) AS distance,'true' as status FROM acc_subhead where as_rout_id = '" + model.routeId + "') a where  distance < 0.0093205679 ";


        //    sql = "select max(a.as_id) as as_id,max(as_name) as as_name,max(as_add1) as as_add1,max(as_mob) as as_mob,max(as_tin) as as_tin,max(as_latitude) as as_latitude,max(as_longitude) as as_longitude,sum(at_Dr)-sum(at_Cr) as Balance,max(status) as status from (SELECT as_id,as_name,as_add1,as_mob,as_tin,as_latitude,as_longitude, ( 3959 * acos( cos( radians(" + model.latitude + ") ) * cos( radians( as_latitude ) ) *  cos(radians(as_longitude) - radians(" + model.longitude + ")) + sin(radians(" + model.latitude + ")) *  sin(radians(as_latitude)) ) ) AS distance,'true' as status FROM acc_subhead where as_rout_id = '" + model.routeId + "') a inner join acc_account_transactions b on a.as_id=b.at_as_id    where  a.distance < 0.0093205679 group by as_id";




        //    DataTable shoproots = usqlre.dbReaderFill(sql);
        //    usqlre.close();
        //    return Ok(ReportModelContext.searializeDt(shoproots));
        //}


        [HttpPost("shop-check-in")]
        public async Task<IActionResult> shopCheckIn([FromBody] ShopCheckin model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            //String sql = "insert into shop_check_in(sc_date,sc_time,sc_userid,sc_shop_id,sc_latitude,sc_longitude,sc_check_in) values ('"+model.date+"','"+model.time+"','"+usqlre.userId+"','"+model.shopId+"','"+model.latitude+"','"+model.longitude+"',1)";
            //String sql = "insert into shop_check_in(sc_date,sc_time,sc_userid,sc_shop_id,sc_latitude,sc_longitude,sc_check_in) values ('" + model.date + "' ,'" + model.time + "','" + usqlre.userId + "','" + model.shopId + "','" + model.latitude + "','" + model.longitude + "',1)";

            int insertId = 0;
            insertId = usqlre.insertShopChekinIn(model);
            if (insertId > 0)
            {

                DataTable shop_dt = usqlre.dbReaderFill("SELECT as_name as label,cast(as_id as Int) as value,as_rate_type from acc_subhead where as_id='" + model.shopId + "'");
                string pending_check_count = "0";
                string sql = "select count(at_id) as pending_check_count from acc_account_transactions where at_pending_status = 1 and at_form='BANK RECEIPT' and at_id='"+model.shopId+"'";
                DataTable dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        pending_check_count = dt.Rows[0]["pending_check_count"].ToString();
                    }
                }

                string pending_check_amount = "0";
                sql = "select isnull(cast(SUM(at_Dr)-SUM(at_Cr) as nvarchar(Max)),'0') as pending_check_amount from acc_account_transactions where at_pending_status = 1 and at_form='BANK RECEIPT' and at_id='" + model.shopId + "'";
                dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        pending_check_amount = dt.Rows[0]["pending_check_amount"].ToString();
                    }
                }

                string old_balance = "0";
                sql = "select isnull(cast(SUM(at_Dr)-SUM(at_Cr) as nvarchar(Max)),'0') as old_balance from acc_account_transactions where at_as_id = '" + model.shopId+"' and at_pending_status = 0";
                dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        old_balance = dt.Rows[0]["old_balance"].ToString();
                    }
                }

                string label = "";
                string value = "";
                string as_rate_type = "";


                if (shop_dt != null)
                {
                    if (shop_dt.Rows.Count > 0)
                    {
                        label = shop_dt.Rows[0]["label"].ToString();
                        value = shop_dt.Rows[0]["value"].ToString();
                        as_rate_type = shop_dt.Rows[0]["as_rate_type"].ToString();
                    }
                }


                DataTable dt_final = new DataTable();
                dt_final.Clear();
                dt_final.Columns.Add("label");
                dt_final.Columns.Add("value");
                dt_final.Columns.Add("as_rate_type");
                
                dt_final.Columns.Add("pending_check_count");
                dt_final.Columns.Add("pending_check_amount");
                dt_final.Columns.Add("old_balance");

                DataRow row1 = dt_final.NewRow();
                row1["label"] = label;
                row1["value"] = value;
                row1["as_rate_type"] = as_rate_type;

                row1["pending_check_count"] = pending_check_count;
                row1["pending_check_amount"] = pending_check_amount;
                row1["old_balance"] = old_balance;

                dt_final.Rows.Add(row1);



                usqlre.close();
                return Ok(new { status = true, insert_id = insertId , shop = ReportModelContext.searializeDt(dt_final) });
            }
            else
            {
                usqlre.close();
                return Ok(new { status = false });
            }

        }


        [HttpPost("shop-check-out")]
        public async Task<IActionResult> shopCheckOut([FromBody] ShopCheckin model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "update shop_check_in set sc_check_out_time='" + model.time + "',sc_latitude_out='" + model.latitude + "',sc_longitude_out='" + model.longitude + "',sc_check_in=0 where sc_id='" + model.checkinId + "'";

            if (usqlre.dbExecute(sql))
            {
                usqlre.close();
                return Ok(new { status = true });
            }
            else
            {
                usqlre.close();
                return Ok(new { status = false });
            }
        }

        [HttpGet("shop-in-root")]
        public async Task<IActionResult> shopcheckOut(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "select isnull(max(as_latitude),'') as as_latitude,isnull(max(as_longitude),'') as as_longitude,max(as_name) as as_name,max(as_id) as as_id,max(as_add1) as as_add1,max(as_mob) as as_mob,max(as_tin) as astin,sum(at_Dr)-sum(at_Cr) as ob from acc_subhead inner join acc_account_transactions on as_id=at_as_id where as_rout_id=" + id + " group by as_id";
            usqlre.dbExecute(sql);
            usqlre.close();
            return Ok(new { status = true });
        }


        [HttpGet("shop-in-route/{id}")]
        public string shopInRoute(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "SELECT isnull(max(as_latitude),'') as as_latitude,isnull(max(as_longitude),'') as as_longitude,isnull(max(as_add1),'') as add1,isnull(max(as_mob),'') as as_mob from acc_subhead left join acc_account_transactions on as_id=at_as_id where as_rout_id = " + id + " group by as_rout_id";
            DataTable shoproots = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(shoproots);
        }


        [HttpGet("user-route-details/{id}")]
        public string userRouteDetails(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "select count(*) as total_shop,isnull(max(r_name),'') as r_name,isnull(max(r_starting_place),'') as r_starting_place,isnull(max(r_ending_place),'') as r_ending_place,isnull(max(r_id),0) as r_id,isnull(max(r_approx_distance),0) as r_approx_distance,0 as amount_collect from inv_rout_reg left join acc_subhead on r_id = as_rout_id where r_id = " + id + "";
            DataTable shoproots = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(shoproots);
        }


        [HttpGet("get-all-salesman-day")]
        public string getAllSalesmanDay(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "select gu_user_id,gu_name,'phote.png' as photo,'1' as work_status from gnl_users";
            DataTable shoproots = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(shoproots);
        }

        //        [HttpGet("get-account-details/{id}")]
        //        public string getAccountDetails(int id)
        //        {
        //            UserSqlServer usqlre = new UserSqlServer(this);
        //            String sql = @"select acc.as_name,acc.as_ap_id,acc.as_add1,acc.as_mob,acc.as_tin,acc.as_longitude,acc.as_latitude,acc.as_add2,acc.as_add3,ap_name,r_id,r_name,gl_id,gl_name,s.as_name as salesman,acc.as_salesman_id as salesman_id 
        //,acc.as_area_id,area_name from acc_subhead as acc
        //            inner join acc_parent on as_ap_id = ap_id
        //            left join inv_rout_reg on as_rout_id = r_id
        //            left join gnl_location on as_location_id = gl_id
        //			left join acc_area on as_area_id=area_id
        //            left join acc_subhead as s on acc.as_salesman_id = s.as_id where acc.as_id=" + id + "";
        //            DataTable shoproots = usqlre.dbReaderFill(sql);
        //            usqlre.close();
        //            return ReportModelContext.searializeDt(shoproots);
        //        }

        [HttpGet("get-account-details/{id}")]
        public async Task<IActionResult> GetAccountDetails(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = @"select acc.as_id,acc.as_name,acc.as_add1 as address,acc.as_add2 as address2,acc.as_add3 as address3,
                    acc.as_location,gl_name as locationName,acc.as_mob as mobile,
                    acc.as_tin as gstin,acc.as_longitude,acc.as_latitude,acc.as_active,
                    acc.as_ap_id as groupId,acc.as_rout_id as routeId,acc.as_salesman_id as salesmanId,
                    acc.as_area_id,area_name,acc.as_location_id as location,
                    acc.as_pincode as pinCode,acc.as_credit_limit as creditLimit,
                    acc.as_credit_days as creditDays,acc.as_ob_Cr as obCr,acc.as_ob_Dr as obDr,
                    acc.as_state as state,acc.as_state_code as stateCode,
                    acc.as_tcs_status as tcsStatus,acc.as_tcs_status_sales as tcsStatusSales,
                    acc.as_tds_status as tdsStatus
                    from acc_subhead acc
                    left join gnl_location on acc.as_location_id = gl_id
                    left join acc_area on acc.as_area_id = area_id
                    where acc.as_id=" + id;

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();

            if (dt == null || dt.Rows.Count == 0)
                return NotFound(); // optional

            DataRow row = dt.Rows[0];

            var result = new ShopUpdateModel()
            {
                shop_id = id,
                shop_name = row["as_name"].ToString(),
                address = row["address"].ToString(),
                address2 = row["address2"].ToString(),
                address3 = row["address3"].ToString(),
                locationName = row["locationName"].ToString(),
                mobile = row["mobile"].ToString(),
                active = row["as_active"] == DBNull.Value ? 0 : Convert.ToInt32(row["as_active"]),
                pinCode = row["pinCode"] == DBNull.Value ? 0 : Convert.ToInt32(row["pinCode"]),
                creditLimit = row["creditLimit"] == DBNull.Value ? 0 : Convert.ToDecimal(row["creditLimit"]),
                creditDays = row["creditDays"] == DBNull.Value ? 0 : Convert.ToInt32(row["creditDays"]),
                obCr = row["obCr"] == DBNull.Value ? 0 : Convert.ToDecimal(row["obCr"]),
                obDr = row["obDr"] == DBNull.Value ? 0 : Convert.ToDecimal(row["obDr"]),
                latitude = row["as_latitude"].ToString(),
                longitude = row["as_longitude"].ToString(),
                tcsStatus = row["tcsStatus"] == DBNull.Value ? 0 : Convert.ToInt32(row["tcsStatus"]),
                tcsStatusSales = row["tcsStatusSales"] == DBNull.Value ? 0 : Convert.ToInt32(row["tcsStatusSales"]),
                tdsStatus = row["tdsStatus"] == DBNull.Value ? 0 : Convert.ToInt32(row["tdsStatus"]),
                gstin = row["gstin"].ToString(),
                state = row["state"].ToString(),
                stateCode = row["stateCode"].ToString(),
                groupId = Convert.ToInt32(row["groupId"]),
                routeId = Convert.ToInt32(row["routeId"]),
                location = Convert.ToInt32(row["location"]),
                salesmanId = row["salesmanId"] == DBNull.Value ? -1 : Convert.ToInt32(row["salesmanId"]),
                as_area_id = row["as_area_id"] == DBNull.Value ? -1 : Convert.ToInt32(row["as_area_id"]),               
            };

            return Ok(result);
        }


        //[HttpPost("update-account-details")]
        //public async Task<IActionResult> updateShopDetails([FromBody] ShopUpdateModel model)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);
        //    int location = model.location <= 0 ? -1 : model.location;
        //    string sql = "";
        //    //@as_salesman_id = " + model.salesmanId +"
        //    if (model.salesmanId>-1)
        //       sql = "update acc_subhead set as_rout_id='" + model.routeId + "',as_name='" + model.shop_name + "',as_add1='" + model.address + "',as_mob='" + model.mobile + "',as_tin='" + model.gstin + "',as_latitude='" + model.latitude + "',as_longitude='" + model.longitude + "',as_location_id='" + location + "', as_salesman_id=" + model.salesmanId + ",as_area_id=" + model.as_area_id + "  where as_id=" + model.shop_id + "";
        //    else
        //        sql = "update acc_subhead set as_rout_id='" + model.routeId + "',as_name='" + model.shop_name + "',as_add1='" + model.address + "',as_mob='" + model.mobile + "',as_tin='" + model.gstin + "',as_latitude='" + model.latitude + "',as_longitude='" + model.longitude + "',as_location_id='" + location + "',as_area_id="+model.as_area_id+"where as_id=" + model.shop_id + "";
        //    if (usqlre.dbExecute(sql))
        //    {
        //        usqlre.close();
        //        return Ok(new { status = true });
        //    }
        //    else
        //    {
        //        usqlre.close();
        //        return Ok(new { status = false });
        //    }

        //}

        [HttpPost("update-account-details")]
        public async Task<IActionResult> updateShopDetails([FromBody] ShopUpdateModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            int location = model.location <= 0 ? -1 : model.location;

            string sql = "";

            // routeId = 0 means keep existing route
            string routeSql = model.routeId > 0
                ? "as_rout_id='" + model.routeId + "',"
                : "";

            if (model.salesmanId > -1)
            {
                sql = "update acc_subhead set " +
                      routeSql +
                      "as_name='" + model.shop_name + "'," +
                      "as_add1='" + model.address + "'," +
                      "as_mob='" + model.mobile + "'," +
                      "as_tin='" + model.gstin + "'," +
                      "as_latitude='" + model.latitude + "'," +
                      "as_longitude='" + model.longitude + "'," +
                      "as_location_id='" + location + "'," +
                      "as_salesman_id=" + model.salesmanId + "," +
                      "as_area_id=" + model.as_area_id +
                      " where as_id=" + model.shop_id;
            }
            else
            {
                sql = "update acc_subhead set " +
                      routeSql +
                      "as_name='" + model.shop_name + "'," +
                      "as_add1='" + model.address + "'," +
                      "as_mob='" + model.mobile + "'," +
                      "as_tin='" + model.gstin + "'," +
                      "as_latitude='" + model.latitude + "'," +
                      "as_longitude='" + model.longitude + "'," +
                      "as_location_id='" + location + "'," +
                      "as_area_id=" + model.as_area_id +
                      " where as_id=" + model.shop_id;
            }

            if (usqlre.dbExecute(sql))
            {
                usqlre.close();

                return Ok(new
                {
                    status = true
                });
            }
            else
            {
                usqlre.close();

                return Ok(new
                {
                    status = false
                });
            }
        }

        [HttpPost("new-account")]
        public async Task<IActionResult> newShop([FromBody] ShopUpdateModel model)
        {
            UserSqlServer usqlre = null;

            try
            {
                usqlre = new UserSqlServer(this);
                string sql = "";
                int location = model.location <= 0 ? -1 : model.location;
                sql = "DECLARE @return_value int EXEC @return_value =  [dbo].[Sp_acc_reg] @as_tin = '" + model.gstin + "',@as_location_id=" + location + ",@as_rout_id=" + model.routeId + ",@as_mob = '" + model.mobile + "',@as_add1 = '" + model.address + "',@as_name = '" + model.shop_name + "',@as_ap_id=" + model.groupId + ",@as_latitude='" + model.latitude + "',@as_longitude='" + model.longitude + "',@as_salesman_id=" + model.salesmanId + ",@as_area_id=" + model.as_area_id + ",@as_add2='" + model.address2 + "',@as_add3='" + model.address3 + "',@as_pincode=" + model.pinCode + ",@as_state='" + model.state + "',@as_state_code='" + model.stateCode + "',@as_location='" + model.locationName + "',@as_active=" + model.active + ",@as_credit_limit=" + model.creditLimit + ",@as_credit_days=" + model.creditDays + ",@as_ob_Cr=" + model.obCr + ",@as_ob_Dr=" + model.obDr + ",@as_tcs_status_sales=" + model.tcsStatusSales + ",@as_tcs_status=" + model.tcsStatus + ",@as_tds_status=" + model.tdsStatus + ",@as_date='" + DateTime.Now.ToString("yyyy-MM-dd") + "',@StatementType = N'Insert' SELECT 'return' = @return_value ";
                DataTable dt = usqlre.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dt.Rows[0]["return"].ToString()) == 1)
                        {
                            int as_id = 0;
                            sql = "select as_id from acc_subhead where as_name = '" + model.shop_name + "'";
                            dt = usqlre.dbReaderFill(sql);
                            if (dt != null)
                            {
                                if (dt.Rows.Count > 0)
                                    as_id = Convert.ToInt32(dt.Rows[0]["as_id"].ToString());
                            }
                            return Ok(new { status = true, accountId = as_id });
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
        [HttpPost("get-checkin-route")]
        public async Task<IActionResult> getCheckinRoute([FromBody] CheckinRouteModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            int SALESMANWISECUSTOMER = 0;
            if (usqlre.get_android_settings("SALESMANWISECUSTOMER"))
            {
                SALESMANWISECUSTOMER = 1;
            }


            DataSet ds = usqlre.getCheckinRoute(model, SALESMANWISECUSTOMER);

            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            hash.Add("route", ds.Tables["Table"]);
            hash.Add("shops", ds.Tables["Table1"]);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(hash));
        }


        [HttpPost("get-checkin-shop")]
        public async Task<IActionResult> getCheckinShop([FromBody] CheckinRouteModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "select TOP 1 sc_id,sc_shop_id,as_name,as_name as label,cast(as_id as Int) as value,as_rate_type from shop_check_in inner join acc_subhead on sc_shop_id=as_id where sc_userid='" + usqlre.userId + "' and cast(sc_date as date)='" + model.date + "' and sc_check_out_time is null ORDER BY sc_id desc";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }

        [HttpPost("search-account")]
        public async Task<IActionResult> searchAccount([FromBody] Params model)
        {
            string sql = "";
            UserSqlServer usqlre = new UserSqlServer(this);
            bool SALESMANWISECUSTOMER = false;
            bool LOCATIONWISECUSTOMER = false;
            if (usqlre.get_android_settings("SALESMANWISECUSTOMER"))
            {
                SALESMANWISECUSTOMER = true;
            }
            if (usqlre.get_android_settings("LOCATION WISE CUSTOMER"))
            {
                LOCATIONWISECUSTOMER = true;
            }
            if (SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT as_id,as_name from acc_subhead where as_name like '%" + model.Value + "%'";

                }
                else
                {
                    sql = "SELECT as_id,as_name from acc_subhead where as_salesman_id='" + usqlre.gu_acc_id + "' and as_location_id='" + usqlre.locationId + "' and as_name like '%" + model.Value + "%'";
                }
            }
            else if (SALESMANWISECUSTOMER && !LOCATIONWISECUSTOMER)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT as_id,as_name from acc_subhead where as_name like '%" + model.Value + "%'";

                }
                else
                {
                    sql = "SELECT as_id,as_name from acc_subhead where as_salesman_id='" + usqlre.gu_acc_id + "' and as_name like '%" + model.Value + "%'";
                }
            }
            else if (!SALESMANWISECUSTOMER && LOCATIONWISECUSTOMER)
            {
                if (usqlre.user_role == "ADMIN")
                {
                    sql = "SELECT as_id,as_name from acc_subhead where as_name like '%" + model.Value + "%'";

                }
                else
                {
                    sql = "SELECT as_id,as_name from acc_subhead where as_location_id='" + usqlre.locationId + "' and as_name like '%" + model.Value + "%'";
                }
            }
            else
            {
                sql = "SELECT as_id,as_name from acc_subhead where as_name like '%" + model.Value + "%'";
            }
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }


        [HttpPost("salesman-tracking")]
        public async Task<IActionResult> getSalesmanTracking([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "select gu_ur_id,gu_name,isnull(a.rt_id,0) as rt_id from gnl_users left join (select * from route_tracking where cast(rt_date as date) >= '" + model.fromDate + "' and cast(rt_date as date) <= '" + model.toDate + "') a on gu_user_id =  a.rt_salesman  ";
            //sql = "select gu_ur_id,gu_name,isnull(rt_id,0) as rt_id from gnl_users left join route_tracking on gu_user_id = rt_salesman where cast(rt_date as date) >= '"+model.from_date+"' and cast(rt_date as date) <= '"+model.to_date+"'";
            sql = "select max(gu_user_id) as gu_ur_id,max(gu_name) as gu_name,max(status) as status from (select gu_user_id,gu_name,CASE WHEN cast(rt_date as date) = '" + model.todayDate + "' THEN 'true' ELSE 'false' end as status from gnl_users left join route_tracking on gu_user_id = rt_salesman where cast(rt_date as date) >= '" + model.fromDate + "' and cast(rt_date as date) <= '" + model.toDate + "') a group by a.gu_user_id";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }

        [HttpGet("salesman-tracking/{id}")]
        public string getSalesmanTrackingById(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            //String sql = "select as_name,as_ap_id,as_add1,as_mob,as_tin,as_longitude,as_latitude,as_add2,as_add3 from acc_subhead where as_id=" + id + "";
            //DataTable shoproots = usqlre.dbReaderFill(sql);
            DataSet ds = usqlre.getSalesmanTrackingById(id);

            DataTable dt = new DataTable();



            dt.Columns.Add("gu_user_id");
            dt.Columns.Add("gu_name");
            dt.Columns.Add("gu_add");
            dt.Columns.Add("gu_phone");
            dt.Columns.Add("gu_photo");

            dt.Columns.Add("total_collection");
            dt.Columns.Add("total_order_value");
            dt.Columns.Add("total_work_days");
            dt.Columns.Add("customer_attended");
            dt.Columns.Add("distance_covered");
            dt.Columns.Add("time_spent_on_customer");
            dt.Columns.Add("time_spent_on_travel");
            dt.Columns.Add("last_route");
            dt.Columns.Add("last_check_out_shop");
            dt.Columns.Add("current_route");
            dt.Columns.Add("current_check_in");


            DataRow dr = dt.NewRow();
            dr["gu_user_id"] = ds.Tables["Table"].Rows[0]["gu_user_id"].ToString();
            dr["gu_name"] = ds.Tables["Table"].Rows[0]["gu_name"].ToString();
            dr["gu_add"] = ds.Tables["Table"].Rows[0]["gu_add"].ToString();
            dr["gu_phone"] = ds.Tables["Table"].Rows[0]["gu_phone"].ToString();
            dr["gu_photo"] = ds.Tables["Table"].Rows[0]["gu_photo"].ToString();

            if (ds.Tables["Table1"].Rows.Count > 0)
                dr["total_collection"] = ds.Tables["Table1"].Rows[0]["total_collection"].ToString();
            else
                dr["total_collection"] = " ";

            if (ds.Tables["Table2"].Rows.Count > 0)
                dr["total_order_value"] = ds.Tables["Table2"].Rows[0]["total_order_value"].ToString();
            else
                dr["total_order_value"] = " ";

            if (ds.Tables["Table3"].Rows.Count > 0)
                dr["total_work_days"] = ds.Tables["Table3"].Rows[0]["total_work_days"].ToString();
            else
                dr["total_work_days"] = " ";


            if (ds.Tables["Table4"].Rows.Count > 0)
                dr["customer_attended"] = ds.Tables["Table4"].Rows[0]["customer_attended"].ToString();
            else
                dr["customer_attended"] = " ";

            if (ds.Tables["Table6"].Rows.Count > 0)
                dr["time_spent_on_customer"] = ds.Tables["Table6"].Rows[0]["time_spent_on_customer"].ToString();
            else
                dr["time_spent_on_customer"] = " ";

            if (ds.Tables["Table7"].Rows.Count > 0)
                dr["time_spent_on_travel"] = ds.Tables["Table7"].Rows[0]["time_spent_on_travel"].ToString();
            else
                dr["time_spent_on_travel"] = " ";

            if (ds.Tables["Table8"].Rows.Count > 0)
                dr["last_route"] = ds.Tables["Table8"].Rows[0]["last_route"].ToString();
            else
                dr["last_route"] = " ";

            if (ds.Tables["Table9"].Rows.Count > 0)
                dr["last_check_out_shop"] = ds.Tables["Table9"].Rows[0]["last_check_out_shop"].ToString();
            else
                dr["last_check_out_shop"] = " ";


            if (ds.Tables["Table10"].Rows.Count > 0)
                dr["current_route"] = ds.Tables["Table10"].Rows[0]["current_route"].ToString();
            else
                dr["current_route"] = " ";

            if (ds.Tables["Table11"].Rows.Count > 0)
                dr["current_check_in"] = ds.Tables["Table11"].Rows[0]["current_check_in"].ToString();
            else
                dr["current_check_in"] = "";

            dt.Rows.Add(dr);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }


        [HttpPost("sales-tracking-history")]
        public async Task<IActionResult> salesTrackingHistory([FromBody] ReportDatesUselModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "select cast(r_id as Int) as r_id,r_name,cast(rt_date as date) as rt_date,rt_id from route_tracking inner join inv_rout_reg on r_id = rt_route_id where rt_salesman = '" + model.userId + "' and  cast(rt_date as date) >= '" + model.fromDate + "' and cast(rt_date as date) <= '" + model.toDate + "'  order by cast(rt_date as date)";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }

        [HttpGet("salesman-tracking-single/{id}")]
        public string salesmanTrackingSingle(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            DataSet ds = usqlre.salesmanTrackingSingle(id);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            hash.Add("shop", ds.Tables["Table"]);
            hash.Add("locations", ds.Tables["Table1"]);

            usqlre.close();
            return ReportModelContext.searializeDt(hash);
        }

        [HttpPost("ledger-report")]
        public async Task<IActionResult> ledgerReport([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT top 10 as_id,'active' as status from acc_subhead where as_ap_id = 14";

            String statement = "ledgerDGV";
            if (model.ledgerExcludePending)
                statement = "ledgerExcludePending";
            if (usqlre.get_android_settings("USER WISE LEDGER REPORT") && usqlre.user_role != "ADMIN")
            {
                statement = "ledgerDGVUser";
            }

            String query = "EXEC [dbo].[Sp_acc_report]\n" +
                    "\t\t@wi_from_date = '" + model.fromDate + "',\n" +
                    "\t\t@wi_to_date = '" + model.toDate + "',\n" +
                    "\t\t@led_id = '" + model.asId + "',\n" +
                    "\t\t@user_id = '" + usqlre.userId + "',\n" +
                    "\t\t@loc_id = '" + usqlre.locationId + "',\n" +
                    "\t\t@balaceonly = 0,\n" +
                    "\t\t@StatementType = N'" + statement + "'";

            DataSet ds = usqlre.dbreadDataset(query);
            usqlre.close();
            if (model.ledgerExcludePending)
            {
                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
                hash.Add("table", ds.Tables["Table"]);
                hash.Add("table1", ds.Tables["Table1"]);
                return Ok(ReportModelContext.searializeDt(hash));
            }
            else
                return Ok(ReportModelContext.searializeDt(ds.Tables["Table"]));
        }

        [HttpPost("cash-report")]
        public async Task<IActionResult> cashReport([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT top 10 as_id,'active' as status from acc_subhead where as_ap_id = 14";

            String statement = "ledgerDGV";
            if (model.ledgerExcludePending)
                statement = "ledgerExcludePending";

            string cash_id = "1";
            if (usqlre.user_role != "ADMIN")
            {
                cash_id = usqlre.gu_user_cash_id;
            }

            String query = "EXEC [dbo].[Sp_acc_report]\n" +
                    "\t\t@wi_from_date = '" + model.fromDate + "',\n" +
                    "\t\t@wi_to_date = '" + model.toDate + "',\n" +
                    "\t\t@led_id = '" + cash_id + "',\n" +
                    //"\t\t@user_id = '" + usqlre.userId + "',\n" +
                    //"\t\t@loc_id = '"+loc_id+"',\n" +
                    "\t\t@balaceonly = 0,\n" +
                    "\t\t@StatementType = N'" + statement + "'";

            DataSet ds = usqlre.dbreadDataset(query);
            usqlre.close();
            if (model.ledgerExcludePending)
            {
                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
                hash.Add("table", ds.Tables["Table"]);
                hash.Add("table1", ds.Tables["Table1"]);
                return Ok(ReportModelContext.searializeDt(hash));
            }
            else
                return Ok(ReportModelContext.searializeDt(ds.Tables["Table"]));
        }

        [HttpPost("paginated-group-report")]
        public async Task<IActionResult> group([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT top 10 as_id,'active' as status from acc_subhead where as_ap_id = 14";
            int pageNumber = model.page;
            int pageSize = model.pageSize;
           
            String query = "";
            int startIndex = (pageNumber - 1) * pageSize;
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            model.salesManId = usqlre.user_role != "ADMIN" ? Convert.ToInt32(usqlre.gu_acc_id) : model.salesManId;
            int totalCount = 0;
            if(model.agingdays>0)
            {
                int _days = Convert.ToInt32(model.agingdays);
                int slno = 1;
                DataTable dt1 = new DataTable();
                DataTable dtagewise = new DataTable();
                dtagewise.Columns.Add("SlNo");
                dtagewise.Columns.Add("Particulars");
                dtagewise.Columns.Add("fourthClosing", typeof(decimal));
                dtagewise.Columns.Add("thirdClosing", typeof(decimal));
                dtagewise.Columns.Add("secondClosing", typeof(decimal));
                dtagewise.Columns.Add("firstClosing", typeof(decimal));
                dtagewise.Columns.Add("Balance", typeof(decimal));
                dtagewise.Columns.Add("Last Payment Date");
                dtagewise.Columns.Add("Due Days");
                dtagewise.Columns.Add("as_id");
                dtagewise.Columns.Add("Pending Amount");
                DataTable dtCount = new DataTable();

                DataTable dtmaster = new DataTable();
                string sql1 = "";
                if (model.salesManId > 0 && model.routeId > 0)
                {
                    //sql1 = @"SELECT as_id, as_name
                    //    FROM (
                    //    SELECT as_id, as_name,
                    //        ROW_NUMBER() OVER (ORDER BY as_id) AS RowNum
                    //    FROM acc_subhead
                    //    WHERE as_ap_id ="+model.asId+@"
                    //    AND as_rout_id = "+model.routeId+@"
                    //    AND as_salesman_id = "+model.salesManId+@"
                    //    ) AS Sub
                    //    WHERE RowNum > "+startIndex + @"
                    //    ORDER BY RowNum
                    //    OFFSET "+pageNumber+@" ROWS
                    //    FETCH NEXT "+pageSize+@" ROWS ONLY";

                   dtCount= usqlre.dbReaderFill("select COUNT(as_id) as totalCount from acc_subhead where as_ap_id=" + model.asId + " and as_rout_id=" + model.routeId + " and as_salesman_id=" + model.salesManId + "");
                    dtmaster = usqlre.dbReaderFill("select as_id,as_name from acc_subhead where as_ap_id=" + model.asId + " and as_rout_id=" + model.routeId+ " and as_salesman_id="+model.salesManId+" ORDER BY as_id OFFSET " + startIndex + " ROWS FETCH NEXT " + pageSize + " ROWS ONLY");
                }
                else if (model.salesManId > 0)
                {
                    dtCount= usqlre.dbReaderFill("select COUNT(as_id)  as totalCount from acc_subhead where as_ap_id=" + model.asId + " and as_salesman_id=" + model.salesManId + "");
                    dtmaster = usqlre.dbReaderFill("select as_id,as_name from acc_subhead where as_ap_id=" + model.asId + " and as_salesman_id="+model.salesManId+ "ORDER BY as_id OFFSET " + startIndex + " ROWS FETCH NEXT " + pageSize + " ROWS ONLY");
                }
                else if(model.routeId>0)
                {
                    dtCount = usqlre.dbReaderFill("select COUNT(as_id)  as totalCount from acc_subhead where as_ap_id=" + model.asId + " and as_rout_id=" + model.routeId + "");
                    dtmaster = usqlre.dbReaderFill("select as_id,as_name from acc_subhead where as_ap_id=" + model.asId + "and as_rout_id=" + model.routeId+" ORDER BY as_id OFFSET " + startIndex + " ROWS FETCH NEXT " + pageSize + "ROWS ONLY");
                }
                else
                {
                     //sql1 = @"WITH OrderedResults AS (SELECT as_id, as_name,ROW_NUMBER() OVER (ORDER BY as_id) AS RowNum FROM acc_subhead WHERE as_ap_id = "+model.asId+") SELECT as_id, as_name FROM OrderedResults WHERE RowNum BETWEEN "+startIndex+" AND "+pageSize+"";
                    dtCount = usqlre.dbReaderFill("select COUNT(as_id)  as totalCount from acc_subhead where as_ap_id=" + model.asId + "");
                    dtmaster = usqlre.dbReaderFill("select as_id,as_name from acc_subhead where as_ap_id=" + model.asId + " ORDER BY as_id OFFSET " + startIndex + " ROWS FETCH NEXT " + pageSize + " ROWS ONLY");
                }
                //dtmaster = usqlre.dbReaderFill(sql1);

            
                for (int n = 0; n <= dtmaster.Rows.Count - 1; n++)
                {
                    //DataTable dt1 = new DataTable();
                    decimal _closingbalance = 0;
                    DateTime dateForButton;
                    if (model.ledgerExcludePending)
                    {
                        dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_pending_status=0 and at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) <= '" + model.toDate + "'");
                        _closingbalance = CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString());
                        dateForButton = Convert.ToDateTime(model.toDate);
                        dateForButton = dateForButton.AddDays(-_days);
                        dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_pending_status=0 and at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) < '" + dateForButton + "'");

                    }
                    else
                    {
                        dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) <= '" +model.toDate + "'");
                        _closingbalance = CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString());
                        dateForButton = Convert.ToDateTime(model.toDate);
                        dateForButton = dateForButton.AddDays(-_days);
                        dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) < '" + dateForButton + "'");

                    }

                    decimal _first = 0;
                    _first = CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString());
                    decimal _firstclosing = 0;
                    _firstclosing = _closingbalance - _first;
                    if (_firstclosing < 0)
                    {
                        _firstclosing = 0;
                    }
                    dateForButton = dateForButton.AddDays(-_days);
                    dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) < '" + dateForButton + "'");
                    decimal _second = 0;
                    _second = CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString());
                    decimal _secondclosing = 0;
                    _secondclosing = _closingbalance - _firstclosing - _second;
                    if (_secondclosing < 0)
                    {
                        _secondclosing = 0;
                    }
                    dateForButton = dateForButton.AddDays(-_days);
                    dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) < '" + dateForButton + "'");
                    decimal _third = 0;
                    if (CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString()) > 0)
                        _third = CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString());
                    decimal _thirdclosing = 0;
                    _thirdclosing = _closingbalance - _firstclosing - _secondclosing - _third;
                    if (_thirdclosing < 0)
                    {
                        _thirdclosing = 0;
                    }

                    
                    decimal _fourthclosing = 0;
                    _fourthclosing = _closingbalance - _firstclosing - _secondclosing - _thirdclosing;
                    string _lastpaymentdate = "";
                    dt1 = usqlre.dbReaderFill("select top 1 at_date from acc_account_transactions where at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " order by at_date desc");
                    if (dt1.Rows.Count > 0)
                    {
                        _lastpaymentdate = String.Format(dt1.Rows[0][0].ToString(), "dd-MMM-yyyy").Substring(0, 9);
                    }
                    else
                    {
                        _lastpaymentdate = "";
                    }
                    string _duedays = "";
                    if (model.asId.ToString() == "4")
                    {
                        dt1 = usqlre.dbReaderFill("select top 1 DATEDIFF(dd,(si_date),getdate()) as DueDays from inv_sales_inf where si_acc_id=" + dtmaster.Rows[n]["as_id"].ToString() + "  order by si_date");

                    }
                    else if (model.asId.ToString() == "6")
                    {
                        dt1 = usqlre.dbReaderFill("select top 1  DATEDIFF(dd,(pi_inv_date),getdate()) as DueDays from inv_purchase_inf where pi_sup_id=" + dtmaster.Rows[n]["as_id"].ToString() + " order by pi_inv_date");
                    }
                    if (dt1.Rows.Count > 0)
                    {
                        _duedays = dt1.Rows[0][0].ToString();
                    }
                    else
                    {
                        _duedays = "";
                    }

                    string _pendingAmt = "";
                    dt1 = usqlre.dbReaderFill("select sum(at_cr) from acc_account_transactions where at_pending_status = 1 and at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " ");
                    if (dt1.Rows.Count > 0)
                    {
                        _pendingAmt = dt1.Rows[0][0].ToString();
                    }
                    else
                    {
                        _pendingAmt = "0";
                    }
                    DataRow dRow = dtagewise.NewRow();
                    dRow[0] = slno.ToString();
                    dRow[1] = dtmaster.Rows[n]["as_name"].ToString();
                    dRow[2] = _fourthclosing.ToString();
                    dRow[3] = _thirdclosing.ToString();
                    dRow[4] = _secondclosing.ToString();
                    dRow[5] = _firstclosing.ToString();
                    dRow[6] = _closingbalance.ToString();
                    dRow[7] = _lastpaymentdate;
                    dRow[8] = _duedays;
                    dRow[9] = dtmaster.Rows[n]["as_id"].ToString();
                    dRow[10] = _pendingAmt;
                    dtagewise.Rows.Add(dRow);
                    slno = slno + 1;

                }
                hash.Add("data", dtagewise);
                hash.Add("otherdata", dtCount);
                return Ok(ReportModelContext.searializeDt(hash));
            }
            else if (model.StatementType == "salesmanwisegroup")
            {
                string sales_man_selected = "";
                if (model.salesManId != 0)
                {
                    sales_man_selected = " and (b.as_id=" + model.salesManId + ") ";
                    query = "EXEC [dbo].[Sp_api_group_report] " +
                            "@wi_from_date = '" + model.fromDate + "'," +
                            "@wi_to_date = '" + model.toDate + "'," +
                            "@area = '" + sales_man_selected + "'," +
                            "@led_id = '" + model.asId + "'," +
                            "@StatementType = N'salesmanwisegroup', " +
                            "@PageSize = " + pageSize + ", " +
                            "@PageNumber = " + pageNumber;
                }

            }
            else if (model.StatementType == "rout_and_salesman")
            {

                string area_selected = "";
                string sales_man_selected = "";

                if (model.routeId != 0 && model.salesManId != 0)
                {
                    area_selected = " and (r_id=" + model.routeId;
                    sales_man_selected = " and (b.as_id=" + model.salesManId;
                    area_selected = area_selected + " ) " + sales_man_selected + " ) ";
                }


                query = "EXEC [dbo].[Sp_api_group_report] " +
                            "@wi_from_date = '" + model.fromDate + "'," +
                            "@wi_to_date = '" + model.toDate + "'," +
                            "@area = '" + area_selected + "'," +

                            "@led_id = '" + model.asId + "'," +
                            "@StatementType = N'rout_and_salesman', " +
                            "@PageSize = " + pageSize + ", " +
                            "@PageNumber = " + pageNumber;
            }
            else
            {
                if (model.routeId == 0)
                {
                    query = "EXEC [dbo].[Sp_api_group_report] " +
                                "@wi_from_date = '" + model.fromDate + "'," +
                                "@wi_to_date = '" + model.toDate + "'," +
                                "@led_id = '" + model.asId + "'," +
                                
                    "@StatementType = N'groupdetailed', " +
                                      "@PageSize = " + pageSize + ", " +
                                      "@PageNumber = " + pageNumber;

                }
                else
                {
                    string area_selected = "and (r_id=" + model.routeId + ")";
                    query = "EXEC [dbo].[Sp_api_group_report] " +
                                "@wi_from_date = '" + model.fromDate + "'," +
                                "@wi_to_date = '" + model.toDate + "'," +
                                "@area = '" + area_selected + "'," +
                                "@led_id = '" + model.asId + "'," +
                               
                                "@StatementType = N'routwise', " +
                                "@PageSize = " + pageSize + ", " +
                                "@PageNumber = " + pageNumber;
                }
            }

            DataTable dt;
            DataSet main_ds = new DataSet();
         

            //if (model.StatementType == "rout_and_salesman" || model.StatementType == "salesmanwisegroup")
            //{
            //    dt = usqlre.dbReaderFill(query);
            //   
            //}
            //else
            //{
            //    DataSet ds = usqlre.dbreadDataset(query);
            //    dt = ds.Tables["Table"];
            //}
            main_ds = usqlre.dbreadDataset(query);
            DataTable dt_main = main_ds.Tables["Table"];
            DataTable dt_other = main_ds.Tables["Table1"];
            hash.Add("data", dt_main);
            hash.Add("otherdata", dt_other);
            usqlre.close();
           
            return Ok(ReportModelContext.searializeDt(hash));
           // return Ok(response);
        }


        [HttpPost("group-report")]
        public async Task<IActionResult> groupReport([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT top 10 as_id,'active' as status from acc_subhead where as_ap_id = 14";
            String query = "";
            if (model.agingdays > 0)
            {
                int _days = Convert.ToInt32(model.agingdays);
                int slno = 1;
                DataTable dt1 = new DataTable();
                DataTable dtagewise = new DataTable();
                dtagewise.Columns.Add("SlNo");
                dtagewise.Columns.Add("Particulars");
                dtagewise.Columns.Add("fourthClosing", typeof(decimal));
                dtagewise.Columns.Add("thirdClosing", typeof(decimal));
                dtagewise.Columns.Add("secondClosing", typeof(decimal));
                dtagewise.Columns.Add("firstClosing", typeof(decimal));
                dtagewise.Columns.Add("Balance", typeof(decimal));
                dtagewise.Columns.Add("Last Payment Date");
                dtagewise.Columns.Add("Due Days");
                dtagewise.Columns.Add("as_id");
                dtagewise.Columns.Add("Pending Amount");


                DataTable dtmaster = new DataTable();
                string sql1 = "";
                if (model.salesManId > 0 && model.routeId > 0)
                {
                   
                    dtmaster = usqlre.dbReaderFill("select as_id,as_name from acc_subhead where as_ap_id=" + model.asId + " and as_rout_id=" + model.routeId + " and as_salesman_id=" + model.salesManId + "");
                }
                else if (model.salesManId > 0)
                {

                    dtmaster = usqlre.dbReaderFill("select as_id,as_name from acc_subhead where as_ap_id=" + model.asId + " and as_salesman_id=" + model.salesManId + "");
                }
                else if (model.routeId > 0)
                {
                    dtmaster = usqlre.dbReaderFill("select as_id,as_name from acc_subhead where as_ap_id=" + model.asId + "and as_rout_id=" + model.routeId + "");
                }
                else
                {
                    // sql1 = @"WITH OrderedResults AS (SELECT as_id, as_name,ROW_NUMBER() OVER (ORDER BY as_id) AS RowNum FROM acc_subhead WHERE as_ap_id = "+model.asId+") SELECT as_id, as_name FROM OrderedResults WHERE RowNum BETWEEN "+startIndex+" AND "+pageSize+"";
                    dtmaster = usqlre.dbReaderFill("select as_id,as_name from acc_subhead where as_ap_id=" + model.asId + "");
                }
                

                for (int n = 0; n <= dtmaster.Rows.Count - 1; n++)
                {
                    //DataTable dt1 = new DataTable();
                    decimal _closingbalance = 0;
                    DateTime dateForButton;
                    if (model.ledgerExcludePending)
                    {
                        dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_pending_status=0 and at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) <= '" + model.toDate + "'");
                        _closingbalance = CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString());
                        dateForButton = Convert.ToDateTime(model.toDate);
                        dateForButton = dateForButton.AddDays(-_days);
                        dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_pending_status=0 and at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) < '" + dateForButton + "'");

                    }
                    else
                    {
                        dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) <= '" + model.toDate + "'");
                        _closingbalance = CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString());
                        dateForButton = Convert.ToDateTime(model.toDate);
                        dateForButton = dateForButton.AddDays(-_days);
                        dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) < '" + dateForButton + "'");

                    }

                    decimal _first = 0;
                    _first = CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString());
                    decimal _firstclosing = 0;
                    _firstclosing = _closingbalance - _first;
                    if (_firstclosing < 0)
                    {
                        _firstclosing = 0;
                    }
                    dateForButton = dateForButton.AddDays(-_days);
                    dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) < '" + dateForButton + "'");
                    decimal _second = 0;
                    _second = CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString());
                    decimal _secondclosing = 0;
                    _secondclosing = _closingbalance - _firstclosing - _second;
                    if (_secondclosing < 0)
                    {
                        _secondclosing = 0;
                    }
                    dateForButton = dateForButton.AddDays(-_days);
                    dt1 = usqlre.dbReaderFill("select sum(at_Dr-at_Cr) from acc_account_transactions where at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " and cast(at_date as date) < '" + dateForButton + "'");
                    decimal _third = 0;
                    if (CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString()) > 0)
                        _third = CommonHelper.GetTextboxValue(dt1.Rows[0][0].ToString());
                    decimal _thirdclosing = 0;
                    _thirdclosing = _closingbalance - _firstclosing - _secondclosing - _third;
                    if (_thirdclosing < 0)
                    {
                        _thirdclosing = 0;
                    }


                    decimal _fourthclosing = 0;
                    _fourthclosing = _closingbalance - _firstclosing - _secondclosing - _thirdclosing;
                    string _lastpaymentdate = "";
                    dt1 = usqlre.dbReaderFill("select top 1 at_date from acc_account_transactions where at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " order by at_date desc");
                    if (dt1.Rows.Count > 0)
                    {
                        _lastpaymentdate = String.Format(dt1.Rows[0][0].ToString(), "dd-MMM-yyyy").Substring(0, 9);
                    }
                    else
                    {
                        _lastpaymentdate = "";
                    }
                    string _duedays = "";
                    if (model.asId.ToString() == "4")
                    {
                        dt1 = usqlre.dbReaderFill("select top 1 DATEDIFF(dd,(si_date),getdate()) as DueDays from inv_sales_inf where si_acc_id=" + dtmaster.Rows[n]["as_id"].ToString() + "  order by si_date");

                    }
                    else if (model.asId.ToString() == "6")
                    {
                        dt1 = usqlre.dbReaderFill("select top 1  DATEDIFF(dd,(pi_inv_date),getdate()) as DueDays from inv_purchase_inf where pi_sup_id=" + dtmaster.Rows[n]["as_id"].ToString() + " order by pi_inv_date");
                    }
                    if (dt1.Rows.Count > 0)
                    {
                        _duedays = dt1.Rows[0][0].ToString();
                    }
                    else
                    {
                        _duedays = "";
                    }

                    string _pendingAmt = "";
                    dt1 = usqlre.dbReaderFill("select sum(at_cr) from acc_account_transactions where at_pending_status = 1 and at_as_id=" + dtmaster.Rows[n]["as_id"].ToString() + " ");
                    if (dt1.Rows.Count > 0)
                    {
                        _pendingAmt = dt1.Rows[0][0].ToString();
                    }
                    else
                    {
                        _pendingAmt = "0";
                    }
                    DataRow dRow = dtagewise.NewRow();
                    dRow[0] = slno.ToString();
                    dRow[1] = dtmaster.Rows[n]["as_name"].ToString();
                    dRow[2] = _fourthclosing.ToString();
                    dRow[3] = _thirdclosing.ToString();
                    dRow[4] = _secondclosing.ToString();
                    dRow[5] = _firstclosing.ToString();
                    dRow[6] = _closingbalance.ToString();
                    dRow[7] = _lastpaymentdate;
                    dRow[8] = _duedays;
                    dRow[9] = dtmaster.Rows[n]["as_id"].ToString();
                    dRow[10] = _pendingAmt;
                    dtagewise.Rows.Add(dRow);
                    slno = slno + 1;

                }
                return Ok(ReportModelContext.searializeDt(dtagewise));
            }
            else if (model.StatementType == "salesmanwisegroup")
            {
                string sales_man_selected = "";
                if (model.salesManId != 0)
                {
                    sales_man_selected = " and (b.as_id=" + model.salesManId + ") ";
                    query = "EXEC [dbo].[Sp_acc_group_report] " +
                            "@wi_from_date = '" + model.fromDate + "'," +
                            "@wi_to_date = '" + model.toDate + "'," +
                            "@area = '" + sales_man_selected + "'," +
                            "@led_id = '" + model.asId + "'," +
                            "@StatementType = N'salesmanwisegroup'";
                }
                
            }
            else if (model.StatementType == "rout_and_salesman")
            {

                string area_selected = "";
                string sales_man_selected = "";

                if (model.routeId != 0 && model.salesManId != 0)
                {
                    area_selected = " and (r_id=" + model.routeId;
                    sales_man_selected = " and (b.as_id=" + model.salesManId;
                    area_selected = area_selected + " ) " + sales_man_selected + " ) ";
                }

                
                query = "EXEC [dbo].[Sp_acc_group_report] " +
                            "@wi_from_date = '" + model.fromDate + "'," +
                            "@wi_to_date = '" + model.toDate + "'," +
                            "@area = '" + area_selected + "'," +
                        
                            "@led_id = '" + model.asId + "'," +
                            "@StatementType = N'rout_and_salesman'";
            }
            else
            {
                if (model.routeId == 0)
                {
                    query = "EXEC [dbo].[Sp_acc_group_report] " +
                                "@wi_from_date = '" + model.fromDate + "'," +
                                "@wi_to_date = '" + model.toDate + "'," +
                                "@led_id = '" + model.asId + "'," +
                                "@StatementType = N'groupdetailed'";

                }
                else
                {
                    string area_selected = "and (r_id=" + model.routeId + ")";
                    query = "EXEC [dbo].[Sp_acc_group_report] " +
                                "@wi_from_date = '" + model.fromDate + "'," +
                                "@wi_to_date = '" + model.toDate + "'," +
                                "@area = '" + area_selected + "'," +
                                "@led_id = '" + model.asId + "'," +
                                "@StatementType = N'routwise'";
                }
            }

            DataTable dt;
            if (model.StatementType == "rout_and_salesman" || model.StatementType == "salesmanwisegroup")
            {
                dt = usqlre.dbReaderFill(query);
            }
            else 
            {
                DataSet ds = usqlre.dbreadDataset(query);
                dt = ds.Tables["Table"];
            }

         
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
           
        }

        // Mirrors the desktop form's cb_billwise_aging branch.  The stored
        // procedure supplies FIFO bill rows; the allocation and aging buckets
        // below deliberately follow the desktop calculation order.
        private DataTable BuildBillWiseAgingReport(UserSqlServer usqlre, GroupReportNewRequest model)
        {
            int days = model.agingdays.GetValueOrDefault();
            DateTime toDate = Convert.ToDateTime(model.toDate).Date;
            bool excludePending = model.excludePending || model.ledgerExcludePending;

            DataTable fifoBills = new DataTable();
            using (SqlCommand command = new SqlCommand("Sp_acc_group_report", usqlre.shop))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@wi_from_date", model.fromDate);
                command.Parameters.AddWithValue("@wi_to_date", model.toDate);
                command.Parameters.AddWithValue("@led_id", Math.Max(model.asId, 0));
                command.Parameters.AddWithValue("@excludepending", excludePending ? 1 : 0);
                command.Parameters.AddWithValue("@groupwhere", "and as_ap_id=(" + Math.Max(model.asId, 0) + ")");
                command.Parameters.AddWithValue("@StatementType", "duebilllistCustFIFOWhere");

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(fifoBills);
                }
            }

            var fifoRows = new List<BillWiseAgingRow>();
            decimal creditBalanceTotal = 0;

            foreach (DataRow row in fifoBills.Rows)
            {
                decimal rowCreditBalance = GetDecimal(row, "CreditBalanceTotal");
                if (rowCreditBalance != 0 && GetDecimal(row, "Slno") == 1)
                {
                    creditBalanceTotal = rowCreditBalance;
                }

                string voucherType = GetString(row, "VOUCHER TYPE");
                decimal billBalance = 0;
                decimal balanceAmount = 0;

                if (voucherType != "SALES-RETURN" && voucherType != "SALES-RETURN-OLD")
                {
                    billBalance = GetDecimal(row, "BalanceAmtNew");
                    if (creditBalanceTotal == 0)
                    {
                        balanceAmount = 0;
                    }
                    else if (billBalance <= creditBalanceTotal && billBalance > 0)
                    {
                        creditBalanceTotal -= billBalance;
                        balanceAmount = billBalance;
                    }
                    else if (billBalance > creditBalanceTotal && billBalance >= 0)
                    {
                        balanceAmount = creditBalanceTotal;
                        billBalance -= creditBalanceTotal;
                        creditBalanceTotal = 0;
                    }
                    else if (billBalance <= 0)
                    {
                        creditBalanceTotal -= billBalance;
                        balanceAmount = billBalance;
                    }
                }

                fifoRows.Add(new BillWiseAgingRow
                {
                    LedgerName = GetString(row, "PARTY"),
                    Date = GetDate(row, "INVOICE DATE"),
                    // Desktop code assigns DueDate only when the FIFO row has
                    // a remaining balance; settled rows contribute zero.
                    DueDays = balanceAmount != 0 ? GetDecimal(row, "DUEDAYS") : 0,
                    BalanceAmount = balanceAmount
                });
            }

            string accountFilter = BuildGroupReportAccountFilter(model);
            DataTable accounts = usqlre.dbReaderFill(
                "SELECT a.as_id, a.as_name FROM acc_subhead a WHERE a.as_ap_id=" + Math.Max(model.asId, 0) + accountFilter);

            DataTable result = new DataTable();
            result.Columns.Add("SlNo");
            result.Columns.Add("Particulars");
            result.Columns.Add("> " + (days * 3) + " Days", typeof(decimal));
            result.Columns.Add((days * 2) + " - " + (days * 3) + " Days", typeof(decimal));
            result.Columns.Add(days + " - " + (days * 2) + " Days", typeof(decimal));
            result.Columns.Add("< " + days + " Days", typeof(decimal));
            result.Columns.Add("Balance", typeof(decimal));
            result.Columns.Add("DueDays");
            result.Columns.Add("as_id");
            result.Columns.Add("PendingAmount", typeof(decimal));

            int slNo = 1;
            foreach (DataRow account in accounts.Rows)
            {
                string accountName = GetString(account, "as_name");
                var accountBills = fifoRows.Where(row => row.LedgerName == accountName).ToList();

                decimal closing = accountBills.Where(row => row.Date <= toDate).Sum(row => row.BalanceAmount);
                decimal oldFirst = accountBills.Where(row => row.Date < toDate.AddDays(-days)).Sum(row => row.BalanceAmount);
                decimal firstClosing = Math.Max(0, closing - oldFirst);
                decimal oldSecond = accountBills.Where(row => row.Date < toDate.AddDays(-(days * 2))).Sum(row => row.BalanceAmount);
                decimal secondClosing = Math.Max(0, closing - firstClosing - oldSecond);
                decimal oldThird = accountBills.Where(row => row.Date < toDate.AddDays(-(days * 3))).Sum(row => row.BalanceAmount);
                decimal thirdClosing = Math.Max(0, closing - firstClosing - secondClosing - oldThird);
                decimal fourthClosing = closing - firstClosing - secondClosing - thirdClosing;

                if (model.balanceOnly && closing <= 0)
                {
                    continue;
                }

                int accountId = Convert.ToInt32(account["as_id"]);
                decimal pendingAmount = GetPendingAmount(usqlre, accountId);
                string dueDays = accountBills.Any()
                    ? accountBills.Max(row => row.DueDays).ToString()
                    : "";

                DataRow output = result.NewRow();
                output[0] = slNo++;
                output[1] = accountName;
                output[2] = fourthClosing;
                output[3] = thirdClosing;
                output[4] = secondClosing;
                output[5] = firstClosing;
                output[6] = closing;
                output[7] = dueDays;
                output[8] = accountId;
                output[9] = pendingAmount;
                result.Rows.Add(output);
            }

            return result;
        }

        private static string BuildGroupReportAccountFilter(GroupReportNewRequest model)
        {
            var filters = new List<string>();
            var areaIds = (model.areaIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            var routeIds = (model.routeIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            var salesmanIds = (model.salesmanIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            var isrIds = (model.isrIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();

            if (model.areaId > 0 && !areaIds.Contains(model.areaId)) areaIds.Add(model.areaId);
            if (model.routeId > 0 && !routeIds.Contains(model.routeId)) routeIds.Add(model.routeId);
            if (model.salesManId > 0 && !salesmanIds.Contains(model.salesManId)) salesmanIds.Add(model.salesManId);

            if (areaIds.Any()) filters.Add("a.as_area_id IN (" + string.Join(",", areaIds) + ")");
            if (routeIds.Any()) filters.Add("a.as_rout_id IN (" + string.Join(",", routeIds) + ")");
            if (salesmanIds.Any()) filters.Add("a.as_salesman_id IN (" + string.Join(",", salesmanIds) + ")");
            if (isrIds.Any()) filters.Add("a.as_isr_id IN (" + string.Join(",", isrIds) + ")");

            var categories = (model.categories ?? new List<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value) && !string.Equals(value, "string", StringComparison.OrdinalIgnoreCase))
                .Select(value => "'" + value.Replace("'", "''") + "'").Distinct().ToList();
            var ratings = (model.ratings ?? new List<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value) && !string.Equals(value, "string", StringComparison.OrdinalIgnoreCase))
                .Select(value => "'" + value.Replace("'", "''") + "'").Distinct().ToList();

            if (categories.Any()) filters.Add("a.as_category IN (" + string.Join(",", categories) + ")");
            if (ratings.Any()) filters.Add("a.as_rating IN (" + string.Join(",", ratings) + ")");

            return filters.Any() ? " AND " + string.Join(" AND ", filters) : "";
        }

        private static decimal GetDecimal(DataRow row, string column)
        {
            return row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                ? Convert.ToDecimal(row[column])
                : 0;
        }

        private static string GetString(DataRow row, string column)
        {
            return row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                ? row[column].ToString()
                : "";
        }

        private static DateTime GetDate(DataRow row, string column)
        {
            return row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                ? Convert.ToDateTime(row[column]).Date
                : DateTime.MinValue;
        }

        private static decimal GetPendingAmount(UserSqlServer usqlre, int accountId)
        {
            DataTable pending = usqlre.dbReaderFill(
                "SELECT SUM(at_cr) AS PendingAmount FROM acc_account_transactions WHERE at_pending_status=1 AND at_as_id=" + accountId);
            return pending.Rows.Count > 0 ? GetDecimal(pending.Rows[0], "PendingAmount") : 0;
        }

        private sealed class BillWiseAgingRow
        {
            public string LedgerName { get; set; }
            public DateTime Date { get; set; }
            public decimal DueDays { get; set; }
            public decimal BalanceAmount { get; set; }
        }

        private static string BuildGroupNewFallbackSql(
            GroupReportNewRequest model,
            string groupWhere)
        {
            DateTime fromDate = Convert.ToDateTime(model.fromDate);
            DateTime toDate = Convert.ToDateTime(model.toDate);
            string pendingCondition = (model.excludePending || model.ledgerExcludePending)
                ? " AND at_pending_status = 0 "
                : "";
            string balanceOnlyCondition = model.balanceOnly
                ? " WHERE report.Credit > 0 OR report.Debit > 0"
                : "";

            // Same SELECT used by Sp_acc_group_report's groupNew branch.
            // This is only used when an older deployed procedure throws its
            // invalid ORDER BY error.
            return @"
SELECT * FROM
(
    SELECT
        a.as_name AS Particulars,
        MAX(a.as_add1) AS Address1,
        MAX(a.as_add2) AS Address2,
        MAX(a.as_mob) AS Mobile,
        MAX(a.as_category) AS Category,
        MAX(a.as_agency_name) AS Agency,
        CASE WHEN SUM(at_Dr) - SUM(at_Cr) > 0
            THEN dbo.MoneyToN(SUM(at_Dr) - SUM(at_Cr), 4)
            ELSE dbo.MoneyToN(0, 4) END AS Debit,
        CASE WHEN SUM(at_Cr) - SUM(at_Dr) > 0
            THEN dbo.MoneyToN(SUM(at_Cr) - SUM(at_Dr), 4)
            ELSE dbo.MoneyToN(0, 4) END AS Credit,
        CASE
            WHEN SUM(at_Dr) - SUM(at_Cr) > 0 AND SUM(at_Cr) - SUM(at_Dr) <= 0
                THEN dbo.MoneyToN(SUM(at_Dr) - SUM(at_Cr), 4)
            WHEN SUM(at_Dr) - SUM(at_Cr) <= 0 AND SUM(at_Cr) - SUM(at_Dr) > 0
                THEN -(dbo.MoneyToN(SUM(at_Cr) - SUM(at_Dr), 4))
            ELSE dbo.MoneyToN(SUM(at_Dr) - SUM(at_Cr), 4)
                - dbo.MoneyToN(SUM(at_Cr) - SUM(at_Dr), 4)
        END AS Balance,
        MAX(a.as_id) AS as_id
    FROM acc_account_transactions
    INNER JOIN acc_subhead a ON at_as_id = a.as_id
    INNER JOIN acc_parent ON a.as_ap_id = ap_id
    " + groupWhere + @"
    AND CAST(at_date AS DATE) >= '" + fromDate.ToString("yyyy-MM-dd") + @"'
    AND CAST(at_date AS DATE) <= '" + toDate.ToString("yyyy-MM-dd") + @"'
    AND a.as_active = 1
    " + pendingCondition + @"
    GROUP BY a.as_name
) report" + balanceOnlyCondition + ";";
        }

        [HttpPost("group-report-new")]
        public async Task<IActionResult> groupReportNew([FromBody] GroupReportNewRequest model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string query = "";

                // ============================================================
                // BILL WISE AGING
                // ============================================================
                if (model.agingdays > 0 && model.billWiseAging)
                {
                    DataTable billWiseAging = BuildBillWiseAgingReport(usqlre, model);
                    usqlre.close();
                    return Ok(ReportModelContext.searializeDt(billWiseAging));
                }

                if (model.agingdays > 0)
                {
                    int _days = Convert.ToInt32(model.agingdays);

                    DataTable dtagewise = new DataTable();

                    dtagewise.Columns.Add("SlNo");
                    dtagewise.Columns.Add("Particulars");
                    dtagewise.Columns.Add("fourthClosing", typeof(decimal));
                    dtagewise.Columns.Add("thirdClosing", typeof(decimal));
                    dtagewise.Columns.Add("secondClosing", typeof(decimal));
                    dtagewise.Columns.Add("firstClosing", typeof(decimal));
                    dtagewise.Columns.Add("Balance", typeof(decimal));
                    dtagewise.Columns.Add("Last Payment Date");
                    dtagewise.Columns.Add("Due Days");
                    dtagewise.Columns.Add("as_id");
                    dtagewise.Columns.Add("Pending Amount");

                    // ------------------------------------------------------------
                    // Account filter - same as old software/API
                    // ------------------------------------------------------------
                    string accountFilter = "";

                    if (model.salesManId > 0 && model.routeId > 0)
                    {
                        accountFilter =
                            " AND a.as_rout_id = " + model.routeId +
                            " AND a.as_salesman_id = " + model.salesManId;
                    }
                    else if (model.salesManId > 0)
                    {
                        accountFilter =
                            " AND a.as_salesman_id = " + model.salesManId;
                    }
                    else if (model.routeId > 0)
                    {
                        accountFilter =
                            " AND a.as_rout_id = " + model.routeId;
                    }

                    // The desktop form permits selecting more than one area,
                    // route, salesman, category, rating and ISR for aging too.
                    var agingAreaIds = (model.areaIds ?? new List<int>())
                        .Where(id => id > 0).Distinct().ToList();
                    if (model.areaId > 0 && !agingAreaIds.Contains(model.areaId)) agingAreaIds.Add(model.areaId);

                    var agingRouteIds = (model.routeIds ?? new List<int>())
                        .Where(id => id > 0).Distinct().ToList();
                    if (model.routeId > 0 && !agingRouteIds.Contains(model.routeId)) agingRouteIds.Add(model.routeId);

                    var agingSalesmanIds = (model.salesmanIds ?? new List<int>())
                        .Where(id => id > 0).Distinct().ToList();
                    if (model.salesManId > 0 && !agingSalesmanIds.Contains(model.salesManId)) agingSalesmanIds.Add(model.salesManId);

                    var agingCategories = (model.categories ?? new List<string>())
                        .Where(value => !string.IsNullOrWhiteSpace(value) && !string.Equals(value, "string", StringComparison.OrdinalIgnoreCase))
                        .Select(value => "'" + value.Replace("'", "''") + "'")
                        .Distinct().ToList();
                    var agingRatings = (model.ratings ?? new List<string>())
                        .Where(value => !string.IsNullOrWhiteSpace(value) && !string.Equals(value, "string", StringComparison.OrdinalIgnoreCase))
                        .Select(value => "'" + value.Replace("'", "''") + "'")
                        .Distinct().ToList();
                    var agingIsrIds = (model.isrIds ?? new List<int>())
                        .Where(id => id > 0).Distinct().ToList();

                    if (agingAreaIds.Any()) accountFilter += " AND a.as_area_id IN (" + string.Join(",", agingAreaIds) + ")";
                    if (agingRouteIds.Any()) accountFilter += " AND a.as_rout_id IN (" + string.Join(",", agingRouteIds) + ")";
                    if (agingSalesmanIds.Any()) accountFilter += " AND a.as_salesman_id IN (" + string.Join(",", agingSalesmanIds) + ")";
                    if (agingCategories.Any()) accountFilter += " AND a.as_category IN (" + string.Join(",", agingCategories) + ")";
                    if (agingRatings.Any()) accountFilter += " AND a.as_rating IN (" + string.Join(",", agingRatings) + ")";
                    if (agingIsrIds.Any()) accountFilter += " AND a.as_isr_id IN (" + string.Join(",", agingIsrIds) + ")";

                    // ------------------------------------------------------------
                    // OLD behavior:
                    // Closing + First => pending filter applies when requested
                    // Second + Third => no pending filter (same as old code)
                    // ------------------------------------------------------------
                    string balancePendingCondition = "";

                    if (model.excludePending || model.ledgerExcludePending)
                    {
                        balancePendingCondition =
                            " AND t.at_pending_status = 0 ";
                    }

                    string sqlAging = @"
WITH Accounts AS
(
    SELECT
        a.as_id,
        a.as_name
    FROM acc_subhead a
    WHERE a.as_ap_id = " + model.asId + @"
    " + accountFilter + @"
),
TransactionSummary AS
(
    SELECT
        t.at_as_id,

        /* Closing Balance */
        SUM
        (
            CASE
                WHEN CAST(t.at_date AS date) <= '" + model.toDate + @"'
                " + balancePendingCondition + @"
                THEN ISNULL(t.at_Dr,0) - ISNULL(t.at_Cr,0)
                ELSE 0
            END
        ) AS ClosingBalance,

        /* First Aging */
        SUM
        (
            CASE
                WHEN CAST(t.at_date AS date)
                     < DATEADD(DAY, -" + _days + @", '" + model.toDate + @"')
                " + balancePendingCondition + @"
                THEN ISNULL(t.at_Dr,0) - ISNULL(t.at_Cr,0)
                ELSE 0
            END
        ) AS OldFirst,

        /* Second Aging - same old behavior */
        SUM
        (
            CASE
                WHEN CAST(t.at_date AS date)
                     < DATEADD(DAY, -" + (_days * 2) + @", '" + model.toDate + @"')
                THEN ISNULL(t.at_Dr,0) - ISNULL(t.at_Cr,0)
                ELSE 0
            END
        ) AS OldSecond,

        /* Third Aging - same old behavior */
        SUM
        (
            CASE
                WHEN CAST(t.at_date AS date)
                     < DATEADD(DAY, -" + (_days * 3) + @", '" + model.toDate + @"')
                THEN ISNULL(t.at_Dr,0) - ISNULL(t.at_Cr,0)
                ELSE 0
            END
        ) AS OldThird,

        /* Last Payment Date */
        MAX(t.at_date) AS LastPaymentDate,

        /* Pending Amount */
        SUM
        (
            CASE
                WHEN t.at_pending_status = 1
                THEN ISNULL(t.at_cr,0)
                ELSE 0
            END
        ) AS PendingAmount

    FROM acc_account_transactions t
    INNER JOIN Accounts a
        ON a.as_id = t.at_as_id

    GROUP BY t.at_as_id
),
Aging1 AS
(
    SELECT
        a.as_id,
        a.as_name,

        ISNULL(ts.ClosingBalance,0) AS ClosingBalance,
        ISNULL(ts.OldFirst,0) AS OldFirst,
        ISNULL(ts.OldSecond,0) AS OldSecond,
        ISNULL(ts.OldThird,0) AS OldThird,
        ts.LastPaymentDate,
        ISNULL(ts.PendingAmount,0) AS PendingAmount,

        CASE
            WHEN ISNULL(ts.ClosingBalance,0)
                 - ISNULL(ts.OldFirst,0) < 0
            THEN 0
            ELSE
                ISNULL(ts.ClosingBalance,0)
                - ISNULL(ts.OldFirst,0)
        END AS FirstClosing

    FROM Accounts a

    LEFT JOIN TransactionSummary ts
        ON ts.at_as_id = a.as_id
),
Aging2 AS
(
    SELECT
        *,
        CASE
            WHEN ClosingBalance
                 - FirstClosing
                 - OldSecond < 0
            THEN 0
            ELSE
                ClosingBalance
                - FirstClosing
                - OldSecond
        END AS SecondClosing

    FROM Aging1
),
Aging3 AS
(
    SELECT
        *,
        CASE
            WHEN OldThird > 0
            THEN OldThird
            ELSE 0
        END AS ThirdValue

    FROM Aging2
),
Aging4 AS
(
    SELECT
        *,
        CASE
            WHEN ClosingBalance
                 - FirstClosing
                 - SecondClosing
                 - ThirdValue < 0
            THEN 0
            ELSE
                ClosingBalance
                - FirstClosing
                - SecondClosing
                - ThirdValue
        END AS ThirdClosing

    FROM Aging3
)
SELECT
    ROW_NUMBER() OVER (ORDER BY as_id) AS SlNo,
    as_name AS Particulars,

    /* > Aging Days * 3 */
    ClosingBalance
        - FirstClosing
        - SecondClosing
        - ThirdClosing AS fourthClosing,

    /* Aging Days * 2 to * 3 */
    ThirdClosing AS thirdClosing,

    /* Aging Days to * 2 */
    SecondClosing AS secondClosing,

    /* < Aging Days */
    FirstClosing AS firstClosing,

    /* Balance */
    ClosingBalance AS Balance,

    /* Last Payment Date */
    LastPaymentDate,

    as_id,

    PendingAmount AS [Pending Amount]

FROM Aging4
" + (model.balanceOnly ? "WHERE ClosingBalance > 0" : "") + @"
ORDER BY as_id;
";

                    DataTable dtAging = usqlre.dbReaderFill(sqlAging);

                    // ------------------------------------------------------------
                    // Due Days
                    // Same as old software:
                    // asId 4 = Sales
                    // asId 6 = Purchase
                    // ------------------------------------------------------------
                    DataTable dtDue = new DataTable();

                    if (model.asId == 4)
                    {
                        string sqlDue = @"
SELECT
    a.as_id,
    DATEDIFF(dd, MIN(s.si_date), GETDATE()) AS DueDays
FROM acc_subhead a
INNER JOIN inv_sales_inf s
    ON s.si_acc_id = a.as_id
WHERE a.as_ap_id = " + model.asId + @"
" + accountFilter + @"
GROUP BY a.as_id";

                        dtDue = usqlre.dbReaderFill(sqlDue);
                    }
                    else if (model.asId == 6)
                    {
                        string sqlDue = @"
SELECT
    a.as_id,
    DATEDIFF(dd, MIN(p.pi_inv_date), GETDATE()) AS DueDays
FROM acc_subhead a
INNER JOIN inv_purchase_inf p
    ON p.pi_sup_id = a.as_id
WHERE a.as_ap_id = " + model.asId + @"
" + accountFilter + @"
GROUP BY a.as_id";

                        dtDue = usqlre.dbReaderFill(sqlDue);
                    }

                    Dictionary<string, string> dueDaysLookup =
                        new Dictionary<string, string>();

                    foreach (DataRow dueRow in dtDue.Rows)
                    {
                        string accountId = dueRow["as_id"].ToString();

                        if (!dueDaysLookup.ContainsKey(accountId))
                        {
                            dueDaysLookup.Add(
                                accountId,
                                dueRow["DueDays"].ToString()
                            );
                        }
                    }

                    // ------------------------------------------------------------
                    // Final response - SAME structure as old endpoint
                    // ------------------------------------------------------------
                    int slno = 1;

                    foreach (DataRow row in dtAging.Rows)
                    {
                        DataRow dRow = dtagewise.NewRow();

                        dRow[0] = slno.ToString();
                        dRow[1] = row["Particulars"].ToString();
                        dRow[2] = row["fourthClosing"];
                        dRow[3] = row["thirdClosing"];
                        dRow[4] = row["secondClosing"];
                        dRow[5] = row["firstClosing"];
                        dRow[6] = row["Balance"];

                        // Same old Last Payment Date output
                        if (row["LastPaymentDate"] != DBNull.Value)
                        {
                            string lastDate =
                                row["LastPaymentDate"].ToString();

                            if (!string.IsNullOrEmpty(lastDate))
                            {
                                try
                                {
                                    dRow[7] =
                                        Convert.ToDateTime(
                                            row["LastPaymentDate"]
                                        )
                                        .ToString("dd-MMM-yyyy");
                                }
                                catch
                                {
                                    dRow[7] = lastDate;
                                }
                            }
                            else
                            {
                                dRow[7] = "";
                            }
                        }
                        else
                        {
                            dRow[7] = "";
                        }

                        string accountId = row["as_id"].ToString();

                        if (dueDaysLookup.ContainsKey(accountId))
                        {
                            dRow[8] = dueDaysLookup[accountId];
                        }
                        else
                        {
                            dRow[8] = "";
                        }

                        dRow[9] = accountId;
                        dRow[10] = row["Pending Amount"];

                        dtagewise.Rows.Add(dRow);

                        slno++;
                    }

                    usqlre.close();

                    return Ok(
                        ReportModelContext.searializeDt(dtagewise)
                    );
                }

                // Desktop "SalesmanWise" report has a different result
                // layout (Salesman, Opening Balance, Debit, Credit, Closing
                // Balance), so it must use its dedicated procedure branch.
                else if (string.Equals(
                    model.StatementType,
                    "salesmanwisegroup",
                    StringComparison.OrdinalIgnoreCase))
                {
                    DataTable salesmanWise = new DataTable();
                    string salesmanWhere = model.salesManId > 0
                        ? " and (b.as_id=" + model.salesManId + ") "
                        : "";

                    using (SqlCommand command = new SqlCommand("Sp_acc_group_report", usqlre.shop))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@wi_from_date", model.fromDate);
                        command.Parameters.AddWithValue("@wi_to_date", model.toDate);
                        command.Parameters.AddWithValue("@area", salesmanWhere);
                        command.Parameters.AddWithValue("@led_id", Math.Max(model.asId, 0));
                        command.Parameters.AddWithValue("@StatementType", "salesmanwisegroup");

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(salesmanWise);
                        }
                    }

                    usqlre.close();
                    return Ok(ReportModelContext.searializeDt(salesmanWise));
                }

                // ============================================================
                // GROUP REPORT (same filtering behaviour as the desktop form)
                // ============================================================
                else
                {
                    var areaIds = model.areaIds ?? new List<int>();
                    var routeIds = model.routeIds ?? new List<int>();
                    var salesmanIds = model.salesmanIds ?? new List<int>();

                    if (model.areaId > 0 && !areaIds.Contains(model.areaId)) areaIds.Add(model.areaId);
                    if (model.routeId > 0 && !routeIds.Contains(model.routeId)) routeIds.Add(model.routeId);
                    if (model.salesManId > 0 && !salesmanIds.Contains(model.salesManId)) salesmanIds.Add(model.salesManId);

                    areaIds = areaIds.Where(id => id > 0).Distinct().ToList();
                    routeIds = routeIds.Where(id => id > 0).Distinct().ToList();
                    salesmanIds = salesmanIds.Where(id => id > 0).Distinct().ToList();

                    var conditions = new List<string>
                    {
                        "a.as_ap_id = " + Math.Max(model.asId, 0)
                    };

                    if (areaIds.Any()) conditions.Add("a.as_area_id IN (" + string.Join(",", areaIds) + ")");
                    if (routeIds.Any()) conditions.Add("a.as_rout_id IN (" + string.Join(",", routeIds) + ")");
                    if (salesmanIds.Any()) conditions.Add("a.as_salesman_id IN (" + string.Join(",", salesmanIds) + ")");

                    var categories = (model.categories ?? new List<string>())
                        .Where(value => !string.IsNullOrWhiteSpace(value) && !string.Equals(value, "string", StringComparison.OrdinalIgnoreCase))
                        .Select(value => "'" + value.Replace("'", "''") + "'")
                        .Distinct()
                        .ToList();
                    var ratings = (model.ratings ?? new List<string>())
                        .Where(value => !string.IsNullOrWhiteSpace(value) && !string.Equals(value, "string", StringComparison.OrdinalIgnoreCase))
                        .Select(value => "'" + value.Replace("'", "''") + "'")
                        .Distinct()
                        .ToList();
                    var isrIds = (model.isrIds ?? new List<int>())
                        .Where(id => id > 0)
                        .Distinct()
                        .ToList();

                    if (categories.Any()) conditions.Add("a.as_category IN (" + string.Join(",", categories) + ")");
                    if (ratings.Any()) conditions.Add("a.as_rating IN (" + string.Join(",", ratings) + ")");
                    if (isrIds.Any()) conditions.Add("a.as_isr_id IN (" + string.Join(",", isrIds) + ")");

                    string groupWhere = "WHERE " + string.Join(" AND ", conditions);

                    using (SqlCommand command = new SqlCommand("Sp_acc_group_report", usqlre.shop))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@wi_from_date", model.fromDate);
                        command.Parameters.AddWithValue("@wi_to_date", model.toDate);
                        command.Parameters.AddWithValue("@led_id", Math.Max(model.asId, 0));
                        command.Parameters.AddWithValue("@balaceonly", model.balanceOnly ? 1 : 0);
                        command.Parameters.AddWithValue("@excludepending", (model.excludePending || model.ledgerExcludePending) ? 1 : 0);
                        command.Parameters.AddWithValue("@groupwhere", groupWhere);
                        command.Parameters.AddWithValue("@StatementType", "groupNew");

                        DataTable dt = new DataTable();
                        try
                        {
                            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                            {
                                adapter.Fill(dt);
                            }
                        }
                        catch (SqlException ex) when
                            (ex.Message.IndexOf("ORDER BY clause is invalid", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // Supports databases that still have the older
                            // procedure version until its SQL script is deployed.
                            dt = usqlre.dbReaderFill(
                                BuildGroupNewFallbackSql(model, groupWhere));
                        }

                        usqlre.close();
                        return Ok(ReportModelContext.searializeDt(dt));
                    }
                }
            }
            catch (Exception ex)
            {
                usqlre.close();

                return StatusCode(
                    500,
                    new
                    {
                        status = false,
                        message = ex.Message
                    }
                );
            }
        }


        [HttpGet("get-checkin-shop-details/{id}")]
        public string getCheckinshopDetails(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "select max(as_id) as as_id,max(as_name) as as_name,max(as_latitude) as as_latitude,max(as_longitude) as as_longitude,isnull(sum(at_Dr)-sum(at_Cr),0) as balance,max(as_add1) as as_add1,max(as_mob) as as_mob,max(as_tin) as as_tin  from acc_subhead left join acc_account_transactions on at_as_id=as_id where as_id=" + id + "";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);

        }


        [HttpPost("day-sheet")]
        public async Task<IActionResult> daySheet([FromBody] ReportDatesModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            if (!string.IsNullOrEmpty(model.statement))
            {
                string statement = model.statement;
                bool isDaySheetClosing = false;
                if (statement.ToLower() == "daysheetclosing")
                {
                    statement = "DaySheetclosing";
                    isDaySheetClosing = true;
                }

                sql = "EXEC  [dbo].[Sp_acc_report] " +
                        "@wi_from_date = '" + model.fromDate + "', " +
                        "@wi_to_date = '" + model.toDate + "', " +
                        "@StatementType = N'" + statement + "'";

                if (isDaySheetClosing)
                {
                    sql += ", @loc_id = '" + model.location_id + "', @user_id = 0";
                }
                else if (model.location_id != 0)
                {
                    sql += ", @loc_id = '" + model.location_id + "'";
                }
            }
            else if(model.location_id == 0)
            {
                sql = "EXEC  [dbo].[Sp_acc_report] " +
                        "@wi_from_date = '" + model.fromDate + "', " +
                        "@wi_to_date = '" + model.toDate + "', " +
                        "@StatementType = N'daysheet'";
            }
            else
            {
                sql =
                    "EXEC [dbo].[Sp_acc_report] " +
                    "@wi_from_date = '" + model.fromDate + "', " +
                    "@wi_to_date = '" + model.toDate + "', " +
                    "@StatementType = N'daysheetloc', " +
                    "@loc_id = '" + model.location_id + "'";
            }

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }

        [HttpPost("pandl-report")]
        public async Task<IActionResult> pandlReport([FromBody] ReportDatesModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT top 10 as_id,'active' as status from acc_subhead where as_ap_id = 14";

            sql = "EXEC  [dbo].[Sp_acc_report] " +
                    "@wi_from_date = '" + model.fromDate + "', " +
                    "@wi_to_date = '" + model.toDate + "', " +
                    "@StatementType = N'daysheet'";


            sql = "EXEC [dbo].[Sp_acc_profitandlossacc] @from_date = '" + model.fromDate + "',@to_date = '" + model.toDate + "',@StatementType = N'Group'";

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }


        [HttpPost("stock-report")]
        public async Task<IActionResult> stockReport([FromBody] StockReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql_where = "";
            DataTable select = usqlre.getReportColumns("Stock");
            if (model.itemName > 0)
            {
                if (sql_where == "")
                {
                    sql_where += " where inv_item_reg.ir_id = " + model.itemName + "";
                }
                else
                {
                    sql_where += " and inv_item_reg.ir_id = " + model.itemName + " ";
                }
            }

            if (!model.showall)
            {
                if (sql_where == "")
                {
                    sql_where += " where qty > 0 ";
                }
                else
                {
                    sql_where += " and qty > 0 ";
                }

            }

            if (model.location > 0)
            {
                if (sql_where == "")
                {
                    sql_where += " where location_id = " + model.location + "";
                }
                else
                {
                    sql_where += " and location_id = " + model.location + "";
                }

            }
            if (model.category > 0)
            {
                if (sql_where == "")
                    sql_where += " where inv_item_reg.ir_category_id ='" + model.category + "'";
                else
                    sql_where += " and inv_item_reg.ir_category_id ='" + model.category + "'";
            }
            if (model.mfr.HasValue && model.mfr.Value > 0)
            {
                if (sql_where == "")
                    sql_where += " where inv_item_reg.ir_mfr_id = " + model.mfr.Value;
                else
                    sql_where += " and inv_item_reg.ir_mfr_id = " + model.mfr.Value;
            }

            if (model.subCategory.HasValue && model.subCategory.Value > 0)
            {
                if (sql_where == "")
                    sql_where += " where inv_item_reg.ir_sub_category_id = " + model.subCategory.Value;
                else
                    sql_where += " and inv_item_reg.ir_sub_category_id = " + model.subCategory.Value;
            }

            if (model.brand>0)
            {
                string sql = "select bra_name from inv_brand where bra_id=" + model.brand + "";
                DataTable dt_brand = usqlre.dbReaderFill(sql);
                usqlre.close();
                if(dt_brand.Rows.Count>0)
                {
                    if (sql_where == "")
                        sql_where += " where brand ='" + dt_brand.Rows[0]["bra_name"].ToString() + "'";
                    else
                        sql_where += " and brand ='" + dt_brand.Rows[0]["bra_name"].ToString() + "'";
                }
            }

            if (usqlre.get_android_settings("brand_from_inv_barcode"))
            {
                if (model.brandName != "")
                {
                    if (sql_where == "")
                        sql_where += " where ib.b_brand ='" + model.brandName + "'";
                    else
                        sql_where += " and ib.b_brand ='" + model.brandName + "'";
                }
                else
                {
                    if (model.brand > 0)
                    {
                        if (sql_where == "")
                            sql_where += " where inv_item_reg.ir_mfr_id ='" + model.brand + "'";
                        else
                            sql_where += " where inv_item_reg.ir_mfr_id ='" + model.brand + "'";
                    }
                }
            }

            if (model.itemCode != "0")
            {
                if (sql_where == "")
                    sql_where += " where inv_item_reg.ir_code ='" + model.itemCode + "'";
                else
                    sql_where += " and inv_item_reg.ir_code ='" + model.itemCode + "'";

            }

            bool showavgstock = usqlre.get_android_settings("showavgstock");


            if (model.intBarcode != "")
            {

                if (showavgstock)
                {
                    if (sql_where == "")
                        sql_where += " where View_stock_avg.int_barcode ='" + model.intBarcode + "'";
                    else
                        sql_where += " and View_stock_avg.int_barcode ='" + model.intBarcode + "'";
                }
                else {
                    if (sql_where == "")
                        sql_where += " where  View_stock.int_barcode ='" + model.intBarcode + "'";
                    else
                        sql_where += " and  View_stock.int_barcode ='" + model.intBarcode + "'";
                }
            }


            if (model.barcode != "")
            {
                if (showavgstock)
                {
                    if (sql_where == "")
                        sql_where += " where uniquecode = " + model.barcode + "";
                    else
                        sql_where += " and uniquecode = " + model.barcode + "";
                }
                else
                {
                    if (sql_where == "")
                        sql_where += " where  uniquecode = " + model.barcode + "";
                    else
                        sql_where += " and  uniquecode = " + model.barcode + "";
                }

            }

            string query_str = "";

            if (select.Rows.Count > 0)
            {
                if (sql_where == "")
                {
                    if (showavgstock)
                        query_str = "SELECT " + select.Rows[0]["grc_m_select"].ToString() + ",cast(prate as numeric(32,2)) as prate from View_stock_avg inner join inv_item_reg on inv_item_reg.ir_id=View_stock_avg.ir_id left join gnl_location on gl_id=location_id";
                    else
                        query_str = "SELECT " + select.Rows[0]["grc_m_select"].ToString() + ",cast(prate as numeric(32,2)) as prate from View_Stock inner join inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id inner join acc_subhead on as_id=Sup left join inv_category ic on inv_item_reg.ir_category_id=ic.c_id left join inv_barcode ib on View_Stock.uniquecode=ib.b_uniquecode left join gnl_location on gl_id=location_id order by inv_item_reg.ir_name asc, exp_date desc";
                }
                else
                {
                    if (showavgstock)
                        query_str += "SELECT " + select.Rows[0]["grc_m_select"].ToString() + ",cast(prate as numeric(32,2)) as prate from View_stock_avg inner join inv_item_reg on inv_item_reg.ir_id=View_stock_avg.ir_id inner join acc_subhead on as_id=Sup left join inv_category ic on inv_item_reg.ir_category_id=ic.c_id left join inv_mfr im on inv_item_reg.ir_mfr_id=im.m_id left join gnl_location on gl_id=location_id " + sql_where + " order by inv_item_reg.ir_name asc, exp_date desc";
                    else
                        query_str += "SELECT " + select.Rows[0]["grc_m_select"].ToString() + ",cast(prate as numeric(32,2)) as prate from View_Stock inner join inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id inner join acc_subhead on as_id=Sup left join inv_category ic on inv_item_reg.ir_category_id=ic.c_id left join inv_barcode ib on View_Stock.uniquecode=ib.b_uniquecode left join gnl_location on gl_id=location_id " + sql_where + " order by inv_item_reg.ir_name asc, exp_date desc";
                }
            }



            DataTable dt = usqlre.dbReaderFill(query_str);

            // Only ADMIN can see the actual purchase rate.
            // For all other authorized users, show prate as 0.
            if (!string.Equals(usqlre.user_role, "ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                if (dt != null && dt.Columns.Contains("prate"))
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        row["prate"] = 0;
                    }
                }
            }

            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

            hash.Add("stock", dt);

            DataTable dts = new DataTable();
            dts.Columns.Add("column");
            dts.Columns.Add("columnas");
            dts.Columns.Add("width");
            dts.Columns.Add("align");

            if (select.Rows.Count > 0)
            {
                DataRow row = dts.NewRow();
                row["column"] = select.Rows[0]["grc_m_column_name"].ToString();
                row["columnas"] = select.Rows[0]["grc_m_as_name"].ToString();
                row["width"] = select.Rows[0]["grc_m_width"].ToString();
                row["align"] = select.Rows[0]["grc_m_align"].ToString();
                dts.Rows.Add(row);
            }

            hash.Add("select", dts);

            usqlre.close();
            return Ok(ReportModelContext.searializeDt(hash));
        }
        [HttpPost("stock-report-with-multiunit")]
        public async Task<IActionResult> stockReportwithMultiunit([FromBody] StockReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            DataTable select = usqlre.getReportColumns("Stock");
            string sql_where = "";
            if (model.itemName > 0)
            {
                if (sql_where == "")
                {
                    sql_where += " where inv_item_reg.ir_id = " + model.itemName + "";
                }
                else
                {
                    sql_where += " and inv_item_reg.ir_id = " + model.itemName + " ";
                }
            }

            if (!model.showall)
            {
                if (sql_where == "")
                {
                    sql_where += " where qty > 0 ";
                }
                else
                {
                    sql_where += " and qty > 0 ";
                }

            }

            if (model.location > 0)
            {
                if (sql_where == "")
                {
                    sql_where += " where location_id = " + model.location + "";
                }
                else
                {
                    sql_where += " and location_id = " + model.location + "";
                }

            }
            if (model.category > 0)
            {
                if (sql_where == "")
                    sql_where += " where inv_item_reg.ir_category_id ='" + model.category + "'";
                else
                    sql_where += " and inv_item_reg.ir_category_id ='" + model.category + "'";
            }
            if (model.brand > 0)
            {
                string sql = "select bra_name from inv_brand where bra_id=" + model.brand + "";
                DataTable dt_brand = usqlre.dbReaderFill(sql);
                usqlre.close();
                if (dt_brand.Rows.Count > 0)
                {
                    if (sql_where == "")
                        sql_where += " where brand ='" + dt_brand.Rows[0]["bra_name"].ToString() + "'";
                    else
                        sql_where += " and brand ='" + dt_brand.Rows[0]["bra_name"].ToString() + "'";
                }
            }

            if (usqlre.get_android_settings("brand_from_inv_barcode"))
            {
                if (model.brandName != "")
                {
                    if (sql_where == "")
                        sql_where += " where ib.b_brand ='" + model.brandName + "'";
                    else
                        sql_where += " and ib.b_brand ='" + model.brandName + "'";
                }
                else
                {
                    if (model.brand > 0)
                    {
                        if (sql_where == "")
                            sql_where += " where inv_item_reg.ir_mfr_id ='" + model.brand + "'";
                        else
                            sql_where += " where inv_item_reg.ir_mfr_id ='" + model.brand + "'";
                    }
                }
            }

            if (model.itemCode != "0")
            {
                if (sql_where == "")
                    sql_where += " where inv_item_reg.ir_code ='" + model.itemCode + "'";
                else
                    sql_where += " and inv_item_reg.ir_code ='" + model.itemCode + "'";

            }

            bool showavgstock = usqlre.get_android_settings("showavgstock");


            if (model.intBarcode != "")
            {

                if (showavgstock)
                {
                    if (sql_where == "")
                        sql_where += " where View_stock_avg.int_barcode ='" + model.intBarcode + "'";
                    else
                        sql_where += " and View_stock_avg.int_barcode ='" + model.intBarcode + "'";
                }
                else
                {
                    if (sql_where == "")
                        sql_where += " where  View_stock.int_barcode ='" + model.intBarcode + "'";
                    else
                        sql_where += " and  View_stock.int_barcode ='" + model.intBarcode + "'";
                }
            }


            if (model.barcode != "")
            {
                if (showavgstock)
                {
                    if (sql_where == "")
                        sql_where += " where uniquecode = " + model.barcode + "";
                    else
                        sql_where += " and uniquecode = " + model.barcode + "";
                }
                else
                {
                    if (sql_where == "")
                        sql_where += " where  uniquecode = " + model.barcode + "";
                    else
                        sql_where += " and  uniquecode = " + model.barcode + "";
                }

            }

            string query_str = "";

            if (sql_where == "")
            {
                query_str += "SELECT ir_name as [Item Name],cast(qty as numeric(32,2)) as Qty, cast(wholesale as numeric(32,2)) as WSale,ir_code,im_conversion,u_name from View_Stock inner join inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id inner join acc_subhead on as_id=Sup left join inv_category ic on inv_item_reg.ir_category_id=ic.c_id left join inv_barcode ib on View_Stock.uniquecode=ib.b_uniquecode left join inv_multi_unit on im_ir_id = inv_item_reg.ir_id inner join inv_unit on im_unit_id = u_id  order by exp_date desc";
            }
            else
            {
                query_str += "SELECT ir_name as [Item Name],cast(qty as numeric(32,2)) as Qty, cast(wholesale as numeric(32,2)) as WSale,ir_code,im_conversion,u_name from View_Stock inner join inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id inner join acc_subhead on as_id=Sup left join inv_category ic on inv_item_reg.ir_category_id=ic.c_id left join inv_barcode ib on View_Stock.uniquecode=ib.b_uniquecode left join inv_multi_unit on im_ir_id = inv_item_reg.ir_id inner join inv_unit on im_unit_id = u_id " + sql_where + " order by exp_date desc";
            }
            DataTable dt = usqlre.dbReaderFill(query_str);
            dt.Columns.Add("str_newtex", typeof(string));

            if (dt.Rows.Count > 0)
            {
                string str_newtex = "";
                decimal dec_minus_qty = 0;

                for (int j = 0; j < dt.Rows.Count; j++)
                {
                    decimal dec_minus_conversion_qty = 0;
                    decimal dec_qty = Convert.ToDecimal(dt.Rows[j]["Qty"].ToString());
                    decimal dec_conversion = Convert.ToDecimal(dt.Rows[j]["im_conversion"].ToString());
                    string str_conversion_unit = dt.Rows[j]["u_name"].ToString();

                    decimal dec_conversionqty = j == 0 ? (decimal)dec_qty / dec_conversion : (decimal)dec_minus_qty / dec_conversion;
                    dec_conversionqty = Math.Floor(dec_conversionqty);

                    if (string.IsNullOrEmpty(str_newtex))
                    {
                        str_newtex = dec_conversionqty.ToString() + " " + str_conversion_unit;
                    }
                    else
                    {
                        str_newtex += " " + dec_conversionqty.ToString() + " " + str_conversion_unit;
                    }

                    dec_minus_conversion_qty = dec_conversion * dec_conversionqty;
                    dec_minus_qty = j == 0 ? dec_qty - dec_minus_conversion_qty : dec_minus_qty - dec_minus_conversion_qty;

                    dt.Rows[j]["str_newtex"] = str_newtex;
                }
            }
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

            hash.Add("stock", dt);

            DataTable dts = new DataTable();
            dts.Columns.Add("column");
            dts.Columns.Add("columnas");
            dts.Columns.Add("width");
            dts.Columns.Add("align");

            if (select.Rows.Count > 0)
            {
                DataRow row = dts.NewRow();
                row["column"] = select.Rows[0]["grc_m_column_name"].ToString();
                row["columnas"] = select.Rows[0]["grc_m_as_name"].ToString();
                row["width"] = select.Rows[0]["grc_m_width"].ToString();
                row["align"] = select.Rows[0]["grc_m_align"].ToString();
                dts.Rows.Add(row);
            }

            hash.Add("select", dts);

            usqlre.close();
            return Ok(ReportModelContext.searializeDt(hash));
        }

        //[HttpPost("balance-sheet")]
        //public async Task<IActionResult> balanceSheet([FromBody] ReportDatesModel model)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);

        //    string sql = " ";
        //    sql += " EXEC [dbo].[Sp_acc_balance_sheet] ";
        //    sql += " @wi_from_date = '" + model.fromDate + "', ";
        //    sql += " @wi_to_date = '" + model.toDate + "', ";
        //    sql += " @StatementType = N'" + model.statement + "' ";

        //    DataSet dt = usqlre.dbreadDataset(sql);
        //    usqlre.close();
        //    return Ok(ReportModelContext.searializeDt(dt));
        //}




        [HttpPost("get-order-invoice")]
        public async Task<IActionResult> getOrderInvoice([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = "SELECT max(si_entryno) as max_entry_no,min(si_entryno) as min_entry_no from inv_sales_inf WHERE si_str_id=3";

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }


        [HttpGet("old-balance/{id}")]
        public string oldBalance(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            bool ENABLELEND = false;
            string sql = "";
            DataTable dt_settings = usqlre.dbReaderFill("SELECT ans_status FROM dbo.android_settings where ans_name ='ENABLE LEND'");
            ENABLELEND = Convert.ToBoolean(Convert.ToInt32(dt_settings.Rows[0][0].ToString()));
            if (!ENABLELEND)
                sql = "SELECT ISNULL(cast(sum(at_Dr)-sum(at_Cr) as nvarchar(100)),'0') Balance,max((as_name+as_add1)) as address,as_mob as mobile,as_tin as tin,MAX('0') as Lend from acc_subhead left join acc_account_transactions on as_id=at_as_id where as_id=" + id + " group by as_id,as_mob,as_tin";
            else
                sql = @"select Balance,a.address,mobile,tin,Lend from(SELECT ISNULL(cast(sum(at_Dr)-sum(at_Cr) as nvarchar(100)),'0') Balance,
                        max((as_name + as_add1)) as address,as_mob as mobile,
                        as_tin as tin,as_id
                        from acc_subhead
                        left join acc_account_transactions on as_id = at_as_id
                        where as_id = " + id + @" group by as_id,as_mob,as_tin)a
                        left join(select ISNULL(cast(sum(li_out) - sum(li_in) as nvarchar(100)), '0') as Lend, as_id
                        from acc_subhead
                        left join inv_lend_item_transactions on as_id = li_as_id
                        where as_id = " + id + " group by as_id)b on b.as_id = a.as_id";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }

        [HttpPost("search-district")]
        public async Task<IActionResult> searchDistrict([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = "";
            if (model.Value == "")
            {
                sql = "SELECT top 10 dct_name as label,cast(dct_id as Int) as value from inv_district";
            }
            else
            {
                sql = "SELECT top 10 dct_name as label,cast(dct_id as Int) as value from inv_district where dct_name like '%" + model.Value + "%'";
            }

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }

        [HttpPost("search-area")]
        public async Task<IActionResult> searchArea([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";

            if (model.Value == "")
            {
                sql = "SELECT top 10 area_name as label,area_id as value from acc_area ";
            }
            else
            {
                sql = "SELECT area_name as label,area_id as value from acc_area where area_name like '%" + model.Value + "%'";
            }

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }

        [HttpGet("sales-type")]
        public string getsalesType()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT str_name as label,cast(str_id as int) as value from inv_sales_type_reg";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }

        [HttpPost("search-cash")]
        public async Task<IActionResult> searchCash([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";

            if (model.Value == "")
            {
                sql = "SELECT top 10 as_name as label,cast(as_id as Int) as value from acc_subhead where as_ap_id='1' or as_ap_id='2'";
            }
            else
            {
                sql = "SELECT as_name as label,cast(as_id as Int) as value from acc_subhead where (as_ap_id='1' or  as_ap_id='2') and as_name like '%" + model.Value + "%'";
            }

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }

        

        [HttpGet("company-details")]
        public string companyDetails()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT * from gnl_company";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }

        [HttpPost("update-logo")]
        public IActionResult UpdateCompanyLogo(IFormFile imageFile)
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
                    bool logoUpdated = usqlre.UpdateCompanyLogo(imageBytes);

                    if (logoUpdated)
                    {
                        return Ok(ReportModelContext.searializeDt(new { status = true }));
                    }
                    else
                    {
                        return NotFound(new { status=false,Message= "No company found or logo not updated" });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("company-logo")]
        public IActionResult GetCompanyLogo()
        {
            try
            {
              
                UserSqlServer usqlre = new UserSqlServer(this);
                DataTable dt_comlogo = usqlre.dbReaderFill("select com_logo from gnl_company");
                byte[] imageBytes = (byte[])dt_comlogo.Rows[0][0];

               
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    return NotFound(new {Status=false,Message= "No company logo found" });
                }

                
                return File(imageBytes, "image/jpeg");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("day-book")]
        public async Task<IActionResult> dayBook([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT top 10 as_id,'active' as status from acc_subhead where as_ap_id = 14";

            String statement = "daybookDGV";
            String query = "EXEC [dbo].[Sp_acc_report]\n" +
                    "\t\t@wi_from_date = '" + model.fromDate + "',\n" +
                    "\t\t@wi_to_date = '" + model.toDate + "',\n" +
                    //"\t\t@led_id = '" + model.asId + "',\n" +
                    "\t\t@balaceonly = 0,\n" +
                    "\t\t@groupsales = " + model.groupSales + ",\n" +
                    "\t\t@StatementType = N'" + statement + "'";

            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }

        [HttpPost("dashboard-mobile")]
        public async Task<IActionResult> dashboardMobile([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = null;
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            try
            {
              
                    String statement = "api_dashboard_mobile";
                    usqlre = new UserSqlServer(this);
                    SqlCommand cmd = new SqlCommand("Sp_api_dashboard", usqlre.shop);
                    cmd.Parameters.AddWithValue("@fromDate", model.fromDate);
                    cmd.Parameters.AddWithValue("@salesManId", Convert.ToInt32(usqlre.gu_acc_id));
                    cmd.Parameters.AddWithValue("@user_Id", Convert.ToInt32(usqlre.userId));
                    cmd.Parameters.AddWithValue("@user_role", usqlre.user_role);
                    cmd.Parameters.AddWithValue("@StatementType", statement);
                    cmd.CommandType = CommandType.StoredProcedure;

                DataTable _temp = new DataTable();
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(_temp);
                return Ok(ReportModelContext.searializeDt(_temp));

                //DataSet main_ds = new DataSet();
                //SqlDataAdapter adp = new SqlDataAdapter(cmd);
                //adp.Fill(main_ds);

                //bool ENABLESOFTWAREASWORKSHOP = false;
                //DataTable dt_settings = usqlre.dbReaderFill("select gs_status from gnl_settings where gs_value = 'ENABLESOFTWAREASWORKSHOP'");
                //ENABLESOFTWAREASWORKSHOP = Convert.ToBoolean(Convert.ToInt32(dt_settings.Rows[0][0].ToString()));
                //    if (!ENABLESOFTWAREASWORKSHOP)
                //    {
                //        hash.Add("data", main_ds.Tables[1]);
                //    }
                //    else
                //    {
                //        hash.Add("data", main_ds.Tables[0]);
                //    }



               // return Ok(ReportModelContext.searializeDt(hash));


            }
            catch (Exception e)
            {
                return Ok(new { status = false, message =e.ToString()});
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }
            
        }



        [HttpPost("dashboard-web")]
        public async Task<IActionResult> dashboardWeb([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String statement = "api_dashboard";
            String query = "EXEC [dbo].[Sp_api_dashboard]\n" +
                   "\t\t@fromDate = '" + model.fromDate + "',\n" +
                   "\t\t@toDate = '" + model.toDate + "',\n" +
                   //"\t\t@led_id = '" + model.asId + "',\n" +
                   //"\t\t@balaceonly = 0,\n" +
                   "\t\t@StatementType = N'" + statement + "'";

            DataSet dt = usqlre.dbreadDataset(query);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            hash.Add("Report", dt.Tables["Table"]);
            hash.Add("Top3Salesman", dt.Tables["Table1"]);
            hash.Add("Top10sellingProducT", dt.Tables["Table2"]);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(hash));
        }

        [HttpGet("search-rate")]
        public string searchRate()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT cast(rt_id as Int) as value, rt_name_display as label, rt_name FROM inv_rate_type ORDER BY rt_order";
            DataTable allSalesType = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(allSalesType);
        }

        [HttpPost("balance-sheet")]
        public string balanceSheet(BalanceSheetModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            decimal net = 0, netlia = 0, netasset = 0;
            int count1 = 0, count2 = 0;
            DataSet ds = new DataSet();

            DataTable reportTable = new DataTable();
            reportTable.Columns.Add("Liabilities");
            reportTable.Columns.Add("lia_amount");
            reportTable.Columns.Add("Assets");
            reportTable.Columns.Add("as_amount");
            reportTable.Columns.Add("lia_id");
            reportTable.Columns.Add("as_id");


            if (model.opening)
            {
                ds = usqlre.balanceSheetFillDataSet("OB", model);
            }
            else
            {
                ds = usqlre.balanceSheetFillDataSet(model.type, model);
            }



            if (model.type == "Group")
            {
                for (int i = 0; i <= ds.Tables[2].Rows.Count - 1; i++)
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[0]["Liabilities"] = ds.Tables[2].Rows[i]["name"].ToString(); //LONG TERM LIABILITY
                    reportTable.Rows[0]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[2].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                }

                for (int i = 0; i <= ds.Tables[3].Rows.Count - 1; i++)
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[1]["Liabilities"] = ds.Tables[3].Rows[i]["name"].ToString(); // SHORT TERM LIABILITY
                    reportTable.Rows[1]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round(CommonHelper.stringToDecimal(ds.Tables[3].Rows[i]["amt"].ToString()), CommonHelper.Decimalpoint).ToString());
                }

                for (int i = 0; i <= ds.Tables[0].Rows.Count - 1; i++)
                {
                    reportTable.Rows[0]["Assets"] = ds.Tables[0].Rows[i]["name"].ToString(); // FIXED ASSET
                    reportTable.Rows[0]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round(CommonHelper.stringToDecimal(ds.Tables[0].Rows[i]["amt"].ToString()), CommonHelper.Decimalpoint).ToString());
                }

                for (int i = 0; i <= ds.Tables[1].Rows.Count - 1; i++)
                {
                    reportTable.Rows[1]["Assets"] = ds.Tables[1].Rows[i]["name"].ToString(); // CURRENT ASSET
                    reportTable.Rows[1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[1].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                }

                for (int i = 0; i <= ds.Tables[5].Rows.Count - 1; i++)
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[2]["Assets"] = ds.Tables[5].Rows[i]["name"].ToString(); // CLOSING STOCK
                    reportTable.Rows[2]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[5].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                }

                for (int i = 0; i <= ds.Tables[4].Rows.Count - 1; i++)
                {
                    if (CommonHelper.stringToDecimal(ds.Tables[4].Rows[i]["amt"].ToString()) > 0)
                    {
                        reportTable.Rows.Add();
                        reportTable.Rows[3]["Assets"] = ds.Tables[4].Rows[i]["name"].ToString(); // DEFF IN OPN BALANCE
                        reportTable.Rows[3]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[4].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                    }
                    else
                    {
                        reportTable.Rows.Add();
                        reportTable.Rows[3]["Liabilities"] = ds.Tables[4].Rows[i]["name"].ToString(); // DEFF IN OPN BALANCE
                        reportTable.Rows[3]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[4].Rows[i]["amt"].ToString())) * -1, CommonHelper.Decimalpoint).ToString());
                    }
                }

                if (CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString()) < 0)
                {
                    netlia = CommonHelper.stringToDecimal(ds.Tables[2].Rows[0]["amt"].ToString()) + CommonHelper.stringToDecimal(ds.Tables[3].Rows[0]["amt"].ToString()) - CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString());
                    netasset = CommonHelper.stringToDecimal(ds.Tables[1].Rows[0]["amt"].ToString()) + CommonHelper.stringToDecimal(ds.Tables[0].Rows[0]["amt"].ToString()) + CommonHelper.stringToDecimal(ds.Tables[5].Rows[0]["amt"].ToString());
                }
                else
                {
                    netlia = CommonHelper.stringToDecimal(ds.Tables[2].Rows[0]["amt"].ToString()) + CommonHelper.stringToDecimal(ds.Tables[3].Rows[0]["amt"].ToString());
                    netasset = CommonHelper.stringToDecimal(ds.Tables[1].Rows[0]["amt"].ToString()) + CommonHelper.stringToDecimal(ds.Tables[0].Rows[0]["amt"].ToString()) + CommonHelper.stringToDecimal(ds.Tables[5].Rows[0]["amt"].ToString()) + CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString());
                }

                if (netasset > netlia)
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[4]["Liabilities"] = "Net Profit";
                    reportTable.Rows[4]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((netasset - netlia), CommonHelper.Decimalpoint).ToString());
                    net = netasset;
                }
                else
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[4]["Assets"] = "Net Loss";
                    reportTable.Rows[4]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((netlia - netasset), CommonHelper.Decimalpoint).ToString());
                    net = netlia;
                }
                reportTable.Rows.Add();
                reportTable.Rows[5]["lia_amount"] = "===============";
                reportTable.Rows[5]["as_amount"] = "===============";
                reportTable.Rows.Add();
                reportTable.Rows[6]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((net), CommonHelper.Decimalpoint).ToString());
                reportTable.Rows[6]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((net), CommonHelper.Decimalpoint).ToString());
                reportTable.Rows.Add();
                reportTable.Rows[7]["lia_amount"] = "===============";
                reportTable.Rows[7]["as_amount"] = "===============";

            }
            else if (model.type == "Group & Ledger")
            {
                reportTable.Rows.Clear();
                reportTable.Rows.Add();
                count2 = 0;
                reportTable.Rows[count2]["Liabilities"] = "CAPITAL";
                if (model.adjustOB)
                {
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString()) + CommonHelper.stringToDecimal(ds.Tables[11].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                }
                else
                {
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                }
                count2 = 1;
                if (CommonHelper.stringToDecimal(ds.Tables[6].Rows[0][0].ToString()) > 0)
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[count2]["Liabilities"] = "NET PROFIT";
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[6].Rows[0][0].ToString())), CommonHelper.Decimalpoint).ToString());
                }
                else
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[count2]["Liabilities"] = "NET LOSS";
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[6].Rows[0][1].ToString()) * -1), CommonHelper.Decimalpoint).ToString());
                }
                count2 = 2;
                reportTable.Rows.Add();
                reportTable.Rows[count2]["Liabilities"] = "LONG TERM LIABILITY";
                count2 = 3;
                for (int i = 0; i <= ds.Tables[2].Rows.Count - 1; i++)
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[count2]["Liabilities"] = "   " + ds.Tables[2].Rows[i]["ledName"].ToString();
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[2].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                    reportTable.Rows[count2]["lia_id"] = ds.Tables[2].Rows[i]["id"].ToString();
                    count2 = count2 + 1;
                }

                reportTable.Rows.Add();
                reportTable.Rows[count2]["Liabilities"] = "CURRENT LIABILITY";
                count2 = count2 + 1;
                for (int i = 0; i <= ds.Tables[3].Rows.Count - 1; i++)
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[count2]["Liabilities"] = "   " + ds.Tables[3].Rows[i]["ledName"].ToString();
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[3].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                    reportTable.Rows[count2]["lia_id"] = ds.Tables[3].Rows[i]["id"].ToString();
                    count2 = count2 + 1;
                }
                reportTable.Rows.Add();
                reportTable.Rows[count2]["Liabilities"] = "TDS PAYABLE";
                reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[7].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count2 = count2 + 1;

                reportTable.Rows.Add();
                reportTable.Rows[count2]["Liabilities"] = "TAX PAYABLE";
                reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[10].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count2 = count2 + 1;

                count1 = 0;
                reportTable.Rows[count1]["Assets"] = "FIXED ASSET";
                count1 = 1;
                for (int i = 0; i <= ds.Tables[0].Rows.Count - 1; i++)
                {
                    if (count1 < reportTable.Rows.Count)
                    {
                    }
                    else
                    {
                        reportTable.Rows.Add();
                    }
                    reportTable.Rows[i + 1]["Assets"] = "   " + ds.Tables[0].Rows[i]["ledName"].ToString();
                    reportTable.Rows[i + 1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[0].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                    reportTable.Rows[i + 1]["as_id"] = ds.Tables[0].Rows[i]["id"].ToString();
                    count1 = count1 + 1;
                }

                if (count1 < reportTable.Rows.Count)
                {
                }
                else
                {
                    reportTable.Rows.Add();
                    count1 = count1 + 1;
                }
                reportTable.Rows[count1]["Assets"] = "CURRENT ASSET";
                count1 = count1 + 1;
                for (int i = 0; i <= ds.Tables[1].Rows.Count - 1; i++)
                {
                    if (count1 < reportTable.Rows.Count)
                    {
                    }
                    else
                    {
                        reportTable.Rows.Add();
                    }
                    reportTable.Rows[count1]["Assets"] = "   " + ds.Tables[1].Rows[i]["ledName"].ToString();
                    reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[1].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                    reportTable.Rows[count1]["as_id"] = ds.Tables[1].Rows[i]["id"].ToString();
                    count1 = count1 + 1;
                }

                reportTable.Rows.Add();
                reportTable.Rows[count1]["Assets"] = ds.Tables[5].Rows[0]["name"].ToString(); // CLOSING STOCK
                reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[5].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count1 = count1 + 1;

                reportTable.Rows.Add();
                reportTable.Rows[count1]["Assets"] = "TDS RECIEVABLE";
                reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[8].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count1 = count1 + 1;

                reportTable.Rows.Add();
                reportTable.Rows[count1]["Assets"] = "TAX RECIEVABLE";
                reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[9].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count1 = count1 + 1;

                if (count1 > count2)
                {

                }
                else
                {
                    count1 = count2;
                }


                if (CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString()) < 0)
                {
                    //if(ds.Tables[0].Rows.Count > 0)
                    try
                    {
                        decimal sum = 0;
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            sum += CommonHelper.stringToDecimal(dr["amt"].ToString());
                        }
                        decimal sum1 = 0;
                        foreach (DataRow dr in ds.Tables[1].Rows)
                        {
                            sum1 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                        }
                        decimal sum2 = 0;
                        foreach (DataRow dr in ds.Tables[2].Rows)
                        {
                            sum2 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                        }
                        decimal sum3 = 0;
                        foreach (DataRow dr in ds.Tables[3].Rows)
                        {
                            sum3 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                        }
                        decimal sum4 = 0;
                        foreach (DataRow dr in ds.Tables[5].Rows)
                        {
                            sum4 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                        }
                        netlia = sum2 + sum3 - CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString());
                        netasset = sum + sum1 + sum4;
                    }
                    catch (Exception ex)
                    {

                    }
                }
                else
                {
                    decimal sum = 0;
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        sum += CommonHelper.stringToDecimal(dr["amt"].ToString());
                    }
                    decimal sum1 = 0;
                    foreach (DataRow dr in ds.Tables[1].Rows)
                    {
                        sum1 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                    }
                    decimal sum2 = 0;
                    foreach (DataRow dr in ds.Tables[2].Rows)
                    {
                        sum2 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                    }
                    decimal sum3 = 0;
                    foreach (DataRow dr in ds.Tables[3].Rows)
                    {
                        sum3 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                    }
                    decimal sum4 = 0;
                    foreach (DataRow dr in ds.Tables[5].Rows)
                    {
                        sum4 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                    }
                    netlia = sum2 + sum3;
                    netasset = sum1 + sum + sum4 + CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString());
                }
                decimal debitsum = 0, creditsum = 0;
                for (int i = 0; i <= reportTable.Rows.Count - 1; i++)
                {
                    try
                    {
                        if (CommonHelper.stringToDecimal(reportTable.Rows[i]["lia_amount"].ToString()) != 0)
                        {
                            debitsum = debitsum + CommonHelper.stringToDecimal(reportTable.Rows[i]["lia_amount"].ToString());
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                for (int j = 0; j <= reportTable.Rows.Count - 1; j++)
                {
                    try
                    {
                        if (CommonHelper.stringToDecimal(reportTable.Rows[j]["as_amount"].ToString()) != 0)
                        {
                            creditsum = creditsum + CommonHelper.stringToDecimal(reportTable.Rows[j]["as_amount"].ToString());
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (creditsum > debitsum)
                {
                    if (count1 < reportTable.Rows.Count)
                    {
                    }
                    else
                    {
                        reportTable.Rows.Add();
                    }
                    reportTable.Rows[count1]["Liabilities"] = "Suspense A/C";
                    reportTable.Rows[count1]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((creditsum - debitsum), CommonHelper.Decimalpoint).ToString());
                    debitsum = creditsum;
                    count1 = count1 + 1;
                }
                else
                {
                    if (count1 < reportTable.Rows.Count)
                    {
                    }
                    else
                    {
                        reportTable.Rows.Add();
                    }
                    reportTable.Rows[count1]["Assets"] = "Suspense A/C";
                    reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((debitsum - creditsum), CommonHelper.Decimalpoint).ToString());
                    creditsum = debitsum;
                    count1 = count1 + 1;
                }
                reportTable.Rows.Add();
                reportTable.Rows[count1]["lia_amount"] = "===============";
                reportTable.Rows[count1]["as_amount"] = "===============";

                reportTable.Rows.Add();
                count1 = count1 + 1;
                reportTable.Rows[count1]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((debitsum), CommonHelper.Decimalpoint).ToString());
                reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((creditsum), CommonHelper.Decimalpoint).ToString());
                count1 = count1 + 1;
                reportTable.Rows.Add();
                reportTable.Rows[count1]["lia_amount"] = "===============";
                reportTable.Rows[count1]["as_amount"] = "===============";

            }
            else if (model.type == "ProductionModel")
            {
                decimal liability_total = 0, assets_total = 0;
                reportTable.Rows.Clear();
                reportTable.Rows.Add();
                count2 = 0;
                reportTable.Rows[count2]["Liabilities"] = "CAPITAL";
                if (model.adjustOB)
                {
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString()) + CommonHelper.stringToDecimal(ds.Tables[11].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                }
                else
                {
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                }
                count2 = 1;
                if (CommonHelper.stringToDecimal(ds.Tables[6].Rows[0][0].ToString()) > 0)
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[count2]["Liabilities"] = "NET PROFIT";
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[6].Rows[0][0].ToString())), CommonHelper.Decimalpoint).ToString());
                }
                else
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[count2]["Liabilities"] = "NET LOSS";
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[6].Rows[0][1].ToString()) * -1), CommonHelper.Decimalpoint).ToString());
                }
                count2 = 2;
                reportTable.Rows.Add();
                reportTable.Rows[count2]["Liabilities"] = "LONG TERM LIABILITY";
                count2 = 3;
                for (int i = 0; i <= ds.Tables[2].Rows.Count - 1; i++)
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[count2]["Liabilities"] = "   " + ds.Tables[2].Rows[i]["ledName"].ToString();
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[2].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                    reportTable.Rows[count2]["lia_id"] = ds.Tables[2].Rows[i]["id"].ToString();
                    count2 = count2 + 1;
                }

                reportTable.Rows.Add();
                reportTable.Rows[count2]["Liabilities"] = "CURRENT LIABILITY";
                count2 = count2 + 1;
                for (int i = 0; i <= ds.Tables[3].Rows.Count - 1; i++)
                {
                    reportTable.Rows.Add();
                    reportTable.Rows[count2]["Liabilities"] = "   " + ds.Tables[3].Rows[i]["ledName"].ToString();
                    reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[3].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                    reportTable.Rows[count2]["lia_id"] = ds.Tables[3].Rows[i]["id"].ToString();
                    count2 = count2 + 1;
                }
                //TOTAL OF LIABILITIES
                reportTable.Rows.Add();
                reportTable.Rows[count2]["Liabilities"] = "TOTAL LIABILITY";
                reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((liability_total), CommonHelper.Decimalpoint).ToString());
                count2 = count2 + 1;

                reportTable.Rows.Add();
                reportTable.Rows[count2]["Liabilities"] = "TDS PAYABLE";
                reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[7].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count2 = count2 + 1;

                reportTable.Rows.Add();
                reportTable.Rows[count2]["Liabilities"] = "TAX PAYABLE";
                reportTable.Rows[count2]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[10].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count2 = count2 + 1;

                count1 = 0;
                reportTable.Rows[count1]["Assets"] = "FIXED ASSET";
                count1 = 1;
                for (int i = 0; i <= ds.Tables[0].Rows.Count - 1; i++)
                {
                    if (count1 < reportTable.Rows.Count)
                    {
                    }
                    else
                    {
                        reportTable.Rows.Add();
                    }
                    reportTable.Rows[i + 1]["Assets"] = "   " + ds.Tables[0].Rows[i]["ledName"].ToString();
                    reportTable.Rows[i + 1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[0].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                    reportTable.Rows[i + 1]["as_id"] = ds.Tables[0].Rows[i]["id"].ToString();
                    count1 = count1 + 1;
                }

                if (count1 < reportTable.Rows.Count)
                {
                }
                else
                {
                    reportTable.Rows.Add();
                    count1 = count1 + 1;
                }
                reportTable.Rows[count1]["Assets"] = "CURRENT ASSET";
                count1 = count1 + 1;
                for (int i = 0; i <= ds.Tables[1].Rows.Count - 1; i++)
                {
                    if (count1 < reportTable.Rows.Count)
                    {
                    }
                    else
                    {
                        reportTable.Rows.Add();
                    }
                    reportTable.Rows[count1]["Assets"] = "   " + ds.Tables[1].Rows[i]["ledName"].ToString();
                    reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[1].Rows[i]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                    reportTable.Rows[count1]["as_id"] = ds.Tables[1].Rows[i]["id"].ToString();
                    count1 = count1 + 1;
                }
                //TOTAL OF ASSETS
                reportTable.Rows.Add();
                reportTable.Rows[count2]["Assets"] = "TOTAL ASSETS";
                reportTable.Rows[count2]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((assets_total), CommonHelper.Decimalpoint).ToString());
                count1 = count1 + 1;

                reportTable.Rows.Add();
                reportTable.Rows[count1]["Assets"] = ds.Tables[5].Rows[0]["name"].ToString(); // CLOSING STOCK
                reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[5].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count1 = count1 + 1;

                reportTable.Rows.Add();
                reportTable.Rows[count1]["Assets"] = "   " + ds.Tables[12].Rows[0]["name"].ToString(); // RAWMATERIALS CLOSING STOCK
                reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[12].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count1 = count1 + 1;

                reportTable.Rows.Add();
                reportTable.Rows[count1]["Assets"] = "   " + ds.Tables[13].Rows[0]["name"].ToString(); // FINISHED GOODS CLOSING STOCK
                reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[13].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count1 = count1 + 1;

                reportTable.Rows.Add();
                reportTable.Rows[count1]["Assets"] = "TDS RECIEVABLE";
                reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[8].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count1 = count1 + 1;

                reportTable.Rows.Add();
                reportTable.Rows[count1]["Assets"] = "TAX RECIEVABLE";
                reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((CommonHelper.stringToDecimal(ds.Tables[9].Rows[0]["amt"].ToString())), CommonHelper.Decimalpoint).ToString());
                count1 = count1 + 1;

                if (count1 > count2)
                {

                }
                else
                {
                    count1 = count2;
                }

                if (CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString()) < 0)
                {
                    //if(ds.Tables[0].Rows.Count > 0)
                    try
                    {
                        decimal sum = 0;
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            sum += CommonHelper.stringToDecimal(dr["amt"].ToString());
                        }
                        decimal sum1 = 0;
                        foreach (DataRow dr in ds.Tables[1].Rows)
                        {
                            sum1 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                        }
                        decimal sum2 = 0;
                        foreach (DataRow dr in ds.Tables[2].Rows)
                        {
                            sum2 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                        }
                        decimal sum3 = 0;
                        foreach (DataRow dr in ds.Tables[3].Rows)
                        {
                            sum3 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                        }
                        decimal sum4 = 0;
                        foreach (DataRow dr in ds.Tables[5].Rows)
                        {
                            sum4 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                        }
                        netlia = sum2 + sum3 - CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString());
                        netasset = sum + sum1 + sum4;
                    }
                    catch (Exception ex)
                    {

                    }
                }
                else
                {
                    decimal sum = 0;
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        sum += CommonHelper.stringToDecimal(dr["amt"].ToString());
                    }
                    decimal sum1 = 0;
                    foreach (DataRow dr in ds.Tables[1].Rows)
                    {
                        sum1 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                    }
                    decimal sum2 = 0;
                    foreach (DataRow dr in ds.Tables[2].Rows)
                    {
                        sum2 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                    }
                    decimal sum3 = 0;
                    foreach (DataRow dr in ds.Tables[3].Rows)
                    {
                        sum3 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                    }
                    decimal sum4 = 0;
                    foreach (DataRow dr in ds.Tables[5].Rows)
                    {
                        sum4 += CommonHelper.stringToDecimal(dr["amt"].ToString());
                    }
                    netlia = sum2 + sum3;
                    netasset = sum1 + sum + sum4 + CommonHelper.stringToDecimal(ds.Tables[4].Rows[0]["amt"].ToString());
                }
                decimal debitsum = 0, creditsum = 0;
                for (int i = 0; i <= reportTable.Rows.Count - 1; i++)
                {
                    try
                    {
                        if (CommonHelper.stringToDecimal(reportTable.Rows[i]["lia_amount"].ToString()) != 0)
                        {
                            debitsum = debitsum + CommonHelper.stringToDecimal(reportTable.Rows[i]["lia_amount"].ToString());
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                for (int j = 0; j <= reportTable.Rows.Count - 1; j++)
                {
                    try
                    {
                        if (CommonHelper.stringToDecimal(reportTable.Rows[j]["as_amount"].ToString()) != 0)
                        {
                            creditsum = creditsum + CommonHelper.stringToDecimal(reportTable.Rows[j]["as_amount"].ToString());
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (creditsum > debitsum)
                {
                    if (count1 < reportTable.Rows.Count)
                    {
                    }
                    else
                    {
                        reportTable.Rows.Add();
                    }
                    reportTable.Rows[count1]["Liabilities"] = "Suspense A/C";
                    reportTable.Rows[count1]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((creditsum - debitsum), CommonHelper.Decimalpoint).ToString());
                    debitsum = creditsum;
                    count1 = count1 + 1;
                }
                else
                {
                    if (count1 < reportTable.Rows.Count)
                    {
                    }
                    else
                    {
                        reportTable.Rows.Add();
                    }
                    reportTable.Rows[count1]["Assets"] = "Suspense A/C";
                    reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((debitsum - creditsum), CommonHelper.Decimalpoint).ToString());
                    creditsum = debitsum;
                    count1 = count1 + 1;
                }

                reportTable.Rows.Add();
                reportTable.Rows[count1]["lia_amount"] = "===============";
                reportTable.Rows[count1]["as_amount"] = "===============";
                reportTable.Rows.Add();
                count1 = count1 + 1;
                reportTable.Rows[count1]["lia_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((debitsum), CommonHelper.Decimalpoint).ToString());
                reportTable.Rows[count1]["as_amount"] = CommonHelper.SeperateNumberbycomma(System.Math.Round((creditsum), CommonHelper.Decimalpoint).ToString());
                count1 = count1 + 1;
                reportTable.Rows.Add();
                reportTable.Rows[count1]["lia_amount"] = "===============";
                reportTable.Rows[count1]["as_amount"] = "===============";
            }
            usqlre.close();
            return ReportModelContext.searializeDt(reportTable);
        }


        [HttpGet("all-permission")]
        public string allPermission()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "select am_as_name as label,am_id as value from android_menu where am_other=1";
            DataTable permisions = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(permisions);

        }
   

        [HttpGet("get-permission/{id}")]
        public string getPermission(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "select am_as_name as label,ap_m_id as value from android_permission_menu inner join android_menu on ap_m_id = am_id  where am_other=1 and android_permission_menu.ap_gu_user_id=" + id + " and android_permission_menu.ap_other_status=1";
            DataTable permisions = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(permisions);
        }

        [HttpPost("save-permission")]
        public async Task<IActionResult> savePermission([FromBody] PermissionModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            bool status = usqlre.savePermission("save_permission", model);
            usqlre.close();
            if (status)
            {
                return Ok(new { status = true});
            }
            else
            {
                return Ok(new { status = false });
            }
        }


        [HttpGet("loyalty-details/{mobile}")]
        public string getPermission(string mobile)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "select lc_mob as mobile,lc_id as accNo,lc_cardno as cardNo,lc_name as name,lc_add1,lc_category,lc_whatsapp,lc_email,lc_dob from acc_loyalty_card where lc_mob = '"+mobile+"'";
            DataTable permisions = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(permisions);
        }

        //[HttpPost("sasdfve-loyalty")]
        //public async Task<IActionResult> saveLoyalty([FromBody] SpLoyality model)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);
        //    string sql = "";
        //    if (model != null)
        //    {
        //        if (model.statement == "Insert")
        //        {
        //            sql = "insert into acc_loyalty_card(lc_mob,lc_cardno,lc_name,lc_add1,lc_category,lc_email,lc_dob) values ('" + model.loyalty.mobile + "','" + model.loyalty.cardNo + "','" + model.loyalty.name + "','" + model.loyalty.add1 + "','" + model.loyalty.category + "','" + model.loyalty.email + "','" + model.loyalty.dob + "')";
        //            DataTable permisions = usqlre.dbReaderFill(sql);
        //            usqlre.close();
        //            return Ok(ReportModelContext.searializeDt(new { status = true }));
        //        }
        //        else if (model.statement == "Update")
        //        {
        //            sql = "update acc_loyalty_card set lc_mob='"+model.loyalty.mobile+ "',lc_cardno='" + model.loyalty.cardNo + "',lc_name='"+model.loyalty.name+ "',lc_add1='"+model.loyalty.add1+ "',lc_category='"+model.loyalty.category+ "',lc_email='"+model.loyalty.email+ "',lc_dob='"+model.loyalty.dob+"' where lc_id="+model.loyalty.accNo+"";
        
        //            usqlre.dbExecute(sql);
        //            usqlre.close();
        //            return Ok(ReportModelContext.searializeDt(new { status = true}));
        //        }
        //    }
        //    usqlre.close();
        //    return Ok(ReportModelContext.searializeDt(new { status = false }));
        //}

        [HttpPost("save-loyalty")]
        public async Task<IActionResult> saveLoyalty([FromBody] SpLoyality model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            if (model != null)
            {
                if (model.statement == "Insert")
                {
                    //using (SqlCommand command = new SqlCommand())
                    //{
                    //    command.Connection = usqlre.shop;            // <== lacking
                    //    command.CommandType = CommandType.Text;
                    //    command.CommandText = "insert into acc_loyalty_card(lc_mob,lc_cardno,lc_name,lc_add1,lc_category,lc_email,lc_dob,lc_whatsapp) values (@lc_mob,@lc_cardno,@lc_name,@lc_add1,@lc_category,@lc_email,@lc_dob,@lc_whatsapp) SELECT SCOPE_IDENTITY()";
                    //    command.Parameters.AddWithValue("@lc_mob", model.loyalty.mobile);
                    //    command.Parameters.AddWithValue("@lc_cardno", model.loyalty.cardNo);
                    //    command.Parameters.AddWithValue("@lc_name", model.loyalty.name);
                    //    command.Parameters.AddWithValue("@lc_add1", model.loyalty.add1);
                    //    command.Parameters.AddWithValue("@lc_category", model.loyalty.category);
                    //    command.Parameters.AddWithValue("@lc_email", model.loyalty.email);
                    //    command.Parameters.AddWithValue("@lc_dob", model.loyalty.dob);
                    //    command.Parameters.AddWithValue("@lc_whatsapp", model.loyalty.whatsapp);

                    //    try
                    //    {
                    //        usqlre.OpenConnection();
                    //        int recordsAffected = command.ExecuteNonQuery();
                    //        return Ok(ReportModelContext.searializeDt(new { status = true, accNo = recordsAffected }));
                    //    }
                    //    catch (SqlException)
                    //    {
                    //        return Ok(ReportModelContext.searializeDt(new { status = false }));
                    //    }
                    //    finally
                    //    {
                    //        usqlre.close();
                    //    }
                    //}
                    //return Ok(ReportModelContext.searializeDt(new { status = false }));

                    sql = "insert into acc_loyalty_card(lc_mob,lc_cardno,lc_name,lc_add1,lc_category,lc_email,lc_dob,lc_whatsapp) values ('"+model.loyalty.mobile+"','"+model.loyalty.cardNo+"','"+model.loyalty.name+"','"+model.loyalty.add1+"','"+model.loyalty.category+"','"+model.loyalty.email+"','"+model.loyalty.dob+"','"+model.loyalty.whatsapp+"') SELECT SCOPE_IDENTITY()";

                    DataTable dt = usqlre.dbReaderFill(sql);
                    if (dt != null)
                    {
                        if (dt.Rows.Count > 0)
                        {
                            int inserId  = Convert.ToInt32(dt.Rows[0]["Column1"].ToString());
                            usqlre.close();
                            return Ok(new { status =  true,inserId = inserId });
                        }

                    }
                    
                }
                else if (model.statement == "Update")
                {
                    sql = "update acc_loyalty_card set lc_mob='" + model.loyalty.mobile + "',lc_cardno='" + model.loyalty.cardNo + "',lc_name='" + model.loyalty.name + "',lc_add1='" + model.loyalty.add1 + "',lc_category='" + model.loyalty.category + "',lc_email='" + model.loyalty.email + "',lc_dob='" + model.loyalty.dob + "',lc_whatsapp='" + model.loyalty.whatsapp + "' where lc_id=" + model.loyalty.accNo + "";

                    usqlre.dbExecute(sql);
                    usqlre.close();
                    return Ok(ReportModelContext.searializeDt(new { status = true }));
                }
            }
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(new { status = false }));
        }

        [HttpGet("app-user-permissions")]
        public string appUserpermissions(string mobile)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "select am_as_name as label,ap_m_id as value from android_permission_menu inner join android_menu on ap_m_id = am_id  where am_other=1 and android_permission_menu.ap_gu_user_id=" + usqlre.userId + " and android_permission_menu.ap_other_status=1";
            DataTable permissionsdt = usqlre.dbReaderFill(sql);
            hash.Add("permissions", permissionsdt);
            sql = "select * from android_settings";
            DataTable android = usqlre.dbReaderFill(sql);
            hash.Add("android", android);
            usqlre.close();
            return ReportModelContext.searializeDt(hash);
        }

        [HttpPost("sync-data")]
        public async Task<IActionResult> syncDatamob()
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                String query = "EXEC [dbo].[Sp_Sync_Data_Mob]\n";
                DataSet dt = usqlre.dbreadDataset(query);
                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
                hash.Add("inv_item_reg", dt.Tables["Table"]);
                hash.Add("inv_unit", dt.Tables["Table1"]);
                hash.Add("inv_multi_unit", dt.Tables["Table2"]);
                hash.Add("old_balance", dt.Tables["Table3"]);
                hash.Add("view_stock", dt.Tables["Table4"]);
                return Ok(ReportModelContext.searializeDt(hash));
            }
            catch (Exception e)
            {
                return Ok(new { status = false, message = e.ToString() });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }
        }

        [HttpPost("search-bank-payment")]
        public async Task<IActionResult> searchPankPayment([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = "";
            if (model.Value == "")
            {
                sql = "select top 10 cast(abpr_entryno as Int) as invoiceNo,as_name as customerName,abpr_total_amount as totalAmount,convert(varchar(11), abpr_date, 106) as date from acc_bank_pv_rv inner join acc_bank_pv_rv_par on abprp_entryno=abpr_entryno inner join acc_subhead on as_id=abpr_as_id where abpr_approved = 0 and abpr_voucher_name='BANK PAYMENT' group by abpr_entryno,as_name,abpr_total_amount,abpr_date,abprp_entryno order by abprp_entryno desc ";
            }
            else
            {
                sql = "select cast(abpr_entryno as Int) as invoiceNo,as_name as customerName,abpr_total_amount as totalAmount,convert(varchar(11), abpr_date, 106) as date from acc_bank_pv_rv inner join acc_bank_pv_rv_par on abprp_entryno=abpr_entryno inner join acc_subhead on as_id=abpr_as_id where abpr_approved = 0 and abpr_voucher_name='BANK PAYMENT' and abprp_entryno=" + model.Value + "  group by abpr_entryno,as_name,abpr_total_amount,abpr_date,abprp_entryno";
            }

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }

        [HttpPost("search-bank-receipt")]
        public async Task<IActionResult> searchPankreceipt([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = "";
            if (model.Value == "")
            {
                sql = "select top 10 cast(abpr_entryno as Int) as invoiceNo,as_name as customerName,abpr_total_amount as totalAmount,convert(varchar(11), abpr_date, 106) as date,abpr_voucher_name from acc_bank_pv_rv inner join acc_bank_pv_rv_par on abprp_entryno=abpr_entryno inner join acc_subhead on as_id=abpr_as_id where abpr_approved = 0 and abpr_voucher_name='BANK RECEIPT'  group by abpr_entryno,as_name,abpr_total_amount,abpr_date,abprp_entryno,abpr_voucher_name order by abprp_entryno desc";
            }
            else
            {
                sql = "select cast(abpr_entryno as Int) as invoiceNo,as_name as customerName,abpr_total_amount as totalAmount,convert(varchar(11), abpr_date, 106) as date,abpr_voucher_name from acc_bank_pv_rv inner join acc_bank_pv_rv_par on abprp_entryno=abpr_entryno inner join acc_subhead on as_id=abpr_as_id where abpr_approved = 0 and abpr_voucher_name='BANK RECEIPT' and abprp_entryno=" + model.Value + " group by abpr_entryno,as_name,abpr_total_amount,abpr_date,abprp_entryno,abpr_voucher_name";
            }

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }

        [HttpGet("get-bank-receipt-invoice")]
        public string getBankRecieptInvoice()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "select MAX(abpr_entryno) as max_entry_no,min(abpr_entryno) as min_entry_no from acc_bank_pv_rv where abpr_approved = 0 and abpr_voucher_name='BANK RECEIPT'";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);

        }

        [HttpGet("get-bank-payment-invoice")]
        public string getBankPaymentInvoice()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "select MAX(abpr_entryno) as max_entry_no,min(abpr_entryno) as min_entry_no from acc_bank_pv_rv where abpr_approved = 0 and  abpr_voucher_name='BANK PAYMENT'";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);

        }

        [HttpPost("save-bank-pv-rv")]
        public async Task<IActionResult> saveBankPaymentReceipt([FromBody] BankPaymentReceipt model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            int entryNo = usqlre.stmtSaveBankPaymentReceipt(model.statement, model);
            usqlre.close();
            return Ok(new { status =entryNo>0?true:false, entryNo = entryNo });
        }

        [HttpPost("search-bank")]
        public async Task<IActionResult> searchBank([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";

            if (model.Value == "")
            {
                sql = "SELECT top 10 as_name as label,cast(as_id as Int) as value from acc_subhead where as_ap_id='2'";
            }
            else
            {
                sql = "SELECT as_name as label,cast(as_id as Int) as value from acc_subhead where as_ap_id='2' and as_name like '%" + model.Value + "%'";
            }

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }
        [HttpGet("company-locations")]
        public string getCompanyLocations()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "select cast(gl_id as nvarchar(50)) as gl_id,gl_name from gnl_location";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);

        }
        [HttpPost("update-settings/{ans_id}/{ans_status}")]
        public async Task<IActionResult> UpdateSettings(int ans_id, string ans_status)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
              
                string sql = "UPDATE android_settings SET ans_status ='"+ ans_status + "' WHERE ans_id = "+ ans_id + "";
                if (usqlre.dbExecute(sql))
                {
                    return Ok(new { status = true });
                }
                else
                {
                    return Ok(new { status = false });
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                return StatusCode(500, new { error = "An error occurred while updating settings." });
            }
        }

        //[HttpGet("get-android-settings")]
        //public IActionResult GetAndroidSettings(string search = null)
        //{
        //    try
        //    {
        //        UserSqlServer usqlre = new UserSqlServer(this);
        //        usqlre.ensure_android_settings_columns();

        //        string sql = "SELECT ans_id, ans_name, ans_status, ans_remark, ans_used_by FROM android_settings";
        //        if (!string.IsNullOrWhiteSpace(search))
        //        {
        //            string name = search.Trim().Replace("'", "''");
        //            sql += " WHERE ans_name LIKE '%" + name + "%'";
        //        }
        //        sql += " ORDER BY ans_name";

        //        DataTable dt = usqlre.dbReaderFill(sql);
        //        usqlre.close();
        //        return Ok(ReportModelContext.searializeDt(dt));
        //    }
        //    catch (Exception)
        //    {
        //        return StatusCode(500, new { status = false, error = "An error occurred while fetching android settings." });
        //    }
        //}
        [HttpGet("get-android-settings")]
        public IActionResult GetAndroidSettings(string search = null, int? service = null)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                usqlre.ensure_android_settings_columns();

                string sql = @"
            SELECT 
                ans_id,
                ans_name,
                ans_status,
                ans_remark,
                ans_used_by,
                ans_service
            FROM android_settings
            WHERE 1 = 1";

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string name = search.Trim().Replace("'", "''");
                    sql += " AND ans_name LIKE '%" + name + "%'";
                }

                // Service filter
                if (service.HasValue)
                {
                    sql += " AND ans_service = " + service.Value;
                }

                sql += " ORDER BY ans_name";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    status = false,
                    error = "An error occurred while fetching android settings."
                });
            }
        }

        [HttpPost("update-android-settings")]
        public IActionResult UpdateAndroidSettings([FromBody] AndroidSettingsModel model)
        {
            try
            {
                if (model == null || model.ans_id <= 0)
                    return Ok(new { status = false, message = "ans_id is required" });

                string usedBy = (model.ans_used_by ?? "").Trim().ToUpper();
                if (!string.IsNullOrEmpty(usedBy) && usedBy != "BACKEND" && usedBy != "FRONTEND" && usedBy != "BOTH")
                    return Ok(new { status = false, message = "ans_used_by must be BACKEND, FRONTEND or BOTH" });

                UserSqlServer usqlre = new UserSqlServer(this);
                usqlre.ensure_android_settings_columns();

                string remark = (model.ans_remark ?? "").Replace("'", "''");
                string status = (model.ans_status ?? "").Replace("'", "''");

                string sql = "UPDATE android_settings SET ans_remark = '" + remark + "'";
                if (!string.IsNullOrEmpty(status))
                    sql += ", ans_status = '" + status + "'";
                if (!string.IsNullOrEmpty(usedBy))
                    sql += ", ans_used_by = '" + usedBy + "'";
                sql += " WHERE ans_id = " + model.ans_id;

                bool ok = usqlre.dbExecute(sql);
                usqlre.close();
                return Ok(new { status = ok });
            }
            catch (Exception)
            {
                return StatusCode(500, new { status = false, error = "An error occurred while updating android settings." });
            }
        }

        [HttpGet("GetAllCustomerNames")]
        public async Task<IActionResult> GetCustomeNames()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string user_id = usqlre.userId;
            //String sql = "select max(r_name) as r_name,max(r_starting_place) as r_starting_place,max(r_ending_place) as r_ending_place,max(r_id) as r_id,max(r_approx_distance) as r_approx_distance,max(as_id) as as_id,max(as_name) as as_name from inv_rout_reg inner join acc_subhead on r_id = as_rout_id inner join inv_route_allocation on r_id = ira_route_id  where ira_user_id = " + user_id + " and as_rout_id > 0 group by r_id";
            //sql = "select TOP 10 as_id,as_name from acc_subhead";
            string sql = "";
            sql = "select as_name from acc_subhead";
            DataTable routesdt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(routesdt));
        }

        [HttpGet("GetAllSundryDebitors")]
        public async Task<IActionResult> GetLedgerNames()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string user_id = usqlre.userId;
            //String sql = "select max(r_name) as r_name,max(r_starting_place) as r_starting_place,max(r_ending_place) as r_ending_place,max(r_id) as r_id,max(r_approx_distance) as r_approx_distance,max(as_id) as as_id,max(as_name) as as_name from inv_rout_reg inner join acc_subhead on r_id = as_rout_id inner join inv_route_allocation on r_id = ira_route_id  where ira_user_id = " + user_id + " and as_rout_id > 0 group by r_id";
            //sql = "select TOP 10 as_id,as_name from acc_subhead";
            string sql = "";
            sql = "select as_id,as_name,as_mob,as_rate_type,as_active,as_tin,(select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr' ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions where at_as_id=as_id) as ob from acc_subhead where as_ap_id = 4";
            DataTable routesdt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(routesdt));
        }
        [HttpGet("GetSundryDebitorById")]
        public async Task<IActionResult> GetSundryDebitorById(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string user_id = usqlre.userId;
            string sql = "";
            sql = @"select as_id as Id,as_name as Name, as_add1 as Address1 ,as_add2 as Address2,as_add3 as Address3, as_mob as Mobile,as_mail AS Email, as_tin as Tin,as_rout_id as RoutId, isnull(r_name,'') as RoutName, as_area_id as AreaId, isnull(area_name,'') as AreaName from acc_subhead
            left join acc_area on area_id= as_area_id
            left join inv_rout_reg on r_id =as_rout_id
            where as_id= " + id + " and as_ap_id = 4";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();

            if (dt.Rows.Count == 0)
                return NotFound();
            DataRow row = dt.Rows[0];

            var obj = new
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString(),
                Address1 = row["Address1"].ToString(),
                Address2 = row["Address2"].ToString(),
                Address3 = row["Address3"].ToString(),
                Mobile = row["Mobile"].ToString(),
                Tin = row["Tin"].ToString(),
                RoutId = row["RoutId"] != DBNull.Value ? Convert.ToInt32(row["RoutId"]) : (int?)null,
                RoutName = row["RoutName"].ToString(),
                AreaId = row["AreaId"] != DBNull.Value ? Convert.ToInt32(row["AreaId"]) : (int?)null,
                AreaName = row["AreaName"].ToString()
            };

            return Ok(obj);
        }
        [HttpGet("GetAllSundryDebitorsByName")]
        public async Task<IActionResult> GetLedgerNamesByName(string? name)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string user_id = usqlre.userId;

            string sql = @" SELECT TOP 100 as_id,as_name,as_add1,as_add2,as_mob,as_mail,as_rout_id as routeId, isnull(r_name,'') as routeName FROM acc_subhead left join inv_rout_reg on r_id= as_rout_id WHERE as_ap_id = 4 and as_active=1";
            if (!string.IsNullOrWhiteSpace(name))
            {
                sql += " AND as_name LIKE '%" + name.Replace("'", "''") + "%'";
            }
            sql += " ORDER BY as_name";
            DataTable routesdt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(routesdt));
        }

        [HttpGet("GetAllSundryCreditors")]
        public async Task<IActionResult> GetSundryCreditors()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string user_id = usqlre.userId;
            string sql = "";
            sql = "select as_id,as_name ,(select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr' ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions where at_as_id=as_id) as ob from acc_subhead where as_ap_id = 6";
            DataTable routesdt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(routesdt));
        }
        [HttpPost("save-credit-and-debit")]
        public async Task<IActionResult> newCustomer([FromBody] CreditAndDebitNoteModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            int entryNo = usqlre.CrAndDrSave("Insert", model);
            return Ok(new { status = entryNo > 0, msg = entryNo > 0 ? "Saved Successfully" : "Error in saving data" });
        }
        [HttpGet("load-entryno-dr")]
        public string DrEntryNo()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT TOP 1 (bei_entryno + 1) AS next_entryno FROM acc_billwise_inf where bei_voucher_name = 'DEBIT NOTE' ORDER BY bei_entryno DESC;";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();

            if (dt != null && dt.Rows.Count > 0)
            {
                return dt.Rows[0]["next_entryno"].ToString();
            }
            else
            {
                return "1";
            }
        }
        [HttpGet("load-entryno-cr")]
        public string CrEntryNo()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT TOP 1 (bei_entryno + 1) AS next_entryno FROM acc_billwise_inf where bei_voucher_name = 'CREDIT NOTE' ORDER BY bei_entryno DESC;";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();

            if (dt != null && dt.Rows.Count > 0)
            {
                return dt.Rows[0]["next_entryno"].ToString();
            }
            else
            {
                return "1";
            }
        }
        [HttpGet("get-bank-details")]
        public async Task<IActionResult> GetBank()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string user_id = usqlre.userId;
            string sql = "";
            if (usqlre.user_role == "ADMIN")
            {
                sql = "SELECT as_name as label,as_id as value from acc_subhead where as_ap_id='2'";
            }
            else
            {
                string checkBankId= "select gu_user_bank_id from   gnl_users where gu_user_id=" + usqlre.userId + "";
                DataTable BankId = usqlre.dbReaderFill(checkBankId);
                if (BankId.Rows.Count > 0)
                {
                    int guUserBankId = Convert.ToInt32(BankId.Rows[0]["gu_user_bank_id"]);
                    if (guUserBankId == -1)
                    {
                        sql = "SELECT as_name as label,as_id as value from acc_subhead where as_ap_id='2'";

                    }
                    else
                    {
                        sql = "select as_name as label,as_id as value from acc_subhead left join gnl_users on  gu_user_bank_id= as_id where gu_user_id=" + usqlre.userId + "";
                    }

                }
                else
                {
                    return BadRequest("Bank ID not found for the user.");
                }

            }
            DataTable bank = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(bank));
        }



        //[HttpPost("save-bulk-tray-lend-transaction")]
        //public async Task<IActionResult> SaveBulkTrayLendTransaction([FromBody] List<BulkTrayLendModel> models)
        //{
        //    if (models == null || models.Count == 0)
        //        return BadRequest(new { status = false, message = "Invalid request body" });

        //    UserSqlServer usqlre = new UserSqlServer(this);

        //    foreach (var model in models)
        //    {
        //        string query = @"INSERT INTO inv_lend_item_transactions (li_date, li_as_id, li_form, li_entryno, li_in, li_out, li_remarks, li_ir_id, li_ir_mrp, li_ift_id) 
        //                       VALUES (GETDATE(), "+model.customerId + @", 'VOUCHER-R', 
        //                       (SELECT ISNULL(MAX(li_entryno), 0) + 1 FROM inv_lend_item_transactions WHERE li_form = 'VOUCHER-R'), 
        //                       "+model.boxQty+ ", 0, '', (SELECT CAST(ans_status AS INT) FROM android_settings WHERE ans_name = 'DEFAULT LEND PRODUCT'), 0, 0)";

        //        if (!usqlre.dbExecute(query))
        //            return BadRequest(new { status = false, message = "Insert failed" });
        //    }

        //    usqlre.close();
        //    return Ok(new { status = true, message = "Bulk transaction saved successfully" });
        //}

        [HttpPost("save-bulk-tray-lend-transaction")]
        public async Task<IActionResult> SaveBulkTrayLendTransaction([FromBody] List<BulkTrayLendModel> models)
        {
            if (models == null || models.Count == 0)
                return BadRequest(new { status = false, message = "Invalid request body" });

            UserSqlServer usqlre = new UserSqlServer(this);
            DataTable entryNoQuery = usqlre.dbReaderFill("SELECT ISNULL(MAX(li_entryno), 0) + 1 FROM inv_lend_item_transactions WHERE li_form = 'VOUCHER-R'");
            int nextEntryNo = Convert.ToInt32(entryNoQuery.Rows[0][0]);

            foreach (var model in models)
            {
                string query = @"INSERT INTO inv_lend_item_transactions (li_date, li_as_id, li_form, li_entryno, li_in, li_out, li_remarks, li_ir_id, li_ir_mrp, li_ift_id) 
                               VALUES (GETDATE(), " + model.customerId + @", 'VOUCHER-R',"+ nextEntryNo + @", 
                               " + model.boxQty + ", 0, '', (SELECT CAST(ans_status AS INT) FROM android_settings WHERE ans_name = 'DEFAULT LEND PRODUCT'), 0, 0)";

                if (!usqlre.dbExecute(query))
                    return BadRequest(new { status = false, message = "Insert failed" });
            }

            usqlre.close();
            DataTable dateQuery = usqlre.dbReaderFill("SELECT TOP 1 li_date FROM inv_lend_item_transactions WHERE li_entryno = " + nextEntryNo + " AND li_form = 'VOUCHER-R'");
            DateTime liDate = Convert.ToDateTime(dateQuery.Rows[0][0]);
            return Ok(new { status = true, message = "Bulk transaction saved successfully", entryno = nextEntryNo, date = liDate });
        }


        [HttpGet("bulk-tray-lend-report")]
        public async Task<IActionResult> BulkTrayLendReport(DateTime date, int custId)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                // First Query (Single Object)
                string sql = @"SELECT si_str_id, as_name AS [AgentName],
                              si_add1 AS [AgentAddress1], 
                              si_add2 AS [AgentAddress2],
                              si_entryno AS [EntryNo],
                              si_date AS [Date],
                              si_grand_total AS [TotalPrice],
                            (SELECT 
                                    CONCAT(ABS(SUM(at_Dr) - SUM(at_Cr)), '0', 
                                           CASE WHEN SUM(at_Dr) - SUM(at_Cr) > 0 THEN 'Dr' ELSE 'Cr' END)
                                 FROM acc_account_transactions
                                 WHERE at_as_id = si_acc_id
                                 AND CAST(at_date AS DATE) < '" + date.ToString("yyyy-MM-dd") + @"') AS OpeningBalance,

                                (SELECT 
                                    CONCAT(ABS(SUM(at_Dr) - SUM(at_Cr)), '0', 
                                           CASE WHEN SUM(at_Dr) - SUM(at_Cr) > 0 THEN 'Dr' ELSE 'Cr' END)
                                 FROM acc_account_transactions
                                 WHERE at_as_id = si_acc_id
                                 AND CAST(at_date AS DATE) <= '" + date.ToString("yyyy-MM-dd") + @"') AS ClosingBalance,
                                (SELECT 
                                        SUM(at_Cr)
                                     FROM acc_account_transactions
                                     WHERE at_as_id = si_acc_id
                                     AND CAST(at_date AS DATE) = '" + date.ToString("yyyy-MM-dd") + @"') AS ReceivedAmount
                                 FROM inv_sales_inf
                       LEFT JOIN acc_subhead ON as_id = si_acc_id
                       WHERE si_str_id IN (1,2,6,7) AND CAST(si_date AS DATE) = '" + date.ToString("yyyy-MM-dd") + "' and si_acc_id ="+custId+"";

                DataTable main = usqlre.dbReaderFill(sql);

                // Second Query (Array)
                string sql1 = @"SELECT sp_str_id, 
                               si_entryno AS [EntryNo],	
                               ir_name AS [ProductName],
                                CASE 
                                        WHEN ir_tray_qty = 0 THEN 0 
                                        ELSE sp_qty / ir_tray_qty 
                                    END AS [Qty/Tray],
                                sp_qty AS [Ltr/Kg],
							   isnull(a.u_name,'') AS minUnit,
							   isnull(b.u_name,'') AS bulkUnit,
                               sp_total AS Amount
                        FROM inv_sales_par
                        LEFT JOIN inv_sales_inf ON si_entryno = sp_entryno AND si_str_id = sp_str_id
                        LEFT JOIN inv_item_reg ON sp_ir_id = ir_id
                        LEFT JOIN inv_unit a ON a.u_id = ir_min_unit_id
						LEFT JOIN inv_unit b ON b.u_id = ir_bulk_unit_id
                        WHERE sp_str_id IN (1,2,6,7) AND CAST(si_date AS DATE) = '" + date.ToString("yyyy-MM-dd") + "' and si_acc_id ="+custId+"";

                DataTable detail = usqlre.dbReaderFill(sql1);               
                string sql2 = @"
                SELECT 
                    ISNULL(SUM(CASE WHEN li_date < '" + date.ToString("yyyy-MM-dd") + @"' THEN li_out - li_in END), 0) AS [OpeningTray],
                    ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '" + date.ToString("yyyy-MM-dd") + @"' THEN li_out END), 0) AS Issue,
                    ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '" + date.ToString("yyyy-MM-dd") + @"' THEN li_in END), 0) AS [Return],  
                    (ISNULL(SUM(CASE WHEN li_date < '" + date.ToString("yyyy-MM-dd") + @"' THEN li_out - li_in END), 0) +
                     ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '" + date.ToString("yyyy-MM-dd") + @"' THEN li_out END), 0) -
                     ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '" + date.ToString("yyyy-MM-dd") + @"' THEN li_in END), 0)) AS [TrayBalance]
                FROM inv_lend_item_transactions
                WHERE li_as_id = " + custId+"";

                DataTable trayDetail = usqlre.dbReaderFill(sql2);
                usqlre.close();
                var responseObject = main.AsEnumerable().Select(row => new
                {
                    main = new
                    {
                        si_str_id = row["si_str_id"],
                        agentName = row["AgentName"],
                        agentAddress1 = row["AgentAddress1"],
                        agentAddress2 = row["AgentAddress2"],
                        entryNo = row["EntryNo"],
                        date = Convert.ToDateTime(row["Date"]).ToString("yyyy-MM-dd"),
                        totalPrice = row["TotalPrice"],
                        openigBalance = row["OpeningBalance"],
                        closingBalance = row["ClosingBalance"],
                        ReceivedAmount = row["ReceivedAmount"]

                    },
                    details = detail.AsEnumerable()
                    .Where(d => d["EntryNo"].ToString() == row["EntryNo"].ToString())
                    .Select(d => new
                    {
                        sp_str_id = d["sp_str_id"],
                        entryNo = d["EntryNo"],
                        productName = d["ProductName"],
                        qtyTray = d["Qty/Tray"],
                        ltrKg = d["Ltr/Kg"],
                        minUnit = d["minUnit"],
                        bulkUnit = d["bulkUnit"],
                        amount = d["Amount"]
                    }).ToList(),
                    trayDetail = trayDetail.AsEnumerable().Select(td => new
                    {
                        openingTray = td["OpeningTray"],
                        issue = td["Issue"],
                        returnQty = td["Return"],
                        trayBalance = td["TrayBalance"]
                    }).FirstOrDefault()
                }).ToList(); // Convert to List

                // Return Correct Response Format
                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Report generated successfully",
                    data = responseObject
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message,
                    data = (object)null
                });
            }
        }
        [HttpGet("get-itemname")]
        public async Task<IActionResult> GetItemName()
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                string user_id = usqlre.userId;
                string sql = "";
                bool productNameOther = false;
                bool productAliasColumn = false;
                bool DISABLE_LOCATION_BASED_STOCK_IN_PRODUCT_LIST = false;
                if (usqlre.get_android_settings("PRODUCT NAME ALTERNATIVE"))
                {
                    productNameOther = true;
                }
                if (usqlre.get_android_settings("DISABLE LOCATION BASED STOCK_IN PRODUCT LIST"))
                {
                    DISABLE_LOCATION_BASED_STOCK_IN_PRODUCT_LIST = true;
                }
                if (!DISABLE_LOCATION_BASED_STOCK_IN_PRODUCT_LIST)
                {
                    if (productNameOther)
                    {
                        if (usqlre.get_android_settings("PRODUCT NAME ALIASCOLUMN"))
                        {
                            productAliasColumn = true;
                        }
                        if (productAliasColumn)
                        {
                            sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_alias_name as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock,i.ir_batch_status AS BatchStatus
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  and  v.location_id='" + usqlre.locationId + @"'
                    GROUP BY i.ir_name,i.ir_alias_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch,i.ir_batch_status;
                    ";
                        }
                        else
                        {
                            sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_hsn_code as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  and  v.location_id='" + usqlre.locationId + @"'
                    GROUP BY i.ir_name,i.ir_hsn_code,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch;
                    ";

                        }
                    }
                    else
                    {
                        sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock,i.ir_batch_status AS BatchStatus
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  and  v.location_id='" + usqlre.locationId + @"'
                    GROUP BY i.ir_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch,i.ir_batch_status;
                    ";

                    }
                }
                else
                {
                    if (productNameOther)
                    {
                        if (usqlre.get_android_settings("PRODUCT NAME ALIASCOLUMN"))
                        {
                            productAliasColumn = true;
                        }
                        if (productAliasColumn)
                        {
                            sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_alias_name as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock,i.ir_batch_status AS BatchStatus
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  
                    GROUP BY i.ir_name,i.ir_alias_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch,i.ir_batch_status;
                    ";
                        }
                        else
                        {
                            sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_hsn_code as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock,i.ir_batch_status AS BatchStatus
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  
                    GROUP BY i.ir_name,i.ir_hsn_code,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch,i.ir_batch_status;
                    ";

                        }
                    }
                    else
                    {
                        sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch,SUM(v.qty) AS Stock,i.ir_batch_status AS BatchStatus
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  
                    GROUP BY i.ir_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch,i.ir_batch_status;
                    ";

                    }
                }
                DataTable routesdt = usqlre.dbReaderFill(sql);
                usqlre.close();
                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Item names retrieved successfully",
                    data = ReportModelContext.searializeDt(routesdt)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message,
                    data = (object)null
                });
            }
            
        }
        [HttpGet("get-itemname-with-barcode")]
        public async Task<IActionResult> GetItemNameWithBarcode()
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                string user_id = usqlre.userId;
                string sql = "";
                bool productNameOther = false;
                bool productAliasColumn = false;
                bool DISABLE_LOCATION_BASED_STOCK_IN_PRODUCT_LIST = false;
                if (usqlre.get_android_settings("PRODUCT NAME ALTERNATIVE"))
                {
                    productNameOther = true;
                }
                if (usqlre.get_android_settings("DISABLE LOCATION BASED STOCK_IN PRODUCT LIST"))
                {
                    DISABLE_LOCATION_BASED_STOCK_IN_PRODUCT_LIST = true;
                }
                if (!DISABLE_LOCATION_BASED_STOCK_IN_PRODUCT_LIST)
                {
                    if (productNameOther)
                    {
                        if (usqlre.get_android_settings("PRODUCT NAME ALIASCOLUMN"))
                        {
                            productAliasColumn = true;
                        }
                        if (productAliasColumn)
                        {
                            sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,color,size,Narration,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_alias_name as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch, ls.color AS Color,ls.size AS Size,ls.Narration AS Narration,SUM(v.qty) AS Stock,i.ir_batch_status AS BatchStatus,v.uniquecode
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  and  v.location_id='" + usqlre.locationId + @"'
                    GROUP BY i.ir_name,i.ir_alias_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch,ls.color ,ls.size ,ls.Narration ,i.ir_batch_status,v.uniquecode;
                    ";
                        }
                        else
                        {
                            sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,color,size,Narration,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_hsn_code as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch, ls.color AS Color,ls.size AS Size,ls.Narration AS Narration,SUM(v.qty) AS Stock,v.uniquecode
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  and  v.location_id='" + usqlre.locationId + @"'
                    GROUP BY i.ir_name,i.ir_hsn_code,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch,ls.color ,ls.size ,ls.Narration ,v.uniquecode;
                    ";

                        }
                    }
                    else
                    {
                        sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,color,size,Narration,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch, ls.color AS Color,ls.size AS Size,ls.Narration AS Narration,SUM(v.qty) AS Stock,i.ir_batch_status AS BatchStatus,v.uniquecode
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  and  v.location_id='" + usqlre.locationId + @"'
                    GROUP BY i.ir_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch,ls.color ,ls.size ,ls.Narration ,i.ir_batch_status,v.uniquecode;
                    ";

                    }
                }
                else
                {
                    if (productNameOther)
                    {
                        if (usqlre.get_android_settings("PRODUCT NAME ALIASCOLUMN"))
                        {
                            productAliasColumn = true;
                        }
                        if (productAliasColumn)
                        {
                            sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,color,size,Narration,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_alias_name as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch, ls.color AS Color,ls.size AS Size,ls.Narration AS Narration,SUM(v.qty) AS Stock,i.ir_batch_status AS BatchStatus,v.uniquecode
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  
                    GROUP BY i.ir_name,i.ir_alias_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch,ls.color ,ls.size ,ls.Narration ,i.ir_batch_status,v.uniquecode;
                    ";
                        }
                        else
                        {
                            sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,color,size,Narration,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_hsn_code as aliasname,
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch, ls.color AS Color,ls.size AS Size,ls.Narration AS Narration,SUM(v.qty) AS Stock,i.ir_batch_status AS BatchStatus,v.uniquecode
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  
                    GROUP BY i.ir_name,i.ir_hsn_code,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch,ls.color ,ls.size ,ls.Narration ,i.ir_batch_status,v.uniquecode;
                    ";

                        }
                    }
                    else
                    {
                        sql = @"WITH LatestStock AS (
                        SELECT 
                            ir_id, 
                            Cost, mrp, retail, wholesale, spretail, branch,color,size,Narration,
                            ROW_NUMBER() OVER (PARTITION BY ir_id ORDER BY uniquecode DESC) AS rn
                        FROM View_Stock
                    )
                    SELECT 
                        i.ir_name AS label,
                        v.ir_id as value, 
	                    i.ir_rawmaterial,
	                    i.ir_lend_validation as isLend ,
                        ls.Cost as PurchaseRate,
                         ls.mrp as Mrp, ls.retail as Retail, ls.wholesale as Wholesale, ls.spretail as SpRetail, ls.branch as Branch, ls.color AS Color,ls.size AS Size,ls.Narration AS Narration,SUM(v.qty) AS Stock,i.ir_batch_status AS BatchStatus,v.uniquecode
                    FROM View_Stock v
                    INNER JOIN LatestStock ls ON v.ir_id = ls.ir_id AND ls.rn = 1
                    INNER JOIN inv_item_reg i ON v.ir_id = i.ir_id  
                    WHERE i.ir_active = 1  
                    GROUP BY i.ir_name,i.ir_rawmaterial,i.ir_lend_validation, v.ir_id, ls.Cost, ls.mrp, ls.retail, ls.wholesale, ls.spretail, ls.branch,ls.color ,ls.size ,ls.Narration ,i.ir_batch_status,v.uniquecode;
                    ";

                    }
                }
                DataTable routesdt = usqlre.dbReaderFill(sql);
                usqlre.close();
                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Item names retrieved successfully",
                    data = ReportModelContext.searializeDt(routesdt)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message,
                    data = (object)null
                });
            }

        }

        [HttpGet("bulk-tray-lend-report-by-customer")]

        public async Task<IActionResult> BulkTrayLendReportByCustomer(DateTime fromDate, DateTime toDate, int custId)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                List<object> trayReportList = new List<object>();

                for (DateTime date = fromDate; date <= toDate; date = date.AddDays(1))
                {
                    string sql = $@"
                SELECT 
                    ISNULL(SUM(CASE WHEN li_date < '{date:yyyy-MM-dd}' THEN li_out - li_in END), 0) AS OpeningTray,
                    ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '{date:yyyy-MM-dd}' THEN li_out END), 0) AS Issue,
                    ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '{date:yyyy-MM-dd}' THEN li_in END), 0) AS [Return]
                FROM inv_lend_item_transactions
                WHERE li_as_id = {custId}";

                    DataTable dt = usqlre.dbReaderFill(sql);
                    string balanceSql = $@"
                SELECT
                    (SELECT 
                        CONCAT(ABS(SUM(at_Dr) - SUM(at_Cr)), '0', 
                               CASE WHEN SUM(at_Dr) - SUM(at_Cr) > 0 THEN 'Dr' ELSE 'Cr' END)
                     FROM acc_account_transactions
                     WHERE at_as_id = {custId} AND CAST(at_date AS DATE) < '{date:yyyy-MM-dd}') AS OpeningBalance,

                    (SELECT 
                        CONCAT(ABS(SUM(at_Dr) - SUM(at_Cr)), '0', 
                               CASE WHEN SUM(at_Dr) - SUM(at_Cr) > 0 THEN 'Dr' ELSE 'Cr' END)
                     FROM acc_account_transactions
                     WHERE at_as_id = {custId} AND CAST(at_date AS DATE) <= '{date:yyyy-MM-dd}') AS ClosingBalance,

                    (SELECT ISNULL(SUM(at_Cr), 0)
                     FROM acc_account_transactions
                     WHERE at_as_id = {custId} AND CAST(at_date AS DATE) = '{date:yyyy-MM-dd}') AS ReceivedAmount
            ";


                    DataTable balanceDt = usqlre.dbReaderFill(balanceSql);
                    if (dt.Rows.Count > 0)
                    {
                        decimal opening = Convert.ToDecimal(dt.Rows[0]["OpeningTray"]);
                        decimal issue = Convert.ToDecimal(dt.Rows[0]["Issue"]);
                        decimal ret = Convert.ToDecimal(dt.Rows[0]["Return"]);
                        decimal balance = opening + issue - ret;

                        string openingBalance = balanceDt.Rows[0]["OpeningBalance"]?.ToString();
                        string closingBalance = balanceDt.Rows[0]["ClosingBalance"]?.ToString();
                        decimal receivedAmount = Convert.ToDecimal(balanceDt.Rows[0]["ReceivedAmount"]);

                        trayReportList.Add(new
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                            OpeningTray = opening,
                            Issue = issue,
                            Return = ret,
                            TrayBalance = balance,
                            OpeningBalance = openingBalance,
                            ClosingBalance = closingBalance,
                            ReceivedAmount = receivedAmount

                        });
                    }
                }

                usqlre.close();

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Tray Report Loaded",
                    data = trayReportList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message,
                    data = (object)null
                });
            }
        }
        //[HttpGet("bulk-tray-lend-report-all-customers")]
        //public async Task<IActionResult> BulkTrayLendReportAllCustomers(DateTime fromDate, DateTime toDate)
        //{
        //    try
        //    {
        //        UserSqlServer usqlre = new UserSqlServer(this);
        //        List<object> trayReportList = new List<object>();

        //        string sqlGetCustomers = "SELECT DISTINCT as_name FROM inv_lend_item_transactions left join acc_subhead on as_id=li_as_id where as_ap_id=4";
        //        DataTable customerTable = usqlre.dbReaderFill(sqlGetCustomers);

        //        for (DateTime date = fromDate; date <= toDate; date = date.AddDays(1))
        //        {
        //            foreach (DataRow row in customerTable.Rows)
        //            {
        //                string custName = row["as_name"].ToString();

        //                string sql = $@"
        //        SELECT 
        //            ISNULL(SUM(CASE WHEN li_date < '{date:yyyy-MM-dd}' THEN li_out - li_in END), 0) AS OpeningTray,
        //            ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '{date:yyyy-MM-dd}' THEN li_out END), 0) AS Issue,
        //            ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '{date:yyyy-MM-dd}' THEN li_in END), 0) AS [Return]
        //        FROM inv_lend_item_transactions
        //        left join acc_subhead on as_id=li_as_id 
        //        WHERE  as_ap_id=4 and as_name = '{custName}'";

        //                DataTable dt = usqlre.dbReaderFill(sql);

        //                if (dt.Rows.Count > 0)
        //                {
        //                    decimal opening = Convert.ToDecimal(dt.Rows[0]["OpeningTray"]);
        //                    decimal issue = Convert.ToDecimal(dt.Rows[0]["Issue"]);
        //                    decimal ret = Convert.ToDecimal(dt.Rows[0]["Return"]);
        //                    decimal balance = opening + issue - ret;

        //                    trayReportList.Add(new
        //                    {
        //                        CustomerName = custName,
        //                        Date = date.ToString("yyyy-MM-dd"),
        //                        OpeningTray = opening,
        //                        Issue = issue,
        //                        Return = ret,
        //                        TrayBalance = balance
        //                    });
        //                }
        //            }
        //        }

        //        usqlre.close();

        //        return Ok(new
        //        {
        //            status = true,
        //            statusCode = 200,
        //            message = "Tray Report Loaded",
        //            data = trayReportList
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = ex.Message,
        //            data = (object)null
        //        });
        //    }
        //}
        //[HttpGet("get-bulk-tray-lend-transaction")]
        [HttpGet("bulk-tray-lend-report-all-customers")]
        public async Task<IActionResult> BulkTrayLendReportAllCustomers(DateTime fromDate, DateTime toDate)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                List<object> trayReportList = new List<object>();

                string sqlGetCustomers = "SELECT DISTINCT as_id, as_name FROM inv_lend_item_transactions LEFT JOIN acc_subhead ON as_id = li_as_id WHERE as_ap_id = 4";
                DataTable customerTable = usqlre.dbReaderFill(sqlGetCustomers);

                for (DateTime date = fromDate; date <= toDate; date = date.AddDays(1))
                {
                    foreach (DataRow row in customerTable.Rows)
                    {
                        int custId = Convert.ToInt32(row["as_id"]);
                        string custName = row["as_name"].ToString();

                        // Tray calculation
                        string sql = $@"
                    SELECT 
                        ISNULL(SUM(CASE WHEN li_date < '{date:yyyy-MM-dd}' THEN li_out - li_in END), 0) AS OpeningTray,
                        ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '{date:yyyy-MM-dd}' THEN li_out END), 0) AS Issue,
                        ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '{date:yyyy-MM-dd}' THEN li_in END), 0) AS [Return]
                    FROM inv_lend_item_transactions
                    WHERE li_as_id = {custId}";

                        DataTable dt = usqlre.dbReaderFill(sql);

                        // Account balance calculations
                        string balanceSql = $@"
                    SELECT 
                        (SELECT 
                            CONCAT(ABS(SUM(at_Dr) - SUM(at_Cr)), '0', 
                                   CASE WHEN SUM(at_Dr) - SUM(at_Cr) > 0 THEN 'Dr' ELSE 'Cr' END)  
                         FROM acc_account_transactions 
                         WHERE at_as_id = {custId} AND CAST(at_date AS DATE) < '{date:yyyy-MM-dd}') AS OpeningBalance,

                        (SELECT 
                            CONCAT(ABS(SUM(at_Dr) - SUM(at_Cr)), '0', 
                                   CASE WHEN SUM(at_Dr) - SUM(at_Cr) > 0 THEN 'Dr' ELSE 'Cr' END)
                         FROM acc_account_transactions 
                         WHERE at_as_id = {custId} AND CAST(at_date AS DATE) <= '{date:yyyy-MM-dd}') AS ClosingBalance,

                        (SELECT ISNULL(SUM(at_Cr), 0) 
                         FROM acc_account_transactions 
                         WHERE at_as_id = {custId} AND CAST(at_date AS DATE) = '{date:yyyy-MM-dd}') AS ReceivedAmount
                ";


                        DataTable balanceDt = usqlre.dbReaderFill(balanceSql);

                        if (dt.Rows.Count > 0)
                        {
                            decimal opening = Convert.ToDecimal(dt.Rows[0]["OpeningTray"]);
                            decimal issue = Convert.ToDecimal(dt.Rows[0]["Issue"]);
                            decimal ret = Convert.ToDecimal(dt.Rows[0]["Return"]);
                            decimal balance = opening + issue - ret;

                            string OpeningBalance = balanceDt.Rows[0]["OpeningBalance"]?.ToString();
                            string ClosingBalance = balanceDt.Rows[0]["ClosingBalance"]?.ToString();
                            decimal ReceivedAmount = Convert.ToDecimal(balanceDt.Rows[0]["ReceivedAmount"]);

                            trayReportList.Add(new
                            {
                                CustomerName = custName,
                                Date = date.ToString("yyyy-MM-dd"),
                                OpeningTray = opening,
                                Issue = issue,
                                Return = ret,
                                TrayBalance = balance,
                                OpeningBalance,
                                ClosingBalance,
                                ReceivedAmount
                            });
                        }
                    }
                }

                usqlre.close();

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Tray Report Loaded",
                    data = trayReportList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message,
                    data = (object)null
                });
            }
        }
        [HttpGet("get-bulk-tray-lend-transaction")]
        public async Task<IActionResult> GetBulkTrayLendTransaction()
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                
                string sql = @"
        SELECT TOP 20  
            li_id AS id, 
            li_date AS date,
            li_entryno AS entryNo,
            li_as_id AS customerId, 
            as_name AS customerName,  
            li_in AS boxQty 
        FROM inv_lend_item_transactions
        LEFT JOIN acc_subhead ON li_as_id = as_id
        WHERE li_form = 'VOUCHER-R'
        ORDER BY li_id DESC";
                DataTable dt = usqlre.dbReaderFill(sql);

                // Group by entryNo, but prepare only the inner list
                var groupedData = dt.AsEnumerable()
                    .GroupBy(row => row["entryNo"])
                    .Select(group => group.Select(r => new
                    {
                        entryNo = Convert.ToInt32(r["entryNo"]),
                        date = Convert.ToDateTime(r["date"]),
                        customerId = Convert.ToInt32(r["customerId"]),
                        customerName = r["customerName"].ToString(),
                        boxQty = Convert.ToSingle(r["boxQty"])  // Convert to float
                    }).ToList())
                    .ToList();

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Tray Report Loaded",
                    data = groupedData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message,
                    data = (object)null
                });
            }
        }
        //[HttpPost("update-bulk-tray-lend-transaction")]
        //public async Task<IActionResult> UpdateBulkTrayLendTransaction([FromBody] List<BulkTrayLendModel> models)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);
        //    if (models == null || models.Count == 0)
        //        return BadRequest(new { status = false, message = "Invalid request body" });

        //    foreach (var model in models)
        //    {
        //        string sql = $@"
        //        update inv_lend_item_transactions 
        //        set li_in = {model.boxQty}, li_date= GETDATE() 
        //        where li_form = 'VOUCHER-R' and li_as_id = {model.customerId} and li_entryno = {model.entryNo}";

        //        if (!usqlre.dbExecute(sql))
        //            return BadRequest(new { status = false, message = "Updation failed" });
        //    }

        //    usqlre.close();
        //    DataTable dateQuery = usqlre.dbReaderFill(@"SELECT TOP 1 li_date FROM inv_lend_item_transactions WHERE li_entryno = " + models.entryNo + " AND li_form = 'VOUCHER-R'");
        //    DateTime liDate = Convert.ToDateTime(dateQuery.Rows[0][0]);
        //    return Ok(new { status = true, message = "Bulk transaction edit successfully", entryno = model.entryno, date = liDate });
        //}
        [HttpPost("update-bulk-tray-lend-transaction")]
        public async Task<IActionResult> UpdateBulkTrayLendTransaction([FromBody] List<BulkTrayLendModel> models)
        {
            if (models == null || models.Count == 0)
                return BadRequest(new { status = false, message = "Invalid request body" });

            UserSqlServer usqlre = new UserSqlServer(this);
            int lastEntryNo = 0;

            foreach (var model in models)
            {
                string sql = $@"
        UPDATE inv_lend_item_transactions 
        SET li_in = {model.boxQty}, li_date = GETDATE() 
        WHERE li_form = 'VOUCHER-R' AND li_as_id = {model.customerId} AND li_entryno = {model.entryNo}";

                if (!usqlre.dbExecute(sql))
                {
                    usqlre.close();
                    return BadRequest(new { status = false, message = "Updation failed" });
                }

                lastEntryNo = model.entryNo; // Store last processed entryNo
            }

            DataTable dateQuery = usqlre.dbReaderFill($@"
            SELECT TOP 1 li_date 
            FROM inv_lend_item_transactions 
            WHERE li_entryno = {lastEntryNo} AND li_form = 'VOUCHER-R'");

            usqlre.close();

            if (dateQuery.Rows.Count == 0)
                return NotFound(new { status = false, message = "Date not found" });

            DateTime liDate = Convert.ToDateTime(dateQuery.Rows[0][0]);

            return Ok(new
            {
                status = true,
                message = "Bulk transaction edit successfully",
                entryno = lastEntryNo,
                date = liDate
            });
        }
        //[HttpPost("get-current-holding-qty")]
        //public IActionResult GetCurrentHoldingQty([FromBody] CurrentHoldingRequest request)
        //{
        //    try
        //    {
        //        // Validation check for null or invalid request
        //        if (request == null || request.ItemIds == null || request.ItemIds.Count == 0)
        //        {
        //            return BadRequest(new
        //            {
        //                status = false,
        //                StatusCode = 400,
        //                message = "Invalid request body",
        //                data = new List<object>()
        //            });
        //        }

        //        UserSqlServer usqlre = new UserSqlServer(this);
        //        string itemIdList = string.Join(",", request.ItemIds);

        //        string sql = $@"
        //    SELECT ch_ir_id, ir_name, ch_qty 
        //    FROM inv_current_holding 
        //    INNER JOIN inv_item_reg ON ir_id = ch_ir_id
        //    WHERE ch_as_id = {request.CustId} 
        //    AND ch_ir_id IN ({itemIdList})";

        //        DataTable dt = usqlre.dbReaderFill(sql);
        //        usqlre.close();

        //        if (dt.Rows.Count == 0)
        //        {
        //            return Ok(new
        //            {
        //                status = true,
        //                StatusCode = 200,
        //                message = "No records found",
        //                data = new List<object>()
        //            });
        //        }

        //        var results = dt.AsEnumerable()
        //                        .Select(row => new
        //                        {
        //                            itemId = Convert.ToInt32(row["ch_ir_id"]),
        //                            itemName = row["ir_name"].ToString(),
        //                            qty = Convert.ToDecimal(row["ch_qty"])
        //                        })
        //                        .ToList();

        //        return Ok(new
        //        {
        //            status = true,
        //            StatusCode = 200,
        //            message = "Successful",
        //            data = results
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            StatusCode = 500,
        //            message = "An error occurred while processing the request. " + ex.Message,
        //            data = new List<object>(),
        //        });
        //    }
        //}
        [HttpPost("get-current-holding-items")]
        public IActionResult GetCurrentHoldingItems()
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql = "SELECT ir_id as ItemId, ir_name as ItemName FROM inv_item_reg WHERE ir_hold = 1 or ir_lend_validation=1";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count == 0)
                {
                    return Ok(new
                    {
                        status = true,
                        StatusCode = 200,
                        message = "No records found",
                        data = new List<object>()
                    });
                }

                // Convert DataTable to a list of dictionaries
                var data = dt.AsEnumerable()
                             .Select(row => dt.Columns.Cast<DataColumn>()
                                 .ToDictionary(col => col.ColumnName, col => row[col]))
                             .ToList();

                return Ok(new
                {
                    status = true,
                    StatusCode = 200,
                    message = "Items fetched successfully",
                    data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    StatusCode = 500,
                    message = "An error occurred while processing the request. " + ex.Message,
                    data = new List<object>()
                });
            }
        }
        [HttpPost("get-current-holding-qty")]
        public IActionResult GetCurrentHoldingQty(int CustId)
        {
            try
            {
                // Validation check for null or invalid request
                if (CustId == null || CustId == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        StatusCode = 400,
                        message = "Invalid request body",
                        data = new List<object>()
                    });
                }

                UserSqlServer usqlre = new UserSqlServer(this);

                string sql = $@"
            SELECT ch_ir_id as ItemId ,ch_qty as ItemQty
            FROM inv_current_holding ch
            JOIN (
                SELECT ch_as_id, MAX(ch_entryno) AS max_entryno
                FROM inv_current_holding
                GROUP BY ch_as_id
            ) latest
            ON ch.ch_as_id = latest.ch_as_id AND ch_entryno = latest.max_entryno

            WHERE ch.ch_as_id = {CustId} 
            ";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count == 0)
                {
                    return Ok(new
                    {
                        status = true,
                        StatusCode = 200,
                        message = "No records found",
                        data = new List<object>()
                    });
                }

                var results = dt.AsEnumerable()
                                .Select(row => new
                                {
                                    ItemId = Convert.ToInt32(row["ItemId"]),
                                    HoldQty = Convert.ToDecimal(row["ItemQty"])
                                })
                                .ToList();

                return Ok(new
                {
                    status = true,
                    StatusCode = 200,
                    message = "Successful",
                    data = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    StatusCode = 500,
                    message = "An error occurred while processing the request. " + ex.Message,
                    data = new List<object>(),
                });
            }
        }
        //   [HttpPost("current-holding-edit-delete")]
        //   public IActionResult CurrentHoldingEditDelete(
        //[FromBody] List<HoldingItem> models,
        //[FromQuery] int custId,
        //[FromQuery] string action)
        //   {
        //       try
        //       {
        //           if (custId <= 0 || string.IsNullOrEmpty(action))
        //           {
        //               return BadRequest(new
        //               {
        //                   status = false,
        //                   StatusCode = 400,
        //                   message = "Invalid request parameters.",
        //                   data = new List<object>()
        //               });
        //           }

        //           UserSqlServer usqlre = new UserSqlServer(this);

        //           if (action.ToLower() == "delete")
        //           {
        //               // Delete only for the requested customer and location
        //               if (models == null || models.Count == 0)
        //               {
        //                   return BadRequest(new
        //                   {
        //                       status = false,
        //                       StatusCode = 400,
        //                       message = "Holding item list cannot be empty for delete.",
        //                       data = new List<object>()
        //                   });
        //               }

        //               foreach (var item in models)
        //               {
        //                   string deleteSql = $@"
        //               DELETE FROM inv_current_holding
        //               WHERE ch_as_id = {custId}
        //               AND ch_ir_id = {item.IrId}
        //               AND ISNULL(ch_location_id, 0) = {item.LocationId}";

        //                   usqlre.dbExecute(deleteSql);
        //               }

        //               usqlre.close();

        //               return Ok(new
        //               {
        //                   status = true,
        //                   StatusCode = 200,
        //                   message = "Holdings deleted successfully.",
        //                   data = new List<object>()
        //               });
        //           }
        //           else if (action.ToLower() == "edit")
        //           {
        //               if (models == null || models.Count == 0)
        //               {
        //                   return BadRequest(new
        //                   {
        //                       status = false,
        //                       StatusCode = 400,
        //                       message = "Holding item list cannot be empty for edit.",
        //                       data = new List<object>()
        //                   });
        //               }

        //               foreach (var item in models)
        //               {
        //                   string holdDate = string.IsNullOrWhiteSpace(item.ToDate)
        //                       ? DateTime.Now.ToString("yyyy-MM-dd")
        //                       : item.ToDate;

        //                   // Check record using Customer + Item + Location
        //                   string checkSql = $@"
        //               SELECT COUNT(*)
        //               FROM inv_current_holding
        //               WHERE ch_as_id = {custId}
        //               AND ch_ir_id = {item.IrId}
        //               AND ISNULL(ch_location_id, 0) = {item.LocationId}";

        //                   DataTable dtCheck = usqlre.dbReaderFill(checkSql);

        //                   int recordExists = Convert.ToInt32(dtCheck.Rows[0][0]);

        //                   if (recordExists > 0)
        //                   {
        //                       // Update only the selected Customer + Item + Location
        //                       string updateSql = $@"
        //                   UPDATE inv_current_holding
        //                   SET
        //                       ch_qty = {item.HoldQty},
        //                       ch_date = '{holdDate}',
        //                       ch_location_id = {item.LocationId}
        //                   WHERE ch_as_id = {custId}
        //                   AND ch_ir_id = {item.IrId}
        //                   AND ISNULL(ch_location_id, 0) = {item.LocationId}";

        //                       usqlre.dbExecute(updateSql);
        //                   }
        //               }

        //               usqlre.close();

        //               return Ok(new
        //               {
        //                   status = true,
        //                   StatusCode = 200,
        //                   message = "Holdings updated successfully.",
        //                   data = new List<object>()
        //               });
        //           }
        //           else
        //           {
        //               return BadRequest(new
        //               {
        //                   status = false,
        //                   StatusCode = 400,
        //                   message = "Invalid action. Use 'Edit' or 'Delete'.",
        //                   data = new List<object>()
        //               });
        //           }
        //       }
        //       catch (Exception ex)
        //       {
        //           return StatusCode(500, new
        //           {
        //               status = false,
        //               StatusCode = 500,
        //               message = "An error occurred while processing the request. " + ex.Message,
        //               data = new List<object>()
        //           });
        //       }
        //   }

        //    [HttpPost("current-holding-edit-delete")]
        //    public IActionResult CurrentHoldingEditDelete(
        //[FromBody] List<HoldingItem> models,
        //[FromQuery] int custId,
        //[FromQuery] string action)
        //    {
        //        try
        //        {
        //            if (custId <= 0 || string.IsNullOrWhiteSpace(action))
        //            {
        //                return BadRequest(new
        //                {
        //                    status = false,
        //                    StatusCode = 400,
        //                    message = "Invalid request parameters.",
        //                    data = new List<object>()
        //                });
        //            }

        //            if (models == null || models.Count == 0)
        //            {
        //                return BadRequest(new
        //                {
        //                    status = false,
        //                    StatusCode = 400,
        //                    message = "Holding item list cannot be empty.",
        //                    data = new List<object>()
        //                });
        //            }

        //            UserSqlServer usqlre = new UserSqlServer(this);

        //            // =========================================================
        //            // DELETE
        //            // =========================================================
        //            if (action.Equals("delete", StringComparison.OrdinalIgnoreCase))
        //            {
        //                foreach (var item in models)
        //                {
        //                    string holdDate = string.IsNullOrWhiteSpace(item.ToDate)
        //                        ? DateTime.Now.ToString("yyyy-MM-dd")
        //                        : item.ToDate;

        //                    // Find only the latest record for this
        //                    // Customer + Item + Location + Date
        //                    string findSql = $@"
        //                SELECT MAX(ch_id) AS ch_id
        //                FROM inv_current_holding
        //                WHERE ch_as_id = {custId}
        //                  AND ch_ir_id = {item.IrId}
        //                  AND ISNULL(ch_location_id, 0) = {item.LocationId}
        //                  AND CAST(ch_date AS DATE) = CAST('{holdDate}' AS DATE)";

        //                    DataTable dt = usqlre.dbReaderFill(findSql);

        //                    if (dt.Rows.Count > 0 &&
        //                        dt.Rows[0]["ch_id"] != DBNull.Value)
        //                    {
        //                        int chId = Convert.ToInt32(dt.Rows[0]["ch_id"]);

        //                        string deleteSql = $@"
        //                    DELETE FROM inv_current_holding
        //                    WHERE ch_id = {chId}";

        //                        usqlre.dbExecute(deleteSql);
        //                    }
        //                }

        //                usqlre.close();

        //                return Ok(new
        //                {
        //                    status = true,
        //                    StatusCode = 200,
        //                    message = "Holdings deleted successfully.",
        //                    data = new List<object>()
        //                });
        //            }

        //            // =========================================================
        //            // EDIT
        //            // =========================================================
        //            else if (action.Equals("edit", StringComparison.OrdinalIgnoreCase))
        //            {
        //                foreach (var item in models)
        //                {
        //                    string holdDate = string.IsNullOrWhiteSpace(item.ToDate)
        //                        ? DateTime.Now.ToString("yyyy-MM-dd")
        //                        : item.ToDate;

        //                    // -------------------------------------------------
        //                    // Find ONLY the latest holding record for:
        //                    //
        //                    // Customer + Item + Location + Date
        //                    //
        //                    // This matches the logic used in Sp_current_holding.
        //                    // -------------------------------------------------
        //                    string findSql = $@"
        //                SELECT MAX(ch_id) AS ch_id
        //                FROM inv_current_holding
        //                WHERE ch_as_id = {custId}
        //                  AND ch_ir_id = {item.IrId}
        //                  AND ISNULL(ch_location_id, 0) = {item.LocationId}
        //                  AND CAST(ch_date AS DATE) = CAST('{holdDate}' AS DATE)";

        //                    DataTable dtCheck = usqlre.dbReaderFill(findSql);

        //                    if (dtCheck.Rows.Count > 0 &&
        //                        dtCheck.Rows[0]["ch_id"] != DBNull.Value)
        //                    {
        //                        int chId = Convert.ToInt32(dtCheck.Rows[0]["ch_id"]);

        //                        // -------------------------------------------------
        //                        // Update ONLY that exact record.
        //                        //
        //                        // Do NOT update ch_date.
        //                        // The record already belongs to holdDate.
        //                        // -------------------------------------------------
        //                        string updateSql = $@"
        //                    UPDATE inv_current_holding
        //                    SET
        //                        ch_qty = {item.HoldQty}
        //                    WHERE ch_id = {chId}";

        //                        usqlre.dbExecute(updateSql);
        //                    }
        //                }

        //                usqlre.close();

        //                return Ok(new
        //                {
        //                    status = true,
        //                    StatusCode = 200,
        //                    message = "Holdings updated successfully.",
        //                    data = new List<object>()
        //                });
        //            }

        //            // =========================================================
        //            // INVALID ACTION
        //            // =========================================================
        //            else
        //            {
        //                usqlre.close();

        //                return BadRequest(new
        //                {
        //                    status = false,
        //                    StatusCode = 400,
        //                    message = "Invalid action. Use 'Edit' or 'Delete'.",
        //                    data = new List<object>()
        //                });
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return StatusCode(500, new
        //            {
        //                status = false,
        //                StatusCode = 500,
        //                message = "An error occurred while processing the request. " + ex.Message,
        //                data = new List<object>()
        //            });
        //        }
        //    }

        [HttpPost("current-holding-edit-delete")]
        public IActionResult CurrentHoldingEditDelete(
    [FromBody] List<HoldingItem> models,
    [FromQuery] int custId,
    [FromQuery] string action)
        {
            try
            {
                if (custId <= 0 || string.IsNullOrWhiteSpace(action))
                {
                    return BadRequest(new
                    {
                        status = false,
                        StatusCode = 400,
                        message = "Invalid request parameters.",
                        data = new List<object>()
                    });
                }

                if (models == null || models.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        StatusCode = 400,
                        message = "Holding item list cannot be empty.",
                        data = new List<object>()
                    });
                }

                UserSqlServer usqlre = new UserSqlServer(this);

                // =========================================================
                // DELETE
                // =========================================================
                if (action.Equals("delete", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var item in models)
                    {
                        string holdDate = string.IsNullOrWhiteSpace(item.ToDate)
                            ? DateTime.Now.ToString("yyyy-MM-dd")
                            : item.ToDate;

                        string findSql = $@"
                    SELECT MAX(ch_id) AS ch_id
                    FROM inv_current_holding
                    WHERE ch_as_id = {custId}
                      AND ch_ir_id = {item.IrId}
                      AND ISNULL(ch_location_id, 0) = {item.LocationId}
                      AND CAST(ch_date AS DATE) = CAST('{holdDate}' AS DATE)";

                        DataTable dt = usqlre.dbReaderFill(findSql);

                        if (dt.Rows.Count > 0 &&
                            dt.Rows[0]["ch_id"] != DBNull.Value)
                        {
                            int chId = Convert.ToInt32(dt.Rows[0]["ch_id"]);

                            string deleteSql = $@"
                        DELETE FROM inv_current_holding
                        WHERE ch_id = {chId}";

                            usqlre.dbExecute(deleteSql);
                        }
                    }

                    usqlre.close();

                    return Ok(new
                    {
                        status = true,
                        StatusCode = 200,
                        message = "Holdings deleted successfully.",
                        data = new List<object>()
                    });
                }

                // =========================================================
                // EDIT
                // =========================================================
                else if (action.Equals("edit", StringComparison.OrdinalIgnoreCase))
                {
                    string strForm = "HOLDING";

                    // Same as software:
                    // Get one entry number for newly inserted records
                    int entryNo = 0;
                    bool insertedAny = false;

                    foreach (var item in models)
                    {
                        string holdDate = string.IsNullOrWhiteSpace(item.ToDate)
                            ? DateTime.Now.ToString("yyyy-MM-dd")
                            : item.ToDate;

                        // -------------------------------------------------
                        // Find latest record for:
                        // Customer + Item + Location + Date
                        // -------------------------------------------------
                        string findSql = $@"
                    SELECT MAX(ch_id) AS ch_id
                    FROM inv_current_holding
                    WHERE ch_as_id = {custId}
                      AND ch_ir_id = {item.IrId}
                      AND ISNULL(ch_location_id, 0) = {item.LocationId}
                      AND CAST(ch_date AS DATE) = CAST('{holdDate}' AS DATE)";

                        DataTable dtCheck = usqlre.dbReaderFill(findSql);

                        // -------------------------------------------------
                        // SAME DATE RECORD EXISTS
                        // → UPDATE
                        // -------------------------------------------------
                        if (dtCheck.Rows.Count > 0 &&
                            dtCheck.Rows[0]["ch_id"] != DBNull.Value)
                        {
                            int chId = Convert.ToInt32(dtCheck.Rows[0]["ch_id"]);

                            string updateSql = $@"
                        UPDATE inv_current_holding
                        SET
                            ch_qty = {item.HoldQty},
                            ch_date = '{holdDate}',
                            ch_form = '{strForm}',
                            ch_location_id = {item.LocationId}
                        WHERE ch_id = {chId}";

                            usqlre.dbExecute(updateSql);
                        }
                        // -------------------------------------------------
                        // NO RECORD FOR THIS DATE
                        // → INSERT NEW HISTORY RECORD
                        //
                        // This is the important missing logic in your API.
                        // -------------------------------------------------
                        else
                        {
                            if (!insertedAny)
                            {
                                string entrySql = $@"
                            SELECT ISNULL(MAX(ch_entryno), 0) + 1
                            FROM inv_current_holding
                            WHERE ch_as_id = {custId}";

                                DataTable dtEntry = usqlre.dbReaderFill(entrySql);

                                entryNo = Convert.ToInt32(dtEntry.Rows[0][0]);

                                insertedAny = true;
                            }

                            string insertSql = $@"
                        INSERT INTO inv_current_holding
                        (
                            ch_date,
                            ch_as_id,
                            ch_form,
                            ch_entryno,
                            ch_qty,
                            ch_ir_id,
                            ch_location_id
                        )
                        VALUES
                        (
                            '{holdDate}',
                            {custId},
                            '{strForm}',
                            {entryNo},
                            {item.HoldQty},
                            {item.IrId},
                            {item.LocationId}
                        )";

                            usqlre.dbExecute(insertSql);
                        }
                    }

                    usqlre.close();

                    return Ok(new
                    {
                        status = true,
                        StatusCode = 200,
                        message = "Holdings updated successfully.",
                        data = new List<object>()
                    });
                }

                // =========================================================
                // INVALID ACTION
                // =========================================================
                else
                {
                    usqlre.close();

                    return BadRequest(new
                    {
                        status = false,
                        StatusCode = 400,
                        message = "Invalid action. Use 'Edit' or 'Delete'.",
                        data = new List<object>()
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    StatusCode = 500,
                    message = "An error occurred while processing the request. " + ex.Message,
                    data = new List<object>()
                });
            }
        }

        [HttpPost("current-holding-report")]
        public IActionResult CurrentHoldingReport(
    [FromBody] CurrentHoldingReportRequest model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        StatusCode = 400,
                        message = "Invalid request.",
                        data = new List<object>()
                    });
                }

                if (string.IsNullOrWhiteSpace(model.FromDate) ||
                    string.IsNullOrWhiteSpace(model.ToDate))
                {
                    return BadRequest(new
                    {
                        status = false,
                        StatusCode = 400,
                        message = "FromDate and ToDate are required.",
                        data = new List<object>()
                    });
                }

                DateTime fromDate;
                DateTime toDate;

                if (!DateTime.TryParse(model.FromDate, out fromDate) ||
                    !DateTime.TryParse(model.ToDate, out toDate))
                {
                    return BadRequest(new
                    {
                        status = false,
                        StatusCode = 400,
                        message = "Invalid date format.",
                        data = new List<object>()
                    });
                }

                if (toDate.Date < fromDate.Date)
                {
                    return BadRequest(new
                    {
                        status = false,
                        StatusCode = 400,
                        message = "ToDate cannot be less than FromDate.",
                        data = new List<object>()
                    });
                }

                string statementType =
                    string.IsNullOrWhiteSpace(model.StatementType)
                        ? "select"
                        : model.StatementType.ToLower();

                // Validate StatementType
                var allowedStatementTypes = new[]
                {
            "select",
            "selectgodown",
            "areawise",
            "areawisegodown"
        };

                if (!allowedStatementTypes.Contains(statementType))
                {
                    return BadRequest(new
                    {
                        status = false,
                        StatusCode = 400,
                        message = "Invalid StatementType.",
                        data = new List<object>()
                    });
                }

                UserSqlServer usqlre = new UserSqlServer(this);

                // ---------------------------------------------------------
                // Build WHERE condition for Route / Salesman / Location
                // ---------------------------------------------------------

                List<string> whereConditions = new List<string>();

                // Route filter
                if (model.RouteIds != null && model.RouteIds.Count > 0)
                {
                    string routeIds = string.Join(",",
                        model.RouteIds.Where(x => x > 0));

                    if (!string.IsNullOrEmpty(routeIds))
                    {
                        whereConditions.Add(
                            $"a.as_rout_id IN ({routeIds})");
                    }
                }

                // Salesman filter
                if (model.SalesmanIds != null && model.SalesmanIds.Count > 0)
                {
                    string salesmanIds = string.Join(",",
                        model.SalesmanIds.Where(x => x > 0));

                    if (!string.IsNullOrEmpty(salesmanIds))
                    {
                        whereConditions.Add(
                            $"si.si_commision_acc_id IN ({salesmanIds})");
                    }
                }

                // Location filter
                if (model.LocationIds != null && model.LocationIds.Count > 0)
                {
                    string locationIds = string.Join(",",
                        model.LocationIds.Where(x => x > 0));

                    if (!string.IsNullOrEmpty(locationIds))
                    {
                        whereConditions.Add(
                            $"ISNULL(NULLIF(ch.ch_location_id, 0), si.si_location_id) IN ({locationIds})");
                    }
                }

                string where = "";

                if (whereConditions.Count > 0)
                {
                    where = " AND " + string.Join(" AND ", whereConditions);
                }

                // ---------------------------------------------------------
                // Execute Stored Procedure
                // ---------------------------------------------------------

                string sql = $@"
EXEC dbo.Sp_current_holding
    @from_date = '{fromDate:yyyy-MM-dd}',
    @to_date = '{toDate:yyyy-MM-dd}',
    @led_id = {Math.Max(model.CustomerId, 0)},
    @ir_id = {Math.Max(model.ItemId, 0)},
    @where = N'{where.Replace("'", "''")}',
    @StatementType = N'{statementType.Replace("'", "''")}'
";

                DataTable dt = usqlre.dbReaderFill(sql);

                usqlre.close();

                // ---------------------------------------------------------
                // Convert DataTable to API response
                // ---------------------------------------------------------

                var data = new List<object>();

                foreach (DataRow row in dt.Rows)
                {
                    data.Add(new
                    {
                        customer = row["Customer"] == DBNull.Value
                            ? null
                            : row["Customer"].ToString(),

                        itemName = row["Item Name"] == DBNull.Value
                            ? null
                            : row["Item Name"].ToString(),

                        entryNo = row["EntryNo"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(row["EntryNo"]),

                        date = row["Date"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(row["Date"]).ToString("yyyy-MM-dd"),

                        form = row["Form"] == DBNull.Value
                            ? null
                            : row["Form"].ToString(),

                        holdQty = row["HoldQty"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(row["HoldQty"]),

                        route = row["Route"] == DBNull.Value
                            ? null
                            : row["Route"].ToString(),

                        salesman = row["Salesman"] == DBNull.Value
                            ? null
                            : row["Salesman"].ToString(),

                        location = row["Location"] == DBNull.Value
                            ? null
                            : row["Location"].ToString()
                    });
                }

                return Ok(new
                {
                    status = true,
                    StatusCode = 200,
                    message = "Current holding report fetched successfully.",
                    data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    StatusCode = 500,
                    message = "An error occurred while fetching current holding report. " + ex.Message,
                    data = new List<object>()
                });
            }
        }
        [HttpGet("shop-visit-report")]
        public IActionResult ShopVisitReport([FromQuery] DateTime inputDate)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                string query;

                if (usqlre.routeId == "0")
                {
                    query = $@"
                DECLARE @input_date DATE = '{inputDate:yyyy-MM-dd}';
                SELECT 
                    as_name, 
                    MAX(si_date) AS si_date, 
                    r_name,
                    DATEDIFF(DAY, MAX(si_date), @input_date) - 5 AS days_difference
                FROM inv_sales_inf
                INNER JOIN acc_subhead ON as_id = si_acc_id
                LEFT JOIN inv_rout_reg ON r_id = as_rout_id
                WHERE as_ap_id = 4 AND si_date > '2025-07-18' and as_active=1 
                GROUP BY as_name, r_name
                HAVING DATEDIFF(DAY, MAX(si_date), @input_date) > 5
                ORDER BY si_date DESC;";
                }
                else
                {
                    query = $@"
                DECLARE @input_date DATE = '{inputDate:yyyy-MM-dd}';
                SELECT 
                    as_name, 
                    MAX(si_date) AS si_date, 
                    r_name,
                    DATEDIFF(DAY, MAX(si_date), @input_date) - 5 AS days_difference
                    FROM inv_sales_inf
                INNER JOIN acc_subhead ON as_id = si_acc_id
                LEFT JOIN inv_rout_reg ON r_id = as_rout_id
                WHERE as_ap_id = 4 AND as_rout_id in ({usqlre.routeId}) AND si_date > '2025-07-18'  and as_active=1 
                GROUP BY as_name, r_name
                HAVING DATEDIFF(DAY, MAX(si_date), @input_date) > 5
                ORDER BY si_date DESC;";
                }

                DataTable dt = usqlre.dbReaderFill(query);
                var customerList = new List<object>();

                foreach (DataRow row in dt.Rows)
                {
                    customerList.Add(new
                    {
                        customer = row["as_name"].ToString(),
                        route = row["r_name"].ToString(),
                        days = Convert.ToInt32(row["days_difference"])
                    });
                }

                return Ok(new
                {
                    status = true,
                    StatusCode = 200,
                    message = "Customers fetched successfully.",
                    data = customerList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    StatusCode = 500,
                    message = "Error: " + ex.Message,
                    data = new List<object>()
                });
            }
        }

      

        [HttpGet("workorder-agewise-report")]
        public IActionResult WorkorderAgewiseReport(
     DateTime fromDate,
     DateTime toDate,
     int led_id = 0,
     int loc_id = 0,
     bool pendingOnly = false)
        {
            UserSqlServer usqlre = null;

            try
            {
                usqlre = new UserSqlServer(this);

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@wi_from_date", fromDate);
                cmd.Parameters.AddWithValue("@wi_to_date", toDate);
                cmd.Parameters.AddWithValue("@led_id", led_id);
                cmd.Parameters.AddWithValue("@loc_id", loc_id);
                cmd.Parameters.AddWithValue("@user_id", 1);
                cmd.Parameters.AddWithValue("@groupwhere", "");
                cmd.Parameters.AddWithValue("@StatementType", "WorkorderAgewise");

                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                adp.Fill(ds);

                var workorderList = new List<object>();

                DataTable dtWorkorder = ds.Tables[0];
                DataTable dtComplaint = ds.Tables.Count > 1 ? ds.Tables[1] : null;

                foreach (DataRow row in dtWorkorder.Rows)
                {
                    string billNo = row["BILL NO"].ToString().Trim();

                    var complaints = new List<object>();

                    if (dtComplaint != null)
                    {
                        var complaintRows = dtComplaint.AsEnumerable()
                            .Where(x => x["BillNo"].ToString().Trim() == billNo);

                        foreach (var c in complaintRows)
                        {
                            complaints.Add(new
                            {
                                complaintName = c["ComplaintName"]?.ToString(),
                                complaintRemarks = c["ComplaintRemarks"]?.ToString()
                            });
                        }
                    }

                    workorderList.Add(new
                    {
                        billNo = billNo,
                        date = Convert.ToDateTime(row["DATE"]),
                        qtnNo = row["QtnNo"]?.ToString(),
                        customer = row["Customer"]?.ToString(),
                        route = row["Route"]?.ToString(),
                        serviceItemDetails = row["ServiceItemDetails"]?.ToString(),
                        model = row["Model"]?.ToString(),
                        invoicedAmount = Convert.ToDecimal(row["Invoiced Amount"]),
                        paidAmount = Convert.ToDecimal(row["Paid Amount"]),
                        pendingBillAmount = Convert.ToDecimal(row["PENDING BILL AMOUNT"]),
                        ageOfBill = Convert.ToInt32(row["Age Of Bill"]),
                        salesMan = row["SalesMan"]?.ToString(),
                        dueDays = Convert.ToInt32(row["Due Days"]),
                        complaint = complaints
                    });
                }

                if (pendingOnly)
                {
                    workorderList = workorderList
                        .Where(x =>
                        {
                            var prop = x.GetType().GetProperty("pendingBillAmount");
                            return prop != null &&
                                   Convert.ToDecimal(prop.GetValue(x)) > 0;
                        })
                        .ToList();
                }

                return Ok(new
                {
                    status = true,
                    workorder = workorderList
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }
        }

        [HttpGet("get-subcategories")]
        public IActionResult GetSubCategories()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = @"SELECT  sc_id, sc_name  FROM inv_subcategory ORDER BY sc_name";

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();

            return Ok(ReportModelContext.searializeDt(dt));
        }
        [HttpGet("get-manufacturers")]
        public IActionResult GetManufacturers()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = @"SELECT  m_id, m_name FROM inv_mfr ORDER BY m_name";

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();

            return Ok(ReportModelContext.searializeDt(dt));
        }
        [HttpGet("get-image-by-id")]
        public IActionResult GetImageById(int id)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                string sql = @"SELECT ir_image_data FROM inv_item_reg   WHERE ir_id = "+id+" ";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                object imageData = null;

                if (dt.Rows.Count > 0)
                {
                    imageData = new
                    {
                        image = dt.Rows[0]["ir_image_data"] == DBNull.Value
                            ? null
                            : Convert.ToBase64String((byte[])dt.Rows[0]["ir_image_data"])
                    };
                }

                return Ok(new
                {
                    status = true,
                    StatusCode = 200,
                    message = "Image fetched successfully.",
                    data = imageData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    StatusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
    }

    // Request contract used only by /group-report-new.  It extends the
    // existing report request so no other endpoint contract is changed.
    public class GroupReportNewRequest : AccReportModel
    {
        [DefaultValue(0)]
        public new int? agingdays { get; set; } = 0;

        [DefaultValue(false)]
        public bool billWiseAging { get; set; } = false;

        [DefaultValue(false)]
        public bool balanceOnly { get; set; } = false;

        [DefaultValue(false)]
        public bool excludePending { get; set; } = false;

        [DefaultValue(false)]
        public new bool ledgerExcludePending { get; set; } = false;

        public int areaId { get; set; }
        public List<int> areaIds { get; set; }
        public List<int> routeIds { get; set; }
        public List<int> salesmanIds { get; set; }
        public List<string> categories { get; set; }
        public List<string> ratings { get; set; }
        public List<int> isrIds { get; set; }
    }
}
