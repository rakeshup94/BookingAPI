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
    public class ExtGetRoomAvail
    {
        XElement reqTravayoo;
        string dmc = "Extranet";
        string customerid = string.Empty;
        #region Hotel Availability of Extranet (XML OUT for Travayoo)
        public List<XElement> GetRoomAvail_Extranet(XElement req, string htlid)
        {
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                string hotelid = htlid;
                HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
                #region Request
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
                string requestxml = string.Empty;
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
                                "<searchRequest>" +
                                  "<Response_Type>XML</Response_Type>" +
                                  "<HotelWithRoom>true</HotelWithRoom>"+
                                  "<CustomerID>" + reqTravayoo.Descendants("CustomerID").FirstOrDefault().Value + "</CustomerID>" +
                                  "<RequestID>" + reqTravayoo.Descendants("TransID").FirstOrDefault().Value + "</RequestID>" +
                                   "<FromDate>" + reqTravayoo.Descendants("FromDate").FirstOrDefault().Value + "</FromDate>" +
                                  "<ToDate>" + reqTravayoo.Descendants("ToDate").FirstOrDefault().Value + "</ToDate>" +
                                  "<CityCode>" + reqTravayoo.Descendants("CityCode").FirstOrDefault().Value + "</CityCode>" +
                                  "<AreaID>0</AreaID>" +
                                  "<AreaName />" +
                                  "<MinStarRating>" + reqTravayoo.Descendants("MinStarRating").FirstOrDefault().Value + "</MinStarRating>" +
                                  "<MaxStarRating>" + reqTravayoo.Descendants("MaxStarRating").FirstOrDefault().Value + "</MaxStarRating>" +
                                  "<HotelName></HotelName>" +
                                  "<PaxNationality_CountryID>" + reqTravayoo.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value + "</PaxNationality_CountryID>" +
                                  "<CurrencyID>1</CurrencyID>" +
                                  reqTravayoo.Descendants("Rooms").SingleOrDefault().ToString() +
                                  "<HotelIdsList><HotelIds>" + hotelid + "</HotelIds></HotelIdsList>" +
                                  "<MealPlanList>" +
                                    "<MealType>1</MealType>" +
                                    "<MealType>2</MealType>" +
                                    "<MealType>3</MealType>" +
                                    "<MealType>4</MealType>" +
                                    "<MealType>5</MealType>" +
                                  "</MealPlanList>" +
                                  "<PropertyType>1</PropertyType>" +
                                  "<onrequest>true</onrequest>" +
                                "</searchRequest>" +
                              "</soapenv:Body>" +
                            "</soapenv:Envelope>";
                #endregion
                #region Response
                var startTime = DateTime.Now;
                object result = extclient.GetSearchCityRequestByXML(requestxml, false); 
                if (result != null)
                {
                    XElement doc = XElement.Parse(result.ToString());
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(reqTravayoo.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = reqTravayoo.Descendants("TransID").Single().Value;
                        log.LogTypeID = 2;
                        log.LogType = "RoomAvail";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = doc.ToString();
                        log.StartTime = startTime;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "GetRoomAvail_Extranet";
                        ex1.PageName = "ExtGetRoomAvail";
                        ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
                        ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    try
                    {
                        hotelavailabilityresponse = GetHtlListextranet(doc.Descendants("Hotel").ToList());
                    }
                    catch
                    {
                        hotelavailabilityresponse = null;
                    }
                }
                else
                {

                    hotelavailabilityresponse = null;

                }
                #endregion
                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "GetRoomAvail_Extranet";
                ex1.PageName = "ExtGetRoomAvail";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransactionID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
                #endregion
            }
        }
        public List<XElement> GetRoomAvail_ExtranetOUT_merge(XElement req)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "3").ToList();
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
                        dmc = "Extranet";
                    }
                    List<XElement> getrom = GetRoomAvail_ExtranetOUT(req, htlid, dmc);
                    roomavailabilityresponse.Add(getrom.Descendants("Rooms").FirstOrDefault());
                }
                #endregion
                return roomavailabilityresponse;
            }
            catch { return null; }
        }
        public List<XElement> GetRoomAvail_ExtranetOUT(XElement req, string htlid,string dmcout)
        {
            dmc = dmcout;
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = null;            
            try
            {
                string onrequest = "false";
                try
                {
                    onrequest = req.Descendants("OnRequest").FirstOrDefault().Value;
                }
                catch { }
                if (dmc == "Extranet")
                {
                    onrequest = "true";
                }
                string hotelid = htlid;
                HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
                #region Request
                string requestxml = string.Empty;
                requestxml = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                              "<soapenv:Header xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                                "<Authentication>" +
                                  "<AgentID>0</AgentID>" +
                                  "<UserName>Suraj</UserName>" +
                                  "<Password>123#</Password>" +
                                  "<ServiceType>HT_001</ServiceType>" +
                                  "<ServiceVersion>v1.0</ServiceVersion>" +
                                "</Authentication>" +
                              "</soapenv:Header>" +
                              "<soapenv:Body>" +
                                "<searchRequest>" +
                                  "<Response_Type>XML</Response_Type>" +
                                  "<HotelWithRoom>true</HotelWithRoom>" +
                                  "<CustomerID>" + customerid + "</CustomerID>" +
                                  "<RequestID>" + reqTravayoo.Descendants("TransID").FirstOrDefault().Value + "</RequestID>" +
                                   "<FromDate>" + reqTravayoo.Descendants("FromDate").FirstOrDefault().Value + "</FromDate>" +
                                  "<ToDate>" + reqTravayoo.Descendants("ToDate").FirstOrDefault().Value + "</ToDate>" +
                                  "<CityCode>" + reqTravayoo.Descendants("CityID").FirstOrDefault().Value + "</CityCode>" +
                                  "<AreaID>0</AreaID>" +
                                  "<AreaName />" +
                                  "<MinStarRating>" + reqTravayoo.Descendants("MinStarRating").FirstOrDefault().Value + "</MinStarRating>" +
                                  "<MaxStarRating>" + reqTravayoo.Descendants("MaxStarRating").FirstOrDefault().Value + "</MaxStarRating>" +
                                  "<HotelName></HotelName>" +
                                  "<PaxNationality_CountryID>" + reqTravayoo.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value + "</PaxNationality_CountryID>" +
                                  "<CurrencyID>1</CurrencyID>" +
                                  reqTravayoo.Descendants("Rooms").SingleOrDefault().ToString() +
                                  "<HotelIdsList><HotelIds>" + hotelid + "</HotelIds></HotelIdsList>" +
                                  "<MealPlanList>" +
                                    "<MealType>1</MealType>" +
                                    "<MealType>2</MealType>" +
                                    "<MealType>3</MealType>" +
                                    "<MealType>4</MealType>" +
                                    "<MealType>5</MealType>" +
                                  "</MealPlanList>" +
                                  "<PropertyType>1</PropertyType>" +
                                  "<onrequest>" + onrequest + "</onrequest>" +
                                "</searchRequest>" +
                              "</soapenv:Body>" +
                            "</soapenv:Envelope>";
                #endregion
                #region Response
                var startTime = DateTime.Now;
                object result = extclient.GetSearchCityRequestByXML(requestxml, false);  
                if (result != null)
                {
                    XElement doc = XElement.Parse(result.ToString());
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(customerid);
                        log.TrackNumber = reqTravayoo.Descendants("TransID").Single().Value;
                        log.LogTypeID = 2;
                        log.LogType = "RoomAvail";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = doc.ToString();
                        log.StartTime = startTime;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "GetRoomAvail_Extranet";
                        ex1.PageName = "ExtGetRoomAvail";
                        ex1.CustomerID = customerid;
                        ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    try
                    {
                        hotelavailabilityresponse = GetHtlListextranet(doc.Descendants("Hotel").ToList());
                    }
                    catch
                    {
                        hotelavailabilityresponse = null;
                    }
                }
                else
                {
                    hotelavailabilityresponse = null;
                }
                #endregion
                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "GetRoomAvail_Extranet";
                ex1.PageName = "ExtGetRoomAvail";
                ex1.CustomerID = customerid;
                ex1.TranID = req.Descendants("TransactionID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
                #endregion
            }
        }
        #endregion
        #region Extranet Hotel Listing
        private List<XElement> GetHtlListextranet(List<XElement> htlist)
        {
            #region Hotels
            List<XElement> hotellst = new List<XElement>();
            try
            {
                Int32 length = Convert.ToInt32(htlist.Count());
                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        try
                        {
                            #region Fetch hotel
                            List<XElement> roomtypelst = htlist[i].Descendants("Room").ToList();
                            List<XElement> fac = htlist[i].Descendants("Facility").ToList();
                            hotellst.Add(new XElement("Hotel",
                                                   new XElement("HotelID", Convert.ToString(htlist[i].Attribute("HotelID").Value)),
                                                   new XElement("HotelName", Convert.ToString(htlist[i].Attribute("HotelName").Value)),
                                                   new XElement("PropertyTypeName", Convert.ToString("")),
                                                   new XElement("CountryID", Convert.ToString("")),
                                                   new XElement("CountryName", Convert.ToString("")),
                                                   new XElement("CountryCode", Convert.ToString("")),
                                                   new XElement("CityId", Convert.ToString("")),
                                                   new XElement("CityCode", Convert.ToString("")),
                                                   new XElement("CityName", Convert.ToString(htlist[i].Attribute("CityName").Value)),
                                                   new XElement("AreaId", Convert.ToString(htlist[i].Attribute("AreaId").Value)),
                                                   new XElement("AreaName", Convert.ToString(htlist[i].Attribute("AreaName").Value)),
                                                   new XElement("RequestID", Convert.ToString("")),
                                                   new XElement("Address", Convert.ToString(htlist[i].Attribute("Address").Value)),
                                                   new XElement("Location", Convert.ToString(htlist[i].Attribute("Address").Value)),
                                                   new XElement("Description", Convert.ToString(htlist[i].Attribute("Description").Value)),
                                                   new XElement("StarRating", Convert.ToString(htlist[i].Attribute("StarRating").Value)),
                                                   new XElement("MinRate", Convert.ToString(htlist[i].Attribute("MinRate").Value)),
                                                   new XElement("HotelImgSmall", Convert.ToString(htlist[i].Attribute("HotelImgSmall").Value)),
                                                   new XElement("HotelImgLarge", Convert.ToString(htlist[i].Attribute("HotelImgLarge").Value)),
                                                   new XElement("MapLink", ""),
                                                   new XElement("Longitude", Convert.ToString(htlist[i].Attribute("Langtitude").Value)),
                                                   new XElement("Latitude", Convert.ToString(htlist[i].Attribute("Latitude").Value)),
                                                   new XElement("DMC", dmc),
                                                   new XElement("SupplierID", "3"),
                                                   new XElement("Currency", Convert.ToString(htlist[i].Attribute("Currency").Value)),
                                                   new XElement("Offers", "")
                                                   , new XElement("Facilities", hotelfacilitiesExtranet(fac))
                                                   , new XElement("Rooms", 
                                                       GetHtlRoomLstngExtranet(roomtypelst, htlist[i].Attribute("HotelID").Value, htlist[i].Attribute("Currency").Value)
                                                       )
                            ));
                            #endregion
                        }
                        catch { }
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
        public IEnumerable<XElement> GetHtlRoomLstngExtranet(List<XElement> roomlist,string Hotelcode,string currency)
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
                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].Attribute("TotalRoomRate").Value), new XAttribute("Index", m + 1), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc),new XAttribute("CUID",customerid),
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
                                 )));
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
                //Parallel.For(0, roomList1.Count(), m =>
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    //Parallel.For(0, roomList2.Count(), n =>
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        string bb1 = roomList1[m].Attribute("boardCode").Value;
                        string bb2 = roomList2[n].Attribute("boardCode").Value;
                        //string avail1 = roomList1[m].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                        //string avail2 = roomList2[n].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false";
                        if (bb1 == bb2)
                        {
                            //if (avail1 == avail2)
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
                                    #region Board Bases >0
                                    List<XElement> pricebrkups1 = roomList1[m].Descendants("Price").ToList();
                                    List<XElement> pricebrkups2 = roomList2[n].Descendants("Price").ToList();
                                    List<XElement> promotions1 = roomList1[m].Descendants("pro").ToList();
                                    List<XElement> promotions2 = roomList2[n].Descendants("pro").ToList();
                                    group++;
                                    decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList2[n].Attribute("TotalRoomRate").Value);
                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),
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
                                     )));
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
                for (int m = 0; m < roomList1.Count(); m++)
            {
                for (int n = 0; n < roomList2.Count(); n++)
                {
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
                                //if (avail1 == avail2 && avail2 == avail3 && avail1 == avail3)
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
                                        #region check allotments

                                        #region room's group
                                        List<XElement> pricebrkups1 = roomList1[m].Descendants("Price").ToList();
                                        List<XElement> pricebrkups2 = roomList2[n].Descendants("Price").ToList();
                                        List<XElement> pricebrkups3 = roomList3[o].Descendants("Price").ToList();
                                        List<XElement> promotions1 = roomList1[m].Descendants("pro").ToList();
                                        List<XElement> promotions2 = roomList2[n].Descendants("pro").ToList();
                                        List<XElement> promotions3 = roomList3[o].Descendants("pro").ToList();
                                        group++;
                                        decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList2[n].Attribute("TotalRoomRate").Value) + Convert.ToDecimal(roomList3[o].Attribute("TotalRoomRate").Value);

                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),

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
                                            //new XAttribute("isAvailable", roomList3[o].Parent.Parent.Attribute("IsOnRequest").Value == "false" ? "true" : "false"),
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
                                         )));
                                        #endregion

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
                                    //if (avail1 == avail2 && avail2 == avail3 && avail1 == avail3 && avail1 == avail4 && avail2 == avail4 && avail3 == avail4)
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
                                            #region check allotments
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

                                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),

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
                                             )));
                                            #endregion
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
                                        //if (avail1 == avail2 && avail2 == avail3 && avail1 == avail3 && avail1 == avail4 && avail2 == avail4 && avail3 == avail4)
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
                                                #region check allotments
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

                                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),

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
                                                 )));
                                                #endregion
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
                str = room6grp(roomlist, Hotelcode, currency);
            }
            #endregion

            return str;
        }
        #endregion
        #region Room's Grouping (6)
        private List<XElement> room6grp(List<XElement> roomlist,string Hotelcode,string currency)
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
                                            //if (avail1 == avail2 && avail2 == avail3 && avail1 == avail3 && avail1 == avail4 && avail2 == avail4 && avail3 == avail4)
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
                                                    #region check allotments
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

                                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),

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
                                                     )));
                                                    #endregion
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
        #region Extranet Room's Price Breakups
        private IEnumerable<XElement> GetRoomsPriceBreakupExtranet(List<XElement> pricebreakups)
        {
            #region Extranet Room's Price Breakups
            List<XElement> str = new List<XElement>();
            for (int i = 0; i < pricebreakups.Count(); i++)
            //Parallel.For(0, pricebreakups.Count(), i =>
            {
                str.Add(new XElement("Price",
                       new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                       new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].Attribute("Total").Value)),
                       new XAttribute("MarkUp", pricebreakups[i].Attribute("MarkUp") == null ? null : Convert.ToString(pricebreakups[i].Attribute("MarkUp").Value)),
                       new XAttribute("MarkUpType", pricebreakups[i].Attribute("MarkUpType") == null ? null : Convert.ToString(pricebreakups[i].Attribute("MarkUpType").Value)),
                       new XAttribute("MarkUpValue", pricebreakups[i].Attribute("MarkUpValue") == null ? null : Convert.ToString(pricebreakups[i].Attribute("MarkUpValue").Value)))
                );
            }
            return str.OrderBy(x => (int)x.Attribute("Night")).ToList();
            #endregion
        }
        #endregion
    }
}