using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualBasic;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using Newtonsoft.Json;                                                    //SHAHIL//
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class ServiceAppController : Controller
    {
        /// <summary>
        /// Limits how many get-tickets queries run on SQL Server at the same time.
        /// Without this, bulk requests pile up hundreds of heavy queries that choke
        /// SQL Server — and it stays slow even after the burst finishes.
        /// 10 concurrent is enough to keep the API responsive without overloading DB.
        /// </summary>
        private static readonly SemaphoreSlim _ticketQueryThrottle = new SemaphoreSlim(10, 10);

        // ---------------------------------------------------
        // INSERT
        // ---------------------------------------------------
        [HttpPost("insert-service-complaint")]
        public async Task<IActionResult> InsertServiceComplaint(ServiceComplaintModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "insert_complaint");
                cmd.Parameters.AddWithValue("@scr_customer_name", model.scr_customer_name);
                cmd.Parameters.AddWithValue("@scr_mobile_no", model.scr_mobile_no);
                cmd.Parameters.AddWithValue("@scr_token_date", model.scr_token_date);
                cmd.Parameters.AddWithValue("@scr_email", model.scr_email);
                cmd.Parameters.AddWithValue("@scr_contact_no", model.scr_contact_no);
                cmd.Parameters.AddWithValue("@scr_category_id", model.scr_category_id);
                cmd.Parameters.AddWithValue("@scr_customer_address", model.scr_customer_address);
                cmd.Parameters.AddWithValue("@scr_findings", model.scr_findings);
                cmd.Parameters.AddWithValue("@src_isLedger", model.src_isLedger);
                cmd.Parameters.AddWithValue("@src_ledger_id", model.src_ledger_id);
                cmd.Parameters.AddWithValue("@user_id", usqlre.userId);
                cmd.Parameters.AddWithValue("@scr_rout_id", model.scr_rout_id);


                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Inserted successfully",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }

        // ---------------------------------------------------
        // UPDATE
        // ---------------------------------------------------
        [HttpPost("update-service-complaint")]
        public async Task<IActionResult> UpdateServiceComplaint(ServiceComplaintModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "update_complaint");
                cmd.Parameters.AddWithValue("@scr_id", model.scr_id);
                cmd.Parameters.AddWithValue("@scr_customer_name", model.scr_customer_name);
                cmd.Parameters.AddWithValue("@scr_mobile_no", model.scr_mobile_no);
                cmd.Parameters.AddWithValue("@scr_email", model.scr_email);
                cmd.Parameters.AddWithValue("@scr_contact_no", model.scr_contact_no);
                cmd.Parameters.AddWithValue("@scr_category_id", model.scr_category_id);
                cmd.Parameters.AddWithValue("@scr_customer_address", model.scr_customer_address);
                cmd.Parameters.AddWithValue("@scr_findings", model.scr_findings);
                cmd.Parameters.AddWithValue("@src_isLedger", model.src_isLedger);
                cmd.Parameters.AddWithValue("@src_ledger_id", model.src_ledger_id);
                cmd.Parameters.AddWithValue("@user_id", usqlre.userId);
                cmd.Parameters.AddWithValue("@scr_rout_id", model.scr_rout_id);


                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Updated successfully",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }

        // ---------------------------------------------------
        // DELETE
        // ---------------------------------------------------
        [HttpPost("delete-service-complaint/{id}")]
        public async Task<IActionResult> DeleteServiceComplaint(int id)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "delete_complaint");
                cmd.Parameters.AddWithValue("@scr_id", id);

                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Deleted successfully",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }

        // ---------------------------------------------------
        // GET ALL
        // ---------------------------------------------------
        [HttpGet("get-service-complaint")]
        public async Task<IActionResult> GetAllServiceComplaint()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "get_complaint"));

                // Prepare object
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                // Convert to JSON string
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        // ---------------------------------------------------
        // GET BY ID
        // ---------------------------------------------------
        [HttpGet("get-service-complaint-by-id/{id}")]
        public async Task<IActionResult> GetAllComplaintsById(int id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "getById_complaint"),
                    new SqlParameter("@scr_id", id));

                // Prepare object
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                // Convert to JSON string
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        // ---------------------------------------------------
        // GET BY ID
        // ---------------------------------------------------
        [HttpGet("get-service-complaint-by-mobile/{mobile}")]
        public async Task<IActionResult> GetAllComplaintsByMob(string mobile)
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "getByMob_complaint"),
                    new SqlParameter("@scr_mobile_no", (object)mobile ?? DBNull.Value));

                // Prepare object
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                // Convert to JSON string
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        // ---------------------------------------------------
        // GET ALL
        // ---------------------------------------------------
        [HttpGet("get-service-category")]
        public async Task<IActionResult> GetAllServiceCategory()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "get_category"));

                // Prepare object
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                // Convert to JSON string
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        [HttpPost("insert-brand")]
        public async Task<IActionResult> InsertBrand(BrandModel model)
        {
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "insert_company"),
                    new SqlParameter("@company_name", (object)model.name ?? DBNull.Value),
                    new SqlParameter("@company_remarks", (object)model.remarks ?? DBNull.Value));

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Inserted successfully",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }


        [HttpGet("get-service-brand")]
        public async Task<IActionResult> GetAllServiceBrand()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "get_company"));

                // Prepare object
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                // Convert to JSON string
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        [HttpPost("insert-route")]
        public async Task<IActionResult> InsertRoute(BrandModel model)
        {
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "insert_rout"),
                    new SqlParameter("@name", (object)model.name ?? DBNull.Value),
                    new SqlParameter("@remarks", (object)model.remarks ?? DBNull.Value));

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Inserted successfully",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }


        [HttpGet("get-service-route")]
        public async Task<IActionResult> GetAllServiceRoute()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "get_rout"));

                // Prepare object
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                // Convert to JSON string
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        [HttpPost("insert-color")]
        public async Task<IActionResult> InsertColor(BrandModel model)
        {
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "insert_color"),
                    new SqlParameter("@name", (object)model.name ?? DBNull.Value),
                    new SqlParameter("@remarks", (object)model.remarks ?? DBNull.Value));

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Inserted successfully",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpGet("get-service-color")]
        public async Task<IActionResult> GetAllServiceColor()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "SELECT clr_id, clr_name, clr_remarks FROM inv_color ORDER BY clr_name",
                    CommandType.Text);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        [HttpPost("insert-model")]
        public async Task<IActionResult> InsertModel(BrandModel model)
        {
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "insert_model"),
                    new SqlParameter("@name", (object)model.name ?? DBNull.Value),
                    new SqlParameter("@remarks", (object)model.remarks ?? DBNull.Value),
                    new SqlParameter("@model_company_id", model.model_company_id));

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Inserted successfully",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpGet("get-service-model")]
        public async Task<IActionResult> GetAllServiceModel(int model_company_id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "get_model"),
                    new SqlParameter("@model_company_id", model_company_id));

                // Prepare object
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                // Convert to JSON string
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        [HttpGet("get-service-cash")]
        public async Task<IActionResult> GetAllServiceCash()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_acc_reg",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "Selectcash"));

                // Prepare object
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                // Convert to JSON string
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        [HttpGet("get-service-card")]
        public async Task<IActionResult> GetAllServiceCard()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_acc_reg",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "selectBank"));

                // Prepare object
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                // Convert to JSON string
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        // ---------------------------------------------------
        // INSERT
        // ---------------------------------------------------
        [HttpPost("insert-ticket")]
        public async Task<IActionResult> InsertTicket([FromForm] ServiceTicketModel model)
        {
            if (model == null)
                return BadRequest(new { status = 0, message = "Invalid payload" });


            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();
                if (!string.IsNullOrWhiteSpace(model.LendItemsJson))
                {
                    model.LendItems =
                        JsonConvert.DeserializeObject<List<LendRowModel>>(model.LendItemsJson);
                }
                // Items collected
                string si_items_collected = model.list_itemscollected != null
                    ? string.Join(",", model.list_itemscollected)
                    : "";

                DataTable lendItemsTable = CreateLendItemsTable(model.LendItems);
                DataTable imageTable = await CreateImageTable(model.Images);

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "InsertTicket");

                cmd.Parameters.Add("@scr_customer_name", SqlDbType.NVarChar).Value = model.CustomerName ?? "";
                cmd.Parameters.AddWithValue("@si_date", SqlDbType.DateTime).Value = model.Date ?? (object)DBNull.Value;

                cmd.Parameters.Add("@si_batteryno", SqlDbType.NVarChar).Value = model.BatteryNo ?? "";

                cmd.Parameters.Add("@si_items_collected", SqlDbType.NVarChar).Value = si_items_collected ?? "";

                cmd.Parameters.Add("@si_company", SqlDbType.Int).Value = model.BrandId ?? 0;
                cmd.Parameters.Add("@si_color", SqlDbType.Int).Value = model.ColorId ?? 0;


                cmd.Parameters.Add("@si_model", SqlDbType.NVarChar).Value = model.ModelNo ?? "";

                cmd.Parameters.Add("@si_imei", SqlDbType.NVarChar).Value = model.IMEI ?? "";

                cmd.Parameters.Add("@si_expected_date", SqlDbType.DateTime).Value = model.ExpectedDate ?? (object)DBNull.Value;

                cmd.Parameters.Add("@si_remarks", SqlDbType.NVarChar).Value = model.EstimateCost ?? "";

                cmd.Parameters.Add("@si_cash_paid_acc", SqlDbType.Int).Value = model.CashPaidAccount ?? 0;
                cmd.Parameters.Add("@si_cash_recieved", SqlDbType.Int).Value = model.CashAmount ?? 0;
                cmd.Parameters.Add("@si_bankacc", SqlDbType.Int).Value = model.CardAccount ?? 0;
                cmd.Parameters.Add("@si_card_amount", SqlDbType.Int).Value = model.CardAmount ?? 0;


                //cmd.Parameters.Add("@si_cash_recieved", SqlDbType.Decimal).Value = model.AdvanceAmount ?? 0;

                cmd.Parameters.Add("@si_category_id", SqlDbType.Int).Value = model.CategoryId ?? 0;

                cmd.Parameters.Add("@si_coupon_no", SqlDbType.NVarChar).Value = model.CouponNo ?? "";

                cmd.Parameters.Add("@si_location_id", SqlDbType.Int).Value = usqlre.locationId;

                cmd.Parameters.Add("@si_user_id", SqlDbType.Int).Value = usqlre.userId;

                SqlParameter tvp = cmd.Parameters.AddWithValue("@type2", lendItemsTable);
                tvp.SqlDbType = SqlDbType.Structured;

                SqlParameter imgTvp = cmd.Parameters.AddWithValue("@type3", imageTable);
                imgTvp.SqlDbType = SqlDbType.Structured;


                SqlParameter entryOut = new SqlParameter("@entryno_ret", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    Scale = 0
                };
                cmd.Parameters.Add(entryOut);

                await cmd.ExecuteNonQueryAsync();
                decimal entryNo = Convert.ToDecimal(entryOut.Value);

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Ticket created successfully",
                    data = entryNo
                });
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.close();
            }
        }
        [HttpPost("update-ticket")]
        public async Task<IActionResult> UpdateTicket([FromForm] ServiceTicketModel model, int ticketId)
        {
            if (model == null)
                return BadRequest(new { status = 0, message = "Invalid payload" });

            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                if (!string.IsNullOrWhiteSpace(model.LendItemsJson))
                {
                    model.LendItems =
                        JsonConvert.DeserializeObject<List<LendRowModel>>(model.LendItemsJson);
                }

                string si_items_collected = model.list_itemscollected != null
                    ? string.Join(",", model.list_itemscollected)
                    : "";

                DataTable lendItemsTable = CreateLendItemsTable(model.LendItems);
                DataTable imageTable = await CreateImageTable(model.Images);

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "UpdateTicket");
                cmd.Parameters.AddWithValue("@si_entryno", ticketId);

                cmd.Parameters.Add("@scr_customer_name", SqlDbType.NVarChar).Value = model.CustomerName ?? "";
                cmd.Parameters.Add("@si_date", SqlDbType.DateTime).Value = model.Date ?? (object)DBNull.Value;
                cmd.Parameters.Add("@si_batteryno", SqlDbType.NVarChar).Value = model.BatteryNo ?? "";
                cmd.Parameters.Add("@si_items_collected", SqlDbType.NVarChar).Value = si_items_collected;
                cmd.Parameters.Add("@si_company", SqlDbType.Int).Value = model.BrandId ?? 0;
                cmd.Parameters.Add("@si_color", SqlDbType.Int).Value = model.ColorId ?? 0;
                cmd.Parameters.Add("@si_model", SqlDbType.NVarChar).Value = model.ModelNo ?? "";
                cmd.Parameters.Add("@si_imei", SqlDbType.NVarChar).Value = model.IMEI ?? "";
                cmd.Parameters.Add("@si_expected_date", SqlDbType.DateTime).Value =
                    model.ExpectedDate ?? (object)DBNull.Value;
                cmd.Parameters.Add("@si_remarks", SqlDbType.NVarChar).Value = model.EstimateCost ?? "";
                cmd.Parameters.Add("@si_cash_paid_acc", SqlDbType.Int).Value = model.CashPaidAccount ?? 0;
                cmd.Parameters.Add("@si_cash_recieved", SqlDbType.Decimal).Value = model.CashAmount ?? 0;
                cmd.Parameters.Add("@si_bankacc", SqlDbType.Int).Value = model.CardAccount ?? 0;
                cmd.Parameters.Add("@si_card_amount", SqlDbType.Decimal).Value = model.CardAmount ?? 0;
                cmd.Parameters.Add("@si_category_id", SqlDbType.Int).Value = model.CategoryId ?? 0;
                cmd.Parameters.Add("@si_coupon_no", SqlDbType.NVarChar).Value = model.CouponNo ?? "";
                cmd.Parameters.Add("@si_location_id", SqlDbType.Int).Value = usqlre.locationId;
                cmd.Parameters.Add("@si_user_id", SqlDbType.Int).Value = usqlre.userId;

                SqlParameter tvp = cmd.Parameters.AddWithValue("@type2", lendItemsTable);
                tvp.SqlDbType = SqlDbType.Structured;

                SqlParameter imgTvp = cmd.Parameters.AddWithValue("@type3", imageTable);
                imgTvp.SqlDbType = SqlDbType.Structured;

                await cmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Ticket updated successfully",
                    data = ticketId
                });
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.close();
            }
        }

        private DataTable CreateLendItemsTable(List<LendRowModel> items)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("li_in", typeof(int));
            dt.Columns.Add("li_out", typeof(int));
            dt.Columns.Add("li_remarks", typeof(string));
            dt.Columns.Add("li_ir_id", typeof(int));
            dt.Columns.Add("li_ir_mrp", typeof(decimal));
            dt.Columns.Add("li_ift_id", typeof(int));

            if (items == null) return dt;

            foreach (var item in items)
            {
                dt.Rows.Add(
                    0,
                    0,
                    item.li_remarks ?? "",
                    item.li_ir_id,
                    0m,
                    0
                );
            }

            return dt;
        }

        private async Task<DataTable> CreateImageTable(List<IFormFile> images)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ii_reference_type", typeof(string));
            dt.Columns.Add("ii_reference_id", typeof(int));
            dt.Columns.Add("ii_image", typeof(string));
            dt.Columns.Add("ii_image_data", typeof(byte[]));

            if (images == null || images.Count == 0)
                return dt;

            foreach (var img in images)
            {
                string path = await SaveImage(img, "Service");

                dt.Rows.Add(
                    "Service",
                    0,
                    path,     // ✅ /uploads/Service/xxx.jpg
                    DBNull.Value
                );
            }

            return dt;
        }
        [HttpPost("remove-service-image/{imageId}")]
        public async Task<IActionResult> RemoveServiceImage(int imageId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                string imagePath = null;

                // 1️⃣ Get image path
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ii_image FROM inv_images WHERE ii_id = @id",
                    usqlre.shop))
                {
                    cmd.Parameters.AddWithValue("@id", imageId);
                    imagePath = Convert.ToString(await cmd.ExecuteScalarAsync());
                }

                if (string.IsNullOrEmpty(imagePath))
                    return NotFound(new { status = false, message = "Image not found" });

                // 2️⃣ Delete DB record
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM inv_images WHERE ii_id = @id",
                    usqlre.shop))
                {
                    cmd.Parameters.AddWithValue("@id", imageId);
                    await cmd.ExecuteNonQueryAsync();
                }


                // 3️⃣ Delete physical file
                string fullPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    imagePath.TrimStart('/')
                );

                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                return Ok(new
                {
                    status = true,
                    message = "Image removed successfully"
                });
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpGet("get-ticket-by-id/{ticketId}")]
        public async Task<IActionResult> GetTicketById(int ticketId)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "GetTicketById");
                cmd.Parameters.AddWithValue("@si_entryno", ticketId);

                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);


                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Ticket not found"
                    });
                }
                var ticketRow = ds.Tables[0].Rows[0];
                var ticket = ds.Tables[0].Columns
                    .Cast<DataColumn>()
                    .ToDictionary(col => col.ColumnName, col => ticketRow[col]);
                var companyRow = ds.Tables[3].Rows[0];
                var company = ds.Tables[3].Columns
                    .Cast<DataColumn>()
                    .ToDictionary(col => col.ColumnName, col => companyRow[col]);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Ticket details fetched successfully",
                    data = new
                    {
                        ticket,
                        lendItems = ds.Tables.Count > 1 ? ds.Tables[1] : null,
                        images = ds.Tables.Count > 2 ? ds.Tables[2] : null,
                        company,
                    }
                };
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }

        //[HttpPost("insert-ticket")]
        //public async Task<IActionResult> InsertTicket([FromForm] ServiceTicketModel model)
        //{
        //    if (model == null)
        //        return BadRequest(new { status = false, message = "Invalid payload" });

        //    UserSqlServer usqlre = new UserSqlServer(this);

        //     ===============================
        //     Deserialize LendItems JSON
        //     ===============================
        //    if (!string.IsNullOrWhiteSpace(model.LendItemsJson))
        //    {
        //        model.LendItems =
        //            JsonConvert.DeserializeObject<List<LendRowModel>>(model.LendItemsJson);
        //    }

        //     ===============================
        //     Items collected
        //     ===============================
        //    string si_items_collected = model.list_itemscollected != null
        //        ? string.Join(",", model.list_itemscollected)
        //        : "";

        //    // ===============================
        //    // Base URL
        //    // ===============================
        //    string baseUrl = "";
        //    using (var cmd = new SqlCommand(
        //        "SELECT TOP 1 ans_status FROM android_settings WHERE ans_name='BaseUrl'",
        //        usqlre.shop))
        //    {
        //        baseUrl = Convert.ToString(cmd.ExecuteScalar());
        //    }

        //     ===============================
        //     Save Images
        //     ===============================
        //    List<string> imagePaths = new();
        //    if (model.Images != null)
        //    {
        //        foreach (var img in model.Images)
        //        {
        //            var path = await SaveImage(img, "Service");
        //            if (path != null)
        //                imagePaths.Add(path);
        //        }
        //    }

        //     ===============================
        //     Build Lend DataTable
        //     ===============================
        //    DataTable dtlent = new();
        //    dtlent.Columns.Add("li_in");
        //    dtlent.Columns.Add("li_out");
        //    dtlent.Columns.Add("li_remarks");
        //    dtlent.Columns.Add("li_ir_id");
        //    dtlent.Columns.Add("li_ift_id");
        //    dtlent.Columns.Add("li_ir_mrp");

        //    foreach (var item in model.LendItems)
        //    {
        //        if (item.li_ir_id == null) continue;

        //        var dr = dtlent.NewRow();
        //        dr["li_in"] = 0;
        //        dr["li_out"] = 0;
        //        dr["li_remarks"] = item.li_remarks ?? "";
        //        dr["li_ir_id"] = item.li_ir_id;
        //        dr["li_ift_id"] = 0;
        //        dr["li_ir_mrp"] = 0;
        //        dtlent.Rows.Add(dr);
        //    }

        //    int ticketId = -1;

        //    try
        //    {
        //        usqlre.OpenConnection();

        //        using (var cmd = new SqlCommand("Sp_Sale", usqlre.shop))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            var output = new SqlParameter("@return", SqlDbType.Int)
        //            {
        //                Direction = ParameterDirection.Output
        //            };
        //            cmd.Parameters.Add(output);

        //            cmd.Parameters.AddWithValue("@si_str_id", 3);
        //            cmd.Parameters.AddWithValue("@si_entryno", 0);
        //            cmd.Parameters.AddWithValue("@si_date", DateTime.Now);
        //            cmd.Parameters.AddWithValue("@si_acc_id", 1);
        //            cmd.Parameters.AddWithValue("@si_cust_name", model.si_cust_name ?? "");
        //            cmd.Parameters.AddWithValue("@si_batteryno", model.BatteryNo ?? "");
        //            cmd.Parameters.AddWithValue("@si_items_collected", si_items_collected);
        //            cmd.Parameters.AddWithValue("@si_company", model.Brand ?? "");
        //            cmd.Parameters.AddWithValue("@si_model", model.si_model ?? "");
        //            cmd.Parameters.AddWithValue("@si_imei", model.si_imei ?? "");
        //            cmd.Parameters.AddWithValue("@si_expected_date", model.si_expected_date ?? "");
        //            cmd.Parameters.AddWithValue("@si_deliverydate", model.si_deliverydate ?? "");
        //            cmd.Parameters.AddWithValue("@si_remarks", model.EstimateCost ?? "");
        //            cmd.Parameters.AddWithValue("@si_cash_paid_acc", model.CashAccId);
        //            cmd.Parameters.AddWithValue("@si_cash_recieved", model.AdvanceAmount);
        //            cmd.Parameters.AddWithValue("@si_work_type_id", 1);
        //            cmd.Parameters.AddWithValue("@si_coupon_no", model.complaint_id);
        //            cmd.Parameters.AddWithValue("@si_finish", "Unassigned");
        //            cmd.Parameters.AddWithValue("@si_location_id", usqlre.locationId);
        //            cmd.Parameters.AddWithValue("@si_user_id", usqlre.userId);
        //            cmd.Parameters.AddWithValue("@type2", dtlent);
        //            cmd.Parameters.AddWithValue("@StatementType", model.StatementType ?? "insert");

        //            await cmd.ExecuteNonQueryAsync();
        //            ticketId = Convert.ToInt32(output.Value);
        //        }

        //         ===============================
        //         Insert Status + Images
        //         ===============================
        //        if (ticketId > 0)
        //        {
        //            using var cmd2 = new SqlCommand(
        //                "INSERT INTO inv_service_status_history " +
        //                "(ssh_ticket_id, ssh_status, ssh_technician_id, ssh_remarks, ssh_changed_by, ssh_changed_date) " +
        //                "VALUES (@id,'Unassigned',0,'Ticket created',@user,GETDATE())",
        //                usqlre.shop);

        //            cmd2.Parameters.AddWithValue("@id", ticketId);
        //            cmd2.Parameters.AddWithValue("@user", usqlre.userId);
        //            await cmd2.ExecuteNonQueryAsync();

        //            foreach (var path in imagePaths)
        //            {
        //                using var imgCmd = new SqlCommand(
        //                    "INSERT INTO inv_images (ii_reference_type, ii_reference_id, ii_image_path) " +
        //                    "VALUES ('Service', @id, @path)", usqlre.shop);

        //                imgCmd.Parameters.AddWithValue("@id", ticketId);
        //                imgCmd.Parameters.AddWithValue("@path", path);
        //                await imgCmd.ExecuteNonQueryAsync();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = false, message = ex.Message });
        //    }

        //    return Ok(new { status = true, ticket_id = ticketId });
        //}


        private async Task<string> SaveImage(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                return null;

            // Create folder: wwwroot/uploads/{folderName}
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(),
                                             "wwwroot", "uploads", folderName);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Unique file name
            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

            // Full path
            string filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            // Return relative path
            return $"/uploads/{folderName}/{fileName}";
        }

        //[HttpPost("insert-ticket")]
        //public async Task<IActionResult> InsertTicket([FromBody] ServiceTicketModel model)
        //{
        //    if (model == null) return BadRequest(new { status = 0, message = "Invalid payload" });

        //    // Build type1 DataTable (dt1)
        //    DataTable dt1 = new DataTable();
        //    dt1.Columns.Add("qty");
        //    dt1.Columns.Add("fqty");
        //    dt1.Columns.Add("s_rate");
        //    dt1.Columns.Add("real_rate");
        //    dt1.Columns.Add("gross");
        //    dt1.Columns.Add("disc_p");
        //    dt1.Columns.Add("disc");
        //    dt1.Columns.Add("real_disc");
        //    dt1.Columns.Add("net");
        //    dt1.Columns.Add("tax");
        //    dt1.Columns.Add("cgst");
        //    dt1.Columns.Add("sgst");
        //    dt1.Columns.Add("igst");
        //    dt1.Columns.Add("total");
        //    dt1.Columns.Add("pt");
        //    dt1.Columns.Add("uniquecode");
        //    dt1.Columns.Add("ir_id");
        //    dt1.Columns.Add("mrp");
        //    dt1.Columns.Add("qty_multi_unit");
        //    dt1.Columns.Add("unit_multi");
        //    dt1.Columns.Add("srate_multiunit");
        //    dt1.Columns.Add("narration");
        //    dt1.Columns.Add("cess");
        //    dt1.Columns.Add("ad_cess");
        //    dt1.Columns.Add("kfc");
        //    dt1.Columns.Add("support");
        //    dt1.Columns.Add("real_support");
        //    dt1.Columns.Add("salesman");
        //    dt1.Columns.Add("sp_prate");
        //    dt1.Columns.Add("sp_realprate");
        //    dt1.Columns.Add("sp_cost");
        //    dt1.Columns.Add("sp_local_exp");
        //    dt1.Columns.Add("sp_lend_amount");
        //    dt1.Columns.Add("sp_discp2");
        //    dt1.Columns.Add("sp_disc2");
        //    dt1.Columns.Add("sp_netratesingle");
        //    dt1.Columns.Add("sp_narration1");
        //    dt1.Columns.Add("sp_other_exp");
        //    dt1.Columns.Add("sp_print");
        //    dt1.Columns.Add("sp_card_disc");
        //    dt1.Columns.Add("sp_depreciation_per");
        //    dt1.Columns.Add("sp_depreciation");
        //    dt1.Columns.Add("sp_narration2");
        //    dt1.Columns.Add("sp_narration3");

        //    // Populate dt1 from model.Items
        //    foreach (var row in model.Items ?? Enumerable.Empty<ItemRowModel>())
        //    {
        //        // Use slno presence as indicator, if null skip
        //        if (string.IsNullOrEmpty(row?.slno)) continue;

        //        var dr = dt1.NewRow();
        //        dr[0] = row.qty?.ToString() ?? "0";
        //        dr[1] = row.fqty?.ToString() ?? "0";
        //        dr[2] = row.s_rate?.ToString() ?? "0";
        //        dr[3] = row.real_rate?.ToString() ?? "0";
        //        dr[4] = row.gross?.ToString() ?? "0";
        //        dr[5] = row.disc_p?.ToString() ?? "0";
        //        dr[6] = row.disc?.ToString() ?? "0";
        //        dr[7] = row.real_disc?.ToString() ?? "0";
        //        dr[8] = row.net?.ToString() ?? "0";
        //        dr[9] = row.tax?.ToString() ?? "0";
        //        dr[10] = row.cgst?.ToString() ?? "0";
        //        dr[11] = row.sgst?.ToString() ?? "0";
        //        dr[12] = row.igst?.ToString() ?? "0";
        //        dr[13] = row.total?.ToString() ?? "0";

        //        // pt may be encrypted — call C_common.RateSettingsReverse if needed
        //        if (!string.IsNullOrEmpty(row.pt))
        //        {
        //            try
        //            {
        //                if (/*ENABLEPRATEENCRYPTIONINSALES*/ false)
        //                {
        //                    dr[14] = C_common.RateSettingsReverse(row.pt);
        //                }
        //                else
        //                {
        //                    dr[14] = row.pt;
        //                }
        //            }
        //            catch
        //            {
        //                dr[14] = row.pt;
        //            }
        //        }
        //        else dr[14] = "0";

        //        dr[15] = row.uniquecode ?? "";
        //        dr[16] = row.ir_id?.ToString() ?? "-1";
        //        dr[17] = row.mrp?.ToString() ?? "0";
        //        dr[18] = row.qty_multi_unit?.ToString() ?? "0";
        //        dr[19] = row.unit_multi ?? "";
        //        dr[20] = row.srate_multiunit?.ToString() ?? "0";
        //        dr[21] = string.IsNullOrEmpty(row.narration) ? "0" : row.narration;
        //        dr[22] = row.cess?.ToString() ?? "0";
        //        dr[23] = row.ad_cess?.ToString() ?? "0";
        //        dr[24] = row.kfc?.ToString() ?? "0";
        //        dr[25] = "0";
        //        dr[26] = "0";

        //        // salesman -> int or -1
        //        if (!string.IsNullOrEmpty(row.salesman))
        //        {
        //            if (int.TryParse(row.salesman, out var sVal))
        //                dr[27] = sVal;
        //            else
        //                dr[27] = -1;
        //        }
        //        else dr[27] = -1;

        //        dr[28] = row.Prate?.ToString() ?? "0";
        //        dr[29] = row.RPrate?.ToString() ?? "0";
        //        dr[30] = row.Cost?.ToString() ?? "0";
        //        dr[31] = "0";
        //        dr[32] = "0";
        //        dr[33] = "0";
        //        dr[34] = "0";
        //        dr[35] = "0";
        //        dr[36] = row.sp_prate?.ToString() ?? "";
        //        dr[37] = row.sp_realprate?.ToString() ?? "";
        //        dr[38] = row.sp_cost?.ToString() ?? "0";
        //        dr[39] = row.sp_local_exp?.ToString() ?? "0";
        //        dr[40] = row.sp_lend_amount?.ToString() ?? "0";
        //        dr[41] = row.sp_discp2?.ToString() ?? "0";
        //        dr[42] = row.sp_disc2?.ToString() ?? "0";
        //        dr[43] = row.sp_netratesingle?.ToString() ?? "0";
        //        dr[44] = row.sp_narration1 ?? "";
        //        dr[45] = row.sp_other_exp?.ToString() ?? "0";
        //        dr[46] = row.sp_print ?? "";
        //        dr[47] = row.sp_card_disc?.ToString() ?? "0";
        //        dr[48] = row.sp_depreciation_per?.ToString() ?? "0";
        //        dr[49] = row.sp_depreciation?.ToString() ?? "0";
        //        dr[50] = row.sp_narration2 ?? "";
        //        dr[51] = row.sp_narration3 ?? "";

        //        dt1.Rows.Add(dr);
        //    }

        //    // Items collected string
        //    string si_items_collected = "";
        //    if (model.list_itemscollected != null && model.list_itemscollected.Count > 0)
        //    {
        //        si_items_collected = string.Join(",", model.list_itemscollected);
        //    }

        //    // Build dtlent
        //    DataTable dtlent = new DataTable();
        //    dtlent.Columns.Add("li_in");
        //    dtlent.Columns.Add("li_out");
        //    dtlent.Columns.Add("li_remarks");
        //    dtlent.Columns.Add("li_ir_id");
        //    dtlent.Columns.Add("li_ift_id");
        //    dtlent.Columns.Add("li_ir_mrp");

        //    foreach (var row in model.LendItems ?? Enumerable.Empty<LendRowModel>())
        //    {
        //        // skip if no li_ir_id
        //        if (row?.li_ir_id == null) continue;

        //        var dr1 = dtlent.NewRow();
        //        dr1[0] = row.li_in?.ToString() ?? "0";
        //        dr1[1] = row.li_out?.ToString() ?? "0";
        //        dr1[2] = row.li_remarks ?? "";
        //        dr1[3] = row.li_ir_id?.ToString() ?? "-1";
        //        dr1[4] = row.li_ift_id?.ToString() ?? "-1";
        //        dr1[5] = row.li_ir_mrp?.ToString() ?? "0";
        //        dtlent.Rows.Add(dr1);
        //    }

        //    // Call SP
        //    int tres = -6;
        //    try
        //    {
        //        UserSqlServer usqlre = new UserSqlServer(this); // if your project uses this wrapper
        //        usqlre.OpenConnection();

        //        using (var cmd = new SqlCommand("Sp_Sale", usqlre.shop))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            var output = new SqlParameter("@return", SqlDbType.Int) { Direction = ParameterDirection.Output };
        //            cmd.Parameters.Add(output);

        //            cmd.Parameters.AddWithValue("@si_str_id", model.si_str_id);
        //            cmd.Parameters.AddWithValue("@si_entryno", model.si_entryno);
        //            cmd.Parameters.AddWithValue("@si_date", model.si_date);
        //            cmd.Parameters.AddWithValue("@si_acc_id", model.si_acc_id);
        //            cmd.Parameters.AddWithValue("@si_cust_name", model.si_cust_name ?? "");
        //            cmd.Parameters.AddWithValue("@si_add1", model.si_add1 ?? "");
        //            cmd.Parameters.AddWithValue("@si_add2", model.si_add2 ?? "");

        //            if (model.si_salesman_id != null) cmd.Parameters.AddWithValue("@si_salesman_id", model.si_salesman_id);
        //            cmd.Parameters.AddWithValue("@si_location_id", model.si_location_id);
        //            cmd.Parameters.AddWithValue("@si_remarks", model.si_remarks ?? "");
        //            cmd.Parameters.AddWithValue("@si_tax_type", model.si_tax_type);

        //            cmd.Parameters.AddWithValue("@si_gross_value", model.txt_sum_gross ?? "0");
        //            cmd.Parameters.AddWithValue("@si_disc_per", model.txt_other_discount_p ?? "0");
        //            cmd.Parameters.AddWithValue("@si_disc", model.txt_sum_disc ?? "0");
        //            cmd.Parameters.AddWithValue("@si_net_amount", model.txt_net_sum ?? "0");
        //            cmd.Parameters.AddWithValue("@si_tax", model.txt_vat_sum ?? "0");
        //            cmd.Parameters.AddWithValue("@si_total", model.txt_sum_total ?? "0");
        //            cmd.Parameters.AddWithValue("@si_other_charge", model.txt_other_charges ?? "0");
        //            cmd.Parameters.AddWithValue("@si_other_disc", model.txt_other_discount ?? "0");
        //            cmd.Parameters.AddWithValue("@si_roundoff", model.txt_round_off ?? "0");

        //            // profit may require reverse
        //            var profitVal = model.txt_sum_profit ?? "0";
        //            //if (ENABLEPRATEENCRYPTIONINSALES) profitVal = C_common.RateSettingsReverse(profitVal);
        //            cmd.Parameters.AddWithValue("@si_profit", profitVal);

        //            cmd.Parameters.AddWithValue("@si_cash_recieved", model.txt_cash_received ?? model.txt_cash_received);
        //            cmd.Parameters.AddWithValue("@si_balance", model.txt_balance ?? model.txt_balance);
        //            cmd.Parameters.AddWithValue("@si_user_id", model.si_user_id);
        //            cmd.Parameters.AddWithValue("@si_grand_total", model.txt_grand_total ?? "0");
        //            cmd.Parameters.AddWithValue("@si_igst_total", model.txt_igst_sum ?? "0");
        //            cmd.Parameters.AddWithValue("@si_sgst_total", model.txt_sgst_sum ?? "0");
        //            cmd.Parameters.AddWithValue("@si_cgst_total", model.txt_cgst_sum ?? "0");

        //            cmd.Parameters.AddWithValue("@si_return_entryno", Convert.ToInt32(string.IsNullOrEmpty(model.txt_return_entryno) ? "0" : model.txt_return_entryno));
        //            cmd.Parameters.AddWithValue("@si_return_amount", string.IsNullOrEmpty(model.txt_return_amount) ? "0" : model.txt_return_amount);

        //            cmd.Parameters.AddWithValue("@si_bankacc", model.cmb_bankacc != null ? model.cmb_bankacc.ToString() : "-1");
        //            cmd.Parameters.AddWithValue("@si_card_amount", string.IsNullOrEmpty(model.txt_card_amount) ? "0" : model.txt_card_amount);

        //            cmd.Parameters.AddWithValue("@si_emi_acc", model.cmb_emi_acc != null ? model.cmb_emi_acc.ToString() : "-1");
        //            cmd.Parameters.AddWithValue("@si_emi_amount", string.IsNullOrEmpty(model.txt_emi_amount) ? "0" : model.txt_emi_amount);

        //            cmd.Parameters.AddWithValue("@si_commision_acc_id", model.cmb_commission != null ? model.cmb_commission.ToString() : "-1");
        //            cmd.Parameters.AddWithValue("@si_commision_per", string.IsNullOrEmpty(model.txt_commition_per) ? "0" : model.txt_commition_per);
        //            cmd.Parameters.AddWithValue("@si_commision_amount", string.IsNullOrEmpty(model.txt_commition_amount) ? "0" : model.txt_commition_amount);

        //            cmd.Parameters.AddWithValue("@si_buyersordernumber", model.txt_buyers_orderno ?? "");
        //            cmd.Parameters.AddWithValue("@si_dispatch_throught", model.txt_dispatch_through ?? "");
        //            cmd.Parameters.AddWithValue("@si_destination", model.txt_destination ?? "");
        //            cmd.Parameters.AddWithValue("@si_destination_add", model.txt_destination_address ?? "");
        //            cmd.Parameters.AddWithValue("@si_vehicle_no", model.txt_vehicle_no ?? "");
        //            cmd.Parameters.AddWithValue("@si_ewayno", model.txt_ewaynumber ?? "");
        //            cmd.Parameters.AddWithValue("@si_contact_person", model.txt_contact_person ?? "");
        //            cmd.Parameters.AddWithValue("@si_cash_paid_acc", model.cmb_cash_piad_acc ?? (object)DBNull.Value);
        //            cmd.Parameters.AddWithValue("@si_ob", model.txt_OB ?? "");
        //            cmd.Parameters.AddWithValue("@si_net_balance", model.txt_net_balance ?? "0");
        //            cmd.Parameters.AddWithValue("@si_interstate", model.si_interstate ? 1 : 0);
        //            cmd.Parameters.AddWithValue("@si_lc_id", Convert.ToInt32(string.IsNullOrEmpty(model.txt_accno) ? "0" : model.txt_accno));
        //            if (model.cmb_work_type != null) cmd.Parameters.AddWithValue("@si_work_type_id", model.cmb_work_type);
        //            cmd.Parameters.AddWithValue("@si_km", model.txt_km ?? "");
        //            cmd.Parameters.AddWithValue("@si_km_per_day", model.txt_avg_km_perday ?? "");
        //            cmd.Parameters.AddWithValue("@si_company", model.si_company ?? "");
        //            cmd.Parameters.AddWithValue("@si_model", model.si_model ?? "");
        //            cmd.Parameters.AddWithValue("@si_rc_id", model.si_rc_id ?? (object)DBNull.Value);
        //            cmd.Parameters.AddWithValue("@si_imei", model.si_imei ?? "");
        //            cmd.Parameters.AddWithValue("@si_deliverydate", model.si_deliverydate ?? "");
        //            cmd.Parameters.AddWithValue("@si_qtn_no", model.si_qtn_no ?? "");

        //            // finish logic
        //            var finishVal = model.si_finish;
        //            cmd.Parameters.AddWithValue("@si_finish", finishVal);
        //            cmd.Parameters.AddWithValue("@si_delivered", model.si_delivered ?? "");
        //            cmd.Parameters.AddWithValue("@si_items_collected", si_items_collected);
        //            cmd.Parameters.AddWithValue("@si_expected_date", model.si_expected_date ?? "");
        //            cmd.Parameters.AddWithValue("@si_due_date", model.si_due_date ?? "");
        //            cmd.Parameters.AddWithValue("@si_work_priority", model.si_work_priority ? 1 : 0);
        //            cmd.Parameters.AddWithValue("@si_finished_date", model.si_finished_date ?? "");
        //            cmd.Parameters.AddWithValue("@woh_salesman_id", model.woh_salesman_id ?? (object)DBNull.Value);
        //            cmd.Parameters.AddWithValue("@si_color", model.si_color ?? "");
        //            cmd.Parameters.AddWithValue("@si_update_date", DateTime.Today);
        //            cmd.Parameters.AddWithValue("@si_assign_to", (object)DBNull.Value);
        //            cmd.Parameters.AddWithValue("@si_freight_charge", model.si_freight_charge ?? "0");

        //            // Table-valued parameters
        //            cmd.Parameters.AddWithValue("@type1", dt1);
        //            cmd.Parameters.AddWithValue("@type2", dtlent);
        //            cmd.Parameters.AddWithValue("@StatementType", model.StatementType ?? "insert");

        //            cmd.Connection = usqlre.shop;
        //            cmd.CommandTimeout = 600; // increase if needed

        //            using (var reader = await cmd.ExecuteReaderAsync())
        //            {
        //                // If your SP returns resultset, you can parse it here; we only want output param
        //            }

        //            tres = Convert.ToInt32(output.Value ?? -6);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = false, message = ex.Message });
        //    }

        //    return Ok(new { status = true, return_value = tres });
        //}

        // ---------------------------------------------------
        // INSERT
        // ---------------------------------------------------
        [HttpPost("insert-qc-list")]
        public async Task<IActionResult> InsertQcList(QCModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "insert_qc_list");
                cmd.Parameters.AddWithValue("@qc_name", model.qc_name);
                cmd.Parameters.AddWithValue("@qc_category_id", model.qc_category_id);
                cmd.Parameters.AddWithValue("@created_by", model.created_by);

                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Inserted successfully",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }

        // ---------------------------------------------------
        // UPDATE
        // ---------------------------------------------------
        [HttpPost("update-qc-list")]
        public async Task<IActionResult> UpdateQcList(QCModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "update_qc_list");
                cmd.Parameters.AddWithValue("@qc_id", model.qc_id);
                cmd.Parameters.AddWithValue("@qc_name", model.qc_name);
                cmd.Parameters.AddWithValue("@qc_category_id", model.qc_category_id);
                cmd.Parameters.AddWithValue("@qc_is_active", model.qc_is_active);
                cmd.Parameters.AddWithValue("@updated_by", model.updated_by);

                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Updated successfully",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }

        // ---------------------------------------------------
        // DELETE
        // ---------------------------------------------------
        [HttpPost("delete-qc-list/{id}")]
        public async Task<IActionResult> DeleteQcList(int id)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "delete_qc_list");
                cmd.Parameters.AddWithValue("@qc_id", id);

                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Deleted successfully",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }

        // ---------------------------------------------------
        // GET ALL
        // ---------------------------------------------------
        [HttpGet("get-qc-list")]
        public async Task<IActionResult> GetAllQcList()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "get_qc_list"));

                // Prepare object
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                // Convert to JSON string
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }

        // ---------------------------------------------------
        // GET BY ID
        // ---------------------------------------------------
        [HttpGet("get-qc-list-by-id/{id}")]
        public async Task<IActionResult> GetAllQcListById(int id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "getById_qc_list"),
                    new SqlParameter("@qc_id", id));

                // Prepare object
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                // Convert to JSON string
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }

        // ---------------------------------------------------
        // INSERT
        // ---------------------------------------------------
        [HttpPost("insert-items-collected")]
        public async Task<IActionResult> InsertItemsCollected(ItemsCollectedModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "insert_items_collected");
                cmd.Parameters.AddWithValue("@iic_name", model.iic_name);
                cmd.Parameters.AddWithValue("@iic_status", model.iic_status);
                cmd.Parameters.AddWithValue("@iic_remarks", model.iic_remarks);

                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Inserted successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        // ---------------------------------------------------
        // UPDATE
        // ---------------------------------------------------
        [HttpPost("update-items-collected")]
        public async Task<IActionResult> UpdateItemsCollected(ItemsCollectedModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "update_items_collected");
                cmd.Parameters.AddWithValue("@iic_id", model.iic_id);
                cmd.Parameters.AddWithValue("@iic_name", model.iic_name);
                cmd.Parameters.AddWithValue("@iic_status", model.iic_status);
                cmd.Parameters.AddWithValue("@iic_remarks", model.iic_remarks);

                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Updated successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        // ---------------------------------------------------
        // DELETE
        // ---------------------------------------------------
        [HttpPost("delete-items-collected/{id}")]
        public async Task<IActionResult> DeleteItemsCollected(int id)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "delete_items_collected");
                cmd.Parameters.AddWithValue("@iic_id", id);

                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Deleted successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        // ---------------------------------------------------
        // GET
        // ---------------------------------------------------
        [HttpGet("get-items-collected")]
        public async Task<IActionResult> GetItemsCollected()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "get_items_collected"));

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        // ---------------------------------------------------
        // GET BY ID
        // ---------------------------------------------------
        [HttpGet("get-items-collected-by-id/{id}")]
        public async Task<IActionResult> GetItemsCollectedById(int id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "getById_items_collected"),
                    new SqlParameter("@iic_id", id));

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetch successful",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        [HttpGet("description-list")]
        public async Task<IActionResult> DescriptionList()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "descriptionList"));

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Spare List Fetched Successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        // ---------------------------------------------------
        // INSERT
        // ---------------------------------------------------
        [HttpPost("insert-description")]
        public async Task<IActionResult> InsertDescription(string descName)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "insert_description");
                cmd.Parameters.AddWithValue("@ir_name", descName);

                SqlDataReader dr = await cmd.ExecuteReaderAsync();
                DataTable dt = new DataTable();
                dt.Load(dr);


                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Inserted successfully",
                    data = dt
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(responseObj);
                return Content(json, "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }

        [HttpGet("technicians")]
        public async Task<IActionResult> GetAllTechnician()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "getAllTechnician"));

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Technician list fetched successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        [HttpPost("assign-technician")]
        public async Task<IActionResult> AssignTechnician(int si_entryno, int as_id)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "assignTechnician");
                cmd.Parameters.AddWithValue("@si_entryno", si_entryno);
                cmd.Parameters.AddWithValue("@as_id", as_id);
                cmd.Parameters.AddWithValue("@created_by", usqlre.userId);


                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Technician assigned successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpPost("update-technician-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] TicketStatusModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "updatetechnicianStatus");
                cmd.Parameters.AddWithValue("@si_entryno", model.ticket_id);
                cmd.Parameters.AddWithValue("@status", model.status);
                cmd.Parameters.AddWithValue("@ssh_remarks", model.remarks);
                cmd.Parameters.AddWithValue("@as_id", model.technician_id);
                cmd.Parameters.AddWithValue("@user_id", usqlre.userId);

                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Updated successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpPost("update-generalRemarks")]
        public async Task<IActionResult> UpdategeneralRemarks([FromBody] GeneralRemarksModel model)
        {
            UserSqlServer usqlre = null;
            try
            {

                if (model == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Invalid payload",
                        data = (object)null
                    });
                }

                if (string.IsNullOrWhiteSpace(model.generalRemarks))
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "General remarks cannot be empty.",
                        data = (object)null
                    });
                }

                if (model.generalRemarks.Length > 512)
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "General remarks cannot exceed 512 characters.",
                        data = (object)null
                    });
                }
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "updateGeneralRemarks");
                cmd.Parameters.AddWithValue("@si_entryno", model.ticket_id);
                cmd.Parameters.AddWithValue("@si_other_works", model.generalRemarks);
                cmd.Parameters.AddWithValue("@user_id", usqlre.userId);

                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Updated successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpGet("get-generalRemarks")]
        public async Task<IActionResult> GetgeneralRemarks(int ticket_id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "GetGeneralRemarks");
                cmd.Parameters.AddWithValue("@si_entryno", ticket_id);

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "get successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpPost("relocate-technician")]
        public async Task<IActionResult> RelocatedTechnician(int si_entryno, int as_id, string remarks)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "relocateTechnician");
                cmd.Parameters.AddWithValue("@si_entryno", si_entryno);
                cmd.Parameters.AddWithValue("@as_id", as_id);
                cmd.Parameters.AddWithValue("@created_by", usqlre.userId);
                cmd.Parameters.AddWithValue("@remarks", remarks);

                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Technician assigned successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpGet("get-tickets")]
        public async Task<IActionResult> GetTickets(
           [FromQuery] TicketFiltrationModel model)
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                DataTable ticketTable;
                DataTable lendTable;

                // Wait up to 30s for a slot. If the server is overloaded,
                // fail fast instead of piling more queries onto SQL Server.
                if (!await _ticketQueryThrottle.WaitAsync(TimeSpan.FromSeconds(30), HttpContext.RequestAborted))
                {
                    return StatusCode(503, new
                    {
                        status = false,
                        statusCode = 503,
                        message = "Server is busy. Please try again shortly.",
                        data = (object)null
                    });
                }

                try
                {
                if (!await usqlre.OpenConnectionAsync(HttpContext.RequestAborted))
                {
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                const string ticketSql = @"
                        SELECT
                            s.si_entryno,
                            s.si_date AS TicketDate,
                            s.si_cust_name,
                            c.scr_mobile_no,
                            s.si_company,
                            s.si_model,
                            s.si_assign_to,
                            s.si_remarks AS EstimateCost,
                            s.si_deliverydate,
                            s.si_finish,
                            t.as_name AS Technician,
                            ass.AssignedDate,
                            s.si_return_entryno,
                            s.si_color,
                            clr.clr_name
                        FROM inv_sales_inf s
                        LEFT JOIN acc_subhead t ON t.as_id = s.si_assign_to
                        LEFT JOIN inv_service_complaint_reg c ON c.scr_id = s.si_coupon_no
                        LEFT JOIN inv_color clr ON clr.clr_id = s.si_color
                        OUTER APPLY (
                            SELECT MAX(h.ssh_changed_date) AS AssignedDate
                            FROM inv_service_status_history h
                            WHERE h.ssh_ticket_id = s.si_entryno
                            AND h.ssh_status IN (N'Assigned', N'Relocate')
                        ) ass
                        WHERE s.si_str_id = 12
                        AND ISNULL(s.si_coupon_no, 0) <> 0
                        AND (
                                (ISNULL(@si_other_remarks, N'') = N'' AND s.si_finish IN (N'Unassigned', N'Relocate'))
                                OR (ISNULL(@si_other_remarks, N'') <> N'' AND s.si_finish = @si_other_remarks)
                            )
                        AND (@from_date IS NULL OR s.si_date >= @from_date)
                        AND (@to_date IS NULL OR s.si_date < DATEADD(DAY, 1, @to_date))
                        AND (@si_entryno IS NULL OR s.si_entryno = @si_entryno)
                        AND (@si_acc_id IS NULL OR s.si_acc_id = @si_acc_id)
                        AND (@si_assign_to IS NULL OR s.si_assign_to = @si_assign_to)
                        AND (@rout_id IS NULL OR c.scr_rout_id = @rout_id)
                        ORDER BY s.si_entryno DESC";

                const string lendSql = @"
                        SELECT
                            l.li_entryno,
                            l.li_in,
                            l.li_out,
                            l.li_remarks,
                            l.li_ir_id,
                            i.ir_name,
                            l.li_ir_mrp,
                            l.li_ift_id
                        FROM inv_sales_inf s
                        INNER JOIN inv_lend_item_transactions l
                            ON l.li_entryno = s.si_entryno
                        AND l.li_form = N'WORKORDER QUOTATION'
                        LEFT JOIN inv_item_reg i ON i.ir_id = l.li_ir_id
                        LEFT JOIN inv_service_complaint_reg c ON c.scr_id = s.si_coupon_no
                        WHERE s.si_str_id = 12
                        AND ISNULL(s.si_coupon_no, 0) <> 0
                        AND (
                                (ISNULL(@si_other_remarks, N'') = N'' AND s.si_finish IN (N'Unassigned', N'Relocate'))
                                OR (ISNULL(@si_other_remarks, N'') <> N'' AND s.si_finish = @si_other_remarks)
                            )
                        AND (@from_date IS NULL OR s.si_date >= @from_date)
                        AND (@to_date IS NULL OR s.si_date < DATEADD(DAY, 1, @to_date))
                        AND (@si_entryno IS NULL OR s.si_entryno = @si_entryno)
                        AND (@si_acc_id IS NULL OR s.si_acc_id = @si_acc_id)
                        AND (@si_assign_to IS NULL OR s.si_assign_to = @si_assign_to)
                        AND (@rout_id IS NULL OR c.scr_rout_id = @rout_id)";

                ticketTable = new DataTable();
                lendTable = new DataTable();

                using (SqlCommand cmd = new SqlCommand(ticketSql, usqlre.shop))
                {
                    cmd.CommandTimeout = 60;
                    AddTicketFilters(cmd, model);
                    ticketTable = await LoadTableAsync(cmd, cancellationToken);
                }

                using (SqlCommand cmd = new SqlCommand(lendSql, usqlre.shop))
                {
                    cmd.CommandTimeout = 60;
                    AddTicketFilters(cmd, model);
                    lendTable = await LoadTableAsync(cmd, cancellationToken);
                }
                }
                finally
                {
                    usqlre.ReleaseConnection();
                    _ticketQueryThrottle.Release();
                }


                // =====================================================
                // LEND ITEMS GROUP BY TICKET
                // =====================================================

                Dictionary<int, List<object>> Complaints =
                    new Dictionary<int, List<object>>();

                foreach (DataRow row in lendTable.Rows)
                {
                    int ticketNo = Convert.ToInt32(row["li_entryno"]);

                    if (!Complaints.ContainsKey(ticketNo))
                    {
                        Complaints[ticketNo] = new List<object>();
                    }

                    Complaints[ticketNo].Add(new
                    {
                        li_in = row["li_in"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(row["li_in"]),

                        li_out = row["li_out"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(row["li_out"]),

                        li_remarks = row["li_remarks"] == DBNull.Value
                            ? null
                            : row["li_remarks"].ToString(),

                        li_ir_id = row["li_ir_id"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(row["li_ir_id"]),

                        ir_name = row["ir_name"] == DBNull.Value
                            ? null
                            : row["ir_name"].ToString(),

                        li_ir_mrp = row["li_ir_mrp"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(row["li_ir_mrp"]),

                        li_ift_id = row["li_ift_id"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(row["li_ift_id"])
                    });
                }


                // =====================================================
                // FINAL TICKET LIST
                // =====================================================

                List<object> tickets = new List<object>();

                foreach (DataRow row in ticketTable.Rows)
                {
                    int ticketNo = Convert.ToInt32(row["si_entryno"]);

                    tickets.Add(new
                    {
                        si_entryno = ticketNo,

                        TicketDate = row["TicketDate"] == DBNull.Value
                            ? null
                            : row["TicketDate"],

                        si_cust_name = row["si_cust_name"] == DBNull.Value
                            ? null
                            : row["si_cust_name"].ToString(),

                        scr_mobile_no = row["scr_mobile_no"] == DBNull.Value
                            ? null
                            : row["scr_mobile_no"].ToString(),

                        si_company = row["si_company"] == DBNull.Value
                            ? null
                            : row["si_company"].ToString(),

                        si_model = row["si_model"] == DBNull.Value
                            ? null
                            : row["si_model"].ToString(),

                        si_assign_to = row["si_assign_to"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(row["si_assign_to"]),

                        EstimateCost = row["EstimateCost"] == DBNull.Value
                            ? null
                            : row["EstimateCost"].ToString(),

                        si_deliverydate = row["si_deliverydate"] == DBNull.Value
                            ? null
                            : row["si_deliverydate"],

                        si_finish = row["si_finish"] == DBNull.Value
                            ? null
                            : row["si_finish"].ToString(),

                        Technician = row["Technician"] == DBNull.Value
                            ? null
                            : row["Technician"].ToString(),

                        AssignedDate = row["AssignedDate"] == DBNull.Value
                            ? null
                            : row["AssignedDate"],

                        si_return_entryno = row["si_return_entryno"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(row["si_return_entryno"]),

                        si_color = row["si_color"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(row["si_color"]),

                        clr_name = row["clr_name"] == DBNull.Value
                            ? null
                            : row["clr_name"].ToString(),

                        Complaints = Complaints.ContainsKey(ticketNo)
                            ? Complaints[ticketNo]
                            : new List<object>()
                    });
                }


                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Ticket list fetched successfully",
                    data = tickets
                };


                return Content(
                    Newtonsoft.Json.JsonConvert.SerializeObject(responseObj),
                    "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }

        /// <summary>
        /// Runs a command on a connection this request owns.
        /// The shared UserSqlServer connection can already be closed when a command starts.
        /// </summary>
        private async Task<DataTable> ReadServiceTableAsync(
            string commandText,
            CommandType commandType,
            params SqlParameter[] parameters)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = new UserSqlServer(this, validateUser: false);
            string connectionString = usqlre.getConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Database connection string is missing.");

            using SqlConnection conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            using SqlCommand cmd = new SqlCommand(commandText, conn)
            {
                CommandType = commandType,
                CommandTimeout = 30
            };
            if (parameters != null)
            {
                foreach (SqlParameter parameter in parameters)
                    cmd.Parameters.Add(parameter);
            }

            return await LoadTableAsync(cmd, cancellationToken);
        }

        /// <summary>
        /// Executes the command and loads the first result set.
        /// Cancels the SQL command if the request token fires, including during the synchronous load.
        /// </summary>
        private static async Task<DataTable> LoadTableAsync(SqlCommand cmd, CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(() =>
            {
                try { cmd.Cancel(); } catch { }
            }))
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken))
            {
                DataTable table = new DataTable();
                table.Load(reader);
                return table;
            }
        }

        /// <summary>
        /// Executes the command and loads every result set.
        /// DataTable.Load advances the reader, so the loop stops when the reader is closed.
        /// </summary>
        private static async Task<DataSet> LoadDataSetAsync(SqlCommand cmd, CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(() =>
            {
                try { cmd.Cancel(); } catch { }
            }))
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken))
            {
                DataSet dataSet = new DataSet();
                while (!reader.IsClosed)
                {
                    DataTable table = new DataTable();
                    table.Load(reader);
                    dataSet.Tables.Add(table);
                }
                return dataSet;
            }
        }

        private static void AddTicketFilters(SqlCommand cmd, TicketFiltrationModel model)
        {
            cmd.Parameters.Add("@si_other_remarks", SqlDbType.NVarChar, -1).Value =
                string.IsNullOrWhiteSpace(model.status) ? DBNull.Value : model.status;
            cmd.Parameters.Add("@from_date", SqlDbType.Date).Value =
                model.fromDate.HasValue ? model.fromDate.Value.Date : DBNull.Value;
            cmd.Parameters.Add("@to_date", SqlDbType.Date).Value =
                model.toDate.HasValue ? model.toDate.Value.Date : DBNull.Value;
            cmd.Parameters.Add("@si_entryno", SqlDbType.Int).Value =
                model.ticketNo.HasValue ? model.ticketNo.Value : DBNull.Value;
            cmd.Parameters.Add("@si_acc_id", SqlDbType.Int).Value =
                model.customerId.HasValue ? model.customerId.Value : DBNull.Value;
            cmd.Parameters.Add("@si_assign_to", SqlDbType.Int).Value =
                model.TechnicianId.HasValue ? model.TechnicianId.Value : DBNull.Value;
            cmd.Parameters.Add("@rout_id", SqlDbType.Int).Value =
                model.routeId.HasValue ? model.routeId.Value : DBNull.Value;
        }

        [HttpGet("get-tickets-by-id")]
        public async Task<IActionResult> GetTicketsById(int Id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "getTicketsById");
                cmd.Parameters.AddWithValue("@si_entryno", Id); // ✅ FIXED

                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);
                object ticket = null;

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    ticket = ds.Tables[0].Columns
                        .Cast<DataColumn>()
                        .ToDictionary(
                            col => col.ColumnName,
                            col => row[col] == DBNull.Value ? null : row[col]
                        );
                }
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Ticket details fetched successfully",
                    data = new
                    {
                        ticket = ticket,
                        complaint = ds.Tables.Count > 1 ? ds.Tables[1] : null,
                        technicians = ds.Tables.Count > 2 ? ds.Tables[2] : null
                    }
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }

        [HttpGet("technician-list")]
        public async Task<IActionResult> TechnicianList()
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@StatementType", "technician_list"));

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Technician list fetched successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpGet("technician-dashboard")]
        public async Task<IActionResult> GetTechnicianDashboard(int id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "getTechnicianDashboardDetailed");
                cmd.Parameters.AddWithValue("@si_entryno", id); // ✅ FIXED

                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);
                object ticket = null;

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    ticket = ds.Tables[0].Columns
                        .Cast<DataColumn>()
                        .ToDictionary(
                            col => col.ColumnName,
                            col => row[col] == DBNull.Value ? null : row[col]
                        );
                }
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Technician Dashboard details fetched successfully",
                    data = new
                    {
                        ticket = ticket,
                        complaint = ds.Tables.Count > 1 ? ds.Tables[1] : null,
                        SpareList = ds.Tables.Count > 1 ? ds.Tables[2] : null
                    }
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpPost("insert-spare")]
        public async Task<IActionResult> InsertSpare([FromBody] SpareRequestModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@sr_ticket_no", model.sr_ticket_no);
                cmd.Parameters.AddWithValue("@sr_item_id", model.sr_item_id);
                cmd.Parameters.AddWithValue("@sr_qty", model.sr_qty);
                cmd.Parameters.AddWithValue("@sr_remarks", model.sr_remarks);
                cmd.Parameters.AddWithValue("@sr_srate", 0);
                cmd.Parameters.AddWithValue("@is_technician_fault", model.is_technician_fault);
                cmd.Parameters.AddWithValue("@fault_remark", model.fault_remark ?? "");
                //cmd.Parameters.AddWithValue("@sr_gross", model.sr_gross);
                //cmd.Parameters.AddWithValue("@sr_net", model.sr_net);
                //cmd.Parameters.AddWithValue("@sr_gst", model.sr_gst);
                //cmd.Parameters.AddWithValue("@sr_total", model.sr_total);
                //cmd.Parameters.AddWithValue("@sr_discount", model.sr_discount);
                //cmd.Parameters.AddWithValue("@sr_uniquecode", model.sr_uniquecode);
                cmd.Parameters.AddWithValue("@created_by", usqlre.userId);
                cmd.Parameters.AddWithValue("@StatementType", "insert_spare_request");

                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Inserted successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpPost("close-spare")]
        public async Task<IActionResult> CloseSpare(int sr_id)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@sr_id", sr_id);
                cmd.Parameters.AddWithValue("@StatementType", "close_spare_request");

                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Closed successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpPost("approve-spare")]
        public async Task<IActionResult> ApproveSpare([FromBody] SpareApproveModel model, int id)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "approve_spare");
                cmd.Parameters.AddWithValue("@si_entryno", id);
                cmd.Parameters.AddWithValue("@sr_ticket_no", model.sr_ticket_no);
                cmd.Parameters.AddWithValue("@sr_item_id", model.sr_item_id);
                cmd.Parameters.AddWithValue("@sr_qty", model.sr_qty);
                cmd.Parameters.AddWithValue("@sr_remarks", model.sr_remarks);
                cmd.Parameters.AddWithValue("@sr_srate", model.sr_srate);
                cmd.Parameters.AddWithValue("@sr_gross", model.sr_gross);
                cmd.Parameters.AddWithValue("@sr_net", model.sr_net);
                cmd.Parameters.AddWithValue("@sr_gst", model.sr_gst);
                cmd.Parameters.AddWithValue("@sr_total", model.sr_total);
                cmd.Parameters.AddWithValue("@sr_discount", model.sr_discount);
                cmd.Parameters.AddWithValue("@sr_uniquecode", model.sr_uniquecode);
                cmd.Parameters.AddWithValue("@created_by", usqlre.userId);
                cmd.Parameters.AddWithValue("@is_technician_fault", model.is_technician_fault);
                cmd.Parameters.AddWithValue("@fault_remark", model.fault_remark ?? "");

                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Approved successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpPost("reject-spare")]
        public async Task<IActionResult> RejectSpare(int id)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "reject_spare");
                cmd.Parameters.AddWithValue("@si_entryno", id);
                cmd.Parameters.AddWithValue("@created_by", usqlre.userId);

                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Rejected successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpPost("reject-spare-by-technician")]
        public async Task<IActionResult> RejectSpareByTechnician(int id, string reason)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "reject_spare_by_technician");
                cmd.Parameters.AddWithValue("@si_entryno", id);
                cmd.Parameters.AddWithValue("@sr_narration", reason);
                cmd.Parameters.AddWithValue("@created_by", usqlre.userId);

                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Rejected successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpGet("spare-list")]
        public async Task<IActionResult> SpareList(string? searchKey)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ir_name", (object)searchKey ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StatementType", "spareList");

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Spare List Fetched Successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }

        [HttpGet("spare-request-list")]
        public async Task<IActionResult> SpareRequestList()
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "spare-request-list");

                //DataTable dt = new DataTable();
                //dt.Load(cmd.ExecuteReader());
                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);

                var spareList = new List<object>();

                DataTable dtSpare = ds.Tables[0];
                DataTable dtComplaint = ds.Tables.Count > 1 ? ds.Tables[1] : null;

                foreach (DataRow row in dtSpare.Rows)
                {
                    string billNo = row["si_entryno"].ToString().Trim();

                    var complaints = new List<object>();

                    if (dtComplaint != null)
                    {
                        var complaintRows = dtComplaint.AsEnumerable()
                            .Where(x => x["BillNo"].ToString().Trim() == billNo);

                        foreach (var c in complaintRows)
                        {
                            complaints.Add(new
                            {
                                complaintName = c["ComplaintName"]?.ToString(),
                                complaintRemarks = c["ComplaintRemarks"]?.ToString()
                            });
                        }
                    }

                    spareList.Add(new
                    {
                        si_entryno = row["si_entryno"],
                        si_cust_name = row["si_cust_name"],
                        scr_mobile_no = row["scr_mobile_no"],
                        si_company = row["si_company"],
                        si_model = row["si_model"],
                        si_finish = row["si_finish"],
                        spareStatus = row["spareStatus"],
                        si_assign_to = row["si_assign_to"],
                        Technician = row["Technician"],
                        EstimateCost = row["EstimateCost"],
                        si_deliverydate = row["si_deliverydate"],
                        complaint = complaints
                    });
                }

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Spare List Fetched Successfully",
                    data = spareList
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }

        [HttpGet("spare-reject-list")]
        public async Task<IActionResult> SpareRejectList()
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "spare-reject-list");

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Spare List Fetched Successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("spare-detailed")]
        public async Task<IActionResult> SpareDetailed(int ir_id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                SqlCommand cmd = new SqlCommand("Sp_Stock", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "RS_select");
                cmd.Parameters.AddWithValue("@ir_id", ir_id);

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Spare List Fetched Successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("spare-parts-history")]
        public async Task<IActionResult> SparePartsHistory(int? ir_id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "sparePartsHistory");
                cmd.Parameters.AddWithValue("@ir_id", ir_id);

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Spare List Fetched Successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("get-spare-detailed")]
        public async Task<IActionResult> GetSpareDetailed(int Id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "spare-detailed");
                cmd.Parameters.AddWithValue("@si_entryno", Id); // ✅ FIXED

                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);
                object ticket = null;

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    ticket = ds.Tables[0].Columns
                        .Cast<DataColumn>()
                        .ToDictionary(
                            col => col.ColumnName,
                            col => row[col] == DBNull.Value ? null : row[col]
                        );
                }
                var complaints = new List<object>();

                if (ds.Tables.Count > 2)
                {
                    foreach (DataRow row in ds.Tables[2].Rows)
                    {
                        complaints.Add(new
                        {
                            complaintName = row["ComplaintName"]?.ToString(),
                            complaintRemarks = row["ComplaintRemarks"]?.ToString()
                        });
                    }
                }

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Ticket details fetched successfully",
                    data = new
                    {
                        ticket = ticket,
                        spare = ds.Tables.Count > 1 ? ds.Tables[1] : null,
                        complaint = complaints
                    }
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("technician-dashboard-detailed")]
        public async Task<IActionResult> GetTechnicianDashboardDetailed(int as_id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@as_id", as_id);
                cmd.Parameters.AddWithValue("@StatementType", "getTechnicianDashboardDetailed");

                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Data loaded successfully",
                    data = new
                    {
                        AssignedJobs = ds.Tables.Count > 0 ? ds.Tables[0] : null,
                        LendItems = ds.Tables.Count > 1 ? ds.Tables[1] : null,
                        SpareRequests = ds.Tables.Count > 2 ? ds.Tables[2] : null,
                    }
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                var responseObj = new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message,
                    data = (object)null
                };

                return StatusCode(500, responseObj);
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("get-all-customers")]
        public async Task<IActionResult> GetAllCustomers(string search = "")
        {
            UserSqlServer usqlre = null;
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                Console.WriteLine("GetAllCustomers");

                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError
                    });
                }

                using (SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@StatementType", "getAllCustomers");
                    cmd.Parameters.AddWithValue("@search", search ?? "");

                    // When the client disconnects, cancel the running SQL command on the server.
                    // This also covers the synchronous DataTable.Load below, which the token
                    // passed to ExecuteReaderAsync alone would not interrupt.
                    using (cancellationToken.Register(() =>
                    {
                        try { cmd.Cancel(); } catch { /* command already finished/disposed */ }
                    }))
                    {
                        DataTable dt = new DataTable();
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken))
                        {
                            dt.Load(reader);
                        }

                        var responseObj = new
                        {
                            status = true,
                            statusCode = 200,
                            message = "Customer List Fetched Successfully",
                            data = dt
                        };
                        return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("OperationCanceledException client aborted");
                // Client aborted the request; 499 = Client Closed Request.
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("OperationCanceledException requested");
                // SQL Server reports "Operation cancelled by user" as a SqlException after cmd.Cancel().
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                // Always return the connection to the pool.
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("search-customer-by-mobile")]
        public async Task<IActionResult> SearchCustomerByMobile(string mobile = "")
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "SearchCustomerByMobile");
                cmd.Parameters.AddWithValue("@search", mobile ?? "");

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Customer List Fetched Successfully",
                    data = dt
                };
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        // ---------------------------------------------------
        // INSERT
        // ---------------------------------------------------
        [HttpPost("insert-delivery")]
        public async Task<IActionResult> InsertDelivery([FromBody] ServiceDeliveryModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            int str_id = model.isTax ? 1 : 14;

            if (model == null) return BadRequest(new { status = 0, message = "Invalid payload" });
            // Items collected string
            string si_items_collected = "";
            if (model.list_itemscollected != null && model.list_itemscollected.Count > 0)
            {
                si_items_collected = string.Join(",", model.list_itemscollected);
            }

            DataTable dt1 = new DataTable();
            dt1.Columns.Add("qty");//0
            dt1.Columns.Add("fqty");//1
            dt1.Columns.Add("s_rate");//2
            dt1.Columns.Add("real_rate");//3
            dt1.Columns.Add("gross");//4
            dt1.Columns.Add("disc_p");//5
            dt1.Columns.Add("disc");//6
            dt1.Columns.Add("real_disc");//7
            dt1.Columns.Add("net");//8
            dt1.Columns.Add("tax");//9
            dt1.Columns.Add("cgst");//10
            dt1.Columns.Add("sgst");//11
            dt1.Columns.Add("igst");//12
            dt1.Columns.Add("total");//13
            dt1.Columns.Add("pt");//14
            dt1.Columns.Add("uniquecode");//15
            dt1.Columns.Add("ir_id");//16
            dt1.Columns.Add("mrp");//17
            dt1.Columns.Add("qty_multi_unit");//18
            dt1.Columns.Add("unit_multi");//19
            dt1.Columns.Add("srate_multiunit");//20
            dt1.Columns.Add("narration");//21
            dt1.Columns.Add("cess");//22
            dt1.Columns.Add("ad_cess");//23
            dt1.Columns.Add("kfc");//24
            dt1.Columns.Add("support");//25
            dt1.Columns.Add("real_support");//26
            dt1.Columns.Add("salesman");//27
            dt1.Columns.Add("sp_prate");//28
            dt1.Columns.Add("sp_realprate");//29
            dt1.Columns.Add("sp_cost");//30
            dt1.Columns.Add("sp_local_exp");//31
            dt1.Columns.Add("sp_lend_amount");//32
            dt1.Columns.Add("sp_discp2");//33
            dt1.Columns.Add("sp_disc2");//34
            dt1.Columns.Add("sp_netratesingle");//35
            dt1.Columns.Add("sp_narration1");//36
            dt1.Columns.Add("sp_other_exp");//37
            dt1.Columns.Add("sp_print");//38
            dt1.Columns.Add("sp_card_disc");//39
            dt1.Columns.Add("sp_depreciation_per");//40
            dt1.Columns.Add("sp_depreciation");//41
            dt1.Columns.Add("sp_narration2");//42
            dt1.Columns.Add("sp_narration3");//43
            bool hasSpRowId = Convert.ToInt32(usqlre.dbScalar(@"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME='inv_sales_par'
            AND COLUMN_NAME='sp_row_id'")) > 0;

            if (hasSpRowId)
            {
                dt1.Columns.Add("sp_row_id");
            }
            int slNo = 1;
            // Populate dt1 from model.Items
            foreach (var row in model.Items ?? Enumerable.Empty<ItemRowModel>())
            {
                // Use slno presence as indicator, if null skip
                if (row == null || row.ir_id == null)
                    continue;
                // 🔹 STEP 1: Check service item
                string chkServiceSql = "select ir_service_item, ir_taxper from inv_item_reg where ir_id=" + row.ir_id;
                DataTable dtService = usqlre.dbReaderFill(chkServiceSql);
                int isService = 0;
                if (dtService.Rows.Count > 0)
                    isService = Convert.ToInt32(dtService.Rows[0]["ir_service_item"]);
                decimal sp_rate = 0, sp_realrate = 0, sp_gross = 0,
                sp_net = 0, sp_tax = 0, sp_total = 0,
                sp_igst = 0, sp_cgst = 0, sp_sgst = 0, sp_disc = 0,
                sp_mrp = 0, sp_srate_multiunit = 0,
                sp_prate = 0, sp_realprate = 0, sp_cost = 0, sp_taxper = 0;
                long sp_uniquecode = 0;
                if (isService == 1)
                {
                    sp_taxper = dtService.Rows[0]["ir_taxper"] != DBNull.Value
                ? Convert.ToDecimal(dtService.Rows[0]["ir_taxper"])
                : 0;

                    decimal total = row.total ?? 0;

                    if (sp_taxper > 0 && model.isTax)
                    {
                        // 🔹 TAX INCLUDED CALCULATION
                        decimal baseAmount = Math.Round(total / (1 + (sp_taxper / 100)), 2);
                        decimal taxAmount = Math.Round(total - baseAmount, 2);

                        sp_rate = total;
                        sp_realrate = baseAmount;
                        sp_gross = total;
                        sp_net = baseAmount;
                        sp_tax = taxAmount;
                        sp_total = total;

                        // 🔹 Split GST (assuming intra-state)
                        sp_cgst = Math.Round(taxAmount / 2, 3);
                        sp_sgst = Math.Round(taxAmount / 2, 3);

                        sp_igst = 0;
                    }
                    else
                    {
                        // 🔹 NO TAX OR TAX OFF
                        sp_rate = total;
                        sp_realrate = total;
                        sp_gross = total;
                        sp_net = total;
                        sp_tax = 0;
                        sp_total = total;

                        sp_cgst = 0;
                        sp_sgst = 0;
                        sp_igst = 0;
                    }

                    sp_disc = row.discount ?? 0;
                }
                else
                {
                    string sql = @"select top 1 sp_uniquecode,sp_rate,sp_realrate, sp_gross_value,sp_disc,sp_net_amount,sp_tax,sp_total,sp_igst,sp_cgst,sp_sgst,sp_mrp,sp_srate_multiunit,sp_prate,sp_realprate,sp_cost,sp_taxper from inv_sales_par where sp_str_id=12 and sp_entryno=" + model.ticket_id + " and sp_ir_id=" + row.ir_id + "";

                    DataTable dt = usqlre.dbReaderFill(sql);



                    if (dt != null && dt.Rows.Count > 0)
                    {
                        DataRow salesRow = dt.Rows[0];

                        sp_uniquecode = Convert.ToInt64(salesRow["sp_uniquecode"]);
                        sp_rate = Convert.ToDecimal(salesRow["sp_rate"]);
                        sp_realrate = Convert.ToDecimal(salesRow["sp_realrate"]);
                        sp_gross = Convert.ToDecimal(salesRow["sp_gross_value"]);
                        sp_disc = Convert.ToDecimal(salesRow["sp_disc"]);
                        sp_net = Convert.ToDecimal(salesRow["sp_net_amount"]);
                        sp_tax = Convert.ToDecimal(salesRow["sp_tax"]);
                        sp_total = Convert.ToDecimal(salesRow["sp_total"]);
                        sp_igst = Convert.ToDecimal(salesRow["sp_igst"]);
                        sp_cgst = Convert.ToDecimal(salesRow["sp_cgst"]);
                        sp_sgst = Convert.ToDecimal(salesRow["sp_sgst"]);
                        sp_mrp = Convert.ToDecimal(salesRow["sp_mrp"]);
                        sp_srate_multiunit = Convert.ToDecimal(salesRow["sp_srate_multiunit"]);
                        sp_prate = Convert.ToDecimal(salesRow["sp_prate"]);
                        sp_realprate = Convert.ToDecimal(salesRow["sp_realprate"]);
                        sp_cost = Convert.ToDecimal(salesRow["sp_cost"]);
                        sp_taxper = Convert.ToDecimal(salesRow["sp_taxper"]);
                        if (!model.isTax)
                        {
                            sp_tax = 0;
                            sp_cgst = 0;
                            sp_sgst = 0;
                            sp_igst = 0;
                            sp_srate_multiunit = 0;

                            // Net should be equal to total (no tax split)
                            sp_rate = sp_rate;
                            sp_realrate = sp_rate;
                            sp_net = sp_total;
                            sp_gross = sp_total;


                        }
                    }
                }


                DataRow dRow = dt1.NewRow();

                dRow[0] = row.qty;                 // qty
                dRow[1] = 0;                       // fqty
                dRow[2] = sp_rate;                 // s_rate
                dRow[3] = sp_realrate;             // real_rate
                dRow[4] = sp_gross;                // gross
                dRow[5] = 0;                       // disc_p
                dRow[6] = sp_disc;                 // disc
                dRow[7] = 0;                       // real_disc
                dRow[8] = sp_net;                  // net
                dRow[9] = sp_tax;                  // tax
                dRow[10] = sp_cgst;                 // cgst
                dRow[11] = sp_sgst;                 // sgst
                dRow[12] = sp_igst;                 // igst
                dRow[13] = sp_total;                // total
                dRow[14] = sp_taxper;               // pt
                dRow[15] = sp_uniquecode;           // uniquecode
                dRow[16] = row.ir_id;               // ir_id
                dRow[17] = sp_mrp;                  // mrp
                dRow[18] = 0;                       // qty_multi_unit
                dRow[19] = 0;                       // unit_multi
                dRow[20] = sp_srate_multiunit;      // srate_multiunit
                dRow[21] = "";                      // narration
                dRow[22] = 0;                       // cess
                dRow[23] = 0;                       // ad_cess
                dRow[24] = 0;                       // kfc
                dRow[25] = 0;                       // support
                dRow[26] = 0;                       // real_support
                dRow[27] = "-1";                    // salesman
                dRow[28] = sp_prate;                // sp_prate
                dRow[29] = sp_realprate;            // sp_realprate
                dRow[30] = sp_cost;                 // sp_cost
                dRow[31] = "";                      // sp_local_exp
                dRow[32] = 0;                       // sp_lend_amount
                dRow[33] = "";                      // sp_discp2
                dRow[34] = 0;                       // sp_disc2
                dRow[35] = 0;                       // sp_netratesingle
                dRow[36] = "";                      // sp_narration1
                dRow[37] = 0;                       // sp_other_exp
                dRow[38] = 0;                       // sp_print
                dRow[39] = 0;                       // sp_card_disc
                dRow[40] = 0;                       // sp_depreciation_per
                dRow[41] = 0;                       // sp_depreciation
                dRow[42] = "";                      // sp_narration2
                dRow[43] = "";                      // sp_narration3
                if (hasSpRowId)
                {
                    dRow["sp_row_id"] = slNo;
                    slNo++;
                }
                dt1.Rows.Add(dRow);
            }


            // Build dtlent
            DataTable dtlent = new DataTable();
            dtlent.Columns.Add("li_in");
            dtlent.Columns.Add("li_out");
            dtlent.Columns.Add("li_remarks");
            dtlent.Columns.Add("li_ir_id");
            dtlent.Columns.Add("li_ift_id");
            dtlent.Columns.Add("li_ir_mrp");

            foreach (var row in model.LendItems ?? Enumerable.Empty<LendRowModel>())
            {
                // skip if no li_ir_id
                if (row?.li_ir_id == null) continue;

                var dr1 = dtlent.NewRow();
                dr1[0] = "0";
                dr1[1] = "0";
                dr1[2] = row.li_remarks ?? "";
                dr1[3] = row.li_ir_id?.ToString() ?? "-1";
                dr1[4] = "0";
                dr1[5] = "0";
                dtlent.Rows.Add(dr1);
            }

            // Call SP
            int tres = -6;
            try
            {
                usqlre.OpenConnection();

                using (var cmd = new SqlCommand("Sp_Sale", usqlre.shop))
                {
                    DataSet dataset = new DataSet();

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_Sale";
                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.Output;
                    SqlParameter errNumber = new SqlParameter("@errNumber", 0);
                    errNumber.Direction = ParameterDirection.Output;
                    SqlParameter errLine = new SqlParameter("@errLine", 0);
                    errNumber.Direction = ParameterDirection.Output;
                    SqlParameter errMessage = new SqlParameter("@errMessage", SqlDbType.VarChar, -1);
                    errMessage.Direction = ParameterDirection.Output;

                    cmd.Parameters.Add(parm);
                    cmd.Parameters.Add(errNumber);
                    cmd.Parameters.Add(errLine);
                    cmd.Parameters.Add(errMessage);

                    string sql = @"SELECT ISNULL(si_acc_id,0) AS si_acc_id,isnull(si_add1,'') as si_add1,isnull(si_add2,'') as si_add2, ISNULL(si_profit,0) AS si_profit, ISNULL(si_tax,0) AS si_tax, ISNULL(si_cgst_total,0) AS si_cgst_total, ISNULL(si_sgst_total,0) AS si_sgst_total,ISNULL(si_gross_value,0) AS si_gross_value, si_category_id FROM inv_sales_inf  WHERE si_entryno = " + model.ticket_id + @"  AND si_str_id = 12";

                    DataTable dt = usqlre.dbReaderFill(sql);

                    // Default values
                    decimal si_profit = 0;
                    decimal si_tax = 0;
                    decimal si_cgst_total = 0;
                    decimal si_sgst_total = 0;
                    decimal si_gross_value = 0;
                    string si_add1 = "";
                    string si_add2 = "";
                    int si_category_id = 0;
                    int si_acc_id = 0;
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];

                        si_acc_id = Convert.ToInt32(row["si_acc_id"]);
                        si_profit = Convert.ToDecimal(row["si_profit"]);
                        si_tax = Convert.ToDecimal(row["si_tax"]);
                        si_cgst_total = Convert.ToDecimal(row["si_cgst_total"]);
                        si_sgst_total = Convert.ToDecimal(row["si_sgst_total"]);
                        si_gross_value = Convert.ToDecimal(row["si_gross_value"]);
                        si_add1 = row["si_add1"].ToString();
                        si_add2 = row["si_add2"].ToString();
                        si_category_id = Convert.ToInt32(row["si_category_id"]);
                    }

                    // Pass values to stored procedure
                    cmd.Parameters.AddWithValue("@si_profit", si_profit);
                    cmd.Parameters.AddWithValue("@si_tax", si_tax);
                    cmd.Parameters.AddWithValue("@si_cgst_total", si_cgst_total);
                    cmd.Parameters.AddWithValue("@si_sgst_total", si_sgst_total);
                    cmd.Parameters.AddWithValue("@si_gross_value", si_gross_value);
                    cmd.Parameters.AddWithValue("@si_category_id", si_category_id);

                    decimal ob = 0;

                    string sqldata = @"SELECT ISNULL(SUM(at_Dr) - SUM(at_Cr), 0) AS ob FROM acc_account_transactions WHERE CAST(at_date AS DATE) <= CAST('" + DateTime.Now + "' AS DATE) AND at_as_id = " + si_acc_id + "";
                    DataTable dtdata = usqlre.dbReaderFill(sqldata);
                    if (dtdata != null && dtdata.Rows.Count > 0)
                    {
                        ob = Convert.ToDecimal(dtdata.Rows[0]["ob"]);
                    }

                    decimal netBalance = ob + model.GrandTotal;

                    cmd.Parameters.AddWithValue("@si_ob", ob);
                    cmd.Parameters.AddWithValue("@si_net_balance", netBalance);
                    cmd.Parameters.AddWithValue("@si_str_id", str_id);
                    cmd.Parameters.AddWithValue("@si_entryno", 0);
                    cmd.Parameters.AddWithValue("@si_date",model.date);
                    cmd.Parameters.AddWithValue("@si_acc_id", si_acc_id);
                    cmd.Parameters.AddWithValue("@si_cust_name", model.si_cust_name ?? "");
                    cmd.Parameters.AddWithValue("@si_assign_to", model.si_assign_to);
                    cmd.Parameters.AddWithValue("@si_batteryno", model.BatteryNo ?? "");
                    cmd.Parameters.AddWithValue("@si_add1", si_add1 ?? "");
                    cmd.Parameters.AddWithValue("@si_add2", si_add2 ?? "");
                    cmd.Parameters.AddWithValue("@si_items_collected", si_items_collected);
                    cmd.Parameters.AddWithValue("@si_company", model.Brand ?? "");
                    cmd.Parameters.AddWithValue("@si_model", model.si_model ?? "");
                    cmd.Parameters.AddWithValue("@si_imei", model.si_imei ?? "");
                    cmd.Parameters.AddWithValue("@si_expected_date", model.si_expected_date ?? "");
                    cmd.Parameters.AddWithValue("@si_deliverydate", model.si_deliverydate ?? "");
                    cmd.Parameters.AddWithValue("@si_remarks", model.EstimateCost ?? "");

                    cmd.Parameters.AddWithValue("@si_grand_total", model.GrandTotal);
                    cmd.Parameters.AddWithValue("@si_total", model.Total);
                    cmd.Parameters.AddWithValue("@si_balance", model.Balance);
                    cmd.Parameters.AddWithValue("@si_other_charge", model.OtherCharge);
                    cmd.Parameters.AddWithValue("@si_other_disc", model.OtherDisc);
                    cmd.Parameters.AddWithValue("@si_other_remarks", model.si_other_remarks);
                    cmd.Parameters.AddWithValue("@si_net_amount", model.NetAmount);


                    cmd.Parameters.AddWithValue("@si_cash_paid_acc", SqlDbType.Int).Value = model.CashPaidAccount ?? 0;
                    cmd.Parameters.AddWithValue("@si_cash_recieved", SqlDbType.Int).Value = model.CashAmount ?? 0;
                    cmd.Parameters.AddWithValue("@si_bankacc", SqlDbType.Int).Value = model.CardAccount ?? 0;
                    cmd.Parameters.AddWithValue("@si_card_amount", SqlDbType.Int).Value = model.CardAmount ?? 0;

                    cmd.Parameters.AddWithValue("@si_work_type_id", 1);
                    cmd.Parameters.AddWithValue("@si_coupon_no", model.complaint_id);
                    cmd.Parameters.AddWithValue("@si_finish", "");
                    cmd.Parameters.AddWithValue("@si_qtn_no", model.ticket_id);

                    cmd.Parameters.AddWithValue("@si_location_id", usqlre.locationId);
                    cmd.Parameters.AddWithValue("@si_user_id", usqlre.userId);
                    cmd.Parameters.AddWithValue("@si_color", model.si_color);
                    cmd.Parameters.AddWithValue("@si_sales_acc_id", 2);

                    // Table-valued parameters
                    cmd.Parameters.AddWithValue("@type1", dt1);
                    cmd.Parameters.AddWithValue("@type2", dtlent);
                    cmd.Parameters.AddWithValue("@StatementType", model.StatementType ?? "insert");
                    cmd.CommandTimeout = 60;
                    cmd.Connection = usqlre.shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();
                    int SqlError = Convert.ToInt32(errNumber.Value);
                    if (SqlError == 0)
                    {
                        tres = Convert.ToInt32(parm.Value);

                    }
                    else
                    {
                        string errmsg = errMessage.Value.ToString();

                        tres = -6;
                        //_tres = tres
                        // whatever else you want to do with the error data
                    }
                    tres = Convert.ToInt32(parm.Value);

                    if (tres > 0)
                    {
                        using (var cmd2 = new SqlCommand(
                    "INSERT INTO inv_service_status_history " +
                    "(ssh_ticket_id, ssh_status, ssh_technician_id, ssh_remarks, ssh_changed_by, ssh_changed_date) " +
                    "VALUES (@ticket_id, @status, @tech_id, @remarks, @changed_by,@ssh_changed_date)", usqlre.shop))
                        {
                            cmd2.Parameters.AddWithValue("@ticket_id", tres);
                            cmd2.Parameters.AddWithValue("@status", "Delivered");
                            cmd2.Parameters.AddWithValue("@tech_id", 0);
                            cmd2.Parameters.AddWithValue("@remarks", "");
                            cmd2.Parameters.AddWithValue("@changed_by", usqlre.userId);
                            cmd2.Parameters.AddWithValue("@ssh_changed_date", model.date);


                            await cmd2.ExecuteNonQueryAsync();
                        }
                        // 2️⃣ Update inv_sales_inf
                        using (var cmd3 = new SqlCommand(
                            "UPDATE inv_sales_inf " +
                            "SET SI_FINISH = 'Delivered' " +
                            "WHERE si_str_id = @str_id AND SI_ENTRYNO = @ticket_id",
                            usqlre.shop))
                        {
                            cmd3.Parameters.AddWithValue("@str_id", 12);
                            cmd3.Parameters.AddWithValue("@ticket_id", model.ticket_id);

                            await cmd3.ExecuteNonQueryAsync();
                        }
                        using (var cmd3 = new SqlCommand(
                            "UPDATE inv_sales_inf " +
                            "SET si_qtn_transfer_status = 1 WHERE si_str_id = @str_id AND SI_ENTRYNO = @ticket_id",
                            usqlre.shop))
                        {

                            cmd3.Parameters.AddWithValue("@str_id", str_id);
                            cmd3.Parameters.AddWithValue("@ticket_id", tres);

                            await cmd3.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
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
            finally
            {
                usqlre?.close();
            }

            return Ok(new
            {
                status = true,
                statusCode = 200,
                message = "Service delivered successfully",
                data = new
                {
                    entryNo = tres,
                    str_id = str_id
                }
            });
        }

        [HttpPost("update-delivery")]
        public async Task<IActionResult> UpdateDelivery([FromBody] ServiceDeliveryModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);


            if (model == null) return BadRequest(new { status = 0, message = "Invalid payload" });
            int str_id = model.isTax ? 1 : 14;

            if (model.delivery_entryno <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid delivery entry number."
                });
            }
            // Items collected string
            string si_items_collected = "";
            if (model.list_itemscollected != null && model.list_itemscollected.Count > 0)
            {
                si_items_collected = string.Join(",", model.list_itemscollected);
            }

            DataTable dt1 = new DataTable();
            dt1.Columns.Add("qty");//0
            dt1.Columns.Add("fqty");//1
            dt1.Columns.Add("s_rate");//2
            dt1.Columns.Add("real_rate");//3
            dt1.Columns.Add("gross");//4
            dt1.Columns.Add("disc_p");//5
            dt1.Columns.Add("disc");//6
            dt1.Columns.Add("real_disc");//7
            dt1.Columns.Add("net");//8
            dt1.Columns.Add("tax");//9
            dt1.Columns.Add("cgst");//10
            dt1.Columns.Add("sgst");//11
            dt1.Columns.Add("igst");//12
            dt1.Columns.Add("total");//13
            dt1.Columns.Add("pt");//14
            dt1.Columns.Add("uniquecode");//15
            dt1.Columns.Add("ir_id");//16
            dt1.Columns.Add("mrp");//17
            dt1.Columns.Add("qty_multi_unit");//18
            dt1.Columns.Add("unit_multi");//19
            dt1.Columns.Add("srate_multiunit");//20
            dt1.Columns.Add("narration");//21
            dt1.Columns.Add("cess");//22
            dt1.Columns.Add("ad_cess");//23
            dt1.Columns.Add("kfc");//24
            dt1.Columns.Add("support");//25
            dt1.Columns.Add("real_support");//26
            dt1.Columns.Add("salesman");//27
            dt1.Columns.Add("sp_prate");//28
            dt1.Columns.Add("sp_realprate");//29
            dt1.Columns.Add("sp_cost");//30
            dt1.Columns.Add("sp_local_exp");//31
            dt1.Columns.Add("sp_lend_amount");//32
            dt1.Columns.Add("sp_discp2");//33
            dt1.Columns.Add("sp_disc2");//34
            dt1.Columns.Add("sp_netratesingle");//35
            dt1.Columns.Add("sp_narration1");//36
            dt1.Columns.Add("sp_other_exp");//37
            dt1.Columns.Add("sp_print");//38
            dt1.Columns.Add("sp_card_disc");//39
            dt1.Columns.Add("sp_depreciation_per");//40
            dt1.Columns.Add("sp_depreciation");//41
            dt1.Columns.Add("sp_narration2");//42
            dt1.Columns.Add("sp_narration3");//43
            bool hasSpRowId = Convert.ToInt32(usqlre.dbScalar(@"
      SELECT COUNT(*)
      FROM INFORMATION_SCHEMA.COLUMNS
      WHERE TABLE_NAME='inv_sales_par'
      AND COLUMN_NAME='sp_row_id'")) > 0;

            if (hasSpRowId)
            {
                dt1.Columns.Add("sp_row_id");
            }
            int slNo = 1;
            // Populate dt1 from model.Items
            foreach (var row in model.Items ?? Enumerable.Empty<ItemRowModel>())
            {
                // Use slno presence as indicator, if null skip
                if (row == null || row.ir_id == null)
                    continue;
                // 🔹 STEP 1: Check service item
                string chkServiceSql = "select ir_service_item, ir_taxper from inv_item_reg where ir_id=" + row.ir_id;
                DataTable dtService = usqlre.dbReaderFill(chkServiceSql);
                int isService = 0;
                if (dtService.Rows.Count > 0)
                    isService = Convert.ToInt32(dtService.Rows[0]["ir_service_item"]);
                decimal sp_rate = 0, sp_realrate = 0, sp_gross = 0,
                sp_net = 0, sp_tax = 0, sp_total = 0,
                sp_igst = 0, sp_cgst = 0, sp_sgst = 0, sp_disc = 0,
                sp_mrp = 0, sp_srate_multiunit = 0,
                sp_prate = 0, sp_realprate = 0, sp_cost = 0, sp_taxper = 0;
                long sp_uniquecode = 0;
                if (isService == 1)
                {
                    sp_taxper = dtService.Rows[0]["ir_taxper"] != DBNull.Value
                ? Convert.ToDecimal(dtService.Rows[0]["ir_taxper"])
                : 0;

                    decimal total = row.total ?? 0;

                    if (sp_taxper > 0 && model.isTax)
                    {
                        // 🔹 TAX INCLUDED CALCULATION
                        decimal baseAmount = Math.Round(total / (1 + (sp_taxper / 100)), 2);
                        decimal taxAmount = Math.Round(total - baseAmount, 2);

                        sp_rate = total;
                        sp_realrate = baseAmount;
                        sp_gross = total;
                        sp_net = baseAmount;
                        sp_tax = taxAmount;
                        sp_total = total;

                        // 🔹 Split GST (assuming intra-state)
                        sp_cgst = Math.Round(taxAmount / 2, 3);
                        sp_sgst = Math.Round(taxAmount / 2, 3);

                        sp_igst = 0;
                    }
                    else
                    {
                        // 🔹 NO TAX OR TAX OFF
                        sp_rate = total;
                        sp_realrate = total;
                        sp_gross = total;
                        sp_net = total;
                        sp_tax = 0;
                        sp_total = total;

                        sp_cgst = 0;
                        sp_sgst = 0;
                        sp_igst = 0;
                    }

                    sp_disc = row.discount ?? 0;
                }
                else
                {
                    string sql = @"select top 1 sp_uniquecode,sp_rate,sp_realrate, sp_gross_value,sp_disc,sp_net_amount,sp_tax,sp_total,sp_igst,sp_cgst,sp_sgst,sp_mrp,sp_srate_multiunit,sp_prate,sp_realprate,sp_cost,sp_taxper from inv_sales_par where sp_str_id=12 and sp_entryno=" + model.ticket_id + " and sp_ir_id=" + row.ir_id + "";

                    DataTable dt = usqlre.dbReaderFill(sql);



                    if (dt != null && dt.Rows.Count > 0)
                    {
                        DataRow salesRow = dt.Rows[0];

                        sp_uniquecode = Convert.ToInt64(salesRow["sp_uniquecode"]);
                        sp_rate = Convert.ToDecimal(salesRow["sp_rate"]);
                        sp_realrate = Convert.ToDecimal(salesRow["sp_realrate"]);
                        sp_gross = Convert.ToDecimal(salesRow["sp_gross_value"]);
                        sp_disc = Convert.ToDecimal(salesRow["sp_disc"]);
                        sp_net = Convert.ToDecimal(salesRow["sp_net_amount"]);
                        sp_tax = Convert.ToDecimal(salesRow["sp_tax"]);
                        sp_total = Convert.ToDecimal(salesRow["sp_total"]);
                        sp_igst = Convert.ToDecimal(salesRow["sp_igst"]);
                        sp_cgst = Convert.ToDecimal(salesRow["sp_cgst"]);
                        sp_sgst = Convert.ToDecimal(salesRow["sp_sgst"]);
                        sp_mrp = Convert.ToDecimal(salesRow["sp_mrp"]);
                        sp_srate_multiunit = Convert.ToDecimal(salesRow["sp_srate_multiunit"]);
                        sp_prate = Convert.ToDecimal(salesRow["sp_prate"]);
                        sp_realprate = Convert.ToDecimal(salesRow["sp_realprate"]);
                        sp_cost = Convert.ToDecimal(salesRow["sp_cost"]);
                        sp_taxper = Convert.ToDecimal(salesRow["sp_taxper"]);
                        if (!model.isTax)
                        {
                            sp_tax = 0;
                            sp_cgst = 0;
                            sp_sgst = 0;
                            sp_igst = 0;
                            sp_srate_multiunit = 0;

                            // Net should be equal to total (no tax split)
                            sp_rate = sp_rate;
                            sp_realrate = sp_rate;
                            sp_net = sp_total;
                            sp_gross = sp_total;


                        }
                    }
                }


                DataRow dRow = dt1.NewRow();

                dRow[0] = row.qty;                 // qty
                dRow[1] = 0;                       // fqty
                dRow[2] = sp_rate;                 // s_rate
                dRow[3] = sp_realrate;             // real_rate
                dRow[4] = sp_gross;                // gross
                dRow[5] = 0;                       // disc_p
                dRow[6] = sp_disc;                 // disc
                dRow[7] = 0;                       // real_disc
                dRow[8] = sp_net;                  // net
                dRow[9] = sp_tax;                  // tax
                dRow[10] = sp_cgst;                 // cgst
                dRow[11] = sp_sgst;                 // sgst
                dRow[12] = sp_igst;                 // igst
                dRow[13] = sp_total;                // total
                dRow[14] = sp_taxper;               // pt
                dRow[15] = sp_uniquecode;           // uniquecode
                dRow[16] = row.ir_id;               // ir_id
                dRow[17] = sp_mrp;                  // mrp
                dRow[18] = 0;                       // qty_multi_unit
                dRow[19] = 0;                       // unit_multi
                dRow[20] = sp_srate_multiunit;      // srate_multiunit
                dRow[21] = "";                      // narration
                dRow[22] = 0;                       // cess
                dRow[23] = 0;                       // ad_cess
                dRow[24] = 0;                       // kfc
                dRow[25] = 0;                       // support
                dRow[26] = 0;                       // real_support
                dRow[27] = "-1";                    // salesman
                dRow[28] = sp_prate;                // sp_prate
                dRow[29] = sp_realprate;            // sp_realprate
                dRow[30] = sp_cost;                 // sp_cost
                dRow[31] = "";                      // sp_local_exp
                dRow[32] = 0;                       // sp_lend_amount
                dRow[33] = "";                      // sp_discp2
                dRow[34] = 0;                       // sp_disc2
                dRow[35] = 0;                       // sp_netratesingle
                dRow[36] = "";                      // sp_narration1
                dRow[37] = 0;                       // sp_other_exp
                dRow[38] = 0;                       // sp_print
                dRow[39] = 0;                       // sp_card_disc
                dRow[40] = 0;                       // sp_depreciation_per
                dRow[41] = 0;                       // sp_depreciation
                dRow[42] = "";                      // sp_narration2
                dRow[43] = "";                      // sp_narration3
                if (hasSpRowId)
                {
                    dRow["sp_row_id"] = slNo;
                    slNo++;
                }
                dt1.Rows.Add(dRow);
            }


            // Build dtlent
            DataTable dtlent = new DataTable();
            dtlent.Columns.Add("li_in");
            dtlent.Columns.Add("li_out");
            dtlent.Columns.Add("li_remarks");
            dtlent.Columns.Add("li_ir_id");
            dtlent.Columns.Add("li_ift_id");
            dtlent.Columns.Add("li_ir_mrp");

            foreach (var row in model.LendItems ?? Enumerable.Empty<LendRowModel>())
            {
                // skip if no li_ir_id
                if (row?.li_ir_id == null) continue;

                var dr1 = dtlent.NewRow();
                dr1[0] = "0";
                dr1[1] = "0";
                dr1[2] = row.li_remarks ?? "";
                dr1[3] = row.li_ir_id?.ToString() ?? "-1";
                dr1[4] = "0";
                dr1[5] = "0";
                dtlent.Rows.Add(dr1);
            }

            // Call SP
            int tres = -6;
            try
            {
                usqlre.OpenConnection();

                using (var cmd = new SqlCommand("Sp_Sale", usqlre.shop))
                {
                    //DataSet dataset = new DataSet();

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_Sale";
                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.Output;
                    SqlParameter errNumber = new SqlParameter("@errNumber", 0);
                    errNumber.Direction = ParameterDirection.Output;
                    SqlParameter errLine = new SqlParameter("@errLine", 0);
                    errNumber.Direction = ParameterDirection.Output;
                    SqlParameter errMessage = new SqlParameter("@errMessage", SqlDbType.VarChar, -1);
                    errMessage.Direction = ParameterDirection.Output;

                    cmd.Parameters.Add(parm);
                    cmd.Parameters.Add(errNumber);
                    cmd.Parameters.Add(errLine);
                    cmd.Parameters.Add(errMessage);

                    string sql = @"SELECT ISNULL(si_acc_id,0) AS si_acc_id,isnull(si_add1,'') as si_add1,isnull(si_add2,'') as si_add2, ISNULL(si_profit,0) AS si_profit, ISNULL(si_tax,0) AS si_tax, ISNULL(si_cgst_total,0) AS si_cgst_total, ISNULL(si_sgst_total,0) AS si_sgst_total,ISNULL(si_gross_value,0) AS si_gross_value, si_category_id FROM inv_sales_inf  WHERE si_entryno = " + model.ticket_id + @"  AND si_str_id = 12";

                    DataTable dt = usqlre.dbReaderFill(sql);

                    // Default values
                    decimal si_profit = 0;
                    decimal si_tax = 0;
                    decimal si_cgst_total = 0;
                    decimal si_sgst_total = 0;
                    decimal si_gross_value = 0;
                    string si_add1 = "";
                    string si_add2 = "";
                    int si_category_id = 0;
                    int si_acc_id = 0;
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];

                        si_acc_id = Convert.ToInt32(row["si_acc_id"]);
                        si_profit = Convert.ToDecimal(row["si_profit"]);
                        si_tax = Convert.ToDecimal(row["si_tax"]);
                        si_cgst_total = Convert.ToDecimal(row["si_cgst_total"]);
                        si_sgst_total = Convert.ToDecimal(row["si_sgst_total"]);
                        si_gross_value = Convert.ToDecimal(row["si_gross_value"]);
                        si_add1 = row["si_add1"].ToString();
                        si_add2 = row["si_add2"].ToString();
                        si_category_id = Convert.ToInt32(row["si_category_id"]);
                    }

                    // Pass values to stored procedure
                    cmd.Parameters.AddWithValue("@si_profit", si_profit);
                    cmd.Parameters.AddWithValue("@si_tax", si_tax);
                    cmd.Parameters.AddWithValue("@si_cgst_total", si_cgst_total);
                    cmd.Parameters.AddWithValue("@si_sgst_total", si_sgst_total);
                    cmd.Parameters.AddWithValue("@si_gross_value", si_gross_value);
                    cmd.Parameters.AddWithValue("@si_category_id", si_category_id);

                    decimal ob = 0;

                    string sqldata = @"SELECT ISNULL(SUM(at_Dr) - SUM(at_Cr), 0) AS ob FROM acc_account_transactions WHERE CAST(at_date AS DATE) <= CAST('" + DateTime.Now + "' AS DATE) AND at_as_id = " + si_acc_id + "";
                    DataTable dtdata = usqlre.dbReaderFill(sqldata);
                    if (dtdata != null && dtdata.Rows.Count > 0)
                    {
                        ob = Convert.ToDecimal(dtdata.Rows[0]["ob"]);
                    }

                    decimal netBalance = ob + model.GrandTotal;

                    cmd.Parameters.AddWithValue("@si_ob", ob);
                    cmd.Parameters.AddWithValue("@si_net_balance", netBalance);
                    cmd.Parameters.AddWithValue("@si_str_id", str_id);
                    cmd.Parameters.AddWithValue("@si_entryno", model.delivery_entryno);
                    cmd.Parameters.AddWithValue("@si_date", model.date);
                    cmd.Parameters.AddWithValue("@si_acc_id", si_acc_id);
                    cmd.Parameters.AddWithValue("@si_cust_name", model.si_cust_name ?? "");
                    cmd.Parameters.AddWithValue("@si_assign_to", model.si_assign_to);
                    cmd.Parameters.AddWithValue("@si_batteryno", model.BatteryNo ?? "");
                    cmd.Parameters.AddWithValue("@si_add1", si_add1 ?? "");
                    cmd.Parameters.AddWithValue("@si_add2", si_add2 ?? "");
                    cmd.Parameters.AddWithValue("@si_items_collected", si_items_collected);
                    cmd.Parameters.AddWithValue("@si_company", model.Brand ?? "");
                    cmd.Parameters.AddWithValue("@si_model", model.si_model ?? "");
                    cmd.Parameters.AddWithValue("@si_imei", model.si_imei ?? "");
                    cmd.Parameters.AddWithValue("@si_expected_date", model.si_expected_date ?? "");
                    cmd.Parameters.AddWithValue("@si_deliverydate", model.si_deliverydate ?? "");
                    cmd.Parameters.AddWithValue("@si_remarks", model.EstimateCost ?? "");

                    cmd.Parameters.AddWithValue("@si_grand_total", model.GrandTotal);
                    cmd.Parameters.AddWithValue("@si_total", model.Total);
                    cmd.Parameters.AddWithValue("@si_balance", model.Balance);
                    cmd.Parameters.AddWithValue("@si_other_charge", model.OtherCharge);
                    cmd.Parameters.AddWithValue("@si_other_disc", model.OtherDisc);
                    cmd.Parameters.AddWithValue("@si_other_remarks", model.si_other_remarks);
                    cmd.Parameters.AddWithValue("@si_net_amount", model.NetAmount);


                    cmd.Parameters.AddWithValue("@si_cash_paid_acc", SqlDbType.Int).Value = model.CashPaidAccount ?? 0;
                    cmd.Parameters.AddWithValue("@si_cash_recieved", SqlDbType.Int).Value = model.CashAmount ?? 0;
                    cmd.Parameters.AddWithValue("@si_bankacc", SqlDbType.Int).Value = model.CardAccount ?? 0;
                    cmd.Parameters.AddWithValue("@si_card_amount", SqlDbType.Int).Value = model.CardAmount ?? 0;

                    cmd.Parameters.AddWithValue("@si_work_type_id", 1);
                    cmd.Parameters.AddWithValue("@si_coupon_no", model.complaint_id);
                    cmd.Parameters.AddWithValue("@si_finish", "");
                    cmd.Parameters.AddWithValue("@si_qtn_no", model.ticket_id);

                    cmd.Parameters.AddWithValue("@si_location_id", usqlre.locationId);
                    cmd.Parameters.AddWithValue("@si_user_id", usqlre.userId);
                    cmd.Parameters.AddWithValue("@si_color", model.si_color);
                    cmd.Parameters.AddWithValue("@si_sales_acc_id", 2);

                    // Table-valued parameters
                    cmd.Parameters.AddWithValue("@type1", dt1);
                    cmd.Parameters.AddWithValue("@type2", dtlent);
                    cmd.Parameters.AddWithValue("@StatementType", "Update");
                    cmd.CommandTimeout = 60;
                    cmd.Connection = usqlre.shop;
                    //SqlDataReader dr = cmd.ExecuteReader();
                    //dr.Dispose();
                    //dr.Close();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                    }
                    int SqlError = Convert.ToInt32(errNumber.Value);
                    if (SqlError == 0)
                    {
                        tres = Convert.ToInt32(parm.Value);

                    }
                    else
                    {
                        string errmsg = errMessage.Value.ToString();

                        tres = -6;
                        //_tres = tres
                        // whatever else you want to do with the error data
                    }


                    if (SqlError == 0)
                    {
                        using (var cmd2 = new SqlCommand(
                    "INSERT INTO inv_service_status_history " +
                    "(ssh_ticket_id, ssh_status, ssh_technician_id, ssh_remarks, ssh_changed_by, ssh_changed_date) " +
                    "VALUES (@ticket_id, @status, @tech_id, @remarks, @changed_by,@ssh_changed_date)", usqlre.shop))
                        {
                            cmd2.Parameters.AddWithValue("@ticket_id", model.delivery_entryno);
                            cmd2.Parameters.AddWithValue("@status", "Delivery Updated");
                            cmd2.Parameters.AddWithValue("@tech_id", 0);
                            cmd2.Parameters.AddWithValue("@remarks", "");
                            cmd2.Parameters.AddWithValue("@changed_by", usqlre.userId);
                            cmd2.Parameters.AddWithValue("@ssh_changed_date", model.date);


                            await cmd2.ExecuteNonQueryAsync();
                        }
                        // 2️⃣ Update inv_sales_inf
                        using (var cmd3 = new SqlCommand(
                            "UPDATE inv_sales_inf " +
                            "SET SI_FINISH = 'Delivered' " +
                            "WHERE si_str_id = @str_id AND SI_ENTRYNO = @ticket_id",
                            usqlre.shop))
                        {
                            cmd3.Parameters.AddWithValue("@str_id", 12);
                            cmd3.Parameters.AddWithValue("@ticket_id", model.ticket_id);

                            await cmd3.ExecuteNonQueryAsync();
                        }
                        using (var cmd3 = new SqlCommand(
                            "UPDATE inv_sales_inf " +
                            "SET si_qtn_transfer_status = 1 WHERE si_str_id = @str_id AND SI_ENTRYNO = @ticket_id",
                            usqlre.shop))
                        {

                            cmd3.Parameters.AddWithValue("@str_id", str_id);
                            cmd3.Parameters.AddWithValue("@ticket_id", model.delivery_entryno);

                            await cmd3.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
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
            finally
            {
                usqlre?.close();
            }

            return Ok(new
            {
                status = true,
                statusCode = 200,
                message = "Service delivery updated successfully",
                data = new
                {
                    entryNo = model.delivery_entryno,
                    str_id = str_id
                }
            });
        }

        [HttpPost("qc-approve")]
        public async Task<IActionResult> QcApprove(QCApproveModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "qc_approve");
                cmd.Parameters.AddWithValue("@si_entryno", model.si_entryno);
                cmd.Parameters.AddWithValue("@qc_status", model.qc_status);
                cmd.Parameters.AddWithValue("@qc_findings", model.qc_findings);
                cmd.Parameters.AddWithValue("@qc_technician_id", model.qc_technician_id);
                cmd.Parameters.AddWithValue("@user_id", model.user_id);

                // -----------------------------
                // TVP for QC Checklist
                // -----------------------------
                DataTable dtQc = new DataTable();
                dtQc.Columns.Add("qc_id", typeof(int));
                dtQc.Columns.Add("qc_checked", typeof(bool));

                if (model.qc_list != null)
                {
                    foreach (var item in model.qc_list)
                    {
                        dtQc.Rows.Add(item.qc_id, item.qc_checked);
                    }
                }

                SqlParameter tvpParam = cmd.Parameters.AddWithValue("@qc_list", dtQc);
                tvpParam.SqlDbType = SqlDbType.Structured;
                tvpParam.TypeName = "dbo.Type_QC_Checklist";

                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "QC updated successfully",
                    data = dt
                };
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
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
            finally
            {
                usqlre?.close();
            }
        }
        [HttpGet("get-qc-by-ticket-id/{ticketId}")]
        public async Task<IActionResult> GetQCByTicketId(int ticketId)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "GetQCByTicketId");
                cmd.Parameters.AddWithValue("@si_entryno", ticketId);

                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);


                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "QC details not found",
                        data = (object)null
                    });
                }

                // QC Header
                var qcRow = ds.Tables[0].Rows[0];
                var qcHeader = ds.Tables[0].Columns
                    .Cast<DataColumn>()
                    .ToDictionary(col => col.ColumnName, col => qcRow[col]);

                // QC Checklist
                var qcChecklist = ds.Tables.Count > 1 ? ds.Tables[1] : null;

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "QC details fetched successfully",
                    data = new
                    {
                        qcHeader,
                        qcChecklist
                    }
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
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
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("qc-completed-list")]
        public async Task<IActionResult> QcCompletedList()
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "QCCompletedList");

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Ticket list fetched successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("get-completed-list-detailed")]
        public async Task<IActionResult> GetQCCompletedListDetailed(int Id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "QCCompletedListDetailed");
                cmd.Parameters.AddWithValue("@si_entryno", Id);
                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);
                object ticket = null;

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    ticket = ds.Tables[0].Columns
                        .Cast<DataColumn>()
                        .ToDictionary(
                            col => col.ColumnName,
                            col => row[col] == DBNull.Value ? null : row[col]
                        );
                }
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Details fetched successfully",
                    data = new
                    {
                        ticket = ticket,
                        complaint = ds.Tables.Count > 1 ? ds.Tables[1] : null,
                        images = ds.Tables.Count > 2 ? ds.Tables[2] : null,
                        qcList = ds.Tables.Count > 3 ? ds.Tables[3] : null
                    }
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("delivery-list")]
        public async Task<IActionResult> DeliveryList()
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "DeliveryList");

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Delivery list fetched successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("completed-works")]
        public async Task<IActionResult> GetCompletedWorks(DateTime? fromDate, DateTime? toDate, string search = "")
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "CompletedWorksList");
                cmd.Parameters.AddWithValue("@from_date", (object?)fromDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@to_date", (object?)toDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@search", search ?? "");

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Completed works fetched successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("completed_works-detailed")]
        public async Task<IActionResult> GetCompletedWorksDetailed(int id, int si_str_id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "getDeliveryById");
                cmd.Parameters.AddWithValue("@si_entryno", id);
                cmd.Parameters.AddWithValue("@si_str_id", si_str_id);

                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);
                object ticket = null;

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    ticket = ds.Tables[0].Columns
                        .Cast<DataColumn>()
                        .ToDictionary(
                            col => col.ColumnName,
                            col => row[col] == DBNull.Value ? null : row[col]
                        );
                }
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Technician Dashboard details fetched successfully",
                    data = new
                    {
                        ticket = ticket,
                        complaint = ds.Tables.Count > 1 ? ds.Tables[1] : null,
                        SpareList = ds.Tables.Count > 1 ? ds.Tables[2] : null
                    }
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpPost("service-return")]
        public async Task<IActionResult> ServiceReturn(int si_entryno, int as_id, string remarks, int si_str_id)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "serviceReturn");
                cmd.Parameters.AddWithValue("@si_entryno", si_entryno);
                cmd.Parameters.AddWithValue("@as_id", as_id);
                cmd.Parameters.AddWithValue("@created_by", usqlre.userId);
                cmd.Parameters.AddWithValue("@remarks", remarks);
                cmd.Parameters.AddWithValue("@si_str_id", si_str_id);

                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Technician assigned successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpGet("get-delivery-by-id/{deliveryId}")]
        public async Task<IActionResult> GetDeliveryById(int deliveryId, int si_str_id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "GetDeliveryByDeliveryId");
                cmd.Parameters.AddWithValue("@si_entryno", deliveryId);
                cmd.Parameters.AddWithValue("@si_str_id", si_str_id);

                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);


                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Delivery not found"
                    });
                }
                var deliveryRow = ds.Tables[0].Rows[0];
                var delivery = ds.Tables[0].Columns
                    .Cast<DataColumn>()
                    .ToDictionary(col => col.ColumnName, col => deliveryRow[col]);

                var companyRow = ds.Tables[3].Rows[0];
                var company = ds.Tables[3].Columns
                    .Cast<DataColumn>()
                    .ToDictionary(col => col.ColumnName, col => companyRow[col]);

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Delivery details fetched successfully",
                    data = new
                    {
                        delivery,
                        lendItems = ds.Tables.Count > 1 ? ds.Tables[1] : null,
                        spareList = ds.Tables.Count > 2 ? ds.Tables[2] : null,
                        company
                    }
                };
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpPost("search-customer-tickets-inf")]
        public IActionResult SearchCustomerTicketsInf(SearchModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "searchCustomerTicketsInf");
                cmd.Parameters.AddWithValue("@si_entryno", model.entryno == 0 ? DBNull.Value : model.entryno);
                cmd.Parameters.AddWithValue("@si_mob", string.IsNullOrWhiteSpace(model.mobilno) ? DBNull.Value : model.mobilno);
                cmd.Parameters.AddWithValue("@si_cust_name", string.IsNullOrWhiteSpace(model.custname) ? DBNull.Value : model.custname);
                cmd.Parameters.AddWithValue("@si_imei", string.IsNullOrWhiteSpace(model.imei) ? DBNull.Value : model.imei);
                cmd.Parameters.AddWithValue("@si_batteryno", string.IsNullOrWhiteSpace(model.slno) ? DBNull.Value : model.slno);
                cmd.Parameters.AddWithValue("@r_route", string.IsNullOrWhiteSpace(model.route) ? DBNull.Value : model.route);
                cmd.Parameters.AddWithValue("@brand_name", string.IsNullOrWhiteSpace(model.brandName) ? DBNull.Value : model.brandName);
                cmd.Parameters.AddWithValue("@model_name", string.IsNullOrWhiteSpace(model.modelName) ? DBNull.Value : model.modelName);
                cmd.Parameters.AddWithValue("@complaint", string.IsNullOrWhiteSpace(model.complaint) ? DBNull.Value : model.complaint);
                cmd.Parameters.AddWithValue("@as_name", string.IsNullOrWhiteSpace(model.technicianName) ? DBNull.Value : model.technicianName);
                cmd.Parameters.AddWithValue("@from_date", model.fromDate.HasValue ? (object)model.fromDate.Value.Date : DBNull.Value);
                cmd.Parameters.AddWithValue("@to_date", model.toDate.HasValue ? (object)model.toDate.Value.Date : DBNull.Value);
                cmd.Parameters.AddWithValue("@si_finish", string.IsNullOrWhiteSpace(model.si_finish) ? DBNull.Value : model.si_finish);
                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }


                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Details fetched successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpGet("search-customer-tickets")]
        public async Task<IActionResult> SearchCustomerTickets(string search)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "searchCustomerTickets");
                cmd.Parameters.AddWithValue("@search", search); // ✅ FIXED

                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);
                object ticket = null;

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];

                    ticket = ds.Tables[0].Columns
                        .Cast<DataColumn>()
                        .ToDictionary(
                            col => col.ColumnName,
                            col => row[col] == DBNull.Value ? null : row[col]
                        );
                }
                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Details fetched successfully",
                    data = new
                    {
                        ticket = ticket,

                        complaint = ds.Tables.Count > 1 ? ds.Tables[1] : null,

                        SpareList = ds.Tables.Count > 2 ? ds.Tables[2] : null,

                        // NEW
                        images = ds.Tables.Count > 3 ? ds.Tables[3] : null,

                        qcHeader = ds.Tables.Count > 4 && ds.Tables[4].Rows.Count > 0
                        ? ds.Tables[4].Columns.Cast<DataColumn>()
                            .ToDictionary(
                                col => col.ColumnName,
                                col => ds.Tables[4].Rows[0][col] == DBNull.Value
                                    ? null
                                    : ds.Tables[4].Rows[0][col]
                            )
                        : null,

                        qcChecklist = ds.Tables.Count > 5 ? ds.Tables[5] : null
                    }
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("get-all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "GetAllUsers");

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);


                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Details fetched successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
        [HttpGet("get-service-item")]
        public async Task<IActionResult> GetServiceItem(string? searchKey)
        {
            var cancellationToken = HttpContext.RequestAborted;
            try
            {
                DataTable dt = await ReadServiceTableAsync(
                    "Sp_Service_Complaint_App",
                    CommandType.StoredProcedure,
                    new SqlParameter("@ir_name", (object)searchKey ?? DBNull.Value),
                    new SqlParameter("@StatementType", "serviceItemList"));

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Service Item List Fetched Successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        [HttpPost("move-to-get_tickets")]
        public async Task<IActionResult> MoveToGetTickets(long si_entryno)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "MoveToGetTickets");
                cmd.Parameters.AddWithValue("@si_entryno", si_entryno);

                DataTable dt = new DataTable();

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Ticket moved to GetTickets successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error : " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.close();
            }
        }
        [HttpPost("insert-service-customer")]
        public async Task<IActionResult> InsertAccSubHead(AccSubHeadModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "insert_customer");
                cmd.Parameters.AddWithValue("@as_name", model.as_name ?? "");
                cmd.Parameters.AddWithValue("@as_mob", model.as_mob ?? "");
                cmd.Parameters.AddWithValue("@as_mail", model.as_mail ?? "");
                cmd.Parameters.AddWithValue("@as_add1", model.as_add1 ?? "");
                cmd.Parameters.AddWithValue("@as_add2", model.as_add2 ?? "");
                cmd.Parameters.AddWithValue("@as_rout_id", model.as_rout_id);

                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);


                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Ledger created successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.close();
            }
        }
        [HttpPost("update-service-customer")]
        public async Task<IActionResult> UpdateAccSubHead(AccSubHeadModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "update_customer");
                cmd.Parameters.AddWithValue("@as_id", model.as_id);
                cmd.Parameters.AddWithValue("@as_name", model.as_name ?? "");
                cmd.Parameters.AddWithValue("@as_mob", model.as_mob ?? "");
                cmd.Parameters.AddWithValue("@as_mail", model.as_mail ?? "");
                cmd.Parameters.AddWithValue("@as_add1", model.as_add1 ?? "");
                cmd.Parameters.AddWithValue("@as_add2", model.as_add2 ?? "");
                cmd.Parameters.AddWithValue("@as_rout_id", model.as_rout_id);

                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dt = new DataTable();
                dt.Load(dr);


                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Customer updated successfully",
                    data = dt
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.close();
            }
        }

        [HttpPost("insert-billwise-receipt")]
        public async Task<IActionResult> InsertBillwiseReceipt([FromBody] BillwiseReceiptModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                if (model.Details == null || model.Details.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "At least one bill detail is required."
                    });
                }

                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                int locationId = model.vbri_location_id > 0 ? model.vbri_location_id : Convert.ToInt32(usqlre.locationId);
                int userId = model.vbri_user_id > 0 ? model.vbri_user_id : Convert.ToInt32(usqlre.userId);
                int salesmanId = model.vbri_salesman != 0 ? model.vbri_salesman : Convert.ToInt32(usqlre.gu_acc_id);
                int partyAccId = await GetBillwiseReceiptPartyAccountId(usqlre.shop, model.Details);

                if (partyAccId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Unable to find customer account from bill details."
                    });
                }

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "insert_billwise_receipt");
                cmd.Parameters.AddWithValue("@vbri_cash_acc", model.vbri_cash_acc);
                cmd.Parameters.AddWithValue("@vbri_party_acc", partyAccId);
                cmd.Parameters.AddWithValue("@vbri_total", model.vbri_total);
                cmd.Parameters.AddWithValue("@vbri_remarks", model.vbri_remarks ?? "");
                cmd.Parameters.AddWithValue("@vbri_verified", 0);
                cmd.Parameters.AddWithValue("@vbri_salesman", salesmanId);
                cmd.Parameters.AddWithValue("@vbri_location_id", locationId);
                cmd.Parameters.AddWithValue("@vbri_user_id", userId);
                cmd.Parameters.Add(BuildBillwiseReceiptDetailsTable(model.Details));

                SqlDataReader dr = await cmd.ExecuteReaderAsync();

                DataTable dtResult = new DataTable();
                dtResult.Load(dr);


                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Billwise receipt saved successfully",
                    data = dtResult
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                usqlre?.close();
            }
        }

        private static async Task<int> GetBillwiseReceiptPartyAccountId(SqlConnection connection, List<ReceiptDetailModel> details)
        {
            int partyAccId = 0;

            const string sql = @"
                SELECT TOP 1 ISNULL(SI.si_acc_id, 0)
                FROM inv_sales_inf SI
                LEFT JOIN inv_sales_type_reg STR ON STR.str_id = SI.si_str_id
                WHERE CONVERT(NVARCHAR(50), SI.si_entryno) = @billno
                  AND (CONVERT(NVARCHAR(50), SI.si_str_id) = @form OR STR.str_name = @form)";

            foreach (ReceiptDetailModel detail in details)
            {
                string billNo = detail.vbrp_billno?.Trim() ?? "";
                string form = detail.vbrp_form?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(billNo) || string.IsNullOrWhiteSpace(form))
                {
                    return 0;
                }

                using SqlCommand partyCmd = new SqlCommand(sql, connection);
                partyCmd.Parameters.AddWithValue("@billno", billNo);
                partyCmd.Parameters.AddWithValue("@form", form);

                object result = await partyCmd.ExecuteScalarAsync();
                int currentPartyAccId = result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);

                if (currentPartyAccId <= 0)
                {
                    return 0;
                }

                if (partyAccId == 0)
                {
                    partyAccId = currentPartyAccId;
                }
                else if (partyAccId != currentPartyAccId)
                {
                    return 0;
                }
            }

            return partyAccId;
        }

        [HttpPost("approve-billwise-receipt")]
        public async Task<IActionResult> ApproveBillwiseReceipt([FromBody] ApproveBillwiseReceiptModel model)
        {
            UserSqlServer usqlre = null;

            try
            {
                if (model.vbri_id <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "A valid vbri_id is required."
                    });
                }

                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                string headerSql = @"
                    SELECT vbri_id, vbri_cash_acc, vbri_party_acc, vbri_date, vbri_total,
                           vbri_remarks, vbri_salesman, vbri_location_id, vbri_user_id, vbri_verified
                    FROM inv_verify_billwise_reciept_inf
                    WHERE vbri_id = @vbri_id";

                DataTable dtHeader;
                using (SqlCommand headerCmd = new SqlCommand(headerSql, usqlre.shop))
                {
                    headerCmd.Parameters.AddWithValue("@vbri_id", model.vbri_id);
                    dtHeader = new DataTable();
                    using SqlDataAdapter adapter = new SqlDataAdapter(headerCmd);
                    adapter.Fill(dtHeader);
                }

                if (dtHeader.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "Pending billwise receipt not found."
                    });
                }

                DataRow header = dtHeader.Rows[0];
                if (Convert.ToInt32(header["vbri_verified"]) == 1)
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "This billwise receipt is already approved."
                    });
                }

                DateTime receiptDate = Convert.ToDateTime(header["vbri_date"]);
                if (!usqlre.FinancialDateCheck(receiptDate))
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Receipt date is outside the active financial period."
                    });
                }

                string detailSql = @"
                    SELECT
                        d.vbrp_billno,
                        str.str_name,
                        d.vbrp_bill_amount,
                        d.vbrp_amount,
                        si.si_entryno,
                        si.si_date,
                        si.si_grand_total
                    FROM inv_verify_billwise_reciept_pur d
                    LEFT JOIN inv_sales_type_reg str ON str.str_id = d.vbrp_form
                    LEFT JOIN inv_sales_inf si
                        ON si.si_entryno = d.vbrp_billno
                       AND si.si_str_id = str.str_id
                    WHERE d.vbrp_vbri_id = @vbri_id";

                DataTable dtDetails;
                using (SqlCommand detailCmd = new SqlCommand(detailSql, usqlre.shop))
                {
                    detailCmd.Parameters.AddWithValue("@vbri_id", model.vbri_id);
                    dtDetails = new DataTable();
                    using SqlDataAdapter adapter = new SqlDataAdapter(detailCmd);
                    adapter.Fill(dtDetails);
                }

                if (dtDetails.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "No bill details found for this receipt."
                    });
                }

                var billWisePur = new List<AccBillwisePur>();
                foreach (DataRow row in dtDetails.Rows)
                {
                    if (row["si_entryno"] == DBNull.Value)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            statusCode = 400,
                            message = $"Sales bill not found for bill no {row["vbrp_billno"]}."
                        });
                    }

                    decimal billAmount = Convert.ToDecimal(row["vbrp_bill_amount"]);
                    decimal receivedAmount = Convert.ToDecimal(row["vbrp_amount"]);

                    billWisePur.Add(new AccBillwisePur
                    {
                        chkTick = true,
                        voucherEntryno = Convert.ToInt64(row["si_entryno"]),
                        billDate = Convert.ToDateTime(row["si_date"]).ToString("yyyy-MM-dd"),
                        voucherType = row["str_name"].ToString(),
                        billAmount = Convert.ToDouble(billAmount),
                        billBalance = Convert.ToDouble(billAmount - receivedAmount),
                        amount = Convert.ToDouble(receivedAmount),
                        chequeNo = "",
                        discount = 0,
                        tds = 0,
                        locEntryno = row["vbrp_billno"].ToString(),
                        supInvno = ""
                    });
                }

                decimal totalAmount = Convert.ToDecimal(header["vbri_total"]);
                var receiptModel = new InvoicewisePaymentAndReceiptModel
                {
                    entryNo = 0,
                    voucherName = "RECEIPT-I",
                    date = receiptDate,
                    accountId = Convert.ToInt32(header["vbri_party_acc"]),
                    drCrAccId = Convert.ToInt32(header["vbri_cash_acc"]),
                    amount = totalAmount,
                    sumDisc = 0,
                    sumTds = 0,
                    reference = "",
                    netAmount = totalAmount,
                    grandTotal = totalAmount,
                    remarks = header["vbri_remarks"]?.ToString() ?? "",
                    billWisePur = billWisePur.ToArray()
                };

                int receiptEntryNo = usqlre.Sp_Billwise("Insert", receiptModel);
                if (receiptEntryNo <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Failed to post receipt to accounting."
                    });
                }

                using (SqlCommand approveCmd = new SqlCommand(
                    "UPDATE inv_verify_billwise_reciept_inf SET vbri_verified = 1 WHERE vbri_id = @vbri_id",
                    usqlre.shop))
                {
                    approveCmd.Parameters.AddWithValue("@vbri_id", model.vbri_id);
                    await approveCmd.ExecuteNonQueryAsync();
                }


                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Billwise receipt approved and posted successfully",
                    data = new
                    {
                        vbri_id = model.vbri_id,
                        receiptEntryNo
                    }
                });
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
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
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }
        }

        private static SqlParameter BuildBillwiseReceiptDetailsTable(List<ReceiptDetailModel> details)
        {
            DataTable dtDetails = new DataTable();
            dtDetails.Columns.Add("vbrp_billno", typeof(string));
            dtDetails.Columns.Add("vbrp_form", typeof(string));
            dtDetails.Columns.Add("vbrp_bill_amount", typeof(decimal));
            dtDetails.Columns.Add("vbrp_amount", typeof(decimal));

            foreach (var item in details)
            {
                dtDetails.Rows.Add(item.vbrp_billno, item.vbrp_form, item.vbrp_bill_amount, item.vbrp_amount);
            }

            SqlParameter tvpParam = new SqlParameter("@ReceiptDetails", dtDetails)
            {
                SqlDbType = SqlDbType.Structured,
                TypeName = "dbo.Type_inv_verify_billwise_reciept_pur"
            };

            return tvpParam;
        }

        [HttpPost("bulk-approve-billwise-receipt")]
        public async Task<IActionResult> BulkApproveBillwiseReceipt([FromBody] BulkApproveBillwiseReceiptModel model)
        {
            UserSqlServer usqlre = null;
            var successList = new List<object>();
            var failedList = new List<object>();
            try
            {
                if (model.vbri_ids == null || model.vbri_ids.Count == 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "vbri_ids required"
                    });
                }
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();
                foreach (int vbri_id in model.vbri_ids)
                {
                    try
                    {

                        string headerSql = @"SELECT * FROM inv_verify_billwise_reciept_inf  WHERE vbri_id=@vbri_id";
                        DataTable dtHeader = new DataTable();
                        using (SqlCommand cmd = new SqlCommand(headerSql, usqlre.shop))
                        {
                            cmd.Parameters.AddWithValue("@vbri_id", vbri_id);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(dtHeader);
                            }
                        }
                        if (dtHeader.Rows.Count == 0)
                        {
                            failedList.Add(new
                            {
                                vbri_id,
                                message = "Receipt not found"
                            });

                            continue;
                        }
                        DataRow header = dtHeader.Rows[0];
                        if (Convert.ToInt32(header["vbri_verified"]) == 1)
                        {
                            failedList.Add(new
                            {
                                vbri_id,
                                message = "Already approved"
                            });

                            continue;
                        }
                        string detailSql = @"
                        SELECT
                            d.vbrp_billno,
                            str.str_name,
                            d.vbrp_bill_amount,
                            d.vbrp_amount,
                            si.si_entryno,
                            si.si_date
                        FROM inv_verify_billwise_reciept_pur d
                        LEFT JOIN inv_sales_type_reg str
                        ON str.str_id=d.vbrp_form
                        LEFT JOIN inv_sales_inf si
                        ON si.si_entryno=d.vbrp_billno
                        AND si.si_str_id=str.str_id
                        WHERE d.vbrp_vbri_id=@vbri_id";
                        DataTable dtDetails = new DataTable();
                        using (SqlCommand cmd = new SqlCommand(detailSql, usqlre.shop))
                        {
                            cmd.Parameters.AddWithValue("@vbri_id", vbri_id);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(dtDetails);
                            }
                        }
                        var billWise = new List<AccBillwisePur>();
                        foreach (DataRow row in dtDetails.Rows)
                        {
                            billWise.Add(new AccBillwisePur
                            {
                                chkTick = true,
                                voucherEntryno = Convert.ToInt64(row["si_entryno"]),
                                billDate = Convert.ToDateTime(row["si_date"]).ToString("yyyy-MM-dd"),
                                voucherType = row["str_name"].ToString(),
                                billAmount = Convert.ToDouble(row["vbrp_bill_amount"]),
                                billBalance = Convert.ToDouble(Convert.ToDecimal(row["vbrp_bill_amount"]) - Convert.ToDecimal(row["vbrp_amount"])),
                                amount = Convert.ToDouble(row["vbrp_amount"]),
                                locEntryno = row["vbrp_billno"].ToString()
                            });
                        }
                        InvoicewisePaymentAndReceiptModel receipt =
                        new InvoicewisePaymentAndReceiptModel
                        {
                            entryNo = 0,
                            voucherName = "RECEIPT-I",
                            date = Convert.ToDateTime(header["vbri_date"]),
                            accountId = Convert.ToInt32(header["vbri_party_acc"]),
                            drCrAccId = Convert.ToInt32(header["vbri_cash_acc"]),
                            amount = Convert.ToDecimal(header["vbri_total"]),
                            netAmount = Convert.ToDecimal(header["vbri_total"]),
                            grandTotal = Convert.ToDecimal(header["vbri_total"]),
                            remarks = header["vbri_remarks"].ToString(),
                            billWisePur = billWise.ToArray()
                        };
                        int entryNo = usqlre.Sp_Billwise("Insert", receipt);
                        if (entryNo > 0)
                        {
                            using (SqlCommand update =
                            new SqlCommand(@" UPDATE inv_verify_billwise_reciept_inf SET vbri_verified=1 WHERE vbri_id=@vbri_id",
                            usqlre.shop))
                            {
                                update.Parameters.AddWithValue("@vbri_id", vbri_id);
                                await update.ExecuteNonQueryAsync();
                            }
                            successList.Add(new
                            {
                                vbri_id,
                                receiptEntryNo = entryNo
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        failedList.Add(new
                        {
                            vbri_id,
                            message = ex.Message
                        });
                    }
                }
                return Ok(new
                {
                    status = true,
                    message = "Bulk approval completed",
                    approved = successList,
                    failed = failedList
                });
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });

            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }
        }

        [HttpGet("service-collection-report")]
        public async Task<IActionResult> ServiceCollectionReport(DateTime fromDate, DateTime toDate, int led_id = 0, int loc_id = 0, int rout_id = 0, string billNo = "")
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                string reportSql = @"

                    SELECT 
                    h.vbri_id,
                    h.vbri_date,
                    d.vbrp_billno,
                    d.vbrp_bill_amount,
                    d.vbrp_amount,
                    (
                        ISNULL(d.vbrp_balance,0)
                        - ISNULL(ADVSI.si_cash_recieved,0)
                        - ISNULL(ADVSI.si_card_amount,0)
                    ) AS balance_amount,
                    CASE 
                        WHEN ISNULL(h.vbri_verified,0) = 0 
                            THEN 'Not Approved'
                        WHEN 
                            (
                                ISNULL(d.vbrp_balance,0)
                                - ISNULL(ADVSI.si_cash_recieved,0)
                                - ISNULL(ADVSI.si_card_amount,0)
                            ) > 0
                        THEN 'Partial'
                        ELSE 'Closed'
                    END AS status,
                    c.as_id AS customer_id,
                    c.as_name AS customer,
                    r.r_name AS route,
                    gl.gl_name AS branch
                FROM inv_verify_billwise_reciept_inf h
                INNER JOIN inv_verify_billwise_reciept_pur d  ON d.vbrp_vbri_id = h.vbri_id
                INNER JOIN acc_subhead c  ON c.as_id = h.vbri_party_acc
                LEFT JOIN inv_sales_inf SI  ON SI.si_entryno = d.vbrp_billno  AND SI.si_str_id = d.vbrp_form
                LEFT JOIN inv_sales_inf ADVSI ON ADVSI.si_entryno = SI.si_qtn_no AND ADVSI.si_str_id = 12
                LEFT JOIN inv_rout_reg r ON r.r_id = c.as_rout_id
                LEFT JOIN gnl_location gl ON gl.gl_id = h.vbri_location_id          
            WHERE CAST(h.vbri_date AS DATE)
            BETWEEN @fromDate AND @toDate
            AND (@led_id = 0  OR h.vbri_party_acc = @led_id)
            AND (@loc_id = 0  OR h.vbri_location_id = @loc_id)
            AND (@rout_id = 0 OR c.as_rout_id = @rout_id)
            AND (@billNo = '' OR d.vbrp_billno LIKE '%' + @billNo + '%')
            ORDER BY  h.vbri_date DESC, d.vbrp_billno ";

                DataTable dt;
                using (SqlCommand cmd = new SqlCommand(reportSql, usqlre.shop))
                {
                    cmd.Parameters.AddWithValue("@fromDate", fromDate.Date);
                    cmd.Parameters.AddWithValue("@toDate", toDate.Date);
                    cmd.Parameters.AddWithValue("@led_id", led_id);
                    cmd.Parameters.AddWithValue("@loc_id", loc_id);
                    cmd.Parameters.AddWithValue("@rout_id", rout_id);
                    cmd.Parameters.AddWithValue("@billNo", billNo?.Trim() ?? "");
                    dt = await LoadTableAsync(cmd, cancellationToken);
                }
                var collections = new List<object>();
                decimal totalReceived = 0;
                string customerName = "All Accounts";
                string routeName = "All Routes";
                string branchName = "All Branches";
                foreach (DataRow row in dt.Rows)
                {
                    decimal receivedAmount = Convert.ToDecimal(row["vbrp_amount"]);
                    totalReceived += receivedAmount;
                    collections.Add(new
                    {
                        vbriId = Convert.ToInt32(row["vbri_id"]),
                        date = Convert.ToDateTime(row["vbri_date"]),
                        billNo = row["vbrp_billno"].ToString(),
                        billAmount = Convert.ToDecimal(row["vbrp_bill_amount"]),
                        receivedAmount = receivedAmount,
                        balanceAmount = Convert.ToDecimal(row["balance_amount"]),
                        status = row["status"].ToString(),
                        customerId = Convert.ToInt32(row["customer_id"]),
                        customer = row["customer"].ToString(),
                        route = row["route"].ToString(),
                        branch = row["branch"].ToString()
                    });
                }

                if (led_id > 0)
                {
                    DataTable dtCustomer;
                    using (SqlCommand nameCmd = new SqlCommand("SELECT as_name FROM acc_subhead WHERE as_id = @as_id", usqlre.shop))
                    {
                        nameCmd.Parameters.AddWithValue("@as_id", led_id);
                        dtCustomer = await LoadTableAsync(nameCmd, cancellationToken);
                    }
                    if (dtCustomer.Rows.Count > 0)
                        customerName = dtCustomer.Rows[0]["as_name"].ToString();
                }
                if (rout_id > 0)
                {
                    DataTable dtRoute;
                    using (SqlCommand nameCmd = new SqlCommand("SELECT r_name FROM inv_rout_reg WHERE r_id = @r_id", usqlre.shop))
                    {
                        nameCmd.Parameters.AddWithValue("@r_id", rout_id);
                        dtRoute = await LoadTableAsync(nameCmd, cancellationToken);
                    }
                    if (dtRoute.Rows.Count > 0)
                        routeName = dtRoute.Rows[0]["r_name"].ToString();
                }
                else if (collections.Count > 0)
                {
                    routeName = dt.Rows[0]["route"].ToString();
                }
                if (loc_id > 0)
                {
                    DataTable dtBranch;
                    using (SqlCommand nameCmd = new SqlCommand("SELECT gl_name FROM gnl_location WHERE gl_id = @gl_id", usqlre.shop))
                    {
                        nameCmd.Parameters.AddWithValue("@gl_id", loc_id);
                        dtBranch = await LoadTableAsync(nameCmd, cancellationToken);
                    }
                    if (dtBranch.Rows.Count > 0)
                        branchName = dtBranch.Rows[0]["gl_name"].ToString();
                }
                else if (collections.Count > 0)
                {
                    branchName = dt.Rows[0]["branch"].ToString();
                }
                return Ok(new
                {
                    status = true,
                    summary = new
                    {
                        customer = customerName,
                        customerId = led_id > 0 ? led_id : (int?)null,
                        fromDate,
                        toDate,
                        route = routeName,
                        branch = branchName,
                        totalCollections = collections.Count,
                        totalAmount = totalReceived
                    },
                    collections
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.ReleaseConnection();
            }
        }

        [HttpGet("service-outstanding-report")]
        public async Task<IActionResult> ServiceOutstandingReport(DateTime fromDate, DateTime toDate, int led_id = 0, int loc_id = 0, int route_id = 0, bool pendingOnly = false)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;

            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@wi_from_date", fromDate);
                cmd.Parameters.AddWithValue("@wi_to_date", toDate);
                cmd.Parameters.AddWithValue("@led_id", led_id);
                cmd.Parameters.AddWithValue("@loc_id", loc_id);
                cmd.Parameters.AddWithValue("@route_id", route_id);
                cmd.Parameters.AddWithValue("@user_id", 1);
                cmd.Parameters.AddWithValue("@groupwhere", "");
                cmd.Parameters.AddWithValue("@StatementType", "service-outstanding-report");

                DataSet ds = await LoadDataSetAsync(cmd, cancellationToken);

                var workorderList = new List<object>();

                DataTable dtWorkorder = ds.Tables[0];
                DataTable dtComplaint = ds.Tables.Count > 1 ? ds.Tables[1] : null;

                foreach (DataRow row in dtWorkorder.Rows)
                {
                    string billNo = row["BILL NO"].ToString().Trim();
                    int formType = Convert.ToInt32(row["str_id"]);
                    var complaints = new List<object>();
                    if (dtComplaint != null)
                    {
                        var complaintRows = dtComplaint.AsEnumerable().Where(x => x["BillNo"].ToString().Trim() == billNo && Convert.ToInt32(x["str_id"]) == formType);
                        foreach (var c in complaintRows)
                        {
                            complaints.Add(new
                            {
                                complaintName = c["ComplaintName"]?.ToString(),
                                complaintRemarks = c["ComplaintRemarks"]?.ToString()
                            });
                        }
                    }

                    workorderList.Add(new
                    {
                        billNo = billNo,
                        formType = row["str_id"] != DBNull.Value ? Convert.ToInt32(row["str_id"]) : 0,
                        date = row["DATE"] != DBNull.Value ? Convert.ToDateTime(row["DATE"]) : DateTime.MinValue,
                        qtnNo = row["QtnNo"]?.ToString(),
                        customer = row["Customer"]?.ToString(),
                        route = row["Route"]?.ToString(),
                        serviceItemDetails = row["ServiceItemDetails"]?.ToString(),
                        model = row["Model"]?.ToString(),
                        invoicedAmount = row["Invoiced Amount"] != DBNull.Value ? Convert.ToDecimal(row["Invoiced Amount"]) : 0,
                        paidAmount = row["Paid Amount"] != DBNull.Value ? Convert.ToDecimal(row["Paid Amount"]) : 0,
                        pendingBillAmount = row["PENDING BILL AMOUNT"] != DBNull.Value ? Convert.ToDecimal(row["PENDING BILL AMOUNT"]) : 0,
                        unverifiedReceiptAmount = row["UNVERIFIED RECEIPT AMOUNT"] != DBNull.Value ? Convert.ToDecimal(row["UNVERIFIED RECEIPT AMOUNT"]) : 0,
                        ageOfBill = row["Age Of Bill"] != DBNull.Value ? Convert.ToInt32(row["Age Of Bill"]) : 0,
                        salesMan = row["SalesMan"]?.ToString(),
                        dueDays = row["Due Days"] != DBNull.Value ? Convert.ToInt32(row["Due Days"]) : 0,
                        complaint = complaints
                    });
                }

                if (pendingOnly)
                {
                    workorderList = workorderList
                        .Where(x =>
                        {
                            var prop = x.GetType().GetProperty("pendingBillAmount");
                            return prop != null &&
                                   Convert.ToDecimal(prop.GetValue(x)) > 0;
                        })
                        .ToList();
                }

                return Ok(new
                {
                    status = true,
                    workorder = workorderList
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
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
        [HttpPost("technician-loss-report")]
        public async Task<IActionResult> TechnicianLossReport([FromBody] TechnicianLossReportModel model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "technician_loss_report");

                cmd.Parameters.AddWithValue("@as_id", model.technician_id);
                cmd.Parameters.AddWithValue("@si_location_id", model.location_id);
                cmd.Parameters.AddWithValue("@from_date", model.from_date);
                cmd.Parameters.AddWithValue("@to_date", model.to_date);


                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());



                return Content(
                    Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Report generated successfully",
                        data = dt
                    }),
                    "application/json"
                );

            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
            finally
            {
                usqlre?.close();
            }
        }

        [HttpPost("performance-report-form")]
        public IActionResult PerformanceReportForm([FromBody] PerformanceReportModel model)
        {
            UserSqlServer usqlre = null;

            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Invalid payload"
                    });
                }

                usqlre = new UserSqlServer(this);
                usqlre.OpenConnection();

                int technicianId = model.technician_id > 0 ? model.technician_id : model.as_id;
                int routeId = model.rout_id > 0 ? model.rout_id : model.route_id;
                int locationId = model.loc_id > 0 ? model.loc_id : model.location_id;
                string reportStatus = model.status ?? "";

                SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "performance-report-form");
                cmd.Parameters.AddWithValue("@wi_from_date", model.fromDate.Date);
                cmd.Parameters.AddWithValue("@wi_to_date", model.toDate.Date);
                cmd.Parameters.AddWithValue("@as_id", technicianId);
                cmd.Parameters.AddWithValue("@rout_id", routeId);
                cmd.Parameters.AddWithValue("@loc_id", locationId);
                cmd.Parameters.AddWithValue("@status", reportStatus);

                DataTable dt = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }

                var report = new List<object>();
                decimal totalSpareCharge = 0;
                decimal totalServiceCharge = 0;
                decimal totalDiscountAmount = 0;
                decimal totalAmount = 0;
                decimal totalReceivedAmount = 0;

                foreach (DataRow row in dt.Rows)
                {
                    decimal spareCharge = Convert.ToDecimal(row["SpareCharge"]);
                    decimal serviceCharge = Convert.ToDecimal(row["ServiceCharge"]);
                    decimal discountAmount = Convert.ToDecimal(row["DiscountAmount"]);
                    decimal billTotal = Convert.ToDecimal(row["Total"]);
                    decimal receivedAmount = Convert.ToDecimal(row["ReceivedAmount"]);

                    totalSpareCharge += spareCharge;
                    totalServiceCharge += serviceCharge;
                    totalDiscountAmount += discountAmount;
                    totalAmount += billTotal;
                    totalReceivedAmount += receivedAmount;

                    report.Add(new
                    {
                        billNo = row["BillNo"].ToString(),
                        date = Convert.ToDateTime(row["Date"]),
                        spareCharge,
                        serviceCharge,
                        discountAmount,
                        total = billTotal,
                        receivedAmount,
                        paymentStatus = row["PaymentStatus"].ToString()
                    });
                }

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Performance report fetched successfully",
                    summary = new
                    {
                        technicianId,
                        routeId,
                        locationId,
                        selectedStatus = reportStatus,
                        fromDate = model.fromDate,
                        toDate = model.toDate,
                        totalBills = report.Count,
                        totalSpareCharge,
                        totalServiceCharge,
                        totalDiscountAmount,
                        totalAmount,
                        totalReceivedAmount
                    },
                    data = report
                });
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message
                });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }
        }


        [HttpGet("service-complaint-dashboard")]
        public async Task<IActionResult> GetServiceComplaintDashboard(DateTime? fromDate, DateTime? toDate, int? locationId)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                DateTime today = DateTime.Today;
                int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
                DateTime weekStart = today.AddDays(-daysFromMonday);
                DateTime weekEnd = weekStart.AddDays(6);

                DateTime filterFrom = fromDate?.Date ?? weekStart;
                DateTime filterTo = toDate?.Date ?? weekEnd;
                int locId = locationId ?? Convert.ToInt32(usqlre.locationId);

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "getServiceComplaintDashboard");
                cmd.Parameters.AddWithValue("@from_date", filterFrom);
                cmd.Parameters.AddWithValue("@to_date", filterTo);
                cmd.Parameters.AddWithValue("@si_location_id", locId);

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);

                int unassigned = 0, assigned = 0, inProgress = 0, hold = 0, notOk = 0;
                int awaitingQc = 0, qcRejected = 0, readyForDelivery = 0, deliveredToday = 0;
                string locationName = "";

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    unassigned = Convert.ToInt32(row["unassigned"]);
                    assigned = Convert.ToInt32(row["assigned"]);
                    inProgress = Convert.ToInt32(row["inProgress"]);
                    hold = Convert.ToInt32(row["hold"]);
                    notOk = Convert.ToInt32(row["notOk"]);
                    awaitingQc = Convert.ToInt32(row["awaitingQc"]);
                    qcRejected = Convert.ToInt32(row["qcRejected"]);
                    readyForDelivery = Convert.ToInt32(row["readyForDelivery"]);
                    deliveredToday = Convert.ToInt32(row["deliveredToday"]);
                    locationName = row["locationName"] == DBNull.Value ? "" : row["locationName"].ToString();
                }

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Service Complaint dashboard fetched successfully",
                    data = new
                    {
                        fromDate = filterFrom.ToString("yyyy-MM-dd"),
                        toDate = filterTo.ToString("yyyy-MM-dd"),
                        locationId = locId,
                        locationName,
                        unassigned,
                        assigned,
                        inProgress,
                        hold,
                        notOk,
                        awaitingQc,
                        qcRejected,
                        readyForDelivery,
                        deliveredToday
                    }
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }

        [HttpGet("collections-loss-dashboard")]
        public async Task<IActionResult> GetCollectionsLossDashboard(DateTime? fromDate, DateTime? toDate, int? locationId)
        {
            var cancellationToken = HttpContext.RequestAborted;
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                if (!await usqlre.OpenConnectionAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StatusCode(500, new
                    {
                        status = false,
                        statusCode = 500,
                        message = "Database connection failed: " + usqlre.lastError,
                        data = (object)null
                    });
                }

                DateTime filterFrom = fromDate?.Date ?? DateTime.Today;
                DateTime filterTo = toDate?.Date ?? DateTime.Today;
                int locId = locationId ?? Convert.ToInt32(usqlre.locationId);

                using SqlCommand cmd = new SqlCommand("Sp_Service_Complaint_App", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "getCollectionsLossDashboard");
                cmd.Parameters.AddWithValue("@from_date", filterFrom);
                cmd.Parameters.AddWithValue("@to_date", filterTo);
                cmd.Parameters.AddWithValue("@si_location_id", locId);

                DataTable dt = await LoadTableAsync(cmd, cancellationToken);

                decimal invoicedAmount = 0, collectedAmount = 0, outstandingAmount = 0, technicianLoss = 0;
                string locationName = "";

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    invoicedAmount = row["invoicedAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["invoicedAmount"]);
                    collectedAmount = row["collectedAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["collectedAmount"]);
                    outstandingAmount = row["outstandingAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["outstandingAmount"]);
                    technicianLoss = row["technicianLoss"] == DBNull.Value ? 0 : Convert.ToDecimal(row["technicianLoss"]);
                    locationName = row["locationName"] == DBNull.Value ? "" : row["locationName"].ToString();
                }

                var responseObj = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Collections and loss dashboard fetched successfully",
                    data = new
                    {
                        fromDate = filterFrom.ToString("yyyy-MM-dd"),
                        toDate = filterTo.ToString("yyyy-MM-dd"),
                        locationId = locId,
                        locationName,
                        invoicedAmount,
                        collectedAmount,
                        outstandingAmount,
                        technicianLoss
                    }
                };

                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (SqlException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(422, new
                {
                    status = false,
                    statusCode = 422,
                    message = sqlEx.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
            finally
            {
                usqlre?.ReleaseConnection();
            }
        }
    }
}