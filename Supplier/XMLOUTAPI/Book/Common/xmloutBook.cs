using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Supplier.XMLOUTAPI.Book;

namespace TravillioXMLOutService.Supplier.XMLOUTAPI.Book.Common
{
    public class xmloutBook : IDisposable
    {
        #region Global vars
        string customerid = string.Empty;
        string dmc = string.Empty;
        int SupplierId = 501;
        string sessionKey = string.Empty;
        string sourcekey = string.Empty;
        string publishedkey = string.Empty;
        XElement reqTravayoo = null;
        #endregion
        #region Book Respones (XML OUT API)
        public XElement book_bookingexpressOUT(XElement req)
        {
            XElement bookresp = null;
            reqTravayoo = req;
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SuppliersID").Single().Value;
            try
            {
                #region Request
                string holdertitle = req.Descendants("Room").FirstOrDefault().Descendants("PaxInfo").FirstOrDefault().Descendants("Title").FirstOrDefault().Value;
                string holderfname = req.Descendants("Room").FirstOrDefault().Descendants("PaxInfo").FirstOrDefault().Descendants("FirstName").FirstOrDefault().Value;
                string holdermname = req.Descendants("Room").FirstOrDefault().Descendants("PaxInfo").FirstOrDefault().Descendants("MiddleName").FirstOrDefault().Value;
                string holderlname = req.Descendants("Room").FirstOrDefault().Descendants("PaxInfo").FirstOrDefault().Descendants("LastName").FirstOrDefault().Value;
                string firstname = holdertitle.Trim().ToString() + " " + holderfname.Trim().ToString() + " " + holdermname.Trim().ToString();
                customerid = req.Descendants("CustomerID").FirstOrDefault().Value;
                XElement request = null;
                request = new XElement("bookingRQ",
                                        new XAttribute("customerID", req.Descendants("CustomerID").FirstOrDefault().Value),
                                        new XAttribute("agencyID", req.Descendants("SubAgentID").FirstOrDefault().Value),
                                        new XAttribute("sessionID", req.Descendants("TransactionID").FirstOrDefault().Value),
                                        new XAttribute("sourceMarket", req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                        new XElement("holder",
                                            new XAttribute("name", firstname),
                                            new XAttribute("surname", holderlname)),
                                        new XElement("remark", req.Descendants("SpecialRemarks").FirstOrDefault().Value),
                                        new XElement("transid", req.Descendants("TransID").FirstOrDefault().Value),
                                        new XElement("hotel",
                                            new XAttribute("code", req.Descendants("HotelID").FirstOrDefault().Value),
                                            new XAttribute("name", req.Descendants("HotelName").FirstOrDefault().Value),
                                            new XAttribute("appkey", req.Descendants("SuppliersID").FirstOrDefault().Value),
                                            new XAttribute("totalRate", req.Descendants("TotalAmount").FirstOrDefault().Value),
                                            new XAttribute("checkIn", req.Descendants("FromDate").FirstOrDefault().Value),
                                            new XAttribute("checkOut", req.Descendants("ToDate").FirstOrDefault().Value),
                                            new XAttribute("currency", req.Descendants("CurrencyCode").FirstOrDefault().Value),
                                            new XAttribute("sessionKey", req.Descendants("sessionKey").FirstOrDefault().Value),
                                            new XAttribute("sourcekey", req.Descendants("sourcekey").FirstOrDefault().Value),
                                            new XAttribute("publishedkey", req.Descendants("publishedkey").FirstOrDefault().Value),
                                            new XElement("rooms", bindroomoutreq(req.Descendants("Room").ToList()))
                                            )
                                      );
                #endregion
                #region Response
                xmlResponseRequest apireq = new xmlResponseRequest();
                string apiresponse = apireq.xmloutHTTPResponse(request, "Book", 5, req.Descendants("TransactionID").FirstOrDefault().Value, customerid, "501");
                XElement apiresp = null;
                try
                {
                    apiresp = XElement.Parse(apiresponse);
                    XElement hoteldetailsresponse = XElement.Parse(apiresponse.ToString());
                    apiresp = RemoveAllNamespaces(hoteldetailsresponse);
                    #region Check Booking Error
                    try
                    {                        
                        int error1 = apiresp.Descendants("Error").Count();
                        string getnam = apiresp.Name.ToString();
                        if (getnam == "Error")
                        {
                            IEnumerable<XElement> requestb = req.Descendants("HotelBookingRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            XElement htlresp = new XElement(
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
                                             new XElement(requestb.Single()),
                                   new XElement("HotelBookingResponse",
                                       new XElement("ErrorTxt", apiresp.Value)
                                               )
                                           )
                          ));
                            return htlresp;
                        }
                        else if (error1 > 0)
                        {
                            IEnumerable<XElement> requestb = req.Descendants("HotelBookingRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            XElement htlresp = new XElement(
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
                                             new XElement(requestb.Single()),
                                   new XElement("HotelBookingResponse",
                                       new XElement("ErrorTxt", apiresp.Descendants("Error").FirstOrDefault().Value)
                                               )
                                           )
                          ));
                            return htlresp;
                        }
                    }
                    catch { }
                    #endregion
                }
                catch { }
                bookresp = hotelbinding(apiresp.Descendants("booking") != null ? apiresp.Descendants("booking").FirstOrDefault() : null);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "book_bookingexpressOUT";
                ex1.PageName = "xmloutBook";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransactionID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #region Exception
                IEnumerable<XElement> request = req.Descendants("HotelBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                bookresp = new XElement(
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
                return bookresp;
                #endregion
            }
            return bookresp;
        }
        #endregion
        #region Hotel Binding
        private XElement hotelbinding(XElement hotelBooking)
        {
            XElement htlresp = null;
            string username = reqTravayoo.Descendants("UserName").Single().Value;
            string password = reqTravayoo.Descendants("Password").Single().Value;
            string AgentID = reqTravayoo.Descendants("AgentID").Single().Value;
            string ServiceType = reqTravayoo.Descendants("ServiceType").Single().Value;
            string ServiceVersion = reqTravayoo.Descendants("ServiceVersion").Single().Value;
            try
            {
                IEnumerable<XElement> request = reqTravayoo.Descendants("HotelBookingRequest").ToList();
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                List<XElement> htreservationlist = reqTravayoo.Descendants("Room").ToList();                
                string status = string.Empty;
                if (hotelBooking.Attribute("status").Value.ToUpper() == "CONFIRMED" || hotelBooking.Attribute("status").Value.ToUpper() == "SUCCESS")
                {
                    status = "Success";
                }
                else
                {
                    status = hotelBooking.Attribute("status").Value;
                }
                htlresp = new XElement(
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
                                               new XElement("HotelID", Convert.ToString(reqTravayoo.Descendants("HotelID").FirstOrDefault().Value)),
                                               new XElement("HotelName", Convert.ToString(reqTravayoo.Descendants("HotelName").FirstOrDefault().Value)),
                                               new XElement("FromDate", Convert.ToString(reqTravayoo.Descendants("FromDate").Single().Value)),
                                               new XElement("ToDate", Convert.ToString(reqTravayoo.Descendants("ToDate").Single().Value)),
                                               new XElement("AdultPax", Convert.ToString("")),
                                               new XElement("ChildPax", Convert.ToString("")),
                                               new XElement("TotalPrice", Convert.ToString(reqTravayoo.Descendants("TotalAmount").Single().Value)),
                                               new XElement("CurrencyID", Convert.ToString("")),
                                               new XElement("CurrencyCode", Convert.ToString("")),
                                               new XElement("MarketID", Convert.ToString("")),
                                               new XElement("MarketName", Convert.ToString("")),
                                               new XElement("HotelImgSmall", Convert.ToString("")),
                                               new XElement("HotelImgLarge", Convert.ToString("")),
                                               new XElement("MapLink", ""),
                                               new XElement("VoucherRemark", ""),
                                               new XElement("TransID", Convert.ToString(reqTravayoo.Descendants("TransID").Single().Value)),
                                               new XElement("ConfirmationNumber", Convert.ToString(hotelBooking.Attribute("bookingID").Value)),
                                               new XElement("Status", Convert.ToString(status)),
                                               new XElement("PassengersDetail",
                                                   new XElement("GuestDetails",
                                                       GetHotelRoomsInfoout(htreservationlist, hotelBooking)
                                                       )
                                                   )
                                               )
                              ))));
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "hotelbinding";
                ex1.PageName = "xmloutBook";
                ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
                ex1.TranID = reqTravayoo.Descendants("TransactionID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #region Exception
                IEnumerable<XElement> request = reqTravayoo.Descendants("HotelBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                htlresp = new XElement(
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
                return htlresp;
                #endregion
            }
            return htlresp;
        }
        #endregion 
        #region Extranet Hotel Room's Info
        private IEnumerable<XElement> GetHotelRoomsInfoout(List<XElement> roomlist, XElement response)
        {
            #region Room's info 
            List<XElement> str = new List<XElement>();
            List<XElement> roomlst = response.Descendants("room").ToList();
            for (int i = 0; i < roomlist.Count(); i++)
            {
                string serviceref = string.Empty;
                try
                {
                    serviceref = roomlst[i].Attribute("serviceref").Value;
                }
                catch { }
                XElement passengers = roomlist[i].Descendants("PaxInfo").FirstOrDefault();
                str.Add(new XElement("Room",
                     new XAttribute("ID", Convert.ToString(roomlist[i].Attribute("RoomTypeID").Value)),
                                                                  new XAttribute("RoomType", Convert.ToString(roomlist[i].Attribute("RoomType").Value)),
                                                                  new XAttribute("ServiceID", Convert.ToString(serviceref)),
                                                                  new XAttribute("MealPlanID", Convert.ToString(roomlist[i].Attribute("MealPlanID").Value)),
                                                                  new XAttribute("MealPlanName", Convert.ToString("")),
                                                                  new XAttribute("MealPlanCode", Convert.ToString("")),
                                                                  new XAttribute("MealPlanPrice", Convert.ToString(roomlist[i].Attribute("MealPlanPrice").Value)),
                                                                  new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                  new XAttribute("RoomStatus", Convert.ToString("true")),
                                                                  new XAttribute("TotalRoomRate", Convert.ToString("")),
                      GetHotelPassengersInfoout(passengers),
                     new XElement("Supplements", ""
                         )
                     ));
            };
            return str;
            #endregion
        }
        #endregion
        #region Extranet Hotel Passenger's Info
        private IEnumerable<XElement> GetHotelPassengersInfoout(XElement htpassengersinfo)
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
        #region Room and Pax Bind (Request)
        private List<XElement> bindroomoutreq(List<XElement> rooms)
        {
            List<XElement> roomlst = new List<XElement>();
            try
            {
                for (int i = 0; i < rooms.Count(); i++)
                {
                    string guestType = string.Empty;
                    roomlst.Add(new XElement("room",
                        new XAttribute("code", rooms[i].Attribute("RoomTypeID").Value),
                        new XAttribute("name", rooms[i].Attribute("RoomType").Value),
                        new XAttribute("rateKey", rooms[i].Attribute("OccupancyID").Value),
                        new XAttribute("requestID", rooms[i].Attribute("SessionID").Value),
                        new XAttribute("termCondition", ""),
                        new XAttribute("boardID", rooms[i].Attribute("MealPlanID").Value),
                        new XAttribute("boardCode", rooms[i].Attribute("MealPlanCode") == null ? "" : rooms[i].Attribute("MealPlanCode").Value),
                        new XAttribute("boardName", rooms[i].Attribute("MealPlanName") == null ? "" : rooms[i].Attribute("MealPlanName").Value),
                        new XAttribute("packaging", ""),
                        new XAttribute("net", rooms[i].Attribute("TotalRoomRate").Value),
                        new XAttribute("allotment", ""),
                        new XAttribute("onRequest", rooms[i].Attribute("isavailable").Value == "true" ? "false" : "true"),
                        new XAttribute("groupID", rooms[i].Descendants("RequestID").FirstOrDefault().Value),
                        new XAttribute("isGroup", ""),
                        new XAttribute("sourcekey", rooms[i].Attribute("sourcekey").Value),
                        new XElement("paxes", bindpaxutreq(rooms[i].Descendants("PaxInfo").ToList(), i + 1))
                        ));
                }
            }
            catch { }
            return roomlst;
        }
        private List<XElement> bindpaxutreq(List<XElement> paxes , int roomNo)
        {
            List<XElement> paxlst = new List<XElement>();
            try
            {
                for (int i = 0; i < paxes.Count(); i++)
                {
                    string guestType = string.Empty; 
                    if(paxes[i].Descendants("GuestType").FirstOrDefault().Value == "Adult")
                    {
                        guestType = "AD";
                    }
                    else if (paxes[i].Descendants("GuestType").FirstOrDefault().Value == "Child")
                    {
                        guestType = "CH";
                    }
                    else
                    {
                        guestType = "IN";
                    }
                    paxlst.Add(new XElement("pax",
                        new XAttribute("roomNo", roomNo),
                        new XAttribute("type", guestType),
                        new XAttribute("age", paxes[i].Descendants("Age").FirstOrDefault().Value),
                        new XAttribute("title", paxes[i].Descendants("Title").FirstOrDefault().Value),
                        new XAttribute("name", paxes[i].Descendants("FirstName").FirstOrDefault().Value + " " + paxes[i].Descendants("MiddleName").FirstOrDefault().Value),
                        new XAttribute("surname", paxes[i].Descendants("LastName").FirstOrDefault().Value)
                        ));
                }
            }
            catch { }
            return paxlst;
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