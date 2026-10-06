using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class InvoiceController : ControllerBase
    {

        [HttpGet("b2c-invoice/{entryNo}")]
        public async Task<IActionResult> b2cinvoice(string entryNo)
        {

            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            string sql_str = "";
            string sql_sales_inf = "";
            string addr1 = "";
            string addr2 = "";
            string gstin = "";
            string mob = "";
            string entry_No;

            DataTable dt_decimal = usqlre.dbReaderFill("select gs_value from gnl_settings where gs_name='DECIMAL'");
            int _decimal = Convert.ToInt32(dt_decimal.Rows[0][0].ToString());
            usqlre.close();

            sql = "SELECT * FROM gnl_company";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            string sqlunit = "";
            sqlunit = "select * from inv_multi_unit";
            DataTable dtCntUnit = usqlre.dbReaderFill(sqlunit);
            usqlre.close();

            //sql_str = "SELECT sp_id,sp_str_id,sp_entryno,sp_uniquecode,sp_ir_id,ISNULL(cast(sp_rate as nvarchar(100)),'0') sp_rate,ISNULL(cast(sp_realrate as nvarchar(100)),'0') sp_realrate,sp_qty,sp_fqty,ISNULL(cast(sp_gross_value as nvarchar(100)),'0') sp_gross_value,sp_disc_per,ISNULL(cast(sp_disc as nvarchar(100)),'0') sp_disc,ISNULL(cast(sp_real_disc as nvarchar(100)),'0') sp_real_disc,ISNULL(cast(sp_net_amount as nvarchar(100)),'0') sp_net_amount,ISNULL(cast(sp_tax as nvarchar(100)),'0') sp_tax,ISNULL(cast(sp_total as nvarchar(100)),'0') sp_total,ISNULL(cast(sp_profit as nvarchar(100)),'0') sp_profit,ISNULL(cast(sp_igst as nvarchar(100)),'0') sp_igst,ISNULL(cast(sp_cgst as nvarchar(100)),'0') sp_cgst,ISNULL(cast(sp_sgst as nvarchar(100)),'0') sp_sgst,ISNULL(cast(sp_mrp as nvarchar(100)),'0') sp_mrp,ISNULL(cast(sp_qty_multi_unit as nvarchar(100)),'0') sp_qty_multi_unit,ISNULL(cast(sp_srate_multiunit as nvarchar(100)),'0') sp_srate_multiunit,ISNULL(cast(sp_kfc as nvarchar(100)),'0') sp_kfc,ISNULL(cast(sp_prate as nvarchar(100)),'0') sp_prate,ISNULL(cast(sp_cost as nvarchar(100)),'0') sp_cost,sp_unit_multi,ir_name,ir_hsn_code,sp_lend_amount FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id WHERE sp_entryno=" +entryNo + " and sp_str_id=1";
            sql_str = @"select ir_name as description,ir_hsn_code as hsnCode,sp_qty as qty,sp_rate as rate,
                sp_gross_value as gross,sp_net_amount as netAmt,sp_disc as discount,ir_cgst as cgstP,
                sp_cgst as cgst,ir_sgst as sgstP,sp_sgst as sgst,ir_taxper as TaxP,sp_tax as GST,sp_total as total,
                sp.u_name as unit,u.u_name as uom
                 FROM inv_sales_par
                 left JOIN inv_item_reg on inv_sales_par.sp_ir_id = inv_item_reg.ir_id
                 left join inv_unit as sp on sp_unit_multi = sp.u_id
                 left join inv_unit as u on ir_min_unit_id = u.u_id WHERE sp_entryno=" + entryNo + " and sp_str_id=1";


            DataTable dtSalesPur = usqlre.dbReaderFill(sql_str);
            usqlre.close();
            sql_sales_inf = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],
                            right(convert(varchar(20),si_date,100),7) [time],a.as_name as custName,a.as_add1 as add1,
                            a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST, si_tax as GST, si_total as Total, si_other_charge as otherCharge, 
                            si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_cash_recieved as cashReceived,si_loc_entryno,
                            a.as_tin as gstin,s.as_name as salesman,si_einvoice_ksa from inv_sales_inf left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_salesman_id=s.as_id where si_str_id=1 and si_entryno=" + entryNo + "";
            DataTable dtSalesInf = usqlre.dbReaderFill(sql_sales_inf);
            usqlre.close();
            bool LOCATIONENTRYNO = false;
            DataTable dt_locEntryNo = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='LOCATION ENTRY NO'");
            if (dt_locEntryNo.Rows.Count > 0)
                LOCATIONENTRYNO = Convert.ToBoolean(Convert.ToInt32(dt_locEntryNo.Rows[0][0].ToString()));

            entry_No = !LOCATIONENTRYNO ? dtSalesInf.Rows[0]["entryNo"].ToString() : dtSalesInf.Rows[0]["si_loc_entryno"].ToString();


            if (dtSalesInf.Rows[0]["id"].ToString() == 1.ToString())
            {
                addr1 = "";
                addr2 = "";
                mob = "";
                gstin = "";
            }
            else
            {
                addr1 = dtSalesInf.Rows[0]["add1"].ToString();
                addr2 = dtSalesInf.Rows[0]["add2"].ToString();
                mob = dtSalesInf.Rows[0]["mob"].ToString();
                gstin = dtSalesInf.Rows[0]["gstin"].ToString();
            }
            //string responseString = "";
            bool ENABLEEINVOICEKSA = false;
            DataTable dt_settings = usqlre.dbReaderFill("select gs_status from gnl_settings where gs_value = 'ENABLEEINVOICEKSA'");
            ENABLEEINVOICEKSA = Convert.ToBoolean(Convert.ToInt32(dt_settings.Rows[0][0].ToString()));
            if (!ENABLEEINVOICEKSA)
            {
                string responseString = @"<!DOCTYPE html>
<html lang='en'>
  <head>
    <meta charset='UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>'MICTCO</title>

    <style>
      @media print {
      html, body {
      width: 210mm;
     height: 99%;
     }
     }
      .wrap {
        margin: 0;
        padding: 0;
        display: flex;
        flex-wrap: wrap;
        flex-direction: column;
        border: 3px solid black;
        margin: 10px;
        font-size: 13px;
      }
     
      body {
        width: 100%;
        height: 100%;
        font-family: 'Poppins', sans-serif;
        font-size: 13px;
      }
      .head {
        margin: 0;
        text-align: center;
        font-size: large;
        font-weight: bold;
      }
      p {
        margin: 0;
        text-align: center;
        padding-top: 5px;
      }
      .gst {
        font-size: 15px;
        font-weight: bold;
      }
      .sub5 {
        border-bottom: 1px solid;
        width: 100%;
        text-align: center;
        font-size: 17px;
      }
      .left {
        float: left;
        font-weight: bold;
      }
      .head {
        font-weight: bold;
        text-align: center;
        font-size: medium;
      }
      .table-wrapper {
        width: 100%;
      }

    
      table {
        border-collapse: collapse;
        width: 100%;
        height: 100%;
        box-sizing: border-box;
      }
      .bill-tbl {
        margin-left: 15px;
      }
      .sub-th {
        padding: 0;
        border: 1px solid black;
        border-left: 0;
        padding-top: 13px;
        text-align: left;
        background-color: rgb(225, 221, 221);
        font-weight: lighter;
      }
      .table-th {
        padding: 0;
        border: 1px solid black;
        border-left: 0;
        /* text-align: left; */
        background-color: rgb(225, 221, 221);
        font-weight: lighter;
        padding: 0;
        font-size: 15px;
        padding-left: 2px;
      }
      .table-th:last-child {
        border-right: 0px solid black;
      }

      .main-table {
        border-right: 1px solid black;
        /* padding: 10px; */
        /* padding: 0; */
        padding-left: 5px;
      }

      td:last-child {
        border-right: 0px solid black;
      }
      .details {
        display: flex;
        flex-direction: row;
        justify-content: space-around;
        gap: 90px;
        padding-top: 10px;
      }
      .bill-bold {
        font-weight: bold;
      }
      .right-tbl {
        display: flex;
        justify-content: space-evenly;
        padding-left: 90px;
      }
      .right-tbl-padding {
        padding-left: 10px;
      }
      .total {
        border-top: 1px solid;
        border-bottom: 1px solid;
        margin: 10px;
        background-color: rgb(225, 221, 221);
      }
      .amt-words {
        display: flex;
      }
      .word-amt {
        font-size: 15px;
        font-weight: bold;
      }
      .last-table {
        display: flex;
        flex-direction: row;
        justify-content: space-between;

        width: 100%;
      }

     .footer-tbl {
        border: 1.5px solid;
        margin-left: 3.5%;
        margin-top: 10px;
        padding: 0;
        margin: 0;
      }
     .last-td{
        font-size: 12px;
      }
      .footer-left-2tabels {
        display: flex;
        flex-direction: column;
        width: 48%;
        height: 100%;
      }

      .footer-wrap {
        display: flex;
        flex-direction: row;
        justify-content: space-between;
      }
      .right-data-wrap {
        display: flex;
        align-content: center;
        justify-content: flex-end;
        align-items: center;
        width: 48%;
        padding-left: 140px;
      }

      .footer-total {
        background-color: rgb(225, 221, 221);
      }
      .font {
        font-size: 13px;
      }
      td {
        font-size: 13px;
      }
      .footer-data {
        border-right: 1px solid;
        text-align: center;
      }

      .last-tbl-wrap {
        width: 100%;
        margin-left: 3.5%;
        margin-top: 10px;
        border: 2px solid;
      }
      .signature {
        background-color: rgb(225, 221, 221);
        height: 40px;
        text-align: center;
      }
      .remarks-wrap {
        margin-left: 3.5%;
        border: 2px solid;
        width: 99%;
        margin-top: 10px;
      }
      .remark {
        border-bottom: 1px solid;
        font-weight: bold;
      }
      .null-data {
        border-bottom: 1px solid;
        height: 40px;
      }
      .data-wrap {
        border-top: 1px solid;
        border-bottom: 1px solid;
      }
      .null-data1 {
        padding: 7px;
        border-right: 1px solid;
      }

     .thanks {
        font-weight: bold;
        text-align: center;
        padding-top: 10px;      }
        @media print {
        html,
        body,
        .wrap {
          width: 210mm;
          height: 297mm;
        }
      }
    </style>
  </head>
  <body>
    <div class='wrap'>
      <!-- <div class='main'> -->
      <p class='head'>" + dt.Rows[0]["com_name"].ToString();
                responseString += "</p>";
                responseString += " <p>" + dt.Rows[0]["com_add1"].ToString() + "</p>" + "<p>" + dt.Rows[0]["com_add2"].ToString() + "</p>";
                responseString += "<p>" + dt.Rows[0]["com_mob"].ToString() + "</p>" + "<p class='gst'>GSTIN:" + dt.Rows[0]["com_gstin"].ToString() + "</p>";
                responseString += "<div class='sub5'><p>SALES B2C INVOICE</p></div> <div class='details'><table class='bill-tbl'><tr> <td class='bill-bold'>BILL TO</td></tr>";
                responseString += "<tr><td class='zero'>" + dtSalesInf.Rows[0]["custName"].ToString() + "</td></tr><tr><td class='zero'>" + addr1 + "</td></tr><tr><td class='zero'>" + addr2 + "</td>";
                responseString += "<tr><td class='zero'>" + mob + "</td></tr><tr><td class='zero'>GSTIN:" + gstin + "</td></tr>";
                responseString += "</table><table class='right-tbl'><tr><td>Invoice No</td> <td style='font-weight: bold' class='right-tbl-padding'> : " + entry_No + " </td></tr>";
                responseString += "<tr><td>Date</td><td class='right-tbl-padding'>: " + dtSalesInf.Rows[0]["Date"].ToString() + "</td></tr><tr><td>Salesman</td><td class='right-tbl-padding'>:" + dtSalesInf.Rows[0]["salesman"].ToString() + "</td></tr></table></div>";
                responseString += "<div class='head'>DETAILS</div><div class='table-wrapper'> <table class='main-tbl-content'><thead><tr><th class='table-th' rowspan='2' style='text-align: right'>No</th>";
                responseString += "<th class='table-th' rowspan='2' style='text-align: left; width: 25%'>Description Of Goods </ th > ";
                responseString += "<th class='table-th'rowspan='2'style='text-align: center; padding-right: 4px'>HSN SAC</th><th class='table-th' rowspan='2' style='text-align: center'>Qty</th><th class='table-th' rowspan='2' style='text-align: center'> Unit</th> ";
                responseString += "<th class='table-th' rowspan='2' style='text-align: center; width: 50px'>Rate</th><th class='table-th' rowspan='2' style='text-align: center'>Net Amount</th>";
                responseString += "<th class='table-th' colspan='2' style='text-align: center'>CGST</th><th class='table-th' colspan='2' style='text-align: center'>SGST</th>";
                responseString += "<th class='table-th' rowspan='2' style='text-align: center'>Total Amount</th></tr>";
                responseString += "<tr><th class='sub-th' style='text-align: center'>%</th> <th class='sub-th' style='text-align: center'>Amount</th><th class='sub-th' style='text-align: center'>%</th><th class='sub-th' style='text-align: center'>Amount</th></tr></thead><tbody>";
                int cnt = 1;
                double rateSum = 0.00;
                double netSum = 0.00;
                double cgstSum = 0.00;
                double sgstSum = 0.00;
                double totalSum = 0.00;
                for (int i = 0; i < dtSalesPur.Rows.Count; i++)
                {

                    responseString += "<tr>";
                    responseString += "<td class='main-table' style='text-align: center'>" + cnt + "</td>";
                    responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["description"].ToString() + "</td>";
                    responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["hsnCode"].ToString() + "</td>";
                    responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["qty"].ToString() + "</td>";
                    if (dtCntUnit.Rows.Count > 0)
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["uom"].ToString() + "</td>";
                    else
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["unit"].ToString() + "</td>";
                    responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString() + "</td>";
                    responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["netamt"].ToString())), _decimal).ToString() + "</td>";
                    responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["cgstP"].ToString() + "</td>";
                    responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["cgst"].ToString())), _decimal).ToString() + "</td>";
                    responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["sgstP"].ToString() + "</td>";
                    responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["sgst"].ToString())), _decimal).ToString() + "</td>";
                    responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal).ToString() + "</td>";

                    //rateSum += Convert.ToDouble(dtSalesPur.Rows[i]["rate"].ToString());
                    //netSum += Convert.ToDouble(dtSalesPur.Rows[i]["netAmt"].ToString());
                    //cgstSum += Convert.ToDouble(dtSalesPur.Rows[i]["cgst"].ToString());
                    //sgstSum += Convert.ToDouble(dtSalesPur.Rows[i]["sgst"].ToString());
                    //totalSum += Convert.ToDouble(dtSalesPur.Rows[i]["total"].ToString());
                    responseString += "</tr>";
                    cnt++;
                }
                string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Total"].ToString())), _decimal).ToString()));
                responseString += "<tr class='total'><td class='main-table'></td><td class='main-table' style='text-align: center'>Total</td><td class='main-table'></td><td class='main-table'></td><td class='main-table'></td><td class='main-table' style='text-align: right'></td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString() + "</td>";
                responseString += "<td class='main-table'></td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["CGST"].ToString())), _decimal).ToString() + "</td><td class='main-table'></td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["SGST"].ToString())), _decimal).ToString() + "</td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Total"].ToString())), _decimal).ToString() + "</td></tr></tbody></table></div>";
                responseString += "<div class='amt-words'><p>Amount in Words :<span class='word-amt'>" + amtInWord + " </span></p></div>";
                responseString += "<div><h3 style='margin - left: 10px; '>Remarks:" + dtSalesInf.Rows[0]["Remarks"].ToString() + "</h3></div>";
                responseString += @"<div class='footer-wrap'><div class='footer-left-2tabels'>
                             <table class='footer-tbl'>
                             <tbody >
                             <tr>
                             <td class='font' colspan='2'>
                                ELECTRONIC BANKING REFERENCE DETAILS<br/>
                             </td>
                             </tr>
                             <tr>
                             <td style = 'font - size: 12px;'>Bank Name</td>
                             <td>:" + dt.Rows[0]["com_bankac"].ToString() + "</td></tr>";
                responseString += "<tr><td class='last - td'>Branch</td><td>:</td></tr>";
                responseString += "<tr><td class='last - td''>A/c.No.</td><td>:" + dt.Rows[0]["com_bank_accno"].ToString() + "</td></tr>";
                responseString += "<tr><td class='last - td''>IFS Code</td><td>:" + dt.Rows[0]["com_bank_ifsc"].ToString() + "</td></tr></tbody> </table></div>";
                responseString += "<div class='right-data-wrap'><table class='f-right_tbl'><tr> <td>Net Amount</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString() + "</td></tr>";
                responseString += "<tr><td>Gst</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["GST"].ToString())), _decimal).ToString() + "</td></tr>";
                responseString += "<tr><td>Other Discount</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherDiscount"].ToString())), _decimal).ToString() + "</td></tr><tr><td>Other Charges</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherCharge"].ToString())), _decimal).ToString() + "</td></tr>";
                responseString += "<tr><td>Freight Charges</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["freightCharge"].ToString())), _decimal).ToString() + "</td></tr>";
                responseString += "<tr class='footer-total'><td style='font-weight: bold'>Grand Total</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["grandTotal"].ToString())), _decimal).ToString() + "</td></tr>";
                responseString += "<tr class='footer-total'><td style='font-weight: bold'>OB</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["ob"].ToString())), _decimal).ToString() + "</td></tr>";
                responseString += "<tr class='footer-total'><td style='font-weight: bold'>Cash Received</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["cashReceived"].ToString())), _decimal).ToString() + "</td></tr>";
                responseString += "<tr class='footer-total'><td style='font-weight: bold'>Net Balance</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["netBalanace"].ToString())), _decimal).ToString() + "</td></tr><tr><td></td> </tr>";
                responseString += "<tr class='signature'><td><h3>Authorized Signatory</h3></td><td></td><td></td></tr> </table></div></div>";
                responseString += "<div><table><tr> <td class='thanks'>****** THANK YOU VISIT AGAIN ******</td></tr></table></div> </div> </ body></html>";

                return base.Content(responseString.ToString(), "text/html");

            }
            else
            {
                DataTable dt_count = usqlre.dbReaderFill("select count(ans_name) as ksacount from android_settings where ans_name='KSA INVOICE'");
                int _cntKsa = Convert.ToInt32(dt_count.Rows[0][0].ToString());
                bool KSINVOICE = false;
                DataTable dt_settings1 = usqlre.dbReaderFill("select ans_status from android_settings where ans_name = 'KSA INVOICE'");
                if (dt_settings1.Rows.Count > 0)
                {
                    KSINVOICE = Convert.ToBoolean(Convert.ToInt32(dt_settings1.Rows[0][0].ToString()));

                }

                if (!KSINVOICE && _cntKsa == 1 || _cntKsa == 0)
                {
                    string responseString = @"<!DOCTYPE html>
                            <html lang='en'>
                              <head>
                                <meta charset='UTF-8' />
                                <meta http-equiv='X-UA-Compatible' content='IE=edge' />
                                <meta name='viewport' content='width=device-width, initial-scale=1.0' />
                                <title>'MICTCO</title>

                                <style>
                                  @media print {
                                  html, body {
                                  width: 210mm;
                                 height: 99%;
                                 }
                                 }
                                  .wrap {
                                    margin: 0;
                                    padding: 0;
                                    display: flex;
                                    flex-wrap: wrap;
                                    flex-direction: column;
                                    border: 3px solid black;
                                    margin: 10px;
                                    font-size: 13px;
                                  }
     
                                  body {
                                    width: 100%;
                                    height: 100%;
                                    font-family: 'Poppins', sans-serif;
                                    font-size: 13px;
                                  }
                                  .head {
                                    margin: 0;
                                    text-align: center;
                                    font-size: large;
                                    font-weight: bold;
                                  }
                                  p {
                                    margin: 0;
                                    text-align: center;
                                    padding-top: 5px;
                                  }
                                  .gst {
                                    font-size: 15px;
                                    font-weight: bold;
                                  }
                                  .sub5 {
                                    border-bottom: 1px solid;
                                    width: 100%;
                                    text-align: center;
                                    font-size: 17px;
                                  }
                                  .left {
                                    float: left;
                                    font-weight: bold;
                                  }
                                  .head {
                                    font-weight: bold;
                                    text-align: center;
                                    font-size: medium;
                                  }
                                  .table-wrapper {
                                    width: 100%;
                                  }

    
                                  table {
                                    border-collapse: collapse;
                                    width: 100%;
                                    height: 100%;
                                    box-sizing: border-box;
                                  }
                                  .bill-tbl {
                                    margin-left: 15px;
                                  }
                                  .sub-th {
                                    padding: 0;
                                    border: 1px solid black;
                                    border-left: 0;
                                    padding-top: 13px;
                                    text-align: left;
                                    background-color: rgb(225, 221, 221);
                                    font-weight: lighter;
                                  }
                                  .table-th {
                                    padding: 0;
                                    border: 1px solid black;
                                    border-left: 0;
                                    /* text-align: left; */
                                    background-color: rgb(225, 221, 221);
                                    font-weight: lighter;
                                    padding: 0;
                                    font-size: 15px;
                                    padding-left: 2px;
                                  }
                                  .table-th:last-child {
                                    border-right: 0px solid black;
                                  }

                                  .main-table {
                                    border-right: 1px solid black;
                                    /* padding: 10px; */
                                    /* padding: 0; */
                                    padding-left: 5px;
                                  }

                                  td:last-child {
                                    border-right: 0px solid black;
                                  }
                                  .details {
                                    display: flex;
                                    flex-direction: row;
                                    justify-content: space-around;
                                    gap: 90px;
                                    padding-top: 10px;
                                  }
                                  .bill-bold {
                                    font-weight: bold;
                                  }
                                  .right-tbl {
                                    display: flex;
                                    justify-content: space-evenly;
                                    padding-left: 90px;
                                  }
                                  .right-tbl-padding {
                                    padding-left: 10px;
                                  }
                                  .total {
                                    border-top: 1px solid;
                                    border-bottom: 1px solid;
                                    margin: 10px;
                                    background-color: rgb(225, 221, 221);
                                  }
                                  .amt-words {
                                    display: flex;
                                  }
                                  .word-amt {
                                    font-size: 15px;
                                    font-weight: bold;
                                  }
                                  .last-table {
                                    display: flex;
                                    flex-direction: row;
                                    justify-content: space-between;

                                    width: 100%;
                                  }

                                 .footer-tbl {
                                    border: 1.5px solid;
                                    margin-left: 3.5%;
                                    margin-top: 10px;
                                    padding: 0;
                                    margin: 0;
                                  }
                                 .last-td{
                                    font-size: 12px;
                                  }
                                  .footer-left-2tabels {
                                    display: flex;
                                    flex-direction: column;
                                    width: 48%;
                                    height: 100%;
                                  }

                                  .footer-wrap {
                                    display: flex;
                                    flex-direction: row;
                                    justify-content: space-between;
                                  }
                                  .right-data-wrap {
                                    display: flex;
                                    align-content: center;
                                    justify-content: flex-end;
                                    align-items: center;
                                    width: 48%;
                                    padding-left: 140px;
                                  }

                                  .footer-total {
                                    background-color: rgb(225, 221, 221);
                                  }
                                  .font {
                                    font-size: 13px;
                                  }
                                  td {
                                    font-size: 13px;
                                  }
                                  .footer-data {
                                    border-right: 1px solid;
                                    text-align: center;
                                  }

                                  .last-tbl-wrap {
                                    width: 100%;
                                    margin-left: 3.5%;
                                    margin-top: 10px;
                                    border: 2px solid;
                                  }
                                  .signature {
                                    background-color: rgb(225, 221, 221);
                                    height: 40px;
                                    text-align: center;
                                  }
                                  .remarks-wrap {
                                    margin-left: 3.5%;
                                    border: 2px solid;
                                    width: 99%;
                                    margin-top: 10px;
                                  }
                                  .remark {
                                    border-bottom: 1px solid;
                                    font-weight: bold;
                                  }
                                  .null-data {
                                    border-bottom: 1px solid;
                                    height: 40px;
                                  }
                                  .data-wrap {
                                    border-top: 1px solid;
                                    border-bottom: 1px solid;
                                  }
                                  .null-data1 {
                                    padding: 7px;
                                    border-right: 1px solid;
                                  }

                                 .thanks {
                                    font-weight: bold;
                                    margin-top:0;
                                    text-align: center;
                                    padding-top: 10px;      }
                                    @media print {
                                    html,
                                    body,
                                    .wrap {
                                      width: 210mm;
                                      height: 297mm;
                                    }
                                  }
                                </style>
                              </head>
                              <body>
                                <div class='wrap'>
                                  <!-- <div class='main'> -->
                                  <p class='head'>" + dt.Rows[0]["com_name"].ToString();
                    responseString += "</p>";
                    responseString += " <p>" + dt.Rows[0]["com_add1"].ToString() + "</p>" + "<p>" + dt.Rows[0]["com_add2"].ToString() + "</p>";
                    responseString += "<p>" + dt.Rows[0]["com_mob"].ToString() + "</p>" + "<p class='gst'>Vat:" + dt.Rows[0]["com_gstin"].ToString() + "</p>";
                    responseString += "<div class='sub5'><p>SALES B2C INVOICE</p></div> <div class='details'><table class='bill-tbl'><tr> <td class='bill-bold'>BILL TO</td></tr>";
                    responseString += "<tr><td class='zero'>" + dtSalesInf.Rows[0]["custName"].ToString() + "</td></tr><tr><td class='zero'>" + addr1 + "</td></tr><tr><td class='zero'>" + addr2 + "</td>";
                    responseString += "<tr><td class='zero'>" + mob + "</td></tr><tr><td class='zero'>Vat:" + gstin + "</td></tr>";
                    responseString += "</table><table class='right-tbl'><tr><td>Invoice No</td> <td style='font-weight: bold' class='right-tbl-padding'> : " + dtSalesInf.Rows[0]["entryNo"].ToString() + " </td></tr>";
                    responseString += "<tr><td>Date</td><td class='right-tbl-padding'>: " + dtSalesInf.Rows[0]["Date"].ToString() + "</td></tr><tr><td>Salesman</td><td class='right-tbl-padding'>:" + dtSalesInf.Rows[0]["salesman"].ToString() + "</td></tr></table></div>";
                    responseString += "<div class='head'>DETAILS</div><div class='table-wrapper'> <table class='main-tbl-content'><thead><tr><th class='table-th' rowspan='2' style='text-align: right'>No</th>";
                    responseString += "<th class='table-th' rowspan='2' style='text-align: left; width: 25%'>Description Of Goods </ th > ";
                    responseString += "<th class='table-th'rowspan='2'style='text-align: center; padding-right: 4px'>HSN SAC</th><th class='table-th' rowspan='2' style='text-align: center'>Qty</th><th class='table-th' rowspan='2' style='text-align: center'> Unit</th> ";
                    responseString += "<th class='table-th' rowspan='2' style='text-align: center; width: 50px'>Rate</th><th class='table-th' rowspan='2' style='text-align: center'>Net Amount</th>";
                    responseString += "<th class='table-th' colspan='2' style='text-align: center'>Vat</th>";
                    responseString += "<th class='table-th' rowspan='2' style='text-align: center'>Total Amount</th></tr>";
                    responseString += "<tr><th class='sub-th' style='text-align: center'>%</th> <th class='sub-th' style='text-align: center'>Amount</th></tr></thead><tbody>";
                    int cnt = 1;
                    double rateSum = 0.00;
                    double netSum = 0.00;
                    double cgstSum = 0.00;
                    double sgstSum = 0.00;
                    double totalSum = 0.00;
                    for (int i = 0; i < dtSalesPur.Rows.Count; i++)
                    {

                        responseString += "<tr>";
                        responseString += "<td class='main-table' style='text-align: center'>" + cnt + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["description"].ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["hsnCode"].ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["qty"].ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["unit"].ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["netamt"].ToString())), _decimal).ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["TaxP"].ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["gst"].ToString())), _decimal).ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal).ToString() + "</td>";

                        //rateSum += Convert.ToDouble(dtSalesPur.Rows[i]["rate"].ToString());
                        //netSum += Convert.ToDouble(dtSalesPur.Rows[i]["netAmt"].ToString());
                        //cgstSum += Convert.ToDouble(dtSalesPur.Rows[i]["cgst"].ToString());
                        //sgstSum += Convert.ToDouble(dtSalesPur.Rows[i]["sgst"].ToString());
                        //totalSum += Convert.ToDouble(dtSalesPur.Rows[i]["total"].ToString());
                        responseString += "</tr>";
                        cnt++;
                    }
                    string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(dtSalesInf.Rows[0]["Total"].ToString()));
                    responseString += "<tr class='total'><td class='main-table'></td><td class='main-table' style='text-align: center'>Total</td><td class='main-table'></td><td class='main-table'></td><td class='main-table'></td><td class='main-table' style='text-align: right'></td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString() + "</td>";
                    responseString += "<td class='main-table'></td><td class='main-table' style='text-align: right'>" + dtSalesInf.Rows[0]["GST"].ToString() + "</td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Total"].ToString())), _decimal).ToString() + "</td></tr></tbody></table></div>";
                    responseString += "<div class='amt-words'><p>Amount in Words :<span class='word-amt'>" + amtInWord + " </span></p></div>";
                    responseString += "<div><h3 style='margin - left: 10px; '>Remarks:" + dtSalesInf.Rows[0]["Remarks"].ToString() + "</h3></div>";
                    responseString += @"<div class='footer-wrap'><div class='footer-left-2tabels'>
                             <table class='footer-tbl'>
                             <tbody >
                             <tr>
                             <td class='font' colspan='2'>
                                ELECTRONIC BANKING REFERENCE DETAILS<br/>
                             </td>
                             </tr>
                             <tr>
                             <td style = 'font - size: 12px;'>Bank Name</td>
                             <td>:" + dt.Rows[0]["com_bankac"].ToString() + "</td></tr>";
                    responseString += "<tr><td class='last - td'>Branch</td><td>:</td></tr>";
                    responseString += "<tr><td class='last - td''>A/c.No.</td><td>:" + dt.Rows[0]["com_bank_accno"].ToString() + "</td></tr>";
                    responseString += "<tr><td class='last - td''>IFS Code</td><td>:" + dt.Rows[0]["com_bank_ifsc"].ToString() + "</td></tr></tbody> </table></div>";
                    responseString += "<div class='right-data-wrap'><table class='f-right_tbl'><tr> <td>Net Amount</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr><td>Total Vat</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["GST"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr><td>Other Discount</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherDiscount"].ToString())), _decimal).ToString() + "</td></tr><tr><td>Other Charges</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherCharge"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr><td>Freight Charges</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["freightCharge"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr class='footer-total'><td style='font-weight: bold'>Grand Total</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["grandTotal"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr class='footer-total'><td style='font-weight: bold'>OB</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["ob"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr class='footer-total'><td style='font-weight: bold'>Net Balance</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["netBalanace"].ToString())), _decimal).ToString() + "</td></tr><tr><td></td> </tr>";
                    responseString += "<tr class='signature'><td><h3>Authorized Signatory</h3></td><td></td><td></td></tr> </table></div></div>";

                    responseString += @" <div style=text-align:center;><img src='https://chart.googleapis.com/chart?cht=qr&chl=" + dtSalesInf.Rows[0]["si_einvoice_ksa"] + "&chs=160x160&chld=L|0' class='qr-code img-thumbnail img-responsive'' />";
                    responseString += @"<div>
                <table>
                    <tr>
                        <td class=' thanks'>****** THANK YOU VISIT AGAIN ******</td>
                    </tr></table> </div> </div></div></body></html>";
                    return base.Content(responseString.ToString(), "text/html");
                }
                else
                {
                    var html = "";
                    DataTable dt_arabicname = usqlre.dbReaderFill("select count(ans_name) as ksacount from android_settings where ans_name='INVOICE NAME ARABIC'");
                    int _cntarabic = Convert.ToInt32(dt_arabicname.Rows[0][0].ToString());
                    bool ARABIC = false;
                    DataTable dt_arabic = usqlre.dbReaderFill("select ans_status from android_settings where ans_name = 'INVOICE NAME ARABIC'");
                    if (dt_arabic.Rows.Count > 0)
                    {
                        ARABIC = Convert.ToBoolean(Convert.ToInt32(dt_arabic.Rows[0][0].ToString()));

                    }
                    DataTable dt_print = usqlre.dbReaderFill("select ps_version from gnl_print_setup where ps_form='SALES-B2C-MOB'");
                    if (dt_print.Rows.Count > 0)
                    {
                        string a = b2cPrintKsaV2(entryNo);
                        return base.Content(a, "text/html");
                    }
                    else
                    {
                        html = System.IO.File.ReadAllText(@"./assets/ksaEinvoice.html");
                        string qr = "<img src='https://chart.googleapis.com/chart?cht=qr&chl=" + dtSalesInf.Rows[0]["si_einvoice_ksa"] + "&chs=160x160&chld=L|0' class='qr-code img-thumbnail img-responsive'' />";
                        #region header
                        html = html.Replace("{{com_name}}", dt.Rows[0]["com_name"].ToString());
                        html = html.Replace("{{com_add1}}", dt.Rows[0]["com_add1"].ToString());
                        html = html.Replace("{{com_add2}}", dt.Rows[0]["com_add2"].ToString());
                        html = html.Replace("{{com_add3}}", dt.Rows[0]["com_add3"].ToString());
                        html = html.Replace("{{com_telephone}}", dt.Rows[0]["com_telephone"].ToString());
                        html = html.Replace("{{com_mob}}", dt.Rows[0]["com_mob"].ToString());
                        html = html.Replace("{{com_ar_name}}", dt.Rows[0]["com_ar_name"].ToString());
                        html = html.Replace("{{com_ar_add1}}", dt.Rows[0]["com_ar_add1"].ToString());
                        html = html.Replace("{{com_ar_add2}}", dt.Rows[0]["com_ar_add2"].ToString());
                        html = html.Replace("{{com_ar_add3}}", dt.Rows[0]["com_ar_add3"].ToString());
                        html = html.Replace("{{com_ar_telephone}}", dt.Rows[0]["com_telephone"].ToString());
                        html = html.Replace("{{com_ar_mob}}", dt.Rows[0]["com_ar_mob"].ToString());
                        html = html.Replace("{{com_gstin}}", dt.Rows[0]["com_gstin"].ToString());
                        #endregion
                        html = html.Replace("{{salesman}}", dtSalesInf.Rows[0]["salesman"].ToString());
                        html = html.Replace("{{cName}}", dtSalesInf.Rows[0]["custName"].ToString());
                        html = html.Replace("{{addr1}}", addr1);
                        html = html.Replace("{{cPhone}}", addr2);
                        html = html.Replace("{{jobNo}}", entry_No);
                        html = html.Replace("{{time}}", dtSalesInf.Rows[0]["time"].ToString());
                        html = html.Replace("{{date}}", dtSalesInf.Rows[0]["Date"].ToString());
                        html = html.Replace("{{vat}}", gstin);
                        html = html.Replace("{{com_ar_name}}", dt.Rows[0]["com_ar_name"].ToString());
                        double estimatedCost = 0.00;
                        String items = "";
                        int cnt = 1;
                        for (int i = 0; i < dtSalesPur.Rows.Count; i++)
                        {
                            items += "<tr>";
                            items += "<td class='tbl-data'>" + cnt + "</td>";
                            if (!ARABIC && _cntarabic == 1 || _cntarabic == 0)
                            {
                                items += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["description"].ToString() + "</td>";
                            }
                            else
                            {
                                items += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["description"].ToString() + "<br>" + dtSalesPur.Rows[i]["hsnCode"].ToString() + "</td>";
                            }
                            items += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["qty"].ToString() + "</td>";
                            items += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString() + "</td>";
                            items += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["netAmt"].ToString())), _decimal).ToString() + "</td>";
                            items += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["gst"].ToString())), _decimal).ToString() + "</td>";
                            items += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal).ToString() + "</td>";

                            cnt = cnt + 1;
                        }
                        string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(dtSalesInf.Rows[0]["Total"].ToString()));
                        html = html.Replace("{{items}}", items);
                        html = html.Replace("{{amtInWord}}", amtInWord);
                        html = html.Replace("{{remark}}", dtSalesInf.Rows[0]["Remarks"].ToString());
                        html = html.Replace("{{si_gross_value}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["gross"].ToString())), _decimal).ToString());
                        html = html.Replace("{{si_disc}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Disc"].ToString())), _decimal).ToString());
                        html = html.Replace("{{si_net}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString());
                        html = html.Replace("{{si_tax}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["gst"].ToString())), _decimal).ToString());
                        html = html.Replace("{{si_total}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Total"].ToString())), _decimal).ToString());
                        html = html.Replace("{{qr}}", qr);

                    }

                    return base.Content(html, "text/html");
                    //                    string responseString = @"<!DOCTYPE html>
                    //<html lang='en'>
                    //  <head>
                    //    <meta charset='UTF-8' />
                    //    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
                    //    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
                    //    <title>Bill</title>
                    //    <style>
                    //      body {
                    //        width: 100%;
                    //        margin-left: 20px;
                    //      }
                    //      * {
                    //        font-family: Arial, Helvetica, sans-serif;
                    //        margin: 0;
                    //        padding: 0;
                    //        border: 0;
                    //        outline: 0;
                    //        font-size: 100%;
                    //        vertical-align: baseline;
                    //        background: transparent;
                    //      }
                    //      p {
                    //        margin: 0;
                    //      }
                    //​
                    //      @page {
                    //        size: A4;
                    //        margin: 0;
                    //      }
                    //      @media print {
                    //        html,
                    //        body {
                    //          width: 210mm;
                    //          height: 99%;
                    //        }
                    //        .header-wrap {
                    //          color: rgb(7, 146, 99);
                    //        }
                    //        .tbl-head{
                    //            background-color: rgb(168, 169, 170);
                    //        }
                    //      }
                    //      .container {
                    //        width: 98%;
                    //      }
                    //      .header-wrap {
                    //        display: flex;
                    //        justify-content: space-between;
                    //        color: rgb(7, 146, 99);
                    //      }
                    //      .header-wrap h1 {
                    //        /* line-height: 2; */
                    //        font-size: 20px;
                    //      }
                    //      .header {
                    //        text-align: center;
                    //        margin-top: 50px;
                    //        margin-bottom: 20px;
                    //      }
                    //      .head-data,
                    //      p {
                    //        font-weight: 600;
                    //        font-size: 14px;

                    //        /* line-height: 2; */
                    //      }
                    //      .head-right-data {
                    //        /* line-height: 2; */
                    //        font-size: 20px;
                    //      }
                    //      .header-tbl {
                    //        display: flex;
                    //        justify-content: space-between;
                    //      }
                    //      table {
                    //        width: 100%;
                    //      }
                    //      .details-tbl {
                    //        display: flex;
                    //        width: 100%;
                    //        flex-direction: row;
                    //        justify-content: space-between;
                    //      }
                    //      .customer-tbl {
                    //        width: 54%;
                    //      }
                    //      .payment-det-tbl {
                    //        width: 45%;
                    //      }
                    //      .customer-det {
                    //        border: 1px solid black;
                    //        border-radius: 20px;
                    //        height: 10rem;
                    //        padding-left: 10px;
                    //        padding-top: 10px;
                    //      }
                    //      .vat-det {
                    //        border: 1px solid black;
                    //        border-radius: 15px;
                    //        height: 1rem;
                    //        padding-left: 10px;
                    //        padding-top: 5px;
                    //      }
                    //      .main-tbl {
                    //        border: 1px solid black;
                    //        margin-top: 50px;
                    //        border-collapse: collapse;
                    //      }
                    //      .item-tbl,
                    //      .tbl-head,
                    //      .tbl-data {
                    //        border: 1px solid black;
                    //        border-collapse: collapse;
                    //        padding: 6px;
                    //      }
                    //      .tbl-head {
                    //        height: 3rem;
                    //        background-color: rgb(168, 169, 170);
                    //        text-align: center;
                    //        padding-top: 20px;
                    //      }
                    //      .foot-total {
                    //        display: flex;
                    //        flex-direction: row;
                    //        justify-content: space-between;
                    //      }
                    //      .amt-in-words {
                    //        border: 1px solid black;
                    //        border-radius: 6px;
                    //        height: 2rem;
                    //      }
                    //      .foot-amt {
                    //        width: 6rem;
                    //        border: 1px solid black;
                    //        border-radius: 5px;
                    //        text-align: center;
                    //        vertical-align: middle;
                    //      }
                    //      .date {
                    //        font-size: 20px;
                    //        font-weight: 600;
                    //      }
                    //      .invoice p {
                    //        line-height: 0;
                    //      }
                    //      .amt-in-arab {
                    //        font-weight: bold;
                    //      }
                    //      .foot-right-data,
                    //      p {
                    //        text-align: right;
                    //        padding-right: 10px;
                    //      }
                    //    </style>
                    //  </head>
                    //  <body>
                    //    <div class='container'>
                    //      <div class='header-wrap'>
                    //        <div>
                    //          <h1>" + dt.Rows[0]["com_name"].ToString() + @"</h1>
                    //          <p class='head-data' style='text-align: center'>
                    //            " + dt.Rows[0]["com_add1"].ToString() + @"
                    //          </p>
                    //          <p style='text-align: center'>" + dt.Rows[0]["com_add2"].ToString() + @"</p>
                    //        </div>
                    //        <div>
                    //          <h1 class='head-right-data'>
                    //            " + dt.Rows[0]["com_ar_name"].ToString() + @"
                    //          </h1>
                    //          <p style='text-align: center'>" + dt.Rows[0]["com_ar_add1"].ToString() + @" : </p>
                    //          <p style='text-align: center'>" + dt.Rows[0]["com_ar_add2"].ToString() + @"</p>
                    //        </div>
                    //      </div>
                    //      <div>
                    //        <h2 class='header'>SIMPLIFIED TAX INVOICE - فاتورة ضريبية مبسطة</h2>
                    //      </div>
                    //      <div class='header-tbl'>
                    //        <div>
                    //          <table>
                    //            <tr>
                    //              <td style='font-weight: bold'>DATE</td>
                    //              <td class='date'>: " + dtSalesInf.Rows[0]["Date"].ToString() + @"</td>
                    //              <td style='font-weight: bold; padding-left: 15px'>: تاریخ </td>
                    //            </tr>
                    //            <tr>
                    //              <td style='font-weight: bold'>REP</td>
                    //              <td style='font-weight: bold'>:" + dtSalesInf.Rows[0]["salesman"].ToString() + @"</td>
                    //              <td style='font-weight: bold; padding-left: 15px'>: مندوب</td>
                    //            </tr>
                    //          </table>
                    //        </div>
                    //        <div class='invoice'>
                    //          <p style='font-size: 1.2em'>
                    //            INVOICE : " + dtSalesInf.Rows[0]["entryNo"].ToString() + @"
                    //            <span style='margin-left: 5px'>: رقم الفاتورة</span>
                    //          </p>
                    //        </div>
                    //      </div>
                    //      <div class='details-tbl'>
                    //        <div class='customer-tbl'>
                    //          <table class='customer-det'>
                    //            <tr>
                    //              <th style='text-align: left'>CUSTOMER CODE</th>
                    //              <td>:</td>
                    //              <td>: كود العميل</td>
                    //            </tr>
                    //            <tr>
                    //              <th style='text-align: left'>CUSTOMER NAME</th>
                    //              <td>
                    //                :
                    //                <span style='font-weight: bold'
                    //                  >" + dtSalesInf.Rows[0]["custName"].ToString() + @"</span
                    //                >
                    //              </td>
                    //              <td>: اسم الزبون</td>
                    //            </tr>
                    //            <tr>
                    //              <th style='text-align: left'>ADDRESS</th>
                    //              <td>:" + addr1 + @"</td>
                    //              <td>: العنوان</td>
                    //            </tr>
                    //            <tr>
                    //              <th style='text-align: left'>VAT#</th>
                    //              <td>:" + gstin + @"</td>
                    //              <td>: رقم الضريبي</td>
                    //            </tr>
                    //          </table>
                    //        </div>
                    //        <div class='payment-det-tbl'>
                    //          <table class='vat-det'>
                    //            <tr>
                    //              <td>VAT-NO</td>
                    //              <td>: " + dt.Rows[0]["com_gstin"].ToString() + @"</td>
                    //              <td>: رقم الضريبي</td>
                    //            </tr>
                    //          </table>
                    //          <table style='height: 10rem'>
                    //            <tr>
                    //              <td style='padding-left: 10px'>P.O NO</td>
                    //              <td>:</td>
                    //              <td>: رقم الضريبي</td>
                    //            </tr>
                    //            <tr>
                    //              <td style='padding-left: 10px'>PAYMENT MODE</td>
                    //              <td>:</td>
                    //              <td>:نوع البيع</td>
                    //            </tr>
                    //            <tr>
                    //              <td style='padding-left: 10px'>DELIVERY NOTE</td>
                    //              <td>:</td>
                    //              <td>:سند <br />الاستلام</td>
                    //            </tr>
                    //          </table>
                    //        </div>
                    //      </div>
                    //      <div class='main-tbl'>
                    //        <table class='item-tbl' style='width: 100%'>
                    //          <tr>
                    //            <th class='tbl-head'>#</th>
                    //            <th class='tbl-head' style='text-align: left'>
                    //              البيان<br />Item Name
                    //            </th>
                    //            <th class='tbl-head'>الكمية<br />QTY</th>
                    //            <th class='tbl-head'>سعر الوحدة<br />Unit Price</th>
                    //            <th class='tbl-head'>جمالي بدون ضريبة<br />Total without VAT</th>
                    //            <th class='tbl-head'>الضريبة<br />VAT</th>
                    //            <th class='tbl-head'>الضريبة<br />Total with VAT</th>
                    //          </tr>";
                    //                    int cnt = 1;
                    //                    for (int i = 0; i < dtSalesPur.Rows.Count; i++)
                    //                    {
                    //                        responseString += "<tr>";
                    //                        responseString += "<td class='tbl-data'>" + cnt + "</td>";
                    //                        if (!ARABIC && _cntarabic == 1 || _cntarabic == 0)
                    //                        {
                    //                            responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["description"].ToString() + "</td>";
                    //                        }
                    //                        else
                    //                        {
                    //                            responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["description"].ToString() + "<br>" + dtSalesPur.Rows[i]["hsnCode"].ToString() + "</td>";
                    //                        }
                    //                        responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["qty"].ToString() + "</td>";
                    //                        responseString += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString() + "</td>";
                    //                        responseString += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["netAmt"].ToString())), _decimal).ToString() + "</td>";
                    //                        responseString += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["gst"].ToString())), _decimal).ToString() + "</td>";
                    //                        responseString += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal).ToString() + "</td>";

                    //                        cnt = cnt + 1;
                    //                    }
                    //                    string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(dtSalesInf.Rows[0]["Total"].ToString()));
                    //                    responseString += @"</table>
                    //      </div>
                    //      <div style='display:flex';>
                    //<img src='https://chart.googleapis.com/chart?cht=qr&chl=" + dtSalesInf.Rows[0]["si_einvoice_ksa"] + "&chs=160x160&chld=L|0' class='qr-code img-thumbnail img-responsive'' />";
                    //                    responseString += @"<table style='margin-top: 20px'>
                    //          <tr>
                    //            <td rowspan='3'>";
                    //                    responseString += @"</td>
                    //            <td>
                    //              <div class='foot-total'>
                    //                <p>TOTAL IN WORDS</p>
                    //                <p>المجموع في الكلمات</p>
                    //              </div>
                    //            </td>
                    //            <td style='line-height: 1.8'>
                    //              <p>المبلغ الإجمالي</p>
                    //              <p>GROSS AMOUNT</p>
                    //            </td>
                    //            <td class='foot-amt'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["gross"].ToString())), _decimal).ToString() + @" <b> ريال </b></td>
                    //          </tr>
                    //          <tr>
                    //            <td style='line-height: 1.8'>
                    //              <div class='amt-in-words'>
                    //                <p style='text-align: center'>
                    //                  " + amtInWord + @"
                    //                </p>
                    //              </div>
                    //            </td>
                    //            <td class='foot-right-data' style='line-height: 1.8'>
                    //              <p>ضريبة القيمة المضافة</p>
                    //              <p>VAT AMOUNT</p>
                    //            </td>
                    //            <td class='foot-amt'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["gst"].ToString())), _decimal).ToString() + @" <b> ريال </b></td>
                    //          </tr>
                    //          <tr>
                    //            <td></td>
                    //            <td style='line-height: 1.8'>
                    //              <p>المجموع الكلي</p>
                    //              <p>GRAND TOTAL</p>
                    //            </td>
                    //            <td class='foot-amt'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["grandTotal"].ToString())), _decimal).ToString() + @"<b>ريال </b></td>
                    //          </tr>
                    //        </table>
                    //      </div>
                    //    </div>
                    //  </body>
                    //</html>";
                    //                    return base.Content(responseString.ToString(), "text/html");



                }
            }
        }



        [HttpGet("b2b-invoice/{entryNo}")]
        public async Task<IActionResult> b2binvoice(string entryNo)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            string sql_str = "";
            string sql_sales_inf = "";
            string addr1 = "";
            string addr2 = "";
            string gstin = "";
            string mob = "";
            string entry_No = "";

            DataTable dt_decimal = usqlre.dbReaderFill("select gs_value from gnl_settings where gs_name='DECIMAL'");
            int _decimal = Convert.ToInt32(dt_decimal.Rows[0][0].ToString());
            usqlre.close();

            sql = "SELECT * FROM gnl_company";
            DataTable dtCompany = usqlre.dbReaderFill(sql);
            usqlre.close();
            string sqlunit = "";
            sqlunit = "select * from inv_multi_unit";
            DataTable dtCntUnit = usqlre.dbReaderFill(sqlunit);
            usqlre.close();

            sql_str = @"select ir_name as description,ir_hsn_code as hsnCode,sp_qty as qty,sp_rate as rate,sp_gross_value as gross,
                    sp_net_amount as netAmt,sp_disc as discount,ir_cgst as cgstP,sp_cgst as cgst,ir_sgst as sgstP,
                    sp_sgst as sgst,ir_taxper TaxP,sp_tax as gst,sp_total as total,sp.u_name as uom,u.u_name as unit FROM inv_sales_par 
                    LEFT JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id 
                    left join inv_unit as sp on sp_unit_multi=sp.u_id 
                     left join inv_unit as u on ir_min_unit_id=u.u_id
                    WHERE sp_entryno=" + entryNo + " and sp_str_id=6";
            DataTable dtSalesPur = usqlre.dbReaderFill(sql_str);
            usqlre.close();
            sql_sales_inf = @"select  si_entryno as entryNo,si_acc_id as id,CONVERT(varchar,si_date,6) as [Date],right(convert(varchar(20),si_date,100),7) [time],si_deliverydate as deliveryDate,
                            si_disc as disc, si_gross_value as gross,si_net_amount as net,si_cgst_total as cgst,si_sgst_total as sgst,
                            si_tax as gst, si_total as Total, si_other_charge as otherCharge,si_other_disc as otherDiscount,
                            si_grand_total as grandTotal,si_remarks as remarks,si_freight_charge as freightCharge,si_ob as ob,
                            si_net_balance as netBalance,a.as_name as custName,a.as_add1 as add1,a.as_add2 as add2,a.as_add3 as add3,si_loc_entryno,
                            a.as_mob as mob,a.as_tin as gstin,a.as_state as state,a.as_state_code as stateCode,s.as_name as salesman ,si_einvoice_ksa,si_cash_recieved as cashReceived,DATEDIFF(month, si_deliverydate, si_date) as date_difference
                            from inv_sales_inf left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_salesman_id=s.as_id 
                            where si_str_id=6 and si_entryno=" + entryNo + "";
            DataTable dtSalesInf = usqlre.dbReaderFill(sql_sales_inf);
            usqlre.close();
            bool LOCATIONENTRYNO = false;
            DataTable dt_locEntryNo = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='LOCATION ENTRY NO'");
            if (dt_locEntryNo.Rows.Count > 0)
                LOCATIONENTRYNO = Convert.ToBoolean(Convert.ToInt32(dt_locEntryNo.Rows[0][0].ToString()));

            entry_No = !LOCATIONENTRYNO ? dtSalesInf.Rows[0]["entryNo"].ToString() : dtSalesInf.Rows[0]["si_loc_entryno"].ToString();

            if (dtSalesInf.Rows[0]["id"].ToString() == 1.ToString())
            {
                addr1 = "";
                addr2 = "";
                mob = "";
                gstin = "";
            }
            else
            {
                addr1 = dtSalesInf.Rows[0]["add1"].ToString();
                addr2 = dtSalesInf.Rows[0]["add2"].ToString();
                mob = dtSalesInf.Rows[0]["mob"].ToString();
                gstin = dtSalesInf.Rows[0]["gstin"].ToString();
            }
            bool ENABLEEINVOICEKSA = false;
            DataTable dt_settings = usqlre.dbReaderFill("select gs_status from gnl_settings where gs_value = 'ENABLEEINVOICEKSA'");
            ENABLEEINVOICEKSA = Convert.ToBoolean(Convert.ToInt32(dt_settings.Rows[0][0].ToString()));
            if (!ENABLEEINVOICEKSA)
            {
                string responseString = @"<!DOCTYPE html>
            <html lang='en'>
              <head>
                <meta charset='UTF-8' />
                <meta http-equiv='X-UA-Compatible' content='IE=edge' />
                <meta name='viewport' content='width=device-width, initial-scale=1.0' />
                <title>MICTCO</title>
               <style>
                @media print {
                  html, body {
                  width: 210mm;
                  height: 99%;
                 }
                 }
      .parent-div {
        border: 2.5px solid black;
        /* min-height: 100%; */
        border-collapse: collapse;
        margin: 20px;
      }
      body {
        font-size: 10px;
      }
      .header-top {
        text-align: center;
        font-size: 13px;
        font-weight: bolder;
        margin: 0;
      }
      .header-main {
        text-align: center;
        font-family: sans-serif;
        font-weight: 800;
        margin: 10px;
      }
      .header-data {
        text-align: center;
        font-size: 13px;
        margin: 5px;
      }
      table {
        width: 100%;
        border-collapse: collapse;
      }
      .head-tbl {
        /* border-top: 2px solid; */
        border: 2px solid black;
        border-right: 0;
        border-left: 0;
        width: 100%;
        font-size:11px;
      }
      .h-tbl-data1 {
        border-bottom: groove;
        padding-left: 2px;
        width: 26%;
      }
      .h-tbl-data2 {
        width: 25%;
        text-align: left;
        border-bottom: groove;
        border-right: 1.5px solid;
      }
      .h-tbl-data3 {
        border-right: 1.5px solid;
      }
      .head-tbl-th {
        border-bottom: groove;
        text-align: left;
        padding-left: 2px;
      }
      .head-tbl-th0 {
        text-align: left;
        padding-left: 2px;
      }
      .head-tbl-00 {
        text-align: left;
        padding-left: 2px;
        border-top: 1.5px solid;
      }
      table {
        border-collapse: collapse;
      }
      .main-tbl-head-no {
        text-align: center;
        border: 1.5px solid;
        border-top: 0;
        border-left: 0;
        background-color: rgb(225, 221, 221);
      }
      .main-tbl-head {
        padding: 0;
        border: 1.5px solid;
        border-top: 0;
        border-left: 0;
        background-color: rgb(225, 221, 221);
        text-align: center;
        font-size:11px;
      }
      .main-tbl-head-last {
        padding: 0;
        border: 1.5px solid;
        border-top: 0;
        border-left: 0;
        padding-right: 59px;
        border-right: 0;
        background-color: rgb(225, 221, 221);
        font-size:11px;
      }
      .main-sub {
        border-right: 1.5px solid;
        border-bottom: 1.5px solid;
        background-color: rgb(225, 221, 221);
        text-align: center;
        font-size:11px;
      }
      .main-sub-last {
        text-align: center;
        border-right: 0;
        border-bottom: 1.5px solid;
        background-color: rgb(225, 221, 221);
        font-size:11px;
      }
      .main-tbl-data {
        text-align: right;
        border-right: 1.5px solid;
        border-bottom: 1.5px solid;
        font-size:11px;
      }
      .main-tbl-desc-data {
        border-right: 1.5px solid;
        border-bottom: 1.5px solid;
        text-align: left;
        width: 30%;
        font-size:11px
      }
      .main-tbl-data-last {
        font-size: 11px;
        border-bottom: 1.5px solid;
        border-left: 0;
        text-align: right;
      }
      .tbl-total {
        background-color: rgb(225, 221, 221);
        font-size:11px;

      }
      .footer-grand-tot {
        border-right: 1.5px solid;
        padding-bottom: 20px;
        padding: 0;
        padding-left: 1%;
      }
      table{
        border-collapse: collapse;
      }
      .foot-amt-no {
        padding-bottom: 62px;
        text-align: left;
        padding-left: 3%;
      }
      .footer-tbl2 {
        font-size: 6px;
        border-bottom: 1.5px solid;
        border-right: 1.5px solid;
        border-top: 1.5px solid;
        text-align: center;
        font-weight: bold;
      }
      .footer-gst-payable {
        font-size: 7px;
        border-bottom: 1.5px solid;
        border-right: 1.5px solid;
      }
      .footer-tot-amt {
        border-right: 1.5px solid;
        border-bottom: 1.5px solid;
        border-left: 1.5px solid;
        text-align: center;
        font-size: 9px;
        height: 33px;
      }
      .footer-tot-amt1 {
        border-right: 1.5px solid;
        border-bottom: 1.5px solid;
        border-left: 1.5px solid;
        text-align: center;
        font-size: 9px;
        height: 23px;
      }
      .footer-tot-amt0 {
        border-bottom: 1.5px solid;
        text-align: right;
        height: 23px;
        font-size:10px;
      }
      .footer-details {
        border-right: 1.5px solid;
        font-size: 9px;
        /* border-bottom: 1.5px solid; */
      }
      .footer-terms-condition {
        border-right: 1.5px solid;
        border-bottom: 1.5px solid;
        font-weight: bold;
        font-size: 10px;
        text-align: center;
        padding-bottom: 20px;
      }
      .footer-null0 {
        border: 1.5px solid;
        height: 40px;
      }
      .footer-null1 {
        border-right: 1.5px solid;
        height: 40px;
      }
      .foot-tot-amt {
        border-right: 1.5px solid;
        border-bottom: 1.5px solid;
        font-weight: bolder;
        text-align: center;
        font-size: 8px;
        height: 33px;
      }
      .foot-tot-amt0 {
        border-bottom: 1.5px solid;
        width: 19%;
        text-align: right;
        font-weight: bolder;
        font-size: 11px
      }
      .foot-left-det {
        border-left: 1.5px solid;
        font-size: 8px;
        font-weight: bolder;
        text-align: center;
        padding-bottom: 4%;
      }
      .foot-branch {
        border-right: 1.5px solid;
      }
      .foot-branch0 {
        border-right: 1.5px solid;
        text-align: center;
        font-size: 11px;
      }
      .footer-right-last {
        border-left: 1.5px solid;
        font-size: 8px;
        font-weight: bolder;
        text-align: center;
      }
      .foot-comp-seal {
        text-align: center;
        font-weight: bolder;
        font-size: 10px;
      }
      .foot-tbl-head2{
        border-right: 1.5px solid;
      }
      .foot-tbl-head1{
        border-right: 1.5px solid;
      }
      .foot-tbl-data-left{
        border-right: 1.5px solid;
      }
    </style>
              </head>
            <body>
            <div class='parent-div'>
                  <head>
                    <div>
                      <p class='header-top'>SALES B2B INVOICE</p>
                      <h2 class='header-main'>" + dtCompany.Rows[0]["com_name"].ToString() + @"</h2>
                      <p class='header-data'>" + dtCompany.Rows[0]["com_add1"].ToString() + @"</p>
                      <p class='header-data'>" + dtCompany.Rows[0]["com_add2"].ToString() + @"</p>
                      <p class='header-data'>" + dtCompany.Rows[0]["com_mob"].ToString() + "-" + dtCompany.Rows[0]["com_telephone"].ToString() + @"</p>
                    </div>
                  </head>
            <div>
                    <table style='width: 100%' class='head-tbl'>
                      <tr>
                        <td class='h-tbl-data1'>GSTIN NO.</td>
                        <td class='h-tbl-data2'>: GSTIN:" + dtCompany.Rows[0]["com_gstin"].ToString() + @"</td>
                        <td class='h-tbl-data1'>TRANSPORTATION MODE</td>
                        <td class='h-tbl-data2' style='border-right: 0'>:</td>
                      </tr>

                      <tr>
                        <td class='h-tbl-data1'>Reverse Charge</td>
                        <td class='h-tbl-data2'>: NOT APPLICABLE</td>
                        <td class='h-tbl-data1'>VEHICLE NO</td>
                        <td class='h-tbl-data1'>:</td>
                      </tr>

                      <tr>
                        <td class='h-tbl-data1'>INVOICE No</td>
                        <td class='h-tbl-data2'>: " + entry_No + @"</td>
                        <td class='h-tbl-data1'>DATE OF SUPPLY</td>";
                if (Convert.ToInt32(dtSalesInf.Rows[0]["date_difference"].ToString()) > 1)
                {

                    responseString += @"<td class='h-tbl-data1'>: " + dtSalesInf.Rows[0]["Date"].ToString() + @"</td>";
                }
                else
                {
                    responseString += @"<td class='h-tbl-data1'>: " + dtSalesInf.Rows[0]["deliveryDate"].ToString() + @"</td>";
                }

                responseString += @" </tr>
                     <tr>
                        <td class='h-tbl-data1'>INVOICE Date</td>
                        <td class='h-tbl-data2'>: " + dtSalesInf.Rows[0]["Date"].ToString() + @"</td>
                        <td class='h-tbl-data1'>TIME OF BILLING</td>
                        <td class='h-tbl-data1'>:" + dtSalesInf.Rows[0]["time"].ToString() + @"</td>
                      </tr>
                      <tr></tr>

                      <tr>
                        <td style='border-bottom: 1.5px solid'>STATE / CODE</td>
                        <td class='h-tbl-data3' style='border-bottom: 1.5px solid'>
                          :" + dtCompany.Rows[0]["com_state"].ToString() + "/" + dtCompany.Rows[0]["com_state_code"].ToString() + @" 
                        </td>
                        <td style='border-bottom: 1.5px solid'>PLACE OF SUPPLY</td>
                        <td style='border-bottom: 1.5px solid'>:</td>
                      </tr>
                      <tr>
                        <th class='head-tbl-th'>NAME OF CONSIGNEE</th>
                        <th colspan='3' class='head-tbl-th'>:" + dtSalesInf.Rows[0]["custName"].ToString() + @"</th>
                      </tr>
                      <tr>
                        <th class='head-tbl-th'>SHIPPING ADDRESS</th>
                        <th colspan='3' class='head-tbl-th'>: " + addr1 + @"</th>
                      </tr>
                      <tr>
                        <th class='head-tbl-th' style='height: 15px'></th>
                        <th colspan='3' class='head-tbl-th'></th>
                      </tr>
                      <tr>
                        <th class='head-tbl-th'>CONTACT NO</th>
                        <th class='head-tbl-th'>: " + mob + @"</th>
                        <th class='head-tbl-th'>E-mail ID</th>
                        <th class='head-tbl-th'>:" + dtCompany.Rows[0]["com_email"].ToString() + @"</th>
                      </tr>
                      <tr>
                        <th class='head-tbl-th0'>CONSIGNEE GSTIN No</th>
                        <th class='head-tbl-th0'>: " + gstin + @"</th>
                        <th class='head-tbl-th0'>STATE / CODE</th>
                        <th class='head-tbl-th0'>:" + dtSalesInf.Rows[0]["state"].ToString() + "/" + dtSalesInf.Rows[0]["stateCode"].ToString() + @"</th>
                      </tr>
                      <tr>
                        <td class='head-tbl-00'>Purchase Order No</td>
                        <td class='head-tbl-00'>:</td>
                        <td class='head-tbl-00' style='border-left: 1.5px solid'>
                          Purchase Order Date
                        </td>
                        <td class='head-tbl-00'>:</td>
                      </tr>
                    </table>
                  </div>
                    <div>
        <table style='width: 100%' class='main-tbl-content'>";
                responseString += @"<tr>
                      <td class='main-tbl-head-no' rowspan='2'>No</td>
                      <td class='main-tbl-head' rowspan='2'>Description Of Products</td>
                      <td class='main-tbl-head' rowspan='2'>HSN CODE</td>
                      <td class='main-tbl-head' rowspan='2' style='width: 38px'>UNIT</td>
                      <td class='main-tbl-head' rowspan='2' style='width: 35px'>Qty</td>
                      <td class='main-tbl-head' rowspan='2' style='width: 47px'>Rate</td>
                      <td class='main-tbl-head' rowspan='2' style='width: 50px'>
                        Amount
                      </td>
                      <td class='main-tbl-head' rowspan='2'>Less Dscnt</td>
                      <td class='main-tbl-head' rowspan='2'>Taxable Value</td>
                      <td class='main-tbl-head' colspan='2'>CGST</td>
                      <td class='main-tbl-head' colspan='2'>SGST</td>
                      <td class='main-tbl-head-last' colspan='2'></td>
                    </tr>
                    <tr>
                      <td class='main-sub'>%</td>
                      <td class='main-sub' style='width: 51px'>Amount</td>
                      <td class='main-sub'>%</td>
                      <td class='main-sub' style='width: 51px'>Amount</td>
                      <td class='main-sub-last'>TOTAL</td>
                    </tr>
                   ";
                int cnt = 1;
                double _totalQty = 0.00;
                double _totalRate = 0.00;
                for (int i = 0; i < dtSalesPur.Rows.Count; i++)
                {
                    responseString += @"<tr>
            <td class='main-tbl-data'>" + cnt + @"</td>
            <td class='main-tbl-desc-data'>" + dtSalesPur.Rows[i]["description"].ToString() + @"</td>
            <td class='main-tbl-data'>" + dtSalesPur.Rows[i]["hsnCode"].ToString() + @"</td>";
                    if (dtCntUnit.Rows.Count > 0)
                        responseString += "<td class='main-tbl-data'>" + dtSalesPur.Rows[i]["uom"].ToString() + @"</td>";
                    else
                        responseString += "<td class='main-tbl-data'>" + dtSalesPur.Rows[i]["unit"].ToString() + @"</td>";
                    responseString += @"<td class='main-tbl-data'>" + dtSalesPur.Rows[i]["qty"].ToString() + @"</td>
            <td class='main-tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString() + @"</td>
            <td class='main-tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["gross"].ToString())), _decimal).ToString() + @"</td>
            <td class='main-tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["discount"].ToString())), _decimal).ToString() + @"</td>
            <td class='main-tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["netamt"].ToString())), _decimal).ToString() + @"</td>
            <td class='main-tbl-data' style='text-align: center'>" + dtSalesPur.Rows[i]["cgstP"].ToString() + @"</td>
            <td class='main-tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["cgst"].ToString())), _decimal).ToString() + @"</td>
            <td class='main-tbl-data' style='text-align: center'>" + dtSalesPur.Rows[i]["sgstP"].ToString() + @"</td>
            <td class='main-tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["sgst"].ToString())), _decimal).ToString() + @"</td>
            <td class='main-tbl-data-last'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal).ToString() + @"</td>
          </tr>";
                    _totalQty += Convert.ToDouble(dtSalesPur.Rows[i]["qty"].ToString());
                    _totalRate += Convert.ToDouble(System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString());
                    cnt += 1;
                }
                string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(dtSalesInf.Rows[0]["total"].ToString()));
                responseString += @"<tr class='tbl-total'>
                      <td class='main-tbl-data'></td>
                      <td class='main-tbl-data'>Total</td>
                      <td class='main-tbl-data'></td>
                      <td class='main-tbl-data'></td>
                      <td class='main-tbl-data'>" + _totalQty + @"</td>
                      <td class='main-tbl-data'>" + _totalRate + @"</td>
                      <td class='main-tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["gross"].ToString())), _decimal).ToString() + @"</td>
                      <td class='main-tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["disc"].ToString())), _decimal).ToString() + @"</td>
                      <td class='main-tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["net"].ToString())), _decimal).ToString() + @"</td>
                      <td class='main-tbl-data'></td>
                      <td class='main-tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["cgst"].ToString())), _decimal).ToString() + @"</td>
                      <td class='main-tbl-data'></td>
                      <td class='main-tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["sgst"].ToString())), _decimal).ToString() + @"</td>
                      <td class='main-tbl-data-last'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["total"].ToString())), _decimal).ToString() + @"</td>
                    </tr>
                  </table>
                </div>
<table style='width: 100%'>
  <tr>
    <td colspan='5' rowspan='2' class='footer-grand-tot'>
      GRAND TOTAL (IN WORDS):
    </td>
  </tr>
  <tr>
    <td class='footer-tot-amt1'>Total Amount (B.T)</td>
    <td class='footer-tot-amt'>Rs</td>
    <td class='footer-tot-amt0'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["net"].ToString())), _decimal).ToString() + @"</td>
  </tr>
  <tr>
    <th colspan='5' rowspan='3' class='foot-amt-no'>
      <u>" + amtInWord + @" </u> <span style='padding-left: 30%;'>Only.</span>
    </th>
  </tr>
  <tr>
    <td class='footer-tot-amt'>Total CGST Amount</td>
    <td class='footer-tot-amt'>Rs</td>
    <td class='footer-tot-amt0'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["cgst"].ToString())), _decimal).ToString() + @"</td>
  </tr>
  <tr>
    <td class='footer-tot-amt'>Total SGST Amount</td>
    <td class='footer-tot-amt'>Rs</td>
    <td class='footer-tot-amt0'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["sgst"].ToString())), _decimal).ToString() + @"</td>
  </tr>
  <tr>
    <td rowspan='5' colspan='5' class='footer-tbl2'>
    </td>
   </tr>
  <tr>
    <td class='footer-tot-amt'>Tax Amount (GST)</td>
    <td class='footer-tot-amt'>Rs</td>
    <td class='footer-tot-amt0'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["gst"].ToString())), _decimal).ToString() + @"</td>
  </tr>
<tr>
    <td class='footer-tot-amt' style='font-weight: bold;'>Grand Total</td>
    <td class='footer-tot-amt' style='font-weight: bold;'>Rs</td>
    <td class='footer-tot-amt0' style='font-weight: bold;'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["grandTotal"].ToString())), _decimal).ToString() + @"</td>
  </tr>
<tr>
    <td class='footer-tot-amt'>OB</td>
    <td class='footer-tot-amt'>Rs</td>
    <td class='footer-tot-amt0'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["ob"].ToString())), _decimal).ToString() + @"</td>
  </tr>
<tr>
    <td class='footer-tot-amt'>Cash Received</td>
    <td class='footer-tot-amt'>Rs</td>
    <td class='footer-tot-amt0'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["cashReceived"].ToString())), _decimal).ToString() + @"</td>
  </tr>
  <tr>
    <td colspan='2' rowspan='2' class='footer-tbl2'>
      Certified that the particular given above are true & correct
    </td>
    <td colspan='3' rowspan='2' class='footer-gst-payable'>
      G.S.T PAYABLE ON REVERSE CHARGE IF NOT APPLICABLE
    </td>
  </tr>
  <tr>
    <td class='foot-tot-amt'>Net Balance</td>
    <td class='foot-tot-amt'>Rs</td>
    <td class='foot-tot-amt0'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["netBalance"].ToString())), _decimal).ToString() + @" </td>
  </tr>
  <tr>
    <td colspan='2' rowspan='2' class='footer-details'>
      ELECTRONIC BANKING REFERENCE DETAILS
    </td>
    <td colspan='3' rowspan='2' class='footer-terms-condition'>
      Terms & Condition of Sale (if Any)
    </td>
  </tr>
  <tr>
    <td></td>
    <td></td>
    <td class='foot-left-det'>for " + dtCompany.Rows[0]["com_name"].ToString() + @"</td>
  </tr>
  <tr>
    <td colspan='1' style='font-weight: bolder;font-size: 11px;'>Bank Name</td>
    <td rowspan='' style='font-weight: bolder;font-size: 11px;'>: " + dtCompany.Rows[0]["com_bankac"] + @"</td>
    <td class='footer-null0' colspan='3'></td>
    <td class='footer-null1' colspan='2'></td>
  </tr>
  <tr>
    <td style='font-weight: bolder;font-size: 11px;'>Branch</td>
    <td class='foot-branch' style='font-weight: bolder; font-size: 11px;'>:</td>
    <td class='foot-branch' colspan='3'></td>
    <td class='foot-branch' colspan='2'></td>
  </tr>
  <tr>
    <td colspan='1' rowspan='1' style='font-weight: bolder;font-size: 11px;'>A/c No.</td>
    <td class='foot-branch' style='font-weight: bolder;font-size: 11px;'>: " + dtCompany.Rows[0]["com_bank_accno"].ToString() + @"</td>
    <td class='foot-branch' colspan='3'></td>
    <td class='foot-branch' colspan='2'></td>
  </tr>
  <tr>
    <td style='font-weight: bolder;'>IFS Code</td>
    <td class='foot-branch' style='font-weight: bolder;font-size: 11px;'>: " + dtCompany.Rows[0]["com_bank_ifsc"].ToString() + @"</td>
    <td colspan='3' class='foot-branch0'>Customer Sign.</td>
    <td class='foot-comp-seal'>Company Seal</td>
    <td></td>
    <td class='footer-right-last'>Authorised Signatory & Name</td>
  </tr>
</table>
</div>

    </div>
  </body>
</html>";
                return base.Content(responseString.ToString(), "text/html");
            }
            else
            {
                DataTable dt_count = usqlre.dbReaderFill("select count(ans_name) as ksacount from android_settings where ans_name='KSA INVOICE'");
                int _cntKsa = Convert.ToInt32(dt_count.Rows[0][0].ToString());
                bool KSINVOICE = false;
                DataTable dt_settings1 = usqlre.dbReaderFill("select ans_status from android_settings where ans_name = 'KSA INVOICE'");
                if (dt_settings1.Rows.Count > 0)
                {
                    KSINVOICE = Convert.ToBoolean(Convert.ToInt32(dt_settings1.Rows[0][0].ToString()));

                }


                if (!KSINVOICE && _cntKsa == 1 || _cntKsa == 0)
                {
                    string responseString = @"<!DOCTYPE html>
<html lang='en'>
  <head>
    <meta charset='UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>'MICTCO</title>

    <style>
      @media print {
      html, body {
      width: 210mm;
     height: 99%;
     }
     }
      .wrap {
        margin: 0;
        padding: 0;
        display: flex;
        flex-wrap: wrap;
        flex-direction: column;
        border: 3px solid black;
        margin: 10px;
        font-size: 13px;
      }
     
      body {
        width: 100%;
        height: 100%;
        font-family: 'Poppins', sans-serif;
        font-size: 13px;
      }
      .head {
        margin: 0;
        text-align: center;
        font-size: large;
        font-weight: bold;
      }
      p {
        margin: 0;
        text-align: center;
        padding-top: 5px;
      }
      .gst {
        font-size: 15px;
        font-weight: bold;
      }
      .sub5 {
        border-bottom: 1px solid;
        width: 100%;
        text-align: center;
        font-size: 17px;
      }
      .left {
        float: left;
        font-weight: bold;
      }
      .head {
        font-weight: bold;
        text-align: center;
        font-size: medium;
      }
      .table-wrapper {
        width: 100%;
      }

    
      table {
        border-collapse: collapse;
        width: 100%;
        height: 100%;
        box-sizing: border-box;
      }
      .bill-tbl {
        margin-left: 15px;
      }
      .sub-th {
        padding: 0;
        border: 1px solid black;
        border-left: 0;
        padding-top: 13px;
        text-align: left;
        background-color: rgb(225, 221, 221);
        font-weight: lighter;
      }
      .table-th {
        padding: 0;
        border: 1px solid black;
        border-left: 0;
        /* text-align: left; */
        background-color: rgb(225, 221, 221);
        font-weight: lighter;
        padding: 0;
        font-size: 15px;
        padding-left: 2px;
      }
      .table-th:last-child {
        border-right: 0px solid black;
      }

      .main-table {
        border-right: 1px solid black;
        /* padding: 10px; */
        /* padding: 0; */
        padding-left: 5px;
      }

      td:last-child {
        border-right: 0px solid black;
      }
      .details {
        display: flex;
        flex-direction: row;
        justify-content: space-around;
        gap: 90px;
        padding-top: 10px;
      }
      .bill-bold {
        font-weight: bold;
      }
      .right-tbl {
        display: flex;
        justify-content: space-evenly;
        padding-left: 90px;
      }
      .right-tbl-padding {
        padding-left: 10px;
      }
      .total {
        border-top: 1px solid;
        border-bottom: 1px solid;
        margin: 10px;
        background-color: rgb(225, 221, 221);
      }
      .amt-words {
        display: flex;
      }
      .word-amt {
        font-size: 15px;
        font-weight: bold;
      }
      .last-table {
        display: flex;
        flex-direction: row;
        justify-content: space-between;

        width: 100%;
      }

     .footer-tbl {
        border: 1.5px solid;
        margin-left: 3.5%;
        margin-top: 10px;
        padding: 0;
        margin: 0;
      }
     .last-td{
        font-size: 12px;
      }
      .footer-left-2tabels {
        display: flex;
        flex-direction: column;
        width: 48%;
        height: 100%;
      }

      .footer-wrap {
        display: flex;
        flex-direction: row;
        justify-content: space-between;
      }
      .right-data-wrap {
        display: flex;
        align-content: center;
        justify-content: flex-end;
        align-items: center;
        width: 48%;
        padding-left: 140px;
      }

      .footer-total {
        background-color: rgb(225, 221, 221);
      }
      .font {
        font-size: 13px;
      }
      td {
        font-size: 13px;
      }
      .footer-data {
        border-right: 1px solid;
        text-align: center;
      }

      .last-tbl-wrap {
        width: 100%;
        margin-left: 3.5%;
        margin-top: 10px;
        border: 2px solid;
      }
      .signature {
        background-color: rgb(225, 221, 221);
        height: 40px;
        text-align: center;
      }
      .remarks-wrap {
        margin-left: 3.5%;
        border: 2px solid;
        width: 99%;
        margin-top: 10px;
      }
      .remark {
        border-bottom: 1px solid;
        font-weight: bold;
      }
      .null-data {
        border-bottom: 1px solid;
        height: 40px;
      }
      .data-wrap {
        border-top: 1px solid;
        border-bottom: 1px solid;
      }
      .null-data1 {
        padding: 7px;
        border-right: 1px solid;
      }

     .thanks {
        font-weight: bold;
        margin-top:0;
        text-align: center;
        padding-top: 10px;      }
        @media print {
        html,
        body,
        .wrap {
          width: 210mm;
          height: 297mm;
        }
      }
    </style>
  </head>
  <body>
    <div class='wrap'>
      <!-- <div class='main'> -->
      <p class='head'>" + dtCompany.Rows[0]["com_name"].ToString();
                    responseString += "</p>";
                    responseString += " <p>" + dtCompany.Rows[0]["com_add1"].ToString() + "</p>" + "<p>" + dtCompany.Rows[0]["com_add2"].ToString() + "</p>";
                    responseString += "<p>" + dtCompany.Rows[0]["com_mob"].ToString() + "</p>" + "<p class='gst'>Vat:" + dtCompany.Rows[0]["com_gstin"].ToString() + "</p>";
                    responseString += "<div class='sub5'><p>SALES B2C INVOICE</p></div> <div class='details'><table class='bill-tbl'><tr> <td class='bill-bold'>BILL TO</td></tr>";
                    responseString += "<tr><td class='zero'>" + dtSalesInf.Rows[0]["custName"].ToString() + "</td></tr><tr><td class='zero'>" + addr1 + "</td></tr><tr><td class='zero'>" + addr2 + "</td>";
                    responseString += "<tr><td class='zero'>" + mob + "</td></tr><tr><td class='zero'>Vat:" + gstin + "</td></tr>";
                    responseString += "</table><table class='right-tbl'><tr><td>Invoice No</td> <td style='font-weight: bold' class='right-tbl-padding'> : " + entryNo + " </td></tr>";
                    responseString += "<tr><td>Date</td><td class='right-tbl-padding'>: " + dtSalesInf.Rows[0]["Date"].ToString() + "</td></tr><tr><td>Salesman</td><td class='right-tbl-padding'>:" + dtSalesInf.Rows[0]["salesman"].ToString() + "</td></tr></table></div>";
                    responseString += "<div class='head'>DETAILS</div><div class='table-wrapper'> <table class='main-tbl-content'><thead><tr><th class='table-th' rowspan='2' style='text-align: right'>No</th>";
                    responseString += "<th class='table-th' rowspan='2' style='text-align: left; width: 25%'>Description Of Goods </ th > ";
                    responseString += "<th class='table-th'rowspan='2'style='text-align: center; padding-right: 4px'>HSN SAC</th><th class='table-th' rowspan='2' style='text-align: center'>Qty</th><th class='table-th' rowspan='2' style='text-align: center'> Unit</th> ";
                    responseString += "<th class='table-th' rowspan='2' style='text-align: center; width: 50px'>Rate</th><th class='table-th' rowspan='2' style='text-align: center'>Net Amount</th>";
                    responseString += "<th class='table-th' colspan='2' style='text-align: center'>Vat</th>";
                    responseString += "<th class='table-th' rowspan='2' style='text-align: center'>Total Amount</th></tr>";
                    responseString += "<tr><th class='sub-th' style='text-align: center'>%</th> <th class='sub-th' style='text-align: center'>Amount</th></tr></thead><tbody>";
                    int cnt = 1;
                    double _totalQty = 0.00;
                    double _totalRate = 0.00;
                    for (int i = 0; i < dtSalesPur.Rows.Count; i++)
                    {

                        responseString += "<tr>";
                        responseString += "<td class='main-table' style='text-align: center'>" + cnt + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["description"].ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["hsnCode"].ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["qty"].ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["unit"].ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["netamt"].ToString())), _decimal).ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["TaxP"].ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["gst"].ToString())), _decimal).ToString() + "</td>";
                        responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal).ToString() + "</td>";

                        _totalQty += Convert.ToDouble(dtSalesPur.Rows[i]["qty"].ToString());
                        _totalRate += Convert.ToDouble(System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString());
                        //cgstSum += Convert.ToDouble(dtSalesPur.Rows[i]["cgst"].ToString());
                        //sgstSum += Convert.ToDouble(dtSalesPur.Rows[i]["sgst"].ToString());
                        //totalSum += Convert.ToDouble(dtSalesPur.Rows[i]["total"].ToString());
                        responseString += "</tr>";
                        cnt++;
                    }
                    string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(dtSalesInf.Rows[0]["Total"].ToString()));
                    responseString += "<tr class='total'><td class='main-table'></td><td class='main-table' style='text-align: center'>Total</td><td class='main-table'></td><td class='main-table'></td><td class='main-table'>" + _totalQty + " </td><td class='main-table' style='text-align: right'>" + _totalRate + "</td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString() + "</td>";
                    responseString += "<td class='main-table'></td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["GST"].ToString())), _decimal).ToString() + "</td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Total"].ToString())), _decimal).ToString() + "</td></tr></tbody></table></div>";
                    responseString += "<div class='amt-words'><p>Amount in Words :<span class='word-amt'>" + amtInWord + " </span></p></div>";
                    responseString += "<div><h3 style='margin - left: 10px; '>Remarks:" + dtSalesInf.Rows[0]["Remarks"].ToString() + "</h3></div>";
                    responseString += @"<div class='footer-wrap'><div class='footer-left-2tabels'>
                             <table class='footer-tbl'>
                             <tbody >
                             <tr>
                             <td class='font' colspan='2'>
                                ELECTRONIC BANKING REFERENCE DETAILS<br/>
                             </td>
                             </tr>
                             <tr>
                             <td style = 'font - size: 12px;'>Bank Name</td>
                             <td>:" + dtCompany.Rows[0]["com_bankac"].ToString() + "</td></tr>";
                    responseString += "<tr><td class='last - td'>Branch</td><td>:</td></tr>";
                    responseString += "<tr><td class='last - td''>A/c.No.</td><td>:" + dtCompany.Rows[0]["com_bank_accno"].ToString() + "</td></tr>";
                    responseString += "<tr><td class='last - td''>IFS Code</td><td>:" + dtCompany.Rows[0]["com_bank_ifsc"].ToString() + "</td></tr></tbody> </table></div>";
                    responseString += "<div class='right-data-wrap'><table class='f-right_tbl'><tr> <td>Net Amount</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr><td>Total Vat</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["GST"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr><td>Other Discount</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherDiscount"].ToString())), _decimal).ToString() + "</td></tr><tr><td>Other Charges</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherCharge"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr><td>Freight Charges</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["freightCharge"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr class='footer-total'><td style='font-weight: bold'>Grand Total</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["grandTotal"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr class='footer-total'><td style='font-weight: bold'>OB</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["ob"].ToString())), _decimal).ToString() + "</td></tr>";
                    responseString += "<tr class='footer-total'><td style='font-weight: bold'>Net Balance</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["netBalance"].ToString())), _decimal).ToString() + "</td></tr><tr><td></td> </tr>";
                    responseString += "<tr class='signature'><td><h3>Authorized Signatory</h3></td><td></td><td></td></tr> </table></div></div>";
                    responseString += @" <div style=text-align:center;><img src='https://chart.googleapis.com/chart?cht=qr&chl=" + dtSalesInf.Rows[0]["si_einvoice_ksa"] + "&chs=160x160&chld=L|0' class='qr-code img-thumbnail img-responsive'' />";
                    responseString += @"<div>
                <table>
                    <tr>
                        <td class=' thanks'>****** THANK YOU VISIT AGAIN ******</td>
                    </tr></table> </div> </div></div></body></html>";
                    return base.Content(responseString.ToString(), "text/html");

                }
                else
                {
                    DataTable dt_arabicname = usqlre.dbReaderFill("select count(ans_name) as ksacount from android_settings where ans_name='INVOICE NAME ARABIC'");
                    int _cntarabic = Convert.ToInt32(dt_arabicname.Rows[0][0].ToString());
                    bool ARABIC = false;
                    DataTable dt_arabic = usqlre.dbReaderFill("select ans_status from android_settings where ans_name = 'INVOICE NAME ARABIC'");
                    if (dt_arabic.Rows.Count > 0)
                    {
                        ARABIC = Convert.ToBoolean(Convert.ToInt32(dt_arabic.Rows[0][0].ToString()));

                    }

                    string responseString = @"<!DOCTYPE html>
<html lang='en'>
  <head>
    <meta charset='UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Bill</title>
    <style>
      body {
        width: 100%;
        margin-left: 20px;
      }
      * {
        font-family: Arial, Helvetica, sans-serif;
        margin: 0;
        padding: 0;
        border: 0;
        outline: 0;
        font-size: 100%;
        vertical-align: baseline;
        background: transparent;
      }
      p {
        margin: 0;
      }
​
      @page {
        size: A4;
        margin: 0;
      }
      @media print {
        html,
        body {
          width: 210mm;
          height: 99%;
        }
        .header-wrap {
          color: rgb(7, 146, 99);
        }
        .tbl-head{
            background-color: rgb(168, 169, 170);
        }
      }
      .container {
        width: 98%;
      }
      .header-wrap {
        display: flex;
        justify-content: space-between;
        color: rgb(7, 146, 99);
      }
      .header-wrap h1 {
        /* line-height: 2; */
        font-size: 20px;
      }
      .header {
        text-align: center;
        margin-top: 50px;
        margin-bottom: 20px;
      }
      .head-data,
      p {
        font-weight: 600;
        font-size: 14px;

        /* line-height: 2; */
      }
      .head-right-data {
        /* line-height: 2; */
        font-size: 20px;
      }
      .header-tbl {
        display: flex;
        justify-content: space-between;
      }
      table {
        width: 100%;
      }
      .details-tbl {
        display: flex;
        width: 100%;
        flex-direction: row;
        justify-content: space-between;
      }
      .customer-tbl {
        width: 54%;
      }
      .payment-det-tbl {
        width: 45%;
      }
      .customer-det {
        border: 1px solid black;
        border-radius: 20px;
        height: 10rem;
        padding-left: 10px;
        padding-top: 10px;
      }
      .vat-det {
        border: 1px solid black;
        border-radius: 15px;
        height: 1rem;
        padding-left: 10px;
        padding-top: 5px;
      }
      .main-tbl {
        border: 1px solid black;
        margin-top: 50px;
        border-collapse: collapse;
      }
      .item-tbl,
      .tbl-head,
      .tbl-data {
        border: 1px solid black;
        border-collapse: collapse;
        padding: 6px;
      }
      .tbl-head {
        height: 3rem;
        background-color: rgb(168, 169, 170);
        text-align: center;
        padding-top: 20px;
      }
      .foot-total {
        display: flex;
        flex-direction: row;
        justify-content: space-between;
      }
      .amt-in-words {
        border: 1px solid black;
        border-radius: 6px;
        height: 2rem;
      }
      .foot-amt {
        width: 6rem;
        border: 1px solid black;
        border-radius: 5px;
        text-align: center;
        vertical-align: middle;
      }
      .date {
        font-size: 20px;
        font-weight: 600;
      }
      .invoice p {
        line-height: 0;
      }
      .amt-in-arab {
        font-weight: bold;
      }
      .foot-right-data,
      p {
        text-align: right;
        padding-right: 10px;
      }
    </style>
  </head>
  <body>
    <div class='container'>
      <div class='header-wrap'>
        <div>
          <h1>" + dtCompany.Rows[0]["com_name"].ToString() + @"</h1>
          <p class='head-data' style='text-align: center'>
            " + dtCompany.Rows[0]["com_add1"].ToString() + @"
          </p>
          <p style='text-align: center'>" + dtCompany.Rows[0]["com_add2"].ToString() + @"</p>
        </div>
        <div>
          <h1 class='head-right-data'>
            " + dtCompany.Rows[0]["com_ar_name"].ToString() + @"
          </h1>
          <p style='text-align: center'>" + dtCompany.Rows[0]["com_ar_add1"].ToString() + @" : </p>
          <p style='text-align: center'>" + dtCompany.Rows[0]["com_ar_add2"].ToString() + @"</p>
        </div>
      </div>
      <div>
        <h2 class='header'>SIMPLIFIED TAX INVOICE - فاتورة ضريبية مبسطة</h2>
      </div>
      <div class='header-tbl'>
        <div>
          <table>
            <tr>
              <td style='font-weight: bold'>DATE</td>
              <td class='date'>: " + dtSalesInf.Rows[0]["Date"].ToString() + @"</td>
              <td style='font-weight: bold; padding-left: 15px'>: تاریخ </td>
            </tr>
            <tr>
              <td style='font-weight: bold'>REP</td>
              <td style='font-weight: bold'>:" + dtSalesInf.Rows[0]["salesman"].ToString() + @"</td>
              <td style='font-weight: bold; padding-left: 15px'>: مندوب</td>
            </tr>
          </table>
        </div>
        <div class='invoice'>
          <p style='font-size: 1.2em'>
            INVOICE : " + dtSalesInf.Rows[0]["entryNo"].ToString() + @"
            <span style='margin-left: 5px'>: رقم الفاتورة</span>
          </p>
        </div>
      </div>
      <div class='details-tbl'>
        <div class='customer-tbl'>
          <table class='customer-det'>
            <tr>
              <th style='text-align: left'>CUSTOMER CODE</th>
              <td>:</td>
              <td>: كود العميل</td>
            </tr>
            <tr>
              <th style='text-align: left'>CUSTOMER NAME</th>
              <td>
                :
                <span style='font-weight: bold'
                  >" + dtSalesInf.Rows[0]["custName"].ToString() + @"</span
                >
              </td>
              <td>: اسم الزبون</td>
            </tr>
            <tr>
              <th style='text-align: left'>ADDRESS</th>
              <td>:" + addr1 + @"</td>
              <td>: العنوان</td>
            </tr>
            <tr>
              <th style='text-align: left'>VAT#</th>
              <td>:" + gstin + @"</td>
              <td>: رقم الضريبي</td>
            </tr>
          </table>
        </div>
        <div class='payment-det-tbl'>
          <table class='vat-det'>
            <tr>
              <td>VAT-NO</td>
              <td>: " + dtCompany.Rows[0]["com_gstin"].ToString() + @"</td>
              <td>: رقم الضريبي</td>
            </tr>
          </table>
          <table style='height: 10rem'>
            <tr>
              <td style='padding-left: 10px'>P.O NO</td>
              <td>:</td>
              <td>: رقم الضريبي</td>
            </tr>
            <tr>
              <td style='padding-left: 10px'>PAYMENT MODE</td>
              <td>:</td>
              <td>:نوع البيع</td>
            </tr>
            <tr>
              <td style='padding-left: 10px'>DELIVERY NOTE</td>
              <td>:</td>
              <td>:سند <br />الاستلام</td>
            </tr>
          </table>
        </div>
      </div>
      <div class='main-tbl'>
        <table class='item-tbl' style='width: 100%'>
          <tr>
            <th class='tbl-head'>#</th>
            <th class='tbl-head' style='text-align: left'>
              البيان<br />Item Name
            </th>
            <th class='tbl-head'>الكمية<br />QTY</th>
            <th class='tbl-head'>سعر الوحدة<br />Unit Price</th>
            <th class='tbl-head'>جمالي بدون ضريبة<br />Total without VAT</th>
            <th class='tbl-head'>الضريبة<br />VAT</th>
            <th class='tbl-head'>الضريبة<br />Total with VAT</th>
          </tr>";
                    int cnt = 1;
                    for (int i = 0; i < dtSalesPur.Rows.Count; i++)
                    {
                        responseString += "<tr>";
                        responseString += "<td class='tbl-data'>" + cnt + "</td>";
                        if (!ARABIC && _cntarabic == 1 || _cntarabic == 0)
                        {
                            responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["description"].ToString() + "</td>";
                        }
                        else
                        {
                            responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["description"].ToString() + "<br>" + dtSalesPur.Rows[i]["hsnCode"].ToString() + "</td>";
                        }
                        responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["qty"].ToString() + "</td>";
                        responseString += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal) + "</td>";
                        responseString += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["netAmt"].ToString())), _decimal) + "</td>";
                        responseString += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["gst"].ToString())), _decimal) + "</td>";
                        responseString += "<td class='tbl-data'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal) + "</td>";

                        cnt = cnt + 1;
                    }
                    string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(dtSalesInf.Rows[0]["Total"].ToString()));
                    responseString += @"</table>
      </div>
      <div>
 <div style='display:flex';>
<img src='https://chart.googleapis.com/chart?cht=qr&chl=" + dtSalesInf.Rows[0]["si_einvoice_ksa"] + "&chs=160x160&chld=L|0' class='qr-code img-thumbnail img-responsive'' />";
                    responseString += @"<table style='margin-top: 20px'>
          <tr>
            <td rowspan='3'>";
                    responseString += @" </td>
            <td>
              <div class='foot-total'>
                <p>TOTAL IN WORDS</p>
                <p>المجموع في الكلمات</p>
              </div>
            </td>
            <td style='line-height: 1.8'>
              <p>المبلغ الإجمالي</p>
              <p>GROSS AMOUNT</p>
            </td>
            <td class='foot-amt'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["gross"].ToString())), _decimal) + @" <b> ريال </b></td>
          </tr>
          <tr>
            <td style='line-height: 1.8'>
              <div class='amt-in-words'>
                <p style='text-align: center'>
                  " + amtInWord + @"
                </p>
              </div>
            </td>
            <td class='foot-right-data' style='line-height: 1.8'>
              <p>ضريبة القيمة المضافة</p>
              <p>VAT AMOUNT</p>
            </td>
            <td class='foot-amt'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["gst"].ToString())), _decimal) + @" <b> ريال </b></td>
          </tr>
          <tr>
            <td></td>
            <td style='line-height: 1.8'>
              <p>المجموع الكلي</p>
              <p>GRAND TOTAL</p>
            </td>
            <td class='foot-amt'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["grandTotal"].ToString())), _decimal) + @"<b>ريال </b></td>
          </tr>
        </table>
      </div>
    </div>
  </body>
</html>";
                    return base.Content(responseString.ToString(), "text/html");

                }

            }


        }
        #region RPV
        [HttpPost("rpv-invoice")]
        public async Task<IActionResult> rpvinvoice([FromBody] ModelVoucher model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            DataTable dt_decimal = usqlre.dbReaderFill("select gs_value from gnl_settings where gs_name='DECIMAL'");
            int _decimal = Convert.ToInt32(dt_decimal.Rows[0][0].ToString());
            usqlre.close();
            string vtype = "";
            string cashType = "";
            string at_v_type = "";
            if (model.voucherType == "SearchRv")
            {
                vtype = "Receipt Voucher";
                at_v_type = "RECEIPT";
                cashType = "Received Amount:";
            }
            else if (model.voucherType == "SearchPv")
            {
                vtype = "Payment Voucher";
                at_v_type = "PAYMENT";
                cashType = "Paid Amount:";
            }

            sql = "SELECT com_name,com_add1,com_add2,com_add3,com_gstin,com_telephone,com_mob,com_bank_accno,com_bank_ifsc,com_bankac,com_email,com_state,com_state_code FROM gnl_company";
            DataTable dtCompany = usqlre.dbReaderFill(sql);
            usqlre.close();
            string query;
            if (usqlre.get_gnl_settings("LOCATION ENTRY NO") && usqlre.userId != "1")
            {
                 query = "EXEC	 [dbo].[Sp_voucher] " +
             "@ri_entryno = " + model.entryNo + "," +
             "@ri_location_id = " + usqlre.locationId + "," +
             "@StatementType = '" + model.voucherType + "'";

            }
            else
            {
                 query = "EXEC	 [dbo].[Sp_voucher] " +
                "@ri_entryno = " + model.entryNo + "," +
                "@ri_location_id = 1, " +  
                "@StatementType = '" + model.voucherType + "'";

            }
            DataSet main_ds = usqlre.dbreadDataset(query);
            string dateeimevalue = main_ds.Tables[0].Rows[0][1].ToString();
            string dateonly = dateeimevalue.Substring(0, 10);
            string at_id = "select at_id from acc_account_transactions where at_entryno=" + model.entryNo + " and at_form='" + at_v_type + "'";
            DataTable dtAtId = usqlre.dbReaderFill(at_id);
            usqlre.close();
            string closing = @"select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr'
						ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions 
						where at_as_id=" + main_ds.Tables[1].Rows[0]["as_id"].ToString() + "and at_id<" + Convert.ToInt32(dtAtId.Rows[0][0].ToString()) + "";
            DataTable dtClosing = usqlre.dbReaderFill(closing);
            usqlre.close();

            string quert1 = @"select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr'
						ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions 
						where at_as_id=" + main_ds.Tables[1].Rows[0]["as_id"].ToString() + "and at_id<=" + Convert.ToInt32(dtAtId.Rows[dtAtId.Rows.Count - 1][0].ToString()) + "";

            //    "EXEC	 [dbo].[Sp_acc_reg] " +
            //"@as_id = " + main_ds.Tables[1].Rows[0]["as_id"].ToString() + "," +
            //"@StatementType = N'ledbalance'";
            DataSet dt_balance = usqlre.dbreadDataset(quert1);
            usqlre.close();
            bool HIDECOMPANYDETAILSFROMRPV = false;
            DataTable dt_hide = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='HIDE COMPANY DETAILS FROM RPV'");
            if (dt_hide.Rows.Count > 0)
                HIDECOMPANYDETAILSFROMRPV = Convert.ToBoolean(Convert.ToInt32(dt_hide.Rows[0][0].ToString()));


            string responseString = @"<!DOCTYPE html>
<html lang='en'>
  <head> 
    <meta charset='UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Reciept Voucher</title>
    <style>
      @media print {
      html, body {
      width: 210mm;
      height: 99%;
     }
     }
      body {
        font-size: 13px;
      }
      .bottom-margin {
        border-top: 10px solid gray;
        margin-top: 18px;
      }
      .head {
        font-weight: 900;
      }
      .header-sec {
        text-align: left;
      }
      .right-data {
        padding-left: 45%;
      }
      .date-span {
        padding-left: 17px;
      }
      .right-data1 {
        padding-left: 78%;
        margin-top: -20px;
      }
      .bill-sapn {
        padding-left: 11px;
      }
      .head-name {
        margin: 0;
      }
      .last-head-sec {
        border-bottom: groove;
      }
      table {
        width: 100%;
        border-collapse: collapse;
      }
      .main-data {
        border-right: 1.5px solid;
        border-bottom: 1.5px solid;
        border-top: 1.5px solid;
      }
      .main-data-last {
        border-bottom: 1.5px solid;
        border-top: 1.5px solid;
      }
      .total-amt {
        border-bottom: 1.5px solid;
        height: 35px;
        padding-left: 65%;
      }
      .total-amount {
        border-bottom: 1.5px solid;
        padding-left: 46%;
      }
      .amt-words {
        border-top: 1.5px solid;
        border-bottom: 1.5px solid;
        border-right: 1.5px solid;
        height: 50px;
        width: 20%;
      }
      .amt-in-words {
        border-bottom: 1.5px solid;
        border-top: 1.5px solid;
      }
      .footer-tbl {
        height: 61px;
      }
      .footer {
        text-align: end;
        margin-bottom: 0;
        height: 22px;
      }
       .bottom-margin-last {
        border-top: 10px solid gray;
        margin-top: 5%;
      }
    </style>
  </head>
   <body>
    <div class='wrap'>
      <div class='bottom-margin'></div>
      <div>
        <h3 class='head'>" + vtype + @"</h3>
​
        <div class='header-sec'>
          <table>";
            if(!HIDECOMPANYDETAILSFROMRPV)
            {
               responseString += @"<tr><th class='header-sec'>" + dtCompany.Rows[0]["com_name"].ToString() + @"</th>
              <td>DATE</td>
              <td>:" + dateonly + @"</td>
            </tr>
            <tr>
              <td style='padding-top: 1%'>" + dtCompany.Rows[0]["com_add1"].ToString() + @"</td>
              <td>Bill No</td>
              <td>:" + main_ds.Tables[0].Rows[0][0] + @"</td>
            </tr>
            <tr>
              <td style='padding-top: 1%'>" + dtCompany.Rows[0]["com_add2"].ToString() + @"</td>
            </tr>
           <tr>
              <td style='padding-top: 1%'>" + dtCompany.Rows[0]["com_mob"].ToString() + @"</td>
            </tr>
            <tr>
              <td style='padding-top: 1%; padding-bottom: 1%'>
                GSTIN : " + dtCompany.Rows[0]["com_gstin"].ToString() + @"
              </td>
             </tr>";
            }
            else
            {
                responseString += @"<tr><th class='header-sec'></th>
              <td style='width:150px;text-align:start;'>DATE:" + dateonly + @"</td>
              </tr>
            <tr>
              <td style='padding-top: 1%'></td>
              <td style='width:150px;text-align:start;'>Bill No:" + main_ds.Tables[0].Rows[0][0] + @"</td>
             </tr>";
            }
            
         responseString +=@" </table>
        </div>
      </div>
      <div style='border: 2px solid; border-top: 0'>
        <table>
          <tr>
            <td class='main-data'>No</td>
            <td class='main-data'>Particulars</td>
            <td class='main-data'>Amount</td>
            <td class='main-data'>Discount</td>
            <td class='main-data-last'>Total</td>
          </tr>";

            for (int i = 0; i < main_ds.Tables[1].Rows.Count; i++)
            {
                responseString += @"<tr>
                <td class='main-data'>" +(i+1) + @"</td>
                <td class='main-data'>" + main_ds.Tables[1].Rows[i]["as_name"].ToString() + @"</td>
                <td class='main-data'>" + main_ds.Tables[1].Rows[i][3].ToString() + @"</td>
                <td class='main-data'>" + main_ds.Tables[1].Rows[i][4].ToString() + @"</td>
                <td class='main-data-last'>" + main_ds.Tables[1].Rows[i][5].ToString() + @"</td>
          </tr>";
            }
            string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(main_ds.Tables[0].Rows[0][5].ToString()));
            responseString += @"<tr>
            <td class='main-data'></td>
            <td class='main-data'>Total</td>
            <td class='main-data'>" + main_ds.Tables[0].Rows[0][3].ToString() + @"</td>
            <td class='main-data'>" + main_ds.Tables[0].Rows[0][4].ToString() + @"</td>
            <td class='main-data-last'>" + main_ds.Tables[0].Rows[0][5].ToString() + @"</td>
          </tr>
          <tr>
          <th colspan='4' style='text-align:right;padding-top:10px'>Old Balance: </th>
          <th style='text-align:left;padding-top:10px'>" + dtClosing.Rows[0][0].ToString() + @"</th>
        </tr>
         <tr>
          <th colspan='4' style='text-align:right;'>" + cashType + @" </th>
          <th style='text-align:left;'>" + System.Math.Round((CommonHelper.GetTextboxValue(main_ds.Tables[0].Rows[0][5].ToString())), _decimal).ToString() + @"</th>
        </tr>
         <tr>
          <th colspan='4' style='text-align:right;'>Net Balance: </th>
          <th style='text-align:left;'>" + dt_balance.Tables[0].Rows[0][0].ToString() + @"</th>
        </tr>
       
        </table>
        
        <table class='footer-tbl'>
                  <tr>
                    <td class='amt-words'>Amount In Words</td>
                    <td class='amt-in-words'>
                     " + amtInWord + @"
                    </td>
                  </tr>
                </table>
                <h3 class='footer'>Authorized Signatory</h3>
              </div>
             <div class='bottom - margin - last'></div>
            </div>
          </body>
        </html>";


            return base.Content(responseString.ToString(), "text/html");
        }
        [HttpPost("bank-voucher")]
        public async Task<IActionResult> bankvoucher([FromBody] ModelVoucher model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            sql = "SELECT com_name,com_add1,com_add2,com_add3,com_gstin,com_telephone,com_mob,com_bank_accno,com_bank_ifsc,com_bankac,com_email,com_state,com_state_code FROM gnl_company";
            DataTable dtCompany = usqlre.dbReaderFill(sql);
            usqlre.close();
            String query = "EXEC	 [dbo].[Sp_Bank_voucher] " +
             "@abpr_entryno = " + model.entryNo + "," +
             "@abpr_voucher_name='" + model.voucherType + "'," +
             "@StatementType = 'Search'";
            DataSet main_ds = usqlre.dbreadDataset(query);


            string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(main_ds.Tables[0].Rows[0][7].ToString()));
            string responseString = @"<!DOCTYPE html>
<html lang='en'>
  <head>
    <meta charset='UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Bank Payment</title>
    <style>
      @media print {
      html, body {
      width: 210mm;
      height: 99%;
     }
     }
      .wrap {
        border: 2px solid;
        /* width: 100%; */
        margin: 20px;
        font-size: 13px;
      }
      .header {
        text-align: center;
      }
      .head-sec1 {
        margin: 0;
        font-weight: bold;
      }
      .header-sec-left {
        padding-left: 70%;
      }
      .header-sec-left1 {
        padding-left: 58%;
      }
      .name {
        margin: 4px;
        padding-bottom: 2%;
      }
      .col-span {
        padding-left: 41px;
      }
      table {
        width: 100%;
        border-collapse: collapse;
      }
      .tbl-head {
        border: 1.5px solid;
        border-left: 0;
        text-align: center;
      }
      .tbl-head-last {
        border-top: 1.5px solid;
        text-align: center;
      }
      .tbl-head-last1 {
        border-top: 1.5px solid;
        border-bottom: 1.5px solid;
        text-align: center;
      }
      .amt-words {
        margin: 0;
      }
      .amt {
        padding-left: 8px;
      }
      .last-data {
        padding-left: 531px;
      }
      .span-right {
        padding-left: 4px;
      }
      .footer {
        margin: 0;
      }
      .footer-data {
        padding-bottom: 51px;
      }
    </style>
  </head>
<body>
    <div class='wrap'>
      <div class='header'>
        <h4 class='head-sec1'>" + dtCompany.Rows[0]["com_name"].ToString() + @"</h4>
        <p>" + dtCompany.Rows[0]["com_add1"].ToString() + @"</p>
        <p>" + dtCompany.Rows[0]["com_add2"].ToString() + @"</p>
        <h2>" + model.voucherType + @"</h2>
      </div>
      <table>
        <tr>
          <td class='header-sec-left'>No</td>
          <td class='header-sec-left1'>:" + model.entryNo + @"</td>
        </tr>
        <tr>
          <td class='header-sec-left'>Date</td>
          <td class='header-sec-left1'>:" + Convert.ToDateTime(main_ds.Tables[0].Rows[0][2].ToString()) + @"</td>
        </tr>
      </table>
      <div>
        <p class='name'>Party Name <span class='col-span'>:" + main_ds.Tables[0].Rows[0]["as_name"].ToString() + @"</span></p>
      </div>
      <div class='main-tbl'>
        <table>
          <tr>
            <td class='tbl-head'>No.</td>
            <td class='tbl-head'>Bank Account</td>
            <td class='tbl-head'>Type</td>
            <td class='tbl-head'>Cheque/DD No.</td>
            <td class='tbl-head-last'>Total</td>
          </tr>";
            for (int i = 0; i < main_ds.Tables[1].Rows.Count; i++)
            {
                responseString += @"<tr>
                <td class='tbl-head'>" + main_ds.Tables[1].Rows[i][0].ToString() + @"</td>
                <td class='tbl-head'>" + main_ds.Tables[1].Rows[i]["as_name"].ToString() + @"</td>
                <td class='tbl-head'>" + main_ds.Tables[1].Rows[i][7].ToString() + @"</td>
                <td class='tbl-head'>" + main_ds.Tables[1].Rows[i][2].ToString() + @"</td>
                <td class='tbl-head-last1'>" + main_ds.Tables[1].Rows[i][5].ToString() + @"</td>
              </tr>";
            }

            responseString += @"<tr>
            <td class='tbl-head'></td>
            <td class='tbl-head'></td>
            <td class='tbl-head'></td>
            <td class='tbl-head'></td>
            <td class='tbl-head-last1'>" + main_ds.Tables[0].Rows[0][7].ToString() + @"</td>
          </tr>
        </table>
      </div>
      <div>
        <p class='amt-words'>
          Amount In Words :<span class='amt'> " + amtInWord + @"</span>
        </p>
      </div>
      <div>
        <p class='last-data'>
          Bank Charge <span class='span-right'> : " + main_ds.Tables[0].Rows[0][6].ToString() + @"</span>
        </p>
        <p class='last-data'>Total Amount :" + main_ds.Tables[0].Rows[0][7].ToString() + @"</p>
      </div>
      <div class='footer'>
        <p class='footer-data'>Narration : " + main_ds.Tables[0].Rows[0][8].ToString() + @"</p>
      </div>
    </div>
  </body>
</html>";
            return base.Content(responseString.ToString(), "text/html");
        }
        #endregion
        #region quotation
        [HttpGet("quotation-invoice/{entryNo}")]
        public async Task<IActionResult> quotationinvoice(string entryNo)
        // public async Task<IActionResult> testinvoice([FromBody] SaleModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            string sql_str = "";
            string sql_sales_inf = "";
            string addr1 = "";
            string addr2 = "";
            string gstin = "";
            string mob = "";
            string entry_No = "";
            sql = "SELECT com_name,com_add1,com_add2,com_add3,com_gstin,com_telephone,com_mob,com_bank_accno,com_bank_ifsc,com_bankac FROM gnl_company";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            string sqlunit = "";
            sqlunit = "select * from inv_multi_unit";
            DataTable dtCntUnit = usqlre.dbReaderFill(sqlunit);
            usqlre.close();
            DataTable dt_decimal = usqlre.dbReaderFill("select gs_value from gnl_settings where gs_name='DECIMAL'");
            int _decimal = Convert.ToInt32(dt_decimal.Rows[0][0].ToString());
            usqlre.close();
            //sql_str = "SELECT sp_id,sp_str_id,sp_entryno,sp_uniquecode,sp_ir_id,ISNULL(cast(sp_rate as nvarchar(100)),'0') sp_rate,ISNULL(cast(sp_realrate as nvarchar(100)),'0') sp_realrate,sp_qty,sp_fqty,ISNULL(cast(sp_gross_value as nvarchar(100)),'0') sp_gross_value,sp_disc_per,ISNULL(cast(sp_disc as nvarchar(100)),'0') sp_disc,ISNULL(cast(sp_real_disc as nvarchar(100)),'0') sp_real_disc,ISNULL(cast(sp_net_amount as nvarchar(100)),'0') sp_net_amount,ISNULL(cast(sp_tax as nvarchar(100)),'0') sp_tax,ISNULL(cast(sp_total as nvarchar(100)),'0') sp_total,ISNULL(cast(sp_profit as nvarchar(100)),'0') sp_profit,ISNULL(cast(sp_igst as nvarchar(100)),'0') sp_igst,ISNULL(cast(sp_cgst as nvarchar(100)),'0') sp_cgst,ISNULL(cast(sp_sgst as nvarchar(100)),'0') sp_sgst,ISNULL(cast(sp_mrp as nvarchar(100)),'0') sp_mrp,ISNULL(cast(sp_qty_multi_unit as nvarchar(100)),'0') sp_qty_multi_unit,ISNULL(cast(sp_srate_multiunit as nvarchar(100)),'0') sp_srate_multiunit,ISNULL(cast(sp_kfc as nvarchar(100)),'0') sp_kfc,ISNULL(cast(sp_prate as nvarchar(100)),'0') sp_prate,ISNULL(cast(sp_cost as nvarchar(100)),'0') sp_cost,sp_unit_multi,ir_name,ir_hsn_code,sp_lend_amount FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id WHERE sp_entryno=" +entryNo + " and sp_str_id=1";
            sql_str = @"select ir_name as description,ir_hsn_code as hsnCode,sp_qty as qty,sp_rate as rate,
                      sp_gross_value as gross,sp_net_amount as netAmt,sp_disc as discount,ir_cgst as cgstP,sp_cgst as cgst,
                      ir_sgst as sgstP,sp_sgst as sgst,sp_total as total,sp.u_name as uom,u.u_name as unit
                      FROM inv_sales_par 
                      LEFT JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id 
                      left join inv_unit as sp on sp_unit_multi=sp.u_id 
                      left join inv_unit as u on ir_min_unit_id=u.u_id
                      WHERE sp_entryno=" + entryNo + " and sp_str_id=3";
            DataTable dtSalesPur = usqlre.dbReaderFill(sql_str);
            usqlre.close();
            sql_sales_inf = @"select si_acc_id as id,si_entryno as entryNo,si_date as [Date],a.as_name as custName,a.as_add1 as add1,a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST, si_tax as GST, si_total as Total, si_other_charge as otherCharge, 
                            si_other_disc as otherDiscount, si_grand_total as grandTotal,si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_loc_entryno,
                            a.as_tin as gstin,s.as_name as salesman from inv_sales_inf left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_salesman_id=s.as_id where si_str_id=3 and si_entryno=" + entryNo + "";
            DataTable dtSalesInf = usqlre.dbReaderFill(sql_sales_inf);
            usqlre.close();
            bool LOCATIONENTRYNO = false;
            DataTable dt_locEntryNo = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='LOCATION ENTRY NO'");
            if (dt_locEntryNo.Rows.Count > 0)
                LOCATIONENTRYNO = Convert.ToBoolean(Convert.ToInt32(dt_locEntryNo.Rows[0][0].ToString()));

            entry_No = !LOCATIONENTRYNO ? dtSalesInf.Rows[0]["entryNo"].ToString() : dtSalesInf.Rows[0]["si_loc_entryno"].ToString();
            if (dtSalesInf.Rows[0]["id"].ToString() == 1.ToString())
            {
                addr1 = "";
                addr2 = "";
                mob = "";
                gstin = "";
            }
            else
            {
                addr1 = dtSalesInf.Rows[0]["add1"].ToString();
                addr2 = dtSalesInf.Rows[0]["add2"].ToString();
                mob = dtSalesInf.Rows[0]["mob"].ToString();
                gstin = dtSalesInf.Rows[0]["gstin"].ToString();
            }

            string responseString = @"<!DOCTYPE html>
<html lang='en'>
  <head>
    <meta charset='UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>'MICTCO</title>

    <style>
      @media print {
      html, body {
      width: 210mm;
      height: 99%;
     }
     }
      .wrap {
        margin: 0;
        padding: 0;
        display: flex;
        flex-wrap: wrap;
        flex-direction: column;
        border: 3px solid black;
        margin: 10px;
        font-size: 13px;
      }
     
      body {
        width: 100%;
        height: 100%;
        font-family: 'Poppins', sans-serif;
        font-size: 13px;
      }
      .head {
        margin: 0;
        text-align: center;
        font-size: large;
        font-weight: bold;
      }
      p {
        margin: 0;
        text-align: center;
        padding-top: 5px;
      }
      .gst {
        font-size: 15px;
        font-weight: bold;
      }
      .sub5 {
        border-bottom: 1px solid;
        width: 100%;
        text-align: center;
        font-size: 17px;
      }
      .left {
        float: left;
        font-weight: bold;
      }
      .head {
        font-weight: bold;
        text-align: center;
        font-size: medium;
      }
      .table-wrapper {
        width: 100%;
      }

    
      table {
        border-collapse: collapse;
        width: 100%;
        height: 100%;
        box-sizing: border-box;
      }
      .bill-tbl {
        margin-left: 15px;
      }
      .sub-th {
        padding: 0;
        border: 1px solid black;
        border-left: 0;
        padding-top: 13px;
        text-align: left;
        background-color: rgb(225, 221, 221);
        font-weight: lighter;
      }
      .table-th {
        padding: 0;
        border: 1px solid black;
        border-left: 0;
        /* text-align: left; */
        background-color: rgb(225, 221, 221);
        font-weight: lighter;
        padding: 0;
        font-size: 15px;
        padding-left: 2px;
      }
      .table-th:last-child {
        border-right: 0px solid black;
      }

      .main-table {
        border-right: 1px solid black;
        /* padding: 10px; */
        /* padding: 0; */
        padding-left: 5px;
      }

      td:last-child {
        border-right: 0px solid black;
      }
      .details {
        display: flex;
        flex-direction: row;
        justify-content: space-around;
        gap: 90px;
        padding-top: 10px;
      }
      .bill-bold {
        font-weight: bold;
      }
      .right-tbl {
        display: flex;
        justify-content: space-evenly;
        padding-left: 90px;
      }
      .right-tbl-padding {
        padding-left: 10px;
      }
      .total {
        border-top: 1px solid;
        border-bottom: 1px solid;
        margin: 10px;
        background-color: rgb(225, 221, 221);
      }
      .amt-words {
        display: flex;
      }
      .word-amt {
        font-size: 15px;
        font-weight: bold;
      }
      .last-table {
        display: flex;
        flex-direction: row;
        justify-content: space-between;

        width: 100%;
      }

     .footer-tbl {
        border: 1.5px solid;
        margin-left: 3.5%;
        margin-top: 10px;
        padding: 0;
        margin: 0;
      }
     .last-td{
        font-size: 12px;
      }
      .footer-left-2tabels {
        display: flex;
        flex-direction: column;
        width: 48%;
        height: 100%;
      }

      .footer-wrap {
        display: flex;
        flex-direction: row;
        justify-content: space-between;
      }
      .right-data-wrap {
        display: flex;
        align-content: center;
        justify-content: flex-end;
        align-items: center;
        width: 48%;
        padding-left: 140px;
      }

      .footer-total {
        background-color: rgb(225, 221, 221);
      }
      .font {
        font-size: 13px;
      }
      td {
        font-size: 13px;
      }
      .footer-data {
        border-right: 1px solid;
        text-align: center;
      }

      .last-tbl-wrap {
        width: 100%;
        margin-left: 3.5%;
        margin-top: 10px;
        border: 2px solid;
      }
      .signature {
        background-color: rgb(225, 221, 221);
        height: 40px;
        text-align: center;
      }
      .remarks-wrap {
        margin-left: 3.5%;
        border: 2px solid;
        width: 99%;
        margin-top: 10px;
      }
      .remark {
        border-bottom: 1px solid;
        font-weight: bold;
      }
      .null-data {
        border-bottom: 1px solid;
        height: 40px;
      }
      .data-wrap {
        border-top: 1px solid;
        border-bottom: 1px solid;
      }
      .null-data1 {
        padding: 7px;
        border-right: 1px solid;
      }

     .thanks {
        font-weight: bold;
        text-align: center;
        padding-top: 10px;      }
        @media print {
        html,
        body,
        .wrap {
          width: 210mm;
          height: 297mm;
        }
      }
    </style>
  </head>
  <body>
    <div class='wrap'>
      <!-- <div class='main'> -->";
            bool HIDECOMPANYDETAILS = false;
            DataTable dt_hide = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='HIDE COMPANY DETAILS FROM SALES-Q'");
            if (dt_hide.Rows.Count > 0)
                HIDECOMPANYDETAILS = Convert.ToBoolean(Convert.ToInt32(dt_hide.Rows[0][0].ToString()));
            if (!HIDECOMPANYDETAILS)
            {
                responseString += "<p class='head'>" + dt.Rows[0]["com_name"].ToString();
                responseString += "</p>";
                responseString += " <p>" + dt.Rows[0]["com_add1"].ToString() + "</p>" + "<p>" + dt.Rows[0]["com_add2"].ToString() + "</p>";
                responseString += "<p>" + dt.Rows[0]["com_mob"].ToString() + "</p>" + "<p class='gst'>GSTIN:" + dt.Rows[0]["com_gstin"].ToString() + "</p>";
            }

            //responseString += "<p class='head'>" + dt.Rows[0]["com_name"].ToString();
            //responseString += "</p>";
            //responseString += " <p>" + dt.Rows[0]["com_add1"].ToString() + "</p>" + "<p>" + dt.Rows[0]["com_add2"].ToString() + "</p>";
            //responseString += "<p>" + dt.Rows[0]["com_mob"].ToString() + "</p>" + "<p class='gst'>GSTIN:" + dt.Rows[0]["com_gstin"].ToString() + "</p>";
            responseString += "<div class='sub5'><p>SALES QUOTATION</p></div> <div class='details'><table class='bill-tbl'><tr> <td class='bill-bold'>BILL TO</td></tr>";
            responseString += "<tr><td class='zero'>" + dtSalesInf.Rows[0]["custName"].ToString() + "</td></tr><tr><td class='zero'>" + addr1 + "</td></tr><tr><td class='zero'>" + addr2 + "</td>";
            responseString += "<tr><td class='zero'>" + mob + "</td></tr><tr><td class='zero'>GSTIN:" + gstin + "</td></tr>";
            responseString += "</table><table class='right-tbl'><tr><td>Invoice No</td> <td style='font-weight: bold' class='right-tbl-padding'> : " + entry_No + " </td></tr>";
            responseString += "<tr><td>Date</td><td class='right-tbl-padding'>: " + dtSalesInf.Rows[0]["Date"].ToString() + "</td></tr><tr><td>Salesman</td><td class='right-tbl-padding'>:" + dtSalesInf.Rows[0]["salesman"].ToString() + "</td></tr></table></div>";
            responseString += "<div class='head'>DETAILS</div><div class='table-wrapper'> <table class='main-tbl-content'><thead><tr><th class='table-th' rowspan='2' style='text-align: right'>No</th>";
            responseString += "<th class='table-th' rowspan='2' style='text-align: left; width: 25%'>Description Of Goods </ th > ";
            responseString += "<th class='table-th'rowspan='2'style='text-align: center; padding-right: 4px'>HSN SAC</th><th class='table-th' rowspan='2' style='text-align: center'>Qty</th><th class='table-th' rowspan='2' style='text-align: center'> Unit</th> ";
            responseString += "<th class='table-th' rowspan='2' style='text-align: center; width: 50px'>Rate</th><th class='table-th' rowspan='2' style='text-align: center'>Net Amount</th>";
            responseString += "<th class='table-th' colspan='2' style='text-align: center'>CGST</th><th class='table-th' colspan='2' style='text-align: center'>SGST</th>";
            responseString += "<th class='table-th' rowspan='2' style='text-align: center'>Total Amount</th></tr>";
            responseString += "<tr><th class='sub-th' style='text-align: center'>%</th> <th class='sub-th' style='text-align: center'>Amount</th><th class='sub-th' style='text-align: center'>%</th><th class='sub-th' style='text-align: center'>Amount</th></tr></thead><tbody>";
            int cnt = 1;

            for (int i = 0; i < dtSalesPur.Rows.Count; i++)
            {

                responseString += "<tr>";
                responseString += "<td class='main-table' style='text-align: center'>" + cnt + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["description"].ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["hsnCode"].ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["qty"].ToString() + "</td>";
                if (dtCntUnit.Rows.Count > 0)
                    responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["uom"].ToString() + "</td>";
                else
                    responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["unit"].ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["netamt"].ToString())), _decimal).ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["cgstP"].ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["cgst"].ToString())), _decimal).ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["sgstP"].ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["sgst"].ToString())), _decimal).ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal).ToString() + "</td>";

                responseString += "</tr>";
                cnt++;
            }
            string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Total"].ToString())), _decimal).ToString()));
            responseString += "<tr class='total'><td class='main-table'></td><td class='main-table' style='text-align: center'>Total</td><td class='main-table'></td><td class='main-table'></td><td class='main-table'></td><td class='main-table' style='text-align: right'></td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString() + "</td>";
            responseString += "<td class='main-table'></td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["CGST"].ToString())), _decimal).ToString() + "</td><td class='main-table'></td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["SGST"].ToString())), _decimal).ToString() + "</td><td class='main-table' style='text-align: right'>" + dtSalesInf.Rows[0]["Total"].ToString() + "</td></tr></tbody></table></div>";
            responseString += "<div class='amt-words'><p>Amount in Words :<span class='word-amt'>" + amtInWord + " </span></p></div>";
            responseString += "<div><h3 style='margin - left: 10px; '>Remarks:" + dtSalesInf.Rows[0]["Remarks"].ToString() + "</h3></div>";
            responseString += @"<div class='footer-wrap'><div class='footer-left-2tabels'>
                             <table class='footer-tbl'>
                             <tbody >
                             <tr>
                             <td class='font' colspan='2'>
                                ELECTRONIC BANKING REFERENCE DETAILS<br/>
                             </td>
                             </tr>
                             <tr>
                             <td style = 'font - size: 12px;'>Bank Name</td>
                             <td>:" + dt.Rows[0]["com_bankac"].ToString() + "</td></tr>";
            responseString += "<tr><td class='last - td'>Branch</td><td>:</td></tr>";
            responseString += "<tr><td class='last - td''>A/c.No.</td><td>:" + dt.Rows[0]["com_bank_accno"].ToString() + "</td></tr>";
            responseString += "<tr><td class='last - td''>IFS Code</td><td>:" + dt.Rows[0]["com_bank_ifsc"].ToString() + "</td></tr></tbody> </table></div>";
            responseString += "<div class='right-data-wrap'><table class='f-right_tbl'><tr> <td>Net Amount</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString() + "</td></tr>";

            if (!HIDECOMPANYDETAILS)
            {
                responseString += "<tr><td>Total CGST</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["CGST"].ToString())), _decimal).ToString() + "</td></tr><tr><td>Total SGST</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["SGST"].ToString())), _decimal).ToString() + "</td></tr>";
                responseString += "<tr><td>Other Discount</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherDiscount"].ToString())), _decimal).ToString() + "</td></tr><tr><td>Other Charges</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherCharge"].ToString())), _decimal).ToString() + "</td></tr>";
            }
            responseString += "<tr><td>Freight Charges</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["freightCharge"].ToString())), _decimal).ToString() + "</td></tr>";
            responseString += "<tr class='footer-total'><td style='font-weight: bold'>Grand Total</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["grandTotal"].ToString())), _decimal).ToString() + "</td></tr>";
            if (!HIDECOMPANYDETAILS)
            {
                responseString += "<tr class='footer-total'><td style='font-weight: bold'>OB</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["ob"].ToString())), _decimal).ToString() + "</td></tr>";
            }
            responseString += "<tr class='footer-total'><td style='font-weight: bold'>Net Balance</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["netBalanace"].ToString())), _decimal).ToString() + "</td></tr><tr><td></td> </tr>";
            responseString += "<tr class='signature'><td><h3>Authorized Signatory</h3></td><td></td><td></td></tr> </table></div></div>";
            responseString += "<div><table><tr> <td class='thanks'>****** THANK YOU VISIT AGAIN ******</td></tr></table></div> </div> </ body></html>";

            return base.Content(responseString.ToString(), "text/html");
        }
        #endregion
        #region estimate
        [HttpGet("estimate-invoice/{entryNo}")]
        public async Task<IActionResult> estimateinvoice(string entryNo)
        // public async Task<IActionResult> testinvoice([FromBody] SaleModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            string sql_str = "";
            string sql_sales_inf = "";
            string addr1 = "";
            string addr2 = "";
            string gstin = "";
            string mob = "";

            sql = "SELECT com_name,com_add1,com_add2,com_add3,com_gstin,com_telephone,com_mob,com_bank_accno,com_bank_ifsc,com_bankac FROM gnl_company";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            DataTable dt_decimal = usqlre.dbReaderFill("select gs_value from gnl_settings where gs_name='DECIMAL'");
            int _decimal = Convert.ToInt32(dt_decimal.Rows[0][0].ToString());
            usqlre.close();

            //sql_str = "SELECT sp_id,sp_str_id,sp_entryno,sp_uniquecode,sp_ir_id,ISNULL(cast(sp_rate as nvarchar(100)),'0') sp_rate,ISNULL(cast(sp_realrate as nvarchar(100)),'0') sp_realrate,sp_qty,sp_fqty,ISNULL(cast(sp_gross_value as nvarchar(100)),'0') sp_gross_value,sp_disc_per,ISNULL(cast(sp_disc as nvarchar(100)),'0') sp_disc,ISNULL(cast(sp_real_disc as nvarchar(100)),'0') sp_real_disc,ISNULL(cast(sp_net_amount as nvarchar(100)),'0') sp_net_amount,ISNULL(cast(sp_tax as nvarchar(100)),'0') sp_tax,ISNULL(cast(sp_total as nvarchar(100)),'0') sp_total,ISNULL(cast(sp_profit as nvarchar(100)),'0') sp_profit,ISNULL(cast(sp_igst as nvarchar(100)),'0') sp_igst,ISNULL(cast(sp_cgst as nvarchar(100)),'0') sp_cgst,ISNULL(cast(sp_sgst as nvarchar(100)),'0') sp_sgst,ISNULL(cast(sp_mrp as nvarchar(100)),'0') sp_mrp,ISNULL(cast(sp_qty_multi_unit as nvarchar(100)),'0') sp_qty_multi_unit,ISNULL(cast(sp_srate_multiunit as nvarchar(100)),'0') sp_srate_multiunit,ISNULL(cast(sp_kfc as nvarchar(100)),'0') sp_kfc,ISNULL(cast(sp_prate as nvarchar(100)),'0') sp_prate,ISNULL(cast(sp_cost as nvarchar(100)),'0') sp_cost,sp_unit_multi,ir_name,ir_hsn_code,sp_lend_amount FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id WHERE sp_entryno=" +entryNo + " and sp_str_id=1";
            sql_str = "select ir_name as description,ir_hsn_code as hsnCode,sp_qty as qty,sp_rate as rate,sp_gross_value as gross,sp_net_amount as netAmt,sp_disc as discount,ir_cgst as cgstP,sp_cgst as cgst,ir_sgst as sgstP,sp_sgst as sgst,sp_total as total,u_name as unit FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id left join inv_unit on sp_unit_multi=u_id WHERE sp_entryno=" + entryNo + " and sp_str_id=2";
            DataTable dtSalesPur = usqlre.dbReaderFill(sql_str);
            usqlre.close();
            sql_sales_inf = @"select si_acc_id as id,si_date as [Date],a.as_name as custName,a.as_add1 as add1,a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST, si_tax as GST, si_total as Total, si_other_charge as otherCharge, 
                            si_other_disc as otherDiscount, si_grand_total as grandTotal,si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_cash_recieved as cashReceived,si_loc_entryno,
                            a.as_tin as gstin,s.as_name as salesman from inv_sales_inf left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_salesman_id=s.as_id where si_str_id=2 and si_entryno=" + entryNo + "";
            DataTable dtSalesInf = usqlre.dbReaderFill(sql_sales_inf);
            usqlre.close();

            if (dtSalesInf.Rows[0]["id"].ToString() == 1.ToString())
            {
                addr1 = "";
                addr2 = "";
                mob = "";
                gstin = "";
            }
            else
            {
                addr1 = dtSalesInf.Rows[0]["add1"].ToString();
                addr2 = dtSalesInf.Rows[0]["add2"].ToString();
                mob = dtSalesInf.Rows[0]["mob"].ToString();
                gstin = dtSalesInf.Rows[0]["gstin"].ToString();
            }
            string _date = dtSalesInf.Rows[0]["Date"].ToString();
            string responseString = @"<!DOCTYPE html>
<html lang='en'>
  <head>
    <meta charset='UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Estimate Bill</title>
    <style>
      * {
        margin: 0;
        padding: 0;
        font-size: 12px;
        /* width: 100%; */
      }
      .wrap {
        font-family: sans-serif;
        border: 2px solid black;
        margin: 10px;
      }
      @media print {
        html,
        body,
        .wrap {
          width: 210mm;
          height: 99%;
        }
      }
      .estimate {
        text-align: center;
        border-bottom: 1.5px solid;
        font-size: 20px;
      }
      .date {
        width: 100%;
        text-align: right;
        padding-right: 25px;
      }
      .head-details {
        display: flex;
        justify-content: space-between;
      }
      .invoice {
        padding-right: 25px;
      }
      .main-tbl {
        border-top: 1.5px solid;
        border-collapse: collapse;
        border-bottom: 1.5px solid;
      }
      .right-border {
        border-right: 1.5px solid;
      }
      .tbl-data {
        border-top: 1.5px solid;
      }
      .footer-tbl {
        display: grid;
        justify-content: end;
        padding-right: 25px;
        padding-top: 11px;
      }
      .foot-tot-amt {
        font-weight: bold;
      }
    </style>
  </head><body>
    <div class='wrap'>
      <div class='head'>
        <h1 class='estimate'>Estimate</h1>
      </div>
      <div>
     
        <div class='head-details'>
          <table>
            <tr>
              <td>Name</td>
              <td>:" + dtSalesInf.Rows[0]["custName"].ToString() + @"</td>
            </tr>
            <tr>
              <td>Mobile no</td>
              <td>:" + mob + @"</td>
            </tr>
          </table>
          <table>
            <tr>
            <td >Date</td>
            <td>:" + _date.Substring(0, 9) + @"</td>
          </tr>
            <tr>
              <td class='invoice'>Invoice</td>
              <td class='invoice'>:" + entryNo + @"</td>
            </tr>
            <tr>
              <td>Salesman</td>
              <td>:</td>
            </tr>
          </table>
        </div>
      </div>
      <div>
        <table class='main-tbl' style='width: 100%'>
          <tr>
            <th class='right-border' style='background-color: #e4e1e1'>Slno</th>
            <th class='right-border'style='background-color: #e4e1e1'>Item Name</th>
            <th class='right-border'style='background-color: #e4e1e1'>Qty</th>
            <th class='right-border'style='background-color: #e4e1e1'>Rate</th>
            <th style='background-color: #e4e1e1'>Total</th>
          </tr>";
            int cnt = 1;
            for (int i = 0; i < dtSalesPur.Rows.Count; i++)
            {
                responseString += @"<tr class='tbl-data'>
                    <td class='right-border' style='text-align: center'>" + cnt + @"</td>
                    <td class='right-border' style='text-align: center'>" + dtSalesPur.Rows[i]["description"].ToString() + @"</td>
                    <td class='right-border' style='text-align: center'>" + dtSalesPur.Rows[i]["qty"].ToString() + @"</td>
                    <td class='right-border' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString() + @"</td>
                    <td style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal).ToString() + @"</td>
                  </tr>";
                cnt++;
            }
            responseString += @"</table>
              </div>
              <div>
                <table class='footer-tbl'>
                  <tr>
                    <td class='foot-tot-amt'>Total Amount</td>
                    <td>:" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["grandTotal"].ToString())), _decimal).ToString() + @"</td>
                  </tr>
                  <tr>
                    <td style='padding-top: 10px'>Other Charges</td>
                    <td style='padding-top: 10px'>:" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherCharge"].ToString())), _decimal).ToString() + @"</td>
                  </tr>
                  <tr>
                    <td>Bill Amount</td>
                    <td>:" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["grandTotal"].ToString())), _decimal).ToString() + @"</td>
                  </tr>
                  <tr>
                    <td>OB</td>
                    <td>:" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["ob"].ToString())), _decimal).ToString() + @"</td>
                  </tr>
                    <tr>
                    <td>Cash Received</td>
                    <td>:" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["cashReceived"].ToString())), _decimal).ToString() + @"</td>
                  </tr>
                  <tr>
                    <td>Net Balance</td>
                    <td>:" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["netBalanace"].ToString())), _decimal).ToString() + @"</td>
                  </tr>
                </table>
              </div>
            </div>
          </body>
        </html>";
            return base.Content(responseString.ToString(), "text / html");
        }
        #endregion
        [HttpGet("sales-return/{entryNo}")]
        public async Task<IActionResult> salesReturn(string entryNo)
        // public async Task<IActionResult> testinvoice([FromBody] SaleModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            string sql_str = "";
            string sql_sales_inf = "";
            string addr1 = "";
            string addr2 = "";
            string gstin = "";
            string mob = "";
            string entry_No = "";
            sql = "SELECT com_name,com_add1,com_add2,com_add3,com_gstin,com_telephone,com_mob,com_bank_accno,com_bank_ifsc,com_bankac FROM gnl_company";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();

            string sqlunit = "";
            sqlunit = "select * from inv_multi_unit";
            DataTable dtCntUnit = usqlre.dbReaderFill(sqlunit);
            usqlre.close();

            DataTable dt_decimal = usqlre.dbReaderFill("select gs_value from gnl_settings where gs_name='DECIMAL'");
            int _decimal = Convert.ToInt32(dt_decimal.Rows[0][0].ToString());
            usqlre.close();
            //sql_str = "SELECT sp_id,sp_str_id,sp_entryno,sp_uniquecode,sp_ir_id,ISNULL(cast(sp_rate as nvarchar(100)),'0') sp_rate,ISNULL(cast(sp_realrate as nvarchar(100)),'0') sp_realrate,sp_qty,sp_fqty,ISNULL(cast(sp_gross_value as nvarchar(100)),'0') sp_gross_value,sp_disc_per,ISNULL(cast(sp_disc as nvarchar(100)),'0') sp_disc,ISNULL(cast(sp_real_disc as nvarchar(100)),'0') sp_real_disc,ISNULL(cast(sp_net_amount as nvarchar(100)),'0') sp_net_amount,ISNULL(cast(sp_tax as nvarchar(100)),'0') sp_tax,ISNULL(cast(sp_total as nvarchar(100)),'0') sp_total,ISNULL(cast(sp_profit as nvarchar(100)),'0') sp_profit,ISNULL(cast(sp_igst as nvarchar(100)),'0') sp_igst,ISNULL(cast(sp_cgst as nvarchar(100)),'0') sp_cgst,ISNULL(cast(sp_sgst as nvarchar(100)),'0') sp_sgst,ISNULL(cast(sp_mrp as nvarchar(100)),'0') sp_mrp,ISNULL(cast(sp_qty_multi_unit as nvarchar(100)),'0') sp_qty_multi_unit,ISNULL(cast(sp_srate_multiunit as nvarchar(100)),'0') sp_srate_multiunit,ISNULL(cast(sp_kfc as nvarchar(100)),'0') sp_kfc,ISNULL(cast(sp_prate as nvarchar(100)),'0') sp_prate,ISNULL(cast(sp_cost as nvarchar(100)),'0') sp_cost,sp_unit_multi,ir_name,ir_hsn_code,sp_lend_amount FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id WHERE sp_entryno=" +entryNo + " and sp_str_id=7";
            sql_str = @"select ir_name as description,ir_hsn_code as hsnCode,sp_qty as qty,sp_rate as rate,
                        sp_gross_value as gross,sp_net_amount as netAmt,sp_disc as discount,ir_cgst as cgstP,sp_cgst as cgst,ir_sgst as sgstP,
                        sp_sgst as sgst,sp_total as total,sp.u_name as uom,u.u_name as unit
                        FROM inv_sales_par 
                        left JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id 
                        left join inv_unit as sp on sp_unit_multi=sp.u_id 
                        left join inv_unit as u on ir_min_unit_id=u.u_id
                        WHERE sp_entryno=" + entryNo + " and sp_str_id=7";
            DataTable dtSalesPur = usqlre.dbReaderFill(sql_str);
            usqlre.close();
            sql_sales_inf = @"select si_acc_id as id,si_entryno as entryNo,si_date as [Date],a.as_name as custName,a.as_add1 as add1,a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST, si_tax as GST, si_total as Total, si_other_charge as otherCharge, 
                            si_other_disc as otherDiscount, si_grand_total as grandTotal,si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,si_loc_entryno,
                            a.as_tin as gstin,s.as_name as salesman from inv_sales_inf left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_salesman_id=s.as_id where si_str_id=7 and si_entryno=" + entryNo + "";
            DataTable dtSalesInf = usqlre.dbReaderFill(sql_sales_inf);
            usqlre.close();
            bool LOCATIONENTRYNO = false;
            DataTable dt_locEntryNo = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='LOCATION ENTRY NO'");
            if (dt_locEntryNo.Rows.Count > 0)
                LOCATIONENTRYNO = Convert.ToBoolean(Convert.ToInt32(dt_locEntryNo.Rows[0][0].ToString()));

            entry_No = !LOCATIONENTRYNO ? dtSalesInf.Rows[0]["entryNo"].ToString() : dtSalesInf.Rows[0]["si_loc_entryno"].ToString();
            if (dtSalesInf.Rows[0]["id"].ToString() == 1.ToString())
            {
                addr1 = "";
                addr2 = "";
                mob = "";
                gstin = "";
            }
            else
            {
                addr1 = dtSalesInf.Rows[0]["add1"].ToString();
                addr2 = dtSalesInf.Rows[0]["add2"].ToString();
                mob = dtSalesInf.Rows[0]["mob"].ToString();
                gstin = dtSalesInf.Rows[0]["gstin"].ToString();
            }

            string responseString = @"<!DOCTYPE html>
<html lang='en'>
  <head>
    <meta charset='UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>'MICTCO</title>

    <style>
      @media print {
      html, body {
      width: 210mm;
     height: 99%;
     }
     }
      .wrap {
        margin: 0;
        padding: 0;
        display: flex;
        flex-wrap: wrap;
        flex-direction: column;
        border: 3px solid black;
        margin: 10px;
        font-size: 13px;
      }
     
      body {
        width: 100%;
        height: 100%;
        font-family: 'Poppins', sans-serif;
        font-size: 13px;
      }
      .head {
        margin: 0;
        text-align: center;
        font-size: large;
        font-weight: bold;
      }
      p {
        margin: 0;
        text-align: center;
        padding-top: 5px;
      }
      .gst {
        font-size: 15px;
        font-weight: bold;
      }
      .sub5 {
        border-bottom: 1px solid;
        width: 100%;
        text-align: center;
        font-size: 17px;
      }
      .left {
        float: left;
        font-weight: bold;
      }
      .head {
        font-weight: bold;
        text-align: center;
        font-size: medium;
      }
      .table-wrapper {
        width: 100%;
      }

    
      table {
        border-collapse: collapse;
        width: 100%;
        height: 100%;
        box-sizing: border-box;
      }
      .bill-tbl {
        margin-left: 15px;
      }
      .sub-th {
        padding: 0;
        border: 1px solid black;
        border-left: 0;
        padding-top: 13px;
        text-align: left;
        background-color: rgb(225, 221, 221);
        font-weight: lighter;
      }
      .table-th {
        padding: 0;
        border: 1px solid black;
        border-left: 0;
        /* text-align: left; */
        background-color: rgb(225, 221, 221);
        font-weight: lighter;
        padding: 0;
        font-size: 15px;
        padding-left: 2px;
      }
      .table-th:last-child {
        border-right: 0px solid black;
      }

      .main-table {
        border-right: 1px solid black;
        /* padding: 10px; */
        /* padding: 0; */
        padding-left: 5px;
      }

      td:last-child {
        border-right: 0px solid black;
      }
      .details {
        display: flex;
        flex-direction: row;
        justify-content: space-around;
        gap: 90px;
        padding-top: 10px;
      }
      .bill-bold {
        font-weight: bold;
      }
      .right-tbl {
        display: flex;
        justify-content: space-evenly;
        padding-left: 90px;
      }
      .right-tbl-padding {
        padding-left: 10px;
      }
      .total {
        border-top: 1px solid;
        border-bottom: 1px solid;
        margin: 10px;
        background-color: rgb(225, 221, 221);
      }
      .amt-words {
        display: flex;
      }
      .word-amt {
        font-size: 15px;
        font-weight: bold;
      }
      .last-table {
        display: flex;
        flex-direction: row;
        justify-content: space-between;

        width: 100%;
      }

     .footer-tbl {
        border: 1.5px solid;
        margin-left: 3.5%;
        margin-top: 10px;
        padding: 0;
        margin: 0;
      }
     .last-td{
        font-size: 12px;
      }
      .footer-left-2tabels {
        display: flex;
        flex-direction: column;
        width: 48%;
        height: 100%;
      }

      .footer-wrap {
        display: flex;
        flex-direction: row;
        justify-content: space-between;
      }
      .right-data-wrap {
        display: flex;
        align-content: center;
        justify-content: flex-end;
        align-items: center;
        width: 48%;
        padding-left: 140px;
      }

      .footer-total {
        background-color: rgb(225, 221, 221);
      }
      .font {
        font-size: 13px;
      }
      td {
        font-size: 13px;
      }
      .footer-data {
        border-right: 1px solid;
        text-align: center;
      }

      .last-tbl-wrap {
        width: 100%;
        margin-left: 3.5%;
        margin-top: 10px;
        border: 2px solid;
      }
      .signature {
        background-color: rgb(225, 221, 221);
        height: 40px;
        text-align: center;
      }
      .remarks-wrap {
        margin-left: 3.5%;
        border: 2px solid;
        width: 99%;
        margin-top: 10px;
      }
      .remark {
        border-bottom: 1px solid;
        font-weight: bold;
      }
      .null-data {
        border-bottom: 1px solid;
        height: 40px;
      }
      .data-wrap {
        border-top: 1px solid;
        border-bottom: 1px solid;
      }
      .null-data1 {
        padding: 7px;
        border-right: 1px solid;
      }

     .thanks {
        font-weight: bold;
        text-align: center;
        padding-top: 10px;      }
        @media print {
        html,
        body,
        .wrap {
          width: 210mm;
          height: 297mm;
        }
      }
    </style>
  </head>
  <body>
    <div class='wrap'>
      <!-- <div class='main'> -->
      <p class='head'>" + dt.Rows[0]["com_name"].ToString();
            responseString += "</p>";
            responseString += " <p>" + dt.Rows[0]["com_add1"].ToString() + "</p>" + "<p>" + dt.Rows[0]["com_add2"].ToString() + "</p>";
            responseString += "<p>" + dt.Rows[0]["com_mob"].ToString() + "</p>" + "<p class='gst'>GSTIN:" + dt.Rows[0]["com_gstin"].ToString() + "</p>";
            responseString += "<div class='sub5'><p>SALES RETURN</p></div> <div class='details'><table class='bill-tbl'><tr> <td class='bill-bold'>BILL TO</td></tr>";
            responseString += "<tr><td class='zero'>" + dtSalesInf.Rows[0]["custName"].ToString() + "</td></tr><tr><td class='zero'>" + addr1 + "</td></tr><tr><td class='zero'>" + addr2 + "</td>";
            responseString += "<tr><td class='zero'>" + mob + "</td></tr><tr><td class='zero'>GSTIN:" + gstin + "</td></tr>";
            responseString += "</table><table class='right-tbl'><tr><td>Invoice No</td> <td style='font-weight: bold' class='right-tbl-padding'> : " + entry_No + " </td></tr>";
            responseString += "<tr><td>Date</td><td class='right-tbl-padding'>: " + dtSalesInf.Rows[0]["Date"].ToString() + "</td></tr><tr><td>Salesman</td><td class='right-tbl-padding'>:" + dtSalesInf.Rows[0]["salesman"].ToString() + "</td></tr></table></div>";
            responseString += "<div class='head'>DETAILS</div><div class='table-wrapper'> <table class='main-tbl-content'><thead><tr><th class='table-th' rowspan='2' style='text-align: right'>No</th>";
            responseString += "<th class='table-th' rowspan='2' style='text-align: left; width: 25%'>Description Of Goods </ th > ";
            responseString += "<th class='table-th'rowspan='2'style='text-align: center; padding-right: 4px'>HSN SAC</th><th class='table-th' rowspan='2' style='text-align: center'>Qty</th><th class='table-th' rowspan='2' style='text-align: center'> Unit</th> ";
            responseString += "<th class='table-th' rowspan='2' style='text-align: center; width: 50px'>Rate</th><th class='table-th' rowspan='2' style='text-align: center'>Net Amount</th>";
            responseString += "<th class='table-th' colspan='2' style='text-align: center'>CGST</th><th class='table-th' colspan='2' style='text-align: center'>SGST</th>";
            responseString += "<th class='table-th' rowspan='2' style='text-align: center'>Total Amount</th></tr>";
            responseString += "<tr><th class='sub-th' style='text-align: center'>%</th> <th class='sub-th' style='text-align: center'>Amount</th><th class='sub-th' style='text-align: center'>%</th><th class='sub-th' style='text-align: center'>Amount</th></tr></thead><tbody>";
            int cnt = 1;
            double rateSum = 0.00;
            double netSum = 0.00;
            double cgstSum = 0.00;
            double sgstSum = 0.00;
            double totalSum = 0.00;
            for (int i = 0; i < dtSalesPur.Rows.Count; i++)
            {
                responseString += "<tr>";
                responseString += "<td class='main-table' style='text-align: center'>" + cnt + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["description"].ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["hsnCode"].ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["qty"].ToString() + "</td>";
                if (dtCntUnit.Rows.Count > 0)
                    responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["uom"].ToString() + "</td>";
                else
                    responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["unit"].ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["netamt"].ToString())), _decimal).ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["cgstP"].ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["cgst"].ToString())), _decimal).ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + dtSalesPur.Rows[i]["sgstP"].ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["sgst"].ToString())), _decimal).ToString() + "</td>";
                responseString += "<td class='main-table' style='text-align: left'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal).ToString() + "</td>";

                responseString += "</tr>";
                cnt++;
            }
            string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Total"].ToString())), _decimal).ToString()));
            responseString += "<tr class='total'><td class='main-table'></td><td class='main-table' style='text-align: center'>Total</td><td class='main-table'></td><td class='main-table'></td><td class='main-table'></td><td class='main-table' style='text-align: right'></td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString() + "</td>";
            responseString += "<td class='main-table'></td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["CGST"].ToString())), _decimal).ToString() + "</td><td class='main-table'></td><td class='main-table' style='text-align: right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["SGST"].ToString())), _decimal).ToString() + "</td><td class='main-table' style='text-align: right'>" + dtSalesInf.Rows[0]["Total"].ToString() + "</td></tr></tbody></table></div>";
            responseString += "<div class='amt-words'><p>Amount in Words :<span class='word-amt'>" + amtInWord + " </span></p></div>";
            responseString += "<div><h3 style='margin - left: 10px; '>Remarks:" + dtSalesInf.Rows[0]["Remarks"].ToString() + "</h3></div>";
            responseString += @"<div class='footer-wrap'><div class='footer-left-2tabels'>
                             <table class='footer-tbl'>
                             <tbody >
                             <tr>
                             <td class='font' colspan='2'>
                                ELECTRONIC BANKING REFERENCE DETAILS<br/>
                             </td>
                             </tr>
                             <tr>
                             <td style = 'font - size: 12px;'>Bank Name</td>
                             <td>:" + dt.Rows[0]["com_bankac"].ToString() + "</td></tr>";
            responseString += "<tr><td class='last - td'>Branch</td><td>:</td></tr>";
            responseString += "<tr><td class='last - td''>A/c.No.</td><td>:" + dt.Rows[0]["com_bank_accno"].ToString() + "</td></tr>";
            responseString += "<tr><td class='last - td''>IFS Code</td><td>:" + dt.Rows[0]["com_bank_ifsc"].ToString() + "</td></tr></tbody> </table></div>";
            responseString += "<div class='right-data-wrap'><table class='f-right_tbl'><tr> <td>Net Amount</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString() + "</td></tr>";
            responseString += "<tr><td>Total CGST</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["CGST"].ToString())), _decimal).ToString() + "</td></tr><tr><td>Total SGST</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["SGST"].ToString())), _decimal).ToString() + "</td></tr>";
            responseString += "<tr><td>Other Discount</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherDiscount"].ToString())), _decimal).ToString() + "</td></tr><tr><td>Other Charges</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherCharge"].ToString())), _decimal).ToString() + "</td></tr>";
            responseString += "<tr><td>Freight Charges</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["freightCharge"].ToString())), _decimal).ToString() + "</td></tr>";
            responseString += "<tr class='footer-total'><td style='font-weight: bold'>Grand Total</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["grandTotal"].ToString())), _decimal).ToString() + "</td></tr>";
            responseString += "<tr class='footer-total'><td style='font-weight: bold'>OB</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["ob"].ToString())), _decimal).ToString() + "</td></tr>";
            responseString += "<tr class='footer-total'><td style='font-weight: bold'>Net Balance</td><td>:</td><td>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["netBalanace"].ToString())), _decimal).ToString() + "</td></tr><tr><td></td> </tr>";
            responseString += "<tr class='signature'><td><h3>Authorized Signatory</h3></td><td></td><td></td></tr> </table></div></div>";
            responseString += "<div><table><tr> <td class='thanks'>****** THANK YOU VISIT AGAIN ******</td></tr></table></div> </div> </ body></html>";
            return base.Content(responseString.ToString(), "text/html");
        }
        [HttpGet("test-invoice/{entryNo}")]
        public async Task<IActionResult> testinvoice(string entryNo)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            string sql_str = "";
            string sql_sales_inf = "";
            string addr1 = "";
            string addr2 = "";
            string gstin = "";
            string mob = "";
            sql = "SELECT * FROM gnl_company";
            DataTable dtCompany = usqlre.dbReaderFill(sql);
            usqlre.close();

            sql_str = "select ir_name as description,ir_hsn_code as hsnCode,sp_qty as qty,sp_rate as rate,sp_gross_value as gross,sp_net_amount as netAmt,sp_disc as discount,ir_cgst as cgstP,sp_cgst as cgst,ir_sgst as sgstP,sp_sgst as sgst,ir_taxper TaxP,sp_tax as gst,sp_total as total,u_name as unit FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id left join inv_unit on sp_unit_multi=u_id WHERE sp_entryno=" + entryNo + " and sp_str_id=1";
            DataTable dtSalesPur = usqlre.dbReaderFill(sql_str);
            usqlre.close();
            sql_sales_inf = @"select  si_entryno as entryNo,si_acc_id as id,si_date as [Date],si_deliverydate as deliveryDate,
                            si_disc as disc, si_gross_value as gross,si_net_amount as net,si_cgst_total as cgst,si_sgst_total as sgst,
                            si_tax as gst, si_total as Total, si_other_charge as otherCharge,si_other_disc as otherDiscount,
                            si_grand_total as grandTotal,si_remarks as remarks,si_freight_charge as freightCharge,si_ob as ob,
                            si_net_balance as netBalance,a.as_name as custName,a.as_add1 as add1,a.as_add2 as add2,a.as_add3 as add3,
                            a.as_mob as mob,a.as_tin as gstin,a.as_state as state,a.as_state_code as stateCode,s.as_name as salesman ,si_einvoice_ksa,si_cash_recieved as cashReceived
                            from inv_sales_inf left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_salesman_id=s.as_id 
                            where si_str_id=1 and si_entryno=" + entryNo + "";
            DataTable dtSalesInf = usqlre.dbReaderFill(sql_sales_inf);
            usqlre.close();
            if (dtSalesInf.Rows[0]["id"].ToString() == 1.ToString())
            {
                addr1 = "";
                addr2 = "";
                mob = "";
                gstin = "";
            }
            else
            {
                addr1 = dtSalesInf.Rows[0]["add1"].ToString();
                addr2 = dtSalesInf.Rows[0]["add2"].ToString();
                mob = dtSalesInf.Rows[0]["mob"].ToString();
                gstin = dtSalesInf.Rows[0]["gstin"].ToString();
            }
            string responseString = @"<!DOCTYPE html>
<html lang='en'>
  <head>
    <meta charset='UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Bill</title>
    <style>
      body {
        width: 100%;
        margin-left: 20px;
      }
      * {
        font-family: Arial, Helvetica, sans-serif;
        margin: 0;
        padding: 0;
        border: 0;
        outline: 0;
        font-size: 100%;
        vertical-align: baseline;
        background: transparent;
      }
      p {
        margin: 0;
      }
​
      @page {
        size: A4;
        margin: 0;
      }
      @media print {
        html,
        body {
          width: 210mm;
          height: 99%;
        }
        .header-wrap {
          color: rgb(7, 146, 99);
        }
        .tbl-head{
            background-color: rgb(168, 169, 170);
        }
      }
      .container {
        width: 98%;
      }
      .header-wrap {
        display: flex;
        justify-content: space-between;
        color: rgb(7, 146, 99);
      }
      .header-wrap h1 {
        /* line-height: 2; */
        font-size: 20px;
      }
      .header {
        text-align: center;
        margin-top: 50px;
        margin-bottom: 20px;
      }
      .head-data,
      p {
        font-weight: 600;
        font-size: 14px;

        /* line-height: 2; */
      }
      .head-right-data {
        /* line-height: 2; */
        font-size: 20px;
      }
      .header-tbl {
        display: flex;
        justify-content: space-between;
      }
      table {
        width: 100%;
      }
      .details-tbl {
        display: flex;
        width: 100%;
        flex-direction: row;
        justify-content: space-between;
      }
      .customer-tbl {
        width: 54%;
      }
      .payment-det-tbl {
        width: 45%;
      }
      .customer-det {
        border: 1px solid black;
        border-radius: 20px;
        height: 10rem;
        padding-left: 10px;
        padding-top: 10px;
      }
      .vat-det {
        border: 1px solid black;
        border-radius: 15px;
        height: 1rem;
        padding-left: 10px;
        padding-top: 5px;
      }
      .main-tbl {
        border: 1px solid black;
        margin-top: 50px;
        border-collapse: collapse;
      }
      .item-tbl,
      .tbl-head,
      .tbl-data {
        border: 1px solid black;
        border-collapse: collapse;
        padding: 6px;
      }
      .tbl-head {
        height: 3rem;
        background-color: rgb(168, 169, 170);
        text-align: center;
        padding-top: 20px;
      }
      .foot-total {
        display: flex;
        flex-direction: row;
        justify-content: space-between;
      }
      .amt-in-words {
        border: 1px solid black;
        border-radius: 6px;
        height: 2rem;
      }
      .foot-amt {
        width: 6rem;
        border: 1px solid black;
        border-radius: 5px;
        text-align: center;
        vertical-align: middle;
      }
      .date {
        font-size: 20px;
        font-weight: 600;
      }
      .invoice p {
        line-height: 0;
      }
      .amt-in-arab {
        font-weight: bold;
      }
      .foot-right-data,
      p {
        text-align: right;
        padding-right: 10px;
      }
    </style>
  </head>
  <body>
    <div class='container'>
      <div class='header-wrap'>
        <div>
          <h1>" + dtCompany.Rows[0]["com_name"].ToString() + @"</h1>
          <p class='head-data' style='text-align: center'>
            " + dtCompany.Rows[0]["com_add1"].ToString() + @"
          </p>
          <p style='text-align: center'>" + dtCompany.Rows[0]["com_add2"].ToString() + @"</p>
        </div>
        <div>
          <h1 class='head-right-data'>
            " + dtCompany.Rows[0]["com_ar_name"].ToString() + @"
          </h1>
          <p style='text-align: center'>" + dtCompany.Rows[0]["com_ar_add1"].ToString() + @" : </p>
          <p style='text-align: center'>" + dtCompany.Rows[0]["com_ar_add2"].ToString() + @"</p>
        </div>
      </div>
      <div>
        <h2 class='header'>SIMPLIFIED TAX INVOICE - فاتورة ضريبية مبسطة</h2>
      </div>
      <div class='header-tbl'>
        <div>
          <table>
            <tr>
              <td style='font-weight: bold'>DATE</td>
              <td class='date'>: " + dtSalesInf.Rows[0]["Date"].ToString() + @"</td>
              <td style='font-weight: bold; padding-left: 15px'>: تاریخ </td>
            </tr>
            <tr>
              <td style='font-weight: bold'>REP</td>
              <td style='font-weight: bold'>:" + dtSalesInf.Rows[0]["salesman"].ToString() + @"</td>
              <td style='font-weight: bold; padding-left: 15px'>: مندوب</td>
            </tr>
          </table>
        </div>
        <div class='invoice'>
          <p style='font-size: 1.2em'>
            INVOICE : " + dtSalesInf.Rows[0]["entryNo"].ToString() + @"
            <span style='margin-left: 5px'>: رقم الفاتورة</span>
          </p>
        </div>
      </div>
      <div class='details-tbl'>
        <div class='customer-tbl'>
          <table class='customer-det'>
            <tr>
              <th style='text-align: left'>CUSTOMER CODE</th>
              <td>:</td>
              <td>: كود العميل</td>
            </tr>
            <tr>
              <th style='text-align: left'>CUSTOMER NAME</th>
              <td>
                :
                <span style='font-weight: bold'
                  >" + dtSalesInf.Rows[0]["custName"].ToString() + @"</span
                >
              </td>
              <td>: اسم الزبون</td>
            </tr>
            <tr>
              <th style='text-align: left'>ADDRESS</th>
              <td>:" + addr1 + @"</td>
              <td>: العنوان</td>
            </tr>
            <tr>
              <th style='text-align: left'>VAT#</th>
              <td>:" + gstin + @"</td>
              <td>: رقم الضريبي</td>
            </tr>
          </table>
        </div>
        <div class='payment-det-tbl'>
          <table class='vat-det'>
            <tr>
              <td>VAT-NO</td>
              <td>: " + dtCompany.Rows[0]["com_gstin"].ToString() + @"</td>
              <td>: رقم الضريبي</td>
            </tr>
          </table>
          <table style='height: 10rem'>
            <tr>
              <td style='padding-left: 10px'>P.O NO</td>
              <td>:</td>
              <td>: رقم الضريبي</td>
            </tr>
            <tr>
              <td style='padding-left: 10px'>PAYMENT MODE</td>
              <td>:</td>
              <td>:نوع البيع</td>
            </tr>
            <tr>
              <td style='padding-left: 10px'>DELIVERY NOTE</td>
              <td>:</td>
              <td>:سند <br />الاستلام</td>
            </tr>
          </table>
        </div>
      </div>
      <div class='main-tbl'>
        <table class='item-tbl' style='width: 100%'>
          <tr>
            <th class='tbl-head'>#</th>
            <th class='tbl-head' style='text-align: left'>
              البيان<br />Item Name
            </th>
            <th class='tbl-head'>الكمية<br />QTY</th>
            <th class='tbl-head'>سعر الوحدة<br />Unit Price</th>
            <th class='tbl-head'>جمالي بدون ضريبة<br />Total without VAT</th>
            <th class='tbl-head'>الضريبة<br />VAT</th>
            <th class='tbl-head'>الضريبة<br />Total with VAT</th>
          </tr>";
            int cnt = 1;
            for (int i = 0; i < dtSalesPur.Rows.Count; i++)
            {
                responseString += "<tr>";
                responseString += "<td class='tbl-data'>" + cnt + "</td>";
                responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["description"].ToString() + "</td>";
                responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["qty"].ToString() + "</td>";
                responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["rate"].ToString() + "</td>";
                responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["netAmt"].ToString() + "</td>";
                responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["gst"].ToString() + "</td>";
                responseString += "<td class='tbl-data'>" + dtSalesPur.Rows[i]["total"].ToString() + "</td>";

                cnt = cnt + 1;
            }
            string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(dtSalesInf.Rows[0]["Total"].ToString()));
            responseString += @"</table>
      </div>
      <div>
        <table style='margin-top: 20px'>
          <tr>
            <td rowspan='3'><img src='https://chart.googleapis.com/chart?cht=qr&chl=" + dtSalesInf.Rows[0]["si_einvoice_ksa"] + "&chs=160x160&chld=L|0' class='qr-code img-thumbnail img-responsive'' />";
            responseString += @"</td>
            <td>
              <div class='foot-total'>
                <p>TOTAL IN WORDS</p>
                <p>المجموع في الكلمات</p>
              </div>
            </td>
            <td style='line-height: 1.8'>
              <p>المبلغ الإجمالي</p>
              <p>GROSS AMOUNT</p>
            </td>
            <td class='foot-amt'>" + dtSalesInf.Rows[0]["gross"].ToString() + @" <b> ريال </b></td>
          </tr>
          <tr>
            <td style='line-height: 1.8'>
              <div class='amt-in-words'>
                <p style='text-align: center'>
                  " + amtInWord + @"
                </p>
              </div>
            </td>
            <td class='foot-right-data' style='line-height: 1.8'>
              <p>ضريبة القيمة المضافة</p>
              <p>VAT AMOUNT</p>
            </td>
            <td class='foot-amt'>" + dtSalesInf.Rows[0]["gst"].ToString() + @" <b> ريال </b></td>
          </tr>
          <tr>
            <td></td>
            <td style='line-height: 1.8'>
              <p>المجموع الكلي</p>
              <p>GRAND TOTAL</p>
            </td>
            <td class='foot-amt'>" + dtSalesInf.Rows[0]["grandTotal"].ToString() + @"<b>ريال </b></td>
          </tr>
        </table>
      </div>
    </div>
  </body>
</html>";
            return base.Content(responseString.ToString(), "text/html");
        }
        [HttpGet("job-card/{entryNo}")]
        public async Task<IActionResult> salesquotation(string entryNo)
        {

            UserSqlServer usqlre = new UserSqlServer(this);
            string responseString = "";
            string sql = "";
            string sql_str = "";
            string sql_sales_inf = "";
            string addr1 = "";
            string addr2 = "";
            string gstin = "";
            string mob = "";
            string entry_No = "";
            string footerPageBreak = "";

            DataTable dt_decimal = usqlre.dbReaderFill("select gs_value from gnl_settings where gs_name='DECIMAL'");
            int _decimal = Convert.ToInt32(dt_decimal.Rows[0][0].ToString());
            usqlre.close();

            sql = "SELECT *,(SELECT 1 FROM sys.columns WHERE Name = N'com_logo' AND Object_ID = Object_ID(N'gnl_company')) as comLogo FROM gnl_company";
            DataTable dt_company = usqlre.dbReaderFill(sql);
            usqlre.close();

            //sql_str = "SELECT sp_id,sp_str_id,sp_entryno,sp_uniquecode,sp_ir_id,ISNULL(cast(sp_rate as nvarchar(100)),'0') sp_rate,ISNULL(cast(sp_realrate as nvarchar(100)),'0') sp_realrate,sp_qty,sp_fqty,ISNULL(cast(sp_gross_value as nvarchar(100)),'0') sp_gross_value,sp_disc_per,ISNULL(cast(sp_disc as nvarchar(100)),'0') sp_disc,ISNULL(cast(sp_real_disc as nvarchar(100)),'0') sp_real_disc,ISNULL(cast(sp_net_amount as nvarchar(100)),'0') sp_net_amount,ISNULL(cast(sp_tax as nvarchar(100)),'0') sp_tax,ISNULL(cast(sp_total as nvarchar(100)),'0') sp_total,ISNULL(cast(sp_profit as nvarchar(100)),'0') sp_profit,ISNULL(cast(sp_igst as nvarchar(100)),'0') sp_igst,ISNULL(cast(sp_cgst as nvarchar(100)),'0') sp_cgst,ISNULL(cast(sp_sgst as nvarchar(100)),'0') sp_sgst,ISNULL(cast(sp_mrp as nvarchar(100)),'0') sp_mrp,ISNULL(cast(sp_qty_multi_unit as nvarchar(100)),'0') sp_qty_multi_unit,ISNULL(cast(sp_srate_multiunit as nvarchar(100)),'0') sp_srate_multiunit,ISNULL(cast(sp_kfc as nvarchar(100)),'0') sp_kfc,ISNULL(cast(sp_prate as nvarchar(100)),'0') sp_prate,ISNULL(cast(sp_cost as nvarchar(100)),'0') sp_cost,sp_unit_multi,ir_name,ir_hsn_code,sp_lend_amount FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id WHERE sp_entryno=" +entryNo + " and sp_str_id=1";
            sql_str = "select ir_name as description,ir_hsn_code as hsnCode,sp_qty as qty,sp_rate as rate,sp_gross_value as gross,sp_net_amount as netAmt,sp_disc as discount,ir_cgst as cgstP,sp_cgst as cgst,ir_sgst as sgstP,sp_sgst as sgst,ir_taxper as TaxP,sp_tax as GST,sp_total as total,u_name as unit FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id left join inv_unit on sp_unit_multi=u_id WHERE sp_entryno=" + entryNo + " and sp_str_id=3";
            DataTable dtSalesPur = usqlre.dbReaderFill(sql_str);
            usqlre.close();
            sql_sales_inf = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],right(convert(varchar(20),si_date,100),7) [time],si_cust_name as custName,si_add1 as add1,si_add2 as add2,a.as_add3 as add3,a.as_mob as mob,si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST, si_tax as GST, si_total as Total, si_other_charge as otherCharge, 
                            si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,company_name,bra_name,clr_name,irc_name as receivingCondition,isr_service_name,si_loc_entryno,si_imei as imei,right(convert(varchar(20),si_expected_date,100),7) [ExpectedTime],CONVERT(varchar,si_expected_date,6) as expectedDate,
                            a.as_tin as gstin,s.as_name as salesman,si_einvoice_ksa,si_items_collected from inv_sales_inf left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_salesman_id=s.as_id left join inv_company on CONVERT(nvarchar(50), company_id) = si_company
                            left join inv_brand on CONVERT(nvarchar(50), bra_id) = si_model
                            left join inv_color on clr_id = si_color
                            left join inv_receiving_condition_reg on si_rc_id = irc_id
                            left join inv_service_reg on isr_id = si_salesman_id where si_str_id=3 and si_entryno=" + entryNo + "";
            DataTable dtSalesInf = usqlre.dbReaderFill(sql_sales_inf);
            usqlre.close();
            bool LOCATIONENTRYNO = false;
            DataTable dt_locEntryNo = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='LOCATION ENTRY NO'");
            if (dt_locEntryNo.Rows.Count > 0)
                LOCATIONENTRYNO = Convert.ToBoolean(Convert.ToInt32(dt_locEntryNo.Rows[0][0].ToString()));

            entry_No = !LOCATIONENTRYNO ? dtSalesInf.Rows[0]["entryNo"].ToString() : dtSalesInf.Rows[0]["si_loc_entryno"].ToString();
            //String[] strArray = null;
            string itemCollect = dtSalesInf.Rows[0]["SI_ITEMS_COLLECTED"].ToString();
            String[] itemCollectArray = itemCollect.Split(",");

            addr1 = dtSalesInf.Rows[0]["add1"].ToString();
            addr2 = dtSalesInf.Rows[0]["add2"].ToString();
            mob = dtSalesInf.Rows[0]["mob"].ToString();
            gstin = dtSalesInf.Rows[0]["gstin"].ToString();

            string sql_complaints = @"select li_ir_id as complaintId,
                                ir_name as complaint,
                                ift_name as fixType,
                                li_ir_mrp as amount,
                                li_ift_id as fixTypeId,
                                li_remarks as remarks
                            from inv_lend_item_transactions
                                left join inv_item_reg on ir_id = li_ir_id
                                left join inv_fix_type_reg on ift_id = li_ift_id
                            where li_entryno =" + dtSalesInf.Rows[0]["entryNo"] + "";
            DataTable dtComplaints = usqlre.dbReaderFill(sql_complaints);
            usqlre.close();

            string sql_collectedItem = @"select iic_id,iic_name from inv_items_collected where iic_status=1";
            DataTable dtCollected = usqlre.dbReaderFill(sql_collectedItem);
            usqlre.close();
            DataTable dt_print = usqlre.dbReaderFill("select ps_version from gnl_print_setup where ps_form='JOB-CARD-MOB'");
            var html = "";
            if (dtComplaints.Rows.Count > 5)
            {
                footerPageBreak = "<div class='page-break'></div>";
            }
            if (dt_print.Rows.Count > 0)
            {
                html = System.IO.File.ReadAllText(@"./assets/" + dt_print.Rows[0][0].ToString() + "");
                #region header
                html = html.Replace("{{com_name}}", dt_company.Rows[0]["com_name"].ToString());
                html = html.Replace("{{com_add1}}", dt_company.Rows[0]["com_add1"].ToString());
                html = html.Replace("{{com_add2}}", dt_company.Rows[0]["com_add2"].ToString());
                html = html.Replace("{{com_add3}}", dt_company.Rows[0]["com_add3"].ToString());
                html = html.Replace("{{com_telephone}}", dt_company.Rows[0]["com_telephone"].ToString());
                html = html.Replace("{{com_mob}}", dt_company.Rows[0]["com_mob"].ToString());
                html = html.Replace("{{com_ar_name}}", dt_company.Rows[0]["com_ar_name"].ToString());
                html = html.Replace("{{com_ar_add1}}", dt_company.Rows[0]["com_ar_add1"].ToString());
                html = html.Replace("{{com_ar_add2}}", dt_company.Rows[0]["com_ar_add2"].ToString());
                html = html.Replace("{{com_ar_add3}}", dt_company.Rows[0]["com_ar_add3"].ToString());
                html = html.Replace("{{com_ar_telephone}}", dt_company.Rows[0]["com_telephone"].ToString());
                html = html.Replace("{{com_ar_mob}}", dt_company.Rows[0]["com_ar_mob"].ToString());
                #endregion
                html = html.Replace("{{cName}}", dtSalesInf.Rows[0]["custName"].ToString());
                html = html.Replace("{{cPhone}}", addr2.ToString());
                html = html.Replace("{{jobNo}}", entry_No.ToString());
                html = html.Replace("{{time}}", dtSalesInf.Rows[0]["time"].ToString());
                html = html.Replace("{{date}}", dtSalesInf.Rows[0]["Date"].ToString());
                html = html.Replace("{{vat}}", gstin.ToString());
                html = html.Replace("{{brand}}", dtSalesInf.Rows[0]["company_name"].ToString());
                html = html.Replace("{{model}}", dtSalesInf.Rows[0]["bra_name"].ToString());
                html = html.Replace("{{color}}", dtSalesInf.Rows[0]["clr_name"].ToString());
                html = html.Replace("{{imei}}", dtSalesInf.Rows[0]["imei"].ToString());
                html = html.Replace("{{serviceType}}", dtSalesInf.Rows[0]["isr_service_name"].ToString());
                html = html.Replace("{{receivingCondition}}", dtSalesInf.Rows[0]["receivingCondition"].ToString());
                html = html.Replace("{{footerPageBreak}}", footerPageBreak);
                String complaints = "";
                double estimatedCost = 0.00;
                int cmplntcnt = 0, remarkcnt = 0, fixcnt = 0, max = 0, max1 = 0;
                for (int i = 0; i < dtComplaints.Rows.Count; i++)
                {
                    if (dtComplaints.Rows[i]["complaint"].ToString().Length / 22 > 0)
                        cmplntcnt += Convert.ToInt32(System.Math.Round(Convert.ToDecimal(dtComplaints.Rows[i]["complaint"].ToString().Length / 22)));//11
                    if (dtComplaints.Rows[i]["remarks"].ToString().Length > 26)
                        remarkcnt += Convert.ToInt32(System.Math.Round(Convert.ToDecimal(dtComplaints.Rows[i]["remarks"].ToString().Length / 26)));//13
                    if (dtComplaints.Rows[i]["fixType"].ToString().Length > 12)
                        fixcnt += Convert.ToInt32(System.Math.Round(Convert.ToDecimal(dtComplaints.Rows[i]["fixType"].ToString().Length / 12)));//9
                    max1 = cmplntcnt > remarkcnt ? cmplntcnt : remarkcnt;
                    max = max1 > fixcnt ? max1 : fixcnt;

                    complaints += @"<div class='cus-body-tr'>
                    <div style='width: 42px' class='cus-td'>" + (i + 1) + @"</div>
                <div style='width: 147.34px' class='cus-td'>" + dtComplaints.Rows[i]["complaint"].ToString() + @"</div>
                <div style='width: 84.66px' class='cus-td'>" + dtComplaints.Rows[i]["remarks"].ToString() + @"</div>
                <div style='width: 84.66px' class='cus-td'>" + dtComplaints.Rows[i]["fixType"].ToString() + @"</div> 
                <div style='width: 63px' class='cus-td'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtComplaints.Rows[i]["amount"].ToString())), _decimal).ToString() + @"</div></div>";
                    double cost = decimal.ToDouble(System.Math.Round((CommonHelper.GetTextboxValue(dtComplaints.Rows[i]["amount"].ToString())), _decimal));
                    estimatedCost += cost;

                }
                if (dtComplaints.Rows.Count <= 4 && dtComplaints.Rows.Count >= 0)
                {
                    if (dtComplaints.Rows.Count + max < 12)
                    {
                        int linecnt = 11 - dtComplaints.Rows.Count - max;
                        for (int l = 0; l < linecnt; l++)
                        {
                            complaints += @"<div class='cus-body-tr-dummy'>
                                <div style='width: 42px' class='cus-td-dummy'>0</div>
                                <div style = 'width: 147.34px' class='cus-td-dummy'></div>
                                <div style = 'width: 84.66px' class='cus-td-dummy'></div>
                                <div style = 'width: 84.66px' class='cus-td-dummy'></div> 
                                <div style = 'width: 63px' class='cus-td-dummy'></div></div>";
                        }
                    }
                }
                else
                {
                    if (dtComplaints.Rows.Count + max < 13)
                    {
                        int linecnt = 12 - dtComplaints.Rows.Count - max;
                        for (int l = 0; l < linecnt; l++)
                        {
                            complaints += @"<div class='cus-body-tr-dummy'>
                                <div style='width: 42px' class='cus-td-dummy'>0</div>
                                <div style = 'width: 147.34px' class='cus-td-dummy'></div>
                                <div style = 'width: 84.66px' class='cus-td-dummy'></div>
                                <div style = 'width: 84.66px' class='cus-td-dummy'></div> 
                                <div style = 'width: 63px' class='cus-td-dummy'></div></div>";
                        }


                    }
                }

                html = html.Replace("{{complaints}}", complaints);
                html = html.Replace("{{remark}}", dtSalesInf.Rows[0]["Remarks"].ToString());
                if (estimatedCost > 0)
                {
                    html = html.Replace("{{cost}}", System.Math.Round((CommonHelper.GetTextboxValue(estimatedCost.ToString())), _decimal).ToString());
                }
                else
                {
                    html = html.Replace("{{cost}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["otherCharge"].ToString())), _decimal).ToString());
                }
                html = html.Replace("{{dDate}}", dtSalesInf.Rows[0]["expectedDate"].ToString());
                html = html.Replace("{{dTime}}", dtSalesInf.Rows[0]["ExpectedTime"].ToString());
                return base.Content(html.ToString(), "text/html");
            }
            else
            {
                string model = salesquotationTTT(entryNo);
                return base.Content(model, "text/html");
            }

            // var html = System.IO.File.ReadAllText(@"./assets/jobCardV1.html");

        }

        [HttpGet("job-card-test/{entryNo}")]
        public string salesquotationTTT(string entryNo)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string responseString = "";
            string sql = "";
            string sql_str = "";
            string sql_sales_inf = "";
            string addr1 = "";
            string addr2 = "";
            string gstin = "";
            string mob = "";
            string entry_No = "";

            DataTable dt_decimal = usqlre.dbReaderFill("select gs_value from gnl_settings where gs_name='DECIMAL'");
            int _decimal = Convert.ToInt32(dt_decimal.Rows[0][0].ToString());
            usqlre.close();

            sql = "SELECT *,(SELECT 1 FROM sys.columns WHERE Name = N'com_logo' AND Object_ID = Object_ID(N'gnl_company')) as comLogo FROM gnl_company";
            DataTable dt_company = usqlre.dbReaderFill(sql);
            usqlre.close();

            //sql_str = "SELECT sp_id,sp_str_id,sp_entryno,sp_uniquecode,sp_ir_id,ISNULL(cast(sp_rate as nvarchar(100)),'0') sp_rate,ISNULL(cast(sp_realrate as nvarchar(100)),'0') sp_realrate,sp_qty,sp_fqty,ISNULL(cast(sp_gross_value as nvarchar(100)),'0') sp_gross_value,sp_disc_per,ISNULL(cast(sp_disc as nvarchar(100)),'0') sp_disc,ISNULL(cast(sp_real_disc as nvarchar(100)),'0') sp_real_disc,ISNULL(cast(sp_net_amount as nvarchar(100)),'0') sp_net_amount,ISNULL(cast(sp_tax as nvarchar(100)),'0') sp_tax,ISNULL(cast(sp_total as nvarchar(100)),'0') sp_total,ISNULL(cast(sp_profit as nvarchar(100)),'0') sp_profit,ISNULL(cast(sp_igst as nvarchar(100)),'0') sp_igst,ISNULL(cast(sp_cgst as nvarchar(100)),'0') sp_cgst,ISNULL(cast(sp_sgst as nvarchar(100)),'0') sp_sgst,ISNULL(cast(sp_mrp as nvarchar(100)),'0') sp_mrp,ISNULL(cast(sp_qty_multi_unit as nvarchar(100)),'0') sp_qty_multi_unit,ISNULL(cast(sp_srate_multiunit as nvarchar(100)),'0') sp_srate_multiunit,ISNULL(cast(sp_kfc as nvarchar(100)),'0') sp_kfc,ISNULL(cast(sp_prate as nvarchar(100)),'0') sp_prate,ISNULL(cast(sp_cost as nvarchar(100)),'0') sp_cost,sp_unit_multi,ir_name,ir_hsn_code,sp_lend_amount FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id WHERE sp_entryno=" +entryNo + " and sp_str_id=1";
            sql_str = "select ir_name as description,ir_hsn_code as hsnCode,sp_qty as qty,sp_rate as rate,sp_gross_value as gross,sp_net_amount as netAmt,sp_disc as discount,ir_cgst as cgstP,sp_cgst as cgst,ir_sgst as sgstP,sp_sgst as sgst,ir_taxper as TaxP,sp_tax as GST,sp_total as total,u_name as unit FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id left join inv_unit on sp_unit_multi=u_id WHERE sp_entryno=" + entryNo + " and sp_str_id=3";
            DataTable dtSalesPur = usqlre.dbReaderFill(sql_str);
            usqlre.close();
            sql_sales_inf = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],si_cust_name as custName,si_add1 as add1,si_add2 as add2,a.as_add3 as add3,a.as_mob as mob,si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST, si_tax as GST, si_total as Total, si_other_charge as otherCharge, 
                            si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,company_name,bra_name,clr_name,irc_name as receivingCondition,isr_service_name,si_expected_date,si_loc_entryno,
                            a.as_tin as gstin,s.as_name as salesman,si_einvoice_ksa,si_items_collected from inv_sales_inf left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_salesman_id=s.as_id left join inv_company on CONVERT(nvarchar(50), company_id) = si_company
                            left join inv_brand on CONVERT(nvarchar(50), bra_id) = si_model
                            left join inv_color on clr_id = si_color
                            left join inv_receiving_condition_reg on si_rc_id = irc_id
                            left join inv_service_reg on isr_id = si_salesman_id where si_str_id=3 and si_entryno=" + entryNo + "";
            DataTable dtSalesInf = usqlre.dbReaderFill(sql_sales_inf);
            usqlre.close();
            bool LOCATIONENTRYNO = false;
            DataTable dt_locEntryNo = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='LOCATION ENTRY NO'");
            if (dt_locEntryNo.Rows.Count > 0)
                LOCATIONENTRYNO = Convert.ToBoolean(Convert.ToInt32(dt_locEntryNo.Rows[0][0].ToString()));

            entry_No = !LOCATIONENTRYNO ? dtSalesInf.Rows[0]["entryNo"].ToString() : dtSalesInf.Rows[0]["si_loc_entryno"].ToString();
            //String[] strArray = null;
            string itemCollect = dtSalesInf.Rows[0]["SI_ITEMS_COLLECTED"].ToString();
            String[] itemCollectArray = itemCollect.Split(",");

            addr1 = dtSalesInf.Rows[0]["add1"].ToString();
            addr2 = dtSalesInf.Rows[0]["add2"].ToString();
            mob = dtSalesInf.Rows[0]["mob"].ToString();
            gstin = dtSalesInf.Rows[0]["gstin"].ToString();

            string sql_complaints = @"select li_ir_id as complaintId,
                                ir_name as complaint,
                                ift_name as fixType,
                                li_ir_mrp as amount,
                                li_ift_id as fixTypeId,
                                li_remarks as remarks
                            from inv_lend_item_transactions
                                left join inv_item_reg on ir_id = li_ir_id
                                left join inv_fix_type_reg on ift_id = li_ift_id
                            where li_entryno =" + dtSalesInf.Rows[0]["entryNo"] + "";
            DataTable dtComplaints = usqlre.dbReaderFill(sql_complaints);
            usqlre.close();

            string sql_collectedItem = @"select iic_id,iic_name from inv_items_collected where iic_status=1";
            DataTable dtCollected = usqlre.dbReaderFill(sql_collectedItem);
            usqlre.close();
            responseString = @"<!DOCTYPE html>
<html lang='en'>
  <head>
    <meta charset='UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Job Card</title>
    <style>
      * {
        padding: 0;
        margin: 0;
      }
      @page {
        size: 7in 9.25in;
        margin: 27mm 16mm 27mm 16mm;
      }
      .header {
        display: flex;
        flex-direction: row;
        justify-content: space-between;
        border-bottom: 3px solid rgb(14, 14, 14);
        padding-bottom: 10px;
      }
      .logo{
        width: 80px;
        height: 80px;
      }
      .nav {
        width: 100%;
        text-align: right;
        border-bottom: 1px solid;
        margin-top: 10px;
      }
      .det-list {
        display: flex;
        flex-direction: row;
        justify-content: space-between;
        margin-top: 10px;
      }
      .det-table {
        border: 1px solid;
        width: 100%;
      }
      .det-table th {
        text-align: center;
        border-bottom: 1px solid;
        border-right: 1px solid;
      }
      .det-table th:last-child {
        border-right: none;
      }
      .det-table td:last-child {
        border-right: none;
      }
      .det-table td {
        border-right: 1px solid;
      }
      .complaint-table {
        border: 1px solid;
        width: 100%;
      }
      .complaint-table th {
        text-align: center;
        border-bottom: 1px solid;
        border-right: 1px solid;
      }
      .complaint-table td {
        border-right: 1px solid;
      }
      .complaint-table th:last-child {
        border-right: none;
      }
      .complaint-table td:last-child {
        border-right: none;
      }
      .footer {
        display: flex;
        flex-direction: row;
        justify-content: space-between;
        margin: 20px;
      }
    </style>
  </head>
  <body>
    <div>
      <!-- Header -->
      <div class='header'>
        <!-- logo -->";
            if (dt_company.Rows[0]["comLogo"].ToString() != "" && dt_company.Rows[0]["com_logo"] != "")
            {
                responseString = "<img class='logo' src='./asset/mictco.jpg' alt=''>";
            }

            responseString += @"<div style='padding-bottom: 10px;'>
          <p>" + dt_company.Rows[0]["com_name"] + @"</p>
          <!-- <br /> -->
          <p>" + dt_company.Rows[0]["com_add1"] + @"</p>
          <!-- <br /> -->
          <p>" + dt_company.Rows[0]["com_add2"] + @"</p>
          <!-- <br /> -->
          <p>" + dt_company.Rows[0]["com_mob"] + @"</p>
          <!-- <br /> -->
          <p>" + dt_company.Rows[0]["com_email"] + @"</p>
          <!-- <br /> -->
        </div>
      </div>
      <div class='nav'>
        <p>Service NO:" + entry_No + @"</p>​
        <p>Date:" + dtSalesInf.Rows[0]["Date"] + @"</p>
      </div>
      <!-- Details -->
      <div class='det-list'>
        <div>
          <h4><u>Customer Details</u></h4>
​
          <table>
            <tr>
              <td>Name</td>
              <td>:" + dtSalesInf.Rows[0]["custName"] + @"</td>
            </tr>
            <tr>
              <td>Phone</td>
              <td>:" + addr2 + @"</td>
            </tr>
            <tr>
              <td>Address</td>
              <td>
                :" + addr1 + @"
              </td>
            </tr>
           </table>
   
        </div>
        <div>
          <table>
            <tr>
              <th><u> Phone Status</u></th>
            </tr>";
            //itemCollectArray.Contains("1")
            if (dtCollected.Rows.Count > 0)
            {
                for (int j = 0; j < dtCollected.Rows.Count; j = j + 2)
                {
                    responseString += "<tr><td>" + dtCollected.Rows[j]["iic_name"] + @"</td>";
                    string collection = dtCollected.Rows[j]["iic_id"].ToString();
                    string isChecked = (itemCollectArray.Contains(collection)) ? "checked" : "";
                    responseString += "<td>:</td><td><input type='checkbox' style='vertical-align: middle'" + isChecked + "/></td>";

                    if (j + 1 < dtCollected.Rows.Count)
                    {

                        responseString += @"<td style='padding-left: 15px'>" + dtCollected.Rows[j + 1]["iic_name"] + @"</td>";
                        string collection1 = dtCollected.Rows[j + 1]["iic_id"].ToString();
                        string isChecked1 = (itemCollectArray.Contains(collection1)) ? "checked" : "";
                        responseString += "<td>:</td><td><input type='checkbox' style='vertical-align: middle'" + isChecked1 + "/></td>";

                    }


                }
            }

            responseString += @"</table>
        </div>
      </div>
        <!-- Det-Table -->
      <div style='width: 100%'>
        <table class='det-table'>
          <tr>
            <th>Company</th>
            <th>Model</th>
            <th>color</th>
            <th>Receiving Condition</th>
            <th>Service Type</th>
          </tr>
          <tr>
            <td>" + dtSalesInf.Rows[0]["company_name"] + @"</td>
            <td>" + dtSalesInf.Rows[0]["bra_name"] + @"</td>
            <td>" + dtSalesInf.Rows[0]["clr_name"] + @"</td>
            <td>" + dtSalesInf.Rows[0]["receivingCondition"] + @"</td>
            <td>" + dtSalesInf.Rows[0]["isr_service_name"] + @"</td>
          </tr>
        </table>
      </div>
      <div style='margin-top: 20px'>
        <h4 style='margin-bottom: 15px'><u> Complaints</u></h4>
        <table class='complaint-table'>
          <tr>
            <th>Sl no</th>
            <th>Complaints</th>
            <th>AMT</th>
            <th>Fix Type</th>
            <th>Remarks</th>
          </tr>";
            double estimatedCost = 0.00;
            for (int i = 0; i < dtComplaints.Rows.Count; i++)
            {
                responseString += @" <tr>
                <td>" + (i + 1) + @"</td>
                <td>" + dtComplaints.Rows[i]["complaint"].ToString() + @"</td>
                <td style='text-align:right;'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtComplaints.Rows[i]["amount"].ToString())), _decimal).ToString() + @"</td>
                <td>" + dtComplaints.Rows[i]["fixType"].ToString() + @"</td>
                <td>" + dtComplaints.Rows[i]["remarks"].ToString() + @"</td>";
                double cost = decimal.ToDouble(System.Math.Round((CommonHelper.GetTextboxValue(dtComplaints.Rows[i]["amount"].ToString())), _decimal));
                estimatedCost += cost;
                responseString += "</tr>";
            }
            responseString += @"</table>
      </div>
      <!-- Footer -->
      <div class='footer'>
        <table>
          <tr>
            <th style='text-align:left;'>Estimated Cost</th>
            <td>:" + estimatedCost + @"</td>
          </tr>
          <tr>
            <th style='text-align:left;'>Declaration</th>
            <td>:</td>
          </tr>
        </table>
        <div>
          <table>
            <tr>
              <th style='text-align:left;'>Expected Date</th>
              <td>:" + dtSalesInf.Rows[0]["si_expected_date"] + @"</td>
            </tr>
            <tr>
              <th style='text-align:left;'>Signature</th>
              <td>:</td>
            </tr>
          </table>
        </div>
      </div>
    </div>
  </body>
</html>";
            return responseString;

        }
        [HttpGet("job-invoice/{entryNo}")]
        public async Task<IActionResult> jobInvoice(string entryNo)
        {

            UserSqlServer usqlre = new UserSqlServer(this);
            string responseString = "";
            string sql = "";
            string sql_str = "";
            string sql_sales_inf = "";
            string addr1 = "";
            string addr2 = "";
            string gstin = "";
            string mob = "";
            string entry_No = "";

            DataTable dt_decimal = usqlre.dbReaderFill("select gs_value from gnl_settings where gs_name='DECIMAL'");
            int _decimal = Convert.ToInt32(dt_decimal.Rows[0][0].ToString());
            usqlre.close();

            sql = "SELECT *,(SELECT 1 FROM sys.columns WHERE Name = N'com_logo' AND Object_ID = Object_ID(N'gnl_company')) as comLogo FROM gnl_company";
            DataTable dt_company = usqlre.dbReaderFill(sql);
            usqlre.close();

            //sql_str = "SELECT sp_id,sp_str_id,sp_entryno,sp_uniquecode,sp_ir_id,ISNULL(cast(sp_rate as nvarchar(100)),'0') sp_rate,ISNULL(cast(sp_realrate as nvarchar(100)),'0') sp_realrate,sp_qty,sp_fqty,ISNULL(cast(sp_gross_value as nvarchar(100)),'0') sp_gross_value,sp_disc_per,ISNULL(cast(sp_disc as nvarchar(100)),'0') sp_disc,ISNULL(cast(sp_real_disc as nvarchar(100)),'0') sp_real_disc,ISNULL(cast(sp_net_amount as nvarchar(100)),'0') sp_net_amount,ISNULL(cast(sp_tax as nvarchar(100)),'0') sp_tax,ISNULL(cast(sp_total as nvarchar(100)),'0') sp_total,ISNULL(cast(sp_profit as nvarchar(100)),'0') sp_profit,ISNULL(cast(sp_igst as nvarchar(100)),'0') sp_igst,ISNULL(cast(sp_cgst as nvarchar(100)),'0') sp_cgst,ISNULL(cast(sp_sgst as nvarchar(100)),'0') sp_sgst,ISNULL(cast(sp_mrp as nvarchar(100)),'0') sp_mrp,ISNULL(cast(sp_qty_multi_unit as nvarchar(100)),'0') sp_qty_multi_unit,ISNULL(cast(sp_srate_multiunit as nvarchar(100)),'0') sp_srate_multiunit,ISNULL(cast(sp_kfc as nvarchar(100)),'0') sp_kfc,ISNULL(cast(sp_prate as nvarchar(100)),'0') sp_prate,ISNULL(cast(sp_cost as nvarchar(100)),'0') sp_cost,sp_unit_multi,ir_name,ir_hsn_code,sp_lend_amount FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id WHERE sp_entryno=" +entryNo + " and sp_str_id=1";
            sql_str = "select ir_name as description,ir_hsn_code as hsnCode,sp_qty as qty,sp_rate as rate,sp_gross_value as gross,sp_net_amount as netAmt,sp_disc as discount,ir_cgst as cgstP,sp_cgst as cgst,ir_sgst as sgstP,sp_sgst as sgst,ir_taxper as TaxP,sp_tax as GST,sp_total as total,u_name as unit FROM inv_sales_par JOIN inv_item_reg on inv_sales_par.sp_ir_id=inv_item_reg.ir_id left join inv_unit on sp_unit_multi=u_id WHERE sp_entryno=" + entryNo + " and sp_str_id=3";
            DataTable dtSalesPur = usqlre.dbReaderFill(sql_str);
            usqlre.close();
            sql_sales_inf = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],right(convert(varchar(20),si_date,100),7) [time],si_cust_name as custName,si_add1 as add1,si_add2 as add2,a.as_add3 as add3,a.as_mob as mob,si_disc as Disc, si_net_amount as Net,si_cgst_total as CGST,si_sgst_total as SGST, si_tax as GST, si_total as Total, si_other_charge as otherCharge, 
                            si_other_disc as otherDiscount,si_gross_value as gross, si_grand_total as grandTotal,si_remarks as Remarks,si_freight_charge as freightCharge,si_ob as ob,si_net_balance as netBalanace,company_name,bra_name,clr_name,irc_name as receivingCondition,isr_service_name,si_loc_entryno,si_imei as imei,right(convert(varchar(20),si_expected_date,100),7) [ExpectedTime],CONVERT(varchar,si_expected_date,6) as expectedDate,
                            a.as_tin as gstin,s.as_name as salesman,si_einvoice_ksa,si_items_collected from inv_sales_inf left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_salesman_id=s.as_id left join inv_company on CONVERT(nvarchar(50), company_id) = si_company
                            left join inv_brand on CONVERT(nvarchar(50), bra_id) = si_model
                            left join inv_color on clr_id = si_color
                            left join inv_receiving_condition_reg on si_rc_id = irc_id
                            left join inv_service_reg on isr_id = si_salesman_id where si_str_id=3 and si_entryno=" + entryNo + "";
            DataTable dtSalesInf = usqlre.dbReaderFill(sql_sales_inf);
            usqlre.close();
            bool LOCATIONENTRYNO = false;
            DataTable dt_locEntryNo = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='LOCATION ENTRY NO'");
            if (dt_locEntryNo.Rows.Count > 0)
                LOCATIONENTRYNO = Convert.ToBoolean(Convert.ToInt32(dt_locEntryNo.Rows[0][0].ToString()));

            entry_No = !LOCATIONENTRYNO ? dtSalesInf.Rows[0]["entryNo"].ToString() : dtSalesInf.Rows[0]["si_loc_entryno"].ToString();
            //String[] strArray = null;
            string itemCollect = dtSalesInf.Rows[0]["SI_ITEMS_COLLECTED"].ToString();
            String[] itemCollectArray = itemCollect.Split(",");

            addr1 = dtSalesInf.Rows[0]["add1"].ToString();
            addr2 = dtSalesInf.Rows[0]["add2"].ToString();
            mob = dtSalesInf.Rows[0]["mob"].ToString();
            gstin = dtSalesInf.Rows[0]["gstin"].ToString();

            string sql_complaints = @"select li_ir_id as complaintId,
                                ir_name as complaint,
                                ift_name as fixType,
                                li_ir_mrp as amount,
                                li_ift_id as fixTypeId,
                                li_remarks as remarks
                            from inv_lend_item_transactions
                                left join inv_item_reg on ir_id = li_ir_id
                                left join inv_fix_type_reg on ift_id = li_ift_id
                            where li_entryno =" + dtSalesInf.Rows[0]["entryNo"] + "";
            DataTable dtComplaints = usqlre.dbReaderFill(sql_complaints);
            usqlre.close();

            string sql_collectedItem = @"select iic_id,iic_name from inv_items_collected where iic_status=1";
            DataTable dtCollected = usqlre.dbReaderFill(sql_collectedItem);
            usqlre.close();
            DataTable dt_print = usqlre.dbReaderFill("select ps_version from gnl_print_setup where ps_form='JOB-INVOICE-MOB'");
            var html = "";
            if (dt_print.Rows.Count > 0)
            {
                html = System.IO.File.ReadAllText(@"./assets/" + dt_print.Rows[0][0].ToString() + "");
                #region header
                html = html.Replace("{{com_name}}", dt_company.Rows[0]["com_name"].ToString());
                html = html.Replace("{{com_add1}}", dt_company.Rows[0]["com_add1"].ToString());
                html = html.Replace("{{com_add2}}", dt_company.Rows[0]["com_add2"].ToString());
                html = html.Replace("{{com_add3}}", dt_company.Rows[0]["com_add3"].ToString());
                html = html.Replace("{{com_telephone}}", dt_company.Rows[0]["com_telephone"].ToString());
                html = html.Replace("{{com_mob}}", dt_company.Rows[0]["com_mob"].ToString());
                html = html.Replace("{{com_ar_name}}", dt_company.Rows[0]["com_ar_name"].ToString());
                html = html.Replace("{{com_ar_add1}}", dt_company.Rows[0]["com_ar_add1"].ToString());
                html = html.Replace("{{com_ar_add2}}", dt_company.Rows[0]["com_ar_add2"].ToString());
                html = html.Replace("{{com_ar_add3}}", dt_company.Rows[0]["com_ar_add3"].ToString());
                html = html.Replace("{{com_ar_telephone}}", dt_company.Rows[0]["com_telephone"].ToString());
                html = html.Replace("{{com_ar_mob}}", dt_company.Rows[0]["com_ar_mob"].ToString());
                #endregion

                html = html.Replace("{{cName}}", dtSalesInf.Rows[0]["custName"].ToString());
                html = html.Replace("{{cPhone}}", addr2.ToString());
                html = html.Replace("{{jobNo}}", entry_No.ToString());
                html = html.Replace("{{time}}", dtSalesInf.Rows[0]["time"].ToString());
                html = html.Replace("{{date}}", dtSalesInf.Rows[0]["Date"].ToString());
                html = html.Replace("{{vat}}", gstin.ToString());
                double estimatedCost = 0.00;
                String items = "";
                int description = 0;
                for (int i = 0; i < dtSalesPur.Rows.Count; i++)
                {
                    if (dtSalesPur.Rows[i]["description"].ToString().Length / 23 > 0)
                        description += Convert.ToInt32(System.Math.Round(Convert.ToDecimal(dtSalesPur.Rows[i]["description"].ToString().Length / 23)));

                    items += @"<div class='cus-body-tr'>
                <div style='width: 42px' class='cus-td'>" + (i + 1) + @"</div>
                <div style='width: 142px' class='cus-td'>" + dtSalesPur.Rows[i]["description"].ToString() + @"</div>
                <div style='width: 60px' class='cus-td'>" + dtSalesPur.Rows[i]["hsnCode"].ToString() + @"</div>
                <div style='width: 40px' class='cus-td'>" + dtSalesPur.Rows[i]["qty"].ToString() + @"</div>
                <div style='width: 53.08px' class='cus-td'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["netAmt"].ToString())), _decimal).ToString() + @"</div>
                <div style='width: 53.08px' class='cus-td'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["Total"].ToString())), _decimal).ToString() + @"</div>
                </div>";

                    double cost = decimal.ToDouble(System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal));
                    estimatedCost += cost;

                }

                if (dtSalesPur.Rows.Count >= 0 && dtSalesPur.Rows.Count <= 4)
                {
                    if (dtSalesPur.Rows.Count + description < 11)
                    {
                        int linecnt = 10 - dtSalesPur.Rows.Count - description;
                        for (int l = 0; l < linecnt; l++)
                        {
                            items += @"<div class='cus-body-tr-dummy'>
                        <div style='width: 42px' class='cus-td-dummy'>0</div>
                        <div style='width: 142px' class='cus-td-dummy'></div>
                        <div style='width: 60px' class='cus-td-dummy'></div>
                        <div style='width: 40px' class='cus-td-dummy'></div>
                        <div style='width: 53.08px' class='cus-td-dummy'></div>
                        <div style='width: 53.08px' class='cus-td-dummy'></div>
                      </div>";
                        }
                    }

                }
                else
                {
                    if (dtSalesPur.Rows.Count + description < 12)
                    {
                        int linecnt = 11 - dtSalesPur.Rows.Count - description;
                        for (int l = 0; l < linecnt; l++)
                        {
                            items += @"<div class='cus-body-tr-dummy'>
                        <div style='width: 42px' class='cus-td-dummy'>0</div>
                        <div style='width: 142px' class='cus-td-dummy'></div>
                        <div style='width: 60px' class='cus-td-dummy'></div>
                        <div style='width: 40px' class='cus-td-dummy'></div>
                        <div style='width: 53.08px' class='cus-td-dummy'></div>
                        <div style='width: 53.08px' class='cus-td-dummy'></div>
                      </div>";
                        }
                    }
                }

                html = html.Replace("{{items}}", items);
                html = html.Replace("{{remark}}", dtSalesInf.Rows[0]["Remarks"].ToString());
                html = html.Replace("{{si_gross_value}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["gross"].ToString())), _decimal).ToString());
                html = html.Replace("{{si_disc}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Disc"].ToString())), _decimal).ToString());
                html = html.Replace("{{si_net}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString());
                html = html.Replace("{{si_tax}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["GST"].ToString())), _decimal).ToString());
                html = html.Replace("{{si_total}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Total"].ToString())), _decimal).ToString());


            }


            return base.Content(html, "text/html");
        }
        [HttpGet("b2c-invoice-test/{entryNo}")]
        public async Task<IActionResult> b2bInvoiceTest(string entryNo)
        {

            UserSqlServer usqlre = new UserSqlServer(this);
            var html = System.IO.File.ReadAllText(@"./assets/ksaEinvoice.html");
            string qr = "<img src='https://chart.googleapis.com/chart?cht=qr&chl=VALUEOFQR&chs=160x160&chld=L|0' class='qr-code img-thumbnail img-responsive'' />";
            #region header
            html = html.Replace("{{com_name}}", "com_name");
            html = html.Replace("{{com_add1}}", "com_add1");
            html = html.Replace("{{com_add2}}", "com_add2");
            html = html.Replace("{{com_add3}}", "com_add3");
            html = html.Replace("{{com_telephone}}", "com_telephone");
            html = html.Replace("{{com_mob}}", "com_mob");
            html = html.Replace("{{com_ar_name}}", "com_ar_name");
            html = html.Replace("{{com_ar_add1}}", "com_ar_add1");
            html = html.Replace("{{com_ar_add2}}", "com_ar_add2");
            html = html.Replace("{{com_ar_add3}}", "com_ar_add3");
            html = html.Replace("{{com_ar_telephone}}", "com_telephone");
            html = html.Replace("{{com_ar_mob}}", "com_ar_mob");
            #endregion
            html = html.Replace("{{cName}}", "custName");
            html = html.Replace("{{cPhone}}", "addr2");
            html = html.Replace("{{jobNo}}", "entry_No");
            html = html.Replace("{{time}}", "time");
            html = html.Replace("{{date}}", "Date");
            html = html.Replace("{{vat}}", "gstin");
            double estimatedCost = 0.00;
            String items = @" <tr>
                    <td class='tbl-data'>1</td>
                    <td class='tbl-data'>RAISINS 200
                        gm<br>زبيب 200 جم</td>
                    <td class='tbl-data'>2</td>
                    <td class='tbl-data'>5.00</td>
                    <td class='tbl-data'>8.70</td>
                    <td class='tbl-data'>1.30</td>
                    <td class='tbl-data'>10.00</td>
                </tr>";
            html = html.Replace("{{items}}", items);
            html = html.Replace("{{remark}}", "Remarks");
            html = html.Replace("{{si_gross_value}}", "gross");
            html = html.Replace("{{si_disc}}", "si_disc");
            html = html.Replace("{{si_net}}", "si_net");
            html = html.Replace("{{si_tax}}", "GST");
            html = html.Replace("{{si_total}}", "total");
            html = html.Replace("{{qr}}", qr);
            return base.Content(html, "text/html");
        }
        [HttpGet("b2cPrintKsaV2/{entryNo}")]
        public string b2cPrintKsaV2(string entryNo)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string printModel = "";
            string addr1 = "", addr2 = "", gstin = "", mob = "";
            string entry_No;
            bool LOCATIONENTRYNO = false;
            DataTable dt_print = usqlre.dbReaderFill("select ps_version from gnl_print_setup where ps_form='SALES-B2C-MOB'");
            printModel = dt_print.Rows[0][0].ToString();
            usqlre.close();
            int _decimal = Convert.ToInt32(usqlre.printmanage(1, entryNo, "decimal").Rows[0][0].ToString());
            DataTable dtSalesInf = usqlre.printmanage(1, entryNo, "salesinf");
            DataTable dtCntUnit = usqlre.printmanage(0, "", "unit");
            DataTable dtSalesPur = usqlre.printmanage(1, entryNo, "salespar");
            DataTable dt_locEntryNo = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='LOCATION ENTRY NO'");

            if (dt_locEntryNo.Rows.Count > 0)
                LOCATIONENTRYNO = Convert.ToBoolean(Convert.ToInt32(dt_locEntryNo.Rows[0][0].ToString()));

            entry_No = !LOCATIONENTRYNO ? dtSalesInf.Rows[0]["entryNo"].ToString() : dtSalesInf.Rows[0]["si_loc_entryno"].ToString();
            DataTable dt_arabicname = usqlre.dbReaderFill("select count(ans_name) as ksacount from android_settings where ans_name='INVOICE NAME ARABIC'");
            int _cntarabic = Convert.ToInt32(dt_arabicname.Rows[0][0].ToString());
            bool ARABIC = false;
            DataTable dt_arabic = usqlre.dbReaderFill("select ans_status from android_settings where ans_name = 'INVOICE NAME ARABIC'");
            if (dt_arabic.Rows.Count > 0)
            {
                ARABIC = Convert.ToBoolean(Convert.ToInt32(dt_arabic.Rows[0][0].ToString()));

            }

            if (dtSalesInf.Rows[0]["id"].ToString() == 1.ToString())
            {
                addr1 = "";
                addr2 = "";
                mob = "";
                gstin = "";
            }
            else
            {
                addr1 = dtSalesInf.Rows[0]["add1"].ToString();
                addr2 = dtSalesInf.Rows[0]["add2"].ToString();
                mob = dtSalesInf.Rows[0]["mob"].ToString();
                gstin = dtSalesInf.Rows[0]["gstin"].ToString();
            }
            string qr = "<img src='https://chart.googleapis.com/chart?cht=qr&chl=" + dtSalesInf.Rows[0]["si_einvoice_ksa"] + "&chs=160x160&chld=L|0' class='qr-code img-thumbnail img-responsive'' />";
            var html = System.IO.File.ReadAllText(@"./assets/" + printModel + "");
            html = html.Replace("{{salesman}}", dtSalesInf.Rows[0]["salesman"].ToString());
            html = html.Replace("{{cName}}", dtSalesInf.Rows[0]["custName"].ToString());
            html = html.Replace("{{cPhone}}", addr2);
            html = html.Replace("{{add1}}", addr1);
            html = html.Replace("{{add2}}", addr2);
            html = html.Replace("{{mob}}", mob);
            html = html.Replace("{{EntryNo}}", entry_No);
            html = html.Replace("{{time}}", dtSalesInf.Rows[0]["time"].ToString());
            html = html.Replace("{{date}}", dtSalesInf.Rows[0]["Date"].ToString());
            html = html.Replace("{{vat}}", gstin);
            double estimatedCost = 0.00;
            String items = "";
            int cnt = 1;
            for (int i = 0; i < dtSalesPur.Rows.Count; i++)
            {
                items += "<tr class='item'>";
                items += "<td class='text-center'>" + cnt + "</td>";
                if (!ARABIC && _cntarabic == 1 || _cntarabic == 0)
                {
                    items += "<td class='text-center'>" + dtSalesPur.Rows[i]["description"].ToString() + "</td>";
                }
                else
                {
                    items += "<td class='text-center'>" + dtSalesPur.Rows[i]["description"].ToString() + "<br>" + dtSalesPur.Rows[i]["hsnCode"].ToString() + "</td>";
                }

                items += "<td class='text-center'>" + dtSalesPur.Rows[i]["qty"].ToString() + "</td>";
                if (dtCntUnit.Rows.Count > 0)
                    items += "<td class='text-center'>" + dtSalesPur.Rows[i]["uom"].ToString() + "</td>";
                else
                    items += "<td class='text-center'>" + dtSalesPur.Rows[i]["unit"].ToString() + "</td>";

                items += "<td class='text-right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["rate"].ToString())), _decimal).ToString() + "</td>";
                items += "<td class='text-right'>" + dtSalesPur.Rows[i]["taxPer"].ToString() + "</td>";
                items += "<td class='text-right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["gst"].ToString())), _decimal).ToString() + "</td>";
                items += "<td class='text-right'>" + System.Math.Round((CommonHelper.GetTextboxValue(dtSalesPur.Rows[i]["total"].ToString())), _decimal).ToString() + "</td>";

                cnt = cnt + 1;
            }
            string amtInWord = CommonHelper.NumberToWordsDouble(Convert.ToDouble(dtSalesInf.Rows[0]["Total"].ToString()));
            if (dtSalesPur.Rows.Count <= 3)
            {
                items += "<tr class='item'><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td></tr>";
                items += "<tr class='item'><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td></tr>";
                items += "<tr class='item'><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td></tr>";
                items += "<tr class='item'><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td></tr>";
                items += "<tr class='item'><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td></tr>";
                items += "<tr class='item'><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td><td style='padding: 12px;'> </td></tr>";
            }
            html = html.Replace("{{items}}", items);
            html = html.Replace("{{amtInWord}}", amtInWord);
            html = html.Replace("{{remark}}", dtSalesInf.Rows[0]["Remarks"].ToString());
            html = html.Replace("{{si_gross_value}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["gross"].ToString())), _decimal).ToString());
            html = html.Replace("{{si_disc}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Disc"].ToString())), _decimal).ToString());
            html = html.Replace("{{si_net}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Net"].ToString())), _decimal).ToString());
            html = html.Replace("{{si_tax}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["gst"].ToString())), _decimal).ToString());
            html = html.Replace("{{si_total}}", System.Math.Round((CommonHelper.GetTextboxValue(dtSalesInf.Rows[0]["Total"].ToString())), _decimal).ToString());
            html = html.Replace("{{qr}}", qr);

            return html;

        }

    }
}
