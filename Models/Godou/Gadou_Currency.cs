using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.Godou
{
    public static class Gadou_Currency
    {
        static readonly XElement gadouCurrencies;
        static Gadou_Currency()
        {
            try
            {
                gadouCurrencies = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/Gadou/Currencies.xml"));
            }
            catch { }
        }
        public static XElement Gadaou_Currencies()
        {
            return gadouCurrencies;
        }
    }
}