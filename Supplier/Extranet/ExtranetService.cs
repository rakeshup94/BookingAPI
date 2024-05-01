using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Supplier.Extranet
{
    public class ExtranetService
    {

        #region Extranet Proc


        public static string GetList_HotelExtranets(string GiataId, string HotelName, string CityId)
        {
            SqlConnection con;
            SqlCommand cmd;
            SqlDataAdapter adap;
            string constr = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(constr);
            con.Open();

            DataTable dt = new DataTable("HotelList");

            try
            {
                //connecttion();
                using (con = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("SP_GetExtranet_HotelDetail_Test", con))
                    {

                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@HotelCode", GiataId);
                        cmd.Parameters.AddWithValue("@HotelName", HotelName);
                        cmd.Parameters.AddWithValue("@CityID", CityId);

                        cmd.Parameters.Add("@retVal", SqlDbType.Int);
                        cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;

                        using (adap = new SqlDataAdapter(cmd))
                        {
                            con.Open();
                            adap.Fill(dt);
                            con.Close();

                            string hotel = string.Empty;
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                hotel = hotel + "<HotelIds>" + dt.Rows[i]["hotelid"].ToString() + "</HotelIds>";
                            }
                            if (hotel != string.Empty)
                            {
                                string lst = "<HotelIdsList>" + hotel + "</HotelIdsList>";
                                return lst;
                            }
                            else
                            {
                                return null;
                            }




                        }
                    }
                }
            }
            catch (Exception ex)
            {
                con.Close();
                //con.Dispose();
                return null; 
            }
        }
        #endregion




    }
}