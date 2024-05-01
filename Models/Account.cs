using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models
{
    #region Parameters for Login
    public class Account
    {
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
        public string AgentID
        {
            get;
            set;
        }
        public string ServiceType
        {
            get;
            set;
        }
        public string ServiceVersion
        {
            get;
            set;
        }
    }
    #endregion
}