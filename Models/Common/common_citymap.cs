using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.Common
{
    public static class common_citymap
    {
        static readonly XElement commoncities;
        static common_citymap()
        {
            try
            {
                commoncities = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/Common/CityMapping.xml"));
            }
            catch { }
        }
        public static XElement common_citymapping()
        {
            try
            {
                return commoncities;
            }
            catch { return null; }
        }
    }
}