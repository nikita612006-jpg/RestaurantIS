using System;
using System.Data.SQLite;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace RestaurantIS
{
    public partial class RegisterForm : Form
    {
        private SQLiteConnection connection;
        private TextBox txtLogin;
        private TextBox txtPassword;
        private TextBox txtConfirmPassword;
        private TextBox txtFullName;
        private ComboBox cmbRole;
        private Button btnRegister;
        private Button btnCancel;
        private Label lblError;

        public RegisterForm(SQLiteConnection conn)
        {
            connection = conn;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Регистрация";
            this.Size = new Size(400, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblTitle = new Label
            {
                Text = "Регистрация",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(140, 20),
                Size = new Size(150, 30)
            };

            Label lblLogin = new Label { Text = "Логин:", Location = new Point(50, 60), Size = new Size(80, 25) };
            txtLogin = new TextBox { Location = new Point(150, 60), Size = new Size(180, 25) };

            Label lblPassword = new Label { Text = "Пароль:", Location = new Point(50, 100), Size = new Size(80, 25) };
            txtPassword = new TextBox { Location = new Point(150, 100), Size = new Size(180, 25), PasswordChar = '*' };

            Label lblConfirm = new Label { Text = "Подтверждение:", Location = new Point(50, 140), Size = new Size(100, 25) };
            txtConfirmPassword = new TextBox { Location = new Point(150, 140), Size = new Size(180, 25), PasswordChar = '*' };

            Label lblFullName = new Label { Text = "ФИО:", Location = new Point(50, 180), Size = new Size(80, 25) };
            txtFullName = new TextBox { Location = new Point(150, 180), Size = new Size(180, 25) };

            Label lblRole = new Label { Text = "Роль:", Location = new Point(50, 220), Size = new Size(80, 25) };
            cmbRole = new ComboBox { Location = new Point(150, 220), Size = new Size(180, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRole.Items.Add("Официант");
            cmbRole.Items.Add("Кладовщик");
            cmbRole.SelectedIndex = 0;

            btnRegister = new Button { Text = "Зарегистрировать", Location = new Point(60, 270), Size = new Size(120, 35), BackColor = Color.LightGreen };
            btnCancel = new Button { Text = "Отмена", Location = new Point(200, 270), Size = new Size(100, 35) };

            lblError = new Label { Text = "", Location = new Point(50, 315), Size = new Size(300, 30), ForeColor = Color.Red, TextAlign = ContentAlignment.MiddleCenter };

            btnRegister.Click += BtnRegister_Click;
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblLogin);
            this.Controls.Add(txtLogin);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(lblConfirm);
            this.Controls.Add(txtConfirmPassword);
            this.Controls.Add(lblFullName);
            this.Controls.Add(txtFullName);
            this.Controls.Add(lblRole);
            this.Controls.Add(cmbRole);
            this.Controls.Add(btnRegister);
            this.Controls.Add(btnCancel);
            this.Controls.Add(lblError);
        }

        private string GetHash(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Text;
            string confirm = txtConfirmPassword.Text;
            string fullName = txtFullName.Text.Trim();
            string role = cmbRole.SelectedItem.ToString();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fullName))
            {
                lblError.Text = "Заполните все поля";
                return;
            }

            if (password != confirm)
            {
                lblError.Text = "Пароли не совпадают";
                return;
            }

            string roleEng = role == "Официант" ? "Waiter" : "Storekeeper";

            string checkQuery = $"SELECT COUNT(*) FROM Users WHERE Login = '{login}'";
            using (SQLiteCommand cmd = new SQLiteCommand(checkQuery, connection))
            {
                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                {
                    lblError.Text = "Пользователь с таким логином уже существует";
                    return;
                }
            }

            string hash = GetHash(password);
            string insertQuery = $"INSERT INTO Users (Login, PasswordHash, Role, FullName) VALUES ('{login}', '{hash}', '{roleEng}', '{fullName}')";

            using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, connection))
            {
                cmd.ExecuteNonQuery();
                MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }
    }
}