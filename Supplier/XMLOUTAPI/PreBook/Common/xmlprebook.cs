using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Supplier.XMLOUTAPI.PreBook.Extranetout;
using TravillioXMLOutService.Common.DotW;
using System.Globalization;

namespace TravillioXMLOutService.Supplier.XMLOUTAPI.PreBook.Common
{
    public class xmlprebook : IDisposable
    {
        #region Global vars
        string customerid = string.Empty;
        string dmc = string.Empty;
        int SupplierId = 501;
        string currency = string.Empty;
        string sessionKey = string.Empty;
        string sourcekey = string.Empty;
        string publishedkey = string.Empty;
        XElement reqTravayoo = null;
        string Hotelcode = string.Empty;
        #endregion
        #region Room Availability
        public XElement prebook_bookingexpressOUT(XElement req,string dmcName)
        {
            reqTravayoo = req;
            dmc = dmcName;
            customerid = req.Descendants("CustomerID").FirstOrDefault().Value;
            List<XElement> roomavailabilityresponse = new List<XElement>();
            XElement prebookresp = null;
            try
            {
                #region Request
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
                            new XAttribute("code", req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value),
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
                string apiresponse = apireq.xmloutHTTPResponse(request, "PreBook", 4, req.Descendants("TransID").FirstOrDefault().Value, customerid, "501");
                XElement apiresp = null;
                try
                {
                    apiresp = XElement.Parse(apiresponse);
                    XElement hoteldetailsresponse = XElement.Parse(apiresponse.ToString());
                    //XElement hoteldetailsresponse = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\xmlres.xml"));
                    apiresp = RemoveAllNamespaces(hoteldetailsresponse);
                }
                catch { }
                prebookresp = hotelbinding(apiresp.Descendants("hotel") != null ? apiresp.Descendants("hotel").FirstOrDefault() : null);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                string username = req.Descendants("UserName").FirstOrDefault().Value;
                string password = req.Descendants("Password").FirstOrDefault().Value;
                string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = req.Descendants("HotelPreBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                prebookresp = new XElement(
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
                       new XElement("HotelPreBookingResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
                               ));
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "prebook_bookingexpressOUT";
                ex1.PageName = "xmlprebook";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return prebookresp;
                #endregion
            }
            return prebookresp;
        }
        #endregion
        #region Hotel Binding
        private XElement hotelbinding(XElement hotelst)
        {
            XElement htlresp = null;
            try
            {
                IEnumerable<XElement> request = reqTravayoo.Descendants("HotelPreBookingRequest").ToList();
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                string username = reqTravayoo.Descendants("UserName").Single().Value;
                string password = reqTravayoo.Descendants("Password").Single().Value;
                string AgentID = reqTravayoo.Descendants("AgentID").Single().Value;
                string ServiceType = reqTravayoo.Descendants("ServiceType").Single().Value;
                string ServiceVersion = reqTravayoo.Descendants("ServiceVersion").Single().Value;
                if (hotelst == null)
                {
                    XElement prebookresp = new XElement(
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
                           new XElement("HotelPreBookingResponse",
                               new XElement("ErrorTxt", "No response")
                                       )
                                   )
                                   ));
                    return prebookresp;
                }
                else
                {
                    Hotelcode = hotelst.Attribute("code").Value;
                    currency = hotelst.Attribute("currency").Value;
                    XElement hotelprebookresponse = null;
                    if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "1").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "3").ToList().Count() > 0)
                    {
                        prebookExtout getprebook = new prebookExtout();
                        hotelprebookresponse = getprebook.GetprebookExtranetout(reqTravayoo, hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "4").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "6").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "7").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "11").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "16").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "17").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "23").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "35").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "41").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "37").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
                    else if (hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "39").ToList().Count() > 0)
                    {
                        hotelprebookresponse = prebookrespout(hotelst);
                    }
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
                                                   new XElement("HotelPreBookingResponse",
                                                       new XElement("NewPrice"),
                                                       new XElement("Hotels",
                                                           hotelprebookresponse
                                          )))));
                }
            }
            catch { }
            return htlresp;
        }
        #endregion   
        #region HotelsPro
        private XElement prebookrespout(XElement supresponse)
        {
            XElement htlresponse = null;
            try
            {
                string tnc = string.Empty;
                string dmc = string.Empty;
                SupplierId = Convert.ToInt32(reqTravayoo.Descendants("GiataHotelList").FirstOrDefault().Attribute("GSupID").Value);
                dmc = reqTravayoo.Descendants("GiataHotelList").FirstOrDefault().Attribute("custName").Value;
                tnc = supresponse.Descendants("rate").FirstOrDefault().Attribute("termCondition").Value;
                htlresponse = new XElement("Hotel",
                                           new XElement("HotelID", Convert.ToString(supresponse.Attribute("code").Value)),
                                                       new XElement("HotelName", Convert.ToString(supresponse.Attribute("name").Value)),
                                                       new XElement("Status", "true"),
                                                       new XElement("TermCondition", tnc),
                                                       new XElement("HotelImgSmall", Convert.ToString("")),
                                                       new XElement("HotelImgLarge", Convert.ToString("")),
                                                       new XElement("MapLink", ""),
                                                       new XElement("DMC", dmc),
                                                       new XElement("sourcekey", supresponse.Attribute("sourcekey").Value),
                                                       new XElement("Currency", Convert.ToString(currency)),
                                                       new XElement("Offers", "")
                                                       , new XElement("Rooms",
                                                roomlstbind(supresponse.Descendants("rooms").FirstOrDefault())
                                               )
                    );
            }
            catch (Exception ex)
            {
                #region Exception
                string username = reqTravayoo.Descendants("UserName").FirstOrDefault().Value;
                string password = reqTravayoo.Descendants("Password").FirstOrDefault().Value;
                string AgentID = reqTravayoo.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = reqTravayoo.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = reqTravayoo.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = reqTravayoo.Descendants("HotelPreBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                htlresponse = new XElement(
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
                       new XElement("HotelPreBookingResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
                               ));
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "prebookrespout";
                ex1.PageName = "xmlprebook";
                ex1.CustomerID = reqTravayoo.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = reqTravayoo.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            return htlresponse;
        }
        #endregion
        #region Room Binding (Response)
        private List<XElement> roomlstbind(XElement roomresp)
        {
            List<XElement> roomlst = new List<XElement>();
            List<XElement> froomlst = new List<XElement>();
            List<XElement> ttlroom = reqTravayoo.Descendants("RoomPax").ToList();
            try
            {
                int i = 0;
                foreach (XElement room in roomresp.Descendants("room"))
                {
                    roomlst.Add(new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(room.Attribute("code").Value)),
                                     new XAttribute("SuppliersID", SupplierId),
                                     new XAttribute("RoomSeq", i+1),
                                     new XAttribute("SessionID", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("rateKey").Value)),
                                     new XAttribute("RoomType", Convert.ToString(room.Attribute("name").Value)),
                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                     new XAttribute("MealPlanID", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("boardID").Value)),
                                     new XAttribute("MealPlanName", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("boardName").Value)),
                                     new XAttribute("MealPlanCode", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("boardCode").Value)),
                                     new XAttribute("MealPlanPrice", ""),
                                     new XAttribute("PerNightRoomRate", Convert.ToString("0")),
                                     new XAttribute("TotalRoomRate", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("net").Value)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                     new XAttribute("sourcekey", room.Descendants("rate").FirstOrDefault().Attribute("sourcekey").Value),
                                     new XAttribute("isAvailable", room.Descendants("rate").FirstOrDefault().Attribute("onRequest").Value == "false" ? "true" : "false"),
                                     new XElement("RequestID", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("requestID").Value)),
                                     new XElement("Offers", ""),
                                     new XElement("PromotionList", promobindlst(room.Descendants("offer").ToList())),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements", supplementbindlst(room.Descendants("supplement").ToList())),
                                         new XElement("PriceBreakups", breakupsbindlst(room.Descendants("dailyRate").ToList())),
                                         new XElement("AdultNum", Convert.ToString(ttlroom[i].Descendants("Adult").FirstOrDefault().Value)),
                                         new XElement("ChildNum", Convert.ToString(ttlroom[i].Descendants("Child").FirstOrDefault().Value))
                                     ));
                    i++;
                }
                XElement roomgrp = new XElement("RoomTypes", new XAttribute("Index", 1),
                        new XAttribute("TotalRate", roomresp.Descendants("room").Descendants("rate").Attributes("net").Sum(x => Convert.ToDecimal(x.Value))),
                        new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency),
                        new XAttribute("DMCType", dmc),
                        new XAttribute("CUID", customerid), roomlst
                        ,new XElement("CancellationPolicies",
                                             getCancellationPolicyOut(roomresp.Descendants("cancellationPolicy").ToList()))
                        );
                froomlst.Add(roomgrp);
            }
            catch { }
            return froomlst;
        }
        #endregion
        #region Bind Promotion (Response)
        private List<XElement> promobindlst(List<XElement> resp)
        {
            List<XElement> resplst = new List<XElement>();
            try
            {
                Int32 length = resp.Count();
                if (length == 0)
                {
                    resplst.Add(new XElement("Promotions", ""));
                }
                else
                {
                    for (int i = 0; i < resp.Count(); i++)
                    {
                        resplst.Add(new XElement("Promotions", Convert.ToString(resp[i].Attribute("name").Value)));
                    }
                }
            }
            catch { }
            return resplst;
        }
        #endregion
        #region Bind Breakups (Response)
        private List<XElement> breakupsbindlst(List<XElement> resp)
        {
            List<XElement> resplst = new List<XElement>();
            try
            {
                for (int i = 0; i < resp.Count(); i++)
                {
                    resplst.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                           new XAttribute("PriceValue", Convert.ToString(resp[i].Attribute("dailyNet").Value)),
                           new XAttribute("sourcekey", resp[i].Attribute("sourcekey") == null ? "" : resp[i].Attribute("sourcekey").Value))
                    );
                }
                return resplst.OrderBy(x => (int)x.Attribute("Night")).ToList();
            }
            catch { }
            return resplst;
        }
        #endregion
        #region Bind Supplements (Response)
        private List<XElement> supplementbindlst(List<XElement> supplements)
        {
            List<XElement> resplst = new List<XElement>();
            try
            {
                for (int i = 0; i < supplements.Count(); i++)
                {
                    resplst.Add(new XElement("Supplement",
                             new XAttribute("suppId", Convert.ToString("0")),
                             new XAttribute("suppName", Convert.ToString(supplements[i].Attribute("name").Value)),
                             new XAttribute("supptType", Convert.ToString("0")),
                             new XAttribute("suppIsMandatory", Convert.ToString(supplements[i].Attribute("mandatory").Value)),
                             new XAttribute("suppChargeType", Convert.ToString(supplements[i].Attribute("type").Value)),
                             new XAttribute("suppPrice", Convert.ToString(supplements[i].Attribute("price").Value)),
                             new XAttribute("suppType", Convert.ToString("PerRoomSupplement")))
                          );
                }
            }
            catch { }
            return resplst;
        }
        #endregion
        #region Bind CXL Policy (Response)
        private IEnumerable<XElement> getCancellationPolicyOut(List<XElement> cancellationpolicy)
        {
            #region Room's Cancellation Policies from Extranet
            try
            {
                List<XElement> htrm = new List<XElement>();
                List<XElement> pc = new List<XElement>();
                for (int i = 0; i < cancellationpolicy.Count(); i++)
                {
                    htrm.Add(new XElement("CancellationPolicy", "Cancellation done on after " + cancellationpolicy[i].Attribute("date").Value + "  will apply " + currency + " " + cancellationpolicy[i].Attribute("amount").Value + "  Cancellation fee"
                        , new XAttribute("LastCancellationDate", Convert.ToString(cancellationpolicy[i].Attribute("date").Value))
                        , new XAttribute("ApplicableAmount", cancellationpolicy[i].Attribute("amount").Value)
                        , new XAttribute("NoShowPolicy", "0")));
                }
                pc.Add(new XElement("Room", htrm));
                XElement cxlfinal = MergCxlPolicy(pc);
                List<XElement> cxlfinalres = cxlfinal.Descendants("CancellationPolicy").ToList();
                return cxlfinalres;
            }
            catch { return null; }
            #endregion
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
        public DateTime chnagetoTime(string strDate)
        {
            DateTime oDate = DateTime.ParseExact(strDate, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            return oDate;

        }
        public XElement MergCxlPolicy_old(List<XElement> rooms)
        {
            List<XElement> cxlList = new List<XElement>();

            IEnumerable<XElement> dateLst = rooms.Descendants("CancellationPolicy").
               GroupBy(r => new { r.Attribute("LastCancellationDate").Value, noshow = r.Attribute("NoShowPolicy").Value }).Select(y => y.First()).
               OrderBy(p => p.Attribute("LastCancellationDate").Value);
            if (dateLst.Count() > 0)
            {

                foreach (var item in dateLst)
                {
                    string date = item.Attribute("LastCancellationDate").Value;
                    string noShow = item.Attribute("NoShowPolicy").Value;
                    decimal datePrice = 0.0m;
                    foreach (var rm in rooms)
                    {
                        var prItem = rm.Descendants("CancellationPolicy").
                       Where(pq => (pq.Attribute("NoShowPolicy").Value == noShow && pq.Attribute("LastCancellationDate").Value == date)).
                       FirstOrDefault();
                        if (prItem != null)
                        {
                            var price = prItem.Attribute("ApplicableAmount").Value;
                            datePrice += Convert.ToDecimal(price);
                        }
                        else
                        {
                            if (noShow == "1")
                            {
                                datePrice += Convert.ToDecimal(rm.Attribute("TotalRoomRate").Value);
                            }
                            else
                            {
                                var lastItem = rm.Descendants("CancellationPolicy").
                                    Where(pq => (pq.Attribute("NoShowPolicy").Value == noShow && Convert.ToDateTime(pq.Attribute("LastCancellationDate").Value) < date.chnagetoTime()));

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

                cxlList = cxlList.GroupBy(x => new { DateTime.ParseExact(x.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", null).Date, x.Attribute("NoShowPolicy").Value }).
                    Select(y => new XElement("CancellationPolicy",
                        new XAttribute("LastCancellationDate", y.Key.Date.ToString("dd/MM/yyyy")),
                        new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("ApplicableAmount").Value))),
                        new XAttribute("NoShowPolicy", y.Key.Value))).OrderBy(p => p.Attribute("LastCancellationDate").Value).ToList();

                var fItem = cxlList.FirstOrDefault();

                if (Convert.ToDecimal(fItem.Attribute("ApplicableAmount").Value) != 0.0m)
                {
                    cxlList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", DateTime.ParseExact(fItem.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", null).AddDays(-1).Date.ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0")));

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
                foreach(XElement room in roomlst)
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
                                            new XAttribute("groupID",""),
                                            new XAttribute("cbID",""),
                                            new XAttribute("isGroup",""),
                                            new XAttribute("sourcekey", room.Attribute("sourcekey").Value),
                                            new XElement("offers", offerbindreq(room.Descendants("Promotions").ToList())),
                                            new XElement("supplements", supplementbindreq(room.Descendants("Supplement").ToList())),
                                            new XElement("dailyRates", pricebrkupbindreq(room.Descendants("Price").ToList())),
                                            new XElement("cancellationPolicies",null)
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

            term = rmlst.Attribute("CancellationAmount").Value;
               
            //foreach (XElement item in rmlst)
            //{
            //    if (!string.IsNullOrEmpty(item.Attribute("CancellationAmount").Value))
            //    {
            //        term = term + System.Environment.NewLine + item.Attribute("CancellationAmount").Value;
            //    }

            //}
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
                foreach(var offer in offers)
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