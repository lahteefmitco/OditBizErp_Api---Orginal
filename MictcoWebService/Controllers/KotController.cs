using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class KotController : Controller
    {
        [HttpGet("kot-tables")]
        public string Fn_kot_table()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = @"EXEC [dbo].[Sp_kot_api] @StatementType = N'table_details'";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }

        [HttpGet("kot-tables-details/{id}")]
        public string Fn_kot_table_details(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = @"EXEC [dbo].[Sp_kot_api] @t_id = " + id + ",@StatementType = 'table_details_single'";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }

        [HttpGet("kot-import-all")]
        public IActionResult importAll()
        {

            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            UserSqlServer usqlre = new UserSqlServer(this);
            string userid = usqlre.userId;
            string sql = "";
            sql = @"EXEC [dbo].[Sp_kot_api] @StatementType = 'kot_import_all'";
            DataSet ds = usqlre.dbreadDataset(sql);
            usqlre.close();

            hash.Add("category", ds.Tables[0]);
            hash.Add("product", ds.Tables[1]);
            hash.Add("extras", ds.Tables[2]);
            hash.Add("varints", ds.Tables[3]);
            hash.Add("group", ds.Tables[4]);
            hash.Add("salesman", ds.Tables[5]);
            hash.Add("cash", ds.Tables[6]);
            hash.Add("users", ds.Tables[7]);
            hash.Add("bank", ds.Tables[8]);
            hash.Add("android", ds.Tables[9]);
            hash.Add("delivery", ds.Tables[10]);
            hash.Add("company", ds.Tables[11]);
            hash.Add("salesPriceType", ds.Tables[12]);
            hash.Add("printers", ds.Tables[13]);
            hash.Add("tables", ds.Tables[14]);

            return Ok(ReportModelContext.searializeDt(hash));
        }

        [HttpPost("kot-search")]
        public async Task<IActionResult> Fn_kotsearch([FromBody] EntrySearchModel model)
        {
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = "";
            sql = @"EXEC [dbo].[Sp_kot_api] @si_str_id=" + model.siStrId + ",@si_entryno=" + model.entryNo + ",@StatementType = 'kot_search'";
            DataSet ds = usqlre.dbreadDataset(sql);
            usqlre.close();

            hash.Add("sales_inf", ds.Tables[0]);
            hash.Add("sales_par", ds.Tables[1]);

            return Ok(ReportModelContext.searializeDt(hash));
        }

        [HttpGet("kot-pending-order/{id}")]
        public string Fn_kot_pending_orders(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = @"EXEC [dbo].[Sp_kot_api] @si_str_id = " + id + ",@StatementType = 'kot_pendingorders'";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }

        [HttpPost("kot-sales-search")]
        public async Task<IActionResult> Fn_sales_search([FromBody] KotReportModel model)
        {
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = "";
            sql = @"EXEC [dbo].[Sp_kot_api] @si_str_id=" + model.si_str_id + ",@si_from_date='" + model.si_from_date + "',@si_to_date='" + model.si_to_date + "',@StatementType = 'kot_salesreport'";
            DataSet ds = usqlre.dbreadDataset(sql);
            usqlre.close();

            hash.Add("sales_inf", ds.Tables[0]);

            return Ok(ReportModelContext.searializeDt(hash));
        }

        [HttpPost("kot-table-change")]
        public int Fn_kot_change_table([FromBody] KotChangeTableModel model)
        {
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = "";
            sql = @"EXEC [dbo].[Sp_kot_change_table] @si_str_id=" + model.si_str_id + ",@si_entryno=" + model.si_entryno + ",@table_id=" + model.table_id + ",@chair_id='" + model.chair_id + "',@StatementType = 'change'";
            usqlre.dbExecute(sql);
            usqlre.close();
            return 1;
        }
    }
}
