using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.Extranet
{
    public class ExtPreBook : IDisposable
    {
        XElement reqTravayoo;
        int totalngt = 1;
        string dmc = string.Empty;
        #region Price Check
        public static bool Check(decimal first, decimal second, decimal margin)
        {
            return Math.Abs(first - second) <= margin;
        }
        #endregion
        #region PreBooking of Extranet (XML OUT for Travayoo)
        public XElement PrebookingExtranet(XElement req, string xmlout)
        {
            reqTravayoo = req;
            XElement hotelprebookresponse = null;
            XElement hotelpreBooking = null;
            dmc = xmlout;
            try
            {
                HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
                #region Extranet Request/Response
                List<XElement> getroom = reqTravayoo.Descendants("Room").ToList();
                XElement occupancyrequest = new XElement(
                        new XElement("Keys", getroomkey(getroom)));
                string requestxml = string.Empty;
                #region Credentials
                string exAgentID = string.Empty;
                string exUserName = string.Empty;
                string exPassword = string.Empty;
                string exServiceType = string.Empty;
                string exServiceVersion = string.Empty;
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "3");
                exAgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
                exUserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
                exPassword = suppliercred.Descendants("Password").FirstOrDefault().Value;
                exServiceType = suppliercred.Descendants("ServiceType").FirstOrDefault().Value;
                exServiceVersion = suppliercred.Descendants("ServiceVersion").FirstOrDefault().Value;
                #endregion
                requestxml = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                              "<soapenv:Header xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                               "<Authentication>" +
                                 "<AgentID>" + exAgentID + "</AgentID>" +
                                  "<UserName>" + exUserName + "</UserName>" +
                                  "<Password>" + exPassword + "</Password>" +
                                  "<ServiceType>" + exServiceType + "</ServiceType>" +
                                  "<ServiceVersion>" + exServiceVersion + "</ServiceVersion>" +
                                "</Authentication>" +
                              "</soapenv:Header>" +
                              "<soapenv:Body>" +
                                "<prebookRequest>" +
                                  "<Response_Type>XML</Response_Type>" +
                                  "<CustomerID>" + reqTravayoo.Descendants("CustomerID").FirstOrDefault().Value + "</CustomerID>" +
                                  "<RequestID>" + reqTravayoo.Descendants("TransID").FirstOrDefault().Value + "</RequestID>" +
                                   "<FromDate>" + reqTravayoo.Descendants("FromDate").FirstOrDefault().Value + "</FromDate>" +
                                  "<ToDate>" + reqTravayoo.Descendants("ToDate").FirstOrDefault().Value + "</ToDate>" +
                                  "<PropertyId>" + reqTravayoo.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value + "</PropertyId>" +
                                  occupancyrequest +
                                  "<CultureId>1</CultureId>" +
                                  "<GuestNationalityId>" + reqTravayoo.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value + "</GuestNationalityId>" +
                                  reqTravayoo.Descendants("Rooms").SingleOrDefault().ToString() +
                                "</prebookRequest>" +
                              "</soapenv:Body>" +
                            "</soapenv:Envelope>";

                #endregion
                object result = extclient.GetPreBookRequestByXML(requestxml);
                if (result != null)
                {

                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(reqTravayoo.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = reqTravayoo.Descendants("TransID").Single().Value;
                        log.LogTypeID = 4;
                        log.LogType = "PreBook";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = result.ToString();
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "PrebookingExtranet";
                        ex1.PageName = "ExtPreBook";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    XElement doc = XElement.Parse(result.ToString());
                    List<XElement> hotelavailabilityres = doc.Descendants("Hotel").ToList();
                    hotelprebookresponse = GetHotelListExtranet(hotelavailabilityres).FirstOrDefault();
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
                else
                {
                    #region No Result
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(reqTravayoo.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = reqTravayoo.Descendants("TransID").Single().Value;
                        log.LogTypeID = 4;
                        log.LogType = "PreBook";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = result.ToString();
                        //APILog.SaveAPILogs(log);
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "PrebookingExtranet";
                        ex1.PageName = "ExtPreBook";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        //APILog.SendCustomExcepToDB(ex1);
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    #endregion
                }
                return hotelpreBooking;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PrebookingExtranet";
                ex1.PageName = "ExtPreBook";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                IEnumerable<XElement> request = req.Descendants("HotelPreBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                string username = req.Descendants("UserName").Single().Value;
                string password = req.Descendants("Password").Single().Value;
                string AgentID = req.Descendants("AgentID").Single().Value;
                string ServiceType = req.Descendants("ServiceType").Single().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
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
                       new XElement("HotelPreBookingResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));
                return searchdoc;
            }
        }
        #endregion
        #region Extranet
        #region Extranet Hotel Listing
        private IEnumerable<XElement> GetHotelListExtranet(List<XElement> htlist)
        {
            #region Extranet
            List<XElement> hotellst = new List<XElement>();
            Int32 length = htlist.Count();

            try
            {
                for (int i = 0; i < length; i++)
                {
                    string tandc = string.Empty;
                    try
                    {
                        tandc = htlist[i].Attribute("Term").Value;
                    }
                    catch { }
                    List<XElement> cxllst = htlist[i].Descendants("CancelPolicy").ToList();
                    hotellst.Add(new XElement("Hotel",
                                           new XElement("HotelID", Convert.ToString(htlist[i].Attribute("HotelID").Value)),
                                                       new XElement("HotelName", Convert.ToString(htlist[i].Attribute("HotelName").Value)),
                                                       new XElement("Status", "true"),
                                                       new XElement("TermCondition", tandc),
                                                       new XElement("HotelImgSmall", Convert.ToString("")),
                                                       new XElement("HotelImgLarge", Convert.ToString("")),
                                                       new XElement("MapLink", ""),
                                                       new XElement("DMC", dmc),
                                                       new XElement("Currency", Convert.ToString(htlist[i].Attribute("Currency").Value)),
                                                       new XElement("Offers", "")
                                                       , new XElement("Rooms",
                                                GetHtlRoomLstngExtranet(htlist[i].Descendants("Room").ToList(), cxllst)
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
        #region grouping Class
        public class roomgroup
        {
            public string Key { get; set; }
            public int Count { get; set; }

            public int allocation { get; set; }

            public string OR { get; set; }

            public List<string> rno { get; set; }
        }
        public class roomsss
        {
            public int rno { get; set; }
            public string rid { get; set; }
            public string risavail { get; set; }
        }
        #endregion
        #region Extranet Hotel's Room Listing
        public IEnumerable<XElement> GetHtlRoomLstngExtranet(List<XElement> roomlist, List<XElement> cxllist)
        {
            List<XElement> str = new List<XElement>();
            List<XElement> roomList1 = new List<XElement>();
            List<XElement> roomList2 = new List<XElement>();
            List<XElement> roomList3 = new List<XElement>();
            List<XElement> roomList4 = new List<XElement>();
            List<XElement> roomList5 = new List<XElement>();
            List<XElement> ttlroom = reqTravayoo.Descendants("RoomPax").ToList();
            int totalroom = Convert.ToInt32(reqTravayoo.Descendants("RoomPax").Count());

            #region Notes: The maximum number of rooms that can be retrieved by a single search request is nine (9)
            #endregion

            #region Room Count 1
            if (totalroom == 1)
            {
                #region Get Combination (Room 1)
                roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "1").ToList();
                #endregion
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    List<XElement> pricebrkups = roomList1[m].Descendants("Price").ToList();
                    List<XElement> promotions1 = roomList1[m].Descendants("pro").ToList();
                    int group = 0;
                    string isavailable = "false";
                    if (roomList1[m].Parent.Parent.Attribute("IsOnRequest").Value == "false" && Convert.ToInt32(roomList1[m].Parent.Parent.Attribute("CurrentAllo").Value) > 0)
                    {
                        isavailable = "true";
                    }
                    else if (roomList1[m].Parent.Parent.Attribute("IsOnRequest").Value == "false" && Convert.ToInt32(roomList1[m].Parent.Parent.Attribute("CurrentAllo").Value) == 0)
                    {
                        group = 1;
                    }
                    else if (roomList1[m].Parent.Parent.Attribute("IsOnRequest").Value == "true" && Convert.ToInt32(roomList1[m].Parent.Parent.Attribute("CurrentAllo").Value) > 0)
                    {
                        isavailable = "true";
                    }
                    if (group == 0)
                    {
                        #region With Board Bases
                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].Attribute("TotalRoomRate").Value), new XAttribute("Index", m + 1),
                            new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeId").Value)),
                                 new XAttribute("SuppliersID", "3"),
                                 new XAttribute("RoomSeq", "1"),
                                 new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("Key").Value)),
                                 new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeName").Value)),
                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                 new XAttribute("MealPlanID", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                 new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                 new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                 new XAttribute("MealPlanPrice", ""),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].Attribute("PricePerNight").Value)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("TotalRoomRate").Value)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                 new XAttribute("isAvailable", isavailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                 new XElement("PromotionList", GetHotelpromotionsExtranet(promotions1)),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements", Getsupplementsextranet(roomList1[m].Descendants("sup").ToList())),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups)),
                                     new XElement("AdultNum", Convert.ToString(ttlroom[0].Descendants("Adult").FirstOrDefault().Value)),
                                     new XElement("ChildNum", Convert.ToString(ttlroom[0].Descendants("Child").FirstOrDefault().Value))
                                 ),
                     new XElement("CancellationPolicies",
                 GetRoomCancellationPolicyExtranet(cxllist))
                 ));
                        #endregion
                    }
                }
                return str;
            }
            #endregion
            #region Room Count 2
            if (totalroom == 2)
            {
                List<roomsss> roomssss = new List<roomsss>() { new roomsss() { rno = 1, risavail = "false" }, new roomsss() { rno = 2, risavail = "false" } };
                #region Get Combination (Room 1)
                roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "1").ToList();
                #endregion
                #region Get Combination (Room 2)
                roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "2").ToList();
                #endregion
                int group = 0;
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        string bb1 = roomList1[m].Attribute("boardCode").Value;
                        string bb2 = roomList2[n].Attribute("boardCode").Value;
                        if (bb1 == bb2)
                        {
                            String condition = "";
                            List<string> ratekeylist = new List<string>();
                            ratekeylist.Add(roomList1[m].Parent.Parent.Attribute("RoomTypeId").Value);
                            ratekeylist.Add(roomList2[n].Parent.Parent.Attribute("RoomTypeId").Value);
                            int iii = 0;
                            foreach (var r in ratekeylist)
                            {
                                roomssss[iii].rid = r;
                                iii++;
                            }
                            var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new roomgroup
                            {
                                Key = ax.Key,
                                Count = ax.Count(),
                                allocation = roomlist.Where(x => x.Attribute("RoomTypeId").Value == ax.Key).Attributes("CurrentAllo").Sum(e => int.Parse(e.Value)),
                                OR = roomlist.Where(x => x.Attribute("RoomTypeId").Value == ax.Key).Attributes("IsOnRequest").FirstOrDefault().Value

                            }).ToList();
                            int k = 0;
                            foreach (var item in grouped)
                            {
                                var rtkey = item.Key;
                                var count = item.Count;
                                int totalt = roomlist.Where(x => x.Attribute("RoomTypeId").Value == rtkey).Attributes("CurrentAllo").Sum(e => int.Parse(e.Value));
                                item.allocation = totalt;
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
                            string r1isavail = "false";
                            int noset = 0;
                            if (_condition)
                            {
                                roomssss[0].risavail = "true";
                                roomssss[1].risavail = "true";
                            }
                            else
                            {
                                #region On Request
                                int _totalt = 0;
                                List<roomsss> new_roomssss = new List<roomsss>();
                                foreach (var item in roomssss)
                                {
                                    var r = item.rid;
                                    var items = grouped.Where(a => a.Key == r).FirstOrDefault();
                                    _totalt = items.allocation;
                                    if (_totalt <= 0)
                                    {
                                        string or = items.OR; ;
                                        if (or == "true")
                                            r1isavail = "false";
                                        else
                                            noset = 1;
                                    }
                                    else
                                    {
                                        items.allocation--;
                                        r1isavail = "true";
                                    }
                                    item.risavail = r1isavail;
                                }
                                #endregion
                            }
                            if (noset == 0)
                            {
                                //if (avail1 == avail2)
                                {
                                    List<XElement> pricebrkups1 = roomList1[m].Descendants("Price").ToList();
                                    List<XElement> pricebrkups2 = roomList2[n].Descendants("Price").ToList();
                                    List<XElement> promotions1 = roomList1[m].Descendants("pro").ToList();
                                    List<XElement> promotions2 = roomList2[n].Descendants("pro").ToList();
                                    #region Board Bases >0
                                    group++;
                                    decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList2[n].Attribute("TotalRoomRate").Value);
                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),
                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeId").Value)),
                                     new XAttribute("SuppliersID", "3"),
                                     new XAttribute("RoomSeq", "1"),
                                     new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("Key").Value)),
                                     new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeName").Value)),
                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                     new XAttribute("MealPlanID", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                     new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].Attribute("PricePerNight").Value)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("TotalRoomRate").Value)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", roomssss[0].risavail.ToString()),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList", GetHotelpromotionsExtranet(promotions1)),
                                        //new XElement("Promotions", Convert.ToString(promo1))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements", Getsupplementsextranet(roomList1[m].Descendants("sup").ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups1)),
                                         new XElement("AdultNum", Convert.ToString(ttlroom[0].Descendants("Adult").FirstOrDefault().Value)),
                                         new XElement("ChildNum", Convert.ToString(ttlroom[0].Descendants("Child").FirstOrDefault().Value))
                                     ),
                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("RoomTypeId").Value)),
                                     new XAttribute("SuppliersID", "3"),
                                     new XAttribute("RoomSeq", "2"),
                                     new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("Key").Value)),
                                     new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("RoomTypeName").Value)),
                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                     new XAttribute("MealPlanID", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                     new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].Attribute("PricePerNight").Value)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("TotalRoomRate").Value)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", roomssss[1].risavail.ToString()),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList", GetHotelpromotionsExtranet(promotions2)),
                                        //new XElement("Promotions", Convert.ToString(promo2))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements", Getsupplementsextranet(roomList2[n].Descendants("sup").ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups2)),
                                         new XElement("AdultNum", Convert.ToString(ttlroom[1].Descendants("Adult").FirstOrDefault().Value)),
                                         new XElement("ChildNum", Convert.ToString(ttlroom[1].Descendants("Child").FirstOrDefault().Value))
                                     ),
                     new XElement("CancellationPolicies",
                 GetRoomCancellationPolicyExtranet(cxllist))
                 ));
                                    #endregion
                                }
                            }
                        }
                    };
                };
                return str;
            }
            #endregion
            #region Room Count 3
            if (totalroom == 3)
            {
                List<roomsss> roomssss = new List<roomsss>() { new roomsss() { rno = 1, risavail = "false" }, new roomsss() { rno = 2, risavail = "false" }, new roomsss() { rno = 3, risavail = "false" } };
                #region Get Combination (Room 1)
                roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "1").ToList();
                #endregion
                #region Get Combination (Room 2)
                roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "2").ToList();
                #endregion
                #region Get Combination (Room 3)
                roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "3").ToList();
                #endregion
                int group = 0;
                #region Room 3
                //Parallel.For(0, roomList1.Count(), m =>
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    //Parallel.For(0, roomList2.Count(), n =>
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        //Parallel.For(0, roomList3.Count(), o =>
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            string bb1 = roomList1[m].Attribute("boardCode").Value;
                            string bb2 = roomList2[n].Attribute("boardCode").Value;
                            string bb3 = roomList3[o].Attribute("boardCode").Value;

                            //string avail1 = roomList1[m].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                            //string avail2 = roomList2[n].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                            //string avail3 = roomList3[o].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";

                            if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3)
                            {
                                String condition = "";
                                List<string> ratekeylist = new List<string>();
                                ratekeylist.Add(roomList1[m].Parent.Parent.Attribute("RoomTypeId").Value);
                                ratekeylist.Add(roomList2[n].Parent.Parent.Attribute("RoomTypeId").Value);
                                ratekeylist.Add(roomList3[o].Parent.Parent.Attribute("RoomTypeId").Value);
                                int iii = 0;
                                foreach (var r in ratekeylist)
                                {
                                    roomssss[iii].rid = r;
                                    iii++;
                                }
                                var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new roomgroup
                                {
                                    Key = ax.Key,
                                    Count = ax.Count(),
                                    allocation = roomlist.Where(x => x.Attribute("RoomTypeId").Value == ax.Key).Attributes("CurrentAllo").Sum(e => int.Parse(e.Value)),
                                    OR = roomlist.Where(x => x.Attribute("RoomTypeId").Value == ax.Key).Attributes("IsOnRequest").FirstOrDefault().Value

                                }).ToList();
                                int k = 0;
                                foreach (var item in grouped)
                                {
                                    var rtkey = item.Key;
                                    var count = item.Count;
                                    int totalt = roomlist.Where(x => x.Attribute("RoomTypeId").Value == rtkey).Attributes("CurrentAllo").Sum(e => int.Parse(e.Value));
                                    item.allocation = totalt;
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
                                string r1isavail = "false";
                                int noset = 0;
                                if (_condition)
                                {
                                    roomssss[0].risavail = "true";
                                    roomssss[1].risavail = "true";
                                    roomssss[2].risavail = "true";
                                }
                                else
                                {
                                    #region On Request
                                    int _totalt = 0;
                                    List<roomsss> new_roomssss = new List<roomsss>();
                                    foreach (var item in roomssss)
                                    {
                                        var r = item.rid;
                                        var items = grouped.Where(a => a.Key == r).FirstOrDefault();
                                        _totalt = items.allocation;
                                        if (_totalt <= 0)
                                        {
                                            string or = items.OR; ;
                                            if (or == "true")
                                                r1isavail = "false";
                                            else
                                                noset = 1;
                                        }
                                        else
                                        {
                                            items.allocation--;
                                            r1isavail = "true";
                                        }
                                        item.risavail = r1isavail;
                                    }
                                    #endregion
                                }
                                if (noset == 0)
                                {
                                    //if (avail1 == avail2 && avail2 == avail3 && avail1 == avail3)
                                    {
                                        #region room's group
                                        List<XElement> pricebrkups1 = roomList1[m].Descendants("Price").ToList();
                                        List<XElement> pricebrkups2 = roomList2[n].Descendants("Price").ToList();
                                        List<XElement> pricebrkups3 = roomList3[o].Descendants("Price").ToList();
                                        List<XElement> promotions1 = roomList1[m].Descendants("pro").ToList();
                                        List<XElement> promotions2 = roomList2[n].Descendants("pro").ToList();
                                        List<XElement> promotions3 = roomList3[o].Descendants("pro").ToList();
                                        group++;
                                        decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList2[n].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList3[o].Attribute("TotalRoomRate").Value);

                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeId").Value)),
                                         new XAttribute("SuppliersID", "3"),
                                         new XAttribute("RoomSeq", "1"),
                                         new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("Key").Value)),
                                         new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeName").Value)),
                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                         new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].Attribute("PricePerNight").Value)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("TotalRoomRate").Value)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomssss[0].risavail.ToString()),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList", GetHotelpromotionsExtranet(promotions1)),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements", Getsupplementsextranet(roomList1[m].Descendants("sup").ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups1)),
                                             new XElement("AdultNum", Convert.ToString(ttlroom[0].Descendants("Adult").FirstOrDefault().Value)),
                                             new XElement("ChildNum", Convert.ToString(ttlroom[0].Descendants("Child").FirstOrDefault().Value))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("RoomTypeId").Value)),
                                         new XAttribute("SuppliersID", "3"),
                                         new XAttribute("RoomSeq", "2"),
                                         new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("Key").Value)),
                                         new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("RoomTypeName").Value)),
                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                         new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].Attribute("PricePerNight").Value)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("TotalRoomRate").Value)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomssss[1].risavail.ToString()),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList", GetHotelpromotionsExtranet(promotions2)),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements", Getsupplementsextranet(roomList2[n].Descendants("sup").ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups2)),
                                             new XElement("AdultNum", Convert.ToString(ttlroom[1].Descendants("Adult").FirstOrDefault().Value)),
                                             new XElement("ChildNum", Convert.ToString(ttlroom[1].Descendants("Child").FirstOrDefault().Value))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("RoomTypeId").Value)),
                                         new XAttribute("SuppliersID", "3"),
                                         new XAttribute("RoomSeq", "3"),
                                         new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("Key").Value)),
                                         new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("RoomTypeName").Value)),
                                         new XAttribute("OccupancyID", Convert.ToString("")),
                                         new XAttribute("OccupancyName", Convert.ToString("")),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                         new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].Attribute("PricePerNight").Value)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("TotalRoomRate").Value)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomssss[2].risavail.ToString()),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList", GetHotelpromotionsExtranet(promotions3)),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements", Getsupplementsextranet(roomList3[o].Descendants("sup").ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups3)),
                                             new XElement("AdultNum", Convert.ToString(ttlroom[2].Descendants("Adult").FirstOrDefault().Value)),
                                             new XElement("ChildNum", Convert.ToString(ttlroom[2].Descendants("Child").FirstOrDefault().Value))
                                         ),
                     new XElement("CancellationPolicies",
                 GetRoomCancellationPolicyExtranet(cxllist))
                 ));
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
            #region Room Count 4
            if (totalroom == 4)
            {
                List<roomsss> roomssss = new List<roomsss>() { new roomsss() { rno = 1, risavail = "false" }, new roomsss() { rno = 2, risavail = "false" }, new roomsss() { rno = 3, risavail = "false" }, new roomsss() { rno = 4, risavail = "false" } };
                #region Get Combination (Room 1)
                roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "1").ToList();
                #endregion
                #region Get Combination (Room 2)
                roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "2").ToList();
                #endregion
                #region Get Combination (Room 3)
                roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "3").ToList();
                #endregion
                #region Get Combination (Room 4)
                roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "4").ToList();
                #endregion
                int group = 0;
                #region Room 4
                //Parallel.For(0, roomList1.Count(), m =>
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    //Parallel.For(0, roomList2.Count(), n =>
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        //Parallel.For(0, roomList3.Count(), o =>
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            //Parallel.For(0, roomList4.Count(), p =>
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                // add room 1, 2, 3,4

                                string bb1 = roomList1[m].Attribute("boardCode").Value;
                                string bb2 = roomList2[n].Attribute("boardCode").Value;
                                string bb3 = roomList3[o].Attribute("boardCode").Value;
                                string bb4 = roomList4[p].Attribute("boardCode").Value;
                                //string avail1 = roomList1[m].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                                //string avail2 = roomList2[n].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                                //string avail3 = roomList3[o].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                                //string avail4 = roomList4[p].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                                if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4)
                                {
                                    String condition = "";
                                    List<string> ratekeylist = new List<string>();
                                    ratekeylist.Add(roomList1[m].Parent.Parent.Attribute("RoomTypeId").Value);
                                    ratekeylist.Add(roomList2[n].Parent.Parent.Attribute("RoomTypeId").Value);
                                    ratekeylist.Add(roomList3[o].Parent.Parent.Attribute("RoomTypeId").Value);
                                    ratekeylist.Add(roomList4[p].Parent.Parent.Attribute("RoomTypeId").Value);
                                    int iii = 0;
                                    foreach (var r in ratekeylist)
                                    {
                                        roomssss[iii].rid = r;
                                        iii++;
                                    }
                                    var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new roomgroup
                                    {
                                        Key = ax.Key,
                                        Count = ax.Count(),
                                        allocation = roomlist.Where(x => x.Attribute("RoomTypeId").Value == ax.Key).Attributes("CurrentAllo").Sum(e => int.Parse(e.Value)),
                                        OR = roomlist.Where(x => x.Attribute("RoomTypeId").Value == ax.Key).Attributes("IsOnRequest").FirstOrDefault().Value

                                    }).ToList();
                                    int k = 0;
                                    foreach (var item in grouped)
                                    {
                                        var rtkey = item.Key;
                                        var count = item.Count;
                                        int totalt = roomlist.Where(x => x.Attribute("RoomTypeId").Value == rtkey).Attributes("CurrentAllo").Sum(e => int.Parse(e.Value));
                                        item.allocation = totalt;
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
                                    string r1isavail = "false";
                                    int noset = 0;
                                    if (_condition)
                                    {
                                        roomssss[0].risavail = "true";
                                        roomssss[1].risavail = "true";
                                        roomssss[2].risavail = "true";
                                        roomssss[3].risavail = "true";
                                    }
                                    else
                                    {
                                        #region On Request
                                        int _totalt = 0;
                                        List<roomsss> new_roomssss = new List<roomsss>();
                                        foreach (var item in roomssss)
                                        {
                                            var r = item.rid;
                                            var items = grouped.Where(a => a.Key == r).FirstOrDefault();
                                            _totalt = items.allocation;
                                            if (_totalt <= 0)
                                            {
                                                string or = items.OR; ;
                                                if (or == "true")
                                                    r1isavail = "false";
                                                else
                                                    noset = 1;
                                            }
                                            else
                                            {
                                                items.allocation--;
                                                r1isavail = "true";
                                            }
                                            item.risavail = r1isavail;
                                        }
                                        #endregion
                                    }
                                    if (noset == 0)
                                    {
                                        //if (avail1 == avail2 && avail2 == avail3 && avail1 == avail3 && avail1 == avail4 && avail2 == avail4 && avail3 == avail4)
                                        {
                                            #region room's group
                                            List<XElement> pricebrkups1 = roomList1[m].Descendants("Price").ToList();
                                            List<XElement> pricebrkups2 = roomList2[n].Descendants("Price").ToList();
                                            List<XElement> pricebrkups3 = roomList3[o].Descendants("Price").ToList();
                                            List<XElement> pricebrkups4 = roomList4[p].Descendants("Price").ToList();
                                            List<XElement> promotions1 = roomList1[m].Descendants("pro").ToList();
                                            List<XElement> promotions2 = roomList2[n].Descendants("pro").ToList();
                                            List<XElement> promotions3 = roomList3[o].Descendants("pro").ToList();
                                            List<XElement> promotions4 = roomList4[p].Descendants("pro").ToList();
                                            group++;
                                            decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList2[n].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList3[o].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList4[p].Attribute("TotalRoomRate").Value);

                                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeId").Value)),
                                             new XAttribute("SuppliersID", "3"),
                                             new XAttribute("RoomSeq", "1"),
                                             new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("Key").Value)),
                                             new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeName").Value)),
                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                             new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].Attribute("PricePerNight").Value)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("TotalRoomRate").Value)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomssss[0].risavail.ToString()),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList", GetHotelpromotionsExtranet(promotions1)),
                                                //new XElement("Promotions", Convert.ToString(promo1))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements", Getsupplementsextranet(roomList1[m].Descendants("sup").ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups1)),
                                                 new XElement("AdultNum", Convert.ToString(ttlroom[0].Descendants("Adult").FirstOrDefault().Value)),
                                                 new XElement("ChildNum", Convert.ToString(ttlroom[0].Descendants("Child").FirstOrDefault().Value))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("RoomTypeId").Value)),
                                             new XAttribute("SuppliersID", "3"),
                                             new XAttribute("RoomSeq", "2"),
                                             new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("Key").Value)),
                                             new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("RoomTypeName").Value)),
                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                             new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].Attribute("PricePerNight").Value)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("TotalRoomRate").Value)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomssss[1].risavail.ToString()),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList", GetHotelpromotionsExtranet(promotions2)),
                                                //new XElement("Promotions", Convert.ToString(promo2))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements", Getsupplementsextranet(roomList2[n].Descendants("sup").ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups2)),
                                                 new XElement("AdultNum", Convert.ToString(ttlroom[1].Descendants("Adult").FirstOrDefault().Value)),
                                                 new XElement("ChildNum", Convert.ToString(ttlroom[1].Descendants("Child").FirstOrDefault().Value))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("RoomTypeId").Value)),
                                             new XAttribute("SuppliersID", "3"),
                                             new XAttribute("RoomSeq", "3"),
                                             new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("Key").Value)),
                                             new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("RoomTypeName").Value)),
                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                             new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].Attribute("PricePerNight").Value)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("TotalRoomRate").Value)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomssss[2].risavail.ToString()),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList", GetHotelpromotionsExtranet(promotions3)),
                                                // new XElement("Promotions", Convert.ToString(promo3))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements", Getsupplementsextranet(roomList3[o].Descendants("sup").ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups3)),
                                                 new XElement("AdultNum", Convert.ToString(ttlroom[2].Descendants("Adult").FirstOrDefault().Value)),
                                                 new XElement("ChildNum", Convert.ToString(ttlroom[2].Descendants("Child").FirstOrDefault().Value))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomList4[p].Parent.Parent.Attribute("RoomTypeId").Value)),
                                             new XAttribute("SuppliersID", "3"),
                                             new XAttribute("RoomSeq", "4"),
                                             new XAttribute("SessionID", Convert.ToString(roomList4[p].Attribute("Key").Value)),
                                             new XAttribute("RoomType", Convert.ToString(roomList4[p].Parent.Parent.Attribute("RoomTypeName").Value)),
                                             new XAttribute("OccupancyID", Convert.ToString("")),
                                             new XAttribute("OccupancyName", Convert.ToString("")),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList4[p].Attribute("boardName").Value)),
                                             new XAttribute("MealPlanCode", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].Attribute("PricePerNight").Value)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].Attribute("TotalRoomRate").Value)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomssss[3].risavail.ToString()),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList", GetHotelpromotionsExtranet(promotions4)),
                                                // new XElement("Promotions", Convert.ToString(promo4))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements", Getsupplementsextranet(roomList4[p].Descendants("sup").ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups4)),
                                                 new XElement("AdultNum", Convert.ToString(ttlroom[3].Descendants("Adult").FirstOrDefault().Value)),
                                                 new XElement("ChildNum", Convert.ToString(ttlroom[3].Descendants("Child").FirstOrDefault().Value))
                                             ),
                     new XElement("CancellationPolicies",
                 GetRoomCancellationPolicyExtranet(cxllist))
                 ));
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
            #region Room Count 5
            if (totalroom == 5)
            {
                List<roomsss> roomssss = new List<roomsss>() { new roomsss() { rno = 1, risavail = "false" }, new roomsss() { rno = 2, risavail = "false" }, new roomsss() { rno = 3, risavail = "false" }, new roomsss() { rno = 4, risavail = "false" }, new roomsss() { rno = 5, risavail = "false" } };
                #region Get Combination (Room 1)
                roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "1").ToList();
                #endregion
                #region Get Combination (Room 2)
                roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "2").ToList();
                #endregion
                #region Get Combination (Room 3)
                roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "3").ToList();
                #endregion
                #region Get Combination (Room 4)
                roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "4").ToList();
                #endregion
                #region Get Combination (Room 5)
                roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "5").ToList();
                #endregion
                int group = 0;
                #region Room 5
                //Parallel.For(0, roomList1.Count(), m =>
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    //Parallel.For(0, roomList2.Count(), n =>
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        //Parallel.For(0, roomList3.Count(), o =>
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            //Parallel.For(0, roomList4.Count(), p =>
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
                                    //string avail1 = roomList1[m].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                                    //string avail2 = roomList2[n].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                                    //string avail3 = roomList3[o].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                                    //string avail4 = roomList4[p].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                                    if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4
                                        && bb1 == bb5 && bb2 == bb5 && bb3 == bb5 && bb4 == bb5)
                                    {
                                        String condition = "";
                                        List<string> ratekeylist = new List<string>();
                                        ratekeylist.Add(roomList1[m].Parent.Parent.Attribute("RoomTypeId").Value);
                                        ratekeylist.Add(roomList2[n].Parent.Parent.Attribute("RoomTypeId").Value);
                                        ratekeylist.Add(roomList3[o].Parent.Parent.Attribute("RoomTypeId").Value);
                                        ratekeylist.Add(roomList4[p].Parent.Parent.Attribute("RoomTypeId").Value);
                                        ratekeylist.Add(roomList5[q].Parent.Parent.Attribute("RoomTypeId").Value);
                                        int iii = 0;
                                        foreach (var r in ratekeylist)
                                        {
                                            roomssss[iii].rid = r;
                                            iii++;
                                        }
                                        var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new roomgroup
                                        {
                                            Key = ax.Key,
                                            Count = ax.Count(),
                                            allocation = roomlist.Where(x => x.Attribute("RoomTypeId").Value == ax.Key).Attributes("CurrentAllo").Sum(e => int.Parse(e.Value)),
                                            OR = roomlist.Where(x => x.Attribute("RoomTypeId").Value == ax.Key).Attributes("IsOnRequest").FirstOrDefault().Value

                                        }).ToList();
                                        int k = 0;
                                        foreach (var item in grouped)
                                        {
                                            var rtkey = item.Key;
                                            var count = item.Count;
                                            int totalt = roomlist.Where(x => x.Attribute("RoomTypeId").Value == rtkey).Attributes("CurrentAllo").Sum(e => int.Parse(e.Value));
                                            item.allocation = totalt;
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
                                        string r1isavail = "false";
                                        int noset = 0;
                                        if (_condition)
                                        {
                                            roomssss[0].risavail = "true";
                                            roomssss[1].risavail = "true";
                                            roomssss[2].risavail = "true";
                                            roomssss[3].risavail = "true";
                                            roomssss[4].risavail = "true";
                                        }
                                        else
                                        {
                                            #region On Request
                                            int _totalt = 0;
                                            List<roomsss> new_roomssss = new List<roomsss>();
                                            foreach (var item in roomssss)
                                            {
                                                var r = item.rid;
                                                var items = grouped.Where(a => a.Key == r).FirstOrDefault();
                                                _totalt = items.allocation;
                                                if (_totalt <= 0)
                                                {
                                                    string or = items.OR; ;
                                                    if (or == "true")
                                                        r1isavail = "false";
                                                    else
                                                        noset = 1;
                                                }
                                                else
                                                {
                                                    items.allocation--;
                                                    r1isavail = "true";
                                                }
                                                item.risavail = r1isavail;
                                            }
                                            #endregion
                                        }
                                        if (noset == 0)
                                        {
                                            //if (avail1 == avail2 && avail2 == avail3 && avail1 == avail3 && avail1 == avail4 && avail2 == avail4 && avail3 == avail4)
                                            {
                                                #region room's group
                                                List<XElement> pricebrkups1 = roomList1[m].Descendants("Price").ToList();
                                                List<XElement> pricebrkups2 = roomList2[n].Descendants("Price").ToList();
                                                List<XElement> pricebrkups3 = roomList3[o].Descendants("Price").ToList();
                                                List<XElement> pricebrkups4 = roomList4[p].Descendants("Price").ToList();
                                                List<XElement> pricebrkups5 = roomList5[q].Descendants("Price").ToList();
                                                List<XElement> promotions1 = roomList1[m].Descendants("pro").ToList();
                                                List<XElement> promotions2 = roomList2[n].Descendants("pro").ToList();
                                                List<XElement> promotions3 = roomList3[o].Descendants("pro").ToList();
                                                List<XElement> promotions4 = roomList4[p].Descendants("pro").ToList();
                                                List<XElement> promotions5 = roomList5[q].Descendants("pro").ToList();
                                                group++;
                                                decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList2[n].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList3[o].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList4[p].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList5[q].Attribute("TotalRoomRate").Value);

                                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeId").Value)),
                                                 new XAttribute("SuppliersID", "3"),
                                                 new XAttribute("RoomSeq", "1"),
                                                 new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("Key").Value)),
                                                 new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeName").Value)),
                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].Attribute("PricePerNight").Value)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("TotalRoomRate").Value)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomssss[0].risavail.ToString()),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList", GetHotelpromotionsExtranet(promotions1)),
                                                    //new XElement("Promotions", Convert.ToString(promo1))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements", Getsupplementsextranet(roomList1[m].Descendants("sup").ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups1)),
                                                     new XElement("AdultNum", Convert.ToString(ttlroom[0].Descendants("Adult").FirstOrDefault().Value)),
                                                     new XElement("ChildNum", Convert.ToString(ttlroom[0].Descendants("Child").FirstOrDefault().Value))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("RoomTypeId").Value)),
                                                 new XAttribute("SuppliersID", "3"),
                                                 new XAttribute("RoomSeq", "2"),
                                                 new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("Key").Value)),
                                                 new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("RoomTypeName").Value)),
                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].Attribute("PricePerNight").Value)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("TotalRoomRate").Value)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomssss[1].risavail.ToString()),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList", GetHotelpromotionsExtranet(promotions2)),
                                                    //new XElement("Promotions", Convert.ToString(promo2))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements", Getsupplementsextranet(roomList2[n].Descendants("sup").ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups2)),
                                                     new XElement("AdultNum", Convert.ToString(ttlroom[1].Descendants("Adult").FirstOrDefault().Value)),
                                                     new XElement("ChildNum", Convert.ToString(ttlroom[1].Descendants("Child").FirstOrDefault().Value))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("RoomTypeId").Value)),
                                                 new XAttribute("SuppliersID", "3"),
                                                 new XAttribute("RoomSeq", "3"),
                                                 new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("Key").Value)),
                                                 new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("RoomTypeName").Value)),
                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].Attribute("PricePerNight").Value)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("TotalRoomRate").Value)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomssss[2].risavail.ToString()),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList", GetHotelpromotionsExtranet(promotions3)),
                                                    // new XElement("Promotions", Convert.ToString(promo3))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements", Getsupplementsextranet(roomList3[o].Descendants("sup").ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups3)),
                                                     new XElement("AdultNum", Convert.ToString(ttlroom[2].Descendants("Adult").FirstOrDefault().Value)),
                                                     new XElement("ChildNum", Convert.ToString(ttlroom[2].Descendants("Child").FirstOrDefault().Value))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomList4[p].Parent.Parent.Attribute("RoomTypeId").Value)),
                                                 new XAttribute("SuppliersID", "3"),
                                                 new XAttribute("RoomSeq", "4"),
                                                 new XAttribute("SessionID", Convert.ToString(roomList4[p].Attribute("Key").Value)),
                                                 new XAttribute("RoomType", Convert.ToString(roomList4[p].Parent.Parent.Attribute("RoomTypeName").Value)),
                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList4[p].Attribute("boardName").Value)),
                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].Attribute("PricePerNight").Value)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].Attribute("TotalRoomRate").Value)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomssss[3].risavail.ToString()),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList", GetHotelpromotionsExtranet(promotions4)),
                                                    // new XElement("Promotions", Convert.ToString(promo4))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements", Getsupplementsextranet(roomList4[p].Descendants("sup").ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups4)),
                                                     new XElement("AdultNum", Convert.ToString(ttlroom[3].Descendants("Adult").FirstOrDefault().Value)),
                                                     new XElement("ChildNum", Convert.ToString(ttlroom[3].Descendants("Child").FirstOrDefault().Value))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomList5[q].Parent.Parent.Attribute("RoomTypeId").Value)),
                                                 new XAttribute("SuppliersID", "3"),
                                                 new XAttribute("RoomSeq", "5"),
                                                 new XAttribute("SessionID", Convert.ToString(roomList5[q].Attribute("Key").Value)),
                                                 new XAttribute("RoomType", Convert.ToString(roomList5[q].Parent.Parent.Attribute("RoomTypeName").Value)),
                                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList5[q].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList5[q].Attribute("boardName").Value)),
                                                 new XAttribute("MealPlanCode", Convert.ToString(roomList5[q].Attribute("boardCode").Value)),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].Attribute("PricePerNight").Value)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].Attribute("TotalRoomRate").Value)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomssss[4].risavail.ToString()),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList", GetHotelpromotionsExtranet(promotions5)),
                                                    // new XElement("Promotions", Convert.ToString(promo4))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements", Getsupplementsextranet(roomList5[q].Descendants("sup").ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups5)),
                                                     new XElement("AdultNum", Convert.ToString(ttlroom[4].Descendants("Adult").FirstOrDefault().Value)),
                                                     new XElement("ChildNum", Convert.ToString(ttlroom[4].Descendants("Child").FirstOrDefault().Value))
                                                 ),
                         new XElement("CancellationPolicies",
                     GetRoomCancellationPolicyExtranet(cxllist))
                     ));
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
            #region Room Count 6
            if (totalroom == 6)
            {
                str = room6grp(roomlist, cxllist);
            }
            #endregion
            return str;
        }
        #endregion
        #region Room's Grouping (6)
        private List<XElement> room6grp(List<XElement> roomlist, List<XElement> cxllist)
        {
            List<XElement> str = new List<XElement>();
            List<XElement> roomList1 = new List<XElement>();
            List<XElement> roomList2 = new List<XElement>();
            List<XElement> roomList3 = new List<XElement>();
            List<XElement> roomList4 = new List<XElement>();
            List<XElement> roomList5 = new List<XElement>();
            List<XElement> roomList6 = new List<XElement>();
            List<XElement> ttlroom = reqTravayoo.Descendants("RoomPax").ToList();
            try
            {
                List<roomsss> roomssss = new List<roomsss>() { new roomsss() { rno = 1, risavail = "false" }, new roomsss() { rno = 2, risavail = "false" }, new roomsss() { rno = 3, risavail = "false" }, new roomsss() { rno = 4, risavail = "false" }, new roomsss() { rno = 5, risavail = "false" }, new roomsss() { rno = 6, risavail = "false" } };
                #region Get Combination (Room 1)
                roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "1").ToList();
                #endregion
                #region Get Combination (Room 2)
                roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "2").ToList();
                #endregion
                #region Get Combination (Room 3)
                roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "3").ToList();
                #endregion
                #region Get Combination (Room 4)
                roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "4").ToList();
                #endregion
                #region Get Combination (Room 5)
                roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "5").ToList();
                #endregion
                #region Get Combination (Room 6)
                roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("roomNo").Value == "6").ToList();
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
                                            && bb1 == bb6 && bb2 == bb6 && bb3 == bb6 && bb4 == bb6)
                                        {
                                            String condition = "";
                                            List<string> ratekeylist = new List<string>();
                                            ratekeylist.Add(roomList1[m].Parent.Parent.Attribute("RoomTypeId").Value);
                                            ratekeylist.Add(roomList2[n].Parent.Parent.Attribute("RoomTypeId").Value);
                                            ratekeylist.Add(roomList3[o].Parent.Parent.Attribute("RoomTypeId").Value);
                                            ratekeylist.Add(roomList4[p].Parent.Parent.Attribute("RoomTypeId").Value);
                                            ratekeylist.Add(roomList5[q].Parent.Parent.Attribute("RoomTypeId").Value);
                                            ratekeylist.Add(roomList6[r].Parent.Parent.Attribute("RoomTypeId").Value);
                                            int iii = 0;
                                            foreach (var rr in ratekeylist)
                                            {
                                                roomssss[iii].rid = rr;
                                                iii++;
                                            }
                                            var grouped = ratekeylist.GroupBy(ss => ss).Select(ax => new roomgroup
                                            {
                                                Key = ax.Key,
                                                Count = ax.Count(),
                                                allocation = roomlist.Where(x => x.Attribute("RoomTypeId").Value == ax.Key).Attributes("CurrentAllo").Sum(e => int.Parse(e.Value)),
                                                OR = roomlist.Where(x => x.Attribute("RoomTypeId").Value == ax.Key).Attributes("IsOnRequest").FirstOrDefault().Value

                                            }).ToList();
                                            int k = 0;
                                            foreach (var item in grouped)
                                            {
                                                var rtkey = item.Key;
                                                var count = item.Count;
                                                int totalt = roomlist.Where(x => x.Attribute("RoomTypeId").Value == rtkey).Attributes("CurrentAllo").Sum(e => int.Parse(e.Value));
                                                item.allocation = totalt;
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
                                            string r1isavail = "false";
                                            int noset = 0;
                                            if (_condition)
                                            {
                                                roomssss[0].risavail = "true";
                                                roomssss[1].risavail = "true";
                                                roomssss[2].risavail = "true";
                                                roomssss[3].risavail = "true";
                                                roomssss[4].risavail = "true";
                                                roomssss[5].risavail = "true";
                                            }
                                            else
                                            {
                                                #region On Request
                                                int _totalt = 0;
                                                List<roomsss> new_roomssss = new List<roomsss>();
                                                foreach (var item in roomssss)
                                                {
                                                    var rr = item.rid;
                                                    var items = grouped.Where(a => a.Key == rr).FirstOrDefault();
                                                    _totalt = items.allocation;
                                                    if (_totalt <= 0)
                                                    {
                                                        string or = items.OR; ;
                                                        if (or == "true")
                                                            r1isavail = "false";
                                                        else
                                                            noset = 1;
                                                    }
                                                    else
                                                    {
                                                        items.allocation--;
                                                        r1isavail = "true";
                                                    }
                                                    item.risavail = r1isavail;
                                                }
                                                #endregion
                                            }
                                            if (noset == 0)
                                            {
                                                //if (avail1 == avail2 && avail2 == avail3 && avail1 == avail3 && avail1 == avail4 && avail2 == avail4 && avail3 == avail4)
                                                {
                                                    #region room's group
                                                    List<XElement> pricebrkups1 = roomList1[m].Descendants("Price").ToList();
                                                    List<XElement> pricebrkups2 = roomList2[n].Descendants("Price").ToList();
                                                    List<XElement> pricebrkups3 = roomList3[o].Descendants("Price").ToList();
                                                    List<XElement> pricebrkups4 = roomList4[p].Descendants("Price").ToList();
                                                    List<XElement> pricebrkups5 = roomList5[q].Descendants("Price").ToList();
                                                    List<XElement> pricebrkups6 = roomList6[r].Descendants("Price").ToList();
                                                    List<XElement> promotions1 = roomList1[m].Descendants("pro").ToList();
                                                    List<XElement> promotions2 = roomList2[n].Descendants("pro").ToList();
                                                    List<XElement> promotions3 = roomList3[o].Descendants("pro").ToList();
                                                    List<XElement> promotions4 = roomList4[p].Descendants("pro").ToList();
                                                    List<XElement> promotions5 = roomList5[q].Descendants("pro").ToList();
                                                    List<XElement> promotions6 = roomList6[r].Descendants("pro").ToList();
                                                    group++;
                                                    decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList2[n].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList3[o].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList4[p].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList5[q].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList6[r].Attribute("TotalRoomRate").Value);

                                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeId").Value)),
                                                     new XAttribute("SuppliersID", "3"),
                                                     new XAttribute("RoomSeq", "1"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("Key").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("RoomTypeName").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].Attribute("PricePerNight").Value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("TotalRoomRate").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomssss[0].risavail.ToString()),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsExtranet(promotions1)),
                                                        //new XElement("Promotions", Convert.ToString(promo1))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", Getsupplementsextranet(roomList1[m].Descendants("sup").ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups1)),
                                                         new XElement("AdultNum", Convert.ToString(ttlroom[0].Descendants("Adult").FirstOrDefault().Value)),
                                                         new XElement("ChildNum", Convert.ToString(ttlroom[0].Descendants("Child").FirstOrDefault().Value))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("RoomTypeId").Value)),
                                                     new XAttribute("SuppliersID", "3"),
                                                     new XAttribute("RoomSeq", "2"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("Key").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("RoomTypeName").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].Attribute("PricePerNight").Value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("TotalRoomRate").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomssss[1].risavail.ToString()),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsExtranet(promotions2)),
                                                        //new XElement("Promotions", Convert.ToString(promo2))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", Getsupplementsextranet(roomList2[n].Descendants("sup").ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups2)),
                                                         new XElement("AdultNum", Convert.ToString(ttlroom[1].Descendants("Adult").FirstOrDefault().Value)),
                                                         new XElement("ChildNum", Convert.ToString(ttlroom[1].Descendants("Child").FirstOrDefault().Value))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("RoomTypeId").Value)),
                                                     new XAttribute("SuppliersID", "3"),
                                                     new XAttribute("RoomSeq", "3"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("Key").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("RoomTypeName").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].Attribute("PricePerNight").Value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("TotalRoomRate").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomssss[2].risavail.ToString()),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsExtranet(promotions3)),
                                                        // new XElement("Promotions", Convert.ToString(promo3))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", Getsupplementsextranet(roomList3[o].Descendants("sup").ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups3)),
                                                         new XElement("AdultNum", Convert.ToString(ttlroom[2].Descendants("Adult").FirstOrDefault().Value)),
                                                         new XElement("ChildNum", Convert.ToString(ttlroom[2].Descendants("Child").FirstOrDefault().Value))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList4[p].Parent.Parent.Attribute("RoomTypeId").Value)),
                                                     new XAttribute("SuppliersID", "3"),
                                                     new XAttribute("RoomSeq", "4"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList4[p].Attribute("Key").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList4[p].Parent.Parent.Attribute("RoomTypeName").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList4[p].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].Attribute("PricePerNight").Value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].Attribute("TotalRoomRate").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomssss[3].risavail.ToString()),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsExtranet(promotions4)),
                                                        // new XElement("Promotions", Convert.ToString(promo4))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", Getsupplementsextranet(roomList4[p].Descendants("sup").ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups4)),
                                                         new XElement("AdultNum", Convert.ToString(ttlroom[3].Descendants("Adult").FirstOrDefault().Value)),
                                                         new XElement("ChildNum", Convert.ToString(ttlroom[3].Descendants("Child").FirstOrDefault().Value))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList5[q].Parent.Parent.Attribute("RoomTypeId").Value)),
                                                     new XAttribute("SuppliersID", "3"),
                                                     new XAttribute("RoomSeq", "5"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList5[q].Attribute("Key").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList5[q].Parent.Parent.Attribute("RoomTypeName").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList5[q].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList5[q].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList5[q].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].Attribute("PricePerNight").Value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].Attribute("TotalRoomRate").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomssss[4].risavail.ToString()),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsExtranet(promotions5)),
                                                        // new XElement("Promotions", Convert.ToString(promo4))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", Getsupplementsextranet(roomList5[q].Descendants("sup").ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups5)),
                                                         new XElement("AdultNum", Convert.ToString(ttlroom[4].Descendants("Adult").FirstOrDefault().Value)),
                                                         new XElement("ChildNum", Convert.ToString(ttlroom[4].Descendants("Child").FirstOrDefault().Value))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomList6[r].Parent.Parent.Attribute("RoomTypeId").Value)),
                                                     new XAttribute("SuppliersID", "3"),
                                                     new XAttribute("RoomSeq", "6"),
                                                     new XAttribute("SessionID", Convert.ToString(roomList6[r].Attribute("Key").Value)),
                                                     new XAttribute("RoomType", Convert.ToString(roomList6[r].Parent.Parent.Attribute("RoomTypeName").Value)),
                                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList6[r].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList6[r].Attribute("boardName").Value)),
                                                     new XAttribute("MealPlanCode", Convert.ToString(roomList6[r].Attribute("boardCode").Value)),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].Attribute("PricePerNight").Value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].Attribute("TotalRoomRate").Value)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomssss[5].risavail.ToString()),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList", GetHotelpromotionsExtranet(promotions6)),
                                                        // new XElement("Promotions", Convert.ToString(promo4))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements", Getsupplementsextranet(roomList6[r].Descendants("sup").ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupExtranet(pricebrkups6)),
                                                         new XElement("AdultNum", Convert.ToString(ttlroom[5].Descendants("Adult").FirstOrDefault().Value)),
                                                         new XElement("ChildNum", Convert.ToString(ttlroom[5].Descendants("Child").FirstOrDefault().Value))
                                                     ),
                             new XElement("CancellationPolicies",
                         GetRoomCancellationPolicyExtranet(cxllist))
                         ));
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
            catch { return null; }
        }
        #endregion
        #region Hotel Facilities Extranet
        public IEnumerable<XElement> hotelfacilitiesExtranet(List<XElement> facility)
        {
            Int32 length = 0;
            if (facility != null)
            {
                length = facility.Count();
            }
            List<XElement> fac = new List<XElement>();

            if (length == 0)
            {
                try
                {
                    fac.Add(new XElement("Facility", "No Facility Available"));
                }
                catch { }
            }
            else
            {
                Parallel.For(0, length, i =>
                {
                    try
                    {
                        fac.Add(new XElement("Facility", Convert.ToString(facility[i].Value)));
                    }
                    catch { }

                });
            }
            return fac;
        }
        #endregion
        #region Extranet's Room's Promotion
        private IEnumerable<XElement> GetHotelpromotionsExtranet(List<XElement> roompromotions)
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
        #region Extranet Room's Price Breakups
        private IEnumerable<XElement> GetRoomsPriceBreakupExtranet(List<XElement> pricebreakups)
        {
            #region Extranet Room's Price Breakups

            List<XElement> str = new List<XElement>();
            try
            {
                for (int i = 0; i < pricebreakups.Count(); i++)
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                           new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].Attribute("Total").Value)),
                           new XAttribute("MarkUp", Convert.ToString(pricebreakups[i].Attribute("MarkUp").Value)),
                           new XAttribute("MarkUpType", Convert.ToString(pricebreakups[i].Attribute("MarkUpType").Value)),
                           new XAttribute("MarkUpValue", Convert.ToString(pricebreakups[i].Attribute("MarkUpValue").Value)),
                           new XAttribute("DescSupply", Convert.ToString(pricebreakups[i].Attribute("DescSupply").Value)),
                           new XAttribute("DescDiscount", Convert.ToString(pricebreakups[i].Attribute("DescDiscount").Value)),
                           new XAttribute("DescPrice", Convert.ToString(pricebreakups[i].Attribute("DescPrice").Value)))
                    );
                }
                return str.OrderBy(x => (int)x.Attribute("Night")).ToList();
            }
            catch { return null; }

            #endregion
        }
        #endregion
        #region Room's Cancellation Policies from Extranet
        private IEnumerable<XElement> GetRoomCancellationPolicyExtranet(List<XElement> cancellationpolicy)
        {
            #region Room's Cancellation Policies from Extranet
            List<XElement> htrm = new List<XElement>();

            for (int i = 0; i < cancellationpolicy.Count(); i++)
            {
                string currencycode = string.Empty;
                try
                { currencycode = cancellationpolicy[i].Attribute("Currency").Value; }
                catch { }
                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on after " + cancellationpolicy[i].Attribute("RefundDate").Value + "  will apply " + currencycode + " " + cancellationpolicy[i].Attribute("RefundPriceEffective").Value + "  Cancellation fee"
                    , new XAttribute("LastCancellationDate", Convert.ToString(cancellationpolicy[i].Attribute("RefundDate").Value))
                    , new XAttribute("ApplicableAmount", cancellationpolicy[i].Attribute("RefundPriceEffective").Value)
                     , new XAttribute("RefundValue", cancellationpolicy[i].Attribute("RefundValue") == null ? null : cancellationpolicy[i].Attribute("RefundValue").Value)
                      , new XAttribute("MarkUpInBreakUps", cancellationpolicy[i].Attribute("MarkUpInBreakUps")== null ? null : cancellationpolicy[i].Attribute("MarkUpInBreakUps").Value)
                       , new XAttribute("MarkUpApplied", cancellationpolicy[i].Attribute("MarkUpApplied")==null ? null : cancellationpolicy[i].Attribute("MarkUpApplied").Value)
                    , new XAttribute("NoShowPolicy", "0")));
            };
            return htrm;
            #endregion
        }

        #endregion
        #endregion
        #region Prebook Request
        private List<XElement> getroomkey(List<XElement> room)
        {
            #region Bind Room keys
            List<XElement> str = new List<XElement>();

            for (int i = 0; i < room.Count(); i++)
            {
                str.Add(new XElement("Key", Convert.ToString(room[i].Attribute("SessionID").Value))
                );
            }
            return str;
            #endregion
        }
        #endregion
        #region Extranet's Supplements
        private IEnumerable<XElement> Getsupplementsextranet(List<XElement> supplements)
        {

            Int32 length = supplements.Count();
            List<XElement> supplementlst = new List<XElement>();

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
                         new XAttribute("suppName", Convert.ToString(supplements[i].Value)),
                         new XAttribute("supptType", Convert.ToString("0")),
                         new XAttribute("suppIsMandatory", Convert.ToString("True")),
                         new XAttribute("suppChargeType", Convert.ToString("Included")),
                         new XAttribute("suppPrice", Convert.ToString("0.00")),
                         new XAttribute("suppType", Convert.ToString("PerRoomSupplement")))
                      );

                }
            }
            return supplementlst;
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