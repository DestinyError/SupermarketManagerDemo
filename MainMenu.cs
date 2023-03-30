using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SupermarketManager
{
    public partial class MainMenu : Form
    {
        private System.Timers.Timer timer;
        private int h, m, s;

        public MainMenu()
        {
            InitializeComponent();

            toolStripUsername.Text = Global.loggedInUser;

            if (Global.userType == 0)
            {
                toolStripUserType.Text = "Administrator";
            }
            else if (Global.userType == 1)
            {
                toolStripUserType.Text = "Employee";
            }

            toolStripDate.Text = DateTime.Now.ToString("dd-MM-yyyy");

            if (Global.userType > 0)
            {
                buttonAdd.Enabled = false;
                buttonClear.Enabled = false;
                groupBoxNewItem.Enabled = false;
                groupBoxNewUnit.Enabled = false;
                radioButtonAddItem.Enabled = false;
                radioButtonAddUnit.Enabled = false;

                groupBoxAddNewUser.Enabled = false;
            }

            radioButtonAddItem.Checked = true;
            dataGridViewItemList.AutoGenerateColumns = false;

            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Tick;
            h = 0;
            m = 0;
            s = 0;
            timer.Start();
        }

        private void MainMenu_Load(object sender, EventArgs e)
        {
            LoadTableItems();
            LoadUnits();

            LoadTableAdmin();
            LoadTableEmployee();
        }

        private void Timer_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            s += 1;
            if (s == 60)
            {
                s = 0;
                m += 1;
            }
            if (m == 60)
            {
                m = 0;
                h += 1;
            }
            toolStripTimer.Text = string.Format("{0}:{1}:{2}", h.ToString().PadLeft(2, '0'), m.ToString().PadLeft(2, '0'), s.ToString().PadLeft(2, '0'));
        }

        private void LoadTableItems()
        {
            SqlDataAdapter sda = new SqlDataAdapter(
                "select items.id as [ID], items.name as [Item], items.price as [Price], items.unit as [Unit], in_stock.quantity as [Quantity] " +
                "from items, in_stock " +
                "where items.id = in_stock.item_id", Global.cn);
            DataTable dt = new DataTable();
            sda.Fill(dt);
            dataGridViewItemList.DataSource = dt;
        }

        private void LoadUnits()
        {
            SqlCommand cmd = new SqlCommand("SELECT * FROM units", Global.cn);
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                comboBoxUnit.Items.Add(reader.GetString(reader.GetOrdinal("name")));
            }
            reader.Close();
            comboBoxUnit.SelectedItem = null;
        }

        private void LogoutButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult confirm = MessageBox.Show("Are you sure you want to logout?", "Confirm logout", MessageBoxButtons.YesNo);
            if (confirm == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                Global.loggedInUser = "";
                Global.userType = -1;
                LoginMenu.Instance.Show();
                LoginMenu.Instance.ButtonClear_Click(sender, e);
            }
        }

        private void DataGridViewItemList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridViewItemList.Columns[e.ColumnIndex].Name == "SubtractButtons" ||
                dataGridViewItemList.Columns[e.ColumnIndex].Name == "AddButtons")
            {
                int itemIdColIndex = dataGridViewItemList.Columns["ID"].Index;
                int amountColIndex = dataGridViewItemList.Columns["AddSubAmount"].Index;
                //If value in Quantity column of the same row as clicked button is valid, then update table
                if (Int32.TryParse(dataGridViewItemList.Rows[e.RowIndex].Cells[amountColIndex].Value.ToString(), out int amount)
                    && amount > 0)
                {
                    string itemId = dataGridViewItemList.Rows[e.RowIndex].Cells[itemIdColIndex].Value.ToString();
                    //If subtract button is clicked, get negative amount
                    if (dataGridViewItemList.Columns[e.ColumnIndex].Name == "AddButtons")
                    {
                        UpdateQuantity(itemId, amount);
                    }
                    else
                    {
                        UpdateQuantity(itemId, -amount);
                    }
                    dataGridViewItemList.Rows[e.RowIndex].Cells[amountColIndex].Value = string.Empty;
                }
                //Otherwise, reset the value
                else
                {
                    dataGridViewItemList.Rows[e.RowIndex].Cells[amountColIndex].Value = string.Empty;
                }
            }
        }

        private void UpdateQuantity(string id, float value)
        {
            bool error = false;

            if (value == 0)
            {
                return;
            }
            try
            {
                SqlCommand cmd = new SqlCommand(
                    "SELECT quantity FROM in_stock " +
                    "WHERE item_id = '" + id + "';", Global.cn);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    double inStock = reader.GetDouble(reader.GetOrdinal("quantity"));
                    if (inStock + value < 0)
                    {
                        MessageBox.Show("The amount to subtract is larger than the currently-in-stock amount!");
                        error = true;
                    }
                }
                reader.Close();

                if (!error)
                {
                    cmd = new SqlCommand(
                    "UPDATE in_stock SET quantity = quantity + " + value + " " +
                    "WHERE item_id = '" + id + "';", Global.cn);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                error = true;
            }

            if (!error)
            {
                if (value > 0)
                {
                    MessageBox.Show("Successfully added " + value + " items of ID: <" + id + ">.");
                }
                else
                {
                    MessageBox.Show("Successfully subtracted " + (-value) + " items of ID: <" + id + ">.");
                }
                LoadTableItems();
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (radioButtonAddItem.Checked)
            {
                if (textBoxItemID.Text == string.Empty || textBoxItemName.Text == string.Empty
                    || textBoxPrice.Text == string.Empty || comboBoxUnit.SelectedItem == null)
                {
                    ItemsTogglePrompt(labelFillInPrompt);
                    return;
                }

                //Check if ID already exists
                try
                {
                    SqlCommand cmd = new SqlCommand(
                    "SELECT * FROM items " +
                    "WHERE id = '" + textBoxItemID.Text + "';", Global.cn);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        ItemsTogglePrompt(labelItemIDPrompt);
                        reader.Close();
                        return;
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                if (AddNewItem())
                {
                    ItemsTogglePrompt();
                    MessageBox.Show("New item added successfully!");
                    LoadTableItems();
                }
            }
            if (radioButtonAddUnit.Checked)
            {
                if (textBoxNewUnit.Text == string.Empty)
                {
                    ItemsTogglePrompt(labelFillInPrompt);
                    return;
                }

                //Check if Unit already exists
                try
                {
                    SqlCommand cmd = new SqlCommand(
                    "SELECT * FROM units " +
                    "WHERE name = '" + textBoxNewUnit.Text + "';", Global.cn);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        ItemsTogglePrompt(labelNewUnitPrompt);
                        reader.Close();
                        return;
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                if (AddNewUnit())
                {
                    ItemsTogglePrompt();
                    MessageBox.Show("New unit created successfully!");
                    LoadUnits();
                }
            }
        }

        private bool AddNewItem()
        {
            string id = textBoxItemID.Text;
            string name = textBoxItemName.Text;
            string price = textBoxPrice.Text;
            string unit = comboBoxUnit.SelectedItem.ToString();
            int affectedRows1 = 0;
            int affectedRows2 = 0;

            try
            {
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO items VALUES ('" + id + "', '" + name + "', " + price +
                    ", '" + unit + "');", Global.cn);
                affectedRows1 = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            try
            {
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO in_stock VALUES ('" + id + "', 0);", Global.cn);
                affectedRows2 = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (affectedRows1 > 0 && affectedRows2 > 0)
            {
                return true;
            }
            return false;
        }

        private bool AddNewUnit()
        {
            int affectedRows = 0;
            try
            {
                SqlCommand cmd = new SqlCommand(
                "INSERT INTO units VALUES ('" + textBoxNewUnit.Text + "')", Global.cn);
                affectedRows = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            if (affectedRows > 0)
            {
                return true;
            }
            return false;
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            textBoxItemID.Text = string.Empty;
            textBoxItemName.Text = string.Empty;
            textBoxPrice.Text = string.Empty;
            comboBoxUnit.SelectedItem = null;
            textBoxNewUnit.Text = string.Empty;
        }

        private void RadioButtonAddItem_Check(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb.Enabled && rb.Checked)
            {
                groupBoxNewItem.Enabled = true;
            }
            else
            {
                groupBoxNewItem.Enabled = false;
            }
        }

        private void RadioButtonAddUnit_Check(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                groupBoxNewUnit.Enabled = true;
            }
            else
            {
                groupBoxNewUnit.Enabled = false;
            }
        }

        private void ItemsTogglePrompt(Label label = null)
        {
            if (label == labelFillInPrompt)
            {
                labelFillInPrompt.Visible = true;
            }
            else
            {
                labelFillInPrompt.Visible = false;
            }

            if (label == labelItemIDPrompt)
            {
                labelItemIDPrompt.Visible = true;
            }
            else
            {
                labelItemIDPrompt.Visible = false;
            }

            if (label == labelNewUnitPrompt)
            {
                labelNewUnitPrompt.Visible = true;
            }
            else
            {
                labelNewUnitPrompt.Visible = false;
            }
        }


        //////////////Users


        private void LoadTableAdmin()
        {
            try
            {
                SqlDataAdapter sda = new SqlDataAdapter(
                    "select username, date_created " +
                    "from users " +
                    "where user_type = 0", Global.cn);
                DataTable dt = new DataTable();
                sda.Fill(dt);
                dataGridViewAdmin.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadTableEmployee()
        {
            try
            {
                SqlDataAdapter sda = new SqlDataAdapter(
                    "select username, password, date_created " +
                    "from users " +
                    "where user_type = 1", Global.cn);
                DataTable dt = new DataTable();
                sda.Fill(dt);
                dataGridViewEmployee.DataSource = dt;

                if (Global.userType > 0)
                {
                    dataGridViewEmployee.Columns["employee_password"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void AddUserButton_Click(object sender, EventArgs e)
        {
            string username = textBoxUsername.Text;
            string password = textBoxPassword.Text;
            int userType = comboBoxUserType.SelectedIndex;

            if (username == string.Empty || password == string.Empty || userType == -1)
            {
                UsersTogglePrompt(labelNewUserFillPrompt);
                return;
            }

            try
            {
                SqlCommand cmd = new SqlCommand(
                    "SELECT * FROM users WHERE username = '" + username + "';", Global.cn);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    UsersTogglePrompt(labelUsernamePrompt);
                    reader.Close();
                    return;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            AddNewUser();
        }

        private void AddNewUser()
        {
            string username = textBoxUsername.Text;
            string password = textBoxPassword.Text;
            int userType = comboBoxUserType.SelectedIndex;

            try
            {
                SqlCommand cmd = new SqlCommand(string.Format(
                    "INSERT INTO Users VALUES('{0}', '{1}', {2}, GETDATE());", username, password, userType), Global.cn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            UsersTogglePrompt();
            MessageBox.Show("New user added successfully!");
            LoadTableAdmin();
            LoadTableEmployee();
        }

        private void UsersTogglePrompt(Label label = null)
        {
            if (label == labelUsernamePrompt)
            {
                labelUsernamePrompt.Visible = true;
            }
            else
            {
                labelUsernamePrompt.Visible = false;
            }

            if (label == labelNewUserFillPrompt)
            {
                labelNewUserFillPrompt.Visible = true;
            }
            else
            {
                labelNewUserFillPrompt.Visible = false;
            }
        }

        private void ClearUserButton_Click(object sender, EventArgs e)
        {
            textBoxUsername.Text = string.Empty;
            textBoxPassword.Text = string.Empty;
            comboBoxUserType.SelectedItem = null;
            UsersTogglePrompt();
        }
    }
}
