using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace RestaurantIS
{
    public partial class LoginForm : Form
    {
        private SQLiteConnection connection;
        private TextBox txtLogin;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnRegister;
        private Label lblError;

        public LoginForm()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        private void InitializeComponent()
        {
            this.Text = "ИС Ресторан - Вход";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblTitle = new Label
            {
                Text = "Вход",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(80, 20),
                Size = new Size(240, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblLogin = new Label { Text = "Логин:", Location = new Point(50, 80), Size = new Size(80, 25) };
            txtLogin = new TextBox { Location = new Point(140, 80), Size = new Size(180, 25) };

            Label lblPassword = new Label { Text = "Пароль:", Location = new Point(50, 120), Size = new Size(80, 25) };
            txtPassword = new TextBox { Location = new Point(140, 120), Size = new Size(180, 25), PasswordChar = '*' };

            btnLogin = new Button { Text = "Войти", Location = new Point(80, 170), Size = new Size(100, 35), BackColor = Color.LightGreen };
            btnRegister = new Button { Text = "Регистрация", Location = new Point(200, 170), Size = new Size(100, 35) };

            lblError = new Label { Text = "", Location = new Point(50, 220), Size = new Size(300, 30), ForeColor = Color.Red, TextAlign = ContentAlignment.MiddleCenter };

            btnLogin.Click += BtnLogin_Click;
            btnRegister.Click += BtnRegister_Click;

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblLogin);
            this.Controls.Add(txtLogin);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
            this.Controls.Add(btnRegister);
            this.Controls.Add(lblError);
        }

        private void InitializeDatabase()
        {
            string dbPath = "RestaurantDB.sqlite";
            connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            connection.Open();

            string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Login TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    FullName TEXT NOT NULL
                )";

            using (SQLiteCommand cmd = new SQLiteCommand(createUsersTable, connection))
                cmd.ExecuteNonQuery();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Users WHERE Role = 'Admin'", connection))
            {
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                {
                    string hash = GetHash("admin123");
                    string insertAdmin = $"INSERT INTO Users (Login, PasswordHash, Role, FullName) VALUES ('admin', '{hash}', 'Admin', 'Главный администратор')";
                    using (SQLiteCommand insertCmd = new SQLiteCommand(insertAdmin, connection))
                        insertCmd.ExecuteNonQuery();
                }
            }

            CreateOtherTables();
        }

        private void CreateOtherTables()
        {
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
        WaiterId INTEGER NOT NULL,
        OrderDate TEXT NOT NULL,
        TotalAmount REAL NOT NULL DEFAULT 0,
        Status TEXT NOT NULL DEFAULT 'Open',
        FOREIGN KEY (TableId) REFERENCES RestaurantTables(Id),
        FOREIGN KEY (WaiterId) REFERENCES Users(Id)
    )";

            string createOrderItemsTable = @"
                CREATE TABLE IF NOT EXISTS OrderItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId INTEGER NOT NULL,
                    DishId INTEGER NOT NULL,
                    Quantity INTEGER NOT NULL,
                    PriceAtOrder REAL NOT NULL,
                    Comment TEXT,
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
                "INSERT INTO RestaurantTables (TableNumber, SeatsCount, Status) VALUES (4, 4, 'Свободен')",
                "INSERT INTO RestaurantTables (TableNumber, SeatsCount, Status) VALUES (5, 6, 'Свободен')",
                "INSERT INTO RestaurantTables (TableNumber, SeatsCount, Status) VALUES (6, 6, 'Свободен')"
            };
            foreach (string table in tables)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(table, connection))
                    cmd.ExecuteNonQuery();
            }
        }

        private string GetHash(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Введите логин и пароль";
                return;
            }

            string hash = GetHash(password);
            string query = $"SELECT Id, Role, FullName FROM Users WHERE Login = '{login}' AND PasswordHash = '{hash}'";

            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int userId = reader.GetInt32(0);
                        string role = reader.GetString(1);
                        string fullName = reader.GetString(2);

                        MainForm mainForm = new MainForm(connection, userId, login, role, fullName);
                        mainForm.Show();
                        this.Hide();
                    }
                    else
                    {
                        lblError.Text = "Неверный логин или пароль";
                    }
                }
            }
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            RegisterForm registerForm = new RegisterForm(connection);
            registerForm.ShowDialog();
        }
    }
}