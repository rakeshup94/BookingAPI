using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.TravelGate
{
    public class TGDataAccess
    {
        static SqlConnection con;
        public void connection()
        {
            string connect = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(connect);
            con.Open();
        }
        public DataTable CityMapping(string CityID, string SuplID)
        {
            DataTable dt = new DataTable("Details");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GadouCityMapping", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
                cmd.Parameters.AddWithValue("@SupplierID", SuplID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                connection();
                dt.Load(cmd.ExecuteReader());
                return dt;
            }
            catch
            {
                return dt;
            }
            finally
            {
                con.Close();
            }
        }
        public DataTable HotelList (string CityID)
        {
            DataTable dt = new DataTable("Details");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_SmyHotelList", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);  
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                connection();
                dt.Load(cmd.ExecuteReader());
                return dt;
            }
            catch
            {
                return dt;
            }
            finally
            {
                con.Close();
            }
        }
        public DataTable HotelDetails(string HotelIDs)
        {
            DataTable dt = new DataTable("Details");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_SmyHotelDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@HotelID", HotelIDs);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                dt.Load(cmd.ExecuteReader());
                return dt;

            }
            catch
            {
                return dt;
            }
            finally
            {
                con.Close();
            }
        }
        public DataTable SingleHotelDetails(string HotelID)
        {
            DataTable dt = new DataTable("Details");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_SmySingleHotelDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@HotelID", HotelID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                dt.Load(cmd.ExecuteReader());
                return dt;

            }
            catch
            {
                return dt;
            }
            finally
            {
                con.Close();
            }
        }
        public DataTable GetLog(string trackID, int LogtypeID, int SupplierID)
        {
            DataTable dt = new DataTable("LogTable");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetLog_XMLs", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue(@"transid", trackID);
                cmd.Parameters.AddWithValue("@logtypeID", LogtypeID);
                cmd.Parameters.AddWithValue("@Supplier", SupplierID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                dt.Load(cmd.ExecuteReader());
                return dt;
            }
            catch
            {
                return dt;
            }
            finally
            {
                con.Close();
            }
        }
    }
}