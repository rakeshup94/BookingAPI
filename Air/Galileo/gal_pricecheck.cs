using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Models.Common;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.Galileo
{
    #region Price Check (Galileo)
    public class gal_pricecheck : IDisposable
    {
        #region Staticdata
        private XElement airlinexml;
        private XElement airportxml;
        public XElement commonrequest = null;
        #endregion
        gal_supresponse api_resp;
        #region api response
        private XElement travayooapiresponse(List<XElement> fltresponse, XElement req, string sessionid)
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
                    if (fltresponse != null)
                    {
                        if (fltresponse.Count() > 0)
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
                ex1.PageName = "gal_pricecheck";
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
                                              new XAttribute("isValid", "false")
                                              )
                             ))));
                return searchdoc;
                #endregion
            }
        }
        #endregion
        #region Price Check Response
        public XElement pricecheckgal_response(XElement req)
        {
            #region Price Check Response (Gal)
            try
            {
                commonrequest = req;
                api_resp = new gal_supresponse();
                string api_response = api_resp.gal_apiresponse(req, pricecheck_request(req).ToString(), "AirPriceCheck", 4, req.Descendants("PreBookRequest").FirstOrDefault().Attribute("TransID").Value, req.Descendants("PreBookRequest").FirstOrDefault().Attribute("CustomerID").Value);
                XElement response = XElement.Parse(api_response);
                XElement supresp = RemoveAllNamespaces(response);
                airlinexml = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Flight\Mystifly\airlinelist.xml"));
                airportxml = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Flight\Mystifly\airportlist.xml"));
                XElement travayoo_out = travayooapiresponse(gal_response(supresp), req, "123456");
                return travayoo_out;
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "pricecheckgal_response";
                ex1.PageName = "gal_pricecheck";
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
                #endregion
            }
            #endregion
        }
        #endregion        
        #region response
        private List<XElement> gal_response(XElement response)
        {
            List<XElement> fltresponse = new List<XElement>();
            List<XElement> fltsegment = null;
            List<XElement> fltlstinfo = null;
            List<XElement> airpricepoint = null;
            //List<XElement> airpricepoint = response.Descendants("AirPricingSolution").ToList();
            List<string> keyList = commonrequest.Descendants("Flight").Attributes("FareBasisCode").Select(x => x.Value).Distinct().ToList();
            try
            {
                airpricepoint = response.Descendants("AirPricingSolution").Where(x => keyList.Contains(x.Descendants("FareInfo").FirstOrDefault().Attribute("FareBasis").Value)).ToList();
                if (airpricepoint.ToList().Count == 0)
                {
                    airpricepoint = response.Descendants("AirPricingSolution").ToList();
                }
            }
            catch { airpricepoint = response.Descendants("AirPricingSolution").ToList(); }
            int i = 1;
            string airlinename = string.Empty;
            string fromcityname = string.Empty;
            string tocityname = string.Empty;
            string fromairportname = string.Empty;
            string toairportname = string.Empty;
            foreach (XElement airpricepnt in airpricepoint)
            {
                fltsegment = new List<XElement>();
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
                #region Segments
                fltlstinfo = new List<XElement>();
                foreach (XElement bookinfo in airpricepnt.Descendants("AirPricingInfo").FirstOrDefault().Descendants("BookingInfo"))
                {
                    #region Flight
                    List<XElement> airsegments = response.Descendants("AirSegment").Where(x => x.Attribute("Key").Value == bookinfo.Attribute("SegmentRef").Value).ToList();
                    string farebasiscode = string.Empty;
                    string fareinfovalue = string.Empty;
                    XElement fareinfo = response.Descendants("FareInfo").Where(x => x.Attribute("Key").Value == bookinfo.Attribute("FareInfoRef").Value).FirstOrDefault();
                    farebasiscode = fareinfo.Attribute("FareBasis").Value;
                    try
                    {
                        fareinfovalue = fareinfo.Descendants("FareRuleKey").FirstOrDefault().Value;
                    }
                    catch { }
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
                                           new XAttribute("cabin", airsegments.Attributes("AvailabilitySource").FirstOrDefault() != null ? airsegments.Attributes("AvailabilitySource").FirstOrDefault().Value : ""),
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
                                    new XAttribute("FareBasisCode", farebasiscode),
                                    new XAttribute("FareInfoValue", fareinfovalue),
                                    new XAttribute("SegmentRef", bookinfo.Attribute("SegmentRef") != null ? bookinfo.Attribute("SegmentRef").Value : ""),
                                    new XAttribute("AvailabilityDisplayType", airsegments.Attributes("AvailabilityDisplayType").FirstOrDefault().Value),
                                     new XAttribute("Group", airsegments.Attributes("Group").FirstOrDefault().Value)
                                    ));
                    #endregion
                }
                XElement fltsectors = new XElement("FlightSegments", fltlstinfo);
                List<XElement> onwardtrip = fltsectors.Descendants("Flight").Where(x => x.Attribute("Group").Value == "0").ToList();
                List<XElement> returntrip = fltsectors.Descendants("Flight").Where(x => x.Attribute("Group").Value == "1").ToList();
                fltsegment.Add(new XElement("FlightSegments", new XAttribute("searchtype", "Onward"), new XAttribute("index", "0"), onwardtrip));
                if (returntrip.Count() != 0)
                {
                    fltsegment.Add(new XElement("FlightSegments", new XAttribute("searchtype", "Return"), new XAttribute("index", "1"), returntrip));
                }
                #endregion
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
                    fltsegment,
                    new XElement("PriceBreakups", pricebreakup(airpricepnt.Descendants("AirPricingInfo").ToList()))
                    ));
                #endregion
                ++i;
            }
            return fltresponse;
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
        #region Price Check Request
        private XElement pricecheck_request(XElement request)
        {
            #region Price Check Request (GAL)
            XElement suppliercred = airsupplier_Cred.getgds_credentials(request.Descendants("PreBookRequest").FirstOrDefault().Attribute("CustomerID").Value, request.Descendants("supplierdetail").FirstOrDefault().Attribute("supplierid").Value);
            string trgtbranch = suppliercred.Descendants("branch").FirstOrDefault().Value;
            string authorizedby = suppliercred.Descendants("authorizedby").FirstOrDefault().Value;
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace ns = "http://www.travelport.com/schema/air_v42_0";
            XNamespace ns1 = "http://www.travelport.com/schema/common_v42_0";
            #region Bind paxes
            List<XElement> adults = new List<XElement>();
            List<XElement> children = new List<XElement>();
            List<XElement> infants = new List<XElement>();
            for (int i = 0; i < Convert.ToInt32(request.Descendants("Occupancy").FirstOrDefault().Attribute("adult").Value); i++)
            {
                adults.Add(new XElement(ns1 + "SearchPassenger", new XAttribute("Code", "ADT"), new XAttribute("BookingTravelerRef", "ingeniumadt" + i + 1), new XAttribute("Key", "ingeniumadt" + i + 1)));
            }
            for (int i = 0; i < Convert.ToInt32(request.Descendants("Occupancy").FirstOrDefault().Attribute("child").Value); i++)
            {
                children.Add(new XElement(ns1 + "SearchPassenger", new XAttribute("Code", "CNN"), new XAttribute("Age", "10"), new XAttribute("BookingTravelerRef", "ingeniumchd" + i + 1), new XAttribute("Key", "ingeniumchd" + i + 1)));
            }
            for (int i = 0; i < Convert.ToInt32(request.Descendants("Occupancy").FirstOrDefault().Attribute("infant").Value); i++)
            {
                infants.Add(new XElement(ns1 + "SearchPassenger", new XAttribute("Code", "INF"), new XAttribute("Age", "1"), new XAttribute("BookingTravelerRef", "ingeniuminf" + i + 1), new XAttribute("Key", "ingeniuminf" + i + 1)));
            }
            #endregion
            #region Segments
            XElement airsgmnt = airsements(request.Descendants("FlightSegments").ToList());
            #endregion
            #region Request
            XElement common_request = new XElement(soap + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                        new XElement(soap + "Body",
                                        new XElement(ns + "AirPriceReq",
                                            new XAttribute("AuthorizedBy", authorizedby),
                                            new XAttribute("TraceId", request.Descendants("PreBookRequest").FirstOrDefault().Attribute("TransID").Value),
                                            new XAttribute("TargetBranch", trgtbranch),
                                            new XElement(ns1 + "BillingPointOfSaleInfo",
                                                new XAttribute("OriginApplication", "uAPI")),
                                                new XElement(ns + "AirItinerary", airsgmnt.Descendants(ns + "AirSegment").ToList()),
                                                new XElement(ns + "AirPricingModifiers",
                                                    new XAttribute("InventoryRequestType", "DirectAccess"),
                                                        new XElement(ns + "BrandModifiers",
                                                            new XAttribute("ModifierType", "FareFamilyDisplay"))),
                                                            adults, children, infants,
                                                            new XElement(ns + "AirPricingCommand", airsgmnt.Descendants(ns + "AirSegmentPricingModifiers").ToList()),
                                                            new XElement(ns1 + "FormOfPayment", new XAttribute("Type", "Credit"))
                                                            )));
            //common_request = XElement.Load(HttpContext.Current.Server.MapPath(@"~\bookresponse.xml"));

            return common_request;
            #endregion
            #endregion
        }
        #endregion
        #region Air Segment (request)
        private XElement airsements(List<XElement> segments)
        {
            try
            {
                XNamespace ns = "http://www.travelport.com/schema/air_v42_0";
                List<XElement> segmentlst = new List<XElement>();
                List<XElement> airprclst = new List<XElement>();
                foreach (XElement segmnt in segments.Descendants("Flight").ToList())
                {
                    segmentlst.Add(new XElement(ns + "AirSegment",
                        new XAttribute("Key", segmnt.Attribute("SegmentRef").Value),
                        new XAttribute("AvailabilitySource", segmnt.Attribute("cabin").Value),
                        new XAttribute("Equipment", segmnt.Attribute("Equipment").Value),
                        new XAttribute("AvailabilityDisplayType", segmnt.Attribute("AvailabilityDisplayType").Value),
                        new XAttribute("Group", segmnt.Parent.Attribute("searchtype").Value == "Onward" ? "0" : "1"),
                        new XAttribute("Carrier", segmnt.Attribute("Marketingairlinecode").Value),
                        new XAttribute("FlightNumber", segmnt.Attribute("airlinenumber").Value),
                        new XAttribute("Origin", segmnt.Attribute("from").Value),
                        new XAttribute("Destination", segmnt.Attribute("to").Value),
                        new XAttribute("DepartureTime", segmnt.Attribute("departdatetime").Value),
                        new XAttribute("ArrivalTime", segmnt.Attribute("arrivaldatetime").Value),
                        new XAttribute("FlightTime", segmnt.Attribute("duration").Value),
                        new XAttribute("Distance", segmnt.Attribute("Distance").Value),
                        new XAttribute("ProviderCode", "1G"),
                        new XAttribute("ClassOfService", segmnt.Attribute("BookingCode").Value)
                        ));
                    airprclst.Add(new XElement(ns + "AirSegmentPricingModifiers",
                        new XAttribute("AirSegmentRef", segmnt.Attribute("SegmentRef").Value),
                        //new XAttribute("FareBasisCode", segmnt.Attribute("FareBasisCode").Value),
                        new XElement(ns + "PermittedBookingCodes",
                            new XElement(ns + "BookingCode", new XAttribute("Code", segmnt.Attribute("BookingCode").Value))
                            )
                        ));
                }
                XElement semnt = new XElement("Segments", segmentlst, airprclst);
                return semnt;
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
        ~gal_pricecheck()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion
}