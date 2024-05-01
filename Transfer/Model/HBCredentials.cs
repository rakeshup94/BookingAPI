using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Transfer.Model
{

    public class HBCredentials
    {
        public string ServiceHost
        {
            //get
            //{
            //    return "http://testapi.interface-xml.com/appservices/http/FrontendService";
            //}

              get;
            set;


        }
        public string UserName
        {
            //get
            //{
            //    return "TESTCHAINS";
            //}

            get;
            set;
        }
        public string Password
        {
            //get
            //{
            //    return "TESTCHAINS";
            //}

            get;
            set;
        }
        public string Id
        {
            get
            {
                return "1018995";
            }
        }

        public int SupplierId
        {
            get
            {
                return 10;
            }
        }
        public string Source
        {
            get
            {
                return "1";
            }
        }
        public int Currency
        {
            get
            {
                return 769;
            }
        }
        public string Culture
        {
            get
            {
                return "en";
            }
        }

        public string Service
        {
            get
            {
                return "Transfer";
            }
        }

        public string Platform
        {
            get
            {
                return "107";
            }

         
        }





    }
}