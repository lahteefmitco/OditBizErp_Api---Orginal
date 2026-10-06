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


namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class HrController : Controller
    {
        [HttpGet("hostel-leaving-request")]
        public IActionResult hostelleavingrequest()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";

            sql = "EXEC  [dbo].[Sp_hr_hostel_out_entry] " +
                   "@user_role = '" + usqlre.user_role + "', " +
                   "@StatementType = N'hostel_leaving_request'";
            DataTable dt = usqlre.dbReaderFill(sql);
            hash.Add("pending", dt);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(hash));

        }
        [HttpPost("hostel-leaving-status")]
        public async Task<IActionResult> hostelleavingstatus([FromBody] HrModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            var messag = "";
            string sql = "";

            sql = "EXEC  [dbo].[Sp_hr_hostel_out_entry] " +
                    "@user_role = '" + usqlre.user_role + "', " +
                    "@hhoe_entryno = " + model.hhoe_entryno + ", " +
                    "@StatementType = N'hostel_leaving_status'";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            messag = dt.Rows.Count > 0 ? "true" : "false";
            return Ok(new { status = messag });

        }

        [HttpPost("save-leave")]
        public async Task<IActionResult> saveleave([FromBody] LeaveApplyModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sqlempid = "select as_id from acc_employees where as_ap_id=" + usqlre.gu_acc_id;
            DataTable dt_empid = usqlre.dbReaderFill(sqlempid);
            int approver = model.hla_leave_entryno == 0 ? 0 : Convert.ToInt32(dt_empid.Rows[0]["as_id"].ToString());
            string sql = "";
            sql = @"DECLARE	@return_value int,
		@return int
        EXEC	@return_value = [dbo].[Sp_hr_leave_apply]
		@hla_leave_entryno =" + model.hla_leave_entryno + " , " +
        "@hla_leave_entrydate = '" + model.hla_leave_entrydate + "', " +
        "@hla_leave_empid =" + model.hla_leave_empid + "  , " +
        "@hla_hlc_id =" + model.hla_hlc_id + " , " +
        "@hla_leave_from_date = '" + model.hla_leave_from_date + "' ," +
        "@hla_leave_to_date = '" + model.hla_leave_from_date + "' ," +
        "@hla_leave_half_full_day =  " + model.hla_leave_half_full_day + " ," +
        "@hla_leave_reason = '" + model.hla_leave_reason + "' ," +
        "@hla_leave_status ='" + model.hla_leave_status + "'," +
        "@hla_leave_approver_empid = " + approver + " ," +
        "@hla_leave_days = '" + model.hla_leave_days + "'  ," +
        "@hla_leave_userid = '" + usqlre.userId + "'  ," +
       "@return = @return OUTPUT," +
        "@StatementType = '" + model.statement + "'";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            int entryNo = Convert.ToInt32(dt.Rows[0]["entry_no"].ToString());
            return Ok(new { status = entryNo > 0 ? true : false, entryNo = entryNo });
        }

        [HttpGet("leave-request-data")]
        public IActionResult LeaveCategoryData()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";

            sql = "select [hlc_id] as value,[hlc_name] as label from [dbo].[hr_leave_category] where [hlc_active]=1";
            string sql_emp = "select as_id as value,as_name as label from acc_employees";
            DataTable dt = usqlre.dbReaderFill(sql);
            DataTable dt_emp = usqlre.dbReaderFill(sql_emp);
            hash.Add("category", dt);
            hash.Add("employees", dt_emp);
            usqlre.close();
            DataTable StatusDT = new DataTable();
            StatusDT.Columns.Add("value");
            StatusDT.Columns.Add("label");
            StatusDT.Rows.Add("APPROVED", "APPROVED");
            StatusDT.Rows.Add("REJECT", "REJECT");
            StatusDT.Rows.Add("CANCEL", "CANCEL");
            StatusDT.Rows.Add("OPEN", "OPEN");
            hash.Add("status", StatusDT);
            return Ok(ReportModelContext.searializeDt(hash));

        }
        [HttpGet("leave-request/{entryNo}")]
        public async Task<IActionResult> leaveRequestByEntry(int entryNo)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";

            sql = "EXEC  [dbo].[Sp_hr_leave_apply] " +
                   "@hla_leave_entryno = " + entryNo + ", " +
                    "@hla_leave_approver_empid = " + usqlre.gu_acc_id + ", " +
                   "@StatementType = N'leave_request_by_entryno'";
            DataSet DS = usqlre.dbreadDataset(sql);
            hash.Add("Request", DS.Tables[0]);
            hash.Add("count", DS.Tables[1]);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(hash));

        }
        [HttpPost("leave-request")]
        public async Task<IActionResult> leaveRequest([FromBody] LeaveApplyModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";
            string sqlwhere = "";
            int approver = 0;
            string sqlempid = "select as_id from acc_employees where as_ap_id=" + usqlre.gu_acc_id;
            DataTable dt_empid = usqlre.dbReaderFill(sqlempid);
            if (dt_empid.Rows.Count > 0)
            {
                approver = Convert.ToInt32(dt_empid.Rows[0]["as_id"].ToString());
            }
            if (model.hla_leave_entrydate != null)
                sqlwhere = " and cast(hla_leave_entrydate as date)=''" + model.hla_leave_entrydate + "''";
            if (model.hla_leave_from_date != null)
            {
                if (sqlwhere != "")
                    sqlwhere += " and cast(hla_leave_from_date as date)>=''" + model.hla_leave_from_date + "'' and cast(hla_leave_to_date as date)<=''" + model.hla_leave_to_date + "''";
                else
                    sqlwhere = " and cast(hla_leave_from_date as date)>=''" + model.hla_leave_from_date + "'' and cast(hla_leave_to_date as date)<=''" + model.hla_leave_to_date + "''";
            }
            if (model.name != null)
            {
                if (sqlwhere != "")
                    sqlwhere += " and emp.as_name like ''%" + model.name + "%''";
                else
                    sqlwhere = " and emp.as_name like ''%" + model.name + "%''";
            }
            sql = "EXEC  [dbo].[Sp_hr_leave_apply] " +
            "@hla_leave_approver_empid = " + approver + ", " +
                   "@sqlwhere = '" + sqlwhere + "', " +
                   "@StatementType = N'leave_request'";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));

        }
        [HttpPost("employee-performance")]
        public async Task<IActionResult> empperformanceInsert([FromBody] EmployeePerformanceModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            int entryNo = usqlre.empSave("Insert", model);
            return Ok(new { status = entryNo > 0 ? true : false,msg= entryNo > 0 ? "Saved Successfully" : "There is no job card" });
        }
        [HttpPost("employee-performance-update")]
        public async Task<IActionResult> empperformanceUpdate([FromBody] EmployeePerformanceModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            int ret = usqlre.empSave("Update", model);
            return Ok(new { status = ret > 0 ? true : false, msg = ret > 0 ? "Saved Successfully" : "There is no job card" });
        }
        [HttpPost("employee-performance-filter")]
        public async Task<IActionResult> performancefilter([FromBody] EmployeePerformanceModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";
            string sqlwhere = "";
           // sqlwhere = " where cast(hep_start_date as date)>=''" + model.hep_start_date+ "'' and cast(hep_end_date as date) <=''" + model.hep_end_date+"''";
            if(model.emp_name!=null&& model.emp_name != "")
            {
                sqlwhere+=" and as_name=''"+model.emp_name+"''";
            }
            if(model.hep_status!=null&& model.hep_status != "")
            {
                sqlwhere += " and hep_status=''" + model.hep_status + "''";
            }
            if(model.hep_id!=0)
            {
                sqlwhere += " and hep_id=" + model.hep_id + "";
            }
            sql = "EXEC  [dbo].[Sp_hr_employee_performance] " +
            "@hep_start_date = '" + model.hep_start_date + "', " +
            "@hep_end_date = '" + model.hep_end_date + "', " +
            "@sqlwhere = '" + sqlwhere + "', " +
                   "@StatementType = N'Search'";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));

        }
        [HttpGet("employee-performance/{id}")]
        public async Task<IActionResult> select(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
           
            string sql = "";

            sql = "EXEC  [dbo].[Sp_hr_employee_performance] " +
                   "@hep_id = " + id + ", " +
                  "@StatementType = N'Select'";
            DataTable dt = usqlre.dbReaderFill(sql);
         
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));

        }
    }
}
