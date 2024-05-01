using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.SalTours
{
    public class SalDataAccess
    {
        static SqlConnection con;
        public void connection()
        {
            string connect = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(connect);
            con.Open();
        }
        public DataTable GetHotelsList(string CityID)
        {
            DataTable dt = new DataTable("Hotels");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetSalTour_HotelList", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
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
        public DataTable GetHotelDetails(string HotelID)
        {
            DataTable dt = new DataTable("Hotels");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetSalTour_SingleHotelDetail", con);
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
        public DataTable GetHotelImages(string HotelID)
        {
            DataTable dt = new DataTable("Hotels");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetSalTour_Images", con);
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
        public DataTable GetHotelFacilities(string HotelID)
        {
            DataTable dt = new DataTable("Hotels");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetSalTour_Facilities", con);
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
        public DataTable SalCityMapping(string CityId, string SupplierID)
        {
            DataTable dt = new DataTable("Details");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GadouCityMapping", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityId);
                cmd.Parameters.AddWithValue("@SupplierID", SupplierID);
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