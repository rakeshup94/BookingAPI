using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Models.Galileo;
using TravillioXMLOutService.Air.Models.TBO;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.TBO
{
    public class tbo_book : IDisposable
    {
        #region Staticdata
        public string countrycode = string.Empty;
        public string countryname = string.Empty;
        public XElement airlinexml;
        public XElement airportxml;
        public XElement commonrequest = null;
        tboair_supresponse api_resp;
        #endregion
        #region Book Response (TBO)
        public XElement book_response(XElement req)
        {
            #region Book Response (TBO)
            XElement travayoo_out = null;
            commonrequest = req;
            try
            {
                api_resp = new tboair_supresponse();
                string bookreq = fltbookrequest(req).ToString();
                if (bookreq != "")
                {
                    #region Booking
                    string api_response = api_resp.tbo_supresponse(req, bookreq, "AirBook", 5, req.Descendants("BookingRequest").FirstOrDefault().Attribute("TransID").Value, req.Descendants("BookingRequest").FirstOrDefault().Attribute("CustomerID").Value, null);
                    var supresxml = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.ASCII.GetBytes(api_response), new XmlDictionaryReaderQuotas()));
                    XElement bookresp = XElement.Parse(supresxml.ToString());
                    travayoo_out = apibook_response(bookresp);
                    return travayoo_out;
                    #endregion
                }
                else
                {
                    #region Invalid Booking Request
                    try
                    {
                        APILogDetail logexc = new APILogDetail();
                        logexc.customerID = Convert.ToInt64(req.Descendants("BookingRequest").FirstOrDefault().Attribute("CustomerID").Value);
                        logexc.TrackNumber = req.Descendants("BookingRequest").FirstOrDefault().Attribute("TransID").Value;
                        logexc.LogTypeID = 5;
                        logexc.LogType = "AirBook";
                        logexc.SupplierID = 51;
                        logexc.logrequestXML = req.ToString();
                        logexc.logresponseXML = "";
                        logexc.StartTime = DateTime.UtcNow; 
                        logexc.EndTime = DateTime.UtcNow;
                        SaveAPILog savelogexc = new SaveAPILog();
                        savelogexc.SaveAPILogsflt(logexc);
                    }
                    catch { }
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
                                              "Invalid Booking Request."
                                              )
                             ))));
                    return responseout;
                    #endregion
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "book_response";
                ex1.PageName = "tbo_book";
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
        #region Book Response (Travayoo)
        private XElement apibook_response(XElement response)
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
                string success = "false";
                string status = "Failed";
                string bookingrefno = string.Empty;
                if (response.Descendants("Errors").Descendants("UserMessage").Count() > 0)
                {
                    string errormsg = string.Empty;

                    try
                    {
                        errormsg = response.Descendants("UserMessage").FirstOrDefault().Value;
                    }
                    catch { errormsg = "Unexpected error occurs."; }

                    XElement respdoc = new XElement(
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
                    return respdoc;
                }
                else
                {
                    try
                    {
                        string supstatus = response.Descendants("Status").FirstOrDefault().Value;
                        if (supstatus == "0")
                        {
                            status = "Failed";
                        }
                        else if (supstatus == "1")
                        {
                            status = "CONFIRMED";
                        }
                        else if (supstatus == "2")
                        {
                            status = "Failed";
                        }
                        else if (supstatus == "3")
                        {
                            status = "OtherFare";
                        }
                        else if (supstatus == "4")
                        {
                            status = "OtherClass";
                        }
                        else if (supstatus == "5")
                        {
                            status = "BookedOther";
                        }
                        else if (supstatus == "6")
                        {
                            status = "NotConfirmed";
                        }
                        else
                        {
                            status = "Failed";
                        }
                        success = response.Descendants("IsSuccess").LastOrDefault().Value;
                    }
                    catch { }
                    try
                    {
                        int checkbookid = response.Descendants("BookingId").Count();
                        if (checkbookid > 0)
                        {
                            bookingrefno = response.Descendants("BookingId").FirstOrDefault().Value;
                        }
                    }
                    catch { }
                    if (success.ToUpper() != "FALSE" && status.ToUpper() != "FAILED")
                    {
                        XElement respdoc = new XElement(
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
                                                          new XAttribute("tickettimelimit", "")
                                                      )
                                     ))));
                        return respdoc;
                    }
                    else
                    {
                        XElement respdoc = new XElement(
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
                                                      new XElement("ErrorTxt", "Booking Failed."
                                          )
                                    ))));
                        return respdoc;
                    }
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "book_response";
                ex1.PageName = "tbo_book";
                ex1.CustomerID = commonrequest.Descendants("BookingRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = commonrequest.Descendants("BookingRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                string username = commonrequest.Descendants("UserName").FirstOrDefault().Value;
                string password = commonrequest.Descendants("Password").FirstOrDefault().Value;
                string AgentID = commonrequest.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = commonrequest.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = commonrequest.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = commonrequest.Descendants("BookingRequest");
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
        }
        #endregion
        #region TBO Air Search Request
        private string fltbookrequest(XElement req)
        {
            string request = string.Empty;
            string prtboreq = string.Empty;
            try
            {
                prcheck_res prchkobj = new prcheck_res();
                DataTable dt = prchkobj.getpricecheckres_tbo(req.Descendants("BookingRequest").FirstOrDefault().Attribute("TransID").Value, req.Descendants("BookingRequest").FirstOrDefault().Descendants("preID").FirstOrDefault().Value);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        prtboreq = dt.Rows[0]["logresponseXML"].ToString();
                    }
                }
                try
                {
                    airportxml = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Flight\Mystifly\airportlist.xml"));
                    XElement record = airportxml.Descendants("record").Where(x => x.Descendants("AirportCode").FirstOrDefault().Value == req.Descendants("BookingRequest").Descendants("Itinerary").Descendants("Origin").FirstOrDefault().Value).FirstOrDefault();
                    countrycode = record.Descendants("countryCode").FirstOrDefault().Value;
                    countryname = record.Descendants("countryName").FirstOrDefault().Value;
                }
                catch { }
            }
            catch { }
            try
            {
                var pricecheckresxml = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.ASCII.GetBytes(prtboreq), new XmlDictionaryReaderQuotas()));
                List<XElement> prebookresponse = pricecheckresxml.Descendants("item").Where(x => x.Attribute("type").Value == "object" && x.Parent.Name == "Result").ToList();
                XElement iternary = req.Descendants("Itinerary").FirstOrDefault();
                tbobookrequest airreq = new tbobookrequest();
                airreq.ResultId = iternary.Descendants("faresoucecode").FirstOrDefault().Value;
                airreq.EndUserBrowserAgent = "Mozilla/5.0(Windows NT 6.1)";
                airreq.PointOfSale = countrycode;
                airreq.RequestOrigin = countryname;
                airreq.UserData = req.Descendants("Passenger").FirstOrDefault().Attribute("title").Value + " " + req.Descendants("Passenger").FirstOrDefault().Attribute("firstname").Value + " " + req.Descendants("Passenger").FirstOrDefault().Attribute("middlename").Value + " " + req.Descendants("Passenger").FirstOrDefault().Attribute("lastname").Value;
                airreq.TokenId = "f360ef32-07fc-4b80-8b86-358fcfb95f61";
                airreq.TrackingId = iternary.Descendants("SessionId").FirstOrDefault().Value;
                airreq.IPAddress = "49.205.173.6";
                airreq.Itinerary = new Itinerary();
                #region Passengers
                airreq.Itinerary.Passenger = new List<Passenger>();
                bool islead = true;
                int index = 0;
                string faretype = string.Empty;
                foreach (XElement passngr in req.Descendants("Passenger").ToList())
                {
                    faretype = Convert.ToString(prebookresponse.Descendants("Fare").Descendants("FareType").FirstOrDefault().Value);
                    string passtype = "1";
                    if (passngr.Attribute("type").Value == "CHD")
                    {
                        passtype = "2";
                    }
                    else if (passngr.Attribute("type").Value == "INF")
                    {
                        passtype = "3";
                    }
                    XElement sngfare = prebookresponse.Descendants("FareBreakdown").FirstOrDefault().Descendants("item").Where(x => x.Element("PassengerType").Value == passtype).FirstOrDefault();
                    int passcount = Convert.ToInt16(sngfare.Descendants("PassengerCount").FirstOrDefault().Value);

                    airreq.Itinerary.Passenger.Add(new Passenger
                    {
                        PassportIssueCountryCode = passngr.Attribute("pissuingcountry").Value,
                        //PassportIssueDate = Convert.ToDateTime(DateTime.ParseExact(passngr.Attribute("pexpirtydate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddT00:00:00", CultureInfo.InvariantCulture)),
                        Title = passngr.Attribute("title").Value,
                        FirstName = passngr.Attribute("firstname").Value + " " + passngr.Attribute("middlename").Value,
                        LastName = passngr.Attribute("lastname").Value,
                        Mobile1 = "91-" +passngr.Attribute("phoneno").Value,
                        Mobile1CountryCode = "91",
                        Mobile2 = null,
                        IsLeadPax = islead,
                        DateOfBirth = Convert.ToDateTime(DateTime.ParseExact(passngr.Attribute("dob").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddT00:00:00", CultureInfo.InvariantCulture)),
                        Type = 0,
                        PassportNo = passngr.Attribute("passportno").Value,
                        PassportExpiry = Convert.ToDateTime(DateTime.ParseExact(passngr.Attribute("pexpirtydate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddT00:00:00", CultureInfo.InvariantCulture)),
                        Nationality = new Nationality
                        {
                            CountryCode = passngr.Attribute("nationality").Value,
                            CountryName = passngr.Attribute("nationality").Value
                        },
                        Country = new Country
                        {
                            CountryCode = passngr.Attribute("nationality").Value,
                            CountryName = passngr.Attribute("nationality").Value
                        },
                        City = null,
                        AddressLine1 = "test address",
                        AddressLine2 = null,
                        Gender = 1,
                        Email = passngr.Attribute("emailid").Value,
                        Meal = passngr.Attribute("mealpreference").Value,
                        Seat = passngr.Attribute("seatpreference").Value,
                        Fare = new Fare
                        {
                            BaseFare = Convert.ToDouble(Convert.ToDecimal(sngfare.Descendants("BaseFare").FirstOrDefault().Value) / passcount),
                            Tax = Convert.ToDouble(Convert.ToDecimal(sngfare.Descendants("Tax").FirstOrDefault().Value) / passcount),
                            OtherCharges = Convert.ToInt32(Convert.ToDecimal(sngfare.Descendants("OtherCharges").FirstOrDefault().Value) / passcount),
                            ServiceFee = Convert.ToInt32(Convert.ToDecimal(sngfare.Descendants("ServiceFee").FirstOrDefault().Value) / passcount),
                            AgentMarkup = Convert.ToInt32(Convert.ToDecimal(sngfare.Descendants("AgentMarkup").FirstOrDefault().Value) / passcount),
                            AgentPreferredCurrency = Convert.ToString(sngfare.Descendants("Currency").FirstOrDefault().Value),
                            FareType = Convert.ToString(prebookresponse.Descendants("Fare").Descendants("FareType").FirstOrDefault().Value),
                            Vat = 0,
                            TotalFare = Convert.ToDouble(Convert.ToDecimal(sngfare.Descendants("TotalFare").FirstOrDefault().Value) / passcount),
                        },
                        FFAirline = passngr.Attribute("ffnairlinecode").Value,
                        FFNumber = passngr.Attribute("ffn").Value,
                        TboAirPaxId = index,
                        PaxBaggage = null,
                        PaxMeal = new List<object>
                    {
                        passngr.Attribute("mealpreference").Value
                    },
                        IDCardNo = null,
                        ZipCode = null,
                        PaxSeat = passngr.Attribute("seatpreference").Value,
                        Ticket = null
                    });
                    islead = false;
                    index++;
                }
                #endregion
                airreq.Itinerary.Segments = new List<Segment>();
                #region Segments
                foreach (XElement segment in prebookresponse.Elements("Segments").Elements("item").Elements("item").ToList())
                {
                    airreq.Itinerary.Segments.Add(new Segment
                    {
                        AccumulatedDuration = segment.Descendants("AccumulatedDuration").FirstOrDefault().Value,
                        AdditionalBaggage = segment.Descendants("AdditionalBaggage").FirstOrDefault().Value,
                        Airline = segment.Descendants("Airline").FirstOrDefault().Value,
                        AirlineDetails = new AirlineDetails
                        {
                            AirlineCode = segment.Descendants("AirlineDetails").Descendants("AirlineCode").FirstOrDefault().Value,
                            AirlineName = segment.Descendants("AirlineDetails").Descendants("AirlineName").FirstOrDefault().Value,
                            Craft = segment.Descendants("AirlineDetails").Descendants("Craft").FirstOrDefault().Value,
                            FlightNumber = segment.Descendants("AirlineDetails").Descendants("FlightNumber").FirstOrDefault().Value,
                            OperatingCarrier = segment.Descendants("AirlineDetails").Descendants("OperatingCarrier").FirstOrDefault().Value
                        },
                        AirlineName = segment.Descendants("Craft").FirstOrDefault().Value,
                        ArrivalTime = Convert.ToDateTime(DateTime.ParseExact(segment.Descendants("ArrivalTime").FirstOrDefault().Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                        BookingClass = segment.Descendants("BookingClass").FirstOrDefault().Value,
                        CabinBaggage = segment.Descendants("CabinBaggage").FirstOrDefault().Value,
                        Craft = segment.Descendants("Craft").FirstOrDefault().Value,
                        DepartureTime = Convert.ToDateTime(DateTime.ParseExact(segment.Descendants("DepartureTime").FirstOrDefault().Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                        Destination = new Destination
                        {
                            AirportCode = segment.Descendants("Destination").Descendants("AirportCode").FirstOrDefault().Value,
                            AirportName = segment.Descendants("Destination").Descendants("AirportName").FirstOrDefault().Value,
                            CityCode = segment.Descendants("Destination").Descendants("CityCode").FirstOrDefault().Value,
                            CityName = segment.Descendants("Destination").Descendants("CityName").FirstOrDefault().Value,
                            CountryCode = segment.Descendants("Destination").Descendants("CountryCode").FirstOrDefault().Value,
                            CountryName = segment.Descendants("Destination").Descendants("CountryName").FirstOrDefault().Value,
                            Terminal = segment.Descendants("Destination").Descendants("Terminal").FirstOrDefault().Value
                        },
                        Duration = segment.Descendants("Duration").FirstOrDefault().Value,
                        ETicketEligible = Convert.ToBoolean(segment.Descendants("ETicketEligible").FirstOrDefault().Value),
                        FlightNumber = segment.Descendants("FlightNumber").FirstOrDefault().Value,
                        GroundTime = segment.Descendants("GroundTime").FirstOrDefault().Value,
                        IncludedBaggage = segment.Descendants("IncludedBaggage").FirstOrDefault().Value,
                        MealType = segment.Descendants("MealType").FirstOrDefault().Value,
                        Mile = Convert.ToInt32(segment.Descendants("Mile").FirstOrDefault().Value),
                        NoOfSeatAvailable = Convert.ToInt32(segment.Descendants("NoOfSeatAvailable").FirstOrDefault().Value),
                        OperatingCarrier = segment.Descendants("OperatingCarrier").FirstOrDefault().Value,
                        Origin = new Origin
                        {
                            AirportCode = segment.Descendants("Origin").Descendants("AirportCode").FirstOrDefault().Value,
                            AirportName = segment.Descendants("Origin").Descendants("AirportName").FirstOrDefault().Value,
                            CityCode = segment.Descendants("Origin").Descendants("CityCode").FirstOrDefault().Value,
                            CityName = segment.Descendants("Origin").Descendants("CityName").FirstOrDefault().Value,
                            CountryCode = segment.Descendants("Origin").Descendants("CountryCode").FirstOrDefault().Value,
                            CountryName = segment.Descendants("Origin").Descendants("CountryName").FirstOrDefault().Value,
                            Terminal = segment.Descendants("Origin").Descendants("Terminal").FirstOrDefault().Value
                        },
                        SegmentIndicator = Convert.ToInt32(segment.Descendants("SegmentIndicator").FirstOrDefault().Value),
                        StopOver = Convert.ToBoolean(segment.Descendants("StopOver").FirstOrDefault().Value),
                        StopPoint = segment.Descendants("StopPoint").FirstOrDefault().Value,
                        StopPointArrivalTime = Convert.ToDateTime(DateTime.ParseExact(segment.Descendants("StopPointArrivalTime").FirstOrDefault().Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                        StopPointDepartureTime = Convert.ToDateTime(DateTime.ParseExact(segment.Descendants("StopPointDepartureTime").FirstOrDefault().Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                        Stops = Convert.ToInt32(segment.Descendants("Stops").FirstOrDefault().Value),
                    });

                }
                #endregion
                airreq.Itinerary.FareRules = new List<FareRule>();
                #region Fare Rule
                foreach (XElement farerule in prebookresponse.Elements("FareRules").Elements("item").ToList())
                {
                    airreq.Itinerary.FareRules.Add(new FareRule
                    {
                        Origin = farerule.Descendants("Origin").FirstOrDefault().Value,
                        Destination = farerule.Descendants("Destination").FirstOrDefault().Value,
                        Airline = farerule.Descendants("Airline").FirstOrDefault().Value,
                        FareRestriction = farerule.Descendants("FareRestriction").FirstOrDefault().Value,
                        FareBasisCode = farerule.Descendants("FareBasisCode").FirstOrDefault().Value,
                        FareRuleDetail = farerule.Descendants("FareRuleDetail").FirstOrDefault().Value,
                        DepartureDate = Convert.ToDateTime(DateTime.ParseExact(farerule.Descendants("DepartureDate").FirstOrDefault().Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                        FlightNumber = farerule.Descendants("FlightNumber").FirstOrDefault().Value,
                    });
                }
                #endregion
                string creatdt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
                airreq.Itinerary.Destination = req.Descendants("Itinerary").FirstOrDefault().Descendants("Destination").FirstOrDefault().Value;
                airreq.Itinerary.Origin = req.Descendants("Itinerary").FirstOrDefault().Descendants("Origin").FirstOrDefault().Value;
                airreq.Itinerary.ValidatingAirlineCode = prebookresponse.Descendants("ValidatingAirline").FirstOrDefault().Value;
                airreq.Itinerary.CreatedOn = Convert.ToDateTime(DateTime.ParseExact(creatdt, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                airreq.Itinerary.FareType = faretype;
                airreq.Itinerary.SearchType = Convert.ToInt16(prebookresponse.Descendants("JourneyType").FirstOrDefault().Value);
                airreq.Itinerary.TravelDate = Convert.ToDateTime(DateTime.ParseExact(prebookresponse.Descendants("DepartureTime").FirstOrDefault().Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                airreq.Itinerary.LastTicketDate = prebookresponse.Descendants("LastTicketDate").FirstOrDefault().Value;
                airreq.Itinerary.NonRefundable = Convert.ToBoolean(prebookresponse.Descendants("NonRefundable").FirstOrDefault().Value);
                airreq.Itinerary.IsLcc = Convert.ToBoolean(prebookresponse.Descendants("IsLcc").FirstOrDefault().Value);
                request = JsonConvert.SerializeObject(airreq);
                return request;
            }
            catch { return ""; }
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
        ~tbo_book()
        {
            Dispose(false);
        }
        #endregion
    }
}