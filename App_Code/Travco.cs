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
using TravillioXMLOutService.Common.Travco;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Travco;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace TravillioXMLOutService.App_Code
{
    public class Travco : IDisposable
    {
        string customerid = string.Empty;
        string dmc = string.Empty;
        #region Credentails
        string AgentCode = string.Empty;
        string AgentPassword = string.Empty;
        //string AgentCode = "257TDR";
        //const string AgentPassword = "111017XHO8";  //Live 
        //string AgentPassword = "2105172DT2";     //test
        const string SupplierId = "7";
        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
        #endregion

        public Travco(string _customerid)
        {
            XElement suppliercred = supplier_Cred.getsupplier_credentials(_customerid, "7");
            try
            {
                customerid = _customerid;
                AgentCode = suppliercred.Descendants("AgentCode").FirstOrDefault().Value;
                AgentPassword = suppliercred.Descendants("AgentPassword").FirstOrDefault().Value;
            }
            catch { }
        }
        public Travco()
        {
        }

        #region Hotel Availability
        public XElement getHotelAvailbalityByCityCode(XElement req, XElement xmlhotelDoc, XElement StarRating, XElement CityMapping)
        {
            string travcoRequest = string.Empty;
            string travcoResponse = string.Empty;
            int singleroom = 0, twinroom = 0, tripleroom = 0, quadroom = 0, childroom = 0;
            XNamespace ns3 = XNamespace.Get("http://www.travco.co.uk/trlink/xsd/starrating/response");
            XElement HotelsData = new XElement("Hotels");
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            XElement searchReq = req.Descendants("searchRequest").FirstOrDefault();
            XElement searchResdoc = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                    new XElement("Authentication", new XElement("AgentID", AgentID), new XElement("UserName", username), new XElement("Password", password),
                                    new XElement("ServiceType", ServiceType), new XElement("ServiceVersion", ServiceVersion))));

            try
            {
                HotelAvailabilityService.RequestBaseType requestBase = new HotelAvailabilityService.RequestBaseType() { AgentCode = AgentCode, AgentPassword = AgentPassword, Lang = "en-GB" };
                HotelAvailabilityService.checkAvailabilityByCity chkAvailability = new HotelAvailabilityService.checkAvailabilityByCity();
                chkAvailability.RequestBase = requestBase;
                var SuppCity = from cty in CityMapping.Descendants("d0").Where(x => x.Element("Serial").Value == req.Descendants("CityID").FirstOrDefault().Value)
                               from suppcty in cty.Descendants("Supplier").Where(y => y.Descendants("SupplierID").FirstOrDefault().Value == "7")
                               select suppcty;
                chkAvailability.CountryCode = SuppCity.Descendants("SupplierCountryID").FirstOrDefault().Value;
                chkAvailability.CityCode = SuppCity.Descendants("SupplierCityID").FirstOrDefault().Value;
                chkAvailability.CheckInDate = req.Descendants("FromDate").FirstOrDefault().Value.StringToDate();
                chkAvailability.CheckOutDate = req.Descendants("ToDate").FirstOrDefault().Value.StringToDate();
                var rooms = req.Descendants("RoomPax").ToList();
                //Add room Data
                foreach (var room in req.Descendants("RoomPax").ToList())
                {
                    Int32 adults = room.Descendants("Adult").FirstOrDefault().Value.ModifyToInt();
                    Int32 children = room.Descendants("ChildAge") != null ? room.Descendants("ChildAge").Where(x => Int32.Parse(x.Value) > 2).Count() : 0;
                    if (adults != 2)
                    {
                        int totaladults = adults + children;
                        if (totaladults == 1)
                        {
                            singleroom += 1;
                        }
                        else if (totaladults == 2)
                        {
                            twinroom += 1;
                        }
                        else if (totaladults == 3)
                        {
                            tripleroom += 1;
                        }
                        else if (totaladults == 4)
                        {
                            quadroom += 1;
                        }

                    }
                    else
                    {
                        if (children == 1)
                        {
                            twinroom += 1;
                            childroom += 1;
                        }
                        else
                        {
                            int totaladults = adults + children;
                            if (totaladults == 2)
                            {
                                twinroom += 1;
                            }
                            else if (totaladults == 3)
                            {
                                tripleroom += 1;
                            }
                            else if (totaladults == 4)
                            {
                                quadroom += 1;
                            }
                        }

                    }

                }
                HotelAvailabilityService.RoomData roomData = new HotelAvailabilityService.RoomData();
                if (singleroom > 0)
                {
                    roomData.SingleRoom = singleroom.ToString();
                }
                if (twinroom > 0)
                {
                    roomData.DoubleRoom = twinroom.ToString();
                }
                if (tripleroom > 0)
                {
                    roomData.TripleRoom = tripleroom.ToString();
                }
                if (quadroom > 0)
                {
                    roomData.QuadRoom = quadroom.ToString();
                }
                if (childroom > 0)
                {
                    roomData.ChildRoom = childroom.ToString();
                }
                chkAvailability.RoomData = roomData;
                //Additional Data
                HotelAvailabilityService.AdditionalData addData = new HotelAvailabilityService.AdditionalData();
                addData.NeedTotalNoOfHotels = HotelAvailabilityService.YesNoType.yes;
                addData.NeedAvailableHotelsOnly = HotelAvailabilityService.YesNoType.yes;
                addData.NeedReductionAmount = HotelAvailabilityService.YesNoType.no;
                addData.NeedHotelMessages = HotelAvailabilityService.YesNoType.no;
                addData.NeedFreeNightDetail = HotelAvailabilityService.YesNoType.no;
                addData.NeedHotelAddress = HotelAvailabilityService.YesNoType.no;
                addData.NeedTelephoneNo = HotelAvailabilityService.YesNoType.no;
                addData.NeedFaxNo = HotelAvailabilityService.YesNoType.no;
                addData.NeedBedPicture = HotelAvailabilityService.YesNoType.no;
                addData.NeedMapPicture = HotelAvailabilityService.YesNoType.no;
                addData.NeedEmail = HotelAvailabilityService.YesNoType.no;
                addData.NeedPicture = HotelAvailabilityService.YesNoType.no;
                addData.NeedAmenity = HotelAvailabilityService.YesNoType.no;
                addData.NeedHotelDescription = HotelAvailabilityService.YesNoType.no;
                addData.NeedHotelCity = HotelAvailabilityService.YesNoType.no;
                addData.NeedArrivalPointMain = HotelAvailabilityService.YesNoType.no;
                addData.NeedArrivalPointOther = HotelAvailabilityService.YesNoType.no;
                addData.NeedGeoCodes = HotelAvailabilityService.YesNoType.no;
                addData.NeedHotelProperties = HotelAvailabilityService.YesNoType.no;
                addData.NeedLocation = HotelAvailabilityService.YesNoType.no;
                addData.NeedCityArea = HotelAvailabilityService.YesNoType.no;
                addData.NeedEnglishText = HotelAvailabilityService.YesNoType.no;
                chkAvailability.AdditionalData = addData;
                HotelAvailabilityService.RequestCriteria reqCriteria = new HotelAvailabilityService.RequestCriteria();
                reqCriteria.ReturnRequestedAllRoomTypes = HotelAvailabilityService.YesNoType.yes;
                reqCriteria.SortingOrder = HotelAvailabilityService.RequestCriteriaSortingOrder.low;

                // chkAvailability.RequestCriteria = reqCriteria;
                List<string> starrating = new List<string>();
                int minrating = req.Descendants("MinStarRating").FirstOrDefault().Value.ModifyToInt();
                int maxrating = req.Descendants("MaxStarRating").FirstOrDefault().Value.ModifyToInt();
                var strRating = StarRating.Descendants(ns3 + "StarRating").Where(x => Int32.Parse(x.Descendants("Description").FirstOrDefault().Attribute("Rating").Value) >= minrating && Int32.Parse(x.Descendants("Description").FirstOrDefault().Attribute("Rating").Value) <= maxrating);
                foreach (var rating in strRating)
                {
                    starrating.Add(rating.Attribute("StarCode").Value);
                }
                chkAvailability.MultiStarsRequest = starrating.ToArray();
                chkAvailability.MultiStarsRequest = starrating.ToArray();
                HotelAvailabilityService.HotelAvailabilityV7ServicePortTypeClient hotelAvailabilityService = new HotelAvailabilityService.HotelAvailabilityV7ServicePortTypeClient();
                HotelAvailabilityService.HotelAvailabilityV7Response hotelAvailabilityResponse = hotelAvailabilityService.checkAvailabilityByCity(chkAvailability);
                HotelAvailabilityService.Response rtnResponse = hotelAvailabilityResponse.Response;
                #region Supplier Log
                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(chkAvailability.GetType());
                    serializer.Serialize(stringwriter, chkAvailability);
                    travcoRequest = stringwriter.ToString();
                }
                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(rtnResponse.GetType());
                    serializer.Serialize(stringwriter, rtnResponse);
                    travcoResponse = stringwriter.ToString();
                }

                APILogDetail log = new APILogDetail();
                log.customerID = req.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 1;
                log.LogType = "Search";
                log.SupplierID = 7;
                log.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = travcoRequest.ToString();
                log.logresponseXML = travcoResponse.ToString();
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion
                // var hotellist = from hotels in xmlhotelDoc.Descendants("Hotel") where hotels.Attribute("CityCode").Value == "AJM" select hotels;
                foreach (var hotel in rtnResponse.HotelDatas.FirstOrDefault().Hotels)
                {
                    var hotelstatic = (from hotels in xmlhotelDoc.Descendants("Hotel") where hotels.Attribute("HotelCode").Value == hotel.HotelCode select hotels).FirstOrDefault();
                    decimal minrate = 0.0m;
                    decimal grouprate = 0.0m;
                    foreach (var subhoteldata in hotel.SubHotelData.ToList())
                    {
                        var roomlist = from roomdt in subhoteldata.RoomDatas where (!roomdt.RoomCode.StartsWith("TW") && !roomdt.RoomCode.StartsWith("DS")) select roomdt;
                        foreach (var roomdata in roomlist)
                        {
                            grouprate += ((decimal)roomdata.TotalAdultPrice + (decimal)roomdata.TotalChildPrice);
                        }
                        if (minrate == 0)
                        {
                            minrate = grouprate;
                        }
                        else
                        {
                            if (grouprate < minrate)
                            {
                                minrate = grouprate;
                            }
                        }
                    }


                    XElement hoteldata = new XElement("Hotel", new XElement("HotelID", hotel.HotelCode),
                        new XElement("HotelName", hotelstatic.Descendants("HotelName").FirstOrDefault().Value),
                        new XElement("PropertyTypeName"), new XElement("CountryID"),
                        new XElement("CountryName", hotelstatic.Descendants("CountryName").FirstOrDefault().Value),
                        new XElement("CountryCode", hotelstatic.Attributes("CountryCode").FirstOrDefault().Value),
                        new XElement("CityId"),
                        new XElement("CityCode", hotelstatic.Attributes("CityCode").FirstOrDefault().Value),
                        new XElement("CityName", hotelstatic.Descendants("CityName").FirstOrDefault().Value),
                        new XElement("AreaId"),
                        new XElement("AreaName", hotelstatic.Descendants("CityAreaName").FirstOrDefault().Value),
                        new XElement("RequestID"),
                        new XElement("Address", hotelstatic.Descendants("LocationName").FirstOrDefault().Value),
                        new XElement("Location", hotelstatic.Descendants("LocationName").FirstOrDefault().Value),
                        new XElement("Description"),
                        new XElement("StarRating", getStarRating(StarRating, hotelstatic.Descendants("StarRate").FirstOrDefault().Value)),
                        new XElement("MinRate", minrate),
                        new XElement("HotelImgSmall", hotelstatic.Descendants("FrontImagePath").FirstOrDefault().Value),
                        new XElement("HotelImgLarge", hotelstatic.Descendants("FrontImagePath").FirstOrDefault().Value),
                        new XElement("MapLink"),
                        new XElement("Longitude", hotelstatic.Descendants("Longitude").FirstOrDefault().Value),
                        new XElement("Latitude", hotelstatic.Descendants("Latitude").FirstOrDefault().Value),
                        new XElement("DMC", "Travco"), new XElement("SupplierID", SupplierId),
                        new XElement("Currency", hotel.SubHotelData.FirstOrDefault().CurrencyCode != "PDS" ? hotel.SubHotelData.FirstOrDefault().CurrencyCode : "GBP"), new XElement("Offers"),
                        new XElement("Facilities", from facility in hotelstatic.Descendants("HotelAmenityName").ToList() select new XElement("Facility", facility.Value)),
                        new XElement("Rooms")
                        );
                    HotelsData.Add(hoteldata);
                }
                searchResdoc.Add(new XElement(soapenv + "Body", searchReq, new XElement("searchResponse", HotelsData)));
                return searchResdoc;

            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "getHotelAvailbalityByCityCode";
                ex1.PageName = "Travco";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                searchResdoc.Add(new XElement(soapenv + "Body", searchReq, new XElement("searchResponse", HotelsData)));

                return searchResdoc;
            }
        }

        #endregion

        #region Merge Hotel Availability
        public List<XElement> getHotelAvailbalityMerge(XElement req, XElement xmlhotelDoc, XElement StarRating, XElement CityMapping, string xtype)
        {
            try
            {



                #region chk and call id method

                string HtId = null;
                string HtName = null;
                if (req.Descendants("HotelID").FirstOrDefault() != null)
                    HtId = req.Descendants("HotelID").FirstOrDefault().Value;
                if (req.Descendants("HotelName").FirstOrDefault() != null)
                    HtName = req.Descendants("HotelName").FirstOrDefault().Value;
                if (HtId != null && HtId != string.Empty)
                {
                    try
                    {
                        var list = this.getHotelAvailbalityMerge_HtId(req, xmlhotelDoc, StarRating, CityMapping, xtype);

                        return list;
                    }
                    catch { return null; }
                }
                #endregion

                dmc = xtype;
                string travcoRequest = string.Empty;
                string travcoResponse = string.Empty;
                List<string> reqpaxlist = new List<string>();
                int singleroom = 0, twinroom = 0, tripleroom = 0, quadroom = 0, childroom = 0;
                XNamespace ns3 = XNamespace.Get("http://www.travco.co.uk/trlink/xsd/starrating/response");
                List<XElement> HotelsData = new List<XElement>();
                HotelAvailabilityService.RequestBaseType requestBase = new HotelAvailabilityService.RequestBaseType() { AgentCode = AgentCode, AgentPassword = AgentPassword, Lang = "en-GB" };
                HotelAvailabilityService.checkAvailabilityByCity chkAvailability = new HotelAvailabilityService.checkAvailabilityByCity();
                chkAvailability.RequestBase = requestBase;
                TravcoHotelStatic TravcoStatic = new TravcoHotelStatic();
                DataTable trvCity = TravcoStatic.GetCityCode(req.Descendants("CityID").FirstOrDefault().Value);
                chkAvailability.CountryCode = trvCity.Rows[0]["CountryCode"].ToString();
                chkAvailability.CityCode = trvCity.Rows[0]["CityCode"].ToString();

                chkAvailability.CheckInDate = req.Descendants("FromDate").FirstOrDefault().Value.StringToDate();
                chkAvailability.CheckOutDate = req.Descendants("ToDate").FirstOrDefault().Value.StringToDate();
                var rooms = req.Descendants("RoomPax").ToList();
                bool validateguest = false;
                //Add room Data
                foreach (var room in req.Descendants("RoomPax").ToList())
                {
                    Int32 adults = room.Descendants("Adult").FirstOrDefault().Value.ModifyToInt() + (room.Descendants("ChildAge") != null ? room.Descendants("ChildAge").Where(x => Int32.Parse(x.Value) > 11).Count() : 0);
                    Int32 children = room.Descendants("ChildAge") != null ? room.Descendants("ChildAge").Where(x => Int32.Parse(x.Value) >= 2 && Int32.Parse(x.Value) <= 11).Count() : 0;
                    if ((adults + children) <= 4)
                    {
                        validateguest = true;
                        if (adults != 2)
                        {
                            int totaladults = adults + children;
                            if (totaladults == 1)
                            {
                                singleroom += 1;
                            }
                            else if (totaladults == 2)
                            {
                                twinroom += 1;
                            }
                            else if (totaladults == 3)
                            {
                                tripleroom += 1;
                            }
                            else if (totaladults == 4)
                            {
                                quadroom += 1;
                            }

                        }
                        else
                        {
                            if (children == 1)
                            {
                                twinroom += 1;
                                childroom += 1;
                            }
                            else
                            {
                                int totaladults = adults + children;
                                if (totaladults == 2)
                                {
                                    twinroom += 1;
                                }
                                else if (totaladults == 3)
                                {
                                    tripleroom += 1;
                                }
                                else if (totaladults == 4)
                                {
                                    quadroom += 1;
                                }
                            }

                        }
                    }
                    else
                    {
                        validateguest = false;
                        break;

                    }

                }
                if (validateguest)
                {
                    HotelAvailabilityService.RoomData roomData = new HotelAvailabilityService.RoomData();
                    if (singleroom > 0)
                    {
                        roomData.SingleRoom = singleroom.ToString();
                        reqpaxlist.Add("1");
                    }
                    if (twinroom > 0)
                    {
                        roomData.DoubleRoom = twinroom.ToString();
                        reqpaxlist.Add("2");
                    }
                    if (tripleroom > 0)
                    {
                        roomData.TripleRoom = tripleroom.ToString();
                        reqpaxlist.Add("3");
                    }
                    if (quadroom > 0)
                    {
                        roomData.QuadRoom = quadroom.ToString();
                        reqpaxlist.Add("4");
                    }
                    if (childroom > 0)
                    {
                        roomData.ChildRoom = childroom.ToString();
                    }
                    chkAvailability.RoomData = roomData;
                    //Additional Data
                    HotelAvailabilityService.AdditionalData addData = new HotelAvailabilityService.AdditionalData();
                    addData.NeedTotalNoOfHotels = HotelAvailabilityService.YesNoType.yes;
                    addData.NeedAvailableHotelsOnly = HotelAvailabilityService.YesNoType.yes;
                    addData.NeedReductionAmount = HotelAvailabilityService.YesNoType.no;
                    addData.NeedHotelMessages = HotelAvailabilityService.YesNoType.no;
                    addData.NeedFreeNightDetail = HotelAvailabilityService.YesNoType.no;
                    addData.NeedHotelAddress = HotelAvailabilityService.YesNoType.no;
                    addData.NeedTelephoneNo = HotelAvailabilityService.YesNoType.no;
                    addData.NeedFaxNo = HotelAvailabilityService.YesNoType.no;
                    addData.NeedBedPicture = HotelAvailabilityService.YesNoType.no;
                    addData.NeedMapPicture = HotelAvailabilityService.YesNoType.no;
                    addData.NeedEmail = HotelAvailabilityService.YesNoType.no;
                    addData.NeedPicture = HotelAvailabilityService.YesNoType.no;
                    addData.NeedAmenity = HotelAvailabilityService.YesNoType.no;
                    addData.NeedHotelDescription = HotelAvailabilityService.YesNoType.no;
                    addData.NeedHotelCity = HotelAvailabilityService.YesNoType.no;
                    addData.NeedArrivalPointMain = HotelAvailabilityService.YesNoType.no;
                    addData.NeedArrivalPointOther = HotelAvailabilityService.YesNoType.no;
                    addData.NeedGeoCodes = HotelAvailabilityService.YesNoType.no;
                    addData.NeedHotelProperties = HotelAvailabilityService.YesNoType.no;
                    addData.NeedLocation = HotelAvailabilityService.YesNoType.no;
                    addData.NeedCityArea = HotelAvailabilityService.YesNoType.no;
                    addData.NeedEnglishText = HotelAvailabilityService.YesNoType.no;
                    chkAvailability.AdditionalData = addData;

                    HotelAvailabilityService.RequestCriteria reqCriteria = new HotelAvailabilityService.RequestCriteria();
                    reqCriteria.ReturnRequestedAllRoomTypes = HotelAvailabilityService.YesNoType.yes;
                    reqCriteria.SortingOrder = HotelAvailabilityService.RequestCriteriaSortingOrder.low;

                    // chkAvailability.RequestCriteria = reqCriteria;

                    List<string> starrating = new List<string>();
                    int minrating = req.Descendants("MinStarRating").FirstOrDefault().Value.ModifyToInt();
                    int maxrating = req.Descendants("MaxStarRating").FirstOrDefault().Value.ModifyToInt();
                    var strRating = StarRating.Descendants(ns3 + "StarRating").Where(x => Int32.Parse(x.Descendants("Description").FirstOrDefault().Attribute("Rating").Value) >= minrating && Int32.Parse(x.Descendants("Description").FirstOrDefault().Attribute("Rating").Value) <= maxrating);
                    foreach (var rating in strRating)
                    {
                        starrating.Add(rating.Attribute("StarCode").Value);
                    }
                    chkAvailability.MultiStarsRequest = starrating.ToArray();

                    HotelAvailabilityService.HotelAvailabilityV7ServicePortTypeClient hotelAvailabilityService = new HotelAvailabilityService.HotelAvailabilityV7ServicePortTypeClient();
                    HotelAvailabilityService.HotelAvailabilityV7Response hotelAvailabilityResponse = hotelAvailabilityService.checkAvailabilityByCity(chkAvailability);
                    HotelAvailabilityService.Response rtnResponse = hotelAvailabilityResponse.Response;
                    rtnResponse.CheckInDate = rtnResponse.CheckInDate.TravcoToLocalDate();
                    rtnResponse.CheckOutDate = rtnResponse.CheckOutDate.TravcoToLocalDate();
                    #region Supplier Log
                    using (StringWriter stringwriter = new System.IO.StringWriter())
                    {
                        var serializer = new XmlSerializer(chkAvailability.GetType());
                        serializer.Serialize(stringwriter, chkAvailability);
                        travcoRequest = stringwriter.ToString();
                    }
                    using (StringWriter stringwriter = new System.IO.StringWriter())
                    {
                        var serializer = new XmlSerializer(rtnResponse.GetType());
                        serializer.Serialize(stringwriter, rtnResponse);
                        travcoResponse = stringwriter.ToString();
                    }

                    APILogDetail log = new APILogDetail();
                    log.customerID = customerid.ConvertToLong();
                    log.LogTypeID = 1;
                    log.LogType = "Search";
                    log.SupplierID = 7;
                    log.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value;
                    log.logrequestXML = travcoRequest.ToString();
                    log.logresponseXML = travcoResponse.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                    #endregion
                    string xmlouttype = string.Empty;
                    try
                    {
                        if (dmc == "Travco")
                        {
                            xmlouttype = "false";
                        }
                        else
                        { xmlouttype = "true"; }
                    }
                    catch { }

                    DataTable TrvHotels = TravcoStatic.GetStaticHotels(chkAvailability.CityCode, chkAvailability.CountryCode, starrating[0], starrating[starrating.Count - 1]);
                    foreach (var hotel in rtnResponse.HotelDatas.FirstOrDefault().Hotels)
                    {
                        bool validpax = true;
                        var HotelData = TrvHotels.Select("[HotelCode] = '" + hotel.HotelCode + "'").FirstOrDefault();
                        if (HotelData != null)
                        {
                            decimal minrate = 0.0m;
                            foreach (var subhoteldata in hotel.SubHotelData.ToList())
                            {
                                decimal grouprate = 0.0m;
                                List<string> respaxlist = new List<string>();
                                //var roomlist = from roomdt in subhoteldata.RoomDatas where (!roomdt.RoomCode.StartsWith("TW") && !roomdt.RoomCode.StartsWith("DS")) select roomdt;
                                var roomlist = from roomdt in subhoteldata.RoomDatas where (!roomdt.RoomCode.StartsWith("TW")) select roomdt;
                                foreach (var roomdata in roomlist)
                                {
                                    grouprate += ((decimal)roomdata.TotalAdultPrice + (decimal)roomdata.TotalChildPrice);
                                    respaxlist.Add(roomdata.RoomPax);
                                }
                                validpax = !reqpaxlist.Except(respaxlist).Any();
                                if (validpax)
                                {
                                    if (minrate == 0)
                                    {
                                        minrate = grouprate;
                                    }
                                    else
                                    {
                                        if (grouprate < minrate)
                                        {
                                            minrate = grouprate;
                                        }
                                    }

                                }
                            }
                            if (validpax)
                            {
                                string area = string.Empty;
                                if (!string.IsNullOrEmpty(HotelData["CityAreaName"].ToString()))
                                {
                                    area = HotelData["CityAreaName"].ToString();
                                }
                                else
                                {
                                    area = HotelData["CityName"].ToString() + " - City";
                                }

                                XElement hoteldata = new XElement("Hotel", new XElement("HotelID", hotel.HotelCode),
                                    new XElement("HotelName", HotelData["HotelName"].ToString()),
                                    new XElement("PropertyTypeName"),
                                    new XElement("CountryID"),
                                    new XElement("CountryName", HotelData["CountryName"].ToString()),
                                    new XElement("CountryCode", HotelData["CountryCode"].ToString()),
                                    new XElement("CityId"),
                                    new XElement("CityCode", HotelData["CityCode"].ToString()),
                                    new XElement("CityName", HotelData["CityName"].ToString()),
                                    new XElement("AreaId"),
                                    new XElement("AreaName", area),
                                    new XElement("RequestID"),
                                    new XElement("Address", HotelData["Address"].ToString()),
                                    new XElement("Location", HotelData["LocationName"].ToString()),
                                    new XElement("Description"),
                                    new XElement("StarRating", getStarRating(StarRating, HotelData["StarRatingName"].ToString())),
                                    new XElement("MinRate", minrate),
                                    new XElement("HotelImgSmall", HotelData["ImagePath"].ToString()),
                                    new XElement("HotelImgLarge", HotelData["ImagePath"].ToString()),
                                    new XElement("MapLink"),
                                    new XElement("Longitude", HotelData["Longitude"].ToString()),
                                    new XElement("Latitude", HotelData["Latitude"].ToString()),
                                    new XElement("xmloutcustid", customerid),
                                    new XElement("xmlouttype", xmlouttype),
                                    new XElement("DMC", dmc), new XElement("SupplierID", SupplierId),
                                    new XElement("Currency", hotel.SubHotelData.FirstOrDefault().CurrencyCode != "PDS" ? hotel.SubHotelData.FirstOrDefault().CurrencyCode : "GBP"),
                                    new XElement("Offers"), 
                                    new XElement("Facilities",null),
                                    //HotelData["Facilities"].ToString().getHotelFacilities(),
                                    new XElement("Rooms")
                                    );

                                HotelsData.Add(hoteldata);
                            }
                        }
                    }

                    return HotelsData;
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "getHotelAvailbalityMerge";
                ex1.PageName = "Travco";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);

                return null;
            }

        }






        public List<XElement> getHotelAvailbalityMerge_HtId(XElement req, XElement xmlhotelDoc, XElement StarRating, XElement CityMapping, string xtype)
        {
            try
            {
                dmc = xtype;
                string travcoRequest = string.Empty;
                string travcoResponse = string.Empty;
                List<string> reqpaxlist = new List<string>();
                int singleroom = 0, twinroom = 0, tripleroom = 0, quadroom = 0, childroom = 0;
                XNamespace ns3 = XNamespace.Get("http://www.travco.co.uk/trlink/xsd/starrating/response");
                List<XElement> HotelsData = new List<XElement>();
                HotelAvailabilityService.RequestBaseType requestBase = new HotelAvailabilityService.RequestBaseType() { AgentCode = AgentCode, AgentPassword = AgentPassword, Lang = "en-GB" };
               // HotelAvailabilityService.checkAvailabilityByCity chkAvailability = new HotelAvailabilityService.checkAvailabilityByCity();
                HotelAvailabilityService.checkAvailabilityByHotelCode chkAvailability2 = new HotelAvailabilityService.checkAvailabilityByHotelCode();
                //chkAvailability.RequestBase = requestBase;
                chkAvailability2.RequestBase = requestBase;
                TravcoHotelStatic TravcoStatic = new TravcoHotelStatic();
                DataTable trvCity = TravcoStatic.GetCityCode(req.Descendants("CityID").FirstOrDefault().Value);
                //chkAvailability.CountryCode = trvCity.Rows[0]["CountryCode"].ToString();
                //chkAvailability.CityCode = trvCity.Rows[0]["CityCode"].ToString();

                string CountryCode = trvCity.Rows[0]["CountryCode"].ToString();
                string CityCode = trvCity.Rows[0]["CityCode"].ToString();


                string HtId = null;
                string HtName = null;
                if (req.Descendants("HotelID").FirstOrDefault() != null)
                    HtId = req.Descendants("HotelID").FirstOrDefault().Value;
                if (req.Descendants("HotelName").FirstOrDefault() != null)
                    HtName = req.Descendants("HotelName").FirstOrDefault().Value;
                //TravcoHotelStaticIds TravcoStaticid = new TravcoHotelStaticIds();
                //DataTable TrvHotels = TravcoStaticid.GetStaticHotels_Id(HtId, HtName, CityCode, CountryCode, null, null);
                DataTable TrvHotels = TravcoStatic.GetStaticHotels(CountryCode, CityCode, null, null, HtId, HtName);

                if (TrvHotels.Rows.Count > 0)
                { 
                    chkAvailability2.HotelCode = TrvHotels.Rows[0]["HotelCode"].ToString();
                }
                else
                {
                    //throw new Exception("There is no hotel available in database");
                    APILogDetail log = new APILogDetail();
                    log.customerID = req.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                    log.LogTypeID = 1;
                    log.LogType = "Search";
                    log.SupplierID = 7;
                    log.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value;
                    log.logrequestXML = travcoRequest.ToString();
                    log.logresponseXML = "<xml>There is no hotel available in database</xml>";
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                    return null;
                }




                //chkAvailability.CheckInDate = req.Descendants("FromDate").FirstOrDefault().Value.StringToDate();
                //chkAvailability.CheckOutDate = req.Descendants("ToDate").FirstOrDefault().Value.StringToDate();

                chkAvailability2.CheckInDate = req.Descendants("FromDate").FirstOrDefault().Value.StringToDate();
                chkAvailability2.CheckOutDate = req.Descendants("ToDate").FirstOrDefault().Value.StringToDate();


                var rooms = req.Descendants("RoomPax").ToList();
                bool validateguest = false;
                //Add room Data
                foreach (var room in req.Descendants("RoomPax").ToList())
                {
                    Int32 adults = room.Descendants("Adult").FirstOrDefault().Value.ModifyToInt() + (room.Descendants("ChildAge") != null ? room.Descendants("ChildAge").Where(x => Int32.Parse(x.Value) > 11).Count() : 0);
                    Int32 children = room.Descendants("ChildAge") != null ? room.Descendants("ChildAge").Where(x => Int32.Parse(x.Value) >= 2 && Int32.Parse(x.Value) <= 11).Count() : 0;
                    if ((adults + children) <= 4)
                    {
                        validateguest = true;
                        if (adults != 2)
                        {
                            int totaladults = adults + children;
                            if (totaladults == 1)
                            {
                                singleroom += 1;
                            }
                            else if (totaladults == 2)
                            {
                                twinroom += 1;
                            }
                            else if (totaladults == 3)
                            {
                                tripleroom += 1;
                            }
                            else if (totaladults == 4)
                            {
                                quadroom += 1;
                            }

                        }
                        else
                        {
                            if (children == 1)
                            {
                                twinroom += 1;
                                childroom += 1;
                            }
                            else
                            {
                                int totaladults = adults + children;
                                if (totaladults == 2)
                                {
                                    twinroom += 1;
                                }
                                else if (totaladults == 3)
                                {
                                    tripleroom += 1;
                                }
                                else if (totaladults == 4)
                                {
                                    quadroom += 1;
                                }
                            }

                        }
                    }
                    else
                    {
                        validateguest = false;
                        break;

                    }

                }
                if (validateguest)
                {
                    HotelAvailabilityService.RoomData roomData = new HotelAvailabilityService.RoomData();
                    if (singleroom > 0)
                    {
                        roomData.SingleRoom = singleroom.ToString();
                        reqpaxlist.Add("1");
                    }
                    if (twinroom > 0)
                    {
                        roomData.DoubleRoom = twinroom.ToString();
                        reqpaxlist.Add("2");
                    }
                    if (tripleroom > 0)
                    {
                        roomData.TripleRoom = tripleroom.ToString();
                        reqpaxlist.Add("3");
                    }
                    if (quadroom > 0)
                    {
                        roomData.QuadRoom = quadroom.ToString();
                        reqpaxlist.Add("4");
                    }
                    if (childroom > 0)
                    {
                        roomData.ChildRoom = childroom.ToString();
                    }
                    //chkAvailability.RoomData = roomData;
                    chkAvailability2.RoomData = roomData;
                    //Additional Data
                    HotelAvailabilityService.AdditionalData addData = new HotelAvailabilityService.AdditionalData();
                    addData.NeedTotalNoOfHotels = HotelAvailabilityService.YesNoType.yes;
                    addData.NeedAvailableHotelsOnly = HotelAvailabilityService.YesNoType.yes;
                    addData.NeedReductionAmount = HotelAvailabilityService.YesNoType.no;
                    addData.NeedHotelMessages = HotelAvailabilityService.YesNoType.no;
                    addData.NeedFreeNightDetail = HotelAvailabilityService.YesNoType.no;
                    addData.NeedHotelAddress = HotelAvailabilityService.YesNoType.no;
                    addData.NeedTelephoneNo = HotelAvailabilityService.YesNoType.no;
                    addData.NeedFaxNo = HotelAvailabilityService.YesNoType.no;
                    addData.NeedBedPicture = HotelAvailabilityService.YesNoType.no;
                    addData.NeedMapPicture = HotelAvailabilityService.YesNoType.no;
                    addData.NeedEmail = HotelAvailabilityService.YesNoType.no;
                    addData.NeedPicture = HotelAvailabilityService.YesNoType.no;
                    addData.NeedAmenity = HotelAvailabilityService.YesNoType.no;
                    addData.NeedHotelDescription = HotelAvailabilityService.YesNoType.no;
                    addData.NeedHotelCity = HotelAvailabilityService.YesNoType.no;
                    addData.NeedArrivalPointMain = HotelAvailabilityService.YesNoType.no;
                    addData.NeedArrivalPointOther = HotelAvailabilityService.YesNoType.no;
                    addData.NeedGeoCodes = HotelAvailabilityService.YesNoType.no;
                    addData.NeedHotelProperties = HotelAvailabilityService.YesNoType.no;
                    addData.NeedLocation = HotelAvailabilityService.YesNoType.no;
                    addData.NeedCityArea = HotelAvailabilityService.YesNoType.no;
                    addData.NeedEnglishText = HotelAvailabilityService.YesNoType.no;

                    //chkAvailability.AdditionalData = addData;
                    chkAvailability2.AdditionalData = addData;

                    HotelAvailabilityService.RequestCriteria reqCriteria = new HotelAvailabilityService.RequestCriteria();
                    reqCriteria.ReturnRequestedAllRoomTypes = HotelAvailabilityService.YesNoType.yes;
                    reqCriteria.SortingOrder = HotelAvailabilityService.RequestCriteriaSortingOrder.low;

                    // chkAvailability.RequestCriteria = reqCriteria;

                    List<string> starrating = new List<string>();
                    int minrating = req.Descendants("MinStarRating").FirstOrDefault().Value.ModifyToInt();
                    int maxrating = req.Descendants("MaxStarRating").FirstOrDefault().Value.ModifyToInt();
                    var strRating = StarRating.Descendants(ns3 + "StarRating").Where(x => Int32.Parse(x.Descendants("Description").FirstOrDefault().Attribute("Rating").Value) >= minrating && Int32.Parse(x.Descendants("Description").FirstOrDefault().Attribute("Rating").Value) <= maxrating);
                    foreach (var rating in strRating)
                    {
                        starrating.Add(rating.Attribute("StarCode").Value);
                    }

                    //chkAvailability.MultiStarsRequest = starrating.ToArray();
                    chkAvailability2.MultiStarsRequest = starrating.ToArray();


                    HotelAvailabilityService.HotelAvailabilityV7ServicePortTypeClient hotelAvailabilityService = new HotelAvailabilityService.HotelAvailabilityV7ServicePortTypeClient();

                    HotelAvailabilityService.HotelAvailabilityV7Response hotelAvailabilityResponse = null;
                    if (chkAvailability2.HotelCode != null && chkAvailability2.HotelCode != string.Empty)
                    {
                        hotelAvailabilityResponse = hotelAvailabilityService.checkAvailabilityByHotelCode(chkAvailability2);

                    }
                    
                    HotelAvailabilityService.Response rtnResponse = hotelAvailabilityResponse.Response;
                    rtnResponse.CheckInDate = rtnResponse.CheckInDate.TravcoToLocalDate();
                    rtnResponse.CheckOutDate = rtnResponse.CheckOutDate.TravcoToLocalDate();
                    #region Supplier Log
                    using (StringWriter stringwriter = new System.IO.StringWriter())
                    {
                        var serializer = new XmlSerializer(chkAvailability2.GetType());
                        serializer.Serialize(stringwriter, chkAvailability2);
                        travcoRequest = stringwriter.ToString();
                    }
                    using (StringWriter stringwriter = new System.IO.StringWriter())
                    {
                        var serializer = new XmlSerializer(rtnResponse.GetType());
                        serializer.Serialize(stringwriter, rtnResponse);
                        travcoResponse = stringwriter.ToString();
                    }

                    APILogDetail log = new APILogDetail();
                    log.customerID = req.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                    log.LogTypeID = 1;
                    log.LogType = "Search";
                    log.SupplierID = 7;
                    log.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value;
                    log.logrequestXML = travcoRequest.ToString();
                    log.logresponseXML = travcoResponse.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                    #endregion
                    string xmlouttype = string.Empty;
                    try
                    {
                        if (dmc == "Travco")
                        {
                            xmlouttype = "false";
                        }
                        else
                        { xmlouttype = "true"; }
                    }
                    catch { }
                    foreach (var hotel in rtnResponse.HotelDatas.FirstOrDefault().Hotels)
                    {
                        bool validpax = true;
                        var HotelData = TrvHotels.Select("[HotelCode] = '" + hotel.HotelCode + "'").FirstOrDefault();
                        if (HotelData != null)
                        {
                            decimal minrate = 0.0m;
                            foreach (var subhoteldata in hotel.SubHotelData.ToList())
                            {
                                decimal grouprate = 0.0m;
                                List<string> respaxlist = new List<string>();
                                //var roomlist = from roomdt in subhoteldata.RoomDatas where (!roomdt.RoomCode.StartsWith("TW") && !roomdt.RoomCode.StartsWith("DS")) select roomdt;
                                var roomlist = from roomdt in subhoteldata.RoomDatas where (!roomdt.RoomCode.StartsWith("TW")) select roomdt;
                                foreach (var roomdata in roomlist)
                                {
                                    grouprate += ((decimal)roomdata.TotalAdultPrice + (decimal)roomdata.TotalChildPrice);
                                    respaxlist.Add(roomdata.RoomPax);
                                }
                                validpax = !reqpaxlist.Except(respaxlist).Any();
                                if (validpax)
                                {
                                    if (minrate == 0)
                                    {
                                        minrate = grouprate;
                                    }
                                    else
                                    {
                                        if (grouprate < minrate)
                                        {
                                            minrate = grouprate;
                                        }
                                    }
                                }
                            }
                            if (validpax)
                            {
                                string area = string.Empty;
                                if (!string.IsNullOrEmpty(HotelData["CityAreaName"].ToString()))
                                {
                                    area = HotelData["CityAreaName"].ToString();
                                }
                                else
                                {
                                    area = HotelData["CityName"].ToString() + " - City";
                                }
                                XElement hoteldata = new XElement("Hotel", new XElement("HotelID", hotel.HotelCode),
                                    new XElement("HotelName", HotelData["HotelName"].ToString()),
                                    new XElement("PropertyTypeName"),
                                    new XElement("CountryID"),
                                    new XElement("CountryName", HotelData["CountryName"].ToString()),
                                    new XElement("CountryCode", HotelData["CountryCode"].ToString()),
                                    new XElement("CityId"),
                                    new XElement("CityCode", HotelData["CityCode"].ToString()),
                                    new XElement("CityName", HotelData["CityName"].ToString()),
                                    new XElement("AreaId"),
                                    new XElement("AreaName", area),
                                    new XElement("RequestID"),
                                    new XElement("Address", HotelData["Address"].ToString()),
                                    new XElement("Location", HotelData["LocationName"].ToString()),
                                    new XElement("Description"),
                                    new XElement("StarRating", getStarRating(StarRating, HotelData["StarRatingName"].ToString())),
                                    new XElement("MinRate", minrate),
                                    new XElement("HotelImgSmall", HotelData["ImagePath"].ToString()),
                                    new XElement("HotelImgLarge", HotelData["ImagePath"].ToString()),
                                    new XElement("MapLink"),
                                    new XElement("Longitude", HotelData["Longitude"].ToString()),
                                    new XElement("Latitude", HotelData["Latitude"].ToString()),
                                    new XElement("xmloutcustid", customerid),
                                    new XElement("xmlouttype", xmlouttype),
                                    new XElement("DMC", dmc), new XElement("SupplierID", SupplierId),
                                    new XElement("Currency", hotel.SubHotelData.FirstOrDefault().CurrencyCode != "PDS" ? hotel.SubHotelData.FirstOrDefault().CurrencyCode : "GBP"),
                                    new XElement("Offers"), 
                                    new XElement("Facilities",null),
                                    //HotelData["Facilities"].ToString().getHotelFacilities(),
                                    new XElement("Rooms")
                                    );
                                HotelsData.Add(hoteldata);
                            }
                        }
                    }
                    return HotelsData;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "getHotelAvailbalityMerge_HtId";
                ex1.PageName = "Travco";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }


        #endregion

        #region Hotel Star rating
        private string getStarRating(XElement Star, string travcoRating)
        {
            //string reqpath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Travco/StarRating.xml");
            // XElement req = XElement.Load(reqpath);
            return (from rating in Star.Descendants("Description")
                    where rating.Value == travcoRating
                    select rating.Attribute("Rating").Value).FirstOrDefault().ToString();

        }
        #endregion

        #region Hotel Description
        public XElement getHotelDescription(XElement req)
        {
            XElement hotelDesc = new XElement("Hotels");
            XElement HotelDescReq = req.Descendants("hoteldescRequest").FirstOrDefault();
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            XElement hotelDescResdoc = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XElement("Authentication", new XElement("AgentID", AgentID), new XElement("UserName", username), new XElement("Password", password), new XElement("ServiceType", ServiceType), new XElement("ServiceVersion", ServiceVersion))));

            try
            {
                TravcoHotelStatic TravcoStatic = new TravcoHotelStatic();
                DataTable HotelDetail = TravcoStatic.GetHotelDetails(req.Descendants("HotelID").FirstOrDefault().Value);

                var ct = HotelDetail.Rows.Count;
                hotelDescResdoc.Add(new XElement(soapenv + "Body", HotelDescReq, new XElement("hoteldescResponse", new XElement("Hotels", new XElement("Hotel", new XElement("HotelID", req.Descendants("HotelID").FirstOrDefault().Value),
                                    new XElement("Description", HotelDetail.Rows[0]["Details"].ToString()), HotelDetail.Rows[0]["Images"].ToString().getHotelImages(), HotelDetail.Rows[0]["Facilities"].ToString().getHotelFacilities(),
                                    new XElement("ContactDetails", new XElement("Phone", HotelDetail.Rows[0]["Telephone"].ToString()), new XElement("Fax", HotelDetail.Rows[0]["Fax"].ToString())),
                                    new XElement("CheckinTime", HotelDetail.Rows[0]["CheckInTime"].ToString()), new XElement("CheckoutTime", HotelDetail.Rows[0]["CheckOutTime"].ToString())
                                    )))));

                return hotelDescResdoc;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "getHotelDescription";
                ex1.PageName = "Travco";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                hotelDescResdoc.Add(new XElement(soapenv + "Body", HotelDescReq, new XElement("hoteldescResponse", new XElement("Hotels"))));
                return hotelDescResdoc;
            }
        }
        #endregion

        #region Hotel Rooms
        public XElement GetRoomAvail_travcoOUT(XElement req)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            XElement getrm = null;
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "7").ToList();
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
                        dmc = "Travco";
                    }
                    Travco travcoObj = new Travco(customerid);
                    roomavailabilityresponse.Add(travcoObj.getHotelRoomDetails(req, dmc, htlid, customerid));
                }
                #endregion
                getrm = new XElement("TotalRooms", roomavailabilityresponse);
                return getrm;
            }
            catch { return null; }
        }
        public XElement getHotelRoomDetails(XElement htlRoomReq, string xtype, string htlid, string custoid)
        {
            customerid = custoid;
            dmc = xtype;
            XElement roomReq = htlRoomReq;
            string travcoRequest = string.Empty;
            string travcoResponse = string.Empty;
            int index = 1;
            int singleroom = 0, twinroom = 0, tripleroom = 0, childroom = 0, quadroom = 0;
            XElement HotelsData = new XElement("Hotels");
            string username = roomReq.Descendants("UserName").Single().Value;
            string password = roomReq.Descendants("Password").Single().Value;
            string AgentID = roomReq.Descendants("AgentID").Single().Value;
            string ServiceType = roomReq.Descendants("ServiceType").Single().Value;
            string ServiceVersion = roomReq.Descendants("ServiceVersion").Single().Value;
            XElement searchReq = roomReq.Descendants("searchRequest").FirstOrDefault();
            XElement RoomDetails = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                   new XElement("Authentication", new XElement("AgentID", AgentID), new XElement("UserName", username), new XElement("Password", password),
                                   new XElement("ServiceType", ServiceType), new XElement("ServiceVersion", ServiceVersion))));
            try
            {

                string hotelrooms = string.Empty;
                HotelAvailabilityService.RequestBaseType requestBase = new HotelAvailabilityService.RequestBaseType() { AgentCode = AgentCode, AgentPassword = AgentPassword, Lang = "en-GB" };
                HotelAvailabilityService.checkAvailabilityByHotelCode getHotelRooms = new HotelAvailabilityService.checkAvailabilityByHotelCode();
                getHotelRooms.RequestBase = requestBase;
                //getHotelRooms.HotelCode = roomReq.Descendants("HotelID").FirstOrDefault().Value;
                getHotelRooms.HotelCode = htlid;
                getHotelRooms.CheckInDate = roomReq.Descendants("searchRequest").Descendants("FromDate").FirstOrDefault().Value.StringToDate();
                getHotelRooms.CheckOutDate = roomReq.Descendants("searchRequest").Descendants("ToDate").FirstOrDefault().Value.StringToDate();
                List<XElement> roomsTPax = new List<XElement>();
                roomsTPax = roomReq.Descendants("RoomPax").ToList();
                //Add room Data
                int rmcnt = 1;
                foreach (var tvroom in roomsTPax)
                {
                    Int32 adults = tvroom.Descendants("Adult").FirstOrDefault().Value.ModifyToInt() + (tvroom.Descendants("ChildAge") != null ? tvroom.Descendants("ChildAge").Where(x => Int32.Parse(x.Value) > 11).Count() : 0);
                    Int32 children = tvroom.Descendants("ChildAge") != null ? tvroom.Descendants("ChildAge").Where(x => Int32.Parse(x.Value) >= 2 && Int32.Parse(x.Value) <= 11).Count() : 0;
                    if (adults != 2)
                    {
                        int totaladults = adults + children;
                        tvroom.Add(new XElement("TravcoAdult", totaladults), new XElement("TRoom", rmcnt), new XElement("st", "0"));
                        if (totaladults == 1)
                        {
                            singleroom += 1;
                        }
                        else if (totaladults == 2)
                        {
                            twinroom += 1;
                        }
                        else if (totaladults == 3)
                        {
                            tripleroom += 1;
                        }
                        else if (totaladults == 4)
                        {
                            quadroom += 1;
                        }

                    }
                    else
                    {
                        if (children == 1)
                        {
                            twinroom += 1;
                            childroom += 1;
                            tvroom.Add(new XElement("TravcoAdult", 2), new XElement("TRoom", rmcnt), new XElement("st", "0"));
                        }
                        else
                        {
                            int totaladults = adults + children;

                            if (totaladults == 2)
                            {
                                twinroom += 1;
                                tvroom.Add(new XElement("TravcoAdult", totaladults), new XElement("TRoom", rmcnt), new XElement("st", "0"));
                            }
                            else if (totaladults == 3)
                            {
                                tripleroom += 1;
                                tvroom.Add(new XElement("TravcoAdult", totaladults), new XElement("TRoom", rmcnt), new XElement("st", "0"));
                            }
                            else if (totaladults == 4)
                            {
                                quadroom += 1;
                                tvroom.Add(new XElement("TravcoAdult", totaladults), new XElement("TRoom", rmcnt), new XElement("st", "0"));
                            }
                        }

                    }
                    rmcnt++;
                }
                HotelAvailabilityService.RoomData roomData = new HotelAvailabilityService.RoomData();
                if (singleroom > 0)
                {
                    roomData.SingleRoom = singleroom.ToString();
                }
                if (twinroom > 0)
                {
                    roomData.DoubleRoom = twinroom.ToString();
                }
                if (tripleroom > 0)
                {
                    roomData.TripleRoom = tripleroom.ToString();
                }
                if (quadroom > 0)
                {
                    roomData.QuadRoom = quadroom.ToString();
                }
                if (childroom > 0)
                {
                    roomData.ChildRoom = childroom.ToString();
                }

                getHotelRooms.RoomData = roomData;
                //Additional Data
                HotelAvailabilityService.AdditionalData addData = new HotelAvailabilityService.AdditionalData();
                addData.NeedTotalNoOfHotels = HotelAvailabilityService.YesNoType.yes;
                addData.NeedAvailableHotelsOnly = HotelAvailabilityService.YesNoType.yes;
                addData.NeedReductionAmount = HotelAvailabilityService.YesNoType.no;
                addData.NeedHotelMessages = HotelAvailabilityService.YesNoType.no;
                addData.NeedFreeNightDetail = HotelAvailabilityService.YesNoType.no;
                addData.NeedHotelAddress = HotelAvailabilityService.YesNoType.no;
                addData.NeedTelephoneNo = HotelAvailabilityService.YesNoType.no;
                addData.NeedFaxNo = HotelAvailabilityService.YesNoType.no;
                addData.NeedBedPicture = HotelAvailabilityService.YesNoType.no;
                addData.NeedMapPicture = HotelAvailabilityService.YesNoType.no;
                addData.NeedEmail = HotelAvailabilityService.YesNoType.no;
                addData.NeedPicture = HotelAvailabilityService.YesNoType.no;
                addData.NeedAmenity = HotelAvailabilityService.YesNoType.no;
                addData.NeedHotelDescription = HotelAvailabilityService.YesNoType.no;
                addData.NeedHotelCity = HotelAvailabilityService.YesNoType.no;
                addData.NeedArrivalPointMain = HotelAvailabilityService.YesNoType.no;
                addData.NeedArrivalPointOther = HotelAvailabilityService.YesNoType.no;
                addData.NeedGeoCodes = HotelAvailabilityService.YesNoType.no;
                addData.NeedHotelProperties = HotelAvailabilityService.YesNoType.no;
                addData.NeedLocation = HotelAvailabilityService.YesNoType.no;
                addData.NeedCityArea = HotelAvailabilityService.YesNoType.no;
                addData.NeedEnglishText = HotelAvailabilityService.YesNoType.no;
                getHotelRooms.AdditionalData = addData;

                HotelAvailabilityService.RequestCriteria reqCriteria = new HotelAvailabilityService.RequestCriteria();
                reqCriteria.ReturnRequestedAllRoomTypes = HotelAvailabilityService.YesNoType.yes;
                reqCriteria.SortingOrder = HotelAvailabilityService.RequestCriteriaSortingOrder.low;
                //reqCriteria.StarRating = "0";
                // reqCriteria.StartingNo = "1";
                //reqCriteria.EndingNo = "100";
                //  reqCriteria.Budget = 0;


                //  getHotelRooms.RequestCriteria = reqCriteria;
                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(getHotelRooms.GetType());
                    serializer.Serialize(stringwriter, getHotelRooms);
                    travcoRequest = stringwriter.ToString();
                }

                HotelAvailabilityService.HotelAvailabilityV7ServicePortTypeClient hotelAvailabilityService = new HotelAvailabilityService.HotelAvailabilityV7ServicePortTypeClient();
                HotelAvailabilityService.HotelAvailabilityV7Response hotelAvailabilityResponse = hotelAvailabilityService.checkAvailabilityByHotelCode(getHotelRooms);
                HotelAvailabilityService.Response rtnResponse = hotelAvailabilityResponse.Response;
                rtnResponse.CheckInDate = rtnResponse.CheckInDate.TravcoToLocalDate();
                rtnResponse.CheckOutDate = rtnResponse.CheckOutDate.TravcoToLocalDate();
                #region supplier log

                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(rtnResponse.GetType());
                    serializer.Serialize(stringwriter, rtnResponse);
                    travcoResponse = stringwriter.ToString();
                }

                APILogDetail log = new APILogDetail();
                log.customerID = customerid.ConvertToLong();
                log.LogTypeID = 2;
                log.LogType = "RoomAvail";
                log.SupplierID = 7;
                log.TrackNumber = roomReq.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = travcoRequest.ToString();
                log.logresponseXML = travcoResponse.ToString();
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion
                XElement roomdetails = new XElement("Rooms");
                bool AvlStatus = false;
                if (rtnResponse.HotelDatas.FirstOrDefault().Hotels.FirstOrDefault().Status.ToString() == "Available")
                {
                    AvlStatus = true;
                }
                TravcoHotelStatic TravcoStatic = new TravcoHotelStatic();
                DataTable ratePlans = TravcoStatic.GetHotelRatePlanDetail(getHotelRooms.HotelCode);
                foreach (var subhoteldata in rtnResponse.HotelDatas.FirstOrDefault().Hotels.FirstOrDefault().SubHotelData.ToList())
                {
                    XElement roomtype = new XElement("RoomTypes");
                    List<XElement> dwRoomTypes = new List<XElement>();
                    string RatePlanDesc = subhoteldata.RatePlanDetails.RatePlanDescription.ToString();
                    string mealplancode = string.Empty, mealplanname = string.Empty;
                    if (ratePlans != null)
                    {
                        var subRatePlan = ratePlans.Select("[RatePlanCode] = '" + subhoteldata.RatePlanDetails.RatePlanCode + "'").FirstOrDefault();
                        if (subRatePlan != null)
                        {
                            mealplancode = subRatePlan["BoardBasisCode"].ToString();
                            mealplanname = subRatePlan["BoardBasisName"].ToString();
                        }
                    }
                    decimal DoubleTotalRate = 0.00m;
                    decimal TotalRate = 0.00m;
                    string subHotelCode = subhoteldata.HotelCode;
                    int doubleroom = getHotelRooms.RoomData.DoubleRoom.ModifyToInt();
                    int singledbroom = getHotelRooms.RoomData.SingleRoom.ModifyToInt();
                    if (doubleroom > 0 || singledbroom > 0)
                    {
                        List<XElement> doubleroomlst = new List<XElement>();
                        List<XElement> singleroomlst = new List<XElement>();
                        int roomSeq = 0;
                        int slromseq = 0;
                        foreach (var room in subhoteldata.RoomDatas.Where(x => x.RoomPax == "2" && x.RoomCode.StartsWith("D")))
                        {
                            DoubleTotalRate = room.TotalAdultPrice + room.TotalChildPrice;
                            XElement dblroomtype = new XElement("RoomTypes", new XAttribute("TotalRate", DoubleTotalRate), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid), new XAttribute("HtlCode", rtnResponse.HotelDatas.FirstOrDefault().Hotels.FirstOrDefault().HotelCode), new XAttribute("CrncyCode", subhoteldata.CurrencyCode != "PDS" ? subhoteldata.CurrencyCode : "GBP"));
                            for (int i = 0; i < doubleroom; i++)
                            {
                                string[] arr = room.AdultPriceDetails.ToString().Split(';');
                                string Promotion = arr.Where(s => s.Contains("Inc")).FirstOrDefault();
                                var dpax = roomsTPax.Where(x => x.Element("TravcoAdult").Value == "2" && x.Element("st").Value == "0").FirstOrDefault();
                                var drm = dpax.Descendants("TRoom").FirstOrDefault().Value;
                                roomSeq = drm.ModifyToInt();
                                decimal TotalRoomRate = DoubleTotalRate / doubleroom;
                                decimal PerNytRoomRate = TotalRoomRate / roomReq.Descendants("Nights").FirstOrDefault().Value.ModifyToInt();
                                XElement roomDtl = new XElement("Room", new XAttribute("ID", room.RoomCode), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", roomSeq), new XAttribute("SessionID", room.PriceCode), new XAttribute("RoomType", room.RoomName), new XAttribute("OccupancyID", subHotelCode),
                                                   new XAttribute("OccupancyName", RatePlanDesc), new XAttribute("MealPlanID", room.CancellationChargeCode), new XAttribute("MealPlanName", mealplanname), new XAttribute("MealPlanCode", mealplancode), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", PerNytRoomRate),
                                                   new XAttribute("TotalRoomRate", TotalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", AvlStatus),
                                                   new XElement("RequestID"), new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions", Promotion != null ? Promotion : string.Empty)),
                                                   new XElement("CancellationPolicy"), new XElement("Amenities", new XElement("Amenity")),
                                                   new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"),
                                                   new XElement(getPriceBreakup(room.AdultPriceDetails.ToString(), (room.TotalChildPrice / doubleroom).ToString())),
                                                  new XElement("AdultNum", dpax.Descendants("Adult").FirstOrDefault().Value),
                                                  new XElement("ChildNum", dpax.Descendants("Child").FirstOrDefault().Value));
                                dblroomtype.Add(roomDtl);
                                foreach (var troom in roomsTPax)
                                {
                                    if (troom.Element("TRoom").Value == drm)
                                    {
                                        troom.Element("st").Value = "1";
                                    }
                                }
                            }
                            doubleroomlst.Add(dblroomtype);
                        }
                        foreach (var room in subhoteldata.RoomDatas.Where(x => x.RoomPax == "1"))
                        {
                            XElement sglroomtype = new XElement("RoomTypes", new XAttribute("TotalRate", room.TotalAdultPrice), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid), new XAttribute("HtlCode", rtnResponse.HotelDatas.FirstOrDefault().Hotels.FirstOrDefault().HotelCode), new XAttribute("CrncyCode", subhoteldata.CurrencyCode != "PDS" ? subhoteldata.CurrencyCode : "GBP"));
                            int count = Convert.ToInt32(getHotelRooms.RoomData.SingleRoom);
                            string roompax = room.RoomPax;
                            for (int i = 0; i < count; i++)
                            {
                                var spax = roomsTPax.Where(x => x.Element("TravcoAdult").Value == "1" && x.Element("st").Value == "0").FirstOrDefault();
                                var srm = spax.Descendants("TRoom").FirstOrDefault().Value;
                                slromseq = srm.ModifyToInt();
                                string[] arr = room.AdultPriceDetails.ToString().Split(';');
                                string Promotion = arr.Where(s => s.Contains("Inc")).FirstOrDefault();
                                decimal TotalRoomRate = room.TotalAdultPrice / count;
                                decimal PerNytRoomRate = TotalRoomRate / roomReq.Descendants("Nights").FirstOrDefault().Value.ModifyToInt();
                                XElement roomDtl = new XElement("Room", new XAttribute("ID", room.RoomCode), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", slromseq), new XAttribute("SessionID", room.PriceCode), new XAttribute("RoomType", room.RoomName), new XAttribute("OccupancyID", subHotelCode),
                                       new XAttribute("OccupancyName", RatePlanDesc), new XAttribute("MealPlanID", room.CancellationChargeCode), new XAttribute("MealPlanName", mealplanname), new XAttribute("MealPlanCode", mealplancode), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", PerNytRoomRate),
                                       new XAttribute("TotalRoomRate", TotalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", AvlStatus),
                                       new XElement("RequestID"), new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions", Promotion != null ? Promotion : string.Empty)), new XElement("CancellationPolicy"),
                                       new XElement("Amenities", new XElement("Amenity")), new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"),
                                       new XElement(getPriceBreakup(room.AdultPriceDetails.ToString(), (room.ChildPrice / count).ToString())),
                                      new XElement("AdultNum", spax.Descendants("Adult").FirstOrDefault().Value),
                                      new XElement("ChildNum", spax.Descendants("Child").FirstOrDefault().Value));
                                sglroomtype.Add(roomDtl);
                                foreach (var troom in roomsTPax)
                                {
                                    if (troom.Element("TRoom").Value == srm)
                                    {
                                        troom.Element("st").Value = "1";
                                    }
                                }
                            }
                            singleroomlst.Add(sglroomtype);
                        }
                        if (doubleroom > 0 && singledbroom > 0)
                        {
                            foreach (XElement dbroom in doubleroomlst)
                            {
                                foreach (XElement seroom in singleroomlst)
                                {
                                    XElement dbroomtype = new XElement("RoomTypes");
                                    if (seroom.Element("Room").Attribute("RoomSeq").Value.ModifyToInt() < dbroom.Element("Room").Attribute("RoomSeq").Value.ModifyToInt())
                                    {
                                        dbroomtype.Add(from room in seroom.Descendants("Room") select room);
                                        foreach (var room in dbroom.Descendants("Room"))
                                        {
                                            dbroomtype.Elements("Room").Where(x => (int)x.Attribute("RoomSeq") < (int)room.Attribute("RoomSeq")).Last().AddAfterSelf(room);
                                        }
                                        decimal DoubleTotalGrpRate = dbroom.Attribute("TotalRate").Value.ModifyToDecimal() + seroom.Attribute("TotalRate").Value.ModifyToDecimal();
                                        dbroomtype.Add(new XAttribute("TotalRate", DoubleTotalGrpRate), new XAttribute("Index", index), new XAttribute("DMCType", dmc), new XAttribute("HtlCode", rtnResponse.HotelDatas.FirstOrDefault().Hotels.FirstOrDefault().HotelCode), new XAttribute("CrncyCode", subhoteldata.CurrencyCode != "PDS" ? subhoteldata.CurrencyCode : "GBP"));
                                        dwRoomTypes.Add(dbroomtype);
                                    }
                                    else
                                    {
                                        dbroomtype.Add(from room in dbroom.Descendants("Room") select room);
                                        foreach (var room in seroom.Descendants("Room"))
                                        {
                                            dbroomtype.Elements("Room").Where(x => (int)x.Attribute("RoomSeq") < (int)room.Attribute("RoomSeq")).Last().AddAfterSelf(room);
                                        }
                                        decimal DoubleTotalGrpRate = dbroom.Attribute("TotalRate").Value.ModifyToDecimal() + seroom.Attribute("TotalRate").Value.ModifyToDecimal();
                                        dbroomtype.Add(new XAttribute("TotalRate", DoubleTotalGrpRate), new XAttribute("Index", index), new XAttribute("DMCType", dmc), new XAttribute("HtlCode", rtnResponse.HotelDatas.FirstOrDefault().Hotels.FirstOrDefault().HotelCode), new XAttribute("CrncyCode", subhoteldata.CurrencyCode != "PDS" ? subhoteldata.CurrencyCode : "GBP"));
                                        dwRoomTypes.Add(dbroomtype);
                                    }
                                    index++;
                                }
                            }
                        }
                        else if (doubleroom > 0 && singledbroom < 1)
                        {
                            foreach (XElement dbroom in doubleroomlst)
                            {
                                dbroom.Add(new XAttribute("Index", index));
                                dwRoomTypes.Add(dbroom);
                                index++;
                            }
                        }
                        else
                        {
                            foreach (XElement seroom in singleroomlst)
                            {
                                seroom.Add(new XAttribute("Index", index));
                                dwRoomTypes.Add(seroom);
                                index++;
                            }
                        }
                        foreach (var dwroomtype in dwRoomTypes)
                        {
                            decimal GroupRate = dwroomtype.Attribute("TotalRate").Value.ModifyToDecimal();
                            foreach (var room in subhoteldata.RoomDatas.Where(x => x.RoomPax != "1" && x.RoomPax != "2"))
                            {
                                GroupRate += room.TotalAdultPrice;
                                int count = 0;
                                string roompax = room.RoomPax;
                                if (roompax == "3")
                                {
                                    count = getHotelRooms.RoomData.TripleRoom.ModifyToInt();
                                }
                                else if (roompax == "4")
                                {
                                    count = getHotelRooms.RoomData.QuadRoom.ModifyToInt();
                                }
                                for (int i = 0; i < count; i++)
                                {
                                    var elpax = roomsTPax.Where(x => x.Element("TravcoAdult").Value == room.RoomPax && x.Element("st").Value == "0").FirstOrDefault();
                                    var rm = elpax.Descendants("TRoom").FirstOrDefault().Value;
                                    int oroomseq = rm.ModifyToInt();
                                    string[] arr = room.AdultPriceDetails.ToString().Split(';');
                                    string Promotion = arr.Where(s => s.Contains("Inc")).FirstOrDefault();
                                    decimal TotalRoomRate = room.TotalAdultPrice / count;
                                    decimal PerNytRoomRate = TotalRoomRate / Convert.ToInt32(roomReq.Descendants("Nights").FirstOrDefault().Value);
                                    XElement roomDtl = new XElement("Room", new XAttribute("ID", room.RoomCode), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", oroomseq), new XAttribute("SessionID", room.PriceCode), new XAttribute("RoomType", room.RoomName), new XAttribute("OccupancyID", subHotelCode),
                                           new XAttribute("OccupancyName", RatePlanDesc), new XAttribute("MealPlanID", room.CancellationChargeCode), new XAttribute("MealPlanName", mealplanname), new XAttribute("MealPlanCode", mealplancode), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", PerNytRoomRate),
                                           new XAttribute("TotalRoomRate", TotalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", AvlStatus), new XElement("RequestID"),
                                           new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions", Promotion != null ? Promotion : string.Empty)), new XElement("CancellationPolicy", ""), new XElement("Amenities", new XElement("Amenity")),
                                           new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"),
                                           new XElement(getPriceBreakup(room.AdultPriceDetails.ToString(), (room.ChildPrice / count).ToString())),
                                           new XElement("AdultNum", elpax.Descendants("Adult").FirstOrDefault().Value),
                                           new XElement("ChildNum", elpax.Descendants("Child").FirstOrDefault().Value));
                                    int ct = dwroomtype.Elements("Room").Where(x => (int)x.Attribute("RoomSeq") < oroomseq).Count();
                                    if (ct == 0)
                                    {
                                        dwroomtype.Elements("Room").Where(x => (int)x.Attribute("RoomSeq") > oroomseq).FirstOrDefault().AddBeforeSelf(roomDtl);
                                    }
                                    else
                                    {
                                        dwroomtype.Elements("Room").Where(x => (int)x.Attribute("RoomSeq") < oroomseq).Last().AddAfterSelf(roomDtl);
                                    }
                                    foreach (var troom in roomsTPax)
                                    {
                                        if (troom.Element("TRoom").Value == rm)
                                        {
                                            troom.Element("st").Value = "1";
                                        }
                                    }
                                }
                            }
                            dwroomtype.Attribute("TotalRate").Value = GroupRate.ToString();
                            if (dwroomtype.Descendants("Room").Count() == roomsTPax.Count())
                            {
                                roomdetails.Add(dwroomtype);
                            }
                        }
                    }
                    else
                    {
                        foreach (var room in subhoteldata.RoomDatas)
                        {
                            TotalRate += room.TotalAdultPrice;
                            int count = 0;
                            string roompax = room.RoomPax;
                            if (roompax == "1")
                            {
                                count = getHotelRooms.RoomData.SingleRoom.ModifyToInt();
                            }
                            else if (roompax == "2")
                            {
                                count = getHotelRooms.RoomData.DoubleRoom.ModifyToInt();
                            }
                            else if (roompax == "3")
                            {
                                count = getHotelRooms.RoomData.TripleRoom.ModifyToInt();
                            }
                            else if (roompax == "4")
                            {
                                count = getHotelRooms.RoomData.QuadRoom.ModifyToInt();
                            }
                            for (int i = 0; i < count; i++)
                            {
                                var elpax = roomsTPax.Where(x => x.Element("TravcoAdult").Value == room.RoomPax && x.Element("st").Value == "0").FirstOrDefault();
                                var rm = elpax.Descendants("TRoom").FirstOrDefault().Value;
                                int roomSeq = rm.ModifyToInt();
                                string[] arr = room.AdultPriceDetails.ToString().Split(';');
                                string Promotion = arr.Where(s => s.Contains("Inc")).FirstOrDefault();
                                decimal TotalRoomRate = room.TotalAdultPrice / count;
                                decimal PerNytRoomRate = TotalRoomRate / roomReq.Descendants("Nights").FirstOrDefault().Value.ModifyToInt();
                                XElement roomDtl = new XElement("Room", new XAttribute("ID", room.RoomCode), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", roomSeq), new XAttribute("SessionID", room.PriceCode), new XAttribute("RoomType", room.RoomName), new XAttribute("OccupancyID", subHotelCode),
                                       new XAttribute("OccupancyName", RatePlanDesc), new XAttribute("MealPlanID", room.CancellationChargeCode), new XAttribute("MealPlanName", mealplanname), new XAttribute("MealPlanCode", mealplancode), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", PerNytRoomRate),
                                       new XAttribute("TotalRoomRate", TotalRoomRate), new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", AvlStatus), new XElement("RequestID"), new XElement("Offers"),
                                       new XElement("PromotionList", new XElement("Promotions", Promotion != null ? Promotion : string.Empty)), new XElement("CancellationPolicy"), new XElement("Amenities", new XElement("Amenity")), new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                       new XElement("Supplements"), new XElement(getPriceBreakup(room.AdultPriceDetails.ToString(), room.ChildPrice.ToString())),
                                       new XElement("AdultNum", elpax.Descendants("Adult").FirstOrDefault().Value),
                                       new XElement("ChildNum", elpax.Descendants("Child").FirstOrDefault().Value));
                                if (i == 0)
                                {
                                    roomtype.Add(roomDtl);
                                }
                                else
                                {
                                    int ct = roomtype.Elements("Room").Where(x => (int)x.Attribute("RoomSeq") < roomSeq).Count();
                                    if (ct == 0)
                                    {
                                        roomtype.Elements("Room").Where(x => (int)x.Attribute("RoomSeq") > roomSeq).FirstOrDefault().AddBeforeSelf(roomDtl);
                                    }
                                    else
                                    {
                                        roomtype.Elements("Room").Where(x => (int)x.Attribute("RoomSeq") < roomSeq).FirstOrDefault().AddAfterSelf(roomDtl);
                                    }
                                }
                                foreach (var troom in roomsTPax)
                                {
                                    if (troom.Element("TRoom").Value == rm)
                                    {
                                        troom.Element("st").Value = "1";
                                    }
                                }
                            }
                        }
                        roomtype.Add(new XAttribute("TotalRate", TotalRate), new XAttribute("Index", index), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid), new XAttribute("HtlCode", rtnResponse.HotelDatas.FirstOrDefault().Hotels.FirstOrDefault().HotelCode), new XAttribute("CrncyCode", subhoteldata.CurrencyCode != "PDS" ? subhoteldata.CurrencyCode : "GBP"));
                        if (roomtype.Descendants("Room").Count() == roomsTPax.Count())
                        {
                            roomdetails.Add(roomtype);
                        }
                        index++;
                    }
                    foreach (var troom in roomsTPax)
                    {
                        troom.Element("st").Value = "0";
                    }
                }
                foreach (var hotel in rtnResponse.HotelDatas.FirstOrDefault().Hotels)
                {

                    //decimal minrate = 0.0m;
                    XElement hoteldata = new XElement("Hotel",
                        new XElement("HotelID", hotel.HotelCode),
                        new XElement("HotelName", hotel.HotelName),
                        new XElement("PropertyTypeName"),
                        new XElement("CountryID", ""),
                        new XElement("CountryName", roomReq.Descendants("CountryName").FirstOrDefault().Value),
                        //new XElement("CityId", roomReq.Descendants("CityID").FirstOrDefault().Value),
                        //new XElement("CityCode", roomReq.Descendants("CityCode").FirstOrDefault().Value),
                        //new XElement("CityName", roomReq.Descendants("CityName").FirstOrDefault().Value),
                        //new XElement("AreaId"),
                        //new XElement("AreaName"),
                        //new XElement("RequestID"),
                        //new XElement("Address"),
                        //new XElement("Location"),
                        //new XElement("Description"),
                        //new XElement("StarRating"),
                        //new XElement("MinRate", minrate),
                        //new XElement("HotelImgSmall"),
                        //new XElement("HotelImgLarge"),
                        //new XElement("MapLink"),
                        //new XElement("Longitude"),
                        //new XElement("Latitude"),
                        new XElement("DMC", dmc),
                        new XElement("SupplierID", SupplierId),
                        new XElement("Currency", hotel.SubHotelData.FirstOrDefault().CurrencyCode != "PDS" ? hotel.SubHotelData.FirstOrDefault().CurrencyCode : "GBP"),
                        new XElement("Offers"),
                        //new XElement("Facilities", new XElement("Facility", "No Facility Available")),
                        new XElement(roomdetails)
                    );
                    HotelsData.Add(hoteldata);
                }
                RoomDetails.Add(new XElement(soapenv + "Body", searchReq, new XElement("searchResponse", HotelsData)));
                return RoomDetails;
            }
            catch (Exception ex)
            {
                APILogDetail log = new APILogDetail();
                log.customerID = customerid.ConvertToLong();
                log.LogTypeID = 2;
                log.LogType = "RoomAvail";
                log.SupplierID = 7;
                log.TrackNumber = roomReq.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = travcoRequest.ToString();
                log.logresponseXML = string.Empty;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);

                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "getHotelRoomDetails";
                ex1.PageName = "Travco";
                ex1.CustomerID = customerid;
                ex1.TranID = roomReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                RoomDetails.Add(new XElement(soapenv + "Body", searchReq, new XElement("searchResponse", new XElement("ErrorTxt", "Room is not available"), HotelsData)));
                return RoomDetails;
            }
        }


        #region Price Breakup
        private XElement getPriceBreakup(string AdultPriceDetails, string ChildPrice)
        {
            decimal earlyDis = 0.0m;
            List<string> arr = AdultPriceDetails.Split(';').Where(x => !x.Contains("%")).ToList();
            List<string> arrDis = AdultPriceDetails.Split(';').Where(x => x.Contains("%")).ToList();
            if (arrDis.Count() > 0)
            {
                earlyDis = arrDis[0].Split('%')[1].ModifyToDecimal();
            }

            decimal childprice = Convert.ToDecimal(ChildPrice);
            string[] prices = null;
            List<string> pricebreakup = new List<string>();
            List<string> chpricebreakup = new List<string>();
            XElement pricebrk = new XElement("PriceBreakups");
            int index = 1;
            foreach (var ar in arr.Where(s => !s.Contains("Inc")))
            {
                int nights = (ar.Split('@').Length == 1 ? "1" : ar.Split('@')[0].Split(' ')[1].Split('n')[0]).ModifyToInt();
                for (int i = 0; i < nights; i++)
                {
                    pricebreakup.Add(ar.Split('@').Length == 1 ? "0" : (ar.Split('@')[1].ModifyToDecimal() - Math.Floor((ar.Split('@')[1].ModifyToDecimal() * earlyDis / 100) * 100) / 100).ToString());
                }
            }
            if (arr.Where(s => s.Contains("Inc")).FirstOrDefault() != null)
            {
                int freeNyt = Convert.ToInt32(arr.Where(s => s.Contains("Inc")).FirstOrDefault().Split(' ')[1]);
                int totalNyt = pricebreakup.Count - 1;
                for (int i = totalNyt; i > totalNyt - freeNyt; i--)
                {
                    pricebreakup[i] = "0";
                }
            }
            if (childprice > 0)
            {
                int count = pricebreakup.Count(x => x != "0");
                decimal nytchildprice = Math.Round(childprice / count, 2);
                for (int i = 0; i < pricebreakup.Count; i++)
                {
                    if (pricebreakup[i] != "0") pricebreakup[i] = (Math.Round(pricebreakup[i].ModifyToDecimal(), 2) + nytchildprice).ToString();
                }
            }

            foreach (var ar in pricebreakup)
            {
                pricebrk.Add(new XElement("Price", new XAttribute("Night", index), new XAttribute("PriceValue", ar)));
                index++;
            }
            return pricebrk;

        }
        #endregion
        #endregion
        #region Hotel Cancellation Policy
        public XElement HotelCancellationPolicy(XElement CxlPolicyReq)
        {
            string username = CxlPolicyReq.Descendants("UserName").Single().Value;
            string password = CxlPolicyReq.Descendants("Password").Single().Value;
            string AgentID = CxlPolicyReq.Descendants("AgentID").Single().Value;
            string ServiceType = CxlPolicyReq.Descendants("ServiceType").Single().Value;
            string ServiceVersion = CxlPolicyReq.Descendants("ServiceVersion").Single().Value;
            XElement CxlReq = CxlPolicyReq.Descendants("hotelcancelpolicyrequest").FirstOrDefault();
            XElement CxlPolicyResponse = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                         new XElement("Authentication", new XElement("AgentID", AgentID), new XElement("UserName", username), new XElement("Password", password), new XElement("ServiceType", ServiceType),
                                         new XElement("ServiceVersion", ServiceVersion))));
            try
            {
                DateTime CheckInDate = CxlPolicyReq.Descendants("FromDate").FirstOrDefault().Value.StringToDate();
                double Duration = (CxlPolicyReq.Descendants("ToDate").FirstOrDefault().Value.StringToDate() - CxlPolicyReq.Descendants("FromDate").FirstOrDefault().Value.StringToDate()).TotalDays;
                List<XElement> rooms = CxlPolicyReq.Descendants("Room").ToList();
                XElement CXLPolicy = getCxlPolicy(rooms, CheckInDate, Duration, CxlPolicyReq.Descendants("CustomerID").FirstOrDefault().Value, CxlPolicyReq.Descendants("TransID").FirstOrDefault().Value);
                List<XElement> cxlPolicyLst = CXLPolicy.Descendants("CancellationPolicy").Distinct().ToList();
                if (cxlPolicyLst.Count == 0)
                {
                    CxlPolicyResponse.Add(new XElement(soapenv + "Body", CxlReq, new XElement("HotelDetailwithcancellationResponse", new XElement("ErrorTxt", "No cancellation policy found"))));
                }
                else
                {
                    CxlPolicyResponse.Add(new XElement(soapenv + "Body", CxlReq, new XElement("HotelDetailwithcancellationResponse",
                                          new XElement("Hotels", new XElement("Hotel", new XElement("HotelID", CxlPolicyReq.Descendants("HotelID").FirstOrDefault().Value),
                                          new XElement("HotelName"), new XElement("HotelImgSmall"), new XElement("HotelImgLarge"), new XElement("MapLink"),
                                          new XElement("DMC", "Travco"), new XElement("Currency"), new XElement("Offers"),
                                          new XElement("Rooms", new XElement("Room", new XAttribute("ID", CxlPolicyReq.Descendants("Room").FirstOrDefault().Attribute("ID").Value),
                                          new XAttribute("RoomType", ""), new XAttribute("PerNightRoomRate", CxlPolicyReq.Descendants("PerNightRoomRate").FirstOrDefault().Value),
                                          new XAttribute("TotalRoomRate", CxlPolicyReq.Descendants("TotalRoomRate").FirstOrDefault().Value),
                                          new XAttribute("LastCancellationDate", ""), CXLPolicy)))))));

                }

                return CxlPolicyResponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelCancellationPolicy";
                ex1.PageName = "Travco";
                ex1.CustomerID = CxlPolicyReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = CxlPolicyReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                CxlPolicyResponse.Add(new XElement(soapenv + "Body", CxlReq, new XElement("HotelDetailwithcancellationResponse", new XElement("ErrorTxt", "No cancellation policy found"))));
                return CxlPolicyResponse;
            }
        }

        public XElement getCxlPolicy(List<XElement> Rooms, DateTime CheckInDate, double Duration, string CustomerId, string TrackId)
        {
            string hostname = string.Empty;
            string weburl = string.Empty;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(CustomerId, "7");
            hostname = suppliercred.Descendants("host").FirstOrDefault().Value;
            weburl = suppliercred.Descendants("requesturl").FirstOrDefault().Value;

            Dictionary<DateTime, decimal> cxlPolicies = new Dictionary<DateTime, decimal>();
            DateTime lastCxldate = DateTime.MaxValue.Date;
            StringBuilder soapRequest = new StringBuilder("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"");
            XNamespace ns = XNamespace.Get("http://www.travco.co.uk/trlink/xsd/hotelcancellationdetailv7/response");
            try
            {
                foreach (var room in Rooms)
                {
                    //HttpWebRequest CxlRequest = (HttpWebRequest)WebRequest.Create(@"http://v8apitest1.travco.co.uk:8080/trlinkjws/services/HotelCancellationDetailV7Service.HotelCancellationDetailV7HttpSoap11Endpoint/HTTP/1.1");      //test
                    // HttpWebRequest CxlRequest = (HttpWebRequest)WebRequest.Create(@"http://xmlv7.travco.co.uk:8080/trlinkjws/services/HotelCancellationDetailV7Service.HotelCancellationDetailV7HttpSoap11Endpoint/HTTP/1.1");     //live
                    HttpWebRequest CxlRequest = (HttpWebRequest)WebRequest.Create(weburl);
                    CxlRequest.Headers.Add(@"SOAPAction: urn:getHotelCancellationDetailsNotCrossSeason");
                    CxlRequest.ContentType = "text/xml;charset=UTF-8";
                    CxlRequest.Method = "POST";
                    //CxlRequest.Host = "v8apitest1.travco.co.uk:8080";   //test
                    CxlRequest.Host = hostname;     //live
                    Stream requestStream = CxlRequest.GetRequestStream();
                    StreamWriter streamWriter = new StreamWriter(requestStream, Encoding.ASCII);
                    soapRequest.Append(" xmlns:req=\"http://www.travco.co.uk/trlink/xsd/hotelcancellationdetailv7/request\">");
                    soapRequest.Append("<soapenv:Header/><soapenv:Body>");
                    soapRequest.Append("<req:getHotelCancellationDetails><req:RequestBase AgentCode=\"" + AgentCode + "\" AgentPassword=\"" + AgentPassword + "\" Lang=\"en-GB\"/>");
                    soapRequest.Append("<req:HotelCode>" + room.Attribute("OccupancyID").Value + "</req:HotelCode>");
                    soapRequest.Append("<req:CheckInDate>" + CheckInDate.ToString("yyyy-MM-dd") + "T00:00:00" + "</req:CheckInDate>");
                    soapRequest.Append("<req:Duration>" + Duration + "</req:Duration>");
                    soapRequest.Append("<req:CancellationChargeCode>" + room.Attribute("MealPlanID").Value + "</req:CancellationChargeCode>");
                    soapRequest.Append("</req:getHotelCancellationDetails>");
                    soapRequest.Append("</soapenv:Body></soapenv:Envelope>");

                    streamWriter.Write(soapRequest.ToString().Trim());
                    streamWriter.Close();
                    HttpWebResponse wr = (HttpWebResponse)CxlRequest.GetResponse();
                    StreamReader srd = new StreamReader(wr.GetResponseStream());
                    XDocument cxlPolicy = XDocument.Parse(srd.ReadToEnd());

                    APILogDetail log = new APILogDetail();
                    log.customerID = CustomerId.ConvertToLong();
                    log.LogTypeID = 3;
                    log.LogType = "CXLPolicy";
                    log.SupplierID = 7;
                    log.TrackNumber = TrackId;
                    log.logrequestXML = soapRequest.ToString();
                    log.logresponseXML = cxlPolicy.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);

                    decimal perNytePrice = room.Attribute("PerNightRoomRate").Value.ModifyToDecimal();
                    string CxlDesc = cxlPolicy.Descendants(ns + "FullCancellationPolicy").FirstOrDefault().Value;
                    int nytes = (CxlDesc.Split('-')[1].Split(' ')[1]).ModifyToInt();
                    decimal cxlPer = CxlDesc.Split(' ').Last().Split('%')[0].ModifyToDecimal();
                    int days = cxlPolicy.Descendants(ns + "Detail").FirstOrDefault().Attribute("NoOfDaysBeforeArrival").Value.ModifyToInt();
                    DateTime Cxldate = CheckInDate.AddDays(-days).Date;
                    if (Cxldate > DateTime.Now.Date)
                    {
                        if (Cxldate.AddDays(-1) < lastCxldate)
                        {
                            lastCxldate = Cxldate.AddDays(-1);
                        }
                    }
                    else
                    {
                        Cxldate = DateTime.Now.Date;
                        lastCxldate = Cxldate.AddDays(-1).Date;
                    }
                    decimal cxlCharges = 0.0m;
                    if (nytes > Duration)
                    {
                        cxlCharges = (perNytePrice * (int)Duration);
                    }
                    else
                    {
                        cxlCharges = (perNytePrice * cxlPer / 100) * nytes;
                    }
                    if (cxlPolicies.Count == 0)
                    {
                        cxlPolicies.Add(Cxldate, cxlCharges);
                    }
                    else
                    {
                        int count = cxlPolicies.Count;
                        for (int i = 0; i < count; i++)
                        {
                            var item = cxlPolicies.ElementAt(i);
                            if (item.Key == Cxldate)
                            {
                                cxlPolicies[item.Key] = item.Value + cxlCharges;
                            }
                            else if (item.Key < Cxldate)
                            {
                                cxlPolicies.Add(Cxldate, item.Value + cxlCharges);
                            }
                            else
                            {
                                cxlPolicies.Add(Cxldate, cxlCharges);
                                cxlPolicies[item.Key] = item.Value + cxlCharges;
                            }
                        }

                    }
                }
                cxlPolicies.Add(lastCxldate, 0);
                XElement cxlplcy = new XElement("CancellationPolicies", 
                    from polc in cxlPolicies.OrderBy(k => k.Key) 
                    select new XElement("CancellationPolicy", 
                        new XAttribute("LastCancellationDate", polc.Key.ToString("dd'/'MM'/'yyyy")), 
                        new XAttribute("ApplicableAmount", polc.Value), 
                        new XAttribute("NoShowPolicy", "0")));
                return cxlplcy;
            }
            catch (Exception ex)
            {
                APILogDetail log = new APILogDetail();
                log.customerID = CustomerId.ConvertToLong();
                log.LogTypeID = 3;
                log.LogType = "CXLPolicy";
                log.SupplierID = 7;
                log.TrackNumber = TrackId;
                log.logrequestXML = soapRequest.ToString();
                log.logresponseXML = string.Empty;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);

                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelCancellationPolicy";
                ex1.PageName = "Travco";
                ex1.CustomerID = CustomerId;
                ex1.TranID = TrackId;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                XElement cxlplcy = new XElement("CancellationPolicies");
                return cxlplcy;
            }
        }
        #endregion

        #region Hotel PreBooking
        public XElement HotelPreBooking(XElement preBookReq, string xmlout)
        {
            string travcoRequest = string.Empty;
            string travcoResponse = string.Empty;
            dmc = xmlout;
            int count = 1;
            string username = preBookReq.Descendants("UserName").Single().Value;
            string password = preBookReq.Descendants("Password").Single().Value;
            string AgentID = preBookReq.Descendants("AgentID").Single().Value;
            string ServiceType = preBookReq.Descendants("ServiceType").Single().Value;
            string ServiceVersion = preBookReq.Descendants("ServiceVersion").Single().Value;
            XElement preBookReqest = preBookReq.Descendants("HotelPreBookingRequest").FirstOrDefault();
            XElement PreBookResponse = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XElement("Authentication", new XElement("AgentID", AgentID), new XElement("UserName", username), new XElement("Password", password),
                                       new XElement("ServiceType", ServiceType), new XElement("ServiceVersion", ServiceVersion))));
            try
            {
                decimal TotalRateOld = preBookReq.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value.ModifyToDecimal();
                string allocationEquiry = string.Empty;
                DateTime CheckInDate = preBookReq.Descendants("FromDate").FirstOrDefault().Value.StringToDate();
                double duration = (preBookReq.Descendants("ToDate").FirstOrDefault().Value.StringToDate() - preBookReq.Descendants("FromDate").FirstOrDefault().Value.StringToDate()).TotalDays;

                AllocationEnquiryService.getAllocationEnquiryForEnquiries getAllocationEnquiry = new AllocationEnquiryService.getAllocationEnquiryForEnquiries();
                getAllocationEnquiry.RequestBase = new AllocationEnquiryService.RequestBaseType() { AgentCode = AgentCode, AgentPassword = AgentPassword, Lang = "en-GB" };
                List<AllocationEnquiryService.Enquiry> enquiries = new List<AllocationEnquiryService.Enquiry>();
                foreach (var room in preBookReq.Descendants("Room").ToList())
                {
                    AllocationEnquiryService.Enquiry enquiry = new AllocationEnquiryService.Enquiry();
                    enquiry.HotelCode = room.Attribute("OccupancyID").Value;
                    enquiry.CheckInDate = CheckInDate;
                    enquiry.Duration = duration.ToString();
                    string[] child;
                    int childnum = 0;
                    child = !string.IsNullOrEmpty(room.Attribute("ChildAge").Value) ? room.Attribute("ChildAge").Value.Split(',') : null;
                    if (child != null)
                    {
                        childnum = child.Where(x => Int32.Parse(x) >= 2 && Int32.Parse(x) <= 11).Count();
                    }
                    int adults = room.Attribute("Adult").Value.ModifyToInt() + (child != null ? child.Where(x => Int32.Parse(x) > 11).Count() : 0);
                    if (adults != 2)
                    {
                        int totaladults = adults + childnum;
                        enquiry.NoOfAdults = totaladults.ToString();
                        enquiry.NoOfPaxInRoom = totaladults.ToString();
                    }
                    else
                    {
                        if (childnum == 1)
                        {
                            enquiry.NoOfAdults = adults.ToString();
                            enquiry.NoOfPaxInRoom = adults.ToString();
                            enquiry.NoOfChildren = childnum.ToString();
                        }
                        else
                        {
                            int totaladults = adults + childnum;
                            enquiry.NoOfAdults = totaladults.ToString();
                            enquiry.NoOfPaxInRoom = totaladults.ToString();
                        }

                    }
                    enquiry.EnquiryNo = count.ToString();
                    count += 1;
                    enquiries.Add(enquiry);
                }
                getAllocationEnquiry.Enquiries = enquiries.ToArray();
                AllocationEnquiryService.AdditionalData add_data = new AllocationEnquiryService.AdditionalData();
                add_data.NeedFreeNightDetails = AllocationEnquiryService.YesNoType.yes;
                add_data.NeedHotelMessages = AllocationEnquiryService.YesNoType.yes;
                add_data.NeedReductionAmount = AllocationEnquiryService.YesNoType.yes;
                getAllocationEnquiry.AdditionalData = add_data;
                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(getAllocationEnquiry.GetType());
                    serializer.Serialize(stringwriter, getAllocationEnquiry);
                    travcoRequest = stringwriter.ToString();
                }
                AllocationEnquiryService.AllocationEnquiryV7ServicePortTypeClient allocationEnquiryClient = new AllocationEnquiryService.AllocationEnquiryV7ServicePortTypeClient();
                AllocationEnquiryService.AllocationEnquiryV7Response allocationEnquiryRes = allocationEnquiryClient.getAllocationEnquiryForEnquiries(getAllocationEnquiry);
                AllocationEnquiryService.Response rtnResponse = allocationEnquiryRes.Response;
                Parallel.ForEach(rtnResponse.Allocations, l => l.Date = l.Date.TravcoToLocalDate());


                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(rtnResponse.GetType());
                    serializer.Serialize(stringwriter, rtnResponse);
                    travcoResponse = stringwriter.ToString();
                }

                APILogDetail log = new APILogDetail();
                log.customerID = preBookReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 4;
                log.LogType = "PreBook";
                log.SupplierID = 7;
                log.TrackNumber = preBookReq.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = travcoRequest.ToString();
                log.logresponseXML = travcoResponse.ToString();
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);

                bool AvlStatus = false;
                XElement roomtype = new XElement("RoomTypes");
                decimal TotalRate = 0.0m;
                string currency = rtnResponse.Allocations[0].CurrencyCode != "PDS" ? rtnResponse.Allocations[0].CurrencyCode : "GBP";
                foreach (var allocation in rtnResponse.Allocations)
                {
                    var room = preBookReq.Descendants("Room").Where(x => x.Attribute("RoomSeq").Value == allocation.EnquiryNo).FirstOrDefault();
                    if (allocation.RoomCode == room.Attribute("ID").Value)
                    {
                        AvlStatus = allocation.Status == "Avl" ? true : false;
                        int childnum = 0;
                        if (!string.IsNullOrEmpty(room.Attribute("ChildAge").Value))
                        {
                            string[] child = room.Attribute("ChildAge").Value.Split(',');
                            childnum = child.Count();
                        }
                        decimal TotalRoomRate = Convert.ToDecimal(allocation.TotalAdultPrice) + Convert.ToDecimal(allocation.TotalChildPrice);
                        decimal PerNightRoomRate = TotalRoomRate / allocation.Duration.ModifyToInt();
                        TotalRate += TotalRoomRate;
                        XElement roomDtl = new XElement("Room", new XAttribute("ID", room.Attribute("ID").Value), new XAttribute("SuppliersID", SupplierId), new XAttribute("RoomSeq", room.Attribute("RoomSeq").Value), new XAttribute("SessionID", allocation.PriceCode), new XAttribute("RoomType", room.Attribute("RoomType").Value), new XAttribute("OccupancyID", allocation.HotelCode),
                                           new XAttribute("OccupancyName", allocation.RatePlanDetails.RatePlanDescription), new XAttribute("MealPlanID", allocation.CancellationChargeCode), new XAttribute("MealPlanName", ""), new XAttribute("MealPlanCode", ""), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", PerNightRoomRate), new XAttribute("TotalRoomRate", TotalRoomRate),
                                           new XAttribute("CancellationDate", ""), new XAttribute("CancellationAmount", ""), new XAttribute("isAvailable", "true"), new XElement("RequestID"), new XElement("Offers"), new XElement("PromotionList", new XElement("Promotions")), new XElement("CancellationPolicy"), new XElement("Amenities", new XElement("Amenity")),
                                           new XElement("Images", new XElement("Image", new XAttribute("Path", ""))), new XElement("Supplements"), new XElement(getPriceBreakup(allocation.AdultPriceDetail.ToString(), (allocation.TotalChildPrice).ToString())), new XElement("AdultNum", room.Attribute("Adult").Value),
                                           new XElement("ChildNum", childnum));
                        roomtype.Add(roomDtl);

                    }

                }
                roomtype.Add(new XAttribute("Index", "1"), new XAttribute("TotalRate", TotalRate));

                roomtype.Add(getCxlPolicy(preBookReq.Descendants("Room").ToList(), CheckInDate, duration, preBookReq.Descendants("CustomerID").FirstOrDefault().Value, preBookReq.Descendants("TransID").FirstOrDefault().Value));
                XElement PreBookingdetails = new XElement("Hotels", new XElement("Hotel", new XElement("HotelID", preBookReq.Descendants("HotelID").FirstOrDefault().Value),
                                             new XElement("HotelName", preBookReq.Descendants("HotelName").FirstOrDefault().Value), new XElement("Status", AvlStatus),
                                             new XElement("TermCondition"), new XElement("HotelImgSmall"), new XElement("HotelImgLarge"),
                                             new XElement("MapLink"), new XElement("DMC", dmc), new XElement("Currency", currency),
                                             new XElement("Offers"), new XElement("Rooms", roomtype)));

                if (TotalRateOld == TotalRate)
                {
                    PreBookResponse.Add(new XElement(soapenv + "Body", preBookReqest, new XElement("HotelPreBookingResponse", new XElement("NewPrice", ""), PreBookingdetails)));
                }
                else
                {
                    PreBookResponse.Add(new XElement(soapenv + "Body", preBookReqest, new XElement("HotelPreBookingResponse", new XElement("ErrorTxt", "Amount has been changed"), new XElement("NewPrice", TotalRate), PreBookingdetails)));
                }

                return PreBookResponse;

            }
            catch (Exception ex)
            {
                APILogDetail log = new APILogDetail();
                log.customerID = preBookReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 4;
                log.LogType = "PreBook";
                log.SupplierID = 7;
                log.TrackNumber = preBookReq.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = travcoRequest.ToString();
                log.logresponseXML = travcoResponse.ToString();
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);

                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelPreBooking";
                ex1.PageName = "Travco";
                ex1.CustomerID = preBookReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = preBookReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                PreBookResponse.Add(new XElement(soapenv + "Body", preBookReqest, new XElement("HotelPreBookingResponse", new XElement("ErrorTxt", "Room is not Available"))));
                return PreBookResponse;
            }

        }

        #endregion

        #region Hotel Booking
        public XElement HotelBooking(XElement BookingReq)
        {
            string travcoRequest = string.Empty;
            string travcoResponse = string.Empty;
            string hotelbooking = string.Empty;
            string username = BookingReq.Descendants("UserName").Single().Value;
            string password = BookingReq.Descendants("Password").Single().Value;
            string AgentID = BookingReq.Descendants("AgentID").Single().Value;
            string ServiceType = BookingReq.Descendants("ServiceType").Single().Value;
            string ServiceVersion = BookingReq.Descendants("ServiceVersion").Single().Value;
            XElement searchReq = BookingReq.Descendants("HotelBookingRequest").FirstOrDefault();
            XElement HotelBookingRes = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                       new XElement("Authentication", new XElement("AgentID", AgentID), new XElement("UserName", username), new XElement("Password", password),
                                       new XElement("ServiceType", ServiceType), new XElement("ServiceVersion", ServiceVersion))));
            try
            {
                HotelBookingRequestService.RequestBaseType requestBase = new HotelBookingRequestService.RequestBaseType() { AgentCode = AgentCode, AgentPassword = AgentPassword, Lang = "en-GB" };
                HotelBookingRequestService.doHotelBooking doHotelBooking = new HotelBookingRequestService.doHotelBooking();
                doHotelBooking.RequestBase = requestBase;
                // doHotelBooking.ClerkCode = "ADM";
                List<HotelBookingRequestService.Booking> bookings = new List<HotelBookingRequestService.Booking>();
                DateTime CheckInDate = BookingReq.Descendants("FromDate").FirstOrDefault().Value.StringToDate();
                string Duration = (BookingReq.Descendants("ToDate").FirstOrDefault().Value.StringToDate() - BookingReq.Descendants("FromDate").FirstOrDefault().Value.StringToDate()).TotalDays.ToString();

                foreach (XElement element in BookingReq.Descendants("PassengersDetail").Descendants("Room"))
                {
                    HotelBookingRequestService.Booking booking = new HotelBookingRequestService.Booking();
                    HotelBookingRequestService.HotelData HotelData = new HotelBookingRequestService.HotelData();
                    string passengerName = string.Empty;
                    XElement leadguest = element.Descendants("PaxInfo").Where(x => x.Descendants("IsLead").FirstOrDefault().Value == "true").FirstOrDefault();
                    if (!string.IsNullOrEmpty(leadguest.Descendants("MiddleName").FirstOrDefault().Value))
                    {
                        if (!string.IsNullOrEmpty(leadguest.Descendants("Title").FirstOrDefault().Value))
                        {
                            passengerName = string.Format("{0}/{1}/{2}/{3}", leadguest.Descendants("LastName").FirstOrDefault().Value.ToUpper(), leadguest.Descendants("MiddleName").FirstOrDefault().Value.ToUpper(), leadguest.Descendants("FirstName").FirstOrDefault().Value.ToUpper(), leadguest.Descendants("Title").FirstOrDefault().Value.ToUpper());
                        }
                        else
                        {
                            passengerName = string.Format("{0}/{1}/{2}", leadguest.Descendants("LastName").FirstOrDefault().Value.ToUpper(), leadguest.Descendants("MiddleName").FirstOrDefault().Value.ToUpper(), leadguest.Descendants("FirstName").FirstOrDefault().Value.ToUpper());

                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(leadguest.Descendants("Title").FirstOrDefault().Value))
                        {
                            passengerName = string.Format("{0}/{1}/{2}", leadguest.Descendants("LastName").FirstOrDefault().Value.ToUpper(), leadguest.Descendants("FirstName").FirstOrDefault().Value.ToUpper(), leadguest.Descendants("Title").FirstOrDefault().Value.ToUpper());
                        }
                        else
                        {
                            passengerName = string.Format("{0}/{1}", leadguest.Descendants("LastName").FirstOrDefault().Value.ToUpper(), leadguest.Descendants("FirstName").FirstOrDefault().Value.ToUpper());

                        }

                    }
                    string AgentTxt = string.Empty;
                    string[] child;
                    child = !string.IsNullOrEmpty(element.Attribute("ChildAge").Value) ? element.Attribute("ChildAge").Value.Split(',') : null;
                    int adults = element.Attribute("Adult").Value.ModifyToInt() + (child != null ? child.Where(x => Int32.Parse(x) > 11).Count() : 0);

                    HotelData.AgentReference = BookingReq.Descendants("TransID").FirstOrDefault().Value;
                    HotelData.CheckInDate = CheckInDate;
                    HotelData.Duration = Duration;
                    HotelData.HotelCode = element.Attribute("OccupancyID").Value;
                    HotelData.RoomCode = element.Attribute("RoomTypeID").Value;
                    HotelData.PriceCode = element.Attribute("SessionID").Value;

                    if (child != null)
                    {
                        int childnum = child.Where(x => Int32.Parse(x) >= 2 && Int32.Parse(x) <= 11).Count();
                        int infants = child.Where(x => Int32.Parse(x) < 2).Count();
                        if (adults != 2)
                        {
                            int totaladults = adults + childnum;
                            HotelData.NoOfAdults = totaladults.ToString();
                        }
                        else
                        {
                            if (childnum == 1)
                            {
                                HotelData.NoOfAdults = adults.ToString();
                                HotelData.NoOfChildren = childnum.ToString();
                            }
                            else
                            {
                                int totaladults = adults + childnum;
                                HotelData.NoOfAdults = totaladults.ToString();
                            }

                        }
                        if (infants > 0)
                        {
                            HotelData.Infants = infants.ToString();
                            AgentTxt = "Cot, " + BookingReq.Descendants("SpecialRemarks").FirstOrDefault().Value;
                        }
                        else
                        {
                            AgentTxt = BookingReq.Descendants("SpecialRemarks").FirstOrDefault().Value;
                        }
                    }
                    else
                    {
                        HotelData.NoOfAdults = adults.ToString();
                        AgentTxt = BookingReq.Descendants("SpecialRemarks").FirstOrDefault().Value;
                    }

                    HotelData.AgentText = AgentTxt;
                    HotelData.PassengerName = passengerName;
                    booking.HotelData = HotelData;
                    bookings.Add(booking);
                    HotelData = null;
                    booking = null;
                }
                doHotelBooking.Bookings = bookings.ToArray();
                HotelBookingRequestService.AdditionalData data = new HotelBookingRequestService.AdditionalData();
                data.NeedHotelMessages = HotelBookingRequestService.YesNoType.yes;
                doHotelBooking.AdditionalData = data;
                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(doHotelBooking.GetType());
                    serializer.Serialize(stringwriter, doHotelBooking);
                    travcoRequest = stringwriter.ToString();
                }
                HotelBookingRequestService.HotelBookingV7RequestServicePortTypeClient hotelBookingRequestClient = new HotelBookingRequestService.HotelBookingV7RequestServicePortTypeClient();
                HotelBookingRequestService.HotelBookingV7Response hotelBookingRes = hotelBookingRequestClient.doHotelBooking(doHotelBooking);
                HotelBookingRequestService.Response rtnResponse = hotelBookingRes.Response;
                string bookingStatus = string.Empty;
                string errormsg = string.Empty;
                foreach (var booking in rtnResponse.Bookings)
                {
                    booking.CheckInDate = booking.CheckInDate.TravcoToLocalDate();
                    if (booking.BookingStatus == "B")
                    {
                        bookingStatus = "Success";
                    }
                    else if (booking.BookingStatus == "P")
                    {
                        bookingStatus = "Pending";
                        errormsg = booking.Message.ErrorCode + booking.Message.ErrorDescription;
                    }
                    else
                    {
                        bookingStatus = "Failed";
                        errormsg = booking.Message.ErrorCode + booking.Message.ErrorDescription;

                    }
                }
                #region Insert supplier log
                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(rtnResponse.GetType());
                    serializer.Serialize(stringwriter, rtnResponse);
                    travcoResponse = stringwriter.ToString();
                }
                APILogDetail log = new APILogDetail();
                log.customerID = BookingReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 5;
                log.LogType = "Book";
                log.SupplierID = 7;
                log.TrackNumber = BookingReq.Descendants("TransactionID").FirstOrDefault().Value;
                log.logrequestXML = travcoRequest;
                log.logresponseXML = travcoResponse;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion
                if (bookingStatus == "Success")
                {
                    XElement BookingRes = new XElement("HotelBookingResponse",
                                           new XElement("Hotels", new XElement("HotelID", BookingReq.Descendants("HotelID").FirstOrDefault().Value),
                                           new XElement("HotelName", BookingReq.Descendants("HotelName").FirstOrDefault().Value),
                                           new XElement("FromDate", BookingReq.Descendants("FromDate").FirstOrDefault().Value),
                                           new XElement("ToDate", BookingReq.Descendants("ToDate").FirstOrDefault().Value),
                                           new XElement("AdultPax", BookingReq.Descendants("Rooms").Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value),
                                           new XElement("ChildPax", BookingReq.Descendants("Rooms").Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value),
                                           new XElement("TotalPrice", BookingReq.Descendants("TotalAmount").FirstOrDefault().Value), new XElement("CurrencyID"),
                                           new XElement("CurrencyCode", rtnResponse.Bookings.FirstOrDefault().Currency.Code != "PDS" ? rtnResponse.Bookings.FirstOrDefault().Currency.Code : "GBP"),
                                           new XElement("MarketID"), new XElement("MarketName"), new XElement("HotelImgSmall"), new XElement("HotelImgLarge"), new XElement("MapLink"), new XElement("VoucherRemark"),
                                           new XElement("TransID", BookingReq.Descendants("TransID").FirstOrDefault().Value),
                                           new XElement("ConfirmationNumber", rtnResponse.Bookings.FirstOrDefault().BookingReference.Split('/')[0]),
                                           new XElement("Status", bookingStatus),
                                           new XElement("PassengersDetail", new XElement("GuestDetails",
                                           from bok in rtnResponse.Bookings
                                           select new XElement("Room", new XAttribute("ID", bok.RoomCode), new XAttribute("RoomType", bok.RoomName), new XAttribute("ServiceID", bok.BookingReference),
                                           new XAttribute("MealPlanID", bok.RatePlanDetails.RatePlanCode), new XAttribute("MealPlanName", bok.RatePlanDetails.RatePlanDescription),
                                           new XAttribute("MealPlanCode", bok.HotelCode), new XAttribute("MealPlanPrice", ""), new XAttribute("PerNightRoomRate", bok.RatePlanDetails.RatePlanCode),
                                           new XAttribute("RoomStatus", bok.BookingStatus == "B" ? "Confirm" : "Pending"), new XAttribute("TotalRoomRate", bok.TotalPrice),
                                           new XElement("RoomGuest", new XElement("GuestType", "Adult"), new XElement("Title"), new XElement("FirstName", bok.PassengerName),
                                           new XElement("MiddleName"), new XElement("LastName"), new XElement("IsLead", "true"), new XElement("Age")),
                                           new XElement("Supplements"))
                                           ))));

                    HotelBookingRes.Add(new XElement(soapenv + "Body", searchReq, BookingRes));
                }
                else
                {
                    HotelBookingRes.Add(new XElement(soapenv + "Body", searchReq, new XElement("HotelBookingResponse", new XElement("ErrorTxt", errormsg))));
                }
                return HotelBookingRes;

            }
            catch (Exception ex)
            {
                APILogDetail log = new APILogDetail();
                log.customerID = BookingReq.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 5;
                log.LogType = "Book";
                log.SupplierID = 7;
                log.TrackNumber = BookingReq.Descendants("TransactionID").FirstOrDefault().Value;
                log.logrequestXML = travcoRequest;
                log.logresponseXML = travcoResponse;
                SaveAPILog savelog = new SaveAPILog();

                savelog.SaveAPILogs(log);
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelBooking";
                ex1.PageName = "Travco";
                ex1.CustomerID = BookingReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = BookingReq.Descendants("TransactionID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                HotelBookingRes.Add(new XElement(soapenv + "Body", searchReq, new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Booking can not be confirmed!"))));
                return HotelBookingRes;
            }

        }

        #endregion

        #region Hotel Booking cancellation
        public XElement BookingCancellation(XElement req)
        {
            string travcoRequest = string.Empty;
            string travcoResponse = string.Empty;
            string travcoEnqRequest = string.Empty;
            string travcoEnqResponse = string.Empty;
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            XElement CxlReq = req.Descendants("HotelCancellationRequest").FirstOrDefault();
            XElement BookCXlRes = new XElement(soapenv + "Envelope", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv), new XElement(soapenv + "Header", new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                  new XElement("Authentication", new XElement("AgentID", AgentID), new XElement("UserName", username), new XElement("Password", password),
                                  new XElement("ServiceType", ServiceType), new XElement("ServiceVersion", ServiceVersion))));
            try
            {
                BookEnquiryService.getBookEnquiry getBookingEnquiry = new BookEnquiryService.getBookEnquiry();
                getBookingEnquiry.RequestBase = new BookEnquiryService.RequestBaseType() { AgentCode = AgentCode, AgentPassword = AgentPassword, Lang = "en-GB" };
                BookEnquiryService.BookEnquiry Enquiry = new BookEnquiryService.BookEnquiry();
                Enquiry.RefNo = req.Descendants("ServiceID").FirstOrDefault().Value;
                getBookingEnquiry.BookEnquiry = Enquiry;
                getBookingEnquiry.NeedAdditionalData = BookEnquiryService.YesNoType.yes;
                getBookingEnquiry.NeedHotelMessages = BookEnquiryService.YesNoType.yes;
                BookEnquiryService.BookEnquiryV7ServicePortTypeClient bookEnquiryClient = new BookEnquiryService.BookEnquiryV7ServicePortTypeClient();
                BookEnquiryService.BookEnquiryV7Response bookEnquiryRes = bookEnquiryClient.getBookEnquiry(getBookingEnquiry);
                BookEnquiryService.Response BokEnqResponse = bookEnquiryRes.Response;
                BokEnqResponse.Hotels.FirstOrDefault().ArrivalDate = BokEnqResponse.Hotels.FirstOrDefault().ArrivalDate.TravcoToLocalDate();
                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(getBookingEnquiry.GetType());
                    serializer.Serialize(stringwriter, getBookingEnquiry);
                    travcoEnqRequest = stringwriter.ToString();
                }
                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(BokEnqResponse.GetType());
                    serializer.Serialize(stringwriter, BokEnqResponse);
                    travcoEnqResponse = stringwriter.ToString();
                }
                APILogDetail Enqlog = new APILogDetail();
                Enqlog.customerID = req.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                Enqlog.LogTypeID = 6;
                Enqlog.LogType = "BookEnq";
                Enqlog.SupplierID = 7;
                Enqlog.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value;
                Enqlog.logrequestXML = travcoEnqRequest;
                Enqlog.logresponseXML = travcoEnqResponse;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(Enqlog);

                HotelBookingRequestService.cancelHotelBooking cancelHotelBooking = new HotelBookingRequestService.cancelHotelBooking();
                cancelHotelBooking.RequestBase = new HotelBookingRequestService.RequestBaseType() { AgentCode = AgentCode, AgentPassword = AgentPassword, Lang = "en-GB" };
                cancelHotelBooking.BookingReference = BokEnqResponse.Hotels.FirstOrDefault().TravcoRefNo;
                HotelBookingRequestService.Booking booking = new HotelBookingRequestService.Booking();
                HotelBookingRequestService.HotelData HotelData = new HotelBookingRequestService.HotelData();
                HotelData.AgentReference = BokEnqResponse.Hotels.FirstOrDefault().AgentRefNo;
                HotelData.AgentText = BokEnqResponse.Hotels.FirstOrDefault().Comments;
                HotelData.CheckInDate = BokEnqResponse.Hotels.FirstOrDefault().ArrivalDate.Date;
                HotelData.Duration = BokEnqResponse.Hotels.FirstOrDefault().Duration;
                HotelData.HotelCode = BokEnqResponse.Hotels.FirstOrDefault().HotelCode;
                HotelData.RoomCode = BokEnqResponse.Hotels.FirstOrDefault().RoomCode;
                HotelData.PriceCode = BokEnqResponse.Hotels.FirstOrDefault().PriceCode;
                HotelData.NoOfAdults = BokEnqResponse.Hotels.FirstOrDefault().NoOfAdults;
                HotelData.NoOfChildren = BokEnqResponse.Hotels.FirstOrDefault().NoOfChildren;
                if (BokEnqResponse.Hotels.FirstOrDefault().Infants != null)
                {
                    HotelData.Infants = BokEnqResponse.Hotels.FirstOrDefault().Infants;
                }

                //HotelData.HotelRequest.
                HotelData.PassengerName = BokEnqResponse.Hotels.FirstOrDefault().LeadPassengerName;
                booking.HotelData = HotelData;
                cancelHotelBooking.Booking = booking;
                HotelBookingRequestService.AdditionalData data = new HotelBookingRequestService.AdditionalData();
                data.NeedHotelMessages = HotelBookingRequestService.YesNoType.yes;
                cancelHotelBooking.AdditionalData = data;
                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(cancelHotelBooking.GetType());
                    serializer.Serialize(stringwriter, cancelHotelBooking);
                    travcoRequest = stringwriter.ToString();
                }
                HotelBookingRequestService.HotelBookingV7RequestServicePortTypeClient cancellationBookingClient = new HotelBookingRequestService.HotelBookingV7RequestServicePortTypeClient();
                HotelBookingRequestService.HotelBookingV7Response cancelBookingRes = cancellationBookingClient.cancelHotelBooking(cancelHotelBooking);
                HotelBookingRequestService.Response rtnResponse = cancelBookingRes.Response;
                rtnResponse.Bookings.FirstOrDefault().CheckInDate = rtnResponse.Bookings.FirstOrDefault().CheckInDate.TravcoToLocalDate();

                #region Insert supplier log

                using (StringWriter stringwriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(rtnResponse.GetType());
                    serializer.Serialize(stringwriter, rtnResponse);
                    travcoResponse = stringwriter.ToString();
                }
                APILogDetail log = new APILogDetail();
                log.customerID = req.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 6;
                log.LogType = "Cancel";
                log.SupplierID = 7;
                log.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = travcoRequest;
                log.logresponseXML = travcoResponse;
                SaveAPILog savelogc = new SaveAPILog();
                savelogc.SaveAPILogs(log);
                #endregion
                string status = rtnResponse.Bookings.FirstOrDefault().BookingStatus == "C" ? "Success" : "Fail";
                decimal CxlAmt = rtnResponse.Bookings.FirstOrDefault().AdultPrice + rtnResponse.Bookings.FirstOrDefault().ChildPrice;
                BookCXlRes.Add(new XElement(soapenv + "Body", CxlReq, new XElement("HotelCancellationResponse", new XElement("Rooms", new XElement("Room", new XElement("Cancellation", new XElement("Amount", CxlAmt), new XElement("Status", status)))))));
                return BookCXlRes;

            }
            catch (Exception ex)
            {
                #region Insert supplier log if got exception from supplier
                APILogDetail log = new APILogDetail();
                log.customerID = req.Descendants("CustomerID").FirstOrDefault().Value.ConvertToLong();
                log.LogTypeID = 6;
                log.LogType = "Cancel";
                log.SupplierID = 7;
                log.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = travcoRequest;
                log.logresponseXML = travcoResponse;
                SaveAPILog savelogc = new SaveAPILog();
                savelogc.SaveAPILogs(log);
                #endregion
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "BookingCancellation";
                ex1.PageName = "Travco";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                BookCXlRes.Add(new XElement(soapenv + "Body", CxlReq, new XElement("HotelCancellationResponse", new XElement("ErrorTxt", "There is some technical error"))));
                return BookCXlRes;
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