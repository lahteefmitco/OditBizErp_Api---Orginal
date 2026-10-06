using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Hubs;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    [ApiController]
   // [Authorize(Roles = UserRoles.EcommerceUser)]

    public class EcommerceController : Controller
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<EcommerceHub> _hubContext;

        public EcommerceController(IConfiguration configuration, IHubContext<EcommerceHub> hubContext)
        {
            _configuration = configuration;
            _connectionString = SqlConnectionPool.Apply(configuration.GetConnectionString("ConnStr"));
            _hubContext = hubContext;
        }
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("category-list")]
        public IActionResult GetCategories(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                // Count total categories
                string countSql = "SELECT COUNT(*) FROM inv_category";
                int totalRecords = Convert.ToInt32(ExecuteScalar(_connectionString, countSql));

                if (totalRecords == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "No categories found",
                        data = (object)null
                    });
                }

                int offset = (pageNumber - 1) * pageSize;

                // Step 1: Get paged categories only
                string categorySql = $@"SELECT c_id AS Id, c_name AS Name FROM inv_category  ORDER BY c_name OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                DataTable categoryTable = ExecuteDataTable(_connectionString, categorySql);

                // Get list of category IDs
                var categoryIds = categoryTable.AsEnumerable()
                    .Select(r => Convert.ToInt32(r["Id"]))
                    .ToList();

                if (!categoryIds.Any())
                {
                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Categories fetched successfully",
                        data = new
                        {
                            pagination = new
                            {
                                page = pageNumber,
                                pageSize = pageSize,
                                totalRecords = totalRecords,
                                totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                            },
                            categories = new List<object>()
                        }
                    });
                }

                // Step 2: Get all images for these category IDs
                string joinedIds = string.Join(",", categoryIds);
                string imageSql = $@"SELECT ii_reference_id AS CategoryId,
                ans.ans_status + ii_image AS Image
                FROM inv_images img
                CROSS JOIN android_settings ans
                WHERE ans.ans_name = 'BaseUrl'
                AND ii_reference_type = 'category' AND ii_reference_id IN ({joinedIds})";

                DataTable imageTable = ExecuteDataTable(_connectionString, imageSql);

                // Step 3: Join category info with images
                var categories = categoryTable.AsEnumerable()
                    .Select(row => new
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Name = row["Name"].ToString(),
                        Images = imageTable.AsEnumerable()
                            .Where(img => Convert.ToInt32(img["CategoryId"]) == Convert.ToInt32(row["Id"]))
                            .Select(img => img["Image"].ToString())
                            .Where(img => !string.IsNullOrWhiteSpace(img))
                            .Distinct()
                            .ToList()
                    }).ToList();

                var response = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Categories fetched successfully",
                    data = new
                    {
                        pagination = new
                        {
                            page = pageNumber,
                            pageSize = pageSize,
                            totalRecords = totalRecords,
                            totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                        },
                        categories = categories
                    }
                };

                return Ok(response);
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("banner-list")]
        public IActionResult GetTwoTypeBanners()
        {
            try
            {
                string sql = @" SELECT ib_id AS Id,
                ib_type AS Type,
                ans.ans_status + ii_image AS Image
                FROM inv_banner b
                LEFT JOIN inv_images img ON img.ii_reference_id = b.ib_id AND img.ii_reference_type = 'banner'
                CROSS JOIN android_settings ans
                WHERE ans.ans_name = 'BaseUrl' AND ib_type IN ('Banner-1', 'Banner-2')";

                DataTable dt = ExecuteDataTable(_connectionString, sql);

                if (dt == null || dt.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "No banners found",
                        data = (object)null
                    });
                }

                var banner1List = dt.AsEnumerable()
                    .Where(r => r["Type"].ToString() == "Banner-1")
                    .GroupBy(r => Convert.ToInt32(r["Id"]))
                    .Select(g => new
                    {
                        id = g.Key,
                        images = g
                            .Where(r => r["Image"] != DBNull.Value)
                            .Select(r => r["Image"].ToString())
                            .Distinct()
                            .ToList()
                    }).ToList();

                var banner2List = dt.AsEnumerable()
                    .Where(r => r["Type"].ToString() == "Banner-2")
                    .GroupBy(r => Convert.ToInt32(r["Id"]))
                    .Select(g => new
                    {
                        id = g.Key,
                        images = g
                            .Where(r => r["Image"] != DBNull.Value)
                            .Select(r => r["Image"].ToString())
                            .Distinct()
                            .ToList()
                    }).ToList();

                var response = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Banner fetched successfully",
                    data = new
                    {
                        Banner1 = banner1List,
                        Banner2 = banner2List
                    }
                };

                return Ok(response);
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("banner-list-with-url")]
        public IActionResult GetTwoTypeBannersWithUrl()
        {
            try
            {
                string sql = @"
        SELECT 
            b.ib_id AS Id,
            b.ib_type AS Type,
            ans.ans_status + img.ii_image AS ImageUrl,
            img.ii_redirect_url AS RedirectUrl
        FROM inv_banner b
        LEFT JOIN inv_images img 
            ON img.ii_reference_id = b.ib_id 
           AND img.ii_reference_type = 'banner'
        CROSS JOIN android_settings ans
        WHERE ans.ans_name = 'BaseUrl'
          AND b.ib_type IN ('Banner-1', 'Banner-2')";

                DataTable dt = ExecuteDataTable(_connectionString, sql);

                if (dt == null || dt.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "No banners found",
                        data = (object)null
                    });
                }

                var banner1List = dt.AsEnumerable()
                    .Where(r => r["Type"].ToString() == "Banner-1")
                    .GroupBy(r => Convert.ToInt32(r["Id"]))
                    .Select(g => new
                    {
                        id = g.Key,
                        items = g
                            .Where(r => r["ImageUrl"] != DBNull.Value)
                            .Select(r => new
                            {
                                imageUrl = r["ImageUrl"].ToString(),
                                redirectUrl = r["RedirectUrl"] == DBNull.Value
                                    ? null
                                    : r["RedirectUrl"].ToString()
                            })
                            .ToList()
                    }).ToList();

                var banner2List = dt.AsEnumerable()
                    .Where(r => r["Type"].ToString() == "Banner-2")
                    .GroupBy(r => Convert.ToInt32(r["Id"]))
                    .Select(g => new
                    {
                        id = g.Key,
                        items = g
                            .Where(r => r["ImageUrl"] != DBNull.Value)
                            .Select(r => new
                            {
                                imageUrl = r["ImageUrl"].ToString(),
                                redirectUrl = r["RedirectUrl"] == DBNull.Value
                                    ? null
                                    : r["RedirectUrl"].ToString()
                            })
                            .ToList()
                    }).ToList();

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Banner fetched successfully",
                    data = new
                    {
                        Banner1 = banner1List,
                        Banner2 = banner2List
                    }
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

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("section-wise-items/{sectionName}")]
        public IActionResult GetSections(string sectionName, int pageNumber = 1, int pageSize = 1000)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Unauthorized: Invalid token.",
                        data = (object)null
                    });
                }

                if (pageNumber <= 0) pageNumber = 1;
                //if (pageSize <= 0) pageSize = 1000;
                pageSize = 1000;


                // Step 1: Total count of distinct items in the section
                string countSql = $@"SELECT COUNT(DISTINCT View_Stock.ir_id) FROM inv_section_master LEFT JOIN inv_section_item ON sm_id = si_section_id  LEFT JOIN View_Stock ON ir_id = si_item_id INNER JOIN inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id WHERE location_id=1 and ir_active = 1 and sm_name = '{sectionName}'";

                int totalRecords = Convert.ToInt32(ExecuteScalar(_connectionString, countSql));

                if (totalRecords == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "No Section found",
                        data = (object)null
                    });
                }

                int offset = (pageNumber - 1) * pageSize;

                // Step 2: Get paged item IDs
                string itemIdsSql = $@" SELECT DISTINCT View_Stock.ir_id FROM inv_section_master   LEFT JOIN inv_section_item ON sm_id = si_section_id  LEFT JOIN View_Stock ON ir_id = si_item_id INNER JOIN inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id WHERE sm_name = '{sectionName}' and ir_active = 1  ORDER BY ir_id OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                DataTable itemIdsTable = ExecuteDataTable(_connectionString, itemIdsSql);
                var itemIds = itemIdsTable.AsEnumerable()
                    .Select(r => Convert.ToInt32(r["ir_id"]))
                    .ToList();

                if (!itemIds.Any())
                {
                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Section fetched successfully",
                        data = new
                        {
                            pagination = new
                            {
                                page = pageNumber,
                                pageSize = pageSize,
                                totalRecords = totalRecords,
                                totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                            },
                            sections = new List<object>()
                        }
                    });
                }

                string joinedIds = string.Join(",", itemIds);

                // Step 3: Fetch full item details + images + wishlist flag
                string detailsSql = $@"
                SELECT  vs.ir_id AS ir_id,
                        ir.ir_name AS ir_name,
                        vs.mrp AS mrp,
                        vs.qty as stock,
                        ISNULL(p.pending_qty, 0) AS pending_qty,
                        (vs.qty - ISNULL(p.pending_qty, 0)) AS available_qty,
                        ans.ans_status + ii_image AS ii_image,
                        CASE WHEN iw.iw_product_id IS NOT NULL THEN 1 ELSE 0 END AS is_in_wishlist
                FROM View_Stock vs
                INNER JOIN inv_item_reg ir ON ir.ir_id = vs.ir_id
                LEFT JOIN inv_images img ON img.ii_reference_id = vs.ir_id AND img.ii_reference_type = 'item'
                LEFT JOIN inv_wishlist iw ON iw.iw_product_id = vs.ir_id AND iw.iw_user_id = {userId}
                LEFT JOIN (SELECT sp_ir_id,SUM(sp_qty) AS pending_qty FROM inv_sales_par WHERE sp_str_id=3 and sp_narration1 in ('pending','accepted') GROUP BY sp_ir_id) p ON p.sp_ir_id = vs.ir_id
                CROSS JOIN android_settings ans
                WHERE ans.ans_name = 'BaseUrl'
                  AND ir.ir_active = 1
                  AND vs.location_id = 1
                  AND vs.ir_id IN ({joinedIds})";
                DataTable dt = ExecuteDataTable(_connectionString, detailsSql);

                // Step 4: Group by item, include images
                var sections = dt.AsEnumerable()
                    .GroupBy(r => new
                    {
                        Id = Convert.ToInt32(r["ir_id"]),
                        Name = r["ir_name"].ToString(),
                        Mrp = Convert.ToDecimal(r["mrp"]),
                        Stock = Convert.ToInt32(r["stock"]),
                        available_qty = Convert.ToInt32(r["available_qty"]),
                        IsInWishlist = Convert.ToInt32(r["is_in_wishlist"])
                    })
                    .Select(g => new
                    {
                        id = g.Key.Id,
                        name = g.Key.Name,
                        mrp = g.Key.Mrp,
                        stock = g.Key.Stock,
                        available_qty = g.Key.available_qty,
                        is_in_wishlist = g.Key.IsInWishlist,
                        images = g.Where(x => !string.IsNullOrWhiteSpace(x["ii_image"].ToString()))
                                  .Select(x => x["ii_image"].ToString())
                                  .Distinct()
                                  .ToList()
                    }).ToList();

                var response = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Section fetched successfully",
                    data = new
                    {
                        pagination = new
                        {
                            page = pageNumber,
                            pageSize = pageSize,
                            totalRecords = totalRecords,
                            totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                        },
                        sections = sections
                    }
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
                    message = "Error: " + ex.Message,
                    data = (object)null
                });
            }
        }
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("ecommerce-search-category/{name}")]
        public IActionResult SearchCategoryByName(string name)
        {
            try
            {
                // Build WHERE clause dynamically
                List<string> conditions = new List<string>();
                List<SqlParameter> parameters = new List<SqlParameter>();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    string[] keywords = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < keywords.Length; i++)
                    {
                        string paramName = $"@word{i}";
                        conditions.Add($"c.c_name LIKE {paramName}");
                        parameters.Add(new SqlParameter(paramName, $"%{keywords[i]}%"));
                    }
                    // Add main param for sorting
                    parameters.Add(new SqlParameter("@searchTerm", $"%{name}%"));
                    parameters.Add(new SqlParameter("@searchTermExact", $"{name}%"));
                }

                string whereClause = conditions.Count > 0 ? "WHERE ans.ans_name = 'BaseUrl' and " + string.Join(" AND ", conditions) : "";

                string query = $@"
                SELECT c.c_id AS Id, 
                       c.c_name AS Name,  
                       ans.ans_status + img.ii_image AS Image
                FROM inv_category c
                LEFT JOIN inv_images img 
                       ON img.ii_reference_id = c.c_id 
                      AND img.ii_reference_type = 'category'
                CROSS JOIN android_settings ans
                
                {whereClause}
                ORDER BY CASE  
                           WHEN c.c_name LIKE @searchTermExact THEN 1  
                           WHEN c.c_name LIKE @searchTerm THEN 2  
                           ELSE 3  
                         END, 
                         c.c_name ASC";

                DataTable dt = ExecuteDataTable(_connectionString, query, parameters.ToArray());

                // Group categories and collect images
                var categories = dt.AsEnumerable()
                    .GroupBy(row => new
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Name = row["Name"].ToString()
                    })
                    .Select(g => new
                    {
                        id = g.Key.Id,
                        name = g.Key.Name,
                        images = g.Where(x => x["Image"] != DBNull.Value && !string.IsNullOrWhiteSpace(x["Image"].ToString()))
                                  .Select(x => x["Image"].ToString())
                                  .Distinct()
                                  .ToList()
                    }).ToList();

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Category search results",
                    data = categories
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

        //[Authorize(Roles = UserRoles.EcommerceUser)]
        //[HttpGet("ecommerce-search-products/{name}")]
        //public IActionResult SearchProducts(string name, int? categoryId)
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        if (string.IsNullOrEmpty(userId))
        //        {
        //            return Unauthorized(new
        //            {
        //                status = false,
        //                statusCode = 401,
        //                message = "Unauthorized: Invalid token.",
        //                data = (object)null
        //            });
        //        }

        //        // Build WHERE clause dynamically
        //        List<string> conditions = new List<string>();
        //        List<SqlParameter> parameters = new List<SqlParameter>
        //        {
        //            new SqlParameter("@userId", userId)
        //        };

        //        if (!string.IsNullOrWhiteSpace(name))
        //        {
        //            string[] keywords = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //            for (int i = 0; i < keywords.Length; i++)
        //            {
        //                string paramName = $"@word{i}";
        //                conditions.Add($"ir.ir_name LIKE {paramName}");
        //                parameters.Add(new SqlParameter(paramName, $"%{keywords[i]}%"));
        //            }
        //            // For ordering by relevance
        //            parameters.Add(new SqlParameter("@searchTerm", $"%{name}%"));
        //            parameters.Add(new SqlParameter("@searchTermExact", $"{name}%"));
        //        }

        //        if (categoryId.HasValue && categoryId.Value > 0)
        //        {
        //            conditions.Add("ir.ir_category_id = @categoryId");
        //            parameters.Add(new SqlParameter("@categoryId", categoryId.Value));
        //        }

        //        string whereClause = conditions.Count > 0 ? "WHERE ans.ans_name = 'BaseUrl' and location_id=1 and ir_active = 1 and " + string.Join(" AND ", conditions) : "";

        //        string query = $@"
        //        SELECT 
        //            vs.ir_id AS ir_id,
        //            ir.ir_name AS ir_name,
        //            vs.mrp AS mrp,
        //            ans.ans_status + img.ii_image AS Image,
        //            CASE 
        //                WHEN wl.iw_product_id IS NOT NULL THEN 1 
        //                ELSE 0 
        //            END AS is_in_wishlist
        //        FROM View_Stock vs
        //        INNER JOIN inv_item_reg ir ON ir.ir_id = vs.ir_id 
        //        LEFT JOIN inv_images img 
        //               ON img.ii_reference_id = vs.ir_id 
        //              AND img.ii_reference_type = 'item'
        //        LEFT JOIN inv_wishlist wl 
        //               ON wl.iw_product_id = vs.ir_id 
        //              AND wl.iw_user_id = @userId
        //        CROSS JOIN android_settings ans

        //        {whereClause}
        //        ORDER BY 
        //            CASE 
        //                WHEN ir.ir_name LIKE @searchTermExact THEN 1
        //                WHEN ir.ir_name LIKE @searchTerm THEN 2
        //                ELSE 3
        //            END,
        //            ir.ir_name ASC";

        //        DataTable dt = ExecuteDataTable(_connectionString, query, parameters.ToArray());

        //        // Group products to get multiple images in a list
        //        var products = dt.AsEnumerable()
        //            .GroupBy(row => new
        //            {
        //                Id = Convert.ToInt32(row["ir_id"]),
        //                Name = row["ir_name"].ToString(),
        //                Mrp = row["mrp"] != DBNull.Value ? Convert.ToDecimal(row["mrp"]) : 0,
        //                IsInWishlist = Convert.ToInt32(row["is_in_wishlist"])
        //            })
        //            .Select(g => new
        //            {
        //                id = g.Key.Id,
        //                name = g.Key.Name,
        //                mrp = g.Key.Mrp,
        //                is_in_wishlist = g.Key.IsInWishlist,
        //                images = g.Where(x => x["Image"] != DBNull.Value && !string.IsNullOrWhiteSpace(x["Image"].ToString()))
        //                          .Select(x => x["Image"].ToString())
        //                          .Distinct()
        //                          .ToList()
        //            }).ToList();

        //        return Ok(new
        //        {
        //            status = true,
        //            statusCode = 200,
        //            message = "Search results",
        //            data = products
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = "Error: " + ex.Message,
        //            data = (object)null
        //        });
        //    }
        //}
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("ecommerce-search-products/{name}")]
        public IActionResult SearchProducts(string name, int? categoryId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Unauthorized: Invalid token.",
                        data = (object)null
                    });
                }

                // Build WHERE clause dynamically
                List<string> conditions = new List<string>();
                List<SqlParameter> parameters = new List<SqlParameter>
        {
            new SqlParameter("@userId", userId)
        };

                if (!string.IsNullOrWhiteSpace(name))
                {
                    string[] keywords = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    List<string> keywordConditions = new List<string>();

                    for (int i = 0; i < keywords.Length; i++)
                    {
                        string paramName = $"@word{i}";
                        keywordConditions.Add($"(ir.ir_name LIKE {paramName} OR ir.ir_code LIKE {paramName})");
                        parameters.Add(new SqlParameter(paramName, $"%{keywords[i]}%"));
                    }

                    conditions.Add("(" + string.Join(" AND ", keywordConditions) + ")");
                    parameters.Add(new SqlParameter("@searchTerm", $"%{name}%"));
                    parameters.Add(new SqlParameter("@searchTermExact", $"{name}%"));
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    conditions.Add("ir.ir_category_id = @categoryId");
                    parameters.Add(new SqlParameter("@categoryId", categoryId.Value));
                }

                string whereClause = conditions.Count > 0
                    ? "WHERE ans.ans_name = 'BaseUrl' AND location_id = 1 AND ir_active = 1 AND " + string.Join(" AND ", conditions)
                    : "WHERE ans.ans_name = 'BaseUrl' AND location_id = 1 AND ir_active = 1";

                string query = $@"
                SELECT 
                    vs.ir_id AS ir_id,
                    ir.ir_name AS ir_name,
                    ir.ir_code AS ir_code,
                    vs.mrp AS mrp,
                    --vs.qty AS stock,
                    --ISNULL(p.pending_qty, 0) AS pending_qty,
                   -- (vs.qty - ISNULL(p.pending_qty, 0)) AS available_qty,
                    ans.ans_status + img.ii_image AS Image,
                    CASE 
                        WHEN wl.iw_product_id IS NOT NULL THEN 1 
                        ELSE 0 
                    END AS is_in_wishlist
                FROM
                (
                    SELECT *,
                           ROW_NUMBER() OVER
                           (
                               PARTITION BY ir_id
                               ORDER BY
                                   CASE WHEN exp_date IS NULL THEN 1 ELSE 0 END,
                                   exp_date DESC
                           ) AS rn
                    FROM View_Stock
                ) vs
                INNER JOIN inv_item_reg ir ON ir.ir_id = vs.ir_id
                LEFT JOIN inv_images img 
                    ON img.ii_reference_id = vs.ir_id 
                   AND img.ii_reference_type = 'item'
                LEFT JOIN inv_wishlist wl 
                    ON wl.iw_product_id = vs.ir_id 
                   AND wl.iw_user_id = @userId
                --LEFT JOIN (SELECT sp_ir_id,SUM(sp_qty) AS pending_qty FROM inv_sales_par WHERE sp_str_id=3 and sp_narration1 in ('pending','accepted') GROUP BY sp_ir_id) p ON p.sp_ir_id = vs.ir_id
                CROSS JOIN android_settings ans
                {whereClause}
                AND vs.rn = 1
                ORDER BY 
                CASE 
                    WHEN ir.ir_name LIKE @searchTermExact OR ir.ir_code LIKE @searchTermExact THEN 1
                    WHEN ir.ir_name LIKE @searchTerm OR ir.ir_code LIKE @searchTerm THEN 2
                    ELSE 3
                END,
                ir.ir_name ASC";

                DataTable dt = ExecuteDataTable(_connectionString, query, parameters.ToArray());

                // Group products
                var products = dt.AsEnumerable()
                    .GroupBy(row => new
                    {
                        Id = Convert.ToInt32(row["ir_id"]),
                        Name = row["ir_name"].ToString(),
                        //Code = row["ir_code"].ToString(),
                        Mrp = row["mrp"] != DBNull.Value ? Convert.ToDecimal(row["mrp"]) : 0,
                        //Stock = Convert.ToInt32(row["stock"]),
                        //available_qty = Convert.ToInt32(row["available_qty"]),
                        IsInWishlist = Convert.ToInt32(row["is_in_wishlist"])
                    })
                    .Select(g => new
                    {
                        id = g.Key.Id,
                        name = g.Key.Name,
                        //code = g.Key.Code,
                        mrp = g.Key.Mrp,
                        //stock = g.Key.Stock,
                        //available_qty = g.Key.available_qty,
                        is_in_wishlist = g.Key.IsInWishlist,
                        images = g.Where(x => x["Image"] != DBNull.Value && !string.IsNullOrWhiteSpace(x["Image"].ToString()))
                                   .Select(x => x["Image"].ToString())
                                   .Distinct()
                                   .ToList()
                    }).ToList();

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Search results",
                    data = products
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

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("ecommerce-products-by-category/{categoryId}")]
        public IActionResult GetProductsByCategory(int categoryId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Unauthorized: Invalid token.",
                        data = (object)null
                    });
                }

                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                // Step 1: Count total products in category
                string countSql = @"SELECT COUNT(*) FROM View_Stock inner join inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id WHERE ir_category_id = " + categoryId+ " and location_id=1 and ir_active = 1";
                int totalRecords = Convert.ToInt32(ExecuteScalar(_connectionString, countSql));


                if (totalRecords == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "No products found",
                        data = (object)null
                    });
                }

                int offset = (pageNumber - 1) * pageSize;

                // Step 2: Get paged products
                string productSql = $@"
               SELECT View_Stock.ir_id ir_id,inv_item_reg.ir_name as ir_name,View_Stock.mrp as mrp,View_Stock.qty as stock,ISNULL(p.pending_qty, 0) AS pending_qty,
               (View_Stock.qty - ISNULL(p.pending_qty, 0)) AS available_qty, CASE WHEN wl.iw_product_id IS NOT NULL THEN 1 ELSE 0 END AS is_in_wishlist
               FROM View_Stock 
               inner join inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id
               LEFT JOIN inv_wishlist wl ON wl.iw_product_id = View_Stock.ir_id AND wl.iw_user_id = @userId
               LEFT JOIN (SELECT sp_ir_id,SUM(sp_qty) AS pending_qty FROM inv_sales_par WHERE sp_str_id=3 and sp_narration1 in ('pending','accepted') GROUP BY sp_ir_id) p ON p.sp_ir_id = View_Stock.ir_id
               WHERE inv_item_reg.ir_category_id = @categoryId and location_id=1 and ir_active = 1
               ORDER BY View_Stock.ir_id Asc
               OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                var productParams = new[]
                {
                    new SqlParameter("@categoryId", SqlDbType.Int) { Value = categoryId },
                    new SqlParameter("@userId", SqlDbType.NVarChar) { Value = userId }
                };

                DataTable productTable = ExecuteDataTable(_connectionString, productSql, productParams);

                var productIds = productTable.AsEnumerable()
                    .Select(r => Convert.ToInt32(r["ir_id"]))
                    .ToList();

                if (!productIds.Any())
                {
                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Products fetched successfully",
                        data = new
                        {
                            pagination = new
                            {
                                page = pageNumber,
                                pageSize = pageSize,
                                totalRecords = totalRecords,
                                totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                            },
                            products = new List<object>()
                        }
                    });
                }

                // Step 3: Get images for these products
                string joinedIds = string.Join(",", productIds);
                string imageSql = $@"
                SELECT ii_reference_id AS ProductId,
                       ans.ans_status + ii_image AS Image
                FROM inv_images img
                CROSS JOIN android_settings ans
                WHERE ans.ans_name = 'BaseUrl'
                  AND ii_reference_type = 'item'
                  AND ii_reference_id IN ({joinedIds})";

                DataTable imageTable = ExecuteDataTable(_connectionString, imageSql);

                // Step 4: Join products with images
                var products = productTable.AsEnumerable()
                    .Select(row => new
                    {
                        ir_id = Convert.ToInt32(row["ir_id"]),
                        ir_name = row["ir_name"].ToString(),
                        ir_mrp = row["mrp"] != DBNull.Value ? Convert.ToDecimal(row["mrp"]) : 0,
                        stock = Convert.ToInt32(row["stock"]),
                        available_qty = Convert.ToInt32(row["available_qty"]),
                        images = imageTable.AsEnumerable()
                            .Where(img => Convert.ToInt32(img["ProductId"]) == Convert.ToInt32(row["ir_id"]))
                            .Select(img => img["Image"].ToString())
                            .Where(img => !string.IsNullOrWhiteSpace(img))
                            .Distinct()
                            .ToList(),
                        is_in_wishlist = Convert.ToInt32(row["is_in_wishlist"]) == 1
                    }).ToList();

                // Step 5: Final response
                var response = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Products fetched successfully",
                    data = new
                    {
                        pagination = new
                        {
                            page = pageNumber,
                            pageSize = pageSize,
                            totalRecords = totalRecords,
                            totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                        },
                        products = products
                    }
                };

                return Ok(response);
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("ecommerce-wishlist")]
        public IActionResult ToggleWishlist([FromBody] WishlistToggleModel model)
        {
            try
            {
                int userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0;

                if (userId == 0)
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Unauthorized: Invalid user ID.",
                        data = (object)null
                    });
                }

                // 1. Check if already exists
                string checkSql = "SELECT COUNT(*) FROM inv_wishlist WHERE iw_user_id = @userId AND iw_product_id = @productId";
                var checkParams = new[]
                {
                        new SqlParameter("@userId", userId),
                        new SqlParameter("@productId", model.productId)
                    };

                int exists = Convert.ToInt32(ExecuteScalar(_connectionString, checkSql, checkParams));

                if (model.isWishlist == 1 && exists == 0)
                {
                    // Insert
                    string insertSql = @"
                    INSERT INTO inv_wishlist (iw_user_id, iw_product_id, iw_created_at)
                    VALUES (@userId, @productId, GETDATE())";

                    var insertParams = new[]
                    {
                    new SqlParameter("@userId", userId),
                    new SqlParameter("@productId", model.productId)
                };

                    ExecuteNonQuery(_connectionString, insertSql, insertParams);
                }
                else if (model.isWishlist == 0 && exists > 0)
                {
                    // 3. Delete
                    string deleteSql = "DELETE FROM inv_wishlist WHERE iw_user_id = @userId AND iw_product_id = @productId";

                    var deleteParams = new[]
                    {
                            new SqlParameter("@userId", userId),
                            new SqlParameter("@productId", model.productId)
                        };

                    ExecuteNonQuery(_connectionString, deleteSql, deleteParams);
                }
                // After toggling, fetch updated wishlist (same as GetWishlist)
                string countSql = @"SELECT COUNT(*) 
                            FROM inv_wishlist 
                            INNER JOIN View_Stock  ON iw_product_id = ir_id
                            WHERE location_id=1 and iw_user_id = @userId AND iw_product_id = @productId ";
                int totalRecords = Convert.ToInt32(ExecuteScalar(_connectionString, countSql,
                new[] 
                {
                    new SqlParameter("@userId", userId),
                    new SqlParameter("@productId", model.productId)
                }));

                object product = null;

                if (totalRecords > 0)
                {
                    string productSql = @"
                    SELECT View_Stock.ir_id ir_id,inv_item_reg.ir_name as ir_name,View_Stock.mrp as mrp
							FROM inv_wishlist 
							INNER JOIN View_Stock  ON iw_product_id = ir_id
							INNER JOIN inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id
							WHERE location_id=1 and iw_user_id = @userId AND iw_product_id = @productId";

                    DataTable productTable = ExecuteDataTable(_connectionString, productSql,
                        new[] 
                        {
                            new SqlParameter("@userId", userId),
                            new SqlParameter("@productId", model.productId)
                        });

                    if (productTable.Rows.Count > 0)
                    {
                        int prodId = Convert.ToInt32(productTable.Rows[0]["ir_id"]);

                        string imageSql = @"
                        SELECT ii_reference_id AS ProductId,
                               ans.ans_status + ii_image AS Image
                        FROM inv_images img
                        CROSS JOIN android_settings ans
                        WHERE ans.ans_name = 'BaseUrl'
                          AND img.ii_reference_type = 'item'
                          AND img.ii_reference_id = @productId";

                        DataTable imageTable = ExecuteDataTable(_connectionString, imageSql,
                            new[] { new SqlParameter("@productId", prodId) });

                        product = new
                        {
                            ir_id = prodId,
                            ir_name = productTable.Rows[0]["ir_name"].ToString(),
                            ir_mrp = productTable.Rows[0]["mrp"] != DBNull.Value
                                        ? Convert.ToDecimal(productTable.Rows[0]["mrp"])
                                        : 0,
                            images = imageTable.AsEnumerable()
                                .Select(img => img["Image"].ToString())
                                .Where(img => !string.IsNullOrWhiteSpace(img))
                                .Distinct()
                                .ToList()
                        };
                    }
                }

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = model.isWishlist == 1
                        ? "Item added to wishlist."
                        : "Item removed from wishlist.",
                    data = product
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-ecommerce-wishlist")]
        public IActionResult GetWishlist(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Unauthorized: Invalid token.",
                        data = (object)null
                    });
                }

                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                // 1. Count wishlist items
                string countSql = @"
                SELECT COUNT(DISTINCT w.iw_product_id)
                FROM inv_wishlist w
                INNER JOIN View_Stock vs
                    ON w.iw_product_id = vs.ir_id
                WHERE vs.location_id = 1
                  AND w.iw_user_id = @userId";
                int totalRecords = Convert.ToInt32(ExecuteScalar(_connectionString, countSql,
                    new[] { new SqlParameter("@userId", userId) }));

                if (totalRecords == 0)
                {
                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Wishlist is empty",
                        data = new
                        {
                            pagination = new
                            {
                                page = pageNumber,
                                pageSize = pageSize,
                                totalRecords = 0,
                                totalPages = 0
                            },
                            products = new List<object>()
                        }
                    });
                }

                int offset = (pageNumber - 1) * pageSize;

                // 2. Get wishlist items
                string productSql = @"
            SELECT
                vs.ir_id,
                MAX(i.ir_name) AS ir_name,
                MAX(vs.mrp) AS mrp,
                SUM(vs.qty) AS stock,
                ISNULL(p.pending_qty, 0) AS pending_qty,
                SUM(vs.qty) - ISNULL(p.pending_qty, 0) AS available_qty
            FROM inv_wishlist w
            INNER JOIN View_Stock vs
                ON w.iw_product_id = vs.ir_id
            INNER JOIN inv_item_reg i
                ON i.ir_id = vs.ir_id
            LEFT JOIN
            (
                SELECT
                    sp_ir_id,
                    SUM(sp_qty) AS pending_qty
                FROM inv_sales_par
                WHERE sp_str_id = 3
                  AND sp_narration1 IN ('pending', 'accepted')
                GROUP BY sp_ir_id
            ) p
                ON p.sp_ir_id = vs.ir_id
            WHERE vs.location_id = 1
              AND w.iw_user_id = @userId
            GROUP BY
                vs.ir_id,
                p.pending_qty
            ORDER BY
                vs.ir_id ASC
            OFFSET @offset ROWS
            FETCH NEXT @pageSize ROWS ONLY";

                var productParams = new[]
                {
                    new SqlParameter("@userId", SqlDbType.NVarChar)
                    {
                        Value = userId
                    },
                    new SqlParameter("@offset", SqlDbType.Int)
                    {
                        Value = offset
                    },
                    new SqlParameter("@pageSize", SqlDbType.Int)
                    {
                        Value = pageSize
                    }
                };

                DataTable productTable = ExecuteDataTable(_connectionString, productSql, productParams);

                var productIds = productTable.AsEnumerable()
                    .Select(r => Convert.ToInt32(r["ir_id"]))
                    .ToList();

                // 3. Get images for wishlist products
                string joinedIds = string.Join(",", productIds);
                string imageSql = $@"
                SELECT ii_reference_id AS ProductId,
                       ans.ans_status + img.ii_image AS Image
                FROM inv_images img
                CROSS JOIN android_settings ans
                WHERE ans.ans_name = 'BaseUrl'
                  AND img.ii_reference_type = 'item'
                  AND img.ii_reference_id IN ({joinedIds})";

                DataTable imageTable = ExecuteDataTable(_connectionString, imageSql);

                // 4. Join products with images
                var products = productTable.AsEnumerable()
                    .Select(row => new
                    {
                        ir_id = Convert.ToInt32(row["ir_id"]),
                        ir_name = row["ir_name"].ToString(),
                        ir_mrp = row["mrp"] != DBNull.Value ? Convert.ToDecimal(row["mrp"]) : 0,
                        stock = Convert.ToInt32(row["stock"]),
                        available_qty = Convert.ToInt32(row["available_qty"]),
                        images = imageTable.AsEnumerable()
                            .Where(img => Convert.ToInt32(img["ProductId"]) == Convert.ToInt32(row["ir_id"]))
                            .Select(img => img["Image"].ToString())
                            .Where(img => !string.IsNullOrWhiteSpace(img))
                            .Distinct()
                            .ToList()
                    }).ToList();

                // 5. Return response
                var response = new
                {
                    status = true,
                    statusCode = 200,
                    message = "Wishlist fetched successfully",
                    data = new
                    {
                        pagination = new
                        {
                            page = pageNumber,
                            pageSize = pageSize,
                            totalRecords = totalRecords,
                            totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                        },
                        products = products
                    }
                };

                return Ok(response);
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
        //[Authorize(Roles = UserRoles.EcommerceUser)]
        //[HttpPost("create-ecommerce-cart")]
        //public IActionResult ToggleCart([FromBody] CartToggleModel model)
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        var role = User.FindFirst("UserRole")?.Value;
        //        if (string.IsNullOrEmpty(userId))
        //        {
        //            return Unauthorized(new
        //            {
        //                status = false,
        //                statusCode = 401,
        //                message = "Unauthorized: Invalid token.",
        //                data = (object)null
        //            });
        //        }

        //        object product = null;
        //        int exists = 0; // declare outside
        //        if (role == "customer")
        //        {
        //            string checkSql = "SELECT COUNT(*) FROM inv_ecommerce_cart WHERE c_customer_id = @customer_id AND c_item_id = @itemId";
        //            var checkParams = new[]
        //            {
        //            new SqlParameter("@customer_id", model.customerId),
        //            new SqlParameter("@itemId", model.itemId)
        //            };

        //            exists = Convert.ToInt32(ExecuteScalar(_connectionString, checkSql, checkParams));
        //        }
        //        // 1. Check if item already exists in cart


        //        if (role == "salesman")
        //        {
        //            string checkSql = "SELECT COUNT(*) FROM inv_ecommerce_cart WHERE c_salesman_id = @salesman_id AND c_item_id = @itemId";
        //            var checkParams = new[]
        //            {
        //            new SqlParameter("@salesman_id", model.salesmanId),
        //            new SqlParameter("@itemId", model.itemId)
        //            };

        //            exists = Convert.ToInt32(ExecuteScalar(_connectionString, checkSql, checkParams));
        //        }
        //        if (exists == 0)
        //        {
        //            // Insert into cart
        //            string insertSql = @"
        //            INSERT INTO inv_ecommerce_cart (c_customer_id, c_salesman_id, c_item_id, c_item_qty, c_price, c_remarks, c_attachment,c_user_id)
        //            VALUES (@customer_id, @salesman_id, @itemId, @qty, @price, @remarks, @attachment,@c_user_id)";

        //            var insertParams = new[]
        //            {
        //                new SqlParameter("@customer_id", model.customerId),
        //                new SqlParameter("@salesman_id", model.salesmanId),
        //                new SqlParameter("@itemId", model.itemId),
        //                new SqlParameter("@qty", model.qty),
        //                new SqlParameter("@price", model.price),
        //                new SqlParameter("@remarks", model.remarks ?? string.Empty),
        //                new SqlParameter("@attachment", model.attachment ?? string.Empty),
        //                new SqlParameter("@c_user_id", userId)
        //            };

        //            ExecuteNonQuery(_connectionString, insertSql, insertParams);

        //            // Retrieve newly inserted product
        //            string productSql = @"
        //            SELECT c.c_id as CartId, c.c_date as Date, 
        //                   ir.ir_id as ItemId, ir.ir_name as ItemName,
        //                   c.c_item_qty as Qty, c.c_price as Price, 
        //                   c.c_remarks as Remarks, c.c_attachment as Attachment,
        //                   c.c_customer_id as CustomerId, a.as_name as CustomerName,
        //                   c.c_salesman_id as SalesmanId, b.as_name as SalesmanName
        //            FROM inv_ecommerce_cart c
        //            LEFT JOIN inv_item_reg ir ON c.c_item_id = ir.ir_id
        //            LEFT JOIN acc_subhead a ON a.as_id = c.c_customer_id
        //            LEFT JOIN acc_subhead b ON b.as_id = c.c_salesman_id
        //            WHERE c.c_customer_id = " + model.customerId + " AND c.c_item_id = " + model.itemId + "";

        //            DataTable productTable = ExecuteDataTable(_connectionString, productSql);

        //            if (productTable.Rows.Count > 0)
        //            {
        //                int prodId = Convert.ToInt32(productTable.Rows[0]["ItemId"]);

        //                string imageSql = @"
        //                SELECT ii_reference_id AS ProductId,
        //                       ans.ans_status + img.ii_image AS Image
        //                FROM inv_images img
        //                CROSS JOIN android_settings ans
        //                WHERE ans.ans_name = 'BaseUrl'
        //                  AND img.ii_reference_type = 'item'
        //                  AND img.ii_reference_id = @productId";

        //                DataTable imageTable = ExecuteDataTable(_connectionString, imageSql,
        //                    new[] { new SqlParameter("@productId", prodId) });

        //                product = new
        //                {
        //                    CartId = Convert.ToInt32(productTable.Rows[0]["CartId"]),
        //                    Date = Convert.ToDateTime(productTable.Rows[0]["Date"]),
        //                    ItemId = Convert.ToInt32(productTable.Rows[0]["ItemId"]),
        //                    ItemName = productTable.Rows[0]["ItemName"].ToString(),
        //                    Qty = Convert.ToDouble(productTable.Rows[0]["Qty"]),
        //                    Price = Convert.ToDecimal(productTable.Rows[0]["Price"]),
        //                    Remarks = productTable.Rows[0]["Remarks"]?.ToString(),
        //                    Attachment = productTable.Rows[0]["Attachment"]?.ToString(),
        //                    CustomerId = Convert.ToInt32(productTable.Rows[0]["CustomerId"]),
        //                    CustomerName = productTable.Rows[0]["CustomerName"]?.ToString(),
        //                    SalesmanId = Convert.ToInt32(productTable.Rows[0]["SalesmanId"]),
        //                    SalesmanName = productTable.Rows[0]["SalesmanName"]?.ToString(),
        //                    Images = imageTable.AsEnumerable()
        //                        .Select(img => img["Image"].ToString())
        //                        .Where(img => !string.IsNullOrWhiteSpace(img))
        //                        .Distinct()
        //                        .ToList()
        //                };
        //            }
        //        }

        //        return Ok(new
        //        {
        //            status = true,
        //            statusCode = 200,
        //            message = exists == 0 ? "Item added to cart." : "Item already exists in cart.",
        //            data = product
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = "Error: " + ex.Message,
        //            data = (object)nullCU
        //        });
        //    }
        //}
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("create-ecommerce-cart")]
        public IActionResult ToggleCart1([FromForm] CartToggleModel1 model, IFormFile attachment)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst("UserRole")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Unauthorized: Invalid token.",
                        data = (object)null
                    });
                }

                // ✅ Get BaseUrl from DB
                string baseUrl = ExecuteScalar(_connectionString, "SELECT ans_status FROM android_settings WHERE ans_name = 'BaseUrl'")?.ToString();

                // ✅ Handle attachment upload
                string attachmentPath = null;
                if (attachment != null && attachment.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cart");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(attachment.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        attachment.CopyTo(stream);
                    }

                    // ✅ Save relative path in DB
                    attachmentPath = "/uploads/cart/" + fileName;
                }

                int exists = 0;

                if (role == "customer")
                {
                    string checkSql = "SELECT COUNT(*) FROM inv_ecommerce_cart WHERE c_customer_id = @customer_id AND c_item_id = @itemId";
                    exists = Convert.ToInt32(ExecuteScalar(_connectionString, checkSql,
                        new[] {
                    new SqlParameter("@customer_id", model.customerId),
                    new SqlParameter("@itemId", model.itemId)
                        }));
                }
                else 
                {
                    string checkSql = "SELECT COUNT(*) FROM inv_ecommerce_cart WHERE c_salesman_id = @salesman_id AND c_item_id = @itemId";
                    exists = Convert.ToInt32(ExecuteScalar(_connectionString, checkSql,
                        new[] {
                    new SqlParameter("@salesman_id", model.salesmanId),
                    new SqlParameter("@itemId", model.itemId)
                        }));
                }
                object product = null;

                if (exists == 0)
                {
                    // ✅ Insert into cart
                    string insertSql = @"
                INSERT INTO inv_ecommerce_cart 
                (c_customer_id, c_salesman_id, c_item_id, c_item_qty, c_price,c_date, c_remarks, c_attachment, c_user_id)
                VALUES (@customer_id, @salesman_id, @itemId, @qty, @price,GETDATE(), @remarks, @attachment, @c_user_id)";

                    ExecuteNonQuery(_connectionString, insertSql, new[] {
                new SqlParameter("@customer_id", role == "customer" ? (object?)model.customerId ?? 0 : 0),
                new SqlParameter("@salesman_id", (object?)model.salesmanId ?? DBNull.Value),
                new SqlParameter("@itemId", model.itemId),
                new SqlParameter("@qty", model.qty),
                new SqlParameter("@price", model.price),
                new SqlParameter("@remarks", model.remarks ?? string.Empty),
                new SqlParameter("@attachment", (object)attachmentPath ?? ""),
                new SqlParameter("@c_user_id", userId)
            });
                    string productSql = @"
                    SELECT TOP 1 c.c_id as CartId, c.c_date as Date, 
                           ir.ir_id as ItemId, ir.ir_name as ItemName,
                           c.c_item_qty as Qty, c.c_price as Price, 
                           c.c_remarks as Remarks, c.c_attachment as Attachment,
                           c.c_customer_id as CustomerId, a.as_name as CustomerName,
                           c.c_salesman_id as SalesmanId, b.as_name as SalesmanName
                    FROM inv_ecommerce_cart c
                    LEFT JOIN inv_item_reg ir ON c.c_item_id = ir.ir_id
                    LEFT JOIN acc_subhead a ON a.as_id = c.c_customer_id
                    LEFT JOIN acc_subhead b ON b.as_id = c.c_salesman_id
                    WHERE (c.c_customer_id = @cust OR @cust = 0) 
                      AND (c.c_salesman_id = @sales OR @sales = 0)
                      AND c.c_item_id = @itemId
                    ORDER BY c.c_id DESC";

                DataTable productTable = ExecuteDataTable(_connectionString, productSql, new[] {
                new SqlParameter("@cust", (object?)model.customerId ?? 0),
                new SqlParameter("@sales", model.salesmanId),
                new SqlParameter("@itemId", model.itemId)
        });

                    if (productTable.Rows.Count > 0)
                    {
                        product = new
                        {
                            CartId = Convert.ToInt32(productTable.Rows[0]["CartId"]),
                            Date = Convert.ToDateTime(productTable.Rows[0]["Date"]),
                            ItemId = Convert.ToInt32(productTable.Rows[0]["ItemId"]),
                            ItemName = productTable.Rows[0]["ItemName"].ToString(),
                            Qty = Convert.ToDouble(productTable.Rows[0]["Qty"]),
                            Price = Convert.ToDecimal(productTable.Rows[0]["Price"]),
                            Remarks = productTable.Rows[0]["Remarks"]?.ToString(),
                            // ✅ Full path (BaseUrl + relativePath)
                            Attachment = string.IsNullOrEmpty(productTable.Rows[0]["Attachment"].ToString())
                                         ? ""
                                         : baseUrl.TrimEnd('/') + productTable.Rows[0]["Attachment"].ToString(),
                            CustomerId = Convert.ToInt32(productTable.Rows[0]["CustomerId"]),
                            CustomerName = productTable.Rows[0]["CustomerName"]?.ToString(),
                            SalesmanId = Convert.ToInt32(productTable.Rows[0]["SalesmanId"]),
                            SalesmanName = productTable.Rows[0]["SalesmanName"]?.ToString()
                        };
                    }

                }

                // ✅ Fetch newly inserted/updated product

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = exists == 0 ? "Item added to cart." : "Item already exists in cart.",
                    data = product
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("remove-ecommerce-cart/{cartId}")]
        public IActionResult RemoveCart(int cartId)
        {
            try
            {
                // 1. Retrieve product before deleting (optional)
                string selectSql = @"SELECT c_id as CartId  FROM inv_ecommerce_cart WHERE c_id = @cartId";

                DataTable productTable = ExecuteDataTable(_connectionString, selectSql,
                    new[] { new SqlParameter("@cartId", cartId) });

                if (productTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "Cart item not found.",
                        data = (object)null
                    });
                }

                // 2. Delete the cart record
                string deleteSql = "DELETE FROM inv_ecommerce_cart WHERE c_id = @cartId";
                ExecuteNonQuery(_connectionString, deleteSql,
                    new[] { new SqlParameter("@cartId", cartId) });

                // 3. Prepare deleted product info
                var deletedCartId = new
                {
                    CartId = Convert.ToInt32(productTable.Rows[0]["CartId"]),
                };

                // 4. Return success
                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Item removed from cart successfully.",
                    data = deletedCartId
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-ecommerce-cart")]
        public IActionResult GetCart(int? customerId,int pageNumber = 1, int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst("UserRole")?.Value;
            var ledgerIdStr = User.FindFirst("LedgerId")?.Value;

            if (!int.TryParse(ledgerIdStr, out int ledgerId))
                return BadRequest("Invalid LedgerId in claims.");

            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;
                // ✅ Get BaseUrl from settings
                string baseUrl = ExecuteScalar(
                    _connectionString,
                    "SELECT ans_status FROM android_settings WHERE ans_name = 'BaseUrl'"
                )?.ToString() ?? "";


                string countSql;
                List<SqlParameter> parameters = new();

                if (userRole == "customer")
                {
                    countSql = @"SELECT COUNT(*) 
                         FROM inv_ecommerce_cart c 
                         WHERE c.c_customer_id = @CustomerId";

                    parameters.Add(new SqlParameter("@CustomerId", ledgerId));
                }
                else
                {
                    countSql = @"SELECT COUNT(*) 
                         FROM inv_ecommerce_cart c 
                         WHERE c.c_user_id = @c_user_id";
                    parameters.Add(new SqlParameter("@c_user_id", userId));
                }
                

                int totalRecords = Convert.ToInt32(
                    ExecuteScalar(_connectionString, countSql, parameters.ToArray())
                );

                if (totalRecords == 0)
                {
                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Cart is empty",
                        data = new
                        {
                            pagination = new
                            {
                                page = pageNumber,
                                pageSize = pageSize,
                                totalRecords = 0,
                                totalPages = 0
                            },
                            products = new List<object>()
                        }
                    });
                }

                int offset = (pageNumber - 1) * pageSize;
                string productSql = "";
                List<SqlParameter> productParams = new();

                if (userRole == "customer")
                {
                    productSql = $@"
                    SELECT c.c_id as CartId, c.c_date as Date, 
       c.c_item_id as ItemId, ir.ir_name as ItemName, c.c_price as  mrp,
       c.c_item_qty as Qty, c.c_price as Price, 
       c.c_remarks as Remarks, c.c_attachment as Attachment,
       c.c_customer_id as CustomerId, a.as_name as CustomerName,
       c.c_salesman_id as SalesmanId, b.as_name as SalesmanName
		FROM inv_ecommerce_cart c
		LEFT JOIN inv_item_reg ir ON c.c_item_id = ir.ir_id
		LEFT JOIN acc_subhead a ON a.as_id = c.c_customer_id
		LEFT JOIN acc_subhead b ON b.as_id = c.c_salesman_id
                    WHERE c.c_customer_id = @CustomerId
                    ORDER BY c.c_date DESC
                    OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                    productParams.Add(new SqlParameter("@CustomerId", ledgerId));
                }
                else
                {
                    productSql = $@"
                    SELECT c.c_id as CartId, c.c_date as Date, 
       c.c_item_id as ItemId, ir.ir_name as ItemName, c.c_price as  mrp,
       c.c_item_qty as Qty, c.c_price as Price, 
       c.c_remarks as Remarks, c.c_attachment as Attachment,
       c.c_customer_id as CustomerId, a.as_name as CustomerName,
       c.c_salesman_id as SalesmanId, b.as_name as SalesmanName
		FROM inv_ecommerce_cart c
		LEFT JOIN inv_item_reg ir ON c.c_item_id = ir.ir_id
		LEFT JOIN acc_subhead a ON a.as_id = c.c_customer_id
		LEFT JOIN acc_subhead b ON b.as_id = c.c_salesman_id
                    WHERE c.c_user_id = @c_user_id                      
                    ORDER BY c.c_date DESC
                    OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                    productParams.Add(new SqlParameter("@c_user_id", userId));
                }
                // 2. Get cart items for customer


                DataTable productTable = ExecuteDataTable(
                _connectionString,
                productSql,
                productParams.ToArray()
            );

                var productIds = productTable.AsEnumerable()
                    .Select(r => Convert.ToInt32(r["ItemId"]))
                    .Distinct()
                    .ToList();

                // 3. Get images
                DataTable imageTable = new DataTable();
                if (productIds.Any())
                {
                    string joinedIds = string.Join(",", productIds);
                    string imageSql = $@"
                    SELECT ii_reference_id AS ProductId,
                           ans.ans_status + img.ii_image AS Image
                    FROM inv_images img
                    CROSS JOIN android_settings ans
                    WHERE ans.ans_name = 'BaseUrl'
                      AND img.ii_reference_type = 'item'
                      AND img.ii_reference_id IN ({joinedIds})";


                    imageTable = ExecuteDataTable(_connectionString, imageSql);
                }

                // 4. Merge products with images
                var products = productTable.AsEnumerable()
            .Select(row =>
            {
                int? itemId = row["ItemId"] != DBNull.Value
                    ? Convert.ToInt32(row["ItemId"])
                    : (int?)null;

                int? cartId = row["CartId"] != DBNull.Value
                    ? Convert.ToInt32(row["CartId"])
                    : (int?)null;

                int? customerIdValue = row["CustomerId"] != DBNull.Value
                    ? Convert.ToInt32(row["CustomerId"])
                    : (int?)null;

                int? salesmanId = row["SalesmanId"] != DBNull.Value
                    ? Convert.ToInt32(row["SalesmanId"])
                    : (int?)null;

                DateTime? date = row["Date"] != DBNull.Value
                    ? Convert.ToDateTime(row["Date"])
                    : (DateTime?)null;

                decimal mrp = row["mrp"] != DBNull.Value
                    ? Convert.ToDecimal(row["mrp"])
                    : 0;

                double qty = row["Qty"] != DBNull.Value
                    ? Convert.ToDouble(row["Qty"])
                    : 0;

                decimal price = row["Price"] != DBNull.Value
                    ? Convert.ToDecimal(row["Price"])
                    : 0;

                string attachment = row["Attachment"] != DBNull.Value
                    ? row["Attachment"].ToString()
                    : "";

                var images = new List<string>();

                if (itemId.HasValue)
                {
                    images = imageTable.AsEnumerable()
                        .Where(img =>
                            img["ProductId"] != DBNull.Value &&
                            Convert.ToInt32(img["ProductId"]) == itemId.Value)
                        .Select(img =>
                            img["Image"] != DBNull.Value
                                ? img["Image"].ToString()
                                : "")
                        .Where(img => !string.IsNullOrWhiteSpace(img))
                        .Distinct()
                        .ToList();
                }

                return new
                {
                    CartId = cartId,
                    Date = date,
                    ItemId = itemId,
                    ItemName = row["ItemName"] != DBNull.Value
                        ? row["ItemName"].ToString()
                        : "",

                    ItemMrp = mrp,
                    Qty = qty,
                    Price = price,

                    Remarks = row["Remarks"] != DBNull.Value
                        ? row["Remarks"].ToString()
                        : "",

                    Attachment = string.IsNullOrEmpty(attachment)
                        ? ""
                        : baseUrl.TrimEnd('/') + "/" + attachment.TrimStart('/'),

                    CustomerId = customerIdValue,

                    CustomerName = row["CustomerName"] != DBNull.Value
                        ? row["CustomerName"].ToString()
                        : "",

                    SalesmanId = salesmanId,

                    SalesmanName = row["SalesmanName"] != DBNull.Value
                        ? row["SalesmanName"].ToString()
                        : "",

                    Images = images
                };
            })
            .ToList();

                // 5. Return result
                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Cart fetched successfully",
                    data = new
                    {
                        pagination = new
                        {
                            page = pageNumber,
                            pageSize = pageSize,
                            totalRecords = totalRecords,
                            totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                        },
                        products = products
                    }
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("update-ecommerce-cart-qty")]
        public IActionResult UpdateCartQty(int CartId, int Qty)
        {
            try
            {
                // 1. Validate input
                if (CartId <= 0 || Qty <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Invalid cartId or qty.",
                        data = (object)null
                    });
                }

                // 2. Check if cart exists
                string selectSql = @"SELECT c_id as CartId, c_item_qty as Qty 
                             FROM inv_ecommerce_cart 
                             WHERE c_id = @cartId";

                DataTable cartTable = ExecuteDataTable(_connectionString, selectSql,
                    new[] { new SqlParameter("@cartId", CartId) });

                if (cartTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "Cart item not found.",
                        data = (object)null
                    });
                }

                // 3. Update qty
                string updateSql = @"UPDATE inv_ecommerce_cart 
                             SET c_item_qty = @qty 
                             WHERE c_id = @cartId";

                ExecuteNonQuery(_connectionString, updateSql,
                    new[]
                    {
                new SqlParameter("@qty", Qty),
                new SqlParameter("@cartId", CartId)
                    });

                // 4. Return success
                var updatedCart = new
                {
                    CartId = CartId,
                    Qty = Qty
                };

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Cart quantity updated successfully.",
                    data = updatedCart
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("ecommerce-product-details/{productId}")]
        public IActionResult GetProductDetails(int productId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Unauthorized: Invalid token.",
                        data = (object)null
                    });
                }

                // Query to get product details
                string productQuery = @"
                SELECT View_Stock.ir_id ir_id,inv_item_reg.ir_name as ir_name,
                View_Stock.mrp as mrp,CASE WHEN iw_product_id IS NOT NULL THEN 1 ELSE 0 END AS is_in_wishlist,ir_category_id
                from View_Stock 
                inner join inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id 
                LEFT JOIN inv_wishlist  ON iw_product_id = View_Stock.ir_id AND iw_user_id = @userId
                WHERE View_Stock.ir_id = @productId and location_id=1 and ir_active = 1";

                // Query to get all images for this product
                string imageQuery = @"
                SELECT ii_id,
                       ans.ans_status + img.ii_image AS Image
                FROM inv_images img
                CROSS JOIN android_settings ans
                WHERE ans.ans_name = 'BaseUrl'
                  AND img.ii_reference_type = 'item'
                  AND img.ii_reference_id = @productId";


                var parameters = new[]
                {
                    new SqlParameter("@userId", userId),
                    new SqlParameter("@productId", productId)
                };

                DataTable productDt = ExecuteDataTable(_connectionString, productQuery, parameters);
                if (productDt.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "Product not found.",
                        data = (object)null
                    });
                }

                DataRow row = productDt.Rows[0];

                // Load images
                DataTable imageDt = ExecuteDataTable(_connectionString, imageQuery, new[] { new SqlParameter("@productId", productId) });

                var images = imageDt.AsEnumerable()
                .Where(imgRow => imgRow["Image"] != DBNull.Value)
                .Select(imgRow => imgRow["Image"].ToString())
                .ToList();

                // Build response
                var product = new
                {
                    ir_id = Convert.ToInt32(row["ir_id"]),
                    ir_name = row["ir_name"].ToString(),
                    ir_mrp = row["mrp"] != DBNull.Value ? Convert.ToDecimal(row["mrp"]) : 0,                                                                        
                    is_in_wishlist = Convert.ToInt32(row["is_in_wishlist"]) == 1,
                    ir_category_id = Convert.ToInt32(row["ir_category_id"]),
                    images = images
                };

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Product details fetched successfully",
                    data = product
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
        //[Authorize(Roles = UserRoles.EcommerceUser)]
        //[HttpGet("get-customers")]
        //public IActionResult GetCustomers(int? areaId, int? routeId)
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        var userRole = User.FindFirst("UserRole")?.Value;
        //        var ledgerId = User.FindFirst("LedgerId")?.Value;

        //        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
        //        {
        //            return Unauthorized(new
        //            {
        //                status = false,
        //                statusCode = 401,
        //                message = "Unauthorized: Invalid token.",
        //                data = (object)null
        //            });
        //        }

        //        if (!string.Equals(userRole, "salesman", StringComparison.OrdinalIgnoreCase))
        //        {
        //            return StatusCode(403, new
        //            {
        //                status = false,
        //                statusCode = 403,
        //                message = "Forbidden: Only salesmen are allowed to access this resource.",
        //                data = (object)null
        //            });
        //        }

        //        string sql = @"
        //        SELECT 
        //            as_id AS CustomerId, 
        //            as_name AS CustomerName, 
        //            as_add1 AS Address1, 
        //            as_add2 AS Address2, 
        //            as_add3 AS Address3, 
        //            as_mob AS Mobile
        //        FROM acc_subhead 
        //        WHERE as_ap_id = 4
        //        AND as_salesman_id = @LedgerId";

        //        if (areaId.HasValue)
        //            sql += " AND as_area_id = @AreaId";

        //        if (routeId.HasValue)
        //            sql += " AND as_rout_id = @RouteId";

        //        var parameters = new List<SqlParameter>
        //        {
        //            new SqlParameter("@LedgerId", ledgerId ?? (object)DBNull.Value)
        //        };

        //        if (areaId.HasValue)
        //            parameters.Add(new SqlParameter("@AreaId", areaId.Value));
        //        if (routeId.HasValue)
        //            parameters.Add(new SqlParameter("@RouteId", routeId.Value));

        //        DataTable customerDt = ExecuteDataTable(_connectionString, sql, parameters.ToArray());

        //        var customerList = DataTableToList(customerDt);

        //        return Ok(new
        //        {
        //            status = true,
        //            statusCode = 200,
        //            message = "Customers fetched successfully",
        //            data = customerList
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = "Error: " + ex.Message,
        //            data = (object)null
        //        });
        //    }
        //}
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-customers")]
        public IActionResult GetCustomers(int? areaId, int? routeId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst("UserRole")?.Value;
                var ledgerId = User.FindFirst("LedgerId")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Unauthorized: Invalid token.",
                        data = (object)null
                    });
                }

                if (!string.Equals(userRole, "salesman", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(userRole, "marketing executive", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(403, new
                    {
                        status = false,
                        statusCode = 403,
                        message = "Forbidden: Only salesmen or marketing executives are allowed to access this resource.",
                        data = (object)null
                    });
                }


                // 2️⃣ Build main query based on count
                string sql;
                var parameters = new List<SqlParameter>();

                if (userRole == "salesman")
                {
                    // Existing condition
                    sql = @"
            SELECT 
                as_id AS CustomerId, 
                as_name AS CustomerName, 
                as_add1 AS Address1, 
                as_add2 AS Address2, 
                as_add3 AS Address3, 
                as_mob AS Mobile
            FROM acc_subhead 
            WHERE as_ap_id = 4
            AND as_salesman_id = @LedgerId";

                    parameters.Add(new SqlParameter("@LedgerId", ledgerId ?? (object)DBNull.Value));

                    if (areaId.HasValue)
                    {
                        sql += " AND as_area_id = @AreaId";
                        parameters.Add(new SqlParameter("@AreaId", areaId.Value));
                    }

                    if (routeId.HasValue)
                    {
                        sql += " AND as_rout_id = @RouteId";
                        parameters.Add(new SqlParameter("@RouteId", routeId.Value));
                    }
                }
                else
                {
                    // No customer for this salesman → return all
                    sql = @"
            SELECT 
                as_id AS CustomerId, 
                as_name AS CustomerName, 
                as_add1 AS Address1, 
                as_add2 AS Address2, 
                as_add3 AS Address3, 
                as_mob AS Mobile
            FROM acc_subhead 
            WHERE as_ap_id = 4";
                }

                DataTable customerDt = ExecuteDataTable(_connectionString, sql, parameters.ToArray());
                var customerList = DataTableToList(customerDt);

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Customers fetched successfully",
                    data = customerList
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-suppliers")]
        public IActionResult GetSuppliers()
        {
            try
            {
                string sql = @"
        SELECT 
            as_id AS SupplierId, 
            as_name AS SupplierName, 
            as_add1 AS Address1, 
            as_add2 AS Address2, 
            as_add3 AS Address3, 
            as_mob AS Mobile
        FROM acc_subhead 
        WHERE as_ap_id = 6";

                // ✅ No parameters needed
                DataTable supplierDt = ExecuteDataTable(_connectionString, sql, null);

                var supplierList = DataTableToList(supplierDt);

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Suppliers fetched successfully",
                    data = supplierList
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-paginated-customers")]
        public IActionResult GetPaginatedCustomers(int? areaId, int? routeId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst("UserRole")?.Value;
                var ledgerId = User.FindFirst("LedgerId")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
                {
                    return Unauthorized(new { status = false, statusCode = 401, message = "Unauthorized: Invalid token." });
                }

                pageNumber = pageNumber < 1 ? 1 : pageNumber;
                pageSize = pageSize < 1 ? 10 : pageSize;
                int offset = (pageNumber - 1) * pageSize;

                // 1. Build the Filter and Parameters
                string filterSql = " WHERE as_ap_id = 4";
                var parameters = new List<SqlParameter>();

                if (userRole.Equals("salesman", StringComparison.OrdinalIgnoreCase))
                {
                    filterSql += " AND as_salesman_id = @LedgerId";
                    parameters.Add(new SqlParameter("@LedgerId", ledgerId ?? (object)DBNull.Value));

                    if (areaId.HasValue)
                    {
                        filterSql += " AND as_area_id = @AreaId";
                        parameters.Add(new SqlParameter("@AreaId", areaId.Value));
                    }

                    if (routeId.HasValue)
                    {
                        filterSql += " AND as_rout_id = @RouteId";
                        parameters.Add(new SqlParameter("@RouteId", routeId.Value));
                    }
                }

                // 2. Get Total Count
                string countSql = $@"SELECT COUNT(*) FROM acc_subhead {filterSql}";

                // FIX: We convert to array for the first call
                var countParams = parameters.Select(x => (SqlParameter)((ICloneable)x).Clone()).ToArray();
                object totalCountObj = ExecuteScalar(_connectionString, countSql, countParams);
                int totalRecords = Convert.ToInt32(totalCountObj);

                // 3. Get Paginated Data
                string dataSql = $@"
            SELECT 
                as_id AS CustomerId, 
                as_name AS CustomerName, 
                as_add1 AS Address1, 
                as_add2 AS Address2, 
                as_add3 AS Address3, 
                as_mob AS Mobile
            FROM acc_subhead 
            {filterSql}
            ORDER BY as_id 
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                // Add the specific pagination parameters
                parameters.Add(new SqlParameter("@Offset", offset));
                parameters.Add(new SqlParameter("@PageSize", pageSize));

                // FIX: Use ToArray() for the second call
                DataTable customerDt = ExecuteDataTable(_connectionString, dataSql, parameters.ToArray());
                var customerList = DataTableToList(customerDt);

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Customers fetched successfully",
                    totalRecords = totalRecords,
                    totalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    currentPage = pageNumber,
                    data = customerList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, statusCode = 500, message = "Error: " + ex.Message });
            }
        }
        

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("checkout-cart")]
        public IActionResult CheckoutCart([FromBody] CheckoutRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ledgerId = User.FindFirst("LedgerId")?.Value;
            var role = User.FindFirst("UserRole")?.Value;
            // ✅ Get BaseUrl from settings
            string baseUrl = ExecuteScalar(
                _connectionString,
                "SELECT ans_status FROM android_settings WHERE ans_name = 'BaseUrl'"
            )?.ToString() ?? "";

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    status = false,
                    statusCode = 401,
                    message = "Unauthorized: Invalid token."
                });
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Insert into inv_sales_inf (header)
                        string insertInfSql = @"
                    INSERT INTO inv_sales_inf(
                    si_str_id, si_entryno, si_date, si_acc_id, si_cust_name,si_add1,si_add2, si_remarks,
                    si_gross_value, si_net_amount, si_total, si_user_id,si_sales_acc_id,si_from_mobile,si_salesman_id,si_location_id,si_tax_type,
                    si_profit,si_grand_total,si_commision_acc_id,si_net_balance,si_loc_entryno, si_loc_entryno_only,si_next_entryno,si_deliverydate,
                    si_qtn_no,si_finish,si_sum_item,si_cash_paid_acc,si_lc_id,si_cycr_far,si_credit_note_no,si_damage_entryno,si_return_against_invoiceno,si_alias_name,
                    si_rate_type,si_functiondate,si_tax_calculation ,si_finished_date,si_expected_date,si_due_date ,si_eway_date,si_additional_cost,si_insert_user_id ,
                    si_f_bumber, si_b_bumber, si_bonnet, si_boot, si_roof, si_fr_light, si_fl_light, si_bl_light, si_br_light, si_fl_tyre, si_fr_tyre, si_bl_tyre, si_br_tyre, si_f_glass, si_b_glass, si_fl_door, si_fr_door, si_bl_door, si_br_door, si_other_works,si_other_remarks,
                    si_distance,si_mode_of_transportation,si_vehicle_type,si_ag2_id,si_cost_centre,si_sales_order_no,si_ob
                    ) 
                    VALUES (
                        @si_str_id, @si_entryno, @si_date, @si_acc_id, @si_cust_name,@si_add1,@si_add2, @si_remarks,
                        @si_gross_value, @si_net_amount, @si_total, @si_user_id,@si_sales_acc_id,1,@si_salesman_id,1,0,
                        @si_profit,@si_grand_total,@si_commision_acc_id,@si_net_balance,@si_loc_entryno,@si_loc_entryno_only,0,@si_deliverydate,
                        '',10,@si_sum_item,@si_cash_paid_acc,0,0,NULL,0,0,'B2B',
                        'Retail',@si_functiondate,'MINUS','2000-01-26 00:00:00.000','2000-01-26 00:00:00.000','2000-01-26 00:00:00.000',@si_eway_date,0,@si_insert_user_id,
                        '','','','','','','','','','','','','','','','','','','','',@si_other_remarks,
                        '0','Road','Regular',0,0,0,@si_ob
                    );
                    SELECT SCOPE_IDENTITY();";

                        var cmdInf = new SqlCommand(insertInfSql, conn, tran);
                        cmdInf.Parameters.AddWithValue("@si_str_id", 3);
                        int nextEntryNo = 1;
                        using (var getCmd = new SqlCommand("SELECT ISNULL(MAX(si_entryno), 0) + 1 FROM inv_sales_inf where si_str_id=3", conn, tran))
                        {
                            nextEntryNo = Convert.ToInt32(getCmd.ExecuteScalar());
                        }
                        int locEntryNo = 1;
                        using (var getCmd = new SqlCommand("SELECT ISNULL(MAX(CAST(si_loc_entryno AS INT) + 1 ), 1) FROM inv_sales_inf where si_str_id=3 and si_location_id =1", conn, tran))
                        {
                            locEntryNo = Convert.ToInt32(getCmd.ExecuteScalar());
                        }

                        decimal ob = 0;
                        using (var getCmd = new SqlCommand(@"SELECT COALESCE(SUM(at_Dr)-SUM(at_Cr),0) FROM acc_account_transactions WHERE CAST(at_date AS DATE) <= '"+request.date+"' AND at_as_id = "+request.customerId+"", conn, tran))
                        {
                            ob = Convert.ToDecimal(getCmd.ExecuteScalar());
                        }
                        decimal netBalance =ob + request.total;

                        cmdInf.Parameters.AddWithValue("@si_ob", ob);
                        cmdInf.Parameters.AddWithValue("@si_entryno", nextEntryNo);
                        cmdInf.Parameters.AddWithValue("@si_loc_entryno", locEntryNo);
                        cmdInf.Parameters.AddWithValue("@si_loc_entryno_only", locEntryNo);

                        cmdInf.Parameters.AddWithValue("@si_acc_id", request.customerId);
                        string custName = "";
                        string address1 = "";
                        string address2 = "";

                        using (var getCmd = new SqlCommand(
                            "SELECT as_name, as_add1, as_add2 FROM acc_subhead WHERE as_id = @as_id", conn, tran))
                        {
                            getCmd.Parameters.AddWithValue("@as_id", request.customerId);
                            using (var reader = getCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    custName = reader["as_name"]?.ToString() ?? "";
                                    address1 = reader["as_add1"]?.ToString() ?? "";
                                    address2 = reader["as_add2"]?.ToString() ?? "";
                                }
                            }
                        }
                        cmdInf.Parameters.AddWithValue("@si_date", request.date);
                        cmdInf.Parameters.AddWithValue("@si_deliverydate", request.date);
                        cmdInf.Parameters.AddWithValue("@si_cust_name", custName);
                        cmdInf.Parameters.AddWithValue("@si_add1", address1);
                        cmdInf.Parameters.AddWithValue("@si_add2", address2);
                        cmdInf.Parameters.AddWithValue("@si_remarks", "");
                        cmdInf.Parameters.AddWithValue("@si_gross_value", request.total);
                        cmdInf.Parameters.AddWithValue("@si_net_amount", request.total);
                        cmdInf.Parameters.AddWithValue("@si_total", request.total);
                        cmdInf.Parameters.AddWithValue("@si_grand_total", request.total);
                        if (role.Equals("customer", StringComparison.OrdinalIgnoreCase))
                        {
                            int salesmanId = 1;
                            using (var getCmd = new SqlCommand("SELECT as_salesman_id FROM acc_subhead where as_id="+ ledgerId + "", conn, tran))
                            {
                                salesmanId = Convert.ToInt32(getCmd.ExecuteScalar());
                            }
                            cmdInf.Parameters.AddWithValue("@si_commision_acc_id", salesmanId);
                            cmdInf.Parameters.AddWithValue("@si_salesman_id", 0);
                        }
                        else if (role.Equals("marketing executive", StringComparison.OrdinalIgnoreCase))
                        {
                            int salesmanId = 1;
                            using (var getCmd = new SqlCommand("SELECT as_salesman_id FROM acc_subhead where as_id=" + request.customerId + "", conn, tran))
                            {
                                salesmanId = Convert.ToInt32(getCmd.ExecuteScalar());
                            }
                            int serviceId = 0;
                            using (var getCmd = new SqlCommand("SELECT as_isr_id FROM acc_subhead where as_id=" + ledgerId + "", conn, tran))
                            {
                                serviceId = Convert.ToInt32(getCmd.ExecuteScalar());
                            }
                            cmdInf.Parameters.AddWithValue("@si_commision_acc_id", ledgerId);
                            cmdInf.Parameters.AddWithValue("@si_salesman_id", serviceId);
                        }
                        else
                        {
                            cmdInf.Parameters.AddWithValue("@si_commision_acc_id", request.salesmanId == 0 ? -1 : request.salesmanId);
                            cmdInf.Parameters.AddWithValue("@si_salesman_id", 0);
                        }
                        cmdInf.Parameters.AddWithValue("@si_net_balance", netBalance);
                        

                        int userIds = 0;
                        using (var getCmd = new SqlCommand("select gel_user_id from gnl_ecommerce_login where gel_id="+userId+"", conn, tran))
                        {
                            userIds = Convert.ToInt32(getCmd.ExecuteScalar());
                        }
                        cmdInf.Parameters.AddWithValue("@si_user_id", userIds);
                        cmdInf.Parameters.AddWithValue("@si_sales_acc_id", 2);
                        decimal totalProfit = 0;

                        // First pass: calculate profit
                        foreach (var item in request.cartItems)
                        {
                            decimal prate = 0;

                            using (var getCmd = new SqlCommand("SELECT top 1 prate FROM view_stock WHERE ir_id = @ir_id and qty>0", conn, tran))
                            {
                                getCmd.Parameters.AddWithValue("@ir_id", item.itemId);
                                using (var reader = getCmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        prate = reader.GetDecimal(0);
                                    }
                                }
                            }

                            decimal sellingPricePerUnit = item.price;
                            decimal profit = (sellingPricePerUnit - prate) * item.qty;
                            totalProfit += profit;
                        }
                        cmdInf.Parameters.AddWithValue("@si_profit", totalProfit);
                        cmdInf.Parameters.AddWithValue("@si_cash_paid_acc", 1);
                        cmdInf.Parameters.AddWithValue("@si_functiondate", request.date);
                        cmdInf.Parameters.AddWithValue("@si_eway_date", request.date);
                        cmdInf.Parameters.AddWithValue("@si_insert_user_id", userIds);
                        cmdInf.Parameters.AddWithValue("@si_other_remarks", request.status);

                        // count of items in cart
                        int sumItem = request.cartItems.Count;
                        cmdInf.Parameters.AddWithValue("@si_sum_item", sumItem);

                        int salesInfId = Convert.ToInt32(cmdInf.ExecuteScalar());

                        // 2. Insert cart items into inv_sales_par (details)
                        string insertParSql = @"
                        INSERT INTO inv_sales_par(
                            sp_str_id, sp_entryno, sp_ir_id, sp_rate,sp_realrate, sp_qty,sp_uniquecode,sp_cost,sp_prate,sp_realprate,sp_gross_value,sp_net_amount,sp_total,
                            sp_profit,sp_mrp,sp_qty_multi_unit,sp_srate_multiunit,sp_netratesingle,sp_narration1,sp_narration2,sp_narration3
                        ) 
                        VALUES (
                            @sp_str_id, @sp_entryno, @sp_ir_id, @sp_rate,@sp_realrate, @sp_qty,@sp_uniquecode,@sp_cost,@sp_prate,@sp_realprate,@sp_gross_value,@sp_net_amount,@sp_total,
                            @sp_profit,@sp_mrp,@sp_qty_multi_unit,@sp_srate_multiunit,@sp_netratesingle,@sp_narration1,@sp_narration2,@sp_narration3
                        );";

                        foreach (var item in request.cartItems)
                        {
                            decimal uniquecode = 0;
                            decimal cost = 0;
                            decimal prate = 0;
                            decimal realprate = 0;
                            decimal mrp = 0;


                            using (var getCmd = new SqlCommand("SELECT  top 1 uniquecode, Cost, prate, realprate,mrp FROM view_stock WHERE ir_id = @ir_id and qty>0", conn, tran))
                            {
                                getCmd.Parameters.AddWithValue("@ir_id", item.itemId);
                                using (var reader = getCmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        uniquecode = reader.GetDecimal(0);
                                        cost = reader.GetDecimal(1);
                                        prate = reader.GetDecimal(2);
                                        realprate = reader.GetDecimal(3);
                                        mrp = reader.GetDecimal(4);
                                    }
                                }
                            }
                            var cmdPar = new SqlCommand(insertParSql, conn, tran);
                            cmdPar.Parameters.AddWithValue("@sp_str_id", 3);
                            cmdPar.Parameters.AddWithValue("@sp_entryno", nextEntryNo);
                            cmdPar.Parameters.AddWithValue("@sp_ir_id", item.itemId);
                            cmdPar.Parameters.AddWithValue("@sp_rate", item.price);
                            cmdPar.Parameters.AddWithValue("@sp_realrate", item.price);
                            cmdPar.Parameters.AddWithValue("@sp_qty", item.qty);                          
                            cmdPar.Parameters.AddWithValue("@sp_uniquecode", uniquecode);
                            cmdPar.Parameters.AddWithValue("@sp_cost", cost);
                            cmdPar.Parameters.AddWithValue("@sp_prate", prate);
                            cmdPar.Parameters.AddWithValue("@sp_realprate", realprate);
                            cmdPar.Parameters.AddWithValue("@sp_gross_value", item.price * item.qty);
                            cmdPar.Parameters.AddWithValue("@sp_net_amount", item.price * item.qty);
                            cmdPar.Parameters.AddWithValue("@sp_total", item.price * item.qty);
                            decimal sellingPricePerUnit = item.price;
                            decimal profit = (sellingPricePerUnit - prate) * item.qty;
                            cmdPar.Parameters.AddWithValue("@sp_profit", profit);
                            cmdPar.Parameters.AddWithValue("@sp_mrp", item.price);
                            cmdPar.Parameters.AddWithValue("@sp_qty_multi_unit", item.qty);
                            cmdPar.Parameters.AddWithValue("@sp_srate_multiunit", item.price);
                            cmdPar.Parameters.AddWithValue("@sp_netratesingle", item.price);
                            cmdPar.Parameters.AddWithValue("@sp_narration1", item.status);
                            cmdPar.Parameters.AddWithValue("@sp_narration2", item.remarks);
                            //cmdPar.Parameters.AddWithValue("@sp_narration3", item.attachment);
                            string trimmedAttachment = item.attachment;

                            if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(item.attachment))
                            {
                                // remove base URL from full path
                                trimmedAttachment = item.attachment.Replace(baseUrl.TrimEnd('/'), "").Trim();
                            }
                            cmdPar.Parameters.AddWithValue("@sp_narration3", trimmedAttachment);

                            cmdPar.ExecuteNonQuery();
                            // Delete from cart for that user & item
                            using (var cmdDel = new SqlCommand(
                                "update inv_ecommerce_cart set c_customer_id= "+request.customerId+" WHERE c_user_id = @userId AND c_item_id = @itemId",
                                conn, tran))
                            {
                                cmdDel.Parameters.AddWithValue("@userId", userId);
                                cmdDel.Parameters.AddWithValue("@itemId", item.itemId);
                                cmdDel.ExecuteNonQuery();
                            }
                            // Delete from cart for that user & item
                            using (var cmdDel = new SqlCommand(
                                "DELETE FROM inv_ecommerce_cart WHERE c_user_id = @userId AND c_item_id = @itemId",
                                conn, tran))
                            {
                                cmdDel.Parameters.AddWithValue("@userId", userId);
                                cmdDel.Parameters.AddWithValue("@itemId", item.itemId);
                                cmdDel.ExecuteNonQuery();
                            }
                        }
                        string insertStatusSql = @"
                        INSERT INTO inv_sales_order_status (sos_orderId, sos_customerId, sos_status)
                        SELECT si_entryno, si_acc_id, '"+request.status+@"'
                        FROM inv_sales_inf 
                        WHERE si_str_id=3 and si_entryno = @NewOrderNo;";

                        using (var cmdStatus = new SqlCommand(insertStatusSql, conn, tran))
                        {
                            cmdStatus.Parameters.AddWithValue("@NewOrderNo", nextEntryNo);
                            cmdStatus.ExecuteNonQuery();
                        }

                        tran.Commit();
                        string headerSql = @"
                        SELECT si_id, si_entryno, si_acc_id AS customerId, a.as_name AS customerName,
                               si_commision_acc_id AS salesmanId, b.as_name AS salesmanName,
                               si_grand_total AS total, si_date AS date,si_other_remarks as status
                        FROM inv_sales_inf
                        INNER JOIN acc_subhead a ON a.as_id = si_acc_id
                        LEFT JOIN acc_subhead b ON b.as_id = si_commision_acc_id
                        WHERE si_id = @si_id;";

                        string itemsSql = @"
                        SELECT sp_ir_id AS itemId, ir_name AS itemName,
                               sp_qty AS qty, sp_rate AS price,
                               sp_narration1 AS status, sp_narration2 AS remarks, sp_narration3 AS attachment
                        FROM inv_sales_par
                        LEFT JOIN inv_item_reg ON ir_id = sp_ir_id
                        WHERE sp_str_id = 3 AND sp_entryno = @entryno;";

                        // Fetch header
                        CheckoutResponse responseData = null;
                        using (var cmd = new SqlCommand(headerSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@si_id", salesInfId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    responseData = new CheckoutResponse
                                    {
                                        orderId = Convert.ToInt32(reader["si_id"]),
                                        orderNo = Convert.ToInt32(reader["si_entryno"]),
                                        customerId = Convert.ToInt32(reader["customerId"]),
                                        customerName = reader["customerName"]?.ToString(),
                                        salesmanId = reader["salesmanId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["salesmanId"]),
                                        salesmanName = reader["salesmanName"]?.ToString(),
                                        date = reader.GetDateTime(reader.GetOrdinal("date")),
                                        total = reader.GetDecimal(reader.GetOrdinal("total")),
                                        status = reader["status"]?.ToString(),
                                        cartItems = new List<CartItemresponse>()
                                    };
                                }
                            }
                        }

                        // Fetch items
                        using (var cmd = new SqlCommand(itemsSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@entryno", nextEntryNo);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var item = new CartItemresponse
                                    {
                                        itemId = Convert.ToInt32(reader["itemId"]),  // itemId is INT in SQL
                                        itemName = reader["itemName"]?.ToString(),
                                        qty = Convert.ToDecimal(reader["qty"]),      // handles float/double → decimal
                                        price = Convert.ToDecimal(reader["price"]),  // handles float/double → decimal
                                        status = reader["status"]?.ToString(),
                                        remarks = reader["remarks"]?.ToString(),
                                        // ✅ Append BaseUrl if not empty
                                        attachment = string.IsNullOrEmpty(reader["attachment"]?.ToString()) ? ""  : baseUrl.TrimEnd('/') + reader["attachment"].ToString()
                                    };
                                    responseData.cartItems.Add(item);
                                }
                            }
                        }

                        return Ok(new
                        {
                            status = true,
                            statusCode = 200,
                            message = "Checkout completed successfully",
                            data = responseData
                        });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new
                        {
                            status = false,
                            statusCode = 500,
                            message = "Error: " + ex.Message,
                            data = (object)null
                        });
                    }
                }
            }
        }
        ////[Authorize(Roles = UserRoles.EcommerceUser)]
        ////[HttpPost("checkout-cart1")]
        ////public IActionResult CheckoutCart1([FromBody] CheckoutRequest1 request)
        ////{
        ////    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        ////    var ledgerId = User.FindFirst("LedgerId")?.Value;
        ////    var role = User.FindFirst("UserRole")?.Value;
        ////    // ✅ Get BaseUrl from settings
        ////    string baseUrl = ExecuteScalar(
        ////        _connectionString,
        ////        "SELECT ans_status FROM android_settings WHERE ans_name = 'BaseUrl'"
        ////    )?.ToString() ?? "";

        ////    if (string.IsNullOrEmpty(userId))
        ////    {
        ////        return Unauthorized(new
        ////        {
        ////            status = false,
        ////            statusCode = 401,
        ////            message = "Unauthorized: Invalid token."
        ////        });
        ////    }

        ////    using (var conn = new SqlConnection(_connectionString))
        ////    {
        ////        conn.Open();
        ////        using (var tran = conn.BeginTransaction())
        ////        {
        ////            try
        ////            {
        ////                // ✅ Fetch cart items by IDs
        ////                var cartItems = new List<dynamic>();
        ////                string sql = $@"
        ////            SELECT c_id, c_item_id, c_item_qty, c_price, c_remarks, c_attachment 
        ////            FROM inv_ecommerce_cart 
        ////            WHERE c_id IN ({string.Join(",", request.cartIds)})";

        ////                using (var cmd = new SqlCommand(sql, conn, tran))
        ////                using (var reader = cmd.ExecuteReader())
        ////                {
        ////                    while (reader.Read())
        ////                    {
        ////                        cartItems.Add(new
        ////                        {
        ////                            itemId = Convert.ToInt32(reader["c_item_id"]),   // if ID should be int
        ////                            qty = Convert.ToDecimal(reader["c_item_qty"]),
        ////                            price = Convert.ToDecimal(reader["c_price"]),
        ////                            remarks = reader["c_remarks"]?.ToString(),
        ////                            attachment = reader["c_attachment"]?.ToString()
        ////                        });
        ////                    }
        ////                }

        ////                if (cartItems.Count == 0)
        ////                {
        ////                    return BadRequest(new { status = false, statusCode = 400, message = "Cart items not found." });
        ////                }
        ////                // 1. Insert into inv_sales_inf (header)
        ////                string insertInfSql = @"
        ////            INSERT INTO inv_sales_inf(
        ////            si_str_id, si_entryno, si_date, si_acc_id, si_cust_name,si_add1,si_add2, si_remarks,
        ////            si_gross_value, si_net_amount, si_total, si_user_id,si_sales_acc_id,si_from_mobile,si_salesman_id,si_location_id,si_tax_type,
        ////            si_profit,si_grand_total,si_commision_acc_id,si_net_balance,si_loc_entryno, si_loc_entryno_only,si_next_entryno,si_deliverydate,
        ////            si_qtn_no,si_finish,si_sum_item,si_cash_paid_acc,si_lc_id,si_cycr_far,si_credit_note_no,si_damage_entryno,si_return_against_invoiceno,si_alias_name,
        ////            si_rate_type,si_functiondate,si_tax_calculation ,si_finished_date,si_expected_date,si_due_date ,si_eway_date,si_additional_cost,si_insert_user_id ,
        ////            si_f_bumber, si_b_bumber, si_bonnet, si_boot, si_roof, si_fr_light, si_fl_light, si_bl_light, si_br_light, si_fl_tyre, si_fr_tyre, si_bl_tyre, si_br_tyre, si_f_glass, si_b_glass, si_fl_door, si_fr_door, si_bl_door, si_br_door, si_other_works,si_other_remarks,
        ////            si_distance,si_mode_of_transportation,si_vehicle_type,si_ag2_id,si_cost_centre,si_sales_order_no,si_ob
        ////        ) 
        ////        VALUES (
        ////            @si_str_id, @si_entryno, @si_date, @si_acc_id, @si_cust_name,@si_add1,@si_add2, @si_remarks,
        ////            @si_gross_value, @si_net_amount, @si_total, @si_user_id,@si_sales_acc_id,1,0,1,0,
        ////            @si_profit,@si_grand_total,@si_commision_acc_id,@si_net_balance,@si_loc_entryno,@si_loc_entryno_only,0,@si_deliverydate,
        ////            '',10,@si_sum_item,@si_cash_paid_acc,0,0,NULL,0,0,'B2B',
        ////            'Retail',@si_functiondate,'MINUS','2000-01-26 00:00:00.000','2000-01-26 00:00:00.000','2000-01-26 00:00:00.000',@si_eway_date,0,@si_insert_user_id,
        ////            '','','','','','','','','','','','','','','','','','','','',@si_other_remarks,
        ////            '0','Road','Regular',0,0,0,@si_ob
        ////        );
        ////        SELECT SCOPE_IDENTITY();";

        ////                var cmdInf = new SqlCommand(insertInfSql, conn, tran);
        ////                cmdInf.Parameters.AddWithValue("@si_str_id", 3);
        ////                int nextEntryNo = 1;
        ////                using (var getCmd = new SqlCommand("SELECT ISNULL(MAX(si_entryno), 0) + 1 FROM inv_sales_inf where si_str_id=3", conn, tran))
        ////                {
        ////                    nextEntryNo = Convert.ToInt32(getCmd.ExecuteScalar());
        ////                }
        ////                int locEntryNo = 1;
        ////                using (var getCmd = new SqlCommand("SELECT ISNULL(MAX(CAST(si_loc_entryno AS INT) + 1 ), 0) FROM inv_sales_inf where si_str_id=3 and si_location_id =1", conn, tran))
        ////                {
        ////                    locEntryNo = Convert.ToInt32(getCmd.ExecuteScalar());
        ////                }

        ////                decimal ob = 0;
        ////                using (var getCmd = new SqlCommand(@"SELECT COALESCE(SUM(at_Dr)-SUM(at_Cr),0) FROM acc_account_transactions WHERE CAST(at_date AS DATE) <= '" + request.date + "' AND at_as_id = " + request.customerId + "", conn, tran))
        ////                {
        ////                    ob = Convert.ToDecimal(getCmd.ExecuteScalar());
        ////                }
        ////                decimal netBalance = ob + request.total;

        ////                cmdInf.Parameters.AddWithValue("@si_ob", ob);
        ////                cmdInf.Parameters.AddWithValue("@si_entryno", nextEntryNo);
        ////                cmdInf.Parameters.AddWithValue("@si_loc_entryno", locEntryNo);
        ////                cmdInf.Parameters.AddWithValue("@si_loc_entryno_only", locEntryNo);

        ////                cmdInf.Parameters.AddWithValue("@si_acc_id", request.customerId);
        ////                string custName = "";
        ////                string address1 = "";
        ////                string address2 = "";

        ////                using (var getCmd = new SqlCommand(
        ////                    "SELECT as_name, as_add1, as_add2 FROM acc_subhead WHERE as_id = @as_id", conn, tran))
        ////                {
        ////                    getCmd.Parameters.AddWithValue("@as_id", request.customerId);
        ////                    using (var reader = getCmd.ExecuteReader())
        ////                    {
        ////                        if (reader.Read())
        ////                        {
        ////                            custName = reader["as_name"]?.ToString() ?? "";
        ////                            address1 = reader["as_add1"]?.ToString() ?? "";
        ////                            address2 = reader["as_add2"]?.ToString() ?? "";
        ////                        }
        ////                    }
        ////                }
        ////                cmdInf.Parameters.AddWithValue("@si_date", request.date);
        ////                cmdInf.Parameters.AddWithValue("@si_deliverydate", request.date);
        ////                cmdInf.Parameters.AddWithValue("@si_cust_name", custName);
        ////                cmdInf.Parameters.AddWithValue("@si_add1", address1);
        ////                cmdInf.Parameters.AddWithValue("@si_add2", address2);
        ////                cmdInf.Parameters.AddWithValue("@si_remarks", "");
        ////                cmdInf.Parameters.AddWithValue("@si_gross_value", request.total);
        ////                cmdInf.Parameters.AddWithValue("@si_net_amount", request.total);
        ////                cmdInf.Parameters.AddWithValue("@si_total", request.total);
        ////                cmdInf.Parameters.AddWithValue("@si_grand_total", request.total);
        ////                if (role == "customer")
        ////                {
        ////                    int salesmanId = 1;
        ////                    using (var getCmd = new SqlCommand("SELECT as_salesman_id FROM acc_subhead where as_id=" + ledgerId + "", conn, tran))
        ////                    {
        ////                        salesmanId = Convert.ToInt32(getCmd.ExecuteScalar());
        ////                    }
        ////                    cmdInf.Parameters.AddWithValue("@si_commision_acc_id", salesmanId);
        ////                }
        ////                else
        ////                {
        ////                    cmdInf.Parameters.AddWithValue("@si_commision_acc_id", request.salesmanId == 0 ? -1 : request.salesmanId);
        ////                }
        ////                cmdInf.Parameters.AddWithValue("@si_net_balance", netBalance);


        ////                int userIds = 0;
        ////                using (var getCmd = new SqlCommand("select gel_user_id from gnl_ecommerce_login where gel_id=" + userId + "", conn, tran))
        ////                {
        ////                    userIds = Convert.ToInt32(getCmd.ExecuteScalar());
        ////                }
        ////                cmdInf.Parameters.AddWithValue("@si_user_id", userIds);
        ////                cmdInf.Parameters.AddWithValue("@si_sales_acc_id", 2);
        ////                decimal totalProfit = 0;

                        
        ////                cmdInf.Parameters.AddWithValue("@si_profit", totalProfit);
        ////                cmdInf.Parameters.AddWithValue("@si_cash_paid_acc", 1);
        ////                cmdInf.Parameters.AddWithValue("@si_functiondate", request.date);
        ////                cmdInf.Parameters.AddWithValue("@si_eway_date", request.date);
        ////                cmdInf.Parameters.AddWithValue("@si_insert_user_id", userIds);
        ////                cmdInf.Parameters.AddWithValue("@si_other_remarks", request.status);

        ////                // count of items in cart
        ////                int sumItem = request.cartIds.Count;
        ////                cmdInf.Parameters.AddWithValue("@si_sum_item", sumItem);

        ////                int salesInfId = Convert.ToInt32(cmdInf.ExecuteScalar());

        ////                // 2. Insert cart items into inv_sales_par (details)
        ////                string insertParSql = @"
        ////                INSERT INTO inv_sales_par(
        ////                    sp_str_id, sp_entryno, sp_ir_id, sp_rate,sp_realrate, sp_qty,sp_uniquecode,sp_cost,sp_prate,sp_realprate,sp_gross_value,sp_net_amount,sp_total,
        ////                    sp_profit,sp_mrp,sp_qty_multi_unit,sp_srate_multiunit,sp_netratesingle,sp_narration1,sp_narration2,sp_narration3
        ////                ) 
        ////                VALUES (
        ////                    @sp_str_id, @sp_entryno, @sp_ir_id, @sp_rate,@sp_realrate, @sp_qty,@sp_uniquecode,@sp_cost,@sp_prate,@sp_realprate,@sp_gross_value,@sp_net_amount,@sp_total,
        ////                    @sp_profit,@sp_mrp,@sp_qty_multi_unit,@sp_srate_multiunit,@sp_netratesingle,@sp_narration1,@sp_narration2,@sp_narration3
        ////                );";

        ////                // ✅ Loop through the cartItems (with itemId, qty, price, remarks, attachment)
        ////                foreach (var item in cartItems)
        ////                {
        ////                    decimal uniquecode = 0;
        ////                    decimal cost = 0;
        ////                    decimal prate = 0;
        ////                    decimal realprate = 0;
        ////                    decimal mrp = 0;

        ////                    // Fetch stock info
        ////                    using (var getCmd = new SqlCommand(@"
        ////                    SELECT TOP 1 uniquecode, Cost, prate, realprate, mrp 
        ////                    FROM view_stock 
        ////                    WHERE ir_id = @ir_id", conn, tran))
        ////                    {
        ////                        getCmd.Parameters.AddWithValue("@ir_id", item.itemId);
        ////                        using (var reader = getCmd.ExecuteReader())
        ////                        {
        ////                            if (reader.Read())
        ////                            {
        ////                                uniquecode = reader.GetDecimal(0);
        ////                                cost = reader.GetDecimal(1);
        ////                                prate = reader.GetDecimal(2);
        ////                                realprate = reader.GetDecimal(3);
        ////                                mrp = reader.GetDecimal(4);
        ////                            }
        ////                        }
        ////                    }

        ////                    // Insert into sales_par
        ////                    var cmdPar = new SqlCommand(insertParSql, conn, tran);
        ////                    cmdPar.Parameters.AddWithValue("@sp_str_id", 3);
        ////                    cmdPar.Parameters.AddWithValue("@sp_entryno", nextEntryNo);
        ////                    cmdPar.Parameters.AddWithValue("@sp_ir_id", item.itemId);
        ////                    cmdPar.Parameters.AddWithValue("@sp_rate", item.price);
        ////                    cmdPar.Parameters.AddWithValue("@sp_realrate", item.price);
        ////                    cmdPar.Parameters.AddWithValue("@sp_qty", item.qty);
        ////                    cmdPar.Parameters.AddWithValue("@sp_uniquecode", uniquecode);
        ////                    cmdPar.Parameters.AddWithValue("@sp_cost", cost);
        ////                    cmdPar.Parameters.AddWithValue("@sp_prate", prate);
        ////                    cmdPar.Parameters.AddWithValue("@sp_realprate", realprate);
        ////                    cmdPar.Parameters.AddWithValue("@sp_gross_value", item.price * item.qty);
        ////                    cmdPar.Parameters.AddWithValue("@sp_net_amount", item.price * item.qty);
        ////                    cmdPar.Parameters.AddWithValue("@sp_total", item.price * item.qty);

        ////                    decimal sellingPricePerUnit = item.price;
        ////                    decimal profit = (sellingPricePerUnit - prate) * item.qty;
        ////                    cmdPar.Parameters.AddWithValue("@sp_profit", profit);
        ////                    cmdPar.Parameters.AddWithValue("@sp_mrp", mrp);
        ////                    cmdPar.Parameters.AddWithValue("@sp_qty_multi_unit", item.qty);
        ////                    cmdPar.Parameters.AddWithValue("@sp_srate_multiunit", item.price);
        ////                    cmdPar.Parameters.AddWithValue("@sp_netratesingle", item.price);
        ////                    cmdPar.Parameters.AddWithValue("@sp_narration1", "Pending");
        ////                    cmdPar.Parameters.AddWithValue("@sp_narration2", item.remarks ?? "");
        ////                    cmdPar.Parameters.AddWithValue("@sp_narration3", item.attachment ?? "");
        ////                    cmdPar.ExecuteNonQuery();

        ////                    // Update customer id in cart
        ////                    using (var cmdDel = new SqlCommand(
        ////                        "UPDATE inv_ecommerce_cart SET c_customer_id = @custId WHERE c_user_id = @userId AND c_item_id = @itemId",
        ////                        conn, tran))
        ////                    {
        ////                        cmdDel.Parameters.AddWithValue("@custId", request.customerId);
        ////                        cmdDel.Parameters.AddWithValue("@userId", userId);
        ////                        cmdDel.Parameters.AddWithValue("@itemId", item.itemId);
        ////                        cmdDel.ExecuteNonQuery();
        ////                    }

        ////                    // Delete item from cart
        ////                    using (var cmdDel = new SqlCommand(
        ////                        "DELETE FROM inv_ecommerce_cart WHERE c_user_id = @userId AND c_item_id = @itemId",
        ////                        conn, tran))
        ////                    {
        ////                        cmdDel.Parameters.AddWithValue("@userId", userId);
        ////                        cmdDel.Parameters.AddWithValue("@itemId", item.itemId);
        ////                        cmdDel.ExecuteNonQuery();
        ////                    }
        ////                }
        ////                string insertStatusSql = @"
        ////                INSERT INTO inv_sales_order_status (sos_orderId, sos_customerId, sos_status)
        ////                SELECT si_entryno, si_acc_id, '" + request.status + @"'
        ////                FROM inv_sales_inf 
        ////                WHERE si_str_id=3 and si_entryno = @NewOrderNo;";

        ////                using (var cmdStatus = new SqlCommand(insertStatusSql, conn, tran))
        ////                {
        ////                    cmdStatus.Parameters.AddWithValue("@NewOrderNo", nextEntryNo);
        ////                    cmdStatus.ExecuteNonQuery();
        ////                }

        ////                tran.Commit();
        ////                string headerSql = @"
        ////                SELECT si_id, si_entryno, si_acc_id AS customerId, a.as_name AS customerName,
        ////                       si_commision_acc_id AS salesmanId, b.as_name AS salesmanName,
        ////                       si_grand_total AS total, si_date AS date,si_other_remarks as status
        ////                FROM inv_sales_inf
        ////                INNER JOIN acc_subhead a ON a.as_id = si_acc_id
        ////                LEFT JOIN acc_subhead b ON b.as_id = si_commision_acc_id
        ////                WHERE si_id = @si_id;";

        ////                string itemsSql = @"
        ////                SELECT sp_ir_id AS itemId, ir_name AS itemName,
        ////                       sp_qty AS qty, sp_total AS price,
        ////                       sp_narration1 AS status, sp_narration2 AS remarks, sp_narration3 AS attachment
        ////                FROM inv_sales_par
        ////                LEFT JOIN inv_item_reg ON ir_id = sp_ir_id
        ////                WHERE sp_str_id = 3 AND sp_entryno = @entryno;";

        ////                // Fetch header
        ////                CheckoutResponse responseData = null;
        ////                using (var cmd = new SqlCommand(headerSql, conn))
        ////                {
        ////                    cmd.Parameters.AddWithValue("@si_id", salesInfId);
        ////                    using (var reader = cmd.ExecuteReader())
        ////                    {
        ////                        if (reader.Read())
        ////                        {
        ////                            responseData = new CheckoutResponse
        ////                            {
        ////                                orderId = Convert.ToInt32(reader["si_id"]),
        ////                                orderNo = Convert.ToInt32(reader["si_entryno"]),
        ////                                customerId = Convert.ToInt32(reader["customerId"]),
        ////                                customerName = reader["customerName"]?.ToString(),
        ////                                salesmanId = reader["salesmanId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["salesmanId"]),
        ////                                salesmanName = reader["salesmanName"]?.ToString(),
        ////                                date = reader.GetDateTime(reader.GetOrdinal("date")),
        ////                                total = reader.GetDecimal(reader.GetOrdinal("total")),
        ////                                status = reader["status"]?.ToString(),
        ////                                cartItems = new List<CartItemresponse>()
        ////                            };
        ////                        }
        ////                    }
        ////                }

        ////                // Fetch items
        ////                using (var cmd = new SqlCommand(itemsSql, conn))
        ////                {
        ////                    cmd.Parameters.AddWithValue("@entryno", nextEntryNo);
        ////                    using (var reader = cmd.ExecuteReader())
        ////                    {
        ////                        while (reader.Read())
        ////                        {
        ////                            var item = new CartItemresponse
        ////                            {
        ////                                itemId = Convert.ToInt32(reader["itemId"]),  // itemId is INT in SQL
        ////                                itemName = reader["itemName"]?.ToString(),
        ////                                qty = Convert.ToDecimal(reader["qty"]),      // handles float/double → decimal
        ////                                price = Convert.ToDecimal(reader["price"]),  // handles float/double → decimal
        ////                                status = reader["status"]?.ToString(),
        ////                                remarks = reader["remarks"]?.ToString(),
        ////                                // ✅ Append BaseUrl if not empty
        ////                                attachment = string.IsNullOrEmpty(reader["attachment"]?.ToString()) ? "" : baseUrl.TrimEnd('/') + reader["attachment"].ToString()
        ////                            };
        ////                            responseData.cartItems.Add(item);
        ////                        }
        ////                    }
        ////                }

        ////                return Ok(new
        ////                {
        ////                    status = true,
        ////                    statusCode = 200,
        ////                    message = "Checkout completed successfully",
        ////                    data = responseData
        ////                });
        ////            }
        ////            catch (Exception ex)
        ////            {
        ////                tran.Rollback();
        ////                return StatusCode(500, new
        ////                {
        ////                    status = false,
        ////                    statusCode = 500,
        ////                    message = "Error: " + ex.Message,
        ////                    data = (object)null
        ////                });
        ////            }
        ////        }
        ////    }
        ////}
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-checkout")]
        public IActionResult GetCheckoutList(int orderId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // ✅ Get BaseUrl from settings
            string baseUrl = ExecuteScalar( _connectionString,"SELECT ans_status FROM android_settings WHERE ans_name = 'BaseUrl'")?.ToString() ?? "";

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    status = false,
                    statusCode = 401,
                    message = "Unauthorized: Invalid token."
                });
            }

            if (orderId <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "OrderId is required",
                    data = (object)null
                });
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // ✅ parameterized SQL (avoid injection)
                    string headerSql = @"
                    SELECT 
                        si_entryno AS orderNo, 
                        si_grand_total AS totalAmount, 
                        si_acc_id AS customerId, 
                        c.as_name AS customerName,
                        si_commision_acc_id AS salesmanId, 
                        s.as_name AS salesmanName,
                        c.as_rout_id AS RouteId, 
                        r.r_name AS RouteName, 
                        c.as_area_id AS AreaId, 
                        a.area_name AS AreaName,
                        si_date AS [date],
                        SUM(sp_qty) AS totalQty,
                        si_other_remarks As Status
                    FROM inv_sales_inf
                    INNER JOIN inv_sales_par 
                        ON si_entryno = sp_entryno AND si_str_id = sp_str_id
                    INNER JOIN acc_subhead c 
                        ON c.as_id = si_acc_id
                    LEFT JOIN acc_subhead s 
                        ON s.as_id = si_commision_acc_id
                    LEFT JOIN inv_rout_reg r 
                        ON r.r_id = c.as_rout_id
                    LEFT JOIN acc_area a 
                        ON a.area_id = c.as_area_id
                    WHERE si_str_id = 3 AND si_id = @orderId
                    GROUP BY 
                        si_entryno, si_grand_total, si_acc_id, c.as_name, 
                        si_commision_acc_id, s.as_name, 
                        c.as_rout_id, r.r_name, 
                        c.as_area_id, a.area_name, 
                        si_date,si_other_remarks;";

                    var checkoutList = new List<CheckoutResponseByOrderId>();

                    using (var cmd = new SqlCommand(headerSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@orderId", orderId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                checkoutList.Add(new CheckoutResponseByOrderId
                                {
                                    orderNo = Convert.ToInt32(reader["orderNo"]),
                                    totalAmount = Convert.ToDecimal(reader["totalAmount"]),
                                    customerId = Convert.ToInt32(reader["customerId"]),
                                    customerName = reader["customerName"]?.ToString(),
                                    salesmanId = reader["salesmanId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["salesmanId"]),
                                    salesmanName = reader["salesmanName"]?.ToString(),
                                    routeId = reader["RouteId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["RouteId"]),
                                    routeName = reader["RouteName"]?.ToString(),
                                    areaId = reader["AreaId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["AreaId"]),
                                    areaName = reader["AreaName"]?.ToString(),
                                    date = Convert.ToDateTime(reader["date"]),
                                    totalQty = Convert.ToDecimal(reader["totalQty"]),
                                    status = reader["Status"]?.ToString(),
                                    cartItems = new List<CartItemresponseByOrderId>() // ✅ Initialize
                                });
                            }
                        }
                    }
                    string statusSql = @"
                    SELECT sos_status AS status, sos_date AS date
                    FROM inv_sales_order_status
                    WHERE sos_orderId = @orderId
                    ORDER BY sos_date ASC;";

                    foreach (var order in checkoutList)
                    {
                        using (var cmd = new SqlCommand(statusSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@orderId", order.orderNo);

                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    order.statusList.Add(new OrderStatusResponse
                                    {
                                        status = reader["status"]?.ToString(),
                                        date = Convert.ToDateTime(reader["date"])
                                    });
                                }
                            }
                        }
                    }
                    // ✅ Now fetch items for each order (with images)
                    string itemsSql = @"
                    SELECT sp_ir_id AS itemId,
                           ir.ir_name AS itemName,
                           sp_qty AS qty,
                           sp_total AS price,
                           sp_narration1 AS status,
                           sp_narration2 AS remarks,
                           sp_narration3 AS attachment,
                           ans.ans_status + img.ii_image AS Image
                    FROM inv_sales_par sp
                    INNER JOIN inv_item_reg ir ON ir.ir_id = sp.sp_ir_id
                    LEFT JOIN inv_images img 
                           ON img.ii_reference_id = sp.sp_ir_id 
                          AND img.ii_reference_type = 'item'
                    CROSS JOIN android_settings ans
                    WHERE ans.ans_name = 'BaseUrl'
                      AND sp.sp_str_id = 3 
                      AND sp.sp_entryno = @entryno;
                    ";

                    foreach (var order in checkoutList)
                    {
                        using (var cmd = new SqlCommand(itemsSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@entryno", order.orderNo);

                            using (var reader = cmd.ExecuteReader())
                            {
                                // Temporary dictionary to group images per item
                                var itemDict = new Dictionary<int, CartItemresponseByOrderId>();

                                while (reader.Read())
                                {
                                    int itemId = Convert.ToInt32(reader["itemId"]);

                                    if (!itemDict.TryGetValue(itemId, out var cartItem))
                                    {
                                        cartItem = new CartItemresponseByOrderId
                                        {
                                            itemId = itemId,
                                            itemName = reader["itemName"]?.ToString(),
                                            qty = Convert.ToDecimal(reader["qty"]),
                                            price = Convert.ToDecimal(reader["price"]),
                                            status = reader["status"]?.ToString(),
                                            remarks = reader["remarks"]?.ToString(),
                                            // ✅ Append BaseUrl if not empty
                                            attachment = string.IsNullOrEmpty(reader["attachment"]?.ToString()) ? "" : baseUrl.TrimEnd('/') + reader["attachment"].ToString(),
                                            images = new List<string>()
                                        };
                                        itemDict[itemId] = cartItem;
                                    }

                                    if (reader["Image"] != DBNull.Value)
                                    {
                                        var imagePath = reader["Image"].ToString();
                                        if (!string.IsNullOrWhiteSpace(imagePath) && !cartItem.images.Contains(imagePath))
                                        {
                                            cartItem.images.Add(imagePath);
                                        }
                                    }
                                }

                                order.cartItems.AddRange(itemDict.Values);
                            }
                        }
                    }
                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Checkout list fetched successfully",
                        data = checkoutList
                    });
                }
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
       
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-checkout-list")]
        public IActionResult GetCheckoutList(int customerId, int pageNumber = 1, int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // ✅ Get BaseUrl from settings
            string baseUrl = ExecuteScalar(_connectionString, "SELECT ans_status FROM android_settings WHERE ans_name = 'BaseUrl'")?.ToString() ?? "";

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    status = false,
                    statusCode = 401,
                    message = "Unauthorized: Invalid token."
                });
            }
            if (customerId <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "CustomerId is required",
                    data = (object)null
                });
            }
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Count total records for pagination
                    int totalRecords = 0;
                    using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM inv_sales_inf WHERE  si_str_id=3 and si_acc_id="+customerId+"", conn))
                    {
                        totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
                    }

                    int offset = (pageNumber - 1) * pageSize;

                    // Get header list with paging
                    string headerSql = @"
                    SELECT si_id, si_entryno, si_acc_id AS customerId, c.as_name AS customerName,
                           si_commision_acc_id AS salesmanId, s.as_name AS salesmanName,
                           si_grand_total AS total, si_date AS date,si_other_remarks as status
                    FROM inv_sales_inf
                    INNER JOIN acc_subhead c ON c.as_id = si_acc_id
                    LEFT JOIN acc_subhead s ON s.as_id = si_commision_acc_id
                    WHERE si_from_mobile = 1 and si_str_id=3 and si_acc_id=" + customerId+@"
                    ORDER BY si_id DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

                    var checkoutList = new List<CheckoutResponseList>();

                    using (var cmd = new SqlCommand(headerSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@offset", offset);
                        cmd.Parameters.AddWithValue("@pageSize", pageSize);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                checkoutList.Add(new CheckoutResponseList
                                {
                                    orderId = Convert.ToInt32(reader["si_id"]), 
                                    orderNo = Convert.ToInt32(reader["si_entryno"]), // ✅ use entryno
                                    customerId = Convert.ToInt32(reader["customerId"]),
                                    customerName = reader["customerName"]?.ToString(),
                                    salesmanId = Convert.ToInt32(reader["salesmanId"]),
                                    salesmanName = reader["salesmanName"]?.ToString(),
                                    date = reader.GetDateTime(reader.GetOrdinal("date")),
                                    total = reader.GetDecimal(reader.GetOrdinal("total")),
                                    status = reader["status"]?.ToString(),
                                    cartItems = new List<CartItemresponse>()
                                });
                            }
                        }
                    }

                    // Fetch items for each order
                    string itemsSql = @"
                    SELECT sp_ir_id AS itemId, ir_name AS itemName,
                           sp_qty AS qty, sp_total AS price,
                           sp_narration1 AS status, sp_narration2 AS remarks, sp_narration3 AS attachment
                    FROM inv_sales_par
                    INNER JOIN inv_item_reg ON ir_id = sp_ir_id
                    WHERE sp_str_id = 3 AND sp_entryno = @entryno;";

                    foreach (var order in checkoutList)
                    {
                        using (var cmd = new SqlCommand(itemsSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@entryno", order.orderNo); // ✅ correct link
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    order.cartItems.Add(new CartItemresponse
                                    {
                                        itemId = Convert.ToInt32(reader["itemId"]),
                                        itemName = reader["itemName"]?.ToString(),
                                        qty = Convert.ToDecimal(reader["qty"]),      // ✅ safe conversion
                                        price = Convert.ToDecimal(reader["price"]),  // ✅ safe conversion
                                        status = reader["status"]?.ToString(),
                                        remarks = reader["remarks"]?.ToString(),
                                        attachment = string.IsNullOrEmpty(reader["attachment"]?.ToString()) ? "" : baseUrl.TrimEnd('/') + reader["attachment"].ToString()
                                    });
                                }
                            }
                        }
                    }

                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Checkout list fetched successfully",
                        totalRecords,
                        pageNumber,
                        pageSize,
                        data = checkoutList
                    });
                }
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        //[HttpGet("get-ecommerce-sales-orders")]
        //public IActionResult GetSalesOrders(int pageNumber = 1, int pageSize = 10)
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    var role = User.FindFirst("UserRole")?.Value;
        //    var ledgerId = User.FindFirst("LedgerId")?.Value;

        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Unauthorized(new
        //        {
        //            status = false,
        //            statusCode = 401,
        //            message = "Unauthorized: Invalid token."
        //        });
        //    }

        //    try
        //    {
        //        using (var conn = new SqlConnection(_connectionString))
        //        {
        //            conn.Open();

        //            // Build filter based on role
        //            if (role == "storekeeper")
        //            {
        //                using (var cmd = new SqlCommand(
        //                "SELECT as_salesman_id FROM acc_subhead WHERE as_id = @ledgerId", conn))
        //                {
        //                    cmd.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                    var salesmanIdObj = cmd.ExecuteScalar(); // ✅ renamed from result
        //                    if (salesmanIdObj != null && salesmanIdObj != DBNull.Value)
        //                    {
        //                        ledgerId = salesmanIdObj.ToString();
        //                    }
        //                }
        //            }


        //            string filterColumn = role == "customer" ? "si_acc_id" : "si_commision_acc_id";
        //            // 1. Count total records
        //            int totalRecords = 0;
        //            using (var countCmd = new SqlCommand($@"
        //            SELECT COUNT(*) 
        //            FROM inv_sales_inf
        //            LEFT JOIN acc_subhead a ON a.as_id = si_acc_id
        //            LEFT JOIN inv_rout_reg ON r_id = as_rout_id
        //            WHERE si_str_id = 3 AND {filterColumn} = @ledgerId", conn))
        //            {
        //                countCmd.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
        //            }

        //            int offset = (pageNumber - 1) * pageSize;

        //            // 2. Paginated query
        //            string sql = $@"
        //            WITH SalesCTE AS (
        //                  SELECT si_entryno as OrderNo, 
        //                   si_acc_id as CustomerId,
        //                   isnull(a.as_name,'') as CustomerName,
        //                   a.as_rout_id as RouteId,
        //                   isnull(r_name,'') as RouteName,
        //             a.as_area_id as AreaId,
        //                   isnull(area_name,'') as AreaName,
        //             si_commision_acc_id as SalesmanId,
        //             isnull(b.as_name,'') as SalesmanName,
        //                   si_date as Date,
        //                   si_other_remarks as Status,
        //                   si_grand_total as Total
        //            FROM inv_sales_inf
        //            LEFT JOIN acc_subhead a ON a.as_id = si_acc_id
        //         LEFT JOIN acc_subhead b ON b.as_id = si_commision_acc_id
        //            LEFT JOIN inv_rout_reg ON r_id = a.as_rout_id
        //         LEFT JOIN acc_area ON a.as_area_id= area_id
        //            WHERE si_str_id = 3  AND {filterColumn} = @ledgerId
        //            )
        //            SELECT *
        //            FROM SalesCTE
        //            ORDER BY OrderNo DESC
        //            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

        //            var orders = new List<object>();

        //            using (var cmd = new SqlCommand(sql, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                cmd.Parameters.AddWithValue("@offset", offset);
        //                cmd.Parameters.AddWithValue("@pageSize", pageSize);

        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    while (reader.Read())
        //                    {
        //                        orders.Add(new
        //                        {
        //                            OrderNo = reader["OrderNo"],
        //                            CustomerId = reader["CustomerId"],
        //                            CustomerName = reader["CustomerName"],
        //                            RouteId = reader["RouteId"],
        //                            RouteName = reader["RouteName"],
        //                            AreaId = reader["AreaId"],
        //                            AreaName = reader["AreaName"],
        //                            SalesmanId = reader["SalesmanId"],
        //                            SalesmanName = reader["SalesmanName"],
        //                            Date = reader["Date"],
        //                            Status = reader["Status"],
        //                            Total = reader["Total"]
        //                        });
        //                    }
        //                }
        //            }

        //            return Ok(new
        //            {
        //                status = true,
        //                statusCode = 200,
        //                message = "Sales orders fetched successfully",
        //                totalRecords,
        //                pageNumber,
        //                pageSize,
        //                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
        //                data = orders
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = "Error: " + ex.Message,
        //            data = (object)null
        //        });
        //    }
        //}
        //[HttpGet("get-ecommerce-sales-orders")]
        //public IActionResult GetSalesOrders(
        //int pageNumber = 1,
        //int pageSize = 10,
        //DateTime? fromDate = null,
        //DateTime? toDate = null)
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    var role = User.FindFirst("UserRole")?.Value;
        //    var ledgerId = User.FindFirst("LedgerId")?.Value;

        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Unauthorized(new
        //        {
        //            status = false,
        //            statusCode = 401,
        //            message = "Unauthorized: Invalid token."
        //        });
        //    }

        //    try
        //    {
        //        using (var conn = new SqlConnection(_connectionString))
        //        {
        //            conn.Open();

        //            // Handle Store Keeper role
        //            if (role.Equals("storekeeper", StringComparison.OrdinalIgnoreCase) || role.Equals("delivery boy", StringComparison.OrdinalIgnoreCase))
        //            {
        //                using (var cmd = new SqlCommand(
        //                    "SELECT as_salesman_id FROM acc_subhead WHERE as_id = @ledgerId", conn))
        //                {
        //                    cmd.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                    var salesmanIdObj = cmd.ExecuteScalar();
        //                    if (salesmanIdObj != null && salesmanIdObj != DBNull.Value)
        //                    {
        //                        ledgerId = salesmanIdObj.ToString();
        //                    }
        //                }
        //            }

        //            // Set default dates (if not provided)
        //            var from = fromDate ?? DateTime.Today;
        //            var to = toDate ?? DateTime.Today;

        //            string filterColumn = role == "customer" ? "si_acc_id" : "si_commision_acc_id";

        //            // 1. Count total records
        //            int totalRecords = 0;
        //            using (var countCmd = new SqlCommand($@"
        //        SELECT COUNT(*) 
        //        FROM inv_sales_inf
        //        LEFT JOIN acc_subhead a ON a.as_id = si_acc_id
        //        LEFT JOIN inv_rout_reg ON r_id = as_rout_id
        //        WHERE si_str_id = 3 
        //          AND {filterColumn} = @ledgerId
        //          AND CAST(si_date AS date) BETWEEN @fromDate AND @toDate", conn))
        //            {
        //                countCmd.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                countCmd.Parameters.AddWithValue("@fromDate", from);
        //                countCmd.Parameters.AddWithValue("@toDate", to);
        //                totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
        //            }

        //            int offset = (pageNumber - 1) * pageSize;

        //            // 2. Paginated query
        //            string sql = $@"
        //        WITH SalesCTE AS (
        //            SELECT 
        //                si_entryno AS OrderNo, 
        //                si_acc_id AS CustomerId,
        //                ISNULL(a.as_name, '') AS CustomerName,
        //                a.as_rout_id AS RouteId,
        //                ISNULL(r_name, '') AS RouteName,
        //                a.as_area_id AS AreaId,
        //                ISNULL(area_name, '') AS AreaName,
        //                si_commision_acc_id AS SalesmanId,
        //                ISNULL(b.as_name, '') AS SalesmanName,
        //                si_date AS Date,
        //                si_other_remarks AS Status,
        //                si_grand_total AS Total
        //            FROM inv_sales_inf
        //            LEFT JOIN acc_subhead a ON a.as_id = si_acc_id
        //            LEFT JOIN acc_subhead b ON b.as_id = si_commision_acc_id
        //            LEFT JOIN inv_rout_reg ON r_id = a.as_rout_id
        //            LEFT JOIN acc_area ON a.as_area_id = area_id
        //            WHERE si_str_id = 3  
        //              AND {filterColumn} = @ledgerId
        //              AND CAST(si_date AS date) BETWEEN @fromDate AND @toDate
        //        )
        //        SELECT *
        //        FROM SalesCTE
        //        ORDER BY OrderNo DESC
        //        OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

        //            var orders = new List<object>();

        //            using (var cmd = new SqlCommand(sql, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                cmd.Parameters.AddWithValue("@fromDate", from);
        //                cmd.Parameters.AddWithValue("@toDate", to);
        //                cmd.Parameters.AddWithValue("@offset", offset);
        //                cmd.Parameters.AddWithValue("@pageSize", pageSize);

        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    while (reader.Read())
        //                    {
        //                        orders.Add(new
        //                        {
        //                            OrderNo = reader["OrderNo"],
        //                            CustomerId = reader["CustomerId"],
        //                            CustomerName = reader["CustomerName"],
        //                            RouteId = reader["RouteId"],
        //                            RouteName = reader["RouteName"],
        //                            AreaId = reader["AreaId"],
        //                            AreaName = reader["AreaName"],
        //                            SalesmanId = reader["SalesmanId"],
        //                            SalesmanName = reader["SalesmanName"],
        //                            Date = reader["Date"],
        //                            Status = reader["Status"],
        //                            Total = reader["Total"]
        //                        });
        //                    }
        //                }
        //            }

        //            return Ok(new
        //            {
        //                status = true,
        //                statusCode = 200,
        //                message = "Sales orders fetched successfully",
        //                totalRecords,
        //                fromDate = from.ToString("yyyy-MM-dd"),
        //                toDate = to.ToString("yyyy-MM-dd"),
        //                pageNumber,
        //                pageSize,
        //                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
        //                data = orders
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = "Error: " + ex.Message,
        //            data = (object)null
        //        });
        //    }
        //}
        #region multiple salesman
        [HttpGet("get-ecommerce-sales-orders")]
        public IActionResult GetSalesOrders(
        int pageNumber = 1,
        int pageSize = 10,
        DateTime? fromDate = null,
        DateTime? toDate = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst("UserRole")?.Value;
            var ledgerId = User.FindFirst("LedgerId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    status = false,
                    statusCode = 401,
                    message = "Unauthorized: Invalid token."
                });
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    List<string> ledgerIds = new List<string>();

                    // ✅ For Storekeeper or Delivery Boy — fetch all assigned salesman IDs
                    if (role.Equals("storekeeper", StringComparison.OrdinalIgnoreCase) ||
                        role.Equals("delivery boy", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var cmd = new SqlCommand(
                            @"SELECT sa_salesman_id FROM inv_salesman_allocation WHERE sa_ledger_id = @ledgerId", conn))
                        {
                            cmd.Parameters.AddWithValue("@ledgerId", ledgerId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    if (reader["sa_salesman_id"] != DBNull.Value)
                                        ledgerIds.Add(reader["sa_salesman_id"].ToString());
                                }
                            }
                        }

                        // Fallback to own ledger if no sub-salesmen
                        if (ledgerIds.Count == 0)
                            ledgerIds.Add(ledgerId);
                    }
                    else
                    {
                        ledgerIds.Add(ledgerId);
                    }

                    // ✅ Default dates
                    var from = fromDate ?? DateTime.Today;
                    var to = toDate ?? DateTime.Today;

                    string filterColumn = role == "customer" ? "si_acc_id" : "si_commision_acc_id";
                    string ledgerIdList = string.Join(",", ledgerIds.Select(id => $"'{id}'"));
                    int offset = (pageNumber - 1) * pageSize;

                    // ✅ 1. Count total records
                    int totalRecords = 0;
                    using (var countCmd = new SqlCommand($@"
                    SELECT COUNT(*) 
                    FROM inv_sales_inf
                    LEFT JOIN acc_subhead a ON a.as_id = si_acc_id
                    LEFT JOIN inv_rout_reg ON r_id = a.as_rout_id
                    WHERE si_str_id = 3 
                      AND {filterColumn} IN ({ledgerIdList})
                      AND CAST(si_date AS date) BETWEEN @fromDate AND @toDate", conn))
                    {
                        countCmd.Parameters.AddWithValue("@fromDate", from);
                        countCmd.Parameters.AddWithValue("@toDate", to);
                        totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
                    }

                    // ✅ 2. Paginated Query
                    string sql = $@"
                    WITH SalesCTE AS (
                        SELECT 
                            si_id AS OrderId, 
                            si_entryno AS OrderNo, 
                            si_acc_id AS CustomerId,
                            ISNULL(a.as_name, '') AS CustomerName,
                            a.as_rout_id AS RouteId,
                            ISNULL(r_name, '') AS RouteName,
                            a.as_area_id AS AreaId,
                            ISNULL(area_name, '') AS AreaName,
                            si_commision_acc_id AS SalesmanId,
                            ISNULL(b.as_name, '') AS SalesmanName,
                            si_date AS Date,
                            si_other_remarks AS Status,
                            si_grand_total AS Total
                        FROM inv_sales_inf
                        LEFT JOIN acc_subhead a ON a.as_id = si_acc_id
                        LEFT JOIN acc_subhead b ON b.as_id = si_commision_acc_id
                        LEFT JOIN inv_rout_reg ON r_id = a.as_rout_id
                        LEFT JOIN acc_area ON a.as_area_id = area_id
                        WHERE si_str_id = 3  
                          AND {filterColumn} IN ({ledgerIdList})
                          AND CAST(si_date AS date) BETWEEN @fromDate AND @toDate
                    )
                    SELECT *
                    FROM SalesCTE
                    ORDER BY OrderNo DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

                    var orders = new List<object>();

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fromDate", from);
                        cmd.Parameters.AddWithValue("@toDate", to);
                        cmd.Parameters.AddWithValue("@offset", offset);
                        cmd.Parameters.AddWithValue("@pageSize", pageSize);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                orders.Add(new
                                {
                                    OrderId = reader["OrderId"],
                                    OrderNo = reader["OrderNo"],
                                    CustomerId = reader["CustomerId"],
                                    CustomerName = reader["CustomerName"],
                                    RouteId = reader["RouteId"],
                                    RouteName = reader["RouteName"],
                                    AreaId = reader["AreaId"],
                                    AreaName = reader["AreaName"],
                                    SalesmanId = reader["SalesmanId"],
                                    SalesmanName = reader["SalesmanName"],
                                    Date = reader["Date"],
                                    Status = reader["Status"],
                                    Total = reader["Total"]
                                });
                            }
                        }
                    }

                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Sales orders fetched successfully",
                        totalRecords,
                        fromDate = from.ToString("yyyy-MM-dd"),
                        toDate = to.ToString("yyyy-MM-dd"),
                        pageNumber,
                        pageSize,
                        totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                        data = orders
                    });
                }
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
        [HttpGet("get-ecommerce-sales-orders-by-status")]
        public IActionResult GetSalesOrdersByStatus(int pageNumber = 1,int pageSize = 10, DateTime? fromDate = null,DateTime? toDate = null, string status=null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst("UserRole")?.Value;
            var ledgerId = User.FindFirst("LedgerId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    status = false,
                    statusCode = 401,
                    message = "Unauthorized: Invalid token."
                });
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    List<string> ledgerIds = new List<string>();

                    // ✅ For Storekeeper or Delivery Boy — fetch all assigned salesman IDs
                    if (role.Equals("storekeeper", StringComparison.OrdinalIgnoreCase) ||
                        role.Equals("delivery boy", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var cmd = new SqlCommand(
                            @"SELECT sa_salesman_id FROM inv_salesman_allocation WHERE sa_ledger_id = @ledgerId", conn))
                        {
                            cmd.Parameters.AddWithValue("@ledgerId", ledgerId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    if (reader["sa_salesman_id"] != DBNull.Value)
                                        ledgerIds.Add(reader["sa_salesman_id"].ToString());
                                }
                            }
                        }

                        // Fallback to own ledger if no sub-salesmen
                        if (ledgerIds.Count == 0)
                            ledgerIds.Add(ledgerId);
                    }
                    else
                    {
                        ledgerIds.Add(ledgerId);
                    }

                    // ✅ Default dates
                    var from = fromDate ?? DateTime.Today;
                    var to = toDate ?? DateTime.Today;

                    string filterColumn = role == "customer" ? "si_acc_id" : "si_commision_acc_id";
                    string ledgerIdList = string.Join(",", ledgerIds.Select(id => $"'{id}'"));
                    int offset = (pageNumber - 1) * pageSize;

                    // ✅ 1. Count total records
                    int totalRecords = 0;
                    using (var countCmd = new SqlCommand($@"
                    SELECT COUNT(*) 
                    FROM inv_sales_inf
                    LEFT JOIN acc_subhead a ON a.as_id = si_acc_id
                    LEFT JOIN inv_rout_reg ON r_id = a.as_rout_id
                    WHERE si_str_id = 3 and si_other_remarks=@status
                      AND {filterColumn} IN ({ledgerIdList})
                      AND CAST(si_date AS date) BETWEEN @fromDate AND @toDate", conn))
                    {
                        countCmd.Parameters.AddWithValue("@fromDate", from);
                        countCmd.Parameters.AddWithValue("@toDate", to);
                        countCmd.Parameters.AddWithValue("@status", status);
                        totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
                    }

                    // ✅ 2. Paginated Query
                    string sql = $@"
                    WITH SalesCTE AS (
                        SELECT 
                            si_id AS OrderId, 
                            si_entryno AS OrderNo, 
                            si_acc_id AS CustomerId,
                            ISNULL(a.as_name, '') AS CustomerName,
                            a.as_rout_id AS RouteId,
                            ISNULL(r_name, '') AS RouteName,
                            a.as_area_id AS AreaId,
                            ISNULL(area_name, '') AS AreaName,
                            si_commision_acc_id AS SalesmanId,
                            ISNULL(b.as_name, '') AS SalesmanName,
                            si_date AS Date,
                            si_other_remarks AS Status,
                            si_grand_total AS Total
                        FROM inv_sales_inf
                        LEFT JOIN acc_subhead a ON a.as_id = si_acc_id
                        LEFT JOIN acc_subhead b ON b.as_id = si_commision_acc_id
                        LEFT JOIN inv_rout_reg ON r_id = a.as_rout_id
                        LEFT JOIN acc_area ON a.as_area_id = area_id
                        WHERE si_str_id = 3  and si_other_remarks=@status
                          AND {filterColumn} IN ({ledgerIdList})
                          AND CAST(si_date AS date) BETWEEN @fromDate AND @toDate
                    )
                    SELECT *
                    FROM SalesCTE
                    ORDER BY OrderNo DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

                    var orders = new List<object>();

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fromDate", from);
                        cmd.Parameters.AddWithValue("@toDate", to);
                        cmd.Parameters.AddWithValue("@offset", offset);
                        cmd.Parameters.AddWithValue("@pageSize", pageSize);
                        cmd.Parameters.AddWithValue("@status", status);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                orders.Add(new
                                {
                                    OrderId = reader["OrderId"],
                                    OrderNo = reader["OrderNo"],
                                    CustomerId = reader["CustomerId"],
                                    CustomerName = reader["CustomerName"],
                                    RouteId = reader["RouteId"],
                                    RouteName = reader["RouteName"],
                                    AreaId = reader["AreaId"],
                                    AreaName = reader["AreaName"],
                                    SalesmanId = reader["SalesmanId"],
                                    SalesmanName = reader["SalesmanName"],
                                    Date = reader["Date"],
                                    Status = reader["Status"],
                                    Total = reader["Total"]
                                });
                            }
                        }
                    }

                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Sales orders fetched successfully",
                        totalRecords,
                        fromDate = from.ToString("yyyy-MM-dd"),
                        toDate = to.ToString("yyyy-MM-dd"),
                        pageNumber,
                        pageSize,
                        totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                        data = orders
                    });
                }
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

        #endregion


        //[Authorize(Roles = UserRoles.EcommerceUser)]
        //[HttpGet("get-ecommerce-dashboard")]
        //public IActionResult GetEcommerceDashboard()
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    var role = User.FindFirst("UserRole")?.Value;
        //    var ledgerId = User.FindFirst("LedgerId")?.Value;

        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Unauthorized(new
        //        {
        //            status = false,
        //            statusCode = 401,
        //            message = "Unauthorized: Invalid token."
        //        });
        //    }

        //    try
        //    {
        //        using (var conn = new SqlConnection(_connectionString))
        //        {
        //            conn.Open();
        //            if (role == "storekeeper")
        //            {
        //                using (var cmd = new SqlCommand(
        //                "SELECT as_salesman_id FROM acc_subhead WHERE as_id = @ledgerId", conn))
        //                {
        //                    cmd.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                    var salesmanIdObj = cmd.ExecuteScalar(); // ✅ renamed from result
        //                    if (salesmanIdObj != null && salesmanIdObj != DBNull.Value)
        //                    {
        //                        ledgerId = salesmanIdObj.ToString();
        //                    }
        //                }
        //            }


        //            string filterColumn = role == "customer" ? "si_acc_id" : "si_commision_acc_id";

        //            // 1. Get total count
        //            int allProducts = 0;
        //            using (var cmdTotal = new SqlCommand($@"
        //            SELECT COUNT(*) 
        //            FROM inv_sales_inf
        //            WHERE si_str_id = 3 AND {filterColumn} = @ledgerId", conn))
        //            {
        //                cmdTotal.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                allProducts = Convert.ToInt32(cmdTotal.ExecuteScalar());
        //            }

        //            // 2. Get fixed status counts
        //            string sql = $@"
        //            WITH StatusList AS (
        //                SELECT 'Pending' AS Status
        //                UNION ALL SELECT 'Accepted'
        //                UNION ALL SELECT 'Billed'
        //                UNION ALL SELECT 'Processing'
        //                UNION ALL SELECT 'Packed'
        //                UNION ALL SELECT 'Dispatched'
        //                UNION ALL SELECT 'Delivered'
        //            )
        //            SELECT s.Status, ISNULL(COUNT(i.si_id), 0) AS TotalCount
        //            FROM StatusList s
        //            LEFT JOIN inv_sales_inf i 
        //                   ON i.si_other_remarks = s.Status
        //                  AND i.si_str_id = 3
        //                  AND {filterColumn} = @ledgerId
        //            GROUP BY s.Status
        //            ORDER BY s.Status;";

        //            var result = new List<object>();
        //            using (var cmd = new SqlCommand(sql, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    while (reader.Read())
        //                    {
        //                        result.Add(new
        //                        {
        //                            Status = reader["Status"].ToString(),
        //                            TotalCount = Convert.ToInt32(reader["TotalCount"])
        //                        });
        //                    }
        //                }
        //            }

        //            return Ok(new
        //            {
        //                status = true,
        //                statusCode = 200,
        //                message = "Dashboard data fetched successfully",
        //                allProducts,
        //                data = result
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = "Error: " + ex.Message,
        //            data = (object)null
        //        });
        //    }
        //}
        //[Authorize(Roles = UserRoles.EcommerceUser)]
        //[HttpGet("get-ecommerce-dashboard")]
        //public IActionResult GetEcommerceDashboard(DateTime? fromDate = null, DateTime? toDate = null)
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    var role = User.FindFirst("UserRole")?.Value;
        //    var ledgerId = User.FindFirst("LedgerId")?.Value;

        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Unauthorized(new
        //        {
        //            status = false,
        //            statusCode = 401,
        //            message = "Unauthorized: Invalid token."
        //        });
        //    }

        //    try
        //    {
        //        using (var conn = new SqlConnection(_connectionString))
        //        {
        //            conn.Open();

        //            // Store keeper adjustment
        //            if (role.Equals("storekeeper", StringComparison.OrdinalIgnoreCase) ||role.Equals("delivery boy", StringComparison.OrdinalIgnoreCase))
        //            {
        //                using (var cmd = new SqlCommand(
        //                    "SELECT as_salesman_id FROM acc_subhead WHERE as_id = @ledgerId", conn))
        //                {
        //                    cmd.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                    var salesmanIdObj = cmd.ExecuteScalar();
        //                    if (salesmanIdObj != null && salesmanIdObj != DBNull.Value)
        //                    {
        //                        ledgerId = salesmanIdObj.ToString();
        //                    }
        //                }
        //            }

        //            string filterColumn = role == "customer" ? "si_acc_id" : "si_commision_acc_id";

        //            // Set default dates (today if not provided)
        //            var from = fromDate ?? DateTime.Today;
        //            var to = toDate ?? DateTime.Today;

        //            // 1. Get total count (with date filter)
        //            int allProducts = 0;
        //            using (var cmdTotal = new SqlCommand($@"
        //        SELECT COUNT(*) 
        //        FROM inv_sales_inf
        //        WHERE si_str_id = 3 
        //          AND {filterColumn} = @ledgerId
        //          AND CAST(si_date AS date) BETWEEN @fromDate AND @toDate", conn))
        //            {
        //                cmdTotal.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                cmdTotal.Parameters.AddWithValue("@fromDate", from);
        //                cmdTotal.Parameters.AddWithValue("@toDate", to);
        //                allProducts = Convert.ToInt32(cmdTotal.ExecuteScalar());
        //            }

        //            // 2. Get status counts (with date filter)
        //            string sql = $@"
        //        WITH StatusList AS (
        //            SELECT 'Pending' AS Status
        //            UNION ALL SELECT 'Accepted'
        //            UNION ALL SELECT 'Billed'
        //            --UNION ALL SELECT 'Processing'
        //            --UNION ALL SELECT 'Packed'
        //            --UNION ALL SELECT 'Dispatched'
        //            UNION ALL SELECT 'Delivered'
        //        )
        //        SELECT s.Status, ISNULL(COUNT(i.si_id), 0) AS TotalCount
        //        FROM StatusList s
        //        LEFT JOIN inv_sales_inf i 
        //            ON i.si_other_remarks = s.Status
        //           AND i.si_str_id = 3
        //           AND {filterColumn} = @ledgerId
        //           AND CAST(i.si_date AS date) BETWEEN @fromDate AND @toDate
        //        GROUP BY s.Status
        //        ORDER BY s.Status;";

        //            var result = new List<object>();
        //            using (var cmd = new SqlCommand(sql, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@ledgerId", ledgerId);
        //                cmd.Parameters.AddWithValue("@fromDate", from);
        //                cmd.Parameters.AddWithValue("@toDate", to);

        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    while (reader.Read())
        //                    {
        //                        result.Add(new
        //                        {
        //                            Status = reader["Status"].ToString(),
        //                            TotalCount = Convert.ToInt32(reader["TotalCount"])
        //                        });
        //                    }
        //                }
        //            }

        //            return Ok(new
        //            {
        //                status = true,
        //                statusCode = 200,
        //                message = "Dashboard data fetched successfully",
        //                allProducts,
        //                fromDate = from.ToString("yyyy-MM-dd"),
        //                toDate = to.ToString("yyyy-MM-dd"),
        //                data = result
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = "Error: " + ex.Message,
        //            data = (object)null
        //        });
        //    }
        //}
        #region Multiple salesaman
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-ecommerce-dashboard")]
        public IActionResult GetEcommerceDashboard(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst("UserRole")?.Value;
            var ledgerId = User.FindFirst("LedgerId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    status = false,
                    statusCode = 401,
                    message = "Unauthorized: Invalid token."
                });
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    List<string> ledgerIds = new List<string>();

                    // ✅ If Storekeeper or Delivery Boy — get all salesmen assigned to them
                    if (role.Equals("storekeeper", StringComparison.OrdinalIgnoreCase) ||
                        role.Equals("delivery boy", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var cmd = new SqlCommand(
                            @"SELECT sa_salesman_id FROM inv_salesman_allocation WHERE sa_ledger_id= @ledgerId", conn))
                        {
                            cmd.Parameters.AddWithValue("@ledgerId", ledgerId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    if (reader["sa_salesman_id"] != DBNull.Value)
                                        ledgerIds.Add(reader["sa_salesman_id"].ToString());
                                }
                            }
                        }

                        // fallback to their own ledger if no sub-salesman found
                        if (ledgerIds.Count == 0)
                            ledgerIds.Add(ledgerId);
                    }
                    else
                    {
                        ledgerIds.Add(ledgerId);
                    }

                    string filterColumn = role == "customer" ? "si_acc_id" : "si_commision_acc_id";

                    // ✅ Default dates
                    var from = fromDate ?? DateTime.Today;
                    var to = toDate ?? DateTime.Today;

                    // ✅ Create table-valued string for IN clause
                    string ledgerIdList = string.Join(",", ledgerIds.Select(id => $"'{id}'"));

                    // ✅ 1. Get total count (with all ledger IDs)
                    int allProducts = 0;
                    using (var cmdTotal = new SqlCommand($@"
                SELECT COUNT(*) 
                FROM inv_sales_inf 
                WHERE si_str_id = 3 
                AND {filterColumn} IN ({ledgerIdList})
                AND CAST(si_date AS date) BETWEEN @fromDate AND @toDate", conn))
                    {
                        cmdTotal.Parameters.AddWithValue("@fromDate", from);
                        cmdTotal.Parameters.AddWithValue("@toDate", to);
                        allProducts = Convert.ToInt32(cmdTotal.ExecuteScalar());
                    }

                    // ✅ 2. Get Status-wise counts (aggregate for all salesmen)
                    string sql = $@"
                WITH StatusList AS (
                    SELECT 'Pending' AS Status UNION ALL
                    SELECT 'Accepted' UNION ALL
                    SELECT 'Billed' UNION ALL
                    SELECT 'Delivered'
                )
                SELECT s.Status, ISNULL(COUNT(i.si_id), 0) AS TotalCount
                FROM StatusList s
                LEFT JOIN inv_sales_inf i
                    ON i.si_other_remarks = s.Status
                    AND i.si_str_id = 3
                    AND {filterColumn} IN ({ledgerIdList})
                    AND CAST(i.si_date AS date) BETWEEN @fromDate AND @toDate
                GROUP BY s.Status
                ORDER BY s.Status;";

                    var result = new List<object>();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fromDate", from);
                        cmd.Parameters.AddWithValue("@toDate", to);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new
                                {
                                    Status = reader["Status"].ToString(),
                                    TotalCount = Convert.ToInt32(reader["TotalCount"])
                                });
                            }
                        }
                    }

                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Dashboard data fetched successfully",
                        allProducts,
                        fromDate = from.ToString("yyyy-MM-dd"),
                        toDate = to.ToString("yyyy-MM-dd"),
                        data = result
                    });
                }
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

        #endregion

        //[HttpGet("get-ecommerce-tv-app")]
        //public IActionResult GetEcommerceTvApp(int salesmanId = 0, string? status = null, DateTime? date = null, int pageNumber = 1, int pageSize = 10)
        //{
        //    try
        //    {
        //        using (var conn = new SqlConnection(_connectionString))
        //        {
        //            conn.Open();

        //            int offset = (pageNumber - 1) * pageSize;

        //            string sql = @"
        //        -- Paginated filtered data
        //        WITH SalesCTE AS (
        //            SELECT 
        //                si_id AS OrderId,
        //                si_entryno AS OrderNo, 
        //                si_acc_id AS CustomerId,
        //                ISNULL(cust.as_name,'') AS CustomerName,
        //                cust.as_rout_id AS RouteId,
        //                ISNULL(r.r_name,'') AS RouteName,
        //                cust.as_area_id AS AreaId,
        //                ISNULL(ar.area_name,'') AS AreaName,
        //                si_commision_acc_id AS SalesmanId,
        //                ISNULL(sales.as_name,'') AS SalesmanName,
        //                si_date AS Date,
        //                ISNULL(si_other_remarks,'') AS Status,
        //                si_grand_total AS Total
        //            FROM inv_sales_inf s
        //            LEFT JOIN acc_subhead cust ON cust.as_id = si_acc_id
        //            LEFT JOIN acc_subhead sales ON sales.as_id = si_commision_acc_id
        //            LEFT JOIN inv_rout_reg r ON r.r_id = cust.as_rout_id
        //            LEFT JOIN acc_area ar ON ar.area_id = cust.as_area_id
        //            WHERE si_str_id = 13
        //              AND (@salesmanId = 0 OR si_commision_acc_id = @salesmanId)
        //              AND (@status IS NULL OR @status = '' OR si_other_remarks = @status)
        //              AND (@date IS NULL OR CAST(si_date AS date) = CAST(@date AS date))
        //        )
        //        SELECT * FROM SalesCTE
        //        ORDER BY OrderNo DESC
        //        OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;

        //        -- Total count of distinct salesmen (filtered)
        //        SELECT COUNT(DISTINCT si_commision_acc_id)
        //        FROM inv_sales_inf
        //        WHERE si_str_id = 13
        //          AND (@salesmanId = 0 OR si_commision_acc_id = @salesmanId)
        //          AND (@status IS NULL OR @status = '' OR si_other_remarks = @status)
        //          AND (@date IS NULL OR CAST(si_date AS date) = CAST(@date AS date));

        //        -- Constant summary (not filtered)
        //        SELECT 
        //            COUNT(*) AS [All],
        //            SUM(CASE WHEN si_other_remarks = 'Pending' THEN 1 ELSE 0 END) AS Pending,
        //            SUM(CASE WHEN si_other_remarks = 'Accepted' THEN 1 ELSE 0 END) AS Accepted,
        //            SUM(CASE WHEN si_other_remarks = 'Billed' THEN 1 ELSE 0 END) AS Billed,
        //            SUM(CASE WHEN si_other_remarks = 'Dispatched' THEN 1 ELSE 0 END) AS Dispatched,
        //            SUM(CASE WHEN si_other_remarks = 'Delivered' THEN 1 ELSE 0 END) AS Delivered
        //        FROM inv_sales_inf
        //        WHERE si_str_id = 13;
        //    ";

        //            var orders = new List<dynamic>();
        //            int totalRecords = 0;
        //            object statusSummary = null;

        //            using (var cmd = new SqlCommand(sql, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@salesmanId", salesmanId);
        //                cmd.Parameters.AddWithValue("@status", (object?)status ?? DBNull.Value);
        //                cmd.Parameters.AddWithValue("@date", (object?)date ?? DBNull.Value);
        //                cmd.Parameters.AddWithValue("@offset", offset);
        //                cmd.Parameters.AddWithValue("@pageSize", pageSize);

        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    // 1. Orders
        //                    while (reader.Read())
        //                    {
        //                        orders.Add(new
        //                        {
        //                            OrderId = reader["OrderId"],
        //                            OrderNo = reader["OrderNo"],
        //                            CustomerId = reader["CustomerId"],
        //                            CustomerName = reader["CustomerName"],
        //                            RouteId = reader["RouteId"],
        //                            RouteName = reader["RouteName"],
        //                            AreaId = reader["AreaId"],
        //                            AreaName = reader["AreaName"],
        //                            SalesmanId = reader["SalesmanId"],
        //                            SalesmanName = reader["SalesmanName"],
        //                            Date = reader["Date"],
        //                            Status = reader["Status"],
        //                            Total = reader["Total"]
        //                        });
        //                    }

        //                    // 2. Total count (filtered)
        //                    if (reader.NextResult() && reader.Read())
        //                    {
        //                        totalRecords = Convert.ToInt32(reader[0]);
        //                    }

        //                    // 3. Status summary (constant, not filtered)
        //                    if (reader.NextResult() && reader.Read())
        //                    {
        //                        statusSummary = new
        //                        {
        //                            All = reader["All"],
        //                            Pending = reader["Pending"],
        //                            Accepted = reader["Accepted"],
        //                            Billed = reader["Billed"],
        //                            Dispatched = reader["Dispatched"],
        //                            Delivered = reader["Delivered"]
        //                        };
        //                    }
        //                }
        //            }

        //            // Group by Salesman
        //            var groupedData = orders
        //                .GroupBy(o => new { o.SalesmanId, o.SalesmanName })
        //                .Select(g => new
        //                {
        //                    SalesmanId = g.Key.SalesmanId,
        //                    SalesmanName = g.Key.SalesmanName,
        //                    Orders = g.ToList()
        //                });

        //            return Ok(new
        //            {
        //                status = true,
        //                statusCode = 200,
        //                message = "Sales orders fetched successfully",
        //                totalRecords,
        //                pageNumber,
        //                pageSize,
        //                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
        //                summary = statusSummary, // always constant for all data
        //                data = groupedData
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = "Error: " + ex.Message,
        //            data = (object)null
        //        });
        //    }
        //}
        //[Authorize(Roles = UserRoles.EcommerceUser)]
        //[HttpGet("get-ecommerce-tv-app")]
        //public async Task<IActionResult> GetEcommerceTvApp(int salesmanId = 0, string? status = null, DateTime? date = null)
        //{
        //    try
        //    {
        //        using (var conn = new SqlConnection(_connectionString))
        //        {
        //            conn.Open();

        //            string sql = @"
        //            -- Filtered data (no pagination, no date filter)
        //            WITH SalesCTE AS (
        //                SELECT 
        //                    ROW_NUMBER() OVER (
        //                        PARTITION BY si_commision_acc_id    
        //                        ORDER BY si_entryno DESC
        //                    ) AS SlNo,
        //                    si_id AS OrderId,
        //                    si_entryno AS OrderNo, 
        //                    si_acc_id AS CustomerId,
        //                    ISNULL(cust.as_name,'') AS CustomerName,
        //                    cust.as_rout_id AS RouteId,
        //                    ISNULL(r.r_name,'') AS RouteName,
        //                    cust.as_area_id AS AreaId,
        //                    ISNULL(ar.area_name,'') AS AreaName,
        //                    si_commision_acc_id AS SalesmanId,
        //                    ISNULL(sales.as_name,'') AS SalesmanName,
        //                    si_date AS Date,
        //                    ISNULL(si_other_remarks,'') AS Status,
        //                    si_grand_total AS Total,
        //                 (SELECT TOP 1 sos_date 
        //                 FROM inv_sales_order_status sos 
        //                 WHERE sos.sos_orderId = s.si_entryno AND sos.sos_status = 'Pending' 
        //                 ORDER BY sos.sos_date ASC) AS OrderTime,

        //                -- Latest ""Accepted"" time
        //                (SELECT TOP 1 sos_date 
        //                 FROM inv_sales_order_status sos 
        //                 WHERE sos.sos_orderId = s.si_entryno AND sos.sos_status = 'Accepted' 
        //                 ORDER BY sos.sos_date ASC) AS AcceptedTime,

        //                -- Latest ""Billed"" time
        //                (SELECT TOP 1 sos_date 
        //                 FROM inv_sales_order_status sos 
        //                 WHERE sos.sos_orderId = s.si_entryno AND sos.sos_status = 'Billed' 
        //                 ORDER BY sos.sos_date ASC) AS BilledTime
        //                FROM inv_sales_inf s
        //                LEFT JOIN acc_subhead cust ON cust.as_id = si_acc_id
        //                LEFT JOIN acc_subhead sales ON sales.as_id = si_commision_acc_id
        //                LEFT JOIN inv_rout_reg r ON r.r_id = cust.as_rout_id
        //                LEFT JOIN acc_area ar ON ar.area_id = cust.as_area_id
        //                WHERE si_str_id = 3
        //                  AND (@salesmanId = 0 OR si_commision_acc_id = @salesmanId)
        //                  AND (@status IS NULL OR @status = '' OR si_other_remarks = @status)
        //                  AND (@date IS NULL OR CAST(si_date AS date) = CAST(@date AS date))
        //            )
        //            SELECT * FROM SalesCTE
        //            ORDER BY SalesmanName Asc;

        //            -- Total count of distinct salesmen (filtered)
        //            SELECT COUNT(DISTINCT si_commision_acc_id)
        //            FROM inv_sales_inf
        //            WHERE si_str_id = 3
        //              AND (@salesmanId = 0 OR si_commision_acc_id = @salesmanId)
        //              AND (@status IS NULL OR @status = '' OR si_other_remarks = @status)
        //              AND (@date IS NULL OR CAST(si_date AS date) = CAST(@date AS date));

        //            -- Constant summary (not filtered)
        //            SELECT 
        //                COUNT(*) AS [All],
        //                SUM(CASE WHEN si_other_remarks = 'Pending' THEN 1 ELSE 0 END) AS Pending,
        //                SUM(CASE WHEN si_other_remarks = 'Accepted' THEN 1 ELSE 0 END) AS Accepted,
        //                SUM(CASE WHEN si_other_remarks = 'Billed' THEN 1 ELSE 0 END) AS Billed,
        //                SUM(CASE WHEN si_other_remarks = 'Dispatched' THEN 1 ELSE 0 END) AS Dispatched,
        //                SUM(CASE WHEN si_other_remarks = 'Delivered' THEN 1 ELSE 0 END) AS Delivered
        //            FROM inv_sales_inf
        //            WHERE si_str_id = 3;
        //        ";

        //            var orders = new List<dynamic>();
        //            int totalRecords = 0;
        //            object statusSummary = null;

        //            using (var cmd = new SqlCommand(sql, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@salesmanId", salesmanId);
        //                cmd.Parameters.AddWithValue("@status", (object?)status ?? DBNull.Value);
        //                cmd.Parameters.AddWithValue("@date", (object?)date ?? DBNull.Value);

        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    // 1. Orders
        //                    while (reader.Read())
        //                    {
        //                        orders.Add(new
        //                        {
        //                            SlNo = reader["SlNo"],
        //                            OrderId = reader["OrderId"],
        //                            OrderNo = reader["OrderNo"],
        //                            CustomerId = reader["CustomerId"],
        //                            CustomerName = reader["CustomerName"],
        //                            RouteId = reader["RouteId"],
        //                            RouteName = reader["RouteName"],
        //                            AreaId = reader["AreaId"],
        //                            AreaName = reader["AreaName"],
        //                            SalesmanId = reader["SalesmanId"],
        //                            SalesmanName = reader["SalesmanName"],
        //                            Date = reader["Date"],
        //                            Status = reader["Status"],
        //                            Total = reader["Total"],
        //                            // 🕒 Add new time fields (handle DBNull or empty string)
        //                            OrderTime = reader["OrderTime"] == DBNull.Value ? "" : reader["OrderTime"].ToString(),
        //                            AcceptedTime = reader["AcceptedTime"] == DBNull.Value ? "" : reader["AcceptedTime"].ToString(),
        //                            BilledTime = reader["BilledTime"] == DBNull.Value ? "" : reader["BilledTime"].ToString()
        //                        });
        //                    }

        //                    // 2. Total count (filtered)
        //                    if (reader.NextResult() && reader.Read())
        //                    {
        //                        totalRecords = Convert.ToInt32(reader[0]);
        //                    }

        //                    // 3. Status summary (constant, not filtered)
        //                    if (reader.NextResult() && reader.Read())
        //                    {
        //                        statusSummary = new
        //                        {
        //                            All = reader["All"],
        //                            Pending = reader["Pending"],
        //                            Accepted = reader["Accepted"],
        //                            Billed = reader["Billed"],
        //                            Dispatched = reader["Dispatched"],
        //                            Delivered = reader["Delivered"]
        //                        };
        //                    }
        //                }
        //            }

        //            // Group by Salesman
        //            var groupedData = orders
        //                .GroupBy(o => new { o.SalesmanId, o.SalesmanName })
        //                .Select(g => new
        //                {
        //                    SalesmanId = g.Key.SalesmanId,
        //                    SalesmanName = g.Key.SalesmanName,
        //                    Orders = g.ToList()
        //                });
        //            await _hubContext.Clients.All.SendAsync("ReceiveEcommerceUpdate", groupedData);
        //            return Ok(new
        //            {
        //                status = true,
        //                statusCode = 200,
        //                message = "Sales orders fetched successfully",
        //                totalRecords,
        //                summary = statusSummary, // always constant for all data
        //                data = groupedData
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = "Error: " + ex.Message,
        //            data = (object)null
        //        });
        //    }
        //}
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-ecommerce-tv-app")]
        public async Task<IActionResult> GetEcommerceTvApp(int salesmanId = 0, string? status = null, DateTime? date = null)
        {
            try
            {
                // ✅ If no date passed, use current date
                if (date == null)
                    date = DateTime.Now.Date;

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
            WITH SalesCTE AS (
                SELECT 
                    ROW_NUMBER() OVER (
                        PARTITION BY si_commision_acc_id    
                        ORDER BY si_entryno DESC
                    ) AS SlNo,
                    si_id AS OrderId,
                    si_entryno AS OrderNo, 
                    si_acc_id AS CustomerId,
                    ISNULL(cust.as_name,'') AS CustomerName,
                    cust.as_rout_id AS RouteId,
                    ISNULL(r.r_name,'') AS RouteName,
                    cust.as_area_id AS AreaId,
                    ISNULL(ar.area_name,'') AS AreaName,
                    si_commision_acc_id AS SalesmanId,
                    ISNULL(sales.as_name,'') AS SalesmanName,
                    si_date AS Date,
                    ISNULL(si_other_remarks,'') AS Status,
                    si_grand_total AS Total,

                    (SELECT TOP 1 sos_date 
                     FROM inv_sales_order_status sos 
                     WHERE sos.sos_orderId = s.si_entryno AND sos.sos_status = 'Pending' 
                     ORDER BY sos.sos_date ASC) AS OrderTime,

                    (SELECT TOP 1 sos_date 
                     FROM inv_sales_order_status sos 
                     WHERE sos.sos_orderId = s.si_entryno AND sos.sos_status = 'Accepted' 
                     ORDER BY sos.sos_date ASC) AS AcceptedTime,

                    (SELECT TOP 1 sos_date 
                     FROM inv_sales_order_status sos 
                     WHERE sos.sos_orderId = s.si_entryno AND sos.sos_status = 'Billed' 
                     ORDER BY sos.sos_date ASC) AS BilledTime
                FROM inv_sales_inf s
                LEFT JOIN acc_subhead cust ON cust.as_id = si_acc_id
                LEFT JOIN acc_subhead sales ON sales.as_id = si_commision_acc_id
                LEFT JOIN inv_rout_reg r ON r.r_id = cust.as_rout_id
                LEFT JOIN acc_area ar ON ar.area_id = cust.as_area_id
                WHERE si_str_id = 3
                  AND (@salesmanId = 0 OR si_commision_acc_id = @salesmanId)
                  AND (@status IS NULL OR @status = '' OR si_other_remarks = @status)
                  AND CAST(si_date AS date) = CAST(@date AS date)
            )
            SELECT * FROM SalesCTE
            ORDER BY SalesmanName ASC;

            SELECT COUNT(DISTINCT si_commision_acc_id)
            FROM inv_sales_inf
            WHERE si_str_id = 3
              AND (@salesmanId = 0 OR si_commision_acc_id = @salesmanId)
              AND (@status IS NULL OR @status = '' OR si_other_remarks = @status)
              AND CAST(si_date AS date) = CAST(@date AS date);

            SELECT 
                COUNT(*) AS [All],
                SUM(CASE WHEN si_other_remarks = 'Pending' THEN 1 ELSE 0 END) AS Pending,
                SUM(CASE WHEN si_other_remarks = 'Accepted' THEN 1 ELSE 0 END) AS Accepted,
                SUM(CASE WHEN si_other_remarks = 'Billed' THEN 1 ELSE 0 END) AS Billed,
                --SUM(CASE WHEN si_other_remarks = 'Dispatched' THEN 1 ELSE 0 END) AS Dispatched,
                SUM(CASE WHEN si_other_remarks = 'Delivered' THEN 1 ELSE 0 END) AS Delivered
            FROM inv_sales_inf
            WHERE si_str_id = 3
              AND CAST(si_date AS date) = CAST(@date AS date);
            ";

                    var orders = new List<dynamic>();
                    int totalRecords = 0;
                    object statusSummary = null;

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@salesmanId", salesmanId);
                        cmd.Parameters.AddWithValue("@status", (object?)status ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@date", date);

                        using (var reader = cmd.ExecuteReader())
                        {
                            // 1️⃣ Orders
                            while (reader.Read())
                            {
                                orders.Add(new
                                {
                                    SlNo = reader["SlNo"],
                                    OrderId = reader["OrderId"],
                                    OrderNo = reader["OrderNo"],
                                    CustomerId = reader["CustomerId"],
                                    CustomerName = reader["CustomerName"],
                                    RouteId = reader["RouteId"],
                                    RouteName = reader["RouteName"],
                                    AreaId = reader["AreaId"],
                                    AreaName = reader["AreaName"],
                                    SalesmanId = reader["SalesmanId"],
                                    SalesmanName = reader["SalesmanName"],
                                    Date = reader["Date"],
                                    Status = reader["Status"],
                                    Total = reader["Total"],
                                    OrderTime = reader["OrderTime"] == DBNull.Value ? "" : reader["OrderTime"].ToString(),
                                    AcceptedTime = reader["AcceptedTime"] == DBNull.Value ? "" : reader["AcceptedTime"].ToString(),
                                    BilledTime = reader["BilledTime"] == DBNull.Value ? "" : reader["BilledTime"].ToString()
                                });
                            }

                            // 2️⃣ Total count
                            if (reader.NextResult() && reader.Read())
                            {
                                totalRecords = Convert.ToInt32(reader[0]);
                            }

                            // 3️⃣ Summary
                            if (reader.NextResult() && reader.Read())
                            {
                                statusSummary = new
                                {
                                    All = reader["All"],
                                    Pending = reader["Pending"],
                                    Accepted = reader["Accepted"],
                                    Billed = reader["Billed"],
                                    //Dispatched = reader["Dispatched"],
                                    Delivered = reader["Delivered"]
                                };
                            }
                        }
                    }

                    var groupedData = orders
                        .GroupBy(o => new { o.SalesmanId, o.SalesmanName })
                        .Select(g => new
                        {
                            g.Key.SalesmanId,
                            g.Key.SalesmanName,
                            Orders = g.ToList()
                        });

                    await _hubContext.Clients.All.SendAsync("ReceiveEcommerceUpdate", groupedData);

                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Sales orders fetched successfully",
                        totalRecords,
                        summary = statusSummary,
                        data = groupedData
                    });
                }
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

        //[Authorize(Roles = UserRoles.EcommerceUser)]
        //[HttpGet("get-ecommerce-product-stock")]
        //public async Task<IActionResult> GetProductStock(int productId)
        //{
        //    if (productId <= 0)
        //        return BadRequest(new
        //        {
        //            status = false,
        //            statusCode = 400,
        //            message = "Invalid Product Id",
        //            data = new { Qty = 0 }
        //        });

        //    try
        //    {
        //        string sql = "SELECT ISNULL(qty, 0) FROM View_Stock WHERE ir_id = @productId and qty>0 and location_id=1";

        //        using (var conn = new SqlConnection(_connectionString))
        //        using (var cmd = new SqlCommand(sql, conn))
        //        {
        //            cmd.Parameters.AddWithValue("@productId", productId);
        //            conn.Open();

        //            object result = await cmd.ExecuteScalarAsync();
        //            decimal stockQty = result != null ? Convert.ToDecimal(result) : 0;

        //            return Ok(new
        //            {
        //                status = true,
        //                statusCode = 200,
        //                message = "Stock quantity fetched successfully",
        //                data = new { Qty = stockQty }
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = "Error: " + ex.Message,
        //            data = new { Qty = 0 }
        //        });
        //    }
        //}
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-ecommerce-product-stock")]
        public async Task<IActionResult> GetProductStock(int productId)
        {
            if (productId <= 0)
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "Invalid Product Id",
                    data = new { Qty = 0, available_qty = 0 }
                });

            try
            {
                string sql = @"
        SELECT 
            ISNULL(vs.qty, 0) AS stock_qty,
            ISNULL(p.pending_qty, 0) AS pending_qty,
            (ISNULL(vs.qty, 0) - ISNULL(p.pending_qty, 0)) AS available_qty
        FROM View_Stock vs
        LEFT JOIN (
            SELECT sp_ir_id, SUM(sp_qty) AS pending_qty
            FROM inv_sales_par
            WHERE sp_str_id=3 and sp_narration1 in ('pending','accepted')
            GROUP BY sp_ir_id
        ) p ON p.sp_ir_id = vs.ir_id
        WHERE vs.ir_id = @productId
          AND vs.location_id = 1";

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@productId", productId);
                    conn.Open();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!reader.Read())
                        {
                            return Ok(new
                            {
                                status = true,
                                statusCode = 200,
                                message = "Stock quantity fetched successfully",
                                data = new { Qty = 0, available_qty = 0 }
                            });
                        }

                        decimal stockQty = Convert.ToDecimal(reader["stock_qty"]);
                        decimal available_qty = Convert.ToDecimal(reader["available_qty"]);

                        return Ok(new
                        {
                            status = true,
                            statusCode = 200,
                            message = "Stock quantity fetched successfully",
                            data = new
                            {
                                Qty = stockQty,
                                available_qty = available_qty
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = new { Qty = 0, AvailableQty = 0 }
                });
            }
        }

        //[HttpGet("update-ecommerce-order-status")]
        //public async Task<IActionResult> UpdateOrderStatus(int orderNo, string status)
        //{
        //    if (orderNo <= 0 || string.IsNullOrEmpty(status))
        //    {
        //        return BadRequest(new
        //        {
        //            status = false,
        //            statusCode = 400,
        //            message = "Invalid order number or status",
        //            data = new { Qty = 0 }
        //        });
        //    }

        //    try
        //    {
        //        string connectionString = _connectionString; // make sure you have this defined
        //        string sql = @"
        //    DECLARE @si_acc_id INT;

        //    UPDATE INV_SALES_INF
        //    SET si_other_remarks = @Status
        //    WHERE si_entryno = @orderNo AND si_str_id = 3;

        //    UPDATE INV_SALES_PAR
        //    SET sp_narration1 = @Status
        //    WHERE sp_entryno = @orderNo AND sp_str_id = 3;

        //     UPDATE INV_SALES_INF
        //    SET si_other_remarks = @Status
        //    WHERE si_qtn_no = @orderNo AND si_str_id = 2;

        //    UPDATE p
        //    SET p.sp_narration1 = @Status
        //    FROM INV_SALES_PAR p
        //    INNER JOIN INV_SALES_INF i ON i.si_entryno = p.sp_entryno and i.si_str_id= p.sp_str_id
        //    WHERE i.si_qtn_no = @orderNo 
        //    AND p.sp_str_id = 2;

        //    SELECT @si_acc_id = si_acc_id
        //    FROM INV_SALES_INF
        //    WHERE si_str_id = 3 AND si_entryno = @orderNo;

        //    SELECT @si_acc_id = si_acc_id
        //    FROM INV_SALES_INF
        //    WHERE si_str_id = 3 AND si_entryno = @orderNo;

        //    IF NOT EXISTS (
        //        SELECT 1 FROM inv_sales_order_status
        //        WHERE sos_orderId = @orderNo
        //          AND sos_customerId = @si_acc_id
        //          AND sos_status = @Status
        //    )
        //    BEGIN
        //        INSERT INTO inv_sales_order_status (sos_orderId, sos_customerId, sos_status)
        //        VALUES (@orderNo, @si_acc_id, @Status);
        //    END
        //";

        //        var parameters = new[]
        //        {
        //    new SqlParameter("@orderNo", SqlDbType.Int) { Value = orderNo },
        //    new SqlParameter("@Status", SqlDbType.NVarChar, 100) { Value = status }
        //};

        //        int rowsAffected = ExecuteNonQuery(connectionString, sql, parameters);

        //        return Ok(new
        //        {
        //            status = true,
        //            statusCode = 200,
        //            message = "Order status updated successfully",
        //            data = new { orderNo = orderNo, status = status }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            status = false,
        //            statusCode = 500,
        //            message = "Error: " + ex.Message,
        //            data = new { Qty = 0 }
        //        });
        //    }
        //}
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("update-ecommerce-order-status")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            if (orderId <= 0 || string.IsNullOrEmpty(status))
            {
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "Invalid order number or status",
                    data = new { Qty = 0 }
                });
            }

            try
            {
                string connectionString = _connectionString; // make sure you have this defined
                string sql = @"
            DECLARE @si_acc_id INT;
            DECLARE @si_entryno NUMERIC;

            -- First update INV_SALES_INF
            UPDATE i
            SET i.si_other_remarks = @Status
            FROM INV_SALES_INF i
            WHERE i.si_id = @orderId 
              AND i.si_str_id = 3;

            -- Then update INV_SALES_PAR
            UPDATE p
            SET p.sp_narration1 = @Status
            FROM INV_SALES_PAR p
            INNER JOIN INV_SALES_INF i 
                ON i.si_entryno = p.sp_entryno 
               AND i.si_str_id = p.sp_str_id
            WHERE i.si_id = @orderId 
              AND p.sp_str_id = 3;

            SELECT @si_acc_id = si_acc_id, @si_entryno= si_entryno 
            FROM INV_SALES_INF 
            WHERE si_str_id = 3 AND si_id = @orderId;

            IF NOT EXISTS (
                SELECT 1 FROM inv_sales_order_status
                WHERE sos_orderId = @orderId
                  AND sos_customerId = @si_acc_id
                  AND sos_status = @Status
            )
            BEGIN
                INSERT INTO inv_sales_order_status (sos_orderId, sos_customerId, sos_status)
                VALUES (@si_entryno, @si_acc_id, @Status);
            END
        ";

                var parameters = new[]
                {
            new SqlParameter("@orderId", SqlDbType.Int) { Value = orderId },
            new SqlParameter("@Status", SqlDbType.NVarChar, 100) { Value = status }
        };

                int rowsAffected = ExecuteNonQuery(connectionString, sql, parameters);

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Order status updated successfully",
                    data = new { orderId = orderId, status = status }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = new { Qty = 0 }
                });
            }
        }
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("cancel-check-out")]
        public async Task<IActionResult> CancelCheckout(int orderId)
        {
            if (orderId <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "Invalid order number",
                    data = new { Qty = 0 }
                });
            }

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            // 1️⃣ Get entry number & customer id
                            string getEntrySql = @"
                        SELECT si_entryno, si_acc_id
                        FROM INV_SALES_INF
                        WHERE si_id = @orderId
                          AND si_str_id = 3
                          AND si_other_remarks = 'Pending'";

                            SqlCommand getCmd = new SqlCommand(getEntrySql, con, tran);
                            getCmd.Parameters.AddWithValue("@orderId", orderId);

                            decimal siEntryNo;
                            decimal customerId;

                            using (SqlDataReader reader = await getCmd.ExecuteReaderAsync())
                            {
                                if (!reader.Read())
                                {
                                    return BadRequest(new
                                    {
                                        status = false,
                                        statusCode = 400,
                                        message = "Order not found or already processed",
                                        data = new { Qty = 0 }
                                    });
                                }

                                siEntryNo = reader.GetDecimal(0);
                                customerId = reader.GetDecimal(1);
                            }

                            // 2️⃣ Delete from INV_SALES_PAR
                            string deleteParSql = @"
                        DELETE FROM INV_SALES_PAR
                        WHERE sp_entryno = @entryNo
                          AND sp_str_id = 3";

                            SqlCommand deleteParCmd = new SqlCommand(deleteParSql, con, tran);
                            deleteParCmd.Parameters.AddWithValue("@entryNo", siEntryNo);
                            await deleteParCmd.ExecuteNonQueryAsync();

                            // 3️⃣ Delete from INV_SALES_INF
                            string deleteInfSql = @"
                        DELETE FROM INV_SALES_INF
                        WHERE si_id = @orderId
                          AND si_str_id = 3
                          AND si_other_remarks = 'Pending'";

                            SqlCommand deleteInfCmd = new SqlCommand(deleteInfSql, con, tran);
                            deleteInfCmd.Parameters.AddWithValue("@orderId", orderId);
                            await deleteInfCmd.ExecuteNonQueryAsync();

                            // 4️⃣ Insert Cancel status (optional but recommended)
                            string deleteStatusSql = @"
                            DELETE FROM inv_sales_order_status
                            WHERE sos_orderId = @orderId";

                            SqlCommand statusCmd = new SqlCommand(deleteStatusSql, con, tran);
                            statusCmd.Parameters.AddWithValue("@orderId", siEntryNo); 
                            await statusCmd.ExecuteNonQueryAsync();

                            // ✅ Commit transaction
                            tran.Commit();

                            return Ok(new
                            {
                                status = true,
                                statusCode = 200,
                                message = "Order cancelled successfully",
                                data = new { orderId = orderId }
                            });
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
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
                    message = "Error : " + ex.Message,
                    data = new { Qty = 0 }
                });
            }
        }

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("ecommerce-user-active")]
        public async Task<IActionResult> UserActive()
        {
            var ledgerIdStr = User.FindFirst("LedgerId")?.Value;
            var userRole = User.FindFirst("UserRole")?.Value;

            if (!string.IsNullOrWhiteSpace(userRole) &&
        userRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Ledger is active",
                    data = new
                    {
                        LedgerId = ledgerIdStr,
                        Role = userRole
                    }
                });
            }

            if (!int.TryParse(ledgerIdStr, out int ledgerId))
                return BadRequest("Invalid LedgerId in claims.");

            try
            {
                string connectionString = _connectionString;

                string sql = @"SELECT as_active 
                       FROM acc_subhead 
                       WHERE as_id = @as_id 
                         AND as_ap_id IN (4, 14, 25)";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@as_id", ledgerId);
                        await conn.OpenAsync();

                        var result = await cmd.ExecuteScalarAsync(); // returns the as_active value

                        bool isActive = false;

                        if (result != null && result != DBNull.Value)
                            isActive = Convert.ToInt32(result) == 1;

                        return Ok(new
                        {
                            status = isActive,
                            statusCode = 200,
                            message = isActive ? "Ledger is active" : "Ledger is inactive",
                            data = new { LedgerId = ledgerId }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = new { LedgerId = ledgerIdStr, Role = userRole }
                });
            }
        }
        // images
        [HttpPost("ecommerce-images")]
        public async Task<IActionResult> ManageImages1([FromForm] ImageUploadRequest request, [FromQuery] string action, IFormFile imageFile)
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

            try
            {
                string connectionString = _connectionString;
                string baseUrl = (string)ExecuteScalar(connectionString,
                    "SELECT TOP 1 ans_status FROM android_settings WHERE ans_name='BaseUrl'");

                string relativePath = null;

                // ✅ Handle Insert / Update with file
                if ((action.Equals("Insert", StringComparison.OrdinalIgnoreCase) ||
                     action.Equals("Update", StringComparison.OrdinalIgnoreCase))
                     && imageFile != null && imageFile.Length > 0)
                {
                    string folderName = request.ReferenceType ?? "general";
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folderName);
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    relativePath = $"/uploads/{folderName}/{fileName}";
                }

                // ✅ Call Stored Procedure
                using (SqlConnection con = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("Sp_inv_images", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@StatementType", action);
                    cmd.Parameters.AddWithValue("@ii_id", request.ImageId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ii_reference_type", request.ReferenceType ?? "");
                    cmd.Parameters.AddWithValue("@ii_reference_id", request.ReferenceId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ii_image", (object)relativePath ?? DBNull.Value);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("create-ecommerce-po-cart")]
        public IActionResult TogglePoCart([FromForm] PoCartToggleModel model, IFormFile attachment)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var ledgerIdStr = User.FindFirst("LedgerId")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Unauthorized: Invalid token.",
                        data = (object)null
                    });
                }
                string baseUrl = ExecuteScalar(_connectionString, "SELECT ans_status FROM android_settings WHERE ans_name = 'BaseUrl'")?.ToString();
                string attachmentPath = null;
                if (attachment != null && attachment.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cart");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);
                    var fileName = Guid.NewGuid() + Path.GetExtension(attachment.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        attachment.CopyTo(stream);
                    }
                    attachmentPath = "/uploads/cart/" + fileName;
                }

                int exists = 0;

                string checkSql = "SELECT COUNT(*) FROM inv_ecommerce_po_cart WHERE c_salesman_id = @salesman_id AND c_customer_id = @customer_id AND ((@itemId <> 0 AND c_item_id = @itemId)OR(@itemId = 0 AND c_item_name = @itemName))";
                exists = Convert.ToInt32(ExecuteScalar(_connectionString, checkSql,
                    new[] {
                    new SqlParameter("@customer_id", model.customerId ?? 0),
                    new SqlParameter("@salesman_id", ledgerIdStr),
                    new SqlParameter("@itemId", model.itemId??0),
                    new SqlParameter("@itemName", model.itemName ?? "")
                    }));
                object product = null;
                if (exists == 0)
                {
                    string userName = Convert.ToString(
                    ExecuteScalar(_connectionString,
                    @"SELECT TOP 1 a.as_name
                      FROM gnl_ecommerce_login g
                      INNER JOIN acc_subhead a ON a.as_id = g.gel_ledger_id
                      WHERE g.gel_id = @userId",
                    new[]
                    {
                        new SqlParameter("@userId", Convert.ToInt32(userId))
                    }));

                    // 🔹 Format remarks (LIKE NOTE FORMAT)
                    string finalRemarks = "";
                    if (!string.IsNullOrWhiteSpace(model.remarks))
                    {
                        finalRemarks = $"{model.remarks} ({userName} - {DateTime.Now:dd-MM-yyyy HH:mm})";
                    }
                    // ✅ Insert into cart
                    string insertSql = @"
                INSERT INTO inv_ecommerce_po_cart 
                (c_customer_id, c_salesman_id, c_item_id,c_item_name, c_item_qty, c_remarks,c_tag, c_attachment, c_user_id)
                VALUES (@customer_id, @salesman_id, @itemId,@itemName, @qty, @remarks,@tag, @attachment, @c_user_id)";

                    ExecuteNonQuery(_connectionString, insertSql, new[] {
                new SqlParameter("@customer_id", model.customerId ?? 0 ),
                new SqlParameter("@salesman_id", ledgerIdStr),
                new SqlParameter("@itemId", model.itemId ?? 0),
                new SqlParameter("@itemName", (object?)model.itemName ?? DBNull.Value),
                new SqlParameter("@qty", model.qty),
                new SqlParameter("@remarks",finalRemarks),
                new SqlParameter("@tag", model.tag ?? string.Empty),
                new SqlParameter("@attachment", (object)attachmentPath ?? ""),
                new SqlParameter("@c_user_id", userId)
            });
                    string productSql = @"
                    SELECT TOP 1 c.c_id as CartId, c.c_date as Date, 
                   ISNULL(ir.ir_id, 0) AS ItemId,CASE 
                    WHEN ISNULL(ir.ir_id, 0) = 0 
                        THEN c.c_item_name
                    ELSE ir.ir_name
                END AS ItemName,
                   c.c_item_qty as Qty,
                   c.c_remarks as Remarks,c.c_tag as Tag, c.c_attachment as Attachment,
                   c.c_customer_id as CustomerId, a.as_name as CustomerName,
                   c.c_salesman_id as SalesmanId, b.as_name as SalesmanName
            FROM inv_ecommerce_po_cart c
            LEFT JOIN inv_item_reg ir ON c.c_item_id = ir.ir_id
            LEFT JOIN acc_subhead a ON a.as_id = c.c_customer_id
            LEFT JOIN acc_subhead b ON b.as_id = c.c_salesman_id
            WHERE (c.c_salesman_id = @sales OR @sales = 0)
                AND ((@itemId <> 0 AND c.c_item_id = @itemId)OR(@itemId = 0 AND c.c_item_name = @itemName))
            ORDER BY c.c_id DESC";

                    DataTable productTable = ExecuteDataTable(_connectionString, productSql, new[] {
                new SqlParameter("@sales", ledgerIdStr),
                new SqlParameter("@itemId", model.itemId??0),
                new SqlParameter("@itemName", model.itemName ?? "")
                    });

                    if (productTable.Rows.Count > 0)
                    {
                        product = new
                        {
                            CartId = Convert.ToInt32(productTable.Rows[0]["CartId"]),
                            Date = Convert.ToDateTime(productTable.Rows[0]["Date"]),
                            ItemId = Convert.ToInt32(productTable.Rows[0]["ItemId"]),
                            ItemName = productTable.Rows[0]["ItemName"].ToString(),
                            Qty = Convert.ToDouble(productTable.Rows[0]["Qty"]),
                            Remarks = productTable.Rows[0]["Remarks"]?.ToString(),
                            Tag = productTable.Rows[0]["Tag"]?.ToString(),

                            // ✅ Full path (BaseUrl + relativePath)
                            Attachment = string.IsNullOrEmpty(productTable.Rows[0]["Attachment"].ToString())
                                         ? ""
                                         : baseUrl.TrimEnd('/') + productTable.Rows[0]["Attachment"].ToString(),
                            CustomerId = Convert.ToInt32(productTable.Rows[0]["CustomerId"]),
                            CustomerName = productTable.Rows[0]["CustomerName"]?.ToString(),
                            SalesmanId = Convert.ToInt32(productTable.Rows[0]["SalesmanId"]),
                            SalesmanName = productTable.Rows[0]["SalesmanName"]?.ToString()
                        };
                    }

                }

                // ✅ Fetch newly inserted/updated product

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = exists == 0 ? "Item added to cart." : "Item already exists in cart.",
                    data = product
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("remove-ecommerce-po-cart/{cartId}")]
        public IActionResult RemovePoCart(int cartId)
        {
            try
            {
                // 1. Retrieve product before deleting (optional)
                string selectSql = @"SELECT c_id as CartId  FROM inv_ecommerce_po_cart WHERE c_id = @cartId";

                DataTable productTable = ExecuteDataTable(_connectionString, selectSql,
                    new[] { new SqlParameter("@cartId", cartId) });

                if (productTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "Cart item not found.",
                        data = (object)null
                    });
                }

                // 2. Delete the cart record
                string deleteSql = "DELETE FROM inv_ecommerce_po_cart WHERE c_id = @cartId";
                ExecuteNonQuery(_connectionString, deleteSql,
                    new[] { new SqlParameter("@cartId", cartId) });

                // 3. Prepare deleted product info
                var deletedCartId = new
                {
                    CartId = Convert.ToInt32(productTable.Rows[0]["CartId"]),
                };

                // 4. Return success
                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Item removed from cart successfully.",
                    data = deletedCartId
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-ecommerce-po-cart")]
        public IActionResult GetPoCart(int pageNumber = 1, int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ledgerIdStr = User.FindFirst("LedgerId")?.Value;

            if (!int.TryParse(ledgerIdStr, out int ledgerId))
                return BadRequest("Invalid LedgerId in claims.");

            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;
                // ✅ Get BaseUrl from settings
                string baseUrl = ExecuteScalar(
                    _connectionString,
                    "SELECT ans_status FROM android_settings WHERE ans_name = 'BaseUrl'"
                )?.ToString() ?? "";


                string countSql;
                List<SqlParameter> parameters = new();

                countSql = @"SELECT COUNT(*) FROM inv_ecommerce_po_cart c WHERE c.c_user_id = @c_user_id";
                parameters.Add(new SqlParameter("@c_user_id", userId));


                int totalRecords = Convert.ToInt32(
                    ExecuteScalar(_connectionString, countSql, parameters.ToArray())
                );

                if (totalRecords == 0)
                {
                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Cart is empty",
                        data = new
                        {
                            pagination = new
                            {
                                page = pageNumber,
                                pageSize = pageSize,
                                totalRecords = 0,
                                totalPages = 0
                            },
                            products = new List<object>()
                        }
                    });
                }

                int offset = (pageNumber - 1) * pageSize;
                string productSql = "";
                List<SqlParameter> productParams = new();

                productSql = $@"
                    SELECT 
                    c.c_id AS CartId,
                    c.c_date AS [Date],
                    c.c_item_id AS ItemId,
                    CASE 
                        WHEN ISNULL(ir.ir_id, 0) = 0 
                            THEN c.c_item_name
                        ELSE ir.ir_name
                    END AS ItemName,
                    c.c_item_qty AS Qty,
                    ISNULL(vs.mrp,0) AS Price,
                    c.c_remarks AS Remarks,
                    c.c_tag AS Tag,
                    c.c_attachment AS Attachment,
                    c.c_customer_id AS CustomerId,
                    a.as_name AS CustomerName,
                    c.c_salesman_id AS SalesmanId,
                    b.as_name AS SalesmanName
                FROM inv_ecommerce_po_cart c
                LEFT JOIN inv_item_reg ir ON c.c_item_id = ir.ir_id
                LEFT JOIN acc_subhead a ON a.as_id = c.c_customer_id
                LEFT JOIN acc_subhead b ON b.as_id = c.c_salesman_id
                LEFT JOIN (SELECT ir_id, MAX(mrp) AS mrp FROM view_stock GROUP BY ir_id) vs ON vs.ir_id = c.c_item_id
                    WHERE c.c_user_id = @c_user_id                      
                    ORDER BY c.c_date DESC
                    OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                productParams.Add(new SqlParameter("@c_user_id", userId));

                DataTable productTable = ExecuteDataTable(
                _connectionString,
                productSql,
                productParams.ToArray()
                );

                var productIds = productTable.AsEnumerable()
                    .Select(r => Convert.ToInt32(r["ItemId"]))
                    .Distinct()
                    .ToList();

                // 3. Get images
                DataTable imageTable = new DataTable();
                if (productIds.Any())
                {
                    string joinedIds = string.Join(",", productIds);
                    string imageSql = $@"
                    SELECT ii_reference_id AS ProductId,
                           ans.ans_status + img.ii_image AS Image
                    FROM inv_images img
                    CROSS JOIN android_settings ans
                    WHERE ans.ans_name = 'BaseUrl'
                      AND img.ii_reference_type = 'item'
                      AND img.ii_reference_id IN ({joinedIds})";


                    imageTable = ExecuteDataTable(_connectionString, imageSql);
                }

                // 4. Merge products with images
                var products = productTable.AsEnumerable()
                    .Select(row => new
                    {
                        CartId = Convert.ToInt32(row["CartId"]),
                        Date = Convert.ToDateTime(row["Date"]),
                        ItemId = Convert.ToInt32(row["ItemId"]),
                        ItemName = row["ItemName"]?.ToString(),
                        Qty = row["Qty"] != DBNull.Value ? Convert.ToDouble(row["Qty"]) : 1,
                        Price = row["Price"] != DBNull.Value ? Convert.ToDecimal(row["Price"]) : 0,
                        Remarks = row["Remarks"]?.ToString(),
                        Tag = row["Tag"]?.ToString(),
                        // ✅ Append BaseUrl if not empty
                        Attachment = string.IsNullOrEmpty(row["Attachment"]?.ToString())
                             ? ""
                             : baseUrl.TrimEnd('/') + row["Attachment"].ToString(),
                        CustomerId = Convert.ToInt32(row["CustomerId"]),
                        CustomerName = row["CustomerName"]?.ToString(),
                        SalesmanId = row["SalesmanId"] != DBNull.Value ? Convert.ToInt32(row["SalesmanId"]) : (int?)null,
                        SalesmanName = row["SalesmanName"]?.ToString(),
                        Images = imageTable.AsEnumerable()
                            .Where(img => Convert.ToInt32(img["ProductId"]) == Convert.ToInt32(row["ItemId"]))
                            .Select(img => img["Image"].ToString())
                            .Where(img => !string.IsNullOrWhiteSpace(img))
                            .Distinct()
                            .ToList()
                    }).ToList();

                // 5. Return result
                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Cart fetched successfully",
                    data = new
                    {
                        pagination = new
                        {
                            page = pageNumber,
                            pageSize = pageSize,
                            totalRecords = totalRecords,
                            totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                        },
                        products = products
                    }
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("update-ecommerce-po-cart-qty")]
        public IActionResult UpdatePoCartQty(int CartId, int Qty)
        {
            try
            {
                // 1. Validate input
                if (CartId <= 0 || Qty <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "Invalid cartId or qty.",
                        data = (object)null
                    });
                }

                // 2. Check if cart exists
                string selectSql = @"SELECT c_id as CartId, c_item_qty as Qty 
                             FROM inv_ecommerce_po_cart 
                             WHERE c_id = @cartId";

                DataTable cartTable = ExecuteDataTable(_connectionString, selectSql,
                    new[] { new SqlParameter("@cartId", CartId) });

                if (cartTable.Rows.Count == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "Cart item not found.",
                        data = (object)null
                    });
                }

                // 3. Update qty
                string updateSql = @"UPDATE inv_ecommerce_po_cart 
                             SET c_item_qty = @qty 
                             WHERE c_id = @cartId";

                ExecuteNonQuery(_connectionString, updateSql,
                    new[]
                    {
                new SqlParameter("@qty", Qty),
                new SqlParameter("@cartId", CartId)
                    });

                // 4. Return success
                var updatedCart = new
                {
                    CartId = CartId,
                    Qty = Qty
                };

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Cart quantity updated successfully.",
                    data = updatedCart
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("create-ecommerce-purchase-order")]
        public IActionResult CreatePurchaseOrderFromCart([FromBody] CreatePoFromCartRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ledgerIdStr = User.FindFirst("LedgerId")?.Value;

            if (!int.TryParse(userIdStr, out int userId))
                return BadRequest("Invalid user");

            if (!decimal.TryParse(ledgerIdStr, out decimal ledgerId))
                return BadRequest("Invalid LedgerId");

            if (request?.CartIds == null || request.CartIds.Count == 0)
            {
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "CartIds required",
                    data = (object)null
                });
            }

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();
            SqlTransaction tran = conn.BeginTransaction();

            try
            {
                /* 1️⃣ INSERT PO INF */
                string insertInfSql = @"
                    INSERT INTO inv_ecommerce_purchase_order_inf
                    (
                        epoi_date,
                        epoi_status,
                        epoi_salesman_id,
                        epoi_user_id,
                        epoi_created_by,
                        epoi_created_date
                    )
                    VALUES
                    (
                        GETDATE(),
                        'Requested',
                        @salesman_id,
                        @user_id,
                        @user_id,
                        GETDATE()
                    );
                    SELECT SCOPE_IDENTITY();
                ";

                SqlCommand infCmd = new SqlCommand(insertInfSql, conn, tran);
                infCmd.Parameters.Add("@salesman_id", SqlDbType.Decimal).Value = ledgerId;
                infCmd.Parameters.Add("@user_id", SqlDbType.Decimal).Value = userId;

                long epoiId = Convert.ToInt64(infCmd.ExecuteScalar());

                /* 2️⃣ INSERT PO PAR FROM CART */
                string joinedCartIds = string.Join(",", request.CartIds);

                string insertParSql = $@"
                        INSERT INTO inv_ecommerce_purchase_order_par
                        (
                            epop_epoi_id,
                            epop_cust_id,
                            epop_ir_id,
                            epop_item_name,
                            epop_requested_qty,
                            epop_requested_by,
                            epop_requested_date,
                            epop_status,
                            epop_note,
                            epop_attachment,
                            epop_tag
                        )
                        SELECT
                            @epoi_id,
                            c.c_customer_id,
                            c.c_item_id,
                            isnull(c.c_item_name,''),
                            c.c_item_qty,
                            c.c_user_id,
                            getdate(),
                            'Requested',
                            c.c_remarks,
                            c.c_attachment,
                            c.c_tag
                        FROM inv_ecommerce_po_cart c
                        WHERE c.c_id IN ({joinedCartIds})
                  AND c.c_user_id = @user_id;
                    ";

                SqlCommand parCmd = new SqlCommand(insertParSql, conn, tran);
                parCmd.Parameters.Add("@epoi_id", SqlDbType.Decimal).Value = epoiId;
                parCmd.Parameters.Add("@user_id", SqlDbType.Decimal).Value = userId;

                int insertedItems = parCmd.ExecuteNonQuery();

                if (insertedItems == 0)
                {
                    tran.Rollback();
                    return BadRequest(new
                    {
                        status = false,
                        statusCode = 400,
                        message = "No cart items found for purchase order",
                        data = (object)null
                    });
                }

                /* 3️⃣ DELETE CART ITEMS */
                string deleteCartSql = $@"
                        DELETE FROM inv_ecommerce_po_cart
                        WHERE c_id IN ({joinedCartIds})
                    ";

                SqlCommand delCmd = new SqlCommand(deleteCartSql, conn, tran);
                delCmd.Parameters.Add("@user_id", SqlDbType.Decimal).Value = userId;
                delCmd.ExecuteNonQuery();

                tran.Commit();

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Purchase order created from cart",
                    data = new
                    {
                        epoi_id = epoiId,
                        itemCount = insertedItems
                    }
                });
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message,
                    data = (object)null
                });
            }
        }
        //    [HttpPost("update-ecommerce-purchase-order")]
        //    public IActionResult UpdatePurchaseOrderPar([FromBody] UpdatePoParRequest request)
        //    {
        //        using SqlConnection conn = new SqlConnection(_connectionString);
        //        conn.Open();

        //        string sql = @"
        //    UPDATE inv_ecommerce_purchase_order_par
        //    SET 
        //        epop_order_qty = @order_qty,
        //        epop_order_by = @order_by,
        //        epop_order_price = @order_price,
        //        epop_received_qty = @received_qty,
        //        epop_received_by = @received_by,
        //        epop_damaged_qty = @damaged_qty,
        //        epop_purchase_price = @purchase_price,
        //        epop_supplier_id = @supplier_id,
        //        epop_tag = @tag,
        //        epop_status = @status,
        //       -- epop_note = @note,
        //       -- epop_attachment = @attachment
        //    WHERE epop_id = @epop_id
        //";

        //        SqlCommand cmd = new SqlCommand(sql, conn);

        //        cmd.Parameters.AddWithValue("@epop_id", request.epop_id);
        //        cmd.Parameters.AddWithValue("@order_qty", (object?)request.order_qty ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@order_by", (object?)request.order_by ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@order_price", (object?)request.order_price ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@received_qty", (object?)request.received_qty ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@received_by", (object?)request.received_by ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@damaged_qty", (object?)request.damaged_qty ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@purchase_price", (object?)request.purchase_price ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@supplier_id", (object?)request.supplier_id ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@tag", (object?)request.tag ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@status", (object?)request.status ?? DBNull.Value);
        //        cmd.Parameters.AddWithValue("@note", (object?)request.note ?? DBNull.Value);
        //        //cmd.Parameters.AddWithValue("@attachment", (object?)request.attachment ?? DBNull.Value);

        //        int rows = cmd.ExecuteNonQuery();

        //        return Ok(new
        //        {
        //            status = true,
        //            statusCode = 200,
        //            message = "Updated successfully",
        //            data = rows
        //        });
        //    }
        [HttpPost("update-ecommerce-purchase-order")]
        public IActionResult UpdatePurchaseOrderPar([FromBody] UpdatePoParRequest request)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string sql = "";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;

            // 🔹 Required
            cmd.Parameters.Add("@epop_id", SqlDbType.Decimal).Value = request.epop_id;

            // 🔥 STEP 1: NOTE PREPARATION
            string noteSqlPart = "";
            string finalNote = null;

            if (!string.IsNullOrWhiteSpace(request.note))
            {
                // 👉 Get existing note
                string existingNote = "";
                using (SqlCommand getCmd = new SqlCommand(
                    "SELECT ISNULL(epop_note,'') FROM inv_ecommerce_purchase_order_par WHERE epop_id=@id", conn))
                {
                    getCmd.Parameters.Add("@id", SqlDbType.Decimal).Value = request.epop_id;
                    existingNote = Convert.ToString(getCmd.ExecuteScalar());
                }

                // 👉 Get username
                string userName = "";
                using (SqlCommand nameCmd = new SqlCommand(@"
            SELECT TOP 1 a.as_name
            FROM gnl_ecommerce_login g
            INNER JOIN acc_subhead a ON a.as_id = g.gel_ledger_id
            WHERE g.gel_id = @userId", conn))
                {
                    nameCmd.Parameters.Add("@userId", SqlDbType.Int).Value =
                        (object?)request.userId ?? DBNull.Value;

                    userName = Convert.ToString(nameCmd.ExecuteScalar());
                }

                // 👉 Build note
                string newEntry = $"{request.note} ({userName} - {DateTime.Now:dd-MM-yyyy HH:mm})";

                if (!string.IsNullOrEmpty(existingNote))
                    finalNote = existingNote + " / " + newEntry;
                else
                    finalNote = newEntry;

                // 👉 Add parameter + SQL part
                cmd.Parameters.Add("@note", SqlDbType.NVarChar).Value = finalNote;
                noteSqlPart = ", epop_note = @note";
            }

            // 🔥 STEP 2: TYPE BASED UPDATE

            if (string.Equals(request.type, "UpdateEnquiry", StringComparison.OrdinalIgnoreCase))
            {
                sql = $@"
        UPDATE inv_ecommerce_purchase_order_par
        SET 
            epop_status = 'Enquiry'
            {noteSqlPart},
            epop_supplier_id = @supplier_id,
            epop_order_price = @order_price,
            epop_enquiry_by = @enquiry_by,
            epop_enquiry_date = getdate()
        WHERE epop_id = @epop_id
        ";

                cmd.Parameters.Add("@supplier_id", SqlDbType.Int).Value =
                    (object?)request.supplier_id ?? DBNull.Value;
                cmd.Parameters.Add("@enquiry_by", SqlDbType.Int).Value =
                    (object?)request.userId ?? DBNull.Value;
                cmd.Parameters.Add("@order_price", SqlDbType.Decimal).Value =
                    (object?)request.order_price ?? DBNull.Value;

            }
            else if (string.Equals(request.type, "UpdatePo", StringComparison.OrdinalIgnoreCase))
            {
                sql = $@"
        UPDATE inv_ecommerce_purchase_order_par
        SET 
            epop_status = 'Ordered',
            epop_order_qty = @order_qty,
            epop_order_by = @order_by,
            epop_order_date = getdate(),
            epop_order_price = @order_price
            {noteSqlPart},
            epop_supplier_id = @supplier_id
        WHERE epop_id = @epop_id
        ";

                cmd.Parameters.Add("@order_qty", SqlDbType.Float).Value =
                    (object?)request.order_qty ?? DBNull.Value;

                cmd.Parameters.Add("@order_by", SqlDbType.Int).Value =
                    (object?)request.userId ?? DBNull.Value;

               
                cmd.Parameters.Add("@order_price", SqlDbType.Decimal).Value =
                    (object?)request.order_price ?? DBNull.Value;

                cmd.Parameters.Add("@supplier_id", SqlDbType.Int).Value =
                    (object?)request.supplier_id ?? DBNull.Value;
            }
            else if (string.Equals(request.type, "UpdateRecive", StringComparison.OrdinalIgnoreCase))
            {
                sql = $@"
        UPDATE inv_ecommerce_purchase_order_par
        SET 
            epop_status = 'Received',
            epop_received_qty = @received_qty,
            epop_received_by = @received_by,
            epop_received_date = getdate(),
            epop_damaged_qty = @damaged_qty,
            epop_purchase_price = @purchase_price
            {noteSqlPart}
        WHERE epop_id = @epop_id
        ";

                cmd.Parameters.Add("@received_qty", SqlDbType.Float).Value =
                    (object?)request.received_qty ?? DBNull.Value;

                cmd.Parameters.Add("@received_by", SqlDbType.Int).Value =
                    (object?)request.userId ?? DBNull.Value;
                

                cmd.Parameters.Add("@damaged_qty", SqlDbType.Float).Value =
                    (object?)request.damaged_qty ?? DBNull.Value;
                cmd.Parameters.Add("@purchase_price", SqlDbType.Decimal).Value =
                   (object?)request.purchase_price ?? DBNull.Value;
            }
            else
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid type"
                });
            }

            // 🔹 Execute
            cmd.CommandText = sql;
            int rows = cmd.ExecuteNonQuery();

            return Ok(new
            {
                status = true,
                statusCode = 200,
                message = "Updated successfully",
                data = rows
            });
        }
        [HttpGet("get-ecommerce-purchase-order-par")]
        public IActionResult GetPurchaseOrderPar(string? status, string? search, DateTime? fromDate, DateTime? toDate, int? supplierId, int? categoryId, int? productId, int? userId, string? userStatus, int? salesmanId, int? customerId)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string sql = @"
    SELECT 
        p.epop_id,
        p.epop_epoi_id,
        p.epop_cust_id,
        p.epop_ir_id,
        p.epop_item_name,
        p.epop_requested_qty,
        p.epop_requested_by,
        p.epop_requested_date,
        
        CASE 
    WHEN UPPER(ISNULL(g_req.gel_role, '')) = 'ADMIN' THEN 'admin'
    ELSE req.as_name
END AS RequesteByName,

        p.epop_enquiry_by,
        p.epop_enquiry_date,
        p.epop_enquiry_qty,
        CASE 
    WHEN UPPER(ISNULL(g_enq.gel_role, '')) = 'ADMIN' THEN 'admin'
    ELSE enq.as_name
END AS EnquiryByName,


        p.epop_order_qty,
        p.epop_order_by,
        p.epop_order_date,
        CASE 
    WHEN UPPER(ISNULL(g_ord.gel_role, '')) = 'ADMIN' THEN 'admin'
    ELSE ord.as_name
END AS OrderByName,

        p.epop_order_price,
        p.epop_received_qty,
        p.epop_received_by,
        p.epop_received_date,
        CASE 
    WHEN UPPER(ISNULL(g_rec.gel_role, '')) = 'ADMIN' THEN 'admin'
    ELSE rec.as_name
END AS ReceivedByName,

        p.epop_damaged_qty,
        p.epop_purchase_price,
        p.epop_supplier_id,
        sup.as_name AS SupplierName,

        p.epop_tag,
        p.epop_status,
        p.epop_note,
        CASE 
            WHEN p.epop_attachment IS NOT NULL 
                 AND p.epop_attachment <> ''
            THEN a.ans_status + p.epop_attachment
            ELSE ''
        END AS epop_attachment,

      lp.pi_date AS LastPurchaseDate,
      lp.pi_entryno AS LastPurchaseEntryNo,
      lp.pi_sup_id AS LastSupplierId,
      lp.LastSupplierName,

      lp.pp_qty AS LastPurchaseQty,
      lp.pp_prate AS LastPurchaseRate,

      lp.pp_mrp AS LastPurchaseMrp,
      lp.pp_retail AS LastPurchaseRetail,
      lp.pp_wholesale AS LastPurchaseWholesale,
      lp.pp_spretail As LastPurchaseSPRetail,
      lp.pp_branch AS LastPurchaseBranch,
      pimg.ProductImage
    FROM inv_ecommerce_purchase_order_par p
    CROSS JOIN android_settings a
    -- 🔹 Requester
    LEFT JOIN gnl_ecommerce_login g_req 
        ON g_req.gel_id = p.epop_requested_by
    LEFT JOIN acc_subhead req 
        ON req.as_id = g_req.gel_ledger_id

  LEFT JOIN gnl_ecommerce_login g_enq 
        ON g_enq.gel_id = p.epop_enquiry_by
    LEFT JOIN acc_subhead enq 
        ON enq.as_id = g_enq.gel_ledger_id

    -- 🔹 Order By
    LEFT JOIN gnl_ecommerce_login g_ord 
        ON g_ord.gel_id = p.epop_order_by
    LEFT JOIN acc_subhead ord 
        ON ord.as_id = g_ord.gel_ledger_id

    -- 🔹 Received By
    LEFT JOIN gnl_ecommerce_login g_rec 
        ON g_rec.gel_id = p.epop_received_by
    LEFT JOIN acc_subhead rec 
        ON rec.as_id = g_rec.gel_ledger_id

    -- 🔹 Supplier
    LEFT JOIN acc_subhead sup 
        ON sup.as_id = p.epop_supplier_id

    LEFT JOIN inv_item_reg ir
    ON ir.ir_id = p.epop_ir_id

    LEFT JOIN inv_ecommerce_purchase_order_inf poi
    ON poi.epoi_id = p.epop_epoi_id

-- 🔹 Last Purchase Details
OUTER APPLY
(
    SELECT TOP 1
        pi.pi_date,
        pi.pi_entryno,
        pi.pi_sup_id,
        s.as_name AS LastSupplierName,

        pp.pp_qty,
        pp.pp_mrp,
        pp.pp_retail,
        pp.pp_wholesale,
        pp.pp_spretail,
        pp.pp_branch,
        pp.pp_prate

    FROM inv_parchase_par pp

    INNER JOIN inv_purchase_inf pi
        ON pi.pi_entryno = pp.pp_entryno

    LEFT JOIN acc_subhead s
        ON s.as_id = pi.pi_sup_id

    WHERE pp.pp_ir_id = p.epop_ir_id

    ORDER BY pi.pi_date DESC, pp.pp_id DESC
) lp

OUTER APPLY
(
    SELECT TOP 1
        a.ans_status + img.ii_image AS ProductImage
    FROM inv_images img
    WHERE img.ii_reference_type = 'item'
      AND img.ii_reference_id = ir.ir_id
      AND img.ii_image IS NOT NULL
      AND img.ii_image <> ''
    ORDER BY img.ii_id
) pimg

    WHERE 
        a.ans_name = 'BaseUrl'  and 
        (@status IS NULL OR p.epop_status = @status)

        AND (
            @search IS NULL
            OR p.epop_item_name LIKE '%' + @search + '%'
            OR p.epop_tag LIKE '%' + @search + '%'
            OR (
    CASE 
        WHEN UPPER(ISNULL(g_req.gel_role, '')) = 'ADMIN' THEN 'admin'
        ELSE req.as_name
    END
) LIKE '%' + @search + '%'
OR (
    CASE 
        WHEN UPPER(ISNULL(g_enq.gel_role, '')) = 'ADMIN' THEN 'admin'
        ELSE enq.as_name
    END
) LIKE '%' + @search + '%'
OR (
    CASE 
        WHEN UPPER(ISNULL(g_ord.gel_role, '')) = 'ADMIN' THEN 'admin'
        ELSE ord.as_name
    END
) LIKE '%' + @search + '%'
OR (
    CASE 
        WHEN UPPER(ISNULL(g_rec.gel_role, '')) = 'ADMIN' THEN 'admin'
        ELSE rec.as_name
    END
) LIKE '%' + @search + '%'
            OR sup.as_name LIKE '%' + @search + '%'
        )
        AND (@categoryId IS NULL OR ir.ir_category_id = @categoryId)

AND (@productId IS NULL OR p.epop_ir_id = @productId)

AND (@customerId IS NULL OR @customerId = 0 OR p.epop_cust_id = @customerId)

AND (@salesmanId IS NULL OR @salesmanId = 0 OR poi.epoi_salesman_id = @salesmanId)

AND (
    @supplierId IS NULL
    OR EXISTS (
        SELECT 1
        FROM inv_parchase_par pp2
        INNER JOIN inv_purchase_inf pi2
            ON pi2.pi_entryno = pp2.pp_entryno
        WHERE pp2.pp_ir_id = p.epop_ir_id
          AND pi2.pi_sup_id = @supplierId
    )
)

AND
(
    -- No userId → don't apply user filter
    @userId IS NULL

    -- userId is provided, but userStatus is empty
    -- → show all purchase orders related to this user
    OR
    (
        @userStatus IS NULL
        AND
        (
            p.epop_requested_by = @userId
            OR p.epop_enquiry_by = @userId
            OR p.epop_order_by = @userId
            OR p.epop_received_by = @userId
        )
    )

    -- REQUESTED
    OR
    (
        UPPER(@userStatus) = 'REQUESTED'
        AND p.epop_requested_by = @userId
    )

    -- ENQUIRY
    OR
    (
        UPPER(@userStatus) = 'ENQUIRY'
        AND
        (
            p.epop_requested_by = @userId
            OR p.epop_enquiry_by = @userId
        )
    )

    -- ORDERED
    OR
    (
        UPPER(@userStatus) = 'ORDERED'
        AND
        (
            p.epop_requested_by = @userId
            OR p.epop_order_by = @userId
        )
    )

    -- RECEIVED
    OR
    (
        UPPER(@userStatus) = 'RECEIVED'
        AND
        (
            p.epop_requested_by = @userId
            OR p.epop_received_by = @userId
        )
    )
)

  -- Request Date Filter
    AND (@fromDate IS NULL OR CAST(p.epop_requested_date AS DATE) >= CAST(@fromDate AS DATE))
    AND (@toDate IS NULL OR CAST(p.epop_requested_date AS DATE) <= CAST(@toDate AS DATE))


    ORDER BY p.epop_id DESC
    ";

            SqlCommand cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@status", (object?)status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@search", (object?)search ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fromDate", (object?)fromDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@toDate", (object?)toDate ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@categoryId", (object?)categoryId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@supplierId", (object?)supplierId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@productId", (object?)productId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@userId", (object?)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@userStatus", (object?)userStatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@salesmanId", (object?)salesmanId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@customerId", (object?)customerId ?? DBNull.Value);

            SqlDataReader reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    epop_id = reader["epop_id"],
                    epop_epoi_id = reader["epop_epoi_id"] == DBNull.Value ? null : reader["epop_epoi_id"],
                    epop_cust_id = reader["epop_cust_id"] == DBNull.Value ? null : reader["epop_cust_id"],
                    epop_ir_id = reader["epop_ir_id"] == DBNull.Value ? null : reader["epop_ir_id"],
                    epop_item_name = reader["epop_item_name"] == DBNull.Value ? null : reader["epop_item_name"],
                    epop_requested_qty = reader["epop_requested_qty"] == DBNull.Value ? null : reader["epop_requested_qty"],
                    epop_requested_by = reader["epop_requested_by"] == DBNull.Value ? null : reader["epop_requested_by"],
                    epop_requested_date = reader["epop_requested_date"] == DBNull.Value ? null : reader["epop_requested_date"],
                    RequesteByName = reader["RequesteByName"] == DBNull.Value ? null : reader["RequesteByName"],

                    epop_enquiry_by = reader["epop_enquiry_by"] == DBNull.Value? null : reader["epop_enquiry_by"],
                    epop_enquiry_date = reader["epop_enquiry_date"] == DBNull.Value ? null : reader["epop_enquiry_date"],
                    epop_enquiry_qty = reader["epop_enquiry_qty"] == DBNull.Value ? null : reader["epop_enquiry_qty"],
                    EnquiryByName = reader["EnquiryByName"] == DBNull.Value ? null : reader["EnquiryByName"],

                    epop_order_qty = reader["epop_order_qty"] == DBNull.Value ? null : reader["epop_order_qty"],
                    epop_order_by = reader["epop_order_by"] == DBNull.Value ? null : reader["epop_order_by"],
                    epop_order_date = reader["epop_order_date"] == DBNull.Value ? null : reader["epop_order_date"],
                    OrderByName = reader["OrderByName"] == DBNull.Value ? null : reader["OrderByName"],

                    epop_order_price = reader["epop_order_price"] == DBNull.Value ? null : reader["epop_order_price"],

                    epop_received_qty = reader["epop_received_qty"] == DBNull.Value ? null : reader["epop_received_qty"],
                    epop_received_by = reader["epop_received_by"] == DBNull.Value ? null : reader["epop_received_by"],
                    epop_received_date = reader["epop_received_date"] == DBNull.Value ? null : reader["epop_received_date"],
                    ReceivedByName = reader["ReceivedByName"] == DBNull.Value ? null : reader["ReceivedByName"],

                    epop_damaged_qty = reader["epop_damaged_qty"] == DBNull.Value ? null : reader["epop_damaged_qty"],

                    epop_purchase_price = reader["epop_purchase_price"] == DBNull.Value ? null : reader["epop_purchase_price"],
                    epop_supplier_id = reader["epop_supplier_id"] == DBNull.Value ? null : reader["epop_supplier_id"],
                    SupplierName = reader["SupplierName"] == DBNull.Value ? null : reader["SupplierName"],

                    epop_tag = reader["epop_tag"] == DBNull.Value ? null : reader["epop_tag"],
                    epop_status = reader["epop_status"] == DBNull.Value ? null : reader["epop_status"],
                    epop_note = reader["epop_note"] == DBNull.Value ? null : reader["epop_note"],
                    epop_attachment = reader["epop_attachment"] == DBNull.Value ? null : reader["epop_attachment"],
                    ProductImage = reader["ProductImage"] == DBNull.Value ? null : reader["ProductImage"],

                    LastPurchaseDate = reader["LastPurchaseDate"] == DBNull.Value ? null : reader["LastPurchaseDate"],
                    LastPurchaseEntryNo = reader["LastPurchaseEntryNo"] == DBNull.Value ? null : reader["LastPurchaseEntryNo"],
                    LastSupplierId = reader["LastSupplierId"] == DBNull.Value ? null : reader["LastSupplierId"],
                    LastSupplierName = reader["LastSupplierName"] == DBNull.Value ? null : reader["LastSupplierName"],

                    LastPurchaseQty = reader["LastPurchaseQty"] == DBNull.Value ? null : reader["LastPurchaseQty"],
                    LastPurchaseRate = reader["LastPurchaseRate"] == DBNull.Value ? null : reader["LastPurchaseRate"],

                    LastPurchaseMrp = reader["LastPurchaseMrp"] == DBNull.Value ? null : reader["LastPurchaseMrp"],
                    LastPurchaseRetail = reader["LastPurchaseRetail"] == DBNull.Value ? null : reader["LastPurchaseRetail"],
                    LastPurchaseWholesale = reader["LastPurchaseWholesale"] == DBNull.Value ? null : reader["LastPurchaseWholesale"],
                    LastPurchaseSPRetail = reader["LastPurchaseSPRetail"] == DBNull.Value ? null : reader["LastPurchaseSPRetail"],
                    LastPurchaseBranch = reader["LastPurchaseBranch"] == DBNull.Value ? null : reader["LastPurchaseBranch"]
                });
            }

            return Ok(new
            {
                status = true,
                message = "Filtered data fetched successfully",
                data = list
            });
        }
        [HttpPost("edit-ecommerce-purchase-order-par")]
        //[Consumes("multipart/form-data")]
        public IActionResult EditPurchaseOrderPar([FromForm] EditPoParRequest request, IFormFile? attachment)
        {
            if (request == null || ((request.epop_id ?? 0) <= 0 && (request.epop_epoi_id ?? 0) <= 0))
            {
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "epop_id or epop_epoi_id required"
                });
            }
            string attachmentPath = null;

            if (attachment != null && attachment.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cart");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + Path.GetExtension(attachment.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    attachment.CopyTo(stream);
                }

                attachmentPath = "/uploads/cart/" + fileName;
            }
            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string sql = @"
            UPDATE inv_ecommerce_purchase_order_par
            SET
                epop_epoi_id = COALESCE(@epop_epoi_id, epop_epoi_id),
                epop_cust_id = COALESCE(@epop_cust_id, epop_cust_id),
                epop_ir_id = COALESCE(@epop_ir_id, epop_ir_id),
                epop_item_name = COALESCE(@epop_item_name, epop_item_name),

                epop_requested_qty = COALESCE(@epop_requested_qty, epop_requested_qty),
                epop_requested_by = COALESCE(@epop_requested_by, epop_requested_by),
                epop_requested_date = COALESCE(@epop_requested_date, epop_requested_date),

                epop_enquiry_by = COALESCE(@epop_enquiry_by, epop_enquiry_by),
                epop_enquiry_date = COALESCE(@epop_enquiry_date, epop_enquiry_date),
                epop_enquiry_qty = COALESCE(@epop_enquiry_qty, epop_enquiry_qty),

                epop_order_qty = COALESCE(@epop_order_qty, epop_order_qty),
                epop_order_by = COALESCE(@epop_order_by, epop_order_by),
                epop_order_date = COALESCE(@epop_order_date, epop_order_date),
                epop_order_price = COALESCE(@epop_order_price, epop_order_price),

                epop_received_qty = COALESCE(@epop_received_qty, epop_received_qty),
                epop_received_by = COALESCE(@epop_received_by, epop_received_by),
                epop_received_date = COALESCE(@epop_received_date, epop_received_date),

                epop_damaged_qty = COALESCE(@epop_damaged_qty, epop_damaged_qty),
                epop_purchase_price = COALESCE(@epop_purchase_price, epop_purchase_price),
                epop_supplier_id = COALESCE(@epop_supplier_id, epop_supplier_id),

                epop_tag = @epop_tag,
                epop_status = COALESCE(@epop_status, epop_status),
                epop_note = COALESCE(@epop_note, epop_note),
                epop_attachment = 
                CASE 
                    WHEN @remove_attachment = 1 THEN NULL
                    WHEN @epop_attachment IS NOT NULL THEN @epop_attachment
                    ELSE epop_attachment
                END
            WHERE
(
    (@epop_id IS NOT NULL AND epop_id = @epop_id)
    OR
    (@epop_id IS NULL AND @epop_epoi_id IS NOT NULL AND epop_epoi_id = @epop_epoi_id)
)
            ";

            using SqlCommand cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@epop_id", (object?)request.epop_id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_epoi_id", (object?)request.epop_epoi_id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_cust_id", (object?)request.epop_cust_id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_ir_id", (object?)request.epop_ir_id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_item_name", (object?)request.epop_item_name ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@epop_requested_qty", (object?)request.epop_requested_qty ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_requested_by", (object?)request.epop_requested_by ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_requested_date", (object?)request.epop_requested_date ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@epop_enquiry_by", (object?)request.epop_enquiry_by ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_enquiry_date", (object?)request.epop_enquiry_date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_enquiry_qty", (object?)request.epop_enquiry_qty ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@epop_order_qty", (object?)request.epop_order_qty ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_order_by", (object?)request.epop_order_by ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_order_date", (object?)request.epop_order_date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_order_price", (object?)request.epop_order_price ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@epop_received_qty", (object?)request.epop_received_qty ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_received_by", (object?)request.epop_received_by ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_received_date", (object?)request.epop_received_date ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@epop_damaged_qty", (object?)request.epop_damaged_qty ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_purchase_price", (object?)request.epop_purchase_price ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_supplier_id", (object?)request.epop_supplier_id ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@epop_tag", (object?)request.epop_tag ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_status", (object?)request.epop_status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_note", (object?)request.epop_note ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@epop_attachment", (object?)attachmentPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@remove_attachment",(object?)request.remove_attachment ?? DBNull.Value);

            int rows = cmd.ExecuteNonQuery();

            if (rows == 0)
            {
                return NotFound(new
                {
                    status = false,
                    statusCode = 404,
                    message = "Purchase order item not found"
                });
            }

            return Ok(new
            {
                status = true,
                statusCode = 200,
                message = "Edited successfully",
                data = rows
            });
        }

        [HttpPost("delete-ecommerce-purchase-order-par/{epop_id}")]
        public IActionResult DeletePurchaseOrderPar(decimal epop_id)
        {
            if (epop_id <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "Invalid epop id"
                });
            }

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string sql = @"
DELETE FROM inv_ecommerce_purchase_order_par
WHERE epop_id = @epop_id
";

            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@epop_id", epop_id);

            int rows = cmd.ExecuteNonQuery();

            if (rows == 0)
            {
                return NotFound(new
                {
                    status = false,
                    statusCode = 404,
                    message = "Purchase order item not found"
                });
            }

            return Ok(new
            {
                status = true,
                statusCode = 200,
                message = "Deleted successfully",
                data = rows
            });
        }

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-all-ecommerce-products")]
        public IActionResult GetAllEcommerceProducts(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        status = false,
                        statusCode = 401,
                        message = "Unauthorized: Invalid token.",
                        data = (object)null
                    });
                }

                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                string countSql = @"
SELECT COUNT(*)
FROM View_Stock
INNER JOIN inv_item_reg ON inv_item_reg.ir_id = View_Stock.ir_id
WHERE location_id = 1
  AND ir_active = 1";

                int totalRecords = Convert.ToInt32(ExecuteScalar(_connectionString, countSql));

                if (totalRecords == 0)
                {
                    return NotFound(new
                    {
                        status = false,
                        statusCode = 404,
                        message = "No products found",
                        data = (object)null
                    });
                }

                int offset = (pageNumber - 1) * pageSize;

                string productSql = $@"
SELECT
    View_Stock.ir_id AS ir_id,
    inv_item_reg.ir_name AS ir_name,
    inv_item_reg.ir_category_id AS ir_category_id,
    View_Stock.mrp AS mrp,
    View_Stock.qty AS stock,
    ISNULL(p.pending_qty, 0) AS pending_qty,
    (View_Stock.qty - ISNULL(p.pending_qty, 0)) AS available_qty,
    CASE WHEN wl.iw_product_id IS NOT NULL THEN 1 ELSE 0 END AS is_in_wishlist
FROM View_Stock
INNER JOIN inv_item_reg
    ON inv_item_reg.ir_id = View_Stock.ir_id
LEFT JOIN inv_wishlist wl
    ON wl.iw_product_id = View_Stock.ir_id
   AND wl.iw_user_id = @userId
LEFT JOIN (
    SELECT sp_ir_id, SUM(sp_qty) AS pending_qty
    FROM inv_sales_par
    WHERE sp_str_id = 3
      AND sp_narration1 IN ('pending', 'accepted')
    GROUP BY sp_ir_id
) p ON p.sp_ir_id = View_Stock.ir_id
WHERE location_id = 1
  AND ir_active = 1
ORDER BY View_Stock.ir_id ASC
OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                DataTable productTable = ExecuteDataTable(
                    _connectionString,
                    productSql,
                    new[] { new SqlParameter("@userId", userId) }
                );

                var productIds = productTable.AsEnumerable()
                    .Select(r => Convert.ToInt32(r["ir_id"]))
                    .ToList();

                if (!productIds.Any())
                {
                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Products fetched successfully",
                        data = new
                        {
                            pagination = new
                            {
                                page = pageNumber,
                                pageSize = pageSize,
                                totalRecords = totalRecords,
                                totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                            },
                            products = new List<object>()
                        }
                    });
                }

                string joinedIds = string.Join(",", productIds);

                string imageSql = $@"
SELECT
    ii_reference_id AS ProductId,
    ans.ans_status + ii_image AS Image
FROM inv_images img
CROSS JOIN android_settings ans
WHERE ans.ans_name = 'BaseUrl'
  AND ii_reference_type = 'item'
  AND ii_reference_id IN ({joinedIds})";

                DataTable imageTable = ExecuteDataTable(_connectionString, imageSql);

                var products = productTable.AsEnumerable()
                    .Select(row => new
                    {
                        ir_id = Convert.ToInt32(row["ir_id"]),
                        ir_name = row["ir_name"].ToString(),
                        ir_category_id = row["ir_category_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["ir_category_id"]),
                        ir_mrp = row["mrp"] == DBNull.Value ? 0 : Convert.ToDecimal(row["mrp"]),
                        stock = row["stock"] == DBNull.Value ? 0 : Convert.ToDecimal(row["stock"]),
                        available_qty = row["available_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["available_qty"]),
                        is_in_wishlist = Convert.ToInt32(row["is_in_wishlist"]) == 1,
                        images = imageTable.AsEnumerable()
                            .Where(img => Convert.ToInt32(img["ProductId"]) == Convert.ToInt32(row["ir_id"]))
                            .Select(img => img["Image"].ToString())
                            .Where(img => !string.IsNullOrWhiteSpace(img))
                            .Distinct()
                            .ToList()
                    }).ToList();

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Products fetched successfully",
                    data = new
                    {
                        pagination = new
                        {
                            page = pageNumber,
                            pageSize = pageSize,
                            totalRecords = totalRecords,
                            totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                        },
                        products = products
                    }
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("order-dashboard")]
        public IActionResult OrderDashboard([FromBody] OrderDashboardRequest request)
        {
            var role = User.FindFirst("UserRole")?.Value?.ToUpper();
            var ledgerIdStr = User.FindFirst("LedgerId")?.Value;

            decimal? ledgerId = null;
            if (decimal.TryParse(ledgerIdStr, out decimal lId))
                ledgerId = lId;

            bool isSalesPerson =
                role == "SALESMAN" ||
                role == "MARKETING EXECUTIVE";

            DateTime fromDate = request?.FromDate ?? DateTime.Today;
            DateTime toDate = request?.ToDate ?? DateTime.Today;

            List<string> allStatuses = new()
            {
                "Requested",
                "Enquiry",
                "Ordered",
                "Received"
            };

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            /* ============================
               1️⃣ STATUS SUMMARY
               ============================ */
            var statusSummary = allStatuses.ToDictionary(s => s, s => 0);

            SqlCommand summaryCmd = new SqlCommand(@"
        SELECT 
            epoi_status,
            COUNT(*) AS Total
        FROM inv_ecommerce_purchase_order_inf
        WHERE CAST(epoi_date AS date) BETWEEN CAST(@fromDate AS date) AND CAST(@toDate AS date)
          AND (@isSales = 0 OR epoi_salesman_id = @ledgerId)
        GROUP BY epoi_status
    ", conn);

            summaryCmd.Parameters.AddWithValue("@fromDate", fromDate);
            summaryCmd.Parameters.AddWithValue("@toDate", toDate);
            summaryCmd.Parameters.AddWithValue("@isSales", isSalesPerson ? 1 : 0);
            summaryCmd.Parameters.AddWithValue("@ledgerId", ledgerId ?? (object)DBNull.Value);

            using (SqlDataReader rdr = summaryCmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    string status = rdr["epoi_status"].ToString()!;
                    int total = Convert.ToInt32(rdr["Total"]);

                    if (statusSummary.ContainsKey(status))
                        statusSummary[status] = total;
                }
            }

            /* ============================
               2️⃣ ORDER LIST
               ============================ */
            SqlCommand listCmd = new SqlCommand(@"
        SELECT
            i.epoi_id,
            sh.as_name AS SalesmanName,
            CAST(i.epoi_date AS date) AS OrderDate,
            FORMAT(i.epoi_created_date, 'hh:mm tt') AS OrderTime,
            i.epoi_status AS Status
        FROM inv_ecommerce_purchase_order_inf i
        INNER JOIN acc_subhead sh ON sh.as_id = i.epoi_salesman_id
        WHERE CAST(i.epoi_date AS date) BETWEEN CAST(@fromDate AS date) AND CAST(@toDate AS date)
          AND (@status IS NULL OR i.epoi_status = @status)
          AND (@isSales = 0 OR i.epoi_salesman_id = @ledgerId)
        ORDER BY i.epoi_id DESC
    ", conn);

            listCmd.Parameters.AddWithValue("@fromDate", fromDate);
            listCmd.Parameters.AddWithValue("@toDate", toDate);
            listCmd.Parameters.AddWithValue(
                "@status",
                string.IsNullOrEmpty(request?.Status) ? DBNull.Value : request.Status
            );
            listCmd.Parameters.AddWithValue("@isSales", isSalesPerson ? 1 : 0);
            listCmd.Parameters.AddWithValue("@ledgerId", ledgerId ?? (object)DBNull.Value);

            DataTable dt = new DataTable();
            new SqlDataAdapter(listCmd).Fill(dt);

            var orders = dt.AsEnumerable().Select(r => new
            {
                orderNo = r["epoi_id"],
                SalesmanName = r["SalesmanName"],
                orderDate = Convert.ToDateTime(r["OrderDate"]).ToString("dd-MMM-yyyy"),
                orderTime = r["OrderTime"],
                status = r["Status"]
            }).ToList();

            /* ============================
               RESPONSE
               ============================ */
            return Ok(new
            {
                status = true,
                statusCode = 200,
                message = "Order dashboard loaded",
                dateRange = new
                {
                    from = fromDate.ToString("dd-MMM-yyyy"),
                    to = toDate.ToString("dd-MMM-yyyy")
                },
                statusSummary,
                data = orders
            });
        }

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("order-details")]
        public IActionResult GetOrderDetails(int OrderId)
        {
            if (OrderId <= 0)
                return BadRequest("Invalid OrderId");

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            /* ============================
               1️⃣ ORDER HEADER
               ============================ */
            SqlCommand headerCmd = new SqlCommand(@"
        SELECT 
            i.epoi_id,
            CAST(i.epoi_date AS date) AS OrderDate,
            sh.as_name AS SalesmanName
        FROM inv_ecommerce_purchase_order_inf i
        INNER JOIN acc_subhead sh ON sh.as_id = i.epoi_salesman_id
        WHERE i.epoi_id = @orderId
    ", conn);

            headerCmd.Parameters.AddWithValue("@orderId", OrderId);

            SqlDataReader hdr = headerCmd.ExecuteReader();
            if (!hdr.Read())
                return NotFound("Order not found");

            var header = new
            {
                orderNo = hdr["epoi_id"],
                orderDate = Convert.ToDateTime(hdr["OrderDate"]).ToString("dd-MMM-yyyy"),
                SalesmanName = hdr["SalesmanName"].ToString()
            };
            hdr.Close();

            /* ============================
               2️⃣ PRODUCT LIST
               ============================ */
            SqlCommand itemsCmd = new SqlCommand(@"
        SELECT 
        i.epop_id,
        i.epop_ir_id,
        i.epop_item_name,
        i.epop_cust_id,
	    isnull(s.as_name,'') as as_name,
        i.epop_qty,
        i.epop_status,
        i.epop_note,
        CASE 
            WHEN i.epop_attachment IS NOT NULL 
                 AND i.epop_attachment <> '' 
            THEN a.ans_status + i.epop_attachment
            ELSE ''
        END AS epop_attachment
    FROM inv_ecommerce_purchase_order_par i
    left join acc_subhead s on s.as_id=i.epop_cust_id
    CROSS JOIN android_settings a
    WHERE a.ans_name = 'BaseUrl' and i.epop_epoi_id = @orderId
    ORDER BY epop_id
    ", conn);

            itemsCmd.Parameters.AddWithValue("@orderId", OrderId);

            DataTable dt = new DataTable();
            new SqlDataAdapter(itemsCmd).Fill(dt);

            var items = dt.AsEnumerable().Select(r => new
            {
                orderParId = r["epop_id"],
                itemId = r["epop_ir_id"],
                itemName = r["epop_item_name"],
                customerId = r["epop_cust_id"],
                customerName = r["as_name"],
                qty = r["epop_qty"],
                status = r["epop_status"],   // Requested / Enquiry / Ordered / Received
                notes = r["epop_note"],
                attachments = r["epop_attachment"]
            }).ToList();

            return Ok(new
            {
                status = true,
                statusCode = 200,
                message = "Order details loaded",
                data = new
                {
                    header,
                    items
                }
            });
        }
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("update-order-item-status")]
        public IActionResult UpdateOrderItemStatus([FromBody] UpdateOrderItemStatusRequest request)
        {
            if (request == null ||
        string.IsNullOrEmpty(request.Status) ||
        request.OrderParIds == null ||
        request.OrderParIds.Count == 0)
            {
                return BadRequest("Invalid request");
            }

            string joinedIds = string.Join(",", request.OrderParIds);

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            SqlTransaction tran = conn.BeginTransaction();

            try
            {
                /* ============================
                   1️⃣ UPDATE ITEM STATUS
                   ============================ */
                SqlCommand updateItemCmd = new SqlCommand($@"
            UPDATE inv_ecommerce_purchase_order_par
            SET epop_status = @status
            WHERE epop_id IN ({joinedIds})
        ", conn, tran);

                updateItemCmd.Parameters.AddWithValue("@status", request.Status);
                int updated = updateItemCmd.ExecuteNonQuery();

                /* ============================
                   2️⃣ GET ORDER ID
                   ============================ */
                SqlCommand orderIdCmd = new SqlCommand(@"
            SELECT TOP 1 epop_epoi_id
            FROM inv_ecommerce_purchase_order_par
            WHERE epop_id IN (" + joinedIds + @")
        ", conn, tran);

                long orderId = Convert.ToInt64(orderIdCmd.ExecuteScalar());

                /* ============================
                   3️⃣ UPDATE HEADER STATUS
                   ============================ */

                // 🔹 If ALL items are Received → Completed
                SqlCommand headerStatusCmd = new SqlCommand(@"
                IF NOT EXISTS (
                    SELECT 1 
                    FROM inv_ecommerce_purchase_order_par
                    WHERE epop_epoi_id = @orderId
                      AND epop_status <> 'Ordered'
                )
                BEGIN
                    UPDATE inv_ecommerce_purchase_order_inf
                    SET epoi_status = 'Ordered'
                    WHERE epoi_id = @orderId
                END
                ELSE IF NOT EXISTS (
                    SELECT 1 
                    FROM inv_ecommerce_purchase_order_par
                    WHERE epop_epoi_id = @orderId
                      AND epop_status <> 'Received'
                )
                BEGIN
                    UPDATE inv_ecommerce_purchase_order_inf
                    SET epoi_status = 'Received'
                    WHERE epoi_id = @orderId
                END
                ELSE IF NOT EXISTS (
                    SELECT 1 
                    FROM inv_ecommerce_purchase_order_par
                    WHERE epop_epoi_id = @orderId
                      AND epop_status <> 'Enquiry'
                )
                BEGIN
                    UPDATE inv_ecommerce_purchase_order_inf
                    SET epoi_status = 'Enquiry'
                    WHERE epoi_id = @orderId
                END
                ", conn, tran);

                headerStatusCmd.Parameters.AddWithValue("@orderId", orderId);
                headerStatusCmd.ExecuteNonQuery();


                tran.Commit();

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Order item & header status updated",
                    updatedCount = updated
                });
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message
                });
            }
        }

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-ecommerce-users")]
        public IActionResult GetAllEcommerceUsers()
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();

                // ✅ Get BaseUrl
                string baseUrl = "";

                using (SqlCommand cmdBase = new SqlCommand(@"
            SELECT TOP 1 ans_status
            FROM android_settings
            WHERE ans_name = 'BaseUrl'
        ", conn))
                {
                    object result = cmdBase.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        baseUrl = result.ToString();
                    }
                }

                // ✅ Execute stored procedure
                using SqlCommand cmd = new SqlCommand(
                    "Sp_EcommerceLogin",
                    conn
                );

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue(
                    "@StatementType",
                    "Get"
                );

                using SqlDataAdapter da = new SqlDataAdapter(cmd);

                DataTable dt = new DataTable();

                da.Fill(dt);

                // ✅ Convert DataTable to normal JSON-safe list
                var users = dt.AsEnumerable()
                    .Select(row =>
                    {
                        var user = new Dictionary<string, object>();

                        foreach (DataColumn column in dt.Columns)
                        {
                            object value = row[column];

                            // DBNull → null
                            if (value == DBNull.Value)
                            {
                                user[column.ColumnName] = null;
                            }
                            else
                            {
                                user[column.ColumnName] = value;
                            }
                        }

                        // ✅ Profile image full URL
                        if (user.ContainsKey("gel_profile_image") &&
                            user["gel_profile_image"] != null)
                        {
                            string relativePath =
                                user["gel_profile_image"].ToString();

                            if (!string.IsNullOrWhiteSpace(relativePath))
                            {
                                user["gel_profile_image"] =
                                    baseUrl.TrimEnd('/') +
                                    "/" +
                                    relativePath.TrimStart('/');
                            }
                        }

                        return user;
                    })
                    .ToList();

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Fetched all ecommerce users successfully.",
                    data = users
                });
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message,
                    data = new object[0]
                });
            }
        }


        private List<Dictionary<string, object>> DataTableToList(DataTable table)
        {
            var list = new List<Dictionary<string, object>>();

            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                }
                list.Add(dict);
            }

            return list;
        }

        public static DataTable ExecuteDataTable(string connectionString, string sql, SqlParameter[] parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }
        public static int ExecuteNonQuery(string connectionString, string query, SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        //public static object ExecuteScalar(string connectionString, string query, SqlParameter[] parameters)
        //{
        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            if (parameters != null)
        //                cmd.Parameters.AddRange(parameters);

        //            conn.Open();
        //            return cmd.ExecuteScalar();
        //        }
        //    }
        //}
        public static object ExecuteScalar(string connectionString, string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    conn.Open();
                    return cmd.ExecuteScalar();
                }
            }
        }


    }
}
