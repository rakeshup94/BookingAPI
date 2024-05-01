using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Common.DotW;
using System.Globalization;

namespace TravillioXMLOutService.Supplier.XMLOUTAPI.CXLPolicy
{
    public class xmloutCXLPolicy : IDisposable
    {
        #region Global vars
        string customerid = string.Empty;
        string dmc = string.Empty;
        int SupplierId = 501;
        string sessionKey = string.Empty;
        string sourcekey = string.Empty;
        string publishedkey = string.Empty;
        XElement reqTravayoo = null;
        string currencycode = string.Empty;
        #endregion
        #region CXL Policy (XML OUT API)
        public XElement cxlpolicy_beOUT(XElement req)
        {
            XElement cxlpolicyresp = null;
            reqTravayoo = req;
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SupplierID").Single().Value;
            try
            {
                #region Request
                customerid = req.Descendants("CustomerID").FirstOrDefault().Value;
                currencycode = req.Descendants("CurrencyName").FirstOrDefault().Value;
                XElement request = new XElement("checkRateRQ",
                    new XAttribute("customerID", customerid),
                    new XAttribute("agencyID", req.Descendants("SubAgentID").FirstOrDefault().Value),
                    new XAttribute("sessionID", req.Descendants("TransID").FirstOrDefault().Value),
                    new XAttribute("sourceMarket", req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                    new XAttribute("cityID", req.Descendants("CityID").FirstOrDefault().Value),
                    new XAttribute("cityCode", req.Descendants("CityCode").FirstOrDefault().Value),
                    new XElement("hotels", new XAttribute("checkIn", req.Descendants("FromDate").FirstOrDefault().Value), new XAttribute("checkOut", req.Descendants("ToDate").FirstOrDefault().Value),
                    new XElement("occupancies", bindoccupancy(req.Descendants("RoomPax").ToList())),
                        new XElement("hotel",
                            new XAttribute("appkey", req.Descendants("SupplierID").FirstOrDefault().Value),
                            new XAttribute("code", req.Descendants("HotelID").FirstOrDefault().Value),
                            new XAttribute("name", req.Descendants("HotelName").FirstOrDefault().Value),
                            new XAttribute("categoryName", req.Descendants("MaxStarRating").FirstOrDefault().Value),
                            new XAttribute("cityCode", req.Descendants("CityCode").FirstOrDefault().Value),
                            new XAttribute("cityName", req.Descendants("CityName").FirstOrDefault().Value),
                            new XAttribute("zoneCode", req.Descendants("AreaID").FirstOrDefault().Value),
                            new XAttribute("zoneName", req.Descendants("AreaName").FirstOrDefault().Value),
                            new XAttribute("address", ""),
                            new XAttribute("latitude", ""),
                            new XAttribute("longitude", ""),
                            new XAttribute("minRate", req.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value),
                            new XAttribute("mapLink", ""),
                            new XAttribute("currency", req.Descendants("CurrencyName").FirstOrDefault().Value),
                            new XAttribute("sessionKey", req.Descendants("GiataHotelList").FirstOrDefault().Attribute("sessionKey").Value),
                            new XAttribute("sourcekey", req.Descendants("GiataHotelList").FirstOrDefault().Attribute("sourcekey").Value),
                            new XAttribute("publishedkey", req.Descendants("GiataHotelList").FirstOrDefault().Attribute("publishedkey").Value),
                            new XElement("rooms", bindroomextreq(req.Descendants("Room").ToList()))
                            )
                    ));
                #endregion
                #region Response
                xmlResponseRequest apireq = new xmlResponseRequest();
                string apiresponse = apireq.xmloutHTTPResponse(request, "CXLPolicy", 3, req.Descendants("TransID").FirstOrDefault().Value, customerid, "501");
                XElement apiresp = null;
                try
                {
                    apiresp = XElement.Parse(apiresponse);
                    XElement hoteldetailsresponse = XElement.Parse(apiresponse.ToString());
                    apiresp = RemoveAllNamespaces(hoteldetailsresponse);
                }
                catch { }
                cxlpolicyresp = hotelbinding(apiresp.Descendants("hotel") != null ? apiresp.Descendants("hotel").FirstOrDefault() : null);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "cxlpolicy_beOUT";
                ex1.PageName = "xmloutCXLPolicy";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                IEnumerable<XElement> request = req.Descendants("hotelcancelpolicyrequest").ToList();
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                cxlpolicyresp = new XElement(
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
                                   new XElement("HotelDetailwithcancellationResponse",
                                       new XElement("ErrorTxt", "No Policy Found")
                                               )
                                           )
                          ));
                return cxlpolicyresp;
                #endregion
            }
            return cxlpolicyresp;
        }
        #endregion
        #region Hotel Binding
        private XElement hotelbinding(XElement hotelst)
        {
            XElement htlresp = null;
            try
            {
                IEnumerable<XElement> request = reqTravayoo.Descendants("hotelcancelpolicyrequest").ToList();
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                string username = reqTravayoo.Descendants("UserName").Single().Value;
                string password = reqTravayoo.Descendants("Password").Single().Value;
                string AgentID = reqTravayoo.Descendants("AgentID").Single().Value;
                string ServiceType = reqTravayoo.Descendants("ServiceType").Single().Value;
                string ServiceVersion = reqTravayoo.Descendants("ServiceVersion").Single().Value;
                if (hotelst == null)
                {
                    XElement cxlpolicyresp = new XElement(
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
                                   new XElement("HotelDetailwithcancellationResponse",
                                       new XElement("ErrorTxt", "No Policy Found. Please check later")
                                               )
                                           )
                          ));
                    return cxlpolicyresp;
                }
                else
                {
                    XElement cxlpolicies = null;
                    cxlpolicies = bindcxlpolicyout(hotelst.Descendants("cancellationPolicy").ToList());
                    htlresp = new XElement(
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
                                   new XElement("HotelDetailwithcancellationResponse",
                                       new XElement("Hotel",
                                           new XElement("HotelID", Convert.ToString(reqTravayoo.Descendants("HotelID").FirstOrDefault().Value)),
                                                       new XElement("HotelName", Convert.ToString(reqTravayoo.Descendants("HotelName").FirstOrDefault().Value)),
                                                       new XElement("HotelImgSmall", Convert.ToString("")),
                                                       new XElement("HotelImgLarge", Convert.ToString("")),
                                                       new XElement("MapLink", ""),
                                                       new XElement("DMC", ""),
                                                       new XElement("Currency", ""),
                                                       new XElement("Offers", ""),
                                                       new XElement("Rooms", cxlpolicies)
                    )))));
                }
            }
            catch { }
            return htlresp;
        }
        #endregion   
        #region Bind CXL Policy       
        private XElement bindcxlpolicyout(List<XElement> cxlpolicyresp)
        {
            XElement cxlpolicy = null;
            try
            {
                cxlpolicy=new XElement("Room",
                 new XAttribute("ID", Convert.ToString("")),
                 new XAttribute("RoomType", Convert.ToString("")),
                 new XAttribute("MealPlanPrice", ""),
                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                 new XAttribute("TotalRoomRate", Convert.ToString("")),
                 new XAttribute("CancellationDate", ""),
                 new XElement("CancellationPolicies",
                                                GetRoomCancellationPolicyout(cxlpolicyresp))
                 );
            }
            catch { cxlpolicy = new XElement("ErrorTxt", "No Policy"); }
            return cxlpolicy;
        }
        private IEnumerable<XElement> GetRoomCancellationPolicyout(List<XElement> cancellationpolicy)
        {
            #region Room's Cancellation Policies from Extranet
            List<XElement> htrm = new List<XElement>();
            List<XElement> pc = new List<XElement>();
            for (int i = 0; i < cancellationpolicy.Count(); i++)
            {
                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on after " + cancellationpolicy[i].Attribute("date").Value + "  will apply " + currencycode + " " + cancellationpolicy[i].Attribute("amount").Value + "  Cancellation fee"
                    , new XAttribute("LastCancellationDate", Convert.ToString(cancellationpolicy[i].Attribute("date").Value))
                    , new XAttribute("ApplicableAmount", cancellationpolicy[i].Attribute("amount").Value)
                     , new XAttribute("RefundValue", cancellationpolicy[i].Attribute("RefundValue") == null ? "" : cancellationpolicy[i].Attribute("RefundValue").Value)
                      , new XAttribute("MarkUpInBreakUps", cancellationpolicy[i].Attribute("MarkUpInBreakUps") == null ? "" : cancellationpolicy[i].Attribute("MarkUpInBreakUps").Value)
                       , new XAttribute("MarkUpApplied", cancellationpolicy[i].Attribute("MarkUpApplied") == null ? "" : cancellationpolicy[i].Attribute("MarkUpApplied").Value)
                    , new XAttribute("NoShowPolicy", "0")));
            };
            pc.Add(new XElement("Room", htrm));
            XElement cxlfinal = MergCxlPolicy(pc);
            List<XElement> cxlfinalres = cxlfinal.Descendants("CancellationPolicy").ToList();
            return cxlfinalres;
            #endregion
        }
        public DateTime chnagetoTime(string strDate)
        {
            DateTime oDate = DateTime.ParseExact(strDate, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            return oDate;

        }
        public XElement MergCxlPolicy(List<XElement> rooms)
        {
            List<XElement> cxlList = new List<XElement>();

            IEnumerable<XElement> dateLst = rooms.Descendants("CancellationPolicy").
               GroupBy(r => new { r.Attribute("LastCancellationDate").Value, noshow = r.Attribute("NoShowPolicy").Value }).Select(y => y.First()).
               OrderBy(p => DateTime.ParseExact(p.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture));
            if (dateLst.Count() > 0)
            {

                foreach (var item in dateLst)
                {
                    string date = item.Attribute("LastCancellationDate").Value;
                    string noShow = item.Attribute("NoShowPolicy").Value;
                    decimal datePrice = 0.0m;
                    foreach (var rm in rooms.Descendants("CancellationPolicy"))
                    {

                        if (rm.Attribute("NoShowPolicy").Value == noShow && rm.Attribute("LastCancellationDate").Value == date)
                        {

                            var price = rm.Attribute("ApplicableAmount").Value;
                            datePrice += Convert.ToDecimal(price);
                        }
                        else
                        {
                            if (noShow == "1")
                            {
                                //datePrice += Convert.ToDecimal(rm.Attribute("ApplicableAmount").Value);
                            }
                            else
                            {


                                var lastItem = rm.Descendants("CancellationPolicy").
                                    Where(pq => (pq.Attribute("NoShowPolicy").Value == noShow && Convert.ToDateTime(pq.Attribute("LastCancellationDate").Value) < chnagetoTime(date)));

                                if (lastItem.Count() > 0)
                                {
                                    var lastDate = lastItem.Max(y => y.Attribute("LastCancellationDate").Value);
                                    var lastprice = rm.Descendants("CancellationPolicy").
                                        Where(pq => (pq.Attribute("NoShowPolicy").Value == noShow && pq.Attribute("LastCancellationDate").Value == lastDate)).
                                        FirstOrDefault().Attribute("ApplicableAmount").Value;
                                    datePrice += Convert.ToDecimal(lastprice);
                                }

                            }

                        }
                    }
                    XElement pItem = new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", date), new XAttribute("ApplicableAmount", datePrice), new XAttribute("NoShowPolicy", noShow));
                    cxlList.Add(pItem);

                }

                cxlList = cxlList.GroupBy(x => new { DateTime.ParseExact(x.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date, x.Attribute("NoShowPolicy").Value }).
                    Select(y => new XElement("CancellationPolicy",
                        new XAttribute("LastCancellationDate", y.Key.Date.ToString("dd/MM/yyyy")),
                        new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("ApplicableAmount").Value))),
                        new XAttribute("NoShowPolicy", y.Key.Value))).OrderBy(p => DateTime.ParseExact(p.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture)).ToList();

                var fItem = cxlList.FirstOrDefault();

                if (Convert.ToDecimal(fItem.Attribute("ApplicableAmount").Value) != 0.0m)
                {
                    cxlList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", DateTime.ParseExact(fItem.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date.AddDays(-1).ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0")));

                }
            }

            XElement cxlItem = new XElement("CancellationPolicies", cxlList);
            return cxlItem;

        }      
        #endregion
        #region Extranet Room's binding (Request)
        private List<XElement> bindroomextreq(List<XElement> roomlst)
        {
            List<XElement> roomlstng = new List<XElement>();
            try
            {
                foreach (XElement room in roomlst)
                {
                    roomlstng.Add(new XElement("room",
                                    new XAttribute("code", room.Attribute("ID").Value),
                                    new XAttribute("name", room.Attribute("RoomType").Value),
                                    new XElement("rates",
                                        new XElement("rate",
                                            new XAttribute("rateKey", room.Attribute("SessionID").Value),
                                            new XAttribute("requestID", room.Descendants("RequestID").FirstOrDefault().Value),
                                            new XAttribute("boardID", room.Attribute("MealPlanID").Value),
                                            new XAttribute("boardCode", room.Attribute("MealPlanCode").Value),
                                            new XAttribute("boardName", room.Attribute("MealPlanName").Value),
                                            new XAttribute("packaging", GetTremCondition(room)),
                                            new XAttribute("net", room.Attribute("TotalRoomRate").Value),
                                            new XAttribute("allotment", ""),
                                            new XAttribute("onRequest", "false"),
                                            new XAttribute("roomNo", room.Attribute("RoomSeq").Value),
                                            new XAttribute("groupID", ""),
                                            new XAttribute("cbID", ""),
                                            new XAttribute("isGroup", ""),
                                            new XAttribute("sourcekey", room.Attribute("sourcekey").Value),
                                            new XElement("offers", offerbindreq(room.Descendants("Promotions").ToList())),
                                            new XElement("supplements", supplementbindreq(room.Descendants("Supplement").ToList())),
                                            new XElement("dailyRates", pricebrkupbindreq(room.Descendants("Price").ToList())),
                                            new XElement("cancellationPolicies", null)
                                            ))
                                    )
                                 );
                }
            }
            catch { }
            return roomlstng;
        }
        #endregion
        #region Term and Condition
        string GetTremCondition(XElement rmlst)
        {            
            string term = string.Empty;
            try
            {
                term = rmlst.Attribute("CancellationAmount").Value;

                //foreach (XElement item in rmlst)
                //{
                //    if (!string.IsNullOrEmpty(item.Attribute("CancellationAmount").Value))
                //    {
                //        term = term + System.Environment.NewLine + item.Attribute("CancellationAmount").Value;
                //    }

                //}
            }
            catch { }
            return term;
        }
        #endregion
        #region Supplements
        private List<XElement> supplementbindreq(List<XElement> supplements)
        {
            List<XElement> supplementlst = new List<XElement>();
            try
            {
                foreach (var supplement in supplements)
                {
                    supplementlst.Add(new XElement("supplement",
                        new XAttribute("mandatory", supplement.Attribute("suppIsMandatory").Value),
                        new XAttribute("type", supplement.Attribute("suppType").Value),
                        new XAttribute("price", supplement.Attribute("suppPrice").Value),
                        new XAttribute("name", supplement.Attribute("suppName").Value)
                        ));
                }
            }
            catch { }
            return supplementlst;
        }
        #endregion
        #region Promotions
        private List<XElement> offerbindreq(List<XElement> offers)
        {
            List<XElement> offerlst = new List<XElement>();
            try
            {
                foreach (var offer in offers)
                {
                    offerlst.Add(new XElement("offer", new XAttribute("name", offer.Value)));
                }
            }
            catch { }
            return offerlst;
        }
        #endregion
        #region Price Breakup (Request)
        private List<XElement> pricebrkupbindreq(List<XElement> prices)
        {
            List<XElement> pricelst = new List<XElement>();
            try
            {
                foreach (var prc in prices)
                {
                    pricelst.Add(new XElement("dailyRate",
                        new XAttribute("night", prc.Attribute("Night").Value),
                        new XAttribute("dailyNet", prc.Attribute("PriceValue").Value),
                        new XAttribute("sourcekey", prc.Attribute("sourcekey").Value)));
                }
            }
            catch { }
            return pricelst;
        }
        #endregion
        #region Bind Occupancy Request
        private List<XElement> bindoccupancy(List<XElement> roompax)
        {
            List<XElement> occuplst = new List<XElement>();
            try
            {
                for (int i = 0; i < roompax.Count(); i++)
                {
                    string childage = string.Empty;
                    if (Convert.ToInt16(roompax[i].Descendants("Child").FirstOrDefault().Value) > 0)
                    {
                        List<XElement> chldlst = roompax[i].Descendants("ChildAge").ToList();
                        for (int j = 0; j < chldlst.Count(); j++)
                        {
                            if (j == roompax[i].Descendants("ChildAge").Count() - 1)
                            {
                                childage += childage + chldlst[j].Value;
                            }
                            else
                            {
                                childage = childage + chldlst[j].Value + ",";
                            }
                        }
                    }
                    occuplst.Add(new XElement("occupancy",
                           new XAttribute("room", Convert.ToString("1")),
                           new XAttribute("adult", Convert.ToString(roompax[i].Descendants("Adult").FirstOrDefault().Value)),
                           new XAttribute("children", Convert.ToString(roompax[i].Descendants("Child").FirstOrDefault().Value)),
                           new XAttribute("ages", childage))
                    );
                }
            }
            catch { }
            return occuplst;
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
        public void Dispose()
        {
            this.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}