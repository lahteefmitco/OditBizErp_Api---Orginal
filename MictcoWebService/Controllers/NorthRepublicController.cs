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
    public class NorthRepublicController : Controller
    {
        [HttpPost("mictco-api")]
        public async Task<IActionResult> authLogin(NorthRepublicUserModel user)
        {
            if (user != null)
            {
                UserSqlServer ussvr = null;
                try
                {
                    ussvr = new UserSqlServer(this);
                    string sql = "";

                    sql = "select gu_user_id,gu_name,ur_name,gl_name from gnl_users inner join gnl_user_roles on gnl_users.gu_ur_id=gnl_user_roles.ur_id left join gnl_location on gu_location_id=gl_id where  gu_name = '" + user.userName + "' and gu_pass = '" + user.password + "' and gu_active = 1";


                    DataTable userRoles = ussvr.dbReaderFill(sql);

                    if (userRoles.Rows.Count <= 0)
                    {
                        return Unauthorized(new { status = false });
                    }
                    else
                    {


                        Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

                        String query = "EXEC [dbo].[Sp_report_external_api] @from_date = '" + user.fromDate + "',@to_date = '" + user.toDate + "' " + ",@StatementType = '" + user.reportType + "'";
                        DataTable ds = ussvr.dbReaderFill(query);
                        ussvr.close();
                        hash.Add(user.reportType, ds);
                        return Ok(ReportModelContext.searializeDt(hash));
                    }




                    return Unauthorized(new { status = true });

                }
                catch (Exception e)
                {
                    return Unauthorized(new { status = false });
                }
                finally
                {
                    if (ussvr != null)
                        ussvr.close();
                }
            }
            else
            {
                return Unauthorized(new { status = false });
            }

        }
        [HttpPost("mictco-api-new")]
        public async Task<IActionResult> authLoginNew(NorthRepublicUserModel user)
        {
            if (user == null)
            {
                return StatusCode(400, new
                {
                    status = false,
                    statusCode = 400,
                    message = "Invalid request data",
                    data = (object)null
                });
            }

            UserSqlServer ussvr = null;
            try
            {
                ussvr = new UserSqlServer(this);
                string sql = @"
            SELECT gu_user_id, gu_name, ur_name, gl_name 
            FROM gnl_users 
            INNER JOIN gnl_user_roles ON gnl_users.gu_ur_id = gnl_user_roles.ur_id 
            LEFT JOIN gnl_location ON gu_location_id = gl_id 
            WHERE gu_name = '"+user.userName+@"' 
              AND gu_pass = '"+user.password+@"' 
              AND gu_active = 1";

                DataTable userRoles = ussvr.dbReaderFill(sql);

                if (userRoles.Rows.Count <= 0)
                {
                    return StatusCode(401, new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Unauthorized: Invalid username or password",
                        data = (object)null
                    });
                }

                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

                string query = $@"
            EXEC [dbo].[Sp_report_external_api] 
                @from_date = '{user.fromDate}', 
                @to_date = '{user.toDate}', 
                @StatementType = '{user.reportType}'";

                DataTable ds = ussvr.dbReaderFill(query);
                hash.Add(user.reportType, ds);

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Login and report retrieval successful",
                    data = ReportModelContext.searializeDt(hash)
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Internal server error: " + e.Message,
                    data = (object)null
                });
            }
            finally
            {
                if (ussvr != null)
                    ussvr.close();
            }
        }

    }
}
