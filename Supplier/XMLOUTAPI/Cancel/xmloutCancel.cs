using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.XMLOUTAPI.Cancel
{
    public class xmloutCancel : IDisposable
    {
        #region Global vars
        string customerid = string.Empty;
        string dmc = string.Empty;
        int SupplierId = 501;
        string sessionKey = string.Empty;
        string sourcekey = string.Empty;
        string publishedkey = string.Empty;
        XElement reqTravayoo = null;
        #endregion
        #region Cancel Booking (XML OUT API)
        public XElement cancel_bookingexpressOUT(XElement req)
        {
            XElement cancelresp = null;
            reqTravayoo = req;
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SupplierID").Single().Value;
            try
            {
                #region Request
                XElement request = null;
                request = new XElement("cancelRQ",
                                        new XAttribute("customerID", req.Descendants("CustomerID").FirstOrDefault().Value),
                                        new XAttribute("sessionID", req.Descendants("TransID").FirstOrDefault().Value),
                                        new XAttribute("agencyID", req.Descendants("SubAgentID").FirstOrDefault().Value),
                                        new XElement("booking",
                                             new XAttribute("bookingID", req.Descendants("ConfirmationNumber").FirstOrDefault().Value),
                                             new XAttribute("bookingRef", req.Descendants("ConfirmationNumber").FirstOrDefault().Value),
                                             new XAttribute("currency", ""),
                                             new XElement("hotel",
                                                new XAttribute("appkey", req.Descendants("SupplierID").FirstOrDefault().Value),
                                                new XAttribute("checkIn", ""),
                                                new XAttribute("checkOut", ""),
                                                new XAttribute("code", ""),
                                                new XAttribute("name", ""),
                                                new XAttribute("sessionKey", req.Descendants("sessionKey").FirstOrDefault().Value),
                                                new XAttribute("sourcekey", req.Descendants("sourcekey").FirstOrDefault().Value),
                                                new XAttribute("publishedkey", req.Descendants("publishedkey").FirstOrDefault().Value),
                                                new XElement("rooms", bindroomoutreq(req.Descendants("Room").ToList()))
                                                 )
                                            )
                                      );
                #endregion 
                #region Response
                xmlResponseRequest apireq = new xmlResponseRequest();
                string apiresponse = apireq.xmloutHTTPResponse(request, "Cancel", 6, req.Descendants("TransID").FirstOrDefault().Value, req.Descendants("CustomerID").FirstOrDefault().Value, "501");
                XElement apiresp = null;
                try
                {
                    apiresp = XElement.Parse(apiresponse);
                    XElement hoteldetailsresponse = XElement.Parse(apiresponse.ToString());
                    apiresp = RemoveAllNamespaces(hoteldetailsresponse);
                }
                catch { }
                cancelresp = hotelbinding(apiresp.Descendants("booking") != null ? apiresp.Descendants("booking").FirstOrDefault() : null);
                #endregion
            }
            catch(Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "cancel_bookingexpressOUT";
                ex1.PageName = "xmloutCancel";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                cancelresp = new XElement(
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
                       new XElement("HotelCancellationResponse",
                           new XElement("ErrorTxt", "Server not responding")
                                   )
                               )
              ));
                return cancelresp;
                #endregion
            }
            return cancelresp;
        }
        #endregion
        #region Hotel Binding
        private XElement hotelbinding(XElement hotelBooking)
        {
            XElement htlresp = null;
            string username = reqTravayoo.Descendants("UserName").Single().Value;
            string password = reqTravayoo.Descendants("Password").Single().Value;
            string AgentID = reqTravayoo.Descendants("AgentID").Single().Value;
            string ServiceType = reqTravayoo.Descendants("ServiceType").Single().Value;
            string ServiceVersion = reqTravayoo.Descendants("ServiceVersion").Single().Value;
            try
            {
                IEnumerable<XElement> request = reqTravayoo.Descendants("HotelCancellationRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                string status = string.Empty;
                if (hotelBooking.Attribute("status").Value.ToUpper() == "CANCELLED" || hotelBooking.Attribute("status").Value.ToUpper() == "SUCCESS")
                {
                    status = "Success";
                }
                else
                {
                    status = hotelBooking.Attribute("status").Value;
                }
                htlresp = new XElement(
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
                               new XElement("HotelCancellationResponse",
                                   new XElement("Rooms",
                                       new XElement("Room",
                                           new XElement("Cancellation",
                                               new XElement("Amount", Convert.ToString(hotelBooking.Attribute("cxltotalRate").Value)),
                                               new XElement("Status", Convert.ToString(status))
                                               )
                                           )
                                       )
                      ))));
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "hotelbinding";
                ex1.PageName = "xmloutCancel";
                ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
                ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                IEnumerable<XElement> request = reqTravayoo.Descendants("HotelCancellationRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement cancellationdoc = new XElement(
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
                       new XElement("HotelCancellationResponse",
                           new XElement("ErrorTxt", ex.Message.ToString())
                                   )
                               )
              ));
                return cancellationdoc;
                #endregion
            }
            return htlresp;
        }
        #endregion 
        #region Room (Request)
        private List<XElement> bindroomoutreq(List<XElement> rooms)
        {
            List<XElement> roomlst = new List<XElement>();
            try
            {
                try
                {
                    for (int i = 0; i < rooms.Count(); i++)
                    {
                        string guestType = string.Empty;
                        roomlst.Add(new XElement("room",
                            new XAttribute("status", ""),
                            new XAttribute("serviceref", rooms[i].Attribute("ServiceID").Value),
                            new XAttribute("code", ""),
                            new XAttribute("name", ""),
                            new XAttribute("sourcekey", "")
                            ));
                    }
                }
                catch { }
            }
            catch { }
            return roomlst;
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
        public void Dispose()
        {
            this.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}