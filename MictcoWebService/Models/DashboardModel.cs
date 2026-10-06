using System;

namespace MictcoWebService.Models
{

    public class DashboardModel
    {
        public int Location { get; set; }     
        public InsightModel Insight { get; set; }
        public PayableReceivableModel PayableReceivable { get; set; }

        public CashFlowModel CashFlow { get; set; }
        public IncomeExpenseModel IncomeExpense { get; set; }
        public TopExpenseModel TopExpense { get; set; }

    }

    public class InsightModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
    
    public class PayableReceivableModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
    public class    CashFlowModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
    public class IncomeExpenseModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
    public class TopExpenseModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class InsightsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Location { get; set; }

    }
    public class FinancialStatementsModel
    {
        public int Location { get; set; }

    }
    public class PayableReceivablesModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Location { get; set; }

    }
    public class CashFlowsModel
    {
      
        public int Location { get; set; }

    }
    public class IncomeExpensesModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Location { get; set; }

    }
    public class TopExpensesModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Location { get; set; }

    }
    public class TopSellingProductsModel
    {
        public int Location { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
    public class TopCustomersModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Location { get; set; }
    }

    public class TopCreditorsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Location { get; set; }
    }

    public class TopAgingBillsModel
    {
        public string GroupName { get; set; }
        public int Location { get; set; }
    }
    public class TopCustomerProfitModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Location { get; set; }
    }





}



