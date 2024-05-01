using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
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
using TravillioXMLOutService.Models.HotelsPro;

namespace TravillioXMLOutService.App_Code
{
    public class HotelsProHotelSearch : IDisposable
    {
        int sup_cutime = 100000;
        XElement reqTravayoo;
        string dmc = string.Empty;
        string customerid = string.Empty;
        #region Hotel Availability of HotelsPro (XML OUT for Travayoo)
        public List<XElement> CheckAvailabilityHotelsPro(XElement req,XElement faci)
        {
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = null;
            try
            {
                List<XElement> roompax = req.Descendants("RoomPax").ToList();
                string paxdetails = searchrequest(roompax);
                #region Hotel Search
                string url = string.Empty;
                HotelsPro_Hotelstatic htlprostaticity = new HotelsPro_Hotelstatic();
                HotelsPro_Detail htlprostatcity = new HotelsPro_Detail();
                htlprostatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
                DataTable dtcity = htlprostaticity.GetCity_HotelsPro(htlprostatcity);
                htlprostatcity.CountryId = req.Descendants("PaxNationality_CountryID").FirstOrDefault().Value;
                DataTable dtcountry = htlprostaticity.GetCountry_HotelsPro(htlprostatcity);
                string destinationcode = string.Empty;
                string countrycode = string.Empty;
                if(dtcity!=null)
                {
                    if (dtcity.Rows.Count != 0)
                    {
                        destinationcode = dtcity.Rows[0]["citycode"].ToString();
                    }
                }
                if (dtcountry != null)
                {
                    countrycode = dtcountry.Rows[0]["countrycode"].ToString();
                }

                string clientnationality = req.Descendants("PaxNationality_CountryCode").Single().Value;
                
                DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                DateTime checkoutdt2 = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkout2 = checkoutdt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                url = "https://api-test.hotelspro.com/api/v2/search/?currency=USD&client_nationality=" + countrycode + "&destination_code=" + destinationcode + "" + paxdetails + "&checkin=" + checkin2 + "&checkout=" + checkout2 + "";
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);                
                string username = "Ingeniumsoftech";
                string password = "Jz7xCJMWWtZe7p8T";
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                webRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
                webRequest.ContentType = "application/json";
                webRequest.ContentLength = 0;
                webRequest.Method = "POST";
                webRequest.Host = "api-test.hotelspro.com";
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
                        //string Text = hotellist.results[0].hotel_code.Value;

                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 1;
                        log.LogType = "Search";
                        log.SupplierID = 6;
                        log.logrequestXML = req.ToString();
                        log.logresponseXML = soapResult.ToString();
                        //log.logresponseXML = doc.OuterXml.ToString();
                        try
                        {
                            APILog.SaveAPILogs(log);
                        }
                        catch (Exception ex)
                        {
                            APILog.SendExcepToDB(ex);
                        }
                        HotelsPro_Detail htlprostat = new HotelsPro_Detail();
                        HotelsPro_Hotelstatic htlprostaticdet = new HotelsPro_Hotelstatic();
                        htlprostat.CityCode = destinationcode;
                        htlprostat.MinStarRating = req.Descendants("MinStarRating").Single().Value;
                        htlprostat.MaxStarRating = req.Descendants("MaxStarRating").Single().Value;
                        DataTable dt = htlprostaticdet.GetHotelList_HotelsPro(htlprostat);
                        hotelavailabilityresponse = GetHotelListHotelsPro(hotellist, dt, faci);
                    }
                }
                catch (Exception ex)
                {
                    APILog.SendExcepToDB(ex);
                }
                #endregion

                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                APILog.SendExcepToDB(ex);
                return null;
            }
        }
        public List<XElement> CheckAvailabilityHotelsProThread(XElement req, XElement faci, string custID, string custName)
        {
            dmc = custName;
            customerid = custID;
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = new List<XElement>();
            try
            {
                #region get cut off time 
                try
                {
                    sup_cutime = supplier_Cred.secondcutoff_time();
                }
                catch { }
                //try
                //{
                //    APILogDetail log = new APILogDetail();
                //    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                //    log.TrackNumber = req.Descendants("TransID").Single().Value;
                //    log.LogTypeID = 151;
                //    log.LogType = "BProstart";
                //    SaveAPILog savelog = new SaveAPILog();
                //    savelog.SaveAPILogs(log);
                //}
                //catch { }
                
                int timeOut = sup_cutime;
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                timer.Start();
                #endregion
               
                List<XElement> roompax = req.Descendants("RoomPax").ToList();
                string paxdetails = searchrequest(roompax);
                #region Hotel Search
                string url = string.Empty;
                HotelsPro_Hotelstatic htlprostaticity = new HotelsPro_Hotelstatic();
                HotelsPro_Detail htlprostatcity = new HotelsPro_Detail();
                htlprostatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
                DataTable dtcity = htlprostaticity.GetCity_HotelsPro(htlprostatcity);
                htlprostatcity.CountryId = req.Descendants("PaxNationality_CountryID").FirstOrDefault().Value;
                DataTable dtcountry = htlprostaticity.GetCountry_HotelsPro(htlprostatcity);                
                string destinationcode = string.Empty;
                string countrycode = string.Empty;
                bool issearch = true;
                if (dtcity != null)
                {
                    if (dtcity.Rows.Count != 0)
                    {
                        destinationcode = dtcity.Rows[0]["citycode"].ToString();
                    }
                    else
                    {
                        issearch = false;
                    }
                }
                if (dtcountry != null)
                {
                    if (dtcountry.Rows.Count != 0)
                    {
                        countrycode = dtcountry.Rows[0]["countrycode"].ToString();
                    }
                    else
                    {
                        issearch = false;
                    }
                }
                if (!issearch)
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(customerid);
                    log.TrackNumber = reqTravayoo.Descendants("TransID").Single().Value;
                    log.LogTypeID = 1;
                    log.LogType = "Search";
                    log.SupplierID = 6;
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = "No city/country mapping found";
                    log.StartTime = DateTime.Now;
                    log.EndTime = DateTime.Now;
                    try
                    {
                        SaveAPILog savelogpro = new SaveAPILog();
                        savelogpro.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "searchresponse";
                        ex1.PageName = "HotelsProHotelSearch";
                        ex1.CustomerID = customerid;
                        ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    return null;
                }
                HotelsPro_Detail htlprostat = new HotelsPro_Detail();
                HotelsPro_Hotelstatic htlprostaticdet = new HotelsPro_Hotelstatic();
                htlprostat.CityCode = destinationcode;
                htlprostat.MinStarRating = req.Descendants("MinStarRating").Single().Value;
                htlprostat.MaxStarRating = req.Descendants("MaxStarRating").Single().Value;
                htlprostat.HotelCode = req.Descendants("HotelID").FirstOrDefault().Value;
                htlprostat.HotelName = req.Descendants("HotelName").FirstOrDefault().Value;



                DataTable dt = htlprostaticdet.GetHotelList_HotelsPro(htlprostat);
                if(dt.Rows.Count==0)
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
                //try
                //{
                //    APILogDetail log = new APILogDetail();
                //    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                //    log.TrackNumber = req.Descendants("TransID").Single().Value;
                //    log.LogTypeID = 151;
                //    log.LogType = "BProend";
                //    SaveAPILog savelog = new SaveAPILog();
                //    savelog.SaveAPILogs(log);
                //}
                //catch { }
                DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                DateTime checkoutdt2 = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkout2 = checkoutdt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "6");
                url = suppliercred.Descendants("searchendpoint").FirstOrDefault().Value;
                string currency = suppliercred.Descendants("currency").FirstOrDefault().Value;
                //url = url + "/?currency=USD&client_nationality=" + countrycode + "&destination_code=" + destinationcode + "" + paxdetails + "&checkin=" + checkin2 + "&checkout=" + checkout2 + "";
                url = url + "/?currency=" + currency + "&client_nationality=" + countrycode + paxdetails + "&checkin=" + checkin2 + "&checkout=" + checkout2 + "";
                #endregion

                int totc1 = dt.Rows.Count;
                string htlcode1 = string.Empty;
                string htlcode2 = string.Empty;
                string htlcode3 = string.Empty;
                string htlcode4 = string.Empty;
                //string htlcode5 = string.Empty;
                //string htlcode6 = string.Empty;  
                int divid = 4;
                if(dt.Rows.Count<=200)
                {
                    divid = 1;
                }

                var totalsets = Partition(dt.AsEnumerable().ToList(), divid);
                #region Slot 1
                if (totalsets[0].Count() > 0)
                {
                    List<DataRow> dr1 = totalsets[0];
                    for (int i = 0; i < dr1.Count(); i++)
                    {
                        if (i == dr1.Count() - 1)
                        {
                            htlcode1 = htlcode1 + Convert.ToString(dr1[i].ItemArray[1].ToString());
                        }
                        else
                        {
                            htlcode1 = htlcode1 + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                        }
                    }
                }
                #endregion
                if (divid == 4)
                {
                    #region Slot 2
                    if (totalsets[1].Count() > 0)
                    {
                        List<DataRow> dr1 = totalsets[1];
                        for (int i = 0; i < dr1.Count(); i++)
                        {
                            if (i == dr1.Count() - 1)
                            {
                                htlcode2 = htlcode2 + Convert.ToString(dr1[i].ItemArray[1].ToString());
                            }
                            else
                            {
                                htlcode2 = htlcode2 + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                            }
                        }
                    }
                    #endregion
                    #region Slot 3
                    if (totalsets[2].Count() > 0)
                    {
                        List<DataRow> dr1 = totalsets[2];
                        for (int i = 0; i < dr1.Count(); i++)
                        {
                            if (i == dr1.Count() - 1)
                            {
                                htlcode3 = htlcode3 + Convert.ToString(dr1[i].ItemArray[1].ToString());
                            }
                            else
                            {
                                htlcode3 = htlcode3 + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                            }
                        }
                    }
                    #endregion
                    #region Slot 4
                    if (totalsets[3].Count() > 0)
                    {
                        List<DataRow> dr1 = totalsets[3];
                        for (int i = 0; i < dr1.Count(); i++)
                        {
                            if (i == dr1.Count() - 1)
                            {
                                htlcode4 = htlcode4 + Convert.ToString(dr1[i].ItemArray[1].ToString());
                            }
                            else
                            {
                                htlcode4 = htlcode4 + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                            }
                        }
                    }
                    #endregion
                }
                //#region Slot 5
                //if (totalsets[4].Count() > 0)
                //{
                //    List<DataRow> dr1 = totalsets[4];
                //    for (int i = 0; i < dr1.Count(); i++)
                //    {
                //        if (i == dr1.Count() - 1)
                //        {
                //            htlcode5 = htlcode5 + Convert.ToString(dr1[i].ItemArray[1].ToString());
                //        }
                //        else
                //        {
                //            htlcode5 = htlcode5 + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                //        }
                //    }
                //}
                //#endregion
                //#region Slot 6 
                //if (totalsets[5].Count() > 0)
                //{
                //    List<DataRow> dr1 = totalsets[5];
                //    for (int i = 0; i < dr1.Count(); i++)
                //    {
                //        if (i == dr1.Count() - 1)
                //        {
                //            htlcode6 = htlcode6 + Convert.ToString(dr1[i].ItemArray[1].ToString());
                //        }
                //        else
                //        {
                //            htlcode6 = htlcode6 + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                //        }
                //    }
                //}
                //#endregion
                
                string postdata1 = string.Empty;
                string postdata2 = string.Empty;
                string postdata3 = string.Empty;
                string postdata4 = string.Empty;

                //string postdata5 = string.Empty;
                //string postdata6 = string.Empty;
                string[] postdata = new string[6];
                
                postdata[0] = "hotel_code=" + htlcode1;
                postdata[1] = "hotel_code=" + htlcode2;
                postdata[2] = "hotel_code=" + htlcode3;
                postdata[3] = "hotel_code=" + htlcode4;

                //postdata[4] = "hotel_code=" + htlcode5;
                //postdata[5] = "hotel_code=" + htlcode6;
               
                string soapResult = string.Empty;

                try
                {

                    #region Thread Initialize
                    //string hplist1 = string.Empty;
                    //string hplist2 = string.Empty;
                    //string hplist3 = string.Empty;
                    //string hplist4 = string.Empty;

                    //string hplist5 = string.Empty;


                    List<XElement> hplist1 = new List<XElement>();
                    List<XElement> hplist2 = new List<XElement>();
                    List<XElement> hplist3 = new List<XElement>();
                    List<XElement> hplist4 = new List<XElement>();
                    List<XElement> hplist5 = new List<XElement>();
                    List<XElement> hplist6 = new List<XElement>();


                    //Thread tid1 = null;
                    //Thread tid2 = null;
                    //Thread tid3 = null;
                    //Thread tid4 = null;

                    //Thread tid5 = null;
                  

                    //if (totalsets[0].Count() > 0)
                    //{
                    //    tid1 = new Thread(new ThreadStart(() => { hplist1 = searchresponse(url, postdata1); }));
                    //}
                    //if (totalsets[1].Count() > 0)
                    //{
                    //    tid2 = new Thread(new ThreadStart(() => { hplist2 = searchresponse(url, postdata2); }));
                    //}
                    //if (totalsets[2].Count() > 0)
                    //{
                    //    tid3 = new Thread(new ThreadStart(() => { hplist3 = searchresponse(url, postdata3); }));
                    //}
                    //if (totalsets[3].Count() > 0)
                    //{
                    //    tid4 = new Thread(new ThreadStart(() => { hplist4 = searchresponse(url, postdata4); }));
                    //}

                    //if (totalsets[4].Count() > 0)
                    //{
                    //    tid5 = new Thread(new ThreadStart(() => { hplist5 = searchresponse(url, postdata5); }));
                    //}                   
                    
                    List<Thread> threadedlist;
                    if (divid == 1)
                    {
                            threadedlist = new List<Thread>
                            {

                           new Thread(()=> hplist1 = searchresponsenew(url, postdata[0],dt,timeOut)),
                            };
                            threadedlist.ForEach(t => t.Start());
                            threadedlist.ForEach(t => t.Join(timeOut));
                            threadedlist.ForEach(t => t.Abort());
                            hotelavailabilityresponse = hplist1;
                            //hotelavailabilityresponse.AddRange(hplist1);

                            //if (hplist1.Count > 0)
                            //{
                            //    hotelavailabilityresponse.AddRange(hplist1);
                            //}
                            //if (hplist2.Count > 0)
                            //{
                            //    hotelavailabilityresponse.AddRange(hplist2);
                            //}
                    }
                    else
                    {
                        for (int i = 0; i < 4; i += 2)
                        {
                            if (timeOut >= 1)
                            {
                                threadedlist = new List<Thread>
                            {

                           new Thread(()=> hplist1 = searchresponsenew(url, postdata[i],dt,timeOut)),
                           new Thread(()=> hplist2 = searchresponsenew(url, postdata[i+1],dt,timeOut))
                           //new Thread(()=> hplist3 = searchresponsenew(url, postdata3,dt)),
                           //new Thread(()=> hplist4 = searchresponsenew(url, postdata4,dt)),
                           //new Thread(()=> hplist5 = searchresponsenew(url, postdata5,dt))
                            };
                                threadedlist.ForEach(t => t.Start());
                                threadedlist.ForEach(t => t.Join(timeOut));
                                threadedlist.ForEach(t => t.Abort());
                                hotelavailabilityresponse.AddRange(hplist1.Concat(hplist2));
                                //hotelavailabilityresponse.AddRange(hplist1);

                                //if (hplist1.Count > 0)
                                //{
                                //    hotelavailabilityresponse.AddRange(hplist1);
                                //}
                                //if (hplist2.Count > 0)
                                //{
                                //    hotelavailabilityresponse.AddRange(hplist2);
                                //}

                                timeOut = timeOut - Convert.ToInt32(timer.ElapsedMilliseconds);
                            }
                        }
                    }
                    return hotelavailabilityresponse;

                    #endregion

                    

                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "CheckAvailabilityHotelsProThread_1";
                    ex1.PageName = "HotelsProHotelSearch";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    return hotelavailabilityresponse;
                }
                #endregion
                
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAvailabilityHotelsProThread_2";
                ex1.PageName = "HotelsProHotelSearch";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return hotelavailabilityresponse;
            }
        }
        public List<XElement> CheckAvailabilityHotelsProThreadHA(XElement req, XElement faci)
        {
            dmc = "HA";
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = new List<XElement>();
            try
            {
                #region get cut off time
                try
                {
                    sup_cutime = supplier_Cred.secondcutoff_time();
                }
                catch { }
                #endregion                
                List<XElement> roompax = req.Descendants("RoomPax").ToList();
                string paxdetails = searchrequest(roompax);
                #region Hotel Search
                string url = string.Empty;
                HotelsPro_Hotelstatic htlprostaticity = new HotelsPro_Hotelstatic();
                HotelsPro_Detail htlprostatcity = new HotelsPro_Detail();
                htlprostatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
                DataTable dtcity = htlprostaticity.GetCity_HotelsPro(htlprostatcity);
                htlprostatcity.CountryId = req.Descendants("PaxNationality_CountryID").FirstOrDefault().Value;
                DataTable dtcountry = htlprostaticity.GetCountry_HotelsPro(htlprostatcity);
                string destinationcode = string.Empty;
                string countrycode = string.Empty;
                if (dtcity != null)
                {
                    if (dtcity.Rows.Count != 0)
                    {
                        destinationcode = dtcity.Rows[0]["citycode"].ToString();
                    }
                }
                if (dtcountry != null)
                {
                    if (dtcountry.Rows.Count != 0)
                    {
                        countrycode = dtcountry.Rows[0]["countrycode"].ToString();
                    }
                }

                HotelsPro_Detail htlprostat = new HotelsPro_Detail();
                HotelsPro_Hotelstatic htlprostaticdet = new HotelsPro_Hotelstatic();
                htlprostat.CityCode = destinationcode;
                htlprostat.MinStarRating = req.Descendants("MinStarRating").Single().Value;
                htlprostat.MaxStarRating = req.Descendants("MaxStarRating").Single().Value;
                DataTable dt = htlprostaticdet.GetHotelList_HotelsPro(htlprostat);
                
                DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                DateTime checkoutdt2 = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkout2 = checkoutdt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                //url = "https://api-test.hotelspro.com/api/v2/search/?currency=USD&client_nationality=" + countrycode + "&destination_code=" + destinationcode + "" + paxdetails + "&checkin=" + checkin2 + "&checkout=" + checkout2 + "";
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "6");
                url = suppliercred.Descendants("searchendpoint").FirstOrDefault().Value;
                string currency = suppliercred.Descendants("currency").FirstOrDefault().Value;
                //url = url + "/?currency=USD&client_nationality=" + countrycode + "&destination_code=" + destinationcode + "" + paxdetails + "&checkin=" + checkin2 + "&checkout=" + checkout2 + "";
                url = url + "/?currency="+currency+"&client_nationality=" + countrycode + paxdetails + "&checkin=" + checkin2 + "&checkout=" + checkout2 + "";
                #endregion
                int totc1 = dt.Rows.Count;
                string htlcode1 = string.Empty;
                string htlcode2 = string.Empty;
                string htlcode3 = string.Empty;
                string htlcode4 = string.Empty;

                string htlcode5 = string.Empty;
               

                var totalsets = Partition(dt.AsEnumerable().ToList(), 5);
                #region Slot 1
                if (totalsets[0].Count() > 0)
                {
                    List<DataRow> dr1 = totalsets[0];
                    for (int i = 0; i < dr1.Count(); i++)
                    {
                        if (i == dr1.Count() - 1)
                        {
                            htlcode1 = htlcode1 + Convert.ToString(dr1[i].ItemArray[1].ToString());
                        }
                        else
                        {
                            htlcode1 = htlcode1 + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                        }
                    }
                }
                #endregion
                #region Slot 2
                if (totalsets[1].Count() > 0)
                {
                    List<DataRow> dr1 = totalsets[1];
                    for (int i = 0; i < dr1.Count(); i++)
                    {
                        if (i == dr1.Count() - 1)
                        {
                            htlcode2 = htlcode2 + Convert.ToString(dr1[i].ItemArray[1].ToString());
                        }
                        else
                        {
                            htlcode2 = htlcode2 + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                        }
                    }
                }
                #endregion
                #region Slot 3
                if (totalsets[2].Count() > 0)
                {
                    List<DataRow> dr1 = totalsets[2];
                    for (int i = 0; i < dr1.Count(); i++)
                    {
                        if (i == dr1.Count() - 1)
                        {
                            htlcode3 = htlcode3 + Convert.ToString(dr1[i].ItemArray[1].ToString());
                        }
                        else
                        {
                            htlcode3 = htlcode3 + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                        }
                    }
                }
                #endregion
                #region Slot 4
                if (totalsets[3].Count() > 0)
                {
                    List<DataRow> dr1 = totalsets[3];
                    for (int i = 0; i < dr1.Count(); i++)
                    {
                        if (i == dr1.Count() - 1)
                        {
                            htlcode4 = htlcode4 + Convert.ToString(dr1[i].ItemArray[1].ToString());
                        }
                        else
                        {
                            htlcode4 = htlcode4 + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                        }
                    }
                }
                #endregion

                #region Slot 5
                if (totalsets[4].Count() > 0)
                {
                    List<DataRow> dr1 = totalsets[4];
                    for (int i = 0; i < dr1.Count(); i++)
                    {
                        if (i == dr1.Count() - 1)
                        {
                            htlcode5 = htlcode5 + Convert.ToString(dr1[i].ItemArray[1].ToString());
                        }
                        else
                        {
                            htlcode5 = htlcode5 + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                        }
                    }
                }
                #endregion
               
                string postdata1 = string.Empty;
                string postdata2 = string.Empty;
                string postdata3 = string.Empty;
                string postdata4 = string.Empty;

                string postdata5 = string.Empty;
               

                postdata1 = "hotel_code=" + htlcode1;
                postdata2 = "hotel_code=" + htlcode2;
                postdata3 = "hotel_code=" + htlcode3;
                postdata4 = "hotel_code=" + htlcode4;

                postdata5 = "hotel_code=" + htlcode5;
                

                string soapResult = string.Empty;

                try
                {

                    #region Thread Initialize
                    //string hplist1 = string.Empty;
                    //string hplist2 = string.Empty;
                    //string hplist3 = string.Empty;
                    //string hplist4 = string.Empty;

                    //string hplist5 = string.Empty;
                    List<XElement> hplist1 = new List<XElement>();
                    List<XElement> hplist2 = new List<XElement>();
                    List<XElement> hplist3 = new List<XElement>();
                    List<XElement> hplist4 = new List<XElement>();
                    List<XElement> hplist5 = new List<XElement>();
                  
                    Thread tid1 = null;
                    Thread tid2 = null;
                    Thread tid3 = null;
                    Thread tid4 = null;

                    Thread tid5 = null;
                   

                    //if (totalsets[0].Count() > 0)
                    //{
                    //    tid1 = new Thread(new ThreadStart(() => { hplist1 = searchresponse(url, postdata1); }));
                    //}
                    //if (totalsets[1].Count() > 0)
                    //{
                    //    tid2 = new Thread(new ThreadStart(() => { hplist2 = searchresponse(url, postdata2); }));
                    //}
                    //if (totalsets[2].Count() > 0)
                    //{
                    //    tid3 = new Thread(new ThreadStart(() => { hplist3 = searchresponse(url, postdata3); }));
                    //}
                    //if (totalsets[3].Count() > 0)
                    //{
                    //    tid4 = new Thread(new ThreadStart(() => { hplist4 = searchresponse(url, postdata4); }));
                    //}

                    //if (totalsets[4].Count() > 0)
                    //{
                    //    tid5 = new Thread(new ThreadStart(() => { hplist5 = searchresponse(url, postdata5); }));
                    //}

                    if (totalsets[0].Count() > 0)
                    {
                        tid1 = new Thread(new ThreadStart(() => { hplist1 = searchresponsenew(url, postdata1,dt,85000); }));
                    }
                    if (totalsets[1].Count() > 0)
                    {
                        tid2 = new Thread(new ThreadStart(() => { hplist2 = searchresponsenew(url, postdata2, dt, 85000); }));
                    }
                    if (totalsets[2].Count() > 0)
                    {
                        tid3 = new Thread(new ThreadStart(() => { hplist3 = searchresponsenew(url, postdata3, dt, 85000); }));
                    }
                    if (totalsets[3].Count() > 0)
                    {
                        tid4 = new Thread(new ThreadStart(() => { hplist4 = searchresponsenew(url, postdata4, dt, 85000); }));
                    }

                    if (totalsets[4].Count() > 0)
                    {
                        tid5 = new Thread(new ThreadStart(() => { hplist5 = searchresponsenew(url, postdata5, dt, 85000); }));
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

                    if (totalsets[4].Count() > 0)
                    {
                        tid5.Start();
                    }
                    
                    #endregion

                    #region Thread Join
                    if (totalsets[0].Count() > 0)
                    {
                        tid1.Join(sup_cutime);
                    }
                    if (totalsets[1].Count() > 0)
                    {
                        tid2.Join(sup_cutime);
                    }
                    if (totalsets[2].Count() > 0)
                    {
                        tid3.Join(sup_cutime);
                    }
                    if (totalsets[3].Count() > 0)
                    {
                        tid4.Join(sup_cutime);
                    }

                    if (totalsets[4].Count() > 0)
                    {
                        tid5.Join(sup_cutime);
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

                    if (totalsets[4].Count() > 0)
                    {
                        tid5.Abort();
                    }
                    
                    #endregion


                    //dynamic hotellist1 = Newtonsoft.Json.JsonConvert.DeserializeObject(hplist1);
                    //List<XElement> hotelavailabilityresponse1 = new List<XElement>();
                    //hotelavailabilityresponse1 = GetHotelListHotelsPro(hotellist1, dt, faci);

                    //dynamic hotellist2 = Newtonsoft.Json.JsonConvert.DeserializeObject(hplist2);
                    //List<XElement> hotelavailabilityresponse2 = new List<XElement>();
                    //hotelavailabilityresponse2 = GetHotelListHotelsPro(hotellist2, dt, faci);

                    //dynamic hotellist3 = Newtonsoft.Json.JsonConvert.DeserializeObject(hplist3);
                    //List<XElement> hotelavailabilityresponse3 = new List<XElement>();
                    //hotelavailabilityresponse3 = GetHotelListHotelsPro(hotellist3, dt, faci);

                    //dynamic hotellist4 = Newtonsoft.Json.JsonConvert.DeserializeObject(hplist4);
                    //List<XElement> hotelavailabilityresponse4 = new List<XElement>();
                    //hotelavailabilityresponse4 = GetHotelListHotelsPro(hotellist4, dt, faci);

                    //dynamic hotellist5 = Newtonsoft.Json.JsonConvert.DeserializeObject(hplist5);
                    //List<XElement> hotelavailabilityresponse5 = new List<XElement>();
                    //hotelavailabilityresponse5 = GetHotelListHotelsPro(hotellist5, dt, faci);

                    //if (hotelavailabilityresponse1 == null)
                    //{
                    //    hotelavailabilityresponse1 = new List<XElement>();
                    //}
                    //if (hotelavailabilityresponse2 == null)
                    //{
                    //    hotelavailabilityresponse2 = new List<XElement>();
                    //}
                    //if (hotelavailabilityresponse3 == null)
                    //{
                    //    hotelavailabilityresponse3 = new List<XElement>();
                    //}
                    //if (hotelavailabilityresponse4 == null)
                    //{
                    //    hotelavailabilityresponse4 = new List<XElement>();
                    //}

                    //if (hotelavailabilityresponse5 == null)
                    //{
                    //    hotelavailabilityresponse5 = new List<XElement>();
                    //}

                    //hotelavailabilityresponse.AddRange(hotelavailabilityresponse1.Concat(hotelavailabilityresponse2).Concat(hotelavailabilityresponse3).Concat(hotelavailabilityresponse4).Concat(hotelavailabilityresponse5));


                    if (hplist1.Count > 0)
                    {
                        hotelavailabilityresponse.AddRange(hplist1);
                    }
                    if (hplist2.Count > 0)
                    {
                        hotelavailabilityresponse.AddRange(hplist2);
                    }
                    if (hplist3.Count > 0)
                    {
                        hotelavailabilityresponse.AddRange(hplist3);
                    }
                    if (hplist4.Count > 0)
                    {
                        hotelavailabilityresponse.AddRange(hplist4);
                    }

                    if (hplist5.Count > 0)
                    {
                        hotelavailabilityresponse.AddRange(hplist5);
                    }

                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "CheckAvailabilityHotelsProThreadHA";
                    ex1.PageName = "HotelsProHotelSearch";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    return null;
                }
                #endregion

                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CheckAvailabilityHotelsProThreadHA";
                ex1.PageName = "HotelsProHotelSearch";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        public List<XElement> CheckAvailabilityHotelsProSpeed(XElement req, XElement faci)
        {
            reqTravayoo = req;
            List<XElement> hotelavailabilityresponse = new List<XElement>();
            try
            {
                List<XElement> roompax = req.Descendants("RoomPax").ToList();
                string paxdetails = searchrequest(roompax);
                #region Hotel Search
                string url = string.Empty;
                HotelsPro_Hotelstatic htlprostaticity = new HotelsPro_Hotelstatic();
                HotelsPro_Detail htlprostatcity = new HotelsPro_Detail();
                htlprostatcity.CityCode = req.Descendants("CityID").FirstOrDefault().Value;
                DataTable dtcity = htlprostaticity.GetCity_HotelsPro(htlprostatcity);
                htlprostatcity.CountryId = req.Descendants("PaxNationality_CountryID").FirstOrDefault().Value;
                DataTable dtcountry = htlprostaticity.GetCountry_HotelsPro(htlprostatcity);
                string destinationcode = string.Empty;
                string countrycode = string.Empty;
                if (dtcity != null)
                {
                    destinationcode = dtcity.Rows[0]["citycode"].ToString();
                }
                if (dtcountry != null)
                {
                    countrycode = dtcountry.Rows[0]["countrycode"].ToString();
                }

                HotelsPro_Detail htlprostat = new HotelsPro_Detail();
                HotelsPro_Hotelstatic htlprostaticdet = new HotelsPro_Hotelstatic();
                htlprostat.CityCode = destinationcode;
                htlprostat.MinStarRating = req.Descendants("MinStarRating").Single().Value;
                htlprostat.MaxStarRating = req.Descendants("MaxStarRating").Single().Value;
                DataTable dt = htlprostaticdet.GetHotelList_HotelsPro(htlprostat);

                DateTime checkindt2 = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkin2 = checkindt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                DateTime checkoutdt2 = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                string checkout2 = checkoutdt2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                url = "https://api-test.hotelspro.com/api/v2/search/?currency=USD&client_nationality=" + countrycode + "&destination_code=" + destinationcode + "" + paxdetails + "&checkin=" + checkin2 + "&checkout=" + checkout2 + "";

                int totc1 = dt.Rows.Count;

                #region changes
                var SlotList = BreakIntoSlots(dt.AsEnumerable().ToList(), 40);
                int Number = SlotList.Count;
                List<XElement> Thresult = new List<XElement>();
                Parallel.ForEach(SlotList, (dr1) =>
                //for(int k=0;k<SlotList.Count();k++)
                {                    
                    string postdata = string.Empty;
                    string htlcode = string.Empty;
                    //List<DataRow> dr1 = SlotList[k];
                    for (int i = 0; i < dr1.Count(); i++)
                    {
                        if (i == dr1.Count() - 1)
                        {
                            htlcode = htlcode + Convert.ToString(dr1[i].ItemArray[1].ToString());
                        }
                        else
                        {
                            htlcode = htlcode + Convert.ToString(dr1[i].ItemArray[1].ToString()) + ",";
                        }
                    }
                    postdata = "hotel_code=" + htlcode;
                    string result = searchresponse(url, postdata);
                    dynamic hotellist = Newtonsoft.Json.JsonConvert.DeserializeObject(result);
                    List<XElement> hotelavailabilityresp = new List<XElement>();
                    hotelavailabilityresp = GetHotelListHotelsPro(hotellist, dt, faci);

                    XElement respon = new XElement("Hotels", hotelavailabilityresp);

                    if (hotelavailabilityresp != null)
                    {
                        Thresult.Add(respon);
                    }
                    else
                    {
                        Thresult.Add(new XElement("Hotels", new XElement("Hotel", null)));
                    }
                });

                hotelavailabilityresponse = Thresult.Descendants("Hotel").ToList();

                #endregion

                #endregion

                return hotelavailabilityresponse;
            }
            catch (Exception ex)
            {
                APILog.SendExcepToDB(ex);
                return null;
            }
        }
        #endregion

        #region Hotel Availability Request
        private string searchrequest(List<XElement> roompax)
        {
            #region Get Total Occupancy
            string paxdetails = string.Empty;
            for (int i = 0; i < roompax.Count(); i++)
            {
                int adults = Convert.ToInt32(roompax[i].Descendants("Adult").FirstOrDefault().Value);
                int children = Convert.ToInt32(roompax[i].Descendants("Child").FirstOrDefault().Value);

                if (children > 0)
                {
                    List<XElement> childcount = roompax[i].Descendants("ChildAge").ToList();
                    string childpaxdetails = string.Empty;
                    for (int j = 0; j < childcount.Count(); j++)
                    {
                        string childage = childcount[j].Value;
                        childpaxdetails = childpaxdetails + "," + childage;
                    }
                    paxdetails = paxdetails + "&pax=" + adults + childpaxdetails;
                }
                else
                {
                    paxdetails = paxdetails + "&pax=" + adults;
                }
            }
            return paxdetails;
            #endregion
        }        
        #endregion
        
        #region HotelsPro Hotel Listing
        private IEnumerable<XElement> GetHotelListHotelsPro(dynamic hotel, DataTable dtTable,XElement fac)
        {            
            #region HotelsPro Hotels
            List<XElement> hotellst = new List<XElement>();
            string xmlouttype = string.Empty;
            try
            {
                if(dmc=="HotelsPro")
                {
                    xmlouttype = "false";
                }
                else
                { xmlouttype = "true"; }
            }
            catch { }
            int totalroom = 1;
            try
            {
                totalroom = Convert.ToInt32(reqTravayoo.Descendants("RoomPax").Count());
            }
            catch { }
            try
            {
                Int32 length = Convert.ToInt32(hotel.count.Value);
                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        try
                        {
                            #region Fetch hotel
                            string hotelcode = Convert.ToString(hotel.results[i].hotel_code.Value);
                            DataRow[] row = dtTable.Select("HotelCode = " + "'" + hotelcode + "'");
                            if (row.Count() > 0)
                            {
                                IEnumerable<XElement> facility = null;
                                decimal minRate = 0;
                                int minindx = 0;
                                string star = string.Empty;
                                try
                                {
                                    #region Get Min Rate
                                    try
                                    {
                                        int count = hotel.results[i].products.Count;
                                        for (int n = 0; n < count; n++)
                                        {
                                            if (Convert.ToInt16(hotel.results[i].products[n].rooms.Count) == totalroom)
                                            {
                                                decimal totalrate = Convert.ToDecimal(hotel.results[i].products[n].price.Value);
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
                                        if (minRate <= 0)
                                        {
                                            minRate = hotel.results[i].products[0].price.Value;
                                        }
                                    }
                                    catch { minRate = hotel.results[i].products[0].price.Value; }
                                    #endregion
                                    star = row[0]["Star"].ToString();
                                    //dynamic facilities = row[0]["facilities"];
                                    
                                    //facility = hotelfacilitiesHotelsPro(facilities,fac);
                                }
                                catch { }
                                hotellst.Add(new XElement("Hotel",
                                                       new XElement("HotelID", Convert.ToString(hotel.results[i].hotel_code.Value)),
                                                       new XElement("HotelName", Convert.ToString(row[0]["HotelName"].ToString())),
                                                       new XElement("PropertyTypeName", Convert.ToString("")),
                                                       new XElement("CountryID", Convert.ToString("")),
                                                       new XElement("CountryName", Convert.ToString("")),
                                                       new XElement("CountryCode", Convert.ToString(row[0]["CountryCode"].ToString())),
                                                       new XElement("CityId", Convert.ToString("")),
                                                       new XElement("CityCode", Convert.ToString(hotel.results[i].destination_code.Value)),
                                                       new XElement("CityName", Convert.ToString("")),
                                                       new XElement("AreaId", Convert.ToString("")),
                                                       new XElement("AreaName", Convert.ToString(row[0]["area"].ToString())),
                                                       new XElement("RequestID", Convert.ToString(hotel.code)),
                                                       new XElement("Address", Convert.ToString(row[0]["Address"].ToString())),
                                                       new XElement("Location", Convert.ToString(row[0]["Address"].ToString())),
                                                       new XElement("Description", Convert.ToString("")),
                                                       new XElement("StarRating", Convert.ToString(star)),
                                                       new XElement("MinRate", Convert.ToString(minRate)),
                                                       new XElement("HotelImgSmall", Convert.ToString(row[0]["MainImage"].ToString())),
                                                       new XElement("HotelImgLarge", Convert.ToString(row[0]["MainImage"].ToString())),
                                                       new XElement("MapLink", ""),
                                                       new XElement("Longitude", Convert.ToString(row[0]["Longitude"].ToString())),
                                                       new XElement("Latitude", Convert.ToString(row[0]["Latitude"].ToString())),
                                                       new XElement("xmloutcustid", customerid),
                                                       new XElement("xmlouttype", xmlouttype),
                                                       new XElement("DMC", dmc),
                                                       new XElement("SupplierID", "6"),
                                                       new XElement("Currency", Convert.ToString(hotel.results[i].products[0].currency.Value)),
                                                       new XElement("Offers", "")
                                                       , new XElement("Facilities",null)
                                                           //facility)
                                                       , new XElement("Rooms", ""

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
        #region Hotel Facilities HotelsPro
        public IEnumerable<XElement> hotelfacilitiesHotelsPro(dynamic facilities, XElement fac)
        {
            Int32 length = 0;
            dynamic facility = Newtonsoft.Json.JsonConvert.DeserializeObject(facilities);
            if (facility != null)
            {
                length = facility.Count;
            }

            //Int32 length = facility.Count;
            List<XElement> faci = new List<XElement>();
            string facilityname = string.Empty;
            if (length == 0)
            {
                faci.Add(new XElement("Facility", "No Facility Available"));
            }
            else
            {

                //XElement fac = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\HotelsPro\Facilities.xml"));

                List<XElement> totfac = fac.Descendants("item").Where(x => x.Attribute("type").Value == "object").ToList();

                for (int i = 0; i < length; i++)
                {
                    try
                    {
                        string code = facility[i].Value;

                        if (code == "96" || code == "229" || code == "3a8" || code == "47" || code == "206" || code == "3eb" || code == "3e9" || code == "3f4" || code == "3ed" || code == "88" || code == "2a7" || code == "22f" || code == "af" || code == "32d")
                        {
                            XElement fcname = totfac.Where(x => x.Descendants("code").FirstOrDefault().Value == code).FirstOrDefault();
                            facilityname = fcname.Descendants("name").FirstOrDefault().Value;
                            faci.Add(new XElement("Facility", Convert.ToString(facilityname)));
                        }
                    }
                    catch { }
                }
                if(faci.Count==0)
                {
                    faci.Add(new XElement("Facility", "No Facility Available"));
                }
            }
            return faci;
        }
        #endregion

        #region Partition
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

        #endregion

        #region Search Method
        public string searchresponse(string url, string postData)
        {
            try
            {
                var startTime = DateTime.Now;
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myHttpWebRequest.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes(postData);
                string username = string.Empty;
                string password = string.Empty;

                //HotelsProCredentials _credential = new HotelsProCredentials();
                //username = _credential.username;
                //password = _credential.password;
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(reqTravayoo.Descendants("CustomerID").FirstOrDefault().Value, "6");
                username = suppliercred.Descendants("username").FirstOrDefault().Value;
                password = suppliercred.Descendants("password").FirstOrDefault().Value;
                #endregion
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                myHttpWebRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
                myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
                myHttpWebRequest.ContentLength = data.Length;                
                Stream requestStream = myHttpWebRequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();                

                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();                                
                Stream responseStream = myHttpWebResponse.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default);
                string pageContent = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                responseStream.Close();
                myHttpWebResponse.Close();
                var xmlresponse = "";
                try
                {
                    var srchresponse = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.ASCII.GetBytes(pageContent), new XmlDictionaryReaderQuotas()));
                    XElement availresponse = XElement.Parse(srchresponse.ToString());
                    XElement doc = RemoveAllNamespaces(availresponse);
                    xmlresponse = doc.ToString();
                }
                catch { xmlresponse = pageContent; }
                #region Log Save
                string suprequest = url +"postedData"+ postData;
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(reqTravayoo.Descendants("CustomerID").Single().Value);
                log.TrackNumber = reqTravayoo.Descendants("TransID").Single().Value;
                log.LogTypeID = 1;
                log.LogType = "Search";
                log.SupplierID = 6;
                log.logrequestXML = suprequest.ToString();
                log.logresponseXML = xmlresponse.ToString();
                log.StartTime = startTime;
                log.EndTime = DateTime.Now;
                try
                {
                    SaveAPILog savelogpro = new SaveAPILog();
                    savelogpro.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "searchresponse";
                    ex1.PageName = "HotelsProHotelSearch";
                    ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
                    ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                #endregion


                return pageContent;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "searchresponse";
                ex1.PageName = "HotelsProHotelSearch";
                ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
                ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        #endregion
        #region Search Method New
        public List<XElement> searchresponsenew(string url, string postData, DataTable dt,int timeout_sup)
        {
            List<XElement> HotelsList = new List<XElement>();
            var startTime = DateTime.Now;
            try
            {                
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myHttpWebRequest.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes(postData);
                string username = string.Empty;
                string password = string.Empty;

                //HotelsProCredentials _credential = new HotelsProCredentials();
                //username = _credential.username;
                //password = _credential.password;
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "6");
                username = suppliercred.Descendants("username").FirstOrDefault().Value;
                password = suppliercred.Descendants("password").FirstOrDefault().Value;
                #endregion
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                myHttpWebRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
                myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
                myHttpWebRequest.ContentLength = data.Length;
                myHttpWebRequest.Timeout = timeout_sup;
                Stream requestStream = myHttpWebRequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                Stream responseStream = myHttpWebResponse.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default);
                string pageContent = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                responseStream.Close();
                myHttpWebResponse.Close();
                var xmlresponse = "";
                try
                {
                    var srchresponse = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.ASCII.GetBytes(pageContent), new XmlDictionaryReaderQuotas()));
                    XElement availresponse = XElement.Parse(srchresponse.ToString());
                    XElement doc = RemoveAllNamespaces(availresponse);
                    xmlresponse = doc.ToString();
                }
                catch { xmlresponse = pageContent; }
                #region Log Save
                string suprequest = url + "postedData" + postData;
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(customerid);
                log.TrackNumber = reqTravayoo.Descendants("TransID").Single().Value;
                log.LogTypeID = 1;
                log.LogType = "Search";
                log.SupplierID = 6;
                log.logrequestXML = suprequest.ToString();
                log.logresponseXML = xmlresponse.ToString();
                log.StartTime = startTime;
                log.EndTime = DateTime.Now;
                try
                {
                    SaveAPILog savelogpro = new SaveAPILog();
                    savelogpro.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "searchresponse";
                    ex1.PageName = "HotelsProHotelSearch";
                    ex1.CustomerID = customerid;
                    ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                #endregion
                dynamic HotelList = JsonConvert.DeserializeObject(pageContent);
                HotelsList = GetHotelListHotelsPro(HotelList, dt, null);

                return HotelsList;
            }
            catch (WebException ex)
            {
                #region Save in apilog table
                if (ex.Response != null)
                {
                    try
                    {
                        var responses = ex.Response;
                        var dataStream = responses.GetResponseStream();
                        var reader = new StreamReader(dataStream);
                        var details = reader.ReadToEnd();
                        string suprequest = url + "postedData" + postData;
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(customerid);
                        log.TrackNumber = reqTravayoo.Descendants("TransID").Single().Value;
                        log.LogTypeID = 1;
                        log.LogType = "Search";
                        log.SupplierID = 6;
                        log.logrequestXML = suprequest.ToString();
                        log.logresponseXML = details.ToString();
                        log.StartTime = startTime;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelogpro = new SaveAPILog();
                        savelogpro.SaveAPILogs(log);
                    }
                    catch { }
                }
                #endregion
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "searchresponsenew";
                ex1.PageName = "HotelsProHotelSearch";
                ex1.CustomerID = customerid;
                ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);               
                return HotelsList;
            }
        }
        //public List<XElement> searchresponsenew1(string url, string postData, DataTable dt)
        //{
        //    try
        //    {
        //        Thread.Sleep(33000);
        //        var startTime = DateTime.Now;
        //        HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
        //        myHttpWebRequest.Method = "POST";
        //        byte[] data = Encoding.ASCII.GetBytes(postData);
        //        string username = string.Empty;
        //        string password = string.Empty;

        //        //HotelsProCredentials _credential = new HotelsProCredentials();
        //        //username = _credential.username;
        //        //password = _credential.password;
        //        #region Credentials
        //        XElement suppliercred = supplier_Cred.getsupplier_credentials(reqTravayoo.Descendants("CustomerID").FirstOrDefault().Value, "6");
        //        username = suppliercred.Descendants("username").FirstOrDefault().Value;
        //        password = suppliercred.Descendants("password").FirstOrDefault().Value;
        //        #endregion
        //        string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
        //        myHttpWebRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
        //        myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
        //        myHttpWebRequest.ContentLength = data.Length;
        //        Stream requestStream = myHttpWebRequest.GetRequestStream();
        //        requestStream.Write(data, 0, data.Length);
        //        requestStream.Close();

        //        HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
        //        Stream responseStream = myHttpWebResponse.GetResponseStream();
        //        StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default);
        //        string pageContent = myStreamReader.ReadToEnd();
        //        myStreamReader.Close();
        //        responseStream.Close();
        //        myHttpWebResponse.Close();
        //        var xmlresponse = "";
        //        try
        //        {
        //            var srchresponse = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.ASCII.GetBytes(pageContent), new XmlDictionaryReaderQuotas()));
        //            XElement availresponse = XElement.Parse(srchresponse.ToString());
        //            XElement doc = RemoveAllNamespaces(availresponse);
        //            xmlresponse = doc.ToString();
        //        }
        //        catch { xmlresponse = pageContent; }
        //        #region Log Save
        //        string suprequest = url + "postedData" + postData;
        //        APILogDetail log = new APILogDetail();
        //        log.customerID = Convert.ToInt64(reqTravayoo.Descendants("CustomerID").Single().Value);
        //        log.TrackNumber = reqTravayoo.Descendants("TransID").Single().Value;
        //        log.LogTypeID = 1;
        //        log.LogType = "Search";
        //        log.SupplierID = 6;
        //        log.logrequestXML = suprequest.ToString();
        //        log.logresponseXML = xmlresponse.ToString();
        //        log.StartTime = startTime;
        //        log.EndTime = DateTime.Now;
        //        try
        //        {
        //            SaveAPILog savelogpro = new SaveAPILog();
        //            savelogpro.SaveAPILogs(log);
        //        }
        //        catch (Exception ex)
        //        {
        //            CustomException ex1 = new CustomException(ex);
        //            ex1.MethodName = "searchresponse";
        //            ex1.PageName = "HotelsProHotelSearch";
        //            ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
        //            ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
        //            SaveAPILog saveex = new SaveAPILog();
        //            saveex.SendCustomExcepToDB(ex1);
        //        }
        //        #endregion
        //        dynamic HotelList = JsonConvert.DeserializeObject(pageContent);
        //        List<XElement> HotelsList = GetHotelListHotelsPro(HotelList, dt, null);

        //        return HotelsList;
        //    }
        //    catch (Exception ex)
        //    {
        //        CustomException ex1 = new CustomException(ex);
        //        ex1.MethodName = "searchresponse";
        //        ex1.PageName = "HotelsProHotelSearch";
        //        ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
        //        ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
        //        SaveAPILog saveex = new SaveAPILog();
        //        saveex.SendCustomExcepToDB(ex1);
        //        return null;
        //    }
        //}
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