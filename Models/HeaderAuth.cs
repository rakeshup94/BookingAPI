using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Models
{
    public class HeaderAuth
    {
        public HeaderAuth()
        {
            
        }
        #region Header Authentication
        public bool Headervalidate(string userName, string password, string AgentID, string ServiceType, string ServiceVersion)
        {
            AccountModel acm = new AccountModel();

            if (acm.Login(userName, password, AgentID, ServiceType, ServiceVersion))
                return true;
            else
                return false;
        }
        #endregion
       
    }
}