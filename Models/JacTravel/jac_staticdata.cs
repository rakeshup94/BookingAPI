using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.JacTravel
{
    public static class jac_staticdata
    {
        static SqlConnection con;
        public static void connection()
        {
            string connect = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(connect);
            con.Open();
        }
        static readonly XElement jac_city;
        static readonly string jac_meal;
        static readonly string jac_facility;
        
        static jac_staticdata()
        {
            try
            {
                jac_city = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/Common/JackCityMapping.xml"));
                jac_meal = HttpContext.Current.Server.MapPath(@"~/App_Data/JacTravel/MealBasis.xml").ToString();
                jac_facility = HttpContext.Current.Server.MapPath(@"~/App_Data/JacTravel/Facility.xml").ToString();
            }
            catch { }
        }     
        public static XElement jac_citymapping()
        {
            return jac_city;
        }
        public static string jac_mealmapping()
        {
            return jac_meal;
        }
        public static string jac_facilitymapping()
        {
            return jac_facility;
        }
        public static DataTable CityMapping(string CityID, string SuplID)
        {
            DataTable dt = new DataTable("CityMapping");
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("SP_GadouCityMapping", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CityID", CityID);
                cmd.Parameters.AddWithValue("@SupplierID", SuplID);
                cmd.Parameters.Add("@retVal", SqlDbType.Int);
                cmd.Parameters["@retVal"].Direction = ParameterDirection.Output;
                if (con.State.Equals("Closed"))
                    con.Open();
                dt.Load(cmd.ExecuteReader());
                return dt;
            }
            catch
            {
                return dt;
            }
            finally
            {
                con.Close();
            }
        }
    }
}