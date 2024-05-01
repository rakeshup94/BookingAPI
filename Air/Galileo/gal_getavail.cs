using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Models.Common;

namespace TravillioXMLOutService.Air.Galileo
{
    #region Availability (Galileo)
    public class gal_getavail : IDisposable
    {
        #region Staticdata
        public XElement airlinexml;
        public XElement airportxml;
        gal_supresponse api_resp;
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
        #region Availability Response
        public XElement avail_response(XElement req)
        {
            #region Availability Response (Gal)
            api_resp = new gal_supresponse();
            string api_response = api_resp.gal_apiresponse(req, avail_request(req).ToString(), "AirSearch", 1, req.Descendants("SearchRequest").FirstOrDefault().Attribute("TransID").Value, req.Descendants("SearchRequest").FirstOrDefault().Attribute("CustomerID").Value);
            XElement response = XElement.Parse(api_response);
            XElement supresp = RemoveAllNamespaces(response);
            airlinexml = air_staticData.air_airlinexml();
            airportxml = air_staticData.air_airportxml();
            XElement travayoo_out = travayooapiresponse(gal_response(supresp), req, "123456");
            return travayoo_out;
            #endregion
        }
        #endregion
        #region Bind Flights
        private List<XElement> gal_response(XElement response)
        {
            List<XElement> fltresponse = new List<XElement>();
            List<XElement> fltsegment = null;
            List<XElement> fltlstinfo = null;
            List<XElement> airpricepoint = response.Descendants("AirPricePoint").ToList();
            int i = 1;
            string airlinename = string.Empty;
            string fromcityname = string.Empty;
            string tocityname = string.Empty;
            string fromairportname = string.Empty;
            string toairportname = string.Empty;
            foreach (XElement airpricepnt in airpricepoint)
            {
                fltsegment = new List<XElement>();
                int segment = 0;
                #region Refundable/Non-Refundable and ETicketability
                string refunable = "No";
                string eticketability = "false";
                XAttribute refund = airpricepnt.Descendants("AirPricingInfo").FirstOrDefault().Attribute("Refundable");
                XAttribute etktability = airpricepnt.Descendants("AirPricingInfo").FirstOrDefault().Attribute("ETicketability");
                if (refund != null)
                {
                    if (refund.Value == "true")
                        refunable = "Yes";
                }
                if (etktability != null)
                {
                    if (etktability.Value == "Yes")
                        eticketability = "true";
                }
                #endregion
                foreach (XElement flightoptions in airpricepnt.Descendants("AirPricingInfo").FirstOrDefault().Descendants("FlightOption").ToList())
                {
                    #region Segments
                    int index = 0;
                    foreach (XElement options in flightoptions.Descendants("Option"))
                    {
                        fltlstinfo = new List<XElement>();
                        foreach (XElement bookinfo in options.Descendants("BookingInfo"))
                        {
                            #region Flight
                            List<XElement> airsegments = response.Descendants("AirSegment").Where(x => x.Attribute("Key").Value == bookinfo.Attribute("SegmentRef").Value
                                && Convert.ToInt32(x.Attribute("Group").Value) == segment).ToList();
                            #region Fare Info Value
                            string fareinfovalue = string.Empty;
                            string baggage = string.Empty;
                            string farebasiscode = string.Empty;
                            try
                            {
                                XElement fareinfo = response.Descendants("FareInfo").Where(x => x.Attribute("Key").Value == bookinfo.Attribute("FareInfoRef").Value).FirstOrDefault();
                                fareinfovalue = fareinfo.Descendants("FareRuleKey").FirstOrDefault().Value;
                                farebasiscode = fareinfo.Attribute("FareBasis").Value;
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
                            try
                            {
                                XElement fromairportlst = airportxml.Descendants("record").Where(x => x.Descendants("AirportCode").FirstOrDefault().Value == airsegments.Attributes("Origin").FirstOrDefault().Value).FirstOrDefault();
                                fromairportname = fromairportlst.Descendants("AirPortName").FirstOrDefault().Value;
                                fromcityname = fromairportlst.Descendants("cityName").FirstOrDefault().Value;
                                XElement toairportlst = airportxml.Descendants("record").Where(x => x.Descendants("AirportCode").FirstOrDefault().Value == airsegments.Attributes("Destination").FirstOrDefault().Value).FirstOrDefault();
                                toairportname = toairportlst.Descendants("AirPortName").FirstOrDefault().Value;
                                tocityname = toairportlst.Descendants("cityName").FirstOrDefault().Value;
                                XElement airlinelst = airlinexml.Descendants("Airline").Where(x => x.Descendants("airlinecode").FirstOrDefault().Value == airsegments.Attributes("Carrier").FirstOrDefault().Value).FirstOrDefault();
                                airlinename = airlinelst.Descendants("airlinename").FirstOrDefault().Value;
                            }
                            catch { }
                            fltlstinfo.Add(new XElement("Flight",
                                             new XAttribute("from", airsegments.Attributes("Origin").FirstOrDefault().Value),
                                              new XAttribute("to", airsegments.Attributes("Destination").FirstOrDefault().Value),
                                               new XAttribute("Marketingairlinecode", airsegments.Attributes("Carrier").FirstOrDefault().Value),
                                                new XAttribute("Operatingairlinecode", airsegments.Attributes("Carrier").FirstOrDefault().Value),
                                                 new XAttribute("Equipment", airsegments.Attributes("Equipment").FirstOrDefault().Value),
                                                  new XAttribute("airlinenumber", airsegments.Attributes("FlightNumber").FirstOrDefault().Value),
                                                   new XAttribute("cabin", airsegments.Attributes("AvailabilitySource").FirstOrDefault().Value),
                                                    new XAttribute("departdatetime", airsegments.Attributes("DepartureTime").FirstOrDefault().Value),
                                                     new XAttribute("arrivaldatetime", airsegments.Attributes("ArrivalTime").FirstOrDefault().Value),
                                                      new XAttribute("eticket", eticketability),
                                                       new XAttribute("duration", airsegments.Attributes("FlightTime").FirstOrDefault().Value),
                                                        new XAttribute("durationtype", "minutes"),
                                                        new XAttribute("MealCode", ""),
                                                        new XAttribute("SeatsRemaining", bookinfo.Attribute("BookingCount") != null ? bookinfo.Attribute("BookingCount").Value : ""),
                                                        new XAttribute("airlinename", airlinename),
                                                        new XAttribute("fromcityname", fromcityname),
                                                        new XAttribute("tocityname", tocityname),
                                                        new XAttribute("fromairportname", fromairportname),
                                                        new XAttribute("toairportname", toairportname),
                                            new XAttribute("Distance", airsegments.Attributes("Distance").FirstOrDefault().Value),
                                            new XAttribute("BookingCode", bookinfo.Attribute("BookingCode") != null ? bookinfo.Attribute("BookingCode").Value : ""),
                                            new XAttribute("CabinClass", bookinfo.Attribute("CabinClass") != null ? bookinfo.Attribute("CabinClass").Value : ""),
                                            new XAttribute("FareInfoRef", bookinfo.Attribute("FareInfoRef") != null ? bookinfo.Attribute("FareInfoRef").Value : ""),
                                            new XAttribute("FareInfoValue", fareinfovalue),
                                            new XAttribute("FareBasisCode", farebasiscode),
                                            new XAttribute("Baggage", baggage),
                                            new XAttribute("SegmentRef", bookinfo.Attribute("SegmentRef") != null ? bookinfo.Attribute("SegmentRef").Value : ""),
                                            new XAttribute("AvailabilityDisplayType", airsegments.Attributes("AvailabilityDisplayType").FirstOrDefault().Value)
                                            ));
                            #endregion
                        }
                        fltsegment.Add(new XElement("FlightSegments", new XAttribute("searchtype", segment == 0 ? "Onward" : "Return"), new XAttribute("index", index), fltlstinfo));
                        ++index;
                    }
                    ++segment;
                    #endregion
                }
                #region Iternary
                fltresponse.Add(new XElement("Itinerary",
                    new XAttribute("SequenceNumber", i),
                    new XAttribute("trip", "Return"),
                    new XAttribute("amount", airpricepnt.Attribute("TotalPrice").Value.Remove(0, 3)),
                    new XAttribute("currencycode", airpricepnt.Attribute("TotalPrice").Value.Substring(0, 3)),
                    new XAttribute("ispassportmandatory", "true"),
                    new XAttribute("faretype", "Public"),
                    new XAttribute("faresoucecode", "test"),
                    new XAttribute("totalduration", ""),
                    new XAttribute("TicketType", "eTicket"),
                    new XAttribute("isRefundable", refunable),
                    new XAttribute("supplierid", "50"),
                    new XAttribute("suppliername", "Travelport"),
                    fltsegment,
                    new XElement("PriceBreakups",pricebreakup(airpricepnt.Descendants("AirPricingInfo").ToList()))
                    ));
                #endregion
                ++i;
            }
            return fltresponse;
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
        #region Taxes
        public List<XElement> taxes(List<XElement> taxes)
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
        #region Availability Request
        private XElement avail_request(XElement request)
        {
            #region Availability Request (GAL)
            XElement suppliercred = airsupplier_Cred.getgds_credentials(request.Descendants("SearchRequest").FirstOrDefault().Attribute("CustomerID").Value, "50");
            string trgtbranch = suppliercred.Descendants("branch").FirstOrDefault().Value;
            string authorizedby = suppliercred.Descendants("authorizedby").FirstOrDefault().Value;
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace ns = "http://www.travelport.com/schema/air_v42_0";
            XNamespace ns1 = "http://www.travelport.com/schema/common_v42_0";
            string trip = request.Descendants("Itinerary").FirstOrDefault().Attribute("code").Value;
            XElement roundtrip = null;
            XElement airsearchModifier = null;
            string preferredairlines = string.Empty;
            string classtype = string.Empty;
            XElement PreferredCarriers = null;
            XElement PreferredCabins = null;
            #region Bind RoundTrip
            #region Cabin
            try
            {
                string classname = request.Descendants("Class").FirstOrDefault().Value;
                if (classname != "")
                {
                    if (classname == "Economy")
                    {
                        classtype = "Economy";
                        PreferredCabins = new XElement(ns + "AirLegModifiers", new XElement(ns + "PreferredCabins", new XElement(ns1 + "CabinClass", new XAttribute("Type", classtype))));
                    }
                    else if (classname == "PremiumEconomy")
                    {
                        classtype = "PremiumEconomy";
                        PreferredCabins = new XElement(ns + "AirLegModifiers", new XElement(ns + "PreferredCabins", new XElement(ns1 + "CabinClass", new XAttribute("Type", classtype))));
                    }
                    else if (classname == "Business")
                    {
                        classtype = "Business";
                        PreferredCabins = new XElement(ns + "AirLegModifiers", new XElement(ns + "PreferredCabins", new XElement(ns1 + "CabinClass", new XAttribute("Type", classtype))));
                    }
                    else if (classname == "First")
                    {
                        classtype = "First";
                        PreferredCabins = new XElement(ns + "AirLegModifiers", new XElement(ns + "PreferredCabins", new XElement(ns1 + "CabinClass", new XAttribute("Type", classtype))));
                    }
                }
            }
            catch { }
            #endregion
            if (trip == "2")
            {
                roundtrip = new XElement(ns + "SearchAirLeg",
                                                    new XElement(ns + "SearchOrigin",
                                                        new XElement(ns1 + "CityOrAirport",
                                                            new XAttribute("Code", request.Descendants("Destination").FirstOrDefault().Value),
                                                            new XAttribute("PreferCity", true))),
                                                            new XElement(ns + "SearchDestination",
                                                                new XElement(ns1 + "CityOrAirport",
                                                                    new XAttribute("Code", request.Descendants("Origin").FirstOrDefault().Value),
                                                                    new XAttribute("PreferCity", true))),
                                                                    new XElement(ns + "SearchDepTime",
                                                                        new XAttribute("PreferredTime", convertdate(request.Descendants("ReturnDate").FirstOrDefault().Value))), PreferredCabins);
            }
            #endregion
            #region Bind paxes
            List<XElement> adults = new List<XElement>();
            List<XElement> children = new List<XElement>();
            List<XElement> infants = new List<XElement>();
            for (int i = 0; i < Convert.ToInt32(request.Descendants("Occupancy").FirstOrDefault().Attribute("adult").Value); i++)
            {
                adults.Add(new XElement(ns1 + "SearchPassenger", new XAttribute("Code", "ADT")));
            }
            for (int i = 0; i < Convert.ToInt32(request.Descendants("Occupancy").FirstOrDefault().Attribute("child").Value); i++)
            {
                children.Add(new XElement(ns1 + "SearchPassenger", new XAttribute("Code", "CNN"), new XAttribute("Age", "10"), new XAttribute("DOB", DateTime.UtcNow.AddYears(-10).ToString("yyyy-MM-dd"))));
            }
            for (int i = 0; i < Convert.ToInt32(request.Descendants("Occupancy").FirstOrDefault().Attribute("infant").Value); i++)
            {
                infants.Add(new XElement(ns1 + "SearchPassenger", new XAttribute("Code", "INF"), new XAttribute("Age", "1"), new XAttribute("DOB", DateTime.UtcNow.AddYears(-1).ToString("yyyy-MM-dd"))));
            }
            #endregion
            #region NonStop
            try
            {
                if (request.Descendants("NonStop").FirstOrDefault().Value == "true")
                {
                    airsearchModifier = new XElement(ns + "FlightType", new XAttribute("NonStopDirects", true));
                }
            }
            catch { }
            #endregion
            #region Preferred Airline
            try
            {
                string preferredflt = request.Descendants("PreferredAirline").FirstOrDefault().Value;
                if (preferredflt.Length > 0)
                {
                    preferredairlines = preferredflt;
                    PreferredCarriers = new XElement(ns + "PreferredCarriers", new XElement(ns1 + "Carrier", new XAttribute("Code", preferredflt)));
                }
            }
            catch { }
            #endregion
            #region Request
            XElement common_request = new XElement(soap + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                        new XElement(soap + "Body",
                                        new XElement(ns + "LowFareSearchReq",
                                            new XAttribute("AuthorizedBy", authorizedby),
                                            new XAttribute("TraceId", request.Descendants("SearchRequest").FirstOrDefault().Attribute("TransID").Value),
                                            new XAttribute("TargetBranch", trgtbranch),
                                            new XAttribute("ReturnUpsellFare", true),
                                            new XElement(ns1 + "BillingPointOfSaleInfo",
                                                new XAttribute("OriginApplication", "uAPI")),
                                                new XElement(ns + "SearchAirLeg",
                                                    new XElement(ns + "SearchOrigin",
                                                        new XElement(ns1 + "CityOrAirport",
                                                            new XAttribute("Code", request.Descendants("Origin").FirstOrDefault().Value),
                                                            new XAttribute("PreferCity", true))),
                                                            new XElement(ns + "SearchDestination",
                                                                new XElement(ns1 + "CityOrAirport",
                                                                    new XAttribute("Code", request.Descendants("Destination").FirstOrDefault().Value),
                                                                    new XAttribute("PreferCity", true))),
                                                                    new XElement(ns + "SearchDepTime",
                                                                        new XAttribute("PreferredTime", convertdate(request.Descendants("DepartDate").FirstOrDefault().Value))), PreferredCabins),
                                                                        roundtrip,
                                                                         new XElement(ns + "AirSearchModifiers",
                                                    new XElement(ns + "PreferredProviders",
                                                        new XElement(ns1 + "Provider",
                                                            new XAttribute("Code", "1G"))), PreferredCarriers, airsearchModifier),
                                                            adults, children, infants,
                                                new XElement(ns + "AirPricingModifiers",
                                                    new XElement(ns + "AccountCodes",
                                                        new XElement(ns1 + "AccountCode",
                                                            new XAttribute("Code", "-")))))));

            return common_request;
            #endregion
            #endregion
        }
        #endregion
        #region Depart/Return Date
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
        ~gal_getavail()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion
}