using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web;

namespace TravillioXMLOutService.Models
{
    public class SaveAPILog : IDisposable
    {
        public SqlConnection con;
        private void connecttion()
        {
            string constr = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(constr);
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }
        }
        public void SaveAPILogs(APILogDetail apilog)
        {
            try
            {
                connecttion();
                string ipaddress = string.Empty;
                try
                {
                    //ipaddress = toGetIpAddress();
                }
                catch { }
                SqlCommand com = new SqlCommand("SP_InsertAPILog", con);
                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@customerID", apilog.customerID);
                com.Parameters.AddWithValue("@TrackNumber", apilog.TrackNumber);
                com.Parameters.AddWithValue("@logTypeID", apilog.LogTypeID);
                com.Parameters.AddWithValue("@logType", apilog.LogType);
                com.Parameters.AddWithValue("@SupplierID", apilog.SupplierID);
                com.Parameters.AddWithValue("@logMsg", ipaddress);
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
        public void SaveAPILogsflt(APILogDetail apilog)
        {
            try
            {
                connecttion();
                string ipaddress = string.Empty;
                try
                {
                    //ipaddress = toGetIpAddress();
                }
                catch { }
                SqlCommand com = new SqlCommand("SP_InsertAPILogflt", con);
                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@preID", apilog.preID);
                com.Parameters.AddWithValue("@customerID", apilog.customerID);
                com.Parameters.AddWithValue("@TrackNumber", apilog.TrackNumber);
                com.Parameters.AddWithValue("@logTypeID", apilog.LogTypeID);
                com.Parameters.AddWithValue("@logType", apilog.LogType);
                com.Parameters.AddWithValue("@SupplierID", apilog.SupplierID);
                com.Parameters.AddWithValue("@logMsg", ipaddress);
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
        public void SaveAPILogwithResponseError(APILogDetail apilog)
        {
            try
            {
                connecttion();
                string ipaddress = string.Empty;
                try
                {
                    //ipaddress = toGetIpAddress();
                }
                catch { }
                SqlCommand com = new SqlCommand("[SP_InsertApilogFail]", con);
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
        public void SendExcepToDB(Exception ex)
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
        public void SendCustomExcepToDB(CustomException ex)
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
        private string GetIPAddress()
        {
            try
            {
                if (System.ServiceModel.OperationContext.Current != null)
                {
                    var endpoint = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                    return endpoint.Address;
                }
                if (System.Web.HttpContext.Current != null)
                {
                    // Check proxied IP address
                    if (HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
                        return HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] + " via " +
                            HttpContext.Current.Request.UserHostAddress;
                    else
                        return HttpContext.Current.Request.UserHostAddress;

                }
            }
            catch { }
            return "Unknown";
        }

        public string toGetIpAddress()  // Get IP Address
        {
            string ip = "";
            try
            {
                IPHostEntry ipEntry = Dns.GetHostEntry(toGetCompCode());
                IPAddress[] addr = ipEntry.AddressList;
                ip = addr[1].ToString();
                return ip;
            }
            catch { }
            return "Unknown";
        }
        public string toGetCompCode()  // Get Computer Name
        {
            string strHostName = "";
            try
            {
                strHostName = Dns.GetHostName();
                return strHostName;
            }
            catch
            {

            }
            return "Unknown pc";
        }
        #region Dispose
        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}