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
    public class darina_Getrooms
    {
        static SqlConnection con;
        private void connecttion()
        {
            string constr = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(constr);
            con.Open();
        }
        public DataTable Getrooms_Darina(Darina_Htdetail rooms)
        {
            DataTable dt = new DataTable("RoomList");
            try
            {
                connecttion();
                SqlCommand com = new SqlCommand("SP_GetRoom_Darina_merge", con);
                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@TrackNumber", rooms.TransID);
                com.Parameters.AddWithValue("@cusdID", rooms.custID);
                com.Parameters.Add("@retVal", SqlDbType.Int);
                com.Parameters["@retVal"].Direction = ParameterDirection.Output;
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