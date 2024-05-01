using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.Juniper
{
    public static class junipercitymap
    {
        static readonly XElement jnpcity;
        static junipercitymap()
        {
            jnpcity = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/Juniper/JuniperCityMapping.xml"));            
        }
        public static XElement juniper_citylist()
        {
            return jnpcity;
        }
    }
}