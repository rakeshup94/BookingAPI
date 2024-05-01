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
    public class HotelsPro_Hotelstatic
    {
        static SqlConnection con;
        SqlCommand cmd;
        SqlDataAdapter adap;
        private void connecttion()
        {
            string constr = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(constr);
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }
        }
        public DataTable GetHotelList_HotelsPro(HotelsPro_Detail htldetail)
        {
            DataTable dt = new DataTable("HotelList");            
            try
            {                
                //connecttion();
                using (con = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("SP_GetHotelsPro_HotelList_Test", con))
                    {
                        //SqlCommand com = new SqlCommand("SP_GetHotelsPro_HotelList", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CityCode", htldetail.CityCode);
                        cmd.Parameters.AddWithValue("@MinStarRating", htldetail.MinStarRating);
                        cmd.Parameters.AddWithValue("@MaxStarRating", htldetail.MaxStarRating);

                        cmd.Parameters.AddWithValue("@HotelCode", htldetail.HotelCode);
                        cmd.Parameters.AddWithValue("@HotelName", htldetail.HotelName);


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
            catch(Exception ex)
            {                
                //con.Dispose();
                return dt;
            }
        }
        public DataTable GetCity_HotelsPro(HotelsPro_Detail htldetail)
        {
            DataTable dt = new DataTable("CityName");
            
            try
            {
                //connecttion();
                using (con = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("SP_GetCity_HotelsPro", con))
                    {
                        //SqlCommand com = new SqlCommand("SP_GetCity_HotelsPro", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CityCode", htldetail.CityCode);
                        cmd.Parameters.Add("@retVal", SqlDbType.Int);
                        cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                        //if (con.State.Equals("Closed"))
                        //    con.Open();
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
            catch(Exception ex)
            {
                //con.Dispose();
                return dt;
            }
        }
        public DataTable GetCountry_HotelsPro(HotelsPro_Detail htldetail)
        {
            DataTable dt = new DataTable("CountryCode");

            try
            {
                //connecttion();
                using (con = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("SP_GetCountry_HotelsPro", con))
                    {
                        //SqlCommand com = new SqlCommand("SP_GetCountry_HotelsPro", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Countryid", htldetail.CountryId);
                        cmd.Parameters.Add("@retVal", SqlDbType.Int);
                        cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                        //if (con.State.Equals("Closed"))
                        //    con.Open();
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
            catch(Exception ex)
            {
                //con.Dispose();
                return dt;
            }
        }
        public DataTable GetHotelDetail_HotelsPro(HotelsPro_Detail htldetail)
        {
            DataTable dt = new DataTable("HotelDetail");
           
            try
            {
                connecttion();
                SqlCommand com = new SqlCommand("SP_GetHotelsPro_HotelDetail", con);
                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@HotelCode", htldetail.HotelCode);
                com.Parameters.Add("@retVal", SqlDbType.Int);
                com.Parameters["@retVal"].Direction = ParameterDirection.Output;
                connecttion();
                dt.Load(com.ExecuteReader());
                return dt;
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