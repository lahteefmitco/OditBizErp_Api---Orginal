using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class DeliveryBoyController : ControllerBase
    {
        [HttpGet("deliveryboy-dashboard")]
        public async Task<IActionResult> DeliveryBoyDashboard(string fromDate, string toDate, string routeId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                Dictionary<string, object> hash = new Dictionary<string, object>();


                if (string.IsNullOrEmpty(fromDate))
                {
                    fromDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }



                string sql = @"

                SELECT

                ISNULL((
                SELECT COUNT(*)
                FROM acc_subhead
                WHERE as_ap_id = 4
                AND as_active = 1
                AND as_salesman_id = '" + usqlre.gu_acc_id + @"'
                AND as_rout_id = '" + routeId + @"'
                ),0) AS totalShop,

                ISNULL((
                SELECT COUNT(DISTINCT SC.sc_shop_id)

                FROM shop_check_in SC

                INNER JOIN acc_subhead A
                ON A.as_id = SC.sc_shop_id

                WHERE SC.sc_userid = '" + usqlre.userId + @"'
                AND SC.sc_check_in = 2

                AND A.as_ap_id = 4
                AND A.as_active = 1
                AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
                AND as_rout_id = '" + routeId + @"'

                AND CAST(SC.sc_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ),0) AS shopCovered,


                ISNULL((
                SELECT COUNT(DISTINCT SC.sc_shop_id)

                FROM shop_check_in SC

                INNER JOIN acc_subhead A
                ON A.as_id = SC.sc_shop_id

                WHERE SC.sc_userid = '" + usqlre.userId + @"'

                AND SC.sc_skip_status = 1

                AND A.as_ap_id = 4
                AND A.as_active = 1
                AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
                AND as_rout_id = '" + routeId + @"'

                AND CAST(SC.sc_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ),0) AS skipShop,

               ISNULL((
                SELECT COUNT(*)

                FROM acc_subhead A

                LEFT JOIN shop_check_in SC
                ON A.as_id = SC.sc_shop_id
                AND SC.sc_userid = '" + usqlre.userId + @"'
                AND (SC.sc_check_in = 2 OR SC.sc_skip_status = 1)
                AND CAST(SC.sc_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                WHERE A.as_ap_id = 4
                AND A.as_active = 1
                AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
                AND as_rout_id = '" + routeId + @"'

                AND SC.sc_shop_id IS NULL

                ),0) AS pendingShop,


               

                ISNULL((
                SELECT SUM(SI.si_grand_total)

                FROM inv_sales_inf SI

                INNER JOIN acc_subhead A
                ON A.as_id = SI.si_acc_id

                WHERE SI.si_user_id = '" + usqlre.userId + @"'

                AND SI.si_str_id IN (1,2,6)

                AND A.as_rout_id = '" + routeId + @"'

                AND CAST(SI.si_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ),0) AS totalSales,

                ISNULL((
                SELECT SUM(SI.si_grand_total)

                FROM inv_sales_inf SI

                INNER JOIN acc_subhead A
                ON A.as_id = SI.si_acc_id

                WHERE SI.si_user_id = '" + usqlre.userId + @"'

                AND SI.si_str_id = 3

                AND A.as_rout_id = '" + routeId + @"'

                AND CAST(SI.si_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ),0) AS totalOrderValue,



                 ISNULL((
                SELECT SUM(SI.si_grand_total)

                FROM inv_sales_inf SI

                INNER JOIN acc_subhead A
                ON A.as_id = SI.si_acc_id

                WHERE SI.si_user_id = '" + usqlre.userId + @"'
                AND SI.si_str_id IN (1,2,6)

                AND A.as_rout_id = '" + routeId + @"'

                AND CAST(SI.si_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ),0)

                -

                 ISNULL((
                SELECT SUM(SI.si_grand_total)

                FROM inv_sales_inf SI

                INNER JOIN acc_subhead A
                ON A.as_id = SI.si_acc_id

                WHERE SI.si_user_id = '" + usqlre.userId + @"'
                AND SI.si_str_id = 7

                AND A.as_rout_id = '" + routeId + @"'

                AND CAST(SI.si_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ),0) AS netSales,

                ISNULL((
                SELECT SUM(SI.si_grand_total)

                FROM inv_sales_inf SI

                INNER JOIN acc_subhead A
                ON A.as_id = SI.si_acc_id

                WHERE SI.si_user_id = '" + usqlre.userId + @"'
                AND SI.si_str_id = 7

                AND A.as_rout_id = '" + routeId + @"'

                AND CAST(SI.si_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ),0) AS totalReturn,


                ISNULL((
                SELECT COUNT(*) FROM
                acc_account_transactions AT

                INNER JOIN acc_subhead A
                ON A.as_id = AT.at_as_id

                INNER JOIN acc_parent AP
                ON AP.ap_id = A.as_ap_id

                WHERE
                (
                    AT.at_form = 'RECEIPT'
                    OR
                    AT.at_form = 'BANK RECEIPT'
                    OR
                    AT.at_form = 'CASH RECEIPT'
                    OR
                    AT.at_form = 'RECEIPT-I'
                )

                
                AND AT.at_user_id =
                '" + usqlre.userId + @"'

                AND A.as_rout_id =
                '" + routeId + @"'

                AND CAST(AT.at_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ),0) AS totalCount,


                ISNULL((
                SELECT SUM(AT.at_Cr)

                FROM acc_account_transactions AT

                INNER JOIN acc_subhead A
                ON A.as_id = AT.at_as_id

                WHERE AT.at_user_id = '" + usqlre.userId + @"'

                AND A.as_rout_id = '" + routeId + @"'

                AND CAST(AT.at_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ),0) AS totalAmount,



                ISNULL((
                SELECT TOP 1
                R.r_starting_place

                FROM inv_rout_reg R

                WHERE R.r_id = '" + routeId + @"'

                ),'') AS route_starting_place,


                ISNULL((
                SELECT TOP 1
                R.r_ending_place

                FROM inv_rout_reg R

                WHERE R.r_id = '" + routeId + @"'

                ),'') AS route_ending_place,


                /*ISNULL((
                SELECT TOP 1

                CASE

                WHEN EXISTS
                (
                    SELECT 1
                    FROM route_tracking_history RTH

                    INNER JOIN route_tracking RT
                    ON RT.rt_id = RTH.rth_route_tracking_id

                    WHERE RT.rt_route_id = '" + routeId + @"'
                    AND RT.rt_salesman = '" + usqlre.gu_acc_id + @"'

                    AND RTH.rth_status = 'end'

                    AND CAST(RT.rt_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                )
                THEN 'end'

                WHEN EXISTS
                (
                    SELECT 1
                    FROM route_tracking_history RTH

                    INNER JOIN route_tracking RT
                    ON RT.rt_id = RTH.rth_route_tracking_id

                    WHERE RT.rt_route_id = '" + routeId + @"'
                    AND RT.rt_salesman = '" + usqlre.gu_acc_id + @"'

                    AND RTH.rth_status = 'pause'

                    AND CAST(RT.rt_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                )
                THEN 'pause'

                WHEN EXISTS
                (
                    SELECT 1
                    FROM route_tracking_history RTH

                    INNER JOIN route_tracking RT
                    ON RT.rt_id = RTH.rth_route_tracking_id

                    WHERE RT.rt_route_id = '" + routeId + @"'
                    AND RT.rt_salesman = '" + usqlre.gu_acc_id + @"'

                    AND RTH.rth_status = 'resume'

                    AND CAST(RT.rt_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                )
                THEN 'resume'

                WHEN EXISTS
                (
                    SELECT 1
                    FROM route_tracking_history RTH

                    INNER JOIN route_tracking RT
                    ON RT.rt_id = RTH.rth_route_tracking_id

                    WHERE RT.rt_route_id = '" + routeId + @"'
                    AND RT.rt_salesman = '" + usqlre.gu_acc_id + @"'

                    AND RTH.rth_status = 'stop'

                    AND CAST(RT.rt_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                )
                THEN 'stop'

                WHEN EXISTS
                (
                    SELECT 1
                    FROM route_tracking_history RTH

                    INNER JOIN route_tracking RT
                    ON RT.rt_id = RTH.rth_route_tracking_id

                    WHERE RT.rt_route_id = '" + routeId + @"'
                    AND RT.rt_salesman = '" + usqlre.gu_acc_id + @"'

                    AND RTH.rth_status = 'start'

                    AND CAST(RT.rt_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                )
                THEN 'start'

                ELSE 'not_started'

                END

                ),'') AS ride_status,*/

                ISNULL((
                SELECT TOP 1
                RTH.rth_status

                FROM route_tracking_history RTH

                INNER JOIN route_tracking RT
                ON RT.rt_id = RTH.rth_route_tracking_id

                WHERE RT.rt_route_id = '" + routeId + @"'
                AND RT.rt_salesman = '" + usqlre.gu_acc_id + @"'

                AND CAST(RT.rt_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ORDER BY RTH.rth_id DESC

                ),'not_started') AS ride_status,



                ISNULL((
                SELECT TOP 1
                RTH.rth_time

                FROM route_tracking_history RTH

                INNER JOIN route_tracking RT
                ON RT.rt_id = RTH.rth_route_tracking_id

                WHERE RT.rt_route_id = '" + routeId + @"'
                AND RT.rt_salesman = '" + usqlre.gu_acc_id + @"'

                AND RTH.rth_status = 'start'

                AND CAST(RT.rt_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ORDER BY RTH.rth_id ASC

                ),'') AS ride_start_time,


                ISNULL((
                SELECT STUFF
                (
                (
                SELECT ', ' + RTH.rth_time

                FROM route_tracking_history RTH

                INNER JOIN route_tracking RT
                ON RT.rt_id = RTH.rth_route_tracking_id

                WHERE RT.rt_route_id = '" + routeId + @"'
                AND RT.rt_salesman = '" + usqlre.gu_acc_id + @"'

                AND RTH.rth_status = 'pause'

                AND CAST(RT.rt_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ORDER BY RTH.rth_id ASC

                FOR XML PATH('')
                ),1,2,''
                )

                ),'') AS ride_pause_times,


                ISNULL((
                SELECT STUFF
                (
                (
                SELECT ', ' + RTH.rth_time

                FROM route_tracking_history RTH

                INNER JOIN route_tracking RT
                ON RT.rt_id = RTH.rth_route_tracking_id

                WHERE RT.rt_route_id = '" + routeId + @"'
                AND RT.rt_salesman = '" + usqlre.gu_acc_id + @"'

                AND RTH.rth_status = 'resume'

                AND CAST(RT.rt_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ORDER BY RTH.rth_id ASC

                FOR XML PATH('')
                ),1,2,''
                )

                ),'') AS ride_resume_times,


                ISNULL((
                SELECT STUFF
                (
                (
                SELECT ', ' + RTH.rth_time

                FROM route_tracking_history RTH

                INNER JOIN route_tracking RT
                ON RT.rt_id = RTH.rth_route_tracking_id

                WHERE RT.rt_route_id = '" + routeId + @"'
                AND RT.rt_salesman = '" + usqlre.gu_acc_id + @"'

                AND RTH.rth_status = 'stop'

                AND CAST(RT.rt_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ORDER BY RTH.rth_id ASC

                FOR XML PATH('')
                ),1,2,''
                )

                ),'') AS ride_stop_times,


                ISNULL((
                SELECT TOP 1
                RTH.rth_time

                FROM route_tracking_history RTH

                INNER JOIN route_tracking RT
                ON RT.rt_id = RTH.rth_route_tracking_id

                WHERE RT.rt_route_id = '" + routeId + @"'
                AND RT.rt_salesman = '" + usqlre.gu_acc_id + @"'

                AND RTH.rth_status = 'end'

                AND CAST(RT.rt_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ORDER BY RTH.rth_id DESC

                ),'') AS ride_end_time




                ";

                Console.WriteLine(sql);

                DataTable dashboard = usqlre.dbReaderFill(sql);

                hash.Add("dashboard", dashboard);

                string recentVisitSql = @"

                SELECT TOP 10

                A.as_name AS shop_name,

                ISNULL(GL.gl_name,'') AS location_name,

                ISNULL(R.r_name,'') AS route_name,

                SC.sc_check_in_time,
                SC.sc_check_out_time,
                SC.sc_latitude_in,
                SC.sc_longitude_in,
                A.as_latitude AS shop_latitude,
                A.as_longitude AS shop_longitude

                FROM shop_check_in SC

                INNER JOIN acc_subhead A
                ON A.as_id = SC.sc_shop_id

                LEFT JOIN gnl_location GL
                ON GL.gl_id = A.as_location_id

                LEFT JOIN inv_rout_reg R
                ON R.r_id = A.as_rout_id

                WHERE SC.sc_userid = '" + usqlre.userId + @"'
                AND SC.sc_check_in = 2

                AND CAST(SC.sc_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                AND A.as_ap_id = 4
                AND A.as_active = 1
                AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
                AND A.as_rout_id = '" + routeId + @"'

                ORDER BY SC.sc_id DESC

                ";

                DataTable recentVisit = usqlre.dbReaderFill(recentVisitSql);


                hash.Add("recent_visit", recentVisit);

                string jsonResult = ReportModelContext.searializeDt(hash);
                return Content(jsonResult, "application/json");
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


        [HttpGet("total-shop-list")]
        public async Task<IActionResult> TotalShopList(string routeId, string toDate)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                string summarySql = @"

        SELECT

        COUNT(*) AS total_shop

        FROM acc_subhead A

        WHERE A.as_ap_id = 4
        AND A.as_active = 1
        AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
        AND A.as_rout_id = '" + routeId + @"'
        AND CAST (A.as_date AS DATE) <= '" + toDate + @"'

        ";

                DataTable summary = usqlre.dbReaderFill(summarySql);

                hash.Add("summary", summary);

                string sql = @"

        SELECT

        A.as_id AS shop_id,
        A.as_name AS shop_name,

        ISNULL(GL.gl_name,'') AS location_name,

        ISNULL(A.as_mob,'') AS mobile,

        ISNULL(A.as_latitude,'0') AS latitude,
        ISNULL(A.as_longitude,'0') AS longitude,

        ISNULL(R.r_name,'') AS route_name,

        ISNULL(A.as_add1,'') AS address1,
        ISNULL(A.as_add2,'') AS address2,
        ISNULL(A.as_add3,'') AS address3,
        ISNULL(A.as_location,'') AS actual_location

        FROM acc_subhead A

        LEFT JOIN gnl_location GL
        ON GL.gl_id = A.as_location_id

        LEFT JOIN inv_rout_reg R
        ON R.r_id = A.as_rout_id

        WHERE A.as_ap_id = 4
        AND A.as_active = 1
        AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'

        AND A.as_rout_id = '" + routeId + @"'
        AND CAST(A.as_date AS DATE) <= '" + toDate + @"'

        ORDER BY A.as_name ASC

        ";

                DataTable totalShop = usqlre.dbReaderFill(sql);

                hash.Add("total_shop", totalShop);

                string jsonResult = ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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


        [HttpGet("pending-shop-list")]
        public async Task<IActionResult> PendingShopList(string fromDate, string toDate, string routeId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                

                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

                if (string.IsNullOrEmpty(fromDate))
                {
                    fromDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                string summarySql = @"

                SELECT

                COUNT(*) AS total_pending_shop

                FROM acc_subhead A

                WHERE A.as_ap_id = 4
                AND A.as_active = 1
                

                AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
                AND A.as_rout_id = '" + routeId + @"'

                AND A.as_id NOT IN
                (
                    SELECT DISTINCT SC.sc_shop_id
                    FROM shop_check_in SC
                    WHERE SC.sc_userid = '" + usqlre.userId + @"'

                    AND
                    (
                        SC.sc_check_in = 2
                        OR SC.sc_skip_status = 1
                    )

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                )

                ";

                DataTable summary = usqlre.dbReaderFill(summarySql);

                hash.Add("summary", summary);

                string sql = @"

                SELECT

                A.as_id AS shop_id,
                A.as_name AS shop_name,

                ISNULL(GL.gl_name,'') AS location_name,
                ISNULL(R.r_name,'') AS route_name,

                ISNULL(A.as_latitude,'0') AS latitude,
                ISNULL(A.as_longitude,'0') AS longitude,
                ISNULL(A.as_location,'') AS actual_location

                FROM acc_subhead A

                LEFT JOIN gnl_location GL
                ON GL.gl_id = A.as_location_id

                LEFT JOIN Inv_rout_reg R
                ON R.r_id = A.as_rout_id

                WHERE A.as_ap_id = 4
                AND A.as_active = 1

                AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
                AND A.as_rout_id = '" + routeId + @"'

                AND A.as_id NOT IN
                (
                    SELECT DISTINCT SC.sc_shop_id
                    FROM shop_check_in SC
                    WHERE SC.sc_userid = '" + usqlre.userId + @"'

                    AND
                    (
                        SC.sc_check_in = 2
                        OR SC.sc_skip_status = 1
                    )

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                )

                ORDER BY A.as_name ASC

                ";

                DataTable pendingShop = usqlre.dbReaderFill(sql);

                hash.Add("pending_shop", pendingShop);

                string jsonResult = ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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


        [HttpGet("shop-covered-list")]
        public async Task<IActionResult> ShopCoveredList(string fromDate, string toDate, string routeId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);


                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

                if (string.IsNullOrEmpty(fromDate))
                {
                    fromDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                string summarySql = @"

        SELECT

        COUNT(DISTINCT SC.sc_shop_id) AS total_shop_covered

        FROM shop_check_in SC

        INNER JOIN acc_subhead A
        ON A.as_id = SC.sc_shop_id

        WHERE SC.sc_userid = '" + usqlre.userId + @"'

        AND SC.sc_check_in = 2

        AND A.as_ap_id = 4
        AND A.as_active = 1
        AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
        AND A.as_rout_id = '" + routeId + @"'

        AND CAST(SC.sc_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'

        ";

                DataTable summary = usqlre.dbReaderFill(summarySql);

                hash.Add("summary", summary);

                string sql = @"

        SELECT DISTINCT

        A.as_id AS shop_id,
        A.as_name AS shop_name,

        ISNULL(GL.gl_name,'') AS location_name,

        ISNULL(R.r_name,'') AS route_name,

        ISNULL(A.as_latitude,'0') AS latitude,
        ISNULL(A.as_longitude,'0') AS longitude,
        ISNULL(A.as_location,'') AS actual_location,

        SC.sc_check_in_time,
        SC.sc_check_out_time

        FROM shop_check_in SC

        INNER JOIN acc_subhead A
        ON A.as_id = SC.sc_shop_id

        LEFT JOIN gnl_location GL
        ON GL.gl_id = A.as_location_id

        LEFT JOIN inv_rout_reg R
        ON R.r_id = A.as_rout_id

        WHERE SC.sc_userid = '" + usqlre.userId + @"'

        AND SC.sc_check_in = 2

        AND A.as_ap_id = 4
        AND A.as_active = 1
        AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
        AND A.as_rout_id = '" + routeId + @"'

        AND CAST(SC.sc_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'

        ORDER BY A.as_name ASC

        ";

                DataTable coveredShop = usqlre.dbReaderFill(sql);

                hash.Add("shop_covered", coveredShop);

                string jsonResult = ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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

        [HttpGet("skip-shop-list")]
        public async Task<IActionResult> SkipShopList(string fromDate, string toDate, string routeId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

                if (string.IsNullOrEmpty(fromDate))
                {
                    fromDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                string summarySql = @"

        SELECT

        COUNT(DISTINCT SC.sc_shop_id) AS total_skip_shop

        FROM shop_check_in SC

        INNER JOIN acc_subhead A
        ON A.as_id = SC.sc_shop_id

        LEFT JOIN gnl_location GL
        ON GL.gl_id = A.as_location_id

        WHERE SC.sc_userid = '" + usqlre.userId + @"'

        AND SC.sc_skip_status = 1

        AND A.as_ap_id = 4
        AND A.as_active = 1
        AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
        AND A.as_rout_id = '" + routeId + @"'

        AND CAST(SC.sc_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'

        ";

                DataTable summary = usqlre.dbReaderFill(summarySql);

                hash.Add("summary", summary);

                string sql = @"

        SELECT DISTINCT

        A.as_id AS shop_id,

        A.as_name AS shop_name,

        ISNULL(GL.gl_name,'') AS location_name,

        ISNULL(R.r_name,'') AS route_name,

        ISNULL(A.as_latitude,'0') AS latitude,
        ISNULL(A.as_longitude,'0') AS longitude,
        ISNULL(A.as_location,'') AS actual_location,


        ISNULL(SC.sc_skip_reason,'') AS skip_reason,
        SC.sc_skip_time

        FROM shop_check_in SC

        INNER JOIN acc_subhead A
        ON A.as_id = SC.sc_shop_id

        LEFT JOIN gnl_location GL
        ON GL.gl_id = A.as_location_id


        LEFT JOIN inv_rout_reg R
        ON R.r_id = A.as_rout_id

        WHERE SC.sc_userid = '" + usqlre.userId + @"'

        AND SC.sc_skip_status = 1

        AND A.as_ap_id = 4
        AND A.as_active = 1
        AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
        AND A.as_rout_id = '" + routeId + @"'

        AND CAST(SC.sc_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'

        ORDER BY A.as_name ASC

        ";

                DataTable skipShop = usqlre.dbReaderFill(sql);

                hash.Add("skip_shop", skipShop);

                string jsonResult = ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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

        [HttpGet("shop-details")]
        public async Task<IActionResult> ShopDetails(int shop_Id)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

                string sql = @"

        SELECT TOP 1

        A.as_id AS shop_id,
        A.as_name AS shop_name,

        ISNULL(A.as_add1,'') AS address1,
        ISNULL(A.as_add2,'') AS address2,
        ISNULL(A.as_add3,'') AS address3,

        ISNULL(A.as_mob,'') AS mobile,

        ISNULL(A.as_tin,'') AS gstin,

        ISNULL(A.as_credit_limit,0) AS credit_limit,
        ISNULL(A.as_credit_days,0) AS credit_days,

        ISNULL(A.as_ob_Cr,0) AS opening_cr,
        ISNULL(A.as_ob_Dr,0) AS opening_dr,

        ISNULL(A.as_latitude,'0') AS latitude,
        ISNULL(A.as_longitude,'0') AS longitude,

        ISNULL(GL.gl_name,'') AS location_name,

        ISNULL(R.r_name,'') AS route_name,
        ISNULL(A.as_location,'') AS actual_location,

        ISNULL(U.gu_name,'') AS salesman_name,

        ISNULL(A.as_city,'') AS city,
        ISNULL(A.as_state,'') AS state,
        ISNULL(A.as_pin,'') AS pincode,

        ISNULL(A.as_mail,'') AS email,
        ISNULL(A.as_whatsapp,'') AS whatsapp,

        ISNULL(A.as_active,0) AS active_status,

        (
            SELECT TOP 1
            CONVERT(VARCHAR,SC.sc_date,23)
            FROM shop_check_in SC
            WHERE SC.sc_shop_id = A.as_id
            ORDER BY SC.sc_id DESC
        ) AS last_visit_date,

        ISNULL((
            SELECT SUM(si_grand_total)
            FROM inv_sales_inf
            WHERE si_acc_id = A.as_id
            AND CAST(si_date AS DATE)=CAST(GETDATE() AS DATE)
        ),0) AS today_order_amount,

        ISNULL((
            SELECT SUM(at_Dr)
            FROM acc_account_transactions
            WHERE at_as_id = A.as_id
            AND CAST(at_date AS DATE)=CAST(GETDATE() AS DATE)
        ),0) AS today_collection_amount

        FROM acc_subhead A

        LEFT JOIN gnl_location GL
        ON GL.gl_id = A.as_location_id

        LEFT JOIN inv_rout_reg R
        ON R.r_id = A.as_rout_id

        LEFT JOIN gnl_users U
        ON U.gu_acc_id = A.as_salesman_id

        WHERE A.as_id = '" + shop_Id + @"'

        AND A.as_ap_id = 4
        AND A.as_active = 1

        ";

                DataTable shopDetails = usqlre.dbReaderFill(sql);

                hash.Add("shop_details", shopDetails);

                string jsonResult = ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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



        [HttpGet("route-start-summary")]
        public async Task<IActionResult> RouteStartSummary(int routeId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

                string sql = @"

        SELECT TOP 1

        ISNULL(R.r_name,'') AS route_name,

        ISNULL(R.r_starting_place,0) AS Startinng_Latitude_Longitude,
        ISNULL(R.r_ending_place,0) AS Ending_Latitude_Longitude,

        ISNULL((
            SELECT COUNT(*)
            FROM acc_subhead A
            WHERE A.as_ap_id = 4
            AND A.as_active = 1
            AND A.as_rout_id = R.r_id
            AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
        ),0) AS total_shop,

        ISNULL((
            SELECT SUM
               (ISNULL(AT.at_Dr,0) - ISNULL(AT.at_Cr,0))
            FROM acc_account_transactions AT
            INNER JOIN acc_subhead A
            ON A.as_id = AT.at_as_id

            WHERE A.as_ap_id = 4  
            AND A.as_active = 1
            AND A.as_rout_id = R.r_id
            AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
            AND AT.at_user_id = '" + usqlre.userId + @"'
        ),0) AS amount_to_collect

        FROM inv_rout_reg R

        WHERE R.r_id = '" + routeId + @"'

        ";

                DataTable routeSummary = usqlre.dbReaderFill(sql);

                hash.Add("route_summary", routeSummary);

                string jsonResult = ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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



[HttpPost("upload-meter-photo")]
public async Task<IActionResult> UploadMeterPhoto(
IFormFile photo,
string historyId)
        {
            UserSqlServer usqlre = null;
            try
            {
                if (photo == null || photo.Length == 0)
                {
                    return Ok(new
                    {
                        status = false,
                        message = "Photo is required"
                    });
                }

                usqlre =
                new UserSqlServer(this);

                string baseUrl;

                using (SqlCommand cmdBase =
                new SqlCommand(
                "SELECT TOP 1 ans_status FROM android_settings WHERE ans_name = 'BaseUrl'",
                usqlre.shop))
                {
                    baseUrl =
                    Convert.ToString(cmdBase.ExecuteScalar());
                }

                // CREATE FOLDER
                string folderPath =
                Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "meterphotos");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // SAVE FILE
                string fileName =
                Guid.NewGuid().ToString()
                + Path.GetExtension(photo.FileName);

                string filePath =
                Path.Combine(folderPath, fileName);

                using (var stream =
                new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                string relativePath =
                "/meterphotos/" + fileName;

                // UPDATE LATEST HISTORY ROW
                string sql = @"

                UPDATE route_tracking_history 
                SET 
                rth_meter_photo = '" + relativePath + @"'
                WHERE rth_id = '" + historyId + @"'
                ";

                usqlre.dbExecute(sql);

                return Ok(new
                {
                    status = true,
                    message = "Photo uploaded successfully",

                    image =
                    baseUrl.TrimEnd('/')
                    + relativePath
                });
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
                usqlre?.close();
            }
        }



        [HttpPost("start-ride")]
        public async Task<IActionResult> StartRide([FromBody] StartRouteRide model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                string checkSql = @"

                SELECT COUNT(*) AS total_count

                FROM route_tracking

                WHERE rt_route_id = '" + model.routeId + @"'
                AND rt_salesman = '" + usqlre.gu_acc_id + @"'
                AND CAST(rt_date AS DATE) = CAST(GETDATE() AS DATE)

                ";

                DataTable checkDt = usqlre.dbReaderFill(checkSql);

                int totalCount = Convert.ToInt32(checkDt.Rows[0]["total_count"]);

                // START RIDE
                if (model.status == "start")
                {
                    if (totalCount > 0)
                    {
                        return StatusCode(400, new
                        {
                            status = false,
                            message = "Ride already started"
                        });
                    }

                    string insertSql = @"

                    INSERT INTO route_tracking
                    (
                        rt_date,
                        rt_salesman,
                        rt_route_id,
                        rt_start_meter_reading,
                        rt_start_time,
                        rt_starting_latitude,
                        rt_starting_longitude
                    )

                    VALUES
                    (
                        GETDATE(),

                        '" + usqlre.gu_acc_id + @"',

                        '" + model.routeId + @"',

                        '" + model.meterReading + @"',

                        CONVERT(VARCHAR(8),GETDATE(),108),

                        '" + model.latitude + @"',

                        '" + model.longitude + @"'
                    )

                    SELECT SCOPE_IDENTITY() AS route_tracking_id

                    ";

                    DataTable insertDt = usqlre.dbReaderFill(insertSql);

                    int routeTrackingId =
                    Convert.ToInt32(insertDt.Rows[0]["route_tracking_id"]);

                    string historySql = @"

                INSERT INTO route_tracking_history
                (
                    rth_route_tracking_id,
                    rth_latitude,
                    rth_longitude,
                    rth_time,
                    rth_status,
                    rth_remark,
                    rth_meter_reading
                )

                VALUES
                (
                    '" + routeTrackingId + @"',

                    '" + model.latitude + @"',

                    '" + model.longitude + @"',

                    CONVERT(VARCHAR(8),GETDATE(),108),

                    'start',
                    '',
                    '" + model.meterReading + @"'
                )
                     SELECT SCOPE_IDENTITY() AS history_id

                ";

                    DataTable historyDt = usqlre.dbReaderFill(historySql);
                    int historyId = Convert.ToInt32(historyDt.Rows[0]["history_id"]);

                    return Ok(new
                    {
                        status = true,
                        message = "Ride started successfully",
                        route_tracking_id = routeTrackingId,
                        history_id = historyId
                    });
                }

                // PAUSE / RESUME / STOP / END
                else
                {
                    string routeSql = @"

                SELECT TOP 1 rt_id

                FROM route_tracking

                WHERE rt_route_id = '" + model.routeId + @"'
                AND rt_salesman = '" + usqlre.gu_acc_id + @"'

                ORDER BY rt_id DESC

                ";

                    DataTable routeDt = usqlre.dbReaderFill(routeSql);

                    if (routeDt.Rows.Count == 0)
                    {
                        return StatusCode(400, new
                        {
                            status = false,
                            message = "Ride not started"
                        });
                    }

                    int routeTrackingId =
                    Convert.ToInt32(routeDt.Rows[0]["rt_id"]);

                    string historySql = @"

                    INSERT INTO route_tracking_history
                    (
                        rth_route_tracking_id,
                        rth_latitude,
                        rth_longitude,
                        rth_time,
                        rth_status,
                        rth_remark,
                        rth_meter_reading
                    )

                    VALUES
                    (
                        '" + routeTrackingId + @"',

                        '" + model.latitude + @"',

                        '" + model.longitude + @"',

                        CONVERT(VARCHAR(8),GETDATE(),108),

                        '" + model.status + @"',

                        '" + model.remark + @"',
                        '" + model.meterReading + @"'
                    )
                        SELECT SCOPE_IDENTITY() AS history_id

                    ";

                    DataTable historyDt = usqlre.dbReaderFill(historySql);
                    int historyId = Convert.ToInt32(historyDt.Rows[0]["history_id"]);

                    // END RIDE
                    if (model.status == "end")
                    {
                        string endSql = @"

                    UPDATE route_tracking

                    SET

                    rt_end_meter_reading = '" + model.meterReading + @"',

                    rt_end_time = CONVERT(VARCHAR(8),GETDATE(),108),

                    rt_ending_latitude = '" + model.latitude + @"',

                    rt_ending_longitude = '" + model.longitude + @"'

                    WHERE rt_id = '" + routeTrackingId + @"'

                    ";

                        usqlre.dbExecute(endSql);
                    }

                    return Ok(new
                    {
                        status = true,
                        message = "Ride " + model.status + " successfully",
                        route_tracking_id = routeTrackingId,
                        history_id = historyId
                    });
                }
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



        [HttpGet("shop-status-list")]
        public async Task<IActionResult> RouteWiseShopStatusList(string routeId, string fromDate, string toDate)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

                if (string.IsNullOrEmpty(fromDate))
                {
                    fromDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                string summarySql = @"

                SELECT

                COUNT(*) AS total_shop,

                ISNULL((
                    SELECT COUNT(DISTINCT SC.sc_shop_id)
                    FROM shop_check_in SC
                    INNER JOIN acc_subhead A
                    ON A.as_id = SC.sc_shop_id

                    WHERE SC.sc_userid = '" + usqlre.userId + @"'
                    AND SC.sc_check_in = 1

                    AND A.as_ap_id = 4
                    AND A.as_active = 1
                    AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
                    AND A.as_rout_id = '" + routeId + @"'

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                ),0) AS total_checkin,

                ISNULL((
                    SELECT COUNT(DISTINCT SC.sc_shop_id)
                    FROM shop_check_in SC
                    INNER JOIN acc_subhead A
                    ON A.as_id = SC.sc_shop_id

                    WHERE SC.sc_userid = '" + usqlre.userId + @"'
                    AND SC.sc_check_in = 2

                    AND A.as_ap_id = 4
                    AND A.as_active = 1
                    AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
                    AND A.as_rout_id = '" + routeId + @"'

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                ),0) AS total_checkout,

                ISNULL((
                    SELECT COUNT(DISTINCT SC.sc_shop_id)
                    FROM shop_check_in SC
                    INNER JOIN acc_subhead A
                    ON A.as_id = SC.sc_shop_id

                    WHERE SC.sc_userid = '" + usqlre.userId + @"'
                    AND SC.sc_skip_status = 1

                    AND A.as_ap_id = 4
                    AND A.as_active = 1
                    AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
                    AND A.as_rout_id = '" + routeId + @"'

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                ),0) AS total_skip

                FROM acc_subhead A

                WHERE A.as_ap_id = 4
                AND A.as_active = 1
                AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
                AND A.as_rout_id = '" + routeId + @"'

                ";

                DataTable summary = usqlre.dbReaderFill(summarySql);

                hash.Add("summary", summary);

                string sql = @"

                SELECT

                A.as_id AS shop_id,
                A.as_name AS shop_name,

                ISNULL(GL.gl_name,'') AS location_name,

                ISNULL(R.r_name,'') AS route_name,

                ISNULL(A.as_mob,'') AS mobile,

                ISNULL(A.as_latitude,'0') AS latitude,
                ISNULL(A.as_longitude,'0') AS longitude,
                ISNULL(A.as_location,'') AS actual_location,

                ISNULL((
                    SELECT TOP 1
                    SC.sc_latitude_in

                    FROM shop_check_in SC

                    WHERE SC.sc_shop_id = A.as_id
                    AND SC.sc_userid = '" + usqlre.userId + @"'

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'

                    ORDER BY SC.sc_id DESC
                ),'0') AS checkin_latitude,


                ISNULL((
                    SELECT TOP 1
                    SC.sc_longitude_in

                    FROM shop_check_in SC

                    WHERE SC.sc_shop_id = A.as_id
                    AND SC.sc_userid = '" + usqlre.userId + @"'

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'

                    ORDER BY SC.sc_id DESC
                ),'0') AS checkin_longitude,

                ISNULL((
                    SELECT TOP 1
                    SC.sc_check_in_time

                    FROM shop_check_in SC

                    WHERE SC.sc_shop_id = A.as_id
                    AND SC.sc_userid = '" + usqlre.userId + @"'

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'

                    ORDER BY SC.sc_id DESC
                ),'') AS checkin_time,


                ISNULL((
                    SELECT TOP 1
                    SC.sc_check_out_time

                    FROM shop_check_in SC

                    WHERE SC.sc_shop_id = A.as_id
                    AND SC.sc_userid = '" + usqlre.userId + @"'

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'

                    ORDER BY SC.sc_id DESC
                ),'') AS checkout_time,


                ISNULL((
                    SELECT TOP 1
                    SC.sc_skip_reason

                    FROM shop_check_in SC

                    WHERE SC.sc_shop_id = A.as_id
                    AND SC.sc_userid = '" + usqlre.userId + @"'
                    AND SC.sc_skip_status = 1

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'

                    ORDER BY SC.sc_id DESC
                ),'') AS skip_remark,


                ISNULL((
                    SELECT TOP 1
                    SC.sc_skip_time

                    FROM shop_check_in SC

                    WHERE SC.sc_shop_id = A.as_id
                    AND SC.sc_userid = '" + usqlre.userId + @"'
                    AND SC.sc_skip_status = 1

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'

                    ORDER BY SC.sc_id DESC
                ),'') AS skip_time,

                CASE

                WHEN EXISTS
                (
                    SELECT 1
                    FROM shop_check_in SC

                    WHERE SC.sc_shop_id = A.as_id
                    AND SC.sc_userid = '" + usqlre.userId + @"'
                    AND SC.sc_skip_status = 1

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                )
                THEN 'Skipped'

                WHEN EXISTS
                (
                    SELECT 1
                    FROM shop_check_in SC

                    WHERE SC.sc_shop_id = A.as_id
                    AND SC.sc_userid = '" + usqlre.userId + @"'
                    AND SC.sc_check_in = 2

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                )
                THEN 'Checked Out'

                WHEN EXISTS
                (
                    SELECT 1
                    FROM shop_check_in SC

                    WHERE SC.sc_shop_id = A.as_id
                    AND SC.sc_userid = '" + usqlre.userId + @"'
                    AND SC.sc_check_in = 1

                    AND CAST(SC.sc_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                )
                THEN 'Checked In'

                ELSE 'Pending'

                END AS shop_status

                FROM acc_subhead A

                LEFT JOIN gnl_location GL
                ON GL.gl_id = A.as_location_id

                LEFT JOIN inv_rout_reg R
                ON R.r_id = A.as_rout_id

                WHERE A.as_ap_id = 4
                AND A.as_active = 1
                AND A.as_salesman_id = '" + usqlre.gu_acc_id + @"'
                AND A.as_rout_id = '" + routeId + @"'

                ORDER BY A.as_name ASC

                ";

                DataTable shopStatus = usqlre.dbReaderFill(sql);

                hash.Add("shop_status_list", shopStatus);

                string jsonResult = ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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

        [HttpPost("shop-checkin")]
        public async Task<IActionResult> ShopCheckin([FromBody] ShopCheckin model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                string checkSql = @"

        SELECT COUNT(*) AS total_count

        FROM shop_check_in

        WHERE sc_shop_id = '" + model.shopId + @"'
        AND sc_userid = '" + usqlre.userId + @"'
        AND sc_check_in IN (1,2)

        AND CAST(sc_date AS DATE) =
        CAST(GETDATE() AS DATE)

        ";

                DataTable checkDt = usqlre.dbReaderFill(checkSql);

                int totalCount = Convert.ToInt32(checkDt.Rows[0]["total_count"]);


                string rideStatusSql = @"

                SELECT TOP 1
                RTH.rth_status

                FROM route_tracking_history RTH

                INNER JOIN route_tracking RT
                ON RT.rt_id = RTH.rth_route_tracking_id

                WHERE RT.rt_salesman = '" + usqlre.gu_acc_id + @"'
                AND RT.rt_route_id = '" + model.routeCheckInId + @"'

                AND CAST(RT.rt_date AS DATE) =
                CAST(GETDATE() AS DATE)

                ORDER BY RTH.rth_id DESC

                ";

                DataTable rideStatusDt =
                usqlre.dbReaderFill(rideStatusSql);

                string rideStatus = "";

                if (rideStatusDt.Rows.Count > 0)
                {
                    rideStatus =
                    rideStatusDt.Rows[0]["rth_status"].ToString()
                    .ToLower();
                }

                if
                (
                    rideStatus == "pause"
                    ||
                    rideStatus == "stop"
                    ||
                    rideStatus == "end"
                )
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message =
                        "Shop checkin not allowed while ride is "
                        + rideStatus
                    });
                }

                if (totalCount > 0)
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message = "Already checked in"
                    });
                }

                string sql = @"

        INSERT INTO shop_check_in
        (
            sc_date,
            sc_check_in_time,
            sc_userid,
            sc_route_id,
            sc_shop_id,
            sc_latitude_in,
            sc_longitude_in,
            sc_check_in,
            sc_location_id
        )

        VALUES
        (
            GETDATE(),
             CONVERT(VARCHAR(8),GETDATE(),108),
            '" + usqlre.userId + @"',
            '" + model.routeCheckInId + @"',
            '" + model.shopId + @"',
            '" + model.latitude + @"',
            '" + model.longitude + @"',
            1,
            '" + usqlre.locationId + @"'
        )

        ";

                usqlre.dbReaderFill(sql);

                return Ok(new
                {
                    status = true,
                    message = "Shop checkin successfully"
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

        [HttpPost("shop-checkout")]
        public async Task<IActionResult> ShopCheckout([FromBody] ShopCheckOut model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                string rideStatusSql = @"

                SELECT TOP 1
                RTH.rth_status

                FROM route_tracking_history RTH

                INNER JOIN route_tracking RT
                ON RT.rt_id = RTH.rth_route_tracking_id

                WHERE RT.rt_salesman = '" + usqlre.gu_acc_id + @"'
                AND RT.rt_route_id = '" + model.route_id + @"'

                AND CAST(RT.rt_date AS DATE) =
                CAST(GETDATE() AS DATE)

                ORDER BY RTH.rth_id DESC

                ";

                DataTable rideStatusDt =
                usqlre.dbReaderFill(rideStatusSql);

                string rideStatus = "";

                if (rideStatusDt.Rows.Count > 0)
                {
                    rideStatus =
                    rideStatusDt.Rows[0]["rth_status"]
                    .ToString()
                    .ToLower();
                }

                if
                (
                    rideStatus == "pause"
                    ||
                    rideStatus == "stop"
                    ||
                    rideStatus == "end"
                )
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message =
                        "Shop checkout not allowed while ride is "
                        + rideStatus
                    });
                }

                string sql = @"

        UPDATE shop_check_in

        SET

        sc_check_out_time = CONVERT(VARCHAR(8),GETDATE(),108),

        sc_latitude_out = '" + model.latitude + @"',

        sc_longitude_out = '" + model.longitude + @"',

        sc_check_in = 2

        WHERE sc_id =
        (
            SELECT TOP 1 sc_id

            FROM shop_check_in

            WHERE sc_userid = '" + usqlre.userId + @"'

            AND sc_route_id = '" + model.route_id + @"'

            AND sc_shop_id = '" + model.shop_id + @"'

            AND sc_check_in = 1

            ORDER BY sc_id DESC
        )

        ";

                Console.WriteLine(sql);

                usqlre.dbExecute(sql);

                return Ok(new
                {
                    status = true,
                    message = "Shop checkout successfully"
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


        [HttpPost("skip-shop")]
        public async Task<IActionResult> SkipShop([FromBody] SkipShop model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                string checkSql = @"

                SELECT COUNT(*) AS total_count

                FROM shop_check_in

                WHERE sc_shop_id = '" + model.shop_id + @"'

                AND sc_userid = '" + usqlre.userId + @"'

                AND
                (
                    sc_check_in IN (1,2)
                    OR sc_skip_status = 1
                )

                AND CAST(sc_date AS DATE) =
                CAST(GETDATE() AS DATE)

                ";

                DataTable checkDt = usqlre.dbReaderFill(checkSql);

                int totalCount = Convert.ToInt32(checkDt.Rows[0]["total_count"]);

                string rideStatusSql = @"

                SELECT TOP 1
                RTH.rth_status

                FROM route_tracking_history RTH

                INNER JOIN route_tracking RT
                ON RT.rt_id = RTH.rth_route_tracking_id

                WHERE RT.rt_salesman = '" + usqlre.gu_acc_id + @"'
                AND RT.rt_route_id = '" + model.route_id + @"'

                AND CAST(RT.rt_date AS DATE) =
                CAST(GETDATE() AS DATE)

                ORDER BY RTH.rth_id DESC

                ";

                DataTable rideStatusDt =
                usqlre.dbReaderFill(rideStatusSql);

                string rideStatus = "";

                if (rideStatusDt.Rows.Count > 0)
                {
                    rideStatus =
                    rideStatusDt.Rows[0]["rth_status"]
                    .ToString()
                    .ToLower();
                }

                if
                (
                    rideStatus == "pause"
                    ||
                    rideStatus == "stop"
                    ||
                    rideStatus == "end"
                )
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message =
                        "Shop skip not allowed while ride is "
                        + rideStatus
                    });
                }

                if (totalCount > 0)
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message = "Already checkedin / checkout / skipped"
                    });
                }


                string sql = @"

        INSERT INTO shop_check_in
        (
            sc_date,
            sc_userid,
            sc_route_id,
            sc_shop_id,
            sc_latitude_in,
            sc_longitude_in,
            sc_check_in,
            sc_skip_status,
            sc_skip_reason,
            sc_skip_time
        )

        VALUES
        (
            GETDATE(),

            '" + usqlre.userId + @"',

            '" + model.route_id + @"',

            '" + model.shop_id + @"',

            '" + model.latitude + @"',

            '" + model.longitude + @"',

            0,

            1,

            '" + model.skip_reason + @"',

            CONVERT(VARCHAR(8),GETDATE(),108)
        )

        ";

                Console.WriteLine(sql);

                usqlre.dbExecute(sql);

                return Ok(new
                {
                    status = true,
                    message = "Shop skipped successfully"
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

        [HttpGet("salesman-locations")]
        public async Task<IActionResult> SalesmanLocations(
    int salesmanId,
    string toDate)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                string sql = @"

;WITH LocationData AS
(

    /* START RIDE */
    SELECT
        RT.rt_starting_latitude AS latitude,
        RT.rt_starting_longitude AS longitude,

        CAST(
            CAST(RT.rt_date AS DATE) AS DATETIME
        )
        +
        CAST(RT.rt_start_time AS DATETIME) AS sort_datetime,

        RT.rt_id AS sort_id

    FROM route_tracking RT

    WHERE RT.rt_salesman = '" + salesmanId + @"'

    AND CAST(RT.rt_date AS DATE)
        = CAST('" + toDate + @"' AS DATE)

    AND RT.rt_starting_latitude IS NOT NULL
    AND RT.rt_starting_longitude IS NOT NULL


    UNION ALL


    /* PAUSE / RESUME / STOP / END */
    SELECT
        RTH.rth_latitude AS latitude,
        RTH.rth_longitude AS longitude,

        CAST(
            CAST(RT.rt_date AS DATE) AS DATETIME
        )
        +
        CAST(RTH.rth_time AS DATETIME) AS sort_datetime,

        RTH.rth_id AS sort_id

    FROM route_tracking_history RTH

    INNER JOIN route_tracking RT
        ON RT.rt_id = RTH.rth_route_tracking_id

    WHERE RT.rt_salesman = '" + salesmanId + @"'

    AND CAST(RT.rt_date AS DATE)
        = CAST('" + toDate + @"' AS DATE)

    AND LOWER(RTH.rth_status) <> 'start'

    AND RTH.rth_latitude IS NOT NULL
    AND RTH.rth_longitude IS NOT NULL


    UNION ALL


    /* SHOP CHECK-IN */
    SELECT
        SC.sc_latitude_in AS latitude,
        SC.sc_longitude_in AS longitude,

        CAST(
            CAST(SC.sc_date AS DATE) AS DATETIME
        )
        +
        CAST(SC.sc_check_in_time AS DATETIME) AS sort_datetime,

        SC.sc_id AS sort_id

    FROM shop_check_in SC

    INNER JOIN gnl_users U
        ON U.gu_user_id = SC.sc_userid

    WHERE U.gu_acc_id = '" + salesmanId + @"'

    AND CAST(SC.sc_date AS DATE)
        = CAST('" + toDate + @"' AS DATE)

    AND SC.sc_latitude_in IS NOT NULL
    AND SC.sc_longitude_in IS NOT NULL


    UNION ALL


    /* SHOP CHECK-OUT */
    SELECT
        SC.sc_latitude_out AS latitude,
        SC.sc_longitude_out AS longitude,

        CAST(
            CAST(SC.sc_date AS DATE) AS DATETIME
        )
        +
        CAST(SC.sc_check_out_time AS DATETIME) AS sort_datetime,

        SC.sc_id AS sort_id

    FROM shop_check_in SC

    INNER JOIN gnl_users U
        ON U.gu_user_id = SC.sc_userid

    WHERE U.gu_acc_id = '" + salesmanId + @"'

    AND CAST(SC.sc_date AS DATE)
        = CAST('" + toDate + @"' AS DATE)

    AND SC.sc_check_out_time IS NOT NULL

    AND SC.sc_latitude_out IS NOT NULL
    AND SC.sc_longitude_out IS NOT NULL

)

SELECT
    latitude,
    longitude

FROM LocationData

ORDER BY
    sort_datetime ASC,
    sort_id ASC

";

                DataTable locations =
                    usqlre.dbReaderFill(sql);

                Dictionary<string, DataTable> hash =
                    new Dictionary<string, DataTable>();

                hash.Add("salesman_locations", locations);

                string jsonResult =
                    ReportModelContext.searializeDt(hash);

                return Content(
                    jsonResult,
                    "application/json"
                );
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


        [HttpPost("shop-action-skip")]
        public async Task<IActionResult> ShopActionSkip([FromBody] ShopActionSkip model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);


                // CHECK SHOP CHECKIN COMPLETED
                string checkinSql = @"

        SELECT COUNT(*) AS total_count

        FROM shop_check_in

        WHERE sc_shop_id = '" + model.shop_id + @"'

        AND sc_userid = '" + usqlre.userId + @"'

        AND sc_check_in = 1

        AND CAST(sc_date AS DATE) =
        CAST(GETDATE() AS DATE)

        ";

                DataTable checkinDt =
                usqlre.dbReaderFill(checkinSql);

                int checkinCount =
                Convert.ToInt32(checkinDt.Rows[0]["total_count"]);

                if (checkinCount == 0)
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message = "Please checkin shop first"
                    });
                }


                // CHECK SHOP ALREADY CHECKOUT
                string checkoutSql = @"

        SELECT COUNT(*) AS total_count

        FROM shop_check_in

        WHERE sc_shop_id = '" + model.shop_id + @"'

        AND sc_userid = '" + usqlre.userId + @"'

        AND sc_check_in = 2

        AND CAST(sc_date AS DATE) =
        CAST(GETDATE() AS DATE)

        ";

                DataTable checkoutDt =
                usqlre.dbReaderFill(checkoutSql);

                int checkoutCount =
                Convert.ToInt32(checkoutDt.Rows[0]["total_count"]);

                if (checkoutCount > 0)
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message = "Shop already checkout completed"
                    });
                }

                string checkSql = @"

        SELECT COUNT(*) AS total_count

        FROM shop_action_skip

        WHERE sas_shop_id = '" + model.shop_id + @"'

        AND sas_user_id = '" + usqlre.userId + @"'

        AND sas_action_type = '" + model.action_type + @"'

        AND CAST(sas_date AS DATE) =
        CAST(GETDATE() AS DATE)

        ";

                DataTable checkDt = usqlre.dbReaderFill(checkSql);

                int totalCount =
                Convert.ToInt32(checkDt.Rows[0]["total_count"]);

                if (totalCount > 0)
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message = model.action_type + " already skipped"
                    });
                }

                string sql = @"

        INSERT INTO shop_action_skip
        (
            sas_date,
            sas_user_id,
            sas_route_id,
            sas_shop_id,
            sas_action_type,
            sas_skip_reason,
            sas_skip_time
        )

        VALUES
        (
            GETDATE(),

            '" + usqlre.userId + @"',

            '" + model.route_id + @"',

            '" + model.shop_id + @"',

            '" + model.action_type + @"',

            '" + model.skip_reason + @"',

            CONVERT(VARCHAR(8),GETDATE(),108)
        )

        ";

                usqlre.dbExecute(sql);

                return Ok(new
                {
                    status = true,
                    message = model.action_type + " skipped successfully"
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


        [HttpGet("salesman-ride-status-list")]
        public async Task<IActionResult> SalesmanRideStatusList()
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                Dictionary<string, DataTable> hash =
                new Dictionary<string, DataTable>();

                string summarySql = @"

        SELECT

        COUNT(DISTINCT A.as_salesman_id)
        AS total_salesman

        FROM acc_subhead A

        WHERE A.as_ap_id = 4
        AND A.as_active = 1

        AND ISNULL(A.as_salesman_id,0) > 0

        ";

                DataTable summary =
                usqlre.dbReaderFill(summarySql);

                hash.Add("summary", summary);

                string sql = @"

        SELECT DISTINCT

        U.gu_acc_id AS salesman_id,

        ISNULL(U.gu_name,'') AS salesman_name,

        ISNULL(R.r_name,'') AS route_name,
        ISNULL(R.r_id,0) AS route_id,

        ISNULL((
            SELECT COUNT(*)

            FROM acc_subhead A2

            WHERE A2.as_ap_id = 4
            AND A2.as_active = 1

            AND A2.as_salesman_id = U.gu_acc_id
            AND A2.as_rout_id = R.r_id

        ),0) AS total_shop,


        ISNULL(RT.rt_start_meter_reading,0)
        AS start_meter_reading,

        ISNULL(RT.rt_end_meter_reading,0)
        AS end_meter_reading,

        ISNULL(RT.rt_start_time,'')
        AS ride_start_time,

        ISNULL(RT.rt_end_time,'')
        AS ride_end_time,

        ISNULL(RT.rt_starting_latitude,'0')
        AS start_latitude,

        ISNULL(RT.rt_starting_longitude,'0')
        AS start_longitude,

        ISNULL(RT.rt_ending_latitude,'0')
        AS end_latitude,

        ISNULL(RT.rt_ending_longitude,'0')
        AS end_longitude,



        ISNULL((
        SELECT TOP 1
        RTH.rth_status

        FROM route_tracking_history RTH

        WHERE RTH.rth_route_tracking_id = RT.rt_id

        ORDER BY RTH.rth_id DESC

        ),'not_started') AS ride_status




        FROM gnl_users U

        INNER JOIN acc_subhead A
        ON A.as_salesman_id = U.gu_acc_id

        LEFT JOIN inv_rout_reg R
        ON R.r_id = A.as_rout_id

        LEFT JOIN route_tracking RT
        ON RT.rt_salesman = U.gu_acc_id
        AND RT.rt_route_id = R.r_id
        AND CAST(RT.rt_date AS DATE) =
        CAST(GETDATE() AS DATE)

        WHERE A.as_ap_id = 4
        AND A.as_active = 1

        ORDER BY salesman_name ASC

        ";

                DataTable salesmanList =
                usqlre.dbReaderFill(sql);

                hash.Add("salesman_ride_list", salesmanList);

                string jsonResult =
                ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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


        [HttpGet("admin-dashboard")]
        public async Task<IActionResult> AdminDashboard(
 string salesmanId,
 string fromDate,
 string toDate,
 int routeId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                if (usqlre.user_role != "ADMIN")
                {
                    return StatusCode(403, new
                    {
                        status = false,
                        message = "Access denied"
                    });
                }

                if (string.IsNullOrEmpty(fromDate))
                {
                    fromDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                Dictionary<string, DataTable> hash =
                new Dictionary<string, DataTable>();

                string sql = @"

        SELECT TOP 1

        U.gu_acc_id AS salesman_id,

        ISNULL(U.gu_name,'') AS salesman_name,

        ISNULL(R.r_name,'') AS route_name,

        ISNULL(R.r_starting_place,'')
        AS route_starting_place,

        ISNULL(R.r_ending_place,'')
        AS route_ending_place,

        ISNULL(RT.rt_start_meter_reading,0)
        AS start_meter_reading,

        ISNULL(RT.rt_end_meter_reading,0)
        AS end_meter_reading,

        ISNULL(RT.rt_start_time,'')
        AS ride_start_time,

        ISNULL(RT.rt_end_time,'')
        AS ride_end_time,

        ISNULL((
        SELECT TOP 1
        RTH.rth_status

        FROM route_tracking_history RTH

        WHERE RTH.rth_route_tracking_id = RT.rt_id

        ORDER BY RTH.rth_id DESC

        ),'not_started') AS ride_status,

        ISNULL((
        SELECT COUNT(*)

        FROM acc_subhead A2

        WHERE A2.as_ap_id = 4
        AND A2.as_active = 1
        AND A2.as_salesman_id = U.gu_acc_id
        AND A2.as_rout_id = R.r_id

        ),0) AS total_shop,

        ISNULL((
        SELECT COUNT(DISTINCT SC.sc_shop_id)

        FROM shop_check_in SC

        WHERE SC.sc_userid = U.gu_user_id
        AND SC.sc_check_in = 2
        AND SC.sc_route_id = '" + routeId + @"'

        AND CAST(SC.sc_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'

        ),0) AS shop_covered,

                ISNULL((
        SELECT COUNT(DISTINCT SC.sc_shop_id)

        FROM shop_check_in SC

        WHERE SC.sc_userid = U.gu_user_id
        AND SC.sc_skip_status = 1
        AND SC.sc_route_id = '" + routeId + @"'

        AND CAST(SC.sc_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'

        ),0) AS skip_shop,

        ISNULL(( SELECT SUM(SI.si_grand_total)
        FROM inv_sales_inf SI
        inner join acc_subhead A1
        on A1.as_id = SI.si_acc_id
        WHERE SI.si_user_id = U.gu_user_id
        AND SI.si_str_id IN (1,2,6)
        AND A1.as_rout_id = '" + routeId + @"' 
        AND CAST(SI.si_date AS DATE) BETWEEN '" + fromDate + @"' AND '" + toDate + @"' ),0)
        AS total_sales,

        ISNULL(( SELECT SUM(SI.si_grand_total)
        FROM inv_sales_inf SI
        INNER JOIN acc_subhead A1
        ON A1.as_id = SI.si_acc_id
        WHERE SI.si_user_id = U.gu_user_id 
        AND SI.si_str_id = 3
        AND A1.as_rout_id = '" + routeId + @"'
        AND CAST(SI.si_date AS DATE) BETWEEN '" + fromDate + @"' AND '" + toDate + @"' ),0)
        AS total_order_value,


        ( ISNULL(( SELECT SUM(SI.si_grand_total)
        FROM inv_sales_inf SI
        INNER JOIN acc_subhead A1
        ON A1.as_id = SI.si_acc_id
        WHERE SI.si_user_id = U.gu_user_id
        AND SI.si_str_id IN (1,2,6)
        AND A1.as_rout_id = '" + routeId + @"'
        AND CAST(SI.si_date AS DATE) BETWEEN '" + fromDate + @"' AND '" + toDate + @"' ),0)
        -
        ISNULL(( SELECT SUM(SI.si_grand_total)
        FROM inv_sales_inf SI
        INNER JOIN acc_subhead A1
        ON A1.as_id = SI.si_acc_id
        WHERE SI.si_user_id = U.gu_user_id
        AND SI.si_str_id = 7 
        AND A1.as_rout_id = '" + routeId + @"'
        AND CAST(SI.si_date AS DATE)
        BETWEEN '" + fromDate + @"' AND '" + toDate + @"' ),0) )
        AS net_sales,


        ISNULL(( SELECT SUM(SI.si_grand_total)
        FROM inv_sales_inf SI
        INNER JOIN acc_subhead A1
        ON A1.as_id = SI.si_acc_id
        WHERE SI.si_user_id = U.gu_user_id
        AND SI.si_str_id = 7
        AND A1.as_rout_id = '" + routeId + @"'
        AND CAST(SI.si_date AS DATE)
        BETWEEN '" + fromDate + @"' AND '" + toDate + @"' ),0)
        AS total_return, 


        ISNULL(( SELECT COUNT(*)
        FROM inv_sales_inf SI
        INNER JOIN acc_subhead A1
        ON A1.as_id = SI.si_acc_id
        WHERE SI.si_user_id = U.gu_user_id
        AND SI.si_str_id = 3
        AND A1.as_rout_id = '" + routeId + @"'
        AND CAST(SI.si_date AS DATE)
        BETWEEN '" + fromDate + @"' AND '" + toDate + @"' ),0)
        AS total_order_count, 



        ISNULL((
        SELECT SUM(AT.at_Cr)

        FROM acc_account_transactions AT
        INNER JOIN acc_subhead A1
        ON A1.as_id = AT.at_as_id

        WHERE AT.at_user_id = U.gu_user_id
        AND A1.as_rout_id = '" + routeId + @"'

        AND CAST(AT.at_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'

        ),0) AS total_collection





        FROM gnl_users U

        LEFT JOIN acc_subhead A
        ON A.as_salesman_id = U.gu_acc_id

        LEFT JOIN inv_rout_reg R
        ON R.r_id = A.as_rout_id

        LEFT JOIN route_tracking RT
        ON RT.rt_id =
        (
            SELECT TOP 1 RT2.rt_id

            FROM route_tracking RT2

            WHERE RT2.rt_salesman = U.gu_acc_id

            ORDER BY RT2.rt_id DESC
        )

        WHERE U.gu_acc_id = '" + salesmanId + @"'
        AND R.r_id = " + routeId + @"
        ORDER BY U.gu_name

        ";

                DataTable dashboard =
                usqlre.dbReaderFill(sql);

                hash.Add("salesman_dashboard", dashboard);


                string recentVisitSql = @"

                SELECT TOP 10

                A.as_name AS shop_name,

                ISNULL(GL.gl_name,'') AS location_name,

                ISNULL(R.r_name,'') AS route_name,

                SC.sc_check_in_time,
                SC.sc_check_out_time,

                SC.sc_latitude_in,
                SC.sc_longitude_in,

                SC.sc_latitude_out,
                SC.sc_longitude_out

                FROM shop_check_in SC

                INNER JOIN acc_subhead A
                ON A.as_id = SC.sc_shop_id

                LEFT JOIN gnl_location GL
                ON GL.gl_id = A.as_location_id

                LEFT JOIN inv_rout_reg R
                ON R.r_id = A.as_rout_id

                INNER JOIN gnl_users U
                ON U.gu_user_id = SC.sc_userid

                WHERE U.gu_acc_id = '" + salesmanId + @"'
                AND A.as_rout_id = '" + routeId + @"'

                AND SC.sc_check_in = 2

                AND CAST(SC.sc_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ORDER BY SC.sc_id DESC

                ";

                DataTable recentVisit =
                usqlre.dbReaderFill(recentVisitSql);

                hash.Add("recent_visit", recentVisit);

                string jsonResult =
                ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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

        [HttpGet("admin-dashboard-new")]
        public async Task<IActionResult> AdminDashboardNew(
string salesmanId,
string fromDate,
string toDate,
int routeId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                if (usqlre.user_role != "ADMIN")
                {
                    return StatusCode(403, new
                    {
                        status = false,
                        message = "Access denied"
                    });
                }

                if (string.IsNullOrEmpty(fromDate))
                {
                    fromDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                Dictionary<string, DataTable> hash =
                new Dictionary<string, DataTable>();

                string sql = @"

SELECT TOP 1

U.gu_acc_id AS salesman_id,

ISNULL(U.gu_name,'') AS salesman_name,

ISNULL(R.as_name,'') AS route_name,

'' AS route_starting_place,

'' AS route_ending_place,

ISNULL(RT.rt_start_meter_reading,0)
AS start_meter_reading,

ISNULL(RT.rt_end_meter_reading,0)
AS end_meter_reading,

ISNULL(RT.rt_start_time,'')
AS ride_start_time,

ISNULL(RT.rt_end_time,'')
AS ride_end_time,

ISNULL((
    SELECT TOP 1
    RTH.rth_status
    FROM route_tracking_history RTH
    WHERE RTH.rth_route_tracking_id = RT.rt_id
    ORDER BY RTH.rth_id DESC
),'not_started') AS ride_status,


ISNULL((
    SELECT COUNT(*)
    FROM acc_subhead A2
    WHERE A2.as_ap_id = 4
    AND A2.as_active = 1
    AND A2.as_salesman_id = U.gu_acc_id
    AND A2.as_rout_id = '" + routeId + @"'
),0) AS total_shop,


ISNULL((
    SELECT COUNT(DISTINCT SC.sc_shop_id)
    FROM shop_check_in SC
    WHERE SC.sc_userid = U.gu_user_id
    AND SC.sc_check_in = 2
    AND SC.sc_route_id = '" + routeId + @"'
    AND CAST(SC.sc_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'
),0) AS shop_covered,


ISNULL((
    SELECT COUNT(DISTINCT SC.sc_shop_id)
    FROM shop_check_in SC
    WHERE SC.sc_userid = U.gu_user_id
    AND SC.sc_skip_status = 1
    AND SC.sc_route_id = '" + routeId + @"'
    AND CAST(SC.sc_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'
),0) AS skip_shop,


ISNULL((
    SELECT SUM(SI.si_grand_total)
    FROM inv_sales_inf SI
    INNER JOIN acc_subhead A1
        ON A1.as_id = SI.si_acc_id
    WHERE SI.si_user_id = U.gu_user_id
    AND SI.si_str_id IN (1,2,6)
    AND A1.as_rout_id = '" + routeId + @"'
    AND CAST(SI.si_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'
),0) AS total_sales,


ISNULL((
    SELECT SUM(SI.si_grand_total)
    FROM inv_sales_inf SI
    INNER JOIN acc_subhead A1
        ON A1.as_id = SI.si_acc_id
    WHERE SI.si_user_id = U.gu_user_id
    AND SI.si_str_id = 3
    AND A1.as_rout_id = '" + routeId + @"'
    AND CAST(SI.si_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'
),0) AS total_order_value,


(
    ISNULL((
        SELECT SUM(SI.si_grand_total)
        FROM inv_sales_inf SI
        INNER JOIN acc_subhead A1
            ON A1.as_id = SI.si_acc_id
        WHERE SI.si_user_id = U.gu_user_id
        AND SI.si_str_id IN (1,2,6)
        AND A1.as_rout_id = '" + routeId + @"'
        AND CAST(SI.si_date AS DATE)
            BETWEEN '" + fromDate + @"'
            AND '" + toDate + @"'
    ),0)

    -

    ISNULL((
        SELECT SUM(SI.si_grand_total)
        FROM inv_sales_inf SI
        INNER JOIN acc_subhead A1
            ON A1.as_id = SI.si_acc_id
        WHERE SI.si_user_id = U.gu_user_id
        AND SI.si_str_id = 7
        AND A1.as_rout_id = '" + routeId + @"'
        AND CAST(SI.si_date AS DATE)
            BETWEEN '" + fromDate + @"'
            AND '" + toDate + @"'
    ),0)

) AS net_sales,


ISNULL((
    SELECT SUM(SI.si_grand_total)
    FROM inv_sales_inf SI
    INNER JOIN acc_subhead A1
        ON A1.as_id = SI.si_acc_id
    WHERE SI.si_user_id = U.gu_user_id
    AND SI.si_str_id = 7
    AND A1.as_rout_id = '" + routeId + @"'
    AND CAST(SI.si_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'
),0) AS total_return,


ISNULL((
    SELECT COUNT(*)
    FROM inv_sales_inf SI
    INNER JOIN acc_subhead A1
        ON A1.as_id = SI.si_acc_id
    WHERE SI.si_user_id = U.gu_user_id
    AND SI.si_str_id = 3
    AND A1.as_rout_id = '" + routeId + @"'
    AND CAST(SI.si_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'
),0) AS total_order_count,


ISNULL((
    SELECT SUM(AT.at_Cr)
    FROM acc_account_transactions AT
    INNER JOIN acc_subhead A1
        ON A1.as_id = AT.at_as_id
    WHERE AT.at_user_id = U.gu_user_id
    AND A1.as_rout_id = '" + routeId + @"'
    AND CAST(AT.at_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'
),0) AS total_collection


FROM gnl_users U

LEFT JOIN acc_subhead A
    ON A.as_salesman_id = U.gu_acc_id

LEFT JOIN acc_subhead R
    ON R.as_rout_id = '" + routeId + @"'

LEFT JOIN route_tracking RT
    ON RT.rt_id =
    (
        SELECT TOP 1 RT2.rt_id
        FROM route_tracking RT2
        WHERE RT2.rt_salesman = U.gu_acc_id
        ORDER BY RT2.rt_id DESC
    )

WHERE U.gu_acc_id = '" + salesmanId + @"'
AND R.as_rout_id = " + routeId + @"

ORDER BY U.gu_name

";

                DataTable dashboard =
                    usqlre.dbReaderFill(sql);

                hash.Add("salesman_dashboard", dashboard);


                string recentVisitSql = @"

                SELECT TOP 10

                A.as_name AS shop_name,

                ISNULL(GL.gl_name,'') AS location_name,

                ISNULL(R.r_name,'') AS route_name,

                SC.sc_check_in_time,
                SC.sc_check_out_time,

                SC.sc_latitude_in,
                SC.sc_longitude_in,

                SC.sc_latitude_out,
                SC.sc_longitude_out,
                
                A.as_latitude,
                A.as_longitude

                FROM shop_check_in SC

                INNER JOIN acc_subhead A
                ON A.as_id = SC.sc_shop_id

                LEFT JOIN gnl_location GL
                ON GL.gl_id = A.as_location_id

                LEFT JOIN inv_rout_reg R
                ON R.r_id = A.as_rout_id

                INNER JOIN gnl_users U
                ON U.gu_user_id = SC.sc_userid

                WHERE U.gu_acc_id = '" + salesmanId + @"'
                AND A.as_rout_id = '" + routeId + @"'

                AND SC.sc_check_in = 2

                AND CAST(SC.sc_date AS DATE)
                BETWEEN '" + fromDate + @"'
                AND '" + toDate + @"'

                ORDER BY SC.sc_id DESC

                ";

                DataTable recentVisit =
                usqlre.dbReaderFill(recentVisitSql);

                hash.Add("recent_visit", recentVisit);

                string jsonResult =
                ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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



        [HttpGet("salesman-total-shop-list")]
        public async Task<IActionResult> SalesmanTotalShopList(
 string salesmanId,
 string routeId,
 string toDate)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                if (usqlre.user_role != "ADMIN")
                {
                    return StatusCode(403, new
                    {
                        status = false,
                        message = "Access denied"
                    });
                }
                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                Dictionary<string, DataTable> hash =
                new Dictionary<string, DataTable>();

                string summarySql = @"

SELECT

COUNT(*) AS total_shop

FROM acc_subhead A

WHERE A.as_ap_id = 4
AND A.as_active = 1
AND A.as_salesman_id = '" + salesmanId + @"'
AND A.as_rout_id = '" + routeId + @"'
AND CAST(A.as_date AS DATE) <= '" + toDate + @"'

";

                DataTable summary =
                usqlre.dbReaderFill(summarySql);

                hash.Add("summary", summary);

                string sql = @"

SELECT

A.as_id AS shop_id,
A.as_name AS shop_name,

ISNULL(GL.gl_name,'') AS location_name,

ISNULL(A.as_mob,'') AS mobile,

ISNULL(A.as_latitude,'0') AS latitude,
ISNULL(A.as_longitude,'0') AS longitude,

ISNULL(R.r_name,'') AS route_name,

ISNULL(A.as_add1,'') AS address1,
ISNULL(A.as_add2,'') AS address2,
ISNULL(A.as_add3,'') AS address3,
ISNULL(A.as_location,'') AS actual_location

FROM acc_subhead A

LEFT JOIN gnl_location GL
ON GL.gl_id = A.as_location_id

LEFT JOIN inv_rout_reg R
ON R.r_id = A.as_rout_id

WHERE A.as_ap_id = 4
AND A.as_active = 1
AND A.as_salesman_id = '" + salesmanId + @"'

AND A.as_rout_id = '" + routeId + @"'
AND CAST(A.as_date AS DATE) <= '" + toDate + @"'

ORDER BY A.as_name ASC

";

                DataTable totalShop =
                usqlre.dbReaderFill(sql);

                hash.Add("total_shop", totalShop);

                string jsonResult =
                ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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

        [HttpGet("salesman-pending-shop-list")]
        public async Task<IActionResult> SalesmanPendingShopList(
string salesmanId,
string toDate,
string routeId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                if (usqlre.user_role != "ADMIN")
                {
                    return StatusCode(403, new
                    {
                        status = false,
                        message = "Access denied"
                    });
                }

                Dictionary<string, DataTable> hash =
                new Dictionary<string, DataTable>();

                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                string userSql = @"

        SELECT TOP 1 gu_user_id

        FROM gnl_users

        WHERE gu_acc_id = '" + salesmanId + @"'

        ";

                DataTable userDt =
                usqlre.dbReaderFill(userSql);

                if (userDt.Rows.Count == 0)
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message = "Salesman not found"
                    });
                }

                string userId =
                userDt.Rows[0]["gu_user_id"].ToString();

                string summarySql = @"

SELECT

COUNT(*) AS total_pending_shop

FROM acc_subhead A

WHERE A.as_ap_id = 4
AND A.as_active = 1

AND A.as_salesman_id = '" + salesmanId + @"'
AND A.as_rout_id = '" + routeId + @"'

AND NOT EXISTS
(
    SELECT 1

    FROM shop_check_in SC

    WHERE SC.sc_shop_id = A.as_id

    AND SC.sc_userid = '" + userId + @"'

    AND
    (
        SC.sc_check_in = 2
        OR SC.sc_skip_status = 1
    )

    AND CAST(SC.sc_date AS DATE) =
    '" + toDate + @"'
)

";

                DataTable summary =
                usqlre.dbReaderFill(summarySql);

                hash.Add("summary", summary);

                string sql = @"

SELECT

A.as_id AS shop_id,
A.as_name AS shop_name,

ISNULL(GL.gl_name,'') AS location_name,
ISNULL(R.r_name,'') AS route_name,

ISNULL(A.as_latitude,'0') AS latitude,
ISNULL(A.as_longitude,'0') AS longitude,
ISNULL(A.as_location,'') AS actual_location

FROM acc_subhead A

LEFT JOIN gnl_location GL
ON GL.gl_id = A.as_location_id

LEFT JOIN inv_rout_reg R
ON R.r_id = A.as_rout_id

WHERE A.as_ap_id = 4
AND A.as_active = 1

AND A.as_salesman_id = '" + salesmanId + @"'
AND A.as_rout_id = '" + routeId + @"'

AND NOT EXISTS
(
    SELECT 1

    FROM shop_check_in SC

    WHERE SC.sc_shop_id = A.as_id

    AND SC.sc_userid = '" + userId + @"'

    AND
    (
        SC.sc_check_in = 2
        OR SC.sc_skip_status = 1
    )

    AND CAST(SC.sc_date AS DATE) =
    '" + toDate + @"'
)

ORDER BY A.as_name ASC

";

                DataTable pendingShop =
                usqlre.dbReaderFill(sql);

                hash.Add("pending_shop", pendingShop);

                string jsonResult =
                ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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


        [HttpGet("salesman-shop-covered-list")]
        public async Task<IActionResult> SalesmanShopCoveredList(
string salesmanId,
string toDate,
string routeId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                if (usqlre.user_role != "ADMIN")
                {
                    return StatusCode(403, new
                    {
                        status = false,
                        message = "Access denied"
                    });
                }

                Dictionary<string, DataTable> hash =
                new Dictionary<string, DataTable>();

                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                string userSql = @"

        SELECT TOP 1 gu_user_id

        FROM gnl_users

        WHERE gu_acc_id = '" + salesmanId + @"'

        ";

                DataTable userDt =
                usqlre.dbReaderFill(userSql);

                if (userDt.Rows.Count == 0)
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message = "Salesman not found"
                    });
                }

                string userId =
                userDt.Rows[0]["gu_user_id"].ToString();

                string summarySql = @"

        SELECT

        COUNT(DISTINCT SC.sc_shop_id)
        AS total_shop_covered

        FROM shop_check_in SC

        INNER JOIN acc_subhead A
        ON A.as_id = SC.sc_shop_id

        WHERE SC.sc_userid = '" + userId + @"'

        AND SC.sc_check_in = 2

        AND A.as_ap_id = 4
        AND A.as_active = 1

        AND A.as_salesman_id = '" + salesmanId + @"'
        AND A.as_rout_id = '" + routeId + @"'

        AND CAST(SC.sc_date AS DATE) =
        '" + toDate + @"'

        ";

                DataTable summary =
                usqlre.dbReaderFill(summarySql);

                hash.Add("summary", summary);

                string sql = @"

        SELECT DISTINCT

        A.as_id AS shop_id,
        A.as_name AS shop_name,

        ISNULL(GL.gl_name,'') AS location_name,

        ISNULL(R.r_name,'') AS route_name,

        ISNULL(A.as_latitude,'0') AS latitude,
        ISNULL(A.as_longitude,'0') AS longitude,
        ISNULL(A.as_location,'') AS actual_location,

        SC.sc_check_in_time,
        SC.sc_check_out_time

        FROM shop_check_in SC

        INNER JOIN acc_subhead A
        ON A.as_id = SC.sc_shop_id

        LEFT JOIN gnl_location GL
        ON GL.gl_id = A.as_location_id

        LEFT JOIN inv_rout_reg R
        ON R.r_id = A.as_rout_id

        WHERE SC.sc_userid = '" + userId + @"'

        AND SC.sc_check_in = 2

        AND A.as_ap_id = 4
        AND A.as_active = 1

        AND A.as_salesman_id = '" + salesmanId + @"'
        AND A.as_rout_id = '" + routeId + @"'

        AND CAST(SC.sc_date AS DATE) =
        '" + toDate + @"'

        ORDER BY A.as_name ASC

        ";

                DataTable coveredShop =
                usqlre.dbReaderFill(sql);

                hash.Add("shop_covered", coveredShop);

                string jsonResult =
                ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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

        [HttpGet("salesman-skip-shop-list")]
        public async Task<IActionResult> SalesmanSkipShopList(
string salesmanId,
string toDate,
string routeId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);

                if (usqlre.user_role != "ADMIN")
                {
                    return StatusCode(403, new
                    {
                        status = false,
                        message = "Access denied"
                    });
                }

                Dictionary<string, DataTable> hash =
                new Dictionary<string, DataTable>();

                if (string.IsNullOrEmpty(toDate))
                {
                    toDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                string userSql = @"

        SELECT TOP 1 gu_user_id

        FROM gnl_users

        WHERE gu_acc_id = '" + salesmanId + @"'

        ";

                DataTable userDt =
                usqlre.dbReaderFill(userSql);

                if (userDt.Rows.Count == 0)
                {
                    return StatusCode(400, new
                    {
                        status = false,
                        message = "Salesman not found"
                    });
                }

                string userId =
                userDt.Rows[0]["gu_user_id"].ToString();

                string summarySql = @"

        SELECT

        COUNT(DISTINCT SC.sc_shop_id)
        AS total_skip_shop

        FROM shop_check_in SC

        INNER JOIN acc_subhead A
        ON A.as_id = SC.sc_shop_id

        WHERE SC.sc_userid = '" + userId + @"'

        AND SC.sc_skip_status = 1

        AND A.as_ap_id = 4
        AND A.as_active = 1

        AND A.as_salesman_id = '" + salesmanId + @"'
        AND A.as_rout_id = '" + routeId + @"'

        AND CAST(SC.sc_date AS DATE) =
        '" + toDate + @"'

        ";

                DataTable summary =
                usqlre.dbReaderFill(summarySql);

                hash.Add("summary", summary);

                string sql = @"

        SELECT DISTINCT

        A.as_id AS shop_id,
        A.as_name AS shop_name,

        ISNULL(GL.gl_name,'') AS location_name,

        ISNULL(R.r_name,'') AS route_name,

        ISNULL(A.as_latitude,'0') AS latitude,
        ISNULL(A.as_longitude,'0') AS longitude,
        ISNULL(A.as_location,'') AS actual_location,

        ISNULL(SC.sc_skip_reason,'') AS skip_reason,

        ISNULL(SC.sc_skip_time,'') AS skip_time

        FROM shop_check_in SC

        INNER JOIN acc_subhead A
        ON A.as_id = SC.sc_shop_id

        LEFT JOIN gnl_location GL
        ON GL.gl_id = A.as_location_id

        LEFT JOIN inv_rout_reg R
        ON R.r_id = A.as_rout_id

        WHERE SC.sc_userid = '" + userId + @"'

        AND SC.sc_skip_status = 1

        AND A.as_ap_id = 4
        AND A.as_active = 1

        AND A.as_salesman_id = '" + salesmanId + @"'
        AND A.as_rout_id = '" + routeId + @"'

        AND CAST(SC.sc_date AS DATE) =
        '" + toDate + @"'

        ORDER BY A.as_name ASC

        ";

                DataTable skipShop =
                usqlre.dbReaderFill(sql);

                hash.Add("skip_shop", skipShop);

                string jsonResult =
                ReportModelContext.searializeDt(hash);

                return Content(jsonResult, "application/json");
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


[HttpGet("admin-meter-reading-history")]
public async Task<IActionResult> AdminMeterReadingHistory(
string salesmanId,
string fromDate,
string toDate,
string routeId)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre =
                new UserSqlServer(this);

                // ADMIN CHECK
                if (usqlre.user_role != "ADMIN")
                {
                    return StatusCode(403, new
                    {
                        status = false,
                        message = "Access denied"
                    });
                }

                if (string.IsNullOrEmpty(fromDate))
                {
                    fromDate =
                    DateTime.Now.ToString("yyyy-MM-dd");
                }

                if (string.IsNullOrEmpty(toDate))
                {
                    toDate =
                    DateTime.Now.ToString("yyyy-MM-dd");
                }

                Dictionary<string, DataTable> hash =
                new Dictionary<string, DataTable>();


                string sql = @"

        SELECT

        RTH.rth_id,

        ISNULL(U.gu_name,'')
        AS salesman_name,

        ISNULL(R.r_name,'')
        AS route_name,

        ISNULL(RTH.rth_status,'')
        AS ride_status,

        ISNULL(RTH.rth_time,'')
        AS ride_time,

        ISNULL(RTH.rth_meter_reading,'0')
        AS meter_reading,

        ISNULL( 
        ( SELECT TOP 1 ans_status FROM android_settings
        WHERE ans_name = 'BaseUrl' ),'')
        + ISNULL(RTH.rth_meter_photo,'')
        AS meter_photo,

        ISNULL(RTH.rth_latitude,'0')
        AS latitude,

        ISNULL(RTH.rth_longitude,'0')
        AS longitude,

        ISNULL(RTH.rth_remark,'')
        AS remark

        FROM route_tracking_history RTH

        INNER JOIN route_tracking RT
        ON RT.rt_id =
        RTH.rth_route_tracking_id

        INNER JOIN gnl_users U
        ON U.gu_acc_id =
        RT.rt_salesman

        LEFT JOIN inv_rout_reg R
        ON R.r_id =
        RT.rt_route_id

        WHERE U.gu_acc_id =
        '" + salesmanId + @"'

        AND RT.rt_route_id = '" + routeId + @"'

        AND CAST(RT.rt_date AS DATE)
        BETWEEN '" + fromDate + @"'
        AND '" + toDate + @"'

        ORDER BY RTH.rth_id ASC

        ";

                DataTable meterHistory =
                usqlre.dbReaderFill(sql);

                hash.Add(
                "meter_reading_history",
                meterHistory);



                string summarySql = @"

                WITH RideData AS
                (
                    SELECT
                    RT.rt_route_id,

                    RTH.rth_id,

                    RTH.rth_status,

                   
                    CAST(
                    ISNULL(
                    NULLIF(RTH.rth_meter_reading,'')
                    ,'0'
                    )
                    AS DECIMAL(18,2))
                    AS meter_reading,

                    LEAD(RTH.rth_status)
                    OVER (
                           PARTITION BY RT.rt_route_id
                           ORDER BY RTH.rth_id ASC)
                    AS next_status,


                    LEAD(
                    CAST(
                    ISNULL(
                    NULLIF(RTH.rth_meter_reading,'')
                    ,'0'
                    )
                    AS DECIMAL(18,2))
                    )


                    OVER (
                           PARTITION BY RT.rt_route_id
                           ORDER BY RTH.rth_id ASC)
                    AS next_meter

                    FROM route_tracking_history RTH

                    INNER JOIN route_tracking RT
                    ON RT.rt_id =
                    RTH.rth_route_tracking_id

                    INNER JOIN gnl_users U
                    ON U.gu_acc_id =
                    RT.rt_salesman

                    WHERE U.gu_acc_id =
                    '" + salesmanId + @"'

                    AND RT.rt_route_id = '" + routeId + @"'

                    AND CAST(RT.rt_date AS DATE)
                    BETWEEN '" + fromDate + @"'
                    AND '" + toDate + @"'
                )

                SELECT

                ISNULL
                (
                    SUM
                    (
                        CASE

                        WHEN rth_status = 'start'
                        AND next_status = 'pause'

                        THEN next_meter - meter_reading


                        WHEN rth_status = 'resume'
                        AND next_status IN ('pause','end','stop')

                        THEN next_meter - meter_reading

                        ELSE 0

                        END
                    )
                ,0)

                AS total_distance_covered

                FROM RideData

                ";

                DataTable summary =
                usqlre.dbReaderFill(summarySql);

                hash.Add(
                "meter_reading_summary",
                summary);




                string jsonResult =
                ReportModelContext.searializeDt(hash);

                return Content(
                jsonResult,
                "application/json");
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



    }
}