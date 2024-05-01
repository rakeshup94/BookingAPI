using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Darina;

namespace TravillioXMLOutService.Supplier.Darina
{
    public class dr_Cancel
    {
        XElement travayooreq = null;
        string customerid = string.Empty;
        string transid = string.Empty;
        #region Darina Booking Cancellation Response (XML)
        public XElement bookingcancellationdarina(XElement req)
        {
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SupplierID").Single().Value;
            customerid = req.Descendants("CustomerID").Single().Value;
            transid = req.Descendants("TransID").Single().Value;
            travayooreq = req;
            //DarinaCredentials _credential = new DarinaCredentials();
            //var _url = _credential.APIURL;
            //var _action = "http://travelcontrol.softexsw.us/GetFileDetails";
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "1");
            var _url = suppliercred.Descendants("APIURL").FirstOrDefault().Value;
            var _action = suppliercred.Descendants("getfileActionURL").FirstOrDefault().Value;
            var flag = "GetFileDetails";
            string fileid = req.Descendants("ConfirmationNumber").Single().Value;
            string serviceid = "";
            string cancelcode = "";
            serviceid = req.Descendants("ServiceID").Single().Value;
            //_action = "http://travelcontrol.softexsw.us/CheckHotelCancellationCharges";
            _action = suppliercred.Descendants("checkchargeActionURL").FirstOrDefault().Value;
            flag = "CheckHotelCancellationCharges";
            string cancellationcharges = CallWebService(_url, _action, flag, fileid, serviceid, cancelcode,req);
         
            XElement doccancellationcharges = XElement.Parse(cancellationcharges);
            XNamespace res = "http://dtcws.softexsw.us";
            int error = doccancellationcharges.Descendants(res + "CancelCode").Count();
            if (error > 0)
            {
                cancelcode = doccancellationcharges.Descendants(res + "CancelCode").Single().Value;
                //_action = "http://travelcontrol.softexsw.us/CancelHotelReservation";
                _action = suppliercred.Descendants("cancelActionURL").FirstOrDefault().Value;
                flag = "CancelHotelReservation";
                string cancelres = CallWebService(_url, _action, flag, fileid, serviceid, cancelcode,req);
                
                if (cancelres != null)
                {
                    #region XML OUT
                   
                    #region calculate cancellation charges (add charges in Amount tag which we need to refund to the customer)
                    decimal totalcxlamt;
                    if (doccancellationcharges.Descendants(res + "ResultTxt").Single().Value == "Success")
                    {
                        if (doccancellationcharges.Descendants(res + "AmountType").Single().Value == "Night")
                        {

                            totalcxlamt = Convert.ToDecimal(doccancellationcharges.Descendants(res + "Amount").Single().Value) * Convert.ToDecimal(req.Descendants("PerNightPrice").Single().Value);

                            
                        }
                        else if (doccancellationcharges.Descendants(res + "AmountType").Single().Value == "Percentage")
                        {

                            totalcxlamt = Convert.ToDecimal(doccancellationcharges.Descendants(res + "Amount").Single().Value) * Convert.ToDecimal(req.Descendants("TotalPrice").Single().Value) / 100;

                           
                        }
                        else
                        {
                            totalcxlamt = 0;
                            
                        }
                    }
                    else
                    {
                        totalcxlamt = Convert.ToDecimal(doccancellationcharges.Descendants(res + "Amount").Single().Value);
                        
                    }
                  
                    #endregion
                    XElement doccancelresult = XElement.Parse(cancelres);
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    XNamespace resname = "http://travelcontrol.softexsw.us/";
                    IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest").ToList();
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
                               new XElement("Rooms",
                                   new XElement("Room",
                                       new XElement("Cancellation",
                                           new XElement("Amount", Convert.ToString(totalcxlamt)),
                                           new XElement("Status", Convert.ToString(doccancelresult.Descendants(resname + "CancelHotelReservationResult").Single().Value))
                                           )
                                       )
                                   )
                  ))));
                    return cancellationdoc;
                    #endregion
                }
                else
                {
                    #region Server is not responding
                    IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
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
                               new XElement("ErrorTxt", "Server is not responding")
                                       )
                                   )
                  ));
                    return cancellationdoc;
                    #endregion
                }
            }
            else
            {
                #region Reservation Not Found
                IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
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
                           new XElement("ErrorTxt", "Reservation Not Found")
                                   )
                               )
              ));
                return cancellationdoc;
                #endregion
            }

        }
        #endregion
        #region Methods for Darina Holidays
        public string CallWebService(string _url, string _action, string flag, string fileid, string serviceid, string cancelcode,XElement req)
        {
            try
            {
                XDocument soapEnvelopeXml = new XDocument();
                if (flag == "GetFileDetails")
                {
                    soapEnvelopeXml = CreateSoapEnvelopeGetFileDetails(fileid,req);
                }
                if (flag == "CheckHotelCancellationCharges")
                {
                    soapEnvelopeXml = CreateSoapEnvelopeCheckHotelCancellationCharge(fileid, serviceid,req);
                }
                if (flag == "CancelHotelReservation")
                {
                    soapEnvelopeXml = CreateSoapEnvelopeCancelHotelReservation(fileid, serviceid, cancelcode,req);
                }
                HttpWebRequest webRequest = CreateWebRequest(_url, _action);
                InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                asyncResult.AsyncWaitHandle.WaitOne();
                string soapResult;
                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                {
                    using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                    //if (flag == "CancelHotelReservation")
                    {
                        try
                        {
                            XElement availresponse = XElement.Parse(soapResult.ToString());
                            XElement doc = RemoveAllNamespaces(availresponse);
                            APILogDetail log = new APILogDetail();
                            log.customerID = Convert.ToInt32(travayooreq.Descendants("CustomerID").FirstOrDefault().Value);
                            log.TrackNumber = travayooreq.Descendants("TransID").Single().Value;
                            log.LogTypeID = 6;
                            log.LogType = "Cancel";
                            log.SupplierID = 1;
                            log.logrequestXML = soapEnvelopeXml.ToString();
                            log.logresponseXML = doc.ToString();
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SaveAPILogs(log);
                        }
                        catch (Exception exx)
                        {
                            CustomException ex1 = new CustomException(exx);
                            ex1.MethodName = "CallWebService";
                            ex1.PageName = "TrvHotelSearch";
                            ex1.CustomerID = travayooreq.Descendants("CustomerID").FirstOrDefault().Value;
                            ex1.TranID = travayooreq.Descendants("TransID").FirstOrDefault().Value;
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                        }
                    }
                    return soapResult;
                }
            }
            catch (WebException webex)
            {
                WebResponse errResp = webex.Response;
                using (Stream respStream = errResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream);
                    string text = reader.ReadToEnd();
                    CustomException ex1 = new CustomException(webex);
                    ex1.MethodName = "CallWebService";
                    ex1.PageName = "TrvHotelSearch";
                    ex1.CustomerID = travayooreq.Descendants("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = travayooreq.Descendants("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    return text;
                }
            }
        }
        private static HttpWebRequest CreateWebRequest(string url, string action)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }
        private static XDocument CreateSoapEnvelopeGetFileDetails(string fileid,XElement req)
        {
            #region Credentials
            string AccountName = string.Empty;
            string UserName = string.Empty;
            string Password = string.Empty;
            string AgentID = string.Empty;
            string Secret = string.Empty;
            //DarinaCredentials _credential = new DarinaCredentials();
            //AccountName = _credential.AccountName;
            //UserName = _credential.UserName;
            //Password = _credential.Password;
            //AgentID = _credential.AgentID;
            //Secret = _credential.Secret;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "1");
            AccountName = suppliercred.Descendants("AccountName").FirstOrDefault().Value;
            UserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
            Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
            AgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
            Secret = suppliercred.Descendants("SecStr").FirstOrDefault().Value;
            #endregion
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                          "<soap:Body>" +
                            "<GetFileDetails xmlns='http://travelcontrol.softexsw.us/'>" +
                             "<SecStr>" + Secret + "</SecStr>" +
                             "<AccountName>" + AccountName + "</AccountName>" +
                             "<UserName>" + UserName + "</UserName>" +
                             "<Password>" + Password + "</Password>" +
                             "<AgentID>" + AgentID + "</AgentID>" +
                              "<FileID>" + fileid + "</FileID>" +
                            "</GetFileDetails>" +
                          "</soap:Body>" +
                        "</soap:Envelope>";
            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private static XDocument CreateSoapEnvelopeCheckHotelCancellationCharge(string fileid, string serviceid,XElement req)
        {
            #region Credentials
            string AccountName = string.Empty;
            string UserName = string.Empty;
            string Password = string.Empty;
            string AgentID = string.Empty;
            string Secret = string.Empty;
            //DarinaCredentials _credential = new DarinaCredentials();
            //AccountName = _credential.AccountName;
            //UserName = _credential.UserName;
            //Password = _credential.Password;
            //AgentID = _credential.AgentID;
            //Secret = _credential.Secret;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "1");
            AccountName = suppliercred.Descendants("AccountName").FirstOrDefault().Value;
            UserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
            Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
            AgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
            Secret = suppliercred.Descendants("SecStr").FirstOrDefault().Value;
            #endregion
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                          "<soap:Body>" +
                            "<CheckHotelCancellationCharges xmlns='http://travelcontrol.softexsw.us/'>" +
                             "<SecStr>" + Secret + "</SecStr>" +
                             "<AccountName>" + AccountName + "</AccountName>" +
                             "<UserName>" + UserName + "</UserName>" +
                             "<Password>" + Password + "</Password>" +
                             "<AgentID>" + AgentID + "</AgentID>" +
                              "<FileID>" + fileid + "</FileID>" +
                              "<ServiceID>" + serviceid + "</ServiceID>" +
                            "</CheckHotelCancellationCharges>" +
                          "</soap:Body>" +
                        "</soap:Envelope>";
            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private static XDocument CreateSoapEnvelopeCancelHotelReservation(string fileid, string serviceid, string cancelcode,XElement req)
        {
            #region Credentials
            string AccountName = string.Empty;
            string UserName = string.Empty;
            string Password = string.Empty;
            string AgentID = string.Empty;
            string Secret = string.Empty;
            //DarinaCredentials _credential = new DarinaCredentials();
            //AccountName = _credential.AccountName;
            //UserName = _credential.UserName;
            //Password = _credential.Password;
            //AgentID = _credential.AgentID;
            //Secret = _credential.Secret;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "1");
            AccountName = suppliercred.Descendants("AccountName").FirstOrDefault().Value;
            UserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
            Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
            AgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
            Secret = suppliercred.Descendants("SecStr").FirstOrDefault().Value;
            #endregion
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                          "<soap:Body>" +
                            "<CancelHotelReservation xmlns='http://travelcontrol.softexsw.us/'>" +
                              "<SecStr>" + Secret + "</SecStr>" +
                             "<AccountName>" + AccountName + "</AccountName>" +
                             "<UserName>" + UserName + "</UserName>" +
                             "<Password>" + Password + "</Password>" +
                             "<AgentID>" + AgentID + "</AgentID>" +
                              "<FileID>" + fileid + "</FileID>" +
                              "<ServiceID>" + serviceid + "</ServiceID>" +
                              "<CancelCode>" + cancelcode + "</CancelCode>" +
                            "</CancelHotelReservation>" +
                          "</soap:Body>" +
                        "</soap:Envelope>";
            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private static void InsertSoapEnvelopeIntoWebRequest(XDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
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