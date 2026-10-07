using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using MictcoWebService.Authentication;
using MictcoWebService.Controllers;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;
using static System.Net.Mime.MediaTypeNames;
//UserSqlServer

namespace MictcoWebService.Common
{
    public class UserSqlServer
    {
        public string userId = "0";
        public string locationId = "0";
        public string server = "";
        public string database = "";
        public string username = "";
        public string password = "";
        public string connetionString = "";
        public string user_role = "";
        public string gu_acc_id = "";
        public string gu_user_cash_id = "";
        public string status = "";
        public string lastError = "";
        public string areaId = "0";
        public string routeId = "0";
        /// <summary>JWT ClaimTypes.Hash — encrypted gu_pass fingerprint; checked only when userid is in gnl_pass_validate.</summary>
        public string tokenPassHash = "";
        public static DateTime sdate, edate;
        //public string connetionString = "";
        public SqlConnection shop;
        public SqlDataAdapter da;
        static bool _passValidateTableEnsured;

        /// <summary>
        /// Caches IsUserValid results per (connectionString + userId) for 5 minutes.
        /// Without this cache, every request opens a separate SQL connection just
        /// for the user-active check, which exhausts the pool under load and
        /// masks connection failures as "User is not authorized."
        /// </summary>
        private static readonly ConcurrentDictionary<string, (bool valid, DateTime expiresUtc)>
            _userValidCache = new ConcurrentDictionary<string, (bool, DateTime)>();

        private const int UserValidCacheMinutes = 5;

        ControllerBase controller;
        //public UserSqlServer(ControllerBase controller, bool validateUser = true)
        //{
        //    this.controller = controller;
        //    var handler = new JwtSecurityTokenHandler();
        //    string authHeader = controller.Request.Headers["Authorization"];
        //    authHeader = authHeader.Replace("Bearer ", "");
        //    var jsonToken = handler.ReadToken(authHeader);
        //    var tokenS = handler.ReadToken(authHeader) as JwtSecurityToken;

        //    var jti = tokenS.Claims.First(claim => claim.Type == "jti").Value;
        //    var id = tokenS.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;

        //    this.userId = tokenS.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;

        //    this.server = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Actor).Value;
        //    this.database = tokenS.Claims.First(claim => claim.Type == ClaimTypes.GivenName).Value;
        //    this.username = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Gender).Value;
        //    this.password = tokenS.Claims.First(claim => claim.Type == ClaimTypes.DateOfBirth).Value;
        //    this.locationId = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Anonymous).Value;

        //    this.user_role = tokenS.Claims.First(claim => claim.Type == ClaimTypes.PostalCode).Value;
        //    this.gu_acc_id = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Sid).Value;
        //    this.gu_user_cash_id = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Surname).Value;
        //    this.areaId = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Locality).Value;
        //    this.routeId = tokenS.Claims.First(claim => claim.Type == ClaimTypes.StreetAddress).Value;
        //    var passHashClaim = tokenS.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Hash);
        //    this.tokenPassHash = passHashClaim != null ? passHashClaim.Value : "";
        //    //this.server = server.Replace(":", ",");


        //    connetionString = @"Data Source=" + CommonHelper.tokenDecrypt(server).Replace(":", ",") + ";Initial Catalog=" + CommonHelper.tokenDecrypt(database) + ";User ID=" + CommonHelper.tokenDecrypt(username) + ";Password=" + CommonHelper.tokenDecrypt(password) + "";
        //    shop = new SqlConnection(connetionString);

        //    if (validateUser)
        //    {
        //        if (!IsUserValid())
        //        {
        //            throw new UnauthorizedAccessException("User is not authorized.");
        //        }
        //    }
        //}

        public UserSqlServer(ControllerBase controller, bool validateUser = true)
        {
            this.controller = controller;

            string authHeader = controller.Request.Headers["Authorization"];

            if (!string.IsNullOrEmpty(authHeader))
            {
                authHeader = authHeader.Replace("Bearer ", "");

                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(authHeader, JwtValidationParameters(), out SecurityToken validatedToken);
                var token = validatedToken as JwtSecurityToken;
                if (token == null)
                    throw new UnauthorizedAccessException("User is not authorized.");

                // Read DB claims (if available)
                this.server = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Actor)?.Value ?? "";
                this.database = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "";
                this.username = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Gender)?.Value ?? "";
                this.password = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.DateOfBirth)?.Value ?? "";

                // Read user claims
                this.userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "0";
                this.locationId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Anonymous)?.Value ?? "0";
                this.user_role = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.PostalCode)?.Value ?? "";
                this.gu_acc_id = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value ?? "0";
                this.gu_user_cash_id = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "0";
                this.areaId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Locality)?.Value ?? "0";
                this.routeId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.StreetAddress)?.Value ?? "0";
                this.tokenPassHash = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Hash)?.Value ?? "";

                // OLD FLOW : If DB claims exist, use them
                if (!string.IsNullOrWhiteSpace(this.server) &&
                    !string.IsNullOrWhiteSpace(this.database))
                {
                    connetionString = SqlConnectionPool.Apply(
                        @"Data Source=" + CommonHelper.tokenDecrypt(this.server).Replace(":", ",") +
                        ";Initial Catalog=" + CommonHelper.tokenDecrypt(this.database) +
                        ";User ID=" + CommonHelper.tokenDecrypt(this.username) +
                        ";Password=" + CommonHelper.tokenDecrypt(this.password));

                    shop = new SqlConnection(connetionString);
                }
                else
                {
                    // NEW FLOW : Use appsettings ERPConnection
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();

                    connetionString = SqlConnectionPool.Apply(configuration.GetConnectionString("ERPConnection"));
                    shop = new SqlConnection(connetionString);
                }
            }
            else
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                connetionString = SqlConnectionPool.Apply(configuration.GetConnectionString("ERPConnection"));
                shop = new SqlConnection(connetionString);
            }

            RegisterConnectionRelease();

            if (validateUser && int.TryParse(this.userId, out var parsedUserId) && parsedUserId > 0)
            {
                // IsUserValid throws InvalidOperationException on DB errors
                // so callers see the real problem instead of "not authorized".
                if (!IsUserValid())
                    throw new UnauthorizedAccessException("User is not authorized.");
            }
        }

        private static TokenValidationParameters _jwtValidation;

        private static TokenValidationParameters JwtValidationParameters()
        {
            if (_jwtValidation != null)
                return _jwtValidation;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            _jwtValidation = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = configuration["JWT:ValidAudience"],
                ValidIssuer = configuration["JWT:ValidIssuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"] ?? "")),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
            return _jwtValidation;
        }

        public string getConnectionString()
        {
            return connetionString;
        }

        /// <summary>
        /// Returns the connection to the pool when the HTTP response finishes,
        /// including when the action forgot to close it or failed with an exception.
        /// </summary>
        private void RegisterConnectionRelease()
        {
            var httpContext = controller?.HttpContext;
            if (httpContext == null || shop == null)
                return;

            httpContext.Response.OnCompleted(() =>
            {
                ReleaseConnection();
                return Task.CompletedTask;
            });
        }

        public void ReleaseConnection()
        {
            var connection = shop;
            if (connection == null)
                return;

            try
            {
                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }
        }

        public async Task<bool> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (shop.State == ConnectionState.Closed)
                {
                    shop.ConnectionString = connetionString;
                    await shop.OpenAsync(cancellationToken);
                }
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public bool OpenConnection()
        {
            try
            {
                if (shop.State == ConnectionState.Closed)
                {
                    shop.ConnectionString = connetionString;
                    shop.Open();
                }
                return true;

            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                Console.WriteLine(ex.ToString());
                return false;
            }

        }

       
        public bool dbExecute(string qry)
        {
            SqlCommand cmd1;
            try
            {
                if (OpenConnection())
                {
                    cmd1 = new SqlCommand(qry, shop);
                    cmd1.CommandTimeout = 60;
                    cmd1.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public DataTable dbReaderFill(string qry)
        {
            if (OpenConnection())
            {
                DataTable _temp = new DataTable();
                da = new SqlDataAdapter(qry, shop);
                da.SelectCommand.CommandTimeout = 60;
                da.Fill(_temp);
                return _temp;
            }
            return new DataTable();
        }
        public object dbScalar(string qry)
        {
            if (OpenConnection())
            {
                SqlCommand cmd = new SqlCommand(qry, shop);
                cmd.CommandTimeout = 60;
                return cmd.ExecuteScalar();
            }
            return null;
        }
        public bool FinancialDateCheck(DateTime dt_now)
        {
            try
            {
                string sql = "select com_sdate,com_edate from gnl_company";
                DataTable dt = dbReaderFill(sql);
                sdate = Convert.ToDateTime(dt.Rows[0]["com_sdate"].ToString());
                edate = Convert.ToDateTime(dt.Rows[0]["com_edate"].ToString());
                return dt_now.Date >= sdate.Date && dt_now.Date <= edate.Date;
            }
            catch (Exception ex)
            {

                return true;
            }
        }


        public DataSet dbreadDataset(string qry)
        {
            if (OpenConnection())
            {
                DataSet _temp = new DataSet();
                da = new SqlDataAdapter(qry, shop);
                da.SelectCommand.CommandTimeout = 60;
                da.Fill(_temp);
                return _temp;
            }
            else
            {
                return null;
            }
        }

        public void close()
        {
            ReleaseConnection();
        }


        public int documentAttachments(string da_type, string da_fserver_id)
        {
            int modified = 0;
            try
            {
                using (SqlCommand cmd = new SqlCommand("INSERT INTO document_attachments(da_type,da_fserver_id) output INSERTED.ID VALUES(@da_type,@da_fserver_id)", shop))
                {
                    if (OpenConnection())
                    {
                        cmd.Parameters.AddWithValue("@da_type", da_type);
                        cmd.Parameters.AddWithValue("@da_fserver_id", da_fserver_id);
                        modified = (int)cmd.ExecuteScalar();

                        return modified;

                    }
                    else
                    {
                        return modified;

                    }
                }
            }
            catch (Exception ex)
            {
                return modified;
            }
        }

        public int insertStartRouteRide(StartRouteRide model)
        {
            int modified = 0;
            try
            {
                using (SqlCommand cmd = new SqlCommand("INSERT INTO route_tracking(rt_starting_latitude,rt_starting_longitude,rt_date,rt_start_time,rt_start_meter_reading,rt_route_id,rt_salesman) output INSERTED.rt_id VALUES(@rt_starting_latitude,@rt_starting_longitude,@rt_date,@rt_start_time,@rt_start_meter_reading,@rt_route_id,@rt_salesman)", shop))
                {
                    if (OpenConnection())
                    {
                        //@rt_starting_latitude,@rt_starting_longitude,@rt_date,@rt_start_time,@rt_start_meter_reading,@rt_route_id
                        cmd.Parameters.AddWithValue("@rt_starting_latitude", model.latitude);
                        cmd.Parameters.AddWithValue("@rt_starting_longitude", model.longitude);
                        cmd.Parameters.AddWithValue("@rt_date", model.date);
                        cmd.Parameters.AddWithValue("@rt_start_time", model.time);
                        cmd.Parameters.AddWithValue("@rt_start_meter_reading", model.meterReading);
                        cmd.Parameters.AddWithValue("@rt_route_id", model.routeId);
                        cmd.Parameters.AddWithValue("@rt_salesman", Convert.ToInt32(userId));
                        modified = (int)cmd.ExecuteScalar();
                        return modified;

                    }
                    else
                    {
                        return modified;

                    }
                }
            }
            catch (Exception ex)
            {
                return modified;
            }
        }


        public int insertShopChekinIn(ShopCheckin model)
        {
            int modified = 0;
            try
            {
                using (SqlCommand cmd = new SqlCommand("INSERT INTO shop_check_in(sc_date,sc_check_in_time,sc_userid,sc_shop_id,sc_latitude_in,sc_longitude_in,sc_check_in,sc_route_checkin_id) output INSERTED.sc_id VALUES(@sc_date,@sc_check_in_time,@sc_userid,@sc_shop_id,@sc_latitude_in,@sc_longitude_in,@sc_check_in,@sc_route_checkin_id)", shop))
                {
                    if (OpenConnection())
                    {
                        cmd.Parameters.AddWithValue("@sc_date", model.date);
                        cmd.Parameters.AddWithValue("@sc_check_in_time", model.time);
                        cmd.Parameters.AddWithValue("@sc_userid", this.userId);
                        cmd.Parameters.AddWithValue("@sc_shop_id", model.shopId);
                        cmd.Parameters.AddWithValue("@sc_latitude_in", model.latitude);
                        cmd.Parameters.AddWithValue("@sc_longitude_in", model.longitude);
                        cmd.Parameters.AddWithValue("@sc_check_in", 1);
                        cmd.Parameters.AddWithValue("@sc_route_checkin_id", model.routeCheckInId);
                        modified = (int)cmd.ExecuteScalar();
                        return modified;

                    }
                    else
                    {
                        return modified;

                    }
                }
            }
            catch (Exception ex)
            {
                return modified;
            }
        }

        //public int getSalesmanTrackingById(int id)
        //{
        //    int tres = -6;
        //    if (OpenConnection())
        //    {
        //        try
        //        {
        //            SqlCommand cmd = new SqlCommand();
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.CommandText = "Sp_AndroidSalesMan";

        //            SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
        //            parm.Direction = ParameterDirection.Output;

        //            cmd.Parameters.Add(parm);
        //            cmd.Parameters.AddWithValue("@as_id", id);
        //            cmd.Parameters.AddWithValue("@StatementType", "SalesManTrackById");
        //            cmd.Connection = shop;
        //            SqlDataReader dr = cmd.ExecuteReader();
        //            dr.Dispose();
        //            dr.Close();
        //            tres = Convert.ToInt32(parm.Value);
        //            //tres = 1;
        //        }
        //        catch (Exception m)
        //        {
        //            Console.WriteLine(m.ToString());
        //        }
        //    }
        //    return tres;
        //}

        //public DataTable getSalesmanTrackingById(string qry)
        //{
        //    if (OpenConnection())
        //    {
        //        DataTable _temp = new DataTable();
        //        SqlDataAdapter da = new SqlDataAdapter(qry, shop);
        //        da.SelectCommand.CommandTimeout = 0;
        //        da.Fill(_temp);
        //        return _temp;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        //public DataTable getSalesmanTrackingById(int id)
        //{
        //    try
        //    {
        //        SqlCommand cmd = new SqlCommand("Sp_AndroidSalesMan", shop);
        //        cmd.Parameters.AddWithValue("@as_id", id);
        //        cmd.Parameters.AddWithValue("@StatementType", "SalesManTrackById");
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        DataTable _temp = new DataTable();
        //        SqlDataAdapter adp = new SqlDataAdapter(cmd);
        //        adp.Fill(_temp);
        //        return _temp;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }

        //}
        public DataSet getSalesmanTrackingById(int id)
        {
            try
            {
                DataSet dataset = new DataSet();
                SqlCommand cmd = new SqlCommand("Sp_AndroidSalesMan", shop);
                cmd.Parameters.AddWithValue("@as_id", id);
                cmd.Parameters.AddWithValue("@StatementType", "SalesManTrackById");
                cmd.CommandType = CommandType.StoredProcedure;
                DataTable _temp = new DataTable();
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dataset);
                return dataset;
            }
            catch (Exception ex)
            {
                ex.ToString();
                return null;
            }
        }

        public DataSet getCheckinRoute(CheckinRouteModel model, int SALESMANWISECUSTOMER)
        {
            try
            {
                DataSet dataset = new DataSet();
                SqlCommand cmd = new SqlCommand("Sp_AndroidSalesMan", shop);
                cmd.Parameters.AddWithValue("@as_id", Convert.ToInt32(userId));
                cmd.Parameters.AddWithValue("@as_date", model.date);
                cmd.Parameters.AddWithValue("@cc_salesman_id", Convert.ToInt32(gu_acc_id));
                cmd.Parameters.AddWithValue("@as_salesmanwiseCustomer", SALESMANWISECUSTOMER);
                cmd.Parameters.AddWithValue("@StatementType", "getCheckinRoute");

                cmd.CommandType = CommandType.StoredProcedure;
                DataTable _temp = new DataTable();
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dataset);
                return dataset;
            }
            catch (Exception ex)
            {
                ex.ToString();
                return null;
            }
        }

        public DataSet getsSalesTrackingHistory(LedgerModel model)
        {
            try
            {
                DataSet dataset = new DataSet();
                SqlCommand cmd = new SqlCommand("Sp_AndroidSalesMan", shop);
                cmd.Parameters.AddWithValue("@as_id", model.asId);
                cmd.Parameters.AddWithValue("@StatementType", "SalesTrackingHistory");
                cmd.CommandType = CommandType.StoredProcedure;
                DataTable _temp = new DataTable();
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dataset);
                return dataset;
            }
            catch (Exception ex)
            {
                ex.ToString();
                return null;
            }
        }

        public DataSet salesmanTrackingSingle(int id)
        {

            try
            {
                DataSet dataset = new DataSet();
                SqlCommand cmd = new SqlCommand("Sp_AndroidSalesMan", shop);
                cmd.Parameters.AddWithValue("@as_id", id);
                cmd.Parameters.AddWithValue("@StatementType", "SalesmanTrackingSingle");
                cmd.CommandType = CommandType.StoredProcedure;
                DataTable _temp = new DataTable();
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dataset);
                return dataset;
            }
            catch (Exception ex)
            {
                ex.ToString();
                return null;
            }


        }

        public DataTable getReportColumns(string param)
        {
            string sql = "SELECT * from gnl_report_m_col where grc_m_name='" + param + "'";
            DataTable dt = dbReaderFill(sql);
            return dt;

        }

        public bool get_android_settings(string param)
        {
            if (OpenConnection())
            {
                string sql = "SELECT * from android_settings where ans_name='" + param + "' and ans_status = '1'";
                DataTable dt = dbReaderFill(sql);
                if (dt.Rows.Count > 0)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public void ensure_android_settings_columns()
        {
            string sql = @"
            IF COL_LENGTH('dbo.android_settings', 'ans_remark') IS NULL
                ALTER TABLE dbo.android_settings ADD ans_remark NVARCHAR(500) NULL;
            IF COL_LENGTH('dbo.android_settings', 'ans_used_by') IS NULL
                ALTER TABLE dbo.android_settings ADD ans_used_by NVARCHAR(20) NULL;";
            dbExecute(sql);
        }

        public void ensure_android_settings(string usedBy, params (string name, string remark)[] settings)
        {
            if (settings == null || settings.Length == 0)
                return;

            string side = string.IsNullOrWhiteSpace(usedBy) ? "BOTH" : usedBy.Trim().ToUpper();
            if (side != "BACKEND" && side != "FRONTEND" && side != "BOTH")
                side = "BOTH";

            foreach (var setting in settings)
            {
                if (string.IsNullOrWhiteSpace(setting.name))
                    continue;

                string name = setting.name.Replace("'", "''");
                string remark = (setting.remark ?? "").Replace("'", "''");
                string sql = @"
                IF NOT EXISTS (SELECT 1 FROM android_settings WHERE ans_name = '" + name + @"')
                BEGIN
                    INSERT INTO android_settings (ans_name, ans_status, ans_remark, ans_used_by)
                    VALUES ('" + name + @"', '0', '" + remark + @"', '" + side + @"')
                END
                ELSE
                BEGIN
                    UPDATE android_settings
                    SET ans_used_by = CASE
                            WHEN ans_used_by IS NULL OR LTRIM(RTRIM(ans_used_by)) = '' THEN '" + side + @"'
                            ELSE ans_used_by
                        END,
                        ans_remark = CASE
                            WHEN ans_remark IS NULL OR LTRIM(RTRIM(ans_remark)) = '' THEN '" + remark + @"'
                            ELSE ans_remark
                        END
                    WHERE ans_name = '" + name + @"'
                      AND (
                            ans_used_by IS NULL OR LTRIM(RTRIM(ans_used_by)) = ''
                         OR ans_remark IS NULL OR LTRIM(RTRIM(ans_remark)) = ''
                      )
                END";
                dbExecute(sql);
            }
        }

        public void ensure_all_android_settings()
        {
            ensure_android_settings_columns();

            // Used in API (backend) only — remark = backend purpose
            ensure_android_settings("BACKEND",
                ("AREAWISE LOGIN", "Filters salesman/customer lists by assigned area on login"),
                ("BaseUrl", "Base URL for image/file paths in ecommerce and delivery APIs"),
                ("brand_from_inv_barcode", "Resolves brand from inventory barcode in product/sales flows"),
                ("CUSTOMER SEARCH VISIT INDICATOR", "Adds visit indicator columns in customer search results"),
                ("DEFAULT LEND PRODUCT", "Default product id used when creating lend item transactions"),
                ("direct_banck_voucher", "Auto-approves bank payment vouchers on save"),
                ("DISABLE LOCATION BASED STOCK_IN PRODUCT LIST", "Skips location filter when loading product stock list"),
                ("ENABLE CUSTOMERWISE DISCPER", "Applies customer-specific discount % in product rate APIs"),
                ("ENABLE FORMATTED INVOICENO", "Builds location-prefixed formatted invoice numbers in sales"),
                ("ENABLE LEND", "Enables lend item transaction features in common APIs"),
                ("ENABLE LOCATIONWISE COMPANY ADDRESS", "Uses location company address on sales invoice PDF"),
                ("ENABLE SINGLE ROUTEWISE LOGIN", "Restricts user to a single assigned route at login"),
                ("ENABLE SUPNAME IN PRODUCT DETAILS", "Includes supplier name in product details response"),
                ("ENABLEAPPROVEOPTIONINPO", "Enables purchase-order approval flow before confirming PO"),
                ("ENABLEITEMSPLITFORDIFFBARCODEINPURCHASE", "Splits purchase lines when same item has different barcodes"),
                ("HIDE COMPANY DETAILS FROM RPV", "Hides company header details on receipt/payment voucher PDF"),
                ("HIDE COMPANY DETAILS FROM SALES-Q", "Hides company header details on sales quotation PDF"),
                ("INVOICE NAME ARABIC", "Uses Arabic invoice title/name on invoice PDF"),
                ("LOCATION WISE CUSTOMER", "Filters customers by logged-in user location"),
                ("LOCATION WISE PRODUCT", "Filters products/stock by logged-in user location"),
                ("NON ROUTE CUSTOMER", "Includes customers outside assigned routes in customer lists"),
                ("PRODUCT NAME ALIASCOLUMN", "Uses product alias column instead of / with product name"),
                ("PRODUCT NAME ALTERNATIVE", "Returns alternative product name field in product list APIs"),
                ("ROUTEWISE LOGIN", "Filters salesman/customer data by assigned route on login"),
                ("SALESMANRECIEPTAPPROVE", "Routes salesman receipts to verify table until admin approves"),
                ("SALESMANWISE_VOUCHER", "Filters vouchers by salesman account in RPV APIs"),
                ("SALESMANWISECUSTOMER", "Filters customers by assigned salesman"),
                ("SALESMANWISESALESREPORT", "Restricts sales report data to salesman scope"),
                ("SHOW SALESMAN CODE", "Includes salesman code in salesman list response"),
                ("SHOW SALESMAN WITH CODE", "Returns salesman display as name with code"),
                ("showavgstock", "Includes average stock values in stock-related queries"),
                ("USER WISE LEDGER REPORT", "Restricts ledger report to current user (non-admin)"),
                ("VALIDATE MACID", "Requires matching device MAC id during login"),
                ("WORK ORDER USERWISE", "Filters work orders by logged-in salesman/user"),
                ("USER WISE LAST SALES", "Filters last sale by user login wise")
            );

            // Used in both backend API and frontend/app
            ensure_android_settings("BOTH",
                ("ENABLE LOCATION IN SALES", "Applies location company address / location scope in sales invoice"),
                ("ENABLE MAX AND MIN RATE IN SALE", "Exposes and validates min/max sale rate in sales APIs"),
                ("ENABLE VEHICLE NUMBER FIELD IN LOYALTY", "Includes vehicle number field in loyalty/sales invoice data"),
                ("KSA INVOICE", "Enables KSA e-invoice / ZATCA related invoice handling"),
                ("LOCATION ENTRY NO", "Uses location-wise entry number on invoices"),
                ("SHOW STATEMENT REPORT SCREENSHOT", "Enables statement report screenshot data in sales APIs"),
                ("SHOWLASTRATEINSALE", "Returns last sale rate for product in sales/product APIs"),
                ("SALESMAN RECIEPT APPROVE", "Salesman receipt approval toggle used by app and API")
            );

            // Frontend / app settings (UI behaviour; not used in API business logic)
            ensure_android_settings("FRONTEND",
                ("ADMIN EDIT", "App: allow admin edit actions"),
                ("ADVANCED CUSTOMER SEARCH", "App: setting unlocks the Advanced Search switch; the switch chooses advanced vs normal customer lookup on sales and voucher screens."),
                ("BARCODE READ", "App: barcode scanner behaviour"),
                ("BILL EDIT", "App: allow bill edit"),
                ("BLUETOOTH PRINTER", "App: bluetooth printer support"),
                ("CHANGE CASH TYPE", "App: lock changing the sales rate type (MRP, Retail, WSale, etc.) when a default is already set for that sale type."),
                ("COMPANY NAME", "App: company display name"),
                ("CURRENCY CODE", "App: currency code display"),
                ("DISABLE CUSTOMERWISE ITEM RETURN", "App: disable customer-wise item return"),
                ("DISABLE DATE SELECTION IN SALES SCREENS", "App: lock date picker on sales screens"),
                ("DISABLE FETCHING PRODUCT DETAILS BASED ON LOCATION", "App: skip location-based product fetch"),
                ("DISABLE ROUND OFF", "App: disable round-off on totals"),
                ("DISABLE SHOWING STOCK IN PRODUCT LIST", "App: hide stock qty in product list"),
                ("DUPLICATE ITEM IN CART", "App: allow duplicate items in cart"),
                ("ENABLE ACCOUNT EDIT", "App: allow account edit"),
                ("ENABLE ANY KEY SEARCH", "App: search products/customers by any key"),
                ("ENABLE ARABIC THERMAL PRINT BY FULL IMAGE", "App: Arabic thermal print as full image"),
                ("ENABLE CURRENT HOLDING", "App: show current holding"),
                ("ENABLE CUSTOMER ADVANCE SEARCH", "App: customer advance search UI"),
                ("ENABLE CUSTOMER CREDIT LIMIT CHECKING", "App: block sale when credit limit exceeded"),
                ("ENABLE MINIMALIST SALES SCREEN", "App: minimalist sales screen layout"),
                ("ENABLE MULTIUNIT", "App: multi-unit selection in sales"),
                ("ENABLE MULTIUNIT SETTINGS IN SALES LINE ITEM", "App: multi-unit settings in line item"),
                ("ENABLE NEW ADMIN DASHBOARD", "App: new admin dashboard UI"),
                ("ENABLE PLUS TAX CALCULATION", "App: plus-tax calculation mode"),
                ("ENABLE PURCHASE AND ITEM REGISTRATION", "App: purchase and item registration screens"),
                ("ENABLE QUANTITY ONLY IN KIOSK", "App: qty-only mode in kiosk"),
                ("ENABLE QUOTATION WITHOUT INV NO", "App: quotation without invoice number"),
                ("ENABLE SALES TARGET AND USER WISE FINANACIAL STATEMENT", "App: sales target and user financial statement"),
                ("ENABLE SALES TARGET AND USER WISE FINANCIAL STATEMENT", "App: sales target and user financial statement"),
                ("ENABLE SALESMAN TRACKING", "App: salesman GPS/location tracking"),
                ("HIDE MRP IN BATCH DETAILS", "App: hide MRP in batch details"),
                ("HIDE MRP IN SALES BATCH DETAILS SCREEN", "App: hide MRP in sales batch screen"),
                ("HIDE P-RATE IN PRODUCT SEARCH", "App: hide purchase rate in product search"),
                ("HIDE PRATE COST SRATE IN KIOSK REPORT", "App: hide P-rate/cost/S-rate in kiosk report"),
                ("HIDE_COST", "App: hide cost column"),
                ("IS KSA PRINT ENABLED", "App: KSA print format"),
                ("LAST CUSTOMER RATE", "App: show last customer rate"),
                ("LEDGER REPORT FROM DATE PRESET", "App: ledger report from-date preset"),
                ("LOCATION WISE ENTRY ENABLED", "App: location-wise entry UI"),
                ("LOCK SALESMAN IN SALES", "App: lock salesman field in sales"),
                ("MIN MAX RATE IN SALE", "App: min/max rate UI in sale"),
                ("PDF WITH LOGO", "App: PDF invoice with logo"),
                ("RATE EDIT", "App: allow rate edit in sales"),
                ("REMOVE OLD AND NET BALANCE FROM THE SALES INVOICE PDF", "App: hide old/net balance on sales PDF"),
                ("SALES ESTIMATE INVOICE WITH LOGO", "App: sales estimate PDF with logo"),
                ("SALES RATE LOCK IN SALES LINE ITEM SCREEN", "App: lock sales rate in line item"),
                ("SAMPLE PDF 1", "App: sample PDF template 1"),
                ("SHOW LEND COLLECTION", "App: show lend collection UI"),
                ("SHOW LOYALTY IN SALES", "App: show loyalty section in sales"),
                ("SHOW LOYALTY IN SALES (pref)", "App: loyalty in sales preference"),
                ("SHOW TRAY SECTION IN INVOICE PDF", "App: tray section on invoice PDF"),
                ("SLAES ESTIMATE INVOICE WITH LOGO", "App: sales estimate invoice with logo"),
                ("USER EDIT", "App: allows non-admin users to access the save/edit sales flow. used in sales screen which allow users to edit"),
                ("ZERO RATE SALE", "App: allow zero-rate sale")
            );
        }
        public bool get_gnl_settings(string param)
        {
            if (OpenConnection())
            {
                string sql = "SELECT * from gnl_settings where gs_value='" + param + "' and gs_status = 1";
                DataTable dt = dbReaderFill(sql);
                if (dt.Rows.Count > 0)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        public DataSet balanceSheetFillDataSet(string StatementType, BalanceSheetModel bsModel)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("Sp_acc_balance_sheet", shop);
                cmd.Parameters.AddWithValue("@wi_from_date", bsModel.frmDate);
                cmd.Parameters.AddWithValue("@wi_to_date", bsModel.toDate);
                cmd.Parameters.AddWithValue("@StatementType", StatementType);
                cmd.CommandType = CommandType.StoredProcedure;
                DataSet _temp = new DataSet();
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(_temp);
                return _temp;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public bool savePermission(string StatementType, PermissionModel bsModel)
        {
            try
            {
                DataTable dt1 = new DataTable();
                dt1.Columns.Add("m_id");
                dt1.Columns.Add("m_name");
                dt1.Columns.Add("m_main_level");
                dt1.Columns.Add("m_codename");
                dt1.Columns.Add("m_parent_id");
                dt1.Columns.Add("m_main_id");
                dt1.Columns.Add("m_orderby");
                dt1.Columns.Add("m_gm_id");
                dt1.Columns.Add("m_entry_status");
                if (bsModel.permission != null)
                {
                    foreach (MenuPermissions myitems in bsModel.permission)
                    {
                        DataRow dRow = dt1.NewRow();
                        dRow[0] = myitems.permissionId.ToString();
                        dRow[1] = "";
                        dRow[2] = myitems.ap_other_status.ToString();
                        dRow[3] = "";
                        dRow[4] = 0;
                        dRow[5] = 0;
                        dRow[6] = 0;
                        dRow[7] = 0;
                        dRow[8] = 0;
                        dt1.Rows.Add(dRow);
                    }
                }

                SqlCommand cmd = new SqlCommand("Sp_AndroidSalesMan", shop);
                cmd.Parameters.AddWithValue("@permission_menu", dt1);
                cmd.Parameters.AddWithValue("@as_user_id", bsModel.userId);
                cmd.Parameters.AddWithValue("@StatementType", StatementType);
                cmd.CommandType = CommandType.StoredProcedure;
                DataSet _temp = new DataSet();
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(_temp);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public void savecheckin(string date, int as_id)
        {
            bool CUSTOMERSEARCHVISITINDICATOR = false;
            if (get_android_settings("CUSTOMER SEARCH VISIT INDICATOR"))
            {
                CUSTOMERSEARCHVISITINDICATOR = true;
            }
            if (CUSTOMERSEARCHVISITINDICATOR)
            {
                string area = areaId;
                string[] areaList = areaId.Split(",");
                if(areaList.Length>1)
                {
                    DataTable dt_area = dbReaderFill("select as_area_id from acc_subhead where as_id=" + as_id + "");
                    if (dt_area.Rows.Count > 0)
                        area = dt_area.Rows[0]["as_area_id"].ToString();
                }
                string sql = "insert into shop_check_in (sc_date,sc_shop_id,sc_userid,sc_route_id,sc_location_id) values('" + date + "', " + as_id + "," + gu_acc_id + "," + area + "," + locationId + ")";
                dbReaderFill(sql);
            }

        }

        public DataTable stmtSave(string statement, SaleModel model)
        {
            //DataTable dt_ = dbReaderFill("select gs_status from gnl_settings where gs_value = 'ENABLESOFTWAREASWORKSHOP'");
            //int workOrderStatus = Convert.ToInt32(dt_.Rows[0][0].ToString());
            //DataTable woStatus = new DataTable();
            //woStatus.Columns.Add("oldDeliveryStatus");
            //woStatus.Columns.Add("oldWoStatus");
            //woStatus.Columns.Add("newDeliveryStatus");
            //woStatus.Columns.Add("newWoStatus");
            //woStatus.Rows.Add();
            //int entryNumber = model.entryNo;
            //if(workOrderStatus==1)
            //{
            //    if(model.statement=="Update")
            //    {
            //        DataTable statusId = dbReaderFill("select si_finish,si_delivered from inv_sales_inf where si_entryno=" + model.entryNo +"");
            //        woStatus.Rows[0]["oldDeliveryStatus"] = statusId.Rows[0]["si_delivered"].ToString();
            //        woStatus.Rows[0]["oldWoStatus"] = statusId.Rows[0]["si_finish"].ToString();
            //    }
            //}
            bool ENABLEEINVOICEKSA = false;
            string _print = null;
            DataTable dt_settings = dbReaderFill("select gs_status from gnl_settings where gs_value = 'ENABLEEINVOICEKSA'");
            ENABLEEINVOICEKSA = Convert.ToBoolean(Convert.ToInt32(dt_settings.Rows[0][0].ToString()));
            if (ENABLEEINVOICEKSA)
            {
                DataTable dtcom = dbReaderFill("select com_name,com_gstin from gnl_company ");
                int _lencomname = 0;
                int _lenvatno = 0;
                int _lenamount = 0;
                int _lenvat = 0;
                string _comname = dtcom.Rows[0]["com_name"].ToString();
                string _vatno = dtcom.Rows[0]["com_gstin"].ToString();
                string _date = model.date.Substring(0, 10);
                string _time = model.date.Substring(11, 8);
                _date = _date + "T" + _time;
                _lencomname = _comname.Length;
                _lenvatno = _vatno.Length;
                _lenamount = model.grandTotal.Length;
                _lenvat = model.taxAmount.ToString().Length;

                string _barcode = Convert.ToChar(1).ToString() + Convert.ToChar(_lencomname).ToString()
                    + _comname + Convert.ToChar(2).ToString() + Convert.ToChar(_lenvatno).ToString()
                    + _vatno + Convert.ToChar(3).ToString() + Convert.ToChar(19).ToString()
                    + _date + Convert.ToChar(4).ToString()
                    + Convert.ToChar(_lenamount).ToString() + model.grandTotal + Convert.ToChar(5).ToString()
                    + Convert.ToChar(_lenvat).ToString() + model.taxAmount.ToString();

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(_barcode);
                _print = System.Convert.ToBase64String(plainTextBytes);
            }
            DataTable dt1 = new DataTable();
            dt1.Columns.Add("qty"); //0
            dt1.Columns.Add("fqty"); //1
            dt1.Columns.Add("s_rate"); //2
            dt1.Columns.Add("real_rate"); //3
            dt1.Columns.Add("gross"); //4
            dt1.Columns.Add("disc_p"); //5
            dt1.Columns.Add("disc"); //6
            dt1.Columns.Add("real_disc"); //7
            dt1.Columns.Add("net"); //8
            dt1.Columns.Add("tax"); // 9
            dt1.Columns.Add("cgst"); //10
            dt1.Columns.Add("sgst"); //11
            dt1.Columns.Add("igst"); //12
            dt1.Columns.Add("total"); //13
            dt1.Columns.Add("pt"); //14
            dt1.Columns.Add("uniquecode"); //15
            dt1.Columns.Add("ir_id"); //16
            dt1.Columns.Add("mrp"); //17
            dt1.Columns.Add("qty_multi_unit"); //18
            dt1.Columns.Add("unit_multi"); //19
            dt1.Columns.Add("srate_multiunit"); //20
            dt1.Columns.Add("narration"); //21
            dt1.Columns.Add("cess"); //22
            dt1.Columns.Add("ad_cess"); //23
            dt1.Columns.Add("kfc"); //24
            dt1.Columns.Add("support"); //25
            dt1.Columns.Add("real_support"); //26
            dt1.Columns.Add("salesman"); //27
            dt1.Columns.Add("sp_prate"); //28
            dt1.Columns.Add("sp_realprate", typeof(decimal)); //29
            dt1.Columns.Add("sp_cost"); //30
            dt1.Columns.Add("sp_local_exp"); //31
            dt1.Columns.Add("sp_lend_amount"); //32
            dt1.Columns.Add("sp_discp2"); //33
            dt1.Columns.Add("sp_disc2"); //34
            dt1.Columns.Add("sp_netratesingle"); //35
            dt1.Columns.Add("sp_narration1"); //36
            dt1.Columns.Add("sp_other_exp"); //37
            dt1.Columns.Add("sp_print"); //38
            dt1.Columns.Add("sp_card_disc"); //39
            dt1.Columns.Add("sp_depreciation_per");//40
            dt1.Columns.Add("sp_depreciation");//41
            dt1.Columns.Add("sp_narration2");//42
            dt1.Columns.Add("sp_narration3");//43
            bool hasSpRowId = Convert.ToInt32(dbScalar(@"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME='inv_sales_par'
            AND COLUMN_NAME='sp_row_id'")) > 0;

            if (hasSpRowId)
            {
                dt1.Columns.Add("sp_row_id");
            }
            DataTable dt2 = new DataTable();
            dt2.Columns.Add("li_in");
            dt2.Columns.Add("li_out");
            dt2.Columns.Add("li_remarks");
            dt2.Columns.Add("li_ir_id");
            dt2.Columns.Add("li_ift_id");
            dt2.Columns.Add("li_ir_mrp");

            if (model.complaint != null)
            {
                foreach (Complaints mycomplaints in model.complaint)
                {
                    DataRow dRow = dt2.NewRow();
                    dRow[0] = mycomplaints.li_in;
                    dRow[1] = mycomplaints.li_out;
                    dRow[2] = mycomplaints.remarks;
                    dRow[3] = mycomplaints.complaintId;
                    dRow[4] = mycomplaints.fixTypeId;
                    dRow[5] = mycomplaints.amount;
                    dt2.Rows.Add(dRow);
                }
            }
            //double _sumqtyT = 0;
            //double _sumgrossT = 0;
            double _sumdiscT = 0;
            //double _sumnetT = 0;
            double _sumtaxT = 0;
            //double _sumcgstT = 0;
            //double _sumsgstT = 0;
            //double _sumigstT = 0;
            double _sumtotalT = 0;


            double _otherexp = 0;
            double _otherexpperone = 0;

            _otherexp = 0 - model.totalDiscount + model.freightCharge;
            double _totalsum = model.net;

            if (_totalsum != 0)
                _otherexpperone = _otherexp / _totalsum;
            int slNo = 1;
            if (model.items != null)
            {
                foreach (SalesProducts myitems in model.items)
                {
                    DataRow dRow = dt1.NewRow();
                    dRow[0] = myitems.psp_qty;
                    //_sumqtyT = _sumqtyT + myitems.psp_qty;
                    dRow[1] = 0;
                    dRow[2] = myitems.psp_rate;
                    dRow[3] = myitems.psp_realrate;
                    dRow[4] = myitems.gross;
                    //_sumgrossT = _sumgrossT + myitems.gross;
                    dRow[5] = myitems.psp_disc_per;
                    dRow[6] = myitems.psp_disc;
                    _sumdiscT = _sumdiscT + myitems.psp_disc;
                    dRow[7] = myitems.psp_real_disc;
                    dRow[8] = myitems.net;
                    //_sumnetT = _sumnetT + myitems.net;
                    if (model.sistrId != 2)
                    {
                        dRow[9] = myitems.tax;
                        _sumtaxT = _sumtaxT + myitems.tax;
                        dRow[10] = myitems.cgst;
                        //_sumcgstT = _sumcgstT + myitems.cgst;
                        dRow[11] = myitems.sgst;
                        //_sumsgstT = _sumsgstT + myitems.sgst;
                    }
                    else
                    {
                        dRow[9] = "0";
                        dRow[10] = "0";
                        dRow[11] = "0";
                    }

                    dRow[12] = 0;
                    //_sumigstT = _sumigstT + 0;
                    dRow[13] = myitems.psp_total;
                    _sumtotalT = _sumtotalT + myitems.psp_total;
                    dRow[14] = myitems.psp_profit;
                    int _ir_id = myitems.psp_ir_id;

                    dRow[15] = myitems.psp_uniquecode;
                    dRow[16] = _ir_id;
                    dRow[17] = myitems.psp_mrp;
                    dRow[18] = myitems.psp_qty_multi_unit;
                    dRow[19] = myitems.psp_unit_multi;
                    dRow[20] = myitems.psp_srate_multiunit;
                    dRow[21] = myitems.sp_narration;
                    dRow[22] = 0;
                    dRow[23] = 0;
                    dRow[24] = myitems.kfc;
                    dRow[25] = 0;
                    dRow[26] = 0;
                    dRow[27] = "-1";
                    dRow[28] = myitems.sp_prate;
                    dRow[29] = Convert.ToDecimal(myitems.realprate);
                    dRow[30] = myitems.sp_cost;
                    dRow[31] = myitems.sp_local_exp;
                    dRow[32] = myitems.sp_lend_amount;
                    dRow[33] = "";
                    dRow[34] = "0";
                    dRow[35] = myitems.net / myitems.psp_qty;
                    dRow[36] = myitems.sp_narration1;
                    dRow[37] = myitems.net * _otherexpperone;
                    dRow[38] = "0";
                    dRow[39] = 0;
                    dRow[40] = 0;
                    dRow[41] = 0;
                    dRow[42] = "";
                    dRow[43] = "";
                    if (hasSpRowId)
                    {
                        dRow["sp_row_id"] = slNo;
                        slNo++;
                    }
                    dt1.Rows.Add(dRow);

                }
            }

            model.si_einvoice_ksa = _print;
            int tres = 0;
            DataTable _values = new DataTable();
            _values.Columns.Add("tres");
            _values.Columns.Add("einvoiceksa");
            _values.Rows.Add();

            if (OpenConnection())
            {

                string as_mob = "";
                string query = "";
                DataTable dt;

                decimal loyality_added = 0;
                if (model.customerId > 0)
                {
                    DataTable dt_loyality = new DataTable();
                    dt_loyality = dbSelectLoyalityPoint(dt1);
                    if (dt_loyality.Rows.Count > 0)
                    {
                        loyality_added = System.Math.Round(CommonHelper.getRoudedValue(dt_loyality.Rows[0][0].ToString()), CommonHelper.Decimalpoint);
                    }

                    //get customer details starts//
                    query = "select as_mob from acc_subhead where as_id = " + model.customerId + "";
                    dt = dbReaderFill(query);
                    if (dt != null)
                    {
                        if (dt.Rows.Count > 0)
                            as_mob = dt.Rows[0]["as_mob"].ToString();
                    }

                    //get customer details ends//
                }
                bool ENABLELOCATION = false;
                int si_location_Id = 0;
              
                if (get_android_settings("ENABLE LOCATION IN SALES"))
                {
                    si_location_Id = model.si_location_id;
                }
                else
                {
                    si_location_Id = Convert.ToInt32(locationId);
                }

                try
                {
                    DataSet dataset = new DataSet();

                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_Sale";
                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm);

                    cmd.Parameters.AddWithValue("@si_balance", model.balance);
                    cmd.Parameters.AddWithValue("@si_user_id", Convert.ToInt32(userId));
                    cmd.Parameters.AddWithValue("@si_grand_total", model.grandTotal);
                    cmd.Parameters.AddWithValue("@si_igst_total", 0);
                    if (model.sistrId != 2)
                    {
                        cmd.Parameters.AddWithValue("@si_cgst_total", model.cgst);
                        cmd.Parameters.AddWithValue("@si_sgst_total", model.sgst);
                        cmd.Parameters.AddWithValue("@si_tax", _sumtaxT);
                        cmd.Parameters.AddWithValue("@si_tax_type", 1);
                    }

                    cmd.Parameters.AddWithValue("@si_commision_acc_id", model.si_commision_acc_id);
                    cmd.Parameters.AddWithValue("@si_freight_charge", model.freightCharge);
                    cmd.Parameters.AddWithValue("@si_cash_paid_acc", model.cashId);
                    cmd.Parameters.AddWithValue("@si_salesman_id", model.salesManId);
                    cmd.Parameters.AddWithValue("@si_remarks", model.remark);
                    cmd.Parameters.AddWithValue("@si_cash_recieved", model.cashReceived);

                    cmd.Parameters.AddWithValue("@si_total", model.total);
                    cmd.Parameters.AddWithValue("@si_profit", model.profit);

                    cmd.Parameters.AddWithValue("@si_other_disc", model.totalDiscount);
                    cmd.Parameters.AddWithValue("@si_other_charge", model.otherCharge);
                    cmd.Parameters.AddWithValue("@si_disc_per", model.totalDiscountPer);
                    cmd.Parameters.AddWithValue("@si_disc", _sumdiscT);
                    cmd.Parameters.AddWithValue("@si_gross_value", model.gross);
                    cmd.Parameters.AddWithValue("@si_rate_type", model.salesRateType);
                    cmd.Parameters.AddWithValue("@si_alias_name", model.aliasName);
                    cmd.Parameters.AddWithValue("@si_add2", model.address2);
                    cmd.Parameters.AddWithValue("@si_ob", model.oldBalance);
                    cmd.Parameters.AddWithValue("@si_net_balance", model.netBalance);
                    cmd.Parameters.AddWithValue("@si_loyality_amount", loyality_added);
                    cmd.Parameters.AddWithValue("@si_lc_id", model.lc_id);
                    cmd.Parameters.AddWithValue("@si_add1", model.address1);
                    cmd.Parameters.AddWithValue("@si_net_amount", model.net);
                    cmd.Parameters.AddWithValue("@si_location_id", si_location_Id);
                    cmd.Parameters.AddWithValue("@si_str_id", model.sistrId);
                    cmd.Parameters.AddWithValue("@si_entryno", Convert.ToInt32(model.entryNo));
                    cmd.Parameters.AddWithValue("@si_date", model.date);
                    cmd.Parameters.AddWithValue("@si_update_date", model.currentDate);
                    cmd.Parameters.AddWithValue("@si_acc_id", model.customerId);
                    cmd.Parameters.AddWithValue("@si_cust_name", model.custName);
                    cmd.Parameters.AddWithValue("@si_sales_acc_id", 2);
                    cmd.Parameters.AddWithValue("@si_lend_add", model.lendAdd);
                    cmd.Parameters.AddWithValue("@si_other_works", model.si_other_works); // chair in restuarent
                    cmd.Parameters.AddWithValue("@si_kot_status", model.si_kot_status);
                    cmd.Parameters.AddWithValue("@si_destination", model.si_destination);
                    cmd.Parameters.AddWithValue("@si_bankacc", model.si_bankacc);
                    cmd.Parameters.AddWithValue("@si_card_amount", model.si_card_amount);
                    cmd.Parameters.AddWithValue("@si_from_mobile", 1);
                    cmd.Parameters.AddWithValue("@si_batteryno", model.si_batteryno);
                    cmd.Parameters.AddWithValue("@si_einvoice_ksa", model.si_einvoice_ksa);
                    cmd.Parameters.AddWithValue("@si_company", model.si_company);
                    cmd.Parameters.AddWithValue("@si_model", model.si_model);
                    cmd.Parameters.AddWithValue("@si_imei", model.si_imei);
                    //DataTable dt_ = dbReaderFill("select gs_status from gnl_settings where gs_value = 'ENABLESOFTWAREASWORKSHOP'");
                    //int _gs_status = Convert.ToInt32(dt_.Rows[0][0].ToString());
                    //if (workOrderStatus == 1)
                    //{
                    //    if (model.itemcollected != null)
                    //    {

                    //        cmd.Parameters.AddWithValue("@si_sim", model.itemcollected.Contains("Sim") ? 1 : 0);
                    //        cmd.Parameters.AddWithValue("@si_charger", model.itemcollected.Contains("Charger") ? 1 : 0);
                    //        cmd.Parameters.AddWithValue("@si_battery", model.itemcollected.Contains("Battery") ? 1 : 0);
                    //        cmd.Parameters.AddWithValue("@si_pouch", model.itemcollected.Contains("Pouch") ? 1 : 0);
                    //        cmd.Parameters.AddWithValue("@si_other", model.itemcollected.Contains("Other") ? 1 : 0);

                    //    }
                    //}
                    //else
                    //{
                    //    cmd.Parameters.AddWithValue("@si_sim", model.si_sim);
                    //}
                    cmd.Parameters.AddWithValue("@si_items_collected", model.itemcollected);
                    cmd.Parameters.AddWithValue("@si_sim", model.si_sim);
                    cmd.Parameters.AddWithValue("@si_signature", model.si_signature);
                    cmd.Parameters.AddWithValue("@si_expected_date", model.si_expected_date);
                    cmd.Parameters.AddWithValue("@si_rc_id", model.si_rc_id);
                    cmd.Parameters.AddWithValue("@si_work_priority", model.si_work_priority);
                    cmd.Parameters.AddWithValue("@si_color", model.si_color);
                    cmd.Parameters.AddWithValue("@si_due_date", model.si_due_date);
                    cmd.Parameters.AddWithValue("@si_finish", model.si_finish.ToString());
                    cmd.Parameters.AddWithValue("@si_delivered", model.si_delivered);
                    cmd.Parameters.AddWithValue("@si_finished_date", model.si_finished_date);
                    cmd.Parameters.AddWithValue("@woh_salesman_id", model.assignedTo);
                    cmd.Parameters.AddWithValue("@si_assign_to", model.assignedTo);
                    cmd.Parameters.AddWithValue("@si_disp_name", model.billingName);
                    cmd.Parameters.AddWithValue("@si_disp_location", model.billingLocation);
                    cmd.Parameters.AddWithValue("@si_disp_add1", model.billingAddress);
                    cmd.Parameters.AddWithValue("@si_disp_scode", model.billingScode);
                    cmd.Parameters.AddWithValue("@si_disp_pincode", model.billingPincode);
                    cmd.Parameters.AddWithValue("@si_ship_name", model.shippingName);
                    cmd.Parameters.AddWithValue("@si_ship_location", model.shippingLocation);
                    cmd.Parameters.AddWithValue("@si_ship_add1", model.shippingAddress);
                    cmd.Parameters.AddWithValue("@si_ship_scode", model.shippingScode);
                    cmd.Parameters.AddWithValue("@si_ship_pincode", model.shippingPincode);
                    cmd.Parameters.AddWithValue("@si_ship_gstin", model.shippingGstin);
                    cmd.Parameters.AddWithValue("@type1", dt1);
                    cmd.Parameters.AddWithValue("@type2", dt2);
                    cmd.Parameters.AddWithValue("@StatementType", statement);
                    cmd.CommandTimeout = 60;
                    cmd.Connection = shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();
                    tres = Convert.ToInt32(parm.Value);
                    //return tres;
                    if (tres > 1)
                    { 
                        try
                        {
                            if (!get_gnl_settings("ENABLEINVOICEWHATSAPPSEND") && (get_gnl_settings("ENABLEWHATSAPPMSG") && (model.sistrId == 1 || model.sistrId == 2)))
                            {
                                string _msg = "";
                                string _mob = "";
                                DataTable dt_msg = new DataTable();
                                DataTable dt_point = new DataTable();
                                dt_msg = dbReaderFill("select wh_sales from gnl_whatsapp_settings");
                                dt_point = dbReaderFill("select COALESCE(sum(lct_in),0)-COALESCE(sum(lct_out),0) from acc_loyalty_card_transactions inner join acc_loyalty_card on lct_lc_id = lc_id where lc_mob = '" + model.address1 + "'");
                                _msg = (dt_msg.Rows[0]["wh_sales"].ToString());
                                _msg = _msg.Replace("_name", model.custName);
                                _msg = _msg.Replace("_amt", model.grandTotal.ToString());
                                _msg = _msg.Replace("_point", Convert.ToInt32(GetTextboxValue(dt_point.Rows[0][0].ToString())).ToString());
                                _msg = _msg.Replace("_ob", model.oldBalance);
                                _msg = _msg.Replace("_netbalance", model.netBalance.ToString());
                                _msg = _msg.Replace("_cashreceived", model.cashReceived.ToString());
                                _msg = _msg.Replace("_bankamount", model.si_card_amount.ToString());

                                if (model.address1 != "")
                                {

                                    dbReaderFill("INSERT INTO gnl_whatsapp_sync(asy_wh_msg,asy_wh_mob,asy_wh_name,asy_wh_status,asy_wh_form,asy_wh_entryno)VALUES('" + _msg + "','" + model.address1 + "','" + model.custName + "',0,'" + model.sistrId + "'," + tres + ")");
                                }
                            }
                            if (get_gnl_settings("ENABLECURRENTHOLDING") && (model.sistrId == 1 || model.sistrId ==2 || model.sistrId==6))
                            {
                                foreach (var item in model.HoldingItems)
                                {
                                    if (model.statement == "Insert")
                                    {
                                        //string checkQuery = $@"SELECT COUNT(*) FROM inv_current_holding WHERE ch_as_id = {model.customerId} and ch_ir_id = {item.IrId}";
                                        string checkQuery = $@"
    SELECT COUNT(*) 
    FROM inv_current_holding 
    WHERE ch_as_id = {model.customerId} 
    AND ch_ir_id = {item.IrId}
    AND ch_location_id = {model.si_location_id}";
                                        DataTable dt_check = dbReaderFill(checkQuery);
                                        int recordExists = 0;
                                        if (dt_check.Rows.Count > 0)
                                        {
                                            recordExists = Convert.ToInt32(dt_check.Rows[0][0]);
                                        }
                                        if (recordExists > 0)
                                        {
                                            query = $@"
    UPDATE inv_current_holding 
    SET ch_entryno = {model.entryNo},
        ch_qty = {item.HoldQty},
        ch_date = '{model.date:yyyy-MM-dd}',
        ch_form = (SELECT str_name 
                   FROM inv_sales_type_reg 
                   WHERE str_id = {model.sistrId})
    WHERE ch_as_id = {model.customerId} 
    AND ch_ir_id = {item.IrId}
    AND ch_location_id = {model.si_location_id}";
                                        }
                                        else 
                                        {
                                            query = $@"INSERT INTO inv_current_holding
                                            (ch_date, ch_as_id, ch_entryno, ch_form, ch_qty, ch_ir_id, ch_location_id)
                                            VALUES
                                            ('{model.date:yyyy-MM-dd}',
                                             {model.customerId},
                                             {model.entryNo},
                                             (SELECT str_name FROM inv_sales_type_reg WHERE str_id = {model.sistrId}),
                                             {item.HoldQty},
                                             {item.IrId},
                                             {model.si_location_id})";
                                        }
                                    }                                    

                                    dbExecute(query);
                                }
                            }


                        }
                        catch (Exception ex) { }
                        savecheckin(model.date, model.customerId);
                    }
                    _values.Rows[0]["tres"] = tres;
                    _values.Rows[0]["einvoiceksa"] = model.si_einvoice_ksa;

                    //if (workOrderStatus == 1)
                    //{
                    //    string insertQuery = "";
                    //    DataTable dt_in = new DataTable();
                    //    if (tres > 0 && model.statement == "Insert")
                    //    {

                    //        insertQuery = "insert into inv_workorder_history(woh_date,woh_entryno,woh_ws_id,woh_delivery_status,woh_action,woh_user_id)values('" + model.date + "'," + tres + "," + model.si_finish + ",'" + model.si_delivered + "','" + model.statement + "'," + userId + ")";
                    //        dbReaderFill(insertQuery);
                    //    }
                    //    if (tres>0 && model.statement == "Update")
                    //    {
                    //        DataTable statusId = dbReaderFill("select top 1 si_finish,si_delivered from inv_sales_inf where si_entryno=" + model.entryNo + "");
                    //        woStatus.Rows[0]["newDeliveryStatus"] = statusId.Rows[0]["si_delivered"].ToString();
                    //        woStatus.Rows[0]["newWoStatus"] = statusId.Rows[0]["si_finish"].ToString();
                    //        int oldfinish = Convert.ToInt32(woStatus.Rows[0]["oldWoStatus"].ToString());
                    //        int newfinish = Convert.ToInt32(woStatus.Rows[0]["newWoStatus"].ToString());
                    //        if ((woStatus.Rows[0]["oldDeliveryStatus"].ToString()!= woStatus.Rows[0]["newDeliveryStatus"].ToString())||oldfinish!=newfinish)
                    //        {
                    //            insertQuery = "insert into inv_workorder_history(woh_date,woh_entryno,woh_ws_id,woh_delivery_status,woh_action,woh_user_id,woh_salesman_id)values('" + model.currentDate + "'," + model.entryNo + "," + model.si_finish + ",'" + model.si_delivered + "','" + model.statement + "'," + userId +","+model.assignedTo+ ")";
                    //            dbReaderFill(insertQuery);
                    //        }
                    //    }
                    //}
                    return _values;

                }
                catch (Exception ex)
                {
                    ex.ToString();
                    tres = 0;
                }
            }
            // return tres;
            return _values;
        }
        decimal GetTextboxValue(string textboxText)
        {
            try
            {
                return Convert.ToDecimal(string.IsNullOrEmpty(textboxText) ? "0" : textboxText);
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        public DataTable dbSelectLoyalityPoint(DataTable dt)
        {
            try
            {
                DataTable dt1 = new DataTable();
                SqlCommand cmd = new SqlCommand("Sp_Sale_Calc_loyality", shop);
                cmd.Parameters.AddWithValue("@type1", dt);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.Fill(dt1);
                return dt1;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public int stmtSaveJournal(string statement, JournalModel model)
        {
            int tres = -6;
            DataTable dt1 = new DataTable();
            dt1.Columns.Add("draccount");
            dt1.Columns.Add("craccount");
            dt1.Columns.Add("amount");
            dt1.Columns.Add("Remarks");
            dt1.Columns.Add("dr_id");
            dt1.Columns.Add("cr_id");

            if (model.jvparnew != null)
            {
                foreach (AccJvParNew row in model.jvparnew)
                {
                    if (row.dr_id != null)
                    {
                        DataRow dRow = dt1.NewRow();
                        dRow[0] = row.draccount;
                        dRow[1] = row.craccount.ToString();
                        dRow[2] = row.amount.ToString();
                        dRow[3] = row.remarks.ToString();
                        dRow[4] = row.dr_id.ToString();
                        dRow[5] = row.cr_id.ToString();
                        dt1.Rows.Add(dRow);
                    }
                }
            }


            #region billwise
            DataTable dt2 = new DataTable();
            dt2.Columns.Add("invoiceno");
            dt2.Columns.Add("date");
            dt2.Columns.Add("voucher");
            dt2.Columns.Add("billamt");
            dt2.Columns.Add("bilbalance");
            dt2.Columns.Add("amt");
            dt2.Columns.Add("cheque");
            dt2.Columns.Add("disc");
            dt2.Columns.Add("TDS");
            dt2.Columns.Add("bp_loc_entryno");
            dt2.Columns.Add("bp_sup_invno");

            if (model.billwisePur != null)
            {
                foreach (AccBillwisePur row in model.billwisePur)
                {
                    DataRow dRow1 = dt2.NewRow();

                    if (row.chkTick == true)
                    {
                        dRow1[0] = row.voucherEntryno.ToString();
                        dRow1[1] = row.billDate.ToString();
                        dRow1[2] = row.voucherType.ToString();
                        dRow1[3] = row.billAmount.ToString();
                        dRow1[4] = row.billBalance.ToString();
                        dRow1[5] = row.amount.ToString();
                        dRow1[6] = "";
                        dRow1[7] = row.discount.ToString();
                        dRow1[8] = row.tds.ToString();
                        dRow1[9] = row.locEntryno.ToString();
                        dRow1[10] = row.supInvno.ToString();
                        dt2.Rows.Add(dRow1);
                    }
                }
            }

            #endregion

            if (OpenConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_j_voucherNew";

                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.ReturnValue;

                    cmd.Parameters.Add(parm);
                    cmd.Parameters.AddWithValue("@ji_entryno", model.entryno);
                    cmd.Parameters.AddWithValue("@ji_date", model.ji_date);
                    cmd.Parameters.AddWithValue("@ji_amount", model.ji_amount);
                    cmd.Parameters.AddWithValue("@ji_user", Convert.ToInt32(userId));
                    cmd.Parameters.AddWithValue("@ji_location_id", Convert.ToInt32(locationId));
                    cmd.Parameters.AddWithValue("@type1", dt1);
                    cmd.Parameters.AddWithValue("@type2", dt2);
                    cmd.Parameters.AddWithValue("@StatementType", model.statement);

                    cmd.Connection = shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();
                    tres = Convert.ToInt32(parm.Value);
                    //tres = 1;
                }
                catch (Exception m)
                {
                    Console.WriteLine(m.ToString());
                }
            }
            return tres;
        }

        public int stmtSaveBankPaymentReceipt(string statement, BankPaymentReceipt model)
        {

            int tres = -6;
            DataTable dt1 = new DataTable();
            dt1.Columns.Add("bankId");
            dt1.Columns.Add("type");
            dt1.Columns.Add("cheque_dd_no");
            dt1.Columns.Add("status");
            dt1.Columns.Add("chequeDate");
            dt1.Columns.Add("amount");
            dt1.Columns.Add("bankCharge");
            dt1.Columns.Add("row_id");
            if (model.bankRvPv != null)
            {
                foreach (BankRvPv row in model.bankRvPv)
                {
                    if (row.type != null)
                    {
                        DataRow dRow = dt1.NewRow();
                        dRow[0] = row.bankId.ToString();
                        dRow[1] = row.type.ToString();
                        dRow[2] = row.cheque_dd_no.ToString();
                        dRow[3] = row.status.ToString();
                        dRow[4] = row.chequeDate.ToString();
                        dRow[5] = row.amount.ToString();
                        dRow[6] = row.bankCharge.ToString();
                        dRow[7] = "7";
                        dt1.Rows.Add(dRow);
                    }
                }
            }


            DataTable dt2 = new DataTable();
            dt2.Columns.Add("invoiceno");
            dt2.Columns.Add("date");
            dt2.Columns.Add("voucher");
            dt2.Columns.Add("billamt");
            dt2.Columns.Add("bilbalance");
            dt2.Columns.Add("amt");
            dt2.Columns.Add("cheque");
            dt2.Columns.Add("disc");
            dt2.Columns.Add("TDS");
            dt2.Columns.Add("bp_loc_entryno");
            dt2.Columns.Add("bp_sup_invno");

            if (model.billwisePur != null)
            {
                foreach (AccBillwisePur row in model.billwisePur)
                {
                    DataRow dRow1 = dt2.NewRow();

                    if (row.chkTick == true)
                    {
                        dRow1[0] = row.voucherEntryno.ToString();
                        dRow1[1] = row.billDate.ToString();
                        dRow1[2] = row.voucherType.ToString();
                        dRow1[3] = row.billAmount.ToString();
                        dRow1[4] = row.billBalance.ToString();
                        dRow1[5] = row.amount.ToString();
                        if (row.chequeNo != null)
                        {
                            dRow1[6] = row.chequeNo.ToString();
                        }
                        else
                        {
                            dRow1[6] = "";
                        }
                        dRow1[7] = "";
                        dRow1[8] = "";
                        dRow1[9] = row.locEntryno.ToString();
                        if (row.supInvno != null)
                        {
                            dRow1[10] = row.supInvno.ToString();
                        }
                        else
                        {
                            dRow1[10] = "";
                        }
                        dt2.Rows.Add(dRow1);
                    }
                }
            }

            if (OpenConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_Bank_voucher";

                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.ReturnValue;

                    double amount = model.chequeAmount + model.bankCharge;
                    cmd.Parameters.Add(parm);
                    cmd.Parameters.AddWithValue("@abpr_voucher_name", model.voucherType);
                    cmd.Parameters.AddWithValue("@abpr_entryno", model.entryNo);
                    cmd.Parameters.AddWithValue("@abpr_date", model.date);
                    cmd.Parameters.AddWithValue("@abpr_as_id", model.customer);
                    cmd.Parameters.AddWithValue("@abpr_amount_paid_rec", model.chequeAmount);
                    cmd.Parameters.AddWithValue("@abpr_discount", 0);
                    cmd.Parameters.AddWithValue("@abpr_bank_charge", model.bankCharge);
                    cmd.Parameters.AddWithValue("@abpr_total_amount", amount);
                    cmd.Parameters.AddWithValue("@abpr_remarks", model.remark);
                    cmd.Parameters.AddWithValue("@abpr_user_id", Convert.ToInt32(userId));
                    cmd.Parameters.AddWithValue("@abpr_location_id", Convert.ToInt32(locationId));
                    cmd.Parameters.AddWithValue("@abpr_approved",get_android_settings("direct_banck_voucher") ? 1 : 0);
                    
                    cmd.Parameters.AddWithValue("@type1", dt1);
                    cmd.Parameters.AddWithValue("@type2", dt2);
                    cmd.Parameters.AddWithValue("@StatementType", model.statement);
                    cmd.Connection = shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();
                    tres = Convert.ToInt32(parm.Value);
                    //tres = 1;
                }
                catch (Exception m)
                {
                    Console.WriteLine(m.ToString());
                }
            }
            return tres;

        }
        public int stmtSaveStockTransfer(string statement, StockModel model)
        {
            int tres = -6;
            DataTable dt1 = new DataTable();
            dt1.Columns.Add("istp_ir_id");//0
            dt1.Columns.Add("istp_uniquecode");//1
            dt1.Columns.Add("istp_qty");//2
            dt1.Columns.Add("istp_rate");//3
            dt1.Columns.Add("istp_amount");//4
            dt1.Columns.Add("istp_qty_multi_unit");//5
            dt1.Columns.Add("istp_rate_multiunit");//6
            dt1.Columns.Add("istp_unit_multi");//7
            dt1.Columns.Add("istp_narration_grid");//8
            dt1.Columns.Add("istp_color");//9
            dt1.Columns.Add("istp_cost");//10
            dt1.Columns.Add("istp_exp_date1");//11
            dt1.Columns.Add("istp_size");//12
            dt1.Columns.Add("istp_int_barcode");//13

            if (model.items != null)
            {
                foreach (SalesProducts row in model.items)
                {
                    if (row.psp_ir_id != null)
                    {
                        DataRow dRow = dt1.NewRow();
                        dRow[0] = row.psp_ir_id.ToString();
                        dRow[1] = row.psp_uniquecode.ToString();
                        dRow[2] = row.psp_qty.ToString();
                        dRow[3] = row.psp_rate.ToString();
                        dRow[4] = row.psp_total.ToString();
                        dRow[5] = 0;
                        dRow[6] = 0;
                        dRow[7] = 0;
                        dRow[8] = "";
                        dRow[9] = "";
                        dRow[10] = row.sp_cost.ToString();
                        dRow[11] = "";
                        dRow[12] = "";
                        dRow[13] = "";
                        dt1.Rows.Add(dRow);
                    }

                }
            }
            if (OpenConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_stock_transfer";

                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.ReturnValue;
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
                    cmd.Parameters.AddWithValue("@isti_date", model.date);
                    cmd.Parameters.AddWithValue("@isti_transfer_from", model.fromId);
                    cmd.Parameters.AddWithValue("@isti_transfer_to", model.toId);
                    cmd.Parameters.AddWithValue("@isti_remarks", model.remark);
                    cmd.Parameters.AddWithValue("@type1", dt1);
                    cmd.Parameters.AddWithValue("@StatementType", model.statement);

                    cmd.Connection = shop;
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

                }
                catch (Exception m)
                {
                    Console.WriteLine(m.ToString());
                }
            }
            return tres;
        }
        public DataTable printmanage(int strId, string entryNo, string detail)
        {

            DataTable dt = new DataTable();
            string sql = "";

            switch (detail)
            {
                case "decimal":
                    dt = dbReaderFill("select gs_value from gnl_settings where gs_name='DECIMAL'");
                    break;
                case "company":
                    sql = "SELECT * FROM gnl_company";
                    dt = dbReaderFill(sql);
                    break;
                case "salesinf":
                    sql = @"select si_acc_id as id,si_entryno as entryNo,CONVERT(varchar,si_date,6) as [Date],
                            right(convert(varchar(20),si_date,100),7) [time],a.as_name as custName,a.as_add1 as add1,
                            a.as_add2 as add2,a.as_add3 as add3,a.as_mob as mob,si_disc as Disc, si_net_amount as Net,
                            si_cgst_total as CGST,si_sgst_total as SGST, si_tax as GST, si_total as Total,   
                            si_other_charge as otherCharge,si_other_disc as otherDiscount,si_gross_value as gross,
                            si_grand_total as grandTotal,si_remarks as Remarks,si_freight_charge as freightCharge,
                            si_ob as ob,si_net_balance as netBalanace,si_cash_recieved as cashReceived,si_loc_entryno,
                            a.as_tin as gstin,s.as_name as salesman,si_einvoice_ksa from inv_sales_inf 
                            left join acc_subhead a on si_acc_id=a.as_id left join acc_subhead s on si_salesman_id=s.as_id 
                             where si_str_id=" + strId + " and si_entryno=" + entryNo + "";
                    dt = dbReaderFill(sql);
                    break;
                case "salespar":
                    sql = @"select ir_name as description,ir_hsn_code as hsnCode,ir_taxper as taxPer,sp_qty as qty,sp_rate as rate,
                            sp_gross_value as gross,sp_net_amount as netAmt,sp_disc as discount,ir_cgst as cgstP,
                            sp_cgst as cgst,ir_sgst as sgstP,sp_sgst as sgst,ir_taxper as TaxP,sp_tax as GST,sp_total as total,
                            sp.u_name as unit,u.u_name as uom
                            FROM inv_sales_par
                            left JOIN inv_item_reg on inv_sales_par.sp_ir_id = inv_item_reg.ir_id
                            left join inv_unit as sp on sp_unit_multi = sp.u_id
                            left join inv_unit as u on ir_min_unit_id = u.u_id
                            where sp_str_id=" + strId + " and sp_entryno=" + entryNo + "";
                    dt = dbReaderFill(sql);
                    break;
                case "unit":
                    sql = "select * from inv_multi_unit";
                    dt = dbReaderFill(sql);
                    break;
            }
            return dt;
        }
        public int empSave(string statement, EmployeePerformanceModel model)
        {
            int tres = 0;
            if (OpenConnection())
            {
                try
                {
                    DataSet dataset = new DataSet();

                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_hr_employee_performance";
                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm);
                    cmd.Parameters.AddWithValue("@hep_id", model.hep_id);
                    cmd.Parameters.AddWithValue("@hep_emp_acc_id", model.hep_emp_acc_id);
                    cmd.Parameters.AddWithValue("@hep_orderno", model.hep_orderno);
                    cmd.Parameters.AddWithValue("@hep_start_date", model.hep_start_date);
                    cmd.Parameters.AddWithValue("@hep_end_date", model.hep_end_date);
                    cmd.Parameters.AddWithValue("@hep_remarks", model.hep_remarks);
                    cmd.Parameters.AddWithValue("@hep_status", model.hep_status);
                    cmd.Parameters.AddWithValue("@StatementType", statement);
                    cmd.CommandTimeout = 60;
                    cmd.Connection = shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();
                    tres = Convert.ToInt32(parm.Value);
                    return tres;
                }
                catch (Exception ex) 
                {
                    ex.ToString();
                    tres = 0;
                }
            }
            return tres;
        }
        public int NotpadSave(string statement, NotepadModel model)
        {
            int tres = 0;
            if (OpenConnection())
            {
                try
                {
                    DataSet dataset = new DataSet();

                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_gnl_notepad";
                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm);
                    cmd.Parameters.AddWithValue("@gn_id", model.gn_id);
                    cmd.Parameters.AddWithValue("@gn_date", model.gn_date);
                    cmd.Parameters.AddWithValue("@gn_string_data", model.gn_string_data);
                    cmd.Parameters.AddWithValue("@gn_user_id", userId);
                    cmd.Parameters.AddWithValue("@gn_location_id", locationId);
                    cmd.Parameters.AddWithValue("@StatementType", statement);
                    cmd.CommandTimeout = 60;
                    cmd.Connection = shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();
                    tres = Convert.ToInt32(parm.Value);
                    return tres;
                }
                catch (Exception ex)
                {
                    ex.ToString();
                    tres = 0;
                }
            }
            return tres;
        }
        public bool UpdateCompanyLogo(byte[] imageBytes)
        {
            try
            {
                if (OpenConnection())
                {
                  

                    string sql = "UPDATE gnl_company SET com_logo = @ImageBytes";

                    using (SqlCommand command = new SqlCommand(sql, shop))
                    {
                        command.Parameters.AddWithValue("@ImageBytes", imageBytes);
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
            return true;
        }
        public bool UpdateImage(byte[] imageBytes, string ItemCode)  // Add 'id' parameter to identify the specific record
        {
            try
            {
                if (OpenConnection())
                {
                    // Update the query to filter by the provided ID
                    string sql = "UPDATE inv_item_reg SET ir_image_data = @ImageBytes WHERE ir_code = @ItemCode";

                    using (SqlCommand command = new SqlCommand(sql, shop))
                    {
                        // Add imageBytes and ID as parameters
                        command.Parameters.AddWithValue("@ImageBytes", imageBytes);
                        command.Parameters.AddWithValue("@ItemCode", ItemCode);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;  // Return true if any rows were updated
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }

            return true;
        }
        public int savePurchase(string statementType, PurchaseModel model)
        {
            int tres = -6;
            DataTable dt1 = new DataTable();
            dt1.Columns.Add("ir_id");
            dt1.Columns.Add("qty");
            dt1.Columns.Add("p_rate");
            dt1.Columns.Add("gross");
            dt1.Columns.Add("disc_p");
            dt1.Columns.Add("disc");
            dt1.Columns.Add("net");
            dt1.Columns.Add("vat");
            dt1.Columns.Add("total");
            dt1.Columns.Add("profit_p");
            dt1.Columns.Add("mrp");
            dt1.Columns.Add("retail_per");
            dt1.Columns.Add("retail");
            dt1.Columns.Add("wholesale_per");
            dt1.Columns.Add("whole_sale");
            dt1.Columns.Add("sp_retail_per");
            dt1.Columns.Add("sp_retail");
            dt1.Columns.Add("branch_per");
            dt1.Columns.Add("branch");
            dt1.Columns.Add("realprate");
            dt1.Columns.Add("location");
            dt1.Columns.Add("exp_date");
            dt1.Columns.Add("sticker");
            dt1.Columns.Add("color");
            dt1.Columns.Add("size");
            dt1.Columns.Add("brand");
            dt1.Columns.Add("int_barcode");
            dt1.Columns.Add("uniquecode");
            dt1.Columns.Add("qty_multi_unit");
            dt1.Columns.Add("unit_multi");
            dt1.Columns.Add("prate_multi_unit");
            dt1.Columns.Add("cost");
            dt1.Columns.Add("narration");
            dt1.Columns.Add("cess");
            dt1.Columns.Add("ad_cess");
            dt1.Columns.Add("pp_taxper");
            dt1.Columns.Add("pp_hsn");
            dt1.Columns.Add("pp_local_exp");
            dt1.Columns.Add("pp_bulkrate");
            dt1.Columns.Add("pp_lend_amount");
            dt1.Columns.Add("pp_sgst");
            dt1.Columns.Add("pp_cgst");
            dt1.Columns.Add("pp_igst");
            dt1.Columns.Add("pp_sgstp");
            dt1.Columns.Add("pp_cgstp");
            dt1.Columns.Add("pp_igstp");
            dt1.Columns.Add("pp_r1");
            dt1.Columns.Add("pp_r2");
            dt1.Columns.Add("pp_r3");
            dt1.Columns.Add("pp_r4");
            dt1.Columns.Add("pp_exp_date1");
            dt1.Columns.Add("pp_row_id");
            dt1.Columns.Add("pp_item_narration");
            dt1.Columns.Add("pp_fqty");
            if (model.items != null)
            {
                foreach (PurchaseProducts item in model.items)
                {
                    if (item.IrId != null)
                    {
                        DataRow dRow = dt1.NewRow();
                        dRow[0] = item.IrId;
                        dRow[1] = item.Qty;
                        dRow[2] = item.PRate;
                        dRow[3] = item.Gross;
                        dRow[4] = item.DiscP;
                        dRow[5] = item.Disc;
                        dRow[6] = item.Net;
                        dRow[7] = item.Vat;
                        dRow[8] = item.Total;
                        dRow[9] = item.ProfitP;
                        dRow[10] = item.MRP;
                        dRow[11] = item.RetailPer;
                        dRow[12] = item.Retail;
                        dRow[13] = item.WholesalePer;
                        dRow[14] = item.WholeSale;
                        dRow[15] = item.SpRetailPer;
                        dRow[16] = item.SpRetail;
                        dRow[17] = item.BranchPer;
                        dRow[18] = item.Branch;
                        dRow[19] = item.RealPRate;
                        dRow[20] = item.Location;
                        dRow[21] = item.ExpDate;
                        dRow[22] = item.Sticker;
                        dRow[23] = item.Color;
                        dRow[24] = item.Size;
                        dRow[25] = item.Brand;
                        dRow[26] = item.IntBarcode;
                        dRow[27] = item.UniqueCode;
                        dRow[28] = item.QtyMultiUnit;
                        dRow[29] = item.UnitMulti;
                        dRow[30] = item.PRateMultiUnit;
                        dRow[31] = item.Cost;
                        dRow[32] = item.Narration;
                        dRow[33] = item.Cess;
                        dRow[34] = item.AdCess;
                        dRow[35] = item.TaxPer;
                        dRow[36] = item.HSN;
                        dRow[37] = item.LocalExp;
                        dRow[38] = item.BulkRate;
                        dRow[39] = item.LendAmount;
                        dRow[40] = item.SGST;
                        dRow[41] = item.CGST;
                        dRow[42] = item.IGST;
                        dRow[43] = item.SGSTP;
                        dRow[44] = item.CGSTP;
                        dRow[45] = item.IGSTP;
                        dRow[46] = item.R1;
                        dRow[47] = item.R2;
                        dRow[48] = item.R3;
                        dRow[49] = item.R4;
                        dRow[50] = item.ExpDate1;
                        dRow[51] = item.SlNo;
                        dRow[52] = item.ItemNarration;
                        dRow[53] = item.FQty;

                        dt1.Rows.Add(dRow);
                    }                   
                }
            }
            //DataTable dt_rack = new DataTable();
            //dt_rack.Columns.Add("rckp_ir_id");
            //dt_rack.Columns.Add("rckp_rack_id");
            //dt_rack.Columns.Add("rckp_qty_in");
            //dt_rack.Columns.Add("rckp_qty_out");
            //dt_rack.Columns.Add("rckp_loc_id");
            //dt_rack.Columns.Add("rckp_remarks");

            //foreach (Racks item in model.rack)
            //{
            //    if (item.RckpIrId.HasValue)
            //    {
            //        if (item.RckpIrId != null)
            //        {
            //            DataRow dRow = dt_rack.NewRow();
            //            dRow[0] = item.RckpIrId;
            //            dRow[1] = item.RckpRackId;
            //            dRow[2] = item.RckpQtyIn;
            //            dRow[3] = 0;
            //            dRow[4] = item.RckpLocId;
            //            dRow[5] = "FROM PURCHASE ENTRY";
            //            dt_rack.Rows.Add(dRow);
            //        }                   
            //    }
            //}

            //DataTable _dtAddCostFromPopUp = new DataTable();
            //_dtAddCostFromPopUp.Columns.Add("draccount");
            //_dtAddCostFromPopUp.Columns.Add("craccount");
            //_dtAddCostFromPopUp.Columns.Add("ppc_dr_id");
            //_dtAddCostFromPopUp.Columns.Add("ppc_cr_id");
            //_dtAddCostFromPopUp.Columns.Add("ppc_amount");
            //_dtAddCostFromPopUp.Columns.Add("ppc_remarks");
            //if (model.additionalCosts != null)
            //{
            //    foreach (AdditionalCost item in model.additionalCosts)
            //    {
            //        if (item.PpcDrId != null)
            //        {
            //            DataRow dRow = _dtAddCostFromPopUp.NewRow();
            //            dRow[0] = item.DrAccount;
            //            dRow[1] = item.CrAccount;
            //            dRow[2] = item.PpcDrId;
            //            dRow[3] = item.PpcCrId;
            //            dRow[4] = item.PpcAmount;
            //            dRow[5] = item.PpcRemarks;
            //            _dtAddCostFromPopUp.Rows.Add(dRow);
            //        }                   
            //    }
            //}
            if (OpenConnection())
            {
                try
                {
                    DataSet dataset = new DataSet();

                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_Purchase";
                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm);

                    // Add necessary parameters to the command
                    cmd.Parameters.AddWithValue("@pi_entryno", model.PiEntryNo);
                    cmd.Parameters.AddWithValue("@pi_date", model.PiDate);
                    cmd.Parameters.AddWithValue("@pi_inv_date", model.PiInvDate);
                    cmd.Parameters.AddWithValue("@pi_sup_id", model.PiSupId);
                    cmd.Parameters.AddWithValue("@pi_sup_invno", model.PiSupInvNo);
                    cmd.Parameters.AddWithValue("@pi_type", "P");
                    cmd.Parameters.AddWithValue("@pi_gross_value", model.PiGrossValue);
                    cmd.Parameters.AddWithValue("@pi_disc", model.PiDisc);
                    cmd.Parameters.AddWithValue("@pi_net", model.PiNet);
                    cmd.Parameters.AddWithValue("@pi_tax", model.PiTax);
                    cmd.Parameters.AddWithValue("@pi_total", model.PiTotal);
                    cmd.Parameters.AddWithValue("@pi_other_charge", model.PiOtherCharge);
                    cmd.Parameters.AddWithValue("@pi_other_discper", model.PiOtherDiscPer);
                    cmd.Parameters.AddWithValue("@pi_other_disc", model.PiOtherDisc);
                    cmd.Parameters.AddWithValue("@pi_grand_total", model.PiGrandTotal);
                    cmd.Parameters.AddWithValue("@pi_tax_type", model.PiTaxType);
                    cmd.Parameters.AddWithValue("@pi_pur_acc_id", model.PiPurAccId);
                    cmd.Parameters.AddWithValue("@pi_narration", model.PiNarration);
                    cmd.Parameters.AddWithValue("@pi_roundoff", model.PiRoundoff);
                    cmd.Parameters.AddWithValue("@pi_cash_paid", model.PiCashPaid);
                    cmd.Parameters.AddWithValue("@pi_balance", model.PiBalance);
                    cmd.Parameters.AddWithValue("@pi_user_id", model.PiUserId);
                    cmd.Parameters.AddWithValue("@pi_pono", Convert.ToInt32(model.PiPono));
                    cmd.Parameters.AddWithValue("@pi_location_id", model.PiLocationId);
                    cmd.Parameters.AddWithValue("@pi_freight_per", model.PiFreightPer);
                    cmd.Parameters.AddWithValue("@pi_freight_charges", model.PiFreightCharges);
                    cmd.Parameters.AddWithValue("@pi_freight_tax", model.PiFreightTax);
                    cmd.Parameters.AddWithValue("@pi_hand_per", model.PiHandPer);
                    cmd.Parameters.AddWithValue("@pi_hand_charges", model.PiHandCharges);
                    cmd.Parameters.AddWithValue("@pi_hand_tax", model.PiHandTax);
                    cmd.Parameters.AddWithValue("@pi_insurance_per", model.PiInsurancePer);
                    cmd.Parameters.AddWithValue("@pi_insurance_charges", model.PiInsuranceCharges);
                    cmd.Parameters.AddWithValue("@pi_insurance_tax", model.PiInsuranceTax);
                    cmd.Parameters.AddWithValue("@poi_destination", model.PoiDestination);
                    cmd.Parameters.AddWithValue("@poi_destination_add1", model.PoiDestinationAdd1);
                    cmd.Parameters.AddWithValue("@poi_destination_add2", model.PoiDestinationAdd2);
                    cmd.Parameters.AddWithValue("@poi_destination_add3", model.PoiDestinationAdd3);
                    cmd.Parameters.AddWithValue("@poi_contact_person", model.PoiContactPerson);
                    cmd.Parameters.AddWithValue("@pi_approve", model.PiApprove);
                    cmd.Parameters.AddWithValue("@pi_approvedate", model.PiApproveDate);
                    cmd.Parameters.AddWithValue("@pi_mrn_bookno", model.PiMrnBookNo);

                    cmd.Parameters.AddWithValue("@pi_next_entryno", Convert.ToInt32(model.PiNextEntryNo));
                    cmd.Parameters.AddWithValue("@pi_unregistered", model.PiUnregistered);
                    
                    cmd.Parameters.AddWithValue("@pi_interstate", model.PiInterstate);
                    cmd.Parameters.AddWithValue("@pi_currency_value", model.PiCurrencyValue);
                    cmd.Parameters.AddWithValue("@pi_gstin", model.PiGstin);
                    cmd.Parameters.AddWithValue("@pi_lrno", model.PiLrno);
                    cmd.Parameters.AddWithValue("@pi_lr_date", model.PiLrDate);
                    cmd.Parameters.AddWithValue("@pi_lend_add", model.PiLendAdd);
                    cmd.Parameters.AddWithValue("@pi_lend_less", model.PiLendLess);
                    cmd.Parameters.AddWithValue("@pi_sum_local_exp", model.PiSumLocalExp);
                    cmd.Parameters.AddWithValue("@pi_sum_sgst", model.PiSumSgst);
                    cmd.Parameters.AddWithValue("@pi_sum_cgst", model.PiSumCgst);
                    cmd.Parameters.AddWithValue("@pi_sum_igst", model.PiSumIgst);
                    cmd.Parameters.AddWithValue("@pi_sum_cess", model.PiSumCess);
                    cmd.Parameters.AddWithValue("@pi_sum_ad_cess", model.PiSumAdCess);
                    cmd.Parameters.AddWithValue("@osi_counter", model.OsiCounter);
                    cmd.Parameters.AddWithValue("@pi_salesman_id", Convert.ToInt32(model.PiSalesmanId));
                    cmd.Parameters.AddWithValue("@pi_expense_amount", model.PiExpenseAmount);
                    cmd.Parameters.AddWithValue("@pi_tcs_per", model.PiTcsPer);
                    cmd.Parameters.AddWithValue("@pi_tcs_amount", model.PiTcsAmount);
                    cmd.Parameters.AddWithValue("@pi_ho_id", Convert.ToInt32(model.PiHoId));
                    cmd.Parameters.AddWithValue("@pi_branch_transfer", model.PiBranchTransfer);
                    cmd.Parameters.AddWithValue("@pi_branch_name", model.PiBranchName);
                    cmd.Parameters.AddWithValue("@pi_tds_per", model.PiTdsPer);
                    cmd.Parameters.AddWithValue("@pi_tds_amount", model.PiTdsAmount);
                    cmd.Parameters.AddWithValue("@pi_additional_cost", model.PiAdditionalCost);
                    cmd.Parameters.AddWithValue("@pi_additional_cost_add", model.PiAdditionalCostAdd);
                    cmd.Parameters.AddWithValue("@pi_additional_cost_less", model.PiAdditionalCostLess);
                    cmd.Parameters.AddWithValue("@pi_sup_name", model.PiSupName);

                    if (get_android_settings("ENABLEAPPROVEOPTIONINPO") == true)
                    {
                        if (statementType == "PO_Insert")
                        {
                            cmd.Parameters.AddWithValue("@poi_approved_status", "0");
                        }
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@poi_approved_status", model.PoiApprovedStatus);
                    }
                    cmd.Parameters.AddWithValue("@poi_approved_days", model.PoiApprovedDays);
                    cmd.Parameters.AddWithValue("@poi_popup_date", model.PoiPopupDate);
                    if (get_android_settings("ENABLEITEMSPLITFORDIFFBARCODEINPURCHASE"))
                    {
                        cmd.Parameters.AddWithValue("@ENABLEITEMSPLITFORDIFFBARCODEINPURCHASE", 1);
                    }
                    cmd.Parameters.AddWithValue("@pi_salesman", model.PiSalesman);

                    cmd.Parameters.AddWithValue("@type1", dt1);
                    //cmd.Parameters.AddWithValue("@type2", dt_rack);
                    //cmd.Parameters.AddWithValue("@type3", _dtAddCostFromPopUp);
                    cmd.Parameters.AddWithValue("@StatementType", statementType);

                    cmd.CommandTimeout = 60;
                    cmd.Connection = shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();
                    tres = Convert.ToInt32(parm.Value);
                    return tres;
                }
                catch (Exception ex)
                {
                    ex.ToString();
                    tres = 0;
                }
            }
            return tres;
        }
        public int itemReg(string StatementType, ItemRegModel model)
        {
            int tres = 0;
            DataTable dt1 = new DataTable();
            dt1.Columns.Add("im_ir_id");//0
            dt1.Columns.Add("im_unit_id");//1
            dt1.Columns.Add("im_conversion");//2
            dt1.Columns.Add("im_min_unit_id");//3
            dt1.Columns.Add("im_int_barcode");//4
            dt1.Columns.Add("im_rate");//5
            dt1.Columns.Add("im_loading_charge");//6
            dt1.Columns.Add("im_gatepass");//7
            dt1.Columns.Add("im_retail");//8
            dt1.Columns.Add("im_wsale");//9
            dt1.Columns.Add("im_spretail");//10
            dt1.Columns.Add("im_branch");//11
            if (model.items != null)
            {
                foreach (MultiUnitModel item in model.items)
                {
                    if (item.ImMinUnitId != null)
                    {
                        DataRow dRow = dt1.NewRow();
                        dRow[0] = item.ImIrId;
                        dRow[1] = item.ImUnitId;
                        dRow[2] = item.ImConversion;
                        dRow[3] = item.ImMinUnitId;
                        dRow[4] = item.ImIntBarcode;
                        dRow[5] = item.ImRate;
                        dRow[6] = item.ImLoadingCharge;
                        dRow[7] = item.ImGatePass;
                        dRow[8] = item.ImRetail;
                        dRow[9] = item.ImWSale;
                        dRow[10] = item.ImSpRetail;
                        dRow[11] = item.ImBranch;

                        dt1.Rows.Add(dRow);
                    }                    
                }
            }
            //DataTable dt3 = new DataTable();
            //dt3.Columns.Add("barcode");
            //dt3.Columns.Add("unitname");
            //dt3.Columns.Add("qty");
            //dt3.Columns.Add("sprice");

            //foreach (ItemRegBarcode item in model.itemregbarcode)
            //{
            //    if (item.IrbUnitName!=null)
            //    {
            //        DataRow dRow = dt3.NewRow();
            //        dRow[0] = item.IrbBarcode;
            //        dRow[1] = item.IrbUnitName;
            //        dRow[2] = item.IrbQty;
            //        dRow[3] = item.IrbRate;
            //        dt3.Rows.Add(dRow);
            //    }
            //}

            //DataTable dt2 = new DataTable();
            //dt2.Columns.Add("ire_ie_id");
            //dt2.Columns.Add("ire_rate");
            //if (model.extras != null)
            //{
            //    foreach (ItemRegExtras item in model.extras)
            //    {
            //        if(item.IreIeId !=null)
            //        {
            //            DataRow dRow = dt2.NewRow();
            //            dRow[0] = item.IreIeId;
            //            dRow[1] = item.IreRate;
            //            dt2.Rows.Add(dRow);
            //        }
            //    }
            //}
            if (OpenConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_inv_product";
                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(parm);

                    // Add necessary parameters to the command
                    //cmd.Parameters.AddWithValue("@ir_id", model.EntryNo);
                    cmd.Parameters.AddWithValue("@ir_code", model.ItemCode);
                    cmd.Parameters.AddWithValue("@ir_name", model.ItemName);
                    cmd.Parameters.AddWithValue("@ir_category_id", model.CategoryId);
                    cmd.Parameters.AddWithValue("@ir_mfr_id", 0);
                    cmd.Parameters.AddWithValue("@ir_min_unit_id", model.MinUnitId);
                    cmd.Parameters.AddWithValue("@ir_rlevel", model.MinQty);
                    cmd.Parameters.AddWithValue("@ir_mrp", model.Mrp);
                    cmd.Parameters.AddWithValue("@ir_retail", model.Retail);
                    cmd.Parameters.AddWithValue("@ir_wholesale", model.Wholesale);
                    cmd.Parameters.AddWithValue("@ir_spretail", model.SpRetail);
                    cmd.Parameters.AddWithValue("@ir_branch", model.Branch);
                    cmd.Parameters.AddWithValue("@ir_taxper", model.TaxPer);
                    cmd.Parameters.AddWithValue("@ir_cgst", model.Cgst);
                    cmd.Parameters.AddWithValue("@ir_sgst", model.Sgst);
                    cmd.Parameters.AddWithValue("@ir_igst", model.Igst);
                    cmd.Parameters.AddWithValue("@ir_hsn_code", model.HsnCode);
                    cmd.Parameters.AddWithValue("@ir_barcode", "");
                    cmd.Parameters.AddWithValue("@ir_batch_status", model.BatchStatus);
                    cmd.Parameters.AddWithValue("@ir_allow_negative", model.AllowNegative);
                    cmd.Parameters.AddWithValue("@ir_lend_validation", 0);
                    cmd.Parameters.AddWithValue("@ir_prate", 0.00);
                    cmd.Parameters.AddWithValue("@ir_realprate", 0.00);
                    cmd.Parameters.AddWithValue("@ir_exp_date", null);
                    cmd.Parameters.AddWithValue("@ir_color", "");
                    cmd.Parameters.AddWithValue("@ir_size", "");
                    cmd.Parameters.AddWithValue("@ir_brand", model.Brand);
                    cmd.Parameters.AddWithValue("@ir_lend_status", 0);
                    cmd.Parameters.AddWithValue("@ir_rawmaterial", 0);
                    cmd.Parameters.AddWithValue("@ir_maxqty", 0);
                    cmd.Parameters.AddWithValue("@ir_active", model.Active);
                    cmd.Parameters.AddWithValue("@ir_cess", model.Cess);
                    //cmd.Parameters.AddWithValue("@ir_ad_cess", model.AdCess);
                    cmd.Parameters.Add("@ir_ad_cess", SqlDbType.Float).Value = model.AdCess ?? 0;
                    cmd.Parameters.AddWithValue("@ir_h1", 0);
                    cmd.Parameters.AddWithValue("@ir_controlled", 0);
                    cmd.Parameters.AddWithValue("@ir_nrx", 0);
                    cmd.Parameters.AddWithValue("@ir_banned", 0);
                    cmd.Parameters.AddWithValue("@ir_kfc", 0);
                    cmd.Parameters.AddWithValue("@ir_image", "");
                    cmd.Parameters.AddWithValue("@ir_sub_category_id", model.SubCategoryId);
                    cmd.Parameters.AddWithValue("@ir_weighing_status", 0);
                    cmd.Parameters.AddWithValue("@ir_alias_name", model.AliasName);
                    cmd.Parameters.AddWithValue("@ir_loading_charge", 0.00);
                    cmd.Parameters.AddWithValue("@ir_lend_rate", 0.00);
                    cmd.Parameters.AddWithValue("@ir_transfer_status", 1);
                    cmd.Parameters.AddWithValue("@ir_bulk_unit_id", 1);
                    cmd.Parameters.AddWithValue("@ir_bulk_qty", 0);
                    cmd.Parameters.AddWithValue("@ir_r1", 0.00);
                    cmd.Parameters.AddWithValue("@ir_r2", 0.00);
                    cmd.Parameters.AddWithValue("@ir_r3", 0.00);
                    cmd.Parameters.AddWithValue("@ir_r4", 0.00);
                    cmd.Parameters.AddWithValue("@ir_printer", "");
                    cmd.Parameters.AddWithValue("@ir_narration", "");
                    cmd.Parameters.AddWithValue("@ir_mrp_per", 0);
                    cmd.Parameters.AddWithValue("@ir_retail_per", 0);
                    cmd.Parameters.AddWithValue("@ir_wholesale_per", 0);
                    cmd.Parameters.AddWithValue("@ir_spretail_per", 0);
                    cmd.Parameters.AddWithValue("@ir_branch_per", 0);
                    cmd.Parameters.AddWithValue("@ir_nos", 0);
                    cmd.Parameters.AddWithValue("@ir_group1", model.Group1);
                    cmd.Parameters.AddWithValue("@ir_group2", model.Group2);
                    cmd.Parameters.AddWithValue("@ir_group3", model.Group3);
                    cmd.Parameters.AddWithValue("@ir_reorder_days", 0);
                    cmd.Parameters.AddWithValue("@ir_weighing_id", 0);
                    cmd.Parameters.AddWithValue("@ir_fast_pos_item", 0);
                    cmd.Parameters.AddWithValue("@ir_netwgt", 0.00);
                    cmd.Parameters.AddWithValue("@ir_service_item", 0);
                    cmd.Parameters.AddWithValue("@ir_negative_profit", 0);
                    cmd.Parameters.AddWithValue("@ir_section_id", 0);
                    if (get_android_settings("ENABLE MAX AND MIN RATE IN SALE"))
                    {
                        cmd.Parameters.AddWithValue("@ir_min_rate", model.MinRate);
                        cmd.Parameters.AddWithValue("@ir_max_rate", model.MaxRate);
                    }
                    cmd.Parameters.AddWithValue("@ir_tax_method", 0);
                    cmd.Parameters.AddWithValue("@ir_user_id", 0);
                    cmd.Parameters.AddWithValue("@ir_ad_cess_amt", 0);

                    cmd.Parameters.AddWithValue("@ir_rack_name", "");
                    cmd.Parameters.AddWithValue("@ir_reduce_per", 0);
                    cmd.Parameters.AddWithValue("@ir_image_data", model.ImageData ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@type1", dt1);
                    //cmd.Parameters.AddWithValue("@type2", dt2); 
                    //cmd.Parameters.AddWithValue("@type3", dt3);
                    cmd.Parameters.AddWithValue("@StatementType", StatementType);

                    cmd.Connection = shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();
                    tres = Convert.ToInt32(parm.Value);
                }
                catch (Exception ex)
                {
                    ex.ToString();
                    tres = 0;
                }
            }
            return tres;
        }
        public int CrAndDrSave(string StatementType, CreditAndDebitNoteModel model)
        {
            int tres = -6;
            //DataTable dt1 = new DataTable();
            //dt1.Columns.Add("invoiceno");
            //dt1.Columns.Add("date");
            //dt1.Columns.Add("voucher");
            //dt1.Columns.Add("billamt");
            //dt1.Columns.Add("bilbalance");
            //dt1.Columns.Add("amt");
            //dt1.Columns.Add("cheque");
            //dt1.Columns.Add("disc");
            //dt1.Columns.Add("TDS");
            //dt1.Columns.Add("bp_loc_entryno");
            //dt1.Columns.Add("bp_sup_invno");

            //if (model.billwisePur != null)
            //{
            //    foreach (AccBillwisePur row in model.billwisePur)
            //    {
            //        DataRow dRow1 = dt1.NewRow();

            //        if (row.chkTick == true)
            //        {
            //            dRow1[0] = row.voucherEntryno.ToString();
            //            dRow1[1] = row.billDate.ToString();
            //            dRow1[2] = row.voucherType.ToString();
            //            dRow1[3] = row.billAmount.ToString();
            //            dRow1[4] = row.billBalance.ToString();
            //            dRow1[5] = row.amount.ToString();
            //            dRow1[6] = "";
            //            dRow1[7] = row.discount.ToString();
            //            dRow1[8] = row.tds.ToString();
            //            dRow1[9] = row.locEntryno.ToString();
            //            dRow1[10] = row.supInvno.ToString();
            //            dt1.Rows.Add(dRow1);
            //        }
            //    }
            //}
            if (OpenConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_Billwise";
                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(parm);

                    cmd.Parameters.AddWithValue("@bei_entryno", Convert.ToInt32(model.EntryNo));
                    cmd.Parameters.AddWithValue("@bei_voucher_name", model.VoucherName);
                    cmd.Parameters.AddWithValue("@bei_date", model.Date);
                    cmd.Parameters.AddWithValue("@bei_acc_id", model.AccountId);
                    cmd.Parameters.AddWithValue("@bei_dr_cr_acc_id", model.DrCrAccId);
                    cmd.Parameters.AddWithValue("@bei_amount", model.Amount);
                    cmd.Parameters.AddWithValue("@bei_remarks", "");
                    cmd.Parameters.AddWithValue("@bei_sum_disc", 0.00);
                    cmd.Parameters.AddWithValue("@bei_sum_tds", 0.00);
                    cmd.Parameters.AddWithValue("@bei_user_id", model.UserId);
                    cmd.Parameters.AddWithValue("@bei_reference", model.Referense);
                    cmd.Parameters.AddWithValue("@bei_location_id", model.LocationId);
                    cmd.Parameters.AddWithValue("@bei_salesman_id", -1);
                    cmd.Parameters.AddWithValue("@bei_gst_status", 0);
                    cmd.Parameters.AddWithValue("@bei_net_amount", model.NetAmount);
                    cmd.Parameters.AddWithValue("@bei_sgstp", model.Sgstp);
                    cmd.Parameters.AddWithValue("@bei_cgstp", model.Cgstp);
                    cmd.Parameters.AddWithValue("@bei_igstp", model.Igstp);
                    cmd.Parameters.AddWithValue("@bei_sgst", model.Sgst);
                    cmd.Parameters.AddWithValue("@bei_cgst", model.Cgst);
                    cmd.Parameters.AddWithValue("@bei_igst", model.Igst);
                    cmd.Parameters.AddWithValue("@bei_scheme_status", 0);
                    cmd.Parameters.AddWithValue("@bei_cost_id", -1);
                    cmd.Parameters.AddWithValue("@bei_roundoff", 0.00);
                    cmd.Parameters.AddWithValue("@bei_grand_total", model.GrandTotal);
                    cmd.Parameters.AddWithValue("@bei_irn", "");
                    cmd.Parameters.AddWithValue("@bei_qr_link", "");
                    //cmd.Parameters.AddWithValue("@type1", dt1);
                    cmd.Parameters.AddWithValue("@StatementType", StatementType);

                    cmd.Connection = shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();
                    tres = Convert.ToInt32(parm.Value);
                }
                catch (Exception ex)
                {
                    ex.ToString();
                    tres = 0;
                }
            }
            return tres;
        }

        public DataSet dbSelectData1(string StatementType, string voucherName, int custId)
        {
            try
            {
                DataSet dataset = new DataSet();
                SqlCommand cmd = new SqlCommand("Sp_Billwise", shop);
                cmd.Parameters.AddWithValue("@bei_acc_id", custId);
                cmd.Parameters.AddWithValue("@bei_voucher_name", voucherName);
                cmd.Parameters.AddWithValue("@StatementType", StatementType);
                cmd.CommandType = CommandType.StoredProcedure;
                DataTable _temp = new DataTable();
                SqlDataAdapter adp = new SqlDataAdapter(cmd);
                adp.SelectCommand.CommandTimeout = 60;
                adp.Fill(dataset);
                return dataset;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public int Sp_voucher(string statement, VouncherModel model)
        {
            int tres = -6;
            DataTable dt1 = new DataTable();
            dt1.Columns.Add("rp_amount");
            dt1.Columns.Add("rp_disc");
            dt1.Columns.Add("rp_total");
            dt1.Columns.Add("rp_remarks");
            dt1.Columns.Add("rp_account_id");
            dt1.Columns.Add("rp_refno");
            dt1.Columns.Add("rp_ob");

            foreach (VouncherParModel row in model.items)
            {
                if (row.partyId != null)
                {
                    string ob = "0";
                    string str_ledbalance = "EXEC [dbo].[Sp_acc_reg] " +
                                            "@as_id = " + row.partyId + "," +
                                            "@StatementType = N'ledbalance'";
                    DataTable dt_ledbalance = dbReaderFill(str_ledbalance);
                    if (dt_ledbalance.Rows.Count > 0)
                    {
                        ob = dt_ledbalance.Rows[0][0].ToString();
                    }

                    DataRow dRow = dt1.NewRow();
                    dRow[0] = row.amount;
                    dRow[1] = row.discount;
                    dRow[2] = row.total;
                    dRow[3] = row.remark;
                    dRow[4] = row.partyId;
                    dRow[5] = "";
                    dRow[6] = ob;
                    dt1.Rows.Add(dRow);
                }

            }
            if (OpenConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_voucher";

                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.Output;

                    cmd.Parameters.Add(parm);

                    cmd.Parameters.AddWithValue("@ri_entryno", model.entryNo);
                    cmd.Parameters.AddWithValue("@ri_date", model.date);
                    cmd.Parameters.AddWithValue("@ri_debitaccount", model.cashacId);
                    cmd.Parameters.AddWithValue("@ri_amount", model.sumAmount);
                    cmd.Parameters.AddWithValue("@ri_disc", model.sumDiscount);
                    cmd.Parameters.AddWithValue("@ri_total", model.sumTotal);
                    cmd.Parameters.AddWithValue("@ri_location_id", model.location);
                    cmd.Parameters.AddWithValue("@ri_salesman_id", 1);
                    cmd.Parameters.AddWithValue("@ri_group_name",-1);
                    cmd.Parameters.AddWithValue("@ri_person_id", -1);
                    cmd.Parameters.AddWithValue("@ri_user", userId);
                    cmd.Parameters.AddWithValue("@ri_day_open_id", 0);
                    cmd.Parameters.AddWithValue("@ri_cost_id", -1);
                    cmd.Parameters.AddWithValue("@type1", dt1);
                    cmd.Parameters.AddWithValue("@StatementType", statement);
                    cmd.Connection = shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();
                    tres = Convert.ToInt32(parm.Value);
                    
                }
                catch (Exception m)
                {
                    Console.WriteLine(m.ToString());
                }
            }
            return tres;
        }


        public int Sp_Billwise(string StatementType, InvoicewisePaymentAndReceiptModel model)
        {
            int tres = -6;
            DataTable dt1 = new DataTable();
            dt1.Columns.Add("invoiceno");
            dt1.Columns.Add("date");
            dt1.Columns.Add("voucher");
            dt1.Columns.Add("billamt");
            dt1.Columns.Add("bilbalance");
            dt1.Columns.Add("amt");
            dt1.Columns.Add("cheque");
            dt1.Columns.Add("disc");
            dt1.Columns.Add("TDS");
            dt1.Columns.Add("bp_loc_entryno");
            dt1.Columns.Add("bp_sup_invno");

            if (model.billWisePur != null)
            {
                foreach (AccBillwisePur row in model.billWisePur)
                {
                    DataRow dRow1 = dt1.NewRow();

                    if (row.chkTick == true)
                    {
                        dRow1[0] = row.voucherEntryno.ToString();
                        dRow1[1] = row.billDate.ToString();
                        dRow1[2] = row.voucherType.ToString();
                        dRow1[3] = row.billAmount.ToString();
                        dRow1[4] = row.billBalance.ToString();
                        dRow1[5] = row.amount.ToString();
                        dRow1[6] = "";
                        dRow1[7] = row.discount.ToString();
                        dRow1[8] = row.tds.ToString();
                        dRow1[9] = row.locEntryno.ToString();
                        dRow1[10] = row.supInvno.ToString();
                        dt1.Rows.Add(dRow1);
                    }
                }
            }
            if (OpenConnection())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_Billwise";
                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(parm);

                    cmd.Parameters.AddWithValue("@bei_entryno", Convert.ToInt32(model.entryNo));
                    cmd.Parameters.AddWithValue("@bei_voucher_name", model.voucherName);
                    cmd.Parameters.AddWithValue("@bei_date", model.date);
                    cmd.Parameters.AddWithValue("@bei_acc_id", model.accountId);
                    cmd.Parameters.AddWithValue("@bei_dr_cr_acc_id", model.drCrAccId);
                    cmd.Parameters.AddWithValue("@bei_amount", model.amount);
                    cmd.Parameters.AddWithValue("@bei_remarks", model.remarks);
                    cmd.Parameters.AddWithValue("@bei_sum_disc", model.sumDisc);
                    cmd.Parameters.AddWithValue("@bei_sum_tds", model.sumTds);
                    cmd.Parameters.AddWithValue("@bei_user_id", userId);
                    cmd.Parameters.AddWithValue("@bei_reference", model.reference);
                    cmd.Parameters.AddWithValue("@bei_location_id", locationId);
                    cmd.Parameters.AddWithValue("@bei_salesman_id", -1);
                    cmd.Parameters.AddWithValue("@bei_gst_status", 0);
                    cmd.Parameters.AddWithValue("@bei_net_amount", model.netAmount);
                    cmd.Parameters.AddWithValue("@bei_sgstp", model.sgstp);
                    cmd.Parameters.AddWithValue("@bei_cgstp", model.cgstp);
                    cmd.Parameters.AddWithValue("@bei_igstp", model.igstp);
                    cmd.Parameters.AddWithValue("@bei_sgst", model.sgst);
                    cmd.Parameters.AddWithValue("@bei_cgst", model.cgst);
                    cmd.Parameters.AddWithValue("@bei_igst", model.igst);
                    cmd.Parameters.AddWithValue("@bei_scheme_status", 0);
                    cmd.Parameters.AddWithValue("@bei_cost_id", -1);
                    cmd.Parameters.AddWithValue("@bei_roundoff", 0.00);
                    cmd.Parameters.AddWithValue("@bei_grand_total", model.grandTotal);
                    cmd.Parameters.AddWithValue("@bei_irn", "");
                    cmd.Parameters.AddWithValue("@bei_qr_link", "");
                    cmd.Parameters.AddWithValue("@type1", dt1);
                    cmd.Parameters.AddWithValue("@StatementType", StatementType);

                    cmd.Connection = shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();
                    tres = Convert.ToInt32(parm.Value);
                }
                catch (Exception ex)
                {
                    ex.ToString();
                    tres = 0;
                }
            }
            return tres;
        }
        //public bool IsUserValid()
        //{
        //    try
        //    {
        //        if (!OpenConnection())
        //            return false;

        //        if (!int.TryParse(userId, out var uid))
        //            return false;

        //        // If an employee resigns, keep the row (FK/history) but disable login by setting gu_active = 0.
        //        // This check runs on every authorized request (via UserSqlServer ctor validateUser=true).
        //        string guPass = null;
        //        using (var cmd = new SqlCommand("SELECT TOP 1 gu_active, gu_pass FROM gnl_users WHERE gu_user_id = @uid", shop))
        //        {
        //            cmd.Parameters.AddWithValue("@uid", uid);
        //            using (var reader = cmd.ExecuteReader())
        //            {
        //                if (!reader.Read())
        //                    return false;

        //                if (Convert.ToInt32(reader["gu_active"]) != 1)
        //                    return false;

        //                guPass = reader["gu_pass"] == DBNull.Value ? "" : reader["gu_pass"].ToString();
        //            }
        //        }

        //        // Password fingerprint check only when userid is listed in gnl_pass_validate.
        //        if (IsPassValidateUser(uid))
        //        {
        //            if (string.IsNullOrEmpty(tokenPassHash))
        //                return false;

        //            string tokenPass = CommonHelper.tokenDecrypt(tokenPassHash);
        //            if (!string.Equals(tokenPass, guPass ?? "", StringComparison.OrdinalIgnoreCase))
        //                return false;
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //    return false;
        //}
        public bool IsUserValid()
        {
            if (!int.TryParse(userId, out var uid) || uid <= 0)
                return false;
            if (string.IsNullOrWhiteSpace(connetionString))
                return false;

            // Cache key: connection + user so different databases / users
            // don't share cached results.
            string cacheKey = connetionString + "|" + uid;

            // Fast path: cache hit — pure in-memory, no blocking.
            if (_userValidCache.TryGetValue(cacheKey, out var cached)
                && cached.expiresUtc > DateTime.UtcNow)
            {
                return cached.valid;
            }

            // Slow path: cache miss. A few concurrent threads may hit the DB
            // simultaneously on the very first request — that's fine.
            // conn.Open() takes ~30ms and SetMinThreads(200) ensures enough
            // threads. Using a lock here would block 99 threads and deadlock IIS.
            try
            {
                using var conn = new SqlConnection(connetionString);
                conn.Open();

                string guPass = null;
                using (var cmd = new SqlCommand(
                    "SELECT TOP 1 gu_active, gu_pass FROM gnl_users WHERE gu_user_id = @uid",
                    conn))
                {
                    cmd.CommandTimeout = 10;
                    cmd.Parameters.Add("@uid", SqlDbType.Int).Value = uid;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            _userValidCache[cacheKey] = (false, DateTime.UtcNow.AddMinutes(UserValidCacheMinutes));
                            return false;
                        }

                        if (Convert.ToInt32(reader["gu_active"]) != 1)
                        {
                            _userValidCache[cacheKey] = (false, DateTime.UtcNow.AddMinutes(UserValidCacheMinutes));
                            return false;
                        }

                        guPass = reader["gu_pass"] == DBNull.Value
                            ? ""
                            : reader["gu_pass"].ToString();
                    }
                }

                bool result;
                if (!IsPassValidateUser(conn, uid))
                {
                    result = true;
                }
                else if (string.IsNullOrEmpty(tokenPassHash))
                {
                    result = false;
                }
                else
                {
                    string tokenPass = CommonHelper.tokenDecrypt(tokenPassHash);
                    result = string.Equals(tokenPass, guPass ?? "", StringComparison.OrdinalIgnoreCase);
                }

                _userValidCache[cacheKey] = (result, DateTime.UtcNow.AddMinutes(UserValidCacheMinutes));
                return result;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                Console.WriteLine("IsUserValid ERROR: " + ex.ToString());
                throw new InvalidOperationException(
                    "User validation failed due to a database error: " + ex.Message, ex);
            }
        }

        void EnsurePassValidateTable(SqlConnection connection)
        {
            if (_passValidateTableEnsured)
                return;

            try
            {
                using (var cmd = new SqlCommand(@"
                IF OBJECT_ID(N'dbo.gnl_pass_validate', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.gnl_pass_validate
                    (
                        gu_user_id INT NOT NULL PRIMARY KEY
                    );
                END", connection))
                {
                    cmd.CommandTimeout = 30;
                    cmd.ExecuteNonQuery();
                }
                _passValidateTableEnsured = true;
            }
            catch
            {
                // Table may already exist or DB user lacks CREATE — ignore.
            }
        }

        bool IsPassValidateUser(SqlConnection connection, int uid)
        {
            try
            {
                EnsurePassValidateTable(connection);
                using (var cmd = new SqlCommand(
                    "SELECT TOP 1 1 FROM dbo.gnl_pass_validate WHERE gu_user_id = @uid",
                    connection))
                {
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.Add("@uid", SqlDbType.Int).Value = uid;
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value;
                }
            }
            catch
            {
                return false;
            }
        }
    }

}
