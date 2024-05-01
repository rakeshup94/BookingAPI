using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Mystifly;

namespace TravillioXMLOutService.Air.Models.Common
{
    public class check_tktorder
    {
        SqlConnection conn;
        SqlCommand command;
        string SessionValue = string.Empty;
        int Status = 0;
        int Result = 0;
        string customerID = string.Empty;
        string transactionid = string.Empty;
        public int check_tktordering(string transid)
        {
            DataTable dt = new DataTable();
            try
            {
                using (conn = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (command = new SqlCommand("usp_checktktorder", conn))
                    {
                        if (command.Connection.State == System.Data.ConnectionState.Closed)
                        {
                            command.Connection.Open();
                        }
                        Status = 0;
                        SqlDataAdapter adapter = new SqlDataAdapter();
                        command.Parameters.Clear();
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@transid", transid);
                        using (adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dt);
                            conn.Close();
                            if (dt.Rows.Count > 0)
                            {
                                return 1;
                            }
                            else
                            { return 0; }                        
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return 2;
            }
        }
    }
}