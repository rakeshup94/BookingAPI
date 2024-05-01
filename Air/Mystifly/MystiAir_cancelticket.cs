using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Models.Common;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.Mystifly
{
    public class MystiAir_cancelticket
    {
        #region Cancel Ticket (Mystifly)
        public XElement cancelticket_mysti(XElement req)
        {
            XElement responseout = null;
            #region Travayoo Header
            string username = req.Descendants("UserName").FirstOrDefault().Value;
            string password = req.Descendants("Password").FirstOrDefault().Value;
            string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
            string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
            IEnumerable<XElement> request = req.Descendants("cancelRequest");
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            try
            {                
                #endregion
                string url = string.Empty;
                string response = string.Empty;
                XElement suppliercred = airsupplier_Cred.getgds_credentials(req.Descendants("cancelRequest").Attributes("CustomerID").FirstOrDefault().Value, "12");
                url = suppliercred.Descendants("URL").FirstOrDefault().Value;
                string method = suppliercred.Descendants("AirCancel").FirstOrDefault().Value;
                string Target = suppliercred.Descendants("Mode").FirstOrDefault().Value;
                string sessionid = suppliercred.Descendants("sessionid").FirstOrDefault().Value;
                Mysti_SupplierResponse sup_response = new Mysti_SupplierResponse();
                string customerid = string.Empty;
                string trackno = string.Empty;
                customerid = req.Descendants("cancelRequest").Attributes("CustomerID").FirstOrDefault().Value;
                trackno = req.Descendants("cancelRequest").Attributes("TransID").FirstOrDefault().Value;
                string uniqueid = req.Descendants("cancelRequest").Attributes("bookingrefno").FirstOrDefault().Value;
                string apireq = cancelrequest(uniqueid, Target, customerid, trackno,sessionid);
                response = sup_response.supplierresponse_mystifly(url, apireq, method, "Cancel", 6, trackno, customerid).ToString();
                XElement availrsponse = XElement.Parse(response.ToString());
                XElement doc = RemoveAllNamespaces(availrsponse);
                string success = string.Empty;
                success = doc.Descendants("Success").FirstOrDefault().Value.Trim().ToString();
                if (success == "true")
                {
                    try
                    {
                        string status = doc.Descendants("Status").FirstOrDefault().Value.Trim().ToString();
                        string bookingrefno = doc.Descendants("UniqueID").FirstOrDefault().Value.Trim().ToString();
                        responseout = new XElement(
                               new XElement(soapenv + "Envelope",
                                         new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                         new XElement(soapenv + "Header",
                                          new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                          new XElement("Authentication",
                                              new XElement("AgentID", AgentID),
                                              new XElement("UserName", username),
                                              new XElement("Password", password),
                                              new XElement("ServiceType", ServiceType),
                                              new XElement("ServiceVersion", ServiceVersion))),
                                          new XElement(soapenv + "Body",
                                              new XElement(request.Single()),
                                    new XElement("cancelResponse",
                                        new XElement("Flights",
                                            new XAttribute("success", success),
                                            new XAttribute("status", status),
                                            new XAttribute("bookingrefno", bookingrefno)
                                            )
                           ))));
                        return responseout;
                    }
                    catch (Exception ex)
                    {
                        responseout = new XElement(
                               new XElement(soapenv + "Envelope",
                                         new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                         new XElement(soapenv + "Header",
                                          new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                          new XElement("Authentication",
                                              new XElement("AgentID", AgentID),
                                              new XElement("UserName", username),
                                              new XElement("Password", password),
                                              new XElement("ServiceType", ServiceType),
                                              new XElement("ServiceVersion", ServiceVersion))),
                                          new XElement(soapenv + "Body",
                                              new XElement(request.Single()),
                                    new XElement("cancelResponse",
                                        new XElement("Flights",
                                            new XElement("ErrorTxt", ex.Message)
                                            )
                           ))));
                        return responseout;

                    }
                }
                else
                {
                    #region Cancel Booking Failed
                    string errortxt = string.Empty;
                    try
                    {
                        errortxt = doc.Descendants("Message").FirstOrDefault().Value;
                    }
                    catch { }
                    responseout = new XElement(
                             new XElement(soapenv + "Envelope",
                                       new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XElement(soapenv + "Header",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                        new XElement("Authentication",
                                            new XElement("AgentID", AgentID),
                                            new XElement("UserName", username),
                                            new XElement("Password", password),
                                            new XElement("ServiceType", ServiceType),
                                            new XElement("ServiceVersion", ServiceVersion))),
                                        new XElement(soapenv + "Body",
                                            new XElement(request.Single()),
                                  new XElement("cancelResponse",
                                      new XElement("ErrorTxt",
                                          errortxt
                                          )
                         ))));
                    return responseout;
                    #endregion
                }
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "cancelticket_mysti";
                ex1.PageName = "MystiAir_cancelticket";
                ex1.CustomerID = req.Descendants("cancelRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("cancelRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                responseout = new XElement(
                               new XElement(soapenv + "Envelope",
                                         new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                         new XElement(soapenv + "Header",
                                          new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                          new XElement("Authentication",
                                              new XElement("AgentID", AgentID),
                                              new XElement("UserName", username),
                                              new XElement("Password", password),
                                              new XElement("ServiceType", ServiceType),
                                              new XElement("ServiceVersion", ServiceVersion))),
                                          new XElement(soapenv + "Body",
                                              new XElement(request.Single()),
                                    new XElement("cancelResponse",
                                        new XElement("Flights",
                                            new XElement("ErrorTxt", ex.Message)
                                            )
                           ))));
                return responseout;
            }
        }
        #endregion
        #region Cancel Ticket (Request)
        private string cancelrequest(string uniqueid, string mode,string customerid,string transid,string sessionid)
        {
            manage_session session_mgmt = new manage_session();
            //string sessionid = session_mgmt.session_manage(customerid, transid);
            string request = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:mys='Mystifly.OnePoint' xmlns:mys1='http://schemas.datacontract.org/2004/07/Mystifly.OnePoint'>" +
                               "<soapenv:Header/>" +
                               "<soapenv:Body>" +
                                  "<mys:CancelBooking>" +
                                     "<mys:rq>" +
                                        "<mys1:SessionId>" + sessionid + "</mys1:SessionId>" +
                                        "<mys1:Target>" + mode + "</mys1:Target>" +
                                        "<mys1:UniqueID>" + uniqueid + "</mys1:UniqueID>" +
                                     "</mys:rq>" +
                                  "</mys:CancelBooking>" +
                               "</soapenv:Body>" +
                            "</soapenv:Envelope>";
            return request;
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