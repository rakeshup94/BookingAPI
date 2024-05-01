using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models
{
    public static class transfersupplier_Cred
    {
        static readonly XElement supcred;
        static transfersupplier_Cred()
        {
            try
            {
                supcred = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/SupplierCredentialTransfer/transfercredentials.xml"));
            }
            catch { }
        }
         public static XElement getsupplier_credentials(this string customerid,string supplierid)
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