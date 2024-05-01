using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models
{
    public class AccountModel
    {
        private List<Account> listAccounts = new List<Account>();
        #region Account Details for Travayoo Service
        public AccountModel()
        {
            #region Live Credentials for Travayoo
            listAccounts.Add(new Account { UserName = "Travillio", Password = "ing@tech", AgentID = "TRV", ServiceType = "HT_001", ServiceVersion = "v1.0" });
            #endregion
            #region Live Credentials for HA
            listAccounts.Add(new Account { UserName = "Travayoo", Password = "ing@tech", AgentID = "HA", ServiceType = "HT_001", ServiceVersion = "v1.0" });
            #endregion
            #region Test Credentials
            listAccounts.Add(new Account { UserName = "TravillioCustomer", Password = "ing@techc", AgentID = "TRV", ServiceType = "HT_001", ServiceVersion = "v1.0" });
            #endregion
            #region Credentials for QA Team
            listAccounts.Add(new Account { UserName = "TravillioQA", Password = "ing@techqa_qa", AgentID = "TRVQA", ServiceType = "HT_001", ServiceVersion = "v1.0" });
            #endregion
            #region Test Credentials for Air
            listAccounts.Add(new Account { UserName = "Travayoo", Password = "ingflt@tech", AgentID = "TRV", ServiceType = "FLT_001", ServiceVersion = "v1.0" });
            #endregion
            #region Credentials for Transfer Test
            listAccounts.Add(new Account { UserName = "Travillio", Password = "ing@tech", AgentID = "TRV", ServiceType = "TR_001", ServiceVersion = "v1.0" });
            #endregion
        }
        #endregion
        #region Authentication of Credentials
        public bool Login(string UserName,string Password,string AgentID, string ServiceType, string ServiceVersion)
        {
            return listAccounts.Count(a => a.UserName.Equals(UserName) && a.Password.Equals(Password) && a.AgentID.Equals(AgentID) && a.ServiceType.Equals(ServiceType) && a.ServiceVersion.Equals(ServiceVersion)) > 0;
        }
        #endregion
        
    }
}