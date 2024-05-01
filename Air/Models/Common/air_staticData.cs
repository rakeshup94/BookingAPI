using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Air.Models.Common
{
    public static class air_staticData
    {
        static readonly XElement airlinexml;
        static readonly XElement airportxml;
        static air_staticData()
        {
            try
            {
                airlinexml = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Flight\Mystifly\airlinelist.xml"));
                airportxml = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Flight\Mystifly\airportlist.xml"));                
            }
            catch { }
        }
        public static XElement air_airlinexml()
        {
            return airlinexml;
        }
        public static XElement air_airportxml()
        {

            return airportxml;
        }
    }
}