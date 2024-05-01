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
using TravillioXMLOutService.Air.Models.Common;
using TravillioXMLOutService.Air.Models.TBO;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.TBO
{
    public class tbo_pricecheck : IDisposable
    {
        #region Staticdata
        public string countrycode = string.Empty;
        public string countryname = string.Empty;
        public string preid = string.Empty;
        public XElement airlinexml;
        public XElement airportxml;
        tboair_supresponse api_resp;
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
        #region api response
        public XElement travayooapiresponse(List<XElement> fltresponse, XElement req, string sessionid)
        {
            string username = req.Descendants("UserName").FirstOrDefault().Value;
            string password = req.Descendants("Password").FirstOrDefault().Value;
            string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
            string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
            try
            {
                string isavailable = "false";
                string status = "false";
                try
                {
                    if (fltresponse.Descendants("Flight") != null)
                    {
                        if (fltresponse.Descendants("Flight").Count() > 0)
                        {
                            isavailable = "true";
                            status = "true";
                        }
                    }
                }
                catch { }
                IEnumerable<XElement> request = req.Descendants("PreBookRequest");
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
                                      new XElement("PreBookResponse",
                                          new XElement("Flights",
                                              new XAttribute("SessionId", sessionid),
                                              new XAttribute("isAvailable", isavailable),
                                              new XAttribute("status", status),
                                              new XAttribute("isValid", status),
                                              new XAttribute("preID", preid),
                                              fltresponse
                                              )
                             ))));
                return searchdoc;
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "travayooapiresponse";
                ex1.PageName = "tbo_pricecheck";
                ex1.CustomerID = req.Descendants("PreBookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("PreBookRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                IEnumerable<XElement> request = req.Descendants("PreBookRequest");
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
                                      new XElement("PreBookResponse",
                                          new XElement("Flights",
                                              new XAttribute("SessionId", sessionid),
                                              new XAttribute("isAvailable", "false"),
                                              new XAttribute("status", "false"),
                                              new XAttribute("isValid", "false"),
                                              new XAttribute("preID", preid)
                                              )
                             ))));
                return searchdoc;
                #endregion
            }
        }
        #endregion
        #region generate GUID
        public string generatepreId()
        {
            try
            {
                var ticks = DateTime.Now.Ticks;
                var guid = Guid.NewGuid().ToString();
                var uniqueSessionId = ticks.ToString() + '-' + guid;
                return uniqueSessionId;
            }
            catch
            {
                var guid = Guid.NewGuid().ToString();
                return guid;
            }
        }
        #endregion
        #region Availability Response
        public XElement pricechecktbo_response(XElement req)
        {
            #region Availability Response (TBO)
            XElement travayoo_out = null;
            try
            {
                try
                {

                    preid = generatepreId();
                    airportxml = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Flight\Mystifly\airportlist.xml"));
                    XElement record = airportxml.Descendants("record").Where(x => x.Descendants("AirportCode").FirstOrDefault().Value == req.Descendants("PreBookRequest").Descendants("Itinerary").Descendants("Origin").FirstOrDefault().Value).FirstOrDefault();
                    countrycode = record.Descendants("countryCode").FirstOrDefault().Value;
                    countryname = record.Descendants("countryName").FirstOrDefault().Value;
                }
                catch { }
                api_resp = new tboair_supresponse();
                string api_response = api_resp.tbo_supresponse(req, fltpricecheckrequest(req).ToString(), "AirPriceCheck", 4, req.Descendants("PreBookRequest").FirstOrDefault().Attribute("TransID").Value, req.Descendants("PreBookRequest").FirstOrDefault().Attribute("CustomerID").Value, preid);
                var xml = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.ASCII.GetBytes(api_response), new XmlDictionaryReaderQuotas()));
                List<XElement> flights = xml.Descendants("item").Where(x => x.Attribute("type").Value == "object" && x.Parent.Name == "Result").ToList();
                if (xml.Descendants("IsSuccess").FirstOrDefault().Value == "true")
                {
                    #region Get Data from DB
                    getcurrencymrkup getcurrencymrk = new getcurrencymrkup();
                    List<DataTable> dtconversion = getcurrencymrk.getcurrencyConversion(Convert.ToInt64(req.Descendants("AgentId").FirstOrDefault().Value), "SAAS");
                    dtconversionrate = dtconversion[0];
                    dtmasacurrency = dtconversion[1];
                    dtmarkup = getcurrencymrk.getmarkupdetails(Convert.ToInt64(req.Descendants("AgentId").FirstOrDefault().Value), "51", "SAAS");
                    #endregion
                    string trackid = xml.Descendants("TrackingId").FirstOrDefault().Value;
                    travayoo_out = travayooapiresponse(bindflt_tbo(flights), req, trackid);
                    return travayoo_out;
                }
                else
                {
                    string username = req.Descendants("UserName").FirstOrDefault().Value;
                    string password = req.Descendants("Password").FirstOrDefault().Value;
                    string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
                    string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
                    string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
                    IEnumerable<XElement> request = req.Descendants("PreBookRequest");
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
                                          new XElement("PreBookResponse",
                                              new XElement("Flights",
                                                  new XAttribute("SessionId", ""),
                                                  new XAttribute("isAvailable", "false"),
                                                  new XAttribute("status", "false"),
                                                  new XAttribute("isValid", "false"),
                                                  new XAttribute("preID", preid)
                                                  )
                                 ))));
                    return searchdoc;
                }

            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "avail_response";
                ex1.PageName = "tbo_pricecheck";
                ex1.CustomerID = req.Descendants("PreBookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("PreBookRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                string username = req.Descendants("UserName").FirstOrDefault().Value;
                string password = req.Descendants("Password").FirstOrDefault().Value;
                string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = req.Descendants("PreBookRequest");
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
                                      new XElement("PreBookResponse",
                                          new XElement("Flights",
                                              new XAttribute("SessionId", ""),
                                              new XAttribute("isAvailable", "false"),
                                              new XAttribute("status", "false"),
                                              new XAttribute("isValid", "false")
                                              )
                             ))));
                return searchdoc;
            }
            #endregion
        }
        #endregion
        #region bind flights
        private List<XElement> bindflt_tbo(List<XElement> flights)
        {
            List<XElement> fltresponse = new List<XElement>();
            try
            {
                int ftcount = flights.Count();
                for (int i = 0; i < ftcount; i++)
                {
                    try
                    {
                        string trip = "Onward";
                        decimal amount = 0;
                        string supamount = string.Empty;
                        string maconversionrt = string.Empty;
                        string saconversionrt = string.Empty;
                        //string amount = string.Empty;
                        string sellingamt = string.Empty;
                        string masellingamt = string.Empty;
                        string mamarkup = string.Empty;
                        string samarkup = string.Empty;
                        string currencycode = string.Empty;
                        string faretype = string.Empty;
                        string faresoucecode = string.Empty;
                        string isRefundable = "No";
                        if (flights[i].Descendants("JourneyType").FirstOrDefault().Value == "2")
                        {
                            trip = "Return";
                        }
                        amount = Convert.ToDecimal(flights[i].Descendants("Fare").FirstOrDefault().Descendants("TotalFare").FirstOrDefault().Value);
                        currencycode = flights[i].Descendants("Fare").FirstOrDefault().Descendants("AgentPreferredCurrency").FirstOrDefault().Value;
                        faretype = flights[i].Descendants("Fare").FirstOrDefault().Descendants("FareType").FirstOrDefault().Value;
                        faresoucecode = flights[i].Descendants("ResultId").FirstOrDefault().Value;
                        if (flights[i].Descendants("NonRefundable").FirstOrDefault().Value.ToUpper() == "FALSE")
                        {
                            isRefundable = "Yes";
                        }
                        supamount = amount.ToString("0.##");
                        #region currency conversion
                        try
                        {                            
                            mamarkuptype = dtmarkup.Rows[0]["MainAgentMarkupType"].ToString();
                            samarkuptype = dtmarkup.Rows[0]["SubAgentMrkupType"].ToString();
                            mamarkupval = Convert.ToDecimal(dtmarkup.Rows[0]["MainAgentMrkupVal"].ToString());
                            samarkupval = Convert.ToDecimal(dtmarkup.Rows[0]["SubAgentMrkupVal"].ToString());
                            DataRow[] row = dtconversionrate.Select("crncyCode = " + "'" + currencycode + "'");
                            maconversion = Convert.ToDecimal(row[0].ItemArray[1]);
                            saconversion = Convert.ToDecimal(row[0].ItemArray[2]);
                            #region Conversion and markup
                            try
                            {
                                sellingamt = Convert.ToString(convertedamt(Convert.ToDecimal(amount.ToString("0.##"))));
                                masellingamt = Convert.ToString(maconvertedamt(Convert.ToDecimal(amount.ToString("0.##"))));
                                mamarkup = Convert.ToString(calculatemamarkup(Convert.ToDecimal(amount.ToString("0.##"))));
                                samarkup = Convert.ToString(calculatesamarkup(Convert.ToDecimal(amount.ToString("0.##"))));
                            }
                            catch { }
                            agentcurrency = dtmasacurrency.Rows[0]["SAcrncy"].ToString();
                            #endregion
                        }
                        catch { }
                        #endregion
                        #region bind flights
                        fltresponse.Add(new XElement("Itinerary",
                       new XAttribute("SequenceNumber", i + 1),
                       new XAttribute("trip", trip),
                       //new XAttribute("amount", amount.ToString("0.##")),
                       new XAttribute("supamount", supamount),
                       new XAttribute("maconversionrt", maconversion),
                       new XAttribute("saconversionrt", saconversion),
                       new XAttribute("amount", sellingamt),
                       new XAttribute("maamount", masellingamt),
                       new XAttribute("mamarkup", mamarkup),
                       new XAttribute("samarkup", samarkup),
                       new XAttribute("currencycode", agentcurrency),
                       new XAttribute("ispassportmandatory", "true"),
                       new XAttribute("faretype", faretype),
                       new XAttribute("faresoucecode", faresoucecode),
                       new XAttribute("totalduration", ""),
                       new XAttribute("TicketType", "eTicket"),
                       new XAttribute("isRefundable", isRefundable),
                       new XAttribute("supplierid", "51"),
                       new XAttribute("suppliername", "TBO"),
                       fltsegments(flights[i].Element("Segments").Elements("item").ToList()),
                       new XElement("PriceBreakups", pricebreakup(flights[i].Element("FareBreakdown").Elements("item").ToList()))
                       ));
                        #endregion
                    }
                    catch { }
                }
            }
            catch { }
            return fltresponse;
        }
        private List<XElement> fltsegments(List<XElement> fltsegmt)
        {
            List<XElement> fltseg = new List<XElement>();
            try
            {
                int index = 0;
                foreach (XElement segment in fltsegmt)
                {
                    index++;
                    string searchtype = "Onward";
                    if (index == 2)
                    {
                        searchtype = "Return";
                    }
                    #region segment
                    try
                    {
                        fltseg.Add(new XElement("FlightSegments", new XAttribute("searchtype", searchtype), bindfltsegment(segment.Descendants("item").ToList())));
                    }
                    catch { }
                    #endregion
                }
            }
            catch { }
            return fltseg;
        }
        private List<XElement> bindfltsegment(List<XElement> fltsegmt)
        {
            List<XElement> fltseg = new List<XElement>();
            try
            {
                int index = 0;
                foreach (XElement segment in fltsegmt)
                {
                    index++;
                    int duration = 0;
                    try
                    {
                        TimeSpan ts = TimeSpan.Parse(segment.Descendants("Duration").FirstOrDefault().Value);
                        duration = Convert.ToInt32(ts.TotalMinutes);
                        if (duration <= 0)
                        {
                            TimeSpan ts1 = TimeSpan.Parse(segment.Descendants("AccumulatedDuration").FirstOrDefault().Value);
                            duration = Convert.ToInt32(ts1.TotalMinutes);
                        }
                    }
                    catch { }
                    #region segment
                    try
                    {
                        fltseg.Add(new XElement("Flight",
                            new XAttribute("from", segment.Descendants("Origin").Descendants("AirportCode").FirstOrDefault().Value),
                            new XAttribute("to", segment.Descendants("Destination").Descendants("AirportCode").FirstOrDefault().Value),
                            new XAttribute("Marketingairlinecode", segment.Element("AirlineDetails").Element("AirlineCode").Value),
                            new XAttribute("Operatingairlinecode", segment.Element("AirlineDetails").Element("OperatingCarrier").Value == "" ? segment.Element("AirlineDetails").Element("AirlineCode").Value : segment.Element("AirlineDetails").Element("OperatingCarrier").Value),
                            new XAttribute("Equipment", segment.Descendants("Craft").FirstOrDefault().Value),
                            new XAttribute("airlinenumber", segment.Descendants("FlightNumber").FirstOrDefault().Value),
                            new XAttribute("cabin", segment.Descendants("BookingClass").FirstOrDefault().Value),
                            new XAttribute("departdatetime", segment.Descendants("DepartureTime").FirstOrDefault().Value),
                            new XAttribute("arrivaldatetime", segment.Descendants("ArrivalTime").FirstOrDefault().Value),
                            new XAttribute("eticket", segment.Descendants("ETicketEligible").FirstOrDefault().Value),
                            new XAttribute("duration", duration),
                            new XAttribute("durationtype", "minutes"),
                            new XAttribute("MealCode", segment.Descendants("MealType").FirstOrDefault().Value),
                            new XAttribute("SeatsRemaining", segment.Descendants("NoOfSeatAvailable").FirstOrDefault().Value),
                            new XAttribute("airlinename", segment.Descendants("AirlineDetails").Descendants("AirlineName").FirstOrDefault().Value),
                            new XAttribute("fromcityname", segment.Descendants("Origin").Descendants("CityName").FirstOrDefault().Value),
                            new XAttribute("tocityname", segment.Descendants("Destination").Descendants("CityName").FirstOrDefault().Value),
                            new XAttribute("fromairportname", segment.Descendants("Origin").Descendants("AirportName").FirstOrDefault().Value),
                            new XAttribute("toairportname", segment.Descendants("Destination").Descendants("AirportName").FirstOrDefault().Value),
                            new XAttribute("Distance", segment.Descendants("Mile").FirstOrDefault().Value),
                            new XAttribute("CabinClass", "InformationNotAvailable"),
                            new XAttribute("FareInfoRef", ""),
                            new XAttribute("FareInfoValue", ""),
                            new XAttribute("FareBasisCode", ""),
                            new XAttribute("Baggage", segment.Descendants("IncludedBaggage").FirstOrDefault().Value)
                            ));
                    }
                    catch { }
                    #endregion
                }
            }
            catch { }
            return fltseg;
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
                    string pType = pricebrkups[i].Descendants("PassengerType").FirstOrDefault().Value;
                    string pTypeval = "ADT";
                    if (pType == "2")
                    {
                        pTypeval = "CHD";
                    }
                    else if (pType == "3")
                    {
                        pTypeval = "INF";
                    }
                    List<XElement> taxlst = pricebrkups[i].Descendants("Tax").ToList();
                    int totpass = Convert.ToInt16(pricebrkups[i].Descendants("PassengerCount").FirstOrDefault().Value);
                    #region Conversion and markup
                    decimal sellingamtbf = 0;
                    decimal sellingamtot = 0;
                    decimal sellingamttax = 0;
                    decimal sellingamsrvfee = 0;
                    decimal sellingamtagtmrkup = 0;
                    decimal sellingamotcharge = 0;

                    decimal masellingamtbf = 0;
                    decimal masellingamtot = 0;
                    decimal masellingamttax = 0;
                    decimal masellingamsrvfee = 0;
                    decimal masellingamtagtmrkup = 0;
                    decimal masellingamotcharge = 0;
                    try
                    {
                        sellingamtbf = convertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("BaseFare").FirstOrDefault().Value) / totpass));
                        sellingamtot = convertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("TotalFare").FirstOrDefault().Value) / totpass));
                        sellingamttax = convertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("Tax").FirstOrDefault().Value) / totpass));
                        sellingamsrvfee = convertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("ServiceFee").FirstOrDefault().Value) / totpass));
                        sellingamtagtmrkup = convertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("AgentMarkup").FirstOrDefault().Value) / totpass));
                        sellingamotcharge = convertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("OtherCharges").FirstOrDefault().Value) / totpass));

                        masellingamtbf = maconvertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("BaseFare").FirstOrDefault().Value) / totpass));
                        masellingamtot = maconvertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("TotalFare").FirstOrDefault().Value) / totpass));
                        masellingamttax = maconvertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("Tax").FirstOrDefault().Value) / totpass));
                        masellingamsrvfee = maconvertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("ServiceFee").FirstOrDefault().Value) / totpass));
                        masellingamtagtmrkup = maconvertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("AgentMarkup").FirstOrDefault().Value) / totpass));
                        masellingamotcharge = maconvertedamt(Convert.ToDecimal(Convert.ToDecimal(pricebrkups[i].Descendants("OtherCharges").FirstOrDefault().Value) / totpass));
                    }
                    catch { }
                    #endregion
                    prcbrk.Add(new XElement("PriceBreakup",
                               new XElement("PType", pTypeval),
                               new XElement("PQty", Convert.ToString(pricebrkups[i].Descendants("PassengerCount").FirstOrDefault().Value)),
                               new XElement("BaseFares",
                               new XElement("BaseFare", Convert.ToString(sellingamtbf)),
                                new XElement("maBaseFare", masellingamtbf),
                               //new XElement("Currency", Convert.ToString(pricebrkups[i].Descendants("Currency").FirstOrDefault().Value))
                               new XElement("Currency", Convert.ToString(agentcurrency))
                               ),
                               new XElement("Surchares", null),

                                new XElement("Taxes", new XElement("Tax",
                               new XElement("TaxCode", "Tax"),
                               new XElement("Currency", Convert.ToString(agentcurrency)),
                               new XElement("maAmount", Convert.ToString(masellingamttax)),
                               new XElement("Amount", Convert.ToString(sellingamttax))
                               ),
                               new XElement("Tax",
                               new XElement("TaxCode", "ServiceFee"),
                               new XElement("Currency", Convert.ToString(agentcurrency)),
                               new XElement("maAmount", Convert.ToString(masellingamsrvfee)),
                               new XElement("Amount", Convert.ToString(sellingamsrvfee))
                               ),
                               new XElement("Tax",
                               new XElement("TaxCode", "AgentMarkup"),
                               new XElement("Currency", Convert.ToString(agentcurrency)),
                               new XElement("maAmount", Convert.ToString(masellingamtagtmrkup)),
                               new XElement("Amount", Convert.ToString(sellingamtagtmrkup))
                               ),
                               new XElement("Tax",
                               new XElement("TaxCode", "OtherCharges"),
                               new XElement("Currency", Convert.ToString(agentcurrency)),
                                new XElement("maAmount", Convert.ToString(masellingamotcharge)),
                               new XElement("Amount", Convert.ToString(sellingamotcharge))
                               )
                               ),
                               new XElement("TotalFares",
                                   new XElement("Amount", Convert.ToString(sellingamtot)),
                                   new XElement("maAmount", masellingamtot),
                                   new XElement("Currency", Convert.ToString(agentcurrency))
                                   )
                               )
                        );
                }
                return prcbrk;
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
        #region TBO Air Search Request
        private string fltpricecheckrequest(XElement req)
        {
            string request = string.Empty;
            try
            {
                XElement iternary = req.Descendants("Itinerary").FirstOrDefault();
                tbocommonreq airreq = new tbocommonreq();
                airreq.IPAddress = "49.205.173.6";
                airreq.EndUserBrowserAgent = "Mozilla/5.0(Windows NT 6.1)";
                airreq.PointOfSale = countrycode;
                airreq.RequestOrigin = countryname;
                airreq.TokenId = "f360ef32-07fc-4b80-8b86-358fcfb95f61";
                airreq.TrackingId = iternary.Descendants("SessionId").FirstOrDefault().Value;
                airreq.ResultId = iternary.Descendants("faresoucecode").FirstOrDefault().Value;
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
        ~tbo_pricecheck()
        {
            Dispose(false);
        }
        #endregion
    }
}