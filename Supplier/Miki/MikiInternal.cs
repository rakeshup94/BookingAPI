using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Xml.Linq;
using System.Data;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.MIKI;
using TravillioXMLOutService.Models.Miki;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using TravillioXMLOutService.Common.Miki;
using System.Globalization;

namespace TravillioXMLOutService.Supplier.Miki
{
    public class MikiInternal : IDisposable
    {
        Utility util = new Utility();
        MikiStaticData msd = new MikiStaticData();
        MikiLogSave model = new MikiLogSave();
        bool paxresponse;
        string XmlPath = ConfigurationManager.AppSettings["MikiPath"];
        string supplierID = "11";
        string dmc = string.Empty;
        string customerid = string.Empty;
        string hotelcode = string.Empty;
        string supcurncy = string.Empty;
        XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";
        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
        MikiExternal mikirep = new MikiExternal();
        XDocument servresp;
        string requestDateTime = DateTime.Now.Date.ToString("yyyy-MM-dd") + "T" + Convert.ToString(DateTime.Now.TimeOfDay).Substring(0, 8);

        #region Hotel Search
        public List<XElement> HotelSearchRequest(XElement Req, string custID, string xtype)
        {
            dmc = xtype;
            customerid = custID;
            List<XElement> tobereturned = new List<XElement>();
            try
            {
                string HtId = Req.Descendants("HotelID").FirstOrDefault().Value;
                if (!string.IsNullOrEmpty(HtId))
                {
                    using (MikiService mSrv = new MikiService(customerid, supplierID, xtype))
                    {
                        var list = mSrv.SearchByHotel(Req);
                        tobereturned = list.Descendants("Hotel").ToList();
                    }

                }
                else
                {

                    Random rnd = new Random();
                    #region Static Data Fetch
                    //List<string> Currencies = new List<string>(new string[] { "EUR", "GBP", "USD" });
                    //string currency = null;
                    //currency = Currencies.Contains(Req.Descendants("DesiredCurrencyCode").FirstOrDefault().Value) ? Req.Descendants("DesiredCurrencyCode").FirstOrDefault().Value : Currencies.FirstOrDefault();
                    string countryID = Req.Descendants("CountryCode").FirstOrDefault().Value;
                    string mikiCity = supplierCityID(Req.Descendants("CityID").FirstOrDefault().Value);
                    if (string.IsNullOrEmpty(mikiCity))
                        return null;
                    #endregion


                    #region Request XML
                    XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "11");
                    string username = suppliercred.Descendants("username").FirstOrDefault().Value;
                    string password = suppliercred.Descendants("password").FirstOrDefault().Value;
                    string currency = suppliercred.Descendants("currency").FirstOrDefault().Value;
                    XDocument mikireq = new XDocument(
                                       new XDeclaration("1.0", "utf-8", "yes"),
                                       new XElement(soap + "Envelope",
                                           new XAttribute(XNamespace.Xmlns + "soap", soap),
                                           new XElement(soap + "Header"),
                                           new XElement(soap + "Body",
                                       new XElement("hotelSearchRequest",
                                           new XAttribute("versionNumber", "7.0"),
                                           new XElement("requestAuditInfo",
                                               new XElement("agentCode", username),
                                               new XElement("requestPassword", password),
                                               new XElement("requestID", Convert.ToString(rnd.Next(999999999))),
                                               new XElement("requestDateTime", requestDateTime)),
                                           new XElement("hotelSearchCriteria",
                                              new XAttribute("currencyCode", currency),
                                               new XAttribute("paxNationality", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                               new XAttribute("languageCode", "en"),
                                               new XElement("destination",

                                                   new XElement("cityNumbers",
                                                       new XElement("cityNumber", Convert.ToString(mikiCity)))),
                                               new XElement("stayPeriod",
                                                   new XElement("checkinDate", reformatDate(Req.Descendants("FromDate").FirstOrDefault().Value)),

                                                   new XElement("checkoutDate", reformatDate(Req.Descendants("ToDate").FirstOrDefault().Value))),
                                               searchRooms(Req),
                                               new XElement("availabilityCriteria",
                                                   new XElement("availabilityIndicator", "2")),
                                               new XElement("priceCriteria",
                                                   new XElement("returnBestPriceIndicator", "true")),
                                               new XElement("hotelCriteria",
                                                 util.starRating(Req)),
                                               new XElement("resultDetails",
                                                   new XElement("returnDailyPrices", "1"),
                                                   new XElement("returnHotelInfo", "1"),
                                                   new XElement("returnSpecialOfferDetails", "1")))))));
                    #endregion
                    XElement response = HotelSearchResponse(mikireq, Req);
                    removetags(Req);
                    tobereturned = response.Descendants("Hotel").ToList();
                    if (tobereturned.Count > 0)
                        tobereturned.Descendants("Hotel")
                            .Where(x => Convert.ToDouble(x.Descendants("MinRate").FirstOrDefault().Value) == 0)
                            .Remove();
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelSearchRequest";
                ex1.PageName = "MikiInternal";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
                #endregion
            }
            return tobereturned;
        }

        #region Response
        public XElement HotelSearchResponse(XDocument mikireq, XElement travyoreq)
        {
            XElement tryresponse = null;
            model.CustomerID = Convert.ToInt32(customerid);
            model.Logtype = "Search";
            model.LogtypeID = 1;
            model.TrackNo = travyoreq.Descendants("TransID").FirstOrDefault().Value;
            servresp = mikirep.MikiResponse(mikireq, "hotelSearch", customerid, model);
            msd.SavedHotels = servresp.Descendants("hotelSearchResponse").FirstOrDefault();
            #region Removing Namespace
            XElement mikireqnns = removeAllNamespaces(mikireq.Root);
            XElement servrespnns = removeAllNamespaces(servresp.Root);
            #endregion
            if (servresp.Descendants("totalAvailable").Any())
            {
                int availabale = Convert.ToInt32(servresp.Descendants("totalAvailable").FirstOrDefault().Value);


                if (availabale > 0)
                {
                    #region Response Data
                    string mikiCity = mikireqnns.Descendants("cityNumber").FirstOrDefault().Value;
                    string countryID = travyoreq.Descendants("CountryID").FirstOrDefault().Value;
                    string countryName = travyoreq.Descendants("CountryName").FirstOrDefault().Value;
                    string countryCode = travyoreq.Descendants("CountryCode").FirstOrDefault().Value;
                    string cityID = travyoreq.Descendants("CityID").FirstOrDefault().Value;
                    string cityName = travyoreq.Descendants("CityName").FirstOrDefault().Value;
                    string cityCode = travyoreq.Descendants("CityCode").FirstOrDefault().Value;
                    string RequestID = travyoreq.Descendants("TransID").FirstOrDefault().Value;
                    #endregion
                    #region Response XML
                    #region By LinQ (Commented)
                    //var searchresponse = from hotels in servresp.Descendants("hotel")
                    //                     join stathtl in staticdata.Descendants("hotel")
                    //                     on hotels.Descendants("productCode").FirstOrDefault().Value equals stathtl.Attribute("productCode").Value
                    //                     select new XElement("Hotel",
                    //                                      new XElement("HotelID", hotels.Element("productCode").Value),
                    //                                      new XElement("HotelName", hotels.Descendants("hotelName").FirstOrDefault().Value),
                    //                                      new XElement("PropertyTypeName", "Hotel"),
                    //                                      new XElement("CountryID", countryID),
                    //                                      new XElement("CountryName", countryName),
                    //                                      new XElement("CountryCode", countryCode),
                    //                                      new XElement("CityId", cityID),
                    //                                      new XElement("CityCode", cityCode),
                    //                                      new XElement("CityName", cityName),
                    //                                      new XElement("AreaId", AreaID),
                    //                                      new XElement("AreaName"),
                    //                                      new XElement("RequestID", RequestID),
                    //                                      new XElement("Address", getAddress(stathtl.Descendants("address").FirstOrDefault().Elements())),
                    //                                      new XElement("Location", location(stathtl.Descendants("locationInfo").FirstOrDefault().Elements("location").FirstOrDefault())),
                    //                                      new XElement("Description", stathtl.Descendants("productDetailText").FirstOrDefault().Value),
                    //                                      new XElement("StarRating", hotels.Descendants("starRating").FirstOrDefault().Value),
                    //                                      new XElement("MinRate", MinRate(hotels.Descendants("roomOptions").FirstOrDefault())),
                    //                                      new XElement("HotelImgSmall", ImageFromdb(hotels.Element("productCode").Value, "small")),
                    //                                      new XElement("HotelImgLarge", ImageFromdb(hotels.Element("productCode").Value, "large")),
                    //                                      new XElement("MapLink"),
                    //                                      new XElement("Longitude", stathtl.Descendants("geoLocation").FirstOrDefault().Attribute("longitude").Value),
                    //                                      new XElement("Latitude", stathtl.Descendants("geoLocation").FirstOrDefault().Attribute("latitude").Value),
                    //                                      new XElement("DMC", "MIKI"),
                    //                                      new XElement("SupplierID", supplierID),
                    //                                      new XElement("Currency", servresp.Descendants("currencyCode").FirstOrDefault().Value),
                    //                                     hotels.Descendants("specialOffers").Any() ? offerList(hotels.Descendants("specialOffers").FirstOrDefault()) : new XElement("Offers", "No offers available"),
                    //                         //new XElement("Offers"),
                    //                                      Facilities(stathtl.Descendants("hotelFacilities").FirstOrDefault()),
                    //                                     new XElement("Rooms")); 
                    #endregion

                    string xmlouttype = string.Empty;
                    try
                    {
                        if (dmc == "Miki")
                        {
                            xmlouttype = "false";
                        }
                        else
                        { xmlouttype = "true"; }
                    }
                    catch { }

                    var trysearchResponse = from hotels in servresp.Descendants("hotel")
                                            select hotels;
                    XElement tryHotels = new XElement("Hotels");
                    DataTable Details = null;
                    Details = msd.GetMiki_HotelDetails(mikiCity);
                    DataTable Images = msd.GetMiki_Images(mikiCity);
                    //XElement FacilityByCity = Facilities(mikiCity);
                    foreach (var hotel in trysearchResponse)
                    {

                        string minstar = travyoreq.Descendants("MinStarRating").FirstOrDefault().Value;
                        string maxstar = travyoreq.Descendants("MaxStarRating").FirstOrDefault().Value;
                        string hotelStar = hotel.Descendants("starRating").FirstOrDefault().Value;
                        string HotelID = hotel.Descendants("productCode").FirstOrDefault().Value;
                        var result = Details.AsEnumerable().Where(dt => dt.Field<string>("HotelID") == HotelID);
                        int countre = result.Count();
                        DataRow[] drow = result.ToArray();
                        DataRow[] imageArray = Images.AsEnumerable().Where(img => img["HotelID"].ToString().Equals(HotelID)).ToArray();
                        if (drow.Length > 0 && StarRating(minstar, maxstar, hotelStar))
                        {
                            //XElement Facility = null;
                            //if (FacilityByCity.Descendants("Facilities").Where(x => x.FirstAttribute.Value.Equals(HotelID)).Any())
                            //    Facility = FacilityByCity.Descendants("Facilities").Where(x => x.FirstAttribute.Value.Equals(HotelID)).FirstOrDefault();
                            //else
                            //    Facility = new XElement("Facilities", new XElement("Facility", "No Facility Found"));
                            string smallImage = string.Empty, LargeImage = string.Empty;
                            if (imageArray.Length > 0)
                            {
                                DataRow HotelImg = imageArray.AsEnumerable().Where(x => x["ImageType"].ToString().Equals("01")).FirstOrDefault();
                                if (HotelImg != null)
                                {
                                    smallImage = HotelImg["ImageThumbnail"].ToString();
                                    LargeImage = HotelImg["ImageLarge"].ToString();
                                }
                            }
                            DataRow dr = drow[0];
                            try
                            {
                                tryHotels.Add(new XElement("Hotel",
                                                 new XElement("HotelID", HotelID),
                                                 new XElement("HotelName", dr["HotelName"].ToString()),
                                                 new XElement("PropertyTypeName", "Hotel"),
                                                 new XElement("CountryID", countryID),
                                                 new XElement("CountryName", countryName),
                                                 new XElement("CountryCode", countryCode),
                                                 new XElement("CityId", cityID),
                                                 new XElement("CityCode", cityCode),
                                                 new XElement("CityName", cityName),
                                                 new XElement("AreaId"),
                                                 new XElement("AreaName", dr["Location"].ToString()),
                                                 new XElement("RequestID", RequestID),
                                                 new XElement("Address", dr["HotelAddress"].ToString()),
                                                 new XElement("Location"),
                                                 new XElement("Description", dr["HotelDescription"].ToString()),
                                                 new XElement("StarRating", dr["StarRating"].ToString()),
                                                 new XElement("MinRate", hotel.Descendants("roomOptions").Any() ? MinRate(hotel.Descendants("roomOptions").FirstOrDefault(), travyoreq) : "0"),
                                                 new XElement("HotelImgSmall", smallImage),
                                                 new XElement("HotelImgLarge", LargeImage),
                                                 new XElement("MapLink"),
                                                 new XElement("Longitude", dr["Longitude"].ToString()),
                                                 new XElement("Latitude", dr["Latitude"].ToString()),
                                                 new XElement("xmloutcustid", customerid),
                                                 new XElement("xmlouttype", xmlouttype),
                                                 new XElement("DMC", dmc),
                                                 new XElement("SupplierID", supplierID),
                                                 new XElement("Currency", servresp.Descendants("currencyCode").Any() ? servresp.Descendants("currencyCode").FirstOrDefault().Value : string.Empty),
                                    //hotel.Descendants("specialOffers").Any() ? offerList(hotel.Descendants("specialOffers").FirstOrDefault()) : new XElement("Offers", "No offers available"),
                                                 new XElement("Offers"),
                                                 new XElement("Facilities", null),
                                             new XElement("Rooms")));
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    tryresponse = new XElement("searchResponse", tryHotels);
                    //response = new XElement("searchResponse",
                    //               new XElement("Hotels", searchresponse));
                    #endregion
                }
                else
                {
                    #region Error Text
                    var errors = from err in servresp.Descendants("error")
                                 select new XElement("ErrorTxt", err.Element("description").Value);
                    tryresponse = new XElement("searchResponse", errors);
                    #endregion
                }
            }
            else
            {
                #region Error Text
                var errors = from err in servresp.Descendants("error")
                             select new XElement("ErrorTxt", err.Element("description").Value);
                if (errors.Count() == 0)
                {
                    tryresponse.Add(new XElement("ErrorTxt", "Server not Responding"));
                    return null;
                }
                tryresponse = null;
                #endregion
            }
            //return response;
            return tryresponse;

        }
        #endregion
        #endregion
        #region Room Availability
        public XElement GetRoomAvail_mikiOUT(XElement req)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            XElement getrm = null;
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "11").ToList();
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
                        dmc = "Miki";
                    }
                    roomavailabilityresponse.Add(availableRooms(req, dmc, htlid));
                }
                #endregion
                getrm = new XElement("TotalRooms", roomavailabilityresponse);
                return getrm;
            }
            catch { return null; }
        }
        public XElement availableRooms(XElement Req, string xtype, string htlid)
        {
            try
            {
                //try
                //{
                //    int totroom = Req.Descendants("RoomPax").Count();
                //    if(totroom>4)
                //    {
                //        return null;
                //    }
                //}
                //catch { }
                dmc = xtype;
                hotelcode = htlid;
                Random rnd = new Random();
                XElement RoomsList = null;
                #region Credentials
                string username = Req.Descendants("UserName").Single().Value;
                string password = Req.Descendants("Password").Single().Value;
                string AgentID = Req.Descendants("AgentID").Single().Value;
                string ServiceType = Req.Descendants("ServiceType").Single().Value;
                string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
                #endregion
                #region Static Data Fetch
                string mikiCity = supplierCityID(Req.Descendants("CityID").FirstOrDefault().Value);
                DataTable Details = msd.GetMiki_HotelDetails(mikiCity);
                #endregion
                try
                {
                    #region Currency
                    string Currency = Roomlist(Req.Descendants("TransID").FirstOrDefault().Value).Descendants("hotel").
                                      Where(x => x.Descendants("productCode").FirstOrDefault().Value.Equals(htlid)).FirstOrDefault().Element("currencyCode").Value;
                    #endregion
                    #region Request XML
                    XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "11");
                    string susername = suppliercred.Descendants("username").FirstOrDefault().Value;
                    string spassword = suppliercred.Descendants("password").FirstOrDefault().Value;
                    XDocument mikireq = new XDocument(
                                     new XDeclaration("1.0", "utf-8", "yes"),
                                     new XElement(soap + "Envelope",
                                         new XAttribute(XNamespace.Xmlns + "soap", soap),
                                         new XElement(soap + "Header"),
                                         new XElement(soap + "Body",
                                     new XElement("hotelSearchRequest",
                                         new XAttribute("versionNumber", "7.0"),
                                         new XElement("requestAuditInfo",
                                             new XElement("agentCode", susername),
                                             new XElement("requestPassword", spassword),
                                             new XElement("requestID", Convert.ToString(rnd.Next(999999999))),
                                             new XElement("requestDateTime", requestDateTime)),
                                         new XElement("hotelSearchCriteria",
                                              new XAttribute("currencyCode", Currency),
                                             new XAttribute("paxNationality", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                             new XAttribute("languageCode", "en"),
                                             new XElement("destination",
                                           new XElement("hotelRefs",
                                           new XElement("productCodes",
                                               new XElement("productCode", htlid)))),
                                             new XElement("stayPeriod",
                                                 new XElement("checkinDate", reformatDate(Req.Descendants("FromDate").FirstOrDefault().Value)),

                                                 new XElement("checkoutDate", reformatDate(Req.Descendants("ToDate").FirstOrDefault().Value))),
                                            searchRooms(Req),
                                             new XElement("availabilityCriteria",
                                                 new XElement("availabilityIndicator", "2")),
                                             new XElement("priceCriteria",
                                                 new XElement("returnBestPriceIndicator", "false")),
                                             new XElement("hotelCriteria",
                                                 util.starRating(Req)),
                                             new XElement("resultDetails",
                                                 new XElement("returnDailyPrices", "1"),
                                                 new XElement("returnHotelInfo", "1"),
                                                 new XElement("returnSpecialOfferDetails", "1")))))));
                    #endregion
                    RoomsList = roomResponse(Req, Details, mikireq);

                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "availableRooms";
                    ex1.PageName = "MikiInternal";
                    ex1.CustomerID = customerid;
                    ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                    //APILog.SendCustomExcepToDB(ex1);
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #region Response Format
                removeIdTag(Req);
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
                                                           removeAllNamespaces(RoomsList))));
                #endregion

                return AvailablilityResponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "availableRooms";
                ex1.PageName = "MikiInternal";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        #region Response
        public XElement roomResponse(XElement travyoReq, DataTable Details, XDocument mikireq)
        {
            string hotelID = hotelcode;
            //XElement resp = Roomlist(travyoReq.Descendants("TransID").FirstOrDefault().Value).Descendants("hotel").
            //               Where(x => x.Descendants("productCode").FirstOrDefault().Value.Equals(hotelID)).FirstOrDefault();
            model.CustomerID = Convert.ToInt32(customerid);
            model.TrackNo = travyoReq.Descendants("TransID").FirstOrDefault().Value;
            model.LogtypeID = 2;
            model.Logtype = "RoomAvail";
            XElement Supplresp = mikirep.MikiResponse(mikireq, "hotelSearch", customerid, model).Root;
            XElement resp = removeAllNamespaces(Supplresp);
            XElement RoomAvailability = new XElement("searchResponse");
            //int availabale = Convert.ToInt32(servresp.Descendants("totalAvailable").FirstOrDefault().Value);
            int roomCount = travyoReq.Descendants("RoomPax").Count();
            var result = Details.AsEnumerable().Where(dt => dt.Field<string>("HotelID") == hotelID);
            int countre = result.Count();
            DataRow[] drow = result.ToArray();
            if (!resp.Descendants("error").Any() && drow.Length > 0)
            {
                DataRow dr = drow[0];
                #region Response Data
                string HotelID = resp.Descendants("productCode").FirstOrDefault().Value;
                #endregion
                #region Response XML
                supcurncy = resp.Descendants("currencyCode").FirstOrDefault().Value;
                var availableRooms =
                                       new XElement("Hotel",
                                        new XElement("HotelID", HotelID),
                                        new XElement("HotelName", dr[1]),
                                        new XElement("PropertyTypeName"),
                                        new XElement("CountryID"),

                                        //new XElement("CountryCode"),
                    //new XElement("CountryName"),
                    //new XElement("CityId", travyoReq.Descendants("CityID").FirstOrDefault().Value),
                    //new XElement("CityCode", travyoReq.Descendants("CityCode").FirstOrDefault().Value),
                    //new XElement("CityName", travyoReq.Descendants("CityName").FirstOrDefault().Value),
                    //new XElement("AreaName"),
                    //new XElement("AreaId", dr[7]),
                    //new XElement("Address", dr[4]),
                    //new XElement("Location"),
                    //new XElement("Description", dr[5]),
                    //new XElement("StarRating", dr[6]),
                    ////new XElement("MinRate", MinRate(resp.Descendants("roomOptions").FirstOrDefault(), travyoReq)),
                    // new XElement("MinRate", ""),
                    //new XElement("HotelImgSmall", ImageFromdb(HotelID, "small")),
                    //new XElement("HotelImgLarge", ImageFromdb(HotelID, "large")),
                    //new XElement("MapLink"),
                    //new XElement("Longitude", dr[3]),
                    //new XElement("Latitude", dr[2]),
                                        new XElement("DMC", dmc),
                                        new XElement("SupplierID", supplierID),
                                        new XElement("Currency", resp.Descendants("currencyCode").FirstOrDefault().Value),
                                       new XElement("Offers"),
                    //new XElement("Facilities",new XElement("Facility","No facility available")),
                                        Rooms(resp.Descendants("roomOptions").FirstOrDefault(), travyoReq));
                RoomAvailability.Add(new XElement("Hotels", availableRooms));

                #endregion
            }
            else
            {
                #region Error Text
                //var errors = from err in servresp.Descendants("error")
                //             select new XElement("ErrorTxt", err.Element("description").Value);
                RoomAvailability.Element("searchResponse").SetValue(
                                                                        new XElement("ErrorText", "No Result Found in database"));
                #endregion
            }
            return RoomAvailability;
        }
        #endregion
        #endregion
        #region Pre Booking
        public XElement PrebookingRequest(XElement Req, string xmlout)
        {
            XElement response = null;
            dmc = xmlout;
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion

            try
            {
                Random rnd = new Random();
                #region Request XML
                XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "11");
                string susername = suppliercred.Descendants("username").FirstOrDefault().Value;
                string spassword = suppliercred.Descendants("password").FirstOrDefault().Value;
                XDocument mikireq = new XDocument(
                                 new XDeclaration("1.0", "utf-8", "yes"),
                                 new XElement(soap + "Envelope",
                                     new XAttribute(XNamespace.Xmlns + "soap", soap),
                                     new XElement(soap + "Header"),
                                     new XElement(soap + "Body",
                                 new XElement("hotelSearchRequest",
                                     new XAttribute("versionNumber", "7.0"),
                                     new XElement("requestAuditInfo",
                                         new XElement("agentCode", susername),
                                         new XElement("requestPassword", spassword),
                                         new XElement("requestID", Convert.ToString(rnd.Next(999999999))),
                                         new XElement("requestDateTime", requestDateTime)),
                                     new XElement("hotelSearchCriteria",
                                          new XAttribute("currencyCode", Req.Descendants("CurrencyName").FirstOrDefault().Value),
                                         new XAttribute("paxNationality", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                         new XAttribute("languageCode", "en"),
                                         new XElement("destination",
                                       new XElement("hotelRefs",
                                       new XElement("productCodes",
                                           new XElement("productCode", Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value)))),
                                         new XElement("stayPeriod",
                                             new XElement("checkinDate", reformatDate(Req.Descendants("FromDate").FirstOrDefault().Value)),
                                             new XElement("checkoutDate", reformatDate(Req.Descendants("ToDate").FirstOrDefault().Value))),
                                        searchRooms(Req),
                                         new XElement("availabilityCriteria",
                                             new XElement("availabilityIndicator", "2")),
                                         new XElement("priceCriteria",
                                             new XElement("returnBestPriceIndicator", "false")),
                                         new XElement("hotelCriteria",
                                             util.starRating(Req)),
                                         new XElement("resultDetails",
                                             new XElement("returnDailyPrices", "1"),
                                             new XElement("returnHotelInfo", "1"),
                                             new XElement("returnSpecialOfferDetails", "1")))))));
                #endregion

                response = PreBookingResponse(mikireq, Req);

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PrebookingRequest";
                ex1.PageName = "MikiInternal";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            #region Response Format
            XElement prebookingfinal = null;
            string oldprice = Req.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value;
            string newprice = Req.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value;
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

                                                          new XElement(Req.Descendants("HotelPreBookingRequest").FirstOrDefault()), response)));
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
        }
        #region  Response
        public XElement PreBookingResponse(XDocument mikireq, XElement travayooreq)
        {
            string hotelID = travayooreq.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value;
            model.CustomerID = Convert.ToInt32(travayooreq.Descendants("CustomerID").FirstOrDefault().Value);
            model.TrackNo = travayooreq.Descendants("TransID").FirstOrDefault().Value;
            model.Logtype = "Prebook";
            model.LogtypeID = 4;
            //servresp = mikirep.MikiResponse(mikireq, "hotelSearch", travayooreq.Descendants("CustomerID").FirstOrDefault().Value, model);
            //XElement resp = Roomlist(travayooreq.Descendants("TransID").FirstOrDefault().Value).Descendants("hotel").
            //                Where(x => x.Descendants("productCode").FirstOrDefault().Value.Equals(hotelID)).FirstOrDefault();
            DateTime starttime = DateTime.Now;
            XElement resp = LogXMLs(travayooreq.Descendants("TransID").FirstOrDefault().Value, 2, 11).Descendants("hotel").
                                Where(x => x.Descendants("productCode").FirstOrDefault().Value.Equals(hotelID)).LastOrDefault();

            XElement response = new XElement("HotelPreBookingResponse");
            #region Log Save
            try
            {
                #region Removing Namespace
                XElement mikireqnns = removeAllNamespaces(mikireq.Root);
                XElement serverespnns = removeAllNamespaces(resp);
                #endregion
                #region Log Save
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(travayooreq.Descendants("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = travayooreq.Descendants("TransID").FirstOrDefault().Value;
                    log.SupplierID = 11;
                    log.logrequestXML = travayooreq.ToString();
                    log.logresponseXML = resp.ToString();
                    log.LogTypeID = 4;
                    log.LogType = "PreBook";
                    log.StartTime = starttime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "PreBookingResponse";
                    ex1.PageName = "MikiInternal";
                    ex1.CustomerID = travayooreq.Descendants("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = travayooreq.Descendants("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBookingResponse";
                ex1.PageName = "MikiInternal";
                ex1.CustomerID = travayooreq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travayooreq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            var responseRooms = resp.Descendants("roomOptions").FirstOrDefault();
            var tester = from resprooms in responseRooms.Descendants("roomOption")
                         join reqrooms in travayooreq.Descendants("Room")
                         on resprooms.Attribute("roomTypeCode").Value equals reqrooms.Attribute("ID").Value

                         select new XElement("tester", resprooms);
            //int availability = Convert.ToInt32(resp.Descendants("totalAvailable").FirstOrDefault().Value);
            if (resp != null)
            {
                List<string> status = new List<string>();
                foreach (XElement roomoption in tester)
                    status.Add(roomoption.Descendants("availabilityStatus").FirstOrDefault().Value);
                string statusvalue = null;
                if (status.Contains("2"))
                    statusvalue = "false";
                else
                    statusvalue = "true";

                #region Terms & Condition
                string TnC = string.Empty;
                foreach (XElement alert in resp.Descendants("alert"))
                {
                    TnC = "<ul><b> " + alert.Element("title").Value + "</b><br><br><li>" + alert.Element("description").Value + "</li>" + "</ul>";
                }
                #endregion
                #region Response XML

                var preBookResponse =
                                                    new XElement("Hotels",
                                                       new XElement("Hotel",
                                                           new XElement("HotelID", hotelID),
                                                           new XElement("HotelName", resp.Descendants("hotelName").FirstOrDefault().Value),
                                                           new XElement("Status", statusvalue),
                                                           new XElement("TermCondition", TnC),
                                                           new XElement("NewPrice"),
                                                           new XElement("HotelImgSmall", ImageFromdb(hotelID, "small")),
                                                           new XElement("HotelImgLarge", ImageFromdb(hotelID, "large")),
                                                           new XElement("MapLink"),
                                                           new XElement("DMC", dmc),
                                                           new XElement("Currency", resp.Descendants("currencyCode").FirstOrDefault().Value),
                                                            new XElement("Offers"),
                                                          PreRooms(resp.Descendants("roomOptions").FirstOrDefault(), travayooreq)
                                                           ));
                response.Add(preBookResponse);
                #endregion
            }
            else
            {
                #region Error Text
                var errors = from err in resp.Descendants("error")
                             select new XElement("ErrorTxt", err.Element("description").Value);
                if (errors.Count() == 0)
                {
                    response.Add(new XElement("ErrorTxt", "Server not responding"));
                    return response;
                }
                response.Element("HotelPreBookingResponse").SetValue(
                                                                      errors);
                #endregion
            }
            return response;
        }
        #region Rooms for PreBooking
        public XElement PreRooms(XElement mikiRooms, XElement travayooreq)
        {
            int cnt = 1;
            foreach (XElement roomPax in travayooreq.Descendants("RoomPax"))
                roomPax.Add(new XElement("id", cnt++));
            List<string> rateIdentifiers = mikiRooms.Descendants("roomOption").Select(x => x.Descendants("rateIdentifier").FirstOrDefault().Value).ToList();
            List<XElement> selectedMikiRooms = mikiRooms.Descendants("roomOption").Where(x => rateIdentifiers.Contains(x.Descendants("rateIdentifier").FirstOrDefault().Value)).ToList();
            List<XElement> Roomtag = new List<XElement>();
            foreach (XElement room in travayooreq.Descendants("Room"))
            {
                XElement b2bRoom = travayooreq.Descendants("RoomPax").Where(x => x.Element("id").Value.Equals(room.Attribute("RoomSeq").Value)).FirstOrDefault();
                XElement mikiRoom = selectedMikiRooms.Where(x => x.Descendants("rateIdentifier").FirstOrDefault().Value.Equals(room.Element("RequestID").Value)).FirstOrDefault();
                Roomtag.Add(new XElement("Room",
                                new XAttribute("ID", room.Attribute("ID").Value),
                                new XAttribute("SuppliersID", room.Attribute("SuppliersID").Value),
                                new XAttribute("RoomSeq", room.Attribute("RoomSeq").Value),
                                new XAttribute("SessionID", room.Attribute("SessionID").Value),
                                new XAttribute("RoomType", room.Attribute("RoomType").Value),
                                new XAttribute("OccupancyID", room.Attribute("OccupancyID").Value),
                                new XAttribute("OccuapncyName", ""),
                                new XAttribute("MealPlanID", room.Attribute("MealPlanID").Value),
                                new XAttribute("MealPlanName", room.Attribute("MealPlanName").Value),
                                new XAttribute("MealPlanCode", room.Attribute("MealPlanCode").Value),
                                new XAttribute("MealPlanPrice", ""),
                                new XAttribute("PerNightRoomRate", room.Attribute("PerNightRoomRate").Value),
                                new XAttribute("TotalRoomRate", room.Attribute("TotalRoomRate").Value),
                                new XAttribute("CancellationDate", ""),
                                new XAttribute("CancellationAmount", ""),
                                new XAttribute("isAvailable", "true"),
                                new XElement("RequestID", room.Element("RequestID").Value),
                                new XElement("Offers"),
                                new XElement("PromotionList", new XElement("Promotion")),
                                new XElement("CancellationPolicy"),
                                new XElement("Amenities", new XElement("Amenity")),
                                new XElement("Images", new XElement("Image",
                                                            new XAttribute("Path", ""), new XAttribute("Caption", ""))),
                                room.Descendants("Supplements").FirstOrDefault(),
                                pb(mikiRoom),
                                new XElement("AdultNum", b2bRoom.Element("Adult").Value),
                                new XElement("ChildNum", b2bRoom.Element("Child").Value)));
            }
            List<double> a = Roomtag.Descendants("Room").Select(x => Convert.ToDouble(x.Attribute("TotalRoomRate").Value)).ToList();

            double totalRate = a.Sum();
            ////XElement getrooms = null;
            ////var abc = from x in req.Descendants("roomOption")
            ////          select getrooms = GetRooms(req, travayooreq, mikireq);
            //int count = 1;
            ////int cnt = 1;
            //int rc = travayooreq.Descendants("RoomPax").Count();
            ////string totalrate = getrooms.Element("TotalRate").Value;
            ////getrooms.Descendants("TotalRate").Remove();

            //var occupancy = from pax in travayooreq.Descendants("RoomPax")
            //                select pax;
            //foreach (var p in occupancy)
            //    p.Add(new XElement("id", cnt++));
            //foreach (var pax in occupancy)
            //    travayooreq.Descendants("Room")
            //        .Where(x => x.Attribute("OccupancyID").Value.Equals(pax.Element("id").Value)).FirstOrDefault()
            //        .Add(new XElement("test", pax.Elements()));
            //List<XElement> abc = new List<XElement>();        
            //var xyz = from respon in mikiRooms.Descendants("roomOption")
            //          select respon;
            //foreach (var room in xyz)
            //{
            //    List<XElement> trial = new List<XElement>();
            //    trial = GetRooms(room, new XElement("test", travayooreq));
            //    foreach (var xele in trial)
            //    {
            //        abc.Add(xele);
            //        totalRate = totalRate + Convert.ToDouble(xele.Attribute("TotalRoomRate").Value);
            //    }
            //}
            XElement cp = CancellationPolicyTag(mikiRooms, travayooreq);
            //XElement retRooms = new XElement("Rooms",
            //                        new XElement("RoomTypes",
            //                    new XAttribute("TotalRate", Convert.ToString(totalRate)),
            //                    new XAttribute("Index", count++), abc));
            //List<XElement> sorting = new List<XElement>();
            //for (int x = 1; x <= abc.Count(); x++)
            //{
            //    sorting.Add(
            //                retRooms.Descendants("Room")
            //                .Where(a => a.Attribute("OccupancyID").Value.Equals(Convert.ToString(x)))
            //                .FirstOrDefault());
            //}
            XElement response = new XElement("Rooms",
                                     new XElement("RoomTypes",
                                       new XAttribute("TotalRate", Convert.ToString(totalRate)),
                                        new XAttribute("Index", "1"), Roomtag));
            response.Descendants("Room").Last().AddAfterSelf(cp);
            return response;
        }
        #endregion
        #endregion
        #endregion
        #region Hotel Booking
        public XElement BookingRequest(XElement Req)
        {
            XElement response = null;
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                Random rnd = new Random();
                #region Request Xml
                XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "11");
                string susername = suppliercred.Descendants("username").FirstOrDefault().Value;
                string spassword = suppliercred.Descendants("password").FirstOrDefault().Value;
                XDocument mikireq = new XDocument(
                                                         new XDeclaration("1.0", "utf-8", "yes"),
                                                         new XElement(soap + "Envelope",
                                                             new XAttribute(XNamespace.Xmlns + "soap", soap),
                                                             new XElement(soap + "Header"),
                                                             new XElement(soap + "Body",
                                                         new XElement("hotelBookingRequest",
                                                             new XAttribute("versionNumber", "7.0"),


                                                         new XElement("requestAuditInfo",
                                                             new XElement("agentCode", susername),
                                                             new XElement("requestPassword", spassword),
                                                             new XElement("requestID", Req.Descendants("TransID").FirstOrDefault().Value),
                                                             new XElement("requestDateTime", requestDateTime)),
                                                         new XElement("booking",
                    //getCurrencyCode(Req.Descendants("CurrencyID").FirstOrDefault().Value),
                                                            new XAttribute("currencyCode", Req.Descendants("CurrencyCode").FirstOrDefault().Value),
                                                             new XAttribute("paxNationality", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                                             new XElement("clientRef",
                                                                 new XAttribute("mustBeUnique", "false"), Req.Descendants("CustomerID").FirstOrDefault().Value),
                                                             new XElement("items",
                                                                 new XElement("item",
                                                                     new XAttribute("itemNumber", "1"),
                                                                     new XElement("immediateConfirmationRequired", "0"),
                                                                     new XElement("productCode", Req.Descendants("HotelID").FirstOrDefault().Value),
                                                                     leadpax(Req.Descendants("PaxInfo").Where(x => x.Descendants("IsLead").FirstOrDefault().Value.ToUpper().Equals("TRUE")).FirstOrDefault()),
                                                                     new XElement("hotel",
                                                                         new XElement("stayPeriod",
                                                                             new XElement("checkinDate", reformatDate(Req.Descendants("FromDate").FirstOrDefault().Value)),
                                                                             new XElement("checkoutDate", reformatDate(Req.Descendants("ToDate").FirstOrDefault().Value))),
                                                                         BookingRooms(Req.Descendants("PassengersDetail").FirstOrDefault())),
                                                                     new XElement("specialRequest", Req.Descendants("SpecialRemarks").FirstOrDefault().Value))))))));
                #endregion
                response = HotelBookingResponse(mikireq, Req);

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "BookingRequest";
                ex1.PageName = "MikiInternal";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransactionID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                XElement confirmedBookingerr = new XElement(
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
                                                         new XElement("HotelBookingResponse",
                                                            new XElement("ErrorTxt", Convert.ToString(ex.Message.ToString())))
                                                       )));
                return confirmedBookingerr;
                #endregion
            }
            #region Response Format
            XElement confirmedBooking = new XElement(
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
                                                       response)));
            #endregion
            return confirmedBooking;
        }
        #region Response
        public XElement HotelBookingResponse(XDocument mikireq, XElement travyobcr)
        {
            XElement bookingconfirmation = null;
            model.Logtype = "Book";
            model.LogtypeID = 5;
            model.CustomerID = Convert.ToInt32(travyobcr.Descendants("CustomerID").FirstOrDefault().Value);
            model.TrackNo = travyobcr.Descendants("TransactionID").FirstOrDefault().Value;
            servresp = mikirep.MikiResponse(mikireq, "hotelBooking", travyobcr.Descendants("CustomerID").FirstOrDefault().Value, model);
            #region Log Save
            try
            {
                #region Removing Namespace
                XElement mikireqnns = removeAllNamespaces(mikireq.Root);
                XElement servrespnns = removeAllNamespaces(servresp.Root);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelBookingResponse";
                ex1.PageName = "MikiInternal";
                ex1.CustomerID = travyobcr.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travyobcr.Descendants("TransactionID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion

            string status = null;
            if (servresp.Descendants("item").Any())
            {
                Boolean condition = servresp.Descendants("item").FirstOrDefault().Attribute("status").Value.Equals("30");
                if (condition)
                {
                    status = "Success";
                    int child = 0, adult = 0;
                    foreach (XElement room in travyobcr.Descendants("RoomPax"))
                    {
                        adult += Convert.ToInt32(room.Element("Adult").Value);
                        child += Convert.ToInt32(room.Element("Child").Value);
                    }
                    #region Response XML
                    var hbr =
                            new XElement("Hotels",
                                          new XElement("HotelID", travyobcr.Descendants("HotelID").FirstOrDefault().Value),
                                          new XElement("HotelName", travyobcr.Descendants("HotelName").FirstOrDefault().Value),
                                          new XElement("FromDate", travyobcr.Descendants("FromDate").FirstOrDefault().Value),
                                          new XElement("ToDate", travyobcr.Descendants("ToDate").FirstOrDefault().Value),
                                          new XElement("AdultPax", adult.ToString()),
                                          new XElement("ChildPax", child.ToString()),
                                          new XElement("TotalPrice", servresp.Descendants("bookingTotalPrice").FirstOrDefault().Element("price").Value),
                                          new XElement("CurrencyID", travyobcr.Descendants("CurrencyID").FirstOrDefault().Value),
                                         new XElement("CurrencyCode", travyobcr.Descendants("CurrencyCode").FirstOrDefault().Value),
                                          new XElement("MarketID"),
                                          new XElement("MarketName"),
                                          new XElement("HotelImgSmall"),
                                          new XElement("HotelImgLarge"),
                                          new XElement("MapLink"),
                                          new XElement("VoucherRemark"),
                                          new XElement("TransID", travyobcr.Descendants("TransID").FirstOrDefault().Value),
                                          new XElement("ConfirmationNumber", servresp.Descendants("booking").FirstOrDefault().Attribute("bookingReference").Value),
                                          new XElement("Status", status),
                                            Booking_Rooms(travyobcr, servresp.Descendants("rooms").FirstOrDefault()));
                    bookingconfirmation = new XElement("HotelBookingResponse", hbr);
                    #endregion
                }
                else
                {
                    switch (servresp.Descendants("item").FirstOrDefault().Attribute("status").Value)
                    {
                        case "25":
                            status = "on request";
                            break;
                        case "70":
                            status = "rejected";
                            break;
                    }
                    #region Status not Confirmed
                    bookingconfirmation.Add(new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Booking Status:  " + status)));
                    return bookingconfirmation;
                    #endregion
                }

            }
            else if (servresp.Descendants("error").Any())
            {
                #region Error Text
                var errors = from err in servresp.Descendants("error")
                             select new XElement("ErrorTxt", err.Element("description").Value);
                if (errors.Count() == 0)
                {
                    bookingconfirmation.Add(new XElement("ErrorTxt", "Server not responding"));
                    return bookingconfirmation;
                }
                bookingconfirmation = new XElement("HotelBookingResponse", errors);
                #endregion
            }
            else
                bookingconfirmation = new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Booking Failed, Please Check logs for detail"));
            return bookingconfirmation;

        }
        #endregion
        #endregion
        #region Booking Cancellation
        public XElement MikiBookingCancellation(XElement req)
        {
            XElement response = null;
            #region Credentials
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            #endregion
            Random rnd = new Random();
            try
            {
                #region Request XML
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "11");
                string susername = suppliercred.Descendants("username").FirstOrDefault().Value;
                string spassword = suppliercred.Descendants("password").FirstOrDefault().Value;
                XDocument mikireq = new XDocument(
                                         new XDeclaration("1.0", "utf-8", "yes"),
                                         new XElement(soap + "Envelope",
                                             new XAttribute(XNamespace.Xmlns + "soap", soap),
                                             new XElement(soap + "Header"),
                                             new XElement(soap + "Body",
                                                 new XElement("cancellationRequest",
                                         new XAttribute("versionNumber", "7.0"),
                                         new XElement("requestAuditInfo",
                                             new XElement("agentCode", susername),
                                             new XElement("requestPassword", spassword),
                                             new XElement("requestID", Convert.ToString(rnd.Next(999999999))),
                                             new XElement("requestDateTime", requestDateTime)),
                                         new XElement("bookingReference", req.Descendants("ConfirmationNumber").FirstOrDefault().Value)))));
                #endregion
                response = CancellationResponse(mikireq, req);

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "MikiBookingCancellation";
                ex1.PageName = "MikiInternal";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            #region Response Format
            XElement cancelledBooking = new XElement(
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
                                                       new XElement(req.Descendants("HotelCancellationRequest").FirstOrDefault()),
                                                      response)));
            #endregion
            return cancelledBooking;
        }

        #region Response
        public XElement CancellationResponse(XDocument mikicancel, XElement travyoreq) // change return type to xelement and generate proper response
        {
            XElement cnx = null;
            model.TrackNo = travyoreq.Descendants("TransID").FirstOrDefault().Value;
            model.CustomerID = Convert.ToInt32(travyoreq.Descendants("CustomerID").FirstOrDefault().Value);
            model.LogtypeID = 6;
            model.Logtype = "Cancel";
            servresp = mikirep.MikiResponse(mikicancel, "cancellation", travyoreq.Descendants("CustomerID").FirstOrDefault().Value, model);
            #region Log Save
            try
            {
                #region Removing Namespace
                XElement mikicancelnns = removeAllNamespaces(mikicancel.Root);
                XElement servrespnns = removeAllNamespaces(servresp.Root);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancellationResponse";
                ex1.PageName = "MikiInternal";
                ex1.CustomerID = travyoreq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travyoreq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion

            string s = null;
            if (servresp.Descendants("status").Any())
            {
                if (servresp.Descendants("status").FirstOrDefault().Value.ToUpper().Equals("CANCELLED"))
                {
                    s = "Success";
                    //roomwise response has been commented out for now
                    #region Response XML roomwise

                    //var cancel = from rooms in servresp.Descendants("roomcancellationchargedetail")
                    //             select
                    //                 new XElement("Room",
                    //                     new XElement("Cancellation",
                    //                         new XElement("Amount", rooms.Element("roomtotalchargeamount").Value),
                    //                         new XElement("Status", s)));
                    //cnx = new XElement("HotelCancellationResponse",
                    //               new XElement("Rooms", cancel));
                    #endregion
                    #region Response XML hotelwise
                    var cancel = new XElement("Room",
                                new XElement("Cancellation",
                                     new XElement("Amount", servresp.Descendants("tourTotalCancellationCharge").FirstOrDefault().Value),
                                     new XElement("Status", s)));
                    #endregion
                    cnx = new XElement("HotelCancellationResponse",
                           new XElement("Rooms", cancel));
                }
            }
            else
            {
                s = "FAILURE";
                #region Error Text
                var errors = from err in servresp.Descendants("error")
                             select new XElement("ErrorTxt", err.Element("description").Value);
                if (errors.Count() == 0)
                {
                    cnx.Add(new XElement("ErrorTxt", "Server Not Responding"));
                    return cnx;
                }
                cnx = new XElement("HotelCancellationResponse", errors);
                #endregion
            }
            return cnx;
        }
        public string status(string s)
        {
            if (s.ToUpper().Equals("CANCELLED"))
                return "SUCCESS";
            else
                return "FAILURE";
        }
        #endregion
        #endregion
        #region Hotel Details
        public XElement hoteldetails(XElement req)
        {
            XElement response = null;
            #region Credentials
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                response = HotelDetailsResponse(req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "hoteldetails";
                ex1.PageName = "MikiInternal";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            #region Response Format
            XElement details = new XElement(
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
                                                        new XElement(req.Descendants("hoteldescRequest").FirstOrDefault()),
                                                       response)));
            #endregion
            return details;
        }
        #region Response
        public XElement HotelDetailsResponse(XElement travayooReq)
        {

            XElement response = null;

            #region Static Data Fetch
            string mikiCity = supplierCityID(travayooReq.Descendants("CityID").FirstOrDefault().Value);
            DataTable details = msd.GetMiki_HotelDetails(mikiCity);
            var result = details.AsEnumerable().Where(dt => dt.Field<string>("HotelID") == travayooReq.Descendants("HotelID").FirstOrDefault().Value);
            int countre = result.Count();
            DataRow[] drow = result.ToArray();
            #endregion
            if (drow.Length > 0)
            {
                DataTable Images = msd.GetMiki_Images(mikiCity);
                DataRow[] imageArray = Images.AsEnumerable().Where(img => img["HotelID"].ToString().Equals(travayooReq.Descendants("HotelID").FirstOrDefault().Value)).ToArray();
                XElement ImagesfromDb = new XElement("Images");
                if (imageArray.Length > 0)
                {
                    for (int i = 0; i < imageArray.Length; i++)
                        ImagesfromDb.Add(new XElement("Image",
                                                  new XAttribute("Path", imageArray[i]["ImageLarge"].ToString()),
                                                      new XAttribute("Caption", string.Empty)));
                }
                DataRow dr = drow[0];
                XElement Facility = Facilities(mikiCity);
                XElement Facil = null;
                if (Facility.Descendants("Facilities").Where(x => x.FirstAttribute.Value.Equals(travayooReq.Descendants("HotelID").FirstOrDefault().Value)).Any())
                    Facil = Facility.Descendants("Facilities").Where(x => x.FirstAttribute.Value.Equals(travayooReq.Descendants("HotelID").FirstOrDefault().Value)).FirstOrDefault();
                else
                    Facil = new XElement("Facilities", new XElement("Facility"));
                if (Facil.HasAttributes)
                    Facil.Attributes().Remove();
                #region Response XML
                var hotels = new XElement("Hotels",
                                  new XElement("Hotel",
                                      new XElement("HotelID", travayooReq.Descendants("HotelID").FirstOrDefault().Value),
                                      new XElement("Description", dr[5]),
                                         ImagesfromDb,
                                      Facil,
                                      new XElement("ContactDetails",
                                          new XElement("Phone", dr[8]),
                                          new XElement("Fax", dr[9])),
                                      new XElement("CheckinTime"),
                                      new XElement("CheckoutTime")
                                      ));
                #endregion
                response = new XElement("hoteldescResponse", hotels);
            }
            return response;
        }
        #endregion
        #endregion
        #region Hotel Details With Cancellation Policy
        public XElement cancelltaionPolicy(XElement Req)
        {
            XElement response = null;
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion

            try
            {
                response = cancellationPolicyResponse(Req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "cancellationPolicy";
                ex1.PageName = "MikiInternal";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            #region Response Format
            XElement cancellationResponse = new XElement(
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
                                                        new XElement(Req.Descendants("hotelcancelpolicyrequest").FirstOrDefault()),
                                                       response)));
            #endregion

            return cancellationResponse;
        }

        #region Response
        public XElement cancellationPolicyResponse(XElement travreq)
        {
            string hotelID = travreq.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value;
            //XElement resp = Roomlist(travreq.Descendants("TransID").FirstOrDefault().Value).Descendants("hotel").
            //               Where(x => x.Descendants("productCode").FirstOrDefault().Value.Equals(hotelID)).FirstOrDefault();
            XElement resp = LogXMLs(travreq.Descendants("TransID").FirstOrDefault().Value, 2, 11).Where(x => x.Descendants("hotel").Descendants("productCode").FirstOrDefault().Value.Equals(hotelID))
                    .FirstOrDefault().Descendants("hotel").FirstOrDefault();

            XElement cancelreponse = new XElement("HotelDetailwithcancellationResponse");
            DateTime starttime = DateTime.Now;

            #region Log Save
            try
            {
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(travreq.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = travreq.Descendants("TransID").FirstOrDefault().Value;
                log.SupplierID = 11;
                log.logrequestXML = travreq.ToString();
                log.logresponseXML = resp.ToString();
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
                ex1.MethodName = "cancellationPolicyResponse";
                ex1.PageName = "MikiInternal";
                ex1.CustomerID = travreq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travreq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            //int availability = Convert.ToInt32(servresp.Descendants("totalAvailable").FirstOrDefault().Value);
            if (resp != null)
            {
                #region ResponseXml


                var cxp = new XElement("Hotels",
                    new XElement("Hotel",
                    new XElement("HotelID", hotelID),
                    new XElement("HotelName", resp.Descendants("hotelName").FirstOrDefault().Value),
                    new XElement("HotelImgSmall", ImageFromdb(hotelID, "small")),
                    new XElement("HotelImgLarge", ImageFromdb(hotelID, "large")),
                    new XElement("MapLink"),
                    new XElement("DMC", "MIKI"),
                    new XElement("Currency", travreq.Descendants("CurrencyName").FirstOrDefault().Value),
                     new XElement("Offers"),
                    CXLRooms(resp.Descendants("roomOptions").FirstOrDefault(), travreq)
                            ));
                cancelreponse.Add(cxp);

                #endregion
            }
            else
            {
                #region Error Text
                //var errors = from error in servresp.Descendants("error")
                //             select new XElement("ErrorText", error.Element("description").Value);
                cancelreponse.Element("HotelDetailwithcancellationResponse").SetValue(
                                                                                        new XElement("ErrorText", "No Result Found in database"));
                #endregion
            }
            return cancelreponse;

        }

        #region Rooms for Cancellation Policy
        private XElement CXLRooms(XElement xElement1, XElement xElement2) // xelement1 takes rooms from miki, while xelement2 takes rooms from b2b
        {
            //XElement getrooms = null;
            //var abc = from x in req.Descendants("roomOption")
            //          select getrooms = GetRooms(req, travayooreq, mikireq);
            //int count = 1;
            //int rc = xElement2.Descendants("RoomPax").Count();
            //string totalrate = getrooms.Element("TotalRate").Value;
            //getrooms.Descendants("TotalRate").Remove();
            //var roomtypes = from resprooms in xElement1.Descendants("roomOption")
            //                join reqrooms in xElement2.Descendants("Rooms").Descendants("Room")
            //                on resprooms.Attribute("roomTypeCode").Value equals reqrooms.Attribute("ID").Value
            //                select

            //                new XElement("RoomTypes",
            //                    new XAttribute("TotalRate", totalRate(resprooms.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value, rc)),
            //                    new XAttribute("Index", count++),
            //                     GetRooms(resprooms, xElement2, amenities),
            //                     getCP(resprooms));
            var retRooms = new XElement("Room",
                                        new XAttribute("ID", ""),
                                        new XAttribute("RoomType", ""),
                                        new XAttribute("MealPlanPrice", ""),
                                        new XAttribute("PerNightRoomRate", ""),
                                        new XAttribute("TotalRoomRate", ""),
                                        new XAttribute("CancellationDate", ""),
                                       CancellationPolicyTag(xElement1, xElement2));
            return new XElement("Rooms", retRooms);
        }
        #endregion
        #endregion
        #endregion



        #region Common Functions

        #region Rooms
        public XElement Rooms(XElement req, XElement travayooreq)
        {
            List<string> MPC = new List<string>();
            int roomcount = travayooreq.Descendants("RoomPax").Count();
            XElement response = new XElement("Rooms");

            #region Room Count = 1
            if (roomcount == 1)
            {
                int count = 1;
                int cnt = 1;
                int rc = travayooreq.Descendants("RoomPax").Count();

                XElement retRooms = retRooms = new XElement("Rooms");

                var occupancy = travayooreq.Descendants("RoomPax");
                foreach (var oc in occupancy)
                {
                    oc.Add(new XElement("id", cnt++));

                    var roomtypes = from rooms in req.Descendants("roomOption")
                                    select
                                    new XElement("RoomTypes",
                                        new XAttribute("TotalRate", rooms.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value),
                                        new XAttribute("Index", count++),
                                         new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", supcurncy), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),
                                    GetRooms1(rooms, new XElement("test", oc)));

                    response.Add(roomtypes);
                }

            }
            #endregion
            #region Room Count = 2
            else if (roomcount == 2)
            {
                int count = 1;
                int cnt = 1;
                int rc = travayooreq.Descendants("RoomPax").Count();

                XElement retRooms = retRooms = new XElement("Rooms");

                #region Get Rooms From Miki's Response
                var occupancy = travayooreq.Descendants("RoomPax");
                foreach (var oc in occupancy)
                {
                    oc.Add(new XElement("id", cnt++));

                    var roomtypes = from rooms in req.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), oc.Descendants("id").FirstOrDefault().Value)
                                    select
                                    GetRooms1(rooms, new XElement("test", oc));

                    retRooms.Add(roomtypes);
                }
                #endregion
                int i = 1;
                #region Rooms Grouping

                if (retRooms.Descendants("Room").Any())
                {
                    var query = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(Convert.ToString(i)));

                    foreach (var rm in query)
                    {
                        var otherRooms = retRooms.Descendants("Room").Where(x => Convert.ToInt32(x.Attribute("OccupancyID").Value) > i);
                        foreach (var orm in otherRooms)
                        {
                            bool RateIdentifierCheck = true;
                            if (rm.Attribute("ID").Value.Equals(orm.Attribute("ID").Value))
                            {
                                if (!rm.Descendants("RequestID").FirstOrDefault().Value.Equals(orm.Descendants("RequestID").FirstOrDefault().Value))
                                    RateIdentifierCheck = false;
                            }
                            if (rm.Attribute("MealPlanCode").Value.Equals(orm.Attribute("MealPlanCode").Value) && RateIdentifierCheck)
                            {
                                #region Total Rate for Grouping
                                double rate1 = Convert.ToDouble(rm.Attribute("TotalRoomRate").Value);
                                double rate2 = Convert.ToDouble(orm.Attribute("TotalRoomRate").Value);
                                double total = rate1 + rate2;
                                #endregion
                                response.Add(new XElement("RoomTypes",
                                                    new XAttribute("TotalRate", Convert.ToString(total)),
                                                     new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", supcurncy), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),
                                                    new XAttribute("Index", count++), rm, orm));
                            }
                        }
                    }

                }
                #endregion
            }
            #endregion
            #region Room Count = 3
            else if (roomcount == 3)
            {

                int count = 1;
                int cnt = 1;
                int rc = travayooreq.Descendants("RoomPax").Count();

                XElement retRooms = retRooms = new XElement("Rooms");
                #region Get Rooms From Miki's Response

                var occupancy = travayooreq.Descendants("RoomPax");
                foreach (var oc in occupancy)
                {
                    oc.Add(new XElement("id", cnt++));

                    var roomtypes = from rooms in req.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), oc.Descendants("id").FirstOrDefault().Value)
                                    select
                                    GetRooms1(rooms, new XElement("test", oc));

                    retRooms.Add(roomtypes);
                }
                #endregion
                int i = 1;
                if (retRooms.Descendants("Room").Any())
                {
                    var query = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(Convert.ToString(i)));

                    foreach (var rm in query)
                    {
                        #region Rooms Grouping
                        var rooms1 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("2"));
                        foreach (var r1 in rooms1)
                        {
                            var otherRooms1 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("3"));
                            foreach (var orm1 in otherRooms1)
                            {
                                List<XElement> mpc1 = new List<XElement>();
                                mpc1.Add(rm);
                                mpc1.Add(r1);
                                mpc1.Add(orm1);
                                List<string> RoomTypeCodes = mpc1.Select(x => x.Attribute("ID").Value).ToList();
                                List<string> repeatedCodes = RoomTypeCodes.GroupBy(n => n).Where(x => x.Count() > 1).Select(y => y.Key).ToList();
                                bool rateIdentifierCheck = true;
                                foreach (string code in repeatedCodes)
                                {
                                    List<string> rateplancodes = mpc1.Where(x => x.Attribute("ID").Value.Equals(code)).Select(y => y.Descendants("RequestID").FirstOrDefault().Value.Split(new char[] { '|' })[1]).Distinct().ToList();
                                    if (rateplancodes.Count > 1)
                                        rateIdentifierCheck = false;
                                }
                                if (rateIdentifierCheck)
                                {
                                    var splitRooms = from rooms in mpc1
                                                     group rooms by rooms.Attribute("MealPlanCode").Value;
                                    foreach (var group in splitRooms)
                                    {
                                        List<XElement> groupedRoomList = new List<XElement>();
                                        double total1 = 0.0;
                                        foreach (var room in group)
                                        {
                                            if (room.HasElements)
                                            {
                                                total1 = total1 + Convert.ToDouble(room.Attribute("TotalRoomRate").Value);
                                                groupedRoomList.Add(room);
                                            }
                                        }
                                        if (groupedRoomList.Count == roomcount)
                                        {
                                            response.Add(new XElement("RoomTypes",
                                                            new XAttribute("TotalRate", Convert.ToString(total1)),
                                                             new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", supcurncy), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),
                                                            new XAttribute("Index", count++), groupedRoomList));
                                        }
                                    }
                                }
                                //if (MPEC(MPC))
                                //{
                                //    #region Total Rate for Grouping
                                //    double rate1 = Convert.ToDouble(rm.Attribute("TotalRoomRate").Value);
                                //    double rate2 = Convert.ToDouble(r1.Attribute("TotalRoomRate").Value);
                                //    double rate3 = Convert.ToDouble(orm1.Attribute("TotalRoomRate").Value);
                                //    double total = rate1 + rate2 + rate3;
                                //    #endregion
                                //    response.Add(new XElement("RoomTypes",
                                //                        new XAttribute("TotalRate", Convert.ToString(total)),
                                //                        new XAttribute("Index", count++), rm, r1, orm1));
                                //}
                            }
                        }
                        #endregion
                    }
                }
            }

            #endregion
            #region Room Count = 4
            else if (roomcount == 4)
            {
                int count = 1;
                int cnt = 1;
                int rc = travayooreq.Descendants("RoomPax").Count();

                XElement retRooms = retRooms = new XElement("Rooms");
                XElement r_retRooms = new XElement("Rooms");
                #region Get Rooms From Miki's Response

                var occupancy = travayooreq.Descendants("RoomPax");
                foreach (var oc in occupancy)
                {
                    XElement rr_retRooms = new XElement("Rooms");
                    oc.Add(new XElement("id", cnt++));

                    var roomtypes = from rooms in req.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), oc.Descendants("id").FirstOrDefault().Value)
                                    select
                                    GetRooms1(rooms, new XElement("test", oc));

                    rr_retRooms.Add(roomtypes);

                    try
                    {
                        var groupsss =
                                        from product in rr_retRooms.Elements("Room")
                                        group product by new { RoomType = (string)product.Attribute("RoomType"), MealPlan = (string)product.Attribute("MealPlanCode") }
                                            into g
                                            select new XElement(roomnode(g.First()));

                        r_retRooms.Add(groupsss);

                    }
                    catch { }
                    //rr_retRooms = null;
                }
                #endregion
                try
                {
                    //XElement docrr = r_retRooms;
                    //retRooms.Add(docrr.Descendants("Room").OrderBy(y => Convert.ToDecimal(y.Attribute("TotalRoomRate").Value)).GroupBy(z => z.Attribute("MealPlanCode").Value).SelectMany(x => x.Take(10)).ToList());
                    retRooms.Add(r_retRooms.Descendants("Room").OrderBy(y => Convert.ToDecimal(y.Attribute("TotalRoomRate").Value)).ToList());
                }
                catch { }
                int i = 1;
                if (retRooms.Descendants("Room").Any())
                {
                    #region Rooms Grouping
                    var query = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(Convert.ToString(i)));

                    foreach (var rm in query)
                    {
                        var rooms1 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("2"));
                        foreach (var r1 in rooms1)
                        {
                            var otherRooms1 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("3"));
                            foreach (var orm1 in otherRooms1)
                            {
                                var otherRooms2 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("4"));

                                foreach (var orm2 in otherRooms2)
                                {
                                    if (count > 200)
                                    {
                                        return response;
                                    }
                                    List<XElement> mpc1 = new List<XElement>();
                                    mpc1.Add(orm2);
                                    mpc1.Add(orm1);
                                    mpc1.Add(r1);
                                    mpc1.Add(rm);
                                    List<string> RoomTypeCodes = mpc1.Select(x => x.Attribute("ID").Value).ToList();
                                    List<string> repeatedCodes = RoomTypeCodes.GroupBy(n => n).Where(x => x.Count() > 1).Select(y => y.Key).ToList();
                                    bool rateIdentifierCheck = true;
                                    foreach (string code in repeatedCodes)
                                    {
                                        List<string> rateplancodes = mpc1.Where(x => x.Attribute("ID").Value.Equals(code)).Select(y => y.Descendants("RequestID").FirstOrDefault().Value.Split(new char[] { '|' })[1]).Distinct().ToList();
                                        if (rateplancodes.Count > 1)
                                            rateIdentifierCheck = false;
                                    }
                                    if (rateIdentifierCheck)
                                    {
                                        var splitRooms = from rooms in mpc1
                                                         group rooms by rooms.Attribute("MealPlanCode").Value;
                                        foreach (var group in splitRooms)
                                        {
                                            List<XElement> groupedRoomList = new List<XElement>();
                                            double total1 = 0.0;
                                            foreach (var room in group)
                                            {
                                                if (room.HasAttributes)
                                                {
                                                    total1 = total1 + Convert.ToDouble(room.Attribute("TotalRoomRate").Value);
                                                    groupedRoomList.Add(room);
                                                }
                                            }
                                            if (groupedRoomList.Count == roomcount)
                                            {
                                                response.Add(
                                                                                            new XElement("RoomTypes",
                                                                                                new XAttribute("TotalRate", Convert.ToString(total1)),
                                                                                                 new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", supcurncy), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),
                                                                                                new XAttribute("Index", count++), groupedRoomList));
                                            }
                                        }
                                    }
                                    //if (MPEC(MPC))
                                    //{
                                    //    #region Total Rate for Grouping
                                    //    double rate1 = Convert.ToDouble(rm.Attribute("TotalRoomRate").Value);
                                    //    double rate2 = Convert.ToDouble(r1.Attribute("TotalRoomRate").Value);
                                    //    double rate3 = Convert.ToDouble(orm1.Attribute("TotalRoomRate").Value);
                                    //    double rate4 = Convert.ToDouble(orm2.Attribute("TotalRoomRate").Value);
                                    //    double total = rate1 + rate2 + rate3 + rate4;
                                    //    #endregion
                                    //    response.Add(new XElement("RoomTypes",
                                    //                        new XAttribute("TotalRate", Convert.ToString(total)),
                                    //                        new XAttribute("Index", count++), rm, r1, orm1, orm2));
                                    //}
                                }
                            }
                        }

                    }
                    #endregion
                }


            }

            #endregion
            #region Room Count = 5
            else if (roomcount == 5)
            {
                int count = 1;
                int cnt = 1;
                int rc = travayooreq.Descendants("RoomPax").Count();

                XElement retRooms = new XElement("Rooms");
                XElement r_retRooms = new XElement("Rooms");

                //XElement rr_retRooms = new XElement("Rooms");

                var occupancy = travayooreq.Descendants("RoomPax");
                foreach (var oc in occupancy)
                {
                    XElement rr_retRooms = new XElement("Rooms");
                    #region Get Rooms From Miki's Response
                    oc.Add(new XElement("id", cnt++));

                    var roomtypes = from rooms in req.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), oc.Descendants("id").FirstOrDefault().Value)
                                    select
                                    GetRooms1(rooms, new XElement("test", oc));



                    rr_retRooms.Add(roomtypes);

                    try
                    {
                        var groupsss =
                                        from product in rr_retRooms.Elements("Room")
                                        group product by new { RoomType = (string)product.Attribute("RoomType"), MealPlan = (string)product.Attribute("MealPlanCode") }
                                            into g
                                            select new XElement(roomnode(g.First()));

                        r_retRooms.Add(groupsss);

                    }
                    catch { }
                    //rr_retRooms = null;

                    #endregion
                }
                try
                {
                    //XElement docrr = r_retRooms;
                    //retRooms.Add(docrr.Descendants("Room").OrderBy(y => Convert.ToDecimal(y.Attribute("TotalRoomRate").Value)).GroupBy(z => z.Attribute("MealPlanCode").Value).SelectMany(x => x.Take(10)).ToList());
                    retRooms.Add(r_retRooms.Descendants("Room").OrderBy(y => Convert.ToDecimal(y.Attribute("TotalRoomRate").Value)).ToList());
                }
                catch { }
                int i = 1;
                if (retRooms.Descendants("Room").Any())
                {
                    #region Room Grouping

                    var query = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals(Convert.ToString(i)));

                    foreach (var rm in query)
                    {
                        var rooms1 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("2"));
                        foreach (var r1 in rooms1)
                        {
                            var otherRooms1 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("3"));
                            foreach (var orm1 in otherRooms1)
                            {
                                var otherRooms2 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("4"));

                                foreach (var orm2 in otherRooms2)
                                {
                                    var otherRooms3 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("5"));
                                    foreach (var orm3 in otherRooms3)
                                    {
                                        if (count > 200)
                                        {
                                            return response;
                                        }
                                        List<XElement> mpc1 = new List<XElement>();
                                        mpc1.Add(orm3);
                                        mpc1.Add(orm2);
                                        mpc1.Add(orm1);
                                        mpc1.Add(r1);
                                        mpc1.Add(rm);
                                        List<string> RoomTypeCodes = mpc1.Select(x => x.Attribute("ID").Value).ToList();
                                        List<string> repeatedCodes = RoomTypeCodes.GroupBy(n => n).Where(x => x.Count() > 1).Select(y => y.Key).ToList();
                                        bool rateIdentifierCheck = true;
                                        foreach (string code in repeatedCodes)
                                        {
                                            List<string> rateplancodes = mpc1.Where(x => x.Attribute("ID").Value.Equals(code)).Select(y => y.Descendants("RequestID").FirstOrDefault().Value.Split(new char[] { '|' })[1]).Distinct().ToList();
                                            if (rateplancodes.Count > 1)
                                                rateIdentifierCheck = false;
                                        }
                                        if (rateIdentifierCheck)
                                        {
                                            var splitrooms = from rooms in mpc1
                                                             group rooms by rooms.Attribute("MealPlanCode").Value;
                                            foreach (var group in splitrooms)
                                            {
                                                List<XElement> groupedRoomList = new List<XElement>();
                                                double total1 = 0.0;
                                                foreach (var room in group)
                                                {
                                                    if (room.HasElements)
                                                    {
                                                        total1 = total1 + Convert.ToDouble(room.Attribute("TotalRoomRate").Value);
                                                        groupedRoomList.Add(room);
                                                    }
                                                }
                                                if (groupedRoomList.Count == roomcount)
                                                {
                                                    response.Add(new XElement("RoomTypes",
                                                                    new XAttribute("TotalRate", Convert.ToString(total1)),
                                                                     new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", supcurncy), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),
                                                                    new XAttribute("Index", count++), groupedRoomList));
                                                }
                                            }
                                        }
                                        //if (MPEC(MPC))
                                        //{
                                        //    double rate1 = Convert.ToDouble(rm.Attribute("TotalRoomRate").Value);
                                        //    double rate2 = Convert.ToDouble(r1.Attribute("TotalRoomRate").Value);
                                        //    double rate3 = Convert.ToDouble(orm1.Attribute("TotalRoomRate").Value);
                                        //    double rate4 = Convert.ToDouble(orm2.Attribute("TotalRoomRate").Value);
                                        //    double rate5 = Convert.ToDouble(orm3.Attribute("TotalRoomRate").Value);
                                        //    double total = rate1 + rate2 + rate3 + rate4 + rate5;
                                        //    response.Add(new XElement("RoomTypes",
                                        //                        new XAttribute("TotalRate", Convert.ToString(total)),
                                        //                        new XAttribute("Index", count++), rm, r1, orm1, orm2, orm3));
                                        //}
                                    }
                                }
                            }
                        }

                    }
                    #endregion
                }


            }

            #endregion
            return response;
        }
        private XElement roomnode(XElement room)
        {
            return room;
        }
        public string totalRate(string roomRate, int paxNumber)
        {
            string tr = null;
            double rrate = Convert.ToDouble(roomRate);
            double total = rrate * paxNumber;
            tr = Convert.ToString(total);
            return tr;
        }
        public List<XElement> GetRooms(XElement rooms, XElement travayooreq)
        {
            //string adults = Convert.ToString(travayooreq.Descendants("Adult").Count());

            //string childs = Convert.ToString(travayooreq.Descendants("Child").Count());

            int index = 1;
            int cnt = 1;
            //XElement response = new XElement("Rooms");

            //int roomCount = travayooreq.Descendants("RoomPax").Count();

            // for (int i = 1; i <= roomCount; i++)
            // {
            List<XElement> roomlist = new List<XElement>();


            var forrooms = from rn in rooms.Descendants("roomNumber")
                           select rn.Value;

            foreach (var pax in travayooreq.Descendants("Room"))
            {

                try
                {
                    if (forrooms.Contains(pax.Attribute("OccupancyID").Value))
                    {
                        roomlist.Add(
                                     new XElement("Room",
                                      new XAttribute("ID", rooms.Attributes("roomTypeCode").FirstOrDefault().Value),
                                      new XAttribute("SuppliersID", supplierID),
                                      new XAttribute("RoomSeq", ""),
                                      new XAttribute("SessionID", pax.Attribute("SessionID").Value),
                                      new XAttribute("RoomType", rooms.Descendants("roomDescription").FirstOrDefault().Value),
                                      new XAttribute("OccupancyID", pax.Attribute("OccupancyID").Value),
                                      new XAttribute("OccupancyName", rooms.Descendants("mealBasis").FirstOrDefault().Descendants("description").FirstOrDefault().Value),
                                      new XAttribute("MealPlanID", rooms.Descendants("includedMeal").FirstOrDefault().Element("mealID").Value),
                                      mealplanname(rooms.Descendants("mealBasis").FirstOrDefault().Attribute("mealBasisCode").Value),
                                      new XAttribute("MealPlanCode", rooms.Descendants("mealBasis").FirstOrDefault().Attribute("mealBasisCode").Value.Equals("RB") ? "BB" : rooms.Descendants("mealBasis").FirstOrDefault().Attribute("mealBasisCode").Value),
                                      new XAttribute("MealPlanPrice", ""),
                                      new XAttribute("PerNightRoomRate", rooms.Descendants("dailyPrice").FirstOrDefault().Element("price").Value),
                                      new XAttribute("TotalRoomRate", rooms.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value),
                                      new XAttribute("CancellationDate", ""),
                                      new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", "true"),
                                      new XElement("RequestID", rooms.Descendants("rateIdentifier").FirstOrDefault().Value),
                                      new XElement("Offers"),
                                      new XElement("PromotionList",
                                      new XElement("Promotions")),
                                      new XElement("CancellationPolicy"),
                                      new XElement("Amenities",
                                          new XElement("Amenity")),
                                      new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                      new XElement("Supplements"),
                                      pb(rooms),
                                      new XElement("AdultNum", pax.Descendants("Adult").FirstOrDefault().Value),
                                      new XElement("ChildNum", pax.Descendants("Child").FirstOrDefault().Value)));
                    }
                }
                catch { }
            }
            //response.Add(getrooms);

            //double rate = Convert.ToDouble(rooms.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value);
            //totalrate = totalrate + rate;

            //}
            // response.AddBeforeSelf(new XElement("TotalRate", Convert.ToString(totalrate)));

            return roomlist;
        }
        public List<XElement> GetRooms1(XElement rooms, XElement travayooreq)
        {
            //string adults = Convert.ToString(travayooreq.Descendants("Adult").Count());

            //string childs = Convert.ToString(travayooreq.Descendants("Child").Count());
            int count = 1;
            int cnt = 1;
            //XElement response = new XElement("Rooms");

            //int roomCount = travayooreq.Descendants("RoomPax").Count();

            // for (int i = 1; i <= roomCount; i++)
            // {
            List<XElement> roomlist = new List<XElement>();
            var paxes = travayooreq.Descendants("RoomPax");
            var forrooms = from rn in rooms.Descendants("roomNumber")
                           select rn.Value;
            foreach (var pax in paxes)
            {
                try
                {
                    bool mealDesc = true;
                    if (!rooms.Descendants("mealBasis").FirstOrDefault().HasElements && !rooms.Descendants("mealBasis").FirstOrDefault().Descendants("description").Any())
                        mealDesc = false;
                    if (forrooms.Contains(pax.Descendants("id").FirstOrDefault().Value))
                    {
                        roomlist.Add(
                                     new XElement("Room",
                                      new XAttribute("ID", rooms.Attributes("roomTypeCode").FirstOrDefault().Value),
                                      new XAttribute("SuppliersID", supplierID),
                                      new XAttribute("RoomSeq", travayooreq.Descendants("id").Any() ? travayooreq.Descendants("id").FirstOrDefault().Value : ""),
                                      new XAttribute("SessionID", rooms.Descendants("rateIdentifier").FirstOrDefault().Value),
                                      new XAttribute("RoomType", rooms.Descendants("roomDescription").FirstOrDefault().Value),
                                      new XAttribute("OccupancyID", travayooreq.Descendants("id").Any() ? travayooreq.Descendants("id").FirstOrDefault().Value : ""),
                                      new XAttribute("OccupancyName", mealDesc ? rooms.Descendants("mealBasis").FirstOrDefault().Descendants("description").FirstOrDefault().Value : ""),
                                      new XAttribute("MealPlanID", ""),
                                      mealplanname(rooms.Descendants("mealBasis").FirstOrDefault().Attribute("mealBasisCode").Value),
                                      new XAttribute("MealPlanCode", rooms.Descendants("mealBasis").FirstOrDefault().Attribute("mealBasisCode").Value.Equals("RB") ? "BB" : rooms.Descendants("mealBasis").FirstOrDefault().Attribute("mealBasisCode").Value),
                                      new XAttribute("MealPlanPrice", ""),
                                      new XAttribute("PerNightRoomRate", rooms.Descendants("dailyPrice").FirstOrDefault().Element("price").Value),
                                      new XAttribute("TotalRoomRate", rooms.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value),
                                      new XAttribute("CancellationDate", ""),
                                      new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", "true"),
                                      new XElement("RequestID", rooms.Descendants("rateIdentifier").FirstOrDefault().Value),
                                      new XElement("Offers"),
                                      new XElement("PromotionList",
                                      rooms.Descendants("specialOffers").Any() ? promotions(rooms.Descendants("specialOffers").FirstOrDefault()) : new List<XElement> { new XElement("Promotions") }),
                                      new XElement("CancellationPolicy"),
                                      new XElement("Amenities",
                                          new XElement("Amenity")),
                                      new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                      new XElement("Supplements"),
                                      pb(rooms),
                                      new XElement("AdultNum", pax.Element("Adult").Value),
                                      new XElement("ChildNum", pax.Element("Child").Value)));
                    }
                }
                catch { }
            }
            //response.Add(getrooms);

            //double rate = Convert.ToDouble(rooms.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value);
            //totalrate = totalrate + rate;
            //}
            // response.AddBeforeSelf(new XElement("TotalRate", Convert.ToString(totalrate)));

            return roomlist;
        }
        public bool occupancyCondition(string RoomID, XElement pax)
        {
            int adults = Convert.ToInt32(pax.Descendants("Adult").FirstOrDefault().Value);
            int children = Convert.ToInt32(pax.Descendants("Child").FirstOrDefault().Value);
            int total = adults + children;
            #region Room Occupancy Cases

            //switch (RoomID)
            //{
            //    case "00002":
            //        if (adults == 1 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "00001":
            //        if (adults > 0 && adults <= 2 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "00003":
            //        if (adults > 0 && adults <= 3 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "00004":
            //        if (adults == 1 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "01001":
            //        if (adults > 0 && adults <= 2 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "01002":
            //        if (adults == 1 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "02095":
            //        if (total <= 4 && adults > 0 && adults <= 4 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "04136":
            //        if (total <= 4 && adults > 0 && adults <= 4 && children <= 1)
            //            return true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "04570":
            //        if (total <= 2 && adults > 0 && adults <= 2 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "05662":
            //        if (adults == 1 && children == 0)
            //            return true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "08685":
            //        if (total <= 2 && adults > 0 && adults <= 2 && children <= 1)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "30001":
            //        if (total == 3 && adults == 2 && children == 1)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "30003":
            //        if (total == 4 && adults == 3 && children == 1)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "31001":
            //        if (total <= 3 && adults == 2 && children == 1)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "50002":
            //        if (adults == 1 && children == 1)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "84851":
            //        if (adults > 0 && adults < 3 && children < 1)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "88018":
            //        if (adults > 0 && adults < 3 && children < 1)
            //            return true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "88027":
            //        if (total < 3 && adults > 0 && adults < 3 && children <= 1)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "91558":
            //        if (total < 9 && adults > 0 && adults < 6 && children <= 3)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "91691":
            //        if (total <= 9 && adults > 0 && adults < 7 && children <= 3)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "91781":
            //        if (adults < 8 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "91970":
            //        if (total < 8 && adults < 7 && children <= 4)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "92113":
            //        if (adults < 9 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "92264":
            //        if (total < 9 && adults < 9 && children <= 4)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "92452":
            //        if (adults < 10 && children == 0)
            //            return true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "92818":
            //        if (total < 10 && adults < 10 && children <= 5)
            //            paxresponse = true;
            //        break;
            //    case "93035":
            //        if (adults == 1 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "93036":
            //        if (adults == 1 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "93037":
            //        if (adults == 1 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "93038":
            //        if (adults == 1 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "93039":
            //        if (adults < 5 && children == 0)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    case "93040":
            //        if (total < 6 && adults < 5 && children == 1)
            //            paxresponse = true;
            //        else
            //            paxresponse = false;
            //        break;
            //    default:
            //        paxresponse = false;
            //        break;
            #endregion
            #region Room occupancy Cases without child condition

            switch (RoomID)
            {
                case "00002":
                    if (adults == 1)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "00001":
                    if (adults > 0 && adults <= 2)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "00003":
                    if (adults > 0 && adults <= 3)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "00004":
                    if (adults == 1)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "01001":
                    if (adults > 0 && adults <= 2)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "01002":
                    if (adults == 1)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "02095":
                    if (total <= 4 && adults > 0 && adults <= 4)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "04136":
                    if (total <= 4 && adults > 0 && adults <= 4 && children <= 1)
                        return true;
                    else
                        paxresponse = false;
                    break;
                case "04570":
                    if (total <= 2 && adults > 0 && adults <= 2)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "05662":
                    if (adults == 1 && children == 0)
                        return true;
                    else
                        paxresponse = false;
                    break;
                case "08685":
                    if (total <= 2 && adults > 0 && adults <= 2 && children <= 1)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "30001":
                    if (total == 3 && adults == 2 && children == 1)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "30003":
                    if (total == 4 && adults == 3 && children == 1)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "31001":
                    if (total <= 3 && adults == 2 && children == 1)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "50002":
                    if (adults == 1 && children == 1)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "84851":
                    if (adults > 0 && adults < 3)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "88018":
                    if (adults > 0 && adults < 3)
                        return true;
                    else
                        paxresponse = false;
                    break;
                case "88027":
                    if (total < 3 && adults > 0 && adults < 3 && children <= 1)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "91558":
                    if (total < 9 && adults > 0 && adults < 6 && children <= 3)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "91691":
                    if (total <= 9 && adults > 0 && adults < 7 && children <= 3)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "91781":
                    if (adults < 8 && children == 0)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "91970":
                    if (total < 8 && adults < 7 && children <= 4)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "92113":
                    if (adults < 9 && children == 0)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "92264":
                    if (total < 9 && adults < 9 && children <= 4)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "92452":
                    if (adults < 10 && children == 0)
                        return true;
                    else
                        paxresponse = false;
                    break;
                case "92818":
                    if (total < 10 && adults < 10 && children <= 5)
                        paxresponse = true;
                    break;
                case "93035":
                    if (adults == 1 && children == 0)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "93036":
                    if (adults == 1 && children == 0)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "93037":
                    if (adults == 1 && children == 0)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "93038":
                    if (adults == 1 && children == 0)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "93039":
                    if (adults < 5 && children == 0)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                case "93040":
                    if (total < 6 && adults < 5 && children == 1)
                        paxresponse = true;
                    else
                        paxresponse = false;
                    break;
                default:
                    paxresponse = false;
                    break;

            }
            #endregion
            return paxresponse;
        }

        #region Price Breakup
        private XElement pb(XElement pricebreakup)
        {
            int counter = 1;
            var breakup = from pbs in pricebreakup.Descendants("dailyPrice")
                          select new XElement("Price",
                    new XAttribute("Night", Convert.ToString(counter++)),
                    new XAttribute("PriceValue", pbs.Element("price").Value));
            XElement pricebreakups = new XElement("PriceBreakups", breakup);
            return pricebreakups;

        }
        #endregion
        #endregion
        #region Rooms for Pre Booking
        private XElement Prebook_Rooms(XDocument servresp, XElement travayooreq, XElement mikireq)
        {
            int roomCount = 1;
            var prebookingRooms = from resprooms in servresp.Descendants("roomOptions").Elements("roomOption")
                                  join reqrooms in travayooreq.Descendants("Rooms").Descendants("Room")
                                  on resprooms.Attribute("roomTypeCode").Value equals reqrooms.Attribute("ID").Value
                                  select
                                  new XElement("RoomTypes",
                                              new XAttribute("Index", roomCount++),
                                              new XAttribute("TotalRate", resprooms.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value),
                                              Rooms(new XElement("preBook", resprooms), travayooreq));
            XElement prebookRoom = new XElement("Rooms", prebookingRooms);

            return prebookRoom;
        }
        #endregion
        #region Rooms for Booking Response
        public XElement Booking_Rooms(XElement travyobcr, XElement mikirooms)
        {

            var RoomsGettingbooked = from rooms in travyobcr.Descendants("PassengersDetail").Elements("Room")
                                     join resprooms in mikirooms.Elements("room")
                                     on rooms.Attribute("RoomTypeID").Value equals resprooms.Attribute("roomTypeCode").Value
                                     where rooms.Attribute("OccupancyID").Value.Equals(resprooms.Attribute("roomNo").Value)
                                     select new XElement("GuestDetails",
                                                new XElement("Room",
                                                    new XAttribute("ID", rooms.Attribute("RoomTypeID").Value),
                                                    new XAttribute("RoomType", rooms.Attribute("RoomType").Value),
                                                    new XAttribute("ServiceID", ""),
                                                    new XAttribute("MealPlanID", rooms.Attribute("MealPlanID").Value),
                                                    new XAttribute("MealPlanName", ""),
                                                    new XAttribute("MealPlanCode", ""),
                                                    new XAttribute("MealPlanPrice", rooms.Attribute("MealPlanPrice").Value),
                                                    new XAttribute("PerNightRoomRate", resprooms.Descendants("dailyPrice").FirstOrDefault().Element("price").Value),
                                                    new XAttribute("RoomStatus", resprooms.Descendants("availabilityStatus").FirstOrDefault().Value.Equals("1") ? "true" : "false"),
                                                    new XAttribute("TotalRoomRate", resprooms.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value),
                                                    new XElement("RoomGuest", rooms.Descendants("PaxInfo").FirstOrDefault()),
                                                    new XElement("Supplements", rooms.Descendants("Supplement"))));
            XElement bookedrooms = new XElement("PassengerDetail", RoomsGettingbooked);
            return bookedrooms;
        }
        #endregion
        #region Get CancellationPolicy tag
        public XElement getCP(XElement roominput)
        {
            XElement RoomInput = new XElement("roomOptions", roominput);   // takes room of API response type
            XElement helper = new XElement("CancellationPolicies");
            var rooms = from resprooms in RoomInput.Elements("roomOption")
                        select resprooms;

            foreach (var room in rooms)
            {
                string tp = room.Descendants("roomTotalPrice").Descendants("price").FirstOrDefault().Value;
                string fnc = room.Descendants("date").FirstOrDefault().Descendants("price").FirstOrDefault().Value;

                var eachpolicy = from pol in room.Descendants("cancellationPolicies").Elements("cancellationPolicy")
                                 select
                                            new XElement("CancellationPolicy",
                                                           new XAttribute("LastCancellationDate", Convert.ToDateTime(pol.Element("appliesFrom").Value).Date.ToString("dd/MM/yyyy")),
                                                           new XAttribute("ApplicableAmount", amount(pol, tp, fnc)),
                                                           new XAttribute("NoShowPolicy", "0"));
                helper.Add(eachpolicy);
            }
            return helper;
        }
        #region Cancellation Policy amount
        private string amount(XElement abc, string tp, string fnc)
        {
            double totalPrice = Convert.ToDouble(tp);
            double firstNightCharge = Convert.ToDouble(fnc);
            double percentage = Convert.ToDouble(abc.Descendants("percentage").FirstOrDefault().Value);
            double calculate = 0;
            if (abc.Descendants("amount").Any())
                return abc.Descendants("amount").FirstOrDefault().Value;
            else
            {
                if (Convert.ToBoolean(abc.Descendants("fullStay").FirstOrDefault().Value))
                    calculate = (percentage / 100) * totalPrice;
                else
                    calculate = (percentage / 100) * firstNightCharge;
            }
            return Convert.ToString(calculate);
        }
        #endregion
        #endregion
        #region Get Xmls From Log
        public List<XElement> LogXMLs(string trackID, int logtypeID, int SupplierID)
        {
            List<XElement> response = new List<XElement>();
            DataTable LogTable = new DataTable();
            LogTable = msd.GetLog(trackID, logtypeID, SupplierID);
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
        #region Minimum Rate provided by a hotel
        public string MinRate(XElement rooms, XElement Req)
        {
            double minimumPrice = MinPriceGrouping(rooms, Req);
            string rate = null;
            //var collect = from price in Roomlist.Descendants("RoomTypes")
            //              select price.Attribute("TotalRate").Value;
            //double minprice = double.MaxValue;
            //foreach (var price in collect)
            //{

            //    double check = Convert.ToDouble(price);
            //    if (check <= minprice)
            //    {
            //        minprice = check;
            //    }

            //}
            if (minimumPrice == double.MaxValue)
                rate = "0.0";
            else
                rate = Convert.ToString(minimumPrice);
            return rate;
        }
        #region Min Price Calculation
        public double MinPriceGrouping(XElement MikiRooms, XElement travReq)
        {
            int count = 1;
            int groupcnt = 1;

            double response = 0.0;

            XElement retRooms = new XElement("Rooms");
            int roomcount = travReq.Descendants("RoomPax").Count();
            #region Room Count = 1
            if (roomcount == 1)
            {
                var occupancy = new List<XElement>(travReq.Descendants("RoomPax")).ToList();
                foreach (var pax in occupancy)
                {
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    var roomtypes = from rooms in MikiRooms.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), pax.Descendants("id").FirstOrDefault().Value)
                                    select
                                    RoomTag1(rooms, pax);
                    retRooms.Add(roomtypes);
                }
                #region RoomGrouping
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("1"));
                double minimum = double.MaxValue;
                foreach (var rm1 in query1)
                {
                    double check = Convert.ToDouble(rm1.Attribute("RoomPrice").Value);
                    if (check < minimum)
                        minimum = check;
                }
                #endregion
                response = minimum;
            }
            #endregion
            #region Room Count = 2
            else if (roomcount == 2)
            {
                var occupancy = new List<XElement>(travReq.Descendants("RoomPax")).ToList();
                foreach (var pax in occupancy)
                {
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    var roomtypes = from rooms in MikiRooms.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), pax.Descendants("id").FirstOrDefault().Value)
                                    select
                                    RoomTag1(rooms, pax);
                    retRooms.Add(roomtypes);
                }
                #region RoomGrouping
                double minimum = double.MaxValue;
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("1"));
                foreach (var rm1 in query1)
                {
                    var query2 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("2"));
                    foreach (var rm2 in query2)
                    {
                        List<XElement> mpc = new List<XElement>();

                        mpc.Add(rm1);
                        mpc.Add(rm2);



                        var splitrooms = from room in mpc
                                         group room by room.Attribute("MealPlanCode").Value;
                        foreach (var group in splitrooms)
                        {
                            List<XElement> groupedRoomList = new List<XElement>();
                            double total = 0.0;
                            foreach (var room in group)
                            {
                                if (room.HasAttributes)
                                {
                                    total = total + Convert.ToDouble(room.Attribute("RoomPrice").Value);
                                    groupedRoomList.Add(room);
                                }
                            }
                            if (groupedRoomList.Count == roomcount)
                            {
                                if (total < minimum)
                                    minimum = total;
                            }
                        }



                    }
                }
                #endregion
                response = minimum;
            }
            #endregion
            #region Room Count = 3
            else if (roomcount == 3)
            {
                var occupancy = new List<XElement>(travReq.Descendants("RoomPax")).ToList();
                foreach (var pax in occupancy)
                {

                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    var roomtypes = from rooms in MikiRooms.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), pax.Descendants("id").FirstOrDefault().Value)
                                    select
                                    RoomTag1(rooms, pax);
                    retRooms.Add(roomtypes);
                }
                #region RoomGrouping
                double minimum = double.MaxValue;
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("1"));
                foreach (var rm1 in query1)
                {
                    var query2 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("2"));
                    foreach (var rm2 in query2)
                    {
                        var query3 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("3"));
                        foreach (var rm3 in query3)
                        {
                            List<XElement> mpc = new List<XElement>();

                            mpc.Add(rm1);
                            mpc.Add(rm2);
                            mpc.Add(rm3);

                            var splitrooms = from room in mpc
                                             group room by room.Attribute("MealPlanCode").Value;
                            foreach (var group in splitrooms)
                            {
                                List<XElement> groupedRoomList = new List<XElement>();
                                double total = 0.0;
                                foreach (var room in group)
                                {

                                    if (room.HasAttributes)
                                    {
                                        total = total + Convert.ToDouble(room.Attribute("RoomPrice").Value);
                                        groupedRoomList.Add(room);
                                    }
                                }
                                if (groupedRoomList.Count == roomcount)
                                {
                                    if (total < minimum)
                                        minimum = total;
                                }
                            }

                        }
                    }
                }
                #endregion
                response = minimum;
            }
            #endregion
            #region Room Count = 4
            else if (roomcount == 4)
            {
                var occupancy = new List<XElement>(travReq.Descendants("RoomPax")).ToList();
                foreach (var pax in occupancy)
                {
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    var roomtypes = from rooms in MikiRooms.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), pax.Descendants("id").FirstOrDefault().Value)
                                    select
                                    RoomTag1(rooms, pax);
                    retRooms.Add(roomtypes);
                }
                #region RoomGrouping
                double minimum = double.MaxValue;
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("1"));
                foreach (var rm1 in query1)
                {
                    var query2 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("2"));
                    foreach (var rm2 in query2)
                    {
                        var query3 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("3"));
                        foreach (var rm3 in query3)
                        {
                            var query4 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("4"));
                            foreach (var rm4 in query4)
                            {
                                List<XElement> mpc = new List<XElement>();
                                mpc.Add(rm1);
                                mpc.Add(rm2);
                                mpc.Add(rm3);
                                mpc.Add(rm4);
                                var splitrooms = from room in mpc
                                                 group room by room.Attribute("MealPlanCode").Value;
                                foreach (var group in splitrooms)
                                {
                                    List<XElement> groupedRoomList = new List<XElement>();
                                    double total = 0.0;
                                    foreach (var room in group)
                                    {
                                        if (room.HasAttributes)
                                        {
                                            total = total + Convert.ToDouble(room.Attribute("RoomPrice").Value);
                                            groupedRoomList.Add(room);
                                        }
                                    }
                                    if (groupedRoomList.Count == roomcount)
                                    {
                                        if (total < minimum)
                                            minimum = total;
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                response = minimum;
            }
            #endregion
            #region Room Count = 5
            else if (roomcount == 5)
            {
                var occupancy = new List<XElement>(travReq.Descendants("RoomPax")).ToList();
                foreach (var pax in occupancy)
                {
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    var roomtypes = from rooms in MikiRooms.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), pax.Descendants("id").FirstOrDefault().Value)
                                    select
                                    RoomTag1(rooms, pax);
                    retRooms.Add(roomtypes);
                }
                #region RoomGrouping
                double minimum = double.MaxValue;
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("1"));
                foreach (var rm1 in query1)
                {
                    var query2 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("2"));
                    foreach (var rm2 in query2)
                    {
                        var query3 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("3"));
                        foreach (var rm3 in query3)
                        {
                            var query4 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("4"));
                            foreach (var rm4 in query4)
                            {
                                var query5 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("5"));
                                foreach (var rm5 in query5)
                                {
                                    List<XElement> mpc = new List<XElement>();
                                    mpc.Add(rm1);
                                    mpc.Add(rm2);
                                    mpc.Add(rm3);
                                    mpc.Add(rm4);
                                    mpc.Add(rm5);
                                    var splitrooms = from room in mpc
                                                     group room by room.Attribute("MealPlanCode").Value;
                                    foreach (var group in splitrooms)
                                    {
                                        List<XElement> groupedRoomList = new List<XElement>();
                                        double total = 0.0;
                                        foreach (var room in group)
                                        {
                                            if (room.HasAttributes)
                                            {
                                                total = total + Convert.ToDouble(room.Attribute("RoomPrice").Value);
                                                groupedRoomList.Add(room);
                                            }
                                        }
                                        if (groupedRoomList.Count == roomcount)
                                        {
                                            if (total < minimum)
                                                minimum = total;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                response = minimum;
            }
            #endregion

            return response;
        }
        public List<XElement> RoomTag1(XElement MikiRoom, XElement occupancy)
        {
            List<XElement> roomlist = new List<XElement>();

            var forrooms = from rn in MikiRoom.Descendants("roomNumber")
                           select rn.Value;
            if (forrooms.Contains(occupancy.Descendants("id").FirstOrDefault().Value))
            {

                try
                {
                    roomlist.Add(new XElement("Room",
                                      new XAttribute("Occupancy", occupancy.Descendants("id").FirstOrDefault().Value),
                                      new XAttribute("MealPlanCode", MikiRoom.Descendants("mealBasis").FirstOrDefault().Attribute("mealBasisCode").Value),
                                      new XAttribute("RoomPrice", MikiRoom.Descendants("roomTotalPrice").FirstOrDefault().Descendants("price").FirstOrDefault().Value)));
                }
                catch { }
            }
            return roomlist;
        }
        #endregion
        #endregion
        #region Pax Details for Booking Request
        public XElement leadpax(XElement guests)
        {


            XElement response = new XElement("leadPaxName",

                                   new XElement("firstName", guests.Element("FirstName").Value),
                                   new XElement("lastName", guests.Element("LastName").Value));

            return response;
        }
        #endregion
        #region Rooms For Booking Request
        public XElement BookingRooms(XElement req)
        {
            int numberofrooms = req.Descendants("Room").Count();
            int count = 1;
            var rooms = from room in req.Descendants("Room")
                        select
                        new XElement("room",
                            new XAttribute("roomTypeCode", room.Attribute("RoomTypeID").Value),
                            new XAttribute("roomNo", room.Attribute("OccupancyID").Value),
                            new XElement("rateIdentifier", room.Descendants("RequestID").FirstOrDefault().Value),
                            new XElement("roomTotalPrice", room.Attribute("TotalRoomRate").Value),
                            guest(room));
            XElement x = new XElement("rooms", rooms);
            return x;
        }
        #region Guests for booking rooms
        private XElement guest(XElement abc)
        {
            string mname = null;
            XElement guests = new XElement("guests");
            var pax = from paxes in abc.Elements("PaxInfo")
                      select
                      paxes;
            foreach (var p in pax)
            {
                if (!p.Element("FirstName").Value.Equals(string.Empty))
                {
                    if (p.Descendants("MiddleName").Any())
                        mname = p.Descendants("MiddleName").FirstOrDefault().Value;
                    var people = new XElement("guest",
                       new XElement("type", type(p.Descendants("GuestType").FirstOrDefault().Value)),
                       p.Element("Age").Value.Equals("0") ? null : new XElement("age", p.Element("Age").Value),
                       new XElement("paxName",

                           new XElement("firstName", p.Descendants("FirstName").FirstOrDefault().Value),
                           new XElement("lastName", p.Descendants("LastName").FirstOrDefault().Value)));

                    guests.Add(people);
                }
            }
            return guests;
        }

        private string type(string typ)
        {
            if (typ.ToUpper().Equals("ADULT"))
                return "ADT";
            else
                return "CHD";
        }
        #endregion
        #endregion
        #region Cancellation Policy With Hotel Details- RoomList(not in use)
        public XElement rooms(XElement mikirooms, XElement comrooms)
        {
            var room = from resprooms in mikirooms.Descendants("roomOption")
                       join reqrooms in comrooms.Descendants("RoomTypes")
                       on resprooms.Attribute("roomTypeCode").Value equals reqrooms.Element("Room").Attribute("ID").Value
                       select
                       new XElement("Room",
                           new XAttribute("ID", resprooms.Attribute("roomTypeCode").Value),
                           new XAttribute("RoomType", resprooms.Element("roomDescription").Value),
                           new XAttribute("PerNightRoomRate", resprooms.Descendants("dailyPrice").FirstOrDefault().Element("price").Value),
                           new XAttribute("TotalRoomRate", resprooms.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value),
                           new XAttribute("LastCancellationDate", ""),
                           getCP(mikirooms, resprooms.Attribute("roomTypeCode").Value));
            XElement result = new XElement("Rooms", room);
            return result;
        }
        #region get cancellation policy(modified)
        public XElement getCP(XElement roominput, string roomID)
        {

            XElement helper = new XElement("CancellationPolicies");
            var rooms = from resprooms in roominput.Elements("roomOption")
                        select resprooms;

            foreach (var room in rooms.Where(x => x.Attribute("roomTypeCode").Value.Equals(roomID)))
            {
                string tp = room.Descendants("roomTotalPrice").Descendants("price").FirstOrDefault().Value;
                string fnc = room.Descendants("date").FirstOrDefault().Descendants("price").FirstOrDefault().Value;

                var eachpolicy = from pol in room.Descendants("cancellationPolicies").Elements("cancellationPolicy")
                                 select
                                            new XElement("CancellationPolicy",
                                                           new XAttribute("LastCancellationDate", Convert.ToDateTime(pol.Element("appliesFrom").Value).Date.ToString("dd/MM/yyyy")),
                                                           new XAttribute("ApplicableAmount", amount(pol, tp, fnc)),
                                                           new XAttribute("NoShowPolicy", "0"), "");
                helper.Add(eachpolicy);
            }
            return helper;
        }
        #endregion
        #endregion
        #region RoomList for Request : hotelSearch
        public XElement searchRooms(XElement req)
        {

            int roomCount = 1;
            var rooms = from paxes in req.Descendants("Rooms").FirstOrDefault().Elements("RoomPax")
                        select new XElement("room",
                                                  new XElement("roomNo", roomCount++),
                                                  util.getGuests(paxes));
            XElement roomlist = new XElement("rooms", rooms);
            return roomlist;
        }
        #endregion
        #region country ID and country code
        public string MapCountry(IEnumerable<XElement> abc, string type)
        {
            string response = null;
            if (type.ToUpper().Equals("ID"))
            {
                response = abc.Descendants("CountryID").FirstOrDefault().Value;
            }
            else if (type.ToUpper().Equals("CODE"))
            {
                response = abc.Descendants("Code").Last().Value;
            }
            else
            {
                response = abc.Descendants("Name").FirstOrDefault().Value;
            }
            return response;
        }
        #endregion
        #region Amenities
        public XElement amenitiesList(XElement amenities)
        {
            XElement response = new XElement("Amenities");
            var amenity = from am in amenities.Elements()
                          where am.Value.Equals("true")
                          select new XElement("Amenity", am.Name);
            if (amenity != null)
            {
                response.Add(amenity);
            }
            else
            {
                response.SetValue("No Amenities Provided");
            }
            return response;
        }
        #endregion
        #region Currency Mapping
        public XAttribute getCurrencyCode(string currencyid)
        {

            string strPath = ConfigurationManager.AppSettings["MikiPath"] + @"CurrencyMapping.xml";

            string FilePath = Path.Combine(HttpRuntime.AppDomainAppPath, strPath);

            XDocument doc = XDocument.Load(FilePath);
            var currencyCode = from currency in doc.Descendants("entry").Where(x => x.Element("currencyID").Value.Equals(currencyid))
                               select new XElement("codes",
                                          new XElement("code", currency.Element("currencyCode").Value));
            XAttribute code = new XAttribute("currencyCode", currencyCode.Elements("code").FirstOrDefault().Value);
            return code;
        }
        #endregion
        #region Offers
        public XElement offerList(XElement specialOffers) //special offers tag in server response as parameter
        {
            string message = null;
            string offered = specialOffers.IsEmpty ? null : specialOffers.ToString();
            XElement empty = new XElement("Offers", "No Offers available");
            if (!string.IsNullOrEmpty(offered))
            {
                string strPath = ConfigurationManager.AppSettings["MikiPath"] + @"Reference_Data.xml";

                string FilePath = Path.Combine(HttpRuntime.AppDomainAppPath, strPath);
                XDocument doc = XDocument.Load(FilePath);
                XElement offers = new XElement("Offers");
                Boolean availability = Convert.ToBoolean(specialOffers.Attribute("offersPresent").Value);
                if (availability)
                {
                    //var offer = from off in specialOffers.Descendants("offer")
                    //            join refer in doc.Descendants("ruleType").Where(x => x.Attribute("ruleGroupID").Value.Equals("1"))
                    //            on off.Attribute("typeID").Value equals refer.Attribute("ID").Value
                    //            select
                    //            new XElement("Offers", refer.Element("ruleText").Value);
                    //offers.SetValue(offer);
                    var offer = from off in specialOffers.Descendants("offer")
                                select off;
                    foreach (var ofr in offer)
                    {
                        if (ofr.Attribute("typeId").Value.Equals("2"))
                        {
                            string minstay = ofr.Descendants("value").Where(x => x.Attribute("id").Value.Equals("1")).FirstOrDefault().Value;
                            string freenighhts = ofr.Descendants("value").Where(x => x.Attribute("id").Value.Equals("5")).FirstOrDefault().Value;
                            message = "Stay " + minstay + " nights, get " + freenighhts + " nights free";
                            offers.SetValue(message);
                        }
                        if (ofr.Attribute("typeId").Value.Equals("1"))
                        {
                            string minstay = ofr.Descendants("value").Where(x => x.Attribute("id").Value.Equals("1")).FirstOrDefault().Value;
                            message = "Discounted Rate on minimum stay of " + minstay + " days";
                            offers.SetValue(message);
                        }
                    }
                    return offers;
                }
                else
                {
                    return empty;
                }

            }
            else
            {
                return empty;
            }
        }
        #endregion
        #region Calculated Cancellation Policy
        public XElement CancellationPolicyTag(XElement roomOptions, XElement RoomTypes)
        {
            DateTime mindate = DateTime.MaxValue;
            XElement policies = null;
            var trial = from mikiRoom in roomOptions.Descendants("roomOption")
                        join b2bRoom in RoomTypes.Descendants("Room")
                        on mikiRoom.Attribute("roomTypeCode").Value equals b2bRoom.Attribute("ID").Value
                        where mikiRoom.Descendants("rateIdentifier").FirstOrDefault().Value.Equals(b2bRoom.Attribute("SessionID").Value)
                        select
                responseCP(mikiRoom);
            XElement result = MergCxlPolicy(trial);
            return result;
        }
        #region Get Cancellation Policies in B2B Format
        public XElement responseCP(XElement roomInputs)
        {
            XElement response = new XElement("CancellationPolicies");

            string tp = roomInputs.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value;
            string fnc = roomInputs.Descendants("date").FirstOrDefault().Descendants("price").FirstOrDefault().Value;

            var eachpolicy = from pol in roomInputs.Descendants("cancellationPolicies").Elements("cancellationPolicy")
                             select
                             new XElement("CancellationPolicy",
                                 new XAttribute("LastCancellationDate", changedate(pol.Element("appliesFrom").Value)),       // Convert.ToDateTime(pol.Element("appliesFrom").Value).Date.ToString("dd/MM/yyyy")
                                 new XAttribute("ApplicableAmount", amount(pol, tp, fnc)),
                                 new XAttribute("NoShowPolicy", "0"));
            response.Add(eachpolicy);

            return response;
        }
        #endregion
        #region Calculation Of CancellationPolicy
        public XElement MergCxlPolicy(IEnumerable<XElement> rooms)
        {
            List<XElement> cxlList = new List<XElement>();

            IEnumerable<XElement> dateLst = rooms.Descendants("CancellationPolicy").
               GroupBy(r => new { r.Attribute("LastCancellationDate").Value }).Select(y => y.FirstOrDefault()).
               OrderBy(p => p.Attribute("LastCancellationDate").Value);
            if (dateLst.Count() > 0)
            {

                foreach (var item in dateLst)
                {
                    string date = item.Attribute("LastCancellationDate").Value;
                    string noShow = item.Attribute("NoShowPolicy").Value;
                    decimal datePrice = 0.0m;
                    foreach (var rm in rooms)
                    {
                        var prItem = rm.Descendants("CancellationPolicy").
                       Where(pq => (pq.Attribute("LastCancellationDate").Value == date)).
                       FirstOrDefault();
                        if (prItem != null)
                        {
                            var price = prItem.Attribute("ApplicableAmount").Value;
                            datePrice += Convert.ToDecimal(price);
                        }
                        else
                        {
                            if (noShow == "1")
                            {
                                datePrice += Convert.ToDecimal(rm.Attribute("TotalRoomRate").Value);
                            }
                            else
                            {


                                var lastItem = rm.Descendants("CancellationPolicy").
                                    Where(pq => Convert.ToDateTime(pq.Attribute("LastCancellationDate").Value) < chnagetoTime(date));

                                if (lastItem.Count() > 0)
                                {
                                    var lastDate = lastItem.Max(y => y.Attribute("LastCancellationDate").Value);
                                    var lastprice = rm.Descendants("CancellationPolicy").
                                        Where(pq => pq.Attribute("LastCancellationDate").Value == lastDate).
                                        FirstOrDefault().Attribute("ApplicableAmount").Value;
                                    datePrice += Convert.ToDecimal(lastprice);
                                }

                            }
                        }
                    }
                    XElement pItem = new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", date), new XAttribute("ApplicableAmount", datePrice), new XAttribute("NoShowPolicy", "0"));
                    cxlList.Add(pItem);

                }

                cxlList = cxlList.GroupBy(x => new { x.Attribute("LastCancellationDate").Value }).
                    Select(y => new XElement("CancellationPolicy",
                        new XAttribute("LastCancellationDate", y.Key.ToString().Substring(10, 10)),
                        new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("ApplicableAmount").Value))),
                        new XAttribute("NoShowPolicy", "0"))).OrderBy(p => p.Attribute("LastCancellationDate").Value).ToList();
                var datecheck = from cp in cxlList
                                select DateTime.ParseExact(cp.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string selectprice = datecheck.Min().ToString("dd/MM/yyyy").Substring(0, 10);
                if (selectprice.Contains('-'))
                    selectprice = selectprice.Replace('-', '/');
                XElement tester = new XElement("test", cxlList);
                var fItem = tester.Descendants("CancellationPolicy")
                    .Where(x => x.Attribute("LastCancellationDate").Value.Equals(selectprice))
                    .FirstOrDefault();

                if (Convert.ToDecimal(fItem.Attribute("ApplicableAmount").Value) != 0.0m)
                {
                    //cxlList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", fItem.Attribute("LastCancellationDate").Value.AddDays(-1).Date.ToString("yyyy-MM-dd")), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0")));
                    cxlList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", changedate1(fItem.Attribute("LastCancellationDate").Value)), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0")));
                }
            }
            XElement cxlItem = new XElement("CancellationPolicies", cxlList);
            cxlItem.Add(cxlItem.Descendants("CancellationPolicy").Last());
            cxlItem.Descendants("CancellationPolicy").Last().Attribute("NoShowPolicy").SetValue("1");
            return cxlItem;

        }
        public DateTime chnagetoTime(string strDate)
        {
            DateTime oDate = DateTime.ParseExact(strDate, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            return oDate;

        }
        #endregion

        #endregion
        #region Get Room List
        public XElement Roomlist(string transID)
        {
            DataTable dt = msd.GetMiki_RoomList(transID);
            XElement response = XElement.Parse(dt.Rows[0]["logresponseXML"].ToString());
            return response.Descendants("hotels").FirstOrDefault();
        }
        #endregion
        #region Meal Plan Equality Check
        public bool MPEC(List<string> mpc)
        {
            bool response = true;
            string[] ar = mpc.ToArray();
            for (int i = 0; i < ar.Length; i++)
            {
                for (int j = i + 1; j < ar.Length; j++)
                {
                    if (!ar[i].Equals(ar[j]))
                        response = false;
                }
            }
            return response;
        }
        #endregion
        #region New Rooms Method
        public XElement RoomsNew(XElement Mikirooms, XElement travayooreq)
        {
            double total = 0.0;
            int count = 1;
            var occupancy = from oc in travayooreq.Descendants("RoomPax")
                            select oc;
            foreach (var oc in occupancy)
                oc.Add(new XElement("id", count++));
            List<XElement> roomlist = new List<XElement>();

            foreach (var rooms in Mikirooms.Descendants("roomOption"))
            {
                var forroom = from index in rooms.Descendants("roomNumber")
                              select index.Value;



                foreach (var pax in occupancy)
                {
                    total = total + Convert.ToDouble(rooms.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value);
                    if (forroom.Contains(pax.Descendants("id").FirstOrDefault().Value))
                    {
                        roomlist.Add(
                                     new XElement("Room",
                                      new XAttribute("ID", rooms.Attributes("roomTypeCode").FirstOrDefault().Value),
                                      new XAttribute("SuppliersID", supplierID),
                                      new XAttribute("RoomSeq", count++),
                                      new XAttribute("SessionID", travayooreq.Descendants("id").Any() ? travayooreq.Descendants("id").FirstOrDefault().Value : ""),
                                      new XAttribute("RoomType", rooms.Descendants("roomDescription").FirstOrDefault().Value),
                                      new XAttribute("OccupancyID", travayooreq.Descendants("id").Any() ? travayooreq.Descendants("id").FirstOrDefault().Value : ""),
                                      new XAttribute("OccupancyName", ""),
                                      new XAttribute("MealPlanID", rooms.Descendants("includedMeal").FirstOrDefault().Element("mealID").Value),
                                      mealplanname(rooms.Descendants("mealBasis").FirstOrDefault().Attribute("mealBasisCode").Value),
                                      new XAttribute("MealPlanCode", rooms.Descendants("mealBasis").FirstOrDefault().Attribute("mealBasisCode").Value),
                                      new XAttribute("MealPlanPrice", ""),
                                      new XAttribute("PerNightRoomRate", rooms.Descendants("dailyPrice").FirstOrDefault().Element("price").Value),
                                      new XAttribute("TotalRoomRate", rooms.Descendants("roomTotalPrice").FirstOrDefault().Element("price").Value),
                                      new XAttribute("CancellationDate", ""),
                                      new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", "true"),
                                      new XElement("RequestID", rooms.Descendants("rateIdentifier").FirstOrDefault().Value),
                                      new XElement("Offers"),
                                      new XElement("PromotionList",
                                      new XElement("Promotions")),
                                      new XElement("CancellationPolicy"),
                                      new XElement("Amenities",
                                          new XElement("Amenity")),
                                      new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                      new XElement("Supplements"),
                                      pb(rooms),
                                      new XElement("AdultNum", pax.Element("Adult").Value),
                                      new XElement("ChildNum", pax.Element("Child").Value)));
                    }
                }
            }
            XElement maybeFinal = new XElement("RoomTypes",
                                       new XAttribute("TotalRate", Convert.ToString(total)),
                                       new XAttribute("Index", "1"), roomlist);
            XElement response = new XElement("Rooms", maybeFinal);
            return response;
        }
        #endregion
        #region Promotions
        public List<XElement> promotions(XElement offerTag)
        {
            string strPath = XmlPath + @"Reference_Data.xml";

            string FilePath = Path.Combine(HttpRuntime.AppDomainAppPath, strPath);
            XDocument doc = XDocument.Load(FilePath);
            List<XElement> prom = new List<XElement>();
            bool lakijanamaikaun = Convert.ToBoolean(offerTag.Attribute("offersPresent").Value);
            string message = null;
            if (lakijanamaikaun)
            {
                var offer = from off in offerTag.Descendants("offer")
                            select off;
                foreach (var ofr in offer)
                {
                    if (ofr.Attribute("typeId").Value.Equals("2"))
                    {
                        string minstay = ofr.Descendants("value").Where(x => x.Attribute("id").Value.Equals("1")).FirstOrDefault().Value;
                        string freenighhts = ofr.Descendants("value").Where(x => x.Attribute("id").Value.Equals("5")).FirstOrDefault().Value;
                        message = "Stay " + minstay + " nights, get " + freenighhts + " nights free";
                        prom.Add(new XElement("Promotions", message));
                    }
                    if (ofr.Attribute("typeId").Value.Equals("1"))
                    {
                        string minstay = ofr.Descendants("value").Where(x => x.Attribute("id").Value.Equals("1")).FirstOrDefault().Value;
                        message = "Discounted Rate(Discount applied already)";
                        prom.Add(new XElement("Promotions", message));
                    }
                }
            }
            else
                prom.Add(new XElement("Promotions"));
            return prom;
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
        #region Star Rating Condition
        public bool StarRating(string minRating, string MaxRating, string HotelStarRating)
        {
            bool result;
            int minrating = Convert.ToInt32(minRating);
            int max = Convert.ToInt32(MaxRating);
            int star = Convert.ToInt32(HotelStarRating);
            if (star <= max && star >= minrating)
                result = true;
            else
                result = false;
            return result;
        }
        #endregion
        public XAttribute mealplanname(string mpcode)
        {
            string strPath = ConfigurationManager.AppSettings["MikiPath"] + @"Reference_Data.xml";

            string FilePath = Path.Combine(HttpRuntime.AppDomainAppPath, strPath);
            XDocument referenceData = XDocument.Load(FilePath);
            //XElement mealpcode = referenceData.Descendants("boardBasis").FirstOrDefault();
            var code = from codes in referenceData.Descendants("basis")
                       select new XElement("code", codes);
            XElement abc = new XElement("codes",
                       code);

            XAttribute name = new XAttribute("MealPlanName", abc.Descendants("basis").Where(x => x.Attribute("id").Value.Equals(mpcode)).FirstOrDefault().Value);
            return name;
        }
        public string reformatDate(string d)
        {
            DateTime dt = DateTime.ParseExact(d, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            string date = dt.ToString("yyyy-MM-dd");
            return date;
        }
        public bool roomnumber(IEnumerable<XElement> roomNumber, string check)
        {
            bool abc = false;
            var rrr = from rn in roomNumber
                      select rn.Value;
            if (rrr.Contains(check))
                abc = true;
            return abc;
        }
        #region date to dd/mm/yyyy
        public string changedate(string d)
        {
            string dd = d.Substring(0, 10);
            DateTime dt = DateTime.ParseExact(dd, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            string date = dt.ToString("dd/MM/yyyy");
            date = date.Replace('-', '/');
            return date;
        }
        #region Remove id tag
        public void removeIdTag(XElement request)
        {
            if (request.Descendants("id").Any())
                request.Descendants("id").Remove();

        }
        #endregion
        public string changedate1(string d)
        {

            DateTime dt = DateTime.ParseExact(d, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime dat = dt.AddDays(-1);
            string date = dat.ToString("dd/MM/yyyy");
            return date;
        }
        #endregion
        #region static data functions
        #region Getting Images from Database
        public string ImageFromdb(string hotelID, string imageType)
        {
            string response = string.Empty;
            DataTable Images = msd.GetMiki_Images(hotelID);
            if (Images.Rows.Count > 0)
            {
                if (imageType.ToUpper().Equals("SMALL"))
                {
                    var abc = from images in Images.AsEnumerable()
                              where images.Field<string>("ImageType") == "01"
                              select images.Field<string>("ImageThumbnail");
                    response = abc.FirstOrDefault();
                }
                else if (imageType.ToUpper().Equals("LARGE"))
                {
                    var abc = from images in Images.AsEnumerable()
                              where images.Field<string>("ImageType") == "01"
                              select images.Field<string>("ImageLarge");
                    response = abc.FirstOrDefault();
                }
            }
            return response;
        }
        #endregion
        #region Facilities
        public XElement Facilities(string hotelID)
        {


            DataTable Facility = msd.GetMiki_Facilities(hotelID);
            XElement response = null;
            XElement facilityList = new XElement("List");
            for (int j = 0; j < Facility.Rows.Count; j++)
            {
                XElement jlt = new XElement("fac");
                DataRow faci = Facility.Rows[j];
                var fac = Facility.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName);
                string[] abc = fac.ToArray();
                int count = faci.ItemArray.Count();
                for (int i = 2; i < count; i++)
                {

                    jlt.Add(new XElement(abc[i], faci[i].ToString()));
                }

                var xyz = from fa in jlt.Elements().Where(x => x.Value.Equals("true"))
                          select new XElement("Facility", fa.Name);
                response = new XElement("Facilities", new XAttribute("HotelID", faci["HotelID"].ToString()), xyz);
                facilityList.Add(response);
            }
            //{
            //    response.Add(new XElement("Facility", f));
            //}
            return facilityList;
        }
        public XElement Facilities(XElement fac)
        {
            var facility = from fc in fac.Elements()
                           where fc.Value.Equals("true")
                           select new XElement("Facility", fc.Name);
            if (facility != null)
            {
                XElement response = new XElement("Facilities", facility);
                return response;
            }
            else
            {
                XElement response = new XElement("Facilities", new XElement("Facility", "No Facilities provided"));
                return response;
            }
        }
        #endregion
        #region location
        public string location(XElement loc)
        {
            string locs = loc.Element("distanceFromHotel").Value + " kms from " + loc.Element("placeName").Value;
            return locs;

        }
        #endregion
        #region images
        public string imagePath(XElement images, string type)
        {
            string imgpath = null;
            if (images.Elements().Any())
            {
                if (images.Elements("image").Where(x => x.Attribute("imageType").Value.Equals("01")).Any())
                {
                    if (type.ToUpper().Equals("SMALL"))
                    {
                        var path = from img in images.Elements("image")
                                   where img.Attribute("imageType").Value.Equals("01")
                                   select img.Attribute("imageThumbnailURL").Value;
                        imgpath = path.FirstOrDefault();
                    }
                    else if (type.ToUpper().Equals("LARGE"))
                    {
                        var path = from img in images.Elements("image")
                                   where img.Attribute("imageType").Value.Equals("01")
                                   select img.Attribute("imageURL").Value;
                        imgpath = path.FirstOrDefault();
                    }
                }
            }
            else
            {
                return null;
            }
            return imgpath;
        }
        #endregion
        #region Images for Hotel Detail
        public XElement HotelDetailImages(XElement imagestag)
        {
            var img = from image in imagestag.Descendants("image")
                      select new XElement("Image",
                          new XAttribute("Path", image.Attribute("imageURL").Value),
                          new XAttribute("Caption", ""));
            XElement images = new XElement("Images", img);
            return images;
        }
        public XElement HotelDetailImages(string HotelID)
        {
            DataTable Images = msd.GetMiki_Images(HotelID);
            var img = from images in Images.AsEnumerable()
                      select new XElement("Image",
                                 new XAttribute("Path", images.Field<string>("ImageLarge")),
                                     new XAttribute("Caption", string.Empty));

            XElement response = new XElement("Images", img);
            return response;
        }
        #endregion
        #region Getting Address from static data
        public string getAddress(IEnumerable<XElement> address)
        {
            string add = null;
            foreach (var a in address)
            {
                if (!a.Name.Equals("line3"))
                    add = add + a.Value;
            }
            return add;
        }
        #endregion
        #region Get City From DB
        public string supplierCityID(string B2BCity)
        {
            DataTable Cities = msd.MikiCityMapping(B2BCity, "11");
            return Cities.Rows.Count > 0 ? Cities.Rows[0]["SupCityId"].ToString() : string.Empty;
        }

        #endregion
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
        #endregion
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