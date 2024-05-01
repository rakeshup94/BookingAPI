using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Models.Common;
using TravillioXMLOutService.Air.Models.Galileo;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.Galileo
{
    #region Ticketing (Galileo)
    public class gal_ticket : IDisposable
    {
        #region Object
        XElement commonrequest = null;
        gal_supresponse api_resp;
        string authorizeby = string.Empty;
        string targetbranch = string.Empty;
        #endregion
        #region Ticket Response
        public XElement ticketing_response(XElement req)
        {
            #region Ticket Response (Gal)
            commonrequest = req;
            try
            {
                XElement suppliercred = airsupplier_Cred.getgds_credentials(req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value, "50");
                authorizeby = suppliercred.Descendants("authorizedby").FirstOrDefault().Value;
                targetbranch = suppliercred.Descendants("branch").FirstOrDefault().Value;
                api_resp = new gal_supresponse();
                string api_response = api_resp.gal_apiresponse(req, ticketreq_gal(req).ToString(), "AirTicketing", 12, req.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value, req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value);
                XElement response = XElement.Parse(api_response);
                XElement supresp = RemoveAllNamespaces(response);
                XElement travayoo_out = ticketresponse_gal(req, supresp);
                return travayoo_out;
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "ticketing_response";
                ex1.PageName = "gal_ticket";
                ex1.CustomerID = req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value;
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
        #region Ticket Response Binding
        private XElement ticketresponse_gal(XElement req, XElement tktresponse)
        {
            #region Ticketing Response
            XElement responseout = null;
            try
            {
                string username = req.Descendants("UserName").FirstOrDefault().Value;
                string password = req.Descendants("Password").FirstOrDefault().Value;
                string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = req.Descendants("ticketRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                string success = string.Empty;
                string bookingrefno = string.Empty;
                string ticketstatus = string.Empty;
                string trip = string.Empty;
                decimal totalamt = 0;
                string currency = string.Empty;
                string faretype = string.Empty;
                string totalduration = string.Empty;
                List<XElement> fltsgmtlst = null;
                XElement docfileres = null;
                int failure = 0;
                failure = tktresponse.Descendants("TicketFailureInfo").Count();
                if (failure > 0)
                {
                    #region Ticketing Failed
                    int counttkt = tktresponse.Descendants("ETR").ToList().Count;
                    if (counttkt > 0)
                    {
                        #region GET UR
                        try
                        {
                            #region Call UR                            
                            api_resp = new gal_supresponse();
                            string api_response = api_resp.gal_apiresponse(req, URreq_gal(req).ToString(), "AirURTicket", 12, req.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value, req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value);
                            XElement response = XElement.Parse(api_response);
                            XElement supresp = RemoveAllNamespaces(response);
                            docfileres = RemoveAllNamespaces(supresp);
                            #endregion
                        }
                        catch { }
                        try
                        {
                            try
                            {
                                bookingrefno = req.Descendants("bookingrefno").FirstOrDefault().Value;
                                success = "true";
                                trip = req.Descendants("Itinerary").FirstOrDefault().Attribute("code").Value;
                                if (trip == "1")
                                    trip = "OneWay";
                                else
                                    trip = "Return";
                            }
                            catch { }
                            try
                            {
                                currency = docfileres.Descendants("AirPricingInfo").FirstOrDefault().Attribute("TotalPrice").Value.Substring(0, 3);
                                fltsgmtlst = docfileres.Descendants("AirPricingInfo").ToList();
                                for (int i = 0; i < fltsgmtlst.Count; i++)
                                {
                                    decimal pax = Convert.ToDecimal(fltsgmtlst[i].Descendants("PassengerType").Count());
                                    totalamt += Convert.ToDecimal(fltsgmtlst[i].Attribute("TotalPrice").Value.Remove(0, 3)) * pax;
                                }
                            }
                            catch { }
                            try
                            {
                                #region Ticket Status
                                string tktstatus = docfileres.Descendants("AirPricingInfo").FirstOrDefault().Attribute("Ticketed").Value;
                                if (tktstatus == "true")
                                {
                                    ticketstatus = "Ticketed";
                                }
                                else
                                {
                                    ticketstatus = "TktInProcess";
                                }
                                #endregion
                            }
                            catch { }
                        }
                        catch { }
                        #region Response
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
                                                                 new XElement("customerinfo", customerlistbind(docfileres.Descendants("TicketInfo").ToList(), docfileres.Descendants("BookingTraveler").ToList())
                                                                     )
                                                                 ),
                                                                 new XElement("ItineraryDetails",
                                                                    new XAttribute("trip", trip),
                                                                    new XAttribute("amount", totalamt),
                                                                    new XAttribute("currencycode", currency),
                                                                    new XAttribute("faretype", faretype),
                                                                    new XAttribute("totalduration", totalduration),
                                                                    new XElement("FlightSegments", flightsegments(docfileres.Descendants("UniversalRecord").FirstOrDefault())
                                                                        ),
                                                                    new XElement("PriceBreakups", pricebreakup(fltsgmtlst)
                                                                        )
                                                                     )
                                                              )
                                                          )
                                                      )
                                     ))));
                        return searchdoc;
                        #endregion
                        #endregion
                    }
                    else
                    {
                        #region Error
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
                                              tktresponse.Descendants("TicketFailureInfo").FirstOrDefault().Attribute("Message").Value
                                              )
                             ))));
                        return responseout;
                        #endregion
                    }
                    #endregion
                }
                else
                {
                    #region Ticketing
                    int counttkt = tktresponse.Descendants("ETR").ToList().Count;
                    if (counttkt > 0)
                    {
                        try
                        {
                            #region Call UR
                            api_resp = new gal_supresponse();
                            string api_response = api_resp.gal_apiresponse(req, URreq_gal(req).ToString(), "AirURTicket", 12, req.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value, req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value);
                            XElement response = XElement.Parse(api_response);
                            XElement supresp = RemoveAllNamespaces(response);
                            docfileres = RemoveAllNamespaces(supresp);
                            #endregion
                        }
                        catch { }
                        try
                        {
                            try
                            {
                                bookingrefno = req.Descendants("bookingrefno").FirstOrDefault().Value;
                                success = "true";
                                trip = req.Descendants("Itinerary").FirstOrDefault().Attribute("code").Value;
                                if (trip == "1")
                                    trip = "OneWay";
                                else
                                    trip = "Return";
                            }
                            catch { }
                            try
                            {
                                currency = docfileres.Descendants("AirPricingInfo").FirstOrDefault().Attribute("TotalPrice").Value.Substring(0, 3);
                                fltsgmtlst = docfileres.Descendants("AirPricingInfo").ToList();
                                for (int i = 0; i < fltsgmtlst.Count; i++)
                                {
                                    decimal pax = Convert.ToDecimal(fltsgmtlst[i].Descendants("PassengerType").Count());
                                    totalamt += Convert.ToDecimal(fltsgmtlst[i].Attribute("TotalPrice").Value.Remove(0, 3)) * pax;
                                }
                            }
                            catch { }
                            try
                            {
                                #region Ticket Status
                                string tktstatus = docfileres.Descendants("AirPricingInfo").FirstOrDefault().Attribute("Ticketed").Value;
                                if (tktstatus == "true")
                                {
                                    ticketstatus = "Ticketed";
                                }
                                else
                                {
                                    ticketstatus = "TktInProcess";
                                }
                                #endregion
                            }
                            catch { }
                        }
                        catch { }
                        #region Response
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
                                                                 new XElement("customerinfo", customerlistbind(docfileres.Descendants("TicketInfo").ToList(), docfileres.Descendants("BookingTraveler").ToList())
                                                                     )
                                                                 ),
                                                                 new XElement("ItineraryDetails",
                                                                    new XAttribute("trip", trip),
                                                                    new XAttribute("amount", totalamt),
                                                                    new XAttribute("currencycode", currency),
                                                                    new XAttribute("faretype", faretype),
                                                                    new XAttribute("totalduration", totalduration),
                                                                    new XElement("FlightSegments", flightsegments(docfileres.Descendants("UniversalRecord").FirstOrDefault())
                                                                        ),
                                                                    new XElement("PriceBreakups", pricebreakup(fltsgmtlst)
                                                                        )
                                                                     ))))
                                     ))));
                        return searchdoc;
                        #endregion
                    }
                    else
                    {                       
                        #region Ticketing Failed
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
                                              "Ticket Not Generated."
                                              )
                             ))));
                        return responseout;
                        #endregion
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "ticketresponse_gal";
                ex1.PageName = "gal_ticket";
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
        #region Flight Trip Details Binding (E Ticket)
        private List<XElement> flightsegments(XElement flightseg)
        {
            List<XElement> flightlst = new List<XElement>();
            List<XElement> flights = new List<XElement>();
            try
            {
                string pnr = string.Empty;
                flights = flightseg.Descendants("AirSegment").ToList();
                try
                {
                    pnr = flightseg.Descendants("ProviderReservationInfo").FirstOrDefault().Attribute("LocatorCode").Value;
                }
                catch { }
                for (int i = 0; i < flights.Count(); i++)
                {
                    try
                    {
                        #region Baggage
                        string baggage = string.Empty;
                        try
                        {
                            XElement fareinfo = flightseg.Descendants("AirPricingInfo").Descendants("FareInfo").FirstOrDefault();
                            int checkbag = fareinfo.Descendants("BaggageAllowance").Descendants("NumberOfPieces").Count();
                            if (checkbag > 0)
                            {
                                baggage = fareinfo.Descendants("BaggageAllowance").Descendants("NumberOfPieces").FirstOrDefault().Value + " Pieces";
                            }
                            else
                            {
                                if (fareinfo.Descendants("BaggageAllowance").Descendants("MaxWeight").Attributes("Unit").FirstOrDefault().Value == "Kilograms")
                                {
                                    baggage = fareinfo.Descendants("BaggageAllowance").Descendants("MaxWeight").Attributes("Value").FirstOrDefault().Value + " KG";
                                }
                                else
                                {
                                    baggage = fareinfo.Descendants("BaggageAllowance").Descendants("MaxWeight").Attributes("Value").FirstOrDefault().Value + " " + fareinfo.Descendants("BaggageAllowance").Descendants("MaxWeight").Attributes("Unit").FirstOrDefault().Value;
                                }
                            }
                        }
                        catch { }
                        #endregion

                        flightlst.Add(new XElement("Flight",
                            new XAttribute("from", flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("Origin") == null ? "" : flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("Origin").Value),
                             new XAttribute("to", flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("Destination") == null ? "" : flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("Destination").Value),
                              new XAttribute("depterminal", flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("OriginTerminal") == null ? "" : flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("OriginTerminal").Value),
                              new XAttribute("arrterminal", flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("DestinationTerminal") == null ? "" : flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("DestinationTerminal").Value),
                              new XAttribute("pnr", pnr),
                               new XAttribute("baggage", baggage),
                                new XAttribute("Marketingairlinecode", flights[i].Attribute("Carrier") == null ? "" : flights[i].Attribute("Carrier").Value),
                                 new XAttribute("Operatingairlinecode", flights[i].Attribute("Carrier") == null ? "" : flights[i].Attribute("Carrier").Value),
                                  new XAttribute("Equipment", flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("Equipment") == null ? "" : flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("Equipment").Value),
                                   new XAttribute("airlinenumber", flights[i].Attribute("FlightNumber") == null ? "" : flights[i].Attribute("FlightNumber").Value),
                                   new XAttribute("cabin", flights[i].Attribute("CabinClass") == null ? "" : flights[i].Attribute("CabinClass").Value),
                                   new XAttribute("departdatetime", flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("DepartureTime").Value),
                                   new XAttribute("arrivaldatetime", flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("ArrivalTime").Value),
                                   new XAttribute("duration", flights[i].Descendants("FlightDetails").FirstOrDefault().Attribute("TravelTime").Value),
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
        #region Bind Customer List
        private List<XElement> customerlistbind(List<XElement> custlst, List<XElement> travellerlst)
        {
            try
            {
                List<XElement> customers = new List<XElement>();
                for (int i = 0; i < custlst.Count(); i++)
                {
                    string passtype = string.Empty;
                    string passGender = string.Empty;
                    try
                    {
                        passtype = travellerlst[i].Attribute("TravelerType").Value;
                        passGender = travellerlst[i].Attribute("Gender").Value;
                    }
                    catch { passtype = ""; passGender = ""; }
                    customers.Add(new XElement("customer",
                               new XAttribute("type", Convert.ToString(passtype)),
                               new XAttribute("gender", Convert.ToString(passGender)),
                               new XAttribute("title", Convert.ToString(custlst[i].Descendants("Name").FirstOrDefault().Attribute("Prefix").Value)),
                               new XAttribute("firstname", Convert.ToString(custlst[i].Descendants("Name").FirstOrDefault().Attribute("First").Value)),
                               new XAttribute("lastname", Convert.ToString(custlst[i].Descendants("Name").FirstOrDefault().Attribute("Last").Value)),
                               new XAttribute("dob", Convert.ToString("")),
                               new XAttribute("nationality", Convert.ToString("")),
                               new XAttribute("passportno", Convert.ToString("")),
                               new XAttribute("pexpirtydate", Convert.ToString("")),
                               new XAttribute("pissuingcountry", Convert.ToString("")),
                               new XAttribute("mealpreference", Convert.ToString("")),
                               new XAttribute("seatpreference", Convert.ToString("")),
                               new XAttribute("emailid", Convert.ToString("")),
                               new XAttribute("phoneno", Convert.ToString("")),
                               new XAttribute("postalcode", Convert.ToString("")),
                               new XAttribute("ffn", Convert.ToString("")),
                               new XAttribute("ffnairlinecode", Convert.ToString("")),
                               new XAttribute("eTicketNumber", Convert.ToString(custlst[i].Attribute("Number").Value))
                               )
                        );
                }
                return customers;
            }
            catch { return null; }
        }
        #endregion
        #region Price Breakups
        #region Base Fare/other charges
        private List<XElement> pricebreakup(List<XElement> pricebrkups)
        {
            try
            {
                List<XElement> prcbrk = new List<XElement>();
                for (int i = 0; i < pricebrkups.Count(); i++)
                {
                    string pType = pricebrkups[i].Descendants("PassengerType").FirstOrDefault().Attribute("Code").Value;
                    string pssngertype = string.Empty;
                    if (pType == "CNN")
                    {
                        pssngertype = "CHD";
                    }
                    else
                    {
                        pssngertype = pricebrkups[i].Descendants("PassengerType").FirstOrDefault().Attribute("Code").Value;
                    }
                    List<XElement> taxlst = pricebrkups[i].Descendants("TaxInfo").ToList();
                    prcbrk.Add(new XElement("PriceBreakup",
                               new XElement("PType", Convert.ToString(pssngertype)),
                               new XElement("PQty", Convert.ToString(pricebrkups[i].Descendants("PassengerType").Count())),
                               new XElement("BaseFares",
                               new XElement("BaseFare", Convert.ToString(pricebrkups[i].Attribute("EquivalentBasePrice").Value).Remove(0, 3)),
                               new XElement("Currency", Convert.ToString(pricebrkups[i].Attribute("EquivalentBasePrice").Value).Substring(0, 3))
                               ),
                               new XElement("Surchares"),
                               new XElement("Taxes", taxes(taxlst)),
                               new XElement("TotalFares",
                                   new XElement("Amount", Convert.ToString(pricebrkups[i].Attribute("TotalPrice").Value.Remove(0, 3))),
                                   new XElement("Currency", Convert.ToString(pricebrkups[i].Attribute("TotalPrice").Value.Substring(0, 3)))
                                   )
                               )
                        );
                }
                return prcbrk;
            }
            catch { return null; }
        }
        #endregion
        #region Taxes
        private List<XElement> taxes(List<XElement> taxes)
        {
            try
            {
                List<XElement> taxbrkup = new List<XElement>();
                for (int i = 0; i < taxes.Count(); i++)
                {
                    taxbrkup.Add(new XElement("Tax",
                               new XElement("TaxCode", Convert.ToString(taxes[i].Attribute("Category").Value)),
                               new XElement("Currency", Convert.ToString(taxes[i].Attribute("Amount").Value.Substring(0, 3))),
                               new XElement("Amount", Convert.ToString(taxes[i].Attribute("Amount").Value.Remove(0, 3)))
                               )
                        );
                }
                return taxbrkup;
            }
            catch { return null; }
        }
        #endregion
        #endregion
        #region Ticketing Request GAL
        private XElement ticketreq_gal(XElement req)
        {
            try
            {
                #region Gal Request
                XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
                XNamespace ns = "http://www.travelport.com/schema/air_v42_0";
                XNamespace ns1 = "http://www.travelport.com/schema/common_v42_0";
                XElement common_request = new XElement(soap + "Envelope",
                                            new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                            new XElement(soap + "Body",
                                            new XElement(ns + "AirTicketingReq",
                                                new XAttribute("AuthorizedBy", authorizeby),
                                                new XAttribute("TargetBranch", targetbranch),
                                                new XElement(ns1 + "BillingPointOfSaleInfo",
                                                    new XAttribute("OriginApplication", "uAPI")),
                                                    new XElement(ns + "AirReservationLocatorCode", req.Descendants("resLocatorCode").FirstOrDefault().Value),
                                                    pricinginfobindreq(req.Descendants("PriceInfo").ToList()),
                                                     new XElement(ns + "AirTicketingModifiers",
                                                        new XElement(ns1 + "FormOfPayment",
                                                            new XAttribute("Type", "Cash")))
                                                                )));
                return common_request;
                #endregion
            }
            catch { return null; }
        }
        #endregion
        #region Pricing Info List (Requst)
        private List<XElement> pricinginfobindreq(List<XElement> pricelst)
        {
            XNamespace ns = "http://www.travelport.com/schema/air_v42_0";
            try
            {
                List<XElement> prclst = new List<XElement>();
                for (int i = 0; i < pricelst.Count(); i++)
                {
                    prclst.Add(new XElement(ns + "AirPricingInfoRef",
                               new XAttribute("Key", Convert.ToString(pricelst[i].Attribute("Key").Value))
                               )
                        );
                }
                return prclst;
            }
            catch { return null; }
        }
        #endregion
        #region Universal Record Retrieve Request GAL
        private XElement URreq_gal(XElement req)
        {
            try
            {
                #region Gal Request
                XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
                XNamespace ns = "http://www.travelport.com/schema/universal_v42_0";
                XNamespace ns1 = "http://www.travelport.com/schema/common_v42_0";
                XElement common_request = new XElement(soap + "Envelope",
                                            new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                            new XElement(soap + "Body",
                                            new XElement(ns + "UniversalRecordRetrieveReq",
                                                new XAttribute("TraceId", commonrequest.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value),
                                                new XAttribute("AuthorizedBy", authorizeby),
                                                new XAttribute("TargetBranch", targetbranch),
                                                new XElement(ns1 + "BillingPointOfSaleInfo",
                                                    new XAttribute("OriginApplication", "uAPI")),
                                                    new XElement(ns + "UniversalRecordLocatorCode", req.Descendants("bookingrefno").FirstOrDefault().Value)
                                                                )));
                return common_request;
                #endregion
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
        ~gal_ticket()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion
}