using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.HotelBeds;

namespace TravillioXMLOutService.App_Code
{
    public class HotelCancellationHotelBeds
    {
        XElement reqTravayoo;
        #region Credentails of HotelBeds
        string apiKey = string.Empty;
        string Secret = string.Empty;
        #endregion

        #region Hotel Availability of HotelBeds (XML OUT for Travayoo)
        public XElement HotelroomCancellationHotelBeds(XElement req)
        {
            XElement hotelCancellation = null;
            XElement cancellationdoc = null;
            try
            {
                //HotelBedsCredential _credential = new HotelBedsCredential();
                //apiKey = _credential.apiKey;
                //Secret = _credential.Secret;
                //string endpoint = "https://api.test.hotelbeds.com/hotel-api/1.0/bookings/" + bookingid + "?cancellationFlag=CANCELLATION";

                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "4");
                apiKey = suppliercred.Descendants("apiKey").FirstOrDefault().Value;
                Secret = suppliercred.Descendants("Secret").FirstOrDefault().Value;
                string endpoint = suppliercred.Descendants("bookingendpoint").FirstOrDefault().Value;
                #endregion

                string bookingid = req.Descendants("ConfirmationNumber").Single().Value;  //"321-2420783";
                //string endpoint = "https://api.hotelbeds.com/hotel-api/1.0/bookings/" + bookingid + "?cancellationFlag=CANCELLATION";
                endpoint = endpoint +"/"+ bookingid + "?cancellationFlag=CANCELLATION";

                string signature;
                using (var sha = SHA256.Create())
                {
                    long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                    Console.WriteLine("Timestamp: " + ts);
                    var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(apiKey + Secret + ts));
                    signature = BitConverter.ToString(computedHash).Replace("-", "");
                }

                string response = string.Empty;
                using (var client = new WebClient())
                {

                    client.Headers.Add("X-Signature", signature);
                    client.Headers.Add("Api-Key", apiKey);
                    client.Headers.Add("Accept", "application/xml");
                    client.Headers.Add("Content-Type", "application/xml");
                    client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12;
                    byte[] response1 = client.UploadData(endpoint, "DELETE", new byte[] { });
                    response = System.Text.Encoding.Default.GetString(response1);

                    XElement availresponse = XElement.Parse(response.ToString());

                    XElement doc = RemoveAllNamespaces(availresponse);

                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                    log.TrackNumber = req.Descendants("TransID").Single().Value;
                    log.LogTypeID = 6;
                    log.LogType = "Cancel";
                    log.SupplierID = 4;
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = doc.ToString();
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SaveAPILogs(log);

                    XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                    XElement hotelcxlres = doc.Descendants("booking").FirstOrDefault();

                    hotelCancellation = hotelcxlres;
                    #region Cancellation Response

                    string username = req.Descendants("UserName").Single().Value;
                    string password = req.Descendants("Password").Single().Value;
                    string AgentID = req.Descendants("AgentID").Single().Value;
                    string ServiceType = req.Descendants("ServiceType").Single().Value;
                    string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;

                    string status = string.Empty;
                    if (hotelCancellation.Attribute("status").Value == "CANCELLED")
                    {
                        status = "Success";
                    }
                    else
                    {
                        status = hotelCancellation.Attribute("status").Value;
                    }

                    #region XML OUT CXL
                    IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    cancellationdoc = new XElement(
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
                                                   new XElement("Amount", Convert.ToString(hotelCancellation.Attribute("totalNet").Value)),
                                                   new XElement("Status", Convert.ToString(status))
                                                   )
                                               )
                                           )
                          ))));

                    #endregion
                    #endregion

                }
                return cancellationdoc;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelroomCancellationHotelBeds";
                ex1.PageName = "HotelCancellationHotelBeds";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                //APILog.SendCustomExcepToDB(ex1);
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);

                #region Check Booking Status
                try
                {
                    //HotelBedsCredential _credential = new HotelBedsCredential();
                    //apiKey = _credential.apiKey;
                    //Secret = _credential.Secret;
                    XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "4");
                    apiKey = suppliercred.Descendants("apiKey").FirstOrDefault().Value;
                    Secret = suppliercred.Descendants("Secret").FirstOrDefault().Value;
                    string endpoint = suppliercred.Descendants("bookingendpoint").FirstOrDefault().Value;

                    string bookingid = req.Descendants("ConfirmationNumber").Single().Value;
                    //string endpoint = "https://api.hotelbeds.com/hotel-api/1.0/bookings/" + bookingid + "";
                    endpoint = endpoint + "/" + bookingid;
                    string signature;
                    using (var sha = SHA256.Create())
                    {
                        long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                        Console.WriteLine("Timestamp: " + ts);
                        var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(apiKey + Secret + ts));
                        signature = BitConverter.ToString(computedHash).Replace("-", "");
                    }

                    string response = string.Empty;
                    using (var client = new WebClient())
                    {
                        string username = req.Descendants("UserName").Single().Value;
                        string password = req.Descendants("Password").Single().Value;
                        string AgentID = req.Descendants("AgentID").Single().Value;
                        string ServiceType = req.Descendants("ServiceType").Single().Value;
                        string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                        try
                        {
                            client.Headers.Add("X-Signature", signature);
                            client.Headers.Add("Api-Key", apiKey);
                            client.Headers.Add("Accept", "application/xml");
                            client.Headers.Add("Content-Type", "application/xml");
                            response = client.DownloadString(endpoint);
                            XElement availresponse = XElement.Parse(response.ToString());
                            XElement doc = RemoveAllNamespaces(availresponse);
                            XElement hotelcxlres = doc.Descendants("booking").FirstOrDefault();

                            hotelCancellation = hotelcxlres;
                            #region API Response                            
                            if (hotelCancellation.Attribute("status").Value == "CANCELLED")
                            {
                                #region XML OUT CXL
                                IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                                cancellationdoc = new XElement(
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
                                                   new XElement("ErrorTxt", "Booking already cancellated at supplier end. Please contact to your admin to cancel the booking."
                                                       )
                                      ))));
                                #endregion
                            }
                            else
                            {
                                #region XML OUT CXL
                                IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                                cancellationdoc = new XElement(
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
                                                   new XElement("ErrorTxt", ex.Message
                                                       )
                                      ))));
                                #endregion
                            }
                            #endregion
                        }
                        catch(Exception ex2)
                        {
                            #region XML OUT CXL
                            IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            cancellationdoc = new XElement(
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
                                               new XElement("ErrorTxt", ex2.Message
                                                   )
                                  ))));
                            #endregion
                        }
                    }
                }
                catch { }
                #endregion
                return cancellationdoc;
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