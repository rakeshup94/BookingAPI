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
using System.Threading;
using System.Xml.Serialization;
using System.Collections;
using TravillioXMLOutService.Supplier.DotW;
using TravillioXMLOutService.Supplier.RTS;
using TravillioXMLOutService.Models.Darina;
using TravillioXMLOutService.Supplier.JacTravel;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Supplier.Miki;
using TravillioXMLOutService.Supplier.Restel;
using TravillioXMLOutService.Supplier.Extranet;
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
using TravillioXMLOutService.Supplier.XMLOUTAPI.GetRoom.Common;

namespace TravillioXMLOutService.App_Code
{
    public class TrvRoomAvailabilityNew : IDisposable
    {
        string availDarina = string.Empty;
        XElement reqTravillio;
        List<Tourico.Hotel> hotelavailabilityresult;
        List<XElement> hotelavailabilitylistextranet;
        int sup_cutime = 90000;
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
                    writer.WriteLine("---------------------------Hotel Availability Response-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        public void WriteToFileResponseTime(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {


                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    writer.WriteLine("---------------------------Time Calculate-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        #region Hotel Availability (XML OUT for Travayoo)
        public XElement CreateCheckAvailability(XElement req)
        {
            HeaderAuth headercheck = new HeaderAuth();
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            #region Hotel Availability
            if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
            {
                try
                {                   
                    reqTravillio = req;
                    
                    int darina = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "1").Count();
                    int tourico = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "2").Count();
                    int extranet = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "3").Count();
                    int hotelbeds = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "4").Count();
                    int DOTW = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "5").Count();
                    int hotelspro = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "6").Count();
                    int travco = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "7").Count();
                    int JacTravel = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "8").Count();
                    int RTS = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "9").Count();
                    int Miki = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "11").Count();
                    int restel = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "13").Count();
                    int JuniperW2M = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "16").Count();
                    int EgyptExpress = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "17").Count();
                    int SalTour = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "19").Count();
                    int tbo = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "21").Count();
                    int LOH = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "23").Count();
                    int Gadou = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "31").Count();
                    int LCI = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "35").Count();
                    int SunHotels = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "36").Count();
                    int totalstay = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "37").Count();
                    int SmyRooms = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "39").Count();
                    int AlphaTours = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "41").Count();
                    int Hoojoozat = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "45").Count();
                    int vot = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "46").Count();
                    int ebookingcenter = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "47").Count();
                    int bookingexpress = req.Descendants("GiataHotelList").Attributes("GSupID").Where(x => x.Value == "501").Count();

                    if (darina > 0 || tourico > 0 || extranet > 0 || hotelbeds > 0 || DOTW > 0 || hotelspro > 0 || travco > 0 || JacTravel > 0 || RTS > 0 || Miki > 0 || restel > 0 || JuniperW2M > 0 || EgyptExpress > 0 || SalTour > 0 || tbo > 0 || LOH > 0 || Gadou > 0 || LCI > 0 || SunHotels > 0 || totalstay > 0 || SmyRooms > 0 || AlphaTours > 0 || Hoojoozat > 0 || vot > 0 || ebookingcenter > 0 || bookingexpress > 0)
                    {
                        #region Supplier Credentials
                        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(supplier_Cred).TypeHandle);
                        #endregion
                        #region get cut off time
                        try
                        {
                            sup_cutime = supplier_Cred.rmcutoff_time();
                        }
                        catch { }
                        #endregion
                        List<XElement> hotelroomresponse = new List<XElement>();
                        List<XElement> htlst = req.Descendants("GiataHotelList").ToList();
                        List<XElement> Thresult = new List<XElement>();
                        List<XElement> hotelavailabilityresp = new List<XElement>();
                        //for (int i = 0; i < htlst.Count(); i++)
                        {
                            List<XElement> darinahotelavailabilityresp = new List<XElement>();
                            List<XElement> touricohotelavailabilityresp = new List<XElement>();
                            List<XElement> extranethotelavailabilityresp = new List<XElement>();
                            List<XElement> hotelbedshotelavailabilityresp = new List<XElement>();
                            XElement DOTWhotelavailabilityresp = null;
                            List<XElement> hotelsprohotelavailabilityresp = new List<XElement>();
                            XElement travcohotelavailabilityresp =  null;
                            List<XElement> JacTravelhotelavailabilityresp = null;
                            List<XElement> RTShotelavailabilityresp = new List<XElement>();
                            XElement Mikihotelavailabilityresp = null;
                            XElement restelhotelavailabilityresp = null;
                            XElement JuniperW2Mhotelavailabilityresp = null;
                            XElement EgyptExpresshotelavailabilityresp = null;
                            XElement SalTourhotelavailabilityresp = null;
                            XElement tbohotelavailabilityresp = null;
                            XElement JuniperLOHhotelavailabilityresp = null;
                            XElement JuniperLCIhotelavailabilityresp = null;
                            XElement Gadouhotelavailabilityresp = null;
                            XElement SunHotelshotelavailabilityresp = null;
                            List<XElement> totalstayhotelavailabilityresp = null;
                            XElement SmyRoomshotelavailabilityresp = null;
                            XElement AlphaTourshotelavailabilityresp = null;
                            XElement Hoojoozathotelavailabilityresp = null;
                            XElement vothotelavailabilityresp = null;
                            XElement ebookingcenterhotelavailabilityresp = null;
                            XElement bookingexpresshotelavailabilityresp = null;                            

                            Thread tid1 = null;
                            Thread tid2 = null;
                            Thread tid3 = null;
                            Thread tid4 = null;
                            Thread tid5 = null;
                            Thread tid6 = null;
                            Thread tid7 = null;
                            Thread tid8 = null;
                            Thread tid9 = null;
                            Thread tid11 = null;
                            Thread tid13 = null;
                            Thread tid16 = null;
                            Thread tid17 = null;
                            Thread tid19 = null;
                            Thread tid21 = null;
                            Thread tid23 = null;
                            Thread tid31 = null;
                            Thread tid35 = null;
                            Thread tid36 = null;
                            Thread tid37 = null;
                            Thread tid39 = null;
                            Thread tid41 = null;
                            Thread tid45 = null;
                            Thread tid46 = null;
                            Thread tid47 = null;
                            Thread tid501 = null;

                            #region Bind Static Data
                            XElement doccurrency = null;
                            XElement docmealplan = null;
                            XElement dococcupancy = null;
                            XElement statictouricohotellist = null;
                            XElement SmyRoomsMealPlans = null;
                            XElement hpromealtype = null;
                            if (darina > 0)
                            {
                                doccurrency = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\currency.xml"));
                                docmealplan = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\MealPlan.xml"));
                                dococcupancy = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\Occupancy.xml"));
                            }
                            if (tourico > 0)
                            {
                                statictouricohotellist = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\HotelInfo.xml"));
                            }  
                            if(hotelspro>0)
                            {
                                hpromealtype = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\HotelsPro\mealtype.xml"));
                            }
                            if(SmyRooms > 0)
                            {
                                SmyRoomsMealPlans = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/SmyRooms/Smy_Meals.xml"));
                            }
                            #endregion

                            #region Darina
                            if (darina > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "1").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "Darina";
                                    //}
                                    dr_Roomavail drreq = new dr_Roomavail();
                                    tid1 = new Thread(new ThreadStart(() => { darinahotelavailabilityresp = drreq.GetRoomAvail_DarinaOUT_merge(req, dococcupancy, docmealplan, doccurrency); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region Tourico
                            if (tourico > 0)
                            {
                                try
                                {
                                    string dmc = string.Empty;
                                    XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "2").FirstOrDefault();
                                    string htlid = htlele.Attribute("GHtlID").Value;
                                    string xmlout = string.Empty;
                                    try
                                    {
                                        xmlout = htlele.Attribute("xmlout").Value;
                                    }
                                    catch { xmlout = "false"; }
                                    if (xmlout == "true")
                                    {
                                        dmc = "HA";
                                    }
                                    else
                                    {
                                        dmc = "Tourico";
                                    }
                                    Tr_GetRoomAvail hbreq = new Tr_GetRoomAvail();
                                    XElement trcredential = null; //XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\Credential.xml"));
                                    //XElement statictouricohotellist = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\HotelInfo.xml"));
                                    tid2 = new Thread(new ThreadStart(() => { touricohotelavailabilityresp = hbreq.GetRoomAvail_Tourico(req, trcredential, statictouricohotellist, htlid, dmc); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region Extranet
                            if (extranet > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "3").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "Extranet";
                                    //}
                                    ExtGetRoomAvail extreq = new ExtGetRoomAvail();
                                    tid3 = new Thread(new ThreadStart(() => { extranethotelavailabilityresp = extreq.GetRoomAvail_ExtranetOUT_merge(req); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region Hotelbeds
                            if (hotelbeds > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "4").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "HotelBeds";
                                    //}
                                    RoomAvailabilityHotelBeds hbreq = new RoomAvailabilityHotelBeds();
                                    tid4 = new Thread(new ThreadStart(() => { hotelbedshotelavailabilityresp = hbreq.getroomavail_HBOUT(req); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region DOTW
                            if (DOTW > 0)
                            {
                                try
                                {
                                    DotwService dotwObj = new DotwService();
                                    tid5 = new Thread(new ThreadStart(() => { DOTWhotelavailabilityresp = dotwObj.GetRoomAvail_DOTWOUT(req); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region HotelsPro
                            if (hotelspro > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "6").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "HotelsPro";
                                    //}
                                    HotelsProRoomAvail hpreq = new HotelsProRoomAvail();
                                    tid6 = new Thread(new ThreadStart(() => { hotelsprohotelavailabilityresp = hpreq.getroomavailability_hpro(req, hpromealtype); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region Travco
                            if (travco > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "7").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "Travco";
                                    //}
                                    Travco travcoObj = new Travco();
                                    tid7 = new Thread(new ThreadStart(() => { travcohotelavailabilityresp = travcoObj.GetRoomAvail_travcoOUT(req); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region JacTravels
                            if (JacTravel > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "8").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "JacTravel";
                                    //}
                                    Jac_RoomRequest hbreq = new Jac_RoomRequest();
                                    tid8 = new Thread(new ThreadStart(() => { JacTravelhotelavailabilityresp = hbreq.getroomavailability_jactotal(req, 8); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region RTS
                            if (RTS > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "9").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "RTS";
                                    //}
                                    HTlStaticData obj = new HTlStaticData();
                                    XDocument doc = obj.GetRTSRoomAvailable(req, 9);
                                    RTS_RoomAvail romavailbj = new RTS_RoomAvail();
                                    tid9 = new Thread(new ThreadStart(() => { RTShotelavailabilityresp = romavailbj.getroomavailability_RTS(doc, req).ToList(); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region Miki
                            if (Miki > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "11").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "Miki";
                                    //}
                                    MikiInternal mik = new MikiInternal();
                                    tid11 = new Thread(new ThreadStart(() => { Mikihotelavailabilityresp = mik.GetRoomAvail_mikiOUT(req); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region Restel
                            if (restel > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "13").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "Restel";
                                    //}
                                    RestelServices rs = new RestelServices();
                                    //tid13 = new Thread(new ThreadStart(() => { restelhotelavailabilityresp = rs.RoomAvailability(req, dmc, htlid, 13); }));
                                    tid13 = new Thread(new ThreadStart(() => { restelhotelavailabilityresp = rs.RoomAvailability_restel(req); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region Juniper W2M
                            if (JuniperW2M > 0)
                            {
                                try
                                {                                    
                                    #region Juniper
                                    //int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                                    //JuniperResponses rs = new JuniperResponses(16, customerid);
                                    //tid16 = new Thread(new ThreadStart(() => { JuniperW2Mhotelavailabilityresp = rs.RoomAvailability_juniper(req, "W2M", "16"); }));
                                    JuniperResponses rs = new JuniperResponses();
                                    tid16 = new Thread(new ThreadStart(() => { JuniperW2Mhotelavailabilityresp = rs.RoomAvailability_juniper(req, "16"); }));
                                    #endregion
                                }
                                catch { }
                            }
                            #endregion
                            #region EgyptExpress
                            if (EgyptExpress > 0)
                            {
                                try
                                {
                                    #region EgyptExpress
                                    //int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                                    JuniperResponses rs = new JuniperResponses();
                                    tid17 = new Thread(new ThreadStart(() => { EgyptExpresshotelavailabilityresp = rs.RoomAvailability_juniper(req, "17"); }));
                                    #endregion
                                }
                                catch { }
                            }
                            #endregion
                            #region Sal Tours
                            if (SalTour > 0)
                            {
                                try
                                {
                                    string dmc = string.Empty;
                                    XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "19").FirstOrDefault();
                                    string htlid = htlele.Attribute("GHtlID").Value;
                                    string xmlout = string.Empty;
                                    try
                                    {
                                        xmlout = htlele.Attribute("xmlout").Value;
                                    }
                                    catch { xmlout = "false"; }
                                    if (xmlout.ToUpper() == "TRUE")
                                        dmc = "HA";
                                    else
                                        dmc = "SALTOURS";
                                    SalServices sser = new SalServices();
                                    tid19 = new Thread(new ThreadStart(() => { SalTourhotelavailabilityresp = sser.RoomAvailability(req, dmc, htlid); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region TBO Holidays
                            if (tbo > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "21").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout.ToUpper() == "TRUE")
                                    //    dmc = "HA";
                                    //else
                                    //    dmc = "TBO";
                                    TBOServices tbs = new TBOServices();
                                    tid21 = new Thread(new ThreadStart(() => { tbohotelavailabilityresp = tbs.getroomavail_tboOUT(req); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region LOH
                            if (LOH > 0)
                            {
                                try
                                {
                                    #region LOH
                                    //int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                                    JuniperResponses rs = new JuniperResponses();                                    
                                    tid23 = new Thread(new ThreadStart(() => { JuniperLOHhotelavailabilityresp = rs.RoomAvailability_juniper(req, "23"); }));
                                    #endregion
                                }
                                catch { }
                            }
                            #endregion
                            #region Gadou
                            if (Gadou > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "31").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout.ToUpper() == "TRUE")
                                    //    dmc = "HA";
                                    //else
                                    //    dmc = "GADOU";
                                    JuniperResponses gds = new JuniperResponses();
                                    tid31 = new Thread(new ThreadStart(() => { Gadouhotelavailabilityresp = gds.RoomAvailability_juniper(req, "31"); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region LCI
                            if (LCI > 0)
                            {
                                try
                                {
                                    #region LCI
                                    JuniperResponses rs = new JuniperResponses();
                                    tid35 = new Thread(new ThreadStart(() => { JuniperLCIhotelavailabilityresp = rs.RoomAvailability_juniper(req, "35"); }));
                                    #endregion
                                }
                                catch { }
                            }
                            #endregion
                            #region SunHotels
                            if (SunHotels > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "36").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "SunHotels";
                                    //}
                                    #region Juniper
                                    //int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                                    SunHotelsResponse objRs = new SunHotelsResponse();
                                    tid36 = new Thread(new ThreadStart(() => { SunHotelshotelavailabilityresp = objRs.GetRoomAvail_sunhotelOUT(req); }));
                                    #endregion
                                }
                                catch { }
                            }
                            #endregion
                            #region Total Stay
                            if (totalstay > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "37").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "TotalStay";
                                    //}
                                    //IEnumerable<XElement> responsehotels = null;
                                    Jac_RoomRequest hbreq = new Jac_RoomRequest();
                                    tid37 = new Thread(new ThreadStart(() => { totalstayhotelavailabilityresp = hbreq.getroomavailability_jactotal(req, 37); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region SmyRooms
                            if (SmyRooms > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "39").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout.ToUpper() == "TRUE")
                                    //    dmc = "HA";
                                    //else
                                    //    dmc = "SMYROOMS";
                                    //TGServices tgs = new TGServices(39, req.Descendants("CustomerID").FirstOrDefault().Value);
                                    TGServices tgs = new TGServices();
                                    tid39 = new Thread(new ThreadStart(() => { SmyRoomshotelavailabilityresp = tgs.GetRoomAvail_smyroomOUT(req, SmyRoomsMealPlans); }));
                                }
                                catch { }
                            }
                            #endregion
                            #region AlphaTours
                            if (AlphaTours > 0)
                            {
                                try
                                {
                                    #region AlphaTours
                                    //int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                                    JuniperResponses rs = new JuniperResponses();                                  
                                    tid41 = new Thread(new ThreadStart(() => { AlphaTourshotelavailabilityresp = rs.RoomAvailability_juniper(req, "41"); }));
                                    #endregion
                                }
                                catch { }
                            }
                            #endregion
                            #region Hoojoozat
                            if (Hoojoozat > 0)
                            {
                                try
                                {
                                    string dmc = string.Empty;
                                    XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "45").FirstOrDefault();
                                    string htlid = htlele.Attribute("GHtlID").Value;
                                    string xmlout = string.Empty;
                                    try
                                    {
                                        xmlout = htlele.Attribute("xmlout").Value;
                                    }
                                    catch { xmlout = "false"; }
                                    if (xmlout == "true")
                                    {
                                        dmc = "HA";
                                    }
                                    else
                                    {
                                        dmc = "Hoojoozat";
                                    }
                                    #region Hoojoozat
                                    string customerid = req.Descendants("CustomerID").Single().Value;
                                    HoojService rs = new HoojService(customerid);
                                    tid45 = new Thread(new ThreadStart(() => { Hoojoozathotelavailabilityresp = rs.RoomAvailability(req, dmc, htlid); }));
                                    #endregion
                                }
                                catch { }
                            }
                            #endregion
                            #region Vot
                            if (vot > 0)
                            {
                                try
                                {
                                    //string dmc = string.Empty;
                                    //XElement htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "46").FirstOrDefault();
                                    //string htlid = htlele.Attribute("GHtlID").Value;
                                    //string xmlout = string.Empty;
                                    //try
                                    //{
                                    //    xmlout = htlele.Attribute("xmlout").Value;
                                    //}
                                    //catch { xmlout = "false"; }
                                    //if (xmlout == "true")
                                    //{
                                    //    dmc = "HA";
                                    //}
                                    //else
                                    //{
                                    //    dmc = "VOT";
                                    //}
                                    #region Vot
                                    //string customerid = req.Descendants("CustomerID").Single().Value;
                                    VOTService rs = new VOTService();
                                    tid46 = new Thread(new ThreadStart(() => { vothotelavailabilityresp = rs.GetRoomAvail_votOUT(req); }));
                                    #endregion
                                }
                                catch { }
                            }
                            #endregion
                            #region Ebookingcenter
                            if (ebookingcenter > 0)
                            {
                                try
                                {
                                    #region Ebookingcenter
                                    //string customerid = req.Descendants("CustomerID").Single().Value;
                                    EBookingService rs = new EBookingService();
                                    tid47 = new Thread(new ThreadStart(() => { ebookingcenterhotelavailabilityresp = rs.GetRoomAvail_ebookingcenterOUT(req); }));
                                    #endregion
                                }
                                catch { }
                            }
                            #endregion
                            #region Booking Express
                            if (bookingexpress > 0)
                            {
                                try
                                {
                                    #region Booking Express
                                    xmlGetroom rs = new xmlGetroom();
                                    tid501 = new Thread(new ThreadStart(() => { bookingexpresshotelavailabilityresp = rs.GetRoomAvail_bookingexpressOUT(req); }));
                                    #endregion
                                }
                                catch { }
                            }
                            #endregion

                            #region Thread Start
                            try
                            {
                                if (darina > 0)
                                {
                                    tid1.Start();
                                }
                                if (tourico > 0)
                                {
                                    tid2.Start();
                                }
                                if (extranet > 0)
                                {
                                    tid3.Start();
                                }
                                if (hotelbeds > 0)
                                {
                                    tid4.Start();
                                }
                                if (DOTW > 0)
                                {
                                    tid5.Start();
                                }
                                if (hotelspro > 0)
                                {
                                    tid6.Start();
                                }
                                if (travco > 0)
                                {
                                    tid7.Start();
                                }
                                if (JacTravel > 0)
                                {
                                    tid8.Start();
                                }
                                if (RTS > 0)
                                {
                                    tid9.Start();
                                }
                                if (Miki > 0)
                                {
                                    tid11.Start();
                                }
                                if (restel > 0)
                                {
                                    tid13.Start();
                                }
                                if (JuniperW2M > 0)
                                {
                                    tid16.Start();
                                }
                                if (EgyptExpress > 0)
                                {
                                    tid17.Start();
                                }
                                if (SalTour > 0)
                                {
                                    tid19.Start();
                                }
                                if (tbo > 0)
                                {
                                    tid21.Start();
                                }
                                if (LOH > 0)
                                {
                                    tid23.Start();
                                }
                                if (Gadou > 0)
                                {
                                    tid31.Start();
                                }
                                if (LCI > 0)
                                {
                                    tid35.Start();
                                }
                                if (SunHotels > 0)
                                {
                                    tid36.Start();
                                }
                                if (totalstay > 0)
                                {
                                    tid37.Start();
                                }
                                if (SmyRooms > 0)
                                {
                                    tid39.Start();
                                }
                                if (AlphaTours > 0)
                                {
                                    tid41.Start();
                                }
                                if (Hoojoozat > 0)
                                {
                                    tid45.Start();
                                }
                                if (vot > 0)
                                {
                                    tid46.Start();
                                }
                                if (ebookingcenter > 0)
                                {
                                    tid47.Start();
                                }
                                if (bookingexpress > 0)
                                {
                                    tid501.Start();
                                }
                            }
                            catch (ThreadStateException te)
                            {

                            }
                            #endregion
                            #region Timer
                            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                            timer.Start();
                            #endregion
                            #region Thread Join
                            if (darina > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid1.Join(newTime);
                                }
                                catch { }
                                //tid1.Join();
                            }
                            if (tourico > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid2.Join(newTime);
                                }
                                catch { }
                            }
                            if (extranet > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid3.Join(newTime);
                                }
                                catch { }
                            }
                            if (hotelbeds > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid4.Join(newTime);
                                }
                                catch { }
                            }
                            if (DOTW > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid5.Join(newTime);
                                }
                                catch { }
                            }
                            if (hotelspro > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid6.Join(newTime);
                                }
                                catch { }
                            }
                            if (travco > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid7.Join(newTime);
                                }
                                catch { }
                            }
                            if (JacTravel > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid8.Join(newTime);
                                }
                                catch { }
                            }
                            if (RTS > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid9.Join(newTime);
                                }
                                catch { }
                            }
                            if (Miki > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid11.Join(newTime);
                                }
                                catch { }
                            }
                            if (restel > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid13.Join(newTime);
                                }
                                catch { }
                            }
                            if (JuniperW2M > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid16.Join(newTime);
                                }
                                catch { }
                            }
                            if (EgyptExpress > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid17.Join(newTime);
                                }
                                catch { }
                            }
                            if (SalTour > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid19.Join(newTime);
                                }
                                catch { }
                            }
                            if (tbo > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid21.Join(newTime);
                                }
                                catch { }
                            }
                            if (LOH > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid23.Join(newTime);
                                }
                                catch { }
                            }
                            if (Gadou > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid31.Join(newTime);
                                }
                                catch { }
                            }
                            if (LCI > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid35.Join(newTime);
                                }
                                catch { }
                            }
                            if (SunHotels > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid36.Join(newTime);
                                }
                                catch { }
                            }
                            if (totalstay > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid37.Join(newTime);
                                }
                                catch { }
                            }
                            if (SmyRooms > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid39.Join(newTime);
                                }
                                catch { }
                            }
                            if (AlphaTours > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid41.Join(newTime);
                                }
                                catch { }
                            }
                            if (Hoojoozat > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid45.Join(newTime);
                                }
                                catch { }
                            }
                            if (vot > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid46.Join(newTime);
                                }
                                catch { }
                            }
                            if (ebookingcenter > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid47.Join(newTime);
                                }
                                catch { }
                            }
                            if (bookingexpress > 0)
                            {
                                try
                                {
                                    int newTime = sup_cutime - Convert.ToInt32(timer.ElapsedMilliseconds);
                                    if (newTime < 0)
                                    {
                                        newTime = 0;
                                    }
                                    tid501.Join(newTime);
                                }
                                catch { }
                            }
                            #endregion
                            #region Thread Abort
                            if (tid1 != null && tid1.IsAlive)
                                tid1.Abort();
                            if (tid2 != null && tid2.IsAlive)
                                tid2.Abort();
                            if (tid3 != null && tid3.IsAlive)
                                tid3.Abort();
                            if (tid4 != null && tid4.IsAlive)
                                tid4.Abort();
                            if (tid5 != null && tid5.IsAlive)
                                tid5.Abort();
                            if (tid6 != null && tid6.IsAlive)
                                tid6.Abort();
                            if (tid7 != null && tid7.IsAlive)
                                tid7.Abort();
                            if (tid8 != null && tid8.IsAlive)
                                tid8.Abort();
                            if (tid9 != null && tid9.IsAlive)
                                tid9.Abort();
                            if (tid11 != null && tid11.IsAlive)
                                tid11.Abort();
                            if (tid13 != null && tid13.IsAlive)
                                tid13.Abort();
                            if (tid16 != null && tid16.IsAlive)
                                tid16.Abort();
                            if (tid17 != null && tid17.IsAlive)
                                tid17.Abort();
                            if (tid19 != null && tid19.IsAlive)
                                tid19.Abort();
                            if (tid21 != null && tid21.IsAlive)
                                tid21.Abort();
                            if (tid23 != null && tid23.IsAlive)
                                tid23.Abort();
                            if (tid31 != null && tid31.IsAlive)
                                tid31.Abort();
                            if (tid35 != null && tid35.IsAlive)
                                tid35.Abort();
                            if (tid36 != null && tid36.IsAlive)
                                tid36.Abort();
                            if (tid37 != null && tid37.IsAlive)
                                tid37.Abort();
                            if (tid39 != null && tid39.IsAlive)
                                tid39.Abort();
                            if (tid41 != null && tid41.IsAlive)
                                tid41.Abort();
                            if (tid45 != null && tid45.IsAlive)
                                tid45.Abort();
                            if (tid46 != null && tid46.IsAlive)
                                tid46.Abort();
                            if (tid47 != null && tid47.IsAlive)
                                tid47.Abort();
                            if (tid501 != null && tid501.IsAlive)
                                tid501.Abort();
                            #endregion

                            #region Merge
                            try
                            {
                                if (darinahotelavailabilityresp != null)
                                {
                                    if (darinahotelavailabilityresp.Count > 0)
                                    {
                                        XElement response = new XElement("Hotels", darinahotelavailabilityresp.Descendants("RoomTypes").ToList());
                                        hotelavailabilityresp.Add(response);
                                    }
                                }
                                if (touricohotelavailabilityresp != null)
                                {
                                    if (touricohotelavailabilityresp.Count > 0)
                                    {
                                        XElement response = new XElement("Hotels", touricohotelavailabilityresp.Descendants("RoomTypes").ToList());
                                        hotelavailabilityresp.Add(response);
                                    }
                                }
                                if (extranethotelavailabilityresp != null)
                                {
                                    if (extranethotelavailabilityresp.Count > 0)
                                    {
                                        XElement response = new XElement("Hotels", extranethotelavailabilityresp.Descendants("RoomTypes").ToList());
                                        hotelavailabilityresp.Add(response);
                                    }
                                }
                                if (hotelbedshotelavailabilityresp != null)
                                {
                                    if (hotelbedshotelavailabilityresp.Count > 0)
                                    {
                                        XElement response = new XElement("Hotels", hotelbedshotelavailabilityresp.Descendants("RoomTypes").ToList());
                                        hotelavailabilityresp.Add(response);
                                    }
                                }
                                //if (DOTWhotelavailabilityresp != null)
                                if (DOTWhotelavailabilityresp != null)
                                {
                                    try
                                    {
                                        XElement response = new XElement("Hotels", DOTWhotelavailabilityresp.Descendants("RoomTypes").ToList());
                                        hotelavailabilityresp.Add(response);
                                    }
                                    catch { }
                                }
                                if (hotelsprohotelavailabilityresp != null)
                                {
                                    try
                                    {
                                        if (hotelsprohotelavailabilityresp.Count > 0)
                                        {
                                            XElement response = new XElement("Hotels", hotelsprohotelavailabilityresp.Descendants("RoomTypes").ToList());
                                            hotelavailabilityresp.Add(response);
                                        }
                                    }
                                    catch { }
                                }
                                if (travcohotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", travcohotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (JacTravelhotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", JacTravelhotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (RTShotelavailabilityresp != null)
                                {
                                    if (RTShotelavailabilityresp.Count > 0)
                                    {
                                        XElement response = new XElement("Hotels", RTShotelavailabilityresp.Descendants("RoomTypes").ToList());
                                        hotelavailabilityresp.Add(response);
                                    }
                                }
                                if (Mikihotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", Mikihotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (restelhotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", restelhotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (JuniperW2Mhotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", JuniperW2Mhotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (EgyptExpresshotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", EgyptExpresshotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (SalTourhotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", SalTourhotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (tbohotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", tbohotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (JuniperLOHhotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", JuniperLOHhotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (Gadouhotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", Gadouhotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (JuniperLCIhotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", JuniperLCIhotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (SunHotelshotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", SunHotelshotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (totalstayhotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", totalstayhotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (SmyRoomshotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", SmyRoomshotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (AlphaTourshotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", AlphaTourshotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (Hoojoozathotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", Hoojoozathotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (vothotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", vothotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }
                                if (ebookingcenterhotelavailabilityresp != null)
                                {
                                    XElement response = new XElement("Hotels", ebookingcenterhotelavailabilityresp.Descendants("RoomTypes").ToList());
                                    hotelavailabilityresp.Add(response);
                                }                                  
                                if (bookingexpresshotelavailabilityresp != null)
                                {
                                    try
                                    {
                                        XElement response = new XElement("Hotels", bookingexpresshotelavailabilityresp.Descendants("RoomTypes").ToList());
                                        hotelavailabilityresp.Add(response);
                                    }
                                    catch { }
                                } 

                            }
                            catch { }
                            #endregion

                            XElement respon = new XElement("TotalSupHotels", hotelavailabilityresp);
                            if (hotelavailabilityresp != null)
                            {
                                Thresult.Add(respon);
                            }
                        }
                        hotelroomresponse = Thresult.Descendants("RoomTypes").ToList();

                        #region Bind all rooms
                        IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                  new XElement("searchResponse",
                                      new XElement("Hotels",
                                          new XElement("Hotel",
                                               new XElement("HotelID", Convert.ToString("")),
                                               new XElement("HotelName", Convert.ToString("")),
                                               new XElement("PropertyTypeName", Convert.ToString("")),
                                               new XElement("CountryID", Convert.ToString("")),
                                               new XElement("CountryName", Convert.ToString("")),
                                               new XElement("CountryCode", Convert.ToString("")),
                                               new XElement("CityId", Convert.ToString("")),
                                               new XElement("CityCode", Convert.ToString("")),
                                               new XElement("CityName", Convert.ToString("")),
                                               new XElement("AreaId", Convert.ToString("")),
                                               new XElement("AreaName", Convert.ToString("")),
                                               new XElement("Address", Convert.ToString("")),
                                               new XElement("Location", Convert.ToString("")),
                                               new XElement("Description", Convert.ToString("")),
                                               new XElement("StarRating", Convert.ToString("")),
                                               new XElement("MinRate", Convert.ToString("")),
                                               new XElement("HotelImgSmall", Convert.ToString("")),
                                               new XElement("HotelImgLarge", Convert.ToString("")),
                                               new XElement("MapLink", ""),
                                               new XElement("Longitude", Convert.ToString("")),
                                               new XElement("Latitude", Convert.ToString("")),
                                               new XElement("DMC", ""),
                                               new XElement("SupplierID", ""),
                                               new XElement("Currency", Convert.ToString("")),
                                               new XElement("Offers", ""),
                                               new XElement("Rooms",
                                                 hotelroomresponse
                                                   )
                        )

                                          )
                         ))));
                        #endregion

                        return searchdoc;
                    } 
                    else
                    {
                        #region Supplier doesn't Exists
                        IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                               new XElement("searchResponse",
                                   new XElement("ErrorTxt", "Supplier doesn't Exists.")
                                           )
                                       )
                      ));
                        return searchdoc;
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    #region Exception
                    IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                           new XElement("searchResponse",
                               new XElement("ErrorTxt", ex.Message)
                                       )
                                   )
                  ));
                    return searchdoc;
                    #endregion
                }
            }
            else
            {
                #region Invalid Credential
                IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                       new XElement("searchResponse",
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
        #region Hotel Availability Methods
        public void gethotelavaillistDarina()
        {
            #region Darina Hotel Availability
            try
            {
                string flag = "htlist";
                //var _url = "http://travelcontrol-agents-api.azurewebsites.net/service_v2.asmx";
                DarinaCredentials _credential = new DarinaCredentials();
                var _url = _credential.APIURL;
                var _action = "http://travelcontrol.softexsw.us/CheckAvailability";
                WriteToFileResponseTime("start time hotel listing Darina");
                availDarina = CallWebService(reqTravillio, _url, _action, flag);
                WriteToFile(availDarina);
                WriteToFileResponseTime("end time hotel listing Darina");
            }
            catch (Exception ex)
            {
                availDarina = "";
            }
            #endregion
        }
        public void gethotelavaillistTourico()
        {
            #region Tourio Hotel Availability

            #region XML Search
            // GetHotelInfoTourico(reqTravillio);
            #endregion



            #region Credentials
            XElement credential = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\Credential.xml"));
            string userlogin = string.Empty;
            string pwd = string.Empty;
            string version = string.Empty;
            userlogin = credential.Descendants("username").FirstOrDefault().Value;
            pwd = credential.Descendants("password").FirstOrDefault().Value;
            version = credential.Descendants("version").FirstOrDefault().Value;
            #endregion

            List<Tourico.Hotel> result = new List<Tourico.Hotel>();

            Tourico.AuthenticationHeader hd = new Tourico.AuthenticationHeader();
            hd.LoginName = userlogin;// "HOL916";
            hd.Password = pwd;// "111111";
            hd.Version = version;// "5";

            Tourico.HotelFlowClient client = new Tourico.HotelFlowClient();

            Tourico.SearchRequest request = new Tourico.SearchRequest();

            List<XElement> trum = reqTravillio.Descendants("RoomPax").ToList();
            Tourico.RoomInfo[] roominfo = new Tourico.RoomInfo[trum.Count()];

            for (int j = 0; j < trum.Count(); j++)
            {
                int childcount = Convert.ToInt32(trum[j].Element("Child").Value);
                Tourico.ChildAge[] chdage = new Tourico.ChildAge[childcount];
                for (int k = 0; k < childcount; k++)
                {
                    chdage[k] = new Tourico.ChildAge
                    {
                        age = Convert.ToInt32(trum[j].Element("ChildAge").Value)
                    };
                }

                roominfo[j] = new Tourico.RoomInfo
                {
                    AdultNum = Convert.ToInt32(trum[j].Element("Adult").Value),
                    ChildNum = Convert.ToInt32(trum[j].Element("Child").Value),
                    ChildAges = chdage
                };

            }

            request.Destination = reqTravillio.Descendants("CityCode").Single().Value.ToString();

            request.CheckIn = DateTime.ParseExact(reqTravillio.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
            request.CheckOut = DateTime.ParseExact(reqTravillio.Descendants("ToDate").Single().Value, "dd/MM/yyyy", null);
            request.RoomsInformation = roominfo;
            request.MaxPrice = 0;
            request.StarLevel = Convert.ToInt32(reqTravillio.Descendants("MinStarRating").Single().Value);
            request.AvailableOnly = true;


            request.PropertyType = 0;

            request.ExactDestination = true;

            Tourico.Feature[] feature = new Tourico.Feature[1];
            for (int i = 0; i < 1; i++)
            {
                feature[i] = new Tourico.Feature { name = "", value = "" };
            }



            hotelavailabilityresult = client.SearchHotels(hd, request, feature).HotelList.ToList();


            //List<Tourico.Hotel> hotl = hotelavailabilityresult.Where(x => x.hotelId == 2207).ToList();


            #endregion

        }
        #region Extranet Availability
        public void gethotelavailabilityExtranet()
        {
            List<XElement> doc1 = new List<XElement>();
            HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
            #region Extranet Request/Response
            string requestxml = string.Empty;
            requestxml = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                          "<soapenv:Header xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                            "<Authentication>" +
                              "<AgentID>0</AgentID>" +
                              "<UserName>Suraj</UserName>" +
                              "<Password>123#</Password>" +
                              "<ServiceType>HT_001</ServiceType>" +
                              "<ServiceVersion>v1.0</ServiceVersion>" +
                            "</Authentication>" +
                          "</soapenv:Header>" +
                          "<soapenv:Body>" +
                            "<searchRequest>" +
                              "<Response_Type>XML</Response_Type>" +
                              "<CustomerID>" + reqTravillio.Descendants("CustomerID").Single().Value + "</CustomerID>" +
                              "<FromDate>" + reqTravillio.Descendants("FromDate").SingleOrDefault().Value + "</FromDate>" +
                              "<ToDate>" + reqTravillio.Descendants("ToDate").SingleOrDefault().Value + "</ToDate>" +
                              "<Nights>1</Nights>" +
                              "<CountryID>0</CountryID>" +
                              "<CountryName />" +
                              "<CityCode>" + reqTravillio.Descendants("CityCode").Single().Value + "</CityCode>" +
                              "<CityName>" + reqTravillio.Descendants("CityCode").Single().Value + "</CityName>" +
                              "<AreaID></AreaID>" +
                              "<AreaName />" +
                              "<MinStarRating>" + reqTravillio.Descendants("MinStarRating").Single().Value + "</MinStarRating>" +
                              "<MaxStarRating>" + reqTravillio.Descendants("MaxStarRating").Single().Value + "</MaxStarRating>" +
                              "<HotelName></HotelName>" +
                              "<PaxNationality_CountryID>" + reqTravillio.Descendants("PaxNationality_CountryCode").Single().Value + "</PaxNationality_CountryID>" +
                              "<CurrencyID>1</CurrencyID>" +
                              reqTravillio.Descendants("Rooms").SingleOrDefault().ToString() +
                              "<MealPlanList>" +
                                "<MealType>1</MealType>" +
                                "<MealType>2</MealType>" +
                                "<MealType>3</MealType>" +
                                "<MealType>4</MealType>" +
                                "<MealType>5</MealType>" +
                              "</MealPlanList>" +
                              "<PropertyType>1</PropertyType>" +
                              "<SuppliersList />" +
                              "<SubAgentID>0</SubAgentID>" +
                            "</searchRequest>" +
                          "</soapenv:Body>" +
                        "</soapenv:Envelope>";
            try
            {

                object result = extclient.GetSearchCityRequestByXML(requestxml,false);
                if (result != null)
                {
                    XElement doc = XElement.Parse(result.ToString());
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(reqTravillio.Descendants("CustomerID").Single().Value);
                        log.LogTypeID = 2;
                        log.LogType = "RoomAvail";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = doc.ToString();
                        //APILog.SaveAPILogs(log);
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                       
                    }
                    hotelavailabilitylistextranet = doc.Descendants("Hotel").ToList();
                }
                else
                {

                    hotelavailabilitylistextranet = doc1.Descendants("Hotel").ToList();

                }
            }
            catch (Exception ex)
            {

                hotelavailabilitylistextranet = doc1.Descendants("Hotel").ToList();
            }


            #endregion
        }
        #endregion
        #endregion
        #region Darina Holidays
        #region Darina's Hotel Listing
        private IEnumerable<XElement> GetHotelList(List<XElement> htlist, IEnumerable<XElement> htlistdetail, XElement docmealplan, XElement dococcupancy, IEnumerable<XElement> currencycode)
        {
            #region Darina Hotel List
            List<XElement> hotellst = new List<XElement>();
            try
            {
                Int32 length = htlist.Descendants("Hotels").Count();
                List<XElement> hotellist = htlist.Descendants("Hotels").ToList();
                try
                {
                    Parallel.For(0, length, i =>
                    {

                        IEnumerable<XElement> hoteldetails = htlistdetail.Descendants("d0").Where(x => x.Descendants("HotelID").Single().Value == hotellist[i].Element("HotelID").Value);
                        List<XElement> hotelfacilities = htlistdetail.Descendants("HFac").Where(x => x.Descendants("HotelID").Single().Value == hotellist[i].Element("HotelID").Value).ToList();
                        List<XElement> roomlist = htlist.Descendants("AccRates").Where(x => x.Descendants("Hotel").Single().Value == hotellist[i].Element("HotelID").Value).ToList();
                        string map = Convert.ToString(hotellist[i].Element("Maplink").Value);

                        hotellst.Add(new XElement("Hotel",
                                               new XElement("HotelID", Convert.ToString(hotellist[i].Element("HotelID").Value)),
                                               new XElement("HotelName", Convert.ToString(hotellist[i].Element("Name").Value)),
                                               new XElement("PropertyTypeName", Convert.ToString(hoteldetails.Descendants("ProprtyTypeName").Single().Value)),
                                               new XElement("CountryID", Convert.ToString(hoteldetails.Descendants("CountryID").Single().Value)),
                                               new XElement("CountryName", Convert.ToString(hoteldetails.Descendants("CountryName").Single().Value)),
                                               new XElement("CountryCode", Convert.ToString(hoteldetails.Descendants("CountryCode").Single().Value)),
                                               new XElement("CityId", Convert.ToString(hoteldetails.Descendants("CityID").Single().Value)),
                                               new XElement("CityCode", Convert.ToString(hoteldetails.Descendants("CityCode").Single().Value)),
                                               new XElement("CityName", Convert.ToString(hoteldetails.Descendants("CityName").Single().Value)),
                                               new XElement("AreaId", Convert.ToString(hoteldetails.Descendants("AreaID").Single().Value)),
                                               new XElement("AreaName", Convert.ToString(hoteldetails.Descendants("AreaName").Single().Value))
                                               , new XElement("Address", Convert.ToString(hoteldetails.Descendants("Address").Single().Value)),
                                               new XElement("Location", Convert.ToString(hoteldetails.Descendants("Address").Single().Value)),
                                               new XElement("Description", ""),
                                               new XElement("StarRating", Convert.ToString(hoteldetails.Descendants("Landcategory").Single().Value)),
                                               new XElement("MinRate", Convert.ToString(roomlist.Descendants("RatePerStay").FirstOrDefault().Value))
                                               , new XElement("HotelImgSmall", Convert.ToString(hotellist[i].Element("SmallImgLink").Value)),
                                               new XElement("HotelImgLarge", Convert.ToString(hotellist[i].Element("LargeImgLink").Value)),
                                               new XElement("MapLink", map),
                                               new XElement("Longitude", ""),
                                               new XElement("Latitude", ""),
                                               new XElement("DMC", "Darina"),
                                               new XElement("SupplierID", "1"),
                                               new XElement("Currency", Convert.ToString(currencycode.Descendants("CurrencyName").Single().Value)),
                                               new XElement("Offers", "")
                                               , new XElement("Facilities",
                                                   GetHotelFacilities(hotelfacilities))
                                               , new XElement("Rooms",

                                                    GetHotelRooms(htlist, roomlist, docmealplan, dococcupancy)
                                                   )
                        ));

                    });
                }
                catch (Exception ex)
                {
                    return hotellst;
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
        #region Darina's Hotel Room's Listing
        public IEnumerable<XElement> GetHotelRooms(List<XElement> htlist, List<XElement> roomlist, XElement docmealplan, XElement dococcupancy)
        {
            #region Darina Hotel's Room List
            List<XElement> str = new List<XElement>();
            IEnumerable<XElement> roomtypes = htlist;
            DateTime fromDate = DateTime.ParseExact(reqTravillio.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
            DateTime toDate = DateTime.ParseExact(reqTravillio.Descendants("ToDate").Single().Value, "dd/MM/yyyy", null);
            int nights = (int)(toDate - fromDate).TotalDays;
            Parallel.For(0, roomlist.Count(), i =>
            {

                IEnumerable<XElement> docmealplandet = docmealplan.Descendants("d0").Where(x => x.Descendants("MealPlanID").Single().Value == roomlist[i].Descendants("MealPlan").Single().Value);
                IEnumerable<XElement> dococcupancydet = dococcupancy.Descendants("d0").Where(x => x.Descendants("OccupancyID").Single().Value == roomlist[i].Descendants("Occupancy").Single().Value);
                IEnumerable<XElement> room = roomtypes.Descendants("RoomTypes").Where(x => x.Descendants("RoomTypeID").Single().Value == roomlist[i].Descendants("RoomType").Single().Value);
                string isavailableval = roomlist[i].Descendants("Availability").Single().Value;
                string isavailable = string.Empty;
                if (isavailableval == "1")
                {
                    isavailable = "true";
                }
                else
                {
                    isavailable = "false";
                }
                str.Add(new XElement("RoomTypes", new XAttribute("Index", i + 1), new XAttribute("TotalRate", roomlist[i].Descendants("RatePerStay").Single().Value),
                    new XElement("Room",
                         new XAttribute("ID", Convert.ToString(roomlist[i].Descendants("RoomType").Single().Value)),
                         new XAttribute("SuppliersID", "1"),
                         new XAttribute("RoomSeq", "1"),
                         new XAttribute("SessionID", Convert.ToString(roomlist[i].Descendants("Serial").Single().Value)),
                         new XAttribute("RoomType", Convert.ToString(room.Descendants("RoomTypeName").Single().Value)),
                         new XAttribute("OccupancyID", Convert.ToString(roomlist[i].Descendants("Occupancy").Single().Value)),
                         new XAttribute("OccupancyName", Convert.ToString(dococcupancydet.Descendants("OccupancyName").Single().Value)),
                         new XAttribute("MealPlanID", Convert.ToString(roomlist[i].Descendants("MealPlan").Single().Value)),
                         new XAttribute("MealPlanName", Convert.ToString(docmealplandet.Descendants("MealPlanName").Single().Value)),
                         new XAttribute("MealPlanCode", ""),
                         new XAttribute("MealPlanPrice", ""),
                         new XAttribute("PerNightRoomRate", Convert.ToString(roomlist[i].Descendants("RatePerNight").Single().Value)),
                         new XAttribute("TotalRoomRate", Convert.ToString(roomlist[i].Descendants("RatePerStay").Single().Value)),
                         new XAttribute("CancellationDate", ""),
                         new XAttribute("CancellationAmount", ""),
                         new XAttribute("isAvailable", isavailable),
                         new XElement("RequestID", Convert.ToString(roomlist[i].Descendants("RequestID").Single().Value)),
                         new XElement("Offers", ""),
                         new XElement("PromotionList",
                         new XElement("Promotions", Convert.ToString(roomlist[i].Descendants("Offers").Single().Value))),
                         new XElement("CancellationPolicy", ""),
                         new XElement("Amenities", new XElement("Amenity", "")),
                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                         new XElement("Supplements", new XElement("Supplement", new XAttribute("suppId", ""), new XAttribute("suppName", ""), new XAttribute("supptType", "")
                             , new XAttribute("suppIsMandatory", false), new XAttribute("suppChargeType", ""), new XAttribute("suppPrice", ""), new XAttribute("suppType", Convert.ToString("")))),
                             new XElement("PriceBreakups",
                                 GetRoomsPriceBreakupDarina(nights, Convert.ToString(roomlist[i].Descendants("RatePerNight").Single().Value))),
                                 new XElement("AdultNum", Convert.ToString(reqTravillio.Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value)),
                                 new XElement("ChildNum", Convert.ToString(reqTravillio.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value))
                         )));
            });
            return str;
            #endregion
        }
        #endregion
        #region Darina's Hotel Facilities
        private IEnumerable<XElement> GetHotelFacilities(List<XElement> hotelfacilities)
        {

            Int32 length = hotelfacilities.Count();
            List<XElement> Facilities = new List<XElement>();

            if (length == 0)
            {
                Facilities.Add(new XElement("Facility", "No Facility Available"));
            }
            else
            {

                Parallel.For(0, length, i =>
                {

                    Facilities.Add(new XElement("Facility", Convert.ToString(hotelfacilities[i].Descendants("Facility").Single().Value)));

                });
            }
            return Facilities;
        }
        #endregion
        private IEnumerable<XElement> GetRoomsPriceBreakupDarina(int nights, string pernightprice)
        {
            #region Darina Room's Price Breakups
            List<XElement> str = new List<XElement>();
            Parallel.For(0, nights, i =>
            {
                str.Add(new XElement("Price",
                       new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                       new XAttribute("PriceValue", Convert.ToString(pernightprice)))
                );
            });
            return str;
            #endregion
        }
        #endregion
        #region Tourico Holidays
        #region Tourico Hotel Listing
        private IEnumerable<XElement> GetHotelListTourico(List<Tourico.Hotel> htlist)
        {
            #region Tourico Hotels
            List<XElement> hotellst = new List<XElement>();
            try
            {
                Int32 length = htlist.Count();
                int minstarlevel = Convert.ToInt32(reqTravillio.Descendants("MinStarRating").Single().Value);
                int maxstarlevel = Convert.ToInt32(reqTravillio.Descendants("MaxStarRating").Single().Value);
                #region For Static Data
                XElement staticallhotellist = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\HotelInfo.xml"));
                List<XElement> statichotellist = staticallhotellist.Descendants("Hotel").Where(x => x.Descendants("sDestination").SingleOrDefault().Value == reqTravillio.Descendants("CityCode").SingleOrDefault().Value).ToList();

                #endregion
                try
                {
                    Parallel.For(0, length, i =>
                    {
                        #region Fetch hotel according to star category
                        if (Convert.ToInt32(htlist[i].starsLevel) >= minstarlevel && Convert.ToInt32(htlist[i].starsLevel) <= maxstarlevel)
                        {

                            IEnumerable<XElement> hoteldetail = statichotellist.Where(x => Convert.ToInt32(x.Descendants("HotelId").SingleOrDefault().Value) == htlist[i].hotelId);
                            if (hoteldetail.ToList().Count() > 0)
                            {
                                decimal totalamt;
                                string exclusivedeal = string.Empty;
                                if (htlist[i].bestValue == true)
                                {
                                    exclusivedeal = "Exclusive Deal";
                                }
                                List<Tourico.RoomType> rmtypelist = new List<Tourico.RoomType>();
                                rmtypelist = htlist[i].RoomTypes.ToList();
                                int bb = rmtypelist[0].Occupancies[0].BoardBases.Count();
                                if (bb > 0)
                                {
                                    totalamt = rmtypelist[0].Occupancies[0].occupPublishPrice + rmtypelist[0].Occupancies[0].BoardBases[0].bbPublishPrice;
                                }
                                else
                                {
                                    totalamt = htlist[i].RoomTypes[0].Occupancies[0].occupPrice;
                                }
                                hotellst.Add(new XElement("Hotel",
                                    //new XElement("HotelID", Convert.ToString(htlist[i].hotelId)),
                                    //new XElement("HotelName", Convert.ToString(htlist[i].name)),
                                                       new XElement("HotelID", Convert.ToString(hoteldetail.Descendants("HotelId").SingleOrDefault().Value)),
                                                       new XElement("HotelName", Convert.ToString(hoteldetail.Descendants("HotelName").SingleOrDefault().Value)),
                                                       new XElement("PropertyTypeName", Convert.ToString(htlist[i].PropertyType)),
                                                       new XElement("CountryID", Convert.ToString(hoteldetail.Descendants("CountryCode").SingleOrDefault().Value)),
                                                       new XElement("CountryName", Convert.ToString(hoteldetail.Descendants("CountryName").SingleOrDefault().Value)),
                                                       new XElement("CountryCode", Convert.ToString(hoteldetail.Descendants("CountryCode").SingleOrDefault().Value)),
                                    //new XElement("CountryCode", Convert.ToString(htlist[i].Location.countryCode)),
                                                       new XElement("CityId", Convert.ToString("")),
                                    //new XElement("CityCode", Convert.ToString(htlist[i].Location.stateCode)),
                                                       new XElement("CityCode", Convert.ToString(hoteldetail.Descendants("sDestination").SingleOrDefault().Value)),
                                    //new XElement("CityName", Convert.ToString(htlist[i].Location.city)),
                                                       new XElement("CityName", Convert.ToString(hoteldetail.Descendants("AddressCity").SingleOrDefault().Value)),
                                                       new XElement("AreaId", Convert.ToString("")),
                                                       new XElement("AreaName", Convert.ToString(hoteldetail.Descendants("Location").SingleOrDefault().Value)),
                                    //new XElement("Address", Convert.ToString(htlist[i].Location.address)),
                                    //new XElement("Location", Convert.ToString(htlist[i].Location.location)),
                                                       new XElement("Address", Convert.ToString(hoteldetail.Descendants("Address").SingleOrDefault().Value)),
                                                       new XElement("Location", Convert.ToString(hoteldetail.Descendants("Location").SingleOrDefault().Value)),
                                                       new XElement("Description", Convert.ToString(htlist[i].desc)),
                                    //new XElement("StarRating", Convert.ToString(htlist[i].starsLevel)),
                                                       new XElement("StarRating", Convert.ToString(hoteldetail.Descendants("Stars").SingleOrDefault().Value)),
                                                       new XElement("MinRate", Convert.ToString(totalamt))
                                                       , new XElement("HotelImgSmall", Convert.ToString(hoteldetail.Descendants("ThumbnailPath").SingleOrDefault().Value)),
                                                       new XElement("HotelImgLarge", Convert.ToString(hoteldetail.Descendants("ThumbnailPath").SingleOrDefault().Value)),
                                                       new XElement("MapLink", ""),
                                    //new XElement("Longitude", Convert.ToString(htlist[i].Location.longitude)),
                                    //new XElement("Latitude", Convert.ToString(htlist[i].Location.latitude)),
                                                       new XElement("Longitude", Convert.ToString(hoteldetail.Descendants("Longitude").SingleOrDefault().Value)),
                                                       new XElement("Latitude", Convert.ToString(hoteldetail.Descendants("Latitude").SingleOrDefault().Value)),
                                                       new XElement("DMC", "Tourico"),
                                                       new XElement("SupplierID", "2"),
                                                       new XElement("Currency", Convert.ToString(htlist[i].currency)),
                                                       new XElement("Offers", Convert.ToString(exclusivedeal))
                                                       , new XElement("Facilities",
                                                           new XElement("Facility", "No Facility Available"))
                                                       , new XElement("Rooms",
                                                             GetHotelRoomgroupTourico(rmtypelist)
                                                           )
                                ));
                            }
                        }
                        #endregion
                    });
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
        #region Tourico Hotel's Room Grouping
        public IEnumerable<XElement> GetHotelRoomgroupTourico(List<Tourico.RoomType> roomlist)
        {
            List<XElement> str = new List<XElement>();
            List<XElement> strgrp = new List<XElement>();
            int rindex = 1;
            Parallel.For(0, roomlist.Count(), i =>
            //for(int i=0;i< roomlist.Count();i++)
            {
                //int bb = roomlist[i].Occupancies[0].BoardBases.Count();
                //if (bb > 1)
                //{

                //    str = GetHotelRoomListingTourico(roomlist[i]).ToList();

                //    for (int j = 0; j < str.Count(); j++)
                //    {

                //        str[j].Add(new XAttribute("Index", rindex));
                //        strgrp.Add(str[j]);
                //        rindex++;
                //    }    

                //}
                //else
                {
                    str = GetHotelRoomListingTourico(roomlist[i]).ToList();
                    Parallel.For(0, str.Count(), k =>
                    //for (int k = 0; k < str.Count(); k++)
                    {

                        str[k].Add(new XAttribute("Index", rindex));
                        strgrp.Add(str[k]);
                        rindex++;
                    });


                }
            });
            return strgrp;
        }
        #endregion
        #region Tourico Hotel's Room Listing
        public IEnumerable<XElement> GetHotelRoomListingTourico(Tourico.RoomType roomlist)
        {
            List<XElement> str = new List<XElement>();
            List<Tourico.Occupancy> roomList1 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList2 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList3 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList4 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList5 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList6 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList7 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList8 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList9 = new List<Tourico.Occupancy>();
            int totalroom = Convert.ToInt32(reqTravillio.Descendants("RoomPax").Count());

            #region Notes: The maximum number of rooms that can be retrieved by a single search request is nine (9)
            #endregion

            #region Room Count 1
            if (totalroom == 1)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {

                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                    });

                });
                //List<Tourico.Occupancy> roomList1 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 1).ToList();

                for (int m = 0; m < roomList1.Count(); m++)
                {
                    // add room 1

                    #region room's group

                    int bb = roomlist.Occupancies[m].BoardBases.Count();
                    List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                    List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                    string promotion = string.Empty;
                    if (roomlist.Discount != null)
                    {
                        #region Discount (Tourico) Amount already subtracted
                        try
                        {
                            XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                            XmlDocument doc3 = new XmlDocument();
                            System.IO.StringWriter sww3 = new System.IO.StringWriter();
                            XmlWriter writer3 = XmlWriter.Create(sww3);
                            xsSubmit3.Serialize(writer3, roomlist.Discount);
                            var typxsd = XDocument.Parse(sww3.ToString());
                            var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                            var distype = typxsd.Root.Attribute(disprefix + "type");
                            if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                            {
                                promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                            }
                            else
                            {
                                promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                        #endregion
                    }
                    if (bb == 0)
                    {
                        group++;
                        #region No Board Bases
                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].occupPublishPrice),
                        new XElement("Room",
                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                             new XAttribute("SuppliersID", "2"),
                             new XAttribute("RoomSeq", "1"),
                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                             new XAttribute("MealPlanID", Convert.ToString("")),
                             new XAttribute("MealPlanName", Convert.ToString("")),
                             new XAttribute("MealPlanCode", ""),
                             new XAttribute("MealPlanPrice", ""),
                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                             new XAttribute("CancellationDate", ""),
                             new XAttribute("CancellationAmount", ""),
                             new XAttribute("isAvailable", roomlist.isAvailable),
                             new XElement("RequestID", Convert.ToString("")),
                             new XElement("Offers", ""),
                             new XElement("PromotionList",
                             new XElement("Promotions", Convert.ToString(promotion))),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements",
                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                 ),
                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                             )));
                        #endregion
                    }
                    else
                    {
                        bool RO = false;
                        #region Board Bases >0
                        Parallel.For(0, bb, j =>
                        //for (int j = 0; j < bb; j++)
                        {
                            int countpaidnight1 = 0;
                            Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                            {
                                if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                {
                                    countpaidnight1 = countpaidnight1 + 1;
                                }
                            });
                            if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                            { RO = true; }
                            group++;
                            decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;

                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalamt),

                            new XElement("Room",
                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                             new XAttribute("SuppliersID", "2"),
                             new XAttribute("RoomSeq", "1"),
                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                             new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                             new XAttribute("MealPlanCode", ""),
                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                             new XAttribute("CancellationDate", ""),
                             new XAttribute("CancellationAmount", ""),
                              new XAttribute("isAvailable", roomlist.isAvailable),
                             new XElement("RequestID", Convert.ToString("")),
                             new XElement("Offers", ""),
                              new XElement("PromotionList",
                             new XElement("Promotions", Convert.ToString(promotion))),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements",
                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                 ),
                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                             )));


                        });

                        #region Room Only
                        if (RO == true)
                        {
                            int countpaidnight1 = 0;
                            Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                            {
                                if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                {
                                    countpaidnight1 = countpaidnight1 + 1;
                                }
                            });
                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].occupPublishPrice),

                       new XElement("Room",
                        new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                        new XAttribute("SuppliersID", "2"),
                        new XAttribute("RoomSeq", "1"),
                        new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                        new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                        new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                        new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                        new XAttribute("MealPlanID", Convert.ToString("")),
                        new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                        new XAttribute("MealPlanCode", ""),
                        new XAttribute("MealPlanPrice", Convert.ToString("")),
                        new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].occupPublishPrice / countpaidnight1)),
                        new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                        new XAttribute("CancellationDate", ""),
                        new XAttribute("CancellationAmount", ""),
                         new XAttribute("isAvailable", roomlist.isAvailable),
                        new XElement("RequestID", Convert.ToString("")),
                        new XElement("Offers", ""),
                         new XElement("PromotionList",
                        new XElement("Promotions", Convert.ToString(promotion))),
                        new XElement("CancellationPolicy", ""),
                        new XElement("Amenities", new XElement("Amenity", "")),
                        new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                        new XElement("Supplements",
                            GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                            ),
                            new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                            new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                            new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                        )));
                        }
                        #endregion

                        #endregion
                    }

                    //return str;
                    #endregion

                }
                return str;
            }
            #endregion
            #region Room Count 2
            if (totalroom == 2)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {

                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {

                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                    });

                });
                //List<Tourico.Occupancy> roomList1 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 1).ToList();
                //List<Tourico.Occupancy> roomList2 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 2).ToList();
                Parallel.For(0, roomList1.Count(), m =>
                //for (int m = 0; m < roomList1.Count(); m++)
                {
                    Parallel.For(0, roomList2.Count(), n =>
                    //for (int n = 0; n < roomList2.Count(); n++)
                    {

                        // add room 1, 2

                        #region room's group

                        int bb = roomlist.Occupancies[m].BoardBases.Count();
                        List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                        List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                        string promotion = string.Empty;
                        if (roomlist.Discount != null)
                        {
                            #region Discount (Tourico) Amount already subtracted
                            try
                            {
                                XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                XmlDocument doc3 = new XmlDocument();
                                System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                XmlWriter writer3 = XmlWriter.Create(sww3);
                                xsSubmit3.Serialize(writer3, roomlist.Discount);
                                var typxsd = XDocument.Parse(sww3.ToString());
                                var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                var distype = typxsd.Root.Attribute(disprefix + "type");
                                if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                {
                                    promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                }
                                else
                                {
                                    promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                            #endregion
                        }

                        if (bb == 0)
                        {
                            group++;
                            decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice;
                            #region No Board Bases
                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                            new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "1"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", ""),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                 ),

                            new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "2"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", ""),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                 )));
                            #endregion
                        }
                        else
                        {
                            bool RO = false;
                            #region Board Bases >0
                            Parallel.For(0, bb, j =>
                            //for (int j = 0; j < bb; j++)
                            {
                                int countpaidnight1 = 0;
                                Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                {
                                    if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                    {
                                        countpaidnight1 = countpaidnight1 + 1;
                                    }
                                });
                                int countpaidnight2 = 0;
                                Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                {
                                    if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                    {
                                        countpaidnight2 = countpaidnight2 + 1;
                                    }
                                });
                                if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                { RO = true; }
                                if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                { RO = true; }
                                group++;
                                decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                decimal totalp = totalamt + totalamt2;
                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "1"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                 new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                 ),

                                new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "2"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                 new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                 )));



                            });
                            #region RO
                            if (RO == true)
                            {
                                int countpaidnight1 = 0;
                                Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                {
                                    if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                    {
                                        countpaidnight1 = countpaidnight1 + 1;
                                    }
                                });
                                int countpaidnight2 = 0;
                                Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                {
                                    if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                    {
                                        countpaidnight2 = countpaidnight2 + 1;
                                    }
                                });

                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice),

                                new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "1"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                 new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].occupPublishPrice / countpaidnight1)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                 ),

                                new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "2"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                 new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].occupPublishPrice / countpaidnight2)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                 )));
                            }
                            #endregion
                            #endregion
                        }

                        //return str;
                        #endregion

                    });
                });
                return str;
            }
            #endregion
            #region Room Count 3
            if (totalroom == 3)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {

                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {

                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {

                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                //List<Tourico.Occupancy> roomList1 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 1).ToList();
                //List<Tourico.Occupancy> roomList2 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 2).ToList();
                //List<Tourico.Occupancy> roomList3 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 3).ToList();
                #region Room 3
                Parallel.For(0, roomList1.Count(), m =>
                //for (int m = 0; m < roomList1.Count(); m++)
                {
                    Parallel.For(0, roomList2.Count(), n =>
                    //for (int n = 0; n < roomList2.Count(); n++)
                    {
                        Parallel.For(0, roomList3.Count(), o =>
                        //for (int o = 0; o < roomList3.Count(); o++)
                        {
                            // add room 1, 2, 3

                            #region room's group

                            int bb = roomlist.Occupancies[m].BoardBases.Count();
                            List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                            List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                            string promotion = string.Empty;
                            if (roomlist.Discount != null)
                            {
                                #region Discount (Tourico) Amount already subtracted
                                try
                                {
                                    XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                    XmlDocument doc3 = new XmlDocument();
                                    System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                    XmlWriter writer3 = XmlWriter.Create(sww3);
                                    xsSubmit3.Serialize(writer3, roomlist.Discount);
                                    var typxsd = XDocument.Parse(sww3.ToString());
                                    var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                    var distype = typxsd.Root.Attribute(disprefix + "type");
                                    if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                    {
                                        promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                    }
                                    else
                                    {
                                        promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                    }
                                }
                                catch (Exception ex)
                                {

                                }
                                #endregion
                            }

                            if (bb == 0)
                            {
                                group++;
                                decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice;
                                #region No Board Bases
                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                                new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "1"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", ""),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                     ),

                                new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "2"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", ""),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                     ),

                                new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "3"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", ""),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                     )));
                                #endregion
                            }
                            else
                            {
                                bool RO = false;
                                #region Board Bases >0
                                Parallel.For(0, bb, j =>
                                //for (int j = 0; j < bb; j++)
                                {
                                    int countpaidnight1 = 0;
                                    Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight1 = countpaidnight1 + 1;
                                        }
                                    });
                                    int countpaidnight2 = 0;
                                    Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight2 = countpaidnight2 + 1;
                                        }
                                    });
                                    int countpaidnight3 = 0;
                                    Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight3 = countpaidnight3 + 1;
                                        }
                                    });
                                    if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                    { RO = true; }
                                    if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                    { RO = true; }
                                    if (roomList3[o].BoardBases[j].bbPublishPrice > 0)
                                    { RO = true; }
                                    group++;
                                    decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                    decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                    decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                    decimal totalp = totalamt + totalamt2 + totalamt3;

                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "1"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                     ),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "2"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                     ),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "3"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3 / countpaidnight3)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                     )));
                                });
                                #endregion
                                #region RO
                                if (RO == true)
                                {
                                    int countpaidnight1 = 0;
                                    Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight1 = countpaidnight1 + 1;
                                        }
                                    });
                                    int countpaidnight2 = 0;
                                    Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight2 = countpaidnight2 + 1;
                                        }
                                    });
                                    int countpaidnight3 = 0;
                                    Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight3 = countpaidnight3 + 1;
                                        }
                                    });
                                    decimal totalamt = roomList1[m].occupPublishPrice;
                                    decimal totalamt2 = roomList2[n].occupPublishPrice;
                                    decimal totalamt3 = roomList3[o].occupPublishPrice;
                                    decimal totalp = totalamt + totalamt2 + totalamt3;

                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                                                        new XElement("Room",
                                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                                         new XAttribute("SuppliersID", "2"),
                                                                         new XAttribute("RoomSeq", "1"),
                                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                                         new XAttribute("MealPlanCode", ""),
                                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                                         new XAttribute("CancellationDate", ""),
                                                                         new XAttribute("CancellationAmount", ""),
                                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                                         new XElement("RequestID", Convert.ToString("")),
                                                                         new XElement("Offers", ""),
                                                                          new XElement("PromotionList",
                                                                         new XElement("Promotions", Convert.ToString(promotion))),
                                                                         new XElement("CancellationPolicy", ""),
                                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                         new XElement("Supplements",
                                                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                                             ),
                                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                                         ),

                                                                        new XElement("Room",
                                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                                         new XAttribute("SuppliersID", "2"),
                                                                         new XAttribute("RoomSeq", "2"),
                                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                                         new XAttribute("MealPlanCode", ""),
                                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                                         new XAttribute("CancellationDate", ""),
                                                                         new XAttribute("CancellationAmount", ""),
                                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                                         new XElement("RequestID", Convert.ToString("")),
                                                                         new XElement("Offers", ""),
                                                                          new XElement("PromotionList",
                                                                         new XElement("Promotions", Convert.ToString(promotion))),
                                                                         new XElement("CancellationPolicy", ""),
                                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                         new XElement("Supplements",
                                                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                                             ),
                                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                                         ),

                                                                        new XElement("Room",
                                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                                         new XAttribute("SuppliersID", "2"),
                                                                         new XAttribute("RoomSeq", "3"),
                                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                                         new XAttribute("MealPlanCode", ""),
                                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3 / countpaidnight3)),
                                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                                         new XAttribute("CancellationDate", ""),
                                                                         new XAttribute("CancellationAmount", ""),
                                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                                         new XElement("RequestID", Convert.ToString("")),
                                                                         new XElement("Offers", ""),
                                                                          new XElement("PromotionList",
                                                                         new XElement("Promotions", Convert.ToString(promotion))),
                                                                         new XElement("CancellationPolicy", ""),
                                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                         new XElement("Supplements",
                                                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                                             ),
                                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                                         )));
                                }
                                #endregion
                            }

                            //return str;
                            #endregion
                        });
                    });
                });
                return str;
                #endregion

            }
            #endregion
            #region Room Count 4
            if (totalroom == 4)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {

                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {

                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {

                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {

                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 4
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                // add room 1, 2, 3, 4

                                #region room's group

                                int bb = roomlist.Occupancies[m].BoardBases.Count();
                                List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                string promotion = string.Empty;
                                if (roomlist.Discount != null)
                                {
                                    #region Discount (Tourico) Amount already subtracted
                                    try
                                    {
                                        XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                        XmlDocument doc3 = new XmlDocument();
                                        System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                        XmlWriter writer3 = XmlWriter.Create(sww3);
                                        xsSubmit3.Serialize(writer3, roomlist.Discount);
                                        var typxsd = XDocument.Parse(sww3.ToString());
                                        var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                        var distype = typxsd.Root.Attribute(disprefix + "type");
                                        if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                        {
                                            promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                        }
                                        else
                                        {
                                            promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                    #endregion
                                }

                                if (bb == 0)
                                {
                                    group++;
                                    decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice;

                                    #region No Board Bases
                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "1"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                         ),

                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "2"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                         ),

                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "3"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                         ),

                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "4"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                         )));
                                    #endregion
                                }
                                else
                                {
                                    bool RO = false;
                                    #region Board Bases >0
                                    for (int j = 0; j < bb; j++)
                                    {
                                        int countpaidnight1 = 0;
                                        Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight1 = countpaidnight1 + 1;
                                            }
                                        });
                                        int countpaidnight2 = 0;
                                        Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight2 = countpaidnight2 + 1;
                                            }
                                        });
                                        int countpaidnight3 = 0;
                                        Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight3 = countpaidnight3 + 1;
                                            }
                                        });
                                        int countpaidnight4 = 0;
                                        Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList4[p].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight4 = countpaidnight4 + 1;
                                            }
                                        });
                                        if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                        { RO = true; }
                                        if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                        { RO = true; }
                                        if (roomList3[o].BoardBases[j].bbPublishPrice > 0)
                                        { RO = true; }
                                        if (roomList4[p].BoardBases[j].bbPublishPrice > 0)
                                        { RO = true; }
                                        group++;
                                        decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                        decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                        decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                        decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4;

                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "1"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "2"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "3"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3 / countpaidnight3)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "4"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt4 / countpaidnight4)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                         )));
                                    }
                                    #endregion
                                    #region RO
                                    if (RO == true)
                                    {
                                        int countpaidnight1 = 0;
                                        Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight1 = countpaidnight1 + 1;
                                            }
                                        });
                                        int countpaidnight2 = 0;
                                        Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight2 = countpaidnight2 + 1;
                                            }
                                        });
                                        int countpaidnight3 = 0;
                                        Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight3 = countpaidnight3 + 1;
                                            }
                                        });
                                        int countpaidnight4 = 0;
                                        Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList4[p].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight4 = countpaidnight4 + 1;
                                            }
                                        });

                                        group++;
                                        decimal totalamt = roomList1[m].occupPublishPrice;
                                        decimal totalamt2 = roomList2[n].occupPublishPrice;
                                        decimal totalamt3 = roomList3[o].occupPublishPrice;
                                        decimal totalamt4 = roomList4[p].occupPublishPrice;
                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4;

                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "1"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "2"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "3"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3 / countpaidnight3)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "4"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt4 / countpaidnight4)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                         )));
                                    }
                                    #endregion
                                }
                                #endregion
                            }
                        }
                    }
                }
                return str;
                #endregion

            }
            #endregion
            #region Room Count 5
            if (totalroom == 5)
            {

                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 5
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    // add room 1, 2, 3, 4, 5

                                    #region room's group

                                    int bb = roomlist.Occupancies[m].BoardBases.Count();
                                    List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                    List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                    string promotion = string.Empty;
                                    if (roomlist.Discount != null)
                                    {
                                        #region Discount (Tourico) Amount already subtracted
                                        try
                                        {
                                            XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                            XmlDocument doc3 = new XmlDocument();
                                            System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                            XmlWriter writer3 = XmlWriter.Create(sww3);
                                            xsSubmit3.Serialize(writer3, roomlist.Discount);
                                            var typxsd = XDocument.Parse(sww3.ToString());
                                            var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                            var distype = typxsd.Root.Attribute(disprefix + "type");
                                            if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                            {
                                                promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                            }
                                            else
                                            {
                                                promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                            }
                                        }
                                        catch (Exception ex)
                                        {

                                        }
                                        #endregion
                                    }

                                    if (bb == 0)
                                    {
                                        group++;
                                        decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice;

                                        #region No Board Bases
                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "1"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                             new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "2"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "3"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "4"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "5"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                             )));
                                        #endregion
                                    }
                                    else
                                    {
                                        #region Board Bases >0
                                        for (int j = 0; j < bb; j++)
                                        {
                                            int countpaidnight1 = 0;
                                            Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight1 = countpaidnight1 + 1;
                                                }
                                            });
                                            int countpaidnight2 = 0;
                                            Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight2 = countpaidnight2 + 1;
                                                }
                                            });
                                            int countpaidnight3 = 0;
                                            Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight3 = countpaidnight3 + 1;
                                                }
                                            });
                                            int countpaidnight4 = 0;
                                            Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList4[p].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight4 = countpaidnight4 + 1;
                                                }
                                            });
                                            int countpaidnight5 = 0;
                                            Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList5[q].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight5 = countpaidnight5 + 1;
                                                }
                                            });
                                            group++;
                                            decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                            decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                            decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                            decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                            decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5;

                                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "1"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "2"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "3"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3 / countpaidnight3)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "4"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt4 / countpaidnight4)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                             new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "5"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt5 / countpaidnight5)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                             new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                             )));
                                        }
                                        #endregion
                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion
            #region Room Count 6
            if (totalroom == 6)
            {

                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 6
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        // add room 1, 2, 3, 4, 5, 6

                                        #region room's group

                                        int bb = roomlist.Occupancies[m].BoardBases.Count();
                                        List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                        List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                        string promotion = string.Empty;
                                        if (roomlist.Discount != null)
                                        {
                                            #region Discount (Tourico) Amount already subtracted
                                            try
                                            {
                                                XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                XmlDocument doc3 = new XmlDocument();
                                                System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                XmlWriter writer3 = XmlWriter.Create(sww3);
                                                xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                var typxsd = XDocument.Parse(sww3.ToString());
                                                var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                var distype = typxsd.Root.Attribute(disprefix + "type");
                                                if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                {
                                                    promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                }
                                                else
                                                {
                                                    promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                }
                                            }
                                            catch (Exception ex)
                                            {

                                            }
                                            #endregion
                                        }

                                        if (bb == 0)
                                        {
                                            group++;
                                            decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice;

                                            #region No Board Bases
                                            str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "1"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "2"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "3"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                 new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "4"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "5"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                 new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "6"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                 )));
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Board Bases >0
                                            for (int j = 0; j < bb; j++)
                                            {
                                                group++;
                                                decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6;

                                                str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "1"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "2"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "3"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "4"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "5"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "6"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                 )));
                                            }
                                            #endregion
                                        }
                                        #endregion

                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion
            #region Room Count 7
            if (totalroom == 7)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 7)
                        {
                            roomList7.Add(roomlist.Occupancies[i]);
                        }
                    });

                });


                #region Room 7
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            // add room 1, 2, 3, 4, 5, 6, 7

                                            #region room's group

                                            int bb = roomlist.Occupancies[m].BoardBases.Count();
                                            List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                            List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                            string promotion = string.Empty;
                                            if (roomlist.Discount != null)
                                            {
                                                #region Discount (Tourico) Amount already subtracted
                                                try
                                                {
                                                    XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                    XmlDocument doc3 = new XmlDocument();
                                                    System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                    XmlWriter writer3 = XmlWriter.Create(sww3);
                                                    xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                    var typxsd = XDocument.Parse(sww3.ToString());
                                                    var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                    var distype = typxsd.Root.Attribute(disprefix + "type");
                                                    if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                    {
                                                        promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                    }
                                                    else
                                                    {
                                                        promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                    }
                                                }
                                                catch (Exception ex)
                                                {

                                                }
                                                #endregion
                                            }

                                            if (bb == 0)
                                            {
                                                group++;
                                                decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice;

                                                #region No Board Bases
                                                str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "1"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "2"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "3"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "4"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "5"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "6"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "7"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                     )));
                                                #endregion
                                            }
                                            else
                                            {
                                                #region Board Bases >0
                                                for (int j = 0; j < bb; j++)
                                                {
                                                    group++;
                                                    decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt7 = roomList7[s].occupPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice;
                                                    decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7;

                                                    str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "1"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "2"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "3"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "4"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "5"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "6"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "7"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList7[s].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList7[s].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt7)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                     )));
                                                }
                                                #endregion
                                            }
                                            #endregion
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion
            #region Room Count 8
            if (totalroom == 8)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 7)
                        {
                            roomList7.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 8)
                        {
                            roomList8.Add(roomlist.Occupancies[i]);
                        }
                    });

                });


                #region Room 8
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            for (int t = 0; t < roomList8.Count(); t++)
                                            {
                                                // add room 1, 2, 3, 4, 5, 6, 7, 8

                                                #region room's group

                                                int bb = roomlist.Occupancies[m].BoardBases.Count();
                                                List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                                List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                                string promotion = string.Empty;
                                                if (roomlist.Discount != null)
                                                {
                                                    #region Discount (Tourico) Amount already subtracted
                                                    try
                                                    {
                                                        XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                        XmlDocument doc3 = new XmlDocument();
                                                        System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                        XmlWriter writer3 = XmlWriter.Create(sww3);
                                                        xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                        var typxsd = XDocument.Parse(sww3.ToString());
                                                        var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                        var distype = typxsd.Root.Attribute(disprefix + "type");
                                                        if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                        {
                                                            promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                        }
                                                        else
                                                        {
                                                            promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {

                                                    }
                                                    #endregion
                                                }

                                                if (bb == 0)
                                                {
                                                    group++;
                                                    decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice + roomList8[t].occupPublishPrice;

                                                    #region No Board Bases
                                                    str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "1"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "2"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "3"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "4"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "5"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "6"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "7"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "8"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList8[t].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                         )));
                                                    #endregion
                                                }
                                                else
                                                {
                                                    #region Board Bases >0
                                                    for (int j = 0; j < bb; j++)
                                                    {
                                                        group++;
                                                        decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt7 = roomList7[s].occupPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt8 = roomList8[t].occupPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice;
                                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8;

                                                        str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "1"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "2"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "3"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "4"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "5"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "6"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "7"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList7[s].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList7[s].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt7)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "8"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList8[t].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList8[t].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList8[t].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt8)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), roomList8[t].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                         )));
                                                    }
                                                    #endregion
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion
            #region Room Count 9
            if (totalroom == 9)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 7)
                        {
                            roomList7.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 8)
                        {
                            roomList8.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 9)
                        {
                            roomList9.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 9
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            for (int t = 0; t < roomList8.Count(); t++)
                                            {
                                                for (int u = 0; u < roomList8.Count(); u++)
                                                {
                                                    // add room 1, 2, 3, 4, 5, 6, 7, 8, 9

                                                    #region room's group

                                                    int bb = roomlist.Occupancies[m].BoardBases.Count();
                                                    List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                                    List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                                    string promotion = string.Empty;
                                                    if (roomlist.Discount != null)
                                                    {
                                                        #region Discount (Tourico) Amount already subtracted
                                                        try
                                                        {
                                                            XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                            XmlDocument doc3 = new XmlDocument();
                                                            System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                            XmlWriter writer3 = XmlWriter.Create(sww3);
                                                            xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                            var typxsd = XDocument.Parse(sww3.ToString());
                                                            var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                            var distype = typxsd.Root.Attribute(disprefix + "type");
                                                            if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                            {
                                                                promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                            }
                                                            else
                                                            {
                                                                promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {

                                                        }
                                                        #endregion
                                                    }

                                                    if (bb == 0)
                                                    {
                                                        group++;
                                                        decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice + roomList8[t].occupPublishPrice + roomList9[u].occupPublishPrice;

                                                        #region No Board Bases
                                                        str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "1"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                             new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "2"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "3"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "4"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "5"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "6"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "7"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "8"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList8[t].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "9"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList9[u].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList9[u].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList9[u].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList9[u].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList9[u].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList9[u].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList9[u].Rooms[0].ChildNum))
                                                             )));
                                                        #endregion
                                                    }
                                                    else
                                                    {
                                                        #region Board Bases >0
                                                        for (int j = 0; j < bb; j++)
                                                        {
                                                            group++;
                                                            decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt7 = roomList7[s].occupPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt8 = roomList8[t].occupPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt9 = roomList9[u].occupPublishPrice + roomList9[u].BoardBases[j].bbPublishPrice;
                                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8 + totalamt9;

                                                            str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "1"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "2"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "3"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "4"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "5"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "6"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "7"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList7[s].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList7[s].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt7)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "8"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList8[t].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList8[t].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList8[t].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt8)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), roomList8[t].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "9"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList9[u].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList9[u].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList9[u].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList9[u].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList9[u].avrNightPublishPrice + roomList9[u].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt9)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList9[u].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList9[u].PriceBreakdown.ToList(), roomList9[u].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList9[u].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList9[u].Rooms[0].ChildNum))
                                                             )));
                                                        }
                                                        #endregion
                                                    }
                                                    #endregion
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion

            return str;
        }
        #endregion
        #region Tourico Hotel's Room Listing
        public IEnumerable<XElement> GetHotelRoomsTouricoMultiBoard(Tourico.RoomType roomlist, int j)
        {
            List<XElement> str = new List<XElement>();
            Parallel.For(0, roomlist.Occupancies.Count(), i =>
            {
                int roomcount = roomlist.Occupancies[0].Rooms.Count();
                //int bb = roomlist.Occupancies[i].BoardBases.Count();
                //int boardbaseid = roomlist.Occupancies[i].BoardBases[i].bbId;
                List<Tourico.Supplement> supplements = roomlist.Occupancies[i].SelctedSupplements.ToList();
                List<Tourico.Price> pricebrkups = roomlist.Occupancies[i].PriceBreakdown.ToList();
                string promotion = string.Empty;
                if (roomlist.Discount != null)
                {
                    #region Discount (Tourico) Amount already subtracted
                    try
                    {
                        XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                        XmlDocument doc3 = new XmlDocument();
                        System.IO.StringWriter sww3 = new System.IO.StringWriter();
                        XmlWriter writer3 = XmlWriter.Create(sww3);
                        xsSubmit3.Serialize(writer3, roomlist.Discount);
                        var typxsd = XDocument.Parse(sww3.ToString());
                        var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                        var distype = typxsd.Root.Attribute(disprefix + "type");
                        if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                        {
                            promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                        }
                        else
                        {
                            promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    #endregion
                }
                Parallel.For(0, roomcount, p =>
                {
                    #region Board Bases >0
                    //Parallel.For(0, bb, j =>
                    //{
                    decimal bbprice = roomlist.Occupancies[i].BoardBases[j].bbPublishPrice;
                    int bbid = roomlist.Occupancies[i].BoardBases[j].bbId;
                    string bbname = roomlist.Occupancies[i].BoardBases[j].bbName;

                    decimal totalamt = roomlist.Occupancies[i].occupPublishPrice + bbprice;
                    //if (bbid == boardbaseid)
                    {
                        str.Add(new XElement("Room",
                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                         new XAttribute("SuppliersID", "2"),
                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                         new XAttribute("OccupancyID", Convert.ToString(roomlist.Occupancies[i].bedding)),
                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                         new XAttribute("MealPlanID", Convert.ToString(bbid)),
                         new XAttribute("MealPlanName", Convert.ToString(bbname)),
                         new XAttribute("MealPlanCode", ""),
                         new XAttribute("MealPlanPrice", Convert.ToString(bbprice)),
                         new XAttribute("PerNightRoomRate", Convert.ToString(roomlist.Occupancies[i].avrNightPublishPrice)),
                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                         new XAttribute("CancellationDate", ""),
                         new XAttribute("CancellationAmount", ""),
                          new XAttribute("isAvailable", roomlist.isAvailable),
                         new XElement("RequestID", Convert.ToString("")),
                         new XElement("Offers", ""),
                         new XElement("Promotions", Convert.ToString(promotion)),
                         new XElement("CancellationPolicy", ""),
                         new XElement("Amenities", new XElement("Amenity", "")),
                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                         new XElement("Supplements",
                             GetRoomsSupplementTourico(supplements)
                             ),
                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(pricebrkups, 0)),
                             new XElement("AdultNum", Convert.ToString(roomlist.Occupancies[i].Rooms[0].AdultNum)),
                             new XElement("ChildNum", Convert.ToString(roomlist.Occupancies[i].Rooms[0].ChildNum))
                         ));
                    }
                    //});
                    #endregion
                });
            });
            return str;
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
                        new XAttribute("suppPrice", Convert.ToString(supplements[i].publishPrice)),
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
                        new XAttribute("suppPrice", Convert.ToString(supplements[i].publishPrice)),
                        new XAttribute("suppType", Convert.ToString(type.Value)))
                     );
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
        #region Tourico Room's Price Breakups
        private IEnumerable<XElement> GetRoomsPriceBreakupTourico(List<Tourico.Price> pricebreakups, decimal mealprice)
        {
            #region Tourico Room's Price Breakups
            List<XElement> str = new List<XElement>();
            int countpaidnight = 0;
            Parallel.For(0, pricebreakups.Count(), j =>
            {
                if (pricebreakups[j].valuePublish != 0)
                {
                    countpaidnight = countpaidnight + 1;
                }
            });
            decimal mealpricepernight = mealprice / countpaidnight;

            Parallel.For(0, pricebreakups.Count(), i =>
            {
                if (pricebreakups[i].valuePublish != 0)
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                           new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].valuePublish + mealpricepernight)))
                    );
                }
                else
                {
                    str.Add(new XElement("Price",
                          new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                          new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].valuePublish)))
                   );
                }
            });
            return str;
            #endregion
        }
        #endregion
        #endregion
        #region Methods for Darina Holidays
        public string CallWebService(XElement req, string url, string action, string flag)
        {
            try
            {
                var _url = url;
                var _action = action;
                XDocument soapEnvelopeXml = new XDocument();
                if (flag == "htlist")
                {
                    WriteToFile("Availability Request start");
                    soapEnvelopeXml = CreateSoapEnvelopehtlist(req);
                    WriteToFile(soapEnvelopeXml.ToString());
                    WriteToFile("Availability Request end");
                }
                if (flag == "htdetail")
                {
                    soapEnvelopeXml = CreateSoapEnvelopehtdetail(req);
                }
                HttpWebRequest webRequest = CreateWebRequest(_url, _action);
                InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                asyncResult.AsyncWaitHandle.WaitOne();
                string soapResult;
                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                {
                    using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                    return soapResult;
                }
            }
            catch (WebException webex)
            {
                WebResponse errResp = webex.Response;
                using (Stream respStream = errResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream);
                    string text = reader.ReadToEnd();
                    return text;
                }
            }
        }
        private static HttpWebRequest CreateWebRequest(string url, string action)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }
        private static XDocument CreateSoapEnvelopehtlist(XElement req)
        {
            int totalchild = Convert.ToInt32(req.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value);
            string mainchildrenages = "";
            string childrenages = "";
            #region City ID / Country ID / Pax Nationality Country ID
            string cityid = "";
            string countryid = "";
            string paxcountryid = "";
            XElement doccity = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\cities.xml"));
            XElement docccountry = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\country.xml"));
            IEnumerable<XElement> doccityd = doccity.Descendants("d0").Where(x => x.Descendants("LetterCode").Single().Value == req.Descendants("CityCode").Single().Value);
            countryid = doccityd.Descendants("CountryID").SingleOrDefault().Value;
            cityid = doccityd.Descendants("Serial").SingleOrDefault().Value;
            IEnumerable<XElement> docccountryd = docccountry.Descendants("d0").Where(x => x.Descendants("Code").Single().Value == req.Descendants("PaxNationality_CountryCode").Single().Value);
            paxcountryid = docccountryd.Descendants("CountryID").SingleOrDefault().Value;
            #endregion
            if (totalchild > 0)
            {
                List<XElement> childage = req.Descendants("RoomPax").Descendants("ChildAge").ToList();
                for (int i = 0; i < childage.Count(); i++)
                {
                    childrenages = childrenages + "<int>" + Convert.ToString(childage[i].Value) + "</int>";
                }
                mainchildrenages = "<ChildrenAges>" + childrenages + "</ChildrenAges>";
            }
            else
            {
                mainchildrenages = "<ChildrenAges />";
            }
            #region Credentials
            string AccountName = string.Empty;
            string UserName = string.Empty;
            string Password = string.Empty;
            string AgentID = string.Empty;
            string Secret = string.Empty;
            DarinaCredentials _credential = new DarinaCredentials();
            AccountName = _credential.AccountName;
            UserName = _credential.UserName;
            Password = _credential.Password;
            AgentID = _credential.AgentID;
            Secret = _credential.Secret;
            #endregion
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                         "<soap:Body>" +
                           "<CheckAvailability xmlns='http://travelcontrol.softexsw.us/'>" +
                              "<SecStr>" + Secret + "</SecStr>" +
                             "<AccountName>" + AccountName + "</AccountName>" +
                             "<UserName>" + UserName + "</UserName>" +
                             "<Password>" + Password + "</Password>" +
                             "<AgentID>" + AgentID + "</AgentID>" +
                             "<FromDate>" + req.Descendants("FromDate").Single().Value + "</FromDate>" +
                             "<ToDate>" + req.Descendants("ToDate").Single().Value + "</ToDate>" +
                             "<CountryID>" + countryid + "</CountryID>" +
                             "<CityID>" + cityid + "</CityID>" + // 50
                             "<AreaID>0</AreaID>" +
                             "<AreaName></AreaName>" +
                             "<MinLandCategory>" + req.Descendants("MinStarRating").Single().Value + "</MinLandCategory>" +
                             "<MaxLandCategory>" + req.Descendants("MaxStarRating").Single().Value + "</MaxLandCategory>" +
                             "<PropertyType>0</PropertyType>" +
                             "<HotelName></HotelName>" +
                             "<OccupancyID>0</OccupancyID>" +
                             "<AdultPax>" + req.Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value + "</AdultPax>" +
                             "<ChildPax>" + req.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value + "</ChildPax>" +
                                    mainchildrenages +
                             "<ExtraBedAdult>false</ExtraBedAdult>" +
                             "<ExtraBedChild>false</ExtraBedChild>" +
                             "<Nationality_CountryID>" + paxcountryid + "</Nationality_CountryID>" +
                             "<CurrencyID>" + req.Descendants("CurrencyID").Single().Value + "</CurrencyID>" +
                             "<MaxOverallPrice>0</MaxOverallPrice>" +
                             "<Availability>ShowAvailableOnly</Availability>" +
                             "<RoomType>0</RoomType>" +
                             "<MealPlan>0</MealPlan>" +
                             "<GetHotelImageLink>true</GetHotelImageLink>" +
                             "<GetHotelMapLink>true</GetHotelMapLink>" +
                             "<Source>0</Source>" +
                             "<LimitRoomTypesInResult>0</LimitRoomTypesInResult>" +
                           "</CheckAvailability>" +
                         "</soap:Body>" +
                       "</soap:Envelope>";
            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private static XDocument CreateSoapEnvelopehtdetail(XElement req)
        {
            #region City ID / Country ID / Pax Nationality Country ID
            string cityid = "";
            string countryid = "";
            string paxcountryid = "";
            XElement doccity = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\cities.xml"));
            XElement docccountry = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\country.xml"));
            IEnumerable<XElement> doccityd = doccity.Descendants("d0").Where(x => x.Descendants("LetterCode").Single().Value == req.Descendants("CityCode").Single().Value);
            countryid = doccityd.Descendants("CountryID").SingleOrDefault().Value;
            cityid = doccityd.Descendants("Serial").SingleOrDefault().Value;
            IEnumerable<XElement> docccountryd = docccountry.Descendants("d0").Where(x => x.Descendants("Code").Single().Value == req.Descendants("PaxNationality_CountryCode").Single().Value);
            paxcountryid = docccountryd.Descendants("CountryID").SingleOrDefault().Value;
            #endregion
            #region Credentials
            string AccountName = string.Empty;
            string UserName = string.Empty;
            string Password = string.Empty;
            string AgentID = string.Empty;
            string Secret = string.Empty;
            DarinaCredentials _credential = new DarinaCredentials();
            AccountName = _credential.AccountName;
            UserName = _credential.UserName;
            Password = _credential.Password;
            AgentID = _credential.AgentID;
            Secret = _credential.Secret;
            #endregion
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                         "<soap:Body>" +
                           "<GetBasicData_Properties_WithFullDetails xmlns='http://travelcontrol.softexsw.us/'>" +
                              "<SecStr>" + Secret + "</SecStr>" +
                             "<AccountName>" + AccountName + "</AccountName>" +
                             "<UserName>" + UserName + "</UserName>" +
                             "<Password>" + Password + "</Password>" +
                             "<AgentID>" + AgentID + "</AgentID>" +
                             "<CountryID>" + countryid + "</CountryID>" +
                             "<CountryCode></CountryCode>" +
                             "<CountryName></CountryName>" +
                             "<CityID>" + cityid + "</CityID>" +// pass 50 for test
                             "<CityCode></CityCode>" +
                             "<CityName></CityName>" +
                             "<AreaID>0</AreaID>" +
                             "<AreaName></AreaName>" +
                             "<LandCatregory>6</LandCatregory>" +
                             "<HotelName></HotelName>" +
                           "</GetBasicData_Properties_WithFullDetails>" +
                         "</soap:Body>" +
                        "</soap:Envelope>";
            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private static void InsertSoapEnvelopeIntoWebRequest(XDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }
        #endregion
        #region Hotel Extranet
        #region Extranet Hotel Listing
        private IEnumerable<XElement> GetHotelListExtranet(List<XElement> htlist)
        {
            #region Extranet Hotels
            List<XElement> hotellst = new List<XElement>();
            try
            {
                Int32 length = 0;
                if (htlist != null)
                {
                    length = htlist.Count();
                }
                else
                {
                    length = 0;
                }
                try
                {
                    Parallel.For(0, length, i =>
                    {


                        hotellst.Add(new XElement("Hotel",
                                               new XElement("HotelID", Convert.ToString(htlist[i].Descendants("HotelID").SingleOrDefault().Value)),
                                               new XElement("HotelName", Convert.ToString(htlist[i].Descendants("HotelName").SingleOrDefault().Value)),
                                               new XElement("PropertyTypeName", Convert.ToString(htlist[i].Descendants("PropertyTypeName").SingleOrDefault().Value)),
                                               new XElement("CountryID", Convert.ToString(htlist[i].Descendants("CountryID").SingleOrDefault().Value)),
                                               new XElement("CountryName", Convert.ToString(htlist[i].Descendants("CountryName").SingleOrDefault().Value)),
                                               new XElement("CountryCode", Convert.ToString(htlist[i].Descendants("CountryCode").SingleOrDefault().Value)),
                                               new XElement("CityId", Convert.ToString(htlist[i].Descendants("CityId").SingleOrDefault().Value)),
                                               new XElement("CityCode", Convert.ToString(htlist[i].Descendants("CityCode").SingleOrDefault().Value)),
                                               new XElement("CityName", Convert.ToString(htlist[i].Descendants("CityName").SingleOrDefault().Value)),
                                               new XElement("AreaId", Convert.ToString(htlist[i].Descendants("AreaId").SingleOrDefault().Value)),
                                               new XElement("AreaName", Convert.ToString(htlist[i].Descendants("AreaName").SingleOrDefault().Value))
                                               , new XElement("Address", Convert.ToString(htlist[i].Descendants("Address").SingleOrDefault().Value)),
                                               new XElement("Location", Convert.ToString(htlist[i].Descendants("Location").SingleOrDefault().Value)),
                                               new XElement("Description", Convert.ToString(htlist[i].Descendants("Description").SingleOrDefault().Value)),
                                               new XElement("StarRating", Convert.ToString(htlist[i].Descendants("StarRating").SingleOrDefault().Value)),
                                               new XElement("MinRate", Convert.ToString(htlist[i].Descendants("MinRate").SingleOrDefault().Value))
                                               , new XElement("HotelImgSmall", Convert.ToString(htlist[i].Descendants("HotelImgSmall").SingleOrDefault().Value)),
                                               new XElement("HotelImgLarge", Convert.ToString(htlist[i].Descendants("HotelImgLarge").SingleOrDefault().Value)),
                                               new XElement("MapLink", ""),
                                               new XElement("Longitude", Convert.ToString(htlist[i].Descendants("Langtitude").SingleOrDefault().Value)),
                                               new XElement("Latitude", Convert.ToString(htlist[i].Descendants("Latitude").SingleOrDefault().Value)),
                                               new XElement("DMC", "Extranet"),
                                               new XElement("SupplierID", "3"),
                                               new XElement("Currency", Convert.ToString(htlist[i].Descendants("Currency").SingleOrDefault().Value)),
                                               new XElement("Offers", "")
                                               , new XElement("Facilities",
                                                  GetHotelFacilitiesExtranet(htlist[i].Descendants("Facility").ToList()))
                                               , new XElement("Rooms",
                                                    GetHotelRoomgroupExtranet(htlist[i].Descendants("Room").ToList())
                                                   )
                        ));

                    });
                }
                catch (Exception ex)
                {
                    return hotellst;
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
        public IEnumerable<XElement> GetHotelRoomgroupExtranet(List<XElement> roomlist)
        {
            List<XElement> str = new List<XElement>();
            List<XElement> strgrp = new List<XElement>();
            //Parallel.For(0, roomlist.Count(), i =>
            int rindex = 1;
            for (int i = 0; i < roomlist.Count(); i++)
            {
                str = GetHotelRoomListingExtranet(roomlist[i]).ToList();

                for (int j = 0; j < str.Count(); j++)
                {

                    str[j].Add(new XAttribute("Index", rindex));
                    strgrp.Add(str[j]);
                    rindex++;
                }
            };
            return strgrp;
        }
        #endregion
        #region Extranet Hotel's Room Listing
        public IEnumerable<XElement> GetHotelRoomListingExtranet(XElement roomlist)
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
                     new XAttribute("MealPlanPrice", Convert.ToString("0")),
                     new XAttribute("PerNightRoomRate", Convert.ToString(bb[j].Descendants("PricePerNight").SingleOrDefault().Value)),
                     new XAttribute("TotalRoomRate", Convert.ToString(totalrate)),
                     new XAttribute("CancellationDate", ""),
                     new XAttribute("CancellationAmount", ""),
                      new XAttribute("isAvailable", Convert.ToString(roomlist.Descendants("AvailableAllRoom").SingleOrDefault().Value)),
                     new XElement("RequestID", Convert.ToString("")),
                     new XElement("Offers", ""),
                     new XElement("PromotionList",
                         GetHotelpromotionsExtranet(bb[j].Descendants("pro").ToList())),
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

                strgrp.Add(new XElement("RoomTypes", new XAttribute("TotalRate", Convert.ToString(bb[j].Descendants("TotalRoomRate").SingleOrDefault().Value)),
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
        #region Extranet's Hotel Facilities
        private IEnumerable<XElement> GetHotelFacilitiesExtranet(List<XElement> hotelfacilities)
        {

            Int32 length = hotelfacilities.Count();
            List<XElement> Facilities = new List<XElement>();

            //Facilities.Add(new XElement("Facility", "No Facility Available"));

            if (length == 0)
            {
                Facilities.Add(new XElement("Facility", "No Facility Available"));
            }
            else
            {

                Parallel.For(0, length, i =>
                {

                    Facilities.Add(new XElement("Facility", Convert.ToString(hotelfacilities[i].Value)));

                });
            }
            return Facilities;
        }
        #endregion
        #endregion
        #region Test
        public HttpWebRequest CreateWebRequestTourico()
        {

            string _url = "http://demo-hotelws.touricoholidays.com/HotelFlow.svc/bas";
            string _action = "http://tourico.com/webservices/hotelv3/IHotelFlow/SearchHotels";
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_url);
            webRequest.Headers.Add(@"SOAPAction:" + _action + "");
            webRequest.ContentType = "text/xml;charset=utf-8";
            webRequest.Method = "POST";
            webRequest.Host = "demo-hotelws.touricoholidays.com";
            return webRequest;
        }

        XElement GetHotelInfoTourico(XElement requesttourico)
        {
            XElement loc;
            HttpWebRequest request = CreateWebRequestTourico();
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(@"<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:aut='http://schemas.tourico.com/webservices/authentication' xmlns:hot='http://tourico.com/webservices/hotelv3'>
                                        <soapenv:Header>
                                              <aut:AuthenticationHeader>
                                                 <aut:LoginName>HOL916</aut:LoginName>
                                                 <aut:Password>111111</aut:Password>
                                                 <aut:Culture>en_US</aut:Culture> 
                                                 <aut:Version></aut:Version>
                                              </aut:AuthenticationHeader>
                                           </soapenv:Header>
                                          <soapenv:Body>
                                              <hot:SearchHotels>
                                                 <hot:request>
                                                    <hot1:Destination>NYC</hot1:Destination>
                                                    <hot1:HotelCityName>New York</hot1:HotelCityName>
                                                    <hot1:HotelLocationName></hot1:HotelLocationName>
                                                    <hot1:HotelName></hot1:HotelName>
                                                    <hot1:CheckIn>2017-06-01</hot1:CheckIn>
                                                    <hot1:CheckOut>2017-06-05</hot1:CheckOut>
                                                    <hot1:RoomsInformation>
                                                     <hot1:RoomInfo>
                                                          <hot1:AdultNum>1</hot1:AdultNum>
                                                          <hot1:ChildNum>0</hot1:ChildNum>
                                                          <hot1:ChildAges>
                                                             <hot1:ChildAge age='0'/>
                                                          </hot1:ChildAges>
                                                       </hot1:RoomInfo>
                                                    </hot1:RoomsInformation>
                                                    <hot1:MaxPrice>0</hot1:MaxPrice>
                                                    <hot1:StarLevel>0</hot1:StarLevel>
                                                    <hot1:AvailableOnly>true</hot1:AvailableOnly>
                                                    <hot1:PropertyType>NotSet</hot1:PropertyType>
                                                    <hot1:ExactDestination>true</hot1:ExactDestination>
                                                 </hot:request>
                                                 <hot:features>
                                                    <hot:Feature name="" value=""/>
                                                 </hot:features>
                                              </hot:SearchHotels>
                                             </soapenv:Body></soapenv:Envelope>"
                );

            using (Stream stream = request.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    string soapResult = rd.ReadToEnd();
                    XDocument xmlData = XDocument.Parse(soapResult);
                    //xmlData.RemoveXmlns();
                    loc = xmlData.Descendants("HotelList").FirstOrDefault();

                }
            }

            return loc;
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