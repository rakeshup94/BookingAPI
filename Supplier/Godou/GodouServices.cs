using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Godou;
using TravillioXMLOutService.Models.Restel;


namespace TravillioXMLOutService.Supplier.Godou
{
    public class GodouServices
    {
        GodouRequest greq = new GodouRequest();
        GodouDataAccess gda = new GodouDataAccess();
        string supplierID = "31";
        string dmc = string.Empty, SupplierCurrency = string.Empty, hotelcode = string.Empty;
        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";

        #region Hotel Search
        public List<XElement> HotelAvailablitySearch(XElement Req, XElement Currencies, string SupplierType)
        {
            dmc = SupplierType;
            GodouRequest greq = new GodouRequest();
            List<XElement> HotelList = new List<XElement>();
            string cityID = Req.Descendants("CityID").FirstOrDefault().Value;
            try
            {
                List<string> CurrencyList = new List<string>();
                string currencyCode = string.Empty;
                if (Currencies == null)
                    currencyCode = "EUR";
                else
                {
                    foreach (XElement currency in Currencies.Descendants("Code"))
                        CurrencyList.Add(currency.Value);
                    currencyCode = CurrencyList.Contains(Req.Descendants("DesiredCurrencyCode").FirstOrDefault().Value) ? Req.Descendants("DesiredCurrencyCode").FirstOrDefault().Value : Currencies.Descendants("Code").FirstOrDefault().Value;
                }
                //if (!CityMapping.Descendants("d0").Where(x => x.Element("Serial").Value.Equals(cityID)).Any())
                //    return null;
                //XElement cityInfo = CityMapping.Descendants("d0")
                //                    .Where(x => x.Element("Serial").Value.Equals(cityID))
                //                    .FirstOrDefault();
                DataTable cityMapping = gda.GadouCityMapping(cityID, supplierID);
                string DestinationID = cityMapping.Rows[0]["SupCityId"].ToString();
                if (string.IsNullOrEmpty(DestinationID))
                    return null;
                XElement sessionID = GetSessionId(Req.Descendants("CustomerID").FirstOrDefault().Value);                
                if (sessionID.Descendants("Success").FirstOrDefault().Value.ToUpper().Equals("TRUE"))
                {
                    string SID = sessionID.Descendants("SessionID").FirstOrDefault().Value;
                    #region Request
                    dynamic foo = new ExpandoObject();
                    foo.SessionID = SID;
                    foo.DestinationId = Convert.ToInt32(DestinationID);
                    foo.CheckInDate = reformatDate(Req.Descendants("FromDate").FirstOrDefault().Value);
                    foo.CheckOutDate = reformatDate(Req.Descendants("ToDate").FirstOrDefault().Value);
                    foo.CurrencyCode = currencyCode;
                    foo.NationalityCode = Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value;
                    foo.Rooms = SearchRooms(Req);
                    #endregion
                    string json = JsonConvert.SerializeObject(foo);
                    DateTime starttime = DateTime.Now;
                    GadouLogs model = new GadouLogs();
                    model.CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value);
                    model.Logtype = "Search";
                    model.LogtypeID = 1;
                    model.TrackNo = Req.Descendants("TransID").FirstOrDefault().Value;
                    string response = greq.ServiceResponses(json, "Search", "", Req.Descendants("CustomerID").FirstOrDefault().Value);
                    var x = JsonConvert.DeserializeXmlNode(response, "root");
                    XDocument SupplResponse = XDocument.Parse(x.InnerXml);
                    #region Log Save
                    try
                    {
                       
                        #region Format Entries
                        var saveReq = JsonConvert.DeserializeXmlNode(json, "Request");
                        XElement save = XElement.Parse(saveReq.InnerXml);
                        #endregion
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                        log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                        log.SupplierID = 31;
                        log.logrequestXML = save.ToString();
                        log.logresponseXML = SupplResponse.ToString();
                        log.LogType = "Search";
                        log.LogTypeID = 1;
                        log.StartTime = starttime;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelAvailablitySearch";
                        ex1.PageName = "GodouServices";
                        ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                        ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    #endregion
                    string minstar = Req.Descendants("MinStarRating").FirstOrDefault().Value;
                    string maxstar = Req.Descendants("MaxStarRating").FirstOrDefault().Value;
                    int counting = SupplResponse.Descendants("Data").Count();
                    DataTable StaticData = gda.GodouHotelDetails(DestinationID);
                    foreach (XElement hotel in SupplResponse.Descendants("Data").Where(z => z.Descendants("Options").Any()))
                    {

                        if (StaticData.Rows.Count != 0)
                        {
                            var result = StaticData.AsEnumerable().Where(dt => dt.Field<string>("HotelID") == hotel.Descendants("HotelCode").FirstOrDefault().Value);
                            int countre = result.Count();
                            DataRow[] drow = result.ToArray();
                            if (drow.Length > 0)
                            {
                                DataRow dr = drow[0];
                                string imgTN = null;
                                string imgLrg = null;
                                if (dr["Images"].ToString() != "")
                                {
                                    imgLrg = dr["Images"].ToString();
                                    XElement Images = XElement.Parse(imgLrg);
                                    imgTN = Images.Descendants("ThumbUrl").FirstOrDefault().Value;
                                    imgLrg = Images.Descendants("FullSizeUrl").FirstOrDefault().Value;
                                }
                                if (StarRating(minstar, maxstar, dr["StarRating"].ToString()) && !string.IsNullOrEmpty(dr["Address"].ToString()))
                                {
                                    #region Response XML
                                    try
                                    {                                        
                                        HotelList.Add(new XElement("Hotel",
                                                            new XElement("HotelID", dr["HtlUniqueID"]),
                                                            new XElement("HotelName", hotel.Descendants("HotelName").FirstOrDefault().Value),
                                                            new XElement("PropertyTypeName"),
                                                            new XElement("CountryID", Req.Descendants("CountryID").FirstOrDefault().Value),
                                                            new XElement("CountryName", Req.Descendants("CountryName").FirstOrDefault().Value),
                                                            new XElement("CountryCode", Req.Descendants("CountryCode").FirstOrDefault().Value),
                                                            new XElement("CityId"),
                                                            new XElement("CityCode"),
                                                            new XElement("CityName"),
                                                            new XElement("AreaId"),
                                                            new XElement("AreaName"),
                                                            new XElement("RequestID", SID),
                                                            new XElement("Address", dr["Address"].ToString()),
                                                            new XElement("Location"),
                                                            new XElement("Description", dr["Description"]),
                                                            new XElement("StarRating", dr["StarRating"]),
                                                            new XElement("MinRate", minRate(hotel.Descendants("Options").ToList())),
                                                            new XElement("HotelImgSmall", imgTN),
                                                            new XElement("HotelImgLarge", imgLrg),
                                                            new XElement("MapLink"),
                                                            new XElement("Longitude", dr["Longitude"]),
                                                            new XElement("Latitude", dr["Latitude"]),
                                                            new XElement("DMC", dmc),
                                                            new XElement("SupplierID", supplierID),
                                                            new XElement("Currency", hotel.Descendants("Currency").FirstOrDefault().Value),
                                                            new XElement("Offers"),
                                                            new XElement("Facilities", new XElement("Facility", "No Facility Available")),
                                                            new XElement("Rooms")));
                                    }
                                    catch{ }
                                    #endregion
                                }
                            }
                        }
                    }
                }
               
                
            }
            catch (Exception ex)
            {
                
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelAvailablitySearch";
                ex1.PageName = "GodouServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            return HotelList; 
        }

        #endregion
        #region Hotel Details 
        public XElement HotelDetails(XElement Req)
        {
            XElement details = null;
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                DataTable staticdatatable = gda.GodouSingleHotelDetails(Req.Descendants("HotelID").FirstOrDefault().Value);
                DataRow staticdata = staticdatatable.Rows[0];
                string parameter = "hotelCode=" + staticdata["HotelID"];
                string resp = greq.GetServerResponse("", "details", parameter, Req.Descendants("CustomerID").FirstOrDefault().Value);
                var conv = JsonConvert.DeserializeXmlNode(resp, "Root");
                XElement SupplResponse = XElement.Parse(conv.InnerXml);
                List<string> Facilities = new List<string>();
                foreach (XElement facility in SupplResponse.Descendants("Facilities"))
                    Facilities.Add(facility.Value);
                List<XElement> Faci = new List<XElement>();
                foreach (string fac in Facilities)
                    Faci.Add(new XElement("Facility", fac));
                List<XElement> Images = new List<XElement>();
                foreach (XElement image in SupplResponse.Descendants("FullSizeUrl"))
                    Images.Add(new XElement("Image", new XAttribute("Path", image.Value), new XAttribute("Caption", "")));
                #region Response XML
                var hotels = new XElement("Hotels",
                                  new XElement("Hotel",
                                      new XElement("HotelID", staticdata["HotelID"]),
                                      new XElement("Description", SupplResponse.Descendants("Description").Any() ? SupplResponse.Descendants("Description").FirstOrDefault().Value : string.Empty),
                                      new XElement("Images", Images),
                                      new XElement("Facilities", Faci),
                                      new XElement("ContactDetails",
                                          new XElement("Phone", SupplResponse.Descendants("Phone").Any() ? SupplResponse.Descendants("Phone").FirstOrDefault().Value : string.Empty),
                                          new XElement("Fax")),
                                      new XElement("CheckinTime"),
                                      new XElement("CheckoutTime")
                                      ));
                #endregion
                XElement response = new XElement("hoteldescResponse", hotels);
                #region Response Format
                details = new XElement(
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
                                                            new XElement(Req.Descendants("hoteldescRequest").FirstOrDefault()),
                                                           response)));
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelDetails";
                ex1.PageName = "GodouServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            return details;
        }
        #endregion
        #region Room Availability
        public XElement Roomavailabilty(XElement Req, string SupplierType, string htlID)
        {
            dmc = SupplierType;
            hotelcode = htlID;
            XElement AvailablilityResponse = null;
            List<XElement> Resp = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 1, 31);
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                int index =1;
                foreach (XElement room in Req.Descendants("RoomPax"))
                    room.Add(new XElement("id", index++));
                DataTable StaticdataTable = gda.GodouSingleHotelDetails(Req.Descendants("HotelID").FirstOrDefault().Value);
                XElement RoomResponse = new XElement("searchResponse");
                if (StaticdataTable.Rows.Count > 0)
                {
                    DataRow Staticdata = StaticdataTable.Rows[0];
                    DateTime starttime = DateTime.Now;
                    XElement resp = Resp.Descendants("Response").FirstOrDefault().Descendants("Data")
                                .Where(x => x.Descendants("HotelCode").FirstOrDefault().Value.Equals(Staticdata["HotelID"]))
                                .FirstOrDefault();
                    #region Log Save
                    try
                    {

                        #region Format Entries
                        //var saveReq = JsonConvert.DeserializeXmlNode(json, "Request");
                        //XElement save = XElement.Parse(saveReq.InnerXml);
                        #endregion
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                        log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                        log.SupplierID = 31;
                        log.logrequestXML = string.Empty;
                        log.logresponseXML = resp.ToString();
                        log.LogType = "RoomAvail";
                        log.LogTypeID = 2;
                        log.StartTime = starttime;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelAvailablitySearch";
                        ex1.PageName = "GodouServices";
                        ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                        ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    #endregion
                    //Get it from Database
                    #region Response XML
                    SupplierCurrency = resp.Descendants("Currency").FirstOrDefault().Value;
                    var availableRooms = new XElement("Hotel",
                                                   new XElement("HotelID", Staticdata["HtlUniqueID"]),
                                                   new XElement("HotelName", resp.Descendants("HotelName").FirstOrDefault().Value),
                                                   new XElement("PropertyTypeName"),
                                                   new XElement("CountryID", Req.Descendants("CountryID").FirstOrDefault().Value),

                                                   new XElement("CountryCode"),
                                                   new XElement("CountryName", Req.Descendants("CountryName").FirstOrDefault().Value),
                                                   new XElement("CityId", Req.Descendants("CityID").FirstOrDefault().Value),
                                                   new XElement("CityCode", Req.Descendants("CityCode").FirstOrDefault().Value),
                                                   new XElement("CityName", Req.Descendants("CityName").FirstOrDefault().Value),
                                                   new XElement("AreaName"),
                                                   new XElement("AreaId"),
                                                   new XElement("Address", Staticdata["Address"]),
                                                   new XElement("Location"),
                                                   new XElement("Description"),
                                                   new XElement("StarRating", Staticdata["StarRating"]),
                                                   new XElement("MinRate"),
                                                   new XElement("HotelImgSmall"),
                                                   new XElement("HotelImgLarge"),
                                                   new XElement("MapLink"),
                                                   new XElement("Longitude", Staticdata["Longitude"]),
                                                   new XElement("Latitude", Staticdata["Latitude"]),
                                                   new XElement("DMC", dmc),
                                                   new XElement("SupplierID", supplierID),
                                                   new XElement("Currency", resp.Descendants("Currency").FirstOrDefault().Value),
                                                  new XElement("Offers"),
                                                   new XElement("Facilities", new XElement("Facility", "No Facility available")),
                                                   groupedRooms(resp, Req));
                    RoomResponse.Add(new XElement("Hotels", availableRooms));
                }
                removetags(Req);
                
                    #endregion;
                    #region Response Format
                AvailablilityResponse = new XElement(
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
                                                           removeAllNamespaces(RoomResponse))));
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "Roomavailabilty";
                ex1.PageName = "GodouServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            return AvailablilityResponse;
        }
        #endregion
        #region Cancellation Policy
        public XElement CancellationPolicy(XElement Req)
        {
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            XElement CXpResponse = null;
            try
            {
                DataTable staticdatatable = gda.GodouSingleHotelDetails(Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value);
                DataRow staticdata = staticdatatable.Rows[0];
                List<XElement> RoomList = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 2, 31);                                    
                XElement cancelresponse = new XElement("HotelDetailwithcancellationResponse");
                string TID = Req.Descendants("Room").FirstOrDefault().Attribute("ID").Value.Split(new char[]{'_'})[0];
                string SID = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                dynamic foo = new ExpandoObject();
                foo.SessionID = SID;
                foo.TID = TID;
                foo.HotelCode = staticdata["HotelID"].ToString();
                string json = JsonConvert.SerializeObject(foo);
                DateTime starttime = DateTime.Now;
                string response = greq.ServiceResponses(json, "optionDetails", "", Req.Descendants("CustomerID").FirstOrDefault().Value);
                var resp = JsonConvert.DeserializeXmlNode(response, "root");
                XDocument SupplResponse = XDocument.Parse(resp.InnerXml);
                #region Log Save
                try
                {
                    #region Format Entries
                    #endregion
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                    log.SupplierID = 31;
                    log.logrequestXML = json;
                    log.logresponseXML = SupplResponse.ToString();
                    log.LogType = "CxlPolicy";
                    log.LogTypeID = 3;
                    log.StartTime = starttime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelAvailablitySearch";
                    ex1.PageName = "GodouServices";
                    ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion
                //XElement SupplResponse = null; //RoomList.Descendants("RoomAvail").Where(a => a.Descendants("Request").FirstOrDefault().Descendants("TID").FirstOrDefault().Value.Equals(TID)).FirstOrDefault();
                //foreach(XElement roomResponse in RoomList)
                //{
                //    if (roomResponse.Descendants("TID").FirstOrDefault().Value.Equals(TID))
                //        SupplResponse = roomResponse.Descendants("Response").FirstOrDefault();
                //}
                List<XElement> Policies = new List<XElement>();
                foreach (XElement policy in SupplResponse.Descendants("CxlPolicy"))
                {
                    Policies.Add(CancellationPolicyTags(policy, SupplResponse.Descendants("TotalPrice").FirstOrDefault().Value,Req.Descendants("FromDate").FirstOrDefault().Value));
                }
                #region Merge Policy and bind Response
                XElement finalcp = MergCxlPolicy(Policies);
                var cxp = new XElement("Hotels",
                                 new XElement("Hotel",
                                     new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                     new XElement("HotelName", Req.Descendants("HotelName").FirstOrDefault().Value),
                                     new XElement("HotelImgSmall"),
                                     new XElement("HotelImgLarge"),
                                     new XElement("MapLink"),
                                     new XElement("DMC", dmc),
                                     new XElement("Currency"),
                                     new XElement("Offers"),
                                     new XElement("Room",
                                         new XAttribute("ID", ""),
                                         new XAttribute("RoomType", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", ""),
                                         new XAttribute("TotalRoomRate", ""),
                                         new XAttribute("CancellationDate", ""),
                                        finalcp)));

                cancelresponse.Add(cxp);

                #region Response Format

                CXpResponse = new XElement(
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
                                                           removeAllNamespaces(cancelresponse))));
                #endregion
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancellationPolicy";
                ex1.PageName = "GodouServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            return CXpResponse;

        }
        #endregion
        #region Pre-Booking
        public XElement PreBooking(XElement Req, string xmlout)
        {
            XElement prebookingfinal = null;
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            dmc = xmlout;
            string TID = Req.Descendants("Room").FirstOrDefault().Attribute("ID").Value.Split(new char[] { '_' })[0];
            string SID = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
            try
            {

                XElement response = new XElement("HotelPreBookingResponse");
                DataTable staticdatatable = gda.GodouSingleHotelDetails(Req.Descendants("HotelID").FirstOrDefault().Value);
                DataRow Staticdata = staticdatatable.Rows[0];
                XElement resp = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 1, 31).Descendants("Response").FirstOrDefault().Descendants("Data")
                                .Where(x => x.Descendants("HotelCode").FirstOrDefault().Value.Equals(Staticdata["HotelID"].ToString()))
                                .FirstOrDefault();                
                //XElement sessionID = GetSessionId();
                //if (sessionID.Descendants("Success").FirstOrDefault().Value.Equals("true"))


                
                //    #region Request
                //    dynamic foo = new ExpandoObject();
                //    foo.SessionID = SID;
                //    foo.HotelCode = Staticdata["HotelID"].ToString();
                //    foo.CheckInDate = reformatDate(Req.Descendants("FromDate").FirstOrDefault().Value);
                //    foo.CheckOutDate = reformatDate(Req.Descendants("ToDate").FirstOrDefault().Value);
                //    foo.CurrencyCode = "EUR";
                //    foo.NationalityCode = Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value;
                //    foo.Rooms = SearchRooms(Req);
                //    string json = JsonConvert.SerializeObject(foo);
                //    DateTime starttime = DateTime.Now;
                //    string stringresponse = greq.ServiceResponses(json, "Search", "");
                //    var xmlconvert = JsonConvert.DeserializeXmlNode(stringresponse, "Root");
                //    XElement SupplResponse = XElement.Parse(xmlconvert.InnerXml);
                //    #region Log Save
                //    try
                //    {
                //        #region Format Entries
                //        var saveReq = JsonConvert.DeserializeXmlNode(json, "Request");
                //        XElement save = XElement.Parse(saveReq.InnerXml);
                //        #endregion
                //        APILogDetail log = new APILogDetail();
                //        log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                //        log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                //        log.SupplierID = 31;
                //        log.logrequestXML = save.ToString();
                //        log.logresponseXML = SupplResponse.ToString();
                //        log.LogType = "PreBook";
                //        log.LogTypeID = 4;
                //        log.StartTime = starttime;
                //        log.EndTime = DateTime.Now;
                //        SaveAPILog savelog = new SaveAPILog();
                //        savelog.SaveAPILogs(log);
                //    }
                //    catch (Exception ex)
                //    {
                //        #region Exception
                //        CustomException ex1 = new CustomException(ex);
                //        ex1.MethodName = "HotelAvailablitySearch";
                //        ex1.PageName = "GodouServices";
                //        ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                //        ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                //        SaveAPILog saveex = new SaveAPILog();
                //        saveex.SendCustomExcepToDB(ex1);
                //        #endregion
                //    }
                //    #endregion
                //    #endregion
                   

                    if (resp!=null)
                    {
                        #region Response Xml
                        var preBookResponse = new XElement("Hotels",
                                                                       new XElement("Hotel",
                                                                           new XElement("HotelID"),
                                                                           new XElement("HotelName"),
                                                                           new XElement("Status", "true"),
                                                                           new XElement("TermCondition"),
                                                                           new XElement("HotelImgSmall"),
                                                                           new XElement("HotelImgLarge"),
                                                                           new XElement("MapLink"),
                                                                           new XElement("DMC", dmc),
                                                                           new XElement("Currency", Req.Descendants("CurrencyName").FirstOrDefault().Value),
                                                                            new XElement("Offers"),
                                                                            PreBookRooms(resp, Req,TID,SID)));
                        string tnc = preBookResponse.Descendants("TermsnCondition").FirstOrDefault().Value;
                        preBookResponse.Descendants("TermsnCondition").FirstOrDefault().Remove();
                        preBookResponse.Descendants("TermCondition").FirstOrDefault().SetValue(tnc);
                        #endregion
                        response.Add(preBookResponse);
                    }
                    #region Response Format
                    string oldprice = Req.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value;
                    string newprice = response.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value;
                    prebookingfinal = new XElement(
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
                                                                new XElement(Req.Descendants("HotelPreBookingRequest").FirstOrDefault()),
                                                               response)));
                    #region Price Change Condition
                    if (oldprice.Equals(newprice))
                        return prebookingfinal;
                    else
                    {
                        prebookingfinal.Descendants("HotelPreBookingResponse").Descendants("Hotels").FirstOrDefault().AddBeforeSelf(
                           new XElement("ErrorTxt", "Amount has been changed"),
                           new XElement("NewPrice", newprice));

                    }
                    #endregion
                    #endregion 
                
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBooking";
                ex1.PageName = "GodouServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            return prebookingfinal;
        }
        #endregion
        #region Booking 
        public XElement BookingConfirmation(XElement Req)
        {
            XElement Response = null, confirmedBooking = null;            
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion

            try
            {
                DataTable StaticDataTable = gda.GodouSingleHotelDetails(Req.Descendants("HotelID").FirstOrDefault().Value);
                if (StaticDataTable.Rows.Count > 0)
                {
                    DataRow StaticData = StaticDataTable.Rows[0];
                    #region JSON Request
                    string SID = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                    string[] splitID = Req.Descendants("Room").FirstOrDefault().Attribute("RoomTypeID").Value.Split(new char[] { '_' });
                    string TID = splitID[0];
                    dynamic foo = new ExpandoObject();
                    foo.SessionID = SID;
                    foo.TID = TID;
                    foo.HotelCode = StaticData["HotelID"].ToString();
                    foo.PaymentOption = 1;
                    foo.AgencyReference = Req.Descendants("TransactionID").FirstOrDefault().Value;
                    foo.CarbonCopyMail = "test@test.com";
                    foo.Rooms = bookingRooms(Req); 
                    #endregion
                    string json = JsonConvert.SerializeObject(foo);
                    DateTime starttime = DateTime.Now;
                    string response = greq.ServiceResponses(json, "book", "",Req.Descendants("CustomerID").FirstOrDefault().Value);
                    var x = JsonConvert.DeserializeXmlNode(response, "root");
                    XElement SuplResponse = XElement.Parse(x.InnerXml);
                    #region Log Save
                    try
                    {
                        #region Format Entries
                        var saveReq = JsonConvert.DeserializeXmlNode(json, "root");
                        XElement save = XElement.Parse(saveReq.InnerXml);
                        #endregion
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                        log.TrackNumber = Req.Descendants("TransactionID").FirstOrDefault().Value;
                        log.SupplierID = 31;
                        log.logrequestXML = save.ToString();
                        log.logresponseXML = SuplResponse.ToString();
                        log.LogType = "Book";
                        log.LogTypeID = 5;
                        log.StartTime = starttime;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "BookingConfirmation";
                        ex1.PageName = "GodouServices";
                        ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                        ex1.TranID = Req.Descendants("TransactionID").FirstOrDefault().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    #endregion
                    if (SuplResponse.Descendants("Success").FirstOrDefault().Value.ToUpper().Equals("TRUE"))
                    {
                        string voucherRemark = string.Empty;
                        if (SuplResponse.Descendants("VoucherData").Any() && SuplResponse.Descendants("VoucherData").FirstOrDefault().HasElements)
                        {
                            voucherRemark += SuplResponse.Descendants("VoucherData").FirstOrDefault().Descendants("Remarks").Any()?SuplResponse.Descendants("VoucherData").FirstOrDefault().Descendants("Remarks").FirstOrDefault().Value:string.Empty;
                            voucherRemark += SuplResponse.Descendants("VoucherData").FirstOrDefault().Descendants("ProviderRemarks").Any() ? SuplResponse.Descendants("VoucherData").FirstOrDefault().Descendants("ProviderRemarks").FirstOrDefault().Value : string.Empty;
                            voucherRemark += SuplResponse.Descendants("VoucherData").FirstOrDefault().Descendants("EmergencyInfo").Any() ? SuplResponse.Descendants("VoucherData").FirstOrDefault().Descendants("EmergencyInfo").FirstOrDefault().Value : string.Empty;
                        }
                        int adult = 0;
                        int child = 0;
                        foreach (XElement ad in Req.Descendants("RoomPax"))
                        {
                            adult = adult + Convert.ToInt32(ad.Element("Adult").Value);
                            child = child + Convert.ToInt32(ad.Element("Child").Value);
                        }
                        List<string> success = new List<string>(new string[] { "CNF", "VCHR" });
                        #region Response XML
                        var hbr =
                                new XElement("Hotels",
                                            new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                            new XElement("HotelName", Req.Descendants("HotelName").FirstOrDefault().Value),
                                            new XElement("FromDate", Req.Descendants("FromDate").FirstOrDefault().Value),
                                            new XElement("ToDate", Req.Descendants("ToDate").FirstOrDefault().Value),
                                            new XElement("AdultPax", Convert.ToString(adult)),
                                            new XElement("ChildPax", Convert.ToString(child)),
                                            new XElement("TotalPrice", Req.Descendants("TotalAmount").FirstOrDefault().Value),
                                            new XElement("CurrencyID", Req.Descendants("CurrencyID").FirstOrDefault().Value),
                                            new XElement("CurrencyCode", Req.Descendants("CurrencyCode").FirstOrDefault().Value),
                                            new XElement("MarketID"),
                                            new XElement("MarketName"),
                                            new XElement("HotelImgSmall"),
                                            new XElement("HotelImgLarge"),
                                            new XElement("MapLink"),
                                            new XElement("VoucherRemark", voucherRemark),
                                            new XElement("TransID", Req.Descendants("TransactionID").FirstOrDefault().Value),
                                            new XElement("ConfirmationNumber", SuplResponse.Descendants("BookingReference").FirstOrDefault().Value),
                                            new XElement("Status", success.Contains(SuplResponse.Descendants("BookingStatus").FirstOrDefault().Value) ? "Success" : "Failed"),
                                            Booking_Rooms(Req, SuplResponse.Descendants("Rooms").ToList()));
                        #endregion
                        if (SuplResponse.Descendants("BookingStatus").FirstOrDefault().Value.ToUpper().Equals("PNDN"))
                        {
                            string parameter = "bookingReference=" + SuplResponse.Descendants("BookingReference").FirstOrDefault().Value;
                            DateTime starttime1 = DateTime.Now;
                            string response1 = greq.GetServerResponse("", "cancelBooking", parameter, Req.Descendants("CustomerID").FirstOrDefault().Value);
                            var x1 = JsonConvert.DeserializeXmlNode(response1, "root");
                            XElement SupplResponse = XElement.Parse(x1.InnerXml);
                            #region Log Save
                            try
                            {
                                APILogDetail log = new APILogDetail();
                                log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                                log.TrackNumber = Req.Descendants("TransactionID").FirstOrDefault().Value;
                                log.SupplierID = 31;
                                log.logrequestXML = null;
                                log.logresponseXML = SupplResponse.ToString();
                                log.LogType = "Cancel";
                                log.LogTypeID = 6;
                                log.StartTime = starttime;
                                log.EndTime = DateTime.Now;
                                SaveAPILog savelog = new SaveAPILog();
                                savelog.SaveAPILogs(log);
                            }
                            catch (Exception ex)
                            {
                                #region Exception
                                CustomException ex1 = new CustomException(ex);
                                ex1.MethodName = "BookingConfirmation";
                                ex1.PageName = "GodouServices";
                                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                                ex1.TranID = Req.Descendants("TransactionID").FirstOrDefault().Value;
                                SaveAPILog saveex = new SaveAPILog();
                                saveex.SendCustomExcepToDB(ex1);
                                #endregion
                            }
                            #endregion
                        }
                        Response = new XElement("HotelBookingResponse", hbr);
                    }
                    else
                    {
                        Response = new XElement("HotelBookingResponse",
                                        new XElement("ErrorTxt", "Booking Status unsuccessful"));
                    }
                }
                #region Response Format
                confirmedBooking = new XElement(
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
                                                            new XElement(Req.Descendants("HotelBookingRequest").FirstOrDefault()),
                                                           Response)));
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "BookingConfirmation";
                ex1.PageName = "GodouServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransactionID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            return confirmedBooking;
        }
        #endregion
        #region Booking Cancellation
        public XElement CanelBooking(XElement Req)
        {
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            XElement cancelledBooking = null;
            XElement Response = new XElement("HotelCancellationResponse");
            try
            {
                //List<XElement> prbResp = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 4, 0);
                //XElement cxp = prbResp.Where(a => a.Descendants("CancellationPolicies").FirstOrDefault().HasElements).FirstOrDefault().Descendants("Response").FirstOrDefault().Descendants("CancellationPolicies").FirstOrDefault();
                //double amount = double.MinValue;
                //foreach (XElement policy in cxp.Descendants("CancellationPolicy"))
                //{
                //    DateTime policyDate = DateTime.ParseExact(policy.Attribute("LastCancellationDate").Value, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                //    if (DateTime.Now > policyDate)
                //        amount = Convert.ToDouble(policy.Attribute("ApplicableAmount").Value);
                //}
                //if (amount == double.MinValue)
                //    amount = Convert.ToDouble(cxp.Descendants("CancellationPolicy").FirstOrDefault().Attribute("ApplicableAmount").Value);
                List<XElement> bkResp = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 5, 31);
                string lastDate = bkResp.Descendants("Response").FirstOrDefault().Descendants("CancellationDeadline").FirstOrDefault().Value.Substring(0, 10);
                DateTime checkDate = DateTime.ParseExact(lastDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (checkDate >= DateTime.Now)//if(true)
                {
                    string parameter = "bookingReference=" + Req.Descendants("ConfirmationNumber").FirstOrDefault().Value;
                    DateTime starttime = DateTime.Now;
                    string response = greq.GetServerResponse("", "cancelBooking", parameter, Req.Descendants("CustomerID").FirstOrDefault().Value);
                    var x = JsonConvert.DeserializeXmlNode(response, "root");
                    XElement SupplResponse = XElement.Parse(x.InnerXml);
                    #region Log Save
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                        log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                        log.SupplierID = 31;
                        log.logrequestXML = null;
                        log.logresponseXML = SupplResponse.ToString();
                        log.LogType = "Cancel";
                        log.LogTypeID = 6;
                        log.StartTime = starttime;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelAvailablitySearch";
                        ex1.PageName = "GodouServices";
                        ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                        ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    #endregion
                    if (SupplResponse.Descendants("Success").FirstOrDefault().Value.ToUpper().Equals("TRUE"))
                    {
                        #region Response XML
                        Response.Add(new XElement("Rooms",
                                             new XElement("Room",
                                                new XElement("Cancellation",
                                                    new XElement("Amount", Convert.ToString(0)),
                                                    new XElement("Status", "Success")))));
                        #endregion
                    }
                    else
                    {
                        Response.Add(new XElement("ErrorTxt", "Cancellation Failed, Please check Supplier Log"));
                    }
                }
                else
                    Response.Add(new XElement("ErrorTxt", "Cancellation Date has already passed. Booking can not be cancelled now"));
                #region Response Format
                cancelledBooking = new XElement(
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
                                                           new XElement(removeAllNamespaces(Req.Descendants("HotelCancellationRequest").FirstOrDefault())),
                                                          Response)));
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CanelBooking";
                ex1.PageName = "GodouServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            return cancelledBooking;
        }
        #endregion

        
        #region Common Functions

        #region Room Grouping From Supplier
        public XElement Rooms(XElement SupplierResponse, XElement request)
        {
            GodouRequest greq = new GodouRequest();
            List<XElement> RoomList = new List<XElement>();
            int count = 1;
            int index = 1;
           
            foreach (XElement rm in request.Descendants("RoomPax"))
            {
                rm.Add(new XElement("id", count++));
            }
            foreach (XElement Room in SupplierResponse.Descendants("Options"))
            {
                try
                {
                    int cnt = 1;
                    dynamic foo = new ExpandoObject();
                    foo.SessionID = request.Descendants("RequestID").FirstOrDefault().Value;
                    foo.TID = Room.Descendants("TID").FirstOrDefault().Value;
                    foo.HotelCode = SupplierResponse.Descendants("HotelCode").FirstOrDefault().Value;
                    string json = JsonConvert.SerializeObject(foo);
                    DateTime starttime = DateTime.Now;
                    string response = greq.ServiceResponses(json, "optionDetails", "", request.Descendants("CustomerID").FirstOrDefault().Value);
                    var resp = JsonConvert.DeserializeXmlNode(response, "root");
                    XDocument serverResponse = XDocument.Parse(resp.InnerXml);
                    #region Log Save
                    try
                    {
                        #region Format Entries
                        var saveReq = JsonConvert.DeserializeXmlNode(json, "root");
                        XElement save = XElement.Parse(saveReq.InnerXml);
                        #endregion
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(request.Descendants("CustomerID").FirstOrDefault().Value);
                        log.TrackNumber = request.Descendants("TransID").FirstOrDefault().Value;
                        log.SupplierID = 31;
                        log.logrequestXML = save.ToString();
                        log.logresponseXML = serverResponse.ToString();
                        log.LogType = "RoomAvail";
                        log.LogTypeID = 2;
                        log.StartTime = starttime;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "Rooms";
                        ex1.PageName = "GodouServices";
                        ex1.CustomerID = request.Descendants("CustomerID").FirstOrDefault().Value;
                        ex1.TranID = request.Descendants("TransID").FirstOrDefault().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    #endregion
                    if (serverResponse.Descendants("Success").Last().Value.ToUpper().Equals("TRUE"))
                    {
                        var list = from reqRooms in Room.Descendants("Rooms")
                                   join respRooms in serverResponse.Descendants("Rooms")
                                   on reqRooms.Element("RoomId").Value equals respRooms.Element("RoomId").Value
                                   join ingRooms in request.Descendants("RoomPax")
                                   on reqRooms.Element("RoomId").Value equals ingRooms.Element("id").Value
                                   select
                                              new XElement("Room",
                                                   new XAttribute("ID", Room.Element("TID").Value + "_" + reqRooms.Element("RoomId").Value),
                                                      new XAttribute("SuppliersID", "31"),
                                                      new XAttribute("RoomSeq", Convert.ToString(cnt++)),
                                                      new XAttribute("SessionID", request.Descendants("RequestID").FirstOrDefault().Value),
                                                      new XAttribute("RoomType", reqRooms.Descendants("Description").FirstOrDefault().Value),
                                                      new XAttribute("OccupancyID", ingRooms.Descendants("id").FirstOrDefault().Value),
                                                      new XAttribute("OccupancyName", ""),
                                                      new XAttribute("MealPlanID", ""),
                                                      new XAttribute("MealPlanName", Room.Descendants("MealName").FirstOrDefault().Value),
                                                      new XAttribute("MealPlanCode", Room.Descendants("MealType").FirstOrDefault().Value),
                                                      new XAttribute("MealPlanPrice", ""),
                                                      new XAttribute("PerNightRoomRate", respRooms.Descendants("Price").FirstOrDefault().Value),
                                                      new XAttribute("TotalRoomRate", TotalRoomRate(respRooms.Descendants("Price").ToList())),                   
                                                      new XAttribute("CancellationDate", ""),
                                                      new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", Room.Descendants("OnRequest").FirstOrDefault().Value.Equals("false") ? "true" : "false"),
                                                      new XElement("RequestID", request.Descendants("RequestID").FirstOrDefault().Value),
                                                      new XElement("Offers"),
                                                      new XElement("PromotionList",
                                                      new XElement("Promotions")),
                                                      new XElement("CancellationPolicy"),
                                                      new XElement("Amenities",
                                                          new XElement("Amenity")),
                                                      new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                      new XElement("Supplements"),
                                                      pb(respRooms.Descendants("Prices").ToList()),
                                                      new XElement("AdultNum", ingRooms.Element("Adult").Value),
                                                      new XElement("ChildNum", ingRooms.Element("Child").Value));

                        RoomList.Add(new XElement("RoomTypes",
                                         new XAttribute("TotalRate", Room.Element("TotalPrice").Value),
                                         new XAttribute("HtlCode", hotelcode),
                                         new XAttribute("CrncyCode", SupplierCurrency),
                                         new XAttribute("DMCType", dmc),
                                         new XAttribute("Index", index++), list));
                    }
                }
                catch {  }
            }
            return new XElement("Rooms", RoomList);
        }
        #region TotalRoomRate
        public string TotalRoomRate(List<XElement> Prices)
        {
            double total = 0.0;
            foreach (XElement price in Prices)
                total += Convert.ToDouble(price.Value);
            return total.ToString();
        }
        #endregion
        #region Price Break-Up
        public XElement pb(List<XElement> Prices)
        {
            XElement response = new XElement("PriceBreakups");
            int counter = 1;
            foreach (XElement element in Prices)
            {
                response.Add(new XElement("Price",
                                 new XAttribute("Night", counter++),
                                 new XAttribute("PriceValue", Convert.ToString(element.Element("Price").Value))));
            }
            return response;
        }
        #endregion
        #endregion
        #region Room Grouping DB
        public XElement groupedRooms(XElement resp, XElement Req)
        {
            XElement Response = new XElement("Rooms");
            int index = 1;
            foreach (XElement Room in resp.Descendants("Options"))
            {                
                int cnt = 1, nights = Convert.ToInt32(Req.Descendants("Nights").FirstOrDefault().Value);
                double totalPrice = Convert.ToDouble(Room.Element("TotalPrice").Value);
                double perRoomPrice = Math.Round(totalPrice / (Req.Descendants("RoomPax").Count()),2);
                double perNightPrice = Math.Round(perRoomPrice / nights, 2);
                XElement RoomTypes = new XElement("RoomTypes",
                                         new XAttribute("TotalRate", Convert.ToString(totalPrice)),
                                         new XAttribute("HtlCode", hotelcode),
                                         new XAttribute("CrncyCode", SupplierCurrency),
                                         new XAttribute("DMCType", dmc),
                                         new XAttribute("Index", index++));
                foreach (XElement reqRooms in Room.Descendants("Rooms"))
                {
                    XElement ingRooms = Req.Descendants("RoomPax").Where(x => x.Element("id").Value.Equals(cnt.ToString())).FirstOrDefault();
                    RoomTypes.Add(new XElement("Room",
                                    new XAttribute("ID", Room.Element("TID").Value + "_" + reqRooms.Element("RoomId").Value),
                                        new XAttribute("SuppliersID", "31"),
                                        new XAttribute("RoomSeq", Convert.ToString(cnt++)),
                                        new XAttribute("SessionID", Req.Descendants("RequestID").FirstOrDefault().Value),
                                        new XAttribute("RoomType", reqRooms.Descendants("Description").FirstOrDefault().Value),
                                        new XAttribute("OccupancyID", ingRooms.Descendants("id").FirstOrDefault().Value),
                                        new XAttribute("OccupancyName", ""),
                                        new XAttribute("MealPlanID", ""),
                                        new XAttribute("MealPlanName", Room.Descendants("MealName").FirstOrDefault().Value),
                                        new XAttribute("MealPlanCode", Room.Descendants("MealType").FirstOrDefault().Value),
                                        new XAttribute("MealPlanPrice", ""),
                                        new XAttribute("PerNightRoomRate", perNightPrice.ToString()),
                                        new XAttribute("TotalRoomRate", perRoomPrice.ToString()),
                                        new XAttribute("CancellationDate", ""),
                                        new XAttribute("CancellationAmount", ""),
                                        new XAttribute("isAvailable", Room.Descendants("OnRequest").FirstOrDefault().Value.Equals("false") ? "true" : "false"),
                                        new XElement("RequestID", Req.Descendants("RequestID").FirstOrDefault().Value),
                                        new XElement("Offers"),
                                        new XElement("PromotionList",
                                        new XElement("Promotions")),
                                        new XElement("CancellationPolicy"),
                                        new XElement("Amenities",
                                            new XElement("Amenity")),
                                        new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                        new XElement("Supplements"),
                                        pbNew(nights, perNightPrice),
                                        new XElement("AdultNum", ingRooms.Element("Adult").Value),
                                        new XElement("ChildNum", ingRooms.Element("Child").Value)));
                }
                Response.Add(RoomTypes);
            }
            return Response;
        }
        #region Static Price Break-up
        public XElement pbNew(int nights, double dailyPrice)
        {
            XElement response = new XElement("PriceBreakups");
            for(int i=1;i<=nights;i++)
            {
                response.Add(new XElement("Price",
                                new XAttribute("Nights", i.ToString()),
                                new XAttribute("PriceValue", dailyPrice)));
            }
            return response;
        }
        #endregion
        #endregion
        #region Pre-booking Rooms
        public XElement PreBookRooms(XElement SupplierResponse, XElement request, string TID, string SID)
        {
            GodouRequest greq = new GodouRequest();
            List<XElement> RoomList = new List<XElement>();
            int count = 1;
            int index = 1;

            foreach (XElement rm in request.Descendants("RoomPax"))
            {
                rm.Add(new XElement("id", count++));
            }
            int checker = SupplierResponse.Descendants("Options").Where(x => x.Element("TID").Value.Equals(TID)).Count();
            foreach (XElement Room in SupplierResponse.Descendants("Options").Where(x=>x.Element("TID").Value.Equals(TID)))
            {
                try
                {
                    int cnt = 1;
                    dynamic foo = new ExpandoObject();
                    foo.SessionID = SID;
                    foo.TID = TID;
                    foo.HotelCode = SupplierResponse.Descendants("HotelCode").FirstOrDefault().Value;
                    string json = JsonConvert.SerializeObject(foo);
                    DateTime starttime = DateTime.Now;
                    string response = greq.ServiceResponses(json, "optionDetails", "", request.Descendants("CustomerID").FirstOrDefault().Value);
                    var resp = JsonConvert.DeserializeXmlNode(response, "root");
                    XDocument serverResponse = XDocument.Parse(resp.InnerXml);
                    #region Log Save
                    try
                    {
                        #region Format Entries
                        var saveReq = JsonConvert.DeserializeXmlNode(json, "root");
                        XElement save = XElement.Parse(saveReq.InnerXml);
                        #endregion
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(request.Descendants("CustomerID").FirstOrDefault().Value);
                        log.TrackNumber = request.Descendants("TransID").FirstOrDefault().Value;
                        log.SupplierID = 31;
                        log.logrequestXML = save.ToString();
                        log.logresponseXML = serverResponse.ToString();
                        log.LogType = "PreBook";
                        log.LogTypeID = 4;
                        log.StartTime = starttime;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "PreBookingRooms";
                        ex1.PageName = "GodouServices";
                        ex1.CustomerID = request.Descendants("CustomerID").FirstOrDefault().Value;
                        ex1.TranID = request.Descendants("TransID").FirstOrDefault().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    #endregion
                    if (serverResponse.Descendants("Success").Last().Value.ToUpper().Equals("TRUE"))
                    {
                        var list = from reqRooms in Room.Descendants("Rooms")
                                   join respRooms in serverResponse.Descendants("Rooms")
                                   on reqRooms.Element("RoomId").Value equals respRooms.Element("RoomId").Value
                                   join ingRooms in request.Descendants("RoomPax")
                                   on reqRooms.Element("RoomId").Value equals ingRooms.Element("id").Value
                                   select
                                              new XElement("Room",
                                                   new XAttribute("ID", TID + "_" + reqRooms.Element("RoomId").Value),
                                                      new XAttribute("SuppliersID", "31"),
                                                      new XAttribute("RoomSeq", Convert.ToString(cnt++)),
                                                      new XAttribute("SessionID", SID),
                                                      new XAttribute("RoomType", reqRooms.Descendants("Description").FirstOrDefault().Value),
                                                      new XAttribute("OccupancyID", ingRooms.Descendants("id").FirstOrDefault().Value),
                                                      new XAttribute("OccupancyName", ""),
                                                      new XAttribute("MealPlanID", ""),
                                                      new XAttribute("MealPlanName", Room.Descendants("MealName").FirstOrDefault().Value),
                                                      new XAttribute("MealPlanCode", Room.Descendants("MealType").FirstOrDefault().Value),
                                                      new XAttribute("MealPlanPrice", ""),
                                                      new XAttribute("PerNightRoomRate", respRooms.Descendants("Price").FirstOrDefault().Value),
                                                      new XAttribute("TotalRoomRate", TotalRoomRate(respRooms.Descendants("Price").ToList())),                     
                                                      new XAttribute("CancellationDate", ""),
                                                      new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", Room.Descendants("OnRequest").FirstOrDefault().Value.Equals("false") ? "true" : "false"),
                                                      new XElement("RequestID", SID),
                                                      new XElement("Offers"),
                                                      new XElement("PromotionList",
                                                      new XElement("Promotions")),
                                                      new XElement("CancellationPolicy"),
                                                      new XElement("Amenities",
                                                          new XElement("Amenity")),
                                                      new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                      new XElement("Supplements"),
                                                      pb(respRooms.Descendants("Prices").ToList()),
                                                      new XElement("AdultNum", ingRooms.Element("Adult").Value),
                                                      new XElement("ChildNum", ingRooms.Element("Child").Value));

                        RoomList.Add(new XElement("RoomTypes",
                                         new XAttribute("TotalRate", serverResponse.Descendants("TotalPrice").FirstOrDefault().Value),
                                         new XAttribute("Index", index++), list));
                        List<XElement> Policies = new List<XElement>();
                        foreach (XElement policy in serverResponse.Descendants("CxlPolicy"))
                        {
                            Policies.Add(CancellationPolicyTags(policy, serverResponse.Descendants("TotalPrice").FirstOrDefault().Value, request.Descendants("FromDate").FirstOrDefault().Value));
                        }
                        XElement finalcp = MergCxlPolicy(Policies);
                        RoomList.Descendants("Room").Last().AddAfterSelf(finalcp);
                        string TnC = null;
                        if (serverResponse.Descendants("Remarks").Any())
                        {
                            foreach (XElement remark in serverResponse.Descendants("Remarks"))
                            {
                                TnC += "<ul><li>" + remark.Value + "</li></ul>";
                            }
                        }
                        foreach(XElement text in serverResponse.Descendants("Conditions"))
                        {
                            if (Convert.ToDouble(text.Descendants("Amount").FirstOrDefault().Value) == 0)
                            {
                                if(!string.IsNullOrEmpty(text.Descendants("Text").FirstOrDefault().Value))
                                    TnC+="<ul><li>"+text.Descendants("Text").FirstOrDefault().Value+"</li></ul>";
                            }
                        }
                        RoomList.Descendants("Room").Last().AddAfterSelf(new XElement("TermsnCondition", TnC));
                    }
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "PreBookRooms";
                    ex1.PageName = "GodouServices";
                    ex1.CustomerID = request.Descendants("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = request.Descendants("TransID").FirstOrDefault().Value;
                    APILog.SendCustomExcepToDB(ex1);
                    #endregion
                }
            }
            return new XElement("Rooms", RoomList);
        }       
        #endregion
        #region Session ID
        public XElement GetSessionId(string customerID)
        {
            string ServerResponse = null;
            //string url = "http://webapi.uat.wbe.travel/api/security/createSessionID";                        
            try
            {
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerID, "31");
                string username = suppliercred.Descendants("Username").FirstOrDefault().Value;
                string password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                string apiKey = suppliercred.Descendants("API_Key").FirstOrDefault().Value;
                string url = suppliercred.Descendants("sessionURL").FirstOrDefault().Value;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                request.Headers.Add("Authorization", "Basic " + svcCredentials);
                request.Headers.Add("WBE-Api-Key", apiKey);
                request.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes("");
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                    ServerResponse = reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.GetEncoding("utf-8"));
                    string errorText = reader.ReadToEnd();
                }
                throw;
            }
            var x = JsonConvert.DeserializeXmlNode(ServerResponse, "root");
            XElement y = XElement.Parse(x.InnerXml);

            return y;
        }
        #endregion
        #region Reformat Date
        public string reformatDate(string date)
        {
            DateTime dt = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            string dd = dt.ToString("yyyy-MM-dd");
            return dd;
        }
        #endregion
        #region Minimum Rate for Hotel
        public string minRate(List<XElement> Rooms)
        {
            double min = int.MaxValue;
            double check = 0;
            foreach (XElement room in Rooms)
            {
                check = Convert.ToDouble(room.Descendants("TotalPrice").FirstOrDefault().Value);
                if (check < min)
                    min = check;
            }
            return min.ToString();
        }
        #endregion
        #region Rooms For Hotel Search
        public dynamic SearchRooms(XElement Req)
        {
            dynamic roomlist = new dynamic[Req.Descendants("RoomPax").Count()];
            int roomid = 1;

            XElement rooms = Req.Descendants("Rooms").FirstOrDefault();
            foreach (XElement room in rooms.Descendants("RoomPax"))
            {
                dynamic foo = new ExpandoObject();
                string[] childrenages = new string[room.Descendants("ChildAge").Count()];
                foo.RoomID = roomid;
                foo.AdultsNo = Convert.ToInt32(room.Descendants("Adult").FirstOrDefault().Value);
                foo.ChildrenNo = Convert.ToInt32(room.Descendants("Child").FirstOrDefault().Value);
                if (room.Descendants("ChildAge").Any())
                {
                    int chcnt = 0;
                    foreach (XElement age in room.Descendants("ChildAge"))
                    {
                        childrenages[chcnt] = age.Value;
                        chcnt++;
                    }
                }
                foo.ChildrenAges = childrenages;
                roomlist[roomid - 1] = foo;
                roomid++;
            }
            return roomlist;
        }
        #endregion
        #region Rooms For Booking Service
        #region Booking Request
		  public dynamic bookingRooms(XElement Req)
        {
            dynamic roomlist = new dynamic[Req.Descendants("Room").Count()];
            int roomID = 1;
            foreach(XElement room in Req.Descendants("Room"))
            {
                dynamic foo = new ExpandoObject();
                foo.RoomID = roomID;
                foo.AdultsNo = room.Descendants("GuestType").Where(x => x.Value.Equals("Adult")).Count();
                foo.ChildrenNo = room.Descendants("GuestType").Where(x => x.Value.Equals("Child")).Count();
                foo.Passengers = passengersBooking(room);
                roomlist[roomID-1] = foo;
                roomID++;
            }
            return roomlist;
        }
        public dynamic passengersBooking(XElement Room)
        {
            dynamic pList = new dynamic[Room.Descendants("PaxInfo").Count()];
            int count = 0;
            foreach(XElement paxes in Room.Descendants("PaxInfo"))
            {
                if (NotNull(paxes))
                {
                    string Salutation = paxes.Element("Title").Value;
                    if (Salutation.Equals("Dr") || Salutation.Equals("Prof") || Salutation.Equals("Sheikh"))
                        Salutation = "Mr";
                    else if (Salutation.Equals("Sheikha"))
                        Salutation = "Mrs";
                    else if (Salutation.Equals("Master"))
                        Salutation = "Chd";
                    bool IsChild = paxes.Element("GuestType").Value.Equals("Adult") ? false : true;
                    if (IsChild)
                        Salutation = "Chd";
                    dynamic foo = new ExpandoObject();
                    foo.Salutation = Salutation;
                    foo.Age = IsChild ? paxes.Element("Age").Value : null;
                    foo.FirstName = paxes.Element("FirstName").Value;
                    foo.LastName = paxes.Element("LastName").Value;
                    foo.IsChild = IsChild;
                    pList[count] = foo;
                    count++;
                }
            }
            return pList;
        }
        public bool NotNull(XElement paxes)
        {
            if(String.IsNullOrEmpty(paxes.Element("Title").Value))
                return false;
            if (String.IsNullOrEmpty(paxes.Element("FirstName").Value))
                return false;
            if (String.IsNullOrEmpty(paxes.Element("LastName").Value))
                return false;
            else
                return true;
        }
	    #endregion
        #region Booking Response
        public XElement Booking_Rooms(XElement Req, List<XElement> Rooms)
        {
            XElement PassengerDetail = new XElement("PassengersDetail");
            XElement GuestList = new XElement("GuestDetails");
            foreach (XElement room in Rooms)
            {
                var Room = new XElement("Room",
                               new XAttribute("ID", ""),
                               new XAttribute("RoomType",""),
                               new XAttribute("ServiceID", ""),
                               new XAttribute("MealPlanID", ""),
                               new XAttribute("MealPlanName", ""),
                               new XAttribute("MealPlanCode", ""),
                               new XAttribute("MealPlanPrice", ""),
                               new XAttribute("PerNightRoomRate", ""),
                               new XAttribute("RoomStatus", ""),
                               new XAttribute("TotalRoomRate", ""),
                                 passengersInfo(room.Descendants("Passengers").Where(x => x.Element("IsChild").Value.Equals("false")).FirstOrDefault()),
                               new XElement("Supplements"));
                GuestList.Add(Room);
            }
            PassengerDetail.Add(GuestList);
            return PassengerDetail;
        }
        #endregion
        public XElement passengersInfo(XElement passenger)
        {
            XElement rguest = new XElement("RoomGuest",
                                new XElement("GuestType", "Adult"),
                                new XElement("Title", passenger.Element("Salutation").Value),
                                new XElement("FirstName", passenger.Element("FirstName").Value),
                                new XElement("MiddleName"),
                                new XElement("LastName", passenger.Element("LastName").Value),
                                new XElement("IsLead", "true"),
                                new XElement("Age", passenger.Element("Age").Value));
            return rguest;
        }
        #endregion
        #region Remove Namespaces
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
        public XElement removecdata(XElement e)
        {
            foreach (XElement x in e.Elements())
            {
                if (x.HasElements)
                {
                    removecdata(x);
                }
                else
                {
                    string check = x.Value;
                    check.Replace("![CDATA[", "").Replace("]]", "");
                    x.SetValue(check);
                }
            }
            return e;
        }
        #endregion
        #region Get Xmls From Log
        public List<XElement> LogXMLs(string trackID, int logtypeID, int SupplierID)
        {
            RestelLogAccess rlac = new RestelLogAccess();
            List<XElement> response = new List<XElement>();
            DataTable LogTable = new DataTable();
            LogTable = rlac.GetLog(trackID, logtypeID, SupplierID);
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
        #region Generate Cancellation Policy Tag
        public XElement CancellationPolicyTags(XElement Policy, string TotalAmount, string Checkin)
        {
            XElement cp = new XElement("CancellationPolicies");
            bool textPreferenceCase =false;
            #region Check for text based policy
            foreach (XElement condition in Policy.Descendants("Conditions").Where(x => Convert.ToDecimal(x.Element("Amount").Value) == 0))
            {
                if (!string.IsNullOrEmpty(condition.Element("Text").Value))
                    textPreferenceCase = true;
            } 
            #endregion
            if (Policy.Descendants("NonRefundable").FirstOrDefault().Value.ToUpper().Equals("TRUE") || textPreferenceCase)
            {
                #region Non Refundable Policy
                string dt = Policy.Descendants("OptionDate").FirstOrDefault().Value.Substring(0, 10);
                DateTime dd = DateTime.ParseExact(dt, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                dt = dd.ToString("dd/MM/yyyy");
                cp.Add(new XElement("CancellationPolicy",
                            new XAttribute("LastCancellationDate", dt),
                            new XAttribute("ApplicableAmount", TotalAmount),
                            new XAttribute("NoShowPolicy", "0"))); 
                #endregion
            }
            else
            {
                foreach (XElement element in Policy.Descendants("Conditions"))
                {
                    string dt = element.Descendants("FromDate").FirstOrDefault().Value.Substring(0, 10);
                    DateTime dd = DateTime.ParseExact(dt, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    dt = dd.ToString("dd/MM/yyyy");
                    cp.Add(new XElement("CancellationPolicy",
                                new XAttribute("LastCancellationDate", dt),
                                new XAttribute("ApplicableAmount", element.Element("Amount").Value),
                                new XAttribute("NoShowPolicy", dt.Equals(Checkin)?"1":"0")));
                }
            }
            return cp;
        }
        #endregion
        #region Merge Cancellation Policy
        public XElement MergCxlPolicy(List<XElement> rooms)
        {
            List<XElement> cxlList = new List<XElement>();

            IEnumerable<XElement> dateLst = rooms.Descendants("CancellationPolicy").
               GroupBy(r => new { r.Attribute("LastCancellationDate").Value, noshow = r.Attribute("NoShowPolicy").Value }).Select(y => y.FirstOrDefault()).
               OrderBy(p => p.Attribute("LastCancellationDate").Value);
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
                                datePrice += Convert.ToDecimal(rm.Attribute("ApplicableAmount").Value);
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
        public DateTime chnagetoTime(string strDate)
        {
            DateTime oDate = DateTime.ParseExact(strDate, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            return oDate;

        }
        #endregion
        #region Star Rating Check
        public bool StarRating(string minstar, string maxstar, string starRating)
        {
            if (Convert.ToInt32(starRating) >= Convert.ToInt32(minstar) && Convert.ToInt32(starRating) <= Convert.ToInt32(maxstar))
                return true;
            else
                return false;
                   
        }
        #endregion
        #region Remove Extra Tags
        public XElement removetags(XElement input)
        {
            if (input.Descendants("id").Any())
                input.Descendants("id").Remove();
            if (input.Descendants("paxes").Any())
                input.Descendants("paxes").Remove();
            if (input.Descendants("NewChildCount").Any())
                input.Descendants("NewChildCount").Remove();
            return input;

        }
        #endregion
        #region Location
        public string getLocation(string LocationString)
        {
            string location = string.Empty;
            foreach (char ch in LocationString)
            {
                if (Char.IsLetter(ch) || Char.IsWhiteSpace(ch))
                    location += ch;
                else
                    break;

            }
            if (location.Replace(" ", string.Empty).ToUpper().Equals("POBOX") || location.Length < 2)
                location = string.Empty;
            return location;
        }
        #endregion
        #endregion
    }
}