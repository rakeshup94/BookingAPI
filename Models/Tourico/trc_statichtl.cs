using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.Tourico
{
    public static class trc_statichtl
    {
        static readonly XElement statichtllist;
        static trc_statichtl()
        {
            try
            {
                statichtllist = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\HotelInfo.xml"));
            }
            catch { }
        }
        public static XElement tourico_htlstatic()
        {
            return statichtllist;
        }
    }
}