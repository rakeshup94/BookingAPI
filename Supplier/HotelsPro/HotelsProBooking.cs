using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.HotelsPro;

namespace TravillioXMLOutService.App_Code
{
    public class HotelsProBooking
    {
        XElement reqtrv = null;
        #region Hotel Booking Request
        public XElement HotelBookingHotelsPro(XElement req)
        {
            XElement hotelBooking = null;
            string bookingresp = string.Empty;
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SuppliersID").Single().Value;
            try
            {
                reqtrv = req;
                List<XElement> paxdetail = req.Descendants("Room").ToList();
                string paxdetails = getpaxdetails(paxdetail);
                string url = string.Empty;
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "6");
                url = suppliercred.Descendants("bookingendpoint").FirstOrDefault().Value;
                #endregion
                string code = Convert.ToString(req.Descendants("Room").Attributes("RoomTypeID").FirstOrDefault().Value);
                url = url + "/" + code;               
                try
                {
                    APILogDetail logreq = new APILogDetail();
                    logreq.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                    logreq.TrackNumber = req.Descendants("TransactionID").Single().Value;
                    logreq.LogTypeID = 5;
                    logreq.LogType = "Book";
                    logreq.SupplierID = 6;
                    logreq.logrequestXML = req.ToString();
                    logreq.logresponseXML = "";
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SaveAPILogs(logreq);
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelBookingHotelsPro";
                    ex1.PageName = "HotelsProBooking";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransactionID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                dynamic resp = bookingresponse(url, paxdetails);
                bookingresp = resp;
                try
                {
                    string suprequest = url + "postedData" + paxdetails;
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                    log.TrackNumber = req.Descendants("TransactionID").Single().Value;
                    log.LogTypeID = 5;
                    log.LogType = "Book";
                    log.SupplierID = 6;
                    log.logrequestXML = suprequest.ToString();
                    log.logresponseXML = bookingresp.ToString();
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelBookingHotelsPro";
                    ex1.PageName = "HotelsProBooking";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransactionID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                resp = Newtonsoft.Json.JsonConvert.DeserializeObject(resp);
                #region Booking Response
                IEnumerable<XElement> request = req.Descendants("HotelBookingRequest").ToList();
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";

                string status = string.Empty;
                dynamic hpstatus = resp.status;
                //CONFIRMED
                if (hpstatus == "succeeded")
                {
                    status = "Success";
                }
                else
                {
                    status = hpstatus;
                }
                string hotelid = string.Empty;
                string totalprice = string.Empty;
                string currency = string.Empty;
                string confirmationnumber = string.Empty;
                hotelid = resp.hotel_code;
                totalprice = resp.price;
                currency = resp.currency;
                //confirmationnumber = resp.confirmation_numbers[0].confirmation_number;
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
                                           new XElement("HotelBookingResponse",
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
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelBookingHotelsPro";
                ex1.PageName = "HotelsProBooking";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransactionID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #region Server not responding "HotelsPro"
                IEnumerable<XElement> request = req.Descendants("HotelBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement bookingdoc = new XElement(
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
                       new XElement("HotelBookingResponse",
                           new XElement("ErrorTxt", bookingresp)
                                   )
                               )
              ));
                return bookingdoc;
                #endregion
            }
        }
        #endregion

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
        #region Booking Method
        public string bookingresponse(string url, string postData)
        {
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
                XElement suppliercred = supplier_Cred.getsupplier_credentials(reqtrv.Descendants("CustomerID").FirstOrDefault().Value, "6");
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

                return pageContent;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "bookingresponse";
                ex1.PageName = "HotelsProBooking";
                ex1.CustomerID = reqtrv.Descendants("CustomerID").Single().Value;
                ex1.TranID = reqtrv.Descendants("TransactionID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return "An error occurred: " + ex.Message;

            }
        }
        #endregion
        #region Booking Request Pax Details
        public string getpaxdetails(List<XElement> req)
        {
            List<XElement> rooms = req;
            string paxdetails = string.Empty;
            paxdetails = "agency_ref_id=" + reqtrv.Descendants("TransID").FirstOrDefault().Value;
            int roomcount = 1;
            for (int i = 0; i < req.Count(); i++)
            {
                List<XElement> passnger = rooms[i].Descendants("PaxInfo").ToList();
                for (int j = 0; j < passnger.Count(); j++)
                {
                    string guesttype = passnger[j].Descendants("GuestType").FirstOrDefault().Value;
                    string title = passnger[j].Descendants("Title").FirstOrDefault().Value;
                    string firstname = passnger[j].Descendants("FirstName").FirstOrDefault().Value;
                    string middlename = passnger[j].Descendants("MiddleName").FirstOrDefault().Value;
                    string lastname = passnger[j].Descendants("LastName").FirstOrDefault().Value;
                    if (guesttype == "Adult")
                    {
                        paxdetails = paxdetails + "&name=" + roomcount + "," + title + " " + firstname + " " + middlename + "," + lastname + ",adult";
                    }
                    else
                    {
                        string childage = passnger[j].Descendants("Age").FirstOrDefault().Value;
                        paxdetails = paxdetails + "&name=" + roomcount + "," + title + " " + firstname + " " + middlename + "," + lastname + ",child," + childage + "";
                    }
                }
                roomcount++;
            }
            return paxdetails;
        }
        #endregion
                
    }
}