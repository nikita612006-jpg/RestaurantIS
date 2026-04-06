using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace RestaurantIS
{
    public class ReportGenerator
    {
        private SQLiteConnection connection;

        public ReportGenerator(SQLiteConnection conn)
        {
            connection = conn;
        }

        public DataTable GetPopularityReport()
        {
            string query = @"
                SELECT d.Name AS Блюдо, 
                       SUM(oi.Quantity) AS Количество_продаж,
                       SUM(oi.Quantity * oi.PriceAtOrder) AS Выручка
                FROM OrderItems oi 
                JOIN Dishes d ON oi.DishId = d.Id
                GROUP BY d.Id, d.Name 
                ORDER BY Количество_продаж DESC";
            
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        public void ShowReport(DataGridView dgv)
        {
            dgv.DataSource = GetPopularityReport();
        }

        public DataTable GetRevenueReport()
        {
            string query = @"
                SELECT DATE(OrderDate) AS Дата, 
                       COUNT(Id) AS Количество_заказов, 
                       SUM(TotalAmount) AS Выручка
                FROM Orders 
                WHERE Status='Closed' 
                GROUP BY DATE(OrderDate) 
                ORDER BY Дата DESC";
            
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }
    }
}