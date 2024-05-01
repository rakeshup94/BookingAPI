using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Web;

namespace TravillioXMLOutService.Models
{
    public static class APILog
    {
        
        static SqlConnection con;
        private static void connecttion()
        {
            string constr = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(constr);
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }
        }
        public static void SaveAPILogs(APILogDetail apilog)
        {
            try
            {
                connecttion();
                SqlCommand com = new SqlCommand("SP_InsertAPILog", con);
                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@customerID", apilog.customerID);
                com.Parameters.AddWithValue("@TrackNumber", apilog.TrackNumber);
                com.Parameters.AddWithValue("@logTypeID", apilog.LogTypeID);
                com.Parameters.AddWithValue("@logType", apilog.LogType);
                com.Parameters.AddWithValue("@SupplierID", apilog.SupplierID);
                com.Parameters.AddWithValue("@logMsg", apilog.logMsg);
                com.Parameters.AddWithValue("@logrequestXML", apilog.logrequestXML);
                com.Parameters.AddWithValue("@logresponseXML", apilog.logresponseXML);
                com.Parameters.AddWithValue("@logStatus", apilog.logStatus);
                com.Parameters.AddWithValue("@StartTime", apilog.StartTime);
                com.Parameters.AddWithValue("@EndTime", apilog.EndTime);
                com.Parameters.Add("@retVal", SqlDbType.Int);
                com.Parameters["@retVal"].Direction = ParameterDirection.Output;
                //connecttion();
                if (com.Connection.State == ConnectionState.Closed)
                { com.Connection.Open(); }
                com.ExecuteNonQuery();
            }
            catch (FormatException ex)
            {
                //throw ex;
            }
            finally
            {
                //com.Dispose();
                //con.Close();
                con.Dispose();
            }
        }
        public static void SendExcepToDB(Exception ex)
        {
            try
            { 
            connecttion();
            SqlCommand com = new SqlCommand("SP_ExceptionLogging", con);
           
            com.CommandType = CommandType.StoredProcedure;
            com.Parameters.AddWithValue("@ExceptionMsg", ex.Message.ToString());
            com.Parameters.AddWithValue("@ExceptionType", ex.GetType().Name.ToString());
            com.Parameters.AddWithValue("@customerID", "");
            com.Parameters.AddWithValue("@ExceptionSource", ex.StackTrace.ToString());
            connecttion();
            com.ExecuteNonQuery();
            }
            catch (FormatException ex2)
            {
                //throw ex2;
            }
            finally
            {
                //com.Dispose();
                //con.Close();   
                con.Dispose();
            }
        }
        public static void SendCustomExcepToDB(CustomException ex)
        {
            try
            {
                connecttion();
                SqlCommand com = new SqlCommand("SP_CustomExceptionLogging", con);

                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@ExceptionMsg", ex.MsgName);
                com.Parameters.AddWithValue("@ExceptionType", ex.ExcType);
                com.Parameters.AddWithValue("@PageName", ex.PageName);
                com.Parameters.AddWithValue("@MethodName", ex.MethodName);
                com.Parameters.AddWithValue("@customerID", ex.CustomerID);
                com.Parameters.AddWithValue("@TransID", ex.TranID);
                com.Parameters.AddWithValue("@ExceptionSource", ex.ExcSource);
                if (com.Connection.State == ConnectionState.Closed)
                { com.Connection.Open(); }
                com.ExecuteNonQuery();
            }
            catch (FormatException ex2)
            {
                //throw ex2;
            }
            finally
            {
                //com.Dispose();
                //con.Close();
                con.Dispose();
            }
        }
    }
}