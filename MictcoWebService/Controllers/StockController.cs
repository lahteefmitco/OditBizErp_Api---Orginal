using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class StockController : Controller
    {
        //[HttpPost("stock-transfer")]
        //public async Task<IActionResult> stockTransfer([FromBody] StockModel model)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);
        //    int entryNo = usqlre.stmtSaveStockTransfer(model.statement, model);
        //    usqlre.close();

        //    return Ok(new { status = entryNo < 0 ? false : true, entryNo = model.entryNo });
        //}
        [HttpPost("stock-transfer")]
        public async Task<IActionResult> stockTransfer([FromBody] StockModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            if (usqlre.FinancialDateCheck(DateTime.Parse(model.date)) == false)
            {
                return Ok(new { status = false, status_code = 400, message = "Date Not Within Financial Year", data = (object)null });
            }
            int result = usqlre.stmtSaveStockTransfer(model.statement, model);

            if (result < 0)
            {
                usqlre.close();
                return BadRequest(new
                {
                    status = false,
                    status_code = 400,
                    message = "Stock Transfer Failed",
                    data = new object[] { }
                });
            }

            // Fetch the generated stock transfer details
            string queryHeader = $@"
        SELECT isti_entryno AS entryNo, isti_date AS date, 
        isti_transfer_from AS fromId, a.gl_name as fromName,isnull(a.gl_add1,'') as fromAdd1,isnull(a.gl_add2,'') as fromAdd2, isti_transfer_to AS toId, b.gl_name as toName,isnull(b.gl_add1,'') as toAdd1,isnull(b.gl_add2,'') as toAdd2 ,isti_remarks AS remark 
        FROM inv_stock_transfer_inf 
        inner join gnl_location a on a.gl_id= isti_transfer_from
        inner join gnl_location b on b.gl_id= isti_transfer_to
        WHERE isti_entryno = (SELECT MAX(isti_entryno) FROM inv_stock_transfer_inf)"; 

            string queryItems = $@"
        SELECT istp_ir_id AS psp_ir_id,ir_code,ir_name , istp_qty AS psp_qty, istp_rate AS psp_rate, 
        istp_uniquecode AS psp_uniquecode, istp_amount AS realprate
        FROM inv_stock_transfer_par 
        inner join inv_item_reg on ir_id = istp_ir_id
        WHERE istp_entryno = (SELECT MAX(isti_entryno) FROM inv_stock_transfer_inf)";  

            DataTable dtHeader = usqlre.dbReaderFill(queryHeader);
            DataTable dtItems = usqlre.dbReaderFill(queryItems);

            usqlre.close();

            if (dtHeader.Rows.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    status_code = 200,
                    message = "Stock Transfer Not Found",
                    data = new object[] { }
                });
            }

            var transferData = new
            {
                entryNo = dtHeader.Rows[0]["entryNo"],
                date = dtHeader.Rows[0]["date"],
                fromId = dtHeader.Rows[0]["fromId"],
                fromName = dtHeader.Rows[0]["fromName"],
                fromAdd1 = dtHeader.Rows[0]["fromAdd1"],
                fromAdd2 = dtHeader.Rows[0]["fromAdd2"],
                toId = dtHeader.Rows[0]["toId"],
                toName = dtHeader.Rows[0]["toName"],
                toAdd1 = dtHeader.Rows[0]["toAdd1"],
                toAdd2 = dtHeader.Rows[0]["toAdd2"],
                remark = dtHeader.Rows[0]["remark"],
                items = dtItems.AsEnumerable()
                .Select(row => new
                {
                    psp_ir_id = row.Field<object>("psp_ir_id"),
                    itemCode = row.Field<object>("ir_code"),
                    itemName = row.Field<object>("ir_name"),
                    psp_qty = row.Field<object>("psp_qty"),
                    psp_rate = row.Field<object>("psp_rate"),
                    psp_uniquecode = row.Field<object>("psp_uniquecode"),
                    realprate = row.Field<object>("realprate")
                })
                .ToList()
            };

            return Ok(new
            {
                status = true,
                status_code = 200,
                message = "Stock Transfer Successful",
                data = transferData
            });
        }
        [HttpPost("last-stock-transfer")]
        public async Task<IActionResult> lastStockTransfer()
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                string queryHeader = $@"
            SELECT top 50 isti_entryno AS entryNo, isti_date AS date, 
            isti_transfer_from AS fromId, a.gl_name as fromName, ISNULL(a.gl_add1, '') as fromAdd1, ISNULL(a.gl_add2, '') as fromAdd2, 
            isti_transfer_to AS toId, b.gl_name as toName, ISNULL(b.gl_add1, '') as toAdd1, ISNULL(b.gl_add2, '') as toAdd2, 
            isti_remarks AS remark 
            FROM inv_stock_transfer_inf 
            INNER JOIN gnl_location a ON a.gl_id = isti_transfer_from
            INNER JOIN gnl_location b ON b.gl_id = isti_transfer_to
            ORDER BY isti_entryno DESC";

                string queryItems = $@"
            SELECT istp_entryno AS entryNo, istp_ir_id AS psp_ir_id, ir_code AS itemCode, ir_name AS itemName, 
            istp_qty AS psp_qty, istp_rate AS psp_rate, istp_uniquecode AS psp_uniquecode, istp_amount AS realprate
            FROM inv_stock_transfer_par 
            INNER JOIN inv_item_reg ON ir_id = istp_ir_id
            ORDER BY istp_entryno DESC";

                DataTable dtHeader = usqlre.dbReaderFill(queryHeader);
                DataTable dtItems = usqlre.dbReaderFill(queryItems);
                usqlre.close();

                if (dtHeader.Rows.Count == 0)
                {
                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Stock Transfer Not Found",
                        data = new object[] { }
                    });
                }

                // Create a list of stock transfers
                var transferData = dtHeader.AsEnumerable().Select(headerRow => new
                {
                    entryNo = Convert.ToInt32(headerRow["entryNo"]),
                    date = Convert.ToDateTime(headerRow["date"]).ToString("yyyy-MM-dd"),
                    fromId = Convert.ToInt32(headerRow["fromId"]),
                    fromName = headerRow["fromName"].ToString(),
                    fromAdd1 = headerRow["fromAdd1"].ToString(),
                    fromAdd2 = headerRow["fromAdd2"].ToString(),
                    toId = Convert.ToInt32(headerRow["toId"]),
                    toName = headerRow["toName"].ToString(),
                    toAdd1 = headerRow["toAdd1"].ToString(),
                    toAdd2 = headerRow["toAdd2"].ToString(),
                    remark = headerRow["remark"].ToString(),
                    items = dtItems.AsEnumerable()
                        .Where(itemRow => itemRow["entryNo"].ToString() == headerRow["entryNo"].ToString()) // Filter items
                        .Select(itemRow => new
                        {
                            psp_ir_id = Convert.ToInt32(itemRow["psp_ir_id"]),
                            itemCode = itemRow["itemCode"].ToString(),
                            itemName = itemRow["itemName"].ToString(),
                            psp_qty = Convert.ToInt32(itemRow["psp_qty"]),
                            psp_rate = Convert.ToDecimal(itemRow["psp_rate"]),
                            psp_uniquecode = Convert.ToInt32(itemRow["psp_uniquecode"]),
                            realprate = Convert.ToDecimal(itemRow["realprate"])
                        })
                        .ToList()
                }).ToList();

                return Ok(new
                {
                    status = true,
                    status_code = 200,
                    message = "Stock Transfer Retrieved Successfully",
                    data = transferData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching stock transfer data: " + ex.Message,
                    data = new object[] { }
                });
            }          
        }
        [HttpPost("stock-transfer-by-entryno")]
        public async Task<IActionResult> lastStockTransferByEntryNo(int EntryNo)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                string queryHeader = $@"
            SELECT isti_entryno AS entryNo, isti_date AS date, 
            isti_transfer_from AS fromId, a.gl_name as fromName, ISNULL(a.gl_add1, '') as fromAdd1, ISNULL(a.gl_add2, '') as fromAdd2, 
            isti_transfer_to AS toId, b.gl_name as toName, ISNULL(b.gl_add1, '') as toAdd1, ISNULL(b.gl_add2, '') as toAdd2, 
            isti_remarks AS remark 
            FROM inv_stock_transfer_inf 
            INNER JOIN gnl_location a ON a.gl_id = isti_transfer_from
            INNER JOIN gnl_location b ON b.gl_id = isti_transfer_to
            where isti_entryno= "+EntryNo+@"
            ORDER BY isti_entryno DESC";

                string queryItems = $@"
            SELECT istp_entryno AS entryNo, istp_ir_id AS psp_ir_id, ir_code AS itemCode, ir_name AS itemName, 
            istp_qty AS psp_qty, istp_rate AS psp_rate, istp_uniquecode AS psp_uniquecode, istp_amount AS realprate
            FROM inv_stock_transfer_par 
            INNER JOIN inv_item_reg ON ir_id = istp_ir_id
            where istp_entryno= "+EntryNo+@"
            ORDER BY istp_entryno DESC";

                DataTable dtHeader = usqlre.dbReaderFill(queryHeader);
                DataTable dtItems = usqlre.dbReaderFill(queryItems);
                usqlre.close();

                if (dtHeader.Rows.Count == 0)
                {
                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Stock Transfer Not Found",
                        data = new object[] { }
                    });
                }

                // Create a list of stock transfers
                var transferData = dtHeader.AsEnumerable().Select(headerRow => new
                {
                    entryNo = Convert.ToInt32(headerRow["entryNo"]),
                    date = Convert.ToDateTime(headerRow["date"]).ToString("yyyy-MM-dd"),
                    fromId = Convert.ToInt32(headerRow["fromId"]),
                    fromName = headerRow["fromName"].ToString(),
                    fromAdd1 = headerRow["fromAdd1"].ToString(),
                    fromAdd2 = headerRow["fromAdd2"].ToString(),
                    toId = Convert.ToInt32(headerRow["toId"]),
                    toName = headerRow["toName"].ToString(),
                    toAdd1 = headerRow["toAdd1"].ToString(),
                    toAdd2 = headerRow["toAdd2"].ToString(),
                    remark = headerRow["remark"].ToString(),
                    items = dtItems.AsEnumerable()
                        .Where(itemRow => itemRow["entryNo"].ToString() == headerRow["entryNo"].ToString()) // Filter items
                        .Select(itemRow => new
                        {
                            psp_ir_id = Convert.ToInt32(itemRow["psp_ir_id"]),
                            itemCode = itemRow["itemCode"].ToString(),
                            itemName = itemRow["itemName"].ToString(),
                            psp_qty = Convert.ToInt32(itemRow["psp_qty"]),
                            psp_rate = Convert.ToDecimal(itemRow["psp_rate"]),
                            psp_uniquecode = Convert.ToInt32(itemRow["psp_uniquecode"]),
                            realprate = Convert.ToDecimal(itemRow["realprate"])
                        })
                        .ToList()
                }).ToList();

                return Ok(new
                {
                    status = true,
                    status_code = 200,
                    message = "Stock Transfer Retrieved Successfully",
                    data = transferData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching stock transfer data: " + ex.Message,
                    data = new object[] { }
                });
            }
        }

        [HttpGet("last-stock-invoice")]
        public string lastStockInvoice(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "SELECT max(isti_entryno) as max_entry_no,min(isti_entryno) as min_entry_no from inv_stock_transfer_inf";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }
    }
}
