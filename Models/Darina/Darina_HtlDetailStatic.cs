using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Web;

namespace TravillioXMLOutService.Models.Darina
{
    public class Darina_HtlDetailStatic
    {
        SqlConnection con;
        private void connecttion()
        {
            string constr = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(constr);
            con.Open();
        }
        public DataTable GetHotelDetail_Darina(Darina_Htdetail htldetail)
        {
            DataTable dt = new DataTable("HotelDetail");
            try
            {
                connecttion();
                SqlCommand com = new SqlCommand("SP_GetDarina_HotelDetail", con);
                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@transid", htldetail.TransID);
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