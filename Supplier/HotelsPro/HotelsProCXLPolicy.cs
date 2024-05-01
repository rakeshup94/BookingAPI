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
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.HotelsPro;

namespace TravillioXMLOutService.App_Code
{
    public class HotelsProCXLPolicy
    {
        string checkindate = string.Empty;
        #region HotelCXLPolicy HotelsPro (XML OUT for Travayoo)
        public XElement HotelCXLPolicyHotelsPro(XElement req)
        {
            XElement hotelprebookresponse = null;
            XElement hotelpreBooking = null;
            string url = string.Empty;
            IEnumerable<XElement> request = req.Descendants("hotelcancelpolicyrequest").ToList();
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            checkindate = req.Descendants("FromDate").FirstOrDefault().Value;
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            try
            {
                string hotelid = string.Empty;
                string sessionid = string.Empty;
                string productcode = string.Empty;
                hotelid = req.Descendants("HotelID").SingleOrDefault().Value;
                sessionid = req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                productcode = req.Descendants("Room").FirstOrDefault().Attribute("ID").Value;
                #region Changes in cxl policy
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "6");
                url = suppliercred.Descendants("provisionendpoint").FirstOrDefault().Value;
                #endregion
                string code = Convert.ToString(req.Descendants("Room").Attributes("ID").FirstOrDefault().Value);
                url = url+"/" + code + "";
                #endregion
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                string username1 = string.Empty;
                string password1 = string.Empty;
                username1 = suppliercred.Descendants("username").FirstOrDefault().Value;
                password1 = suppliercred.Descendants("password").FirstOrDefault().Value;
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username1 + ":" + password1));
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
                            log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                            log.TrackNumber = req.Descendants("TransID").Single().Value;
                            log.LogTypeID = 3;
                            log.LogType = "CXLPolicy";
                            log.SupplierID = 6;
                            log.logrequestXML = suprequest.ToString();
                            log.logresponseXML = soapResult.ToString();
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SaveAPILogs(log);
                        }
                        catch (Exception ex)
                        {
                            #region Exception
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "HotelCXLPolicyHotelsPro";
                            ex1.PageName = "HotelsProCXLPolicy";
                            ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                            ex1.TranID = req.Descendants("TransID").Single().Value;
                            //APILog.SendCustomExcepToDB(ex1);
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                            #endregion
                        }
                    }
                    dynamic hotellist = Newtonsoft.Json.JsonConvert.DeserializeObject(soapResult);
                    List<XElement> hotelavailabilityresponse = GetHotelListHotelsPro(hotellist, productcode);
                    hotelprebookresponse = hotelavailabilityresponse.FirstOrDefault();
                    #region CXL Response
                    #region XML OUT
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
                                               new XElement("HotelDetailwithcancellationResponse",
                                                   new XElement("Hotels",
                                                       hotelprebookresponse
                                      )))));

                    #endregion
                    #endregion
                }
                return hotelpreBooking;
            }
            catch (WebException webex)
            {
                #region Exception
                WebResponse errResp = webex.Response;
                using (Stream respStream = errResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream);
                    string text = reader.ReadToEnd();
                    #region Exception
                    CustomException ex1 = new CustomException(webex);
                    ex1.MethodName = "HotelCXLPolicyHotelsPro";
                    ex1.PageName = "HotelsProCXLPolicy";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    try
                    {
                        string suprequest = url;
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 3;
                        log.LogType = "CXLPolicy";
                        log.SupplierID = 6;
                        log.logrequestXML = suprequest.ToString();
                        log.logresponseXML = text.ToString();
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception eeeee)
                    { }
                    #endregion
                    XElement searchdoc = new XElement(
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
                                       new XElement("HotelDetailwithcancellationResponse",
                                           new XElement("ErrorTxt", "No Policy Found")
                                                   )
                                               )
                              ));
                    return searchdoc;
                }
                #endregion
            }
        }
        #endregion
        #region HotelsPro
        #region HotelsPro Hotel Listing
        private IEnumerable<XElement> GetHotelListHotelsPro(dynamic hotel, string productcode)
        {
            #region HotelsPro
            List<XElement> hotellst = new List<XElement>();

            try
            {
                //Int32 length = Convert.ToInt32(hotel.count.Value);
                for (int i = 0; i < 1; i++)
                {

                    //string pcode = productcode;
                    //var result = ((IEnumerable<dynamic>)hotel.results).Cast<dynamic>()
                    //            .Where(p => p.code == pcode);



                    //dynamic roomlist = result.FirstOrDefault();
                    //dynamic roomlist = hotel.rooms;

                    hotellst.Add(new XElement("Hotel",
                                           new XElement("HotelID", Convert.ToString(hotel.hotel_code.Value)),
                                                       new XElement("HotelName", Convert.ToString("")),
                                                       new XElement("HotelImgSmall", Convert.ToString("")),
                                                       new XElement("HotelImgLarge", Convert.ToString("")),
                                                       new XElement("MapLink", ""),
                                                       new XElement("DMC", "HotelsPro"),
                                                       new XElement("Currency", ""),
                                                       new XElement("Offers", "")
                                                       , new XElement("Rooms",
                                                    GetHotelRoomListingHotelsPro(hotel)
                                                   )
                    ));
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
        private IEnumerable<XElement> GetHotelRoomListingHotelsPro(dynamic hotellist)
        {
            List<XElement> str = new List<XElement>();
            //dynamic cxlpolicies = hotellist.policies;
            //string totp = hotellist.price;
            //decimal totalprice = Convert.ToDecimal(totp);
            string currency = hotellist.currency;

            #region CXL Policy
            str.Add(new XElement("Room",
                 new XAttribute("ID", Convert.ToString("")),
                 new XAttribute("RoomType", Convert.ToString("")),
                 new XAttribute("MealPlanPrice", ""),
                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                 new XAttribute("TotalRoomRate", Convert.ToString("")),
                 new XAttribute("CancellationDate", ""),
                 new XElement("CancellationPolicies",
             GetRoomCancellationPolicyHotelsPro(hotellist, currency))
                 ));
            #endregion
            return str;

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
                    int totpercent = 100;
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