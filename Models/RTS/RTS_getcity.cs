using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.RTS
{
    public class RTS_getcity
    {
        static SqlConnection con;
        private void connecttion()
        {
            string constr = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(constr);
            con.Open();
        }
        public DataTable GetCity_RTS(RTS_citydetail ctydetail)
        {
            DataTable dt = new DataTable();
            try
            {
                connecttion();
                SqlCommand com = new SqlCommand("usp_GetcitycodeRTS", con);
                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@cityid", ctydetail.CityID);
                connecttion();
                dt.Load(com.ExecuteReader());
                return dt;
            }
            finally
            {
                con.Dispose();
            }
        }
    }
}