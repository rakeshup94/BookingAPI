using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Xml.Linq;


namespace TravillioXMLOutService.Models.EBookingCenter
{
    public class EBookingStatic
    {
       static readonly XElement ebookingnationality;
       static EBookingStatic()
        {
            try
            {
                ebookingnationality = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/EBookingCenter/Nationality.xml"));
            }
            catch { }
        } 
        public static XElement ebook_nationality()
        {
            return ebookingnationality;
        }
        
    }
}