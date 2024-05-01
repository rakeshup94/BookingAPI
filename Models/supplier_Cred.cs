using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace TravillioXMLOutService.Models
{
    public static class supplier_Cred
    {
         static readonly XElement supcred;  
         static supplier_Cred()
        {
            try
            {
                supcred = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/SupplierCredential/suppliercredentials.xml"));
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
        public static int cutoff_time()
         {
            try
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["cutofftime"].ToString());
            }
            catch { return 90000; }
         }
        public static int rmcutoff_time()
        {
            try
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["rmcutofftime"].ToString());
            }
            catch { return 90000; }
        }
        public static int secondcutoff_time()
        {
            try
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["secondcutime"].ToString());
            }
            catch { return 90000; }
        }
    }
}