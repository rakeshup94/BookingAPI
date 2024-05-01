using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.SalTours
{
    public static class salCityMapping
    {
        static readonly XElement salCity;
        static salCityMapping()
        {
            //try
            //{
                salCity = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/SalTours/SalCityMapping.xml"));
            //}
            //catch { }
        }
        public static XElement sal_city()
        {
            return salCity;
        }
    }
}