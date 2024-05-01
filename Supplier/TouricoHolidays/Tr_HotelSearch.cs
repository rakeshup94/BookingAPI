using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Supplier.TouricoHolidays;



using System.Net;
using System.Xml;

using System.Threading.Tasks;
using System.Threading;

using System.Collections;

namespace TravillioXMLOutService.Supplier.TouricoHolidays
{
    public class Tr_HotelSearch : IDisposable
    {
        string touricouserlogin = string.Empty;
        string touricopassword = string.Empty;
        string touricoversion = string.Empty;
        XElement statictouricohotellist;
        XElement reqTravillio;
        List<Tourico.Hotel> hotelavailabilityresult;
        List<XElement> touricoht = null;
        int totroom = 1;
        string dmc = string.Empty;
        #region Hotel Availability (Tourico)
        public List<XElement> HotelSearch_Tourico(XElement req, XElement statictdata)
        {
            try
            {
                totroom = Convert.ToInt32(req.Descendants("RoomPax").ToList().Count());
                reqTravillio = req;
                dmc = "Tourico";
                #region Credentials
                //XElement credential = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\Credential.xml"));
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "2");
                touricouserlogin = suppliercred.Descendants("username").FirstOrDefault().Value;
                touricopassword = suppliercred.Descendants("password").FirstOrDefault().Value;
                touricoversion = suppliercred.Descendants("version").FirstOrDefault().Value;
                //touricouserlogin = credential.Descendants("username").FirstOrDefault().Value;
                //touricopassword = credential.Descendants("password").FirstOrDefault().Value;
                //touricoversion = credential.Descendants("version").FirstOrDefault().Value;
                #endregion

                #region Tourio Hotel Availability
                
                statictouricohotellist = statictdata;
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
                    List<XElement> chldcnt = trum[j].Descendants("ChildAge").ToList();
                    for (int k = 0; k < childcount; k++)
                    {                        
                        chdage[k] = new Tourico.ChildAge
                        {
                            age = Convert.ToInt32(chldcnt[k].Value)
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
                request.HotelCityName = reqTravillio.Descendants("CityName").FirstOrDefault().Value.ToString();
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
                var startTime = DateTime.Now;
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
                    log.StartTime = startTime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SaveAPILogs(log);
                    //APILog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "gethotelavaillistTourico";
                    ex1.PageName = "TrvHotelSearch";
                    ex1.CustomerID = reqTravillio.Descendants("CustomerID").Single().Value;
                    ex1.TranID = reqTravillio.Descendants("TransID").Single().Value;
                    //APILog.SendCustomExcepToDB(ex1);
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }

                #endregion

                touricoht = GetHotelListTourico(hotelavailabilityresult).ToList();
                return touricoht;
                #endregion
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelSearch_Tourico";
                ex1.PageName = "Tr_HotelSearch";
                ex1.CustomerID = reqTravillio.Descendants("CustomerID").Single().Value;
                ex1.TranID = reqTravillio.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        public List<XElement> HotelSearch_TouricoHAOUT(XElement req, XElement statictdata)
        {
            
            try
            {
                totroom = Convert.ToInt32(req.Descendants("RoomPax").ToList().Count());
                reqTravillio = req;
                dmc = "HA";
                #region Credentials
                //XElement credential = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\Credential.xml"));
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "2");
                touricouserlogin = suppliercred.Descendants("username").FirstOrDefault().Value;
                touricopassword = suppliercred.Descendants("password").FirstOrDefault().Value;
                touricoversion = suppliercred.Descendants("version").FirstOrDefault().Value;
                //touricouserlogin = credential.Descendants("username").FirstOrDefault().Value;
                //touricopassword = credential.Descendants("password").FirstOrDefault().Value;
                //touricoversion = credential.Descendants("version").FirstOrDefault().Value;
                #endregion

                #region Tourio Hotel Availability
                
                statictouricohotellist = statictdata;
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
                    List<XElement> chldcnt = trum[j].Descendants("ChildAge").ToList();
                    for (int k = 0; k < childcount; k++)
                    {
                        chdage[k] = new Tourico.ChildAge
                        {
                            age = Convert.ToInt32(chldcnt[k].Value)
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
                request.HotelCityName = reqTravillio.Descendants("CityName").FirstOrDefault().Value.ToString();
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
                var startTime = DateTime.Now;
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
                    log.StartTime = startTime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SaveAPILogs(log);
                    //APILog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelSearch_TouricoHAOUT";
                    ex1.PageName = "Tr_HotelSearch";
                    ex1.CustomerID = reqTravillio.Descendants("CustomerID").Single().Value;
                    ex1.TranID = reqTravillio.Descendants("TransID").Single().Value;
                    //APILog.SendCustomExcepToDB(ex1);
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }

                #endregion

                touricoht = GetHotelListTourico(hotelavailabilityresult).ToList();
                return touricoht;
                #endregion
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelSearch_TouricoHAOUT";
                ex1.PageName = "Tr_HotelSearch";
                ex1.CustomerID = reqTravillio.Descendants("CustomerID").Single().Value;
                ex1.TranID = reqTravillio.Descendants("TransID").Single().Value;
                //APILog.SendCustomExcepToDB(ex1);
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        #endregion
        #region Tourico Holidays
        #region Tourico Hotel Listing
        private IEnumerable<XElement> GetHotelListTourico(List<Tourico.Hotel> htlist)
        {
            #region Tourico Hotels
            List<Tourico.Occupancy> roomList1 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList2 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList3 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList4 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList5 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList6 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList7 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList8 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList9 = new List<Tourico.Occupancy>();
            List<XElement> hotellst = new List<XElement>();
            try
            {
                //WriteToFileResponseTime("Skip HotelID Tourico Start");
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
                        try
                        {
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
                                        totalamt = rmtypelist[0].Occupancies[0].occupPrice + rmtypelist[0].Occupancies[0].BoardBases[0].bbPrice;
                                    }
                                    else
                                    {
                                        totalamt = htlist[i].RoomTypes[0].Occupancies[0].occupPrice;
                                    }
                                    #region Min Rate
                                    try
                                    {
                                        #region room 1
                                        if (totroom == 1)
                                        {
                                            if (bb > 0)
                                            {
                                                totalamt = rmtypelist[0].Occupancies[0].occupPrice + rmtypelist[0].Occupancies[0].BoardBases[0].bbPrice;
                                            }
                                            else
                                            {
                                                totalamt = htlist[i].RoomTypes[0].Occupancies[0].occupPrice;
                                            }
                                        }
                                        #endregion
                                        #region room 2
                                        if (totroom == 2)
                                        {
                                            decimal room1 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 1), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room2 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 2), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            totalamt = room1 + room2;
                                        }
                                        #endregion
                                        #region room 3
                                        if (totroom == 3)
                                        {
                                            decimal room1 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 1), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room2 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 2), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room3 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 3), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            totalamt = room1 + room2 + room3;
                                        }
                                        #endregion
                                        #region room 4
                                        if (totroom == 4)
                                        {
                                            decimal room1 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 1), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room2 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 2), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room3 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 3), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room4 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 4), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            totalamt = room1 + room2 + room3 + room4;
                                        }
                                        #endregion
                                        #region room 5
                                        if (totroom == 5)
                                        {
                                            decimal room1 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 1), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room2 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 2), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room3 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 3), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room4 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 4), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room5 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 5), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            totalamt = room1 + room2 + room3 + room4 + room5;
                                        }
                                        #endregion
                                        #region room 6
                                        if (totroom == 6)
                                        {
                                            decimal room1 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 1), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room2 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 2), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room3 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 3), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room4 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 4), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room5 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 5), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room6 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 6), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            totalamt = room1 + room2 + room3 + room4 + room5 + room6;
                                        }
                                        #endregion
                                        #region room 7
                                        if (totroom == 7)
                                        {
                                            decimal room1 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 1), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room2 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 2), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room3 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 3), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room4 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 4), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room5 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 5), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room6 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 6), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room7 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 7), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            totalamt = room1 + room2 + room3 + room4 + room5 + room6 + room7;
                                        }
                                        #endregion
                                        #region room 8
                                        if (totroom == 8)
                                        {
                                            decimal room1 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 1), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room2 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 2), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room3 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 3), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room4 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 4), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room5 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 5), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room6 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 6), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room7 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 7), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room8 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 8), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            totalamt = room1 + room2 + room3 + room4 + room5 + room6 + room7 + room8;
                                        }
                                        #endregion
                                        #region room 9
                                        if (totroom == 9)
                                        {
                                            decimal room1 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 1), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room2 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 2), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room3 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 3), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room4 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 4), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room5 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 5), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room6 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 6), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room7 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 7), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room8 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 8), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            decimal room9 = rmtypelist[0].Occupancies.SelectMany(x => x.Rooms.Where(y => y.seqNum == 9), (x, y) => new { r = x, d = y }).FirstOrDefault().r.occupPrice;
                                            totalamt = room1 + room2 + room3 + room4 + room5 + room6 + room7 + room8 + room9;
                                        }
                                        #endregion
                                    }
                                    catch
                                    {
                                        if (bb > 0)
                                        {
                                            totalamt = rmtypelist[0].Occupancies[0].occupPrice + rmtypelist[0].Occupancies[0].BoardBases[0].bbPrice;
                                        }
                                        else
                                        {
                                            totalamt = htlist[i].RoomTypes[0].Occupancies[0].occupPrice;
                                        }
                                    }
                                    #endregion
                                    try
                                    {
                                        //IEnumerable<XElement> roomlst = GetHotelRoomgroupTourico(rmtypelist);
                                        //if (roomlst.ToList().Count() > 0)
                                        {
                                            //1341374
                                            try
                                            {
                                                #region Bind Image
                                                string imgpath = string.Empty;
                                                string address = string.Empty;
                                                try
                                                {
                                                    address = hoteldetail.Descendants("Address").FirstOrDefault().Value + ", " + hoteldetail.Descendants("AddressZip").FirstOrDefault().Value + ", " + hoteldetail.Descendants("AddressCity").FirstOrDefault().Value + ", " + hoteldetail.Descendants("CountryName").FirstOrDefault().Value;
                                                    if (hoteldetail.Descendants("ThumbnailPath").FirstOrDefault().Value == "")
                                                    {
                                                        imgpath = htlist[i].thumb;
                                                    }
                                                    else
                                                    {
                                                        imgpath = hoteldetail.Descendants("ThumbnailPath").FirstOrDefault().Value;
                                                    }
                                                }
                                                catch { }
                                                #endregion

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
                                                                       new XElement("Address", Convert.ToString(address)),
                                                                       new XElement("Location", Convert.ToString(hoteldetail.Descendants("Location").FirstOrDefault().Value)),
                                                                       new XElement("Description", Convert.ToString(htlist[i].desc)),
                                                    //new XElement("StarRating", Convert.ToString(hoteldetail.Descendants("Stars").FirstOrDefault().Value)),
                                                                        new XElement("StarRating", Convert.ToString(htlist[i].starsLevel)),
                                                                       new XElement("MinRate", Convert.ToString(totalamt))
                                                                       , new XElement("HotelImgSmall", Convert.ToString(imgpath)),
                                                                       new XElement("HotelImgLarge", Convert.ToString(imgpath)),
                                                                       new XElement("MapLink", ""),
                                                                       new XElement("Longitude", Convert.ToString(hoteldetail.Descendants("Longitude").FirstOrDefault().Value)),
                                                                       new XElement("Latitude", Convert.ToString(hoteldetail.Descendants("Latitude").FirstOrDefault().Value)),
                                                                       new XElement("DMC", dmc),
                                                                       new XElement("SupplierID", "2"),
                                                                       new XElement("Currency", Convert.ToString(htlist[i].currency)),
                                                                       new XElement("Offers", Convert.ToString(exclusivedeal))
                                                                       , new XElement("Facilities",null)
                                                                           //new XElement("Facility", "No Facility Available"))
                                                                       , new XElement("Rooms",""
                                                                           //roomlst
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
                                    //WriteToFileResponseTimeHotel(Convert.ToString(htlist[i].hotelId));
                                }
                            }
                        }
                        catch { }
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    return hotellst;
                }
                //WriteToFileResponseTime("Skip HotelID Tourico End");
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
                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].occupPrice),
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
                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPrice)),
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
                            //int countpaidnight1 = 0;
                            //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                            //{
                            //    if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                            //    {
                            //        countpaidnight1 = countpaidnight1 + 1;
                            //    }
                            //});
                            if (roomList1[m].BoardBases[j].bbPrice > 0)
                            { RO = true; }
                            group++;
                            decimal totalamt = roomList1[m].occupPrice + roomList1[m].BoardBases[j].bbPrice;

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
                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPrice)),
                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPrice)),
                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                             )));


                        });

                        #region Room Only
                        if (RO == true)
                        {
                            //int countpaidnight1 = 0;
                            //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                            //{
                            //    if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                            //    {
                            //        countpaidnight1 = countpaidnight1 + 1;
                            //    }
                            //});
                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].occupPrice),

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
                        new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
                        new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPrice)),
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
                            decimal totalp = roomList1[m].occupPrice + roomList2[n].occupPrice;
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
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPrice)),
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
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPrice)),
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
                                //int countpaidnight1 = 0;
                                //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                //{
                                //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                //    {
                                //        countpaidnight1 = countpaidnight1 + 1;
                                //    }
                                //});
                                //int countpaidnight2 = 0;
                                //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                //{
                                //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                //    {
                                //        countpaidnight2 = countpaidnight2 + 1;
                                //    }
                                //});
                                if (roomList1[m].BoardBases[j].bbPrice > 0)
                                { RO = true; }
                                if (roomList2[n].BoardBases[j].bbPrice > 0)
                                { RO = true; }
                                group++;
                                decimal totalamt = roomList1[m].occupPrice + roomList1[m].BoardBases[j].bbPrice;
                                decimal totalamt2 = roomList2[n].occupPrice + roomList2[n].BoardBases[j].bbPrice;
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
                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPrice)),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPrice)),
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
                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPrice)),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPrice)),
                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                 )));



                            });
                            #region RO
                            if (RO == true)
                            {
                                //int countpaidnight1 = 0;
                                //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                //{
                                //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                //    {
                                //        countpaidnight1 = countpaidnight1 + 1;
                                //    }
                                //});
                                //int countpaidnight2 = 0;
                                //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                //{
                                //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                //    {
                                //        countpaidnight2 = countpaidnight2 + 1;
                                //    }
                                //});

                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].occupPrice + roomList2[n].occupPrice),

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
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPrice)),
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
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPrice)),
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
                                decimal totalp = roomList1[m].occupPrice + roomList2[n].occupPrice + roomList3[o].occupPrice;
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
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPrice)),
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
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPrice)),
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
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPrice)),
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
                                    //int countpaidnight1 = 0;
                                    //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                    //{
                                    //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                    //    {
                                    //        countpaidnight1 = countpaidnight1 + 1;
                                    //    }
                                    //});
                                    //int countpaidnight2 = 0;
                                    //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                    //{
                                    //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                    //    {
                                    //        countpaidnight2 = countpaidnight2 + 1;
                                    //    }
                                    //});
                                    //int countpaidnight3 = 0;
                                    //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                    //{
                                    //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                    //    {
                                    //        countpaidnight3 = countpaidnight3 + 1;
                                    //    }
                                    //});
                                    if (roomList1[m].BoardBases[j].bbPrice > 0)
                                    { RO = true; }
                                    if (roomList2[n].BoardBases[j].bbPrice > 0)
                                    { RO = true; }
                                    if (roomList3[o].BoardBases[j].bbPrice > 0)
                                    { RO = true; }
                                    group++;
                                    decimal totalamt = roomList1[m].occupPrice + roomList1[m].BoardBases[j].bbPrice;
                                    decimal totalamt2 = roomList2[n].occupPrice + roomList2[n].BoardBases[j].bbPrice;
                                    decimal totalamt3 = roomList3[o].occupPrice + roomList3[o].BoardBases[j].bbPrice;
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
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPrice)),
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
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPrice)),
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
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPrice)),
                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                     )));
                                });
                                #endregion
                                #region RO
                                if (RO == true)
                                {
                                    //int countpaidnight1 = 0;
                                    //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                    //{
                                    //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                    //    {
                                    //        countpaidnight1 = countpaidnight1 + 1;
                                    //    }
                                    //});
                                    //int countpaidnight2 = 0;
                                    //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                    //{
                                    //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                    //    {
                                    //        countpaidnight2 = countpaidnight2 + 1;
                                    //    }
                                    //});
                                    //int countpaidnight3 = 0;
                                    //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                    //{
                                    //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                    //    {
                                    //        countpaidnight3 = countpaidnight3 + 1;
                                    //    }
                                    //});
                                    decimal totalamt = roomList1[m].occupPrice;
                                    decimal totalamt2 = roomList2[n].occupPrice;
                                    decimal totalamt3 = roomList3[o].occupPrice;
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
                                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                    decimal totalp = roomList1[m].occupPrice + roomList2[n].occupPrice + roomList3[o].occupPrice + roomList4[p].occupPrice;

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
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPrice)),
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
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPrice)),
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
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPrice)),
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
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPrice)),
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
                                        //int countpaidnight1 = 0;
                                        //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                        //{
                                        //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                        //    {
                                        //        countpaidnight1 = countpaidnight1 + 1;
                                        //    }
                                        //});
                                        //int countpaidnight2 = 0;
                                        //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                        //{
                                        //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                        //    {
                                        //        countpaidnight2 = countpaidnight2 + 1;
                                        //    }
                                        //});
                                        //int countpaidnight3 = 0;
                                        //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                        //{
                                        //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                        //    {
                                        //        countpaidnight3 = countpaidnight3 + 1;
                                        //    }
                                        //});
                                        //int countpaidnight4 = 0;
                                        //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                        //{
                                        //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                        //    {
                                        //        countpaidnight4 = countpaidnight4 + 1;
                                        //    }
                                        //});
                                        if (roomList1[m].BoardBases[j].bbPrice > 0)
                                        { RO = true; }
                                        if (roomList2[n].BoardBases[j].bbPrice > 0)
                                        { RO = true; }
                                        if (roomList3[o].BoardBases[j].bbPrice > 0)
                                        { RO = true; }
                                        if (roomList4[p].BoardBases[j].bbPrice > 0)
                                        { RO = true; }
                                        group++;
                                        decimal totalamt = roomList1[m].occupPrice + roomList1[m].BoardBases[j].bbPrice;
                                        decimal totalamt2 = roomList2[n].occupPrice + roomList2[n].BoardBases[j].bbPrice;
                                        decimal totalamt3 = roomList3[o].occupPrice + roomList3[o].BoardBases[j].bbPrice;
                                        decimal totalamt4 = roomList4[p].occupPrice + roomList4[p].BoardBases[j].bbPrice;
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
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPrice)),
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
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPrice)),
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
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPrice)),
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
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                         )));
                                    }
                                    #endregion
                                    #region RO
                                    if (RO == true)
                                    {
                                        //int countpaidnight1 = 0;
                                        //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                        //{
                                        //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                        //    {
                                        //        countpaidnight1 = countpaidnight1 + 1;
                                        //    }
                                        //});
                                        //int countpaidnight2 = 0;
                                        //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                        //{
                                        //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                        //    {
                                        //        countpaidnight2 = countpaidnight2 + 1;
                                        //    }
                                        //});
                                        //int countpaidnight3 = 0;
                                        //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                        //{
                                        //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                        //    {
                                        //        countpaidnight3 = countpaidnight3 + 1;
                                        //    }
                                        //});
                                        //int countpaidnight4 = 0;
                                        //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                        //{
                                        //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                        //    {
                                        //        countpaidnight4 = countpaidnight4 + 1;
                                        //    }
                                        //});

                                        group++;
                                        decimal totalamt = roomList1[m].occupPrice;
                                        decimal totalamt2 = roomList2[n].occupPrice;
                                        decimal totalamt3 = roomList3[o].occupPrice;
                                        decimal totalamt4 = roomList4[p].occupPrice;
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
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                #region GRP
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
                #endregion
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
                                    // add room 1, 2, 3, 4,5

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
                                        decimal totalp = roomList1[m].occupPrice + roomList2[n].occupPrice + roomList3[o].occupPrice + roomList4[p].occupPrice + roomList5[q].occupPrice;

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
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPrice)),
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
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPrice)),
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
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPrice)),
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
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPrice)),
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
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPrice)),
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
                                        bool RO = false;
                                        #region Board Bases >0
                                        for (int j = 0; j < bb; j++)
                                        {
                                            //int countpaidnight1 = 0;
                                            //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                            //{
                                            //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                            //    {
                                            //        countpaidnight1 = countpaidnight1 + 1;
                                            //    }
                                            //});
                                            //int countpaidnight2 = 0;
                                            //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                            //{
                                            //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                            //    {
                                            //        countpaidnight2 = countpaidnight2 + 1;
                                            //    }
                                            //});
                                            //int countpaidnight3 = 0;
                                            //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                            //{
                                            //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                            //    {
                                            //        countpaidnight3 = countpaidnight3 + 1;
                                            //    }
                                            //});
                                            //int countpaidnight4 = 0;
                                            //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                            //{
                                            //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                            //    {
                                            //        countpaidnight4 = countpaidnight4 + 1;
                                            //    }
                                            //});
                                            //int countpaidnight5 = 0;
                                            //Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                            //{
                                            //    if (roomList5[p].PriceBreakdown[jj].value != 0)
                                            //    {
                                            //        countpaidnight5 = countpaidnight5 + 1;
                                            //    }
                                            //});
                                            if (roomList1[m].BoardBases[j].bbPrice > 0)
                                            { RO = true; }
                                            if (roomList2[n].BoardBases[j].bbPrice > 0)
                                            { RO = true; }
                                            if (roomList3[o].BoardBases[j].bbPrice > 0)
                                            { RO = true; }
                                            if (roomList4[p].BoardBases[j].bbPrice > 0)
                                            { RO = true; }
                                            if (roomList5[q].BoardBases[j].bbPrice > 0)
                                            { RO = true; }
                                            group++;
                                            decimal totalamt = roomList1[m].occupPrice + roomList1[m].BoardBases[j].bbPrice;
                                            decimal totalamt2 = roomList2[n].occupPrice + roomList2[n].BoardBases[j].bbPrice;
                                            decimal totalamt3 = roomList3[o].occupPrice + roomList3[o].BoardBases[j].bbPrice;
                                            decimal totalamt4 = roomList4[p].occupPrice + roomList4[p].BoardBases[j].bbPrice;
                                            decimal totalamt5 = roomList5[q].occupPrice + roomList5[q].BoardBases[j].bbPrice;
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
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPrice)),
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
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPrice)),
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
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPrice)),
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
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPrice)),
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
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
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
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                             )));
                                        }
                                        #endregion
                                        #region RO
                                        if (RO == true)
                                        {
                                            //int countpaidnight1 = 0;
                                            //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                            //{
                                            //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                            //    {
                                            //        countpaidnight1 = countpaidnight1 + 1;
                                            //    }
                                            //});
                                            //int countpaidnight2 = 0;
                                            //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                            //{
                                            //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                            //    {
                                            //        countpaidnight2 = countpaidnight2 + 1;
                                            //    }
                                            //});
                                            //int countpaidnight3 = 0;
                                            //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                            //{
                                            //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                            //    {
                                            //        countpaidnight3 = countpaidnight3 + 1;
                                            //    }
                                            //});
                                            //int countpaidnight4 = 0;
                                            //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                            //{
                                            //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                            //    {
                                            //        countpaidnight4 = countpaidnight4 + 1;
                                            //    }
                                            //});
                                            //int countpaidnight5 = 0;
                                            //Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                            //{
                                            //    if (roomList5[q].PriceBreakdown[jj].value != 0)
                                            //    {
                                            //        countpaidnight5 = countpaidnight5 + 1;
                                            //    }
                                            //});

                                            group++;
                                            decimal totalamt = roomList1[m].occupPrice;
                                            decimal totalamt2 = roomList2[n].occupPrice;
                                            decimal totalamt3 = roomList3[o].occupPrice;
                                            decimal totalamt4 = roomList4[p].occupPrice;
                                            decimal totalamt5 = roomList5[q].occupPrice;
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
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
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
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
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
                #region GRP
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
                #endregion
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
                                        // add room 1, 2, 3, 4,5,6

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
                                            decimal totalp = roomList1[m].occupPrice + roomList2[n].occupPrice + roomList3[o].occupPrice + roomList4[p].occupPrice + roomList5[q].occupPrice + roomList6[r].occupPrice;

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
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPrice)),
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
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPrice)),
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
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPrice)),
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
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPrice)),
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
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPrice)),
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
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPrice)),
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
                                            bool RO = false;
                                            #region Board Bases >0
                                            for (int j = 0; j < bb; j++)
                                            {
                                                //int countpaidnight1 = 0;
                                                //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight1 = countpaidnight1 + 1;
                                                //    }
                                                //});
                                                //int countpaidnight2 = 0;
                                                //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight2 = countpaidnight2 + 1;
                                                //    }
                                                //});
                                                //int countpaidnight3 = 0;
                                                //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight3 = countpaidnight3 + 1;
                                                //    }
                                                //});
                                                //int countpaidnight4 = 0;
                                                //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight4 = countpaidnight4 + 1;
                                                //    }
                                                //});
                                                //int countpaidnight5 = 0;
                                                //Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList5[p].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight5 = countpaidnight5 + 1;
                                                //    }
                                                //});
                                                //int countpaidnight6 = 0;
                                                //Parallel.For(0, roomList6[r].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList6[r].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight6 = countpaidnight6 + 1;
                                                //    }
                                                //});
                                                if (roomList1[m].BoardBases[j].bbPrice > 0)
                                                { RO = true; }
                                                if (roomList2[n].BoardBases[j].bbPrice > 0)
                                                { RO = true; }
                                                if (roomList3[o].BoardBases[j].bbPrice > 0)
                                                { RO = true; }
                                                if (roomList4[p].BoardBases[j].bbPrice > 0)
                                                { RO = true; }
                                                if (roomList5[q].BoardBases[j].bbPrice > 0)
                                                { RO = true; }
                                                if (roomList6[r].BoardBases[j].bbPrice > 0)
                                                { RO = true; }
                                                group++;
                                                decimal totalamt = roomList1[m].occupPrice + roomList1[m].BoardBases[j].bbPrice;
                                                decimal totalamt2 = roomList2[n].occupPrice + roomList2[n].BoardBases[j].bbPrice;
                                                decimal totalamt3 = roomList3[o].occupPrice + roomList3[o].BoardBases[j].bbPrice;
                                                decimal totalamt4 = roomList4[p].occupPrice + roomList4[p].BoardBases[j].bbPrice;
                                                decimal totalamt5 = roomList5[q].occupPrice + roomList5[q].BoardBases[j].bbPrice;
                                                decimal totalamt6 = roomList6[r].occupPrice + roomList6[r].BoardBases[j].bbPrice;
                                                decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6;

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
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPrice)),
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
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPrice)),
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
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPrice)),
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
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPrice)),
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
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
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
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPrice)),
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
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
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
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                 )));
                                            }
                                            #endregion
                                            #region RO
                                            if (RO == true)
                                            {
                                                //int countpaidnight1 = 0;
                                                //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight1 = countpaidnight1 + 1;
                                                //    }
                                                //});
                                                //int countpaidnight2 = 0;
                                                //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight2 = countpaidnight2 + 1;
                                                //    }
                                                //});
                                                //int countpaidnight3 = 0;
                                                //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight3 = countpaidnight3 + 1;
                                                //    }
                                                //});
                                                //int countpaidnight4 = 0;
                                                //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight4 = countpaidnight4 + 1;
                                                //    }
                                                //});
                                                //int countpaidnight5 = 0;
                                                //Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList5[q].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight5 = countpaidnight5 + 1;
                                                //    }
                                                //});
                                                //int countpaidnight6 = 0;
                                                //Parallel.For(0, roomList6[r].PriceBreakdown.Count(), jj =>
                                                //{
                                                //    if (roomList6[r].PriceBreakdown[jj].value != 0)
                                                //    {
                                                //        countpaidnight6 = countpaidnight6 + 1;
                                                //    }
                                                //});

                                                group++;
                                                decimal totalamt = roomList1[m].occupPrice;
                                                decimal totalamt2 = roomList2[n].occupPrice;
                                                decimal totalamt3 = roomList3[o].occupPrice;
                                                decimal totalamt4 = roomList4[p].occupPrice;
                                                decimal totalamt5 = roomList5[q].occupPrice;
                                                decimal totalamt6 = roomList6[r].occupPrice;
                                                decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6;

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
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                                                 new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
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
                                                 new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
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
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), 0)),
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
                #region GRP
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
                #endregion
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
                                            // add room 1, 2, 3, 4,5,6,7

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
                                                decimal totalp = roomList1[m].occupPrice + roomList2[n].occupPrice + roomList3[o].occupPrice + roomList4[p].occupPrice + roomList5[q].occupPrice + roomList6[r].occupPrice + roomList7[s].occupPrice;

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
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPrice)),
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
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPrice)),
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
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPrice)),
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
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPrice)),
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
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPrice)),
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
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPrice)),
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
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].PriceBreakdown[0].value)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPrice)),
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
                                                bool RO = false;
                                                #region Board Bases >0
                                                for (int j = 0; j < bb; j++)
                                                {
                                                    //int countpaidnight1 = 0;
                                                    //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight1 = countpaidnight1 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight2 = 0;
                                                    //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight2 = countpaidnight2 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight3 = 0;
                                                    //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight3 = countpaidnight3 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight4 = 0;
                                                    //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight4 = countpaidnight4 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight5 = 0;
                                                    //Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList5[p].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight5 = countpaidnight5 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight6 = 0;
                                                    //Parallel.For(0, roomList6[r].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList6[r].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight6 = countpaidnight6 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight7 = 0;
                                                    //Parallel.For(0, roomList7[s].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList7[s].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight7 = countpaidnight7 + 1;
                                                    //    }
                                                    //});
                                                    if (roomList1[m].BoardBases[j].bbPrice > 0)
                                                    { RO = true; }
                                                    if (roomList2[n].BoardBases[j].bbPrice > 0)
                                                    { RO = true; }
                                                    if (roomList3[o].BoardBases[j].bbPrice > 0)
                                                    { RO = true; }
                                                    if (roomList4[p].BoardBases[j].bbPrice > 0)
                                                    { RO = true; }
                                                    if (roomList5[q].BoardBases[j].bbPrice > 0)
                                                    { RO = true; }
                                                    if (roomList6[r].BoardBases[j].bbPrice > 0)
                                                    { RO = true; }
                                                    if (roomList7[s].BoardBases[j].bbPrice > 0)
                                                    { RO = true; }
                                                    group++;
                                                    decimal totalamt = roomList1[m].occupPrice + roomList1[m].BoardBases[j].bbPrice;
                                                    decimal totalamt2 = roomList2[n].occupPrice + roomList2[n].BoardBases[j].bbPrice;
                                                    decimal totalamt3 = roomList3[o].occupPrice + roomList3[o].BoardBases[j].bbPrice;
                                                    decimal totalamt4 = roomList4[p].occupPrice + roomList4[p].BoardBases[j].bbPrice;
                                                    decimal totalamt5 = roomList5[q].occupPrice + roomList5[q].BoardBases[j].bbPrice;
                                                    decimal totalamt6 = roomList6[r].occupPrice + roomList6[r].BoardBases[j].bbPrice;
                                                    decimal totalamt7 = roomList7[s].occupPrice + roomList7[s].BoardBases[j].bbPrice;
                                                    decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7;

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
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPrice)),
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
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPrice)),
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
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPrice)),
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
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPrice)),
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
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
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
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPrice)),
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
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
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
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPrice)),
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
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].PriceBreakdown[0].value)),
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
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                     )));
                                                }
                                                #endregion
                                                #region RO
                                                if (RO == true)
                                                {
                                                    //int countpaidnight1 = 0;
                                                    //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight1 = countpaidnight1 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight2 = 0;
                                                    //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight2 = countpaidnight2 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight3 = 0;
                                                    //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight3 = countpaidnight3 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight4 = 0;
                                                    //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight4 = countpaidnight4 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight5 = 0;
                                                    //Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList5[q].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight5 = countpaidnight5 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight6 = 0;
                                                    //Parallel.For(0, roomList6[r].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList6[r].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight6 = countpaidnight6 + 1;
                                                    //    }
                                                    //});
                                                    //int countpaidnight7 = 0;
                                                    //Parallel.For(0, roomList7[s].PriceBreakdown.Count(), jj =>
                                                    //{
                                                    //    if (roomList7[s].PriceBreakdown[jj].value != 0)
                                                    //    {
                                                    //        countpaidnight7 = countpaidnight7 + 1;
                                                    //    }
                                                    //});

                                                    group++;
                                                    decimal totalamt = roomList1[m].occupPrice;
                                                    decimal totalamt2 = roomList2[n].occupPrice;
                                                    decimal totalamt3 = roomList3[o].occupPrice;
                                                    decimal totalamt4 = roomList4[p].occupPrice;
                                                    decimal totalamt5 = roomList5[q].occupPrice;
                                                    decimal totalamt6 = roomList6[r].occupPrice;
                                                    decimal totalamt7 = roomList7[s].occupPrice;
                                                    decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7;

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
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                                                     new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
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
                                                     new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
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
                                                     new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].PriceBreakdown[0].value)),
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
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), 0)),
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
                #region GRP
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
                #endregion
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
                                                // add room 1, 2, 3, 4,5,6,7,8

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
                                                    decimal totalp = roomList1[m].occupPrice + roomList2[n].occupPrice + roomList3[o].occupPrice + roomList4[p].occupPrice + roomList5[q].occupPrice + roomList6[r].occupPrice + roomList7[s].occupPrice + roomList8[t].occupPrice;

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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPrice)),
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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPrice)),
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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPrice)),
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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPrice)),
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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPrice)),
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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPrice)),
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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].PriceBreakdown[0].value)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPrice)),
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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].PriceBreakdown[0].value)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList8[t].occupPrice)),
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
                                                    bool RO = false;
                                                    #region Board Bases >0
                                                    for (int j = 0; j < bb; j++)
                                                    {
                                                        //int countpaidnight1 = 0;
                                                        //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight1 = countpaidnight1 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight2 = 0;
                                                        //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight2 = countpaidnight2 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight3 = 0;
                                                        //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight3 = countpaidnight3 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight4 = 0;
                                                        //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight4 = countpaidnight4 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight5 = 0;
                                                        //Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList5[p].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight5 = countpaidnight5 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight6 = 0;
                                                        //Parallel.For(0, roomList6[r].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList6[r].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight6 = countpaidnight6 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight7 = 0;
                                                        //Parallel.For(0, roomList7[s].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList7[s].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight7 = countpaidnight7 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight8 = 0;
                                                        //Parallel.For(0, roomList8[t].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList8[t].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight8 = countpaidnight8 + 1;
                                                        //    }
                                                        //});
                                                        if (roomList1[m].BoardBases[j].bbPrice > 0)
                                                        { RO = true; }
                                                        if (roomList2[n].BoardBases[j].bbPrice > 0)
                                                        { RO = true; }
                                                        if (roomList3[o].BoardBases[j].bbPrice > 0)
                                                        { RO = true; }
                                                        if (roomList4[p].BoardBases[j].bbPrice > 0)
                                                        { RO = true; }
                                                        if (roomList5[q].BoardBases[j].bbPrice > 0)
                                                        { RO = true; }
                                                        if (roomList6[r].BoardBases[j].bbPrice > 0)
                                                        { RO = true; }
                                                        if (roomList7[s].BoardBases[j].bbPrice > 0)
                                                        { RO = true; }
                                                        if (roomList8[t].BoardBases[j].bbPrice > 0)
                                                        { RO = true; }
                                                        group++;
                                                        decimal totalamt = roomList1[m].occupPrice + roomList1[m].BoardBases[j].bbPrice;
                                                        decimal totalamt2 = roomList2[n].occupPrice + roomList2[n].BoardBases[j].bbPrice;
                                                        decimal totalamt3 = roomList3[o].occupPrice + roomList3[o].BoardBases[j].bbPrice;
                                                        decimal totalamt4 = roomList4[p].occupPrice + roomList4[p].BoardBases[j].bbPrice;
                                                        decimal totalamt5 = roomList5[q].occupPrice + roomList5[q].BoardBases[j].bbPrice;
                                                        decimal totalamt6 = roomList6[r].occupPrice + roomList6[r].BoardBases[j].bbPrice;
                                                        decimal totalamt7 = roomList7[s].occupPrice + roomList7[s].BoardBases[j].bbPrice;
                                                        decimal totalamt8 = roomList8[t].occupPrice + roomList8[t].BoardBases[j].bbPrice;
                                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8;

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
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPrice)),
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
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPrice)),
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
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPrice)),
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
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPrice)),
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
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
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
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPrice)),
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
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
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
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPrice)),
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
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].PriceBreakdown[0].value)),
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
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPrice)),
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
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList8[t].BoardBases[j].bbPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].PriceBreakdown[0].value)),
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
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), roomList8[t].BoardBases[j].bbPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                         )));
                                                    }
                                                    #endregion
                                                    #region RO
                                                    if (RO == true)
                                                    {
                                                        //int countpaidnight1 = 0;
                                                        //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight1 = countpaidnight1 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight2 = 0;
                                                        //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight2 = countpaidnight2 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight3 = 0;
                                                        //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight3 = countpaidnight3 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight4 = 0;
                                                        //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight4 = countpaidnight4 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight5 = 0;
                                                        //Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList5[q].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight5 = countpaidnight5 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight6 = 0;
                                                        //Parallel.For(0, roomList6[r].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList6[r].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight6 = countpaidnight6 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight7 = 0;
                                                        //Parallel.For(0, roomList7[s].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList7[s].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight7 = countpaidnight7 + 1;
                                                        //    }
                                                        //});
                                                        //int countpaidnight8 = 0;
                                                        //Parallel.For(0, roomList8[t].PriceBreakdown.Count(), jj =>
                                                        //{
                                                        //    if (roomList8[t].PriceBreakdown[jj].value != 0)
                                                        //    {
                                                        //        countpaidnight8 = countpaidnight8 + 1;
                                                        //    }
                                                        //});

                                                        group++;
                                                        decimal totalamt = roomList1[m].occupPrice;
                                                        decimal totalamt2 = roomList2[n].occupPrice;
                                                        decimal totalamt3 = roomList3[o].occupPrice;
                                                        decimal totalamt4 = roomList4[p].occupPrice;
                                                        decimal totalamt5 = roomList5[q].occupPrice;
                                                        decimal totalamt6 = roomList6[r].occupPrice;
                                                        decimal totalamt7 = roomList7[s].occupPrice;
                                                        decimal totalamt8 = roomList8[t].occupPrice;
                                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8;

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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
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
                                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
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
                                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].PriceBreakdown[0].value)),
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
                                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].PriceBreakdown[0].value)),
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
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), 0)),
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
                #region GRP
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
                #endregion
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
                                                for (int u = 0; u < roomList9.Count(); u++)
                                                {
                                                    // add room 1, 2, 3, 4,5,6,7,8,9

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
                                                        decimal totalp = roomList1[m].occupPrice + roomList2[n].occupPrice + roomList3[o].occupPrice + roomList4[p].occupPrice + roomList5[q].occupPrice + roomList6[r].occupPrice + roomList7[s].occupPrice + roomList8[t].occupPrice + roomList9[u].occupPrice;

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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPrice)),
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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPrice)),
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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPrice)),
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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPrice)),
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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPrice)),
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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPrice)),
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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].PriceBreakdown[0].value)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPrice)),
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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].PriceBreakdown[0].value)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList8[t].occupPrice)),
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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList9[u].PriceBreakdown[0].value)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList9[u].occupPrice)),
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
                                                        bool RO = false;
                                                        #region Board Bases >0
                                                        for (int j = 0; j < bb; j++)
                                                        {
                                                            //int countpaidnight1 = 0;
                                                            //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight1 = countpaidnight1 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight2 = 0;
                                                            //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight2 = countpaidnight2 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight3 = 0;
                                                            //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight3 = countpaidnight3 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight4 = 0;
                                                            //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight4 = countpaidnight4 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight5 = 0;
                                                            //Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList5[p].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight5 = countpaidnight5 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight6 = 0;
                                                            //Parallel.For(0, roomList6[r].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList6[r].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight6 = countpaidnight6 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight7 = 0;
                                                            //Parallel.For(0, roomList7[s].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList7[s].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight7 = countpaidnight7 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight8 = 0;
                                                            //Parallel.For(0, roomList8[t].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList8[t].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight8 = countpaidnight8 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight9 = 0;
                                                            //Parallel.For(0, roomList9[u].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList9[u].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight9 = countpaidnight9 + 1;
                                                            //    }
                                                            //});
                                                            if (roomList1[m].BoardBases[j].bbPrice > 0)
                                                            { RO = true; }
                                                            if (roomList2[n].BoardBases[j].bbPrice > 0)
                                                            { RO = true; }
                                                            if (roomList3[o].BoardBases[j].bbPrice > 0)
                                                            { RO = true; }
                                                            if (roomList4[p].BoardBases[j].bbPrice > 0)
                                                            { RO = true; }
                                                            if (roomList5[q].BoardBases[j].bbPrice > 0)
                                                            { RO = true; }
                                                            if (roomList6[r].BoardBases[j].bbPrice > 0)
                                                            { RO = true; }
                                                            if (roomList7[s].BoardBases[j].bbPrice > 0)
                                                            { RO = true; }
                                                            if (roomList8[t].BoardBases[j].bbPrice > 0)
                                                            { RO = true; }
                                                            if (roomList9[u].BoardBases[j].bbPrice > 0)
                                                            { RO = true; }
                                                            group++;
                                                            decimal totalamt = roomList1[m].occupPrice + roomList1[m].BoardBases[j].bbPrice;
                                                            decimal totalamt2 = roomList2[n].occupPrice + roomList2[n].BoardBases[j].bbPrice;
                                                            decimal totalamt3 = roomList3[o].occupPrice + roomList3[o].BoardBases[j].bbPrice;
                                                            decimal totalamt4 = roomList4[p].occupPrice + roomList4[p].BoardBases[j].bbPrice;
                                                            decimal totalamt5 = roomList5[q].occupPrice + roomList5[q].BoardBases[j].bbPrice;
                                                            decimal totalamt6 = roomList6[r].occupPrice + roomList6[r].BoardBases[j].bbPrice;
                                                            decimal totalamt7 = roomList7[s].occupPrice + roomList7[s].BoardBases[j].bbPrice;
                                                            decimal totalamt8 = roomList8[t].occupPrice + roomList8[t].BoardBases[j].bbPrice;
                                                            decimal totalamt9 = roomList9[u].occupPrice + roomList9[u].BoardBases[j].bbPrice;
                                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8 + totalamt9;

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
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPrice)),
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
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPrice)),
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
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPrice)),
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
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPrice)),
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
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
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
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPrice)),
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
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
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
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPrice)),
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
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].PriceBreakdown[0].value)),
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
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPrice)),
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
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList8[t].BoardBases[j].bbPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].PriceBreakdown[0].value)),
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
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), roomList8[t].BoardBases[j].bbPrice)),
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
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList9[u].BoardBases[j].bbPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList9[u].PriceBreakdown[0].value)),
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
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList9[u].PriceBreakdown.ToList(), roomList9[u].BoardBases[j].bbPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList9[u].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList9[u].Rooms[0].ChildNum))
                                                             )));
                                                        }
                                                        #endregion
                                                        #region RO
                                                        if (RO == true)
                                                        {
                                                            //int countpaidnight1 = 0;
                                                            //Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList1[m].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight1 = countpaidnight1 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight2 = 0;
                                                            //Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList2[n].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight2 = countpaidnight2 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight3 = 0;
                                                            //Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList3[o].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight3 = countpaidnight3 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight4 = 0;
                                                            //Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList4[p].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight4 = countpaidnight4 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight5 = 0;
                                                            //Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList5[q].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight5 = countpaidnight5 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight6 = 0;
                                                            //Parallel.For(0, roomList6[r].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList6[r].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight6 = countpaidnight6 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight7 = 0;
                                                            //Parallel.For(0, roomList7[s].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList7[s].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight7 = countpaidnight7 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight8 = 0;
                                                            //Parallel.For(0, roomList8[t].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList8[t].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight8 = countpaidnight8 + 1;
                                                            //    }
                                                            //});
                                                            //int countpaidnight9 = 0;
                                                            //Parallel.For(0, roomList9[u].PriceBreakdown.Count(), jj =>
                                                            //{
                                                            //    if (roomList9[u].PriceBreakdown[jj].value != 0)
                                                            //    {
                                                            //        countpaidnight9 = countpaidnight9 + 1;
                                                            //    }
                                                            //});

                                                            group++;
                                                            decimal totalamt = roomList1[m].occupPrice;
                                                            decimal totalamt2 = roomList2[n].occupPrice;
                                                            decimal totalamt3 = roomList3[o].occupPrice;
                                                            decimal totalamt4 = roomList4[p].occupPrice;
                                                            decimal totalamt5 = roomList5[q].occupPrice;
                                                            decimal totalamt6 = roomList6[r].occupPrice;
                                                            decimal totalamt7 = roomList7[s].occupPrice;
                                                            decimal totalamt8 = roomList8[t].occupPrice;
                                                            decimal totalamt9 = roomList9[u].occupPrice;
                                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8 + totalamt9;

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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].PriceBreakdown[0].value)),
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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].PriceBreakdown[0].value)),
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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].PriceBreakdown[0].value)),
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
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].PriceBreakdown[0].value)),
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
                                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].PriceBreakdown[0].value)),
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
                                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].PriceBreakdown[0].value)),
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
                                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].PriceBreakdown[0].value)),
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
                                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].PriceBreakdown[0].value)),
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
                                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList9[u].PriceBreakdown[0].value)),
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
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList9[u].PriceBreakdown.ToList(), 0)),
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
                    decimal bbprice = roomlist.Occupancies[i].BoardBases[j].bbPrice;
                    int bbid = roomlist.Occupancies[i].BoardBases[j].bbId;
                    string bbname = roomlist.Occupancies[i].BoardBases[j].bbName;

                    decimal totalamt = roomlist.Occupancies[i].occupPrice + bbprice;
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
                         new XAttribute("PerNightRoomRate", Convert.ToString(roomlist.Occupancies[i].avrNightPrice)),
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
            try
            {
                int countpaidnight = 0;
                Parallel.For(0, pricebreakups.Count(), j =>
                {
                    if (pricebreakups[j].value != 0)
                    {
                        countpaidnight = countpaidnight + 1;
                    }
                });
                decimal mealpricepernight = mealprice / countpaidnight;

                Parallel.For(0, pricebreakups.Count(), i =>
                {
                    if (pricebreakups[i].value != 0)
                    {
                        str.Add(new XElement("Price",
                               new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                               new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].value + mealpricepernight)))
                        );
                    }
                    else
                    {
                        str.Add(new XElement("Price",
                              new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                              new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].value)))
                       );
                    }
                });
                return str.OrderBy(x => (int)x.Attribute("Night")).ToList();
            }
            catch { return null; }
            #endregion
        }
        #endregion
        #region Log Write
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


        #region Tourico Hotel's Room Grouping for minimum rate
        public decimal GetHotelMinRateRoomgroupTourico(List<Tourico.RoomType> roomlist, string Hotelcode, string currency)
        {
            decimal minRate = 0;
            int minindx = 0;
            for (int i = 0; i < roomlist.Count(); i++)
            {
                decimal totalrate = GetHotelMinRateTourico(roomlist[i], Hotelcode, currency);
                if (minindx == 0)
                {
                    minRate = totalrate;
                }
                if (totalrate < minRate)
                {
                    minRate = totalrate;
                }
                minindx++;
            }
            return minRate;
        }
        #endregion
        #region Tourico Hotel's Min Rate
        public decimal GetHotelMinRateTourico(Tourico.RoomType roomlist, string Hotelcode, string currency)
        {
            decimal minRate = 0;
            int minindx = 0;
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
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    #region room's group
                    int bb = roomlist.Occupancies[m].BoardBases.Count();
                    if (bb == 0)
                    {
                        group++;
                        decimal totalamt = roomList1[m].occupPublishPrice;
                        if (minindx == 0)
                        {
                            minRate = totalamt;
                        }
                        if (totalamt < minRate)
                        {
                            minRate = totalamt;
                        }
                        minindx++;
                    }
                    else
                    {
                        bool RO = false;
                        #region Board Bases >0
                        for (int j = 0; j < bb; j++)
                        {
                            if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                            { RO = true; }
                            group++;
                            decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                            if (minindx == 0)
                            {
                                minRate = totalamt;
                            }
                            if (totalamt < minRate)
                            {
                                minRate = totalamt;
                            }
                            minindx++;
                        }
                        #region Room Only
                        if (RO == true)
                        {
                            decimal totalamt = roomList1[m].occupPublishPrice;
                            if (minindx == 0)
                            {
                                minRate = totalamt;
                            }
                            if (totalamt < minRate)
                            {
                                minRate = totalamt;
                            }
                            minindx++;
                        }
                        #endregion
                        #endregion
                    }
                    #endregion
                }
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
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        // add room 1, 2
                        #region room's group
                        int bb = roomlist.Occupancies[m].BoardBases.Count();
                        if (bb == 0)
                        {
                            group++;
                            decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice;
                            if (minindx == 0)
                            {
                                minRate = totalp;
                            }
                            if (totalp < minRate)
                            {
                                minRate = totalp;
                            }
                            minindx++;
                        }
                        else
                        {
                            bool RO = false;
                            #region Board Bases >0
                            for (int j = 0; j < bb; j++)
                            {
                                if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                { RO = true; }
                                if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                { RO = true; }
                                group++;
                                decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                decimal totalp = totalamt + totalamt2;
                                if (minindx == 0)
                                {
                                    minRate = totalp;
                                }
                                if (totalp < minRate)
                                {
                                    minRate = totalp;
                                }
                                minindx++;
                            }
                            #region RO
                            if (RO == true)
                            {
                                decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice;
                                if (minindx == 0)
                                {
                                    minRate = totalp;
                                }
                                if (totalp < minRate)
                                {
                                    minRate = totalp;
                                }
                                minindx++;
                            }
                            #endregion
                            #endregion
                        }
                        #endregion
                    }
                }
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
                #region Room 3
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            // add room 1, 2, 3
                            #region room's group
                            int bb = roomlist.Occupancies[m].BoardBases.Count();
                            if (bb == 0)
                            {
                                group++;
                                decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice;
                                if (minindx == 0)
                                {
                                    minRate = totalp;
                                }
                                if (totalp < minRate)
                                {
                                    minRate = totalp;
                                }
                                minindx++;
                            }
                            else
                            {
                                bool RO = false;
                                #region Board Bases >0
                                for (int j = 0; j < bb; j++)
                                {
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
                                    if (minindx == 0)
                                    {
                                        minRate = totalp;
                                    }
                                    if (totalp < minRate)
                                    {
                                        minRate = totalp;
                                    }
                                    minindx++;
                                }
                                #endregion
                                #region RO
                                if (RO == true)
                                {
                                    decimal totalamt = roomList1[m].occupPublishPrice;
                                    decimal totalamt2 = roomList2[n].occupPublishPrice;
                                    decimal totalamt3 = roomList3[o].occupPublishPrice;
                                    decimal totalp = totalamt + totalamt2 + totalamt3;
                                    if (minindx == 0)
                                    {
                                        minRate = totalp;
                                    }
                                    if (totalp < minRate)
                                    {
                                        minRate = totalp;
                                    }
                                    minindx++;
                                }
                                #endregion
                            }
                            #endregion
                        }
                    }
                }
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
                                if (bb == 0)
                                {
                                    group++;
                                    decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice;
                                    if (minindx == 0)
                                    {
                                        minRate = totalp;
                                    }
                                    if (totalp < minRate)
                                    {
                                        minRate = totalp;
                                    }
                                    minindx++;
                                }
                                else
                                {
                                    bool RO = false;
                                    #region Board Bases >0
                                    for (int j = 0; j < bb; j++)
                                    {
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
                                        if (minindx == 0)
                                        {
                                            minRate = totalp;
                                        }
                                        if (totalp < minRate)
                                        {
                                            minRate = totalp;
                                        }
                                        minindx++;
                                    }
                                    #endregion
                                    #region RO
                                    if (RO == true)
                                    {
                                        group++;
                                        decimal totalamt = roomList1[m].occupPublishPrice;
                                        decimal totalamt2 = roomList2[n].occupPublishPrice;
                                        decimal totalamt3 = roomList3[o].occupPublishPrice;
                                        decimal totalamt4 = roomList4[p].occupPublishPrice;
                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4;
                                        if (minindx == 0)
                                        {
                                            minRate = totalp;
                                        }
                                        if (totalp < minRate)
                                        {
                                            minRate = totalp;
                                        }
                                        minindx++;
                                    }
                                    #endregion
                                }
                                #endregion
                            }
                        }
                    }
                }
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
                                    if (bb == 0)
                                    {
                                        group++;
                                        decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice;
                                        if (minindx == 0)
                                        {
                                            minRate = totalp;
                                        }
                                        if (totalp < minRate)
                                        {
                                            minRate = totalp;
                                        }
                                        minindx++;
                                    }
                                    else
                                    {
                                        bool RO = false;
                                        #region Board Bases >0
                                        for (int j = 0; j < bb; j++)
                                        {
                                            if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                            { RO = true; }
                                            if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                            { RO = true; }
                                            if (roomList3[o].BoardBases[j].bbPublishPrice > 0)
                                            { RO = true; }
                                            if (roomList4[p].BoardBases[j].bbPublishPrice > 0)
                                            { RO = true; }
                                            if (roomList5[q].BoardBases[j].bbPublishPrice > 0)
                                            { RO = true; }
                                            group++;
                                            decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                            decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                            decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                            decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                            decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5;
                                            if (minindx == 0)
                                            {
                                                minRate = totalp;
                                            }
                                            if (totalp < minRate)
                                            {
                                                minRate = totalp;
                                            }
                                            minindx++;
                                        }
                                        #endregion
                                        #region RO
                                        if (RO == true)
                                        {
                                            group++;
                                            decimal totalamt = roomList1[m].occupPublishPrice;
                                            decimal totalamt2 = roomList2[n].occupPublishPrice;
                                            decimal totalamt3 = roomList3[o].occupPublishPrice;
                                            decimal totalamt4 = roomList4[p].occupPublishPrice;
                                            decimal totalamt5 = roomList5[q].occupPublishPrice;
                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5;
                                            if (minindx == 0)
                                            {
                                                minRate = totalp;
                                            }
                                            if (totalp < minRate)
                                            {
                                                minRate = totalp;
                                            }
                                            minindx++;
                                        }
                                        #endregion
                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                }
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
                                        if (bb == 0)
                                        {
                                            group++;
                                            decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice;
                                            if (minindx == 0)
                                            {
                                                minRate = totalp;
                                            }
                                            if (totalp < minRate)
                                            {
                                                minRate = totalp;
                                            }
                                            minindx++;
                                        }
                                        else
                                        {
                                            bool RO = false;
                                            #region Board Bases >0
                                            for (int j = 0; j < bb; j++)
                                            {
                                                if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                                { RO = true; }
                                                if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                                { RO = true; }
                                                if (roomList3[o].BoardBases[j].bbPublishPrice > 0)
                                                { RO = true; }
                                                if (roomList4[p].BoardBases[j].bbPublishPrice > 0)
                                                { RO = true; }
                                                if (roomList5[q].BoardBases[j].bbPublishPrice > 0)
                                                { RO = true; }
                                                if (roomList6[r].BoardBases[j].bbPublishPrice > 0)
                                                { RO = true; }
                                                group++;
                                                decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6;
                                                if (minindx == 0)
                                                {
                                                    minRate = totalp;
                                                }
                                                if (totalp < minRate)
                                                {
                                                    minRate = totalp;
                                                }
                                                minindx++;
                                            }
                                            #endregion
                                            #region RO
                                            if (RO == true)
                                            {
                                                group++;
                                                decimal totalamt = roomList1[m].occupPublishPrice;
                                                decimal totalamt2 = roomList2[n].occupPublishPrice;
                                                decimal totalamt3 = roomList3[o].occupPublishPrice;
                                                decimal totalamt4 = roomList4[p].occupPublishPrice;
                                                decimal totalamt5 = roomList5[q].occupPublishPrice;
                                                decimal totalamt6 = roomList6[r].occupPublishPrice;
                                                decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6;
                                                if (minindx == 0)
                                                {
                                                    minRate = totalp;
                                                }
                                                if (totalp < minRate)
                                                {
                                                    minRate = totalp;
                                                }
                                                minindx++;
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
                                            if (bb == 0)
                                            {
                                                group++;
                                                decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice;
                                                if (minindx == 0)
                                                {
                                                    minRate = totalp;
                                                }
                                                if (totalp < minRate)
                                                {
                                                    minRate = totalp;
                                                }
                                                minindx++;
                                            }
                                            else
                                            {
                                                bool RO = false;
                                                #region Board Bases >0
                                                for (int j = 0; j < bb; j++)
                                                {
                                                    if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                                    { RO = true; }
                                                    if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                                    { RO = true; }
                                                    if (roomList3[o].BoardBases[j].bbPublishPrice > 0)
                                                    { RO = true; }
                                                    if (roomList4[p].BoardBases[j].bbPublishPrice > 0)
                                                    { RO = true; }
                                                    if (roomList5[q].BoardBases[j].bbPublishPrice > 0)
                                                    { RO = true; }
                                                    if (roomList6[r].BoardBases[j].bbPublishPrice > 0)
                                                    { RO = true; }
                                                    if (roomList7[s].BoardBases[j].bbPublishPrice > 0)
                                                    { RO = true; }
                                                    group++;
                                                    decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt7 = roomList7[s].occupPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice;
                                                    decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7;
                                                    if (minindx == 0)
                                                    {
                                                        minRate = totalp;
                                                    }
                                                    if (totalp < minRate)
                                                    {
                                                        minRate = totalp;
                                                    }
                                                    minindx++;
                                                }
                                                #endregion
                                                #region RO
                                                if (RO == true)
                                                {
                                                    group++;
                                                    decimal totalamt = roomList1[m].occupPublishPrice;
                                                    decimal totalamt2 = roomList2[n].occupPublishPrice;
                                                    decimal totalamt3 = roomList3[o].occupPublishPrice;
                                                    decimal totalamt4 = roomList4[p].occupPublishPrice;
                                                    decimal totalamt5 = roomList5[q].occupPublishPrice;
                                                    decimal totalamt6 = roomList6[r].occupPublishPrice;
                                                    decimal totalamt7 = roomList7[s].occupPublishPrice;
                                                    decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7;
                                                    if (minindx == 0)
                                                    {
                                                        minRate = totalp;
                                                    }
                                                    if (totalp < minRate)
                                                    {
                                                        minRate = totalp;
                                                    }
                                                    minindx++;
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
                                                if (bb == 0)
                                                {
                                                    group++;
                                                    decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice + roomList8[t].occupPublishPrice;
                                                    if (minindx == 0)
                                                    {
                                                        minRate = totalp;
                                                    }
                                                    if (totalp < minRate)
                                                    {
                                                        minRate = totalp;
                                                    }
                                                    minindx++;
                                                }
                                                else
                                                {
                                                    bool RO = false;
                                                    #region Board Bases >0
                                                    for (int j = 0; j < bb; j++)
                                                    {
                                                        if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                                        { RO = true; }
                                                        if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                                        { RO = true; }
                                                        if (roomList3[o].BoardBases[j].bbPublishPrice > 0)
                                                        { RO = true; }
                                                        if (roomList4[p].BoardBases[j].bbPublishPrice > 0)
                                                        { RO = true; }
                                                        if (roomList5[q].BoardBases[j].bbPublishPrice > 0)
                                                        { RO = true; }
                                                        if (roomList6[r].BoardBases[j].bbPublishPrice > 0)
                                                        { RO = true; }
                                                        if (roomList7[s].BoardBases[j].bbPublishPrice > 0)
                                                        { RO = true; }
                                                        if (roomList8[t].BoardBases[j].bbPublishPrice > 0)
                                                        { RO = true; }
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
                                                        if (minindx == 0)
                                                        {
                                                            minRate = totalp;
                                                        }
                                                        if (totalp < minRate)
                                                        {
                                                            minRate = totalp;
                                                        }
                                                        minindx++;
                                                    }
                                                    #endregion
                                                    #region RO
                                                    if (RO == true)
                                                    {
                                                        group++;
                                                        decimal totalamt = roomList1[m].occupPublishPrice;
                                                        decimal totalamt2 = roomList2[n].occupPublishPrice;
                                                        decimal totalamt3 = roomList3[o].occupPublishPrice;
                                                        decimal totalamt4 = roomList4[p].occupPublishPrice;
                                                        decimal totalamt5 = roomList5[q].occupPublishPrice;
                                                        decimal totalamt6 = roomList6[r].occupPublishPrice;
                                                        decimal totalamt7 = roomList7[s].occupPublishPrice;
                                                        decimal totalamt8 = roomList8[t].occupPublishPrice;
                                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8;
                                                        if (minindx == 0)
                                                        {
                                                            minRate = totalp;
                                                        }
                                                        if (totalp < minRate)
                                                        {
                                                            minRate = totalp;
                                                        }
                                                        minindx++;
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
                                                for (int u = 0; u < roomList9.Count(); u++)
                                                {
                                                    // add room 1, 2, 3, 4, 5, 6, 7, 8, 9
                                                    #region room's group
                                                    int bb = roomlist.Occupancies[m].BoardBases.Count();
                                                    if (bb == 0)
                                                    {
                                                        group++;
                                                        decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice + roomList8[t].occupPublishPrice + roomList9[u].occupPublishPrice;
                                                        if (minindx == 0)
                                                        {
                                                            minRate = totalp;
                                                        }
                                                        if (totalp < minRate)
                                                        {
                                                            minRate = totalp;
                                                        }
                                                        minindx++;
                                                    }
                                                    else
                                                    {
                                                        bool RO = false;
                                                        #region Board Bases >0
                                                        for (int j = 0; j < bb; j++)
                                                        {
                                                            if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                                            { RO = true; }
                                                            if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                                            { RO = true; }
                                                            if (roomList3[o].BoardBases[j].bbPublishPrice > 0)
                                                            { RO = true; }
                                                            if (roomList4[p].BoardBases[j].bbPublishPrice > 0)
                                                            { RO = true; }
                                                            if (roomList5[q].BoardBases[j].bbPublishPrice > 0)
                                                            { RO = true; }
                                                            if (roomList6[r].BoardBases[j].bbPublishPrice > 0)
                                                            { RO = true; }
                                                            if (roomList7[s].BoardBases[j].bbPublishPrice > 0)
                                                            { RO = true; }
                                                            if (roomList8[t].BoardBases[j].bbPublishPrice > 0)
                                                            { RO = true; }
                                                            if (roomList9[u].BoardBases[j].bbPublishPrice > 0)
                                                            { RO = true; }
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
                                                            if (minindx == 0)
                                                            {
                                                                minRate = totalp;
                                                            }
                                                            if (totalp < minRate)
                                                            {
                                                                minRate = totalp;
                                                            }
                                                            minindx++;
                                                        }
                                                        #endregion
                                                        #region RO
                                                        if (RO == true)
                                                        {
                                                            group++;
                                                            decimal totalamt = roomList1[m].occupPublishPrice;
                                                            decimal totalamt2 = roomList2[n].occupPublishPrice;
                                                            decimal totalamt3 = roomList3[o].occupPublishPrice;
                                                            decimal totalamt4 = roomList4[p].occupPublishPrice;
                                                            decimal totalamt5 = roomList5[q].occupPublishPrice;
                                                            decimal totalamt6 = roomList6[r].occupPublishPrice;
                                                            decimal totalamt7 = roomList7[s].occupPublishPrice;
                                                            decimal totalamt8 = roomList8[t].occupPublishPrice;
                                                            decimal totalamt9 = roomList9[u].occupPublishPrice;
                                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8 + totalamt9;
                                                            if (minindx == 0)
                                                            {
                                                                minRate = totalp;
                                                            }
                                                            if (totalp < minRate)
                                                            {
                                                                minRate = totalp;
                                                            }
                                                            minindx++;
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
                #endregion
            }
            #endregion

            return minRate;
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