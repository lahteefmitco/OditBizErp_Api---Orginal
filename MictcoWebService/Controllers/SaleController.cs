using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;
using System.Text;


namespace MictcoWebService.Controllers
{

    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class SaleController : ControllerBase
    {
        [HttpPost("save-sales")]
        public async Task<IActionResult> saveSales([FromBody] SaleModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            //int entryNo = usqlre.stmtSave(model.statement, model);
            
            if(usqlre.FinancialDateCheck(DateTime.Parse(model.date))==false)
            {
                return Ok(new { status = false, entryNo = 0, si_einvoice_ksa = "0", si_loc_entryno = "0" });
            }
            string sql1 = "SELECT COUNT(1) FROM inv_sales_inf WHERE si_str_id=" + model.sistrId + " and si_entryno = " + model.entryNo + "";
            DataTable entryno = usqlre.dbReaderFill(sql1);
            if (model.statement == "Update" && entryno.Rows.Count > 0 && Convert.ToInt32(entryno.Rows[0][0].ToString()) == 0)
            {
                return Ok(new { status = false, msg = "Sales does not exists" });
            }
            DataTable dt = usqlre.stmtSave(model.statement, model);
            //int entryNo = model.statement == "Insert" ? Convert.ToInt32(dt.Rows[0]["tres"].ToString()) : model.entryNo;
            int entryNo = model.statement == "Insert"
    ? (int.TryParse(dt.Rows[0]["tres"]?.ToString(), out int parsedEntryNo) ? parsedEntryNo : 0)
    : model.entryNo;
            string si_einvoice_ksa = dt.Rows[0]["einvoiceksa"].ToString();
            int newEntryno = model.entryNo == 0 ? entryNo : model.entryNo;
            string sql = "select si_loc_entryno from inv_sales_inf where si_str_id=" + model.sistrId + " and si_entryno=" + newEntryno + "";
            DataTable locdt = usqlre.dbReaderFill(sql);
            string si_loc_entryno = "";
            usqlre.close();
            if (locdt.Rows.Count > 0)
            {
                try
                {
                    si_loc_entryno = locdt.Rows[0]["si_loc_entryno"].ToString();
                }
                catch (Exception ex)
                {

                }
            }

            //return Ok(new { status = true,entryNo = entryNo });
            return Ok(new { status = entryNo > 0 ? true : false, entryNo = entryNo, si_einvoice_ksa = si_einvoice_ksa, si_loc_entryno = si_loc_entryno });
        }
        class response
        {
            public string location_entryno { get; set; }
            public string min_entry_no { get; set; }
            public string max_entry_no { get; set; }
        }
        [HttpGet("last-sales-invoice/{id}")]
        public string lastSalesInvoice(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT max(si_entryno) as max_entry_no,min(si_entryno) as min_entry_no from inv_sales_inf WHERE si_str_id=" + id + "";
            //string sql = "SELECT COALESCE(max(si_entryno),0) as max_entry_no,COALESCE(min(si_entryno),0) as min_entry_no from inv_sales_inf WHERE si_str_id=" + id + "";
            //string Newsql = "select top 1 LTRIM(RTRIM(dbo.generate_entry_no(si_loc_entryno_only+1," + usqlre.locationId + "," + id + "))) as location_entryno from inv_sales_inf where si_str_id=" + id + " and si_location_id='" + usqlre.locationId + "' order by si_entryno desc";
            string Newsql = "select top 1 LTRIM(RTRIM(dbo.generate_entry_no( COALESCE((select top 1 si_loc_entryno_only from inv_sales_inf where si_str_id=" + id + " and si_location_id=" + usqlre.locationId + " order by si_entryno desc),0)+1," + usqlre.locationId + "," + id + "))) as location_entryno from inv_sales_inf ";
            DataTable dt = usqlre.dbReaderFill(sql);
            DataTable dtNew = usqlre.dbReaderFill(Newsql);
            usqlre.close();

            DataTable ReturnDt = new DataTable();
            ReturnDt.Columns.Add("location_entryno");
            ReturnDt.Columns.Add("max_entry_no");
            ReturnDt.Columns.Add("min_entry_no");

            foreach (DataRow row in dtNew.Rows)
            {
                ReturnDt.Rows.Add();
                ReturnDt.Rows[0]["location_entryno"] = dtNew.Rows[0]["location_entryno"].ToString();

            }
            foreach (DataRow row in dt.Rows)
            {
                ReturnDt.Rows[0]["max_entry_no"] = dt.Rows[0]["max_entry_no"].ToString();
                ReturnDt.Rows[0]["min_entry_no"] = dt.Rows[0]["min_entry_no"].ToString();
            }

            return ReportModelContext.searializeDt(ReturnDt);

        }

        [HttpPost("search-sales")]
        public async Task<IActionResult> saveSales([FromBody] EntrySearchModel model)
        {
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            UserSqlServer usqlre = new UserSqlServer(this);
            DataTable dt = new DataTable();
            string query = "";
            //query = "SELECT si_entryno, si_acc_id, si_cust_name, si_salesman_id, si_location_id, si_remarks, ISNULL(cast(si_gross_value as nvarchar(100)), '0') si_gross_value,ISNULL(cast(si_disc as nvarchar(100)), '0') si_disc,ISNULL(cast(si_disc_per as nvarchar(100)), '0') si_disc_per,ISNULL(cast(si_net_amount as nvarchar(100)), '0') si_net_amount,ISNULL(cast(si_tax as nvarchar(100)), '0') si_tax,ISNULL(cast(si_total as nvarchar(100)), '0') im_rate,ISNULL(cast(si_loading_charge as nvarchar(100)), '0') im_rate,ISNULL(cast(si_other_charge as nvarchar(100)), '0') si_other_charge,ISNULL(cast(si_other_disc as nvarchar(100)), '0') si_other_disc,ISNULL(cast(si_profit as nvarchar(100)), '0') si_profit,ISNULL(cast(si_cash_recieved as nvarchar(100)), '0') si_cash_recieved,ISNULL(cast(si_balance as nvarchar(100)), '0') si_balance,ISNULL(cast(si_grand_total as nvarchar(100)), '0') si_grand_total,ISNULL(cast(si_igst_total as nvarchar(100)), '0') si_igst_total,ISNULL(cast(si_cgst_total as nvarchar(100)), '0') si_cgst_total,ISNULL(cast(si_sgst_total as nvarchar(100)), '0') si_sgst_total,ISNULL(cast(si_ob as nvarchar(100)), '0') si_ob,ISNULL(cast(si_net_balance as nvarchar(100)), '0') si_net_balance,ISNULL(cast(si_freight_charge as nvarchar(100)), '0') si_freight_charge,ISNULL(cast(si_sum_kfc as nvarchar(100)), '0') si_sum_kfc,ISNULL(cast(si_total as nvarchar(100)), '0') si_total,si_cash_paid_acc,si_date FROM inv_sales_inf WHERE si_entryno =" + model.entryNo + "";
            if (usqlre.get_android_settings("ENABLE FORMATTED INVOICENO"))
            {
                query += @"SELECT si_entryno,CASE 
                WHEN si_str_id = 1  THEN ISNULL(gl_short_1, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_1, '')
                WHEN si_str_id = 2  THEN ISNULL(gl_short_2, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id IN (3, 13) THEN ISNULL(gl_short_3, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 4  THEN ISNULL(gl_short_4, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 5  THEN ISNULL(gl_short_5, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 6  THEN ISNULL(gl_short_6, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_6, '')
                WHEN si_str_id = 7  THEN ISNULL(gl_short_7, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_7, '')
                WHEN si_str_id = 8  THEN ISNULL(gl_short_8, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 9  THEN ISNULL(gl_short_9, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_9, '')
                WHEN si_str_id = 10 THEN ISNULL(gl_short_10, '') + CAST(si_entryno AS NVARCHAR)
                ELSE CAST(si_entryno AS NVARCHAR)
                END AS si_loc_entryno,si_acc_id,si_cust_name,si_salesman_id,si_location_id,si_remarks,ISNULL(cast(si_gross_value as nvarchar(100)),'0') si_gross_value,ISNULL(cast(si_disc as nvarchar(100)),'0') si_disc,ISNULL(cast(si_disc_per as nvarchar(100)),'0') si_disc_per,ISNULL(cast(si_net_amount as nvarchar(100)),'0') si_net_amount,ISNULL(cast(si_tax as nvarchar(100)),'0') si_tax,ISNULL(cast(si_total as nvarchar(100)),'0') im_rate,ISNULL(cast(si_loading_charge as nvarchar(100)),'0') im_rate,ISNULL(cast(si_other_charge as nvarchar(100)),'0') si_other_charge,ISNULL(cast(si_other_disc as nvarchar(100)),'0') si_other_disc,ISNULL(cast(si_profit as nvarchar(100)),'0') si_profit,ISNULL(cast(si_cash_recieved as nvarchar(100)),'0') si_cash_recieved,ISNULL(cast(si_balance as nvarchar(100)),'0') si_balance,ISNULL(cast(si_grand_total as nvarchar(100)),'0') si_grand_total,ISNULL(cast(si_igst_total as nvarchar(100)),'0') si_igst_total,ISNULL(cast(si_cgst_total as nvarchar(100)),'0') si_cgst_total,ISNULL(cast(si_sgst_total as nvarchar(100)),'0') si_sgst_total,ISNULL(cast(si_ob as nvarchar(100)),'0') si_ob,ISNULL(cast(si_net_balance as nvarchar(100)),'0') si_net_balance,ISNULL(cast(si_freight_charge as nvarchar(100)),'0') si_freight_charge,ISNULL(cast(si_sum_kfc as nvarchar(100)),'0') si_sum_kfc,ISNULL(cast(si_total as nvarchar(100)),'0') si_total,si_cash_paid_acc,CONVERT(date, si_date) as si_date,si_lc_id,si_date as [datetime],si_einvoice_ksa,si_commision_acc_id,lc_mob,si_disp_name,si_disp_location,si_disp_add1,si_disp_scode,si_disp_pincode,si_ship_name,si_ship_location,si_ship_add1,si_ship_scode,si_ship_pincode,si_ship_gstin ,si_bankacc,si_card_amount FROM inv_sales_inf left join acc_loyalty_card on lc_id=si_lc_id  LEFT JOIN gnl_location ON si_location_id = gl_id WHERE si_entryno=" + model.entryNo + "";

            }

            else
            {
                query += "SELECT si_entryno,si_loc_entryno,si_acc_id,si_cust_name,si_salesman_id,si_location_id,si_remarks,ISNULL(cast(si_gross_value as nvarchar(100)),'0') si_gross_value,ISNULL(cast(si_disc as nvarchar(100)),'0') si_disc,ISNULL(cast(si_disc_per as nvarchar(100)),'0') si_disc_per,ISNULL(cast(si_net_amount as nvarchar(100)),'0') si_net_amount,ISNULL(cast(si_tax as nvarchar(100)),'0') si_tax,ISNULL(cast(si_total as nvarchar(100)),'0') im_rate,ISNULL(cast(si_loading_charge as nvarchar(100)),'0') im_rate,ISNULL(cast(si_other_charge as nvarchar(100)),'0') si_other_charge,ISNULL(cast(si_other_disc as nvarchar(100)),'0') si_other_disc,ISNULL(cast(si_profit as nvarchar(100)),'0') si_profit,ISNULL(cast(si_cash_recieved as nvarchar(100)),'0') si_cash_recieved,ISNULL(cast(si_balance as nvarchar(100)),'0') si_balance,ISNULL(cast(si_grand_total as nvarchar(100)),'0') si_grand_total,ISNULL(cast(si_igst_total as nvarchar(100)),'0') si_igst_total,ISNULL(cast(si_cgst_total as nvarchar(100)),'0') si_cgst_total,ISNULL(cast(si_sgst_total as nvarchar(100)),'0') si_sgst_total,ISNULL(cast(si_ob as nvarchar(100)),'0') si_ob,ISNULL(cast(si_net_balance as nvarchar(100)),'0') si_net_balance,ISNULL(cast(si_freight_charge as nvarchar(100)),'0') si_freight_charge,ISNULL(cast(si_sum_kfc as nvarchar(100)),'0') si_sum_kfc,ISNULL(cast(si_total as nvarchar(100)),'0') si_total,si_cash_paid_acc,CONVERT(date, si_date) as si_date,si_lc_id,si_date as [datetime],si_einvoice_ksa,si_commision_acc_id,lc_mob,si_disp_name,si_disp_location,si_disp_add1,si_disp_scode,si_disp_pincode,si_ship_name,si_ship_location,si_ship_add1,si_ship_scode,si_ship_pincode,si_ship_gstin ,si_bankacc,si_card_amount FROM inv_sales_inf left join acc_loyalty_card on lc_id=si_lc_id WHERE si_entryno=" + model.entryNo + "";

            }
            query += "and si_str_id = " + model.siStrId + "";
            if (usqlre.user_role != "ADMIN")
            {
                query += " and si_location_id = '" + usqlre.locationId + "'";
            }
            dt = usqlre.dbReaderFill(query);
            hash.Add("main", dt);
            string extraColumns = "";

            if (usqlre.get_android_settings("ENABLE MAX AND MIN RATE IN SALE"))
            {
                extraColumns = ", ir_min_rate AS MinRate, ir_max_rate AS MaxRate";
            }
            query = "SELECT sp_id,sp_str_id,sp_entryno,sp_uniquecode,sp_ir_id,ISNULL(cast(sp_rate as nvarchar(100)),'0') sp_rate,ISNULL(cast(sp_realrate as nvarchar(100)),'0') sp_realrate,sp_qty,sp_fqty,ISNULL(cast(sp_gross_value as nvarchar(100)),'0') sp_gross_value,sp_disc_per,ISNULL(cast(sp_disc as nvarchar(100)),'0') sp_disc,ISNULL(cast(sp_real_disc as nvarchar(100)),'0') sp_real_disc,ISNULL(cast(sp_net_amount as nvarchar(100)),'0') sp_net_amount,ISNULL(cast(sp_tax as nvarchar(100)),'0') sp_tax,ISNULL(cast(sp_total as nvarchar(100)),'0') sp_total,ISNULL(cast(sp_profit as nvarchar(100)),'0') sp_profit,ISNULL(cast(sp_igst as nvarchar(100)),'0') sp_igst,ISNULL(cast(sp_cgst as nvarchar(100)),'0') sp_cgst,ISNULL(cast(sp_sgst as nvarchar(100)),'0') sp_sgst,ISNULL(cast(sp_mrp as nvarchar(100)),'0') sp_mrp,ISNULL(cast(sp_qty_multi_unit as nvarchar(100)),'0') sp_qty_multi_unit,ISNULL(cast(sp_srate_multiunit as nvarchar(100)),'0') sp_srate_multiunit,ISNULL(cast(sp_kfc as nvarchar(100)),'0') sp_kfc,ISNULL(cast(sp_prate as nvarchar(100)),'0') sp_prate,ISNULL(cast(sp_cost as nvarchar(100)),'0') sp_cost,sp_unit_multi,ir_name,ir_hsn_code,ISNULL(cast(sp_realprate as nvarchar(100)),'0') sp_realprate,ir_allow_negative ,isnull(sp_narration,'') as sp_narration, isnull(sp_narration1,'') as sp_narration1 ,as_name as supplierName " + extraColumns + @"  FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id LEFT JOIN (SELECT ir_id, MAX(Sup) AS Sup, MAX(Cost) AS Cost FROM View_Stock GROUP BY ir_id) vs ON inv_item_reg.ir_id = vs.ir_id LEFT JOIN acc_subhead s ON vs.Sup = s.as_id  WHERE sp_entryno=" + model.entryNo + " and sp_str_id=" + model.siStrId + "";
            dt = usqlre.dbReaderFill(query);
            hash.Add("products", dt);
            query = "SELECT li_in,li_out,li_remarks as Remarks, li_ir_id as complaintId, li_ift_id as fixTypeId, li_ir_mrp as amount FROM inv_lend_item_transactions LEFT JOIN inv_item_reg ON li_ir_id = ir_id LEFT JOIN inv_sales_type_reg ON str_name=li_form WHERE li_entryno=" + model.entryNo + " and str_id=" + model.siStrId + "";
            dt = usqlre.dbReaderFill(query);
            hash.Add("complaints", dt);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(hash));
        }
        
        [HttpPost("sale-report")]
        public async Task<IActionResult> saleReport([FromBody] SaleReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            DataTable dt_report;
            DataTable select;
            if (model.dayWise)
            {
                String query = "EXEC [dbo].[Sp_Report_Sales] @sales_type = " + model.stypes[0] + ",@from_date = '" + model.fromDate + "',@to_date = '" + model.toDate + "' " + ",@StatementType = 'daywise'";
                DataSet ds = usqlre.dbreadDataset(query);
                usqlre.close();
                return Ok(ReportModelContext.searializeDt(ds.Tables[0]));
            }
            else if (model.itemWise)
            {
                string sqlwhere = "";
                if (model.toDate != "")
                    sqlwhere += " where cast(si_date as date) <=''" + model.toDate + "''";

                if (model.fromDate != "")
                {
                    if (sqlwhere == "")
                        sqlwhere += " cast(si_date as date) >=''" + model.fromDate + "''";
                    else
                        sqlwhere += " and cast(si_date as date) >=''" + model.fromDate + "''";
                }
                
                if (model.customer > 0)
                {
                    if (sqlwhere == "")
                        sqlwhere += " si_acc_id =''" + model.customer + "''";
                    else
                        sqlwhere += " and si_acc_id =''" + model.customer + "''";

                }

                if (model.salesMan > 0)
                {
                    if (model.reportAccReg == false)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " si_commision_acc_id =''" + model.salesMan + "''";
                        else
                            sqlwhere += " and si_commision_acc_id =''" + model.salesMan + "''";
                    }

                }

                if (model.location > 0)
                {
                    if (sqlwhere == "")
                        sqlwhere += " si_location_id =''" + model.location + "''";
                    else
                        sqlwhere += " and si_location_id =''" + model.location + "''";
                }


                if (model.district > 0)
                {
                    if (sqlwhere == "")
                        sqlwhere += " dct_id =''" + model.district + "''";
                    else
                        sqlwhere += " and dct_id =''" + model.district + "''";

                }



                if (model.area > 0)
                {
                    if (sqlwhere == "")
                        sqlwhere += " area_id =''" + model.area + "''";
                    else
                        sqlwhere += " and area_id =''" + model.area + "''";

                }

                if (usqlre.user_role != "ADMIN")
                {
                    if (model.reportAccReg == false)
                    {
                        if (usqlre.get_android_settings("SALESMANWISESALESREPORT"))
                        {
                            if (sqlwhere == "")
                            {
                                sqlwhere += " si_commision_acc_id =''" + usqlre.gu_acc_id + "''";
                            }
                            else
                            {
                                sqlwhere += " and si_commision_acc_id =''" + usqlre.gu_acc_id + "''";
                            }
                        }
                        else
                        {
                            if (sqlwhere == "")
                            {
                                sqlwhere += " si_user_id =''" + usqlre.userId + "''";
                            }
                            else
                            {
                                sqlwhere += " and si_user_id =''" + usqlre.userId + "''";
                            }
                        }
                    }
                }
                if (model.routeId > 0)
                {
                    if (sqlwhere == "")
                        sqlwhere += " as_rout_id =''" + model.routeId + "''";
                    else
                        sqlwhere += " and as_rout_id =''" + model.routeId + "''";
                }
                if (model.stypes != null)
                {
                    if (model.stypes.Length > 0)
                    {

                        if (sqlwhere == "")
                        {
                            sqlwhere += " ( ";
                        }
                        else
                        {
                            sqlwhere += " and ( ";
                        }

                        for (int i = 0; i < model.stypes.Length; i++)
                        {
                            if (i == 0)
                            {
                                sqlwhere += " si_str_id =''" + model.stypes[i] + "'' ";
                            }
                            else
                            {
                                sqlwhere += " or si_str_id =''" + model.stypes[i] + "'' ";
                            }
                        }
                        sqlwhere += " ) '";
                    }
                }
                String query = "EXEC [dbo].[Sp_Report_Sales] @sales_type = " + model.stypes[0] + ",@from_date = '" + model.fromDate + "',@to_date = '" + model.toDate + "' " + ",@StatementType = 'itemwise',@sqlwhere='"+sqlwhere+"";
                DataSet ds = usqlre.dbreadDataset(query);
                usqlre.close();
                return Ok(ReportModelContext.searializeDt(ds.Tables[0]));
            }
            else
            {
                if (model.reprtType == "summary")
                {
                    string sqlwhere = "";

                    select = usqlre.getReportColumns("SalesSummary");
                    if (model.toDate != "")
                        sqlwhere += " cast(si_date as date) <='" + model.toDate + "'";

                    if (model.fromDate != "")
                    {
                        if (sqlwhere == "")
                            sqlwhere += " cast(si_date as date) >='" + model.fromDate + "'";
                        else
                            sqlwhere += " and cast(si_date as date) >='" + model.fromDate + "'";
                    }

                    if (model.customer > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " si_acc_id ='" + model.customer + "'";
                        else
                            sqlwhere += " and si_acc_id ='" + model.customer + "'";

                    }

                    if (model.salesMan > 0)
                    {
                        if (model.reportAccReg == false)
                        {
                            if (sqlwhere == "")
                                sqlwhere += " si_commision_acc_id ='" + model.salesMan + "'";
                            else
                                sqlwhere += " and si_commision_acc_id ='" + model.salesMan + "'";
                        }

                    }

                    if (model.location > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " si_location_id ='" + model.location + "'";
                        else
                            sqlwhere += " and si_location_id ='" + model.location + "'";
                    }


                    if (model.district > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " dct_id ='" + model.district + "'";
                        else
                            sqlwhere += " and dct_id ='" + model.district + "'";

                    }



                    if (model.area > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " area_id ='" + model.area + "'";
                        else
                            sqlwhere += " and area_id ='" + model.area + "'";

                    }

                    if (usqlre.user_role != "ADMIN")
                    {
                        if (model.reportAccReg == false)
                        {
                            if (usqlre.get_android_settings("SALESMANWISESALESREPORT"))
                            {
                                if (sqlwhere == "")
                                {
                                    sqlwhere += " si_commision_acc_id ='" + usqlre.gu_acc_id + "'";
                                }
                                else
                                {
                                    sqlwhere += " and si_commision_acc_id ='" + usqlre.gu_acc_id + "'";
                                }
                            }
                            else
                            {
                                if (sqlwhere == "")
                                {
                                    sqlwhere += " si_user_id ='" + usqlre.userId + "'";
                                }
                                else
                                {
                                    sqlwhere += " and si_user_id ='" + usqlre.userId + "'";
                                }
                            }
                        }
                    }

                    string sqlwherer = sqlwhere;

                    if (model.stypes != null)
                    {
                        if (model.stypes.Length > 0)
                        {

                            if (sqlwhere == "")
                            {
                                sqlwhere += " ( ";
                            }
                            else
                            {
                                sqlwhere += " and ( ";
                            }

                            for (int i = 0; i < model.stypes.Length; i++)
                            {
                                if (i == 0)
                                {
                                    sqlwhere += " si_str_id ='" + model.stypes[i] + "' ";
                                }
                                else
                                {
                                    sqlwhere += " or si_str_id ='" + model.stypes[i] + "' ";
                                }
                            }
                            sqlwhere += " ) ";
                        }
                    }

                    if (model.routeId > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " as_rout_id ='" + model.routeId + "'";
                        else
                            sqlwhere += " and as_rout_id ='" + model.routeId + "'";
                    }


                    string queryi = "";
                    if (select.Rows.Count > 0)
                    {
                        if (sqlwhere == "")
                        {
                            queryi = "SELECT " + select.Rows[0]["grc_m_select"].ToString() + " from inv_sales_inf  inner join acc_subhead on si_acc_id = as_id left join inv_district on as_district_id = dct_id left join acc_area on as_area_id = area_id order by si_entryno desc";
                        }
                        else
                        {
                            queryi = "SELECT " + select.Rows[0]["grc_m_select"].ToString() + " from inv_sales_inf  inner join acc_subhead on si_acc_id = as_id left join inv_district on as_district_id = dct_id left join acc_area on as_area_id = area_id where " + sqlwhere + " order by si_entryno desc";
                            if (model.reportAccReg)
                            {
                                queryi = "SELECT " + select.Rows[0]["grc_m_select"].ToString() + " from inv_sales_inf  inner join acc_subhead on si_acc_id = as_id left join inv_district on as_district_id = dct_id left join acc_area on as_area_id = area_id where " + sqlwhere + " and as_salesman_id = '" + model.salesMan + "' order by si_entryno desc";
                            }
                        }
                    }
                    dt_report = usqlre.dbReaderFill(queryi);

                }
                else
                {
                    string sqlwhere = "";
                    select = usqlre.getReportColumns("SalesDetailed");

                    if (model.toDate != "")
                        sqlwhere += " cast(si.si_date as date) <='" + model.toDate + "'";

                    if (model.fromDate != "")
                    {
                        if (sqlwhere == "")
                        {
                            sqlwhere += " cast(si.si_date as date) >='" + model.fromDate + "'";

                        }
                        else
                        {
                            sqlwhere += " and cast(si.si_date as date) >='" + model.fromDate + "'";
                        }
                    }

                    // if($ir_id!=0)
                    // {
                    // 	if (empty($sqlwhere))
                    // 		$sqlwhere .= " ir.ir_id =".$ir_id."";
                    // 	else
                    // 		$sqlwhere .= " and ir.ir_id =".$ir_id."";
                    // }

                    if (model.customer > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " si.si_acc_id ='" + model.customer + "'";
                        else
                            sqlwhere += " and si.si_acc_id ='" + model.customer + "'";
                    }

                    if (usqlre.user_role != "ADMIN")
                    {
                        if (model.reportAccReg == false)
                        {
                            if (usqlre.get_android_settings("SALESMANWISESALESREPORT"))
                            {
                                if (sqlwhere == "")
                                {
                                    sqlwhere += " si_commision_acc_id ='" + usqlre.gu_acc_id + "'";
                                }
                                else
                                {
                                    sqlwhere += " and si_commision_acc_id ='" + usqlre.gu_acc_id + "'";
                                }
                            }
                            else
                            {
                                if (sqlwhere == "")
                                {
                                    sqlwhere += " si.si_user_id ='" + usqlre.userId + "'";
                                }
                                else
                                {
                                    sqlwhere += " and si.si_user_id ='" + usqlre.userId + "'";
                                }
                            }

                        }
                    }

                    if (model.salesMan > 0)
                    {
                        if (model.reportAccReg == false)
                        {
                            if (sqlwhere == "")
                                sqlwhere += " si.si_commision_acc_id ='" + model.salesMan + "'";
                            else
                                sqlwhere += " and si.si_commision_acc_id ='" + model.salesMan + "'";
                        }
                    }

                    if (model.location > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " si.si_location_id ='" + model.location + "'";
                        else
                            sqlwhere += " and si.si_location_id ='" + model.location + "'";

                    }


                    if (model.category > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " ir.ir_category_id ='" + model.category + "'";
                        else
                            sqlwhere += " and ir.ir_category_id ='" + model.category + "'";
                    }
                    if (model.subCategory > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " ir.ir_sub_category_id ='" + model.subCategory + "'";
                        else
                            sqlwhere += " and ir.ir_sub_category_id ='" + model.subCategory + "'";
                    }

                    if (model.brand > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " ib.b_brand ='" + model.brand + "'";
                        else
                            sqlwhere += " and ib.b_brand ='" + model.brand + "'";
                    }


                    if (model.district > 0)
                    {
                        if (sqlwhere != "")
                            sqlwhere += " dct_id ='" + model.district + "'";
                        else
                            sqlwhere += " and dct_id ='" + model.district + "'";

                    }

                    if (model.area > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " area_id ='" + model.area + "'";
                        else
                            sqlwhere += " and area_id ='" + model.area + "'";
                    }
                    if (model.product > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += "ir.ir_id ='" + model.product + "'";
                        else
                            sqlwhere += " and ir.ir_id ='" + model.product + "'";

                    }

                    string sqlwherer = sqlwhere;

                    if (model.stypes != null)
                    {
                        if (model.stypes.Length > 0)
                        {
                            if (sqlwhere == "")
                                sqlwhere += " ( ";
                            else
                                sqlwhere += " and ( ";

                            for (int i = 0; i < model.stypes.Length; i++)
                            {
                                if (i == 0)
                                    sqlwhere += " si_str_id ='" + model.stypes[i] + "' ";
                                else
                                    sqlwhere += " or si_str_id ='" + model.stypes[i] + "' ";
                            }
                            sqlwhere += " ) ";
                        }
                    }

                    if (model.routeId > 0)
                    {
                        if (sqlwhere == "")
                            sqlwhere += " ascs.as_rout_id ='" + model.routeId + "'";
                        else
                            sqlwhere += " and ascs.as_rout_id ='" + model.routeId + "'";
                    }

                    string queryi = "";

                    if (select.Rows.Count > 0)
                    {
                        if (usqlre.get_android_settings("brand_from_inv_barcode"))
                        {
                            if (sqlwhere != "")
                            {
                                queryi = "SELECT " + select.Rows[0]["grc_m_select"].ToString() + " from inv_sales_par sp inner join inv_sales_inf si on sp.sp_entryno=si.si_entryno and sp.sp_str_id = si.si_str_id  inner join acc_subhead ascs  on si.si_acc_id=ascs.as_id  left join inv_item_reg ir on sp.sp_ir_id=ir.ir_id left join inv_category ic on ir.ir_category_id=ic.c_id left join inv_barcode ib on sp.sp_uniquecode=ib.b_uniquecode left join inv_unit iu on sp.sp_unit_multi=iu.u_id left join acc_area on ascs.as_area_id = area_id order by si_entryno desc";
                            }
                            else
                            {
                                queryi = "SELECT " + select.Rows[0]["grc_m_select"].ToString() + " from inv_sales_par sp inner join inv_sales_inf si on sp.sp_entryno=si.si_entryno and sp.sp_str_id = si.si_str_id inner join acc_subhead ascs  on si.si_acc_id=ascs.as_id  left join inv_item_reg ir on sp.sp_ir_id=ir.ir_id left join inv_category ic on ir.ir_category_id=ic.c_id left join inv_barcode ib on sp.sp_uniquecode=ib.b_uniquecode left join inv_unit iu on sp.sp_unit_multi=iu.u_id left join inv_district on as_district_id = dct_id left join acc_area on ascs.as_area_id = area_id where " + sqlwhere + " order by si_entryno desc";
                                if (model.reportAccReg)
                                {
                                    queryi = "SELECT " + select.Rows[0]["grc_m_select"].ToString() + " from inv_sales_par sp inner join inv_sales_inf si on sp.sp_entryno=si.si_entryno and sp.sp_str_id = si.si_str_id inner join acc_subhead ascs  on si.si_acc_id=ascs.as_id  left join inv_item_reg ir on sp.sp_ir_id=ir.ir_id left join inv_category ic on ir.ir_category_id=ic.c_id left join inv_barcode ib on sp.sp_uniquecode=ib.b_uniquecode left join inv_unit iu on sp.sp_unit_multi=iu.u_id left join inv_district on as_district_id = dct_id left join acc_area on ascs.as_area_id = area_id where " + sqlwhere + " and ascs.as_salesman_id = '" + model.salesMan + "' order by si_entryno desc";
                                }

                            }
                        }
                        else
                        {
                            if (sqlwhere == "")
                            {
                                queryi = "SELECT " + select.Rows[0]["grc_m_select"].ToString() + " from inv_sales_par sp inner join inv_sales_inf si on sp.sp_entryno=si.si_entryno and sp.sp_str_id = si.si_str_id  inner join acc_subhead ascs  on si.si_acc_id=ascs.as_id  left join inv_item_reg ir on sp.sp_ir_id=ir.ir_id left join inv_category ic on ir.ir_category_id=ic.c_id left join inv_mfr im on ir.ir_mfr_id=im.m_id left join inv_unit iu on sp.sp_unit_multi=iu.u_id left join acc_area on ascs.as_area_id = area_id order by si_entryno desc";
                            }
                            else
                            {
                                queryi = "SELECT " + select.Rows[0]["grc_m_select"].ToString() + " from inv_sales_par sp inner join inv_sales_inf si on sp.sp_entryno=si.si_entryno and sp.sp_str_id = si.si_str_id inner join acc_subhead ascs  on si.si_acc_id=ascs.as_id  left join inv_item_reg ir on sp.sp_ir_id=ir.ir_id left join inv_category ic on ir.ir_category_id=ic.c_id left join inv_subcategory isc on ir.ir_sub_category_id=isc.sc_id      left join inv_mfr im on ir.ir_mfr_id=im.m_id left join inv_unit iu on sp.sp_unit_multi=iu.u_id left join inv_district on as_district_id = dct_id left join acc_area on ascs.as_area_id = area_id where " + sqlwhere + " order by si_entryno desc";
                                if (model.reportAccReg)
                                {
                                    queryi = "SELECT " + select.Rows[0]["grc_m_select"].ToString() + " from inv_sales_par sp inner join inv_sales_inf si on sp.sp_entryno=si.si_entryno and sp.sp_str_id = si.si_str_id inner join acc_subhead ascs  on si.si_acc_id=ascs.as_id  left join inv_item_reg ir on sp.sp_ir_id=ir.ir_id left join inv_category ic on ir.ir_category_id=ic.c_id left join inv_mfr im on ir.ir_mfr_id=im.m_id left join inv_unit iu on sp.sp_unit_multi=iu.u_id left join inv_district on as_district_id = dct_id left join acc_area on ascs.as_area_id  = area_id where " + sqlwhere + " and ascs.as_salesman_id = '" + model.salesMan + "' order by si_entryno desc";
                                }

                            }
                        }
                    }
                    dt_report = usqlre.dbReaderFill(queryi);
                }

                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
                hash.Add("sales", dt_report);

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

        }

        [HttpPost("last-sales")]
        public async Task<IActionResult> lastSales([FromBody] SaleModel model)
        {

            UserSqlServer usqlre = new UserSqlServer(this);
            String sql = "";

            if (usqlre.get_gnl_settings("ENABLELOCATIONCHANGEINPURCHASE"))
            {
                sql = "select top 50 LTRIM(RTRIM(si_loc_entryno)) as si_loc_entryno,si_entryno as InvoiceNo,si_cust_name as CustomerName,si_grand_total as totalAmount,convert(varchar(11), si_date, 106) as date  from inv_sales_inf where si_str_id = '" + model.sistrId + "' order by si_entryno desc";
            }
            else if (model.entryNo == 0 && usqlre.get_android_settings("ENABLE FORMATTED INVOICENO"))
            {
                sql = @"
        SELECT TOP 100 
            LTRIM(RTRIM(
                CASE 
                WHEN si_str_id = 1  THEN ISNULL(gl_short_1, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_1, '')
                WHEN si_str_id = 2  THEN ISNULL(gl_short_2, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id IN (3, 13) THEN ISNULL(gl_short_3, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 4  THEN ISNULL(gl_short_4, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 5  THEN ISNULL(gl_short_5, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 6  THEN ISNULL(gl_short_6, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_6, '')
                WHEN si_str_id = 7  THEN ISNULL(gl_short_7, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_7, '')
                WHEN si_str_id = 8  THEN ISNULL(gl_short_8, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 9  THEN ISNULL(gl_short_9, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_9, '')
                WHEN si_str_id = 10 THEN ISNULL(gl_short_10, '') + CAST(si_entryno AS NVARCHAR)
                ELSE CAST(si_entryno AS NVARCHAR)
            END
        )) AS si_loc_entryno,
        si_entryno AS InvoiceNo,
        si_cust_name AS CustomerName,
        si_grand_total AS totalAmount,
        CONVERT(VARCHAR(11), si_date, 106) AS date
        FROM inv_sales_inf
        LEFT JOIN gnl_location ON inv_sales_inf.si_location_id = gnl_location.gl_id
        WHERE si_str_id = '" + model.sistrId + @"'
          AND si_location_id = " + Convert.ToInt32(usqlre.locationId) + @"
        ORDER BY si_entryno DESC";

            }
            else if (model.entryNo == 0)
            {
                if (usqlre.get_gnl_settings("USER WISE LAST SALES"))
                {
                    sql = "select top 100 LTRIM(RTRIM(si_loc_entryno)) as si_loc_entryno,si_entryno as InvoiceNo,si_cust_name as CustomerName,si_grand_total as totalAmount,convert(varchar(11), si_date, 106) as date  from inv_sales_inf where si_user_id = '" + usqlre.userId + "'and si_str_id = '" + model.sistrId + "'and si_location_id=" + Convert.ToInt32(usqlre.locationId) + " order by si_entryno desc";
                }
                else
                {
                    sql = "select top 100 LTRIM(RTRIM(si_loc_entryno)) as si_loc_entryno,si_entryno as InvoiceNo,si_cust_name as CustomerName,si_grand_total as totalAmount,convert(varchar(11), si_date, 106) as date  from inv_sales_inf where si_str_id = '" + model.sistrId + "'and si_location_id=" + Convert.ToInt32(usqlre.locationId) + " order by si_entryno desc";

                }
            }
            else
            {
                if (usqlre.get_android_settings("LOCATION ENTRY NO"))
                {
                    sql = "select LTRIM(RTRIM(si_loc_entryno))as si_loc_entryno,si_entryno as InvoiceNo,si_cust_name as CustomerName,si_grand_total as totalAmount,convert(varchar(11), si_date, 106) as date  from inv_sales_inf where si_str_id = '" + model.sistrId + "' AND si_loc_entryno like '%" + model.entryNo + "%' and si_location_id=" + Convert.ToInt32(usqlre.locationId) + " order by si_entryno desc";
                }
                else
                {
                    sql = "select si_entryno as InvoiceNo,si_cust_name as CustomerName,si_grand_total as totalAmount,convert(varchar(11), si_date, 106) as date  from inv_sales_inf where si_str_id = '" + model.sistrId + "' AND si_entryno like '%" + model.entryNo + "%' and si_location_id=" + Convert.ToInt32(usqlre.locationId) + " order by si_entryno desc";
                }
            }
            DataTable ledgers = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(ledgers));

        }

        [HttpPost("get-sales")]
        public async Task<IActionResult> getSales([FromBody] SaleModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                string sql_str = "EXEC	 [dbo].[Sp_AndroidSalesMan] " +
            "@cc_entryno = " + model.entryNo + "," +
            "@cc_str_id = " + model.sistrId + "," +
            "@cc_location_id = " + Convert.ToInt32(usqlre.locationId) + "," +
            "@StatementType = N'get_sales_main'";
                DataSet main_ds = usqlre.dbreadDataset(sql_str);

                DataTable dt_main = main_ds.Tables["Table"];
                DataTable dt_customer = main_ds.Tables["Table1"];
                DataTable dt_salesman = main_ds.Tables["Table2"];
                DataTable dt_cash_acc = main_ds.Tables["Table3"];
                DataTable dt_rate_type = main_ds.Tables["Table4"];

                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
                hash.Add("data", dt_main);
                hash.Add("customer", dt_customer);
                hash.Add("cash_acc", dt_cash_acc);
                hash.Add("sales_man", dt_salesman);
                hash.Add("rate_type", dt_rate_type);

                DataTable dt_loyality = null;
                if (dt_main != null)
                {
                    if (dt_main.Rows.Count > 0)
                    {
                        int lc_id = Convert.ToInt32(dt_main.Rows[0]["si_lc_id"].ToString());
                        if (lc_id > 0)
                        {
                            sql_str = "select lc_mob as mobile,lc_id as accNo,lc_cardno as cardNo,lc_name as name,lc_add1,lc_category,lc_whatsapp,lc_email,lc_dob from acc_loyalty_card where lc_id = " + lc_id + "";
                            dt_loyality = usqlre.dbReaderFill(sql_str);
                        }

                    }
                }

                sql_str = "SELECT sp_id,sp_str_id,sp_entryno,sp_uniquecode,sp_ir_id,ISNULL(cast(sp_rate as nvarchar(100)),'0') sp_rate,ISNULL(cast(sp_realprate as nvarchar(100)),'0') sp_realprate,ISNULL(cast(sp_realrate as nvarchar(100)),'0') sp_realrate,sp_qty,sp_fqty,ISNULL(cast(sp_gross_value as nvarchar(100)),'0') sp_gross_value,sp_disc_per,ISNULL(cast(sp_disc as nvarchar(100)),'0') sp_disc,ISNULL(cast(sp_real_disc as nvarchar(100)),'0') sp_real_disc,ISNULL(cast(sp_net_amount as nvarchar(100)),'0') sp_net_amount,ISNULL(cast(sp_tax as nvarchar(100)),'0') sp_tax,ISNULL(cast(sp_total as nvarchar(100)),'0') sp_total,ISNULL(cast(sp_profit as nvarchar(100)),'0') sp_profit,ISNULL(cast(sp_igst as nvarchar(100)),'0') sp_igst,ISNULL(cast(sp_cgst as nvarchar(100)),'0') sp_cgst,ISNULL(cast(sp_sgst as nvarchar(100)),'0') sp_sgst,ISNULL(cast(sp_mrp as nvarchar(100)),'0') sp_mrp,ISNULL(cast(sp_qty_multi_unit as nvarchar(100)),'0') sp_qty_multi_unit,ISNULL(cast(sp_srate_multiunit as nvarchar(100)),'0') sp_srate_multiunit,ISNULL(cast(sp_kfc as nvarchar(100)),'0') sp_kfc,ISNULL(cast(sp_prate as nvarchar(100)),'0') sp_prate,ISNULL(cast(sp_cost as nvarchar(100)),'0') sp_cost,sp_unit_multi,ir_name,ir_hsn_code,sp_lend_amount,sp_narration1 FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id WHERE sp_entryno=" + model.entryNo + " and sp_str_id=" + model.sistrId + "";
                DataTable products = usqlre.dbReaderFill(sql_str);

                string sql_lend = @"select ir_id,li_in,li_out from inv_lend_item_transactions 
                            left join inv_item_reg on ir_id = li_ir_id
                            left join inv_sales_type_reg on str_name = li_form
                            where li_entryno = " + model.entryNo + " and str_id=" + model.sistrId + "";
                DataTable lenditems = usqlre.dbReaderFill(sql_lend);

                //DataTable dt_entryno = new DataTable();
                //dt_entryno.Clear();
                //dt_entryno.Columns.Add("entryno");
                //DataRow dt_entryno_row = dt_entryno.NewRow();
                //dt_entryno_row["entryno"] = model.entryNo;
                //dt_entryno.Rows.Add(dt_entryno_row);
                //Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
                //hash.Add("status", dt_status);
                //hash.Add("main", main);
                //hash.Add("products", products);
                //hash.Add("entryno", dt_entryno);
                string resp = "{ \"status\" : true " + ",\"product\":" + ReportModelContext.searializeDt(products) + ",\"main\":" + ReportModelContext.searializeDt(hash) + "," + "  \"entryno\" :" + model.entryNo + ",\"loyality\":" + ReportModelContext.searializeDt(dt_loyality) + ",\"lenditems\":" + ReportModelContext.searializeDt(lenditems) + "}";
                return Ok(resp);
            }
            catch (Exception e)
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }
            //DataTable dt_status = new DataTable();
            //dt_status.Clear();
            //dt_status.Columns.Add("status");
            //DataRow dt_status_row = dt_status.NewRow();
            //dt_status_row["status"] = true;
            //dt_status.Rows.Add(dt_status_row);

        }

        [HttpGet("sales-types")]
        public string allSalesType()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT * from inv_sales_type_reg";
            DataTable allSalesType = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(allSalesType);
        }
        // corrected in sp of [BMI_AGENCIES_2526]
        [HttpPost("lend-collection")]
        public async Task<IActionResult> lendcollection([FromBody] Complaints model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            int tres = -1;
            try
            {
                if (usqlre.OpenConnection())
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_inv_lend_item_transactions";
                    cmd.Connection = usqlre.shop;
                    cmd.CommandTimeout = 0;

                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm);

                    cmd.Parameters.AddWithValue("@li_date", model.li_date);
                    cmd.Parameters.AddWithValue("@li_in", model.li_in);
                    cmd.Parameters.AddWithValue("@li_out", model.li_out);
                    cmd.Parameters.AddWithValue("@li_as_id", model.li_as_id);
                    cmd.Parameters.AddWithValue("@li_ir_id", model.complaintId);
                    cmd.Parameters.AddWithValue("@StatementType", "Insert");

                    cmd.ExecuteNonQuery(); 
                    tres = Convert.ToInt32(cmd.Parameters["@return"].Value);

                    if (tres > 1)
                    {
                        usqlre.savecheckin(model.li_date, model.li_as_id);
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }

            return Ok(new { status = tres > 0 });
        }
        
        //[HttpPost("lend-collection")]
        //public async Task<IActionResult> lendcollection([FromBody] Complaints model)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);
        //    int tres = -1;
        //    try
        //    {
        //        if (usqlre.OpenConnection())
        //        {
        //            SqlCommand cmd = new SqlCommand();
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.CommandText = "Sp_inv_lend_item_transactions";
        //            SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
        //            parm.Direction = ParameterDirection.Output;
        //            cmd.Parameters.Add(parm);
        //            cmd.Parameters.AddWithValue("@li_date", model.li_date);
        //            cmd.Parameters.AddWithValue("@li_in", model.li_in);
        //            cmd.Parameters.AddWithValue("@li_out", model.li_out);
        //            cmd.Parameters.AddWithValue("@li_as_id", model.li_as_id);
        //            cmd.Parameters.AddWithValue("@li_ir_id", model.complaintId);
        //            cmd.Parameters.AddWithValue("@StatementType", "Insert");
        //            cmd.CommandTimeout = 0;
        //            cmd.Connection = usqlre.shop;
        //            SqlDataReader dr = cmd.ExecuteReader();
        //            dr.Dispose();
        //            dr.Close();
        //            tres = Convert.ToInt32(parm.Value);
        //            if (tres > 1)
        //            {
        //                usqlre.savecheckin(model.li_date, model.li_as_id);
        //            }
        //        }
        //    }
        //    catch (Exception ex) { }

        //    return Ok(new { status = tres > 0 ? true : false });
        //}
        [HttpPost("sales-order-status")]
        public async Task<IActionResult> salesorderstatus([FromBody] SaleReportModel model)
        {
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql_str = "";
            DataTable dt_decimal = usqlre.dbReaderFill("select gs_value from gnl_settings where gs_name='DECIMAL'");
            int _decimal = Convert.ToInt32(dt_decimal.Rows[0][0].ToString());
            usqlre.close();
            sql_str = "EXEC	 [dbo].[Sp_Report_Sales] " +
                      "@status =" + model.status + "," +
                      "@from_date = '" + model.fromDate + "'," +
                      "@StatementType = 'sales_order_status_report'";
            DataSet main_ds = usqlre.dbreadDataset(sql_str);

            List<object> main = new List<object>();
            List<object> maindata = new List<object>();
            Dictionary<string, object> detailed = new Dictionary<string, object>();
            Dictionary<string, object> summary = new Dictionary<string, object>();
            Dictionary<string, object> detaileddata = new Dictionary<string, object>();
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            Dictionary<string, object> rowss;
            Dictionary<string, object> rowsc;

            List<object> rowsin;
            Dictionary<string, object> rowsinDic = new Dictionary<string, object>();
            rowsc = new Dictionary<string, object>();
            DataTable dt_salesman = new DataTable();
            dt_salesman.Columns.Add("name");
            for (int i = main_ds.Tables[1].Rows.Count - 1; i >= 0; i--)
            {
                if (main_ds.Tables[1].Rows[i]["salesman"] == DBNull.Value)
                    main_ds.Tables[1].Rows[i].Delete();
                main_ds.Tables[1].Rows[i]["grandTotal"] = System.Math.Round((CommonHelper.GetTextboxValue(main_ds.Tables[1].Rows[i]["grandTotal"].ToString())), _decimal).ToString();
            }
            main_ds.Tables[1].AcceptChanges();

            foreach (DataRow dr in main_ds.Tables[1].Rows)
            {
                string cat = dr["salesman"].ToString();

                var filter = (from n in main_ds.Tables[1].AsEnumerable()
                              where n.Field<string>("salesman").Contains(cat)
                              select n).ToList();

                DataRow[] foundsalesman = dt_salesman.Select("name = '" + cat + "'");
                if (foundsalesman.Length == 0)
                {
                    if (filter.Count != 0)
                    {
                        DataTable t = filter.CopyToDataTable();
                        int count = filter.Count;
                        int pending = t.Select("status = 'PENDING'").Length;
                        int accepted = t.Select("status = 'ACCEPTED'").Length;
                        int completed = t.Select("status = 'COMPLETED'").Length;

                        // int pending_status = accepted + pending;
                        t.Columns.Remove("salesman");
                        rowss = new Dictionary<string, object>();

                        string name = cat == "empty" ? "" : cat;
                        rowsin = new List<object>();
                        rowsinDic.Add("Name", name);
                        rowsinDic.Add("Count", count);
                        rowsinDic.Add("Pending", pending);
                        rowsinDic.Add("Accepted", accepted);
                        rowsinDic.Add("Completed", completed);

                        int i = 0;
                        decimal grandTotal = 0;
                        foreach (DataRow drr in t.Rows)
                        {
                            rowss.Add("Slno", ++i);
                            foreach (DataColumn col in t.Columns)
                            {
                                rowss.Add(col.ColumnName, drr[col]);

                            }
                            rowsin.Add(rowss);
                            grandTotal = grandTotal + Convert.ToDecimal(drr["grandTotal"]);
                            grandTotal = System.Math.Round((CommonHelper.GetTextboxValue(grandTotal.ToString())), _decimal);
                            rowss = new Dictionary<string, object>();


                        }
                        try
                        {
                            rowsinDic.Add("Total", grandTotal);
                            rowsinDic.Add("Sales", rowsin);
                            //rowsc.Add(cat, rowsinDic);
                            maindata.Add(rowsinDic);
                            rowsinDic = new Dictionary<string, object>();

                            t.Dispose();
                            t = null;
                            filter = null;
                        }
                        catch (Exception ex)
                        {
                            t.Dispose();
                            t = null;
                            filter = null;
                        }


                    }
                    dt_salesman.Rows.Add(cat);
                }


            }
            //rows.Add(rowsc);
            detailed.Add("summary", main_ds.Tables[0]);
            detailed.Add("Detailed", maindata);
            string json = JsonConvert.SerializeObject(detailed, Newtonsoft.Json.Formatting.Indented);
            if (json.Length > 2)
            {
                json = json.Substring(1, json.Length - 2);
                json = "{" + json + "}";
            }

            usqlre.close();
            return Ok(json);
        }


        //FOR API INTEGRATION
        [HttpPost("report-sale")]
        public async Task<IActionResult> salesReport([FromBody] SalesReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";

            String query = "EXEC [dbo].[Sp_report_external_api] @from_date = '" + model.fromDate + "',@to_date = '" + model.toDate + "' " + ",@StatementType = '" + model.reportType + "'";
            DataTable ds = usqlre.dbReaderFill(query);
            usqlre.close();
            hash.Add(model.reportType, ds);
            return Ok(ReportModelContext.searializeDt(hash));
        }

        [HttpGet("invoice-data/{entryNo}/{strId}")]
        public string invoice_data(int entryNo, int strId)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";
            string columnCheckQuery = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'gnl_company' AND COLUMN_NAME = 'com_crno'";
            DataTable dtColumnCheck = usqlre.dbReaderFill(columnCheckQuery);
            string columnCheckQuery1 = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'acc_subhead' AND COLUMN_NAME = 'as_crno'";
            DataTable dtColumnCheck1 = usqlre.dbReaderFill(columnCheckQuery1);
            string loyaltyVehicleNo = "";
            if (usqlre.get_android_settings("ENABLE VEHICLE NUMBER FIELD IN LOYALTY"))
            {
                loyaltyVehicleNo = ", isnull(lc_cardno,'') as loyaltyVehicleNo";
            }
            if (usqlre.get_android_settings("ENABLE LOCATIONWISE COMPANY ADDRESS") || usqlre.get_android_settings("ENABLE LOCATION IN SALES"))
            {
                sql = @"
	                select si_location_id AS locationId from inv_sales_inf 
	                left join gnl_location on gl_id= si_location_id
	                where si_str_id=" + strId + " and si_entryno=" + entryNo + "";
                DataTable dt0 = usqlre.dbReaderFill(sql);
                if (dt0.Rows.Count > 0)
                {
                    if (dtColumnCheck.Rows.Count > 0)
                    {
                        sql = @"SELECT com.com_name AS name,loc.gl_add1 AS address1, loc.gl_add2 AS address2, loc.gl_add3 AS address3,com.com_mob AS mobilenumber,
                com.com_gstin AS GSTIN,com.com_crno AS CrNo,com.com_email AS mail,com.com_state AS state,com.com_state_code AS statecode,com.com_bankac AS bankname,
                com.com_bank_accno AS bankaccno,com.com_bank_ifsc AS ifsc,com.com_ar_name,com.com_ar_add1,com.com_ar_add2,com.com_ar_add3,
                com.com_ar_mob,com.com_ar_vat FROM gnl_company com CROSS JOIN inv_sales_inf si INNER JOIN gnl_location loc ON loc.gl_id = si.si_location_id
                where si.si_str_id=" + strId + " and si.si_entryno=" + entryNo + "";
                    }
                    else
                    {
                        sql = @"SELECT com.com_name AS name,loc.gl_add1 AS address1, loc.gl_add2 AS address2, loc.gl_add3 AS address3,com.com_mob AS mobilenumber,
                com.com_gstin AS GSTIN,com.com_email AS mail,com.com_state AS state,com.com_state_code AS statecode,com.com_bankac AS bankname,
                com.com_bank_accno AS bankaccno,com.com_bank_ifsc AS ifsc,com.com_ar_name,com.com_ar_add1,com.com_ar_add2,com.com_ar_add3,
                com.com_ar_mob,com.com_ar_vat FROM gnl_company com CROSS JOIN inv_sales_inf si INNER JOIN gnl_location loc ON loc.gl_id = si.si_location_id
                where si.si_str_id=" + strId + " and si.si_entryno=" + entryNo + "";
                        
                    }
                    DataTable dt1 = usqlre.dbReaderFill(sql);
                    hash.Add("company", dt1);
                }
                else
                {
                    if (dtColumnCheck.Rows.Count > 0)
                    {
                        sql = @"select com_name as name,com_add1 as address1,com_add2 as address2,com_add3 as address3,com_add4 as address4,com_add5 as address5,
                   com_mob as mobilenumber,com_gstin  as GSTIN,com_crno AS CrNo,com_email as mail,com_state as state,com_state_code as statecode,
                   com_bankac as bankname,com_bank_accno as bankaccno,com_bank_ifsc as ifsc,
                   com_ar_name,com_ar_add1,com_ar_add2,com_ar_add3,com_ar_mob,com_ar_vat
                   from gnl_company";
                    }
                    else
                    {
                        sql = @"select com_name as name,com_add1 as address1,com_add2 as address2,com_add3 as address3,com_add4 as address4,com_add5 as address5,
                   com_mob as mobilenumber,com_gstin  as GSTIN,com_email as mail,com_state as state,com_state_code as statecode,
                   com_bankac as bankname,com_bank_accno as bankaccno,com_bank_ifsc as ifsc,
                   com_ar_name,com_ar_add1,com_ar_add2,com_ar_add3,com_ar_mob,com_ar_vat
                   from gnl_company";
                    }
                    DataTable dt2 = usqlre.dbReaderFill(sql);
                    hash.Add("company", dt2);
                }
            }
            else
            {
                if (dtColumnCheck.Rows.Count > 0)
                {
                    sql = @"select com_name as name,com_add1 as address1,com_add2 as address2,com_add3 as address3,com_add4 as address4,com_add5 as address5,
                   com_mob as mobilenumber,com_gstin  as GSTIN,com_crno AS CrNo,com_email as mail,com_state as state,com_state_code as statecode,
                   com_bankac as bankname,com_bank_accno as bankaccno,com_bank_ifsc as ifsc,
                   com_ar_name,com_ar_add1,com_ar_add2,com_ar_add3,com_ar_mob,com_ar_vat
                   from gnl_company";
                }
                else
                {
                    sql = @"select com_name as name,com_add1 as address1,com_add2 as address2,com_add3 as address3,com_add4 as address4,com_add5 as address5,
                   com_mob as mobilenumber,com_gstin  as GSTIN,com_email as mail,com_state as state,com_state_code as statecode,
                   com_bankac as bankname,com_bank_accno as bankaccno,com_bank_ifsc as ifsc,
                   com_ar_name,com_ar_add1,com_ar_add2,com_ar_add3,com_ar_mob,com_ar_vat
                   from gnl_company";
                }
                
                DataTable dt2 = usqlre.dbReaderFill(sql);
                hash.Add("company", dt2);
            }
            sql = @"
	                select si_ship_name,si_ship_location,si_ship_add1,si_ship_pincode,si_ship_scode,si_ship_gstin from inv_sales_inf 
	                where si_str_id=" + strId + " and si_entryno=" + entryNo + "";
            DataTable dt = usqlre.dbReaderFill(sql);
            if(usqlre.get_android_settings("ENABLE FORMATTED INVOICENO"))
            {
                if (dt.Rows.Count > 0 && (!string.IsNullOrEmpty(dt.Rows[0]["si_ship_name"].ToString()) || !string.IsNullOrEmpty(dt.Rows[0]["si_ship_location"].ToString()) || !string.IsNullOrEmpty(dt.Rows[0]["si_ship_add1"].ToString()) || !string.IsNullOrEmpty(dt.Rows[0]["si_ship_pincode"].ToString()) || !string.IsNullOrEmpty(dt.Rows[0]["si_ship_scode"].ToString()) || !string.IsNullOrEmpty(dt.Rows[0]["si_ship_gstin"].ToString())))
                {
                    if (dtColumnCheck1.Rows.Count > 0)
                    {
                        sql = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],
                    right(convert(varchar(20),si_date,100),7) [time],a.as_name as custName,a.as_add1 as add1,
                    a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,a.as_state as state,a.as_state_code as stateCode,
                    si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST,si_igst_total as IGST,si_tax as GST, si_total as Total,
                    si_other_charge as otherCharge,si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,
                    si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_cash_recieved as cashReceived,
                    CASE 
                        WHEN si_str_id = 1  THEN ISNULL(gl_short_1, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_1, '')
                        WHEN si_str_id = 2  THEN ISNULL(gl_short_2, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id IN (3, 13) THEN ISNULL(gl_short_3, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 4  THEN ISNULL(gl_short_4, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 5  THEN ISNULL(gl_short_5, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 6  THEN ISNULL(gl_short_6, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_6, '')
                        WHEN si_str_id = 7  THEN ISNULL(gl_short_7, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_7, '')
                        WHEN si_str_id = 8  THEN ISNULL(gl_short_8, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 9  THEN ISNULL(gl_short_9, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_9, '')
                        WHEN si_str_id = 10 THEN ISNULL(gl_short_10, '') + CAST(si_entryno AS NVARCHAR)
                        ELSE CAST(si_entryno AS NVARCHAR)
                    END AS si_loc_entryno,
                    a.as_tin as gstin,a.as_crno as CrNo,s.as_name as salesman,si_einvoice_ksa,s.as_whatsapp as salesman_mob,si_einvoice_ksa,case when si_cash_recieved >0 or si_upi_amount>0 or si_card_amount>0 then 'Cash' else 'Credit'end as modeOfPayment,
                    si_ship_name,si_ship_location,si_ship_add1,si_ship_pincode,si_ship_scode,si_ship_gstin,isnull(lc_name,'') as loyaltyName ,isnull(lc_mob,'') as loyaltyMob, isnull(lc_add1,'') as loyaltyAdd, isnull(lc_whatsapp,'') as loyaltyWhatsapp" + loyaltyVehicleNo + @"  from inv_sales_inf 
                    left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_commision_acc_id=s.as_id
                    LEFT JOIN gnl_location ON si_location_id = gl_id
                    left join acc_loyalty_card on si_lc_id= lc_id
                    where si_str_id=" + strId + " and si_entryno=" + entryNo + "";
                    }
                    else
                    {
                        sql = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],
                    right(convert(varchar(20),si_date,100),7) [time],a.as_name as custName,a.as_add1 as add1,
                    a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,a.as_state as state,a.as_state_code as stateCode,
                    si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST,si_igst_total as IGST,si_tax as GST, si_total as Total,
                    si_other_charge as otherCharge,si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,
                    si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_cash_recieved as cashReceived,
                    CASE 
                        WHEN si_str_id = 1  THEN ISNULL(gl_short_1, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_1, '')
                        WHEN si_str_id = 2  THEN ISNULL(gl_short_2, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id IN (3, 13) THEN ISNULL(gl_short_3, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 4  THEN ISNULL(gl_short_4, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 5  THEN ISNULL(gl_short_5, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 6  THEN ISNULL(gl_short_6, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_6, '')
                        WHEN si_str_id = 7  THEN ISNULL(gl_short_7, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_7, '')
                        WHEN si_str_id = 8  THEN ISNULL(gl_short_8, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 9  THEN ISNULL(gl_short_9, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_9, '')
                        WHEN si_str_id = 10 THEN ISNULL(gl_short_10, '') + CAST(si_entryno AS NVARCHAR)
                        ELSE CAST(si_entryno AS NVARCHAR)
                    END AS si_loc_entryno,
                    a.as_tin as gstin,s.as_name as salesman,si_einvoice_ksa,s.as_whatsapp as salesman_mob,si_einvoice_ksa,case when si_cash_recieved >0 or si_upi_amount>0 or si_card_amount>0 then 'Cash' else 'Credit'end as modeOfPayment,
                    si_ship_name,si_ship_location,si_ship_add1,si_ship_pincode,si_ship_scode,si_ship_gstin ,isnull(lc_name,'') as loyaltyName ,isnull(lc_mob,'') as loyaltyMob, isnull(lc_add1,'') as loyaltyAdd, isnull(lc_whatsapp,'') as loyaltyWhatsapp" + loyaltyVehicleNo + @"  from inv_sales_inf 
                    left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_commision_acc_id=s.as_id
                    LEFT JOIN gnl_location ON si_location_id = gl_id
                    left join acc_loyalty_card on si_lc_id= lc_id
                    where si_str_id=" + strId + " and si_entryno=" + entryNo + "";
                    }

                }
                else
                {
                    if (dtColumnCheck1.Rows.Count > 0)
                    {
                        sql = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],
                 right(convert(varchar(20),si_date,100),7) [time],a.as_name as custName,a.as_add1 as add1,
                  a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,a.as_state as state,a.as_state_code as stateCode,
                 si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST,si_igst_total as IGST,si_tax as GST, si_total as Total,
                 si_other_charge as otherCharge,si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,
                 si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_cash_recieved as cashReceived,
                 CASE 
                        WHEN si_str_id = 1  THEN ISNULL(gl_short_1, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_1, '')
                        WHEN si_str_id = 2  THEN ISNULL(gl_short_2, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id IN (3, 13) THEN ISNULL(gl_short_3, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 4  THEN ISNULL(gl_short_4, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 5  THEN ISNULL(gl_short_5, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 6  THEN ISNULL(gl_short_6, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_6, '')
                        WHEN si_str_id = 7  THEN ISNULL(gl_short_7, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_7, '')
                        WHEN si_str_id = 8  THEN ISNULL(gl_short_8, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 9  THEN ISNULL(gl_short_9, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_9, '')
                        WHEN si_str_id = 10 THEN ISNULL(gl_short_10, '') + CAST(si_entryno AS NVARCHAR)
                        ELSE CAST(si_entryno AS NVARCHAR)
                    END AS si_loc_entryno,
                 a.as_tin as gstin,a.as_crno as CrNo,s.as_name as salesman,si_einvoice_ksa,s.as_whatsapp as salesman_mob,si_einvoice_ksa,case when si_cash_recieved >0 or si_upi_amount>0 or si_card_amount>0 then 'Cash' else 'Credit'end as modeOfPayment,
		         a.as_name as si_ship_name,a.as_location as si_ship_location,a.as_add1 as si_ship_add1,a.as_pincode as si_ship_pincode,si_ship_scode,a.as_tin as si_ship_gstin ,isnull(lc_name,'') as loyaltyName ,isnull(lc_mob,'') as loyaltyMob, isnull(lc_add1,'') as loyaltyAdd, isnull(lc_whatsapp,'') as loyaltyWhatsapp" + loyaltyVehicleNo + @" from inv_sales_inf 
                 left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_commision_acc_id=s.as_id
                 LEFT JOIN gnl_location ON si_location_id = gl_id
                left join acc_loyalty_card on si_lc_id= lc_id
                where si_str_id=" + strId + " and si_entryno=" + entryNo + "";
                    }
                    else
                    {
                        sql = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],
                 right(convert(varchar(20),si_date,100),7) [time],a.as_name as custName,a.as_add1 as add1,
                  a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,a.as_state as state,a.as_state_code as stateCode,
                 si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST,si_igst_total as IGST,si_tax as GST, si_total as Total,
                 si_other_charge as otherCharge,si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,
                 si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_cash_recieved as cashReceived,
                 CASE 
                        WHEN si_str_id = 1  THEN ISNULL(gl_short_1, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_1, '')
                        WHEN si_str_id = 2  THEN ISNULL(gl_short_2, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id IN (3, 13) THEN ISNULL(gl_short_3, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 4  THEN ISNULL(gl_short_4, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 5  THEN ISNULL(gl_short_5, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 6  THEN ISNULL(gl_short_6, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_6, '')
                        WHEN si_str_id = 7  THEN ISNULL(gl_short_7, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_7, '')
                        WHEN si_str_id = 8  THEN ISNULL(gl_short_8, '') + CAST(si_entryno AS NVARCHAR)
                        WHEN si_str_id = 9  THEN ISNULL(gl_short_9, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_9, '')
                        WHEN si_str_id = 10 THEN ISNULL(gl_short_10, '') + CAST(si_entryno AS NVARCHAR)
                        ELSE CAST(si_entryno AS NVARCHAR)
                END AS si_loc_entryno,
                a.as_tin as gstin,s.as_name as salesman,si_einvoice_ksa,s.as_whatsapp as salesman_mob,si_einvoice_ksa,case when si_cash_recieved >0 or si_upi_amount>0 or si_card_amount>0 then 'Cash' else 'Credit'end as modeOfPayment,
		         a.as_name as si_ship_name,a.as_location as si_ship_location,a.as_add1 as si_ship_add1,a.as_pincode as si_ship_pincode,si_ship_scode,a.as_tin as si_ship_gstin ,isnull(lc_name,'') as loyaltyName ,isnull(lc_mob,'') as loyaltyMob, isnull(lc_add1,'') as loyaltyAdd, isnull(lc_whatsapp,'') as loyaltyWhatsapp" + loyaltyVehicleNo + @"  from inv_sales_inf 
                 left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_commision_acc_id=s.as_id
                 LEFT JOIN gnl_location ON si_location_id = gl_id   
                left join acc_loyalty_card on si_lc_id= lc_id               
                where si_str_id=" + strId + " and si_entryno=" + entryNo + "";
                    }

                }
            }
            else
            {
                if (dt.Rows.Count > 0 && (!string.IsNullOrEmpty(dt.Rows[0]["si_ship_name"].ToString()) || !string.IsNullOrEmpty(dt.Rows[0]["si_ship_location"].ToString()) || !string.IsNullOrEmpty(dt.Rows[0]["si_ship_add1"].ToString()) || !string.IsNullOrEmpty(dt.Rows[0]["si_ship_pincode"].ToString()) || !string.IsNullOrEmpty(dt.Rows[0]["si_ship_scode"].ToString()) || !string.IsNullOrEmpty(dt.Rows[0]["si_ship_gstin"].ToString())))
                {
                    if (dtColumnCheck1.Rows.Count > 0)
                    {
                        sql = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],
                    right(convert(varchar(20),si_date,100),7) [time],a.as_name as custName,a.as_add1 as add1,
                    a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,a.as_state as state,a.as_state_code as stateCode,
                    si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST,si_igst_total as IGST,si_tax as GST, si_total as Total,
                    si_other_charge as otherCharge,si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,
                    si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_cash_recieved as cashReceived,
                    si_loc_entryno,a.as_tin as gstin,a.as_crno as CrNo,s.as_name as salesman,si_einvoice_ksa,s.as_whatsapp as salesman_mob,si_einvoice_ksa,case when si_cash_recieved >0 or si_upi_amount>0 or si_card_amount>0 then 'Cash' else 'Credit'end as modeOfPayment,
                    si_ship_name,si_ship_location,si_ship_add1,si_ship_pincode,si_ship_scode,si_ship_gstin ,isnull(lc_name,'') as loyaltyName ,isnull(lc_mob,'') as loyaltyMob, isnull(lc_add1,'') as loyaltyAdd, isnull(lc_whatsapp,'') as loyaltyWhatsapp" + loyaltyVehicleNo + @" from inv_sales_inf 
                    left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_commision_acc_id=s.as_id
                    left join acc_loyalty_card on si_lc_id= lc_id
                    where si_str_id=" + strId + " and si_entryno=" + entryNo + "";
                    }
                    else
                    {
                        sql = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],
                    right(convert(varchar(20),si_date,100),7) [time],a.as_name as custName,a.as_add1 as add1,
                    a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,a.as_state as state,a.as_state_code as stateCode,
                    si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST,si_igst_total as IGST,si_tax as GST, si_total as Total,
                    si_other_charge as otherCharge,si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,
                    si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_cash_recieved as cashReceived,
                    si_loc_entryno,a.as_tin as gstin,s.as_name as salesman,si_einvoice_ksa,s.as_whatsapp as salesman_mob,si_einvoice_ksa,case when si_cash_recieved >0 or si_upi_amount>0 or si_card_amount>0 then 'Cash' else 'Credit'end as modeOfPayment,
                    si_ship_name,si_ship_location,si_ship_add1,si_ship_pincode,si_ship_scode,si_ship_gstin ,isnull(lc_name,'') as loyaltyName ,isnull(lc_mob,'') as loyaltyMob, isnull(lc_add1,'') as loyaltyAdd, isnull(lc_whatsapp,'') as loyaltyWhatsapp" + loyaltyVehicleNo + @" from inv_sales_inf 
                    left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_commision_acc_id=s.as_id
                    left join acc_loyalty_card on si_lc_id= lc_id
                    where si_str_id=" + strId + " and si_entryno=" + entryNo + "";
                    }

                }
                else
                {
                    if (dtColumnCheck1.Rows.Count > 0)
                    {
                        sql = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],
                 right(convert(varchar(20),si_date,100),7) [time],a.as_name as custName,a.as_add1 as add1,
                  a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,a.as_state as state,a.as_state_code as stateCode,
                 si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST,si_igst_total as IGST,si_tax as GST, si_total as Total,
                 si_other_charge as otherCharge,si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,
                 si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_cash_recieved as cashReceived,
                 si_loc_entryno,a.as_tin as gstin,a.as_crno as CrNo,s.as_name as salesman,si_einvoice_ksa,s.as_whatsapp as salesman_mob,si_einvoice_ksa,case when si_cash_recieved >0 or si_upi_amount>0 or si_card_amount>0 then 'Cash' else 'Credit'end as modeOfPayment,
		         a.as_name as si_ship_name,a.as_location as si_ship_location,a.as_add1 as si_ship_add1,a.as_pincode as si_ship_pincode,si_ship_scode,a.as_tin as si_ship_gstin ,isnull(lc_name,'') as loyaltyName ,isnull(lc_mob,'') as loyaltyMob, isnull(lc_add1,'') as loyaltyAdd, isnull(lc_whatsapp,'') as loyaltyWhatsapp" + loyaltyVehicleNo + @" from inv_sales_inf 
                 left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_commision_acc_id=s.as_id
                 left join acc_loyalty_card on si_lc_id= lc_id
                where si_str_id=" + strId + " and si_entryno=" + entryNo + "";
                    }
                    else
                    {
                        sql = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],
                 right(convert(varchar(20),si_date,100),7) [time],a.as_name as custName,a.as_add1 as add1,
                  a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,a.as_state as state,a.as_state_code as stateCode,
                 si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST,si_igst_total as IGST,si_tax as GST, si_total as Total,
                 si_other_charge as otherCharge,si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,
                 si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_cash_recieved as cashReceived,
                 si_loc_entryno,a.as_tin as gstin,s.as_name as salesman,si_einvoice_ksa,s.as_whatsapp as salesman_mob,si_einvoice_ksa,case when si_cash_recieved >0 or si_upi_amount>0 or si_card_amount>0 then 'Cash' else 'Credit'end as modeOfPayment,
		         a.as_name as si_ship_name,a.as_location as si_ship_location,a.as_add1 as si_ship_add1,a.as_pincode as si_ship_pincode,si_ship_scode,a.as_tin as si_ship_gstin ,isnull(lc_name,'') as loyaltyName ,isnull(lc_mob,'') as loyaltyMob, isnull(lc_add1,'') as loyaltyAdd, isnull(lc_whatsapp,'') as loyaltyWhatsapp" + loyaltyVehicleNo + @"  from inv_sales_inf 
                 left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_commision_acc_id=s.as_id
                 left join acc_loyalty_card on si_lc_id= lc_id
                where si_str_id=" + strId + " and si_entryno=" + entryNo + "";
                    }

                }
            }           

            DataTable dt3 = usqlre.dbReaderFill(sql);
            hash.Add("salesInf", dt3);

            sql = @"select ir_name as description,ir_hsn_code as hsnCode,sp_qty as qty,sp_rate as rate,
                    sp_gross_value as gross,sp_net_amount as netAmt,sp_mrp as mrp,sp_disc as discount,ir_cgst as cgstP,
                    sp_cgst as cgst,ir_sgst as sgstP,sp_sgst as sgst,ir_igst as igstP,sp_igst as igst,
                    ir_taxper as TaxP,sp_tax as GST,sp_total as total,sp.u_name as unit,u.u_name as uom,sp_narration1,sp_Qty_multi_unit as MQty,sp_srate_multiunit as BSrate
                    FROM inv_sales_par
                    left JOIN inv_item_reg on inv_sales_par.sp_ir_id = inv_item_reg.ir_id
                    left join inv_unit as sp on sp_unit_multi = sp.u_id
                    left join inv_unit as u on ir_min_unit_id = u.u_id WHERE sp_entryno=" + entryNo + " and sp_str_id=" + strId + "";
            DataTable dt4 = usqlre.dbReaderFill(sql);
            hash.Add("ItemDetails", dt4);

            string fetchDetails = "SELECT si_acc_id, si_date FROM inv_sales_inf WHERE si_str_id = " + strId + " AND si_entryno = " + entryNo;
            DataTable dtDetails = usqlre.dbReaderFill(fetchDetails);

            if (dtDetails.Rows.Count > 0)
            {
                int accId = Convert.ToInt32(dtDetails.Rows[0]["si_acc_id"]);
                DateTime date = Convert.ToDateTime(dtDetails.Rows[0]["si_date"]);
                string formattedDate = date.ToString("yyyy-MM-dd");

                if (usqlre.get_android_settings("SHOW STATEMENT REPORT SCREENSHOT"))
                {
                     sql = $@"
        SELECT 
            ISNULL(SUM(CASE WHEN li_date < '{formattedDate}' THEN li_out - li_in END), 0) AS [OpeningTray],
            ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '{formattedDate}' THEN li_out END), 0) AS Issue,
            ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '{formattedDate}' THEN li_in END), 0) AS [Return],  
            (
                ISNULL(SUM(CASE WHEN li_date < '{formattedDate}' THEN li_out - li_in END), 0) +
                ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '{formattedDate}' THEN li_out END), 0) -
                ISNULL(SUM(CASE WHEN CAST(li_date AS DATE) = '{formattedDate}' THEN li_in END), 0)
            ) AS [TrayBalance]
        FROM inv_lend_item_transactions
        WHERE li_as_id = {accId}";

                    DataTable dt5 = usqlre.dbReaderFill(sql);
                    hash.Add("BulkTrayDetails", dt5);
                }
                sql = $@"SELECT SUM(at_Cr) AS ReceivedAmount FROM acc_account_transactions
            WHERE at_as_id = {accId} AND CAST(at_date AS DATE) = '{formattedDate}'";
                DataTable dt6 = usqlre.dbReaderFill(sql);
                hash.Add("ReceivedAmountInToday", dt6);
            }
            return ReportModelContext.searializeDt(hash);
        }

        [HttpPost("search-sales-multyUnit")]
        public async Task<IActionResult> saveSalesMulty([FromBody] EntrySearchModel model)
        {
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            UserSqlServer usqlre = new UserSqlServer(this);
            DataTable dt = new DataTable();
            string query = "";
            //query = "SELECT si_entryno, si_acc_id, si_cust_name, si_salesman_id, si_location_id, si_remarks, ISNULL(cast(si_gross_value as nvarchar(100)), '0') si_gross_value,ISNULL(cast(si_disc as nvarchar(100)), '0') si_disc,ISNULL(cast(si_disc_per as nvarchar(100)), '0') si_disc_per,ISNULL(cast(si_net_amount as nvarchar(100)), '0') si_net_amount,ISNULL(cast(si_tax as nvarchar(100)), '0') si_tax,ISNULL(cast(si_total as nvarchar(100)), '0') im_rate,ISNULL(cast(si_loading_charge as nvarchar(100)), '0') im_rate,ISNULL(cast(si_other_charge as nvarchar(100)), '0') si_other_charge,ISNULL(cast(si_other_disc as nvarchar(100)), '0') si_other_disc,ISNULL(cast(si_profit as nvarchar(100)), '0') si_profit,ISNULL(cast(si_cash_recieved as nvarchar(100)), '0') si_cash_recieved,ISNULL(cast(si_balance as nvarchar(100)), '0') si_balance,ISNULL(cast(si_grand_total as nvarchar(100)), '0') si_grand_total,ISNULL(cast(si_igst_total as nvarchar(100)), '0') si_igst_total,ISNULL(cast(si_cgst_total as nvarchar(100)), '0') si_cgst_total,ISNULL(cast(si_sgst_total as nvarchar(100)), '0') si_sgst_total,ISNULL(cast(si_ob as nvarchar(100)), '0') si_ob,ISNULL(cast(si_net_balance as nvarchar(100)), '0') si_net_balance,ISNULL(cast(si_freight_charge as nvarchar(100)), '0') si_freight_charge,ISNULL(cast(si_sum_kfc as nvarchar(100)), '0') si_sum_kfc,ISNULL(cast(si_total as nvarchar(100)), '0') si_total,si_cash_paid_acc,si_date FROM inv_sales_inf WHERE si_entryno =" + model.entryNo + "";
            if (usqlre.get_android_settings("ENABLE FORMATTED INVOICENO"))
            {
                query += @"SELECT si_entryno,CASE 
                WHEN si_str_id = 1  THEN ISNULL(gl_short_1, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_1, '')
                WHEN si_str_id = 2  THEN ISNULL(gl_short_2, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id IN (3, 13) THEN ISNULL(gl_short_3, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 4  THEN ISNULL(gl_short_4, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 5  THEN ISNULL(gl_short_5, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 6  THEN ISNULL(gl_short_6, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_6, '')
                WHEN si_str_id = 7  THEN ISNULL(gl_short_7, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_7, '')
                WHEN si_str_id = 8  THEN ISNULL(gl_short_8, '') + CAST(si_entryno AS NVARCHAR)
                WHEN si_str_id = 9  THEN ISNULL(gl_short_9, '') + CAST(si_entryno AS NVARCHAR) + ISNULL(gl_suffix_9, '')
                WHEN si_str_id = 10 THEN ISNULL(gl_short_10, '') + CAST(si_entryno AS NVARCHAR)
                ELSE CAST(si_entryno AS NVARCHAR)
                END AS si_loc_entryno,
                si_acc_id,si_cust_name,si_salesman_id,si_location_id,si_remarks,ISNULL(cast(si_gross_value as nvarchar(100)),'0') si_gross_value,ISNULL(cast(si_disc as nvarchar(100)),'0') si_disc,ISNULL(cast(si_disc_per as nvarchar(100)),'0') si_disc_per,ISNULL(cast(si_net_amount as nvarchar(100)),'0') si_net_amount,ISNULL(cast(si_tax as nvarchar(100)),'0') si_tax,ISNULL(cast(si_total as nvarchar(100)),'0') im_rate,ISNULL(cast(si_loading_charge as nvarchar(100)),'0') im_rate,ISNULL(cast(si_other_charge as nvarchar(100)),'0') si_other_charge,ISNULL(cast(si_other_disc as nvarchar(100)),'0') si_other_disc,ISNULL(cast(si_profit as nvarchar(100)),'0') si_profit,ISNULL(cast(si_cash_recieved as nvarchar(100)),'0') si_cash_recieved,ISNULL(cast(si_balance as nvarchar(100)),'0') si_balance,ISNULL(cast(si_grand_total as nvarchar(100)),'0') si_grand_total,ISNULL(cast(si_igst_total as nvarchar(100)),'0') si_igst_total,ISNULL(cast(si_cgst_total as nvarchar(100)),'0') si_cgst_total,ISNULL(cast(si_sgst_total as nvarchar(100)),'0') si_sgst_total,ISNULL(cast(si_ob as nvarchar(100)),'0') si_ob,ISNULL(cast(si_net_balance as nvarchar(100)),'0') si_net_balance,ISNULL(cast(si_freight_charge as nvarchar(100)),'0') si_freight_charge,ISNULL(cast(si_sum_kfc as nvarchar(100)),'0') si_sum_kfc,ISNULL(cast(si_total as nvarchar(100)),'0') si_total,si_cash_paid_acc,CONVERT(date, si_date) as si_date,si_lc_id,si_date as [datetime],si_einvoice_ksa,si_commision_acc_id,lc_mob ,si_disp_name,si_disp_location,si_disp_add1,si_disp_scode,si_disp_pincode,si_ship_name,si_ship_location,si_ship_add1,si_ship_scode,si_ship_pincode,si_ship_gstin FROM inv_sales_inf left join acc_loyalty_card on lc_id=si_lc_id  LEFT JOIN gnl_location ON si_location_id = gl_id WHERE si_entryno=" + model.entryNo + "";

            }
            else
            {
                query += "SELECT si_entryno,si_loc_entryno,si_acc_id,si_cust_name,si_salesman_id,si_location_id,si_remarks,ISNULL(cast(si_gross_value as nvarchar(100)),'0') si_gross_value,ISNULL(cast(si_disc as nvarchar(100)),'0') si_disc,ISNULL(cast(si_disc_per as nvarchar(100)),'0') si_disc_per,ISNULL(cast(si_net_amount as nvarchar(100)),'0') si_net_amount,ISNULL(cast(si_tax as nvarchar(100)),'0') si_tax,ISNULL(cast(si_total as nvarchar(100)),'0') im_rate,ISNULL(cast(si_loading_charge as nvarchar(100)),'0') im_rate,ISNULL(cast(si_other_charge as nvarchar(100)),'0') si_other_charge,ISNULL(cast(si_other_disc as nvarchar(100)),'0') si_other_disc,ISNULL(cast(si_profit as nvarchar(100)),'0') si_profit,ISNULL(cast(si_cash_recieved as nvarchar(100)),'0') si_cash_recieved,ISNULL(cast(si_balance as nvarchar(100)),'0') si_balance,ISNULL(cast(si_grand_total as nvarchar(100)),'0') si_grand_total,ISNULL(cast(si_igst_total as nvarchar(100)),'0') si_igst_total,ISNULL(cast(si_cgst_total as nvarchar(100)),'0') si_cgst_total,ISNULL(cast(si_sgst_total as nvarchar(100)),'0') si_sgst_total,ISNULL(cast(si_ob as nvarchar(100)),'0') si_ob,ISNULL(cast(si_net_balance as nvarchar(100)),'0') si_net_balance,ISNULL(cast(si_freight_charge as nvarchar(100)),'0') si_freight_charge,ISNULL(cast(si_sum_kfc as nvarchar(100)),'0') si_sum_kfc,ISNULL(cast(si_total as nvarchar(100)),'0') si_total,si_cash_paid_acc,CONVERT(date, si_date) as si_date,si_lc_id,si_date as [datetime],si_einvoice_ksa,si_commision_acc_id,lc_mob ,si_disp_name,si_disp_location,si_disp_add1,si_disp_scode,si_disp_pincode,si_ship_name,si_ship_location,si_ship_add1,si_ship_scode,si_ship_pincode,si_ship_gstin FROM inv_sales_inf left join acc_loyalty_card on lc_id=si_lc_id WHERE si_entryno=" + model.entryNo + "";

            }
            query += "and si_str_id = " + model.siStrId + "";
            if (usqlre.user_role != "ADMIN")
            {
                query += " and si_location_id = '" + usqlre.locationId + "'";
            }
            dt = usqlre.dbReaderFill(query);
            hash.Add("main", dt);
            query = "SELECT sp_id,sp_str_id,sp_entryno,sp_uniquecode,sp_ir_id,ISNULL(cast(sp_rate as nvarchar(100)),'0') sp_rate,ISNULL(cast(sp_realrate as nvarchar(100)),'0') sp_realrate,ISNULL(cast(sp_realprate as nvarchar(100)),'0') sp_realprate,sp_qty,sp_fqty,ISNULL(cast(sp_gross_value as nvarchar(100)),'0') sp_gross_value,sp_disc_per,ISNULL(cast(sp_disc as nvarchar(100)),'0') sp_disc,ISNULL(cast(sp_real_disc as nvarchar(100)),'0') sp_real_disc,ISNULL(cast(sp_net_amount as nvarchar(100)),'0') sp_net_amount,ISNULL(cast(sp_tax as nvarchar(100)),'0') sp_tax,ISNULL(cast(sp_total as nvarchar(100)),'0') sp_total,ISNULL(cast(sp_profit as nvarchar(100)),'0') sp_profit,ISNULL(cast(sp_igst as nvarchar(100)),'0') sp_igst,ISNULL(cast(sp_cgst as nvarchar(100)),'0') sp_cgst,ISNULL(cast(sp_sgst as nvarchar(100)),'0') sp_sgst,ISNULL(cast(sp_mrp as nvarchar(100)),'0') sp_mrp,ISNULL(cast(sp_qty_multi_unit as nvarchar(100)),'0') sp_qty_multi_unit,ISNULL(cast(sp_srate_multiunit as nvarchar(100)),'0') sp_srate_multiunit,ISNULL(cast(sp_kfc as nvarchar(100)),'0') sp_kfc,ISNULL(cast(sp_prate as nvarchar(100)),'0') sp_prate,ISNULL(cast(sp_cost as nvarchar(100)),'0') sp_cost,sp_unit_multi,ir_name,ir_hsn_code,ir_allow_negative FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id WHERE sp_entryno=" + model.entryNo + " and sp_str_id=" + model.siStrId + "";
            dt = usqlre.dbReaderFill(query);
            hash.Add("products", dt);

            query = "SELECT m.im_id as value ,m.im_unit_id,m.im_conversion,u.u_name as label,ISNULL(cast(m.im_rate as nvarchar(100)),'0') im_rate,ISNULL(cast(m.im_loading_charge as nvarchar(100)),'0') im_loading_charge,ISNULL(cast(m.im_retail as nvarchar(100)),'0') im_retail,ISNULL(cast(m.im_wsale as nvarchar(100)),'0') im_wsale,ISNULL(cast(m.im_spretail as nvarchar(100)),'0') im_spretail,ISNULL(cast(m.im_branch as nvarchar(100)),'0') im_branch,im_ir_id from inv_multi_unit m inner join inv_unit u on m.im_unit_id=u.u_id where im_ir_id in (SELECT sp_ir_id FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id WHERE sp_entryno= " + model.entryNo + " and sp_str_id= " + model.siStrId + ")order by im_ir_id ";
            dt = usqlre.dbReaderFill(query);
            hash.Add("multi", dt);

            usqlre.close();
            return Ok(ReportModelContext.searializeDt(hash));
        }
 
        [HttpGet("last-sales-invoice-new/{id}")]
        public IActionResult LastSaleInvoiceNew(int id)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                DataTable CurrentSaleInvoice = new DataTable();
                DataTable BranchSaleInvoice = new DataTable();

                string sql = $@"SELECT 
    (SELECT TOP 1 si_loc_entryno_only + 1 
     FROM inv_sales_inf 
     WHERE si_str_id = {id} AND si_location_id = {usqlre.locationId}
     ORDER BY si_entryno DESC) AS next_location_entryno,
    (SELECT MAX(si_entryno) FROM inv_sales_inf WHERE si_str_id = {id}) AS max_entry_no,
                             (SELECT MIN(si_entryno) FROM inv_sales_inf WHERE si_str_id = {id}) AS min_entry_no
                             FROM inv_sales_inf 
                             WHERE si_str_id = {id}
                             GROUP BY si_str_id";
                CurrentSaleInvoice = usqlre.dbReaderFill(sql);
                usqlre.close();

                string sql1 = $@"
                            SELECT 
                            si_location_id as location_id,
                            LTRIM(RTRIM(dbo.generate_entry_no(MAX(si_loc_entryno_only) + 1, 1, 1))) AS location_entryno
                        FROM 
                            inv_sales_inf
                        WHERE 
                            si_str_id = {id}
                        GROUP BY 
                            si_location_id
                        ORDER BY 
	                        si_location_id";

                BranchSaleInvoice = usqlre.dbReaderFill(sql1);

                var currentSaleInvoice = CurrentSaleInvoice.Rows[0].Table.Columns
                    .Cast<DataColumn>()
                    .ToDictionary(col => col.ColumnName, col => CurrentSaleInvoice.Rows[0][col]);

                var branchSaleInvoice = BranchSaleInvoice.AsEnumerable()
                    .ToDictionary(
                        row => Convert.ToInt32(row["location_id"]),
                        row => row["location_entryno"].ToString());

                usqlre.close();

                var response = new ApiResponse<object>
                {
                    Status = "success",
                    StatusCode = 200,
                    Message = "Request processed successfully.",
                    Data = new { CurrentSaleInvoice = currentSaleInvoice, BranchSaleInvoice = branchSaleInvoice }
                };

                string jsonResult = ReportModelContext.searializeDt(response);
                return Content(jsonResult, "application/json");
            }
            catch (Exception ex)
            {
                var errorResponse = new ApiResponse<object>
                {
                    Status = "error",
                    StatusCode = 500,
                    Message = $"An error occurred while processing the request: {ex.Message}",
                    Data = new object[] { }
                };
                string jsonResult = ReportModelContext.searializeDt(errorResponse);

                return Content(jsonResult, "application/json");
            }
        }

        [HttpPost("sale-report-test")]
        public async Task<IActionResult> saleReportTest([FromBody] SaleReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            DataTable dt_report;
            DataTable select;

            string queryi = @"
select  sp_entryno as EntryNo,si_loc_entryno as LocEntryNo,si_date as [SDate],ir_name as Itemname,a.as_name as Customer,a.as_tin as GSTIN,b.as_name as SalesMan,isr_service_name as Agent,c_name as Category, m_name as Brand, sp_rate as Rate, sp_qty as Qty,  sp_gross_value as Gross, sp_disc as Disc, sp_net_amount as Net,sp_cgst as CGST,sp_sgst as SGST, sp_tax as Gst, sp_total as Total 
from inv_sales_par inner join inv_item_reg on sp_ir_id=ir_id inner join 
		inv_sales_inf on si_entryno=sp_entryno and si_str_id=sp_str_id
		 left join acc_subhead a on si_acc_id=a.as_id
		 left join acc_parent on a.as_ap_id=ap_id
		 left join acc_subhead b on si_commision_acc_id=b.as_id
		 left join inv_barcode on b_uniquecode=sp_uniquecode 
		 left join acc_subhead c on b_as_id=c.as_id
		 left join inv_mfr on m_id=ir_mfr_id
		 left join inv_category on c_id=ir_category_id
		 left join inv_unit mi on mi.u_id=ir_min_unit_id
		 left join inv_unit bul on bul.u_id=ir_bulk_unit_id
		 left join inv_subcategory on ir_sub_category_id=sc_id
		 left join acc_loyalty_card on lc_id=si_lc_id
		 left join inv_rout_reg on r_id=a.as_rout_id
		 left join acc_area on area_id=a.as_area_id 
		 left join inv_district on dct_id=a.as_district_id 
		 left join inv_service_reg on isr_id=si_salesman_id
		 left join gnl_users on gu_user_id=si_user_id
		 left join inv_group1 on g1_id=ir_group1
		 left join inv_group2 on g2_id=ir_group2
		 left join inv_group3 on g3_id=ir_group3
		 left join inv_sales_type_reg on sp_str_id=str_id
		 left join gnl_location on b_location_id=gl_id
		-- left join gnl_location on ir_location_id=gl_id
		 left join acc_employees s on CAST(sp_salesman as nvarchar(50))=s.as_employee_code and s.as_active=1 
		 left join inv_agent2_reg on si_ag2_id=ag2_id";
            dt_report = usqlre.dbReaderFill(queryi);

            //Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            //hash.Add("sales", dt_report);
            //return Ok(ReportModelContext.searializeDt(hash));
           // string json = JsonSerializer.Serialize(dt_report, new JsonSerializerOptions { WriteIndented = false });
           // string json = System.Text.Json.JsonSerializer.Serialize(dt_report, new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(dt_report, Newtonsoft.Json.Formatting.None);

            // Compress JSON using GZip
            byte[] compressedData = CompressGZip(json);

            // Return as FileContentResult to fit IActionResult
            return File(compressedData, "application/octet-stream", "compressed_data.gz");


        }
        private byte[] CompressGZip(string data)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress))
                using (StreamWriter writer = new StreamWriter(gzip, Encoding.UTF8))
                {
                    writer.Write(data);
                }
                return output.ToArray();
            }
        }
        [HttpPost("save-sales-target")]
        public IActionResult SaveSalesTarget([FromBody] TargetQtyRequest req)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                // UPSERT + OUTPUT inserted data
                string sql = $@"
        DECLARE @Output TABLE
        (
            st_id INT,
            st_location_id INT,
            st_yearmonth VARCHAR(7),
            st_target_qty DECIMAL(18,2),
            st_created_at DATETIME,
            st_updated_at DATETIME
        );

        IF EXISTS (
            SELECT 1 
            FROM inv_sales_target 
            WHERE st_location_id = {req.LocationId}
              AND st_yearmonth = '{req.YearMonth}'
        )
        BEGIN
            UPDATE inv_sales_target
            SET 
                st_target_qty = {req.TargetQty},
                st_updated_at = GETDATE()
            OUTPUT INSERTED.*
            INTO @Output
            WHERE st_location_id = {req.LocationId}
              AND st_yearmonth = '{req.YearMonth}';
        END
        ELSE
        BEGIN
            INSERT INTO inv_sales_target
                (st_location_id, st_yearmonth, st_target_qty,st_created_at)
            OUTPUT INSERTED.*
            INTO @Output
            VALUES
                ({req.LocationId}, '{req.YearMonth}', {req.TargetQty},GETDATE());
        END

        SELECT * FROM @Output;
        ";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                // convert datatable to single object response
                var data = new
                {
                    TargetId = Convert.ToInt32(dt.Rows[0]["st_id"]),
                    LocationId = Convert.ToInt32(dt.Rows[0]["st_location_id"]),
                    YearMonth = dt.Rows[0]["st_yearmonth"].ToString(),
                    TargetQty = Convert.ToDecimal(dt.Rows[0]["st_target_qty"]),
                    CreatedAt = dt.Rows[0]["st_created_at"],
                    UpdatedAt = dt.Rows[0]["st_updated_at"]
                };

                return Ok(new ApiResponse<object>
                {
                    Status = "success",
                    StatusCode = 200,
                    Message = "Sales Target Saved Successfully",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Status = "error",
                    StatusCode = 500,
                    Message = ex.Message,
                    Data = ""
                });
            }
        }

        [HttpPost("sales-target-report")]
        public IActionResult SalesTargetReport(int LocationId, string YearMonth)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                DataTable dt = new DataTable();
                string sql = "";
                sql = $@"
                SELECT 
                    ISNULL(SUM(
                        CASE 
                            WHEN si_str_id IN (1,2,6,4,8.14) THEN sp_qty
                            WHEN si_str_id IN (7,9,10) THEN -sp_qty
                        END
                    ),0) AS ActualQty
                FROM inv_sales_par 
                INNER JOIN inv_sales_inf ON si_entryno = sp_entryno AND si_str_id = sp_str_id
                WHERE si_location_id ={LocationId}
                AND CONVERT(VARCHAR(7), si_date, 120)= '{YearMonth}'";


                dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                decimal actual = Convert.ToDecimal(dt.Rows[0]["ActualQty"]);
                // 2️⃣ Fetch TARGET QTY from your target table (change table name & columns)
                string sqlTarget = $@"SELECT ISNULL(st_target_qty,0) AS TargetQty FROM inv_sales_target WHERE st_yearmonth = '{YearMonth}' AND (st_location_id = {LocationId}) ";
                DataTable dtT = usqlre.dbReaderFill(sqlTarget);
                decimal target = 0;

                if (dtT.Rows.Count > 0)
                {
                    target = Convert.ToDecimal(dtT.Rows[0]["TargetQty"]);
                }
                usqlre.close();

                decimal difference = target - actual;
                var response = new
                {
                    TargetQty = target,
                    ActualQty = actual,
                    Difference = difference
                };

                return Ok(new ApiResponse<object>
                {
                    Status = "success",
                    StatusCode = 200,
                    Message = "Sales Target Report generated",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = "error",
                    StatusCode = 500,
                    Message = ex.Message,
                    Data = new object()
                });
            }
        }
        [HttpPost("admin-sales-target-report")]
        public IActionResult AdminSalesTargetReport(string YearMonth)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                // 1️⃣ Get Actual sales for all locations
            //    string sql = $@"
            //SELECT 
            //gl.gl_id AS LocationId,
            //gl.gl_name AS LocationName,
            //ISNULL(SUM(
            //    CASE 
            //        WHEN inf.si_str_id IN (1,2,6,4,8) THEN par.sp_qty
            //        WHEN inf.si_str_id IN (7,9,10)    THEN -par.sp_qty
            //        ELSE 0
            //    END
            //), 0) AS ActualQty
            //FROM gnl_location gl
            //LEFT JOIN inv_sales_inf inf
            //    ON inf.si_location_id = gl.gl_id
            //    AND CONVERT(VARCHAR(7), inf.si_date, 120) = '{YearMonth}'
            //LEFT JOIN inv_sales_par par
            //    ON inf.si_entryno = par.sp_entryno
            //    AND inf.si_str_id = par.sp_str_id
            //GROUP BY gl.gl_id, gl.gl_name
            //ORDER BY gl.gl_name ASC;
            //";
                string sql = $@"
            WITH LocationQty AS
        (
            SELECT 
                gl.gl_id AS LocationId,
                gl.gl_name AS LocationName,
                ISNULL(SUM(
                    CASE 
                        WHEN inf.si_str_id IN (1,2,6,4,8,14) THEN par.sp_qty
                        WHEN inf.si_str_id IN (7,9,10)   THEN -par.sp_qty
                        ELSE 0
                    END
                ), 0) AS ActualQty
            FROM gnl_location gl
            LEFT JOIN inv_sales_inf inf
                ON inf.si_location_id = gl.gl_id
                AND CONVERT(VARCHAR(7), inf.si_date, 120) = '{YearMonth}'
            LEFT JOIN inv_sales_par par
                ON inf.si_entryno = par.sp_entryno
                AND inf.si_str_id = par.sp_str_id
            GROUP BY gl.gl_id, gl.gl_name
        )
        SELECT
            LocationId,
            LocationName,
            CASE 
                WHEN LocationId = 1 
                    THEN ISNULL((SELECT SUM(ActualQty) FROM LocationQty WHERE LocationId <> 1),0)
                ELSE ActualQty
            END AS ActualQty
        FROM LocationQty
        ORDER BY LocationId;

            ";

                DataTable dtActual = usqlre.dbReaderFill(sql);

                // Final result list
                var result = new List<object>();

                foreach (DataRow row in dtActual.Rows)
                {
                    int locationId = Convert.ToInt32(row["LocationId"]);
                    decimal actual = Convert.ToDecimal(row["ActualQty"]);

                    // 2️⃣ Fetch TARGET QTY for this Location from your target table
                    string sqlTarget = $@"SELECT ISNULL(st_target_qty, 0) AS TargetQty FROM inv_sales_target WHERE st_yearmonth = '{YearMonth}' AND st_location_id = {locationId}";

                    DataTable dtTarget = usqlre.dbReaderFill(sqlTarget);

                    decimal target = 0;
                    if (dtTarget.Rows.Count > 0)
                    {
                        target = Convert.ToDecimal(dtTarget.Rows[0]["TargetQty"]);
                    }

                    decimal difference = target - actual;

                    // 3️⃣ Add final object
                    result.Add(new
                    {
                        LocationId = locationId,
                        LocationName = row["LocationName"].ToString(),
                        TargetQty = target,
                        ActualQty = actual,
                        Difference = difference
                    });
                }

                usqlre.close();

                return Ok(new ApiResponse<object>
                {
                    Status = "success",
                    StatusCode = 200,
                    Message = "Admin sales target report generated",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Status = "error",
                    StatusCode = 500,
                    Message = ex.Message,
                    Data = new object()
                });
            }
        }

    }
}
