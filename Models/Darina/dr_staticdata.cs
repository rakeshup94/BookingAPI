using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models.Darina
{
    public static class dr_staticdata
    {
        static readonly XElement dr_doccurrency;
        static readonly XElement dr_docmealplan;
        static readonly XElement dr_dococcupancy;
        static readonly XElement dr_doccity;
        static readonly XElement dr_doccountry;
        static dr_staticdata()
        {
            try
            {
                dr_doccurrency = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\currency.xml"));
                dr_docmealplan = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\MealPlan.xml"));
                dr_dococcupancy = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\Occupancy.xml"));
                dr_doccity = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\cities.xml"));
                dr_doccountry = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\country.xml"));
            }
            catch { }
        }
        public static XElement drn_doccurrency()
        {
            return dr_doccurrency;
        }
        public static XElement drn_docmealplan()
        {
            
            return dr_docmealplan;
        }
        public static XElement drn_dococcupancy()
        {
            
            return dr_dococcupancy;
        }
        public static XElement drn_doccity()
        {

            return dr_doccity;
        }
        public static XElement drn_doccountry()
        {

            return dr_doccountry;
        }
    }
}