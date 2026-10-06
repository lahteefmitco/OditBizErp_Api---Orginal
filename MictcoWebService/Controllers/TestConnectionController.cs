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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace MictcoWebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestConnectionController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public TestConnectionController(IConfiguration configuration)
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
                                loginId = reader["LoginId"]?.ToString();  // Mobile
                                role = reader["Role"]?.ToString();          // Role
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

                var (tokenString, expiration) = GenerateJwtToken(userId, loginId, role);

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Token Created Successfully",
                    data = new
                    {
                        token = tokenString,
                        expiration = expiration,
                        otpCode = otpCode,
                        userId = userId,
                        loginId = loginId,
                        role = role
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

        private (string token, DateTime expiration) GenerateJwtToken(string userId, string loginId, string role)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role ?? "customer"), // fallback
                new Claim("LoginId", loginId ?? string.Empty),
                new Claim("UserRole", role ?? string.Empty)
            };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            return (tokenHandler.WriteToken(token), token.ValidTo);
        }
    }
}