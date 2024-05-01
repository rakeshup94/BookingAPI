using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.RTS
{
    public static class RTS_citymap
    {
         static readonly string rtscity;
         static RTS_citymap()
        {
            try
            {
                rtscity = HttpContext.Current.Server.MapPath(@"~/App_Data/RTS_CityMapping/RTSCityMapping.xml").ToString();
            }
            catch { }
        }
        public static string rts_citylist()
        {
            return rtscity;
        }
    }
}