using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Common;


namespace TravillioXMLOutService.Common
{
    public class TravayooDataAcess
    {
        private static string sqlconnectionstring;
        static TravayooDataAcess()
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
                using (SqlConnection vSqlConnection = TravayooDataAcess.OpenConnection())
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
                using (SqlConnection vSqlConnection = TravayooDataAcess.OpenConnection())
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



    public class TravayooRepository
    {


        public string GetHttpResponse(RequestModel request)
        {
            DateTime startTime = DateTime.Now;
            string response = string.Empty;
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(request.RequestStr);
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(request.HostName);
                myHttpWebRequest.Method = request.Method;
                myHttpWebRequest.ContentType = request.ContentType;
                myHttpWebRequest.ContentLength = data.Length;
                myHttpWebRequest.KeepAlive = true;
                myHttpWebRequest.AutomaticDecompression = DecompressionMethods.GZip;
                if (!string.IsNullOrEmpty(request.Header))
                {
                    myHttpWebRequest.Headers.Add(request.Header);
                }
                using (Stream requestStream = myHttpWebRequest.GetRequestStream())
                {
                    requestStream.Write(data, 0, data.Length);
                    requestStream.Close();
                    using (HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse())
                    {
                        if (myHttpWebResponse.StatusCode == HttpStatusCode.OK)
                        {
                            using (Stream responseStream = myHttpWebResponse.GetResponseStream())
                            {
                                using (StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default))
                                {
                                    response = myStreamReader.ReadToEnd();
                                    myStreamReader.Close();
                                    responseStream.Close();
                                    myHttpWebResponse.Close();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //throw ex;
            }
            return response;
        }

        public int SaveErrorLog(ExceptionModel log)
        {

            string code = string.Empty;

            SqlParameter[] pList = new SqlParameter[5];
            var flag = new SqlParameter();
            flag.ParameterName = "@flag";
            flag.Direction = ParameterDirection.Input;
            flag.SqlDbType = SqlDbType.Int;
            flag.Value = 1;
            pList[0] = flag;

            var Msg = new SqlParameter();
            Msg.ParameterName = "@ExceptionMsg";
            Msg.Direction = ParameterDirection.Input;
            Msg.SqlDbType = SqlDbType.BigInt;
            Msg.Value = log.Message;
            pList[1] = Msg;

            var logtype = new SqlParameter();
            logtype.ParameterName = "@ExceptionType";
            logtype.Direction = ParameterDirection.Input;
            logtype.SqlDbType = SqlDbType.VarChar;
            logtype.Value = log.Detail;
            pList[2] = logtype;

            var source = new SqlParameter();
            source.ParameterName = "@ExceptionSource";
            source.Direction = ParameterDirection.Input;
            source.SqlDbType = SqlDbType.NVarChar;
            source.Value = log.Source;
            pList[3] = source;
            var customer = new SqlParameter();
            customer.ParameterName = "@customerID";
            customer.Direction = ParameterDirection.Input;
            customer.SqlDbType = SqlDbType.VarChar;
            customer.Value = log.CustomerId;
            pList[4] = customer;
            var TransID = new SqlParameter();
            TransID.ParameterName = "@TransID";
            TransID.Direction = ParameterDirection.Input;
            TransID.SqlDbType = SqlDbType.NVarChar;
            TransID.Value = log.TrackNo;
            pList[4] = TransID;
            var method = new SqlParameter();
            method.ParameterName = "@MethodName";
            method.Direction = ParameterDirection.Input;
            method.SqlDbType = SqlDbType.VarChar;
            method.Value = log.Method;
            pList[4] = method;
            var page = new SqlParameter();
            page.ParameterName = "@PageName";
            page.Direction = ParameterDirection.Input;
            page.SqlDbType = SqlDbType.VarChar;
            page.Value = log.FileName;
            pList[4] = page;


            int result = TravayooDataAcess.Insert("APIProc", pList);
            return result;

        }

        public static DataTable GetData(SqlModel request)
        {
            SqlParameter[] pList = new SqlParameter[6];
            var flag = new SqlParameter();
            flag.ParameterName = "@flag";
            flag.Direction = ParameterDirection.Input;
            flag.SqlDbType = SqlDbType.Int;
            flag.Value = request.flag;
            pList[0] = flag;
            var columnList = new SqlParameter();
            columnList.ParameterName = "@columnList";
            columnList.Direction = ParameterDirection.Input;
            columnList.SqlDbType = SqlDbType.NVarChar;
            columnList.Value = request.columnList;
            pList[1] = columnList;
            var table = new SqlParameter();
            table.ParameterName = "@table";
            table.Direction = ParameterDirection.Input;
            table.SqlDbType = SqlDbType.VarChar;
            table.Value = request.table;
            pList[2] = table;

            var filter = new SqlParameter();
            filter.ParameterName = "@filter";
            filter.Direction = ParameterDirection.Input;
            filter.SqlDbType = SqlDbType.VarChar;
            filter.Value = request.filter;
            pList[3] = filter;

            var HotelCode = new SqlParameter();
            HotelCode.ParameterName = "@HotelCode";
            HotelCode.Direction = ParameterDirection.Input;
            HotelCode.SqlDbType = SqlDbType.VarChar;
            HotelCode.Value = request.HotelCode;
            pList[4] = HotelCode;

            var SupplierId = new SqlParameter();
            SupplierId.ParameterName = "@SuplId";
            SupplierId.Direction = ParameterDirection.Input;
            SupplierId.SqlDbType = SqlDbType.Int;
            SupplierId.Value = request.SupplierId;
            pList[5] = SupplierId;



            DataTable result = TravayooDataAcess.Get("APIProc", pList);
            return result;
        }



        public static string SupllierCity(string suplId, string CityId)
        {
            string code = "0";
            SqlParameter[] pList = new SqlParameter[3];
            var flag = new SqlParameter();
            flag.ParameterName = "@flag";
            flag.Direction = ParameterDirection.Input;
            flag.SqlDbType = SqlDbType.Int;
            flag.Value = 1;
            pList[0] = flag;

            var supplier = new SqlParameter();
            supplier.ParameterName = "@SuplId";
            supplier.Direction = ParameterDirection.Input;
            supplier.SqlDbType = SqlDbType.BigInt;
            supplier.Value = Convert.ToInt32(suplId);
            pList[1] = supplier;

            var city = new SqlParameter();
            city.ParameterName = "@CityId";
            city.Direction = ParameterDirection.Input;
            city.SqlDbType = SqlDbType.BigInt;
            city.Value = Convert.ToInt64(CityId);

            pList[2] = city;
            DataTable result = TravayooDataAcess.Get("dotwProc", pList);
            //code = result.Rows[0]["SupCityId"].ToString().Trim();
            if (result.Rows.Count > 0)
            {
                code = result.Rows[0]["SupCityId"].ToString().Trim();
            }
            return code;
        }










    }


    public static class TravayooHelper
    {
        public static string AlterFormat(this string strDate, string oldFormat, string newFormat)
        {
            DateTime dte = DateTime.ParseExact(strDate.Trim(), oldFormat, CultureInfo.InvariantCulture);
            return dte.ToString(newFormat);
        }

        public static string TotalStayEncode(this string Data)
        {
            Data = HttpUtility.UrlEncode(Data);
            Data = "data=" + Data;
            return Data;
        }


        public static XElement RemoveXmlns(this XElement doc)
        {
            doc.Descendants().Attributes().Where(x => x.IsNamespaceDeclaration).Remove();
            foreach (var elem in doc.Descendants())
                elem.Name = elem.Name.LocalName;

            return doc;
        }

        //public static string HotelIdByGiata(this string item, int flagId, int supplierId)
        //{
        //    string code = string.Empty;
        //    if (!string.IsNullOrEmpty(item))
        //    {
        //        SqlParameter[] pList = new SqlParameter[3];
        //        var flag = new SqlParameter();
        //        flag.ParameterName = "@flag";
        //        flag.Direction = ParameterDirection.Input;
        //        flag.SqlDbType = SqlDbType.Int;
        //        flag.Value = flagId;
        //        pList[0] = flag;

        //        var supplier = new SqlParameter();
        //        supplier.ParameterName = "@SuplId";
        //        supplier.Direction = ParameterDirection.Input;
        //        supplier.SqlDbType = SqlDbType.BigInt;
        //        supplier.Value = supplierId;
        //        pList[1] = supplier;

        //        var htlcode = new SqlParameter();
        //        htlcode.ParameterName = "@HotelCode";
        //        htlcode.Direction = ParameterDirection.Input;
        //        htlcode.SqlDbType = SqlDbType.VarChar;
        //        htlcode.Value = Convert.ToInt64(item);
        //        pList[2] = htlcode;

        //        DataTable result = TravayooDataAcess.Get("APIProc", pList);
        //        code = result.Rows[0]["hotelid"].ToString().Trim();
        //    }
        //    return code;
        //}



    }






}