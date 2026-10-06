using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    [ApiController]
    public class EcommerceLoginController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public EcommerceLoginController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = SqlConnectionPool.Apply(configuration.GetConnectionString("ConnStr"));
        }
        [HttpPost("ecommerce-login")]
        public async Task<IActionResult> EcommerceLogin(string MobileNo)
        {
            if (string.IsNullOrEmpty(MobileNo))
            {
                return Unauthorized(new
                {
                    status = false,
                    statusCode = 401,
                    message = "Mobile number is required.",
                    data = (object)null
                });
            }

            try
            {
                string otpCode = null;
                string userId = null;
                string loginId = null;
                string role = null;
                string ledgerId = null;

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_UsersManage", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Mobile", MobileNo);
                        cmd.Parameters.AddWithValue("@Action", 1);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                otpCode = reader["OtpCode"]?.ToString();
                                userId = reader["UserId"]?.ToString();
                                loginId = reader["LoginId"]?.ToString();  
                                role = reader["Role"]?.ToString();
                                ledgerId = reader["LedgerId"]?.ToString();

                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "User not found.",
                        data = (object)null
                    });
                }

                var (tokenString, expiration) = GenerateJwtToken(userId, loginId, role,ledgerId);

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Token Created Successfully",
                    data = new
                    {
                        token = tokenString,
                        expiration = expiration,
                        otpCode = otpCode                        
                    }
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = e.Message,
                    data = (object)null
                });
            }
        }
        [HttpPost("ecommerce-login-customer")]
        public async Task<IActionResult> EcommerceLoginCustomer(string MobileNo)
        {
            if (string.IsNullOrEmpty(MobileNo))
            {
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "Mobile number is required"
                });
            }

            try
            {
                string otpCode = null;
                string userId = null;
                string loginId = null;
                string role = null;
                string ledgerId = null;

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();
                    bool userExists = false;
                    using (SqlCommand checkCmd = new SqlCommand(
                    "SELECT 1 FROM gnl_ecommerce_login WHERE gel_login_id = @Mobile", con))
                    {
                        checkCmd.Parameters.AddWithValue("@Mobile", MobileNo);
                        userExists = await checkCmd.ExecuteScalarAsync() != null;
                    }
                    if (!userExists)
                    {
                        using (SqlCommand insertCmd = new SqlCommand("Sp_EcommerceLogin", con))
                        {
                            insertCmd.CommandType = CommandType.StoredProcedure;
                            insertCmd.Parameters.AddWithValue("@StatementType", "Insert");
                            insertCmd.Parameters.AddWithValue("@gel_login_id", MobileNo);
                            insertCmd.Parameters.AddWithValue("@gel_role", "customer");
                            insertCmd.Parameters.AddWithValue("@gel_ledger_id", 0);
                            insertCmd.Parameters.AddWithValue("@gel_created_by", 0);
                            insertCmd.Parameters.AddWithValue("@gel_profile_image", "");

                            try
                            {
                                await insertCmd.ExecuteNonQueryAsync();
                            }
                            catch
                            {
                                // Ignore duplicate user error
                            }
                        }

                        /* 🔹 STEP 2: LOGIN + OTP GENERATION */
                        using (SqlCommand loginCmd = new SqlCommand("sp_UsersManage", con))
                        {
                            loginCmd.CommandType = CommandType.StoredProcedure;
                            loginCmd.Parameters.AddWithValue("@Mobile", MobileNo);
                            loginCmd.Parameters.AddWithValue("@Action", 1);

                            using (SqlDataReader reader = await loginCmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    otpCode = reader["OtpCode"]?.ToString();
                                    userId = reader["UserId"]?.ToString();
                                    loginId = reader["LoginId"]?.ToString();
                                    role = reader["Role"]?.ToString();
                                    ledgerId = reader["LedgerId"]?.ToString();
                                }
                            }
                        }
                    }
                    else
                    {
                        return Unauthorized(new
                        {
                            status = false,
                            statusCode = 401,
                            message = "User Already Exist"
                        });
                    }
                        /* 🔹 STEP 1: INSERT CUSTOMER IF NOT EXISTS */
                    
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Login failed"
                    });
                }

                var (tokenString, expiration) = GenerateJwtToken(userId, loginId, role, ledgerId);

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "OTP sent successfully",
                    data = new
                    {
                        token = tokenString,
                        expiration,
                        otpCode
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message
                });
            }
        }
        private (string token, DateTime expiration) GenerateJwtToken(string userId, string loginId, string role, string ledgerId)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, UserRoles.EcommerceUser),
                new Claim("LoginId", loginId ?? string.Empty),
                new Claim("UserRole", role ?? string.Empty),
                new Claim("LedgerId", ledgerId ?? string.Empty)
            };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddYears(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            return (tokenHandler.WriteToken(token), token.ValidTo);
        }




        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(string OtpCode)
        {
            // Get userId from JWT token
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_UsersManage", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", 3);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@OtpCode", OtpCode);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataSet ds = new DataSet();
                            da.Fill(ds);

                            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                            {
                                return Unauthorized(new
                                {
                                    status = false,
                                    statusCode = 401,
                                    message = "OTP expired or incorrect.",
                                    data = (object)null
                                });
                            }

                            var userRow = ds.Tables[0].Rows[0];
                            var role = userRow["role"]?.ToString();

                            var data = new
                            {
                                userId = userRow["userId"],
                                mobileNo = userRow["mobileNo"],
                                role = role,
                                ledgerId = userRow["ledgerId"],
                                ledgerName = userRow["ledgerName"] == DBNull.Value? "" : userRow["ledgerName"].ToString(),
                                profileImage = userRow["profileImage"],
                                areas = (role == "salesman" && ds.Tables.Count > 1)
                                    ? ds.Tables[1].AsEnumerable().Select(a => new
                                    {
                                        areaId = a["areaId"],
                                        areaName = a["areaName"]
                                    })
                                    : Enumerable.Empty<object>(),

                                routes = (role == "salesman" && ds.Tables.Count > 2)
                                    ? ds.Tables[2].AsEnumerable().Select(r => new
                                    {
                                        routeId = r["routeId"],
                                        routeName = r["routeName"]
                                    })
                                    : Enumerable.Empty<object>(),

                            };

                            return Ok(new
                            {
                                status = true,
                                statusCode = 200,
                                message = "OTP Verified Successfully",
                                data
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message,
                    data = (object)null
                });
            }
        }
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("verify-otp-ecart")]
        public async Task<IActionResult> VerifyOtpCustomer(string OtpCode)
        {
           // Get userId from JWT token

           var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_UsersManage", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", 2);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@OtpCode", OtpCode);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataSet ds = new DataSet();
                            da.Fill(ds);

                            if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                            {
                                return Unauthorized(new
                                {
                                    status = false,
                                    statusCode = 401,
                                    message = "OTP expired or incorrect.",
                                    data = (object)null
                                });
                            }

                            var userRow = ds.Tables[0].Rows[0];
                            var role = userRow["role"]?.ToString();

                            var data = new
                            {
                                userId = userRow["userId"],
                                mobileNo = userRow["mobileNo"],
                                role = role,
                                ledgerId = userRow["ledgerId"],
                                ledgerName = userRow["ledgerName"],
                                profileImage = userRow["profileImage"],
                                Address1 = userRow["Address1"],
                                Address2 = userRow["Address2"],
                                Address3 = userRow["Address3"],
                                areas = (role == "salesman" && ds.Tables.Count > 1)
                                    ? ds.Tables[1].AsEnumerable().Select(a => new
                                    {
                                        areaId = a["areaId"],
                                        areaName = a["areaName"]
                                    })
                                    : Enumerable.Empty<object>(),

                                routes = (role == "salesman" && ds.Tables.Count > 2)
                                    ? ds.Tables[2].AsEnumerable().Select(r => new
                                    {
                                        routeId = r["routeId"],
                                        routeName = r["routeName"]
                                    })
                                    : Enumerable.Empty<object>(),

                            };

                            return Ok(new
                            {
                                status = true,
                                statusCode = 200,
                                message = "OTP Verified Successfully",
                                data
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message,
                    data = (object)null
                });
            }
        }


    }
}



