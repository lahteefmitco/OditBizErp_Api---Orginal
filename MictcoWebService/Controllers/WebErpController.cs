using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    [Route("api/[controller]")]
    public class WebErpController : ControllerBase
    {
        [HttpGet("stock-category-list")]
        public IActionResult CategoryList()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string query = @"
                    SELECT
                        c_id,
                        c_name
                    FROM inv_category
                    ORDER BY c_name";

                DataTable dt = usqlre.dbReaderFill(query);

                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
            finally
            {
                usqlre.close();
            }
        }

        [HttpGet("stock-brand-list")]
        public IActionResult BrandList()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string query = @"
            SELECT
                bra_id,
                bra_name
            FROM inv_brand
            ORDER BY bra_name";

                DataTable dt = usqlre.dbReaderFill(query);

                usqlre.close();

                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("stock-item-list")]
        public IActionResult ItemList()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string query = @"
            SELECT
                ir_id,
                ir_code,
                ir_name
            FROM inv_item_reg
            WHERE ir_active = 1
            ORDER BY ir_name";

                DataTable dt = usqlre.dbReaderFill(query);

                usqlre.close();

                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("stock-item-list-with-barcode")]
        public IActionResult ItemListWithBarcode()
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql = "";
                bool productNameOther = false;
                bool productAliasColumn = false;
                bool disableLocationBasedStock = false;

                if (usqlre.get_android_settings("PRODUCT NAME ALTERNATIVE"))
                    productNameOther = true;

                if (usqlre.get_android_settings("DISABLE LOCATION BASED STOCK_IN PRODUCT LIST"))
                    disableLocationBasedStock = true;

                if (!disableLocationBasedStock)
                {
                    if (productNameOther)
                    {
                        if (usqlre.get_android_settings("PRODUCT NAME ALIASCOLUMN"))
                            productAliasColumn = true;

                        if (productAliasColumn)
                        {
                            sql = @"WITH LatestStock AS
                    (
                        SELECT ir_id,Cost,mrp,retail,wholesale,spretail,branch,
                        ROW_NUMBER() OVER(PARTITION BY ir_id ORDER BY uniquecode DESC) rn
                        FROM View_Stock
                    )
                    SELECT
                        i.ir_name AS label,
                        v.ir_id AS value,
                        i.ir_alias_name AS aliasname,
                        i.ir_rawmaterial,
                        i.ir_lend_validation AS isLend,
                        ls.Cost AS PurchaseRate,
                        ls.mrp AS Mrp,
                        ls.retail AS Retail,
                        ls.wholesale AS Wholesale,
                        ls.spretail AS SpRetail,
                        ls.branch AS Branch,
                        SUM(v.qty) AS Stock,
                        i.ir_batch_status AS BatchStatus,
                        v.uniquecode,
                        v.int_barcode
                    FROM View_Stock v
                    INNER JOIN LatestStock ls
                        ON v.ir_id=ls.ir_id AND ls.rn=1
                    INNER JOIN inv_item_reg i
                        ON v.ir_id=i.ir_id
                    WHERE i.ir_active=1
                    AND v.location_id='" + usqlre.locationId + @"'
                    GROUP BY
                        i.ir_name,
                        i.ir_alias_name,
                        i.ir_rawmaterial,
                        i.ir_lend_validation,
                        v.ir_id,
                        ls.Cost,
                        ls.mrp,
                        ls.retail,
                        ls.wholesale,
                        ls.spretail,
                        ls.branch,
                        i.ir_batch_status,
                        v.uniquecode,
                        v.int_barcode";
                        }
                        else
                        {
                            sql = @"WITH LatestStock AS
                    (
                        SELECT ir_id,Cost,mrp,retail,wholesale,spretail,branch,
                        ROW_NUMBER() OVER(PARTITION BY ir_id ORDER BY uniquecode DESC) rn
                        FROM View_Stock
                    )
                    SELECT
                        i.ir_name AS label,
                        v.ir_id AS value,
                        i.ir_hsn_code AS aliasname,
                        i.ir_rawmaterial,
                        i.ir_lend_validation AS isLend,
                        ls.Cost AS PurchaseRate,
                        ls.mrp AS Mrp,
                        ls.retail AS Retail,
                        ls.wholesale AS Wholesale,
                        ls.spretail AS SpRetail,
                        ls.branch AS Branch,
                        SUM(v.qty) AS Stock,
                        i.ir_batch_status AS BatchStatus,
                        v.uniquecode,
                        v.int_barcode
                    FROM View_Stock v
                    INNER JOIN LatestStock ls
                        ON v.ir_id=ls.ir_id AND ls.rn=1
                    INNER JOIN inv_item_reg i
                        ON v.ir_id=i.ir_id
                    WHERE i.ir_active=1
                    AND v.location_id='" + usqlre.locationId + @"'
                    GROUP BY
                        i.ir_name,
                        i.ir_hsn_code,
                        i.ir_rawmaterial,
                        i.ir_lend_validation,
                        v.ir_id,
                        ls.Cost,
                        ls.mrp,
                        ls.retail,
                        ls.wholesale,
                        ls.spretail,
                        ls.branch,
                        i.ir_batch_status,
                        v.uniquecode,
                        v.int_barcode";
                        }
                    }
                    else
                    {
                        sql = @"WITH LatestStock AS
                (
                    SELECT ir_id,Cost,mrp,retail,wholesale,spretail,branch,
                    ROW_NUMBER() OVER(PARTITION BY ir_id ORDER BY uniquecode DESC) rn
                    FROM View_Stock
                )
                SELECT
                    i.ir_name AS label,
                    v.ir_id AS value,
                    i.ir_rawmaterial,
                    i.ir_lend_validation AS isLend,
                    ls.Cost AS PurchaseRate,
                    ls.mrp AS Mrp,
                    ls.retail AS Retail,
                    ls.wholesale AS Wholesale,
                    ls.spretail AS SpRetail,
                    ls.branch AS Branch,
                    SUM(v.qty) AS Stock,
                    i.ir_batch_status AS BatchStatus,
                    v.uniquecode,
                    v.int_barcode
                FROM View_Stock v
                INNER JOIN LatestStock ls
                    ON v.ir_id=ls.ir_id AND ls.rn=1
                INNER JOIN inv_item_reg i
                    ON v.ir_id=i.ir_id
                WHERE i.ir_active=1
                AND v.location_id='" + usqlre.locationId + @"'
                GROUP BY
                    i.ir_name,
                    i.ir_rawmaterial,
                    i.ir_lend_validation,
                    v.ir_id,
                    ls.Cost,
                    ls.mrp,
                    ls.retail,
                    ls.wholesale,
                    ls.spretail,
                    ls.branch,
                    i.ir_batch_status,
                    v.uniquecode,
                    v.int_barcode";
                    }
                }
                else
                {
                    // Use the same SQL logic from your existing
                    // GetItemNameWithBarcode() method here,
                    // only removing the location filter exactly
                    // as your current endpoint does.
                }

                DataTable dt = usqlre.dbReaderFill(sql);

                usqlre.close();

                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("supplier-list")]
        public IActionResult SupplierList()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string query = @"
            SELECT DISTINCT
                a.as_id,
                a.as_name
            FROM acc_subhead a
            INNER JOIN inv_purchase_inf p
                ON a.as_id = p.pi_sup_id
            ORDER BY a.as_name";

                DataTable dt = usqlre.dbReaderFill(query);

                usqlre.close();

                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("stock-item-group-list")]
        public IActionResult ItemGroupList()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

                string sql1 = @"
        SELECT
            g1_id,
            g1_name
        FROM inv_group1
        ORDER BY g1_id";

                DataTable group1 = usqlre.dbReaderFill(sql1);
                hash.Add("group1", group1);

                string sql2 = @"
        SELECT
            g2_id,
            g2_name
        FROM inv_group2
        ORDER BY g2_id";

                DataTable group2 = usqlre.dbReaderFill(sql2);
                hash.Add("group2", group2);

                string sql3 = @"
        SELECT
            g3_id,
            g3_name
        FROM inv_group3
        ORDER BY g3_id";

                DataTable group3 = usqlre.dbReaderFill(sql3);
                hash.Add("group3", group3);

                usqlre.close();

                string jsonResult = ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
            }
            catch (Exception ex)
            {
                if (usqlre != null)
                    usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("stock-report")]
        public IActionResult StockReport([FromBody] StockReportRequest model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string where = " WHERE 1=1 ";

                if (model.LocationId > 0)
                    where += " AND location_id=" + model.LocationId;

                if (model.ItemId > 0)
                    where += " AND View_Stock.ir_id=" + model.ItemId;

                if (model.SupplierId > 0)
                    where += " AND Sup=" + model.SupplierId;

                if (model.CategoryId > 0)
                    where += " AND inv_item_reg.ir_category_id=" + model.CategoryId;

                if (model.SubCategoryId > 0)
                    where += " AND inv_item_reg.ir_sub_category_id=" + model.SubCategoryId;

                if (model.Group1Id > 0)
                    where += " AND inv_item_reg.ir_group1=" + model.Group1Id;

                if (model.Group2Id > 0)
                    where += " AND inv_item_reg.ir_group2=" + model.Group2Id;

                if (model.Group3Id > 0)
                    where += " AND inv_item_reg.ir_group3=" + model.Group3Id;

                if (model.BrandId > 0)
                {
                    string sqlBrand = "SELECT bra_name FROM inv_brand WHERE bra_id=" + model.BrandId;

                    DataTable dtBrand = usqlre.dbReaderFill(sqlBrand);

                    if (dtBrand != null && dtBrand.Rows.Count > 0)
                    {
                        string brandName = dtBrand.Rows[0]["bra_name"].ToString().Replace("'", "''");

                        where += " AND inv_item_reg.ir_brand='" + brandName + "'";
                    }
                }

                if (!string.IsNullOrWhiteSpace(model.Barcode))
                    where += " AND uniquecode='" + model.Barcode + "'";

                if (!string.IsNullOrWhiteSpace(model.IntBarcode))
                    where += " AND int_barcode='" + model.IntBarcode + "'";

                if (model.RawMaterials)
                    where += " AND inv_item_reg.ir_rawmaterial=1";

                if (model.FinishedGoods)
                    where += " AND inv_item_reg.ir_rawmaterial=0";

                if (model.TaxItems)
                    where += " AND inv_item_reg.ir_taxper>0";

                if (model.NonTaxItems)
                    where += " AND inv_item_reg.ir_taxper=0";

                if (!model.ShowAll)
                    where += " AND qty>0";

                string sql = @"
                SELECT
                View_Stock.ir_id,
                inv_item_reg.ir_code,
                inv_item_reg.ir_name,
                qty,
                prate,
                retail,
                wholesale,
                mrp,
                Cost,
                location_id,
                as_name AS supplier,
                uniquecode,
                int_barcode

                FROM View_Stock

                INNER JOIN inv_item_reg
                ON View_Stock.ir_id=inv_item_reg.ir_id

                LEFT JOIN acc_subhead
                ON acc_subhead.as_id=View_Stock.Sup

                " + where + @"

                ORDER BY inv_item_reg.ir_name";

                DataTable dt = usqlre.dbReaderFill(sql);

                usqlre.close();

                return Content(
                    ReportModelContext.searializeDt(dt),
                    "application/json");
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("stock-subcategories")]
        public IActionResult GetSubCategories()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string sql = @"
        SELECT
            sc_id,
            sc_name
        FROM inv_subcategory
        ORDER BY sc_name";

                DataTable dt = usqlre.dbReaderFill(sql);

                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
            finally
            {
                usqlre.close();
            }
        }

        [HttpGet("stock-manufacturers")]
        public IActionResult GetManufacturers()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            string sql = @"
    SELECT
        m_id,
        m_name
    FROM inv_mfr
    ORDER BY m_name";

            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();

            return Ok(ReportModelContext.searializeDt(dt));
        }

        [HttpPost("sales-report")]
        public IActionResult SalesReport([FromBody] SalesReportRequest model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string where = " WHERE 1=1 ";



                if (!string.IsNullOrWhiteSpace(model.FromDate))
                {
                    where += " AND CAST(si.si_date AS DATE) >= '" +
                             model.FromDate.Replace("'", "''") + "'";
                }

                if (!string.IsNullOrWhiteSpace(model.ToDate))
                {
                    where += " AND CAST(si.si_date AS DATE) <= '" +
                             model.ToDate.Replace("'", "''") + "'";
                }




                if (model.FormId > 0)
                {
                    where += " AND si.si_str_id = " + model.FormId;
                }
                else if (!model.IncludeReturn)
                {


                    where += " AND si.si_str_id IN (1,2,4,6,8)";
                }



                if (model.LocationId > 0)
                {
                    where += " AND si.si_location_id = " + model.LocationId;
                }



                if (model.ItemId > 0)
                {
                    where += " AND sp.sp_ir_id = " + model.ItemId;
                }



                if (model.CustomerId > 0)
                {
                    where += " AND si.si_acc_id = " + model.CustomerId;
                }



                if (model.SalesmanId > 0)
                {
                    where += " AND si.si_commision_acc_id = " + model.SalesmanId;
                }




                if (model.UserId > 0)
                {
                    where += " AND si.si_user_id = " + model.UserId;
                }




                if (model.CategoryId > 0)
                {
                    where += " AND ir.ir_category_id = " + model.CategoryId;
                }




                if (model.SubCategoryId > 0)
                {
                    where += " AND ir.ir_sub_category_id = " +
                             model.SubCategoryId;
                }



                if (!string.IsNullOrWhiteSpace(model.Barcode))
                {
                    string barcode = model.Barcode.Replace("'", "''");

                    where += @"
                AND (
                    sp.sp_uniquecode = '" + barcode + @"'
                    OR EXISTS
                    (
                        SELECT 1
                        FROM inv_barcode bx
                        WHERE bx.b_ir_id = sp.sp_ir_id
                        AND bx.b_uniquecode = '" + barcode + @"'
                    )
                )";
                }



                if (!string.IsNullOrWhiteSpace(model.IntBarcode))
                {
                    string intBarcode = model.IntBarcode.Replace("'", "''");

                    where += @"
                AND (
                    ir.ir_int_barcode = '" + intBarcode + @"'
                    OR EXISTS
                    (
                        SELECT 1
                        FROM inv_barcode bx
                        WHERE bx.b_ir_id = sp.sp_ir_id
                        AND bx.b_int_barcode = '" + intBarcode + @"'
                    )
                )";
                }




                if (model.SupplierId > 0)
                {
                    where += @"
                AND EXISTS
                (
                    SELECT 1
                    FROM inv_barcode sb
                    WHERE sb.b_ir_id = sp.sp_ir_id
                    AND sb.b_as_id = " + model.SupplierId + @"
                )";
                }




                if (model.TaxItems)
                {
                    where += " AND ISNULL(sp.sp_taxper, ir.ir_taxper) > 0";
                }

                if (model.NonTaxItems)
                {
                    where += " AND ISNULL(sp.sp_taxper, ir.ir_taxper) = 0";
                }




                if (model.GstReport)
                {
                    where += @"
                AND (
                    ISNULL(sp.sp_cgst, 0) > 0
                    OR ISNULL(sp.sp_sgst, 0) > 0
                    OR ISNULL(sp.sp_igst, 0) > 0
                )";
                }




                if (model.Summary && !model.Detailed)
                {
                    string summarySql = @"
                SELECT
                    si.si_entryno AS EntryNo,
                    CAST(si.si_date AS DATE) AS SalesDate,

                    MAX(st.str_name) AS Form,
                    MAX(si.si_cust_name) AS Customer,

                    ir.ir_name AS ItemName,

                    SUM(sp.sp_qty) AS Qty,

                    SUM(ISNULL(sp.sp_rate * sp.sp_qty, 0)) AS Gross,

                    SUM(ISNULL(sp.sp_disc, 0)) AS Discount,

                    SUM(ISNULL(sp.sp_net_amount, 0)) AS Net,

                    SUM(ISNULL(sp.sp_profit, 0)) AS Profit

                FROM inv_sales_inf si

                INNER JOIN inv_sales_par sp
                    ON si.si_str_id = sp.sp_str_id
                    AND si.si_entryno = sp.sp_entryno

                INNER JOIN inv_item_reg ir
                    ON sp.sp_ir_id = ir.ir_id

                LEFT JOIN inv_sales_type_reg st
                    ON si.si_str_id = st.str_id

                LEFT JOIN acc_subhead customer
                    ON si.si_acc_id = customer.as_id

                " + where + @"

                GROUP BY
                    si.si_entryno,
                    CAST(si.si_date AS DATE),
                    ir.ir_name

                ORDER BY
                    si.si_entryno,
                    ir.ir_name";

                    DataTable dtSummary = usqlre.dbReaderFill(summarySql);

                    usqlre.close();

                    return Content(
                        ReportModelContext.searializeDt(dtSummary),
                        "application/json");
                }




                string sql = @"
            SELECT

                si.si_entryno AS EntryNo,

                CAST(si.si_date AS DATE) AS SalesDate,

                st.str_name AS Form,

                si.si_cust_name AS Customer,

                ir.ir_name AS ItemName,

                sp.sp_qty AS Qty,

                sp.sp_rate AS SRate,

                sp.sp_gross_value AS Gross,

                ISNULL(sp.sp_disc, 0) AS Discount,

                sp.sp_net_amount AS Net,

                ISNULL(sp.sp_profit, 0) AS Profit,

                ISNULL(sp.sp_taxper, ir.ir_taxper) AS TaxPer,

                ISNULL(sp.sp_cgst, 0) AS CGST,

                ISNULL(sp.sp_sgst, 0) AS SGST,

                ISNULL(sp.sp_igst, 0) AS IGST,

                ISNULL(sp.sp_cess, 0) AS Cess,

                ISNULL(sp.sp_kfc, 0) AS KFC,

                sp.sp_uniquecode AS Barcode,

                ir.ir_int_barcode AS IntBarcode,

                ir.ir_code AS ItemCode,

                sp.sp_hsn_code AS HSNCode

            FROM inv_sales_inf si

            INNER JOIN inv_sales_par sp
                ON si.si_str_id = sp.sp_str_id
                AND si.si_entryno = sp.sp_entryno

            INNER JOIN inv_item_reg ir
                ON sp.sp_ir_id = ir.ir_id

            LEFT JOIN inv_sales_type_reg st
                ON si.si_str_id = st.str_id

            LEFT JOIN acc_subhead customer
                ON si.si_acc_id = customer.as_id

            " + where + @"

            ORDER BY
                si.si_entryno,
                ir.ir_name";


                DataTable dt = usqlre.dbReaderFill(sql);

                usqlre.close();

                return Content(
                    ReportModelContext.searializeDt(dt),
                    "application/json");
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("sales-all-customers")]
        public async Task<IActionResult> GetAllCustomers()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string sql = @"
            SELECT
                as_id AS CustomerId,
                as_name AS CustomerName
            FROM acc_subhead
            WHERE as_ap_id = 4
              AND as_name IS NOT NULL
              AND LTRIM(RTRIM(as_name)) <> ''
            ORDER BY as_name";

                DataTable dt = usqlre.dbReaderFill(sql);

                usqlre.close();

                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("sales-all-salesman")]
        public async Task<IActionResult> GetAllSalesman()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string sql = @"
            SELECT
                as_id AS SalesmanId,
                as_name AS SalesmanName
            FROM acc_subhead
            WHERE as_ap_id = 14
              AND as_name IS NOT NULL
              AND LTRIM(RTRIM(as_name)) <> ''
            ORDER BY as_name";

                DataTable dt = usqlre.dbReaderFill(sql);

                usqlre.close();

                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("sales-all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string sql = @"
            SELECT
                gu.gu_user_id AS value,
                gu.gu_name AS label,
                ISNULL(ur.ur_name, '') AS role
            FROM gnl_users gu
            LEFT JOIN gnl_user_roles ur
                ON gu.gu_ur_id = ur.ur_id
            WHERE gu.gu_active = 1
            ORDER BY gu.gu_name";

                DataTable dt = usqlre.dbReaderFill(sql);

                usqlre.close();

                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("sales-form-type")]
        public IActionResult GetSalesType()
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                string sql = @"
            SELECT
                str_name AS label,
                CAST(str_id AS INT) AS value
            FROM inv_sales_type_reg
            ORDER BY str_name";

                DataTable dt = usqlre.dbReaderFill(sql);

                usqlre.close();

                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("get-gst-types")]
        public IActionResult GetGstTypes()
        {
            try
            {
                var gstTypes = new[]
                {
            new
            {
                id = 1,
                name = "All"
            },
            new
            {
                id = 2,
                name = "Included"
            },
            new
            {
                id = 3,
                name = "Excluded"
            }
        };

                return Ok(new
                {
                    status = true,
                    statusCode = 200,
                    data = gstTypes
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
        }

        [HttpPost("purchase-report")]
        public IActionResult PurchaseReport([FromBody] PurchaseReportSales model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                var hash = new Dictionary<string, DataTable>();


                string dateColumn = "pi_date";

                if (model.DateType == 2)
                {
                    dateColumn = "pi_inv_date";
                }

                string headerWhere = " WHERE 1 = 1 ";
                string detailWhere = " WHERE 1 = 1 ";


                if (!string.IsNullOrWhiteSpace(model.FromDate))
                {
                    if (!DateTime.TryParse(model.FromDate, out DateTime fromDate))
                    {
                        return Ok(new
                        {
                            status = false,
                            message = "Invalid FromDate"
                        });
                    }

                    string fromDateValue = fromDate.ToString("yyyy-MM-dd");

                    headerWhere +=
                        " AND CAST(" + dateColumn + " AS DATE) >= '" +
                        fromDateValue + "'";

                    detailWhere +=
                        " AND CAST(" + dateColumn + " AS DATE) >= '" +
                        fromDateValue + "'";
                }


                if (!string.IsNullOrWhiteSpace(model.ToDate))
                {
                    if (!DateTime.TryParse(model.ToDate, out DateTime toDate))
                    {
                        return Ok(new
                        {
                            status = false,
                            message = "Invalid ToDate"
                        });
                    }

                    string toDateValue = toDate.ToString("yyyy-MM-dd");

                    headerWhere +=
                        " AND CAST(" + dateColumn + " AS DATE) <= '" +
                        toDateValue + "'";

                    detailWhere +=
                        " AND CAST(" + dateColumn + " AS DATE) <= '" +
                        toDateValue + "'";
                }

                if (model.LocationId > 0)
                {
                    headerWhere +=
                        " AND pi_location_id = " + model.LocationId;

                    detailWhere +=
                        " AND pi_location_id = " + model.LocationId;
                }

                if (model.SupplierId > 0)
                {
                    headerWhere +=
                        " AND pi_sup_id = " + model.SupplierId;

                    detailWhere +=
                        " AND pi_sup_id = " + model.SupplierId;
                }


                if (model.SalesmanId > 0)
                {
                    headerWhere +=
                        " AND pi_salesman_id = " + model.SalesmanId;

                    detailWhere +=
                        " AND pi_salesman_id = " + model.SalesmanId;
                }


                if (model.UserId > 0)
                {
                    headerWhere +=
                        " AND pi_user_id = " + model.UserId;

                    detailWhere +=
                        " AND pi_user_id = " + model.UserId;
                }

                if (model.ItemId > 0)
                {
                    detailWhere +=
                        " AND pp_ir_id = " + model.ItemId;
                }


                if (model.CategoryId > 0)
                {
                    detailWhere +=
                        " AND ir_category_id = " + model.CategoryId;
                }

                if (model.SubCategoryId > 0)
                {
                    detailWhere +=
                        " AND ir_sub_category_id = " + model.SubCategoryId;
                }


                if (!string.IsNullOrWhiteSpace(model.Barcode))
                {
                    string barcode = model.Barcode.Replace("'", "''");

                    detailWhere +=
                        " AND pp_uniquecode = '" + barcode + "'";
                }


                if (!string.IsNullOrWhiteSpace(model.IntBarcode))
                {
                    string intBarcode = model.IntBarcode.Replace("'", "''");

                    detailWhere +=
                        " AND pp_int_barcode = '" + intBarcode + "'";
                }


                if (model.GstType == 2)
                {
                    // GST Included / Taxable items
                    detailWhere += " AND ISNULL(ir_taxper, 0) > 0";
                }
                else if (model.GstType == 3)
                {
                    // GST Exempt / Non-taxable items
                    detailWhere += " AND ISNULL(ir_taxper, 0) = 0";
                }

                if (model.Summary)
                {
                    string summarySql = @"
                SELECT

                    pp.pp_entryno AS EntryNo,

                    pi.pi_inv_date AS InvoiceDate,

                    sup.as_name AS Supplier,

                    ir.ir_name AS ItemName,

                    SUM(ISNULL(pp.pp_qty, 0)) AS Qty,

                    pp.pp_prate AS PRate,

                    SUM(ISNULL(pp.pp_gross_value, 0)) AS Gross,

                    SUM(ISNULL(pp.pp_disc, 0)) AS Disc,

                    SUM(ISNULL(pp.pp_net_amount, 0)) AS Net,

                    SUM(ISNULL(pp.pp_tax, 0)) AS Gst

                FROM inv_parchase_par pp

                INNER JOIN inv_item_reg ir
                    ON pp.pp_ir_id = ir.ir_id

                INNER JOIN inv_purchase_inf pi
                    ON pi.pi_entryno = pp.pp_entryno

                INNER JOIN acc_subhead sup
                    ON pi.pi_sup_id = sup.as_id

                " + detailWhere + @"

                GROUP BY

                    pp.pp_entryno,

                    pi.pi_inv_date,

                    sup.as_name,

                    ir.ir_name,

                    pp.pp_prate

                ORDER BY

                    pp.pp_entryno,
                    ir.ir_name";

                    DataTable summary =
                        usqlre.dbReaderFill(summarySql);

                    hash.Add("Customsummery", summary);
                }

                if (model.Detailed)
                {
                    string detailSql = @"
SELECT

    pp_entryno AS EntryNo,

    pi_inv_date AS InvoiceDate,

    sup.as_name AS Supplier,

    pi_sup_invno AS SupplierInvoiceNo,

    ir_name AS ItemName,

    pp_qty AS Qty,

    pp_prate AS PRate,

    pp_gross_value AS Gross,

    pp_disc AS Disc,

    pp_net_amount AS Net,

    pp_tax AS Gst,

    pp_taxper AS TaxPercentage,

    pp_sgst AS SGST,

    pp_cgst AS CGST,

    pp_igst AS IGST,

    pp_cess AS Cess,

    pp_ad_cess AS AdCess,

    pp_total AS Total,

    pp_mrp AS Mrp,

    pp_retail AS Retail,

    pp_wholesale AS Wholesale,

    pp_spretail AS Spretail,

    pp_branch AS Branch,

    pp_realprate AS RPrate,

    pp_uniquecode AS Barcode,

    pp_int_barcode AS IntBarcode,

    pp_hsn AS HSN,

    u_name AS Unit,

    gl_name AS Location

FROM inv_parchase_par

INNER JOIN inv_item_reg
    ON pp_ir_id = ir_id

INNER JOIN inv_purchase_inf
    ON pi_entryno = pp_entryno

INNER JOIN acc_subhead sup
    ON pi_sup_id = sup.as_id

LEFT JOIN inv_unit
    ON u_id = ir_min_unit_id

LEFT JOIN gnl_location
    ON gl_id = pi_location_id

" + detailWhere + @"

ORDER BY
    pp_entryno,
    pp_row_id";

                    DataTable details =
                        usqlre.dbReaderFill(detailSql);

                    hash.Add("Customdetails", details);
                }

                usqlre.close();

                return Content(
                    ReportModelContext.searializeDt(hash),
                    "application/json"
                );
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("purchase-return-report")]
        public IActionResult PurchaseReturnReport(
     [FromBody] PurchaseReturnReportRequest model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                var hash = new Dictionary<string, DataTable>();


                string dateColumn = "pr.pr_date";

                if (model.DateType == 2)
                {
                    dateColumn = "pr.pr_inv_date";
                }

                string headerWhere = " WHERE 1 = 1 ";
                string detailWhere = " WHERE 1 = 1 ";



                if (!string.IsNullOrWhiteSpace(model.FromDate))
                {
                    if (!DateTime.TryParse(model.FromDate, out DateTime fromDate))
                    {
                        return Ok(new
                        {
                            status = false,
                            message = "Invalid FromDate"
                        });
                    }

                    string fromDateValue = fromDate.ToString("yyyy-MM-dd");

                    headerWhere +=
                        " AND CAST(" + dateColumn + " AS DATE) >= '" +
                        fromDateValue + "'";

                    detailWhere +=
                        " AND CAST(" + dateColumn + " AS DATE) >= '" +
                        fromDateValue + "'";
                }


                if (!string.IsNullOrWhiteSpace(model.ToDate))
                {
                    if (!DateTime.TryParse(model.ToDate, out DateTime toDate))
                    {
                        return Ok(new
                        {
                            status = false,
                            message = "Invalid ToDate"
                        });
                    }

                    string toDateValue = toDate.ToString("yyyy-MM-dd");

                    headerWhere +=
                        " AND CAST(" + dateColumn + " AS DATE) <= '" +
                        toDateValue + "'";

                    detailWhere +=
                        " AND CAST(" + dateColumn + " AS DATE) <= '" +
                        toDateValue + "'";
                }




                if (model.LocationId > 0)
                {
                    headerWhere +=
                        " AND pr.pr_location_id = " +
                        model.LocationId;

                    detailWhere +=
                        " AND pr.pr_location_id = " +
                        model.LocationId;
                }




                if (model.SupplierId > 0)
                {
                    headerWhere +=
                        " AND pr.pr_sup_id = " +
                        model.SupplierId;

                    detailWhere +=
                        " AND pr.pr_sup_id = " +
                        model.SupplierId;
                }



                if (model.SalesmanId > 0)
                {
                    headerWhere +=
                        " AND pr.pr_salesman_id = " +
                        model.SalesmanId;

                    detailWhere +=
                        " AND pr.pr_salesman_id = " +
                        model.SalesmanId;
                }


                if (model.UserId > 0)
                {
                    headerWhere +=
                        " AND pr.pr_user_id = " +
                        model.UserId;

                    detailWhere +=
                        " AND pr.pr_user_id = " +
                        model.UserId;
                }




                if (model.ItemId > 0)
                {
                    detailWhere +=
                        " AND prp.prp_ir_id = " +
                        model.ItemId;

                    headerWhere += @"
                AND EXISTS
                (
                    SELECT 1
                    FROM inv_parchase_rt_par prpx
                    WHERE prpx.prp_entryno = pr.pr_entryno
                    AND prpx.prp_ir_id = " + model.ItemId + @"
                )";
                }




                if (model.CategoryId > 0)
                {
                    detailWhere +=
                        " AND ir.ir_category_id = " +
                        model.CategoryId;

                    headerWhere += @"
                AND EXISTS
                (
                    SELECT 1
                    FROM inv_parchase_rt_par prpx
                    INNER JOIN inv_item_reg irx
                        ON prpx.prp_ir_id = irx.ir_id
                    WHERE prpx.prp_entryno = pr.pr_entryno
                    AND irx.ir_category_id = " +
                            model.CategoryId + @"
                )";
                }



                if (model.SubCategoryId > 0)
                {
                    detailWhere +=
                        " AND ir.ir_sub_category_id = " +
                        model.SubCategoryId;

                    headerWhere += @"
                AND EXISTS
                (
                    SELECT 1
                    FROM inv_parchase_rt_par prpx
                    INNER JOIN inv_item_reg irx
                        ON prpx.prp_ir_id = irx.ir_id
                    WHERE prpx.prp_entryno = pr.pr_entryno
                    AND irx.ir_sub_category_id = " +
                            model.SubCategoryId + @"
                )";
                }




                if (!string.IsNullOrWhiteSpace(model.Barcode))
                {
                    string barcode =
                        model.Barcode.Replace("'", "''");

                    detailWhere +=
                        " AND prp.prp_uniquecode = '" +
                        barcode + "'";

                    headerWhere += @"
                AND EXISTS
                (
                    SELECT 1
                    FROM inv_parchase_rt_par prpx
                    WHERE prpx.prp_entryno = pr.pr_entryno
                    AND prpx.prp_uniquecode = '" +
                        barcode + @"'
                )";
                }



                if (!string.IsNullOrWhiteSpace(model.IntBarcode))
                {
                    string intBarcode =
                        model.IntBarcode.Replace("'", "''");

                    detailWhere +=
                        " AND prp.prp_int_barcode = '" +
                        intBarcode + "'";

                    headerWhere += @"
                AND EXISTS
                (
                    SELECT 1
                    FROM inv_parchase_rt_par prpx
                    WHERE prpx.prp_entryno = pr.pr_entryno
                    AND prpx.prp_int_barcode = '" +
                        intBarcode + @"'
                )";
                }



                if (!string.IsNullOrWhiteSpace(model.GstType))
                {
                    string gstType = model.GstType.Trim();

                    if (gstType.Equals(
                        "Included",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        detailWhere +=
                            " AND ISNULL(prp.prp_taxper, 0) > 0";

                        headerWhere += @"
                    AND EXISTS
                    (
                        SELECT 1
                        FROM inv_parchase_rt_par prpx
                        WHERE prpx.prp_entryno = pr.pr_entryno
                        AND ISNULL(prpx.prp_taxper, 0) > 0
                    )";
                    }
                    else if (gstType.Equals(
                        "Excluded",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        detailWhere +=
                            " AND ISNULL(prp.prp_taxper, 0) = 0";

                        headerWhere += @"
                    AND EXISTS
                    (
                        SELECT 1
                        FROM inv_parchase_rt_par prpx
                        WHERE prpx.prp_entryno = pr.pr_entryno
                        AND ISNULL(prpx.prp_taxper, 0) = 0
                    )";
                    }
                }


                if (model.Summary)
                {
                    string summarySql = @"
                SELECT

                    prp.prp_entryno AS EntryNo,

                    pr.pr_inv_date AS InvoiceDate,

                    sup.as_name AS Supplier,

                    ir.ir_name AS ItemName,

                    SUM(ISNULL(prp.prp_qty, 0)) AS Qty,

                    prp.prp_prate AS PRate,

                    SUM(ISNULL(prp.prp_gross_value, 0)) AS Gross,

                    SUM(ISNULL(prp.prp_disc, 0)) AS Disc,

                    SUM(ISNULL(prp.prp_net_amount, 0)) AS Net,

                    SUM(ISNULL(prp.prp_tax, 0)) AS Gst

                FROM inv_parchase_rt_par prp

                INNER JOIN inv_item_reg ir
                    ON prp.prp_ir_id = ir.ir_id

                INNER JOIN inv_purchase_rt_inf pr
                    ON pr.pr_entryno = prp.prp_entryno

                INNER JOIN acc_subhead sup
                    ON pr.pr_sup_id = sup.as_id

                " + detailWhere + @"

                GROUP BY

                    prp.prp_entryno,

                    pr.pr_inv_date,

                    sup.as_name,

                    ir.ir_name,

                    prp.prp_prate

                ORDER BY

                    prp.prp_entryno,
                    ir.ir_name";

                    DataTable summary =
                        usqlre.dbReaderFill(summarySql);

                    hash.Add(
                        "Customsummery",
                        summary);
                }



                if (model.Detailed)
                {
                    string detailSql = @"
                SELECT

                    prp.prp_entryno AS EntryNo,

                    pr.pr_date AS PDate,

                    pr.pr_inv_date AS InvoiceDate,

                    sup.as_name AS Supplier,

                    pr.pr_sup_invno AS SupplierInvoiceNo,

                    ir.ir_name AS ItemName,

                    c.c_name AS Category,

                    sc.sc_name AS SubCategory,

                    prp.prp_prate AS PRate,

                    prp.prp_qty AS Qty,

                    prp.prp_gross_value AS Gross,

                    prp.prp_disc_per AS DiscPercentage,

                    prp.prp_disc AS Disc,

                    prp.prp_net_amount AS Net,

                    prp.prp_tax AS Gst,

                    prp.prp_sgst AS SGST,

                    prp.prp_cgst AS CGST,

                    prp.prp_igst AS IGST,

                    prp.prp_sgstp AS SGSTPercentage,

                    prp.prp_cgstp AS CGSTPercentage,

                    prp.prp_igstp AS IGSTPercentage,

                    prp.prp_taxper AS TaxPercentage,

                    prp.prp_total AS Total,

                    prp.prp_mrp_per AS MRPPercentage,

                    prp.prp_mrp AS Mrp,

                    prp.prp_retail_per AS RetailPercentage,

                    prp.prp_retail AS Retail,

                    prp.prp_wholesale_per AS WholesalePercentage,

                    prp.prp_wholesale AS WSale,

                    prp.prp_spretail_per AS SpretailPercentage,

                    prp.prp_spretail AS Spretail,

                    prp.prp_branch_per AS BranchPercentage,

                    prp.prp_branch AS Branch,

                    prp.prp_realprate AS RPrate,

                    prp.prp_exp_date AS [Exp],

                    prp.prp_color AS Color,

                    prp.prp_size AS Size,

                    prp.prp_brand AS Brand,

                    prp.prp_int_barcode AS IntBarcode,

                    prp.prp_uniquecode AS Barcode,

                    prp.prp_qty_multi_unit AS QtyMultiUnit,

                    prp.prp_unit_multi AS UnitMulti,

                    prp.prp_prate_multi_unit AS PRateMultiUnit,

                    prp.prp_narration AS Narration,

                    prp.prp_item_narration AS ItemNarration,

                    prp.prp_hsn_code AS HSN,

                    m.m_name AS Manufacturer,

                    u.u_name AS Unit,

                    gu.gu_name AS [User],

                    gl.gl_name AS Location,

                    area.area_name AS Area,

                    g1.g1_name AS Group1,

                    g2.g2_name AS Group2,

                    pr.pr_location_id AS LocationId,

                    pr.pr_salesman_id AS SalesmanId,

                    pr.pr_user_id AS UserId,

                    pr.pr_salesman AS Salesman

                FROM inv_parchase_rt_par prp

                INNER JOIN inv_item_reg ir
                    ON prp.prp_ir_id = ir.ir_id

                INNER JOIN inv_purchase_rt_inf pr
                    ON pr.pr_entryno = prp.prp_entryno

                INNER JOIN acc_subhead sup
                    ON pr.pr_sup_id = sup.as_id

                LEFT JOIN inv_mfr m
                    ON m.m_id = ir.ir_mfr_id

                LEFT JOIN inv_category c
                    ON c.c_id = ir.ir_category_id

                LEFT JOIN inv_unit u
                    ON u.u_id = ir.ir_min_unit_id

                LEFT JOIN inv_subcategory sc
                    ON sc.sc_id = ir.ir_sub_category_id

                LEFT JOIN gnl_users gu
                    ON gu.gu_user_id = pr.pr_user_id

                LEFT JOIN gnl_location gl
                    ON gl.gl_id = pr.pr_location_id

                LEFT JOIN inv_group1 g1
                    ON g1.g1_id = ir.ir_group1

                LEFT JOIN inv_group2 g2
                    ON g2.g2_id = ir.ir_group2

                LEFT JOIN acc_area area
                    ON area.area_id = sup.as_area_id

                " + detailWhere + @"

                ORDER BY
                    prp.prp_entryno,
                    prp.prp_ir_id";

                    DataTable details =
                        usqlre.dbReaderFill(detailSql);

                    hash.Add(
                        "Customdetails",
                        details);
                }


                usqlre.close();

                return Content(
                    ReportModelContext.searializeDt(hash),
                    "application/json"
                );
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("stock-ledger-report")]
        public IActionResult StockLedgerReport([FromBody] StockLedgerReport model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                var hash = new Dictionary<string, DataTable>();

                if (string.IsNullOrWhiteSpace(model.FromDate) ||
                    !DateTime.TryParse(model.FromDate, out DateTime fromDate))
                {
                    return Ok(new
                    {
                        status = false,
                        message = "Invalid FromDate"
                    });
                }

                if (string.IsNullOrWhiteSpace(model.ToDate) ||
                    !DateTime.TryParse(model.ToDate, out DateTime toDate))
                {
                    return Ok(new
                    {
                        status = false,
                        message = "Invalid ToDate"
                    });
                }

                if (fromDate.Date > toDate.Date)
                {
                    return Ok(new
                    {
                        status = false,
                        message = "FromDate cannot be greater than ToDate"
                    });
                }

                string fromDateValue = fromDate.ToString("yyyy-MM-dd");
                string toDateValue = toDate.ToString("yyyy-MM-dd");


                string itemWhere = " WHERE 1 = 1 ";

                if (model.ItemId > 0)
                {
                    itemWhere +=
                        " AND ir.ir_id = " + model.ItemId;
                }

                if (model.CategoryId > 0)
                {
                    itemWhere +=
                        " AND ir.ir_category_id = " + model.CategoryId;
                }

                if (model.SubCategoryId > 0)
                {
                    itemWhere +=
                        " AND ir.ir_sub_category_id = " + model.SubCategoryId;
                }

                if (model.Group1Id > 0)
                {
                    itemWhere +=
                        " AND ir.ir_group1 = " + model.Group1Id;
                }

                if (model.Group2Id > 0)
                {
                    itemWhere +=
                        " AND ir.ir_group2 = " + model.Group2Id;
                }

                if (model.Group3Id > 0)
                {
                    itemWhere +=
                        " AND ir.ir_group3 = " + model.Group3Id;
                }

                if (model.SupplierId > 0)
                {
                    itemWhere += @"
                AND EXISTS
                (
                    SELECT 1
                    FROM inv_barcode b
                    WHERE b.b_ir_id = ir.ir_id
                    AND b.b_as_id = " + model.SupplierId + @"
                )";
                }

                string locationWhere = "";

                if (model.LocationId > 0)
                {
                    locationWhere =
                        " AND isl.isl_location_id = " + model.LocationId;
                }


                string sql = @"

SELECT

    ir.ir_id AS ItemId,

    ir.ir_code AS ItemCode,

    ir.ir_name AS ItemName,


    /*
     * OPENING
     * isl_date < from date OR OS
     */
    ISNULL(
        SUM(
            CASE
                WHEN CAST(isl.isl_date AS DATE) < '" + fromDateValue + @"'
                     OR isl.isl_form = 'OS'
                THEN
                    ISNULL(isl.isl_in, 0)
                    -
                    ISNULL(isl.isl_out, 0)
                ELSE 0
            END
        ),
        0
    ) AS Opening,


    /*
     * PURCHASE
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'PURCHASE'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_in, 0)
                ELSE 0
            END
        ),
        0
    ) AS Purchase,


    /*
     * PURCHASE RETURN
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'PR'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_out, 0)
                ELSE 0
            END
        ),
        0
    ) AS PReturn,


    /*
     * PRODUCTION IN
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'PRODUCTION'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_in, 0)
                ELSE 0
            END
        ),
        0
    ) AS PPlus,


    /*
     * PRODUCTION OUT
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'PRODUCTION'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_out, 0)
                ELSE 0
            END
        ),
        0
    ) AS PMinus,


    /*
     * SALES
     *
     * Original SP:
     * 1, 2, 6, 4, 8
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form IN ('1', '2', '6', '4', '8')
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_out, 0)
                ELSE 0
            END
        ),
        0
    ) AS Sales,


    /*
     * SALES RETURN
     *
     * Original SP:
     * 7, 9, 10
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form IN ('7', '9', '10')
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_in, 0)
                ELSE 0
            END
        ),
        0
    ) AS SReturn,


    /*
     * DAMAGE
     *
     * IMPORTANT:
     * Original SP uses isl_form = 'DAMAGE'
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'DAMAGE'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_out, 0)
                ELSE 0
            END
        ),
        0
    ) AS Damage,


    /*
     * STOCK ADJUSTMENT IN
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'STOCK ADJUSTMENT'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_in, 0)
                ELSE 0
            END
        ),
        0
    ) AS SAPlus,


    /*
     * STOCK ADJUSTMENT OUT
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'STOCK ADJUSTMENT'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_out, 0)
                ELSE 0
            END
        ),
        0
    ) AS SAMinus,


    /*
     * EXCESS
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'EXCESS'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_in, 0)
                ELSE 0
            END
        ),
        0
    ) AS Excess,


    /*
     * SHORT
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'SHORT'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_out, 0)
                ELSE 0
            END
        ),
        0
    ) AS Short,


    /*
     * WARRANTY IN
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'WARRANTY'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_in, 0)
                ELSE 0
            END
        ),
        0
    ) AS WD,


    /*
     * WARRANTY OUT
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'WARRANTY'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN ISNULL(isl.isl_out, 0)
                ELSE 0
            END
        ),
        0
    ) AS WR,


    /*
     * STOCK TRANSFER
     *
     * Location-wise TVF:
     * SUM(In) - SUM(Out)
     */
    ISNULL(
        SUM(
            CASE
                WHEN isl.isl_form = 'ST'
                 AND CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN
                    ISNULL(isl.isl_in, 0)
                    -
                    ISNULL(isl.isl_out, 0)
                ELSE 0
            END
        ),
        0
    ) AS ST,


    /*
     * FINAL QTY
     *
     * Same formula as tvf_Stock_ledger
     */
    ISNULL(
        SUM(
            CASE
                WHEN CAST(isl.isl_date AS DATE) < '" + fromDateValue + @"'
                     OR isl.isl_form = 'OS'
                THEN
                    ISNULL(isl.isl_in, 0)
                    -
                    ISNULL(isl.isl_out, 0)

                WHEN CAST(isl.isl_date AS DATE)
                     BETWEEN '" + fromDateValue + @"'
                     AND '" + toDateValue + @"'
                THEN
                    ISNULL(isl.isl_in, 0)
                    -
                    ISNULL(isl.isl_out, 0)

                ELSE 0
            END
        ),
        0
    ) AS Qty


FROM inv_item_reg ir

INNER JOIN inv_stock_ledger isl
    ON isl.isl_ir_id = ir.ir_id

" + itemWhere + @"

" + locationWhere + @"

GROUP BY

    ir.ir_id,
    ir.ir_code,
    ir.ir_name

ORDER BY

    ir.ir_name;
";


                DataTable stockLedger =
                    usqlre.dbReaderFill(sql);

                hash.Add("StockLedger", stockLedger);


                if (model.Detailed)
                {
                    string detailSql = @"

SELECT

    isl.isl_date AS Date,

    isl.isl_entryno AS EntryNo,

    ir.ir_code AS ItemCode,

    ir.ir_name AS ItemName,


    CASE

        WHEN isl.isl_form = 'OS'
            THEN 'Opening Stock'

        WHEN isl.isl_form = 'PURCHASE'
            THEN 'Purchase'

        WHEN isl.isl_form = 'PR'
            THEN 'Purchase Return'

        WHEN isl.isl_form = 'PRODUCTION'
            THEN 'Production'

        WHEN isl.isl_form = 'DAMAGE'
            THEN 'Damage'

        WHEN isl.isl_form = '1'
            THEN 'Sales B2C'

        WHEN isl.isl_form = '2'
            THEN 'Sales ES'

        WHEN isl.isl_form = '6'
            THEN 'Sales B2B'

        WHEN isl.isl_form = '4'
            THEN 'Sales'

        WHEN isl.isl_form = '8'
            THEN 'Sales'

        WHEN isl.isl_form = '7'
            THEN 'Sales Return'

        WHEN isl.isl_form = '9'
            THEN 'Sales Return'

        WHEN isl.isl_form = '10'
            THEN 'Sales Return'

        WHEN isl.isl_form = 'ST'
            THEN
                CASE
                    WHEN ISNULL(isl.isl_in, 0) > 0
                        THEN 'ST+'
                    WHEN ISNULL(isl.isl_out, 0) > 0
                        THEN 'ST-'
                    ELSE 'ST'
                END

        WHEN isl.isl_form = 'STOCK ADJUSTMENT'
            THEN 'Stock Adjustment'

        WHEN isl.isl_form = 'EXCESS'
            THEN 'Excess'

        WHEN isl.isl_form = 'SHORT'
            THEN 'Short'

        WHEN isl.isl_form = 'WARRANTY'
            THEN 'Warranty'

        ELSE isl.isl_form

    END AS Form,


    ISNULL(isl.isl_in, 0) AS [In],

    ISNULL(isl.isl_out, 0) AS [Out],


    CASE

        WHEN ISNULL(isl.isl_in, 0) > 0
            THEN ISNULL(isl.isl_in, 0)

        ELSE -ISNULL(isl.isl_out, 0)

    END AS Qty,


    isl.isl_prate AS PRate,

    isl.isl_realprate AS RPRate,

    isl.isl_cost AS Cost,

    isl.isl_uniquecode AS UniqueCode,

    isl.isl_location_id AS LocationId


FROM inv_stock_ledger isl

INNER JOIN inv_item_reg ir
    ON isl.isl_ir_id = ir.ir_id

" + itemWhere + @"

" + locationWhere + @"

AND CAST(isl.isl_date AS DATE)
    BETWEEN '" + fromDateValue + @"'
    AND '" + toDateValue + @"'
";


                    if (model.InOut)
                    {
                        detailSql += @"

AND
(
    ISNULL(isl.isl_in, 0) <> 0
    OR
    ISNULL(isl.isl_out, 0) <> 0
)
";
                    }


                    detailSql += @"

ORDER BY

    isl.isl_date,

    isl.isl_id;
";


                    DataTable details =
                        usqlre.dbReaderFill(detailSql);

                    hash.Add("StockLedgerDetails", details);
                }


                usqlre.close();


                return Content(
                    ReportModelContext.searializeDt(hash),
                    "application/json"
                );
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("stock-transfer-report")]
        public IActionResult StockTransferReport([FromBody] StockTransferReport model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                var hash = new Dictionary<string, DataTable>();

                if (string.IsNullOrWhiteSpace(model.FromDate) ||
                    !DateTime.TryParse(model.FromDate, out DateTime fromDate))
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "Invalid FromDate",
                        data = new object[] { }
                    });
                }

                if (string.IsNullOrWhiteSpace(model.ToDate) ||
                    !DateTime.TryParse(model.ToDate, out DateTime toDate))
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "Invalid ToDate",
                        data = new object[] { }
                    });
                }

                if (fromDate.Date > toDate.Date)
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "FromDate cannot be greater than ToDate",
                        data = new object[] { }
                    });
                }

                string fromDateValue = fromDate.ToString("yyyy-MM-dd");
                string toDateValue = toDate.Date.AddDays(1).ToString("yyyy-MM-dd");

                string transferWhere = "";

                if (model.TransferFromId > 0)
                {
                    transferWhere +=
                        " AND sti.isti_transfer_from = " + model.TransferFromId;
                }

                if (model.TransferToId > 0)
                {
                    transferWhere +=
                        " AND sti.isti_transfer_to = " + model.TransferToId;
                }


                string sql = @"

SELECT

    ROW_NUMBER() OVER
    (
        ORDER BY
            sti.isti_date DESC,
            sti.isti_entryno DESC,
            stp.istp_id
    ) AS SlNo,

    sti.isti_date AS Date,

    sti.isti_entryno AS EntryNo,

    sti.isti_transfer_from AS FromId,

    ISNULL(lf.gl_name, '') AS [From],

    sti.isti_transfer_to AS ToId,

    ISNULL(lt.gl_name, '') AS [To],

    stp.istp_ir_id AS ItemId,

    ir.ir_code AS ItemCode,

    ir.ir_name AS ItemName,

    ISNULL(stp.istp_qty, 0) AS Qty,

    ISNULL(stp.istp_rate, 0) AS Rate,

    ISNULL(
        stp.istp_amount,
        ISNULL(stp.istp_qty, 0) * ISNULL(stp.istp_rate, 0)
    ) AS Total

FROM inv_stock_transfer_inf sti

INNER JOIN inv_stock_transfer_par stp
    ON sti.isti_entryno = stp.istp_entryno

INNER JOIN inv_item_reg ir
    ON ir.ir_id = stp.istp_ir_id

LEFT JOIN gnl_location lf
    ON lf.gl_id = sti.isti_transfer_from

LEFT JOIN gnl_location lt
    ON lt.gl_id = sti.isti_transfer_to

WHERE

    sti.isti_date >= '" + fromDateValue + @"'

    AND sti.isti_date < '" + toDateValue + @"'

" + transferWhere + @"

ORDER BY

    sti.isti_date DESC,
    sti.isti_entryno DESC,
    stp.istp_id;
";


                DataTable stockTransfer =
                    usqlre.dbReaderFill(sql);

                hash.Add("StockTransferReport", stockTransfer);



                if (model.Detailed)
                {
                    string detailSql = @"

SELECT

    sti.isti_date AS Date,

    sti.isti_entryno AS EntryNo,

    sti.isti_transfer_from AS FromId,

    ISNULL(lf.gl_name, '') AS [From],

    sti.isti_transfer_to AS ToId,

    ISNULL(lt.gl_name, '') AS [To],

    stp.istp_ir_id AS ItemId,

    ir.ir_code AS ItemCode,

    ir.ir_name AS ItemName,

    stp.istp_uniquecode AS UniqueCode,

    ISNULL(stp.istp_qty, 0) AS Qty,

    ISNULL(stp.istp_rate, 0) AS Rate,

    ISNULL(
        stp.istp_amount,
        ISNULL(stp.istp_qty, 0) * ISNULL(stp.istp_rate, 0)
    ) AS Total,

    ISNULL(stp.istp_cost, 0) AS Cost,

    ISNULL(sti.isti_remarks, '') AS Remark,

    sti.isti_indentno AS IndentNo,

    sti.isti_loc_entryno AS LocationEntryNo,

    ISNULL(sti.isti_delivery_chalan_no, '') AS DeliveryChalanNo,

    ISNULL(sti.isti_vehicle_no, '') AS VehicleNo

FROM inv_stock_transfer_inf sti

INNER JOIN inv_stock_transfer_par stp
    ON sti.isti_entryno = stp.istp_entryno

INNER JOIN inv_item_reg ir
    ON ir.ir_id = stp.istp_ir_id

LEFT JOIN gnl_location lf
    ON lf.gl_id = sti.isti_transfer_from

LEFT JOIN gnl_location lt
    ON lt.gl_id = sti.isti_transfer_to

WHERE

    sti.isti_date >= '" + fromDateValue + @"'

    AND sti.isti_date < '" + toDateValue + @"'

" + transferWhere + @"

ORDER BY

    sti.isti_date DESC,
    sti.isti_entryno DESC,
    stp.istp_id;
";


                    DataTable details =
                        usqlre.dbReaderFill(detailSql);

                    hash.Add("StockTransferReportDetails", details);
                }


                usqlre.close();


                return Content(
                    ReportModelContext.searializeDt(hash),
                    "application/json"
                );
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    status_code = 500,
                    message = ex.Message,
                    data = new object[] { }
                });
            }
        }

        [HttpPost("opening-stock-report")]
        public IActionResult OpeningStockReport([FromBody] OpeningStockReport model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                var hash = new Dictionary<string, DataTable>();

                if (model == null)
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "Invalid request",
                        data = new object[] { }
                    });
                }

                // ---------------------------------------------------------
                // DATE VALIDATION
                // ---------------------------------------------------------

                if (string.IsNullOrWhiteSpace(model.FromDate) ||
                    !DateTime.TryParse(model.FromDate, out DateTime fromDate))
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "Invalid FromDate",
                        data = new object[] { }
                    });
                }

                if (string.IsNullOrWhiteSpace(model.ToDate) ||
                    !DateTime.TryParse(model.ToDate, out DateTime toDate))
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "Invalid ToDate",
                        data = new object[] { }
                    });
                }

                if (fromDate.Date > toDate.Date)
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "FromDate cannot be greater than ToDate",
                        data = new object[] { }
                    });
                }

                string fromDateValue = fromDate.ToString("yyyy-MM-dd");
                string toDateValue = toDate.ToString("yyyy-MM-dd");


                // ---------------------------------------------------------
                // DATE TYPE
                // ---------------------------------------------------------

                string dateColumn = "osi.osi_date";

                if (model.DateType == 2)
                {
                    dateColumn = "osi.osi_inv_date";
                }


                // ---------------------------------------------------------
                // FILTERS
                // ---------------------------------------------------------

                string where = "";


                // Location
                if (model.LocationId > 0)
                {
                    where +=
                        " AND ISNULL(osp.osp_location_id, osi.osi_location_id) = "
                        + model.LocationId;
                }


                // Item
                if (model.ItemId > 0)
                {
                    where +=
                        " AND osp.osp_ir_id = "
                        + model.ItemId;
                }


                // Category
                if (model.CategoryId > 0)
                {
                    where +=
                        " AND ir.ir_category_id = "
                        + model.CategoryId;
                }


                // Sub Category
                if (model.SubCategoryId > 0)
                {
                    where +=
                        " AND ir.ir_sub_category_id = "
                        + model.SubCategoryId;
                }


                // Supplier
                if (model.SupplierId > 0)
                {
                    where +=
                        " AND osi.osi_sup_id = "
                        + model.SupplierId;
                }


                // Salesman
                if (model.SalesmanId > 0)
                {
                    where +=
                        " AND osi.osi_salesman_id = "
                        + model.SalesmanId;
                }


                // User
                if (model.UserId > 0)
                {
                    where +=
                        " AND osi.osi_user_id = "
                        + model.UserId;
                }


                // Barcode
                if (!string.IsNullOrWhiteSpace(model.Barcode))
                {
                    string barcode = model.Barcode.Replace("'", "''");

                    where +=
                        " AND CAST(osp.osp_uniquecode AS NVARCHAR(100)) = '"
                        + barcode + "'";
                }


                // Internal Barcode
                if (!string.IsNullOrWhiteSpace(model.IntBarcode))
                {
                    string intBarcode = model.IntBarcode.Replace("'", "''");

                    where +=
                        " AND ISNULL(osp.osp_int_barcode, '') = '"
                        + intBarcode + "'";
                }


                // ---------------------------------------------------------
                // GST TYPE
                //
                // 1 = All
                // 2 = Included / Taxable
                // 3 = Excluded / Non-taxable
                // ---------------------------------------------------------

                if (model.GstType == 2)
                {
                    // GST Included / Taxable items
                    where +=
                        " AND ISNULL(ir.ir_taxper, 0) > 0";
                }
                else if (model.GstType == 3)
                {
                    // GST Excluded / Non-taxable items
                    where +=
                        " AND ISNULL(ir.ir_taxper, 0) = 0";
                }


                // ---------------------------------------------------------
                // DATE FILTER
                // ---------------------------------------------------------

                string dateWhere = @"
            AND CAST(" + dateColumn + @"
                AS DATE) BETWEEN '" + fromDateValue + @"'
                AND '" + toDateValue + @"'
        ";


                // =========================================================
                // SUMMARY
                // =========================================================

                if (model.Summary)
                {
                    string summarySql = @"

SELECT

    ROW_NUMBER() OVER
    (
        ORDER BY
            " + dateColumn + @" DESC,
            osi.osi_entryno DESC,
            osp.osp_id
    ) AS SlNo,

    osi.osi_entryno AS EntryNo,

    " + dateColumn + @" AS [Date],

    osi.osi_sup_id AS SupplierId,

    ISNULL(sup.as_name, '') AS Supplier,

    osp.osp_ir_id AS ItemId,

    ISNULL(ir.ir_code, '') AS ItemCode,

    ISNULL(ir.ir_name, '') AS ItemName,

    ISNULL(osp.osp_qty, 0) AS Qty,

    ISNULL(osp.osp_prate, 0) AS PRate,

    ISNULL(osp.osp_gross_value, 0) AS Gross,

    ISNULL(osp.osp_disc, 0) AS Disc,

    ISNULL(osp.osp_net_amount, 0) AS Net,

    ISNULL(osp.osp_tax, 0) AS Gst,

    ISNULL(osp.osp_total, 0) AS Total,

    ISNULL(loc.gl_name, '') AS Location


FROM inv_openingstock_inf osi

INNER JOIN inv_openingstock_par osp
    ON osi.osi_entryno = osp.osp_entryno

INNER JOIN inv_item_reg ir
    ON osp.osp_ir_id = ir.ir_id

LEFT JOIN acc_subhead sup
    ON osi.osi_sup_id = sup.as_id

LEFT JOIN gnl_location loc
    ON ISNULL(osp.osp_location_id, osi.osi_location_id)
       = loc.gl_id

WHERE 1 = 1

" + dateWhere + @"

" + where + @"

ORDER BY

    " + dateColumn + @" DESC,
    osi.osi_entryno DESC,
    osp.osp_id;

";

                    DataTable summary =
                        usqlre.dbReaderFill(summarySql);

                    hash.Add("OpeningStockReport", summary);
                }


                // =========================================================
                // DETAILED
                // =========================================================

                if (model.Detailed)
                {
                    string detailSql = @"

SELECT

    osi.osi_entryno AS EntryNo,

    " + dateColumn + @" AS [Date],

    osi.osi_inv_date AS InvoiceDate,

    osi.osi_sup_id AS SupplierId,

    ISNULL(sup.as_name, '') AS Supplier,

    osp.osp_ir_id AS ItemId,

    ISNULL(ir.ir_code, '') AS ItemCode,

    ISNULL(ir.ir_name, '') AS ItemName,

    ISNULL(cat.c_name, '') AS Category,

    ISNULL(subcat.sc_name, '') AS SubCategory,

    ISNULL(loc.gl_name, '') AS Location,

    ISNULL(osp.osp_qty, 0) AS Qty,

    ISNULL(osp.osp_prate, 0) AS PRate,

    ISNULL(osp.osp_realprate, 0) AS RPRate,

    ISNULL(osp.osp_cost, 0) AS Cost,

    ISNULL(osp.osp_gross_value, 0) AS Gross,

    ISNULL(osp.osp_disc_per, 0) AS DiscPer,

    ISNULL(osp.osp_disc, 0) AS Disc,

    ISNULL(osp.osp_net_amount, 0) AS Net,

    ISNULL(osp.osp_tax, 0) AS Gst,

    ISNULL(osp.osp_sgst, 0) AS SGST,

    ISNULL(osp.osp_cgst, 0) AS CGST,

    ISNULL(osp.osp_igst, 0) AS IGST,

    ISNULL(osp.osp_total, 0) AS Total,

    ISNULL(osp.osp_mrp, 0) AS MRP,

    ISNULL(osp.osp_retail, 0) AS Retail,

    ISNULL(osp.osp_wholesale, 0) AS Wholesale,

    ISNULL(osp.osp_spretail, 0) AS SpRetail,

    ISNULL(osp.osp_branch, 0) AS Branch,

    osp.osp_exp_date AS ExpiryDate,

    ISNULL(osp.osp_color, '') AS Color,

    ISNULL(osp.osp_size, '') AS Size,

    ISNULL(osp.osp_brand, '') AS Brand,

    ISNULL(osp.osp_int_barcode, '') AS IntBarcode,

    osp.osp_uniquecode AS Barcode,

    ISNULL(osi.osi_sup_invno, '') AS SupplierInvoiceNo,

    ISNULL(osi.osi_narration, '') AS Remark,

    osi.osi_user_id AS UserId,

    ISNULL(usr.gu_name, '') AS [User],

    osi.osi_salesman_id AS SalesmanId,

    ISNULL(osi.osi_salesman, '') AS Salesman,

    osi.osi_location_id AS HeaderLocationId,

    osp.osp_location_id AS ItemLocationId


FROM inv_openingstock_inf osi

INNER JOIN inv_openingstock_par osp
    ON osi.osi_entryno = osp.osp_entryno

INNER JOIN inv_item_reg ir
    ON osp.osp_ir_id = ir.ir_id

LEFT JOIN acc_subhead sup
    ON osi.osi_sup_id = sup.as_id

LEFT JOIN gnl_location loc
    ON ISNULL(osp.osp_location_id, osi.osi_location_id)
       = loc.gl_id

LEFT JOIN inv_category cat
    ON ir.ir_category_id = cat.c_id

LEFT JOIN inv_subcategory subcat
    ON ir.ir_sub_category_id = subcat.sc_id

LEFT JOIN gnl_users usr
    ON osi.osi_user_id = usr.gu_user_id

WHERE 1 = 1

" + dateWhere + @"

" + where + @"

ORDER BY

    " + dateColumn + @" DESC,
    osi.osi_entryno DESC,
    osp.osp_id;

";

                    DataTable details =
                        usqlre.dbReaderFill(detailSql);

                    hash.Add("OpeningStockReportDetails", details);
                }


                usqlre.close();


                return Content(
                    ReportModelContext.searializeDt(hash),
                    "application/json"
                );
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    status_code = 500,
                    message = ex.Message,
                    data = new object[] { }
                });
            }
        }

        [HttpPost("damage-report")]
        public IActionResult DamageReport([FromBody] DamageReport model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                var hash = new Dictionary<string, DataTable>();

                if (model == null)
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "Invalid request",
                        data = new object[] { }
                    });
                }


                if (string.IsNullOrWhiteSpace(model.FromDate) ||
                    !DateTime.TryParse(model.FromDate, out DateTime fromDate))
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "Invalid FromDate",
                        data = new object[] { }
                    });
                }

                if (string.IsNullOrWhiteSpace(model.ToDate) ||
                    !DateTime.TryParse(model.ToDate, out DateTime toDate))
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "Invalid ToDate",
                        data = new object[] { }
                    });
                }

                if (fromDate.Date > toDate.Date)
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "FromDate cannot be greater than ToDate",
                        data = new object[] { }
                    });
                }

                string fromDateValue = fromDate.ToString("yyyy-MM-dd");
                string toDateValue = toDate.ToString("yyyy-MM-dd");


                string where = "";

                if (model.ItemId > 0)
                {
                    where +=
                        " AND dp.dp_ir_id = " + model.ItemId;
                }


                if (!string.IsNullOrWhiteSpace(model.Barcode))
                {
                    string barcode = model.Barcode.Replace("'", "''");

                    where +=
                        " AND CAST(dp.dp_uniquecode AS NVARCHAR(100)) = '" +
                        barcode + "'";
                }



                if (model.Summary)
                {
                    string summarySql = @"

SELECT

    ROW_NUMBER() OVER
    (
        ORDER BY
            di.di_date DESC,
            di.di_entryno DESC,
            dp.dp_row_id
    ) AS SlNo,

    di.di_date AS [Date],

    di.di_entryno AS EntryNo,

    ISNULL(ir.ir_name, '') AS ItemName,

    ISNULL(dp.dp_qty, 0) AS Qty,

    ISNULL(dp.dp_rate, 0) AS Rate,

    ISNULL(
        dp.dp_total,
        ISNULL(dp.dp_qty, 0) *
        ISNULL(dp.dp_rate, 0)
    ) AS Total

FROM inv_damage_inf di

INNER JOIN inv_damage_par dp
    ON di.di_entryno = dp.dp_entryno

INNER JOIN inv_item_reg ir
    ON dp.dp_ir_id = ir.ir_id

WHERE
    CAST(di.di_date AS DATE)
        BETWEEN '" + fromDateValue + @"'
        AND '" + toDateValue + @"'

" + where + @"

ORDER BY

    di.di_date DESC,
    di.di_entryno DESC,
    dp.dp_row_id;

";

                    DataTable summary =
                        usqlre.dbReaderFill(summarySql);

                    hash.Add("DamageReport", summary);
                }


                if (model.Detailed)
                {
                    string detailSql = @"

SELECT

    di.di_date AS [Date],

    di.di_entryno AS EntryNo,

    di.di_voucher_name AS VoucherName,

    dp.dp_ir_id AS ItemId,

    ISNULL(ir.ir_code, '') AS ItemCode,

    ISNULL(ir.ir_name, '') AS ItemName,

    dp.dp_uniquecode AS UniqueCode,

    ISNULL(dp.dp_qty, 0) AS Qty,

    ISNULL(dp.dp_rate, 0) AS Rate,

    ISNULL(
        dp.dp_total,
        ISNULL(dp.dp_qty, 0) *
        ISNULL(dp.dp_rate, 0)
    ) AS Total,

    ISNULL(dp.dp_pur_no, 0) AS PurchaseNo,

    ISNULL(dp.dp_remarks, '') AS Remarks,

    di.di_damage_acc_id AS DamageAccountId,

    di.di_credit_acc_id AS CreditAccountId,

    di.di_user_id AS UserId,

    ISNULL(usr.gu_name, '') AS [User],

    di.di_sales_status AS SalesStatus,

    di.di_pending_status AS PendingStatus

FROM inv_damage_inf di

INNER JOIN inv_damage_par dp
    ON di.di_entryno = dp.dp_entryno

INNER JOIN inv_item_reg ir
    ON dp.dp_ir_id = ir.ir_id

LEFT JOIN gnl_users usr
    ON di.di_user_id = usr.gu_user_id

WHERE
    CAST(di.di_date AS DATE)
        BETWEEN '" + fromDateValue + @"'
        AND '" + toDateValue + @"'

" + where + @"

ORDER BY

    di.di_date DESC,
    di.di_entryno DESC,
    dp.dp_row_id;

";

                    DataTable details =
                        usqlre.dbReaderFill(detailSql);

                    hash.Add("DamageReportDetails", details);
                }


                usqlre.close();


                return Content(
                    ReportModelContext.searializeDt(hash),
                    "application/json"
                );
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    status_code = 500,
                    message = ex.Message,
                    data = new object[] { }
                });
            }
        }

        [HttpPost("item-history-report")]
        public IActionResult ItemHistoryReport([FromBody] ItemHistoryReport model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            try
            {
                var hash = new Dictionary<string, DataTable>();

                if (model == null)
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "Invalid request",
                        data = new object[] { }
                    });
                }




                if (string.IsNullOrWhiteSpace(model.FromDate) ||
                    !DateTime.TryParse(model.FromDate, out DateTime fromDate))
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "Invalid FromDate",
                        data = new object[] { }
                    });
                }


                if (string.IsNullOrWhiteSpace(model.ToDate) ||
                    !DateTime.TryParse(model.ToDate, out DateTime toDate))
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "Invalid ToDate",
                        data = new object[] { }
                    });
                }


                if (fromDate.Date > toDate.Date)
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 400,
                        message = "FromDate cannot be greater than ToDate",
                        data = new object[] { }
                    });
                }


                string fromDateValue = fromDate.ToString("yyyy-MM-dd");
                string toDateValue = toDate.ToString("yyyy-MM-dd");


                string where = "";

                if (model.ItemId > 0)
                {
                    where +=
                        " AND isl.isl_ir_id = " + model.ItemId;
                }



                string sql = @"

SELECT

    ROW_NUMBER() OVER
    (
        ORDER BY
            isl.isl_date DESC,
            isl.isl_entryno DESC
    ) AS SlNo,

    CAST(isl.isl_date AS DATE) AS [Date],

    isl.isl_entryno AS EntryNo,

    ISNULL(gl.gl_name, '') AS Location,

    ISNULL(ir.ir_name, '') AS ItemName,

    CAST('' AS NVARCHAR(200)) AS Party,

    CASE
        WHEN ISNULL(isl.isl_in, 0) > 0
            THEN isl.isl_in
        WHEN ISNULL(isl.isl_out, 0) > 0
            THEN -isl.isl_out
        ELSE 0
    END AS Qty,

    ISNULL(isl.isl_cost, 0) AS Cost,

    ISNULL(isl.isl_prate, 0) AS Rate,

    (
        CASE
            WHEN ISNULL(isl.isl_in, 0) > 0
                THEN isl.isl_in
            WHEN ISNULL(isl.isl_out, 0) > 0
                THEN -isl.isl_out
            ELSE 0
        END
        *
        ISNULL(isl.isl_prate, 0)
    ) AS Total

FROM inv_stock_ledger isl

INNER JOIN inv_item_reg ir
    ON isl.isl_ir_id = ir.ir_id

LEFT JOIN gnl_location gl
    ON isl.isl_location_id = gl.gl_id

WHERE
    CAST(isl.isl_date AS DATE)
        BETWEEN '" + fromDateValue + @"'
        AND '" + toDateValue + @"'

" + where + @"

ORDER BY

    isl.isl_date DESC,
    isl.isl_entryno DESC;

";


                DataTable itemHistory =
                    usqlre.dbReaderFill(sql);


                hash.Add("ItemHistoryReport", itemHistory);



                usqlre.close();


                return Content(
                    ReportModelContext.searializeDt(hash),
                    "application/json"
                );
            }
            catch (Exception ex)
            {
                usqlre.close();

                return Ok(new
                {
                    status = false,
                    status_code = 500,
                    message = ex.Message,
                    data = new object[] { }
                });
            }
        }
    }
}