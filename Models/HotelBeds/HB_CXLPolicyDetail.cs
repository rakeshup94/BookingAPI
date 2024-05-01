using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Web;

namespace TravillioXMLOutService.Models.HotelBeds
{
    public class HB_CXLPolicyDetail
    {
        static SqlConnection con;
        private static void connecttion()
        {
            string constr = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(constr);
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }
        }
        public DataTable GetCXLPolicy_HotelBeds(HotelBeds_Detail trfdetail)
        {
            DataTable dt = new DataTable("HotelList");
            try
            {
                connecttion();
                SqlCommand com = new SqlCommand("SP_HBCXLPolicyTransfer", con);
                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@TrackNumber", trfdetail.TrackNumber);
                com.Parameters.Add("@retVal", SqlDbType.Int);
                com.Parameters["@retVal"].Direction = ParameterDirection.Output;
                connecttion();
                dt.Load(com.ExecuteReader());
                return dt;
            }
            finally
            {
                //com.Dispose();
                con.Close();
            }
        }
    }
}