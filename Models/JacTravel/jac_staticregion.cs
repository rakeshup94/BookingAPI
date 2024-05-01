using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.JacTravel
{
    public static class jac_staticregion
    {
        static string jac_region;
        //static jac_staticregion(int regionid)
        //{
        //    jac_region = HttpContext.Current.Server.MapPath(@"~\App_Data\JacTravel\Property\" + regionid + ".xml").ToString();
        //}
        public static string jac_regionmapping(this int regionid)
        {
            try
            {
                jac_region = HttpContext.Current.Server.MapPath(@"~\App_Data\JacTravel\Property\" + regionid + ".xml").ToString();
                return jac_region;
            }
            catch { return null; }
        }
    }
}