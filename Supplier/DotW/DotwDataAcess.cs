using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Common.DotW
{
    public class DotwDataAcess
    {
        private static string sqlconnectionstring;
        static DotwDataAcess()
        {
            sqlconnectionstring = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();

        }
        private static SqlConnection OpenConnection()
        {
            SqlConnection objsqlconnection;
            try
            {
                objsqlconnection = new SqlConnection();
                objsqlconnection = GetConnection();
                objsqlconnection.Open();
                return objsqlconnection;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        private static SqlConnection GetConnection()
        {
            try
            {
                return new SqlConnection(sqlconnectionstring);
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        private static void CloseConnection(SqlConnection con)
        {
            try
            {
                if ((con != null) && (con.State & ConnectionState.Open) == ConnectionState.Open)
                {
                    con.Close();
                    con.Dispose();
                }
            }
            catch
            {
                con = null;
            }
        }

        public static DataTable Get(string pQueryCode, params SqlParameter[] pParameterValues)
        {
            try
            {
                DataTable vDataTable = new DataTable();
                using (SqlConnection vSqlConnection = DotwDataAcess.OpenConnection())
                {
                    using (SqlCommand vSqlCommand = new SqlCommand(pQueryCode, vSqlConnection))
                    {
                        vSqlCommand.CommandType = CommandType.StoredProcedure;
                        foreach (SqlParameter vSqlParameter in pParameterValues)
                        {
                            vSqlCommand.Parameters.Add(vSqlParameter);
                        }
                        using (SqlDataAdapter vSqlDataAdapter = new SqlDataAdapter(vSqlCommand))
                        {
                            vSqlDataAdapter.Fill(vDataTable);
                            return vDataTable;
                        }
                    }
                }
            }
            catch (Exception xe)
            {
                throw new Exception(xe.ToString());
            }

        }

        public static int Insert(string pQueryCode, params SqlParameter[] pParameterValues)
        {
            try
            {
                using (SqlConnection vSqlConnection = DotwDataAcess.OpenConnection())
                {
                    using (SqlCommand vSqlCommand = new SqlCommand(pQueryCode, vSqlConnection))
                    {
                        vSqlCommand.CommandType = CommandType.StoredProcedure;
                        foreach (SqlParameter vSqlParameter in pParameterValues)
                        {
                            vSqlCommand.Parameters.Add(vSqlParameter);
                        }
                        int row = vSqlCommand.ExecuteNonQuery();
                        CloseConnection(vSqlConnection);
                        return row;
                    }
                }
            }
            catch (Exception xe)
            {
                throw new Exception(xe.ToString());
            }
        }

    }
}