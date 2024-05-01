using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.HotelsPro;

namespace TravillioXMLOutService.App_Code
{
    public class HotelsProPreBook
    {
        string checkindate = string.Empty;
        XElement reqTravillio;
        string dmc = string.Empty;
        #region Price Check
        public static bool PrcCheck(decimal first, decimal second, decimal margin)
        {
            return Math.Abs(first - second) <= margin;
        }
        #endregion
        #region PreBooking of HotelsPro (XML OUT for Travayoo)
        public XElement PrebookingHotelsPro(XElement req, string xmlout)
        {
            reqTravillio = req;
            XElement hotelprebookresponse = null;
            XElement hotelpreBooking = null;
            dmc = xmlout;
            try
            {
                checkindate = req.Descendants("FromDate").FirstOrDefault().Value;
                string url = string.Empty;
                HotelsPro_Hotelstatic htlprostaticity = new HotelsPro_Hotelstatic();
                HotelsPro_Detail htlprostatcity = new HotelsPro_Detail();
                htlprostatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
                DataTable dtcity = htlprostaticity.GetCity_HotelsPro(htlprostatcity);
                string destinationcode = string.Empty;
                if (dtcity != null)
                {
                    destinationcode = dtcity.Rows[0]["citycode"].ToString();
                }
                string code = Convert.ToString(req.Descendants("Room").Attributes("ID").FirstOrDefault().Value);
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "6");
                url = suppliercred.Descendants("provisionendpoint").FirstOrDefault().Value;
                #endregion
                url = url + "/" + code + "";
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                string usernamep = string.Empty;
                string passwordp = string.Empty;
                //HotelsProCredentials _credential = new HotelsProCredentials();
                //usernamep = _credential.username;
                //passwordp = _credential.password;
                usernamep = suppliercred.Descendants("username").FirstOrDefault().Value;
                passwordp = suppliercred.Descendants("password").FirstOrDefault().Value;
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(usernamep + ":" + passwordp));
                webRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
                webRequest.ContentType = "application/json";
                webRequest.ContentLength = 0;
                webRequest.Method = "POST";
                webRequest.Host = suppliercred.Descendants("host").FirstOrDefault().Value;

                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                asyncResult.AsyncWaitHandle.WaitOne();
                string soapResult;
                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                {
                    using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                        try
                        {
                            string suprequest = url;
                            APILogDetail log = new APILogDetail();
                            log.customerID = Convert.ToInt64(req.Descendants("CustomerID").FirstOrDefault().Value);
                            log.TrackNumber = req.Descendants("TransID").Single().Value;
                            log.LogTypeID = 4;
                            log.LogType = "PreBook";
                            log.SupplierID = 6;
                            log.logrequestXML = suprequest.ToString();
                            log.logresponseXML = soapResult.ToString();
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SaveAPILogs(log);
                        }
                        catch (Exception ex)
                        {
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "PrebookingHotelsPro";
                            ex1.PageName = "HotelsProPreBook";
                            ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                            ex1.TranID = req.Descendants("TransID").Single().Value;
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                        }

                    }

                    dynamic hotellist = Newtonsoft.Json.JsonConvert.DeserializeObject(soapResult);

                    HotelsPro_Detail htlprostat = new HotelsPro_Detail();
                    HotelsPro_Hotelstatic htlprostaticdet = new HotelsPro_Hotelstatic();
                    htlprostat.CityCode = destinationcode;
                    htlprostat.MinStarRating = req.Descendants("MinStarRating").Single().Value;
                    htlprostat.MaxStarRating = req.Descendants("MaxStarRating").Single().Value;
                    DataTable dt = htlprostaticdet.GetHotelList_HotelsPro(htlprostat);
                    //hotelprebookresponse = GetHotelListHotelsPro(hotellist, dt)[0].Value;
                    List<XElement> hotelavailabilityresponse = GetHotelListHotelsPro(hotellist, dt);
                    hotelprebookresponse = hotelavailabilityresponse.FirstOrDefault();

                    // hotelprebookresponse = ((System.Xml.Linq.XElement)(GetHotelListHotelsPro(hotellist, dt)[0]));
                    #region PreBooking Response
                    IEnumerable<XElement> request = req.Descendants("HotelPreBookingRequest").ToList();
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";

                    string username = req.Descendants("UserName").Single().Value;
                    string password = req.Descendants("Password").Single().Value;
                    string AgentID = req.Descendants("AgentID").Single().Value;
                    string ServiceType = req.Descendants("ServiceType").Single().Value;
                    string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                    string supplierid = req.Descendants("SupplierID").Single().Value;


                    decimal oldprice = 0;
                    decimal newprice = 0;
                    decimal margin = 0.01m;
                    oldprice = Convert.ToDecimal(req.Descendants("HotelPreBookingRequest").Descendants("RoomTypes").Attributes("TotalRate").FirstOrDefault().Value);
                    newprice = Convert.ToDecimal(hotelprebookresponse.Descendants("RoomTypes").Attributes("TotalRate").FirstOrDefault().Value);
                    bool pricechange = PrcCheck(oldprice, newprice, margin);


                    #region XML OUT
                    if (pricechange == true)
                    {
                        hotelpreBooking = new XElement(
                          new XElement(soapenv + "Envelope",
                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                    new XElement(soapenv + "Header",
                                     new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                     new XElement("Authentication",
                                         new XElement("AgentID", AgentID),
                                         new XElement("UserName", username),
                                         new XElement("Password", password),
                                         new XElement("ServiceType", ServiceType),
                                         new XElement("ServiceVersion", ServiceVersion))),
                                            new XElement(soapenv + "Body",
                                                new XElement(request.Single()),
                                                   new XElement("HotelPreBookingResponse",
                                                       new XElement("NewPrice", null), // said by manisha
                                                       new XElement("Hotels",
                                                           hotelprebookresponse
                                          )))));

                    }
                    else
                    {
                        hotelpreBooking = new XElement(
                          new XElement(soapenv + "Envelope",
                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                    new XElement(soapenv + "Header",
                                     new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                     new XElement("Authentication",
                                         new XElement("AgentID", AgentID),
                                         new XElement("UserName", username),
                                         new XElement("Password", password),
                                         new XElement("ServiceType", ServiceType),
                                         new XElement("ServiceVersion", ServiceVersion))),
                                            new XElement(soapenv + "Body",
                                                new XElement(request.Single()),
                                                   new XElement("HotelPreBookingResponse",
                                                        new XElement("ErrorTxt", "Amount has been changed"),
                                                        new XElement("NewPrice", newprice),
                                                       new XElement("Hotels",
                                                           hotelprebookresponse
                                          )))));
                    }

                    #endregion
                    #endregion

                }
                return hotelpreBooking;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PrebookingHotelsPro";
                ex1.PageName = "HotelsProPreBook";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                IEnumerable<XElement> request = req.Descendants("HotelPreBookingRequest").ToList();
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                string username = req.Descendants("UserName").Single().Value;
                string password = req.Descendants("Password").Single().Value;
                string AgentID = req.Descendants("AgentID").Single().Value;
                string ServiceType = req.Descendants("ServiceType").Single().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                string supplierid = req.Descendants("SupplierID").Single().Value;
                hotelpreBooking = new XElement(
                         new XElement(soapenv + "Envelope",
                                   new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                   new XElement(soapenv + "Header",
                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                    new XElement("Authentication",
                                        new XElement("AgentID", AgentID),
                                        new XElement("UserName", username),
                                        new XElement("Password", password),
                                        new XElement("ServiceType", ServiceType),
                                        new XElement("ServiceVersion", ServiceVersion))),
                                           new XElement(soapenv + "Body",
                                               new XElement(request.Single()),
                                                  new XElement("HotelPreBookingResponse",
                                                       new XElement("ErrorTxt", ex.Message.ToString())))));
                return hotelpreBooking;
            }
        }
        #endregion
        #region HotelsPro
        #region HotelsPro Hotel Listing
        private IEnumerable<XElement> GetHotelListHotelsPro(dynamic hotel, DataTable dtTable)
        {
            #region HotelsPro
            List<XElement> hotellst = new List<XElement>();
           
            try
            {
                //Int32 length = Convert.ToInt32(hotel.count.Value);
                XElement mealtype = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\HotelsPro\mealtype.xml"));
                for (int i = 0; i < 1; i++)
                {
                        string hotelcode = Convert.ToString(hotel.hotel_code.Value);
                        DataRow[] row = dtTable.Select("HotelCode = " + "'" + hotelcode + "'");
                        if (row.Count() > 0)
                        {
                            string tandc = string.Empty;
                            string taxes = string.Empty;
                            string refundable = string.Empty;
                            string star = string.Empty;
                            string code = hotel.code;
                            string minRate = string.Empty;
                            string currency = string.Empty;
                            //foreach (var ent in hotel.results)
                            //{
                            //    minRate = ent.price;
                            //    currency = ent.currency;
                            //    break;
                            //}
                            dynamic hotellist = hotel;
                            dynamic roomlist = hotel.rooms;
                            try
                            {
                                star = row[0]["Star"].ToString();
                                tandc = hotel.additional_info.Value;
                                //minRate = hotel.price;
                                currency = hotel.currency;
                            }
                            catch { }
                            try
                            {
                                string inclusive = string.Empty;
                                string taxtype = string.Empty;
                                string taxcurr = string.Empty;
                                string taxamt = string.Empty;
                                string inclusivinclude = string.Empty;
                                int counttax = hotel.taxes.Count;
                                if (counttax > 0)
                                {
                                    for (int k = 0; k < counttax; k++)
                                    {
                                        inclusive = Convert.ToString(hotel.taxes[k].inclusive.Value);
                                        if (inclusive.ToUpper() == "FALSE")
                                        {
                                            inclusivinclude = "Not Included";
                                            taxtype = hotel.taxes[k].type.Value;
                                            taxcurr = hotel.taxes[k].currency.Value;
                                            taxamt = hotel.taxes[k].amount.Value;
                                            taxes += ". Taxes: " + taxtype + " " + taxcurr + " " + taxamt + "-" + inclusivinclude;
                                        }                                        
                                    }
                                }
                            }
                            catch { }
                            try
                            {
                                string refund = Convert.ToString(hotel.supports_cancellation.Value);
                                if (refund.ToUpper() == "FALSE")
                                {
                                    refundable = ". this product can not cancel over online.";
                                }
                            }
                            catch { }
                            hotellst.Add(new XElement("Hotel",
                                                   new XElement("HotelID", Convert.ToString(hotel.hotel_code.Value)),
                                                               new XElement("HotelName", Convert.ToString(row[0]["HotelName"].ToString())),
                                                               new XElement("Status", "true"),
                                                               new XElement("TermCondition", tandc + refundable + taxes),
                                                               new XElement("HotelImgSmall", Convert.ToString("")),
                                                               new XElement("HotelImgLarge", Convert.ToString("")),
                                                               new XElement("MapLink", ""),
                                                               new XElement("DMC", dmc),
                                                               new XElement("Currency", Convert.ToString(currency)),
                                                               new XElement("Offers", "")
                                                               , new XElement("Rooms",
                                                        GetHotelRoomListingHotelsPro(hotellist,roomlist, mealtype, code)
                                                       )
                            ));
                        }
                }
            }
            catch (Exception ex)
            {
                return hotellst;
            }
            return hotellst;
            #endregion
        }
        #endregion

        #region HotelsPro Hotel's Room Listing
        private IEnumerable<XElement> GetHotelRoomListingHotelsPro(dynamic hotellist, dynamic roomlist, XElement mealtype, string code)
        {

            List<XElement> strgrp = new List<XElement>();
            int nights = 0;
            int totalroom = 0;
            try
            {
                totalroom = Convert.ToInt32(reqTravillio.Descendants("RoomPax").Count());
                DateTime fromDate = DateTime.ParseExact(reqTravillio.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
                DateTime toDate = DateTime.ParseExact(reqTravillio.Descendants("ToDate").Single().Value, "dd/MM/yyyy", null);
                nights = (int)(toDate - fromDate).TotalDays;
            }
            catch { }
            #region Room Count

            //for (int i = 0; i < roomlist.Count; i++)
           // {
                List<XElement> str = new List<XElement>();
                for (int i = 0; i < roomlist.Count; i++)
                {
                    #region Parameters
                    IEnumerable<XElement> totroomprc = null;
                    dynamic pricebrkups = roomlist[i].nightly_prices;
                    string mealplancode = hotellist.meal_type;
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
                        totalamt = Convert.ToDecimal(hotellist.price.Value) / totalroom;
                        totroomprc = GetequalBreakuphPro(Convert.ToString(totalamt), nights);
                        mealname = meal.Descendants("name").FirstOrDefault().Value;
                    }
                    catch { mealname = hotellist.meal_type; }
                    try
                    {
                        totalchild = roomlist[i].pax.children_ages.Count;
                    }
                    catch { }
                    #endregion
                    #region With Board Bases
                    string roomtypename = string.Empty;
                    try
                    {
                        bool romvv = IsValidXmlString(Convert.ToString(roomlist[i].room_description.Value));
                        if (romvv == false)
                        {
                            roomtypename = roomlist[i].room_category.Value;
                        }
                        else
                        {
                            roomtypename = Convert.ToString(roomlist[i].room_description.Value);
                        }
                    }
                    catch { roomtypename = roomlist[i].room_category.Value; }
                    str.Add(new XElement("Room",
                         new XAttribute("ID", Convert.ToString(hotellist.code)),
                         new XAttribute("SuppliersID", "6"),
                         new XAttribute("RoomSeq", i + 1),
                         new XAttribute("SessionID", Convert.ToString(code)),
                         new XAttribute("RoomType", Convert.ToString(roomtypename)),
                         new XAttribute("OccupancyID", Convert.ToString("")),
                         new XAttribute("OccupancyName", Convert.ToString("")),
                         new XAttribute("MealPlanID", Convert.ToString("")),
                         new XAttribute("MealPlanName", Convert.ToString(mealname)),
                         new XAttribute("MealPlanCode", Convert.ToString(hotellist.meal_type)),
                         new XAttribute("MealPlanPrice", ""),
                         new XAttribute("PerNightRoomRate", Convert.ToString("0")), // 0 said by manisha (18072018)
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
                             //new XElement("PriceBreakups", GetRoomsPriceBreakupHotelsPro(pricebrkups)),
                              new XElement("PriceBreakups", totroomprc),
                             new XElement("AdultNum", Convert.ToString(roomlist[i].pax.adult_quantity.Value)),
                             new XElement("ChildNum", Convert.ToString(totalchild))
                         ));
                    #endregion
                }
                dynamic cxlpolicies = hotellist.policies; 
                string totp = hotellist.price;
                decimal totalprice = Convert.ToDecimal(totp);
                string currency = hotellist.currency;
                str.Add(new XElement("CancellationPolicies",
                         GetRoomCancellationPolicyHotelsPro(hotellist, currency)));

                strgrp.Add(new XElement("RoomTypes", new XAttribute("Index", "1"), new XAttribute("TotalRate", Convert.ToString(hotellist.price.Value)),
                             str));

           // }
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
        private IEnumerable<XElement> GetRoomsPriceBreakupHotelsPro(dynamic pricebreakups)
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
            return str;

            #endregion
        }
        #endregion

        #region Room's Cancellation Policies from HotelsPro
        private IEnumerable<XElement> GetRoomCancellationPolicyHotelsPro(dynamic hotellist, string currencycode)
        {
            #region Room's Cancellation Policies from HotelsPro
            List<XElement> htrm = new List<XElement>();
            dynamic cxlpolicies = hotellist.policies;
            string totp = hotellist.price;
            decimal totalprice = Convert.ToDecimal(totp);
            List<XElement> strgrp = new List<XElement>();
            DateTime cxldate = DateTime.ParseExact(checkindate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            if (cxlpolicies.Count != 0)
            {
                foreach (var ent in cxlpolicies)
                {
                    List<XElement> str = new List<XElement>();
                    string ratio = ent.ratio;

                    var values = ratio.ToString(CultureInfo.InvariantCulture).Split('.');
                    int totpercent = 0;
                    if (values.Count() > 1)
                    {
                        totpercent = int.Parse(values[1]);
                    }
                    else
                    {
                        totpercent = 100;
                    }
                    //int totpercent = int.Parse(values[1]);
                    decimal percent = 0;
                    if (Convert.ToDecimal(ratio) >= 1)
                    {
                        percent = 100;
                    }
                    else
                    {
                        percent = totpercent;
                    }
                    string daysremain = ent.days_remaining;
                    int days = Convert.ToInt32(daysremain);

                    //DateTime date = System.DateTime.Now;
                    cxldate = cxldate.AddDays(-days);
                    decimal totamt = 0;
                    totamt = totalprice * Convert.ToDecimal(percent) / 100;

                    htrm.Add(new XElement("CancellationPolicy", "Cancellation done on after " + Convert.ToDateTime(cxldate).ToString("dd/MM/yyyy") + "  will apply " + currencycode + " " + totamt + "  Cancellation fee"
                       , new XAttribute("LastCancellationDate", Convert.ToString(Convert.ToDateTime(cxldate).ToString("dd/MM/yyyy")))
                       , new XAttribute("ApplicableAmount", totamt)
                       , new XAttribute("NoShowPolicy", "0")));

                }
                cxldate = cxldate.AddDays(-1);
                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on before " + Convert.ToDateTime(cxldate).ToString("dd/MM/yyyy") + "  will apply " + currencycode + " " + 0 + "  Cancellation fee"
                       , new XAttribute("LastCancellationDate", Convert.ToString(Convert.ToDateTime(cxldate).ToString("dd/MM/yyyy")))
                       , new XAttribute("ApplicableAmount", 0)
                       , new XAttribute("NoShowPolicy", "0")));

            }
            else
            {
                cxldate = System.DateTime.Now.AddDays(-1);
                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on before " + Convert.ToDateTime(cxldate).ToString("dd/MM/yyyy") + "  will apply " + currencycode + " " + 0 + "  Cancellation fee"
                       , new XAttribute("LastCancellationDate", Convert.ToString(Convert.ToDateTime(cxldate).ToString("dd/MM/yyyy")))
                       , new XAttribute("ApplicableAmount", 0)
                       , new XAttribute("NoShowPolicy", "0")));

                cxldate = System.DateTime.Now;
                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on before " + Convert.ToDateTime(cxldate).ToString("dd/MM/yyyy") + "  will apply " + currencycode + " " + totp + "  Cancellation fee"
                       , new XAttribute("LastCancellationDate", Convert.ToString(Convert.ToDateTime(cxldate).ToString("dd/MM/yyyy")))
                       , new XAttribute("ApplicableAmount", totp)
                       , new XAttribute("NoShowPolicy", "0")));
            }



            return htrm;
            #endregion
        }
        #endregion

        #endregion                
    }
}