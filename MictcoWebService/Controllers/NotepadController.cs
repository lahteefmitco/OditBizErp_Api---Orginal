using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Policy;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class NotepadController : ControllerBase
    {
        [HttpPost("save-notpad")]
        public async Task<IActionResult> NotPadSave([FromBody] NotepadModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            int _status = usqlre.NotpadSave(model.statement, model);
            return Ok(new { status = _status > 0 ? true : false});
        }

        [HttpGet("search-details/{id}")]
        public string sesrschDetails(int id)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string stmnt = id > 0 ? "Search" : "SelectAll";
            String query = @"EXEC [dbo].[Sp_gnl_notepad]
                           @gn_id = " + id + "," +
                           "@StatementType = '" + stmnt + "'";

            DataTable dt = usqlre.dbReaderFill(query);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }
    }
}
