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
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.TouricoHolidays
{
    public class Tr_CXLPolicy
    {
        #region Cancellation Policies of Trourico (XML OUT for Travayoo)
        public XElement GetCXLPolicyTourico(XElement request)
        {
            #region Credentials
            string userlogin = string.Empty;
            string pwd = string.Empty;
            string version = string.Empty;
            //userlogin = credential.Descendants("username").FirstOrDefault().Value;
            //pwd = credential.Descendants("password").FirstOrDefault().Value;
            //version = credential.Descendants("version").FirstOrDefault().Value;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(request.Descendants("CustomerID").FirstOrDefault().Value, "2");
            userlogin = suppliercred.Descendants("username").FirstOrDefault().Value;
            pwd = suppliercred.Descendants("password").FirstOrDefault().Value;
            version = suppliercred.Descendants("version").FirstOrDefault().Value;
            #endregion

            #region XML OUT from Tourico Supplier
            Tourico.AuthenticationHeader hd = new Tourico.AuthenticationHeader();
            hd.LoginName = userlogin;//"HOL916";
            hd.Password = pwd;// "111111";
            hd.Version = version;// "5";
            Tourico.HotelFlowClient client = new Tourico.HotelFlowClient();
            Tourico.SearchHotelsByIdRequest req2 = new Tourico.SearchHotelsByIdRequest();
            Tourico.CancellationPoliciesRequest req3 = new Tourico.CancellationPoliciesRequest();
            // req.nResId = 4830398;
            req3.hotelId = Convert.ToInt32(request.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value);
            req3.hotelRoomTypeId = Convert.ToInt32(request.Descendants("Room").Attributes("ID").FirstOrDefault().Value);
            // req.productId = "1040577;13147975;302661";
            req3.dtCheckIn = DateTime.ParseExact(request.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
            req3.dtCheckOut = DateTime.ParseExact(request.Descendants("ToDate").Single().Value, "dd/MM/yyyy", null);
            Tourico.HotelPolicyType1 htlst1 = client.GetCancellationPolicies(hd, "0", Convert.ToInt32(request.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value), Convert.ToInt32(request.Descendants("Room").Attributes("ID").FirstOrDefault().Value), "", req3.dtCheckIn, req3.dtCheckOut);
            #region Log Save
            try
            {
                #region supplier Request Log
                string touricologreq = "";
                try
                {
                    XmlSerializer serializer1 = new XmlSerializer(typeof(Tourico.CancellationPoliciesRequest));

                    using (StringWriter writer = new StringWriter())
                    {
                        serializer1.Serialize(writer, req3);
                        touricologreq = writer.ToString();
                    }
                }
                catch { touricologreq = request.ToString(); }
                #endregion
                XmlSerializer serializer = new XmlSerializer(typeof(Tourico.HotelPolicyType1));
                string touricologres = "";
                using (StringWriter writer = new StringWriter())
                {
                    serializer.Serialize(writer, htlst1);
                    touricologres = writer.ToString();
                }
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(request.Descendants("CustomerID").Single().Value);
                    log.TrackNumber = request.Descendants("TransID").Single().Value;
                    log.LogTypeID = 3;
                    log.LogType = "CXLPolicy";
                    log.SupplierID = 2;
                    log.logrequestXML = touricologreq.ToString();
                    log.logresponseXML = touricologres.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "GetCXLPolicyTourico";
                    ex1.PageName = "Tr_CXLPolicy";
                    ex1.CustomerID = request.Descendants("CustomerID").Single().Value;
                    ex1.TranID = request.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
            }
            catch (Exception ee)
            {
                CustomException ex1 = new CustomException(ee);
                ex1.MethodName = "GetCXLPolicyTourico";
                ex1.PageName = "Tr_CXLPolicy";
                ex1.CustomerID = request.Descendants("CustomerID").Single().Value;
                ex1.TranID = request.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
            }

            #endregion
            IEnumerable<XElement> request1 = request.Descendants("hotelcancelpolicyrequest");
            List<Tourico.CancelPenaltyType1> htlst = htlst1.RoomTypePolicy.CancelPolicy.ToList();
            string hotelid = Convert.ToString(htlst1.hotelId);
            string roomid = Convert.ToString(htlst1.RoomTypePolicy.hotelRoomTypeId);
            string checkin = Convert.ToString(htlst1.RoomTypePolicy.CheckIn);
            string checkout = Convert.ToString(htlst1.RoomTypePolicy.CheckOut);
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            XElement searchdoc = new XElement(
              new XElement(soapenv + "Envelope",
                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                        new XElement(soapenv + "Header",
                         new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                         new XElement("Authentication",
                             new XElement("AgentID", ""),
                             new XElement("UserName", ""),
                             new XElement("Password", ""),
                             new XElement("ServiceType", ""),
                             new XElement("ServiceVersion", ""))),
                         new XElement(soapenv + "Body",
                 new XElement(request1.Single()),
                   new XElement("HotelDetailwithcancellationResponse",
                       new XElement("Hotels",
                           new XElement("Hotel",
                                   new XElement("HotelID", Convert.ToString(hotelid)),
                                   new XElement("HotelName", Convert.ToString(""))
                                   , new XElement("HotelImgSmall", Convert.ToString("")),
                                   new XElement("HotelImgLarge", Convert.ToString("")),
                                   new XElement("MapLink", ""),
                                   new XElement("DMC", "Tourico"),
                                   new XElement("Currency", ""),
                                   new XElement("Offers", "")
                                   , new XElement("Rooms",
                                       GetHotelRoomsTourico(htlst, roomid, checkin, checkout, request.Descendants("CurrencyName").Single().Value, request.Descendants("PerNightRoomRate").FirstOrDefault().Value, request.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value)
                                       )
            ))
          ))));
            return searchdoc;
            #endregion
            
        }
        #endregion
        #region Hotel Room's Details from Tourico Holidays
        private IEnumerable<XElement> GetHotelRoomsTourico(List<Tourico.CancelPenaltyType1> htlist, string roomid, string checkin, string checkout, string CurrencyName, string PerNightRoomRate, string TotalRoomRate)
        {
            #region Hotel Room's Details from Tourico Holidays
            List<XElement> htrm = new List<XElement>();
            for (int i = 0; i < htlist.Count(); i++)
            {
                htrm.Add(new XElement("Room",
                    new XAttribute("ID", Convert.ToString(roomid)),
                    new XAttribute("RoomType", Convert.ToString("")),
                    new XAttribute("PerNightRoomRate", Convert.ToString(PerNightRoomRate)),
                    new XAttribute("TotalRoomRate", Convert.ToString(TotalRoomRate)),
                    new XAttribute("LastCancellationDate", ""),
                    new XElement("CancellationPolicies",
                         GetRoomCancellationPolicyTourico(htlist, checkin, checkout, CurrencyName, PerNightRoomRate, TotalRoomRate))
                    ));
                break;
            };
            return htrm;
            #endregion
        }
        #endregion
        #region Room's Cancellation Policies from Tourico Holidays
        private IEnumerable<XElement> GetRoomCancellationPolicyTourico(List<Tourico.CancelPenaltyType1> cancellationpolicy, string checkin, string checkout, string CurrencyName, string PerNightRoomRate, string TotalRoomRate)
        {
            #region Room's Cancellation Policies from Tourico Holidays
            List<XElement> htrm = new List<XElement>();
            List<Tourico.CancelPenaltyType1> roomlist = cancellationpolicy;
            TimeSpan t = Convert.ToDateTime(checkout) - Convert.ToDateTime(checkin);
            double NrOfnights = t.TotalDays;
            //PerNightRoomRate = Convert.ToString(Convert.ToDecimal(TotalRoomRate) / Convert.ToDecimal(NrOfnights));
            #region Last Cancellation Date
            try
            {
                string currencycode1 = string.Empty;
                string lastcxl = string.Empty;
                for (int i = 0; i < roomlist.Count(); i++)
                {

                    DateTime checkindt = Convert.ToDateTime(checkin);
                    DateTime checkoutdt = Convert.ToDateTime(checkout);
                    string timein = Convert.ToString(roomlist[i].Deadline.OffsetTimeUnit);
                    double totalhour = Convert.ToDouble(roomlist[i].Deadline.OffsetUnitMultiplier);
                    string arrival = Convert.ToString(roomlist[i].Deadline.OffsetDropTime);
                    TimeSpan days = TimeSpan.FromHours(totalhour);
                    DateTime canceldate = checkindt.AddDays(-days.TotalDays);
                    string amount = Convert.ToString(roomlist[i].AmountPercent.Amount);
                    string basistype = Convert.ToString(roomlist[i].AmountPercent.BasisType);
                    if (roomlist[i].AmountPercent.CurrencyCode != null)
                    {
                        currencycode1 = Convert.ToString(roomlist[i].AmountPercent.CurrencyCode);
                    }
                    else
                    {
                        currencycode1 = CurrencyName;
                    }
                    int numberofngt = Convert.ToInt32(roomlist[i].AmountPercent.NmbrOfNights);
                    int percent = Convert.ToInt32(roomlist[i].AmountPercent.Percent);
                    string amountapplicabl = string.Empty;
                    string bybefore = string.Empty;


                    if (timein == "Hour" && arrival == "AfterBooking" && numberofngt > 0 && basistype == "Nights")
                    {
                        canceldate = System.DateTime.Now;
                    }
                    else if (timein == "Hour" && arrival == "AfterBooking" && percent > 0 && basistype == "FullStay")
                    {
                        canceldate = System.DateTime.Now;
                    }
                    else if (timein == "Hour" && arrival == "AfterBooking" && amount != null && basistype == "FullStay")
                    {
                        canceldate = System.DateTime.Now;
                    }

                    else
                    {

                    }

                    if (i == 0)
                    {
                        lastcxl = canceldate.ToString("dd/MM/yyyy");
                    }
                    if (DateTime.ParseExact(lastcxl, "dd/MM/yyyy", null) <= Convert.ToDateTime(canceldate))
                    {

                    }
                    else
                    {
                        lastcxl = canceldate.ToString("dd/MM/yyyy");
                    }
                }
                lastcxl = Convert.ToString(DateTime.ParseExact(lastcxl, "dd/MM/yyyy", null).AddDays(-1).ToString("dd/MM/yyyy"));
                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on before " + lastcxl + "  will apply " + currencycode1 + " 0 Cancellation fee"
                        , new XAttribute("LastCancellationDate", Convert.ToString(lastcxl))
                        , new XAttribute("ApplicableAmount", "0")
                        , new XAttribute("NoShowPolicy", "0")));
            }
            catch (Exception ex)
            {

            }
            #endregion

            #region CXL Policy
            for (int i = 0; i < roomlist.Count(); i++)
            {
                string NoShowPolicy = "0";

                string currencycode = string.Empty;
                DateTime checkindt = Convert.ToDateTime(checkin);
                DateTime checkoutdt = Convert.ToDateTime(checkout);
                string timein = Convert.ToString(roomlist[i].Deadline.OffsetTimeUnit);
                double totalhour = Convert.ToDouble(roomlist[i].Deadline.OffsetUnitMultiplier);
                string arrival = Convert.ToString(roomlist[i].Deadline.OffsetDropTime);
                TimeSpan days = TimeSpan.FromHours(totalhour);
                DateTime canceldate = checkindt.AddDays(-days.TotalDays);
                string amount = Convert.ToString(roomlist[i].AmountPercent.Amount);
                string basistype = Convert.ToString(roomlist[i].AmountPercent.BasisType);
                if (roomlist[i].AmountPercent.CurrencyCode != null)
                {
                    currencycode = Convert.ToString(roomlist[i].AmountPercent.CurrencyCode);
                }
                else
                {
                    currencycode = CurrencyName;
                }
                int numberofngt = Convert.ToInt32(roomlist[i].AmountPercent.NmbrOfNights);
                int percent = Convert.ToInt32(roomlist[i].AmountPercent.Percent);
                string amountapplicabl = string.Empty;
                string bybefore = string.Empty;
                if ((roomlist[i].Deadline.OffsetUnitMultiplier == 0) && (arrival == "BeforeArrival"))
                {
                    NoShowPolicy = "1";
                }

                if (timein == "Hour" && arrival == "BeforeArrival" && numberofngt > 0 && basistype == "Nights")
                {
                    bybefore = "before";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(PerNightRoomRate) * numberofngt);
                }
                else if (timein == "Hour" && arrival == "BeforeArrival" && numberofngt > 0 && basistype == "Nights")
                {
                    bybefore = "before";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(PerNightRoomRate) * numberofngt);
                }
                else if (timein == "Hour" && arrival == "BeforeArrival" && percent > 0 && basistype == "FullStay")
                {
                    bybefore = "before";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(TotalRoomRate) * percent / 100);
                }
                else if (timein == "Hour" && arrival == "BeforeArrival" && percent > 0 && basistype == "FullStay")
                {
                    bybefore = "before";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(TotalRoomRate) * percent / 100);
                }
                else if (timein == "Hour" && arrival == "AfterBooking" && numberofngt > 0 && basistype == "Nights")
                {
                    canceldate = System.DateTime.Now;
                    bybefore = "after";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(PerNightRoomRate) * numberofngt);
                }
                else if (timein == "Hour" && arrival == "BeforeArrival" && percent > 0 && basistype == "FullStay")
                {
                    bybefore = "before";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(TotalRoomRate) * percent / 100);
                }
                else if (timein == "Hour" && arrival == "AfterBooking" && percent > 0 && basistype == "FullStay")
                {
                    canceldate = System.DateTime.Now;
                    bybefore = "after";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(TotalRoomRate) * percent / 100);
                }
                else if (timein == "Hour" && arrival == "AfterBooking" && amount != null && basistype == "FullStay")
                {
                    canceldate = System.DateTime.Now;
                    bybefore = "after";
                    amountapplicabl = amount;
                }
                else if (timein == "Hour" && arrival == "BeforeArrival" && amount != null && basistype == "FullStay")
                {

                    bybefore = "before";
                    amountapplicabl = amount;
                }
                else
                {
                    bybefore = "by or before";
                    amountapplicabl = TotalRoomRate;
                }
                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on after " + canceldate.ToString("dd/MM/yyyy") + "  will apply " + currencycode + " " + amountapplicabl + "  Cancellation fee"
                    , new XAttribute("LastCancellationDate", Convert.ToString(canceldate.ToString("dd/MM/yyyy")))
                    , new XAttribute("ApplicableAmount", amountapplicabl)
                    , new XAttribute("NoShowPolicy", NoShowPolicy)));
            };
            return htrm;

            #endregion
            #endregion
        }
        #endregion
    }
}