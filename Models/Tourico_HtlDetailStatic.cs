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
    public class Tourico_HtlDetailStatic
    {
        static SqlConnection con;
        private void connecttion()
        {
            string constr = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(constr);
            con.Open();
        }
        public DataTable GetHotelDetail_Tourico(Tourico_HtDetail htldetail)
        {
            DataTable dt = new DataTable("HotelDetail");
            try
            {
                connecttion();
                SqlCommand com = new SqlCommand("SP_GetTourico_HotelDetail", con);
                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@hotelid", htldetail.HotelID);
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
    }
}