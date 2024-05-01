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
    #region Book (Galileo)
    public class gal_book : IDisposable
    {
        #region Object
        XElement commonrequest = null;
        gal_supresponse api_resp;
        string carrier = string.Empty;
        #endregion
        #region Book Response
        public XElement book_response(XElement req)
        {
            #region Book Response (Gal)
            commonrequest = req;
            try
            {      
                api_resp = new gal_supresponse();
                string api_response = api_resp.gal_apiresponse(req, book_request(req).ToString(), "AirBook", 5, req.Descendants("BookingRequest").FirstOrDefault().Attribute("TransID").Value, req.Descendants("BookingRequest").FirstOrDefault().Attribute("CustomerID").Value);
                XElement response = XElement.Parse(api_response);
                XElement supresp = RemoveAllNamespaces(response);
                XElement travayoo_out = bookingresponse_gal(supresp);
                return travayoo_out;
            }
            catch(Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "book_response";
                ex1.PageName = "gal_book";
                ex1.CustomerID = req.Descendants("BookingRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("BookingRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                string username = req.Descendants("UserName").FirstOrDefault().Value;
                string password = req.Descendants("Password").FirstOrDefault().Value;
                string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = req.Descendants("BookingRequest");
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
                                  new XElement("BookingResponse",
                                      new XElement("ErrorTxt",
                                          ex.Message.ToString()
                                          )
                         ))));
                return responseout;
                #endregion
            }
            #endregion
        }
        #endregion
        #region Booking Response Binding
        private XElement bookingresponse_gal(XElement response)
        {
            try
            {
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                string username = commonrequest.Descendants("UserName").FirstOrDefault().Value;
                string password = commonrequest.Descendants("Password").FirstOrDefault().Value;
                string AgentID = commonrequest.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = commonrequest.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = commonrequest.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = commonrequest.Descendants("BookingRequest");
                string success = string.Empty;
                string status = string.Empty;
                string bookingrefno = string.Empty;
                string resLocatorCode = string.Empty;
                string supLocatorCode = string.Empty;
                string provresLocatorCode = string.Empty;
                List<XElement> pricinginfolst = null;
                if (response.Descendants("ErrorInfo").Count() > 0 || response.Descendants("AirSegmentError").Count()>0)
                {
                    string errormsg = string.Empty;
                    if (response.Descendants("ErrorInfo").Count() > 0)
                    {
                        errormsg = response.Descendants("ErrorInfo").Descendants("Description").FirstOrDefault().Value;
                    }
                    else if(response.Descendants("AirSegmentError").Count()>0)
                    {
                        errormsg = response.Descendants("AirSegmentError").Descendants("ErrorMessage").FirstOrDefault().Value;
                    }
                    else
                    {
                        errormsg = "Booking Failed.";
                    }
                    XElement searchdoc = new XElement(
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
                                              new XElement("BookingResponse",
                                                  new XElement("ErrorTxt", errormsg)
                                     ))));
                    return searchdoc;
                }
                else
                {
                    try
                    {
                        try
                        {
                            bookingrefno = response.Descendants("UniversalRecord").FirstOrDefault().Attribute("LocatorCode").Value;
                            // will be used to cancel universal record
                        }
                        catch { }
                        try
                        {
                            resLocatorCode = response.Descendants("AirReservation").FirstOrDefault().Attribute("LocatorCode").Value;
                            // will be used in ticketing
                        }
                        catch { }
                        try
                        {
                            provresLocatorCode = response.Descendants("UniversalRecord").FirstOrDefault().Descendants("ProviderReservationInfo").FirstOrDefault().Attribute("LocatorCode").Value;
                        }
                        catch { }
                        try
                        {
                            supLocatorCode = response.Descendants("AirReservation").FirstOrDefault().Descendants("SupplierLocator").FirstOrDefault().Attribute("SupplierLocatorCode").Value;
                        }
                        catch { }
                        try
                        {
                            pricinginfolst = pricinginfobind(response.Descendants("AirPricingInfo").ToList());
                        }
                        catch { }
                        if (response.Descendants("UniversalRecord").FirstOrDefault().Attribute("Status").Value == "Active")
                        {
                            status = "CONFIRMED";
                        }
                        else
                        {
                            status = response.Descendants("UniversalRecord").FirstOrDefault().Attribute("Status").Value;
                        }
                        success = "true";
                        if (bookingrefno != "")
                        {
                            #region Ticketing
                            try
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    if(i==1)
                                    {
                                        pricinginfolst = null;
                                    }
                                    gal_ticket objgal = new gal_ticket();
                                    #region create ticketrequest
                                    XElement tktreq = tktrequest_gal(commonrequest, bookingrefno, resLocatorCode, pricinginfolst);
                                    #endregion
                                    XElement tktresponse = objgal.ticketing_response(tktreq);
                                    int tktcount = tktresponse.Descendants("Flights").Descendants("TicketingDetails").ToList().Count;
                                    if (tktcount > 0)
                                    {
                                        XElement resdoc = new XElement(
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
                                                     new XElement("BookingResponse",
                                                         tktresponse.Descendants("Flights").FirstOrDefault()
                                        ))));
                                        return resdoc;
                                    }
                                }
                            }
                            catch { }
                            #endregion
                        }
                        else
                        {
                            XElement errsearchdoc = new XElement(
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
                                              new XElement("BookingResponse",
                                                  new XElement("ErrorTxt", "Booking Failed. Please try again")
                                     ))));
                            return errsearchdoc;
                        }
                    }
                    catch { }
                    XElement searchdoc = new XElement(
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
                                          new XElement("BookingResponse",
                                              new XElement("Flights",
                                                  new XAttribute("success", success),
                                                  new XAttribute("status", status),
                                                  new XAttribute("bookingrefno", bookingrefno),
                                                  new XAttribute("tickettimelimit", ""),
                                                  new XAttribute("resLocatorCode", resLocatorCode),
                                                  new XAttribute("provresLocatorCode", provresLocatorCode),
                                                  new XAttribute("supLocatorCode", supLocatorCode),
                                                  new XElement("PriceInfoList", pricinginfolst)
                                              )
                             ))));
                    return searchdoc;
                }
            }
            catch (Exception ex)
            {
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                string username = commonrequest.Descendants("UserName").FirstOrDefault().Value;
                string password = commonrequest.Descendants("Password").FirstOrDefault().Value;
                string AgentID = commonrequest.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = commonrequest.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = commonrequest.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = commonrequest.Descendants("BookingRequest");
                XElement searchdoc = new XElement(
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
                                              new XElement("BookingResponse",
                                                  new XElement("ErrorTxt", ex.Message)
                                     ))));
                return searchdoc;
            }
        }
        #endregion
        #region Pricing Info List (Respone)
        private List<XElement> pricinginfobind(List<XElement> pricelst)
        {
            try
            {
                List<XElement> prclst = new List<XElement>();
                for (int i = 0; i < pricelst.Count(); i++)
                {
                    prclst.Add(new XElement("PriceInfo",
                               new XAttribute("Key", Convert.ToString(pricelst[i].Attribute("Key").Value)),
                               new XAttribute("Type", Convert.ToString(pricelst[i].Descendants("PassengerType").FirstOrDefault().Attribute("Code").Value) == "CNN" ? "CHD" : Convert.ToString(pricelst[i].Descendants("PassengerType").FirstOrDefault().Attribute("Code").Value))
                               )
                        );
                }
                return prclst;
            }
            catch { return null; }
        }
        #endregion
        #region Book Request
        private XElement book_request(XElement req)
        {
            #region Book Request (GAL)
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace ns = "http://www.travelport.com/schema/air_v42_0";
            XNamespace ns1 = "http://www.travelport.com/schema/common_v42_0";
            XNamespace ns3 = "http://www.travelport.com/schema/universal_v42_0";
            XElement prgalreq = null; //XElement.Load(HttpContext.Current.Server.MapPath(@"~\prebookres_gal.xml"));
            prcheck_res prchkobj = new prcheck_res();
            DataTable dt = prchkobj.getpricecheckres_gal(req.Descendants("BookingRequest").FirstOrDefault().Attribute("TransID").Value);
            if(dt!=null)
            {
                if(dt.Rows.Count>0)
                {
                    string pricres = dt.Rows[0]["logresponseXML"].ToString();
                    prgalreq = XElement.Parse(pricres);
                }
            }
            XElement prdocreq = RemoveAllNamespaces(prgalreq);
            try
            {
                carrier = prdocreq.Descendants("AirSegment").FirstOrDefault().Attribute("Carrier").Value;
            }
            catch { }
            string totalamt = prdocreq.Descendants("AirPricingSolution").FirstOrDefault().Attribute("TotalPrice").Value;
            XElement airpricesolution = galpricesolution(prdocreq);
            #region Request
            XElement common_request = new XElement(soap + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                        new XElement(soap + "Body",
                                        new XElement(ns3 + "AirCreateReservationReq",
                                            new XAttribute("TraceId", "sk123"),
                                            new XAttribute("AuthorizedBy", "User"),
                                            new XAttribute("TargetBranch", "P7109079"),
                                            new XAttribute("ProviderCode", "1G"),
                                            new XAttribute("RetainReservation", "Both"),
                                            new XElement(ns1 + "BillingPointOfSaleInfo",
                                                new XAttribute("OriginApplication", "uAPI")),
                                                gal_searchrequestpax(commonrequest.Descendants("Passenger").ToList()),
                                                new XElement(ns1 + "FormOfPayment",
                                                new XAttribute("Type", "Check"),
                                                 new XAttribute("Key", "1"),
                                                  new XElement(ns1 + "Check",
                                                     new XAttribute("RoutingNumber", "456"),
                                                     new XAttribute("AccountNumber", "7890"),
                                                     new XAttribute("CheckNumber", "1234567"))),
                                                airpricesolution,                                                
                                                 new XElement(ns1 + "ActionStatus",
                                                 new XAttribute("Type", "ACTIVE"),
                                                  new XAttribute("TicketDate", "T*"),
                                                  new XAttribute("ProviderCode", "1G")),
                                                new XElement(ns1 + "Payment",
                                                new XAttribute("Key", "2"),
                                                 new XAttribute("Type", "Itinerary"),
                                                  new XAttribute("FormOfPaymentRef", "1"),
                                                  new XAttribute("Amount", totalamt))
                                                            )));
            return common_request;
            #endregion
            #endregion
        }
        private List<XElement> gal_searchrequestpax(List<XElement> passengers)
        {
            XNamespace ns1 = "http://www.travelport.com/schema/common_v42_0";
            List<XElement> passengerlst = new List<XElement>();
            int adtpaxindex = 1;
            int chdpaxindex = 1;
            int infpaxindex = 1;
            for (int i = 0; i < passengers.Count(); i++)
            {
                string travelertype = string.Empty;
                string passkey = string.Empty;
                if (passengers[i].Attribute("type").Value == "CHD")
                {
                    travelertype = "CNN";
                    passkey = "ingeniumchd" + chdpaxindex + 1;
                    chdpaxindex++;
                }
                else if (passengers[i].Attribute("type").Value == "INF")
                {
                    travelertype = "INF";
                    passkey = "ingeniuminf" + infpaxindex + 1;
                    infpaxindex++;
                }
                else
                {
                    travelertype = "ADT";
                    passkey = "ingeniumadt" + adtpaxindex + 1;
                    adtpaxindex++;
                }
                string freetxt = string.Empty;
                try
                {
                    freetxt = "P/" + passengers[i].Attribute("nationality").Value.ToUpper() + "/000005678/" + passengers[i].Attribute("nationality").Value.ToUpper() + "/" + DateTime.ParseExact(passengers[i].Attribute("dob").Value, "dd/MM/yyyy", null).ToString("ddMMMyy").ToUpper() + "/" + passengers[i].Attribute("gender").Value.ToUpper() + "/" + DateTime.ParseExact(passengers[i].Attribute("pexpirtydate").Value, "dd/MM/yyyy", null).ToString("ddMMMyy").ToUpper() + "/" + passengers[i].Attribute("lastname").Value.ToUpper() + "/" + passengers[i].Attribute("firstname").Value.ToUpper() + passengers[i].Attribute("title").Value.ToUpper();
                }
                catch { freetxt = "T*"; }
                passengerlst.Add(new XElement(ns1 + "BookingTraveler",
                                    new XAttribute("Key", passkey),
                                    new XAttribute("TravelerType", travelertype),
                                    new XAttribute("Age", calcage(commonrequest.Descendants("DepartDate").FirstOrDefault().Value, passengers[i].Attribute("dob").Value)),
                                    new XAttribute("DOB", convertdate(passengers[i].Attribute("dob").Value)),
                                    new XAttribute("Gender", passengers[i].Attribute("gender").Value),
                                    new XAttribute("Nationality", passengers[i].Attribute("nationality").Value),
                                    new XElement(ns1 + "BookingTravelerName",
                                        new XAttribute("Prefix", passengers[i].Attribute("title").Value),
                                        new XAttribute("First", passengers[i].Attribute("firstname").Value + " " + passengers[i].Attribute("middlename").Value),
                                        new XAttribute("Last", passengers[i].Attribute("lastname").Value)),
                                        new XElement(ns1 + "DeliveryInfo",
                                            new XElement(ns1 + "ShippingAddress",
                                            new XAttribute("Key", passkey),
                                            new XElement(ns1 + "Street", passengers[i].Attribute("nationality").Value),
                                            new XElement(ns1 + "City", passengers[i].Attribute("nationality").Value),
                                            new XElement(ns1 + "State", ""),
                                            new XElement(ns1 + "PostalCode", "00000"),
                                            new XElement(ns1 + "Country", passengers[i].Attribute("nationality").Value))),
                                    new XElement(ns1 + "PhoneNumber",
                                        new XAttribute("Location", ""),
                                        new XAttribute("CountryCode", ""),
                                        new XAttribute("AreaCode", ""),
                                        new XAttribute("Number", passengers[i].Attribute("phoneno").Value)),
                                    new XElement(ns1 + "Email",
                                        new XAttribute("EmailID", passengers[i].Attribute("emailid").Value)),
                                        new XElement(ns1 + "SSR",
                                                new XAttribute("Type", "DOCS"),
                                                 new XAttribute("FreeText", freetxt),
                                                  new XAttribute("Carrier", carrier)),
                                        new XElement(ns1 + "Address",
                                            new XElement(ns1 + "AddressName", ""),
                                            new XElement(ns1 + "Street", passengers[i].Attribute("nationality").Value),
                                            new XElement(ns1 + "City", passengers[i].Attribute("nationality").Value),
                                            new XElement(ns1 + "State", ""),
                                            new XElement(ns1 + "PostalCode", "00000"),
                                            new XElement(ns1 + "Country", passengers[i].Attribute("nationality").Value))
                                    ));
            }
            return passengerlst;
        }
        private string convertdate(string date)
        {
            try
            {
                DateTime dt = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                return dt.ToString("yyyy-MM-dd");
            }
            catch
            {
                return null;
            }
        }
        private long calcage(string journeydt, string dob)
        {
            DateTime Startdate = DateTime.ParseExact(journeydt, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime EndDate = DateTime.ParseExact(dob, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            long age = 0;
            System.TimeSpan ts = new TimeSpan(Startdate.Ticks - EndDate.Ticks);
            age = (long)(ts.Days / 365);
            return age;
        }
        private XElement galpricesolution(XElement pricsol)
        {
            try
            {
                XNamespace ns = "http://www.travelport.com/schema/air_v42_0";
                XNamespace ns1 = "http://www.travelport.com/schema/common_v42_0";
                List<XElement> airseg = pricsol.Descendants("AirSegment").ToList();
                XElement airpricsol = pricsol.Descendants("AirPricingSolution").FirstOrDefault();
                XElement airpricinfo = pricsol.Descendants("AirPricingInfo").FirstOrDefault();
                airpricinfo.AddBeforeSelf(airseg);
                int airsegref = airpricsol.Descendants("AirSegmentRef").Count();
                if (airpricsol.Elements("OptionalServices").ToList().Count > 0)
                {
                    airpricsol.Elements("OptionalServices").Remove();
                }
                if (airsegref > 0)
                {
                    airpricsol.Descendants("AirSegmentRef").Remove();
                }
                int adtpaxindex = 1;
                int chdpaxindex = 1;
                int infpaxindex = 1;
                foreach (XElement e in airpricsol.DescendantsAndSelf())
                {
                    if (e.Name.LocalName == "PassengerType")
                    {
                        string passkey = string.Empty;
                        if (e.Attribute("Code").Value == "CNN")
                        {
                            passkey = "ingeniumchd" + chdpaxindex + 1;
                            chdpaxindex++;
                        }
                        else if (e.Attribute("Code").Value == "INF")
                        {
                            passkey = "ingeniuminf" + infpaxindex + 1;
                            infpaxindex++;
                        }
                        else
                        {
                            passkey = "ingeniumadt" + adtpaxindex + 1;
                            adtpaxindex++;
                        }
                        e.Add(new XAttribute("BookingTravelerRef", passkey));
                    }
                    if (e.Name.LocalName == "HostToken")
                    {
                        e.Name = ns1 + e.Name.LocalName;
                    }
                    else if (e.Name.LocalName == "ServiceData")
                    {
                        e.Name = ns1 + e.Name.LocalName;
                    }
                    else if (e.Name.LocalName == "ServiceInfo")
                    {
                        e.Name = ns1 + e.Name.LocalName;
                    }
                    else if (e.Name.LocalName == "Description")
                    {
                        e.Name = ns1 + e.Name.LocalName;
                    }
                    else if (e.Name.LocalName == "MediaItem")
                    {
                        e.Name = ns1 + e.Name.LocalName;
                    }
                    else if (e.Name.LocalName == "SeatAttributes")
                    {
                        e.Name = ns1 + e.Name.LocalName;
                    }
                    else if (e.Name.LocalName == "SeatAttribute")
                    {
                        e.Name = ns1 + e.Name.LocalName;
                    }
                    else if (e.Name.LocalName == "CabinClass")
                    {
                        e.Name = ns1 + e.Name.LocalName;
                    }
                    else if (e.Name.LocalName == "Remark")
                    {
                        e.Name = ns1 + e.Name.LocalName;
                    }  
                    else
                    {
                        e.Name = ns + e.Name.LocalName;
                    }
                }
                return airpricsol;
            }
            catch { return null; }
        }
        #endregion
        #region Tkt Request
        private XElement tktrequest_gal(XElement req, string suprefno, string pnrno, List<XElement> airpriceinfo)
        {
            try
            {
                int triptype = Convert.ToInt32(req.Descendants("BookingRequest").Descendants("Itinerary").FirstOrDefault().Attribute("indextype") == null ? 1 : Convert.ToInt32(req.Descendants("BookingRequest").Descendants("Itinerary").FirstOrDefault().Attribute("indextype").Value));
                string triptypeid = "1";
                string type = "OneWay";
                string indextype = "1";
                if (triptype == 2)
                {
                    triptypeid = "2";
                    type = "Roundtrip";
                    indextype = "2";
                }
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement bookingRequest =
                    new XElement(soapenv + "Envelope",
                            new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                            new XElement(soapenv + "Header",
                             new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                             new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travayoo"), new XElement("Password", "ingflt@tech"), new XElement("ServiceType", "FLT_001"), new XElement("ServiceVersion", "v1.0"))),
                       new XElement(soapenv + "Body",
                       new XElement("ticketRequest",
                           new XAttribute("TransID", req.Descendants("BookingRequest").FirstOrDefault().Attribute("TransID").Value),
                           new XAttribute("CustomerID", req.Descendants("BookingRequest").FirstOrDefault().Attribute("CustomerID").Value),
                           new XAttribute("Ip", ""),
                            new XElement("SuppliersList",
                            new XElement("supplierdetail",
                               new XAttribute("supplierid", "50"),
                               new XAttribute("username", "INGEN_XML"),
                               new XAttribute("password", "INGEN2017_xml"),
                               new XAttribute("accountno", "MCN001486"), "Test"
                               )),
                        new XElement("Itinerary", new XAttribute("type", type), new XAttribute("code", ""), new XAttribute("indextype", indextype),
                            new XElement("Origin", req.Descendants("Origin").FirstOrDefault().Value),
                             new XElement("OriginCity", req.Descendants("Origin").FirstOrDefault().Value),
                            new XElement("Destination", req.Descendants("Destination").FirstOrDefault().Value),
                            new XElement("DestinationCity", req.Descendants("Destination").FirstOrDefault().Value),
                            new XElement("DepartDate", req.Descendants("DepartDate").FirstOrDefault().Value),
                            new XElement("ReturnDate", req.Descendants("ReturnDate").FirstOrDefault().Value),
                            new XElement("Class", ""),
                            new XElement("ClassCode", ""),
                            new XElement("faresoucecode", ""),
                            new XElement("SessionId", ""),
                             new XElement("bookingrefno", suprefno),
                             new XElement("resLocatorCode", pnrno),
                             new XElement("PriceInfoList", airpriceinfo),
                             new XElement("FareType", ""),
                              new XElement("IsPassportMandatory", ""),
                               new XElement("IsRefundable", ""),
                               new XElement("TicketType", "")
                            ),
                       new XElement("StaffUser", ""),
                       new XElement("AgentId", ""),
                       new XElement("CurrencyID", "1"),
                       new XElement("language", "ENG"),
                       new XElement("CurrencyCode", "")

                )));
                return bookingRequest;
            }
            catch { return null; }
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
        ~gal_book()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion
}