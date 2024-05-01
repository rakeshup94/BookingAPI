﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.HotelBeds;

namespace TravillioXMLOutService.App_Code
{
    public class ImprtBookingHotelBeds
    {
        #region Credentails of HotelBeds
        string apiKey = string.Empty;
        string Secret = string.Empty;
        #endregion

        #region Hotel Booking Request
        public XElement ImportBookingHotelBeds(XElement req)
        {
            XElement hotelBooking = null;
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SuppliersID").Single().Value;
            try
            {
                //HotelBedsCredential _credential = new HotelBedsCredential();
                //apiKey = _credential.apiKey;
                //Secret = _credential.Secret;
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "4");
                apiKey = suppliercred.Descendants("apiKey").FirstOrDefault().Value;
                Secret = suppliercred.Descendants("Secret").FirstOrDefault().Value;
                string endpoint = suppliercred.Descendants("bookingendpoint").FirstOrDefault().Value;
                #endregion

                string bookingid = req.Descendants("ConfirmationNumber").Single().Value;  //"321-2420783";
                
                endpoint = endpoint+"/" + bookingid + "";

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
                    client.Headers.Add("X-Signature", signature);
                    client.Headers.Add("Api-Key", apiKey);
                    client.Headers.Add("Accept", "application/xml");
                    client.Headers.Add("Content-Type", "application/xml");
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12;
                    response = client.DownloadString(endpoint);

                    XElement hoteldetailsresponse = XElement.Parse(response.ToString());

                    XElement doc = RemoveAllNamespaces(hoteldetailsresponse);

                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.LogTypeID = 7;
                        log.LogType = "Import";
                        log.SupplierID = 4;
                        log.logrequestXML = req.ToString();
                        log.logresponseXML = hoteldetailsresponse.ToString();
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "ImportBookingHotelBeds";
                        ex1.PageName = "ImprtBookingHotelBeds";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }

                    XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                    XElement hotelbookingres = doc.Descendants("booking").FirstOrDefault();

                    hotelBooking = hotelbookingres;

                    #region Booking Response
                    IEnumerable<XElement> request = req.Descendants("HotelBookingRequest").ToList();
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                          
                    string status = string.Empty;
                    //CONFIRMED
                    if (hotelBooking.Attribute("status").Value == "CONFIRMED")
                    {
                        status = "Success";
                    }
                    else
                    {
                        status = hotelBooking.Attribute("status").Value;
                    }

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
                                                       new XElement("HotelID", Convert.ToString(hotelBooking.Descendants("hotel").FirstOrDefault().Attribute("code").Value)),
                                                       new XElement("HotelName", Convert.ToString(hotelBooking.Descendants("hotel").FirstOrDefault().Attribute("name").Value)),
                                                       new XElement("FromDate", Convert.ToString(req.Descendants("FromDate").Single().Value)),
                                                       new XElement("ToDate", Convert.ToString(req.Descendants("ToDate").Single().Value)),
                                                       new XElement("AdultPax", Convert.ToString("")),
                                                       new XElement("ChildPax", Convert.ToString("")),
                                                       new XElement("TotalPrice", Convert.ToString(hotelBooking.Attribute("totalNet").Value)),
                                                       new XElement("CurrencyID", Convert.ToString("")),
                                                       new XElement("CurrencyCode", Convert.ToString(hotelBooking.Attribute("currency").Value)),
                                                       new XElement("MarketID", Convert.ToString("")),
                                                       new XElement("MarketName", Convert.ToString("")),
                                                       new XElement("HotelImgSmall", Convert.ToString("")),
                                                       new XElement("HotelImgLarge", Convert.ToString("")),
                                                       new XElement("MapLink", ""),
                                                       new XElement("VoucherRemark", ""),
                                                       new XElement("TransID", Convert.ToString(req.Descendants("TransID").Single().Value)),
                                                       new XElement("ConfirmationNumber", Convert.ToString(hotelBooking.Attribute("reference").Value)),
                                                       new XElement("Status", Convert.ToString(hotelBooking.Attribute("status").Value)),
                                                       new XElement("PassengersDetail",
                                                           new XElement("GuestDetails",
                                                               GetHotelRoomsInfoHotelBeds(htreservationlist)
                                                               )
                                                           )
                                                       )
                                      ))));
                    //return hotelBooking;
                    #endregion
                    #endregion

                }
                return hotelBooking;
            }
            catch(Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "ImportBookingHotelBeds";
                ex1.PageName = "ImprtBookingHotelBeds";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
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
        #endregion

        #region HotelBeds Hotel Room's Info
        private IEnumerable<XElement> GetHotelRoomsInfoHotelBeds(List<XElement> roomlist)
        {
            #region Room's info (HotelBeds)

            List<XElement> str = new List<XElement>();
            for (int i = 0; i < roomlist.Count(); i++)
            {
                XElement passengers = roomlist[i].Descendants("PaxInfo").FirstOrDefault();

                str.Add(new XElement("Room",
                     new XAttribute("ID", Convert.ToString(roomlist[i].Attribute("RoomTypeID").Value)),
                                                                  new XAttribute("RoomType", Convert.ToString(roomlist[i].Attribute("RoomType").Value)),
                                                                  new XAttribute("ServiceID", Convert.ToString("")),
                                                                  new XAttribute("MealPlanID", Convert.ToString(roomlist[i].Attribute("MealPlanID").Value)),
                                                                  new XAttribute("MealPlanName", Convert.ToString("")),
                                                                  new XAttribute("MealPlanCode", Convert.ToString("")),
                                                                  new XAttribute("MealPlanPrice", Convert.ToString(roomlist[i].Attribute("MealPlanPrice").Value)),
                                                                  new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                  new XAttribute("RoomStatus", Convert.ToString("true")),
                                                                  new XAttribute("TotalRoomRate", Convert.ToString("")),
                      GetHotelPassengersInfoHotelBeds(passengers),
                     new XElement("Supplements", ""
                         )
                     ));


            };
            return str;



            #endregion
        }
        #endregion

        #region HotelBeds Hotel Passenger's Info
        private IEnumerable<XElement> GetHotelPassengersInfoHotelBeds(XElement htpassengersinfo)
        {
            #region Passenger's info (HotelBeds)

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

        #region Hotel Booking Request
        private List<XElement> getroom(List<XElement> room)
        {
            #region Get Total Room
            List<XElement> str = new List<XElement>();

            for (int i = 0; i < room.Count(); i++)
            {
                str.Add(new XElement("room",
                       new XAttribute("rateKey", Convert.ToString(room[i].Attribute("SessionID").Value)),
                       new XElement("paxes", getadults(room[i]), getchildren(room[i])))
                );
            }
            return str;
            #endregion
        }
        private IEnumerable<XElement> getadults(XElement room)
        {
            #region Get Total Adults
            List<XElement> str = new List<XElement>();
            List<XElement> rcount = room.Descendants("PaxInfo").Where(x => x.Descendants("GuestType").FirstOrDefault().Value == "Adult").ToList();

            for (int i = 0; i < rcount.Count(); i++)
            {
                str.Add(new XElement("pax",
                       new XAttribute("roomId", "1"),
                       new XAttribute("type", "AD"),
                       new XAttribute("age", ""),
                       new XAttribute("name", Convert.ToString(rcount[i].Descendants("FirstName").FirstOrDefault().Value)),
                       new XAttribute("surname", Convert.ToString(rcount[i].Descendants("LastName").FirstOrDefault().Value)))
                );
            }

            return str;
            #endregion
        }
        private IEnumerable<XElement> getchildren(XElement room)
        {
            #region Get Total Children
            List<XElement> str = new List<XElement>();
            List<XElement> tchild = room.Descendants("PaxInfo").Where(x => x.Descendants("GuestType").FirstOrDefault().Value == "Child").ToList();
            for (int i = 0; i < tchild.Count(); i++)
            {
                str.Add(new XElement("pax",
                        new XAttribute("roomId", "1"),
                       new XAttribute("type", "CH"),
                       new XAttribute("age", Convert.ToString(tchild[i].Descendants("Age").FirstOrDefault().Value)),
                       new XAttribute("name", Convert.ToString(tchild[i].Descendants("FirstName").FirstOrDefault().Value)),
                       new XAttribute("surname", Convert.ToString(tchild[i].Descendants("LastName").FirstOrDefault().Value)))                       
                );
            }
            return str;
            #endregion
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
    }
}