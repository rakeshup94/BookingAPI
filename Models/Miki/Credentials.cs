using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.MIKI
{
    public class Credentials
    {
        public string username
        {
            get
            {
                // HA
                //return "HAB003";
                // booking express
                return "BXT002";
            }
        }
        public string password
        {
            get
            {
                // HA
                //return "PASSWORDPASSWORD1234";
                // booking express
                return "PASSWORDPASSWORD1234";
            }
        }
    }
}