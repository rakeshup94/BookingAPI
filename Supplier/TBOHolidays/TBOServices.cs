using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Common;
using TravillioXMLOutService.Models.TBO;

namespace TravillioXMLOutService.Supplier.TBOHolidays
{
    public class TBOServices
    {
        XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";
        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
        XNamespace hot = "http://TekTravel/HotelBookingApi";
        XNamespace wsa = "http://www.w3.org/2005/08/addressing";
        string DMC, hotelcode, currencyCode;
        TBORequest treq = new TBORequest();
        TBO_Data tbd = new TBO_Data();
        int _supplierID = 21;
        string customerid = string.Empty;
        string trans_id = string.Empty;
        string BookingId = string.Empty;
        XElement travreq = null;
        #region Hotel Search
        string GetHotelCode(string SupCityId, string HotelId, string HotelName)
        {
            string codeList = string.Empty;
            int CityId = Convert.ToInt32(SupCityId);
            var model = new SqlModel()
            {
                flag = 2,
                columnList = "HotelID,HotelName,StarRating",
                table = "tblTBOHotelList",
                filter = "CityID=" + CityId.ToString() + " AND HotelName LIKE '%" + HotelName + "%'",
                SupplierId = 21
            };
            if (!string.IsNullOrEmpty(HotelId))
            {
                model.HotelCode = HotelId;
            }
            DataTable htlList = TravayooRepository.GetData(model);
            if (htlList.Rows.Count > 0)
            {
                foreach (DataRow item in htlList.Rows)
                {
                    codeList += item["HotelID"] + ",";
                }

                codeList = codeList.Remove(codeList.Length - 1);
            }
            else
            {
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt32(customerid);
                    log.LogTypeID = 1;
                    log.LogType = "Search";
                    log.SupplierID = 21;
                    log.TrackNumber = trans_id;
                    log.logrequestXML = null;
                    log.logresponseXML = "There is no hotel available in database";
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch
                {
                }
                return null;
                //throw new Exception("There is no hotel available in database");
            }


            return codeList;
        }
        public List<XElement> HotelSearch(XElement Req, string custID, string xtype)
        {
            customerid = custID;
            trans_id = Req.Descendants("TransID").FirstOrDefault().Value;
            DMC = xtype;
            List<XElement> HotelList = new List<XElement>();
            try
            {
                DataTable CityMapping = tbd.CityMapping(Req.Descendants("CityID").FirstOrDefault().Value, "21");
                XElement SuplCreds = supplier_Cred.getsupplier_credentials(customerid, Convert.ToString(_supplierID));
                string url = SuplCreds.Element("URL").Value;
                string Action = SuplCreds.Element("HotelSearch").Value;
                string UserName = SuplCreds.Element("UserName").Value;
                string PWord = SuplCreds.Element("Password").Value;
                List<string> Destinations = new List<string>();
                if (CityMapping.Rows.Count == 0)
                {
                    #region Log Save
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(customerid);
                        log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                        log.SupplierID = 21;
                        log.logrequestXML = string.Empty;
                        log.logresponseXML = "City not mapped";
                        log.LogType = "Search";
                        log.LogTypeID = 1;
                        log.StartTime = DateTime.Now;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelSearchResponse";
                        ex1.PageName = "RestelServices";
                        ex1.CustomerID = customerid;
                        ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    #endregion
                }
                for (int i = 0; i < CityMapping.Rows.Count; i++)
                {
                    string city = CityMapping.Rows[i]["SupCityID"].ToString();
                    DataTable CityData = tbd.CityData(city);
                    Destinations.Add(city + "_" + CityData.Rows[0]["CityName"].ToString() + "_" + CityData.Rows[0]["CountryName"].ToString());
                }
                XElement AreaInfo = tagInfo(Convert.ToInt32(Req.Descendants("CountryID").FirstOrDefault().Value));
                string rating = StarRating(Req.Descendants("MinStarRating").FirstOrDefault().Value, Req.Descendants("MaxStarRating").FirstOrDefault().Value);
                foreach (string city in Destinations)
                {
                    string[] destination = city.Split(new char[] { '_' });

                    string HotelCodes = string.Empty;
                    if (!string.IsNullOrEmpty(Req.Descendants("HotelID").FirstOrDefault().Value))
                    {
                        HotelCodes = GetHotelCode(destination[0], Req.Descendants("HotelID").FirstOrDefault().Value, Req.Descendants("HotelName").FirstOrDefault().Value);
                    }


                    XDocument searchReq = new XDocument(new XDeclaration("1,0", "UTF-8", "yes"),
                                                    new XElement(soap + "Envelope",
                                                        new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                                        new XAttribute(XNamespace.Xmlns + "hot", hot),
                                                        new XElement(soap + "Header",
                                                        new XAttribute(XNamespace.Xmlns + "wsa", wsa),
                                                            new XElement(hot + "Credentials",
                                                                new XAttribute("UserName", UserName),
                                                                new XAttribute("Password", PWord)),
                                                            new XElement(wsa + "Action", Action),
                                                            new XElement(wsa + "To", url)
                                                            ),
                                                        new XElement(soap + "Body",
                                                            new XElement(hot + "HotelSearchRequest",
                                                                new XElement(hot + "CheckInDate", ReformatDate(Req.Descendants("FromDate").FirstOrDefault().Value)),
                                                                new XElement(hot + "CheckOutDate", ReformatDate(Req.Descendants("ToDate").FirstOrDefault().Value)),
                                                                new XElement(hot + "CountryName", destination[2]),
                                                                new XElement(hot + "CityName", destination[1]),
                                                                new XElement(hot + "CityId", destination[0]),
                                                                new XElement(hot + "IsNearBySearchAllowed", false),
                                                                new XElement(hot + "NoOfRooms", Req.Descendants("RoomPax").Count()),
                                                                new XElement(hot + "GuestNationality", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                                                new XElement(hot + "RoomGuests", roomGuests(Req.Descendants("Rooms").FirstOrDefault())),
                        //new XElement(hot + "ResultCount"),
                                                                //new XElement(hot + "Filters",
                                                                //    string.IsNullOrEmpty(rating) ? null : new XElement(hot + "StarRating", rating))

                                                                    new XElement(hot + "Filters",
                                                                    (string.IsNullOrEmpty(HotelCodes) ? null : new XElement(hot + "HotelCodeList", HotelCodes)),
                                                                    (string.IsNullOrEmpty(rating) ? null : new XElement(hot + "StarRating", rating)))
                        //new XElement(hot + "ResponseTime")
                                                                ))));
                    Log_Model model = new Log_Model
                    {
                        CustomerID = Convert.ToInt32(customerid),
                        Logtype = "Search",
                        LogtypeID = 1,
                        Supl_Id = _supplierID,
                        TrackNo = Req.Descendants("TransID").FirstOrDefault().Value
                    };
                    XDocument Response = treq.Request(searchReq, url, Action, model);
                    XElement suplResponse = removeAllNamespaces(Response.Root);
                    if (suplResponse.Descendants("StatusCode").FirstOrDefault().Value.Equals("01"))
                    {
                        string xmlouttype = string.Empty;
                        try
                        {
                            if (DMC == "TBO")
                            {
                                xmlouttype = "false";
                            }
                            else
                            { xmlouttype = "true"; }
                        }
                        catch { }
                        string reqID = suplResponse.Descendants("SessionId").FirstOrDefault().Value;
                        foreach (XElement hotel in suplResponse.Descendants("HotelResult"))
                        {
                            string min = Req.Descendants("MinStarRating").FirstOrDefault().Value, max = Req.Descendants("MaxStarRating").FirstOrDefault().Value;
                            bool StarRatingCheck = string.IsNullOrEmpty(rating) ? StarRating(min, max, hotel.Descendants("Rating").FirstOrDefault().Value) : true;
                            if (StarRatingCheck)
                            {
                                string areaName = string.Empty;
                                try
                                {
                                    List<string> receivedTags = hotel.Descendants("TagIds").FirstOrDefault().Value.Split(new char[] { ',' }).ToList();
                                    if (receivedTags != null)
                                    {
                                        foreach (string tag in receivedTags)
                                        {
                                            areaName = AreaInfo.Descendants("Tag").Any() ?
                                                AreaInfo.Descendants("Tag").Where(x => x.Element("TagID").Value.Equals(tag)).FirstOrDefault().Element("Location").Value : string.Empty;
                                            if (string.IsNullOrEmpty(areaName))
                                                continue;
                                            else
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        areaName = string.Empty;
                                    }
                                }
                                catch { areaName = string.Empty; }
                                HotelList.Add(new XElement("Hotel",
                                                    new XElement("HotelID", hotel.Descendants("HotelCode").FirstOrDefault().Value),
                                                    new XElement("HotelName", hotel.Descendants("HotelName") != null ? hotel.Descendants("HotelName").FirstOrDefault().Value : string.Empty),
                                                    new XElement("PropertyTypeName", ""),
                                                    new XElement("CountryID", Req.Descendants("CountryID").FirstOrDefault().Value),
                                                    new XElement("CountryName", Req.Descendants("CountryName").FirstOrDefault().Value),
                                                    new XElement("CountryCode", Req.Descendants("CountryCode").FirstOrDefault().Value),
                                                    new XElement("CityId", Req.Descendants("CityID").FirstOrDefault().Value),
                                                    new XElement("CityCode", Req.Descendants("CityCode").FirstOrDefault().Value),
                                                    new XElement("CityName", Req.Descendants("CityName").FirstOrDefault().Value),
                                                    new XElement("AreaId"),
                                                    new XElement("AreaName", areaName),
                                                    new XElement("RequestID", reqID + "_" + hotel.Descendants("ResultIndex").FirstOrDefault().Value),
                                                    new XElement("Address", hotel.Descendants("HotelAddress") != null ? hotel.Descendants("HotelAddress").FirstOrDefault().Value : string.Empty),
                                                    new XElement("Location"),
                                                    new XElement("Description"),
                                                    new XElement("StarRating", getRating(hotel.Descendants("Rating").FirstOrDefault().Value)),
                                                    new XElement("MinRate", hotel.Descendants("MinHotelPrice").FirstOrDefault().Attribute("TotalPrice").Value),
                                                    new XElement("HotelImgSmall", hotel.Descendants("HotelPicture").Count() != 0 ? hotel.Descendants("HotelPicture").FirstOrDefault().Value : string.Empty),
                                                    new XElement("HotelImgLarge", hotel.Descendants("HotelPicture").Count() != 0 ? hotel.Descendants("HotelPicture").FirstOrDefault().Value : string.Empty),
                                                    new XElement("MapLink"),
                                                    new XElement("Longitude", hotel.Descendants("Longitude") != null ? hotel.Descendants("Longitude").FirstOrDefault().Value : string.Empty),
                                                    new XElement("Latitude", hotel.Descendants("Latitude") != null ? hotel.Descendants("Latitude").FirstOrDefault().Value : string.Empty),
                                                    new XElement("xmloutcustid", customerid),
                                                    new XElement("xmlouttype", xmlouttype),
                                                    new XElement("DMC", DMC),
                                                    new XElement("SupplierID", _supplierID.ToString()),
                                                    new XElement("Currency", hotel.Descendants("MinHotelPrice").FirstOrDefault().Attribute("Currency").Value),
                                                    new XElement("Offers"),
                                                    new XElement("Facilities", new XElement("Facility", "No Facility")),
                                                    new XElement("Rooms")));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelSearch";
                ex1.PageName = "TBOServices";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            return HotelList;
        }
        #endregion
        #region Hotel Detail
        public XElement HotelDetail(XElement Req)
        {
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            XElement Response = new XElement("hoteldescResponse");
            try
            {
                string hotelID = Req.Descendants("HotelID").FirstOrDefault().Value;
                DataTable HotelDetails = tbd.hotelDetail(hotelID);
                if (HotelDetails.Rows.Count > 0)
                {
                    XElement Images = new XElement("Images");
                    DataTable ImageTable = tbd.hotelImages(hotelID);
                    for (int i = 0; i < ImageTable.Rows.Count; i++)
                    {
                        Images.Add(new XElement("Image",
                                        new XAttribute("Path", ImageTable.Rows[i]["ImageURL"].ToString()),
                                        new XAttribute("Caption", "")));
                    }
                    XElement Facilities = new XElement("Facilities");
                    string[] FacilityArray = HotelDetails.Rows[0]["Facilities"].ToString().Split(new char[] { '#' });
                    for (int i = 0; i < FacilityArray.Length; i++)
                        Facilities.Add(new XElement("Facility", FacilityArray[i]));
                    Response.Add(new XElement("Hotels",
                                    new XElement("Hotel",
                                        new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                        new XElement("Description", HotelDetails.Rows[0]["Description"].ToString()),
                                        Images,
                                        Facilities,
                                        new XElement("ContactDetails",
                                            new XElement("Phone", HotelDetails.Rows[0]["Telephone"].ToString()),
                                            new XElement("Fax", HotelDetails.Rows[0]["Fax"].ToString())),
                                        new XElement("CheckinTime"),
                                        new XElement("CheckoutTime"))));
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelDetail";
                ex1.PageName = "TBOServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            XElement DetailsResponse = new XElement(soapenv + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                        new XElement(soapenv + "Header",
                                            new XElement("Authentication",
                                            new XElement("AgentID", AgentID),
                                            new XElement("Username", username),
                                            new XElement("Password", password),
                                            new XElement("ServiceType", ServiceType),
                                            new XElement("ServiceVersion", ServiceVersion))),
                                        new XElement(soapenv + "Body",
                                            Req.Descendants("hoteldescRequest").FirstOrDefault(),
                                            Response));
            return DetailsResponse;
        }
        #endregion
        #region Room Availability
        public XElement getroomavail_tboOUT(XElement req)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            XElement getrm = null;
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "21").ToList();
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
                        dmc = "TBO";
                    }
                    roomavailabilityresponse.Add(RoomAvail(req, dmc, htlid, customerid));
                }
                #endregion
                getrm = new XElement("TotalRooms", roomavailabilityresponse);
                return getrm;
            }
            catch { return null; }
        }
        public XElement RoomAvail(XElement Req, string dmc, string htlcode, string custoID)
        {
            DMC = dmc;
            hotelcode = htlcode;
            XElement availResponse = new XElement("searchResponse");
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                #region Supplier Credential
                XElement SuplCreds = supplier_Cred.getsupplier_credentials(custoID, Convert.ToString(_supplierID));
                string url = SuplCreds.Element("URL").Value;
                string Action = SuplCreds.Element("AvailableHotelRoom").Value;
                string UserName = SuplCreds.Element("UserName").Value;
                string PWord = SuplCreds.Element("Password").Value;
                #endregion
                string[] requestData = Req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "21").FirstOrDefault().Attribute("GRequestID").Value.Split(new char[] { '_' });
                XDocument RoomRequest = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                            new XElement(soap + "Envelope",
                                                new XAttribute(XNamespace.Xmlns + "soap", soap),
                                                new XAttribute(XNamespace.Xmlns + "hot", hot),
                                                new XElement(soap + "Header",
                                                    new XAttribute(XNamespace.Xmlns + "wsa", wsa),
                                                    new XElement(hot + "Credentials",
                                                        new XAttribute("UserName", UserName),
                                                        new XAttribute("Password", PWord)),
                                                    new XElement(wsa + "Action", Action),
                                                    new XElement(wsa + "To", url)),
                                                new XElement(soap + "Body",
                                                    new XElement(hot + "HotelRoomAvailabilityRequest",
                                                        new XElement(hot + "SessionId", requestData[0]),
                                                        new XElement(hot + "ResultIndex", requestData[1]),
                                                        new XElement(hot + "HotelCode", hotelcode)))));
                Log_Model model = new Log_Model
                {
                    CustomerID = Convert.ToInt32(custoID),
                    Supl_Id = _supplierID,
                    LogtypeID = 2,
                    Logtype = "RoomAvail",
                    TrackNo = Req.Descendants("TransID").FirstOrDefault().Value
                };
                XDocument SupplierResponse = treq.Request(RoomRequest, url, Action, model);
                XElement Response = removeAllNamespaces(SupplierResponse.Root);
                if (Response.Descendants("StatusCode").FirstOrDefault().Value.Equals("01"))
                {
                    currencyCode = Response.Descendants("RoomRate").FirstOrDefault().Attribute("Currency").Value;
                    var availabilityResponse = new XElement("Hotel",
                                                    new XElement("HotelID", hotelcode),
                                                    new XElement("HotelName", Req.Descendants("HotelName").FirstOrDefault().Value),
                                                    new XElement("PropertyTypeName"),
                                                    new XElement("CountryID", Req.Descendants("CountryID").FirstOrDefault().Value),

                                                    new XElement("CountryCode"),
                                                    new XElement("CountryName", Req.Descendants("CountryName").FirstOrDefault().Value),
                                                    new XElement("CityId", Req.Descendants("CityID").FirstOrDefault().Value),
                                                    new XElement("CityCode", Req.Descendants("CityCode").FirstOrDefault().Value),
                                                    new XElement("CityName", Req.Descendants("CityName").FirstOrDefault().Value),
                                                    new XElement("AreaName"),
                                                    new XElement("AreaId"),
                                                    new XElement("Address"),
                                                    new XElement("Location"),
                                                    new XElement("Description"),
                                                    new XElement("StarRating"),
                                                    new XElement("MinRate"),
                                                    new XElement("HotelImgSmall"),
                                                    new XElement("HotelImgLarge"),
                                                    new XElement("MapLink"),
                                                    new XElement("Longitude"),
                                                    new XElement("Latitude"),
                                                    new XElement("DMC", DMC),
                                                    new XElement("SupplierID", _supplierID),
                                                    new XElement("Currency"),
                                                    new XElement("Offers"),
                                                    new XElement("Facilities", new XElement("Facility", "No Facility available")),
                                                    groupedRooms(Response, Req));
                    availResponse.Add(availabilityResponse);
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "RoomAvail";
                ex1.PageName = "TBOServices";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            XElement AvailablilityResponse = new XElement(
                                                    new XElement(soapenv + "Envelope",
                                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                        new XElement(soapenv + "Header",
                                                            new XElement("Authentication",
                                                                new XElement("AgentID", AgentID),
                                                                new XElement("Username", username),
                                                                new XElement("Password", password),
                                                                new XElement("ServiceType", ServiceType),
                                                                new XElement("ServiceVersion", ServiceVersion))),
                                                        new XElement(soapenv + "Body",
                                                            new XElement(removeAllNamespaces(Req.Descendants("searchRequest").FirstOrDefault())),
                                                           removeAllNamespaces(availResponse))));
            return AvailablilityResponse;
        }
        #endregion
        #region Cancellation Policy
        public XElement CancelPolicy(XElement Req)
        {
            XElement Response = new XElement("HotelDetailwithcancellationResponse");
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                #region Supplier Credential
                XElement SuplCreds = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, Convert.ToString(_supplierID));
                string url = SuplCreds.Element("URL").Value;
                string Action = SuplCreds.Element("HotelCancellationPolicy").Value;
                string UserName = SuplCreds.Element("UserName").Value;
                string PWord = SuplCreds.Element("Password").Value;
                #endregion
                string[] requestData = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value.Split(new char[] { '_' });
                XDocument PolicyReq = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                          new XElement(soap + "Envelope",
                                              new XAttribute(XNamespace.Xmlns + "soap", soap),
                                              new XAttribute(XNamespace.Xmlns + "hot", hot),
                                              new XElement(soap + "Header",
                                                  new XAttribute(XNamespace.Xmlns + "wsa", wsa),
                                                  new XElement(hot + "Credentials",
                                                      new XAttribute("UserName", UserName),
                                                      new XAttribute("Password", PWord)),
                                                  new XElement(wsa + "Action", Action),
                                                  new XElement(wsa + "To", url)),
                                             new XElement(soap + "Body",
                                                 new XElement(hot + "HotelCancellationPolicyRequest",
                                                     new XElement(hot + "ResultIndex", requestData[1]),
                                                     new XElement(hot + "SessionId", requestData[0]),
                                                     new XElement(hot + "OptionsForBooking",
                                                     new XElement(hot + "FixedFormat", requestData[2]),
                                                     new XElement(hot + "RoomCombination",
                                                         from room in Req.Descendants("Room")
                                                         select new XElement(hot + "RoomIndex", room.Attribute("OccupancyID").Value)))))));
                Log_Model model = new Log_Model
                {
                    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                    Logtype = "CXPolicy",
                    LogtypeID = 3,
                    Supl_Id = _supplierID,
                    TrackNo = Req.Descendants("TransID").FirstOrDefault().Value
                };
                XElement SuplResponse = removeAllNamespaces(treq.Request(PolicyReq, url, Action, model).Root);
                #region Old Method
                //foreach(XElement policy in SuplResponse.Descendants("CancelPolicy"))
                //{
                //    double Amount = 0.0, roomPrice = 0.0;
                //    switch(policy.Attribute("ChargeType").Value)
                //    {
                //        case "Fixed":
                //            Amount = Convert.ToDouble(policy.Attribute("CancellationCharge").Value);
                //            break;
                //        case "Percentage":
                //            roomPrice = policy.Attributes("RoomIndex").Any() ?
                //                Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(policy.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("TotalRoomRate").Value) :
                //                Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("TotalRoomRate").Value);
                //            Amount = (Convert.ToDouble(policy.Attribute("CancellationCharge").Value) / 100) * roomPrice;
                //            break;
                //        case "Night":
                //            roomPrice = policy.Attributes("RoomIndex").Any() ?
                //                Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(policy.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("PerNightRoomRate").Value) :
                //                Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("PerNightRoomRate").Value);
                //            Amount = roomPrice * Convert.ToInt32(policy.Attribute("CancellationCharge").Value);
                //            break;
                //    }
                //    DateTime lastDate = DateTime.ParseExact(policy.Attribute("FromDate").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                //    Cp.Add(new XElement("CancellationPolicy",
                //        new XAttribute("LastCancellationDate", lastDate.ToString("dd/MM/yyyy")),
                //        new XAttribute("ApplicableAmount", Amount.ToString()),
                //        new XAttribute("NoShowPolicy","0")));
                //}
                //foreach(XElement noshow in SuplResponse.Descendants("NoShowPolicy"))
                //{
                //    double Amount = 0.0, roomPrice = 0.0;
                //    switch (noshow.Attribute("ChargeType").Value)
                //    {
                //        case "Fixed":
                //            Amount = Convert.ToDouble(noshow.Attribute("CancellationCharge").Value);
                //            break;
                //        case "Percentage":
                //            roomPrice = noshow.Attributes("RoomIndex").Any() ?
                //                Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(noshow.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("TotalRoomRate").Value) :
                //                Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("TotalRoomRate").Value);
                //            Amount = (Convert.ToDouble(noshow.Attribute("CancellationCharge").Value) / 100) * roomPrice;
                //            break;
                //        case "Night":
                //            roomPrice = noshow.Attributes("RoomIndex").Any() ?
                //                Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(noshow.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("PerNightRoomRate").Value) :
                //                Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("PerNightRoomRate").Value);
                //            Amount = roomPrice * Convert.ToInt32(noshow.Attribute("CancellationCharge").Value);
                //            break;
                //    }
                //    DateTime lastDate = DateTime.ParseExact(noshow.Attribute("FromDate").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                //    Cp.Add(new XElement("CancellationPolicy",
                //        new XAttribute("LastCancellationDate", lastDate.ToString("dd/MM/yyyy")),
                //        new XAttribute("ApplicableAmount", Amount.ToString()),
                //        new XAttribute("NoShowPolicy", "1")));
                //}
                //var policyGroups = from policy in Cp.Descendants("CancellationPolicy")
                //                  group policy by new
                //                  {
                //                      c1 = policy.Attribute("LastCancellationDate").Value,
                //                      c2 = policy.Attribute("NoShowPolicy").Value
                //                  };
                //XElement FinalCp = new XElement("CancellationPolicies");
                //foreach (var policyGroup in policyGroups)
                //{
                //    double amount = policyGroup.Select(x => Convert.ToDouble(x.Attribute("ApplicableAmount").Value)).Sum();
                //    FinalCp.Add(new XElement("CancellationPolicy",
                //                    new XAttribute("LastCancellationDate", policyGroup.Key.c1),
                //                    new XAttribute("ApplicableAmount", amount.ToString()),
                //                    new XAttribute("NoShowPolicy", policyGroup.Key.c2)));
                //} 
                #endregion
                XElement FinalCp = policyTagsNew(SuplResponse, Req);
                var cxp = new XElement("Hotels",
                                 new XElement("Hotel",
                                     new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                     new XElement("HotelName", Req.Descendants("HotelName").FirstOrDefault().Value),
                                     new XElement("HotelImgSmall"),
                                     new XElement("HotelImgLarge"),
                                     new XElement("MapLink"),
                                     new XElement("DMC", "TBO"),
                                     new XElement("Currency"),
                                     new XElement("Offers"),
                                     new XElement("Room",
                                         new XAttribute("ID", ""),
                                         new XAttribute("RoomType", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", ""),
                                         new XAttribute("TotalRoomRate", ""),
                                         new XAttribute("CancellationDate", ""),
                                        FinalCp)));
                Response.Add(cxp);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancelPolicy";
                ex1.PageName = "TBOServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            #region Response Format

            XElement CXpResponse = new XElement(
                                                new XElement(soapenv + "Envelope",
                                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                    new XElement(soapenv + "Header",
                                                        new XElement("Authentication",
                                                            new XElement("AgentID", AgentID),
                                                            new XElement("Username", username),
                                                            new XElement("Password", password),
                                                            new XElement("ServiceType", ServiceType),
                                                            new XElement("ServiceVersion", ServiceVersion))),
                                                    new XElement(soapenv + "Body",
                                                        new XElement(Req.Descendants("hotelcancelpolicyrequest").Any() ? removeAllNamespaces(Req.Descendants("hotelcancelpolicyrequest").FirstOrDefault()) : removeAllNamespaces(Req)),
                                                       removeAllNamespaces(Response))));
            #endregion
            return CXpResponse;
        }
        #endregion
        #region PreBooking
        public XElement PreBooking(XElement Req, string xmlout)
        {
            XElement Response = new XElement("HotelPreBookingResponse");
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                #region Supplier Credential
                XElement SuplCreds = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, Convert.ToString(_supplierID));
                string url = SuplCreds.Element("URL").Value;
                string Action = SuplCreds.Element("AvailabilityandPricing").Value;
                string UserName = SuplCreds.Element("UserName").Value;
                string PWord = SuplCreds.Element("Password").Value;
                #endregion
                DMC = xmlout;
                string[] requestData = Req.Descendants("RequestID").FirstOrDefault().Value.Split(new char[] { '_' });
                XDocument preBook = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                        new XElement(soap + "Envelope",
                                            new XAttribute(XNamespace.Xmlns + "soap", soap),
                                            new XAttribute(XNamespace.Xmlns + "hot", hot),
                                            new XElement(soap + "Header",
                                                new XAttribute(XNamespace.Xmlns + "wsa", wsa),
                                                new XElement(hot + "Credentials",
                                                    new XAttribute("UserName", UserName),
                                                    new XAttribute("Password", PWord)),
                                                new XElement(wsa + "Action", Action),
                                                new XElement(wsa + "To", url)),
                                            new XElement(soap + "Body",
                                                new XElement(hot + "AvailabilityAndPricingRequest",
                                                    new XElement(hot + "ResultIndex", requestData[1]),
                                                    new XElement(hot + "HotelCode", Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value),
                                                    new XElement(hot + "SessionId", requestData[0]),
                                                    new XElement(hot + "OptionsForBooking",
                                                    new XElement(hot + "FixedFormat", requestData[2]),
                                                    new XElement(hot + "RoomCombination",
                                                    from room in Req.Descendants("Room")
                                                    select new XElement(hot + "RoomIndex", room.Attribute("OccupancyID").Value)))))));
                Log_Model model = new Log_Model
                {
                    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                    Logtype = "PreBook",
                    LogtypeID = 4,
                    Supl_Id = _supplierID,
                    TrackNo = Req.Descendants("TransID").FirstOrDefault().Value
                };
                XDocument SuplResponse = treq.Request(preBook, url, Action, model);
                XElement Resp = removeAllNamespaces(SuplResponse.Root);
                if (Resp.Descendants("StatusCode").FirstOrDefault().Value.Equals("01"))
                {
                    bool AvailableForBook = Resp.Descendants("AvailableForBook").Any() ? Resp.Descendants("AvailableForBook").FirstOrDefault().Value.ToUpper().Equals("TRUE") : false;
                    bool HotelDetailsVerification = Resp.Descendants("HotelDetailsVerification").Any() ? Resp.Descendants("HotelDetailsVerification").FirstOrDefault().Attribute("Status").Value.ToUpper().Equals("SUCCESSFUL") : false;
                    bool CancellationPolicyAvailable = Resp.Descendants("CancellationPoliciesAvailable").Any() ? Resp.Descendants("CancellationPoliciesAvailable").FirstOrDefault().Value.ToUpper().Equals("TRUE") : false;
                    bool PriceVerification = Resp.Descendants("PriceVerification").Any() ? Resp.Descendants("PriceVerification").FirstOrDefault().Attribute("Status").Value.ToUpper().Equals("SUCCESSFUL") || Resp.Descendants("PriceVerification").FirstOrDefault().Attribute("Status").Value.Equals("NotAvailable") : false;
                    StringBuilder failureReason = new StringBuilder();
                    if (!AvailableForBook)
                        failureReason.Append("Room not available ");
                    if (!HotelDetailsVerification)
                        failureReason.Append("Hotel Details Verification failed ");
                    if (!CancellationPolicyAvailable)
                        failureReason.Append("Cancellation Policy not available ");
                    if (!PriceVerification)
                        failureReason.Append("Price Verification failed ");
                    //bool AvailableForConfirmBook = Resp.Descendants("AvailableForConfirmBook").Any() ? Resp.Descendants("AvailableForConfirmBook").FirstOrDefault().Value.ToUpper().Equals("TRUE") : false;
                    if (AvailableForBook && HotelDetailsVerification && CancellationPolicyAvailable && PriceVerification)
                    {
                        StringBuilder TnC = new StringBuilder();
                        foreach (XElement tnc in Resp.Descendants("HotelNorms").FirstOrDefault().Elements("string"))
                        {
                            TnC.Append(tnc.Value + " ");
                        }
                        if (Resp.Descendants("DefaultPolicy").Any())
                            TnC.Append("<br/><br/> <b>Default Policy</b>: " + Resp.Descendants("DefaultPolicy").FirstOrDefault().Value);
                        XElement Room = preBookRooms(Req, Resp);
                        var pb = new XElement("Hotels",
                                    new XElement("Hotel",
                                        new XElement("HotelID"),
                                        new XElement("HotelName"),
                                        new XElement("NewPrice"),
                                        new XElement("Status", "true"),
                                        new XElement("TermCondition", TnC.ToString()),
                                        new XElement("HotelImgSmall"),
                                        new XElement("HotelImgLarge"),
                                        new XElement("MapLink"),
                                        new XElement("DMC", "TBO"),
                                        new XElement("Currency"),
                                        new XElement("Offers"),
                                        new XElement("Rooms", Room)));
                        Response.Add(pb);
                    }
                    else
                        Response.Add(new XElement("ErrorTxt", failureReason.ToString()));
                }
                else
                {
                    if (Resp.Descendants("Description").Any())
                        Response.Add(new XElement("ErrorTxt", Resp.Descendants("Description").FirstOrDefault().Value));
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBooking";
                ex1.PageName = "TBOServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }

            XElement PreBookingResponse = new XElement(soapenv + "Envelope",
                                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                    new XElement(soapenv + "Header",
                                                        new XElement("Authentication",
                                                            new XElement("AgentID", AgentID),
                                                            new XElement("Username", username),
                                                            new XElement("Password", password),
                                                            new XElement("ServiceType", ServiceType),
                                                            new XElement("ServiceVersion", ServiceVersion))),
                                                    new XElement(soapenv + "Body",
                                                        new XElement(Req.Descendants("HotelPreBookingRequest").First()),
                                                       Response));
            if (Response.Descendants("ErrorTxt").Any())
                return PreBookingResponse;
            string oldprice = Req.Descendants("RoomTypes").First().Attribute("TotalRate").Value;
            string newprice = Response.Descendants("RoomTypes").First().Attribute("TotalRate").Value;
            #region Price Change Condition
            if (oldprice.Equals(newprice))
                return PreBookingResponse;
            else
            {
                PreBookingResponse.Descendants("NewPrice").Remove();
                PreBookingResponse.Descendants("HotelPreBookingResponse").Descendants("Hotels").First().AddBeforeSelf(
                   new XElement("ErrorTxt", "Amount has been changed"),
                   new XElement("NewPrice", newprice));
                return PreBookingResponse;
            }
            #endregion
        }

        #region Pre-Booking Rooms
        private XElement preBookRooms(XElement Req, XElement Resp)
        {
            XElement Room = new XElement("Rooms");
            bool PriceChange = Convert.ToBoolean(Resp.Descendants("PriceVerification").FirstOrDefault().Attribute("PriceChanged").Value);
            if (PriceChange)
            {
                int index = 1;
                foreach (XElement paxes in Req.Descendants("RoomPax"))
                    paxes.Add(new XElement("id", index++));
                XElement RoomTypes = new XElement("RoomTypes",
                                        new XAttribute("Index", "1"),
                                        new XAttribute("TotalRate", ""));
                double totalrate = 0.00;
                int cnt = 1;
                string reqId = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                foreach (XElement roomTag in Resp.Descendants("HotelRoom"))
                {
                    try
                    {
                        //XElement roomTag = Suplresponse.Descendants("HotelRoom").Where(x => x.Element("RoomIndex").Value.Equals(room.Value)).FirstOrDefault();
                        List<XElement> SuppList = new List<XElement>();
                        foreach (XElement supl in roomTag.Descendants("Supplement"))
                        {
                            SuppList.Add(new XElement("Supplement",
                                            new XAttribute("suppId", supl.Attribute("SuppID").Value),
                                            new XAttribute("suppName", supl.Attribute("SuppName").Value),
                                            new XAttribute("supptType", supl.Attribute("Type").Value),
                                            new XAttribute("suppIsMandatory", supl.Attribute("SuppIsMandatory").Value),
                                            new XAttribute("suppChargeType", supl.Attribute("SuppChargeType").Value),
                                            new XAttribute("suppPrice", supl.Attribute("Price").Value),
                                            new XAttribute("suppType", supl.Attribute("Type").Value)));
                        }
                        XElement rm = Req.Descendants("RoomPax").Where(x => x.Element("id").Value.Equals(cnt.ToString())).FirstOrDefault();
                        string meal = string.IsNullOrEmpty(roomTag.Element("Inclusion").Value) ? "Room Only" : roomTag.Element("Inclusion").Value;
                        string promTxt = roomTag.Descendants("RoomPromtion").FirstOrDefault().Value;
                        RoomTypes.Add(new XElement("Room",
                                        new XAttribute("ID", roomTag.Element("RoomTypeCode").Value),
                                        new XAttribute("SuppliersID", _supplierID.ToString()),
                                        new XAttribute("RoomSeq", Convert.ToString(cnt++)),
                                        new XAttribute("SessionID", reqId),
                                        new XAttribute("RoomType", roomTag.Element("RoomTypeName").Value),
                                        new XAttribute("OccupancyID", roomTag.Element("RoomIndex").Value),
                                        new XAttribute("OccupancyName", ""),
                                        new XAttribute("MealPlanID", ""),
                                        new XAttribute("MealPlanName", meal),
                                        new XAttribute("MealPlanCode", ""),//MealPlanCode(room.Descendants("Meal").First().Attribute("Name").Value)),
                                        new XAttribute("MealPlanPrice", ""),
                                        new XAttribute("PerNightRoomRate", Convert.ToString(Convert.ToDouble(roomTag.Descendants("RoomRate").FirstOrDefault().Attribute("TotalFare").Value) / roomTag.Descendants("DayRate").Count())),
                                        new XAttribute("TotalRoomRate", roomTag.Descendants("RoomRate").FirstOrDefault().Attribute("TotalFare").Value),
                                        new XAttribute("CancellationDate", ""),
                                        new XAttribute("CancellationAmount", ""),
                                        new XAttribute("isAvailable", "true"),
                                        new XElement("RequestID", reqId),
                                        new XElement("Offers"),
                                        new XElement("PromotionList",
                                        new XElement("Promotions", promTxt)),
                                        new XElement("CancellationPolicy"),
                                        new XElement("Amenities",
                                            new XElement("Amenity")),
                                        new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                        new XElement("Supplements", SuppList),
                                        roomTag.Descendants("DayRates").FirstOrDefault().HasElements ? pb(roomTag.Descendants("DayRates").FirstOrDefault(), Convert.ToDouble(roomTag.Element("RoomRate").Attribute("RoomTax").Value)) : null,
                                        new XElement("AdultNum", rm.Element("Adult").Value),
                                        new XElement("ChildNum", rm.Element("Child").Value)));
                        totalrate += Convert.ToDouble(roomTag.Descendants("RoomRate").FirstOrDefault().Attribute("TotalFare").Value);
                    }
                    catch { }
                }
                RoomTypes.Attribute("TotalRate").SetValue(totalrate.ToString());
                if (Convert.ToBoolean(Resp.Descendants("CancellationPoliciesAvailable").FirstOrDefault().Value))
                    RoomTypes.Descendants("Room").LastOrDefault().AddAfterSelf(policyTagsNew(Resp, Req));
                else
                    RoomTypes.Descendants("Room").LastOrDefault().AddAfterSelf(PolicyDB(Req));
                return RoomTypes;

            }
            else
            {
                List<XElement> GetLog = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 2, 0);
                XElement RoomTypeDB = GetLog.Descendants("RoomTypes").Where(x => x.Element("Room").Attribute("SuppliersID").Value.Equals("21")
                                    && x.Attribute("HtlCode").Value.Equals(Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value)
                                    && x.Attribute("Index").Value.Equals(Req.Descendants("RoomTypes").FirstOrDefault().Attribute("Index").Value)).FirstOrDefault();
                XElement RoomType = new XElement("RoomTypes",
                                        new XAttribute("Index", RoomTypeDB.Attribute("Index").Value),
                                        new XAttribute("TotalRate", RoomTypeDB.Attribute("TotalRate").Value));
                RoomType.Add(RoomTypeDB.Descendants("Room"));
                if (Convert.ToBoolean(Resp.Descendants("CancellationPoliciesAvailable").FirstOrDefault().Value))
                    RoomType.Descendants("Room").LastOrDefault().AddAfterSelf(policyTagsNew(Resp, Req));//policyTags
                else
                    RoomType.Descendants("Room").LastOrDefault().AddAfterSelf(PolicyDB(Req));
                return RoomType;
            }
        }
        #endregion
        #endregion

        #region Booking
        public XElement Booking(XElement Req)
        {
            XElement resp = null;
            int Voucher = Convert.ToInt16(Req.Descendants("IsVoucher").FirstOrDefault().Value);
            if (Voucher < 3)
            {
                bool IsVoucher = Voucher == 1 ? true : false;
                resp = ConfirmBooking(Req, IsVoucher);
            }
            else
            {
                resp = VoucherBooking(Req);
            }
            return resp;
        }
        #endregion

        #region Invoice
        public XElement ConfirmBooking(XElement Req, bool IsVoucher)
        {
            XElement Response = new XElement("HotelBookingResponse");
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                #region Supplier Credential
                XElement SuplCreds = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, Convert.ToString(_supplierID));
                string url = SuplCreds.Element("URL").Value;
                string Action = SuplCreds.Element("HotelBook").Value;
                string client = SuplCreds.Element("Client").Value;
                string UserName = SuplCreds.Element("UserName").Value;
                string PWord = SuplCreds.Element("Password").Value;
                #endregion
                //string timeStamp = DateTime.UtcNow.ToString("ddMMyyHHssfff") + "#" + client;
                string timeStamp = DateTime.UtcNow.ToString("ddMMyyHHmmssfff") + "#" + client;
                string[] sessionCode = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value.Split(new char[] { '_' });
                XDocument BookReq = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                        new XElement(soap + "Envelope",
                                            new XAttribute(XNamespace.Xmlns + "soap", soap),
                                            new XAttribute(XNamespace.Xmlns + "hot", hot),
                                            new XElement(soap + "Header",
                                                new XAttribute(XNamespace.Xmlns + "wsa", wsa),
                                                new XElement(hot + "Credentials",
                                                    new XAttribute("UserName", UserName),
                                                    new XAttribute("Password", PWord)),
                                                new XElement(wsa + "Action", Action),
                                                new XElement(wsa + "To", url)),
                                            new XElement(soap + "Body",
                                                new XElement(hot + "HotelBookRequest",
                                                    new XElement(hot + "ClientReferenceNumber", timeStamp),
                                                    new XElement(hot + "GuestNationality", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                                    bookingGuests(Req.Descendants("PassengersDetail").FirstOrDefault()),
                                                    new XElement(hot + "PaymentInfo",
                                                        new XAttribute("VoucherBooking", IsVoucher),
                                                        new XAttribute("PaymentModeType", "Limit")),
                                                    new XElement(hot + "SessionId", sessionCode[0]),
                                                    new XElement(hot + "NoOfRooms", Req.Descendants("Room").Count()),
                                                    new XElement(hot + "ResultIndex", sessionCode[1]),
                                                    new XElement(hot + "HotelCode", Req.Descendants("HotelID").FirstOrDefault().Value),
                                                    new XElement(hot + "HotelName", Req.Descendants("HotelName").FirstOrDefault().Value),
                                                    bookReqRooms(Req.Descendants("PassengersDetail").FirstOrDefault(), Req.Descendants("CurrencyCode").FirstOrDefault().Value)))));
                try
                {
                    APILogDetail logreq = new APILogDetail();
                    logreq.customerID = Convert.ToInt64(Req.Descendants("CustomerID").Single().Value);
                    logreq.TrackNumber = Req.Descendants("TransactionID").Single().Value;
                    logreq.LogTypeID = 5;
                    logreq.LogType = "Book";
                    logreq.SupplierID = 21;
                    logreq.logrequestXML = BookReq.ToString();
                    logreq.logresponseXML = "";
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SaveAPILogs(logreq);
                }
                catch { }
                Log_Model model = new Log_Model
                {
                    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                    Logtype = "Book",
                    LogtypeID = 5,
                    Supl_Id = _supplierID,
                    TrackNo = Req.Descendants("TransactionID").FirstOrDefault().Value
                };
                //XDocument resp = treq.Request(BookReq, url, Action, model);
                XDocument resp = treq.Httppostbookrequest(BookReq, url, Action, Req.Descendants("CustomerID").FirstOrDefault().Value, Req.Descendants("TransactionID").FirstOrDefault().Value);
                XElement SuplResponse = removeAllNamespaces(resp.Root);

                if (SuplResponse.Descendants("StatusCode").Any() && SuplResponse.Descendants("StatusCode").FirstOrDefault().Value.Equals("01"))
                {

                    BookingId = SuplResponse.Descendants("BookingId").FirstOrDefault().Value;
                    bool priceChange = Convert.ToBoolean(SuplResponse.Descendants("PriceChange").FirstOrDefault().Attribute("Status").Value);
                    if (!priceChange)
                    {
                        string status = SuplResponse.Descendants("BookingStatus").FirstOrDefault().Value;
                        bool success = false;
                        if (status.Equals("Confirmed") || status.Equals("Vouchered"))
                            success = true;
                        Response.Add(new XElement("Hotels",
                                             new XElement("Hotel",
                                                 new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                                 new XElement("HotelName", Req.Descendants("HotelName").FirstOrDefault().Value),
                                                 new XElement("FromDate", Req.Descendants("FromDate").FirstOrDefault().Value),
                                                 new XElement("ToDate", Req.Descendants("ToDate").FirstOrDefault().Value),
                                                 new XElement("AdultPax"),
                                                 new XElement("ChildPax"),
                                                 new XElement("TotalPrice", Req.Descendants("TotalAmount").FirstOrDefault().Value),
                                                 new XElement("CurrencyID"),
                                                 new XElement("CurrencyCode", Req.Descendants("CurrencyCode").FirstOrDefault().Value),
                                                 new XElement("MarketID"),
                                                 new XElement("MarketName"),
                                                 new XElement("HotelImgSmall"),
                                                 new XElement("HotelImgLarge"),
                                                 new XElement("MapLink"),
                                                 new XElement("VoucherRemark"),
                                                 new XElement("TransID", Req.Descendants("TransactionID").FirstOrDefault().Value),
                                                 new XElement("ConfirmationNumber", SuplResponse.Descendants("ConfirmationNo").FirstOrDefault().Value),
                                                 new XElement("Status", success ? "Success" : "Failed"),
                                                 new XElement("PassengerDetail", bookRespRooms(Req.Descendants("PassengersDetail").FirstOrDefault())))));
                    }
                    else
                        Response.Add(new XElement("ErrorTxt", "Price has been changed"));
                }
                else
                {
                    string confirmationNumber = bookingDetail(timeStamp, model);
                    if (!string.IsNullOrEmpty(confirmationNumber))
                        Response.Add(new XElement("Hotels",
                                             new XElement("Hotel",
                                                 new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                                 new XElement("HotelName", Req.Descendants("HotelName").FirstOrDefault().Value),
                                                 new XElement("FromDate", Req.Descendants("FromDate").FirstOrDefault().Value),
                                                 new XElement("ToDate", Req.Descendants("ToDate").FirstOrDefault().Value),
                                                 new XElement("AdultPax"),
                                                 new XElement("ChildPax"),
                                                 new XElement("TotalPrice", Req.Descendants("TotalAmount").FirstOrDefault().Value),
                                                 new XElement("CurrencyID"),
                                                 new XElement("CurrencyCode", Req.Descendants("CurrencyCode").FirstOrDefault().Value),
                                                 new XElement("MarketID"),
                                                 new XElement("MarketName"),
                                                 new XElement("HotelImgSmall"),
                                                 new XElement("HotelImgLarge"),
                                                 new XElement("MapLink"),
                                                 new XElement("VoucherRemark"),
                                                 new XElement("TransID", Req.Descendants("TransactionID").FirstOrDefault().Value),
                                                 new XElement("ConfirmationNumber", confirmationNumber),
                                                 new XElement("Status", "Success"),
                                                 new XElement("PassengerDetail", bookRespRooms(Req.Descendants("PassengersDetail").FirstOrDefault())))));

                    else if (SuplResponse.Descendants("Data").Any())
                        Response.Add(new XElement("ErrorTxt", SuplResponse.Descendants("Data").FirstOrDefault().Value));

                    else if (SuplResponse.Descendants("Description").Any())
                        Response.Add(new XElement("ErrorTxt", SuplResponse.Descendants("Description").FirstOrDefault().Value));

                    else
                        Response.Add(new XElement("ErrorTxt", "Booking Failed. Please check logs for more details"));

                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "Booking";
                ex1.PageName = "TBOServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransactionID").FirstOrDefault().Value;
                //APILog.SendCustomExcepToDB(ex1);
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            XElement BookResponse = new XElement(soapenv + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                        new XElement(soapenv + "Header",
                                            new XElement("Authentication",
                                                new XElement("AgentID", AgentID),
                                                new XElement("Username", username),
                                                new XElement("Password", password),
                                                new XElement("ServiceType", ServiceType),
                                                new XElement("ServiceVersion", ServiceVersion))),
                                        new XElement(soapenv + "Body",
                                            Req.Descendants("HotelBookingRequest").FirstOrDefault(), Response));
            return BookResponse;

        }

        public XElement VoucherBooking(XElement Req)
        {

            XElement tboResp;
            XElement SearReq = Req.Descendants("HotelBookingRequest").FirstOrDefault();
            try
            {
                XElement SuplCreds = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, Convert.ToString(_supplierID));
                string url = SuplCreds.Element("URL").Value;
                string Action = SuplCreds.Element("GenerateInvoice").Value;
                string client = SuplCreds.Element("Client").Value;
                string UserName = SuplCreds.Element("UserName").Value;
                string PWord = SuplCreds.Element("Password").Value;
                XDocument tboReq = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                        new XElement(soap + "Envelope",
                                            new XAttribute(XNamespace.Xmlns + "soap", soap),
                                            new XAttribute(XNamespace.Xmlns + "hot", hot),
                                            new XElement(soap + "Header",
                                                new XAttribute(XNamespace.Xmlns + "wsa", wsa),
                                                new XElement(hot + "Credentials",
                                                    new XAttribute("UserName", UserName),
                                                    new XAttribute("Password", PWord)),
                                                new XElement(wsa + "Action", Action),
                                                new XElement(wsa + "To", url)),
                                            new XElement(soap + "Body",
                                                new XElement(hot + "GenerateInvoiceRequest",
                                                    new XElement(hot + "ConfirmationNo", Req.Descendants("ConfirmationNo").FirstOrDefault().Value),
                                                    new XElement(hot + "BookingId", Req.Descendants("BookingId").FirstOrDefault().Value),
                                                    new XElement(hot + "PaymentInfo",
                                                        new XAttribute("VoucherBooking", true),
                                                        new XAttribute("PaymentModeType", "Limit"))))));
                Log_Model model = new Log_Model
                {
                    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                    Logtype = "Voucher",
                    LogtypeID = 5,
                    Supl_Id = _supplierID,
                    TrackNo = Req.Descendants("TransID").FirstOrDefault().Value
                };
                //XDocument resp = treq.Request(tboReq, url, Action, model);
                XDocument resp = treq.Httppostbookrequest(tboReq, url, Action, Req.Descendants("CustomerID").FirstOrDefault().Value, Req.Descendants("TransactionID").FirstOrDefault().Value);
                XElement SuplResp = removeAllNamespaces(resp.Root);
                if (SuplResp.Descendants("StatusCode").FirstOrDefault().Value.Equals("01"))
                {
                    tboResp = new XElement("HotelBookingResponse",
                           new XElement("Status", "Success"),
                        new XElement("InvoiceNo", SuplResp.Descendants("InvoiceNo").FirstOrDefault().Value));
                }
                else
                {
                    tboResp = new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Booking cann't be Vouchered"));
                }
            }
            catch (Exception ex)
            {
                tboResp = new XElement("HotelBookingResponse", new XElement("ErrorTxt", ex.Message));
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "VoucherBooking";
                custEx.PageName = "TBOServices";
                custEx.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                custEx.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);
            }
            SearReq.AddAfterSelf(tboResp);
            return Req;

        }


        public XElement VoucherBooking1(XElement Req)
        {
            XElement tboResp;
            XElement SearReq = Req.Descendants("VoucherRequest").FirstOrDefault();
            try
            {
                XElement SuplCreds = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, Convert.ToString(_supplierID));
                string url = SuplCreds.Element("URL").Value;
                string Action = SuplCreds.Element("GenerateInvoice").Value;
                string client = SuplCreds.Element("Client").Value;
                string UserName = SuplCreds.Element("UserName").Value;
                string PWord = SuplCreds.Element("Password").Value;
                XDocument tboReq = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                        new XElement(soap + "Envelope",
                                            new XAttribute(XNamespace.Xmlns + "soap", soap),
                                            new XAttribute(XNamespace.Xmlns + "hot", hot),
                                            new XElement(soap + "Header",
                                                new XAttribute(XNamespace.Xmlns + "wsa", wsa),
                                                new XElement(hot + "Credentials",
                                                    new XAttribute("UserName", UserName),
                                                    new XAttribute("Password", PWord)),
                                                new XElement(wsa + "Action", Action),
                                                new XElement(wsa + "To", url)),
                                            new XElement(soap + "Body",
                                                new XElement(hot + "GenerateInvoiceRequest",
                                                    new XElement(hot + "ConfirmationNo", Req.Descendants("ConfirmationNo").FirstOrDefault().Value),
                                                    new XElement(hot + "BookingId", Req.Descendants("BookingId").FirstOrDefault().Value),
                                                    new XElement(hot + "PaymentInfo",
                                                        new XAttribute("VoucherBooking", true),
                                                        new XAttribute("PaymentModeType", "Limit"))))));
                Log_Model model = new Log_Model
                {
                    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                    Logtype = "Voucher",
                    LogtypeID = 21,
                    Supl_Id = _supplierID,
                    TrackNo = Req.Descendants("TransID").FirstOrDefault().Value
                };
                XDocument resp = treq.Request(tboReq, url, Action, model);
                XElement SuplResp = removeAllNamespaces(resp.Root);
                if (SuplResp.Descendants("StatusCode").FirstOrDefault().Value.Equals("01"))
                {
                    tboResp = new XElement("VoucherResponse",
                           new XElement("Status", "Success"),
                        new XElement("InvoiceNo", SuplResp.Descendants("InvoiceNo").FirstOrDefault().Value));
                }
                else
                {
                    tboResp = new XElement("VoucherResponse", new XElement("ErrorTxt", "Booking cann't be Vouchered"));
                }
            }
            catch (Exception ex)
            {
                tboResp = new XElement("VoucherResponse", new XElement("ErrorTxt", ex.Message));
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "VoucherBooking";
                custEx.PageName = "TBOServices";
                custEx.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                custEx.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);
            }
            SearReq.AddAfterSelf(tboResp);
            return Req;

        }
        #endregion

        #region Cancel Booking
        public XElement Cancel(XElement Req)
        {
            XElement cancelResponse = new XElement("HotelCancellationResponse");
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                #region Supplier Credential
                XElement SuplCreds = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, Convert.ToString(_supplierID));
                string url = SuplCreds.Element("URL").Value;
                string Action = SuplCreds.Element("HotelCancel").Value;
                string UserName = SuplCreds.Element("UserName").Value;
                string PWord = SuplCreds.Element("Password").Value;
                #endregion
                XDocument cancelBooking = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                              new XElement(soap + "Envelope",
                                                  new XAttribute(XNamespace.Xmlns + "soap", soap),
                                                  new XAttribute(XNamespace.Xmlns + "hot", hot),
                                                  new XElement(soap + "Header",
                                                      new XAttribute(XNamespace.Xmlns + "wsa", wsa),
                                                      new XElement(hot + "Credentials",
                                                          new XAttribute("UserName", UserName),
                                                          new XAttribute("Password", PWord)),
                                                      new XElement(wsa + "Action", Action),
                                                      new XElement(wsa + "To", url)),
                                                  new XElement(soap + "Body",
                                                      new XElement(hot + "HotelCancelRequest",
                                                          new XElement(hot + "ConfirmationNo", Req.Descendants("ConfirmationNumber").FirstOrDefault().Value),
                                                          new XElement(hot + "RequestType", "HotelCancel"),
                                                          new XElement(hot + "Remarks", "Test Booking")))));
                Log_Model model = new Log_Model
                {
                    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                    Logtype = "Cancel",
                    LogtypeID = 6,
                    Supl_Id = _supplierID,
                    TrackNo = Req.Descendants("TransID").FirstOrDefault().Value
                };
                XDocument resp = treq.Request(cancelBooking, url, Action, model);
                XElement SuplResp = removeAllNamespaces(resp.Root);
                if (SuplResp.Descendants("StatusCode").FirstOrDefault().Value.Equals("01") && SuplResp.Descendants("RequestStatus").FirstOrDefault().Value.Equals("Processed"))
                {
                    cancelResponse.Add(new XElement("Rooms",
                                            new XElement("Room",
                                                new XElement("Cancellation",
                                                    new XElement("Amount", SuplResp.Descendants("CancellationCharge").FirstOrDefault().Value),
                                                    new XElement("Status", "Success")))));
                }
                else if (SuplResp.Descendants("Description").Any())
                {
                    List<string> desc = SuplResp.Descendants("Description").FirstOrDefault().Value.Split(new char[] { ' ' }).ToList();
                    if (desc.Contains("ProcessingErr:") && desc.Contains("24"))
                    {
                        cancelResponse.Add(new XElement("Rooms",
                                            new XElement("Room",
                                                new XElement("Cancellation",
                                                    new XElement("Amount", "00"),
                                                    new XElement("Status", "Success")))));
                    }
                    else
                        cancelResponse.Add(new XElement("Rooms", new XElement("Room",
                                        new XElement("Cancellation", new XElement("ErrorTxt", resp.Descendants("Description").FirstOrDefault().Value)))));
                }
                else
                {
                    cancelResponse.Add(new XElement("Rooms", new XElement("Room",
                                        new XElement("Cancellation", new XElement("ErrorTxt", "Cancellation Failed. Please contact admin")))));
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "Cancel";
                ex1.PageName = "TBOServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            XElement Cancellation = new XElement(soapenv + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                        new XElement(soapenv + "Header",
                                            new XElement("Authentication",
                                                new XElement("AgentID", AgentID),
                                                new XElement("Username", username),
                                                new XElement("Password", password),
                                                new XElement("ServiceType", ServiceType),
                                                new XElement("ServiceVersion", ServiceVersion))),
                                        new XElement(soapenv + "Body",
                                            Req.Descendants("HotelCancellationRequest").FirstOrDefault(), cancelResponse));
            return Cancellation;
        }
        #endregion
        #region Booking Detail
        public string bookingDetail(string ConfNo, Log_Model model)
        {
            #region Supplier Credential
            XElement SuplCreds = supplier_Cred.getsupplier_credentials(Convert.ToString(model.CustomerID), Convert.ToString(_supplierID));
            string url = SuplCreds.Element("URL").Value;
            string Action = SuplCreds.Element("HotelBookingDetail").Value;
            string UserName = SuplCreds.Element("UserName").Value;
            string PWord = SuplCreds.Element("Password").Value;
            #endregion
            XDocument bookDetailReq = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                          new XElement(soap + "Envelope",
                                              new XAttribute(XNamespace.Xmlns + "soap", soap),
                                              new XAttribute(XNamespace.Xmlns + "hot", hot),
                                              new XElement(soap + "Header",
                                                  new XAttribute(XNamespace.Xmlns + "wsa", wsa),
                                                  new XElement(hot + "Credentials",
                                                      new XAttribute("UserName", UserName),
                                                      new XAttribute("Password", PWord)),
                                                  new XElement(wsa + "Action", Action),
                                                  new XElement(wsa + "To", url)),
                                             new XElement(soap + "Body",
                                                 new XElement(hot + "HotelBookingDetailRequest",
                                                     new XElement(hot + "ClientReferenceNumber", ConfNo)))));
            model.Logtype = "Booking_Detail";
            XDocument resp = treq.Request(bookDetailReq, url, Action, model);
            XElement SuplResp = removeAllNamespaces(resp.Root);
            if (SuplResp.Descendants("BookingDetail").Any() && SuplResp.Descendants("BookingDetail").FirstOrDefault().Attributes("ConfirmationNo").Any())
                return SuplResp.Descendants("BookingDetail").FirstOrDefault().Attribute("ConfirmationNo").Value;
            else
                return string.Empty;
        }
        #endregion

        #region Common Methods

        #region Search Rooms
        public List<XElement> roomGuests(XElement RoomPax)
        {
            List<XElement> rooms = new List<XElement>();
            foreach (XElement room in RoomPax.Descendants("RoomPax"))
            {
                XElement Room = new XElement(hot + "RoomGuest",
                                    new XAttribute("AdultCount", room.Element("Adult").Value),
                                    new XAttribute("ChildCount", room.Element("Child").Value));
                foreach (XElement childAge in room.Descendants("ChildAge"))
                    Room.Add(new XElement(hot + "ChildAge",
                                    new XElement(hot + "int", childAge.Value)));
                rooms.Add(Room);
            }
            return rooms;
        }
        #endregion
        #region Star Rating
        public string StarRating(string MinStar, string MaxStar)
        {
            int min = Convert.ToInt32(MinStar), max = Convert.ToInt32(MaxStar);
            System.Collections.Hashtable orLess = new System.Collections.Hashtable();
            orLess.Add(1, "OneStarOrLess");
            orLess.Add(2, "TwoStarOrLess");
            orLess.Add(3, "ThreeStarOrLess");
            orLess.Add(4, "FourStarOrLess");
            orLess.Add(5, "FiveStarOrLess");
            System.Collections.Hashtable orMore = new System.Collections.Hashtable();
            orMore.Add(1, "OneStarOrMore");
            orMore.Add(2, "TwoStarOrMore");
            orMore.Add(3, "ThreeStarOrMore");
            orMore.Add(4, "FourStarOrMore");
            orMore.Add(5, "FiveStarOrMore");
            string rating = string.Empty;
            if (min == 0 && max == 5)
                rating = "All";
            else if (min == 0)
                rating = orLess[max].ToString();
            else if (max == 5)
                rating = orMore[min].ToString();
            else
                rating = string.Empty;
            return rating;
        }

        private bool StarRating(string MinStar, string MaxStar, string hotelRating)
        {
            int min = Convert.ToInt32(MinStar), max = Convert.ToInt32(MaxStar), rating = Convert.ToInt32(hotelRating);
            if (min <= rating && max >= rating)
                return true;
            else
                return false;
        }
        private string getRating(string rating)
        {
            switch (rating)
            {
                case "All":
                    return "0";
                case "FiveStar":
                    return "5";
                case "FourStar":
                    return "4";
                case "ThreeStar":
                    return "3";
                case "TwoStar":
                    return "2";
                case "OneStar":
                    return "1";
                default:
                    return "0";
            }
        }
        #endregion
        #region Room Grouping
        private XElement groupedRooms(XElement Suplresponse, XElement Req)
        {
            XElement Rooms = new XElement("Rooms");
            int cnt = 1, index = 1;
            foreach (XElement rm in Req.Descendants("RoomPax"))
                rm.Add(new XElement("id", cnt++));

            foreach (XElement roomGroup in Suplresponse.Descendants("RoomCombination"))
            {
                XElement RoomTypes = new XElement("RoomTypes",
                                        new XAttribute("Index", index++),
                                        new XAttribute("TotalRate", ""),
                                        new XAttribute("HtlCode", hotelcode),
                                        new XAttribute("CrncyCode", currencyCode),
                                        new XAttribute("DMCType", DMC),
                                        new XAttribute("CUID", customerid));
                double totalrate = 0.00;
                cnt = 1;
                string reqId = Req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "21").FirstOrDefault().Attribute("GRequestID").Value + "_" + Suplresponse.Descendants("FixedFormat").FirstOrDefault().Value;
                foreach (XElement room in roomGroup.Descendants("RoomIndex"))
                {
                    try
                    {
                        XElement roomTag = Suplresponse.Descendants("HotelRoom").Where(x => x.Element("RoomIndex").Value.Equals(room.Value)).FirstOrDefault();
                        List<XElement> SuppList = new List<XElement>();
                        foreach (XElement supl in roomTag.Descendants("Supplement"))
                        {
                            SuppList.Add(new XElement("Supplement",
                                            new XAttribute("suppId", supl.Attribute("SuppID").Value),
                                            new XAttribute("suppName", supl.Attribute("SuppName").Value),
                                            new XAttribute("supptType", supl.Attribute("Type").Value),
                                            new XAttribute("suppIsMandatory", supl.Attribute("SuppIsMandatory").Value),
                                            new XAttribute("suppChargeType", supl.Attribute("SuppChargeType").Value),
                                            new XAttribute("suppPrice", supl.Attribute("Price").Value),
                                            new XAttribute("suppType", supl.Attribute("Type").Value)));
                        }
                        XElement rm = Req.Descendants("RoomPax").Where(x => x.Element("id").Value.Equals(cnt.ToString())).FirstOrDefault();
                        string meal = string.IsNullOrEmpty(roomTag.Element("Inclusion").Value) ? "Room Only" : roomTag.Element("Inclusion").Value;
                        string promTxt = (string)roomTag.Element("RoomPromtion") != null ? roomTag.Descendants("RoomPromtion").FirstOrDefault().Value : string.Empty;
                        RoomTypes.Add(new XElement("Room",
                                        new XAttribute("ID", roomTag.Element("RoomTypeCode").Value + "#ingenium" + roomTag.Element("RatePlanCode").Value),
                                        new XAttribute("SuppliersID", _supplierID.ToString()),
                                        new XAttribute("RoomSeq", Convert.ToString(cnt++)),
                                        new XAttribute("SessionID", reqId),
                                        new XAttribute("RoomType", roomTag.Element("RoomTypeName").Value),
                                        new XAttribute("OccupancyID", room.Value),
                                        new XAttribute("OccupancyName", ""),
                                        new XAttribute("MealPlanID", ""),
                                        new XAttribute("MealPlanName", meal),
                                        new XAttribute("MealPlanCode", ""),//MealPlanCode(room.Descendants("Meal").First().Attribute("Name").Value)),
                                        new XAttribute("MealPlanPrice", ""),
                                        new XAttribute("PerNightRoomRate", Convert.ToString(Convert.ToDouble(roomTag.Descendants("RoomRate").FirstOrDefault().Attribute("TotalFare").Value) / roomTag.Descendants("DayRate").Count())),
                                        new XAttribute("TotalRoomRate", roomTag.Descendants("RoomRate").FirstOrDefault().Attribute("TotalFare").Value),
                                        new XAttribute("CancellationDate", ""),
                                        new XAttribute("CancellationAmount", ""),
                                        new XAttribute("isAvailable", "true"),
                                        new XElement("RequestID", reqId),
                                        new XElement("Offers"),
                                        new XElement("PromotionList",
                                        new XElement("Promotions", promTxt)),
                                        new XElement("CancellationPolicy"),
                                        new XElement("Amenities",
                                            new XElement("Amenity")),
                                        new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                        new XElement("Supplements", SuppList),
                                        roomTag.Descendants("DayRates").FirstOrDefault().HasElements ? pb(roomTag.Descendants("DayRates").FirstOrDefault(), Convert.ToDouble(roomTag.Element("RoomRate").Attribute("RoomTax").Value)) : null,
                                        new XElement("AdultNum", rm.Element("Adult").Value),
                                        new XElement("ChildNum", rm.Element("Child").Value)));
                        totalrate += Convert.ToDouble(roomTag.Descendants("RoomRate").FirstOrDefault().Attribute("TotalFare").Value);
                    }
                    catch { }
                }
                RoomTypes.Attribute("TotalRate").SetValue(totalrate.ToString());
                Rooms.Add(RoomTypes);
            }

            return Rooms;
        }
        #region Price Breakup
        private XElement pb(XElement DailyRates, double roomTax)
        {
            int nights = DailyRates.Descendants("DayRate").Count();
            double taxAdjustment = roomTax / nights;
            int index = 1;
            XElement response = new XElement("PriceBreakups");
            foreach (XElement price in DailyRates.Descendants("DayRate"))
            {
                double amount = Convert.ToDouble(price.Attribute("BaseFare").Value) + taxAdjustment;
                response.Add(new XElement("Price", new XAttribute("Night", index++),
                                new XAttribute("PriceValue", amount.ToString())));
            }
            return response;
        }

        #endregion
        #endregion
        #region Merge Cancellation Policy
        public XElement MergCxlPolicy(List<XElement> rooms)
        {
            List<XElement> cxlList = new List<XElement>();

            IEnumerable<XElement> dateLst = rooms.Descendants("CancellationPolicy").
               GroupBy(r => new { r.Attribute("LastCancellationDate").Value, noshow = r.Attribute("NoShowPolicy").Value }).Select(y => y.FirstOrDefault()).
               OrderBy(p => DateTime.ParseExact(p.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture));
            if (dateLst.Count() > 0)
            {
                int counter = 1;
                string price = string.Empty;
                foreach (var item in dateLst)
                {
                    int policynumberdatewise = 1;
                    string date = item.Attribute("LastCancellationDate").Value;
                    string noShow = item.Attribute("NoShowPolicy").Value;
                    decimal datePrice = 0.0m;
                    foreach (var rm in rooms.Descendants("CancellationPolicy").OrderBy(x => DateTime.ParseExact(x.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture)))
                    {

                        if (rm.Attribute("NoShowPolicy").Value == noShow && rm.Attribute("LastCancellationDate").Value == date)
                        {

                            if (counter == 1)
                                price = rm.Attribute("ApplicableAmount").Value;
                            else
                                price = (Convert.ToDouble(price) + Convert.ToDouble(rm.Attribute("ApplicableAmount").Value)).ToString();
                            datePrice += Convert.ToDecimal(price);
                            if (policynumberdatewise > 1)
                                price = datePrice.ToString();
                            policynumberdatewise++;
                        }
                        else
                        {
                            //if (noShow == "1")
                            //{
                            //    datePrice += Convert.ToDecimal(rm.Attribute("ApplicableAmount").Value);
                            //}
                            //else
                            //{


                            //    var lastItem = rm.Descendants("CancellationPolicy").
                            //        Where(pq => (pq.Attribute("NoShowPolicy").Value == noShow && Convert.ToDateTime(pq.Attribute("LastCancellationDate").Value) < chnagetoTime(date)));

                            //    if (lastItem.Count() > 0)
                            //    {
                            //        var lastDate = lastItem.Max(y => y.Attribute("LastCancellationDate").Value);
                            //        var lastprice = rm.Descendants("CancellationPolicy").
                            //            Where(pq => (pq.Attribute("NoShowPolicy").Value == noShow && pq.Attribute("LastCancellationDate").Value == lastDate)).
                            //            FirstOrDefault().Attribute("ApplicableAmount").Value;
                            //        datePrice += Convert.ToDecimal(lastprice);
                            //    }

                            //}

                        }
                    }
                    XElement pItem = new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", date), new XAttribute("ApplicableAmount", datePrice), new XAttribute("NoShowPolicy", noShow));
                    cxlList.Add(pItem);
                    counter++;
                }

                cxlList = cxlList.GroupBy(x => new { DateTime.ParseExact(x.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date, x.Attribute("NoShowPolicy").Value }).
                    Select(y => new XElement("CancellationPolicy",
                        new XAttribute("LastCancellationDate", y.Key.Date.ToString("yyyy/MM/dd")),
                        new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("ApplicableAmount").Value))),
                        new XAttribute("NoShowPolicy", y.Key.Value))).OrderBy(p => p.Attribute("LastCancellationDate").Value).ToList();

                var fItem = cxlList.FirstOrDefault();

                if (Convert.ToDecimal(fItem.Attribute("ApplicableAmount").Value) != 0.0m)
                {
                    cxlList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", Convert.ToDateTime(fItem.Attribute("LastCancellationDate").Value).AddDays(-1).Date.ToString("yyyy/MM/dd")), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0")));

                }
            }

            XElement cxlItem = new XElement("CancellationPolicies", cxlList);
            return cxlItem;

        }
        #endregion
        #region Generate Cancellation Policy tags
        public XElement policyTags(XElement SuplResponse, XElement Req)
        {
            XElement Cp = new XElement("CancellationPolicies");
            List<string> RoomIndexes = Req.Descendants("Room").Select(x => x.Attribute("OccupancyID").Value).ToList();
            foreach (XElement policy in SuplResponse.Descendants("CancelPolicy").Where(x => !x.Attribute("CancellationCharge").Value.Equals("0")))
            {
                double Amount = 0.0, roomPrice = 0.0;
                switch (policy.Attribute("ChargeType").Value)
                {
                    case "Fixed":
                        Amount = Convert.ToDouble(policy.Attribute("CancellationCharge").Value);
                        break;
                    case "Percentage":
                        roomPrice = policy.Attributes("RoomIndex").Any() ?
                            Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(policy.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("TotalRoomRate").Value) :
                            Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("TotalRoomRate").Value);
                        Amount = (Convert.ToDouble(policy.Attribute("CancellationCharge").Value) / 100) * roomPrice;
                        break;
                    case "Night":
                        roomPrice = policy.Attributes("RoomIndex").Any() ?
                            Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(policy.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("PerNightRoomRate").Value) :
                            Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("PerNightRoomRate").Value);
                        Amount = roomPrice * Convert.ToInt32(policy.Attribute("CancellationCharge").Value);
                        break;
                }
                DateTime lastDate = DateTime.ParseExact(policy.Attribute("FromDate").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                Cp.Add(new XElement("CancellationPolicy",
                    new XAttribute("LastCancellationDate", lastDate.ToString("dd/MM/yyyy")),
                    new XAttribute("ApplicableAmount", Amount.ToString()),
                    new XAttribute("NoShowPolicy", "0")));
            }
            foreach (XElement noshow in SuplResponse.Descendants("NoShowPolicy"))
            {
                double Amount = 0.0, roomPrice = 0.0;
                switch (noshow.Attribute("ChargeType").Value)
                {
                    case "Fixed":
                        Amount = Convert.ToDouble(noshow.Attribute("CancellationCharge").Value);
                        break;
                    case "Percentage":
                        roomPrice = noshow.Attributes("RoomIndex").Any() ?
                            Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(noshow.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("TotalRoomRate").Value) :
                            Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("TotalRoomRate").Value);
                        Amount = (Convert.ToDouble(noshow.Attribute("CancellationCharge").Value) / 100) * roomPrice;
                        break;
                    case "Night":
                        roomPrice = noshow.Attributes("RoomIndex").Any() ?
                            Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(noshow.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("PerNightRoomRate").Value) :
                            Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("PerNightRoomRate").Value);
                        Amount = roomPrice * Convert.ToInt32(noshow.Attribute("CancellationCharge").Value);
                        break;
                }
                DateTime lastDate = DateTime.ParseExact(noshow.Attribute("FromDate").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                Cp.Add(new XElement("CancellationPolicy",
                    new XAttribute("LastCancellationDate", lastDate.ToString("dd/MM/yyyy")),
                    new XAttribute("ApplicableAmount", Amount.ToString()),
                    new XAttribute("NoShowPolicy", "1")));
            }
            var policyGroups = from policy in Cp.Descendants("CancellationPolicy")
                               group policy by new
                               {
                                   c1 = policy.Attribute("LastCancellationDate").Value,
                                   c2 = policy.Attribute("NoShowPolicy").Value
                               };
            XElement FinalCp = new XElement("CancellationPolicies");
            foreach (var policyGroup in policyGroups)
            {
                double amount = policyGroup.Select(x => Convert.ToDouble(x.Attribute("ApplicableAmount").Value)).Sum();
                FinalCp.Add(new XElement("CancellationPolicy",
                                new XAttribute("LastCancellationDate", policyGroup.Key.c1),
                                new XAttribute("ApplicableAmount", amount.ToString()),
                                new XAttribute("NoShowPolicy", policyGroup.Key.c2)));
            }
            DateTime FirstDate = FinalCp.Descendants("CancellationPolicy").Select(x => DateTime.ParseExact(x.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture))
                .OrderBy(x => x).FirstOrDefault();
            FirstDate = FirstDate.AddDays(-1);
            FinalCp.Add(new XElement("CancellationPolicy",
                            new XAttribute("LastCancellationDate", FirstDate.ToString("dd/MM/yyyy")),
                            new XAttribute("ApplicableAmount", "0"),
                            new XAttribute("NoShowPolicy", "0")));
            return FinalCp;
        }
        public XElement policyTagsNew(XElement SuplResponse, XElement Req)
        {
            List<PolicyHelper> policies = new List<PolicyHelper>();
            XElement RoomWiseCp = new XElement("Policies");
            XElement Cp = new XElement("CancellationPolicies");
            List<string> RoomIndexes = Req.Descendants("Room").Select(x => x.Attribute("OccupancyID").Value).ToList();
            foreach (XElement policy in SuplResponse.Descendants("CancelPolicy").Where(x => !x.Attribute("CancellationCharge").Value.Equals("0")))
            {
                double Amount = 0.0, roomPrice = 0.0;
                switch (policy.Attribute("ChargeType").Value)
                {
                    case "Fixed":
                        Amount = Convert.ToDouble(policy.Attribute("CancellationCharge").Value);
                        break;
                    case "Percentage":
                        roomPrice = policy.Attributes("RoomIndex").Any() ?
                            Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(policy.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("TotalRoomRate").Value) :
                            Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("TotalRoomRate").Value);
                        Amount = (Convert.ToDouble(policy.Attribute("CancellationCharge").Value) / 100) * roomPrice;
                        break;
                    case "Night":
                        roomPrice = policy.Attributes("RoomIndex").Any() ?
                            Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(policy.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("PerNightRoomRate").Value) :
                            Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("PerNightRoomRate").Value);
                        Amount = roomPrice * Convert.ToInt32(policy.Attribute("CancellationCharge").Value);
                        break;
                }
                //RoomWiseCp.Add(new XElement("Policy",
                //                    new XAttribute("Index",policy.Attributes("RoomIndex").Any()?policy.Attribute("RoomIndex").Value:"0"),
                //                    new XElement("startdate",policy.Attribute("FromDate").Value),
                //                    new XElement("enddate", policy.Attribute("ToDate").Value),
                //                    new XElement("noshow","0"),
                //                    new XElement("amount",Amount)));    
                policies.Add(new PolicyHelper
                {
                    Index = Convert.ToInt32(policy.Attributes("RoomIndex").Any() ? policy.Attribute("RoomIndex").Value : "0"),
                    StartDate = DateTime.ParseExact(policy.Attribute("FromDate").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    EndDate = DateTime.ParseExact(policy.Attribute("ToDate").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    NoShow = 0,
                    Amount = Amount
                });
                //DateTime lastDate = DateTime.ParseExact(policy.Attribute("FromDate").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                //Cp.Add(new XElement("CancellationPolicy",
                //    new XAttribute("LastCancellationDate", lastDate.ToString("dd/MM/yyyy")),
                //    new XAttribute("ApplicableAmount", Amount.ToString()),
                //    new XAttribute("NoShowPolicy", "0")));
            }
            foreach (XElement noshow in SuplResponse.Descendants("NoShowPolicy"))
            {
                double Amount = 0.0, roomPrice = 0.0;
                switch (noshow.Attribute("ChargeType").Value)
                {
                    case "Fixed":
                        Amount = Convert.ToDouble(noshow.Attribute("CancellationCharge").Value);
                        break;
                    case "Percentage":
                        roomPrice = noshow.Attributes("RoomIndex").Any() ?
                            Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(noshow.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("TotalRoomRate").Value) :
                            Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("TotalRoomRate").Value);
                        Amount = (Convert.ToDouble(noshow.Attribute("CancellationCharge").Value) / 100) * roomPrice;
                        break;
                    case "Night":
                        roomPrice = noshow.Attributes("RoomIndex").Any() ?
                            Convert.ToDouble(Req.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(noshow.Attribute("RoomIndex").Value)).FirstOrDefault().Attribute("PerNightRoomRate").Value) :
                            Convert.ToDouble(Req.Descendants("Room").FirstOrDefault().Attribute("PerNightRoomRate").Value);
                        Amount = roomPrice * Convert.ToInt32(noshow.Attribute("CancellationCharge").Value);
                        break;
                }
                //DateTime lastDate = DateTime.ParseExact(noshow.Attribute("FromDate").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                //Cp.Add(new XElement("CancellationPolicy",
                //    new XAttribute("LastCancellationDate", lastDate.ToString("dd/MM/yyyy")),
                //    new XAttribute("ApplicableAmount", Amount.ToString()),
                //    new XAttribute("NoShowPolicy", "1")));
                //RoomWiseCp.Add(new XElement("Policy",
                //                    new XAttribute("Index",noshow.Attributes("RoomIndex").Any()?noshow.Attribute("RoomIndex").Value:"0"),
                //                    new XElement("startdate",noshow.Attribute("FromDate").Value),
                //                    new XElement("enddate", noshow.Attribute("ToDate").Value),
                //                    new XElement("noshow","1"),
                //                    new XElement("amount",Amount)));
                policies.Add(new PolicyHelper
                {
                    Index = Convert.ToInt32(noshow.Attributes("RoomIndex").Any() ? noshow.Attribute("RoomIndex").Value : "0"),
                    StartDate = DateTime.ParseExact(noshow.Attribute("FromDate").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    EndDate = DateTime.ParseExact(noshow.Attribute("ToDate").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    NoShow = 1,
                    Amount = Amount
                });
            }
            //foreach(XElement policy in RoomWiseCp.Descendants("Policy").Where(x=>x.Element("noshow").Value.Equals("0")))
            //{
            //    DateTime currentdate = DateTime.ParseExact(policy.Element("startdate").Value,"yyyy-MM-dd",CultureInfo.InvariantCulture);
            //    RoomWiseCp.Descendants("Policy").Where(x=> x.Element)
            //    Cp.Add(new XElement("CancellationPolicy"))
            //}
            foreach (PolicyHelper policy in policies.Where(x => x.Amount > 0))
            {
                double amt = policy.Amount;
                amt += policies.Where(x => x.StartDate < policy.StartDate && x.EndDate > policy.StartDate && !x.Index.Equals(policy.Index)).Select(x => x.Amount).Sum();
                Cp.Add(new XElement("CancellationPolicy",
                            new XAttribute("LastCancellationDate", policy.StartDate.ToString("dd/MM/yyyy")),
                            new XAttribute("ApplicableAmount", amt.ToString()),
                            new XAttribute("NoShowPolicy", policy.NoShow)));
            }
            var policyGroups = from policy in Cp.Descendants("CancellationPolicy")
                               group policy by new
                               {
                                   c1 = policy.Attribute("LastCancellationDate").Value,
                                   c2 = policy.Attribute("NoShowPolicy").Value
                               };
            XElement FinalCp = new XElement("CancellationPolicies");
            foreach (var policyGroup in policyGroups)
            {
                double amount = policyGroup.Select(x => Convert.ToDouble(x.Attribute("ApplicableAmount").Value)).Sum();
                FinalCp.Add(new XElement("CancellationPolicy",
                                new XAttribute("LastCancellationDate", policyGroup.Key.c1),
                                new XAttribute("ApplicableAmount", amount.ToString()),
                                new XAttribute("NoShowPolicy", policyGroup.Key.c2)));
            }
            DateTime FirstDate = FinalCp.Descendants("CancellationPolicy").Select(x => DateTime.ParseExact(x.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture))
                .OrderBy(x => x).FirstOrDefault();
            FirstDate = FirstDate.AddDays(-1);
            FinalCp.Add(new XElement("CancellationPolicy",
                            new XAttribute("LastCancellationDate", FirstDate.ToString("dd/MM/yyyy")),
                            new XAttribute("ApplicableAmount", "0"),
                            new XAttribute("NoShowPolicy", "0")));
            return FinalCp;
        }
        #endregion
        #region Fetch Cancellation Policy from DB
        public XElement PolicyDB(XElement Req)
        {
            List<XElement> Policies = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 3, 0);
            string htlcode = Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value;
            XElement cxp = Policies.Descendants("Response").Where(x => x.Descendants("RoomTypes").FirstOrDefault().Value.Equals(htlcode)
                            && x.Descendants("Room").FirstOrDefault().Attribute("ID").Value.Equals(Req.Descendants("Room").FirstOrDefault().Attribute("ID").Value)).FirstOrDefault()
                            .Descendants("CancellationPolicies").FirstOrDefault();
            return cxp;

        }
        #endregion
        #region Get Xmls From Log
        public List<XElement> LogXMLs(string trackID, int logtypeID, int SupplierID)
        {
            List<XElement> response = new List<XElement>();
            DataTable LogTable = new DataTable();
            LogTable = tbd.GetLog(trackID, logtypeID, SupplierID);
            string checkreq = LogTable.Rows[0]["logrequestXML"].ToString();
            if (checkreq.Equals(null) || checkreq.Equals(string.Empty))
            {
                for (int i = 0; i < LogTable.Rows.Count; i++)
                    response.Add(new XElement(LogTable.Rows[i]["logType"].ToString(),
                                     new XElement("Request", null),
                                     new XElement("Response", XElement.Parse(LogTable.Rows[i]["logresponseXML"].ToString()))));
                return response;
            }
            for (int i = 0; i < LogTable.Rows.Count; i++)
            {
                response.Add(new XElement(LogTable.Rows[i]["logType"].ToString(),
                                           new XElement("Request", XElement.Parse(LogTable.Rows[i]["logrequestXML"].ToString())),
                                           new XElement("Response", XElement.Parse(LogTable.Rows[i]["logresponseXML"].ToString()))));
            }
            return response;
        }
        #endregion
        #region Guests For Booking
        public XElement bookingGuests(XElement Rooms)
        {
            XElement guests = new XElement(hot + "Guests");
            bool leadGuest = false;
            int roomNumber = 1;
            foreach (XElement room in Rooms.Descendants("Room"))
            {
                foreach (XElement guest in room.Descendants("PaxInfo"))
                {
                    string title = guest.Element("Title").Value;
                    if (guest.Element("GuestType").Value.Equals("Child") && guest.Element("Title").Value.Equals("Mrs"))
                        title = "Miss";
                    string firstName = Regex.Replace(guest.Element("FirstName").Value, @"\s+", "");
                    string lastName = Regex.Replace(guest.Element("LastName").Value, @"\s+", "");
                    guests.Add(new XElement(hot + "Guest",
                                    new XAttribute("LeadGuest", leadGuest ? "false" : guest.Element("IsLead").Value),
                                    new XAttribute("GuestType", guest.Element("GuestType").Value),
                                    new XAttribute("GuestInRoom", Convert.ToString(roomNumber)),
                                    new XElement(hot + "Title", title),
                                    new XElement(hot + "FirstName", guest.Element("FirstName").Value),
                                    new XElement(hot + "LastName", guest.Element("LastName").Value),
                                    guest.Element("GuestType").Value.Equals("Adult") ? null : new XElement(hot + "Age", guest.Element("Age").Value)));
                    if (!leadGuest)
                        leadGuest = Convert.ToBoolean(guest.Element("IsLead").Value);
                }
                roomNumber++;
            }
            return guests;
        }
        #endregion
        #region Rooms for Booking request
        public XElement bookReqRooms(XElement Rooms, string CurrencyCode)
        {
            XElement respRooms = new XElement(hot + "HotelRooms");
            foreach (XElement room in Rooms.Descendants("Room"))
            {
                XElement Supplements = null;
                if (room.Descendants("Supplement").Any())
                {
                    List<XElement> supls = new List<XElement>();

                    foreach (XElement supl in room.Descendants("Supplement"))
                    {
                        supls.Add(new XElement(hot + "SuppInfo",
                                        new XAttribute("SuppID", supl.Attribute("suppId").Value),
                                        new XAttribute("SuppChargeType", supl.Attribute("suppChargeType").Value),
                                        new XAttribute("Price", supl.Attribute("suppPrice").Value),
                                        new XAttribute("SuppIsSelected", supl.Attribute("suppIsMandatory").Value)));
                    }
                    Supplements = new XElement(hot + "Supplements", supls);
                }
                //string[] roomCodes = room.FirstAttribute.Value.Split(new[] { '#ingenium' });
                string[] roomCodes = room.FirstAttribute.Value.Split(new[] { "#ingenium" }, StringSplitOptions.None);
                respRooms.Add(new XElement(hot + "HotelRoom",
                                  new XElement(hot + "RoomIndex", room.Attribute("OccupancyID").Value),
                                  new XElement(hot + "RoomTypeName", room.Attribute("RoomType").Value),
                                  new XElement(hot + "RoomTypeCode", roomCodes[0]),
                                  new XElement(hot + "RatePlanCode", roomCodes[1]),
                                  new XElement(hot + "RoomRate",
                                      new XAttribute("RoomFare", room.Attribute("TotalRoomRate").Value),
                                      new XAttribute("Currency", CurrencyCode),
                                      new XAttribute("AgentMarkUp", "0.00"),
                                      new XAttribute("RoomTax", "0.00"),
                                      new XAttribute("TotalFare", room.Attribute("TotalRoomRate").Value)), Supplements));
            }
            return respRooms;
        }
        #endregion
        #region Booking Response Rooms
        public XElement bookRespRooms(XElement Rooms)
        {
            XElement gDetail = new XElement("GuestDetail");
            foreach (XElement room in Rooms.Descendants("Room"))
            {
                gDetail.Add(new XElement("Room",
                                new XAttribute("ID", room.Attribute("RoomTypeID").Value),
                                new XAttribute("RoomType", room.Attribute("RoomType").Value),
                                new XAttribute("ServiceID", ""),
                                new XAttribute("RefNo", BookingId),//new XAttribute("RefNo", room.Attribute("SessionID").Value),
                                new XElement("MealPlanID", ""),
                                new XElement("MealPlanName", ""),
                                new XElement("MealPlanCode", ""),
                                new XElement("MealPlanPrice", ""),
                                new XElement("PerNightRoomRate", ""),
                                new XElement("RoomStatus", "true"),
                                new XElement("TotalRoomRate", room.Attribute("TotalRoomRate").Value),
                                room.Descendants("PaxInfo").Select(x => new XElement("RoomGuest", x.Descendants()))));
            }
            return gDetail;
        }
        #endregion
        #region Tag Info Xml
        public XElement tagInfo(int countryid)
        {
            List<XElement> dataTags = new List<XElement>();
            DataTable tags = tbd.TagInfo(countryid);
            if (tags.Rows.Count > 0)
            {
                dataTags = tags.AsEnumerable().Select(x => new XElement("Tag",
                                                                            new XElement("TagID", x.Field<string>("Id")),
                                                                            new XElement("Location", x.Field<string>("Name")))).ToList();
                return new XElement("Tags", dataTags);
            }
            else
                return new XElement("Tags", "No Tags Found");
        }
        #endregion
        private string ReformatDate(string inputDate)
        {
            DateTime date = DateTime.ParseExact(inputDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            return date.ToString("yyyy-MM-dd");
        }
        public static XElement removeAllNamespaces(XElement e)
        {
            return new XElement(e.Name.LocalName,
              (from n in e.Nodes()
               select ((n is XElement) ? removeAllNamespaces(n as XElement) : n)),
                  (e.HasAttributes) ?
                    (from a in e.Attributes()
                     where (!a.IsNamespaceDeclaration)
                     select new XAttribute(a.Name.LocalName, a.Value)) : null);
        }
        #endregion
    }
}