using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.Darina
{
    public class DarinaCredentials
    {
        public string AccountName
        {
            get
            {
                //Holidays Arabia Test
                return "DTC";
                //Holidays Arabia LIVE
                //return "DTC";
            }
        }
        public string UserName
        {
            get
            {
                //Holidays Arabia Test
                return "XML2016Ra";
                //Holidays Arabia LIVE
                //return "XML2016Ra";
            }
        }
        public string Password
        {
            get
            {
                //Holidays Arabia Test
                return "DarinAH";
                //Holidays Arabia LIVE
                //return "DarinAH";
            }
        }
        public string AgentID
        {
            get
            {
                //Holidays Arabia Test
                return "1701";
                //Holidays Arabia LIVE
                //return "1701";
            }
        }
        public string Secret
        {
            get
            {
                //Holidays Arabia Test
                return "#C|559341#W#274298";
                //Holidays Arabia LIVE
                //return "#C|559341#W#274298";
            }
        }
        public string APIURL
        {
            get
            {
                //Holidays Arabia Test
                return "http://travelcontrol-agents-api.azurewebsites.net/service_v4.asmx";
                //Holidays Arabia LIVE
                //return "http://travelcontrol-agents-api.azurewebsites.net/service_v4.asmx";
            }
        }
        public string APIURL_PreBook_Book
        {
            get
            {
                //Holidays Arabia LIVE
                return "http://travelcontrol-agents-api-nocache.softexservers.com/service_v4_nocache.asmx";
                //Holidays Arabia LIVE
                //return "http://travelcontrol-agents-api.azurewebsites.net/service_v4.asmx";
            }
        }
        public string ActionURL
        {
            get
            {
                //Holidays Arabia Test
                return "http://travelcontrol-agents-api.azurewebsites.net/service_v4.asmx";
                //Holidays Arabia LIVE
                //return "http://travelcontrol-agents-api.azurewebsites.net/service_v4.asmx";
            }
        }
        public string Supplier
        {
            get
            {
                return "1";
            }
        }
    }
}