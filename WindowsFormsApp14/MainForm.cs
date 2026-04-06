using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace RestaurantIS
{
    public partial class MainForm : Form
    {
        private SQLiteConnection connection;
        private int currentUserId;
        private string currentLogin;
        private string currentRole;
        private string currentFullName;

        private TabControl tabControl;
        private DataGridView dgvDishes;
        private DataGridView dgvTables;
        private DataGridView dgvOrders;
        private DataGridView dgvOrderItems;
        private TextBox txtSearch;
        private ComboBox cmbCategoryFilter;
        private ComboBox cmbTableStatusFilter;
        private ComboBox cmbSeatsFilter;  // Фильтр по количеству мест
        private Label lblStatus;

        private Button btnAddDish;
        private Button btnEditDish;
        private Button btnDeleteDish;
        private Button btnAddTable;
        private Button btnEditTable;
        private Button btnDeleteTable;
        private Button btnChangeTableStatus;
        private Button btnCreateOrder;
        private Button btnAddToOrder;
        private Button btnCloseOrder;
        private ComboBox cmbSelectTable;
        private ComboBox cmbSelectDish;
        private NumericUpDown nudQuantity;
        private TextBox txtComment;
        private Label lblCurrentOrderTotal;
        private int currentOrderId = -1;

        public MainForm(SQLiteConnection conn, int userId, string login, string role, string fullName)
        {
            connection = conn;
            currentUserId = userId;
            currentLogin = login;
            currentRole = role;
            currentFullName = fullName;
            InitializeComponent();
            LoadData();
            ApplyRolePermissions();
        }

        private void InitializeComponent()
        {
            this.Text = $"ИС Ресторан - {currentFullName} ({GetRoleName(currentRole)})";
            this.Size = new Size(1300, 750);
            this.StartPosition = FormStartPosition.CenterScreen;

            tabControl = new TabControl { Dock = DockStyle.Fill };

            TabPage tabDishes = CreateDishesTab();
            TabPage tabTables = CreateTablesTab();
            TabPage tabOrders = CreateOrdersTab();
            TabPage tabReports = CreateReportsTab();

            tabControl.TabPages.Add(tabDishes);
            tabControl.TabPages.Add(tabTables);
            tabControl.TabPages.Add(tabOrders);
            tabControl.TabPages.Add(tabReports);

            if (currentRole == "Admin")
            {
                TabPage tabUsers = CreateUsersTab();
                tabControl.TabPages.Add(tabUsers);
            }

            lblStatus = new Label
            {
                Text = "Готово",
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = Color.LightGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            this.Controls.Add(tabControl);
            this.Controls.Add(lblStatus);
        }

        private string GetRoleName(string role)
        {
            switch (role)
            {
                case "Admin": return "Администратор";
                case "Waiter": return "Официант";
                case "Storekeeper": return "Кладовщик";
                default: return role;
            }
        }

        private TabPage CreateDishesTab()
        {
            TabPage tab = new TabPage("Блюда");
            Panel panel = new Panel { Dock = DockStyle.Fill };

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

            Button btnSearch = new Button { Text = "Поиск", Location = new Point(540, 8), Size = new Size(100, 30) };
            Button btnRefresh = new Button { Text = "Обновить", Location = new Point(650, 8), Size = new Size(100, 30) };

            dgvDishes = new DataGridView { Location = new Point(10, 50), Size = new Size(1260, 300), AllowUserToAddRows = false };
            dgvDishes.ReadOnly = true;
            dgvDishes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            btnAddDish = new Button { Text = "Добавить блюдо", Location = new Point(10, 360), Size = new Size(120, 35), BackColor = Color.LightGreen };
            btnEditDish = new Button { Text = "Редактировать", Location = new Point(140, 360), Size = new Size(120, 35), BackColor = Color.LightYellow };
            btnDeleteDish = new Button { Text = "Удалить", Location = new Point(270, 360), Size = new Size(120, 35), BackColor = Color.LightCoral };

            btnSearch.Click += (s, e) => SearchDishes();
            btnRefresh.Click += (s, e) => LoadDishes();
            btnAddDish.Click += (s, e) => AddDish();
            btnEditDish.Click += (s, e) => EditDish();
            btnDeleteDish.Click += (s, e) => DeleteDish();

            panel.Controls.Add(lblSearch);
            panel.Controls.Add(txtSearch);
            panel.Controls.Add(lblCategory);
            panel.Controls.Add(cmbCategoryFilter);
            panel.Controls.Add(btnSearch);
            panel.Controls.Add(btnRefresh);
            panel.Controls.Add(dgvDishes);
            panel.Controls.Add(btnAddDish);
            panel.Controls.Add(btnEditDish);
            panel.Controls.Add(btnDeleteDish);

            tab.Controls.Add(panel);
            return tab;
        }

        private TabPage CreateTablesTab()
        {
            TabPage tab = new TabPage("Столы");
            Panel panel = new Panel { Dock = DockStyle.Fill };

            Label lblStatusFilter = new Label { Text = "Статус:", Location = new Point(10, 10), Size = new Size(50, 25) };
            cmbTableStatusFilter = new ComboBox { Location = new Point(70, 10), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTableStatusFilter.Items.Add("Все");
            cmbTableStatusFilter.Items.Add("Свободен");
            cmbTableStatusFilter.Items.Add("Занят");
            cmbTableStatusFilter.Items.Add("Требует уборки");
            cmbTableStatusFilter.Items.Add("Забронирован");
            cmbTableStatusFilter.SelectedIndex = 0;

            Label lblSeatsFilter = new Label { Text = "Кол-во мест:", Location = new Point(240, 10), Size = new Size(80, 25) };
            cmbSeatsFilter = new ComboBox { Location = new Point(330, 10), Size = new Size(100, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSeatsFilter.Items.Add("Все");
            cmbSeatsFilter.Items.Add("2");
            cmbSeatsFilter.Items.Add("4");
            cmbSeatsFilter.Items.Add("6");
            cmbSeatsFilter.Items.Add("8");
            cmbSeatsFilter.SelectedIndex = 0;

            Button btnFilterTables = new Button { Text = "Фильтр", Location = new Point(450, 8), Size = new Size(100, 30) };
            Button btnRefreshTables = new Button { Text = "Обновить", Location = new Point(560, 8), Size = new Size(100, 30) };

            dgvTables = new DataGridView { Location = new Point(10, 50), Size = new Size(1260, 300), AllowUserToAddRows = false };
            dgvTables.ReadOnly = true;
            dgvTables.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            btnAddTable = new Button { Text = "Добавить стол", Location = new Point(10, 360), Size = new Size(120, 35), BackColor = Color.LightGreen };
            btnEditTable = new Button { Text = "Редактировать", Location = new Point(140, 360), Size = new Size(120, 35), BackColor = Color.LightYellow };
            btnDeleteTable = new Button { Text = "Удалить", Location = new Point(270, 360), Size = new Size(120, 35), BackColor = Color.LightCoral };
            btnChangeTableStatus = new Button { Text = "Сменить статус", Location = new Point(400, 360), Size = new Size(120, 35), BackColor = Color.LightBlue };

            btnFilterTables.Click += (s, e) => FilterTables();
            btnRefreshTables.Click += (s, e) => LoadTables();
            btnAddTable.Click += (s, e) => AddTable();
            btnEditTable.Click += (s, e) => EditTable();
            btnDeleteTable.Click += (s, e) => DeleteTable();
            btnChangeTableStatus.Click += (s, e) => ChangeTableStatus();

            panel.Controls.Add(lblStatusFilter);
            panel.Controls.Add(cmbTableStatusFilter);
            panel.Controls.Add(lblSeatsFilter);
            panel.Controls.Add(cmbSeatsFilter);
            panel.Controls.Add(btnFilterTables);
            panel.Controls.Add(btnRefreshTables);
            panel.Controls.Add(dgvTables);
            panel.Controls.Add(btnAddTable);
            panel.Controls.Add(btnEditTable);
            panel.Controls.Add(btnDeleteTable);
            panel.Controls.Add(btnChangeTableStatus);

            tab.Controls.Add(panel);
            return tab;
        }

        private TabPage CreateOrdersTab()
        {
            TabPage tab = new TabPage("Заказы");
            Panel panel = new Panel { Dock = DockStyle.Fill };

            GroupBox groupSelectTable = new GroupBox { Text = "Выбор стола", Location = new Point(10, 10), Size = new Size(500, 80) };
            Label lblSelectTable = new Label { Text = "Стол:", Location = new Point(10, 30), Size = new Size(50, 25) };
            cmbSelectTable = new ComboBox { Location = new Point(70, 30), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            btnCreateOrder = new Button { Text = "Новый заказ", Location = new Point(290, 28), Size = new Size(180, 30), BackColor = Color.LightGreen };
            groupSelectTable.Controls.Add(lblSelectTable);
            groupSelectTable.Controls.Add(cmbSelectTable);
            groupSelectTable.Controls.Add(btnCreateOrder);

            GroupBox groupAddItems = new GroupBox { Text = "Добавление блюд", Location = new Point(10, 100), Size = new Size(700, 130) };
            Label lblSelectDish = new Label { Text = "Блюдо:", Location = new Point(10, 30), Size = new Size(50, 25) };
            cmbSelectDish = new ComboBox { Location = new Point(70, 30), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            Label lblQuantity = new Label { Text = "Кол-во:", Location = new Point(340, 30), Size = new Size(50, 25) };
            nudQuantity = new NumericUpDown { Location = new Point(400, 30), Size = new Size(70, 25), Minimum = 1, Maximum = 99, Value = 1 };
            Label lblComment = new Label { Text = "Комментарий:", Location = new Point(10, 70), Size = new Size(80, 25) };
            txtComment = new TextBox { Location = new Point(100, 70), Size = new Size(300, 25) };
            btnAddToOrder = new Button { Text = "Добавить в заказ", Location = new Point(430, 65), Size = new Size(150, 35), BackColor = Color.LightBlue };
            groupAddItems.Controls.Add(lblSelectDish);
            groupAddItems.Controls.Add(cmbSelectDish);
            groupAddItems.Controls.Add(lblQuantity);
            groupAddItems.Controls.Add(nudQuantity);
            groupAddItems.Controls.Add(lblComment);
            groupAddItems.Controls.Add(txtComment);
            groupAddItems.Controls.Add(btnAddToOrder);

            GroupBox groupCurrentOrder = new GroupBox { Text = "Текущий заказ", Location = new Point(10, 240), Size = new Size(1260, 220) };
            dgvOrderItems = new DataGridView { Location = new Point(10, 25), Size = new Size(1240, 140), AllowUserToAddRows = false, ReadOnly = true };
            lblCurrentOrderTotal = new Label { Text = "Итого: 0 руб.", Location = new Point(10, 175), Size = new Size(200, 25), Font = new Font("Arial", 10, FontStyle.Bold) };
            btnCloseOrder = new Button { Text = "Закрыть заказ (оплата)", Location = new Point(1050, 170), Size = new Size(200, 35), BackColor = Color.Gold };
            groupCurrentOrder.Controls.Add(dgvOrderItems);
            groupCurrentOrder.Controls.Add(lblCurrentOrderTotal);
            groupCurrentOrder.Controls.Add(btnCloseOrder);

            GroupBox groupHistory = new GroupBox { Text = "История заказов", Location = new Point(10, 470), Size = new Size(1260, 200) };
            dgvOrders = new DataGridView { Location = new Point(10, 25), Size = new Size(1240, 160), AllowUserToAddRows = false, ReadOnly = true };
            groupHistory.Controls.Add(dgvOrders);

            btnCreateOrder.Click += (s, e) => CreateNewOrder();
            btnAddToOrder.Click += (s, e) => AddToOrder();
            btnCloseOrder.Click += (s, e) => CloseOrder();

            panel.Controls.Add(groupSelectTable);
            panel.Controls.Add(groupAddItems);
            panel.Controls.Add(groupCurrentOrder);
            panel.Controls.Add(groupHistory);

            tab.Controls.Add(panel);
            return tab;
        }

        private TabPage CreateReportsTab()
        {
            TabPage tab = new TabPage("Отчёты");
            Panel panel = new Panel { Dock = DockStyle.Fill };

            Button btnPopularity = new Button { Text = "Популярность блюд", Location = new Point(20, 20), Size = new Size(200, 40) };
            Button btnRevenue = new Button { Text = "Выручка по дням", Location = new Point(240, 20), Size = new Size(200, 40) };
            Button btnTableLoad = new Button { Text = "Загрузка столов", Location = new Point(460, 20), Size = new Size(200, 40) };

            DataGridView dgvReport = new DataGridView { Location = new Point(20, 80), Size = new Size(1240, 550), AllowUserToAddRows = false, ReadOnly = true };

            btnPopularity.Click += (s, e) => ShowPopularityReport(dgvReport);
            btnRevenue.Click += (s, e) => ShowRevenueReport(dgvReport);
            btnTableLoad.Click += (s, e) => ShowTableLoadReport(dgvReport);

            panel.Controls.Add(btnPopularity);
            panel.Controls.Add(btnRevenue);
            panel.Controls.Add(btnTableLoad);
            panel.Controls.Add(dgvReport);

            tab.Controls.Add(panel);
            return tab;
        }

        private TabPage CreateUsersTab()
        {
            TabPage tab = new TabPage("Пользователи");
            Panel panel = new Panel { Dock = DockStyle.Fill };

            DataGridView dgvUsers = new DataGridView { Location = new Point(10, 10), Size = new Size(1260, 500), AllowUserToAddRows = false, ReadOnly = true };

            Button btnRefreshUsers = new Button { Text = "Обновить", Location = new Point(10, 520), Size = new Size(100, 35) };
            Button btnDeleteUser = new Button { Text = "Удалить пользователя", Location = new Point(120, 520), Size = new Size(150, 35), BackColor = Color.LightCoral };

            LoadUsers(dgvUsers);
            btnRefreshUsers.Click += (s, e) => LoadUsers(dgvUsers);
            btnDeleteUser.Click += (s, e) => DeleteUser(dgvUsers);

            panel.Controls.Add(dgvUsers);
            panel.Controls.Add(btnRefreshUsers);
            panel.Controls.Add(btnDeleteUser);

            tab.Controls.Add(panel);
            return tab;
        }

        private void ApplyRolePermissions()
        {
            if (currentRole == "Admin")
            {
                btnAddDish.Visible = true;
                btnEditDish.Visible = true;
                btnDeleteDish.Visible = true;
                btnAddTable.Visible = true;
                btnEditTable.Visible = true;
                btnDeleteTable.Visible = true;
                btnChangeTableStatus.Visible = true;
                btnCreateOrder.Visible = true;
                btnAddToOrder.Visible = true;
                btnCloseOrder.Visible = true;
            }
            else if (currentRole == "Waiter")
            {
                btnAddDish.Visible = false;
                btnEditDish.Visible = false;
                btnDeleteDish.Visible = false;
                btnAddTable.Visible = false;
                btnEditTable.Visible = false;
                btnDeleteTable.Visible = false;
                btnChangeTableStatus.Visible = true;
                btnCreateOrder.Visible = true;
                btnAddToOrder.Visible = true;
                btnCloseOrder.Visible = true;
            }
            else
            {
                btnAddDish.Visible = false;
                btnEditDish.Visible = false;
                btnDeleteDish.Visible = false;
                btnAddTable.Visible = false;
                btnEditTable.Visible = false;
                btnDeleteTable.Visible = false;
                btnChangeTableStatus.Visible = false;
                btnCreateOrder.Visible = false;
                btnAddToOrder.Visible = false;
                btnCloseOrder.Visible = false;
            }
        }

        private void LoadData()
        {
            LoadDishes();
            LoadTables();
            LoadOrdersHistory();
            LoadTablesForCombo();
            LoadDishesForCombo();
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

        private void LoadOrdersHistory()
        {
            string query = @"
                SELECT o.Id AS Номер_заказа, t.TableNumber AS Стол, o.OrderDate AS Дата, o.TotalAmount AS Сумма
                FROM Orders o
                JOIN RestaurantTables t ON o.TableId = t.Id
                ORDER BY o.Id DESC";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvOrders.DataSource = dt;
            dgvOrders.AutoResizeColumns();
        }

        private void LoadTablesForCombo()
        {
            cmbSelectTable.Items.Clear();
            string query = "SELECT Id, TableNumber FROM RestaurantTables WHERE Status != 'Занят'";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            foreach (DataRow row in dt.Rows)
            {
                int id = Convert.ToInt32(row["Id"]);
                cmbSelectTable.Items.Add(new { Id = id, Text = $"Стол {row["TableNumber"]}" });
            }
            cmbSelectTable.DisplayMember = "Text";
            cmbSelectTable.ValueMember = "Id";
            if (cmbSelectTable.Items.Count > 0)
                cmbSelectTable.SelectedIndex = 0;
        }

        private void LoadDishesForCombo()
        {
            cmbSelectDish.Items.Clear();
            string query = "SELECT Id, Name, Price FROM Dishes";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            foreach (DataRow row in dt.Rows)
            {
                int id = Convert.ToInt32(row["Id"]);
                cmbSelectDish.Items.Add(new { Id = id, Text = $"{row["Name"]} - {row["Price"]} руб." });
            }
            cmbSelectDish.DisplayMember = "Text";
            cmbSelectDish.ValueMember = "Id";
            if (cmbSelectDish.Items.Count > 0)
                cmbSelectDish.SelectedIndex = 0;
        }

        private void LoadOrderItems()
        {
            if (currentOrderId == -1) return;
            string query = @"
                SELECT oi.Id, d.Name AS Блюдо, oi.Quantity AS Кол_во, oi.PriceAtOrder AS Цена, (oi.Quantity * oi.PriceAtOrder) AS Сумма, oi.Comment AS Комментарий
                FROM OrderItems oi
                JOIN Dishes d ON oi.DishId = d.Id
                WHERE oi.OrderId = @orderId";
            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@orderId", currentOrderId);
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                dgvOrderItems.DataSource = dt;
                dgvOrderItems.AutoResizeColumns();

                decimal total = 0;
                foreach (DataRow row in dt.Rows)
                    total += Convert.ToDecimal(row["Сумма"]);
                lblCurrentOrderTotal.Text = $"Итого: {total} руб.";
            }
        }

        private void SearchDishes()
        {
            string searchText = txtSearch.Text.Trim();
            string category = cmbCategoryFilter.SelectedItem.ToString();
            string query = "SELECT Id, Name AS Название, Category AS Категория, Price AS Цена, CookingTime AS Время_приготовления FROM Dishes WHERE 1=1";
            if (!string.IsNullOrEmpty(searchText))
                query += $" AND (Name LIKE '%{searchText}%' OR Category LIKE '%{searchText}%')";
            if (category != "Все")
                query += $" AND Category = '{category}'";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvDishes.DataSource = dt;
            lblStatus.Text = $"Найдено блюд: {dt.Rows.Count}";
        }

        private void FilterTables()
        {
            string status = cmbTableStatusFilter.SelectedItem.ToString();
            string seats = cmbSeatsFilter.SelectedItem.ToString();

            string query = "SELECT Id, TableNumber AS Номер_стола, SeatsCount AS Мест, Status AS Статус FROM RestaurantTables WHERE 1=1";

            if (status != "Все")
            {
                query += $" AND Status = '{status}'";
            }

            if (seats != "Все")
            {
                query += $" AND SeatsCount = {seats}";
            }

            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvTables.DataSource = dt;
            lblStatus.Text = $"Найдено столов: {dt.Rows.Count}";
        }

        private void AddDish()
        {
            Form dialog = new Form { Text = "Добавление блюда", Size = new Size(400, 300), StartPosition = FormStartPosition.CenterParent };
            TextBox txtName = new TextBox { Location = new Point(120, 20), Size = new Size(200, 25) };
            ComboBox cmbCat = new ComboBox { Location = new Point(120, 60), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCat.Items.AddRange(new[] { "Салат", "Горячее", "Десерт", "Напиток" });
            NumericUpDown nudPrice = new NumericUpDown { Location = new Point(120, 100), Size = new Size(200, 25), Minimum = 0, Maximum = 10000, DecimalPlaces = 2 };
            NumericUpDown nudTime = new NumericUpDown { Location = new Point(120, 140), Size = new Size(200, 25), Minimum = 1, Maximum = 120 };
            Button btnSave = new Button { Text = "Сохранить", Location = new Point(120, 190), Size = new Size(100, 35), BackColor = Color.LightGreen };

            dialog.Controls.Add(new Label { Text = "Название:", Location = new Point(20, 25), Size = new Size(80, 20) });
            dialog.Controls.Add(txtName);
            dialog.Controls.Add(new Label { Text = "Категория:", Location = new Point(20, 65), Size = new Size(80, 20) });
            dialog.Controls.Add(cmbCat);
            dialog.Controls.Add(new Label { Text = "Цена:", Location = new Point(20, 105), Size = new Size(80, 20) });
            dialog.Controls.Add(nudPrice);
            dialog.Controls.Add(new Label { Text = "Время (мин):", Location = new Point(20, 145), Size = new Size(80, 20) });
            dialog.Controls.Add(nudTime);
            dialog.Controls.Add(btnSave);

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Введите название"); return; }
                string insert = $"INSERT INTO Dishes (Name, Category, Price, CookingTime) VALUES ('{txtName.Text}', '{cmbCat.SelectedItem}', {nudPrice.Value}, {nudTime.Value})";
                using (SQLiteCommand cmd = new SQLiteCommand(insert, connection))
                    cmd.ExecuteNonQuery();
                LoadDishes();
                LoadDishesForCombo();
                dialog.Close();
            };
            dialog.ShowDialog();
        }

        private void EditDish()
        {
            if (dgvDishes.SelectedRows.Count == 0) { MessageBox.Show("Выберите блюдо"); return; }
            int id = Convert.ToInt32(dgvDishes.SelectedRows[0].Cells["Id"].Value);
            string oldName = dgvDishes.SelectedRows[0].Cells["Название"].Value.ToString();
            string oldCat = dgvDishes.SelectedRows[0].Cells["Категория"].Value.ToString();
            decimal oldPrice = Convert.ToDecimal(dgvDishes.SelectedRows[0].Cells["Цена"].Value);

            Form dialog = new Form { Text = "Редактирование блюда", Size = new Size(400, 300), StartPosition = FormStartPosition.CenterParent };
            TextBox txtName = new TextBox { Text = oldName, Location = new Point(120, 20), Size = new Size(200, 25) };
            ComboBox cmbCat = new ComboBox { Location = new Point(120, 60), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCat.Items.AddRange(new[] { "Салат", "Горячее", "Десерт", "Напиток" });
            cmbCat.SelectedItem = oldCat;
            NumericUpDown nudPrice = new NumericUpDown { Location = new Point(120, 100), Size = new Size(200, 25), Minimum = 0, Maximum = 10000, DecimalPlaces = 2, Value = oldPrice };
            NumericUpDown nudTime = new NumericUpDown { Location = new Point(120, 140), Size = new Size(200, 25), Minimum = 1, Maximum = 120 };
            Button btnSave = new Button { Text = "Сохранить", Location = new Point(120, 190), Size = new Size(100, 35), BackColor = Color.LightGreen };

            dialog.Controls.Add(new Label { Text = "Название:", Location = new Point(20, 25), Size = new Size(80, 20) });
            dialog.Controls.Add(txtName);
            dialog.Controls.Add(new Label { Text = "Категория:", Location = new Point(20, 65), Size = new Size(80, 20) });
            dialog.Controls.Add(cmbCat);
            dialog.Controls.Add(new Label { Text = "Цена:", Location = new Point(20, 105), Size = new Size(80, 20) });
            dialog.Controls.Add(nudPrice);
            dialog.Controls.Add(new Label { Text = "Время (мин):", Location = new Point(20, 145), Size = new Size(80, 20) });
            dialog.Controls.Add(nudTime);
            dialog.Controls.Add(btnSave);

            btnSave.Click += (s, e) =>
            {
                string update = $"UPDATE Dishes SET Name='{txtName.Text}', Category='{cmbCat.SelectedItem}', Price={nudPrice.Value}, CookingTime={nudTime.Value} WHERE Id={id}";
                using (SQLiteCommand cmd = new SQLiteCommand(update, connection))
                    cmd.ExecuteNonQuery();
                LoadDishes();
                LoadDishesForCombo();
                dialog.Close();
            };
            dialog.ShowDialog();
        }

        private void DeleteDish()
        {
            if (dgvDishes.SelectedRows.Count == 0) { MessageBox.Show("Выберите блюдо"); return; }
            int id = Convert.ToInt32(dgvDishes.SelectedRows[0].Cells["Id"].Value);
            if (MessageBox.Show("Удалить блюдо?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                using (SQLiteCommand cmd = new SQLiteCommand($"DELETE FROM Dishes WHERE Id={id}", connection))
                    cmd.ExecuteNonQuery();
                LoadDishes();
                LoadDishesForCombo();
            }
        }

        private void AddTable()
        {
            Form dialog = new Form { Text = "Добавление стола", Size = new Size(350, 200), StartPosition = FormStartPosition.CenterParent };
            NumericUpDown nudNumber = new NumericUpDown { Location = new Point(120, 20), Size = new Size(150, 25), Minimum = 1, Maximum = 100 };
            NumericUpDown nudSeats = new NumericUpDown { Location = new Point(120, 60), Size = new Size(150, 25), Minimum = 1, Maximum = 20 };
            Button btnSave = new Button { Text = "Сохранить", Location = new Point(100, 110), Size = new Size(100, 35), BackColor = Color.LightGreen };

            dialog.Controls.Add(new Label { Text = "Номер стола:", Location = new Point(20, 25), Size = new Size(80, 20) });
            dialog.Controls.Add(nudNumber);
            dialog.Controls.Add(new Label { Text = "Кол-во мест:", Location = new Point(20, 65), Size = new Size(80, 20) });
            dialog.Controls.Add(nudSeats);
            dialog.Controls.Add(btnSave);

            btnSave.Click += (s, e) =>
            {
                string insert = $"INSERT INTO RestaurantTables (TableNumber, SeatsCount, Status) VALUES ({nudNumber.Value}, {nudSeats.Value}, 'Свободен')";
                using (SQLiteCommand cmd = new SQLiteCommand(insert, connection))
                    cmd.ExecuteNonQuery();
                LoadTables();
                LoadTablesForCombo();
                dialog.Close();
            };
            dialog.ShowDialog();
        }

        private void EditTable()
        {
            if (dgvTables.SelectedRows.Count == 0) { MessageBox.Show("Выберите стол"); return; }
            int id = Convert.ToInt32(dgvTables.SelectedRows[0].Cells["Id"].Value);
            int oldNumber = Convert.ToInt32(dgvTables.SelectedRows[0].Cells["Номер_стола"].Value);
            int oldSeats = Convert.ToInt32(dgvTables.SelectedRows[0].Cells["Мест"].Value);

            Form dialog = new Form { Text = "Редактирование стола", Size = new Size(350, 200), StartPosition = FormStartPosition.CenterParent };
            NumericUpDown nudNumber = new NumericUpDown { Location = new Point(120, 20), Size = new Size(150, 25), Minimum = 1, Maximum = 100, Value = oldNumber };
            NumericUpDown nudSeats = new NumericUpDown { Location = new Point(120, 60), Size = new Size(150, 25), Minimum = 1, Maximum = 20, Value = oldSeats };
            Button btnSave = new Button { Text = "Сохранить", Location = new Point(100, 110), Size = new Size(100, 35), BackColor = Color.LightGreen };

            dialog.Controls.Add(new Label { Text = "Номер стола:", Location = new Point(20, 25), Size = new Size(80, 20) });
            dialog.Controls.Add(nudNumber);
            dialog.Controls.Add(new Label { Text = "Кол-во мест:", Location = new Point(20, 65), Size = new Size(80, 20) });
            dialog.Controls.Add(nudSeats);
            dialog.Controls.Add(btnSave);

            btnSave.Click += (s, e) =>
            {
                string update = $"UPDATE RestaurantTables SET TableNumber={nudNumber.Value}, SeatsCount={nudSeats.Value} WHERE Id={id}";
                using (SQLiteCommand cmd = new SQLiteCommand(update, connection))
                    cmd.ExecuteNonQuery();
                LoadTables();
                LoadTablesForCombo();
                dialog.Close();
            };
            dialog.ShowDialog();
        }

        private void DeleteTable()
        {
            if (dgvTables.SelectedRows.Count == 0) { MessageBox.Show("Выберите стол"); return; }
            int id = Convert.ToInt32(dgvTables.SelectedRows[0].Cells["Id"].Value);
            if (MessageBox.Show("Удалить стол?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                using (SQLiteCommand cmd = new SQLiteCommand($"DELETE FROM RestaurantTables WHERE Id={id}", connection))
                    cmd.ExecuteNonQuery();
                LoadTables();
                LoadTablesForCombo();
            }
        }

        private void ChangeTableStatus()
        {
            if (dgvTables.SelectedRows.Count == 0) { MessageBox.Show("Выберите стол"); return; }
            int id = Convert.ToInt32(dgvTables.SelectedRows[0].Cells["Id"].Value);
            string currentStatus = dgvTables.SelectedRows[0].Cells["Статус"].Value.ToString();

            Form dialog = new Form { Text = "Смена статуса стола", Size = new Size(300, 150), StartPosition = FormStartPosition.CenterParent };
            ComboBox cmbStatus = new ComboBox { Location = new Point(20, 30), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new[] { "Свободен", "Занят", "Требует уборки", "Забронирован" });
            cmbStatus.SelectedItem = currentStatus;
            Button btnSave = new Button { Text = "Изменить", Location = new Point(20, 70), Size = new Size(100, 35), BackColor = Color.LightBlue };

            dialog.Controls.Add(cmbStatus);
            dialog.Controls.Add(btnSave);

            btnSave.Click += (s, e) =>
            {
                string update = $"UPDATE RestaurantTables SET Status='{cmbStatus.SelectedItem}' WHERE Id={id}";
                using (SQLiteCommand cmd = new SQLiteCommand(update, connection))
                    cmd.ExecuteNonQuery();
                LoadTables();
                LoadTablesForCombo();
                dialog.Close();
            };
            dialog.ShowDialog();
        }

        private void CreateNewOrder()
        {
            if (cmbSelectTable.SelectedItem == null) { MessageBox.Show("Выберите стол"); return; }
            dynamic selected = cmbSelectTable.SelectedItem;
            int tableId = Convert.ToInt32(selected.Id);

            string insert = $"INSERT INTO Orders (TableId, WaiterId, OrderDate, TotalAmount, Status) VALUES ({tableId}, {currentUserId}, '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', 0, 'Open')";
            using (SQLiteCommand cmd = new SQLiteCommand(insert, connection))
                cmd.ExecuteNonQuery();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT last_insert_rowid()", connection))
            {
                long lastId = (long)cmd.ExecuteScalar();
                currentOrderId = (int)lastId;
            }

            using (SQLiteCommand cmd = new SQLiteCommand($"UPDATE RestaurantTables SET Status='Занят' WHERE Id={tableId}", connection))
                cmd.ExecuteNonQuery();

            LoadTables();
            LoadTablesForCombo();
            LoadOrdersHistory();
            LoadOrderItems();
            MessageBox.Show($"Заказ #{currentOrderId} создан для стола {selected.Text}");
        }

        private void AddToOrder()
        {
            if (currentOrderId == -1) { MessageBox.Show("Сначала создайте новый заказ"); return; }
            if (cmbSelectDish.SelectedItem == null) { MessageBox.Show("Выберите блюдо"); return; }

            dynamic selectedDish = cmbSelectDish.SelectedItem;
            int dishId = Convert.ToInt32(selectedDish.Id);
            int quantity = (int)nudQuantity.Value;
            string comment = txtComment.Text.Trim();

            decimal price = 0;
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT Price FROM Dishes WHERE Id={dishId}", connection))
                price = Convert.ToDecimal(cmd.ExecuteScalar());

            string insert = $"INSERT INTO OrderItems (OrderId, DishId, Quantity, PriceAtOrder, Comment) VALUES ({currentOrderId}, {dishId}, {quantity}, {price}, '{comment}')";
            using (SQLiteCommand cmd = new SQLiteCommand(insert, connection))
                cmd.ExecuteNonQuery();

            string updateTotal = $"UPDATE Orders SET TotalAmount = (SELECT SUM(Quantity * PriceAtOrder) FROM OrderItems WHERE OrderId={currentOrderId}) WHERE Id={currentOrderId}";
            using (SQLiteCommand cmd = new SQLiteCommand(updateTotal, connection))
                cmd.ExecuteNonQuery();

            LoadOrderItems();
            nudQuantity.Value = 1;
            txtComment.Text = "";
        }

        private void CloseOrder()
        {
            if (currentOrderId == -1) { MessageBox.Show("Нет открытого заказа"); return; }
            if (MessageBox.Show("Закрыть заказ и освободить стол?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            int tableId = 0;
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT TableId FROM Orders WHERE Id={currentOrderId}", connection))
                tableId = Convert.ToInt32(cmd.ExecuteScalar());

            using (SQLiteCommand cmd = new SQLiteCommand($"UPDATE Orders SET Status='Closed' WHERE Id={currentOrderId}", connection))
                cmd.ExecuteNonQuery();

            using (SQLiteCommand cmd = new SQLiteCommand($"UPDATE RestaurantTables SET Status='Требует уборки' WHERE Id={tableId}", connection))
                cmd.ExecuteNonQuery();

            currentOrderId = -1;
            dgvOrderItems.DataSource = null;
            lblCurrentOrderTotal.Text = "Итого: 0 руб.";
            LoadTables();
            LoadTablesForCombo();
            LoadOrdersHistory();
            MessageBox.Show("Заказ закрыт, стол требует уборки");
        }

        private void ShowPopularityReport(DataGridView dgvReport)
        {
            string query = @"
                SELECT d.Name AS Блюдо, SUM(oi.Quantity) AS Количество_продаж, SUM(oi.Quantity * oi.PriceAtOrder) AS Выручка
                FROM OrderItems oi JOIN Dishes d ON oi.DishId = d.Id
                GROUP BY d.Id, d.Name ORDER BY Количество_продаж DESC";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvReport.DataSource = dt;
        }

        private void ShowRevenueReport(DataGridView dgvReport)
        {
            string query = @"
                SELECT DATE(OrderDate) AS Дата, COUNT(Id) AS Количество_заказов, SUM(TotalAmount) AS Выручка
                FROM Orders WHERE Status='Closed' GROUP BY DATE(OrderDate) ORDER BY Дата DESC";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvReport.DataSource = dt;
        }

        private void ShowTableLoadReport(DataGridView dgvReport)
        {
            string query = @"
                SELECT t.TableNumber AS Номер_стола, t.SeatsCount AS Мест, t.Status AS Статус, COUNT(o.Id) AS Количество_заказов
                FROM RestaurantTables t LEFT JOIN Orders o ON t.Id = o.TableId
                GROUP BY t.Id, t.TableNumber, t.SeatsCount, t.Status ORDER BY Количество_заказов DESC";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvReport.DataSource = dt;
        }

        private void LoadUsers(DataGridView dgvUsers)
        {
            string query = "SELECT Id, Login, Role, FullName FROM Users";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            dgvUsers.DataSource = dt;
            dgvUsers.AutoResizeColumns();
        }

        private void DeleteUser(DataGridView dgvUsers)
        {
            if (dgvUsers.SelectedRows.Count == 0) { MessageBox.Show("Выберите пользователя"); return; }
            int id = Convert.ToInt32(dgvUsers.SelectedRows[0].Cells["Id"].Value);
            string login = dgvUsers.SelectedRows[0].Cells["Login"].Value.ToString();
            if (login == "admin") { MessageBox.Show("Нельзя удалить администратора по умолчанию"); return; }
            if (MessageBox.Show($"Удалить пользователя {login}?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                using (SQLiteCommand cmd = new SQLiteCommand($"DELETE FROM Users WHERE Id={id}", connection))
                    cmd.ExecuteNonQuery();
                LoadUsers(dgvUsers);
            }
        }
    }
}