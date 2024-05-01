using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Linq;
using System.Configuration;

namespace TravillioXMLOutService.Models.Miki
{
    public class MikiStaticData
    {
        public XElement SavedHotels { get; set; }
        static SqlConnection con;
        public void connection()
        {
            string connect = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(connect);
            con.Open();
        }
        public DataTable GetMiki_Images(string CityID)
        {
            DataTable dt = new DataTable("Images");

            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetMiki_Images", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                if (con.State.Equals("Closed"))
                    con.Open();
                dt.Load(cmd.ExecuteReader());
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
                if (con.State.Equals("Closed"))
                    con.Open();
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
        public DataTable GetMiki_HotelDetails(string CityID)
        {
            DataTable dt = new DataTable("Details");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetMiki_HotelDetails", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                if (con.State.Equals("Closed"))
                    con.Open();
                dt.Load(cmd.ExecuteReader());
                return dt;
            }

            finally
            {
                con.Close();
            }
        }
        public DataTable GetMiki_Facilities(string CityID)
        {
            DataTable dt = new DataTable("Facilities");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetMiki_Facilities", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                if (con.State.Equals("Closed"))
                    con.Open();
                dt.Load(cmd.ExecuteReader());
                return dt;
            }

            finally
            {
                con.Close();
            }
        }
        public DataTable GetMiki_RoomList(string trackID)
        {
            DataTable dt = new DataTable("RoomList");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GetMiki_RoomList", con);
                cmd.CommandType = CommandType.StoredProcedure;
                 cmd.Parameters.AddWithValue("@transid", trackID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                if (con.State.Equals("Closed"))
                    con.Open();
                dt.Load(cmd.ExecuteReader());
                return dt;
            }

            finally
            {
                con.Close();
            }
            
        }
        public DataTable MikiCityMapping(string CityId, string SupplierID)
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
