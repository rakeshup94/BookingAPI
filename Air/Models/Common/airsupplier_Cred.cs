using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Air.Models.Common
{
    public static class airsupplier_Cred
    {
        static readonly XElement supcred;
        static airsupplier_Cred()
        {
            try
            {
                supcred = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/AirSupplierCredential/suppliercredentials.xml"));
            }
            catch { }
        }
        public static XElement getgds_credentials(this string customerid, string supplierid)
        {
            XElement suppcred = null;
            try
            {
                if (!string.IsNullOrEmpty(customerid))
                {
                    suppcred = supcred.Descendants("credential").Where(x => x.Attribute("customerid").Value == customerid && x.Attribute("supplierid").Value == supplierid).FirstOrDefault();
                }
                return suppcred;
            }
            catch
            {
                return null;
            }
        }
    }
}