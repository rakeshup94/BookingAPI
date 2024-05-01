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
    public class dr_Booking
    {        
        #region Darina Booking Response (XML)
        public XElement bookingresponsedarina(XElement req)
        {
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SuppliersID").Single().Value;

            string flag = "booking";
            string fileid = "";
            string bookingres = "";
            #region Booking Function            
            //DarinaCredentials _credential = new DarinaCredentials();
            //var _url = _credential.APIURL_PreBook_Book;
            //var _action = "http://travelcontrol.softexsw.us/SubmitBooking_Accomodation";
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "1");
            var _url = suppliercred.Descendants("APIURL_PreBook_Book").FirstOrDefault().Value;
            var _action = suppliercred.Descendants("bookActionURL").FirstOrDefault().Value;
            bookingres = CallWebService(req, _url, _action, flag, fileid);
            #endregion
            string filedetail = "";
            if (bookingres != null)
            {
                #region Booking Darina
                XElement doc = XElement.Parse(bookingres);
                fileid = doc.Descendants("FileID").FirstOrDefault().Value;
                #region Get Booking Details
                flag = "getfiledetail";               
                //_url = _credential.APIURL;
                //_action = "http://travelcontrol.softexsw.us/GetFileDetails";
                _url = suppliercred.Descendants("APIURL").FirstOrDefault().Value;
                _action = suppliercred.Descendants("getfileActionURL").FirstOrDefault().Value;
                filedetail = CallWebService(req, _url, _action, flag, fileid);
                #endregion
                XElement docfile = XElement.Parse(filedetail);
                XElement docmealplan = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\MealPlan.xml"));
                XNamespace res = "http://dtcws.softexsw.us";
                List<XElement> htpassengersinfo = docfile.Descendants(res + "PassengerInfo").ToList();
                IEnumerable<XElement> hthoteldetails = docfile.Descendants(res + "ServiceInfo").ToList();
                IEnumerable<XElement> request = req.Descendants("HotelBookingRequest").ToList();
                IEnumerable<XElement> roomdet = doc.Descendants("AccRates").ToList();
                IEnumerable<XElement> mealdetails = docmealplan.Descendants("d0").Where(x => x.Descendants("MealPlanID").Single().Value == roomdet.Descendants("MealPlan").Single().Value);
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                #region XML OUT
                XElement bookingdoc = new XElement(
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
                       new XElement("HotelBookingResponse",
                           new XElement("Hotels",
                               new XElement("HotelID", Convert.ToString(docfile.Descendants(res + "Product").Single().Value)),
                               new XElement("HotelName", Convert.ToString(docfile.Descendants(res + "ProductName").Single().Value)),
                               new XElement("FromDate", Convert.ToString(docfile.Descendants(res + "FromDate").Single().Value)),
                               new XElement("ToDate", Convert.ToString(docfile.Descendants(res + "ToDate").Single().Value)),
                               new XElement("AdultPax", Convert.ToString(docfile.Descendants(res + "AdultPax").Single().Value)),
                               new XElement("ChildPax", Convert.ToString(docfile.Descendants(res + "ChildPax").Single().Value)),
                               new XElement("TotalPrice", Convert.ToString(hthoteldetails.Descendants(res + "Price").Single().Value)),
                               new XElement("CurrencyID", Convert.ToString(docfile.Descendants(res + "Currency").Single().Value)),
                               new XElement("CurrencyCode", Convert.ToString(docfile.Descendants(res + "CurrencyName").Single().Value)),
                               new XElement("MarketID", Convert.ToString(docfile.Descendants(res + "Market").Single().Value)),
                               new XElement("MarketName", Convert.ToString(docfile.Descendants(res + "MarketName").Single().Value)),
                               new XElement("HotelImgSmall", ""),
                               new XElement("HotelImgLarge", ""),
                               new XElement("MapLink", ""),
                               new XElement("VoucherRemark", ""),
                               new XElement("TransID", Convert.ToString(docfile.Descendants(res + "AgentRef").Single().Value)),
                               new XElement("ConfirmationNumber", Convert.ToString(doc.Descendants("FileID").Single().Value)),
                               new XElement("Status", Convert.ToString(doc.Descendants("Result").Single().Value)),
                               new XElement("PassengersDetail",
                                   new XElement("GuestDetails",
                                       new XElement("Room",
                                          new XAttribute("ID", Convert.ToString(roomdet.Descendants("RoomType").Single().Value)),
                                          new XAttribute("RoomType", ""),
                                          new XAttribute("ServiceID", Convert.ToString(hthoteldetails.Descendants(res + "Serial").Single().Value)),
                                          new XAttribute("MealPlanID", Convert.ToString(roomdet.Descendants("MealPlan").Single().Value)),
                                          new XAttribute("MealPlanName", Convert.ToString(mealdetails.Descendants("MealPlanName").Single().Value)),
                                          new XAttribute("MealPlanCode", Convert.ToString(mealdetails.Descendants("MealPlanCode").Single().Value)),
                                          new XAttribute("MealPlanPrice", Convert.ToString("")),
                                          new XAttribute("PerNightRoomRate", Convert.ToString(roomdet.Descendants("RatePerNight").Single().Value)),
                                          new XAttribute("RoomStatus", Convert.ToString(doc.Descendants("Result").Single().Value)),
                                          new XAttribute("TotalRoomRate", Convert.ToString(roomdet.Descendants("RatePerStay").Single().Value)),
                                          GetHotelPassengersInfo(htpassengersinfo)))
                                   )

                               )
              ))));
                #endregion
                return bookingdoc;
                #endregion
            }
            else
            {
                #region Server not responding "Darina"
                IEnumerable<XElement> request = req.Descendants("HotelBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement bookingdoc = new XElement(
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
                       new XElement("HotelBookingResponse",
                           new XElement("ErrorTxt", "Server is not responding")
                                   )
                               )
              ));
                return bookingdoc;
                #endregion
            }
        }
        #endregion
        #region Darina Hotel Passenger's Info
        private IEnumerable<XElement> GetHotelPassengersInfo(List<XElement> htpassengersinfo)
        {
            #region Passenger's info for Darina Holidays
            List<XElement> pssngrlst = new List<XElement>();
            XNamespace res = "http://dtcws.softexsw.us";
            IEnumerable<XElement> roomtypes = htpassengersinfo;
            Parallel.For(0, htpassengersinfo.Count(), i =>
            {
                pssngrlst.Add(new XElement("RoomGuest",
                    new XElement("GuestType", Convert.ToString(htpassengersinfo[i].Descendants(res + "PassType").Single().Value)),
                    new XElement("Title", ""),
                    new XElement("FirstName", Convert.ToString(htpassengersinfo[i].Descendants(res + "Name").Single().Value)),
                    new XElement("MiddleName", ""),
                    new XElement("LastName", ""),
                    new XElement("IsLead", Convert.ToString(htpassengersinfo[i].Descendants(res + "IsPrimary").Single().Value)),
                    new XElement("Age", Convert.ToString(htpassengersinfo[i].Descendants(res + "Age").Single().Value))
                    ));
            });
            return pssngrlst;
            #endregion
        }
        #endregion
        #region Method's for Darina Holidays
        public string CallWebService(XElement req, string url, string action, string flag, string fileid)
        {
            try
            {
                var _url = url;
                var _action = action;
                XDocument soapEnvelopeXml = new XDocument();
                if (flag == "booking")
                {
                    soapEnvelopeXml = CreateSoapEnvelope(req);
                    try
                    {
                        APILogDetail logreq = new APILogDetail();
                        logreq.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        logreq.TrackNumber = req.Descendants("TransactionID").Single().Value;
                        logreq.LogTypeID = 5;
                        logreq.LogType = "Book";
                        logreq.SupplierID = 1;
                        logreq.logrequestXML = soapEnvelopeXml.ToString();
                        logreq.logresponseXML = "";
                        SaveAPILog savelogreq = new SaveAPILog();
                        savelogreq.SaveAPILogs(logreq);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CallWebService";
                        ex1.PageName = "TrvHotelBooking";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransactionID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                }
                if (flag == "getfiledetail")
                {
                    soapEnvelopeXml = CreateSoapEnvelopeGetFileDetails(fileid,req);
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
                    #region Log
                    if (flag == "booking")
                    {
                        try
                        {
                            XElement availresponse = XElement.Parse(soapResult.ToString());
                            XElement doc = RemoveAllNamespaces(availresponse);
                            APILogDetail log = new APILogDetail();
                            log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                            log.TrackNumber = req.Descendants("TransactionID").Single().Value;
                            log.LogTypeID = 5;
                            log.LogType = "Book";
                            log.SupplierID = 1;
                            log.logrequestXML = soapEnvelopeXml.ToString();
                            log.logresponseXML = doc.ToString();
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SaveAPILogs(log);
                        }
                        catch (Exception ex)
                        {
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "CallWebService";
                            ex1.PageName = "TrvHotelBooking";
                            ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                            ex1.TranID = req.Descendants("TransactionID").Single().Value;
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                        }
                    }
                    #endregion
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
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransactionID").Single().Value;
                        log.LogTypeID = 5;
                        log.LogType = "Book";
                        log.SupplierID = 1;
                        log.logrequestXML = req.ToString();
                        log.logresponseXML = text.ToString();
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CallWebService";
                        ex1.PageName = "TrvHotelBooking";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransactionID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
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

        private XDocument CreateSoapEnvelope(XElement req)
        {
            int totalchild = Convert.ToInt32(req.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value);
            string mainchildrenages = "";
            string childrenages = "";
            #region City ID / Country ID / Pax Nationality Country ID
            //string cityid = "";
            //string countryid = "";
            string paxcountryid = "";
            //XElement doccity = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\cities.xml"));
            XElement docccountry = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\country.xml"));
            //IEnumerable<XElement> doccityd = doccity.Descendants("d0").Where(x => x.Descendants("LetterCode").Single().Value == req.Descendants("CityCode").Single().Value);
            //countryid = doccityd.Descendants("CountryID").SingleOrDefault().Value;
            //cityid = doccityd.Descendants("Serial").SingleOrDefault().Value;
            IEnumerable<XElement> docccountryd = docccountry.Descendants("d0").Where(x => x.Descendants("Code").Single().Value == req.Descendants("PaxNationality_CountryCode").Single().Value);
            paxcountryid = docccountryd.Descendants("CountryID").SingleOrDefault().Value;
            #endregion
            if (totalchild > 0)
            {
                List<XElement> childage = req.Descendants("RoomPax").Descendants("ChildAge").ToList();
                for (int i = 0; i < childage.Count(); i++)
                {
                    childrenages = childrenages + "<int>" + Convert.ToString(childage[i].Value) + "</int>";
                }
                mainchildrenages = "<ChildrenAges>" + childrenages + "</ChildrenAges>";
            }
            else
            {
                mainchildrenages = "<ChildrenAges />";
            }
            #region Credentials
            string AccountName = string.Empty;
            string UserName = string.Empty;
            string Password = string.Empty;
            string AgentID = string.Empty;
            string Secret = string.Empty;
            string currencyid = string.Empty;
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
            currencyid = suppliercred.Descendants("currencyid").FirstOrDefault().Value;
            #endregion
            List<XElement> paxinfo = req.Descendants("Room").Where(x => x.Attribute("SupplierID").Value == "1").ToList();
            List<XElement> ss1 = PassengersDetailRequest(paxinfo).ToList();
            string ss2 = string.Concat(ss1.Nodes());
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                          "<soap:Body>" +
                            "<SubmitBooking_Accomodation xmlns='http://travelcontrol.softexsw.us/'>" +
                              "<SecStr>" + Secret + "</SecStr>" +
                             "<AccountName>" + AccountName + "</AccountName>" +
                             "<UserName>" + UserName + "</UserName>" +
                             "<Password>" + Password + "</Password>" +
                             "<AgentID>" + AgentID + "</AgentID>" +
                              "<FromDate>" + req.Descendants("FromDate").FirstOrDefault().Value + "</FromDate>" +
                              "<ToDate>" + req.Descendants("ToDate").FirstOrDefault().Value + "</ToDate>" +
                              "<AgentRef>" + req.Descendants("TransID").FirstOrDefault().Value + "</AgentRef>" +
                              "<CurrencyID>" + currencyid + "</CurrencyID>" +
                              "<Nationality_CountryID>" + paxcountryid + "</Nationality_CountryID>" + //320
                              "<AdultPax>" + req.Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value + "</AdultPax>" +
                              "<ChildPax>" + req.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value + "</ChildPax>" +
                              mainchildrenages +
                              "<Remarks>" + req.Descendants("SpecialRemarks").FirstOrDefault().Value + "</Remarks>" +
                              "<Passengers>" + ss2 +
                              "</Passengers>" +
                              "<RequestID>" + req.Descendants("RequestID").FirstOrDefault().Value + "</RequestID>" +   //903|636|1|7|0|945|T|F|1|0
                              "<IsTest>false</IsTest>" +
                            "</SubmitBooking_Accomodation>" +
                          "</soap:Body>" +
                        "</soap:Envelope>";
            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private IEnumerable<XElement> PassengersDetailRequest(List<XElement> htpassengersinfo)
        {
            XNamespace ns = "http://dtcws.softexsw.us";
            List<XElement> roomtypes = htpassengersinfo.Descendants("PaxInfo").ToList();
            List<XElement> roomtypes1 = htpassengersinfo.ToList();
            int count = 1;
            for (int i = 0; i < roomtypes.Count(); i++)
            {
                string Name = (Convert.ToString(roomtypes[i].Descendants("Title").FirstOrDefault().Value) + " " + Convert.ToString(roomtypes[i].Descendants("FirstName").FirstOrDefault().Value) + " " + Convert.ToString(roomtypes[i].Descendants("MiddleName").FirstOrDefault().Value) + " " + Convert.ToString(roomtypes[i].Descendants("LastName").FirstOrDefault().Value));
                yield return new XElement("Passengers", new XElement("PassInfo",
                    new XElement(ns + "Serial", count),
                    new XElement(ns + "Acc_Serial", Convert.ToString(roomtypes1[0].Attribute("SessionID").Value)),
                    new XElement(ns + "Name", Name),
                    new XElement(ns + "Type", Convert.ToString(roomtypes[i].Descendants("GuestType").FirstOrDefault().Value)),
                    new XElement(ns + "Primary", Convert.ToString(roomtypes[i].Descendants("IsLead").FirstOrDefault().Value)),
                    new XElement(ns + "Age", Convert.ToString(roomtypes[i].Descendants("Age").FirstOrDefault().Value)),
                    new XElement(ns + "NeedVisa", Convert.ToString(roomtypes[i].Descendants("NeedVisa").FirstOrDefault().Value))
                    )
                );
                count++;
            };
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