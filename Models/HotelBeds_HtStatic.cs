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
    public class HotelBeds_HtStatic
    {
        static SqlConnection con;
        SqlCommand cmd;
        SqlDataAdapter adap;
        private void connecttion()
        {
            string constr = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(constr);
            con.Open();
        }
        public DataTable GetCity_HotelBeds(HotelBeds_Detail htldetail)
        {
            DataTable dt = new DataTable("CityName");

            try
            {
                //connecttion();
                using (con = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("SP_GetCityHotelBeds", con))
                    {
                        //SqlCommand com = new SqlCommand("SP_GetCityHotelBeds", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CityCode", htldetail.CityCode);
                        cmd.Parameters.Add("@retVal", SqlDbType.Int);
                        cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                        //connecttion();
                        //dt.Load(com.ExecuteReader());
                        //return dt;
                        using (adap = new SqlDataAdapter(cmd))
                        {
                            con.Open();
                            adap.Fill(dt);
                            con.Close();
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //con.Dispose();
                return dt;
            }
        }
        public DataTable GetHotelList_HotelBeds(HotelBeds_Detail htldetail)
        {
            DataTable dt = new DataTable("HotelList");
            try
            {
                //connecttion();
                using (con = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("SP_GetHB_HotelDetail_Test", con))
                    {
                        //SqlCommand com = new SqlCommand("SP_GetHB_HotelDetail", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CityCode", htldetail.CityCode);
                        cmd.Parameters.AddWithValue("@CityID", htldetail.CityID);
                        cmd.Parameters.AddWithValue("@HotelCode", htldetail.HotelCode);
                        cmd.Parameters.AddWithValue("@MinRating", htldetail.MinRating);
                        cmd.Parameters.AddWithValue("@MaxRating", htldetail.MaxRating);

                        //cmd.Parameters.AddWithValue("@HotelCode", htldetail.HotelCode);
                        cmd.Parameters.AddWithValue("@HotelName", htldetail.HotelName);

                        cmd.Parameters.Add("@retVal", SqlDbType.Int);
                        cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                        //dt.Load(com.ExecuteReader());
                        //return dt;
                        using (adap = new SqlDataAdapter(cmd))
                        {
                            con.Open();
                            adap.Fill(dt);
                            con.Close();
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //con.Dispose();
                return dt;
            }
        }
        public DataTable GetHotelDetail_HotelBeds(HotelBeds_Detail htldetail)
        {
            DataTable dt = new DataTable("HotelDetail");
            try
            {
                connecttion();
                SqlCommand com = new SqlCommand("SP_GetHB_HtlDetail", con);
                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@CityCode", htldetail.CityCode);
                com.Parameters.AddWithValue("@HotelCode", htldetail.HotelCode);
                com.Parameters.Add("@retVal", SqlDbType.Int);
                com.Parameters["@retVal"].Direction = ParameterDirection.Output;
                dt.Load(com.ExecuteReader());
                return dt;
            }
            finally
            {
                //con.Close();
                con.Dispose();
            }
        }
        public DataTable GetRooms_HotelBeds(HotelBeds_Detail htldetail)
        {
            DataTable dt = new DataTable("RoomList");
            try
            {
                //connecttion();
                using (con = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("SP_GetRoom_HB_Giata_merge", con))
                    {
                        //SqlCommand com = new SqlCommand("SP_GetRoom_HB_Giata", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@type", "getroom");
                        cmd.Parameters.AddWithValue("@xmltype", htldetail.xmltype);
                        cmd.Parameters.AddWithValue("@tracknumber", htldetail.TrackNumber);
                        cmd.Parameters.AddWithValue("@custID", htldetail.custID);
                        cmd.Parameters.AddWithValue("@hotelcode", htldetail.HotelCode);
                        cmd.Parameters.Add("@retVal", SqlDbType.Int);
                        cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                        //dt.Load(com.ExecuteReader());
                        //return dt;
                        using (adap = new SqlDataAdapter(cmd))
                        {
                            con.Open();
                            adap.Fill(dt);
                            con.Close();
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //con.Dispose();
                return dt;
            }
        }
       
    }
}