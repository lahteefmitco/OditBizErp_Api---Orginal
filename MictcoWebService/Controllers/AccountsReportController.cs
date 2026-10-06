using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class AccountsReportController : ControllerBase
    {
        [HttpPost("group-report-odbz")]
        public async Task<IActionResult> groupReportOditBiz([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            String query = "";
            if (model.StatementType == "salesmanwisegroup")
            {
                string sales_man_selected = "";
                if (model.salesManId != 0)
                {
                    sales_man_selected = " and (b.as_id=" + model.salesManId + ") ";
                    query = "EXEC [dbo].[Sp_acc_report] " +
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


                query = "EXEC [dbo].[Sp_acc_report] " +
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
                    query = "EXEC [dbo].[Sp_acc_report] " +
                                "@wi_from_date = '" + model.fromDate + "'," +
                                "@wi_to_date = '" + model.toDate + "'," +
                                "@led_id = '" + model.asId + "'," +
                                "@StatementType = N'groupdetailed'";

                }
                else
                {
                    string area_selected = "and (r_id=" + model.routeId + ")";
                    query = "EXEC [dbo].[Sp_acc_report] " +
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
    }
}