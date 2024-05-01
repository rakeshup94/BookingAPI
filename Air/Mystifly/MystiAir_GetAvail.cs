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
    public class MystiAir_GetAvail
    {
        #region Staticdata
        public XElement airlinexml;
        public XElement airportxml;
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
        #endregion
        #region Travayoo Availability of Air (Mystifly)
        public XElement AirAvailability_mysti(XElement req)
        {
            try
            {
                XElement travayoo_out = null;
                string url = string.Empty;
                string response = string.Empty;
                XElement suppliercred = airsupplier_Cred.getgds_credentials(req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value, "12");
                url = suppliercred.Descendants("URL").FirstOrDefault().Value;
                string method = suppliercred.Descendants("AirSearch").FirstOrDefault().Value;
                string Target = suppliercred.Descendants("Mode").FirstOrDefault().Value;
                Mysti_SupplierResponse sup_response = new Mysti_SupplierResponse();
                string customerid = string.Empty;
                string trackno = string.Empty;
                customerid = req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value;
                trackno = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;                
                manage_session session_mgmt = new manage_session();
                //string sessionid = session_mgmt.session_manage(req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value, req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value);
                string sessionid = suppliercred.Descendants("sessionid").FirstOrDefault().Value;
                string apireq = apirequest(req, sessionid, Target);
                response = sup_response.supplierresponse_mystifly(url, apireq, method, "AirSearch", 1, trackno, customerid).ToString();
                #region Get Data from DB
                getcurrencymrkup getcurrencymrk=new getcurrencymrkup();
                List<DataTable> dtconversion = getcurrencymrk.getcurrencyConversion(Convert.ToInt64(req.Descendants("AgentId").FirstOrDefault().Value), "SAAS");
                dtconversionrate = dtconversion[0];
                dtmasacurrency = dtconversion[1];
                dtmarkup = getcurrencymrk.getmarkupdetails(Convert.ToInt64(req.Descendants("AgentId").FirstOrDefault().Value),"12", "SAAS");
                #endregion
                XElement availrsponse = XElement.Parse(response.ToString());
                XElement doc = RemoveAllNamespaces(availrsponse);
                //airlinexml = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Flight\Mystifly\airlinelist.xml"));
                //airportxml = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Flight\Mystifly\airportlist.xml"));
                airlinexml = air_staticData.air_airlinexml();
                airportxml = air_staticData.air_airportxml();
                List<XElement> flightlist = FlightList(doc.Descendants("PricedItinerary").ToList());
                travayoo_out = travayooapiresponse(flightlist, req, sessionid);
                return travayoo_out;
            }
            catch(Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "AirAvailability_mysti";
                ex1.PageName = "MystiAir_GetAvail";
                ex1.CustomerID = req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        #endregion
        #region Flight List
        public List<XElement> FlightList(List<XElement> fltlist)
        {
            List<XElement> flightlst = new List<XElement>();            
            try
            {
                for (int i = 0; i < fltlist.Count(); i++)
                {
                    try
                    {
                        string seqno = string.Empty;
                        string searchtype = string.Empty;
                        string trip = string.Empty;
                        string amount = string.Empty;
                        string sellingamt = string.Empty;
                        string currencycode = string.Empty;
                        string ispssportmndtry = string.Empty;
                        string faretype = string.Empty;
                        string faresourcecode = string.Empty;
                        string totalduration = string.Empty;
                        string tickettype = string.Empty;
                        string isrefundable = string.Empty;
                        seqno = fltlist[i].Descendants("SequenceNumber").FirstOrDefault().Value;
                        searchtype = fltlist[i].Descendants("DirectionInd").FirstOrDefault().Value;
                        ispssportmndtry = fltlist[i].Descendants("IsPassportMandatory").FirstOrDefault().Value;
                        faretype = fltlist[i].Descendants("FareType").FirstOrDefault().Value;
                        faresourcecode = fltlist[i].Descendants("FareSourceCode").FirstOrDefault().Value;
                        tickettype = fltlist[i].Descendants("TicketType").FirstOrDefault().Value;
                        XElement onwordseg = null;
                        XElement returnseg = null;
                        List<XElement> triplist = fltlist[i].Descendants("OriginDestinationOption").ToList();
                        List<XElement> fltsegmentsonward = triplist[0].Descendants("FlightSegment").ToList();
                        onwordseg = new XElement("FlightSegments", new XAttribute("searchtype", "Onward"), flightsegments(fltsegmentsonward));
                        List<XElement> fltsegmentsreturn = null;
                        if (triplist.Count() > 1)
                        {
                            fltsegmentsreturn = triplist[1].Descendants("FlightSegment").ToList();
                            returnseg = new XElement("FlightSegments", new XAttribute("searchtype", "Return"), flightsegments(fltsegmentsreturn));
                        }
                        try
                        {
                            isrefundable = fltlist[i].Descendants("IsRefundable").FirstOrDefault().Value;
                            XElement ItinTotalFare = fltlist[i].Descendants("ItinTotalFare").Descendants("TotalFare").FirstOrDefault();
                            amount = ItinTotalFare.Descendants("Amount").FirstOrDefault().Value;
                            #region currency conversion
                            try
                            {
                                mamarkuptype = dtmarkup.Rows[0]["MainAgentMarkupType"].ToString();
                                samarkuptype = dtmarkup.Rows[0]["SubAgentMrkupType"].ToString();
                                mamarkupval = Convert.ToDecimal(dtmarkup.Rows[0]["MainAgentMrkupVal"].ToString());
                                samarkupval = Convert.ToDecimal(dtmarkup.Rows[0]["SubAgentMrkupVal"].ToString());
                                DataRow[] row = dtconversionrate.Select("crncyCode = " + "'" + ItinTotalFare.Descendants("CurrencyCode").FirstOrDefault().Value + "'");
                                maconversion = Convert.ToDecimal(row[0].ItemArray[1]);
                                saconversion = Convert.ToDecimal(row[0].ItemArray[2]);
                                #region Conversion and markup
                                try
                                {
                                    sellingamt = Convert.ToString(convertedamt(Convert.ToDecimal(amount)));
                                }
                                catch { }
                                #endregion
                            }
                            catch { }
                            #endregion
                            currencycode = ItinTotalFare.Descendants("CurrencyCode").FirstOrDefault().Value;
                            agentcurrency = dtmasacurrency.Rows[0]["SAcrncy"].ToString();
                        }
                        catch { }
                        flightlst.Add(new XElement("Itinerary",
                            new XAttribute("SequenceNumber", seqno),
                              new XAttribute("trip", searchtype),
                               new XAttribute("amount", sellingamt),
                                new XAttribute("currencycode", agentcurrency),
                                 new XAttribute("ispassportmandatory", ispssportmndtry),
                                  new XAttribute("faretype", faretype),
                                  new XAttribute("faresoucecode", faresourcecode),
                                   new XAttribute("totalduration", totalduration),
                                   new XAttribute("TicketType", tickettype),
                                   new XAttribute("isRefundable",isrefundable),
                                   new XAttribute("supplierid", "12"),
                                   new XAttribute("suppliername", "Mystifly"),
                                   onwordseg,
                                   returnseg,
                                   new XElement("PriceBreakups", pricebreakup(fltlist[i].Descendants("PTC_FareBreakdown").ToList()))
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
        #region Flight Segments
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
                        string cabinclass = string.Empty;
                        string departdatetime = string.Empty;
                        string arrivaldatetime = string.Empty;
                        string eticket = string.Empty;
                        string JourneyDuration = string.Empty;
                        string mealcode = string.Empty;
                        string seatsremaining = string.Empty;
                        string airlinename = string.Empty;
                        string fromcityname = string.Empty;
                        string tocityname = string.Empty;
                        string fromairportname = string.Empty;
                        string toairportname = string.Empty;
                        from = flights[i].Descendants("DepartureAirportLocationCode").FirstOrDefault().Value;
                        to = flights[i].Descendants("ArrivalAirportLocationCode").FirstOrDefault().Value;
                        Marketingairlinecode = flights[i].Descendants("MarketingAirlineCode").FirstOrDefault().Value;
                        airlinenumber = flights[i].Descendants("FlightNumber").FirstOrDefault().Value;
                        cabin = flights[i].Descendants("CabinClassCode").FirstOrDefault().Value;
                        eticket = flights[i].Descendants("Eticket").FirstOrDefault().Value;
                        JourneyDuration = flights[i].Descendants("JourneyDuration").FirstOrDefault().Value;
                        mealcode = flights[i].Descendants("MealCode").FirstOrDefault().Value;
                        try
                        {
                            if (cabin == "Y")
                            {
                                cabinclass = "Economy";
                            }
                            else if (cabin == "C")
                            {
                                cabinclass = "Business";
                            }
                            else if (cabin == "F")
                            {
                                cabinclass = "First";
                            }
                            else if (cabin == "S")
                            {
                                cabinclass = "Premium Economy";
                            }                            
                        }
                        catch { }
                        try
                        {
                            XElement operatingairlines = flights[i].Descendants("OperatingAirline").FirstOrDefault();
                            operatingairlinecode = operatingairlines.Descendants("Code").FirstOrDefault().Value;
                            equipment = operatingairlines.Descendants("Equipment").FirstOrDefault().Value;
                        }
                        catch { }
                        try
                        {
                            XElement seatsremain = flights[i].Descendants("SeatsRemaining").FirstOrDefault();
                            seatsremaining = seatsremain.Descendants("Number").FirstOrDefault().Value;
                            departdatetime = flights[i].Descendants("DepartureDateTime").FirstOrDefault().Value;
                            arrivaldatetime = flights[i].Descendants("ArrivalDateTime").FirstOrDefault().Value;
                            XElement fromairportlst = airportxml.Descendants("record").Where(x => x.Descendants("AirportCode").FirstOrDefault().Value == from).FirstOrDefault();
                            fromairportname = fromairportlst.Descendants("AirPortName").FirstOrDefault().Value;
                            fromcityname = fromairportlst.Descendants("cityName").FirstOrDefault().Value;
                            XElement toairportlst = airportxml.Descendants("record").Where(x => x.Descendants("AirportCode").FirstOrDefault().Value == to).FirstOrDefault();
                            toairportname = toairportlst.Descendants("AirPortName").FirstOrDefault().Value;
                            tocityname = toairportlst.Descendants("cityName").FirstOrDefault().Value;
                            XElement airlinelst = airlinexml.Descendants("Airline").Where(x => x.Descendants("airlinecode").FirstOrDefault().Value == operatingairlinecode).FirstOrDefault();
                            airlinename = airlinelst.Descendants("airlinename").FirstOrDefault().Value;
                        }
                        catch { }
                        flightlst.Add(new XElement("Flight",
                            new XAttribute("from", from),
                             new XAttribute("to", to),
                              new XAttribute("Marketingairlinecode", Marketingairlinecode),
                              new XAttribute("Operatingairlinecode", operatingairlinecode),
                              new XAttribute("Equipment", equipment),
                               new XAttribute("airlinenumber", airlinenumber),
                                new XAttribute("cabin", cabin),
                                 new XAttribute("departdatetime", departdatetime),
                                  new XAttribute("arrivaldatetime", arrivaldatetime),
                                   new XAttribute("eticket", eticket),
                                   new XAttribute("duration", JourneyDuration),
                                   new XAttribute("durationtype", "minutes"),
                                   new XAttribute("MealCode", mealcode),
                                   new XAttribute("SeatsRemaining", seatsremaining),
                                   new XAttribute("airlinename", airlinename),
                                   new XAttribute("fromcityname", fromcityname),
                                   new XAttribute("tocityname", tocityname),
                                   new XAttribute("fromairportname", fromairportname),
                                   new XAttribute("toairportname", toairportname),
                                   new XAttribute("CabinClass", cabinclass),
                                   new XAttribute("Baggage", "")
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
                    try
                    {
                        sellingamtbf = convertedamt(Convert.ToDecimal(pricebrkups[i].Descendants("EquivFare").Descendants("Amount").FirstOrDefault().Value));
                        sellingamtot = convertedamt(Convert.ToDecimal(pricebrkups[i].Descendants("TotalFare").Descendants("Amount").FirstOrDefault().Value));
                         
                    }
                    catch { }
                    #endregion
                    List<XElement> taxlst = pricebrkups[i].Descendants("Tax").ToList();
                    prcbrk.Add(new XElement("PriceBreakup",
                               new XElement("PType", Convert.ToString(pricebrkups[i].Descendants("PassengerTypeQuantity").Descendants("Code").FirstOrDefault().Value)),
                               new XElement("PQty", Convert.ToString(pricebrkups[i].Descendants("PassengerTypeQuantity").Descendants("Quantity").FirstOrDefault().Value)),
                               new XElement("BaseFares",
                               new XElement("BaseFare", Convert.ToString(sellingamtbf)),
                               new XElement("Currency", agentcurrency)
                               ),
                               //new XElement("Surchares", Convert.ToString(pricebrkups[i].Descendants("Surcharges").FirstOrDefault().Value)),
                               new XElement("Surchares",surcharges(pricebrkups[i].Descendants("Surcharge").ToList(),pricebrkups[i].Descendants("EquivFare").Descendants("CurrencyCode").FirstOrDefault().Value)),
                               new XElement("Taxes", taxes(taxlst)),
                               new XElement("TotalFares",
                                   new XElement("Amount", sellingamtot),
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
                    try
                    {
                        sellingamttax = convertedamt(Convert.ToDecimal(taxes[i].Descendants("Amount").FirstOrDefault().Value));
                    }
                    catch { }
                    #endregion
                    taxbrkup.Add(new XElement("Tax",
                               new XElement("TaxCode", Convert.ToString(taxes[i].Descendants("TaxCode").FirstOrDefault().Value)),
                               new XElement("Currency", agentcurrency),
                               new XElement("Amount", sellingamttax)
                               )
                        );
                }
                return taxbrkup;
            }
            catch { return null; }
        }
        public List<XElement> surcharges(List<XElement> surchargs,string currency)
        {
            try
            {
                List<XElement> taxbrkup = new List<XElement>();
                for (int i = 0; i < surchargs.Count(); i++)
                {
                    #region Conversion and markup
                    decimal sellingamtsrv = 0;
                    try
                    {
                        sellingamtsrv = convertedamt(Convert.ToDecimal(surchargs[i].Descendants("Amount").FirstOrDefault().Value));
                    }
                    catch { }
                    #endregion
                    taxbrkup.Add(new XElement("Surcharge",
                               new XElement("Type", Convert.ToString(surchargs[i].Descendants("Type").FirstOrDefault().Value)),
                               new XElement("Currency", agentcurrency),
                               new XElement("Amount", sellingamtsrv)
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
            catch {  }
            return finalamt;
        }
        #endregion
        #endregion
        #region Supplier API Response
        public string searchresponse(string url, string postData, string soapaction)
        {
            try
            {
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myHttpWebRequest.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes(postData);
                myHttpWebRequest.Headers.Add("SOAPAction", soapaction);
                myHttpWebRequest.ContentType = "text/xml;charset=UTF-8";
                myHttpWebRequest.ContentLength = data.Length;
                Stream requestStream = myHttpWebRequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                Stream responseStream = myHttpWebResponse.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default);
                string pageContent = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                responseStream.Close();
                myHttpWebResponse.Close();
                return pageContent;
            }
            catch (WebException webex)
            {
                WebResponse errResp = webex.Response;
                using (Stream respStream = errResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream);
                    string text = reader.ReadToEnd();
                }
                return null;
            }
        }
        #endregion
        #region api response
        public XElement travayooapiresponse(List<XElement> fltresponse, XElement req, string sessionid)
        {
            string username = req.Descendants("UserName").FirstOrDefault().Value;
            string password = req.Descendants("Password").FirstOrDefault().Value;
            string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
            string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
            IEnumerable<XElement> request = req.Descendants("SearchRequest");
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
                                  new XElement("SearchResponse",
                                      new XElement("Flights",
                                          new XAttribute("SessionId", sessionid),
                                          fltresponse
                                          )
                         ))));
            return searchdoc;
        }
        #endregion
        #region API Request
        public string apirequest(XElement req, string sessionid,string mode)
        {
            string tripcode = string.Empty;
            string triptype = string.Empty;
            string adult = string.Empty;
            string child = string.Empty;
            string infant = string.Empty;
            string origin = string.Empty;
            string destination = string.Empty;
            string departdate = string.Empty;
            string returndate = string.Empty;
            string language = string.Empty;
            string classcode = string.Empty;
            string childpax = string.Empty;
            string infantpax = string.Empty;
            string returntrip = string.Empty;
            string preferredairlines = string.Empty;
            string nonstop = "All";
            string preferencelevel = "Preferred";
            string classtype = "Y";
            XElement occupancy = req.Descendants("Occupancy").FirstOrDefault();
            adult = occupancy.Attribute("adult").Value;
            child = occupancy.Attribute("child").Value;
            infant = occupancy.Attribute("infant").Value;
            XElement iternary = req.Descendants("Itinerary").FirstOrDefault();
            tripcode = iternary.Attribute("code").Value;
            origin = iternary.Descendants("Origin").FirstOrDefault().Value;
            destination = iternary.Descendants("Destination").FirstOrDefault().Value;
            classcode = iternary.Descendants("ClassCode").FirstOrDefault().Value;
            try
            {                
                string preferredflt = iternary.Descendants("PreferredAirline").FirstOrDefault().Value;
                if (preferredflt != null)
                {
                    preferredairlines = preferredairline(preferredflt);
                }
            }
            catch { }
            try
            {
                string nonstp = iternary.Descendants("NonStop").FirstOrDefault().Value;
                if (nonstp == "true")
                {
                    nonstop = "Direct";
                }
            }
            catch { }
            try
            {
                string classname = iternary.Descendants("Class").FirstOrDefault().Value;
                if (classname != "")
                {                    
                    if (classname == "Economy")
                    {
                        preferencelevel = "Restricted";
                        classtype = "Y";
                    }
                    else if (classname == "PremiumEconomy")
                    {
                        preferencelevel = "Restricted";
                        classtype = "S";
                    }
                    else if (classname == "Business")
                    {
                        preferencelevel = "Restricted";
                        classtype = "C";
                    }
                    else if (classname == "First")
                    {
                        preferencelevel = "Restricted";
                        classtype = "F";
                    }
                    else
                    {
                        preferencelevel = "Preferred";
                        classtype = "Y";
                    }
                }
            }
            catch
            {
                preferencelevel = "Preferred";
                classtype = "Y";
            }
            if (Convert.ToInt32(child) > 0)
            {
                childpax = paxreq(Convert.ToInt32(child), "CHD");
            }
            if (Convert.ToInt32(infant) > 0)
            {
                infantpax = paxreq(Convert.ToInt32(infant), "INF");
            }
            DateTime depdateTime = DateTime.ParseExact(iternary.Descendants("DepartDate").FirstOrDefault().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            departdate = depdateTime.ToString("yyyy-MM-dd");
            triptype = "OneWay";
            if (Convert.ToInt32(tripcode) > 1)
            {
                triptype = "Return";
                returntrip = returntripreq(iternary);
            }
            string request = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'" +
                                 " xmlns:mys='Mystifly.OnePoint' xmlns:mys1='http://schemas.datacontract.org/2004/07/Mystifly.OnePoint'" +
                                  " xmlns:arr='http://schemas.microsoft.com/2003/10/Serialization/Arrays'>" +
                                  "<soapenv:Header/>" +
                                  "<soapenv:Body>" +
                                    "<mys:AirLowFareSearch>" +
                                      "<mys:rq>" +
                                        "<mys1:OriginDestinationInformations>" +
                                          "<mys1:OriginDestinationInformation>" +
                                            "<mys1:DepartureDateTime>" + departdate + "T00:00:00</mys1:DepartureDateTime>" +
                                            "<mys1:DestinationLocationCode>" + destination + "</mys1:DestinationLocationCode>" +
                                            "<mys1:OriginLocationCode>" + origin + "</mys1:OriginLocationCode>" +
                                          "</mys1:OriginDestinationInformation>" +
                                          returntrip +
                                        "</mys1:OriginDestinationInformations>" +
                                        "<mys1:PassengerTypeQuantities>" +
                                          "<mys1:PassengerTypeQuantity>" +
                                            "<mys1:Code>ADT</mys1:Code>" +
                                            "<mys1:Quantity>" + adult + "</mys1:Quantity>" +
                                          "</mys1:PassengerTypeQuantity> " +
                                          childpax +
                                          infantpax +
                                        "</mys1:PassengerTypeQuantities>" +
                                        "<mys1:PricingSourceType>All</mys1:PricingSourceType>" +
                                        "<mys1:RequestOptions>TwoHundred</mys1:RequestOptions>" +
                                        "<mys1:SessionId>" + sessionid + "</mys1:SessionId>" +
                                        "<mys1:Target>" + mode + "</mys1:Target>" +
                                        "<mys1:TravelPreferences>" +
                                          "<mys1:AirTripType>" + triptype + "</mys1:AirTripType>" +
                                          //"<mys1:Preferences>" +
                                             //"<mys1:CabinClassPreference>" +
                                              "<mys1:CabinPreference>" + classtype + "</mys1:CabinPreference>" +
                                                    "<mys1:CabinType>" + classtype + "</mys1:CabinType>" +
                                                    "<mys1:PreferenceLevel>" + preferencelevel + "</mys1:PreferenceLevel>" +
                                               //"</mys1:CabinClassPreference>" +
                                             //"</mys1:Preferences>" +
                                          //"<mys1:CabinPreference>Y</mys1:CabinPreference>" +
                                          "<mys1:MaxStopsQuantity>" + nonstop + "</mys1:MaxStopsQuantity>" +
                                           preferredairlines +
                                        "</mys1:TravelPreferences>" +                                        
                                      "</mys:rq>" +
                                    "</mys:AirLowFareSearch>" +
                                  "</soapenv:Body>" +
                                "</soapenv:Envelope>";
            return request;
        }
        public string paxreq(int paxcount, string paxtype)
        {
            string paxrequest = "<mys1:PassengerTypeQuantity>" +
                                            "<mys1:Code>" + paxtype + "</mys1:Code>" +
                                            "<mys1:Quantity>" + paxcount + "</mys1:Quantity>" +
                                          "</mys1:PassengerTypeQuantity> ";
            return paxrequest;
        }
        private string preferredairline(string airlinelst)
        {
            string airlines = string.Empty;
            try
            {
                if (airlinelst != "")
                airlines = "<mys1:VendorPreferenceCodes><arr:string>" + airlinelst + "</arr:string></mys1:VendorPreferenceCodes>";
            }
            catch { }
            return airlines;
        }
        public string returntripreq(XElement iternary)
        {
            DateTime retdateTime = DateTime.ParseExact(iternary.Descendants("ReturnDate").FirstOrDefault().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            string retdate = retdateTime.ToString("yyyy-MM-dd");

            string retrequest = "<mys1:OriginDestinationInformation>" +
                                            "<mys1:DepartureDateTime>" + retdate + "T00:00:00</mys1:DepartureDateTime>" +
                                            "<mys1:DestinationLocationCode>" + iternary.Descendants("Origin").FirstOrDefault().Value + "</mys1:DestinationLocationCode>" +
                                            "<mys1:OriginLocationCode>" + iternary.Descendants("Destination").FirstOrDefault().Value + "</mys1:OriginLocationCode>" +
                                          "</mys1:OriginDestinationInformation>";
            return retrequest;
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
        #region Create Session Request
        public string createsessionrequest()
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
    }
}