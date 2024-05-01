using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Supplier.XMLOUTAPI.GetRoom.Extranetout;

namespace TravillioXMLOutService.Supplier.XMLOUTAPI.GetRoom.Common
{
    public class xmlGetroom : IDisposable
    {
        #region Global vars
        string customerid = string.Empty;
        string dmc = string.Empty;
        int SupplierId = 501;
        string sessionKey = string.Empty;
        string sourcekey = string.Empty;
        string publishedkey = string.Empty;
        XElement reqTravayoo = null;
        int totalrooms = 1;
        #endregion
        #region Room Availability
        public XElement GetRoomAvail_bookingexpressOUT(XElement req)
        {
            reqTravayoo = req;
            totalrooms = req.Descendants("RoomPax").Count();
            List<XElement> roomavailabilityresponse = new List<XElement>();
            XElement getrm = null;
            try
            {
                #region changed
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "501").ToList();
                for (int i = 0; i < htlele.Count(); i++)
                {
                    string custID = string.Empty;
                    string custName = string.Empty;
                    string htlid = htlele[i].Attribute("GHtlID").Value;
                    string xmlout = string.Empty;
                    try
                    {
                        xmlout = htlele[i].Attribute("xmlout").Value;
                    }
                    catch { xmlout = "false"; }
                    if (xmlout == "true")
                    {
                        try
                        {
                            customerid = htlele[i].Attribute("custID").Value;
                            dmc = htlele[i].Attribute("custName").Value;
                            SupplierId = Convert.ToInt16(htlele[i].Attribute("GSupID").Value);
                            sessionKey = htlele[i].Attribute("sessionKey").Value;
                            sourcekey = htlele[i].Attribute("sourcekey").Value;
                            publishedkey = htlele[i].Attribute("publishedkey").Value;
                        }
                        catch { custName = "HA"; }
                    }
                    else
                    {
                        try
                        {
                            customerid = htlele[i].Attribute("custID").Value;
                            SupplierId = Convert.ToInt16(htlele[i].Attribute("GSupID").Value);
                            sessionKey = htlele[i].Attribute("sessionKey").Value;
                            sourcekey = htlele[i].Attribute("sourcekey").Value;
                            publishedkey = htlele[i].Attribute("publishedkey").Value;
                        }
                        catch { }
                        dmc = "BookingExpress";
                    }
                    //EBookingService rs = new EBookingService();
                    //roomavailabilityresponse.Add(rs.RoomAvailability(req, dmc, htlid));

                    roomavailabilityresponse.Add(getRoom(req, htlid));
                }
                #endregion
                getrm = new XElement("TotalRooms", roomavailabilityresponse);
                return getrm;
            }
            catch { return null; }
        }
        #endregion
        #region Get Room's
        private XElement getRoom(XElement req,string hotelid)
        {
            XElement response = null;
            try
            {
                #region Request
                XElement request = new XElement("roomRQ",
                    new XAttribute("customerID", customerid),
                    new XAttribute("agencyID", req.Descendants("SubAgentID").FirstOrDefault().Value),
                    new XAttribute("sessionID", req.Descendants("TransID").FirstOrDefault().Value),
                    new XAttribute("sourceMarket", req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                    new XAttribute("cityID", req.Descendants("CityID").FirstOrDefault().Value),
                    new XAttribute("cityCode", req.Descendants("CityCode").FirstOrDefault().Value),
                    new XAttribute("cityName", req.Descendants("CityName").FirstOrDefault().Value),
                    new XAttribute("zoneCode", req.Descendants("RequestID").FirstOrDefault().Value),
                    new XAttribute("zoneName", req.Descendants("AreaName").FirstOrDefault().Value),
                    new XAttribute("currency", req.Descendants("DesiredCurrencyCode").FirstOrDefault().Value),
                    new XElement("stay", new XAttribute("checkIn", req.Descendants("FromDate").FirstOrDefault().Value), new XAttribute("checkOut", req.Descendants("ToDate").FirstOrDefault().Value)),
                    new XElement("occupancies", bindoccupancy(req.Descendants("RoomPax").ToList())),
                    new XElement("hotels",
                        new XElement("hotel",
                            new XAttribute("appkey",SupplierId),
                            new XAttribute("sessionKey", sessionKey),
                            new XAttribute("sourcekey", sourcekey),
                            new XAttribute("publishedkey", publishedkey), hotelid))
                    );
                #endregion
                #region Response
                xmlResponseRequest apireq = new xmlResponseRequest();
                string apiresponse = apireq.xmloutHTTPResponse(request, "RoomAvail", 2, req.Descendants("TransID").FirstOrDefault().Value, customerid, "501");
                XElement apiresp = null;
                try
                {
                    apiresp = XElement.Parse(apiresponse);
                    XElement hoteldetailsresponse = XElement.Parse(apiresponse.ToString());
                    apiresp = RemoveAllNamespaces(hoteldetailsresponse);
                }
                catch { }
                response = hotelbinding(apiresp.Descendants("hotel") != null ? apiresp.Descendants("hotel").FirstOrDefault():null);
                #endregion
            }
            catch { }
            return response;
        }
        #endregion
        #region Extranet Hotel Binding
        private XElement hotelbinding(XElement hotelst)
        {
            XElement htlresp = null;
            try
            {
                if (hotelst == null)
                {
                    return null;
                }
                else
                {
                    List<XElement> darinarmlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "1").ToList();
                    List<XElement> extrmlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "3").ToList();
                    List<XElement> hbrmlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "4").ToList();
                    List<XElement> hprormlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "6").ToList();
                    List<XElement> travcormlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "7").ToList();
                    List<XElement> mikirmlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "11").ToList();
                    List<XElement> restelrmlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "13").ToList();
                    List<XElement> w2mrmlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "16").ToList();
                    List<XElement> eermlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "17").ToList();
                    List<XElement> lohrmlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "23").ToList();
                    List<XElement> lcirmlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "35").ToList();
                    List<XElement> jacrmlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "37").ToList();
                    List<XElement> smyrmlst = hotelst.Descendants("room").Where(x => x.Descendants("rate").FirstOrDefault().Attribute("cbID").Value == "39").ToList();
                    List<XElement> darinaroomlst = null;
                    List<XElement> extroomlst=null;
                    List<XElement> hbroomlst = null;
                    List<XElement> hproroomlst = null;
                    List<XElement> travcoroomlst = null;
                    List<XElement> mikiroomlst = null;
                    List<XElement> restelroomlst = null;
                    List<XElement> w2mroomlst = null;
                    List<XElement> eeroomlst = null;
                    List<XElement> lohroomlst = null;
                    List<XElement> lciroomlst = null;
                    List<XElement> jacroomlst = null;
                    List<XElement> smyroomlst = null;
                    #region Darina Rooms
                    if (darinarmlst.Count() > 0)
                    {
                        //darinaroomlst = GetHotelRoomsDarinaout(darinarmlst, hotelst.Attribute("code").Value, hotelst.Attribute("currency").Value, dmc, SupplierId);
                        darinaroomlst = GetHotelRoomsHProout(darinarmlst, hotelst.Attribute("code").Value, hotelst.Attribute("currency").Value, dmc, SupplierId);
                    }
                    #endregion
                    #region Extranet Rooms
                    if (extrmlst.Count() > 0)
                    {
                        getRoomExtout getRum = new getRoomExtout();
                        extroomlst = getRum.GetHtlRoomLstngExtranetout(extrmlst, hotelst.Attribute("code").Value, hotelst.Attribute("currency").Value, reqTravayoo, customerid, dmc, SupplierId);
                    }
                    #endregion
                    #region HotelBeds Rooms
                    if (hbrmlst.Count() > 0)
                    {
                        hbroomlst = GetHotelRoomsHProout(hbrmlst, hotelst.Attribute("code").Value, hotelst.Attribute("currency").Value, dmc, SupplierId);
                    }
                    #endregion
                    #region HotelsPro Rooms
                    if (hprormlst.Count() > 0)
                    {
                        hproroomlst = GetHotelRoomsHProout(hprormlst, hotelst.Attribute("code").Value, hotelst.Attribute("currency").Value, dmc, SupplierId);
                    }
                    #endregion
                    #region Travco Rooms
                    if (travcormlst.Count() > 0)
                    {
                        travcoroomlst = GetHotelRoomsHProout(travcormlst, hotelst.Attribute("code").Value, hotelst.Attribute("currency").Value, dmc, SupplierId);
                    }
                    #endregion
                    #region Miki Rooms
                    if (mikirmlst.Count() > 0)
                    {
                        mikiroomlst = jacroomgrouping(mikirmlst, hotelst.Attribute("code").Value).ToList();
                    }
                    #endregion
                    #region Restel Rooms
                    if (restelrmlst.Count() > 0)
                    {
                        restelroomlst = jacroomgrouping(restelrmlst, hotelst.Attribute("code").Value).ToList();
                    }
                    #endregion
                    #region W2M Rooms
                    if (w2mrmlst.Count() > 0)
                    {
                        w2mroomlst = GetHotelRoomsHProout(w2mrmlst, hotelst.Attribute("code").Value, hotelst.Attribute("currency").Value, dmc, SupplierId);
                    }
                    #endregion
                    #region Egypt Express Rooms
                    if (eermlst.Count() > 0)
                    {
                        eeroomlst = GetHotelRoomsHProout(eermlst, hotelst.Attribute("code").Value, hotelst.Attribute("currency").Value, dmc, SupplierId);
                    }
                    #endregion
                    #region LOH Rooms
                    if (lohrmlst.Count() > 0)
                    {
                        lohroomlst = GetHotelRoomsHProout(lohrmlst, hotelst.Attribute("code").Value, hotelst.Attribute("currency").Value, dmc, SupplierId);
                    }
                    #endregion
                    #region LCI Rooms
                    if (lcirmlst.Count() > 0)
                    {
                        lciroomlst = GetHotelRoomsHProout(lcirmlst, hotelst.Attribute("code").Value, hotelst.Attribute("currency").Value, dmc, SupplierId);
                    }
                    #endregion
                    #region Jac Rooms
                    if (jacrmlst.Count() > 0)
                    {
                        jacroomlst = jacroomgrouping(jacrmlst, hotelst.Attribute("code").Value).ToList();
                    }
                    #endregion
                    #region Smy Rooms
                    if (smyrmlst.Count() > 0)
                    {
                        smyroomlst = jacroomgrouping(smyrmlst, hotelst.Attribute("code").Value).ToList();
                    }
                    #endregion
                    htlresp = new XElement("Hotel", new XElement("rooms",
                        darinaroomlst,
                        extroomlst,
                        hbroomlst,
                        hproroomlst,
                        travcoroomlst,
                        mikiroomlst,
                        restelroomlst,
                        w2mroomlst,
                        eeroomlst,
                        lohroomlst,
                        lciroomlst,
                        jacroomlst,
                        smyroomlst
                        ));
                }
            }
            catch { }
            return htlresp;
        }
        #endregion  
        #region HotelsPro Rooms Binding
        public List<XElement> GetHotelRoomsHProout(List<XElement> roomlist, string Hotelcode, string currency, string dmc, int suppID)
        {
            #region HotelsPro Hotel's Room List
            List<XElement> str = new List<XElement>();
            List<XElement> ttlroom = reqTravayoo.Descendants("RoomPax").ToList();
            int totgrp = roomlist.GroupBy(x => x.Descendants("rate").FirstOrDefault().Attribute("groupID").Value).Count();
            for (int i = 0; i < totgrp; i++)
            {
                int roomNo = 0;
                List<XElement> grpstr = new List<XElement>();
                List<XElement> ggrmlst = roomlist.Where(x => x.Descendants("rate").FirstOrDefault().Attribute("groupID").Value == Convert.ToString(i+1)).OrderBy(y => y.Descendants("rate").FirstOrDefault().Attribute("roomNo").Value).ToList();
                foreach (XElement room in ggrmlst)
                {
                        grpstr.Add(new XElement("Room",
                             new XAttribute("ID", Convert.ToString(room.Attribute("code").Value)),
                             new XAttribute("SuppliersID", suppID),
                             new XAttribute("RoomSeq", roomNo+1),
                             new XAttribute("SessionID", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("rateKey").Value)),
                             new XAttribute("RoomType", Convert.ToString(room.Attribute("name").Value)),
                             new XAttribute("OccupancyID", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("requestID").Value)),
                             new XAttribute("OccupancyName", Convert.ToString("")),
                             new XAttribute("MealPlanID", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("boardID").Value)),
                             new XAttribute("MealPlanName", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("boardName").Value)),
                             new XAttribute("MealPlanCode", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("boardCode").Value)),
                             new XAttribute("MealPlanPrice", ""),
                             new XAttribute("PerNightRoomRate", Convert.ToString("0")),
                             new XAttribute("TotalRoomRate", Convert.ToString(room.Descendants("rate").FirstOrDefault().Attribute("net").Value)),
                             new XAttribute("CancellationDate", ""),
                             new XAttribute("CancellationAmount", room.Descendants("rate").FirstOrDefault().Attribute("packaging").Value),
                             new XAttribute("sourcekey", room.Descendants("rate").FirstOrDefault().Attribute("sourcekey").Value),
                             new XAttribute("isAvailable", room.Descendants("rate").FirstOrDefault().Attribute("onRequest").Value == "false" ? "true" : "false"),
                             new XElement("RequestID", room.Descendants("rate").FirstOrDefault().Attribute("requestID").Value),
                             new XElement("Offers", ""),
                             new XElement("PromotionList", GetHotelpromotionsHProout(room.Descendants("rate").FirstOrDefault().Descendants("offer").ToList())),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements", Getsupplementsout(room.Descendants("supplement").ToList())),
                                 new XElement("PriceBreakups",
                                     GetRoomsPriceBreakupHProout(room.Descendants("rate").FirstOrDefault().Descendants("dailyRate").ToList())),
                                     new XElement("AdultNum", Convert.ToString(ttlroom[roomNo].Descendants("Adult").FirstOrDefault().Value)),
                                     new XElement("ChildNum", Convert.ToString(ttlroom[roomNo].Descendants("Child").FirstOrDefault().Value))
                             ));
                        roomNo++;
                }
                str.Add(new XElement("RoomTypes", new XAttribute("Index", i + 1),
                        new XAttribute("TotalRate", ggrmlst.Descendants("rate").Attributes("net").Sum(x => Convert.ToDecimal(x.Value))),
                        new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency),
                        new XAttribute("DMCType", dmc),
                        new XAttribute("CUID", customerid),grpstr));
            }
            return str;
            #endregion
        }
        private IEnumerable<XElement> Getsupplementsout(List<XElement> supplements)
        {

            Int32 length = supplements.Count();
            List<XElement> supplementlst = new List<XElement>();
            try
            {
                if (length == 0)
                {
                    //supplementlst.Add(new XElement("Supplement", ""));
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        supplementlst.Add(new XElement("Supplement",
                             new XAttribute("suppId", Convert.ToString("0")),
                             new XAttribute("suppName", Convert.ToString(supplements[i].Attribute("name").Value)),
                             new XAttribute("supptType", Convert.ToString("SUP")),
                             new XAttribute("suppIsMandatory", Convert.ToString(supplements[i].Attribute("mandatory").Value)),
                             new XAttribute("suppChargeType", Convert.ToString(supplements[i].Attribute("type").Value)),
                             new XAttribute("suppPrice", Convert.ToString(supplements[i].Attribute("price").Value)),
                             new XAttribute("suppType", Convert.ToString("SUP")))
                          );
                    }
                }
                return supplementlst;
            }
            catch { return null; }
        }
        private IEnumerable<XElement> GetHotelpromotionsHProout(List<XElement> roompromotions)
        {
            Int32 length = roompromotions.Count();
            List<XElement> promotion = new List<XElement>();
            try
            {
                if (length == 0)
                {
                    promotion.Add(new XElement("Promotions", ""));
                }
                else
                {

                    for (int i = 0; i < length; i++)
                    {
                        promotion.Add(new XElement("Promotions", Convert.ToString(roompromotions[i].Attribute("name").Value)));

                    };
                }
                return promotion;
            }
            catch { return null; }
        }
        private List<XElement> GetRoomsPriceBreakupHProout(List<XElement> pricebreakups)
        {
            #region Darina Room's Price Breakups
            List<XElement> str = new List<XElement>();
            try
            {
                for (int i = 0; i < pricebreakups.Count(); i++)
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                            new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].Attribute("dailyNet").Value)),
                           new XAttribute("sourcekey", pricebreakups[i].Attribute("sourcekey") == null ? "" : pricebreakups[i].Attribute("sourcekey").Value)
                           )
                    );
                }
                return str.OrderBy(x => (int)x.Attribute("Night")).ToList();
            }
            catch { return null; }
            #endregion
        }
        #endregion
        #region Darina Rooms Binding
        public List<XElement> GetHotelRoomsDarinaout(List<XElement> roomlist, string Hotelcode, string currency,string dmc, int suppID)
        {
            #region Darina Hotel's Room List
            List<XElement> str = new List<XElement>();
            for (int i = 0; i < roomlist.Count(); i++)
            {
                str.Add(new XElement("RoomTypes", new XAttribute("Index", i + 1),
                    new XAttribute("TotalRate", roomlist[i].Attribute("net").Value),
                    new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency),
                    new XAttribute("DMCType", dmc),
                    new XAttribute("CUID", customerid),
                    new XElement("Room",
                         new XAttribute("ID", Convert.ToString(roomlist[i].Parent.Parent.Attribute("code").Value)),
                         new XAttribute("SuppliersID", suppID),
                         new XAttribute("RoomSeq", "1"),
                         new XAttribute("SessionID", Convert.ToString(roomlist[i].Attribute("rateKey").Value)),
                         new XAttribute("RoomType", Convert.ToString(roomlist[i].Parent.Parent.Attribute("name").Value)),
                         new XAttribute("OccupancyID", Convert.ToString(roomlist[i].Attribute("requestID").Value)),
                         new XAttribute("OccupancyName", Convert.ToString("")),
                         new XAttribute("MealPlanID", Convert.ToString(roomlist[i].Attribute("boardID").Value)),
                         new XAttribute("MealPlanName", Convert.ToString(roomlist[i].Attribute("boardName").Value)),
                         new XAttribute("MealPlanCode", Convert.ToString(roomlist[i].Attribute("boardCode").Value)),
                         new XAttribute("MealPlanPrice", ""),
                         new XAttribute("PerNightRoomRate", Convert.ToString("0")),
                         new XAttribute("TotalRoomRate", Convert.ToString(roomlist[i].Attribute("net").Value)),
                         new XAttribute("CancellationDate", ""),
                         new XAttribute("CancellationAmount", ""),
                         new XAttribute("isAvailable", roomlist[i].Attribute("onRequest").Value=="false"?"true":"false"),
                         new XElement("RequestID", roomlist[i].Attribute("requestID").Value),
                         new XElement("Offers", ""),
                         new XElement("PromotionList",
                         new XElement("Promotions", GetHotelpromotionsdarinaout(roomlist[i].Descendants("offer").ToList()))),
                         new XElement("CancellationPolicy", ""),
                         new XElement("Amenities", new XElement("Amenity", "")),
                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                         new XElement("Supplements", null),
                             new XElement("PriceBreakups",
                                 GetRoomsPriceBreakupDarinaout(roomlist[i].Descendants("dailyRate").ToList())),
                                 new XElement("AdultNum", Convert.ToString(reqTravayoo.Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value)),
                                 new XElement("ChildNum", Convert.ToString(reqTravayoo.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value))
                         )));
            }
            return str;
            #endregion
        }
        private IEnumerable<XElement> GetHotelpromotionsdarinaout(List<XElement> roompromotions)
        {
            Int32 length = roompromotions.Count();
            List<XElement> promotion = new List<XElement>();
            try
            {
                if (length == 0)
                {
                    promotion.Add(new XElement("Promotions", ""));
                }
                else
                {

                    for (int i = 0; i < length; i++)
                    {
                        promotion.Add(new XElement("Promotions", Convert.ToString(roompromotions[i].Attribute("name").Value)));

                    };
                }
                return promotion;
            }
            catch { return null; }
        }
        private List<XElement> GetRoomsPriceBreakupDarinaout(List<XElement> pricebreakups)
        {
            #region Darina Room's Price Breakups
            List<XElement> str = new List<XElement>();
            try
            {
                for (int i = 0; i < pricebreakups.Count(); i++)
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                            new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].Attribute("dailyNet").Value)),
                           new XAttribute("sourcekey", pricebreakups[i].Attribute("sourcekey") == null ? "" : pricebreakups[i].Attribute("sourcekey").Value)
                           )
                    );
                }
                return str.OrderBy(x => (int)x.Attribute("Night")).ToList();
            }
            catch { return null; }
            #endregion
        }
        #endregion
        #region Jac/Total Stay Rooms Binding
        private IEnumerable<XElement> jacroomgrouping(List<XElement> rmlst, string hotelcode)
        {
            Dictionary<int, List<XElement>> dic = new Dictionary<int, List<XElement>>();
            for (int i = 1; i <= totalrooms; i++)
            {
                List<XElement> test = rmlst.Where(el => Convert.ToInt16(el.Descendants("rate").FirstOrDefault().Attribute("groupID").Value) == i).ToList();
                if (test != null)
                {
                    test = jacGroupOfRoom(test);
                    dic.Add(i, test);
                }
            }
            List<XElement> lst = null;
            for (int i = totalrooms; i >= 1; i--)
            {
                List<XElement> item = dic[i];
                lst = jacGetitem(i, item, lst, hotelcode);
            }
            return lst;
        }
        private List<XElement> jacGetitem(int i, List<XElement> item, List<XElement> lst, string hotelcode)
        {
            List<XElement> testlst = new List<XElement>();
            if (lst != null)
            {
                foreach (XElement item1 in item)
                {
                    foreach (XElement item2 in lst)
                    {
                        decimal totalrate = Convert.ToDecimal(item2.Attribute("TotalRate").Value) + Convert.ToDecimal(item1.Attribute("TotalRoomRate").Value);
                        if (item1.Attribute("MealPlanID").Value == item2.Descendants("Room").FirstOrDefault().Attribute("MealPlanID").Value)
                        {
                            IEnumerable<XElement> romcoll = item2.Descendants("Room");
                            testlst.Add(new XElement("RoomTypes",
                            new XAttribute("TotalRate", totalrate),
                            new XAttribute("Index", testlst.Count + 1),
                                 new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", "USD"), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),
                            item1,
                            romcoll));
                        }
                    }
                }
            }
            else if (lst == null)
            {
                foreach (XElement item1 in item)
                {
                    decimal totalrate = Convert.ToDecimal(item1.Attribute("TotalRoomRate").Value);
                    testlst.Add(new XElement("RoomTypes",
                        new XAttribute("TotalRate", totalrate),
                        new XAttribute("Index", testlst.Count + 1),
                             new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", "USD"), new XAttribute("DMCType", dmc),
                        item1));
                }
            }
            try
            {
                var rsult = testlst.GroupBy(x => x.Attribute("TotalRate").Value).Select(y => y.First()).OrderBy(z => z.Attribute("TotalRate").Value).ToList().Take(200);
                IEnumerable<XElement> rooms = rsult;
                testlst = rooms.ToList();
            }
            catch { return null; }
            return testlst;
        }
        private List<XElement> jacGroupOfRoom(IEnumerable<XElement> ele)
        {
            #region Add Sequence in B2B request
            int seq = 1;
            foreach (XElement room in reqTravayoo.Descendants("RoomPax"))
            {
                room.Add(new XElement("Seq", seq));
                seq++;
            }
            #endregion
            List<XElement> roomlst = new List<XElement>();           
            string roomno = ele.Descendants("rate").FirstOrDefault().Attribute("roomNo").Value;
            string adult = reqTravayoo.Descendants("RoomPax").Where(x => x.Descendants("Seq").FirstOrDefault().Value == roomno).FirstOrDefault().Descendants("Adult").FirstOrDefault().Value;
            string child = reqTravayoo.Descendants("RoomPax").Where(x => x.Descendants("Seq").FirstOrDefault().Value == roomno).FirstOrDefault().Descendants("Child").FirstOrDefault().Value;            
            foreach(XElement room in ele)
            {
                roomlst.Add(new XElement("Room",
                                                    new XAttribute("ID", room.Attribute("code").Value),
                                                    new XAttribute("SuppliersID", SupplierId),
                                                    new XAttribute("RoomSeq", room.Descendants("rate").FirstOrDefault().Attribute("roomNo").Value),
                                                    new XAttribute("SessionID", room.Descendants("rate").FirstOrDefault().Attribute("requestID").Value),
                                                    new XAttribute("RoomType", room.Attribute("name").Value),
                                                    new XAttribute("OccupancyID", ""),
                                                    new XAttribute("OccupancyName", ""),
                                                    new XAttribute("MealPlanID", room.Descendants("rate").FirstOrDefault().Attribute("boardID").Value),
                                                    new XAttribute("MealPlanName", room.Descendants("rate").FirstOrDefault().Attribute("boardName").Value),

                                                    new XAttribute("MealPlanCode", room.Descendants("rate").FirstOrDefault().Attribute("boardCode").Value),
                                                    new XAttribute("MealPlanPrice", ""),
                                                    new XAttribute("PerNightRoomRate", "0"),
                                                    new XAttribute("TotalRoomRate", room.Descendants("rate").FirstOrDefault().Attribute("net").Value),
                                                    new XAttribute("CancellationDate", ""),
                                                    new XAttribute("CancellationAmount", room.Descendants("rate").FirstOrDefault().Attribute("packaging").Value),
                                                     new XAttribute("sourcekey", room.Descendants("rate").FirstOrDefault().Attribute("sourcekey").Value),
                                                    new XAttribute("isAvailable", true),
                                                    new XElement("RequestID", ""),
                                                    new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                                      new XElement("Promotions", room.Descendants("rate").FirstOrDefault().Descendants("offer").FirstOrDefault() != null ? room.Descendants("rate").FirstOrDefault().Descendants("offer").FirstOrDefault().Value : string.Empty)
                                                         ),
                                                     new XElement("CancellationPolicy", ""),
                                                      new XElement("PriceBreakups", GetRoomsPriceBreakupHProout(room.Descendants("rate").FirstOrDefault().Descendants("dailyRate").ToList())
                                                          ),
                                                      new XElement("Amenities", ""),
                                                       new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                        new XElement("Supplements", Getsupplementsout(room.Descendants("supplement").ToList())
                                                            ),
                                                    new XElement("AdultNum", adult),
                                                    new XElement("ChildNum", child)));
            }
            
            return roomlst.OrderBy(x => x.Attribute("RoomSeq").Value).ToList();
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