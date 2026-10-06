using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class DailyTransactionController : Controller
    {
        [HttpPost("updateDailyTransaction")]
        public async Task<IActionResult> SaveDailyTransactionList([FromBody] List<DailyTransactionSaveModel> modelList)
        {
            UserSqlServer usqlre = null;

            try
            {
                usqlre = new UserSqlServer(this);
                List<int> savedIds = new List<int>();

                foreach (var model in modelList)
                {
                    string sql = "DECLARE @return_value int " +
                                 "EXEC @return_value = [dbo].[sp_inv_daily_transaction] " +
                                 "@dt_ir_id = " + model.itemId + ", " +
                                 "@dt_location_id = " + model.locationId + ", " +
                                 "@dt_user_id = " + model.userId + ", " +
                                 "@dt_opening_qty = " + model.openingQty + ", " +
                                 "@StatementType = N'Insert' " +
                                 "SELECT 'return' = @return_value ";

                    DataTable dt = usqlre.dbReaderFill(sql);
                }

                return Ok(new { status = true });
            }
            catch (Exception ex)
            {
                return Ok(new { status = false, error = ex.Message });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpPost("clearAllData/{location_id}")]
        public async Task<IActionResult> UpdateDailyTransaction(decimal location_id)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                string sql = "UPDATE inv_daily_transaction SET dt_opening_qty = 0 WHERE dt_location_id = " + location_id + "";
                if (usqlre.dbExecute(sql))
                {
                    return Ok(new { status = true });
                }
                else
                {
                    return Ok(new { status = false });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while clear data." });
            }
        }
        [HttpGet("getAllData/{location_id}")]
        public async Task<IActionResult> getAllData(decimal location_id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string user_id = usqlre.userId;
            string dateTimeNow = DateTime.Today.ToString("yyyy-MM-dd");
            string sql = "";
            sql=$@"SELECT 
    v.ir_id AS ItemId, 
    ir.ir_name AS ItemName, 
    ISNULL(sales.SaledQty, 0) AS SaledQty, 
    ISNULL(daily.OpeningQty, 0) AS OpeningQty,
    ISNULL(daily.OpeningQty, 0) - ISNULL(sales.SaledQty, 0) AS RemainingQty
FROM 
    view_stock v
LEFT JOIN 
    (
        SELECT 
            sp.sp_ir_id, 
            SUM(sp.sp_qty) AS SaledQty
        FROM 
            inv_sales_par sp
        INNER JOIN 
            inv_sales_inf si ON sp.sp_entryno = si.si_entryno 
            AND sp.sp_str_id = si.si_str_id
        WHERE 
            CAST(si.si_date AS Date) = '{dateTimeNow}' and si_location_id ={location_id}
        GROUP BY 
            sp.sp_ir_id
    ) AS sales ON v.ir_id = sales.sp_ir_id
LEFT JOIN 
    (
        SELECT 
            dt_ir_id, 
            SUM(dt_opening_qty) AS OpeningQty
        FROM 
            inv_daily_transaction
        WHERE dt_location_id ={location_id}
        GROUP BY 
            dt_ir_id
    ) AS daily ON v.ir_id = daily.dt_ir_id
INNER JOIN 
    inv_item_reg ir ON v.ir_id = ir.ir_id
WHERE 
    v.location_id = {location_id} and ir.ir_active =1
GROUP BY 
    v.ir_id, ir.ir_name, sales.SaledQty, daily.OpeningQty";
            DataTable items = usqlre.dbReaderFill(sql);
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(items));

        }        
    }
}
