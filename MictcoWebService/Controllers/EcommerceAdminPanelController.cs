using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class EcommerceAdminPanelController : Controller
    {
        [HttpPost("create-ecommerce-user")]
        public async Task<IActionResult> Create([FromForm] EcommerceUserCreate model, IFormFile profileImage)
        {
            try
            {
                string relativePath = null;

                UserSqlServer usqlre = new UserSqlServer(this);

                // ✅ Get Base URL from DB
                string baseUrl;
                using (SqlCommand cmdBase = new SqlCommand("SELECT TOP 1 ans_status FROM android_settings WHERE ans_name = 'BaseUrl'", usqlre.shop))
                {
                    baseUrl = (string)cmdBase.ExecuteScalar();
                }
                // ✅ Handle profile image upload
                if (profileImage != null && profileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "users");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(profileImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }

                    // ✅ Store only relative path in DB
                    relativePath = "/uploads/users/" + fileName;
                }



                // ✅ Insert user
                SqlCommand cmd = new SqlCommand("Sp_EcommerceLogin", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "Insert");
                cmd.Parameters.AddWithValue("@gel_login_id", model.gel_login_id);
                cmd.Parameters.AddWithValue("@gel_role", model.gel_role);
                cmd.Parameters.AddWithValue("@gel_ledger_id", model.gel_ledger_id);
                cmd.Parameters.AddWithValue("@gel_created_by", usqlre.userId);
                cmd.Parameters.AddWithValue("@gel_profile_image", (object)relativePath ?? "");

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                usqlre.close();

                // ✅ Append BaseUrl in response
                var data = dt.Rows.Count > 0
                    ? dt.Rows[0].Table.Columns.Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName,
                            col => col.ColumnName == "gel_profile_image" && dt.Rows[0][col] != ""
                                ? baseUrl.TrimEnd('/') + dt.Rows[0][col].ToString()
                                : dt.Rows[0][col])
                    : null;

                var responseData = new
                {
                    status = true,
                    statusCode = 200,
                    message = "User Created Successfully",
                    data = data
                };

                string jsonResult = ReportModelContext.searializeDt(responseData);
                return Content(jsonResult, "application/json");
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Database error: " + ex.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "General error: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpPost("update-ecommerce-user")]
        public async Task<IActionResult> Update([FromForm] EcommerceUserUpdate model, IFormFile profileImage)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                // ✅ Get Base URL from DB
                string baseUrl;
                using (SqlCommand cmdBase = new SqlCommand("SELECT TOP 1 ans_status FROM android_settings WHERE ans_name = 'BaseUrl'", usqlre.shop))
                {
                    baseUrl = (string)cmdBase.ExecuteScalar();
                }

                // ✅ Handle profile image upload
                string relativePath = null;
                if (profileImage != null && profileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "users");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(profileImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }

                    // ✅ Save relative path in DB
                    relativePath = "/uploads/users/" + fileName;
                }

                // ✅ Update user via stored procedure
                SqlCommand cmd = new SqlCommand("Sp_EcommerceLogin", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "Update");
                cmd.Parameters.AddWithValue("@gel_id", model.gel_id);
                cmd.Parameters.AddWithValue("@gel_login_id", model.gel_login_id);
                cmd.Parameters.AddWithValue("@gel_role", model.gel_role);
                cmd.Parameters.AddWithValue("@gel_ledger_id", model.gel_ledger_id);
                cmd.Parameters.AddWithValue("@gel_updated_by", usqlre.userId);
                cmd.Parameters.AddWithValue("@gel_active", model.gel_active);
                cmd.Parameters.AddWithValue("@gel_profile_image", (object?)relativePath ?? "");

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                usqlre.close();

                // ✅ Build response with full image URL
                var data = dt.Rows.Count > 0
                    ? dt.Rows[0].Table.Columns.Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName,
                            col => col.ColumnName == "gel_profile_image" && dt.Rows[0][col] != DBNull.Value && dt.Rows[0][col].ToString() != ""
                                ? baseUrl.TrimEnd('/') + dt.Rows[0][col].ToString()
                                : dt.Rows[0][col])
                    : null;

                var response = new
                {
                    status = true,
                    statusCode = 200,
                    message = "User Updated Successfully",
                    data = data
                };

                string jsonResult = ReportModelContext.searializeDt(response);
                return Content(jsonResult, "application/json");
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Database error: " + ex.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "General error: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpPost("delete-ecommerce-user/{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                SqlCommand cmd = new SqlCommand("Sp_EcommerceLogin", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", "Delete");
                cmd.Parameters.AddWithValue("@gel_id", id);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                usqlre.close();

                var response = new
                {
                    status = true,
                    statusCode = 200,
                    message = "User deleted successfully.",
                    data = dt.Rows.Count > 0 ? dt.Rows[0].Table.Columns.Cast<DataColumn>()
                    .ToDictionary(col => col.ColumnName, col => dt.Rows[0][col]) : null
                };

                string jsonResult = ReportModelContext.searializeDt(response);
                return Content(jsonResult, "application/json");
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Database error: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpGet("get-ecommerce-user/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                // ✅ Get BaseUrl from settings
                string baseUrl;
                using (SqlCommand cmdBase = new SqlCommand("SELECT TOP 1 ans_status FROM android_settings WHERE ans_name = 'BaseUrl'", usqlre.shop))
                {
                    baseUrl = (string)cmdBase.ExecuteScalar();
                }

                // ✅ Execute stored procedure
                SqlCommand cmd = new SqlCommand("Sp_EcommerceLogin", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "GetById");
                cmd.Parameters.AddWithValue("@gel_id", id);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                usqlre.close();

                // ✅ Build response
                object? data = null;
                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var dict = dt.Columns.Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col]);

                    // ✅ Prepend BaseUrl for profile image
                    if (dict.ContainsKey("gel_profile_image") && dict["gel_profile_image"] != DBNull.Value)
                    {
                        string relativePath = dict["gel_profile_image"].ToString();
                        if (!string.IsNullOrEmpty(relativePath))
                        {
                            dict["gel_profile_image"] = baseUrl.TrimEnd('/') + relativePath;
                        }
                    }

                    data = dict;
                }

                var response = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Get User Successfully",
                    data = data
                };

                string jsonResult = ReportModelContext.searializeDt(response);
                return Content(jsonResult, "application/json");
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Database error: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpGet("get-all-ecommerce-users")]
        public IActionResult GetAll()
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                // ✅ Get BaseUrl from settings
                string baseUrl;
                using (SqlCommand cmdBase = new SqlCommand("SELECT TOP 1 ans_status FROM android_settings WHERE ans_name = 'BaseUrl'", usqlre.shop))
                {
                    baseUrl = (string)cmdBase.ExecuteScalar();
                }

                // ✅ Execute stored procedure
                SqlCommand cmd = new SqlCommand("Sp_EcommerceLogin", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "Get");

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                usqlre.close();

                // ✅ Fix gel_profile_image for each row
                foreach (DataRow row in dt.Rows)
                {
                    if (row["gel_profile_image"] != DBNull.Value)
                    {
                        string relativePath = row["gel_profile_image"].ToString();
                        if (!string.IsNullOrEmpty(relativePath))
                        {
                            row["gel_profile_image"] = baseUrl.TrimEnd('/') + relativePath;
                        }
                    }
                }

                var response = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetched all users successfully.",
                    data = dt
                };

                string jsonResult = ReportModelContext.searializeDt(response);
                return Content(jsonResult, "application/json");
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Database error: " + ex.Message,
                    data = new object[0]
                });
            }
        }

        // images
        [HttpPost("images")]
        public async Task<IActionResult> ManageImages([FromForm] ImageUploadRequest request, [FromQuery] string action, IFormFile imageFile)
        {
            try
            {
                if (string.IsNullOrEmpty(action))
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Action parameter is required (Insert / Update / Delete / Get).",
                        data = (object)null
                    });
                }

                UserSqlServer usqlre = new UserSqlServer(this);

                // ✅ Get Base URL from DB
                string baseUrl;
                using (SqlCommand cmdBase = new SqlCommand("SELECT TOP 1 ans_status FROM android_settings WHERE ans_name = 'BaseUrl'", usqlre.shop))
                {
                    baseUrl = (string)cmdBase.ExecuteScalar();
                }

                string relativePath = null;

                // ✅ Handle upload only if Insert or Update + file is present
                if ((action.Equals("Insert", StringComparison.OrdinalIgnoreCase) ||
                     action.Equals("Update", StringComparison.OrdinalIgnoreCase))
                    && imageFile != null && imageFile.Length > 0)
                {
                    // 👇 Use ReferenceType for folder
                    string folderName = request.ReferenceType ?? "general";
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folderName);

                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // ✅ Store only relative path in DB
                    relativePath = $"/uploads/{folderName}/{fileName}";
                }

                // ✅ Call your SP
                SqlCommand cmd = new SqlCommand("Sp_inv_images", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", action);
                cmd.Parameters.AddWithValue("@ii_id", request.ImageId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ii_reference_type", request.ReferenceType ?? "");
                cmd.Parameters.AddWithValue("@ii_reference_id", request.ReferenceId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ii_image", (object)relativePath ?? imageFile ?? (object)DBNull.Value);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                usqlre.shop.Close();

                // ✅ Build response with BaseUrl
                var data = dt.Rows.Count > 0
                    ? dt.AsEnumerable().Select(row =>
                    {
                        var dict = dt.Columns.Cast<DataColumn>()
                            .ToDictionary(col => col.ColumnName, col => row[col]);

                        if (dict.ContainsKey("Image") && dict["Image"] != DBNull.Value)
                        {
                            string rel = dict["Image"].ToString();
                            if (!string.IsNullOrEmpty(rel))
                                dict["Image"] = baseUrl.TrimEnd('/') + rel;
                        }

                        return dict;
                    }).ToList()
                    : null;

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = $"Image action '{action}' executed successfully",
                    data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Server Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        //[HttpPost("images/save-grouped")]
        //public IActionResult SaveGroupedImages([FromBody] List<ImageGroupRequest> request,[FromQuery] string referenceType)
        //{
        //    try
        //    {
        //        if (request == null || request.Count == 0)
        //        {
        //            return BadRequest(new
        //            {
        //                status = false,
        //                message = "Request body is empty"
        //            });
        //        }

        //        UserSqlServer usqlre = new UserSqlServer(this);

        //        foreach (var group in request)
        //        {
        //            foreach (var img in group.Images)
        //            {
        //                SqlCommand cmd = new SqlCommand("Sp_inv_images", usqlre.shop);
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@StatementType", "INSERT");
        //                cmd.Parameters.AddWithValue("@ii_reference_type", referenceType);
        //                cmd.Parameters.AddWithValue("@ii_reference_id", group.ReferenceId);
        //                cmd.Parameters.AddWithValue("@ii_image", img.Image);
        //                cmd.Parameters.AddWithValue("@ii_redirect_url", img.RedirectUrl ?? "");

        //                cmd.ExecuteNonQuery();
        //            }
        //        }

        //        usqlre.shop.Close();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Grouped images saved successfully"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            message = ex.Message
        //        });
        //    }
        //}

        [HttpPost("images/bulk")]
        public async Task<IActionResult> InsertImagesBulk([FromForm] List<BulkImageFormRequest> model)
        {
            if (model == null || model.Count == 0)
                return BadRequest("No images found");

            UserSqlServer usqlre = new UserSqlServer(this);

            foreach (var item in model)
            {
                if (item.Image == null || item.Image.Length == 0)
                    continue;

                string folder =  "banner";

                string uploadPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot", "uploads", folder);

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                string fileName = Guid.NewGuid() + Path.GetExtension(item.Image.FileName);
                string filePath = Path.Combine(uploadPath, fileName);

                using (var fs = new FileStream(filePath, FileMode.Create))
                {
                    await item.Image.CopyToAsync(fs);
                }

                string relativePath = $"/uploads/{folder}/{fileName}";

                using SqlCommand cmd = new SqlCommand("Sp_inv_images", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StatementType", "Insert");
                cmd.Parameters.AddWithValue("@ii_reference_type", folder);
                cmd.Parameters.AddWithValue("@ii_reference_id", item.ReferenceId);
                cmd.Parameters.AddWithValue("@ii_image", relativePath);
                cmd.Parameters.AddWithValue("@ii_redirect_url", item.RedirectUrl ?? "");
                cmd.ExecuteNonQuery();
            }

            usqlre.shop.Close();

            return Ok(new
            {
                status = true,
                message = "Images uploaded successfully"
            });
        }



        [HttpPost("section-master")]
        public IActionResult ManageSectionMaster([FromBody] SectionMasterModel model, [FromQuery] string action)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                SqlCommand cmd = new SqlCommand("sp_SectionManagement", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", action);
                cmd.Parameters.AddWithValue("@sm_id", model.sm_id);
                cmd.Parameters.AddWithValue("@sm_name", model.sm_name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@sm_order", model.sm_order);
                cmd.Parameters.AddWithValue("@sm_created_by", usqlre.userId);
                cmd.Parameters.AddWithValue("@sm_updated_by", usqlre.userId);


                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                usqlre.shop.Close();

                object data = null;

                if (dt.Rows.Count == 1)
                {
                    data = dt.Columns.Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => dt.Rows[0][col]);
                }
                else if (dt.Rows.Count > 1)
                {
                    data = dt.AsEnumerable().Select(row => dt.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col])).ToList();
                }
                else
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Section not created or no data found",
                        data = (object)null
                    });
                }
                var response = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Section Master processed successfully",
                    data = data
                };

                string jsonResult = ReportModelContext.searializeDt(response);
                return Content(jsonResult, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Server Error: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpPost("section-item")]
        public IActionResult ManageSectionItem([FromBody] SectionItemModel model, [FromQuery] string action)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                SqlCommand cmd = new SqlCommand("sp_SectionManagement", usqlre.shop);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@StatementType", action);
                cmd.Parameters.AddWithValue("@si_id", model.si_id);
                cmd.Parameters.AddWithValue("@si_section_id", model.si_section_id);
                cmd.Parameters.AddWithValue("@si_item_id", model.si_item_id);
                cmd.Parameters.AddWithValue("@si_created_by", usqlre.userId);
                cmd.Parameters.AddWithValue("@si_updated_by", usqlre.userId);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                usqlre.shop.Close();
                object data = null;

                if (dt.Rows.Count == 1)
                {
                    data = dt.Columns.Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => dt.Rows[0][col]);
                }
                else if (dt.Rows.Count > 1)
                {
                    data = dt.AsEnumerable().Select(row => dt.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col])).ToList();
                }
                else
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Section Item not created or no data found",
                        data = (object)null
                    });
                }
                var response = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Section Item processed successfully",
                    data = data
                };

                string jsonResult = ReportModelContext.searializeDt(response);
                return Content(jsonResult, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Server Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
    }
}
