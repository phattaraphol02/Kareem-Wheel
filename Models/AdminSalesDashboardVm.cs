namespace CS66_WebApplication1.Models
{
    public class AdminSalesDashboardVm
    {
        public decimal TodaySales { get; set; }
        public decimal ThisMonthSales { get; set; }

        public List<DailySalesRow> DailySales { get; set; } = new();
        public List<MonthlySalesRow> MonthlySales { get; set; } = new();
    }


    public class DailySalesRow
    {
        public DateTime Date { get; set; }
        public int OrdersCount { get; set; }
        public decimal Total { get; set; }
    }

    public class MonthlySalesRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int OrdersCount { get; set; }
        public decimal Total { get; set; }
    }
}
