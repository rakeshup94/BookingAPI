using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.Restel
{
    public static class restel_citymapping
    {
        static readonly XElement restelcity;
        static restel_citymapping()
        {
            try
            {
                restelcity = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/Restel/RestelCityMapping.xml"));
            }
            catch { }
        }
        public static XElement restel_city()
        {
            return restelcity;
        }
    }
}