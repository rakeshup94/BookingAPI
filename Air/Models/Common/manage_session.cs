using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Mystifly;

namespace TravillioXMLOutService.Air.Models.Common
{
    public class manage_session
    {
        SqlConnection conn;
        SqlCommand command;
        string SessionValue = string.Empty;
        int Status = 0;
        int Result = 0;
        string customerID = string.Empty;
        string transactionid = string.Empty;
        public string session_manage(string custID,string transid, int suppID)
        {
            try
            {
                using (conn = new SqlConnection(ConfigurationManager.ConnectionStrings["INGMContext"].ToString()))
                {
                    using (command = new SqlCommand("usp_GetSessionValue", conn))
                    {
                        if (command.Connection.State == System.Data.ConnectionState.Closed)
                        {
                            command.Connection.Open();
                        }
                        Status = 0;
                        SqlDataAdapter adapter = new SqlDataAdapter();
                        command.Parameters.Clear();
                        //command.CommandText = "[dbo].[sp_GetSessionValue]";
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@CustomerID", custID);
                        command.Parameters.AddWithValue("@InsertStatus", 0);
                        command.Parameters.AddWithValue("@SessionVal", "");
                        command.Parameters.Add("@ChkStatus", SqlDbType.Int);
                        command.Parameters["@ChkStatus"].Direction = ParameterDirection.Output;
                        command.Parameters.Add("@SessionValue", SqlDbType.NVarChar, 4000);
                        command.Parameters["@SessionValue"].Direction = ParameterDirection.Output;
                        Result = command.ExecuteNonQuery();
                        Status = Convert.ToInt32(command.Parameters["@ChkStatus"].Value);
                        SessionValue = command.Parameters["@SessionValue"].Value.ToString();
                        adapter.SelectCommand = command;

                        if (Status == 0)
                        {
                            customerID = custID;
                            transactionid = transid;
                            SessionValue = create_session();
                            adapter = new SqlDataAdapter();
                            command.Parameters.Clear();
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@CustomerID", custID);
                            command.Parameters.AddWithValue("@InsertStatus", 1);
                            command.Parameters.AddWithValue("@SessionVal", SessionValue);
                            command.Parameters.Add("@ChkStatus", SqlDbType.Int);
                            command.Parameters["@ChkStatus"].Direction = ParameterDirection.Output;
                            command.Parameters.Add("@SessionValue", SqlDbType.NVarChar, 4000);
                            command.Parameters["@SessionValue"].Direction = ParameterDirection.Output;
                            Result = command.ExecuteNonQuery();
                            SessionValue = command.Parameters["@SessionValue"].Value.ToString();
                            Status = Convert.ToInt32(command.Parameters["@ChkStatus"].Value);
                            adapter.SelectCommand = command;
                        }

                    }
                }
                return SessionValue;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        #region Create Session Request
        private string createsessionrequest()
        {
            XElement suppliercred = airsupplier_Cred.getgds_credentials(customerID, "12");
            string AccountNumber = suppliercred.Descendants("AccountNumber").FirstOrDefault().Value;
            string Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
            string Target = suppliercred.Descendants("Mode").FirstOrDefault().Value;
            string UserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;

            string request = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:mys='Mystifly.OnePoint' xmlns:mys1='http://schemas.datacontract.org/2004/07/Mystifly.OnePoint'>" +
                              "<soapenv:Header/>" +
                              "<soapenv:Body>" +
                                "<mys:CreateSession>" +
                                  "<mys:rq>" +
                                    "<mys1:AccountNumber>" + AccountNumber + "</mys1:AccountNumber>" +
                                    "<mys1:Password>" + Password + "</mys1:Password>" +
                                    "<mys1:Target>" + Target + "</mys1:Target>" +
                                    "<mys1:UserName>" + UserName + "</mys1:UserName>" +
                                  "</mys:rq>" +
                                "</mys:CreateSession>" +
                              "</soapenv:Body>" +
                            "</soapenv:Envelope>";
            return request;
        }
        #endregion
        #region Create Session
        private string create_session()
        {
            try
            {
                string response = string.Empty;
                XElement suppliercred = airsupplier_Cred.getgds_credentials(customerID, "12");
                string url = suppliercred.Descendants("URL").FirstOrDefault().Value;
                string CreateSession = suppliercred.Descendants("CreateSession").FirstOrDefault().Value;
                #region Create Session
                string sessionrequest = createsessionrequest();
                Mysti_SupplierResponse sup_response = new Mysti_SupplierResponse();
                string sessionresponse = sup_response.supplierresponse_mystifly(url, sessionrequest, CreateSession, "CreateSession", 20, transactionid, customerID);
                XElement sessionresp = XElement.Parse(sessionresponse);
                XElement docsession = RemoveAllNamespaces(sessionresp);
                string sessionid = docsession.Descendants("SessionId").FirstOrDefault().Value;
                return sessionid;
                #endregion
            }
            catch(Exception ex)
            {
                return "exception";
            }
        }
        #endregion
        #region Remove Namespaces
        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            XElement xmlDocumentWithoutNs = removeAllNamespaces(xmlDocument);
            return xmlDocumentWithoutNs;
        }
        private static XElement removeAllNamespaces(XElement xmlDocument)
        {
            var stripped = new XElement(xmlDocument.Name.LocalName);
            foreach (var attribute in
                    xmlDocument.Attributes().Where(
                    attribute =>
                        !attribute.IsNamespaceDeclaration &&
                        String.IsNullOrEmpty(attribute.Name.NamespaceName)))
            {
                stripped.Add(new XAttribute(attribute.Name.LocalName, attribute.Value));
            }
            if (!xmlDocument.HasElements)
            {
                stripped.Value = xmlDocument.Value;
                return stripped;
            }
            stripped.Add(xmlDocument.Elements().Select(
                el =>
                    RemoveAllNamespaces(el)));
            return stripped;
        }
        #endregion
    }
}