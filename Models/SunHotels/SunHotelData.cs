using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.SunHotels
{
    
    public class SunHotelData
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
                SqlCommand cmd = new SqlCommand("SP_GetSunHotel_ResortList", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
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

        public DataTable GetSunHotelHotelDetails(string CityID)
        {
            DataTable dt = new DataTable("Details");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetSunHotel_HotelDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
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
        public DataTable GetSunSingleHotelDetails(string HotelID)
        {
            DataTable dt = new DataTable("HotelDetails");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetSunHotel_HotelDetails_Unique", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@HotelID", HotelID);
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
        public DataTable GetSunImages(string HotelID, string cityID)
        {
            DataTable dt = new DataTable("Images");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetSunHotel_Images", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@HotelID", HotelID);
                cmd.Parameters.AddWithValue("@CityID", cityID);
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
        public DataTable GetSunHotelFacilities(string HotelID)
        {
            DataTable dt = new DataTable("Facilites");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetSunHotel_Facilities", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@HotelID", HotelID);
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
        public DataTable GetSunHotelRoomDetails()//(string HotelID)
        {
            DataTable dt = new DataTable("RoomDetails");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetSunHotel_SingleHotelDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;
                //cmd.Parameters.AddWithValue("@HotelID", HotelID);
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
        public DataTable GetW2MHotelFacility(string CityID)
        {
            DataTable dt = new DataTable("Facilities");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetW2M_Facilities", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
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

        public DataTable CityMapping(string CityID, string SupplierID)
        {
            DataTable dt = new DataTable("CityMapping");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GadouCityMapping", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
                cmd.Parameters.AddWithValue("@SupplierID", SupplierID);
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

    }
}