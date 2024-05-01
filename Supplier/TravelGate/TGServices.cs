using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Common;
using TravillioXMLOutService.Models.TravelGate;

namespace TravillioXMLOutService.Supplier.TravelGate
{
    public class TGServices
    {
        string dmc, hotelcode, currencyCode;
        string customerid = string.Empty;
        int sup_cutime = 100000, timeDifference = 1800; 
        TGDataAccess tgd = new TGDataAccess();
        TGRequest tgr = new TGRequest();
        int supplierID;
        string client, testMode, context, url, apiKey, accessCode;
        XNamespace xmlns = "http://schemas.xmlsoap.org/soap/envelope/";
        XElement SmyRoomsMealPlans = null;
        public TGServices(int SuplID, string CustID)
        {
            try
            {
                supplierID = SuplID;
                TravelGateConfiguration(SuplID, CustID);
            }
            catch { }
        }
        public TGServices()
        {
        }
        #region Hotel Search
        public List<XElement> HotelSearch(XElement Req, string suplType,string custID)
        {
            dmc = suplType;
            customerid = custID;
            List<XElement> HotelList = new List<XElement>();
            try
            {
                #region get cut off time
                try
                {
                    sup_cutime = supplier_Cred.secondcutoff_time();
                }
                catch { }
                #endregion
                //string CityID = Req.Descendants("CityID").FirstOrDefault().Value;
                //DataTable cityMapping = tgd.CityMapping(CityID, "39");
                //List<string> Supl_Cities = new List<string>();
                List<string> HotelListForReq = new List<string>();

                string HtId = Req.Descendants("HotelID").FirstOrDefault().Value;
                if (!string.IsNullOrEmpty(HtId))
                {
                    string SupCityId = TravayooRepository.SupllierCity("39", Req.Descendants("CityID").FirstOrDefault().Value);
                    int CityId = Convert.ToInt32(SupCityId);
                    var reqmodel = new SqlModel()
                    {
                        flag = 2,
                        columnList = "HotelID,HotelName,Star",
                        table = "smyhotellist",
                        filter = "CityID=" + CityId.ToString() + " AND HotelName LIKE '%" + Req.Descendants("HotelName").FirstOrDefault().Value + "%'",
                        SupplierId = 39
                    };
                    if (!string.IsNullOrEmpty(Req.Descendants("HotelID").FirstOrDefault().Value))
                    {
                        reqmodel.HotelCode = Req.Descendants("HotelID").FirstOrDefault().Value;
                    }
                    DataTable htlList = TravayooRepository.GetData(reqmodel);
                    if (htlList.Rows.Count > 0)
                    {
                        foreach (DataRow item in htlList.Rows)
                        {
                            HotelListForReq.Add(item["HotelID"].ToString());
                        }
                    }
                    else
                    {
                        try
                        {
                            APILogDetail log = new APILogDetail();
                            log.customerID = Convert.ToInt32(customerid);
                            log.LogTypeID = 1;
                            log.LogType = "Search";
                            log.SupplierID = 39;
                            log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
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
                }
                else
                {
                    List<string> Supl_Cities = new List<string>();
                    string CityID = Req.Descendants("CityID").FirstOrDefault().Value;
                    DataTable cityMapping = tgd.CityMapping(CityID, "39");
                    for (int i = 0; i < cityMapping.Rows.Count; i++)
                    {
                        Supl_Cities.Add(cityMapping.Rows[i]["SupCityId"].ToString());
                    }
                    int maxStar = Convert.ToInt32(Req.Descendants("MaxStarRating").FirstOrDefault().Value), minStar = Convert.ToInt32(Req.Descendants("MinStarRating").FirstOrDefault().Value);
                    foreach (string city in Supl_Cities)
                    {
                        DataTable cityWiseHotels = tgd.HotelList(city);
                        for (int j = 0; j < cityWiseHotels.Rows.Count; j++)
                        {
                            int starRating = Convert.ToInt32(cityWiseHotels.Rows[j]["Star"].ToString());
                            if (starRating >= minStar && starRating <= maxStar)
                                HotelListForReq.Add(cityWiseHotels.Rows[j]["HotelID"].ToString());
                        }
                    }
                }
                //for (int i = 0; i < cityMapping.Rows.Count; i++)
                //{
                //    Supl_Cities.Add(cityMapping.Rows[i]["SupCityId"].ToString());
                //}
                //int maxStar = Convert.ToInt32(Req.Descendants("MaxStarRating").FirstOrDefault().Value), minStar = Convert.ToInt32(Req.Descendants("MinStarRating").FirstOrDefault().Value);
                //foreach (string city in Supl_Cities)
                //{
                //    DataTable cityWiseHotels = tgd.HotelList(city);
                //    for (int j = 0; j < cityWiseHotels.Rows.Count; j++)
                //    {
                //        int starRating = Convert.ToInt32(cityWiseHotels.Rows[j]["Star"].ToString());
                //        if (starRating >= minStar && starRating <= maxStar)
                //            HotelListForReq.Add(cityWiseHotels.Rows[j]["HotelID"].ToString());
                //    }
                //}
                string hotelsString = string.Empty;
                int timeout = sup_cutime - timeDifference > 24700 ? 24700 : sup_cutime - timeDifference;
                
                List<string> Currencies = currencyList();
                if (Currencies.Contains(Req.Descendants("DesiredCurrencyCode").FirstOrDefault().Value))
                    currencyCode = Req.Descendants("DesiredCurrencyCode").FirstOrDefault().Value;
                else
                    currencyCode = "EUR";
                #region Request
                #region Requset Filter Access
                var filter = new Access
                {
                    access = new Includes
                    {
                        includes = accessCode
                    },
                };
                var filterserializer = new JsonSerializer();
                var filterstringwriter = new StringWriter();
                using(var filterWriter = new JsonTextWriter(filterstringwriter))
                {
                    filterWriter.QuoteName = false;
                    filterserializer.Serialize(filterWriter, filter);
                }
                string searchFilter = filterstringwriter.ToString();
                #endregion
                #region Request Settings
                var settings = new Settings
                   {
                       Client = client,
                       TestMode = Convert.ToBoolean(testMode),
                       Context = context,
                       AuditTransactions = false,
                       TimeOut = timeout
                   };
                var settingsserializer = new JsonSerializer();
                var settingsstringWriter = new StringWriter();
                using (var settingswriter = new JsonTextWriter(settingsstringWriter))
                {
                    settingswriter.QuoteName = false;
                    settingsserializer.Serialize(settingswriter, settings);
                }
                string searchSettings = settingsstringWriter.ToString();
                #endregion
                #region Request Criteria
                var Criteria = new Criteria
                    {
                        CheckIn = reformatDate(Req.Descendants("FromDate").FirstOrDefault().Value),
                        CheckOut = reformatDate(Req.Descendants("ToDate").FirstOrDefault().Value),
                        Hotels = HotelListForReq.ToArray(),
                        Occupancies = SearchRooms(Req.Descendants("Rooms").FirstOrDefault()),
                        Currency = currencyCode,
                        Language = "EN",
                        Market = Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value,
                        Nationality = Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value
                    };

                var serializer = new JsonSerializer();
                var stringWriter = new StringWriter();
                using (var writer = new JsonTextWriter(stringWriter))
                {
                    writer.QuoteName = false;
                    serializer.Serialize(writer, Criteria);
                }
                string searchCriteria = stringWriter.ToString();//JsonConvert.SerializeObject(criteria); 
                #endregion
                #region GraphQL Query
                string query = string.Empty;
                query += "query {\r\n  hotelX {\r\n search(criteria:" + searchCriteria + ", settings:" + searchSettings + ", filter:" + searchFilter + ") {\r\n auditData {\r\n transactions {\r\n request\r\n response\r\n}\r\n}\r\n errors {\r\n code\r\n  type\r\n description\r\n  }\r\n";
                query += "warnings {\r\n code\r\n type\r\n description\r\n}\r\n options {\r\n id\r\n accessCode\r\n supplierCode\r\n hotelCode\r\n  hotelName\r\n boardCode\r\n price {\r\n net\r\n currency\r\n }\r\n }\r\n}\r\n}\r\n}";
                #endregion
                dynamic Request = new ExpandoObject();
                Request.query = query;
                string json = JsonConvert.SerializeObject(Request);
                LogModel model = new LogModel
                {
                    TrackNo = Req.Descendants("TransID").FirstOrDefault().Value,
                    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                    Logtype = "Search",
                    LogtypeID = 1,
                    Supl_Id = supplierID
                };
                string response = tgr.serverRequest(json, model, url, apiKey);
                #endregion
                #region Response
                var jsonResponse = JsonConvert.DeserializeXmlNode(response, "Response");
                XElement supplierResponse = XElement.Parse(jsonResponse.InnerXml);
                if (supplierResponse.Descendants("hotelCode").Any())
                {
                    List<string> HotelCodes = supplierResponse.Descendants("hotelCode").Select(x => x.Value).Distinct().ToList();
                    StringBuilder hotelcode = new StringBuilder();
                    foreach (string s in HotelCodes)
                    {
                        if (hotelcode.Length == 0)
                            hotelcode.Append("\'" + s + "\'");
                        else
                            hotelcode.Append(",\'" + s + "\'");
                    }
                    currencyCode = supplierResponse.Descendants("options").FirstOrDefault().Descendants("price").FirstOrDefault().Element("currency").Value;

                    string requestContent = Req.Descendants("CountryCode").FirstOrDefault().Value + "_" + currencyCode;
                    string hotelCodeString = hotelcode.ToString();
                    DataTable StaticData = tgd.HotelDetails(hotelCodeString);
                    var HotelGroups = from Rooms in supplierResponse.Descendants("options")//.FirstOrDefault().Elements("element")
                                      group Rooms by Rooms.Element("hotelCode").Value;
                    
                    foreach (var Hotel in HotelGroups)
                    {
                        string hotelID = Hotel.Descendants("hotelCode").FirstOrDefault().Value;
                        var result = StaticData.AsEnumerable().Where(dt => dt.Field<string>("HotelID") == hotelID);
                        DataRow[] drow = result.ToArray();
                        if (drow.Count() > 0)
                        {
                            List<double> Prices = Hotel.Descendants("price").Select(x => Convert.ToDouble(x.Element("net").Value)).OrderBy(x => x).ToList();
                            double mPrice = Prices.Min();
                            DataRow dr = drow[0];
                            string img = hotelImage(dr["Images"].ToString());
                            string xmlouttype = string.Empty;
                            try
                            {
                                if (dmc == "SMYROOMS")
                                {
                                    xmlouttype = "false";
                                }
                                else
                                { xmlouttype = "true"; }
                            }
                            catch { }
                            #region Response XML
                            HotelList.Add(new XElement("Hotel",
                                                            new XElement("HotelID", hotelID),
                                                            new XElement("HotelName", dr["HotelName"].ToString()),
                                                            new XElement("PropertyTypeName", ""),
                                                            new XElement("CountryID", Req.Descendants("CountryID").FirstOrDefault().Value),
                                                            new XElement("CountryName", Req.Descendants("CountryName").FirstOrDefault().Value),
                                                            new XElement("CountryCode", Req.Descendants("CountryCode").FirstOrDefault().Value),
                                                            new XElement("CityId", Req.Descendants("CityID").FirstOrDefault().Value),
                                                            new XElement("CityCode", Req.Descendants("CityCode").FirstOrDefault().Value),
                                                            new XElement("CityName", Req.Descendants("CityName").FirstOrDefault().Value),
                                                            new XElement("AreaId"),
                                                            new XElement("AreaName"),
                                                            new XElement("RequestID", requestContent),
                                                            new XElement("Address", dr["Address"]),
                                                            new XElement("Location"),
                                                            new XElement("Description"),
                                                            new XElement("StarRating", dr["Star"]),
                                                            new XElement("MinRate", mPrice.ToString()),
                                                            new XElement("HotelImgSmall", img),
                                                            new XElement("HotelImgLarge", img),
                                                            new XElement("MapLink"),
                                                            new XElement("Longitude", dr["Longitude"].ToString()),
                                                            new XElement("Latitude", dr["Latitude"].ToString()),
                                                            new XElement("xmloutcustid", customerid),
                                                            new XElement("xmlouttype", xmlouttype),
                                                            new XElement("DMC", dmc),
                                                            new XElement("SupplierID", supplierID.ToString()),
                                                            new XElement("Currency", Hotel.Descendants("price").FirstOrDefault().Element("currency").Value),
                                                            new XElement("Offers"),
                                                            new XElement("Facilities", new XElement("Facility", "No Facility")),
                                                            new XElement("Rooms")));
                            #endregion
                        }
                    }
                }
                #endregion
            }
            catch(Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelSearch";
                ex1.PageName = "TGServices";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
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
                string hotelID = "\'" + Req.Descendants("HotelID").FirstOrDefault().Value + "\'";
                DataTable HotelDetails = tgd.SingleHotelDetails(hotelID);
                if (HotelDetails.Rows.Count > 0)
                {
                    XElement Images = new XElement("Images");
                    List<string> imageString = HotelDetails.Rows[0]["Images"].ToString().Split(new char[] { '#' }).ToList();
                    if (imageString.Count > 0)
                    {

                        foreach (string imageWithType in imageString)
                        {
                            if (!string.IsNullOrEmpty(imageWithType))
                            {
                                string[] image = imageWithType.Split(new char[] { '_' });
                                Images.Add(new XElement("Image",
                                            new XAttribute("Path", image[1]),
                                            new XAttribute("Caption", image[0])));
                            }
                        }
                    }
                    else
                        Images.Add(new XElement("Image",
                                        new XAttribute("Path", ""),
                                        new XAttribute("Caption", "")));
                    XElement Facilities = new XElement("Facilities");
                    List<string> FacilityList = HotelDetails.Rows[0]["Facilities"].ToString().Split(new char[] { '#' }).ToList();
                    if (FacilityList.Count > 0)
                    {
                        foreach (string facilityWithID in FacilityList)
                        {
                            if (!string.IsNullOrEmpty(facilityWithID))
                            {
                                string[] facility = facilityWithID.Split(new char[] { '_' });
                                Facilities.Add(new XElement("Facility", facility[1]));
                            }
                        }
                    }
                    StringBuilder Description = new StringBuilder();
                    XElement descList = XElement.Parse(HotelDetails.Rows[0]["Description"].ToString());
                    if (descList.HasElements)
                    {
                        foreach (XElement descr in descList.Descendants("texts").Where(x => x.Element("language").Value.ToUpper().Equals("EN")))
                        {
                            if (Description.Length == 0)
                                Description.Append(descr.Element("text").Value);
                            else
                                Description.Append(" \n" + descr.Element("text").Value);
                        }
                        if (Description.Length == 0)
                            Description.Append(descList.Descendants("text").FirstOrDefault().Value);
                    }
                    string desc = Description.ToString();
                    Response.Add(new XElement("Hotels",
                                    new XElement("Hotel",
                                        new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                        new XElement("Description", desc),
                                        Images,
                                        Facilities,
                                        new XElement("ContactDetails",
                                            new XElement("Phone", HotelDetails.Rows[0]["telephone"].ToString()),
                                            new XElement("Fax", HotelDetails.Rows[0]["fax"].ToString())),
                                        new XElement("CheckinTime"),
                                        new XElement("CheckoutTime"))));
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelDetail";
                ex1.PageName = "TGServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            XElement DetailsResponse = new XElement(xmlns + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", xmlns),
                                        new XElement(xmlns + "Header",
                                            new XElement("Authentication",
                                            new XElement("AgentID", AgentID),
                                            new XElement("Username", username),
                                            new XElement("Password", password),
                                            new XElement("ServiceType", ServiceType),
                                            new XElement("ServiceVersion", ServiceVersion))),
                                        new XElement(xmlns + "Body",
                                            Req.Descendants("hoteldescRequest").FirstOrDefault(),
                                            Response));
            return DetailsResponse;
        }
        #endregion
        #region Room Availability
        public XElement GetRoomAvail_smyroomOUT(XElement req, XElement SmyRoomsMealPlansfile)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            XElement getrm = null;
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "39").ToList();
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
                        dmc = "SMYROOMS";
                    }
                    TGServices tgs = new TGServices(39, customerid);
                    roomavailabilityresponse.Add(tgs.RoomAvailability(req, dmc, htlid, SmyRoomsMealPlansfile,customerid));
                }
                #endregion
                getrm = new XElement("TotalRooms", roomavailabilityresponse);
                return getrm;
            }
            catch { return null; }
        }
        public XElement RoomAvailability(XElement Req, string SupplierType, string htlID, XElement SmyRoomsMealPlansfile, string custIDs)
        {
            customerid = custIDs;
            SmyRoomsMealPlans = SmyRoomsMealPlansfile;
            dmc = SupplierType;
            hotelcode = htlID;
            XElement RoomResponse = new XElement("searchResponse");
            XElement AvailablilityResponse = null;
            try
            {
                #region Credentials
                string username = Req.Descendants("UserName").Single().Value;
                string password = Req.Descendants("Password").Single().Value;
                string AgentID = Req.Descendants("AgentID").Single().Value;
                string ServiceType = Req.Descendants("ServiceType").Single().Value;
                string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
                #endregion
                //#region Supplier Credentials
                //XElement SmyCredentials = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, supplierID.ToString());
                //string client = SmyCredentials.Element("Client").Value;
                //string testMode = SmyCredentials.Element("TestMode").Value;
                //string context = SmyCredentials.Element("Context").Value;
                //string url = SmyCredentials.Element("Url").Value;
                //string apiKey = SmyCredentials.Element("ApiKey").Value;
                //string accessCode = SmyCredentials.Element("AccessCode").Value;
                //#endregion
                #region Request
                #region Requset Filter Access
                var filter = new Access
                {
                    access = new Includes
                    {
                        includes = accessCode
                    },
                };
                var filterserializer = new JsonSerializer();
                var filterstringwriter = new StringWriter();
                using (var filterWriter = new JsonTextWriter(filterstringwriter))
                {
                    filterWriter.QuoteName = false;
                    filterserializer.Serialize(filterWriter, filter);
                }
                string searchFilter = filterstringwriter.ToString();
                #endregion
                #region Request Settings
                var settings = new Settings
                {
                    Client = client,
                    TestMode = Convert.ToBoolean(testMode),
                    Context = context,
                    AuditTransactions = false,
                    TimeOut = 24700
                };
                var settingsserializer = new JsonSerializer();
                var settingsstringWriter = new StringWriter();
                using (var settingswriter = new JsonTextWriter(settingsstringWriter))
                {
                    settingswriter.QuoteName = false;
                    settingsserializer.Serialize(settingswriter, settings);
                }
                string searchSettings = settingsstringWriter.ToString();
                #endregion
                #region ID for Room Mapping
                int id = 1;
                //foreach (XElement Room in Req.Descendants("RoomPax"))
                //{
                //    Room.Add(new XElement("id", id));
                //    id++;
                //}
                #endregion
                string[] splitContent = Req.Descendants("GiataHotelList").Where(x=>x.Attribute("GSupID").Value.Equals(supplierID.ToString())).FirstOrDefault()
                                            .Attribute("GRequestID").Value.Split(new char[] { '_' });
                currencyCode = splitContent[1];
                #region Request Criteria
                var Criteria = new Criteria
                {
                    CheckIn = reformatDate(Req.Descendants("FromDate").FirstOrDefault().Value),
                    CheckOut = reformatDate(Req.Descendants("ToDate").FirstOrDefault().Value),
                    Hotels = (Req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value.Equals(supplierID.ToString())).FirstOrDefault().Attribute("GHtlID").Value).Split(' ').ToArray(),
                    //Hotels = Req.Descendants("HotelID").Select(x => x.Value).Distinct().ToArray(),
                    Occupancies = SearchRooms(Req.Descendants("Rooms").FirstOrDefault()),
                    Currency = currencyCode,
                    Language = "EN",
                    Market = Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value,//"GB",//Req.Descendants("CountryCode").FirstOrDefault().Value,
                    Nationality = Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value
                };
                
                var serializer = new JsonSerializer();
                var stringWriter = new StringWriter();
                using (var writer = new JsonTextWriter(stringWriter))
                {
                    writer.QuoteName = false;
                    serializer.Serialize(writer, Criteria);
                }
                string searchCriteria = stringWriter.ToString();//JsonConvert.SerializeObject(criteria); 
                #endregion
                #endregion
                #region GraphQL Request
                StringBuilder Query = new StringBuilder();
                Query.Append("{\r\n hotelX {\r\n search(criteria:" + searchCriteria + " , settings:" + searchSettings + ",filter:" + searchFilter + " ) {\r\n ");
                //Query.Append("auditData {\r\n transactions {\r\n request\r\n response\r\n}\r\n}\r\n errors {\r\n code\r\n type\r\n description\r\n }\r\n warnings {\r\n code\r\n type\r\n description\r\n }\r\n");
                //Query.Append("options {\r\n id\r\n rateRules \r\n supplements{\r\n supplementType\r\n price{\r\n net\r\n currency\r\n}\r\n mandatory\r\n quantity\r\n name\r\n description\r\n effectiveDate\r\n expireDate\r\n}\r\n");
                //Query.Append("market\r\n rooms{\r\n code\r\n beds{\r\n type\r\n count\r\n description\r\n }\r\n units\r\n roomPrice{\r\n breakdown{\r\n price{\r\n net\r\n currency\r\n  }\r\n effectiveDate\r\n expireDate\r\n}\r\n}\r\n");
                //Query.Append("ratePlans{\r\n code\r\n name\r\n effectiveDate\r\n expireDate\r\n }\r\n refundable\r\n promotions{\r\n code\r\n name\r\n}\r\n description\r\n occupancyRefId\r\n }");
                //Query.Append("cancelPolicy{\r\n refundable\r\n cancelPenalties{\r\n value\r\n currency\r\n hoursBefore\r\n penaltyType\r\n}\r\n}\r\n accessCode\r\n supplierCode\r\n hotelCode\r\n hotelName\r\n boardCode\r\n price {\r\n net\r\n currency\r\n}}}}}");

                Query.Append("auditData {\r\n transactions {\r\n request\r\n response\r\n }\r\n}\r\n context\r\n options {\r\n surcharges {\r\n chargeType\r\n mandatory\r\n description\r\n price {\r\n currency\r\n  binding\r\n net\r\n gross\r\n ");
                Query.Append("exchange {\r\n currency\r\n rate\r\n}\r\n markups {\r\n channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n}\r\n}\r\n}\r\n accessCode\r\n supplierCode\r\n market\r\n ");
                Query.Append("hotelCode\r\n hotelName\r\n boardCode\r\n paymentType\r\n status\r\n occupancies {\r\n id\r\n paxes {\r\n age\r\n}\r\n}\r\n rooms {\r\n occupancyRefId\r\n code\r\n description\r\n refundable\r\n units\r\n roomPrice {\r\n ");
                Query.Append("price {\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n}\r\nmarkups {\r\n channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n}\r\n}\r\n}\r\n}\r\n ");
                Query.Append("beds {\r\n type\r\n description\r\n count\r\n shared\r\n }\r\n ratePlans {\r\n code\r\n name\r\n effectiveDate\r\n expireDate\r\n}\r\n promotions {\r\n code\r\n name\r\n effectiveDate\r\n expireDate\r\n}\r\n}\r\n ");
                Query.Append("price {\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n}\r\n markups {\r\n channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n}\r\n}\r\n}\r\n ");
                Query.Append("addOns {\r\n distribute\r\n }\r\n supplements {\r\n code\r\n name\r\n description\r\n supplementType\r\n chargeType\r\n mandatory\r\n durationType\r\n quantity\r\n unit\r\n effectiveDate\r\n expireDate\r\n resort {\r\n ");
                Query.Append("code\r\n name\r\n description\r\n }\r\n price {\r\n currency\r\n  binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n markups {\r\n channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n}\r\n}\r\n}\r\n}\r\n ");
                Query.Append("surcharges {\r\n chargeType\r\n description\r\n price {\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n markups {\r\n channel\r\n currency\r\n binding\r\n  net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n}\r\n}\r\n}\r\n}\r\n ");
                Query.Append("rateRules\r\n cancelPolicy {\r\n refundable\r\n cancelPenalties {\r\n hoursBefore\r\n penaltyType\r\n currency\r\n value\r\n }\r\n}\r\n remarks\r\n token\r\n id\r\n}\r\n errors {\r\n code\r\n type\r\n description\r\n }\r\n warnings {\r\n code\r\n type\r\n description\r\n }\r\n}\r\n}\r\n}");
                   
                dynamic Request = new ExpandoObject();
                Request.query = Query.ToString();
                string json = JsonConvert.SerializeObject(Request);
                LogModel model = new LogModel
                {
                    TrackNo = Req.Descendants("TransID").FirstOrDefault().Value,
                    CustomerID = Convert.ToInt32(customerid),
                    Logtype = "RoomAvail",
                    LogtypeID = 2,
                    Supl_Id = supplierID
                };
                #endregion
                #region Response
                string response = tgr.serverRequest(json, model, url, apiKey);
                var jsonResponse = JsonConvert.DeserializeXmlNode(response, "Response");
                XElement SupplierResponse = XElement.Parse(jsonResponse.InnerXml);
                if (SupplierResponse.Descendants("supplements").Any() && SupplierResponse.Descendants("supplements").FirstOrDefault().HasElements)
                {
                    string abc = "";
                }
                foreach (XElement Room in Req.Descendants("RoomPax"))
                {
                    Room.Add(new XElement("id", id));
                    id++;
                }
                XElement RoomTags = Rooms(Req, SupplierResponse);
                //SupplierResponse.Save(@"D:\Aman\TravelGateX\test.xml");
                #region Response XML
                var availabilityResponse = new XElement("Hotel",
                                                                         new XElement("HotelID", hotelcode),
                                                                         new XElement("HotelName"),
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
                                                                         new XElement("DMC", dmc),
                                                                         new XElement("SupplierID", supplierID.ToString()),
                                                                         new XElement("Currency"),
                                                                        new XElement("Offers"),
                                                                         new XElement("Facilities", new XElement("Facility", "No Facility available")),
                                                                         RoomTags);
                #endregion
                RoomResponse.Add(new XElement("Hotels", availabilityResponse));
                #region Response Format
                AvailablilityResponse = new XElement(
                                                    new XElement(xmlns + "Envelope",
                                                        new XAttribute(XNamespace.Xmlns + "soapenv", xmlns),
                                                        new XElement(xmlns + "Header",
                                                            new XElement("Authentication",
                                                                new XElement("AgentID", AgentID),
                                                                new XElement("Username", username),
                                                                new XElement("Password", password),
                                                                new XElement("ServiceType", ServiceType),
                                                                new XElement("ServiceVersion", ServiceVersion))),
                                                        new XElement(xmlns + "Body",
                                                            new XElement(removeAllNamespaces(Req.Descendants("searchRequest").FirstOrDefault())),
                                                            removeAllNamespaces(RoomResponse))));
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "RoomAvailability";
                ex1.PageName = "TGServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
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
            XElement cancelresponse = new XElement("HotelDetailwithcancellationResponse");
            try
            {
                #region Cancellation Policy From DB
                XElement Data = XmlData(Req.Descendants("TransID").FirstOrDefault().Value, 2, supplierID);
                XElement Hotel = Data.Descendants("options").Where(x => x.Element("id").Value.Equals(Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value)).FirstOrDefault();
                #endregion
                List<XElement> cxPolicies = new List<XElement>();
                XElement policyTag = Hotel.Descendants("cancelPolicy").FirstOrDefault();
                bool refundable = policyTag.Element("refundable").Value.ToUpper().Equals("TRUE") ? true : false;
                XElement Policies = new XElement("CancellationPolicies");
                int Nights = getNights(Req.Descendants("FromDate").FirstOrDefault().Value, Req.Descendants("ToDate").FirstOrDefault().Value);
                double amount = Convert.ToDouble(Hotel.Element("price").Element("net").Value);
                if (refundable)
                {
                    #region Refundable Polcies
                    foreach (XElement policy in policyTag.Descendants("cancelPenalties").ToList())
                    {
                        double pAmount = PolicyAmount(policy.Element("penaltyType").Value, policy.Element("value").Value, Nights, amount);
                        if (pAmount > 0)
                        {
                            DateTime lastCancellationDate = DateTime.ParseExact(Req.Descendants("FromDate").FirstOrDefault().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            int days = Convert.ToInt32(policy.Element("hoursBefore").Value) / 24;
                            int hours = Convert.ToInt32(policy.Element("hoursBefore").Value) % 24;
                            lastCancellationDate = lastCancellationDate.AddDays(-days);
                            lastCancellationDate = lastCancellationDate.AddHours(-hours);
                            cxPolicies.Add(new XElement("CancellationPolicy",
                                                    new XAttribute("LastCancellationDate", lastCancellationDate.ToString("dd/MM/yyyy")),
                                                    new XAttribute("ApplicableAmount", pAmount),
                                                    new XAttribute("NoShowPolicy", hours.Equals("0") ? "1" : "0")));
                        }
                    }
                    DateTime minDate = cxPolicies.Select(x => DateTime.ParseExact(x.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture)).Min();
                    cxPolicies.Add(new XElement("CancellationPolicy",
                                        new XAttribute("LastCancellationDate", minDate.AddDays(-1).ToString("dd/MM/yyyy")),
                                        new XAttribute("ApplicableAmount", "0"),
                                        new XAttribute("NoShowPolicy", "0")));
                    Policies.Add(cxPolicies);
                    #endregion
                }
                else
                {
                    #region Non-Refundable Policies
                    Policies.Add(new XElement("CancellationPolicy",
                                                    new XAttribute("LastCancellationDate", DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy")),
                                                    new XAttribute("ApplicableAmount", "0"),
                                                    new XAttribute("NoShowPolicy", "0")));
                    Policies.Add(new XElement("CancellationPolicy",
                                            new XAttribute("LastCancellationDate", DateTime.Now.ToString("dd/MM/yyyy")),
                                            new XAttribute("ApplicableAmount", amount.ToString()),
                                            new XAttribute("NoShowPolicy", "0")));
                    #endregion
                }
                if (Policies.HasElements)
                {
                    dmc = "SmyRooms";
                    Policies.Descendants("CancellationPolicy").OrderBy(x => DateTime.ParseExact(x.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture));
                    #region Response XML
                    var cxp = new XElement("Hotels",
                                          new XElement("Hotel",
                                              new XElement("HotelID", Hotel.Descendants("hotelCode").FirstOrDefault().Value),
                                              new XElement("HotelName"),
                                              new XElement("HotelImgSmall"),
                                              new XElement("HotelImgLarge"),
                                              new XElement("MapLink"),
                                              new XElement("DMC", dmc),
                                              new XElement("Currency", currencyCode),
                                              new XElement("Offers"),
                                              new XElement("Rooms",
                                              new XElement("Room",
                                                  new XAttribute("ID", ""),
                                                  new XAttribute("RoomType", ""),
                                                new XAttribute("MealPlanPrice", ""),
                                                new XAttribute("PerNightRoomRate", ""),
                                                new XAttribute("TotalRoomRate", ""),
                                                new XAttribute("CancellationDate", ""),
                                                Policies))));
                    cancelresponse.Add(cxp);
                    #endregion
                }
                else
                {
                    cancelresponse.Add(new XElement("ErrorTxt", "No Cancellation Policy found"));
                }

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancellationPolicy";
                ex1.PageName = "TGServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            #region Response Format
            XElement CXpResponse = new XElement(
                                                new XElement(xmlns + "Envelope",
                                                    new XAttribute(XNamespace.Xmlns + "soapenv", xmlns),
                                                    new XElement(xmlns + "Header",
                                                        new XElement("Authentication",
                                                            new XElement("AgentID", AgentID),
                                                            new XElement("Username", username),
                                                            new XElement("Password", password),
                                                            new XElement("ServiceType", ServiceType),
                                                            new XElement("ServiceVersion", ServiceVersion))),
                                                    new XElement(xmlns + "Body",
                                                        new XElement(Req.Descendants("hotelcancelpolicyrequest").Any() ? removeAllNamespaces(Req.Descendants("hotelcancelpolicyrequest").FirstOrDefault()) : removeAllNamespaces(Req)),
                                                       removeAllNamespaces(cancelresponse))));
            #endregion
            return CXpResponse;
        }
        #endregion
        #region Pre-Booking
        public XElement PreBook(XElement Req, string xmlout)
        {
            //if (xmlout.ToUpper().Equals("TRUE"))
            //    dmc = "HA";
            //else
            //    dmc = "SMYROOMS";
            dmc = xmlout;
            XElement Response = null;
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                //#region Supplier Credentials
                //XElement SmyCredentials = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, supplierID.ToString());
                //string client = SmyCredentials.Element("Client").Value;
                //string testMode = SmyCredentials.Element("TestMode").Value;
                //string context = SmyCredentials.Element("Context").Value;
                //string url = SmyCredentials.Element("Url").Value;
                //string apiKey = SmyCredentials.Element("ApiKey").Value;
                //string accessCode = SmyCredentials.Element("AccessCode").Value;
                //#endregion
                int cnt = 1;
                foreach (XElement room in Req.Descendants("RoomPax"))
                    room.Add(new XElement("id", cnt++));
                XElement preBookResponse = new XElement("HotelPreBookingResponse");
                var settings = new Settings
                {
                    Client = client,
                    TestMode = Convert.ToBoolean(testMode),
                    Context = context,
                    AuditTransactions = true,
                    TimeOut = 179700
                };
                var settingsSerializer = new JsonSerializer();
                var settingsStringWriter = new StringWriter();
                using (var settingsWriter = new JsonTextWriter(settingsStringWriter))
                {
                    settingsWriter.QuoteName = false;
                    settingsSerializer.Serialize(settingsWriter, settings);
                }
                string prebookSettings = settingsStringWriter.ToString();
                var criteria = new CriteriaQuote
                {
                    OptionRefID = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value,
                    Language = "EN"
                };
                var pbSerializer = new JsonSerializer();
                var pbStringWriter = new StringWriter();
                using (var pbWriter = new JsonTextWriter(pbStringWriter))
                {
                    pbWriter.QuoteName = false;
                    pbSerializer.Serialize(pbWriter, criteria);
                }
                string pbCriteria = pbStringWriter.ToString();
                StringBuilder Query = new StringBuilder();
                Query.Append("{\r\n hotelX {\r\n quote(criteria: " + pbCriteria + ", settings: " + prebookSettings + ") {\r\n auditData {\r\n transactions {\r\n request\r\n response\r\n}\r\n}\r\n optionQuote {\r\n optionRefId\r\n status\r\n price {\r\n currency\r\n binding\r\n net\r\n  gross}\r\n ");
                //Query.Append("errors {\r\n  code\r\n  description\r\n  }\r\n warnings {\r\n code\r\n  description\r\n }\r\n");
                Query.Append("surcharges {\r\n chargeType\r\n price {\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n}}\r\n");
                Query.Append("description}\r\n  cancelPolicy {\r\n refundable\r\n cancelPenalties {\r\n  hoursBefore\r\n  penaltyType\r\n currency\r\n value\r\n}}remarks}}}}");
                dynamic foo = new ExpandoObject();
                foo.query = Query.ToString();
                string json = JsonConvert.SerializeObject(foo);
                LogModel model = new LogModel
                {
                    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                    Logtype = "PreBook",
                    LogtypeID = 4,
                    Supl_Id = supplierID,
                    TrackNo = Req.Descendants("TransID").FirstOrDefault().Value
                };
                string jsonResponse = tgr.serverRequest(json, model, url, apiKey);
                var xml = JsonConvert.DeserializeXmlNode(jsonResponse, "Response");
                XElement supplierResponse = XElement.Parse(xml.InnerXml);
                if (supplierResponse.Descendants("status").FirstOrDefault().Value.ToUpper().Equals("OK"))
                {
                    preBookResponse.Add(new XElement("Hotels",
                                            new XElement("Hotel",
                                                new XElement("HotelID"),
                                                new XElement("HotelName"),
                                                new XElement("Status", "true"),
                                                //new XElement("NewPrice"),
                                                new XElement("TermCondition", supplierResponse.Descendants("remarks").FirstOrDefault().Value),
                                                new XElement("HotelImgSmall"),
                                                new XElement("HotelImgLarge"),
                                                new XElement("MapLink"),
                                                new XElement("DMC", dmc),
                                                new XElement("Currency"),
                                                new XElement("Offers"),
                                                PreBookingRooms(supplierResponse, Req))));
                }
                double oldprice = Convert.ToDouble(Req.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value);
                double newprice = Convert.ToDouble(preBookResponse.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value);
                bool priceChanged = false;
                if (oldprice != newprice)
                    priceChanged = true;

                Response = new XElement(xmlns + "Envelope",
                                           new XAttribute(XNamespace.Xmlns + "soapenv", xmlns),
                                           new XElement(xmlns + "Header",
                                               new XElement("Authentication",
                                                   new XElement("AgentID", AgentID),
                                                   new XElement("Username", username),
                                                   new XElement("Password", password),
                                                   new XElement("ServiceType", ServiceType),
                                                   new XElement("ServiceVersion", ServiceVersion))),
                                           new XElement(xmlns + "Body",
                                               new XElement(Req.Descendants("HotelPreBookingRequest").FirstOrDefault()),
                                               preBookResponse));
                if (priceChanged)
                {
                    Response.Descendants("HotelPreBookingResponse").Descendants("Hotels").FirstOrDefault()
                        .AddBeforeSelf(
                            new XElement("ErrorTxt", "Amount has been changed"),
                            new XElement("NewPrice", newprice.ToString())
                        );
                }
                else
                {
                    Response.Descendants("HotelPreBookingResponse").Descendants("Hotels").FirstOrDefault()
                        .AddBeforeSelf(new XElement("NewPrice", ""));
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBook";
                ex1.PageName = "TGServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
             return Response;
        }

        #region Pre-booking Rooms
        private object PreBookingRooms(XElement supplierResponse, XElement Req)
        {
            XElement RoomTag = new XElement("RoomTypes",
                            new XAttribute("Index", "1"),
                            new XAttribute("TotalRate",string.Empty));
            try
            {
                double amount = Convert.ToDouble(supplierResponse.Descendants("price").FirstOrDefault().Element("net").Value);
                double perRoomRate = Math.Round((amount / Req.Descendants("RoomPax").Count()), 2);
                int nights = getNights(Req.Descendants("FromDate").FirstOrDefault().Value, Req.Descendants("ToDate").FirstOrDefault().Value);
                RoomTag.Attribute("TotalRate").SetValue(amount.ToString());
                XElement PriceBreakup = new XElement("PriceBreakups");
                double perNightPrice = Math.Round(perRoomRate / nights, 2);
                for (int i = 0; i < nights; i++)
                {
                    PriceBreakup.Add(new XElement("Price",
                                                new XAttribute("Night", Convert.ToString(i + 1)),
                                                new XAttribute("PriceValue", perNightPrice.ToString())));
                }
                foreach (XElement room in Req.Descendants("Room"))
                {
                    XElement roomPax = Req.Descendants("RoomPax").Where(x => x.Element("id").Value.Equals(room.Attribute("RoomSeq").Value)).FirstOrDefault();
                    RoomTag.Add(new XElement("Room",
                                    new XAttribute("ID", room.Attribute("ID").Value),
                                    new XAttribute("SuppliersID", supplierID.ToString()),
                                    new XAttribute("RoomSeq", room.Attribute("RoomSeq").Value),
                                    new XAttribute("SessionID", supplierResponse.Descendants("optionRefId").FirstOrDefault().Value),
                                    new XAttribute("RoomType", room.Attribute("RoomType").Value),
                                    new XAttribute("OccupancyID", room.Attribute("OccupancyID").Value),
                                    new XAttribute("OccuapncyName", ""),
                                    new XAttribute("MealPlanID", room.Attribute("MealPlanID").Value),
                                    new XAttribute("MealPlanName", room.Attribute("MealPlanName").Value),
                                    new XAttribute("MealPlanCode", room.Attribute("MealPlanCode").Value),
                                    new XAttribute("MealPlanPrice", ""),
                                    new XAttribute("PerNightRoomRate", perNightPrice.ToString()),
                                    new XAttribute("TotalRoomRate", perRoomRate.ToString()),
                                    new XAttribute("CancellationDate", ""),
                                    new XAttribute("CancellationAmount", ""),
                                    new XAttribute("isAvailable", supplierResponse.Descendants("status").FirstOrDefault().Value.Equals("OK") ? "true" : "false"),
                                    new XElement("RequestID", supplierResponse.Descendants("optionRefId").FirstOrDefault().Value),
                                    new XElement("Offers"),
                                    new XElement("PromotionList", new XElement("Promotion")),
                                    new XElement("CancellationPolicy"),
                                    new XElement("Amenities", new XElement("Amenity")),
                                    new XElement("Images", new XElement("Image",
                                                                new XAttribute("Path", ""), new XAttribute("Caption", ""))),
                                    room.Descendants("Supplements").FirstOrDefault(),
                                    PriceBreakup,
                                    new XElement("AdultNum", roomPax.Element("Adult").Value),
                                    new XElement("ChildNum", roomPax.Element("Child").Value)));
                }
                RoomTag.Descendants("Room").LastOrDefault().AddAfterSelf(
                    PreBookCancellationPolicy(supplierResponse.Descendants("cancelPolicy").FirstOrDefault(),nights,amount,Req));
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBookingRooms";
                ex1.PageName = "TGServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            return new XElement("Rooms", RoomTag);
        } 
        #endregion
        #region Pre-booking Cancellation Policy
        public XElement PreBookCancellationPolicy(XElement policyTag, int Nights, double amount, XElement Req)
        {
            XElement Policies = new XElement("CancellationPolicies");
            List<XElement> cxPolicies = new List<XElement>();
            try
            {
                bool refundable = policyTag.Element("refundable").Value.ToUpper().Equals("TRUE") ? true : false;
                if (refundable)
                {
                    #region Refundable Polcies
                    foreach (XElement policy in policyTag.Descendants("cancelPenalties").ToList())
                    {
                        double pAmount = PolicyAmount(policy.Element("penaltyType").Value, policy.Element("value").Value, Nights, amount);
                        if (pAmount > 0)
                        {
                            DateTime lastCancellationDate = DateTime.ParseExact(Req.Descendants("FromDate").FirstOrDefault().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            int days = Convert.ToInt32(policy.Element("hoursBefore").Value) / 24;
                            int hours = Convert.ToInt32(policy.Element("hoursBefore").Value) % 24;
                            lastCancellationDate = lastCancellationDate.AddDays(-days);
                            lastCancellationDate = lastCancellationDate.AddHours(-hours);
                            cxPolicies.Add(new XElement("CancellationPolicy",
                                                    new XAttribute("LastCancellationDate", lastCancellationDate.ToString("dd/MM/yyyy")),
                                                    new XAttribute("ApplicableAmount", pAmount),
                                                    new XAttribute("NoShowPolicy", hours.Equals("0") ? "1" : "0")));
                        }
                    }
                    DateTime minDate = cxPolicies.Select(x => DateTime.ParseExact(x.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture)).Min();
                    cxPolicies.Add(new XElement("CancellationPolicy",
                                        new XAttribute("LastCancellationDate", minDate.AddDays(-1).ToString("dd/MM/yyyy")),
                                        new XAttribute("ApplicableAmount", "0"),
                                        new XAttribute("NoShowPolicy", "0")));
                    Policies.Add(cxPolicies);
                    #endregion
                }
                else
                {
                    #region Non-Refundable Policies
                    Policies.Add(new XElement("CancellationPolicy",
                                                    new XAttribute("LastCancellationDate", DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy")),
                                                    new XAttribute("ApplicableAmount", "0"),
                                                    new XAttribute("NoShowPolicy", "0")));
                    Policies.Add(new XElement("CancellationPolicy",
                                            new XAttribute("LastCancellationDate", DateTime.Now.ToString("dd/MM/yyyy")),
                                            new XAttribute("ApplicableAmount", amount.ToString()),
                                            new XAttribute("NoShowPolicy", "0")));
                    #endregion
                }
                return Policies;
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBookCancellationPolicy";
                ex1.PageName = "TGServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
                Policies.Add(new XElement("CancellationPolicy",
                                                   new XAttribute("LastCancellationDate", DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy")),
                                                   new XAttribute("ApplicableAmount", "0"),
                                                   new XAttribute("NoShowPolicy", "0")));
                Policies.Add(new XElement("CancellationPolicy",
                                        new XAttribute("LastCancellationDate", DateTime.Now.ToString("dd/MM/yyyy")),
                                        new XAttribute("ApplicableAmount", amount.ToString()),
                                        new XAttribute("NoShowPolicy", "0")));
                return Policies;
            }
        }
        #endregion
        #endregion
        #region Booking
        public XElement Booking(XElement Req)
        {
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            XElement BookingResponse = new XElement("HotelBookingResponse");
            try
            {
                //#region Supplier Credentials
                //XElement SmyCredentials = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, supplierID.ToString());
                //string client = SmyCredentials.Element("Client").Value;
                //string testMode = SmyCredentials.Element("TestMode").Value;
                //string context = SmyCredentials.Element("Context").Value;
                //string url = SmyCredentials.Element("Url").Value;
                //string apiKey = SmyCredentials.Element("ApiKey").Value;
                //string accessCode = SmyCredentials.Element("AccessCode").Value;
                //#endregion
                #region Request Inputs
                XElement leadGuest = Req.Descendants("Room").FirstOrDefault().Descendants("PaxInfo").Where(x => x.Element("IsLead").Value.ToUpper().Equals("TRUE")).FirstOrDefault();
                var leadPax = new Holder
                {
                    FirstName = leadGuest.Element("FirstName").Value,
                    LastName = leadGuest.Element("LastName").Value
                };
                //var delta = new DeltaPrice
                //{
                //    Amount = 0,
                //    PercentageAmount = 0,
                //    ApplyBoth = true
                //};
                var bookInput = new BookInput
                {
                    OptionReferenceID = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value,
                    ClientReference = Req.Descendants("TransactionID").FirstOrDefault().Value,
                    //DeltaPrice = delta,
                    LeadPax = leadPax,
                    specialRequest = Req.Descendants("SpecialRemarks").FirstOrDefault().Value,
                    Rooms = bookingReqRooms(Req)
                };
                var bookingStringWriter = new StringWriter();
                var bookingSerializer = new JsonSerializer();
                using (var bookingWriter = new JsonTextWriter(bookingStringWriter))
                {
                    bookingWriter.QuoteName = false;
                    bookingSerializer.Serialize(bookingWriter, bookInput);
                }
                string bookingInput = bookingStringWriter.ToString();
                var settings = new Settings
               {
                   Client = client,
                   TestMode = Convert.ToBoolean(testMode),
                   Context = context,
                   AuditTransactions = true,
                   TimeOut = 179700
               };
                var settingsSerializer = new JsonSerializer();
                var settingsStringWriter = new StringWriter();
                using (var settingsWriter = new JsonTextWriter(settingsStringWriter))
                {
                    settingsWriter.QuoteName = false;
                    settingsSerializer.Serialize(settingsWriter, settings);
                }
                string bookSettings = settingsStringWriter.ToString();
                #endregion
                #region GraphQL Query
                StringBuilder Query = new StringBuilder();
                Query.Append("mutation { \r\n  hotelX {\r\n book(input: " + bookingInput + ", settings: " + bookSettings + ") {\r\n auditData {\r\n transactions {\r\n request\r\n response\r\n}\r\n}\r\n booking {\r\n price \r\n{  currency\r\n  binding\r\n net\r\n  gross\r\n exchange {\r\n  currency\r\n ");
                Query.Append("rate\r\n }\r\n markups {\r\n channel\r\n  currency\r\n binding\r\n net\r\n gross\r\n  exchange {\r\n  currency\r\n  rate\r\n}\r\n }\r\n}\r\nstatus\r\n remarks\r\n reference {\r\n client\r\n supplier\r\n}\r\n holder {\r\n name\r\n surname\r\n}");
                Query.Append("hotel {\r\n creationDate\r\n  checkIn\r\n checkOut\r\n hotelCode\r\n hotelName\r\n boardCode\r\n occupancies {\r\n id\r\n  paxes {\r\n age\r\n}\r\n}\r\n rooms {\r\n code\r\n description\r\n occupancyRefId\r\n");
                Query.Append(" price {\r\ncurrency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n  rate\r\n }\r\n markups {\r\n channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n}\r\n}\r\n}\r\n}\r\n}\r\n}");
                Query.Append("errors {\r\n code\r\n type\r\n description\r\n}\r\n warnings {\r\ncode\r\n type\r\n description\r\n}\r\n}\r\n}\r\n}");
                dynamic Request = new ExpandoObject();
                Request.query = Query.ToString();
                string jsonRequest = JsonConvert.SerializeObject(Request);
                LogModel model = new LogModel
                {
                    TrackNo = Req.Descendants("TransactionID").FirstOrDefault().Value,
                    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                    Logtype = "Book",
                    LogtypeID = 5,
                    Supl_Id = supplierID
                };
                string response = tgr.serverRequest(jsonRequest, model, url, apiKey);
                var jsonResponse = JsonConvert.DeserializeXmlNode(response, "Response");
                XElement SupplierResponse = XElement.Parse(jsonResponse.InnerXml);
                #endregion
                #region Response XML
                #region Check booking status and cancel if not OK
                string jsonRes = CheckBooking(Req.Descendants("TransactionID").FirstOrDefault().Value, SupplierResponse.Descendants("reference").FirstOrDefault().Element("supplier").Value, Req.Descendants("HotelID").FirstOrDefault().Value, Req.Descendants("CurrencyCode").FirstOrDefault().Value, bookSettings, model);
                var res = JsonConvert.DeserializeXmlNode(jsonRes, "Response");
                if (res != null)
                {
                    XElement readBookSuplResponse = XElement.Parse(res.InnerXml);
                    if (readBookSuplResponse.Descendants("status").FirstOrDefault().Value.Equals("OK"))
                    {
                        double amount = 0;
                        try
                        {
                            amount = Convert.ToDouble(readBookSuplResponse.Descendants("net").FirstOrDefault().Value);
                        }
                        catch { }
                        BookingResponse.Add(new XElement("Hotels",
                                                new XElement("Hotel",
                                                    new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                                    new XElement("HotelName", Req.Descendants("HotelName").FirstOrDefault().Value),
                                                    new XElement("FromDate", Req.Descendants("FromDate").FirstOrDefault().Value),
                                                    new XElement("ToDate", Req.Descendants("ToDate").FirstOrDefault().Value),
                                                    new XElement("AdultPax", Req.Descendants("Rooms").Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value),
                                                    new XElement("ChildPax", Req.Descendants("Rooms").Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value),
                                                    new XElement("TotalPrice", amount.ToString()),
                                                    new XElement("CurrencyID"),
                                                    new XElement("CurrencyCode"),
                                                    new XElement("MarketID"),
                                                    new XElement("MarketName"),
                                                    new XElement("HotelImgSmall"),
                                                    new XElement("HotelImgLarge"),
                                                    new XElement("MapLink"),
                                                    new XElement("VoucherRemark", readBookSuplResponse.Descendants("bookings").FirstOrDefault().Element("remarks") == null ? "" : readBookSuplResponse.Descendants("bookings").FirstOrDefault().Element("remarks").Value),
                                                    new XElement("TransID", Req.Descendants("TransactionID").FirstOrDefault().Value),
                                                    new XElement("ConfirmationNumber", readBookSuplResponse.Descendants("reference").FirstOrDefault().Element("supplier").Value),
                                                    new XElement("Status", "Success"),
                                                    new XElement("PassengerDetail",
                                                    bookingRespRooms(Req.Descendants("PassengersDetail").FirstOrDefault(), amount, Req.Descendants("HotelID").FirstOrDefault().Value)))));
                    }
                    else
                    {
                        #region Cancel booking if status is not OK
                        if (readBookSuplResponse.Descendants("warnings").Descendants("description").Count() > 0)
                        {
                            if (readBookSuplResponse.Descendants("warnings").Descendants("description").FirstOrDefault().Value.Contains("Booking not found"))
                            {
                                BookingResponse.Add(new XElement("ErrorTxt", "Process_Error: Booking not found"));
                            }
                            else
                            {
                                BookingResponse.Add(new XElement("ErrorTxt", "Process_Error"));
                            }
                        }
                        else
                        {

                            string jsonCxlRes = CancelCheckedBooking(Req.Descendants("TransactionID").FirstOrDefault().Value, SupplierResponse.Descendants("reference").FirstOrDefault().Element("supplier").Value, Req.Descendants("HotelID").FirstOrDefault().Value, Req.Descendants("CurrencyCode").FirstOrDefault().Value, bookSettings, model);
                            var cxlRes = JsonConvert.DeserializeXmlNode(jsonCxlRes, "Response");
                            if (cxlRes != null)
                            {
                                XElement readCxlSuplResponse = XElement.Parse(cxlRes.InnerXml);
                                if (readCxlSuplResponse.Descendants("status").FirstOrDefault().Value.Equals("CANCELLED"))
                                {
                                    BookingResponse.Add(new XElement("ErrorTxt", "Booking status received as " + readBookSuplResponse.Descendants("status").FirstOrDefault().Value));
                                }
                                else
                                {
                                    BookingResponse.Add(new XElement("ErrorTxt", "Booking status received as " + readBookSuplResponse.Descendants("status").FirstOrDefault().Value + ". Booking cancellation failed."));

                                }
                            }
                            else
                            {
                                BookingResponse.Add(new XElement("ErrorTxt", "Booking status received as " + readBookSuplResponse.Descendants("status").FirstOrDefault().Value + ". Booking cancellation failed."));
                            }
                        }
                        #endregion
                    }
                }
                else
                {
                    BookingResponse.Add(new XElement("ErrorTxt", "Booking status received as " + SupplierResponse.Descendants("status").FirstOrDefault().Value));
                }
                #endregion

                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "Booking";
                ex1.PageName = "TGServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransactionID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
                BookingResponse.Add(new XElement("ErrorTxt", "Process Error"));

            }
            #region Response Format
            XElement Response = new XElement(xmlns + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", xmlns),
                                        new XElement(xmlns + "Header",
                                            new XElement("Authentication",
                                                new XElement("AgentID", AgentID),
                                                new XElement("Username", username),
                                                new XElement("Password", password),
                                                new XElement("ServiceType", ServiceType),
                                                new XElement("ServiceVersion", ServiceVersion))),
                                        new XElement(xmlns + "Body",
                                            new XElement(Req.Descendants("HotelBookingRequest").FirstOrDefault()),
                                            BookingResponse));       
            #endregion 
            return Response;
        }
        #endregion
        #region Booking Cancellation
        public XElement CancelBooking(XElement Req)
        {
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;            
            #endregion
            XElement Cancellation = null;
            XElement CancelResponse = new XElement("HotelCancellationResponse");
            try
            {
                //#region Supplier Credentials
                //XElement SmyCredentials = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, supplierID.ToString());
                //string client = SmyCredentials.Element("Client").Value;
                //string testMode = SmyCredentials.Element("TestMode").Value;
                //string context = SmyCredentials.Element("Context").Value;
                //string url = SmyCredentials.Element("Url").Value;
                //string apiKey = SmyCredentials.Element("ApiKey").Value;
                //string accessCode = SmyCredentials.Element("AccessCode").Value;
                //#endregion
                CancelReference reference = new CancelReference
                {
                    ClientReference = Req.Descendants("TransID").FirstOrDefault().Value,
                    SupplierReference = Req.Descendants("ConfirmationNumber").FirstOrDefault().Value
                };
                CancelInput cancelInput = new CancelInput
                {
                    AccessCode = accessCode,
                    Language = "EN",
                    HotelID = Req.Descendants("BookingCode").FirstOrDefault().Value,
                    ReferenceIds = reference
                };
                var cancelStringWriter = new StringWriter();
                var cancelSerializer = new JsonSerializer();
                using (var cancelWriter = new JsonTextWriter(cancelStringWriter))
                {
                    cancelWriter.QuoteName = false;
                    cancelSerializer.Serialize(cancelWriter, cancelInput);
                }
                Settings settings = new Settings
                {
                    Client = client,
                    TestMode = Convert.ToBoolean(testMode),
                    Context = context,
                    AuditTransactions = true,
                    TimeOut = 179700
                };
                var settingsStringWriter = new StringWriter();
                var settingsSerializer = new JsonSerializer();
                using (var settingsWriter = new JsonTextWriter(settingsStringWriter))
                {
                    settingsWriter.QuoteName = false;
                    settingsSerializer.Serialize(settingsWriter, settings);
                }
                string cancellationInput = cancelStringWriter.ToString();
                string cancellationSettings = settingsStringWriter.ToString();
                StringBuilder Query = new StringBuilder();
                Query.Append("mutation \r\n { hotelX { \r\n cancel(input: " + cancellationInput + ", settings: " + cancellationSettings + ") {\r\n ");
                Query.Append("auditData {\r\n transactions {\r\n request\r\n response\r\n timeStamp\r\n }\r\n }\r\n cancellation {\r\n reference {\r\n client\r\n supplier\r\n }\r\n cancelReference\r\n status\r\n price {\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n ");
                Query.Append("currency\r\n rate\r\n }\r\n markups {\r\n channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n}\r\n}\r\n booking {\r\n reference {\r\n client\r\n supplier\r\n }\r\n holder {\r\n name\r\n surname\r\n }\r\n ");
                Query.Append("hotel {\r\n creationDate\r\n checkIn\r\n checkOut\r\n hotelCode\r\n hotelName\r\n boardCode\r\n occupancies {\r\n id\r\n paxes {\r\n age\r\n }\r\n}\r\n rooms {\r\n occupancyRefId\r\n code\r\n description\r\n price {\r\n currency\r\n binding\r\n net\r\n gross\r\n ");
                Query.Append("exchange {\r\n currency\r\n rate\r\n }\r\n markups {\r\n channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n}\r\n}\r\n}\r\n}\r\n price {\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n markups {\r\n ");
                Query.Append("channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n }\r\n }\r\n cancelPolicy {\r\n refundable\r\n cancelPenalties {\r\n hoursBefore\r\n penaltyType\r\n currency\r\n  value\r\n}\r\n}\r\n remarks\r\n status\r\n payable\r\n}\r\n}\r\n}\r\n}\r\n}");
                dynamic Request = new ExpandoObject();
                Request.query = Query.ToString();
                string jsonReq = JsonConvert.SerializeObject(Request);
                LogModel model = new LogModel
                {
                    TrackNo = Req.Descendants("TransID").FirstOrDefault().Value,
                    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                    Supl_Id = supplierID,
                    Logtype = "Cancel",
                    LogtypeID = 6
                };
                string jsonResponse = tgr.serverRequest(jsonReq, model, url, apiKey);
                var response = JsonConvert.DeserializeXmlNode(jsonResponse, "Response");
                XElement supplierResponse = XElement.Parse(response.InnerXml);
                //supplierResponse.Save(@"D:\Aman\TravelGateX\CancelResponse.xml");
                if (supplierResponse.Descendants("status").Any() && supplierResponse.Descendants("status").FirstOrDefault().Value.ToUpper().Equals("CANCELLED"))
                {
                    double cxAmount = Convert.ToDouble(supplierResponse.Descendants("net").FirstOrDefault().Value);
                    CancelResponse.Add(new XElement("Rooms",
                                            new XElement("Room",
                                                new XElement("Cancellation",
                                                    new XElement("Amount", cxAmount.ToString()),
                                                    new XElement("Status", "Success")))));
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancelBooking";
                ex1.PageName = "TGServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            Cancellation = new XElement(xmlns + "Envelope",
                                           new XAttribute(XNamespace.Xmlns + "soapenv", xmlns),
                                           new XElement(xmlns + "Header",
                                               new XElement("Authentication",
                                                   new XElement("AgentID", AgentID),
                                                   new XElement("Username", username),
                                                   new XElement("Password", password),
                                                   new XElement("ServiceType", ServiceType),
                                                   new XElement("ServiceVersion", ServiceVersion))),
                                           new XElement(xmlns + "Body",
                                               new XElement(Req.Descendants("HotelCancellationRequest").FirstOrDefault()),
                                               CancelResponse));
            return Cancellation;
        }
        #endregion

        #region Check booking status
        public string CheckBooking(string transId, string suplReference, string hotelCode, string CurrencyCode, string bokSetting, LogModel model)
        {
            string jsonRes = string.Empty;
            try
            {
                List<CancelReference> RefList = new List<CancelReference>();
                CancelReference reference = new CancelReference
                {
                    ClientReference = transId,
                    SupplierReference = suplReference

                };
                RefList.Add(reference);
                var bookListRefs = new BookingListReferences
                {
                    Currency = CurrencyCode,
                    HotelCode = hotelCode,
                    ReferenceIds = RefList

                };
                BookingListCriteria BkCriteria = new BookingListCriteria
                {
                    AccessCode = accessCode,
                    language = "en",
                    References = bookListRefs,
                    typeSearch = "REFERENCES"

                };
                var serializer = new JsonSerializer();
                var stringWriter = new StringWriter();
                using (var writer = new JsonTextWriter(stringWriter))
                {
                    writer.QuoteName = false;
                    serializer.Serialize(writer, BkCriteria);
                }
                string bokListCriteria = stringWriter.ToString();
                StringBuilder BokListQuery = new StringBuilder();
                BokListQuery.Append("query {\r\n hotelX {\r\n booking(criteria: " + bokListCriteria + ", settings: " + bokSetting + ") {\r\n ");
                BokListQuery.Append("auditData{\r\n transactions{\r\n request\r\n request\r\n}\r\n timeStamp\r\n}\r\n bookings {\r\n reference {\r\n client\r\n supplier\r\n }\r\n holder {\r\n name\r\n surname\r\n}\r\n status\r\n hotel {\r\n creationDate\r\n ");
                BokListQuery.Append("checkIn\r\n checkOut\r\n hotelCode\r\n hotelName\r\n boardCode\r\n occupancies { \r\n id\r\n paxes {\r\n age\r\n}\r\n}\r\n rooms {\r\n occupancyRefId\r\n code\r\n description\r\n price {\r\n ");
                BokListQuery.Append("currency\r\n binding\r\n net\r\n gross\r\n exchange { \r\n currency\r\n rate\r\n}\r\n markups {\r\n channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n}\r\n}\r\n}\r\n} ");
                BokListQuery.Append("price {\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n}\r\n markups {\r\n channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n}\r\n} ");
                BokListQuery.Append("cancelPolicy {\r\n refundable\r\n cancelPenalties {\r\n hoursBefore\r\n penaltyType\r\n currency\r\n value\r\n}\r\n}\r\n remarks\r\n status\r\n payable\r\n} ");
                BokListQuery.Append("errors {\r\n code\r\n type\r\n description\r\n }\r\n warnings {\r\n code\r\n description\r\n type\r\n}\r\n}\r\n}\r\n}");
                dynamic bkLstRequest = new ExpandoObject();
                bkLstRequest.query = BokListQuery.ToString();
                model.Logtype = "CheckBooking";
                model.LogtypeID = 8;
                string jsonReq = JsonConvert.SerializeObject(bkLstRequest);
                jsonRes = tgr.serverRequest(jsonReq, model, url, apiKey);
                
            }
            catch (Exception ex)
            { 
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckedBooking";
                ex1.PageName = "TGServices";
                ex1.CustomerID = model.CustomerID.ToString();
                ex1.TranID = transId;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            return jsonRes;
        }
        #endregion

        #region Cancel booking with non OK status

        public string CancelCheckedBooking(string transId, string suplReference, string hotelCode, string CurrencyCode, string Settings, LogModel model)
        {
            string jsonRes = string.Empty;
            try
            {
                CancelReference reference = new CancelReference
                {
                    ClientReference = transId,
                    SupplierReference = suplReference
                };
                CancelInput cancelInput = new CancelInput
                {
                    AccessCode = accessCode,
                    Language = "en",
                    HotelID = hotelCode,
                    ReferenceIds = reference
                };
                var cancelStringWriter = new StringWriter();
                var cancelSerializer = new JsonSerializer();
                using (var cancelWriter = new JsonTextWriter(cancelStringWriter))
                {
                    cancelWriter.QuoteName = false;
                    cancelSerializer.Serialize(cancelWriter, cancelInput);
                }
                string cancellationInput = cancelStringWriter.ToString();
                StringBuilder Query = new StringBuilder();
                Query.Append("mutation \r\n { hotelX { \r\n cancel(input: " + cancellationInput + ", settings: " + Settings + ") {\r\n ");
                Query.Append("auditData {\r\n transactions {\r\n request\r\n response\r\n timeStamp\r\n }\r\n }\r\n cancellation {\r\n reference {\r\n client\r\n supplier\r\n }\r\n cancelReference\r\n status\r\n price {\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n ");
                Query.Append("currency\r\n rate\r\n }\r\n markups {\r\n channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n}\r\n}\r\n booking {\r\n reference {\r\n client\r\n supplier\r\n }\r\n holder {\r\n name\r\n surname\r\n }\r\n ");
                Query.Append("hotel {\r\n creationDate\r\n checkIn\r\n checkOut\r\n hotelCode\r\n hotelName\r\n boardCode\r\n occupancies {\r\n id\r\n paxes {\r\n age\r\n }\r\n}\r\n rooms {\r\n occupancyRefId\r\n code\r\n description\r\n price {\r\n currency\r\n binding\r\n net\r\n gross\r\n ");
                Query.Append("exchange {\r\n currency\r\n rate\r\n }\r\n markups {\r\n channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n}\r\n}\r\n}\r\n}\r\n price {\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n markups {\r\n ");
                Query.Append("channel\r\n currency\r\n binding\r\n net\r\n gross\r\n exchange {\r\n currency\r\n rate\r\n }\r\n }\r\n }\r\n cancelPolicy {\r\n refundable\r\n cancelPenalties {\r\n hoursBefore\r\n penaltyType\r\n currency\r\n  value\r\n}\r\n}\r\n remarks\r\n status\r\n payable\r\n}\r\n}\r\n}\r\n}\r\n}");
                dynamic Request = new ExpandoObject();
                Request.query = Query.ToString();
                string jsonReq = JsonConvert.SerializeObject(Request);
                model.LogtypeID = 6;
                model.Logtype = "Cancel";
                jsonRes = tgr.serverRequest(jsonReq, model, url, apiKey);

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancelCheckedBooking";
                ex1.PageName = "TGServices";
                ex1.CustomerID = model.CustomerID.ToString();
                ex1.TranID = transId;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            
            }
            return jsonRes;
        }
        #endregion

        #region Common Functions
        public string reformatDate(string date)
        {
            DateTime dt = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            return dt.ToString("yyyy-MM-dd");
        }
        #region Rooms For Hotel Search
        public Occupancy[] SearchRooms(XElement Rooms)
        {
            var occupancyList = new List<Occupancy>();
            foreach (XElement room in Rooms.Descendants("RoomPax"))
            {
                int adults = Convert.ToInt32(room.Element("Adult").Value);
                Occupancy occupancy = new Occupancy();
                int i = 0;
                for (i = 0; i < adults; i++)
                {
                    occupancy.Paxes.Add(new Pax { Age = 30 });
                }
                int[] childAges = room.Descendants("ChildAge").Select(x => Convert.ToInt32(x.Value)).ToArray();
                for (int j = 0; j < childAges.Count(); j++)
                {
                    i++;
                    occupancy.Paxes.Add(new Pax { Age = childAges[j] });
                }
                //occupancy.Paxes.Where(x => x != null).ToArray();
                occupancyList.Add(occupancy);
            }
            return occupancyList.ToArray();
        }
        #endregion
        #region Image for search
        public string hotelImage(string imgString)
        {
            string[] images = imgString.Split(new char[] { '#' });
            for (int i = 0; i < images.Length; i++)
            {
                string[] splitImageString = images[i].Split(new char[] { '_' });
                if (splitImageString[0].ToUpper().Equals("GENERAL") && !string.IsNullOrEmpty(splitImageString[1]))
                    return splitImageString[1];
            }
            return string.IsNullOrEmpty(imgString) ? string.Empty : images[0].Split(new char[] { '_' })[1];
        }
        #endregion
        #region Room Tags
        public XElement Rooms(XElement Req, XElement SuplResp)
        {
            XElement RoomTag = new XElement("Rooms");
            try
            {
                XElement MealPlans = SmyRoomsMealPlans; // XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/SmyRooms/Smy_Meals.xml"));
                int index = 1;
                foreach (XElement Option in SuplResp.Descendants("options"))
                {
                    if (Option.Descendants("breakdown").Any() && Option.Descendants("breakdown").FirstOrDefault().HasElements)
                    {
                        bool a = true;
                    }
                    string boardCode = Option.Descendants("boardCode").FirstOrDefault().Value;
                    XElement RoomType = new XElement("RoomTypes",
                                            new XAttribute("TotalRate", Option.Element("price").Element("net").Value),
                                            new XAttribute("HtlCode", hotelcode),
                                            new XAttribute("CrncyCode", currencyCode),
                                            new XAttribute("DMCType", dmc),
                                            new XAttribute("CUID", customerid),
                                            new XAttribute("Index", Convert.ToString(index++)));
                    try
                    {
                        double optionPrice = Convert.ToDouble(Option.Element("price").Element("net").Value);
                        int cnt = 1, roomCount = Req.Descendants("RoomPax").Count();
                        double perRoomRate = Math.Round(optionPrice / roomCount, 2);
                        int nights = getNights(Req.Descendants("FromDate").FirstOrDefault().Value, Req.Descendants("ToDate").FirstOrDefault().Value);
                        XElement PriceBreakup = new XElement("PriceBreakups");
                        double perNightPrice = Math.Round(perRoomRate / nights, 2);
                        for (int i = 0; i < nights; i++)
                        {
                            PriceBreakup.Add(new XElement("Price",
                                                        new XAttribute("Night", Convert.ToString(i + 1)),
                                                        new XAttribute("PriceValue", perNightPrice.ToString())));
                        }
                        foreach (XElement room in Option.Descendants("rooms"))
                        {
                            XElement roomRequested = Req.Descendants("RoomPax").Where(x => x.Element("id").Value.Equals(room.Descendants("occupancyRefId").FirstOrDefault().Value)).FirstOrDefault();
                            RoomType.Add(new XElement("Room",
                                                           new XAttribute("ID", room.Element("code").Value),
                                                              new XAttribute("SuppliersID", supplierID.ToString()),
                                                              new XAttribute("RoomSeq", Convert.ToString(cnt++)),
                                                              new XAttribute("SessionID", Option.Element("id").Value),
                                                              new XAttribute("RoomType", room.Descendants("description").FirstOrDefault().Value),
                                                              new XAttribute("OccupancyID", room.Descendants("occupancyRefId").FirstOrDefault().Value),
                                                              new XAttribute("OccupancyName", ""),
                                                              MealDetails(MealPlans, boardCode),
                                                              new XAttribute("PerNightRoomRate", perNightPrice.ToString()),
                                                              new XAttribute("TotalRoomRate", perRoomRate.ToString()),
                                                              new XAttribute("CancellationDate", ""),
                                                              new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", "true"),
                                                              new XElement("RequestID", Req.Descendants("RequestID").FirstOrDefault().Value + "_"+hotelcode),
                                                              new XElement("Offers"),
                                                              Promotion(room, Req.Descendants("FromDate").FirstOrDefault().Value),
                                                              new XElement("CancellationPolicy"),
                                                              new XElement("Amenities",
                                                                  new XElement("Amenity")),
                                                              new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                              new XElement("Supplements", null),
                                                              PriceBreakup,
                                                              new XElement("AdultNum", roomRequested.Element("Adult").Value),
                                                              new XElement("ChildNum", roomRequested.Element("Child").Value)));
                        }
                        RoomTag.Add(RoomType);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "Rooms";
                ex1.PageName = "TGServices";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            return RoomTag;
        }
        #endregion
        #region Calculate Number of Nights
        public int getNights(string startDate, string endDate)
        {
            DateTime start = DateTime.ParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime end = DateTime.ParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            return end.Subtract(start).Days;
        }
        #endregion
        #region Meal Plan Details
        public List<XAttribute> MealDetails(XElement Meals, string boardCode)
        {
            List<XAttribute> MealPlan = new List<XAttribute>();
            string mpName=string.Empty,mpcode=string.Empty;
            if (Meals!=null)
            {
                XElement meal = Meals.Descendants("Meal").Where(x => x.Attribute("Code").Value.Equals(boardCode)).FirstOrDefault();
                mpName = meal.Attribute("Name").Value;
                mpcode = string.IsNullOrEmpty(meal.Attribute("B2BCode").Value) ? meal.Attribute("Code").Value : meal.Attribute("B2BCode").Value;
            }
            MealPlan.Add(new XAttribute("MealPlanID", ""));
            MealPlan.Add(new XAttribute("MealPlanName", mpName));
            MealPlan.Add(new XAttribute("MealPlanCode", mpcode));
            MealPlan.Add(new XAttribute("MealPlanPrice", ""));
            return MealPlan;
        }
        #endregion
        #region Promotions
        public XElement Promotion(XElement room, string checkin)
        {
            XElement promotions = new XElement("PromotionList");
            DateTime checkinDate = DateTime.ParseExact(checkin, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            foreach (XElement promo in room.Descendants("promotions"))
            {
                if (promo.HasElements)
                {
                    bool validity = true;
                    string effectiveDate = promo.Element("effectiveDate").Value, expireDate = promo.Element("expireDate").Value;
                    if (!string.IsNullOrEmpty(effectiveDate) && !string.IsNullOrEmpty(expireDate))
                    {
                        DateTime onSet = DateTime.ParseExact(effectiveDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        DateTime expiry = DateTime.ParseExact(expireDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        if (checkinDate < onSet && checkinDate > expiry)
                            validity = false;
                    }
                    if (validity)
                        promotions.Add(new XElement("Promotions", promo.Element("Name").Value));
                }
            }
            return promotions;
        }
        #endregion
        #region Remove Namespaces
        public XElement removeAllNamespaces(XElement e)
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
        #region Get Data From API log
        public XElement XmlData(string trackNumber, int logTypeID, int supplierID)
        {
            DataTable dt = tgd.GetLog(trackNumber, logTypeID, supplierID);
            XElement Data = new XElement("Data");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //string respJson = JsonConvert.DeserializeObject(dt.Rows[i]["logrequestXml"].ToString()).ToString();
                //JContainer.Parse(dt.Rows[i]["logrequestXml"].ToString());
                //string temp = JsonConvert.SerializeObject(respJson);
                //var XmlElement = JsonConvert.DeserializeXmlNode("Response", respJson);
                XElement response = XElement.Parse(dt.Rows[i]["logResponseXml"].ToString());
                if (response.Descendants("JSON").Any())
                    response.Descendants("JSON").Remove();
                Data.Add(response);
            }
            return Data;
        }
        #endregion
        #region Get Currency List
        public List<string> currencyList()
        {
            string strPath = @"App_Data\SmyRooms\Smy_Currencies.xml";
            string filePath = Path.Combine(HttpRuntime.AppDomainAppPath, strPath);
            XElement C = XElement.Load(filePath);
            List<string> Currencies = C.Descendants("Currency").Select(x => x.Value).ToList();
            return Currencies;
        }
        #endregion
        #region Cancellation Policy Amount
        public double PolicyAmount(string type, string value, int nights, double bookAmt)
        {
            double amount = 0.00, input = Convert.ToDouble(value);
            switch (type)
            {
                case "NIGHTS":
                    {
                        amount = bookAmt / nights;
                        amount = amount * input;
                        break;
                    }
                case "PERCENT":
                    {
                        amount = bookAmt * input / 100;
                        break;
                    }
                case "IMPORT":
                    {
                        amount = input;
                        break;
                    }
            }
            return amount;
        } 
        #endregion
        #region Rooms For Booking
       public Rooms[] bookingReqRooms(XElement Req)
        {
            var roomList = new List<Rooms>();
            int counter = 1;
            foreach (XElement room in Req.Descendants("Room"))
            {
                Rooms currentRoom = new Rooms();
                var paxes = new List<PaxDetail>();
                foreach (XElement guest in room.Descendants("PaxInfo"))
                {
                    string age = guest.Element("GuestType").Value.ToUpper().Equals("ADULT") ? "30" : guest.Element("Age").Value;
                    paxes.Add(new PaxDetail
                    {
                        FirstName = guest.Element("FirstName").Value,
                        LastName = guest.Element("LastName").Value,
                        Age = Convert.ToInt32(age)
                    });
                }
                currentRoom.OccupancyReferenceId = counter++;
                currentRoom.Paxes = paxes.ToArray();
                roomList.Add(currentRoom);
            }
            return roomList.ToArray();
        }
        #endregion
        #region Rooms for booking response 
        public XElement bookingRespRooms(XElement Rooms, double amount, string HotelID)
        {
            double perRoomRate = amount / Rooms.Descendants("Room").Count();
            XElement GuestDetails = new XElement("GuestDetails");
            foreach(XElement room in Rooms.Descendants("Room"))
            {
                GuestDetails.Add(new XElement("Room",
                                    new XAttribute("ID",room.Attribute("RoomTypeID").Value),
                                    new XAttribute("RoomType", room.Attribute("RoomType").Value),
                                    new XAttribute("ServiceID",""),
                                    new XAttribute("RefNo", HotelID),
                                    new XAttribute("MealPlanID", room.Attribute("MealPlanID").Value),
                                    new XAttribute("MealPlanName",""),
                                    new XAttribute("MealPlanCode",""),
                                    new XAttribute("MealPlanPrice", room.Attribute("MealPlanPrice").Value),
                                    new XAttribute("PerNightRoomRate",""),
                                    new XAttribute("RoomStatus","true"),
                                    new XAttribute("TotalRoomRate", perRoomRate.ToString()),
                                    guestDetailsResp(room.Descendants("PaxInfo").ToList())));
            }
            return GuestDetails;
        }
        public List<XElement> guestDetailsResp(List<XElement> PaxInfo)
        {
            List<XElement> guests = new List<XElement>();
            try
            {
                foreach (XElement pax in PaxInfo)
                    guests.Add(new XElement("RoomGuest", pax.Elements()));
                return guests;
            }
            catch { return guests; }
        }
        #endregion
        #region TravelGate Configuration
        private void TravelGateConfiguration(int supplierID, string CustomerID)
        {
            #region Supplier Credentials
            XElement SmyCredentials = supplier_Cred.getsupplier_credentials(CustomerID, supplierID.ToString());
            client = SmyCredentials.Element("Client").Value;
            testMode = SmyCredentials.Element("TestMode").Value;
            context = SmyCredentials.Element("Context").Value;
            url = SmyCredentials.Element("Url").Value;
            apiKey = SmyCredentials.Element("ApiKey").Value;
            accessCode = SmyCredentials.Element("AccessCode").Value;
            #endregion
        }
        #endregion
        #endregion

    }
}