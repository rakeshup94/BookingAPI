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
    public class getcurrencymrkup
    {
        SqlConnection conn;
        SqlCommand cmd;
        SqlDataAdapter adap;        
        public List<DataTable> getcurrencyConversion(long usrID, string clientType)
        {
            List<DataTable> dt = new List<DataTable>();
            DataTable table1 = new DataTable();
            DataTable table2 = new DataTable();
            try
            {
                using (conn = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("USP_GetCurrencyXMLOut", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@usrID", usrID);
                        cmd.Parameters.AddWithValue("@ClientType", clientType);
                        using (adap = new SqlDataAdapter(cmd))
                        {
                            conn.Open();
                            DataSet dataSet = new DataSet();
                            adap.Fill(dataSet);
                            if (dataSet.Tables.Count > 0)
                                table1 = dataSet.Tables[0];
                            if (dataSet.Tables.Count > 1)
                                table2 = dataSet.Tables[1];
                            dt.Add(table1);
                            dt.Add(table2);
                            conn.Close();
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return dt;

            }
        }
        public DataTable getmarkupdetails(long usrID,string supID, string clientType)
        {
            DataTable dt = new DataTable("Markup");
            try
            {
                using (conn = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("USP_GeTXMLOUTMarkup", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@UsrID", usrID);
                        cmd.Parameters.AddWithValue("@SupplierID", supID);
                        cmd.Parameters.AddWithValue("@MarkupType", clientType);
                        cmd.Parameters.AddWithValue("@SrvID", 2);
                        using (adap = new SqlDataAdapter(cmd))
                        {
                            conn.Open();
                            adap.Fill(dt);
                            conn.Close();
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return dt;
            }
        }
    }
}