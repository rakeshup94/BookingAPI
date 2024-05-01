using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.HotelsPro
{
    public static class htlpro_facility
    {
        static readonly XElement htlspro_facility;
        static htlpro_facility()
        {
            try
            {
                htlspro_facility = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\HotelsPro\Facilities.xml"));
            }
            catch { }
        }
        public static XElement hotelspro_facility()
        {
            return htlspro_facility;
        }
    }
}