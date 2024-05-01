using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.Extranet
{
    public class ExtBooking
    {
        #region Hotel Booking Request
        public XElement HotelBookingExtranet(XElement req)
        {
            XElement hotelBooking = null;
            XElement hotelconfirmbookingextranet = null;
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SuppliersID").Single().Value;
            try
            {
                XElement roompass = null;
                List<XElement> getroom = req.Descendants("Room").ToList();
                XElement keyreq = new XElement(
                        new XElement("Keys", getroomkey(getroom)));

                HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
                string Title = string.Empty;
                string FirstName = string.Empty;
                // string MiddleName = string.Empty;
                string LastName = string.Empty;
                string fullname = string.Empty;
                Title = req.Descendants("Room").FirstOrDefault().Descendants("PaxInfo").FirstOrDefault().Descendants("Title").SingleOrDefault().Value;
                FirstName = req.Descendants("Room").FirstOrDefault().Descendants("PaxInfo").FirstOrDefault().Descendants("FirstName").SingleOrDefault().Value;
                LastName = req.Descendants("Room").FirstOrDefault().Descendants("PaxInfo").FirstOrDefault().Descendants("LastName").SingleOrDefault().Value;
                fullname = Title + " " + FirstName + " " + LastName;
                if (fullname.Length > 75)
                {
                    #region Exception
                    Exception ex = new Exception();
                    ex.Source = "Invalid Name Length";
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelBookingExtranet";
                    ex1.PageName = "ExtBooking";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransactionID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    return hotelconfirmbookingextranet;
                    #endregion                    
                }
                if (req.Descendants("SpecialRemarks").SingleOrDefault().Value.Length > 150)
                {
                    #region Exception
                    Exception ex = new Exception();
                    ex.Source = "Invalid remarks Length";
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelBookingExtranet";
                    ex1.PageName = "ExtBooking";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransactionID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    return hotelconfirmbookingextranet;
                    #endregion
                }
                #region Credentials
                string exAgentID = string.Empty;
                string exUserName = string.Empty;
                string exPassword = string.Empty;
                string exServiceType = string.Empty;
                string exServiceVersion = string.Empty;
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "3");
                exAgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
                exUserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
                exPassword = suppliercred.Descendants("Password").FirstOrDefault().Value;
                exServiceType = suppliercred.Descendants("ServiceType").FirstOrDefault().Value;
                exServiceVersion = suppliercred.Descendants("ServiceVersion").FirstOrDefault().Value;
                #endregion
                #region Extranet Booking Request/Response
                string requestxml = string.Empty;
                #region Bookable/On-Request
                string onrequeststatus = "1";
                string isbookable = string.Empty;
                // if isbookable is false (booking will be ON-REQUEST).
                // 0 means On Request, 1 means Bookable.
                try
                {
                    List<XElement> totroom = req.Descendants("Room").ToList();
                    for (int i = 0; i < totroom.Count(); i++)
                    {
                        isbookable = totroom[i].Attribute("isavailable").Value;
                        if (isbookable == "false")
                        {
                            onrequeststatus = "0";
                        }
                    }
                }
                catch { }
                try
                {
                    roompass = new XElement("passangers", roompaxes(req.Descendants("Room").ToList()));
                }
                catch { }
                #endregion
                requestxml = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                              "<soapenv:Header xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                                "<Authentication>" +
                                  "<AgentID>" + exAgentID + "</AgentID>" +
                                  "<UserName>" + exUserName + "</UserName>" +
                                  "<Password>" + exPassword + "</Password>" +
                                  "<ServiceType>" + exServiceType + "</ServiceType>" +
                                  "<ServiceVersion>" + exServiceVersion + "</ServiceVersion>" +
                                "</Authentication>" +
                              "</soapenv:Header>" +
                              "<soapenv:Body>" +
                                "<bookRequest>" +
                                  "<Price>" + req.Descendants("TotalAmount").Single().Value + "</Price>" +
                                  "<Guest>" + fullname + "</Guest>" +
                                  roompass +
                                  "<GuestNo>0</GuestNo>" +
                                  "<CustomerID>" + req.Descendants("CustomerID").Single().Value + "</CustomerID>" +
                                  "<RequestID>" + req.Descendants("TransactionID").Single().Value + "</RequestID>" +
                                  "<Response_Type>XML</Response_Type>" +
                                  "<FromDate>" + req.Descendants("FromDate").Single().Value + "</FromDate>" +
                                  "<ToDate>" + req.Descendants("ToDate").Single().Value + "</ToDate>" +
                                  "<PropertyId>" + req.Descendants("HotelID").SingleOrDefault().Value + "</PropertyId>" +
                                  keyreq+
                                  "<CultureId>1</CultureId>" +
                                  "<Status>" + onrequeststatus + "</Status>" +
                                  "<GuestNationalityId>" + req.Descendants("PaxNationality_CountryCode").Single().Value + "</GuestNationalityId>" +
                                  req.Descendants("Rooms").SingleOrDefault().ToString() +
                                  "<SpecialRequest>" + req.Descendants("SpecialRemarks").SingleOrDefault().Value + "</SpecialRequest>" +
                                "</bookRequest>" +
                              "</soapenv:Body>" +
                            "</soapenv:Envelope>";
                #endregion

                #region Request captured
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                    log.TrackNumber = req.Descendants("TransactionID").Single().Value;
                    log.LogTypeID = 5;
                    log.LogType = "Book";
                    log.SupplierID = 3;
                    log.logrequestXML = requestxml.ToString();
                    log.logresponseXML = "";
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelBookingExtranet";
                    ex1.PageName = "ExtBooking";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransactionID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion

                object result = extclient.MakeBookRequestByXML(requestxml, 1);
                XElement doc = null;
                if (result != null)
                {                    
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransactionID").Single().Value;
                        log.LogTypeID = 5;
                        log.LogType = "Book";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = result.ToString();
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelBookingExtranet";
                        ex1.PageName = "ExtBooking";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransactionID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        return hotelconfirmbookingextranet;
                        #endregion
                    }
                    doc = XElement.Parse(result.ToString());
                    hotelBooking = doc.Descendants("Hotel").FirstOrDefault();
                }
                else
                {
                    #region No Response from Supplier
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                    log.TrackNumber = req.Descendants("TransactionID").Single().Value;
                    log.LogTypeID = 5;
                    log.LogType = "Book";
                    log.SupplierID = 3;
                    log.logrequestXML = requestxml.ToString();
                    log.logresponseXML = result.ToString();
                    //APILog.SaveAPILogs(log);
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SaveAPILogs(log);

                    #region Exception
                    IEnumerable<XElement> request2 = req.Descendants("HotelBookingRequest");
                    XNamespace soapenv2 = "http://schemas.xmlsoap.org/soap/envelope/";
                    hotelconfirmbookingextranet = new XElement(
                      new XElement(soapenv2 + "Envelope",
                                new XAttribute(XNamespace.Xmlns + "soapenv", soapenv2),
                                new XElement(soapenv2 + "Header",
                                 new XAttribute(XNamespace.Xmlns + "soapenv", soapenv2),
                                 new XElement("Authentication",
                                     new XElement("AgentID", AgentID),
                                     new XElement("UserName", username),
                                     new XElement("Password", password),
                                     new XElement("ServiceType", ServiceType),
                                     new XElement("ServiceVersion", ServiceVersion))),
                                 new XElement(soapenv2 + "Body",
                                     new XElement(request2.Single()),
                           new XElement("HotelBookingResponse",
                               new XElement("ErrorTxt", "No Response from supplier")
                                       )
                                   )
                  ));
                    #endregion


                    return hotelconfirmbookingextranet;
                    #endregion
                }                     
                               

                #region Booking Response
                IEnumerable<XElement> request = req.Descendants("HotelBookingRequest").ToList();
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";             

                string status = string.Empty;
                //CONFIRMED
                if (hotelBooking.Descendants("status").FirstOrDefault().Value == "Success")
                {
                    status = "Success";
                }
                else
                {
                    status = hotelBooking.Descendants("status").FirstOrDefault().Value;
                }

                #region XML OUT

                List<XElement> htreservationlist = req.Descendants("Room").ToList();

                hotelconfirmbookingextranet = new XElement(
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
                                                   new XElement("HotelID", Convert.ToString(req.Descendants("HotelID").FirstOrDefault().Value)),
                                                   new XElement("HotelName", Convert.ToString(req.Descendants("HotelName").FirstOrDefault().Value)),
                                                   new XElement("FromDate", Convert.ToString(req.Descendants("FromDate").Single().Value)),
                                                   new XElement("ToDate", Convert.ToString(req.Descendants("ToDate").Single().Value)),
                                                   new XElement("AdultPax", Convert.ToString("")),
                                                   new XElement("ChildPax", Convert.ToString("")),
                                                   new XElement("TotalPrice", Convert.ToString(req.Descendants("TotalAmount").Single().Value)),
                                                   new XElement("CurrencyID", Convert.ToString("")),
                                                   new XElement("CurrencyCode", Convert.ToString("")),
                                                   new XElement("MarketID", Convert.ToString("")),
                                                   new XElement("MarketName", Convert.ToString("")),
                                                   new XElement("HotelImgSmall", Convert.ToString("")),
                                                   new XElement("HotelImgLarge", Convert.ToString("")),
                                                   new XElement("MapLink", ""),
                                                   new XElement("VoucherRemark", ""),
                                                   new XElement("TransID", Convert.ToString(req.Descendants("TransID").Single().Value)),
                                                   new XElement("ConfirmationNumber", Convert.ToString(hotelBooking.Descendants("ConfirmationNo").FirstOrDefault().Value)),
                                                   new XElement("Status", Convert.ToString(status)),
                                                   new XElement("PassengersDetail",
                                                       new XElement("GuestDetails",
                                                           GetHotelRoomsInfoExtranet(htreservationlist)
                                                           )
                                                       )
                                                   )
                                  ))));
               
                #endregion
                #endregion


                return hotelconfirmbookingextranet;
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelBookingExtranet";
                ex1.PageName = "ExtBooking";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransactionID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #region Exception
                IEnumerable<XElement> request = req.Descendants("HotelBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                hotelconfirmbookingextranet = new XElement(
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
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));               
                #endregion
                return hotelconfirmbookingextranet;
                #endregion
            }
        }
        #endregion

        #region Extranet Hotel Room's Info
        private IEnumerable<XElement> GetHotelRoomsInfoExtranet(List<XElement> roomlist)
        {
            #region Room's info (Extranet)

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
                      GetHotelPassengersInfoExtranet(passengers),
                     new XElement("Supplements", ""
                         )
                     ));


            };
            return str;



            #endregion
        }
        #endregion

        #region Extranet Hotel Passenger's Info
        private IEnumerable<XElement> GetHotelPassengersInfoExtranet(XElement htpassengersinfo)
        {
            #region Passenger's info (Extranet)

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
        #region Get Room's Keys
        private List<XElement> getroomkey(List<XElement> room)
        {
            #region Bind Room keys
            List<XElement> str = new List<XElement>();

            for (int i = 0; i < room.Count(); i++)
            {
                str.Add(new XElement("Key", Convert.ToString(room[i].Attribute("SessionID").Value))
                );
            }
            return str;
            #endregion
        }
        #endregion
        #region Bind room Pax
        private List<XElement> roompaxes(List<XElement> rooms)
        {
            try
            {
                List<XElement> str = new List<XElement>();
                for (int i = 0; i < rooms.Count(); i++)
                {
                    str.Add(new XElement("room",
                           new XAttribute("count", Convert.ToString(i+1)),
                           new XElement("paxes", roompax(rooms[i].Descendants("PaxInfo").ToList())))
                    );
                }
                return str;
            }
            catch { return null; }
        }
        #endregion
        #region Bind Pax
        private List<XElement> roompax(List<XElement> paxes)
        {
            try
            {
                List<XElement> str = new List<XElement>();
                for (int i = 0; i < paxes.Count(); i++)
                {
                    string islead = "false";
                    string age = "30";
                    string title = "adult";
                    if(i==0)
                    {
                        islead = "true";
                    }
                    if (paxes[i].Descendants("GuestType").FirstOrDefault().Value != "Adult")
                    {
                        age = paxes[i].Descendants("Age").FirstOrDefault().Value;
                        title = "child";
                    }
                    string name = paxes[i].Descendants("Title").FirstOrDefault().Value + " " + paxes[i].Descendants("FirstName").FirstOrDefault().Value + " " + paxes[i].Descendants("MiddleName").FirstOrDefault().Value + " " + paxes[i].Descendants("LastName").FirstOrDefault().Value;
                    str.Add(new XElement("pax",
                           new XElement("guesttype", title),
                           new XElement("name", name),
                            new XElement("age", age),
                            new XElement("islead", islead)
                    ));
                }
                return str;
            }
            catch { return null; }
        }
        #endregion
        #endregion
    }
}