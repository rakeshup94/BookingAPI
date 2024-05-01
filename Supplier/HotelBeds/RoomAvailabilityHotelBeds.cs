using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Common.DotW;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.HotelBeds;

namespace TravillioXMLOutService.App_Code
{
    public class RoomAvailabilityHotelBeds:IDisposable
    {
        XElement reqTravayoo;
        string dmc = string.Empty;
        string customerid = string.Empty;
        #region Credentails of HotelBeds
        string apiKey = string.Empty;
        string Secret = string.Empty;
        #endregion
        #region Hotel Availability of HotelBeds (XML OUT for Travayoo)
        public List<XElement> getroomavail_HBOUT(XElement req)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "4").ToList();
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
                        }
                        catch { custName = "HA"; }
                    }
                    else
                    {
                        try
                        {
                            customerid = htlele[i].Attribute("custID").Value;
                        }
                        catch { }
                        dmc = "HotelBeds";
                    }
                    List<XElement> getrom = CheckRoomAvailabilityHotelBedsOUT(req, htlid, dmc);
                    roomavailabilityresponse.Add(getrom.Descendants("Rooms").FirstOrDefault());
                }
                #endregion
                return roomavailabilityresponse;
            }
            catch { return null; }
        }
        public List<XElement> CheckRoomAvailabilityHotelBedsOUT(XElement req, string htlid, string xmltype)
        {
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                string hotelid = htlid;
                dmc = xmltype;
                string xtype = string.Empty;
                if (xmltype == "HotelBeds")
                {
                    xtype = null;
                }
                else
                {
                    xtype = "OUT";
                }  
                #region GetRoom
                HotelBeds_Detail htlhbstat = new HotelBeds_Detail();
                HotelBeds_HtStatic htlhbstaticdet = new HotelBeds_HtStatic();
                htlhbstat.HotelCode = hotelid.ToString();
                htlhbstat.custID = customerid;
                //htlhbstat.xmltype = xtype;
                htlhbstat.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value.ToString();
                DataTable dt = htlhbstaticdet.GetRooms_HotelBeds(htlhbstat);
                #endregion
                string response = string.Empty;
                using (var client = new WebClient())
                {
                    try
                    {

                        if (dt.Rows.Count > 0)
                        {
                            response = dt.Rows[0][0].ToString();
                        }
                        XElement availresponse = XElement.Parse(response.ToString());
                        XElement doc = RemoveAllNamespaces(availresponse);
                        XElement adddoc = new XElement("hotels", doc);
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(customerid);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 2;
                        log.LogType = "RoomAvail";
                        log.SupplierID = 4;
                        log.logrequestXML = req.ToString();
                        log.logresponseXML = adddoc.ToString();
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                        XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                        hotelavailabilityresponse = GetHotelListHotelBeds(adddoc).ToList();
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CheckRoomAvailabilityHotelBedsOUT";
                        ex1.PageName = "RoomAvailabilityHotelBeds";
                        ex1.CustomerID = customerid;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                }
                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckRoomAvailabilityHotelBedsOUT";
                ex1.PageName = "RoomAvailabilityHotelBeds";
                ex1.CustomerID = customerid;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        public List<XElement> CheckRoomAvailabilityHotelBeds(XElement req)
        {
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                //HotelBedsCredential _credential = new HotelBedsCredential();
                //apiKey = _credential.apiKey;
                //Secret = _credential.Secret;

                #region GetRoom
                HotelBeds_Detail htlhbstat = new HotelBeds_Detail();
                HotelBeds_HtStatic htlhbstaticdet = new HotelBeds_HtStatic();
                htlhbstat.HotelCode = req.Descendants("HotelID").FirstOrDefault().Value.ToString();
                htlhbstat.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value.ToString();
                DataTable dt = htlhbstaticdet.GetRooms_HotelBeds(htlhbstat);
                #endregion

                string response = string.Empty;
                using (var client = new WebClient())
                {
                    try
                    {
                        
                        if(dt.Rows.Count>0)
                        {
                            response = dt.Rows[0][0].ToString();
                        }
                        XElement availresponse = XElement.Parse(response.ToString());
                        XElement doc = RemoveAllNamespaces(availresponse);
                        XElement seldoc = doc.Descendants("hotel").Where(x => x.Attribute("code").Value == req.Descendants("HotelID").FirstOrDefault().Value).FirstOrDefault();
                        XElement adddoc = new XElement("hotels", seldoc);

                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 2;
                        log.LogType = "RoomAvail";
                        log.SupplierID = 4;
                        log.logrequestXML = req.ToString();
                        log.logresponseXML = adddoc.ToString();
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);

                        XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                        //List<XElement> hotelavailabilityres = adddoc.Descendants("hotel").ToList();

                        hotelavailabilityresponse = GetHotelListHotelBeds(adddoc).ToList();
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CheckRoomAvailabilityHotelBeds";
                        ex1.PageName = "RoomAvailabilityHotelBeds";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                }
                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckRoomAvailabilityHotelBeds";
                ex1.PageName = "RoomAvailabilityHotelBeds";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        public List<XElement> CheckRoomAvailabilityHotelBedsComment(XElement req)
        {
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                XElement staticdatahb = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\HotelBeds\hotelstatic_hb.xml"));
                List<XElement> statichotellist = staticdatahb.Descendants("hotel").Where(x => x.Attribute("destinationCode").Value == req.Descendants("CityCode").Single().Value.ToString()).ToList();
                List<XElement> htlst = staticdatahb.Descendants("hotel").ToList();
                string hotel = string.Empty;

                hotel = "<hotel>" + req.Descendants("HotelID").Single().Value + "</hotel>";
               
                DateTime checkindt = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
                string minstar = string.Empty;
                if (req.Descendants("MinStarRating").Single().Value == "0")
                {
                    minstar = "1";
                }
                else
                {
                    minstar = req.Descendants("MinStarRating").Single().Value;
                }
                List<XElement> troom = req.Descendants("RoomPax").ToList();
                XElement occupancyrequest = new XElement(
                                      new XElement("occupancies", getoccupancy(troom)));

                DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                DateTime checkoutdt2 = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkout2 = checkoutdt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string sourcemarket = Convert.ToString(req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value).ToUpper().ToString();

                string reqstr = "<?xml version='1.0' encoding='UTF-8'?>" +
                                   "<availabilityRQ sourceMarket='" + sourcemarket + "' xmlns='http://www.hotelbeds.com/schemas/messages' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' dailyRate='true'>" +
                                      "<stay checkIn='" + checkin2 + "' checkOut='" + checkout2 + "' />" +
                                      occupancyrequest.ToString() +
                                      "<hotels>" +//<hotel>6807</hotel><hotel>6808</hotel><hotel>6809</hotel><hotel>6810</hotel>" +
                    hotel +
                                      "</hotels>" +
                                      "<filter minCategory='" + minstar + "' maxCategory='" + req.Descendants("MaxStarRating").Single().Value + "' />" +
                                       "<filter maxRatesPerRoom='10' />" +
                                   "</availabilityRQ>";

                
                string endpoint = "https://api.test.hotelbeds.com/hotel-api/1.0/hotels";

                string signature;
                using (var sha = SHA256.Create())
                {
                    long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                    Console.WriteLine("Timestamp: " + ts);
                    var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(apiKey + Secret + ts));
                    signature = BitConverter.ToString(computedHash).Replace("-", "");
                }

                string response = string.Empty;
                using (var client = new WebClient())
                {
                    try
                    {
                        client.Headers.Add("X-Signature", signature);
                        client.Headers.Add("Api-Key", apiKey);
                        client.Headers.Add("Accept", "application/xml");
                        client.Headers.Add("Content-Type", "application/xml");
                        response = client.UploadString(endpoint, reqstr);




                        XElement availresponse = XElement.Parse(response.ToString());

                        XElement doc = RemoveAllNamespaces(availresponse);

                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 2;
                        log.LogType = "RoomAvail";
                        log.SupplierID = 4;
                        log.logrequestXML = req.ToString();
                        log.logresponseXML = doc.ToString();
                        APILog.SaveAPILogs(log);

                        XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                        //List<XElement> hotelavailabilityres = doc.Descendants("hotel").ToList();

                        hotelavailabilityresponse = GetHotelListHotelBeds(doc.Descendants("hotel").FirstOrDefault()).ToList();
                    }
                    catch (Exception ex)
                    {
                        APILog.SendExcepToDB(ex);
                    }
                }
                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                APILog.SendExcepToDB(ex);
                return null;
            }
        }
        #endregion
        #region Hotel Availability Request
        private List<XElement> getoccupancy(List<XElement> room)
        {
            #region Get Total Occupancy
            List<XElement> str = new List<XElement>();

            for (int i = 0; i < room.Count(); i++)
            {
                str.Add(new XElement("occupancy",
                       new XAttribute("rooms", Convert.ToString("1")),
                       new XAttribute("adults", Convert.ToString(room[i].Descendants("Adult").SingleOrDefault().Value)),
                       new XAttribute("children", Convert.ToString(room[i].Descendants("Child").SingleOrDefault().Value)),
                       new XElement("paxes", getadults(room[i]), getchildren(room[i])))
                );
            }
            return str;
            #endregion
        }
        private IEnumerable<XElement> getadults(XElement room)
        {
            #region Get Total Adults
            List<XElement> str = new List<XElement>();
            int rcount = Convert.ToInt32(room.Descendants("Adult").SingleOrDefault().Value);

            for (int i = 0; i < rcount; i++)
            {
                str.Add(new XElement("pax",
                       new XAttribute("type", "AD"))
                );
            }

            return str;
            #endregion
        }
        private IEnumerable<XElement> getchildren(XElement room)
        {
            #region Get Total Children
            List<XElement> str = new List<XElement>();
            List<XElement> tchild = room.Descendants("ChildAge").ToList();
            for (int i = 0; i < tchild.Count(); i++)
            {
                str.Add(new XElement("pax",
                       new XAttribute("type", "CH"),
                       new XAttribute("age", Convert.ToString(tchild[i].Value)))
                );
            }
            return str;
            #endregion
        }
        #endregion
        #region Hotel Detail Request
        public XElement HotelDetailHotelBeds(XElement req)
        {
            XElement hoteldetail = null;
            string hotelid = req.Descendants("HotelID").SingleOrDefault().Value;
            //string endpoint = "https://api.test.hotelbeds.com/hotel-content-api/1.0/hotels/" + hotelid + "?language=ENG";

            string signature;
            using (var sha = SHA256.Create())
            {
                long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                Console.WriteLine("Timestamp: " + ts);
                var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(apiKey + Secret + ts));
                signature = BitConverter.ToString(computedHash).Replace("-", "");
            }


            string response = string.Empty;
            using (var client = new WebClient())
            {
                // Request configuration            
                client.Headers.Add("X-Signature", signature);
                client.Headers.Add("Api-Key", apiKey);
                client.Headers.Add("Accept", "application/xml");

                // Request execution
                //response = client.DownloadString(endpoint);

                //XElement hoteldetailsresponse = XElement.Parse(response.ToString());
                HotelBeds_Detail htlhbstat = new HotelBeds_Detail();
                HotelBeds_HtStatic htlhbstaticdet = new HotelBeds_HtStatic();
                htlhbstat.HotelCode = req.Descendants("HotelID").SingleOrDefault().Value;
                DataTable dt = htlhbstaticdet.GetHotelList_HotelBeds(htlhbstat);
                XElement hoteldetailsresponse = null;
                if (dt.Rows.Count > 0)
                {
                    hoteldetailsresponse = XElement.Parse(dt.Rows[0]["HotelXML"].ToString());
                }

                XElement doc = RemoveAllNamespaces(hoteldetailsresponse);

                XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                XElement hotelavailabilityres = doc.Descendants("hotel").FirstOrDefault();

                hoteldetail = hotelavailabilityres;

            }
            return hoteldetail;
        }
        #endregion
        #region HotelBeds Hotel Listing
        private IEnumerable<XElement> GetHotelListHotelBeds(XElement hotels)
        {
            XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
            #region HotelBeds Hotels
            List<XElement> hotellst = new List<XElement>();
            try
            {
                //Int32 length = hotel.Count();
                XElement hotel = hotels.Descendants("hotel").FirstOrDefault();
                try
                {
                    //for (int i = 0; i < length; i++)
                    {
                        #region Fetch hotel
                        string starcategory = hotel.Attribute("categoryName").Value;
                        string[] split = starcategory.Split(' ');
                        string star = string.Empty;
                        List<XElement> rmtypelist = new List<XElement>();
                        try
                        {                            
                            //rmtypelist = hotel[i].Descendants("room").ToList();
                            star = Convert.ToString(Convert.ToInt32(split[0]));
                        }
                        catch (Exception ex) { star = ""; }
                        hotellst.Add(new XElement("Hotel",
                                               new XElement("HotelID", Convert.ToString(hotel.Attribute("code").Value)),
                                               new XElement("HotelName", Convert.ToString(hotel.Attribute("name").Value)),
                                               new XElement("PropertyTypeName", Convert.ToString("")),
                                               new XElement("CountryID", Convert.ToString("")), 
                                               new XElement("DMC", dmc),
                                               new XElement("SupplierID", "4"),
                                               new XElement("Currency", Convert.ToString(hotel.Attribute("currency").Value)),
                                               new XElement("Offers", ""),                                            
                                               roomgrp(hotel, hotel.Attribute("code").Value, Convert.ToString(hotel.Attribute("currency").Value))
                        ));

                        #endregion
                    };
                }
                catch (Exception ex)
                {
                    return hotellst;
                }
            }
            catch (Exception exe)
            {
                return hotellst;
            }
            return hotellst;
            #endregion
        }
        #endregion
        #region Room Grouping New (Part 1)
        private XElement roomgrp(XElement HbRooms, string Hotelcode, string currency)
        {
            foreach (XElement rate in HbRooms.Descendants("rate").Where(x => x.Attributes("childrenAges").Count() == 0))
                rate.Add(new XAttribute("childrenAges", "0"));
            foreach (XElement pax in reqTravayoo.Descendants("RoomPax"))
            {
                string s = null;
                if (pax.Element("Child").Value.Equals("0"))
                    pax.Add(new XElement("ChildAges", "0"));
                else
                {
                    foreach (XElement childage in pax.Descendants("ChildAge"))
                        s += childage.Value + ",";
                    pax.Add(new XElement("ChildAges", s.Substring(0, s.Length - 1)));
                }
            }
            var SupplGrouping = from rates in HbRooms.Descendants("rate")
                                group rates by new
                                {
                                    c1 = rates.Attribute("adults").Value,
                                    c2 = rates.Attribute("children").Value,
                                    c3 = rates.Attribute("childrenAges").Value,
                                    c4 = rates.Attribute("boardCode").Value
                                };
            XElement Grouping = new XElement("Groups");
            List<string> allMeals = new List<string>();
            foreach (var group in SupplGrouping)
            {
                if (!allMeals.Contains(group.Key.c4))
                    allMeals.Add(group.Key.c4);
            }
            foreach (string meal in allMeals)
            {
                Grouping.Add(new XElement("MealIncluded", new XAttribute("Name", meal)));
                foreach (XElement room in reqTravayoo.Descendants("RoomPax"))
                {
                    int child = 0, infant = 0;
                    if (room.Descendants("ChildAge").Any())
                    {
                        foreach (XElement age in room.Descendants("ChildAge"))
                        {
                            if (Convert.ToInt32(age.Value) <= 2)
                                child++;
                            else
                                infant++;
                        }
                    }
                    string paxes = room.Element("Adult").Value + "-" + room.Element("Child").Value + "-" + room.Element("ChildAges").Value;
                    var entries = SupplGrouping.Where(x => (x.Key.c1 + "-" + x.Key.c2 + "-" + x.Key.c3).Equals(paxes) && x.Key.c4.Equals(meal));
                    Grouping.Elements("MealIncluded").Where(x => x.Attribute("Name").Value.Equals(meal)).FirstOrDefault().Add(new XElement("Group", new XAttribute("Paxes", paxes), entries));
                }
            }
            int roomcount = reqTravayoo.Descendants("RoomPax").Count();
            XElement FinalGroup = new XElement("Groups");
            #region Room Grouping New
            int take = 0;

            if (roomcount == 9 || roomcount == 8)
                take = 4;
            else if (roomcount > 6 && roomcount < 8)
                take = 5;
            else if (roomcount < 7 && roomcount > 3)
                take = 10;
            else if (roomcount == 3)
                take = 20;
            else if (roomcount == 2)
                take = 30;
            else if (roomcount == 1)
                take = 200;
            List<string> Roomtypes = new List<string>();
            foreach (XElement rt in HbRooms.Descendants("room"))
                Roomtypes.Add(rt.Attribute("code").Value);
            foreach (XElement meal in Grouping.Descendants("MealIncluded"))
            {
                List<string> Room1 = new List<string>();
                List<string> Room2 = new List<string>();
                List<string> Room3 = new List<string>();
                List<string> Room4 = new List<string>();
                List<string> Room5 = new List<string>();
                List<string> Room6 = new List<string>();
                List<string> Room7 = new List<string>();
                List<string> Room8 = new List<string>();
                List<string> Room9 = new List<string>();
                XElement RoomtypeData = stringAsNode(Roomtypes, new XElement("RoomTypeList"));
                int roomCounter = 1;
                foreach (XElement room in reqTravayoo.Descendants("RoomPax"))
                {
                    int child = 0, infant = 0;
                    if (room.Descendants("ChildAge").Any())
                    {
                        foreach (XElement age in room.Descendants("ChildAge"))
                        {
                            if (Convert.ToInt32(age.Value) <= 2)
                                child++;
                            else
                                infant++;
                        }
                    }
                    string paxes = room.Element("Adult").Value + "-" + room.Element("Child").Value + "-" + room.Element("ChildAges").Value;
                    if (roomCounter == 1)
                    {
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements())
                        {
                            string rType = roomfortypes.Attribute("rateKey").Value.Split(new char[] { '|' })[5];
                            if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(rType)).Any() && Room1.Count < take)
                                Room1.Add(roomfortypes.Attribute("rateKey").Value + "," + roomfortypes.Attribute("allotment").Value);
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room1.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 2)
                    {
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements())
                        {
                            string rType = roomfortypes.Attribute("rateKey").Value.Split(new char[] { '|' })[5];
                            if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(rType)).Any() && Room2.Count < take)
                                Room2.Add(roomfortypes.Attribute("rateKey").Value + "," + roomfortypes.Attribute("allotment").Value);
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room2.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 3)
                    {
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements())
                        {
                            string rType = roomfortypes.Attribute("rateKey").Value.Split(new char[] { '|' })[5];
                            if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(rType)).Any() && Room3.Count < take)
                                Room3.Add(roomfortypes.Attribute("rateKey").Value + "," + roomfortypes.Attribute("allotment").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room3.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 4)
                    {
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements())
                        {
                            string rType = roomfortypes.Attribute("rateKey").Value.Split(new char[] { '|' })[5];
                            if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(rType)).Any() && Room4.Count < take)
                                Room4.Add(roomfortypes.Attribute("rateKey").Value + "," + roomfortypes.Attribute("allotment").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room4.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 5)
                    {
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements())
                        {
                            string rType = roomfortypes.Attribute("rateKey").Value.Split(new char[] { '|' })[5];
                            if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(rType)).Any() && Room5.Count < take)
                                Room5.Add(roomfortypes.Attribute("rateKey").Value + "," + roomfortypes.Attribute("allotment").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room5.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 6)
                    {
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements())
                        {
                            string rType = roomfortypes.Attribute("rateKey").Value.Split(new char[] { '|' })[5];
                            if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(rType)).Any() && Room6.Count < take)
                                Room6.Add(roomfortypes.Attribute("rateKey").Value + "," + roomfortypes.Attribute("allotment").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room6.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 7)
                    {
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements())
                        {
                            string rType = roomfortypes.Attribute("rateKey").Value.Split(new char[] { '|' })[5];
                            if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(rType)).Any() && Room7.Count < take)
                                Room7.Add(roomfortypes.Attribute("rateKey").Value + "," + roomfortypes.Attribute("allotment").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room7.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 8)
                    {
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements())
                        {
                            string rType = roomfortypes.Attribute("rateKey").Value.Split(new char[] { '|' })[5];
                            if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(rType)).Any() && Room8.Count < take)
                                Room8.Add(roomfortypes.Attribute("rateKey").Value + "," + roomfortypes.Attribute("allotment").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room8.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 9)
                    {
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements())
                        {
                            string rType = roomfortypes.Attribute("rateKey").Value.Split(new char[] { '|' })[5];
                            if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(rType)).Any() && Room9.Count < take)
                                Room9.Add(roomfortypes.Attribute("rateKey").Value + "," + roomfortypes.Attribute("allotment").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room9.Contains(x.Value)).Remove();
                    }
                    roomCounter++;
                }
                #region 1 Room
                if (roomcount == 1)
                {
                    foreach (string rm1 in Room1)
                    {
                        //FinalGroup.Add(new XElement("Room", rm1));
                        string CurrentConfig = string.Empty;
                        XElement Currentgroup = new XElement("Room",
                                            new XElement("type", rm1));
                        if (Convert.ToInt32(rm1.Split(new char[] { ',' })[1]) > 0)
                        {
                            FinalGroup.Add(Currentgroup);
                        }
                    }
                }
                #endregion
                #region 2 Rooms
                else if (roomcount == 2)
                {
                    foreach (string rm1 in Room1)
                    {
                        foreach (string rm2 in Room2)
                        {
                            string CurrentConfig = string.Empty;
                            bool AllotmentCheck = true;
                            string[] split1 = rm1.Split(new char[] { ',' });
                            string[] split2 = rm2.Split(new char[] { ',' });
                            if (split1[0].Equals(split2[0]))
                            {
                                if (Convert.ToInt32(split1[1]) < 2 && Convert.ToInt32(split2[1]) < 2)
                                    AllotmentCheck = false;
                            }
                            if (AllotmentCheck)
                            {
                                XElement Currentgroup = new XElement("Room",
                                                                        new XElement("type", rm1),
                                                                        new XElement("type", rm2));
                                FinalGroup.Add(Currentgroup);
                            }

                        }
                    }
                }
                #endregion
                #region 3 Rooms
                else if (roomcount == 3)
                {
                    foreach (string rm1 in Room1)
                    {
                        foreach (string rm2 in Room2)
                        {
                            foreach (string rm3 in Room3)
                            {
                                string CurrentConfig = string.Empty;
                                XElement Currentgroup = new XElement("Room",
                                                    new XElement("type", rm1),
                                                    new XElement("type", rm2),
                                                    new XElement("type", rm3));
                                bool AllotmentCheck = true;
                                foreach (string rt in Roomtypes)
                                {
                                    XElement GetType = Currentgroup.Elements("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).FirstOrDefault();
                                    if (GetType != null)
                                    {
                                        int allotment = Convert.ToInt32(GetType.Value.Split(new char[] { ',' })[1]);
                                        if (Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count() > allotment)
                                            AllotmentCheck = false;
                                        CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count().ToString() + "-";
                                    }
                                }
                                CurrentConfig += meal.Attribute("Name").Value;
                                if (AllotmentCheck)
                                {
                                    Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                    if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                        FinalGroup.Add(Currentgroup);
                                }
                                else break;
                            }
                        }
                    }
                }
                #endregion
                #region 4 Rooms
                else if (roomcount == 4)
                {
                    foreach (string rm1 in Room1)
                    {
                        foreach (string rm2 in Room2)
                        {
                            foreach (string rm3 in Room3)
                            {
                                foreach (string rm4 in Room4)
                                {
                                    string CurrentConfig = string.Empty;
                                    XElement Currentgroup = new XElement("Room",
                                                        new XElement("type", rm1),
                                                        new XElement("type", rm2),
                                                        new XElement("type", rm3),
                                                        new XElement("type", rm4));
                                    bool AllotmentCheck = true;
                                    foreach (string rt in Roomtypes)
                                    {
                                        XElement GetType = Currentgroup.Elements("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).FirstOrDefault();
                                        if (GetType != null)
                                        {
                                            int allotment = Convert.ToInt32(GetType.Value.Split(new char[] { ',' })[1]);
                                            if (Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count() > allotment)
                                                AllotmentCheck = false;
                                            CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count().ToString() + "-";
                                        }
                                    }
                                    CurrentConfig += meal.Attribute("Name").Value;
                                    if (AllotmentCheck)
                                    {
                                        Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                        if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                            FinalGroup.Add(Currentgroup);
                                    }
                                    else break;
                                }
                            }
                        }
                    }
                }
                #endregion
                #region 5 Rooms
                else if (roomcount == 5)
                {
                    foreach (string rm1 in Room1)
                    {
                        foreach (string rm2 in Room2)
                        {
                            foreach (string rm3 in Room3)
                            {
                                foreach (string rm4 in Room4)
                                {
                                    foreach (string rm5 in Room5)
                                    {
                                        string CurrentConfig = string.Empty;
                                        XElement Currentgroup = new XElement("Room",
                                                            new XElement("type", rm1),
                                                            new XElement("type", rm2),
                                                            new XElement("type", rm3),
                                                            new XElement("type", rm4),
                                                            new XElement("type", rm5));
                                        bool AllotmentCheck = true;
                                        foreach (string rt in Roomtypes)
                                        {
                                            XElement GetType = Currentgroup.Elements("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).FirstOrDefault();
                                            if (GetType != null)
                                            {
                                                int allotment = Convert.ToInt32(GetType.Value.Split(new char[] { ',' })[1]);
                                                if (Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count() > allotment)
                                                    AllotmentCheck = false;
                                                CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count().ToString() + "-";
                                            }
                                        }
                                        CurrentConfig += meal.Attribute("Name").Value;
                                        if (AllotmentCheck)
                                        {
                                            Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                            if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                FinalGroup.Add(Currentgroup);
                                        }
                                        else break;
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                #region 6 Rooms
                else if (roomcount == 6)
                {
                    foreach (string rm1 in Room1)
                    {
                        foreach (string rm2 in Room2)
                        {
                            foreach (string rm3 in Room3)
                            {
                                foreach (string rm4 in Room4)
                                {
                                    foreach (string rm5 in Room5)
                                    {
                                        foreach (string rm6 in Room6)
                                        {
                                            string CurrentConfig = string.Empty;
                                            XElement Currentgroup = new XElement("Room",
                                                                new XElement("type", rm1),
                                                                new XElement("type", rm2),
                                                                new XElement("type", rm3),
                                                                new XElement("type", rm4),
                                                                new XElement("type", rm5),
                                                                new XElement("type", rm6));
                                            bool AllotmentCheck = true;
                                            foreach (string rt in Roomtypes)
                                            {
                                                XElement GetType = Currentgroup.Elements("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).FirstOrDefault();
                                                if (GetType != null)
                                                {
                                                    int allotment = Convert.ToInt32(GetType.Value.Split(new char[] { ',' })[1]);
                                                    if (Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count() > allotment)
                                                        AllotmentCheck = false;
                                                    CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count().ToString() + "-";
                                                }
                                            }
                                            CurrentConfig += meal.Attribute("Name").Value;
                                            if (AllotmentCheck)
                                            {
                                                Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                    FinalGroup.Add(Currentgroup);
                                            }
                                            else break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                #region 7 Rooms
                else if (roomcount == 7)
                {
                    foreach (string rm1 in Room1)
                    {
                        foreach (string rm2 in Room2)
                        {
                            foreach (string rm3 in Room3)
                            {
                                foreach (string rm4 in Room4)
                                {
                                    foreach (string rm5 in Room5)
                                    {
                                        foreach (string rm6 in Room6)
                                        {
                                            foreach (string rm7 in Room7)
                                            {
                                                string CurrentConfig = string.Empty;
                                                XElement Currentgroup = new XElement("Room",
                                                                    new XElement("type", rm1),
                                                                    new XElement("type", rm2),
                                                                    new XElement("type", rm3),
                                                                    new XElement("type", rm4),
                                                                    new XElement("type", rm5),
                                                                    new XElement("type", rm6),
                                                                    new XElement("type", rm7));
                                                bool AllotmentCheck = true;
                                                foreach (string rt in Roomtypes)
                                                {
                                                    XElement GetType = Currentgroup.Elements("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).FirstOrDefault();
                                                    if (GetType != null)
                                                    {
                                                        int allotment = Convert.ToInt32(GetType.Value.Split(new char[] { ',' })[1]);
                                                        if (Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count() > allotment)
                                                            AllotmentCheck = false;
                                                        CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count().ToString() + "-";
                                                    }
                                                }
                                                CurrentConfig += meal.Attribute("Name").Value;
                                                if (AllotmentCheck)
                                                {
                                                    Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                    if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                        FinalGroup.Add(Currentgroup);
                                                }
                                                else break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                #region 8 Rooms
                else if (roomcount == 8)
                {
                    foreach (string rm1 in Room1)
                    {
                        foreach (string rm2 in Room2)
                        {
                            foreach (string rm3 in Room3)
                            {
                                foreach (string rm4 in Room4)
                                {
                                    foreach (string rm5 in Room5)
                                    {
                                        foreach (string rm6 in Room6)
                                        {
                                            foreach (string rm7 in Room7)
                                            {
                                                foreach (string rm8 in Room8)
                                                {
                                                    string CurrentConfig = string.Empty;
                                                    XElement Currentgroup = new XElement("Room",
                                                                        new XElement("type", rm1),
                                                                        new XElement("type", rm2),
                                                                        new XElement("type", rm3),
                                                                        new XElement("type", rm4),
                                                                        new XElement("type", rm5),
                                                                        new XElement("type", rm6),
                                                                        new XElement("type", rm7),
                                                                        new XElement("type", rm8));
                                                    bool AllotmentCheck = true;
                                                    foreach (string rt in Roomtypes)
                                                    {
                                                        XElement GetType = Currentgroup.Elements("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).FirstOrDefault();
                                                        if (GetType != null)
                                                        {
                                                            int allotment = Convert.ToInt32(GetType.Value.Split(new char[] { ',' })[1]);
                                                            if (Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count() > allotment)
                                                                AllotmentCheck = false;
                                                            CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count().ToString() + "-";
                                                        }
                                                    }
                                                    CurrentConfig += meal.Attribute("Name").Value;
                                                    if (AllotmentCheck)
                                                    {
                                                        Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                        if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                            FinalGroup.Add(Currentgroup);
                                                    }
                                                    else break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                #region 9 Rooms
                else if (roomcount == 9)
                {

                    foreach (string rm1 in Room1)
                    {
                        foreach (string rm2 in Room2)
                        {
                            foreach (string rm3 in Room3)
                            {
                                foreach (string rm4 in Room4)
                                {
                                    foreach (string rm5 in Room5)
                                    {
                                        foreach (string rm6 in Room6)
                                        {
                                            foreach (string rm7 in Room7)
                                            {
                                                foreach (string rm8 in Room8)
                                                {
                                                    foreach (string rm9 in Room9)
                                                    {
                                                        string CurrentConfig = string.Empty;
                                                        XElement Currentgroup = new XElement("Room",
                                                                            new XElement("type", rm1),
                                                                            new XElement("type", rm2),
                                                                            new XElement("type", rm3),
                                                                            new XElement("type", rm4),
                                                                            new XElement("type", rm5),
                                                                            new XElement("type", rm6),
                                                                            new XElement("type", rm7),
                                                                            new XElement("type", rm8),
                                                                            new XElement("type", rm9));
                                                        bool AllotmentCheck = true;
                                                        foreach (string rt in Roomtypes)
                                                        {
                                                            XElement GetType = Currentgroup.Elements("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).FirstOrDefault();
                                                            if (GetType != null)
                                                            {
                                                                int allotment = Convert.ToInt32(GetType.Value.Split(new char[] { ',' })[1]);
                                                                if (Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count() > allotment)
                                                                    AllotmentCheck = false;
                                                                CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { '|' })[5].Equals(rt)).Count().ToString() + "-";
                                                            }
                                                        }
                                                        CurrentConfig += meal.Attribute("Name").Value;
                                                        if (AllotmentCheck)
                                                        {
                                                            Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                            if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                                FinalGroup.Add(Currentgroup);
                                                        }
                                                        else break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion

            }
            #endregion

            XElement Rooms = new XElement("Rooms");
            int index = 1;
            foreach (XElement room in FinalGroup.Descendants("Room"))
            {
                Rooms.Add(Roomtag(room, HbRooms, index++, Hotelcode, currency));
            }
            return Rooms;
        }
        #endregion
        #region Hotel's Room Grouping (Part 2)
        public XElement Roomtag(XElement roomElement, XElement hotelElement, int index, string Hotelcode, string currency)
        {
            List<XElement> RoomTypestag = new List<XElement>();
            int cnt = 1;
            decimal totalrate = 0;
           
           
            foreach (XElement room1 in roomElement.Descendants("type"))
            {
                XElement room = hotelElement.Descendants("rate").Where(x => x.Attribute("rateKey").Value.Equals(room1.Value.Split(new char[] { ',' })[0])).FirstOrDefault();
                int nights = room.Descendants("dailyRate").Count();
                #region Rate Comment
                string ratecommentid = string.Empty;
                try
                {
                    if (room.Attribute("rateCommentsId") != null)
                    {
                        ratecommentid = room.Attribute("rateCommentsId").Value;
                    }
                }
                catch { }
                #endregion
                RoomTypestag.Add(new XElement("Room",
                                                   new XAttribute("ID", room.FirstAttribute.Value),
                                                      new XAttribute("SuppliersID", "4"),
                                                      new XAttribute("RoomSeq", Convert.ToString(cnt++)),
                                                      new XAttribute("SessionID", room.FirstAttribute.Value),
                                                      new XAttribute("RoomType", room.Parent.Parent.Attribute("name").Value),
                                                      new XAttribute("OccupancyID", ratecommentid),
                                                      new XAttribute("OccupancyName", ""),
                                                      new XAttribute("MealPlanID", ""),
                                                      new XAttribute("MealPlanName", room.Attribute("boardName").Value),
                                                      new XAttribute("MealPlanCode", room.Attribute("boardCode").Value),
                                                      new XAttribute("MealPlanPrice", ""),
                                                      new XAttribute("PerNightRoomRate", Convert.ToString(Convert.ToDouble(room.Attribute("net").Value) / nights)),
                                                      new XAttribute("TotalRoomRate", room.Attribute("net").Value),
                                                      new XAttribute("CancellationDate", ""),
                                                      new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", "true"),
                                                      new XElement("RequestID"),
                                                      new XElement("Offers"),
                                                      new XElement("PromotionList",GetHotelpromotionsHotelBeds(room.Descendants("promotion").ToList()), GetHotelpromotionsHotelBeds(room.Descendants("offer").ToList())),                                                     
                                                      new XElement("CancellationPolicy"),
                                                      new XElement("Amenities",
                                                          new XElement("Amenity")),
                                                      new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                      new XElement("Supplements"),
                                                      pricebreakup(hotelElement.Descendants("rate").Where(x => x.FirstAttribute.Value.Equals(room.FirstAttribute.Value)).First().Element("dailyRates")),
                                                      new XElement("AdultNum", room.Attribute("adults").Value),
                                                      new XElement("ChildNum", room.Attribute("children").Value)));
                totalrate = totalrate + Convert.ToDecimal(room.Attribute("net").Value);
            }
            XElement RoomTypes = new XElement("RoomTypes",
                                        new XAttribute("TotalRate", totalrate),
                                        new XAttribute("Index", Convert.ToString(index)), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc),new XAttribute("CUID",customerid)
                                        , RoomTypestag);

            return RoomTypes;            
        }
        #endregion
        #region Price Breakups (New)
        private static XElement pricebreakup(XElement Prices)
        {
            XElement response = new XElement("PriceBreakups");
            foreach (XElement dailyprice in Prices.Elements())
            {
                response.Add(new XElement("Price",
                                 new XAttribute("Night", dailyprice.FirstAttribute.Value),
                                 new XAttribute("PriceValue", dailyprice.Attribute("dailyNet").Value)));
            }
            return response;
        }
        #endregion
        #region HotelBeds Hotel's Room Listing (OLD)
        public IEnumerable<XElement> GetHotelRoomListingHotelBeds(List<XElement> roomlist, string Hotelcode, string currency)
        {
            XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
            List<XElement> str = new List<XElement>();
            List<XElement> roomList1 = new List<XElement>();
            List<XElement> roomList2 = new List<XElement>();
            List<XElement> roomList3 = new List<XElement>();
            List<XElement> roomList4 = new List<XElement>();
            List<XElement> roomList5 = new List<XElement>();
            List<XElement> roomList6 = new List<XElement>();
            List<XElement> roomList7 = new List<XElement>();
            List<XElement> roomList8 = new List<XElement>();
            List<XElement> roomList9 = new List<XElement>();
            
            int totalroom = Convert.ToInt32(reqTravayoo.Descendants("RoomPax").Count());

            #region Notes: The maximum number of rooms that can be retrieved by a single search request is nine (9)
            #endregion

            #region Room Count 1
            if (totalroom == 1)
            {
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").Descendants("ChildAge").ToList();
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                }
                #endregion
                #endregion

                for (int m = 0; m < roomList1.Count(); m++)
                {
                    List<XElement> pricebrkups = roomList1[m].Descendants("dailyRate").ToList();
                    List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();
                    List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();

                    #region With Board Bases
                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].Attribute("net").Value), new XAttribute("Index", m + 1), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc),
                        new XElement("Room",
                             new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                             new XAttribute("SuppliersID", "4"),
                             new XAttribute("RoomSeq", "1"),
                             new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                             new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                             new XAttribute("OccupancyID", Convert.ToString("")),
                             new XAttribute("OccupancyName", Convert.ToString("")),
                             new XAttribute("MealPlanID", Convert.ToString("")),
                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                             new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                             new XAttribute("MealPlanPrice", ""),
                             new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                             new XAttribute("CancellationDate", ""),
                             new XAttribute("CancellationAmount", ""),
                             new XAttribute("isAvailable", "true"),
                             new XElement("RequestID", Convert.ToString("")),
                             new XElement("Offers", ""),
                             new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                        //new XElement("Promotions", Convert.ToString(promo))),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements", ""),
                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups)),
                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                             )));
                    #endregion
                }
                return str;
            }
            #endregion

            #region Room Count 2
            if (totalroom == 2)
            {
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children2ages = string.Empty;
                    List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                    int totc2 = child2age.Count();
                    for (int i = 0; i < child2age.Count(); i++)
                    {
                        if (i == totc2 - 1)
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value);
                        }
                        else
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                        }
                    }
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                }
                #endregion
                #endregion

                int group = 0;
                //Parallel.For(0, roomList1.Count(), m =>
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    //Parallel.For(0, roomList2.Count(), n =>
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        string bb1 = roomList1[m].Attribute("boardCode").Value;
                        string bb2 = roomList2[n].Attribute("boardCode").Value;
                        if (bb1 == bb2)
                        {                            
                            int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                            if (totalroom <= totalallots)
                            {
                                #region check allotments
                                String condition = "";
                                List<string> ratekeylist = new List<string>();
                                ratekeylist.Add(roomList1[m].Attribute("rateKey").Value);
                                ratekeylist.Add(roomList2[n].Attribute("rateKey").Value);
                                var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new { Key = ax.Key, Count = ax.Count() });
                                int k = 0;
                                foreach (var item in grouped)
                                {
                                    var rtkey = item.Key;
                                    var count = item.Count;
                                    int totalt = roomlist.Descendants("rate").Where(x => x.Attribute("rateKey").Value == rtkey).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                    if (k == grouped.Count() - 1)
                                    {
                                        condition = condition + totalt + " >= " + count;
                                    }
                                    else
                                    {
                                        condition = condition + totalt + " >= " + count + " and ";
                                    }
                                    k++;
                                }
                                System.Data.DataTable table = new System.Data.DataTable();
                                table.Columns.Add("", typeof(Boolean));
                                table.Columns[0].Expression = condition;

                                System.Data.DataRow ckr = table.NewRow();
                                table.Rows.Add(ckr);
                                bool _condition = (Boolean)ckr[0];
                                if (_condition)
                                {
                                    List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                                    List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                                    List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();
                                    List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();
                                    List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();
                                    List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();

                                    #region Board Bases >0
                                    group++;
                                    decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value);
                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                                     new XAttribute("SuppliersID", "4"),
                                     new XAttribute("RoomSeq", "1"),
                                     new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                                     new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                     new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                     new XAttribute("PerNightRoomRate", Convert.ToString("20")),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", "true"),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                                        //new XElement("Promotions", Convert.ToString(promo1))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements", ""
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups1)),
                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                                     ),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("code").Value)),
                                     new XAttribute("SuppliersID", "4"),
                                     new XAttribute("RoomSeq", "2"),
                                     new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("rateKey").Value)),
                                     new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("name").Value)),
                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                     new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                     new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("net").Value)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", "true"),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions2), GetHotelpromotionsHotelBeds(offer2)),
                                        //new XElement("Promotions", Convert.ToString(promo2))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements", ""
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups2)),
                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                                     )));




                                    #endregion

                                    if (group > 550)
                                    { return str; }

                                }
                                #endregion
                            }
                        }
                    }
                }
                return str;
            }
            #endregion

            #region Room Count 3
            if (totalroom == 3)
            {

                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children2ages = string.Empty;
                    List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                    int totc2 = child2age.Count();
                    for (int i = 0; i < child2age.Count(); i++)
                    {
                        if (i == totc2 - 1)
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value);
                        }
                        else
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                        }
                    }
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children3ages = string.Empty;
                    List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                    int totc3 = child3age.Count();
                    for (int i = 0; i < child3age.Count(); i++)
                    {
                        if (i == totc3 - 1)
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value);
                        }
                        else
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                        }
                    }
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                }
                #endregion
                #endregion
                int group = 0;

                #region Room 3
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            // add room 1, 2, 3

                            string bb1 = roomList1[m].Attribute("boardCode").Value;
                            string bb2 = roomList2[n].Attribute("boardCode").Value;
                            string bb3 = roomList3[o].Attribute("boardCode").Value;
                            if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3)
                            {
                                
                                int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                if (totalroom <= totalallots)
                                {
                                    #region check allotments
                                    String condition = "";
                                    List<string> ratekeylist = new List<string>();
                                    ratekeylist.Add(roomList1[m].Attribute("rateKey").Value);
                                    ratekeylist.Add(roomList2[n].Attribute("rateKey").Value);
                                    ratekeylist.Add(roomList3[o].Attribute("rateKey").Value);
                                    var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new { Key = ax.Key, Count = ax.Count() });
                                    int k = 0;
                                    foreach (var item in grouped)
                                    {
                                        var rtkey = item.Key;
                                        var count = item.Count;
                                        int totalt = roomlist.Descendants("rate").Where(x => x.Attribute("rateKey").Value == rtkey).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                        if (k == grouped.Count() - 1)
                                        {
                                            condition = condition + totalt + " >= " + count;
                                        }
                                        else
                                        {
                                            condition = condition + totalt + " >= " + count + " and ";
                                        }
                                        k++;
                                    }
                                    System.Data.DataTable table = new System.Data.DataTable();
                                    table.Columns.Add("", typeof(Boolean));
                                    table.Columns[0].Expression = condition;

                                    System.Data.DataRow ckr = table.NewRow();
                                    table.Rows.Add(ckr);
                                    bool _condition = (Boolean)ckr[0];
                                    if (_condition)
                                    {
                                        #region room's group
                                        List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                                        List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                                        List<XElement> pricebrkups3 = roomList3[o].Descendants("dailyRate").ToList();

                                        List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();

                                        List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();

                                        List<XElement> promotions3 = roomList3[o].Descendants("promotion").ToList();

                                        List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();

                                        List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();

                                        List<XElement> offer3 = roomList3[o].Descendants("offer").ToList();


                                        group++;
                                        decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value);

                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                                         new XAttribute("SuppliersID", "4"),
                                         new XAttribute("RoomSeq", "1"),
                                         new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                                         new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                         new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", "true"),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                                            // new XElement("Promotions", Convert.ToString(promo1))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements", ""
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups1)),
                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("code").Value)),
                                         new XAttribute("SuppliersID", "4"),
                                         new XAttribute("RoomSeq", "2"),
                                         new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("rateKey").Value)),
                                         new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("name").Value)),
                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                         new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("net").Value)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", "true"),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions2), GetHotelpromotionsHotelBeds(offer2)),
                                            //new XElement("Promotions", Convert.ToString(promo2))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements", ""
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups2)),
                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("code").Value)),
                                         new XAttribute("SuppliersID", "4"),
                                         new XAttribute("RoomSeq", "3"),
                                         new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("rateKey").Value)),
                                         new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("name").Value)),
                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                         new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("net").Value)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", "true"),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions3), GetHotelpromotionsHotelBeds(offer3)),
                                            //new XElement("Promotions", Convert.ToString(promo3))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements", ""
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups3)),
                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Attribute("adults").Value)),
                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Attribute("children").Value))
                                         )));
                                        #endregion
                                        if (group > 550)
                                        { return str; }

                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion

            #region Room Count 4
            if (totalroom == 4)
            {

                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children2ages = string.Empty;
                    List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                    int totc2 = child2age.Count();
                    for (int i = 0; i < child2age.Count(); i++)
                    {
                        if (i == totc2 - 1)
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value);
                        }
                        else
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                        }
                    }
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children3ages = string.Empty;
                    List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                    int totc3 = child3age.Count();
                    for (int i = 0; i < child3age.Count(); i++)
                    {
                        if (i == totc3 - 1)
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value);
                        }
                        else
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                        }
                    }
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children4ages = string.Empty;
                    List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                    int totc4 = child4age.Count();
                    for (int i = 0; i < child4age.Count(); i++)
                    {
                        if (i == totc4 - 1)
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value);
                        }
                        else
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                        }
                    }
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                }
                #endregion
                #endregion

                int group = 0;

                #region Room 4
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                // add room 1, 2, 3,4

                                string bb1 = roomList1[m].Attribute("boardCode").Value;
                                string bb2 = roomList2[n].Attribute("boardCode").Value;
                                string bb3 = roomList3[o].Attribute("boardCode").Value;
                                string bb4 = roomList4[p].Attribute("boardCode").Value;
                                if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4)
                                {
                                    
                                    int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                    if (totalroom <= totalallots)
                                    {
                                        #region check allotments
                                        String condition = "";
                                        List<string> ratekeylist = new List<string>();
                                        ratekeylist.Add(roomList1[m].Attribute("rateKey").Value);
                                        ratekeylist.Add(roomList2[n].Attribute("rateKey").Value);
                                        ratekeylist.Add(roomList3[o].Attribute("rateKey").Value);
                                        ratekeylist.Add(roomList4[p].Attribute("rateKey").Value);
                                        var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new { Key = ax.Key, Count = ax.Count() });
                                        int k = 0;
                                        foreach (var item in grouped)
                                        {
                                            var rtkey = item.Key;
                                            var count = item.Count;
                                            int totalt = roomlist.Descendants("rate").Where(x => x.Attribute("rateKey").Value == rtkey).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                            if (k == grouped.Count() - 1)
                                            {
                                                condition = condition + totalt + " >= " + count;
                                            }
                                            else
                                            {
                                                condition = condition + totalt + " >= " + count + " and ";
                                            }
                                            k++;
                                        }
                                        System.Data.DataTable table = new System.Data.DataTable();
                                        table.Columns.Add("", typeof(Boolean));
                                        table.Columns[0].Expression = condition;

                                        System.Data.DataRow ckr = table.NewRow();
                                        table.Rows.Add(ckr);
                                        bool _condition = (Boolean)ckr[0];
                                        if (_condition)
                                        {
                                            #region room's group
                                            List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                                            List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                                            List<XElement> pricebrkups3 = roomList3[o].Descendants("dailyRate").ToList();
                                            List<XElement> pricebrkups4 = roomList4[p].Descendants("dailyRate").ToList();

                                            List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();

                                            List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();

                                            List<XElement> promotions3 = roomList3[o].Descendants("promotion").ToList();

                                            List<XElement> promotions4 = roomList4[p].Descendants("promotion").ToList();
                                            List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();

                                            List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();

                                            List<XElement> offer3 = roomList3[o].Descendants("offer").ToList();

                                            List<XElement> offer4 = roomList4[p].Descendants("offer").ToList();


                                            group++;
                                            decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value);

                                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                                             new XAttribute("SuppliersID", "4"),
                                             new XAttribute("RoomSeq", "1"),
                                             new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                                             new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                             new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", "true"),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                                                //new XElement("Promotions", Convert.ToString(promo1))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements", ""
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups1)),
                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("code").Value)),
                                             new XAttribute("SuppliersID", "4"),
                                             new XAttribute("RoomSeq", "2"),
                                             new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("rateKey").Value)),
                                             new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("name").Value)),
                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                             new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("net").Value)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", "true"),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions2), GetHotelpromotionsHotelBeds(offer2)),
                                                //new XElement("Promotions", Convert.ToString(promo2))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements", ""
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups2)),
                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("code").Value)),
                                             new XAttribute("SuppliersID", "4"),
                                             new XAttribute("RoomSeq", "3"),
                                             new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("rateKey").Value)),
                                             new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("name").Value)),
                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                             new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("net").Value)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", "true"),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions3), GetHotelpromotionsHotelBeds(offer3)),
                                                // new XElement("Promotions", Convert.ToString(promo3))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements", ""
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups3)),
                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Attribute("adults").Value)),
                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Attribute("children").Value))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomList4[p].Parent.Parent.Attribute("code").Value)),
                                             new XAttribute("SuppliersID", "4"),
                                             new XAttribute("RoomSeq", "4"),
                                             new XAttribute("SessionID", Convert.ToString(roomList4[p].Attribute("rateKey").Value)),
                                             new XAttribute("RoomType", Convert.ToString(roomList4[p].Parent.Parent.Attribute("name").Value)),
                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList4[p].Attribute("boardName").Value)),
                                             new XAttribute("MealPlanCode", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].Attribute("net").Value)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", "true"),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions4), GetHotelpromotionsHotelBeds(offer4)),
                                                // new XElement("Promotions", Convert.ToString(promo4))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements", ""
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups4)),
                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Attribute("adults").Value)),
                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Attribute("children").Value))
                                             )));
                                            #endregion
                                            if (group > 550)                                               
                                                { return str; }

                                        }
                                        #endregion
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion

            #region Room Count 5
            if (totalroom == 5)
            {
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children2ages = string.Empty;
                    List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                    int totc2 = child2age.Count();
                    for (int i = 0; i < child2age.Count(); i++)
                    {
                        if (i == totc2 - 1)
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value);
                        }
                        else
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                        }
                    }
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children3ages = string.Empty;
                    List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                    int totc3 = child3age.Count();
                    for (int i = 0; i < child3age.Count(); i++)
                    {
                        if (i == totc3 - 1)
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value);
                        }
                        else
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                        }
                    }
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children4ages = string.Empty;
                    List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                    int totc4 = child4age.Count();
                    for (int i = 0; i < child4age.Count(); i++)
                    {
                        if (i == totc4 - 1)
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value);
                        }
                        else
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                        }
                    }
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild5 == "0")
                {
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild5 == "1")
                {
                    List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children5ages = string.Empty;
                    List<XElement> child5age = room5child[4].Descendants("ChildAge").ToList();
                    int totc5 = child5age.Count();
                    for (int i = 0; i < child5age.Count(); i++)
                    {
                        if (i == totc5 - 1)
                        {
                            children5ages = children5ages + Convert.ToString(child5age[i].Value);
                        }
                        else
                        {
                            children5ages = children5ages + Convert.ToString(child5age[i].Value) + ",";
                        }
                    }
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false).ToList();
                }
                #endregion
                #endregion
                int group = 0;               
                #region Room 5
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    // add room 1, 2, 3, 4, 5
                                    string bb1 = roomList1[m].Attribute("boardCode").Value;
                                    string bb2 = roomList2[n].Attribute("boardCode").Value;
                                    string bb3 = roomList3[o].Attribute("boardCode").Value;
                                    string bb4 = roomList4[p].Attribute("boardCode").Value;
                                    string bb5 = roomList5[q].Attribute("boardCode").Value;
                                    if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4
                                         && bb1 == bb5 && bb2 == bb5 && bb3 == bb5 && bb4 == bb5)
                                    {
                                        
                                        int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                        if (totalroom <= totalallots)
                                        {
                                            #region check allotments
                                            String condition = "";
                                            List<string> ratekeylist = new List<string>();
                                            ratekeylist.Add(roomList1[m].Attribute("rateKey").Value);
                                            ratekeylist.Add(roomList2[n].Attribute("rateKey").Value);
                                            ratekeylist.Add(roomList3[o].Attribute("rateKey").Value);
                                            ratekeylist.Add(roomList4[p].Attribute("rateKey").Value);
                                            ratekeylist.Add(roomList5[q].Attribute("rateKey").Value);
                                            var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new { Key = ax.Key, Count = ax.Count() });
                                            int k = 0;
                                            foreach (var item in grouped)
                                            {
                                                var rtkey = item.Key;
                                                var count = item.Count;
                                                int totalt = roomlist.Descendants("rate").Where(x => x.Attribute("rateKey").Value == rtkey).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                                if (k == grouped.Count() - 1)
                                                {
                                                    condition = condition + totalt + " >= " + count;
                                                }
                                                else
                                                {
                                                    condition = condition + totalt + " >= " + count + " and ";
                                                }
                                                k++;
                                            }
                                            System.Data.DataTable table = new System.Data.DataTable();
                                            table.Columns.Add("", typeof(Boolean));
                                            table.Columns[0].Expression = condition;

                                            System.Data.DataRow ckr = table.NewRow();
                                            table.Rows.Add(ckr);
                                            bool _condition = (Boolean)ckr[0];
                                            if (_condition)
                                            {
                                                #region room's group
                                                List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                                                List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                                                List<XElement> pricebrkups3 = roomList3[o].Descendants("dailyRate").ToList();
                                                List<XElement> pricebrkups4 = roomList4[p].Descendants("dailyRate").ToList();
                                                List<XElement> pricebrkups5 = roomList5[q].Descendants("dailyRate").ToList();

                                                List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();
                                                List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();
                                                List<XElement> promotions3 = roomList3[o].Descendants("promotion").ToList();
                                                List<XElement> promotions4 = roomList4[p].Descendants("promotion").ToList();
                                                List<XElement> promotions5 = roomList5[q].Descendants("promotion").ToList();

                                                List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();
                                                List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();
                                                List<XElement> offer3 = roomList3[o].Descendants("offer").ToList();
                                                List<XElement> offer4 = roomList4[p].Descendants("offer").ToList();
                                                List<XElement> offer5 = roomList5[q].Descendants("offer").ToList();

                                                group++;
                                                decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value);

                                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                                                 new XAttribute("SuppliersID", "4"),
                                                 new XAttribute("RoomSeq", "1"),
                                                 new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                                                 new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", "true"),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                                                    //new XElement("Promotions", Convert.ToString(promo1))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements", ""
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups1)),
                                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("code").Value)),
                                                 new XAttribute("SuppliersID", "4"),
                                                 new XAttribute("RoomSeq", "2"),
                                                 new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("rateKey").Value)),
                                                 new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("name").Value)),
                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("net").Value)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", "true"),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions2), GetHotelpromotionsHotelBeds(offer2)),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements", ""
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups2)),
                                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("code").Value)),
                                                 new XAttribute("SuppliersID", "4"),
                                                 new XAttribute("RoomSeq", "3"),
                                                 new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("rateKey").Value)),
                                                 new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("name").Value)),
                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("net").Value)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", "true"),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions3), GetHotelpromotionsHotelBeds(offer3)),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements", ""
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups3)),
                                                     new XElement("AdultNum", Convert.ToString(roomList3[o].Attribute("adults").Value)),
                                                     new XElement("ChildNum", Convert.ToString(roomList3[o].Attribute("children").Value))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomList4[p].Parent.Parent.Attribute("code").Value)),
                                                 new XAttribute("SuppliersID", "4"),
                                                 new XAttribute("RoomSeq", "4"),
                                                 new XAttribute("SessionID", Convert.ToString(roomList4[p].Attribute("rateKey").Value)),
                                                 new XAttribute("RoomType", Convert.ToString(roomList4[p].Parent.Parent.Attribute("name").Value)),
                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList4[p].Attribute("boardName").Value)),
                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].Attribute("net").Value)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", "true"),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions4), GetHotelpromotionsHotelBeds(offer4)),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements", ""
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups4)),
                                                     new XElement("AdultNum", Convert.ToString(roomList4[p].Attribute("adults").Value)),
                                                     new XElement("ChildNum", Convert.ToString(roomList4[p].Attribute("children").Value))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomList5[q].Parent.Parent.Attribute("code").Value)),
                                                 new XAttribute("SuppliersID", "4"),
                                                 new XAttribute("RoomSeq", "5"),
                                                 new XAttribute("SessionID", Convert.ToString(roomList5[q].Attribute("rateKey").Value)),
                                                 new XAttribute("RoomType", Convert.ToString(roomList5[q].Parent.Parent.Attribute("name").Value)),
                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList5[q].Attribute("boardName").Value)),
                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList5[q].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].Attribute("net").Value)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", "true"),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions5), GetHotelpromotionsHotelBeds(offer5)),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements", ""
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups5)),
                                                     new XElement("AdultNum", Convert.ToString(roomList5[q].Attribute("adults").Value)),
                                                     new XElement("ChildNum", Convert.ToString(roomList5[q].Attribute("children").Value))
                                                 )));
                                                #endregion
                                                if (group > 350)
                                                { return str; }
                                            }
                                            #endregion
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion

            #region Room Count 6
            if (totalroom == 6)
            {
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children2ages = string.Empty;
                    List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                    int totc2 = child2age.Count();
                    for (int i = 0; i < child2age.Count(); i++)
                    {
                        if (i == totc2 - 1)
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value);
                        }
                        else
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                        }
                    }
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children3ages = string.Empty;
                    List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                    int totc3 = child3age.Count();
                    for (int i = 0; i < child3age.Count(); i++)
                    {
                        if (i == totc3 - 1)
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value);
                        }
                        else
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                        }
                    }
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();                   
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children4ages = string.Empty;
                    List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                    int totc4 = child4age.Count();
                    for (int i = 0; i < child4age.Count(); i++)
                    {
                        if (i == totc4 - 1)
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value);
                        }
                        else
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                        }
                    }
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild5 == "0")
                {
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild5 == "1")
                {
                    List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();                    
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children5ages = string.Empty;
                    List<XElement> child5age = room5child[4].Descendants("ChildAge").ToList();
                    int totc5 = child5age.Count();
                    for (int i = 0; i < child5age.Count(); i++)
                    {
                        if (i == totc5 - 1)
                        {
                            children5ages = children5ages + Convert.ToString(child5age[i].Value);
                        }
                        else
                        {
                            children5ages = children5ages + Convert.ToString(child5age[i].Value) + ",";
                        }
                    }
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild6 == "0")
                {
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild6 == "1")
                {
                    List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();                   
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children6ages = string.Empty;
                    List<XElement> child6age = room6child[5].Descendants("ChildAge").ToList();
                    int totc6 = child6age.Count();
                    for (int i = 0; i < child6age.Count(); i++)
                    {
                        if (i == totc6 - 1)
                        {
                            children6ages = children6ages + Convert.ToString(child6age[i].Value);
                        }
                        else
                        {
                            children6ages = children6ages + Convert.ToString(child6age[i].Value) + ",";
                        }
                    }
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false).ToList();
                }
                #endregion
                #endregion
                int group = 0;
                #region Room 6
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        // add room 1, 2, 3, 4, 5, 6
                                        string bb1 = roomList1[m].Attribute("boardCode").Value;
                                        string bb2 = roomList2[n].Attribute("boardCode").Value;
                                        string bb3 = roomList3[o].Attribute("boardCode").Value;
                                        string bb4 = roomList4[p].Attribute("boardCode").Value;
                                        string bb5 = roomList5[q].Attribute("boardCode").Value;
                                        string bb6 = roomList6[r].Attribute("boardCode").Value;
                                        if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4
                                            && bb1 == bb5 && bb2 == bb5 && bb3 == bb5 && bb4 == bb5
                                            && bb1 == bb6 && bb2 == bb6 && bb3 == bb6 && bb4 == bb6 && bb5 == bb6)
                                        {
                                            
                                            int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                            if (totalroom <= totalallots)
                                            {
                                                #region check allotments
                                                String condition = "";
                                                List<string> ratekeylist = new List<string>();
                                                ratekeylist.Add(roomList1[m].Attribute("rateKey").Value);
                                                ratekeylist.Add(roomList2[n].Attribute("rateKey").Value);
                                                ratekeylist.Add(roomList3[o].Attribute("rateKey").Value);
                                                ratekeylist.Add(roomList4[p].Attribute("rateKey").Value);
                                                ratekeylist.Add(roomList5[q].Attribute("rateKey").Value);
                                                ratekeylist.Add(roomList6[r].Attribute("rateKey").Value);
                                                var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new { Key = ax.Key, Count = ax.Count() });
                                                int k = 0;
                                                foreach (var item in grouped)
                                                {
                                                    var rtkey = item.Key;
                                                    var count = item.Count;
                                                    int totalt = roomlist.Descendants("rate").Where(x => x.Attribute("rateKey").Value == rtkey).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                                    if (k == grouped.Count() - 1)
                                                    {
                                                        condition = condition + totalt + " >= " + count;
                                                    }
                                                    else
                                                    {
                                                        condition = condition + totalt + " >= " + count + " and ";
                                                    }
                                                    k++;
                                                }
                                                System.Data.DataTable table = new System.Data.DataTable();
                                                table.Columns.Add("", typeof(Boolean));
                                                table.Columns[0].Expression = condition;

                                                System.Data.DataRow ckr = table.NewRow();
                                                table.Rows.Add(ckr);
                                                bool _condition = (Boolean)ckr[0];
                                                if (_condition)
                                                {
                                                    #region room's group
                                                    List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                                                    List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                                                    List<XElement> pricebrkups3 = roomList3[o].Descendants("dailyRate").ToList();
                                                    List<XElement> pricebrkups4 = roomList4[p].Descendants("dailyRate").ToList();
                                                    List<XElement> pricebrkups5 = roomList5[q].Descendants("dailyRate").ToList();
                                                    List<XElement> pricebrkups6 = roomList6[r].Descendants("dailyRate").ToList();

                                                    List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();
                                                    List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();
                                                    List<XElement> promotions3 = roomList3[o].Descendants("promotion").ToList();
                                                    List<XElement> promotions4 = roomList4[p].Descendants("promotion").ToList();
                                                    List<XElement> promotions5 = roomList5[q].Descendants("promotion").ToList();
                                                    List<XElement> promotions6 = roomList6[r].Descendants("promotion").ToList();

                                                    List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();
                                                    List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();
                                                    List<XElement> offer3 = roomList3[o].Descendants("offer").ToList();
                                                    List<XElement> offer4 = roomList4[p].Descendants("offer").ToList();
                                                    List<XElement> offer5 = roomList5[q].Descendants("offer").ToList();
                                                    List<XElement> offer6 = roomList6[r].Descendants("offer").ToList();

                                                    group++;
                                                    decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value);

                                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                                                     new XAttribute("SuppliersID", "4"),
                                                     new XAttribute("RoomSeq", "1"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", "true"),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                                                        //new XElement("Promotions", Convert.ToString(promo1))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", ""
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups1)),
                                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("code").Value)),
                                                     new XAttribute("SuppliersID", "4"),
                                                     new XAttribute("RoomSeq", "2"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("rateKey").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("name").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("net").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", "true"),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions2), GetHotelpromotionsHotelBeds(offer2)),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", ""
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups2)),
                                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("code").Value)),
                                                     new XAttribute("SuppliersID", "4"),
                                                     new XAttribute("RoomSeq", "3"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("rateKey").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("name").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("net").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", "true"),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions3), GetHotelpromotionsHotelBeds(offer3)),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", ""
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups3)),
                                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Attribute("adults").Value)),
                                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Attribute("children").Value))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList4[p].Parent.Parent.Attribute("code").Value)),
                                                     new XAttribute("SuppliersID", "4"),
                                                     new XAttribute("RoomSeq", "4"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList4[p].Attribute("rateKey").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList4[p].Parent.Parent.Attribute("name").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList4[p].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].Attribute("net").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", "true"),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions4), GetHotelpromotionsHotelBeds(offer4)),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", ""
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups4)),
                                                         new XElement("AdultNum", Convert.ToString(roomList4[p].Attribute("adults").Value)),
                                                         new XElement("ChildNum", Convert.ToString(roomList4[p].Attribute("children").Value))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList5[q].Parent.Parent.Attribute("code").Value)),
                                                     new XAttribute("SuppliersID", "4"),
                                                     new XAttribute("RoomSeq", "5"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList5[q].Attribute("rateKey").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList5[q].Parent.Parent.Attribute("name").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList5[q].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList5[q].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].Attribute("net").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", "true"),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions5), GetHotelpromotionsHotelBeds(offer5)),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", ""
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups5)),
                                                         new XElement("AdultNum", Convert.ToString(roomList5[q].Attribute("adults").Value)),
                                                         new XElement("ChildNum", Convert.ToString(roomList5[q].Attribute("children").Value))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList6[r].Parent.Parent.Attribute("code").Value)),
                                                     new XAttribute("SuppliersID", "4"),
                                                     new XAttribute("RoomSeq", "6"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList6[r].Attribute("rateKey").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList6[r].Parent.Parent.Attribute("name").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList6[r].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList6[r].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].Attribute("net").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", "true"),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions6), GetHotelpromotionsHotelBeds(offer6)),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", ""
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups6)),
                                                         new XElement("AdultNum", Convert.ToString(roomList6[r].Attribute("adults").Value)),
                                                         new XElement("ChildNum", Convert.ToString(roomList6[r].Attribute("children").Value))
                                                     )));
                                                    #endregion
                                                    if (group > 350)
                                                    { return str; }
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion

            #region Room Count 7
            if (totalroom == 7)
            {
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children2ages = string.Empty;
                    List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                    int totc2 = child2age.Count();
                    for (int i = 0; i < child2age.Count(); i++)
                    {
                        if (i == totc2 - 1)
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value);
                        }
                        else
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                        }
                    }
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children3ages = string.Empty;
                    List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                    int totc3 = child3age.Count();
                    for (int i = 0; i < child3age.Count(); i++)
                    {
                        if (i == totc3 - 1)
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value);
                        }
                        else
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                        }
                    }
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children4ages = string.Empty;
                    List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                    int totc4 = child4age.Count();
                    for (int i = 0; i < child4age.Count(); i++)
                    {
                        if (i == totc4 - 1)
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value);
                        }
                        else
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                        }
                    }
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild5 == "0")
                {
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild5 == "1")
                {
                    List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children5ages = string.Empty;
                    List<XElement> child5age = room5child[4].Descendants("ChildAge").ToList();
                    int totc5 = child5age.Count();
                    for (int i = 0; i < child5age.Count(); i++)
                    {
                        if (i == totc5 - 1)
                        {
                            children5ages = children5ages + Convert.ToString(child5age[i].Value);
                        }
                        else
                        {
                            children5ages = children5ages + Convert.ToString(child5age[i].Value) + ",";
                        }
                    }
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild6 == "0")
                {
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild6 == "1")
                {
                    List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children6ages = string.Empty;
                    List<XElement> child6age = room6child[5].Descendants("ChildAge").ToList();
                    int totc6 = child6age.Count();
                    for (int i = 0; i < child6age.Count(); i++)
                    {
                        if (i == totc6 - 1)
                        {
                            children6ages = children6ages + Convert.ToString(child6age[i].Value);
                        }
                        else
                        {
                            children6ages = children6ages + Convert.ToString(child6age[i].Value) + ",";
                        }
                    }
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 7)
                List<XElement> room7child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild7 = room7child[6].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild7 == "0")
                {
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild7 == "1")
                {
                    List<XElement> childage = room7child[6].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children7ages = string.Empty;
                    List<XElement> child7age = room7child[6].Descendants("ChildAge").ToList();
                    int totc7 = child7age.Count();
                    for (int i = 0; i < child7age.Count(); i++)
                    {
                        if (i == totc7 - 1)
                        {
                            children7ages = children7ages + Convert.ToString(child7age[i].Value);
                        }
                        else
                        {
                            children7ages = children7ages + Convert.ToString(child7age[i].Value) + ",";
                        }
                    }
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children7ages) : false).ToList();
                }
                #endregion
                #endregion
                int group = 0;
                #region Room 7
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            // add room 1, 2, 3, 4, 5, 6, 7
                                            string bb1 = roomList1[m].Attribute("boardCode").Value;
                                            string bb2 = roomList2[n].Attribute("boardCode").Value;
                                            string bb3 = roomList3[o].Attribute("boardCode").Value;
                                            string bb4 = roomList4[p].Attribute("boardCode").Value;
                                            string bb5 = roomList5[q].Attribute("boardCode").Value;
                                            string bb6 = roomList6[r].Attribute("boardCode").Value;
                                            string bb7 = roomList7[s].Attribute("boardCode").Value;
                                            if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4
                                                && bb1 == bb5 && bb2 == bb5 && bb3 == bb5 && bb4 == bb5
                                                && bb1 == bb6 && bb2 == bb6 && bb3 == bb6 && bb4 == bb6 && bb5 == bb6
                                                && bb1 == bb7 && bb2 == bb7 && bb3 == bb7 && bb4 == bb7 && bb5 == bb7 && bb6 == bb7)
                                            {
                                                
                                                int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                                if (totalroom <= totalallots)
                                                {
                                                    #region check allotments
                                                    String condition = "";
                                                    List<string> ratekeylist = new List<string>();
                                                    ratekeylist.Add(roomList1[m].Attribute("rateKey").Value);
                                                    ratekeylist.Add(roomList2[n].Attribute("rateKey").Value);
                                                    ratekeylist.Add(roomList3[o].Attribute("rateKey").Value);
                                                    ratekeylist.Add(roomList4[p].Attribute("rateKey").Value);
                                                    ratekeylist.Add(roomList5[q].Attribute("rateKey").Value);
                                                    ratekeylist.Add(roomList6[r].Attribute("rateKey").Value);
                                                    ratekeylist.Add(roomList7[s].Attribute("rateKey").Value);
                                                    var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new { Key = ax.Key, Count = ax.Count() });
                                                    int k = 0;
                                                    foreach (var item in grouped)
                                                    {
                                                        var rtkey = item.Key;
                                                        var count = item.Count;
                                                        int totalt = roomlist.Descendants("rate").Where(x => x.Attribute("rateKey").Value == rtkey).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                                        if (k == grouped.Count() - 1)
                                                        {
                                                            condition = condition + totalt + " >= " + count;
                                                        }
                                                        else
                                                        {
                                                            condition = condition + totalt + " >= " + count + " and ";
                                                        }
                                                        k++;
                                                    }
                                                    System.Data.DataTable table = new System.Data.DataTable();
                                                    table.Columns.Add("", typeof(Boolean));
                                                    table.Columns[0].Expression = condition;

                                                    System.Data.DataRow ckr = table.NewRow();
                                                    table.Rows.Add(ckr);
                                                    bool _condition = (Boolean)ckr[0];
                                                    if (_condition)
                                                    {
                                                        #region room's group
                                                        List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                                                        List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                                                        List<XElement> pricebrkups3 = roomList3[o].Descendants("dailyRate").ToList();
                                                        List<XElement> pricebrkups4 = roomList4[p].Descendants("dailyRate").ToList();
                                                        List<XElement> pricebrkups5 = roomList5[q].Descendants("dailyRate").ToList();
                                                        List<XElement> pricebrkups6 = roomList6[r].Descendants("dailyRate").ToList();
                                                        List<XElement> pricebrkups7 = roomList7[s].Descendants("dailyRate").ToList();

                                                        List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();
                                                        List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();
                                                        List<XElement> promotions3 = roomList3[o].Descendants("promotion").ToList();
                                                        List<XElement> promotions4 = roomList4[p].Descendants("promotion").ToList();
                                                        List<XElement> promotions5 = roomList5[q].Descendants("promotion").ToList();
                                                        List<XElement> promotions6 = roomList6[r].Descendants("promotion").ToList();
                                                        List<XElement> promotions7 = roomList7[s].Descendants("promotion").ToList();

                                                        List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();
                                                        List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();
                                                        List<XElement> offer3 = roomList3[o].Descendants("offer").ToList();
                                                        List<XElement> offer4 = roomList4[p].Descendants("offer").ToList();
                                                        List<XElement> offer5 = roomList5[q].Descendants("offer").ToList();
                                                        List<XElement> offer6 = roomList6[r].Descendants("offer").ToList();
                                                        List<XElement> offer7 = roomList7[s].Descendants("offer").ToList();

                                                        group++;
                                                        decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value) + Convert.ToDecimal(roomList7[s].Attribute("net").Value);

                                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                                                         new XAttribute("SuppliersID", "4"),
                                                         new XAttribute("RoomSeq", "1"),
                                                         new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                                                         new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                                         new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", "true"),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements", ""
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups1)),
                                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("code").Value)),
                                                         new XAttribute("SuppliersID", "4"),
                                                         new XAttribute("RoomSeq", "2"),
                                                         new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("rateKey").Value)),
                                                         new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("name").Value)),
                                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                                         new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("net").Value)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", "true"),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions2), GetHotelpromotionsHotelBeds(offer2)),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements", ""
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups2)),
                                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("code").Value)),
                                                         new XAttribute("SuppliersID", "4"),
                                                         new XAttribute("RoomSeq", "3"),
                                                         new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("rateKey").Value)),
                                                         new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("name").Value)),
                                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                                         new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("net").Value)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", "true"),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions3), GetHotelpromotionsHotelBeds(offer3)),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements", ""
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups3)),
                                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Attribute("adults").Value)),
                                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Attribute("children").Value))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomList4[p].Parent.Parent.Attribute("code").Value)),
                                                         new XAttribute("SuppliersID", "4"),
                                                         new XAttribute("RoomSeq", "4"),
                                                         new XAttribute("SessionID", Convert.ToString(roomList4[p].Attribute("rateKey").Value)),
                                                         new XAttribute("RoomType", Convert.ToString(roomList4[p].Parent.Parent.Attribute("name").Value)),
                                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList4[p].Attribute("boardName").Value)),
                                                         new XAttribute("MealPlanCode", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].Attribute("net").Value)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", "true"),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions4), GetHotelpromotionsHotelBeds(offer4)),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements", ""
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups4)),
                                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Attribute("adults").Value)),
                                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Attribute("children").Value))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomList5[q].Parent.Parent.Attribute("code").Value)),
                                                         new XAttribute("SuppliersID", "4"),
                                                         new XAttribute("RoomSeq", "5"),
                                                         new XAttribute("SessionID", Convert.ToString(roomList5[q].Attribute("rateKey").Value)),
                                                         new XAttribute("RoomType", Convert.ToString(roomList5[q].Parent.Parent.Attribute("name").Value)),
                                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList5[q].Attribute("boardName").Value)),
                                                         new XAttribute("MealPlanCode", Convert.ToString(roomList5[q].Attribute("boardCode").Value)),
                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].Attribute("net").Value)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", "true"),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions5), GetHotelpromotionsHotelBeds(offer5)),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements", ""
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups5)),
                                                             new XElement("AdultNum", Convert.ToString(roomList5[q].Attribute("adults").Value)),
                                                             new XElement("ChildNum", Convert.ToString(roomList5[q].Attribute("children").Value))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomList6[r].Parent.Parent.Attribute("code").Value)),
                                                         new XAttribute("SuppliersID", "4"),
                                                         new XAttribute("RoomSeq", "6"),
                                                         new XAttribute("SessionID", Convert.ToString(roomList6[r].Attribute("rateKey").Value)),
                                                         new XAttribute("RoomType", Convert.ToString(roomList6[r].Parent.Parent.Attribute("name").Value)),
                                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList6[r].Attribute("boardName").Value)),
                                                         new XAttribute("MealPlanCode", Convert.ToString(roomList6[r].Attribute("boardCode").Value)),
                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].Attribute("net").Value)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", "true"),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions6), GetHotelpromotionsHotelBeds(offer6)),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements", ""
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups6)),
                                                             new XElement("AdultNum", Convert.ToString(roomList6[r].Attribute("adults").Value)),
                                                             new XElement("ChildNum", Convert.ToString(roomList6[r].Attribute("children").Value))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomList7[s].Parent.Parent.Attribute("code").Value)),
                                                         new XAttribute("SuppliersID", "4"),
                                                         new XAttribute("RoomSeq", "7"),
                                                         new XAttribute("SessionID", Convert.ToString(roomList7[s].Attribute("rateKey").Value)),
                                                         new XAttribute("RoomType", Convert.ToString(roomList7[s].Parent.Parent.Attribute("name").Value)),
                                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList7[s].Attribute("boardName").Value)),
                                                         new XAttribute("MealPlanCode", Convert.ToString(roomList7[s].Attribute("boardCode").Value)),
                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].Attribute("net").Value)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", "true"),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions7), GetHotelpromotionsHotelBeds(offer7)),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements", ""
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups7)),
                                                             new XElement("AdultNum", Convert.ToString(roomList7[s].Attribute("adults").Value)),
                                                             new XElement("ChildNum", Convert.ToString(roomList7[s].Attribute("children").Value))
                                                         )));
                                                        #endregion
                                                        if (group > 350)
                                                        { return str; }
                                                    }
                                                    #endregion
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion

            #region Room Count 8
            if (totalroom == 8)
            {
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children2ages = string.Empty;
                    List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                    int totc2 = child2age.Count();
                    for (int i = 0; i < child2age.Count(); i++)
                    {
                        if (i == totc2 - 1)
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value);
                        }
                        else
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                        }
                    }
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children3ages = string.Empty;
                    List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                    int totc3 = child3age.Count();
                    for (int i = 0; i < child3age.Count(); i++)
                    {
                        if (i == totc3 - 1)
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value);
                        }
                        else
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                        }
                    }
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children4ages = string.Empty;
                    List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                    int totc4 = child4age.Count();
                    for (int i = 0; i < child4age.Count(); i++)
                    {
                        if (i == totc4 - 1)
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value);
                        }
                        else
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                        }
                    }
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild5 == "0")
                {
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild5 == "1")
                {
                    List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children5ages = string.Empty;
                    List<XElement> child5age = room5child[4].Descendants("ChildAge").ToList();
                    int totc5 = child5age.Count();
                    for (int i = 0; i < child5age.Count(); i++)
                    {
                        if (i == totc5 - 1)
                        {
                            children5ages = children5ages + Convert.ToString(child5age[i].Value);
                        }
                        else
                        {
                            children5ages = children5ages + Convert.ToString(child5age[i].Value) + ",";
                        }
                    }
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild6 == "0")
                {
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild6 == "1")
                {
                    List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children6ages = string.Empty;
                    List<XElement> child6age = room6child[5].Descendants("ChildAge").ToList();
                    int totc6 = child6age.Count();
                    for (int i = 0; i < child6age.Count(); i++)
                    {
                        if (i == totc6 - 1)
                        {
                            children6ages = children6ages + Convert.ToString(child6age[i].Value);
                        }
                        else
                        {
                            children6ages = children6ages + Convert.ToString(child6age[i].Value) + ",";
                        }
                    }
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 7)
                List<XElement> room7child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild7 = room7child[6].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild7 == "0")
                {
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild7 == "1")
                {
                    List<XElement> childage = room7child[6].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children7ages = string.Empty;
                    List<XElement> child7age = room7child[6].Descendants("ChildAge").ToList();
                    int totc7 = child7age.Count();
                    for (int i = 0; i < child7age.Count(); i++)
                    {
                        if (i == totc7 - 1)
                        {
                            children7ages = children7ages + Convert.ToString(child7age[i].Value);
                        }
                        else
                        {
                            children7ages = children7ages + Convert.ToString(child7age[i].Value) + ",";
                        }
                    }
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children7ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 8)
                List<XElement> room8child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild8 = room8child[7].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild8 == "0")
                {
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild8 == "1")
                {
                    List<XElement> childage = room8child[7].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children8ages = string.Empty;
                    List<XElement> child8age = room8child[7].Descendants("ChildAge").ToList();
                    int totc8 = child8age.Count();
                    for (int i = 0; i < child8age.Count(); i++)
                    {
                        if (i == totc8 - 1)
                        {
                            children8ages = children8ages + Convert.ToString(child8age[i].Value);
                        }
                        else
                        {
                            children8ages = children8ages + Convert.ToString(child8age[i].Value) + ",";
                        }
                    }
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children8ages) : false).ToList();
                }
                #endregion
                #endregion
                int group = 0;
                #region Room 8
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            for (int t = 0; t < roomList8.Count(); t++)
                                            {
                                                // add room 1, 2, 3, 4, 5, 6, 7, 8
                                                string bb1 = roomList1[m].Attribute("boardCode").Value;
                                                string bb2 = roomList2[n].Attribute("boardCode").Value;
                                                string bb3 = roomList3[o].Attribute("boardCode").Value;
                                                string bb4 = roomList4[p].Attribute("boardCode").Value;
                                                string bb5 = roomList5[q].Attribute("boardCode").Value;
                                                string bb6 = roomList6[r].Attribute("boardCode").Value;
                                                string bb7 = roomList7[s].Attribute("boardCode").Value;
                                                string bb8 = roomList8[t].Attribute("boardCode").Value;
                                                if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4
                                                    && bb1 == bb5 && bb2 == bb5 && bb3 == bb5 && bb4 == bb5
                                                    && bb1 == bb6 && bb2 == bb6 && bb3 == bb6 && bb4 == bb6 && bb5 == bb6
                                                    && bb1 == bb7 && bb2 == bb7 && bb3 == bb7 && bb4 == bb7 && bb5 == bb7 && bb6 == bb7
                                                    && bb1 == bb8 && bb2 == bb8 && bb3 == bb8 && bb4 == bb8 && bb5 == bb8 && bb6 == bb8 && bb7 == bb8)
                                                {
                                                    
                                                    int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                                    if (totalroom <= totalallots)
                                                    {
                                                        #region check allotments
                                                        String condition = "";
                                                        List<string> ratekeylist = new List<string>();
                                                        ratekeylist.Add(roomList1[m].Attribute("rateKey").Value);
                                                        ratekeylist.Add(roomList2[n].Attribute("rateKey").Value);
                                                        ratekeylist.Add(roomList3[o].Attribute("rateKey").Value);
                                                        ratekeylist.Add(roomList4[p].Attribute("rateKey").Value);
                                                        ratekeylist.Add(roomList5[q].Attribute("rateKey").Value);
                                                        ratekeylist.Add(roomList6[r].Attribute("rateKey").Value);
                                                        ratekeylist.Add(roomList7[s].Attribute("rateKey").Value);
                                                        ratekeylist.Add(roomList8[t].Attribute("rateKey").Value);
                                                        var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new { Key = ax.Key, Count = ax.Count() });
                                                        int k = 0;
                                                        foreach (var item in grouped)
                                                        {
                                                            var rtkey = item.Key;
                                                            var count = item.Count;
                                                            int totalt = roomlist.Descendants("rate").Where(x => x.Attribute("rateKey").Value == rtkey).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                                            if (k == grouped.Count() - 1)
                                                            {
                                                                condition = condition + totalt + " >= " + count;
                                                            }
                                                            else
                                                            {
                                                                condition = condition + totalt + " >= " + count + " and ";
                                                            }
                                                            k++;
                                                        }
                                                        System.Data.DataTable table = new System.Data.DataTable();
                                                        table.Columns.Add("", typeof(Boolean));
                                                        table.Columns[0].Expression = condition;

                                                        System.Data.DataRow ckr = table.NewRow();
                                                        table.Rows.Add(ckr);
                                                        bool _condition = (Boolean)ckr[0];
                                                        if (_condition)
                                                        {
                                                            #region room's group
                                                            List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                                                            List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                                                            List<XElement> pricebrkups3 = roomList3[o].Descendants("dailyRate").ToList();
                                                            List<XElement> pricebrkups4 = roomList4[p].Descendants("dailyRate").ToList();
                                                            List<XElement> pricebrkups5 = roomList5[q].Descendants("dailyRate").ToList();
                                                            List<XElement> pricebrkups6 = roomList6[r].Descendants("dailyRate").ToList();
                                                            List<XElement> pricebrkups7 = roomList7[s].Descendants("dailyRate").ToList();
                                                            List<XElement> pricebrkups8 = roomList8[t].Descendants("dailyRate").ToList();

                                                            List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();
                                                            List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();
                                                            List<XElement> promotions3 = roomList3[o].Descendants("promotion").ToList();
                                                            List<XElement> promotions4 = roomList4[p].Descendants("promotion").ToList();
                                                            List<XElement> promotions5 = roomList5[q].Descendants("promotion").ToList();
                                                            List<XElement> promotions6 = roomList6[r].Descendants("promotion").ToList();
                                                            List<XElement> promotions7 = roomList7[s].Descendants("promotion").ToList();
                                                            List<XElement> promotions8 = roomList8[t].Descendants("promotion").ToList();

                                                            List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();
                                                            List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();
                                                            List<XElement> offer3 = roomList3[o].Descendants("offer").ToList();
                                                            List<XElement> offer4 = roomList4[p].Descendants("offer").ToList();
                                                            List<XElement> offer5 = roomList5[q].Descendants("offer").ToList();
                                                            List<XElement> offer6 = roomList6[r].Descendants("offer").ToList();
                                                            List<XElement> offer7 = roomList7[s].Descendants("offer").ToList();
                                                            List<XElement> offer8 = roomList8[t].Descendants("offer").ToList();

                                                            group++;
                                                            decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value) + Convert.ToDecimal(roomList7[s].Attribute("net").Value) + Convert.ToDecimal(roomList8[t].Attribute("net").Value);

                                                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                                                             new XAttribute("SuppliersID", "4"),
                                                             new XAttribute("RoomSeq", "1"),
                                                             new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                                                             new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                                             new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", "true"),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements", ""
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups1)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("code").Value)),
                                                             new XAttribute("SuppliersID", "4"),
                                                             new XAttribute("RoomSeq", "2"),
                                                             new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("rateKey").Value)),
                                                             new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("name").Value)),
                                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                                             new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("net").Value)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", "true"),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions2), GetHotelpromotionsHotelBeds(offer2)),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements", ""
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups2)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("code").Value)),
                                                             new XAttribute("SuppliersID", "4"),
                                                             new XAttribute("RoomSeq", "3"),
                                                             new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("rateKey").Value)),
                                                             new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("name").Value)),
                                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                                             new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("net").Value)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", "true"),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions3), GetHotelpromotionsHotelBeds(offer3)),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements", ""
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups3)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Attribute("adults").Value)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Attribute("children").Value))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomList4[p].Parent.Parent.Attribute("code").Value)),
                                                             new XAttribute("SuppliersID", "4"),
                                                             new XAttribute("RoomSeq", "4"),
                                                             new XAttribute("SessionID", Convert.ToString(roomList4[p].Attribute("rateKey").Value)),
                                                             new XAttribute("RoomType", Convert.ToString(roomList4[p].Parent.Parent.Attribute("name").Value)),
                                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList4[p].Attribute("boardName").Value)),
                                                             new XAttribute("MealPlanCode", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].Attribute("net").Value)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", "true"),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions4), GetHotelpromotionsHotelBeds(offer4)),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements", ""
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups4)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Attribute("adults").Value)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Attribute("children").Value))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomList5[q].Parent.Parent.Attribute("code").Value)),
                                                             new XAttribute("SuppliersID", "4"),
                                                             new XAttribute("RoomSeq", "5"),
                                                             new XAttribute("SessionID", Convert.ToString(roomList5[q].Attribute("rateKey").Value)),
                                                             new XAttribute("RoomType", Convert.ToString(roomList5[q].Parent.Parent.Attribute("name").Value)),
                                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList5[q].Attribute("boardName").Value)),
                                                             new XAttribute("MealPlanCode", Convert.ToString(roomList5[q].Attribute("boardCode").Value)),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].Attribute("net").Value)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", "true"),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions5), GetHotelpromotionsHotelBeds(offer5)),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements", ""
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups5)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Attribute("adults").Value)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Attribute("children").Value))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomList6[r].Parent.Parent.Attribute("code").Value)),
                                                             new XAttribute("SuppliersID", "4"),
                                                             new XAttribute("RoomSeq", "6"),
                                                             new XAttribute("SessionID", Convert.ToString(roomList6[r].Attribute("rateKey").Value)),
                                                             new XAttribute("RoomType", Convert.ToString(roomList6[r].Parent.Parent.Attribute("name").Value)),
                                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList6[r].Attribute("boardName").Value)),
                                                             new XAttribute("MealPlanCode", Convert.ToString(roomList6[r].Attribute("boardCode").Value)),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].Attribute("net").Value)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", "true"),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions6), GetHotelpromotionsHotelBeds(offer6)),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements", ""
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups6)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList6[r].Attribute("adults").Value)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList6[r].Attribute("children").Value))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomList7[s].Parent.Parent.Attribute("code").Value)),
                                                             new XAttribute("SuppliersID", "4"),
                                                             new XAttribute("RoomSeq", "7"),
                                                             new XAttribute("SessionID", Convert.ToString(roomList7[s].Attribute("rateKey").Value)),
                                                             new XAttribute("RoomType", Convert.ToString(roomList7[s].Parent.Parent.Attribute("name").Value)),
                                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList7[s].Attribute("boardName").Value)),
                                                             new XAttribute("MealPlanCode", Convert.ToString(roomList7[s].Attribute("boardCode").Value)),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].Attribute("net").Value)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", "true"),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions7), GetHotelpromotionsHotelBeds(offer7)),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements", ""
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups7)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList7[s].Attribute("adults").Value)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList7[s].Attribute("children").Value))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomList8[t].Parent.Parent.Attribute("code").Value)),
                                                             new XAttribute("SuppliersID", "4"),
                                                             new XAttribute("RoomSeq", "8"),
                                                             new XAttribute("SessionID", Convert.ToString(roomList8[t].Attribute("rateKey").Value)),
                                                             new XAttribute("RoomType", Convert.ToString(roomList8[t].Parent.Parent.Attribute("name").Value)),
                                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList8[t].Attribute("boardName").Value)),
                                                             new XAttribute("MealPlanCode", Convert.ToString(roomList8[t].Attribute("boardCode").Value)),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList8[t].Attribute("net").Value)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", "true"),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions8), GetHotelpromotionsHotelBeds(offer8)),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements", ""
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups8)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList8[t].Attribute("adults").Value)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList8[t].Attribute("children").Value))
                                                             )));
                                                            #endregion
                                                            if (group > 300)
                                                            { return str; }
                                                        }
                                                        #endregion
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion

            #region Room Count 9
            if (totalroom == 9)
            {
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children2ages = string.Empty;
                    List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                    int totc2 = child2age.Count();
                    for (int i = 0; i < child2age.Count(); i++)
                    {
                        if (i == totc2 - 1)
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value);
                        }
                        else
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                        }
                    }
                    //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children3ages = string.Empty;
                    List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                    int totc3 = child3age.Count();
                    for (int i = 0; i < child3age.Count(); i++)
                    {
                        if (i == totc3 - 1)
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value);
                        }
                        else
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                        }
                    }
                    //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children4ages = string.Empty;
                    List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                    int totc4 = child4age.Count();
                    for (int i = 0; i < child4age.Count(); i++)
                    {
                        if (i == totc4 - 1)
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value);
                        }
                        else
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                        }
                    }
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild5 == "0")
                {
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild5 == "1")
                {
                    List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children5ages = string.Empty;
                    List<XElement> child5age = room5child[4].Descendants("ChildAge").ToList();
                    int totc5 = child5age.Count();
                    for (int i = 0; i < child5age.Count(); i++)
                    {
                        if (i == totc5 - 1)
                        {
                            children5ages = children5ages + Convert.ToString(child5age[i].Value);
                        }
                        else
                        {
                            children5ages = children5ages + Convert.ToString(child5age[i].Value) + ",";
                        }
                    }
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild6 == "0")
                {
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild6 == "1")
                {
                    List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children6ages = string.Empty;
                    List<XElement> child6age = room6child[5].Descendants("ChildAge").ToList();
                    int totc6 = child6age.Count();
                    for (int i = 0; i < child6age.Count(); i++)
                    {
                        if (i == totc6 - 1)
                        {
                            children6ages = children6ages + Convert.ToString(child6age[i].Value);
                        }
                        else
                        {
                            children6ages = children6ages + Convert.ToString(child6age[i].Value) + ",";
                        }
                    }
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 7)
                List<XElement> room7child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild7 = room7child[6].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild7 == "0")
                {
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild7 == "1")
                {
                    List<XElement> childage = room7child[6].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children7ages = string.Empty;
                    List<XElement> child7age = room7child[6].Descendants("ChildAge").ToList();
                    int totc7 = child7age.Count();
                    for (int i = 0; i < child7age.Count(); i++)
                    {
                        if (i == totc7 - 1)
                        {
                            children7ages = children7ages + Convert.ToString(child7age[i].Value);
                        }
                        else
                        {
                            children7ages = children7ages + Convert.ToString(child7age[i].Value) + ",";
                        }
                    }
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children7ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 8)
                List<XElement> room8child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild8 = room8child[7].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild8 == "0")
                {
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild8 == "1")
                {
                    List<XElement> childage = room8child[7].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children8ages = string.Empty;
                    List<XElement> child8age = room8child[7].Descendants("ChildAge").ToList();
                    int totc8 = child8age.Count();
                    for (int i = 0; i < child8age.Count(); i++)
                    {
                        if (i == totc8 - 1)
                        {
                            children8ages = children8ages + Convert.ToString(child8age[i].Value);
                        }
                        else
                        {
                            children8ages = children8ages + Convert.ToString(child8age[i].Value) + ",";
                        }
                    }
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children8ages) : false).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 9)
                List<XElement> room9child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild9 = room9child[8].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild9 == "0")
                {
                    roomList9 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room9child[8].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild9 == "1")
                {
                    List<XElement> childage = room9child[8].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList9 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room9child[8].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children9ages = string.Empty;
                    List<XElement> child9age = room9child[8].Descendants("ChildAge").ToList();
                    int totc9 = child9age.Count();
                    for (int i = 0; i < child9age.Count(); i++)
                    {
                        if (i == totc9 - 1)
                        {
                            children9ages = children9ages + Convert.ToString(child9age[i].Value);
                        }
                        else
                        {
                            children9ages = children9ages + Convert.ToString(child9age[i].Value) + ",";
                        }
                    }
                    roomList9 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room9child[8].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children9ages) : false).ToList();
                }
                #endregion
                #endregion
                int group = 0;
                #region Room 9
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            for (int t = 0; t < roomList8.Count(); t++)
                                            {
                                                for (int u = 0; u < roomList9.Count(); u++)
                                                {
                                                    // add room 1, 2, 3, 4, 5, 6, 7, 8, 9

                                                    string bb1 = roomList1[m].Attribute("boardCode").Value;
                                                    string bb2 = roomList2[n].Attribute("boardCode").Value;
                                                    string bb3 = roomList3[o].Attribute("boardCode").Value;
                                                    string bb4 = roomList4[p].Attribute("boardCode").Value;
                                                    string bb5 = roomList5[q].Attribute("boardCode").Value;
                                                    string bb6 = roomList6[r].Attribute("boardCode").Value;
                                                    string bb7 = roomList7[s].Attribute("boardCode").Value;
                                                    string bb8 = roomList8[t].Attribute("boardCode").Value;
                                                    string bb9 = roomList9[u].Attribute("boardCode").Value;
                                                    if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4
                                                        && bb1 == bb5 && bb2 == bb5 && bb3 == bb5 && bb4 == bb5
                                                        && bb1 == bb6 && bb2 == bb6 && bb3 == bb6 && bb4 == bb6 && bb5 == bb6
                                                        && bb1 == bb7 && bb2 == bb7 && bb3 == bb7 && bb4 == bb7 && bb5 == bb7 && bb6 == bb7
                                                        && bb1 == bb8 && bb2 == bb8 && bb3 == bb8 && bb4 == bb8 && bb5 == bb8 && bb6 == bb8 && bb7 == bb8
                                                        && bb1 == bb9 && bb2 == bb9 && bb3 == bb9 && bb4 == bb9 && bb5 == bb9 && bb6 == bb9 && bb7 == bb9 && bb8 == bb9)
                                                    {
                                                        
                                                        #region Room Allotments
                                                        int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                                        #endregion
                                                        if (totalroom <= totalallots)
                                                        {
                                                            #region check allotments
                                                            String condition = "";
                                                            List<string> ratekeylist = new List<string>();
                                                            ratekeylist.Add(roomList1[m].Attribute("rateKey").Value);
                                                            ratekeylist.Add(roomList2[n].Attribute("rateKey").Value);
                                                            ratekeylist.Add(roomList3[o].Attribute("rateKey").Value);
                                                            ratekeylist.Add(roomList4[p].Attribute("rateKey").Value);
                                                            ratekeylist.Add(roomList5[q].Attribute("rateKey").Value);
                                                            ratekeylist.Add(roomList6[r].Attribute("rateKey").Value);
                                                            ratekeylist.Add(roomList7[s].Attribute("rateKey").Value);
                                                            ratekeylist.Add(roomList8[t].Attribute("rateKey").Value);
                                                            ratekeylist.Add(roomList9[u].Attribute("rateKey").Value);
                                                            var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new { Key = ax.Key, Count = ax.Count() });
                                                            int k = 0;
                                                            foreach (var item in grouped)
                                                            {
                                                                var rtkey = item.Key;
                                                                var count = item.Count;
                                                                int totalt = roomlist.Descendants("rate").Where(x => x.Attribute("rateKey").Value == rtkey).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                                                if (k == grouped.Count() - 1)
                                                                {
                                                                    condition = condition + totalt + " >= " + count;
                                                                }
                                                                else
                                                                {
                                                                    condition = condition + totalt + " >= " + count + " and ";
                                                                }
                                                                k++;
                                                            }
                                                            System.Data.DataTable table = new System.Data.DataTable();
                                                            table.Columns.Add("", typeof(Boolean));
                                                            table.Columns[0].Expression = condition;

                                                            System.Data.DataRow ckr = table.NewRow();
                                                            table.Rows.Add(ckr);
                                                            bool _condition = (Boolean)ckr[0];
                                                            if (_condition)
                                                            {
                                                                #region room's group
                                                                List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                                                                List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                                                                List<XElement> pricebrkups3 = roomList3[o].Descendants("dailyRate").ToList();
                                                                List<XElement> pricebrkups4 = roomList4[p].Descendants("dailyRate").ToList();
                                                                List<XElement> pricebrkups5 = roomList5[q].Descendants("dailyRate").ToList();
                                                                List<XElement> pricebrkups6 = roomList6[r].Descendants("dailyRate").ToList();
                                                                List<XElement> pricebrkups7 = roomList7[s].Descendants("dailyRate").ToList();
                                                                List<XElement> pricebrkups8 = roomList8[t].Descendants("dailyRate").ToList();
                                                                List<XElement> pricebrkups9 = roomList9[u].Descendants("dailyRate").ToList();

                                                                List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();
                                                                List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();
                                                                List<XElement> promotions3 = roomList3[o].Descendants("promotion").ToList();
                                                                List<XElement> promotions4 = roomList4[p].Descendants("promotion").ToList();
                                                                List<XElement> promotions5 = roomList5[q].Descendants("promotion").ToList();
                                                                List<XElement> promotions6 = roomList6[r].Descendants("promotion").ToList();
                                                                List<XElement> promotions7 = roomList7[s].Descendants("promotion").ToList();
                                                                List<XElement> promotions8 = roomList8[t].Descendants("promotion").ToList();
                                                                List<XElement> promotions9 = roomList9[u].Descendants("promotion").ToList();

                                                                List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();
                                                                List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();
                                                                List<XElement> offer3 = roomList3[o].Descendants("offer").ToList();
                                                                List<XElement> offer4 = roomList4[p].Descendants("offer").ToList();
                                                                List<XElement> offer5 = roomList5[q].Descendants("offer").ToList();
                                                                List<XElement> offer6 = roomList6[r].Descendants("offer").ToList();
                                                                List<XElement> offer7 = roomList7[s].Descendants("offer").ToList();
                                                                List<XElement> offer8 = roomList8[t].Descendants("offer").ToList();
                                                                List<XElement> offer9 = roomList9[u].Descendants("offer").ToList();

                                                                group++;
                                                                decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value) + Convert.ToDecimal(roomList7[s].Attribute("net").Value) + Convert.ToDecimal(roomList8[t].Attribute("net").Value) + Convert.ToDecimal(roomList9[u].Attribute("net").Value);

                                                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc),

                                                                new XElement("Room",
                                                                 new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                                                                 new XAttribute("SuppliersID", "4"),
                                                                 new XAttribute("RoomSeq", "1"),
                                                                 new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                                                                 new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                                 new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                                                                 new XAttribute("CancellationDate", ""),
                                                                 new XAttribute("CancellationAmount", ""),
                                                                  new XAttribute("isAvailable", "true"),
                                                                 new XElement("RequestID", Convert.ToString("")),
                                                                 new XElement("Offers", ""),
                                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                                                                 new XElement("CancellationPolicy", ""),
                                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                 new XElement("Supplements", ""
                                                                     ),
                                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups1)),
                                                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                                                                 ),

                                                                new XElement("Room",
                                                                 new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("code").Value)),
                                                                 new XAttribute("SuppliersID", "4"),
                                                                 new XAttribute("RoomSeq", "2"),
                                                                 new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("rateKey").Value)),
                                                                 new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("name").Value)),
                                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                                 new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("net").Value)),
                                                                 new XAttribute("CancellationDate", ""),
                                                                 new XAttribute("CancellationAmount", ""),
                                                                  new XAttribute("isAvailable", "true"),
                                                                 new XElement("RequestID", Convert.ToString("")),
                                                                 new XElement("Offers", ""),
                                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions2), GetHotelpromotionsHotelBeds(offer2)),
                                                                 new XElement("CancellationPolicy", ""),
                                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                 new XElement("Supplements", ""
                                                                     ),
                                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups2)),
                                                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                                                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                                                                 ),

                                                                new XElement("Room",
                                                                 new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("code").Value)),
                                                                 new XAttribute("SuppliersID", "4"),
                                                                 new XAttribute("RoomSeq", "3"),
                                                                 new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("rateKey").Value)),
                                                                 new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("name").Value)),
                                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                                 new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("net").Value)),
                                                                 new XAttribute("CancellationDate", ""),
                                                                 new XAttribute("CancellationAmount", ""),
                                                                  new XAttribute("isAvailable", "true"),
                                                                 new XElement("RequestID", Convert.ToString("")),
                                                                 new XElement("Offers", ""),
                                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions3), GetHotelpromotionsHotelBeds(offer3)),
                                                                 new XElement("CancellationPolicy", ""),
                                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                 new XElement("Supplements", ""
                                                                     ),
                                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups3)),
                                                                     new XElement("AdultNum", Convert.ToString(roomList3[o].Attribute("adults").Value)),
                                                                     new XElement("ChildNum", Convert.ToString(roomList3[o].Attribute("children").Value))
                                                                 ),

                                                                new XElement("Room",
                                                                 new XAttribute("ID", Convert.ToString(roomList4[p].Parent.Parent.Attribute("code").Value)),
                                                                 new XAttribute("SuppliersID", "4"),
                                                                 new XAttribute("RoomSeq", "4"),
                                                                 new XAttribute("SessionID", Convert.ToString(roomList4[p].Attribute("rateKey").Value)),
                                                                 new XAttribute("RoomType", Convert.ToString(roomList4[p].Parent.Parent.Attribute("name").Value)),
                                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                                 new XAttribute("MealPlanName", Convert.ToString(roomList4[p].Attribute("boardName").Value)),
                                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].Attribute("net").Value)),
                                                                 new XAttribute("CancellationDate", ""),
                                                                 new XAttribute("CancellationAmount", ""),
                                                                  new XAttribute("isAvailable", "true"),
                                                                 new XElement("RequestID", Convert.ToString("")),
                                                                 new XElement("Offers", ""),
                                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions4), GetHotelpromotionsHotelBeds(offer4)),
                                                                 new XElement("CancellationPolicy", ""),
                                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                 new XElement("Supplements", ""
                                                                     ),
                                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups4)),
                                                                     new XElement("AdultNum", Convert.ToString(roomList4[p].Attribute("adults").Value)),
                                                                     new XElement("ChildNum", Convert.ToString(roomList4[p].Attribute("children").Value))
                                                                 ),

                                                                new XElement("Room",
                                                                 new XAttribute("ID", Convert.ToString(roomList5[q].Parent.Parent.Attribute("code").Value)),
                                                                 new XAttribute("SuppliersID", "4"),
                                                                 new XAttribute("RoomSeq", "5"),
                                                                 new XAttribute("SessionID", Convert.ToString(roomList5[q].Attribute("rateKey").Value)),
                                                                 new XAttribute("RoomType", Convert.ToString(roomList5[q].Parent.Parent.Attribute("name").Value)),
                                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                                 new XAttribute("MealPlanName", Convert.ToString(roomList5[q].Attribute("boardName").Value)),
                                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList5[q].Attribute("boardCode").Value)),
                                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].Attribute("net").Value)),
                                                                 new XAttribute("CancellationDate", ""),
                                                                 new XAttribute("CancellationAmount", ""),
                                                                  new XAttribute("isAvailable", "true"),
                                                                 new XElement("RequestID", Convert.ToString("")),
                                                                 new XElement("Offers", ""),
                                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions5), GetHotelpromotionsHotelBeds(offer5)),
                                                                 new XElement("CancellationPolicy", ""),
                                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                 new XElement("Supplements", ""
                                                                     ),
                                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups5)),
                                                                     new XElement("AdultNum", Convert.ToString(roomList5[q].Attribute("adults").Value)),
                                                                     new XElement("ChildNum", Convert.ToString(roomList5[q].Attribute("children").Value))
                                                                 ),

                                                                new XElement("Room",
                                                                 new XAttribute("ID", Convert.ToString(roomList6[r].Parent.Parent.Attribute("code").Value)),
                                                                 new XAttribute("SuppliersID", "4"),
                                                                 new XAttribute("RoomSeq", "6"),
                                                                 new XAttribute("SessionID", Convert.ToString(roomList6[r].Attribute("rateKey").Value)),
                                                                 new XAttribute("RoomType", Convert.ToString(roomList6[r].Parent.Parent.Attribute("name").Value)),
                                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                                 new XAttribute("MealPlanName", Convert.ToString(roomList6[r].Attribute("boardName").Value)),
                                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList6[r].Attribute("boardCode").Value)),
                                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].Attribute("net").Value)),
                                                                 new XAttribute("CancellationDate", ""),
                                                                 new XAttribute("CancellationAmount", ""),
                                                                  new XAttribute("isAvailable", "true"),
                                                                 new XElement("RequestID", Convert.ToString("")),
                                                                 new XElement("Offers", ""),
                                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions6), GetHotelpromotionsHotelBeds(offer6)),
                                                                 new XElement("CancellationPolicy", ""),
                                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                 new XElement("Supplements", ""
                                                                     ),
                                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups6)),
                                                                     new XElement("AdultNum", Convert.ToString(roomList6[r].Attribute("adults").Value)),
                                                                     new XElement("ChildNum", Convert.ToString(roomList6[r].Attribute("children").Value))
                                                                 ),

                                                                new XElement("Room",
                                                                 new XAttribute("ID", Convert.ToString(roomList7[s].Parent.Parent.Attribute("code").Value)),
                                                                 new XAttribute("SuppliersID", "4"),
                                                                 new XAttribute("RoomSeq", "7"),
                                                                 new XAttribute("SessionID", Convert.ToString(roomList7[s].Attribute("rateKey").Value)),
                                                                 new XAttribute("RoomType", Convert.ToString(roomList7[s].Parent.Parent.Attribute("name").Value)),
                                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                                 new XAttribute("MealPlanName", Convert.ToString(roomList7[s].Attribute("boardName").Value)),
                                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList7[s].Attribute("boardCode").Value)),
                                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].Attribute("net").Value)),
                                                                 new XAttribute("CancellationDate", ""),
                                                                 new XAttribute("CancellationAmount", ""),
                                                                  new XAttribute("isAvailable", "true"),
                                                                 new XElement("RequestID", Convert.ToString("")),
                                                                 new XElement("Offers", ""),
                                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions7), GetHotelpromotionsHotelBeds(offer7)),
                                                                 new XElement("CancellationPolicy", ""),
                                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                 new XElement("Supplements", ""
                                                                     ),
                                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups7)),
                                                                     new XElement("AdultNum", Convert.ToString(roomList7[s].Attribute("adults").Value)),
                                                                     new XElement("ChildNum", Convert.ToString(roomList7[s].Attribute("children").Value))
                                                                 ),

                                                                new XElement("Room",
                                                                 new XAttribute("ID", Convert.ToString(roomList8[t].Parent.Parent.Attribute("code").Value)),
                                                                 new XAttribute("SuppliersID", "4"),
                                                                 new XAttribute("RoomSeq", "8"),
                                                                 new XAttribute("SessionID", Convert.ToString(roomList8[t].Attribute("rateKey").Value)),
                                                                 new XAttribute("RoomType", Convert.ToString(roomList8[t].Parent.Parent.Attribute("name").Value)),
                                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                                 new XAttribute("MealPlanName", Convert.ToString(roomList8[t].Attribute("boardName").Value)),
                                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList8[t].Attribute("boardCode").Value)),
                                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList8[t].Attribute("net").Value)),
                                                                 new XAttribute("CancellationDate", ""),
                                                                 new XAttribute("CancellationAmount", ""),
                                                                  new XAttribute("isAvailable", "true"),
                                                                 new XElement("RequestID", Convert.ToString("")),
                                                                 new XElement("Offers", ""),
                                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions8), GetHotelpromotionsHotelBeds(offer8)),
                                                                 new XElement("CancellationPolicy", ""),
                                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                 new XElement("Supplements", ""
                                                                     ),
                                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups8)),
                                                                     new XElement("AdultNum", Convert.ToString(roomList8[t].Attribute("adults").Value)),
                                                                     new XElement("ChildNum", Convert.ToString(roomList8[t].Attribute("children").Value))
                                                                 ),

                                                                new XElement("Room",
                                                                 new XAttribute("ID", Convert.ToString(roomList9[u].Parent.Parent.Attribute("code").Value)),
                                                                 new XAttribute("SuppliersID", "4"),
                                                                 new XAttribute("RoomSeq", "9"),
                                                                 new XAttribute("SessionID", Convert.ToString(roomList9[u].Attribute("rateKey").Value)),
                                                                 new XAttribute("RoomType", Convert.ToString(roomList9[u].Parent.Parent.Attribute("name").Value)),
                                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                                 new XAttribute("MealPlanName", Convert.ToString(roomList9[u].Attribute("boardName").Value)),
                                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList9[u].Attribute("boardCode").Value)),
                                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList9[u].Attribute("net").Value)),
                                                                 new XAttribute("CancellationDate", ""),
                                                                 new XAttribute("CancellationAmount", ""),
                                                                  new XAttribute("isAvailable", "true"),
                                                                 new XElement("RequestID", Convert.ToString("")),
                                                                 new XElement("Offers", ""),
                                                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions9), GetHotelpromotionsHotelBeds(offer9)),
                                                                 new XElement("CancellationPolicy", ""),
                                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                 new XElement("Supplements", ""
                                                                     ),
                                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups9)),
                                                                     new XElement("AdultNum", Convert.ToString(roomList9[u].Attribute("adults").Value)),
                                                                     new XElement("ChildNum", Convert.ToString(roomList9[u].Attribute("children").Value))
                                                                 )));
                                                                #endregion

                                                                if (group > 300)
                                                                { return str; }
                                                            }
                                                            #endregion
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion

            return str;
        }
        #endregion
        #region HotelBeds's Room's Promotion
        private IEnumerable<XElement> GetHotelpromotionsHotelBeds(List<XElement> roompromotions)
        {

            Int32 length = roompromotions.Count();
            List<XElement> promotion = new List<XElement>();

            if (length == 0)
            {
                //promotion.Add(new XElement("Promotions", ""));
            }
            else
            {

                //Parallel.For(0, length, i =>
                for (int i = 0; i < length;i++ )
                {

                    promotion.Add(new XElement("Promotions", Convert.ToString(roompromotions[i].Attribute("name").Value)));

                }
            }
            return promotion;
        }
        #endregion
        #region HotelBeds Room's Price Breakups
        private IEnumerable<XElement> GetRoomsPriceBreakupHotelBeds(List<XElement> pricebreakups)
        {
            #region HotelBeds Room's Price Breakups

            List<XElement> str = new List<XElement>();
            try
            {
                //Parallel.For(0, pricebreakups.Count(), i =>
                for (int i = 0; i < pricebreakups.Count();i++ )
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                           new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].Attribute("dailyNet").Value)))
                    );
                }
                return str.OrderBy(x => x.Attribute("Night").Value).ToList();
            }
            catch { return null; }

            #endregion
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
        private static XElement stringAsNode(List<string> toBeAdded, XElement Element)
        {
            XElement response = Element;
            foreach (string s in toBeAdded)
                Element.Add(new XElement("Node", s));
            return response;
        }
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