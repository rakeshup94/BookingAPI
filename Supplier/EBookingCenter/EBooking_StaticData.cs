using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Web;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TravillioXMLOutService.Supplier.EBookingCenter
{
    public class EBooking_StaticData
    {
        SqlConnection conn;
        SqlCommand cmd;
        SqlDataAdapter adap;
        public DataTable GetCityCode(string CityCode, int SupplierId)
        {
            DataTable dt = new DataTable("CityCode");
            try
            {
                using (conn = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("usp_GetTravcoCity", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@cityId", CityCode);
                        cmd.Parameters.AddWithValue("@suplId", SupplierId);
                        using (adap = new SqlDataAdapter(cmd))
                        {
                            conn.Open();
                            adap.Fill(dt);
                            conn.Close();
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return dt;

            }

        }
        public string GetSearchResponseXml(string TrackNumber, int SupplierId)
        {
            string ResXml = string.Empty;
            try
            {
                using (conn = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("usp_GetVotSearchResponse", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TrackNumber", TrackNumber);
                        cmd.Parameters.AddWithValue("@SupplierId", SupplierId);
                        using (adap = new SqlDataAdapter(cmd))
                        {
                            conn.Open();
                            DataTable dt = new DataTable();
                            adap.Fill(dt);
                            conn.Close();
                            ResXml = dt.Rows[0]["logresponseXML"].ToString().Trim();
                            return ResXml;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ResXml;

            }


        }
        public DataTable GetStaticHotels(string CityCode, string CountryCode, int MinRating, int MaxRating)
        {
            DataTable dt = new DataTable("Hotels");
            try
            {
                using (conn = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("usp_GetEBookingHotels", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CityCode", CityCode);
                        cmd.Parameters.AddWithValue("@CountryCode", CountryCode);
                        cmd.Parameters.AddWithValue("@MinStarRating", MinRating);
                        cmd.Parameters.AddWithValue("@MaxStarRating", MaxRating);
                        using (adap = new SqlDataAdapter(cmd))
                        {
                            conn.Open();
                            adap.Fill(dt);
                            conn.Close();
                            return dt;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                return dt;

            }


        }
        public DataTable GetHotelDetails(string HotelId)
        {
            DataTable dt = new DataTable("HotelDetail");
            try
            {
                using (conn = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("usp_GetEBookHotelDetail", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@HotelId", HotelId);
                        using (adap = new SqlDataAdapter(cmd))
                        {
                            conn.Open();
                            adap.Fill(dt);
                            conn.Close();
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return dt;

            }
        }
        public void InsertEBookHotelDetails(string htlcode, string address, string location, string image, string latitude, string longitude, string desc, string gallery, string phone, string email, string facility)
        {
            try
            {
                    using (conn = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                    {

                        using (cmd = new SqlCommand("insert into staticdata..EBookingHotelDetails ([HotelId],[Address],[Location],[Image],[Latitude],[Longitude],[Description],[Gallery],[Phone],[Email],[Facility]) values(@code,@address,@location,@image,@latitude,@longitude,@desc,@gallery,@phone,@email,@facility)", conn))
                        {
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandTimeout = 950;
                            cmd.Parameters.AddWithValue("@code", htlcode);
                            cmd.Parameters.AddWithValue("@address", address);
                            cmd.Parameters.AddWithValue("@location", location);
                            cmd.Parameters.AddWithValue("@image", image);
                            cmd.Parameters.AddWithValue("@latitude", latitude);
                            cmd.Parameters.AddWithValue("@longitude", longitude);
                            cmd.Parameters.AddWithValue("@desc", desc);
                            cmd.Parameters.AddWithValue("@gallery", gallery.ToString());
                            cmd.Parameters.AddWithValue("@phone", phone);
                            cmd.Parameters.AddWithValue("@email", email);
                            cmd.Parameters.AddWithValue("@facility", facility.ToString());
                            using (adap = new SqlDataAdapter(cmd))
                            {
                                conn.Open();
                                cmd.ExecuteNonQuery();
                                conn.Close();

                            }
                        }
                    }
                
            }
            catch (Exception ex)
            {


            }

        }
    }
}