using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Net;
using System.Xml;
using TravillioXMLOutService.App_Code;
using TravillioXMLOutService.Models;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TravillioXMLOutService.Supplier.DotW;
using TravillioXMLOutService.Supplier.JacTravel;
using TravillioXMLOutService.Supplier.Extranet;
using TravillioXMLOutService.Supplier.RTS;
using TravillioXMLOutService.Models.Darina;
using TravillioXMLOutService.Supplier.Miki;
using TravillioXMLOutService.Supplier.Restel;
using TravillioXMLOutService.Supplier.Darina;
using TravillioXMLOutService.Supplier.TouricoHolidays;
using TravillioXMLOutService.Supplier.Juniper;
using TravillioXMLOutService.Supplier.Godou;
using TravillioXMLOutService.Supplier.SalTours;
using TravillioXMLOutService.Supplier.SunHotels;
using TravillioXMLOutService.Supplier.Hoojoozat;
using TravillioXMLOutService.Supplier.TravelGate;
using TravillioXMLOutService.Supplier.TBOHolidays;
using TravillioXMLOutService.Supplier.VOT;
using TravillioXMLOutService.Supplier.EBookingCenter;
using TravillioXMLOutService.Supplier.XMLOUTAPI.Book.Common;

namespace TravillioXMLOutService.App_Code
{
    public class TrvHotelBooking:IDisposable
    {
        #region Logs
        public void WriteToFile(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    writer.WriteLine("---------------------------Confirm Booking Response-----------------------------------------");
                    writer.Close();
                }
            }
            catch(Exception ex)
            {

            }
        }
        #endregion
        #region Booking (XML OUT for Travayoo)
        public XElement HotelBookingConfirmation(XElement req)
        {
            #region XML OUT
            #region Header Authentication and header's parameters
            HeaderAuth headercheck = new HeaderAuth();
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SuppliersID").Single().Value;
            #endregion
            if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
            {
                #region Booking
                #region Supplier Credentials
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(supplier_Cred).TypeHandle);
                #endregion
                try
                {
                    #region Darina
                    if (supplierid == "1")
                    {
                        #region Darina
                        dr_Booking darreq = new dr_Booking();
                        XElement hotelBooking = darreq.bookingresponsedarina(req);
                        return hotelBooking;
                        #endregion
                    }
                    #endregion
                    #region Tourico
                    if (supplierid=="2")
                    {
                        #region Tourico
                        Tr_Book trreq = new Tr_Book();
                        XElement hotelBooking = trreq.HotelBooking_Tourico(req);
                        return hotelBooking;
                        #endregion
                    }
                    #endregion
                    #region Hotel Extranet
                    if (supplierid == "3")
                    {
                        #region Extranet
                        ExtBooking extreq = new ExtBooking();
                        XElement hotelBooking = extreq.HotelBookingExtranet(req);
                        return hotelBooking;
                        #endregion
                    }
                    #endregion
                    #region HotelBeds
                    if (supplierid == "4")
                    {
                         #region HotelBeds
                        BookingHotelBeds hbreq = new BookingHotelBeds();

                        XElement hotelBooking = hbreq.HotelBookingHotelBeds(req);

                        return hotelBooking;
                         #endregion
                    }
                    #endregion
                    #region DOTW
                    if (supplierid == "5")
                    {
                        #region DOTW
                        DotwService dotwObj = new DotwService(req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement bookingres = dotwObj.CNFBookingSeaReq(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region HotelsPro
                    if (supplierid == "6")
                    {
                        #region HotelsPro
                        HotelsProBooking hbreq = new HotelsProBooking();

                        XElement hotelBooking = hbreq.HotelBookingHotelsPro(req);

                        return hotelBooking;
                        #endregion
                    }
                    #endregion
                    #region Travco
                    if (supplierid == "7")
                    {
                        #region Travco
                        Travco trvObj = new Travco(req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement bookingres = trvObj.HotelBooking(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region JacTravel
                    if (supplierid == "8")
                    {
                        #region JacTravel
                        Jac_HtlBokng jacreq = new Jac_HtlBokng();
                        XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "8");
                        string Login = suppliercred.Descendants("Login").FirstOrDefault().Value;
                        string Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                        string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
                        XElement hotelBooking = jacreq.BokngRequestXml(Login, Password, url, req);
                        return hotelBooking;
                        #endregion
                    }
                    #endregion
                    #region RTS
                    if (supplierid == "9")
                    {                       
                        try
                        {
                            foreach (var item in req.Descendants("HotelBookingRequest"))
                            {
                                string GuestCountyCode = item.Element("PaxNationality_CountryCode").Value != string.Empty ? item.Element("PaxNationality_CountryCode").Value.ToUpper() : string.Empty;
                                RTS_HTlBooking RTSreq = new RTS_HTlBooking();
                                XElement hotelBooking = RTSreq.HtlBooking(req, GuestCountyCode);
                                return hotelBooking;
                            }                            
                        }
                        catch(Exception ex)
                        {
                            #region Exception
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "HotelBookingConfirmation";
                            ex1.PageName = "TrvHotelBooking";
                            ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                            ex1.TranID = req.Descendants("TransactionID").Single().Value;
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);                           
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
                                       new XElement("ErrorTxt", ex.Message)
                                               )
                                           )
                          ));
                            return bookingdoc;
                            #endregion
                        }
                    }
                    #endregion
                    #region MIKI
                    if (supplierid == "11")
                    {
                        #region MIKI
                        MikiInternal mikiobj = new MikiInternal();
                        XElement bookingres = mikiobj.BookingRequest(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region Restel
                    if (supplierid == "13")
                    {
                        #region Restel
                        RestelServices rs = new RestelServices();
                        XElement bookingres = rs.BookingRequest(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region JuniperW2M
                    if (supplierid == "16")
                    {
                        #region Juniper
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(16, customerid);
                        XElement bookingres = rs.BookingRequest(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region EgyptExpress
                    if (supplierid == "17")
                    {
                        #region EgyptExpress
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(17, customerid);
                        XElement bookingres = rs.BookingRequest(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region Sal Tours
                    if (supplierid == "19")
                    {
                        SalServices sser = new SalServices();
                        XElement bookingres = sser.Booking(req);
                        return bookingres;
                    }
                    #endregion
                    #region TBO Holidays
                    if (supplierid == "21")
                    {
                        TBOServices tbs = new TBOServices();
                        XElement bookingres = tbs.Booking(req);
                        return bookingres;
                    }
                    #endregion
                    #region LOH
                    if (supplierid == "23")
                    {
                        #region LOH
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(23, customerid);
                        XElement bookingres = rs.BookingRequest(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region Gadou
                    if (supplierid == "31")
                    {
                        //GodouServices gds = new GodouServices();
                        //XElement bookingres = gds.BookingConfirmation(req);
                        //return bookingres;
                        #region Gadou
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(31, customerid);
                        XElement bookingres = rs.BookingRequest(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region LCI
                    if (supplierid == "35")
                    {
                        #region LCI
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(35, customerid);
                        XElement bookingres = rs.BookingRequest(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region SunHotels
                    if (supplierid == "36")
                    {
                        #region SunHotels
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        SunHotelsResponse objRs = new SunHotelsResponse(36, customerid);
                        XElement bookingres = objRs.BookingRequest(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region Total Stay
                    if (supplierid == "37")
                    {
                        #region Total Stay
                        Jac_HtlBokng jacreq = new Jac_HtlBokng();
                        XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "37");
                        string Login = suppliercred.Descendants("Login").FirstOrDefault().Value;
                        string Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                        string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
                        XElement hotelBooking = jacreq.BokngRequestXml(Login, Password, url, req);
                        return hotelBooking;
                        #endregion
                    }
                    #endregion
                    #region SmyRooms
                    if (supplierid == "39")
                    {
                        TGServices tgs = new TGServices(39, req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement bookingResponse = tgs.Booking(req);
                        return bookingResponse;
                    }
                    #endregion
                    #region AlphaTours
                    if (supplierid == "41")
                    {
                        #region AlphaTours
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(41, customerid);
                        XElement bookingres = rs.BookingRequest(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region Hoojoozat
                    if (supplierid == "45")
                    {
                        #region Hoojoozat
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        HoojService rs = new HoojService(customerid);
                        XElement bookingres = rs.HotelBooking(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region Vot
                    if (supplierid == "46")
                    {
                        #region Vot
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        VOTService rs = new VOTService(customerid);
                        XElement bookingres = rs.HotelBooking(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region Ebookingcenter
                    if (supplierid == "47")
                    {
                        #region Ebookingcenter
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        EBookingService rs = new EBookingService(customerid);
                        XElement bookingres = rs.HotelBooking(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region Booking Express
                    if (supplierid == "501")
                    {
                        #region Booking Express
                        xmloutBook rs = new xmloutBook();
                        XElement bookingres = rs.book_bookingexpressOUT(req);
                        return bookingres;
                        #endregion
                    }
                    #endregion
                    #region Supplier doesn't Exists
                    else
                    {
                        #region No Supplier's Details Found
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
                                   new XElement("ErrorTxt", "No Supplier's Details Found")
                                           )
                                       )
                      ));
                        return bookingdoc;
                        #endregion
                    }
                    #endregion
                }
                catch(Exception ex)
                {
                    #region Exception
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
                               new XElement("ErrorTxt", ex.Message)
                                       )
                                   )
                  ));


                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelBookingConfirmation";
                    ex1.PageName = "TrvHotelBooking";
                    ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("TransactionID").FirstOrDefault().Value;
                    //APILog.SendCustomExcepToDB(ex1);
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    return bookingdoc;
                    #endregion
                }
                #endregion
            }
            else
            {
                #region Invalid Credential
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
                           new XElement("ErrorTxt", "Invalid Credentials")
                                   )
                               )
              ));
                return bookingdoc;
                #endregion
            }
            #endregion
        }
        #endregion
        #region Tourico Hotel Room's Info
        private IEnumerable<XElement> GetHotelRoomsInfoTourico(List<Tourico.Reservation> roomlist)
        {
            #region Room's info (Tourico)

            List<XElement> str = new List<XElement>();
            for (int i = 0; i < roomlist.Count(); i++)
            {
                Tourico.Passenger passengers = ((Tourico.HotelInfo)(roomlist[i].ProductInfo)).Passenger;
                List<Tourico.Supplement> supplements = ((Tourico.HotelInfo)(roomlist[i].ProductInfo)).RoomExtraInfo.SelectedSupplements.ToList();

                if (((Tourico.HotelInfo)(roomlist[i].ProductInfo)).RoomExtraInfo.BoardBase == null)
                {

                    str.Add(new XElement("Room",
                         new XAttribute("ID", Convert.ToString((((Tourico.HotelInfo)(roomlist[i].ProductInfo))).RoomExtraInfo.hotelRoomTypeId)),
                                                                      new XAttribute("RoomType", Convert.ToString(((Tourico.HotelInfo)(roomlist[i].ProductInfo)).roomTypeCategory)),
                                                                      new XAttribute("ServiceID", Convert.ToString(roomlist[i].reservationId)),
                                                                      new XAttribute("MealPlanID", Convert.ToString("")),
                                                                      new XAttribute("MealPlanName", Convert.ToString("")),
                                                                      new XAttribute("MealPlanCode", Convert.ToString("")),
                                                                      new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                      new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                      new XAttribute("RoomStatus", Convert.ToString(roomlist[i].status)),
                                                                      new XAttribute("TotalRoomRate", Convert.ToString(roomlist[i].price)),
                         GetHotelPassengersInfoTourico(passengers),
                         new XElement("Supplements",
                             GetRoomsSupplementTourico(supplements)
                             )

                         ));
                }
                else
                {

                    str.Add(new XElement("Room",
                         new XAttribute("ID", Convert.ToString((((Tourico.HotelInfo)(roomlist[i].ProductInfo))).RoomExtraInfo.hotelRoomTypeId)),
                                                                      new XAttribute("RoomType", Convert.ToString(((Tourico.HotelInfo)(roomlist[i].ProductInfo)).roomTypeCategory)),
                                                                      new XAttribute("ServiceID", Convert.ToString(roomlist[i].reservationId)),
                                                                      new XAttribute("MealPlanID", Convert.ToString(((Tourico.HotelInfo)(roomlist[i].ProductInfo)).RoomExtraInfo.BoardBase.bbId)),
                                                                      new XAttribute("MealPlanName", Convert.ToString(((Tourico.HotelInfo)(roomlist[i].ProductInfo)).RoomExtraInfo.BoardBase.bbName)),
                                                                      new XAttribute("MealPlanCode", Convert.ToString("")),
                                                                      new XAttribute("MealPlanPrice", Convert.ToString(((Tourico.HotelInfo)(roomlist[i].ProductInfo)).RoomExtraInfo.BoardBase.bbPrice)),
                                                                      new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                      new XAttribute("RoomStatus", Convert.ToString(roomlist[i].status)),
                                                                      new XAttribute("TotalRoomRate", Convert.ToString(roomlist[i].price)),
                         GetHotelPassengersInfoTourico(passengers),
                         new XElement("Supplements",
                             GetRoomsSupplementTourico(supplements)
                             )
                         ));
                }

            };
            return str;



            #endregion
        }
        #endregion
        #region Tourico Room's Supplements
        private IEnumerable<XElement> GetRoomsSupplementTourico(List<Tourico.Supplement> supplements)
        {
            #region Tourico Supplements
            List<XElement> str = new List<XElement>();

            Parallel.For(0, supplements.Count(), i =>
            {

                Tourico.Supplement ss = supplements[i];
                if (ss != null)
                {
                    XmlSerializer xsSubmit = new XmlSerializer(typeof(Tourico.Supplement));
                    XmlDocument doc = new XmlDocument();
                    System.IO.StringWriter sww = new System.IO.StringWriter();
                    XmlWriter writer = XmlWriter.Create(sww);
                    xsSubmit.Serialize(writer, ss);
                    var xsd = XDocument.Parse(sww.ToString());
                    var prefix = xsd.Root.GetNamespaceOfPrefix("xsi");
                    var type = xsd.Root.Attribute(prefix + "type");
                    if (Convert.ToString(type.Value) == "q1:PerPersonSupplement")
                    {
                        var agegroup = XDocument.Parse(sww.ToString());

                        var prefixage = agegroup.Root.GetNamespaceOfPrefix("q1");

                        List<XElement> totalsupp = agegroup.Root.Descendants(prefixage + "SupplementAge").ToList();

                        str.Add(new XElement("Supplement",
                            new XAttribute("suppId", Convert.ToString(supplements[i].suppId)),
                            new XAttribute("suppName", Convert.ToString(supplements[i].suppName)),
                            new XAttribute("supptType", Convert.ToString(supplements[i].supptType)),
                            new XAttribute("suppIsMandatory", Convert.ToString(supplements[i].suppIsMandatory)),
                            new XAttribute("suppChargeType", Convert.ToString(supplements[i].suppChargeType)),
                            new XAttribute("suppPrice", Convert.ToString(supplements[i].price)),
                            new XAttribute("suppType", Convert.ToString(type.Value)),
                            new XElement("SuppAgeGroup",
                                GetRoomsSupplementAgeGroupTourico(totalsupp)
                                )
                            ));
                    }
                    else
                    {
                        str.Add(new XElement("Supplement",
                            new XAttribute("suppId", Convert.ToString(supplements[i].suppId)),
                            new XAttribute("suppName", Convert.ToString(supplements[i].suppName)),
                            new XAttribute("supptType", Convert.ToString(supplements[i].supptType)),
                            new XAttribute("suppIsMandatory", Convert.ToString(supplements[i].suppIsMandatory)),
                            new XAttribute("suppChargeType", Convert.ToString(supplements[i].suppChargeType)),
                            new XAttribute("suppPrice", Convert.ToString(supplements[i].price)),
                            new XAttribute("suppType", Convert.ToString(type.Value)))
                         );
                    }
                }
            });

            return str;
            #endregion
        }
        #endregion
        #region Tourico Room Supplement's Age Group
        private IEnumerable<XElement> GetRoomsSupplementAgeGroupTourico(List<XElement> supplementagegroup)
        {
            #region Tourico Supplements Age Group
            List<XElement> str = new List<XElement>();

            Parallel.For(0, supplementagegroup.Count(), i =>
            {
                str.Add(new XElement("SupplementAge",
                       new XAttribute("suppFrom", Convert.ToString(supplementagegroup[i].Attribute("suppFrom").Value)),
                       new XAttribute("suppTo", Convert.ToString(supplementagegroup[i].Attribute("suppTo").Value)),
                       new XAttribute("suppQuantity", Convert.ToString(supplementagegroup[i].Attribute("suppQuantity").Value)),
                       new XAttribute("suppPrice", Convert.ToString(supplementagegroup[i].Attribute("suppPrice").Value)))
                );
            });
            return str;
            #endregion
        }
        #endregion
        #region Tourico Hotel Passenger's Info
        private IEnumerable<XElement> GetHotelPassengersInfoTourico(Tourico.Passenger htpassengersinfo)
        {
            #region Passenger's info (Tourico)


            List<XElement> pssngrlst = new List<XElement>();
            pssngrlst.Add(new XElement("RoomGuest",
                new XElement("GuestType", Convert.ToString("Adult")),
                new XElement("Title", ""),
                new XElement("FirstName", Convert.ToString(htpassengersinfo.firstName + " " + htpassengersinfo.middleName + " " + htpassengersinfo.lastName)),
                new XElement("MiddleName", ""),
                new XElement("LastName", ""),
                new XElement("IsLead", Convert.ToString("true")),
                new XElement("Age", Convert.ToString(""))
                ));
            return pssngrlst;
            #endregion
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
                         new XElement("Supplements",""
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