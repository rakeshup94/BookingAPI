using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml;
using System.Globalization;
using TravillioXMLOutService.Models.Restel;
using TravillioXMLOutService.Models.Juniper;
using System.IO;
using System.Configuration;
using TravillioXMLOutService.Models;
using System.Data;
using System.Threading;
using System.Text;
using TravillioXMLOutService.Common.DotW;
using System.Text.RegularExpressions;

namespace TravillioXMLOutService.Supplier.Juniper
{
    public class JuniperResponses
    {
        //XDocument citymapping = XDocument.Load(Path.Combine(HttpRuntime.AppDomainAppPath, ConfigurationManager.AppSettings["RestelPath"] + @"RestelCityMapping.xml"));          
        //string XmlPath = ConfigurationManager.AppSettings["RestelPath"];
        //string JunpVersion = "1.1";
        int _chunksize = 1000;
        int sup_cutime = 100000, threadCount;
        JuniperOutward serverRequest = new JuniperOutward();
        RestelLogAccess rlac = new RestelLogAccess();
        JuniperData jdata = new JuniperData();
        XNamespace soap = "http://www.juniper.es/webservice/2007/";
        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
        XDocument serverResponse;
        int _JuniperSupplierID;
        string _JNPRSupplierName = "";
        string _JNPRHotSearURL = "";
        string _JNPRHotPreBookURL = "";
        string _JNPRHotBookURL = "";
        string _JNPRHotBookCanURL = "";
        string _JNPRLoginID = "";
        string _JNPRPassword = "";
        string _JNPRVersion = "";
        string dmc = string.Empty, SupplierCurrency = string.Empty, hotelcode = string.Empty;
        int customerid = 0;
        DataTable StaticData_All = new DataTable();
        public string junipercustomer_id = string.Empty;
        public JuniperResponses(int SuppID, int CustID)
        {
            customerid = CustID;
            _JuniperSupplierID = SuppID;
            JuniperConfiguration(SuppID, CustID);
        }
        public JuniperResponses()
        {
        }
        #region Hotel Search
        int StarRating(XElement item)
        {
            int rating = 0;
            try
            {
                if (item != null)
                {
                    var type = item.Attribute("Type").Value;
                    if (!string.IsNullOrEmpty(item.Attribute("Type").Value))
                    {
                        rating = Convert.ToInt16(type.TrimStart().Substring(0, 1));
                    }
                }
                return rating;
            }
            catch { return 0; }
        }
        public List<XElement> ThreadedHotelSearch(XElement Req, string xtype)
        {
            dmc = xtype;
            XElement response = null;
            List<XElement> HotelList = new List<XElement>();
            try
            {
                //DataTable cityMapping = jdata.CityMapping(Req.Descendants("CityID").FirstOrDefault().Value, _JuniperSupplierID.ToString());
                //if (cityMapping.Rows.Count == 0)
                //    return null;
                //string suppliercity = cityMapping.Rows[0]["SupCityId"].ToString();
                //XElement Facilities = HotelFacilities(suppliercity);
                XElement Facilities = new XElement("Facility", "No Facility Available");
                List<string> hotelIDs = new List<string>();
                DataTable citiesWithResult = new DataTable();
                citiesWithResult.Columns.Add("id");
                citiesWithResult.Columns.Add("cityid");
                citiesWithResult.Columns.Add("supcityid");
                citiesWithResult.Columns.Add("supid");
                citiesWithResult.Columns.Add("cntid");
                citiesWithResult.Columns.Add("stateid");
                citiesWithResult.Columns.Add("ctyLclName");
                List<XElement> HotelsForCity = new List<XElement>();
                List<string> citywiseHotels = new List<string>();
                List<XElement> getHotels = new List<XElement>();
                XElement hotelData = new XElement("Cities");
                #region get cut off time
                try
                {
                    sup_cutime = supplier_Cred.secondcutoff_time();
                    threadCount = Convert.ToInt32(ConfigurationManager.AppSettings["JuniperThreads"].ToString());
                }
                catch { }
                #endregion
                string HotelId = Req.Descendants("HotelID").FirstOrDefault().Value;
                string HotelName = Req.Descendants("HotelName").FirstOrDefault().Value;


                hotelData = HotelCodes(Req.Descendants("CityID").FirstOrDefault().Value, _JuniperSupplierID.ToString(), HotelId, HotelName);
                hotelIDs = hotelData.Descendants("Hotel").Select(x => x.Element("HotelID").Value).Distinct().ToList();


                if (hotelIDs.Count == 0)
                {
                    throw new Exception("There is no hotel available in database");
                }

                var chunklist = BreakIntoChunks(hotelIDs.Distinct().ToList(), _chunksize);
                int Number = chunklist.Count;
                List<XElement> tr1 = new List<XElement>();
                List<XElement> tr2 = new List<XElement>();
                List<XElement> tr3 = new List<XElement>();
                List<XElement> tr4 = new List<XElement>();
                List<XElement> tr5 = new List<XElement>();

                int city = Convert.ToInt32(Req.Descendants("CityID").FirstOrDefault().Value);
                StaticData_All = jdata.GetJuniperHotelImage(_JuniperSupplierID, city);

                int timeOut = sup_cutime;
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                timer.Start();
                for (int i = 0; i < Number; i += threadCount)
                {
                    List<Thread> threadedlist;
                    int rangecount = threadCount;
                    if (chunklist.Count - i < threadCount)
                        rangecount = chunklist.Count - i;
                    var chn = chunklist.GetRange(i, rangecount);

                    #region rangecount equals 1
                    if (rangecount == 1)
                    {
                        threadedlist = new List<Thread>
                       {   
                           new Thread(()=> tr1 = HotelSearch(Req, chunklist.ElementAt(i),citiesWithResult,HotelsForCity, hotelData))
                       };
                        threadedlist.ForEach(t => t.Start());
                        threadedlist.ForEach(t => t.Join(timeOut));
                        threadedlist.ForEach(t => t.Abort());
                        #region Add to list
                        foreach (XElement hotel in tr1)
                            HotelList.Add(hotel);
                        #endregion

                    }
                    #endregion
                    #region rangecount equals 2
                    else if (rangecount == 2)
                    {
                        threadedlist = new List<Thread>
                       {   
                           new Thread(()=> tr1 = HotelSearch(Req, chunklist.ElementAt(i),citiesWithResult,HotelsForCity, hotelData)),
                           new Thread(()=> tr2 = HotelSearch(Req, chunklist.ElementAt(i+1),citiesWithResult,HotelsForCity, hotelData))
                       };
                        threadedlist.ForEach(t => t.Start());
                        threadedlist.ForEach(t => t.Join(timeOut));
                        threadedlist.ForEach(t => t.Abort());
                        #region Add to List
                        foreach (XElement hotel in tr1)
                            HotelList.Add(hotel);
                        foreach (XElement hotel in tr2)
                            HotelList.Add(hotel);
                        #endregion
                    }
                    #endregion
                    #region rangecount equals 3
                    else if (rangecount == 3)
                    {
                        threadedlist = new List<Thread>
                       {   
                           new Thread(()=> tr1 = HotelSearch(Req, chunklist.ElementAt(i),citiesWithResult,HotelsForCity, hotelData)),
                           new Thread(()=> tr2 = HotelSearch(Req, chunklist.ElementAt(i+1),citiesWithResult,HotelsForCity, hotelData)),
                           new Thread(()=> tr3 = HotelSearch(Req, chunklist.ElementAt(i+2),citiesWithResult,HotelsForCity, hotelData))
                       };
                        threadedlist.ForEach(t => t.Start());
                        threadedlist.ForEach(t => t.Join(timeOut));
                        threadedlist.ForEach(t => t.Abort());
                        #region Add to List
                        foreach (XElement hotel in tr1)
                            HotelList.Add(hotel);
                        foreach (XElement hotel in tr2)
                            HotelList.Add(hotel);
                        foreach (XElement hotel in tr3)
                            HotelList.Add(hotel);
                        #endregion
                    }
                    #endregion
                    #region rangecount equals 4
                    else if (rangecount == 4)
                    {
                        threadedlist = new List<Thread>
                       {

                           new Thread(()=> tr1 = HotelSearch(Req, chunklist.ElementAt(i),citiesWithResult,HotelsForCity, hotelData)),
                           new Thread(()=> tr2 = HotelSearch(Req, chunklist.ElementAt(i+1),citiesWithResult,HotelsForCity, hotelData)),
                           new Thread(()=> tr3 = HotelSearch(Req, chunklist.ElementAt(i+2),citiesWithResult,HotelsForCity, hotelData)),
                           new Thread(()=> tr4 = HotelSearch(Req, chunklist.ElementAt(i+3),citiesWithResult,HotelsForCity, hotelData))

                       };
                        threadedlist.ForEach(t => t.Start());
                        threadedlist.ForEach(t => t.Join(timeOut));
                        threadedlist.ForEach(t => t.Abort());
                        #region Add to List
                        foreach (XElement hotel in tr1)
                            HotelList.Add(hotel);
                        foreach (XElement hotel in tr2)
                            HotelList.Add(hotel);
                        foreach (XElement hotel in tr3)
                            HotelList.Add(hotel);
                        foreach (XElement hotel in tr4)
                            HotelList.Add(hotel);
                        #endregion
                    }
                    #endregion
                    #region rangecount equals 5
                    else if (rangecount == 5)
                    {
                        threadedlist = new List<Thread>
                       {

                           new Thread(()=> tr1 = HotelSearch(Req, chunklist.ElementAt(i),citiesWithResult,HotelsForCity, hotelData)),
                           new Thread(()=> tr2 = HotelSearch(Req, chunklist.ElementAt(i+1),citiesWithResult,HotelsForCity, hotelData)),
                           new Thread(()=> tr3 = HotelSearch(Req, chunklist.ElementAt(i+2),citiesWithResult,HotelsForCity, hotelData)),
                           new Thread(()=> tr4 = HotelSearch(Req, chunklist.ElementAt(i+3),citiesWithResult,HotelsForCity, hotelData)),
                           new Thread(()=> tr5 = HotelSearch(Req, chunklist.ElementAt(i+4),citiesWithResult,HotelsForCity, hotelData))
                       };
                        threadedlist.ForEach(t => t.Start());
                        threadedlist.ForEach(t => t.Join(timeOut));
                        threadedlist.ForEach(t => t.Abort());
                        #region Add to List
                        foreach (XElement hotel in tr1)
                            HotelList.Add(hotel);
                        foreach (XElement hotel in tr2)
                            HotelList.Add(hotel);
                        foreach (XElement hotel in tr3)
                            HotelList.Add(hotel);
                        foreach (XElement hotel in tr4)
                            HotelList.Add(hotel);
                        foreach (XElement hotel in tr5)
                            HotelList.Add(hotel);
                        #endregion
                    }
                    #endregion
                    timeOut = timeOut - Convert.ToInt32(timer.ElapsedMilliseconds);

                }
                removetags(Req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "ThreadedHotelSearch";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = Convert.ToString(customerid);
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                return null;
                #endregion
            }
            return HotelList;
        }
        public List<XElement> HotelSearch(XElement Req, List<string> chunkthread, DataTable CitiesWithHotels, List<XElement> HBECodes, XElement hotelData)
        {
            List<XElement> ThreadResult = new List<XElement>();
            try
            {

                #region Request XML
                XDocument HotelsearchRequest = new XDocument(
                                  new XDeclaration("1.0", "utf-8", "yes"),
                                  new XElement(soapenv + "Envelope",
                                      new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                      new XAttribute(XNamespace.None + "xmlns", soap),
                                      new XElement(soapenv + "Header"),
                                      new XElement(soapenv + "Body",
                                  new XElement(soap + "HotelAvail",
                                      new XElement(soap + "HotelAvailRQ",
                                       new XAttribute("Version", _JNPRVersion),
                                       new XAttribute("Language", "en"),
                                       new XElement(soap + "Login",
                                            new XAttribute("Email", _JNPRLoginID),
                                            new XAttribute("Password", _JNPRPassword)),
                                            new XElement(soap + "Paxes", (bindpax(Req.Descendants("RoomPax").ToList()))),
                                      new XElement(soap + "HotelRequest",
                                           new XElement(soap + "SearchSegmentsHotels",
                                           new XElement(soap + "SearchSegmentHotels",
                                                new XAttribute("Start", JuniperDate(Req.Descendants("FromDate").First().Value)),
                                                new XAttribute("End", JuniperDate(Req.Descendants("ToDate").First().Value))),
                                                 new XElement(soap + "CountryOfResidence", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                                 new XElement(soap + "PackageContracts", "Package"),
                                                 BindHotelCodes(chunkthread)),
                                                      new XElement(soap + "RelPaxesDist", bindRomPax(Req.Descendants("RoomPax").ToList()))),
                                                      new XElement(soap + "AdvancedOptions",
                                                          new XElement(soap + "UseCurrency", "USD"),
                                                          new XElement(soap + "ShowHotelInfo", "true"),
                                                          new XElement(soap + "ShowBreakdownPrice", "true"),
                                                           new XElement(soap + "ShowOnlyAvailable", "true"),
                                                            new XElement(soap + "ShowCancellationPolicies", "true")))))));
                #endregion
                XElement response = HotelSearchResponse(HotelsearchRequest, Req);

                if (response != null)
                {
                    foreach (XElement hotel in response.Descendants("Hotel"))
                        ThreadResult.Add(hotel);
                }


            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelSearch";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = Req.Descendants("CustomerID").First().Value;
                ex1.TranID = Req.Descendants("TransID").First().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
                #endregion
            }
            return ThreadResult;

        }
        #region Response

        public static void WriteToFile(string text)
        {
            string strPath = @"App_Data\List.txt";
            string Filepath = Path.Combine(HttpRuntime.AppDomainAppPath, strPath);
            string path = Filepath;
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                writer.WriteLine("---------------------------Hotel Availability Response-----------------------------------------");
                writer.Close();
            }
        }

        public XElement HotelSearchResponse(XDocument toRequest, XElement travReq)
        {
            DateTime starttime = DateTime.Now;
            XElement response = null;
            try
            {
                serverResponse = serverRequest.JuniperResponse(toRequest, "HotelAvail", _JNPRHotSearURL, Convert.ToInt64(customerid), Convert.ToString(travReq.Descendants("TransID").FirstOrDefault().Value), _JuniperSupplierID);
                #region Log Save
                XElement responseRNS = removeAllNamespaces(serverResponse.Root);
                XElement requestRNS = removeAllNamespaces(toRequest.Root);

                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(customerid);
                log.TrackNumber = travReq.Descendants("TransID").FirstOrDefault().Value;
                log.SupplierID = _JuniperSupplierID;
                log.logrequestXML = requestRNS.ToString();
                log.logresponseXML = responseRNS.ToString();
                log.LogType = "Search";
                log.LogTypeID = 1;
                log.StartTime = starttime;
                log.EndTime = DateTime.Now;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);

                #endregion
                List<XElement> Hotels = new List<XElement>();
                #region Response XML

                //int countSHotel = responseRNS.Descendants("HotelResult").Count();    
                //string str = Thread.CurrentThread.ManagedThreadId.ToString() + "-H-" + countSHotel.ToString();
                //WriteToFile(str);

                if (responseRNS.Descendants("HotelResult").Count() > 0)
                {
                    string cityID = responseRNS.Descendants("HotelResult").FirstOrDefault().Attribute("DestinationZone").Value;
                    //DataTable StaticData_All = new DataTable();
                    string xmlouttype = string.Empty;
                    if (dmc == "W2M" || dmc == "EgyptExpress" || dmc == "AlphaTours" || dmc == "LOH" || dmc == "LCI")
                    {
                        xmlouttype = "false";
                    }
                    else
                    {
                        xmlouttype = "true";
                    }
                    //int city = Convert.ToInt32(travReq.Descendants("CityID").FirstOrDefault().Value);
                    //StaticData_All = jdata.GetJuniperHotelImage(_JuniperSupplierID, city);
                    var result = from ResHotels in responseRNS.Descendants("HotelResult")
                                 join t in StaticData_All.AsEnumerable() on ResHotels.Attribute("Code").Value equals t.Field<string>("HotelID")
                                 into ps
                                 from htl in ps.DefaultIfEmpty()
                                 select new XElement("Hotel",
                                                     new XElement("HotelID", ResHotels.Attribute("Code").Value),
                                                     new XElement("HotelName", ResHotels.Element("HotelInfo").Element("Name").Value),
                                                     new XElement("PropertyTypeName"),
                                                     new XElement("CountryID", travReq.Descendants("CountryID").First().Value),
                                                     new XElement("CountryName", travReq.Descendants("CountryName").First().Value),
                                                     new XElement("CountryCode", travReq.Descendants("CountryCode").First().Value),
                                                     new XElement("CityId", travReq.Descendants("CityID").First().Value),
                                                     new XElement("CityCode", travReq.Descendants("CityCode").First().Value),
                                                     new XElement("CityName", travReq.Descendants("CityName").First().Value),
                                                     new XElement("AreaId"),
                                                     new XElement("AreaName", ""),
                                                     new XElement("RequestID", ResHotels.Attribute("Code").Value),
                                                     new XElement("Address", ResHotels.Element("HotelInfo").Element("Address").Value),
                                                     new XElement("Location"),
                                                     new XElement("Description"),
                                                     new XElement("StarRating", StarRating(ResHotels.Element("HotelInfo").Element("HotelCategory"))),
                                                     new XElement("MinRate", MinRate(ResHotels.Descendants("HotelOptions").First())),
                                                     new XElement("HotelImgSmall",
                                                         htl != null ? htl.Field<string>("ImageURL") : string.Empty),
                                                     new XElement("HotelImgLarge",
                                                         htl != null ? htl.Field<string>("ImageURL") : string.Empty),
                                                     new XElement("MapLink"),
                                                     new XElement("Longitude", ResHotels.Element("HotelInfo").Element("Longitude") == null ? "" : ResHotels.Element("HotelInfo").Element("Longitude").Value),
                                                     new XElement("Latitude", ResHotels.Element("HotelInfo").Element("Latitude") == null ? "" : ResHotels.Element("HotelInfo").Element("Latitude").Value),
                                                     new XElement("xmloutcustid", customerid),
                                                     new XElement("xmlouttype", xmlouttype),
                                                     new XElement("DMC", dmc),
                                                     new XElement("SupplierID", _JuniperSupplierID.ToString()),
                                                     new XElement("Currency", ResHotels.Descendants("Price").First().Attribute("Currency").Value),
                                                     new XElement("Offers"),
                                                     new XElement("Facilities", null),
                                                     new XElement("Rooms"));
                    Hotels.AddRange(result.ToList());
                }
                #endregion

                //int countHotel = Hotels.Count;
                //string str1 = Thread.CurrentThread.ManagedThreadId.ToString() + "-HPost-" + countHotel.ToString();
                //WriteToFile(str1);

                response = new XElement("searchResponse", new XElement("Hotels", Hotels));
                if (response.Descendants("HotelID").Any())
                    response.Descendants("Hotel").Where(x => x.DescendantNodes().Count() == 0).Remove();

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelSearchResponse";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = Convert.ToString(customerid);
                ex1.TranID = travReq.Descendants("TransID").First().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            return response;
        }
        #endregion
        #endregion
        #region Room Availability New
        #region Commented
        //public XElement GetRoomAvail_juniperOUT(XElement req,string supid)
        //{
        //    List<XElement> roomavailabilityresponse = new List<XElement>();
        //    XElement getrm = null;
        //    try
        //    {
        //        #region changed
        //        string dmc = string.Empty;
        //        List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == supid).ToList();
        //        for (int i = 0; i < htlele.Count(); i++)
        //        {
        //            string custID = string.Empty;
        //            string custName = string.Empty;
        //            string htlid = htlele[i].Attribute("GHtlID").Value;
        //            string xmlout = string.Empty;
        //            try
        //            {
        //                xmlout = htlele[i].Attribute("xmlout").Value;
        //            }
        //            catch { xmlout = "false"; }
        //            if (xmlout == "true")
        //            {
        //                try
        //                {
        //                    junipercustomer_id = htlele[i].Attribute("custID").Value;
        //                    dmc = htlele[i].Attribute("custName").Value;
        //                }
        //                catch { custName = "HA"; }
        //            }
        //            else
        //            {
        //                try
        //                {
        //                    junipercustomer_id = htlele[i].Attribute("custID").Value;
        //                }
        //                catch { }
        //                if(supid=="16")
        //                {
        //                    dmc = "W2M";
        //                }
        //                else if (supid == "17")
        //                {
        //                    dmc = "EgyptExpress";
        //                }
        //                else if (supid == "23")
        //                {
        //                    dmc = "LOH";
        //                }
        //                else if (supid == "41")
        //                {
        //                    dmc = "AlphaTours";
        //                }

        //            }
        //            int customerid = Convert.ToInt32(junipercustomer_id);
        //            JuniperResponses rs = new JuniperResponses(Convert.ToInt32(supid), customerid);
        //            roomavailabilityresponse.Add(rs.RoomAvailability_juniper(req, dmc, supid));                    
        //        }
        //        #endregion
        //        getrm = new XElement("TotalJupRooms", roomavailabilityresponse);
        //        return getrm;
        //    }
        //    catch { return null; }
        //}
        #endregion
        public XElement RoomAvailability_juniper(XElement req, string supid)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            XElement getrm = null;
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == supid).ToList();
                for (int i = 0; i < htlele.Count(); i++)
                {
                    string custID = string.Empty;
                    string custName = string.Empty;
                    string htlid = htlele[i].Attribute("GHtlID").Value;
                    string xmlout = string.Empty;
                    try
                    {
                        xmlout = htlele[i].Attribute("xmlout").Value;
                    }
                    catch { xmlout = "false"; }
                    if (xmlout == "true")
                    {
                        try
                        {
                            junipercustomer_id = htlele[i].Attribute("custID").Value;
                            dmc = htlele[i].Attribute("custName").Value;
                        }
                        catch { custName = "HA"; }
                    }
                    else
                    {
                        try
                        {
                            junipercustomer_id = htlele[i].Attribute("custID").Value;
                        }
                        catch { }
                        if (supid == "16")
                        {
                            dmc = "W2M";
                        }
                        else if (supid == "17")
                        {
                            dmc = "EgyptExpress";
                        }
                        else if (supid == "23")
                        {
                            dmc = "LOH";
                        }
                        else if (supid == "31")
                        {
                            dmc = "GADOU";
                        }
                        else if (supid == "35")
                        {
                            dmc = "LCI";
                        }
                        else if (supid == "41")
                        {
                            dmc = "AlphaTours";
                        }
                    }
                    string reqid = htlele[i].Attribute("GRequestID").Value;
                    JuniperResponses rs = new JuniperResponses(Convert.ToInt32(supid), Convert.ToInt32(junipercustomer_id));

                    roomavailabilityresponse.Add(rs.RoomAvailability(req, dmc, htlid, reqid, junipercustomer_id));
                }
                #endregion
                getrm = new XElement("TotalRooms", roomavailabilityresponse);
                return getrm;
            }
            catch { return null; }
        }
        #endregion
        #region Room Availability
        public XElement RoomAvailability(XElement Req, string xtype, string htlcode, string reqid, string customerid)
        {
            try
            {
                junipercustomer_id = customerid;
                dmc = xtype;
                hotelcode = htlcode;
                DateTime starttime = DateTime.Now;
                string hid = htlcode;
                string[] splitid = hid.Split(new char[] { '_' });
                #region Credentials
                string username = Req.Descendants("UserName").Single().Value;
                string password = Req.Descendants("Password").Single().Value;
                string AgentID = Req.Descendants("AgentID").Single().Value;
                string ServiceType = Req.Descendants("ServiceType").Single().Value;
                string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
                #endregion
                XElement RoomResponse = new XElement("searchResponse");
                try
                {
                    List<XElement> respon = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 1, _JuniperSupplierID);
                    XElement resp = respon.Descendants("Response").Where(x => x.Descendants("HotelResult").Where(y => y.Attribute("Code").Value.Equals(reqid)).Any()).FirstOrDefault().Descendants("HotelResult")
                                    .Where(x => x.Attributes("Code").First().Value.Equals(reqid))
                                    .First();

                    //Req = XElement.Load(@"C:\Users\Aman\Desktop\EGYpt_req.xml");
                    //resp = XElement.Load(@"C:\Users\Aman\Desktop\Test.xml");
                    #region Log Save
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(junipercustomer_id);
                        log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                        log.SupplierID = _JuniperSupplierID;
                        log.logrequestXML = null;
                        log.logresponseXML = resp.ToString();
                        log.LogTypeID = 2;
                        log.LogType = "RoomAvail";
                        log.StartTime = starttime;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "RoomAvailability";
                        ex1.PageName = "JuniperResponses";
                        ex1.CustomerID = junipercustomer_id;
                        ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    #endregion
                    if (resp != null)
                    {
                        string hotelstar = resp.Descendants("HotelCategory").First().Value.TrimStart().Substring(0, 1);
                        string hotelID = string.Empty;
                        hotelID = resp.Attribute("Code").Value;




                        //if (_JuniperSupplierID == 41)
                        //    hotelID = reqid;
                        //else
                        //    hotelID = resp.Attribute("Code").Value;

                        #region Response XML
                        SupplierCurrency = resp.Descendants("Price").First().Attribute("Currency").Value;
                        var availableRooms = new XElement("Hotel",
                                                       new XElement("HotelID", hotelID),
                                                       new XElement("HotelName", resp.Element("HotelInfo").Element("Name").Value),
                                                       new XElement("PropertyTypeName"),
                            //new XElement("CountryID", Req.Descendants("CountryID").First().Value),
                            //new XElement("CountryCode"),
                            //new XElement("CountryName", Req.Descendants("CountryName").First().Value),
                            //new XElement("CityId", Req.Descendants("CityID").First().Value),
                            //new XElement("CityCode", Req.Descendants("CityCode").First().Value),
                            //new XElement("CityName", Req.Descendants("CityName").First().Value),
                            //new XElement("AreaName"),
                            //new XElement("AreaId"),
                            //new XElement("Address", resp.Descendants("Address").First().Value),
                            //new XElement("Location"),
                            //new XElement("Description"),
                            //new XElement("StarRating", hotelstar),
                            //new XElement("MinRate"),
                            //new XElement("HotelImgSmall", resp.Descendants("Image").Any() ? resp.Descendants("Image").First().Value : null),
                            //new XElement("HotelImgLarge", resp.Descendants("Image").Any() ? resp.Descendants("Image").First().Value : null),
                            //new XElement("MapLink"),
                            //new XElement("Longitude", ""),
                            //new XElement("Latitude", ""),
                                                       new XElement("DMC", dmc),
                                                       new XElement("SupplierID", _JuniperSupplierID.ToString()),
                                                       new XElement("Currency", resp.Descendants("Price").First().Attribute("Currency").Value),
                                                      new XElement("Offers"),
                                                       new XElement("Facilities", null),
                                                       groupedRooms(resp.Descendants("HotelOptions").First(), Req, reqid));
                        #endregion;
                        RoomResponse.Add(new XElement("Hotels", availableRooms));
                    }
                    #region Response Format
                    removetags(Req);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "RoomAvailability";
                    ex1.PageName = "JuniperResponses";
                    ex1.CustomerID = Req.Descendants("CustomerID").First().Value;
                    ex1.TranID = Req.Descendants("TransID").First().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                XElement AvailablilityResponse = new XElement(
                                                    new XElement(soapenv + "Envelope",
                                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                        new XElement(soapenv + "Header",
                                                            new XElement("Authentication",
                                                                new XElement("AgentID", AgentID),
                                                                new XElement("Username", username),
                                                                new XElement("Password", password),
                                                                new XElement("ServiceType", ServiceType),
                                                                new XElement("ServiceVersion", ServiceVersion))),
                                                        new XElement(soapenv + "Body",
                                                            new XElement(removeAllNamespaces(Req.Descendants("searchRequest").First())),
                                                           removeAllNamespaces(RoomResponse))));
                    #endregion
                return AvailablilityResponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "RoomAvailability";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = Req.Descendants("CustomerID").Single().Value;
                ex1.TranID = Req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        #endregion
        #region Hotel Details
        public XElement hoteldetails(XElement req)
        {
            XElement response = null;
            #region Credentials
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            #endregion
            try
            {
                #region Request XML

                XDocument restelreq = null;
                #endregion
                response = HotelDetailsResponse(restelreq, req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "hoteldetails";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            #region Response Format
            XElement details = new XElement(
                                                new XElement(soapenv + "Envelope",
                                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                    new XElement(soapenv + "Header",
                                                        new XElement("Authentication",
                                                            new XElement("AgentID", AgentID),
                                                            new XElement("Username", username),
                                                            new XElement("Password", password),
                                                            new XElement("ServiceType", ServiceType),
                                                            new XElement("ServiceVersion", ServiceVersion))),
                                                    new XElement(soapenv + "Body",
                                                        new XElement(req.Descendants("hoteldescRequest").First()),
                                                       response)));
            #endregion
            return details;
        }
        #region Response
        public XElement HotelDetailsResponse(XDocument supplierReq, XElement travayooReq)
        {
            XElement response = null;
            //DataTable cityMapping = jdata.CityMapping(travayooReq.Descendants("CityID").FirstOrDefault().Value, _JuniperSupplierID.ToString());
            // string SupplierCityID = cityMapping.Rows[0]["SupCityId"].ToString();
            string hid = travayooReq.Descendants("HotelID").FirstOrDefault().Value;
            string[] splitid = hid.Split(new char[] { '_' });
            string hotelID = string.Empty, Supl_ID = _JuniperSupplierID.ToString();

            hotelID = hid;


            //if (_JuniperSupplierID == 41)
            //    hotelID = travayooReq.Descendants("RequestID").FirstOrDefault().Value;
            //else
            //    hotelID = hid;

            DataTable Details = jdata.GetJuniperSingleHotelDetails(hotelID, Supl_ID);
            //var result = Details.AsEnumerable().Where(dt => dt.Field<string>("HotelID") == hotelID);
            //DataRow[] drow = result.ToArray();
            if (Details.Rows.Count > 0)
            {
                DataRow dr = Details.Rows[0];
                XElement ima = new XElement("Images"), Facility = new XElement("Facilities");
                #region Images
                //if (!String.IsNullOrEmpty(dr["Images"].ToString()))
                //    ima = XElement.Parse(dr["Images"].ToString()); 
                DataTable imagesFromDB = jdata.GetJuniperSingleHotelImages(hotelID, Supl_ID);

                for (int i = 0; i < imagesFromDB.Rows.Count; i++)
                {
                    if (imagesFromDB.Rows[i]["ImageType"].ToString().ToUpper().Equals("BIG"))
                        ima.Add(new XElement("Image", new XAttribute("Path", imagesFromDB.Rows[i]["ImageURL"]), new XAttribute("Caption", "")));
                }
                if (!ima.HasElements)
                {

                    ima.Add(new XElement("Image",
                                    new XAttribute("Path", ""),
                                    new XAttribute("Caption", "")));
                }
                #endregion
                #region Facilities
                DataTable FacilitiesFromDB = jdata.GetJuniperHotelFacility(hotelID, Supl_ID);
                for (int i = 0; i < FacilitiesFromDB.Rows.Count; i++)
                {
                    Facility.Add(new XElement("Facility", FacilitiesFromDB.Rows[i]["Facility"].ToString()));
                }
                if (!Facility.HasElements)
                {
                    Facility.Add(new XElement("Facility", "No Facilities Listed"));
                }
                #endregion
                #region Response XML
                var hotels = new XElement("Hotels",
                                  new XElement("Hotel",
                                      new XElement("HotelID", travayooReq.Descendants("HotelID").First().Value),
                                      new XElement("Description", dr["Description"].ToString()),
                                      ima,
                                      Facility,
                                      new XElement("ContactDetails",
                                          new XElement("Phone", dr["PhoneNumber"].ToString()),
                                          new XElement("Fax", dr["Fax"].ToString())),
                                      new XElement("CheckinTime"),
                                      new XElement("CheckoutTime")
                                      ));
                #endregion
                response = new XElement("hoteldescResponse", hotels);
            }
            return response;
        }
        #endregion
        #endregion
        #region Cancellation Policy Service
        /// <summary>
        /// This region  is  created by user Rakesh
        /// </summary>  
        #region CxlPolicy
        public XElement cancellationPolicy(XElement Req)
        {

            try
            {

                DateTime starttime = DateTime.Now;
                XElement cancelresponse = new XElement("HotelDetailwithcancellationResponse");

                string hid = Req.Descendants("RoomTypes").Attributes("HtlCode").FirstOrDefault().Value;
                string[] splitid = hid.Split(new char[] { '_' });
                string hotelID = string.Empty;
                if (_JuniperSupplierID == 41)
                    hotelID = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                else
                    hotelID = hid;

                XElement supplierResponse = LogXMLs(Req.Descendants("TransID").First().Value, 1, _JuniperSupplierID).Descendants("Response")
                                    .Where(x => x.Descendants("HotelResult").Where(y => y.Attribute("Code").Value.Equals(hotelID)).Any()).First().Descendants("HotelResult")
                                    .Where(x => x.Attribute("Code").Value.Equals(hotelID))
                                    .First();
                string[] RatePlanCode = Req.Descendants("Room").First().Attribute("ID").Value.Split(new char[] { '_' });
                XElement HotelOption = supplierResponse.Descendants("HotelOption").Where(x => x.Attribute("RatePlanCode").Value.Equals(RatePlanCode[0])).First();
                var itemmm = new XElement("RoomData", HotelOption);
                #region Log Save
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                log.SupplierID = _JuniperSupplierID;
                log.logresponseXML = HotelOption.ToString();
                log.LogTypeID = 3;
                log.LogType = "CXLPolicy";
                log.StartTime = starttime;
                log.EndTime = DateTime.Now;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion

                XElement tryRoom = PreBookRooms(itemmm, Req);

                var cxlRoom = newCxlPolicy(HotelOption, tryRoom.Descendants("Price").ToList(), Req.Descendants("FromDate").FirstOrDefault().Value);
                XElement _travyoResp;
                _travyoResp = new XElement("HotelDetailwithcancellationResponse",
                    new XElement("Hotels",
                        new XElement("Hotel",
                            new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                            new XElement("HotelName", Req.Descendants("HotelName").First().Value),
                            new XElement("HotelImgSmall", null),
                            new XElement("HotelImgLarge", null),
                            new XElement("MapLink", null),
                            new XElement("DMC", _JNPRSupplierName),
                            new XElement("Currency"),
                            new XElement("Offers"),
                            new XElement("Rooms",
                                new XElement("Room",
                                new XAttribute("ID", ""),
                                new XAttribute("RoomType", ""),
                                new XAttribute("MealPlanPrice", ""),
                                new XAttribute("PerNightRoomRate", ""),
                                new XAttribute("TotalRoomRate", ""),
                                new XAttribute("CancellationDate", ""), cxlRoom)))));


                XElement SearReq = Req.Descendants("hotelcancelpolicyrequest").FirstOrDefault();
                SearReq.AddAfterSelf(_travyoResp);
                log.logresponseXML = _travyoResp.ToString();
                return Req;



            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "cancellationPolicy";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = Req.Descendants("CustomerID").First().Value;
                ex1.TranID = Req.Descendants("TransID").First().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
                XElement SearReq = Req.Descendants("hotelcancelpolicyrequest").FirstOrDefault();
                SearReq.AddAfterSelf(new XElement("HotelDetailwithcancellationResponse", new XElement("ErrorTxt", "No detail found")));
            }



            return Req;
        }
        #endregion
        public XElement cancellationPolicy_old(XElement Req)
        {
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            XElement response = null;
            string[] splitID = Req.Descendants("Room").First().Attribute("ID").Value.Split(new char[] { '_' });
            //string hid = Req.Descendants("HotelID").FirstOrDefault().Value;
            string hid = Req.Descendants("RoomTypes").Attributes("HtlCode").FirstOrDefault().Value;
            string[] splithid = hid.Split(new char[] { '_' });
            string HotelCode = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
            XElement advancedOptions = null;
            if (_JuniperSupplierID == 16)
                advancedOptions = new XElement(soap + "UseCurrency", "USD");
            try
            {

                #region Request XML
                //  XDocument BookingRulesRequest = null;
                XDocument BookingRulesRequest = new XDocument(
                                new XDeclaration("1.0", "utf-8", "yes"),
                                new XElement(soapenv + "Envelope",
                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                    new XAttribute(XNamespace.None + "xmlns", soap),
                                    new XElement(soapenv + "Header"),
                                    new XElement(soapenv + "Body",
                                    new XElement(soap + "HotelBookingRules",
                                     new XElement(soap + "HotelBookingRulesRQ",
                                     new XAttribute("Version", _JNPRVersion),
                                     new XAttribute("Language", "en"),
                                     new XElement(soap + "Login",
                                          new XAttribute("Email", _JNPRLoginID),
                                          new XAttribute("Password", _JNPRPassword)),
                                    new XElement(soap + "HotelBookingRulesRequest",
                                         new XElement(soap + "HotelOption",
                                         new XAttribute("RatePlanCode", splitID[0])),
                                         new XElement(soap + "SearchSegmentsHotels",
                                         new XElement(soap + "SearchSegmentHotels",
                                              new XAttribute("Start", JuniperDate(Req.Descendants("FromDate").First().Value)),
                                              new XAttribute("End", JuniperDate(Req.Descendants("ToDate").First().Value))),
                                               new XElement(soap + "HotelCodes",
                                               new XElement(soap + "HotelCode", HotelCode)))),
                                                new XElement(soap + "AdvancedOptions",
                                                     new XElement(soap + "ShowBreakdownPrice", "true"),
                                                     advancedOptions,
                                                           new XElement(soap + "ShowCompleteInfo", "true")))))));


                #endregion
                response = cancellationPolicyResponse(BookingRulesRequest, Req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "cancellationPolicy";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = Req.Descendants("CustomerID").First().Value;
                ex1.TranID = Req.Descendants("TransID").First().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #region Response Format

            XElement CXpResponse = new XElement(
                                                new XElement(soapenv + "Envelope",
                                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                    new XElement(soapenv + "Header",
                                                        new XElement("Authentication",
                                                            new XElement("AgentID", AgentID),
                                                            new XElement("Username", username),
                                                            new XElement("Password", password),
                                                            new XElement("ServiceType", ServiceType),
                                                            new XElement("ServiceVersion", ServiceVersion))),
                                                    new XElement(soapenv + "Body",
                                                        new XElement(Req.Descendants("hotelcancelpolicyrequest").Any() ? removeAllNamespaces(Req.Descendants("hotelcancelpolicyrequest").First()) : removeAllNamespaces(Req)),
                                                       removeAllNamespaces(response))));
            #endregion
            return CXpResponse;
        }
        #region Cancellation Policy Response
        public XElement cancellationPolicyResponse(XDocument Request, XElement travReq)
        {

            DateTime starttime = DateTime.Now;
            XElement cancelresponse = new XElement("HotelDetailwithcancellationResponse");

            string hid = travReq.Descendants("RoomTypes").Attributes("HtlCode").FirstOrDefault().Value;
            string[] splitid = hid.Split(new char[] { '_' });
            string hotelID = string.Empty;
            if (_JuniperSupplierID == 41)
                hotelID = travReq.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
            else
                hotelID = hid;
            //serverResponse = XDocument.Load("D:\\Projects\\XML Integration\\W2M-Juniper\\Juniper_Response_BookingRules.xml"); // serverRequest.JuniperResponse(Request);
            XElement supplierResponse = LogXMLs(travReq.Descendants("TransID").First().Value, 1, _JuniperSupplierID).Descendants("Response")
                                .Where(x => x.Descendants("HotelResult").Where(y => y.Attribute("Code").Value.Equals(hotelID)).Any()).First().Descendants("HotelResult")
                                .Where(x => x.Attribute("Code").Value.Equals(hotelID))
                                .First();
            string[] RatePlanCode = travReq.Descendants("Room").First().Attribute("ID").Value.Split(new char[] { '_' });
            XElement HotelOption = supplierResponse.Descendants("HotelOption").Where(x => x.Attribute("RatePlanCode").Value.Equals(RatePlanCode[0])).First();
            bool CheckNewPolicy = false;

            if (HotelOption.Descendants("PolicyRules").Any())
            {
                if (!HotelOption.Descendants("PolicyRules").FirstOrDefault().HasElements)
                    CheckNewPolicy = true;
            }
            else
            {
                CheckNewPolicy = true;
            }

            if (CheckNewPolicy)
            {
                #region Supplier Interaction
                serverResponse = serverRequest.JuniperResponse(Request, "HotelBookingRules", _JNPRHotPreBookURL, Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value), Convert.ToString(travReq.Descendants("TransID").FirstOrDefault().Value), _JuniperSupplierID);
                XElement responseelement = removeAllNamespaces(serverResponse.Root);
                if (responseelement.Descendants("HotelOption").Any() && responseelement.Descendants("HotelOption").First().Attribute("Status").Value.Equals("OK"))
                {

                    HotelOption = responseelement.Descendants("HotelOption").FirstOrDefault();
                }
                else
                {
                    #region old code
                    //if (responseelement.Descendants("Error").Any() && responseelement.Descendants("Error").FirstOrDefault().Attributes("Text").Any())
                    //    cancelresponse.Add(new XElement("ErrorTxt", responseelement.Descendants("Error").FirstOrDefault().Attribute("Text").Value));
                    //else
                    //    cancelresponse.Add(new XElement("ErrorTxt", "Request failed at supplier, please check logs."));

                    //return cancelresponse; 


                    // This code has been commented as in case cancellation policy is not received then generate a non refundable cancellation policy
                    #endregion

                }
                #endregion
                #region Log Save
                try
                {
                    #region Removing Namespace
                    XElement RestelRNS = removeAllNamespaces(Request.Root);
                    XElement servresp = removecdata(supplierResponse);
                    #endregion
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = travReq.Descendants("TransID").FirstOrDefault().Value;
                    log.SupplierID = _JuniperSupplierID;
                    log.logrequestXML = RestelRNS.ToString();
                    log.logresponseXML = serverResponse.ToString();
                    log.LogTypeID = 3;
                    log.LogType = "CXLPolicy";
                    log.StartTime = starttime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "cancellationPolicyResponse";
                    ex1.PageName = "JuniperServices";
                    ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = travReq.Descendants("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion
                if (!HotelOption.Descendants("Rule").Any())
                {
                    double amount = Convert.ToDouble(HotelOption.Descendants("TotalFixAmounts").FirstOrDefault().Attribute("Nett").Value);
                    List<XElement> CancelPolicies = new List<XElement>(){ new XElement("CancellationPolicies",
                                                new XElement("CancellationPolicy",
                                                    new XAttribute("LastCancellationDate", DateTime.Now.AddDays(-2).ToString("dd/MM/yyyy")),
                                                    new XAttribute("ApplicableAmount", amount.ToString()),
                                                    new XAttribute("NoShowPolicy", "0")))};
                    XElement finalPolicies = MergCxlPolicy(CancelPolicies);
                    var cxp = new XElement("Hotels",
                                 new XElement("Hotel",
                                     new XElement("HotelID", hid),
                                     new XElement("HotelName", travReq.Descendants("HotelName").First().Value),
                                     new XElement("HotelImgSmall"),
                                     new XElement("HotelImgLarge"),
                                     new XElement("MapLink"),
                                     new XElement("DMC", _JNPRSupplierName),
                                     new XElement("Currency"),
                                     new XElement("Offers"),
                                     new XElement("Room",
                                         new XAttribute("ID", ""),
                                         new XAttribute("RoomType", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", ""),
                                         new XAttribute("TotalRoomRate", ""),
                                         new XAttribute("CancellationDate", ""),
                                        finalPolicies)));

                    cancelresponse.Add(cxp);
                    return cancelresponse;
                }
            }
            else
            {

                #region Log Save
                try
                {
                    #region Removing Namespace
                    //XElement RestelRNS = removeAllNamespaces(Request.Root);
                    XElement servresp = removecdata(supplierResponse);
                    #endregion
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = travReq.Descendants("TransID").FirstOrDefault().Value;
                    log.SupplierID = _JuniperSupplierID;
                    log.logrequestXML = null;
                    log.logresponseXML = servresp.ToString();
                    log.LogTypeID = 3;
                    log.LogType = "CXLPolicy";
                    log.StartTime = starttime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "cancellationPolicyResponse";
                    ex1.PageName = "JuniperServices";
                    ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = travReq.Descendants("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion

            }

            try
            {

                double perNightRate = 0.0;
                double totalRate = Convert.ToDouble(travReq.Descendants("RoomTypes").First().Attribute("TotalRate").Value);
                double firstnight = 0.0;
                #region Nights
                DateTime from = DateTime.ParseExact(travReq.Descendants("FromDate").First().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime toDate = DateTime.ParseExact(travReq.Descendants("ToDate").First().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                int nights = toDate.Subtract(from).Days;
                #endregion
                string ind = travReq.Descendants("RoomTypes").First().Attribute("Index").Value;
                List<XElement> roomAvail = LogXMLs(travReq.Descendants("TransID").FirstOrDefault().Value, 2, 0).Descendants("Response").ToList();
                XElement pbList = roomAvail.Where(x => x.Descendants("HotelID").FirstOrDefault().Value.Equals(travReq.Descendants("HotelID").FirstOrDefault().Value)).FirstOrDefault();
                foreach (XElement room in pbList.Descendants("RoomTypes").Where(x => x.Attribute("Index").Value.Equals(ind)).First().Descendants("Room"))
                {

                    firstnight += Convert.ToDouble(room.Descendants("PriceBreakups").First().Descendants("Price").Where(x => x.Attribute("Night").Value.Equals("1")).First().Attribute("PriceValue").Value);
                    //perNightRate = perNightRate + Convert.ToDouble(room.Attribute("PerNightRoomRate").Value);
                }

                XElement cp = new XElement("CancellationPolicies");

                #region Cancellation Policy Tag
                foreach (XElement policy in HotelOption.Descendants("Rule"))
                {
                    //int daysbefore = Convert.ToInt32(policy.Descendants("dias_antelacion").First().Value);
                    DateTime date = DateTime.ParseExact(policy.Attribute("DateFrom").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    //date = date.AddDays(-daysbefore);
                    string dt = date.ToString("dd/MM/yyyy");
                    dt = dt.Replace('-', '/');
                    double nightsCharged = 0;
                    double rate = 0.0;
                    if (Convert.ToInt32(policy.Attribute("Nights").Value) == 0)
                    {
                        if (!policy.Attribute("FixedPrice").Value.Equals("0"))
                        {
                            rate = Convert.ToDouble(policy.Attribute("FixedPrice").Value);
                        }
                        else
                        {
                            double per = Convert.ToDouble(policy.Attribute("PercentPrice").Value);
                            nightsCharged = per / 100;
                            rate = nightsCharged * totalRate;
                        }
                    }
                    else
                    {
                        if (policy.Attribute("ApplicationTypeNights").Value.Equals("FirstNight"))
                        {
                            rate = firstnight;
                        }
                        else
                        {
                            // int bookingnights = Convert.ToInt32(travReq.Descendants("Nights").First().Value);
                            rate = totalRate / nights;
                            nightsCharged = Convert.ToDouble(policy.Attribute("Nights").Value);
                            rate = rate * nightsCharged;
                        }
                    }
                    rate = Math.Round(rate, 2);
                    string checkinDate = travReq.Descendants("FromDate").FirstOrDefault().Value;
                    if (policy.Attribute("Type").Value.Equals("V"))
                    {
                        cp.Add(new XElement("CancellationPolicy",
                                                       new XAttribute("LastCancellationDate", dt),
                                                       new XAttribute("ApplicableAmount", Convert.ToString(rate)),
                                                       new XAttribute("NoShowPolicy", checkinDate.Equals(dt) ? "1" : "0")));
                    }
                    else if (policy.Attribute("Type").Value.Equals("R"))
                    {
                        string newdt = TravayooDate(policy.Attribute("DateFrom").Value);
                        cp.Add(new XElement("CancellationPolicy",
                            // new XAttribute("LastCancellationDate", DateTime.Now.AddDays(Convert.ToDouble(policy.Attribute("From").Value)).ToString("dd/MM/yyyy")),
                                                        new XAttribute("LastCancellationDate", newdt),
                                                       new XAttribute("ApplicableAmount", Convert.ToString(rate)),
                                                       new XAttribute("NoShowPolicy", newdt.Equals(checkinDate) ? "1" : "0")));
                    }
                    else
                    {
                        cp.Add(new XElement("CancellationPolicy",
                                                      new XAttribute("LastCancellationDate", travReq.Descendants("FromDate").First().Value),
                                                      new XAttribute("ApplicableAmount", Convert.ToString(rate)),
                                                      new XAttribute("NoShowPolicy", "1")));
                    }
                }

                List<XElement> mergeinput = new List<XElement>();
                cp.Descendants("CancellationPolicy").Where(x => x.Attribute("ApplicableAmount").Value.Equals("0")).Remove();
                mergeinput.Add(cp);
                XElement finalcp = MergCxlPolicy(mergeinput);
                #endregion
                #region Response XML
                if (finalcp.Descendants("CancellationPolicy").Any() && finalcp.Descendants("CancellationPolicy").Last().HasAttributes)
                {
                    foreach (XElement policy in finalcp.Descendants("CancellationPolicy").Where(x => x.Attribute("NoShowPolicy").Value.Equals("1")))
                    {
                        double nspRate = Convert.ToDouble(policy.Attribute("ApplicableAmount").Value);
                        if (nspRate > totalRate)
                            policy.Attribute("ApplicableAmount").SetValue(Convert.ToString(totalRate));
                    }
                    var cxp = new XElement("Hotels",
                             new XElement("Hotel",
                                 new XElement("HotelID", hid),
                                 new XElement("HotelName", travReq.Descendants("HotelName").First().Value),
                                 new XElement("HotelImgSmall"),
                                 new XElement("HotelImgLarge"),
                                 new XElement("MapLink"),
                                 new XElement("DMC", _JNPRSupplierName),
                                 new XElement("Currency"),
                                 new XElement("Offers"),
                                 new XElement("Room",
                                     new XAttribute("ID", ""),
                                     new XAttribute("RoomType", ""),
                                     new XAttribute("MealPlanPrice", ""),
                                     new XAttribute("PerNightRoomRate", ""),
                                     new XAttribute("TotalRoomRate", ""),
                                     new XAttribute("CancellationDate", ""),
                                    finalcp)));

                    cancelresponse.Add(cxp);
                }
                else
                {
                    cancelresponse.Add(new XElement("ErrorTxt", "No Cancellation Policy found"));
                }
                #endregion


            }
            catch (Exception ex)
            {
                cancelresponse.Add(new XElement("ErrorTxt", ex.Message));
            }

            return cancelresponse;
        }
        #endregion
        #endregion
        #region Pre-Booking
        public XElement PreBookingRequest(XElement Req, string xmlout, int supplierid)
        {
            dmc = xmlout;
            XElement response = null;


            string cityID = Req.Descendants("CityID").First().Value;


            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            #region Pre-Booking Request
            //Req = XElement.Load(@"C:\Users\Aman\Desktop\Test.xml");
            //string trackNumber = Req.Descendants("TransID").First().Value;
            //string HotelID = Req.Descendants("HotelID").First().Value;
            List<XElement> prebooklines = new List<XElement>();
            try
            {
                string hid = Req.Descendants("RoomTypes").Attributes("HtlCode").FirstOrDefault().Value;
                string[] splithid = hid.Split(new char[] { '_' });
                string hotelID = string.Empty;
                if (_JuniperSupplierID == 41)
                    hotelID = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                else
                    hotelID = hid;

                #region Request XML
                string[] splitID = Req.Descendants("Room").First().Attribute("ID").Value.Split(new char[] { '_' });
                XDocument BookingRulesRequest = new XDocument(
                                new XDeclaration("1.0", "utf-8", "yes"),
                                new XElement(soapenv + "Envelope",
                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                    new XAttribute(XNamespace.None + "xmlns", soap),
                                    new XElement(soapenv + "Header"),
                                    new XElement(soapenv + "Body",
                                    new XElement(soap + "HotelBookingRules",
                                     new XElement(soap + "HotelBookingRulesRQ",
                                     new XAttribute("Version", _JNPRVersion),
                                     new XAttribute("Language", "en"),
                                     new XElement(soap + "Login",
                                          new XAttribute("Email", _JNPRLoginID),
                                          new XAttribute("Password", _JNPRPassword)),
                                    new XElement(soap + "HotelBookingRulesRequest",
                                         new XElement(soap + "HotelOption",
                                         new XAttribute("RatePlanCode", splitID[0])),
                                         new XElement(soap + "SearchSegmentsHotels",
                                         new XElement(soap + "SearchSegmentHotels",
                                              new XAttribute("Start", JuniperDate(Req.Descendants("FromDate").First().Value)),
                                              new XAttribute("End", JuniperDate(Req.Descendants("ToDate").First().Value))),
                                               new XElement(soap + "HotelCodes",
                                               new XElement(soap + "HotelCode", hotelID)))),
                                                new XElement(soap + "AdvancedOptions",
                                                          new XElement(soap + "UseCurrency", "USD"),
                                                          new XElement(soap + "ShowBreakdownPrice", "true"),
                                                            new XElement(soap + "ShowCompleteInfo", "true")))))));


                #endregion
            #endregion
                response = PreBookingResponse(Req, BookingRulesRequest);

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBookingRequest";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion

            }

            #region Response Format
            string oldprice = Req.Descendants("RoomTypes").First().Attribute("TotalRate").Value;
            string newprice = response.Descendants("RoomTypes").Any() ? response.Descendants("RoomTypes").First().Attribute("TotalRate").Value : oldprice;
            XElement prebookingfinal = null;
            #region Price Change Condition
            double oprice = Convert.ToDouble(oldprice), nPrice = Convert.ToDouble(newprice);
            bool NoChangePrice = true;
            if (oprice != nPrice)
                NoChangePrice = false;
            if (NoChangePrice)
            {
                prebookingfinal = new XElement(
                                               new XElement(soapenv + "Envelope",
                                                   new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                   new XElement(soapenv + "Header",
                                                       new XElement("Authentication",
                                                           new XElement("AgentID", AgentID),
                                                           new XElement("Username", username),
                                                           new XElement("Password", password),
                                                           new XElement("ServiceType", ServiceType),
                                                           new XElement("ServiceVersion", ServiceVersion))),
                                                   new XElement(soapenv + "Body",
                                                       new XElement(Req.Descendants("HotelPreBookingRequest").First()),
                                                      response)));
                return prebookingfinal;
            }
            else
            {
                response.Descendants("NewPrice").Remove();
                prebookingfinal = new XElement(
                                            new XElement(soapenv + "Envelope",
                                                new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                new XElement(soapenv + "Header",
                                                    new XElement("Authentication",
                                                        new XElement("AgentID", AgentID),
                                                        new XElement("Username", username),
                                                        new XElement("Password", password),
                                                        new XElement("ServiceType", ServiceType),
                                                        new XElement("ServiceVersion", ServiceVersion))),
                                                new XElement(soapenv + "Body",
                                                    new XElement(Req.Descendants("HotelPreBookingRequest").First()),
                                                   new XElement("HotelPreBookingResponse", new XElement("ErrorTxt", "Amount has been changed"), new XElement("NewPrice", nPrice), response.Element("Hotels")))));
                return prebookingfinal;
            }



            #endregion
            #endregion


        }
        #region Response
        public XElement PreBookingResponse(XElement travReq, XDocument prebookreq)
        {
            XElement resp = null;
            XElement response = new XElement("HotelPreBookingResponse");
            DateTime starttime = DateTime.Now;
            // string hostUrl="http://xml2.bookingengine.es/webservice/jp/operations/checktransactions.asmx";
            // string hostUrl = ConfigurationManager.AppSettings["JNPRHotPreBookURL"].ToString();

            //serverResponse = XDocument.Load(@"C:\Users\Aman\Desktop\TestXML.xml");
            serverResponse = serverRequest.JuniperResponse(prebookreq, "HotelBookingRules", _JNPRHotPreBookURL, Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value), Convert.ToString(travReq.Descendants("TransID").FirstOrDefault().Value), _JuniperSupplierID);
            XElement responseelement = removeAllNamespaces(serverResponse.Root);
            XElement requestelement = removeAllNamespaces(prebookreq.Root);
            #region Log Save
            try
            {

                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = travReq.Descendants("TransID").FirstOrDefault().Value;
                log.SupplierID = _JuniperSupplierID;
                log.logrequestXML = requestelement.ToString();
                log.logresponseXML = responseelement.ToString();
                log.LogTypeID = 4;
                log.LogType = "PreBook";
                log.StartTime = starttime;
                log.EndTime = DateTime.Now;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBookingResponse";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion


            if (responseelement.Descendants("HotelOption").Any() && responseelement.Descendants("HotelOption").First().Attribute("Status").Value.Equals("OK"))
            {
                string bookingamount = responseelement.Descendants("TotalFixAmounts").First().Attribute("Nett").Value;
                #region Comments


                List<XElement> Comments = responseelement.Descendants("Comments").ToList();
                string conditions = null;

                foreach (XElement comnt in Comments)
                {
                    conditions += "<ul><li>" + comnt.Value + "</li></ul><br>";
                }
                // Hide on 29 jan, 2019 because it was required for certification only

                //if (responseelement.Descendants("CancellationPolicy").Any())
                //{
                //    if (responseelement.Descendants("CancellationPolicy").Descendants("Description").Any())
                //    {
                //        conditions += responseelement.Descendants("CancellationPolicy").Descendants("Description").FirstOrDefault().Value;
                //    }
                //}

                string hotelStar = string.Empty, receivd = responseelement.Descendants("HotelCategory").FirstOrDefault().Value;
                if (_JuniperSupplierID == 16)
                {
                    if (!char.IsDigit(receivd.ToCharArray()[0]))
                        hotelStar = receivd.ToCharArray().Length.ToString();
                    else
                        hotelStar = receivd.Substring(0, 1);
                }
                else
                    hotelStar = receivd.Substring(0, 1);
                #endregion
                XElement tryRoom = PreBookRooms(responseelement.Descendants("HotelOptions").First(), travReq);
                string checkString = responseelement.Descendants("Address").Where(x => !x.HasElements).FirstOrDefault().Value;
                #region Response Xml
                string hotelName = string.Empty;
                string address = string.Empty;
                string contact = string.Empty;
                string starRating = string.Empty;

                XElement HotelContent = responseelement.DescendantsOrEmpty("HotelContent").FirstOrDefault();
                if (HotelContent != null)
                {

                    if (HotelContent.Element("HotelName") != null)
                    {
                        hotelName = HotelContent.Element("HotelName").Value;
                    }

                    if (HotelContent.Element("Address") != null)
                    {
                        address = HotelContent.Element("Address").Element("Address").Value;
                    }
                    if (HotelContent.Element("ContactInfo") != null)
                    {
                        contact = HotelContent.Element("ContactInfo").Descendants("PhoneNumber").FirstOrDefault().Value;
                    }
                    if (HotelContent.Element("HotelCategory") != null)
                    {
                        string Rating = HotelContent.Element("HotelCategory").Value;
                        if (!string.IsNullOrEmpty(Rating))
                        {

                            starRating = Rating.Substring(0, 1);

                            char[] checkstar = starRating.ToString().ToArray();
                            if (!Char.IsDigit(checkstar[0]))
                            {
                                if (checkstar[0].Equals('*'))
                                {

                                    var lstarray = Rating.ToArray();
                                    starRating = lstarray.Where(x => x.Equals('*')).Count().ToString();
                                }
                                else
                                {
                                    starRating = "0";
                                }
                            }
                        }
                        else
                        {
                            starRating = "0";
                        }
                    }
                }
                var preBookResponse = new XElement("Hotels",
                                                               new XElement("Hotel",
                                                                   new XElement("HotelID", travReq.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value),
                                                                    new XElement("HotelName", hotelName),
                                                                    new XElement("Address", address),
                                                                    new XElement("ContactInfo", contact),
                                                                    new XElement("StarRating", starRating),
                    //new XElement("NewPrice"),
                    //new XElement("Address"),
                    //new XElement("StarRating", hotelStar),
                                                                   new XElement("Status", "true"),
                                                                   new XElement("TermCondition", conditions),
                                                                   new XElement("HotelImgSmall"),
                                                                   new XElement("HotelImgLarge"),
                                                                   new XElement("MapLink"),
                                                                   new XElement("DMC", _JNPRSupplierName),
                                                                   new XElement("Currency", travReq.Descendants("CurrencyName").First().Value),
                                                                    new XElement("Offers"),
                                                                          tryRoom));
                //XElement cp = cancellationPolicy(travReq).Descendants("CancellationPolicies").First();
                //XElement cp = PreBookPolicy(responseelement.Descendants("HotelOption").Any()? responseelement.Descendants("HotelOption").First():null,travReq,tryRoom.Descendants("PriceBreakups").ToList());
                XElement cp = newCxlPolicy(responseelement.Descendants("HotelOption").FirstOrDefault().Descendants("CancellationPolicy").FirstOrDefault(), tryRoom.Descendants("Price").ToList(), travReq.Descendants("FromDate").FirstOrDefault().Value);
                preBookResponse.Descendants("Room").Last().AddAfterSelf(cp);
                #endregion
                response.Add(new XElement("NewPrice"));
                response.Add(preBookResponse);
            }
            else
            {

                if (responseelement.Descendants("Warning").Any() ? responseelement.Descendants("Warning").First().Value.Equals("warnPriceChanged") : false)
                    response.Add(new XElement("ErrorTxt", "Amount has been changed"));
                else if (responseelement.Descendants("Warning").Any() ? responseelement.Descendants("Warning").First().Value.Equals("warnStatusChanged") : false)
                    response.Add(new XElement("ErrorTxt", "Status changed"));
                else if (responseelement.Descendants("Warning").Any())
                    response.Add(new XElement("ErrorTxt", "Supplier Warning: " + responseelement.Descendants("Warning").First().Value));
                else
                    response.Add(new XElement("ErrorTxt", "Availability Status Changed"));
            }
            return response;
        }
        public XElement newCxlPolicy(XElement xmlText, IEnumerable<XElement> priceList, string from)
        {
            XElement cxlItem = null;
            var groupPrice = priceList.GroupBy(x => x.Attribute("Night").Value).
                Select(y => new XElement("NightBreakup", new XAttribute("Night", y.Key),
                    new XAttribute("Amount", y.Sum(p => Convert.ToDecimal(p.Attribute("PriceValue").Value))))).ToList();
            var bookingAmt = groupPrice.Sum(x => Convert.ToDecimal(x.Attribute("Amount").Value));
            try
            {
                DateTime bookingDate = DateTime.Now;
                DateTime checkIn = from.GetDateTime("dd/MM/yyyy");
                List<XElement> cxlList = new List<XElement>();
                if (xmlText.Descendants("Rule").Count() > 0)
                {
                    foreach (XElement rule in xmlText.Descendants("Rule"))
                    {
                        string fromdate = bookingDate.ToString("yyyy-MM-dd HH:mm"); ;
                        if (rule.Attribute("DateFrom") != null)
                        {
                            fromdate = rule.Attribute("DateFrom").Value + " " + rule.Attribute("DateFromHour").Value;
                        }
                        //string todate = checkIn.ToString("yyyy-MM-dd HH:mm");
                        //if (rule.Attribute("DateTo") != null)
                        //{
                        //    todate = rule.Attribute("DateTo").Value + " " + rule.Attribute("DateToHour").Value;
                        //}
                        decimal cxlAmt;
                        int penaltyNights = Convert.ToInt32(rule.Attribute("Nights").Value);
                        if (penaltyNights == 0)
                        {
                            if (!rule.Attribute("FixedPrice").Value.Equals("0"))
                            {
                                cxlAmt = Convert.ToDecimal(rule.Attribute("FixedPrice").Value);
                            }
                            else
                            {
                                decimal percentage = Convert.ToDecimal(rule.Attribute("PercentPrice").Value);
                                cxlAmt = bookingAmt * percentage / 100;
                            }
                        }
                        else
                        {
                            decimal nightCost;
                            if (rule.Attribute("ApplicationTypeNights").Value.Equals("FirstNight"))
                            {
                                string FirstNightCost = string.Empty;
                                if (rule.Attributes("FirstNightPrice").Any())
                                {
                                    FirstNightCost = rule.Attribute("FirstNightPrice").Value;
                                }
                                else
                                {
                                    FirstNightCost = groupPrice.First().Attribute("Amount").Value;
                                }
                                nightCost = Convert.ToDecimal(FirstNightCost);
                            }
                            else if (rule.Attribute("ApplicationTypeNights").Value.Equals("Average"))
                            {
                                var Average = groupPrice.Average(x => Convert.ToDecimal(x.Attribute("Amount").Value));
                                nightCost = Average;
                            }
                            else
                            {
                                var maximum = groupPrice.Max(x => Convert.ToDecimal(x.Attribute("Amount").Value));
                                nightCost = maximum;
                            }
                            cxlAmt = nightCost * penaltyNights;
                        }
                        cxlList.Add(new XElement("CxlItem",
                                    new XAttribute("cxlDate", fromdate),
                                    new XAttribute("Cost", cxlAmt),
                                    new XAttribute("NoShow", rule.Attribute("Type").Value.Equals("S") ? 1 : 0)));

                        //cxlList.Add(new XElement("CxlItem",
                        //            new XAttribute("cxlDate", todate),
                        //            new XAttribute("Cost", cxlAmt),
                        //             new XAttribute("NoShow", rule.Attribute("Type").Value.Equals("S") ? 1 : 0)));

                    }


                    var withoutNoShow = cxlList.Where(x => x.Attribute("NoShow").Value == "0" && Convert.ToDecimal(x.Attribute("Cost").Value) != 0.0m).
                        GroupBy(x => new { x.Attribute("cxlDate").Value.GetDateTime("yyyy-MM-dd HH:mm").Date }).
                            Select(y => new XElement("CancellationPolicy",
                                new XAttribute("LastCancellationDate", y.Key.Date.ToString("dd/MM/yyyy")),
                                new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("Cost").Value))),
                                new XAttribute("NoShowPolicy", "0"))).OrderBy(p => p.Attribute("LastCancellationDate").Value.GetDateTime("dd/MM/yyyy")).ToList();


                    var noShow = cxlList.Where(x => x.Attribute("NoShow").Value == "1" && Convert.ToDecimal(x.Attribute("Cost").Value) != 0.0m).
                     GroupBy(x => new { x.Attribute("cxlDate").Value.GetDateTime("yyyy-MM-dd HH:mm").Date }).
                         Select(y => new XElement("CancellationPolicy",
                             new XAttribute("LastCancellationDate", y.Key.Date.ToString("dd/MM/yyyy")),
                             new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("Cost").Value))),
                             new XAttribute("NoShowPolicy", "0"))).OrderBy(p => p.Attribute("LastCancellationDate").Value).ToList();

                    if (noShow.Count > 0)
                    {
                        withoutNoShow.Add(new XElement("CancellationPolicy",
                               new XAttribute("LastCancellationDate", noShow.FirstOrDefault().Attribute("LastCancellationDate").Value),
                               new XAttribute("ApplicableAmount", noShow.Max(p => Convert.ToDecimal(p.Attribute("ApplicableAmount").Value))),
                               new XAttribute("NoShowPolicy", "1")));
                    }

                    var fItem = withoutNoShow.FirstOrDefault();
                    if (Convert.ToDecimal(fItem.Attribute("ApplicableAmount").Value) != 0.0m)
                    {
                        withoutNoShow.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate",
                            fItem.Attribute("LastCancellationDate").Value.GetDateTime("dd/MM/yyyy").AddDays(-1).Date.ToString("dd/MM/yyyy")),
                            new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0")));
                    }
                    cxlItem = new XElement("CancellationPolicies", withoutNoShow);

                }
                else
                {

                    cxlItem = new XElement("CancellationPolicies",
                         new XElement("CancellationPolicy",
                             new XAttribute("LastCancellationDate", DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy")),
                             new XAttribute("ApplicableAmount", "0.00"),
                             new XAttribute("NoShowPolicy", 0)),
                             new XElement("CancellationPolicy",
                                 new XAttribute("LastCancellationDate", DateTime.Now.ToString("dd/MM/yyyy")),
                                 new XAttribute("ApplicableAmount", bookingAmt),
                                 new XAttribute("NoShowPolicy", 0)));
                }
            }
            catch
            {
                cxlItem = new XElement("CancellationPolicies",
                        new XElement("CancellationPolicy",
                            new XAttribute("LastCancellationDate", DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy")),
                            new XAttribute("ApplicableAmount", "0.00"),
                            new XAttribute("NoShowPolicy", 0)),
                            new XElement("CancellationPolicy",
                                new XAttribute("LastCancellationDate", DateTime.Now.ToString("dd/MM/yyyy")),
                                new XAttribute("ApplicableAmount", bookingAmt),
                                new XAttribute("NoShowPolicy", 0)));
            }
            return cxlItem;
        }

        #region Pre-Booking Rooms
        string GetRating(string rating)
        {
            string starRating = string.Empty;
            if (!string.IsNullOrEmpty(rating))
            {
                string[] rateArry = rating.Split(' ');
                starRating = rateArry[0];
            }
            else
            {
                starRating = "0";
            }
            return starRating;
        }
        public XElement PreBookRooms(XElement JunPRooms, XElement travReq)
        {
            int count = 1;
            int index = 1;
            int cnt = 1;
            string reqid = travReq.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
            foreach (XElement roompax in travReq.Descendants("RoomPax"))
            {
                roompax.Add(new XElement("id", count++));
            }
            string child = null;
            #region Nights
            DateTime from = DateTime.ParseExact(travReq.Descendants("FromDate").First().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(travReq.Descendants("ToDate").First().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            int nights = toDate.Subtract(from).Days;
            #endregion
            List<XElement> roomtypeList = new List<XElement>();

            foreach (XElement room in JunPRooms.Descendants("HotelOption"))
            {


                bool roomflag = IsRoomRate(room);




                List<XElement> roomlist = new List<XElement>();
                decimal totalRate = 0;
                foreach (XElement roompax in travReq.Descendants("RoomPax"))
                {
                    int RoomSqlCounter = 1;

                    string[] strSource = room.Descendants("HotelRoom").First().Attribute("Source").Value.Split(new char[] { ',' });

                    // foreach (XElement hotelRoom in room.Descendants("HotelRoom").Where(x => x.Attribute("Source").Value.Equals(roompax.Element("id").Value)))
                    foreach (XElement hotelRoom in room.Descendants("HotelRoom"))
                    {
                        //string rmlist = null; 
                        string[] strSrc = hotelRoom.Attribute("Source").Value.Split(new char[] { ',' });

                        for (int i = 0; i < strSrc.Length; i++)
                        {
                            if (roompax.Element("id").Value.Equals(strSrc[i]))
                            {

                                decimal TotalAmounts = Convert.ToDecimal(room.Descendants("TotalFixAmounts").FirstOrDefault().Attribute("Nett").Value);

                                XElement pricedetail = PriceBreakup(room.Descendants("Prices").First(), strSrc[i], nights, Convert.ToDateTime(JuniperDate((travReq.Descendants("FromDate").First().Value))), travReq.Descendants("RoomPax").Count(), roomflag, TotalAmounts);

                                decimal totalroomrate = 0;

                                foreach (XElement p1 in pricedetail.Descendants("Price"))
                                {
                                    totalroomrate = totalroomrate + Convert.ToDecimal(p1.Attribute("PriceValue").Value);
                                }
                                totalRate += totalroomrate;

                                string mealType = string.Empty;
                                string mealName = string.Empty;
                                if (room.Descendants("Board").Count() > 0)
                                {
                                    var item = room.Descendants("Board").FirstOrDefault();
                                    mealType = item.Attribute("Type") != null ? item.Attribute("Type").Value : "";
                                    mealName = item.Value;
                                }
                                roomlist.Add(new XElement("Room",
                                                                new XAttribute("ID", room.Element("BookingCode").GetValueOrDefault("") + "_" + hotelRoom.Descendants("Name").FirstOrDefault().Value),
                                                                  new XAttribute("SuppliersID", _JuniperSupplierID.ToString()),
                                                                  new XAttribute("RoomSeq", RoomSqlCounter++),
                                                                  new XAttribute("SessionID", reqid),
                                                                  new XAttribute("RoomType", hotelRoom.Descendants("Name").First().Value),
                                                                  new XAttribute("OccupancyID", strSrc[i]),
                                                                  new XAttribute("OccupancyName", ""),
                                                                  MealPlanDetails(mealType, mealName),
                                                                  new XAttribute("MealPlanPrice", ""),
                                                                  new XAttribute("PerNightRoomRate", Math.Round(totalroomrate / nights, 2)),
                                                                  new XAttribute("TotalRoomRate", totalroomrate),
                                                                  new XAttribute("CancellationDate", ""),
                                                                  new XAttribute("CancellationAmount", ""),
                                                                  new XAttribute("isAvailable", "true"),
                                                                  new XElement("RequestID", room.Element("BookingCode").GetValueOrDefault("")),
                                                                  new XElement("Offers"),
                                                                  RoomPromotion(room.Descendants("HotelOffer")),
                                                                  new XElement("CancellationPolicy"),
                                                                  new XElement("Amenities",
                                                                      new XElement("Amenity")),
                                                                  new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                  SupplementTag(room.Descendants("HotelSupplement")),
                                                                   pricedetail,
                                                                  new XElement("AdultNum", roompax.Element("Adult").Value),
                                                                  new XElement("ChildNum", roompax.Element("Child").Value)));
                            }
                        }
                    }
                }
                roomtypeList.Add(new XElement("RoomTypes",
                                    new XAttribute("Index", cnt++),
                                    new XAttribute("TotalRate", totalRate), roomlist));
            }

            XElement response = new XElement("Rooms");
            response.Add(roomtypeList);
            return response;
        }

        #endregion
        #endregion
        #endregion
        #region Booking
        public XElement BookingRequest(XElement Req)
        {
            XElement response = null;
            bool condition = false;
            double newrate = 0.0;
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion

            try
            {

                string hid = Req.Descendants("HotelID").FirstOrDefault().Value, hotelID = string.Empty, bookingCode = Req.Descendants("Room").Descendants("RequestID").FirstOrDefault().Value;
                string[] splitid = hid.Split(new char[] { '_' });
                if (_JuniperSupplierID == 41)
                    hotelID = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                else
                    hotelID = hid;
                XElement advancedOptions = null;
                if (_JuniperSupplierID == 16)
                    advancedOptions = new XElement(soap + "AdvancedOptions", new XElement(soap + "UseCurrency", "USD"));
                XElement PreBookElement = new XElement("Logs", LogXMLs(Req.Descendants("TransactionID").FirstOrDefault().Value, 4, _JuniperSupplierID));
                XElement PrebookResp = PreBookElement.Descendants("Response").Where(x => x.Descendants("BookingCode").FirstOrDefault().Value.Equals(bookingCode)).FirstOrDefault().Descendants("HotelRequiredFields").First();
                int HolderCount = PrebookResp.Descendants("Holder").First().Descendants("RelPax").Count();
                //string price = PreBookElement.Descendants("TotalFixAmounts").FirstOrDefault().Attribute("Nett").Value;
                string price = Req.Descendants("TotalAmount").FirstOrDefault().Value;
                XElement holder = new XElement("HoldingParent", PrebookResp.Descendants("Holder").First());
                foreach (XElement element in holder.Descendants())
                    element.Name = soap + element.Name.LocalName;
                //XElement Paxes = bookingpax(Req.Descendants("PassengersDetail").First(), HolderCount, Req.Descendants("PaxNationality_CountryCode").First().Value);
                XDocument Bookingconfirmation = new XDocument(
                                                    new XDeclaration("1.0", "UTF-8", "yes"),
                                                    new XElement(soapenv + "Envelope",
                                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                        new XAttribute(XNamespace.None + "xmlns", soap),
                                                        new XElement(soapenv + "Header"),
                                                        new XElement(soapenv + "Body",
                                                            new XElement(soap + "HotelBooking",
                                                                new XElement(soap + "HotelBookingRQ",
                                                                    new XAttribute("Version", _JNPRVersion),
                                                                    new XAttribute("Language", "en"),
                                                                    new XElement(soap + "Login",
                                                                        new XAttribute("Email", _JNPRLoginID),
                                                                        new XAttribute("Password", _JNPRPassword)),
                                                                        bookingpax(Req.Descendants("PassengersDetail").First(), HolderCount, Req.Descendants("PaxNationality_CountryCode").First().Value),
                                                                        holder.Element(soap + "Holder"),
                    //new XElement(soap+"Holder",
                    //    new XElement(soap+"RelPax",
                    //        new XAttribute("IdPax",""))),
                    //new XElement(soap+"ExternalBookingReference",""),
                                                                                new XElement(soap + "ExternalBookingReference", Req.Descendants("TransactionID").First().Value),
                                                                                new XElement(soap + "Comments",
                                                                                    new XElement(soap + "Comment", "")),
                                                                                    new XElement(soap + "Elements",
                                                                                        new XElement(soap + "HotelElement",
                                                                                            new XElement(soap + "BookingCode", bookingCode),
                                                                                             new XElement(soap + "RelPaxesDist", bindRomPax(Req.Descendants("RoomPax").ToList())),
                                                                                             new XElement(soap + "Comments",
                                                                                                 new XElement(soap + "Comment",
                                                                                                     new XAttribute("Type", "ELE"), Req.Descendants("SpecialRemarks").FirstOrDefault().Value)),
                                                                                             new XElement(soap + "HotelBookingInfo",
                                                                                                 new XAttribute("Start", JuniperDate(Req.Descendants("FromDate").First().Value)),
                                                                                                 new XAttribute("End", JuniperDate(Req.Descendants("ToDate").First().Value)),
                                                                                                 new XElement(soap + "HotelCode", hotelID),
                                                                                                 new XElement(soap + "Price",
                                                                                                     new XElement(soap + "PriceRange",
                                                                                                     new XAttribute("Currency", Req.Descendants("CurrencyCode").First().Value),
                                                                                                     new XAttribute("Minimum", price),
                                                                                                     new XAttribute("Maximum", price))),
                                                                                                     new XElement(soap + "PackageContracts", "Package")))), advancedOptions)))));


                response = bookingResponse(Bookingconfirmation, Req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "BookingRequest";
                ex1.PageName = "JuniperReponses";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransactionID").FirstOrDefault().Value;
                SaveAPILog saveexl = new SaveAPILog();
                saveexl.SendCustomExcepToDB(ex1);
                XElement confirmedBookingerr = new XElement(
                                                new XElement(soapenv + "Envelope",
                                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                    new XElement(soapenv + "Header",
                                                        new XElement("Authentication",
                                                            new XElement("AgentID", AgentID),
                                                            new XElement("Username", username),
                                                            new XElement("Password", password),
                                                            new XElement("ServiceType", ServiceType),
                                                            new XElement("ServiceVersion", ServiceVersion))),
                                                    new XElement(soapenv + "Body",
                                                        new XElement(Req.Descendants("HotelBookingRequest").First()),
                                                         new XElement("HotelBookingResponse",
                                                            new XElement("ErrorTxt", Convert.ToString(ex.Message.ToString())))
                                                       )));
                return confirmedBookingerr;
                #endregion
            }
            #region Response Format
            XElement confirmedBooking = new XElement(
                                                new XElement(soapenv + "Envelope",
                                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                    new XElement(soapenv + "Header",
                                                        new XElement("Authentication",
                                                            new XElement("AgentID", AgentID),
                                                            new XElement("Username", username),
                                                            new XElement("Password", password),
                                                            new XElement("ServiceType", ServiceType),
                                                            new XElement("ServiceVersion", ServiceVersion))),
                                                    new XElement(soapenv + "Body",
                                                        new XElement(Req.Descendants("HotelBookingRequest").First()),
                                                       response)));
            #region Price Change Condition
            if (condition)
                confirmedBooking.Descendants("Hotels").First().AddBeforeSelf(
                     new XElement("ErrorTxt", "Amount has been changed"),
                   new XElement("NewPrice", Convert.ToString(newrate)));
            #endregion
            #endregion
            return confirmedBooking;
        }
        public XElement bookingResponse(XDocument RestelRequest, XElement travReq)
        {
            XElement response = null;
            try
            {
                DateTime starttime = DateTime.Now;
                serverResponse = serverRequest.JuniperResponse(RestelRequest, "HotelBooking", _JNPRHotBookURL, Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value), Convert.ToString(travReq.Descendants("TransID").FirstOrDefault().Value), _JuniperSupplierID);
                XElement servrespnns = removeAllNamespaces(serverResponse.Root);
                XElement restreqnns = removeAllNamespaces(RestelRequest.Root);



                #region Save Log
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = travReq.Descendants("TransactionID").FirstOrDefault().Value;
                log.SupplierID = _JuniperSupplierID;
                log.logrequestXML = restreqnns.ToString();
                log.logresponseXML = servrespnns.ToString();
                log.LogType = "Book";
                log.LogTypeID = 5;
                log.StartTime = starttime;
                log.EndTime = DateTime.Now;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion

                if (servrespnns.Descendants("Error").Count() == 0)
                {
                    int adult = 0;
                    int child = 0;
                    foreach (XElement ad in travReq.Descendants("RoomPax"))
                    {
                        adult = adult + Convert.ToInt32(ad.Element("Adult").Value);
                        child = child + Convert.ToInt32(ad.Element("Child").Value);
                    }
                    List<string> status = new List<string>(new string[] { "PAG", "CON", "TAR" });
                    if (status.Contains(servrespnns.Descendants("Reservation").First().Attribute("Status").Value.ToUpper()))
                    {
                        //string taxes = string.Empty;
                        //if (servrespnns.Descendants("ServiceTaxes").Any())
                        //    taxes = "Tax Amount = " + servrespnns.Descendants("ServiceTaxes").FirstOrDefault().Attribute("Amount").Value;
                        string commentText = string.Empty;
                        var commentList = servrespnns.DescendantsOrEmpty("Comment").Where(y => y.Attribute("Type").Value == "HOT");

                        foreach (var item in commentList)
                        {
                            commentText += item.Value;
                            commentText += "\n";
                        }
                        #region Response XML
                        var hbr = new XElement("Hotels",
                            new XElement("HotelID", travReq.Descendants("HotelID").First().Value),
                                                        new XElement("HotelName", travReq.Descendants("HotelName").First().Value),
                                                        new XElement("FromDate", travReq.Descendants("FromDate").First().Value),
                                                        new XElement("ToDate", travReq.Descendants("ToDate").First().Value),
                                                        new XElement("AdultPax", Convert.ToString(adult)),
                                                        new XElement("ChildPax", Convert.ToString(child)),
                                                        new XElement("TotalPrice", travReq.Descendants("TotalAmount").First().Value),
                                                        new XElement("CurrencyID", travReq.Descendants("CurrencyID").First().Value),
                                                        new XElement("CurrencyCode", travReq.Descendants("CurrencyCode").First().Value),
                                                        new XElement("MarketID"),
                                                        new XElement("MarketName"),
                                                        new XElement("HotelImgSmall"),
                                                        new XElement("HotelImgLarge"),
                                                        new XElement("MapLink"),
                                                        new XElement("VoucherRemark", commentText),
                                                        new XElement("TransID", travReq.Descendants("TransID").First().Value),
                                                        new XElement("ConfirmationNumber", servrespnns.Descendants("Reservation").First().Attribute("Locator").Value),
                                                        new XElement("Status", status.Contains(servrespnns.Descendants("Reservation").First().Attribute("Status").Value.ToUpper()) ? "Success" : "Fail"),
                                                        Booking_Rooms(travReq));
                        #endregion
                        response = new XElement("HotelBookingResponse", hbr);
                    }
                    else
                    {
                        response = new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Your booking was unsuccessful"));
                    }
                }
                else
                {

                    var txtMsg = servrespnns.Descendants("Error").FirstOrDefault().Attribute("Text").Value;
                    txtMsg = Regex.Replace(txtMsg, @"[\d-]", string.Empty);
                    response = new XElement("HotelBookingResponse", new XElement("ErrorTxt", txtMsg));

                }

            }
            catch (Exception ex)
            {


                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "BookingResponse";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travReq.Descendants("TransactionID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion

                response = new XElement("HotelBookingResponse", new XElement("ErrorTxt", ex.Message));
            }

            return response;
        }
        #endregion
        #region Booking Cancellation
        public XElement BookingCancel(XElement Req)
        {
            XElement response = null;

            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            //-- To check Cancellation Amount          

            DateTime starttime = DateTime.Now;
            XElement advancedOptions = null;
            if (_JuniperSupplierID == 16)
                advancedOptions = new XElement(soap + "AdvancedOptions", new XElement(soap + "UseCurrency", "USD"));

            try
            {
                XDocument CheckCXAmt = new XDocument(
                                  new XDeclaration("1.0", "UTF-8", "yes"),
                                  new XElement(soapenv + "Envelope",
                                      new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                      new XAttribute(XNamespace.None + "xmlns", soap),
                                      new XElement(soapenv + "Header"),
                                      new XElement(soapenv + "Body",
                                          new XElement(soap + "CancelBooking",
                                              new XElement(soap + "CancelRQ",
                                                  new XAttribute("Version", _JNPRVersion),
                                                  new XAttribute("Language", "en"),
                                                  new XElement(soap + "Login",
                                                      new XAttribute("Email", _JNPRLoginID),
                                                      new XAttribute("Password", _JNPRPassword)),
                                                      new XElement(soap + "CancelRequest",
                                                          new XAttribute("ReservationLocator", Req.Descendants("ConfirmationNumber").First().Value)), advancedOptions)))));
                response = CancellationResponse(CheckCXAmt, Req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "BookingCancel";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
            #region Response Format
            XElement cancelledBooking = new XElement(
                                               new XElement(soapenv + "Envelope",
                                                   new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                                   new XElement(soapenv + "Header",
                                                       new XElement("Authentication",
                                                           new XElement("AgentID", AgentID),
                                                           new XElement("Username", username),
                                                           new XElement("Password", password),
                                                           new XElement("ServiceType", ServiceType),
                                                           new XElement("ServiceVersion", ServiceVersion))),
                                                   new XElement(soapenv + "Body",
                                                       new XElement(removeAllNamespaces(Req.Descendants("HotelCancellationRequest").First())),
                                                      response)));
            return cancelledBooking;
            #endregion
        }
        #region Cancellation  Response
        public XElement CancellationResponse(XDocument BookingCancel, XElement Req)
        {
            DateTime starttime = DateTime.Now;
            //string hostUrl = "http://xml2.bookingengine.es/webservice/jp/operations/booktransactions.asmx";
            // string hostUrl = ConfigurationManager.AppSettings["JNPRHotBookCanURL"].ToString();
            serverResponse = serverRequest.JuniperResponse(BookingCancel, "CancelBooking", _JNPRHotBookCanURL, Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value), Convert.ToString(Req.Descendants("TransID").FirstOrDefault().Value), _JuniperSupplierID);
            #region Log Save

            #region Removing Namespace
            XElement restcancelnns = removeAllNamespaces(BookingCancel.Root);
            XElement servrespnns = removeAllNamespaces(serverResponse.Root);
            #endregion
            try
            {

                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                log.SupplierID = _JuniperSupplierID;
                log.logrequestXML = restcancelnns.ToString();
                log.logresponseXML = servrespnns.ToString();
                log.LogTypeID = 6;
                log.LogType = "Cancel";
                log.StartTime = starttime;
                log.EndTime = DateTime.Now;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancellationResponse";
                ex1.PageName = "JuniperResponses";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            XElement response = new XElement("HotelCancellationResponse");
            string status = null;

            if (servrespnns.Descendants("BookingCodeState").Any() && servrespnns.Descendants("BookingCodeState").First().Value.Equals("CaC") || servrespnns.Descendants("BookingCodeState").First().Value.Equals("Can"))
            {
                status = "Success";
                string cancelcost = servrespnns.Descendants("BookingCancelCost").First().Value;
                var cancel = new XElement("Room",
                                     new XElement("Cancellation",
                                         new XElement("Amount", cancelcost),
                                         new XElement("Status", status)));
                response.Add(new XElement("Rooms", cancel));
            }
            else
            {
                status = "Failed";
                #region Error Text
                String Error = null;
                if (servrespnns.Descendants("Error").Any())
                {
                    Error = servrespnns.Descendants("Error").First().Attribute("Text").Value;
                }
                else if (servrespnns.Descendants("BookingCodeState").Any())
                {
                    Error = "Cancellation status: " + servrespnns.Descendants("BookingCodeState").First().Value;
                }
                response.Add(new XElement("ErrorTxt", servrespnns.Descendants("Error").First().Attribute("Text").Value));
                #endregion
            }
            return response;
        }
        #endregion
        #endregion





        #region Common Functions
        #region Rooms for Response

        public XElement groupedRooms(XElement JunpRooms, XElement travReq, string reqid)
        {
            int count = 1;
            int index = 1;
            int cnt = 1;
            foreach (XElement roompax in travReq.Descendants("RoomPax"))
            {
                roompax.Add(new XElement("id", count++));
            }
            string child = null;

            List<XElement> roomtypeList = new List<XElement>();

            foreach (XElement room in JunpRooms.Descendants("HotelOption"))
            {

                bool roomflag = IsRoomRate(room);


                try
                {
                    List<XElement> roomlist = new List<XElement>();
                    foreach (XElement roompax in travReq.Descendants("RoomPax"))
                    {
                        int RoomSqlCounter = 1;

                        string[] strSource = room.Descendants("HotelRoom").First().Attribute("Source").Value.Split(new char[] { ',' });

                        // foreach (XElement hotelRoom in room.Descendants("HotelRoom").Where(x => x.Attribute("Source").Value.Equals(roompax.Element("id").Value)))
                        foreach (XElement hotelRoom in room.Descendants("HotelRoom"))
                        {
                            //string rmlist = null; 
                            string[] strSrc = hotelRoom.Attribute("Source").Value.Split(new char[] { ',' });

                            for (int i = 0; i < strSrc.Length; i++)
                            {
                                if (roompax.Element("id").Value.Equals(strSrc[i]))
                                {

                                    decimal TotalAmounts = Convert.ToDecimal(room.Descendants("TotalFixAmounts").FirstOrDefault().Attribute("Nett").Value);
                                    XElement pricedetail = PriceBreakup(room.Descendants("Prices").First(), strSrc[i], Convert.ToInt16(travReq.Descendants("Nights").First().Value), Convert.ToDateTime(JuniperDate((travReq.Descendants("FromDate").First().Value))), travReq.Descendants("RoomPax").Count(), roomflag, TotalAmounts);

                                    decimal totalroomrate = 0;

                                    foreach (XElement p1 in pricedetail.Descendants("Price"))
                                    {
                                        totalroomrate = totalroomrate + Convert.ToDecimal(p1.Attribute("PriceValue").Value);
                                    }

                                    string mealType = string.Empty;
                                    string mealName = string.Empty;
                                    if (room.Descendants("Board").Count() > 0)
                                    {
                                        var item = room.Descendants("Board").FirstOrDefault();
                                        mealType = item.Attribute("Type") != null ? item.Attribute("Type").Value : "";
                                        mealName = item.Value;
                                    }
                                    roomlist.Add(new XElement("Room",
                                        new XAttribute("ID", room.Attribute("RatePlanCode").Value + "_" + hotelRoom.Descendants("Name").FirstOrDefault().Value),
                                        new XAttribute("SuppliersID", _JuniperSupplierID.ToString()),
                                        new XAttribute("RoomSeq", RoomSqlCounter++),
                                        new XAttribute("SessionID", reqid),
                                        new XAttribute("RoomType", hotelRoom.Descendants("Name").First().Value),
                                        new XAttribute("OccupancyID", strSrc[i]),
                                        new XAttribute("OccupancyName", ""),
                                        MealPlanDetails(mealType, mealName),
                                        new XAttribute("MealPlanPrice", ""),
                                        new XAttribute("PerNightRoomRate", Math.Round(totalroomrate / Convert.ToInt16(travReq.Descendants("Nights").First().Value), 2)),
                                        new XAttribute("TotalRoomRate", totalroomrate),
                                        new XAttribute("CancellationDate", ""),
                                        new XAttribute("CancellationAmount", ""),
                                        new XAttribute("isAvailable", "true"),
                                        new XElement("RequestID", reqid),
                                        new XElement("Offers"),
                                        RoomPromotion(room.Descendants("HotelOffer")),
                                        SupplementTag(room.Descendants("HotelSupplement")),
                                        new XElement("CancellationPolicy"),
                                        new XElement("Amenities",
                                            new XElement("Amenity")),
                                            new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                            pricedetail,
                                            new XElement("AdultNum", roompax.Element("Adult").Value),
                                            new XElement("ChildNum", roompax.Element("Child").Value)));
                                }
                            }
                        }
                    }

                    roomtypeList.Add(new XElement("RoomTypes",
                                        new XAttribute("Index", cnt++),
                                        new XAttribute("TotalRate", room.Descendants("TotalFixAmounts").First().Attribute("Nett").Value),
                                        new XAttribute("HtlCode", hotelcode),
                                         new XAttribute("CrncyCode", SupplierCurrency),
                                         new XAttribute("DMCType", dmc),
                                         new XAttribute("CUID", junipercustomer_id),
                                        roomlist));
                }
                catch
                {

                }

            }

            XElement response = new XElement("Rooms");
            response.Add(roomtypeList);
            return response;

        }


        #endregion

        #region Rooms for Booking Response
        public XElement Booking_Rooms(XElement travyobcr)
        {
            int count = 0;
            XElement guests = new XElement("GuestDetails");
            List<string> linesvalues = new List<string>();

            XElement bookedrooms = new XElement("PassengerDetail");
            foreach (XElement Room in travyobcr.Descendants("Room"))
            {
                var RoomsGettingbooked =
                                                    new XElement("Room",
                                                        new XAttribute("ID", count++),
                                                        new XAttribute("RoomType", Room.Attribute("RoomType").Value),
                                                        new XAttribute("ServiceID", ""),
                                                        new XAttribute("MealPlanID", Room.Attribute("MealPlanID").Value),
                                                        new XAttribute("MealPlanName", ""),
                                                        new XAttribute("MealPlanCode", ""),
                                                        new XAttribute("MealPlanPrice", ""),
                                                        new XAttribute("PerNightRoomRate", ""),
                                                        new XAttribute("RoomStatus", "true"),
                                                        new XAttribute("TotalRoomRate", Room.Attribute("TotalRoomRate").Value),
                                                        new XElement("RoomGuest", Room.Descendants("PaxInfo").Elements()),
                                                        new XElement("Supplements", Room.Descendants("Supplements")));
                if (RoomsGettingbooked.Descendants("RoomGuest").Any())
                {

                    guests.Add(RoomsGettingbooked);
                }

            }
            bookedrooms.Add(guests);
            return bookedrooms;
        }
        #endregion

        #region Tags for Cancellation Policy
        public XElement CXLTags(XElement RestelPolicy, double price)
        {
            XElement responseCP = new XElement("CancellationPolicies");
            var policies = RestelPolicy.Descendants("politicaCanc");
            foreach (var policy in policies)
            {
                int daysbefore = Convert.ToInt32(policy.Descendants("dias_antelacion").First().Value);
                DateTime date = DateTime.ParseExact(policy.Attribute("fecha").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                date = date.AddDays(-daysbefore);
                string dt = date.ToString("dd/MM/yyyy");
                double nightsCharged = 0;
                if (!policy.Descendants("noches_gasto").First().Value.Equals("0"))
                    nightsCharged = Convert.ToDouble(policy.Descendants("noches_gasto").First().Value);
                else
                {
                    double per = Convert.ToDouble(policy.Descendants("estCom_gasto").First().Value);
                    nightsCharged = per / 100;
                }
                responseCP.Add(new XElement("CancellationPolicy",
                                    new XAttribute("LastCancellationDate", dt),
                                    new XAttribute("ApplicableAmount", Convert.ToString(nightsCharged * price)),
                                    new XAttribute("NoShowPolicy", "0")));
            }
            return responseCP;
        }
        #endregion
        #region Merge Cancellation Policy
        public XElement MergCxlPolicy(List<XElement> rooms)
        {
            List<XElement> cxlList = new List<XElement>();

            IEnumerable<XElement> dateLst = rooms.Descendants("CancellationPolicy").
               GroupBy(r => new { r.Attribute("LastCancellationDate").Value, noshow = r.Attribute("NoShowPolicy").Value }).Select(y => y.First()).
               OrderBy(p => DateTime.ParseExact(p.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture));
            if (dateLst.Count() > 0)
            {

                foreach (var item in dateLst)
                {
                    string date = item.Attribute("LastCancellationDate").Value;
                    string noShow = item.Attribute("NoShowPolicy").Value;
                    decimal datePrice = 0.0m;
                    foreach (var rm in rooms.Descendants("CancellationPolicy"))
                    {

                        if (rm.Attribute("NoShowPolicy").Value == noShow && rm.Attribute("LastCancellationDate").Value == date)
                        {

                            var price = rm.Attribute("ApplicableAmount").Value;
                            datePrice += Convert.ToDecimal(price);
                        }
                        else
                        {
                            if (noShow == "1")
                            {
                                //datePrice += Convert.ToDecimal(rm.Attribute("ApplicableAmount").Value);
                            }
                            else
                            {


                                var lastItem = rm.Descendants("CancellationPolicy").
                                    Where(pq => (pq.Attribute("NoShowPolicy").Value == noShow && Convert.ToDateTime(pq.Attribute("LastCancellationDate").Value) < chnagetoTime(date)));

                                if (lastItem.Count() > 0)
                                {
                                    var lastDate = lastItem.Max(y => y.Attribute("LastCancellationDate").Value);
                                    var lastprice = rm.Descendants("CancellationPolicy").
                                        Where(pq => (pq.Attribute("NoShowPolicy").Value == noShow && pq.Attribute("LastCancellationDate").Value == lastDate)).
                                        FirstOrDefault().Attribute("ApplicableAmount").Value;
                                    datePrice += Convert.ToDecimal(lastprice);
                                }

                            }

                        }
                    }
                    XElement pItem = new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", date), new XAttribute("ApplicableAmount", datePrice), new XAttribute("NoShowPolicy", noShow));
                    cxlList.Add(pItem);

                }

                cxlList = cxlList.GroupBy(x => new { DateTime.ParseExact(x.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date, x.Attribute("NoShowPolicy").Value }).
                    Select(y => new XElement("CancellationPolicy",
                        new XAttribute("LastCancellationDate", y.Key.Date.ToString("dd/MM/yyyy")),
                        new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("ApplicableAmount").Value))),
                        new XAttribute("NoShowPolicy", y.Key.Value))).OrderBy(p => DateTime.ParseExact(p.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture)).ToList();

                var fItem = cxlList.FirstOrDefault();

                if (Convert.ToDecimal(fItem.Attribute("ApplicableAmount").Value) != 0.0m)
                {
                    cxlList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", DateTime.ParseExact(fItem.Attribute("LastCancellationDate").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date.AddDays(-1).ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0")));

                }
            }

            XElement cxlItem = new XElement("CancellationPolicies", cxlList);
            return cxlItem;

        }
        public DateTime chnagetoTime(string strDate)
        {
            DateTime oDate = DateTime.ParseExact(strDate, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            return oDate;

        }
        #endregion

        #region Break hotel list into chunks
        public List<List<T>> BreakIntoChunks<T>(List<T> list, int chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentException("chunkSize must be greater than 0.");
            }
            List<List<T>> retVal = new List<List<T>>();
            while (list.Count > 0)
            {
                int count = list.Count > chunkSize ? chunkSize : list.Count;
                retVal.Add(list.GetRange(0, count));
                list.RemoveRange(0, count);
            }

            return retVal;
        }
        #endregion


        #region Meal Plan Name and Code and ID
        public List<XAttribute> MealPlanDetails(string reg, string mealname)
        {

            string mpName = null;
            string mpCode = null;
            string mpID = null;
            switch (reg)
            {
                case "RO":
                    mpName = "Room Only";
                    mpID = "1";
                    mpCode = "RO";
                    break;
                case "SA":
                    mpName = "Room Only";
                    mpID = "1";
                    mpCode = "RO";
                    break;
                case "TI":
                    mpName = "All Inclusive";
                    mpID = "5";
                    mpCode = "AI";
                    break;
                case "AI":
                    mpName = "All Inclusive";
                    mpID = "5";
                    mpCode = "AI";
                    break;
                case "STI":
                    mpName = "Soft All inclusive";
                    mpID = "5";
                    mpCode = "AI";
                    break;
                case "AD":
                    mpName = "Bed & Breakfast";
                    mpID = "2";
                    mpCode = "BB";
                    break;
                case "DMP":
                    mpName = "Bed Breakfast / Half Board";
                    mpID = "2";
                    mpCode = "BB";
                    break;
                case "PC":
                    mpName = "Full Board";
                    mpID = "4";
                    mpCode = "FB";
                    break;
                case "MP":
                    mpName = "Half Board";
                    mpID = "3";
                    mpCode = "HB";
                    break;
                case "HC":
                    mpName = "Half Board";
                    mpID = "3";
                    mpCode = "HB";
                    break;
                case "FI":
                    mpName = "New Years Eve Dinner";
                    mpID = "6";
                    mpCode = "FI";
                    break;
                case "HP":
                    mpName = "Half Board";
                    mpID = "3";
                    mpCode = "HB";
                    break;
                case "SC":
                    mpName = "Self Catering";
                    mpID = "";
                    mpCode = "";
                    break;
                default:
                    mpName = mealname;
                    mpID = " ";
                    mpCode = reg;
                    break;
            }
            List<XAttribute> response = new List<XAttribute>();
            response.Add(new XAttribute("MealPlanID", mpID));
            response.Add(new XAttribute("MealPlanName", mpName));
            response.Add(new XAttribute("MealPlanCode", mpCode));
            return response;
        }
        #endregion

        #region Remove Namespaces
        public static XElement removeAllNamespaces(XElement e)
        {
            return new XElement(e.Name.LocalName,
              (from n in e.Nodes()
               select ((n is XElement) ? removeAllNamespaces(n as XElement) : n)),
                  (e.HasAttributes) ?
                    (from a in e.Attributes()
                     where (!a.IsNamespaceDeclaration)
                     select new XAttribute(a.Name.LocalName, a.Value)) : null);
        }
        public XElement removecdata(XElement e)
        {
            foreach (XElement x in e.Elements())
            {
                if (x.HasElements)
                {
                    removecdata(x);
                }
                else
                {
                    string check = x.Value;
                    check.Replace("![CDATA[", "").Replace("]]", "");
                    x.SetValue(check);
                }
            }
            return e;
        }
        #endregion

        #region Currency Mapping
        public string currency(string input)
        {
            string response = null;
            switch (input)
            {
                case "EU":
                    response = "EUR";
                    break;
                case "PA":
                    response = "ARS";
                    break;
                case "DC":
                    response = "CAD";
                    break;
                case "FS":
                    response = "CHF";
                    break;
                case "YE":
                    response = "JPY";
                    break;
                case "DO":
                    response = "USD";
                    break;
                case "LB":
                    response = "GBP";
                    break;
                case "PM":
                    response = "MXN";
                    break;
                case "DA":
                    response = "AUD";
                    break;
                case "RB":
                    response = "RUB";
                    break;
                case "BR":
                    response = "BRL";
                    break;
                case "TH":
                    response = "THB";
                    break;
                case "DT":
                    response = "TND";
                    break;
                case "IN":
                    response = "INR";
                    break;
                case "DE":
                    response = "AED";
                    break;
                case "DI":
                    response = "MAD";
                    break;
                case "CP":
                    response = "COP";
                    break;
                case "QR":
                    response = "QAR";
                    break;
                case "BL":
                    response = "BGN";
                    break;
                case "CD":
                    response = "DKK";
                    break;
                case "CN":
                    response = "NOK";
                    break;
                case "CS":
                    response = "SEK";
                    break;
                case "CZ":
                    response = "CZK";
                    break;
                case "HR":
                    response = "HRK";
                    break;
                case "ID":
                    response = "IDR";
                    break;
                case "IL":
                    response = "ILS";
                    break;
                case "KR":
                    response = "KRW";
                    break;
                case "LT":
                    response = "TRY";
                    break;
                case "MR":
                    response = "MYR";
                    break;
                case "ND":
                    response = "NZD";
                    break;
                case "PH":
                    response = "PHP";
                    break;
                case "RA":
                    response = "ZAR";
                    break;
                case "RO":
                    response = "RON";
                    break;
                case "SD":
                    response = "SGD";
                    break;
                case "ZL":
                    response = "PLN";
                    break;
                case "DZ":
                    response = "DZD";
                    break;
                case "DJ":
                    response = "JOD";
                    break;
                case "NN":
                    response = "NGN";
                    break;
                case "TD":
                    response = "TWD";
                    break;
                case "RE":
                    response = "CNY";
                    break;
                case "DH":
                    response = "HKD";
                    break;
                default:
                    response = input;
                    break;
            }
            return response;
        }
        #endregion

        #region Minimum Rate
        public string MinRate(XElement rooms)
        {
            double minprice = double.MaxValue;
            foreach (XElement room in rooms.Descendants("HotelOption"))
            {
                double check = Convert.ToDouble(room.Descendants("TotalFixAmounts").First().Attribute("Nett").Value);
                if (check < minprice)
                    minprice = check;
            }
            return minprice.ToString();

        }

        #endregion

        #region Star Rating Condition
        public bool StarRating(string minRating, string MaxRating, string HotelStarRating)
        {
            if (HotelStarRating != null)
            {
                bool result;
                int minrating = Convert.ToInt32(minRating);
                int max = Convert.ToInt32(MaxRating);
                int star = Convert.ToInt32(HotelStarRating);
                if (star <= max && star >= minrating)
                    result = true;
                else
                    result = false;
                return result;
            }
            else
                return false;
        }
        #endregion

        #region Remove Extra Tags
        public XElement removetags(XElement input)
        {
            if (input.Descendants("id").Any())
                input.Descendants("id").Remove();
            if (input.Descendants("paxes").Any())
                input.Descendants("paxes").Remove();
            if (input.Descendants("NewChildCount").Any())
                input.Descendants("NewChildCount").Remove();
            return input;

        }
        #endregion

        #region Get Xmls From Log
        public List<XElement> LogXMLs(string trackID, int logtypeID, int SupplierID)
        {
            List<XElement> response = new List<XElement>();
            DataTable LogTable = new DataTable();
            LogTable = jdata.GetLog(trackID, logtypeID, SupplierID);
            string checkreq = LogTable.Rows[0]["logrequestXML"].ToString();
            if (checkreq.Equals(null) || checkreq.Equals(string.Empty))
            {
                for (int i = 0; i < LogTable.Rows.Count; i++)
                    response.Add(new XElement(LogTable.Rows[i]["logType"].ToString(),
                                     new XElement("Request", null),
                                     new XElement("Response", XElement.Parse(LogTable.Rows[i]["logresponseXML"].ToString()))));
                return response;
            }
            for (int i = 0; i < LogTable.Rows.Count; i++)
            {
                response.Add(new XElement(LogTable.Rows[i]["logType"].ToString(),
                                           new XElement("Request", XElement.Parse(LogTable.Rows[i]["logrequestXML"].ToString())),
                                           new XElement("Response", XElement.Parse(LogTable.Rows[i]["logresponseXML"].ToString()))));
            }
            return response;
        }
        #endregion

        #region List of Hotel Codes
        public XElement HotelCodes(string CityCode, string SupplierID, string HotelCode, string HotelName)
        {
            //XElement DataList = new XElement("Hotels");
            //DataTable hotelsList = jdata.GetHotelsList(CityCode, SupplierID);
            //List<string> NewResponse = new List<string>();
            //List<string> RegionList = new List<string>();
            //for (int i = 0; i < hotelsList.Rows.Count; i++)
            //{
            //    DataList.Add(new XElement("Hotel",
            //                        new XElement("HotelID", hotelsList.Rows[i]["HotelID"].ToString()),
            //                        new XElement("Region", hotelsList.Rows[i]["ZoneName"].ToString())));
            //}
            XElement DataList = new XElement("Hotels");
            DataTable hotelsList = jdata.GetHotelsList(CityCode, SupplierID, HotelCode, HotelName);

            var query = from p in hotelsList.AsEnumerable()
                        select new XElement("Hotel",
                            new XElement("HotelID", p.Field<string>("HotelID")),
                            new XElement("Region", p.Field<string>("ZoneName")));


            DataList.Add(query);
            return DataList;
        }
        public List<XElement> HotelCodesNew(string CityCode, string SupplierID, string HotelCode, string HotelName)
        {
            DataTable hotelsList = jdata.GetHotelsList(CityCode, SupplierID, HotelCode, HotelName);
            List<XElement> Hotels = new List<XElement>();
            //List<string> NewResponse = new List<string>();
            for (int i = 0; i < hotelsList.Rows.Count; i++)
            {
                Hotels.Add(new XElement("Hotel",
                                    new XElement("HotelID", hotelsList.Rows[i]["HotelID"].ToString()),
                                    new XElement("HBECode", hotelsList.Rows[i]["HBEcode"].ToString()),
                                    new XElement("JPCode", hotelsList.Rows[i]["JPCode"].ToString()),
                                    new XElement("Region", hotelsList.Rows[i]["CityName"].ToString())));
            }
            return Hotels.Distinct().ToList();
        }
        #endregion

        #region Facilities
        //public XElement HotelFacilities(string CityID)
        //{
        //    XElement response = new XElement("FacilitiesCityWise");
        //    List<XElement> Hotels = new List<XElement>();

        //    DataTable Facilities = jdata.GetW2MHotelFacility(CityID);
        //    if (Facilities != null && Facilities.Rows.Count != 0)
        //    {                
        //        var facil = Facilities.Columns.Cast<DataColumn>()
        //                                           .Select(x => x.ColumnName);
        //        string[] facilityArray = facil.ToArray();
        //        for (int j = 0; j < Facilities.Rows.Count; j++)
        //        {
        //            DataRow fac = Facilities.Rows[j];
        //            if (!String.IsNullOrEmpty(fac["Facilities"].ToString()))
        //            {
        //                XElement Hotel = new XElement("Hotel",
        //                                new XAttribute("HotelID", fac["HotelID"]),
        //                                new XAttribute("CityID", fac["CityID"]));

        //                XElement Temp = XElement.Parse(fac["Facilities"].ToString());
        //                List<XElement> TempFacilities = new List<XElement>();
        //                List<XElement> TempData = Temp.Descendants("Feature").ToList();
        //                foreach (XElement f in TempData)
        //                {
        //                    TempFacilities.Add(new XElement("Facility", f.Value));
        //                }

        //                Hotel.Add(new XElement("Facilities", TempFacilities));
        //                Hotels.Add(Hotel);
        //            }
        //        }
        //    }
        //    //else
        //    //{
        //    //    response = new XElement("Facilities", new XElement("Facility", "No Facility Available"));
        //    //}
        //    response.Add(Hotels);
        //    return response;
        //}
        #endregion



        #endregion
        #region PreBook Cancellation Policy
        public XElement PreBookPolicy(XElement HotelOption, XElement travReq, List<XElement> pbList)
        {
            double perNightRate = 0.0;
            double totalRate = Convert.ToDouble(travReq.Descendants("RoomTypes").First().Attribute("TotalRate").Value);
            double firstnight = 0.0;
            string ind = travReq.Descendants("RoomTypes").First().Attribute("Index").Value;
            foreach (XElement room in pbList)
            {

                firstnight += Convert.ToDouble(room.Descendants("Price").Where(x => x.Attribute("Night").Value.Equals("1")).First().Attribute("PriceValue").Value);
                //perNightRate = perNightRate + Convert.ToDouble(room.Attribute("PerNightRoomRate").Value);
            }

            XElement cp = new XElement("CancellationPolicies");

            #region Cancellation Policy Tag
            if (HotelOption.Descendants("Rule").Any())
            {
                foreach (XElement policy in HotelOption.Descendants("Rule"))
                {
                    //int daysbefore = Convert.ToInt32(policy.Descendants("dias_antelacion").First().Value);
                    DateTime date = DateTime.ParseExact(policy.Attribute("DateFrom").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    //date = date.AddDays(-daysbefore);
                    string dt = date.ToString("dd/MM/yyyy");
                    dt = dt.Replace('-', '/');
                    double nightsCharged = 0;
                    double rate = 0.0;
                    if (Convert.ToInt32(policy.Attribute("Nights").Value) == 0)
                    {
                        if (!policy.Attribute("FixedPrice").Value.Equals("0"))
                        {
                            rate = Convert.ToDouble(policy.Attribute("FixedPrice").Value);
                        }
                        else
                        {
                            double per = Convert.ToDouble(policy.Attribute("PercentPrice").Value);
                            nightsCharged = per / 100;
                            rate = nightsCharged * totalRate;
                        }
                    }
                    else
                    {
                        if (policy.Attribute("ApplicationTypeNights").Value.Equals("FirstNight"))
                        {
                            rate = firstnight;
                        }
                        else
                        {
                            #region Nights
                            DateTime from = DateTime.ParseExact(travReq.Descendants("FromDate").First().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            DateTime toDate = DateTime.ParseExact(travReq.Descendants("ToDate").First().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            int bookingnights = toDate.Subtract(from).Days;
                            #endregion
                            //int bookingnights = Convert.ToInt32(travReq.Descendants("Nights").First().Value);
                            rate = totalRate / bookingnights;
                            nightsCharged = Convert.ToDouble(policy.Attribute("Nights").Value);
                            rate = rate * nightsCharged;
                        }
                    }
                    rate = Math.Round(rate, 2);
                    string checkinDate = travReq.Descendants("FromDate").FirstOrDefault().Value;
                    if (policy.Attribute("Type").Value.Equals("V"))
                    {
                        cp.Add(new XElement("CancellationPolicy",
                                                       new XAttribute("LastCancellationDate", dt),
                                                       new XAttribute("ApplicableAmount", Convert.ToString(rate)),
                                                       new XAttribute("NoShowPolicy", dt.Equals(checkinDate) ? "1" : "0")));
                    }
                    else if (policy.Attribute("Type").Value.Equals("R"))
                    {
                        string newdt = TravayooDate(policy.Attribute("DateFrom").Value);
                        cp.Add(new XElement("CancellationPolicy",
                            // new XAttribute("LastCancellationDate", DateTime.Now.AddDays(Convert.ToDouble(policy.Attribute("From").Value)).ToString("dd/MM/yyyy")),
                                                        new XAttribute("LastCancellationDate", newdt),
                                                       new XAttribute("ApplicableAmount", Convert.ToString(rate)),
                                                       new XAttribute("NoShowPolicy", dt.Equals(checkinDate) ? "1" : "0")));
                    }
                    else
                    {
                        cp.Add(new XElement("CancellationPolicy",
                                                      new XAttribute("LastCancellationDate", travReq.Descendants("FromDate").First().Value),
                                                      new XAttribute("ApplicableAmount", Convert.ToString(rate)),
                                                      new XAttribute("NoShowPolicy", "1")));
                    }
                }
            }
            else
            {
                double amount = Convert.ToDouble(HotelOption.Descendants("TotalFixAmounts").FirstOrDefault().Attribute("Nett").Value);
                List<XElement> Cxps = new List<XElement>(){
                    new XElement("CancellationPolicies",
                        new XElement("CancellationPolicy",
                            new XAttribute("LastCancellationDate", DateTime.Now.AddDays(-2).ToString("dd/MM/yyyy")),
                            new XAttribute("ApplicableAmount",amount.ToString()),
                            new XAttribute("NoShowPolicy","0")))
                };
            }
            List<XElement> mergeinput = new List<XElement>();
            cp.Descendants("CancellationPolicy").Where(x => x.Attribute("ApplicableAmount").Value.Equals("0")).Remove();
            mergeinput.Add(cp);
            XElement finalcp = MergCxlPolicy(mergeinput);
            #endregion
            return finalcp;
        }
        #endregion
        #region Price breakup
        public XElement PriceBackup(XElement roomratelist, string roomsource, int totalNight, DateTime startdate)
        {
            XElement response = new XElement("PriceBreakups");

            List<XElement> Taxlist = new List<XElement>();
            if (roomratelist.Descendants("Taxes").Any())
                Taxlist = roomratelist.Descendants("Tax").Where(x => x.Attribute("Included").Value.Equals("false") && x.Attribute("IsFix").Value.Equals("false") && x.Attribute("Commissionable").Value.Equals("false")).ToList();

            #region Date Wise
            for (int j = 1; j <= totalNight; j++)
            {
                double roomrate = 0;
                double supplrate = 0;
                double taxnight = 0;

                DateTime nightdate = startdate.AddDays(j - 1);
                string checkdate = JuniperDate(nightdate.ToString("dd/MM/yyyy"));

                foreach (XElement roomsoffer in roomratelist.Descendants("Concept").ToList())
                {
                    if (roomsoffer.Attribute("Type").Value == "BAS")
                    {
                        List<XElement> itemlist = new List<XElement>();
                        //itemlist = roomsoffer.Descendants("Item").Where(x => x.Attribute("Source").Value.Equals(roomsource) && x.Attribute("Date").Value.Equals(checkdate)).ToList();

                        itemlist = roomsoffer.Descendants("Item").Where(x => x.Attributes("Source").Any() && x.Attribute("Source").Value.Equals(roomsource)).ToList();
                        //itemlist.Add(roomsoffer.Descendants("Item").Where(x => !x.Attributes("Source").Any());

                        foreach (XElement item in roomsoffer.Descendants("Item").Where(x => !x.Attributes("Source").Any()))
                            itemlist.Add(item);

                        foreach (XElement lin in itemlist)
                        {
                            if (lin.Attributes("Days").Any() && lin.Attribute("Days").Value == "1")
                            {
                                if (lin.Attribute("Date").Value == checkdate)
                                {
                                    roomrate = roomrate + (Convert.ToDouble(lin.Attribute("Amount").Value) * Convert.ToDouble(lin.Attribute("Quantity").Value));
                                }
                            }
                            else if (!lin.Attributes("Days").Any())
                            {
                                supplrate = supplrate + ((Convert.ToDouble(lin.Attribute("Amount").Value) / totalNight) * Convert.ToDouble(lin.Attribute("Quantity").Value));
                            }
                            else
                            {
                                roomrate = roomrate + (Convert.ToDouble(lin.Attribute("Amount").Value) * Convert.ToDouble(lin.Attribute("Quantity").Value));
                            }
                        }

                    }
                    else
                    {
                        List<XElement> itemlist = new List<XElement>();
                        //itemlist = roomsoffer.Descendants("Item").Where(x => x.Attribute("Source").Value.Equals(roomsource)).ToList();
                        itemlist = roomsoffer.Descendants("Item").Where(x => x.Attributes("Source").Any() && x.Attribute("Source").Value.Equals(roomsource)).ToList();
                        foreach (XElement item in roomsoffer.Descendants("Item").Where(x => !x.Attributes("Source").Any()))
                            itemlist.Add(item);

                        foreach (XElement lin in itemlist)
                        {

                            if (lin.Attributes("Days").Any() && lin.Attribute("Days").Value == "1")
                            {
                                if (lin.Attribute("Date").Value == checkdate)
                                {
                                    supplrate = supplrate + (Convert.ToDouble(lin.Attribute("Amount").Value) * Convert.ToDouble(lin.Attribute("Quantity").Value));
                                }
                            }
                            else if (!lin.Attributes("Days").Any())
                            {
                                supplrate = supplrate + ((Convert.ToDouble(lin.Attribute("Amount").Value) / totalNight) * Convert.ToDouble(lin.Attribute("Quantity").Value));
                            }
                            else
                            {
                                supplrate = supplrate + (Convert.ToDouble(lin.Attribute("Amount").Value) * Convert.ToDouble(lin.Attribute("Quantity").Value));
                            }

                        }

                    }

                }

                foreach (XElement taxes in Taxlist)
                {
                    if (taxes.Attribute("IsFix").Value.Equals("false"))
                    {
                        if (taxes.Attribute("ByNight").Value.Equals("false"))
                            taxnight = taxnight + ((roomrate + supplrate) * Convert.ToDouble(taxes.Attribute("Value").Value)) / 100;
                    }
                    else if (taxes.Attribute("IsFix").Value.Equals("true"))
                    {
                        if (taxes.Attribute("ByNight").Value.Equals("false"))
                        {
                            taxnight = taxnight + Convert.ToDouble(taxes.Attribute("Value").Value);

                        }
                        else
                        {
                            taxnight = taxnight + (Convert.ToDouble(taxes.Attribute("Value").Value)) / totalNight;
                        }

                    }

                }
                response.Add(new XElement("Price",
                                 new XAttribute("Night", j),
                                 new XAttribute("PriceValue", Convert.ToString(Math.Round(roomrate + supplrate + taxnight, 2)))));
            }
            #endregion
            #region Days Wise
            //            for (int j = 0; j < totalNight; j++)
            //            {
            //                double roomrate = 0;
            //                double supplrate = 0;
            //                double taxnight = 0;

            //                DateTime nightdate = startdate.AddDays(j);
            //                string checkdate = JuniperDate(nightdate.ToString("dd/MM/yyyy"));

            //                foreach (XElement roomsoffer in roomratelist.Descendants("Concept").ToList())
            //                {

            //                    if (roomsoffer.Attribute("Type").Value == "BAS")
            //                    {
            //                        List<XElement> itemlist = new List<XElement>();
            //                        itemlist = roomsoffer.Descendants("Item").Where(x => x.Attribute("Source").Value.Equals(roomsource)).ToList();

            //                        foreach (XElement lin in itemlist.Where(x=>x.Attribute("Date").Value.Equals(checkdate)))
            //                        {
            //                            int days = Convert.ToInt32(lin.Attribute("Days").Value);
            //                            for (int d = 0; d < days;d++ )
            //                                roomrate = roomrate + (Convert.ToDouble(lin.Attribute("Amount").Value));                            
            //                        }
            //                    }
            //                    else
            //                    {
            //                        List<XElement> itemlist = new List<XElement>();
            //                        itemlist = roomsoffer.Descendants("Item").Where(x => x.Attribute("Source").Value.Equals(roomsource)).ToList();
            //                        foreach (XElement lin in itemlist.Where(x => x.Attribute("Date").Value.Equals(checkdate)))
            //                        {
            //                            int days = Convert.ToInt32(lin.Attribute("Days").Value);
            //                            for (int d = 0; d < days; d++)
            //                                supplrate = supplrate + (Convert.ToDouble(lin.Attribute("Amount").Value) * Convert.ToDouble(lin.Attribute("Quantity").Value));                           
            //                        }

            //                    }

            //                }

            //                foreach (XElement taxes in Taxlist)
            //                {
            //                    if (taxes.Attribute("IsFix").Value.Equals("false"))
            //                    {
            //                        if (taxes.Attribute("ByNight").Value.Equals("false"))
            //                            taxnight = taxnight + ((roomrate + supplrate) * Convert.ToDouble(taxes.Attribute("Value").Value)) / 100;
            //                    }
            //                    else if (taxes.Attribute("IsFix").Value.Equals("true"))
            //                    {
            //                        if (taxes.Attribute("ByNight").Value.Equals("false"))
            //                        {
            //                            taxnight = taxnight + Convert.ToDouble(taxes.Attribute("Value").Value);

            //                        }
            //                        else
            //                        {
            //                            taxnight = taxnight + (Convert.ToDouble(taxes.Attribute("Value").Value)) / totalNight;
            //                        }

            //                    }

            //                }
            //                response.Add(new XElement("Price",
            //                                 new XAttribute("Night", j),
            //                                 new XAttribute("PriceValue", Convert.ToString(roomrate + supplrate + taxnight))));
            //            }

            #endregion
            //for (int j = 1; j <= totalNight; j++)
            //{
            //    response.Add(new XElement("Price",
            //                     new XAttribute("Night", j),
            //                     new XAttribute("PriceValue", Convert.ToString(roomrate + supplrate + taxnight ))));

            //}
            return response;
        }
        #endregion
        #region New Price Breakup

        protected bool IsRoomRate(XElement rmItem)
        {
            bool flag = false;
            var roomCount = (from x in rmItem.Descendants("HotelRoom")
                             from y in x.Attribute("Source").Value.Split(new char[] { ',' })
                             select new XElement("HRoom", new XAttribute("Source", y))).Distinct();

            var RateCount = (from x in roomCount
                             join y in rmItem.Descendants("Item") on x.Attribute("Source").GetValueOrDefault("0") equals y.Attribute("Source").GetValueOrDefault("0")
                             select new XElement("RoomID", x.Attribute("Source").Value)).Distinct();
            if (roomCount.Count() == RateCount.Count())
            {
                flag = true;
            }
            return flag;
        }


        public XElement SupplementTag(IEnumerable<XElement> lst)
        {
            List<XElement> itemSupl = new List<XElement>();
            try
            {
                if (lst.Count() > 0)
                {
                    var item = from itm in lst
                               select new XElement("Supplement",
                            new XAttribute("suppId", "0"),
                                 new XAttribute("suppName", itm.Element("Name").Value),
                            new XAttribute("supptType", "0"),
                            new XAttribute("suppIsMandatory", "True"),
                            new XAttribute("suppChargeType", "Included"),
                            new XAttribute("suppPrice", "0.00"),
                            new XAttribute("suppType", "PerRoomSupplement"));
                    itemSupl.AddRange(item);
                }
                return new XElement("Supplements", itemSupl.GroupBy(x => x.Attribute("suppName").Value).Select(y => y.FirstOrDefault()));
            }
            catch
            {
                XElement sup = new XElement("Supplements", "");
                return sup;
            }
        }

        public XElement RoomPromotion(IEnumerable<XElement> Promotionlst)
        {
            XElement promoItem;
            if (!Promotionlst.IsNullOrEmpty())
            {
                var result = from promo in Promotionlst
                             select new XElement("Promotions", promo.Element("Name").Value);
                promoItem = new XElement("PromotionList", result);
            }
            else
            {
                promoItem = new XElement("PromotionList", new XElement("Promotions", null));
            }
            return promoItem;
        }


















        public XElement PriceBreakup(XElement roomratelist, string roomsource, int totalNight, DateTime startDate, int roomCount, bool isRoom, decimal totalAmount)
        {
            XElement breakup = new XElement("PriceBreakups");
            if (isRoom)
            {


                decimal perNightTax = 0;

                if (roomratelist.Descendants("ServiceTaxes").Count() > 0)
                {
                    var taxItem = roomratelist.Descendants("ServiceTaxes").FirstOrDefault();
                    if (taxItem.Attribute("Included").Value.Equals("false"))
                    {
                        perNightTax = Convert.ToDecimal(taxItem.Attribute("Amount").Value) / totalNight / roomCount;
                    }
                }



                List<Helper> ItemList = new List<Helper>();
                foreach (XElement concept in roomratelist.Descendants("Concept"))
                {
                    foreach (XElement item in concept.Descendants("Item"))
                    {
                        decimal itemAmount = Convert.ToDecimal(item.Attribute("Amount").Value);
                        int quantity = Convert.ToInt32(item.Attribute("Quantity").Value);
                        if (item.Attributes("Days").Any())
                        {
                            int days = Convert.ToInt32(item.Attribute("Days").Value);

                            DateTime itemDate = item.Attribute("Date").GetCheckinDate("yyyy-MM-dd", startDate.ToString("yyyy-MM-dd"));

                            string source = "0";
                            itemAmount *= quantity;
                            if (item.Attributes("Source").Any())
                            {
                                var countt = concept.Descendants("Item").Where(x => x.Attribute("Source").GetValueOrDefault("0") == roomsource).Count();
                                if (countt == 0)
                                {
                                    source = roomsource;
                                }
                                else
                                {
                                    source = item.Attribute("Source").Value;
                                }
                            }
                            else
                            {
                                itemAmount /= roomCount;
                            }


                            for (int i = 0; i < days; i++)
                            {
                                DateTime contentDate = itemDate.AddDays(i);
                                Helper itemContent = new Helper
                                {
                                    itemDate = contentDate,
                                    Amount = itemAmount,
                                    itemName = concept.Attribute("Name").Value,
                                    itemType = concept.Attribute("Type").Value,
                                    quantity = Convert.ToInt32(item.Attribute("Quantity").Value),
                                    roomSource = item.Attributes("Source").Any() ? item.Attribute("Source").Value : "0"
                                };
                                ItemList.Add(itemContent);
                            }
                        }
                        else
                        {


                            itemAmount /= totalNight;
                            string source = "0";
                            if (item.Attributes("Source").Any())
                            {

                                var countt = concept.Descendants("Item").Where(x => x.Attribute("Source").GetValueOrDefault("0") == roomsource).Count();
                                if (countt == 0)
                                {
                                    source = roomsource;
                                }
                                else
                                {
                                    source = item.Attribute("Source").Value;
                                }
                            }
                            else
                            {
                                itemAmount /= roomCount;
                            }


                            for (int i = 0; i < totalNight; i++)
                            {
                                DateTime contentDate = startDate.AddDays(i);
                                Helper itemContent = new Helper
                                {
                                    itemDate = contentDate,
                                    Amount = itemAmount,
                                    itemName = concept.Attribute("Name").Value,
                                    itemType = concept.Attribute("Type").Value,
                                    quantity = Convert.ToInt32(item.Attribute("Quantity").Value),
                                    roomSource = source
                                };
                                ItemList.Add(itemContent);
                            }
                        }
                    }
                }
                if (ItemList.Where(x => x.roomSource == roomsource || x.roomSource == "0").GroupBy(x => x.itemDate).Count() != totalNight)
                {
                    var result = ItemList.Where(x => x.roomSource == roomsource || x.roomSource == "0").
                        GroupBy(x => x.itemDate).
                        Select(x => x.Sum(y => y.Amount)).FirstOrDefault();


                    for (int i = 0; i < totalNight; i++)
                    {
                        var item = new XElement("Price",
                         new XAttribute("Night", i + 1),
                         new XAttribute("PriceValue", ((result / totalNight) + perNightTax)));
                        breakup.Add(item);
                    }

                }
                else
                {
                    var result = ItemList.Where(x => x.roomSource == roomsource || x.roomSource == "0").GroupBy(x => x.itemDate).
                        Select((value, index) => new XElement("Price",
                         new XAttribute("Night", ++index),
                         new XAttribute("PriceValue", (value.Sum(x => x.Amount) + perNightTax))));
                    breakup.Add(result);
                }

            }
            else
            {

                decimal price = (totalAmount / roomCount) / totalNight;
                for (int i = 1; i <= totalNight; i++)
                {
                    breakup.Add(new XElement("Price",
              new XAttribute("Night", i),
              new XAttribute("PriceValue", Math.Round(price, 3))));
                }
            }
            return breakup;
        }
        public XElement PriceBreakup_old(XElement roomratelist, string roomsource, int totalNight, DateTime startDate, int roomCount)
        {
            XElement breakup = new XElement("PriceBreakups");
            List<Helper> ItemList = new List<Helper>();

            DateTime date = startDate;
            foreach (XElement concept in roomratelist.Descendants("Concept"))
            {
                foreach (XElement item in concept.Descendants("Item"))
                {

                    if (item.Attributes("Days").Any())
                    {
                        int days = Convert.ToInt32(item.Attribute("Days").Value);
                        DateTime itemDate = item.Attribute("Date").GetCheckinDate("yyyy-MM-dd", startDate.ToString("yyyy-MM-dd"));
                        //DateTime itemDate = DateTime.ParseExact(item.Attribute("Date").Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);    
                        decimal itemAmount = Convert.ToDecimal(item.Attribute("Amount").Value);
                        if (!item.Attributes("Source").Any())
                            itemAmount = itemAmount / roomCount;
                        for (int i = 0; i < days; i++)
                        {
                            DateTime contentDate = itemDate.AddDays(i);
                            Helper itemContent = new Helper
                            {
                                itemDate = contentDate,
                                Amount = itemAmount,
                                itemName = concept.Attribute("Name").Value,
                                itemType = concept.Attribute("Type").Value,
                                quantity = Convert.ToInt32(item.Attribute("Quantity").Value),
                                roomSource = item.Attributes("Source").Any() ? item.Attribute("Source").Value : "0"
                            };
                            ItemList.Add(itemContent);
                        }
                    }
                    else
                    {
                        decimal newAmount = Convert.ToDecimal(item.Attribute("Amount").Value) / totalNight;
                        if (!item.Attributes("Source").Any())
                            newAmount = newAmount / roomCount;
                        for (int i = 0; i < totalNight; i++)
                        {
                            DateTime contentDate = date.AddDays(i);
                            Helper itemContent = new Helper
                            {
                                itemDate = contentDate,
                                Amount = newAmount,
                                itemName = concept.Attribute("Name").Value,
                                itemType = concept.Attribute("Type").Value,
                                quantity = Convert.ToInt32(item.Attribute("Quantity").Value),
                                roomSource = item.Attributes("Source").Any() ? item.Attribute("Source").Value : "0"
                            };
                            ItemList.Add(itemContent);
                        }
                    }
                }
            }
            var result = ItemList.Where(x => x.roomSource == roomsource || x.roomSource == "0").GroupBy(x => x.itemDate).
               Select((value, index) => new XElement("Price",
                                   new XAttribute("Night", ++index),
                                   new XAttribute("PriceValue", value.Sum(x => x.Amount * x.quantity))));
            breakup.Add(result);
            #region firstly Coded
            //DateTime endDate = date.AddDays(totalNight);
            //int night = 1;
            //if (ItemList.Select(x => x.roomSource).ToList().Contains("0"))
            //{
            //    while (date < endDate)
            //    {
            //        decimal nightAmount = 0;
            //        List<Helper> dateWiseList = ItemList.Where(x => x.itemDate.Equals(date)).ToList();
            //        foreach (Helper item in dateWiseList)
            //            nightAmount += (item.Amount * item.quantity);
            //        breakup.Add(new XElement("Price",
            //                        new XAttribute("Night", night++),
            //                        new XAttribute("PriceValue", Convert.ToString(nightAmount))));
            //        date = date.AddDays(1);
            //    }
            //}
            //else
            //{
            //    while (date < endDate)
            //    {
            //        decimal nightAmount = 0;
            //        List<Helper> dateWiseList = ItemList.Where(x => x.itemDate.Equals(date) && x.roomSource.Equals(roomsource)).ToList();
            //        foreach (Helper item in dateWiseList)
            //            nightAmount += (item.Amount * item.quantity);
            //        breakup.Add(new XElement("Price",
            //                        new XAttribute("Night", night++),
            //                        new XAttribute("PriceValue", Convert.ToString(nightAmount))));
            //        date = date.AddDays(1);
            //    }
            //}
            #endregion
            return breakup;
        }
        #endregion
        #region Paxes
        private List<XElement> bindpax(List<XElement> paxlst)
        {
            List<XElement> strpax = new List<XElement>();
            int idpax = 1;
            for (int i = 0; i < paxlst.Count(); i++)
            {
                int adultcount = Convert.ToInt16(paxlst[i].Descendants("Adult").FirstOrDefault().Value);
                for (int j = 0; j < adultcount; j++)
                {
                    strpax.Add(new XElement(soap + "Pax",
                        new XAttribute("IdPax", idpax),
                        new XElement(soap + "Age", "30")
                        ));
                    idpax++;
                }
                if (Convert.ToInt16(paxlst[i].Descendants("Child").FirstOrDefault().Value) > 0)
                {
                    List<XElement> childlst = paxlst[i].Descendants("ChildAge").ToList();
                    //for (int k = 0; k < childlst.Count(); k++)
                    //{
                    //    string childage = childlst[k].Element("ChildAge").Value;
                    //    strpax.Add(new XElement(soap + "Pax",
                    //        new XAttribute("IdPax", idpax),
                    //        new XElement(soap + "Age", childage)
                    //        ));
                    //    idpax++;
                    //}
                    foreach (XElement ch in childlst)
                    {
                        string childage = ch.Value;
                        strpax.Add(new XElement(soap + "Pax",
                            new XAttribute("IdPax", idpax),
                            new XElement(soap + "Age", childage)
                            ));
                        idpax++;
                    }
                }
            }
            return strpax;
        }

        private List<XElement> bindRomPax(List<XElement> paxlst)
        {
            List<XElement> strpax = new List<XElement>();
            int idpax = 1;
            for (int i = 0; i < paxlst.Count(); i++)
            {
                XElement RoomPax = new XElement(soap + "RelPaxDist",
                                        new XElement(soap + "RelPaxes"));
                int adultcount = Convert.ToInt16(paxlst[i].Descendants("Adult").FirstOrDefault().Value);
                for (int j = 0; j < adultcount; j++)
                {
                    RoomPax.Descendants(soap + "RelPaxes").First().Add(
                            new XElement(soap + "RelPax",
                            new XAttribute("IdPax", idpax)));
                    idpax++;
                }
                if (Convert.ToInt16(paxlst[i].Descendants("Child").FirstOrDefault().Value) > 0)
                {
                    List<XElement> childlst = paxlst[i].Descendants("ChildAge").ToList();
                    for (int k = 0; k < childlst.Count(); k++)
                    {
                        string childage = childlst[k].Value;
                        RoomPax.Descendants(soap + "RelPaxes").First().Add(
                             new XElement(soap + "RelPax",
                             new XAttribute("IdPax", idpax)));
                        idpax++;
                    }
                }
                strpax.Add(RoomPax);
            }
            return strpax;
        }
        #endregion

        #region HotelCodes

        private XElement BindHotelCodes(List<string> hlst)
        {
            XElement htlItem;
            if (hlst != null)
            {
                var result = from item in hlst
                             select new XElement(soap + "HotelCode", item);
                htlItem = new XElement(soap + "HotelCodes", result);
            }
            else
            {
                htlItem = new XElement(soap + "HotelCodes", new XElement("HotelCode", 0));
            }
            return htlItem;
        }


        #endregion
        public string JuniperDate(string d)
        {
            DateTime dt = DateTime.ParseExact(d, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            string date = dt.ToString("yyyy-MM-dd");
            return date;
        }

        public string TravayooDate(string d)
        {
            string dd = d.Substring(0, 10);
            DateTime dt = DateTime.ParseExact(dd, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            string date = dt.ToString("dd/MM/yyyy");
            // date = date.Replace('-', '/');

            return date;
        }

        #region BindPromotions
        public List<XElement> RoomPromotion(XElement SuplementList, XElement offerslist, string roomsource)
        {
            List<XElement> promotion = new List<XElement>();
            if (offerslist != null)
            {
                foreach (XElement offer in offerslist.Descendants("HotelOffer"))
                {
                    // string promocode = offer.Descendants("HotelOffer").First().Attribute("Code").Value;
                    //List<XElement> supplements = SuplementList.Descendants("Concept").Where(x => x.Attribute("Type").Value.Equals("SUP")).ToList();
                    //foreach (XElement roomsoffer in supplements.Where(x => x.Attribute("RelationalCode").Value.Equals(offer.Attribute("Code").Value)).ToList())
                    {
                        //if( roomsoffer.Descendants("Item").Where( x=> x.Attribute("Source").Value.Equals(roomsource)).Count()>0)
                        if (offer.Descendants("Name").Any())
                            promotion.Add(new XElement("Promotions", offer.Element("Name").Value));
                        else if (offer.Descendants("Description").Any())
                            promotion.Add(new XElement("Promotions", offer.Element("Description").Value));
                    }
                }
            }

            return promotion;
        }

        public List<XElement> RoomSupplement(XElement SuplementList, XElement offerslist, string roomsource)
        {
            List<XElement> Supplement = new List<XElement>();
            //List<XElement> listsupplements = SuplementList.Descendants("Concept").Where(x => x.Attribute("Type").Value.Equals("SUP") || x.Attribute("Type").Value.Equals("OTH")).ToList();
            List<XElement> listsupplements = SuplementList.Descendants("Concept").
         Where(x => (x.Attribute("Type").Value.Equals("SUP") || x.Attribute("Type").Value.Equals("OTH"))
             && (x.Descendants("Item").Where(y => y.Attribute("Source").GetValueOrDefault(roomsource).Equals(roomsource)).Count() > 0
             )).ToList();
            List<string> OfferCodes = new List<string>();
            if (offerslist != null && offerslist.Descendants("HotelOffer").Attributes("Code").Any())
                OfferCodes = offerslist.Descendants("HotelOffer").Where(y => y.Attributes("Code").Any()).Select(x => x.Attribute("Code").Value).ToList();
            foreach (XElement roomsoffer in listsupplements)
            {
                //string str = roomsoffer.Attributes("RelationalCode").Any() ? roomsoffer.Attribute("RelationalCode").Value : null;
                string suppID = roomsoffer.Attributes("RelationalCode").Any() ? roomsoffer.Attribute("RelationalCode").Value : "00";
                Supplement.Add(new XElement("Supplement",
                          new XAttribute("suppId", suppID),
                          new XAttribute("suppName", roomsoffer.Attribute("Name").Value),
                          new XAttribute("supptType", roomsoffer.Attribute("Type").Value),
                          new XAttribute("suppIsMandatory", OfferCodes.Count > 0 ? OfferCodes.Contains(suppID) ? "TRUE" : "True" : "True"),
                          new XAttribute("suppChargeType", "Included"),
                          new XAttribute("suppPrice", roomsoffer.Descendants("Item").Any() ? roomsoffer.Descendants("Item").FirstOrDefault().Attribute("Amount").Value : "00"),
                          new XAttribute("suppType", roomsoffer.Attribute("Type").Value)));
            }

            return Supplement;
        }

        public List<XElement> OptionalSupplement(XElement SuplementList, XElement offerslist, string roomsource)
        {
            List<XElement> Supplement = new List<XElement>();
            List<XElement> listsupplements = SuplementList.Descendants("Concept").Where(x => x.Attribute("Type").Value.Equals("SUP")).ToList();
            foreach (XElement roomsoffer in listsupplements)
            {

                Supplement.Add(new XElement("Supplement",
                          new XAttribute("suppId", roomsoffer.Attribute("RelationalCode").Value),
                          new XAttribute("suppName", roomsoffer.Attribute("Name").Value),
                          new XAttribute("supptType", roomsoffer.Attribute("Type").Value),
                          new XAttribute("suppIsMandatory", "TRUE"),
                          new XAttribute("suppChargeType", "Addition"),
                          new XAttribute("suppPrice", "0"),
                          new XAttribute("suppType", roomsoffer.Attribute("Type").Value)));
            }

            return Supplement;
        }

        private XElement bookingpax(XElement paxCount, int HolderCount, string countryCode)
        {
            //List<XElement> pax=new List<XElement>();
            //pax.Add(new XElement(soap+"Pax",
            //    new XAttribute("IdPax=", "")));
            XElement Response = new XElement("Rooms");
            List<XElement> paxes = new List<XElement>();
            XElement PaxDetail = new XElement(soap + "Paxes");
            List<XElement> leads = paxCount.Descendants("PaxInfo").Where(x => x.Descendants("IsLead").First().Value.Equals("true")).ToList();
            int idpax = 1;
            foreach (XElement room in paxCount.Descendants("Room"))
            {
                foreach (XElement guest in room.Descendants("PaxInfo"))
                {
                    if (guest.Descendants("IsLead").First().Value.Equals("true") && HolderCount > 0)
                    {
                        PaxDetail.Add(new XElement(soap + "Pax",
                            new XAttribute("IdPax", idpax++),
                            new XElement(soap + "Name", guest.Element("FirstName").Value),
                            new XElement(soap + "Surname", guest.Element("LastName").Value),
                            // new XElement(soap + "Email", "asdas@asdfsd.asd"),
                            new XElement(soap + "PhoneNumbers",
                                new XElement(soap + "PhoneNumber", "000000000")),
                            new XElement(soap + "Email", "noreply@test.com"),
                            new XElement(soap + "Address", "Address"),
                            new XElement(soap + "City", "City"),
                            new XElement(soap + "Country", "Country"),
                            new XElement(soap + "PostalCode", "00000"),
                            new XElement(soap + "Age", "30"),
                            new XElement(soap + "Nationality", countryCode)
                            /*   new XElement(soap + "PhoneNumbers",
                                 new XElement(soap + "PhoneNumber", "000000000")),
                                 new XElement(soap + "Address", "Address"),
                                 new XElement(soap + "City", "City"),
                                 new XElement(soap + "Country", "Country"),
                                 new XElement(soap + "PostalCode", "00000")*/
                            ));

                        HolderCount--;
                    }
                    else
                    {
                        if (guest.Element("GuestType").Value.Equals("Adult"))
                        {
                            PaxDetail.Add(new XElement(soap + "Pax",
                              new XAttribute("IdPax", idpax++),
                              new XElement(soap + "Name", guest.Element("FirstName").Value),
                              new XElement(soap + "Surname", guest.Element("LastName").Value),
                              new XElement(soap + "Age", "30")));
                        }
                        else
                        {
                            PaxDetail.Add(new XElement(soap + "Pax",
                              new XAttribute("IdPax", idpax++),
                              new XElement(soap + "Name", guest.Element("FirstName").Value),
                              new XElement(soap + "Surname", guest.Element("LastName").Value),
                              new XElement(soap + "Age", guest.Element("Age").Value)));
                        }
                    }
                }
            }


            //bool holder = true;
            //for (int i = 0; i < HolderCount; i++)
            //{
            //    int adultcount = Convert.ToInt16(paxCount.Descendants("Adult").FirstOrDefault().Value);
            //    for (int j = 0; j < adultcount; j++)
            //    {
            //        if (holder)
            //        {
            //            PaxDetail.Add(new XElement(soap + "Pax",
            //                new XAttribute("IdPax", idpax),
            //                new XElement(soap + "Name", "Name"),
            //                new XElement(soap + "Surname", "Name"),
            //                new XElement(soap + "Email", "Name"),
            //                new XElement(soap + "PhoneNumbers",
            //                    new XElement(soap + "PhoneNumber", "")),
            //                new XElement(soap + "Email", "Name"),
            //                new XElement(soap + "Address", "Name"),
            //                new XElement(soap + "City", "Name"),
            //                new XElement(soap + "Country", "Name"),
            //                new XElement(soap + "PostalCode", "Name"),
            //                new XElement(soap + "Age", "30"),
            //                new XElement(soap + "Nationality", "Name")));

            //        }
            //        else
            //        {
            //            PaxDetail.Add(new XElement(soap + "Pax",
            //               new XAttribute("IdPax", idpax),
            //               new XElement(soap + "Name", "Name"),
            //               new XElement(soap + "Surname", "Name"),                         
            //               new XElement(soap + "Age", "30")  ));
            //        }
            //        idpax++;
            //    }
            //    if (Convert.ToInt16(paxCount.Descendants("Child").FirstOrDefault().Value) > 0)
            //    {
            //        List<XElement> childlst = paxCount.Descendants("ChildAge").ToList();

            //        foreach (XElement ch in childlst)
            //        {
            //            string childage = ch.Value;
            //            PaxDetail.Add(new XElement(soap + "Pax",
            //             new XAttribute("IdPax", idpax),
            //            new XElement(soap + "Name", "Name"),
            //            new XElement(soap + "Surname", "Name"),
            //            new XElement(soap + "Email", "Name"),
            //            new XElement(soap + "PhoneNumbers",
            //                new XElement(soap + "PhoneNumber", "")),
            //            new XElement(soap + "Email", "Name"),
            //            new XElement(soap + "Address", "Name"),
            //            new XElement(soap + "City", "Name"),
            //            new XElement(soap + "Country", "Name"),
            //            new XElement(soap + "PostalCode", "Name"),
            //            new XElement(soap + "Age", childage),
            //            new XElement(soap + "Nationality", "Name")

            //            ));
            //            idpax++;
            //        }
            //    }
            //}
            return PaxDetail;


        }



        private void JuniperConfiguration(int suppID, int CustId)
        {
            //XElement SupplCredetials = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/SupplierCredential/suppliercredentials.xml"));
            //XDocument SupplCredetials = XDocument.Load(Path.Combine(HttpRuntime.AppDomainAppPath, ConfigurationManager.AppSettings["SupplierCredetial"] + @"suppliercredentials.xml"));
            //XElement SuplCred = SupplCredetials.Descendants("credential").Where(x => x.Attribute("customerid").Value.Equals(CustId.ToString()) &&  x.Attribute("supplierid").Value.Equals(suppID.ToString())).FirstOrDefault();
            XElement SuplCred = supplier_Cred.getsupplier_credentials(Convert.ToString(CustId), Convert.ToString(suppID));
            _JNPRHotSearURL = SuplCred.Element("JNPRHotSearURL").Value;
            _JNPRHotPreBookURL = SuplCred.Element("JNPRHotPreBookURL").Value;
            _JNPRHotBookURL = SuplCred.Element("JNPRHotBookURL").Value;
            _JNPRHotBookCanURL = SuplCred.Element("JNPRHotBookCanURL").Value;
            _JNPRLoginID = SuplCred.Element("Login").Value;
            _JNPRPassword = SuplCred.Element("Password").Value;
            _JNPRVersion = SuplCred.Element("Version").Value;

            switch (suppID)
            {
                case 16: _JNPRSupplierName = "W2M";
                    break;
                case 17: _JNPRSupplierName = "EgyptExpress";
                    break;
                case 23: _JNPRSupplierName = "LOH";
                    break;
                case 35: _JNPRSupplierName = "LCI";
                    break;
                case 41: _JNPRSupplierName = "AlphaTours";
                    break;
            }
        }

        #endregion
    }

}