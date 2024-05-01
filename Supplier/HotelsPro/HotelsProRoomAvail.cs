using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.HotelsPro;

namespace TravillioXMLOutService.App_Code
{
    public class HotelsProRoomAvail
    {
        XElement reqTravayoo;
        XElement promeal = null;
        int totalroomreq = 1;
        string dmc = string.Empty;
        string customerid = string.Empty;
        #region Hotel Availability of HotelsPro (XML OUT for Travayoo)
        public List<XElement> CheckAvailabilityHotelsPro(XElement req, string htlid)
        {
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                HotelsPro_Hotelstatic htlprostaticity = new HotelsPro_Hotelstatic();
                HotelsPro_Detail htlprostatcity = new HotelsPro_Detail();
                htlprostatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
                DataTable dtcity = htlprostaticity.GetCity_HotelsPro(htlprostatcity);
                string destinationcode = string.Empty;
                if (dtcity != null)
                {
                    destinationcode = dtcity.Rows[0]["citycode"].ToString();
                }
                IEnumerable<XElement> glist = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "6");
                string hotelid = htlid;
                string code = glist.Attributes("GRequestID").FirstOrDefault().Value;                 
                #region Room Search
                string url = string.Empty;
                //url = "https://api-test.hotelspro.com/api/v2/hotel-availability/?hotel_code=" + hotelid + "&search_code=" + code + "";
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "6");
                url = suppliercred.Descendants("roomendpoint").FirstOrDefault().Value;
                url = url + hotelid + "&search_code=" + code + "";
                #endregion
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                string username = string.Empty;
                string password = string.Empty;
                //HotelsProCredentials _credential = new HotelsProCredentials();
                //username = _credential.username;
                //password = _credential.password;
                username = suppliercred.Descendants("username").FirstOrDefault().Value;
                password = suppliercred.Descendants("password").FirstOrDefault().Value;
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                webRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
                webRequest.ContentType = "application/json";
                webRequest.ContentLength = 0;
                webRequest.Method = "GET";
                webRequest.Host = suppliercred.Descendants("host").FirstOrDefault().Value;
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                asyncResult.AsyncWaitHandle.WaitOne();
                string soapResult;
                try
                {
                    using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                    {
                        using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                        {
                            soapResult = rd.ReadToEnd();
                        }
                        XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(soapResult, "hotellist");
                        dynamic hotellist = Newtonsoft.Json.JsonConvert.DeserializeObject(soapResult);
                        try
                        {
                            string suprequest = url;
                            APILogDetail log = new APILogDetail();
                            log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                            log.TrackNumber = req.Descendants("TransID").Single().Value;
                            log.LogTypeID = 2;
                            log.LogType = "RoomAvail";
                            log.SupplierID = 6;
                            log.logrequestXML = suprequest.ToString();
                            log.logresponseXML = soapResult.ToString();
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SaveAPILogs(log);
                        }
                        catch(Exception ex)
                        {
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "CheckAvailabilityHotelsPro";
                            ex1.PageName = "HotelsProRoomAvail";
                            ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
                            ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                        }

                        HotelsPro_Detail htlprostat = new HotelsPro_Detail();
                        HotelsPro_Hotelstatic htlprostaticdet = new HotelsPro_Hotelstatic();
                        htlprostat.CityCode = destinationcode;
                        htlprostat.MinStarRating = req.Descendants("MinStarRating").FirstOrDefault().Value;
                        htlprostat.MaxStarRating = req.Descendants("MaxStarRating").FirstOrDefault().Value;
                        DataTable dt = htlprostaticdet.GetHotelList_HotelsPro(htlprostat);
                        hotelavailabilityresponse = GetHotelListHotelsPro(hotellist, dt);
                    }
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "CheckAvailabilityHotelsPro";
                    ex1.PageName = "HotelsProRoomAvail";
                    ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
                    ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                    //APILog.SendCustomExcepToDB(ex1);
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                #endregion

                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAvailabilityHotelsPro";
                ex1.PageName = "HotelsProRoomAvail";
                ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
                ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                //APILog.SendCustomExcepToDB(ex1);
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        public List<XElement> getroomavailability_hpro(XElement req, XElement hpromeal)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            try
            {
                #region changed
                string dmc = string.Empty;
                totalroomreq = req.Descendants("RoomPax").ToList().Count();
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "6").ToList();
                for (int i = 0; i < htlele.Count(); i++)
                {
                    string custID = string.Empty;
                    string custName = string.Empty;
                    string grequestid = string.Empty;
                    string htlid = htlele[i].Attribute("GHtlID").Value;
                    string xmlout = string.Empty;
                    try
                    {
                        xmlout = htlele[i].Attribute("xmlout").Value;
                        grequestid = htlele[i].Attribute("GRequestID").Value;
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
                        dmc = "HotelsPro";
                    }
                    List<XElement> getrom = CheckAvailabilityHotelsProOUT(req, htlid, dmc, hpromeal, grequestid);
                    roomavailabilityresponse.Add(getrom.Descendants("Rooms").FirstOrDefault());
                }
                #endregion
                return roomavailabilityresponse;
            }
            catch { return null; }
        }
        public List<XElement> CheckAvailabilityHotelsProOUT(XElement req, string htlid, string xtype,XElement hpromeal,string grequestid)
        {
            dmc = xtype;
            reqTravayoo = req;
            promeal = hpromeal;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                HotelsPro_Hotelstatic htlprostaticity = new HotelsPro_Hotelstatic();
                HotelsPro_Detail htlprostatcity = new HotelsPro_Detail();
                htlprostatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
                DataTable dtcity = htlprostaticity.GetCity_HotelsPro(htlprostatcity);
                string destinationcode = string.Empty;
                if (dtcity != null)
                {
                    destinationcode = dtcity.Rows[0]["citycode"].ToString();
                }
                //IEnumerable<XElement> glist = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "6");
                string hotelid = htlid;
                //string code = glist.Attributes("GRequestID").FirstOrDefault().Value;
                string code = grequestid;
                #region Room Search
                string url = string.Empty;
                //url = "https://api-test.hotelspro.com/api/v2/hotel-availability/?hotel_code=" + hotelid + "&search_code=" + code + "";
               
                string username = string.Empty;
                string password = string.Empty;
                //HotelsProCredentials _credential = new HotelsProCredentials();
                //username = _credential.username;
                //password = _credential.password;
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "6");
                url = suppliercred.Descendants("roomendpoint").FirstOrDefault().Value;
                url = url + hotelid + "&search_code=" + code + "";
                username = suppliercred.Descendants("username").FirstOrDefault().Value;
                password = suppliercred.Descendants("password").FirstOrDefault().Value;
                #endregion
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                webRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
                webRequest.ContentType = "application/json";
                webRequest.ContentLength = 0;
                webRequest.Method = "GET";
                webRequest.Host = suppliercred.Descendants("host").FirstOrDefault().Value;
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                asyncResult.AsyncWaitHandle.WaitOne();
                string soapResult;
                try
                {
                    using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                    {
                        using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                        {
                            soapResult = rd.ReadToEnd();
                        }
                        XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(soapResult, "hotellist");
                        dynamic hotellist = Newtonsoft.Json.JsonConvert.DeserializeObject(soapResult);
                        try
                        {
                            string suprequest = url;
                            APILogDetail log = new APILogDetail();
                            log.customerID = Convert.ToInt64(customerid);
                            log.TrackNumber = req.Descendants("TransID").Single().Value;
                            log.LogTypeID = 2;
                            log.LogType = "RoomAvail";
                            log.SupplierID = 6;
                            log.logrequestXML = suprequest.ToString();
                            log.logresponseXML = soapResult.ToString();
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SaveAPILogs(log);
                        }
                        catch (Exception ex)
                        {
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "CheckAvailabilityHotelsProOUT";
                            ex1.PageName = "HotelsProRoomAvail";
                            ex1.CustomerID = customerid;
                            ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                        }

                        HotelsPro_Detail htlprostat = new HotelsPro_Detail();
                        HotelsPro_Hotelstatic htlprostaticdet = new HotelsPro_Hotelstatic();
                        htlprostat.CityCode = destinationcode;
                        htlprostat.MinStarRating = req.Descendants("MinStarRating").FirstOrDefault().Value;
                        htlprostat.MaxStarRating = req.Descendants("MaxStarRating").FirstOrDefault().Value;
                        //DataTable dt = htlprostaticdet.GetHotelList_HotelsPro(htlprostat);
                        DataTable dt = null;
                        hotelavailabilityresponse = GetHotelListHotelsPro(hotellist, dt);
                    }
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "CheckAvailabilityHotelsProOUT";
                    ex1.PageName = "HotelsProRoomAvail";
                    ex1.CustomerID = customerid;
                    ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                #endregion

                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAvailabilityHotelsProOUT";
                ex1.PageName = "HotelsProRoomAvail";
                ex1.CustomerID = customerid;
                ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        #endregion
                
        #region HotelsPro Hotel Listing
        private IEnumerable<XElement> GetHotelListHotelsPro(dynamic hotel, DataTable dtTable)
        {
            #region HotelsPro Hotels
            List<XElement> hotellst = new List<XElement>();
            try
            {
                Int32 length = Convert.ToInt32(hotel.count.Value);
                XElement mealtype = promeal; // XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\HotelsPro\mealtype.xml"));
                try
                {
                    for (int i = 0; i < 1; i++)
                    {
                        #region Fetch hotel
                        //string hotelcode = Convert.ToString(hotel.results[i].hotel_code.Value);
                        //DataRow[] row = dtTable.Select("HotelCode = " + "'" + hotelcode + "'");
                        //if (row.Count() > 0)
                        {
                            //string star = string.Empty;
                            string code = hotel.code;
                            //string minRate = string.Empty;
                            string currency = string.Empty;
                            foreach (var ent in hotel.results)
                            {
                                //minRate = ent.price;
                                currency = ent.currency;
                                break;
                            }

                            dynamic roomlist = hotel.results;
                            try
                            {
                                //star = row[0]["Star"].ToString();                               
                            }
                            catch { }
                            hotellst.Add(new XElement("Hotel",
                                                   //new XElement("HotelID", Convert.ToString(hotel.results[i].hotel_code.Value)),
                                                   //new XElement("HotelName", Convert.ToString(row[0]["HotelName"].ToString())),
                                                   //new XElement("PropertyTypeName", Convert.ToString("")),
                                                   //new XElement("CountryID", Convert.ToString("")),
                                                   //new XElement("CountryName", Convert.ToString("")),
                                                   //new XElement("CountryCode", Convert.ToString(row[0]["CountryCode"].ToString())),
                                                   //new XElement("CityId", Convert.ToString("")),
                                                   //new XElement("CityCode", Convert.ToString(hotel.results[i].destination_code.Value)),
                                                   //new XElement("CityName", Convert.ToString("")),
                                                   //new XElement("AreaId", Convert.ToString("")),
                                                   //new XElement("AreaName", Convert.ToString("")),
                                                   //new XElement("Address", Convert.ToString(row[0]["Address"].ToString())),
                                                   //new XElement("Location", Convert.ToString("")),
                                                   //new XElement("Description", Convert.ToString("")),
                                                   //new XElement("StarRating", Convert.ToString(star)),
                                                   //new XElement("MinRate", Convert.ToString(minRate)),
                                                   //new XElement("HotelImgSmall", Convert.ToString(row[0]["MainImage"].ToString())),
                                                   //new XElement("HotelImgLarge", Convert.ToString(row[0]["MainImage"].ToString())),
                                                   //new XElement("MapLink", ""),
                                                   //new XElement("Longitude", Convert.ToString(row[0]["Longitude"].ToString())),
                                                   //new XElement("Latitude", Convert.ToString(row[0]["Latitude"].ToString())),
                                                   new XElement("DMC", dmc),
                                                  // new XElement("SupplierID", "6"),
                                                   new XElement("Currency", Convert.ToString(currency)),
                                                   new XElement("Offers", "")
                                                   , new XElement("Facilities",
                                                       new XElement("Facility", "No Facility Available"))
                                                   , new XElement("Rooms",
                                                       GetHotelRoomListingHotelsPro(roomlist, mealtype, code, hotel.results[i].hotel_code.Value, currency)
                                                       )
                            ));
                        }
                        #endregion
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
        #region HotelsPro Hotel's Room Listing
        public IEnumerable<XElement> GetHotelRoomListingHotelsPro(dynamic roomlist, XElement mealtype, string code, string Hotelcode, string currency)
        {
            List<XElement> strgrp = new List<XElement>();


            #region Notes: The maximum number of rooms that can be retrieved by a single search request is nine (9)
            #endregion
            int nights = 0;
            int totalroom = 0;
            try
            {
                totalroom = Convert.ToInt32(reqTravayoo.Descendants("RoomPax").Count());
                DateTime fromDate = DateTime.ParseExact(reqTravayoo.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
                DateTime toDate = DateTime.ParseExact(reqTravayoo.Descendants("ToDate").Single().Value, "dd/MM/yyyy", null);
                nights = (int)(toDate - fromDate).TotalDays;
            }
            catch { }
            #region Room Count

            for (int i = 0; i < roomlist.Count; i++)
            {
                List<XElement> str = new List<XElement>();
                for (int m = 0; m < roomlist[i].rooms.Count; m++)
                {
                    IEnumerable<XElement> totroomprc = null;
                    dynamic pricebrkups = roomlist[i].rooms[m].nightly_prices;
                    string mealplancode = roomlist[i].meal_type;
                    XElement meal = mealtype.Descendants("item").Where(x => x.Descendants("code").FirstOrDefault().Value == mealplancode).FirstOrDefault();
                    string mealname = string.Empty;
                    decimal totalamt = 0;
                    int totalchild = 0;
                    try
                    {
                        //foreach (var ent in pricebrkups)
                        //{
                        //    string prc = ent.Value;
                        //    totalamt = totalamt + Convert.ToDecimal(prc);
                        //}
                        //totalamt = roomlist[i].price;
                        try
                        {
                            //totroomprc = GetRoomsPriceBreakupHotelsPro(pricebrkups, Convert.ToString(roomlist[i].price.Value));
                            //totalamt = totroomprc.Sum(x => Convert.ToDecimal(x.Attribute("PriceValue").Value));
                            totalamt = Convert.ToDecimal(roomlist[i].price.Value) / totalroom;
                            totroomprc = GetequalBreakuphPro(Convert.ToString(totalamt), nights);
                        }
                        catch { totalamt = roomlist[i].price; }
                        mealname = meal.Descendants("name").FirstOrDefault().Value;
                    }
                    catch { mealname = roomlist[i].meal_type; }
                    try
                    {
                        totalchild = roomlist[i].rooms[m].pax.children_ages.Count;
                    }
                    catch { }
                    #region With Board Bases
                    string roomtypename = string.Empty;
                    try
                    {
                        bool romvv = IsValidXmlString(Convert.ToString(roomlist[i].rooms[m].room_description.Value));
                        if(romvv==false)
                        {
                            roomtypename = roomlist[i].rooms[m].room_category.Value;
                        }
                        else
                        {
                            roomtypename = Convert.ToString(roomlist[i].rooms[m].room_description.Value);
                        }
                    }
                    catch { roomtypename = roomlist[i].rooms[m].room_category.Value; }
                    str.Add(new XElement("Room",
                         new XAttribute("ID", Convert.ToString(roomlist[i].code)),
                         new XAttribute("SuppliersID", "6"),
                         new XAttribute("RoomSeq", m + 1),
                         new XAttribute("SessionID", Convert.ToString(code)),
                         new XAttribute("RoomType", roomtypename),
                         new XAttribute("OccupancyID", Convert.ToString("")),
                         new XAttribute("OccupancyName", Convert.ToString("")),
                         new XAttribute("MealPlanID", Convert.ToString("")),
                         new XAttribute("MealPlanName", Convert.ToString(mealname)),
                         new XAttribute("MealPlanCode", Convert.ToString(roomlist[i].meal_type)),
                         new XAttribute("MealPlanPrice", ""),
                         new XAttribute("PerNightRoomRate", Convert.ToString("0")),
                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                         new XAttribute("CancellationDate", ""),
                         new XAttribute("CancellationAmount", ""),
                         new XAttribute("isAvailable", "true"),
                         new XElement("RequestID", Convert.ToString("")),
                         new XElement("Offers", ""),
                         new XElement("PromotionList", ""),
                         new XElement("CancellationPolicy", ""),
                         new XElement("Amenities", new XElement("Amenity", "")),
                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                         new XElement("Supplements", ""),
                             //new XElement("PriceBreakups", GetRoomsPriceBreakupHotelsPro(pricebrkups, Convert.ToString(roomlist[i].price.Value))),
                             new XElement("PriceBreakups", totroomprc),
                             new XElement("AdultNum", Convert.ToString(roomlist[i].rooms[m].pax.adult_quantity.Value)),
                             new XElement("ChildNum", Convert.ToString(totalchild))
                         ));
                    #endregion
                }
                if (totalroomreq == str.ToList().Count())
                {
                    strgrp.Add(new XElement("RoomTypes", new XAttribute("Index", i + 1), new XAttribute("TotalRate", Convert.ToString(roomlist[i].price.Value)), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmc), new XAttribute("CUID",customerid),
                                 str));
                }
            }
            return strgrp;

            #endregion
        }
        private static bool IsValidXmlString(string text)
        {
            try
            {
                XmlConvert.VerifyXmlChars(text);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
        #region HotelsPro Room's Price Breakups
        private IEnumerable<XElement> GetequalBreakuphPro(string totalprice, int totalnight)
        {
            #region HotelsPro Room's Price Breakups
            List<XElement> str = new List<XElement>();
            try
            {
                decimal prctotal = Convert.ToDecimal(totalprice);
                decimal nightprc = prctotal / totalnight;
                for (int i = 0; i < totalnight; i++)
                {
                    str.Add(new XElement("Price",
                          new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                          new XAttribute("PriceValue", Convert.ToString(nightprc)))
                   );
                }
            }
            catch { }
            return str;
            #endregion
        }
        private IEnumerable<XElement> GetRoomsPriceBreakupHotelsPro(dynamic pricebreakups,string totalprice)
        {
            #region HotelsPro Room's Price Breakups
            Int32 length = ((Newtonsoft.Json.Linq.JContainer)(pricebreakups)).Count;
            List<XElement> str = new List<XElement>();
            int index = 1;
            foreach (var ent in pricebreakups)
            {
                str.Add(new XElement("Price",
                      new XAttribute("Night", Convert.ToString(Convert.ToInt32(index))),
                      new XAttribute("PriceValue", Convert.ToString(ent.Value)))
               );
                index++;
            }
            #region total price check
            try
            {
                decimal totalval = str.Sum(x => Convert.ToDecimal(x.Attribute("PriceValue").Value));
                if (totalval <= 0)
                {
                    decimal prctotal = Convert.ToDecimal(totalprice);
                    int totngt = index - 1;
                    decimal nightprc = prctotal / totngt;
                    int findex = 1;
                    str = new List<XElement>();
                    foreach (var ent in pricebreakups)
                    {
                        str.Add(new XElement("Price",
                              new XAttribute("Night", Convert.ToString(Convert.ToInt32(findex))),
                              new XAttribute("PriceValue", Convert.ToString(nightprc)))
                       );
                        findex++;
                    }
                }
            }
            catch { }
            #endregion
            return str;
            #endregion
        }
        #endregion        
    }
}