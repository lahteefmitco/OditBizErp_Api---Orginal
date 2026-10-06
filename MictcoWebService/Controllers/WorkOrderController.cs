using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    public class WorkOrderController : Controller
    {
        [HttpPost("work-orders")]
        public async Task<IActionResult> workorderReport([FromBody] WorkOrder model)
        {
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            UserSqlServer usqlre = new UserSqlServer(this);
            usqlre = new UserSqlServer(this);
            string _where = "";
            string _wherecommoision = "";
            string sql_str = "";
            if (model.value.Length >= 10)
            {
                _where = "and si_add2=''" + model.value + "''";
              
            }
            else if (model.value.Length > 0)
            {
                _where = "and si_entryno=" + model.value + "";
               
            }
            bool WORKORDERUSERWISE = false;
            DataTable dt_WORKORDERUSERWISE = usqlre.dbReaderFill("select ans_status from android_settings where ans_name = 'WORK ORDER USERWISE'");
            WORKORDERUSERWISE = Convert.ToBoolean(Convert.ToInt32(dt_WORKORDERUSERWISE.Rows[0][0].ToString()));
            if (WORKORDERUSERWISE)
            {

                if (usqlre.user_role != "ADMIN")
                {
                    _wherecommoision = _where + " and si_commision_acc_id=" + usqlre.gu_acc_id + "";
                    _where = _where + " and woh_salesman_id=" + usqlre.gu_acc_id + "";
                  
                }
            }
          
                sql_str = "EXEC	 [dbo].[Sp_workorder] " +
 "@sqlwhere = '" + _where + "'," +
 "@sqlwhere1 = '" + _wherecommoision + "'," +
 "@wi_order_date = '" + model.date + "'," +
 "@StatementType = 'workOrders'";

            DataSet main_ds = usqlre.dbreadDataset(sql_str);
            hash.Add("received", main_ds.Tables[0]);
            hash.Add("pending", main_ds.Tables[1]);
            hash.Add("finished", main_ds.Tables[2]);
            hash.Add("delivered", main_ds.Tables[3]);
            hash.Add("returned", main_ds.Tables[4]);

            return Ok(ReportModelContext.searializeDt(hash));
        }

        [HttpPost("work-finished")]
        public async Task<IActionResult> workfinished([FromBody] WorkOrder model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            bool WORKORDERUSERWISE = false;
            DataTable dt_WORKORDERUSERWISE = usqlre.dbReaderFill("select ans_status from android_settings where ans_name = 'WORK ORDER USERWISE'");
            WORKORDERUSERWISE = Convert.ToBoolean(Convert.ToInt32(dt_WORKORDERUSERWISE.Rows[0][0].ToString()));
            string _where = "";
            string query = "";
            if (model.value.Length >= 10)
            {
                _where = "and si_add1=''" + model.value + "''";
            }
            else if (model.value.Length > 0)
            {
                _where = "and si_entryno=" + model.value + "";
            }
            else
            {
                _where = "and cast(si_finished_date as date)=''" + model.date + "''";
            }

            if (WORKORDERUSERWISE)
            { 
                if(usqlre.user_role != "ADMIN")
                {
                    _where = _where + " and woh_salesman_id=" + usqlre.gu_acc_id + "";
                }
            }

                query = "EXEC	 [dbo].[Sp_workorder] " +
             "@sqlwhere = '" + _where + "'," +
             "@StatementType = 'workOrderFinished'";

            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }


        [HttpPost("work-finished-search")]
        public async Task<IActionResult> workfinishedsearch([FromBody] WorkOrder model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string query = @"select si_entryno as orderID,si_cust_name as CustName,CASE WHEN si_acc_id=1 THEN si_add1 ELSE si_add2 END as mobile,si_grand_total as amt,'2022-02-05' as finishesDate,'05.00 am' as finishedTime from inv_sales_inf where si_finish='FINISHED'  and si_str_id=3 and  si_add2='" + model.orderId + "' or si_entryno=" + model.orderId + "";
            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }
        [HttpPost("Work-order-customer-details")]
        public async Task<IActionResult> Workordercustomerdetails([FromBody] WorkOrder model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string query = @"select si_cust_name,si_add1,si_add2 from inv_sales_inf where si_str_id=3 and si_add1='" + model.mob + "' or si_add2='" + model.mob + "'";
            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }
        [HttpPost("save-brand")]
        public async Task<IActionResult> savebrand([FromBody] WorkOrder model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string query = @"select * from inv_company where  company_name='" + model.companyName + "'";
            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            if (dt.Rows.Count > 0)
            {
                return Ok(new { status = false, message = "Already Saved" });
            }
            else
            {
                string _sql = "insert into inv_company(company_name)values ('" + model.companyName + "') SELECT SCOPE_IDENTITY()";
                DataTable dt_insert = usqlre.dbReaderFill(_sql);
                if (dt_insert.Rows.Count > 0)
                {
                    int inserId = Convert.ToInt32(dt_insert.Rows[0]["Column1"].ToString());
                    usqlre.close();
                    return Ok(new { status = true, message = "Saved Successfully", brandId = inserId });
                }
                else
                {
                    return Ok(new { status = false, message = "Try Again" });
                }

            }

        }
        [HttpPost("save-model")]
        public async Task<IActionResult> savemodel([FromBody] WorkOrder model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string query = @"select * from inv_brand where bra_name='" + model.modelName + "' and bra_company_id=" + model.braCompanyId + "";
            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            if (dt.Rows.Count > 0)
            {
                return Ok(new { status = false, message = "Already Saved" });
            }
            else
            {
                string _sql = "insert into inv_brand(bra_name,bra_company_id)values ('" + model.modelName + "'," + model.braCompanyId + ") SELECT SCOPE_IDENTITY()";
                DataTable dt_insert = usqlre.dbReaderFill(_sql);
                if (dt_insert.Rows.Count > 0)
                {
                    int inserId = Convert.ToInt32(dt_insert.Rows[0]["Column1"].ToString());
                    usqlre.close();
                    return Ok(new { status = true, message = "Saved Successfully", modelId = inserId });
                }
                else
                {
                    return Ok(new { status = false, message = "Try Again" });
                }
            }

        }
        [HttpPost("save-color")]
        public async Task<IActionResult> savecolor([FromBody] WorkOrder model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string query = @"select * from inv_color where  clr_name='" + model.color + "'";
            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            if (dt.Rows.Count > 0)
            {
                return Ok(new { status = false, message = "Already Saved" });
            }
            else
            {
                string _sql = "insert into inv_color(clr_name)values ('" + model.color + "') SELECT SCOPE_IDENTITY()";
                DataTable dt_insert = usqlre.dbReaderFill(_sql);
                if (dt_insert.Rows.Count > 0)
                {
                    int inserId = Convert.ToInt32(dt_insert.Rows[0]["Column1"].ToString());
                    usqlre.close();
                    return Ok(new { status = true, message = "Saved Successfully", colorId = inserId });
                }
                else
                {
                    return Ok(new { status = false, message = "Try Again" });
                }
            }

        }
        [HttpPost("work-report-count")]
        public async Task<IActionResult> workReportPost([FromBody] WorkOrder model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string _where = "";
            string query = "";
            bool WORKORDERUSERWISE = false;
            DataTable dt_WORKORDERUSERWISE = usqlre.dbReaderFill("select ans_status from android_settings where ans_name = 'WORK ORDER USERWISE'");
            WORKORDERUSERWISE = Convert.ToBoolean(Convert.ToInt32(dt_WORKORDERUSERWISE.Rows[0][0].ToString()));
            int userId = 0;
            if (WORKORDERUSERWISE)
            {

                if (usqlre.user_role != "ADMIN")
                {
                    userId = Convert.ToInt32(usqlre.gu_acc_id);
                       
                }
            }
            
       query = "EXEC	 [dbo].[Sp_workorder] " +
                  "@wi_to_date = '" + model.toDate + "'," +
                  "@wi_user_id = '" +userId + "'," +
                  "@wi_from_date = '" + model.fromDate + "'," +
                  "@StatementType = 'WorkReportCount'";

            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            //return Ok(ReportModelContext.searializeDt(dt));
            return Ok(new
            {
                toDate = model.toDate,
                fromDate = model.fromDate,
                received = dt.Rows[0]["received"].ToString(),
                finished = dt.Rows[0]["finished"].ToString(),
                pending = dt.Rows[0]["pending"].ToString(),
                delivered = dt.Rows[0]["delivered"].ToString(),
                returned = dt.Rows[0]["returned"].ToString()
            });
        }

        [HttpPost("work-report")]
        public async Task<IActionResult> workReport([FromBody] WorkOrder model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string _where = "";
            string query = "";
            if (model.value == "Received")
            {
                _where += " and cast(woh_date as date)<=''" + model.toDate + "'' and woh_ws_id=1";
            }
            if (model.value == "Pending")
            {
                _where += " and cast(woh_date as date)<=''" + model.toDate + "'' and woh_ws_id in(2,3,4,6)";
            }
            if (model.value == "Finished")
            {
                _where += " and cast(woh_date as date)<=''" + model.toDate + "'' and cast(woh_date as date)>=''" + model.fromDate + "'' and woh_ws_id in(5,8,9) and woh_delivery_status!=''DELIVERED''";
            }
            if (model.value == "Returned")
            {
                _where += " and cast(woh_date as date)<=''" + model.toDate + "'' and cast(woh_date as date)>=''" + model.fromDate + "'' and woh_ws_id=7";
            }
            if (model.value == "Delivered")
            {
                _where = " and cast(woh_date as date)<=''" + model.toDate + "'' and cast(woh_date as date)>=''" + model.fromDate + "'' and woh_delivery_status=''DELIVERED''";
            }
            if (model.userId > 0)
            {
                if(model.value=="Received")
                     _where += " and si_commision_acc_id=" + model.userId + "";
                else
                    _where += " and woh_salesman_id=" + model.userId + "";
            }

            query = "EXEC	 [dbo].[Sp_workorder] " +
                 "@sqlwhere = '" + _where + "'," +
                 //"@wi_to_date = '" + model.toDate + "'," +
                 //"@wi_from_date = '" + model.fromDate + "'," +
                "@StatementType = 'WorkReport'";

            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }
        [HttpPost("work-order-assigned")]
        public async Task<IActionResult> workorderassigned([FromBody] WorkOrder model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string query = @"select top 1 * from inv_workorder_history where woh_entryno=" + model.orderId + "";
            string _sql = "";
            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            if(Convert.ToInt32(dt.Rows[0]["woh_ws_id"].ToString())==1)
            {
                _sql = @"insert into inv_workorder_history(woh_date,woh_entryno,woh_ws_id,woh_delivery_status,woh_action,woh_user_id,woh_salesman_id)values('" + model.date + "'," + model.orderId + "," + 2 + ",'" + dt.Rows[0]["woh_delivery_status"] + "','Update'," + usqlre.userId + "," + model.assignTo + ")" +
                    " update inv_sales_inf set si_finish = 2,si_assign_to="+ model.assignTo + " where si_str_id = 3 and si_entryno =" + model.orderId +"";
            }
            else
            {
              _sql = "insert into inv_workorder_history(woh_date,woh_entryno,woh_ws_id,woh_delivery_status,woh_action,woh_user_id,woh_salesman_id)values('" + model.date + "'," + model.orderId + "," + dt.Rows[0]["woh_ws_id"] + ",'" + dt.Rows[0]["woh_delivery_status"] + "','Update'," + usqlre.userId + "," + model.assignTo + ")";
            }
            
            usqlre.dbReaderFill(_sql);
            usqlre.close();
            string query1 = @"select top 1 *,iws_name from inv_workorder_history left join inv_work_order_status_reg on CONVERT(nvarchar(50),iws_id)=woh_ws_id  where woh_entryno=" + model.orderId + " ORDER BY woh_id desc";
            DataTable dt_workstaus=usqlre.dbReaderFill(query1);
            usqlre.close();

            return Ok(new { status = true, message = "Saved Successfully" , woh_ws_id = dt_workstaus.Rows[0]["woh_ws_id"], iws_name = dt_workstaus.Rows[0]["iws_name"] });

        }
        [HttpPost("work-report-user-count")]
        public async Task<IActionResult> workreportusercount([FromBody] WorkOrder model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string query = "EXEC	 [dbo].[Sp_workorder] " +
                    "@wi_to_date = '" + model.toDate + "'," +
                    "@wi_from_date = '" + model.fromDate + "'," +
                    "@wi_user_id = " + model.userId + "," +
                    "@StatementType = 'WorkReportUserWise'";
            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));

        }
        [HttpGet("work-order/{id}")]
        public async Task<IActionResult> workorderbyid(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string query = "EXEC	 [dbo].[Sp_workorder] " +
                           "@wi_orderno = " + id + "," +
                           "@StatementType = 'workOrderbyId'";
            DataSet main_ds = usqlre.dbreadDataset(query);
            DataTable dt_main_inf = main_ds.Tables["Table"];
            //DataTable dt_collected = main_ds.Tables["Table1"];
            DataTable dt_complaints = main_ds.Tables["Table1"];
            DataTable dt_products = main_ds.Tables["Table2"];

            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            hash.Add("data", dt_main_inf);
            //hash.Add("collecteditems", dt_collected);
            hash.Add("complaints", dt_complaints);
            hash.Add("products", dt_products);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(hash));

        }

    }
}
