using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.HotelBeds;

namespace TravillioXMLOutService.App_Code
{
    public class PreBookHotelBeds
    {
        XElement reqTravillio;
        string ratecomment = string.Empty;
        int totalngt = 1;
        #region Credentails of HotelBeds
        string apiKey = string.Empty;
        string Secret = string.Empty;
        string dmc = string.Empty;
        #endregion
        #region Price Check
        public static bool Check(decimal first, decimal second, decimal margin)
        {
            return Math.Abs(first - second) <= margin;
        }
        #endregion
        #region PreBooking of HotelBeds (XML OUT for Travayoo)
        public XElement PrebookingHotelBeds(XElement req,string xmlout)
        {
            string reqstrprebook = string.Empty;
            reqTravillio = req;
            XElement hotelprebookresponse = null;
            XElement hotelpreBooking = null;
            dmc = xmlout;
            try
            {
                //HotelBedsCredential _credential = new HotelBedsCredential();
                //apiKey = _credential.apiKey;
                //Secret = _credential.Secret;
                //string endpoint = "https://api.test.hotelbeds.com/hotel-api/1.0/checkrates";
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "4");
                apiKey = suppliercred.Descendants("apiKey").FirstOrDefault().Value;
                Secret = suppliercred.Descendants("Secret").FirstOrDefault().Value;
                string endpoint = suppliercred.Descendants("checkrateendpoint").FirstOrDefault().Value;
                #endregion
                string checkin = req.Descendants("FromDate").FirstOrDefault().Value;
                string checkout = req.Descendants("ToDate").FirstOrDefault().Value;


                DateTime checkindt = DateTime.ParseExact(checkin, "dd/MM/yyyy", null);
                DateTime checkoutdt = DateTime.ParseExact(checkout, "dd/MM/yyyy", null);



                DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                double days = (checkoutdt - checkindt).TotalDays;
                totalngt = Convert.ToInt32(days);

               
                List<XElement> getroom = reqTravillio.Descendants("Room").ToList();
                string hotel = string.Empty;
                //string sourcemarket = Convert.ToString(req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value).ToUpper().ToString();

                XElement occupancyrequest = new XElement(
                        new XElement("rooms", getroomkey(getroom)));
                string reqstr = "<checkRateRQ xmlns='http://www.hotelbeds.com/schemas/messages' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' upselling='false' language='ENG' dailyRate='true'>" +
	                               occupancyrequest +
                                "</checkRateRQ>";

                reqstrprebook = reqstr;
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

                    client.Headers.Add("X-Signature", signature);
                    client.Headers.Add("Api-Key", apiKey);
                    client.Headers.Add("Accept", "application/xml");
                    client.Headers.Add("Content-Type", "application/xml");
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12;
                    response = client.UploadString(endpoint, reqstr);


                    XElement availresponse = XElement.Parse(response.ToString());

                    XElement doc = RemoveAllNamespaces(availresponse);

                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                    log.TrackNumber = req.Descendants("TransID").Single().Value;
                    log.LogTypeID = 4;
                    log.LogType = "PreBook";
                    log.SupplierID = 4;
                    log.logrequestXML = reqstr.ToString();
                    log.logresponseXML = doc.ToString();
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SaveAPILogs(log);

                    XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                    List<XElement> hotelavailabilityres = doc.Descendants("hotel").ToList();
                    #region Rate Comment
                    try
                    {
                        List<XElement> totroom = req.Descendants("Room").GroupBy(x=>x.Attribute("OccupancyID").Value).Select(y=>y.First()).ToList();
                        for (int k = 0; k < totroom.Count(); k++)
                        {
                            if (totroom[k].Attribute("OccupancyID").Value != "")
                            {
                                try
                                {
                                    string ratecommentresponse = string.Empty;
                                    using (var clientc = new WebClient())
                                    {
                                        clientc.Headers.Add("X-Signature", signature);
                                        clientc.Headers.Add("Api-Key", apiKey);
                                        clientc.Headers.Add("Accept", "application/xml");
                                        clientc.Headers.Add("Content-Type", "application/xml");
                                        string urlcomment = suppliercred.Descendants("commentendpoint").FirstOrDefault().Value;
                                        urlcomment = urlcomment + "?code=" + totroom[k].Attribute("OccupancyID").Value + "&date=" + checkin2;
                                        ratecommentresponse = clientc.DownloadString(urlcomment);
                                    }
                                    XElement rateresponse = XElement.Parse(ratecommentresponse.ToString());
                                    XElement ratedoc = RemoveAllNamespaces(rateresponse);
                                    try
                                    {
                                        APILogDetail log2 = new APILogDetail();
                                        log2.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                                        log2.TrackNumber = req.Descendants("TransID").Single().Value;
                                        log2.LogTypeID = 54;
                                        log2.LogType = "TermCondition";
                                        log2.SupplierID = 4;
                                        log2.logrequestXML = rateresponse.ToString();
                                        log2.logresponseXML = ratedoc.ToString();
                                        SaveAPILog saveex2 = new SaveAPILog();
                                        saveex2.SaveAPILogs(log2);
                                    }
                                    catch (Exception ex)
                                    {
                                        CustomException ex1 = new CustomException(ex);
                                        ex1.MethodName = "RateCommentroomwisesave";
                                        ex1.PageName = "PreBookHotelBeds";
                                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                                        ex1.TranID = req.Descendants("TransID").Single().Value;
                                        SaveAPILog saveex1 = new SaveAPILog();
                                        saveex1.SendCustomExcepToDB(ex1);
                                    }
                                    string ratecom = string.Empty;
                                    List<XElement> hotelrateres = ratedoc.Descendants("description").ToList();
                                    for (int i = 0; i < hotelrateres.Count(); i++)
                                    {
                                        ratecom += hotelrateres[i].Value;
                                    }
                                    ratecomment += totroom[k].Attribute("RoomType").Value + ": " + ratecom + ". ";
                                }
                                catch (Exception ex)
                                {
                                    CustomException ex1 = new CustomException(ex);
                                    ex1.MethodName = "RateCommentroomwise";
                                    ex1.PageName = "PreBookHotelBeds";
                                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                                    ex1.TranID = req.Descendants("TransID").Single().Value;
                                    SaveAPILog saveex1 = new SaveAPILog();
                                    saveex1.SendCustomExcepToDB(ex1);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "RateComment";
                        ex1.PageName = "PreBookHotelBeds";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex1 = new SaveAPILog();
                        saveex1.SendCustomExcepToDB(ex1);
                    }

                    #endregion
                    hotelprebookresponse = GetHotelListHotelBeds(hotelavailabilityres).FirstOrDefault();
                    #region PreBooking Response
                    IEnumerable<XElement> request = req.Descendants("HotelPreBookingRequest").ToList();
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    string username = req.Descendants("UserName").Single().Value;
                    string password = req.Descendants("Password").Single().Value;
                    string AgentID = req.Descendants("AgentID").Single().Value;
                    string ServiceType = req.Descendants("ServiceType").Single().Value;
                    string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                    string supplierid = req.Descendants("SupplierID").Single().Value;
                    decimal oldprice = 0;
                    decimal newprice = 0;
                    decimal margin = 0.01m;
                    oldprice = Convert.ToDecimal(req.Descendants("HotelPreBookingRequest").Descendants("RoomTypes").Attributes("TotalRate").FirstOrDefault().Value);
                    newprice = Convert.ToDecimal(hotelprebookresponse.Descendants("RoomTypes").Attributes("TotalRate").FirstOrDefault().Value);
                    bool pricechange = Check(oldprice, newprice, margin); 
                    #region XML OUT
                    if (pricechange == true)
                    {
                        hotelpreBooking = new XElement(
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
                    else
                    {
                        hotelpreBooking = new XElement(
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
                                                        new XElement("ErrorTxt", "Amount has been changed"),
                                                        new XElement("NewPrice", newprice),
                                                       new XElement("Hotels",
                                                           hotelprebookresponse
                                          )))));
                    }                    
                    #endregion
                    #endregion                    
                }
                return hotelpreBooking;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PrebookingHotelBeds";
                ex1.PageName = "PreBookHotelBeds";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                    log.TrackNumber = req.Descendants("TransID").Single().Value;
                    log.LogTypeID = 4;
                    log.LogType = "PreBook";
                    log.SupplierID = 4;
                    log.logrequestXML = reqstrprebook.ToString();
                    log.logresponseXML = ex.Message.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch { }
                try
                {
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    IEnumerable<XElement> request = req.Descendants("HotelPreBookingRequest").ToList();
                    string username = req.Descendants("UserName").Single().Value;
                    string password = req.Descendants("Password").Single().Value;
                    string AgentID = req.Descendants("AgentID").Single().Value;
                    string ServiceType = req.Descendants("ServiceType").Single().Value;
                    string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                    hotelpreBooking = new XElement(
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
                                                           new XElement("Hotels",
                                                               new XElement("ErrorTxt", ex.Message)
                                              )))));
                    return hotelpreBooking;
                }
                catch { return null; }
            }
        }
        #endregion
        #region HotelBeds
        #region HotelBeds Hotel Listing
        private IEnumerable<XElement> GetHotelListHotelBeds(List<XElement> htlist)
        {
            #region HotelBeds
            List<XElement> hotellst = new List<XElement>();
            Int32 length = htlist.Count();

            try
            {
                //Parallel.For(0, length, i =>
                for (int i = 0; i < length; i++)
                {
                    string tandc = string.Empty;
                    try
                    {
                        tandc = GetHotelTermConditionHotelBeds(htlist[i].Descendants("room").ToList(), htlist);
                    }
                    catch { }
                    hotellst.Add(new XElement("Hotel",
                                           new XElement("HotelID", Convert.ToString(htlist[i].Attribute("code").Value)),
                                                       new XElement("HotelName", Convert.ToString(htlist[i].Attribute("name").Value)),
                                                       new XElement("Status", "true"),
                                                       new XElement("TermCondition", tandc + ". " + ratecomment),
                                                       new XElement("HotelImgSmall", Convert.ToString("")),
                                                       new XElement("HotelImgLarge", Convert.ToString("")),
                                                       new XElement("MapLink", ""),
                                                       new XElement("DMC", dmc),
                                                       new XElement("Currency", ""),
                                                       new XElement("Offers", "")
                                                       , new XElement("Rooms",
                                                GetHotelRoomListingHotelBeds(htlist[i].Descendants("room").ToList(), htlist)
                                               )
                    ));

                }
            }
            catch (Exception ex)
            {
                return hotellst;
            }
            return hotellst;
            #endregion
        }
        #endregion
        #region HotelBeds Hotel's Room Listing
        private IEnumerable<XElement> GetHotelRoomListingHotelBeds(List<XElement> roomlist, List<XElement> htlist)
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

            int totalroom = Convert.ToInt32(reqTravillio.Descendants("RoomPax").Count());

            #region Notes: The maximum number of rooms that can be retrieved by a single search request is nine (9)
            #endregion

            #region Room Count 1
            if (totalroom == 1)
            {
                //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "1").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
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
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion

                for (int m = 0; m < roomList1.Count(); m++)
                {
                    List<XElement> pricebrkups = roomList1[m].Descendants("dailyRate").ToList();
                    IEnumerable<XElement> breakups = null; 
                    if (pricebrkups.Count() > 0)
                    {
                        breakups = GetRoomsPriceBreakupHotelBeds(pricebrkups);
                    }
                    else
                    {
                        breakups = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList1[m].Attribute("net").Value));
                    }
                    List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();
                    List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();

                    #region With Board Bases
                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].Attribute("net").Value), new XAttribute("Index", m + 1),
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
                             new XAttribute("PerNightRoomRate", Convert.ToString("0")),
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
                                 new XElement("PriceBreakups", breakups),
                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                             ),
                             new XElement("CancellationPolicies",
                         GetRoomCancellationPolicyHotelBeds(htlist[0].Descendants("cancellationPolicies").ToList(), htlist[0].Attribute("currency").Value))
                             ));
                    #endregion
                }
                return str;
            }
            #endregion

            #region Room Count 2
            if (totalroom == 2)
            {
                List<XElement> roomratekey = reqTravillio.Descendants("Room").ToList();
                //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "1").ToList();
                //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "2").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
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

                        List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                        List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                        List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();
                        List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();
                        List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();
                        List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();

                        IEnumerable<XElement> breakups1 = null;
                        if (pricebrkups1.Count() > 0)
                        {
                            breakups1 = GetRoomsPriceBreakupHotelBeds(pricebrkups1);
                        }
                        else
                        {
                            breakups1 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList1[m].Attribute("net").Value));
                        }
                        IEnumerable<XElement> breakups2 = null;
                        if (pricebrkups2.Count() > 0)
                        {
                            breakups2 = GetRoomsPriceBreakupHotelBeds(pricebrkups2);
                        }
                        else
                        {
                            breakups2 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList2[n].Attribute("net").Value));
                        }

                        #region Board Bases >0
                        group++;
                        decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value);
                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

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
                             new XElement("PriceBreakups", breakups1),
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
                            //new XElement("Promotions", Convert.ToString(promo2))),
                         new XElement("CancellationPolicy", ""),
                         new XElement("Amenities", new XElement("Amenity", "")),
                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                         new XElement("Supplements", ""
                             ),
                             new XElement("PriceBreakups", breakups2),
                             new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                             new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                         ),
                                 new XElement("CancellationPolicies",
                         GetRoomCancellationPolicyHotelBeds(htlist[0].Descendants("cancellationPolicies").ToList(), htlist[0].Attribute("currency").Value))));




                        #endregion
                        return str;
                    }
                }
                return str;
            }
            #endregion

            #region Room Count 3
            if (totalroom == 3)
            {
                List<XElement> roomratekey = reqTravillio.Descendants("Room").ToList();
                //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "1").ToList();
                //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "2").ToList();
                //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "3").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                int group = 0;

                #region Room 3
                for (int m = 0; m < roomList1.Count(); m++)
                //Parallel.For(0, roomList1.Count(), m =>
                {
                    for(int n=0;n<roomList2.Count();n++)
                    //Parallel.For(0, roomList2.Count(), n =>
                    {
                        for(int o=0;o<roomList3.Count();o++)
                        //Parallel.For(0, roomList3.Count(), o =>
                        {
                            // add room 1, 2, 3

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

                            IEnumerable<XElement> breakups1 = null;
                            if (pricebrkups1.Count() > 0)
                            {
                                breakups1 = GetRoomsPriceBreakupHotelBeds(pricebrkups1);
                            }
                            else
                            {
                                breakups1 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList1[m].Attribute("net").Value));
                            }
                            IEnumerable<XElement> breakups2 = null;
                            if (pricebrkups2.Count() > 0)
                            {
                                breakups2 = GetRoomsPriceBreakupHotelBeds(pricebrkups2);
                            }
                            else
                            {
                                breakups2 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList2[n].Attribute("net").Value));
                            }
                            IEnumerable<XElement> breakups3 = null;
                            if (pricebrkups3.Count() > 0)
                            {
                                breakups3 = GetRoomsPriceBreakupHotelBeds(pricebrkups3);
                            }
                            else
                            {
                                breakups3 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList3[o].Attribute("net").Value));
                            }

                            group++;
                            decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value);

                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

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
                                 new XElement("PriceBreakups", breakups1),
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
                                 new XElement("PriceBreakups", breakups2),
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
                                 new XElement("PriceBreakups", breakups3),
                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Attribute("adults").Value)),
                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Attribute("children").Value))
                             ),
                                 new XElement("CancellationPolicies",
                         GetRoomCancellationPolicyHotelBeds(htlist[0].Descendants("cancellationPolicies").ToList(), htlist[0].Attribute("currency").Value))));
                            #endregion
                            return str;
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
                List<XElement> roomratekey = reqTravillio.Descendants("Room").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                int group = 0;
                #region Room 4
                for(int m=0;m<roomList1.Count();m++)
                {
                    for(int n=0;n<roomList2.Count();n++)
                    {
                        for(int o=0;o<roomList3.Count();o++)
                        {
                            for(int p=0;p<roomList4.Count();p++)
                            {
                                // add room 1, 2, 3,4
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

                                IEnumerable<XElement> breakups1 = null;
                                if (pricebrkups1.Count() > 0)
                                {
                                    breakups1 = GetRoomsPriceBreakupHotelBeds(pricebrkups1);
                                }
                                else
                                {
                                    breakups1 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList1[m].Attribute("net").Value));
                                }
                                IEnumerable<XElement> breakups2 = null;
                                if (pricebrkups2.Count() > 0)
                                {
                                    breakups2 = GetRoomsPriceBreakupHotelBeds(pricebrkups2);
                                }
                                else
                                {
                                    breakups2 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList2[n].Attribute("net").Value));
                                }
                                IEnumerable<XElement> breakups3 = null;
                                if (pricebrkups3.Count() > 0)
                                {
                                    breakups3 = GetRoomsPriceBreakupHotelBeds(pricebrkups3);
                                }
                                else
                                {
                                    breakups3 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList3[o].Attribute("net").Value));
                                }
                                IEnumerable<XElement> breakups4 = null;
                                if (pricebrkups4.Count() > 0)
                                {
                                    breakups4 = GetRoomsPriceBreakupHotelBeds(pricebrkups4);
                                }
                                else
                                {
                                    breakups4 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList4[p].Attribute("net").Value));
                                }

                                group++;
                                decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value);

                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

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
                                     new XElement("PriceBreakups", breakups1),
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
                                     new XElement("PriceBreakups", breakups2),
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
                                     new XElement("PriceBreakups", breakups3),
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
                                     new XElement("PriceBreakups", breakups4),
                                     new XElement("AdultNum", Convert.ToString(roomList4[p].Attribute("adults").Value)),
                                     new XElement("ChildNum", Convert.ToString(roomList4[p].Attribute("children").Value))
                                 ),
                                 new XElement("CancellationPolicies",
                         GetRoomCancellationPolicyHotelBeds(htlist[0].Descendants("cancellationPolicies").ToList(), htlist[0].Attribute("currency").Value))
                                 ));
                                #endregion
                                return str;
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
                List<XElement> roomratekey = reqTravillio.Descendants("Room").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild5 == "0")
                {
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild5 == "1")
                {
                    List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
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

                                    IEnumerable<XElement> breakups1 = null;
                                    if (pricebrkups1.Count() > 0)
                                    {
                                        breakups1 = GetRoomsPriceBreakupHotelBeds(pricebrkups1);
                                    }
                                    else
                                    {
                                        breakups1 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList1[m].Attribute("net").Value));
                                    }
                                    IEnumerable<XElement> breakups2 = null;
                                    if (pricebrkups2.Count() > 0)
                                    {
                                        breakups2 = GetRoomsPriceBreakupHotelBeds(pricebrkups2);
                                    }
                                    else
                                    {
                                        breakups2 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList2[n].Attribute("net").Value));
                                    }
                                    IEnumerable<XElement> breakups3 = null;
                                    if (pricebrkups3.Count() > 0)
                                    {
                                        breakups3 = GetRoomsPriceBreakupHotelBeds(pricebrkups3);
                                    }
                                    else
                                    {
                                        breakups3 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList3[o].Attribute("net").Value));
                                    }
                                    IEnumerable<XElement> breakups4 = null;
                                    if (pricebrkups4.Count() > 0)
                                    {
                                        breakups4 = GetRoomsPriceBreakupHotelBeds(pricebrkups4);
                                    }
                                    else
                                    {
                                        breakups4 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList4[p].Attribute("net").Value));
                                    }
                                    IEnumerable<XElement> breakups5 = null;
                                    if (pricebrkups5.Count() > 0)
                                    {
                                        breakups5 = GetRoomsPriceBreakupHotelBeds(pricebrkups5);
                                    }
                                    else
                                    {
                                        breakups5 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList5[q].Attribute("net").Value));
                                    }

                                    group++;
                                    decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value);

                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

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
                                         new XElement("PriceBreakups", breakups1),
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
                                         new XElement("PriceBreakups", breakups2),
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
                                         new XElement("PriceBreakups", breakups3),
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
                                         new XElement("PriceBreakups", breakups4),
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
                                         new XElement("PriceBreakups", breakups5),
                                         new XElement("AdultNum", Convert.ToString(roomList5[q].Attribute("adults").Value)),
                                         new XElement("ChildNum", Convert.ToString(roomList5[q].Attribute("children").Value))
                                     ),
                                     new XElement("CancellationPolicies",
                             GetRoomCancellationPolicyHotelBeds(htlist[0].Descendants("cancellationPolicies").ToList(), htlist[0].Attribute("currency").Value))
                                     ));
                                    #endregion
                                    return str;
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
                List<XElement> roomratekey = reqTravillio.Descendants("Room").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild5 == "0")
                {
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild5 == "1")
                {
                    List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild6 == "0")
                {
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild6 == "1")
                {
                    List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
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

                                        IEnumerable<XElement> breakups1 = null;
                                        if (pricebrkups1.Count() > 0)
                                        {
                                            breakups1 = GetRoomsPriceBreakupHotelBeds(pricebrkups1);
                                        }
                                        else
                                        {
                                            breakups1 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList1[m].Attribute("net").Value));
                                        }
                                        IEnumerable<XElement> breakups2 = null;
                                        if (pricebrkups2.Count() > 0)
                                        {
                                            breakups2 = GetRoomsPriceBreakupHotelBeds(pricebrkups2);
                                        }
                                        else
                                        {
                                            breakups2 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList2[n].Attribute("net").Value));
                                        }
                                        IEnumerable<XElement> breakups3 = null;
                                        if (pricebrkups3.Count() > 0)
                                        {
                                            breakups3 = GetRoomsPriceBreakupHotelBeds(pricebrkups3);
                                        }
                                        else
                                        {
                                            breakups3 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList3[o].Attribute("net").Value));
                                        }
                                        IEnumerable<XElement> breakups4 = null;
                                        if (pricebrkups4.Count() > 0)
                                        {
                                            breakups4 = GetRoomsPriceBreakupHotelBeds(pricebrkups4);
                                        }
                                        else
                                        {
                                            breakups4 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList4[p].Attribute("net").Value));
                                        }
                                        IEnumerable<XElement> breakups5 = null;
                                        if (pricebrkups5.Count() > 0)
                                        {
                                            breakups5 = GetRoomsPriceBreakupHotelBeds(pricebrkups5);
                                        }
                                        else
                                        {
                                            breakups5 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList5[q].Attribute("net").Value));
                                        }
                                        IEnumerable<XElement> breakups6 = null;
                                        if (pricebrkups6.Count() > 0)
                                        {
                                            breakups6 = GetRoomsPriceBreakupHotelBeds(pricebrkups6);
                                        }
                                        else
                                        {
                                            breakups6 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList6[r].Attribute("net").Value));
                                        }

                                        group++;
                                        decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value);

                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

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
                                             new XElement("PriceBreakups", breakups1),
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
                                             new XElement("PriceBreakups", breakups2),
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
                                             new XElement("PriceBreakups", breakups3),
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
                                             new XElement("PriceBreakups", breakups4),
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
                                             new XElement("PriceBreakups", breakups5),
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
                                             new XElement("PriceBreakups", breakups6),
                                             new XElement("AdultNum", Convert.ToString(roomList6[r].Attribute("adults").Value)),
                                             new XElement("ChildNum", Convert.ToString(roomList6[r].Attribute("children").Value))
                                         ),
                                         new XElement("CancellationPolicies",
                                 GetRoomCancellationPolicyHotelBeds(htlist[0].Descendants("cancellationPolicies").ToList(), htlist[0].Attribute("currency").Value))
                                         ));
                                        #endregion
                                        return str;
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
                List<XElement> roomratekey = reqTravillio.Descendants("Room").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild5 == "0")
                {
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild5 == "1")
                {
                    List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild6 == "0")
                {
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild6 == "1")
                {
                    List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 7)
                List<XElement> room7child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild7 = room7child[6].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild7 == "0")
                {
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[6].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild7 == "1")
                {
                    List<XElement> childage = room7child[6].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[6].Attribute("SessionID").Value).ToList();
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
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children7ages) : false && el.Attribute("rateKey").Value == roomratekey[6].Attribute("SessionID").Value).ToList();
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

                                            IEnumerable<XElement> breakups1 = null;
                                            if (pricebrkups1.Count() > 0)
                                            {
                                                breakups1 = GetRoomsPriceBreakupHotelBeds(pricebrkups1);
                                            }
                                            else
                                            {
                                                breakups1 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList1[m].Attribute("net").Value));
                                            }
                                            IEnumerable<XElement> breakups2 = null;
                                            if (pricebrkups2.Count() > 0)
                                            {
                                                breakups2 = GetRoomsPriceBreakupHotelBeds(pricebrkups2);
                                            }
                                            else
                                            {
                                                breakups2 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList2[n].Attribute("net").Value));
                                            }
                                            IEnumerable<XElement> breakups3 = null;
                                            if (pricebrkups3.Count() > 0)
                                            {
                                                breakups3 = GetRoomsPriceBreakupHotelBeds(pricebrkups3);
                                            }
                                            else
                                            {
                                                breakups3 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList3[o].Attribute("net").Value));
                                            }
                                            IEnumerable<XElement> breakups4 = null;
                                            if (pricebrkups4.Count() > 0)
                                            {
                                                breakups4 = GetRoomsPriceBreakupHotelBeds(pricebrkups4);
                                            }
                                            else
                                            {
                                                breakups4 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList4[p].Attribute("net").Value));
                                            }
                                            IEnumerable<XElement> breakups5 = null;
                                            if (pricebrkups5.Count() > 0)
                                            {
                                                breakups5 = GetRoomsPriceBreakupHotelBeds(pricebrkups5);
                                            }
                                            else
                                            {
                                                breakups5 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList5[q].Attribute("net").Value));
                                            }
                                            IEnumerable<XElement> breakups6 = null;
                                            if (pricebrkups6.Count() > 0)
                                            {
                                                breakups6 = GetRoomsPriceBreakupHotelBeds(pricebrkups6);
                                            }
                                            else
                                            {
                                                breakups6 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList6[r].Attribute("net").Value));
                                            }
                                            IEnumerable<XElement> breakups7 = null;
                                            if (pricebrkups7.Count() > 0)
                                            {
                                                breakups7 = GetRoomsPriceBreakupHotelBeds(pricebrkups7);
                                            }
                                            else
                                            {
                                                breakups7 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList7[s].Attribute("net").Value));
                                            }

                                            group++;
                                            decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value) + Convert.ToDecimal(roomList7[s].Attribute("net").Value);

                                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

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
                                                 new XElement("PriceBreakups", breakups1),
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
                                                 new XElement("PriceBreakups", breakups2),
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
                                                 new XElement("PriceBreakups", breakups3),
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
                                                 new XElement("PriceBreakups", breakups4),
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
                                                 new XElement("PriceBreakups", breakups5),
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
                                                 new XElement("PriceBreakups", breakups6),
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
                                                 new XElement("PriceBreakups", breakups7),
                                                 new XElement("AdultNum", Convert.ToString(roomList7[s].Attribute("adults").Value)),
                                                 new XElement("ChildNum", Convert.ToString(roomList7[s].Attribute("children").Value))
                                             ),
                                             new XElement("CancellationPolicies",
                                     GetRoomCancellationPolicyHotelBeds(htlist[0].Descendants("cancellationPolicies").ToList(), htlist[0].Attribute("currency").Value))
                                             ));
                                            #endregion
                                            return str;
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
                List<XElement> roomratekey = reqTravillio.Descendants("Room").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild5 == "0")
                {
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild5 == "1")
                {
                    List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild6 == "0")
                {
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild6 == "1")
                {
                    List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 7)
                List<XElement> room7child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild7 = room7child[6].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild7 == "0")
                {
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[6].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild7 == "1")
                {
                    List<XElement> childage = room7child[6].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[6].Attribute("SessionID").Value).ToList();
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
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children7ages) : false && el.Attribute("rateKey").Value == roomratekey[6].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 8)
                List<XElement> room8child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild8 = room8child[7].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild8 == "0")
                {
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[7].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild8 == "1")
                {
                    List<XElement> childage = room8child[7].Descendants("ChildAge").ToList();
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[7].Attribute("SessionID").Value).ToList();
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
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children8ages) : false && el.Attribute("rateKey").Value == roomratekey[7].Attribute("SessionID").Value).ToList();
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

                                                IEnumerable<XElement> breakups1 = null;
                                                if (pricebrkups1.Count() > 0)
                                                {
                                                    breakups1 = GetRoomsPriceBreakupHotelBeds(pricebrkups1);
                                                }
                                                else
                                                {
                                                    breakups1 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList1[m].Attribute("net").Value));
                                                }
                                                IEnumerable<XElement> breakups2 = null;
                                                if (pricebrkups2.Count() > 0)
                                                {
                                                    breakups2 = GetRoomsPriceBreakupHotelBeds(pricebrkups2);
                                                }
                                                else
                                                {
                                                    breakups2 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList2[n].Attribute("net").Value));
                                                }
                                                IEnumerable<XElement> breakups3 = null;
                                                if (pricebrkups3.Count() > 0)
                                                {
                                                    breakups3 = GetRoomsPriceBreakupHotelBeds(pricebrkups3);
                                                }
                                                else
                                                {
                                                    breakups3 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList3[o].Attribute("net").Value));
                                                }
                                                IEnumerable<XElement> breakups4 = null;
                                                if (pricebrkups4.Count() > 0)
                                                {
                                                    breakups4 = GetRoomsPriceBreakupHotelBeds(pricebrkups4);
                                                }
                                                else
                                                {
                                                    breakups4 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList4[p].Attribute("net").Value));
                                                }
                                                IEnumerable<XElement> breakups5 = null;
                                                if (pricebrkups5.Count() > 0)
                                                {
                                                    breakups5 = GetRoomsPriceBreakupHotelBeds(pricebrkups5);
                                                }
                                                else
                                                {
                                                    breakups5 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList5[q].Attribute("net").Value));
                                                }
                                                IEnumerable<XElement> breakups6 = null;
                                                if (pricebrkups6.Count() > 0)
                                                {
                                                    breakups6 = GetRoomsPriceBreakupHotelBeds(pricebrkups6);
                                                }
                                                else
                                                {
                                                    breakups6 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList6[r].Attribute("net").Value));
                                                }
                                                IEnumerable<XElement> breakups7 = null;
                                                if (pricebrkups7.Count() > 0)
                                                {
                                                    breakups7 = GetRoomsPriceBreakupHotelBeds(pricebrkups7);
                                                }
                                                else
                                                {
                                                    breakups7 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList7[s].Attribute("net").Value));
                                                }
                                                IEnumerable<XElement> breakups8 = null;
                                                if (pricebrkups8.Count() > 0)
                                                {
                                                    breakups8 = GetRoomsPriceBreakupHotelBeds(pricebrkups8);
                                                }
                                                else
                                                {
                                                    breakups8 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList8[t].Attribute("net").Value));
                                                }

                                                group++;
                                                decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value) + Convert.ToDecimal(roomList7[s].Attribute("net").Value) + Convert.ToDecimal(roomList8[t].Attribute("net").Value);

                                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

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
                                                     new XElement("PriceBreakups", breakups1),
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
                                                     new XElement("PriceBreakups", breakups2),
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
                                                     new XElement("PriceBreakups", breakups3),
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
                                                     new XElement("PriceBreakups", breakups4),
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
                                                     new XElement("PriceBreakups", breakups5),
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
                                                     new XElement("PriceBreakups", breakups6),
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
                                                     new XElement("PriceBreakups", breakups7),
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
                                                     new XElement("PriceBreakups", breakups8),
                                                     new XElement("AdultNum", Convert.ToString(roomList8[t].Attribute("adults").Value)),
                                                     new XElement("ChildNum", Convert.ToString(roomList8[t].Attribute("children").Value))
                                                 ),
                                                 new XElement("CancellationPolicies",
                                         GetRoomCancellationPolicyHotelBeds(htlist[0].Descendants("cancellationPolicies").ToList(), htlist[0].Attribute("currency").Value))
                                                 ));
                                                #endregion
                                                return str;
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
                List<XElement> roomratekey = reqTravillio.Descendants("Room").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages && el.Attribute("rateKey").Value == roomratekey[0].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages && el.Attribute("rateKey").Value == roomratekey[1].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages && el.Attribute("rateKey").Value == roomratekey[2].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages && el.Attribute("rateKey").Value == roomratekey[3].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild5 == "0")
                {
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild5 == "1")
                {
                    List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false && el.Attribute("rateKey").Value == roomratekey[4].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild6 == "0")
                {
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild6 == "1")
                {
                    List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false && el.Attribute("rateKey").Value == roomratekey[5].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 7)
                List<XElement> room7child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild7 = room7child[6].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild7 == "0")
                {
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[6].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild7 == "1")
                {
                    List<XElement> childage = room7child[6].Descendants("ChildAge").ToList();
                    //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[6].Attribute("SessionID").Value).ToList();
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
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children7ages) : false && el.Attribute("rateKey").Value == roomratekey[6].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 8)
                List<XElement> room8child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild8 = room8child[7].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild8 == "0")
                {
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[7].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild8 == "1")
                {
                    List<XElement> childage = room8child[7].Descendants("ChildAge").ToList();
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[7].Attribute("SessionID").Value).ToList();
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
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children8ages) : false && el.Attribute("rateKey").Value == roomratekey[7].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 9)
                List<XElement> room9child = reqTravillio.Descendants("RoomPax").ToList();
                string totalchild9 = room9child[8].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild9 == "0")
                {
                    roomList9 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room9child[8].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0" && el.Attribute("rateKey").Value == roomratekey[8].Attribute("SessionID").Value).ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild9 == "1")
                {
                    List<XElement> childage = room9child[8].Descendants("ChildAge").ToList();
                    roomList9 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room9child[8].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false && el.Attribute("rateKey").Value == roomratekey[8].Attribute("SessionID").Value).ToList();
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
                    roomList9 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room9child[8].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children9ages) : false && el.Attribute("rateKey").Value == roomratekey[8].Attribute("SessionID").Value).ToList();
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

                                                    IEnumerable<XElement> breakups1 = null;
                                                    if (pricebrkups1.Count() > 0)
                                                    {
                                                        breakups1 = GetRoomsPriceBreakupHotelBeds(pricebrkups1);
                                                    }
                                                    else
                                                    {
                                                        breakups1 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList1[m].Attribute("net").Value));
                                                    }
                                                    IEnumerable<XElement> breakups2 = null;
                                                    if (pricebrkups2.Count() > 0)
                                                    {
                                                        breakups2 = GetRoomsPriceBreakupHotelBeds(pricebrkups2);
                                                    }
                                                    else
                                                    {
                                                        breakups2 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList2[n].Attribute("net").Value));
                                                    }
                                                    IEnumerable<XElement> breakups3 = null;
                                                    if (pricebrkups3.Count() > 0)
                                                    {
                                                        breakups3 = GetRoomsPriceBreakupHotelBeds(pricebrkups3);
                                                    }
                                                    else
                                                    {
                                                        breakups3 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList3[o].Attribute("net").Value));
                                                    }
                                                    IEnumerable<XElement> breakups4 = null;
                                                    if (pricebrkups4.Count() > 0)
                                                    {
                                                        breakups4 = GetRoomsPriceBreakupHotelBeds(pricebrkups4);
                                                    }
                                                    else
                                                    {
                                                        breakups4 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList4[p].Attribute("net").Value));
                                                    }
                                                    IEnumerable<XElement> breakups5 = null;
                                                    if (pricebrkups5.Count() > 0)
                                                    {
                                                        breakups5 = GetRoomsPriceBreakupHotelBeds(pricebrkups5);
                                                    }
                                                    else
                                                    {
                                                        breakups5 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList5[q].Attribute("net").Value));
                                                    }
                                                    IEnumerable<XElement> breakups6 = null;
                                                    if (pricebrkups6.Count() > 0)
                                                    {
                                                        breakups6 = GetRoomsPriceBreakupHotelBeds(pricebrkups6);
                                                    }
                                                    else
                                                    {
                                                        breakups6 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList6[r].Attribute("net").Value));
                                                    }
                                                    IEnumerable<XElement> breakups7 = null;
                                                    if (pricebrkups7.Count() > 0)
                                                    {
                                                        breakups7 = GetRoomsPriceBreakupHotelBeds(pricebrkups7);
                                                    }
                                                    else
                                                    {
                                                        breakups7 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList7[s].Attribute("net").Value));
                                                    }
                                                    IEnumerable<XElement> breakups8 = null;
                                                    if (pricebrkups8.Count() > 0)
                                                    {
                                                        breakups8 = GetRoomsPriceBreakupHotelBeds(pricebrkups8);
                                                    }
                                                    else
                                                    {
                                                        breakups8 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList8[t].Attribute("net").Value));
                                                    }
                                                    IEnumerable<XElement> breakups9 = null;
                                                    if (pricebrkups9.Count() > 0)
                                                    {
                                                        breakups9 = GetRoomsPriceBreakupHotelBeds(pricebrkups9);
                                                    }
                                                    else
                                                    {
                                                        breakups9 = GetRoomsPriceBreakupHotelBedsTRV(Convert.ToDecimal(roomList9[u].Attribute("net").Value));
                                                    }

                                                    group++;
                                                    decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value) + Convert.ToDecimal(roomList7[s].Attribute("net").Value) + Convert.ToDecimal(roomList8[t].Attribute("net").Value) + Convert.ToDecimal(roomList9[u].Attribute("net").Value);

                                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

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
                                                         new XElement("PriceBreakups", breakups1),
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
                                                         new XElement("PriceBreakups", breakups2),
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
                                                         new XElement("PriceBreakups", breakups3),
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
                                                         new XElement("PriceBreakups", breakups4),
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
                                                         new XElement("PriceBreakups", breakups5),
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
                                                         new XElement("PriceBreakups", breakups6),
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
                                                         new XElement("PriceBreakups", breakups7),
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
                                                         new XElement("PriceBreakups", breakups8),
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
                                                         new XElement("PriceBreakups", breakups9),
                                                         new XElement("AdultNum", Convert.ToString(roomList9[u].Attribute("adults").Value)),
                                                         new XElement("ChildNum", Convert.ToString(roomList9[u].Attribute("children").Value))
                                                     ),
                                                     new XElement("CancellationPolicies",
                                             GetRoomCancellationPolicyHotelBeds(htlist[0].Descendants("cancellationPolicies").ToList(), htlist[0].Attribute("currency").Value))
                                                     ));
                                                    #endregion
                                                    return str;
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
        #region HotelBeds Hotel's Terms & Conditions
        private string GetHotelTermConditionHotelBeds(List<XElement> roomlist, List<XElement> htlist)
        {

            XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
            string str = string.Empty;
            List<XElement> roomList1 = new List<XElement>();
            List<XElement> roomList2 = new List<XElement>();
            List<XElement> roomList3 = new List<XElement>();
            List<XElement> roomList4 = new List<XElement>();
            List<XElement> roomList5 = new List<XElement>();
            List<XElement> roomList6 = new List<XElement>();
            List<XElement> roomList7 = new List<XElement>();
            List<XElement> roomList8 = new List<XElement>();
            List<XElement> roomList9 = new List<XElement>();
            int totalroom = Convert.ToInt32(reqTravillio.Descendants("RoomPax").Count());

            #region Notes: The maximum number of rooms that can be retrieved by a single search request is nine (9)
            #endregion

            #region Room Count 1
            if (totalroom == 1)
            {
                //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "1").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
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
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion

                for (int m = 0; m < roomList1.Count(); m++)
                {
                    string ratecomments = string.Empty;
                    try
                    {
                        ratecomments = roomList1[m].Attribute("rateComments").Value;
                    }
                    catch { }
                    str = str + " &amp;" + ratecomments;
                }
                return str;
            }
            #endregion

            #region Room Count 2
            if (totalroom == 2)
            {
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
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
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                }
                #endregion
                #endregion
          
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        string ratecomments1 = string.Empty;
                        string ratecomments2 = string.Empty;
                        try
                        {

                            ratecomments1 = roomList1[m].Attribute("rateComments").Value;
                        }
                        catch { }
                        try
                        {

                            ratecomments2 = roomList2[n].Attribute("rateComments").Value;
                        }
                        catch { }
                        
                        str = str + " &amp;" + ratecomments1 + " &amp;" + ratecomments2;
                        return str;
                    }
                }
                return str;
            }
            #endregion

            #region Room Count 3
            if (totalroom == 3)
            {
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
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
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
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
                            string ratecomments1 = string.Empty;
                            string ratecomments2 = string.Empty;
                            string ratecomments3 = string.Empty;
                            try
                            {

                                ratecomments1 = roomList1[m].Attribute("rateComments").Value;
                            }
                            catch { }
                            try
                            {

                                ratecomments2 = roomList2[n].Attribute("rateComments").Value;
                            }
                            catch { }
                            try
                            {

                                ratecomments3 = roomList3[o].Attribute("rateComments").Value;
                            }
                            catch { }

                            str = str + " &amp;" + ratecomments1 + " &amp;" + ratecomments2 + " &amp;" + ratecomments3;
                            return str;
                            
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
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
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
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages).ToList();
                }
                #endregion
                #endregion

                #region Room 4
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                string ratecomments1 = string.Empty;
                                string ratecomments2 = string.Empty;
                                string ratecomments3 = string.Empty;
                                string ratecomments4 = string.Empty;
                                try
                                {

                                    ratecomments1 = roomList1[m].Attribute("rateComments").Value;
                                }
                                catch { }
                                try
                                {

                                    ratecomments2 = roomList2[n].Attribute("rateComments").Value;
                                }
                                catch { }
                                try
                                {

                                    ratecomments3 = roomList3[o].Attribute("rateComments").Value;
                                }
                                catch { }
                                try
                                {

                                    ratecomments4 = roomList4[p].Attribute("rateComments").Value;
                                }
                                catch { }
                                str = str + " &amp;" + ratecomments1 + " &amp;" + ratecomments2 + " &amp;" + ratecomments3 + " &amp;" + ratecomments4;
                                return str;
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
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
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
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children5ages).ToList();
                }
                #endregion
                #endregion                

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
                                        string ratecomments1 = string.Empty;
                                        string ratecomments2 = string.Empty;
                                        string ratecomments3 = string.Empty;
                                        string ratecomments4 = string.Empty;
                                        string ratecomments5 = string.Empty;
                                        try
                                        {
                                            ratecomments1 = roomList1[m].Attribute("rateComments").Value;
                                        }
                                        catch { }
                                        try
                                        {
                                            ratecomments2 = roomList2[n].Attribute("rateComments").Value;
                                        }
                                        catch { }
                                        try
                                        {
                                            ratecomments3 = roomList3[o].Attribute("rateComments").Value;
                                        }
                                        catch { }
                                        try
                                        {
                                            ratecomments4 = roomList4[p].Attribute("rateComments").Value;
                                        }
                                        catch { }
                                        try
                                        {
                                            ratecomments5 = roomList5[q].Attribute("rateComments").Value;
                                        }
                                        catch { }                                       

                                        str = str + " &amp;" + ratecomments1 + " &amp;" + ratecomments2 + " &amp;" + ratecomments3 + " &amp;" + ratecomments4 + " &amp;" + ratecomments5;
                                        return str;                                    
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
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
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
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children5ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children6ages).ToList();
                }
                #endregion
                #endregion                

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
                                            string ratecomments1 = string.Empty;
                                            string ratecomments2 = string.Empty;
                                            string ratecomments3 = string.Empty;
                                            string ratecomments4 = string.Empty;
                                            string ratecomments5 = string.Empty;
                                            string ratecomments6 = string.Empty;
                                            try
                                            {
                                                ratecomments1 = roomList1[m].Attribute("rateComments").Value;
                                            }
                                            catch { }
                                            try
                                            {
                                                ratecomments2 = roomList2[n].Attribute("rateComments").Value;
                                            }
                                            catch { }
                                            try
                                            {
                                                ratecomments3 = roomList3[o].Attribute("rateComments").Value;
                                            }
                                            catch { }
                                            try
                                            {
                                                ratecomments4 = roomList4[p].Attribute("rateComments").Value;
                                            }
                                            catch { }
                                            try
                                            {
                                                ratecomments5 = roomList5[q].Attribute("rateComments").Value;
                                            }
                                            catch { }
                                            try
                                            {
                                                ratecomments6 = roomList6[r].Attribute("rateComments").Value;
                                            }
                                            catch { }                                           

                                            str = str + " &amp;" + ratecomments1 + " &amp;" + ratecomments2 + " &amp;" + ratecomments3 + " &amp;" + ratecomments4 + " &amp;" + ratecomments5 + " &amp;" + ratecomments6;
                                            return str;                                        
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
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
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
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children5ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children6ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 7)
                List<XElement> room7child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children7ages).ToList();
                }
                #endregion
                #endregion                

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
                                                string ratecomments1 = string.Empty;
                                                string ratecomments2 = string.Empty;
                                                string ratecomments3 = string.Empty;
                                                string ratecomments4 = string.Empty;
                                                string ratecomments5 = string.Empty;
                                                string ratecomments6 = string.Empty;
                                                string ratecomments7 = string.Empty;                                               
                                                try
                                                {
                                                    ratecomments1 = roomList1[m].Attribute("rateComments").Value;
                                                }
                                                catch { }
                                                try
                                                {
                                                    ratecomments2 = roomList2[n].Attribute("rateComments").Value;
                                                }
                                                catch { }
                                                try
                                                {
                                                    ratecomments3 = roomList3[o].Attribute("rateComments").Value;
                                                }
                                                catch { }
                                                try
                                                {
                                                    ratecomments4 = roomList4[p].Attribute("rateComments").Value;
                                                }
                                                catch { }
                                                try
                                                {
                                                    ratecomments5 = roomList5[q].Attribute("rateComments").Value;
                                                }
                                                catch { }
                                                try
                                                {
                                                    ratecomments6 = roomList6[r].Attribute("rateComments").Value;
                                                }
                                                catch { }
                                                try
                                                {
                                                    ratecomments7 = roomList7[s].Attribute("rateComments").Value;
                                                }
                                                catch { }
                                                
                                                str = str + " &amp;" + ratecomments1 + " &amp;" + ratecomments2 + " &amp;" + ratecomments3 + " &amp;" + ratecomments4 + " &amp;" + ratecomments5 + " &amp;" + ratecomments6 + " &amp;" + ratecomments7;
                                                return str;                                           
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
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
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
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children5ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children6ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 7)
                List<XElement> room7child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children7ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 8)
                List<XElement> room8child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children8ages).ToList();
                }
                #endregion
                #endregion                

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
                                                    string ratecomments1 = string.Empty;
                                                    string ratecomments2 = string.Empty;
                                                    string ratecomments3 = string.Empty;
                                                    string ratecomments4 = string.Empty;
                                                    string ratecomments5 = string.Empty;
                                                    string ratecomments6 = string.Empty;
                                                    string ratecomments7 = string.Empty;
                                                    string ratecomments8 = string.Empty;
                                                    try
                                                    {
                                                        ratecomments1 = roomList1[m].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments2 = roomList2[n].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments3 = roomList3[o].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments4 = roomList4[p].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments5 = roomList5[q].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments6 = roomList6[r].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments7 = roomList7[s].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments8 = roomList8[t].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                   
                                                    str = str + " &amp;" + ratecomments1 + " &amp;" + ratecomments2 + " &amp;" + ratecomments3 + " &amp;" + ratecomments4 + " &amp;" + ratecomments5 + " &amp;" + ratecomments6 + " &amp;" + ratecomments7 + " &amp;" + ratecomments8;
                                                    return str;                                                
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
                List<XElement> room1child = reqTravillio.Descendants("RoomPax").ToList();
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
                    List<XElement> childage = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravillio.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
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
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 5)
                List<XElement> room5child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children5ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 6)
                List<XElement> room6child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children6ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 7)
                List<XElement> room7child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children7ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 8)
                List<XElement> room8child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children8ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 9)
                List<XElement> room9child = reqTravillio.Descendants("RoomPax").ToList();
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
                    roomList9 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room9child[8].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
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
                    roomList9 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room9child[8].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children9ages).ToList();
                }
                #endregion
                #endregion

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
                                                    string ratecomments1 = string.Empty;
                                                    string ratecomments2 = string.Empty;
                                                    string ratecomments3 = string.Empty;
                                                    string ratecomments4 = string.Empty;
                                                    string ratecomments5 = string.Empty;
                                                    string ratecomments6 = string.Empty;
                                                    string ratecomments7 = string.Empty;
                                                    string ratecomments8 = string.Empty;
                                                    string ratecomments9 = string.Empty;
                                                    try
                                                    {
                                                        ratecomments1 = roomList1[m].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments2 = roomList2[n].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments3 = roomList3[o].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments4 = roomList4[p].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments5 = roomList5[q].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments6 = roomList6[r].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments7 = roomList7[s].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments8 = roomList8[t].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        ratecomments9 = roomList9[u].Attribute("rateComments").Value;
                                                    }
                                                    catch { }
                                                    str = str + " &amp;" + ratecomments1 + " &amp;" + ratecomments2 + " &amp;" + ratecomments3 + " &amp;" + ratecomments4 + " &amp;" + ratecomments5 + " &amp;" + ratecomments6 + " &amp;" + ratecomments7 + " &amp;" + ratecomments8 + " &amp;" + ratecomments9;
                                                    return str;
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
        #region HotelBeds Room's Price Breakups
        private IEnumerable<XElement> GetRoomsPriceBreakupHotelBeds(List<XElement> pricebreakups)
        {
            #region HotelBeds Room's Price Breakups

            List<XElement> str = new List<XElement>();
            try
            {
                //Parallel.For(0, pricebreakups.Count(), i =>
                for (int i = 0; i < pricebreakups.Count(); i++)
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                           new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].Attribute("dailyNet").Value)))
                    );
                };
                return str.OrderBy(x => (int)x.Attribute("Night")).ToList();
            }
            catch { return null; }

            #endregion
        }
        #endregion
        #region HotelBeds Room's Price Breakups (Self Defined)
        private IEnumerable<XElement> GetRoomsPriceBreakupHotelBedsTRV(decimal netamount)
        {
            #region HotelBeds Room's Price Breakups          
            decimal perdayprice = netamount/totalngt;
            List<XElement> str = new List<XElement>();
            try
            {
                //Parallel.For(0, totalngt, i =>
                for (int i = 0; i < totalngt; i ++)
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                           new XAttribute("PriceValue", Convert.ToString(perdayprice)))
                    );
                };
                return str.OrderBy(x => (int)x.Attribute("Night")).ToList();
            }
            catch { return null; }

            #endregion
        }
        #endregion
        #region HotelBeds's Room's Promotion
        private IEnumerable<XElement> GetHotelpromotionsHotelBeds(List<XElement> roompromotions)
        {

            Int32 length = roompromotions.Count();
            List<XElement> promotion = new List<XElement>();

            if (length == 0)
            {
                promotion.Add(new XElement("Promotions", ""));
            }
            else
            {

                Parallel.For(0, length, i =>
                {

                    promotion.Add(new XElement("Promotions", Convert.ToString(roompromotions[i].Value)));

                });
            }
            return promotion;
        }
        #endregion
        #region Room's Cancellation Policies from HotelBeds
        private IEnumerable<XElement> GetRoomCancellationPolicyHotelBeds(List<XElement> troom, string currencycode)
        {
            #region Room's Cancellation Policies from HotelBeds
            List<XElement> grhtrm = new List<XElement>();
            for (int i = 0; i < troom.Count(); i++)
            {
                List<XElement> cxlplc = troom[i].Descendants("cancellationPolicy").ToList();
                string chformat = cxlplc[0].Attribute("from").Value;
                string output = chformat.Substring(chformat.Length - 1, 1);
                string dtlastcxldate2 = string.Empty;
                if (output == "Z")
                {
                    dtlastcxldate2 = ((DateTimeOffset.ParseExact(cxlplc[0].Attribute("from").Value, "yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture).UtcDateTime).AddDays(-1).ToString("dd/MM/yyyy"));
                }
                else
                {
                    dtlastcxldate2 = ((DateTimeOffset.ParseExact(cxlplc[0].Attribute("from").Value, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture).UtcDateTime).AddDays(-1).ToString("dd/MM/yyyy"));
                } 
                List<XElement> htrm = new List<XElement>();
                htrm.Add(
                       new XElement("CancellationPolicy", ""
                       , new XAttribute("LastCancellationDate", Convert.ToString(dtlastcxldate2))
                       , new XAttribute("ApplicableAmount", "0.00")
                       , new XAttribute("NoShowPolicy", "0")));

                for (int j = 0; j < cxlplc.Count(); j++)
                {
                    string date2 = string.Empty;
                    if (output == "Z")
                    {
                        date2 = ((DateTimeOffset.ParseExact(cxlplc[j].Attribute("from").Value, "yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture).UtcDateTime).ToString("dd/MM/yyyy"));
                    }
                    else
                    {
                        date2 = ((DateTimeOffset.ParseExact(cxlplc[j].Attribute("from").Value, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture).UtcDateTime).ToString("dd/MM/yyyy"));
                    }
                    //string date2 = ((DateTimeOffset.ParseExact(cxlplc[j].Attribute("from").Value, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture).UtcDateTime).ToString("dd/MM/yyyy"));
                    string totamt = cxlplc[j].Attribute("amount").Value;

                    htrm.Add(
                        new XElement("CancellationPolicy", "Cancellation done on after " + date2 + "  will apply " + currencycode + " " + Convert.ToDouble(totamt) + "  Cancellation fee"
                        , new XAttribute("LastCancellationDate", Convert.ToString(date2))
                        , new XAttribute("ApplicableAmount", Convert.ToDouble(totamt))
                        , new XAttribute("NoShowPolicy", "0")));
                }
                grhtrm.Add(new XElement("Room", htrm));
            }
            XElement cxlfinal = MergCxlPolicy(grhtrm, currencycode);
            List<XElement> cxlfinalres = cxlfinal.Descendants("CancellationPolicy").ToList();
            return cxlfinalres;
            #endregion
        }
        #region Merge CXL Policy
        public XElement MergCxlPolicy(List<XElement> rooms, string currencycode)
        {
            List<XElement> policyList = new List<XElement>();



            IEnumerable<XElement> dateLst = rooms.Descendants("CancellationPolicy").
                Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0")).
                GroupBy(r => new { r.Attribute("LastCancellationDate").Value }).Select(y => y.First()).
                OrderBy(p => p.Attribute("LastCancellationDate").Value);
            if (dateLst.Count() > 0)
            {

                foreach (var item in dateLst)
                {
                    string date = item.Attribute("LastCancellationDate").Value;

                    decimal datePrice = 0.0m;
                    foreach (var rm in rooms)
                    {
                        var prItem = rm.Descendants("CancellationPolicy").
                            Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0" && pq.Attribute("LastCancellationDate").Value == date)).
                            FirstOrDefault();
                        if (prItem != null)
                        {
                            var price = prItem.Attribute("ApplicableAmount").Value;
                            datePrice += Convert.ToDecimal(price);
                        }
                        else
                        {

                            DateTime oDate = date.HotelsDate();

                            var lastItem = rm.Descendants("CancellationPolicy").
                                Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0" && pq.Attribute("LastCancellationDate").Value.HotelsDate() < oDate));

                            if (lastItem.Count() > 0)
                            {
                                var lastDate = lastItem.Max(y => y.Attribute("LastCancellationDate").Value);
                                var lastprice = rm.Descendants("CancellationPolicy").
                        Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0" && pq.Attribute("LastCancellationDate").Value == lastDate)).
                        FirstOrDefault().Attribute("ApplicableAmount").Value;
                                datePrice += Convert.ToDecimal(lastprice);
                            }
                        }
                    }
                    XElement pItem = new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", date), new XAttribute("ApplicableAmount", datePrice), new XAttribute("NoShowPolicy", "0"), "");
                    policyList.Add(pItem);

                }



                policyList = policyList.GroupBy(x => new { x.Attribute("LastCancellationDate").Value.HotelsDate().Date }).
                    Select(y => new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", y.Key.Date.ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("ApplicableAmount").Value))), new XAttribute("NoShowPolicy", "0"), "")).ToList();

            }
            var lastCxlDate = rooms.Descendants("CancellationPolicy").Where(pq => (pq.Attribute("ApplicableAmount").Value == "0.00" && pq.Attribute("NoShowPolicy").Value == "0")).Min(y => y.Attribute("LastCancellationDate").Value.HotelsDate().Date);
            policyList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", lastCxlDate.ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0"), "Cancellation done on before " + lastCxlDate.ToString("dd/MM/yyyy") + "  will apply " + currencycode + " " + 0 + "  Cancellation fee"));


            var NoShow = rooms.Descendants("CancellationPolicy").Where(pq => pq.Attribute("NoShowPolicy").Value == "1").GroupBy(r => new { r.Attribute("NoShowPolicy").Value }).Select(x => new { date = x.Min(y => y.Attribute("LastCancellationDate").Value.HotelsDate().Date), price = x.Sum(y => Convert.ToDecimal(y.Attribute("ApplicableAmount").Value)) });

            if (NoShow.Count() > 0)
            {
                var showItem = NoShow.FirstOrDefault();
                policyList.Add(new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", showItem.date.ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", showItem.price), new XAttribute("NoShowPolicy", "1"), ""));

            }
            XElement cxlItem = new XElement("CancellationPolicies", policyList);
            return cxlItem;
        }
        #endregion
        #region OLD Policy
        private IEnumerable<XElement> GetRoomCancellationPolicyHotelBedsOLD(List<XElement> cancellationpolicy, string currencycode)
        {
            #region Room's Cancellation Policies from HotelBeds
            List<XElement> htrm = new List<XElement>();

            var distinctcp = cancellationpolicy.GroupBy(x => x.Attribute("from").Value).ToList();

            #region Last CXL Date Policy
            XElement lastcxldate = cancellationpolicy.OrderBy(x => x.Attribute("from").Value).FirstOrDefault();
            string slastcxldate = lastcxldate.Attribute("from").Value;
            var dddtt = DateTimeOffset.ParseExact(slastcxldate, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
            DateTime ddlastcxldate = dddtt.UtcDateTime;
            //DateTime ddlastcxldate = DateTime.ParseExact(slastcxldate, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
            ddlastcxldate = ddlastcxldate.AddDays(-1);
            string dtlastcxldate = ddlastcxldate.ToString("dd/MM/yyyy");
            htrm.Add(new XElement("CancellationPolicy", "Cancellation done on before " + dtlastcxldate + "  will apply " + currencycode + " " + 0 + "  Cancellation fee"
                   , new XAttribute("LastCancellationDate", Convert.ToString(dtlastcxldate))
                   , new XAttribute("ApplicableAmount", "0")
                   , new XAttribute("NoShowPolicy", "0")));
            #endregion

            for (int i = 0; i < distinctcp.Count(); i++)
            {
                List<XElement> ff = distinctcp[i].ToList();
                decimal totamt = 0;
                for (int j = 0; j < ff.Count(); j++)
                {
                    decimal at = Convert.ToDecimal(ff[j].Attribute("amount").Value);
                    totamt = totamt + at;
                }
                var dtt = DateTimeOffset.ParseExact(distinctcp[i].Key, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
                DateTime fdate = dtt.UtcDateTime;
                //DateTime fdate = DateTime.ParseExact(distinctcp[i].Key, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
                string date2 = fdate.ToString("dd/MM/yyyy");


                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on after " + date2 + "  will apply " + currencycode + " " + totamt + "  Cancellation fee"
                    , new XAttribute("LastCancellationDate", Convert.ToString(date2))
                    , new XAttribute("ApplicableAmount", totamt)
                    , new XAttribute("NoShowPolicy", "0")));

            }

            return htrm;
            #endregion
        }
        #endregion
        #endregion
        #endregion
        #region Prebook Request
        private List<XElement> getroomkey(List<XElement> room)
        {
            #region Bind Room keys
            List<XElement> str = new List<XElement>();

            for (int i = 0; i < room.Count(); i++)
            {
                str.Add(new XElement("room",
                       new XAttribute("rateKey", Convert.ToString(room[i].Attribute("SessionID").Value)))
                );
            }
            return str;
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
    }
}