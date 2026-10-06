using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;


namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class RPVController : ControllerBase
    {
        [HttpGet("get-payment-invoice")]
        public string getPaymentInvoice()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT max(pi_entryno) as max_entry_no,min(pi_entryno) as min_entry_no from acc_pv_inf";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);

        }

        [HttpPost("save-payment")]
        public async Task<IActionResult> savePayment([FromBody] ModelVoucher model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            if (usqlre.FinancialDateCheck(DateTime.Parse(model.date)) == false)
            {
                return Ok(new { status = false });
            }
            string ob = "0";
            string str_ledbalance = "EXEC	 [dbo].[Sp_acc_reg] " +
            "@as_id = " + model.partyId + "," +
            "@StatementType = N'ledbalance'";
            DataTable dt_ledbalance = usqlre.dbReaderFill(str_ledbalance);
            if (dt_ledbalance.Rows.Count > 0)
            {
                ob = dt_ledbalance.Rows[0][0].ToString();
            }
            string sql = " ";
            double ri_total = model.amount + model.discount;

            sql = "DECLARE @return_value int declare @Ids Type_acc_rv_par insert into @Ids ";
            sql += "Values ('" + model.amount + "','" + model.discount + "','" + ri_total + "','" + model.remark + "','" + model.partyId + "',' ','" + ob + "') ";
            sql += " EXEC @return_value = [dbo].[Sp_voucher] @ri_disc='" + model.discount + "',@ri_date='" + model.date + "',@ri_amount='" + model.amount + "',@ri_total='" + ri_total + "',@ri_debitaccount='" + model.cashacId + "',@ri_user='" + usqlre.userId + "',@ri_location_id='" + model.location + "',@type1=@Ids,@StatementType='InsertPv'";

            usqlre.close();
            string entryno = "0";
            DataTable dt = usqlre.dbReaderFill(sql);
            if (dt != null)
            {
                if (dt.Rows.Count > 0)
                {
                    entryno = dt.Rows[0]["entry_no"].ToString();
                    #region after save
                    try
                    {
                        if (usqlre.get_gnl_settings("ENABLEWHATSAPPMSG"))
                        {
                            string str_cb = "EXEC	 [dbo].[Sp_acc_reg] " +
                                "@as_id = " + model.partyId + "," +
                                "@StatementType = N'ledbalance'";
                            DataTable dt_cb = usqlre.dbReaderFill(str_cb);
                            string _cb = "";
                            _cb = dt_cb.Rows[0][0].ToString();
                            int acc_id = model.partyId;
                            DataTable dt_acc = new DataTable();
                            dt_acc = usqlre.dbReaderFill("select as_name,as_mob from acc_subhead where as_id= " + acc_id + "");
                            string _msg = "";
                            DataTable dt_msg = new DataTable();
                            dt_msg = usqlre.dbReaderFill("select wh_payment from gnl_whatsapp_settings");
                            _msg = (dt_msg.Rows[0]["wh_payment"].ToString());
                            _msg = _msg.Replace("_name", dt_acc.Rows[0]["as_name"].ToString());
                            _msg = _msg.Replace("_amt", ri_total.ToString());
                            _msg = _msg.Replace("_netbalance", _cb);

                            if (dt_acc.Rows.Count > 0)
                            {
                                string _mob = dt_acc.Rows[0]["as_mob"].ToString();
                                if(_mob.Length>0)
                                   usqlre.dbExecute("INSERT INTO gnl_whatsapp_sync(asy_wh_msg,asy_wh_mob,asy_wh_name,asy_wh_status,asy_wh_form,asy_wh_entryno)VALUES('" + _msg + "','" + _mob + "','" + dt_acc.Rows[0]["as_name"].ToString() + "',0,'Payment Voucher'," + entryno + ")");
                            }
                        }
                    }
                    catch(Exception ex) { }
                    usqlre.savecheckin(model.date, model.partyId);
                    #endregion
                    return Ok(new { entry_no = entryno, status = true });
                }
                else
                    return Ok(new { status = false });
            }
            else
            {
                return Ok(new { status = false });
            }

        }

        [HttpGet("get-payment/{id}")]
        public string getPaymentById(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT pi_entryno, pi_creditaccount, pp_account_id, pi_date, ISNULL(cast(pi_amount as nvarchar(100)), '0') pi_amount,ISNULL(cast(pp_disc as nvarchar(100)), '0') pp_disc,pp_remarks,pp_ob FROM acc_pv_inf JOIN acc_pv_par ON acc_pv_inf.pi_entryno = acc_pv_par.pp_rv_entryno WHERE acc_pv_inf.pi_entryno =" + id + "";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }

        [HttpGet("delete-payment/{id}")]
        public string deletePaymentById(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "DECLARE @return_value int EXEC @return_value = [dbo].[Sp_voucher]  @ri_entryno=" + id + ", @StatementType='DeletePv'";
            bool status = usqlre.dbExecute(sql);
            usqlre.close();
            if (status)
            {
                return ReportModelContext.searializeDt(new { status = true });
            }
            else
            {
                return ReportModelContext.searializeDt(new { status = false });
            }
        }

        [HttpPost("update-payment")]
        public async Task<IActionResult> updatePayment([FromBody] ModelVoucher model)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                if (usqlre.FinancialDateCheck(DateTime.Parse(model.date)) == false)
                {
                    return Ok(new { status = false });
                }
                string sql = " ";
                double ri_total = model.amount + model.discount;

                sql = "DECLARE @return_value int declare @Ids Type_acc_rv_par insert into @Ids ";
                sql += "Values ('" + model.amount + "','" + model.discount + "','" + ri_total + "','" + model.remark + "','" + model.partyId + "',' ', '" + model.ob + "') ";
                sql += " EXEC @return_value = [dbo].[Sp_voucher] @ri_disc='" + model.discount + "',@ri_date='" + model.date + "',@ri_amount='" + ri_total + "',@ri_total='" + ri_total + "',@ri_debitaccount='" + model.cashacId + "',@ri_user='" + usqlre.userId + "',@ri_location_id='" + model.location + "',@type1=@Ids,@ri_entryno='" + model.entryNo + "',@StatementType='UpdatePv'";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();
                return Ok(new
                {
                    status = true,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while updating the payment.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("search-payment")]
        public async Task<IActionResult> searchPayment([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = "";
            if (usqlre.user_role == "ADMIN")
            {

                if (model.Value == "")
                {
                    sql = "select top 10 cast(pp_rv_entryno as Int) as invoice_id,as_name as customer,cast(pi_date as date) as pi_date,pi_total from acc_pv_inf inner join acc_pv_par on pi_entryno=pp_rv_entryno inner join acc_subhead on pp_account_id = as_id order by pp_rv_entryno desc";
                }
                else
                {
                    sql = "select cast(pp_rv_entryno as Int) as invoice_id,as_name as customer,cast(pi_date as date) as pi_date,pi_total from acc_pv_inf inner join acc_pv_par on pi_entryno=pp_rv_entryno inner join acc_subhead on pp_account_id = as_id where pi_entryno = '" + model.Value + "'";
                }
            }
            else
            {
                if (model.Value == "")
                {
                    sql = "select top 10 cast(pp_rv_entryno as Int) as invoice_id,as_name as customer,cast(pi_date as date) as pi_date,pi_total from acc_pv_inf inner join acc_pv_par on pi_entryno=pp_rv_entryno inner join acc_subhead on pp_account_id = as_id where pi_user='" + usqlre.userId + "' order by pp_rv_entryno desc";
                }
                else
                {
                    sql = "select cast(pp_rv_entryno as Int) as invoice_id,as_name as customer,cast(pi_date as date) as pi_date,pi_total from acc_pv_inf inner join acc_pv_par on pi_entryno=pp_rv_entryno inner join acc_subhead on pp_account_id = as_id where pi_entryno = '" + model.Value + "' and pi_user='" + usqlre.userId + "'";
                }
            }

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }
        [HttpGet("get-receipt-invoice")]
        public string getRecieptInvoice()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT max(ri_entryno) as max_entry_no,min(ri_entryno) as min_entry_no from acc_rv_inf";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);

        }
        [HttpPost("save-receipt")]
        public async Task<IActionResult> saveReceipt([FromBody] ModelVoucher model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            if (usqlre.FinancialDateCheck(DateTime.Parse(model.date)) == false)
            {
                return Ok(new { status = false });
            }
            double ri_total = model.amount + model.discount;
            string sql = " ";
            string ob = "0";
            string str_ledbalance = "EXEC	 [dbo].[Sp_acc_reg] " +
            "@as_id = " + model.partyId + "," +
            "@StatementType = N'ledbalance'";
            DataTable dt_ledbalance = usqlre.dbReaderFill(str_ledbalance);
            if (dt_ledbalance.Rows.Count > 0)
            {
                ob = dt_ledbalance.Rows[0][0].ToString();
            }
            bool SALESMANRECIEPTAPPROVE = false;
            if (usqlre.get_android_settings("SALESMANRECIEPTAPPROVE"))
            {
                SALESMANRECIEPTAPPROVE = true;
            }

            if (usqlre.user_role == "ADMIN" || !SALESMANRECIEPTAPPROVE)
            {
                sql = "DECLARE @return_value int declare @Ids Type_acc_rv_par insert into @Ids ";
                sql += "Values ('" + model.amount + "','" + model.discount + "','" + ri_total + "','" + model.remark + "','" + model.partyId + "',' ','" + ob + "') ";
                sql += " EXEC @return_value = [dbo].[Sp_voucher] @ri_disc='" + model.discount + "',@ri_date='" + model.date + "',@ri_amount='" + model.amount + "',@ri_total='" + ri_total + "',@ri_debitaccount='" + model.cashacId + "',@ri_user='" + usqlre.userId + "',@ri_location_id='" + model.location + "',@type1=@Ids,@StatementType='InsertRv'";
            }
            else
            {
                sql = "insert into inv_verify_reciept(vr_cash_acc,vr_date,vr_party_acc,vr_party_amount,vr_discount,vr_total,vr_remarks,vr_salesman,vr_location_id,vr_user_id) values(" + model.cashacId + ",'" + model.date + "'," + model.partyId + ",'" + model.amount + "','" + model.discount + "','" + ri_total + "','" + model.remark + "'," + usqlre.gu_acc_id + "," + usqlre.locationId + "," + usqlre.userId + ") SELECT CAST(scope_identity() AS int) as entry_no";
            }

            string entryno = "0";
            DataTable dt = usqlre.dbReaderFill(sql);
            if (dt != null)
            {
                if (dt.Rows.Count > 0)
                {
                    entryno = dt.Rows[0]["entry_no"].ToString();
                    #region after save
                    try
                    {
                        if (usqlre.get_gnl_settings("ENABLEWHATSAPPMSG") && (!SALESMANRECIEPTAPPROVE || usqlre.user_role == "ADMIN"))
                        {
                            string str_cb = "EXEC	 [dbo].[Sp_acc_reg] " +
                                "@as_id = " + model.partyId + "," +
                                "@StatementType = N'ledbalance'";
                            DataTable dt_cb = usqlre.dbReaderFill(str_cb);
                            string _cb = "";
                            _cb = dt_cb.Rows[0][0].ToString();
                            int acc_id = model.partyId;
                            DataTable dt_acc = new DataTable();
                            dt_acc = usqlre.dbReaderFill("select as_name,as_mob from acc_subhead where as_id= " + acc_id + "");
                            string _msg = "";
                            DataTable dt_msg = new DataTable();
                            dt_msg = usqlre.dbReaderFill("select wh_receipt from gnl_whatsapp_settings");
                            _msg = (dt_msg.Rows[0]["wh_receipt"].ToString());
                            _msg = _msg.Replace("_name", dt_acc.Rows[0]["as_name"].ToString());
                            _msg = _msg.Replace("_amt", ri_total.ToString());
                            _msg = _msg.Replace("_netbalance", _cb);

                            if (dt_acc.Rows.Count > 0)
                            {
                                string _mob = dt_acc.Rows[0]["as_mob"].ToString();
                                if(_mob.Length>0)
                                   usqlre.dbExecute("INSERT INTO gnl_whatsapp_sync(asy_wh_msg,asy_wh_mob,asy_wh_name,asy_wh_status,asy_wh_form,asy_wh_entryno)VALUES('" + _msg + "','" + _mob + "','" + dt_acc.Rows[0]["as_name"].ToString() + "',0,'Reciept Voucher'," + entryno + ")");
                            }
                        }
                    }
                    catch (Exception ex) { }
                    #endregion

                    usqlre.savecheckin(model.date, model.partyId);
                    return Ok(new { entry_no = entryno, status = true });
                }
                else
                    return Ok(new { status = false });
            }
            else
            {
                return Ok(new { status = false });
            }
        }
        [HttpGet("get-verify-reciept/{id}")]
        public string getVerifyRecieptById(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT * from inv_verify_reciept where vr_id=" + id + "";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }
        [HttpGet("get-reciept/{id}")]
        public string getRecieptById(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT ri_entryno,ri_debitaccount,rp_account_id,ri_date,ISNULL(cast(ri_amount as nvarchar(100)),'0') ri_amount,ISNULL(cast(rp_disc as nvarchar(100)),'0') rp_disc,rp_remarks, rp_ob FROM acc_rv_inf JOIN acc_rv_par ON acc_rv_inf.ri_entryno=acc_rv_par.rp_rv_entryno WHERE acc_rv_inf.ri_entryno=" + id + "";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }

        [HttpGet("delete-reciept/{id}")]
        public string deleteRecieptById(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "DECLARE @return_value int EXEC @return_value = [dbo].[Sp_voucher]  @ri_entryno=" + id + ", @StatementType='DeleteRv'";

            bool status = usqlre.dbExecute(sql);
            usqlre.close();
            if (status)
            {
                return ReportModelContext.searializeDt(new { status = true });
            }
            else
            {
                return ReportModelContext.searializeDt(new { status = false });
            }
        }

        [HttpPost("update-reciept")]
        public async Task<IActionResult> updateReciept([FromBody] ModelVoucher model)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                if (usqlre.FinancialDateCheck(DateTime.Parse(model.date)) == false)
                {
                    return Ok(new { status = false });
                }
                string sql = " ";
                double ri_total = model.amount + model.discount;

                sql = "DECLARE @return_value int declare @Ids Type_acc_rv_par insert into @Ids ";
                sql += "Values ('" + model.amount + "','" + model.discount + "','" + ri_total + "','" + model.remark + "','" + model.partyId + "',' ','" + model.ob + "') ";
                sql += " EXEC @return_value = [dbo].[Sp_voucher] @ri_disc='" + model.discount + "',@ri_date='" + model.date + "',@ri_amount='" + model.amount + "',@ri_total='" + ri_total + "',@ri_debitaccount='" + model.cashacId + "',@ri_user='" + usqlre.userId + "',@ri_location_id='" + model.location + "',@type1=@Ids,@ri_entryno='" + model.entryNo + "',@StatementType='UpdateRv'";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();
                return Ok(new
                {
                    status = true,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred while updating the Receipt.",
                    error = ex.Message
                });
            }            
        }
        [HttpPost("voucher")]
        public async Task<IActionResult> Voucher([FromBody] VouncherModel model)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                int entryno = -1;

                bool SALESMANRECIEPTAPPROVE = false;
                if (usqlre.get_android_settings("SALESMANRECIEPTAPPROVE"))
                {
                    SALESMANRECIEPTAPPROVE = true;
                }
                if (usqlre.FinancialDateCheck(DateTime.Parse(model.date)) == false)
                {
                    return BadRequest(new { status = false, message = "Invalid financial date." });
                }

                if (model.Statement.Contains("Update") || model.Statement.Contains("Delete"))
                {
                    if (model.Statement.Contains("Rv"))
                    {
                        string sql = @"SELECT COUNT(*) FROM acc_rv_inf WHERE ri_entryno = " + model.entryNo;
                        DataTable entrynoexist = usqlre.dbReaderFill(sql);
                        if (entrynoexist.Rows.Count > 0 && Convert.ToInt32(entrynoexist.Rows[0][0].ToString()) == 0)
                        {
                            return BadRequest(new { status = false, message = "Voucher does not exist." });
                        }
                    }
                    else if (model.Statement.Contains("Pv"))
                    {
                        string sql = @"SELECT COUNT(*) FROM acc_pv_inf WHERE pi_entryno = " + model.entryNo;
                        DataTable entrynoexist = usqlre.dbReaderFill(sql);
                        if (entrynoexist.Rows.Count > 0 && Convert.ToInt32(entrynoexist.Rows[0][0].ToString()) == 0)
                        {
                            return BadRequest(new { status = false, message = "Voucher does not exist." });
                        }
                    }
                }

                if (model.Statement == "InsertRv")
                {
                    if (usqlre.user_role == "ADMIN" || !SALESMANRECIEPTAPPROVE)
                    {
                        entryno = usqlre.Sp_voucher("InsertRv", model);

                    }
                    else
                    {
                        if (model.items == null || model.items.Length != 1)
                        {
                            return BadRequest(new { status = false, message = "Exactly one item is allowed for this operation." });
                        }
                        var firstItem = model.items[0];

                        DataTable dt = usqlre.dbReaderFill("insert into inv_verify_reciept(vr_cash_acc,vr_date,vr_party_acc,vr_party_amount,vr_discount,vr_total,vr_remarks,vr_salesman,vr_location_id,vr_user_id) values(" + model.cashacId + ",'" + model.date + "'," + firstItem.partyId + ",'" + firstItem.amount + "','" + firstItem.discount + "','" + firstItem.total + "','" + firstItem.remark + "'," + usqlre.gu_acc_id + "," + usqlre.locationId + "," + usqlre.userId + ") SELECT CAST(scope_identity() AS int) as entry_no");
                        if (dt != null)
                        {
                            if (dt.Rows.Count > 0)
                            {
                                entryno = Convert.ToInt32(dt.Rows[0]["entry_no"]);
                            }
                        }
                    }
                }
                else if (model.Statement == "UpdateRv" && (usqlre.user_role == "ADMIN" || !SALESMANRECIEPTAPPROVE))
                {
                    entryno = usqlre.Sp_voucher("UpdateRv", model);
                }
                else if (model.Statement == "DeleteRv" && (usqlre.user_role == "ADMIN" || !SALESMANRECIEPTAPPROVE))
                {
                    entryno = usqlre.Sp_voucher("DeleteRv", model);
                }
                else if (model.Statement == "InsertPv")
                {
                    entryno = usqlre.Sp_voucher("InsertPv", model);
                }
                else if (model.Statement == "UpdatePv")
                {
                    entryno = usqlre.Sp_voucher("UpdatePv", model);
                }
                else if (model.Statement == "DeletePv")
                {
                    entryno = usqlre.Sp_voucher("DeletePv", model);
                }

                usqlre.close();

                if (entryno > -1)
                {
                    if (model.Statement == "InsertRv" || model.Statement == "InsertPv")
                    {
                        return Ok(new { status = true, result = entryno });
                    }
                    else
                    {
                        return Ok(new { status = true });
                    }
                }

                return BadRequest(new { status = false, message = "Operation failed." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred.",
                    error = ex.Message
                });
            }
        }

        //[HttpPost("search-receipt")]
        //public async Task<IActionResult> searchReceipt([FromBody] Params model)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);
        //    string sql = "";
        //    if (model.Value == "")
        //    {
        //        sql = "select top 10 cast(rp_rv_entryno as Int) as invoice_id,as_name as customer,cast(ri_date as date) as ri_date,ri_total from acc_rv_inf inner join acc_rv_par on ri_entryno=rp_rv_entryno inner join acc_subhead on rp_account_id = as_id where ri_user=" + usqlre.userId + " order by rp_rv_entryno desc";
        //    }
        //    else
        //    {
        //        sql = "select top 10 cast(rp_rv_entryno as Int) as invoice_id,as_name as customer,cast(ri_date as date) as ri_date,ri_total from acc_rv_inf inner join acc_rv_par on ri_entryno=rp_rv_entryno inner join acc_subhead on rp_account_id = as_id where ri_entryno = " + model.Value + " and ri_user=" + usqlre.userId + "";
        //    }

        //    DataTable dt = usqlre.dbReaderFill(sql);
        //    usqlre.close();
        //    return Ok(ReportModelContext.searializeDt(dt));
        //}
        [HttpPost("search-receipt")]
        public async Task<IActionResult> searchReceipt([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            if (model.Value == "")
            {
                if (usqlre.get_gnl_settings("LOCATION ENTRY NO") && usqlre.userId != "1")
                {
                    sql = "select top 10 cast(rp_rv_entryno as Int) as invoice_id,as_name as customer,cast(ri_date as date) as ri_date,ri_total from acc_rv_inf inner join acc_rv_par on ri_entryno=rp_rv_entryno inner join acc_subhead on rp_account_id = as_id where ri_location_id=" + usqlre.locationId + " order by rp_rv_entryno desc";
                }
                else
                {
                    sql = "select top 10 cast(rp_rv_entryno as Int) as invoice_id,as_name as customer,cast(ri_date as date) as ri_date,ri_total from acc_rv_inf inner join acc_rv_par on ri_entryno=rp_rv_entryno inner join acc_subhead on rp_account_id = as_id  order by rp_rv_entryno desc";
                }
            }
            else
            {
                if (usqlre.get_gnl_settings("LOCATION ENTRY NO") && usqlre.userId != "1")
                {
                    sql = "select top 10 cast(rp_rv_entryno as Int) as invoice_id,as_name as customer,cast(ri_date as date) as ri_date,ri_total from acc_rv_inf inner join acc_rv_par on ri_entryno=rp_rv_entryno inner join acc_subhead on rp_account_id = as_id where ri_entryno = " + model.Value + " and ri_location_id=" + usqlre.locationId + "";
                }
                else
                {
                    sql = "select top 10 cast(rp_rv_entryno as Int) as invoice_id,as_name as customer,cast(ri_date as date) as ri_date,ri_total from acc_rv_inf inner join acc_rv_par on ri_entryno=rp_rv_entryno inner join acc_subhead on rp_account_id = as_id where ri_entryno = " + model.Value + "";
                }

            }

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }
        [HttpGet("payment/{id}")]
        public async Task<IActionResult> paymentByID(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            sql = @"SELECT pi_entryno AS entryNo,
                    ISNULL(NULLIF(CAST(pi_loc_entryno AS nvarchar(50)), ''), CAST(pi_entryno AS nvarchar(50))) AS paymentNo,
                    'PAYMENT VOUCHER' AS voucherTitle,
                    pp_amount AS amount,pp_disc AS discount,pp_remarks AS remark,
                    pp_account_id AS partyId,ACC.as_name as partyName,ACC.as_mob AS phone,ACC.as_tin AS gstin,
                    pi_date as date,CONVERT(varchar(11), pi_date, 106) AS paymentDate,
                    pi_creditaccount as cashacId,CR.as_name as cashName,CR.as_name AS paymentMode,pi_location_id as location,
                    pi_amount AS amountPaid,pi_amount AS amountReceived,pp_amount AS cashPaid,pp_amount AS cashReceived,
                    pp_disc AS discountAmount,pp_total AS netAmount,
                    (select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr'
	                ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions 
	                where at_as_id=pp_account_id) as ob,
                    (select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr'
                    ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions
                    where at_as_id=pp_account_id and at_id < ISNULL((select MIN(at_id) from acc_account_transactions where at_entryno=pi_entryno and at_form='PAYMENT'), 0)) as oldBalance,
                    (select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr'
                    ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions
                    where at_as_id=pp_account_id and at_id <= ISNULL((select MAX(at_id) from acc_account_transactions where at_entryno=pi_entryno and at_form='PAYMENT'), 0)) as closingBalance,
                    COM.com_name AS companyName,COM.com_add1 AS companyAddress1,COM.com_add2 AS companyAddress2,
                    COM.com_add3 AS companyAddress3,COM.com_gstin AS companyGstin,COM.com_mob AS companyMobile,
                    COM.com_telephone AS companyPhone,LOC.gl_name AS locationName,LOC.gl_add1 AS officeAddress1,
                    LOC.gl_add2 AS officeAddress2,LOC.gl_add3 AS officeAddress3
                    FROM  [dbo].[acc_pv_inf] 
                    INNER JOIN [dbo].[acc_pv_par] ON pi_entryno=pp_rv_entryno 
                    LEFT JOIN acc_subhead CR ON pi_creditaccount=CR.as_id
                    LEFT JOIN acc_subhead ACC ON pp_account_id=ACC.as_id
                    LEFT JOIN gnl_location LOC ON pi_location_id=LOC.gl_id
                    CROSS JOIN gnl_company COM
                    WHERE pi_entryno=" + id + "";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();

            if (dt == null || dt.Rows.Count == 0)
            {
                return NotFound(new { message = "Payment not found" });
            }

            if (!dt.Columns.Contains("amountInWords"))
            {
                dt.Columns.Add("amountInWords", typeof(string));
            }
            foreach (DataRow row in dt.Rows)
            {
                double amountPaid = 0;
                if (row["amountPaid"] != DBNull.Value)
                {
                    double.TryParse(row["amountPaid"].ToString(), out amountPaid);
                }
                row["amountInWords"] = CommonHelper.NumberToWordsDouble(amountPaid) + " Only";
            }

            return Ok(ReportModelContext.searializeDt(dt));
        }
        [HttpGet("receipt/{id}")]
        public async Task<IActionResult> receiptByID(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            if (usqlre.user_role == "ADMIN" || !usqlre.get_android_settings("SALESMANRECIEPTAPPROVE"))
            {
                sql = @"SELECT ri_entryno AS entryNo,
                    ISNULL(NULLIF(CAST(ri_loc_entryno AS nvarchar(50)), ''), CAST(ri_entryno AS nvarchar(50))) AS receiptNo,
                    'RECEIPT VOUCHER' AS voucherTitle,
                    rp_amount AS amount,rp_disc AS discount,rp_remarks AS remark,
                    rp_account_id AS partyId,ACC.as_name as partyName,ACC.as_mob AS phone,ACC.as_tin AS gstin,
                    ri_date as date,CONVERT(varchar(11), ri_date, 106) AS receiptDate,
                    ri_debitaccount as cashacId,DR.as_name as cashName,DR.as_name AS paymentMode,ri_location_id as location,
                    ri_amount AS amountReceived,rp_amount AS cashReceived,rp_disc AS discountAmount,
					(select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr'
					ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions 
					where at_as_id=rp_account_id) as ob,
                    (select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr'
                    ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions
                    where at_as_id=rp_account_id and at_id < ISNULL((select MIN(at_id) from acc_account_transactions where at_entryno=ri_entryno and at_form='RECEIPT'), 0)) as oldBalance,
                    (select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr'
                    ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions
                    where at_as_id=rp_account_id and at_id <= ISNULL((select MAX(at_id) from acc_account_transactions where at_entryno=ri_entryno and at_form='RECEIPT'), 0)) as closingBalance,
                    COM.com_name AS companyName,COM.com_add1 AS companyAddress1,COM.com_add2 AS companyAddress2,
                    COM.com_add3 AS companyAddress3,COM.com_gstin AS companyGstin,COM.com_mob AS companyMobile,
                    COM.com_telephone AS companyPhone,LOC.gl_name AS locationName,LOC.gl_add1 AS officeAddress1,
                    LOC.gl_add2 AS officeAddress2,LOC.gl_add3 AS officeAddress3
                    FROM  [dbo].[acc_rv_inf]
                    INNER JOIN [dbo].[acc_rv_par] ON ri_entryno=rp_rv_entryno 
                    LEFT JOIN acc_subhead DR ON ri_debitaccount=DR.as_id
                    LEFT JOIN acc_subhead ACC ON rp_account_id=ACC.as_id
                    LEFT JOIN gnl_location LOC ON ri_location_id=LOC.gl_id
                    CROSS JOIN gnl_company COM
                    WHERE ri_entryno=" + id + "";
            }
            else 
            {
                sql = @"select vr_id AS entryNo,CAST(vr_id AS nvarchar(50)) AS receiptNo,'RECEIPT VOUCHER' AS voucherTitle,
                    vr_party_amount As amount, vr_discount as discount,vr_remarks as remark, vr_party_acc as partyId,
					ACC.as_name as partyName,ACC.as_mob AS phone,ACC.as_tin AS gstin,
                    vr_date as date,CONVERT(varchar(11), vr_date, 106) AS receiptDate,
                    vr_cash_acc as cashacId,DR.as_name as cashName,DR.as_name AS paymentMode,vr_location_id as location,
                    vr_party_amount AS amountReceived,vr_party_amount AS cashReceived,vr_discount AS discountAmount,
                    vr_total AS netAmount,vr_total AS totalAmount,'0' AS oldBalance,
					(select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr'
					ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions 
					where at_as_id=vr_party_acc) as ob,
                    (select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr'
                    ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions 
					where at_as_id=vr_party_acc) as closingBalance,
                    COM.com_name AS companyName,COM.com_add1 AS companyAddress1,COM.com_add2 AS companyAddress2,
                    COM.com_add3 AS companyAddress3,COM.com_gstin AS companyGstin,COM.com_mob AS companyMobile,
                    COM.com_telephone AS companyPhone,LOC.gl_name AS locationName,LOC.gl_add1 AS officeAddress1,
                    LOC.gl_add2 AS officeAddress2,LOC.gl_add3 AS officeAddress3
					from inv_verify_reciept 
					LEFT JOIN acc_subhead ACC ON vr_party_acc=ACC.as_id
					LEFT JOIN acc_subhead DR ON vr_cash_acc=DR.as_id
                    LEFT JOIN gnl_location LOC ON vr_location_id=LOC.gl_id
                    CROSS JOIN gnl_company COM
					where vr_id=" + id + "";
            }

                
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();

            if (dt == null || dt.Rows.Count == 0)
            {
                return NotFound(new { message = "Receipt not found" });
            }

            if (!dt.Columns.Contains("amountInWords"))
            {
                dt.Columns.Add("amountInWords", typeof(string));
            }
            foreach (DataRow row in dt.Rows)
            {
                double amountReceived = 0;
                if (row["amountReceived"] != DBNull.Value)
                {
                    double.TryParse(row["amountReceived"].ToString(), out amountReceived);
                }
                row["amountInWords"] = CommonHelper.NumberToWordsDouble(amountReceived) + " Only";
            }

            return Ok(ReportModelContext.searializeDt(dt));
        }

        [HttpGet("show-bill")]
        public async Task<IActionResult> ShowBill(int custId, string voucherName)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            bool ENABLENEWWORKSHOP = usqlre.get_gnl_settings("ENABLENEWWORKSHOP");
            bool ENABLETRIP = usqlre.get_gnl_settings("ENABLETRIP");
            bool ENABLELOGISTICS = usqlre.get_gnl_settings("ENABLELOGISTICS");

            DataSet ds = new DataSet();
            var billDetails = new List<object>();
            int serialNo = 1;
            if (voucherName == "DEBIT NOTE")
            {
                ds = usqlre.dbSelectData1("ShowBill", "DEBIT NOTE", custId);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    DataRow row = ds.Tables[0].Rows[i];
                    decimal balanceAmt = Convert.ToDecimal(row["Balanceamt"]);
                    string voucher = row["voucher"].ToString();

                    if (balanceAmt > 0)
                    {
                        var detail = new
                        {
                            SlNo = serialNo++,
                            Date = Convert.ToDateTime(row["pi_date"]),
                            InvoiceNo = Convert.ToInt32(row["pi_entryno"]),
                            LocEntryNo = TryGetValue(row, "pi_tax_invno") ?? row["pi_loc_entryno"].ToString(),

                            Voucher = voucher,
                            BillAmt = Convert.ToDecimal(row["pi_grand_total"]),
                            BillBalance = balanceAmt,
                            Amt = 0,
                            Balance = 0,
                            SupInvNo = row["pi_sup_invno"].ToString(),
                        };

                        billDetails.Add(detail);
                    }
                    
                }


            }
            if (voucherName == "CREDIT NOTE")
            {
                ds = usqlre.dbSelectData1("ShowBill", "CREDIT NOTE", custId);

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    DataRow row = ds.Tables[0].Rows[i];
                    decimal balanceAmt = Convert.ToDecimal(row["Balanceamt"]);
                    string voucher = row["voucher"].ToString();

                    if (balanceAmt > 0)
                    {
                        var detail = new
                        {
                            SlNo = serialNo++,
                            Date = Convert.ToDateTime(row["si_date"]),
                            InvoiceNo = Convert.ToInt32(row["si_entryno"]),
                            LocEntryNo = row["si_loc_entryno"].ToString(),
                            Voucher = voucher,
                            BillAmt = Convert.ToDecimal(row["si_grand_total"]),
                            BillBalance = balanceAmt,
                            Amt = 0,
                            Balance = 0,
                            Disc = 0,
                            TDS = 0,
                            SupInvNo = "",
                        };

                        billDetails.Add(detail);
                    }

                }


            }
            else if (voucherName == "RECEIPT-I")
            {
                if (ENABLETRIP)
                {
                    ds = usqlre.dbSelectData1("ShowBill_trip", "RECEIPT-I", custId);
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        DataRow row = ds.Tables[0].Rows[i];
                        decimal balanceAmt = Convert.ToDecimal(row["Balanceamt"]);
                        string voucher = row["voucher"].ToString();

                        if (balanceAmt > 0)
                        {
                            var detail = new
                            {
                                SlNo = serialNo++,
                                Date = Convert.ToDateTime(row["lm_tbi_entry_date"]),
                                InvoiceNo = Convert.ToInt32(row["lm_tbi_entry_no"]),
                                LocEntryNo = row["lm_tbi_entry_no"].ToString(),
                                Voucher = voucher,
                                BillAmt = Convert.ToDecimal(row["lm_tbi_grand_total"]),
                                BillBalance = balanceAmt,
                                Amt = 0,
                                Balance = 0,
                                Disc = 0,
                                TDS = 0,
                                SupInvNo = "",
                            };

                            billDetails.Add(detail);
                        }
                    }

                }
                else if (ENABLELOGISTICS)
                {
                    ds = usqlre.dbSelectData1("ShowBill_billing", "PAYMENT-I", custId);
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        DataRow row = ds.Tables[0].Rows[i];
                        decimal balanceAmt = Convert.ToDecimal(row["Balanceamt"]);
                        string voucher = row["voucher"].ToString();

                        if (balanceAmt > 0)
                        {
                            var detail = new
                            {
                                SlNo = serialNo++,
                                Date = Convert.ToDateTime(row["lb_kr_invoice_date"]),
                                InvoiceNo = Convert.ToInt32(row["lb_entryno"]),
                                LocEntryNo = row["ldpvp_consignor_invoice_no"].ToString(),
                                Voucher = voucher,
                                BillAmt = Convert.ToDecimal(row["lb_total_freight"]),
                                BillBalance = balanceAmt,
                                Amt = 0,
                                Balance = 0,
                                SupInvNo = "",
                            };

                            billDetails.Add(detail);
                        }
                    }

                }
                else
                {
                    ds = usqlre.dbSelectData1("ShowBill", "RECEIPT-I", custId);
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        DataRow row = ds.Tables[0].Rows[i];
                        decimal balanceAmt = Convert.ToDecimal(row["Balanceamt"]);
                        string voucher = row["voucher"].ToString();

                        if (balanceAmt > 0)
                        {
                            var detail = new
                            {
                                SlNo = serialNo++,
                                Date = Convert.ToDateTime(row["si_date"]),
                                InvoiceNo = Convert.ToInt32(row["si_entryno"]),
                                LocEntryNo = row["si_loc_entryno"].ToString(),
                                Voucher = voucher,
                                BillAmt = Convert.ToDecimal(row["si_grand_total"]),
                                BillBalance = balanceAmt,
                                Amt = 0,
                                Balance = 0,
                                Disc = 0,
                                TDS = 0,
                                SupInvNo = "",
                                LCMob = ENABLENEWWORKSHOP ? row["lc_mob"].ToString() : "",
                                LCName = ENABLENEWWORKSHOP ? row["lc_name"].ToString() : ""
                            };

                            billDetails.Add(detail);
                        }
                        else if (balanceAmt < 0  && voucher == "SALES RETURN")
                        {
                            var detail = new
                            {
                                SlNo = serialNo++,
                                Date = Convert.ToDateTime(row["si_date"]),
                                InvoiceNo = Convert.ToInt32(row["si_entryno"]),
                                LocEntryNo = row["si_loc_entryno"].ToString(),
                                Voucher = voucher,
                                BillAmt = Convert.ToDecimal(row["si_grand_total"]),
                                BillBalance = balanceAmt,
                                Amt = 0,
                                Balance = 0,
                                Disc = 0,
                                TDS = 0,
                                SupInvNo = "",
                                LCMob = ENABLENEWWORKSHOP ? row["lc_mob"].ToString() : "",
                                LCName = ENABLENEWWORKSHOP ? row["lc_name"].ToString() : ""
                            };

                            billDetails.Add(detail);
                        }
                    }

                }


            }
            else if(voucherName == "PAYMENT-I")
            {
                if (ENABLELOGISTICS)
                {
                    ds = usqlre.dbSelectData1("ShowBill_payment", "PAYMENT-I", custId);
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        DataRow row = ds.Tables[0].Rows[i];
                        decimal balanceAmt = Convert.ToDecimal(row["Balanceamt"]);
                        string voucher = row["voucher"].ToString();

                        if (balanceAmt > 0 )
                        {
                            var detail = new
                            {
                                SlNo = serialNo++,
                                Date = Convert.ToDateTime(row["at_date"]),
                                InvoiceNo = Convert.ToInt32(row["at_entryno"]),
                                LocEntryNo = row["at_loc_entryno"].ToString(),
                                Voucher = voucher,
                                BillAmt = Convert.ToDecimal(row["BillTotal"]),
                                BillBalance = balanceAmt,
                                Amt = 0,
                                Balance = 0,
                                SupInvNo = row["at_loc_entryno"].ToString(),
                            };

                            billDetails.Add(detail);
                        }
                    }


                }
                else
                {
                    ds = usqlre.dbSelectData1("ShowBill", "PAYMENT-I", custId);
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        DataRow row = ds.Tables[0].Rows[i];
                        decimal balanceAmt = Convert.ToDecimal(row["Balanceamt"]);
                        string voucher = row["voucher"].ToString();

                        if (balanceAmt > 0)
                        {
                            var detail = new
                            {
                                SlNo = serialNo++,
                                Date = Convert.ToDateTime(row["pi_date"]),
                                InvoiceNo = Convert.ToInt32(row["pi_entryno"]),
                                LocEntryNo = row["pi_loc_entryno"].ToString(),
                                Voucher = voucher,
                                BillAmt = Convert.ToDecimal(row["pi_grand_total"]),
                                BillBalance = balanceAmt,
                                Amt = 0,
                                Balance = 0,
                                SupInvNo = row["pi_sup_invno"].ToString(),
                            };

                            billDetails.Add(detail);
                        }
                        else if(balanceAmt < 0)
                        {
                            if (voucher== "PURCHASE-R")
                            {
                                var detail = new
                                {
                                    SlNo = serialNo++,
                                    Date = Convert.ToDateTime(row["pi_date"]),
                                    InvoiceNo = Convert.ToInt32(row["pi_entryno"]),
                                    LocEntryNo = row["pi_loc_entryno"].ToString(),
                                    Voucher = voucher,
                                    BillAmt = Convert.ToDecimal(row["pi_grand_total"]),
                                    BillBalance = balanceAmt,
                                    Amt = 0,
                                    Balance = 0,
                                    //SupInvNo = row["pi_sup_invno"].ToString(),
                                };

                                billDetails.Add(detail);
                            }
                        }
                    }

                }
            }            
            return Ok(billDetails);
        }
        private string? TryGetValue(DataRow row, string columnName)
        {
            try
            {
                return row[columnName]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        [HttpPost("invoice-wise-payment-and-receipt-voucher")]
        public async Task<IActionResult> PaymentIandReceiptIVoucher([FromBody] InvoicewisePaymentAndReceiptModel model)
        {
            int entryNo = -1;
            UserSqlServer usqlre = new UserSqlServer(this);
            if (usqlre.FinancialDateCheck(model.date) == false)
            {
                return Ok(new { status = false });
            }
            if (model.statement== "Insert" && (model.voucherName== "PAYMENT-I" || model.voucherName == "RECEIPT-I" || model.voucherName == "CREDIT NOTE" || model.voucherName == "DEBIT NOTE"))
            {
                 entryNo = usqlre.Sp_Billwise("Insert", model);
            }
            else if (model.statement == "Update" && (model.voucherName == "PAYMENT-I" || model.voucherName == "RECEIPT-I" || model.voucherName == "CREDIT NOTE" || model.voucherName == "DEBIT NOTE"))
            {
                 entryNo = usqlre.Sp_Billwise("Update", model);
            }
            else if (model.statement == "Delete" && (model.voucherName == "PAYMENT-I" || model.voucherName == "RECEIPT-I" || model.voucherName == "CREDIT NOTE" || model.voucherName == "DEBIT NOTE"))
            {
                entryNo = usqlre.Sp_Billwise("Delete", model);
            }
            if (entryNo <= 0)
            {
                return BadRequest(new { status = false, msg = "Error in data", entryNo = entryNo });
            }
            return Ok(new { status = true, msg = "Successful", entryNo = entryNo });
        }

        //[HttpGet("get-invoice-wise-payment-and-receipt-voucher")]
        //public async Task<IActionResult> GetPaymentIandReceiptIVoucher(string VoucherName, int EntryNo)
        //{
        //    try
        //    {
        //        DataTable main = new DataTable();
        //        DataTable detail = new DataTable();
        //        UserSqlServer usqlre = new UserSqlServer(this);

        //        if (VoucherName == "PAYMENT-I" || VoucherName == "RECEIPT-I")
        //        {
        //            // Main info query
        //            string mainSql = $@"
        //                SELECT 
        //                    bei_entryno AS entryNo,
        //                    bei_voucher_name AS voucherName,
        //                    bei_date AS date,
        //                    bei_acc_id AS accountId,
        //                    bei_dr_cr_acc_id AS drCrAccId,
        //                    bei_amount AS amount,
        //                    bei_sum_disc AS sumDisc,
        //                    bei_sum_tds AS sumTds,
        //                    bei_reference AS reference,
        //                    bei_net_amount AS netAmount,
        //                    bei_sgstp AS sgstp,
        //                    bei_cgstp AS cgstp,
        //                    bei_igstp AS igstp,
        //                    bei_sgst AS sgst,
        //                    bei_cgst AS cgst,
        //                    bei_igst AS igst,
        //                    bei_grand_total AS grandTotal,
        //                    bei_remarks AS remarks,
        //                    bei_salesman_id AS salasManId,
        //                    bei_approved AS approved,
        //                    bei_gst_status AS gstStatus,
        //                    bei_transfer_status AS transferStatus,
        //                    bei_scheme_status AS schemeStatus,
        //                    bei_irn AS irn,
        //                    bei_qr_link AS qrLink,
        //                    bei_json_insert AS jsonInsert,
        //                    bei_cancel_status AS cancelStatus,
        //                    bei_cost_id AS costId,
        //                    bei_roundoff AS roundOff
        //                FROM acc_billwise_inf
        //                LEFT JOIN acc_subhead a ON bei_acc_id = a.as_id
        //                LEFT JOIN acc_subhead b ON bei_dr_cr_acc_id = b.as_id
        //                WHERE bei_voucher_name = '{VoucherName}' and bei_entryno = {EntryNo}
        //                ORDER BY bei_date DESC;
        //            ";

        //            main = usqlre.dbReaderFill(mainSql);

        //            // Detail info query
        //            string detailSql = $@"
        //                SELECT TOP 20 
        //                bp_entryno AS entryNo,
        //                bp_voucher_entryno AS voucherEntryno,
        //                bp_bill_date AS billDate,
        //                bp_voucher_type AS voucherType,
        //                bp_bill_amount AS billAmount,
        //                bp_bill_balance AS billBalance,
        //                bp_amount AS amount,
        //                bp_chequeno AS chequeNo,
        //                bp_discount AS discount,
        //                bp_tds AS tds,
        //                bp_loc_entryno AS locEntryno,
        //                bp_sup_invno AS supInvno
        //                FROM acc_billwise_pur
        //                WHERE bp_voucher_name = '{VoucherName}' and bp_entryno = {EntryNo}
        //                ORDER BY bp_bill_date DESC;
        //            ";

        //            detail = usqlre.dbReaderFill(detailSql);



        //            return Ok(new
        //            {
        //                status = true,
        //                statusCode = 200,
        //                message = "Report generated successfully",
        //                data = new object[] {}
        //            });
        //        }
        //        else
        //        {
        //            return BadRequest(new
        //            {
        //                status = false,
        //                statusCode = 400,
        //                message = "Invalid voucher name",
        //                data = (object)null
        //            });
        //        }
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
        [HttpGet("get-invoice-wise-payment-and-receipt-voucher")]
        public async Task<IActionResult> GetPaymentIandReceiptIVoucher(string VoucherName, int EntryNo)
        {
            try
            {
                DataTable main = new DataTable();
                DataTable detail = new DataTable();
                UserSqlServer usqlre = new UserSqlServer(this);

                if (VoucherName == "PAYMENT-I" || VoucherName == "RECEIPT-I" || VoucherName == "CREDIT NOTE" || VoucherName == "DEBIT NOTE")
                {
                    // Main info query
                    string mainSql = $@"
                SELECT 
                    bei_entryno AS entryNo,
                    bei_voucher_name AS voucherName,
                    bei_date AS date,
                    bei_acc_id AS accountId,
                    a.as_name AS accountName,
                    bei_dr_cr_acc_id AS drCrAccId,
                    b.as_name AS drCrAccName,
                    bei_amount AS amount,
                    bei_sum_disc AS sumDisc,
                    bei_sum_tds AS sumTds,
                    bei_reference AS reference,
                    bei_net_amount AS netAmount,
                    bei_sgstp AS sgstp,
                    bei_cgstp AS cgstp,
                    bei_igstp AS igstp,
                    bei_sgst AS sgst,
                    bei_cgst AS cgst,
                    bei_igst AS igst,
                    bei_grand_total AS grandTotal,
                    bei_remarks AS remarks,
                    bei_salesman_id AS salasManId,
                    bei_approved AS approved,
                    bei_gst_status AS gstStatus,
                    bei_transfer_status AS transferStatus,
                    bei_scheme_status AS schemeStatus,
                    bei_irn AS irn,
                    bei_qr_link AS qrLink,
                    bei_json_insert AS jsonInsert,
                    bei_cancel_status AS cancelStatus,
                    bei_cost_id AS costId,
                    bei_roundoff AS roundOff,
                    (select CASE WHEN COALESCE(SUM(at_Cr)-SUM(at_Dr),0) > 0 THEN CAST(COALESCE(SUM(at_Cr)-SUM(at_Dr),0) as nvarchar(max)) + ' Cr' ELSE CAST(COALESCE(SUM(at_Dr)-SUM(at_Cr),0) as nvarchar(max)) + ' Dr' END AS balance from acc_account_transactions where at_as_id=bei_acc_id) as ob 
                FROM acc_billwise_inf
                LEFT JOIN acc_subhead a ON bei_acc_id = a.as_id
                LEFT JOIN acc_subhead b ON bei_dr_cr_acc_id = b.as_id
                WHERE bei_voucher_name = '{VoucherName}' and bei_entryno = {EntryNo}
                ORDER BY bei_date DESC;
            ";

                    main = usqlre.dbReaderFill(mainSql);

                    // Detail info query
                    string detailSql = $@"
                SELECT TOP 20 
                bp_entryno AS entryNo,
                bp_voucher_entryno AS voucherEntryno,
                bp_bill_date AS billDate,
                bp_voucher_type AS voucherType,
                bp_bill_amount AS billAmount,
                bp_bill_balance AS billBalance,
                bp_amount AS amount,
                bp_chequeno AS chequeNo,
                bp_discount AS discount,
                bp_tds AS tds,
                bp_loc_entryno AS locEntryno,
                bp_sup_invno AS supInvno
                FROM acc_billwise_pur
                WHERE bp_voucher_name = '{VoucherName}' and bp_entryno = {EntryNo}
                ORDER BY bp_bill_date DESC;
            ";

                    detail = usqlre.dbReaderFill(detailSql);

                    if (main.Rows.Count == 0)
                    {
                        return NotFound(new
                        {
                            status = false,
                            statusCode = 404,
                            message = "No records found",
                            data = (object)null
                        });
                    }

                    var mainRow = main.Rows[0];

                    var responseObject = new
                    {
                        entryNo = Convert.ToInt32(mainRow["entryNo"]),
                        voucherName = Convert.ToString(mainRow["voucherName"]),
                        date = Convert.ToDateTime(mainRow["date"]),
                        accountId = Convert.ToInt32(mainRow["accountId"]),
                        accountName = Convert.ToString(mainRow["accountName"]),
                        drCrAccId = Convert.ToInt32(mainRow["drCrAccId"]),
                        drCrAccName = Convert.ToString(mainRow["drCrAccName"]),
                        amount = Convert.ToDecimal(mainRow["amount"]),
                        sumDisc = Convert.ToDecimal(mainRow["sumDisc"]),
                        sumTds = Convert.ToDecimal(mainRow["sumTds"]),
                        reference = Convert.ToString(mainRow["reference"]),
                        netAmount = Convert.ToDecimal(mainRow["netAmount"]),
                        sgstp = Convert.ToDecimal(mainRow["sgstp"]),
                        cgstp = Convert.ToDecimal(mainRow["cgstp"]),
                        igstp = Convert.ToDecimal(mainRow["igstp"]),
                        sgst = Convert.ToDecimal(mainRow["sgst"]),
                        cgst = Convert.ToDecimal(mainRow["cgst"]),
                        igst = Convert.ToDecimal(mainRow["igst"]),
                        grandTotal = Convert.ToDecimal(mainRow["grandTotal"]),
                        remarks = Convert.ToString(mainRow["remarks"]),
                        salasManId = Convert.ToInt32(mainRow["salasManId"]),
                        approved = Convert.ToInt32(mainRow["approved"]),
                        gstStatus = Convert.ToInt32(mainRow["gstStatus"]),
                        transferStatus = Convert.ToInt32(mainRow["transferStatus"]),
                        schemeStatus = Convert.ToInt32(mainRow["schemeStatus"]),
                        irn = Convert.ToString(mainRow["irn"]),
                        qrLink = Convert.ToString(mainRow["qrLink"]),
                        jsonInsert = Convert.ToString(mainRow["jsonInsert"]),
                        cancelStatus = Convert.ToInt32(mainRow["cancelStatus"]),
                        costId = Convert.ToInt32(mainRow["costId"]),
                        roundOff = Convert.ToDecimal(mainRow["roundOff"]),
                        ob = Convert.ToString(mainRow["ob"]),
                        statement = "",
                        billWisePur = detail.AsEnumerable().Select(row => new
                        {
                            voucherEntryno = Convert.ToInt32(row["voucherEntryno"]),
                            billDate = Convert.ToString(row["billDate"]),
                            voucherType = Convert.ToString(row["voucherType"]),
                            billAmount = Convert.ToDecimal(row["billAmount"]),
                            billBalance = Convert.ToDecimal(row["billBalance"]),
                            amount = Convert.ToDecimal(row["amount"]),
                            chequeNo = Convert.ToString(row["chequeNo"]),
                            discount = Convert.ToDecimal(row["discount"]),
                            tds = Convert.ToDecimal(row["tds"]),
                            locEntryno = Convert.ToString(row["locEntryno"]),
                            supInvno = Convert.ToString(row["supInvno"]),
                            chkTick = true
                        }).ToList()
                    };

                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Report generated successfully",
                        data = responseObject
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Invalid voucher name",
                        data = (object)null
                    });
                }
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

        [HttpGet("get-voucher")]
        public async Task<IActionResult> GetVoucher(string VoucherName, int EntryNo)
        {
            try
            {
                DataTable main = new DataTable();
                DataTable detail = new DataTable();
                UserSqlServer usqlre = new UserSqlServer(this);

                bool salesmanWiseVoucher = usqlre.get_android_settings("SALESMANWISE_VOUCHER");
                bool showSalesmanOnly = usqlre.user_role != "ADMIN" && salesmanWiseVoucher;


                //string paymentWhere = showSalesmanOnly
                //    ? " WHERE pi_user = '" + usqlre.userId + "'"
                //    : "";

                string receiptWhere = showSalesmanOnly
                    ? " WHERE ri_salesman_id = '" + usqlre.gu_acc_id + "'"
                    : "";

                if (EntryNo == 0)
                {
                    if (VoucherName == "PAYMENT")
                    {
                        string mainSql = $@"
                SELECT TOP 20 
                    pi_entryno,
                    pi_creditaccount,
                    as_name as creditaccName,
                    pi_date, 
                    ISNULL(cast(pi_amount as nvarchar(100)), '0') pi_amount
                    FROM acc_pv_inf 
                    LEFT JOIN acc_subhead a ON pi_creditaccount = as_id
                    ORDER BY pi_entryno DESC;
            ";

                        main = usqlre.dbReaderFill(mainSql);

                        string detailSql = $@"
                SELECT TOP 20 
                    pp_rv_entryno,pp_account_id,ISNULL(as_name,'') as accName, pp_amount,pp_disc, pp_total,pp_remarks,pp_ob from acc_pv_par
                LEFT JOIN acc_subhead a ON pp_account_id = as_id
                ORDER BY pp_rv_entryno DESC;
                ";

                        detail = usqlre.dbReaderFill(detailSql);

                        var responseObject = main.AsEnumerable().Select(row => new
                        {
                            main = new
                            {
                                pi_entryno = row["pi_entryno"],
                                pi_creditaccount = row["pi_creditaccount"],
                                creditaccName = row["creditaccName"],
                                pi_date = row["pi_date"],
                                pi_amount = row["pi_amount"]
                            },
                            details = detail.AsEnumerable()
                                .Where(d => d["pp_rv_entryno"].ToString() == row["pi_entryno"].ToString())
                                .Select(d => new
                                {
                                    pp_rv_entryno = d["pp_rv_entryno"],
                                    pp_account_id = d["pp_account_id"],
                                    accName = d["accName"],
                                    pp_amount = d["pp_amount"],
                                    pp_disc = d["pp_disc"],
                                    pp_total = d["pp_total"],
                                    pp_remarks = d["pp_remarks"],
                                    pp_ob = d["pp_ob"]
                                }).ToList()
                        }).ToList();

                        return Ok(new
                        {
                            status = true,
                            statusCode = 200,
                            message = "voucher generated successfully",
                            data = responseObject
                        });
                    }
                    else if (VoucherName == "RECEIPT")
                    {
                        string mainSql = $@"
                SELECT TOP 20 
                    ri_entryno,
                    ri_debitaccount,
                    as_name as debitaccName,
                    ri_date, 
                    ISNULL(cast(ri_amount as nvarchar(100)), '0') ri_amount
                   FROM acc_rv_inf 
                    LEFT JOIN acc_subhead a ON ri_debitaccount = as_id
                    {receiptWhere}
                    order by ri_entryno desc;

            ";

                        main = usqlre.dbReaderFill(mainSql);

                        string detailSql = $@"
                SELECT 
                   rp_rv_entryno,
                   rp_account_id,
                   ISNULL(as_name,'') as accName,
                   rp_amount,
                   rp_disc,
                   rp_total,
                   rp_remarks,
                   rp_ob
                FROM acc_rv_par
                LEFT JOIN acc_subhead a ON rp_account_id = as_id
                WHERE rp_rv_entryno IN (
                    SELECT TOP 20 ri_entryno
                    FROM acc_rv_inf
                    {receiptWhere}
                    ORDER BY ri_entryno DESC
                )
                ORDER BY rp_rv_entryno DESC;
                ";

                        detail = usqlre.dbReaderFill(detailSql);

                        var responseObject = main.AsEnumerable().Select(row => new
                        {
                            main = new
                            {
                                ri_entryno = row["ri_entryno"],
                                ri_debitaccount = row["ri_debitaccount"],
                                debitaccName = row["debitaccName"],
                                ri_date = row["ri_date"],
                                ri_amount = row["ri_amount"]
                            },
                            details = detail.AsEnumerable()
                                .Where(d => d["rp_rv_entryno"].ToString() == row["ri_entryno"].ToString())
                                .Select(d => new
                                {
                                    rp_rv_entryno = d["rp_rv_entryno"],
                                    rp_account_id = d["rp_account_id"],
                                    accName = d["accName"],
                                    rp_amount = d["rp_amount"],
                                    rp_disc = d["rp_disc"],
                                    rp_total = d["rp_total"],
                                    rp_remarks = d["rp_remarks"],
                                    rp_ob = d["rp_ob"]
                                }).ToList()
                        }).ToList();

                        return Ok(new
                        {
                            status = true,
                            statusCode = 200,
                            message = "Report generated successfully",
                            data = responseObject
                        });
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            status = false,
                            statusCode = 400,
                            message = "Invalid voucher name",
                            data = (object)null
                        });
                    }
                }

                else
                {
                    if (VoucherName == "PAYMENT")
                    {
                        string mainSql = $@"
                SELECT 
                    pi_entryno,
                    pi_creditaccount,
                    as_name as creditaccName,
                    pi_date, 
                    ISNULL(cast(pi_amount as nvarchar(100)), '0') pi_amount
                    FROM acc_pv_inf 
                    LEFT JOIN acc_subhead a ON pi_creditaccount = as_id
                    WHERE pi_entryno ={EntryNo}
            ";

                        main = usqlre.dbReaderFill(mainSql);

                        string detailSql = $@"
                SELECT
                    pp_rv_entryno,pp_account_id,as_name as accName, pp_amount,pp_disc, pp_total,pp_remarks,pp_ob from acc_pv_par
                    LEFT JOIN acc_subhead a ON pp_account_id = as_id
                    WHERE pp_rv_entryno ={EntryNo}
            ";

                        detail = usqlre.dbReaderFill(detailSql);

                        var responseObject = main.AsEnumerable().Select(row => new
                        {
                            main = new
                            {
                                pi_entryno = row["pi_entryno"],
                                pi_creditaccount = row["pi_creditaccount"],
                                creditaccName = row["creditaccName"],
                                pi_date = row["pi_date"],
                                pi_amount = row["pi_amount"]
                            },
                            details = detail.AsEnumerable()
                            .Where(d => d["pp_rv_entryno"].ToString() == row["pi_entryno"].ToString())
                            .Select(d => new
                            {
                                pp_rv_entryno = d["pp_rv_entryno"],
                                pp_account_id = d["pp_account_id"],
                                accName = d["accName"],
                                pp_amount = d["pp_amount"],
                                pp_disc = d["pp_disc"],
                                pp_total = d["pp_total"],
                                pp_remarks = d["pp_remarks"],
                                pp_ob = d["pp_ob"]
                            }).ToList()
                        }).ToList();

                        return Ok(new
                        {
                            status = true,
                            statusCode = 200,
                            message = "Report generated successfully",
                            data = responseObject
                        });
                    }
                    else if (VoucherName == "RECEIPT")
                    {
                        string mainSql = $@"
                SELECT 
                    ri_entryno,
                    ri_debitaccount,
                    as_name as debitaccName,
                    ri_date, 
                    ISNULL(cast(ri_amount as nvarchar(100)), '0') ri_amount
                    FROM acc_rv_inf 
                    LEFT JOIN acc_subhead a ON ri_debitaccount = as_id
                    WHERE ri_entryno = {EntryNo}
                    {(showSalesmanOnly ? " AND ri_salesman_id = '" + usqlre.gu_acc_id + "'" : "")}
            ";

                        main = usqlre.dbReaderFill(mainSql);

                        string detailSql = $@"
                SELECT 
                   rp_rv_entryno,rp_account_id,as_name as accName, rp_amount,rp_disc, rp_total,rp_remarks,rp_ob from acc_rv_par
                    LEFT JOIN acc_subhead a ON rp_account_id = as_id
                    WHERE rp_rv_entryno = {EntryNo}
            ";

                        detail = usqlre.dbReaderFill(detailSql);

                        var responseObject = main.AsEnumerable().Select(row => new
                        {
                            main = new
                            {
                                ri_entryno = row["ri_entryno"],
                                ri_debitaccount = row["ri_debitaccount"],
                                debitaccName = row["debitaccName"],
                                ri_date = row["ri_date"],
                                ri_amount = row["ri_amount"]
                            },
                            details = detail.AsEnumerable()
                                .Where(d => d["rp_rv_entryno"].ToString() == row["ri_entryno"].ToString())
                                .Select(d => new
                                {
                                    rp_rv_entryno = d["rp_rv_entryno"],
                                    rp_account_id = d["rp_account_id"],
                                    accName = d["accName"],
                                    rp_amount = d["rp_amount"],
                                    rp_disc = d["rp_disc"],
                                    rp_total = d["rp_total"],
                                    rp_remarks = d["rp_remarks"],
                                    rp_ob = d["rp_ob"]
                                }).ToList()
                        }).ToList();

                        return Ok(new
                        {
                            status = true,
                            statusCode = 200,
                            message = "Report generated successfully",
                            data = responseObject
                        });
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            status = false,
                            statusCode = 400,
                            message = "Invalid voucher name",
                            data = (object)null
                        });
                    }
                }
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
        [HttpGet("get-invoice-wise-payment-and-receipt-voucher-entryno")]
        public string getRecieptAndPaymentInvoiceEntryNo(string VoucherName)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = @"SELECT ISNULL(MAX(bei_entryno), 0) AS max_entry_no, ISNULL(MIN(bei_entryno), 0) AS min_entry_no 
                   FROM acc_billwise_inf 
                   WHERE bei_voucher_name = '" + VoucherName + "'";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();

            // Create a manual list of objects with int types
            var list = new List<object>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new
                {
                    max_entry_no = Convert.ToInt32(row["max_entry_no"]),
                    min_entry_no = Convert.ToInt32(row["min_entry_no"])
                });
            }

            // Now serialize
            return System.Text.Json.JsonSerializer.Serialize(list);

        }
        [HttpPost("search-invoice-payment")]
        public async Task<IActionResult> searchInvoicePayment([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = "";
            if (usqlre.user_role == "ADMIN")
            {

                if (model.Value == "")
                {
                    sql = "select top 10 cast(bei_entryno as Int) as invoice_id, as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total  from acc_billwise_inf  inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='PAYMENT-I' order by bei_entryno desc";
                }
                else
                {
                    sql = "select cast(bei_entryno as Int) as invoice_id,as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total from acc_billwise_inf inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='PAYMENT-I' and bei_entryno = '" + model.Value + "'";
                }
            }
            else
            {
                if (model.Value == "")
                {
                    sql = "select top 10 cast(bei_entryno as Int) as invoice_id,as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total from acc_billwise_inf inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='PAYMENT-I' and bei_user_id='" + usqlre.userId + "' order by bei_entryno desc";
                }
                else
                {
                    sql = "select cast(bei_entryno as Int) as invoice_id,as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total from acc_billwise_inf inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='PAYMENT-I' and bei_entryno = '" + model.Value + "' and bei_user_id='" + usqlre.userId + "'";
                }
            }

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }
        //[HttpPost("search-invoice-receipt")]
        //public async Task<IActionResult> searchInvoiceReceipt([FromBody] Params model)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);

        //    string sql = "";
        //    if (usqlre.user_role == "ADMIN")
        //    {

        //        if (model.Value == "")
        //        {
        //            sql = "select top 10 cast(bei_entryno as Int) as invoice_id, as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total  from acc_billwise_inf  inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='RECEIPT-I' order by bei_entryno desc";
        //        }
        //        else
        //        {
        //            sql = "select cast(bei_entryno as Int) as invoice_id,as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total from acc_billwise_inf inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='RECEIPT-I' and bei_entryno = '" + model.Value + "'";
        //        }
        //    }
        //    else
        //    {
        //        if (model.Value == "")
        //        {
        //            sql = "select top 10 cast(bei_entryno as Int) as invoice_id,as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total from acc_billwise_inf inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='RECEIPT-I' and bei_user_id='" + usqlre.userId + "' order by bei_entryno desc";
        //        }
        //        else
        //        {
        //            sql = "select cast(bei_entryno as Int) as invoice_id,as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total from acc_billwise_inf inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='RECEIPT-I' and bei_entryno = '" + model.Value + "' and bei_user_id='" + usqlre.userId + "'";
        //        }
        //    }

        //    DataTable dt = usqlre.dbReaderFill(sql);
        //    usqlre.close();
        //    return Ok(ReportModelContext.searializeDt(dt));
        //}

        [HttpPost("search-invoice-receipt")]
        public async Task<IActionResult> searchInvoiceReceipt([FromBody] Params model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            if (model.Value == "")
            {
                if (usqlre.get_gnl_settings("LOCATION ENTRY NO") && usqlre.userId != "1")
                {
                    sql = "select top 10 cast(bei_entryno as Int) as invoice_id, as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total  from acc_billwise_inf  inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='RECEIPT-I' and bei_location_id=" + usqlre.locationId + " order by bei_entryno desc";
                }
                else
                {
                    sql = "select top 10 cast(bei_entryno as Int) as invoice_id, as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total  from acc_billwise_inf  inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='RECEIPT-I' order by bei_entryno desc";
                }
            }
            else
            {
                if (usqlre.get_gnl_settings("LOCATION ENTRY NO") && usqlre.userId != "1")
                {
                    sql = "select top 10 cast(bei_entryno as Int) as invoice_id, as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total  from acc_billwise_inf  inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='RECEIPT-I' and bei_entryno = " + model.Value + " and bei_location_id=" + usqlre.locationId + "";
                }
                else
                {
                    sql = "select top 10 cast(bei_entryno as Int) as invoice_id, as_name as customer,cast(bei_date as date) as bei_date,bei_grand_total  from acc_billwise_inf  inner join acc_subhead on bei_acc_id = as_id where bei_voucher_name='RECEIPT-I' and bei_entryno = " + model.Value + "";
                }
            }

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }
        [HttpPost("get-payment-receipt-voucher")]
        public async Task<IActionResult> getPaymentAndReceiptVoucher(string voucherName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(voucherName))
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Voucher name is required",
                        data = new object []{},
                    });
                }

                UserSqlServer usqlre = new UserSqlServer(this);
                string sql = "";

                if (voucherName.ToUpper() == "PAYMENT")
                {
                    sql = "select top 20 cast(pi_entryno as Int) as entryNo, cast(pi_date as date) as date, pi_total as total from acc_pv_inf order by pi_entryno desc";
                }
                else if (voucherName.ToUpper() == "RECEIPT")
                {
                    sql = "select top 20 cast(ri_entryno as Int) as entryNo, cast(ri_date as date) as date, ri_total as total from acc_rv_inf order by ri_entryno desc";
                }
                else
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Invalid voucher name. Use 'PAYMENT' or 'RECEIPT'.",
                        data = new object[] { },
                    });
                }

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                var response=new
                {
                    status = true,
                    statusCode = 200,
                    message = "Report generated successfully",
                    data = dt
                };
                return Content(JsonConvert.SerializeObject(response), "application/json");

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "An error occurred while generating the report"+ ex.Message,
                    data = new object[] { },
                });
            }
        }
        [HttpGet("get-credit-and-debit")]
        public async Task<IActionResult> GetCreditandDebit(string VoucherName, int EntryNo)
        {
            try
            {
                DataTable main = new DataTable();
                DataTable detail = new DataTable();
                UserSqlServer usqlre = new UserSqlServer(this);
                if (EntryNo == 0)
                {
                    if (VoucherName == "CREDIT NOTE")
                    {
                        string mainSql = @"SELECT TOP 25 bei_entryno as EntryNo,bei_date as [Date],bei_acc_id as AccountId,a.as_name as AccountName,bei_dr_cr_acc_id as DrCrAccId ,b.as_name as DrCrAccName, bei_amount as Amount,bei_reference as Reference, bei_net_amount as NetAmount, bei_grand_total as GrandTotal, bei_user_id as UserId, gu_name as UserName,bei_location_id as LocationId, gl_name as LocationName  FROM acc_billwise_inf 
                        LEFT JOIN acc_subhead a ON bei_acc_id = a.as_id
						LEFT JOIN acc_subhead b ON bei_dr_cr_acc_id = b.as_id
						LEFT JOIN gnl_users  ON gu_user_id = bei_user_id
						LEFT JOIN gnl_location  ON gl_id = bei_location_id
						where bei_voucher_name= '"+ VoucherName + @"'
                        ORDER BY bei_entryno DESC;
                    ";

                        main = usqlre.dbReaderFill(mainSql);


                        var response = new
                        {
                            status = true,
                            statusCode = 200,
                            message = "voucher generated successfully",
                            data = main
                        };
                        return Content(JsonConvert.SerializeObject(response), "application/json");

                    }
                    else if (VoucherName == "DEBIT NOTE")
                    {
                        string mainSql = @"SELECT TOP 25 bei_entryno as EntryNo,bei_date as [Date],bei_acc_id as AccountId,a.as_name as AccountName,bei_dr_cr_acc_id as DrCrAccId ,b.as_name as DrCrAccName, bei_amount as Amount,bei_reference as Reference, bei_net_amount as NetAmount, bei_grand_total as GrandTotal, bei_user_id as UserId, gu_name as UserName,bei_location_id as LocationId, gl_name as LocationName  FROM acc_billwise_inf 
                        LEFT JOIN acc_subhead a ON bei_acc_id = a.as_id
						LEFT JOIN acc_subhead b ON bei_dr_cr_acc_id = b.as_id
						LEFT JOIN gnl_users  ON gu_user_id = bei_user_id
						LEFT JOIN gnl_location  ON gl_id = bei_location_id
						where bei_voucher_name= '" + VoucherName + @"'
                        ORDER BY bei_entryno DESC;

                    ";

                        main = usqlre.dbReaderFill(mainSql);



                        var response = new
                        {
                            status = true,
                            statusCode = 200,
                            message = "Report generated successfully",
                            data = main
                        };
                        return Content(JsonConvert.SerializeObject(response), "application/json");
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            status = false,
                            statusCode = 400,
                            message = "Invalid voucher name",
                            data = (object)null
                        });
                    }
                }

                else
                {
                    if (VoucherName == "CREDIT NOTE")
                    {
                        string mainSql = @"SELECT TOP 25 bei_entryno as EntryNo,bei_date as [Date],bei_acc_id as AccountId,a.as_name as AccountName,bei_dr_cr_acc_id as DrCrAccId ,b.as_name as DrCrAccName, bei_amount as Amount,bei_reference as Reference, bei_net_amount as NetAmount, bei_grand_total as GrandTotal, bei_user_id as UserId, gu_name as UserName,bei_location_id as LocationId, gl_name as LocationName  FROM acc_billwise_inf 
                        LEFT JOIN acc_subhead a ON bei_acc_id = a.as_id
						LEFT JOIN acc_subhead b ON bei_dr_cr_acc_id = b.as_id
						LEFT JOIN gnl_users  ON gu_user_id = bei_user_id
						LEFT JOIN gnl_location  ON gl_id = bei_location_id
						where bei_voucher_name= '" + VoucherName + @"' and bei_entryno= "+EntryNo+@"
                        ORDER BY bei_entryno DESC;
                    ";

                        main = usqlre.dbReaderFill(mainSql);

                        if (main.Rows.Count > 0)
                        {
                            var row = main.Rows[0];
                            var resultObject = main.Columns.Cast<DataColumn>()
                                .ToDictionary(col => col.ColumnName, col => row[col]);

                            var response = new
                            {
                                status = true,
                                statusCode = 200,
                                message = "Report generated successfully",
                                data = resultObject
                            };
                            return Content(JsonConvert.SerializeObject(response), "application/json");
                        }
                        else
                        {
                            var response = new
                            {
                                status = false,
                                statusCode = 404,
                                message = "No data found",
                                data = (object)null
                            };
                            return Content(JsonConvert.SerializeObject(response), "application/json");
                        }

                    }
                    else if (VoucherName == "DEBIT NOTE")
                    {
                        string mainSql = @"SELECT TOP 25 bei_entryno as EntryNo,bei_date as [Date],bei_acc_id as AccountId,a.as_name as AccountName,bei_dr_cr_acc_id as DrCrAccId ,b.as_name as DrCrAccName, bei_amount as Amount,bei_reference as Reference, bei_net_amount as NetAmount, bei_grand_total as GrandTotal, bei_user_id as UserId, gu_name as UserName,bei_location_id as LocationId, gl_name as LocationName  FROM acc_billwise_inf 
                        LEFT JOIN acc_subhead a ON bei_acc_id = a.as_id
						LEFT JOIN acc_subhead b ON bei_dr_cr_acc_id = b.as_id
						LEFT JOIN gnl_users  ON gu_user_id = bei_user_id
						LEFT JOIN gnl_location  ON gl_id = bei_location_id
						where bei_voucher_name= '" + VoucherName + @"' and bei_entryno= " + EntryNo + @"
                        ORDER BY bei_entryno DESC;";
                        main = usqlre.dbReaderFill(mainSql);

                        if (main.Rows.Count > 0)
                        {
                            var row = main.Rows[0];
                            var resultObject = main.Columns.Cast<DataColumn>()
                                .ToDictionary(col => col.ColumnName, col => row[col]);

                            var response = new
                            {
                                status = true,
                                statusCode = 200,
                                message = "Report generated successfully",
                                data = resultObject
                            };
                            return Content(JsonConvert.SerializeObject(response), "application/json");
                        }
                        else
                        {
                            var response = new
                            {
                                status = false,
                                statusCode = 404,
                                message = "No data found",
                                data = (object)null
                            };
                            return Content(JsonConvert.SerializeObject(response), "application/json");
                        }

                    }
                    else
                    {
                        return BadRequest(new
                        {
                            status = false,
                            statusCode = 400,
                            message = "Invalid voucher name",
                            data = (object)null
                        });
                    }
                }
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
    }
}
