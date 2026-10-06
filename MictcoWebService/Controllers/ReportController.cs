using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    
    [Authorize(Roles = UserRoles.User)]
    public class ReportController : ControllerBase
    {
        [HttpGet]
        [Route("getledger")]
        public async Task<ActionResult> getLedger(LedgerModel ledferModel)
        {
            if (ledferModel != null)
            {


                return Ok();
            }
            else
                return NotFound();
        }

        //[HttpGet("testapi")]
        //public IActionResult testapi()
        //{

        //    var messag = new { message = "Ok" };
        //    return Ok(messag);
        //}

        [HttpPost("collection-report")]
        public async Task<IActionResult> ledgerReport([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            //string sql = "SELECT top 10 as_id,'active' as status from acc_subhead where as_ap_id = 14";
            int user_id = usqlre.user_role == "ADMIN" ? 0 : Convert.ToInt32(usqlre.userId);
            String query = "EXEC [dbo].[Sp_acc_report]\n" +
                    "\t\t@wi_from_date = '" + model.fromDate + "',\n" +
                    "\t\t@wi_to_date = '" + model.toDate + "',\n" +
                    "\t\t@user_id = " + user_id + ",\n" +
                    "\t\t@StatementType = N'" + model.StatementType + "'";

            DataSet ds = usqlre.dbreadDataset(query);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(ds.Tables["Table"]));
        }
        [HttpPost("receipt-collection-report")]
        public async Task<IActionResult> receiptCollectonReport([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sqlwhere = "";
            if(usqlre.user_role != "ADMIN")
                sqlwhere="and vr_user_id =" + usqlre.userId;
            String query = "EXEC [dbo].[Sp_acc_report]\n" +
                    "\t\t@wi_from_date = '" + model.fromDate + "',\n" +
                    "\t\t@wi_to_date = '" + model.toDate + "',\n" +
                    "\t\t@sqlwhere = '" + sqlwhere + "',\n" +
                    "\t\t@StatementType = N'" + model.StatementType + "'";

            DataSet ds = usqlre.dbreadDataset(query);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(ds.Tables["Table"]));
        }

        [HttpGet("branch")]
        public async Task<IActionResult> branchNames()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "";
            sql = "select gb_branch_name as label,gb_branch_name as value from gnl_branchs";
            DataTable dt_branchname = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt_branchname));
        }

        [HttpPost("branch-report")]
        public async Task<IActionResult> branchReport([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            String query = "USE " + model.StatementType + " EXEC [dbo].[Sp_android]  @date = '" + model.toDate + "', @StatementType = N'branch_report'";

            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(dt));
        }
        [HttpPost("sales-count-details")]
        public async Task<IActionResult> salesDetail([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = null;
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            try
            {

                String statement = "sale_count_details";
                usqlre = new UserSqlServer(this);
                SqlCommand cmd = new SqlCommand("Sp_api_dashboard", usqlre.shop);
                cmd.Parameters.AddWithValue("@fromDate", model.fromDate);
                cmd.Parameters.AddWithValue("@salesManId", Convert.ToInt32(usqlre.gu_acc_id));
                cmd.Parameters.AddWithValue("@user_Id", Convert.ToInt32(usqlre.userId));
                cmd.Parameters.AddWithValue("@user_role", usqlre.user_role);
                cmd.Parameters.AddWithValue("@str_Id", model.strId);
                cmd.Parameters.AddWithValue("@StatementType", statement);
                cmd.CommandType = CommandType.StoredProcedure;

                DataTable _temp = new DataTable();
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(_temp);
                return Ok(ReportModelContext.searializeDt(_temp));
            }
            catch(Exception e)
            {
                return Ok(new { status = false, message = e.ToString() });
            }
        }
        [HttpPost("sales-rpv-details")]
        public async Task<IActionResult> rpvDetail([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = null;
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            try
            {

                String statement = "rpv_details";
                usqlre = new UserSqlServer(this);
                SqlCommand cmd = new SqlCommand("Sp_api_dashboard", usqlre.shop);
                cmd.Parameters.AddWithValue("@fromDate", model.fromDate);
                cmd.Parameters.AddWithValue("@salesManId", Convert.ToInt32(usqlre.gu_acc_id));
                cmd.Parameters.AddWithValue("@user_Id", Convert.ToInt32(usqlre.userId));
                cmd.Parameters.AddWithValue("@user_role", usqlre.user_role);
                cmd.Parameters.AddWithValue("@type", model.type);
                cmd.Parameters.AddWithValue("@StatementType", statement);
                cmd.CommandType = CommandType.StoredProcedure;

                DataSet _temp = new DataSet();
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(_temp);
                hash.Add("CASH", _temp.Tables[0]);
                hash.Add("BANK", _temp.Tables[1]);
                return Ok(ReportModelContext.searializeDt(hash));
            }
            catch (Exception e)
            {
                return Ok(new { status = false, message = e.ToString() });
            }
        }
        [HttpGet("warranty-details/{id}")]
        public string warrantyById(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

            string sql = @"SELECT iwei_entryno,iwei_date ,iwei_acc_id,acc.as_name,acc.as_area_id,iwei_salesman_id,
                        iwei_person_id,iwei_location_id,iwei_d_cleared_total_qty,iwei_d_cleared_total_rate,
                        iwei_r_cleared_total_qty,iwei_r_cleared_total_rate,s.as_name as salesman,isr_service_name as person,gl_name as Location
                        FROM inv_waranty_entry_inf 
                        left join acc_subhead acc on acc.as_id=iwei_acc_id
                        left join acc_area on area_id=as_area_id
                        left join acc_subhead s on s.as_id=iwei_salesman_id
                        left join inv_service_reg on isr_id=iwei_person_id
                        left join gnl_location on gl_id=iwei_location_id
                        WHERE iwei_entryno=" + id+"";
          
            DataTable dt_inf = usqlre.dbReaderFill(sql);
            hash.Add("warrantyInf", dt_inf);
            string sql_par = @"SELECT iwep_entryno,iwep_d_uniquecode,iwep_d_ir_id,d.ir_code as d_ir_code,d.ir_name as d_ir_name,
                    d.ir_int_barcode as d_barcode,iwep_d_qty,iwep_d_rate,iwep_d_total,iwep_r_uniquecode,iwep_r_ir_id,
                    r.ir_code as r_ir_code,r.ir_name as r_ir_name,r.ir_int_barcode as r_barcode,iwep_r_qty,iwep_r_rate,
                    iwep_r_total,iwep_narration,iwep_status,iwep_d_new_uniquecode
                    FROM inv_waranty_entry_par 
                    inner join inv_item_reg as d on d.ir_id=iwep_d_ir_id
                    left join inv_item_reg as r on r.ir_id=iwep_r_ir_id
                    WHERE iwep_entryno="+id+"";
            DataTable dt_par = usqlre.dbReaderFill(sql_par);
            hash.Add("warrantyPar", dt_par);
            usqlre.close();
            return ReportModelContext.searializeDt(hash);
        }


    }
}
