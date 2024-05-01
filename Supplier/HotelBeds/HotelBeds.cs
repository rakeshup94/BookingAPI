using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.HotelBeds;

namespace TravillioXMLOutService.App_Code
{
    public class HotelBeds : IDisposable
    {
        int sup_cutime = 100000;
        XElement reqTravayoo;
        string dmc = string.Empty;
        string customerid = string.Empty;
        XElement staticdtimg;
        #region Credentails of HotelBeds
        string apiKey = string.Empty;
        string Secret = string.Empty;
        #endregion
        #region Hotel Availability of HotelBeds (XML OUT for Travayoo)
        public List<XElement> CheckAvailabilityHotelBeds(XElement req)
        {
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                HotelBedsCredential _credential = new HotelBedsCredential();
                apiKey = _credential.apiKey;
                Secret = _credential.Secret;

                HotelBeds_Detail htlhbstat = new HotelBeds_Detail();
                HotelBeds_HtStatic htlhbstaticdet = new HotelBeds_HtStatic();
                htlhbstat.CityCode = req.Descendants("CityCode").FirstOrDefault().Value.ToString();
                htlhbstat.MinRating = req.Descendants("MinStarRating").FirstOrDefault().Value.ToString();
                htlhbstat.MaxRating = req.Descendants("MaxStarRating").FirstOrDefault().Value.ToString();
                DataTable dt = htlhbstaticdet.GetHotelList_HotelBeds(htlhbstat);
                
                string hotel = string.Empty;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (i < 2000)
                    {
                        hotel = hotel + "<hotel>" + dt.Rows[i]["HotelCode"].ToString() + "</hotel>";
                    }
                }
                DateTime checkindt = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
                string minstar = string.Empty;
                if (req.Descendants("MinStarRating").Single().Value == "0")
                {
                    minstar = "1";
                }
                else
                {
                    minstar = req.Descendants("MinStarRating").Single().Value;
                }
                List<XElement> troom = req.Descendants("RoomPax").ToList();
                XElement occupancyrequest = new XElement(
                                      new XElement("occupancies", getoccupancy(troom)));

                DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                DateTime checkoutdt2 = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkout2 = checkoutdt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string sourcemarket = Convert.ToString(req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value).ToUpper().ToString();

                //string reqstr = "<?xml version='1.0' encoding='UTF-8'?>" +
                string reqstr =  "<availabilityRQ sourceMarket='" + sourcemarket + "' xmlns='http://www.hotelbeds.com/schemas/messages' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' dailyRate='true'>" +
                                      "<stay checkIn='" + checkin2 + "' checkOut='" + checkout2 + "' />" +
                                      occupancyrequest.ToString() +
                                      "<hotels>" +//<hotel>6807</hotel><hotel>6808</hotel><hotel>6809</hotel><hotel>6810</hotel>" +
                                            hotel +
                                      "</hotels>" +
                                      "<filter minCategory='" + minstar + "' maxCategory='" + req.Descendants("MaxStarRating").Single().Value + "' />" +
                                      "<filter maxRatesPerRoom='10' />" +
                                   "</availabilityRQ>";
                
                string endpoint = "https://api.test.hotelbeds.com/hotel-api/1.0/hotels";

                string signature;
                using (var sha = SHA256.Create())
                {
                    long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                    Console.WriteLine("Timestamp: " + ts);
                    var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(apiKey + Secret + ts));
                    signature = BitConverter.ToString(computedHash).Replace("-", "");
                }

                string response = string.Empty;
                using (var client = new WebClient())
                {
                    try
                    {
                        XElement availreq1 = XElement.Parse(reqstr.ToString());
                        client.Headers.Add("X-Signature", signature);
                        client.Headers.Add("Api-Key", apiKey);
                        client.Headers.Add("Accept", "application/xml");
                        client.Headers.Add("Content-Type", "application/xml");
                        response = client.UploadString(endpoint, reqstr);

                        XElement availresponse = XElement.Parse(response.ToString());

                        XElement doc = RemoveAllNamespaces(availresponse);

                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 1;
                        log.LogType = "Search";
                        log.SupplierID = 4;
                        log.logrequestXML = availreq1.ToString();
                        log.logresponseXML = doc.ToString();
                        APILog.SaveAPILogs(log);

                        XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                        List<XElement> hotelavailabilityres = doc.Descendants("hotel").ToList();

                        hotelavailabilityresponse = GetHotelListHotelBeds(hotelavailabilityres, dt).ToList();
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CheckAvailabilityHotelBeds";
                        ex1.PageName = "HotelBeds";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        APILog.SendCustomExcepToDB(ex1);
                    }
                }
                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAvailabilityHotelBeds";
                ex1.PageName = "HotelBeds";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                APILog.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        public List<XElement> CheckAvailabilityHotelBedsMerge(XElement req, XElement staticd)
        {
            reqTravayoo = req;
            //staticdtimg = staticdimg;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                HotelBedsCredential _credential = new HotelBedsCredential();
                apiKey = _credential.apiKey;
                Secret = _credential.Secret;

                #region City Mapping Applied
                HotelBeds_HtStatic hbstaticity = new HotelBeds_HtStatic();
                HotelBeds_Detail hbstatcity = new HotelBeds_Detail();
                hbstatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
                DataTable dtcity = hbstaticity.GetCity_HotelBeds(hbstatcity);
                string citycode = string.Empty;
                citycode = req.Descendants("CityCode").FirstOrDefault().Value;
                if (dtcity != null)
                {
                    citycode = dtcity.Rows[0]["citycode"].ToString();
                }                
                #endregion

                HotelBeds_Detail htlhbstat = new HotelBeds_Detail();
                HotelBeds_HtStatic htlhbstaticdet = new HotelBeds_HtStatic();
                htlhbstat.CityCode = citycode;
                htlhbstat.MinRating = req.Descendants("MinStarRating").FirstOrDefault().Value.ToString();
                htlhbstat.MaxRating = req.Descendants("MaxStarRating").FirstOrDefault().Value.ToString();
                DataTable dt = htlhbstaticdet.GetHotelList_HotelBeds(htlhbstat);

                string hotel = string.Empty;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (i < 2000)
                    {
                        hotel = hotel + "<hotel>" + dt.Rows[i]["HotelCode"].ToString() + "</hotel>";
                    }
                }
                DateTime checkindt = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
                string minstar = string.Empty;
                if (req.Descendants("MinStarRating").Single().Value == "0")
                {
                    minstar = "1";
                }
                else
                {
                    minstar = req.Descendants("MinStarRating").Single().Value;
                }

                List<XElement> troom = req.Descendants("RoomPax").ToList();
                XElement occupancyrequest = new XElement(
                                      new XElement("occupancies", getoccupancy(troom)));

                DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                DateTime checkoutdt2 = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkout2 = checkoutdt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                string sourcemarket = Convert.ToString(req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value).ToUpper().ToString();

                //string reqstr = "<?xml version='1.0' encoding='UTF-8'?>" +
                string reqstr = "<availabilityRQ sourceMarket='" + sourcemarket + "' xmlns='http://www.hotelbeds.com/schemas/messages' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' dailyRate='true'>" +
                                      "<stay checkIn='" + checkin2 + "' checkOut='" + checkout2 + "' />" +
                                      occupancyrequest.ToString() +
                                      "<hotels>"+//<hotel>6807</hotel><hotel>6808</hotel><hotel>6809</hotel><hotel>6810</hotel>" +
                                            hotel +
                                      "</hotels>" +
                                      "<filter minCategory='" + minstar + "' maxCategory='" + req.Descendants("MaxStarRating").Single().Value + "' />" +
                                      "<filter maxRatesPerRoom='10' />"+
                                   "</availabilityRQ>";
                
                string endpoint = "https://api.test.hotelbeds.com/hotel-api/1.0/hotels";

                string signature;
                using (var sha = SHA256.Create())
                {
                    long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                    Console.WriteLine("Timestamp: " + ts);
                    var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(apiKey + Secret + ts));
                    signature = BitConverter.ToString(computedHash).Replace("-", "");
                }

                string response = string.Empty;
                using (var client = new WebClient())
                {
                    try
                    {
                        client.Headers.Add("X-Signature", signature);
                        client.Headers.Add("Api-Key", apiKey);
                        client.Headers.Add("Accept", "application/xml");
                        client.Headers.Add("Content-Type", "application/xml");
                        var startTime = DateTime.Now;
                        response = client.UploadString(endpoint, reqstr);                                              
                        XElement availresponse = XElement.Parse(response.ToString());                        
                        XElement doc = RemoveAllNamespaces(availresponse);

                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 1;
                        log.LogType = "Search";
                        log.SupplierID = 4;
                        log.logrequestXML = reqstr.ToString();
                        log.logresponseXML = doc.ToString();
                        log.StartTime = startTime;
                        log.EndTime = DateTime.Now;
                        try
                        {                            
                            APILog.SaveAPILogs(log);
                        }
                        catch (Exception ex)
                        {
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "CheckAvailabilityHotelBedsMerge";
                            ex1.PageName = "HotelBeds";
                            ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                            ex1.TranID = req.Descendants("TransID").Single().Value;
                            APILog.SendCustomExcepToDB(ex1);
                        }
                        
                        XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                        List<XElement> hotelavailabilityres = doc.Descendants("hotel").ToList();

                        hotelavailabilityresponse = GetHotelListHotelBedsMerge(hotelavailabilityres,dt).ToList();
                    }
                    catch(Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CheckAvailabilityHotelBedsMerge";
                        ex1.PageName = "HotelBeds";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        APILog.SendCustomExcepToDB(ex1);
                    }
                }
                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAvailabilityHotelBedsMerge";
                ex1.PageName = "HotelBeds";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                APILog.SendCustomExcepToDB(ex1);
                return null;
            }
        }

        public List<XElement> CheckAvailabilityHotelBedsMergeThread(XElement req, XElement staticd)
        {
            reqTravayoo = req;
            //staticdtimg = staticdimg;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                HotelBedsCredential _credential = new HotelBedsCredential();
                apiKey = _credential.apiKey;
                Secret = _credential.Secret;

                #region City Mapping Applied
                HotelBeds_HtStatic hbstaticity = new HotelBeds_HtStatic();
                HotelBeds_Detail hbstatcity = new HotelBeds_Detail();
                hbstatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
                DataTable dtcity = hbstaticity.GetCity_HotelBeds(hbstatcity);
                string citycode = string.Empty;
                citycode = req.Descendants("CityCode").FirstOrDefault().Value;
                if (dtcity != null)
                {
                    citycode = dtcity.Rows[0]["citycode"].ToString();
                }
                else
                {
                    return null;
                }
                #endregion

                HotelBeds_Detail htlhbstat = new HotelBeds_Detail();
                HotelBeds_HtStatic htlhbstaticdet = new HotelBeds_HtStatic();
                htlhbstat.CityCode = citycode;
                htlhbstat.MinRating = req.Descendants("MinStarRating").FirstOrDefault().Value.ToString();
                htlhbstat.MaxRating = req.Descendants("MaxStarRating").FirstOrDefault().Value.ToString();
                DataTable dt = htlhbstaticdet.GetHotelList_HotelBeds(htlhbstat);

                string hotel = string.Empty;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    hotel = hotel + "<hotel>" + dt.Rows[i]["HotelCode"].ToString() + "</hotel>";
                }
                string lst = "<Hotels>" + hotel + "</Hotels>";
                XElement doclst = XElement.Parse(lst);
                var totalsets = Partition(doclst.Descendants("hotel").ToList(), 4);

                #region Thread Initialize
                List<XElement> hotelbedslist1 = new List<XElement>();
                List<XElement> hotelbedslist2 = new List<XElement>();
                List<XElement> hotelbedslist3 = new List<XElement>();
                List<XElement> hotelbedslist4 = new List<XElement>();
                Thread tid1 = null;
                Thread tid2 = null;
                Thread tid3 = null;
                Thread tid4 = null;

                if (totalsets[0].Count()>0)
                {                    
                    tid1 = new Thread(new ThreadStart(() => { hotelbedslist1 = CheckAvailabilityHotelBedsMergeThreadStart(req, staticd, totalsets[0].ToList()); }));
                }
                if (totalsets[1].Count() > 0)
                {
                    tid2 = new Thread(new ThreadStart(() => { hotelbedslist2 = CheckAvailabilityHotelBedsMergeThreadStart(req, staticd, totalsets[1].ToList()); }));
                }
                if (totalsets[2].Count() > 0)
                {
                    tid3 = new Thread(new ThreadStart(() => { hotelbedslist3 = CheckAvailabilityHotelBedsMergeThreadStart(req, staticd, totalsets[2].ToList()); }));
                }
                if (totalsets[3].Count() > 0)
                {
                    tid4 = new Thread(new ThreadStart(() => { hotelbedslist4 = CheckAvailabilityHotelBedsMergeThreadStart(req, staticd, totalsets[3].ToList()); }));
                }
                #endregion

                #region Thread Start
                if (totalsets[0].Count() > 0)
                {
                    tid1.Start();
                }
                if (totalsets[1].Count() > 0)
                {
                    tid2.Start();
                }
                if (totalsets[2].Count() > 0)
                {
                    tid3.Start();
                }
                if (totalsets[3].Count() > 0)
                {
                    tid4.Start();
                }
                #endregion

                #region Thread Join
                if (totalsets[0].Count() > 0)
                {
                    tid1.Join();
                }
                if (totalsets[1].Count() > 0)
                {
                    tid2.Join();
                }
                if (totalsets[2].Count() > 0)
                {
                    tid3.Join();
                }
                if (totalsets[3].Count() > 0)
                {
                    tid4.Join();
                }
                #endregion

                #region Thread Abort
                if (totalsets[0].Count() > 0)
                {
                    tid1.Abort();
                }
                if (totalsets[1].Count() > 0)
                {
                    tid2.Abort();
                }
                if (totalsets[2].Count() > 0)
                {
                    tid3.Abort();
                }
                if (totalsets[3].Count() > 0)
                {
                    tid4.Abort();
                }
                #endregion

                

                List<XElement> hotelbedslist = new List<XElement>();
                
                if (hotelbedslist1==null)
                {
                    hotelbedslist1 = new List<XElement>();
                }
                if (hotelbedslist2 == null)
                {
                    hotelbedslist2 = new List<XElement>();
                }
                if (hotelbedslist3 == null)
                {
                    hotelbedslist3 = new List<XElement>();
                }
                if (hotelbedslist4 == null)
                {
                    hotelbedslist4 = new List<XElement>();
                }

                hotelbedslist.AddRange(hotelbedslist1.Concat(hotelbedslist2).Concat(hotelbedslist3).Concat(hotelbedslist4));

                hotelavailabilityresponse = GetHotelListHotelBedsMerge(hotelbedslist, dt).ToList();
                   
                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAvailabilityHotelBedsMergeThread";
                ex1.PageName = "HotelBeds";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                APILog.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        public List<XElement> CheckAvailabilityHotelBedsMergeThreadStart(XElement req, XElement staticd, List<XElement> htllist)
        {
            reqTravayoo = req;
            //staticdtimg = staticdimg;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                
                DateTime checkindt = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
                string minstar = string.Empty;
                if (req.Descendants("MinStarRating").Single().Value == "0")
                {
                    minstar = "1";
                }
                else
                {
                    minstar = req.Descendants("MinStarRating").Single().Value;
                }

                List<XElement> troom = req.Descendants("RoomPax").ToList();
                XElement occupancyrequest = new XElement(
                                      new XElement("occupancies", getoccupancy(troom)));

                DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                DateTime checkoutdt2 = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkout2 = checkoutdt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                XElement htllistreq = new XElement("hotels", htllist.ToList());
                string sourcemarket = Convert.ToString(req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value).ToUpper().ToString();

                string reqstr = "<?xml version='1.0' encoding='UTF-8'?>" +
                                   "<availabilityRQ sourceMarket='" + sourcemarket + "' xmlns='http://www.hotelbeds.com/schemas/messages' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' dailyRate='true'>" +
                                      "<stay checkIn='" + checkin2 + "' checkOut='" + checkout2 + "' />" +
                                      occupancyrequest.ToString() +
                                      //"<hotels>" +
                                      //      htllist.ToList() +
                                      //"</hotels>" +
                                      htllistreq+
                                      "<filter minCategory='" + minstar + "' maxCategory='" + req.Descendants("MaxStarRating").Single().Value + "' />" +
                                      "<filter maxRatesPerRoom='10' />" +
                                   "</availabilityRQ>";

                string endpoint = "https://api.test.hotelbeds.com/hotel-api/1.0/hotels";

                string signature;
                using (var sha = SHA256.Create())
                {
                    long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                    Console.WriteLine("Timestamp: " + ts);
                    var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(apiKey + Secret + ts));
                    signature = BitConverter.ToString(computedHash).Replace("-", "");
                }

                string response = string.Empty;
                using (var client = new WebClient())
                {
                    try
                    {
                        XElement availreq1 = XElement.Parse(reqstr.ToString());
                        client.Headers.Add("X-Signature", signature);
                        client.Headers.Add("Api-Key", apiKey);
                        client.Headers.Add("Accept", "application/xml");
                        client.Headers.Add("Content-Type", "application/xml");
                        response = client.UploadString(endpoint, reqstr);
                        XElement availresponse = XElement.Parse(response.ToString());
                        XElement doc = RemoveAllNamespaces(availresponse);
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 1;
                        log.LogType = "Search";
                        log.SupplierID = 4;
                        log.logrequestXML = availreq1.ToString();
                        log.logresponseXML = doc.ToString();
                        try
                        {
                            APILog.SaveAPILogs(log);
                        }
                        catch (Exception ex)
                        {
                            #region Exception
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "CheckAvailabilityHotelBedsMergeThreadStart";
                            ex1.PageName = "HotelBeds";
                            ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                            ex1.TranID = req.Descendants("TransID").Single().Value;
                            APILog.SendCustomExcepToDB(ex1);

                            #endregion
                        }
                        XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                        List<XElement> hotelavailabilityres = doc.Descendants("hotel").ToList();

                        hotelavailabilityresponse = hotelavailabilityres;
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CheckAvailabilityHotelBedsMergeThreadStart";
                        ex1.PageName = "HotelBeds";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        APILog.SendCustomExcepToDB(ex1);

                        #endregion
                    }
                }
                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAvailabilityHotelBedsMergeThreadStart";
                ex1.PageName = "HotelBeds";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                APILog.SendCustomExcepToDB(ex1);
                return null;
            }
        }


        //public List<XElement> CheckAvailabilityHotelBedsMergeThreadFinal(XElement req, XElement staticd)
        //{
        //    dmc = "HotelBeds";
        //    reqTravayoo = req;
        //    List<XElement> hotelavailabilityresponse = null;
        //    try
        //    {
        //        #region get cut off time
        //        try
        //        {
        //            sup_cutime = supplier_Cred.secondcutoff_time();
        //        }
        //        catch { }
        //        #endregion
        //        #region City Mapping Applied
        //        HotelBeds_HtStatic hbstaticity = new HotelBeds_HtStatic();
        //        HotelBeds_Detail hbstatcity = new HotelBeds_Detail();
        //        hbstatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
        //        DataTable dtcity = hbstaticity.GetCity_HotelBeds(hbstatcity);
        //        string citycode = string.Empty;
        //        citycode = req.Descendants("CityCode").FirstOrDefault().Value;
        //        if (dtcity != null)
        //        {
        //            if (dtcity.Rows.Count != 0)
        //            {
        //                citycode = dtcity.Rows[0]["citycode"].ToString();
        //            }
        //        }
        //        #endregion

        //        HotelBeds_Detail htlhbstat = new HotelBeds_Detail();
        //        HotelBeds_HtStatic htlhbstaticdet = new HotelBeds_HtStatic();
        //        htlhbstat.CityCode = citycode;
        //        htlhbstat.MinRating = req.Descendants("MinStarRating").FirstOrDefault().Value.ToString();
        //        htlhbstat.MaxRating = req.Descendants("MaxStarRating").FirstOrDefault().Value.ToString();
        //        DataTable dt = htlhbstaticdet.GetHotelList_HotelBeds(htlhbstat);


        //        string hotel = string.Empty;
        //        for (int i = 0; i < dt.Rows.Count; i++)
        //        {
        //            hotel = hotel + "<hotel>" + dt.Rows[i]["HotelCode"].ToString() + "</hotel>";
        //        }
        //        string lst = "<Hotels>" + hotel + "</Hotels>";
        //        XElement doclst = XElement.Parse(lst);
                
        //        var SlotList = BreakIntoSlots(doclst.Descendants("hotel").ToList(), 500);
        //        int Number = SlotList.Count;

        //        Thread[] theThreads = new Thread[Number];
        //        List<XElement> Thresult = new List<XElement>();

        //        #region Request Parameter
        //        DateTime checkindt = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
        //        string minstar = string.Empty;
        //        if (req.Descendants("MinStarRating").Single().Value == "0")
        //        {
        //            minstar = "1";
        //        }
        //        else
        //        {
        //            minstar = req.Descendants("MinStarRating").Single().Value;
        //        }

        //        List<XElement> troom = req.Descendants("RoomPax").ToList();
        //        XElement occupancyrequest = new XElement(
        //                              new XElement("occupancies", getoccupancy(troom)));

        //        DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        //        string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        //        DateTime checkoutdt2 = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        //        string checkout2 = checkoutdt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        //        string sourcemarket = Convert.ToString(req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value).ToUpper().ToString();
        //        #endregion
        //        if(sourcemarket=="GB")
        //        {
        //            sourcemarket = "UK";
        //        }
        //        for (int counter = 0; counter < Number; counter++)
        //        {                   
        //            XElement htllistreq = new XElement("hotels", SlotList[counter].ToList());
        //            string reqstr = "<?xml version='1.0' encoding='UTF-8'?>" +
        //                               "<availabilityRQ sourceMarket='" + sourcemarket + "' xmlns='http://www.hotelbeds.com/schemas/messages' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' dailyRate='true'>" +
        //                                  "<stay checkIn='" + checkin2 + "' checkOut='" + checkout2 + "' />" +
        //                                  occupancyrequest.ToString() +
        //                                  htllistreq +
        //                                  "<filter minCategory='" + minstar + "' maxCategory='" + req.Descendants("MaxStarRating").Single().Value + "' packaging='true' />" +
        //                                  "<filter maxRatesPerRoom='10' />" +
        //                               "</availabilityRQ>";

        //            ThreadStart ts = delegate
        //            {
        //                XElement result = CheckAvailabilityHotelBedsMergeThreadStartFinal(req, reqstr, dt);
        //                if(result!=null)
        //                {
        //                    Thresult.Add(result);
        //                }
        //            };
        //            Thread t = new Thread(ts);
        //            t.Name = counter.ToString();
        //            t.IsBackground = true;
        //            theThreads[counter] = t;
        //            theThreads[counter].Start();
        //        }
        //        int timeOut = sup_cutime;
        //        System.Diagnostics.Stopwatch Timer = new System.Diagnostics.Stopwatch();
        //        Timer.Start();
        //        for (int i = 0; i < theThreads.Length; i++)
        //        {
        //            theThreads[i].Join(timeOut);
        //            timeOut = timeOut - Convert.ToInt32(Timer.ElapsedMilliseconds);
        //        }
        //        for (int i = 0; i < theThreads.Length; i++)
        //        {
        //            theThreads[i].Abort();
        //        }
        //        List<XElement> hotelbedslist = new List<XElement>();
        //        foreach (var item in Thresult)
        //        {
        //            if (item != null)
        //            {
        //                if (item.Descendants("Hotel").Count() > 0)
        //                {
        //                    hotelbedslist.AddRange(item.Descendants("Hotel"));
        //                }
        //            }
        //        }                
        //        hotelavailabilityresponse = hotelbedslist;
        //        return hotelavailabilityresponse;
        //    }
        //    catch (Exception ex)
        //    {
        //        CustomException ex1 = new CustomException(ex);
        //        ex1.MethodName = "CheckAvailabilityHotelBedsMergeThreadFinal";
        //        ex1.PageName = "HotelBeds";
        //        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
        //        ex1.TranID = req.Descendants("TransID").Single().Value;
        //        SaveAPILog saveex = new SaveAPILog();
        //        saveex.SendCustomExcepToDB(ex1);
        //        return null;
        //    }
        //}
        public List<XElement> CheckAvailabilityHotelBedsMergeThreadFinal(XElement req, XElement staticd, string custID, string custName)
        {
            dmc = custName;
            customerid = custID;
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                #region get cut off time
                try
                {
                    sup_cutime = supplier_Cred.secondcutoff_time(); 
                }
                catch { }
                #endregion

                #region City Mapping Applied
                HotelBeds_HtStatic hbstaticity = new HotelBeds_HtStatic();
                HotelBeds_Detail hbstatcity = new HotelBeds_Detail();
                hbstatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
                DataTable dtcity = hbstaticity.GetCity_HotelBeds(hbstatcity);
                string citycode = string.Empty;
                citycode = req.Descendants("CityCode").FirstOrDefault().Value;
                if (dtcity != null)
                {
                    if (dtcity.Rows.Count != 0)
                    {
                        citycode = dtcity.Rows[0]["citycode"].ToString();
                    }
                    else
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(customerid);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 1;
                        log.LogType = "Search";
                        log.SupplierID = 4;
                        log.logrequestXML = req.ToString();
                        log.logresponseXML = "There is no city mapped in database";
                        try
                        {
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SaveAPILogs(log);
                        }
                        catch { }
                        return null;
                    }
                }
                else
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(customerid);
                    log.TrackNumber = req.Descendants("TransID").Single().Value;
                    log.LogTypeID = 1;
                    log.LogType = "Search";
                    log.SupplierID = 4;
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = "There is no city mapped in database";
                    try
                    {
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch { }
                    return null;
                }
                #endregion

                HotelBeds_Detail htlhbstat = new HotelBeds_Detail();
                HotelBeds_HtStatic htlhbstaticdet = new HotelBeds_HtStatic();
                htlhbstat.CityCode = citycode;
                htlhbstat.CityID = req.Descendants("CityID").FirstOrDefault().Value;
                htlhbstat.MinRating = req.Descendants("MinStarRating").FirstOrDefault().Value.ToString();
                htlhbstat.MaxRating = req.Descendants("MaxStarRating").FirstOrDefault().Value.ToString();

                htlhbstat.HotelCode = req.Descendants("HotelID").FirstOrDefault().Value;
                htlhbstat.HotelName = req.Descendants("HotelName").FirstOrDefault().Value;

                DataTable dt = htlhbstaticdet.GetHotelList_HotelBeds(htlhbstat);
                if (dt.Rows.Count == 0)
                {
                    #region Exception
                    CustomException ex1 = new CustomException("There is no hotel available in database");
                    ex1.MethodName = "CheckAvailabilityHotelsProThread";
                    ex1.PageName = "HotelsProHotelSearch";
                    ex1.CustomerID = customerid.ToString();
                    ex1.TranID = req.Descendants("TransID").First().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                    return null;
                }

                string hotel = string.Empty;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    hotel = hotel + "<hotel>" + dt.Rows[i]["HotelCode"].ToString() + "</hotel>";
                }
                string lst = "<Hotels>" + hotel + "</Hotels>";
                XElement doclst = XElement.Parse(lst);

                int minCount = 200;
                int setCount = 2;                   // make sure number and set count are both even numbers. Currently numbers = 6 and setCount = 2
                int chunkSize = 0;
                if (doclst.Descendants("hotel").Count() <= minCount)
                {
                    chunkSize = minCount;
                    setCount = 1;
                }
                else
                {
                    chunkSize = doclst.Descendants("hotel").Count();
                    int modCount = chunkSize / 4;
                    chunkSize = chunkSize - (modCount * 3);
                }

                var SlotList = BreakIntoSlots(doclst.Descendants("hotel").ToList(), chunkSize);
                int Number = SlotList.Count;
                
                int threadCount = Number / setCount;
                Thread[] theThreads = new Thread[threadCount];
                //XElement[] Thresult = new XElement[Number + 10];
                List<XElement> Thresult = new List<XElement>();

                #region Request Parameter
                DateTime checkindt = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
                string minstar = string.Empty;
                if (req.Descendants("MinStarRating").Single().Value == "0")
                {
                    minstar = "1";
                }
                else
                {
                    minstar = req.Descendants("MinStarRating").Single().Value;
                }

                List<XElement> troom = req.Descendants("RoomPax").ToList();
                XElement occupancyrequest = new XElement(
                new XElement("occupancies", getoccupancy(troom)));

                DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                DateTime checkoutdt2 = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkout2 = checkoutdt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                string sourcemarket = Convert.ToString(req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value).ToUpper().ToString();
                #endregion
                if (sourcemarket == "GB")
                {
                    sourcemarket = "UK";
                }

                int cnt = 0;
                for (int outer = 0; outer < setCount; outer++)
                {

                    for (int counter = 0; counter < threadCount; counter++)
                    {
                        XElement htllistreq = new XElement("hotels", SlotList[counter + cnt].ToList());
                        string reqstr = "<?xml version='1.0' encoding='UTF-8'?>" +
                        "<availabilityRQ sourceMarket='" + sourcemarket + "' xmlns='http://www.hotelbeds.com/schemas/messages' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' dailyRate='true'>" +
                        "<stay checkIn='" + checkin2 + "' checkOut='" + checkout2 + "' />" +
                        occupancyrequest.ToString() +
                        htllistreq +
                        "<filter minCategory='" + minstar + "' maxCategory='" + req.Descendants("MaxStarRating").Single().Value + "' packaging='true' />" +
                        "<filter maxRatesPerRoom='10' />" +
                        "</availabilityRQ>";

                        ThreadStart ts = delegate
                        {
                            XElement result = CheckAvailabilityHotelBedsMergeThreadStartFinal(req, reqstr, dt);
                            if (result != null)
                            {
                                Thresult.Add(result);
                            }
                        };
                        Thread t = new Thread(ts);
                        t.Name = counter.ToString();
                        t.IsBackground = true;
                        theThreads[counter] = t;
                        theThreads[counter].Start();
                    }
                    cnt = threadCount;
                    int timeOut = sup_cutime;
                    System.Diagnostics.Stopwatch Timer = new System.Diagnostics.Stopwatch();
                    Timer.Start();
                    for (int i = 0; i < theThreads.Length; i++)
                    {
                        theThreads[i].Join(timeOut);
                        timeOut = timeOut - Convert.ToInt32(Timer.ElapsedMilliseconds);
                    }
                    for (int i = 0; i < theThreads.Length; i++)
                    {
                        theThreads[i].Abort();
                    }
                    theThreads = new Thread[threadCount];
                }
                List<XElement> hotelbedslist = new List<XElement>();
                foreach (var item in Thresult)
                {
                    if (item != null)
                    {
                        if (item.Descendants("Hotel").Count() > 0)
                        {
                            hotelbedslist.AddRange(item.Descendants("Hotel"));
                        }
                    }
                }
                hotelavailabilityresponse = hotelbedslist;
                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAvailabilityHotelBedsMergeThreadFinal";
                ex1.PageName = "HotelBeds";
                ex1.CustomerID = customerid;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        public List<XElement> CheckAvailabilityHotelBedsMergeThreadFinalHA(XElement req, XElement staticd)
        {
            dmc = "HA";
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                #region get cut off time
                try
                {
                    sup_cutime = supplier_Cred.secondcutoff_time();
                }
                catch { }
                #endregion
                //HotelBedsCredential _credential = new HotelBedsCredential();
                //apiKey = _credential.apiKey;
                //Secret = _credential.Secret;

                #region City Mapping Applied
                HotelBeds_HtStatic hbstaticity = new HotelBeds_HtStatic();
                HotelBeds_Detail hbstatcity = new HotelBeds_Detail();
                hbstatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
                DataTable dtcity = hbstaticity.GetCity_HotelBeds(hbstatcity);
                string citycode = string.Empty;
                citycode = req.Descendants("CityCode").FirstOrDefault().Value;
                if (dtcity != null)
                {
                    if (dtcity.Rows.Count != 0)
                    {
                        citycode = dtcity.Rows[0]["citycode"].ToString();
                    }
                }
                #endregion

                HotelBeds_Detail htlhbstat = new HotelBeds_Detail();
                HotelBeds_HtStatic htlhbstaticdet = new HotelBeds_HtStatic();
                htlhbstat.CityCode = citycode;
                htlhbstat.CityID = req.Descendants("CityID").FirstOrDefault().Value;
                htlhbstat.MinRating = req.Descendants("MinStarRating").FirstOrDefault().Value.ToString();
                htlhbstat.MaxRating = req.Descendants("MaxStarRating").FirstOrDefault().Value.ToString();
                DataTable dt = htlhbstaticdet.GetHotelList_HotelBeds(htlhbstat);


                string hotel = string.Empty;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    hotel = hotel + "<hotel>" + dt.Rows[i]["HotelCode"].ToString() + "</hotel>";
                }
                string lst = "<Hotels>" + hotel + "</Hotels>";
                XElement doclst = XElement.Parse(lst);

                var SlotList = BreakIntoSlots(doclst.Descendants("hotel").ToList(), 500);
                int Number = SlotList.Count;

                Thread[] theThreads = new Thread[Number];
                //XElement[] Thresult = new XElement[Number + 10];
                List<XElement> Thresult = new List<XElement>();

                #region Request Parameter
                DateTime checkindt = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
                string minstar = string.Empty;
                if (req.Descendants("MinStarRating").Single().Value == "0")
                {
                    minstar = "1";
                }
                else
                {
                    minstar = req.Descendants("MinStarRating").Single().Value;
                }

                List<XElement> troom = req.Descendants("RoomPax").ToList();
                XElement occupancyrequest = new XElement(
                                      new XElement("occupancies", getoccupancy(troom)));

                DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                DateTime checkoutdt2 = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkout2 = checkoutdt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                string sourcemarket = Convert.ToString(req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value).ToUpper().ToString();
                #endregion
                if (sourcemarket == "GB")
                {
                    sourcemarket = "UK";
                }
                for (int counter = 0; counter < Number; counter++)
                {
                    XElement htllistreq = new XElement("hotels", SlotList[counter].ToList());
                    string reqstr = "<?xml version='1.0' encoding='UTF-8'?>" +
                                       "<availabilityRQ sourceMarket='" + sourcemarket + "' xmlns='http://www.hotelbeds.com/schemas/messages' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' dailyRate='true'>" +
                                          "<stay checkIn='" + checkin2 + "' checkOut='" + checkout2 + "' />" +
                                          occupancyrequest.ToString() +
                                          htllistreq +
                                          "<filter minCategory='" + minstar + "' maxCategory='" + req.Descendants("MaxStarRating").Single().Value + "' packaging='true' />" +
                                          "<filter maxRatesPerRoom='10' />" +
                                       "</availabilityRQ>";

                    ThreadStart ts = delegate
                    {
                        XElement result = CheckAvailabilityHotelBedsMergeThreadStartFinal(req, reqstr, dt);
                        if (result != null)
                        {
                            Thresult.Add(result);
                        }
                    };
                    Thread t = new Thread(ts);
                    t.Name = counter.ToString();
                    t.IsBackground = true;
                    theThreads[counter] = t;
                    theThreads[counter].Start();
                }
                int timeOut = sup_cutime;
                System.Diagnostics.Stopwatch Timer = new System.Diagnostics.Stopwatch();
                Timer.Start();
                for (int i = 0; i < theThreads.Length; i++)
                {
                    theThreads[i].Join(timeOut);
                    timeOut = timeOut - Convert.ToInt32(Timer.ElapsedMilliseconds);
                }
                for (int i = 0; i < theThreads.Length; i++)
                {
                    theThreads[i].Abort();
                }
                List<XElement> hotelbedslist = new List<XElement>();
                foreach (var item in Thresult)
                {
                    if (item != null)
                    {
                        if (item.Descendants("Hotel").Count() > 0)
                        {
                            hotelbedslist.AddRange(item.Descendants("Hotel"));
                        }
                    }
                }

                hotelavailabilityresponse = hotelbedslist;

                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAvailabilityHotelBedsMergeThreadFinalHA";
                ex1.PageName = "HotelBeds";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }

        public XElement CheckAvailabilityHotelBedsMergeThreadStartFinal(XElement req, string hbreq, DataTable dt)
        {
            reqTravayoo = req;
            XElement hotelavailabilityresponse=null;
            var startTime = DateTime.Now;
            try
            {
                //HotelBedsCredential _credential = new HotelBedsCredential();
                //apiKey = _credential.apiKey;
                //Secret = _credential.Secret;
                //string endpoint = "https://api.test.hotelbeds.com/hotel-api/1.0/hotels"; 
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "4");
                apiKey = suppliercred.Descendants("apiKey").FirstOrDefault().Value;
                Secret = suppliercred.Descendants("Secret").FirstOrDefault().Value;
                string endpoint = suppliercred.Descendants("searchendpoint").FirstOrDefault().Value;
                #endregion
                string signature;
                using (var sha = SHA256.Create())
                {
                    long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                    //Console.WriteLine("Timestamp: " + ts);
                    var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(apiKey + Secret + ts));
                    signature = BitConverter.ToString(computedHash).Replace("-", "");
                    //var crypt = new SHA256Managed();
                    //string hash = String.Empty;
                    //byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(apiKey + Secret + ts));
                    //foreach (byte theByte in crypto)
                    //{
                    //    hash += theByte.ToString("x2");
                    //}
                    //signature = hash;
                }
                string response = string.Empty;
                using (var client = new WebClient())
                {
                    try
                    {                        
                        XElement availreq1 = XElement.Parse(hbreq.ToString());
                        client.Headers.Add("X-Signature", signature);
                        client.Headers.Add("Api-Key", apiKey);
                        client.Headers.Add("Accept", "application/xml");
                        client.Headers.Add("Content-Type", "application/xml");
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12;
                        response = client.UploadString(endpoint, hbreq);
                        XElement availresponse = XElement.Parse(response.ToString());
                        XElement doc = RemoveAllNamespaces(availresponse);
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(customerid);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 1;
                        log.LogType = "Search";
                        log.SupplierID = 4;
                        log.logrequestXML = availreq1.ToString();
                        log.logresponseXML = doc.ToString();
                        log.StartTime = startTime;
                        log.EndTime = DateTime.Now;
                        try
                        {
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SaveAPILogs(log);
                        }
                        catch(Exception ex)
                        {
                            #region Exception
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "CheckAvailabilityHotelBedsMergeThreadStartFinal1";
                            ex1.PageName = "HotelBeds";
                            ex1.CustomerID = customerid;
                            ex1.TranID = req.Descendants("TransID").Single().Value;
                            //APILog.SendCustomExcepToDB(ex1);
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                            #endregion
                        }
                        XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                        List<XElement> hotelavailabilityres = doc.Descendants("hotel").ToList();
                        List<XElement> hotelavailabilityresp = GetHotelListHotelBedsMerge(hotelavailabilityres, dt).ToList();
                        XElement resp = new XElement("Hotels", hotelavailabilityresp);
                        return resp;
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(customerid);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 1;
                        log.LogType = "Search";
                        log.SupplierID = 4;
                        log.logrequestXML = hbreq.ToString();
                        log.logresponseXML = ex.Message.ToString();
                        log.StartTime = startTime;
                        log.EndTime = DateTime.Now;
                        try
                        {
                            SaveAPILog saveapi = new SaveAPILog();
                            saveapi.SaveAPILogs(log);
                        }
                        catch { }
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CheckAvailabilityHotelBedsMergeThreadStartFinal2";
                        ex1.PageName = "HotelBeds";
                        ex1.CustomerID = customerid;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                }
                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                #region Exception
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(customerid);
                log.TrackNumber = req.Descendants("TransID").Single().Value;
                log.LogTypeID = 1;
                log.LogType = "Search";
                log.SupplierID = 4;
                log.logrequestXML = hbreq.ToString();
                log.logresponseXML = ex.Message.ToString();
                log.StartTime = startTime;
                log.EndTime = DateTime.Now;
                try
                {
                    SaveAPILog saveapi = new SaveAPILog();
                    saveapi.SaveAPILogs(log);
                }
                catch { }
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAvailabilityHotelBedsMergeThreadStartFinal3";
                ex1.PageName = "HotelBeds";
                ex1.CustomerID = customerid;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
                return null;
            }
        }
        #endregion
        #region Hotel Availability Request
        private List<XElement> getoccupancy(List<XElement> room)
        {
            #region Get Total Occupancy
            List<XElement> str = new List<XElement>();

            for (int i = 0; i < room.Count(); i++)
            {
                str.Add(new XElement("occupancy",
                       new XAttribute("rooms", Convert.ToString("1")),
                       new XAttribute("adults", Convert.ToString(room[i].Descendants("Adult").SingleOrDefault().Value)),
                       new XAttribute("children", Convert.ToString(room[i].Descendants("Child").SingleOrDefault().Value)),
                       new XElement("paxes", getadults(room[i]), getchildren(room[i])))
                );
            }
            return str;
            #endregion
        }
        #region total room with paxes
        public List<XElement> getoccupancyroom(List<XElement> troom)
        {
            #region Test
            
            XElement Totrooms = new XElement("Rooms");
            var groupedRooms = from room in troom
                               group room by room.Value;
            foreach (var group1 in groupedRooms)
            {
                int count = group1.Count();
                XElement RoomtoAdd = group1.First();
                RoomtoAdd.Add(new XAttribute("count", count));
                Totrooms.Add(RoomtoAdd);
            }
            List<XElement> toroom = Totrooms.Descendants("RoomPax").ToList();
            #endregion

            #region Get Total Occupancy
            List<XElement> str = new List<XElement>();

            for (int i = 0; i < toroom.Count(); i++)
            {
                str.Add(new XElement("occupancy",
                       new XAttribute("rooms", Convert.ToString(toroom[i].Attribute("count").Value)),
                       new XAttribute("adults", Convert.ToString(toroom[i].Descendants("Adult").SingleOrDefault().Value)),
                       new XAttribute("children", Convert.ToString(toroom[i].Descendants("Child").SingleOrDefault().Value)),
                       new XElement("paxes", getadults(toroom[i]), getchildren(toroom[i])))
                );
            }
            return str;
            #endregion
        }
        #endregion
        private IEnumerable<XElement> getadults(XElement room)
        {
            #region Get Total Adults
            List<XElement> str = new List<XElement>();
            int rcount = Convert.ToInt32(room.Descendants("Adult").SingleOrDefault().Value);

            for (int i = 0; i < rcount; i++)
            {
                str.Add(new XElement("pax",
                       new XAttribute("type", "AD"))
                );
            }

            return str;
            #endregion
        }
        private IEnumerable<XElement> getchildren(XElement room)
        {
            #region Get Total Children
            List<XElement> str = new List<XElement>();
            List<XElement> tchild = room.Descendants("ChildAge").ToList();
            for (int i = 0; i < tchild.Count(); i++)
            {
                str.Add(new XElement("pax",
                       new XAttribute("type", "CH"),
                       new XAttribute("age", Convert.ToString(tchild[i].Value)))
                );
            }
            return str;
            #endregion
        }
        #endregion

        #region Hotel Detail Request
        public XElement HotelDetailHotelBeds(XElement req)
        {
            XElement hoteldetail=null;
            string hotelid = req.Descendants("HotelID").SingleOrDefault().Value;
            string endpoint = "https://api.test.hotelbeds.com/hotel-content-api/1.0/hotels/" + hotelid + "?language=ENG";

            string signature;
            using (var sha = SHA256.Create())
            {
                long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                Console.WriteLine("Timestamp: " + ts);
                var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(apiKey + Secret + ts));
                signature = BitConverter.ToString(computedHash).Replace("-", "");
            }

            string response = string.Empty;
            using (var client = new WebClient())
            {
                // Request configuration            
                client.Headers.Add("X-Signature", signature);
                client.Headers.Add("Api-Key", apiKey);
                client.Headers.Add("Accept", "application/xml");

                // Request execution
                //response = client.DownloadString(endpoint);

                HotelBeds_Detail htlhbstat = new HotelBeds_Detail();
                HotelBeds_HtStatic htlhbstaticdet = new HotelBeds_HtStatic();
                htlhbstat.HotelCode = req.Descendants("HotelID").SingleOrDefault().Value;
                DataTable dt = htlhbstaticdet.GetHotelDetail_HotelBeds(htlhbstat);
                XElement hoteldetailsresponse = null;
                if (dt.Rows.Count > 0)
                {
                    hoteldetailsresponse = XElement.Parse(dt.Rows[0]["HotelXML"].ToString());
                }
                //XElement hoteldetailsresponse = XElement.Parse(response.ToString());

                XElement doc = RemoveAllNamespaces(hoteldetailsresponse);

                XNamespace ns = "http://www.hotelbeds.com/schemas/messages";

                XElement hotelavailabilityres = doc.Descendants("hotel").FirstOrDefault();

                try
                {
                    if(hotelavailabilityres==null)
                    {
                        hotelavailabilityres = doc;
                    }
                }
                catch { }

                hoteldetail = hotelavailabilityres;

            }
            return hoteldetail;
        }
        #endregion

        #region HotelBeds Hotel Listing
        private IEnumerable<XElement> GetHotelListHotelBeds(List<XElement> hotel,DataTable dt)
        {
            XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
            #region HotelBeds Hotels
            List<XElement> hotellst = new List<XElement>();
            try
            {
                Int32 length = hotel.Count();

                try
                {
                    XElement staticdatahb = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\HotelBeds\hotelimage.xml"));
                    List<XElement> statichotellist = staticdatahb.Descendants("hotel").Where(x => x.Attribute("destinationCode").Value == reqTravayoo.Descendants("CityCode").Single().Value.ToString()).ToList();
                    List<XElement> htlst = staticdatahb.Descendants("hotel").ToList();

                    for (int i = 0; i < length; i++)
                    {
                        try
                        {
                            #region Fetch hotel
                            string starcategory = hotel[i].Attribute("categoryName").Value;
                            //string[] split = starcategory.Split(' ');
                            string star = string.Empty;
                            XElement hotelstatic = null;
                            List<XElement> fac = null;
                            string address = string.Empty;
                            string smallimagepath = string.Empty;
                            string largeimagepath = string.Empty;
                            decimal minRateHotel = 0;
                            List<XElement> rmtypelist = new List<XElement>();
                            try
                            {

                                //star = Convert.ToString(Convert.ToInt32(split[0]));
                                rmtypelist = hotel[i].Descendants("room").ToList();
                                minRateHotel = GetHotelMinRateHotelBeds(rmtypelist);

                                if (htlst.Count() > 0)
                                {
                                    try
                                    {
                                        IEnumerable<XElement> imgpth = htlst.Where(x => x.Attribute("code").Value == hotel[i].Attribute("code").Value);
                                        if (imgpth != null)
                                        {
                                            smallimagepath = "http://photos.hotelbeds.com/giata/medium/" + imgpth.Attributes("path").FirstOrDefault().Value;
                                            largeimagepath = "http://photos.hotelbeds.com/giata/bigger/" + imgpth.Attributes("path").FirstOrDefault().Value;
                                        }
                                    }
                                    catch { }
                                }
                                DataRow[] row = dt.Select("HotelCode = " + "'" + hotel[i].Attribute("code").Value + "'");
                                if (row.Count() > 0)
                                {
                                    try
                                    {
                                        hotelstatic = XElement.Parse(row[0]["HotelXML"].ToString());
                                        address = Convert.ToString(hotelstatic.Descendants("address").FirstOrDefault().Value);
                                        fac = hotelstatic.Descendants("facility").ToList();
                                    }
                                    catch { }
                                }
                                star = Regex.Replace(hotel[i].Attribute("categoryName").Value, "[^0-9]+", string.Empty);
                            }
                            catch (Exception ex) { star = ""; }
                            hotellst.Add(new XElement("Hotel",
                                                   new XElement("HotelID", Convert.ToString(hotel[i].Attribute("code").Value)),
                                                   new XElement("HotelName", Convert.ToString(hotel[i].Attribute("name").Value)),
                                                   new XElement("PropertyTypeName", Convert.ToString("")),
                                                   new XElement("CountryID", Convert.ToString("")),
                                                   new XElement("CountryName", Convert.ToString("")),
                                                   new XElement("CountryCode", Convert.ToString("")),
                                                   new XElement("CityId", Convert.ToString("")),
                                                   new XElement("CityCode", Convert.ToString(hotel[i].Attribute("destinationCode").Value)),
                                                   new XElement("CityName", Convert.ToString(hotel[i].Attribute("destinationName").Value)),
                                                   new XElement("AreaId", Convert.ToString(hotel[i].Attribute("zoneCode").Value)),
                                                   new XElement("AreaName", Convert.ToString(hotel[i].Attribute("zoneName").Value)),
                                                   new XElement("RequestID", Convert.ToString("")),
                                                   new XElement("Address", Convert.ToString(address)),
                                                   new XElement("Location", Convert.ToString("")),
                                                   new XElement("Description", Convert.ToString("")),
                                                   new XElement("StarRating", Convert.ToString(star)),
                                //new XElement("MinRate", Convert.ToString(hotel[i].Attribute("minRate").Value)),
                                                   new XElement("MinRate", Convert.ToString(minRateHotel)),
                                                   new XElement("HotelImgSmall", Convert.ToString(smallimagepath)),
                                                   new XElement("HotelImgLarge", Convert.ToString(largeimagepath)),
                                                   new XElement("MapLink", ""),
                                                   new XElement("Longitude", Convert.ToString(hotel[i].Attribute("longitude").Value)),
                                                   new XElement("Latitude", Convert.ToString(hotel[i].Attribute("latitude").Value)),
                                                   new XElement("DMC", "HotelBeds"),
                                                   new XElement("SupplierID", "4"),
                                                   new XElement("Currency", Convert.ToString(hotel[i].Attribute("currency").Value)),
                                                   new XElement("Offers", "")
                                                   , new XElement("Facilities", hotelfacilitiesHotelBeds(fac))
                                //new XElement("Facility", "No Facility Available"))
                                                   , new XElement("Rooms", ""
                                //GetHotelRoomListingHotelBeds(rmtypelist)
                                                       )
                            ));

                            #endregion
                        }
                        catch { }
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
        private IEnumerable<XElement> GetHotelListHotelBedsMerge(List<XElement> hotel,DataTable dt)
        {
            XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
            #region HotelBeds Hotels
            List<XElement> hotellst = new List<XElement>();
            string xmlouttype = string.Empty;
            try
            {
                if (dmc == "HotelBeds")
                {
                    xmlouttype = "false";
                }
                else
                { xmlouttype = "true"; }
            }
            catch { }
            try
            {
                Int32 length = hotel.Count();

                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        try
                        {
                            #region Fetch hotel
                            string starcategory = hotel[i].Attribute("categoryName").Value;
                            string star = string.Empty;
                            XElement hotelstatic = null;
                            string address = string.Empty;
                            string smallimagepath = string.Empty;
                            string largeimagepath = string.Empty;
                            decimal minRateHotel = 0;
                            List<XElement> rmtypelist = new List<XElement>();
                            try
                            {
                                rmtypelist = hotel[i].Descendants("room").ToList();
                                try
                                {
                                    //minRateHotel = GetHotelMinRateHotelBeds(rmtypelist);
                                    minRateHotel = minRateHB(rmtypelist);
                                }
                                catch { }
                                DataRow[] row = dt.Select("HotelCode = " + "'" + hotel[i].Attribute("code").Value + "'");
                                if (row.Count() > 0)
                                {
                                    try
                                    {
                                        hotelstatic = XElement.Parse(row[0]["HotelXML"].ToString());
                                        address = Convert.ToString(hotelstatic.Descendants("address").FirstOrDefault().Value);
                                        //fac = hotelstatic.Descendants("facility").ToList();
                                    }
                                    catch { }
                                    try
                                    {                                        
                                        try
                                        {
                                            if (hotelstatic.Descendants("image").Attributes("imageTypeCode").Count() > 0)
                                            {
                                                XElement imgpth = hotelstatic.Descendants("image").Where(x => x.Attribute("imageTypeCode").Value == "GEN").FirstOrDefault();
                                                //string pth = imgpth.Attribute("path").Value;
                                                if (imgpth != null)
                                                {
                                                    smallimagepath = "http://photos.hotelbeds.com/giata/medium/" + imgpth.Attribute("path").Value;
                                                    largeimagepath = "http://photos.hotelbeds.com/giata/bigger/" + imgpth.Attribute("path").Value;
                                                }
                                                else
                                                {
                                                    XElement imgpthne = hotelstatic.Descendants("image").FirstOrDefault();
                                                    if (imgpthne != null)
                                                    {
                                                        smallimagepath = "http://photos.hotelbeds.com/giata/medium/" + imgpthne.Attribute("path").Value;
                                                        largeimagepath = "http://photos.hotelbeds.com/giata/bigger/" + imgpthne.Attribute("path").Value;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                XElement imgpth = hotelstatic.Descendants("image").Where(x => x.Descendants("type").Attributes("code").FirstOrDefault().Value == "GEN").FirstOrDefault();
                                                //string pth = imgpth.Attribute("path").Value;
                                                if (imgpth != null)
                                                {
                                                    smallimagepath = "http://photos.hotelbeds.com/giata/medium/" + imgpth.Attribute("path").Value;
                                                    largeimagepath = "http://photos.hotelbeds.com/giata/bigger/" + imgpth.Attribute("path").Value;
                                                }
                                                else
                                                {
                                                    XElement imgpthol = hotelstatic.Descendants("image").FirstOrDefault();
                                                    if (imgpthol != null)
                                                    {
                                                        smallimagepath = "http://photos.hotelbeds.com/giata/medium/" + imgpthol.Attribute("path").Value;
                                                        largeimagepath = "http://photos.hotelbeds.com/giata/bigger/" + imgpthol.Attribute("path").Value;
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                    catch { }
                                }
                                //star = Regex.Replace(hotel[i].Attribute("categoryName").Value, "[^0-9]+", string.Empty);
                                //star = Convert.ToString(row[0]["res"].ToString());
                                try
                                {
                                    string star1 = "'1EST' ,'ALBER','APTH','CAMP1','H1_5','HS','HSR1','PENSI','1LL'";
                                    string star2 = "'2EST','2LL','APTH2','AT3','CAMP2','CHUES','H2S','H2_5','HR2','HS2','HSR2','LODGE','MINI','STD'";
                                    string star3 = "'3EST','3LL','APTH3','AT2','BB','BB3','H3S','H3_S','HR','HR3','HS3','RESID','SPC','H3_5'";
                                    string star4 = "'4EST','4LL','4LUX','AG','APTH4','AT1','BB4','BOU','H4_5','HR4','HRS','HS4','POUSA','RSORT','SUP','VILLA','VTV'";
                                    string star5 = "'5EST','5LL','5LUX','APTH5','BB5','H5_5','HIST','HR5','HS5'";
                                    if (star1.Contains(hotel[i].Attribute("categoryCode").Value))
                                    {
                                        star = "1";
                                    }
                                    else if (star2.Contains(hotel[i].Attribute("categoryCode").Value))
                                    {
                                        star = "2";
                                    }
                                    else if (star3.Contains(hotel[i].Attribute("categoryCode").Value))
                                    {
                                        star = "3";
                                    }
                                    else if (star4.Contains(hotel[i].Attribute("categoryCode").Value))
                                    {
                                        star = "4";
                                    }
                                    else if (star5.Contains(hotel[i].Attribute("categoryCode").Value))
                                    {
                                        star = "5";
                                    }
                                    else
                                    {
                                        star = Convert.ToString(row[0]["res"].ToString());
                                    }
                                }
                                catch { star = "0"; }
                            }                                
                            catch (Exception ex) { star = "0"; }
                            if (minRateHotel != 0)
                            { 
                                hotellst.Add(new XElement("Hotel",
                                                       new XElement("HotelID", Convert.ToString(hotel[i].Attribute("code").Value)),
                                                       new XElement("HotelName", Convert.ToString(hotel[i].Attribute("name").Value)),
                                                       new XElement("PropertyTypeName", Convert.ToString("")),
                                                       new XElement("CountryID", Convert.ToString("")),
                                                       new XElement("CountryName", Convert.ToString("")),
                                                       new XElement("CountryCode", Convert.ToString("")),
                                                       new XElement("CityId", Convert.ToString("")),
                                                       new XElement("CityCode", Convert.ToString(hotel[i].Attribute("destinationCode").Value)),
                                                       new XElement("CityName", Convert.ToString(hotel[i].Attribute("destinationName").Value)),
                                                       new XElement("AreaId", Convert.ToString(hotel[i].Attribute("zoneCode").Value)),
                                                       new XElement("AreaName", Convert.ToString(hotel[i].Attribute("zoneName").Value)),
                                                       new XElement("RequestID", Convert.ToString("")),
                                                       new XElement("Address", Convert.ToString(address)),
                                                       new XElement("Location", Convert.ToString("")),
                                                       new XElement("Description", Convert.ToString("")),
                                                       new XElement("StarRating", Convert.ToString(star)),
                                    //new XElement("MinRate", Convert.ToString(hotel[i].Attribute("minRate").Value)),
                                                       new XElement("MinRate", Convert.ToString(minRateHotel)),
                                                       new XElement("HotelImgSmall", Convert.ToString(smallimagepath)),
                                                       new XElement("HotelImgLarge", Convert.ToString(largeimagepath)),
                                                       new XElement("MapLink", ""),
                                                       new XElement("Longitude", Convert.ToString(hotel[i].Attribute("longitude").Value)),
                                                       new XElement("Latitude", Convert.ToString(hotel[i].Attribute("latitude").Value)),
                                                       new XElement("xmloutcustid", customerid),
                                                       new XElement("xmlouttype", xmlouttype),
                                                       new XElement("DMC", dmc),
                                                       new XElement("SupplierID", "4"),
                                                       new XElement("Currency", Convert.ToString(hotel[i].Attribute("currency").Value)),
                                                       new XElement("Offers", "")
                                                       , new XElement("Facilities", null)
                                                           //hotelfacilitiesHotelBeds(fac))
                                    // new XElement("Facility", "No Facility Available"))
                                                       , new XElement("Rooms", ""
                                    //GetHotelRoomListingHotelBeds(rmtypelist)
                                                           )
                                ));
                            }
                            #endregion
                        }
                        catch { }
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

        #region HotelBeds Hotel's Min Rate
        private decimal minRateHB(List<XElement> HbRooms)
        {
            var SupplGrouping = from rates in HbRooms.Descendants("rate")
                                group rates by new
                                {
                                    c1 = rates.Attribute("adults").Value,
                                    c2 = rates.Attribute("children").Value
                                };
            decimal amount = 0;
            foreach (var grp in SupplGrouping)
            {
                int roomCount = reqTravayoo.Descendants("RoomPax").Where(x => x.Element("Adult").Value.Equals(grp.Key.c1) && x.Element("Child").Value.Equals(grp.Key.c2)).Count();
                decimal min = grp.Select(x => Convert.ToDecimal(x.Attribute("net").Value)).Min();
                amount += min;
                amount = amount * roomCount;
            }
            return amount;
        }
        public decimal GetHotelMinRateHotelBeds(List<XElement> roomlist)
        {
            decimal minRate = 0;
            int minindx = 0;
            XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
            List<XElement> str = new List<XElement>();
            List<XElement> roomList1 = new List<XElement>();
            List<XElement> roomList2 = new List<XElement>();
            List<XElement> roomList3 = new List<XElement>();
            List<XElement> roomList4 = new List<XElement>();
            List<XElement> roomList5 = new List<XElement>();
            List<XElement> roomList6 = new List<XElement>();
            List<XElement> roomList7 = new List<XElement>();
            List<XElement> roomList8 = new List<XElement>();
            List<XElement> roomList9 = new List<XElement>();
            
            int totalroom = Convert.ToInt32(reqTravayoo.Descendants("RoomPax").Count());

            #region Notes: The maximum number of rooms that can be retrieved by a single search request is nine (9)
            #endregion

            try
            {

                #region Room Count 1
                if (totalroom == 1)
                {

                    #region Get Combination (Room 1)
                    List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild1 == "0")
                    {
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild1 == "1")
                    {
                        List<XElement> childage = reqTravayoo.Descendants("RoomPax").Descendants("ChildAge").ToList();
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children1ages = string.Empty;
                        List<XElement> child1age = reqTravayoo.Descendants("RoomPax").Descendants("ChildAge").ToList();
                        int totc1 = child1age.Count();
                        for (int i = 0; i < child1age.Count(); i++)
                        {
                            if (i == totc1 - 1)
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value);
                            }
                            else
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                            }
                        }
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                    }
                    #endregion
                    #endregion

                    for (int m = 0; m < roomList1.Count(); m++)
                    {
                        if (minindx == 0)
                        {
                            minRate = Convert.ToDecimal(roomList1[m].Attribute("net").Value);
                        }
                        if (Convert.ToDecimal(roomList1[m].Attribute("net").Value) < minRate)
                        {
                            minRate = Convert.ToDecimal(roomList1[m].Attribute("net").Value);
                        }
                        minindx++;
                    }
                    return minRate;
                }
                #endregion

                #region Room Count 2
                if (totalroom == 2)
                {
                    #region Get Combination (Room 1)
                    List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild1 == "0")
                    {
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild1 == "1")
                    {
                        List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children1ages = string.Empty;
                        List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        int totc1 = child1age.Count();
                        for (int i = 0; i < child1age.Count(); i++)
                        {
                            if (i == totc1 - 1)
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value);
                            }
                            else
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                            }
                        }
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 2)
                    List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild2 == "0")
                    {
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild2 == "1")
                    {
                        List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children2ages = string.Empty;
                        List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                        int totc2 = child2age.Count();
                        for (int i = 0; i < child2age.Count(); i++)
                        {
                            if (i == totc2 - 1)
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value);
                            }
                            else
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                            }
                        }
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                    }
                    #endregion
                    #endregion

                    //Parallel.For(0, roomList1.Count(), m =>
                    for (int m = 0; m < roomList1.Count(); m++)
                    {
                        //Parallel.For(0, roomList2.Count(), n =>
                        for (int n = 0; n < roomList2.Count(); n++)
                        {
                            string bb1 = roomList1[m].Attribute("boardCode").Value;
                            string bb2 = roomList2[n].Attribute("boardCode").Value;
                            if (bb1 == bb2)
                            {
                                decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value);
                                if (minindx == 0)
                                {
                                    minRate = totalrate;
                                }
                                if (totalrate < minRate)
                                {
                                    minRate = totalrate;
                                }
                                minindx++;
                            }
                        };
                    };
                    return minRate;
                }
                #endregion

                #region Room Count 3
                if (totalroom == 3)
                {

                    #region Get Combination (Room 1)
                    List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild1 == "0")
                    {
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild1 == "1")
                    {
                        List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children1ages = string.Empty;
                        List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        int totc1 = child1age.Count();
                        for (int i = 0; i < child1age.Count(); i++)
                        {
                            if (i == totc1 - 1)
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value);
                            }
                            else
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                            }
                        }
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 2)
                    List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild2 == "0")
                    {
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild2 == "1")
                    {
                        List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children2ages = string.Empty;
                        List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                        int totc2 = child2age.Count();
                        for (int i = 0; i < child2age.Count(); i++)
                        {
                            if (i == totc2 - 1)
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value);
                            }
                            else
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                            }
                        }
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 3)
                    List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild3 == "0")
                    {
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild3 == "1")
                    {
                        List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children3ages = string.Empty;
                        List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                        int totc3 = child3age.Count();
                        for (int i = 0; i < child3age.Count(); i++)
                        {
                            if (i == totc3 - 1)
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value);
                            }
                            else
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                            }
                        }
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    int group = 0;

                    #region Room 3
                    for (int m = 0; m < roomList1.Count(); m++)
                    {
                        for (int n = 0; n < roomList2.Count(); n++)
                        {
                            for (int o = 0; o < roomList3.Count(); o++)
                            {
                                // add room 1, 2, 3

                                string bb1 = roomList1[m].Attribute("boardCode").Value;
                                string bb2 = roomList2[n].Attribute("boardCode").Value;
                                string bb3 = roomList3[o].Attribute("boardCode").Value;
                                if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3)
                                {
                                    decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value);
                                    if (minindx == 0)
                                    {
                                        minRate = totalrate;
                                    }
                                    if (totalrate < minRate)
                                    {
                                        minRate = totalrate;
                                    }
                                    minindx++;
                                }
                            }
                        }
                    }
                    return minRate;
                    #endregion
                }
                #endregion

                #region Room Count 4
                if (totalroom == 4)
                {

                    #region Get Combination (Room 1)
                    List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild1 == "0")
                    {
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild1 == "1")
                    {
                        List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children1ages = string.Empty;
                        List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        int totc1 = child1age.Count();
                        for (int i = 0; i < child1age.Count(); i++)
                        {
                            if (i == totc1 - 1)
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value);
                            }
                            else
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                            }
                        }
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 2)
                    List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild2 == "0")
                    {
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild2 == "1")
                    {
                        List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children2ages = string.Empty;
                        List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                        int totc2 = child2age.Count();
                        for (int i = 0; i < child2age.Count(); i++)
                        {
                            if (i == totc2 - 1)
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value);
                            }
                            else
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                            }
                        }
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 3)
                    List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild3 == "0")
                    {
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild3 == "1")
                    {
                        List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children3ages = string.Empty;
                        List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                        int totc3 = child3age.Count();
                        for (int i = 0; i < child3age.Count(); i++)
                        {
                            if (i == totc3 - 1)
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value);
                            }
                            else
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                            }
                        }
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 4)
                    List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild4 == "0")
                    {
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild4 == "1")
                    {
                        List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children4ages = string.Empty;
                        List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                        int totc4 = child4age.Count();
                        for (int i = 0; i < child4age.Count(); i++)
                        {
                            if (i == totc4 - 1)
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value);
                            }
                            else
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                            }
                        }
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    int group = 0;
                    #region Room 4
                    //Parallel.For(0, roomList1.Count(), m =>
                    for (int m = 0; m < roomList1.Count(); m++)
                    {
                        //Parallel.For(0, roomList2.Count(), n =>
                        for (int n = 0; n < roomList2.Count(); n++)
                        {
                            //Parallel.For(0, roomList3.Count(), o =>
                            for (int o = 0; o < roomList3.Count(); o++)
                            {
                                //Parallel.For(0, roomList4.Count(), p =>
                                for (int p = 0; p < roomList4.Count(); p++)
                                {
                                    // add room 1, 2, 3,4

                                    string bb1 = roomList1[m].Attribute("boardCode").Value;
                                    string bb2 = roomList2[n].Attribute("boardCode").Value;
                                    string bb3 = roomList3[o].Attribute("boardCode").Value;
                                    string bb4 = roomList4[p].Attribute("boardCode").Value;
                                    if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4)
                                    {
                                        group++;
                                        decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value);
                                        if (minindx == 0)
                                        {
                                            minRate = totalrate;
                                        }
                                        if (totalrate < minRate)
                                        {
                                            minRate = totalrate;
                                        }
                                        if (group > 550)
                                        {
                                            return minRate;
                                        }
                                        minindx++;

                                    }
                                }
                            }
                        }
                    }
                    return minRate;
                    #endregion
                }
                #endregion

                #region Room Count 5
                if (totalroom == 5)
                {

                    #region Get Combination (Room 1)
                    List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild1 == "0")
                    {
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild1 == "1")
                    {
                        List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children1ages = string.Empty;
                        List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        int totc1 = child1age.Count();
                        for (int i = 0; i < child1age.Count(); i++)
                        {
                            if (i == totc1 - 1)
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value);
                            }
                            else
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                            }
                        }
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 2)
                    List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild2 == "0")
                    {
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild2 == "1")
                    {
                        List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children2ages = string.Empty;
                        List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                        int totc2 = child2age.Count();
                        for (int i = 0; i < child2age.Count(); i++)
                        {
                            if (i == totc2 - 1)
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value);
                            }
                            else
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                            }
                        }
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 3)
                    List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild3 == "0")
                    {
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild3 == "1")
                    {
                        List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children3ages = string.Empty;
                        List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                        int totc3 = child3age.Count();
                        for (int i = 0; i < child3age.Count(); i++)
                        {
                            if (i == totc3 - 1)
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value);
                            }
                            else
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                            }
                        }
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 4)
                    List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild4 == "0")
                    {
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild4 == "1")
                    {
                        List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children4ages = string.Empty;
                        List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                        int totc4 = child4age.Count();
                        for (int i = 0; i < child4age.Count(); i++)
                        {
                            if (i == totc4 - 1)
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value);
                            }
                            else
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                            }
                        }
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 5)
                    List<XElement> room5child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild5 == "0")
                    {
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild5 == "1")
                    {
                        List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children5ages = string.Empty;
                        List<XElement> child5age = room5child[4].Descendants("ChildAge").ToList();
                        int totc5 = child5age.Count();
                        for (int i = 0; i < child5age.Count(); i++)
                        {
                            if (i == totc5 - 1)
                            {
                                children5ages = children5ages + Convert.ToString(child5age[i].Value);
                            }
                            else
                            {
                                children5ages = children5ages + Convert.ToString(child5age[i].Value) + ",";
                            }
                        }
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false).ToList();
                    }
                    #endregion
                    #endregion

                    #region Room 5
                    //Parallel.For(0, roomList1.Count(), m =>
                    for (int m = 0; m < roomList1.Count(); m++)
                    {
                        //Parallel.For(0, roomList2.Count(), n =>
                        for (int n = 0; n < roomList2.Count(); n++)
                        {
                            //Parallel.For(0, roomList3.Count(), o =>
                            for (int o = 0; o < roomList3.Count(); o++)
                            {
                                //Parallel.For(0, roomList4.Count(), p =>
                                for (int p = 0; p < roomList4.Count(); p++)
                                {
                                    //Parallel.For(0, roomList5.Count(), q =>
                                    for (int q = 0; q < roomList5.Count(); q++)
                                    {
                                        // add room 1, 2, 3,4,5

                                        string bb1 = roomList1[m].Attribute("boardCode").Value;
                                        string bb2 = roomList2[n].Attribute("boardCode").Value;
                                        string bb3 = roomList3[o].Attribute("boardCode").Value;
                                        string bb4 = roomList4[p].Attribute("boardCode").Value;
                                        string bb5 = roomList5[q].Attribute("boardCode").Value;
                                        if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4 && bb1 == bb5 && bb2 == bb5 && bb3 == bb5 && bb4 == bb5)
                                        {
                                            //int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                            //if (totalroom <= totalallots)
                                            {
                                                decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value);
                                                if (minindx == 0)
                                                {
                                                    minRate = totalrate;
                                                    return minRate;
                                                }
                                                if (totalrate < minRate)
                                                {
                                                    minRate = totalrate;
                                                }
                                                minindx++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return minRate;
                    #endregion
                }
                #endregion

                #region Room Count 6
                if (totalroom == 6)
                {

                    #region Get Combination (Room 1)
                    List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild1 == "0")
                    {
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild1 == "1")
                    {
                        List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children1ages = string.Empty;
                        List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        int totc1 = child1age.Count();
                        for (int i = 0; i < child1age.Count(); i++)
                        {
                            if (i == totc1 - 1)
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value);
                            }
                            else
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                            }
                        }
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 2)
                    List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild2 == "0")
                    {
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild2 == "1")
                    {
                        List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children2ages = string.Empty;
                        List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                        int totc2 = child2age.Count();
                        for (int i = 0; i < child2age.Count(); i++)
                        {
                            if (i == totc2 - 1)
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value);
                            }
                            else
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                            }
                        }
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 3)
                    List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild3 == "0")
                    {
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild3 == "1")
                    {
                        List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children3ages = string.Empty;
                        List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                        int totc3 = child3age.Count();
                        for (int i = 0; i < child3age.Count(); i++)
                        {
                            if (i == totc3 - 1)
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value);
                            }
                            else
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                            }
                        }
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 4)
                    List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild4 == "0")
                    {
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild4 == "1")
                    {
                        List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children4ages = string.Empty;
                        List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                        int totc4 = child4age.Count();
                        for (int i = 0; i < child4age.Count(); i++)
                        {
                            if (i == totc4 - 1)
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value);
                            }
                            else
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                            }
                        }
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 5)
                    List<XElement> room5child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild5 == "0")
                    {
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild5 == "1")
                    {
                        List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children5ages = string.Empty;
                        List<XElement> child5age = room5child[4].Descendants("ChildAge").ToList();
                        int totc5 = child5age.Count();
                        for (int i = 0; i < child5age.Count(); i++)
                        {
                            if (i == totc5 - 1)
                            {
                                children5ages = children5ages + Convert.ToString(child5age[i].Value);
                            }
                            else
                            {
                                children5ages = children5ages + Convert.ToString(child5age[i].Value) + ",";
                            }
                        }
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 6)
                    List<XElement> room6child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild6 == "0")
                    {
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild6 == "1")
                    {
                        List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children6ages = string.Empty;
                        List<XElement> child6age = room6child[5].Descendants("ChildAge").ToList();
                        int totc6 = child6age.Count();
                        for (int i = 0; i < child6age.Count(); i++)
                        {
                            if (i == totc6 - 1)
                            {
                                children6ages = children6ages + Convert.ToString(child6age[i].Value);
                            }
                            else
                            {
                                children6ages = children6ages + Convert.ToString(child6age[i].Value) + ",";
                            }
                        }
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false).ToList();
                    }
                    #endregion
                    #endregion

                    #region Room 6
                    //Parallel.For(0, roomList1.Count(), m =>
                    for (int m = 0; m < roomList1.Count(); m++)
                    {
                        //Parallel.For(0, roomList2.Count(), n =>
                        for (int n = 0; n < roomList2.Count(); n++)
                        {
                            //Parallel.For(0, roomList3.Count(), o =>
                            for (int o = 0; o < roomList3.Count(); o++)
                            {
                                //Parallel.For(0, roomList4.Count(), p =>
                                for (int p = 0; p < roomList4.Count(); p++)
                                {
                                    //Parallel.For(0, roomList5.Count(), q =>
                                    for (int q = 0; q < roomList5.Count(); q++)
                                    {
                                        //Parallel.For(0, roomList6.Count(), r =>
                                        for (int r = 0; r < roomList6.Count(); r++)
                                        {
                                            // add room 1, 2, 3,4,5,6 

                                            string bb1 = roomList1[m].Attribute("boardCode").Value;
                                            string bb2 = roomList2[n].Attribute("boardCode").Value;
                                            string bb3 = roomList3[o].Attribute("boardCode").Value;
                                            string bb4 = roomList4[p].Attribute("boardCode").Value;
                                            string bb5 = roomList5[q].Attribute("boardCode").Value;
                                            string bb6 = roomList6[r].Attribute("boardCode").Value;
                                            if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4 && bb1 == bb5 && bb2 == bb5 && bb3 == bb5 && bb4 == bb5
                                                && bb1 == bb6 && bb2 == bb6 && bb3 == bb6 && bb4 == bb6 && bb5 == bb6)
                                            {
                                                 //int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                                 //if (totalroom <= totalallots)
                                                 {
                                                     decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value);
                                                     if (minindx == 0)
                                                     {
                                                         minRate = totalrate;
                                                         return minRate;
                                                     }
                                                     if (totalrate < minRate)
                                                     {
                                                         minRate = totalrate;
                                                     }
                                                     minindx++;
                                                 }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return minRate;
                    #endregion
                }
                #endregion

                #region Room Count 7
                if (totalroom == 7)
                {

                    #region Get Combination (Room 1)
                    List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild1 == "0")
                    {
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild1 == "1")
                    {
                        List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children1ages = string.Empty;
                        List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        int totc1 = child1age.Count();
                        for (int i = 0; i < child1age.Count(); i++)
                        {
                            if (i == totc1 - 1)
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value);
                            }
                            else
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                            }
                        }
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 2)
                    List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild2 == "0")
                    {
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild2 == "1")
                    {
                        List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children2ages = string.Empty;
                        List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                        int totc2 = child2age.Count();
                        for (int i = 0; i < child2age.Count(); i++)
                        {
                            if (i == totc2 - 1)
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value);
                            }
                            else
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                            }
                        }
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 3)
                    List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild3 == "0")
                    {
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild3 == "1")
                    {
                        List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children3ages = string.Empty;
                        List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                        int totc3 = child3age.Count();
                        for (int i = 0; i < child3age.Count(); i++)
                        {
                            if (i == totc3 - 1)
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value);
                            }
                            else
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                            }
                        }
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 4)
                    List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild4 == "0")
                    {
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild4 == "1")
                    {
                        List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children4ages = string.Empty;
                        List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                        int totc4 = child4age.Count();
                        for (int i = 0; i < child4age.Count(); i++)
                        {
                            if (i == totc4 - 1)
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value);
                            }
                            else
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                            }
                        }
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 5)
                    List<XElement> room5child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild5 == "0")
                    {
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild5 == "1")
                    {
                        List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children5ages = string.Empty;
                        List<XElement> child5age = room5child[4].Descendants("ChildAge").ToList();
                        int totc5 = child5age.Count();
                        for (int i = 0; i < child5age.Count(); i++)
                        {
                            if (i == totc5 - 1)
                            {
                                children5ages = children5ages + Convert.ToString(child5age[i].Value);
                            }
                            else
                            {
                                children5ages = children5ages + Convert.ToString(child5age[i].Value) + ",";
                            }
                        }
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 6)
                    List<XElement> room6child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild6 == "0")
                    {
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild6 == "1")
                    {
                        List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children6ages = string.Empty;
                        List<XElement> child6age = room6child[5].Descendants("ChildAge").ToList();
                        int totc6 = child6age.Count();
                        for (int i = 0; i < child6age.Count(); i++)
                        {
                            if (i == totc6 - 1)
                            {
                                children6ages = children6ages + Convert.ToString(child6age[i].Value);
                            }
                            else
                            {
                                children6ages = children6ages + Convert.ToString(child6age[i].Value) + ",";
                            }
                        }
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 7)
                    List<XElement> room7child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild7 = room7child[6].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild7 == "0")
                    {
                        roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild7 == "1")
                    {
                        List<XElement> childage = room7child[6].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children7ages = string.Empty;
                        List<XElement> child7age = room7child[6].Descendants("ChildAge").ToList();
                        int totc7 = child7age.Count();
                        for (int i = 0; i < child7age.Count(); i++)
                        {
                            if (i == totc7 - 1)
                            {
                                children7ages = children7ages + Convert.ToString(child7age[i].Value);
                            }
                            else
                            {
                                children7ages = children7ages + Convert.ToString(child7age[i].Value) + ",";
                            }
                        }
                        roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children7ages) : false).ToList();
                    }
                    #endregion
                    #endregion

                    #region Room 7
                    //Parallel.For(0, roomList1.Count(), m =>
                    for (int m = 0; m < roomList1.Count(); m++)
                    {
                        //Parallel.For(0, roomList2.Count(), n =>
                        for (int n = 0; n < roomList2.Count(); n++)
                        {
                            //Parallel.For(0, roomList3.Count(), o =>
                            for (int o = 0; o < roomList3.Count(); o++)
                            {
                                //Parallel.For(0, roomList4.Count(), p =>
                                for (int p = 0; p < roomList4.Count(); p++)
                                {
                                    //Parallel.For(0, roomList5.Count(), q =>
                                    for (int q = 0; q < roomList5.Count(); q++)
                                    {
                                        //Parallel.For(0, roomList6.Count(), r =>
                                        for (int r = 0; r < roomList6.Count(); r++)
                                        {
                                            //Parallel.For(0, roomList7.Count(), s =>
                                            for (int s = 0; s < roomList7.Count(); s++)
                                            {
                                                // add room 1, 2, 3,4,5,6,7

                                                string bb1 = roomList1[m].Attribute("boardCode").Value;
                                                string bb2 = roomList2[n].Attribute("boardCode").Value;
                                                string bb3 = roomList3[o].Attribute("boardCode").Value;
                                                string bb4 = roomList4[p].Attribute("boardCode").Value;
                                                string bb5 = roomList5[q].Attribute("boardCode").Value;
                                                string bb6 = roomList6[r].Attribute("boardCode").Value;
                                                string bb7 = roomList7[s].Attribute("boardCode").Value;
                                                if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4 && bb1 == bb5 && bb2 == bb5 && bb3 == bb5 && bb4 == bb5
                                                    && bb1 == bb6 && bb2 == bb6 && bb3 == bb6 && bb4 == bb6 && bb5 == bb6
                                                    && bb1 == bb7 && bb2 == bb7 && bb3 == bb7 && bb4 == bb7 && bb5 == bb7 && bb6 == bb7)
                                                {
                                                    //int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                                    //if (totalroom <= totalallots)
                                                    {
                                                        decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value) + Convert.ToDecimal(roomList7[s].Attribute("net").Value);
                                                        if (minindx == 0)
                                                        {
                                                            minRate = totalrate;
                                                            return minRate;
                                                        }
                                                        if (totalrate < minRate)
                                                        {
                                                            minRate = totalrate;
                                                        }
                                                        minindx++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return minRate;
                    #endregion
                }
                #endregion

                #region Room Count 8
                if (totalroom == 8)
                {

                    #region Get Combination (Room 1)
                    List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild1 == "0")
                    {
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild1 == "1")
                    {
                        List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children1ages = string.Empty;
                        List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        int totc1 = child1age.Count();
                        for (int i = 0; i < child1age.Count(); i++)
                        {
                            if (i == totc1 - 1)
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value);
                            }
                            else
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                            }
                        }
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 2)
                    List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild2 == "0")
                    {
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild2 == "1")
                    {
                        List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children2ages = string.Empty;
                        List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                        int totc2 = child2age.Count();
                        for (int i = 0; i < child2age.Count(); i++)
                        {
                            if (i == totc2 - 1)
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value);
                            }
                            else
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                            }
                        }
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 3)
                    List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild3 == "0")
                    {
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild3 == "1")
                    {
                        List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children3ages = string.Empty;
                        List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                        int totc3 = child3age.Count();
                        for (int i = 0; i < child3age.Count(); i++)
                        {
                            if (i == totc3 - 1)
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value);
                            }
                            else
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                            }
                        }
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 4)
                    List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild4 == "0")
                    {
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild4 == "1")
                    {
                        List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children4ages = string.Empty;
                        List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                        int totc4 = child4age.Count();
                        for (int i = 0; i < child4age.Count(); i++)
                        {
                            if (i == totc4 - 1)
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value);
                            }
                            else
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                            }
                        }
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 5)
                    List<XElement> room5child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild5 == "0")
                    {
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild5 == "1")
                    {
                        List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children5ages = string.Empty;
                        List<XElement> child5age = room5child[4].Descendants("ChildAge").ToList();
                        int totc5 = child5age.Count();
                        for (int i = 0; i < child5age.Count(); i++)
                        {
                            if (i == totc5 - 1)
                            {
                                children5ages = children5ages + Convert.ToString(child5age[i].Value);
                            }
                            else
                            {
                                children5ages = children5ages + Convert.ToString(child5age[i].Value) + ",";
                            }
                        }
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 6)
                    List<XElement> room6child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild6 == "0")
                    {
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild6 == "1")
                    {
                        List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children6ages = string.Empty;
                        List<XElement> child6age = room6child[5].Descendants("ChildAge").ToList();
                        int totc6 = child6age.Count();
                        for (int i = 0; i < child6age.Count(); i++)
                        {
                            if (i == totc6 - 1)
                            {
                                children6ages = children6ages + Convert.ToString(child6age[i].Value);
                            }
                            else
                            {
                                children6ages = children6ages + Convert.ToString(child6age[i].Value) + ",";
                            }
                        }
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 7)
                    List<XElement> room7child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild7 = room7child[6].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild7 == "0")
                    {
                        roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild7 == "1")
                    {
                        List<XElement> childage = room7child[6].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children7ages = string.Empty;
                        List<XElement> child7age = room7child[6].Descendants("ChildAge").ToList();
                        int totc7 = child7age.Count();
                        for (int i = 0; i < child7age.Count(); i++)
                        {
                            if (i == totc7 - 1)
                            {
                                children7ages = children7ages + Convert.ToString(child7age[i].Value);
                            }
                            else
                            {
                                children7ages = children7ages + Convert.ToString(child7age[i].Value) + ",";
                            }
                        }
                        roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children7ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 8)
                    List<XElement> room8child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild8 = room8child[7].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild8 == "0")
                    {
                        roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild8 == "1")
                    {
                        List<XElement> childage = room8child[7].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children8ages = string.Empty;
                        List<XElement> child8age = room8child[7].Descendants("ChildAge").ToList();
                        int totc8 = child8age.Count();
                        for (int i = 0; i < child8age.Count(); i++)
                        {
                            if (i == totc8 - 1)
                            {
                                children8ages = children8ages + Convert.ToString(child8age[i].Value);
                            }
                            else
                            {
                                children8ages = children8ages + Convert.ToString(child8age[i].Value) + ",";
                            }
                        }
                        roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children8ages) : false).ToList();
                    }
                    #endregion
                    #endregion

                    #region Room 8
                    //Parallel.For(0, roomList1.Count(), m =>
                    for (int m = 0; m < roomList1.Count(); m++)
                    {
                        //Parallel.For(0, roomList2.Count(), n =>
                        for (int n = 0; n < roomList2.Count(); n++)
                        {
                            //Parallel.For(0, roomList3.Count(), o =>
                            for (int o = 0; o < roomList3.Count(); o++)
                            {
                                //Parallel.For(0, roomList4.Count(), p =>
                                for (int p = 0; p < roomList4.Count(); p++)
                                {
                                    //Parallel.For(0, roomList5.Count(), q =>
                                    for (int q = 0; q < roomList5.Count(); q++)
                                    {
                                        for (int r = 0; r < roomList6.Count(); r++)
                                        {
                                            for (int s = 0; s < roomList7.Count(); s++)
                                            {
                                                for (int t = 0; t < roomList8.Count(); t++)
                                                {
                                                    // add room 1, 2, 3,4,5,6,7,8

                                                    string bb1 = roomList1[m].Attribute("boardCode").Value;
                                                    string bb2 = roomList2[n].Attribute("boardCode").Value;
                                                    string bb3 = roomList3[o].Attribute("boardCode").Value;
                                                    string bb4 = roomList4[p].Attribute("boardCode").Value;
                                                    string bb5 = roomList5[q].Attribute("boardCode").Value;
                                                    string bb6 = roomList6[r].Attribute("boardCode").Value;
                                                    string bb7 = roomList7[s].Attribute("boardCode").Value;
                                                    string bb8 = roomList8[t].Attribute("boardCode").Value;
                                                    if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4 && bb1 == bb5 && bb2 == bb5 && bb3 == bb5 && bb4 == bb5
                                                        && bb1 == bb6 && bb2 == bb6 && bb3 == bb6 && bb4 == bb6 && bb5 == bb6
                                                        && bb1 == bb7 && bb2 == bb7 && bb3 == bb7 && bb4 == bb7 && bb5 == bb7 && bb6 == bb7
                                                        && bb1 == bb8 && bb2 == bb8 && bb3 == bb8 && bb4 == bb8 && bb5 == bb8 && bb6 == bb8 && bb7 == bb8)
                                                    {
                                                        //int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));
                                                        //if (totalroom <= totalallots)
                                                        {
                                                            decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value) + Convert.ToDecimal(roomList7[s].Attribute("net").Value) + Convert.ToDecimal(roomList8[t].Attribute("net").Value);
                                                            if (minindx == 0)
                                                            {
                                                                minRate = totalrate;
                                                                return minRate;
                                                            }
                                                            if (totalrate < minRate)
                                                            {
                                                                minRate = totalrate;
                                                            }
                                                            minindx++;
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
                    return minRate;
                    #endregion
                }
                #endregion

                #region Room Count 9
                if (totalroom == 9)
                {

                    #region Get Combination (Room 1)
                    List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild1 == "0")
                    {
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild1 == "1")
                    {
                        List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children1ages = string.Empty;
                        List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                        int totc1 = child1age.Count();
                        for (int i = 0; i < child1age.Count(); i++)
                        {
                            if (i == totc1 - 1)
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value);
                            }
                            else
                            {
                                children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                            }
                        }
                        //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                        roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children1ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 2)
                    List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild2 == "0")
                    {
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild2 == "1")
                    {
                        List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children2ages = string.Empty;
                        List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                        int totc2 = child2age.Count();
                        for (int i = 0; i < child2age.Count(); i++)
                        {
                            if (i == totc2 - 1)
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value);
                            }
                            else
                            {
                                children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                            }
                        }
                        //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                        roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children2ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 3)
                    List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild3 == "0")
                    {
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild3 == "1")
                    {
                        List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children3ages = string.Empty;
                        List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                        int totc3 = child3age.Count();
                        for (int i = 0; i < child3age.Count(); i++)
                        {
                            if (i == totc3 - 1)
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value);
                            }
                            else
                            {
                                children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                            }
                        }
                        //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                        roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children3ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 4)
                    List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild4 == "0")
                    {
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild4 == "1")
                    {
                        List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children4ages = string.Empty;
                        List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                        int totc4 = child4age.Count();
                        for (int i = 0; i < child4age.Count(); i++)
                        {
                            if (i == totc4 - 1)
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value);
                            }
                            else
                            {
                                children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                            }
                        }
                        roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children4ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 5)
                    List<XElement> room5child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild5 = room5child[4].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild5 == "0")
                    {
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild5 == "1")
                    {
                        List<XElement> childage = room5child[4].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children5ages = string.Empty;
                        List<XElement> child5age = room5child[4].Descendants("ChildAge").ToList();
                        int totc5 = child5age.Count();
                        for (int i = 0; i < child5age.Count(); i++)
                        {
                            if (i == totc5 - 1)
                            {
                                children5ages = children5ages + Convert.ToString(child5age[i].Value);
                            }
                            else
                            {
                                children5ages = children5ages + Convert.ToString(child5age[i].Value) + ",";
                            }
                        }
                        roomList5 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room5child[4].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children5ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 6)
                    List<XElement> room6child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild6 = room6child[5].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild6 == "0")
                    {
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild6 == "1")
                    {
                        List<XElement> childage = room6child[5].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children6ages = string.Empty;
                        List<XElement> child6age = room6child[5].Descendants("ChildAge").ToList();
                        int totc6 = child6age.Count();
                        for (int i = 0; i < child6age.Count(); i++)
                        {
                            if (i == totc6 - 1)
                            {
                                children6ages = children6ages + Convert.ToString(child6age[i].Value);
                            }
                            else
                            {
                                children6ages = children6ages + Convert.ToString(child6age[i].Value) + ",";
                            }
                        }
                        roomList6 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room6child[5].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children6ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 7)
                    List<XElement> room7child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild7 = room7child[6].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild7 == "0")
                    {
                        roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild7 == "1")
                    {
                        List<XElement> childage = room7child[6].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children7ages = string.Empty;
                        List<XElement> child7age = room7child[6].Descendants("ChildAge").ToList();
                        int totc7 = child7age.Count();
                        for (int i = 0; i < child7age.Count(); i++)
                        {
                            if (i == totc7 - 1)
                            {
                                children7ages = children7ages + Convert.ToString(child7age[i].Value);
                            }
                            else
                            {
                                children7ages = children7ages + Convert.ToString(child7age[i].Value) + ",";
                            }
                        }
                        roomList7 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room7child[6].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children7ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 8)
                    List<XElement> room8child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild8 = room8child[7].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild8 == "0")
                    {
                        roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild8 == "1")
                    {
                        List<XElement> childage = room8child[7].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children8ages = string.Empty;
                        List<XElement> child8age = room8child[7].Descendants("ChildAge").ToList();
                        int totc8 = child8age.Count();
                        for (int i = 0; i < child8age.Count(); i++)
                        {
                            if (i == totc8 - 1)
                            {
                                children8ages = children8ages + Convert.ToString(child8age[i].Value);
                            }
                            else
                            {
                                children8ages = children8ages + Convert.ToString(child8age[i].Value) + ",";
                            }
                        }
                        roomList8 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room8child[7].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children8ages) : false).ToList();
                    }
                    #endregion
                    #endregion
                    #region Get Combination (Room 9)
                    List<XElement> room9child = reqTravayoo.Descendants("RoomPax").ToList();
                    string totalchild9 = room9child[8].Descendants("Child").FirstOrDefault().Value;
                    #region if total children 0
                    if (totalchild9 == "0")
                    {
                        roomList9 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room9child[8].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                    }
                    #endregion
                    #region if total children 1
                    else if (totalchild9 == "1")
                    {
                        List<XElement> childage = room9child[8].Descendants("ChildAge").ToList();
                        //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                        roomList9 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room9child[8].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(childage[0].Value) : false).ToList();
                    }
                    #endregion
                    #region if total children >1
                    else
                    {
                        string children9ages = string.Empty;
                        List<XElement> child9age = room9child[8].Descendants("ChildAge").ToList();
                        int totc9 = child9age.Count();
                        for (int i = 0; i < child9age.Count(); i++)
                        {
                            if (i == totc9 - 1)
                            {
                                children9ages = children9ages + Convert.ToString(child9age[i].Value);
                            }
                            else
                            {
                                children9ages = children9ages + Convert.ToString(child9age[i].Value) + ",";
                            }
                        }
                        roomList9 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room9child[8].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges") != null ? el.Attribute("childrenAges").Value.Equals(children9ages) : false).ToList();
                    }
                    #endregion
                    #endregion

                    #region Room 9
                    //Parallel.For(0, roomList1.Count(), m =>
                    for (int m = 0; m < roomList1.Count(); m++)
                    {
                        //Parallel.For(0, roomList2.Count(), n =>
                        for (int n = 0; n < roomList2.Count(); n++)
                        {
                            //Parallel.For(0, roomList3.Count(), o =>
                            for (int o = 0; o < roomList3.Count(); o++)
                            {
                                //Parallel.For(0, roomList4.Count(), p =>
                                for (int p = 0; p < roomList4.Count(); p++)
                                {
                                    //Parallel.For(0, roomList5.Count(), q =>
                                    for (int q = 0; q < roomList5.Count(); q++)
                                    {
                                        for (int r = 0; r < roomList6.Count(); r++)
                                        {
                                            for (int s = 0; s < roomList7.Count(); s++)
                                            {
                                                for (int t = 0; t < roomList8.Count(); t++)
                                                {
                                                    for (int u = 0; u < roomList9.Count(); u++)
                                                    {
                                                        // add room 1, 2, 3,4,5,6,7,8,9

                                                        string bb1 = roomList1[m].Attribute("boardCode").Value;
                                                        string bb2 = roomList2[n].Attribute("boardCode").Value;
                                                        string bb3 = roomList3[o].Attribute("boardCode").Value;
                                                        string bb4 = roomList4[p].Attribute("boardCode").Value;
                                                        string bb5 = roomList5[q].Attribute("boardCode").Value;
                                                        string bb6 = roomList6[r].Attribute("boardCode").Value;
                                                        string bb7 = roomList7[s].Attribute("boardCode").Value;
                                                        string bb8 = roomList8[t].Attribute("boardCode").Value;
                                                        string bb9 = roomList9[u].Attribute("boardCode").Value;
                                                        if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4 && bb1 == bb5 && bb2 == bb5 && bb3 == bb5 && bb4 == bb5
                                                            && bb1 == bb6 && bb2 == bb6 && bb3 == bb6 && bb4 == bb6 && bb5 == bb6
                                                            && bb1 == bb7 && bb2 == bb7 && bb3 == bb7 && bb4 == bb7 && bb5 == bb7 && bb6 == bb7
                                                            && bb1 == bb8 && bb2 == bb8 && bb3 == bb8 && bb4 == bb8 && bb5 == bb8 && bb6 == bb8 && bb7 == bb8
                                                            && bb1 == bb9 && bb2 == bb9 && bb3 == bb9 && bb4 == bb9 && bb5 == bb9 && bb6 == bb9 && bb7 == bb9 && bb8 == bb9)
                                                        {
                                                            //int totalallots = roomlist.Descendants("rate").Where(x => x.Attribute("boardCode").Value == bb1).Attributes("allotment").Sum(e => int.Parse(e.Value));                                                           
                                                            //if (totalroom <= totalallots)
                                                            {
                                                                decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value) + Convert.ToDecimal(roomList5[q].Attribute("net").Value) + Convert.ToDecimal(roomList6[r].Attribute("net").Value) + Convert.ToDecimal(roomList7[s].Attribute("net").Value) + Convert.ToDecimal(roomList8[t].Attribute("net").Value) + Convert.ToDecimal(roomList9[u].Attribute("net").Value);
                                                                if (minindx == 0)
                                                                {
                                                                    minRate = totalrate;
                                                                    return minRate;
                                                                }
                                                                if (totalrate < minRate)
                                                                {
                                                                    minRate = totalrate;
                                                                }
                                                                minindx++;
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
                    return minRate;
                    #endregion
                }
                #endregion
            }
            catch { }

            return minRate;
        }
        #endregion

        #region HotelBeds Hotel's Room Listing
        public IEnumerable<XElement> GetHotelRoomListingHotelBeds(List<XElement> roomlist)
        {
            XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
            List<XElement> str = new List<XElement>();
            List<XElement> roomList1 = new List<XElement>();
            List<XElement> roomList2 = new List<XElement>();
            List<XElement> roomList3 = new List<XElement>();
            List<XElement> roomList4 = new List<XElement>();
            List<XElement> roomList5 = new List<XElement>();

            int totalroom = Convert.ToInt32(reqTravayoo.Descendants("RoomPax").Count());

            #region Notes: The maximum number of rooms that can be retrieved by a single search request is nine (9)
            #endregion

            #region Room Count 1
            if (totalroom == 1)
            {
                //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "1").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion

                for (int m = 0; m < roomList1.Count(); m++)
                {
                    List<XElement> pricebrkups = roomList1[m].Descendants("dailyRate").ToList();
                    List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();
                    List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();

                    #region With Board Bases
                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].Attribute("net").Value), new XAttribute("Index", m + 1),
                        new XElement("Room",
                             new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                             new XAttribute("SuppliersID", "4"),
                             new XAttribute("RoomSeq", "1"),
                             new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                             new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                             new XAttribute("OccupancyID", Convert.ToString("")),
                             new XAttribute("OccupancyName", Convert.ToString("")),
                             new XAttribute("MealPlanID", Convert.ToString("")),
                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                             new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                             new XAttribute("MealPlanPrice", ""),
                             new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                             new XAttribute("CancellationDate", ""),
                             new XAttribute("CancellationAmount", ""),
                             new XAttribute("isAvailable", "true"),
                             new XElement("RequestID", Convert.ToString("")),
                             new XElement("Offers", ""),
                             new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements", ""),
                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups)),
                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                             )));
                    #endregion
                }
                return str;
            }
            #endregion

            #region Room Count 2
            if (totalroom == 2)
            {
                //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "1").ToList();
                //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "2").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children2ages = string.Empty;
                    List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                    int totc2 = child2age.Count();
                    for (int i = 0; i < child2age.Count(); i++)
                    {
                        if (i == totc2 - 1)
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value);
                        }
                        else
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                        }
                    }
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                }
                #endregion
                #endregion

                int group = 0;
                //Parallel.For(0, roomList1.Count(), m =>
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    //Parallel.For(0, roomList2.Count(), n =>
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        string bb1 = roomList1[m].Attribute("boardCode").Value;
                        string bb2 = roomList2[n].Attribute("boardCode").Value;
                        if (bb1 == bb2)
                        {
                            List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                            List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                            List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();
                            List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();
                            List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();
                            List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();

                            #region Board Bases >0
                            group++;
                            decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value);
                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

                            new XElement("Room",
                             new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                             new XAttribute("SuppliersID", "4"),
                             new XAttribute("RoomSeq", "1"),
                             new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                             new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                             new XAttribute("OccupancyID", Convert.ToString("")),
                             new XAttribute("OccupancyName", Convert.ToString("")),
                             new XAttribute("MealPlanID", Convert.ToString("")),
                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                             new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                             new XAttribute("PerNightRoomRate", Convert.ToString("20")),
                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                             new XAttribute("CancellationDate", ""),
                             new XAttribute("CancellationAmount", ""),
                              new XAttribute("isAvailable", "true"),
                             new XElement("RequestID", Convert.ToString("")),
                             new XElement("Offers", ""),
                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                                //new XElement("Promotions", Convert.ToString(promo1))),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements", ""
                                 ),
                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups1)),
                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                             ),

                            new XElement("Room",
                             new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("code").Value)),
                             new XAttribute("SuppliersID", "4"),
                             new XAttribute("RoomSeq", "2"),
                             new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("rateKey").Value)),
                             new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("name").Value)),
                             new XAttribute("OccupancyID", Convert.ToString("")),
                             new XAttribute("OccupancyName", Convert.ToString("")),
                             new XAttribute("MealPlanID", Convert.ToString("")),
                             new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                             new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                             new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("net").Value)),
                             new XAttribute("CancellationDate", ""),
                             new XAttribute("CancellationAmount", ""),
                              new XAttribute("isAvailable", "true"),
                             new XElement("RequestID", Convert.ToString("")),
                             new XElement("Offers", ""),
                              new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions2), GetHotelpromotionsHotelBeds(offer2)),
                                //new XElement("Promotions", Convert.ToString(promo2))),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements", ""
                                 ),
                                 new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups2)),
                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                             )));




                            #endregion
                        }
                    };
                };
                return str;
            }
            #endregion

            #region Room Count 3
            if (totalroom == 3)
            {
                //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "1").ToList();
                //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "2").ToList();
                //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "3").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children2ages = string.Empty;
                    List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                    int totc2 = child2age.Count();
                    for (int i = 0; i < child2age.Count(); i++)
                    {
                        if (i == totc2 - 1)
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value);
                        }
                        else
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                        }
                    }
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children3ages = string.Empty;
                    List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                    int totc3 = child3age.Count();
                    for (int i = 0; i < child3age.Count(); i++)
                    {
                        if (i == totc3 - 1)
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value);
                        }
                        else
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                        }
                    }
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                }
                #endregion
                #endregion
                int group = 0;

                #region Room 3
                Parallel.For(0, roomList1.Count(), m =>
                {
                    Parallel.For(0, roomList2.Count(), n =>
                    {
                        Parallel.For(0, roomList3.Count(), o =>
                        {
                            // add room 1, 2, 3

                            string bb1 = roomList1[m].Attribute("boardCode").Value;
                            string bb2 = roomList2[n].Attribute("boardCode").Value;
                            string bb3 = roomList3[o].Attribute("boardCode").Value;
                            if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3)
                            {

                                #region room's group
                                List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                                List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                                List<XElement> pricebrkups3 = roomList3[o].Descendants("dailyRate").ToList();

                                List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();

                                List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();

                                List<XElement> promotions3 = roomList3[o].Descendants("promotion").ToList();

                                List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();

                                List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();

                                List<XElement> offer3 = roomList3[o].Descendants("offer").ToList();


                                group++;
                                decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value);

                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

                                new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                                 new XAttribute("SuppliersID", "4"),
                                 new XAttribute("RoomSeq", "1"),
                                 new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                                 new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                 new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                 new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                 new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", "true"),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                                    // new XElement("Promotions", Convert.ToString(promo1))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements", ""
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups1)),
                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                                 ),

                                new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("code").Value)),
                                 new XAttribute("SuppliersID", "4"),
                                 new XAttribute("RoomSeq", "2"),
                                 new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("rateKey").Value)),
                                 new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("name").Value)),
                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                 new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                 new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                 new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("net").Value)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", "true"),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions2), GetHotelpromotionsHotelBeds(offer2)),
                                    //new XElement("Promotions", Convert.ToString(promo2))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements", ""
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups2)),
                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                                 ),

                                new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("code").Value)),
                                 new XAttribute("SuppliersID", "4"),
                                 new XAttribute("RoomSeq", "3"),
                                 new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("rateKey").Value)),
                                 new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("name").Value)),
                                 new XAttribute("OccupancyID", Convert.ToString("")),
                                 new XAttribute("OccupancyName", Convert.ToString("")),
                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                 new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                 new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                 new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("net").Value)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", "true"),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions3), GetHotelpromotionsHotelBeds(offer3)),
                                    //new XElement("Promotions", Convert.ToString(promo3))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements", ""
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups3)),
                                     new XElement("AdultNum", Convert.ToString(roomList3[o].Attribute("adults").Value)),
                                     new XElement("ChildNum", Convert.ToString(roomList3[o].Attribute("children").Value))
                                 )));
                                #endregion
                            }
                        });
                    });
                });
                return str;
                #endregion
            }
            #endregion

            #region Room Count 4
            if (totalroom == 4)
            {
                //roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "1").ToList();
                //roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "2").ToList();
                //roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "3").ToList();
                //roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("rooms").Value == "4").ToList();
                #region Get Combination (Room 1)
                List<XElement> room1child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild1 = room1child[0].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild1 == "0")
                {
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild1 == "1")
                {
                    List<XElement> childage = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children1ages = string.Empty;
                    List<XElement> child1age = reqTravayoo.Descendants("RoomPax").FirstOrDefault().Descendants("ChildAge").ToList();
                    int totc1 = child1age.Count();
                    for (int i = 0; i < child1age.Count(); i++)
                    {
                        if (i == totc1 - 1)
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value);
                        }
                        else
                        {
                            children1ages = children1ages + Convert.ToString(child1age[i].Value) + ",";
                        }
                    }
                    roomList1 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room1child[0].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children1ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 2)
                List<XElement> room2child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild2 = room2child[1].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild2 == "0")
                {
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild2 == "1")
                {
                    List<XElement> childage = room2child[1].Descendants("ChildAge").ToList();
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children2ages = string.Empty;
                    List<XElement> child2age = room2child[1].Descendants("ChildAge").ToList();
                    int totc2 = child2age.Count();
                    for (int i = 0; i < child2age.Count(); i++)
                    {
                        if (i == totc2 - 1)
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value);
                        }
                        else
                        {
                            children2ages = children2ages + Convert.ToString(child2age[i].Value) + ",";
                        }
                    }
                    roomList2 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room2child[1].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children2ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 3)
                List<XElement> room3child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild3 = room3child[2].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild3 == "0")
                {
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild3 == "1")
                {
                    List<XElement> childage = room3child[2].Descendants("ChildAge").ToList();
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children3ages = string.Empty;
                    List<XElement> child3age = room3child[2].Descendants("ChildAge").ToList();
                    int totc3 = child3age.Count();
                    for (int i = 0; i < child3age.Count(); i++)
                    {
                        if (i == totc3 - 1)
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value);
                        }
                        else
                        {
                            children3ages = children3ages + Convert.ToString(child3age[i].Value) + ",";
                        }
                    }
                    roomList3 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room3child[2].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children3ages).ToList();
                }
                #endregion
                #endregion
                #region Get Combination (Room 4)
                List<XElement> room4child = reqTravayoo.Descendants("RoomPax").ToList();
                string totalchild4 = room4child[3].Descendants("Child").FirstOrDefault().Value;
                #region if total children 0
                if (totalchild4 == "0")
                {
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("children").Value == "0").ToList();
                }
                #endregion
                #region if total children 1
                else if (totalchild4 == "1")
                {
                    List<XElement> childage = room4child[3].Descendants("ChildAge").ToList();
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == childage[0].Value).ToList();
                }
                #endregion
                #region if total children >1
                else
                {
                    string children4ages = string.Empty;
                    List<XElement> child4age = room4child[3].Descendants("ChildAge").ToList();
                    int totc4 = child4age.Count();
                    for (int i = 0; i < child4age.Count(); i++)
                    {
                        if (i == totc4 - 1)
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value);
                        }
                        else
                        {
                            children4ages = children4ages + Convert.ToString(child4age[i].Value) + ",";
                        }
                    }
                    roomList4 = roomlist.Descendants("rate").Where(el => el.Attribute("adults").Value == room4child[3].Descendants("Adult").FirstOrDefault().Value && el.Attribute("childrenAges").Value == children4ages).ToList();
                }
                #endregion
                #endregion

                int group = 0;

                #region Room 4
                Parallel.For(0, roomList1.Count(), m =>
                {
                    Parallel.For(0, roomList2.Count(), n =>
                    {
                        Parallel.For(0, roomList3.Count(), o =>
                        {
                            Parallel.For(0, roomList4.Count(), p =>
                            {
                                // add room 1, 2, 3,4

                                string bb1 = roomList1[m].Attribute("boardCode").Value;
                                string bb2 = roomList2[n].Attribute("boardCode").Value;
                                string bb3 = roomList3[o].Attribute("boardCode").Value;
                                string bb4 = roomList4[p].Attribute("boardCode").Value;
                                if (bb1 == bb2 && bb2 == bb3 && bb1 == bb3 && bb1 == bb4 && bb2 == bb4 && bb3 == bb4)
                                {

                                    #region room's group
                                    List<XElement> pricebrkups1 = roomList1[m].Descendants("dailyRate").ToList();
                                    List<XElement> pricebrkups2 = roomList2[n].Descendants("dailyRate").ToList();
                                    List<XElement> pricebrkups3 = roomList3[o].Descendants("dailyRate").ToList();
                                    List<XElement> pricebrkups4 = roomList4[p].Descendants("dailyRate").ToList();

                                    List<XElement> promotions1 = roomList1[m].Descendants("promotion").ToList();

                                    List<XElement> promotions2 = roomList2[n].Descendants("promotion").ToList();

                                    List<XElement> promotions3 = roomList3[o].Descendants("promotion").ToList();

                                    List<XElement> promotions4 = roomList4[p].Descendants("promotion").ToList();
                                    List<XElement> offer1 = roomList1[m].Descendants("offer").ToList();

                                    List<XElement> offer2 = roomList2[n].Descendants("offer").ToList();

                                    List<XElement> offer3 = roomList3[o].Descendants("offer").ToList();

                                    List<XElement> offer4 = roomList4[p].Descendants("offer").ToList();


                                    group++;
                                    decimal totalrate = Convert.ToDecimal(roomList1[m].Attribute("net").Value) + Convert.ToDecimal(roomList2[n].Attribute("net").Value) + Convert.ToDecimal(roomList3[o].Attribute("net").Value) + Convert.ToDecimal(roomList4[p].Attribute("net").Value);

                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalrate), new XAttribute("Index", group),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomList1[m].Parent.Parent.Attribute("code").Value)),
                                     new XAttribute("SuppliersID", "4"),
                                     new XAttribute("RoomSeq", "1"),
                                     new XAttribute("SessionID", Convert.ToString(roomList1[m].Attribute("rateKey").Value)),
                                     new XAttribute("RoomType", Convert.ToString(roomList1[m].Parent.Parent.Attribute("name").Value)),
                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList1[m].Attribute("boardName").Value)),
                                     new XAttribute("MealPlanCode", Convert.ToString(roomList1[m].Attribute("boardCode").Value)),
                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                     new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].Attribute("net").Value)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", "true"),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions1), GetHotelpromotionsHotelBeds(offer1)),
                                        //new XElement("Promotions", Convert.ToString(promo1))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements", ""
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups1)),
                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Attribute("adults").Value)),
                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Attribute("children").Value))
                                     ),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomList2[n].Parent.Parent.Attribute("code").Value)),
                                     new XAttribute("SuppliersID", "4"),
                                     new XAttribute("RoomSeq", "2"),
                                     new XAttribute("SessionID", Convert.ToString(roomList2[n].Attribute("rateKey").Value)),
                                     new XAttribute("RoomType", Convert.ToString(roomList2[n].Parent.Parent.Attribute("name").Value)),
                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList2[n].Attribute("boardName").Value)),
                                     new XAttribute("MealPlanCode", Convert.ToString(roomList2[n].Attribute("boardCode").Value)),
                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                     new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].Attribute("net").Value)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", "true"),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions2), GetHotelpromotionsHotelBeds(offer2)),
                                        //new XElement("Promotions", Convert.ToString(promo2))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements", ""
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups2)),
                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Attribute("adults").Value)),
                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Attribute("children").Value))
                                     ),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomList3[o].Parent.Parent.Attribute("code").Value)),
                                     new XAttribute("SuppliersID", "4"),
                                     new XAttribute("RoomSeq", "3"),
                                     new XAttribute("SessionID", Convert.ToString(roomList3[o].Attribute("rateKey").Value)),
                                     new XAttribute("RoomType", Convert.ToString(roomList3[o].Parent.Parent.Attribute("name").Value)),
                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList3[o].Attribute("boardName").Value)),
                                     new XAttribute("MealPlanCode", Convert.ToString(roomList3[o].Attribute("boardCode").Value)),
                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                     new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].Attribute("net").Value)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", "true"),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions3), GetHotelpromotionsHotelBeds(offer3)),
                                        // new XElement("Promotions", Convert.ToString(promo3))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements", ""
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups3)),
                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Attribute("adults").Value)),
                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Attribute("children").Value))
                                     ),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomList4[p].Parent.Parent.Attribute("code").Value)),
                                     new XAttribute("SuppliersID", "4"),
                                     new XAttribute("RoomSeq", "4"),
                                     new XAttribute("SessionID", Convert.ToString(roomList4[p].Attribute("rateKey").Value)),
                                     new XAttribute("RoomType", Convert.ToString(roomList4[p].Parent.Parent.Attribute("name").Value)),
                                     new XAttribute("OccupancyID", Convert.ToString("")),
                                     new XAttribute("OccupancyName", Convert.ToString("")),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList4[p].Attribute("boardName").Value)),
                                     new XAttribute("MealPlanCode", Convert.ToString(roomList4[p].Attribute("boardCode").Value)),
                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                     new XAttribute("PerNightRoomRate", Convert.ToString("10")),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].Attribute("net").Value)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", "true"),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList", GetHotelpromotionsHotelBeds(promotions4), GetHotelpromotionsHotelBeds(offer4)),
                                        // new XElement("Promotions", Convert.ToString(promo4))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements", ""
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupHotelBeds(pricebrkups4)),
                                         new XElement("AdultNum", Convert.ToString(roomList4[p].Attribute("adults").Value)),
                                         new XElement("ChildNum", Convert.ToString(roomList4[p].Attribute("children").Value))
                                     )));
                                    #endregion
                                }
                            });
                        });
                    });
                });
                return str;
                #endregion
            }
            #endregion

            return str;
        }
        #endregion

        #region HotelBeds's Room's Promotion
        private IEnumerable<XElement> GetHotelpromotionsHotelBeds(List<XElement> roompromotions)
        {

            Int32 length = roompromotions.Count();
            List<XElement> promotion = new List<XElement>();

            if (length == 0)
            {
                promotion.Add(new XElement("Promotions", ""));
            }
            else
            {

                Parallel.For(0, length, i =>
                {

                    promotion.Add(new XElement("Promotions", Convert.ToString(roompromotions[i].Attribute("name").Value)));

                });
            }
            return promotion;
        }
        #endregion
                       
        #region HotelBeds Room's Price Breakups
        private IEnumerable<XElement> GetRoomsPriceBreakupHotelBeds(List<XElement> pricebreakups)
        {
            #region HotelBeds Room's Price Breakups
        
            List<XElement> str = new List<XElement>();
            try
            {
                Parallel.For(0, pricebreakups.Count(), i =>
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                           new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].Attribute("dailyNet").Value)))
                    );
                });
                return str.OrderBy(x => (int)x.Attribute("Night")).ToList();
            }
            catch { return null; }
         
            #endregion
        }
        #endregion

        #region Hotel Facilities HotelBeds
        public IEnumerable<XElement> hotelfacilitiesHotelBeds(List<XElement> facility)
        {
            Int32 length = 0;
            if (facility != null)
            {
                length = facility.Count();
            }
            List<XElement> image = new List<XElement>();

            if (length == 0)
            {
                try
                {
                    image.Add(new XElement("Facility", "No Facility Available"));
                }
                catch { }
            }
            else
            {
                Parallel.For(0, length, i =>
                {
                    try
                    {
                        string code = string.Empty;
                        code = facility[i].Attribute("facilityCode").Value;
                        if (code == "130" || code == "220" || code == "575" || code == "550" || code == "250" || code == "261" || code == "100" || code == "535" || code == "540" || code == "560" || code == "470" || code == "200" || code == "295" || code == "575" || code == "620" || code == "295" || code == "280" || code == "170" || code == "605" || code == "363")
                        {
                            image.Add(new XElement("Facility", Convert.ToString(facility[i].Descendants("description").FirstOrDefault().Value)));
                        }
                    }
                    catch { }

                });
                if (image.Count == 0)
                {
                    image.Add(new XElement("Facility", "No Facility Available"));
                }
            }
            return image;
        }
        #endregion

        #region Remove Namespaces
        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            XElement xmlDocumentWithoutNs = removeAllNamespaces(xmlDocument);
            return xmlDocumentWithoutNs;
        }

        private static XElement removeAllNamespaces(XElement xmlDocument)
        {
            var stripped = new XElement(xmlDocument.Name.LocalName);
            foreach (var attribute in
                    xmlDocument.Attributes().Where(
                    attribute =>
                        !attribute.IsNamespaceDeclaration &&
                        String.IsNullOrEmpty(attribute.Name.NamespaceName)))
            {
                stripped.Add(new XAttribute(attribute.Name.LocalName, attribute.Value));
            }
            if (!xmlDocument.HasElements)
            {
                stripped.Value = xmlDocument.Value;
                return stripped;
            }
            stripped.Add(xmlDocument.Elements().Select(
                el =>
                    RemoveAllNamespaces(el)));
            return stripped;
        }
        #endregion

        public static List<T>[] Partition<T>(List<T> list, int totalPartitions)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (totalPartitions < 1)
                throw new ArgumentOutOfRangeException("totalPartitions");

            List<T>[] partitions = new List<T>[totalPartitions];

            int maxSize = (int)Math.Ceiling(list.Count / (double)totalPartitions);
            int k = 0;

            for (int i = 0; i < partitions.Length; i++)
            {
                partitions[i] = new List<T>();
                for (int j = k; j < k + maxSize; j++)
                {
                    if (j >= list.Count)
                        break;
                    partitions[i].Add(list[j]);
                }
                k += maxSize;
            }

            return partitions;
        }
        public static List<List<T>> BreakIntoSlots<T>(List<T> list, int slotSize)
        {
            if (slotSize <= 0)
            {
                throw new ArgumentException("Slot Size must be greater than 0.");
            }
            List<List<T>> retVal = new List<List<T>>();
            while (list.Count > 0)
            {
                int count = list.Count > slotSize ? slotSize : list.Count;
                retVal.Add(list.GetRange(0, count));
                list.RemoveRange(0, count);
            }

            return retVal;
        }
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
