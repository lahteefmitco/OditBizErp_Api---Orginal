using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class ReportSalesController : ControllerBase
    {
        [HttpPost("sales-gstr2-report")]
        public async Task<IActionResult> sales_report_gstr2([FromBody] SalesReporGsrtR2Model model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            string sqlwhere = @"where cast(si_date as date)<=''" + model.toDate + "'' and cast(si_date as date)>=''" + model.fromDate + "'' and (si_str_id=" + model.strid + ")";
            String query = "EXEC [dbo].[Sp_Report_Sales]\n" +
                    "\t\t@from_date = '" + model.fromDate + "',\n" +
                    "\t\t@to_date = '" + model.toDate + "',\n" +
                    "\t\t@sqlwhere = '" + sqlwhere + "',\n" +
                    "\t\t@StatementType = N'" + model.StatementType + "'";

            DataTable ds = usqlre.dbReaderFill(query);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(ds));
        }
    }
}