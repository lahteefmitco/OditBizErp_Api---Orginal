using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class ImageMobilesController : Controller
    {
        [HttpPost("customer-list-image-mobiles-api")]
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

                        string query = "EXEC [dbo].[Sp_report_external_api] @StatementType='customer-list-image-mobiles'"; 
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
    }
}
