using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace MictcoWebService.Common
{
    public class ClientSqlServer
    {
        public static string server = "server.oditbiz.in,2438";
        public static string database = "MOBIL_USERS";
        public static string username = "mictco_oditbiz";
        public static string password = "Mictco@Meizon#Codignus@#MIS6600";
        // public static string server = "DESKTOP-GHPN416\\ANANTHU";
        // public static string database = "MOBIL_USERS";
        // public static string username = "sa";
        // public static string password = "wf"; 

        // test

        public string connetionString;
        public SqlConnection shop;
        public SqlDataAdapter da;


        public ClientSqlServer()
        {
            connetionString = SqlConnectionPool.Apply(@"Data Source=" + server + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + password + "");
            shop = new SqlConnection(connetionString);
        }
        public  bool OpenConnection()
        {
            try
            {
                if (shop.State == ConnectionState.Closed)
                {
                    shop.ConnectionString = connetionString;
                    shop.Open();
                }
                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

        }

        public  void dbExecute(string qry)
        {
            SqlCommand cmd1;
            try
            {
                if (OpenConnection())
                {
                    cmd1 = new SqlCommand(qry, shop);
                    cmd1.CommandTimeout = 60;
                    cmd1.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                
            }
        }

        public DataTable dbReaderFill(string qry)
        {
            if (OpenConnection())
            {
                DataTable _temp = new DataTable();
                da = new SqlDataAdapter(qry, shop);
                da.SelectCommand.CommandTimeout = 60;
                da.Fill(_temp);
                return _temp;
            }
            else
            {
                return null;
            }
        }
        public void close()
        {
            try
            {
                if (shop != null)
                    shop.Close();
            }
            catch (Exception ex)
            {

            }
        }


    }
}
