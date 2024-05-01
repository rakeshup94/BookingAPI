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
using TravillioXMLOutService.Supplier.TouricoHolidays;
using TravillioXMLOutService.Supplier.Darina;
using TravillioXMLOutService.Supplier.Juniper;
using TravillioXMLOutService.Supplier.Godou;
using TravillioXMLOutService.Supplier.SalTours;
using TravillioXMLOutService.Supplier.SunHotels;
using TravillioXMLOutService.Supplier.Hoojoozat;
using TravillioXMLOutService.Supplier.TravelGate;
using TravillioXMLOutService.Supplier.TBOHolidays;
using TravillioXMLOutService.Supplier.VOT;
using TravillioXMLOutService.Supplier.EBookingCenter;
using TravillioXMLOutService.Supplier.XMLOUTAPI.PreBook.Common;

namespace TravillioXMLOutService.App_Code
{
    public class TrvHotelPreBooking :IDisposable
    {
        XElement reqTravillio;
        XElement bookingroom;
        List<XElement> hotelavailabilitylistextranet;
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
                    writer.WriteLine("---------------------------Pre Booking Response-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        public void WriteToFileErrorfromsupplier(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    writer.WriteLine("---------------------------Pre Booking Response Error From supplier end-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        public void WriteToFilerequest(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    writer.WriteLine("---------------------------Pre Booking Request Log -----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        #region XML OUT for Hotel PreBooking (Travayoo)
        public XElement HotelPreBooking(XElement req)
        {
            #region XML OUT for Hotel PreBooking (Travayoo)
            HeaderAuth headercheck = new HeaderAuth();
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SupplierID").Single().Value;
            if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
            {
                try
                {
                    #region Supplier Credentials
                    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(supplier_Cred).TypeHandle);
                    #endregion
                    IEnumerable<XElement> request = req.Descendants("HotelPreBookingRequest");
                    reqTravillio = req;
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    List<XElement> htlst = req.Descendants("GiataHotelList").ToList();
                    supplierid = htlst[0].Attribute("GSupID").Value;
                    string xmlout = string.Empty;
                    string custName = string.Empty;
                    try
                    {
                        xmlout = htlst[0].Attribute("xmlout").Value;                       
                        try
                        {
                            custName = htlst[0].Attribute("custName").Value;
                        }
                        catch { custName = ""; }
                    }
                    catch { xmlout = "false"; }
                    #region Darina
                    if (supplierid == "1")
                    {
                        #region Darina
                        if (custName == "")
                        {
                            custName = "Darina";
                        }
                        dr_prebook darreq = new dr_prebook();
                        XElement hotelpreBooking = darreq.prebookdarina(req, custName);
                        return hotelpreBooking;
                        #endregion
                    }
                    #endregion
                    #region Tourico
                    if (supplierid == "2")
                    {
                        #region Tourico
                        if (custName == "")
                        {
                            custName = "Tourico";
                        }
                        XElement credential = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\Credential.xml"));
                        Tr_PreBook hbreq = new Tr_PreBook();
                        XElement hotelpreBooking = hbreq.PrebookingTourico(req, credential, custName);
                        return hotelpreBooking;
                        #endregion
                    }
                    #endregion
                    #region Extranet
                    if (supplierid == "3")
                    {
                        #region Extranet
                        if (custName == "")
                        {
                            custName = "Extranet";
                        }
                        ExtPreBook extreq = new ExtPreBook();
                        XElement hotelpreBooking = extreq.PrebookingExtranet(req, custName);
                        return hotelpreBooking;
                        #endregion
                    }
                    #endregion
                    #region HotelBeds
                    if (supplierid == "4")
                    {
                        #region HotelBeds
                        if (custName == "")
                        {
                            custName = "HotelBeds";
                        }
                        PreBookHotelBeds hbreq = new PreBookHotelBeds();

                        XElement hotelpreBooking = hbreq.PrebookingHotelBeds(req, custName);

                        return hotelpreBooking;
                        #endregion
                    }
                    #endregion
                    #region DOTW
                    if (supplierid == "5")
                    {
                        #region DOTW
                        if (custName == "")
                        {
                            custName = "DOTW";
                        }
                        DotwService dotwObj = new DotwService(req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement prebookres = dotwObj.PreBookingSeaReq(req, custName);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region HotelsPro
                    if (supplierid == "6")
                    {
                        #region HotelsPro
                        if (custName == "")
                        {
                            custName = "HotelsPro";
                        }
                        HotelsProPreBook hbreq = new HotelsProPreBook();

                        XElement hotelpreBooking = hbreq.PrebookingHotelsPro(req, custName);

                        return hotelpreBooking;
                        #endregion
                    }
                    #endregion
                    #region Travco
                    if (supplierid == "7")
                    {
                        #region Travco
                        if (custName == "")
                        {
                            custName = "Travco";
                        }
                        Travco trvObj = new Travco(req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement prebookres = trvObj.HotelPreBooking(req, custName);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region JacTravels
                    if (supplierid == "8")
                    {
                        #region Jac
                        if (custName == "")
                        {
                            custName = "JacTravel";
                        }
                        HtlPreBooking jacreq = new HtlPreBooking();
                        XElement suppliercred = supplier_Cred.getsupplier_credentials(reqTravillio.Descendants("CustomerID").FirstOrDefault().Value, "8");
                        string Login = suppliercred.Descendants("Login").FirstOrDefault().Value;
                        string Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                        string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
                        XElement hotelpreBooking = jacreq.PreBokngRequest(req, Login, Password, url, custName, "JacTravel");
                        return hotelpreBooking;
                        #endregion
                    }
                    #endregion
                    #region RTS
                    if (supplierid == "9")
                    {
                        if (custName == "")
                        {
                            custName = "RTS";
                        }
                        foreach (var item in req.Descendants("HotelPreBookingRequest"))
                        {
                            string GuestCountyCode = item.Element("PaxNationality_CountryCode").Value != string.Empty ? item.Element("PaxNationality_CountryCode").Value.ToUpper() : string.Empty;
                            RTS_PreBooking obj = new RTS_PreBooking();
                            XElement htdetails = obj.GetPreBook(req, GuestCountyCode, custName);
                            return htdetails;
                        }
                        return null;
                    }
                    #endregion
                    #region MIKI
                    if (supplierid == "11")
                    {
                        #region MIKI
                        if (custName == "")
                        {
                            custName = "Miki";
                        }
                        MikiInternal mk = new MikiInternal();
                        XElement prebookres = mk.PrebookingRequest(req, custName);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region Restel
                    if (supplierid == "13")
                    {
                        #region Restel
                        if (custName == "")
                        {
                            custName = "Restel";
                        }
                        RestelServices rs = new RestelServices();
                        XElement prebookres = rs.PreBookingRequest(req, 13, custName);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region JuniperW2m
                    if (supplierid == "16")
                    {
                        #region Juniper
                        if (custName == "")
                        {
                            custName = "W2M";
                        }
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(16, customerid);
                        XElement prebookres = rs.PreBookingRequest(req, custName, 16);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region EgyptExpress
                    if (supplierid == "17")
                    {
                        #region EgyptExpress
                        if (custName == "")
                        {
                            custName = "EgyptExpress";
                        }
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(17, customerid);
                        XElement prebookres = rs.PreBookingRequest(req, custName, 17);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region Sal Tours
                    if (supplierid == "19")
                    {
                        if (custName == "")
                        {
                            custName = "SALTOURS";
                        }
                        SalServices sser = new SalServices();
                        XElement prebookres = sser.PreBooking(req, custName);
                        return prebookres;
                    }
                    #endregion
                    #region TBO Holidays
                    if (supplierid == "21")
                    {
                        if (custName == "")
                        {
                            custName = "TBO";
                        }
                        TBOServices tbs = new TBOServices();
                        XElement prebookres = tbs.PreBooking(req, custName);
                        return prebookres;
                    }
                    #endregion
                    #region LOH
                    if (supplierid == "23")
                    {
                        #region Juniper
                        if (custName == "")
                        {
                            custName = "LOH";
                        }
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(23, customerid);
                        XElement prebookres = rs.PreBookingRequest(req, custName, 23);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region Gadou
                    if (supplierid == "31")
                    {
                        //GodouServices gds = new GodouServices();
                        //XElement prebookres = gds.PreBooking(req, custName);
                        //return prebookres;
                        #region Juniper
                        if (custName == "")
                        {
                            custName = "GADOU";
                        }
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(31, customerid);
                        XElement prebookres = rs.PreBookingRequest(req, custName, 31);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region LCI
                    if (supplierid == "35")
                    {
                        #region Juniper
                        if (custName == "")
                        {
                            custName = "LCI";
                        }
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(35, customerid);
                        XElement prebookres = rs.PreBookingRequest(req, custName, 35);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region SunHotels
                    if (supplierid == "36")
                    {
                        #region SunHotel
                        if (custName == "")
                        {
                            custName = "SunHotels";
                        }
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        SunHotelsResponse objRs = new SunHotelsResponse(36, customerid);
                        XElement prebookres = objRs.PreBookingRequest(req, custName, 36);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region Total Stay
                    if (supplierid == "37")
                    {
                        #region Total Stay
                        if (custName == "")
                        {
                            custName = "TotalStay";
                        }
                        HtlPreBooking jacreq = new HtlPreBooking();
                        XElement suppliercred = supplier_Cred.getsupplier_credentials(reqTravillio.Descendants("CustomerID").FirstOrDefault().Value, "37");
                        string Login = suppliercred.Descendants("Login").FirstOrDefault().Value;
                        string Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                        string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
                        XElement hotelpreBooking = jacreq.PreBokngRequest(req, Login, Password, url, custName, "TotalStay");
                        return hotelpreBooking;
                        #endregion
                    }
                    #endregion
                    #region SmyRooms
                    if (supplierid == "39")
                    {
                        if (custName == "")
                        {
                            custName = "SMYROOMS";
                        }
                        TGServices tgs = new TGServices(39, req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement prebookres = tgs.PreBook(req, custName);
                        return prebookres;
                    }
                    #endregion
                    #region AlphaTours
                    if (supplierid == "41")
                    {
                        #region AlphaTours
                        if (custName == "")
                        {
                            custName = "AlphaTours";
                        }
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(41, customerid);
                        XElement prebookres = rs.PreBookingRequest(req, custName, 41);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region Hoojoozat
                    if (supplierid == "45")
                    {
                        #region Hoojoozat
                        if (custName == "")
                        {
                            custName = "Hoojoozat";
                        }
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        HoojService rs = new HoojService(customerid);
                        XElement prebookres = rs.PreBooking(req, custName);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region VOT
                    if (supplierid == "46")
                    {
                        #region VOT
                        if (custName == "")
                        {
                            custName = "VOT";
                        }
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        VOTService rs = new VOTService(customerid);
                        XElement prebookres = rs.PreBooking(req, custName);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region Ebookingcenter
                    if (supplierid == "47")
                    {
                        #region Ebookingcenter
                        if (custName == "")
                        {
                            custName = "EBookingCenter";
                        }
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        EBookingService rs = new EBookingService(customerid);
                        XElement prebookres = rs.PreBooking(req, custName);
                        return prebookres;
                        #endregion
                    }
                    #endregion
                    #region Booking Express
                    if (supplierid == "501")
                    {
                        #region Booking Express
                        if (custName == "")
                        {
                            custName = "BookingExpress";
                        }
                        xmlprebook objreq = new xmlprebook();
                        XElement hotelpreBooking = objreq.prebook_bookingexpressOUT(req, custName);
                        return hotelpreBooking;
                        #endregion
                    }
                    #endregion
                    #region No Supplier's Details Found
                    else
                    {
                        #region No Supplier's Details Found
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
                               new XElement("HotelPreBookingResponse",
                                   new XElement("ErrorTxt", "No Supplier's Details Found")
                                           )
                                       )
                      ));
                        return searchdoc;
                        #endregion
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    #region Exception
                    IEnumerable<XElement> request = req.Descendants("HotelPreBookingRequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
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
                           new XElement("HotelPreBookingResponse",
                               new XElement("ErrorTxt", ex.Message)
                                       )
                                   )
                  ));

                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelPreBooking";
                    ex1.PageName = "TrvHotelPreBooking";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    return searchdoc;
                    #endregion
                }
            }
            else
            {
                #region Invalid Credential
                IEnumerable<XElement> request = req.Descendants("HotelPreBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
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
                       new XElement("HotelPreBookingResponse",
                           new XElement("ErrorTxt", "Invalid Credentials")
                                   )
                               )
              ));
                return searchdoc;
                #endregion
            }
            #endregion
        }
        #endregion
        #region Hotel Extranet
        #region Extranet Hotel Listing
        private IEnumerable<XElement> GetHotelListExtranet(List<XElement> htlist)
        {
            #region Extranet Hotels
            List<XElement> hotellst = new List<XElement>();
            Int32 length = htlist.Count();

            try
            {
                //Parallel.For(0, length, i =>
                for (int i = 0; i < length; i++)
                {


                    hotellst.Add(new XElement("Hotel",
                                           new XElement("HotelID", Convert.ToString(htlist[i].Descendants("HotelID").Single().Value)),
                                                       new XElement("HotelName", Convert.ToString(htlist[i].Descendants("HotelName").Single().Value)),
                                                       new XElement("Status", htlist[i].Descendants("Room").FirstOrDefault().Descendants("AvailableAllRoom").SingleOrDefault().Value),
                                                       new XElement("TermCondition", ""),
                                                       new XElement("HotelImgSmall", Convert.ToString("")),
                                                       new XElement("HotelImgLarge", Convert.ToString("")),
                                                       new XElement("MapLink", ""),
                                                       new XElement("DMC", "Extranet"),
                                                       new XElement("Currency", ""),
                                                       new XElement("Offers", "")
                                                       , new XElement("Rooms",
                                                GetHotelRoomgroupExtranet(htlist[i].Descendants("Room").ToList(), htlist)
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
        #region Extranet Hotel's Room Grouping
        public IEnumerable<XElement> GetHotelRoomgroupExtranet(List<XElement> roomlist,List<XElement> htlist)
        {
            List<XElement> str = new List<XElement>();
            Parallel.For(0, roomlist.Count(), i =>
            {
                str = GetHotelRoomListingExtranet(roomlist[i],htlist).ToList();


            });
            return str;
        }
        #endregion
        #region Extranet Hotel's Room Listing
        public IEnumerable<XElement> GetHotelRoomListingExtranet(XElement roomlist,List<XElement> htlist)
        {

            List<XElement> strgrp = new List<XElement>();
            int group = 0;

            List<XElement> totalroom = reqTravillio.Descendants("RoomPax").ToList();


            #region room's group

            #region Board Bases >0

            List<XElement> bb = roomlist.Descendants("Meal").ToList();

            //Parallel.For(0, bb.Count(), j =>
            for (int j = 0; j < bb.Count(); j++)
            {
                group++;
                List<XElement> str = new List<XElement>();
                for (int m = 0; m < totalroom.Count(); m++)
                {

                    decimal totalrate = calculatetotalroomrate(bb[j].Descendants("Price").ToList(), Convert.ToString(m + 1));

                    str.Add(new XElement("Room",
                     new XAttribute("ID", Convert.ToString(roomlist.Descendants("RoomTypeId").SingleOrDefault().Value)),
                     new XAttribute("SuppliersID", "3"),
                     new XAttribute("RoomSeq", m + 1),
                     new XAttribute("SessionID", Convert.ToString("")),
                     new XAttribute("RoomType", Convert.ToString(roomlist.Descendants("RoomTypeName").SingleOrDefault().Value)),
                     new XAttribute("OccupancyID", Convert.ToString("")),
                     new XAttribute("OccupancyName", Convert.ToString("")),
                     new XAttribute("MealPlanID", Convert.ToString(bb[j].Descendants("MealPlanID").SingleOrDefault().Value)),
                     new XAttribute("MealPlanName", Convert.ToString(bb[j].Descendants("MealPlanName").SingleOrDefault().Value)),
                     new XAttribute("MealPlanCode", ""),
                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                     new XAttribute("PerNightRoomRate", Convert.ToString(bb[j].Descendants("PricePerNight").SingleOrDefault().Value)),
                     new XAttribute("TotalRoomRate", Convert.ToString(totalrate)),
                     new XAttribute("CancellationDate", ""),
                     new XAttribute("CancellationAmount", ""),
                      new XAttribute("isAvailable", Convert.ToString(roomlist.Descendants("AvailableAllRoom").SingleOrDefault().Value)),
                     new XElement("RequestID", Convert.ToString("")),
                     new XElement("Offers", ""),
                     new XElement("PromotionList",
                         GetHotelpromotionsExtranet(bb.Descendants("pro").ToList())),
                     new XElement("CancellationPolicy", ""),
                     new XElement("Amenities", new XElement("Amenity", "")),
                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                     new XElement("Supplements", ""
                        //GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                         ),
                         new XElement("PriceBreakups",
                        GetRoomsPriceBreakupExtranet(bb[j].Descendants("Price").ToList(), Convert.ToString(m + 1))
                             ),
                         new XElement("AdultNum", Convert.ToString(totalroom[m].Element("Adult").Value)),
                         new XElement("ChildNum", Convert.ToString(totalroom[m].Element("Child").Value))
                     ));

                }

                strgrp.Add(new XElement("RoomTypes", 
                    new XAttribute("Index", j + 1),
                    
                    new XElement("CancellationPolicies",
                         GetRoomCancellationPolicyExtranet(htlist[0].Descendants("CancellationPolicies").ToList(), htlist[0].Descendants("Currency").SingleOrDefault().Value)),
                    new XAttribute("TotalRate", Convert.ToString(bb[j].Descendants("TotalRoomRate").SingleOrDefault().Value)),
                str));

            }
            #endregion


            //return str;
            #endregion






            return strgrp;
        }
        #endregion
        #region Calculate total room price
        private decimal calculatetotalroomrate(List<XElement> pernightrate, string roomseq)
        {
            decimal totalroomrate = 0;

            for (int i = 0; i < pernightrate.Count(); i++)
            {
                List<XElement> pernightprice = pernightrate[i].Descendants("SearchRoomPrice").Where(x => x.Descendants("RoomNo").Single().Value == roomseq).ToList();

                if (pernightprice.Count > 0)
                {
                    totalroomrate = totalroomrate + Convert.ToDecimal(pernightprice.Descendants("PriceForDate").SingleOrDefault().Value);
                }
                else
                {
                    totalroomrate = totalroomrate + 0;
                }
            }

            return totalroomrate;
        }
        #endregion
        #region Extranet Room's Price Breakups
        private IEnumerable<XElement> GetRoomsPriceBreakupExtranet(List<XElement> pricebreakups, string roomseq)
        {
            #region Extranet Room's Price Breakups
            List<XElement> str = new List<XElement>();
            Parallel.For(0, pricebreakups.Count(), i =>
            {
                List<XElement> pernightprice = pricebreakups[i].Descendants("SearchRoomPrice").Where(x => x.Descendants("RoomNo").Single().Value == roomseq).ToList();

                if (pernightprice.Count > 0)
                {

                    str.Add(new XElement("Price",
                       new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                       new XAttribute("PriceValue", Convert.ToString(pernightprice.Descendants("PriceForDate").SingleOrDefault().Value)))
                );
                }
                else
                {
                    str.Add(new XElement("Price",
                       new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                       new XAttribute("PriceValue", Convert.ToString(0)))
                );
                }


            });
            return str;
            #endregion
        }
        #endregion
        #region Extranet's Room's Promotion
        private IEnumerable<XElement> GetHotelpromotionsExtranet(List<XElement> roompromotions)
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

                    promotion.Add(new XElement("Promotions", Convert.ToString(roompromotions[i].Value)));

                });
            }
            return promotion;
        }
        #endregion
        #region Room's Cancellation Policies from Hotel Extranet
        private IEnumerable<XElement> GetRoomCancellationPolicyExtranet(List<XElement> cancellationpolicy, string currencycode)
        {
            #region Room's Cancellation Policies from Hotel Extranet
            List<XElement> htrm = new List<XElement>();
            List<XElement> roomlist = cancellationpolicy.Descendants("Cancel").ToList();
            //PerNightRoomRate = Convert.ToString(Convert.ToDecimal(TotalRoomRate) / Convert.ToDecimal(NrOfnights));
            for (int i = 0; i < roomlist.Count(); i++)
            {
                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on after " + roomlist[i].Descendants("RefundDate").SingleOrDefault().Value + "  will apply " + currencycode + " " + roomlist[i].Descendants("RefundPriceEffective").SingleOrDefault().Value + "  Cancellation fee"
                    , new XAttribute("LastCancellationDate", Convert.ToString(roomlist[i].Descendants("RefundDate").SingleOrDefault().Value))
                    , new XAttribute("ApplicableAmount", roomlist[i].Descendants("RefundPriceEffective").SingleOrDefault().Value)
                    , new XAttribute("NoShowPolicy", "0")));
            };
            return htrm;
            #endregion
        }
        #endregion
        #region Extranet Availability
        public void gethotelavailabilityExtranet()
        {
            List<XElement> doc1 = new List<XElement>();
            HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
            #region Extranet Request/Response
            string requestxml = string.Empty;
            requestxml="<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>"+
                          "<soapenv:Header xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>"+
                            "<Authentication>"+
                              "<AgentID>0</AgentID>" +
                              "<UserName>Suraj</UserName>" +
                              "<Password>123#</Password>" +
                              "<ServiceType>HT_001</ServiceType>"+
                              "<ServiceVersion>v1.0</ServiceVersion>"+
                            "</Authentication>"+
                          "</soapenv:Header>"+
                          "<soapenv:Body>"+
                            "<prebookRequest>"+
                              "<CustomerID>" + reqTravillio.Descendants("CustomerID").Single().Value + "</CustomerID>" +
                            //"<CustomerID>" + reqTravillio.Descendants("CustomerID").SingleOrDefault().Value + "</CustomerID>" +
                              "<Response_Type>XML</Response_Type>"+
                              "<FromDate>" + reqTravillio.Descendants("FromDate").Single().Value + "</FromDate>" +
                              "<ToDate>" + reqTravillio.Descendants("ToDate").Single().Value + "</ToDate>" +
                              "<PropertyId>" + reqTravillio.Descendants("HotelID").Single().Value + "</PropertyId>" +
                              "<RoomId>" + reqTravillio.Descendants("RoomTypeID").Single().Value + "</RoomId>" +
                              "<MealId>" + reqTravillio.Descendants("Room").FirstOrDefault().Attribute("MealPlanID").Value + "</MealId>" +
                              "<CultureId>1</CultureId>"+
                              "<GuestNationalityId>" + reqTravillio.Descendants("PaxNationality_CountryCode").Single().Value + "</GuestNationalityId>" +
                              reqTravillio.Descendants("Rooms").SingleOrDefault().ToString() +
                            "</prebookRequest>"+
                          "</soapenv:Body>"+
                        "</soapenv:Envelope>";

            try
            {
                object result = extclient.GetPreBookRequestByXML(requestxml);
                if (result != null)
                {
                    XElement doc = XElement.Parse(result.ToString());
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(reqTravillio.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = reqTravillio.Descendants("TransID").Single().Value;
                        log.LogTypeID = 4;
                        log.LogType = "PreBook";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = doc.ToString();
                        APILog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        APILog.SendExcepToDB(ex);
                    }
                    hotelavailabilitylistextranet = doc.Descendants("Hotel").ToList();
                }
                else
                {
                    
                    hotelavailabilitylistextranet = doc1.Descendants("Hotel").ToList();
                }
            }
            catch(Exception ex)
            {
                
                hotelavailabilitylistextranet = doc1.Descendants("Hotel").ToList();
            }

            #endregion
        }
        #endregion
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