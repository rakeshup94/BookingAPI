using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.HotelsPro
{
    public class HotelsProImport:IDisposable
    {
        XElement reqtrv = null;
        #region Import Booking (HotelsPro)
        public XElement importbooking_hotelspro(XElement req)
        {
            XElement hotelBooking = null;
            string bookingresp = string.Empty;
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            try
            {
                reqtrv = req;
                #region Credentials
                string url = string.Empty;
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "6");
                url = suppliercred.Descendants("importBooking").FirstOrDefault().Value;
                #endregion
                string bookingid = req.Descendants("ConfirmationNumber").FirstOrDefault().Value;
                url = url + "/" + bookingid;
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                string usernamep = string.Empty;
                string passwordp = string.Empty;
                usernamep = suppliercred.Descendants("username").FirstOrDefault().Value;
                passwordp = suppliercred.Descendants("password").FirstOrDefault().Value;
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(usernamep + ":" + passwordp));
                webRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
                webRequest.ContentType = "application/x-www-form-urlencoded";
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
                            log.customerID = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                            log.TrackNumber = req.Descendants("TransID").Single().Value;
                            log.LogTypeID = 7;
                            log.LogType = "Import";
                            log.SupplierID = 6;
                            log.logrequestXML = suprequest.ToString();
                            log.logresponseXML = soapResult.ToString();
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SaveAPILogs(log);
                        }
                        catch (Exception exx)
                        {
                            CustomException ex1 = new CustomException(exx);
                            ex1.MethodName = "importbooking_hotelspro";
                            ex1.PageName = "HotelsProImport";
                            ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                            ex1.TranID = req.Descendants("TransID").Single().Value;
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                        }
                    }
                    dynamic resp = Newtonsoft.Json.JsonConvert.DeserializeObject(soapResult);                    
                    #region Supplier Response

                    #region Booking Response
                    IEnumerable<XElement> request = req.Descendants("HotelBookingRequest").ToList();
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";

                    string status = string.Empty;
                    dynamic hpstatus = resp.status;
                    
                    status = hpstatus;
                    
                    string hotelid = string.Empty;
                    string totalprice = string.Empty;
                    string currency = string.Empty;
                    string confirmationnumber = string.Empty;
                    hotelid = resp.hotel_code;
                    totalprice = resp.price;
                    currency = resp.currency;
                    confirmationnumber = resp.code;
                    #region XML OUT

                    List<XElement> htreservationlist = req.Descendants("Room").ToList();

                    hotelBooking = new XElement(
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
                                               new XElement("HotelImportBookingResponse",
                                                   new XElement("Hotels",
                                                       new XElement("HotelID", Convert.ToString(hotelid)),
                                                       new XElement("HotelName", Convert.ToString("")),
                                                       new XElement("FromDate", Convert.ToString(req.Descendants("FromDate").Single().Value)),
                                                       new XElement("ToDate", Convert.ToString(req.Descendants("ToDate").Single().Value)),
                                                       new XElement("AdultPax", Convert.ToString("")),
                                                       new XElement("ChildPax", Convert.ToString("")),
                                                       new XElement("TotalPrice", Convert.ToString(totalprice)),
                                                       new XElement("CurrencyID", Convert.ToString("")),
                                                       new XElement("CurrencyCode", Convert.ToString(currency)),
                                                       new XElement("MarketID", Convert.ToString("")),
                                                       new XElement("MarketName", Convert.ToString("")),
                                                       new XElement("HotelImgSmall", Convert.ToString("")),
                                                       new XElement("HotelImgLarge", Convert.ToString("")),
                                                       new XElement("MapLink", ""),
                                                       new XElement("VoucherRemark", ""),
                                                       new XElement("TransID", Convert.ToString(req.Descendants("TransID").Single().Value)),
                                                       new XElement("ConfirmationNumber", Convert.ToString(confirmationnumber)),
                                                       new XElement("Status", Convert.ToString(status)),
                                                       new XElement("PassengersDetail",
                                                           new XElement("GuestDetails",
                                                               GetHotelRoomsInfoHotelsPro(htreservationlist)
                                                               )
                                                           )
                                                       )
                                      ))));

                    #endregion
                    #endregion


                    return hotelBooking;
                    
                    #endregion

                }
               

            }
            catch(Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "importbooking_hotelspro";
                ex1.PageName = "HotelsProImport";
                ex1.CustomerID = reqtrv.Descendants("CustomerID").Single().Value;
                ex1.TranID = reqtrv.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                IEnumerable<XElement> request = req.Descendants("HotelImportBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                hotelBooking = new XElement(
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
                       new XElement("HotelImportBookingResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));
                return hotelBooking;
            }
        }
        #region HotelsPro Hotel Room's Info
        private IEnumerable<XElement> GetHotelRoomsInfoHotelsPro(List<XElement> roomlist)
        {
            #region Room's info (HotelsPro)

            List<XElement> str = new List<XElement>();
            for (int i = 0; i < roomlist.Count(); i++)
            {
                XElement passengers = roomlist[i].Descendants("PaxInfo").FirstOrDefault();

                str.Add(new XElement("Room",
                     new XAttribute("ID", Convert.ToString("")),
                                                                  new XAttribute("RoomType", Convert.ToString(roomlist[i].Attribute("RoomType").Value)),
                                                                  new XAttribute("ServiceID", Convert.ToString("")),
                                                                  new XAttribute("MealPlanID", Convert.ToString(roomlist[i].Attribute("MealPlanID").Value)),
                                                                  new XAttribute("MealPlanName", Convert.ToString("")),
                                                                  new XAttribute("MealPlanCode", Convert.ToString("")),
                                                                  new XAttribute("MealPlanPrice", Convert.ToString(roomlist[i].Attribute("MealPlanPrice").Value)),
                                                                  new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                  new XAttribute("RoomStatus", Convert.ToString("true")),
                                                                  new XAttribute("TotalRoomRate", Convert.ToString("")),
                      GetHotelPassengersInfoHotelsPro(passengers),
                     new XElement("Supplements", ""
                         )
                     ));


            };
            return str;



            #endregion
        }
        #endregion
        #region HotelsPro Hotel Passenger's Info
        private IEnumerable<XElement> GetHotelPassengersInfoHotelsPro(XElement htpassengersinfo)
        {
            #region Passenger's info (HotelsPro)

            List<XElement> pssngrlst = new List<XElement>();
            pssngrlst.Add(new XElement("RoomGuest",
                new XElement("GuestType", Convert.ToString("Adult")),
                new XElement("Title", ""),
                new XElement("FirstName", Convert.ToString(htpassengersinfo.Descendants("Title").SingleOrDefault().Value + " " + htpassengersinfo.Descendants("FirstName").SingleOrDefault().Value + " " + htpassengersinfo.Descendants("MiddleName").SingleOrDefault().Value + " " + htpassengersinfo.Descendants("LastName").SingleOrDefault().Value)),
                new XElement("MiddleName", ""),
                new XElement("LastName", ""),
                new XElement("IsLead", Convert.ToString("true")),
                new XElement("Age", Convert.ToString(""))
                ));
            return pssngrlst;
            #endregion
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
        #endregion
    }
}