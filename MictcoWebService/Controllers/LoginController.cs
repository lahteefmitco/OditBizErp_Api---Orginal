using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MictcoWebService.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using MictcoWebService.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using MictcoWebService.Common;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Cors;

namespace MictcoWebService.Controllers
{
    [ApiController]
    //[Authorize(Roles = UserRoles.ClientUser)]

    public class LoginController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration _configuration;
        public LoginController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            _configuration = configuration;
        }

        // POST: api/TodoItems
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("user-auth")]
        public async Task<ActionResult<UserModel>> authLogin(UserModel user)
        {
            if (user != null)
            {
                UserSqlServer ussvr = null;
                try
                {
                    ussvr = new UserSqlServer(this, validateUser: false);
                    string loginQuery = "";

                    string areaid = "";
                    string routeid = "";
                    //areaid = user.area > 0 ? user.area.ToString() : 0.ToString();
                    areaid = user.area != "" ? user.area.ToString() : 0.ToString();
                    routeid = user.routId != "" ? user.routId.ToString() : 0.ToString();
                    string sql = "";
                    DataTable dt_count = ussvr.dbReaderFill("select count(ans_id) as cntmacid from android_settings where ans_name='VALIDATE MACID'");
                    if (dt_count == null)
                        throw new Exception(string.IsNullOrEmpty(ussvr.lastError) ? "Database connection failed" : ussvr.lastError);
                    int _cntvalidatemacid = Convert.ToInt32(dt_count.Rows[0][0].ToString());
                    bool ENABLEVALIDATEMACID = false;
                    DataTable dt_settings = ussvr.dbReaderFill("SELECT ans_status FROM android_settings where ans_name='VALIDATE MACID'");
                    if (dt_settings.Rows.Count > 0)
                    {
                        ENABLEVALIDATEMACID = Convert.ToBoolean(Convert.ToInt32(dt_settings.Rows[0][0].ToString()));
                    }


                    if (ENABLEVALIDATEMACID && _cntvalidatemacid == 1)
                    {
                        sql = "select gu_user_id, gu_name, ur_name, gl_name from gnl_users inner join gnl_user_roles on gnl_users.gu_ur_id = gnl_user_roles.ur_id left join gnl_location on gu_location_id = gl_id where gu_name = '" + user.userName + "' and gu_pass = '" + user.password + "' and gu_macid='" + user.macId + "' and gu_active = 1";
                    }
                    else
                    {
                        sql = "select gu_user_id,gu_name,ur_name,gl_name from gnl_users inner join gnl_user_roles on gnl_users.gu_ur_id=gnl_user_roles.ur_id left join gnl_location on gu_location_id=gl_id where  gu_name = '" + user.userName + "' and gu_pass = '" + user.password + "' and gu_active = 1";
                    }

                    //sql = "select gu_user_id,gu_name,ur_name,gl_name from gnl_users inner join gnl_user_roles on gnl_users.gu_ur_id=gnl_user_roles.ur_id left join gnl_location on gu_location_id=gl_id where  gu_name = '" + user.userName + "' and gu_pass = '" + user.password + "' and gu_active = 1";
                    DataTable userRoles = ussvr.dbReaderFill(sql);

                    if (userRoles.Rows.Count <= 0)
                    {
                        return Unauthorized(new { status = false });
                    }


                    if (ENABLEVALIDATEMACID && _cntvalidatemacid == 1)
                    {
                        loginQuery = "select gu_user_id,gu_active,isnull(gu_acc_id,0) AS gu_acc_id,isnull(gu_user_cash_id,0) as gu_user_cash_id from dbo.gnl_users where gu_name = '" + user.userName + "' and gu_pass='" + user.password + "' and gu_macid='" + user.macId + "' and gu_active = 1";
                    }
                    else
                    {
                        loginQuery = "select gu_user_id,gu_active,isnull(gu_acc_id,0) AS gu_acc_id,isnull(gu_user_cash_id,0) as gu_user_cash_id from dbo.gnl_users where gu_name = '" + user.userName + "' and gu_pass='" + user.password + "' and gu_active = 1";

                    }

                    String user_role = userRoles.Rows[0]["ur_name"].ToString();

                    ussvr.user_role = user_role;

                    if (user_role != "ADMIN")
                        if (ENABLEVALIDATEMACID && _cntvalidatemacid == 1)
                        {
                            loginQuery = "select gu_user_id,gu_active,isnull(gu_acc_id,0) AS gu_acc_id,isnull(gu_user_cash_id,0) as gu_user_cash_id from dbo.gnl_users where gu_name = '" + user.userName + "' and gu_pass='" + user.password + "' and gu_location_id='" + user.location + "' and gu_macid='" + user.macId + "' and gu_active = 1";
                        }
                        else
                        {
                            loginQuery = "select gu_user_id,gu_active,isnull(gu_acc_id,0) AS gu_acc_id,isnull(gu_user_cash_id,0) as gu_user_cash_id from dbo.gnl_users where gu_name = '" + user.userName + "' and gu_pass='" + user.password + "' and gu_location_id='" + user.location + "' and gu_active = 1";
                        }



                    DataTable dt = ussvr.dbReaderFill(loginQuery);
                    if (dt.Rows.Count == 1)
                    {

                        string userId = dt.Rows[0]["gu_user_id"].ToString();
                        string gu_acc_id = dt.Rows[0]["gu_acc_id"].ToString();
                        string gu_user_cash_id = dt.Rows[0]["gu_user_cash_id"].ToString();

                        if (dt.Rows[0]["gu_active"].ToString() == "0")
                            return Unauthorized();

                        //var handler = new JwtSecurityTokenHandler();
                        //string authHeader = Request.Headers["Authorization"];
                        //authHeader = authHeader.Replace("Bearer ", "");
                        //var jsonToken = handler.ReadToken(authHeader);
                        //var tokenS = handler.ReadToken(authHeader) as JwtSecurityToken;
                        //var jti = tokenS.Claims.First(claim => claim.Type == "jti").Value;
                        //var id = tokenS.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;

                        //var d_host = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Actor).Value;
                        //var d_database = tokenS.Claims.First(claim => claim.Type == ClaimTypes.GivenName).Value;
                        //var d_username = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Gender).Value;
                        //var d_password = tokenS.Claims.First(claim => claim.Type == ClaimTypes.DateOfBirth).Value;

                        //SETTING NEW TOKE
                        //var authClaims = new List<Claim>
                        //{
                        //    new Claim(ClaimTypes.Name, userId),
                        //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        //    new Claim(ClaimTypes.NameIdentifier, userId),
                        //    //new Claim(ClaimTypes.Locality,areaid)

                        //};

                        //authClaims.Add(new Claim(ClaimTypes.Role, UserRoles.User));

                        //authClaims.Add(new Claim(ClaimTypes.Actor,d_host));
                        //authClaims.Add(new Claim(ClaimTypes.GivenName, d_database));
                        //authClaims.Add(new Claim(ClaimTypes.Gender, d_username));
                        //authClaims.Add(new Claim(ClaimTypes.DateOfBirth, d_password));
                        //authClaims.Add(new Claim(ClaimTypes.PostalCode, user_role));
                        //authClaims.Add(new Claim(ClaimTypes.Anonymous, user.location));
                        //authClaims.Add(new Claim(ClaimTypes.Sid, gu_acc_id));
                        //authClaims.Add(new Claim(ClaimTypes.Surname, gu_user_cash_id));
                        //authClaims.Add(new Claim(ClaimTypes.Locality, areaid));
                        //authClaims.Add(new Claim(ClaimTypes.StreetAddress, routeid));
                        //// Encrypted password fingerprint — validated on API calls only if userid is in gnl_pass_validate
                        //authClaims.Add(new Claim(ClaimTypes.Hash, CommonHelper.tokenEncrypt(user.password ?? "")));

                        var handler = new JwtSecurityTokenHandler();
                        string authHeader = Request.Headers["Authorization"];

                        string d_host = "";
                        string d_database = "";
                        string d_username = "";
                        string d_password = "";

                        if (!string.IsNullOrEmpty(authHeader))
                        {
                            authHeader = authHeader.Replace("Bearer ", "");

                            var tokenS = handler.ReadToken(authHeader) as JwtSecurityToken;

                            d_host = tokenS.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor)?.Value ?? "";
                            d_database = tokenS.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "";
                            d_username = tokenS.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Gender)?.Value ?? "";
                            d_password = tokenS.Claims.FirstOrDefault(c => c.Type == ClaimTypes.DateOfBirth)?.Value ?? "";
                        }

                        var authClaims = new List<Claim>
{
                        new Claim(ClaimTypes.Name, userId),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Role, UserRoles.User),

                        new Claim(ClaimTypes.PostalCode, user_role),
                        new Claim(ClaimTypes.Anonymous, user.location),
                        new Claim(ClaimTypes.Sid, gu_acc_id),
                        new Claim(ClaimTypes.Surname, gu_user_cash_id),
                        new Claim(ClaimTypes.Locality, areaid),
                        new Claim(ClaimTypes.StreetAddress, routeid),
                        new Claim(ClaimTypes.Hash, CommonHelper.tokenEncrypt(user.password ?? ""))
};

                        if (!string.IsNullOrEmpty(d_host))
                        {
                            authClaims.Add(new Claim(ClaimTypes.Actor, d_host));
                            authClaims.Add(new Claim(ClaimTypes.GivenName, d_database));
                            authClaims.Add(new Claim(ClaimTypes.Gender, d_username));
                            authClaims.Add(new Claim(ClaimTypes.DateOfBirth, d_password));
                        }

                        //authClaims.Add(new Claim(ClaimTypes.Role, UserRoles.User));
                        //authClaims.Add(new Claim(ClaimTypes.PostalCode, user_role));
                        //authClaims.Add(new Claim(ClaimTypes.Anonymous, user.location));
                        //authClaims.Add(new Claim(ClaimTypes.Sid, gu_acc_id));
                        //authClaims.Add(new Claim(ClaimTypes.Surname, gu_user_cash_id));
                        //authClaims.Add(new Claim(ClaimTypes.Locality, areaid));
                        //authClaims.Add(new Claim(ClaimTypes.StreetAddress, routeid));
                        //authClaims.Add(new Claim(ClaimTypes.Hash, CommonHelper.tokenEncrypt(user.password ?? "")));


                        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
                        var token = new JwtSecurityToken(
                            issuer: _configuration["JWT:ValidIssuer"],
                            audience: _configuration["JWT:ValidAudience"],
                            expires: DateTime.Now.AddHours(30000),
                            claims: authClaims,
                            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                            );

                        String location_name = "";
                        DataTable location_dt = ussvr.dbReaderFill("select gl_name from gnl_location where gl_id = '" + user.location + "'");
                        if (location_dt != null)
                        {
                            if (location_dt.Rows.Count > 0)
                                location_name = location_dt.Rows[0]["gl_name"].ToString();
                        }

                        String tax_calc = "";
                        DataTable tax_calc_dt = ussvr.dbReaderFill("select gs_value from gnl_settings where gs_name = 'GSTCALC'");
                        if (tax_calc_dt != null)
                        {
                            if (tax_calc_dt.Rows.Count > 0)
                                tax_calc = tax_calc_dt.Rows[0]["gs_value"].ToString();
                        }
                        //
                        var user_inf = new
                        {
                            gu_user_id = userId.ToString(),
                            gu_name = user.userName,
                            gu_location_id = user.location.ToString(),
                            gl_name = location_name,
                            role = user_role,
                            tax_calc = tax_calc,
                            gu_acc_id = gu_acc_id
                        };

                        sql = "select * from android_settings";
                        DataTable android_impoa = ussvr.dbReaderFill(sql);

                        //salesman
                        DataTable salessman_dt = null;

                        DataTable dt_ = ussvr.dbReaderFill("select gs_status from gnl_settings where gs_value = 'ENABLESOFTWAREASWORKSHOP'");
                        int workOrderStatus = Convert.ToInt32(dt_.Rows[0][0].ToString());
                        DataTable woStatus = new DataTable();
                        string str_erp_type = "";
                        if (workOrderStatus == 1)
                        {
                            str_erp_type = "WORK ORDER";
                        }
                        else
                        {
                            str_erp_type = "ERP";
                        }

                        if (Convert.ToInt32(gu_acc_id) > 0)
                        {
                            salessman_dt = ussvr.dbReaderFill("SELECT as_name as label,cast(as_id as Int) as value,as_rate_type from acc_subhead where as_id='" + gu_acc_id + "'");
                        }

                        //cashaccount//
                        DataTable cash_account_dt = ussvr.dbReaderFill("SELECT as_name as label,cast(as_id as Int) as value from acc_subhead where as_id='" + gu_user_cash_id + "'");

                        return Ok(new
                        {
                            token = new JwtSecurityTokenHandler().WriteToken(token),
                            expiration = token.ValidTo,
                            status = true,
                            user = user_inf,
                            user_role = user_role,
                            android = ReportModelContext.searializeDt(android_impoa),
                            sales_man = ReportModelContext.searializeDt(salessman_dt),
                            cash_account = ReportModelContext.searializeDt(cash_account_dt),
                            erp_type=str_erp_type
                        });
                    }
                    else
                    {
                        return Unauthorized(new { status = false });
                    }
                }
                catch (Exception e)
                {
                    return Unauthorized(new { status = false, error = e.Message });
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


        [HttpGet("user-locations")]
        public IActionResult getUserLocations()
        {
            UserSqlServer usqlre = new UserSqlServer(this, validateUser: false);
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            DataTable dt_area = new DataTable();
            DataTable dt_route = new DataTable();
            string sql = "select cast(gl_id as nvarchar(50)) as gl_id,gl_name from gnl_location";
            DataTable locations = usqlre.dbReaderFill(sql);
            usqlre.close();
            hash.Add("loactions", locations);
            bool AREAWISELOGIN = false;
            bool ROUTWISELOGIN = false;
            DataTable dt_settings = usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='AREAWISE LOGIN'");
            if (dt_settings.Rows.Count > 0)
                AREAWISELOGIN = Convert.ToBoolean(Convert.ToInt32(dt_settings.Rows[0][0].ToString()));
            if (AREAWISELOGIN)
            {
                string sqlarea = "select cast(area_id as nvarchar(50)) as value,area_name as label from acc_area";
                dt_area = usqlre.dbReaderFill(sqlarea);
                usqlre.close();
            }
            DataTable dt_rout= usqlre.dbReaderFill("SELECT ans_status FROM android_settings WHERE ans_name='ROUTEWISE LOGIN'");
            if (dt_rout.Rows.Count > 0)
                ROUTWISELOGIN = Convert.ToBoolean(Convert.ToInt32(dt_rout.Rows[0][0].ToString()));
            if (ROUTWISELOGIN)
            {
                string sqlroute= "select cast(r_id as nvarchar(50)) as value,r_name as label from inv_rout_reg";
                dt_route = usqlre.dbReaderFill(sqlroute);
                usqlre.close();
            }

            hash.Add("area", dt_area);
            hash.Add("route", dt_route);
            return Ok(ReportModelContext.searializeDt(hash));
        }
        [HttpGet("check-route-wise-login")]
        public IActionResult CheckRouteWiseLogin()
        {
            UserSqlServer usqlre = new UserSqlServer(this, validateUser: false);

            try
            {
                bool isEnabled = false;

                string sql = @"select ans_status from android_settings where ans_name = 'ENABLE SINGLE ROUTEWISE LOGIN'";

                DataTable dt = usqlre.dbReaderFill(sql);

                if (dt.Rows.Count > 0)
                {
                    isEnabled = Convert.ToBoolean(
                        Convert.ToInt32(dt.Rows[0]["ans_status"].ToString())
                    );
                }

                return Ok(new
                {
                    success = true,
                    routeWiseLoginEnabled = isEnabled
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            finally
            {
                usqlre.close();
            }
        }

    }
}
