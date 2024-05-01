using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Web;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TravillioXMLOutService.Air.Models.Galileo
{
    public class prcheck_res
    {
        SqlConnection conn;
        SqlCommand cmd;
        SqlDataAdapter adap;
        public DataTable getpricecheckres_gal(string tracknumber)
        {
            DataTable dt = new DataTable();
            try
            {
                using (conn = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("usp_Getprcheck_gal", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TrackNumber", tracknumber);
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
        public DataTable getpricecheckres_tbo(string tracknumber,string preID)
        {
            DataTable dt = new DataTable();
            try
            {
                using (conn = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (cmd = new SqlCommand("usp_Getprcheck_tbo", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TrackNumber", tracknumber);
                        cmd.Parameters.AddWithValue("@preID", preID);
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
    }
}