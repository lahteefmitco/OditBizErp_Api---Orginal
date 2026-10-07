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
using Microsoft.AspNetCore.Cors;

namespace MictcoWebService.Controllers
{
    
    [ApiController]
    public class ClientLoginController : ControllerBase
    {
       
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration _configuration;
        private readonly ClientLoginContext _context;

        public ClientLoginController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            _configuration = configuration;

            _context = new ClientLoginContext();
        }

        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("app-auth")]
        public async Task<ActionResult<DataBaseModel>> authClient(ClientLoginModel client)
        {
            ClientSqlServer csqlr = null;
            try
            {
                csqlr = new ClientSqlServer();
                DataTable dt = _context.authLogin(client, csqlr);
                if (dt == null)
                {
                    return Unauthorized(new { status = false, message = "Unable to connect to MOBIL_USERS" });
                }
                if (dt.Rows.Count > 0 && client != null)
                {
                    DataTable tokenDt = new DataTable();
                    tokenDt.Columns.Add("label");
                    tokenDt.Columns.Add("value");
                    tokenDt.Columns.Add("expiration");

                    foreach (DataRow dr in dt.Rows)
                    {
                        string d_host = dr["d_host"].ToString();
                        string d_database = dr["d_database"].ToString();
                        string d_username = dr["d_username"].ToString();
                        string d_password = dr["d_password"].ToString();

                        if (d_host.Length > 0 && d_database.Length > 0 && d_username.Length > 0 && d_password.Length > 0)
                        {
                            var authClaims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, client.ClientId),
                                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                                new Claim(ClaimTypes.NameIdentifier, client.ClientId)
                            };
                            authClaims.Add(new Claim(ClaimTypes.Role, UserRoles.ClientUser));
                            authClaims.Add(new Claim(ClaimTypes.Actor, CommonHelper.tokenEncrypt(d_host)));
                            authClaims.Add(new Claim(ClaimTypes.GivenName, CommonHelper.tokenEncrypt(d_database)));
                            authClaims.Add(new Claim(ClaimTypes.Gender, CommonHelper.tokenEncrypt(d_username)));
                            authClaims.Add(new Claim(ClaimTypes.DateOfBirth, CommonHelper.tokenEncrypt(d_password)));
                            authClaims.Add(new Claim(ClaimTypes.Anonymous, "-1"));
                            authClaims.Add(new Claim(ClaimTypes.PostalCode, "-1"));
                            authClaims.Add(new Claim(ClaimTypes.Sid, "-1"));
                            authClaims.Add(new Claim(ClaimTypes.Surname, "-1"));
                            authClaims.Add(new Claim(ClaimTypes.Locality, "-1"));
                            authClaims.Add(new Claim(ClaimTypes.StreetAddress, "-1"));

                            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
                            var token = new JwtSecurityToken(
                                issuer: _configuration["JWT:ValidIssuer"],
                                audience: _configuration["JWT:ValidAudience"],
                                expires: DateTime.Now.AddHours(30000),
                                claims: authClaims,
                                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                                );
                            //return Ok(new
                            //{
                            //    token = new JwtSecurityTokenHandler().WriteToken(token),
                            //    expiration = token.ValidTo,
                            //    status = true
                            //});
                            var token1 = new JwtSecurityTokenHandler().WriteToken(token);

                            DataRow _dr_token = tokenDt.NewRow();
                            _dr_token["label"] = d_database;
                            _dr_token["value"] = token1;
                            _dr_token["expiration"] = token.ValidTo;
                            tokenDt.Rows.Add(_dr_token);
                        }
                        else
                        {
                            //return Unauthorized(new { status = false });
                        }
                    }

                    return Ok(new
                    {
                        token = ReportModelContext.searializeDt(tokenDt),
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
                csqlr?.close();
            }
        }
        //[HttpPost("app-auth-decrypt")]
        //public async Task<ActionResult<DataBaseModel>> decrypt(string client)
        //{ 

        //}
    }

}
