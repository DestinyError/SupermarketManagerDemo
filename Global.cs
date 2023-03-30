using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupermarketManager
{
    public class Global
    {
        public static SqlConnection cn;
        public static string cnStr = @"Data Source=MSI-GF63-MINH;Initial Catalog=DB_Supermarket;Integrated Security=True";
        public static int userType = -1;
        public static string loggedInUser = "";
    }
}
