using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TravillioXMLOutService.Common.DotW;



namespace TravillioXMLOutService.Models.DotW
{
    public class DotWCredentials
    {


        public DotWCredentials(string _serviceHost, string _userName, string _password, string _id, int _Currency)
        {
            this.ServiceHost = _serviceHost;
            this.UserName = _userName;
            this.Password = _password;
            this.Id = _id;
            this.Currency = _Currency;
            
        }

        public string ServiceHost
        {
            get;
            set;
        }
        public string UserName
        {
            get;
            set;
        }
        public string Password
        {
            get;
            set;
        }
        public string Id
        {
            get;
            set;
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
            get;
            set;
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
                return DotWProduct.hotel.ToString();
            }
        }
        public string Supplier
        {
            get
            {
                return "5";
            }
        }
    }
}