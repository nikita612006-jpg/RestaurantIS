using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace RestaurantIS
{
    public partial class Form1 : Form
    {
        private SQLiteConnection connection;
        private DataGridView dgvDishes;
        private DataGridView dgvTables;
        private TextBox txtSearch;
        private ComboBox cmbCategoryFilter;
        private ComboBox cmbTableStatusFilter;
        private Button btnSearch;
        private Button btnRefresh;
        private Button btnShowPopularityReport;
        private Button btnShowRevenueReport;
        private Button btnShowTableLoadReport;
        private TabControl tabControl;
        private Label lblStatus;

        public Form1()
        {
            InitializeMyComponents(); // Переименовано, чтобы избежать конфликта
            InitializeDatabase();
            LoadData();
        }

        private void InitializeMyComponents()
        {
            this.Text = "ИС Ресторан - Управление";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            tabControl = new TabControl { Dock = DockStyle.Fill };

            // ========== Вкладка "Блюда" ==========
            TabPage tabDishes = new TabPage("Блюда");
            Panel panelDishes = new Panel { Dock = DockStyle.Fill };

            Label lblSearch = new Label { Text = "Поиск:", Location = new Point(10, 10), Size = new Size(50, 25) };
            txtSearch = new TextBox { Location = new Point(70, 10), Size = new Size(200, 25) };

            Label lblCategory = new Label { Text = "Категория:", Location = new Point(290, 10), Size = new Size(70, 25) };
            cmbCategoryFilter = new ComboBox { Location = new Point(370, 10), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCategoryFilter.Items.Add("Все");
            cmbCategoryFilter.Items.Add("Салат");
            cmbCategoryFilter.Items.Add("Горячее");
            cmbCategoryFilter.Items.Add("Десерт");
            cmbCategoryFilter.Items.Add("Напиток");
            cmbCategoryFilter.SelectedIndex = 0;

            btnSearch = new Button { Text = "Поиск", Location = new Point(540, 8), Size = new Size(100, 30) };
            btnRefresh = new Button { Text = "Обновить", Location = new Point(650, 8), Size = new Size(100, 30) };

            dgvDishes = new DataGridView { Location = new Point(10, 50), Size = new Size(1050, 250), AllowUserToAddRows = false };
            dgvDishes.ReadOnly = true;
            dgvDishes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            panelDishes.Controls.Add(lblSearch);
            panelDishes.Controls.Add(txtSearch);
            panelDishes.Controls.Add(lblCategory);
            panelDishes.Controls.Add(cmbCategoryFilter);
            panelDishes.Controls.Add(btnSearch);
            panelDishes.Controls.Add(btnRefresh);
            panelDishes.Controls.Add(dgvDishes);
            tabDishes.Controls.Add(panelDishes);

            // ========== Вкладка "Столы" ==========
            TabPage tabTables = new TabPage("Столы");
            Panel panelTables = new Panel { Dock = DockStyle.Fill };

            Label lblStatusFilter = new Label { Text = "Статус:", Location = new Point(10, 10), Size = new Size(50, 25) };
            cmbTableStatusFilter = new ComboBox { Location = new Point(70, 10), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTableStatusFilter.Items.Add("Все");
            cmbTableStatusFilter.Items.Add("Свободен");
            cmbTableStatusFilter.Items.Add("Занят");
            cmbTableStatusFilter.Items.Add("Забронирован");
            cmbTableStatusFilter.SelectedIndex = 0;

            Button btnFilterTables = new Button { Text = "Поиск", Location = new Point(240, 8), Size = new Size(100, 30) };
            Button btnRefreshTables = new Button { Text = "Обновить", Location = new Point(350, 8), Size = new Size(100, 30) };

            dgvTables = new DataGridView { Location = new Point(10, 50), Size = new Size(1050, 250), AllowUserToAddRows = false };
            dgvTables.ReadOnly = true;
            dgvTables.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            panelTables.Controls.Add(lblStatusFilter);
            panelTables.Controls.Add(cmbTableStatusFilter);
            panelTables.Controls.Add(btnFilterTables);
            panelTables.Controls.Add(btnRefreshTables);
            panelTables.Controls.Add(dgvTables);
            tabTables.Controls.Add(panelTables);

            // ========== Вкладка "Отчёты" ==========
            TabPage tabReports = new TabPage("Отчёты");
            Panel panelReports = new Panel { Dock = DockStyle.Fill };

            btnShowPopularityReport = new Button { Text = "Популярность блюд", Location = new Point(20, 20), Size = new Size(200, 40) };
            btnShowRevenueReport = new Button { Text = "Выручка по дням", Location = new Point(240, 20), Size = new Size(200, 40) };
            btnShowTableLoadReport = new Button { Text = "Загрузка столов", Location = new Point(460, 20), Size = new Size(200, 40) };

            DataGridView dgvReport = new DataGridView { Location = new Point(20, 80), Size = new Size(1050, 350), AllowUserToAddRows = false, ReadOnly = true };

            btnShowPopularityReport.Click += (s, e) => ShowPopularityReport(dgvReport);
            btnShowRevenueReport.Click += (s, e) => ShowRevenueReport(dgvReport);
            btnShowTableLoadReport.Click += (s, e) => ShowTableLoadReport(dgvReport);

            panelReports.Controls.Add(btnShowPopularityReport);
            panelReports.Controls.Add(btnShowRevenueReport);
            panelReports.Controls.Add(btnShowTableLoadReport);
            panelReports.Controls.Add(dgvReport);
            tabReports.Controls.Add(panelReports);

            tabControl.TabPages.Add(tabDishes);
            tabControl.TabPages.Add(tabTables);
            tabControl.TabPages.Add(tabReports);

            lblStatus = new Label { Text = "Готово", Dock = DockStyle.Bottom, Height = 25, BackColor = Color.LightGray, TextAlign = ContentAlignment.MiddleLeft };

            this.Controls.Add(tabControl);
            this.Controls.Add(lblStatus);

            // Подписка на события
            btnSearch.Click += (s, e) => SearchDishes();
            btnRefresh.Click += (s, e) => LoadDishes();
            btnFilterTables.Click += (s, e) => FilterTables();
            btnRefreshTables.Click += (s, e) => LoadTables();
        }

        private void InitializeDatabase()
        {
            string dbPath = "RestaurantDB.sqlite";
            connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            connection.Open();

            string createDishesTable = @"
                CREATE TABLE IF NOT EXISTS Dishes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    Price REAL NOT NULL,
                    CookingTime INTEGER NOT NULL
                )";

            string createTablesTable = @"
                CREATE TABLE IF NOT EXISTS RestaurantTables (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TableNumber INTEGER NOT NULL UNIQUE,
                    SeatsCount INTEGER NOT NULL,
                    Status TEXT NOT NULL
                )";

            string createOrdersTable = @"
                CREATE TABLE IF NOT EXISTS Orders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TableId INTEGER NOT NULL,
                    OrderDate TEXT NOT NULL,
                    TotalAmount REAL NOT NULL,
                    FOREIGN KEY (TableId) REFERENCES RestaurantTables(Id)
                )";

            string createOrderItemsTable = @"
                CREATE TABLE IF NOT EXISTS OrderItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId INTEGER NOT NULL,
                    DishId INTEGER NOT NULL,
                    Quantity INTEGER NOT NULL,
                    PriceAtOrder REAL NOT NULL,
                    FOREIGN KEY (OrderId) REFERENCES Orders(Id),
                    FOREIGN KEY (DishId) REFERENCES Dishes(Id)
                )";

            using (SQLiteCommand cmd = new SQLiteCommand(createDishesTable, connection))
                cmd.ExecuteNonQuery();
            using (SQLiteCommand cmd = new SQLiteCommand(createTablesTable, connection))
                cmd.ExecuteNonQuery();
            using (SQLiteCommand cmd = new SQLiteCommand(createOrdersTable, connection))
                cmd.ExecuteNonQuery();
            using (SQLiteCommand cmd = new SQLiteCommand(createOrderItemsTable, connection))
                cmd.ExecuteNonQuery();

            // Заполнение тестовыми данными, если таблицы пусты
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Dishes", connection))
            {
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                {
                    InsertTestData();
                }
            }
        }

        private void InsertTestData()
        {
            string[] dishes = {
                "INSERT INTO Dishes (Name, Category, Price, CookingTime) VALUES ('Цезарь с курицей', 'Салат', 350, 10)",
                "INSERT INTO Dishes (Name, Category, Price, CookingTime) VALUES ('Греческий салат', 'Салат', 280, 8)",
                "INSERT INTO Dishes (Name, Category, Price, CookingTime) VALUES ('Стейк Рибай', 'Горячее', 850, 20)",
                "INSERT INTO Dishes (Name, Category, Price, CookingTime) VALUES ('Паста Карбонара', 'Горячее', 420, 15)",
                "INSERT INTO Dishes (Name, Category, Price, CookingTime) VALUES ('Тирамису', 'Десерт', 250, 5)",
                "INSERT INTO Dishes (Name, Category, Price, CookingTime) VALUES ('Чизкейк', 'Десерт', 220, 5)",
                "INSERT INTO Dishes (Name, Category, Price, CookingTime) VALUES ('Кола', 'Напиток', 120, 1)",
                "INSERT INTO Dishes (Name, Category, Price, CookingTime) VALUES ('Чай', 'Напиток', 80, 3)"
            };
            foreach (string dish in dishes)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(dish, connection))
                    cmd.ExecuteNonQuery();
            }

            string[] tables = {
                "INSERT INTO RestaurantTables (TableNumber, SeatsCount, Status) VALUES (1, 2, 'Свободен')",
                "INSERT INTO RestaurantTables (TableNumber, SeatsCount, Status) VALUES (2, 2, 'Свободен')",
                "INSERT INTO RestaurantTables (TableNumber, SeatsCount, Status) VALUES (3, 4, 'Свободен')",
                "INSERT INTO RestaurantTables (TableNumber, SeatsCount, Status) VALUES (4, 4, 'Занят')",
                "INSERT INTO RestaurantTables (TableNumber, SeatsCount, Status) VALUES (5, 6, 'Свободен')",
                "INSERT INTO RestaurantTables (TableNumber, SeatsCount, Status) VALUES (6, 6, 'Забронирован')"
            };
            foreach (string table in tables)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(table, connection))
                    cmd.ExecuteNonQuery();
            }

            // Тестовые заказы для отчётов
            string[] orders = {
                "INSERT INTO Orders (TableId, OrderDate, TotalAmount) VALUES (4, '2026-04-04', 1250)",
                "INSERT INTO Orders (TableId, OrderDate, TotalAmount) VALUES (3, '2026-04-04', 850)",
                "INSERT INTO Orders (TableId, OrderDate, TotalAmount) VALUES (1, '2026-04-03', 420)"
            };
            foreach (string order in orders)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(order, connection))
                    cmd.ExecuteNonQuery();
            }

            string[] orderItems = {
                "INSERT INTO OrderItems (OrderId, DishId, Quantity, PriceAtOrder) VALUES (1, 1, 2, 350)",
                "INSERT INTO OrderItems (OrderId, DishId, Quantity, PriceAtOrder) VALUES (1, 3, 1, 850)",
                "INSERT INTO OrderItems (OrderId, DishId, Quantity, PriceAtOrder) VALUES (2, 4, 2, 420)",
                "INSERT INTO OrderItems (OrderId, DishId, Quantity, PriceAtOrder) VALUES (3, 2, 1, 280)"
            };
            foreach (string item in orderItems)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(item, connection))
                    cmd.ExecuteNonQuery();
            }
        }

        private void LoadData()
        {
            LoadDishes();
            LoadTables();
        }

        private void LoadDishes()
        {
            string query = "SELECT Id, Name AS Название, Category AS Категория, Price AS Цена, CookingTime AS Время_приготовления FROM Dishes";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvDishes.DataSource = dt;
            dgvDishes.AutoResizeColumns();
        }

        private void LoadTables()
        {
            string query = "SELECT Id, TableNumber AS Номер_стола, SeatsCount AS Мест, Status AS Статус FROM RestaurantTables";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvTables.DataSource = dt;
            dgvTables.AutoResizeColumns();
        }

        private void SearchDishes()
        {
            string searchText = txtSearch.Text.Trim();
            string category = cmbCategoryFilter.SelectedItem.ToString();

            string query = "SELECT Id, Name AS Название, Category AS Категория, Price AS Цена, CookingTime AS Время_приготовления FROM Dishes WHERE 1=1";

            if (!string.IsNullOrEmpty(searchText))
            {
                query += $" AND (Name LIKE '%{searchText}%' OR Category LIKE '%{searchText}%')";
            }

            if (category != "Все")
            {
                query += $" AND Category = '{category}'";
            }

            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvDishes.DataSource = dt;
            lblStatus.Text = $"Найдено блюд: {dt.Rows.Count}";
        }

        private void FilterTables()
        {
            string status = cmbTableStatusFilter.SelectedItem.ToString();
            string query = "SELECT Id, TableNumber AS Номер_стола, SeatsCount AS Мест, Status AS Статус FROM RestaurantTables";

            if (status != "Все")
            {
                query += $" WHERE Status = '{status}'";
            }

            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvTables.DataSource = dt;
            lblStatus.Text = $"Столов с фильтром '{status}': {dt.Rows.Count}";
        }

        private void ShowPopularityReport(DataGridView dgvReport)
        {
            string query = @"
                SELECT 
                    d.Name AS Блюдо,
                    SUM(oi.Quantity) AS Количество_продаж,
                    SUM(oi.Quantity * oi.PriceAtOrder) AS Выручка
                FROM OrderItems oi
                JOIN Dishes d ON oi.DishId = d.Id
                GROUP BY d.Id, d.Name
                ORDER BY Количество_продаж DESC";

            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvReport.DataSource = dt;
            dgvReport.AutoResizeColumns();
            lblStatus.Text = "Отчёт о популярности блюд сформирован";
        }

        private void ShowRevenueReport(DataGridView dgvReport)
        {
            string query = @"
                SELECT 
                    OrderDate AS Дата,
                    COUNT(Id) AS Количество_заказов,
                    SUM(TotalAmount) AS Выручка
                FROM Orders
                GROUP BY OrderDate
                ORDER BY OrderDate DESC";

            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvReport.DataSource = dt;
            dgvReport.AutoResizeColumns();
            lblStatus.Text = "Отчёт о выручке сформирован";
        }

        private void ShowTableLoadReport(DataGridView dgvReport)
        {
            string query = @"
                SELECT 
                    t.TableNumber AS Номер_стола,
                    t.SeatsCount AS Мест,
                    t.Status AS Статус,
                    COUNT(o.Id) AS Количество_заказов
                FROM RestaurantTables t
                LEFT JOIN Orders o ON t.Id = o.TableId
                GROUP BY t.Id, t.TableNumber, t.SeatsCount, t.Status
                ORDER BY Количество_заказов DESC";

            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvReport.DataSource = dt;
            dgvReport.AutoResizeColumns();
            lblStatus.Text = "Отчёт о загрузке столов сформирован";
        }
    }
}