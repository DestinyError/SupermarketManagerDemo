using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SupermarketManager
{
    public partial class LoginMenu : Form
    {
        public static LoginMenu Instance;

        public LoginMenu()
        {
            InitializeComponent();
            Instance = this;

            try
            {
                Global.cn = new SqlConnection(Global.cnStr);
                Global.cn.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (Properties.Settings.Default.username != string.Empty)
            {
                textBoxUsername.Text = Properties.Settings.Default.username;
                checkBoxRememberMe.Checked = true;
                if (Properties.Settings.Default.password != string.Empty)
                {
                    textBoxPassword.Text = Properties.Settings.Default.password;
                }
            }
        }

        public void ButtonClear_Click(object sender, EventArgs e)
        {
            textBoxUsername.Text = string.Empty;
            textBoxPassword.Text = string.Empty;
        }

        private void ButtonLogin_Click(object sender, EventArgs e)
        {
            if (textBoxUsername.Text == string.Empty)
            {
                labelBlankUsername.Visible = true;
                return;
            }
            else
            {
                labelBlankUsername.Visible = false;
            }

            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = Global.cn;
                cmd.CommandText =
                    "SELECT * FROM dbo.[Users] WHERE username = '" + textBoxUsername.Text +
                    "' AND PASSWORD = '" + textBoxPassword.Text + "'";
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    labelIncorrectInfo.Visible = false;

                    if (checkBoxRememberMe.Checked)
                    {
                        Properties.Settings.Default.username = textBoxUsername.Text;
                        Properties.Settings.Default.password = textBoxPassword.Text;
                        Properties.Settings.Default.Save();
                    }
                    else
                    {
                        Properties.Settings.Default.username = string.Empty;
                        Properties.Settings.Default.password = string.Empty;
                        Properties.Settings.Default.Save();
                    }

                    reader.Read();
                    Global.loggedInUser = (string)reader["username"];
                    Global.userType = (int)reader["user_type"];
                    if (Global.userType == 0)
                    {
                        MessageBox.Show("Logged-in successfully as admin!");
                    }
                    else if (Global.userType == 1)
                    {
                        MessageBox.Show("Logged-in successfully!");
                    }
                    reader.Close();

                    MainMenu mainMenu = new MainMenu();
                    mainMenu.Show();
                    this.Hide();
                }
                else
                {
                    reader.Close();
                    textBoxPassword.Text = string.Empty;
                    labelIncorrectInfo.Visible = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoginMenu_EnterKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ButtonLogin_Click(sender, e);
            }
        }
    }
}
