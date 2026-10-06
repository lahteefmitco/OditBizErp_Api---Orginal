using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MictcoWebService.Models;
using System.Data;
using MictcoWebService.Common;
using MictcoWebService.Authentication;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text;
using System.Data.SqlClient;

namespace MictcoWebService.Controllers
{
  
    [ApiController]
    public class DbUserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration _configuration;
        private readonly ClientLoginContext _context;

        public DbUserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            _configuration = configuration;

            _context = new ClientLoginContext();
        }

        [HttpPost("login-db-app")]
        public async Task<ActionResult<UserModel>> loginDbApp(UserModel user)
        {
            ClientSqlServer csqlr = null;
            var token1 = "";
            try
            {
                csqlr = new ClientSqlServer();
                String sql = "";
                sql = "select * from main_user  where  username = '" + user.userName + "' and password = '" + user.password + "' and status = 1";
                DataTable userList = csqlr.dbReaderFill(sql);
                if (userList.Rows.Count > 0)
                {
                    foreach (DataRow dr in userList.Rows)
                    {
                        string id = dr["id"].ToString();
                        string username = dr["username"].ToString();
                        string role = dr["role"].ToString();
                        string status = dr["status"].ToString();
                        if (id.Length > 0 && username.Length > 0 && role.Length > 0 && status.Length > 0)
                        {
                            var authClaims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, username),
                                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                                new Claim(ClaimTypes.NameIdentifier, username)
                            };
                            authClaims.Add(new Claim(ClaimTypes.Role, CommonHelper.tokenEncrypt(role)));
                            authClaims.Add(new Claim(ClaimTypes.Name, CommonHelper.tokenEncrypt(username)));
                            authClaims.Add(new Claim(ClaimTypes.Sid, CommonHelper.tokenEncrypt(id)));
                            authClaims.Add(new Claim(ClaimTypes.Gender, CommonHelper.tokenEncrypt(status)));
                            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
                            var token = new JwtSecurityToken(
                                issuer: _configuration["JWT:ValidIssuer"],
                                audience: _configuration["JWT:ValidAudience"],
                                expires: DateTime.Now.AddHours(300),
                                claims: authClaims,
                                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                                );
                            token1 = new JwtSecurityTokenHandler().WriteToken(token);

                        }
                    }
                    return Ok(new
                    {
                        token = token1,
                        status = true
                    });
                }
                else
                {
                    return Unauthorized(new { status = false });
                }

            }
            catch (Exception e)
            {
                return Unauthorized(new { status = false });
            }
            finally
            {
                if (csqlr != null)
                    csqlr.close();
            }

        }
        [HttpGet("duc-db")]
        public async Task<IActionResult> getdatabase()
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            DbUserServer dbuserver = new DbUserServer(this);
            
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";

            sql = "EXEC  [dbo].[Sp_databaseuser] " +
                  "@StatementType = N'Get_Db'";
            DataTable dt = csqlr.dbReaderFill(sql);
            csqlr.close();
            if (dt.Rows.Count > 0)
                return Ok(ReportModelContext.searializeDt(dt));
            else
                return Ok(new { status = false });
        }
        [HttpGet("duc-db/{id}")]
        public async Task<IActionResult> getdatabaseID(int id)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            string sql = "";

            sql = "EXEC  [dbo].[Sp_databaseuser] " +
                  "@d_id = " + id + ", " +
                  "@StatementType = N'Get_Db_BY_Id'";
            DataTable dt = csqlr.dbReaderFill(sql);
            csqlr.close();
            if (dt.Rows.Count > 0)
                return Ok(ReportModelContext.searializeDt(dt));
            else
                return Ok(new { status = false });
        }
        #region save,update,delete databases
        [HttpPost("duc-save-db")]
        public async Task<IActionResult> saveDb([FromBody] DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            int ret=savedatabase("Insert_databases", model);
            if (ret == 1)
                return Ok(new { status = true });
            else
                return Ok(new { status = false });
        }
        [HttpPost("fnSaveDb")]
        public int savedatabase(string statement, DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            csqlr.OpenConnection();
            int tres = -6;
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Sp_databaseuser";

                SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                parm.Direction = ParameterDirection.ReturnValue;

                cmd.Parameters.Add(parm);
                cmd.Parameters.AddWithValue("@d_id", model.d_id);
                cmd.Parameters.AddWithValue("@d_database", model.d_database);
                cmd.Parameters.AddWithValue("@d_host", model.d_host);
                cmd.Parameters.AddWithValue("@d_username", model.d_username);
                cmd.Parameters.AddWithValue("@d_password", model.d_password);
                cmd.Parameters.AddWithValue("@d_encread", model.d_encread);
                cmd.Parameters.AddWithValue("@d_alias", model.d_alias);
                cmd.Parameters.AddWithValue("@StatementType", statement);
                cmd.Connection = csqlr.shop;
                SqlDataReader dr = cmd.ExecuteReader();
                dr.Dispose();
                dr.Close();
                tres = Convert.ToInt32(parm.Value);
            }
            catch (Exception m)
            {
                Console.WriteLine(m.ToString());
            }
            return tres;
        }
        [HttpPost("duc-update-db")]
        public async Task<IActionResult> updateDb([FromBody] DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            int ret = savedatabase("Update_databases", model);
            if (ret == 1)
                return Ok(new { status = true });
            else
                return Ok(new { status = false });
        }
        [HttpPost("duc-delete-db")]
        public async Task<IActionResult> deleteDb([FromBody] DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            int ret = savedatabase("Delete_databases", model);
            if (ret == 1)
                return Ok(new { status = true });
            else
                return Ok(new { status = false });
        }
        #endregion

        [HttpGet("duc-db-connection")]
        public async Task<IActionResult> ducconnection()
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";

            sql = "EXEC  [dbo].[Sp_databaseuser] " +
                  "@StatementType = N'Get_Connection'";
            DataTable dt = csqlr.dbReaderFill(sql);
            csqlr.close();
            if (dt.Rows.Count > 0)
                return Ok(ReportModelContext.searializeDt(dt));
            else
                return Ok(new { status = false });
        }
        #region save,edit,delete db_to_user
        [HttpPost("fnSaveDbToUser")]
        public int savedbtouser(string statement, DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            csqlr.OpenConnection();
            int tres = -6;
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Sp_databaseuser";

                SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                parm.Direction = ParameterDirection.ReturnValue;

                cmd.Parameters.Add(parm);
                cmd.Parameters.AddWithValue("@du_id", model.du_id);
                cmd.Parameters.AddWithValue("@du_user", model.du_user);
                cmd.Parameters.AddWithValue("@du_database", model.du_database);
                cmd.Parameters.AddWithValue("@StatementType", statement);
                cmd.Connection = csqlr.shop;
                SqlDataReader dr = cmd.ExecuteReader();
                dr.Dispose();
                dr.Close();
                tres = Convert.ToInt32(parm.Value);
            }
            catch (Exception m)
            {
                Console.WriteLine(m.ToString());
            }
            return tres;
        }
        [HttpPost("duc-save-connection")]
        public async Task<IActionResult> ducsaveconnection([FromBody] DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            int ret = savedbtouser("Insert_db_to_user", model);
            if (ret == 1)
                return Ok(new { status = true });
            else
                return Ok(new { status = false });
        }
        [HttpPost("duc-update-connection")]
        public async Task<IActionResult> ducupdateconnection([FromBody] DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            int ret = savedbtouser("Update_db_to_user", model);
            if (ret == 1)
                return Ok(new { status = true });
            else
                return Ok(new { status = false });
        }
        [HttpPost("duc-delete-connection")]
        public async Task<IActionResult> ducdeleteconnection([FromBody] DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            int ret = savedbtouser("Delete_db_to_user", model);
            if (ret == 1)
                return Ok(new { status = true });
            else
                return Ok(new { status = false });
        }
        #endregion
        [HttpPost("duc-db-list")]
        public async Task<IActionResult> ducdblist([FromBody] DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            string sql = "";

            sql = "EXEC  [dbo].[Sp_databaseuser] " +
                  "@d_database = '" + model.value + "', " +
                  "@StatementType = N'Get_Db_List'";
            DataTable dt = csqlr.dbReaderFill(sql);
            csqlr.close();
            if (dt.Rows.Count > 0)
                return Ok(ReportModelContext.searializeDt(dt));
            else
                return Ok(new { status = false });
        }
        [HttpPost("duc-user-list")]
        public async Task<IActionResult> ducdbuserlist([FromBody] DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            string sql = "";

            sql = "EXEC  [dbo].[Sp_databaseuser] " +
                  "@u_username = '" + model.value + "', " +
                  "@StatementType = N'Get_User_List'";
            DataTable dt = csqlr.dbReaderFill(sql);
            csqlr.close();
            if (dt.Rows.Count > 0)
                return Ok(ReportModelContext.searializeDt(dt));
            else
                return Ok(new { status = false });
        }
        [HttpGet("duc-main-user")]
        public async Task<IActionResult> getallmainuser()
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
            string sql = "";

            sql = "EXEC  [dbo].[Sp_databaseuser] " +
                  "@StatementType = N'Get_Main_User'";
            DataTable dt = csqlr.dbReaderFill(sql);
            csqlr.close();
            if (dt.Rows.Count > 0)
                return Ok(ReportModelContext.searializeDt(dt));
            else
                return Ok(new { status = false });
        }
        #region save,edit,delete main_user
        [HttpPost("fnSaveMainUser")]
        public int savemainuser(string statement, DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            csqlr.OpenConnection();
            int tres = -6;
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Sp_databaseuser";

                SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                parm.Direction = ParameterDirection.ReturnValue;

                cmd.Parameters.Add(parm);
                cmd.Parameters.AddWithValue("@id", model.id);
                cmd.Parameters.AddWithValue("@username", model.username);
                cmd.Parameters.AddWithValue("@password", model.password);
                cmd.Parameters.AddWithValue("@role", model.role);
                cmd.Parameters.AddWithValue("@status", model.status);
                cmd.Parameters.AddWithValue("@StatementType", statement);
                cmd.Connection = csqlr.shop;
                SqlDataReader dr = cmd.ExecuteReader();
                dr.Dispose();
                dr.Close();
                tres = Convert.ToInt32(parm.Value);
            }
            catch (Exception m)
            {
                Console.WriteLine(m.ToString());
            }
            return tres;
        }
        [HttpPost("duc-save-mainuser")]
        public async Task<IActionResult> ducsavemainuser([FromBody] DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            int ret = savemainuser("Insert_main_user", model);
            if (ret == 1)
                return Ok(new { status = true });
            else
                return Ok(new { status = false });
        }
        [HttpPost("duc-update-mainuser")]
        public async Task<IActionResult> ducupdatemainuser([FromBody] DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            int ret = savemainuser("Update_main_user", model);
            if (ret == 1)
                return Ok(new { status = true });
            else
                return Ok(new { status = false });
        }
        [HttpPost("duc-delete-mainuser")]
        public async Task<IActionResult> ducdeletemainuser([FromBody] DbUserModel model)
        {
            ClientSqlServer csqlr = new ClientSqlServer();
            int ret = savemainuser("Delete_main_user", model);
            if (ret == 1)
                return Ok(new { status = true });
            else
                return Ok(new { status = false });
        }
        #endregion
    }
}
