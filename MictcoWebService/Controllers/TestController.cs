using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    // [Route("[controller]")]

    //[ApiController]
    //[Route("[controller]")]
    public class TestController : ControllerBase
    {

        [HttpGet("test-api")]
        public IActionResult testApiOne()
        {
            int fasil = 0;
            
            return Ok(new { id = 3, status = true });
        }

        [HttpPost("test-post")]
        public IActionResult testPost()
        {
            string name = HttpContext.Request.Form["name"].ToString();
            int fasil = 0;
            return Ok(new { name = name, status = true });
        }

        [HttpGet("test-it-now/{id}")]
        public IActionResult testItNow(int id)
        {
            string connetionString;
            SqlConnection cnn;
            //connetionString = @"Data Source=192.168.1.95;Initial Catalog=G7;User ID=sa;Password=wf";
            try
            {
                connetionString = @"Data Source = 35.244.38.164,1436\\WEBDB; Initial Catalog = MOBIL_USERS; User ID = mictco; Password = Mis@#meizOn123@#LLP ";
                SqlConnection shop = new SqlConnection(connetionString);
                shop.Open();

                DataTable _temp = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM DATABASES WHERE D_ID = "+id+"", shop);
                da.SelectCommand.CommandTimeout = 0;
                da.Fill(_temp);
                DataTable dt = _temp;
                shop.Close();
                int fasil = 0;
                return Ok(ReportModelContext.searializeDt(dt));

            }
            catch (Exception emsg)
            {
               return Ok(new {msg = emsg});
            }
        }

    }
}
