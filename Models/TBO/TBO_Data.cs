using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.TBO
{
    public class TBO_Data
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
            DataTable dt = new DataTable("CityMapping");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GadouCityMapping", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
                cmd.Parameters.AddWithValue("@SupplierID", SuplID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                if (con.State.Equals("Closed"))
                    con.Open();
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
        public DataTable CityData(string CityID)
        {
            DataTable dt = new DataTable("CityData");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetTBO_CityData", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                if (con.State.Equals("Closed"))
                    con.Open();
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
        public DataTable hotelDetail(string HotelID)
        {
            DataTable dt = new DataTable("HotelDetail");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetTBO_HotelDetail", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@HotelID", HotelID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                if (con.State.Equals("Closed"))
                    con.Open();
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
        public DataTable hotelImages(string HotelID)
        {
            DataTable dt = new DataTable("HotelDetail");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetTBO_Images", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@HotelID", HotelID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                if (con.State.Equals("Closed"))
                    con.Open();
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
                connection();
                dt.Load(cmd.ExecuteReader());
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                con.Close();
            }
        }
        public DataTable TagInfo(int localcountryid)
        {
            DataTable dt = new DataTable("TagInfo");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetTBO_TagInfo", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CountryID", localcountryid);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                if (con.State.Equals("Closed"))
                    con.Open();
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