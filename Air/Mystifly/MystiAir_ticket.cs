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
    public class MystiAir_ticket
    {
        #region Ticketing
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
        public XElement ticket_mysti(XElement req)
        {
            #region Ticketing Request
            XElement responseout = null;
            travayoorequest = req;
            try
            {
                Mysti_SupplierResponse sup_response = new Mysti_SupplierResponse();                
                string url = string.Empty;
                string response = string.Empty;
                XElement suppliercred = airsupplier_Cred.getgds_credentials(req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value, "12");
                url = suppliercred.Descendants("URL").FirstOrDefault().Value;
                string method = suppliercred.Descendants("AirTicketing").FirstOrDefault().Value;
                string tktordermethod = suppliercred.Descendants("AirTicketOrder").FirstOrDefault().Value;
                string Target = suppliercred.Descendants("Mode").FirstOrDefault().Value;
                string sessionid = suppliercred.Descendants("sessionid").FirstOrDefault().Value;
                string ticketingrequest = string.Empty;
                string ticketresponse = string.Empty;
                string customerid = string.Empty;
                string trackno = string.Empty;
                customerid = req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value;
                trackno = req.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value;
                #region SessionID
                //manage_session session_mgmt = new manage_session();
                //string sessionid = session_mgmt.session_manage(req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value, req.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value);
                #endregion

                #region Check Ticket Order
                check_tktorder chkord = new check_tktorder();
                int chkor = chkord.check_tktordering(trackno);
                #endregion
                try
                {
                    if (chkor == 0)
                    {
                        #region Order Ticketing 
                        ticketingrequest = orderticketrequest(req.Descendants("bookingrefno").FirstOrDefault().Value, Target, sessionid);
                        response = sup_response.supplierresponse_mystifly(url, ticketingrequest, tktordermethod, "AirTicketOrder", 11, trackno, customerid).ToString();
                        XElement availrsponsetktorder = XElement.Parse(response.ToString());
                        XElement doctktorder = RemoveAllNamespaces(availrsponsetktorder);
                        #endregion
                    }
                }
                catch { }
                ticketingrequest = ticketrequest(sessionid, req.Descendants("bookingrefno").FirstOrDefault().Value, Target);
                #region Ticketing Response (Trip Details)
                response = sup_response.supplierresponse_mystifly(url, ticketingrequest, method, "AirTicketing", 12, trackno, customerid).ToString();
                #region Get Data from DB
                getcurrencymrkup getcurrencymrk = new getcurrencymrkup();
                List<DataTable> dtconversion = getcurrencymrk.getcurrencyConversion(Convert.ToInt64(req.Descendants("AgentId").FirstOrDefault().Value), "SAAS");
                dtconversionrate = dtconversion[0];
                dtmasacurrency = dtconversion[1];
                dtmarkup = getcurrencymrk.getmarkupdetails(Convert.ToInt64(req.Descendants("AgentId").FirstOrDefault().Value), "12", "SAAS");
                #endregion
                XElement tripdetailsres = XElement.Parse(response.ToString());
                XElement docttripdetres = RemoveAllNamespaces(tripdetailsres);
                responseout = ticketresponsebinding(req, docttripdetres);
                return responseout;
                #endregion
            }
            catch(Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "ticket_mysti";
                ex1.PageName = "MystiAir_ticket";
                ex1.CustomerID = req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                string username = req.Descendants("UserName").FirstOrDefault().Value;
                string password = req.Descendants("Password").FirstOrDefault().Value;
                string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = req.Descendants("ticketRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
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
                                  new XElement("ticketResponse",
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
        private XElement ticketresponsebinding(XElement req, XElement tktresponse)
        {
            string username = req.Descendants("UserName").FirstOrDefault().Value;
            string password = req.Descendants("Password").FirstOrDefault().Value;
            string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
            string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
            IEnumerable<XElement> request = req.Descendants("ticketRequest");
            string success = string.Empty;
            string status = string.Empty;
            string bookingrefno = string.Empty;
            string ticketstatus = string.Empty;
            string trip = string.Empty;
            string supamount = string.Empty;
            string maconversionrt = string.Empty;
            string saconversionrt = string.Empty;
            string totalamt = string.Empty;
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
                //bookingrefno = tktresponse.Descendants("UniqueID").FirstOrDefault().Value.Trim().ToString();
                bookingrefno = tktresponse.Descendants("TravelItinerary").FirstOrDefault().Descendants("UniqueID").LastOrDefault().Value.Trim().ToString();
                if (bookingrefno == null)
                {
                    if (bookingrefno == "")
                    {
                        bookingrefno = tktresponse.Descendants("UniqueID").FirstOrDefault().Value.Trim().ToString();
                    }
                }
                //ticketstatus = tktresponse.Descendants("TicketStatus").FirstOrDefault().Value.Trim().ToString();
                ticketstatus = tktresponse.Descendants("TravelItinerary").FirstOrDefault().Descendants("TicketStatus").LastOrDefault().Value.Trim().ToString();
                if (ticketstatus == null)
                {
                    if (ticketstatus == "")
                    {
                        ticketstatus = tktresponse.Descendants("TicketStatus").FirstOrDefault().Value.Trim().ToString();
                    }
                }
                if (status == "")
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
                                  new XElement("ticketResponse",
                                      new XElement("Flights",
                                         new XAttribute("success", success),
                                          new XAttribute("status", ticketstatus),
                                          new XAttribute("bookingrefno", bookingrefno),
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
                                              )
                                          )
                         ))));
            return searchdoc;
        }
        #endregion
        #region Flight Trip Details Binding (E Ticket)
        private List<XElement> flightsegments(List<XElement> flights)
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
        private List<XElement> pricebreakup(List<XElement> pricebrkups)
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
                               new XElement("Surchares", Convert.ToString("")),
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
        private List<XElement> taxes(List<XElement> taxes)
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
        private List<XElement> customerlistbind(List<XElement> custlst)
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
        #region Ticketing Request (Supplier)
        private string ticketrequest(string sessionid, string uniqueid, string mode)
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
        #region Create Session Request
        private string createsessionrequest()
        {
            string request = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:mys='Mystifly.OnePoint' xmlns:mys1='http://schemas.datacontract.org/2004/07/Mystifly.OnePoint'>" +
                              "<soapenv:Header/>" +
                              "<soapenv:Body>" +
                                "<mys:CreateSession>" +
                                  "<mys:rq>" +
                                    "<mys1:AccountNumber>MCN001486</mys1:AccountNumber>" +
                                    "<mys1:Password>INGEN2017_xml</mys1:Password>" +
                                    "<mys1:Target>Test</mys1:Target>" +
                                    "<mys1:UserName>INGEN_XML</mys1:UserName>" +
                                  "</mys:rq>" +
                                "</mys:CreateSession>" +
                              "</soapenv:Body>" +
                            "</soapenv:Envelope>";
            return request;
        }
        #endregion
        #region Order Ticketing Request (Supplier)
        public string orderticketrequest(string uniqueid, string mode,string sessionid)
        {
            //manage_session session_mgmt = new manage_session();
            //string sessionid = session_mgmt.session_manage(travayoorequest.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value, travayoorequest.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value);
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
    }
}