using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.SalTours;

namespace TravillioXMLOutService.Supplier.SalTours
{
    public class SalServices
    {
        
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance", xsd = "http://www.w3.org/2001/XMLSchema", xmlns = "http://schemas.xmlsoap.org/soap/envelope/";
        XNamespace securityNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        XNamespace OTANamespace = "http://www.opentravel.org/OTA/2003/05";
        string DMC = string.Empty;
        string hotelcode = string.Empty;
        string supplierCurrency = string.Empty;
        int suplierID = 19;

        SalServerRequest servreq = new SalServerRequest();
        SalDataAccess sda = new SalDataAccess();
        #region Hotel Search
        public List<XElement> HotelAvailability(XElement Req, string SupplierType)
        {
            DMC = SupplierType;
            List<XElement> hotelList = new List<XElement>();
            try
            {                
                string checkin = reformatDate(Req.Descendants("FromDate").FirstOrDefault().Value);
                string checkout = reformatDate(Req.Descendants("ToDate").FirstOrDefault().Value);        
                #region Currency and City Mapping
                XElement Currencies = Sal_Currency.sal_Curencies(); ;
                string cityID = Req.Descendants("CityID").FirstOrDefault().Value;
                DataTable CityData = sda.SalCityMapping(cityID, "19");               
                if (CityData.Rows.Count == 0)
                    return null;
                List<string> SalCities = new List<string>();
                for (int i = 0; i < CityData.Rows.Count; i++)
                    SalCities.Add(CityData.Rows[i]["SupCityId"].ToString());                         
                List<XElement> CurrencyList = new List<XElement>();
                if (Currencies != null)
                    CurrencyList = Currencies.Descendants("Code").ToList();
                string currency = string.Empty;
                if (CurrencyList != null)
                    currency = CurrencyList.Where(x => x.Value.Equals(Req.Descendants("DesiredCurrencyCode").FirstOrDefault().Value)).Any() ?
                                 Req.Descendants("DesiredCurrencyCode").FirstOrDefault().Value : CurrencyList.LastOrDefault().Value;
                else
                    currency = "EUR"; 
                #endregion                     
                #region Supplier Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "19");
                string username = suppliercred.Descendants("username").FirstOrDefault().Value;
                string password = suppliercred.Descendants("password").FirstOrDefault().Value;
                string action = suppliercred.Descendants("searchURL").FirstOrDefault().Value; 
                #endregion
                #region Supplier Interaction
                foreach (string city in SalCities)
                {

                    try
                    {
                        #region Request XML
                        XDocument SupplierRequest = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                                                      new XElement(xmlns + "Envelope",
                                                          new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                                          new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                                          new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                          new XElement(xmlns + "Header",
                                                              new XElement(securityNamespace + "Security",
                                                                  new XAttribute(XNamespace.None + "xmlns", securityNamespace),
                                                                  new XElement(securityNamespace + "UsernameToken",
                                                                      new XElement(securityNamespace + "Username", username),
                                                                      new XElement(securityNamespace + "Password", password)))),
                                                          new XElement(xmlns + "Body",
                                                              new XElement(xmlns + "OTA_HotelAvailRQ",
                                                                  new XAttribute("Version", "0"),
                                                                  new XAttribute("RequestedCurrency", currency),
                                                                  new XAttribute("RateDetailsInd", "true"),
                                                                  new XElement(OTANamespace + "POS",
                                                                      new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                      new XElement(OTANamespace + "Source",
                                                                          new XAttribute("ISOCountry", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value))),
                                                                  new XElement(OTANamespace + "AvailRequestSegments",
                                                                      new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                      new XElement(OTANamespace + "AvailRequestSegment",
                                                                          new XElement(OTANamespace + "StayDateRange",
                                                                              new XAttribute("Start", checkin),
                                                                              new XAttribute("End", checkout)),
                                                                          SearchRooms(Req.Descendants("Rooms").FirstOrDefault()),
                                                                          new XElement(OTANamespace + "HotelSearchCriteria",
                                                                              new XElement(OTANamespace + "Criterion",
                                                                                  new XElement(OTANamespace + "HotelRef",
                                                                                      new XAttribute("HotelCityCode", city))))))))));
                        #endregion
                        #region Log Save
                        SalTours_Logs model = new SalTours_Logs();
                        model.CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value);
                        model.Logtype = "Search";
                        model.LogtypeID = 1;
                        model.TrackNo = Req.Descendants("TransID").FirstOrDefault().Value;
                        #endregion
                        #region Room Mapping for Min Rate
                        int id = 1;
                        foreach (XElement room in Req.Descendants("RoomPax"))
                            room.Add(new XElement("id", id++.ToString()));
                        #endregion
                        #region Response
                        XDocument response = servreq.SalRequest(SupplierRequest, action, model, Req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement SupplierResponse = removeAllNamespaces(response.Root);
                        string hotelIDs = string.Empty;
                        List<string> HotelIDs = SupplierResponse.Descendants("RoomStay").Select(x => x.Descendants("BasicPropertyInfo").FirstOrDefault().Attribute("HotelCode").Value).ToList();
                        foreach(string s in HotelIDs)
                        {
                            if (string.IsNullOrEmpty(hotelIDs))
                                hotelIDs += "\'"+s+"\'";
                            else
                                hotelIDs += ",\'" + s+"\'";
                        }
                        DataTable DBImages = sda.GetHotelImages(hotelIDs);
                        if (!SupplierResponse.Descendants("Errors").Any())
                        {
                            XElement Hotel = new XElement("Hotel");
                            DataTable staticdata = sda.GetHotelsList(city);
                            foreach (XElement hotel in SupplierResponse.Descendants("RoomStay"))
                            {
                                string hotelid = hotel.Descendants("BasicPropertyInfo").FirstOrDefault().Attribute("HotelCode").Value;
                                string hotelName = hotel.Descendants("BasicPropertyInfo").First().Attribute("HotelName").Value;
                                var result = staticdata.AsEnumerable().Where(dt => dt.Field<string>("HotelCode") == hotelid);
                                int countre = result.Count();
                                DataRow[] drow = result.ToArray();
                                if (drow.Count() > 0)
                                {
                                    DataRow dr = drow[0];
                                    string minstar = Req.Descendants("MinStarRating").FirstOrDefault().Value;
                                    string maxstar = Req.Descendants("MaxStarRating").FirstOrDefault().Value;
                                    string hotelstar = dr["Rating"].ToString();
                                    if (StarRating(minstar, maxstar, hotelstar))
                                    {
                                        string address = dr["AddressLine1"].ToString() + ", " + dr["AddressLine2"].ToString();
                                        #region Image
                                        string image = null, tempimg = null;
                                        //DataTable imagesfromdb = sda.GetHotelImages(hotelid);
                                        var imageforHotel = DBImages.AsEnumerable().Where(dt => dt.Field<string>("HotelID") == hotelid);
                                        DataRow[] imagesfromDB = imageforHotel.ToArray();
                                        for (int ic = 0; ic < imagesfromDB.Count(); ic++)
                                        {
                                            tempimg = imagesfromDB[0]["ImageUrl"].ToString();
                                            if (imagesfromDB[ic]["Caption"].ToString().ToUpper().Equals("HOTEL"))
                                            {
                                                image = imagesfromDB[ic]["ImageUrl"].ToString();
                                                break;
                                            }
                                        }
                                        if (string.IsNullOrEmpty(image))
                                            image = tempimg;
                                        #endregion
                                        hotelList.Add(new XElement("Hotel",
                                                                                    new XElement("HotelID", hotel.Descendants("BasicPropertyInfo").First().Attribute("HotelCode").Value),
                                                                                    new XElement("HotelName", hotel.Descendants("BasicPropertyInfo").First().Attribute("HotelName").Value),
                                                                                    new XElement("PropertyTypeName", ""),
                                                                                    new XElement("CountryID", Req.Descendants("CountryID").FirstOrDefault().Value),
                                                                                    new XElement("CountryName", Req.Descendants("CountryName").FirstOrDefault().Value),
                                                                                    new XElement("CountryCode", Req.Descendants("CountryCode").FirstOrDefault().Value),
                                                                                    new XElement("CityId", Req.Descendants("CityID").FirstOrDefault().Value),
                                                                                    new XElement("CityCode", Req.Descendants("CityCode").FirstOrDefault().Value),
                                                                                    new XElement("CityName", Req.Descendants("CityName").FirstOrDefault().Value),
                                                                                    new XElement("AreaId"),
                                                                                    new XElement("AreaName"),
                                                                                    new XElement("RequestID", city),
                                                                                    new XElement("Address", address),
                                                                                    new XElement("Location"),
                                                                                    new XElement("Description"),
                                                                                    new XElement("StarRating", dr["Rating"]),
                                                                                    new XElement("MinRate", mPrice(hotel, Req)),
                                                                                    new XElement("HotelImgSmall", image),
                                                                                    new XElement("HotelImgLarge", image),
                                                                                    new XElement("MapLink"),
                                                                                    new XElement("Longitude", dr["Longitude"]),
                                                                                    new XElement("Latitude", dr["Latitude"]),
                                                                                    new XElement("DMC", DMC),
                                                                                    new XElement("SupplierID", suplierID.ToString()),
                                                                                    new XElement("Currency", hotel.Descendants("RoomRate").First().Element("Total").Attribute("CurrencyCode").Value),
                                                                                    new XElement("Offers"),
                                                                                    new XElement("Facilities", new XElement("Facility", "No Facility")),
                                                                                    new XElement("Rooms")));
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ce = new CustomException(ex);
                        ce.MethodName = "HotelAvailability(Supplier Interaction)";
                        ce.PageName = "SalServices";
                        ce.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                        ce.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                        #endregion
                    }
                } 
                #endregion
            }
            catch (Exception ex)
            {
                 #region Exception
                        CustomException ce = new CustomException(ex);
                        ce.MethodName = "HotelAvailability";
                        ce.PageName = "SalServices";
                        ce.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                        ce.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                        #endregion                
            }
            return hotelList;
        } 
        #endregion
        #region Hotel Detail
        public XElement HotelDetails(XElement Req)
        {
            XElement details = null;
            XElement responseElement = null;
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion           
            try
            {
                DataTable staticData = sda.GetHotelDetails(Req.Descendants("HotelID").FirstOrDefault().Value);
                if (staticData.Rows.Count > 0)
                {
                    DataRow dr = staticData.Rows[0];

                    #region Images
                    //XElement Images = new XElement("Images");
                    //if (!string.IsNullOrEmpty(dr["images"].ToString()))
                    //{
                    //    XElement SupplierImages = XElement.Parse(dr["images"].ToString());

                    //    List<XElement> ImageList = new List<XElement>();
                    //    foreach (XElement imageitem in SupplierImages.Descendants("ImageItem"))
                    //        ImageList.Add(new XElement("Image",
                    //                          new XAttribute("Path", imageitem.Descendants("URL").FirstOrDefault().Value),
                    //                          new XAttribute("Caption", imageitem.Descendants("Description").FirstOrDefault().Attribute("Caption").Value)));

                    //    Images.Add(ImageList);
                    //}
                    XElement ImageList = Images("\'"+Req.Descendants("HotelID").FirstOrDefault().Value+"\'");
                    if (!ImageList.HasElements)
                    {
                        ImageList.Add(new XElement("Image",
                                        new XAttribute("Path", ""),
                                        new XAttribute("Caption", "")));
                    }
                    #endregion
                    #region Facilities
                    DataTable FacilitiesFromDB = sda.GetHotelFacilities(Req.Descendants("HotelID").FirstOrDefault().Value);
                    List<XElement> FacilityList = new List<XElement>();
                    for (int i = 0; i < FacilitiesFromDB.Rows.Count; i++)
                    {
                        FacilityList.Add(new XElement("Facility", FacilitiesFromDB.Rows[i]["Facility"].ToString()));
                    }
                    XElement Facilities = new XElement("Facilities", FacilityList.OrderBy(x => x.Value));
                    if (!Facilities.HasElements)
                        Facilities.Add(new XElement("Facility", "No Facility Available"));

                    //if (!string.IsNullOrEmpty(dr["facilities"].ToString()))
                    //{
                    //    XElement SupplierFacilities = XElement.Parse(dr["facilities"].ToString());


                    //    foreach (XElement Facility in SupplierFacilities.Descendants("Service"))
                    //        FacilityList.Add(new XElement("Facility", Facility.Descendants("DescriptiveText").FirstOrDefault().Value));
                    //    Facilities.Add(FacilityList.OrderBy(x=>x.Value));
                    //}
                    #endregion
                    string desc = dr["Description"].ToString();    // update after data download
                    string fax = !string.IsNullOrEmpty(dr["Fax"].ToString()) ? dr["Fax"].ToString() : string.Empty;
                    string phone = !string.IsNullOrEmpty(dr["Telephone"].ToString()) ? dr["Telephone"].ToString() : string.Empty;
                    var hotels = new XElement("Hotels",
                                     new XElement("Hotel",
                                         new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                         new XElement("Description", desc),
                                         ImageList,
                                         Facilities,
                                         new XElement("ContactDetails",
                                             new XElement("Phone", phone),
                                             new XElement("Fax", fax)),
                                         new XElement("CheckinTime"),
                                         new XElement("CheckoutTime")));
                    responseElement = new XElement("hoteldescResponse", hotels);
                }
                #region Response Format
                details = new XElement(
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
                                    new XElement(Req.Descendants("hoteldescRequest").FirstOrDefault()),
                                    responseElement)));
                #endregion
            }
            catch (Exception ex)
            {
                
                 #region Exception
                        CustomException ce = new CustomException(ex);
                        ce.MethodName = "HotelDetails";
                        ce.PageName = "SalServices";
                        ce.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                        ce.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                        #endregion                
            }
            return details;
        }
        #endregion
        #region Room Availability
        public XElement RoomAvailability(XElement Req, string SupplierType,string htlID)
        {
            DMC = SupplierType;
            hotelcode = htlID;
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            XElement RoomResponse = new XElement("searchResponse");
            XElement AvailablilityResponse = null;
            try
            {
                //Req = XDocument.Load(@"C:\Users\Aman\Desktop\newTest.xml").Root;
                string hotelID = hotelcode;
                List<XElement> logresponse = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 1, 19);
                DateTime starttime = DateTime.Now;
                XElement resp = logresponse.Descendants("RoomStay")
                                .Where(x => x.Descendants("BasicPropertyInfo").FirstOrDefault().Attribute("HotelCode").Value.Equals(hotelID))
                                .FirstOrDefault();
                //resp = XDocument.Load(@"C:\Users\Aman\Desktop\Test.xml").Root;

                #region Log Save
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                    log.SupplierID = 19;
                    log.logrequestXML = removeAllNamespaces(Req).ToString();
                    log.logresponseXML = resp.ToString();
                    log.LogTypeID = 2;
                    log.LogType = "RoomAvail";
                    log.StartTime = starttime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "RoomAvailability";
                    ex1.PageName = "SalServices";
                    ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion
                #region Adding Occupancy Id
                int id = 1;
                foreach (XElement room in Req.Descendants("RoomPax"))
                    room.Add(new XElement("id", id++.ToString()));
                #endregion
                string Salcity = logresponse.FirstOrDefault().Descendants("HotelRef").FirstOrDefault().Attribute("HotelCityCode").Value;
                supplierCurrency = resp.Descendants("Rate").Where(x => x.Attribute("ChargeType").Value.Equals("26")).FirstOrDefault()
                                    .Element("Base").Attribute("CurrencyCode").Value;
                DataTable staticdata = sda.GetHotelsList(Salcity);
                var result = staticdata.AsEnumerable().Where(dt => dt.Field<string>("HotelCode") == hotelID);
                int countre = result.Count();
                DataRow[] drow = result.ToArray();
                if (drow.Count() > 0)
                {
                    DataRow dr = drow[0];
                    #region Response XML
                    var availabilityResponse = new XElement("Hotel",
                                                                  new XElement("HotelID", hotelID),
                                                                  new XElement("HotelName", resp.Descendants("BasicPropertyInfo").FirstOrDefault().Attribute("HotelName").Value),
                                                                  new XElement("PropertyTypeName"),
                                                                  new XElement("CountryID", Req.Descendants("CountryID").FirstOrDefault().Value),

                                                                  new XElement("CountryCode"),
                                                                  new XElement("CountryName", Req.Descendants("CountryName").FirstOrDefault().Value),
                                                                  new XElement("CityId", Req.Descendants("CityID").FirstOrDefault().Value),
                                                                  new XElement("CityCode", Req.Descendants("CityCode").FirstOrDefault().Value),
                                                                  new XElement("CityName", Req.Descendants("CityName").FirstOrDefault().Value),
                                                                  new XElement("AreaName"),
                                                                  new XElement("AreaId"),
                                                                  new XElement("Address", address(resp.Descendants("Address").FirstOrDefault())),
                                                                  new XElement("Location"),
                                                                  new XElement("Description"),
                                                                  new XElement("StarRating", dr["Rating"]),
                                                                  new XElement("MinRate"),
                                                                  new XElement("HotelImgSmall"),
                                                                  new XElement("HotelImgLarge"),
                                                                  new XElement("MapLink"),
                                                                  new XElement("Longitude", dr["Longitude"]),
                                                                  new XElement("Latitude", dr["Latitude"]),
                                                                  new XElement("DMC", DMC),
                                                                  new XElement("SupplierID", suplierID.ToString()),
                                                                  new XElement("Currency"),
                                                                 new XElement("Offers"),
                                                                  new XElement("Facilities", new XElement("Facility", "No Facility available")),
                                                                  groupedRooms(resp, Req)); 
                    #endregion
                    RoomResponse.Add(new XElement("Hotels", availabilityResponse));
                }
                #region Response Format
                //removetags(Req);
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
                CustomException ce = new CustomException(ex);
                ce.MethodName = "RoomAvailablity";
                ce.PageName = "SalServices";
                ce.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ce.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                #endregion                
            }
            return AvailablilityResponse;
        }

        #region Room Grouping
        public XElement groupedRooms(XElement SalRooms, XElement Req)
        {
            #region Old Grouping
            // bool mpFlag = false;                                 
            // foreach(XElement room in SalRooms.Descendants("RoomRate"))
            // {
            //     List<string> meals = new List<string>();
            //     XElement mealIncluded = SalRooms.Descendants("RatePlan")
            //                            .Where(x => x.Attribute("RatePlanCode").Value.Equals(room.Attribute("RatePlanCode").Value)).FirstOrDefault();
            //     foreach (XAttribute m in mealIncluded.Descendants("MealsIncluded").FirstOrDefault().Attributes())
            //         meals.Add(m.Value);
            //     if (meals.Contains("true"))
            //         mpFlag = true; 
            //     int adults = 0, child = 0, infant = 0;
            //     adults = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("10"))
            //              .FirstOrDefault().Attribute("Count").Value);
            //     if (room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("4")).Any())
            //         child = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("4"))
            //                  .FirstOrDefault().Attribute("Count").Value);
            //     if(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("3")).Any())
            //         infant = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("3"))
            //                 .FirstOrDefault().Attribute("Count").Value);
            //     string occupancy = adults.ToString() + "-" + child.ToString() + "-" + infant.ToString();
            //     room.Add(new XElement("Occupancy", occupancy));
            //     if (!mpFlag)
            //         room.Element("Rates").AddBeforeSelf(new XElement("Meal", new XAttribute("Name","Room only")));
            // }
            // var SupplGrouping = from rooms in SalRooms.Descendants("RoomRate")
            //                     group rooms by rooms.Descendants("Occupancy").FirstOrDefault().Value;
            // int counter = 1;
            // XElement Grouping = new XElement("Groups");
            //foreach(XElement room in Req.Descendants("RoomPax"))
            //{
            //    int child = 0, infant = 0;
            //    if(room.Descendants("ChildAge").Any())
            //    {
            //        foreach(XElement age in room.Descendants("ChildAge"))
            //        {
            //            if (Convert.ToInt32(age.Value) <= 2)
            //                child++;
            //            else
            //                infant++;
            //        }
            //    }
            //    string paxes = room.Element("Adult").Value +"-"+ child.ToString() +"-"+ infant.ToString();
            //    var entries = SupplGrouping.Where(x => x.Key.Equals(paxes));
            //    Grouping.Add(new XElement("Group" + counter++.ToString(), new XAttribute("Paxes", paxes), entries));
            //}
            //int roomcount = Req.Descendants("RoomPax").Count();
            //XElement FinalGroup = null;
            //#region Room Grouping
            //#region 1 Room
            //if (roomcount == 1)
            //    FinalGroup = new XElement("Group", new XElement("Room", Grouping.Descendants("RoomRate").ToList()));
            //#endregion
            //#region 2 Rooms
            //else if (roomcount == 2)
            //{
            //    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            //                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  select new XElement("Room", r1, r2);
            //    FinalGroup = new XElement("Groups", joinall);
            //}
            //#endregion
            //#region 3 Rooms
            //else if (roomcount == 3)
            //{
            //    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            //                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  select new XElement("Room", r1, r2, r3);

            //    FinalGroup = new XElement("Groups", joinall);
            //}
            //#endregion
            //#region 4 Rooms
            //else if (roomcount == 4)
            //{
            //    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            //                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  select new XElement("Room", r1, r2, r3, r4);

            //    FinalGroup = new XElement("Groups", joinall);
            //}
            //#endregion
            //#region 5 Rooms
            //else if (roomcount == 5)
            //{
            //    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            //                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r5 in Grouping.Elements("Group5").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r5.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  select new XElement("Room", r1, r2, r3, r4, r5);

            //    FinalGroup = new XElement("Groups", joinall);
            //}
            //#endregion
            //#region 6 Rooms
            //else if (roomcount == 6)
            //{
            //    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            //                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r5 in Grouping.Elements("Group5").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r5.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r6 in Grouping.Elements("Group6").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r6.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  select new XElement("Room", r1, r2, r3, r4, r5, r6);

            //    FinalGroup = new XElement("Groups", joinall);
            //}
            //#endregion
            //#region 7 Rooms
            //else if (roomcount == 7)
            //{
            //    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            //                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r5 in Grouping.Elements("Group5").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r5.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r6 in Grouping.Elements("Group6").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r6.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r7 in Grouping.Elements("Group7").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r7.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  select new XElement("Room", r1, r2, r3, r4, r5, r6, r7);

            //    FinalGroup = new XElement("Groups", joinall);
            //}
            //#endregion
            //#region 8 Rooms
            //else if (roomcount == 8)
            //{
            //    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            //                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r5 in Grouping.Elements("Group5").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r5.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r6 in Grouping.Elements("Group6").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r6.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r7 in Grouping.Elements("Group7").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r7.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r8 in Grouping.Elements("Group8").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r8.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  select new XElement("Room", r1, r2, r3, r4, r5, r6, r7, r8);

            //    FinalGroup = new XElement("Groups", joinall);
            //}
            //#endregion
            //#region 9 Rooms
            //else if (roomcount == 9)
            //{
            //    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            //                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r5 in Grouping.Elements("Group5").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r5.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r6 in Grouping.Elements("Group6").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r6.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r7 in Grouping.Elements("Group7").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r7.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r8 in Grouping.Elements("Group8").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r8.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  join r9 in Grouping.Elements("Group9").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r9.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                  select new XElement("Room", r1, r2, r3, r4, r5, r6, r7, r8, r9);

            //    FinalGroup = new XElement("Groups", joinall);
            //}
            //#endregion
            //#endregion
            // XElement Rooms = new XElement("Rooms");
            // int index = 1; 
            #endregion
            XElement Rooms = new XElement("Rooms");
            bool Package = false;
            try
            {
                
                if (SalRooms.Descendants("RatePlan").Where(x => x.Attributes("RatePlanType").Any() && x.Attribute("RatePlanType").Value.ToUpper().Equals("PACKAGE")).Any())
                    Package = true;
                foreach (XElement room in SalRooms.Descendants("RoomRate"))
                {                    
                    bool mpFlag = false;
                    List<string> meals = new List<string>();
                    XElement mealIncluded = SalRooms.Descendants("RatePlan")
                                           .Where(x => x.Attribute("RatePlanCode").Value.Equals(room.Attribute("RatePlanCode").Value)).FirstOrDefault();
                    foreach (XAttribute m in mealIncluded.Descendants("MealsIncluded").FirstOrDefault().Attributes())
                        meals.Add(m.Value);
                    if (meals.Contains("true"))
                        mpFlag = true;
                    int adults = 0, child = 0, infant = 0;
                    adults = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("10"))
                             .FirstOrDefault().Attribute("Count").Value);
                    if (room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("8")).Any())
                        child = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("8"))
                                 .FirstOrDefault().Attribute("Count").Value);
                    if (room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("7")).Any())
                        infant = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("7"))
                                .FirstOrDefault().Attribute("Count").Value);
                    string occupancy = adults.ToString() + "-" + child.ToString() + "-" + infant.ToString();
                    room.Add(new XElement("Occupancy", occupancy));
                    if (!mpFlag)
                        room.Element("Rates").AddBeforeSelf(new XElement("Meal", new XAttribute("Name", "Room only")));
                    room.Add(new XElement("Package", Package.ToString()));
                }

                var SupplGrouping = from rooms in SalRooms.Descendants("RoomRate")
                                    group rooms by new
                                    {
                                        c1 = rooms.Descendants("Occupancy").FirstOrDefault().Value,
                                        c2 = rooms.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
                                    };
                int possibleOutcomes = 1;
                int roomCount = SalRooms.Descendants("RoomRate").Count();
                XElement Grouping = new XElement("Groups");
                //foreach (XElement room in Req.Descendants("RoomPax"))
                //{
                //    int child = 0, infant = 0;
                //    if (room.Descendants("ChildAge").Any())
                //    {
                //        foreach (XElement age in room.Descendants("ChildAge"))
                //        {
                //            if (Convert.ToInt32(age.Value) <= 2)
                //                child++;
                //            else
                //                infant++;
                //        }
                //    }
                //    string paxes = room.Element("Adult").Value + "-" + child.ToString() + "-" + infant.ToString();
                //    var entries = SupplGrouping.Where(x => x.Key.c1.Equals(paxes));
                //    possibleOutcomes = possibleOutcomes * entries.FirstOrDefault().Elements().Count();
                //    Grouping.Add(new XElement("Group" + counter++.ToString(), new XAttribute("Paxes", paxes), entries));
                //}
                List<string> allMeals = new List<string>();
                List<string> allPax = new List<string>();
                foreach (var group in SupplGrouping)
                {
                    if (!allMeals.Contains(group.Key.c2))
                        allMeals.Add(group.Key.c2);
                    if (!allPax.Contains(group.Key.c1))
                        allPax.Add(group.Key.c1);
                }
                foreach (string meal in allMeals)
                {
                    Grouping.Add(new XElement("MealIncluded", new XAttribute("Name", meal)));
                    foreach (XElement room in Req.Descendants("RoomPax"))
                    {
                        int child = 0, infant = 0;
                        if (room.Descendants("ChildAge").Any())
                        {
                            foreach (XElement age in room.Descendants("ChildAge"))
                            {
                                if (Convert.ToInt32(age.Value) <= 2)
                                    infant++;
                                else
                                    child++;
                            }
                        }
                        string paxes = room.Element("Adult").Value + "-" + child.ToString() + "-" + infant.ToString();// "-0-0"; //+child.ToString() + "-" + infant.ToString();
                        var entries = SupplGrouping.Where(x => x.Key.c1.Equals(paxes) && x.Key.c2.Equals(meal));
                        Grouping.Elements("MealIncluded").Where(x => x.Attribute("Name").Value.Equals(meal)).FirstOrDefault().Add(new XElement("Group", new XAttribute("Paxes", paxes), entries));
                    }
                }
                int roomcount = Req.Descendants("RoomPax").Count();
                XElement FinalGroup = new XElement("Groups");
                #region Room Grouping New
               
                int repeats = roomcount - allPax.Count;
                int take = 0;
                //if (roomcount > 6 && repeats > 3)                   //max = 19683
                //    take = 3;               
                //else if (roomcount > 6 && repeats < 4)             //
                //    take = 5;
                //else if (roomcount < 6 && roomcount > 2 && repeats > 3)
                //    take = 5;
                //else if (roomcount < 6 && roomcount > 2 && repeats < 4)
                //    take = 7;
                //else if (roomcount < 3)
                //    take = 15;
                if (roomcount == 9 || roomcount == 8)                                 //max = 512
                    take = 4;
                else if (roomcount > 6 && roomcount < 8)                   //max = 6561
                    take = 5;
                else if (roomcount < 6 && roomcount > 3)            //max = 3125 at 6 max =15625
                    take = 10;
                else if (roomcount == 3)                            // max == 8000
                    take = 20;
                else if (roomcount == 2)                            // max == 900
                    take = 30;
                else if (roomcount == 1)                            // max = 400
                    take = 200;
                List<string> Roomtypes = new List<string>();
                foreach (XElement rt in SalRooms.Descendants("RoomType"))
                    Roomtypes.Add(rt.Attribute("RoomTypeCode").Value);
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
                    #region Room Types

                    //int t1=0, t2=0, t3=0, t4=0, t5=0, t6=0, t7=0, t8=0, t9=0, t10=0;
                    XElement RoomtypeData = stringAsNode(Roomtypes, new XElement("RoomTypeList"));
                    #endregion
                    int roomCounter = 1;
                    foreach (XElement room in Req.Descendants("RoomPax"))
                    {
                        int child = 0, infant = 0;
                        if (room.Descendants("ChildAge").Any())
                        {
                            foreach (XElement age in room.Descendants("ChildAge"))
                            {
                                if (Convert.ToInt32(age.Value) <= 2)
                                    infant++;
                                else
                                    child++;
                            }
                        }
                        string id = room.Element("id").Value;
                        string paxes = room.Element("Adult").Value + "-" + child.ToString() + "-" + infant.ToString(); //"-0-0";// +child.ToString() + "-" + infant.ToString();
                        if (roomCounter == 1)
                        {

                            //Room1 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                            foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                            {
                                if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room1.Count < take)
                                    Room1.Add(roomfortypes.Attribute("RoomTypeCode").Value + "," + id + "," + roomfortypes.Attribute("RatePlanCode").Value);
                            }
                            RoomtypeData.Descendants("Node").Where(x => Room1.Contains(x.Value)).Remove();
                        }
                        else if (roomCounter == 2)
                        {
                            //Room2 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                            if (!RoomtypeData.Descendants("Node").Any())
                                RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                            foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                            {
                                if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room2.Count < take)
                                    Room2.Add(roomfortypes.Attribute("RoomTypeCode").Value + "," + id + "," + roomfortypes.Attribute("RatePlanCode").Value);
                            }
                            RoomtypeData.Descendants("Node").Where(x => Room2.Contains(x.Value)).Remove();
                        }
                        else if (roomCounter == 3)
                        {
                            //Room3 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                            if (!RoomtypeData.Descendants("Node").Any())
                                RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                            foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                            {
                                if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room3.Count < take)
                                    Room3.Add(roomfortypes.Attribute("RoomTypeCode").Value + "," + id + "," + roomfortypes.Attribute("RatePlanCode").Value);
                                else
                                    break;
                            }
                            RoomtypeData.Descendants("Node").Where(x => Room3.Contains(x.Value)).Remove();
                        }
                        else if (roomCounter == 4)
                        {
                            //Room4 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                            if (!RoomtypeData.Descendants("Node").Any())
                                RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                            foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                            {
                                if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room4.Count < take)
                                    Room4.Add(roomfortypes.Attribute("RoomTypeCode").Value + "," + id + "," + roomfortypes.Attribute("RatePlanCode").Value);
                                else
                                    break;
                            }
                            RoomtypeData.Descendants("Node").Where(x => Room4.Contains(x.Value)).Remove();
                        }
                        else if (roomCounter == 5)
                        {
                            //Room5 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                            if (!RoomtypeData.Descendants("Node").Any())
                                RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                            foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                            {
                                if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room5.Count < take)
                                    Room5.Add(roomfortypes.Attribute("RoomTypeCode").Value + "," + id + "," + roomfortypes.Attribute("RatePlanCode").Value);
                                else
                                    break;
                            }
                            RoomtypeData.Descendants("Node").Where(x => Room5.Contains(x.Value)).Remove();
                        }
                        else if (roomCounter == 6)
                        {
                            //Room6 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                            if (!RoomtypeData.Descendants("Node").Any())
                                RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                            foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                            {
                                if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room6.Count < take)
                                    Room6.Add(roomfortypes.Attribute("RoomTypeCode").Value + "," + id + "," + roomfortypes.Attribute("RatePlanCode").Value);
                                else
                                    break;
                            }
                            RoomtypeData.Descendants("Node").Where(x => Room6.Contains(x.Value)).Remove();
                        }
                        else if (roomCounter == 7)
                        {
                            //Room7 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                            if (!RoomtypeData.Descendants("Node").Any())
                                RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                            foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                            {
                                if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room7.Count < take)
                                    Room7.Add(roomfortypes.Attribute("RoomTypeCode").Value + "," + id + "," + roomfortypes.Attribute("RatePlanCode").Value);
                                else
                                    break;
                            }
                            RoomtypeData.Descendants("Node").Where(x => Room7.Contains(x.Value)).Remove();
                        }
                        else if (roomCounter == 8)
                        {
                            //Room8 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                            if (!RoomtypeData.Descendants("Node").Any())
                                RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                            foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                            {
                                if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room8.Count < take)
                                    Room8.Add(roomfortypes.Attribute("RoomTypeCode").Value + "," + id + "," + roomfortypes.Attribute("RatePlanCode").Value);
                                else
                                    break;
                            }
                            RoomtypeData.Descendants("Node").Where(x => Room8.Contains(x.Value)).Remove();
                        }
                        else if (roomCounter == 9)
                        {
                            //Room9 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                            if (!RoomtypeData.Descendants("Node").Any())
                                RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                            foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                            {
                                if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room9.Count < take)
                                    Room9.Add(roomfortypes.Attribute("RoomTypeCode").Value + "," + id + "," + roomfortypes.Attribute("RatePlanCode").Value);
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
                            FinalGroup.Add(new XElement("Room", new XElement("type", rm1)));
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
                                List<string> rType = new List<string>();
                                List<string> rPlans = new List<string>();
                                bool samePackage = true;
                                if(Package)
                                {
                                    string[] data1 = rm1.Split(new char[] { ',' });
                                    rType.Add(data1[0]);
                                    rPlans.Add(data1[2]);
                                    string[] data2 = rm2.Split(new char[] { ',' });
                                    rType.Add(data2[0]);
                                    rPlans.Add(data2[2]);
                                    if (rType.Distinct().Count() != 1 && rPlans.Distinct().Count() != 1)
                                        samePackage = false;
                                }                                
                                if(samePackage)
                                    FinalGroup.Add(new XElement("Room", new XElement("type", rm1), new XElement("type", rm2)));
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
                                    List<string> rType = new List<string>();
                                    List<string> rPlans = new List<string>();
                                    bool samePackage = true;
                                    if (Package)
                                    {
                                        string[] data1 = rm1.Split(new char[] { ',' });
                                        rType.Add(data1[0]);
                                        rPlans.Add(data1[2]);
                                        string[] data2 = rm2.Split(new char[] { ',' });
                                        rType.Add(data2[0]);
                                        rPlans.Add(data2[2]);
                                        string[] data3 = rm3.Split(new char[] { ',' });
                                        rType.Add(data3[0]);
                                        rPlans.Add(data3[2]);
                                        if (rType.Distinct().Count() != 1 && rPlans.Distinct().Count() != 1)
                                            samePackage = false;
                                    }       
                                    string CurrentConfig = string.Empty;                                   
                                    if (samePackage)
                                    {
                                        XElement Currentgroup = new XElement("Room",
                                                                                       new XElement("type", rm1),
                                                                                       new XElement("type", rm2),
                                                                                       new XElement("type", rm3));
                                        foreach (string rt in Roomtypes)
                                        {
                                            CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { ',' })[0].Equals(rt)).Count().ToString() + "-";
                                        }
                                        CurrentConfig += meal.Attribute("Name").Value;
                                        Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                        if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                            FinalGroup.Add(Currentgroup); 
                                    }
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
                                        List<string> rType = new List<string>();
                                        List<string> rPlans = new List<string>();
                                        bool samePackage = true;
                                        if (Package)
                                        {
                                            string[] data1 = rm1.Split(new char[] { ',' });
                                            rType.Add(data1[0]);
                                            rPlans.Add(data1[2]);
                                            string[] data2 = rm2.Split(new char[] { ',' });
                                            rType.Add(data2[0]);
                                            rPlans.Add(data2[2]);
                                            string[] data3 = rm3.Split(new char[] { ',' });
                                            rType.Add(data3[0]);
                                            rPlans.Add(data3[2]);
                                            string[] data4 = rm4.Split(new char[] { ',' });
                                            rType.Add(data4[0]);
                                            rPlans.Add(data4[2]);
                                            if (rType.Distinct().Count() != 1 && rPlans.Distinct().Count() != 1)
                                                samePackage = false;
                                        }       
                                        if (samePackage)
                                        {
                                            XElement Currentgroup = new XElement("Room",
                                                                new XElement("type", rm1),
                                                                new XElement("type", rm2),
                                                                new XElement("type", rm3),
                                                                new XElement("type", rm4));
                                            foreach (string rt in Roomtypes)
                                            {
                                                CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { ',' })[0].Equals(rt)).Count().ToString() + "-";
                                            }
                                            CurrentConfig += meal.Attribute("Name").Value;
                                            Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                            if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                FinalGroup.Add(Currentgroup);
                                        }
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
                                            List<string> rType = new List<string>();
                                            List<string> rPlans = new List<string>();
                                            bool samePackage = true;
                                            if (Package)
                                            {
                                                string[] data1 = rm1.Split(new char[] { ',' });
                                                rType.Add(data1[0]);
                                                rPlans.Add(data1[2]);
                                                string[] data2 = rm2.Split(new char[] { ',' });
                                                rType.Add(data2[0]);
                                                rPlans.Add(data2[2]);
                                                string[] data3 = rm3.Split(new char[] { ',' });
                                                rType.Add(data3[0]);
                                                rPlans.Add(data3[2]);
                                                string[] data4 = rm4.Split(new char[] { ',' });
                                                rType.Add(data4[0]);
                                                rPlans.Add(data4[2]);
                                                string[] data5 = rm5.Split(new char[] { ',' });
                                                rType.Add(data5[0]);
                                                rPlans.Add(data5[2]);
                                                if (rType.Distinct().Count() != 1 && rPlans.Distinct().Count() != 1)
                                                    samePackage = false;
                                            }       
                                            if (samePackage)
                                            {
                                                XElement Currentgroup = new XElement("Room",
                                                                    new XElement("type", rm1),
                                                                    new XElement("type", rm2),
                                                                    new XElement("type", rm3),
                                                                    new XElement("type", rm4),
                                                                    new XElement("type", rm5));
                                                foreach (string rt in Roomtypes)
                                                {
                                                    CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { ',' })[0].Equals(rt)).Count().ToString() + "-";
                                                }
                                                CurrentConfig += meal.Attribute("Name").Value;
                                                Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                    FinalGroup.Add(Currentgroup);
                                            }
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
                                                List<string> rType = new List<string>();
                                                List<string> rPlans = new List<string>();
                                                bool samePackage = true;
                                                if (Package)
                                                {
                                                    string[] data1 = rm1.Split(new char[] { ',' });
                                                    rType.Add(data1[0]);
                                                    rPlans.Add(data1[2]);
                                                    string[] data2 = rm2.Split(new char[] { ',' });
                                                    rType.Add(data2[0]);
                                                    rPlans.Add(data2[2]);
                                                    string[] data3 = rm3.Split(new char[] { ',' });
                                                    rType.Add(data3[0]);
                                                    rPlans.Add(data3[2]);
                                                    string[] data4 = rm4.Split(new char[] { ',' });
                                                    rType.Add(data4[0]);
                                                    rPlans.Add(data4[2]);
                                                    string[] data5 = rm5.Split(new char[] { ',' });
                                                    rType.Add(data5[0]);
                                                    rPlans.Add(data5[2]);
                                                    string[] data6 = rm6.Split(new char[] { ',' });
                                                    rType.Add(data6[0]);
                                                    rPlans.Add(data6[2]);
                                                    if (rType.Distinct().Count() != 1 && rPlans.Distinct().Count() != 1)
                                                        samePackage = false;
                                                }
                                                if (samePackage)
                                                {
                                                    XElement Currentgroup = new XElement("Room",
                                                                        new XElement("type", rm1),
                                                                        new XElement("type", rm2),
                                                                        new XElement("type", rm3),
                                                                        new XElement("type", rm4),
                                                                        new XElement("type", rm5),
                                                                        new XElement("type", rm6));
                                                    foreach (string rt in Roomtypes)
                                                    {
                                                        CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { ',' })[0].Equals(rt)).Count().ToString() + "-";
                                                    }
                                                    CurrentConfig += meal.Attribute("Name").Value;
                                                    Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                    if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                        FinalGroup.Add(Currentgroup);
                                                }
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
                                                    List<string> rType = new List<string>();
                                                    List<string> rPlans = new List<string>();
                                                    bool samePackage = true;
                                                    if (Package)
                                                    {
                                                        string[] data1 = rm1.Split(new char[] { ',' });
                                                        rType.Add(data1[0]);
                                                        rPlans.Add(data1[2]);
                                                        string[] data2 = rm2.Split(new char[] { ',' });
                                                        rType.Add(data2[0]);
                                                        rPlans.Add(data2[2]);
                                                        string[] data3 = rm3.Split(new char[] { ',' });
                                                        rType.Add(data3[0]);
                                                        rPlans.Add(data3[2]);
                                                        string[] data4 = rm4.Split(new char[] { ',' });
                                                        rType.Add(data4[0]);
                                                        rPlans.Add(data4[2]);
                                                        string[] data5 = rm5.Split(new char[] { ',' });
                                                        rType.Add(data5[0]);
                                                        rPlans.Add(data5[2]);
                                                        string[] data6 = rm6.Split(new char[] { ',' });
                                                        rType.Add(data6[0]);
                                                        rPlans.Add(data6[2]);
                                                        string[] data7 = rm7.Split(new char[] { ',' });
                                                        rType.Add(data7[0]);
                                                        rPlans.Add(data7[2]);
                                                        if (rType.Distinct().Count() != 1 && rPlans.Distinct().Count() != 1)
                                                            samePackage = false;
                                                    }
                                                    if (samePackage)
                                                    {
                                                        XElement Currentgroup = new XElement("Room",
                                                                            new XElement("type", rm1),
                                                                            new XElement("type", rm2),
                                                                            new XElement("type", rm3),
                                                                            new XElement("type", rm4),
                                                                            new XElement("type", rm5),
                                                                            new XElement("type", rm6),
                                                                            new XElement("type", rm7));
                                                        foreach (string rt in Roomtypes)
                                                        {
                                                            CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { ',' })[0].Equals(rt)).Count().ToString() + "-";
                                                        }
                                                        CurrentConfig += meal.Attribute("Name").Value;
                                                        Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                        if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                            FinalGroup.Add(Currentgroup);
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
                                                        List<string> rType = new List<string>();
                                                        List<string> rPlans = new List<string>();
                                                        bool samePackage = true;
                                                        if (Package)
                                                        {
                                                            string[] data1 = rm1.Split(new char[] { ',' });
                                                            rType.Add(data1[0]);
                                                            rPlans.Add(data1[2]);
                                                            string[] data2 = rm2.Split(new char[] { ',' });
                                                            rType.Add(data2[0]);
                                                            rPlans.Add(data2[2]);
                                                            string[] data3 = rm3.Split(new char[] { ',' });
                                                            rType.Add(data3[0]);
                                                            rPlans.Add(data3[2]);
                                                            string[] data4 = rm4.Split(new char[] { ',' });
                                                            rType.Add(data4[0]);
                                                            rPlans.Add(data4[2]);
                                                            string[] data5 = rm5.Split(new char[] { ',' });
                                                            rType.Add(data5[0]);
                                                            rPlans.Add(data5[2]);
                                                            string[] data6 = rm6.Split(new char[] { ',' });
                                                            rType.Add(data6[0]);
                                                            rPlans.Add(data6[2]);
                                                            string[] data7 = rm7.Split(new char[] { ',' });
                                                            rType.Add(data7[0]);
                                                            rPlans.Add(data7[2]);
                                                            string[] data8 = rm8.Split(new char[] { ',' });
                                                            rType.Add(data8[0]);
                                                            rPlans.Add(data8[2]);
                                                            if (rType.Distinct().Count() != 1 && rPlans.Distinct().Count() != 1)
                                                                samePackage = false;
                                                        }
                                                        if (samePackage)
                                                        {
                                                            XElement Currentgroup = new XElement("Room",
                                                                                new XElement("type", rm1),
                                                                                new XElement("type", rm2),
                                                                                new XElement("type", rm3),
                                                                                new XElement("type", rm4),
                                                                                new XElement("type", rm5),
                                                                                new XElement("type", rm6),
                                                                                new XElement("type", rm7),
                                                                                new XElement("type", rm8));
                                                            foreach (string rt in Roomtypes)
                                                            {
                                                                CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { ',' })[0].Equals(rt)).Count().ToString() + "-";
                                                            }
                                                            CurrentConfig += meal.Attribute("Name").Value;
                                                            Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                            if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                                FinalGroup.Add(Currentgroup);
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
                                                            List<string> rType = new List<string>();
                                                            List<string> rPlans = new List<string>();
                                                            bool samePackage = true;
                                                            if (Package)
                                                            {
                                                                string[] data1 = rm1.Split(new char[] { ',' });
                                                                rType.Add(data1[0]);
                                                                rPlans.Add(data1[2]);
                                                                string[] data2 = rm2.Split(new char[] { ',' });
                                                                rType.Add(data2[0]);
                                                                rPlans.Add(data2[2]);
                                                                string[] data3 = rm3.Split(new char[] { ',' });
                                                                rType.Add(data3[0]);
                                                                rPlans.Add(data3[2]);
                                                                string[] data4 = rm4.Split(new char[] { ',' });
                                                                rType.Add(data4[0]);
                                                                rPlans.Add(data4[2]);
                                                                string[] data5 = rm5.Split(new char[] { ',' });
                                                                rType.Add(data5[0]);
                                                                rPlans.Add(data5[2]);
                                                                string[] data6 = rm6.Split(new char[] { ',' });
                                                                rType.Add(data6[0]);
                                                                rPlans.Add(data6[2]);
                                                                string[] data7 = rm7.Split(new char[] { ',' });
                                                                rType.Add(data7[0]);
                                                                rPlans.Add(data7[2]);
                                                                string[] data8 = rm8.Split(new char[] { ',' });
                                                                rType.Add(data8[0]);
                                                                rPlans.Add(data8[2]);
                                                                string[] data9 = rm9.Split(new char[] { ',' });
                                                                rType.Add(data9[0]);
                                                                rPlans.Add(data9[2]);
                                                                if (rType.Distinct().Count() != 1 && rPlans.Distinct().Count() != 1)
                                                                    samePackage = false;
                                                            }
                                                            if (samePackage)
                                                            {
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
                                                                foreach (string rt in Roomtypes)
                                                                {
                                                                    CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Split(new char[] { ',' })[0].Equals(rt)).Count().ToString() + "-";
                                                                }
                                                                CurrentConfig += meal.Attribute("Name").Value;
                                                                Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                                if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                                    FinalGroup.Add(Currentgroup);
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
                    #endregion

                }
                #endregion               
                int index = 1;
                XElement RatePlansDone = new XElement("Rooms");
                foreach (XElement room in FinalGroup.Descendants("Room"))
                {
                    XElement RoomTag = Roomtag(room, SalRooms, index++, Req, RatePlansDone);
                    if (RoomTag != null)
                    {
                        Rooms.Add(RoomTag);
                        RatePlansDone.Add(new XElement("Room", new XElement("RoomType", getIdBySplit(RoomTag.Element("Room").Attribute("ID").Value)),
                                        new XElement("RatePlanCode", RoomTag.Element("Room").Attribute("SessionID").Value)));
                    }
                    
                }
            }
            catch (Exception ex)
            {

                #region Exception
                CustomException ce = new CustomException(ex);
                ce.MethodName = "RoomGrouping";
                ce.PageName = "SalServices";
                ce.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ce.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                #endregion
            }

            return Rooms;
        } 
        #endregion
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
                DateTime starttime = DateTime.Now;
                List<XElement> logResponse = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 2, suplierID);
                XElement Hotel = logResponse.Where(x => x.Descendants("BasicPropertyInfo").FirstOrDefault().Attribute("HotelCode").Value.Equals(Req.Descendants("HotelID").FirstOrDefault().Value))
                                        .FirstOrDefault().Descendants("RoomStay").FirstOrDefault();
                List<XElement> PolicyList = new List<XElement>();
                foreach (XElement room in Req.Descendants("Room"))
                    PolicyList.Add(new XElement("Policies", new XAttribute("RoomID", room.Attribute("ID").Value),
                Hotel.Descendants("RoomRate").Where(x => x.Attribute("RoomTypeCode").Value.Equals(room.Attribute("ID").Value.Split(new char[] { ',' })[0])).FirstOrDefault().Descendants("CancelPolicies").FirstOrDefault()));
                DateTime checkin = DateTime.ParseExact(Req.Descendants("FromDate").FirstOrDefault().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                XElement CxPolicies = new XElement("CancellationPolicies"); 
                bool CXPolicyNotFound = PolicyList.Descendants("CancelPolicies").Select(x => x.Attribute("CancelPolicyIndicator").Value.ToUpper()).ToList().Contains("FALSE");
                if (CXPolicyNotFound)
                {
                    #region Get CXPolicy from supplier
                    PolicyList = new List<XElement>();
                    string HotelCity = LogXMLs(Req.Descendants("TransactionID").FirstOrDefault().Value, 1, 19).Where(x => x.Descendants("Response").Descendants("RoomStay").Any())
                                    .Descendants("HotelRef").FirstOrDefault().FirstAttribute.Value;
                    #region Supplier Credentials
                    XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "19");
                    string sup_username = suppliercred.Descendants("username").FirstOrDefault().Value;
                    string sup_password = suppliercred.Descendants("password").FirstOrDefault().Value;
                    string action = suppliercred.Descendants("cancelPolicyURL").FirstOrDefault().Value;
                    #endregion
                    #region Log Save
                    SalTours_Logs model = new SalTours_Logs();
                    model.CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value);
                    model.TrackNo = Req.Descendants("TransID").FirstOrDefault().Value;
                    model.Logtype = "CXLPolicy";
                    model.LogtypeID = 3;
                    #endregion
                    foreach (XElement Room in Req.Descendants("Room"))
                    {
                        XDocument CancelPolicyReq = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                                                    new XElement(xmlns + "Envelope",
                                                        new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                                        new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                                        new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                        new XElement(xmlns + "Header",
                                                            new XElement(securityNamespace + "Security",
                                                                new XAttribute(XNamespace.None + "xmlns", securityNamespace),
                                                                new XElement(securityNamespace + "UsernameToken",
                                                                    new XElement(securityNamespace + "Username", sup_username),
                                                                    new XElement(securityNamespace + "Password", sup_password)))),
                                                        new XElement(xmlns + "Body",
                                                            new XElement(xmlns + "OTA_HotelBookingRuleRQ",
                                                                new XAttribute("Version", "0"),
                                                                new XElement(OTANamespace + "POS",
                                                                    new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                    new XElement(OTANamespace + "Source",
                                                                        new XAttribute("ISOCountry", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value))),
                                                                    new XElement(OTANamespace + "RuleMessage",
                                                                        new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                        new XAttribute("HotelCode", Req.Descendants("HotelID").FirstOrDefault().Value),
                                                                        new XAttribute("HotelCityCode", HotelCity),
                                                                        new XElement(OTANamespace + "StatusApplication",
                                                                            new XAttribute("NumberOfUnits", Req.Descendants("RoomPax").Count()),
                                                                            new XAttribute("Start", reformatDate(Req.Descendants("FromDate").FirstOrDefault().Value)),
                                                                            new XAttribute("End", reformatDate(Req.Descendants("ToDate").FirstOrDefault().Value)),
                                                                            new XAttribute("Duration", getNights(Req.Descendants("FromDate").FirstOrDefault().Value, Req.Descendants("ToDate").FirstOrDefault().Value)),
                                                                            new XAttribute("InvCode", getIdBySplit(Room.Attribute("ID").Value)),
                                                                            new XAttribute("RatePlanCode", Room.Attribute("SessionID").Value)))))));
                        XDocument SupplierResponse = servreq.SalRequest(CancelPolicyReq, action, model, model.CustomerID.ToString());
                        XElement response = removeAllNamespaces(SupplierResponse.Root);
                        if (!response.Descendants("Error").Any())
                        {
                            PolicyList.Add(new XElement("Policies", new XAttribute("RoomID", Room.Attribute("ID").Value),
                                new XElement("CancelPolicies",
                                response.Descendants("CancelPenalties").ToList())));
                        }
                        else
                        {
                            #region Error
                            XElement cancelPolresp = new XElement(
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
                                                                          new XElement("ErrorTxt", response.Descendants("Error").FirstOrDefault().Attribute("ShortText").Value))));
                            return cancelPolresp;
                            #endregion
                        } 
                    
                    }
                    foreach(XElement policy in PolicyList.Descendants("CancelPenalty"))
                    {
                        int daysBefore = Convert.ToInt32(policy.Element("Deadline").Attribute("OffsetUnitMultiplier").Value);
                        DateTime lastcancellationDate = checkin.AddDays(-daysBefore);
                        double amount = Convert.ToDouble(policy.Element("AmountPercent").Attribute("Amount").Value);
                        CxPolicies.Add(new XElement("CancellationPolicy",
                                            new XAttribute("LastCancellationDate", lastcancellationDate.ToString("dd/MM/yyyy")),
                                            new XAttribute("ApplicableAmount", amount.ToString()),
                                            new XAttribute("NoShowPolicy", "0")));
                    }
                    #endregion
                }
                else
                {
                    #region Log Save
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                        log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                        log.SupplierID = 19;
                        log.logrequestXML = removeAllNamespaces(Req).ToString();
                        log.logresponseXML = new XElement("PolicyList", PolicyList).ToString();
                        log.LogTypeID = 3;
                        log.LogType = "CXLPolicy";
                        log.StartTime = starttime;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "RoomAvailability";
                        ex1.PageName = "SalServices";
                        ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                        ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    #endregion
                    foreach (XElement policies in PolicyList)
                    {

                        foreach (XElement policy in policies.Descendants("CancelPenalty"))
                        {
                            int daysBefore = Convert.ToInt32(policy.Element("Deadline").Attribute("OffsetUnitMultiplier").Value);
                            DateTime lastcancellationDate = checkin.AddDays(-daysBefore);
                            double amount = Convert.ToDouble(policy.Element("AmountPercent").Attribute("Amount").Value);
                            CxPolicies.Add(new XElement("CancellationPolicy",
                                                new XAttribute("LastCancellationDate", lastcancellationDate.ToString("dd/MM/yyyy")),
                                                new XAttribute("ApplicableAmount", amount.ToString()),
                                                new XAttribute("NoShowPolicy", "0")));

                        }
                    }
                }
                List<XElement> mergeinput = new List<XElement>();
                mergeinput.Add(CxPolicies);
                XElement finalcp = MergCxlPolicy(mergeinput);
                #region Response XML
                if (finalcp.Descendants("CancellationPolicy").Any() && finalcp.Descendants("CancellationPolicy").Last().HasAttributes)
                {
                    var cxp = new XElement("Hotels",
                             new XElement("Hotel",
                                 new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                 new XElement("HotelName", Req.Descendants("HotelName").FirstOrDefault().Value),
                                 new XElement("HotelImgSmall"),
                                 new XElement("HotelImgLarge"),
                                 new XElement("MapLink"),
                                 new XElement("DMC", "Restel"),
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
                }
                else
                {
                    cancelresponse.Add(new XElement("ErrorTxt", "No Cancellation Policy found"));
                }
                #endregion
            }
            catch (Exception ex)
            {                
                #region Exception
                CustomException ce = new CustomException(ex);
                ce.MethodName = "CancellationPolicy";
                ce.PageName = "SalServices";
                ce.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ce.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
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
        #region Pre Booking
        public XElement PreBooking(XElement Req, string xmlout)
         {
             DMC = xmlout;
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            #region Adding Occupancy Id
            int id = 1;
            foreach (XElement room in Req.Descendants("RoomPax"))
                room.Add(new XElement("id", id++.ToString()));
            #endregion
            XElement PreBookingResponse = new XElement("HotelPreBookingResponse");
            XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "19");
            string susername = suppliercred.Descendants("username").FirstOrDefault().Value;
            string spassword = suppliercred.Descendants("password").FirstOrDefault().Value;
            string action = suppliercred.Descendants("bookURL").FirstOrDefault().Value;
            XElement SupplierResp = null;
            try
            {
                string hotelID = Req.Descendants("HotelID").FirstOrDefault().Value;                
                List<XElement> logResponse = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 1, 19);
                DateTime starttime = DateTime.Now;
                //string HotelCity = logResponse.Where(x => x.Descendants("Response").FirstOrDefault().Descendants("RoomStay").Any() && x.Descendants("Response").FirstOrDefault().Descendants("RoomStay").Where(y => y.Descendants("BasicPropertyInfo").FirstOrDefault().Attribute("HotelCode").Equals(hotelID)).Any())
                //                    .FirstOrDefault().Descendants("HotelRef").FirstOrDefault().FirstAttribute.Value;
                //XElement request = logResponse.Where(x => x.Descendants("Response").Where(y => y.Descendants("RoomStay").Any() &&
                //                    y.Descendants("RoomStay").Where(z => z.Descendants("BasicPropertyInfo").FirstOrDefault().Attribute("HotelCode").Equals(hotelID)).Any()).Any())
                //                    .FirstOrDefault();
                string HotelCity = Req.Descendants("Room").FirstOrDefault().Descendants("RequestID").FirstOrDefault().Value;
                //string HotelCity = logResponse.Where(x => x.Descendants("Response").Where(y=> y.Descendants("RoomStay").Any() &&
                //                    y.Descendants("RoomStay").Where(z => z.Descendants("BasicPropertyInfo").FirstOrDefault().Attribute("HotelCode").Equals(hotelID)).Any()).Any())
                //                    .FirstOrDefault().Descendants("HotelRef").FirstOrDefault().FirstAttribute.Value;
                //string HotelCitytest = logResponse.Where(x => x.Descendants("Response").Where(y => y.Descendants("RoomStay").Any()).Any())
                //                    .FirstOrDefault().Descendants("HotelRef").FirstOrDefault().FirstAttribute.Value;
                //string HotelCity = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 1, 19).Where(x => x.Descendants("Response").Descendants("RoomStay").Any() && x.Descendants("Response").Descendants("Roomstay").Where(y => y.Element("BasicPropertyInfo").Attribute("HotelCode").Value.Equals(hotelID)).Any())
                //                    .Descendants("HotelRef").FirstOrDefault().FirstAttribute.Value;
                XElement hotel = logResponse.Descendants("RoomStay").Where(x => x.Descendants("BasicPropertyInfo").FirstOrDefault().Attribute("HotelCode").Value.Equals(hotelID))
                                    .FirstOrDefault();
                XElement RequestElement = new XElement(Req);
                RequestElement = ModifyRequest(RequestElement);
                XDocument BookingReq = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                                               new XElement(xmlns + "Envelope",
                                                   new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                                   new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                                   new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                   new XElement(xmlns + "Header",
                                                       new XElement(securityNamespace + "Security",
                                                           new XAttribute(XNamespace.None + "xmlns", securityNamespace),
                                                           new XElement(securityNamespace + "UsernameToken",
                                                               new XElement(securityNamespace + "Username", susername),
                                                                new XElement(securityNamespace + "Password", spassword)))),
                                                    new XElement(xmlns + "Body",
                                                        new XElement(xmlns + "OTA_HotelResRQ",
                                                            new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                            new XAttribute("TransactionStatusCode", "Start"),
                                                            new XAttribute("ResStatus", "Initiate"),
                                                            new XElement(OTANamespace + "POS",
                                                                new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                new XElement(OTANamespace + "Source", new XAttribute("ISOCountry", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value))),
                                                            new XElement(OTANamespace + "HotelReservations",
                                                                new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                new XElement(OTANamespace + "HotelReservation",
                                                                    new XElement(OTANamespace + "UniqueID",
                                                                        new XAttribute("ID", Req.Descendants("TransID").FirstOrDefault().Value),
                                                                        new XAttribute("Instance", "PNR"),
                                                                        new XAttribute("Type", "14")),
                                                                        BookingRoomstay(hotel, RequestElement, HotelCity)))))));
                #region Log Save
                SalTours_Logs model = new SalTours_Logs();
                model.CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value);
                model.Logtype = "PreBook";
                model.LogtypeID = 4;
                model.TrackNo = Req.Descendants("TransID").FirstOrDefault().Value;
                #endregion
                XDocument response = servreq.SalRequest(BookingReq, action, model, Req.Descendants("CustomerID").FirstOrDefault().Value);
                SupplierResp = removeAllNamespaces(response.Root);
                XElement preRooms = PreBookingRooms(hotel, Req);
                StringBuilder TNC = new StringBuilder();
                string comments = SupplierResp.Descendants("Comment").Any()? "<ul><li>"+SupplierResp.Descendants("Comment").FirstOrDefault().Value+"</li></ul>":string.Empty;
                string tnc = preRooms.Descendants("TempTnc").FirstOrDefault().Value;
                TNC.Append(comments);
                TNC.Append(tnc);
                preRooms.Descendants("TempTnc").Remove();
                var prebook = new XElement("Hotels",
                                    new XElement("Hotel",
                                    new XElement("HotelID"),
                                    new XElement("HotelName"),
                                    new XElement("NewPrice"),
                                    new XElement("Status", "true"),
                                    new XElement("TermCondition",TNC.ToString()),
                                    new XElement("HotelImgSmall"),
                                    new XElement("HotelImgLarge"),
                                    new XElement("MapLink"),
                                    new XElement("DMC",DMC),
                                    new XElement("Currency"),
                                    new XElement("Offers"),
                                    new XElement("Rooms", preRooms)));
                PreBookingResponse.Add(prebook);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ce = new CustomException(ex);
                ce.MethodName = "PreBooking";
                ce.PageName = "SalServices";
                ce.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ce.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                #endregion                
            }
            finally
            {
                if (SupplierResp!=null)
                {
                    #region Ignore Initiated Booking
                    #region Log Save
                    SalTours_Logs model = new SalTours_Logs();
                    model.CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value);
                    model.Logtype = "PreBook(Ignore)";
                    model.LogtypeID = 4;
                    model.TrackNo = Req.Descendants("TransID").FirstOrDefault().Value;
                    #endregion
                    XDocument BookingIgnoreReq = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                                               new XElement(xmlns + "Envelope",
                                                   new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                                   new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                                   new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                   new XElement(xmlns + "Header",
                                                       new XElement(securityNamespace + "Security",
                                                           new XAttribute(XNamespace.None + "xmlns", securityNamespace),
                                                           new XElement(securityNamespace + "UsernameToken",
                                                               new XElement(securityNamespace + "Username", susername),
                                                                new XElement(securityNamespace + "Password", spassword)))),
                                                    new XElement(xmlns + "Body",
                                                        new XElement(xmlns + "OTA_HotelResRQ",
                                                            new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                            new XAttribute("TransactionStatusCode", "End"),
                                                            new XAttribute("ResStatus", "Ignore"),
                                                            new XAttribute("TimeStamp", DateTime.UtcNow),
                                                            new XAttribute("TransactionIdentifier", model.TrackNo),
                                                            new XAttribute("Version", "1.002"),
                                                            new XElement(OTANamespace + "HotelReservations",
                                                                new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                new XElement(OTANamespace + "HotelReservation",
                                                                    new XElement(OTANamespace + "UniqueID",
                                                                        new XAttribute("ID", Req.Descendants("TransID").FirstOrDefault().Value),
                                                                        new XAttribute("Instance", "PNR"),
                                                                        new XAttribute("Type", "14")),
                                                                       new XElement(OTANamespace + "ResGlobalInfo",
                                                                           new XElement(OTANamespace+"HotelReservationIDs",
                                                                               new XElement(OTANamespace+"HotelReservationID",
                                                                                   new XAttribute("ResID_Type", SupplierResp.Descendants("UniqueID").FirstOrDefault().Attribute("Type").Value),
                                                                                   new XAttribute("ResID_Value", SupplierResp.Descendants("UniqueID").FirstOrDefault().Attribute("ID").Value))))))))));
                    XDocument IgnoreResponse = servreq.SalRequest(BookingIgnoreReq, action, model, model.CustomerID.ToString());
                    #endregion 
                }
            }
            #region Response Format
            string oldprice = Req.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value;
            string newprice = PreBookingResponse.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value;
            XElement prebookingfinal = new XElement(
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
                                                        new XElement(Req.Descendants("HotelPreBookingRequest").FirstOrDefault()),
                                                       PreBookingResponse)));
            #region Price Change Condition
            if (oldprice.Equals(newprice))
                return prebookingfinal;
            else
            {
                prebookingfinal.Descendants("HotelPreBookingResponse").Descendants("Hotels").FirstOrDefault().AddBeforeSelf(
                   new XElement("ErrorTxt", "Amount has been changed"),
                   new XElement("NewPrice", newprice));
                return prebookingfinal;
            }
            #endregion
            #endregion
        }
        #region Rooms For Pre Booking
        public XElement PreBookingRooms(XElement hotel, XElement Req)
        {
            string tnc = string.Empty;
            XElement RoomTypes = new XElement("RoomTypes",
                                        new XAttribute("TotalRate", Req.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value),
                                        new XAttribute("Index", "1"));
            try
            {
                double total = 0.0;
                List<XElement> PolicyList = new List<XElement>();
                int roomSeq = 1;
                foreach (XElement room1 in Req.Descendants("Room"))
                {
                    string[] splitCode = room1.Attribute("ID").Value.Split(new char[] { ',' });
                    XElement room = hotel.Descendants("RoomRate").Where(x => x.Attribute("RoomTypeCode").Value.Equals(splitCode[0])
                                   && x.Attribute("RatePlanCode").Value.Equals(room1.Attribute("SessionID").Value)).FirstOrDefault();
                    XElement guest = Req.Descendants("RoomPax").Where(x => x.Element("id").Value.Equals(splitCode[1])).FirstOrDefault();
                    if(room.Descendants("MealDescription").Any())
                    {
                        foreach(XElement mealText in room.Descendants("MealDescription"))
                        {
                            tnc += "<ul><li>" + mealText.Element("Text").Value + "</li></ul>";
                        }
                    }
                    string PromotionText = string.Empty;
                    double discount = room.Descendants("Discount").Any() ? Convert.ToDouble(room.Descendants("Discount").FirstOrDefault().Attribute("AmountAfterTax").Value) : 0.00, percentageDiscount = 0.00;
                    if (discount > 0)
                    {
                        double basePrice = Convert.ToDouble(room.Descendants("Rate").Where(x => x.Attribute("ChargeType").Value.Equals("26")).FirstOrDefault().Element("Base").Attribute("AmountAfterTax").Value);
                        percentageDiscount = (discount / basePrice) * 100;
                        percentageDiscount = Math.Round(percentageDiscount, 2);
                        PromotionText = percentageDiscount.ToString() + "% discount(Already apllied)";
                    }
                    List<XElement> SuppList = new List<XElement>();
                    if (room.Descendants("Fees").Any() && room.Descendants("Fees").FirstOrDefault().HasElements)
                    {
                        foreach (XElement supl in room.Descendants("Fee"))
                        {
                            if (supl.Attribute("Code").Value.Equals("City hotel fee"))
                                SuppList.Add(new XElement("Supplement",
                                                    new XAttribute("suppId", ""),
                                                    new XAttribute("suppName", supl.Attribute("Code").Value),
                                                    new XAttribute("supptType", ""),
                                                    new XAttribute("suppIsMandatory", supl.Attribute("MandatoryIndicator").Value),
                                                    new XAttribute("suppChargeType", "AtProperty"),
                                                    new XAttribute("suppPrice", supl.Attribute("Amount").Value),
                                                    new XAttribute("suppType", supl.Attribute("ChargeFrequency").Value)));
                            else
                                SuppList.Add(new XElement("Supplement",
                                                    new XAttribute("suppId", ""),
                                                    new XAttribute("suppName", supl.Attribute("Code").Value),
                                                    new XAttribute("supptType", ""),
                                                    new XAttribute("suppIsMandatory", supl.Attribute("MandatoryIndicator").Value),
                                                    new XAttribute("suppChargeType", supl.Attribute("ChargeUnit").Value),
                                                    new XAttribute("suppPrice", supl.Attribute("Amount").Value),
                                                    new XAttribute("suppType", supl.Attribute("ChargeFrequency").Value)));
                        }
                    }
                    string roomtype = hotel.Descendants("RoomType").Where(x => x.Attribute("RoomTypeCode").Value.Equals(room.Attribute("RoomTypeCode").Value))
                                      .FirstOrDefault().Descendants("Text").FirstOrDefault().Value;
                    int nights = room.Descendants("Rate").Where(x => x.Attributes("EffectiveDate").Any()).Count();
                    RoomTypes.Add(new XElement("Room",
                                                       new XAttribute("ID", splitCode[0]),
                                                          new XAttribute("SuppliersID", "19"),
                                                          new XAttribute("RoomSeq", roomSeq++),
                                                          new XAttribute("SessionID", room.Attribute("RatePlanCode").Value),
                                                          new XAttribute("RoomType", roomtype),
                                                          new XAttribute("OccupancyID", splitCode[1]),
                                                          new XAttribute("OccupancyName", ""),
                                                          new XAttribute("MealPlanID", ""),
                                                          new XAttribute("MealPlanName", room.Descendants("Meal").Any() ? room.Descendants("Meal").First().Attribute("Name").Value : "Room Only"),
                                                          new XAttribute("MealPlanCode", room.Descendants("Meal").Any() ? MealPlanCode(room.Descendants("Meal").First().Attribute("Name").Value) : "RO"),
                                                          new XAttribute("MealPlanPrice", ""),
                                                          new XAttribute("PerNightRoomRate", Convert.ToString(Convert.ToDouble(room.Descendants("Total").FirstOrDefault().Attribute("AmountAfterTax").Value) / nights)),
                                                          new XAttribute("TotalRoomRate", room.Descendants("Total").FirstOrDefault().Attribute("AmountAfterTax").Value),
                                                          new XAttribute("CancellationDate", ""),
                                                          new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", "true"),
                                                          new XElement("RequestID", Req.Descendants("Room").FirstOrDefault().Descendants("RequestID").FirstOrDefault().Value),
                                                          new XElement("Offers"),
                                                          new XElement("PromotionList",
                                                          new XElement("Promotions", PromotionText)),
                                                          new XElement("CancellationPolicy"),
                                                          new XElement("Amenities",
                                                              new XElement("Amenity")),
                                                          new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                          new XElement("Supplements", SuppList),
                                                          pb(room.Descendants("Rate").Where(x => x.Attributes("EffectiveDate").Any()).ToList(), percentageDiscount),
                                                          new XElement("AdultNum", guest.Element("Adult").Value),
                                                          new XElement("ChildNum", guest.Element("Child").Value)));
                    total += Convert.ToDouble(room.Descendants("Rate").Where(x => x.Attribute("ChargeType").Value.Equals("26")).FirstOrDefault().Element("Base").Attribute("AmountAfterTax").Value);


                    PolicyList.Add(new XElement("Policies", new XAttribute("RoomID", splitCode[0]),
                        room.Descendants("CancelPolicies").FirstOrDefault()));

                }
                DateTime checkin = DateTime.ParseExact(Req.Descendants("FromDate").FirstOrDefault().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                XElement CxPolicies = new XElement("CancellationPolicies");
                foreach (XElement policies in PolicyList)
                {
                    foreach (XElement policy in policies.Descendants("CancelPenalty"))
                    {
                        int daysBefore = Convert.ToInt32(policy.Element("Deadline").Attribute("OffsetUnitMultiplier").Value);
                        DateTime lastcancellationDate = checkin.AddDays(-daysBefore);
                        double amount = Convert.ToDouble(policy.Element("AmountPercent").Attribute("Amount").Value);
                        CxPolicies.Add(new XElement("CancellationPolicy",
                                            new XAttribute("LastCancellationDate", lastcancellationDate.ToString("dd/MM/yyyy")),
                                            new XAttribute("ApplicableAmount", amount.ToString()),
                                            new XAttribute("NoShowPolicy", "0")));

                    }
                }
                List<XElement> mergeinput = new List<XElement>();
                mergeinput.Add(CxPolicies);
                XElement finalcp = MergCxlPolicy(mergeinput);
                RoomTypes.Descendants("Room").LastOrDefault().AddAfterSelf(finalcp);
                RoomTypes.Add(new XElement("TempTnc", tnc));
                //RoomTypes.Attribute("TotalRate").SetValue(total.ToString());
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ce = new CustomException(ex);
                ce.MethodName = "PreBookRooms";
                ce.PageName = "SalServices";
                ce.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ce.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                #endregion                
            }
            return RoomTypes;
        }
        #endregion
        #endregion
        #region Booking
        public XElement Booking(XElement Req)
        {
            string hotelID = Req.Descendants("HotelID").FirstOrDefault().Value;
            XElement BookingResponse = new XElement("HotelBookingResponse");            
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                List<XElement> logResponse = LogXMLs(Req.Descendants("TransactionID").FirstOrDefault().Value, 2, 19);
                string HotelCity = Req.Descendants("Room").FirstOrDefault().Descendants("RequestID").FirstOrDefault().Value;
                //string HotelCity = logResponse.Where(x => x.Descendants("Response").FirstOrDefault().Descendants("RoomStay").Any() && x.Descendants("Response").FirstOrDefault().Descendants("RoomStay").Where(y => y.Element("BasicPropertyInfo").Attribute("HotelCode").Equals(hotelID)).Any())
                //                    .Descendants("HotelRef").FirstOrDefault().FirstAttribute.Value;
                XElement hotel = logResponse.Descendants("RoomStay").Where(x => x.Element("BasicPropertyInfo").Attribute("HotelCode").Value.Equals(hotelID)).FirstOrDefault();
                #region Supplier Credential
                XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "19");
                string sup_username = suppliercred.Descendants("username").FirstOrDefault().Value;
                string sup_password = suppliercred.Descendants("password").FirstOrDefault().Value;
                string action = suppliercred.Descendants("bookURL").FirstOrDefault().Value;
                #endregion
                #region Initiate Booking
                XDocument BookingReq = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                                               new XElement(xmlns + "Envelope",
                                                   new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                                   new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                                   new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                   new XElement(xmlns + "Header",
                                                       new XElement(securityNamespace + "Security",
                                                           new XAttribute(XNamespace.None + "xmlns", securityNamespace),
                                                           new XElement(securityNamespace + "UsernameToken",
                                                               new XElement(securityNamespace + "Username", sup_username),
                                                                new XElement(securityNamespace + "Password", sup_password)))),
                                                    new XElement(xmlns + "Body",
                                                        new XElement(xmlns + "OTA_HotelResRQ",
                                                            new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                            new XAttribute("TransactionStatusCode", "Start"),
                                                            new XAttribute("ResStatus", "Initiate"),
                                                            new XElement(OTANamespace + "POS",
                                                                new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                new XElement(OTANamespace + "Source", new XAttribute("ISOCountry", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value))),
                                                            new XElement(OTANamespace + "HotelReservations",
                                                                new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                new XElement(OTANamespace + "HotelReservation",
                                                                    new XElement(OTANamespace + "UniqueID",
                                                                        new XAttribute("ID", Req.Descendants("TransID").FirstOrDefault().Value),
                                                                        new XAttribute("Instance", "PNR"),
                                                                        new XAttribute("Type", "14")),
                                                                        BookingRoomstay(hotel, Req, HotelCity)))))));
                BookingReq.Descendants(OTANamespace + "BasicPropertyInfo").FirstOrDefault().AddAfterSelf(
                    new XElement(OTANamespace + "Comments",
                        new XElement(OTANamespace + "Comment",
                            new XElement(OTANamespace + "Text", Req.Descendants("SpecialRemarks").FirstOrDefault().Value))));
                #region Log Save
                SalTours_Logs model = new SalTours_Logs();
                model.CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value);
                model.Logtype = "Book(Initiate)";
                model.LogtypeID = 5;
                model.TrackNo = Req.Descendants("TransactionID").FirstOrDefault().Value;
                #endregion
                XDocument SupplResponse = servreq.SalRequest(BookingReq, action, model, Req.Descendants("CustomerID").FirstOrDefault().Value);
                XElement InitateResponse = removeAllNamespaces(SupplResponse.Root);
                #endregion
                if (InitateResponse.Descendants("OTA_HotelResRS").Descendants("UniqueID").Any() && InitateResponse.Descendants("OTA_HotelResRS").Descendants("UniqueID").FirstOrDefault().Attribute("Type").Value.Equals("16"))
                {
                    #region Commit Booking
                    XDocument BookingConfirmationRequest = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                                                                  new XElement(xmlns + "Envelope",
                                                                      new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                                                      new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                                                      new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                                      new XElement(xmlns + "Header",
                                                                          new XElement(securityNamespace + "Security",
                                                                          new XAttribute(XNamespace.None + "xmlns", securityNamespace),
                                                                          new XElement(securityNamespace + "UsernameToken",
                                                                              new XElement(securityNamespace + "Username", sup_username),
                                                                              new XElement(securityNamespace + "Password", sup_password)))),
                                                                       new XElement(xmlns + "Body",
                                                                           new XElement(xmlns + "OTA_HotelResRQ",
                                                                               new XAttribute("TransactionStatusCode", "End"),
                                                                               new XAttribute("Version", "1.002"),
                                                                               new XAttribute("TimeStamp", DateTime.Now),
                                                                               new XAttribute("TransactionIdentifier", Req.Descendants("TransactionID").FirstOrDefault().Value),
                                                                               new XAttribute("ResStatus", "Commit"),
                                                                               new XElement(OTANamespace + "HotelReservations",
                                                                                   new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                                   new XElement(OTANamespace + "HotelReservation",
                                                                                       new XElement(OTANamespace + "UniqueID",
                                                                                           new XAttribute("ID", Req.Descendants("TransID").FirstOrDefault().Value),
                                                                                           new XAttribute("Instance", "PNR"),
                                                                                           new XAttribute("Type", "14")),
                                                                                       new XElement(OTANamespace + "ResGlobalInfo",
                                                                                           new XElement(OTANamespace + "HotelReservationIDs",
                                                                                               new XElement(OTANamespace + "HotelReservationID",
                                                                                                   new XAttribute("ResID_Type", InitateResponse.Descendants("UniqueID").FirstOrDefault().Attribute("Type").Value),
                                                                                                   new XAttribute("ResID_Value", InitateResponse.Descendants("UniqueID").FirstOrDefault().Attribute("ID").Value))))))))));
                    model.Logtype = "Book(Commit)";
                    XDocument BookingCommitResponse = servreq.SalRequest(BookingConfirmationRequest, action, model, Req.Descendants("CustomerID").FirstOrDefault().Value);
                    XElement supplierBookingCommit = removeAllNamespaces(BookingCommitResponse.Root);
                    #endregion
                    if (supplierBookingCommit.Descendants("OTA_HotelResRS").Any() && supplierBookingCommit.Descendants("OTA_HotelResRS").FirstOrDefault().Attribute("ResResponseType").Value.ToUpper().Equals("COMMITTED")
                        && supplierBookingCommit.Descendants("OTA_HotelResRS").Descendants("UniqueID").Any())
                    {
                        #region Response
                        string confNumber = supplierBookingCommit.Descendants("UniqueID").Where(x => x.Attribute("Type").Value.Equals("14")).FirstOrDefault().Attribute("ID").Value;
                        BookingResponse.Add(new XElement("Hotels",
                                                new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                                new XElement("HotelName", Req.Descendants("HotelName").FirstOrDefault().Value),
                                                new XElement("FromDate", Req.Descendants("HotelName").FirstOrDefault().Value),
                                                new XElement("ToDate", Req.Descendants("HotelName").FirstOrDefault().Value),
                                                new XElement("AdultPax"),
                                                new XElement("ChildPax"),
                                                new XElement("TotalPrice"),
                                                new XElement("CurrencyID"),
                                                new XElement("CurrencyCode"),
                                                new XElement("MarketID"),
                                                new XElement("MarketName"),
                                                new XElement("HotelImgSmall"),
                                                new XElement("HotelImgLarge"),
                                                new XElement("MapLink"),
                                                new XElement("VoucherRemark"),
                                                new XElement("TransID"),
                                                new XElement("ConfirmationNumber", confNumber),
                                                new XElement("Status", "true"),
                                                new XElement("PassengersDetail",
                                                    new XElement("GuestDetails", BookingRespRooms(Req.Descendants("PassengersDetail").FirstOrDefault())))));
                    #endregion
                    }
                    else
                    {
                        #region Ignore Initiated Booking
                        XDocument BookingIgnoreReq = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                                                   new XElement(xmlns + "Envelope",
                                                       new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                                       new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                                       new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                       new XElement(xmlns + "Header",
                                                           new XElement(securityNamespace + "Security",
                                                               new XAttribute(XNamespace.None + "xmlns", securityNamespace),
                                                               new XElement(securityNamespace + "UsernameToken",
                                                                   new XElement(securityNamespace + "Username", sup_username),
                                                                    new XElement(securityNamespace + "Password", sup_password)))),
                                                        new XElement(xmlns + "Body",
                                                            new XElement(xmlns + "OTA_HotelResRQ",
                                                                new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                                new XAttribute("TransactionStatusCode", "End"),
                                                                new XAttribute("ResStatus", "Ignore"),
                                                                new XAttribute("TimeStamp", DateTime.UtcNow),
                                                                new XAttribute("TransactionIdentifier", model.TrackNo),
                                                                new XAttribute("Version", "1.002"),
                                                                new XElement(OTANamespace + "HotelReservations",
                                                                    new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                    new XElement(OTANamespace + "HotelReservation",
                                                                        new XElement(OTANamespace + "UniqueID",
                                                                            new XAttribute("ID", Req.Descendants("TransID").FirstOrDefault().Value),
                                                                            new XAttribute("Instance", "PNR"),
                                                                            new XAttribute("Type", "14")),
                                                                           new XElement(OTANamespace + "ResGlobalInfo",
                                                                               new XElement(OTANamespace+"HotelReservationIDs",
                                                                                   new XElement(OTANamespace+"HotelReservationID",
                                                                                       new XAttribute("ResID_Type", InitateResponse.Descendants("UniqueID").FirstOrDefault().Attribute("Type").Value),
                                                                                       new XAttribute("ResID_Value", InitateResponse.Descendants("UniqueID").FirstOrDefault().Attribute("ID").Value))))))))));
                        model.Logtype = "Book(Ignore)";
                        XDocument IgnoreResponse = servreq.SalRequest(BookingIgnoreReq, action, model, model.CustomerID.ToString());
                        BookingResponse.Add(new XElement("ErrorTxt", supplierBookingCommit.Descendants("Error").Any()? supplierBookingCommit.Descendants("Error").FirstOrDefault().Value: "Booking Commit Failed"));
                        #endregion
                    }                   
                }
                else
                {
                    #region Error
                    BookingResponse.Add(new XElement("ErrorTxt",
                                                InitateResponse.Descendants("Error").Any() ?
                                                InitateResponse.Descendants("Error").FirstOrDefault().Attribute("ShortText").Value : "Booking Initiate failed")); 
                    #endregion
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ce = new CustomException(ex);
                ce.MethodName = "Booking";
                ce.PageName = "SalServices";
                ce.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ce.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                #endregion                
            }
            #region Response Format
            XElement bookingfinal = new XElement(
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
                                                        new XElement(Req.Descendants("HotelBookingRequest").FirstOrDefault()),
                                                       BookingResponse)));                                     
            #endregion
            return bookingfinal;
        }
        #endregion
        #region Booking Cancellation
        public XElement Cancellation(XElement Req)
        {
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            #region Supplier Credential
            XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "19");
            string sup_username = suppliercred.Descendants("username").FirstOrDefault().Value;
            string sup_password = suppliercred.Descendants("password").FirstOrDefault().Value;
            string action = suppliercred.Descendants("cancelURL").FirstOrDefault().Value;
            #endregion
            XElement CancellationResponse = new XElement("HotelCancellationResponse");

            try
            {
                #region Request
                DataTable LogTable = sda.GetLog(Req.Descendants("TransID").FirstOrDefault().Value, 5, 19);
                string requestDate = LogTable.Rows[0]["logcreatedOn"].ToString();
                XElement BookInitiate = XElement.Parse(LogTable.Rows[0]["logrequestXML"].ToString());
                XElement CommitResponse = XElement.Parse(LogTable.Rows[1]["logresponseXML"].ToString());
                List<XElement> IDs = CommitResponse.Descendants("UniqueID").ToList();
                List<XElement> UniqueIDs = new List<XElement>();
                foreach (XElement ID in IDs)
                {
                    UniqueIDs.Add(new XElement(OTANamespace + "UniqueID",
                        new XAttribute("ID", ID.Attribute("ID").Value),
                        new XAttribute("Type", ID.Attribute("Type").Value),
                        new XAttribute(XNamespace.None + "xmlns", OTANamespace)));
                }
                string start = BookInitiate.Descendants("TimeSpan").FirstOrDefault().Attribute("Start").Value,
                    end = BookInitiate.Descendants("TimeSpan").FirstOrDefault().Attribute("End").Value;
                XElement guest = BookInitiate.Descendants("ResGuest").Where(x => x.Attribute("PrimaryIndicator").Value.ToUpper().Equals("TRUE")).FirstOrDefault();
                #region Request XML
                XDocument CancellationRequest = new XDocument(new XDeclaration("1.0", "utf-16", "yes"),
                                                           new XElement(xmlns + "Envelope",
                                                               new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                                               new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                                               new XAttribute(XNamespace.None + "xmlns", xmlns),
                                                               new XElement(xmlns + "Header",
                                                                   new XElement(securityNamespace + "Security",
                                                                       new XAttribute(XNamespace.None + "xmlns", securityNamespace),
                                                                       new XElement(securityNamespace + "UsernameToken",
                                                                           new XElement(securityNamespace + "Username", sup_username),
                                                                           new XElement(securityNamespace + "Password", sup_password)))),
                                                               new XElement(xmlns + "Body",
                                                                   new XElement(xmlns + "OTA_CancelRQ",
                                                                       new XAttribute("CancelType", "Commit"),
                                                                       new XAttribute("TransactionStatusCode", "End"),
                                                                       new XElement(OTANamespace + "POS",
                                                                           new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                           new XElement(OTANamespace + "Source",
                                                                               new XElement(OTANamespace + "TPA_Extension",
                                                                                   new XAttribute("RequestUserId", sup_username),
                                                                                   new XAttribute("RequestDate", requestDate)))),
                                                                       UniqueIDs,
                                                                       new XElement(OTANamespace + "Verification",
                                                                           new XAttribute(XNamespace.None + "xmlns", OTANamespace),
                                                                           new XElement(OTANamespace + "PersonName",
                                                                               new XElement(OTANamespace + "NamePrefix", guest.Descendants("NamePrefix").FirstOrDefault().Value),
                                                                               new XElement(OTANamespace + "GivenName", guest.Descendants("GivenName").FirstOrDefault().Value),
                                                                               new XElement(OTANamespace + "Surname", guest.Descendants("Surname").FirstOrDefault().Value)),
                                                                           new XElement(OTANamespace + "ReservationTimeSpan",
                                                                               new XAttribute("Duration", Duration(start, end)),
                                                                               new XAttribute("Start", start),
                                                                               new XAttribute("End", end)))))));
                #endregion
                #region Log Save
                string CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                SalTours_Logs model = new SalTours_Logs();
                model.CustomerID = Convert.ToInt32(CustomerID);
                model.TrackNo = Req.Descendants("TransID").FirstOrDefault().Value;
                model.Logtype = "Cancel";
                model.LogtypeID = 6;
                #endregion 
                #endregion
                #region Response
                XDocument response = servreq.SalRequest(CancellationRequest, action, model, CustomerID);
                XElement SupplierRespons = removeAllNamespaces(response.Root);
                if (SupplierRespons.Descendants("OTA_CancelRS").Any() && SupplierRespons.Descendants("OTA_CancelRS").FirstOrDefault().Attribute("Status").Value.ToUpper().Equals("CANCELLED"))
                {
                    #region Response XML
                    CancellationResponse.Add(new XElement("Rooms",
                                                            new XElement("Room",
                                                                new XElement("Cancellation",
                                                                    new XElement("Amount", SupplierRespons.Descendants("CancelRule").FirstOrDefault().Attribute("Amount").Value),
                                                                    new XElement("Status", "Success"))))); 
                    #endregion
                }
                else 
                {
                    #region Error
                    CancellationResponse.Add(new XElement("ErrorTxt", SupplierRespons.Descendants("Errors").Any() ?
                                    SupplierRespons.Descendants("Error").FirstOrDefault().Attribute("ShortText").Value : "Cancellation Failed. Please check supplier and exception logs for more details")); 
                    #endregion
                }                
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ce = new CustomException(ex);
                ce.MethodName = "Booking";
                ce.PageName = "SalServices";
                ce.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ce.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                #endregion                
            }
            #region Response Format
            XElement Cancellationfinal = new XElement(
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
                                                            new XElement(Req.Descendants("HotelCancellationRequest").FirstOrDefault()),
                                                           CancellationResponse))); 
            #endregion
            return Cancellationfinal;

        }
        #endregion
        


        #region Common Functions
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
        #region Room Tag for B2B
        public XElement Roomtag(XElement roomElement, XElement hotelElement, int index, XElement Req, XElement RatePlansDone)
        {

            XElement RoomTypes = new XElement("RoomTypes",
                                         new XAttribute("TotalRate", ""),
                                         new XAttribute("HtlCode", hotelcode), 
                                         new XAttribute("CrncyCode", supplierCurrency), 
                                         new XAttribute("DMCType", DMC),
                                         new XAttribute("Index", Convert.ToString(index)));
            int cnt = 1;
           
            double total = 0.0;
            List<string> meals = new List<string>();
            foreach (XElement room1 in roomElement.Descendants("type"))
            {
                string[] splitCode = room1.Value.Split(new char[] { ',' });
                List<string> RoomRates = hotelElement.Descendants("RoomRate").Where(x => x.Attribute("RoomTypeCode").Value.Equals(splitCode[0]) && x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(splitCode[1]))
                                            .Select(x => x.Attribute("RatePlanCode").Value).ToList();
                //List<string> RoomRatesDone = RoomTypes.Descendants("Room").Where(x => x.Attribute("ID").Value.Equals(splitCode[0])).Select(x => x.Attribute("SessionID").Value).ToList();
                List<string> RoomRatesDone = new List<string>();
                if(RatePlansDone.HasElements)
                {
                    RoomRatesDone = RatePlansDone.Descendants("Room").Where(x => x.Element("RoomType").Value.Equals(splitCode[0])).Select(x => x.Element("RatePlanCode").Value).ToList();
                }
                List<XElement> RoomsForThisType = hotelElement.Descendants("RoomRate").Where(x => x.Attribute("RoomTypeCode").Value.Equals(splitCode[0])  && x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(splitCode[1])).ToList();

                XElement room = hotelElement.Descendants("RoomRate").Where(x => x.Attribute("RoomTypeCode").Value.Equals(splitCode[0]) && x.Attribute("RatePlanCode").Value.Equals(splitCode[2]) && x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(splitCode[1])).FirstOrDefault();
                //XElement room = RoomsForThisType.Where(x => !RoomRatesDone.Contains(x.Attribute("RatePlanCode").Value)).FirstOrDefault();
                if (room == null)
                    break;
                int adult = 0, child = 0;
                string PromotionText = string.Empty;
                double discount = room.Descendants("Discount").Any() ? Convert.ToDouble(room.Descendants("Discount").FirstOrDefault().Attribute("AmountAfterTax").Value) : 0.00, percentageDiscount = 0.00;
                if (discount > 0)
                {
                    double basePrice  = Convert.ToDouble(room.Descendants("Rate").Where(x=>x.Attribute("ChargeType").Value.Equals("26")).FirstOrDefault().Element("Base").Attribute("AmountAfterTax").Value);                    
                    percentageDiscount = (discount / basePrice) * 100;
                    percentageDiscount = Math.Round(percentageDiscount, 2);
                    PromotionText = percentageDiscount.ToString() + "% discount(Already apllied)";
                }
                List<XElement> SuppList = new List<XElement>();
                if(room.Descendants("Fees").Any() && room.Descendants("Fees").FirstOrDefault().HasElements)
                {
                    foreach(XElement supl in room.Descendants("Fee"))
                    {
                        if (supl.Attribute("Code").Value.Equals("City hotel fee"))
                            SuppList.Add(new XElement("Supplement",
                                                new XAttribute("suppId", ""),
                                                new XAttribute("suppName", supl.Attribute("Code").Value),
                                                new XAttribute("supptType", ""),
                                                new XAttribute("suppIsMandatory", supl.Attribute("MandatoryIndicator").Value),
                                                new XAttribute("suppChargeType", "AtProperty"),
                                                new XAttribute("suppPrice", supl.Attribute("Amount").Value),
                                                new XAttribute("suppType", supl.Attribute("ChargeFrequency").Value)));
                        else
                            SuppList.Add(new XElement("Supplement",
                                                new XAttribute("suppId", ""),
                                                new XAttribute("suppName", supl.Attribute("Code").Value),
                                                new XAttribute("supptType", ""),
                                                new XAttribute("suppIsMandatory", supl.Attribute("MandatoryIndicator").Value),
                                                new XAttribute("suppChargeType", supl.Attribute("ChargeUnit").Value),
                                                new XAttribute("suppPrice", supl.Attribute("Amount").Value),
                                                new XAttribute("suppType",  supl.Attribute("ChargeFrequency").Value)));
                    }
                }
                XElement guest = Req.Descendants("RoomPax").Where(x => x.Element("id").Value.Equals(splitCode[1])).FirstOrDefault();
                meals.Add( room.Descendants("Meal").First().Attribute("Name").Value);
                string roomtype = hotelElement.Descendants("RoomType").Where(x => x.Attribute("RoomTypeCode").Value.Equals(room.Attribute("RoomTypeCode").Value))
                                  .FirstOrDefault().Descendants("Text").FirstOrDefault().Value;
                int nights = room.Descendants("Rate").Where(x => x.Attributes("EffectiveDate").Any()).Count();
                RoomTypes.Add(new XElement("Room",
                                                   new XAttribute("ID", room1.Value),
                                                      new XAttribute("SuppliersID", "19"),
                                                      new XAttribute("RoomSeq", Convert.ToString(cnt++)),
                                                      new XAttribute("SessionID", room.Attribute("RatePlanCode").Value),
                                                      new XAttribute("RoomType", roomtype),
                                                      new XAttribute("OccupancyID", splitCode[1]),
                                                      new XAttribute("OccupancyName", ""),
                                                      new XAttribute("MealPlanID", ""),
                                                      new XAttribute("MealPlanName", room.Descendants("Meal").First().Attribute("Name").Value),
                                                      new XAttribute("MealPlanCode", MealPlanCode(room.Descendants("Meal").First().Attribute("Name").Value)),
                                                      new XAttribute("MealPlanPrice", ""),
                                                      new XAttribute("PerNightRoomRate", Convert.ToString(Convert.ToDouble(room.Descendants("Total").FirstOrDefault().Attribute("AmountAfterTax").Value) / nights)),
                                                      new XAttribute("TotalRoomRate", room.Descendants("Total").FirstOrDefault().Attribute("AmountAfterTax").Value),                     
                                                      new XAttribute("CancellationDate", ""),
                                                      new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", "true"),
                                                      new XElement("RequestID", Req.Descendants("RequestID").FirstOrDefault().Value),
                                                      new XElement("Offers"),
                                                      new XElement("PromotionList",
                                                      new XElement("Promotions", PromotionText)),
                                                      new XElement("CancellationPolicy"),
                                                      new XElement("Amenities",
                                                          new XElement("Amenity")),
                                                      new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                      new XElement("Supplements", SuppList),
                                                      pb(room.Descendants("Rate").Where(x => x.Attributes("EffectiveDate").Any()).ToList(), percentageDiscount),
                                                      new XElement("AdultNum", guest.Element("Adult").Value),
                                                      new XElement("ChildNum", guest.Element("Child").Value)));
                total += Convert.ToDouble(room.Descendants("Total").FirstOrDefault().Attribute("AmountAfterTax").Value);
            }
            RoomTypes.Attribute("TotalRate").SetValue(total.ToString());
            int mealCount = meals.Distinct().Count();
            return mealCount == 1? RoomTypes: null;
        }
        #region Price Breakup 
        public XElement  pb(List<XElement> rates, double discountPer)
        {
            int index = 1;
            rates = rates.OrderBy(x => Convert.ToDateTime(x.Attribute("EffectiveDate").Value)).ToList();
            XElement response = new XElement("PriceBreakups");
            foreach(XElement rate in rates)
            {
                double price = Convert.ToDouble(rate.Element("Base").Attribute("AmountAfterTax").Value);
                double discPrice = price * (discountPer / 100);
                price = price - discPrice;
                response.Add(new XElement("Price", new XAttribute("Night", index++),
                                new XAttribute("PriceValue", price.ToString())));
            }
            return response;
        }
        #endregion
        #endregion
        #region Search Response Address
        public string address(XElement addressTag)
        {
            string Address = string.Empty;
            foreach (XElement line in addressTag.Descendants("AddressLine"))
                Address += line.Value;
            return Address;
        }
        #endregion
        #region Date Formatting
        public string reformatDate(string date)
        {
            DateTime dt = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            string dd = dt.ToString("yyyy-MM-dd");
            return dd;            
        }
        #endregion
        #region Search Request Pax
        public XElement GuestCount(XElement RoomPax)
        {
            XElement Guests = new XElement(OTANamespace+"GuestCounts", new XAttribute("IsPerRoom", "false"));
            int adult = 0, child = 0, infant = 0;            
            foreach(XElement Room in RoomPax.Descendants("RoomPax"))
            {
                adult += Convert.ToInt32(Room.Element("Adult").Value);
                if(Convert.ToInt32(Room.Element("Child").Value)>0)
                {
                    foreach(XElement childAges in Room.Descendants("ChildAge"))
                    {
                        if (Convert.ToInt32(childAges.Value) <= 2)
                            infant++;
                        else
                            child++;
                    }
                }
            }
            if (adult > 0)
                Guests.Add(new XElement(OTANamespace + "GuestCount",
                                new XAttribute("AgeQualifyingCode", "10"),
                                new XAttribute("Count", adult.ToString())));
            if (child > 0)
                Guests.Add(new XElement(OTANamespace + "GuestCount",
                                new XAttribute("AgeQualifyingCode", "8"),
                                new XAttribute("Count", child.ToString())));
            if (infant > 0)
                Guests.Add(new XElement(OTANamespace + "GuestCount",
                                new XAttribute("AgeQualifyingCode", "7"),
                                new XAttribute("Count", infant.ToString())));
            return Guests;
        }
        public XElement SearchRooms(XElement Rooms)
        {            
            XElement Response = new XElement(OTANamespace + "RoomStayCandidates");
            foreach(XElement room in Rooms.Descendants("RoomPax"))
            {
                XElement kamra = new XElement(OTANamespace+"RoomStayCandidate",
                                     new XAttribute("Quantity","1"));
                XElement Guests = new XElement(OTANamespace + "GuestCounts", new XAttribute("IsPerRoom", "true"));
                int adult = Convert.ToInt32(room.Element("Adult").Value), child = 0, infants = 0;
                if(room.Descendants("ChildAge").OrderBy(x=>x.Value).ToList().Any())
                {
                    foreach (XElement childages in room.Descendants("ChildAge"))
                        if (Convert.ToInt32(childages.Value) <= 2)
                            Guests.Add(new XElement(OTANamespace + "GuestCount",
                                    new XAttribute("AgeQualifyingCode", "7"),
                                    new XAttribute("Count", child.ToString()),
                                    new XAttribute("Age", childages.Value)));
                        else
                            Guests.Add(new XElement(OTANamespace + "GuestCount",
                                    new XAttribute("AgeQualifyingCode", "8"),
                                    new XAttribute("Count", child.ToString()),
                                    new XAttribute("Age", childages.Value))); ;
                }
                var childAgeGroup = from children in Guests.Descendants(OTANamespace + "GuestCount")
                                    group children by new
                                    {
                                        c1 = children.Attribute("AgeQualifyingCode").Value,
                                        c2 = children.Attribute("Age").Value
                                    };                
                List<XElement> childGuests = new List<XElement>();
                foreach(var childGroup in childAgeGroup)
                {
                    childGuests.Add(new XElement(OTANamespace + "GuestCount",
                                    new XAttribute("AgeQualifyingCode",childGroup.Key.c1),
                                    new XAttribute("Count",childGroup.Count()),
                                    new XAttribute("Age", childGroup.Key.c2)));
                }
                if (adult > 0)
                    childGuests.Add(new XElement(OTANamespace + "GuestCount",
                                    new XAttribute("AgeQualifyingCode", "10"),
                                    new XAttribute("Count", adult.ToString())));
                Guests.Descendants().Remove();
                Guests.Add(childGuests);
                //if (child > 0)
                //    Guests.Add(new XElement(OTANamespace + "GuestCount",
                //                    new XAttribute("AgeQualifyingCode", "4"),
                //                    new XAttribute("Count", child.ToString())));
                //if (infants > 0)
                //    Guests.Add(new XElement(OTANamespace + "GuestCount",
                //                    new XAttribute("AgeQualifyingCode", "3"),
                //                    new XAttribute("Count", infants.ToString())));
                kamra.Add(Guests);
                Response.Add(kamra);
            }
            return Response;
        }
        #endregion
        #region Get Xmls From Log
        public List<XElement> LogXMLs(string trackID, int logtypeID, int SupplierID)
        {
            List<XElement> response = new List<XElement>();
            DataTable LogTable = new DataTable();
            LogTable = sda.GetLog(trackID, logtypeID, SupplierID);
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
        #region Star Rating Condition
        public bool StarRating(string minRating, string MaxRating, string HotelStarRating)
        {
            bool result;
            int minrating = Convert.ToInt32(minRating);
            int max = Convert.ToInt32(MaxRating);
            int star = Convert.ToInt32(Convert.ToDouble(HotelStarRating));
            if (star <= max && star >= minrating)
                result = true;
            else
                result = false;
            return result;
        }
        #endregion
        #region Meal Code Mapping
        public string MealPlanCode(string mealName)
        {
            string response = null;
            switch(mealName)
            {
                case "All Inclusive":
                    response = "AI";
                    break;
                case "All-Inclusive":
                    response = "AI";
                    break;               
                case "Bed & Breakfast":
                    response = "BB";
                    break;
                case "Full Board":
                    response = "FB";
                    break;
                case "Half Board":
                    response = "HB";
                    break;
                case "Room only":
                    response = "RO";
                    break;
                default:
                    response = string.Empty;
                    break;
            }
            return response;
        }
        #endregion
        #region Minimum Price
        public string mPrice(XElement salhotel, XElement Req)
        {



            #region Old Calculation
            //foreach (XElement room in salhotel.Descendants("RoomRate"))
            //{
            //    bool mpFlag = false;
            //    List<string> meals = new List<string>();
            //    XElement mealIncluded = salhotel.Descendants("RatePlan")
            //                           .Where(x => x.Attribute("RatePlanCode").Value.Equals(room.Attribute("RatePlanCode").Value)).FirstOrDefault();
            //    foreach (XAttribute m in mealIncluded.Descendants("MealsIncluded").FirstOrDefault().Attributes())
            //        meals.Add(m.Value);
            //    if (meals.Contains("true"))
            //        mpFlag = true;
            //    int adults = 0, child = 0, infant = 0;
            //    adults = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("10"))
            //             .FirstOrDefault().Attribute("Count").Value);
            //    if (room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("4")).Any())
            //        child = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("4"))
            //                 .FirstOrDefault().Attribute("Count").Value);
            //    if (room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("3")).Any())
            //        infant = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("3"))
            //                .FirstOrDefault().Attribute("Count").Value);
            //    string occupancy = adults.ToString() + "-" + child.ToString() + "-" + infant.ToString();
            //    room.Add(new XElement("Occupancy", occupancy));
            //    if (!mpFlag)
            //        room.Element("Rates").AddBeforeSelf(new XElement("Meal", new XAttribute("Name", "Room only")));                
            //}

            //var SupplGrouping = from rooms in salhotel.Descendants("RoomRate")
            //                    group rooms by new
            //                    {
            //                        c1 = rooms.Descendants("Occupancy").FirstOrDefault().Value,
            //                        c2 = rooms.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            //                    };
            //int possibleOutcomes = 1;
            //int roomCount = salhotel.Descendants("RoomRate").Count();           
            //XElement Grouping = new XElement("Groups");
            ////foreach (XElement room in Req.Descendants("RoomPax"))
            ////{
            ////    int child = 0, infant = 0;
            ////    if (room.Descendants("ChildAge").Any())
            ////    {
            ////        foreach (XElement age in room.Descendants("ChildAge"))
            ////        {
            ////            if (Convert.ToInt32(age.Value) <= 2)
            ////                child++;
            ////            else
            ////                infant++;
            ////        }
            ////    }
            ////    string paxes = room.Element("Adult").Value + "-" + child.ToString() + "-" + infant.ToString();
            ////    var entries = SupplGrouping.Where(x => x.Key.c1.Equals(paxes));
            ////    possibleOutcomes = possibleOutcomes * entries.FirstOrDefault().Elements().Count();
            ////    Grouping.Add(new XElement("Group" + counter++.ToString(), new XAttribute("Paxes", paxes), entries));
            ////}
            //List<string> allMeals = new List<string>();
            //List<string> allPax = new List<string>();
            //foreach (var group in SupplGrouping)
            //{
            //    if(!allMeals.Contains(group.Key.c2))
            //        allMeals.Add(group.Key.c2);
            //    if (!allPax.Contains(group.Key.c1))
            //        allPax.Add(group.Key.c1);
            //}
            //foreach(string meal in allMeals)
            //{
            //    Grouping.Add(new XElement("Meal", new XAttribute("Name", meal)));                
            //    foreach (XElement room in Req.Descendants("RoomPax"))
            //    {                    
            //        int child = 0, infant = 0;
            //        if (room.Descendants("ChildAge").Any())
            //        {
            //            foreach (XElement age in room.Descendants("ChildAge"))
            //            {
            //                if (Convert.ToInt32(age.Value) <= 2)
            //                    child++;
            //                else
            //                    infant++;
            //            }
            //        }
            //        string paxes = room.Element("Adult").Value + "-" + child.ToString() + "-" + infant.ToString();
            //        var entries = SupplGrouping.Where(x => x.Key.c1.Equals(paxes) && x.Key.c2.Equals(meal));
            //        Grouping.Elements("Meal").Where(x=>x.Attribute("Name").Value.Equals(meal)).FirstOrDefault().Add( new XElement("Group", new XAttribute("Paxes", paxes), entries));
            //    }
            //}
            //Grouping.Descendants("Meal").Where(x => !x.HasElements).Remove();
            //int roomcount = Req.Descendants("RoomPax").Count();
            //XElement FinalGroup = new XElement("Groups");
            //#region Room Grouping
            ////#region 1 Room
            ////if (roomcount == 1)
            ////    FinalGroup = new XElement("Group", new XElement("Room", Grouping.Descendants("RoomRate").ToList()));
            ////#endregion
            ////#region 2 Rooms
            ////else if (roomcount == 2)
            ////{
            ////    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            ////                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  select new XElement("Room", r1, r2);
            ////    FinalGroup = new XElement("Groups", joinall);
            ////}
            ////#endregion
            ////#region 3 Rooms
            ////else if (roomcount == 3)
            ////{
            ////    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            ////                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  select new XElement("Room", r1, r2, r3);

            ////    FinalGroup = new XElement("Groups", joinall);
            ////}
            ////#endregion
            ////#region 4 Rooms
            ////else if (roomcount == 4)
            ////{
            ////    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            ////                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  select new XElement("Room", r1, r2, r3, r4);

            ////    FinalGroup = new XElement("Groups", joinall);
            ////}
            ////#endregion
            ////#region 5 Rooms
            ////else if (roomcount == 5)
            ////{
            ////    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            ////                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r5 in Grouping.Elements("Group5").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r5.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  select new XElement("Room", r1, r2, r3, r4, r5);

            ////    FinalGroup = new XElement("Groups", joinall);
            ////}
            ////#endregion
            ////#region 6 Rooms
            ////else if (roomcount == 6)
            ////{
            ////    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            ////                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r5 in Grouping.Elements("Group5").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r5.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r6 in Grouping.Elements("Group6").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r6.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  select new XElement("Room", r1, r2, r3, r4, r5, r6);

            ////    FinalGroup = new XElement("Groups", joinall);
            ////}
            ////#endregion
            ////#region 7 Rooms
            ////else if (roomcount == 7)
            ////{
            ////    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            ////                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r5 in Grouping.Elements("Group5").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r5.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r6 in Grouping.Elements("Group6").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r6.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r7 in Grouping.Elements("Group7").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r7.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  select new XElement("Room", r1, r2, r3, r4, r5, r6, r7);

            ////    FinalGroup = new XElement("Groups", joinall);
            ////}
            ////#endregion
            ////#region 8 Rooms
            ////else if (roomcount == 8)
            ////{
            ////    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            ////                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r5 in Grouping.Elements("Group5").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r5.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r6 in Grouping.Elements("Group6").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r6.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r7 in Grouping.Elements("Group7").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r7.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r8 in Grouping.Elements("Group8").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r8.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  select new XElement("Room", r1, r2, r3, r4, r5, r6, r7, r8);

            ////    FinalGroup = new XElement("Groups", joinall);
            ////}
            ////#endregion
            ////#region 9 Rooms
            ////else if (roomcount == 9)
            ////{
            ////    var joinall = from r1 in Grouping.Elements("Group1").First().Descendants("RoomRate")
            ////                  join r2 in Grouping.Elements("Group2").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r2.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r3 in Grouping.Elements("Group3").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r3.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r4 in Grouping.Elements("Group4").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r4.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r5 in Grouping.Elements("Group5").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r5.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r6 in Grouping.Elements("Group6").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r6.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r7 in Grouping.Elements("Group7").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r7.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r8 in Grouping.Elements("Group8").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r8.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  join r9 in Grouping.Elements("Group9").First().Descendants("RoomRate") on r1.Descendants("Meal").FirstOrDefault().Attribute("Name").Value equals r9.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
            ////                  select new XElement("Room", r1, r2, r3, r4, r5, r6, r7, r8, r9);
            ////    FinalGroup = new XElement("Groups");
            ////    List<XElement> tryit = new List<XElement>();
            ////    //var chunkwise = BreakIntoChunks(joinall.ToList(), 150);
            ////    //foreach (var chunk in chunkwise)
            ////    //    FinalGroup.Add(chunk.ToList());
            ////    if (!FinalGroup.HasElements)
            ////        tryit = joinall.Take(100).ToList();
            ////    else
            ////        tryit = joinall.Where(x => !FinalGroup.Descendants("RoomRate").ToList().Contains(x)).Take(100).ToList();


            ////}
            ////#endregion
            //#endregion
            //#region Room Grouping New
            //List<XElement> Room1 = new List<XElement>();
            //List<XElement> Room2 = new List<XElement>();
            //List<XElement> Room3 = new List<XElement>();
            //List<XElement> Room4 = new List<XElement>();
            //List<XElement> Room5 = new List<XElement>();
            //List<XElement> Room6 = new List<XElement>();
            //List<XElement> Room7 = new List<XElement>();
            //List<XElement> Room8 = new List<XElement>();
            //List<XElement> Room9 = new List<XElement>();
            //int repeats = roomcount - allPax.Count;
            //int take = 10;      
            ////if (roomcount > 6 && repeats > 3)                   //max = 19683
            ////    take = 3;               
            ////else if (roomcount > 6 && repeats < 4)             //
            ////    take = 5;
            ////else if (roomcount < 6 && roomcount > 2 && repeats > 3)
            ////    take = 5;
            ////else if (roomcount < 6 && roomcount > 2 && repeats < 4)
            ////    take = 7;
            ////else if (roomcount < 3)
            ////    take = 15;
            //if (roomcount == 9)                                 //max = 512
            //    take = 2;
            //if (roomcount > 6 && roomcount<9)                   //max = 6561
            //    take = 3;
            //else if (roomcount < 6 && roomcount > 3)            //max = 3125 at 6 max =15625
            //    take = 5;
            //else if (roomcount == 3)                            // max == 8000
            //    take = 20;              
            //else if (roomcount == 2)                            // max == 900
            //    take = 30;
            //else if (roomcount == 1)                            // max = 400
            //    take = 200;
            //foreach(XElement meal in Grouping.Descendants("Meal"))
            //{
            //    int roomCounter = 1;
            //    foreach(XElement room in Req.Descendants("RoomPax"))
            //    {                   
            //        int child = 0, infant = 0;
            //        if (room.Descendants("ChildAge").Any())
            //        {
            //            foreach (XElement age in room.Descendants("ChildAge"))
            //            {
            //                if (Convert.ToInt32(age.Value) <= 2)
            //                    child++;
            //                else
            //                    infant++;
            //            }
            //        }
            //        string id = room.Element("id").Value;
            //        string paxes = room.Element("Adult").Value + "-" + child.ToString() + "-" + infant.ToString();
            //        if (roomCounter == 1)
            //            Room1 = Grouping.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
            //        else if (roomCounter == 2)
            //            Room2 = Grouping.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
            //        else if (roomCounter == 3)
            //            Room3 = Grouping.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
            //        else if (roomCounter == 4)
            //            Room4 = Grouping.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
            //        else if (roomCounter == 5)
            //            Room5 = Grouping.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
            //        else if (roomCounter == 6)
            //            Room6 = Grouping.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
            //        else if (roomCounter == 7)
            //            Room7 = Grouping.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
            //        else if (roomCounter == 8)
            //            Room8 = Grouping.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
            //        else if (roomCounter == 9)
            //            Room9 = Grouping.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
            //        roomCounter++;
            //    }
            //    #region 1 Room
            //    if (roomcount == 1)
            //    {
            //        foreach (XElement rm1 in Room1)
            //        {                       
            //            FinalGroup.Add(new XElement("Room", rm1));
            //        }                                    
            //    }
            //    #endregion
            //    #region 2 Rooms
            //    if (roomcount == 2)
            //    {
            //        foreach (XElement rm1 in Room1)
            //        {
            //            foreach (XElement rm2 in Room2)
            //            {                           
            //                FinalGroup.Add(new XElement("Room", rm1, rm2));
            //            }
            //        }                                
            //    }
            //    #endregion
            //    #region 3 Rooms
            //    if (roomcount == 3)
            //    {
            //        foreach (XElement rm1 in Room1)
            //        {
            //            foreach (XElement rm2 in Room2)
            //            {
            //                foreach (XElement rm3 in Room3)
            //                {                               
            //                    FinalGroup.Add(new XElement("Room", rm1, rm2, rm3));
            //                }
            //            }
            //        }                           
            //    }
            //    #endregion
            //    #region 4 Rooms
            //    if (roomcount == 4)
            //    {
            //        foreach (XElement rm1 in Room1)
            //        {
            //            foreach (XElement rm2 in Room2)
            //            {
            //                foreach (XElement rm3 in Room3)
            //                {
            //                    foreach (XElement rm4 in Room4)
            //                    {                                   
            //                        FinalGroup.Add(new XElement("Room", rm1, rm2, rm3, rm4));
            //                    }
            //                }
            //            }
            //        }                       
            //    }
            //    #endregion
            //    #region 5 Rooms
            //    if (roomcount == 5)
            //    {
            //        foreach (XElement rm1 in Room1)
            //        {
            //            foreach (XElement rm2 in Room2)
            //            {
            //                foreach (XElement rm3 in Room3)
            //                {
            //                    foreach (XElement rm4 in Room4)
            //                    {
            //                        foreach (XElement rm5 in Room5)
            //                        {                                        
            //                            FinalGroup.Add(new XElement("Room", rm1, rm2, rm3, rm4, rm5));
            //                        }
            //                    }
            //                }
            //            }
            //        }                    
            //    }
            //    #endregion
            //    #region 6 Rooms
            //    if (roomcount == 6)
            //    {
            //        foreach (XElement rm1 in Room1)
            //        {
            //            foreach (XElement rm2 in Room2)
            //            {
            //                foreach (XElement rm3 in Room3)
            //                {
            //                    foreach (XElement rm4 in Room4)
            //                    {
            //                        foreach (XElement rm5 in Room5)
            //                        {
            //                            foreach (XElement rm6 in Room6)
            //                            {
            //                                FinalGroup.Add(new XElement("Room", rm1, rm2, rm3, rm4, rm5, rm6));
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    } 
            //    #endregion
            //    #region 7 Rooms
            //    if (roomcount == 7)
            //    {
            //        foreach (XElement rm1 in Room1)
            //        {
            //            foreach (XElement rm2 in Room2)
            //            {
            //                foreach (XElement rm3 in Room3)
            //                {
            //                    foreach (XElement rm4 in Room4)
            //                    {
            //                        foreach (XElement rm5 in Room5)
            //                        {
            //                            foreach (XElement rm6 in Room6)
            //                            {
            //                                foreach (XElement rm7 in Room7)
            //                                {
            //                                    FinalGroup.Add(new XElement("Room", rm1, rm2, rm3, rm4, rm5, rm6, rm7));
            //                                }
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    #endregion
            //    #region 8 Rooms
            //    if (roomcount == 8)
            //    {
            //        foreach (XElement rm1 in Room1)
            //        {
            //            foreach (XElement rm2 in Room2)
            //            {
            //                foreach (XElement rm3 in Room3)
            //                {
            //                    foreach (XElement rm4 in Room4)
            //                    {
            //                        foreach (XElement rm5 in Room5)
            //                        {
            //                            foreach (XElement rm6 in Room6)
            //                            {
            //                                foreach (XElement rm7 in Room7)
            //                                {
            //                                    foreach (XElement rm8 in Room8)
            //                                    {
            //                                        FinalGroup.Add(new XElement("Room", rm1, rm2, rm3, rm4, rm5, rm6,rm7,rm8));
            //                                    }
            //                                }
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    #endregion
            //    #region 9 Rooms
            //    if (roomcount == 9)
            //    {
            //        foreach (XElement rm1 in Room1)
            //        {
            //            foreach (XElement rm2 in Room2)
            //            {
            //                foreach (XElement rm3 in Room3)
            //                {
            //                    foreach (XElement rm4 in Room4)
            //                    {
            //                        foreach (XElement rm5 in Room5)
            //                        {
            //                            foreach (XElement rm6 in Room6)
            //                            {
            //                                foreach (XElement rm7 in Room7)
            //                                {
            //                                    foreach (XElement rm8 in Room8)
            //                                    {
            //                                        foreach (XElement rm9 in Room9)
            //                                        {
            //                                            FinalGroup.Add(new XElement("Room", rm1, rm2, rm3, rm4, rm5, rm6,rm7,rm8,rm9));
            //                                        }
            //                                    }
            //                                }
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    #endregion

            //}
            //#endregion 
            #endregion
            foreach (XElement room in salhotel.Descendants("RoomRate"))
            {
                bool mpFlag = false;
                List<string> meals = new List<string>();
                XElement mealIncluded = salhotel.Descendants("RatePlan")
                                       .Where(x => x.Attribute("RatePlanCode").Value.Equals(room.Attribute("RatePlanCode").Value)).FirstOrDefault();
                foreach (XAttribute m in mealIncluded.Descendants("MealsIncluded").FirstOrDefault().Attributes())
                    meals.Add(m.Value);
                if (meals.Contains("true"))
                    mpFlag = true;
                int adults = 0, child = 0, infant = 0;
                adults = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("10"))
                         .FirstOrDefault().Attribute("Count").Value);
                if (room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("8")).Any())
                    child = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("8"))
                             .FirstOrDefault().Attribute("Count").Value);
                if (room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("7")).Any())
                    infant = Convert.ToInt32(room.Descendants("GuestCount").Where(x => x.Attribute("AgeQualifyingCode").Value.Equals("7"))
                            .FirstOrDefault().Attribute("Count").Value);
                string occupancy = adults.ToString() + "-" + child.ToString() + "-" + infant.ToString();
                room.Add(new XElement("Occupancy", occupancy));
                if (!mpFlag)
                    room.Element("Rates").AddBeforeSelf(new XElement("Meal", new XAttribute("Name", "Room only")));
            }

            var SupplGrouping = from rooms in salhotel.Descendants("RoomRate")
                                group rooms by new
                                {
                                    c1 = rooms.Descendants("Occupancy").FirstOrDefault().Value,
                                    c2 = rooms.Descendants("Meal").FirstOrDefault().Attribute("Name").Value
                                };
            int possibleOutcomes = 1;
            int roomCount = salhotel.Descendants("RoomRate").Count();
            XElement Grouping = new XElement("Groups");
            //foreach (XElement room in Req.Descendants("RoomPax"))
            //{
            //    int child = 0, infant = 0;
            //    if (room.Descendants("ChildAge").Any())
            //    {
            //        foreach (XElement age in room.Descendants("ChildAge"))
            //        {
            //            if (Convert.ToInt32(age.Value) <= 2)
            //                child++;
            //            else
            //                infant++;
            //        }
            //    }
            //    string paxes = room.Element("Adult").Value + "-" + child.ToString() + "-" + infant.ToString();
            //    var entries = SupplGrouping.Where(x => x.Key.c1.Equals(paxes));
            //    possibleOutcomes = possibleOutcomes * entries.FirstOrDefault().Elements().Count();
            //    Grouping.Add(new XElement("Group" + counter++.ToString(), new XAttribute("Paxes", paxes), entries));
            //}
            List<string> allMeals = new List<string>();
            List<string> allPax = new List<string>();
            foreach (var group in SupplGrouping)
            {
                if (!allMeals.Contains(group.Key.c2))
                    allMeals.Add(group.Key.c2);
                if (!allPax.Contains(group.Key.c1))
                    allPax.Add(group.Key.c1);
            }
            foreach (string meal in allMeals)
            {
                Grouping.Add(new XElement("MealIncluded", new XAttribute("Name", meal)));
                foreach (XElement room in Req.Descendants("RoomPax"))
                {
                    int child = 0, infant = 0;
                    if (room.Descendants("ChildAge").Any())
                    {
                        foreach (XElement age in room.Descendants("ChildAge"))
                        {
                            if (Convert.ToInt32(age.Value) <= 2)
                                infant++;
                            else
                                child++;
                        }
                    }
                    string paxes = room.Element("Adult").Value + "-" + child.ToString() + "-" + infant.ToString();// "-0-0"; //+child.ToString() + "-" + infant.ToString();
                    var entries = SupplGrouping.Where(x => x.Key.c1.Equals(paxes) && x.Key.c2.Equals(meal));
                    Grouping.Elements("MealIncluded").Where(x => x.Attribute("Name").Value.Equals(meal)).FirstOrDefault().Add(new XElement("Group", new XAttribute("Paxes", paxes), entries));
                }
            }
            int roomcount = Req.Descendants("RoomPax").Count();
            XElement FinalGroup = new XElement("Groups");
            #region Room Grouping New
           
            int repeats = roomcount - allPax.Count;
            int take = 10;
            //if (roomcount > 6 && repeats > 3)                   //max = 19683
            //    take = 3;               
            //else if (roomcount > 6 && repeats < 4)             //
            //    take = 5;
            //else if (roomcount < 6 && roomcount > 2 && repeats > 3)
            //    take = 5;
            //else if (roomcount < 6 && roomcount > 2 && repeats < 4)
            //    take = 7;
            //else if (roomcount < 3)
            //    take = 15;
            if (roomcount == 9)                                 //max = 512
                take = 4;
            if (roomcount > 6 && roomcount < 9)                   //max = 6561
                take = 5;
            else if (roomcount < 6 && roomcount > 3)            //max = 3125 at 6 max =15625
                take = 10;
            else if (roomcount == 3)                            // max == 8000
                take = 20;
            else if (roomcount == 2)                            // max == 900
                take = 30;
            else if (roomcount == 1)                            // max = 400
                take = 200;
            List<string> Roomtypes = new List<string>();
            foreach (XElement rt in salhotel.Descendants("RoomType"))
                Roomtypes.Add(rt.Attribute("RoomTypeCode").Value);
            foreach (XElement meal in Grouping.Descendants("MealIncluded"))
            {
                if (meal.Descendants("Group").Where(x => !x.HasElements).Any())
                    continue;
                List<string> Room1 = new List<string>();
                List<string> Room2 = new List<string>();
                List<string> Room3 = new List<string>();
                List<string> Room4 = new List<string>();
                List<string> Room5 = new List<string>();
                List<string> Room6 = new List<string>();
                List<string> Room7 = new List<string>();
                List<string> Room8 = new List<string>();
                List<string> Room9 = new List<string>();
                #region Room Types

                //int t1=0, t2=0, t3=0, t4=0, t5=0, t6=0, t7=0, t8=0, t9=0, t10=0;
                XElement RoomtypeData = stringAsNode(Roomtypes, new XElement("RoomTypeList"));
                #endregion
                int roomCounter = 1;
                foreach (XElement room in Req.Descendants("RoomPax"))
                {
                    int child = 0, infant = 0;
                    if (room.Descendants("ChildAge").Any())
                    {
                        foreach (XElement age in room.Descendants("ChildAge"))
                        {
                            if (Convert.ToInt32(age.Value) <= 2)
                                infant++;
                            else
                                child++;
                        }
                    }
                    string id = room.Element("id").Value;
                    string paxes = room.Element("Adult").Value + "-" + child.ToString() + "-" + infant.ToString();// "-0-0"; //+child.ToString() + "-" + infant.ToString();
                    if (roomCounter == 1)
                    {

                        //Room1 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                        {
                            if(Room1.Count<take)//if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room1.Count < take)
                                Room1.Add(roomfortypes.Attribute("RoomTypeCode").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room1.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 2)
                    {
                        //Room2 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                        {
                            if (Room2.Count < take) //if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room2.Count < take)
                                Room2.Add(roomfortypes.Attribute("RoomTypeCode").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room2.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 3)
                    {
                        //Room3 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                        {
                            if (Room3.Count < take) //if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room3.Count < take)
                                Room3.Add(roomfortypes.Attribute("RoomTypeCode").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room3.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 4)
                    {
                        //Room4 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                        {
                            if (Room4.Count < take) //if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room4.Count < take)
                                Room4.Add(roomfortypes.Attribute("RoomTypeCode").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room4.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 5)
                    {
                        //Room5 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                        {
                            if (Room5.Count < take) //if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room5.Count < take)
                                Room5.Add(roomfortypes.Attribute("RoomTypeCode").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room5.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 6)
                    {
                        //Room6 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                        {
                            if (Room6.Count < take) //if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room6.Count < take)
                                Room6.Add(roomfortypes.Attribute("RoomTypeCode").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room6.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 7)
                    {
                        //Room7 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                        {
                            if (Room7.Count < take) //if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room7.Count < take)
                                Room7.Add(roomfortypes.Attribute("RoomTypeCode").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room7.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 8)
                    {
                        //Room8 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                        {
                            if (Room8.Count < take) //if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room8.Count < take)
                                Room8.Add(roomfortypes.Attribute("RoomTypeCode").Value);
                            else
                                break;
                        }
                        RoomtypeData.Descendants("Node").Where(x => Room8.Contains(x.Value)).Remove();
                    }
                    else if (roomCounter == 9)
                    {
                        //Room9 = meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)).Take(take).ToList();
                        if (!RoomtypeData.Descendants("Node").Any())
                            RoomtypeData = stringAsNode(Roomtypes, RoomtypeData);
                        foreach (XElement roomfortypes in meal.Descendants("Group").Where(x => x.FirstAttribute.Value.Equals(paxes)).FirstOrDefault().Elements().Where(x => x.Descendants("RoomRateDescription").FirstOrDefault().Element("Text").Value.Equals(id)))
                        {
                            if (Room9.Count < take) //if (RoomtypeData.Descendants("Node").Where(x => x.Value.Equals(roomfortypes.Attribute("RoomTypeCode").Value)).Any() && Room9.Count < take)
                                Room9.Add(roomfortypes.Attribute("RoomTypeCode").Value);
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
                        FinalGroup.Add(new XElement("Room",new XElement("type", rm1)));
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
                            // List<string> AllocationCheck_List = new List<string>();
                            //AllocationCheck_List.Add(getIdBySplit(rm2));
                            //AllocationCheck_List.Add(getIdBySplit(rm1));
                            //bool Allocation = true;
                            //if (rm1.Equals(rm2) && salhotel.Descendants("RoomType").Where(x => x.Attribute("RoomTypeCode").Value.Equals(rm1)).FirstOrDefault().Attribute("NumberOfUnits").Value.Equals("1"))
                            //    Allocation = false;
                            //if (Allocation)
                            //{
                                FinalGroup.Add(new XElement("Room", new XElement("type", rm1), new XElement("type", rm2)));
                            //}
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
                                // List<string> AllocationCheck_List = new List<string>();
                                //AllocationCheck_List.Add(getIdBySplit(rm2));
                                //AllocationCheck_List.Add(getIdBySplit(rm1));
                                //AllocationCheck_List.Add(getIdBySplit(rm3));
                                //bool Allocation = true;
                                string CurrentConfig = string.Empty;
                                //var typeGroup = from rTypes in AllocationCheck_List
                                //                group rTypes by rTypes;
                                //foreach(var type in typeGroup)
                                //{
                                //    int availUnits = Convert.ToInt32(salhotel.Descendants("RoomType").Where(x => x.Attribute("RoomTypeCode").Value.Equals(type.Key))
                                //                        .FirstOrDefault().Attribute("NumberOfUnits").Value);
                                //    if (availUnits < type.Count())
                                //        Allocation = false;
                                //}
                                //if (Allocation)
                                //{
                                    XElement Currentgroup = new XElement("Room",
                                                        new XElement("type", rm1),
                                                        new XElement("type", rm2),
                                                        new XElement("type", rm3));
                                    foreach (string rt in Roomtypes)
                                    {
                                        CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Equals(rt)).Count().ToString() + "-";
                                    }
                                    CurrentConfig += meal.Attribute("Name").Value;
                                    Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                    if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                        FinalGroup.Add(Currentgroup);
                            //}
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
                                    //List<string> AllocationCheck_List = new List<string>();
                                    //AllocationCheck_List.Add(getIdBySplit(rm2));
                                    //AllocationCheck_List.Add(getIdBySplit(rm1));
                                    //AllocationCheck_List.Add(getIdBySplit(rm3));
                                    //AllocationCheck_List.Add(getIdBySplit(rm4));
                                    //bool Allocation = true;
                                    string CurrentConfig = string.Empty;
                                    //var typeGroup = from rTypes in AllocationCheck_List
                                    //                group rTypes by rTypes;
                                    //foreach(var type in typeGroup)
                                    //{
                                    //    int availUnits = Convert.ToInt32(salhotel.Descendants("RoomType").Where(x => x.Attribute("RoomTypeCode").Value.Equals(type.Key))
                                    //                        .FirstOrDefault().Attribute("NumberOfUnits").Value);
                                    //    if (availUnits < type.Count())
                                    //        Allocation = false;
                                    //}
                                    //if (Allocation)
                                    //{
                                        XElement Currentgroup = new XElement("Room",
                                                            new XElement("type", rm1),
                                                            new XElement("type", rm2),
                                                            new XElement("type", rm3),
                                                            new XElement("type", rm4));
                                        foreach (string rt in Roomtypes)
                                        {
                                            CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Equals(rt)).Count().ToString() + "-";
                                        }
                                        CurrentConfig += meal.Attribute("Name").Value;
                                        Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                        if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                            FinalGroup.Add(Currentgroup);
                                    //}
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
                                        //List<string> AllocationCheck_List = new List<string>();
                                        //AllocationCheck_List.Add(getIdBySplit(rm2));
                                        //AllocationCheck_List.Add(getIdBySplit(rm1));
                                        //AllocationCheck_List.Add(getIdBySplit(rm3));
                                        //AllocationCheck_List.Add(getIdBySplit(rm4));
                                        //AllocationCheck_List.Add(getIdBySplit(rm5));
                                        //bool Allocation = true;
                                        string CurrentConfig = string.Empty;
                                        //var typeGroup = from rTypes in AllocationCheck_List
                                        //                group rTypes by rTypes;
                                        //foreach(var type in typeGroup)
                                        //{
                                        //    int availUnits = Convert.ToInt32(salhotel.Descendants("RoomType").Where(x => x.Attribute("RoomTypeCode").Value.Equals(type.Key))
                                        //                        .FirstOrDefault().Attribute("NumberOfUnits").Value);
                                        //    if (availUnits < type.Count())
                                        //        Allocation = false;
                                        //}
                                        //if (Allocation)
                                        //{
                                            XElement Currentgroup = new XElement("Room",
                                                                new XElement("type", rm1),
                                                                new XElement("type", rm2),
                                                                new XElement("type", rm3),
                                                                new XElement("type", rm4),
                                                                new XElement("type", rm5));
                                            foreach (string rt in Roomtypes)
                                            {
                                                CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Equals(rt)).Count().ToString() + "-";
                                            }
                                            CurrentConfig += meal.Attribute("Name").Value;
                                            Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                            if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                FinalGroup.Add(Currentgroup);
                                        //}
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
                                            //List<string> AllocationCheck_List = new List<string>();
                                            //AllocationCheck_List.Add(getIdBySplit(rm2));
                                            //AllocationCheck_List.Add(getIdBySplit(rm1));
                                            //AllocationCheck_List.Add(getIdBySplit(rm3));
                                            //AllocationCheck_List.Add(getIdBySplit(rm4));
                                            //AllocationCheck_List.Add(getIdBySplit(rm5));
                                            //AllocationCheck_List.Add(getIdBySplit(rm6));                                            
                                            //bool Allocation = true;
                                            string CurrentConfig = string.Empty;
                                            //var typeGroup = from rTypes in AllocationCheck_List
                                            //                group rTypes by rTypes;
                                            //foreach(var type in typeGroup)
                                            //{
                                            //    int availUnits = Convert.ToInt32(salhotel.Descendants("RoomType").Where(x => x.Attribute("RoomTypeCode").Value.Equals(type.Key))
                                            //                        .FirstOrDefault().Attribute("NumberOfUnits").Value);
                                            //    if (availUnits < type.Count())
                                            //        Allocation = false;
                                            //}
                                            //if (Allocation)
                                            //{
                                                XElement Currentgroup = new XElement("Room",
                                                                    new XElement("type", rm1),
                                                                    new XElement("type", rm2),
                                                                    new XElement("type", rm3),
                                                                    new XElement("type", rm4),
                                                                    new XElement("type", rm5),
                                                                    new XElement("type", rm6));
                                                foreach (string rt in Roomtypes)
                                                {
                                                    CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Equals(rt)).Count().ToString() + "-";
                                                }
                                                CurrentConfig += meal.Attribute("Name").Value;
                                                Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                    FinalGroup.Add(Currentgroup);
                                            //}
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
                                                // List<string> AllocationCheck_List = new List<string>();
                                                //AllocationCheck_List.Add(getIdBySplit(rm2));
                                                //AllocationCheck_List.Add(getIdBySplit(rm1));
                                                //AllocationCheck_List.Add(getIdBySplit(rm3));
                                                //AllocationCheck_List.Add(getIdBySplit(rm4));
                                                //AllocationCheck_List.Add(getIdBySplit(rm5));
                                                //AllocationCheck_List.Add(getIdBySplit(rm6));
                                                //AllocationCheck_List.Add(getIdBySplit(rm7));
                                                //bool Allocation = true;
                                                string CurrentConfig = string.Empty;
                                                //var typeGroup = from rTypes in AllocationCheck_List
                                                //                group rTypes by rTypes;
                                                //foreach(var type in typeGroup)
                                                //{
                                                //    int availUnits = Convert.ToInt32(salhotel.Descendants("RoomType").Where(x => x.Attribute("RoomTypeCode").Value.Equals(type.Key))
                                                //                        .FirstOrDefault().Attribute("NumberOfUnits").Value);
                                                //    if (availUnits < type.Count())
                                                //        Allocation = false;
                                                //}
                                                //if (Allocation)
                                                //{
                                                    XElement Currentgroup = new XElement("Room",
                                                                        new XElement("type", rm1),
                                                                        new XElement("type", rm2),
                                                                        new XElement("type", rm3),
                                                                        new XElement("type", rm4),
                                                                        new XElement("type", rm5),
                                                                        new XElement("type", rm6),
                                                                        new XElement("type", rm7));
                                                    foreach (string rt in Roomtypes)
                                                    {
                                                        CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Equals(rt)).Count().ToString() + "-";
                                                    }
                                                    CurrentConfig += meal.Attribute("Name").Value;
                                                    Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                    if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                        FinalGroup.Add(Currentgroup);
                                               // }
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
                                                    //List<string> AllocationCheck_List = new List<string>();
                                                    //AllocationCheck_List.Add(getIdBySplit(rm2));
                                                    //AllocationCheck_List.Add(getIdBySplit(rm1));
                                                    //AllocationCheck_List.Add(getIdBySplit(rm3));
                                                    //AllocationCheck_List.Add(getIdBySplit(rm4));
                                                    //AllocationCheck_List.Add(getIdBySplit(rm5));
                                                    //AllocationCheck_List.Add(getIdBySplit(rm6));
                                                    //AllocationCheck_List.Add(getIdBySplit(rm7));
                                                    //AllocationCheck_List.Add(getIdBySplit(rm8));                                                    
                                                    //bool Allocation = true;
                                                    string CurrentConfig = string.Empty;
                                                    //var typeGroup = from rTypes in AllocationCheck_List
                                                    //                group rTypes by rTypes;
                                                    //foreach(var type in typeGroup)
                                                    //{
                                                    //    int availUnits = Convert.ToInt32(salhotel.Descendants("RoomType").Where(x => x.Attribute("RoomTypeCode").Value.Equals(type.Key))
                                                    //                        .FirstOrDefault().Attribute("NumberOfUnits").Value);
                                                    //    if (availUnits < type.Count())
                                                    //        Allocation = false;
                                                    //}
                                                    //if (Allocation)
                                                    //{
                                                        XElement Currentgroup = new XElement("Room",
                                                                            new XElement("type", rm1),
                                                                            new XElement("type", rm2),
                                                                            new XElement("type", rm3),
                                                                            new XElement("type", rm4),
                                                                            new XElement("type", rm5),
                                                                            new XElement("type", rm6),
                                                                            new XElement("type", rm7),
                                                                            new XElement("type", rm8));
                                                        foreach (string rt in Roomtypes)
                                                        {
                                                            CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Equals(rt)).Count().ToString() + "-";
                                                        }
                                                        CurrentConfig += meal.Attribute("Name").Value;
                                                        Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                        if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                            FinalGroup.Add(Currentgroup);
                                                    //}
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
                                                        // List<string> AllocationCheck_List = new List<string>();
                                                        //AllocationCheck_List.Add(getIdBySplit(rm2));
                                                        //AllocationCheck_List.Add(getIdBySplit(rm1));
                                                        //AllocationCheck_List.Add(getIdBySplit(rm3));
                                                        //AllocationCheck_List.Add(getIdBySplit(rm4));
                                                        //AllocationCheck_List.Add(getIdBySplit(rm5));
                                                        //AllocationCheck_List.Add(getIdBySplit(rm6));
                                                        //AllocationCheck_List.Add(getIdBySplit(rm7));
                                                        //AllocationCheck_List.Add(getIdBySplit(rm8));
                                                        //AllocationCheck_List.Add(getIdBySplit(rm9));
                                                        //bool Allocation = true;
                                                        string CurrentConfig = string.Empty;
                                                        //var typeGroup = from rTypes in AllocationCheck_List
                                                        //                group rTypes by rTypes;
                                                        //foreach(var type in typeGroup)
                                                        //{
                                                        //    int availUnits = Convert.ToInt32(salhotel.Descendants("RoomType").Where(x => x.Attribute("RoomTypeCode").Value.Equals(type.Key))
                                                        //                        .FirstOrDefault().Attribute("NumberOfUnits").Value);
                                                        //    if (availUnits < type.Count())
                                                        //        Allocation = false;
                                                        //}
                                                        //if (Allocation)
                                                        //{
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
                                                            foreach (string rt in Roomtypes)
                                                            {
                                                                CurrentConfig += Currentgroup.Descendants("type").Where(x => x.Value.Equals(rt)).Count().ToString() + "-";
                                                            }
                                                            CurrentConfig += meal.Attribute("Name").Value;
                                                            Currentgroup.Elements("type").LastOrDefault().AddAfterSelf(new XElement("TypesIncluded", CurrentConfig));
                                                            if (!FinalGroup.Descendants("TypesIncluded").Where(x => x.Value.Equals(CurrentConfig)).Any())
                                                                FinalGroup.Add(Currentgroup);
                                                        //}
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
            string rate = "0.00";
            double minimum = double.MaxValue;
            foreach (XElement group in FinalGroup.Descendants("Room"))
            {
               
                double total = 0.0;
                foreach (XElement room1 in group.Descendants("type"))
                {                    
                    XElement room = salhotel.Descendants("RoomRate").Where(x => x.Attribute("RoomTypeCode").Value.Equals(room1.Value)).FirstOrDefault();
                    total += Convert.ToDouble(room.Descendants("Rate").Where(x => x.Attribute("ChargeType").Value.Equals("26")).FirstOrDefault()
                            .Element("Base").Attribute("AmountAfterTax").Value);
                }
                if (total < minimum)
                    minimum = total;
                rate = minimum.ToString();
            }
            return rate;
        }
        #endregion
        #region Merge Cancellation Policy
        public XElement MergCxlPolicy(List<XElement> rooms)
        {
            List<XElement> cxlList = new List<XElement>();

            IEnumerable<XElement> dateLst = rooms.Descendants("CancellationPolicy").
               GroupBy(r => new { r.Attribute("LastCancellationDate").Value, noshow = r.Attribute("NoShowPolicy").Value }).Select(y => y.FirstOrDefault()).
               OrderBy(p => DateTime.ParseExact( p.Attribute("LastCancellationDate").Value,"dd/MM/yyyy",CultureInfo.InvariantCulture));
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
                    foreach (var rm in rooms.Descendants("CancellationPolicy").OrderBy(x =>  DateTime.ParseExact( x.Attribute("LastCancellationDate").Value,"dd/MM/yyyy",CultureInfo.InvariantCulture)))
                    {

                        if (rm.Attribute("NoShowPolicy").Value == noShow && rm.Attribute("LastCancellationDate").Value == date)
                        {

                            if (counter == 1)
                                price = rm.Attribute("ApplicableAmount").Value;
                            else
                                price = (Convert.ToDouble(price) + Convert.ToDouble(rm.Attribute("ApplicableAmount").Value)).ToString();
                            datePrice += Convert.ToDecimal(price);
                            if (policynumberdatewise>1)
                                price = datePrice.ToString();
                            policynumberdatewise++;
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
        public DateTime chnagetoTime(string strDate)
        {
            DateTime oDate = DateTime.ParseExact(strDate, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            return oDate;

        }
        #endregion
        #region Break hotel list into chunks
        public List<List<T>> BreakIntoChunks<T>(List<T> list, int chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentException("chunkSize must be greater than 0.");
            }
            List<List<T>> retVal = new List<List<T>>();
            while (list.Count > 0)
            {
                int count = list.Count > chunkSize ? chunkSize : list.Count;
                retVal.Add(list.GetRange(0, count));
                list.RemoveRange(0, count);
            }

            return retVal;
        }
        #endregion
        #region Rooms
        //public XElement getRooms(XElement Salhotel, XElement Req)
        //{
        //    foreach (XElement type in Salhotel.Descendants("RoomType"))
        //    {
        //        List<XElement> typeForid = Salhotel.Descendants("RoomRate").Where(x => x.Attribute("RoomTypeCode").Value.Equals(type.Attribute("RoomTypeCode").Value))
        //                                    .Descendants("RoomRateDescription").Descendants("Text").ToList();
        //        type.Add(new XElement("ForRoomIds", typeForid));
        //    }
        //}
        #endregion
        #region Add String as Node
        public XElement stringAsNode(List<string> toBeAdded, XElement Element)
        {
            XElement response = Element;
            foreach (string s in toBeAdded)
                Element.Add(new XElement("Node", s));
            return response;
        }
        #endregion
        #region Room Details for booking
        public List<XElement> BookingRoomstay(XElement hotel, XElement Req, string CityID)
        {
            string HotelCity = CityID;
            #region Adding Occupancy Id
            int id = 1;
            foreach (XElement room in Req.Descendants("RoomPax"))
                room.Add(new XElement("id", id++.ToString()));
            #endregion
            List<XElement> CombinedResponse = new List<XElement>();
            XElement response = new XElement(OTANamespace + "RoomStay");
            List<XElement> RoomTypeList = new List<XElement>();
            List<XElement> RoomRateList = new List<XElement>();
            List<XElement> GuestCountList = new List<XElement>();
            List<XElement> GuestList = new List<XElement>();
            bool primaryAllowed = true;
            foreach(XElement Room in Req.Descendants("Room"))
            {              
                XElement ReqRoom = Req.Descendants("RoomPax").Where(x => x.Element("id").Value.Equals(Room.Attribute("OccupancyID").Value)).FirstOrDefault();
                string roomId = Room.FirstAttribute.Value.Split(new char[] { ',' })[0];
                XElement rtype = hotel.Descendants("RoomType").Where(x => x.Attribute("RoomTypeCode").Value.Equals(roomId)).FirstOrDefault();
                XElement RoomType = new XElement(OTANamespace+"RoomType",
                                        new XAttribute("NumberOfUnits", "1"),
                                        new XAttribute("RoomTypeCode", rtype.Attribute("RoomTypeCode").Value));
                RoomType.Add(new XElement(OTANamespace + "Occupancy",
                                 new XAttribute("AgeQualifyingCode", "10"),
                                 new XAttribute("MinOccupancy", ReqRoom.Element("Adult").Value)));
                if(ReqRoom.Descendants("ChildAge").Any())
                {                    
                    List<string> ages = ReqRoom.Descendants("ChildAge").Select(x => x.Value).OrderByDescending(x => Convert.ToInt32(x)).ToList();
                    var ageGroups = from age in ages
                                   group age by age;
                    foreach(var ageGroup in ageGroups)                    
                        RoomType.Add(new XElement(OTANamespace + "Occupancy",
                                    new XAttribute("AgeQualifyingCode", Convert.ToInt32(ageGroup.Key) > 2 ? "8" : "7"),
                                    new XAttribute("MinOccupancy", ageGroup.Count().ToString()),
                                    new XAttribute("MinAge", ageGroup.Key)));                                            
                }
                RoomType.Add(new XElement(OTANamespace + "TPA_Extension",
                                 new XElement(OTANamespace + "ResGuestRPH",
                                     new XAttribute("RPH", Room.Attribute("OccupancyID").Value))));
                RoomTypeList.Add(RoomType);
                XElement rRate = hotel.Descendants("RoomRate").Where(x => x.Attribute("RoomTypeCode").Value.Equals(roomId) &&
                                    x.Attribute("RatePlanCode").Value.Equals(Room.Attribute("SessionID").Value)).FirstOrDefault();
                RoomRateList.Add(new XElement(OTANamespace + "RoomRate",
                                     new XAttribute("RoomTypeCode", roomId),
                                     new XAttribute("RatePlanCode", Room.Attribute("SessionID").Value),
                                     new XElement(OTANamespace + "Rates",
                                         new XElement(OTANamespace + "Rate",
                                             new XElement(OTANamespace + "Base",
                                                 new XAttribute("AmountAfterTax", rRate.Descendants("Rate").Where(x => x.Attribute("ChargeType").Value.Equals("26"))
                                                                .FirstOrDefault().Element("Total").Attribute("AmountAfterTax").Value),
                                                 new XAttribute("CurrencyCode", rRate.Descendants("Rate").Where(x => x.Attribute("ChargeType").Value.Equals("26"))
                                                                .FirstOrDefault().Element("Total").Attribute("CurrencyCode").Value))))));
                #region Guest List
                foreach (XElement guestInThisRoom in Room.Descendants("PaxInfo"))
                {
                    if (!guestInThisRoom.Element("GuestType").Value.ToUpper().Equals("ADULT"))
                    {
                        GuestList.Add(new XElement(OTANamespace + "ResGuest",
                                                             new XAttribute("PrimaryIndicator","false"),
                                                             new XAttribute("ResGuestRPH", Room.Attribute("OccupancyID").Value),
                                                             new XAttribute("AgeQualifyingCode", getAgeCode(guestInThisRoom)),
                                                             new XAttribute("Age", guestInThisRoom.Element("Age").Value),
                                                             new XElement(OTANamespace + "Profiles",
                                                                 new XElement(OTANamespace + "ProfileInfo",
                                                                     new XElement(OTANamespace + "Profile",
                                                                         new XAttribute("ProfileType", "1"),
                                                                         new XElement(OTANamespace + "Customer",
                                                                             new XElement(OTANamespace + "PersonName",
                                                                                 new XElement(OTANamespace + "NamePrefix", guestInThisRoom.Element("Title").Value),
                                                                                 new XElement(OTANamespace + "GivenName", guestInThisRoom.Element("FirstName").Value),
                                                                                 new XElement(OTANamespace + "Surname", guestInThisRoom.Element("LastName").Value)),
                                                                                 new XElement(OTANamespace + "Email", "test@test.com")))))));
                        
                    }
                    else
                    {
                        GuestList.Add(new XElement(OTANamespace + "ResGuest",
                                                             new XAttribute("PrimaryIndicator", primaryAllowed?  guestInThisRoom.Element("IsLead").Value : "false"),
                                                             new XAttribute("ResGuestRPH", Room.Attribute("OccupancyID").Value),
                                                             new XAttribute("AgeQualifyingCode", getAgeCode(guestInThisRoom)),                                                             
                                                             new XElement(OTANamespace + "Profiles",
                                                                 new XElement(OTANamespace + "ProfileInfo",
                                                                     new XElement(OTANamespace + "Profile",
                                                                         new XAttribute("ProfileType", "1"),
                                                                         new XElement(OTANamespace + "Customer",
                                                                             new XElement(OTANamespace + "PersonName",
                                                                                 new XElement(OTANamespace + "NamePrefix", guestInThisRoom.Element("Title").Value),
                                                                                 new XElement(OTANamespace + "GivenName", guestInThisRoom.Element("FirstName").Value),
                                                                                 new XElement(OTANamespace + "Surname", guestInThisRoom.Element("LastName").Value)),
                                                                                 new XElement(OTANamespace + "Email", "test@test.com")))))));
                        if (primaryAllowed)
                            primaryAllowed = false;
                    }
                }
                #endregion
                #region Guest Count
                //int adult = 0, child = 0, infant = 0;
                //adult = Room.Descendants("GuestType").Where(x => x.Value.ToUpper().Equals("ADULT")).Count();
                //child = Room.Descendants("PaxInfo").Where(x => x.Descendants("GuestType").FirstOrDefault().Value.ToUpper().Equals("CHILD") && Convert.ToInt32(x.Descendants("Age").FirstOrDefault().Value) > 2).Count();
                //infant = Room.Descendants("PaxInfo").Count()-(adult + child);                
                //    //if (Convert.ToInt32(Room.Element("Child").Value) > 0)
                //    //{
                //    //    foreach (XElement childAges in Room.Descendants("ChildAge"))
                //    //    {
                //    //        if (Convert.ToInt32(childAges.Value) <= 2)
                //    //            infant++;
                //    //        else
                //    //            child++;
                //    //    }
                //    //}                
                //if (adult > 0)
                //    GuestCountList.Add(new XElement(OTANamespace + "GuestCount",
                //                    new XAttribute("AgeQualifyingCode", "10"),
                //                    new XAttribute("Count", adult.ToString())));
                //if (child > 0)
                //    GuestCountList.Add(new XElement(OTANamespace + "GuestCount",
                //                    new XAttribute("AgeQualifyingCode", "4"),
                //                    new XAttribute("Count", child.ToString())));
                //if (infant > 0)
                //    GuestCountList.Add(new XElement(OTANamespace + "GuestCount",
                //                    new XAttribute("AgeQualifyingCode", "3"),
                //                    new XAttribute("Count", infant.ToString())));
                #endregion
              
               
            }
            List<XElement> adults = RoomTypeList.Descendants(OTANamespace + "Occupancy").Where(x => x.FirstAttribute.Value.Equals("10")).ToList();
            int adultsCount = 0;
            foreach (XElement adult in adults)
                adultsCount += Convert.ToInt32(adult.Attribute("MinOccupancy").Value);
            GuestCountList.Add(new XElement(OTANamespace+"GuestCount",
                                    new XAttribute("AgeQualifyingCode", "10"),
                                    new XAttribute("Count", adultsCount.ToString())));
            var occupancyGroupings = from occupancy in RoomTypeList.Descendants(OTANamespace + "Occupancy").Where(x=>x.Attributes("MinAge").Any())
                                     group occupancy by occupancy.Attribute("MinAge").Value;
            foreach(var occupancyGroup in occupancyGroupings)
            {
                int occupCount = 0;
                foreach(XElement grp in occupancyGroup)
                {
                    occupCount += Convert.ToInt32(grp.Attribute("MinOccupancy").Value);
                }
                GuestCountList.Add(new XElement(OTANamespace + "GuestCount",
                                    new XAttribute("AgeQualifyingCode", occupancyGroup.FirstOrDefault().Attribute("AgeQualifyingCode").Value),
                                    new XAttribute("Count", occupCount.ToString()),
                                    new XAttribute("Age", occupancyGroup.Key)));
            }
            response.Add(new XElement(OTANamespace + "RoomTypes", RoomTypeList),
                               new XElement(OTANamespace + "RoomRates", RoomRateList),
                               new XElement(OTANamespace + "GuestCounts", GuestCountList),
                                    new XElement(OTANamespace + "TimeSpan",
                                     new XAttribute("Start", reformatDate(Req.Descendants("FromDate").FirstOrDefault().Value)),
                                     new XAttribute("End", reformatDate(Req.Descendants("ToDate").FirstOrDefault().Value))),
                                 new XElement(OTANamespace + "BasicPropertyInfo",
                                     new XAttribute("HotelCode", Req.Descendants("HotelID").FirstOrDefault().Value),
                                     new XAttribute("HotelCityCode", HotelCity)));

            CombinedResponse.Add(new XElement(OTANamespace + "RoomStays", response));
            CombinedResponse.Add(new XElement(OTANamespace + "ResGuests", GuestList));
            return CombinedResponse;
        }
        private string getAgeCode(XElement PaxInfo)
        {
            if (PaxInfo.Element("GuestType").Value.Equals("Adult"))
                return "10";
            else
            {
                if (Convert.ToInt32(PaxInfo.Element("Age").Value) <= 2)
                    return "7";
                else
                    return "8";
            }
        }
        #endregion
        #region Hotel Detail Images
        public XElement Images(string HotelID)
        {
            DataTable ImagesFromDB = sda.GetHotelImages(HotelID);
            XElement response = new XElement("Images");
            for (int i = 0; i < ImagesFromDB.Rows.Count;i++ )
            {
                DataRow dr = ImagesFromDB.Rows[i];
                XElement image = new XElement("Image", new XAttribute("Path", dr["ImageUrl"]), new XAttribute("Caption", dr["Caption"]));
                response.Add(image);
            }
            return response;
        }
        #endregion
        #region Booking response rooms
        public List<XElement> BookingRespRooms(XElement Rooms)
        {
            List<XElement> RoomResponse = new List<XElement>();
            foreach(XElement room in Rooms.Descendants("Room"))
            {
                RoomResponse.Add(new XElement("Room",
                                       new XAttribute("ID", room.Attribute("RoomTypeID").Value),
                                       new XAttribute("RoomType", room.Attribute("RoomType").Value),
                                       new XAttribute("ServiceID", ""),
                                       new XAttribute("MealPlanID", room.Attribute("MealPlanID").Value),
                                       new XAttribute("MealPlanName", ""),
                                       new XAttribute("MealPlanCode", ""),
                                       new XAttribute("MealPlanPrice", room.Attribute("MealPlanPrice").Value),
                                       new XAttribute("PerNightRoomRate", ""),
                                       new XAttribute("RoomStatus", "true"),
                                       new XAttribute("TotalRoomRate", room.Attribute("TotalRoomRate").Value),
                                       GuestsForBooking(room.Descendants("PaxInfo").ToList())));
            }
            return RoomResponse;
        }
        public List<XElement> GuestsForBooking(List<XElement> Guests)
        {
            List<XElement> Response = new List<XElement>();
            foreach(XElement guest in Guests)
            {                
                Response.Add(new XElement("RoomGuest",
                                new XElement("GuestType", guest.Element("GuestType").Value),
                                new XElement("Title", guest.Element("Title").Value),
                                new XElement("FirstName", guest.Element("FirstName").Value),
                                new XElement("MiddleName", guest.Element("MiddleName").Value),
                                new XElement("LastName", guest.Element("LastName").Value),
                                new XElement("IsLead", guest.Element("IsLead").Value),
                                new XElement("Age", guest.Element("Age").Value)));
            }
            return Response;
        }
        #endregion
        #region Duration
        public string Duration(string from, string to)
        {
            DateTime beginning = DateTime.ParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime End = DateTime.ParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            int TripDuration = End.Subtract(beginning).Days;
            return TripDuration.ToString();
        }            
        #endregion
        #region Get ID
        public string getIdBySplit(string fullString)
        {
            return fullString.Split(new char[] { ',' })[0];
        }
        #endregion
        #region Get Nights 
        public string getNights(string startDate, string endDate)
        {
            DateTime start = DateTime.ParseExact(startDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime end = DateTime.ParseExact(endDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            return end.Subtract(start).Days.ToString();
        }
        #endregion
        #region Supplements
        public List<XElement> Supplements(XElement Room)
        {
            List<XElement> Supplement = new List<XElement>();
            List<XElement> listsupplements = Room.Descendants("Fee").ToList();
            foreach (XElement roomsoffer in listsupplements)
            {
                /// Amount need to be calculated
                Supplement.Add(new XElement("Supplement",
                          new XAttribute("suppId",""),
                          new XAttribute("suppName", roomsoffer.Attribute("Code").Value),
                          new XAttribute("supptType", roomsoffer.Attribute("ChargeFrequency").Value),
                          new XAttribute("suppIsMandatory", roomsoffer.Attribute("MandatoryIndicator").Value),
                          new XAttribute("suppChargeType", "Addition"),
                          new XAttribute("suppPrice", roomsoffer.Attribute("Amount").Value),
                          new XAttribute("suppType", roomsoffer.Attribute("Type").Value)));
            }

            return Supplement;
        }
        #endregion
        #region Modify Request for Pre-Book
        public XElement ModifyRequest(XElement Req)
        {
            foreach(XElement room in Req.Descendants("RoomPax"))
            {
                List<XElement> PaxList = new List<XElement>();
                int adult = Convert.ToInt32(room.Element("Adult").Value);
                for (int i = 0; i < adult; i++)
                    PaxList.Add(new XElement("PaxInfo",
                                new XElement("GuestType", "Adult"),
                                new XElement("Title", "Mr"),
                                new XElement("FirstName", "AgentFirstName"),
                                new XElement("MiddleName"),
                                new XElement("LastName", "AgentLastName"),
                                new XElement("Age","30"),
                                new XElement("IsLead","true")));
                foreach(XElement age in room.Descendants("ChildAge"))
                {
                    PaxList.Add(new XElement("PaxInfo",
                                new XElement("GuestType", "Child"),
                                new XElement("Title", "Mr"),
                                new XElement("FirstName", "AgentFirstName"),
                                new XElement("MiddleName"),
                                new XElement("LastName", "AgentLastName"),
                                new XElement("Age", age.Value),
                                new XElement("IsLead")));
                }
                Req.Descendants("Room").Where(x => !x.Descendants("PaxInfo").Any()).FirstOrDefault().Add(PaxList);
            }
            return Req;
        }
        #endregion
        #endregion

    }
}