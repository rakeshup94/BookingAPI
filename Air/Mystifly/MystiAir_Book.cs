using System;
using System.Collections.Generic;
using System.Data;
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
    public class MystiAir_Book
    {
        #region Travayoo Booking of Air (Mystifly)
        XElement travayoorequest = null;
        public DataTable dtconversionrate = null;
        public DataTable dtmasacurrency = null;
        public DataTable dtmarkup = null;
        string mamarkuptype = string.Empty;
        string samarkuptype = string.Empty;
        decimal mamarkupval = 0;
        decimal samarkupval = 0;
        decimal maconversion = 0;
        decimal saconversion = 0;
        string agentcurrency = string.Empty;
        public XElement Airbooking_mysti(XElement req)
        {
            #region Air Booking
            travayoorequest = req;
            XElement responseout = null;
            #region Travayoo Header
            string username = req.Descendants("UserName").FirstOrDefault().Value;
            string password = req.Descendants("Password").FirstOrDefault().Value;
            string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
            string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
            IEnumerable<XElement> request = req.Descendants("BookingRequest");
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            #endregion
            try
            {                
                string url = string.Empty;
                string response = string.Empty;
                string bookingnote = string.Empty;
                XElement suppliercred = airsupplier_Cred.getgds_credentials(req.Descendants("BookingRequest").Attributes("CustomerID").FirstOrDefault().Value, "12");
                url = suppliercred.Descendants("URL").FirstOrDefault().Value;
                string bookmethod = suppliercred.Descendants("AirBook").FirstOrDefault().Value;
                string tktordermethod = suppliercred.Descendants("AirTicketOrder").FirstOrDefault().Value;
                string tktngmethod = suppliercred.Descendants("AirTicketing").FirstOrDefault().Value;
                string Target = suppliercred.Descendants("Mode").FirstOrDefault().Value;
                string sessionid = suppliercred.Descendants("sessionid").FirstOrDefault().Value;
                string booknotemethod = suppliercred.Descendants("AddBookingNotes").FirstOrDefault().Value;
                Mysti_SupplierResponse sup_response = new Mysti_SupplierResponse();
                string customerid = string.Empty;
                string trackno = string.Empty;
                customerid = req.Descendants("BookingRequest").Attributes("CustomerID").FirstOrDefault().Value;
                trackno = req.Descendants("BookingRequest").Attributes("TransID").FirstOrDefault().Value;
                string apireq = apirequest(req, Target,sessionid);
                #region Get Data from DB
                getcurrencymrkup getcurrencymrk = new getcurrencymrkup();
                List<DataTable> dtconversion = getcurrencymrk.getcurrencyConversion(Convert.ToInt64(req.Descendants("AgentId").FirstOrDefault().Value), "SAAS");
                dtconversionrate = dtconversion[0];
                dtmasacurrency = dtconversion[1];
                dtmarkup = getcurrencymrk.getmarkupdetails(Convert.ToInt64(req.Descendants("AgentId").FirstOrDefault().Value), "12", "SAAS");
                #endregion
                response = sup_response.supplierresponse_mystifly(url, apireq, bookmethod, "AirBook", 5, trackno, customerid).ToString();
                XElement availrsponse = XElement.Parse(response.ToString());
                XElement doc = RemoveAllNamespaces(availrsponse);
                string success = string.Empty;
                success = doc.Descendants("Success").FirstOrDefault().Value.Trim().ToString();
                if (success == "true")
                {
                    string ticketingrequest = string.Empty;
                    string ticketresponse = string.Empty;
                    string status = string.Empty;
                    string bookingrefno = string.Empty;
                    string tickettimelimit = string.Empty;
                    //string sessionid = string.Empty;
                    status = doc.Descendants("Status").FirstOrDefault().Value.Trim().ToString();
                    bookingrefno = doc.Descendants("UniqueID").FirstOrDefault().Value.Trim().ToString();
                    tickettimelimit = doc.Descendants("TktTimeLimit").FirstOrDefault().Value.Trim().ToString();
                    #region if status confirmed
                    if (status == "CONFIRMED")
                    {
                        #region If booking already exists
                        int bookingexist = doc.Descendants("Error").Descendants("Message").Count();
                        if (bookingexist == 1)
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
                                                             new XElement("BookingResponse",
                                                                 new XElement("ErrorTxt",
                                                                     doc.Descendants("Error").Descendants("Message").FirstOrDefault().Value
                                                                     )
                                                    ))));
                            return responseout;
                        }
                        #endregion
                        #region Start Ticketing process
                        if (1 == 2)
                        {
                            #region LCC Flow
                            #region Ticketing Request
                            ticketingrequest = ticketrequest(bookingrefno, Target, sessionid);
                            #endregion
                            #region Ticketing Response
                            //response = searchresponse(url, ticketingrequest, "Mystifly.OnePoint/OnePoint/TripDetails").ToString();
                            return responseout;
                            #endregion
                            #endregion
                        }
                        else
                        {                            
                            #region NON LCC Flow
                            int chkor = 0;
                            string tktorderstatus = string.Empty;
                            #region LCC Check
                            try
                            {
                                string faretypetxt = req.Descendants("BookingRequest").Descendants("FareType").FirstOrDefault().Value;
                                if (faretypetxt == "3")
                                {
                                    chkor = 1;
                                }
                            }
                            catch { }
                            #endregion
                            if (chkor == 0)
                            {
                                #region Order Ticketing Request
                                ticketingrequest = orderticketrequest(bookingrefno, Target, sessionid);
                                #endregion
                                #region Check Ticket Order
                                check_tktorder chkord = new check_tktorder();
                                chkor = chkord.check_tktordering(trackno);
                                #endregion
                            }
                            #region Order Ticketing Response
                            try
                            {
                                if (chkor == 0)
                                {
                                    response = sup_response.supplierresponse_mystifly(url, ticketingrequest, tktordermethod, "AirTicketOrder", 11, trackno, customerid).ToString();
                                    XElement availrsponsetktorder = XElement.Parse(response.ToString());
                                    XElement doctktorder = RemoveAllNamespaces(availrsponsetktorder);
                                    tktorderstatus = doctktorder.Descendants("Success").FirstOrDefault().Value.Trim().ToString();
                                }
                                else
                                {
                                    tktorderstatus = "true";
                                }
                            }
                            catch { tktorderstatus = "true"; }
                            //#region Fare Rules
                            XElement farerules = null;
                            //try
                            //{
                            //    string fareapireq = fareapirequest(req);
                            //    string fareresponse = sup_response.supplierresponse_mystifly(url, fareapireq, "Mystifly.OnePoint/OnePoint/FareRules1_1", "AirFareRules", 13, trackno, customerid).ToString();
                            //    XElement fareavailrsponse = XElement.Parse(fareresponse.ToString());
                            //    XElement faredoc = RemoveAllNamespaces(fareavailrsponse);
                            //    farerules = farerulebind(faredoc);
                            //}
                            //catch { }
                            //#endregion
                            #region Booking Note
                            try
                            {
                                string buknotsts = string.Empty;
                                string specreqtext = string.Empty;
                                int checktxt = 0;
                                try
                                {
                                    checktxt = req.Descendants("SpecialRequest").Count();
                                }
                                catch { }
                                if (checktxt > 0)
                                {
                                    string ssrtxt = req.Descendants("SpecialRequest").FirstOrDefault().Value;
                                    if (ssrtxt.Trim() != "")
                                    {
                                        string booknoterequest = specialbooknote_request(bookingrefno, Target, sessionid, ssrtxt);
                                        string ssrresponse = sup_response.supplierresponse_mystifly(url, booknoterequest, booknotemethod, "AddBookingNotes", 5, trackno, customerid).ToString();
                                        //XElement booknote = XElement.Parse(ssrresponse.ToString());
                                        //XElement booknotresp = RemoveAllNamespaces(booknote);
                                        //buknotsts = booknotresp.Descendants("Success").FirstOrDefault().Value.Trim();
                                        //if (buknotsts.ToString() == "true")
                                        //{
                                        //    try
                                        //    {
                                        //        bookingnote = docttripdetres.Descendants("BookingNotes").FirstOrDefault().Value;
                                        //    }
                                        //    catch { }
                                        //}
                                    }
                                }
                            }
                            catch { }
                            #endregion
                            if (tktorderstatus == "true")
                            {
                                #region Ticketing Request
                                ticketingrequest = ticketrequest(bookingrefno, Target, sessionid);
                                #endregion
                                #region Ticketing Response (Trip Details)
                                response = sup_response.supplierresponse_mystifly(url, ticketingrequest, tktngmethod, "AirTicketing", 12, trackno, customerid).ToString();
                                //string test = string.Empty;
                                //XElement ddd = XElement.Load(HttpContext.Current.Server.MapPath(@"~\bookresponse.xml"));                                
                                string tripdetailstatus = string.Empty;
                                XElement tripdetailsres = XElement.Parse(response.ToString());
                                XElement docttripdetres = RemoveAllNamespaces(tripdetailsres);
                                tripdetailstatus = docttripdetres.Descendants("Success").FirstOrDefault().Value.Trim().ToString();                               
                                #region if ticket order status is true
                                if (tktorderstatus == "true")
                                {
                                    responseout = ticketresponsebinding(req, docttripdetres, farerules);                                    
                                    return responseout;
                                }
                                #endregion
                                #region if order status false
                                else
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
                                              new XElement("BookingResponse",
                                                  new XElement("Flights",
                                                      new XAttribute("success", success),
                                                      new XAttribute("status", status),
                                                      new XAttribute("bookingrefno", bookingrefno),
                                                      new XAttribute("tickettimelimit", tickettimelimit),
                                                      farerules
                                                      )
                                     ))));
                                    return responseout;
                                }
                                #endregion
                                #endregion
                            }
                            else
                            {
                                #region Ticketing Order Failed
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
                                  new XElement("BookingResponse",
                                      new XElement("Flights",
                                          new XAttribute("success", success),
                                          new XAttribute("status", status),
                                          new XAttribute("bookingrefno", bookingrefno),
                                          new XAttribute("tickettimelimit", tickettimelimit),
                                          farerules
                                          )
                         ))));
                                return responseout;
                                #endregion
                            }
                            #endregion
                            #endregion
                        }
                        #endregion
                    }
                    #endregion
                    #region status not confirmed
                    else
                    {
                        int bookingexist = doc.Descendants("Error").Descendants("Message").Count();
                        if (bookingexist == 1)
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
                                      new XElement("BookingResponse",
                                          new XElement("Flights",
                                              new XAttribute("success", success),
                                              new XAttribute("status", status),
                                              new XAttribute("bookingrefno", bookingrefno),
                                              new XAttribute("tickettimelimit", tickettimelimit)
                                              ),
                                              new XElement("ErrorTxt",
                                                                     doc.Descendants("Error").Descendants("Message").FirstOrDefault().Value
                                                                     )
                             ))));
                            return responseout;
                        }
                        else
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
                                      new XElement("BookingResponse",
                                          new XElement("Flights",
                                              new XAttribute("success", success),
                                              new XAttribute("status", status),
                                              new XAttribute("bookingrefno", bookingrefno),
                                              new XAttribute("tickettimelimit", tickettimelimit)
                                              )
                             ))));
                            return responseout;
                        }
                    }
                    #endregion
                }
                else
                {
                    #region Booking Failed
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
                                  new XElement("BookingResponse",
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
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "Airbooking_mysti";
                ex1.PageName = "MystiAir_Book";
                ex1.CustomerID = req.Descendants("BookingRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("BookingRequest").Attributes("TransID").FirstOrDefault().Value;
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
        #region Ticket response binding
        public XElement ticketresponsebinding(XElement req, XElement tktresponse,XElement farerules)
        {
            string username = req.Descendants("UserName").FirstOrDefault().Value;
            string password = req.Descendants("Password").FirstOrDefault().Value;
            string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
            string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
            IEnumerable<XElement> request = req.Descendants("BookingRequest");
            string success = string.Empty;
            string status = string.Empty;
            string bookingrefno = string.Empty;
            string ticketstatus = string.Empty;
            string trip = string.Empty;
            string totalamt = string.Empty;
            string supamount = string.Empty;
            string maconversionrt = string.Empty;
            string saconversionrt = string.Empty;
            string sellingamt = string.Empty;
            string masellingamt = string.Empty;
            string mamarkup = string.Empty;
            string samarkup = string.Empty;
            string currency = string.Empty;
            string faretype = string.Empty;
            string totalduration = string.Empty;
            List<XElement> custlst = null;
            List<XElement> fltsgmtlst = null;
            List<XElement> prclst = null;
            try
            {
                success = tktresponse.Descendants("Success").FirstOrDefault().Value.Trim().ToString();
                status = tktresponse.Descendants("BookingStatus").FirstOrDefault().Value.Trim().ToString();
                bookingrefno = tktresponse.Descendants("TravelItinerary").FirstOrDefault().Descendants("UniqueID").LastOrDefault().Value.Trim().ToString();
                if (bookingrefno == null)
                {
                    if (bookingrefno == "")
                    {
                        bookingrefno = tktresponse.Descendants("UniqueID").FirstOrDefault().Value.Trim().ToString();
                    }
                }
                //ticketstatus = tktresponse.Descendants("TicketStatus").FirstOrDefault().Value.Trim().ToString();
                //if (ticketstatus == "")
                {
                    ticketstatus = tktresponse.Descendants("TicketStatus").LastOrDefault().Value.Trim().ToString();
                }
                if (status=="")
                {
                    status = ticketstatus;
                }
                trip = req.Descendants("Itinerary").Attributes("type").FirstOrDefault().Value.Trim().ToString();
                custlst = tktresponse.Descendants("CustomerInfo").ToList();
                fltsgmtlst = tktresponse.Descendants("ReservationItem").ToList();
                XElement totamt = tktresponse.Descendants("ItineraryPricing").Descendants("TotalFare").FirstOrDefault();
                totalamt = totamt.Descendants("Amount").FirstOrDefault().Value.Trim().ToString();
                currency = totamt.Descendants("CurrencyCode").FirstOrDefault().Value.Trim().ToString();
                agentcurrency = dtmasacurrency.Rows[0]["SAcrncy"].ToString();
                supamount = totalamt;
                #region currency conversion
                try
                {
                    mamarkuptype = dtmarkup.Rows[0]["MainAgentMarkupType"].ToString();
                    samarkuptype = dtmarkup.Rows[0]["SubAgentMrkupType"].ToString();
                    mamarkupval = Convert.ToDecimal(dtmarkup.Rows[0]["MainAgentMrkupVal"].ToString());
                    samarkupval = Convert.ToDecimal(dtmarkup.Rows[0]["SubAgentMrkupVal"].ToString());
                    DataRow[] row = dtconversionrate.Select("crncyCode = " + "'" + currency + "'");
                    maconversion = Convert.ToDecimal(row[0].ItemArray[1]);
                    saconversion = Convert.ToDecimal(row[0].ItemArray[2]);
                    #region Conversion and markup
                    try
                    {
                        sellingamt = Convert.ToString(convertedamt(Convert.ToDecimal(totalamt)));
                        masellingamt = Convert.ToString(maconvertedamt(Convert.ToDecimal(totalamt)));
                        mamarkup = Convert.ToString(calculatemamarkup(Convert.ToDecimal(totalamt)));
                        samarkup = Convert.ToString(calculatesamarkup(Convert.ToDecimal(totalamt)));
                        #region Get Total Amount
                        prclst = pricebreakup(tktresponse.Descendants("TripDetailsPTC_FareBreakdown").ToList());
                        decimal btotalamt = 0;
                        decimal mabtotalamt = 0;
                        foreach (XElement prc in prclst)
                        {
                            if (prc.Element("BaseFares").Name == "BaseFares")
                            {
                                btotalamt += Convert.ToDecimal(prc.Descendants("BaseFare").FirstOrDefault().Value);
                                mabtotalamt += Convert.ToDecimal(prc.Descendants("maBaseFare").FirstOrDefault().Value);
                            }
                            if (prc.Element("Surchares").Name == "Surchares")
                            {
                                btotalamt += Convert.ToDecimal(prc.Descendants("Surcharge").Descendants("Amount").Sum(nd => Decimal.Parse(nd.Value)));
                                mabtotalamt += Convert.ToDecimal(prc.Descendants("Surcharge").Descendants("maAmount").Sum(nd => Decimal.Parse(nd.Value)));
                            }
                            if (prc.Element("Taxes").Name == "Taxes")
                            {
                                btotalamt += Convert.ToDecimal(prc.Descendants("Tax").Descendants("Amount").Sum(nd => Decimal.Parse(nd.Value)));
                                mabtotalamt += Convert.ToDecimal(prc.Descendants("Tax").Descendants("maAmount").Sum(nd => Decimal.Parse(nd.Value)));
                            }
                            btotalamt = btotalamt * Convert.ToInt32(prc.Descendants("PQty").FirstOrDefault().Value);
                            mabtotalamt = mabtotalamt * Convert.ToInt32(prc.Descendants("PQty").FirstOrDefault().Value);
                        }
                        sellingamt = Convert.ToString(btotalamt);
                        masellingamt = Convert.ToString(mabtotalamt);
                        #endregion
                    }
                    catch { }
                    #endregion
                }
                catch { }
                #endregion
                faretype = tktresponse.Descendants("FareType").FirstOrDefault().Value.Trim().ToString();
            }
            catch { }
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
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
                                          new XElement("TicketingDetails",
                                              new XElement("TicketInfo",
                                                 new XElement("success", success),
                                                 new XElement("status", ticketstatus),
                                                 new XElement("customerdetails",
                                                     new XElement("customerinfo", customerlistbind(custlst))
                                                     ),
                                                     new XElement("ItineraryDetails",
                                                        new XAttribute("trip", trip),
                                                         new XAttribute("supamount", supamount),
                                                           new XAttribute("maconversionrt", maconversion),
                                                           new XAttribute("saconversionrt", saconversion),
                                                        new XAttribute("amount", sellingamt),
                                                        new XAttribute("maamount", masellingamt),
                                                          new XAttribute("mamarkup", mamarkup),
                                                            new XAttribute("samarkup", samarkup),
                                                        new XAttribute("currencycode", agentcurrency),
                                                        new XAttribute("faretype", faretype),
                                                        new XAttribute("totalduration", totalduration),
                                                        new XElement("FlightSegments", flightsegments(fltsgmtlst)),
                                                        new XElement("PriceBreakups", prclst)
                                                         )
                                                  )
                                              ), farerules
                                          )
                         ))));
            return searchdoc;
        }
        #endregion
        #region Flight Trip Details Binding (E Ticket)
        public List<XElement> flightsegments(List<XElement> flights)
        {
            List<XElement> flightlst = new List<XElement>();
            try
            {
                for (int i = 0; i < flights.Count(); i++)
                {
                    try
                    {
                        string from = string.Empty;
                        string to = string.Empty;
                        string Marketingairlinecode = string.Empty;
                        string operatingairlinecode = string.Empty;
                        string equipment = string.Empty;
                        string airlinenumber = string.Empty;
                        string cabin = string.Empty;
                        string departdatetime = string.Empty;
                        string arrivaldatetime = string.Empty;
                        string pnr = string.Empty;
                        string JourneyDuration = string.Empty;
                        string baggage = string.Empty;
                        string depterminal = string.Empty;
                        string arrterminal = string.Empty;
                        from = flights[i].Descendants("DepartureAirportLocationCode").FirstOrDefault().Value.Trim().ToString();
                        to = flights[i].Descendants("ArrivalAirportLocationCode").FirstOrDefault().Value.Trim().ToString();
                        depterminal = flights[i].Descendants("DepartureTerminal").FirstOrDefault().Value.Trim().ToString();
                        arrterminal = flights[i].Descendants("ArrivalTerminal").FirstOrDefault().Value.Trim().ToString();
                        Marketingairlinecode = flights[i].Descendants("MarketingAirlineCode").FirstOrDefault().Value.Trim().ToString();
                        airlinenumber = flights[i].Descendants("FlightNumber").FirstOrDefault().Value.Trim().ToString();
                        cabin = flights[i].Descendants("CabinClassText").FirstOrDefault().Value.Trim().ToString();
                        pnr = flights[i].Descendants("AirlinePNR").FirstOrDefault().Value.Trim().ToString();
                        JourneyDuration = flights[i].Descendants("JourneyDuration").FirstOrDefault().Value.Trim().ToString();
                        baggage = flights[i].Descendants("Baggage").FirstOrDefault().Value.Trim().ToString();
                        try
                        {
                            operatingairlinecode = flights[i].Descendants("OperatingAirlineCode").FirstOrDefault().Value.Trim().ToString();
                            equipment = flights[i].Descendants("AirEquipmentType").FirstOrDefault().Value.Trim().ToString();
                        }
                        catch { }
                        try
                        {
                            departdatetime = flights[i].Descendants("DepartureDateTime").FirstOrDefault().Value.Trim().ToString();
                            arrivaldatetime = flights[i].Descendants("ArrivalDateTime").FirstOrDefault().Value.Trim().ToString();
                        }
                        catch { }
                        flightlst.Add(new XElement("Flight",
                            new XAttribute("from", from),
                             new XAttribute("to", to),
                              new XAttribute("depterminal", depterminal),
                              new XAttribute("arrterminal", arrterminal),
                              new XAttribute("pnr", pnr),
                               new XAttribute("baggage", baggage),
                                new XAttribute("Marketingairlinecode", Marketingairlinecode),
                                 new XAttribute("Operatingairlinecode", operatingairlinecode),
                                  new XAttribute("Equipment", equipment),
                                   new XAttribute("airlinenumber", airlinenumber),
                                   new XAttribute("cabin", cabin),
                                   new XAttribute("departdatetime", departdatetime),
                                   new XAttribute("arrivaldatetime", arrivaldatetime),
                                   new XAttribute("duration", JourneyDuration),
                                   new XAttribute("durationtype", "minutes")
                            )
                            );
                    }
                    catch { }
                }
            }
            catch { }
            return flightlst;
        }
        #endregion
        #region Price Breakups
        public List<XElement> pricebreakup(List<XElement> pricebrkups)
        {
            try
            {
                List<XElement> prcbrk = new List<XElement>();
                for (int i = 0; i < pricebrkups.Count(); i++)
                {
                    #region Conversion and markup
                    decimal sellingamtbf = 0;
                    decimal sellingamtot = 0;
                    decimal masellingamtbf = 0;
                    decimal masellingamtot = 0;
                    try
                    {
                        sellingamtbf = convertedamt(Convert.ToDecimal(pricebrkups[i].Descendants("EquiFare").Descendants("Amount").FirstOrDefault().Value.Trim().ToString()));
                        sellingamtot = convertedamt(Convert.ToDecimal(pricebrkups[i].Descendants("TotalFare").Descendants("Amount").FirstOrDefault().Value.Trim().ToString()));
                        masellingamtbf = maconvertedamt(Convert.ToDecimal(pricebrkups[i].Descendants("EquiFare").Descendants("Amount").FirstOrDefault().Value.Trim().ToString()));
                        masellingamtot = maconvertedamt(Convert.ToDecimal(pricebrkups[i].Descendants("TotalFare").Descendants("Amount").FirstOrDefault().Value.Trim().ToString()));
                    }
                    catch { }
                    #endregion
                    List<XElement> taxlst = pricebrkups[i].Descendants("Tax").ToList();
                    prcbrk.Add(new XElement("PriceBreakup",
                               new XElement("PType", Convert.ToString(pricebrkups[i].Descendants("PassengerTypeQuantity").Descendants("Code").FirstOrDefault().Value.Trim().ToString())),
                               new XElement("PQty", Convert.ToString(pricebrkups[i].Descendants("PassengerTypeQuantity").Descendants("Quantity").FirstOrDefault().Value.Trim().ToString())),
                               new XElement("BaseFares",
                               new XElement("BaseFare", sellingamtbf),
                               new XElement("maBaseFare", masellingamtbf),
                               new XElement("Currency", agentcurrency)
                               ),
                               //new XElement("Surchares", Convert.ToString("")),
                                new XElement("Surchares", surcharges(pricebrkups[i].Descendants("Surcharge").ToList(), null)),
                               new XElement("Taxes", taxes(taxlst)),
                               new XElement("TotalFares",
                                   new XElement("Amount", sellingamtot),
                                   new XElement("maAmount", masellingamtot),
                                   new XElement("Currency", agentcurrency)
                                   )
                               )
                        );
                }
                return prcbrk;
            }
            catch { return null; }
        }
        #region Taxes
        public List<XElement> taxes(List<XElement> taxes)
        {
            try
            {
                List<XElement> taxbrkup = new List<XElement>();
                for (int i = 0; i < taxes.Count(); i++)
                {
                    #region Conversion and markup
                    decimal sellingamttax = 0;
                    decimal masellingamttax = 0;
                    try
                    {
                        sellingamttax = convertedamt(Convert.ToDecimal(taxes[i].Descendants("Amount").FirstOrDefault().Value.Trim().ToString()));
                        masellingamttax = maconvertedamt(Convert.ToDecimal(taxes[i].Descendants("Amount").FirstOrDefault().Value.Trim().ToString()));
                    }
                    catch { }
                    #endregion
                    taxbrkup.Add(new XElement("Tax",
                               new XElement("Currency", agentcurrency),
                               new XElement("Amount", sellingamttax),
                               new XElement("maAmount", masellingamttax)
                               )
                        );
                }
                return taxbrkup;
            }
            catch { return null; }
        }
        public List<XElement> surcharges(List<XElement> surchargs, string currency)
        {
            try
            {
                List<XElement> taxbrkup = new List<XElement>();
                for (int i = 0; i < surchargs.Count(); i++)
                {
                    #region Conversion and markup
                    decimal sellingamtsrv = 0;
                    decimal masellingamtsrv = 0;
                    try
                    {
                        sellingamtsrv = convertedamt(Convert.ToDecimal(surchargs[i].Descendants("Amount").FirstOrDefault().Value));
                        masellingamtsrv = convertedamt(Convert.ToDecimal(surchargs[i].Descendants("Amount").FirstOrDefault().Value));
                    }
                    catch { }
                    #endregion
                    taxbrkup.Add(new XElement("Surcharge",
                               new XElement("Type", Convert.ToString(surchargs[i].Descendants("Type").FirstOrDefault().Value)),
                               new XElement("Currency", agentcurrency),
                               new XElement("Amount", sellingamtsrv),
                               new XElement("maAmount", masellingamtsrv)
                               )
                        );
                }
                return taxbrkup;
            }
            catch { return null; }
        }
        #endregion
        #region currency conversion/markup
        private decimal convertedamt(decimal amount)
        {
            decimal finalamt = 0;
            try
            {
                decimal macustprc = 0;
                decimal sacustprc = 0;
                decimal mabuyrate = maconversion * Convert.ToDecimal(amount);
                if (mamarkuptype == "1")
                {
                    macustprc = (mabuyrate * mamarkupval / 100) + mabuyrate;
                }
                else
                {
                    macustprc = mamarkupval + mabuyrate;
                }
                sacustprc = macustprc * Convert.ToDecimal(saconversion);
                if (samarkuptype == "1")
                {
                    decimal agntprc = (sacustprc * samarkupval / 100) + sacustprc;
                    finalamt = Math.Round(agntprc, 2);
                }
                else
                {
                    decimal agntprc = samarkupval + sacustprc;
                    finalamt = Math.Round(agntprc, 2);
                }
            }
            catch { }
            return finalamt;
        }
        private decimal maconvertedamt(decimal amount)
        {
            decimal finalamt = 0;
            try
            {
                decimal macustprc = 0;
                decimal mabuyrate = maconversion * Convert.ToDecimal(amount);
                if (mamarkuptype == "1")
                {
                    macustprc = (mabuyrate * mamarkupval / 100) + mabuyrate;
                }
                else
                {
                    macustprc = mamarkupval + mabuyrate;
                }
                finalamt = Math.Round(macustprc, 2);
            }
            catch { }
            return finalamt;
        }
        private decimal calculatemamarkup(decimal amount)
        {
            decimal finalamt = 0;
            try
            {
                decimal macustprc = 0;
                decimal mabuyrate = maconversion * Convert.ToDecimal(amount);
                if (mamarkuptype == "1")
                {
                    macustprc = (mabuyrate * mamarkupval / 100);
                }
                else
                {
                    macustprc = mamarkupval;
                }
                finalamt = Math.Round(macustprc, 2);
            }
            catch { }
            return finalamt;
        }
        private decimal calculatesamarkup(decimal amount)
        {
            decimal finalamt = 0;
            try
            {
                decimal macustprc = 0;
                decimal sacustprc = 0;
                decimal mabuyrate = maconversion * Convert.ToDecimal(amount);
                if (mamarkuptype == "1")
                {
                    macustprc = (mabuyrate * mamarkupval / 100) + mabuyrate;
                }
                else
                {
                    macustprc = mamarkupval + mabuyrate;
                }
                sacustprc = macustprc * Convert.ToDecimal(saconversion);
                if (samarkuptype == "1")
                {
                    decimal agntprc = (sacustprc * samarkupval / 100);
                    finalamt = Math.Round(agntprc, 2);
                }
                else
                {
                    decimal agntprc = samarkupval;
                    finalamt = Math.Round(agntprc, 2);
                }
            }
            catch { }
            return finalamt;
        }
        #endregion
        #endregion
        #region Bind Customer List
        public List<XElement> customerlistbind(List<XElement> custlst)
        {
            try
            {
                List<XElement> customers = new List<XElement>();
                for (int i = 0; i < custlst.Count(); i++)
                {
                    customers.Add(new XElement("customer",
                               new XAttribute("type", Convert.ToString(custlst[i].Descendants("PassengerType").FirstOrDefault().Value)),
                               new XAttribute("gender", Convert.ToString(custlst[i].Descendants("Gender").FirstOrDefault().Value)),
                               new XAttribute("title", Convert.ToString(custlst[i].Descendants("PaxName").Descendants("PassengerTitle").FirstOrDefault().Value)),
                               new XAttribute("firstname", Convert.ToString(custlst[i].Descendants("PaxName").Descendants("PassengerFirstName").FirstOrDefault().Value)),
                               new XAttribute("lastname", Convert.ToString(custlst[i].Descendants("PaxName").Descendants("PassengerLastName").FirstOrDefault().Value)),
                               new XAttribute("dob", Convert.ToString(custlst[i].Descendants("DateOfBirth").FirstOrDefault().Value)),
                               new XAttribute("nationality", Convert.ToString(custlst[i].Descendants("PassengerNationality").FirstOrDefault().Value)),
                               new XAttribute("passportno", Convert.ToString(custlst[i].Descendants("PassportNumber").FirstOrDefault().Value)),
                               new XAttribute("pexpirtydate", Convert.ToString(custlst[i].Descendants("PassportExpiresOn").FirstOrDefault().Value)),
                               new XAttribute("pissuingcountry", Convert.ToString(custlst[i].Descendants("PassportIssuanceCountry").FirstOrDefault().Value)),
                               new XAttribute("mealpreference", Convert.ToString(custlst[i].Descendants("MealPreference").FirstOrDefault().Value)),
                               new XAttribute("seatpreference", Convert.ToString(custlst[i].Descendants("SeatPreference").FirstOrDefault().Value)),
                               new XAttribute("emailid", Convert.ToString(custlst[i].Descendants("EmailAddress").FirstOrDefault().Value)),
                               new XAttribute("phoneno", Convert.ToString(custlst[i].Descendants("PhoneNumber").FirstOrDefault().Value)),
                               new XAttribute("postalcode", Convert.ToString(custlst[i].Descendants("PostCode").FirstOrDefault().Value)),
                               new XAttribute("ffn", Convert.ToString(custlst[i].Descendants("KnownTravelerNo").FirstOrDefault().Value)),
                               new XAttribute("ffnairlinecode", Convert.ToString("")),
                               new XAttribute("eTicketNumber", Convert.ToString(custlst[i].Descendants("eTicketNumber").FirstOrDefault() != null ? custlst[i].Descendants("eTicketNumber").FirstOrDefault().Value : ""))
                               )
                        );
                }
                return customers;
            }
            catch { return null; }
        }
        #endregion
        #region API Request
        public string apirequest(XElement req, string mode,string sessionid)
        {
            string faresourcecode = string.Empty;
            //string sessionid = string.Empty;
            string phcountrycode = string.Empty;
            string emailid = string.Empty;
            string phoneno = string.Empty;
            string postcode = string.Empty;
            manage_session session_mgmt = new manage_session();
            //sessionid = session_mgmt.session_manage(req.Descendants("BookingRequest").Attributes("CustomerID").FirstOrDefault().Value, req.Descendants("BookingRequest").Attributes("TransID").FirstOrDefault().Value);
            List<XElement> passengerslst = req.Descendants("Passenger").ToList();
            faresourcecode = req.Descendants("faresoucecode").FirstOrDefault().Value;
            phcountrycode = req.Descendants("Passenger").FirstOrDefault().Attribute("phcountrycode").Value;
            emailid = req.Descendants("Passenger").FirstOrDefault().Attribute("emailid").Value;
            phoneno = req.Descendants("Passenger").FirstOrDefault().Attribute("phoneno").Value;
            postcode = req.Descendants("Passenger").FirstOrDefault().Attribute("postalcode").Value;
            string reqresp = travellers(passengerslst);
            string freq = "<mys1:AirTravelers>" + reqresp + "</mys1:AirTravelers>";
            string airtravelerslst = freq.ToString();
            string request = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:mys='Mystifly.OnePoint' xmlns:mys1='http://schemas.datacontract.org/2004/07/Mystifly.OnePoint' xmlns:mys2='Mystifly.OnePoint.OnePointEntities' xmlns:arr='http://schemas.microsoft.com/2003/10/Serialization/Arrays'>" +
                              "<soapenv:Header/>" +
                              "<soapenv:Body>" +
                                "<mys:BookFlight>" +
                                  "<mys:rq>" +
                                    "<mys1:FareSourceCode>" + faresourcecode + "</mys1:FareSourceCode>" +
                                    "<mys1:SessionId>" + sessionid + "</mys1:SessionId>" +
                                    "<mys1:Target>" + mode + "</mys1:Target>" +
                                    "<mys1:TravelerInfo>" +
                                        airtravelerslst +
                                      "<mys1:AreaCode></mys1:AreaCode>" +
                                      "<mys1:CountryCode>" + phcountrycode + "</mys1:CountryCode>" +
                                      "<mys1:Email>" + emailid + "</mys1:Email>" +
                                      "<mys1:PhoneNumber>" + phoneno + "</mys1:PhoneNumber>" +
                                      "<mys1:PostCode>" + postcode + "</mys1:PostCode>" +
                                    "</mys1:TravelerInfo>" +
                                  "</mys:rq>" +
                                "</mys:BookFlight>" +
                              "</soapenv:Body>" +
                            "</soapenv:Envelope>";
            return request;
        }
        public List<XElement> reqtravellers(List<XElement> travellerslst)
        {
            try
            {
                List<XElement> trvlst = new List<XElement>();
                for (int i = 0; i < travellerslst.Count(); i++)
                {
                    string dob = string.Empty;
                    DateTime depdateTime = DateTime.ParseExact(travellerslst[i].Attribute("dob").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    dob = depdateTime.ToString("yyyy-MM-dd");
                    string gender = string.Empty;
                    string lastname = string.Empty;
                    string title = string.Empty;
                    string passengernationality = string.Empty;
                    string passengertype = string.Empty;
                    string passissuingcntry = string.Empty;
                    string passexpirydate = string.Empty;
                    string passportno = string.Empty;
                    string mealtype = string.Empty;
                    string seattype = string.Empty;
                    string ffn = string.Empty;

                    passissuingcntry = travellerslst[i].Attribute("pissuingcountry").Value;
                    DateTime passexpirydt = DateTime.ParseExact(travellerslst[i].Attribute("pexpirtydate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    passexpirydate = passexpirydt.ToString("yyyy-MM-dd");
                    passportno = travellerslst[i].Attribute("passportno").Value;
                    passengertype = travellerslst[i].Attribute("type").Value;
                    passengernationality = travellerslst[i].Attribute("nationality").Value;

                    mealtype = travellerslst[i].Attribute("mealpreference").Value;
                    seattype = travellerslst[i].Attribute("seatpreference").Value;

                    string firstmiddlename = travellerslst[i].Attribute("firstname").Value + " " + travellerslst[i].Attribute("middlename").Value;
                    lastname = travellerslst[i].Attribute("lastname").Value;
                    title = travellerslst[i].Attribute("title").Value;
                    XNamespace ns = "http://url/for/mys1";
                    trvlst.Add(new XElement("container",
                    new XAttribute(XNamespace.Xmlns + "mys1", ns),

                        new XElement(ns + "AirTraveler",
                        new XElement(ns + "DateOfBirth", dob + "T00:00:00"),
                         new XElement(ns + "Gender", gender),
                         new XElement(ns + "PassengerName",
                             new XElement(ns + "PassengerFirstName", firstmiddlename),
                             new XElement(ns + "PassengerLastName", lastname),
                             new XElement(ns + "PassengerTitle", title)),
                          new XElement(ns + "PassengerNationality", passengernationality),
                           new XElement(ns + "PassengerType", passengertype),
                           new XElement(ns + "Passport",
                             new XElement(ns + "Country", passissuingcntry),
                             new XElement(ns + "ExpiryDate", passexpirydate + "T00:00:00"),
                             new XElement(ns + "PassportNumber", passportno)),
                           new XElement(ns + "SpecialServiceRequest",
                             new XElement(ns + "MealPreference", mealtype),
                             new XElement(ns + "SeatPreference", seattype))
                           )));
                }
                return trvlst;
            }
            catch { return null; }
        }
        public string travellers(List<XElement> travellerslst)
        {
            try
            {
                string trvpsgrs = string.Empty;
                for (int i = 0; i < travellerslst.Count(); i++)
                {
                    string dob = string.Empty;
                    DateTime depdateTime = DateTime.ParseExact(travellerslst[i].Attribute("dob").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    dob = depdateTime.ToString("yyyy-MM-dd");
                    string gender = string.Empty;
                    string lastname = string.Empty;
                    string title = string.Empty;
                    string passengernationality = string.Empty;
                    string passengertype = string.Empty;
                    string passissuingcntry = string.Empty;
                    string passexpirydate = string.Empty;
                    string passportno = string.Empty;
                    string mealtype = string.Empty;
                    string seattype = string.Empty;
                    string ffn = string.Empty;

                    passissuingcntry = travellerslst[i].Attribute("pissuingcountry").Value;
                    DateTime passexpirydt = DateTime.ParseExact(travellerslst[i].Attribute("pexpirtydate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    passexpirydate = passexpirydt.ToString("yyyy-MM-dd");
                    passportno = travellerslst[i].Attribute("passportno").Value;
                    passengertype = travellerslst[i].Attribute("type").Value;
                    passengernationality = travellerslst[i].Attribute("nationality").Value;

                    mealtype = travellerslst[i].Attribute("mealpreference").Value;
                    seattype = travellerslst[i].Attribute("seatpreference").Value;

                    string firstmiddlename = travellerslst[i].Attribute("firstname").Value + " " + travellerslst[i].Attribute("middlename").Value;
                    lastname = travellerslst[i].Attribute("lastname").Value;
                    title = travellerslst[i].Attribute("title").Value;
                    gender = travellerslst[i].Attribute("gender").Value;
                    try
                    {
                        ffn = travellerslst[i].Attribute("ffnairlinecode").Value + travellerslst[i].Attribute("ffn").Value;
                    }
                    catch { }
                    string extrabag = string.Empty;
                    try
                    {
                        string faretypetxt = travayoorequest.Descendants("BookingRequest").Descendants("FareType").FirstOrDefault().Value;
                        if (faretypetxt == "3")
                        {
                            extrabag = extra_baggage_service(travellerslst[i].Descendants("Baggage").ToList());
                        }                        
                    }
                    catch { }
                    trvpsgrs = trvpsgrs + "<mys1:AirTraveler>" +
                              "<mys1:DateOfBirth>" + dob + "T00:00:00</mys1:DateOfBirth>" +
                              extrabag +
                              "<mys1:FrequentFlyerNumber>" + ffn + "</mys1:FrequentFlyerNumber>" +
                              "<mys1:Gender>" + gender + "</mys1:Gender>" +
                              "<mys1:PassengerName>" +
                                "<mys1:PassengerFirstName>" + firstmiddlename + "</mys1:PassengerFirstName>" +
                                "<mys1:PassengerLastName>" + lastname + "</mys1:PassengerLastName>" +
                                "<mys1:PassengerTitle>" + title + "</mys1:PassengerTitle>" +
                              "</mys1:PassengerName>" +
                              "<mys1:PassengerNationality>" + passengernationality + "</mys1:PassengerNationality>" +
                              "<mys1:PassengerType>" + passengertype + "</mys1:PassengerType>" +
                              "<mys1:Passport>" +
                                "<mys1:Country>" + passissuingcntry + "</mys1:Country>" +
                                "<mys1:ExpiryDate>" + passexpirydate + "T00:00:00</mys1:ExpiryDate>" +
                                "<mys1:PassportNumber>" + passportno + "</mys1:PassportNumber>" +
                              "</mys1:Passport>" +
                              "<mys1:SpecialServiceRequest>" +
                                "<mys1:MealPreference>" + mealtype + "</mys1:MealPreference>" +
                                "<mys1:SeatPreference>" + seattype + "</mys1:SeatPreference>" +
                              "</mys1:SpecialServiceRequest>" +
                            "</mys1:AirTraveler>";
                }
                return trvpsgrs;
            }
            catch { return ""; }
        }
        private string extra_baggage_service(List<XElement> extrabag)
        {
            try
            {
                string trvpsgrs = string.Empty;
                string bagtrvpsgrs = string.Empty;
                int j = 0;
                for (int i = 0; i < extrabag.Count(); i++)
                {
                    j = 1;
                    trvpsgrs = trvpsgrs + "<mys2:Services>" +
                              "<mys2:ExtraServiceId>" + Convert.ToString(extrabag[i].Attribute("ServiceId").Value) + "</mys2:ExtraServiceId>" +                             
                            "</mys2:Services>";
                }
                if(j==1)
                {
                    bagtrvpsgrs = "<mys1:ExtraServices1_1>" + trvpsgrs + "</mys1:ExtraServices1_1>";
                }
                return bagtrvpsgrs;
            }
            catch { return ""; }
        }
        #endregion
        #region Ticketing Request (Supplier)
        public string ticketrequest(string uniqueid, string mode,string sessionid)
        {
            string request = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:mys='Mystifly.OnePoint' xmlns:mys1='http://schemas.datacontract.org/2004/07/Mystifly.OnePoint'>" +
                              "<soapenv:Header/>" +
                              "<soapenv:Body>" +
                                 "<mys:TripDetails>" +
                                    "<mys:rq>" +
                                       "<mys1:SessionId>" + sessionid + "</mys1:SessionId>" +
                                       "<mys1:Target>" + mode + "</mys1:Target>" +
                                       "<mys1:UniqueID>" + uniqueid + "</mys1:UniqueID>" +
                                    "</mys:rq>" +
                                 "</mys:TripDetails>" +
                              "</soapenv:Body>" +
                           "</soapenv:Envelope>";
            return request;
        }
        #endregion
        #region Order Ticketing Request (Supplier)
        public string orderticketrequest(string uniqueid,string mode,string sessionid)
        {
            manage_session session_mgmt = new manage_session();
            //string sessionid = session_mgmt.session_manage(travayoorequest.Descendants("BookingRequest").Attributes("CustomerID").FirstOrDefault().Value, travayoorequest.Descendants("BookingRequest").Attributes("TransID").FirstOrDefault().Value);
            string request = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:mys='Mystifly.OnePoint' xmlns:mys1='http://schemas.datacontract.org/2004/07/Mystifly.OnePoint'>" +
                               "<soapenv:Header/>" +
                               "<soapenv:Body>" +
                                  "<mys:TicketOrder>" +
                                     "<mys:rq>" +
                                        "<mys1:ConversationId>Urgent</mys1:ConversationId>" +
                                        "<mys1:SessionId>" + sessionid + "</mys1:SessionId>" +
                                        "<mys1:Target>" + mode + "</mys1:Target>" +
                                        "<mys1:UniqueID>" + uniqueid + "</mys1:UniqueID>" +
                                     "</mys:rq>" +
                                  "</mys:TicketOrder>" +
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
        #region Fare Rules Binding
        public XElement farerulebind(XElement farerules)
        {
            try
            {
                List<XElement> baglst = farerules.Descendants("BaggageInfo").ToList();
                List<XElement> farelst = farerules.Descendants("FareRule").ToList();
                List<XElement> baggagelst = new List<XElement>();
                List<XElement> farerulelst = new List<XElement>();
                if (baglst.Count() > 0)
                {
                    baggagelst = baggagebind(baglst);
                }
                if (farelst.Count() > 0)
                {
                    farerulelst = faredetailbind(farelst);
                }
                XElement responsedoc = new XElement(
                    new XElement("FareRulesResult",
                        baggagelst,
                        farerulelst
                        ));
                return responsedoc;
            }
            catch { return null; }
        }
        #endregion
        #region Baggage Binding
        public List<XElement> baggagebind(List<XElement> baggagelist)
        {
            try
            {
                List<XElement> baggagelst = new List<XElement>();
                for (int i = 0; i < baggagelist.Count(); i++)
                {
                    baggagelst.Add(new XElement("BaggageInfo",
                               new XElement("Departure", Convert.ToString(baggagelist[i].Descendants("Departure").FirstOrDefault().Value)),
                               new XElement("Arrival", Convert.ToString(baggagelist[i].Descendants("Arrival").FirstOrDefault().Value)),
                               new XElement("Baggage", Convert.ToString(baggagelist[i].Descendants("Baggage").FirstOrDefault().Value)),
                               new XElement("FlightNo", Convert.ToString(baggagelist[i].Descendants("FlightNo").FirstOrDefault().Value))
                               )
                        );
                }
                return baggagelst;
            }
            catch { return null; }
        }
        #endregion
        #region Fare Details Binding
        public List<XElement> faredetailbind(List<XElement> fareruleslst)
        {
            try
            {
                List<XElement> faredetlst = new List<XElement>();
                for (int i = 0; i < fareruleslst.Count(); i++)
                {
                    List<XElement> rulelst = fareruleslst[i].Descendants("RuleDetail").ToList();
                    faredetlst.Add(new XElement("FareRule",
                               new XElement("Airline", Convert.ToString(fareruleslst[i].Descendants("Airline").FirstOrDefault().Value)),
                               new XElement("City", Convert.ToString(fareruleslst[i].Descendants("CityPair").FirstOrDefault().Value)),
                               faredetails(rulelst)
                               )
                        );
                }
                return faredetlst;
            }
            catch { return null; }
        }
        #endregion
        #region Fare Details Binding
        public List<XElement> faredetails(List<XElement> faredetailslst)
        {
            try
            {
                List<XElement> faredetlst = new List<XElement>();
                for (int i = 0; i < faredetailslst.Count(); i++)
                {
                    faredetlst.Add(new XElement("RuleDetails",
                                   new XAttribute("title", Convert.ToString(faredetailslst[i].Descendants("Category").FirstOrDefault().Value)),
                                   Convert.ToString(faredetailslst[i].Descendants("Rules").FirstOrDefault().Value))
                        );
                }
                return faredetlst;
            }
            catch { return null; }
        }
        #endregion
        #region Fare Rule Request
        public string fareapirequest(XElement req,string sessionid)
        {
            string faresourcecode = string.Empty;
            //string sessionid = string.Empty;
            string mode = string.Empty;
            mode = "Test";
            faresourcecode = req.Descendants("faresoucecode").FirstOrDefault().Value;
            manage_session session_mgmt = new manage_session();
            //sessionid = session_mgmt.session_manage(req.Descendants("BookingRequest").Attributes("CustomerID").FirstOrDefault().Value, req.Descendants("BookingRequest").Attributes("TransID").FirstOrDefault().Value);

            string request = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:mys='Mystifly.OnePoint' xmlns:mys1='http://schemas.datacontract.org/2004/07/Mystifly.OnePoint.AirRules1_1'>" +
                              "<soapenv:Header/>" +
                              "<soapenv:Body>" +
                                "<mys:FareRules1_1>" +
                                  "<mys:rq>" +
                                    "<mys1:FareSourceCode>" + faresourcecode + "</mys1:FareSourceCode>" +
                                    "<mys1:SessionId>" + sessionid + "</mys1:SessionId>" +
                                    "<mys1:Target>" + mode + "</mys1:Target>" +
                                  "</mys:rq>" +
                                "</mys:FareRules1_1>" +
                              "</soapenv:Body>" +
                            "</soapenv:Envelope>";
            return request;
        }
        #endregion
        #region Special Booking Note Request
        public string specialbooknote_request(string uniqueid, string mode, string sessionid,string specialtxt)
        {
            string request = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:mys='Mystifly.OnePoint' xmlns:mys1='http://schemas.datacontract.org/2004/07/Mystifly.OnePoint' xmlns:arr='http://schemas.microsoft.com/2003/10/Serialization/Arrays'>" +
                              "<soapenv:Header/>" +
                              "<soapenv:Body>" +
                                 "<mys:AddBookingNotes>" +
                                    "<mys:rq>" +
                                        "<mys1:Notes>"+
                                          "<arr:string>" + specialtxt + "</arr:string>" +
                                        "</mys1:Notes>"+
                                       "<mys1:SessionId>" + sessionid + "</mys1:SessionId>" +
                                       "<mys1:Target>" + mode + "</mys1:Target>" +
                                       "<mys1:UniqueID>" + uniqueid + "</mys1:UniqueID>" +
                                    "</mys:rq>" +
                                 "</mys:AddBookingNotes>" +
                              "</soapenv:Body>" +
                           "</soapenv:Envelope>";
            return request;
        }
        #endregion
    }
}