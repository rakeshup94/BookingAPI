using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Models.Galileo;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.Galileo
{
    public class gal_cancelreservation : IDisposable
    {
        #region Object
        XElement commonrequest = null;
        gal_supresponse api_resp;
        #endregion
        #region Common Response GAL
        public XElement cancelreservation_gal(XElement req)
        {
            #region Cancel Response (Gal)
            try
            {
                commonrequest = req;
                api_resp = new gal_supresponse();
                string api_response = api_resp.gal_apiresponse(req, cancelrequest(req).ToString(), "AirCancel", 6, req.Descendants("cancelRequest").FirstOrDefault().Attribute("TransID").Value, req.Descendants("cancelRequest").FirstOrDefault().Attribute("CustomerID").Value);
                XElement response = XElement.Parse(api_response);
                XElement supresp = RemoveAllNamespaces(response);
                XElement travayoo_out = reservationresponse(supresp);
                return travayoo_out;
            }
            catch(Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "cancelreservation_gal";
                ex1.PageName = "gal_cancelreservation";
                ex1.CustomerID = req.Descendants("cancelRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("cancelRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                string username = req.Descendants("UserName").FirstOrDefault().Value;
                string password = req.Descendants("Password").FirstOrDefault().Value;
                string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = req.Descendants("cancelRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement responseout = new XElement(
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
                #endregion
            }
             #endregion
        }
        #endregion
        #region Bind Response
        private XElement reservationresponse(XElement response)
        {
            XElement responseout = null;
            string username = commonrequest.Descendants("UserName").FirstOrDefault().Value;
            string password = commonrequest.Descendants("Password").FirstOrDefault().Value;
            string AgentID = commonrequest.Descendants("AgentID").FirstOrDefault().Value;
            string ServiceType = commonrequest.Descendants("ServiceType").FirstOrDefault().Value;
            string ServiceVersion = commonrequest.Descendants("ServiceVersion").FirstOrDefault().Value;
            IEnumerable<XElement> request = commonrequest.Descendants("cancelRequest");
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            try
            {
                string status = string.Empty;
                string cancelstatus = response.Descendants("ProviderReservationStatus").FirstOrDefault().Attribute("Cancelled").Value.Trim().ToString();
                if (cancelstatus == "true")
                {
                    status = "Cancelled";
                }
                else
                {
                    status = cancelstatus;
                }
                string bookingrefno = commonrequest.Descendants("cancelRequest").Attributes("bookingrefno").FirstOrDefault().Value;
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
                                    new XAttribute("success", "true"),
                                    new XAttribute("status", status),
                                    new XAttribute("bookingrefno", bookingrefno)
                                    )
                   ))));
                return responseout;
            }
            catch (Exception ex)
            {
                string errormsg = string.Empty;
                try
                {
                    errormsg = response.Descendants("Description").FirstOrDefault().Value;
                }
                catch
                {
                    errormsg = ex.Message;
                }
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
                                                new XElement("ErrorTxt", errormsg)
                                                )
                               ))));
                return responseout;
            }
        }
        #endregion
        #region Gal_Request
        private XElement cancelrequest(XElement req)
        {
            #region Gal Request
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace ns = "http://www.travelport.com/schema/air_v42_0";
            XNamespace ns1 = "http://www.travelport.com/schema/common_v42_0";
            XNamespace ns3 = "http://www.travelport.com/schema/universal_v42_0";
            #region Request
            XElement common_request = new XElement(soap + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                        new XElement(soap + "Body",
                                        new XElement(ns3 + "UniversalRecordCancelReq",
                                            new XAttribute("TraceId", "sk123"),
                                            new XAttribute("AuthorizedBy", "User"),
                                            new XAttribute("TargetBranch", "P7109079"),
                                            new XAttribute("UniversalRecordLocatorCode", req.Descendants("cancelRequest").Attributes("bookingrefno").FirstOrDefault().Value),
                                            new XAttribute("Version", "0"),
                                            new XElement(ns1 + "BillingPointOfSaleInfo",
                                                new XAttribute("OriginApplication", "uAPI"))
                                                            )));
            return common_request;
            #endregion
            #endregion
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
        #region Dispose
        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
            {
                // Free any other managed objects here.
            }
            disposed = true;
        }
        ~gal_cancelreservation()
        {
            Dispose(false);
        }
        #endregion
    }
}