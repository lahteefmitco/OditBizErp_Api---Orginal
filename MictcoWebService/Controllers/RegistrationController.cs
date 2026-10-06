using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        [HttpPost("create-app-contact")]
        public async Task<IActionResult> createAppContact([FromBody] AppContact model)
        {
            ClientSqlServer csqlre = new ClientSqlServer();
            using (csqlre.shop)
            {
                string insertQuery = "INSERT into app_contact (username,password,phone,place,status) VALUES (@username,@password,@phone,@place,@status)";

                using (SqlCommand myCommand = new SqlCommand(insertQuery))
                {
                    myCommand.Connection = csqlre.shop;
                    myCommand.Parameters.Add("@username", SqlDbType.VarChar, 30).Value = model.username;
                    myCommand.Parameters.Add("@password", SqlDbType.VarChar, 30).Value = model.password;
                    myCommand.Parameters.Add("@phone", SqlDbType.VarChar, 30).Value = model.phone;
                    myCommand.Parameters.Add("@place", SqlDbType.VarChar, 30).Value = model.place;
                    myCommand.Parameters.Add("@status", SqlDbType.Int, 30).Value = 1;
                    csqlre.OpenConnection();
                    myCommand.ExecuteNonQuery();
                }
            }
            csqlre.close();
            return Ok(new { status = true });
        }
    }
}
