using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class FileUpController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IWebHostEnvironment webHostEnvironment;
        public FileUpController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            dbContext = context;
            webHostEnvironment = hostEnvironment;
        }

        [HttpPost("start-route-ride"), DisableRequestSizeLimit]
        public async Task<IActionResult> startRouteRide([FromBody] StartRouteRide model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            //string meterphotoFile = CommonHelper.UploadedFile(webHostEnvironment,model.meterPhoto);
            //int attachfId = usqlre.documentAttachments("1",meterphotoFile);
            //string sql = "";
            //DataTable shoproots = usqlre.dbReaderFill(sql);
            //usqlre.close();

            string sql = "insert into route_tracking(rt_starting_latitude,rt_starting_longitude,rt_date,rt_start_time,meterReading,routeId) ";
            sql += " values('" + model.latitude + "','" + model.longitude + "','" + model.date + "','" + model.time + "','" + model.meterReading + "','" + model.routeId + "')";

            sql = "insert into route_tracking(rt_starting_latitude,rt_starting_longitude,rt_date,rt_start_time,rt_start_meter_reading,rt_route_id,rt_salesman) " +
                 " values('" + model.latitude + "','" + model.latitude + "','" + model.date + "','" + model.time + "'," + model.meterReading + "," + model.routeId + ","+Convert.ToInt32(usqlre.userId)+")";


            int inertId = usqlre.insertStartRouteRide(model);


            if (inertId > 0)
            {
                usqlre.close();
                return Ok(new { status = true, insert_id = inertId });
            }
            else
            {
                usqlre.close();
                return Ok(new { status = false });
            }
        }


        [HttpPost("end-route-ride"), DisableRequestSizeLimit]
        public async Task<IActionResult> endRouteRide([FromBody] StartRouteRide model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            //string meterphotoFile = CommonHelper.UploadedFile(webHostEnvironment,model.meterPhoto);
            //int attachfId = usqlre.documentAttachments("1",meterphotoFile);
            //string sql = "";
            //DataTable shoproots = usqlre.dbReaderFill(sql);
            //usqlre.close();

            string sql = "update route_tracking set rt_ending_latitude ='" + model.latitude + "'  , rt_ending_longitude='" + model.longitude + "',rt_end_time='" + model.time + "',rt_end_meter_reading ='" + model.meterReading + "' where rt_id=" + model.rowId + "";

            if (usqlre.dbExecute(sql))
            {
                usqlre.close();
                return Ok(new { status = true });
            }
            else
            {
                usqlre.close();
                return Ok(new { status = false });
            }
        }

        [HttpPost("test-file-upload"), DisableRequestSizeLimit]
        public async Task<IActionResult> testFileUpload([FromBody] FileUpModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string meterphotoFile = CommonHelper.UploadedFile(webHostEnvironment, model.filename);
            int attachfId = usqlre.documentAttachments("1", meterphotoFile);
            return Ok(new { status = false });
        }
    }
}
