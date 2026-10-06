using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static MictcoWebService.Models.EcartModel;

namespace MictcoWebService.Controllers
{
    [ApiController]
    public class EcartController : Controller
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;
        public EcartController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = SqlConnectionPool.Apply(configuration.GetConnectionString("ConnStr"));
        }
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("category-list-ecart")]
        public IActionResult GetCategoriesEcart(int pageNumber = 1, int pageSize = 10)
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
        [HttpGet("banner-list-with-url-ecart")]
        public IActionResult GetTwoTypeBannersWithUrlEcart()
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
        [HttpGet("section-wise-items-ecart/{sectionName}")]
        public IActionResult GetSectionsEcart(string sectionName, int pageNumber = 1, int pageSize = 1000)
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
        [HttpGet("ecommerce-search-category-ecart/{name}")]
        public IActionResult SearchCategoryByNameEcart(string name)
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

        
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("ecommerce-search-products-ecart/{name}")]
        public IActionResult SearchProductsEcart(string name, int? categoryId)
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
                    vs.qty AS stock,
                    ISNULL(p.pending_qty, 0) AS pending_qty,
                    (vs.qty - ISNULL(p.pending_qty, 0)) AS available_qty,
                    ans.ans_status + img.ii_image AS Image,
                    CASE 
                        WHEN wl.iw_product_id IS NOT NULL THEN 1 
                        ELSE 0 
                    END AS is_in_wishlist
                FROM View_Stock vs
                INNER JOIN inv_item_reg ir ON ir.ir_id = vs.ir_id 
                LEFT JOIN inv_images img 
                    ON img.ii_reference_id = vs.ir_id 
                   AND img.ii_reference_type = 'item'
                LEFT JOIN inv_wishlist wl 
                    ON wl.iw_product_id = vs.ir_id 
                   AND wl.iw_user_id = @userId
                LEFT JOIN (SELECT sp_ir_id,SUM(sp_qty) AS pending_qty FROM inv_sales_par WHERE sp_str_id=3 and sp_narration1 in ('pending','accepted') GROUP BY sp_ir_id) p ON p.sp_ir_id = vs.ir_id
                CROSS JOIN android_settings ans
                {whereClause}
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
                        Stock = Convert.ToInt32(row["stock"]),
                        available_qty = Convert.ToInt32(row["available_qty"]),
                        IsInWishlist = Convert.ToInt32(row["is_in_wishlist"])
                    })
                    .Select(g => new
                    {
                        id = g.Key.Id,
                        name = g.Key.Name,
                        //code = g.Key.Code,
                        mrp = g.Key.Mrp,
                        stock = g.Key.Stock,
                        available_qty = g.Key.available_qty,
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
        [HttpGet("ecommerce-products-by-category-ecart/{categoryId}")]
        public IActionResult GetProductsByCategoryEcart(int categoryId, int pageNumber = 1, int pageSize = 10)
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
                string countSql = @"SELECT COUNT(*) FROM View_Stock inner join inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id WHERE ir_category_id = " + categoryId + " and location_id=1 and ir_active = 1";
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
        [HttpPost("ecommerce-wishlist-ecart")]
        public IActionResult ToggleWishlistEcart([FromBody] WishlistToggleModelEcart model)
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
        [HttpGet("get-ecommerce-wishlist-ecart")]
        public IActionResult GetWishlistEcart(int pageNumber = 1, int pageSize = 10)
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
                string countSql = @"SELECT COUNT(*) 
                            FROM inv_wishlist 
                            INNER JOIN View_Stock  ON iw_product_id = ir_id
                            WHERE location_id=1  and iw_user_id = @userId";
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
                string productSql = $@"
                SELECT View_Stock.ir_id ir_id,inv_item_reg.ir_name as ir_name,View_Stock.mrp as mrp,View_Stock.qty as stock,ISNULL(p.pending_qty, 0) AS pending_qty,(View_Stock.qty - ISNULL(p.pending_qty, 0)) AS available_qty
				FROM inv_wishlist 
				INNER JOIN View_Stock  ON iw_product_id = ir_id
				INNER JOIN inv_item_reg on inv_item_reg.ir_id=View_Stock.ir_id
                LEFT JOIN (SELECT sp_ir_id,SUM(sp_qty) AS pending_qty FROM inv_sales_par WHERE sp_str_id=3 and sp_narration1 in ('pending','accepted') GROUP BY sp_ir_id) p ON p.sp_ir_id = View_Stock.ir_id
				WHERE location_id=1 and iw_user_id = @userId 
				ORDER BY ir_id ASC
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                var productParams = new[]
                {
                    new SqlParameter("@userId", SqlDbType.NVarChar) { Value = userId }
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
       
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("create-ecommerce-cart-ecart")]
        public IActionResult ToggleCart1Ecart([FromForm] CartToggleModel1Ecart model, IFormFile attachment)
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
                (c_customer_id, c_salesman_id, c_item_id, c_item_qty, c_price, c_remarks, c_attachment, c_user_id)
                VALUES (@customer_id, @salesman_id, @itemId, @qty, @price, @remarks, @attachment, @c_user_id)";

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
        [HttpPost("remove-ecommerce-cart-ecart/{cartId}")]
        public IActionResult RemoveCartEcart(int cartId)
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
        [HttpGet("get-ecommerce-cart-ecart")]
        public IActionResult GetCartEcart(int? customerId, int pageNumber = 1, int pageSize = 10)
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
                    .Select(row => new
                    {
                        CartId = Convert.ToInt32(row["CartId"]),
                        Date = Convert.ToDateTime(row["Date"]),
                        ItemId = Convert.ToInt32(row["ItemId"]),
                        ItemName = row["ItemName"]?.ToString(),
                        ItemMrp = row["mrp"] != DBNull.Value ? Convert.ToDecimal(row["mrp"]) : 0,
                        Qty = row["Qty"] != DBNull.Value ? Convert.ToDouble(row["Qty"]) : 1,
                        Price = row["Price"] != DBNull.Value ? Convert.ToDecimal(row["Price"]) : 0,
                        Remarks = row["Remarks"]?.ToString(),
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
        [HttpPost("update-ecommerce-cart-qty-ecart")]
        public IActionResult UpdateCartQtyEcart(int CartId, int Qty)
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
        [HttpGet("ecommerce-product-details-ecart/{productId}")]
        public IActionResult GetProductDetailsEcart(int productId)
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
        
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-customers-ecart")]
        public IActionResult GetCustomersEcart(int? areaId, int? routeId)
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
        [HttpGet("get-paginated-customers-ecart")]
        public IActionResult GetPaginatedCustomersEcart(int? areaId, int? routeId, int pageNumber = 1, int pageSize = 10)
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
        [HttpPost("checkout-cart-ecart")]
        public IActionResult CheckoutCartEcart([FromBody] CheckoutRequestEcart request)
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
                    si_distance,si_mode_of_transportation,si_vehicle_type,si_ag2_id,si_cost_centre,si_sales_order_no,si_ob,si_address_id
                    ) 
                    VALUES (
                        @si_str_id, @si_entryno, @si_date, @si_acc_id, @si_cust_name,@si_add1,@si_add2, @si_remarks,
                        @si_gross_value, @si_net_amount, @si_total, @si_user_id,@si_sales_acc_id,1,@si_salesman_id,1,0,
                        @si_profit,@si_grand_total,@si_commision_acc_id,@si_net_balance,@si_loc_entryno,@si_loc_entryno_only,0,@si_deliverydate,
                        '',10,@si_sum_item,@si_cash_paid_acc,0,0,NULL,0,0,'B2B',
                        'Retail',@si_functiondate,'MINUS','2000-01-26 00:00:00.000','2000-01-26 00:00:00.000','2000-01-26 00:00:00.000',@si_eway_date,0,@si_insert_user_id,
                        '','','','','','','','','','','','','','','','','','','','',@si_other_remarks,
                        '0','Road','Regular',0,0,0,@si_ob,@si_address_id
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
                        using (var getCmd = new SqlCommand(@"SELECT COALESCE(SUM(at_Dr)-SUM(at_Cr),0) FROM acc_account_transactions WHERE CAST(at_date AS DATE) <= '" + request.date + "' AND at_as_id = " + request.customerId + "", conn, tran))
                        {
                            ob = Convert.ToDecimal(getCmd.ExecuteScalar());
                        }
                        decimal netBalance = ob + request.total;

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
                            using (var getCmd = new SqlCommand("SELECT as_salesman_id FROM acc_subhead where as_id=" + ledgerId + "", conn, tran))
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
                        using (var getCmd = new SqlCommand("select gel_user_id from gnl_ecommerce_login where gel_id=" + userId + "", conn, tran))
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
                        cmdInf.Parameters.AddWithValue("@si_address_id", request.addressId);


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
                                "update inv_ecommerce_cart set c_customer_id= " + request.customerId + " WHERE c_user_id = @userId AND c_item_id = @itemId",
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
                        SELECT si_entryno, si_acc_id, '" + request.status + @"'
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
                        CheckoutResponseEcart responseData = null;
                        using (var cmd = new SqlCommand(headerSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@si_id", salesInfId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    responseData = new CheckoutResponseEcart
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
                                        cartItems = new List<CartItemresponseEcart>()
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
                                    var item = new CartItemresponseEcart
                                    {
                                        itemId = Convert.ToInt32(reader["itemId"]),  // itemId is INT in SQL
                                        itemName = reader["itemName"]?.ToString(),
                                        qty = Convert.ToDecimal(reader["qty"]),      // handles float/double → decimal
                                        price = Convert.ToDecimal(reader["price"]),  // handles float/double → decimal
                                        status = reader["status"]?.ToString(),
                                        remarks = reader["remarks"]?.ToString(),
                                        // ✅ Append BaseUrl if not empty
                                        attachment = string.IsNullOrEmpty(reader["attachment"]?.ToString()) ? "" : baseUrl.TrimEnd('/') + reader["attachment"].ToString()
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
       
        
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-checkout-ecart")]
        public IActionResult GetCheckoutListEcartEcart(int orderId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst("UserRole")?.Value;
            string baseUrl = ExecuteScalar(_connectionString, "SELECT ans_status FROM android_settings WHERE ans_name = 'BaseUrl'")?.ToString() ?? "";

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    status = false,
                    statusCode = 401,
                    message = "Unauthorized"
                });
            }

            if (orderId <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "OrderId is required"
                });
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    int loginId = Convert.ToInt32(userId);

                    // ✅ FETCH FALLBACK ADDRESS ONCE
                    EcartAddressModelEcart fallbackAddress = null;

                    if (userRole == "salesman")
                    {
                        // Get customer address from order
                        using (var fallbackCmd = new SqlCommand(@"
                        SELECT 
                            c.as_name,
                            c.as_mob,
                            c.as_add1,
                            c.as_add2,
                            c.as_add3
                        FROM inv_sales_inf si
                        INNER JOIN acc_subhead c ON c.as_id = si.si_acc_id
                        WHERE si.si_id = @orderId", conn))
                        {
                            fallbackCmd.Parameters.AddWithValue("@orderId", orderId);

                            using (var reader = fallbackCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    fallbackAddress = new EcartAddressModelEcart
                                    {
                                        FullName = reader["as_name"]?.ToString(),
                                        MobileNo = reader["as_mob"]?.ToString(),
                                        HouseName = reader["as_add1"]?.ToString(),
                                        Area = reader["as_add2"]?.ToString(),
                                        PinCode = reader["as_add3"]?.ToString(),
                                        AddressType = "PRIMARY",
                                        IsDefault = true
                                    };
                                }
                            }
                        }
                    }
                    else
                    {
                        // Existing fallback for customer login
                        using (var fallbackCmd = new SqlCommand(@"
                        SELECT 
                            as_name,
                            as_mob,
                            as_add1,
                            as_add2,
                            as_add3
                        FROM acc_subhead
                        LEFT JOIN gnl_ecommerce_login 
                            ON as_id = gel_ledger_id
                        WHERE gel_id = @loginId", conn))
                        {
                            fallbackCmd.Parameters.AddWithValue("@loginId", loginId);

                            using (var reader = fallbackCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    fallbackAddress = new EcartAddressModelEcart
                                    {
                                        FullName = reader["as_name"]?.ToString(),
                                        MobileNo = reader["as_mob"]?.ToString(),
                                        HouseName = reader["as_add1"]?.ToString(),
                                        Area = reader["as_add2"]?.ToString(),
                                        PinCode = reader["as_add3"]?.ToString(),
                                        AddressType = "PRIMARY",
                                        IsDefault = true
                                    };
                                }
                            }
                        }
                    }

                    #region HEADER + ADDRESS

                    string headerSql = @"
            SELECT 
                si.si_entryno AS orderNo,
                si.si_grand_total - ISNULL((
                SELECT SUM((sp.sp_total / NULLIF(sp.sp_qty,0)) * rp.srp_return_qty)
                FROM inv_sales_return_req_par rp
                INNER JOIN inv_sales_return_req_inf ri ON ri.srr_id = rp.srp_srr_id 
                INNER JOIN inv_sales_par sp  ON sp.sp_entryno = rp.srp_entryno  AND sp.sp_ir_id = rp.srp_ir_id
                WHERE rp.srp_entryno = si.si_entryno),0) AS totalAmount,
                si.si_acc_id AS customerId,
                c.as_name AS customerName,
                si.si_commision_acc_id AS salesmanId,
                s.as_name AS salesmanName,
                c.as_rout_id AS routeId,
                r.r_name AS routeName,
                c.as_area_id AS areaId,
                a.area_name AS areaName,
                si.si_date AS [date],
                si.si_other_remarks AS status,
                CASE 
                    WHEN EXISTS (
                        SELECT 1 
                        FROM inv_sales_return_req_inf 
                        WHERE srr_si_id = si.si_id
                    )
                    THEN CAST(1 AS BIT)
                    ELSE CAST(0 AS BIT)
                END AS isReturn,
                addr.eca_id AS addressId,
                addr.eca_name,
                addr.eca_mobile,
                addr.eca_add1,
                addr.eca_add2,
                addr.eca_add3,
                addr.eca_address_type,
                addr.eca_is_default
               
            FROM inv_sales_inf si
            INNER JOIN acc_subhead c ON c.as_id = si.si_acc_id
            LEFT JOIN acc_subhead s ON s.as_id = si.si_commision_acc_id
            LEFT JOIN inv_rout_reg r ON r.r_id = c.as_rout_id
            LEFT JOIN acc_area a ON a.area_id = c.as_area_id
            LEFT JOIN gnl_ecart_customer_address addr 
                   ON addr.eca_id = si.si_address_id

            WHERE si.si_str_id = 3 
            AND si.si_id = @orderId";

                    var checkoutList = new List<CheckoutResponseByOrderIdEcart>();

                    using (var cmd = new SqlCommand(headerSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@orderId", orderId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                EcartAddressModelEcart addressObj = null;

                                // ✅ If specific address exists
                                if (reader["addressId"] != DBNull.Value)
                                {
                                    addressObj = new EcartAddressModelEcart
                                    {
                                        FullName = reader["eca_name"]?.ToString(),
                                        MobileNo = reader["eca_mobile"]?.ToString(),
                                        HouseName = reader["eca_add1"]?.ToString(),
                                        Area = reader["eca_add2"]?.ToString(),
                                        PinCode = reader["eca_add3"]?.ToString(),
                                        AddressType = reader["eca_address_type"]?.ToString(),
                                        IsDefault = reader["eca_is_default"] != DBNull.Value &&
                                                    Convert.ToBoolean(reader["eca_is_default"])
                                    };
                                }
                                else
                                {
                                    // ✅ Use fallback
                                    addressObj = fallbackAddress;
                                }

                                checkoutList.Add(new CheckoutResponseByOrderIdEcart
                                {
                                    orderNo = Convert.ToInt32(reader["orderNo"]),
                                    totalAmount = Convert.ToDecimal(reader["totalAmount"]),
                                    customerId = Convert.ToInt32(reader["customerId"]),
                                    customerName = reader["customerName"]?.ToString(),
                                    salesmanId = reader["salesmanId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["salesmanId"]),
                                    salesmanName = reader["salesmanName"]?.ToString(),
                                    routeId = reader["routeId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["routeId"]),
                                    routeName = reader["routeName"]?.ToString(),
                                    areaId = reader["areaId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["areaId"]),
                                    areaName = reader["areaName"]?.ToString(),
                                    date = Convert.ToDateTime(reader["date"]),
                                    status = reader["status"]?.ToString(),
                                    isReturn = reader["isReturn"] != DBNull.Value && Convert.ToBoolean(reader["isReturn"]),
                                    address = addressObj,
                                    cartItems = new List<CartItemresponseByOrderIdEcart>(),
                                    statusList = new List<OrderStatusResponseEcart>()
                                });
                            }
                        }
                    }

                    #endregion

                    #region STATUS HISTORY

                    string statusSql = @"
                    SELECT sos_status AS status, sos_date AS date
                    FROM inv_sales_order_status
                    WHERE sos_orderId = @orderId
                    ORDER BY sos_date";

                    foreach (var order in checkoutList)
                    {
                        using (var cmd = new SqlCommand(statusSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@orderId", order.orderNo);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    order.statusList.Add(new OrderStatusResponseEcart
                                    {
                                        status = reader["status"].ToString(),
                                        date = Convert.ToDateTime(reader["date"])
                                    });
                                }
                            }
                        }
                    }

                    #endregion

                    #region ITEMS (RETURN-ADJUSTED QTY)

                    string itemsSql = @"
            SELECT 
                sp.sp_ir_id AS itemId,
                ir.ir_name AS itemName,

                -- ✅ delivered qty
                sp.sp_qty - ISNULL(SUM(rp.srp_return_qty), 0) AS qty,

                (sp.sp_total - ((sp.sp_total / NULLIF(sp.sp_qty,0)) * ISNULL(SUM(rp.srp_return_qty),0))) AS price,
                sp.sp_narration1 AS status,
                sp.sp_narration2 AS remarks,
                sp.sp_narration3 AS attachment,
                ans.ans_status + img.ii_image AS image
            FROM inv_sales_par sp
            INNER JOIN inv_item_reg ir ON ir.ir_id = sp.sp_ir_id

            LEFT JOIN inv_sales_return_req_par rp 
                ON rp.srp_ir_id = sp.sp_ir_id and rp.srp_entryno= sp.sp_entryno

            LEFT JOIN inv_images img 
                ON img.ii_reference_id = sp.sp_ir_id
               AND img.ii_reference_type = 'item'

            CROSS JOIN android_settings ans

            WHERE ans.ans_name = 'BaseUrl'
              AND sp.sp_str_id = 3
              AND sp.sp_entryno = @entryno

            GROUP BY 
                sp.sp_ir_id, ir.ir_name, sp.sp_qty,
                sp.sp_total, sp.sp_narration1,
                sp.sp_narration2, sp.sp_narration3,
                ans.ans_status, img.ii_image

            HAVING sp.sp_qty - ISNULL(SUM(rp.srp_return_qty), 0) > 0;
            ";

                    foreach (var order in checkoutList)
                    {
                        using (var cmd = new SqlCommand(itemsSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@entryno", order.orderNo);
                            using (var reader = cmd.ExecuteReader())
                            {
                                var itemDict = new Dictionary<int, CartItemresponseByOrderIdEcart>();

                                while (reader.Read())
                                {
                                    int itemId = Convert.ToInt32(reader["itemId"]);

                                    if (!itemDict.TryGetValue(itemId, out var item))
                                    {
                                        item = new CartItemresponseByOrderIdEcart
                                        {
                                            itemId = itemId,
                                            itemName = reader["itemName"].ToString(),
                                            qty = Convert.ToDecimal(reader["qty"]),
                                            price = Convert.ToDecimal(reader["price"]),
                                            status = reader["status"]?.ToString(),
                                            remarks = reader["remarks"]?.ToString(),
                                            attachment = string.IsNullOrEmpty(reader["attachment"]?.ToString())
                                                ? ""
                                                : baseUrl.TrimEnd('/') + reader["attachment"],
                                            images = new List<string>()
                                        };
                                        itemDict[itemId] = item;
                                    }

                                    if (reader["image"] != DBNull.Value)
                                    {
                                        var img = reader["image"].ToString();
                                        if (!item.images.Contains(img))
                                            item.images.Add(img);
                                    }
                                }

                                order.cartItems.AddRange(itemDict.Values);
                            }
                        }
                    }

                    #endregion

                    #region REMOVE FULLY RETURNED ORDERS

                    checkoutList = checkoutList
                        .Where(o => o.cartItems.Any())
                        .ToList();

                    #endregion

                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Checkout fetched successfully",
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
                    message = ex.Message
                });
            }
        }

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-checkout-list-ecart")]
        public IActionResult GetCheckoutListEcart(int customerId, int pageNumber = 1, int pageSize = 10)
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
                    using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM inv_sales_inf WHERE  si_str_id=3 and si_acc_id=" + customerId + "", conn))
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
                    WHERE si_from_mobile = 1 and si_str_id=3 and si_acc_id=" + customerId + @"
                    ORDER BY si_id DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

                    var checkoutList = new List<CheckoutResponseListEcart>();

                    using (var cmd = new SqlCommand(headerSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@offset", offset);
                        cmd.Parameters.AddWithValue("@pageSize", pageSize);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                checkoutList.Add(new CheckoutResponseListEcart
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
                                    cartItems = new List<CartItemresponseEcart>()
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
                                    order.cartItems.Add(new CartItemresponseEcart
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
        #region multiple salesman
        [HttpGet("get-ecommerce-sales-orders-ecart")]
        public IActionResult GetSalesOrdersEcart(
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
                      AND si_other_remarks !='Returned'
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
                            si_grand_total - ISNULL((
                            SELECT SUM((sp.sp_total / NULLIF(sp.sp_qty,0)) * rp.srp_return_qty)
                            FROM inv_sales_return_req_par rp
                            INNER JOIN inv_sales_return_req_inf ri ON ri.srr_id = rp.srp_srr_id 
                            INNER JOIN inv_sales_par sp  ON sp.sp_entryno = rp.srp_entryno  AND sp.sp_ir_id = rp.srp_ir_id
                            WHERE rp.srp_entryno = si_entryno),0) AS Total
                        FROM inv_sales_inf
                        LEFT JOIN acc_subhead a ON a.as_id = si_acc_id
                        LEFT JOIN acc_subhead b ON b.as_id = si_commision_acc_id
                        LEFT JOIN inv_rout_reg ON r_id = a.as_rout_id
                        LEFT JOIN acc_area ON a.as_area_id = area_id
                        WHERE si_str_id = 3  
                          AND {filterColumn} IN ({ledgerIdList})
                          AND si_other_remarks !='Returned'
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
        [HttpGet("get-ecommerce-sales-orders-by-status-ecart")]
        public IActionResult GetSalesOrdersByStatusEcart(int pageNumber = 1, int pageSize = 10, DateTime? fromDate = null, DateTime? toDate = null, string status = null)
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
                            si_grand_total - ISNULL((
                            SELECT SUM((sp.sp_total / NULLIF(sp.sp_qty,0)) * rp.srp_return_qty)
                            FROM inv_sales_return_req_par rp
                            INNER JOIN inv_sales_return_req_inf ri ON ri.srr_id = rp.srp_srr_id 
                            INNER JOIN inv_sales_par sp  ON sp.sp_entryno = rp.srp_entryno  AND sp.sp_ir_id = rp.srp_ir_id
                            WHERE rp.srp_entryno = si_entryno),0) AS Total
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


        #region Multiple salesaman
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-ecommerce-dashboard-ecart")]
        public IActionResult GetEcommerceDashboardEcart(DateTime? fromDate = null, DateTime? toDate = null)
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
                AND si_other_remarks != 'Returned'
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
                    SELECT 'Returned' UNION ALL
                    SELECT 'Delivered'
                ),
                ReturnOrders AS (
                    SELECT DISTINCT si.si_id
                    FROM inv_sales_inf si
                    INNER JOIN inv_sales_return_req_inf r 
                        ON r.srr_si_id = si.si_id
                    WHERE si.si_str_id = 3
                      AND {filterColumn} IN ({ledgerIdList})
                      AND CAST(si.si_date AS date) BETWEEN @fromDate AND @toDate
                )
                SELECT s.Status, CASE 
                WHEN s.Status = 'Returned' THEN 
                    (SELECT COUNT(*) FROM ReturnOrders)
                ELSE 
                    ISNULL(COUNT(i.si_id),0)
                END AS TotalCount
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

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-ecommerce-product-stock-ecart")]
        public async Task<IActionResult> GetProductStockEcart(int productId)
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

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("update-ecommerce-order-status-ecart")]
        public async Task<IActionResult> UpdateOrderStatusEcart(int orderId, string status)
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
        [HttpPost("cancel-check-out-ecart")]
        public async Task<IActionResult> CancelCheckoutEcart(int orderId)
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
        [HttpGet("ecommerce-user-active-ecart")]
        public async Task<IActionResult> UserActiveEcart()
        {
            var ledgerIdStr = User.FindFirst("LedgerId")?.Value;

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
                    data = new { LedgerId = ledgerIdStr }
                });
            }
        }
        // images
        [HttpPost("ecommerce-images-ecart")]
        public async Task<IActionResult> ManageImages1Ecart([FromForm] ImageUploadRequestEcart request, [FromQuery] string action, IFormFile imageFile)
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
        [HttpPost("sales-return-request-ecart")]
        public IActionResult CreateSalesReturnRequestEcart([FromBody] SalesReturnRequestDtoEcart request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (request == null || request.Items == null || !request.Items.Any())
                return BadRequest("Invalid return request");

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1️⃣ Insert header
                        var headerCmd = new SqlCommand(@"
                    INSERT INTO inv_sales_return_req_inf
                    (srr_si_id, srr_entryno, srr_customer_id, 
                     srr_reason, srr_attachment, srr_status, srr_created_by,srr_str_id)
                    SELECT si_id, si_entryno, si_acc_id,
                           @reason, @attachment, 'Requested', @userId,3
                    FROM inv_sales_inf
                    WHERE si_id = @orderId;

                    SELECT SCOPE_IDENTITY();",
                            conn, tran);

                        headerCmd.Parameters.AddWithValue("@orderId", request.OrderId);
                        headerCmd.Parameters.AddWithValue("@reason", request.Reason ?? "");
                        headerCmd.Parameters.AddWithValue("@attachment", request.Attachment ?? "");
                        headerCmd.Parameters.AddWithValue("@userId", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                        int srrId = Convert.ToInt32(headerCmd.ExecuteScalar());

                        // 2️⃣ Insert items
                        foreach (var item in request.Items)
                        {
                            var itemCmd = new SqlCommand(@"
                        INSERT INTO inv_sales_return_req_par(srp_srr_id, srp_entryno, srp_ir_id, srp_return_qty,srp_total,srp_str_id)
                        SELECT @srrId, si_entryno, @itemId, @qty,@total,3 FROM inv_sales_inf WHERE si_id = @orderId;",
                                conn, tran);

                            itemCmd.Parameters.AddWithValue("@srrId", srrId);
                            itemCmd.Parameters.AddWithValue("@itemId", item.ItemId);
                            itemCmd.Parameters.AddWithValue("@qty", item.ReturnQty);
                            itemCmd.Parameters.AddWithValue("@orderId", request.OrderId);
                            itemCmd.Parameters.AddWithValue("@total", item.TotalPrice);

                            itemCmd.ExecuteNonQuery();
                        }
                        // 3️⃣ Insert Status History (IMPORTANT)
                        var historyCmd = new SqlCommand(@"
                        INSERT INTO inv_sales_return_status_history
                        (srsh_srr_id, srsh_status, srsh_updated_by)
                        VALUES (@srrId, 'Requested', @userId)",
                            conn, tran);

                        historyCmd.Parameters.AddWithValue("@srrId", srrId);
                        historyCmd.Parameters.AddWithValue("@userId", userId);
                        historyCmd.ExecuteNonQuery();

                        var checkCmd = new SqlCommand(@"
                        SELECT COUNT(*)
                        FROM inv_sales_par sp
                        WHERE sp.sp_entryno = (
                            SELECT si_entryno 
                            FROM inv_sales_inf 
                            WHERE si_id = @orderId
                        )
                        AND sp.sp_str_id = 3
                        AND (
                            sp.sp_qty - ISNULL((
                                SELECT SUM(rp.srp_return_qty)
                                FROM inv_sales_return_req_par rp
                                WHERE rp.srp_entryno = sp.sp_entryno
                                  AND rp.srp_ir_id = sp.sp_ir_id
                            ),0)
                        ) > 0
                    ", conn, tran);

                        checkCmd.Parameters.AddWithValue("@orderId", request.OrderId);

                        int remainingItems = Convert.ToInt32(checkCmd.ExecuteScalar());


                        string newStatus = remainingItems == 0
                                           ? "Returned"
                                           : "Delivered";

                        var updateCmd = new SqlCommand(@"
                    UPDATE inv_sales_inf
                    SET si_other_remarks = @status
                    WHERE si_id = @orderId
                ", conn, tran);

                        updateCmd.Parameters.AddWithValue("@status", newStatus);
                        updateCmd.Parameters.AddWithValue("@orderId", request.OrderId);
                        updateCmd.ExecuteNonQuery();

                        var updateItemsCmd = new SqlCommand(@"
                        UPDATE p
                        SET p.sp_narration1 = @status
                        FROM INV_SALES_PAR p
                        WHERE p.sp_entryno = (
                            SELECT si_entryno 
                            FROM inv_sales_inf 
                            WHERE si_id = @orderId
                        )
                        AND p.sp_str_id = 3
                    ", conn, tran);

                        updateItemsCmd.Parameters.AddWithValue("@status", newStatus);
                        updateItemsCmd.Parameters.AddWithValue("@orderId", request.OrderId);
                        updateItemsCmd.ExecuteNonQuery();

                        tran.Commit();

                        return Ok(new
                        {
                            status = true,
                            message = "Return request submitted successfully"
                        });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, ex.Message);
                    }
                }
            }
        }
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-returned-sales-orders-ecart")]
        public IActionResult GetReturnedSalesOrdersEcart(int pageNumber = 1,int pageSize = 10,DateTime? fromDate = null,DateTime? toDate = null)
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

                    // ✅ Storekeeper / Delivery Boy → multiple salesmen
                    if (role.Equals("storekeeper", StringComparison.OrdinalIgnoreCase) ||
                        role.Equals("delivery boy", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var cmd = new SqlCommand(
                            @"SELECT sa_salesman_id 
                      FROM inv_salesman_allocation 
                      WHERE sa_ledger_id = @ledgerId", conn))
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

                        if (ledgerIds.Count == 0)
                            ledgerIds.Add(ledgerId);
                    }
                    else
                    {
                        ledgerIds.Add(ledgerId);
                    }

                    var from = fromDate ?? DateTime.Today;
                    var to = toDate ?? DateTime.Today;

                    string filterColumn = role == "customer" ? "si_acc_id" : "si_commision_acc_id";
                    string ledgerIdList = string.Join(",", ledgerIds.Select(id => $"'{id}'"));
                    int offset = (pageNumber - 1) * pageSize;

                    // ✅ 1️⃣ Count Only Returned Orders
                    int totalRecords = 0;
                    using (var countCmd = new SqlCommand($@"
                SELECT COUNT(DISTINCT si.si_id)
                FROM inv_sales_inf si
                INNER JOIN inv_sales_return_req_inf r 
                        ON r.srr_si_id = si.si_id
                WHERE si.si_str_id = 3
                  AND {filterColumn} IN ({ledgerIdList})
                  AND CAST(si.si_date AS date) 
                      BETWEEN @fromDate AND @toDate", conn))
                    {
                        countCmd.Parameters.AddWithValue("@fromDate", from);
                        countCmd.Parameters.AddWithValue("@toDate", to);

                        totalRecords = Convert.ToInt32(countCmd.ExecuteScalar());
                    }

                    // ✅ 2️⃣ Paginated Returned Orders
                    string sql = $@"
                WITH ReturnCTE AS (
                    SELECT DISTINCT
                        si.si_id AS OrderId,
                        si.si_entryno AS OrderNo,
                        si.si_acc_id AS CustomerId,
                        ISNULL(a.as_name, '') AS CustomerName,
                        a.as_rout_id AS RouteId,
                        ISNULL(r_name, '') AS RouteName,
                        a.as_area_id AS AreaId,
                        ISNULL(area_name, '') AS AreaName,
                        si.si_commision_acc_id AS SalesmanId,
                        ISNULL(b.as_name, '') AS SalesmanName,
                        si.si_date AS Date,
                        si.si_other_remarks AS Status,
                        r.srr_status AS ReturnStatus,
                         -- ✅ RETURN REQUEST TOTAL (NOT GRAND TOTAL)
                        (
                            SELECT SUM(rp.srp_total)
                            FROM inv_sales_return_req_par rp
                            WHERE rp.srp_srr_id = r.srr_id
                        ) AS Total
                    FROM inv_sales_inf si
                    INNER JOIN inv_sales_return_req_inf r 
                            ON r.srr_si_id = si.si_id
                    LEFT JOIN acc_subhead a ON a.as_id = si.si_acc_id
                    LEFT JOIN acc_subhead b ON b.as_id = si.si_commision_acc_id
                    LEFT JOIN inv_rout_reg ON r_id = a.as_rout_id
                    LEFT JOIN acc_area ON a.as_area_id = area_id
                    WHERE si.si_str_id = 3
                      AND {filterColumn} IN ({ledgerIdList})
                      AND CAST(si.si_date AS date)
                          BETWEEN @fromDate AND @toDate
                )
                SELECT *
                FROM ReturnCTE
                ORDER BY OrderNo DESC
                OFFSET @offset ROWS
                FETCH NEXT @pageSize ROWS ONLY;";

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
                                    ReturnStatus = reader["ReturnStatus"],
                                    Total = reader["Total"]
                                });
                            }
                        }
                    }

                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Returned sales orders fetched successfully",
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
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-sales-return-details-ecart")]
        public IActionResult GetSalesReturnDetailsEcart(int orderId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    status = false,
                    statusCode = 401,
                    message = "Unauthorized"
                });
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    int orderNo = 0;
                    string reason = "";
                    decimal totalAmount = 0;

                    var items = new List<object>();
                    var timeline = new List<object>();

                    #region 1️⃣ Get Order Number + Reason

                    string headerSql = @"
                SELECT TOP 1
                    si.si_entryno,
                    r.srr_reason
                FROM inv_sales_return_req_inf r
                INNER JOIN inv_sales_inf si ON si.si_id = r.srr_si_id
                WHERE r.srr_si_id = @orderId
                ORDER BY r.srr_request_date ASC";

                    using (var cmd = new SqlCommand(headerSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@orderId", orderId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                orderNo = reader["si_entryno"] != DBNull.Value
                                          ? Convert.ToInt32(reader["si_entryno"])
                                          : 0;

                                reason = reader["srr_reason"]?.ToString() ?? "";
                            }
                        }
                    }

                    #endregion

                    #region 2️⃣ Get Returned Items (Grouped)

                    string itemSql = @"
                SELECT 
                    ir.ir_name,
                    SUM(ISNULL(rp.srp_return_qty,0)) AS TotalQty,
                    SUM(ISNULL(rp.srp_total,0)) AS TotalAmount
                FROM inv_sales_return_req_par rp
                INNER JOIN inv_item_reg ir ON ir.ir_id = rp.srp_ir_id
                INNER JOIN inv_sales_return_req_inf ri 
                        ON ri.srr_id = rp.srp_srr_id
                WHERE ri.srr_si_id = @orderId
                GROUP BY ir.ir_name";

                    using (var cmd = new SqlCommand(itemSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@orderId", orderId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var amount = reader["TotalAmount"] != DBNull.Value
                                             ? Convert.ToDecimal(reader["TotalAmount"])
                                             : 0;

                                totalAmount += amount;

                                items.Add(new
                                {
                                    itemName = reader["ir_name"]?.ToString() ?? "",
                                    quantity = reader["TotalQty"] != DBNull.Value
                                               ? Convert.ToInt32(reader["TotalQty"])
                                               : 0,
                                    price = amount
                                });
                            }
                        }
                    }

                    #endregion

                    #region 3️⃣ Clean Timeline (No Duplicate Status)

                    string timelineSql = @"
                    SELECT 
                        h.srsh_status,
                        MAX(h.srsh_date) AS srsh_date
                    FROM inv_sales_return_status_history h
                    INNER JOIN inv_sales_return_req_inf r
                            ON r.srr_id = h.srsh_srr_id
                    WHERE r.srr_si_id = @orderId
                    GROUP BY h.srsh_status
                    ORDER BY MAX(h.srsh_date) ASC";

                    using (var cmd = new SqlCommand(timelineSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@orderId", orderId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                timeline.Add(new
                                {
                                    status = reader["srsh_status"]?.ToString() ?? "",
                                    date = reader["srsh_date"] != DBNull.Value
                                           ? Convert.ToDateTime(reader["srsh_date"])
                                           : (DateTime?)null
                                });
                            }
                        }
                    }

                    #endregion

                    return Ok(new
                    {
                        status = true,
                        statusCode = 200,
                        message = "Return details fetched successfully",
                        data = new
                        {
                            orderId,
                            orderNo = orderNo,
                            items = items,
                            timeline = timeline,
                            reason = reason,
                            totalAmount = totalAmount
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = "Error: " + ex.Message
                });
            }
        }


        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("approve-return-order-ecart")]
        public IActionResult ApproveOrderReturnEcart([FromBody] ReturnRequestEcart model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ledgerId = User.FindFirst("LedgerId")?.Value;
            var role = User.FindFirst("UserRole")?.Value;

            int tres = -6;

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

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                foreach (var row in model.ReturnItems ?? Enumerable.Empty<ReturnItemEcart>())
                {
                    if (row?.itemId == null) continue;

                    decimal uniquecode = 0, cost = 0, prate = 0, realprate = 0, mrp = 0;

                    // 🔹 STOCK FETCH
                    using (SqlCommand scmd = new SqlCommand(@"SELECT TOP 1 uniquecode, Cost, prate, realprate, mrp FROM view_stock WHERE ir_id=@ir_id AND qty>0", con))
                    {
                        scmd.Parameters.AddWithValue("@ir_id", row.itemId);
                        using (var r = scmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                uniquecode = r.IsDBNull(0) ? 0 : r.GetDecimal(0);
                                cost = r.IsDBNull(1) ? 0 : r.GetDecimal(1);
                                prate = r.IsDBNull(2) ? 0 : r.GetDecimal(2);
                                realprate = r.IsDBNull(3) ? 0 : r.GetDecimal(3);
                                mrp = r.IsDBNull(4) ? 0 : r.GetDecimal(4);
                            }
                        }
                    }
                    decimal sellingPricePerUnit = row.price;
                    decimal profit = (sellingPricePerUnit - prate) * row.qty;

                    DataRow dRow = dt1.NewRow();
                    dRow[0] = row.qty;
                    dRow[1] = 0;
                    dRow[2] = 0;
                    dRow[3] = 0;
                    dRow[4] = 0;
                    dRow[5] = 0;
                    dRow[6] = 0;
                    dRow[7] = 0;
                    dRow[8] = 0;
                    dRow[9] = "0";
                    dRow[10] = "0";
                    dRow[11] = "0";
                    dRow[12] = 0;
                    dRow[13] = 0;
                    dRow[14] = 0;

                    dRow[15] = uniquecode;
                    dRow[16] = row.itemId;
                    dRow[17] = mrp;

                    dRow[18] = 0;
                    dRow[19] = 0;
                    dRow[20] = 0;
                    dRow[21] = "";
                    dRow[22] = 0;
                    dRow[23] = 0;
                    dRow[24] = 0;
                    dRow[25] = 0;
                    dRow[26] = 0;
                    dRow[27] = "-1";

                    dRow[28] = prate;
                    dRow[29] = realprate;
                    dRow[30] = cost;

                    dRow[31] = "";
                    dRow[32] = 0;
                    dRow[33] = "";
                    dRow[34] = "0";
                    dRow[35] = 0;
                    dRow[36] = "";
                    dRow[37] = 0;
                    dRow[38] = "0";
                    dRow[39] = 0;
                    dRow[40] = 0;
                    dRow[41] = 0;
                    dRow[42] = "";
                    dRow[43] = "";

                    dt1.Rows.Add(dRow);
                }
                // Call SP
                try
                {
                    using (SqlCommand cmd = new SqlCommand("Sp_Sale", con))
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

                        // -------- CUSTOMER DETAILS --------
                        string custName = "", add1 = "", add2 = "";

                        using (SqlCommand ccmd = new SqlCommand("SELECT as_name, as_add1, as_add2 FROM acc_subhead WHERE as_id=@id", con))
                        {
                            ccmd.Parameters.AddWithValue("@id", model.customerId);
                            using (var r = ccmd.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    custName = r["as_name"]?.ToString() ?? "";
                                    add1 = r["as_add1"]?.ToString() ?? "";
                                    add2 = r["as_add2"]?.ToString() ?? "";
                                }
                            }
                        }

                        // -------- BASIC PARAMS --------
                        cmd.Parameters.AddWithValue("@si_str_id", 7);
                        cmd.Parameters.AddWithValue("@si_acc_id", model.customerId);
                        cmd.Parameters.AddWithValue("@si_date", model.date);
                        cmd.Parameters.AddWithValue("@si_deliverydate", model.date);
                        cmd.Parameters.AddWithValue("@si_cust_name", custName);
                        cmd.Parameters.AddWithValue("@si_add1", add1);
                        cmd.Parameters.AddWithValue("@si_add2", add2);
                        cmd.Parameters.AddWithValue("@si_remarks", "");
                        cmd.Parameters.AddWithValue("@si_gross_value", model.total);
                        cmd.Parameters.AddWithValue("@si_net_amount", model.total);
                        cmd.Parameters.AddWithValue("@si_total", model.total);
                        cmd.Parameters.AddWithValue("@si_grand_total", model.total);
                        cmd.Parameters.AddWithValue("@si_sales_acc_id", 2);

                        decimal ob = 0;
                        using (var getCmd = new SqlCommand(@"SELECT COALESCE(SUM(at_Dr)-SUM(at_Cr),0) FROM acc_account_transactions WHERE CAST(at_date AS DATE) <= '" + model.date + "' AND at_as_id = " + model.customerId + "", con))
                        {
                            ob = Convert.ToDecimal(getCmd.ExecuteScalar());
                        }
                        decimal netBalance = ob + model.total;

                        cmd.Parameters.AddWithValue("@si_ob", ob);
                        cmd.Parameters.AddWithValue("@si_net_balance", netBalance);

                        // -------- SALESMAN LOGIC --------

                        if (role.Equals("customer", StringComparison.OrdinalIgnoreCase))
                        {
                            int salesmanId = 1;
                            using (var getCmd = new SqlCommand("SELECT as_salesman_id FROM acc_subhead where as_id=" + ledgerId + "", con))
                            {
                                salesmanId = Convert.ToInt32(getCmd.ExecuteScalar());
                            }
                            cmd.Parameters.AddWithValue("@si_commision_acc_id", salesmanId);
                            cmd.Parameters.AddWithValue("@si_salesman_id", 0);
                        }
                        else if (role.Equals("marketing executive", StringComparison.OrdinalIgnoreCase))
                        {
                            int salesmanId = 1;
                            using (var getCmd = new SqlCommand("SELECT as_salesman_id FROM acc_subhead where as_id=" + model.customerId + "", con))
                            {
                                salesmanId = Convert.ToInt32(getCmd.ExecuteScalar());
                            }
                            int serviceId = 0;
                            using (var getCmd = new SqlCommand("SELECT as_isr_id FROM acc_subhead where as_id=" + ledgerId + "", con))
                            {
                                serviceId = Convert.ToInt32(getCmd.ExecuteScalar());
                            }
                            cmd.Parameters.AddWithValue("@si_commision_acc_id", ledgerId);
                            cmd.Parameters.AddWithValue("@si_salesman_id", serviceId);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@si_commision_acc_id", model.salesmanId == 0 ? -1 : model.salesmanId);
                            cmd.Parameters.AddWithValue("@si_salesman_id", 0);
                        }

                        // -------- USER MAP --------
                        int userIds = Convert.ToInt32(
                            new SqlCommand(
                                "SELECT gel_user_id FROM gnl_ecommerce_login WHERE gel_id=@id", con)
                            {
                                Parameters = { new SqlParameter("@id", userId) }
                            }.ExecuteScalar() ?? 0);

                        cmd.Parameters.AddWithValue("@si_user_id", userIds);


                        cmd.Parameters.AddWithValue("@si_cash_paid_acc", 1);
                        cmd.Parameters.AddWithValue("@si_other_remarks", model.status);
                        cmd.Parameters.AddWithValue("@si_location_id", 1);
                        decimal totalProfit = 0;

                        // First pass: calculate profit
                        foreach (var item in model.ReturnItems)
                        {
                            decimal prate = 0;

                            using (var getCmd = new SqlCommand("SELECT top 1 prate FROM view_stock WHERE ir_id = @ir_id and qty>0", con))
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
                        cmd.Parameters.AddWithValue("@si_profit", totalProfit);

                        // -------- TVP --------
                        SqlParameter tvp = cmd.Parameters.AddWithValue("@type1", dt1);
                        tvp.SqlDbType = SqlDbType.Structured;
                        cmd.Parameters.AddWithValue("@StatementType", "insert");

                        // -------- EXECUTE --------
                        cmd.ExecuteNonQuery();

                        if (Convert.ToInt32(errNumber.Value) != 0)
                        {
                            return StatusCode(500, new
                            {
                                status = false,
                                message = errMessage.Value?.ToString()
                            });
                        }

                        tres = Convert.ToInt32(parm.Value);
                    }

                    // -------- STATUS HISTORY --------
                    if (tres > 0)
                    {

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

            return Ok(new
            {
                status = true,
                statusCode = 200,
                message = "Delivery Return Successfully",
                data = tres
            });
        }
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("update-return-status")]
        public IActionResult UpdateReturnStatus(int orderId, string newStatus)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        int srrId = 0;

                        // 1️⃣ Get srr_id using srr_si_id (orderId)
                        var getIdCmd = new SqlCommand(@"SELECT TOP 1 srr_id FROM inv_sales_return_req_inf WHERE srr_si_id = @orderId", conn, tran);

                        getIdCmd.Parameters.AddWithValue("@orderId", orderId);

                        var result = getIdCmd.ExecuteScalar();

                        if (result == null)
                        {
                            return NotFound(new { status = false, message = "Return request not found" });
                        }

                        srrId = Convert.ToInt32(result);
                        // 1️⃣ Update main table
                        var updateCmd = new SqlCommand(@"UPDATE inv_sales_return_req_inf SET srr_status = @status WHERE srr_id = @srrId", conn, tran);

                        updateCmd.Parameters.AddWithValue("@status", newStatus);
                        updateCmd.Parameters.AddWithValue("@srrId", srrId);
                        updateCmd.ExecuteNonQuery();

                        // 2️⃣ Insert into history table
                        var historyCmd = new SqlCommand(@"INSERT INTO inv_sales_return_status_history(srsh_srr_id, srsh_status, srsh_updated_by) VALUES (@srrId, @status, 1)", conn, tran);

                        historyCmd.Parameters.AddWithValue("@srrId", srrId);
                        historyCmd.Parameters.AddWithValue("@status", newStatus);
                        historyCmd.ExecuteNonQuery();

                        tran.Commit();

                        return Ok(new { status = true });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, ex.Message);
                    }
                }
            }
        }

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("save-ecart-customer-ecart")]
        public async Task<IActionResult> SaveEcartCustomerEcart([FromForm] EcartCustomerModelEcart request, IFormFile profileImage)
        {
            if (request == null)
                return BadRequest("Invalid request");

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        int ledgerId = 0;
                        string relativePath = null;

                        /* 🔹 1️⃣ CHECK EXISTING LOGIN */
                        using (SqlCommand chkLogin = new SqlCommand(@"
                    SELECT ISNULL(gel_ledger_id,0) 
                    FROM gnl_ecommerce_login 
                    WHERE gel_login_id = @mobile
                ", conn, tran))
                        {
                            chkLogin.Parameters.AddWithValue("@mobile", request.MobileNo);
                            ledgerId = Convert.ToInt32(chkLogin.ExecuteScalar());
                        }

                        /* 🔹 2️⃣ DUPLICATE NAME CHECK */
                        using (SqlCommand chkName = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM acc_subhead
                    WHERE as_name = @name
                    AND (@ledgerId = 0 OR as_id <> @ledgerId)
                ", conn, tran))
                        {
                            chkName.Parameters.AddWithValue("@name", request.Name.Trim());
                            chkName.Parameters.AddWithValue("@ledgerId", ledgerId);

                            if (Convert.ToInt32(chkName.ExecuteScalar()) > 0)
                                return Conflict(new
                                {
                                    status = false,
                                    message = "Account name already exists"
                                });
                        }

                        /* 🔹 3️⃣ IMAGE UPLOAD (OPTIONAL) */
                        if (profileImage != null && profileImage.Length > 0)
                        {
                            var uploads = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot", "uploads", "users");

                            if (!Directory.Exists(uploads))
                                Directory.CreateDirectory(uploads);

                            string fileName = Guid.NewGuid() + Path.GetExtension(profileImage.FileName);
                            string filePath = Path.Combine(uploads, fileName);

                            using (FileStream fs = new FileStream(filePath, FileMode.Create))
                            {
                                await profileImage.CopyToAsync(fs);
                            }

                            relativePath = "/uploads/users/" + fileName;
                        }

                        /* 🔹 4️⃣ INSERT / UPDATE acc_subhead */
                        if (ledgerId == 0)
                        {
                            // 👉 INSERT
                            using (SqlCommand ins = new SqlCommand(@"
                        DECLARE @LedgerId INT;
                        INSERT INTO acc_subhead
                        (
                            as_location_id, as_mob, as_add1, as_add2, as_add3,
                            as_name, as_ap_id, as_pincode, as_active, as_date
                        )
                        VALUES
                        (
                            1, @mob, @add1, @add2, @add3,
                            @name, 4, @pincode, 1, GETDATE()
                        );
                        SET @LedgerId = SCOPE_IDENTITY();

                        UPDATE acc_subhead
                        SET as_account_code = @LedgerId
                        WHERE as_id = @LedgerId;
                        SELECT @LedgerId;
                    ", conn, tran))
                            {
                                ins.Parameters.AddWithValue("@mob", request.MobileNo);
                                ins.Parameters.AddWithValue("@add1", request.Add1 ?? "");
                                ins.Parameters.AddWithValue("@add2", request.Add2 ?? "");
                                ins.Parameters.AddWithValue("@add3", request.Add3 ?? "");
                                ins.Parameters.AddWithValue("@name", request.Name);
                                ins.Parameters.AddWithValue("@pincode", request.PinCode ?? "");

                                ledgerId = Convert.ToInt32(ins.ExecuteScalar());
                            }

                            using (SqlCommand updLogin = new SqlCommand(@"
                        UPDATE gnl_ecommerce_login
                        SET gel_ledger_id = @ledgerId,
                            gel_profile_image = ISNULL(@img, gel_profile_image)
                        WHERE gel_login_id = @mobile
                    ", conn, tran))
                            {
                                updLogin.Parameters.AddWithValue("@ledgerId", ledgerId);
                                updLogin.Parameters.AddWithValue("@mobile", request.MobileNo);
                                updLogin.Parameters.AddWithValue("@img", (object)relativePath ?? DBNull.Value);
                                updLogin.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // 👉 UPDATE
                            using (SqlCommand upd = new SqlCommand(@"
                        UPDATE acc_subhead
                        SET
                            as_mob = @mob,
                            as_add1 = @add1,
                            as_add2 = @add2,
                            as_add3 = @add3,
                            as_name = @name,
                            as_pincode = @pincode
                        WHERE as_id = @ledgerId
                    ", conn, tran))
                            {
                                upd.Parameters.AddWithValue("@ledgerId", ledgerId);
                                upd.Parameters.AddWithValue("@mob", request.MobileNo);
                                upd.Parameters.AddWithValue("@add1", request.Add1 ?? "");
                                upd.Parameters.AddWithValue("@add2", request.Add2 ?? "");
                                upd.Parameters.AddWithValue("@add3", request.Add3 ?? "");
                                upd.Parameters.AddWithValue("@name", request.Name);
                                upd.Parameters.AddWithValue("@pincode", request.PinCode ?? "");
                                upd.ExecuteNonQuery();
                            }

                            if (relativePath != null)
                            {
                                using (SqlCommand updImg = new SqlCommand(@"
                            UPDATE gnl_ecommerce_login
                            SET gel_profile_image = @img
                            WHERE gel_login_id = @mobile
                        ", conn, tran))
                                {
                                    updImg.Parameters.AddWithValue("@img", relativePath);
                                    updImg.Parameters.AddWithValue("@mobile", request.MobileNo);
                                    updImg.ExecuteNonQuery();
                                }
                            }
                        }

                        tran.Commit();

                        return Ok(new
                        {
                            status = true,
                            statusCode = 200,
                            message = ledgerId == 0 ? "Customer created" : "Customer updated",
                            data = ledgerId
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
            }
        }
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("get-ecart-customer")]
        public IActionResult GetEcartCustomerEcart([FromQuery] string mobileNo)
        {
            if (string.IsNullOrEmpty(mobileNo))
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "Mobile number is required",
                    data = (object)null
                });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string baseUrl = ExecuteScalar(_connectionString, "SELECT ans_status FROM android_settings WHERE ans_name = 'BaseUrl'")?.ToString();

                    using (SqlCommand cmd = new SqlCommand(@"
                SELECT 
                    l.gel_login_id        AS MobileNo,
                    l.gel_ledger_id       AS LedgerId,
                    l.gel_profile_image   AS ProfileImage,
                    a.as_name             AS Name,
                    a.as_add1             AS Add1,
                    a.as_add2             AS Add2,
                    a.as_add3             AS Add3,
                    a.as_pincode          AS PinCode,
                    a.as_account_code     AS AccountCode
                FROM gnl_ecommerce_login l
                LEFT JOIN acc_subhead a 
                    ON a.as_id = l.gel_ledger_id
                WHERE l.gel_login_id = @mobile
            ", conn))
                    {
                        cmd.Parameters.AddWithValue("@mobile", mobileNo);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                return NotFound(new
                                {
                                    status = false,
                                    statusCode = 404,
                                    message = "Customer not found",
                                    data = (object)null
                                });
                            }

                            var data = new
                            {
                                MobileNo = dr["MobileNo"].ToString(),
                                LedgerId = dr["LedgerId"],
                                Name = dr["Name"]?.ToString(),
                                Add1 = dr["Add1"]?.ToString(),
                                Add2 = dr["Add2"]?.ToString(),
                                Add3 = dr["Add3"]?.ToString(),
                                PinCode = dr["PinCode"]?.ToString(),
                                AccountCode = dr["AccountCode"]?.ToString(),
                                ProfileImage = string.IsNullOrEmpty(dr["ProfileImage"]?.ToString())
                                ? ""
                                : baseUrl.TrimEnd('/') + dr["ProfileImage"].ToString()
                            };

                            return Ok(new
                            {
                                status = true,
                                statusCode = 200,
                                message = "Ecart customer fetched successfully",
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
        [HttpPost("add-delivery-address-ecart")]
        public IActionResult AddDeliveryAddressEcart([FromBody] EcartAddressModelEcart request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (request == null)
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "Invalid request",
                    data = (object)null
                });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        try
                        {
                            // 🔹 If default address → unset previous default
                            if (request.IsDefault)
                            {
                                using (SqlCommand reset = new SqlCommand(@"
                            UPDATE gnl_ecart_customer_address
                            SET eca_is_default = 0
                            WHERE eca_login_id = @loginId
                        ", conn, tran))
                                {
                                    reset.Parameters.AddWithValue("@loginId", userId);
                                    reset.ExecuteNonQuery();
                                }
                            }

                            int addressId;

                            using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO gnl_ecart_customer_address
                            (
                                eca_login_id, eca_name, eca_mobile,
                                eca_add1, eca_add2, eca_add3,
                                eca_address_type, eca_is_default
                            )
                            VALUES
                            (
                                @loginId, @name, @mobile,
                                @house, @area, @pincode,
                                @type, @isDefault
                            );

                            SELECT CAST(SCOPE_IDENTITY() AS INT);
                        ", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@loginId", userId);
                                cmd.Parameters.AddWithValue("@name", request.FullName);
                                cmd.Parameters.AddWithValue("@mobile", request.MobileNo);
                                cmd.Parameters.AddWithValue("@house", request.HouseName);
                                cmd.Parameters.AddWithValue("@area", request.Area);
                                cmd.Parameters.AddWithValue("@pincode", request.PinCode);
                                cmd.Parameters.AddWithValue("@type", request.AddressType);
                                cmd.Parameters.AddWithValue("@isDefault", 1);

                                addressId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            tran.Commit();

                            return Ok(new
                            {
                                status = true,
                                statusCode = 200,
                                message = "Delivery address added successfully",
                                data = new { AddressId = addressId }
                            });
                        }
                        catch (Exception)
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
                    message = ex.Message,
                    data = (object)null
                });
            }
        }
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpGet("delivery-address-list-ecart")]
        public IActionResult GetDeliveryAddressListEcart()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(@"
                       SELECT
                        eca_login_id,
                        eca_id              AS address_id,
                        eca_name            AS name,
                        eca_mobile          AS mobile,
                        eca_add1            AS add1,
                        eca_add2            AS add2,
                        eca_add3            AS add3,
                        eca_address_type    AS address_type,
                        eca_is_default      AS is_default
                    FROM gnl_ecart_customer_address
                    WHERE eca_login_id = @loginId
                      AND eca_active = 1

                    UNION ALL

                    SELECT
                        gel_id              AS eca_login_id,
                        -1               AS address_id,
                        as_name             AS name,
                        as_mob              AS mobile,
                        as_add1             AS add1,
                        as_add2             AS add2,
                        as_add3             AS add3,
                        'PRIMARY'           AS address_type,
                        1                   AS is_default
                    FROM acc_subhead
                    LEFT JOIN gnl_ecommerce_login 
                        ON as_id = gel_ledger_id
                    WHERE gel_id = @loginId

                    ORDER BY address_id asc

                    ", conn))
                    {
                        cmd.Parameters.AddWithValue("@loginId", userId);

                        DataTable dt = new DataTable();
                        dt.Load(cmd.ExecuteReader());
                        var responseObj = new
                        {
                            status = true,
                            statusCode = 200,
                            message = "Address list fetched successfully",
                            data = dt
                        };
                        return Content(Newtonsoft.Json.JsonConvert.SerializeObject(responseObj), "application/json");

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
        [HttpPost("update-delivery-address-ecart/{addressId}")]
        public IActionResult UpdateDeliveryAddressEcart(int addressId, [FromBody] EcartAddressModelEcart model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (addressId <= 0)
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "Invalid address id"
                });

            if (model == null)
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "Invalid request body"
                });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Reset default if needed
                    if (model.IsDefault)
                    {
                        using SqlCommand resetCmd = new SqlCommand(@"
                    UPDATE gnl_ecart_customer_address
                    SET eca_is_default = 0
                    WHERE eca_login_id = @loginId
                      AND eca_active = 1
                ", conn);

                        resetCmd.Parameters.AddWithValue("@loginId", userId);
                        resetCmd.ExecuteNonQuery();
                    }

                    using SqlCommand cmd = new SqlCommand(@"
                UPDATE gnl_ecart_customer_address
                SET
                    eca_name         = @name,
                    eca_mobile       = @mobile,
                    eca_add1         = @add1,
                    eca_add2         = @add2,
                    eca_add3         = @add3,
                    eca_address_type = @addressType,
                    eca_is_default   = @isDefault
                WHERE
                    eca_id = @addressId
                    AND eca_login_id = @loginId
                    AND eca_active = 1
            ", conn);

                    cmd.Parameters.AddWithValue("@name", model.FullName ?? "");
                    cmd.Parameters.AddWithValue("@mobile", model.MobileNo ?? "");
                    cmd.Parameters.AddWithValue("@add1", model.HouseName ?? "");
                    cmd.Parameters.AddWithValue("@add2", model.Area ?? "");
                    cmd.Parameters.AddWithValue("@add3", model.PinCode ?? "");
                    cmd.Parameters.AddWithValue("@addressType", model.AddressType ?? "HOME");
                    cmd.Parameters.AddWithValue("@isDefault", model.IsDefault ? 1 : 0);
                    cmd.Parameters.AddWithValue("@addressId", addressId);
                    cmd.Parameters.AddWithValue("@loginId", userId);

                    int rows = cmd.ExecuteNonQuery();

                    if (rows == 0)
                    {
                        return NotFound(new
                        {
                            status = false,
                            statusCode = 404,
                            message = "Address not found or already deleted",
                            data = (object)null
                        });
                    }
                }

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Delivery address updated successfully",
                    data = addressId
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

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("delete-delivery-address-ecart/{addressId}")]
        public IActionResult DeleteDeliveryAddressEcart(int addressId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (addressId <= 0)
                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "Invalid address id",
                    data = (object)null
                });

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(@"
                UPDATE gnl_ecart_customer_address
                SET 
                    eca_active = 0
                WHERE 
                    eca_id = @addressId
                    AND eca_login_id = @loginId
                    AND eca_active = 1
            ", conn))
                    {
                        cmd.Parameters.AddWithValue("@addressId", addressId);
                        cmd.Parameters.AddWithValue("@loginId", userId);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows == 0)
                        {
                            return NotFound(new
                            {
                                status = false,
                                statusCode = 404,
                                message = "Address not found or already deleted",
                                data = (object)null
                            });
                        }
                    }
                }

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    message = "Delivery address deleted successfully",
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

        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("report-issue-ecart")]
        public async Task<IActionResult> ReportIssueEcart([FromForm] EcartIssueReportModel request,IFormFile attachment)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            if (request.Category == null || !request.Category.Any() ||
            string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Category and Description are required"
                });
            }

            int userId = Convert.ToInt32(userIdClaim);

            using SqlConnection conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using SqlTransaction tran = conn.BeginTransaction();

            try
            {
                /* 🔹 GET BASE URL */
                string baseUrl;
                using (SqlCommand baseCmd = new SqlCommand(
                    "SELECT ans_status FROM android_settings WHERE ans_name = 'BaseUrl'",
                    conn, tran))
                {
                    baseUrl = (await baseCmd.ExecuteScalarAsync())?.ToString() ?? "";
                }

                /* 🔹 FILE UPLOAD */
                string? relativePath = null;

                if (attachment != null && attachment.Length > 0)
                {
                    string uploadsFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot", "uploads", "issues");

                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(attachment.FileName)}";
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    await using (FileStream fs = new FileStream(filePath, FileMode.Create))
                    {
                        await attachment.CopyToAsync(fs);
                    }

                    relativePath = $"/uploads/issues/{fileName}";
                }
                string? category = request.Category != null && request.Category.Any()
                ? string.Join(",", request.Category)
                : null;
                /* 🔥 INSERT + RETURN DATA IN ONE QUERY */
                using (SqlCommand cmd = new SqlCommand(@"
                INSERT INTO gnl_ecart_issue_report
                (
                    gir_user_id,
                    gir_category,
                    gir_description,
                    gir_attachment,
                    gir_created_by,
                    gir_created_at
                )
                OUTPUT 
                    INSERTED.gir_id,
                    INSERTED.gir_user_id,
                    INSERTED.gir_category,
                    INSERTED.gir_description,
                    INSERTED.gir_attachment,
                    INSERTED.gir_status,
                    INSERTED.gir_created_at
                VALUES
                (
                    @userId,
                    @category,
                    @description,
                    @attachment,
                    @createdBy,
                    GETDATE()
                );
            ", conn, tran))
                {
                    cmd.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                    cmd.Parameters.Add("@category", SqlDbType.NVarChar).Value =
                        (object?)category ?? DBNull.Value; 
                    cmd.Parameters.Add("@description", SqlDbType.NVarChar).Value = request.Description;
                    cmd.Parameters.Add("@attachment", SqlDbType.NVarChar).Value =
                        (object?)relativePath ?? DBNull.Value;
                    cmd.Parameters.Add("@createdBy", SqlDbType.Int).Value = userId;

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        string? dbAttachment =
                            reader["gir_attachment"] == DBNull.Value
                                ? null
                                : reader["gir_attachment"].ToString();

                        string fullAttachmentUrl = string.IsNullOrEmpty(dbAttachment)
                            ? ""
                            : (!string.IsNullOrEmpty(baseUrl)
                                ? $"{baseUrl.TrimEnd('/')}/{dbAttachment.TrimStart('/')}"
                                : dbAttachment);

                        var result = new
                        {
                            reportId = Convert.ToInt32(reader["gir_id"]),
                            userId = Convert.ToInt32(reader["gir_user_id"]),
                            category = reader["gir_category"]?.ToString(),
                            description = reader["gir_description"]?.ToString(),
                            attachment = fullAttachmentUrl,
                            status = reader["gir_status"]?.ToString(),
                            createdAt = reader["gir_created_at"]
                        };

                        await reader.CloseAsync();
                        await tran.CommitAsync();

                        return Ok(new
                        {
                            status = true,
                            statusCode = 200,
                            message = "Issue reported successfully",
                            data = result
                        });
                    }
                }

                await tran.RollbackAsync();

                return BadRequest(new
                {
                    status = false,
                    statusCode = 400,
                    message = "Insert failed",
                    data = (object?)null
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();

                return StatusCode(500, new
                {
                    status = false,
                    statusCode = 500,
                    message = ex.Message,
                    data = (object?)null
                });
            }
        }
        [Authorize(Roles = UserRoles.EcommerceUser)]
        [HttpPost("submit-feedback")]
        public async Task<IActionResult> SubmitFeedback([FromBody] EcartFeedbackModel request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest("Rating must be between 1 and 5");

            int userId = Convert.ToInt32(userIdClaim);

            // 🔹 Convert list to comma separated string
            string? options = request.LikedOptions != null && request.LikedOptions.Any()
                ? string.Join(",", request.LikedOptions)
                : null;

            using SqlConnection conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand(@"
        INSERT INTO gnl_ecart_feedback
        (
            gef_user_id,
            gef_rating,
            gef_option,
            gef_comment,
            gef_created_by,
            gef_created_at
        )
        VALUES
        (
            @userId,
            @rating,
            @option,
            @comment,
            @createdBy,
            GETDATE()
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);
    ", conn);

            cmd.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@rating", SqlDbType.Int).Value = request.Rating;
            cmd.Parameters.Add("@option", SqlDbType.NVarChar).Value =
                (object?)options ?? DBNull.Value;
            cmd.Parameters.Add("@comment", SqlDbType.NVarChar).Value =
                (object?)request.Comment ?? DBNull.Value;
            cmd.Parameters.Add("@createdBy", SqlDbType.Int).Value = userId;

            int insertedId = (int)(await cmd.ExecuteScalarAsync() ?? 0);

            return Ok(new
            {
                status = true,
                statusCode = 200,
                message = "Feedback submitted successfully",
                data = new
                {
                    feedbackId = insertedId,
                    rating = request.Rating,
                    options,
                    comment = request.Comment
                }
            });
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
