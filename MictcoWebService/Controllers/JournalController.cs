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
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class JournalController : ControllerBase
    {
        
        [HttpGet("last-journal-invoice")]
        public string lastJournalInvoice()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT isnull(max(ji_entryno),0) as max_entry_no,isnull(min(ji_entryno),0) as min_entry_no FROM  acc_jvn_inf";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }

        [HttpPost("save-journal")]
        public async Task<IActionResult> saveJournal([FromBody] JournalModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            int entryNo = usqlre.stmtSaveJournal(model.statement, model);
            usqlre.close();
            return Ok(new { status = true, entryNo = entryNo });
        }

        [HttpPost("search-journal")]
        public async Task<IActionResult> searchPayment([FromBody] Params model)
        {
            
            UserSqlServer usqlre = new UserSqlServer(this);
            String statement = "search_journal";
            String query = "EXEC [dbo].[Sp_AndroidSalesMan]\n" +
                   "\t\t@cc_entryno = '" + Convert.ToInt32(model.Value) + "',\n" +
                   "\t\t@StatementType = N'" + statement + "'";


            DataSet ds = usqlre.dbreadDataset(query);

            string js_entryno = "0";
            string js_date = "";
            string js_userid = "0";

            DataTable jinf = ds.Tables["Table1"];
            if (jinf != null)
            {
                if (jinf.Rows.Count > 0)
                {
                    js_entryno = jinf.Rows[0]["entryno"].ToString();
                    js_date = jinf.Rows[0]["date"].ToString();
                    js_userid = jinf.Rows[0]["userid"].ToString();
                }
            }

           Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
           hash.Add("inf", ds.Tables["Table1"]);
           hash.Add("particular", ds.Tables["Table"]);
           usqlre.close();

           //string res = "{{\"entrnyno\":"+js_entryno+ ",\"date\":"+js_date+ ",\"userid\":"+ js_userid + "},\"particular\":" + ReportModelContext.searializeDt(ds.Tables["Table"])+"}";

           string res = "{{\"entrnyno\":" + js_entryno + ",\"date\":" + js_date + ",\"userid\":" + js_userid + "},\"particular\":" + ReportModelContext.searializeDt(ds.Tables["Table"]) + "}";

           return Ok(ReportModelContext.searializeDt(hash));
        }






    }
}
