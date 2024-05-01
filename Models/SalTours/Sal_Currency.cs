using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.SalTours
{
    public class Sal_Currency
    {
        static readonly XElement salCurrencies;
        static Sal_Currency()
        {
            try
            {
                salCurrencies = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/SalTours/SalCurrencies.xml"));
            }
            catch { }
        }
        public static XElement sal_Curencies()
        {
            return salCurrencies;
        }
    }
}