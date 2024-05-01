using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TravillioXMLOutService.Supplier.Hoojoozat;
using TravillioXMLOutService.Models;
using System.Data;
using System.Data.SqlClient;


namespace TravillioXMLOutService.Supplier.Hoojoozat
{
    public class HoojService : IDisposable
    {
        string dmc = string.Empty;
        #region Credentails
        string AgentCode = string.Empty;
        string AgentPassword = string.Empty;
        const int SupplierId = 45;
        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
        #endregion
        public HoojService(string _customerid)
        {
            XElement suppliercred = supplier_Cred.getsupplier_credentials(_customerid, "45");
            try
            {
                AgentCode = suppliercred.Descendants("AgentCode").FirstOrDefault().Value;
                AgentPassword = suppliercred.Descendants("AgentPassword").FirstOrDefault().Value;
            }
            catch { }
        }

        #region Hotel Availability
        public List<XElement> HotelAvailability(XElement req, string xtype)
        {
            dmc = xtype;
            string hoojRequest = string.Empty;
            string hoojResponse = string.Empty;
            string soapResult = string.Empty;
            string htlCode = string.Empty;
            try
            {
                HoojStatic hoojStatic = new HoojStatic();
                DataTable hoojCity = hoojStatic.GetHoojCityCode(req.Descendants("CityID").FirstOrDefault().Value,SupplierId);
                List<XElement> HotelsData = new List<XElement>();
                HttpWebRequest request = CreateWebRequest();
                XmlDocument SoapReq = new XmlDocument();
                SoapReq.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>  
                            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">  
                            <soap:Body><GetHotelRoomAvailabilityV2 xmlns=""http://hoojoozat.com/"">  
                            <AgentUsername>" + AgentCode + @"</AgentUsername><AgentPassword>" + AgentPassword + @"</AgentPassword> 
                            <CountryCode>" + hoojCity.Rows[0]["CountryCode"].ToString() + @"</CountryCode><DestinationCode>" + hoojCity.Rows[0]["CityCode"].ToString() + @"</DestinationCode>   
                            <FromDate>" + req.Descendants("FromDate").FirstOrDefault().Value.HoojDateString() + @"</FromDate>
                            <ToDate>" + req.Descendants("ToDate").FirstOrDefault().Value.HoojDateString() + @"</ToDate> 
                            <Occupancy>" + req.Descendants("Rooms").FirstOrDefault().HoojOccupancy() + @"</Occupancy> 
                            <HotelCode>0</HotelCode><CategoryCode>1</CategoryCode><SortBy></SortBy><ClientNationality>" + req.Descendants("NationalityName").FirstOrDefault().Value + @"</ClientNationality>
                            <RoomRateDisplay>Yes</RoomRateDisplay>
                            </GetHotelRoomAvailabilityV2>  
                            </soap:Body></soap:Envelope>");

                hoojRequest = SoapReq.InnerXml;
                using (Stream stream = request.GetRequestStream())
                {
                    SoapReq.Save(stream);
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                }

                XDocument xmlData = XDocument.Parse(soapResult);
                XElement resp = xmlData.Root.RemoveXmlns();
                XElement newResp = resp.Descendants("GetHotelRoomAvailabilityV2Result").FirstOrDefault();
                XElement respHTL = XElement.Parse(newResp.Value);
                respHTL = respHTL.RemoveXmlns();
                hoojResponse = respHTL.ToString();
                #region supplier log
                APILogDetail log = new APILogDetail();
                log.customerID = req.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 1;
                log.LogType = "Search";
                log.SupplierID = SupplierId;
                log.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = hoojRequest;
                log.logresponseXML = hoojResponse;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion
                int errCnt = respHTL.Descendants("Error").Count();
                if (errCnt > 0)
                {
                    return null;
                }
                else
                {
                    DataTable hoojHotels = hoojStatic.GetHoojStaticHotels(hoojCity.Rows[0]["CityCode"].ToString(), hoojCity.Rows[0]["CountryCode"].ToString(), req.Descendants("MinStarRating").FirstOrDefault().Value.ModifyToInt(), req.Descendants("MaxStarRating").FirstOrDefault().Value.ModifyToInt());
                    foreach (var hotel in respHTL.Descendants("Hotel"))
                    {
                        var HotelData = hoojHotels.Select("[Code] = '" + hotel.Descendants("Code").FirstOrDefault().Value + "'").FirstOrDefault();
                        if (HotelData != null)
                        {
                            htlCode = HotelData["Code"].ToString();
                            decimal minprice = 0.0m;
                            int count = 0;
                            bool validpax = true;
                            if (hotel.Descendants("RoomRates").Where(x => x.Element("Type").Value == "Available").ToList().Count == 0)
                            {
                                validpax = false;
                            }
                            else
                            {
                                foreach (var rpax in req.Descendants("RoomPax"))
                                {
                                    var rmDetail = hotel.Descendants("RoomRates").Where(x => x.Element("Adults").Value == rpax.Descendants("Adult").FirstOrDefault().Value && x.Element("Children").Value == rpax.Descendants("Child").FirstOrDefault().Value && x.Element("Type").Value == "Available").FirstOrDefault();
                                    if (rmDetail != null)
                                    {
                                        int quantity = rmDetail.Descendants("Quantity").FirstOrDefault().Value.ModifyToInt();
                                        minprice += rmDetail.Descendants("Price").FirstOrDefault().Value.ModifyToDecimal() / quantity;
                                        if (quantity == 1)
                                        {
                                            count++;
                                        }
                                        validpax = true;
                                    }
                                    else
                                    {
                                        validpax = false;
                                        break;
                                    }

                                }
                            }
                            if (validpax)
                            {
                                XElement hoteldata = new XElement("Hotel", new XElement("HotelID", HotelData["Code"].ToString()),
                                                new XElement("HotelName", HotelData["Name"].ToString()),
                                                new XElement("PropertyTypeName"),
                                                new XElement("CountryID"),
                                                new XElement("CountryName", HotelData["Country"].ToString()),
                                                new XElement("CountryCode", HotelData["CountryCode"].ToString()),
                                                new XElement("CityId"),
                                                new XElement("CityCode", HotelData["CityCode"].ToString()),
                                                new XElement("CityName", HotelData["CityName"].ToString()),
                                                new XElement("AreaId"),
                                                new XElement("AreaName"),
                                                new XElement("RequestID"),
                                                new XElement("Address", HotelData["Address"].ToString()),
                                                new XElement("Location"),
                                                new XElement("Description"),
                                                new XElement("StarRating", HotelData["StarRating"].ToString() == "6" ? "0" : HotelData["StarRating"].ToString()),
                                                new XElement("MinRate", minprice),
                                                new XElement("HotelImgSmall", "https://www.hoojoozat.com/pictures/hotelpic/mainnew/" + HotelData["MainPic"].ToString()),
                                                new XElement("HotelImgLarge", "https://www.hoojoozat.com/pictures/hotelpic/mainnew/" + HotelData["MainPic"].ToString()),
                                                new XElement("MapLink"),
                                                new XElement("Longitude", HotelData["Longitude"].ToString()),
                                                new XElement("Latitude", HotelData["Latitude"].ToString()),
                                                new XElement("DMC", dmc), new XElement("SupplierID", SupplierId),
                                                new XElement("Currency", hotel.Descendants("CurrencyCode").FirstOrDefault().Value),
                                                new XElement("Offers"), new XElement("Facilities"),
                                                new XElement("Rooms")
                                                );

                                HotelsData.Add(hoteldata);
                            }
                        }

                    }
                    return HotelsData;
                }

            }
            catch (Exception ex)
            {
                return null;
            }

        }
        #endregion
        #region Soap Connection
        public static HttpWebRequest CreateWebRequest()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(@"http://130.211.25.226/availabilityservices.asmx");
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            webRequest.Timeout = 2000000;
            webRequest.ReadWriteTimeout = 2000000;
            return webRequest;
        }
        #endregion
        #region Hotel Description
        public XElement HotelDescription(XElement req)
        {
            XElement hotelDesc = new XElement("Hotels");
            XElement HotelDescReq = req.Descendants("hoteldescRequest").FirstOrDefault();
            XElement hotelDescResdoc = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XElement("Authentication", new XElement("AgentID", req.Descendants("AgentID").Single().Value), new XElement("UserName", req.Descendants("UserName").Single().Value), 
                                       new XElement("Password", req.Descendants("Password").Single().Value), new XElement("ServiceType", req.Descendants("ServiceType").Single().Value), new XElement("ServiceVersion", req.Descendants("ServiceVersion").Single().Value))));

            try
            {
                HoojStatic HoojStatic = new HoojStatic();
                DataTable HotelDetail = HoojStatic.GetHoojHotelDetails(req.Descendants("HotelID").FirstOrDefault().Value);
              
                hotelDescResdoc.Add(new XElement(soapenv + "Body", HotelDescReq, new XElement("hoteldescResponse", new XElement("Hotels", new XElement("Hotel", new XElement("HotelID", req.Descendants("HotelID").FirstOrDefault().Value),
                                    new XElement("Description"), HotelDetail.Rows[0]["Images"].ToString().getHotelImages(), new XElement("Facilities"),
                                    new XElement("ContactDetails", new XElement("Phone", HotelDetail.Rows[0]["Phone"].ToString()), new XElement("Fax", HotelDetail.Rows[0]["Fax"].ToString())),
                                    new XElement("CheckinTime"), new XElement("CheckoutTime")
                                    )))));

                return hotelDescResdoc;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "getHotelDescription";
                ex1.PageName = "Hoojoozat";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                hotelDescResdoc.Add(new XElement(soapenv + "Body", HotelDescReq, new XElement("hoteldescResponse", new XElement("Hotels"))));
                return hotelDescResdoc;
            }
        }
        #endregion

        #region Room Availability
        public XElement RoomAvailability(XElement roomReq, string xtype, string htlid)
        {
            dmc = xtype;
            string hoojRequest = string.Empty;
            string hoojResponse = string.Empty;
            XElement searchReq = roomReq.Descendants("searchRequest").FirstOrDefault();
            XElement RoomDetails = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                   new XElement("Authentication", new XElement("AgentID", roomReq.Descendants("AgentID").FirstOrDefault().Value), new XElement("UserName", roomReq.Descendants("UserName").FirstOrDefault().Value), new XElement("Password", roomReq.Descendants("Password").FirstOrDefault().Value),
                                   new XElement("ServiceType", roomReq.Descendants("ServiceType").FirstOrDefault().Value), new XElement("ServiceVersion", roomReq.Descendants("ServiceVersion").FirstOrDefault().Value))));

            string soapResult = string.Empty;
            try
            {
                HoojStatic hoojStatic = new HoojStatic();
                DataTable hoojCity = hoojStatic.GetHoojCityCode(roomReq.Descendants("CityID").FirstOrDefault().Value, SupplierId);
                HttpWebRequest request = CreateWebRequest();
                XmlDocument SoapReq = new XmlDocument();
                SoapReq.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>  
                                 <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">  
                                 <soap:Body><GetHotelRoomAvailabilityByHotelsV2 xmlns=""http://hoojoozat.com/"">  
                                 <AgentUsername>" + AgentCode + @"</AgentUsername><AgentPassword>" + AgentPassword + @"</AgentPassword> 
                                 <CountryCode>" + hoojCity.Rows[0]["CountryCode"].ToString() + @"</CountryCode><HotelCodes>" + htlid + @"</HotelCodes>   
                                 <FromDate>" + roomReq.Descendants("FromDate").FirstOrDefault().Value.HoojDateString() + @"</FromDate>
                                 <ToDate>" + roomReq.Descendants("ToDate").FirstOrDefault().Value.HoojDateString() + @"</ToDate> 
                                 <Occupancy>" + roomReq.Descendants("Rooms").FirstOrDefault().HoojOccupancy() + @"</Occupancy> 
                                 <SortBy></SortBy><ClientNationality>" + roomReq.Descendants("NationalityName").FirstOrDefault().Value + @"</ClientNationality>
                                 <RoomRateDisplay>Yes</RoomRateDisplay>
                                 </GetHotelRoomAvailabilityByHotelsV2>  
                                 </soap:Body></soap:Envelope>");
                hoojRequest = SoapReq.InnerXml;
                using (Stream stream = request.GetRequestStream())
                {
                    SoapReq.Save(stream);
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                }
                XDocument xmlData = XDocument.Parse(soapResult);
                XElement resp = xmlData.Root.RemoveXmlns();
                XElement newResp = resp.Descendants("GetHotelRoomAvailabilityByHotelsV2Result").FirstOrDefault();
                XElement respHTL = XElement.Parse(newResp.Value);
                respHTL = respHTL.RemoveXmlns();
                hoojResponse = respHTL.ToString();
                #region supplier log
                APILogDetail log = new APILogDetail();
                log.customerID = roomReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 2;
                log.LogType = "RoomAvail";
                log.SupplierID = SupplierId;
                log.TrackNumber = roomReq.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = hoojRequest;
                log.logresponseXML = hoojResponse;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion
                int errCnt = respHTL.Descendants("Error").Count();
                if (errCnt > 0)
                {
                    RoomDetails.Add(new XElement(soapenv + "Body", searchReq, new XElement("HotelPreBookingResponse", new XElement("ErrorTxt", "Room is not available"))));
                    return RoomDetails;
                }
                else
                {
                    string htlCode = respHTL.Descendants("Code").FirstOrDefault().Value;
                    int nights = (int)(roomReq.Descendants("ToDate").FirstOrDefault().Value.ConvertToDate() - roomReq.Descendants("FromDate").FirstOrDefault().Value.ConvertToDate()).TotalDays;
                    int roomSeq = 1;
                    int totalrooms = roomReq.Descendants("RoomPax").Count();
                    XElement groupDetails = new XElement("Rooms");
                    List<XElement> roomlst = new List<XElement>();
                    List<string> mealList = new List<string> { "No Breakfast", "Bed Only", "Room Only", "Breakfast", "Half Board", "Full Board", "All Inclusive" };
                    foreach (var rpax in roomReq.Descendants("RoomPax"))
                    {
                        int adults = rpax.Descendants("Adult").FirstOrDefault().Value.ModifyToInt();
                        int child = rpax.Descendants("Child").FirstOrDefault().Value.ModifyToInt();
                        if (roomSeq == 1)
                        {
                            int grpIndex = 1;
                            foreach (var room in respHTL.Descendants("RoomRates").Where(x => x.Element("Adults").Value == rpax.Descendants("Adult").FirstOrDefault().Value && x.Element("Children").Value == rpax.Descendants("Child").FirstOrDefault().Value && x.Element("Type").Value == "Available" && (!x.Element("RoomType").Value.Contains("No Breakfast") && !x.Element("RoomType").Value.Contains("Bed Only") && !x.Element("RoomType").Value.Contains("Room Only") && !x.Element("RoomType").Value.Contains("Breakfast") && !x.Element("RoomType").Value.Contains("Half Board") && !x.Element("RoomType").Value.Contains("Full Board") && !x.Element("RoomType").Value.Contains("All Inclusive"))))
                            {
                                if (room != null)
                                {
                                    int rmCount = room.Descendants("Quantity").FirstOrDefault().Value.ModifyToInt();
                                    decimal RoomGroupRate = room.Descendants("Price").FirstOrDefault().Value.ModifyToDecimal();
                                    decimal totalRoomRate = RoomGroupRate / rmCount;
                                    XElement RoomType = new XElement("RoomTypes", new XAttribute("Index", grpIndex), new XAttribute("TotalRate", totalRoomRate), new XAttribute("HtlCode", htlCode), new XAttribute("CrncyCode", room.Descendants("CurrencyCode").FirstOrDefault().Value), new XAttribute("DMCType", dmc));

                                    XElement roomDtl = new XElement("Room", new XAttribute("ID", room.Descendants("RoomCode").FirstOrDefault().Value), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", roomSeq), new XAttribute("SessionID", respHTL.Descendants("SessionNB").FirstOrDefault().Value), new XAttribute("RoomType", room.Descendants("RoomType").FirstOrDefault().Value), new XAttribute("OccupancyID", room.Descendants("RoomPurchaseToken").FirstOrDefault().Value),
                                                       new XAttribute("OccupancyName", ""), new XAttribute("MealPlanID", ""), new XAttribute("MealPlanName", "Room Only"), new XAttribute("MealPlanCode", ""), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", totalRoomRate / nights),
                                                       new XAttribute("TotalRoomRate", totalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", room.Descendants("Type").FirstOrDefault().Value == "Available" ? true : false),
                                                       new XElement("RequestID", respHTL.Descendants("ReferenceNB").FirstOrDefault().Value), new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions", room.Element("OfferType") != null ? room.Descendants("OfferType").FirstOrDefault().Value : string.Empty)),
                                                       new XElement("CancellationPolicy"), new XElement("Amenities", new XElement("Amenity")),
                                                       new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"),
                                                       new XElement(getPriceBreakup(nights, totalRoomRate)),
                                                       new XElement("AdultNum", room.Descendants("Adults").FirstOrDefault().Value),
                                                       new XElement("ChildNum", room.Descendants("Children").FirstOrDefault().Value));
                                    RoomType.Add(roomDtl);
                                    roomlst.Add(RoomType);
                                    grpIndex++;
                                }
                            }
                            foreach (var mealtype in mealList)
                            {
                                foreach (var room in respHTL.Descendants("RoomRates").Where(x => x.Element("Adults").Value == rpax.Descendants("Adult").FirstOrDefault().Value && x.Element("Children").Value == rpax.Descendants("Child").FirstOrDefault().Value && x.Element("Type").Value == "Available" && x.Element("RoomType").Value.Contains(mealtype)&&(mealtype=="Breakfast"?!x.Element("RoomType").Value.Contains("No Breakfast"):true)))
                                {
                                    if (room != null)
                                    {
                                        int rmCount = room.Descendants("Quantity").FirstOrDefault().Value.ModifyToInt();
                                        decimal RoomGroupRate = room.Descendants("Price").FirstOrDefault().Value.ModifyToDecimal();
                                        decimal totalRoomRate = RoomGroupRate / rmCount;
                                        XElement RoomType = new XElement("RoomTypes", new XAttribute("Index", grpIndex), new XAttribute("TotalRate", totalRoomRate), new XAttribute("HtlCode", htlCode), new XAttribute("CrncyCode", room.Descendants("CurrencyCode").FirstOrDefault().Value), new XAttribute("DMCType", dmc));

                                        XElement roomDtl = new XElement("Room", new XAttribute("ID", room.Descendants("RoomCode").FirstOrDefault().Value), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", roomSeq), new XAttribute("SessionID", respHTL.Descendants("SessionNB").FirstOrDefault().Value), new XAttribute("RoomType", room.Descendants("RoomType").FirstOrDefault().Value), new XAttribute("OccupancyID", room.Descendants("RoomPurchaseToken").FirstOrDefault().Value),
                                                           new XAttribute("OccupancyName", ""), new XAttribute("MealPlanID", ""), new XAttribute("MealPlanName", (mealtype == "Bed Only" || mealtype == "No Breakfast") ? "Room Only" : mealtype), new XAttribute("MealPlanCode", ""), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", totalRoomRate / nights),
                                                           new XAttribute("TotalRoomRate", totalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", room.Descendants("Type").FirstOrDefault().Value == "Available" ? true : false),
                                                           new XElement("RequestID", respHTL.Descendants("ReferenceNB").FirstOrDefault().Value), new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions", room.Element("OfferType") != null ? room.Descendants("OfferType").FirstOrDefault().Value : string.Empty)),
                                                           new XElement("CancellationPolicy"), new XElement("Amenities", new XElement("Amenity")),
                                                           new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"),
                                                           new XElement(getPriceBreakup(nights, totalRoomRate)),
                                                           new XElement("AdultNum", room.Descendants("Adults").FirstOrDefault().Value),
                                                           new XElement("ChildNum", room.Descendants("Children").FirstOrDefault().Value));
                                        RoomType.Add(roomDtl);
                                        roomlst.Add(RoomType);
                                        grpIndex++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            int ind = 0;
                            foreach (var room in respHTL.Descendants("RoomRates").Where(x => x.Element("Adults").Value == rpax.Descendants("Adult").FirstOrDefault().Value && x.Element("Children").Value == rpax.Descendants("Child").FirstOrDefault().Value && x.Element("Type").Value == "Available" && (!x.Element("RoomType").Value.Contains("No Breakfast") && !x.Element("RoomType").Value.Contains("Bed Only") && !x.Element("RoomType").Value.Contains("Room Only") && !x.Element("RoomType").Value.Contains("Breakfast") && !x.Element("RoomType").Value.Contains("Half Board") && !x.Element("RoomType").Value.Contains("Full Board") && !x.Element("RoomType").Value.Contains("All Inclusive"))))
                            {
                                if (ind < roomlst.Count)
                                {
                                    int rmCount = room.Descendants("Quantity").FirstOrDefault().Value.ModifyToInt();
                                    decimal RoomGroupRate = room.Descendants("Price").FirstOrDefault().Value.ModifyToDecimal();
                                    decimal totalRoomRate = RoomGroupRate / rmCount;
                                    XElement roomDtl = new XElement("Room", new XAttribute("ID", room.Descendants("RoomCode").FirstOrDefault().Value), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", roomSeq), new XAttribute("SessionID", respHTL.Descendants("SessionNB").FirstOrDefault().Value), new XAttribute("RoomType", room.Descendants("RoomType").FirstOrDefault().Value), new XAttribute("OccupancyID", room.Descendants("RoomPurchaseToken").FirstOrDefault().Value),
                                                       new XAttribute("OccupancyName", ""), new XAttribute("MealPlanID", ""), new XAttribute("MealPlanName", "Room Only"), new XAttribute("MealPlanCode", ""), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", totalRoomRate / nights),
                                                       new XAttribute("TotalRoomRate", totalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", room.Descendants("Type").FirstOrDefault().Value == "Available" ? true : false),
                                                       new XElement("RequestID", respHTL.Descendants("ReferenceNB").FirstOrDefault().Value), new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions", room.Element("OfferType") != null ? room.Descendants("OfferType").FirstOrDefault().Value : string.Empty)),
                                                       new XElement("CancellationPolicy"), new XElement("Amenities", new XElement("Amenity")),
                                                       new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"),
                                                       new XElement(getPriceBreakup(nights, totalRoomRate)),
                                                       new XElement("AdultNum", room.Descendants("Adults").FirstOrDefault().Value),
                                                       new XElement("ChildNum", room.Descendants("Children").FirstOrDefault().Value));

                                    XElement rmtype = roomlst[ind];
                                    decimal GroupRate = rmtype.Attribute("TotalRate").Value.ModifyToDecimal();
                                    GroupRate += totalRoomRate;
                                    rmtype.Attribute("TotalRate").Value = GroupRate.ToString();
                                    rmtype.Elements("Room").Where(x => (int)x.Attribute("RoomSeq") < roomSeq).Last().AddAfterSelf(roomDtl);
                                    roomlst[ind] = rmtype;
                                    ind++;
                                }
                            }
                            foreach (var mealtype in mealList)
                            {
                                foreach (var room in respHTL.Descendants("RoomRates").Where(x => x.Element("Adults").Value == rpax.Descendants("Adult").FirstOrDefault().Value && x.Element("Children").Value == rpax.Descendants("Child").FirstOrDefault().Value && x.Element("Type").Value == "Available" && x.Element("RoomType").Value.Contains(mealtype)&&(mealtype=="Breakfast"?!x.Element("RoomType").Value.Contains("No Breakfast"):true)))
                                {
                                    if (ind < roomlst.Count)
                                    {
                                        int rmCount = room.Descendants("Quantity").FirstOrDefault().Value.ModifyToInt();
                                        decimal RoomGroupRate = room.Descendants("Price").FirstOrDefault().Value.ModifyToDecimal();
                                        decimal totalRoomRate = RoomGroupRate / rmCount;
                                        XElement roomDtl = new XElement("Room", new XAttribute("ID", room.Descendants("RoomCode").FirstOrDefault().Value), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", roomSeq), new XAttribute("SessionID", respHTL.Descendants("SessionNB").FirstOrDefault().Value), new XAttribute("RoomType", room.Descendants("RoomType").FirstOrDefault().Value), new XAttribute("OccupancyID", room.Descendants("RoomPurchaseToken").FirstOrDefault().Value),
                                                           new XAttribute("OccupancyName", ""), new XAttribute("MealPlanID", ""), new XAttribute("MealPlanName", (mealtype == "Bed Only" || mealtype == "No Breakfast") ? "Room Only" : mealtype), new XAttribute("MealPlanCode", ""), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", totalRoomRate / nights),
                                                           new XAttribute("TotalRoomRate", totalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", room.Descendants("Type").FirstOrDefault().Value == "Available" ? true : false),
                                                           new XElement("RequestID", respHTL.Descendants("ReferenceNB").FirstOrDefault().Value), new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions", room.Element("OfferType") != null ? room.Descendants("OfferType").FirstOrDefault().Value : string.Empty)),
                                                           new XElement("CancellationPolicy"), new XElement("Amenities", new XElement("Amenity")),
                                                           new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"),
                                                           new XElement(getPriceBreakup(nights, totalRoomRate)),
                                                           new XElement("AdultNum", room.Descendants("Adults").FirstOrDefault().Value),
                                                           new XElement("ChildNum", room.Descendants("Children").FirstOrDefault().Value));

                                        XElement rmtype = roomlst[ind];
                                        decimal GroupRate = rmtype.Attribute("TotalRate").Value.ModifyToDecimal();
                                        GroupRate += totalRoomRate;
                                        rmtype.Attribute("TotalRate").Value = GroupRate.ToString();
                                        rmtype.Elements("Room").Where(x => (int)x.Attribute("RoomSeq") < roomSeq).Last().AddAfterSelf(roomDtl);
                                        roomlst[ind] = rmtype;
                                        ind++;
                                    }
                                }

                            }

                        }
                        roomSeq++;
                    }
                    foreach (var roomgrp in roomlst)
                    {
                        if (roomgrp.Elements("Room").Count() == totalrooms)
                        {
                            groupDetails.Add(roomgrp);
                        }
                    }
                    XElement hoteldata = new XElement("Hotels", new XElement("Hotel", new XElement("HotelID", ""), new XElement("HotelName", ""), new XElement("PropertyTypeName"),
                                         new XElement("CountryID", ""), new XElement("CountryName", ""), new XElement("CityCode", ""), new XElement("CityName", ""),
                                         new XElement("AreaId"), new XElement("AreaName"), new XElement("RequestID"), new XElement("Address"), new XElement("Location"),
                                         new XElement("Description"), new XElement("StarRating"), new XElement("MinRate", ""), new XElement("HotelImgSmall"),
                                         new XElement("HotelImgLarge"), new XElement("MapLink"), new XElement("Longitude"), new XElement("Latitude"), new XElement("DMC", "Hoojoozat"),
                                         new XElement("SupplierID", SupplierId), new XElement("Currency", ""), new XElement("Offers"), new XElement("Facilities", new XElement("Facility", "No Facility Available")),
                                         new XElement(groupDetails)));
                    RoomDetails.Add(new XElement(soapenv + "Body", searchReq, new XElement("searchResponse", hoteldata)));
                }
                return RoomDetails;

            }
            catch (Exception ex)
            {
                APILogDetail log = new APILogDetail();
                log.customerID = roomReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 2;
                log.LogType = "RoomAvail";
                log.SupplierID = SupplierId;
                log.TrackNumber = roomReq.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = hoojRequest;
                log.logresponseXML = hoojResponse;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);

                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "RoomAvailability";
                ex1.PageName = "Hoojoozat";
                ex1.CustomerID = roomReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = roomReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                RoomDetails.Add(new XElement(soapenv + "Body", searchReq, new XElement("searchResponse", new XElement("ErrorTxt", "Room is not available"))));
                return RoomDetails;
            }
        }
        #endregion
       
        #region Price Breakup
        private XElement getPriceBreakup(int nights, decimal roomPrice)
        {
            XElement pricebrk = new XElement("PriceBreakups");
            decimal nightPrice = Math.Round(roomPrice / nights, 4);
            for (int i = 1; i <= nights; i++)
            {
                pricebrk.Add(new XElement("Price", new XAttribute("Night", i), new XAttribute("PriceValue", nightPrice)));

            }
            return pricebrk;
        }
        #endregion



        #region PreBooking
        public XElement PreBooking(XElement preBookReq,string xmlout)
        {
            string hoojRequest = string.Empty;
            string hoojResponse = string.Empty;
            dmc = xmlout;
            XElement preBookReqest = preBookReq.Descendants("HotelPreBookingRequest").FirstOrDefault();
            XElement PreBookResponse = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XElement("Authentication", new XElement("AgentID", preBookReq.Descendants("AgentID").FirstOrDefault().Value), new XElement("UserName", preBookReq.Descendants("UserName").FirstOrDefault().Value),
                                       new XElement("Password", preBookReq.Descendants("Password").FirstOrDefault().Value), new XElement("ServiceType", preBookReq.Descendants("ServiceType").FirstOrDefault().Value),
                                       new XElement("ServiceVersion", preBookReq.Descendants("ServiceVersion").FirstOrDefault().Value))));
            try
            {
                string soapResult = string.Empty;
                List<string> tokenList = new List<string>();
                string purchasetoken = string.Empty;
                decimal TotalRateOld = preBookReq.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value.ModifyToDecimal();
                string sessionNB = preBookReq.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                string refNB = preBookReq.Descendants("Room").FirstOrDefault().Descendants("RequestID").FirstOrDefault().Value;
                foreach (var room in preBookReq.Descendants("Room"))
                {
                    string token = room.Attribute("OccupancyID").Value;
                    if (!tokenList.Contains(token))
                    {
                        tokenList.Add(token);
                    }
                }
                foreach (var ptoken in tokenList)
                {
                    if (string.IsNullOrEmpty(purchasetoken))
                    {
                        purchasetoken = ptoken;
                    }
                    else
                    {
                        purchasetoken += ";" + ptoken;
                    }
                }
                HttpWebRequest request = CreateWebRequest();
                XmlDocument SoapReq = new XmlDocument();
                SoapReq.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>  
                                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">  
                                <soap:Body><ConfirmAvailability xmlns=""http://hoojoozat.com/"">  
                                <AgentUsername>" + AgentCode + @"</AgentUsername><AgentPassword>" + AgentPassword + @"</AgentPassword> 
                                <SessionNB>" + sessionNB + @"</SessionNB>
                                <ReferenceNB>" + refNB + @"</ReferenceNB>
                                <RoomPurchaseToken>" + purchasetoken + @"</RoomPurchaseToken>
                                </ConfirmAvailability></soap:Body></soap:Envelope>");
                hoojRequest = SoapReq.InnerXml;
                using (Stream stream = request.GetRequestStream())
                {
                    SoapReq.Save(stream);
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                }
                XDocument xmlData = XDocument.Parse(soapResult);
                XElement resp = xmlData.Root.RemoveXmlns();
                XElement newResp = resp.Descendants("ConfirmAvailabilityResponse").FirstOrDefault();
                XElement resPreBook = XElement.Parse(newResp.Value);
                resPreBook = resPreBook.RemoveXmlns();
                hoojResponse = resPreBook.ToString();
                #region supplier log
                APILogDetail log = new APILogDetail();
                log.customerID = preBookReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 4;
                log.LogType = "PreBook";
                log.SupplierID = SupplierId;
                log.TrackNumber = preBookReq.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = hoojRequest;
                log.logresponseXML = hoojResponse;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion
                int errCnt = resPreBook.Descendants("Error").Count();
                if (errCnt > 0)
                {
                    PreBookResponse.Add(new XElement(soapenv + "Body", preBookReqest, new XElement("HotelPreBookingResponse", new XElement("ErrorTxt", "Room is not available"))));
                    return PreBookResponse;
                }
                else
                {
                    bool AvlStatus = false;
                    decimal TotalRate = 0.0m;
                    string currency = string.Empty;
                    int nights = (int)(preBookReq.Descendants("ToDate").FirstOrDefault().Value.ConvertToDate() - preBookReq.Descendants("FromDate").FirstOrDefault().Value.ConvertToDate()).TotalDays;
                    int roomSeq = 1;
                    XElement groupDetails = new XElement("Rooms");
                    List<XElement> roomlst = new List<XElement>();
                    List<string> termsList = new List<string>();
                    string termsCondition = string.Empty;
                    foreach (var rpax in preBookReq.Descendants("Room"))
                    {
                        int adults = rpax.Attribute("Adult").Value.ModifyToInt();
                        int childnum = 0;
                        if (!string.IsNullOrEmpty(rpax.Attribute("ChildAge").Value))
                        {
                            string[] child = rpax.Attribute("ChildAge").Value.Split(',');
                            childnum = child.Count();
                        }
                        if (roomSeq == 1)
                        {
                            int grpIndex = 1;
                            foreach (var room in resPreBook.Descendants("RoomRates").Where(x => x.Element("Adults").Value == adults.ToString() && x.Element("Children").Value == childnum.ToString()))
                            {
                                AvlStatus = room.Descendants("Type").FirstOrDefault().Value == "Available" ? true : false;
                                if (room.Descendants("RoomRatesDetail").FirstOrDefault().Element("Remark") != null)
                                {
                                    string remark = room.Descendants("Remark").FirstOrDefault().Value;
                                    if (!termsList.Contains(remark))
                                    {
                                        termsList.Add(remark);
                                    }
                                }
                                currency = room.Descendants("CurrencyCode").FirstOrDefault().Value;
                                int rmCount = room.Descendants("Quantity").FirstOrDefault().Value.ModifyToInt();
                                decimal RoomGroupRate = room.Descendants("TotalPrice").FirstOrDefault().Value.ModifyToDecimal();
                                decimal totalRoomRate = RoomGroupRate / rmCount;
                                TotalRate += totalRoomRate;
                                XElement RoomType = new XElement("RoomTypes", new XAttribute("Index", grpIndex), new XAttribute("TotalRate", totalRoomRate));
                                XElement roomDtl = new XElement("Room", new XAttribute("ID", room.Descendants("RoomCode").FirstOrDefault().Value), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", roomSeq), new XAttribute("SessionID", rpax.Attribute("SessionID").Value), new XAttribute("RoomType", room.Descendants("RoomType").FirstOrDefault().Value),
                                                   new XAttribute("OccupancyID", room.Descendants("RoomPurchaseToken").FirstOrDefault().Value), new XAttribute("OccupancyName", ""), new XAttribute("MealPlanID", ""), new XAttribute("MealPlanName", ""), new XAttribute("MealPlanCode", ""), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", totalRoomRate / nights),
                                                   new XAttribute("TotalRoomRate", totalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", room.Descendants("Type").FirstOrDefault().Value == "Available" ? true : false),
                                                   new XElement("RequestID", rpax.Descendants("RequestID").FirstOrDefault().Value), new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions", "")),
                                                   new XElement("CancellationPolicy"), new XElement("Amenities", new XElement("Amenity")),
                                                   new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"),
                                                   new XElement(getPriceBreakup(nights, totalRoomRate)),
                                                   new XElement("AdultNum", adults),
                                                   new XElement("ChildNum", childnum));
                                RoomType.Add(roomDtl);
                                roomlst.Add(RoomType);
                                grpIndex++;
                            }
                        }
                        else
                        {
                            int ind = 0;
                            foreach (var room in resPreBook.Descendants("RoomRates").Where(x => x.Element("Adults").Value == adults.ToString() && x.Element("Children").Value == childnum.ToString()))
                            {
                                AvlStatus = room.Descendants("Type").FirstOrDefault().Value == "Available" ? true : false;
                                currency = room.Descendants("CurrencyCode").FirstOrDefault().Value;
                                string cxlDsc = room.Descendants("CancellationPolicy").FirstOrDefault().Value;
                                int rmCount = room.Descendants("Quantity").FirstOrDefault().Value.ModifyToInt();
                                decimal RoomGroupRate = room.Descendants("Price").FirstOrDefault().Value.ModifyToDecimal();
                                decimal totalRoomRate = RoomGroupRate / rmCount;
                                XElement roomDtl = new XElement("Room", new XAttribute("ID", room.Descendants("RoomCode").FirstOrDefault().Value), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", roomSeq), new XAttribute("SessionID", rpax.Attribute("SessionID").Value), new XAttribute("RoomType", room.Descendants("RoomType").FirstOrDefault().Value),
                                                   new XAttribute("OccupancyID", room.Descendants("RoomPurchaseToken").FirstOrDefault().Value), new XAttribute("OccupancyName", ""), new XAttribute("MealPlanID", ""), new XAttribute("MealPlanName", ""), new XAttribute("MealPlanCode", ""), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", totalRoomRate / nights),
                                                   new XAttribute("TotalRoomRate", totalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", room.Descendants("Type").FirstOrDefault().Value == "Available" ? true : false),
                                                   new XElement("RequestID", rpax.Descendants("RequestID").FirstOrDefault().Value), new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions", "")),
                                                   new XElement("CancellationPolicy"), new XElement("Amenities", new XElement("Amenity")),
                                                   new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"),
                                                   new XElement(getPriceBreakup(nights, totalRoomRate)),
                                                   new XElement("AdultNum", adults),
                                                   new XElement("ChildNum", childnum));

                                XElement rmtype = roomlst[ind];
                                decimal GroupRate = rmtype.Attribute("TotalRate").Value.ModifyToDecimal();
                                TotalRate += totalRoomRate;
                                GroupRate += totalRoomRate;
                                rmtype.Attribute("TotalRate").Value = GroupRate.ToString();
                                rmtype.Elements("Room").Where(x => (int)x.Attribute("RoomSeq") < roomSeq).Last().AddAfterSelf(roomDtl);
                                roomlst[ind] = rmtype;
                                ind++;
                            }

                        }
                        roomSeq++;
                    }
                    foreach (var roomgrp in roomlst)
                    {
                        groupDetails.Add(roomgrp);

                    }
                    foreach (string term in termsList)
                    {
                        if (string.IsNullOrEmpty(termsCondition))
                        {
                            termsCondition = term;
                        }
                        else
                        {
                            termsCondition += "." + term;
                        }
                    }
                    var rt = groupDetails.Element("RoomTypes");
                    groupDetails.Descendants("Room").Last().AddAfterSelf(GetCxlPolicy(resPreBook.Descendants("RoomRates").ToList(), preBookReq.Descendants("FromDate").FirstOrDefault().Value.ConvertToDate()));
                    XElement hoteldata = new XElement("Hotels", new XElement("Hotel", new XElement("HotelID", preBookReq.Descendants("HotelID").FirstOrDefault().Value),
                                                new XElement("HotelName", preBookReq.Descendants("HotelName").FirstOrDefault().Value), new XElement("Status", AvlStatus),
                                                new XElement("TermCondition", termsCondition), new XElement("HotelImgSmall"), new XElement("HotelImgLarge"),
                                                new XElement("MapLink"), new XElement("DMC", dmc), new XElement("Currency", currency),
                                                new XElement("Offers"), groupDetails));

                    if (TotalRateOld == TotalRate)
                    {
                        PreBookResponse.Add(new XElement(soapenv + "Body", preBookReqest, new XElement("HotelPreBookingResponse", new XElement("NewPrice", ""), hoteldata)));
                    }
                    else
                    {
                        PreBookResponse.Add(new XElement(soapenv + "Body", preBookReqest, new XElement("HotelPreBookingResponse", new XElement("ErrorTxt", "Amount has been changed"), new XElement("NewPrice", TotalRate), hoteldata)));
                    }
                }
                return PreBookResponse;

            }
            catch (Exception ex)
            {
                APILogDetail log = new APILogDetail();
                log.customerID = preBookReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 4;
                log.LogType = "PreBook";
                log.SupplierID = SupplierId;
                log.TrackNumber = preBookReq.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = hoojRequest;
                log.logresponseXML = hoojResponse;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);

                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelPreBooking";
                ex1.PageName = "Hoojoozat";
                ex1.CustomerID = preBookReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = preBookReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                PreBookResponse.Add(new XElement(soapenv + "Body", preBookReqest, new XElement("HotelPreBookingResponse", new XElement("ErrorTxt", "Room is not available"))));
                return PreBookResponse;
            }
        }

        #endregion


        #region Cancellation Policy
        public XElement CancellationPolicy(XElement cxlPolicyReq)
        {
            XElement CxlPolicyReqest = cxlPolicyReq.Descendants("hotelcancelpolicyrequest").FirstOrDefault();
            XElement CxlPolicyResponse = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XElement("Authentication", new XElement("AgentID", cxlPolicyReq.Descendants("AgentID").FirstOrDefault().Value), new XElement("UserName", cxlPolicyReq.Descendants("UserName").FirstOrDefault().Value),
                                       new XElement("Password", cxlPolicyReq.Descendants("Password").FirstOrDefault().Value), new XElement("ServiceType", cxlPolicyReq.Descendants("ServiceType").FirstOrDefault().Value),
                                       new XElement("ServiceVersion", cxlPolicyReq.Descendants("ServiceVersion").FirstOrDefault().Value))));


            try
            {
                CxlPolicyResponse.Add(new XElement(soapenv + "Body", cxlPolicyReq, new XElement("HotelDetailwithcancellationResponse",
                        new XElement("Hotels", new XElement("Hotel", new XElement("HotelID", cxlPolicyReq.Descendants("HotelID").FirstOrDefault().Value),
                        new XElement("HotelName"), new XElement("HotelImgSmall"), new XElement("HotelImgLarge"), new XElement("MapLink"),
                        new XElement("DMC", "Hoojoozat"), new XElement("Currency"), new XElement("Offers"),
                        new XElement("Rooms", new XElement("Room", new XAttribute("ID", cxlPolicyReq.Descendants("Room").FirstOrDefault().Attribute("ID").Value),
                        new XAttribute("RoomType", ""), new XAttribute("PerNightRoomRate", cxlPolicyReq.Descendants("PerNightRoomRate").FirstOrDefault().Value),
                        new XAttribute("TotalRoomRate", cxlPolicyReq.Descendants("TotalRoomRate").FirstOrDefault().Value),
                        new XAttribute("LastCancellationDate", ""), GetCxlPolicy(cxlPolicyReq.Descendants("Room").ToList(), cxlPolicyReq.Descendants("FromDate").FirstOrDefault().Value.ConvertToDate()))))))));
                return CxlPolicyResponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelCancellationPolicy";
                ex1.PageName = "Hoojoozat";
                ex1.CustomerID = cxlPolicyReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = cxlPolicyReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                CxlPolicyResponse.Add(new XElement(soapenv + "Body", CxlPolicyReqest, new XElement("HotelDetailwithcancellationResponse", new XElement("ErrorTxt", "No cancellation policy found"))));
                return CxlPolicyResponse;

            }
        }


        private XElement GetCxlPolicy(List<XElement> roomList, DateTime CheckInDate)
        {
            Dictionary<DateTime, decimal> cxlPolicies = new Dictionary<DateTime, decimal>();
            DateTime lastCxldate = DateTime.MaxValue.Date;
            string policyTxt = string.Empty;
            try
            {
                foreach (var room in roomList)
                {
                    decimal roomprice = 0.0m;
                    decimal cxlCharges = 0.0m;
                    DateTime Cxldate;
                    if (room.Attribute("TotalRoomRate") != null)
                    {
                        policyTxt = string.Empty;
                        roomprice = room.Attribute("TotalRoomRate").Value.ModifyToDecimal();
                    }
                    else
                    {
                        roomprice = room.Descendants("TotalPrice").FirstOrDefault().Value.ModifyToDecimal();
                        policyTxt = room.Descendants("CancellationPolicy").FirstOrDefault().Value;
                    }
                    if (policyTxt == "non-refundable" || string.IsNullOrEmpty(policyTxt))
                    {
                        lastCxldate = DateTime.Now.AddDays(-1).Date;
                        Cxldate = DateTime.Now.Date;
                        cxlCharges = roomprice;
                        cxlPolicies.AddCxlPolicy(Cxldate, cxlCharges);
                    }
                    else
                    {
                        if (policyTxt.Contains("days"))
                        {
                            int daysprior = HoojHelper.getWordInBetween(policyTxt, "done", "days").Split('-')[0].ModifyToInt();
                            Cxldate = CheckInDate.AddDays(-daysprior).Date;
                            if (Cxldate.AddDays(-1) < lastCxldate)
                            {
                                lastCxldate = Cxldate.AddDays(-1);
                            }
                            decimal charge_per = HoojHelper.getWordInBetween(policyTxt, "with", "charges").Split('%')[0].ModifyToDecimal();
                            cxlCharges = roomprice * charge_per / 100;
                            cxlPolicies.AddCxlPolicy(Cxldate, cxlCharges);

                        }
                        else if (policyTxt.Contains("between "))
                        {
                            Cxldate = HoojHelper.getWordInBetween(policyTxt, "between", "and").ConvertToDate();
                            if (Cxldate.AddDays(-1) < lastCxldate)
                            {
                                lastCxldate = Cxldate.AddDays(-1);
                            }
                            if (policyTxt.Contains("with"))
                            {
                                decimal charge_per = HoojHelper.getWordInBetween(policyTxt, "with", "charges").Split('%')[0].ModifyToDecimal();
                                cxlCharges = roomprice * charge_per / 100;
                                cxlPolicies.AddCxlPolicy(Cxldate, cxlCharges);
                            }
                            else
                            {
                                string chrg = policyTxt.Substring(policyTxt.LastIndexOf('f') + 1);
                                cxlCharges = chrg.Split(' ')[1].ModifyToDecimal();
                                cxlPolicies.AddCxlPolicy(Cxldate, cxlCharges);
                            }
                        }
                        else if (policyTxt.Split("after").Count() > 1)
                        {
                            var plcArr = policyTxt.Split("after ");
                            for (int i = 1; i < plcArr.Count(); i++)
                            {
                                Cxldate = plcArr[i].Split(' ')[0].ConvertToDate();
                                if (Cxldate.AddDays(-1) < lastCxldate)
                                {
                                    lastCxldate = Cxldate.AddDays(-1);
                                }
                                if (plcArr[i].Contains("%"))
                                {
                                    decimal charge_per = HoojHelper.getWordInBetween(policyTxt, "with", "charges").Split('%')[0].ModifyToDecimal();
                                    cxlCharges = roomprice * charge_per / 100;
                                }
                                else
                                {
                                    cxlCharges = plcArr[i].Split("of ")[1].Split(' ')[0].ModifyToDecimal();

                                }
                                cxlPolicies.AddCxlPolicy(Cxldate, cxlCharges);
                            }
                        }
                        else
                        {
                            Cxldate = DateTime.Now.Date;
                            if (Cxldate.AddDays(-1) < lastCxldate)
                            {
                                lastCxldate = Cxldate.AddDays(-1);
                            }
                            cxlCharges = room.Descendants("TotalPrice").FirstOrDefault().Value.ModifyToDecimal();
                            cxlPolicies.AddCxlPolicy(Cxldate, cxlCharges);
                        }
                    }

                }
                cxlPolicies.Add(lastCxldate, 0);
                XElement cxlplcy = new XElement("CancellationPolicies", from polc in cxlPolicies.OrderBy(k => k.Key) select new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", polc.Key.ToString("dd'/'MM'/'yyyy")), new XAttribute("ApplicableAmount", polc.Value), new XAttribute("NoShowPolicy", "0")));
                return cxlplcy;
            }
            catch (Exception ex)
            {
                decimal cxlCharges = 0.0m;
                DateTime Cxldate;
                Cxldate = DateTime.Now.Date;
                if (Cxldate.AddDays(-1) < lastCxldate)
                {
                    lastCxldate = Cxldate.AddDays(-1);
                }
                foreach (var room in roomList)
                {
                    cxlCharges += room.Descendants("TotalPrice").FirstOrDefault().Value.ModifyToDecimal();
                }
                cxlPolicies.Add(lastCxldate, 0);
                cxlPolicies.AddCxlPolicy(Cxldate, cxlCharges);
                XElement cxlplcy = new XElement("CancellationPolicies", from polc in cxlPolicies.OrderBy(k => k.Key) select new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", polc.Key.ToString("dd'/'MM'/'yyyy")), new XAttribute("ApplicableAmount", polc.Value), new XAttribute("NoShowPolicy", "0")));
                return cxlplcy;
            }

        }
        #endregion

        #region Hotel Booking
        public XElement HotelBooking(XElement BookingReq)
        {
            string hoojRequest = string.Empty;
            string hoojResponse = string.Empty;
            string hotelbooking = string.Empty;
            XElement BookReq = BookingReq.Descendants("HotelBookingRequest").FirstOrDefault();
            XElement HotelBookingRes = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XElement("Authentication", new XElement("AgentID", BookingReq.Descendants("AgentID").FirstOrDefault().Value), new XElement("UserName", BookingReq.Descendants("UserName").FirstOrDefault().Value), new XElement("Password", BookingReq.Descendants("Password").FirstOrDefault().Value),
                                       new XElement("ServiceType", BookingReq.Descendants("ServiceType").FirstOrDefault().Value), new XElement("ServiceVersion", BookingReq.Descendants("ServiceVersion").FirstOrDefault().Value))));
            try
            {
                string soapResult = string.Empty;
                List<string> tokenList = new List<string>();
                string purchasetoken = string.Empty;
                string PassengerDetails = string.Empty;
                string sessionNB = BookingReq.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                string refNB = BookingReq.Descendants("Room").FirstOrDefault().Descendants("RequestID").FirstOrDefault().Value;
                foreach (var room in BookingReq.Descendants("Room"))
                {
                    string token = room.Attribute("OccupancyID").Value;
                    if (!tokenList.Contains(token))
                    {
                        tokenList.Add(token);
                    }
                    foreach (var guest in room.Descendants("PaxInfo"))
                    {
                        string age = guest.Descendants("GuestType").FirstOrDefault().Value == "Adult" ? "-1" : guest.Descendants("Age").FirstOrDefault().Value;
                        if (string.IsNullOrEmpty(PassengerDetails))
                        {
                            PassengerDetails = token + "_" + guest.Descendants("FirstName").FirstOrDefault().Value + "_" + guest.Descendants("LastName").FirstOrDefault().Value + "_" + age;
                        }
                        else
                        {
                            PassengerDetails += ";" + token + "_" + guest.Descendants("FirstName").FirstOrDefault().Value + "_" + guest.Descendants("LastName").FirstOrDefault().Value + "_" + age;

                        }
                    }
                }
                foreach (var ptoken in tokenList)
                {
                    if (string.IsNullOrEmpty(purchasetoken))
                    {
                        purchasetoken = ptoken;
                    }
                    else
                    {
                        purchasetoken += ";" + ptoken;
                    }
                }
                HttpWebRequest request = CreateWebRequest();
                XmlDocument SoapReq = new XmlDocument();
                XElement LeadGuest = BookingReq.Descendants("Room").FirstOrDefault().Descendants("PaxInfo").Where(x => x.Descendants("IsLead").FirstOrDefault().Value == "true").FirstOrDefault();
                SoapReq.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>  
                                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">  
                                <soap:Body><MakeBookingV2 xmlns=""http://hoojoozat.com/"">  
                                <AgentUsername>" + AgentCode + @"</AgentUsername><AgentPassword>" + AgentPassword + @"</AgentPassword> 
                                <SessionNB>" + sessionNB + @"</SessionNB>
                                <ReferenceNB>" + refNB + @"</ReferenceNB>
                                <RoomPurchaseToken>" + purchasetoken + @"</RoomPurchaseToken>
                                <Passengers>" + PassengerDetails + @"</Passengers>
                                <ClientFirstName>" + LeadGuest.Descendants("FirstName").FirstOrDefault().Value + @"</ClientFirstName>
                                <ClientLastName>" + LeadGuest.Descendants("LastName").FirstOrDefault().Value + @"</ClientLastName>
                                <ClientComments>" + BookingReq.Descendants("SpecialRemarks").FirstOrDefault().Value + @"</ClientComments>
                                <ClientReference>" + BookingReq.Descendants("TransactionID").FirstOrDefault().Value + @"</ClientReference>
                                </MakeBookingV2></soap:Body></soap:Envelope>");
                hoojRequest = SoapReq.InnerXml;
                using (Stream stream = request.GetRequestStream())
                {
                    SoapReq.Save(stream);
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                }
                XDocument xmlData = XDocument.Parse(soapResult);
                XElement resp = xmlData.Root.RemoveXmlns();
                XElement newResp = resp.Descendants("MakeBookingV2Result").FirstOrDefault();
                XElement resBook = XElement.Parse(newResp.Value);
                resBook = resBook.RemoveXmlns();
                hoojResponse = resBook.ToString();
                #region supplier log
                APILogDetail log = new APILogDetail();
                log.customerID = BookingReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 5;
                log.LogType = "Book";
                log.SupplierID = SupplierId;
                log.TrackNumber = BookingReq.Descendants("TransactionID").FirstOrDefault().Value;
                log.logrequestXML = hoojRequest;
                log.logresponseXML = hoojResponse;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion
                int errCnt = resBook.Descendants("Error").Count();
                if (errCnt > 0)
                {
                    bool res = CheckAndCancelFailedBooking(BookingReq.Descendants("CustomerID").FirstOrDefault().Value, BookingReq.Descendants("TransactionID").FirstOrDefault().Value, BookingReq.Descendants("FromDate").FirstOrDefault().Value, BookingReq.Descendants("ToDate").FirstOrDefault().Value);
                    HotelBookingRes.Add(new XElement(soapenv + "Body", BookReq, new XElement("HotelBookingResponse", new XElement("ErrorTxt", resBook.Descendants("Message").FirstOrDefault().Value))));
                    return HotelBookingRes;
                }
                else
                {
                    XElement BookingRes = new XElement("HotelBookingResponse",
                                           new XElement("Hotels", new XElement("HotelID", BookingReq.Descendants("HotelID").FirstOrDefault().Value),
                                           new XElement("HotelName", BookingReq.Descendants("HotelName").FirstOrDefault().Value),
                                           new XElement("FromDate", BookingReq.Descendants("FromDate").FirstOrDefault().Value),
                                           new XElement("ToDate", BookingReq.Descendants("ToDate").FirstOrDefault().Value),
                                           new XElement("AdultPax", BookingReq.Descendants("Rooms").Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value),
                                           new XElement("ChildPax", BookingReq.Descendants("Rooms").Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value),
                                           new XElement("TotalPrice", BookingReq.Descendants("TotalAmount").FirstOrDefault().Value), new XElement("CurrencyID"),
                                           new XElement("CurrencyCode", ""),
                                           new XElement("MarketID"), new XElement("MarketName"), new XElement("HotelImgSmall"), new XElement("HotelImgLarge"), new XElement("MapLink"), new XElement("VoucherRemark"),
                                           new XElement("TransID", BookingReq.Descendants("TransID").FirstOrDefault().Value),
                                           new XElement("ConfirmationNumber", resBook.Descendants("BookingLocator").FirstOrDefault().Value),
                                           new XElement("Status", "Success"),
                                           new XElement("PassengersDetail", new XElement("GuestDetails",
                                           from room in BookingReq.Descendants("Room")
                                           select new XElement("Room", new XAttribute("ID", room.Attribute("RoomTypeID").Value), new XAttribute("RoomType", room.Attribute("RoomType").Value), new XAttribute("ServiceID", ""),
                                           new XAttribute("MealPlanID", ""), new XAttribute("MealPlanName", ""),
                                           new XAttribute("MealPlanCode", ""), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", ""),
                                           new XAttribute("RoomStatus", "true"), new XAttribute("TotalRoomRate", ""),
                                           new XElement("RoomGuest", new XElement("GuestType", "Adult"), new XElement("Title"), new XElement("FirstName", room.Descendants("PaxInfo").FirstOrDefault().Descendants("FirstName").FirstOrDefault().Value),
                                           new XElement("MiddleName"), new XElement("LastName", room.Descendants("PaxInfo").FirstOrDefault().Descendants("LastName").FirstOrDefault().Value), new XElement("IsLead", "true"), new XElement("Age")),
                                           new XElement("Supplements"))
                                           ))));

                    HotelBookingRes.Add(new XElement(soapenv + "Body", BookReq, BookingRes));

                }
                return HotelBookingRes;

            }
            catch (Exception ex)
            {
                APILogDetail log = new APILogDetail();
                log.customerID = BookingReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 5;
                log.LogType = "Book";
                log.SupplierID = SupplierId;
                log.TrackNumber = BookingReq.Descendants("TransactionID").FirstOrDefault().Value;
                log.logrequestXML = hoojRequest;
                log.logresponseXML = hoojResponse;
                SaveAPILog savelog = new SaveAPILog();

                savelog.SaveAPILogs(log);
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelBooking";
                ex1.PageName = "Hoojoozat";
                ex1.CustomerID = BookingReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = BookingReq.Descendants("TransactionID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                HotelBookingRes.Add(new XElement(soapenv + "Body", BookReq, new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Booking can not be confirmed!"))));
                return HotelBookingRes;

            }

        }
        #endregion

        #region Booking Cancellation
        public XElement BookingCancellation(XElement cancelReq)
        {
            string hoojRequest = string.Empty;
            string hoojResponse = string.Empty;
            XElement CxlReq = cancelReq.Descendants("HotelCancellationRequest").FirstOrDefault();
            XElement BookCXlRes = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                  new XElement("Authentication", new XElement("AgentID", cancelReq.Descendants("AgentID").FirstOrDefault().Value), new XElement("UserName", cancelReq.Descendants("UserName").FirstOrDefault().Value),
                                  new XElement("Password", cancelReq.Descendants("Password").FirstOrDefault().Value), new XElement("ServiceType", cancelReq.Descendants("ServiceType").FirstOrDefault().Value),
                                  new XElement("ServiceVersion", cancelReq.Descendants("ServiceVersion").FirstOrDefault().Value))));

            try
            {
                string soapResult = string.Empty;
                HttpWebRequest request = CreateWebRequest();
                XmlDocument SoapReq = new XmlDocument();
                SoapReq.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>  
                                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">  
                                <soap:Body><CancelBooking xmlns=""http://hoojoozat.com/"">  
                                <AgentUsername>" + AgentCode + @"</AgentUsername><AgentPassword>" + AgentPassword + @"</AgentPassword> 
                                <BookingLocator>" + cancelReq.Descendants("ConfirmationNumber").FirstOrDefault().Value + @"</BookingLocator>
                                <Type>C</Type>
                                </CancelBooking></soap:Body></soap:Envelope>");
                hoojRequest = SoapReq.InnerXml;
                using (Stream stream = request.GetRequestStream())
                {
                    SoapReq.Save(stream);
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                }
                XDocument xmlData = XDocument.Parse(soapResult);
                XElement resp = xmlData.Root.RemoveXmlns();
                XElement newResp = resp.Descendants("CancelBookingResult").FirstOrDefault();
                XElement resBokCancel = XElement.Parse(newResp.Value);
                resBokCancel = resBokCancel.RemoveXmlns();
                hoojResponse = resBokCancel.ToString();
                #region supplier log
                APILogDetail log = new APILogDetail();
                log.customerID = cancelReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 6;
                log.LogType = "Cancel";
                log.SupplierID = SupplierId;
                log.TrackNumber = cancelReq.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = hoojRequest;
                log.logresponseXML = hoojResponse;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion
                int errCnt = resBokCancel.Descendants("Error").Count();
                if (errCnt > 0)
                {

                    BookCXlRes.Add(new XElement(soapenv + "Body", CxlReq, new XElement("HotelCancellationResponse", new XElement("ErrorTxt", resBokCancel.Descendants("Message").FirstOrDefault().Value))));
                    return BookCXlRes;
                }
                else
                {
                    BookCXlRes.Add(new XElement(soapenv + "Body", CxlReq, new XElement("HotelCancellationResponse", new XElement("Rooms", new XElement("Room", new XElement("Cancellation", new XElement("Amount", ""), new XElement("Status", resBokCancel.Descendants("Status").FirstOrDefault().Value == "Cancelled" ? "Success" : "Fail")))))));
                }
                return BookCXlRes;
            }
            catch (Exception ex)
            {
                #region Insert supplier log if got exception from supplier
                APILogDetail log = new APILogDetail();
                log.customerID = cancelReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 6;
                log.LogType = "Cancel";
                log.SupplierID = SupplierId;
                log.TrackNumber = cancelReq.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = hoojRequest;
                log.logresponseXML = hoojResponse;
                SaveAPILog savelogc = new SaveAPILog();
                savelogc.SaveAPILogs(log);
                #endregion
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "BookingCancellation";
                ex1.PageName = "Hoojoozat";
                ex1.CustomerID = cancelReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = cancelReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                BookCXlRes.Add(new XElement(soapenv + "Body", CxlReq, new XElement("HotelCancellationResponse", new XElement("ErrorTxt", "There is some technical error"))));
                return BookCXlRes;
            }
        }

        #endregion
        #region Check and cancel if Booking failed
        public bool CheckAndCancelFailedBooking(string CustomerId,string TransId, string FromDate, string ToDate)
        {
            bool flag = false;
            string hoojRequest = string.Empty;
            string hoojResponse = string.Empty;
            try
            {
                string soapResult = string.Empty;
                HttpWebRequest request = CreateWebRequest();
                XmlDocument SoapReq = new XmlDocument();
                SoapReq.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>  
                                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">  
                                <soap:Body><CheckBooking xmlns=""http://hoojoozat.com/"">  
                                <AgentUsername>" + AgentCode + @"</AgentUsername><AgentPassword>" + AgentPassword + @"</AgentPassword> 
                                <BookingLocator></BookingLocator><FromDate>" + FromDate.HoojDateString() + @"</FromDate><ToDate>"+ToDate.HoojDateString()+@"</ToDate><DateType></DateType>
                                <ClientBookingReference>" + TransId + @"</ClientBookingReference>
                                </CheckBooking></soap:Body></soap:Envelope>");
                hoojRequest = SoapReq.InnerXml;
                using (Stream stream = request.GetRequestStream())
                {
                    SoapReq.Save(stream);
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                }
                XDocument xmlData = XDocument.Parse(soapResult);
                XElement resp = xmlData.Root.RemoveXmlns();
                XElement newResp = resp.Descendants("CheckBookingResult").FirstOrDefault();
                XElement resBokCheck = XElement.Parse(newResp.Value);
                resBokCheck = resBokCheck.RemoveXmlns();
                hoojResponse = resBokCheck.ToString();
                #region supplier log
                APILogDetail log = new APILogDetail();
                log.customerID = CustomerId.ConvertToLong();
                log.LogTypeID =8;
                log.LogType = "CheckBooking";
                log.SupplierID = SupplierId;
                log.TrackNumber = TransId;
                log.logrequestXML = hoojRequest;
                log.logresponseXML = hoojResponse;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion
                int errCnt = resBokCheck.Descendants("Error").Count();
                if (errCnt > 0)
                {
                    flag = false;
                }
                else
                {
                    string SuplConfNo = resBokCheck.Element("BookingLocator").Value;
                    if (!string.IsNullOrEmpty(SuplConfNo))
                    {
                        CancelFailedBooking(CustomerId, TransId, SuplConfNo);
                    }
                    flag = true;
                }
                return flag;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAndCancelFailedBooking";
                ex1.PageName = "Hoojoozat";
                ex1.CustomerID = CustomerId;
                ex1.TranID = TransId;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return false;
            }
        }
        #endregion
        #region Cancel Failed Booking
        public void CancelFailedBooking(string CustomerId, string TransId, string SuplConfNo)
        {
            string hoojRequest = string.Empty;
            string hoojResponse = string.Empty;
            XmlDocument SoapReq = new XmlDocument();
            string soapResult = string.Empty;
            try
            {
                HttpWebRequest request = CreateWebRequest();
                SoapReq.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>  
                                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">  
                                <soap:Body><CancelBooking xmlns=""http://hoojoozat.com/"">  
                                <AgentUsername>" + AgentCode + @"</AgentUsername><AgentPassword>" + AgentPassword + @"</AgentPassword> 
                                <BookingLocator>" + SuplConfNo + @"</BookingLocator>
                                <Type>C</Type>
                                </CancelBooking></soap:Body></soap:Envelope>");
                hoojRequest = SoapReq.InnerXml;
                using (Stream stream = request.GetRequestStream())
                {
                    SoapReq.Save(stream);
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                }
                XDocument xmlData = XDocument.Parse(soapResult);
                XElement resp = xmlData.Root.RemoveXmlns();
                XElement newResp = resp.Descendants("CancelBookingResult").FirstOrDefault();
                XElement resBokCancel = XElement.Parse(newResp.Value);
                resBokCancel = resBokCancel.RemoveXmlns();
                hoojResponse = resBokCancel.ToString();
                #region supplier log
                APILogDetail log = new APILogDetail();
                log.customerID = CustomerId.ConvertToLong();
                log.LogTypeID = 6;
                log.LogType = "Cancel";
                log.SupplierID = SupplierId;
                log.TrackNumber = TransId;
                log.logrequestXML = hoojRequest;
                log.logresponseXML = hoojResponse;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancelFailedBooking";
                ex1.PageName = "Hoojoozat";
                ex1.CustomerID = CustomerId;
                ex1.TranID = TransId;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
           }
            
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