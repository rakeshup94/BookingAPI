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
using TravillioXMLOutService.Common.DotW;
using TravillioXMLOutService.Supplier.JacTravel;
using TravillioXMLOutService.Supplier.Extranet;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Supplier.TouricoHolidays;
using TravillioXMLOutService.Supplier.GIATA;
using TravillioXMLOutService.Models.Darina;
using TravillioXMLOutService.Models.HotelsPro;
using TravillioXMLOutService.Models.Tourico;
using TravillioXMLOutService.Supplier.Darina;
using TravillioXMLOutService.Models.Common;
using TravillioXMLOutService.Models.Travco;
using TravillioXMLOutService.Models.RTS;
using TravillioXMLOutService.Supplier.RTS;
using TravillioXMLOutService.Supplier.Juniper;
using TravillioXMLOutService.Models.Juniper;
using TravillioXMLOutService.Supplier.Restel;
using TravillioXMLOutService.Models.Restel;
using TravillioXMLOutService.Models.JacTravel;
using TravillioXMLOutService.Supplier.Miki;
using TravillioXMLOutService.Supplier.Godou;
using TravillioXMLOutService.Models.Godou;
using TravillioXMLOutService.Supplier.SalTours;
using TravillioXMLOutService.Supplier.SunHotels;
using TravillioXMLOutService.Supplier.Hoojoozat;
using TravillioXMLOutService.Supplier.TravelGate;
using TravillioXMLOutService.Supplier.TBOHolidays;
using TravillioXMLOutService.Supplier.VOT;
using TravillioXMLOutService.Supplier.EBookingCenter;
using TravillioXMLOutService.Models.EBookingCenter;

namespace TravillioXMLOutService.Supplier.XMLOUT
{
    public class HotelSearch_HA : IDisposable
    {
        int sup_cutime = 100000;
        DotwService dotwObj;
        //GodouServices gds;
        dr_Hotelsearch darinareq;
        Tr_HotelSearch touricoreq;
        ExtHotelSearch extreq;
        HotelBeds hbedreq;
        HotelsProHotelSearch htlproreq;
        MikiInternal miki;
        SalServices sal;
        TGServices TGS;
        string touricouserlogin = string.Empty;
        string touricopassword = string.Empty;
        string touricoversion = string.Empty;
        XElement statictouricohotellist;
        string availDarina = string.Empty;
        string hoteldetailDarina = string.Empty;
        XElement reqTravillio;
        List<Tourico.Hotel> hotelavailabilityresult;
        List<XElement> hotelavailabilitylistextranet;
        List<XElement> jactravelslist = null;
        List<XElement> totalstaylist = null;
        XElement HProfac = null;
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
        public void WriteToFileResponseTimeHotel(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {


                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    //writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    //writer.WriteLine("---------------------------Time Calculate-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        #region Hotel Availability (XML OUT for Travayoo)
        public XElement HotelAvail_HA(XElement req, string dmc)
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
                    int darina = req.Descendants("SupplierID").Where(x => x.Value == "1" && x.Attribute("xmlout").Value=="true").Count();
                    int tourico = req.Descendants("SupplierID").Where(x => x.Value == "2" && x.Attribute("xmlout").Value == "true").Count();
                    int extranet = req.Descendants("SupplierID").Where(x => x.Value == "3" && x.Attribute("xmlout").Value == "true").Count();
                    int hotelbeds = req.Descendants("SupplierID").Where(x => x.Value == "4" && x.Attribute("xmlout").Value == "true").Count();
                    int DOTW = req.Descendants("SupplierID").Where(x => x.Value == "5" && x.Attribute("xmlout").Value == "true").Count();
                    int hotelspro = req.Descendants("SupplierID").Where(x => x.Value == "6" && x.Attribute("xmlout").Value == "true").Count();
                    int travco = req.Descendants("SupplierID").Where(x => x.Value == "7" && x.Attribute("xmlout").Value == "true").Count();
                    int JacTravel = req.Descendants("SupplierID").Where(x => x.Value == "8" && x.Attribute("xmlout").Value == "true").Count();
                    int RTS = req.Descendants("SupplierID").Where(x => x.Value == "9" && x.Attribute("xmlout").Value == "true").Count();
                    int Miki = req.Descendants("SupplierID").Where(x => x.Value == "11" && x.Attribute("xmlout").Value == "true").Count();
                    int restel = req.Descendants("SupplierID").Where(x => x.Value == "13" && x.Attribute("xmlout").Value == "true").Count();
                    int JuniperW2M = req.Descendants("SupplierID").Where(x => x.Value == "16" && x.Attribute("xmlout").Value == "true").Count();
                    int EgyptExpress = req.Descendants("SupplierID").Where(x => x.Value == "17" && x.Attribute("xmlout").Value == "true").Count();
                    int SalTour = req.Descendants("SupplierID").Where(x => x.Value == "19" && x.Attribute("xmlout").Value == "true").Count();
                    int tbo = req.Descendants("SupplierID").Where(x => x.Value == "21" && x.Attribute("xmlout").Value == "true").Count();
                    int LOH = req.Descendants("SupplierID").Where(x => x.Value == "23" && x.Attribute("xmlout").Value == "true").Count();
                    int Gadou = req.Descendants("SupplierID").Where(x => x.Value == "31" && x.Attribute("xmlout").Value == "true").Count();
                    int LCI = req.Descendants("SupplierID").Where(x => x.Value == "35" && x.Attribute("xmlout").Value == "true").Count();
                    int SunHotels = req.Descendants("SupplierID").Where(x => x.Value == "36" && x.Attribute("xmlout").Value == "true").Count();
                    int totalstay = req.Descendants("SupplierID").Where(x => x.Value == "37" && x.Attribute("xmlout").Value == "true").Count();
                    int SmyRooms = req.Descendants("SupplierID").Where(x => x.Value == "39" && x.Attribute("xmlout").Value == "true").Count();
                    int AlphaTours = req.Descendants("SupplierID").Where(x => x.Value == "41" && x.Attribute("xmlout").Value == "true").Count();
                    int Hoojoozat = req.Descendants("SupplierID").Where(x => x.Value == "45" && x.Attribute("xmlout").Value == "true").Count();
                    int vot = req.Descendants("SupplierID").Where(x => x.Value == "46" && x.Attribute("xmlout").Value == "true").Count();
                    int ebookingcenter = req.Descendants("SupplierID").Where(x => x.Value == "47" && x.Attribute("xmlout").Value == "true").Count();
                    if (darina > 0 || tourico > 0 || extranet > 0 || hotelbeds > 0 || DOTW > 0 || hotelspro > 0 || travco > 0 || JacTravel > 0 || RTS > 0 || Miki > 0 || restel > 0 || JuniperW2M > 0 || EgyptExpress > 0 || SalTour > 0 || tbo > 0 || LOH > 0 || Gadou > 0 || LCI > 0 || SunHotels > 0 || totalstay > 0 || SmyRooms > 0 || AlphaTours > 0 || Hoojoozat > 0 || vot > 0 || ebookingcenter > 0)
                    {
                        #region get cut off time
                        try
                        {
                            sup_cutime = supplier_Cred.cutoff_time();
                        }
                        catch { }
                        #endregion
                        IEnumerable<XElement> darinahotellist = null;
                        List<XElement> responsetourico = null;
                        List<XElement> responsehotelspro = null;
                        List<XElement> hotelbedslist = null;
                        List<XElement> dotwlist = null;
                        List<XElement> travcolist = null;
                        List<XElement> RTSlst = null;
                        List<XElement> Mikilst = null;
                        List<XElement> Restellst = null;
                        List<XElement> JuniparW2Mlst = null;
                        List<XElement> egyptexpresslst = null;
                        List<XElement> Sallst = null;
                        List<XElement> TBOlst = null;
                        List<XElement> JuniparLOHlst = null;
                        List<XElement> JuniparLCIlst = null;
                        List<XElement> alphatourslst = null;
                        List<XElement> Smylst = null;
                        List<XElement> sunhotelslst = null;
                        List<XElement> hoojhotelslst = null;
                        List<XElement> vothotelslst = null;
                        List<XElement> ebookingcenterhotelslst = null;
                        #region Bind Static Data
                        XElement doccurrency = null;
                        XElement docmealplan = null;
                        XElement dococcupancy = null;
                        XElement staticdatahb = null;
                        XElement travco_htlstatic = null;
                        XElement travco_htlstar = null;
                        XElement travcocitymapping = null;
                        string rtspath = string.Empty;
                        XElement gadouCurrencies = null;
                        List<XElement> Gadoulst = null;
                        XElement ebookingcenterdocnationality = null;
                        if (darina == 1)
                        {
                            doccurrency = dr_staticdata.drn_doccurrency();
                            docmealplan = dr_staticdata.drn_docmealplan();
                            dococcupancy = dr_staticdata.drn_dococcupancy();
                            darinareq = new dr_Hotelsearch();
                        }
                        if (tourico == 1)
                        {
                            statictouricohotellist = trc_statichtl.tourico_htlstatic();
                            touricoreq = new Tr_HotelSearch();
                        }
                        if (extranet == 1)
                        {
                            extreq = new ExtHotelSearch();
                        }
                        if (hotelbeds == 1)
                        {
                            hbedreq = new HotelBeds();
                        }
                        if (DOTW == 1)
                        {
                            string custID = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "5" && x.Attribute("xmlout").Value == "false").FirstOrDefault().Attribute("custID").Value;
                            }
                            catch { }
                            dotwObj = new DotwService(custID);
                        }
                        if (hotelspro == 1)
                        {
                            htlproreq = new HotelsProHotelSearch();
                        }
                        if (travco == 1)
                        {
                            travco_htlstar = travco_static.travco_starcat();
                        }
                        if (JacTravel == 1 || totalstay > 0)
                        {
                            //jaccitymapping = jac_staticdata.jac_citymapping();
                        }
                        if (RTS == 1)
                        {
                            //rtspath = RTS_citymap.rts_citylist();
                        }
                        if (Miki == 1)
                        {
                            miki = new MikiInternal();
                        }
                        if (restel == 1)
                        {
                            //restelcitymapping = restel_citymapping.restel_city();
                        }
                        if (Gadou == 1)
                        {
                            //gds = new GodouServices();
                            //gadouCurrencies = Gadou_Currency.Gadaou_Currencies();
                        }
                        if (ebookingcenter == 1)
                        {
                            ebookingcenterdocnationality = EBookingStatic.ebook_nationality();
                        }
                        #endregion
                        #region Thread Initialize 
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
                        if (darina == 1)
                        {
                            if (Convert.ToInt32(req.Descendants("RoomPax").Count()) == 1)
                            {
                                string custID = string.Empty;
                                string custName = string.Empty;
                                try
                                {
                                    custID = req.Descendants("SupplierID").Where(x => x.Value == "1" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                    custName = req.Descendants("SupplierID").Where(x => x.Value == "1" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                                }
                                catch { custName = "HA"; }
                                tid1 = new Thread(new ThreadStart(() => { darinahotellist = darinareq.darinaavailcombined(req, doccurrency, docmealplan, dococcupancy, custID, custName); }));
                            }
                        }
                        if (tourico == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            tid2 = new Thread(new ThreadStart(() => { responsetourico = touricoreq.HotelSearch_TouricoHAOUT(reqTravillio, statictouricohotellist); }));
                        }
                        if (extranet == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "3" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "3" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            tid3 = new Thread(new ThreadStart(() => { hotelavailabilitylistextranet = extreq.CheckHtlAvailabilityExtranet(reqTravillio, custID, custName); }));
                        }
                        if (hotelbeds == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "4" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "4" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            tid4 = new Thread(new ThreadStart(() => { hotelbedslist = hbedreq.CheckAvailabilityHotelBedsMergeThreadFinal(reqTravillio, staticdatahb, custID, custName); }));
                        }
                        if (DOTW == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 6)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "5" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "5" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            tid5 = new Thread(new ThreadStart(() => { dotwlist = dotwObj.HtlSearchReq(reqTravillio,custID, custName); }));
                        }
                        if (hotelspro == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 6)
                        {
                            bool roomPax = true;
                            int totalPax = 0;
                            foreach (XElement room in reqTravillio.Descendants("RoomPax"))
                            {
                                //roomPax = Convert.ToInt32(room.Element("Adult").Value) < 6 ? Convert.ToInt32(room.Element("Child").Value) <= 2 ? true : false : false;
                                totalPax = totalPax + Convert.ToInt32(room.Element("Adult").Value);
                                totalPax = totalPax + Convert.ToInt32(room.Element("Child").Value);
                                if (totalPax > 6)
                                {
                                    roomPax = false;
                                }
                            }
                            if (roomPax)
                            {
                                string custID = string.Empty;
                                string custName = string.Empty;
                                try
                                {
                                    custID = req.Descendants("SupplierID").Where(x => x.Value == "6" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                    custName = req.Descendants("SupplierID").Where(x => x.Value == "6" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                                }
                                catch { custName = "HA"; }
                                tid6 = new Thread(new ThreadStart(() => { responsehotelspro = htlproreq.CheckAvailabilityHotelsProThread(reqTravillio, HProfac, custID, custName); }));
                            }
                            else
                            {
                                hotelspro = 0;
                            }
                        }
                        if (travco == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "7" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "7" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            Travco travobj = new Travco(custID);
                            tid7 = new Thread(new ThreadStart(() => { travcolist = travobj.getHotelAvailbalityMerge(reqTravillio, travco_htlstatic, travco_htlstar, travcocitymapping, custName); }));
                        }
                        if (JacTravel == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
                        {
                            try
                            {
                                int totalPax = 0;
                                foreach (XElement item in reqTravillio.Descendants("RoomPax"))
                                {
                                    totalPax = totalPax + Convert.ToInt32(item.Element("Adult").Value);
                                    totalPax = totalPax + Convert.ToInt32(item.Element("Child").Value);
                                }
                                if (totalPax < 10)
                                {
                                    string custID = string.Empty;
                                    string custName = string.Empty;
                                    try
                                    {
                                        custID = req.Descendants("SupplierID").Where(x => x.Value == "8" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                        custName = req.Descendants("SupplierID").Where(x => x.Value == "8" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                                    }
                                    catch { custName = "HA"; }
                                    JacTravel_Intial obj = new JacTravel_Intial();
                                    obj.MyEvent += obj_MyEvent;
                                    tid8 = new Thread(new ThreadStart(() => { obj.CallHtlSearch(reqTravillio,custID, custName, 8); }));
                                }
                                else { JacTravel = 0; }
                            }
                            catch { JacTravel = 0; }
                        }
                        if (totalstay == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
                        {
                            int totalPax = 0;
                            foreach (XElement item in reqTravillio.Descendants("RoomPax"))
                            {
                                totalPax = totalPax + Convert.ToInt32(item.Element("Adult").Value);
                                totalPax = totalPax + Convert.ToInt32(item.Element("Child").Value);
                            }
                            if (totalPax < 10)
                            {
                                string custID = string.Empty;
                                string custName = string.Empty;
                                try
                                {
                                    custID = req.Descendants("SupplierID").Where(x => x.Value == "37" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                    custName = req.Descendants("SupplierID").Where(x => x.Value == "37" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                                }
                                catch { custName = "HA"; }
                                JacTravel_Intial obj1 = new JacTravel_Intial();
                                obj1.MyEvent += obj_MyEvent1;
                                tid37 = new Thread(new ThreadStart(() => { obj1.CallHtlSearch(reqTravillio, custID, custName, 37); }));
                            }
                            else { totalstay = 0; }
                        }
                        if (RTS == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "9" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "9" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            RTSIntial obj = new RTSIntial();
                            tid9 = new Thread(new ThreadStart(() => { RTSlst = obj.CallSearch(reqTravillio, rtspath, custID, custName); }));
                        }
                        if (Miki == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 6)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "11" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "11" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            tid11 = new Thread(new ThreadStart(() => { Mikilst = miki.HotelSearchRequest(reqTravillio, custID, custName); }));
                        }
                        if (restel == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5 && Convert.ToInt32(req.Descendants("Nights").First().Value) <= 15)
                        {
                            try
                            {
                                RestelServices rs = new RestelServices();
                                #region Condition Check
                                bool condition1 = true;
                                bool condition2 = true;
                                int count = 0;
                                int roomcount = req.Descendants("RoomPax").Count();
                                List<string> PaxAllowed = new List<string>(new string[] { "1-0", "1-1", "1-2", "2-0", "2-1", "2-2", "3-0", "3-1", "4-0", "5-0", "6-0", "7-0", "8-0" });
                                List<string> occupancy = new List<string>();
                                foreach (XElement pax in req.Descendants("RoomPax"))
                                {
                                    string occ = pax.Element("Adult").Value + "-" + pax.Element("Child").Value;
                                    occupancy.Add(occ);
                                    if (PaxAllowed.Contains(occ))
                                        count++;
                                }
                                if (roomcount == count)
                                    condition2 = true;
                                else
                                    condition2 = false;
                                if (req.Descendants("RoomPax").Count() == 4)
                                {
                                    if (occupancy.Distinct().Count() <= 3)
                                        condition1 = true;
                                    else
                                        condition1 = false;
                                }
                                #endregion
                                if (condition1 && condition2)
                                {
                                    string custID = string.Empty;
                                    string custName = string.Empty;
                                    try
                                    {
                                        custID = req.Descendants("SupplierID").Where(x => x.Value == "13" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                        custName = req.Descendants("SupplierID").Where(x => x.Value == "13" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                                    }
                                    catch { custName = "HA"; }
                                    tid13 = new Thread(new ThreadStart(() => { Restellst = rs.ThreadedHotelSearch(reqTravillio, custName, 13, custID, custName); }));
                                }
                                else
                                {
                                    restel = 0;
                                }
                            }
                            catch { restel = 0; }
                        }
                        if (JuniperW2M == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "16" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "16" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            int customerid = Convert.ToInt32(custID);
                            JuniperResponses objRs = new JuniperResponses(16, customerid);
                            tid16 = new Thread(new ThreadStart(() => { JuniparW2Mlst = objRs.ThreadedHotelSearch(reqTravillio, custName); }));
                        }
                        if (EgyptExpress == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "17" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "17" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            int customerid = Convert.ToInt32(custID);
                            JuniperResponses objRs = new JuniperResponses(17, customerid);
                            tid17 = new Thread(new ThreadStart(() => { egyptexpresslst = objRs.ThreadedHotelSearch(reqTravillio, custName); }));
                        }
                        if (SalTour == 1 && req.Descendants("RoomPax").Count() < 10)
                        {
                            sal = new SalServices();
                            tid19 = new Thread(new ThreadStart(() => { Sallst = sal.HotelAvailability(reqTravillio, "HA"); }));
                        }
                        if (tbo == 1 && req.Descendants("RoomPax").Count() < 10)
                        {
                            bool roomPax = true;
                            foreach (XElement room in reqTravillio.Descendants("RoomPax"))
                            {
                                roomPax = Convert.ToInt32(room.Element("Adult").Value) <= 6 ? Convert.ToInt32(room.Element("Child").Value) <= 2 ? true : false : false;
                            }
                            if (roomPax)
                            {
                                string custID = string.Empty;
                                string custName = string.Empty;
                                try
                                {
                                    custID = req.Descendants("SupplierID").Where(x => x.Value == "21" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                    custName = req.Descendants("SupplierID").Where(x => x.Value == "21" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                                }
                                catch { custName = "HA"; }
                                int customerid = Convert.ToInt32(custID);
                                TBOServices tbs = new TBOServices();
                                tid21 = new Thread(new ThreadStart(() => { TBOlst = tbs.HotelSearch(reqTravillio, custID, custName); }));
                            }
                            else
                                tbo = 0;
                        }
                        if (LOH == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "23" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "23" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            int customerid = Convert.ToInt32(custID);
                            JuniperResponses objRs = new JuniperResponses(23, customerid);
                            tid23 = new Thread(new ThreadStart(() => { JuniparLOHlst = objRs.ThreadedHotelSearch(reqTravillio, custName); }));
                        }
                        if (Gadou == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            //tid31 = new Thread(new ThreadStart(() => { Gadoulst = gds.HotelAvailablitySearch(reqTravillio, gadouCurrencies, "HA"); }));
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "31" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "31" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            int customerid = Convert.ToInt32(custID);
                            JuniperResponses objRs = new JuniperResponses(31, customerid);
                            tid31 = new Thread(new ThreadStart(() => { Gadoulst = objRs.ThreadedHotelSearch(reqTravillio, custName); }));
                        }
                        if (LCI == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "35" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "35" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            int customerid = Convert.ToInt32(custID);
                            JuniperResponses objRs = new JuniperResponses(35, customerid);
                            tid35 = new Thread(new ThreadStart(() => { JuniparLCIlst = objRs.ThreadedHotelSearch(reqTravillio, custName); }));
                        }
                        if (SunHotels == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10 && !(req.Descendants("MinStarRating").FirstOrDefault().Value.Equals("0") && req.Descendants("MaxStarRating").FirstOrDefault().Value.Equals("0")))
                        {
                            int paxes = req.Descendants("Adult").Select(x => Convert.ToInt32(x.Value)).Sum();
                            int child = req.Descendants("Child").Select(x => Convert.ToInt32(x.Value)).Sum();
                            if (paxes < 10 && child < 10)
                            {
                                string custID = string.Empty;
                                string custName = string.Empty;
                                try
                                {
                                    custID = req.Descendants("SupplierID").Where(x => x.Value == "36" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                    custName = req.Descendants("SupplierID").Where(x => x.Value == "36" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                                }
                                catch { custName = "HA"; }
                                int customerid = Convert.ToInt32(custID);
                                SunHotelsResponse objRs = new SunHotelsResponse(36, customerid);
                                tid36 = new Thread(new ThreadStart(() => { sunhotelslst = objRs.ThreadedHotelSearch(reqTravillio, custID, custName); }));
                            }
                            else
                            {
                                SunHotels = 0;
                            }
                        }
                        if (SmyRooms == 1 && req.Descendants("RoomPax").Count() < 5)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "39" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "39" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            int customerid = Convert.ToInt32(custID);
                            TGS = new TGServices(39, custID);
                            tid39 = new Thread(new ThreadStart(() => { Smylst = TGS.HotelSearch(req, custName, custID); }));
                        }
                        if (AlphaTours == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "41" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "41" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            int customerid = Convert.ToInt32(custID);
                            JuniperResponses objRs = new JuniperResponses(41, customerid);
                            tid41 = new Thread(new ThreadStart(() => { alphatourslst = objRs.ThreadedHotelSearch(reqTravillio, custName); }));
                        }
                        if (Hoojoozat == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            string customerid = req.Descendants("CustomerID").Single().Value;
                            HoojService objHooj = new HoojService(customerid);
                            tid45 = new Thread(new ThreadStart(() => { hoojhotelslst = objHooj.HotelAvailability(reqTravillio,"HA"); }));
                        }
                        if (vot == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "46" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "46" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { custName = "HA"; }
                            string customerid = req.Descendants("CustomerID").Single().Value;
                            VOTService objVot = new VOTService(customerid);
                            tid46 = new Thread(new ThreadStart(() => { vothotelslst = objVot.VotHotelAvailability(reqTravillio, custID, custName); }));
                        }
                        if (ebookingcenter == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            string custID = string.Empty;
                            string custName = string.Empty;
                            try
                            {
                                custID = req.Descendants("SupplierID").Where(x => x.Value == "47" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custID").Value;
                                custName = req.Descendants("SupplierID").Where(x => x.Value == "47" && x.Attribute("xmlout").Value == "true").FirstOrDefault().Attribute("custName").Value;
                            }
                            catch { }
                            string customerid = custID;
                            EBookingService objebookcntr = new EBookingService(customerid);
                            tid47 = new Thread(new ThreadStart(() => { ebookingcenterhotelslst = objebookcntr.HotelAvailability(reqTravillio, ebookingcenterdocnationality, custID, custName); }));
                        }
                        #endregion
                        List<Tourico.Hotel> hotellisttourico = new List<Tourico.Hotel>();
                        #region Thread Start
                        try
                        {
                            if (darina == 1)
                            {
                                if (Convert.ToInt32(req.Descendants("RoomPax").Count()) == 1)
                                {
                                    tid1.Start();
                                }
                            }
                            if (tourico == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid2.Start();
                            }
                            if (extranet == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
                            {
                                tid3.Start();
                            }
                            if (hotelbeds == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid4.Start();
                            }
                            if (DOTW == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 6)
                            {
                                tid5.Start();
                            }
                            if (hotelspro == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 6)
                            {
                                tid6.Start();
                            }
                            if (travco == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
                            {
                                tid7.Start();
                            }
                            if (JacTravel == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
                            {
                                tid8.Start();
                            }
                            if (RTS == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid9.Start();
                            }
                            if (Miki == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 6)
                            {
                                tid11.Start();
                            }
                            if (restel == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5 && Convert.ToInt32(req.Descendants("Nights").First().Value) <= 15)
                            {
                                tid13.Start();
                            }
                            if (JuniperW2M == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid16.Start();
                            }
                            if (EgyptExpress == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid17.Start();
                            }
                            if (SalTour == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid19.Start();
                            }
                            if (tbo == 1 && req.Descendants("RoomPax").Count() < 10)
                            {
                                tid21.Start();
                            }
                            if (LOH == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid23.Start();
                            }
                            if (Gadou == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid31.Start();
                            }
                            if (LCI == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid35.Start();
                            }
                            if (SunHotels == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10 && !(req.Descendants("MinStarRating").FirstOrDefault().Value.Equals("0") && req.Descendants("MaxStarRating").FirstOrDefault().Value.Equals("0")))
                            {
                                tid36.Start();
                            }
                            if (totalstay == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
                            {
                                tid37.Start();
                            }
                            if (SmyRooms == 1 && req.Descendants("RoomPax").Count() < 5)
                            {
                                tid39.Start();
                            }
                            if (AlphaTours == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid41.Start();
                            }
                            if (Hoojoozat == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid45.Start();
                            }
                            if (vot == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid46.Start();
                            }
                            if (ebookingcenter == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                            {
                                tid47.Start();
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
                        if (darina == 1)
                        {
                            if (Convert.ToInt32(req.Descendants("RoomPax").Count()) == 1)
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
                            }
                        }
                        if (tourico == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        if (extranet == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 7)
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
                        if (hotelbeds == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        if (DOTW == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 6)
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
                        if (hotelspro == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 6)
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
                        if (travco == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
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
                        if (JacTravel == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
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
                        if (RTS == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        if (Miki == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 6)
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
                        if (restel == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5 && Convert.ToInt32(req.Descendants("Nights").First().Value) <= 15)
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
                        if (JuniperW2M == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        if (EgyptExpress == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        if (SalTour == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        if (tbo == 1 && req.Descendants("RoomPax").Count() < 10)
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
                        if (LOH == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        if (Gadou == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        if (SunHotels == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10 && !(req.Descendants("MinStarRating").FirstOrDefault().Value.Equals("0") && req.Descendants("MaxStarRating").FirstOrDefault().Value.Equals("0")))
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
                        if (totalstay == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
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
                        if (SmyRooms == 1 && req.Descendants("RoomPax").Count() < 5)
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
                        if (AlphaTours == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        if (Hoojoozat == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        if (vot == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        if (ebookingcenter == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
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
                        #endregion
                        #region Merge Hotel's List
                        IEnumerable<XElement> request = req.Descendants("searchRequest");
                        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                        #region Get Hotel List
                        IEnumerable<XElement> darinahotels = null;
                        List<XElement> listextanethtl = null;
                        List<XElement> listtouricohtl = null;
                        List<XElement> listhotelbedshtl = null;
                        List<XElement> listdotwhtl = null;
                        List<XElement> listhotelsprohtl = null;
                        List<XElement> listtravcohtl = null;
                        List<XElement> listjactravelhtl = null;
                        List<XElement> listtotalstayhtl = null;
                        List<XElement> listRTShtl = null;
                        List<XElement> listMikihtl = null;
                        List<XElement> listRestelhtl = null;
                        List<XElement> listJuniperW2mtl = null;
                        List<XElement> listegyptexprstl = null;
                        List<XElement> listalphtourstl = null;
                        List<XElement> listSalhtl = null;
                        List<XElement> listTBOhtl = null;
                        List<XElement> listJuniperLOHtl = null;
                        List<XElement> listJuniperLCItl = null;
                        List<XElement> listGadouhtl = null;
                        List<XElement> listsunhotelstl = null;
                        List<XElement> listSmyhtl = null;
                        List<XElement> listhoojhotelstl = null;
                        List<XElement> listvothotelstl = null;
                        List<XElement> listebookingcenterhotelstl = null;
                        List<Task> tasks = new List<Task>();
                        #region Darina
                        if (darina == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) == 1)
                        {
                            if (darinahotellist != null)
                            {
                                if (darinahotellist.Count() > 0)
                                {
                                    try
                                    {
                                        tasks.Add(Task.Run(() => { darinahotels = darinahotellist; }));
                                    }
                                    catch { }
                                }
                            }
                        }
                        #endregion
                        #region Tourico
                        if (tourico == 1)
                        {
                            if (responsetourico != null)
                            {
                                if (responsetourico.Count() > 0)
                                {
                                    try
                                    {
                                        tasks.Add(Task.Run(() => { listtouricohtl = responsetourico; }));
                                    }
                                    catch { }
                                }
                            }
                        }
                        #endregion
                        #region Extranet
                        if (extranet == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 7)
                        {
                            if (hotelavailabilitylistextranet != null)
                            {
                                if (hotelavailabilitylistextranet.Count() > 0)
                                {
                                    try
                                    {
                                        tasks.Add(Task.Run(() => { listextanethtl = hotelavailabilitylistextranet; }));
                                    }
                                    catch { }
                                }
                            }
                        }
                        #endregion
                        #region HotelBeds
                        if (hotelbeds == 1)
                        {
                            if (hotelbedslist != null)
                            {
                                if (hotelbedslist.Count() > 0)
                                {
                                    try
                                    {
                                        tasks.Add(Task.Run(() => { ; }));
                                        listhotelbedshtl = hotelbedslist;
                                    }
                                    catch { }
                                }
                            }
                        }
                        #endregion
                        #region DOTW
                        if (DOTW == 1)
                        {

                            if (dotwlist != null)
                            {
                                try
                                {
                                    if (dotwlist.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listdotwhtl = dotwlist; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region HotelsPro
                        if (hotelspro == 1)
                        {
                            if (responsehotelspro != null)
                            {
                                if (responsehotelspro.Count() > 0)
                                {
                                    try
                                    {
                                        tasks.Add(Task.Run(() => { listhotelsprohtl = responsehotelspro; }));
                                    }
                                    catch { }
                                }
                            }
                        }
                        #endregion
                        #region Travco
                        if (travco == 1)
                        {
                            if (travcolist != null)
                            {
                                if (travcolist.Count() > 0)
                                {
                                    try
                                    {
                                        tasks.Add(Task.Run(() => { listtravcohtl = travcolist; }));
                                    }
                                    catch { }
                                }
                            }
                        }
                        #endregion
                        #region Jac Travel
                        if (JacTravel == 1)
                        {
                            if (jactravelslist != null)
                            {
                                if (jactravelslist.Count() > 0)
                                {
                                    try
                                    {
                                        tasks.Add(Task.Run(() => { listjactravelhtl = jactravelslist; }));
                                    }
                                    catch { }
                                }
                            }
                        }
                        #endregion
                        #region Total Stay
                        if (totalstay == 1)
                        {
                            if (totalstaylist != null)
                            {
                                if (totalstaylist.Count() > 0)
                                {
                                    try
                                    {
                                        tasks.Add(Task.Run(() => { listtotalstayhtl = totalstaylist; }));
                                    }
                                    catch { }
                                }
                            }
                        }
                        #endregion
                        #region RTS
                        if (RTS == 1)
                        {

                            if (RTSlst != null)
                            {
                                if (RTSlst.Count() > 0)
                                {
                                    try
                                    {
                                        tasks.Add(Task.Run(() => { listRTShtl = RTSlst; }));
                                    }
                                    catch { }
                                }
                            }
                        }
                        #endregion
                        #region MIKI
                        if (Miki == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 6)
                        {
                            if (Mikilst != null)
                            {
                                try
                                {
                                    if (Mikilst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listMikihtl = Mikilst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region Restel
                        if (restel == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 5)
                        {
                            if (Restellst != null)
                            {
                                try
                                {
                                    if (Restellst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listRestelhtl = Restellst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region JuniperW2M
                        if (JuniperW2M == 1)
                        {
                            if (JuniparW2Mlst != null)
                            {
                                try
                                {
                                    if (JuniparW2Mlst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listJuniperW2mtl = JuniparW2Mlst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region Egypt Express
                        if (EgyptExpress == 1)
                        {
                            if (egyptexpresslst != null)
                            {
                                try
                                {
                                    if (egyptexpresslst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listegyptexprstl = egyptexpresslst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region Sal Tours
                        if (SalTour == 1)
                        {
                            try
                            {
                                if (Sallst.Count() > 0)
                                {
                                    tasks.Add(Task.Run(() => { listSalhtl = Sallst; }));
                                }
                            }
                            catch { }
                        }
                        #endregion
                        #region TBO Holiday
                        if (tbo == 1)
                        {
                            try
                            {
                                if (TBOlst.Count > 0)
                                {
                                    tasks.Add(Task.Run(() => { listTBOhtl = TBOlst; }));
                                }
                            }
                            catch { }
                        }
                        #endregion
                        #region LOH
                        if (LOH == 1)
                        {
                            if (JuniparLOHlst != null)
                            {
                                try
                                {
                                    if (JuniparLOHlst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listJuniperLOHtl = JuniparLOHlst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region Godou
                        if (Gadou == 1)
                        {
                            if (Gadoulst != null)
                            {
                                try
                                {
                                    if (Gadoulst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listGadouhtl = Gadoulst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region LCI
                        if (LCI == 1)
                        {
                            if (JuniparLCIlst != null)
                            {
                                try
                                {
                                    if (JuniparLCIlst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listJuniperLCItl = JuniparLCIlst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region SunHotels
                        if (SunHotels == 1)
                        {
                            if (sunhotelslst != null)
                            {
                                try
                                {
                                    if (sunhotelslst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listsunhotelstl = sunhotelslst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region SmyRooms
                        if (SmyRooms == 1 && req.Descendants("RoomPax").Count() < 5)
                        {
                            try
                            {
                                if (Smylst.Count() > 0)
                                {
                                    tasks.Add(Task.Run(() => { listSmyhtl = Smylst; }));
                                }
                            }
                            catch { }
                        }
                        #endregion
                        #region Alpha Tours
                        if (AlphaTours == 1)
                        {
                            if (alphatourslst != null)
                            {
                                try
                                {
                                    if (alphatourslst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listalphtourstl = alphatourslst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region Hoojoozat
                        if (Hoojoozat == 1)
                        {
                            if (hoojhotelslst != null)
                            {
                                try
                                {
                                    if (hoojhotelslst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listhoojhotelstl = hoojhotelslst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region VOT
                        if (vot == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            if (vothotelslst != null)
                            {
                                try
                                {
                                    if (vothotelslst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listvothotelstl = vothotelslst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region Ebookingcenter
                        if (ebookingcenter == 1 && Convert.ToInt32(req.Descendants("RoomPax").Count()) < 10)
                        {
                            if (ebookingcenterhotelslst != null)
                            {
                                try
                                {
                                    if (ebookingcenterhotelslst.Count() > 0)
                                    {
                                        tasks.Add(Task.Run(() => { listebookingcenterhotelstl = ebookingcenterhotelslst; }));
                                    }
                                }
                                catch { }
                            }
                        }
                        #endregion
                        Task.WaitAll(tasks.ToArray());
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
                               new XElement("searchResponse",
                                   new XElement("Hotels",
                                        darinahotels,
                                          listtouricohtl,
                                          listextanethtl,
                                           listhotelbedshtl,
                                           listdotwhtl,
                                           listhotelsprohtl,
                                           listtravcohtl,
                                           listjactravelhtl,
                                           listtotalstayhtl,
                                           listRTShtl,
                                           listMikihtl,
                                           listRestelhtl,
                                            listJuniperW2mtl,
                                            listegyptexprstl,
                                            listSalhtl,
                                            listTBOhtl,
                                             listJuniperLOHtl,
                                             listGadouhtl,
                                             listJuniperLCItl,
                                             listsunhotelstl,
                                             listSmyhtl,
                                             listalphtourstl,
                                             listhoojhotelstl,
                                             listvothotelstl,
                                             listebookingcenterhotelstl
                                       )
                      ))));
                        return searchdoc;
                        #endregion
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
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "CreateCheckAvailability";
                    ex1.PageName = "TrvHotelSearch";
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
                availDarina = CallWebService(reqTravillio, _url, _action, flag);
            }
            catch (Exception ex)
            {
                availDarina = "";
            }
            #endregion
        }
        public void gethoteldetailDarina()
        {
            #region Darina Hotel Detail
            try
            {
                string flag = "htdetail";
                //string _url = "http://travelcontrol-agents-api.azurewebsites.net/service.asmx";
                DarinaCredentials _credential = new DarinaCredentials();
                string _url = _credential.APIURL;
                string _action = "http://travelcontrol.softexsw.us/GetBasicData_Properties_WithFullDetails";
                hoteldetailDarina = CallWebService(reqTravillio, _url, _action, flag);
            }
            catch (Exception ex)
            {
                hoteldetailDarina = "";
            }
            #endregion
        }
        public void gethotelavaillistTourico()
        {
            #region Tourio Hotel Availability

            List<Tourico.Hotel> result = new List<Tourico.Hotel>();

            Tourico.AuthenticationHeader hd = new Tourico.AuthenticationHeader();
            hd.LoginName = touricouserlogin;// "HOL916";
            hd.Password = touricopassword;// "111111";
            hd.Version = touricoversion;// "5";

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

            Tourico.SearchResult resultresponse = client.SearchHotels(hd, request, feature);

            hotelavailabilityresult = (resultresponse).HotelList.ToList();
            #region Log Save
            #region supplier Request Log
            string touricologreq = "";
            try
            {
                XmlSerializer serializer1 = new XmlSerializer(typeof(Tourico.SearchRequest));

                using (StringWriter writer = new StringWriter())
                {
                    serializer1.Serialize(writer, request);
                    touricologreq = writer.ToString();
                }
            }
            catch { touricologreq = reqTravillio.ToString(); }
            #endregion
            XmlSerializer serializer = new XmlSerializer(typeof(Tourico.SearchResult));
            string touricologres = "";
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, resultresponse);
                touricologres = writer.ToString();
            }
            try
            {
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(reqTravillio.Descendants("CustomerID").Single().Value);
                log.TrackNumber = reqTravillio.Descendants("TransID").Single().Value;
                log.LogTypeID = 1;
                log.LogType = "Search";
                log.SupplierID = 2;
                log.logrequestXML = touricologreq.ToString();
                log.logresponseXML = touricologres.ToString();
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "gethotelavaillistTourico";
                ex1.PageName = "TrvHotelSearch";
                ex1.CustomerID = reqTravillio.Descendants("CustomerID").Single().Value;
                ex1.TranID = reqTravillio.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
            }

            #endregion

            #endregion

        }
        #region Extranet Availability
        public void gethotelavailabilityExtranet()
        {
            List<XElement> doc1 = new List<XElement>();
            HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
            #region Need to comment later

            APILogDetail logreq = new APILogDetail();
            logreq.customerID = Convert.ToInt64(reqTravillio.Descendants("CustomerID").Single().Value);
            logreq.LogTypeID = 1;
            logreq.LogType = "Search";
            logreq.SupplierID = 3;
            logreq.logrequestXML = reqTravillio.ToString();
            logreq.logresponseXML = "";
            SaveAPILog savelog = new SaveAPILog();
            savelog.SaveAPILogs(logreq);

            #endregion
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
                              "<CustomerID>" + reqTravillio.Descendants("CustomerID").FirstOrDefault().Value + "</CustomerID>" +
                              "<FromDate>" + reqTravillio.Descendants("FromDate").FirstOrDefault().Value + "</FromDate>" +
                              "<ToDate>" + reqTravillio.Descendants("ToDate").FirstOrDefault().Value + "</ToDate>" +
                              "<Nights>1</Nights>" +
                              "<CountryID>0</CountryID>" +
                              "<CountryName />" +
                              "<CityCode>" + reqTravillio.Descendants("CityCode").FirstOrDefault().Value + "</CityCode>" +
                              "<CityName>" + reqTravillio.Descendants("CityCode").FirstOrDefault().Value + "</CityName>" +
                              "<AreaID></AreaID>" +
                              "<AreaName />" +
                              "<MinStarRating>" + reqTravillio.Descendants("MinStarRating").FirstOrDefault().Value + "</MinStarRating>" +
                              "<MaxStarRating>" + reqTravillio.Descendants("MaxStarRating").FirstOrDefault().Value + "</MaxStarRating>" +
                              "<HotelName></HotelName>" +
                              "<PaxNationality_CountryID>" + reqTravillio.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value + "</PaxNationality_CountryID>" +
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

                object result = extclient.GetSearchCityRequestByXML(requestxml, false);
                if (result != null)
                {
                    XElement doc = XElement.Parse(result.ToString());
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(reqTravillio.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = reqTravillio.Descendants("TransID").Single().Value;
                        log.LogTypeID = 1;
                        log.LogType = "Search";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = doc.ToString();
                        SaveAPILog savelogt = new SaveAPILog();
                        savelogt.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "gethotelavailabilityExtranet";
                        ex1.PageName = "TrvHotelSearch";
                        ex1.CustomerID = reqTravillio.Descendants("CustomerID").Single().Value;
                        ex1.TranID = reqTravillio.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    try
                    {
                        hotelavailabilitylistextranet = doc.Descendants("Hotel").ToList();
                    }
                    catch
                    {
                        hotelavailabilitylistextranet = null;
                    }
                }
                else
                {

                    hotelavailabilitylistextranet = null;

                }
            }
            catch (Exception ex)
            {

                hotelavailabilitylistextranet = null;
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
                                               new XElement("AreaName", Convert.ToString(hoteldetails.Descendants("AreaName").Single().Value)),
                                               new XElement("RequestID", Convert.ToString(""))
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
                WriteToFileResponseTime("Skip HotelID Tourico Start");
                Int32 length = htlist.Count();
                int minstarlevel = Convert.ToInt32(reqTravillio.Descendants("MinStarRating").Single().Value);
                int maxstarlevel = Convert.ToInt32(reqTravillio.Descendants("MaxStarRating").Single().Value);
                #region For Static Data
                //XElement staticallhotellist = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\HotelInfo.xml"));
                XElement staticallhotellist = statictouricohotellist;
                List<XElement> statichotellist = staticallhotellist.Descendants("Hotel").Where(x => x.Descendants("sDestination").SingleOrDefault().Value == reqTravillio.Descendants("CityCode").SingleOrDefault().Value || x.Descendants("sHotelCityName").SingleOrDefault().Value == reqTravillio.Descendants("CityName").SingleOrDefault().Value).ToList();

                // List<XElement> statichotellist = staticallhotellist.Descendants("Hotel").Where(x => x.Descendants("sDestination").SingleOrDefault().Value == reqTravillio.Descendants("CityCode").SingleOrDefault().Value || (x.Descendants("State").SingleOrDefault().Value == reqTravillio.Descendants("CityName").SingleOrDefault().Value || x.Descendants("sHotelCityName").SingleOrDefault().Value == reqTravillio.Descendants("CityName").SingleOrDefault().Value && x.Descendants("CountryCode").SingleOrDefault().Value == reqTravillio.Descendants("CountryCode").SingleOrDefault().Value)).ToList();

                #endregion
                try
                {
                    //Parallel.For(0, length, i =>
                    for (int i = 0; i < length; i++)
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
                                try
                                {
                                    IEnumerable<XElement> roomlst = GetHotelRoomgroupTourico(rmtypelist);
                                    if (roomlst.ToList().Count() > 0)
                                    {
                                        //1341374
                                        try
                                        {

                                            hotellst.Add(new XElement("Hotel",
                                                                   new XElement("HotelID", Convert.ToString(hoteldetail.Descendants("HotelId").FirstOrDefault().Value)),
                                                                   new XElement("HotelName", Convert.ToString(hoteldetail.Descendants("HotelName").FirstOrDefault().Value)),
                                                                   new XElement("PropertyTypeName", Convert.ToString(htlist[i].PropertyType)),
                                                                   new XElement("CountryID", Convert.ToString(hoteldetail.Descendants("CountryCode").FirstOrDefault().Value)),
                                                                   new XElement("CountryName", Convert.ToString(hoteldetail.Descendants("CountryName").FirstOrDefault().Value)),
                                                                   new XElement("CountryCode", Convert.ToString(hoteldetail.Descendants("CountryCode").FirstOrDefault().Value)),
                                                                   new XElement("CityId", Convert.ToString("")),
                                                                   new XElement("CityCode", Convert.ToString(hoteldetail.Descendants("sDestination").FirstOrDefault().Value)),
                                                                   new XElement("CityName", Convert.ToString(hoteldetail.Descendants("AddressCity").FirstOrDefault().Value)),
                                                                   new XElement("AreaId", Convert.ToString("")),
                                                                   new XElement("AreaName", Convert.ToString(hoteldetail.Descendants("Location").FirstOrDefault().Value)),
                                                                   new XElement("RequestID", Convert.ToString("")),
                                                                   new XElement("Address", Convert.ToString(hoteldetail.Descendants("Address").FirstOrDefault().Value)),
                                                                   new XElement("Location", Convert.ToString(hoteldetail.Descendants("Location").FirstOrDefault().Value)),
                                                                   new XElement("Description", Convert.ToString(htlist[i].desc)),
                                                                   new XElement("StarRating", Convert.ToString(hoteldetail.Descendants("Stars").FirstOrDefault().Value)),
                                                                   new XElement("MinRate", Convert.ToString(totalamt))
                                                                   , new XElement("HotelImgSmall", Convert.ToString(hoteldetail.Descendants("ThumbnailPath").FirstOrDefault().Value)),
                                                                   new XElement("HotelImgLarge", Convert.ToString(hoteldetail.Descendants("ThumbnailPath").FirstOrDefault().Value)),
                                                                   new XElement("MapLink", ""),
                                                                   new XElement("Longitude", Convert.ToString(hoteldetail.Descendants("Longitude").FirstOrDefault().Value)),
                                                                   new XElement("Latitude", Convert.ToString(hoteldetail.Descendants("Latitude").FirstOrDefault().Value)),
                                                                   new XElement("DMC", "Tourico"),
                                                                   new XElement("SupplierID", "2"),
                                                                   new XElement("Currency", Convert.ToString(htlist[i].currency)),
                                                                   new XElement("Offers", Convert.ToString(exclusivedeal))
                                                                   , new XElement("Facilities",
                                                                       new XElement("Facility", "No Facility Available"))
                                                                   , new XElement("Rooms",
                                                                       roomlst
                                                                       )
                                            ));

                                        }
                                        catch { }
                                    }

                                }
                                catch (Exception ex)
                                { }
                            }
                            else
                            {
                                WriteToFileResponseTimeHotel(Convert.ToString(htlist[i].hotelId));
                            }
                        }
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    return hotellst;
                }
                WriteToFileResponseTime("Skip HotelID Tourico End");
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
            //Parallel.For(0, roomlist.Count(), i =>
            for (int i = 0; i < roomlist.Count(); i++)
            {
                str = GetHotelRoomListingTourico(roomlist[i]).ToList();
                //Parallel.For(0, str.Count(), k =>
                for (int k = 0; k < str.Count(); k++)
                {
                    try
                    {
                        str[k].Add(new XAttribute("Index", rindex));
                        strgrp.Add(str[k]);
                        rindex++;
                    }
                    catch (Exception ee) { }
                };

            };




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
                var startTime = DateTime.Now;
                var _url = url;
                var _action = action;
                XDocument soapEnvelopeXml = new XDocument();
                APILogDetail log = new APILogDetail();
                if (flag == "htlist")
                {
                    soapEnvelopeXml = CreateSoapEnvelopehtlist(req);
                    log.LogTypeID = 1;
                    log.LogType = "Search";
                }
                if (flag == "htdetail")
                {
                    soapEnvelopeXml = CreateSoapEnvelopehtdetail(req);
                    log.LogTypeID = 10;
                    log.LogType = "HotelDetail";
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
                    #region Log
                    //if (flag == "htlist")
                    {
                        try
                        {
                            XElement availresponse = XElement.Parse(soapResult.ToString());
                            XElement doc = RemoveAllNamespaces(availresponse);

                            log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                            log.TrackNumber = req.Descendants("TransID").Single().Value;
                            log.SupplierID = 1;
                            log.logrequestXML = soapEnvelopeXml.ToString();
                            log.logresponseXML = doc.ToString();
                            log.StartTime = startTime;
                            log.EndTime = DateTime.Now;
                            SaveAPILog savelog = new SaveAPILog();
                            savelog.SaveAPILogs(log);
                        }
                        catch (Exception ex)
                        {
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "CallWebService";
                            ex1.PageName = "TrvHotelSearch";
                            ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                            ex1.TranID = req.Descendants("TransID").Single().Value;
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                        }
                    }
                    #endregion
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
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                         "<soap:Body>" +
                           "<CheckAvailability xmlns='http://travelcontrol.softexsw.us/'>" +
                             "<SecStr>#C|559341#W#274298</SecStr>" +
                             "<AccountName>DTC</AccountName>" +
                             "<UserName>XML2016Ra</UserName>" +
                             "<Password>DarinAH</Password>" +
                             "<AgentID>1701</AgentID>" +
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

            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                         "<soap:Body>" +
                           "<GetBasicData_Properties_WithFullDetails xmlns='http://travelcontrol.softexsw.us/'>" +
                             "<SecStr>#C|559341#W#274298</SecStr>" +
                             "<AccountName>DTC</AccountName>" +
                             "<UserName>XML2016Ra</UserName>" +
                             "<Password>DarinAH</Password>" +
                             "<AgentID>1701</AgentID>" +
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

        void obj_MyEvent(List<XElement> lst)
        {
            if (jactravelslist == null)
            {
                jactravelslist = lst;
            }
            foreach (XElement item in lst)
            {
                jactravelslist.Add(item);
            }
        }
        void obj_MyEvent1(List<XElement> lst)
        {
            if (totalstaylist == null)
            {
                totalstaylist = lst;
            }
            else
            {
                foreach (XElement item in lst)
                {
                    totalstaylist.Add(item);
                }
            }
        }
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