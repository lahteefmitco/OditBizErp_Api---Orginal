using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class DashboardController : ControllerBase
    {
        [HttpPost("dashboard-odbz")]
        public async Task<IActionResult> DashboardOditBiz([FromBody] AccReportModel model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            if (string.IsNullOrEmpty(model.StatementType))
            {
                var hash = new Dictionary<string, DataTable>();
                string[] statementTypes = { "Display", "Cashflow", "IncomeAndExpense", "ReceivableAndPayable" };

                foreach (var statementType in statementTypes)
                {
                    string query = "EXEC [dbo].[Sp_Dashboard_OditBiz] " +
                                   "@from_date = '" + model.fromDate + "', " +
                                   "@to_date = '" + model.toDate + "', " +
                                   "@user_role = '" + usqlre.user_role + "', " +
                                   "@user_Id = " + Convert.ToInt32(usqlre.userId) + ", " +
                                   "@StatementType = '" + statementType + "'";

                    DataTable dataTable = usqlre.dbReaderFill(query);
                    hash.Add(statementType, dataTable);
                }
                string sql = $@"SELECT TOP 5 ir_name AS Item, SUM(CASE WHEN sp_str_id IN(1, 2, 4, 6, 8,14) THEN 1 ELSE 0 END)-
                            SUM(CASE WHEN sp_str_id IN(7, 9, 10) THEN 1 ELSE 0 END) AS Count FROM inv_sales_par LEFT JOIN
                            inv_sales_inf ON si_entryno = sp_entryno AND si_str_id = sp_str_id LEFT JOIN inv_item_reg ON ir_id = sp_ir_id
                            left join gnl_users on gu_user_id = si_user_id
                            WHERE sp_str_id IN(1, 2, 4, 6, 7, 8, 9, 10,14) AND 
                            inv_sales_inf.si_date >= '{model.fromDate}'
                            AND inv_sales_inf.si_date <= '{model.toDate}'
                            AND gu_name ='{usqlre.user_role}'
                            AND inv_sales_inf.si_user_id= {Convert.ToInt32(usqlre.userId)}
                            GROUP BY ir_name ORDER BY Count DESC";
                DataTable dataTable1 = usqlre.dbReaderFill(sql);
                hash.Add("TopSellingItems", dataTable1);
                usqlre.close();
                string sql1 = $@"SELECT TOP 5 customer_name, SUM(net_sale_total) AS total_purchase
                    FROM (
                    SELECT 
                    CASE
                    WHEN as_ap_id != 1 AND si_lc_id = 0  THEN as_name
                    WHEN as_ap_id = 1 AND si_lc_id > 0 and lc_name!=''THEN lc_name+'('+cast(lc_id as nvarchar(100)) +')'
                    WHEN as_ap_id > 1 AND si_lc_id > 0  THEN as_name
            
                    END AS customer_name,
                    SUM(CASE WHEN si_str_id IN (1, 2, 4, 6, 8,14) THEN si_grand_total ELSE -si_grand_total END) AS net_sale_total
                    FROM inv_sales_inf
                    INNER JOIN acc_subhead ON as_id = si_acc_id
                    INNER JOIN acc_parent ON as_ap_id = ap_id
                    LEFT JOIN acc_loyalty_card ON lc_id = si_lc_id
					left join gnl_users on gu_user_id = si_user_id   
                    WHERE si_str_id IN (1, 2, 4, 6, 8, 7, 9, 10,14)
					AND inv_sales_inf.si_date >= '{model.fromDate}'
                            AND inv_sales_inf.si_date <= '{model.toDate}'
                            AND gu_name ='{usqlre.user_role}'
                            AND inv_sales_inf.si_user_id={Convert.ToInt32(usqlre.userId)}
                    GROUP BY 
                    CASE 
                    WHEN as_ap_id != 1 AND si_lc_id = 0  THEN as_name 
                    WHEN as_ap_id = 1 AND si_lc_id > 0 and lc_name!='' THEN lc_name+'('+cast(lc_id as nvarchar(100)) +')' 
                    WHEN as_ap_id > 1 AND si_lc_id > 0  THEN as_name 
                    END, si_str_id, si_acc_id, si_entryno, si_lc_id, ap_id 
                    ) AS customer_purchase where customer_name !='' 
                    GROUP BY customer_name 
                    ORDER BY total_purchase DESC";
                DataTable dataTable2 = usqlre.dbReaderFill(sql1);
                hash.Add("TopCustomer", dataTable2);
                usqlre.close();

                string jsonResult = ReportModelContext.searializeDt(hash);
                return Content(jsonResult, "application/json");
            }
            else
            {
                String query = "";

                query = "EXEC [dbo].[Sp_Dashboard_OditBiz] " +
                                    "@from_date = '" + model.fromDate + "'," +
                                    "@to_date = '" + model.toDate + "'," +
                                    "@user_role = '" + usqlre.user_role + "'," +
                                    "@user_Id = " + Convert.ToInt32(usqlre.userId) + "," +
                                    "@StatementType = '" + model.StatementType + "'";

                DataSet ds = usqlre.dbreadDataset(query);
                usqlre.close();
                return Ok(ReportModelContext.searializeDt(ds));
            }
        }
        [HttpPost("dashboard")]
        public async Task<IActionResult> Dashboard([FromBody] DashboardModel model)
        {
            try
            {
                Dictionary<string, object> hash = new Dictionary<string, object>();
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql = "";

                if (model.Location != 0)
                {
                    sql = @"SELECT 
                isnull((SELECT SUM(si_grand_total) 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (1,2,6,4,8,14) and  si_location_id=" + model.Location + @" and  cast(si_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
				 ),0) AS totalSale,0 AS totalSaleChange,

                isnull((SELECT SUM(si_grand_total) 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (7,9,10) and si_location_id=" + model.Location + @" and  cast(si_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
				 ),0) AS salesReturn,0 AS salesReturnChange,

                 ISNULL((SELECT SUM(si_grand_total) 
				FROM inv_sales_inf 
				WHERE si_str_id IN (1,2,6,4,8,14) AND si_location_id = " + model.Location + @"  AND CAST(si_date AS date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
                ), 0)
		        - 
				ISNULL((SELECT SUM(si_grand_total) 
				 FROM inv_sales_inf 
				WHERE si_str_id IN (7,9,10) 
				AND si_location_id = " + model.Location + @" AND CAST(si_date AS date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
                ), 0) AS netSale,0 AS netSaleChange,

                isnull((SELECT SUM(pi_grand_total) 
                 FROM inv_purchase_inf
				 where pi_location_id=" + model.Location + @" and cast(pi_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
				 ),0) AS purchase,0 AS purchaseChange,

                isnull((SELECT SUM(pr_grand_total) 
                 FROM inv_purchase_rt_inf
				 where pr_location_id=" + model.Location + @" and cast(pr_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
				 ),0) AS purchaseReturn,0 AS purchaseReturnChange,

                isnull((SELECT SUM(at_Cr) 
                 FROM acc_account_transactions 
				 inner join acc_subhead on as_id=at_as_id
				inner join acc_parent on ap_id=as_ap_id  where (at_form='PAYMENT' or at_form='BANK PAYMENT') and at_Cr>0 
				and (ap_name='CASH IN HAND' or ap_name='BANK') and 
                cast(at_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"' and at_location_id=" + model.Location + @"),0)
				 AS payment,0 AS paymentChange,

                isnull((SELECT SUM(at_Dr)
                 FROM acc_account_transactions 
                 inner join acc_subhead on as_id=at_as_id
				inner join acc_parent on ap_id=as_ap_id where (at_form='RECEIPT' or at_form='BANK RECEIPT') and at_Dr>0 
				and (ap_name='CASH IN HAND' or ap_name='BANK') and cast(at_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'  and at_location_id=" + model.Location + @"),0)
				 AS receipt,0 AS recieptChange;
            ";
                }
                else 
                {
                    sql = @"SELECT 
                isnull((SELECT SUM(si_grand_total) 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (1,2,6,4,8,14) and  cast(si_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
				 ),0) AS totalSale,0 AS totalSaleChange,

                isnull((SELECT SUM(si_grand_total) 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (7,9,10) and  cast(si_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
				 ),0) AS salesReturn,0 AS salesReturnChange,

                 ISNULL((SELECT SUM(si_grand_total) 
				FROM inv_sales_inf 
				WHERE si_str_id IN (1,2,6,4,8,14)  AND CAST(si_date AS date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
                ), 0)
		        - 
				ISNULL((SELECT SUM(si_grand_total) 
				 FROM inv_sales_inf 
				WHERE si_str_id IN (7,9,10) 
				AND  CAST(si_date AS date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
                ), 0) AS netSale,0 AS netSaleChange,

                isnull((SELECT SUM(pi_grand_total) 
                 FROM inv_purchase_inf
				 where cast(pi_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
				 ),0) AS purchase,0 AS purchaseChange,

                isnull((SELECT SUM(pr_grand_total) 
                 FROM inv_purchase_rt_inf
				 where cast(pr_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
				 ),0) AS purchaseReturn,0 AS purchaseReturnChange,

                isnull((SELECT SUM(at_Cr) 
                 FROM acc_account_transactions 
				 inner join acc_subhead on as_id=at_as_id
				inner join acc_parent on ap_id=as_ap_id  where (at_form='PAYMENT' or at_form='BANK PAYMENT') and at_Cr>0 
				and (ap_name='CASH IN HAND' or ap_name='BANK') and 
                cast(at_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'),0)
				 AS payment,0 AS paymentChange,

                isnull((SELECT SUM(at_Dr)
                 FROM acc_account_transactions 
                 inner join acc_subhead on as_id=at_as_id
				inner join acc_parent on ap_id=as_ap_id where (at_form='RECEIPT' or at_form='BANK RECEIPT') and at_Dr>0 
				and (ap_name='CASH IN HAND' or ap_name='BANK') and cast(at_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'),0)
				 AS receipt,0 AS recieptChange;
            ";
                }
                

                DataTable insights = usqlre.dbReaderFill(sql);
                var insights1 = insights.Rows.Count > 0 ? new
                {
                    totalSale = insights.Rows[0]["totalSale"],
                    totalSaleChange = insights.Rows[0]["totalSaleChange"],
                    salesReturn = insights.Rows[0]["salesReturn"],
                    salesReturnChange = insights.Rows[0]["salesReturnChange"],
                    netSale = insights.Rows[0]["netSale"],
                    netSaleChange = insights.Rows[0]["netSaleChange"],
                    purchase = insights.Rows[0]["purchase"],
                    purchaseChange = insights.Rows[0]["purchaseChange"],
                    purchaseReturn = insights.Rows[0]["purchaseReturn"],
                    purchaseReturnChange = insights.Rows[0]["purchaseReturnChange"],
                    payment = insights.Rows[0]["payment"],
                    paymentChange = insights.Rows[0]["paymentChange"],
                    receipt = insights.Rows[0]["receipt"],
                    recieptChange = insights.Rows[0]["recieptChange"]
                } : null;

                hash.Add("insights", insights1);
                if (usqlre.user_role == "ADMIN")
                {
                    sql = @"
                SELECT 
                    ABS(SUM(CASE WHEN ap_name = 'CASH IN HAND' THEN at_dr - at_Cr ELSE 0 END)) AS cashInHand,
                    ABS(SUM(CASE WHEN ap_name = 'BANK' THEN at_dr - at_Cr ELSE 0 END)) AS cashInBank
                FROM acc_account_transactions 
                LEFT JOIN acc_subhead ON at_as_id = as_id 
                LEFT JOIN acc_parent ON as_ap_id = ap_id 
                WHERE ap_name IN ('CASH IN HAND', 'BANK')";
                }
                else
                {
                    sql = $@"
        
                SELECT 
                    ABS(SUM(CASE 
                            WHEN p.ap_name = 'CASH IN HAND' 
                                 AND at.at_as_id = {usqlre.gu_user_cash_id}
                            THEN at.at_dr - at.at_cr 
                            ELSE 0 
                        END)) AS cashInHand,

                    ABS(SUM(CASE 
                            WHEN p.ap_name = 'BANK' 
                                 AND at.at_as_id = u.gu_user_bank_id
                            THEN at.at_dr - at.at_cr 
                            ELSE 0 
                        END)) AS cashInBank
                FROM acc_account_transactions at
                JOIN acc_subhead s ON at.at_as_id = s.as_id
                JOIN acc_parent p ON s.as_ap_id = p.ap_id
                LEFT JOIN gnl_users u ON u.gu_user_id = {usqlre.userId};";
                }


                DataTable financial_statement = usqlre.dbReaderFill(sql);
                var financial_statement1 = financial_statement.Rows.Count > 0 ? new
                {
                    cashInHand = financial_statement.Rows[0]["cashInHand"],
                    cashInBank = financial_statement.Rows[0]["cashInBank"]
                } : null;

                hash.Add("financial_statement", financial_statement1);
                if (model.Location !=0)
                {
                    sql = @"
                    SELECT 
                        CONVERT(CHAR(7), at_date, 120) AS [month],
                        ISNULL(SUM(CASE 
                            WHEN at_form IN ('PAYMENT','BANK PAYMENT') 
                                 AND at_Cr > 0 
                                 AND (ap_name = 'CASH IN HAND' OR ap_name = 'BANK') 
                            THEN at_Cr ELSE 0 END), 0) AS payment,
                        ISNULL(SUM(CASE 
                            WHEN at_form IN ('RECEIPT','BANK RECEIPT') 
                                 AND at_Dr > 0 
                                 AND (ap_name = 'CASH IN HAND' OR ap_name = 'BANK') 
                            THEN at_Dr ELSE 0 END), 0) AS receipt
                    FROM acc_account_transactions 
                    INNER JOIN acc_subhead ON as_id = at_as_id
                    INNER JOIN acc_parent ON ap_id = as_ap_id
                    WHERE CAST(at_date AS date) 
                          BETWEEN '" + model.Insight.FromDate + @"' 
                          AND '" + model.Insight.ToDate + @"'
                      AND at_location_id = " + model.Location + @"
                    GROUP BY CONVERT(CHAR(7), at_date, 120)
                    ORDER BY CONVERT(CHAR(7), at_date, 120);";

                }
                else
                {
                    sql = @"
                 SELECT 
                CONVERT(CHAR(7), at_date, 120) AS [month],
                ISNULL(SUM(CASE 
                    WHEN at_form IN ('PAYMENT','BANK PAYMENT') 
                         AND at_Cr > 0 
                         AND (ap_name='CASH IN HAND' OR ap_name='BANK') 
                    THEN at_Cr ELSE 0 END),0) AS payment,
                ISNULL(SUM(CASE 
                    WHEN at_form IN ('RECEIPT','BANK RECEIPT') 
                         AND at_Dr > 0 
                         AND (ap_name='CASH IN HAND' OR ap_name='BANK') 
                    THEN at_Dr ELSE 0 END),0) AS receipt
            FROM acc_account_transactions 
            INNER JOIN acc_subhead ON as_id = at_as_id
            INNER JOIN acc_parent ON ap_id = as_ap_id
            WHERE CAST(at_date AS date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
            GROUP BY CONVERT(CHAR(7), at_date, 120)
            ORDER BY CONVERT(CHAR(7), at_date, 120);";

                }
                DataTable cashflow = usqlre.dbReaderFill(sql);
                hash.Add("cashflow", cashflow);
                if (model.Location != 0)
                {
                    sql = @"
                	SELECT
                    ABS(SUM(CASE WHEN a.as_ap_id = 4 THEN (t.at_Dr - t.at_Cr) ELSE 0 END)) AS Receivable,
                    ABS(SUM(CASE WHEN a.as_ap_id = 6 THEN (t.at_Dr - t.at_Cr) ELSE 0 END)) AS Payable
                FROM acc_account_transactions t
                INNER JOIN acc_subhead a 
                    ON t.at_as_id = a.as_id
                WHERE a.as_active = 1
                  AND CAST(t.at_date AS date)<='" + model.CashFlow.ToDate + @"'
                  AND a.as_ap_id IN (4, 6);";
                }
                else
                {
                    sql = @"
                	SELECT
                    ABS(SUM(CASE WHEN a.as_ap_id = 4 THEN (t.at_Dr - t.at_Cr) ELSE 0 END)) AS Receivable,
                    ABS(SUM(CASE WHEN a.as_ap_id = 6 THEN (t.at_Dr - t.at_Cr) ELSE 0 END)) AS Payable
                FROM acc_account_transactions t
                INNER JOIN acc_subhead a 
                    ON t.at_as_id = a.as_id
                WHERE a.as_active = 1
                  AND CAST(t.at_date AS date)<='" + model.CashFlow.ToDate + @"'
                  AND a.as_ap_id IN (4, 6);";
                }
                
                DataTable payable_recievable = usqlre.dbReaderFill(sql);
                var payable_recievable1 = payable_recievable.Rows.Count > 0 ? new
                {
                    payable = payable_recievable.Rows[0]["Payable"],
                    receipt = payable_recievable.Rows[0]["Receivable"]
                } : null;

                hash.Add("payable_recievable", payable_recievable1);
                if (model.Location != 0)
                {
                    sql = @"
                    DECLARE @StartDate DATE, @EndDate DATE;
                    DECLARE @FromDate DATE, @ToDate DATE;

                    SET @FromDate = '" + model.IncomeExpense.FromDate + @"';
                    SET @ToDate = '" + model.IncomeExpense.ToDate + @"'; 

                    SELECT @StartDate = com_sdate, @EndDate = com_edate FROM gnl_company;

                    IF @FromDate < @StartDate SET @FromDate = @StartDate;
                    IF @ToDate > @EndDate SET @ToDate = @EndDate;

                    CREATE TABLE #months (
                        MonthYear NVARCHAR(7), 
                        Month NVARCHAR(20)
                    );

                    CREATE TABLE #income (
                        Month NVARCHAR(7), 
                        DirectIncome MONEY, 
                        InDirectIncome MONEY, 
                        DirectExpense MONEY, 
                        InDirectExpense MONEY
                    );

                    INSERT INTO #income
                    SELECT 
                        CONVERT(VARCHAR(7), at_date, 120) AS Month,
                        SUM(CASE WHEN ah_name = 'DIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS DirectIncome,
                        SUM(CASE WHEN ah_name = 'INDIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS InDirectIncome,
                        SUM(CASE WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS DirectExpense,
                        SUM(CASE WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS InDirectExpense
                    FROM acc_account_transactions
                    INNER JOIN acc_subhead ON at_as_id = as_id
                    INNER JOIN acc_parent ON as_ap_id = ap_id
                    INNER JOIN acc_head ON ah_id = ap_ah_id
                    WHERE ah_name IN ('DIRECT INCOME', 'INDIRECT INCOME', 'DIRECT EXPENSE', 'INDIRECT EXPENSE')
                      AND at_location_id = " + model.Location + @" 
                      AND at_date BETWEEN @FromDate AND @ToDate
                    GROUP BY CONVERT(VARCHAR(7), at_date, 120)
                    ORDER BY CONVERT(VARCHAR(7), at_date, 120);

                    WITH MonthsCTE AS (
                        SELECT @FromDate AS Month
                        UNION ALL
                        SELECT DATEADD(MONTH, 1, Month)
                        FROM MonthsCTE
                        WHERE DATEADD(MONTH, 1, Month) <= @ToDate
                    )
                    INSERT INTO #months
                    SELECT 
                        CONVERT(VARCHAR(7), Month, 120) AS MonthYear,
                        DATENAME(MONTH, CAST(CONVERT(VARCHAR(7), Month, 120) + '-01' AS DATE))
                    FROM MonthsCTE
                    OPTION (MAXRECURSION 0);

                    SELECT 
                        m.MonthYear AS month,
                        COALESCE(i.DirectIncome, 0) + COALESCE(i.InDirectIncome, 0) AS income,
                        COALESCE(i.DirectExpense, 0) + COALESCE(i.InDirectExpense, 0) AS expense
                    FROM #months m
                    LEFT JOIN #income i ON i.Month = m.MonthYear
                    ORDER BY m.MonthYear;

                    DROP TABLE #months;
                    DROP TABLE #income;
                    ";

                }
                else
                {
                    sql = @"
                DECLARE @StartDate DATE, @EndDate DATE;
                DECLARE @FromDate DATE, @ToDate DATE;

                SET @FromDate = '" + model.IncomeExpense.FromDate + @"';
                SET @ToDate = '" + model.IncomeExpense.ToDate + @"'; 

                SELECT @StartDate = com_sdate, @EndDate = com_edate FROM gnl_company;

                IF @FromDate < @StartDate SET @FromDate = @StartDate;
                IF @ToDate > @EndDate SET @ToDate = @EndDate;

                CREATE TABLE #months (
                    MonthYear NVARCHAR(7), 
                    Month NVARCHAR(20)
                );

                CREATE TABLE #income (
                    Month NVARCHAR(7), 
                    DirectIncome MONEY, 
                    InDirectIncome MONEY, 
                    DirectExpense MONEY, 
                    InDirectExpense MONEY
                );

                INSERT INTO #income
                SELECT 
                    CONVERT(VARCHAR(7), at_date, 120) AS Month,
                    SUM(CASE WHEN ah_name = 'DIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS DirectIncome,
                    SUM(CASE WHEN ah_name = 'INDIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS InDirectIncome,
                    SUM(CASE WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS DirectExpense,
                    SUM(CASE WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS InDirectExpense
                FROM acc_account_transactions
                INNER JOIN acc_subhead ON at_as_id = as_id
                INNER JOIN acc_parent ON as_ap_id = ap_id
                INNER JOIN acc_head ON ah_id = ap_ah_id
                WHERE ah_name IN ('DIRECT INCOME', 'INDIRECT INCOME', 'DIRECT EXPENSE', 'INDIRECT EXPENSE')
                AND at_date BETWEEN @FromDate AND @ToDate
                GROUP BY CONVERT(VARCHAR(7), at_date, 120)
                ORDER BY CONVERT(VARCHAR(7), at_date, 120);

                WITH MonthsCTE AS (
                    SELECT @FromDate AS Month
                    UNION ALL
                    SELECT DATEADD(MONTH, 1, Month)
                    FROM MonthsCTE
                    WHERE DATEADD(MONTH, 1, Month) <= @ToDate
                )
                INSERT INTO #months
                SELECT 
                    CONVERT(VARCHAR(7), Month, 120) AS MonthYear,
                    DATENAME(MONTH, CAST(CONVERT(VARCHAR(7), Month, 120) + '-01' AS DATE))
                FROM MonthsCTE
                OPTION (MAXRECURSION 0);

                SELECT 
                    m.MonthYear AS month,
                    COALESCE(i.DirectIncome, 0) + COALESCE(i.InDirectIncome, 0) AS income,
                    COALESCE(i.DirectExpense, 0) + COALESCE(i.InDirectExpense, 0) AS expense
                FROM #months m
                LEFT JOIN #income i ON i.Month = m.MonthYear
                ORDER BY MonthYear;

                DROP TABLE #months;
                DROP TABLE #income;
                ";

                }
                DataTable income_expense = usqlre.dbReaderFill(sql);
                hash.Add("income_expense", income_expense);
                if (model.Location != 0)
                {
                    sql = @"
                SELECT 
                    ah_name AS label,
                    SUM(CASE 
                        WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr
                        WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr
                        ELSE 0 
                    END) AS amount
                FROM acc_account_transactions 
                INNER JOIN acc_subhead ON at_as_id = as_id
                INNER JOIN acc_parent ON as_ap_id = ap_id
                INNER JOIN acc_head ON ah_id = ap_ah_id
                WHERE ah_name IN ('DIRECT EXPENSE', 'INDIRECT EXPENSE') 
                AND at_location_id="+model.Location+" AND at_date BETWEEN '" + model.TopExpense.FromDate + "' AND '" + model.TopExpense.ToDate + "' GROUP BY ah_name ORDER BY amount DESC";

                }
                else
                {
                    sql = @"
                SELECT 
                    ah_name AS label,
                    SUM(CASE 
                        WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr
                        WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr
                        ELSE 0 
                    END) AS amount
                FROM acc_account_transactions 
                INNER JOIN acc_subhead ON at_as_id = as_id
                INNER JOIN acc_parent ON as_ap_id = ap_id
                INNER JOIN acc_head ON ah_id = ap_ah_id
                WHERE ah_name IN ('DIRECT EXPENSE', 'INDIRECT EXPENSE') 
                AND at_date BETWEEN '" + model.TopExpense.FromDate + "' AND '" + model.TopExpense.ToDate + "' GROUP BY ah_name ORDER BY amount DESC";

                }

                DataTable top_expense = usqlre.dbReaderFill(sql);
                hash.Add("top_expense", top_expense);
                usqlre.close();
                var response = new
                {
                    status = true,
                    status_code = 200,
                    message = "Bookings retrieved successfully",
                    data= hash
                };
                string jsonResult = ReportModelContext.searializeDt(response);
                return Content(jsonResult, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching financial statement: " + ex.Message,
                    data = new object[] { } 
                });
            }
        }
        [HttpPost("admin-dashboard")]
        public async Task<IActionResult> AdminDashboard([FromBody] DashboardModel model)
        {
            try
            {
                Dictionary<string, object> hash = new Dictionary<string, object>();
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql = "";
                if (usqlre.user_role == "ADMIN")
                {
                    sql = @"SELECT 
                    ISNULL((
                        SELECT SUM(si_grand_total)
                        FROM inv_sales_inf 
                        WHERE si_str_id IN (1,2,6,4,8,14)
                          AND CAST(si_date AS DATE) 
                              BETWEEN '" + model.Insight.FromDate + @"' 
                                  AND '" + model.Insight.ToDate + @"'
                    ), 0) AS totalSale,

                    ISNULL((
                        SELECT SUM(par.sp_qty)
                        FROM inv_sales_inf inf
                        INNER JOIN inv_sales_par par
                            ON inf.si_entryno = par.sp_entryno
                           AND inf.si_str_id = par.sp_str_id
                        WHERE inf.si_str_id IN (1,2,6,4,8,14)
                          AND CAST(inf.si_date AS DATE) 
                              BETWEEN '" + model.Insight.FromDate + @"' 
                                  AND '" + model.Insight.ToDate + @"'
                    ), 0) AS totalSaleQty,

                    0 AS totalSaleChange,

                isnull((SELECT SUM(si_grand_total) 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (7,9,10) and  cast(si_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
				 ),0) AS salesReturn,0 AS salesReturnChange,

                 ISNULL((SELECT SUM(si_grand_total) 
				FROM inv_sales_inf 
				WHERE si_str_id IN (1,2,6,4,8,14)  AND CAST(si_date AS date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
                ), 0)
		        - 
				ISNULL((SELECT SUM(si_grand_total) 
				 FROM inv_sales_inf 
				WHERE si_str_id IN (7,9,10) 
				AND  CAST(si_date AS date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
                ), 0) AS netSale,0 AS netSaleChange,

                isnull((SELECT SUM(pi_grand_total) 
                 FROM inv_purchase_inf
				 where cast(pi_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
				 ),0) AS purchase,0 AS purchaseChange,

                isnull((SELECT SUM(pr_grand_total) 
                 FROM inv_purchase_rt_inf
				 where cast(pr_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
				 ),0) AS purchaseReturn,0 AS purchaseReturnChange,

                isnull((SELECT SUM(at_Cr) 
                 FROM acc_account_transactions 
				 inner join acc_subhead on as_id=at_as_id
				inner join acc_parent on ap_id=as_ap_id  where (at_form='PAYMENT' or at_form='BANK PAYMENT') and at_Cr>0 
				and (ap_name='CASH IN HAND' or ap_name='BANK') and 
                cast(at_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'),0)
				 AS payment,0 AS paymentChange,

                isnull((SELECT SUM(at_Dr)
                 FROM acc_account_transactions 
                 inner join acc_subhead on as_id=at_as_id
				inner join acc_parent on ap_id=as_ap_id where (at_form='RECEIPT' or at_form='BANK RECEIPT') and at_Dr>0 
				and (ap_name='CASH IN HAND' or ap_name='BANK') and cast(at_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'),0)
				 AS receipt,0 AS recieptChange;
            ";


                    DataTable insights = usqlre.dbReaderFill(sql);
                    var insights1 = insights.Rows.Count > 0 ? new
                    {
                        totalSale = insights.Rows[0]["totalSale"],
                        totalSaleQty = insights.Rows[0]["totalSaleQty"],
                        totalSaleChange = insights.Rows[0]["totalSaleChange"],
                        salesReturn = insights.Rows[0]["salesReturn"],
                        salesReturnChange = insights.Rows[0]["salesReturnChange"],
                        netSale = insights.Rows[0]["netSale"],
                        netSaleChange = insights.Rows[0]["netSaleChange"],
                        purchase = insights.Rows[0]["purchase"],
                        purchaseChange = insights.Rows[0]["purchaseChange"],
                        purchaseReturn = insights.Rows[0]["purchaseReturn"],
                        purchaseReturnChange = insights.Rows[0]["purchaseReturnChange"],
                        payment = insights.Rows[0]["payment"],
                        paymentChange = insights.Rows[0]["paymentChange"],
                        receipt = insights.Rows[0]["receipt"],
                        recieptChange = insights.Rows[0]["recieptChange"]
                    } : null;

                    hash.Add("insights", insights1);
                    sql = @"
                SELECT 
                    ABS(SUM(CASE WHEN ap_name = 'CASH IN HAND' THEN at_dr - at_Cr ELSE 0 END)) AS cashInHand,
                    ABS(SUM(CASE WHEN ap_name = 'BANK' THEN at_dr - at_Cr ELSE 0 END)) AS cashInBank
                FROM acc_account_transactions 
                LEFT JOIN acc_subhead ON at_as_id = as_id 
                LEFT JOIN acc_parent ON as_ap_id = ap_id 
                WHERE ap_name IN ('CASH IN HAND', 'BANK')";

                    DataTable financial_statement = usqlre.dbReaderFill(sql);
                    var financial_statement1 = financial_statement.Rows.Count > 0 ? new
                    {
                        cashInHand = financial_statement.Rows[0]["cashInHand"],
                        cashInBank = financial_statement.Rows[0]["cashInBank"]
                    } : null;


                    hash.Add("financial_statement", financial_statement1);
                    sql = @"
                SELECT MAX(as_id) AS UserId , as_name AS UserName, 
                       CASE 
                            WHEN SUM(at_Dr) - SUM(at_Cr) > 0 AND SUM(at_Cr) - SUM(at_Dr) <= 0 THEN dbo.MoneyToN(SUM(at_Dr) - SUM(at_Cr), 4)
                            WHEN SUM(at_Dr) - SUM(at_Cr) <= 0 AND SUM(at_Cr) - SUM(at_Dr) > 0 THEN -(dbo.MoneyToN(SUM(at_Cr) - SUM(at_Dr), 4))
                            ELSE dbo.MoneyToN(SUM(at_Dr) - SUM(at_Cr), 4) - dbo.MoneyToN(SUM(at_Cr) - SUM(at_Dr), 4)
                       END AS CashInHand                      
                FROM acc_account_transactions 
                INNER JOIN acc_subhead a ON at_as_id = a.as_id 
                INNER JOIN acc_parent ON a.as_ap_id = ap_id 
				where a.as_ap_id=1
                AND as_active = 1 
                GROUP BY as_name";

                    DataTable financial_statement_summery = usqlre.dbReaderFill(sql);
                    var financial_statement_summery_list =
                    financial_statement_summery.AsEnumerable()
                    .Select(r => new
                    {
                        UserId = r["UserId"],
                        UserName = r["UserName"],
                        cashInHand = r["CashInHand"]
                    })
                    .ToList();

                    hash.Add("financial_statement_summery", financial_statement_summery_list);

                    sql = @"
                 SELECT 
                CONVERT(CHAR(7), at_date, 120) AS [month],
                ISNULL(SUM(CASE 
                    WHEN at_form IN ('PAYMENT','BANK PAYMENT') 
                         AND at_Cr > 0 
                         AND (ap_name='CASH IN HAND' OR ap_name='BANK') 
                    THEN at_Cr ELSE 0 END),0) AS payment,
                ISNULL(SUM(CASE 
                    WHEN at_form IN ('RECEIPT','BANK RECEIPT') 
                         AND at_Dr > 0 
                         AND (ap_name='CASH IN HAND' OR ap_name='BANK') 
                    THEN at_Dr ELSE 0 END),0) AS receipt
            FROM acc_account_transactions 
            INNER JOIN acc_subhead ON as_id = at_as_id
            INNER JOIN acc_parent ON ap_id = as_ap_id
            WHERE CAST(at_date AS date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"'
            GROUP BY CONVERT(CHAR(7), at_date, 120)
            ORDER BY CONVERT(CHAR(7), at_date, 120);";

                    DataTable cashflow = usqlre.dbReaderFill(sql);
                    hash.Add("cashflow", cashflow);
                    sql = @"
                	SELECT
                    ABS(SUM(CASE WHEN a.as_ap_id = 4 THEN (t.at_Dr - t.at_Cr) ELSE 0 END)) AS Receivable,
                    ABS(SUM(CASE WHEN a.as_ap_id = 6 THEN (t.at_Dr - t.at_Cr) ELSE 0 END)) AS Payable
                FROM acc_account_transactions t
                INNER JOIN acc_subhead a 
                    ON t.at_as_id = a.as_id
                WHERE a.as_active = 1
                  AND CAST(t.at_date AS date)<='" + model.CashFlow.ToDate + @"'
                  AND a.as_ap_id IN (4, 6);";

                    DataTable payable_recievable = usqlre.dbReaderFill(sql);
                    var payable_recievable1 = payable_recievable.Rows.Count > 0 ? new
                    {
                        payable = payable_recievable.Rows[0]["Payable"],
                        receipt = payable_recievable.Rows[0]["Receivable"]
                    } : null;

                    hash.Add("payable_recievable", payable_recievable1);
                    sql = @"
                DECLARE @StartDate DATE, @EndDate DATE;
                DECLARE @FromDate DATE, @ToDate DATE;

                SET @FromDate = '" + model.IncomeExpense.FromDate + @"';
                SET @ToDate = '" + model.IncomeExpense.ToDate + @"'; 

                SELECT @StartDate = com_sdate, @EndDate = com_edate FROM gnl_company;

                IF @FromDate < @StartDate SET @FromDate = @StartDate;
                IF @ToDate > @EndDate SET @ToDate = @EndDate;

                CREATE TABLE #months (
                    MonthYear NVARCHAR(7), 
                    Month NVARCHAR(20)
                );

                CREATE TABLE #income (
                    Month NVARCHAR(7), 
                    DirectIncome MONEY, 
                    InDirectIncome MONEY, 
                    DirectExpense MONEY, 
                    InDirectExpense MONEY
                );

                INSERT INTO #income
                SELECT 
                    CONVERT(VARCHAR(7), at_date, 120) AS Month,
                    SUM(CASE WHEN ah_name = 'DIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS DirectIncome,
                    SUM(CASE WHEN ah_name = 'INDIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS InDirectIncome,
                    SUM(CASE WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS DirectExpense,
                    SUM(CASE WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS InDirectExpense
                FROM acc_account_transactions
                INNER JOIN acc_subhead ON at_as_id = as_id
                INNER JOIN acc_parent ON as_ap_id = ap_id
                INNER JOIN acc_head ON ah_id = ap_ah_id
                WHERE ah_name IN ('DIRECT INCOME', 'INDIRECT INCOME', 'DIRECT EXPENSE', 'INDIRECT EXPENSE')
                AND at_date BETWEEN @FromDate AND @ToDate
                GROUP BY CONVERT(VARCHAR(7), at_date, 120)
                ORDER BY CONVERT(VARCHAR(7), at_date, 120);

                WITH MonthsCTE AS (
                    SELECT @FromDate AS Month
                    UNION ALL
                    SELECT DATEADD(MONTH, 1, Month)
                    FROM MonthsCTE
                    WHERE DATEADD(MONTH, 1, Month) <= @ToDate
                )
                INSERT INTO #months
                SELECT 
                    CONVERT(VARCHAR(7), Month, 120) AS MonthYear,
                    DATENAME(MONTH, CAST(CONVERT(VARCHAR(7), Month, 120) + '-01' AS DATE))
                FROM MonthsCTE
                OPTION (MAXRECURSION 0);

                SELECT 
                    m.MonthYear AS month,
                    COALESCE(i.DirectIncome, 0) + COALESCE(i.InDirectIncome, 0) AS income,
                    COALESCE(i.DirectExpense, 0) + COALESCE(i.InDirectExpense, 0) AS expense
                FROM #months m
                LEFT JOIN #income i ON i.Month = m.MonthYear
                ORDER BY MonthYear;

                DROP TABLE #months;
                DROP TABLE #income;
                ";
                    DataTable income_expense = usqlre.dbReaderFill(sql);
                    hash.Add("income_expense", income_expense);
                    sql = @"
                SELECT 
                    ah_name AS label,
                    SUM(CASE 
                        WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr
                        WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr
                        ELSE 0 
                    END) AS amount
                FROM acc_account_transactions 
                INNER JOIN acc_subhead ON at_as_id = as_id
                INNER JOIN acc_parent ON as_ap_id = ap_id
                INNER JOIN acc_head ON ah_id = ap_ah_id
                WHERE ah_name IN ('DIRECT EXPENSE', 'INDIRECT EXPENSE') 
                AND at_date BETWEEN '" + model.TopExpense.FromDate + "' AND '" + model.TopExpense.ToDate + "' GROUP BY ah_name ORDER BY amount DESC";

                    DataTable top_expense = usqlre.dbReaderFill(sql);
                    hash.Add("top_expense", top_expense);
                    usqlre.close();
                    
                }
                var response = new
                {
                    status = true,
                    status_code = 200,
                    message = "Bookings retrieved successfully",
                    data = hash
                };
                string jsonResult = ReportModelContext.searializeDt(response);
                return Content(jsonResult, "application/json");


            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching financial statement: " + ex.Message,
                    data = new object[] { }
                });
            }
        }
        [HttpPost("user-dashboard")]
        public async Task<IActionResult> UserDashboard([FromBody] DashboardModel model)
        {
            try
            {
                Dictionary<string, object> hash = new Dictionary<string, object>();
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql = "";
                if (usqlre.user_role != "ADMIN")
                {
                    sql = @"SELECT 
                    ISNULL((
                        SELECT SUM(si_grand_total)
                        FROM inv_sales_inf 
                        WHERE si_str_id IN (1,2,6,4,8,14)
                          AND CAST(si_date AS DATE) 
                              BETWEEN '" + model.Insight.FromDate + @"' 
                                  AND '" + model.Insight.ToDate + @"'  AND si_location_id= '" + usqlre.locationId + @"'
                    ), 0) AS totalSale,

                    ISNULL((
                        SELECT SUM(par.sp_qty)
                        FROM inv_sales_inf inf
                        INNER JOIN inv_sales_par par
                            ON inf.si_entryno = par.sp_entryno
                           AND inf.si_str_id = par.sp_str_id
                        WHERE inf.si_str_id IN (1,2,6,4,8,14)
                          AND CAST(inf.si_date AS DATE) 
                              BETWEEN '" + model.Insight.FromDate + @"' 
                                  AND '" + model.Insight.ToDate + @"' AND si_location_id= '" + usqlre.locationId + @"'
                    ), 0) AS totalSaleQty,

                    0 AS totalSaleChange,

                isnull((SELECT SUM(si_grand_total) 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (7,9,10) and  cast(si_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"' AND si_location_id=  '" + usqlre.locationId + @"'
				 ),0) AS salesReturn,0 AS salesReturnChange,

                 ISNULL((SELECT SUM(si_grand_total) 
				FROM inv_sales_inf 
				WHERE si_str_id IN (1,2,6,4,8,14)  AND CAST(si_date AS date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"' AND si_location_id= '" + usqlre.locationId + @"'
                ), 0)
		        - 
				ISNULL((SELECT SUM(si_grand_total) 
				 FROM inv_sales_inf 
				WHERE si_str_id IN (7,9,10) 
				AND  CAST(si_date AS date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"' AND si_location_id= '" + usqlre.locationId + @"' 
                ), 0) AS netSale,0 AS netSaleChange,

                isnull((SELECT SUM(pi_grand_total) 
                 FROM inv_purchase_inf
				 where cast(pi_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"' AND pi_location_id= '" + usqlre.locationId + @"'
				 ),0) AS purchase,0 AS purchaseChange,

                isnull((SELECT SUM(pr_grand_total) 
                 FROM inv_purchase_rt_inf
				 where cast(pr_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"' AND pr_location_id= '" + usqlre.locationId + @"'
				 ),0) AS purchaseReturn,0 AS purchaseReturnChange,

                isnull((SELECT SUM(at_Cr) 
                 FROM acc_account_transactions 
				 inner join acc_subhead on as_id=at_as_id
				inner join acc_parent on ap_id=as_ap_id  where (at_form='PAYMENT' or at_form='BANK PAYMENT') and at_Cr>0 
				and (ap_name='CASH IN HAND' or ap_name='BANK') and 
                cast(at_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"' AND at_location_id= '" + usqlre.locationId + @"'),0)
				 AS payment,0 AS paymentChange,

                isnull((SELECT SUM(at_Dr)
                 FROM acc_account_transactions 
                 inner join acc_subhead on as_id=at_as_id
				inner join acc_parent on ap_id=as_ap_id where (at_form='RECEIPT' or at_form='BANK RECEIPT') and at_Dr>0 
				and (ap_name='CASH IN HAND' or ap_name='BANK') and cast(at_date as date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"' AND at_location_id= '" + usqlre.locationId + @"'),0)
				 AS receipt,0 AS recieptChange;
            ";


                    DataTable insights = usqlre.dbReaderFill(sql);
                    var insights1 = insights.Rows.Count > 0 ? new
                    {
                        totalSale = insights.Rows[0]["totalSale"],
                        totalSaleQty = insights.Rows[0]["totalSaleQty"],
                        totalSaleChange = insights.Rows[0]["totalSaleChange"],
                        salesReturn = insights.Rows[0]["salesReturn"],
                        salesReturnChange = insights.Rows[0]["salesReturnChange"],
                        netSale = insights.Rows[0]["netSale"],
                        netSaleChange = insights.Rows[0]["netSaleChange"],
                        purchase = insights.Rows[0]["purchase"],
                        purchaseChange = insights.Rows[0]["purchaseChange"],
                        purchaseReturn = insights.Rows[0]["purchaseReturn"],
                        purchaseReturnChange = insights.Rows[0]["purchaseReturnChange"],
                        payment = insights.Rows[0]["payment"],
                        paymentChange = insights.Rows[0]["paymentChange"],
                        receipt = insights.Rows[0]["receipt"],
                        recieptChange = insights.Rows[0]["recieptChange"]
                    } : null;

                    hash.Add("insights", insights1);
                    sql = @"
                SELECT 
                    ABS(SUM(CASE WHEN ap_name = 'CASH IN HAND' THEN at_dr - at_Cr ELSE 0 END)) AS cashInHand,
                    ABS(SUM(CASE WHEN ap_name = 'BANK' THEN at_dr - at_Cr ELSE 0 END)) AS cashInBank
                FROM acc_account_transactions 
                LEFT JOIN acc_subhead ON at_as_id = as_id 
                LEFT JOIN acc_parent ON as_ap_id = ap_id 
                WHERE ap_name IN ('CASH IN HAND', 'BANK')";

                    DataTable financial_statement = usqlre.dbReaderFill(sql);
                    var financial_statement1 = financial_statement.Rows.Count > 0 ? new
                    {
                        cashInHand = financial_statement.Rows[0]["cashInHand"],
                        cashInBank = financial_statement.Rows[0]["cashInBank"]
                    } : null;


                    hash.Add("financial_statement", financial_statement1);
                    sql = @"
                SELECT
                t.at_user_id AS UserId, gu_name as UserName,

                ABS(SUM(
                    CASE 
                        WHEN ap.ap_name = 'CASH IN HAND' 
                        THEN t.at_dr - t.at_cr 
                        ELSE 0 
                    END
                )) AS CashInHand

            FROM acc_account_transactions t
            INNER JOIN acc_subhead s 
                ON t.at_as_id = s.as_id
            INNER JOIN acc_parent ap 
                ON s.as_ap_id = ap.ap_id
            inner join gnl_users on gu_user_id=at_user_id

            WHERE ap.ap_name IN ('CASH IN HAND', 'BANK') and gu_ur_id !=1

            GROUP BY t.at_user_id,gu_name
            ORDER BY t.at_user_id;";

                    DataTable financial_statement_summery = usqlre.dbReaderFill(sql);
                    var financial_statement_summery_list =
                    financial_statement_summery.AsEnumerable()
                    .Select(r => new
                    {
                        UserId = r["UserId"],
                        UserName = r["UserName"],
                        cashInHand = r["CashInHand"]
                    })
                    .ToList();

                    hash.Add("financial_statement_summery", financial_statement_summery_list);

                    sql = @"
                 SELECT 
                CONVERT(CHAR(7), at_date, 120) AS [month],
                ISNULL(SUM(CASE 
                    WHEN at_form IN ('PAYMENT','BANK PAYMENT') 
                         AND at_Cr > 0 
                         AND (ap_name='CASH IN HAND' OR ap_name='BANK') 
                    THEN at_Cr ELSE 0 END),0) AS payment,
                ISNULL(SUM(CASE 
                    WHEN at_form IN ('RECEIPT','BANK RECEIPT') 
                         AND at_Dr > 0 
                         AND (ap_name='CASH IN HAND' OR ap_name='BANK') 
                    THEN at_Dr ELSE 0 END),0) AS receipt
            FROM acc_account_transactions 
            INNER JOIN acc_subhead ON as_id = at_as_id
            INNER JOIN acc_parent ON ap_id = as_ap_id
            WHERE CAST(at_date AS date) BETWEEN '" + model.Insight.FromDate + "' AND '" + model.Insight.ToDate + @"' AND at_location_id= '" + usqlre.locationId + @"'
            GROUP BY CONVERT(CHAR(7), at_date, 120)
            ORDER BY CONVERT(CHAR(7), at_date, 120);";

                    DataTable cashflow = usqlre.dbReaderFill(sql);
                    hash.Add("cashflow", cashflow);
                    sql = @"
                	SELECT
                    ABS(SUM(CASE WHEN a.as_ap_id = 4 THEN (t.at_Dr - t.at_Cr) ELSE 0 END)) AS Receivable,
                    ABS(SUM(CASE WHEN a.as_ap_id = 6 THEN (t.at_Dr - t.at_Cr) ELSE 0 END)) AS Payable
                FROM acc_account_transactions t
                INNER JOIN acc_subhead a 
                    ON t.at_as_id = a.as_id
                WHERE a.as_active = 1
                  AND CAST(t.at_date AS date)<='" + model.CashFlow.ToDate + @"' AND t.at_location_id= '" + usqlre.locationId + @"'
                  AND a.as_ap_id IN (4, 6);";

                    DataTable payable_recievable = usqlre.dbReaderFill(sql);
                    var payable_recievable1 = payable_recievable.Rows.Count > 0 ? new
                    {
                        payable = payable_recievable.Rows[0]["Payable"],
                        receipt = payable_recievable.Rows[0]["Receivable"]
                    } : null;

                    hash.Add("payable_recievable", payable_recievable1);
                    sql = @"
                DECLARE @StartDate DATE, @EndDate DATE;
                DECLARE @FromDate DATE, @ToDate DATE;

                SET @FromDate = '" + model.IncomeExpense.FromDate + @"';
                SET @ToDate = '" + model.IncomeExpense.ToDate + @"'; 

                SELECT @StartDate = com_sdate, @EndDate = com_edate FROM gnl_company;

                IF @FromDate < @StartDate SET @FromDate = @StartDate;
                IF @ToDate > @EndDate SET @ToDate = @EndDate;

                CREATE TABLE #months (
                    MonthYear NVARCHAR(7), 
                    Month NVARCHAR(20)
                );

                CREATE TABLE #income (
                    Month NVARCHAR(7), 
                    DirectIncome MONEY, 
                    InDirectIncome MONEY, 
                    DirectExpense MONEY, 
                    InDirectExpense MONEY
                );

                INSERT INTO #income
                SELECT 
                    CONVERT(VARCHAR(7), at_date, 120) AS Month,
                    SUM(CASE WHEN ah_name = 'DIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS DirectIncome,
                    SUM(CASE WHEN ah_name = 'INDIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS InDirectIncome,
                    SUM(CASE WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS DirectExpense,
                    SUM(CASE WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS InDirectExpense
                FROM acc_account_transactions
                INNER JOIN acc_subhead ON at_as_id = as_id
                INNER JOIN acc_parent ON as_ap_id = ap_id
                INNER JOIN acc_head ON ah_id = ap_ah_id
                WHERE ah_name IN ('DIRECT INCOME', 'INDIRECT INCOME', 'DIRECT EXPENSE', 'INDIRECT EXPENSE')
                AND at_date BETWEEN @FromDate AND @ToDate
                GROUP BY CONVERT(VARCHAR(7), at_date, 120)
                ORDER BY CONVERT(VARCHAR(7), at_date, 120);

                WITH MonthsCTE AS (
                    SELECT @FromDate AS Month
                    UNION ALL
                    SELECT DATEADD(MONTH, 1, Month)
                    FROM MonthsCTE
                    WHERE DATEADD(MONTH, 1, Month) <= @ToDate
                )
                INSERT INTO #months
                SELECT 
                    CONVERT(VARCHAR(7), Month, 120) AS MonthYear,
                    DATENAME(MONTH, CAST(CONVERT(VARCHAR(7), Month, 120) + '-01' AS DATE))
                FROM MonthsCTE
                OPTION (MAXRECURSION 0);

                SELECT 
                    m.MonthYear AS month,
                    COALESCE(i.DirectIncome, 0) + COALESCE(i.InDirectIncome, 0) AS income,
                    COALESCE(i.DirectExpense, 0) + COALESCE(i.InDirectExpense, 0) AS expense
                FROM #months m
                LEFT JOIN #income i ON i.Month = m.MonthYear
                ORDER BY MonthYear;

                DROP TABLE #months;
                DROP TABLE #income;
                ";
                    DataTable income_expense = usqlre.dbReaderFill(sql);
                    hash.Add("income_expense", income_expense);
                    sql = @"
                SELECT 
                    ah_name AS label,
                    SUM(CASE 
                        WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr
                        WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr
                        ELSE 0 
                    END) AS amount
                FROM acc_account_transactions 
                INNER JOIN acc_subhead ON at_as_id = as_id
                INNER JOIN acc_parent ON as_ap_id = ap_id
                INNER JOIN acc_head ON ah_id = ap_ah_id
                WHERE ah_name IN ('DIRECT EXPENSE', 'INDIRECT EXPENSE') 
                AND at_date BETWEEN '" + model.TopExpense.FromDate + "' AND '" + model.TopExpense.ToDate + "' AND at_location_id= '" + usqlre.locationId + @"' GROUP BY ah_name ORDER BY amount DESC";

                    DataTable top_expense = usqlre.dbReaderFill(sql);
                    hash.Add("top_expense", top_expense);
                    usqlre.close();

                }
                var response = new
                {
                    status = true,
                    status_code = 200,
                    message = "Bookings retrieved successfully",
                    data = hash
                };
                string jsonResult = ReportModelContext.searializeDt(response);
                return Content(jsonResult, "application/json");


            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching financial statement: " + ex.Message,
                    data = new object[] { }
                });
            }
        }

        [HttpPost("dashboard-insights")]
        public async Task<IActionResult> DashboardInsights([FromBody] InsightsModel model)
        {
            try
            {
                string sql = "";
                UserSqlServer usqlre = new UserSqlServer(this);
                if (model.Location != 0)
                {
                    sql = @"SELECT 
                isnull((SELECT SUM(si_grand_total) 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (1,2,6,4,8,14) and  si_location_id=" + model.Location + @" and si_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"'
				 ),0) AS totalSale,0 AS totalSaleChange,

                isnull((SELECT SUM(si_grand_total) 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (7,9,10) and si_location_id=" + model.Location + @" and si_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"'
				 ),0) AS salesReturn,0 AS salesReturnChange,

                isnull((SELECT SUM(CASE WHEN si_str_id IN (1,2,4,6,8,14) THEN si_grand_total ELSE 0 END) - 
                        SUM(CASE WHEN si_str_id IN (7,9,10) THEN si_grand_total ELSE 0 END) 
                 FROM inv_sales_inf
				 where si_location_id=" + model.Location + @" and si_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"'
				 ),0) AS netSale,0 AS netSaleChange,

                isnull((SELECT SUM(pi_grand_total) 
                 FROM inv_purchase_inf
				 where pi_location_id=" + model.Location + @" and pi_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"'
				 ),0) AS purchase,0 AS purchaseChange,

                isnull((SELECT SUM(pr_grand_total) 
                 FROM inv_purchase_rt_inf
				 where pr_location_id=" + model.Location + @" and pr_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"'
				 ),0) AS purchaseReturn,0 AS purchaseReturnChange,

                isnull((SELECT SUM(at_Dr - at_Cr) 
                 FROM acc_account_transactions 
                 WHERE at_location_id=" + model.Location + @" and at_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"'  and at_form IN ('PAYMENT','PAYMENT-I','BANK PAYMENT')),0)
				 AS payment,0 AS paymentChange,

                isnull((SELECT SUM(at_Dr - at_Cr) * -1 
                 FROM acc_account_transactions 
                 WHERE at_location_id=" + model.Location + @" and at_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"' and at_form IN ('RECEIPT','RECEIPT-I','BANK RECEIPT')),0)
				 AS receipt,0 AS recieptChange;
            ";
                }
                else 
                {
                    sql = @"SELECT 
                isnull((SELECT SUM(si_grand_total) 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (1,2,6,4,8,14) and si_date BETWEEN '"+ model.FromDate + "' AND '"+ model.ToDate + @"'
				 ),0) AS totalSale,0 AS totalSaleChange,

                isnull((SELECT SUM(si_grand_total) 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (7,9,10)  and si_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"'
				 ),0) AS salesReturn,0 AS salesReturnChange,


                isnull((SELECT SUM(CASE WHEN si_str_id IN (1,2,4,6,8,14) THEN si_grand_total ELSE 0 END) - 
                        SUM(CASE WHEN si_str_id IN (7,9,10) THEN si_grand_total ELSE 0 END) 
                 FROM inv_sales_inf
				 where si_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"'
				 ),0) AS netSale,0 AS netSaleChange,

                isnull((SELECT SUM(pi_grand_total) 
                 FROM inv_purchase_inf
				 where  pi_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"'
				 ),0) AS purchase,0 AS purchaseChange,


                isnull((SELECT SUM(pr_grand_total) 
                 FROM inv_purchase_rt_inf
				 where  pr_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"'
				 ),0) AS purchaseReturn,0 AS purchaseReturnChange,


                isnull((SELECT SUM(at_Dr - at_Cr) 
                 FROM acc_account_transactions 
                 WHERE  at_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"' and at_form IN ('PAYMENT','PAYMENT-I','BANK PAYMENT')),0)
				 AS payment,0 AS paymentChange,

                isnull((SELECT SUM(at_Dr - at_Cr) * -1 
                 FROM acc_account_transactions 
                 WHERE at_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + @"' and at_form IN ('RECEIPT','RECEIPT-I','BANK RECEIPT')),0)
				 AS receipt, 0 AS recieptChange;
            ";
                }
               
                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();
                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];

                    var data = new
                    {
                        totalSale = Convert.ToDecimal(row["totalSale"]),
                        totalSaleChange = Convert.ToDecimal(row["totalSaleChange"]),

                        salesReturn = Convert.ToDecimal(row["salesReturn"]),
                        salesReturnChange = Convert.ToDecimal(row["salesReturnChange"]),

                        netSale = Convert.ToDecimal(row["netSale"]),
                        netSaleChange = Convert.ToDecimal(row["netSaleChange"]),

                        purchase = Convert.ToDecimal(row["purchase"]),
                        purchaseChange = Convert.ToDecimal(row["purchaseChange"]),

                        purchaseReturn = Convert.ToDecimal(row["purchaseReturn"]),
                        purchaseReturnChange = Convert.ToDecimal(row["purchaseReturnChange"]),

                        payment = Convert.ToDecimal(row["payment"]),
                        paymentChange = Convert.ToDecimal(row["paymentChange"]),

                        receipt = Convert.ToDecimal(row["receipt"]),
                        recieptChange = Convert.ToDecimal(row["recieptChange"]),

                    };

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Dashboard insights fetched successfully",
                        data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No insights found",
                        data = new object[] { }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching financial statement: " + ex.Message,
                    data = new object[] { } // Empty array to match your structure
                });
            }

        }

        [HttpPost("financial-statement")]
        public async Task<IActionResult> FinancialStatement(FinancialStatementsModel model)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql;
                if (model.Location !=0)
                {
                    sql = @"
        SELECT 
            SUM(CASE WHEN ap_name = 'CASH IN HAND' THEN at_dr - at_Cr ELSE 0 END) AS cashInHand,
            SUM(CASE WHEN ap_name = 'BANK' THEN at_dr - at_Cr ELSE 0 END) AS cashInBank
        FROM acc_account_transactions 
        LEFT JOIN acc_subhead ON at_as_id = as_id 
        LEFT JOIN acc_parent ON as_ap_id = ap_id 
        WHERE ap_name IN ('CASH IN HAND', 'BANK')
         AND at_location_id="+model.Location+";";
                }
                else
                {
                    sql = @"
        SELECT 
            SUM(CASE WHEN ap_name = 'CASH IN HAND' THEN at_dr - at_Cr ELSE 0 END) AS cashInHand,
            SUM(CASE WHEN ap_name = 'BANK' THEN at_dr - at_Cr ELSE 0 END) AS cashInBank
        FROM acc_account_transactions 
        LEFT JOIN acc_subhead ON at_as_id = as_id 
        LEFT JOIN acc_parent ON as_ap_id = ap_id 
        WHERE ap_name IN ('CASH IN HAND', 'BANK');";
                }

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count > 0)
                {
                    var data = new
                    {
                        cashInHand = dt.Rows[0]["cashInHand"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["cashInHand"]) : 0,
                        cashInBank = dt.Rows[0]["cashInBank"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["cashInBank"]) : 0
                    };

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Financial statement fetched successfully",
                        data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No financial data found",
                        data = new object[] { } // Empty array to match your structure
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching financial statement: " + ex.Message,
                    data = new object[] { } // Empty array to match your structure
                });
            }
        }
        [HttpPost("financial-statement-new")]
        public async Task<IActionResult> FinancialStatementNew()
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql;
                if (usqlre.user_role == "ADMIN")
                {
                    sql = @"
        SELECT 
            ABS(SUM(CASE WHEN ap_name = 'CASH IN HAND' THEN at_dr - at_Cr ELSE 0 END)) AS cashInHand,
            ABS(SUM(CASE WHEN ap_name = 'BANK' THEN at_dr - at_Cr ELSE 0 END)) AS cashInBank
        FROM acc_account_transactions 
        LEFT JOIN acc_subhead ON at_as_id = as_id 
        LEFT JOIN acc_parent ON as_ap_id = ap_id 
        WHERE ap_name IN ('CASH IN HAND', 'BANK')";
                }
                else
                {
                    sql = $@"
        
            SELECT 
                ABS(SUM(CASE 
                        WHEN p.ap_name = 'CASH IN HAND' 
                             AND at.at_as_id = {usqlre.gu_user_cash_id}
                        THEN at.at_dr - at.at_cr 
                        ELSE 0 
                    END)) AS cashInHand,

                ABS(SUM(CASE 
                        WHEN p.ap_name = 'BANK' 
                             AND at.at_as_id = u.gu_user_bank_id
                        THEN at.at_dr - at.at_cr 
                        ELSE 0 
                    END)) AS cashInBank
            FROM acc_account_transactions at
            JOIN acc_subhead s ON at.at_as_id = s.as_id
            JOIN acc_parent p ON s.as_ap_id = p.ap_id
            LEFT JOIN gnl_users u ON u.gu_user_id = {usqlre.userId};";
                }

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count > 0)
                {
                    var data = new
                    {
                        cashInHand = dt.Rows[0]["cashInHand"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["cashInHand"]) : 0,
                        cashInBank = dt.Rows[0]["cashInBank"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["cashInBank"]) : 0
                    };

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Financial statement fetched successfully",
                        data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No financial data found",
                        data = new object[] { } // Empty array to match your structure
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching financial statement: " + ex.Message,
                    data = new object[] { } // Empty array to match your structure
                });
            }
        }

        [HttpPost("payable-receivable")]
        public async Task<IActionResult> PayableReceivable(PayableReceivablesModel model)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql;
                if (model.Location !=0)
                {
                    sql = @"
                 SELECT 
                    (SELECT SUM(at_Dr - at_Cr) 
                     FROM acc_account_transactions 
                     WHERE at_form IN ('PAYMENT','PAYMENT-I','BANK PAYMENT') 
                      AND at_location_id="+model.Location+"  AND CAST(at_date AS DATE) BETWEEN '" + model.FromDate + @"' AND '" + model.ToDate + @"') AS payment,

                    (SELECT SUM(at_Dr - at_Cr) * -1 
                     FROM acc_account_transactions 
                     WHERE at_form IN ('RECEIPT','RECEIPT-I','BANK RECEIPT') 
                      AND at_location_id="+model.Location+" AND CAST(at_date AS DATE) BETWEEN '" + model.FromDate + @"' AND '" + model.ToDate + @"') AS receipt";
                }
                else
                {
                    sql = @"
                 SELECT 
                    (SELECT SUM(at_Dr - at_Cr) 
                     FROM acc_account_transactions 
                     WHERE at_form IN ('PAYMENT','PAYMENT-I','BANK PAYMENT') 
                     AND CAST(at_date AS DATE) BETWEEN '" + model.FromDate + @"' AND '" + model.ToDate + @"') AS payment,

                    (SELECT SUM(at_Dr - at_Cr) * -1 
                     FROM acc_account_transactions 
                     WHERE at_form IN ('RECEIPT','RECEIPT-I','BANK RECEIPT') 
                     AND CAST(at_date AS DATE) BETWEEN '" + model.FromDate + @"' AND '" + model.ToDate + @"') AS receipt";

                }


                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();
                if (dt.Rows.Count > 0)
                {
                    var data = new
                    {
                        payment = dt.Rows[0]["payment"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["payment"]) : 0,
                        receipt = dt.Rows[0]["receipt"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["receipt"]) : 0
                    };

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Payable and receivable amounts fetched successfully",
                        data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No transactions found",
                        data = new object[] { } // Empty array to match your structure
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching top expenses: " + ex.Message,
                    data = new object[] { } // Empty array to match your structure
                });
            }
            
        }
        [HttpPost("cashflow")]
        public async Task<IActionResult> CashFlow(CashFlowsModel model)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql;

                if (model.Location != 0)
                {
                    sql = @"
            SELECT 
                DATENAME(MONTH, at_date) AS Month,
                SUM(CASE WHEN at_form IN ('PAYMENT', 'PAYMENT-I', 'BANK PAYMENT') THEN at_Dr - at_Cr ELSE 0 END) AS Payments,
                SUM(CASE WHEN at_form IN ('RECEIPT', 'RECEIPT-I', 'BANK RECEIPT') THEN (at_Dr - at_Cr) * -1 ELSE 0 END) AS Receipts
            FROM acc_account_transactions
            WHERE at_location_id = " + model.Location + @"
            GROUP BY DATENAME(MONTH, at_date), MONTH(at_date)
            ORDER BY MONTH(at_date);";
                }
                else
                {
                    sql = @"
            SELECT 
                DATENAME(MONTH, at_date) AS Month,
                SUM(CASE WHEN at_form IN ('PAYMENT', 'PAYMENT-I', 'BANK PAYMENT') THEN at_Dr - at_Cr ELSE 0 END) AS Payments,
                SUM(CASE WHEN at_form IN ('RECEIPT', 'RECEIPT-I', 'BANK RECEIPT') THEN (at_Dr - at_Cr) * -1 ELSE 0 END) AS Receipts
            FROM acc_account_transactions
            GROUP BY DATENAME(MONTH, at_date), MONTH(at_date)
            ORDER BY MONTH(at_date);";
                }

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count > 0)
                {
                    var data = dt.AsEnumerable().Select(row => new
                    {
                        month = row["Month"].ToString(),
                        payments = row["Payments"] != DBNull.Value ? Convert.ToDecimal(row["Payments"]) : 0,
                        receipts = row["Receipts"] != DBNull.Value ? Convert.ToDecimal(row["Receipts"]) : 0
                    }).ToList();

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Cash flow data fetched successfully",
                        data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No transactions found",
                        data = new List<object>() // Empty list instead of invalid JSON format
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching cash flow data: " + ex.Message,
                    data = new List<object>() // Empty list for consistency
                });
            }
        }

        [HttpPost("income-expense")]
        public async Task<IActionResult> IncomeExpense(IncomeExpensesModel model)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql;
                if (model.Location != 0)
                {
                    sql = @"DECLARE @StartDate DATE, @EndDate DATE;
                DECLARE @FromDate DATE, @ToDate DATE;

                SET @FromDate = '" + model.FromDate + @"';
                SET @ToDate = '" + model.ToDate + @"'; 

                SELECT @StartDate = com_sdate, @EndDate = com_edate FROM gnl_company;

                IF @FromDate < @StartDate SET @FromDate = @StartDate;
                IF @ToDate > @EndDate SET @ToDate = @EndDate;

                CREATE TABLE #months (
                    MonthYear NVARCHAR(7), 
                    Month NVARCHAR(20)
                );

                CREATE TABLE #income (
                    Month NVARCHAR(7), 
                    DirectIncome MONEY, 
                    InDirectIncome MONEY, 
                    DirectExpense MONEY, 
                    InDirectExpense MONEY
                );

                INSERT INTO #income
                SELECT 
                    CONVERT(VARCHAR(7), at_date, 120) AS Month,
                    SUM(CASE WHEN ah_name = 'DIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS DirectIncome,
                    SUM(CASE WHEN ah_name = 'INDIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS InDirectIncome,
                    SUM(CASE WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS DirectExpense,
                    SUM(CASE WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS InDirectExpense
                FROM acc_account_transactions
                INNER JOIN acc_subhead ON at_as_id = as_id
                INNER JOIN acc_parent ON as_ap_id = ap_id
                INNER JOIN acc_head ON ah_id = ap_ah_id
                WHERE ah_name IN ('DIRECT INCOME', 'INDIRECT INCOME', 'DIRECT EXPENSE', 'INDIRECT EXPENSE')
                 AND at_location_id="+model.Location+ @" AND at_date BETWEEN @FromDate AND @ToDate
                GROUP BY CONVERT(VARCHAR(7), at_date, 120)
                ORDER BY CONVERT(VARCHAR(7), at_date, 120);

                WITH MonthsCTE AS (
                    SELECT @FromDate AS Month
                    UNION ALL
                    SELECT DATEADD(MONTH, 1, Month)
                    FROM MonthsCTE
                    WHERE DATEADD(MONTH, 1, Month) <= @ToDate
                )
                INSERT INTO #months
                SELECT 
                    CONVERT(VARCHAR(7), Month, 120) AS MonthYear,
                    DATENAME(MONTH, CAST(CONCAT(CONVERT(VARCHAR(7), Month, 120), '-01') AS DATE))
                FROM MonthsCTE
                OPTION (MAXRECURSION 0);

                SELECT 
                m.Month AS month, -- Use the month name instead of MonthYear
                COALESCE(i.DirectIncome, 0) + COALESCE(i.InDirectIncome, 0) AS income,
                COALESCE(i.DirectExpense, 0) + COALESCE(i.InDirectExpense, 0) AS expense
            FROM #months m
            LEFT JOIN #income i ON i.Month = m.MonthYear
            ORDER BY MonthYear;


                DROP TABLE #months;
                DROP TABLE #income;
                ";
                }
                else
                {
                    sql = @"DECLARE @StartDate DATE, @EndDate DATE;
                DECLARE @FromDate DATE, @ToDate DATE;

                SET @FromDate = '" + model.FromDate + @"';
                SET @ToDate = '" + model.ToDate + @"'; 

                SELECT @StartDate = com_sdate, @EndDate = com_edate FROM gnl_company;

                IF @FromDate < @StartDate SET @FromDate = @StartDate;
                IF @ToDate > @EndDate SET @ToDate = @EndDate;

                CREATE TABLE #months (
                    MonthYear NVARCHAR(7), 
                    Month NVARCHAR(20)
                );

                CREATE TABLE #income (
                    Month NVARCHAR(7), 
                    DirectIncome MONEY, 
                    InDirectIncome MONEY, 
                    DirectExpense MONEY, 
                    InDirectExpense MONEY
                );

                INSERT INTO #income
                SELECT 
                    CONVERT(VARCHAR(7), at_date, 120) AS Month,
                    SUM(CASE WHEN ah_name = 'DIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS DirectIncome,
                    SUM(CASE WHEN ah_name = 'INDIRECT INCOME' THEN at_cr - at_dr ELSE 0 END) AS InDirectIncome,
                    SUM(CASE WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS DirectExpense,
                    SUM(CASE WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr ELSE 0 END) AS InDirectExpense
                FROM acc_account_transactions
                INNER JOIN acc_subhead ON at_as_id = as_id
                INNER JOIN acc_parent ON as_ap_id = ap_id
                INNER JOIN acc_head ON ah_id = ap_ah_id
                WHERE ah_name IN ('DIRECT INCOME', 'INDIRECT INCOME', 'DIRECT EXPENSE', 'INDIRECT EXPENSE')
                AND at_date BETWEEN @FromDate AND @ToDate
                GROUP BY CONVERT(VARCHAR(7), at_date, 120)
                ORDER BY CONVERT(VARCHAR(7), at_date, 120);

                WITH MonthsCTE AS (
                    SELECT @FromDate AS Month
                    UNION ALL
                    SELECT DATEADD(MONTH, 1, Month)
                    FROM MonthsCTE
                    WHERE DATEADD(MONTH, 1, Month) <= @ToDate
                )
                INSERT INTO #months
                SELECT 
                    CONVERT(VARCHAR(7), Month, 120) AS MonthYear,
                    DATENAME(MONTH, CAST(CONCAT(CONVERT(VARCHAR(7), Month, 120), '-01') AS DATE))
                FROM MonthsCTE
                OPTION (MAXRECURSION 0);

                SELECT 
                m.Month AS month, -- Use the month name instead of MonthYear
                COALESCE(i.DirectIncome, 0) + COALESCE(i.InDirectIncome, 0) AS income,
                COALESCE(i.DirectExpense, 0) + COALESCE(i.InDirectExpense, 0) AS expense
            FROM #months m
            LEFT JOIN #income i ON i.Month = m.MonthYear
            ORDER BY MonthYear;


                DROP TABLE #months;
                DROP TABLE #income;
                ";
                }



                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();
                if (dt.Rows.Count > 0)
                {
                    var data = dt.AsEnumerable()
                        .Select(row => new
                        {
                            month = row["month"].ToString(),
                            income = Convert.ToDecimal(row["income"]),
                            expense = Convert.ToDecimal(row["expense"])
                        }).ToList();

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Income and Expense data fetched successfully",
                        data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No income or expense data found",
                        data = new object[] { }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching top expenses: " + ex.Message,
                    data = new object[] { } // Empty array to match your structure
                });
            }
            
        }
        [HttpPost("top-expense")]
        public async Task<IActionResult> TopExpense(TopExpensesModel model)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                string sql;

                if (model.Location != 0)
                {
                    sql = @"
            SELECT 
                ah_name AS label,
                SUM(CASE 
                    WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr
                    WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr
                    ELSE 0 
                END) AS amount
            FROM acc_account_transactions 
            INNER JOIN acc_subhead ON at_as_id = as_id
            INNER JOIN acc_parent ON as_ap_id = ap_id
            INNER JOIN acc_head ON ah_id = ap_ah_id
            WHERE ah_name IN ('DIRECT EXPENSE', 'INDIRECT EXPENSE') 
            AND at_location_id=" + model.Location +
                    " AND at_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + "' GROUP BY ah_name ORDER BY amount DESC";
                }
                else
                {
                    sql = @"
            SELECT 
                ah_name AS label,
                SUM(CASE 
                    WHEN ah_name = 'DIRECT EXPENSE' THEN at_dr - at_cr
                    WHEN ah_name = 'INDIRECT EXPENSE' THEN at_dr - at_cr
                    ELSE 0 
                END) AS amount
            FROM acc_account_transactions 
            INNER JOIN acc_subhead ON at_as_id = as_id
            INNER JOIN acc_parent ON as_ap_id = ap_id
            INNER JOIN acc_head ON ah_id = ap_ah_id
            WHERE ah_name IN ('DIRECT EXPENSE', 'INDIRECT EXPENSE') 
            AND at_date BETWEEN '" + model.FromDate + "' AND '" + model.ToDate + "' GROUP BY ah_name ORDER BY amount DESC";
                }

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count > 0)
                {
                    var data = dt.AsEnumerable()
                        .Select(row => new
                        {
                            label = row["label"].ToString(),
                            amount = Convert.ToDecimal(row["amount"])
                        })
                        .ToList();

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Top expenses fetched successfully",
                        data = data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No expenses found",
                        data = new object[] { }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching top expenses: " + ex.Message,
                    data = new object[] { }
                });
            }

        }
        [HttpPost("top-selling-products")]
        public async Task<IActionResult> TopSellingProducts([FromBody] TopSellingProductsModel model)
        {
            var result = new List<object>();

            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                string fromDate = model.FromDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string toDate = model.ToDate.ToString("yyyy-MM-dd HH:mm:ss.fff");

                string locationCondition = model.Location != 0
                    ? $"AND si_location_id = {model.Location}" : "";

                string sql = $@"
                    SELECT TOP 5 
                        sale.ir_name AS label,
                        (ISNULL(sale.qty, 0) - ISNULL(ret.qty, 0)) AS qty,
                        (ISNULL(sale.amount, 0) - ISNULL(ret.amount, 0)) AS amount
                    FROM
                    (
                        SELECT ir_name, SUM(sp_qty) as qty, SUM(sp_gross_value) as amount
                        FROM inv_sales_par
                        INNER JOIN inv_sales_inf ON sp_entryno = si_entryno AND si_str_id = sp_str_id
                        INNER JOIN inv_item_reg ON sp_ir_id = ir_id
                        WHERE si_str_id IN (1,2,4,6,8,14)
                          AND si_date BETWEEN '{fromDate}' AND '{toDate}'
                          {locationCondition}
                        GROUP BY ir_name
                    ) sale
                    LEFT JOIN
                    (
                        SELECT ir_name, SUM(sp_qty) as qty, SUM(sp_gross_value) as amount
                        FROM inv_sales_par
                        INNER JOIN inv_sales_inf ON sp_entryno = si_entryno AND si_str_id = sp_str_id
                        INNER JOIN inv_item_reg ON sp_ir_id = ir_id
                        WHERE si_str_id IN (7,9,10)
                          AND si_date BETWEEN '{fromDate}' AND '{toDate}'
                          {locationCondition}
                        GROUP BY ir_name
                    ) ret
                    ON sale.ir_name = ret.ir_name
                    ORDER BY qty DESC";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count > 0)
                {
                    var data = dt.AsEnumerable()
                        .Select(row => new
                        {
                            label = row["label"].ToString(),
                            qty = Convert.ToInt32(row["qty"]),
                            amount = Convert.ToDecimal(row["amount"])
                        })
                        .ToList();

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Top selling products fetched successfully",
                        data = data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No products found",
                        data = new object[] { }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching top selling products: " + ex.Message,
                    data = new object[] { }
                });
            }
        }

        [HttpPost("top-customers")]
        public async Task<IActionResult> TopCustomers([FromBody] TopCustomersModel model)
        {
            var result = new List<object>();

            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                string fromDate = model.FromDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string toDate = model.ToDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string locationCondition = model.Location != 0
                    ? $"AND si_location_id = {model.Location}" : "";

                string sql = $@"
                    SELECT TOP 5 
                        sale.as_name AS customer, 
                        (ISNULL(sale.amount,0) - ISNULL(ret.amount,0)) AS amount
                    FROM (
                        SELECT as_name, si_acc_id, SUM(si_grand_total) AS amount
                        FROM inv_sales_inf
                        INNER JOIN acc_subhead ON si_acc_id = as_id
                        WHERE si_str_id IN (1,2,4,6,8,14)
                          AND si_date BETWEEN '{fromDate}' AND '{toDate}'
                          {locationCondition}
                        GROUP BY as_name, si_acc_id
                    ) sale
                    LEFT JOIN (
                        SELECT si_acc_id, SUM(si_grand_total) AS amount
                        FROM inv_sales_inf
                        WHERE si_str_id IN (7,9,10)
                          AND si_date BETWEEN '{fromDate}' AND '{toDate}'
                          {locationCondition}
                        GROUP BY si_acc_id
                    ) ret
                    ON sale.si_acc_id = ret.si_acc_id
                    ORDER BY amount DESC";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count > 0)
                {
                    var data = dt.AsEnumerable()
                        .Select(row => new
                        {
                            customer = row["customer"].ToString(),
                            amount = Convert.ToDecimal(row["amount"])
                        })
                        .ToList();

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Top customers fetched successfully",
                        data = data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No customers found",
                        data = new object[] { }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching top customers: " + ex.Message,
                    data = new object[] { }
                });
            }
        }

        [HttpPost("top-suppliers")]
        public async Task<IActionResult> TopSuppliers([FromBody] TopCreditorsModel model)
        {
            var result = new List<object>();

            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                string fromDate = model.FromDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string toDate = model.ToDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string locationCondition = model.Location != 0
                    ? $"AND pi_location_id = {model.Location}" : "";

                string sql = $@"
                SELECT TOP 5
                    purchase.as_name AS supplier,
                    (ISNULL(purchase.amount,0) - ISNULL(ret.amount,0)) AS amount
                FROM (
                    SELECT as_name, pi_sup_id, SUM(pi_grand_total) AS amount
                    FROM inv_purchase_inf
                    INNER JOIN acc_subhead ON pi_sup_id = as_id
                    WHERE pi_inv_date BETWEEN '{fromDate}' AND '{toDate}'
                      {locationCondition}
                    GROUP BY as_name, pi_sup_id
                ) purchase
                LEFT JOIN (
                    SELECT pr_sup_id, SUM(pr_grand_total) AS amount
                    FROM inv_purchase_rt_inf
                    WHERE pr_inv_date BETWEEN '{fromDate}' AND '{toDate}'
                      {locationCondition.Replace("pi_location_id", "pr_location_id")}
                    GROUP BY pr_sup_id
                ) ret
                ON purchase.pi_sup_id = ret.pr_sup_id
                ORDER BY amount DESC";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count > 0)
                {
                    var data = dt.AsEnumerable()
                        .Select(row => new
                        {
                            supplier = row["supplier"].ToString(),
                            amount = Convert.ToDecimal(row["amount"])
                        })
                        .ToList();

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Top suppliers fetched successfully",
                        data = data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No supplier found",
                        data = new object[] { }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching top supplier: " + ex.Message,
                    data = new object[] { }
                });
            }
        }
        [HttpPost("top-creditors")]
        public async Task<IActionResult> TopCreditors([FromBody] TopCreditorsModel model)
        {
            var result = new List<object>();

            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                string fromDate = model.FromDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string toDate = model.ToDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string locationCondition = model.Location != 0
                    ? $"AND at_location_id = {model.Location}" : "";

                string sql = $@"
                SELECT top 5 as_name AS Particulars, 
                       MAX(as_add1) AS Address1, 
                       MAX(as_add2) AS Address2, 
                       MAX(as_mob) AS Mobile, 
					   MAX(as_category) AS Category,
                       MAX(as_agency_name) AS Agency,
                       CASE WHEN SUM(at_Dr) - SUM(at_Cr) > 0 THEN dbo.MoneyToN(SUM(at_Dr) - SUM(at_Cr), 4) ELSE dbo.MoneyToN(0, 4) END AS Debit,
                       CASE WHEN SUM(at_Cr) - SUM(at_Dr) > 0 THEN dbo.MoneyToN(SUM(at_Cr) - SUM(at_Dr), 4) ELSE dbo.MoneyToN(0, 4) END AS Credit,
                       CASE 
                            WHEN SUM(at_Dr) - SUM(at_Cr) > 0 AND SUM(at_Cr) - SUM(at_Dr) <= 0 THEN dbo.MoneyToN(SUM(at_Dr) - SUM(at_Cr), 4)
                            WHEN SUM(at_Dr) - SUM(at_Cr) <= 0 AND SUM(at_Cr) - SUM(at_Dr) > 0 THEN -(dbo.MoneyToN(SUM(at_Cr) - SUM(at_Dr), 4))
                            ELSE dbo.MoneyToN(SUM(at_Dr) - SUM(at_Cr), 4) - dbo.MoneyToN(SUM(at_Cr) - SUM(at_Dr), 4)
                       END AS Balance,
                       MAX(as_id) AS as_id 
                FROM acc_account_transactions 
                INNER JOIN acc_subhead a ON at_as_id = a.as_id 
                INNER JOIN acc_parent ON a.as_ap_id = ap_id 
				where a.as_ap_id=6   AND CAST(at_date AS DATE) >= '{fromDate}' {locationCondition}
                AND CAST(at_date AS DATE) <= '{toDate}' 
                AND as_active = 1 
                GROUP BY as_name order by Balance desc";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count > 0)
                {
                    var data = dt.AsEnumerable()
                        .Select(row => new
                        {
                            creditor = row["Particulars"].ToString(),
                            amount = Convert.ToDecimal(row["Balance"])
                        })
                        .ToList();

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Top creditors fetched successfully",
                        data = data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No creditors found",
                        data = new object[] { }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching top creditors: " + ex.Message,
                    data = new object[] { }
                });
            }
        }
        [HttpPost("top-debitors")]
        public async Task<IActionResult> TopDebitors([FromBody] TopCreditorsModel model)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                string fromDate = model.FromDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string toDate = model.ToDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string locationCondition = model.Location != 0
                    ? $"AND at_location_id = {model.Location}" : "";

                string sql = $@"
                SELECT top 5 as_name AS Particulars, 
                       MAX(as_add1) AS Address1, 
                       MAX(as_add2) AS Address2, 
                       MAX(as_mob) AS Mobile, 
					   MAX(as_category) AS Category,
                       MAX(as_agency_name) AS Agency,
                       CASE WHEN SUM(at_Dr) - SUM(at_Cr) > 0 THEN dbo.MoneyToN(SUM(at_Dr) - SUM(at_Cr), 4) ELSE dbo.MoneyToN(0, 4) END AS Debit,
                       CASE WHEN SUM(at_Cr) - SUM(at_Dr) > 0 THEN dbo.MoneyToN(SUM(at_Cr) - SUM(at_Dr), 4) ELSE dbo.MoneyToN(0, 4) END AS Credit,
                       CASE 
                            WHEN SUM(at_Dr) - SUM(at_Cr) > 0 AND SUM(at_Cr) - SUM(at_Dr) <= 0 THEN dbo.MoneyToN(SUM(at_Dr) - SUM(at_Cr), 4)
                            WHEN SUM(at_Dr) - SUM(at_Cr) <= 0 AND SUM(at_Cr) - SUM(at_Dr) > 0 THEN -(dbo.MoneyToN(SUM(at_Cr) - SUM(at_Dr), 4))
                            ELSE dbo.MoneyToN(SUM(at_Dr) - SUM(at_Cr), 4) - dbo.MoneyToN(SUM(at_Cr) - SUM(at_Dr), 4)
                       END AS Balance,
                       MAX(as_id) AS as_id 
                FROM acc_account_transactions 
                INNER JOIN acc_subhead a ON at_as_id = a.as_id 
                INNER JOIN acc_parent ON a.as_ap_id = ap_id 
				where a.as_ap_id=4   AND CAST(at_date AS DATE) >= '{fromDate}' {locationCondition}
                AND CAST(at_date AS DATE) <= '{toDate}' 
                AND as_active = 1 
                GROUP BY as_name order by Balance desc";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count > 0)
                {
                    var data = dt.AsEnumerable()
                        .Select(row => new
                        {
                            debitor = row["Particulars"].ToString(),
                            amount = Convert.ToDecimal(row["Balance"])
                        })
                        .ToList();

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Top debitors fetched successfully",
                        data = data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No debitors found",
                        data = new object[] { }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching top debitors: " + ex.Message,
                    data = new object[] { }
                });
            }
        }
        [HttpPost("top-aging-bills")]
        public IActionResult TopAgingBills([FromBody] TopAgingBillsModel model)
        {
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);
                int locId = model.Location; // 0 = all

                string sql = "";

                if (model.GroupName == "customer")
                {
                    sql = $@"
                SELECT TOP 5
                    si_loc_entryno AS BillNo,
                    si_date AS BillDate,
                    b.as_name AS PartyName,
                    gl_name AS Location,
                    si_grand_total AS InvoicedAmount,

                    CASE 
                        WHEN Balanceamt > 0 THEN si_grand_total - Balanceamt 
                        ELSE si_grand_total 
                    END AS PaidAmount,

                    Balanceamt AS PendingAmount,

                    CASE 
                        WHEN Balanceamt > 0 
                            THEN DATEDIFF(DAY, si_date, GETDATE()) 
                        ELSE 0 
                    END AS AgeOfBill,

                    CASE 
                        WHEN (DATEDIFF(DAY, si_date, GETDATE()) - a.as_credit_days) > 0  
                             AND a.as_credit_days > 0
                            THEN (DATEDIFF(DAY, si_date, GETDATE()) - a.as_credit_days)
                        ELSE 0
                    END AS DueDays,

                    SalesMan
                FROM tvf_billwisecustall('', 0, {locId}) b
                LEFT JOIN acc_subhead a ON b.as_name = a.as_name
                LEFT JOIN gnl_location ON gl_id = a.as_location_id
                WHERE Balanceamt > 0 and a.as_ap_id=4
                ORDER BY AgeOfBill DESC;";
                }
                else if (model.GroupName == "supplier")
                {
                    sql = $@"
                SELECT TOP 5
                    pi_entryno AS BillNo,
                    pi_date AS BillDate,
                    a.as_name AS PartyName,
                    NULL AS Location,
                    pi_grand_total AS InvoicedAmount,

                    CASE 
                        WHEN Balanceamt > 0 THEN pi_grand_total - Balanceamt 
                        ELSE pi_grand_total 
                    END AS PaidAmount,

                    Balanceamt AS PendingAmount,

                    CASE 
                        WHEN Balanceamt > 0 
                            THEN DATEDIFF(DAY, pi_date, GETDATE()) 
                        ELSE 0 
                    END AS AgeOfBill,

                    CASE 
                        WHEN (DATEDIFF(DAY, pi_date, GETDATE()) - a.as_credit_days) > 0  
                             AND a.as_credit_days > 0
                            THEN (DATEDIFF(DAY, pi_date, GETDATE()) - a.as_credit_days)
                        ELSE 0
                    END AS DueDays,

                    NULL AS SalesMan
                FROM tvf_billwisesupplierall('', 0, {locId}) t
                INNER JOIN acc_subhead a ON t.as_name = a.as_name
                WHERE Balanceamt > 0 and a.as_ap_id=6
                ORDER BY AgeOfBill DESC;";
                }
                else
                {
                    return BadRequest("Invalid GroupName");
                }

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count == 0)
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No aging bills found",
                        data = new object[] { }
                    });
                }

                var data = dt.AsEnumerable()
                    .Select(row => new
                    {
                        billNo = row["BillNo"].ToString(),
                        billDate = Convert.ToDateTime(row["BillDate"]),
                        partyName = row["PartyName"].ToString(),
                        location = row["Location"]?.ToString(),
                        invoicedAmount = Convert.ToDecimal(row["InvoicedAmount"]),
                        paidAmount = Convert.ToDecimal(row["PaidAmount"]),
                        pendingAmount = Convert.ToDecimal(row["PendingAmount"]),
                        ageOfBill = Convert.ToInt32(row["AgeOfBill"]),
                        dueDays = Convert.ToInt32(row["DueDays"]),
                        salesMan = row["SalesMan"]?.ToString()
                    })
                    .ToList();

                return Ok(new
                {
                    status = true,
                    status_code = 200,
                    message = "Top aging bills fetched successfully",
                    data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "Error while fetching aging bills: " + ex.Message,
                    data = new object[] { }
                });
            }
        }


        [HttpPost("top-customer-profit")]
        public async Task<IActionResult> TopCustomerProfit([FromBody] TopCustomerProfitModel model)
        {
            var result = new List<object>();
            try
            {
                UserSqlServer usqlre = new UserSqlServer(this);

                string fromDate = model.FromDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string toDate = model.ToDate.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string locationCondition = model.Location != 0
                    ? $"AND si_location_id = {model.Location}" : "";

                string totalSql = $@"
            SELECT 
                ISNULL(SUM(main.profit),0) - ISNULL(SUM(ret.profit),0) AS TotalProfit
            FROM 
                (SELECT SUM(si_profit) AS profit 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (1,2,4,6,8,14) 
                 AND si_date BETWEEN '{fromDate}' AND '{toDate}' 
                 {locationCondition}) main,
                (SELECT SUM(si_profit) AS profit 
                 FROM inv_sales_inf 
                 WHERE si_str_id IN (7,9,10) 
                 AND si_date BETWEEN '{fromDate}' AND '{toDate}' 
                 {locationCondition}) ret
        ";
                DataTable totalDt = usqlre.dbReaderFill(totalSql);
                decimal totalProfit = 0;
                if (totalDt.Rows.Count > 0 && totalDt.Rows[0]["TotalProfit"] != DBNull.Value)
                    totalProfit = Convert.ToDecimal(totalDt.Rows[0]["TotalProfit"]);

                string sql = $@"
            SELECT TOP 5 main.as_name AS customer, 
                (ISNULL(main.profit,0) - ISNULL(ret.profit,0)) AS profit
            FROM (
                SELECT as_name, si_acc_id, SUM(si_profit) AS profit 
                FROM inv_sales_inf
                INNER JOIN acc_subhead ON si_acc_id = as_id
                WHERE si_str_id IN (1,2,4,6,8,14)
                AND si_date BETWEEN '{fromDate}' AND '{toDate}'
                {locationCondition}
                GROUP BY as_name, si_acc_id
            ) main
            LEFT JOIN (
                SELECT si_acc_id, SUM(si_profit) AS profit 
                FROM inv_sales_inf
                WHERE si_str_id IN (7,9,10)
                AND si_date BETWEEN '{fromDate}' AND '{toDate}'
                {locationCondition}
                GROUP BY si_acc_id
            ) ret ON main.si_acc_id = ret.si_acc_id
            ORDER BY profit DESC";

                DataTable dt = usqlre.dbReaderFill(sql);
                usqlre.close();

                if (dt.Rows.Count > 0)
                {


                    var data = dt.AsEnumerable()
                        .Select(row => new
                        {
                            customer = row["customer"].ToString(),
                            profit = Convert.ToDecimal(row["profit"]),
                            percentage = totalProfit > 0
                                ? Math.Round(Convert.ToDecimal(row["profit"]) * 100 / totalProfit, 1)
                                : 0
                        })
                        .ToList();

                    return Ok(new
                    {
                        status = true,
                        status_code = 200,
                        message = "Top customer profits fetched successfully",
                        total_profit = totalProfit,
                        data = data
                    });
                }
                else
                {
                    return Ok(new
                    {
                        status = false,
                        status_code = 404,
                        message = "No customer profit found",
                        total_profit = 0,
                        data = new object[] { }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    status_code = 500,
                    message = "An error occurred while fetching top customer profits: " + ex.Message,
                    total_profit = 0,
                    data = new object[] { }
                });
            }
        }




    }
}


