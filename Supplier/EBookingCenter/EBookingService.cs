using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Data;
using System.Data.SqlClient;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Supplier.EBookingCenter;
using TravillioXMLOutService.Models.EBookingCenter;
using TravillioXMLOutService.Models.Common;
using TravillioXMLOutService.Common;


namespace TravillioXMLOutService.Supplier.EBookingCenter
{
    public class EBookingService
    {
        #region Credentails
        string SellerId = string.Empty;
        string UserName = string.Empty;
        string Password = string.Empty;
        string Url = string.Empty;
        string customerid = string.Empty;
        #endregion
        #region Global vars
        string dmc = string.Empty;
        const int SupplierId = 47;
        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
        XNamespace ns = XNamespace.Get("http://tbs.dcsplus.net/ws/1.0/");
        EBookingRequest srvRequest;
        #endregion
        public EBookingService(string _customerid)
        {
            XElement suppliercred = supplier_Cred.getsupplier_credentials(_customerid, "47");
            try
            {
                UserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
                Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                SellerId = suppliercred.Descendants("SellerId").FirstOrDefault().Value;
                Url = suppliercred.Descendants("URL").FirstOrDefault().Value;
            }
            catch { }
        }
        public EBookingService()
        {

        }
        #region Hotel Availability
        public List<XElement> HotelAvailability(XElement req, XElement Nationality, string custID, string xtype)
        {
            dmc = xtype;
            customerid = custID;
            string ebookRequest = string.Empty;
            string ebookResponse = string.Empty;
            string soapResult = string.Empty;
            string htlCode = string.Empty;
            List<XElement> HotelsData = new List<XElement>();
            try
            {
                EBooking_StaticData ebookStatic = new EBooking_StaticData();
                DataTable ebookCity = ebookStatic.GetCityCode(req.Descendants("CityID").FirstOrDefault().Value, 47);
                List<int> starList = new List<int>();
                for (int i = req.Descendants("MinStarRating").FirstOrDefault().Value.ModifyToInt(); i <= req.Descendants("MaxStarRating").FirstOrDefault().Value.ModifyToInt(); i++)
                {
                    starList.Add(i);
                }
                string nationalityId = string.Empty;
                if (Nationality != null)
                {
                    nationalityId = Nationality.Descendants(ns + "Nationality").Where(x => x.Attribute("ISO").Value == req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value).FirstOrDefault().Attribute("ID").Value;
                }
                else
                {
                    nationalityId = "1";
                }
                string HtId = req.Descendants("HotelID").FirstOrDefault().Value;


                XElement htlItem=null;

                if (!string.IsNullOrEmpty(req.Descendants("HotelID").FirstOrDefault().Value) || !string.IsNullOrEmpty(req.Descendants("HotelName").FirstOrDefault().Value))
                {

                    int CityId = Convert.ToInt32(ebookCity.Rows[0]["CityCode"].ToString());
                    var model = new SqlModel()
                    {
                        flag = 2,
                        columnList = "HotelId,HotelName,StarRating",
                        table = "EBookingHotelList",
                        filter = "CityId=" + CityId.ToString() + " AND HotelName LIKE '%" + req.Descendants("HotelName").FirstOrDefault().Value + "%'",
                        SupplierId = SupplierId
                    };
                    if (!string.IsNullOrEmpty(req.Descendants("HotelID").FirstOrDefault().Value))
                    {
                        model.HotelCode = req.Descendants("HotelID").FirstOrDefault().Value;
                    }
                    DataTable htlList = TravayooRepository.GetData(model);

                    if (htlList.Rows.Count > 0)
                    {
                        var result = htlList.AsEnumerable().Select(y => new XElement("productCode", y.Field<string>("HotelID")));
                        htlItem = new XElement("productCodes", result);
                    }
                    else
                    {
                        //throw new Exception("There is no hotel available in database");
                        return null;
                    }
                }
            
                srvRequest = new EBookingRequest();
                XmlDocument SoapReq = new XmlDocument();

                XElement searchRequest = new XElement(soapenv + "Envelope",
                     new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                     new XAttribute(XNamespace.Xmlns + "ns", "http://tbs.dcsplus.net/ws/1.0/"),
                     new XElement(soapenv + "Header",
                     new XElement(ns + "AuthHeader", new XElement(ns + "ResellerCode", SellerId), new XElement(ns + "Username", UserName), new XElement(ns + "Password", Password), new XElement(ns + "ApplicationId"))),
                     new XElement(soapenv + "Body",
                        new XElement(ns + "HotelGetAvailabilityXMLRQ", new XAttribute("AddHotelDetails", "false"), new XAttribute("AddHotelMinPrice", "true"), new XAttribute("IgnoreHotelOffers", "false"),
                        (htlItem != null) ?
                        new XElement(ns + "Location", new XElement(ns + "City", new XAttribute("CityID", ebookCity.Rows[0]["CityCode"].ToString()), new XAttribute("CityCode", ""),
                        from htl in htlItem.Descendants("productCode") select new XElement(ns + "Hotel", new XAttribute("HotelID", htl.Value))))
                        : new XElement(ns + "Location", new XElement(ns + "City", new XAttribute("CityID", ebookCity.Rows[0]["CityCode"].ToString()), new XAttribute("CityCode", ""))),
                        new XElement(ns + "DateRange", new XAttribute("Start", req.Descendants("FromDate").FirstOrDefault().Value.EBookDateString()), new XAttribute("End", req.Descendants("ToDate").FirstOrDefault().Value.EBookDateString())),
                        new XElement(ns + "Rooms", from rpax in req.Descendants("RoomPax")
                                                   select new XElement(ns + "Room", new XAttribute("Adults", rpax.Descendants("Adult").FirstOrDefault().Value), new XAttribute("Children", rpax.Descendants("Child").FirstOrDefault().Value),
                                                   from chpax in rpax.Descendants("ChildAge") select new XElement(ns + "ChildAge", chpax.Value))),
                        new XElement(ns + "Filters", new XElement(ns + "Nationality", new XAttribute("ID", nationalityId)), new XElement(ns + "SellingChannel"), new XElement(ns + "AvailableOnly", "1"), new XElement(ns + "HotelStars", from star in starList select new XElement(ns + "Rating", star)), new XElement(ns + "CompleteOffersOnly"), new XElement(ns + "System"))
                    )));

               
                LogModel logmodel = new LogModel()
                {
                    TrackNo = req.Descendants("TransID").FirstOrDefault().Value,
                    CustomerID = customerid.ConvertToLong(),
                    SuplId = SupplierId,
                    LogType = "Search",
                    LogTypeID = 1
                };
                XDocument respHTL = srvRequest.ServerRequest(searchRequest.ToString(), Url, logmodel);
                if (respHTL.Descendants("Hotel").Count() == 0)
                {
                    return null;
                }
                else
                {
                    DataTable eBookHotels = ebookStatic.GetStaticHotels(ebookCity.Rows[0]["CityCode"].ToString(), ebookCity.Rows[0]["CountryCode"].ToString(), req.Descendants("MinStarRating").FirstOrDefault().Value.ModifyToInt(), req.Descendants("MaxStarRating").FirstOrDefault().Value.ModifyToInt());
                    string xmlouttype = string.Empty;
                    try
                    {
                        if (dmc == "EBookingCenter")
                        {
                            xmlouttype = "false";
                        }
                        else
                        { xmlouttype = "true"; }
                    }
                    catch { }
                    foreach (var hotel in respHTL.Descendants("Hotel"))
                    {
                        var HotelData = eBookHotels.Select("[HotelId] = '" + hotel.Descendants("HotelDetails").FirstOrDefault().Attribute("ID").Value + "'").FirstOrDefault();
                        if (HotelData != null)
                        {
                            if (hotel.Descendants("HotelMinPrice").FirstOrDefault().Attribute("Amount").Value.ModifyToDecimal()>0)
                            {
                                XElement hoteldata = new XElement("Hotel", new XElement("HotelID", HotelData["HotelId"].ToString()),
                                                new XElement("HotelName", HotelData["HotelName"].ToString()),
                                                new XElement("PropertyTypeName"),
                                                new XElement("CountryID"),
                                                new XElement("CountryName", req.Descendants("CountryName").FirstOrDefault().Value),
                                                new XElement("CountryCode", HotelData["CountryId"].ToString()),
                                                new XElement("CityId"),
                                                new XElement("CityCode", HotelData["CityId"].ToString()),
                                                new XElement("CityName", req.Descendants("CityName").FirstOrDefault().Value),
                                                new XElement("AreaId"),
                                                new XElement("AreaName"),
                                                new XElement("RequestID"),
                                                new XElement("Address", ""),
                                                new XElement("Location", HotelData["Location"].ToString()),
                                                new XElement("Description"),
                                                new XElement("StarRating", HotelData["StarRating"].ToString()),
                                                new XElement("MinRate", hotel.Descendants("HotelMinPrice").FirstOrDefault().Attribute("Amount").Value),
                                                new XElement("HotelImgSmall", HotelData["Image"].ToString()),
                                                new XElement("HotelImgLarge", HotelData["Image"].ToString()),
                                                new XElement("MapLink"),
                                                new XElement("Longitude", HotelData["Longitude"].ToString()),
                                                new XElement("Latitude", HotelData["Latitude"].ToString()),
                                                new XElement("xmloutcustid", customerid),
                                                new XElement("xmlouttype", xmlouttype),
                                                new XElement("DMC", dmc), new XElement("SupplierID", SupplierId),
                                                new XElement("Currency", hotel.Descendants("HotelMinPrice").FirstOrDefault().Attribute("Currency").Value),
                                                new XElement("Offers"), new XElement("Facilities"),
                                                new XElement("Rooms")
                                                );

                                HotelsData.Add(hoteldata);
                            }
                        }

                    }
                }
                return HotelsData;
            }
            catch(Exception ex)
            {
                return null;
            }
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
                EBooking_StaticData eBookStatic = new EBooking_StaticData();
                DataTable HotelDetail = eBookStatic.GetHotelDetails(req.Descendants("HotelID").FirstOrDefault().Value);
                if (!string.IsNullOrEmpty(HotelDetail.Rows[0]["HotelId"].ToString()))
                {

                    hotelDescResdoc.Add(new XElement(soapenv + "Body", HotelDescReq, new XElement("hoteldescResponse", new XElement("Hotels", new XElement("Hotel", new XElement("HotelID", req.Descendants("HotelID").FirstOrDefault().Value),
                                        new XElement("Description", HotelDetail.Rows[0]["Description"].ToString()), HotelDetail.Rows[0]["Gallery"].ToString().getHotelImages(), XElement.Parse(HotelDetail.Rows[0]["Facility"].ToString()),
                                        new XElement("ContactDetails", new XElement("Phone", HotelDetail.Rows[0]["Phone"].ToString()), new XElement("Fax")),
                                        new XElement("CheckinTime"), new XElement("CheckoutTime")
                                        )))));
                }
                else
                {
                    srvRequest = new EBookingRequest();
                    XmlDocument SoapReq = new XmlDocument();
                    SoapReq.LoadXml(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ns=""http://tbs.dcsplus.net/ws/1.0/"">
                                                   <soapenv:Header><ns:AuthHeader><ns:ResellerCode>1774</ns:ResellerCode><ns:Username>plazatour1</ns:Username><ns:Password>6Tp~8Fe*</ns:Password><ns:ApplicationId/></ns:AuthHeader>
                                                   </soapenv:Header><soapenv:Body><ns:GetHotelDetailsRQ><ns:HotelID>" + req.Descendants("HotelID").FirstOrDefault().Value + "</ns:HotelID></ns:GetHotelDetailsRQ></soapenv:Body></soapenv:Envelope>");
                    string reqq = SoapReq.InnerXml.ToString();
                    XDocument xmlData = srvRequest.HotelDetailRequest(reqq, "");
                    if (xmlData.Descendants(ns + "HotelDetails").Count() > 0)
                    {

                        string address = xmlData.Descendants(ns + "Address").FirstOrDefault().Value;
                        string location = xmlData.Descendants(ns + "Location").Count() > 0 ? xmlData.Descendants(ns + "Location").FirstOrDefault().Value : string.Empty;
                        string image = xmlData.Descendants(ns + "Image").Count() > 0 ? xmlData.Descendants(ns + "Image").FirstOrDefault().Attribute("URL").Value : string.Empty;
                        string latitude = xmlData.Descendants(ns + "Position").Count() > 0 ? xmlData.Descendants(ns + "Position").FirstOrDefault().Attribute("Latitude").Value : string.Empty;
                        string longitude = xmlData.Descendants(ns + "Position").Count() > 0 ? xmlData.Descendants(ns + "Position").FirstOrDefault().Attribute("Longitude").Value : string.Empty;
                        string desc = (xmlData.Descendants(ns + "Descriptions").FirstOrDefault().Descendants(ns + "FullDescription").Count() > 0 ? xmlData.Descendants(ns + "Descriptions").FirstOrDefault().Descendants(ns + "FullDescription").FirstOrDefault().Value : string.Empty) + Environment.NewLine + (xmlData.Descendants(ns + "Descriptions").FirstOrDefault().Descendants(ns + "Location").Count() > 0 ? xmlData.Descendants(ns + "Descriptions").FirstOrDefault().Descendants(ns + "Location").FirstOrDefault().Value : string.Empty) + Environment.NewLine + (xmlData.Descendants(ns + "Descriptions").FirstOrDefault().Descendants(ns + "Facilities").Count() > 0 ? xmlData.Descendants(ns + "Descriptions").FirstOrDefault().Descendants(ns + "Facilities").FirstOrDefault().Value : string.Empty) + Environment.NewLine + (xmlData.Descendants(ns + "Descriptions").FirstOrDefault().Descendants(ns + "EssentialInformation").Count() > 0 ? xmlData.Descendants(ns + "Descriptions").FirstOrDefault().Descendants(ns + "EssentialInformation").FirstOrDefault().Value : string.Empty);
                        XElement gallery = new XElement("Images",
                            from img in xmlData.Descendants(ns + "GalleryImage").ToList()
                            select new XElement("Image", new XAttribute("Path", img.Attribute("URL").Value))
                            );
                        XElement facility = new XElement("Facilities",
                            from faci in xmlData.Descendants(ns + "Facility").ToList()
                            select new XElement("Facility", faci.Value)
                            );
                        string phone = xmlData.Descendants(ns + "Contact").Count() > 0 ? xmlData.Descendants(ns + "Contact").FirstOrDefault().Descendants(ns + "Phone").Count() > 0 ? xmlData.Descendants(ns + "Contact").FirstOrDefault().Descendants(ns + "Phone").FirstOrDefault().Value : string.Empty : string.Empty;
                        string email = xmlData.Descendants(ns + "Contact").Count() > 0 ? xmlData.Descendants(ns + "Contact").FirstOrDefault().Descendants(ns + "Email").Count() > 0 ? xmlData.Descendants(ns + "Contact").FirstOrDefault().Descendants(ns + "Email").FirstOrDefault().Value : string.Empty : string.Empty;
                        eBookStatic.InsertEBookHotelDetails(req.Descendants("HotelID").FirstOrDefault().Value, address, location, image, latitude, longitude, desc, gallery.ToString(), phone, email, facility.ToString());

                        hotelDescResdoc.Add(new XElement(soapenv + "Body", HotelDescReq, new XElement("hoteldescResponse", new XElement("Hotels", new XElement("Hotel", new XElement("HotelID", req.Descendants("HotelID").FirstOrDefault().Value),
                                       new XElement("Description", desc), gallery, facility),
                                       new XElement("ContactDetails", new XElement("Phone", phone), new XElement("Fax")),
                                       new XElement("CheckinTime"), new XElement("CheckoutTime")
                                       ))));
                    }


                }

                return hotelDescResdoc;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelDescription";
                ex1.PageName = "EBookingCenter";
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
        public XElement GetRoomAvail_ebookingcenterOUT(XElement req)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            XElement getrm = null;
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "47").ToList();
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
                        dmc = "EBookingCenter";
                    }
                    EBookingService rs = new EBookingService(customerid);
                    roomavailabilityresponse.Add(rs.RoomAvailability(req, dmc, htlid));
                }
                #endregion
                getrm = new XElement("TotalRooms", roomavailabilityresponse);
                return getrm;
            }
            catch { return null; }
        }
        public XElement RoomAvailability(XElement roomReq, string xtype, string htlid)
        {
            dmc = xtype;
            XElement searchReq = roomReq.Descendants("searchRequest").FirstOrDefault();
            XElement RoomDetails = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                   new XElement("Authentication", new XElement("AgentID", roomReq.Descendants("AgentID").FirstOrDefault().Value), new XElement("UserName", roomReq.Descendants("UserName").FirstOrDefault().Value), new XElement("Password", roomReq.Descendants("Password").FirstOrDefault().Value),
                                   new XElement("ServiceType", roomReq.Descendants("ServiceType").FirstOrDefault().Value), new XElement("ServiceVersion", roomReq.Descendants("ServiceVersion").FirstOrDefault().Value))));

            string soapResult = string.Empty;

            try
            {
                EBooking_StaticData ebookStatic = new EBooking_StaticData();
                soapResult = ebookStatic.GetSearchResponseXml(roomReq.Descendants("TransID").FirstOrDefault().Value, SupplierId);
                if (!string.IsNullOrEmpty(soapResult))
                {
                    int totalrooms = roomReq.Descendants("RoomPax").Count();
                    XElement groupDetails = new XElement("Rooms");
                    List<XElement> roomlst = new List<XElement>();
                    XElement respHtlXml = XElement.Parse(soapResult);
                    string ResultCode = respHtlXml.Attribute("ResultCode").Value;
                    string htlCode = htlid;
                    int nights = (int)(roomReq.Descendants("ToDate").FirstOrDefault().Value.ConvertToDate() - roomReq.Descendants("FromDate").FirstOrDefault().Value.ConvertToDate()).TotalDays;
                    XElement respHTL = respHtlXml.Descendants("Hotel").Where(x => x.Descendants("HotelDetails").FirstOrDefault().Attribute("ID").Value == htlCode).FirstOrDefault();
                    int grpIndex = 1;
                    foreach (var hoteloffer in respHTL.Descendants("HotelOffer").ToList())
                    {
                        foreach (var package in hoteloffer.Descendants("Package").ToList())
                        {
                            List<XElement> packagelist = package.Descendants("PackageRoom").ToList();
                            string packageCode = package.Attribute("PackageCode").Value;
                            decimal RoomGroupRate = package.Descendants("Price").FirstOrDefault().Attribute("Amount").Value.ModifyToDecimal();
                            XElement RoomType = new XElement("RoomTypes", new XAttribute("Index", grpIndex), new XAttribute("TotalRate", RoomGroupRate), new XAttribute("HtlCode", htlCode), new XAttribute("CrncyCode", package.Descendants("Price").FirstOrDefault().Attribute("Currency").Value), new XAttribute("DMCType", dmc));
                            int roomSeq = 1;
                            foreach (var roompax in roomReq.Descendants("RoomPax"))
                            {
                                var packageroom = packagelist.Find(x => x.Descendants("Occupancy").FirstOrDefault().Attribute("Adults").Value == roompax.Descendants("Adult").FirstOrDefault().Value && x.Descendants("Occupancy").FirstOrDefault().Attribute("Children").Value == roompax.Descendants("Child").FirstOrDefault().Value);
                                int indx = packagelist.FindIndex(x => x.Descendants("Occupancy").FirstOrDefault().Attribute("Adults").Value == roompax.Descendants("Adult").FirstOrDefault().Value && x.Descendants("Occupancy").FirstOrDefault().Attribute("Children").Value == roompax.Descendants("Child").FirstOrDefault().Value);
                                if (packageroom != null)
                                {
                                    string packageRoomCode = packageroom.Attribute("PackageRoomCode").Value;
                                    XElement roomref = packageroom.Descendants("RoomRef").Where(x => x.Attribute("Selected").Value == "true").FirstOrDefault();
                                    XElement roomInfo = hoteloffer.Descendants("Room").Where(x => x.Attribute("RoomCode").Value == roomref.Attribute("RoomCode").Value).FirstOrDefault();
                                    if (roomInfo.Descendants("Price").Count() > 0)
                                    {
                                        var sdr = 0;
                                    }
                                    decimal totalRoomRate = roomInfo.Descendants("Price").Count() > 0 ? roomInfo.Descendants("Price").FirstOrDefault().Attribute("Amount").Value.ModifyToDecimal() : RoomGroupRate / totalrooms;
                                    string otherInfo = roomInfo.Descendants("Info").Count() > 0 ? roomInfo.Descendants("Info").FirstOrDefault().Value.Contains("Promotion:") ? roomInfo.Descendants("Info").FirstOrDefault().Value.Split("Promotion:")[0] : roomInfo.Descendants("Info").FirstOrDefault().Value : string.Empty;
                                    string Promotion = roomInfo.Descendants("Info").Count() > 0 ? roomInfo.Descendants("Info").FirstOrDefault().Value.Contains("Promotion:") ? roomInfo.Descendants("Info").FirstOrDefault().Value.Split("Promotion:")[1] : string.Empty : string.Empty;
                                    XElement roomDtl = new XElement("Room", new XAttribute("ID", roomref.Attribute("RoomCode").Value), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", roomSeq), new XAttribute("SessionID", packageCode), new XAttribute("RoomType", roomInfo.Element("Name").Value), new XAttribute("OccupancyID", packageRoomCode),
                                                       new XAttribute("OccupancyName", ""), new XAttribute("MealPlanID", ResultCode), new XAttribute("MealPlanName", roomInfo.Element("Board") != null ? roomInfo.Element("Board").Value : "Room Only"), new XAttribute("MealPlanCode", ""), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", totalRoomRate / nights),
                                                       new XAttribute("TotalRoomRate", totalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", true),
                                                       new XElement("RequestID"), new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions", Promotion != null ? Promotion : string.Empty)),
                                                       new XElement("CancellationPolicy"), new XElement("Amenities", new XElement("Amenity")),
                                                       new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"),
                                                       new XElement(getPriceBreakup(nights, totalRoomRate)),
                                                       new XElement("AdultNum", packageroom.Descendants("Occupancy").FirstOrDefault().Attribute("Adults").Value),
                                                       new XElement("ChildNum", packageroom.Descendants("Occupancy").FirstOrDefault().Attribute("Children").Value));
                                    RoomType.Add(roomDtl);
                                    roomSeq++;
                                    packagelist.RemoveAt(indx);
                                }

                            }
                            roomlst.Add(RoomType);
                            grpIndex++;
                        }
                    }

                    foreach (var roomgrp in roomlst)
                    {
                        if (roomgrp.Elements("Room").Count() == totalrooms)
                        {
                            groupDetails.Add(roomgrp);
                        }
                    }
                    XElement hoteldata = new XElement("Hotels", new XElement("Hotel", new XElement("HotelID"), new XElement("HotelName"), new XElement("PropertyTypeName"),
                                         new XElement("CountryID"), new XElement("CountryName"), new XElement("CityCode"), new XElement("CityName"),
                                         new XElement("AreaId"), new XElement("AreaName"), new XElement("RequestID"), new XElement("Address"), new XElement("Location"),
                                         new XElement("Description"), new XElement("StarRating"), new XElement("MinRate"), new XElement("HotelImgSmall"),
                                         new XElement("HotelImgLarge"), new XElement("MapLink"), new XElement("Longitude"), new XElement("Latitude"), new XElement("DMC"),
                                         new XElement("SupplierID"), new XElement("Currency"), new XElement("Offers"),
                                         new XElement(groupDetails)));
                    RoomDetails.Add(new XElement(soapenv + "Body", searchReq, new XElement("searchResponse", hoteldata)));
                }
                else
                {
                    RoomDetails.Add(new XElement(soapenv + "Body", searchReq, new XElement("searchResponse", new XElement("ErrorTxt", "Room is not available"))));
                }
                APILogDetail log = new APILogDetail();
                log.customerID = roomReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 2;
                log.LogType = "RoomAvail";
                log.SupplierID = SupplierId;
                log.TrackNumber = roomReq.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = roomReq.ToString();
                log.logresponseXML = RoomDetails.ToString();
                APILog.SaveAPILogs(log);
                return RoomDetails;
            }
            catch(Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "RoomAvailability";
                ex1.PageName = "EBookingCenter";
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
            decimal nightPrice = Math.Round(roomPrice / nights, 5);
            for (int i = 1; i <= nights; i++)
            {
                pricebrk.Add(new XElement("Price", new XAttribute("Night", i), new XAttribute("PriceValue", nightPrice)));

            }
            return pricebrk;
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
                srvRequest = new EBookingRequest();
                int nights = (int)(cxlPolicyReq.Descendants("ToDate").FirstOrDefault().Value.ConvertToDate() - cxlPolicyReq.Descendants("FromDate").FirstOrDefault().Value.ConvertToDate()).TotalDays;
                string ResultCode = cxlPolicyReq.Descendants("Room").FirstOrDefault().Attribute("MealPlanID").Value;
                string PackageCode = cxlPolicyReq.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                string soapResult = string.Empty;
                XElement cxlPolicyRequest = new XElement(soapenv + "Envelope",
                  new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                  new XAttribute(XNamespace.Xmlns + "ns", "http://tbs.dcsplus.net/ws/1.0/"),
                  new XElement(soapenv + "Header",
                  new XElement(ns + "AuthHeader", new XElement(ns + "ResellerCode", SellerId), new XElement(ns + "Username", UserName), new XElement(ns + "Password", Password), new XElement(ns + "ApplicationId"))),
                  new XElement(soapenv + "Body",
                     new XElement(ns + "HotelGetOfferDetailsRQ",
                     new XElement(ns + "ResultCode", ResultCode),
                     new XElement(ns + "PackageCode", PackageCode),
                     new XElement(ns + "PackageRooms",
                     from room in cxlPolicyReq.Descendants("Room") select new XElement(ns + "PackageRoom", new XAttribute("PackageRoomCode", room.Attribute("OccupancyID").Value), new XAttribute("RoomCode", room.Attribute("ID").Value)))
                    )));
                LogModel logmodel = new LogModel()
                {
                    TrackNo = cxlPolicyReq.Descendants("TransID").FirstOrDefault().Value,
                    CustomerID = cxlPolicyReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong(),
                    SuplId = SupplierId,
                    LogType = "CXLPolicy",
                    LogTypeID = 3
                };
                XDocument respCxlPolicy = srvRequest.ServerRequest(cxlPolicyRequest.ToString(), Url, logmodel);
                decimal RoomGroupRate = respCxlPolicy.Descendants(ns+"Package").FirstOrDefault().Descendants(ns+"Price").FirstOrDefault().Attribute("Amount").Value.ModifyToDecimal();
           
                CxlPolicyResponse.Add(new XElement(soapenv + "Body", cxlPolicyReq, new XElement("HotelDetailwithcancellationResponse",
                        new XElement("Hotels", new XElement("Hotel", new XElement("HotelID", cxlPolicyReq.Descendants("HotelID").FirstOrDefault().Value),
                        new XElement("HotelName"), new XElement("HotelImgSmall"), new XElement("HotelImgLarge"), new XElement("MapLink"),
                        new XElement("DMC", "EBookingCenter"), new XElement("Currency"), new XElement("Offers"),
                        new XElement("Rooms", new XElement("Room", new XAttribute("ID", cxlPolicyReq.Descendants("Room").FirstOrDefault().Attribute("ID").Value),
                        new XAttribute("RoomType", ""), new XAttribute("PerNightRoomRate", cxlPolicyReq.Descendants("PerNightRoomRate").FirstOrDefault().Value),
                        new XAttribute("TotalRoomRate", cxlPolicyReq.Descendants("TotalRoomRate").FirstOrDefault().Value),
                        new XAttribute("LastCancellationDate", ""),
                        GetCxlPolicy(respCxlPolicy.Descendants(ns + "Policy").ToList(), RoomGroupRate, cxlPolicyReq.Descendants("FromDate").FirstOrDefault().Value.ConvertToDate())
                        )))))));
                return CxlPolicyResponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancellationPolicy";
                ex1.PageName = "EBookingCenter";
                ex1.CustomerID = cxlPolicyReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = cxlPolicyReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                CxlPolicyResponse.Add(new XElement(soapenv + "Body", CxlPolicyReqest, new XElement("HotelDetailwithcancellationResponse", new XElement("ErrorTxt", "No cancellation policy found"))));
                return CxlPolicyResponse;

            }
        }
        #region Get Cancellation Policy Element
        private XElement GetCxlPolicy(List<XElement> policyList, decimal TotalPrice, DateTime CheckInDate)
        {
            Dictionary<DateTime, decimal> cxlPolicies = new Dictionary<DateTime, decimal>();
            DateTime lastCxldate = DateTime.MaxValue.Date;
            string policyTxt = string.Empty;
            try
            {
                foreach (var policy in policyList)
                {
                    decimal cxlCharges = 0.0m;
                    DateTime Cxldate;
                    if (policy.Attribute("Type").Value == "standard" || policy.Attribute("Type").Value == "noshow")
                    {
                        Cxldate = policy.Attribute("Limit").Value.EBookCxlDate();
                        if (Cxldate.AddDays(-1) < lastCxldate)
                        {
                            lastCxldate = Cxldate.AddDays(-1);
                        }
                        cxlCharges = policy.Descendants(ns + "Charge").FirstOrDefault().Attribute("Amount").Value.ModifyToDecimal();
                        cxlPolicies.Add(Cxldate, cxlCharges);
                    }
                    else if (policy.Attribute("Type").Value == "limit" || policy.Attribute("Type").Value == "lock")
                    {
                        Cxldate = policy.Attribute("Limit").Value.EBookCxlDate();
                        if (cxlPolicies.ContainsKey(Cxldate))
                        {
                            if (cxlPolicies[Cxldate] < TotalPrice)
                            {
                                cxlPolicies[Cxldate] = TotalPrice;
                            }
                        }
                        else
                        {
                            cxlPolicies.Add(Cxldate, TotalPrice);
                        }
                    }
                }
                cxlPolicies.Add(lastCxldate, 0);
                XElement cxlplcy = new XElement("CancellationPolicies", from polc in cxlPolicies.OrderBy(k => k.Key) select new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", polc.Key.ToString("dd'/'MM'/'yyyy")), new XAttribute("ApplicableAmount", polc.Value), new XAttribute("NoShowPolicy", polc.Key==CheckInDate?"1":"0")));
                return cxlplcy;
            }
            catch (Exception ex)
            {
                DateTime Cxldate;
                Cxldate = DateTime.Now.Date;
                if (Cxldate.AddDays(-1) < lastCxldate)
                {
                    lastCxldate = Cxldate.AddDays(-1);
                }
                cxlPolicies.Add(lastCxldate, 0);
                cxlPolicies.AddCxlPolicy(Cxldate, TotalPrice);
                XElement cxlplcy = new XElement("CancellationPolicies", from polc in cxlPolicies.OrderBy(k => k.Key) select new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", polc.Key.ToString("dd'/'MM'/'yyyy")), new XAttribute("ApplicableAmount", polc.Value), new XAttribute("NoShowPolicy", "0")));
                return cxlplcy;
            }
        }
        #endregion
        #endregion
        #region PreBooking
        public XElement PreBooking(XElement preBookReq, string xmlout)
        {
            //if (xmlout == "true")
            //{
            //    dmc = "HA";
            //}
            //else
            //{
            //    dmc = "EBookingCenter";
            //}
            dmc = xmlout;
            XElement preBookReqest = preBookReq.Descendants("HotelPreBookingRequest").FirstOrDefault();
            XElement PreBookResponse = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XElement("Authentication", new XElement("AgentID", preBookReq.Descendants("AgentID").FirstOrDefault().Value), new XElement("UserName", preBookReq.Descendants("UserName").FirstOrDefault().Value),
                                       new XElement("Password", preBookReq.Descendants("Password").FirstOrDefault().Value), new XElement("ServiceType", preBookReq.Descendants("ServiceType").FirstOrDefault().Value),
                                       new XElement("ServiceVersion", preBookReq.Descendants("ServiceVersion").FirstOrDefault().Value))));

            try
            {
                srvRequest = new EBookingRequest();
                XElement groupDetails = new XElement("Rooms");
                List<XElement> roomlst = new List<XElement>();
                string termsCondition = string.Empty;
                int totalrooms = preBookReq.Descendants("Room").Count();
                int nights = (int)(preBookReq.Descendants("ToDate").FirstOrDefault().Value.ConvertToDate() - preBookReq.Descendants("FromDate").FirstOrDefault().Value.ConvertToDate()).TotalDays;
                decimal TotalRateOld = preBookReq.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value.ModifyToDecimal();
                string ResultCode = preBookReq.Descendants("Room").FirstOrDefault().Attribute("MealPlanID").Value;
                string PackageCode=preBookReq.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                string soapResult = string.Empty;
                XElement PreBookRequest = new XElement(soapenv + "Envelope",
                  new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                  new XAttribute(XNamespace.Xmlns + "ns", "http://tbs.dcsplus.net/ws/1.0/"),
                  new XElement(soapenv + "Header",
                  new XElement(ns + "AuthHeader", new XElement(ns + "ResellerCode", SellerId), new XElement(ns + "Username", UserName), new XElement(ns + "Password", Password), new XElement(ns + "ApplicationId"))),
                  new XElement(soapenv + "Body",
                     new XElement(ns + "HotelGetOfferDetailsRQ",
                     new XElement(ns + "ResultCode", ResultCode),
                     new XElement(ns + "PackageCode", PackageCode), 
                     new XElement(ns+"PackageRooms",
                     from room in preBookReq.Descendants("Room") select new XElement(ns + "PackageRoom", new XAttribute("PackageRoomCode", room.Attribute("OccupancyID").Value), new XAttribute("RoomCode", room.Attribute("ID").Value)))
                    )));
                LogModel logmodel = new LogModel()
                {
                    TrackNo = preBookReq.Descendants("TransID").FirstOrDefault().Value,
                    CustomerID = preBookReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong(),
                    SuplId = SupplierId,
                    LogType = "PreBook",
                    LogTypeID = 4
                };
                XDocument respPreBook = srvRequest.ServerRequest(PreBookRequest.ToString(), Url, logmodel);
                int grpIndex = 1;
                decimal RoomGroupRate = respPreBook.Descendants(ns+"Package").FirstOrDefault().Descendants(ns+"Price").FirstOrDefault().Attribute("Amount").Value.ModifyToDecimal();
                string currency = respPreBook.Descendants(ns+"Package").FirstOrDefault().Descendants(ns+"Price").FirstOrDefault().Attribute("Currency").Value;
                XElement RoomType = new XElement("RoomTypes", new XAttribute("Index", grpIndex), new XAttribute("TotalRate", RoomGroupRate));
                List<XElement> packagelist = respPreBook.Descendants(ns + "Package").FirstOrDefault().Descendants(ns + "PackageRoom").ToList();
                int roomSeq = 1;
                foreach (var roompax in preBookReq.Descendants("RoomPax"))
                {
                    var packageroom = packagelist.Find(x => x.Descendants(ns + "Occupancy").FirstOrDefault().Attribute("Adults").Value == roompax.Descendants("Adult").FirstOrDefault().Value && x.Descendants(ns + "Occupancy").FirstOrDefault().Attribute("Children").Value == roompax.Descendants("Child").FirstOrDefault().Value);
                    int pkIndx = packagelist.FindIndex(x => x.Descendants(ns + "Occupancy").FirstOrDefault().Attribute("Adults").Value == roompax.Descendants("Adult").FirstOrDefault().Value && x.Descendants(ns + "Occupancy").FirstOrDefault().Attribute("Children").Value == roompax.Descendants("Child").FirstOrDefault().Value);
                    string packageRoomCode = packageroom.Attribute("PackageRoomCode").Value;
                    XElement roomref = packageroom.Descendants(ns + "RoomRef").Where(x => x.Attribute("Selected").Value == "true").FirstOrDefault();
                    XElement roomInfo = respPreBook.Descendants(ns + "Room").Where(x => x.Attribute("RoomCode").Value == roomref.Attribute("RoomCode").Value).FirstOrDefault();
                    decimal totalRoomRate = roomInfo.Descendants(ns + "Price").Count() > 0 ? roomInfo.Descendants(ns + "Price").FirstOrDefault().Attribute("Amount").Value.ModifyToDecimal() : RoomGroupRate / totalrooms;
                    string[] others = roomInfo.Descendants(ns + "Info").Count() > 0 ? roomInfo.Descendants(ns + "Info").FirstOrDefault().Value.Split("Promotion:") : null;
                    string Promotion = others != null ? others.Length == 2 ? others[1] : string.Empty : string.Empty;
                    // string otherInfo = roomInfo.Descendants(ns+"Info").FirstOrDefault().Value.Split("Promotion:")[0];

                    XElement roomDtl = new XElement("Room", new XAttribute("ID", roomref.Attribute("RoomCode").Value), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", roomSeq), new XAttribute("SessionID", PackageCode), new XAttribute("RoomType", roomInfo.Element(ns + "Name").Value), new XAttribute("OccupancyID", packageRoomCode),
                                       new XAttribute("OccupancyName", ""), new XAttribute("MealPlanID", ""), new XAttribute("MealPlanName", roomInfo.Element(ns + "Board") != null ? roomInfo.Element(ns + "Board").Value : "Room Only"), new XAttribute("MealPlanCode", ""), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", totalRoomRate / nights),
                                       new XAttribute("TotalRoomRate", totalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", true),
                                       new XElement("RequestID", ResultCode), new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions", Promotion)),
                                       new XElement("CancellationPolicy"), new XElement("Amenities", new XElement("Amenity")),
                                       new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"),
                                       new XElement(respPreBook.Descendants(ns + "PriceBreakdown").FirstOrDefault().Descendants(ns + "Room").Count() > 0 ? getPreBookPriceBreakup(respPreBook.Descendants(ns + "PriceBreakdown").FirstOrDefault().Descendants(ns + "Room").Where(x => x.Attribute("RoomCode").Value == roomref.Attribute("RoomCode").Value).FirstOrDefault().Descendants(ns + "DayPrice").ToList()) : getPriceBreakup(nights, totalRoomRate)),
                                       new XElement("AdultNum", packageroom.Descendants(ns + "Occupancy").FirstOrDefault().Attribute("Adults").Value),
                                       new XElement("ChildNum", packageroom.Descendants(ns + "Occupancy").FirstOrDefault().Attribute("Children").Value));
                    RoomType.Add(roomDtl);
                    roomSeq++;
                    packagelist.RemoveAt(pkIndx);

                }
                groupDetails.Add(RoomType);
                if (respPreBook.Descendants(ns+"Remark").Count() > 0)
                {
                    foreach (var rem in respPreBook.Descendants(ns+"Remark").ToList())
                    {
                        if (string.IsNullOrEmpty(termsCondition))
                        {
                            termsCondition = rem.Value;
                        }
                        else
                        {
                            termsCondition += "<br/>" + rem.Value;
                        }
                    }
                }



                groupDetails.Descendants("Room").Last().AddAfterSelf(GetCxlPolicy(respPreBook.Descendants(ns + "Policy").ToList(), RoomGroupRate, preBookReq.Descendants("FromDate").FirstOrDefault().Value.ConvertToDate()));
                XElement hoteldata = new XElement("Hotels", new XElement("Hotel", new XElement("HotelID", preBookReq.Descendants("HotelID").FirstOrDefault().Value),
                                            new XElement("HotelName", preBookReq.Descendants("HotelName").FirstOrDefault().Value), new XElement("Status", true),
                                            new XElement("TermCondition", termsCondition), new XElement("HotelImgSmall"), new XElement("HotelImgLarge"),
                                            new XElement("MapLink"), new XElement("DMC", dmc), new XElement("Currency", currency),
                                            new XElement("Offers"), groupDetails));
                if (TotalRateOld == RoomGroupRate)
                {
                    PreBookResponse.Add(new XElement(soapenv + "Body", preBookReqest, new XElement("HotelPreBookingResponse", new XElement("NewPrice", ""), hoteldata)));
                }
                else
                {
                    PreBookResponse.Add(new XElement(soapenv + "Body", preBookReqest, new XElement("HotelPreBookingResponse", new XElement("ErrorTxt", "Amount has been changed"), new XElement("NewPrice", RoomGroupRate), hoteldata)));
                }
                return PreBookResponse;

            }
            catch(Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBooking";
                ex1.PageName = "EBookingCenter";
                ex1.CustomerID = preBookReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = preBookReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                PreBookResponse.Add(new XElement(soapenv + "Body", preBookReqest, new XElement("HotelPreBookingResponse", new XElement("ErrorTxt", "Room is not available"))));
                return PreBookResponse;
            }
               
            
           
        }
        
        #region Pre Book Price Breakup
        private XElement getPreBookPriceBreakup(List<XElement> NightRateList)
        {
            XElement pricebrk = new XElement("PriceBreakups");
            int nyt = 1;
            foreach (var nightdetail in NightRateList)
            {
                pricebrk.Add(new XElement("Price", new XAttribute("Night", nyt), new XAttribute("PriceValue", nightdetail.Attribute("Amount").Value)));
                nyt++;
            }

            return pricebrk;
        }
        #endregion
        #endregion
     

        #region Hotel Booking
        public XElement HotelBooking(XElement BookingReq)
        {
            XElement BookReq = BookingReq.Descendants("HotelBookingRequest").FirstOrDefault();
            XElement HotelBookingRes = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XElement("Authentication", new XElement("AgentID", BookingReq.Descendants("AgentID").FirstOrDefault().Value), new XElement("UserName", BookingReq.Descendants("UserName").FirstOrDefault().Value), new XElement("Password", BookingReq.Descendants("Password").FirstOrDefault().Value),
                                       new XElement("ServiceType", BookingReq.Descendants("ServiceType").FirstOrDefault().Value), new XElement("ServiceVersion", BookingReq.Descendants("ServiceVersion").FirstOrDefault().Value))));
            try
            {
                srvRequest = new EBookingRequest();
                string soapResult = string.Empty;
                List<string> titleList = new List<string>() { "Mr", "Mrs", "Miss" };
                List<XElement> roomlst = new List<XElement>();
                string termsCondition = string.Empty;
                int nights = (int)(BookingReq.Descendants("ToDate").FirstOrDefault().Value.ConvertToDate() - BookingReq.Descendants("FromDate").FirstOrDefault().Value.ConvertToDate()).TotalDays;
                string ResultCode = BookingReq.Descendants("Room").FirstOrDefault().Descendants("RequestID").FirstOrDefault().Value;
                string PackageCode = BookingReq.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                roomlst=BookingReq.Descendants("Room").ToList();
                int paxid=1;
               foreach(var room in roomlst)
               {
                   foreach(var paxinfo in roomlst.Descendants("PaxInfo").ToList())
                   {         
                       paxinfo.Add(new XElement("PaxRef", paxid));
                       paxid++;
                   }
               }
               XElement rooms = new XElement("Rooms", roomlst);
               XElement BookRequest = new XElement(soapenv + "Envelope",
                                       new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XAttribute(XNamespace.Xmlns + "ns", "http://tbs.dcsplus.net/ws/1.0/"),
                                       new XElement(soapenv + "Header",
                                       new XElement(ns + "AuthHeader", new XElement(ns + "ResellerCode", SellerId), new XElement(ns + "Username", UserName), new XElement(ns + "Password", Password), new XElement(ns + "ApplicationId"))),
                                       new XElement(soapenv + "Body",
                                       new XElement(ns + "HotelMakeReservationRQ",
                                       new XElement(ns + "ExternalRef", BookingReq.Descendants("TransactionID").FirstOrDefault().Value),
                                       new XElement(ns + "Passengers",
                                       from pax in rooms.Descendants("PaxInfo")
                                       select new XElement(ns + "Passenger", new XAttribute("PaxRef", pax.Descendants("PaxRef").FirstOrDefault().Value), new XAttribute("Type", pax.Descendants("GuestType").FirstOrDefault().Value == "Adult" ? "adult" : (pax.Descendants("Age").FirstOrDefault().Value.ModifyToInt() > 1) ? "child" : "infant"), 
                                           new XAttribute("Lead",pax.Descendants("PaxRef").FirstOrDefault().Value=="1"? pax.Descendants("IsLead").FirstOrDefault().Value:"false"),
                                       new XElement(ns + "Title", pax.Descendants("GuestType").FirstOrDefault().Value == "Adult" ? (titleList.Contains(pax.Descendants("Title").FirstOrDefault().Value) ? pax.Descendants("Title").FirstOrDefault().Value : "Mr") : "Master"),
                                       new XElement(ns+"FirstName", pax.Descendants("FirstName").FirstOrDefault().Value),
                                       new XElement(ns+"LastName", pax.Descendants("LastName").FirstOrDefault().Value),
                                       new XElement(ns + "BirthDate", pax.Descendants("GuestType").FirstOrDefault().Value == "Adult" ? string.Empty : pax.Descendants("Age").FirstOrDefault().Value.DOB(BookingReq.Descendants("FromDate").FirstOrDefault().Value))
                                       )),
                                       new XElement(ns + "ServiceConfig", new XElement(ns + "HotelService", new XAttribute("ResultCode", ResultCode),
                                        new XElement(ns + "PackageCode", PackageCode),
                                            new XElement(ns + "PackageRooms",
                                            from room in rooms.Descendants("Room")
                                            select new XElement(ns + "PackageRoom", new XAttribute("PackageRoomCode", room.Attribute("OccupancyID").Value), new XAttribute("RoomCode", room.Attribute("RoomTypeID").Value),
                                                new XElement(ns + "Passengers",
                                                from pax in room.Descendants("PaxInfo")
                                                select new XElement(ns + "PaxRef", pax.Descendants("PaxRef").FirstOrDefault().Value)
                                         ))),
                                         new XElement(ns + "PaymentMethod", "credit"),
                                         new XElement(ns + "BookOptions"),
                                         new XElement(ns + "Comments", BookingReq.Descendants("SpecialRemarks").FirstOrDefault().Value)
                             )))));
                LogModel logmodel = new LogModel()
                {
                    TrackNo = BookingReq.Descendants("TransactionID").FirstOrDefault().Value,
                    CustomerID = BookingReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong(),
                    SuplId = SupplierId,
                    LogType = "Book",
                    LogTypeID = 5
                };
                XDocument respBook = srvRequest.ServerRequest(BookRequest.ToString(), Url, logmodel);

                int errCnt = respBook.Descendants(ns+"Error").Count();
                int warCnt = respBook.Descendants(ns + "Warning").Count();
                if (errCnt > 0 || warCnt>0)
                {
                    HotelBookingRes.Add(new XElement(soapenv + "Body", BookReq, new XElement("HotelBookingResponse", new XElement("ErrorTxt", respBook.Descendants(ns+"Message").FirstOrDefault().Value))));
                    return HotelBookingRes;
                }
                else
                {
                    string bookStatus = respBook.Descendants(ns + "HotelService").FirstOrDefault().Attribute("Status").Value;
                    if (bookStatus == "OK")
                    {
                        decimal amount = 0.0m;
                        try
                        {
                            amount = respBook.Descendants(ns + "Price").FirstOrDefault().Attribute("Amount").Value.ModifyToDecimal();
                        }
                        catch
                        {
                            amount = BookingReq.Descendants("TotalAmount").FirstOrDefault().Value.ModifyToDecimal();
                        }
                        XElement BookingRes = new XElement("HotelBookingResponse",
                                               new XElement("Hotels", new XElement("HotelID", BookingReq.Descendants("HotelID").FirstOrDefault().Value),
                                               new XElement("HotelName", BookingReq.Descendants("HotelName").FirstOrDefault().Value),
                                               new XElement("FromDate", BookingReq.Descendants("FromDate").FirstOrDefault().Value),
                                               new XElement("ToDate", BookingReq.Descendants("ToDate").FirstOrDefault().Value),
                                               new XElement("AdultPax", BookingReq.Descendants("Rooms").Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value),
                                               new XElement("ChildPax", BookingReq.Descendants("Rooms").Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value),
                                               new XElement("TotalPrice", amount), new XElement("CurrencyID"),
                                               new XElement("CurrencyCode", ""),
                                               new XElement("MarketID"), new XElement("MarketName"), new XElement("HotelImgSmall"), new XElement("HotelImgLarge"), new XElement("MapLink"), new XElement("VoucherRemark"),
                                               new XElement("TransID", BookingReq.Descendants("TransID").FirstOrDefault().Value),
                                               new XElement("ConfirmationNumber", respBook.Descendants(ns + "Reservation").FirstOrDefault().Attribute("ReservationID").Value),
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
                    else if (bookStatus == "RQ")
                    {
                        HotelBookingRes.Add(new XElement(soapenv + "Body", BookReq, new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Booking is on request!"))));
                    }
                    else
                    {
                        HotelBookingRes.Add(new XElement(soapenv + "Body", BookReq, new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Booking can not be generated!"))));
                    
                    }

                }
 
                return HotelBookingRes;

            }
            catch (Exception ex)
            {
               
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelBooking";
                ex1.PageName = "EBookingCenter";
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
            XElement CxlReq = cancelReq.Descendants("HotelCancellationRequest").FirstOrDefault();
            XElement BookCXlRes = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                  new XElement("Authentication", new XElement("AgentID", cancelReq.Descendants("AgentID").FirstOrDefault().Value), new XElement("UserName", cancelReq.Descendants("UserName").FirstOrDefault().Value),
                                  new XElement("Password", cancelReq.Descendants("Password").FirstOrDefault().Value), new XElement("ServiceType", cancelReq.Descendants("ServiceType").FirstOrDefault().Value),
                                  new XElement("ServiceVersion", cancelReq.Descendants("ServiceVersion").FirstOrDefault().Value))));

            try
            {
                srvRequest = new EBookingRequest();
                XElement cancelRequest = new XElement(soapenv + "Envelope",
                                       new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XAttribute(XNamespace.Xmlns + "ns", "http://tbs.dcsplus.net/ws/1.0/"),
                                       new XElement(soapenv + "Header",
                                       new XElement(ns + "AuthHeader", new XElement(ns + "ResellerCode", SellerId), new XElement(ns + "Username", UserName), new XElement(ns + "Password", Password), new XElement(ns + "ApplicationId"))),
                                       new XElement(soapenv + "Body",
                                       new XElement(ns + "ReservationCancelRQ",
                                           new XElement(ns + "ReservationID", cancelReq.Descendants("ConfirmationNumber").FirstOrDefault().Value)
                                     )));
                LogModel logmodel = new LogModel()
                {
                    TrackNo = cancelReq.Descendants("TransID").FirstOrDefault().Value,
                    CustomerID = cancelReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong(),
                    SuplId = SupplierId,
                    LogType = "Cancel",
                    LogTypeID = 6
                };
                XDocument resBokCancel = srvRequest.ServerRequest(cancelRequest.ToString(), Url, logmodel);
                int errCnt = resBokCancel.Descendants(ns+"Error").Count();
                int warCnt = resBokCancel.Descendants(ns + "Warning").Count();
                if (errCnt > 0 || warCnt > 0)
                {
                    BookCXlRes.Add(new XElement(soapenv + "Body", CxlReq, new XElement("HotelCancellationResponse", new XElement("ErrorTxt", resBokCancel.Descendants(ns + "Message").FirstOrDefault().Value))));
                    return BookCXlRes;
                }
                else
                {
                    decimal amount = 0.0m;
                    try
                    {
                        amount = resBokCancel.Descendants(ns + "Price").FirstOrDefault().Attribute("Amount").Value.ModifyToDecimal();
                    }
                    catch
                    {
                        amount = 0.0m;
                    }
                    BookCXlRes.Add(new XElement(soapenv + "Body", CxlReq, new XElement("HotelCancellationResponse", new XElement("Rooms", new XElement("Room", new XElement("Cancellation", new XElement("Amount", amount), new XElement("Status", resBokCancel.Descendants(ns + "HotelService").FirstOrDefault().Attribute("Status").Value == "XX" ? "Success" : "Fail")))))));
                }

                return BookCXlRes;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "BookingCancellation";
                ex1.PageName = "EBookingCenter";
                ex1.CustomerID = cancelReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = cancelReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                BookCXlRes.Add(new XElement(soapenv + "Body", CxlReq, new XElement("HotelCancellationResponse", new XElement("ErrorTxt", "There is some technical error"))));
                return BookCXlRes;
            }
        }
        #endregion
    }
}