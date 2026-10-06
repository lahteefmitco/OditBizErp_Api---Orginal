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
    public class InventoryController : ControllerBase
    {
        //[HttpPost("barcode-stock-info")]
        //public async Task<IActionResult> barcodeSearch([FromBody] InventoryModel model)
        //{
        //    UserSqlServer usqlre = new UserSqlServer(this);

        //    String query = "";
        //    if(model.ir_id != 0)
        //    {
        //        query = "EXEC [dbo].[Sp_Stock] @ir_id = " + model.ir_id + ",@location_id =0 " + ",@StatementType = 'RS_select'";
        //    }
        //    else if(model.narration.ToString() != "0")
        //    {
        //        query = "EXEC [dbo].[Sp_Stock] @int_barcode = " + model.narration + ",@location_id = " + Convert.ToInt32(usqlre.locationId) + "" + ",@StatementType = 'RS_narration'";
        //    }
        //    else if(model.size.ToString() != "0")
        //    {
        //        query = "EXEC [dbo].[Sp_Stock] @int_barcode = " + model.size + ",@location_id = " + Convert.ToInt32(usqlre.locationId) + "" + ",@StatementType = 'RS_size'";
        //    }
        //    else if(model.uniquecode.ToString() != "0")
        //    {
        //        query = "EXEC [dbo].[Sp_Stock] @uniquecode = " + model.uniquecode + ",@location_id = " + Convert.ToInt32(usqlre.locationId) + "" + ",@StatementType = 'RS_uniquecode'";
        //    }
        //    else
        //    {
        //        query = "EXEC [dbo].[Sp_Stock] @int_barcode = '" + model.intBarcode + "',@location_id = " + Convert.ToInt32(usqlre.locationId) + "" + ",@StatementType = 'RS_int_barcode'";
        //    }
        //    //else
        //    //{
        //    //    bool ENABLEFRANCHISEE = false;
        //    //    DataTable dt_settings = usqlre.dbReaderFill("select gs_status from gnl_settings where gs_value = 'ENABLEFRANCHISEE'");
        //    //    ENABLEFRANCHISEE = Convert.ToBoolean(Convert.ToInt32(dt_settings.Rows[0][0].ToString()));
        //    //    if (ENABLEFRANCHISEE)
        //    //    {
        //    //        int n;
        //    //        bool isNumeric = int.TryParse(model.uniquecode, out n);
        //    //        if (isNumeric)
        //    //        {
        //    //            query = "EXEC [dbo].[Sp_Stock] @int_barcode = " + model.narration + ",@location_id = " + Convert.ToInt32(usqlre.locationId) + "" + ",@StatementType = 'RS_narration'";
        //    //        }
        //    //        else
        //    //        {
        //    //            query = "EXEC [dbo].[Sp_Stock] @int_barcode = " + model.size + ",@location_id = " + Convert.ToInt32(usqlre.locationId) + "" + ",@StatementType = 'RS_size'";
        //    //        }

        //    //    }
        //    //    else
        //    //    {
        //    //        int n;
        //    //        bool isNumeric = int.TryParse(model.uniquecode, out n);
        //    //        if (isNumeric)
        //    //            query = "EXEC [dbo].[Sp_Stock] @uniquecode = " + model.uniquecode + ",@location_id = " + Convert.ToInt32(usqlre.locationId) + "" + ",@StatementType = 'RS_uniquecode_both'";
        //    //        else
        //    //            query = "EXEC [dbo].[Sp_Stock] @int_barcode = '" + model.uniquecode + "',@location_id = " + Convert.ToInt32(usqlre.locationId) + "" + ",@StatementType = 'RS_int_barcode'";
        //    //    }
        //    //}


        //    DataSet ds = usqlre.dbreadDataset(query);
        //    usqlre.close();
        //    return Ok(ReportModelContext.searializeDt(ds.Tables[0]));
        //}


        [HttpPost("barcode-stock-info")]
        public async Task<IActionResult> barcodeSearch([FromBody] InventoryModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            String query = "";

            // If only ir_id is provided -> existing RS_select workflow
            if (model.ir_id != 0 && model.uniquecode.ToString() == "0")
            {
                query = "EXEC [dbo].[Sp_Stock] @ir_id = "
                    + model.ir_id
                    + ",@location_id = "
                    + Convert.ToInt32(usqlre.locationId)
                    + ",@StatementType = 'RS_select'";
            }
            else if (model.narration.ToString() != "0")
            {
                query = "EXEC [dbo].[Sp_Stock] @int_barcode = "
                    + model.narration
                    + ",@location_id = "
                    + Convert.ToInt32(usqlre.locationId)
                    + ",@StatementType = 'RS_narration'";
            }
            else if (model.size.ToString() != "0")
            {
                query = "EXEC [dbo].[Sp_Stock] @int_barcode = "
                    + model.size
                    + ",@location_id = "
                    + Convert.ToInt32(usqlre.locationId)
                    + ",@StatementType = 'RS_size'";
            }
            else if (model.uniquecode.ToString() != "0")
            {
                query = "EXEC [dbo].[Sp_Stock] @uniquecode = "
                    + model.uniquecode
                    + ",@location_id = "
                    + Convert.ToInt32(usqlre.locationId)
                    + ",@StatementType = 'RS_uniquecode'";
            }
            else
            {
                query = "EXEC [dbo].[Sp_Stock] @int_barcode = '"
                    + model.intBarcode
                    + "',@location_id = "
                    + Convert.ToInt32(usqlre.locationId)
                    + ",@StatementType = 'RS_int_barcode'";
            }

            DataSet ds = usqlre.dbreadDataset(query);
            usqlre.close();

            return Ok(ReportModelContext.searializeDt(ds.Tables[0]));
        }

        [HttpPost("StockInfByBarcode")]
        public async Task<IActionResult> StockInfByBarcode([FromBody] InventoryModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            String query = "";
            DataSet ds;
            bool ENABLEFRANCHISEE = false;
            DataTable dt_settings = usqlre.dbReaderFill("select gs_status from gnl_settings where gs_value = 'ENABLEFRANCHISEE'");
            ENABLEFRANCHISEE = Convert.ToBoolean(Convert.ToInt32(dt_settings.Rows[0][0].ToString()));

            long n;
            bool isNumeric = long.TryParse(model.uniquecode, out n);

            if (model.ir_id != 0)
            {
                query = "EXEC [dbo].[Sp_Stock] @ir_id = " + model.ir_id + ",@location_id =0 " + ",@StatementType = 'RS_select'";
                ds = usqlre.dbreadDataset(query);
            }
            else if (model.itemcode !=null && model.itemcode != "0" && model.itemcode != string.Empty)
            {
                query = "EXEC [dbo].[Sp_Stock] @int_barcode = " + model.itemcode + ",@location_id = " + usqlre.locationId +",@StatementType = 'RS_itemcode'";
                ds = usqlre.dbreadDataset(query);
            }
            else if (ENABLEFRANCHISEE)
            {
                query = isNumeric
                    ? $"EXEC [dbo].[Sp_Stock] @int_barcode = {model.uniquecode},@location_id = {usqlre.locationId},@StatementType = 'RS_narration'"
                    : $"EXEC [dbo].[Sp_Stock] @int_barcode = {model.uniquecode},@location_id = {usqlre.locationId},@StatementType = 'RS_size'";

                ds = usqlre.dbreadDataset(query);
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    query = $"EXEC [dbo].[Sp_Stock] @int_barcode = '{model.uniquecode}',@location_id = {usqlre.locationId},@StatementType = 'RS_int_barcode'";
                    ds = usqlre.dbreadDataset(query);
                }
            }
            else
            {
                query = $"EXEC [dbo].[Sp_Stock] @int_barcode = '{model.uniquecode}',@location_id = {usqlre.locationId},@StatementType = 'RS_uniquecode'";
                ds = usqlre.dbreadDataset(query);
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    query = $"EXEC [dbo].[Sp_Stock] @int_barcode = '{model.uniquecode}',@location_id = {usqlre.locationId},@StatementType = 'RS_int_barcode'";
                    ds = usqlre.dbreadDataset(query);
                }
            }
            usqlre.close();
            return Ok(ReportModelContext.searializeDt(ds.Tables[0]));
        }

    }
}