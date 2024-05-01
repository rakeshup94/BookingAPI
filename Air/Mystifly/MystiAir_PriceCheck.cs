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
    public class MystiAir_PriceCheck
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
        public XElement AirPriceCheck_mysti(XElement req)
        {
            try
            {
                XElement travayoo_out = null;
                string url = string.Empty;
                string response = string.Empty;
                XElement suppliercred = airsupplier_Cred.getgds_credentials(req.Descendants("PreBookRequest").Attributes("CustomerID").FirstOrDefault().Value, "12");
                url = suppliercred.Descendants("URL").FirstOrDefault().Value;
                string method = suppliercred.Descendants("AirPriceCheck").FirstOrDefault().Value;
                string Target = suppliercred.Descendants("Mode").FirstOrDefault().Value;
                string sessionid = suppliercred.Descendants("sessionid").FirstOrDefault().Value;
                string apireq = apirequest(req, Target, sessionid);
                Mysti_SupplierResponse sup_response = new Mysti_SupplierResponse();
                string customerid = string.Empty;
                string trackno = string.Empty;
                customerid = req.Descendants("PreBookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                trackno = req.Descendants("PreBookRequest").Attributes("TransID").FirstOrDefault().Value;
                response = sup_response.supplierresponse_mystifly(url, apireq, method, "AirPriceCheck", 4, trackno, customerid).ToString();
                #region Get Data from DB
                getcurrencymrkup getcurrencymrk = new getcurrencymrkup();
                List<DataTable> dtconversion = getcurrencymrk.getcurrencyConversion(Convert.ToInt64(req.Descendants("AgentId").FirstOrDefault().Value), "SAAS");
                dtconversionrate = dtconversion[0];
                dtmasacurrency = dtconversion[1];
                dtmarkup = getcurrencymrk.getmarkupdetails(Convert.ToInt64(req.Descendants("AgentId").FirstOrDefault().Value), "12", "SAAS");
                #endregion
                XElement availrsponse = XElement.Parse(response.ToString());
                XElement doc = RemoveAllNamespaces(availrsponse);
                string status = string.Empty;
                try
                {
                    status = doc.Descendants("IsValid").FirstOrDefault().Value;
                }
                catch { }
                airlinexml = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Flight\Mystifly\airlinelist.xml"));
                airportxml = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Flight\Mystifly\airportlist.xml"));
                List<XElement> extraservices = null;
                try
                {
                    extraservices = doc.Descendants("ExtraServices1_1").FirstOrDefault() == null ? null : doc.Descendants("ExtraServices1_1").FirstOrDefault().Descendants("Service").ToList();
                }
                catch { }
                List<XElement> flightlist = FlightList(doc.Descendants("PricedItinerary").ToList(), extraservices);
                travayoo_out = travayooapiresponse(flightlist, req, status);
                return travayoo_out;
            }
            catch(Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "AirPriceCheck_mysti";
                ex1.PageName = "MystiAir_PriceCheck";
                ex1.CustomerID = req.Descendants("PreBookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("PreBookRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        #endregion
        #region Flight List
        public List<XElement> FlightList(List<XElement> fltlist, List<XElement> extraservicebag)
        {
            List<XElement> flightlst = new List<XElement>();
            try
            {
                for (int i = 0; i < fltlist.Count(); i++)
                {
                    try
                    {
                        List<XElement> prclst = null;
                        string seqno = string.Empty;
                        string searchtype = string.Empty;
                        string trip = string.Empty;
                        string supamount = string.Empty;
                        string maconversionrt = string.Empty;
                        string saconversionrt = string.Empty;
                        string amount = string.Empty;
                        string sellingamt = string.Empty;
                        string masellingamt = string.Empty;
                        string mamarkup = string.Empty;
                        string samarkup = string.Empty;
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
                            supamount = amount;
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
                                    masellingamt = Convert.ToString(maconvertedamt(Convert.ToDecimal(amount)));
                                    mamarkup = Convert.ToString(calculatemamarkup(Convert.ToDecimal(amount)));
                                    samarkup = Convert.ToString(calculatesamarkup(Convert.ToDecimal(amount)));
                                    #region Get Total Amount
                                    prclst = pricebreakup(fltlist[i].Descendants("PTC_FareBreakdown").ToList());
                                    decimal totalamt = 0;
                                    decimal matotalamt = 0;
                                    foreach (XElement prc in prclst)
                                    {
                                        if (prc.Element("BaseFares").Name == "BaseFares")
                                        {
                                            totalamt += Convert.ToDecimal(prc.Descendants("BaseFare").FirstOrDefault().Value);
                                            matotalamt += Convert.ToDecimal(prc.Descendants("maBaseFare").FirstOrDefault().Value);
                                        }
                                        if (prc.Element("Surchares").Name == "Surchares")
                                        {
                                            totalamt += Convert.ToDecimal(prc.Descendants("Surcharge").Descendants("Amount").Sum(nd => Decimal.Parse(nd.Value)));
                                            matotalamt += Convert.ToDecimal(prc.Descendants("Surcharge").Descendants("maAmount").Sum(nd => Decimal.Parse(nd.Value)));
                                        }
                                        if (prc.Element("Taxes").Name == "Taxes")
                                        {
                                            totalamt += Convert.ToDecimal(prc.Descendants("Tax").Descendants("Amount").Sum(nd => Decimal.Parse(nd.Value)));
                                            matotalamt += Convert.ToDecimal(prc.Descendants("Tax").Descendants("maAmount").Sum(nd => Decimal.Parse(nd.Value)));
                                        }
                                        totalamt = totalamt * Convert.ToInt32(prc.Descendants("PQty").FirstOrDefault().Value);
                                        matotalamt = matotalamt * Convert.ToInt32(prc.Descendants("PQty").FirstOrDefault().Value);
                                    }
                                    sellingamt = Convert.ToString(totalamt);
                                    masellingamt = Convert.ToString(matotalamt);
                                    #endregion
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
                              new XAttribute("supamount", supamount),
                               new XAttribute("maconversionrt", maconversion),
                               new XAttribute("saconversionrt", saconversion),
                               new XAttribute("amount", sellingamt),
                               new XAttribute("maamount", masellingamt),
                               new XAttribute("mamarkup", mamarkup),
                               new XAttribute("samarkup", samarkup),
                                new XAttribute("currencycode", agentcurrency),
                                 new XAttribute("ispassportmandatory", ispssportmndtry),
                                  new XAttribute("faretype", faretype),
                                  new XAttribute("faresoucecode", faresourcecode),
                                   new XAttribute("totalduration", totalduration),
                                   new XAttribute("TicketType", tickettype),
                                   new XAttribute("isRefundable", isrefundable),
                                   onwordseg,
                                   returnseg,
                                   extrabaggage_binding(extraservicebag),
                                   new XElement("PriceBreakups", prclst)
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
                                   new XAttribute("CabinClass", cabinclass)
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
                        sellingamtbf = convertedamt(Convert.ToDecimal(pricebrkups[i].Descendants("EquivFare").Descendants("Amount").FirstOrDefault().Value));
                        sellingamtot = convertedamt(Convert.ToDecimal(pricebrkups[i].Descendants("TotalFare").Descendants("Amount").FirstOrDefault().Value));
                        masellingamtbf = maconvertedamt(Convert.ToDecimal(pricebrkups[i].Descendants("EquivFare").Descendants("Amount").FirstOrDefault().Value));
                        masellingamtot = maconvertedamt(Convert.ToDecimal(pricebrkups[i].Descendants("TotalFare").Descendants("Amount").FirstOrDefault().Value));

                    }
                    catch { }
                    #endregion
                    List<XElement> taxlst = pricebrkups[i].Descendants("Tax").ToList();
                    prcbrk.Add(new XElement("PriceBreakup",
                               new XElement("PType", Convert.ToString(pricebrkups[i].Descendants("PassengerTypeQuantity").Descendants("Code").FirstOrDefault().Value)),
                               new XElement("PQty", Convert.ToString(pricebrkups[i].Descendants("PassengerTypeQuantity").Descendants("Quantity").FirstOrDefault().Value)),
                               new XElement("BaseFares",
                               new XElement("BaseFare", sellingamtbf),
                               new XElement("maBaseFare", masellingamtbf),
                               new XElement("Currency", agentcurrency)
                               ),
                               //new XElement("Surchares", Convert.ToString(pricebrkups[i].Descendants("Surcharges").FirstOrDefault().Value)),
                               new XElement("Surchares",surcharges(pricebrkups[i].Descendants("Surcharge").ToList(),pricebrkups[i].Descendants("EquivFare").Descendants("CurrencyCode").FirstOrDefault().Value)),
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
                        sellingamttax = convertedamt(Convert.ToDecimal(taxes[i].Descendants("Amount").FirstOrDefault().Value));
                        masellingamttax = maconvertedamt(Convert.ToDecimal(taxes[i].Descendants("Amount").FirstOrDefault().Value));
                    }
                    catch { }
                    #endregion
                    taxbrkup.Add(new XElement("Tax",
                               new XElement("TaxCode", Convert.ToString(taxes[i].Descendants("TaxCode").FirstOrDefault().Value)),
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
                        masellingamtsrv = maconvertedamt(Convert.ToDecimal(surchargs[i].Descendants("Amount").FirstOrDefault().Value));
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
        #region Extra Baggage Binding
        private XElement extrabaggage_binding(List<XElement> baggages)
        {
            try
            {
                XElement extbag = null;
                List<XElement> exbag = new List<XElement>();
                for (int i = 0; i < baggages.Count(); i++)
                {
                    #region Conversion and markup
                    decimal sellingamtbag = 0;
                    decimal masellingamtbag = 0;
                    string description = string.Empty;
                    string mamarkup = string.Empty;
                    string samarkup = string.Empty;
                    string supamount = string.Empty;
                    try
                    {
                        supamount = Convert.ToString(Convert.ToDecimal(baggages[i].Descendants("Amount") == null ? "0" : baggages[i].Descendants("Amount").FirstOrDefault().Value));
                        sellingamtbag = convertedamt(Convert.ToDecimal(baggages[i].Descendants("Amount") == null ? "0" : baggages[i].Descendants("Amount").FirstOrDefault().Value));
                        masellingamtbag = maconvertedamt(Convert.ToDecimal(baggages[i].Descendants("Amount") == null ? "0" : baggages[i].Descendants("Amount").FirstOrDefault().Value));

                        mamarkup = Convert.ToString(calculatemamarkup(Convert.ToDecimal(baggages[i].Descendants("Amount") == null ? "0" : baggages[i].Descendants("Amount").FirstOrDefault().Value)));
                        samarkup = Convert.ToString(calculatesamarkup(Convert.ToDecimal(baggages[i].Descendants("Amount") == null ? "0" : baggages[i].Descendants("Amount").FirstOrDefault().Value)));
                        try
                        {
                            var desc = Convert.ToString(baggages[i].Descendants("Description") == null ? "" : baggages[i].Descendants("Description").FirstOrDefault().Value).Split(new string[] { "||" }, StringSplitOptions.None);
                            description = desc[0].ToString() + " || " + sellingamtbag + " " + agentcurrency;
                        }
                        catch { description = Convert.ToString(baggages[i].Descendants("Description") == null ? "" : baggages[i].Descendants("Description").FirstOrDefault().Value); }
                    }
                    catch { }
                    #endregion
                    exbag.Add(new XElement("Baggage",
                               new XAttribute("ServiceId", Convert.ToString(baggages[i].Descendants("ServiceId") == null ? "" : baggages[i].Descendants("ServiceId").FirstOrDefault().Value)),
                               new XAttribute("Type", Convert.ToString(baggages[i].Descendants("Type") == null ? "" : baggages[i].Descendants("Type").FirstOrDefault().Value)),
                               new XAttribute("Behavior", Convert.ToString(baggages[i].Descendants("Behavior") == null ? "" : baggages[i].Descendants("Behavior").FirstOrDefault().Value)),
                               new XAttribute("checkinType", Convert.ToString(baggages[i].Descendants("CheckInType") == null ? "" : baggages[i].Descendants("CheckInType").FirstOrDefault().Value)),
                               new XAttribute("Description", Convert.ToString(description)),
                               new XAttribute("supDescription", Convert.ToString(baggages[i].Descendants("Description") == null ? "" : baggages[i].Descendants("Description").FirstOrDefault().Value)),
                               new XAttribute("CurrencyCode", agentcurrency),
                               new XAttribute("supAmount", supamount),
                               new XAttribute("Amount", sellingamtbag),
                               new XAttribute("maAmount", masellingamtbag),
                                new XAttribute("mamarkup", mamarkup),
                                 new XAttribute("samarkup", samarkup)
                               )
                        );
                }
                if(exbag.Count()>0)
                {
                    extbag = new XElement("ExtraBaggage", exbag);
                }
                return extbag;
            }
            catch { return null; }
        }
        #endregion
        #region api response
        public XElement travayooapiresponse(List<XElement> fltresponse, XElement req, string status)
        {
            string username = req.Descendants("UserName").FirstOrDefault().Value;
            string password = req.Descendants("Password").FirstOrDefault().Value;
            string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
            string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
            string sessionid = req.Descendants("SessionId").FirstOrDefault().Value;
            string isavailable = "false";
            try
            {
                if (fltresponse != null)
                {
                    isavailable = "true";
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
                                          fltresponse
                                          )
                         ))));
            return searchdoc;
        }
        #endregion
        #region API Request
        public string apirequest(XElement req, string mode, string sessionid)
        {
            string faresourcecode = string.Empty;
            //string sessionid = string.Empty;
            faresourcecode = req.Descendants("faresoucecode").FirstOrDefault().Value;
            manage_session session_mgmt = new manage_session();
            //sessionid = session_mgmt.session_manage(req.Descendants("PreBookRequest").Attributes("CustomerID").FirstOrDefault().Value, req.Descendants("PreBookRequest").Attributes("TransID").FirstOrDefault().Value); 
            string request = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:mys='Mystifly.OnePoint' xmlns:mys1='http://schemas.datacontract.org/2004/07/Mystifly.OnePoint'>" +
                                "<soapenv:Header/>" +
                                    "<soapenv:Body>" +
                                       "<mys:AirRevalidate>" +
                                             "<mys:rq>" +
                                              "<mys1:FareSourceCode>" + faresourcecode + "</mys1:FareSourceCode>" +
                                              "<mys1:SessionId>" + sessionid + "</mys1:SessionId>" +
                                              "<mys1:Target>" + mode + "</mys1:Target>" +
                                          "</mys:rq>" +
                                      "</mys:AirRevalidate>" +
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