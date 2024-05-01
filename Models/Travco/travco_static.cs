using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.Travco
{
    public class travco_static
    {
        static readonly XElement travcocitymapping;
        static readonly XElement travco_htlstatic;
        static readonly XElement travcostaticrating;
        static travco_static()
        {
            try
            {
                //travcocitymapping = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/Common/TravcoCityMapping.xml"));
                //travco_htlstatic = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/Travco/HotelStatic.xml"));
                travcostaticrating = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/Travco/StarRating.xml"));
            }
            catch { }
        } 
        public static XElement travco_starcat()
        {
            return travcostaticrating;
        }
        public static XElement travco_statichtl()
        {
            return travco_htlstatic;
        }
        public static XElement travco_citymap()
        {
            return travcocitymapping;
        }
    }
}